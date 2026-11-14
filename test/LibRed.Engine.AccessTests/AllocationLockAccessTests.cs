using System.Collections.Concurrent;
using System.Data.OleDb;
using System.Diagnostics;
using LibRed.Core.Tests;
using Xunit;

namespace LibRed.Engine.Tests;

// LibRed and ACE inserting into one file at once (docs/format/locking.md §4): the table's definition page locked first
// serialises the two engines' INSERTs into one table, so each draws its own AutoNumber; page 1 locked for every change to
// the global maps keeps their reservations apart, so neither hands out or writes over a page the other took. ACE runs on
// the test's own thread throughout (its objects are created and used on one thread, one connection per test); LibRed on
// a thread of its own. Each side runs its own statement again when it is refused with 3218 (or 3211 for a table).
[Collection(AceCollection.Name)]
public class AllocationLockAccessTests
{
    private const string CurrentlyLocked = "Could not update; currently locked.";
    private const int Rows = 100;
    private const int MaxAttempts = 20;

    // ACE refreshes another user's pages from the file on its own interval (PageTimeout, 5 s by default).
    private static readonly TimeSpan AceSees = TimeSpan.FromSeconds(7);

    private static string Northwind => Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");

    private static readonly string LongMemo = new('m', 3000);

    [Fact]
    public void ACE_and_LibRed_inserting_into_one_AutoNumber_table_at_once_draw_distinct_ids_and_lose_no_row() =>
        InsertAtOnce(memo: false);

    [Fact]
    public void ACE_and_LibRed_inserting_rows_with_long_memos_at_once_allocate_apart() =>
        InsertAtOnce(memo: true);

    [Fact]
    public void An_INSERT_in_LibReds_open_transaction_keeps_ACEs_INSERT_into_the_table_out_until_COMMIT()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-alloclock-ace-");
        try
        {
            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                var engine = new QueryEngine(database);
                engine.ExecuteNonQuery("CREATE TABLE T (Id AUTOINCREMENT PRIMARY KEY, Who TEXT(10), A MEMO)");
                engine.ExecuteNonQuery("INSERT INTO T (Who) VALUES ('first')");

                using OleDbConnection ace = AceTestDatabase.Open(path);
                Assert.Equal(1, Count(ace));
                engine.ExecuteNonQuery("BEGIN TRANSACTION");
                engine.ExecuteNonQuery("INSERT INTO T (Who) VALUES ('libred')");

                // LibRed holds the table's definition page to COMMIT (ACE lets its own go about 500 ms after the
                // INSERT; LibRed does not, see verification-backlog.md): ACE's INSERT is refused with 3218.
                var refused = Assert.Throws<OleDbException>(() => Exec(ace, "INSERT INTO T (Who) VALUES ('ace')"));
                Assert.Equal("3218", refused.Errors[0].SQLState);

                engine.ExecuteNonQuery("COMMIT");
                Exec(ace, "INSERT INTO T (Who) VALUES ('ace')");
                Assert.True(Poll(AceSees, () => Count(ace) == 3), "ACE did not read every row.");
                Assert.True(Poll(AceSees, () => Ids(engine).Length == 3), "LibRed did not read ACE's row.");
                int[] ids = Ids(engine);
                Assert.Equal(ids.Length, ids.Distinct().Count());
            }
            AssertOpensCleanly(path, 3);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void ACE_inserting_while_LibRed_creates_and_drops_tables_loses_nothing() => InsertBesideDdl(aceInserts: true);

    [Fact]
    public void LibRed_inserting_while_ACE_creates_and_drops_tables_loses_nothing() => InsertBesideDdl(aceInserts: false);

