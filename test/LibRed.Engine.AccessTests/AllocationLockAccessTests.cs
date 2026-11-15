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

                // ACE's INSERT is refused with 3218, as it is beside another ACE user's open transaction that inserted
                // into the table: LibRed has written the AutoNumber it drew and let the definition page go, as ACE's
                // transaction does, so ACE draws the next id and is refused at the data and index pages LibRed holds to
                // COMMIT, its refused batch writing the id it drew (measured ACE beside ACE, locking.md §5).
                var refused = Assert.Throws<OleDbException>(() => Exec(ace, "INSERT INTO T (Who) VALUES ('ace')"));
                Assert.Equal("3218", refused.Errors[0].SQLState);

                engine.ExecuteNonQuery("COMMIT");
                Exec(ace, "INSERT INTO T (Who) VALUES ('ace')");
                Assert.True(Poll(AceSees, () => Count(ace) == 3), "ACE did not read every row.");
                Assert.True(Poll(AceSees, () => Ids(engine).Length == 3), "LibRed did not read ACE's row.");
                // The id ACE's refused INSERT drew is never issued, as beside an ACE transaction (measured: the other
                // user's refused INSERT drew 202 beside the transaction's 201, and the transaction's COMMIT kept it).
                Assert.Equal([1, 2, 4], Ids(engine));
            }
            AssertOpensCleanly(path, 3);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // The reverse: ACE's INSERT in its open transaction writes the AutoNumber it drew and lets the table's definition page
    // go about 500 ms after (measured, locking.md §5); LibRed's INSERT into the table then draws the next id and is refused
    // with 3218 at the data and index pages ACE holds to COMMIT, keeping the id it drew, as another ACE user's is.
    [Fact]
    public void An_INSERT_in_ACEs_open_transaction_lets_LibRed_draw_the_next_AutoNumber_and_keeps_its_INSERT_out_until_COMMIT()
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

                using (OleDbTransaction transaction = ace.BeginTransaction())
                {
                    using (OleDbCommand insert = ace.CreateCommand())
                    {
                        insert.Transaction = transaction;
                        insert.CommandText = "INSERT INTO T (Who) VALUES ('ace')";
                        insert.ExecuteNonQuery();
                        Thread.Sleep(1000);   // ACE's early write, about 500 ms after the INSERT
                        insert.CommandText = "SELECT COUNT(*) FROM T";
                        insert.ExecuteScalar();
                    }

                    var refused = Assert.Throws<InvalidOperationException>(() => engine.ExecuteNonQuery("INSERT INTO T (Who) VALUES ('libred')"));
                    Assert.Equal(CurrentlyLocked, refused.Message);
                    transaction.Commit();
                }

                Assert.True(Poll(AceSees, () => Ids(engine).Length == 2), "LibRed did not read ACE's row.");
                Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (Who) VALUES ('libred')"));
                // The id LibRed's refused INSERT drew is never issued, as another ACE user's is not.
                Assert.Equal([1, 2, 4], Ids(engine));
                Assert.Equal(["first", "ace", "libred"], Whos(engine, "T"));
                Assert.True(Poll(AceSees, () => Count(ace) == 3), "ACE did not read LibRed's row.");
            }
            AssertOpensCleanly(path, 3);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // LibRed's INSERT in an open transaction takes a new run of pages; ACE's first INSERT of its session with a long value
    // then reserves a run of its own under page 1's lock (measured: an ACE session's first such INSERT writes page 1).
    // LibRed has written page 1 and let it go as the statement returned, as ACE lets its own go about 500 ms after an
    // INSERT in a transaction, so ACE's INSERT goes ahead at once, while LibRed's transaction is still open.
    [Fact]
    public void An_INSERT_in_LibReds_open_transaction_lets_ACE_reserve_pages_for_another_table_before_COMMIT()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-alloclock-ace-");
        try
        {
            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                var engine = new QueryEngine(database);
                CreateTwoTables(engine);
                using OleDbConnection ace = AceTestDatabase.Open(path);
                Assert.Equal(1, Count(ace));

                engine.ExecuteNonQuery("BEGIN TRANSACTION");
                engine.ExecuteNonQuery("INSERT INTO T (Who, A) VALUES ('libred', @a)", RunOfPages);
                var clock = Stopwatch.StartNew();
                AceInsertInto(ace, "U");
                Assert.True(clock.ElapsedMilliseconds < 1000, $"ACE's INSERT took {clock.ElapsedMilliseconds} ms.");

                engine.ExecuteNonQuery("COMMIT");
                Assert.True(Poll(AceSees, () => Count(ace) == 2), "ACE did not read LibRed's row.");
                // ACE's write may be held until its next statement (locking.md §5): Count gave it one.
                Assert.True(Poll(AceSees, () => Count(ace) >= 0 && Whos(engine, "U").Length == 1), "LibRed did not read ACE's row.");
                Assert.Equal(["ace"], Whos(engine, "U"));
                Assert.Equal(RunOfPagesLength, MemoLength(engine, "T", "libred"));
            }
            AssertOpensCleanly(path, 2, uRows: 1);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // The reverse: ACE's INSERT in its open transaction takes a run, and lets page 1 go about 500 ms after (measured);
    // LibRed's INSERT into another table then reserves a run of its own before ACE commits.
    [Fact]
    public void An_INSERT_in_ACEs_open_transaction_lets_LibRed_reserve_pages_for_another_table_before_COMMIT()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-alloclock-ace-");
        try
        {
            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                var engine = new QueryEngine(database);
                CreateTwoTables(engine);
                using OleDbConnection ace = AceTestDatabase.Open(path);
                Assert.Equal(1, Count(ace));

                using (OleDbTransaction transaction = ace.BeginTransaction())
                {
                    AceInsertInto(ace, "T", transaction);
                    Thread.Sleep(1000);
                    var clock = Stopwatch.StartNew();
                    engine.ExecuteNonQuery("INSERT INTO U (Who, A) VALUES ('libred', @a)", RunOfPages);
                    Assert.True(clock.ElapsedMilliseconds < 1000, $"LibRed's INSERT took {clock.ElapsedMilliseconds} ms.");
                    transaction.Commit();
                }
                Assert.True(Poll(AceSees, () => Whos(engine, "T").Length == 2), "LibRed did not read ACE's row.");
                Assert.Equal(["libred"], Whos(engine, "U"));
            }
            AssertOpensCleanly(path, 2, uRows: 1);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // LibRed's transaction rolled back after its run went out early and ACE reserved and wrote pages of its own meanwhile:
    // the rollback gives back only the pages LibRed's undone INSERT took, ACE's rows stay, and the file opens cleanly
    // with every page owned once.
    [Fact]
    public void A_ROLLBACK_of_LibReds_transaction_after_ACE_inserted_meanwhile_leaves_ACEs_rows_and_pages()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-alloclock-ace-");
        try
        {
            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                var engine = new QueryEngine(database);
                CreateTwoTables(engine);
                using OleDbConnection ace = AceTestDatabase.Open(path);
                Assert.Equal(1, Count(ace));

                engine.ExecuteNonQuery("BEGIN TRANSACTION");
                engine.ExecuteNonQuery("INSERT INTO T (Who, A) VALUES ('libred', @a)", RunOfPages);
                AceInsertInto(ace, "U");
                engine.ExecuteNonQuery("ROLLBACK");
                AceInsertInto(ace, "U");
                Thread.Sleep(1000);   // ACE's last write may wait (locking.md §5)
                Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (Who, A) VALUES ('after', @a)", RunOfPages));

                // ACE's write may be held until its next statement (locking.md §5): each poll gives it one.
                Assert.True(Poll(AceSees, () => Count(ace) >= 0 && Whos(engine, "U").Length == 2), "LibRed did not read ACE's rows.");
                Assert.Equal(["first", "after"], Whos(engine, "T"));
                Assert.True(Poll(AceSees, () => Count(ace) == 2), "ACE did not read LibRed's row.");
            }
            AssertOpensCleanly(path, 2, uRows: 2);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A long value that takes more pages than one run of 32 holds, so the INSERT reserves a run of its own.
    private const int RunOfPagesLength = 160_000;

    private static Dictionary<string, object?> RunOfPages => new() { ["@a"] = new string('m', RunOfPagesLength) };

    // T with one row, and U empty, both with a long-value column.
    private static void CreateTwoTables(QueryEngine engine)
    {
        engine.ExecuteNonQuery("CREATE TABLE T (Id AUTOINCREMENT PRIMARY KEY, Who TEXT(10), A MEMO)");
        engine.ExecuteNonQuery("CREATE TABLE U (Id AUTOINCREMENT PRIMARY KEY, Who TEXT(10), A MEMO)");
        engine.ExecuteNonQuery("INSERT INTO T (Who) VALUES ('first')");
    }

    private static void AceInsertInto(OleDbConnection ace, string table, OleDbTransaction? transaction = null)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"INSERT INTO {table} (Who, A) VALUES ('ace', ?)";
        command.Parameters.Add("a", OleDbType.LongVarWChar).Value = LongMemo;
        command.ExecuteNonQuery();
    }

    private static string[] Whos(QueryEngine engine, string table) =>
        [.. engine.ExecuteQuery($"SELECT Who FROM {table} ORDER BY Id").Rows.Select(r => (string)r[0]!)];

    private static int MemoLength(QueryEngine engine, string table, string who) =>
        ((string)engine.ExecuteQuery($"SELECT A FROM {table} WHERE Who = '{who}'").Rows.Single()[0]!).Length;

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
    private static void AssertOpensCleanly(string path, int rows, int? uRows = null)
    {
        using (OleDbConnection ace = AceTestDatabase.Open(path))
        {
            Assert.Equal(rows, Count(ace));
            using OleDbCommand command = ace.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM (SELECT DISTINCT Id FROM T)";
            Assert.Equal(rows, Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
            if (uRows is { } u)
            {
                command.CommandText = "SELECT COUNT(*) FROM U";
                Assert.Equal(u, Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
            }
        }
        using JetDatabase reopened = JetDatabase.Open(path, readOnly: false);
        Assert.Equal(rows, Ids(new QueryEngine(reopened)).Length);
        if (uRows is { } us) Assert.Equal(us, Whos(new QueryEngine(reopened), "U").Length);
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
