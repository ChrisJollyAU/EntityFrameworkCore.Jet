using System.Data.OleDb;
using System.Diagnostics;
using LibRed.Core.Tests;
using Xunit;

namespace LibRed.Engine.Tests;

// LibRed and ACE sharing one file through ACE's table locks (docs/format/locking.md §4, §5, §7): a design change waits
// for, and is refused by, the other engine having the table in use — ACE's 3211 either way — goes ahead once it lets
// go, and the other engine sees the new design on its next statement. ACE runs on the test's own thread throughout:
// its objects are created and used on one thread, and one connection per test, since ACE is unstable under connection
// churn.
[Collection(AceCollection.Name)]
public class TableLockAccessTests
{
    private const string CurrentlyLocked = "Could not update; currently locked.";

    // How often each side runs one statement again on a 3211 or 3218 before the test gives up on it.
    private const int MaxAttempts = 10;

    // ACE refreshes another user's pages from the file on its own interval (PageTimeout, 5 s by default).
    private static readonly TimeSpan AceSees = TimeSpan.FromSeconds(7);

    private static string Northwind => Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");

    private static string InUse(string table) =>
        $"The database engine could not lock table '{table}' because it is already in use by another person or process.";

    [Fact]
    public void A_table_ACE_has_in_a_transaction_refuses_LibReds_design_change_with_3211_until_ACE_commits()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-tablelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateRows(engine, 2);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            string[] designChanges = ["ALTER TABLE T ADD COLUMN C INTEGER", "CREATE INDEX IxA ON T (A)"];
            using (OleDbTransaction transaction = ace.BeginTransaction())
            {
                Exec(ace, "UPDATE T SET A = 'ace' WHERE Id = 2", transaction);
                foreach (string ddl in designChanges)
                {
                    var wait = Stopwatch.StartNew();
                    var refused = Assert.Throws<InvalidOperationException>(() => engine.ExecuteNonQuery(ddl));
                    wait.Stop();
                    Assert.Equal(InUse("T"), refused.Message);
                    Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);
                }
                transaction.Commit();
            }

            engine.ExecuteNonQuery(designChanges[0]);
            Assert.True(Poll(AceSees, () => AceColumns(ace).SequenceEqual(["Id", "A", "C"])),
                $"ACE reads the columns {string.Join(", ", AceColumns(ace))}.");
            Assert.Equal("ace", Value(ace, 2));
            // ACE is not asked to read T after this: LibRed's CREATE INDEX appends the index's root page, and ACE then
            // refuses every read of T with 3159 "Not a valid bookmark" until its 5-second refresh (measured; after ACE's
            // own CREATE INDEX from another process it reads T at once). The cause is not known; allocating without
            // ACE's allocation lock is the suspect (verification-backlog §1).
            engine.ExecuteNonQuery(designChanges[1]);
            Assert.Contains(database.Catalog.FindTable("T")!.Indexes, i => i.Name == "IxA");
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void A_table_LibRed_has_in_a_transaction_refuses_ACEs_ALTER_TABLE_with_3211_until_LibRed_commits()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-tablelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateRows(engine, 2);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            engine.ExecuteNonQuery("BEGIN TRANSACTION");
            engine.ExecuteNonQuery("UPDATE T SET A = 'libred' WHERE Id = 1");

            var wait = Stopwatch.StartNew();
            var refused = Assert.Throws<OleDbException>(() => Exec(ace, "ALTER TABLE T ADD COLUMN C INTEGER"));
            wait.Stop();
            Assert.Equal("3211", refused.Errors[0].SQLState);
            Assert.Contains(InUse("T"), refused.Message, StringComparison.Ordinal);
            TestContext.Current.TestOutputHelper?.WriteLine($"ACE refused after {wait.ElapsedMilliseconds} ms.");

            engine.ExecuteNonQuery("COMMIT");
            Exec(ace, "ALTER TABLE T ADD COLUMN C INTEGER");
            Assert.Equal("libred", Value(ace, 1));

            Assert.True(Poll(AceSees, () => engine.ExecuteQuery("SELECT * FROM T").ColumnNames.SequenceEqual(["Id", "A", "C"])),
                $"LibRed reads the columns {string.Join(", ", engine.ExecuteQuery("SELECT * FROM T").ColumnNames)}.");
            Assert.Equal(["libred", "row2"], Values(engine, 2));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void A_table_one_engine_creates_and_drops_appears_and_disappears_for_the_other()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-tablelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateRows(engine, 1);
            using OleDbConnection ace = AceTestDatabase.Open(path);
            Assert.Equal("row1", Value(ace, 1));

            engine.ExecuteNonQuery("CREATE TABLE N (Id INTEGER)");
            engine.ExecuteNonQuery("INSERT INTO N (Id) VALUES (7)");
            Assert.True(Poll(AceSees, () => AceCount(ace, "N") == 1), "ACE does not see LibRed's new table.");
            engine.ExecuteNonQuery("DROP TABLE N");
            Assert.True(Poll(AceSees, () => AceCount(ace, "N") is null), "ACE still sees LibRed's dropped table.");

            // ACE's turn. Each ACE statement is followed by an ACE read: an autocommit statement can keep its write
            // until ACE's next statement (locking.md §5).
            Exec(ace, "CREATE TABLE M (Id INTEGER)");
            Exec(ace, "INSERT INTO M (Id) VALUES (8)");
            Assert.Equal(1, AceCount(ace, "M"));
            Assert.True(Poll(AceSees, () => LibRedCount(engine, "M") == 1), "LibRed does not see ACE's new table.");
            Exec(ace, "DROP TABLE M");
            Assert.Null(AceCount(ace, "M"));
            Assert.True(Poll(AceSees, () => LibRedCount(engine, "M") is null), "LibRed still sees ACE's dropped table.");
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void ACE_reading_a_table_while_LibRed_adds_its_columns_fails_nothing_and_both_end_agreeing()
    {
        const int columns = 20;
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-tablelock-ace-");
        try
        {
            int libRedRetries = 0, aceRetries = 0, aceReads = 0;
            string[] expected = ["Id", "A", .. Enumerable.Range(1, columns).Select(i => $"C{i}")];
            var clock = Stopwatch.StartNew();
            var log = new System.Collections.Concurrent.ConcurrentQueue<string>();
            string Log() => string.Join(Environment.NewLine, log);

            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                var engine = new QueryEngine(database);
                CreateRows(engine, 2);
                using OleDbConnection ace = AceTestDatabase.Open(path);
                Assert.Equal(2, AceCount(ace, "T"));

                // LibRed on a thread of its own, ACE on this one.
                Exception? libRedFailure = null;
                var libRed = new Thread(() =>
                {
                    try
                    {
                        for (int i = 1; i <= columns; i++)
                            for (int attempt = 1; ; attempt++)
                            {
                                try
                                {
                                    engine.ExecuteNonQuery($"ALTER TABLE T ADD COLUMN C{i} INTEGER");
                                    log.Enqueue($"{clock.ElapsedMilliseconds,6} libred C{i} ok");
                                    Thread.Sleep(50);   // so ACE's reads fall between the changes as well as on them
                                    break;
                                }
                                catch (InvalidOperationException e) when ((e.Message == InUse("T") || e.Message == CurrentlyLocked) && attempt < MaxAttempts)
                                {
                                    log.Enqueue($"{clock.ElapsedMilliseconds,6} libred C{i} {e.Message}");
                                    Interlocked.Increment(ref libRedRetries);
                                }
                            }
                    }
                    catch (Exception e) { libRedFailure = e; }
                });
                libRed.Start();

                Exception? aceFailure = null;
                try
                {
                    int attempt = 1;
                    while (libRed.IsAlive)
                    {
                        try
                        {
                            int read = AceColumns(ace).Length;
                            aceReads++;
                            attempt = 1;
                            log.Enqueue($"{clock.ElapsedMilliseconds,6} ace    {read} columns");
                        }
                        catch (OleDbException e) when (e.Errors[0].SQLState is "3211" or "3218" && attempt++ < MaxAttempts)
                        {
                            log.Enqueue($"{clock.ElapsedMilliseconds,6} ace    {e.Errors[0].SQLState}");
                            aceRetries++;
                        }
                    }
                }
                catch (Exception e) { aceFailure = e; }
                libRed.Join();
                TestContext.Current.TestOutputHelper?.WriteLine(Log());
                Assert.True(aceFailure is null && libRedFailure is null,
                    $"ACE: {(aceFailure as OleDbException)?.Errors[0].SQLState} {aceFailure?.Message}{Environment.NewLine}"
                    + $"LibRed: {libRedFailure?.Message}{Environment.NewLine}{Log()}");

                Assert.Equal(expected, engine.ExecuteQuery("SELECT * FROM T").ColumnNames);
                Assert.True(Poll(AceSees, () => AceColumns(ace).SequenceEqual(expected)),
                    $"ACE reads the columns {string.Join(", ", AceColumns(ace))}.");
                Assert.Equal(2, LibRedCount(engine, "T"));
                Assert.Equal(2, AceCount(ace, "T"));
            }

            // The file both used, read by each afresh.
            using (OleDbConnection ace = AceTestDatabase.Open(path))
                Assert.Equal(expected, AceColumns(ace));
            using (JetDatabase reopened = JetDatabase.Open(path, readOnly: false))
            {
                Assert.Equal(expected, new QueryEngine(reopened).ExecuteQuery("SELECT * FROM T").ColumnNames);
                TestDatabases.AssertPageOwnership(reopened);
            }
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"ACE reads: {aceReads}. Retries on 3211/3218: LibRed {libRedRetries}, ACE {aceRetries}.");
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A LibRed query whose rows are being read holds the table, as ACE's open reader does (measured): ACE's ALTER TABLE
    // is refused with its own 3211 until the enumeration is disposed, and then succeeds.
    [Fact]
    public void A_LibRed_result_being_read_refuses_ACEs_ALTER_TABLE_with_3211_until_it_is_disposed()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-tablelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateRows(engine, 50);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            Assert.Equal("row1", Value(ace, 1));
            using (IEnumerator<object?[]> rows = engine.ExecuteQuery("SELECT A FROM T ORDER BY Id").Rows.GetEnumerator())
            {
                for (int i = 0; i < 10; i++) Assert.True(rows.MoveNext());
                var refused = Assert.Throws<OleDbException>(() => Exec(ace, "ALTER TABLE T ADD COLUMN C INTEGER"));
                Assert.Equal("3211", refused.Errors[0].SQLState);
                Assert.Contains(InUse("T"), refused.Message, StringComparison.Ordinal);
                Assert.True(rows.MoveNext());
            }

            Exec(ace, "ALTER TABLE T ADD COLUMN C INTEGER");
            Assert.True(Poll(AceSees, () => engine.ExecuteQuery("SELECT * FROM T").Rows.ToList() is { Count: 50 }
                && engine.ExecuteQuery("SELECT * FROM T").ColumnNames.SequenceEqual(["Id", "A", "C"])),
                "LibRed does not read ACE's new column.");
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ACE's open reader holds the table until it is closed, whether or not every row has been fetched (measured), and
    // LibRed's design change is refused meanwhile with 3211, and goes ahead once it is closed.
    [Fact]
    public void An_open_ACE_reader_refuses_LibReds_ALTER_TABLE_with_3211_until_it_is_closed()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-tablelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateRows(engine, 50);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            using (OleDbCommand command = ace.CreateCommand())
            {
                command.CommandText = "SELECT A FROM T";
                using OleDbDataReader reader = command.ExecuteReader();
                Assert.True(reader.Read());
                var wait = Stopwatch.StartNew();
                var refused = Assert.Throws<InvalidOperationException>(() => engine.ExecuteNonQuery("ALTER TABLE T ADD COLUMN C INTEGER"));
                Assert.Equal(InUse("T"), refused.Message);
                Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);
                while (reader.Read()) { }
            }

            engine.ExecuteNonQuery("ALTER TABLE T ADD COLUMN C INTEGER");
            Assert.True(Poll(AceSees, () => AceColumns(ace).SequenceEqual(["Id", "A", "C"])),
                $"ACE reads the columns {string.Join(", ", AceColumns(ace))}.");
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    private static void CreateRows(QueryEngine engine, int count)
    {
        engine.ExecuteNonQuery("CREATE TABLE T (Id INTEGER PRIMARY KEY, A TEXT(20))");
        for (int id = 1; id <= count; id++)
            engine.ExecuteNonQuery($"INSERT INTO T (Id, A) VALUES ({id}, 'row{id}')");
    }

    private static string[] Values(QueryEngine engine, int count) =>
        [.. engine.ExecuteQuery($"SELECT A FROM T WHERE Id <= {count} ORDER BY Id").Rows.Select(r => (string)r[0]!)];

    // The table's row count as LibRed reads it, or null when it has no such table.
    private static int? LibRedCount(QueryEngine engine, string table)
    {
        try { return Convert.ToInt32(engine.ExecuteQuery($"SELECT COUNT(*) FROM {table}").Rows.Single()[0], System.Globalization.CultureInfo.InvariantCulture); }
        catch (LibRed.Sql.Binding.SqlBindException) { return null; }
    }

    private static string? Value(OleDbConnection ace, int id)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = $"SELECT A FROM T WHERE Id = {id}";
        return (string?)command.ExecuteScalar();
    }

    // The table's row count as ACE reads it, or null when ACE has no such table: 3078, "cannot find the input table or
    // query", or 3011, "could not find the object" — which ACE gives for a table dropped since its catalog last read it
    // (measured, after another user's DROP TABLE).
    private static int? AceCount(OleDbConnection ace, string table)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        try { return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture); }
        catch (OleDbException e) when (e.Errors[0].SQLState is "3078" or "3011") { return null; }
    }

    // T's columns as ACE reads them, reading every row.
    private static string[] AceColumns(OleDbConnection ace)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = "SELECT * FROM T";
        using OleDbDataReader reader = command.ExecuteReader();
        while (reader.Read()) { }
        return [.. Enumerable.Range(0, reader.FieldCount).Select(reader.GetName)];
    }

    private static void Exec(OleDbConnection ace, string sql, OleDbTransaction? transaction = null)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
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