    private static void InsertAtOnce(bool memo)
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-alloclock-ace-");
        try
        {
            var log = new ConcurrentQueue<string>();
            string Log() => string.Join(Environment.NewLine, log);
            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                var engine = new QueryEngine(database);
                engine.ExecuteNonQuery("CREATE TABLE T (Id AUTOINCREMENT PRIMARY KEY, Who TEXT(10), A MEMO)");

                using OleDbConnection ace = AceTestDatabase.Open(path);
                Assert.Equal(0, Count(ace));
                var clock = Stopwatch.StartNew();

                Exception? libRedFailure = null;
                var libRed = new Thread(() =>
                {
                    try
                    {
                        for (int i = 1; i <= Rows; i++)
                            LibRedRetrying(log, clock, $"insert {i}", () => engine.ExecuteNonQuery(memo
                                ? $"INSERT INTO T (Who, A) VALUES ('libred', {LibRedMemo})"
                                : "INSERT INTO T (Who) VALUES ('libred')"));
                    }
                    catch (Exception e) { libRedFailure = e; }
                }) { IsBackground = true };
                libRed.Start();

                Exception? aceFailure = null;
                try
                {
                    for (int i = 1; i <= Rows; i++)
                    {
                        AceRetrying(log, clock, $"insert {i}", () => AceInsert(ace, memo));
                        AceLetsGo(i);
                    }
                    // ACE's last write may wait for its next statement (locking.md §5), which LibRed may be waiting for.
                    _ = Count(ace);
                }
                catch (Exception e) { aceFailure = e; }
                libRed.Join();
                TestContext.Current.TestOutputHelper?.WriteLine(Log());
                Assert.True(aceFailure is null && libRedFailure is null,
                    $"ACE: {aceFailure?.Message}{Environment.NewLine}LibRed: {libRedFailure}{Environment.NewLine}{Log()}");

                Assert.True(Poll(AceSees, () => Ids(engine).Length == 2 * Rows), $"LibRed reads {Ids(engine).Length} rows.");
                Assert.True(Poll(AceSees, () => Count(ace) == 2 * Rows), $"ACE reads {Count(ace)} rows.");
                int[] ids = Ids(engine);
                Assert.Equal(ids.Length, ids.Distinct().Count());
                Assert.Equal(Rows, CountOf(engine, "libred"));
                Assert.Equal(Rows, CountOf(engine, "ace"));
                // The two really interleaved: the inserting engine changes, in id order, many times.
                string[] whos = [.. engine.ExecuteQuery("SELECT Who FROM T ORDER BY Id").Rows.Select(r => (string)r[0]!)];
                int changes = whos.Zip(whos.Skip(1)).Count(p => p.First != p.Second);
                TestContext.Current.TestOutputHelper?.WriteLine($"Engine changes in id order: {changes}; took {clock.ElapsedMilliseconds} ms.");
                Assert.True(changes >= 10, $"The inserts barely interleaved: {changes} changes of engine.");
                if (memo)
                    Assert.Equal(2 * Rows, Convert.ToInt32(engine.ExecuteQuery("SELECT COUNT(*) FROM T WHERE Len(A) = 3000")
                        .Rows.Single()[0], System.Globalization.CultureInfo.InvariantCulture));
            }
            AssertOpensCleanly(path, 2 * Rows);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    private static void InsertBesideDdl(bool aceInserts)
    {
        const int cycles = 15;
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-alloclock-ace-");
        try
        {
            var log = new ConcurrentQueue<string>();
            string Log() => string.Join(Environment.NewLine, log);
            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                var engine = new QueryEngine(database);
                engine.ExecuteNonQuery("CREATE TABLE T (Id AUTOINCREMENT PRIMARY KEY, Who TEXT(10), A MEMO)");

                using OleDbConnection ace = AceTestDatabase.Open(path);
                Assert.Equal(0, Count(ace));
                var clock = Stopwatch.StartNew();

                Exception? libRedFailure = null;
                var libRed = new Thread(() =>
                {
                    try
                    {
                        if (aceInserts)
                        {
                            for (int i = 1; i <= cycles; i++)
                            {
                                LibRedRetrying(log, clock, $"create {i}", () =>
                                    engine.ExecuteNonQuery($"CREATE TABLE S{i} (Id AUTOINCREMENT PRIMARY KEY, B TEXT(50))"));
                                LibRedRetrying(log, clock, $"fill {i}", () => engine.ExecuteNonQuery($"INSERT INTO S{i} (B) VALUES ('s')"));
                                LibRedRetrying(log, clock, $"drop {i}", () => engine.ExecuteNonQuery($"DROP TABLE S{i}"));
                            }
                        }
                        else
                        {
                            for (int i = 1; i <= Rows; i++)
                                LibRedRetrying(log, clock, $"insert {i}", () =>
                                    engine.ExecuteNonQuery($"INSERT INTO T (Who, A) VALUES ('libred', {LibRedMemo})"));
                        }
                    }
                    catch (Exception e) { libRedFailure = e; }
                }) { IsBackground = true };
                libRed.Start();

                Exception? aceFailure = null;
                try
                {
                    if (aceInserts)
                    {
                        for (int i = 1; i <= Rows; i++)
                        {
                            AceRetrying(log, clock, $"insert {i}", () => AceInsert(ace, memo: true));
                            AceLetsGo(i);
                        }
                    }
                    else
                    {
                        for (int i = 1; i <= cycles; i++)
                        {
                            AceRetrying(log, clock, $"create {i}", () => Exec(ace, $"CREATE TABLE S{i} (Id AUTOINCREMENT PRIMARY KEY, B TEXT(50))"));
                            AceRetrying(log, clock, $"fill {i}", () => Exec(ace, $"INSERT INTO S{i} (B) VALUES ('s')"));
                            AceRetrying(log, clock, $"drop {i}", () => Exec(ace, $"DROP TABLE S{i}"));
                        }
                    }
                    _ = Count(ace);
                }
                catch (Exception e) { aceFailure = e; }
                libRed.Join();
                TestContext.Current.TestOutputHelper?.WriteLine(Log());
                Assert.True(aceFailure is null && libRedFailure is null,
                    $"ACE: {aceFailure?.Message}{Environment.NewLine}LibRed: {libRedFailure}{Environment.NewLine}{Log()}");

                Assert.True(Poll(AceSees, () => Ids(engine).Length == Rows), $"LibRed reads {Ids(engine).Length} rows.");
                Assert.True(Poll(AceSees, () => Count(ace) == Rows), $"ACE reads {Count(ace)} rows.");
            }
            AssertOpensCleanly(path, Rows);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // 3000 characters as LibRed's SQL writes them: Jet SQL literals stop at 255.
    private static string LibRedMemo => string.Join(" & ", Enumerable.Repeat($"'{new string('m', 250)}'", 12));

    private static void AceInsert(OleDbConnection ace, bool memo)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = "INSERT INTO T (Who, A) VALUES ('ace', ?)";
        command.Parameters.Add("a", OleDbType.LongVarWChar).Value = memo ? LongMemo : DBNull.Value;
        command.ExecuteNonQuery();
    }

