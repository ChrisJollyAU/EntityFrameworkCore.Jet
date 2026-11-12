using System.Data.OleDb;
using System.Diagnostics;
using LibRed.Core.Tests;
using Xunit;

namespace LibRed.Engine.Tests;

// LibRed and ACE writing one file at once through ACE's page locks (docs/format/locking.md §4, §5, §7): each is refused
// a page the other holds, with ACE's 3218 once the retries are spent, and goes ahead once the other lets go — on top of
// the other's change, never over it. ACE runs on the test's own thread throughout: its objects are created and used on
// one thread, and one connection per test, since ACE is unstable under connection churn.
[Collection(AceCollection.Name)]
public class PageLockAccessTests
{
    private const string CurrentlyLocked = "Could not update; currently locked.";

    // How often each side runs one statement again on a 3218 before the test gives up on it.
    private const int MaxAttempts = 10;

    // ACE refreshes another user's pages from the file on its own interval (PageTimeout, 5 s by default).
    private static readonly TimeSpan AceSees = TimeSpan.FromSeconds(7);

    private static string Northwind => Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");

    [Fact]
    public void A_page_ACE_holds_in_a_transaction_refuses_LibReds_UPDATE_until_ACE_commits()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateRows(engine, 2);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            using (OleDbTransaction transaction = ace.BeginTransaction())
            {
                Exec(ace, "UPDATE T SET A = 'ace' WHERE Id = 2", transaction);

                var wait = Stopwatch.StartNew();
                var refused = Assert.Throws<InvalidOperationException>(
                    () => engine.ExecuteNonQuery("UPDATE T SET A = 'libred' WHERE Id = 1"));
                wait.Stop();
                Assert.Equal(CurrentlyLocked, refused.Message);
                Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);

                transaction.Commit();
            }

            Assert.Equal(1, engine.ExecuteNonQuery("UPDATE T SET A = 'libred' WHERE Id = 1"));
            Assert.Equal(["libred", "ace"], Values(engine, 2));
            Assert.True(Poll(AceSees, () => Value(ace, 1) == "libred"), "ACE did not read LibRed's update.");
            Assert.Equal("ace", Value(ace, 2));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void A_page_LibRed_holds_in_a_transaction_refuses_ACEs_UPDATE_with_3218_until_LibRed_commits()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateRows(engine, 2);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            engine.ExecuteNonQuery("BEGIN TRANSACTION");
            engine.ExecuteNonQuery("UPDATE T SET A = 'libred' WHERE Id = 1");

            var refused = Assert.Throws<OleDbException>(() => Exec(ace, "UPDATE T SET A = 'ace' WHERE Id = 2"));
            Assert.Equal("3218", refused.Errors[0].SQLState);
            Assert.Contains(CurrentlyLocked, refused.Message, StringComparison.Ordinal);

            engine.ExecuteNonQuery("COMMIT");
            Exec(ace, "UPDATE T SET A = 'ace' WHERE Id = 2");

            Assert.Equal("libred", Value(ace, 1));
            Assert.Equal("ace", Value(ace, 2));
            Assert.True(Poll(AceSees, () => Values(engine, 2).SequenceEqual(["libred", "ace"])),
                "LibRed did not read ACE's update.");
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void LibReds_UPDATE_right_after_ACEs_waits_for_ACEs_deferred_write_and_loses_neither()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateRows(engine, 2);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            Assert.Equal("row1", Value(ace, 1));   // ACE has the page
            Exec(ace, "UPDATE T SET A = 'ace' WHERE Id = 2");   // returns; the write goes out about 500 ms later
            Assert.Equal(1, engine.ExecuteNonQuery("UPDATE T SET A = 'libred' WHERE Id = 1"));

            Assert.Equal(["libred", "ace"], Values(engine, 2));
            Assert.True(Poll(AceSees, () => Value(ace, 1) == "libred"), "ACE did not read LibRed's update.");
            Assert.Equal("ace", Value(ace, 2));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void ACE_and_LibRed_updating_one_table_in_turn_and_then_at_once_lose_no_update()
    {
        const int rows = 50, half = rows / 2, alternating = 10;
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            int libRedRetries = 0, aceRetries = 0;
            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                var engine = new QueryEngine(database);
                CreateRows(engine, rows);

                using OleDbConnection ace = AceTestDatabase.Open(path);
                var clock = Stopwatch.StartNew();
                var log = new System.Collections.Concurrent.ConcurrentQueue<string>();
                string Log() => string.Join(Environment.NewLine, log);
                void AceUpdate(int id)
                {
                    for (int attempt = 1; ; attempt++)
                    {
                        try
                        {
                            Exec(ace, $"UPDATE T SET A = 'ace{id}' WHERE Id = {id}");
                            log.Enqueue($"{clock.ElapsedMilliseconds,6} ace    {id} ok");
                            return;
                        }
                        catch (OleDbException e) when (e.Errors[0].SQLState == "3218" && attempt < MaxAttempts)
                        {
                            log.Enqueue($"{clock.ElapsedMilliseconds,6} ace    {id} 3218");
                            aceRetries++;
                        }
                    }
                }
                void LibRedUpdate(int id)
                {
                    for (int attempt = 1; ; attempt++)
                    {
                        try
                        {
                            Assert.Equal(1, engine.ExecuteNonQuery($"UPDATE T SET A = 'libred{id}' WHERE Id = {id}"));
                            log.Enqueue($"{clock.ElapsedMilliseconds,6} libred {id} ok");
                            return;
                        }
                        catch (InvalidOperationException e) when (e.Message == CurrentlyLocked && attempt < MaxAttempts)
                        {
                            log.Enqueue($"{clock.ElapsedMilliseconds,6} libred {id} {e.GetType().Name}");
                            libRedRetries++;
                        }
                    }
                }

                // In turn: ACE row 1, LibRed row 26, ACE row 2, ... Each ACE update is followed by an ACE read: an ACE
                // UPDATE that starts right after another user's commit keeps its page and its write until ACE's next
                // statement (measured, locking.md §5), and on this one thread LibRed waiting for it would be waiting
                // for itself.
                try
                {
                    for (int i = 1; i <= alternating; i++)
                    {
                        AceUpdate(i);
                        Assert.Equal($"ace{i}", Value(ace, i));
                        LibRedUpdate(half + i);
                    }
                }
                catch (Exception e) when (e is OleDbException or InvalidOperationException)
                {
                    Assert.Fail($"In turn: {e.Message}{Environment.NewLine}{Log()}");
                }

                // At once: LibRed on a thread of its own, ACE on this one.
                Exception? libRedFailure = null;
                var libRed = new Thread(() =>
                {
                    try
                    {
                        for (int i = alternating + 1; i <= half; i++) LibRedUpdate(half + i);
                    }
                    catch (Exception e) { libRedFailure = e; }
                });
                libRed.Start();
                Exception? aceFailure = null;
                try
                {
                    for (int i = alternating + 1; i <= half; i++) AceUpdate(i);
                    // ACE's last write goes out with its next statement (see above), which LibRed may be waiting for.
                    Assert.Equal($"ace{half}", Value(ace, half));
                }
                catch (Exception e) { aceFailure = e; }
                libRed.Join();
                TestContext.Current.TestOutputHelper?.WriteLine(Log());
                Assert.True(aceFailure is null && libRedFailure is null,
                    $"ACE: {aceFailure?.Message}{Environment.NewLine}LibRed: {libRedFailure?.Message}{Environment.NewLine}{Log()}");

                string[] expected = [.. Enumerable.Range(1, half).Select(i => $"ace{i}"),
                    .. Enumerable.Range(half + 1, half).Select(i => $"libred{i}")];
                Assert.True(Poll(AceSees, () => Values(engine, rows).SequenceEqual(expected)),
                    $"LibRed reads: {string.Join(", ", Values(engine, rows))}");
                Assert.True(Poll(AceSees, () => AceValues(ace, rows).SequenceEqual(expected)),
                    $"ACE reads: {string.Join(", ", AceValues(ace, rows))}");
            }

            // The file both wrote, read by each afresh.
            string[] all = [.. Enumerable.Range(1, half).Select(i => $"ace{i}"),
                .. Enumerable.Range(half + 1, half).Select(i => $"libred{i}")];
            using (OleDbConnection ace = AceTestDatabase.Open(path))
                Assert.Equal(all, AceValues(ace, rows));
            using (JetDatabase reopened = JetDatabase.Open(path, readOnly: false))
            {
                Assert.Equal(all, Values(new QueryEngine(reopened), rows));
                TestDatabases.AssertPageOwnership(reopened);
            }
            TestContext.Current.TestOutputHelper?.WriteLine($"Retries on 3218: LibRed {libRedRetries}, ACE {aceRetries}.");
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

    private static string? Value(OleDbConnection ace, int id)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = $"SELECT A FROM T WHERE Id = {id}";
        return (string?)command.ExecuteScalar();
    }

    private static string[] AceValues(OleDbConnection ace, int count)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = $"SELECT A FROM T WHERE Id <= {count} ORDER BY Id";
        using OleDbDataReader reader = command.ExecuteReader();
        var values = new List<string>();
        while (reader.Read()) values.Add(reader.GetString(0));
        return [.. values];
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