    // Each side pauses after each statement, for about as long as the other's retry interval: a user that takes the
    // table's definition page again within a millisecond of letting it go wins every round against the other's
    // ~110 ms retries (locking.md §4), and the two would insert one after the other rather than at once.
    private static void Pace() => Thread.Sleep(Random.Shared.Next(30, 150));

    // An ACE statement that follows the last within about 500 ms keeps that one's page locks until it starts, letting
    // them go for well under a millisecond (locking.md §5): ACE inserting every few tens of milliseconds holds the
    // table's definition page throughout, and another user's ~100 ms retries are refused for as long as it goes on —
    // as another ACE user's are. So every tenth row ACE pauses past that deferral, and lets the other user in.
    private static void AceLetsGo(int row)
    {
        if (row % 10 == 0) Thread.Sleep(700);
    }

    private static void LibRedRetrying(ConcurrentQueue<string> log, Stopwatch clock, string what, Action statement)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                statement();
                Pace();
                return;
            }
            catch (InvalidOperationException e) when (attempt < MaxAttempts
                && (e.Message == CurrentlyLocked || e.Message.StartsWith("The database engine could not lock table", StringComparison.Ordinal)))
            {
                log.Enqueue($"{clock.ElapsedMilliseconds,6} libred {what} refused: {e.Message}");
            }
        }
    }

    private static void AceRetrying(ConcurrentQueue<string> log, Stopwatch clock, string what, Action statement)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                statement();
                Pace();
                return;
            }
            catch (OleDbException e) when (attempt < MaxAttempts && e.Errors[0].SQLState is "3218" or "3211")
            {
                log.Enqueue($"{clock.ElapsedMilliseconds,6} ace    {what} {e.Errors[0].SQLState}");
            }
        }
    }

    // Read by each engine afresh once both have closed: every row, distinct ids, and no page owned twice or both owned
    // and free.
    private static void AssertOpensCleanly(string path, int rows)
    {
        using (OleDbConnection ace = AceTestDatabase.Open(path))
        {
            Assert.Equal(rows, Count(ace));
            using OleDbCommand command = ace.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM (SELECT DISTINCT Id FROM T)";
            Assert.Equal(rows, Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
        }
        using JetDatabase reopened = JetDatabase.Open(path, readOnly: false);
        Assert.Equal(rows, Ids(new QueryEngine(reopened)).Length);
        TestDatabases.AssertPageOwnership(reopened);
    }

    private static int[] Ids(QueryEngine engine) =>
        [.. engine.ExecuteQuery("SELECT Id FROM T ORDER BY Id").Rows.Select(r => (int)r[0]!)];

    private static int CountOf(QueryEngine engine, string who) =>
        Convert.ToInt32(engine.ExecuteQuery($"SELECT COUNT(*) FROM T WHERE Who = '{who}'").Rows.Single()[0],
            System.Globalization.CultureInfo.InvariantCulture);

    private static int Count(OleDbConnection ace)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM T";
        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void Exec(OleDbConnection ace, string sql)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static bool Poll(TimeSpan limit, Func<bool> done)
    {
        var clock = Stopwatch.StartNew();
        while (true)
        {
            if (done()) return true;
            if (clock.Elapsed > limit) return false;
            Thread.Sleep(100);
        }
    }
}
