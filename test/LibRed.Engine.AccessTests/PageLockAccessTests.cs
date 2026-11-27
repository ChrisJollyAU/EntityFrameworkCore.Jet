using System.Data.OleDb;
using System.Diagnostics;
using LibRed.Core.Tests;
using LibRed.IO;
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

    // A chained memo: ACE's UPDATE of it locks the old chain's first page exclusively and lets it go about 500 ms after
    // the statement, inside a transaction too (measured); LibRed's read waits for it, and reads the value committed.
    [Fact]
    public void LibReds_read_of_a_chained_memo_ACE_is_updating_waits_for_the_chain_and_reads_the_committed_value()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateMemo(engine);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            using (OleDbTransaction transaction = ace.BeginTransaction())
            {
                UpdateMemo(ace, AceMemo, transaction);
                var wait = Stopwatch.StartNew();
                Assert.Equal(LibRedMemo, Memo(engine));
                wait.Stop();
                TestContext.Current.TestOutputHelper?.WriteLine($"LibRed's read waited {wait.ElapsedMilliseconds} ms.");
                Assert.InRange(wait.ElapsedMilliseconds, 100, 1900);
                transaction.Commit();
            }

            Assert.True(Poll(AceSees, () => Memo(engine) == AceMemo), "LibRed did not read ACE's update.");
            Assert.Equal(AceMemo, AceMemoValue(ace));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // LibRed holds its shared byte on the chain's first page for its statement, and ACE's UPDATE of the memo is refused
    // it with ACE's own 3202 once its retries are spent, as against another ACE reader (measured).
    [Fact]
    public void ACEs_UPDATE_of_a_chained_memo_LibRed_is_reading_is_refused_with_3202_and_goes_ahead_after()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateMemo(engine);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            database.ReadConsistent(() =>
            {
                Assert.Equal(LibRedMemo, Memo(engine));
                var refused = Assert.Throws<OleDbException>(() => UpdateMemo(ace, AceMemo));
                Assert.Equal("3202", refused.Errors[0].SQLState);
                return 0;
            });

            UpdateMemo(ace, AceMemo);
            Assert.Equal(AceMemo, AceMemoValue(ace));
            Assert.True(Poll(AceSees, () => Memo(engine) == AceMemo), "LibRed did not read ACE's update.");
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ACE's read of a chained memo keeps its shared byte on the chain's first page about 500 ms after the statement
    // (measured); LibRed's UPDATE replacing the memo locks that page exclusively, so it waits for ACE to let go, and then
    // goes ahead.
    [Fact]
    public void LibReds_UPDATE_of_a_chained_memo_ACE_has_just_read_waits_for_ACEs_read_lock_and_goes_ahead()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateMemo(engine);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            Assert.Equal(LibRedMemo, AceMemoValue(ace));
            var wait = Stopwatch.StartNew();
            Assert.Equal(1, engine.ExecuteNonQuery("UPDATE M SET Notes = @p WHERE Id = 1",
                new Dictionary<string, object?> { ["@p"] = AceMemo }));
            wait.Stop();
            TestContext.Current.TestOutputHelper?.WriteLine($"LibRed's UPDATE waited {wait.ElapsedMilliseconds} ms.");
            Assert.InRange(wait.ElapsedMilliseconds, 100, 1900);

            Assert.Equal(AceMemo, Memo(engine));
            Assert.True(Poll(AceSees, () => AceMemoValue(ace) == AceMemo), "ACE did not read LibRed's update.");
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A DELETE whose row's chained memo another user is reading waits out the retries and deletes the row, leaving the
    // chain allocated and the column's (measured): LibRed's leaves the file byte for byte as ACE's does.
    [Fact]
    public void LibReds_and_ACEs_DELETE_of_a_chained_memo_another_user_is_reading_leave_the_same_file()
    {
        string seed = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        string byAce = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        string byLibRed = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            string chained = new string('币', 1000) + new string('市', 4000);   // its first page alone holds 币
            using (JetDatabase database = JetDatabase.Open(seed, readOnly: false))
            {
                var engine = new QueryEngine(database);
                engine.ExecuteNonQuery("CREATE TABLE M (Id INTEGER PRIMARY KEY, Tag INTEGER, Notes MEMO)");
                engine.ExecuteNonQuery("INSERT INTO M (Id, Tag, Notes) VALUES (1, 0, @p)", new Dictionary<string, object?> { ["@p"] = chained });
                engine.ExecuteNonQuery("INSERT INTO M (Id, Tag, Notes) VALUES (2, 0, 'x')");
            }
            File.Copy(seed, byAce, overwrite: true);
            File.Copy(seed, byLibRed, overwrite: true);
            int first = ChainFirstPage(seed, '币');

            using (OleDbConnection ace = AceTestDatabase.Open(byAce))
            {
                Assert.Equal("x", (string?)Scalar(ace, "SELECT Notes FROM M WHERE Id = 2"));
                Thread.Sleep(1000);   // ACE's own locks of that read are gone
                using JetLockFile other = JetLockFile.Open(JetLockFile.PathFor(byAce)!, createIfMissing: false);
                long reading = JetLockLayout.SharedByte(JetLockRegion.WriteRead, first, OtherUser);
                Assert.True(other.TryLock(reading, 1));
                var wait = Stopwatch.StartNew();
                Exec(ace, "DELETE FROM M WHERE Id = 1");
                Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);
                other.Unlock(reading, 1);
                Assert.Equal(1, Convert.ToInt32(Scalar(ace, "SELECT COUNT(*) FROM M"), System.Globalization.CultureInfo.InvariantCulture));
                Thread.Sleep(1000);
            }

            using (JetDatabase database = JetDatabase.Open(byLibRed, readOnly: false))
            {
                using JetLockFile other = JetLockFile.Open(JetLockFile.PathFor(byLibRed)!, createIfMissing: false);
                long reading = JetLockLayout.SharedByte(JetLockRegion.WriteRead, first, OtherUser);
                Assert.True(other.TryLock(reading, 1));
                var wait = Stopwatch.StartNew();
                Assert.Equal(1, new QueryEngine(database).ExecuteNonQuery("DELETE FROM M WHERE Id = 1"));
                Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);
                other.Unlock(reading, 1);
            }
            AceTestDatabase.ReleaseAbandonedComObjects();

            byte[] aceFile = File.ReadAllBytes(byAce), libRedFile = File.ReadAllBytes(byLibRed);
            Assert.Equal(aceFile.Length, libRedFile.Length);
            int[] differing = [.. Enumerable.Range(0, aceFile.Length).Where(i => aceFile[i] != libRedFile[i])];
            Assert.True(differing.Length == 0,
                $"Bytes differ at {string.Join(", ", differing.Take(20).Select(i => $"page {i / 4096} 0x{i % 4096:X3}"))}.");
        }
        finally
        {
            TemporaryDatabase.Delete(seed);
            TemporaryDatabase.Delete(byAce);
            TemporaryDatabase.Delete(byLibRed);
        }
    }

    // A child row's foreign-key check holds the shared byte on the parent's index page holding the key until COMMIT, as
    // ACE's does (measured), so ACE's DELETE of the parent — which writes that page — is refused with 3218 meanwhile, and
    // after the COMMIT with 3200, the child row found.
    [Fact]
    public void LibReds_child_INSERT_in_a_transaction_refuses_ACEs_parent_DELETE_with_3218_and_after_COMMIT_3200()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateParentsAndChildren(engine);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            Assert.Equal(2, Convert.ToInt32(Scalar(ace, "SELECT COUNT(*) FROM Parents")));
            engine.ExecuteNonQuery("BEGIN TRANSACTION");
            engine.ExecuteNonQuery("INSERT INTO Children (Id, ParentId) VALUES (10, 1)");

            var wait = Stopwatch.StartNew();
            var refused = Assert.Throws<OleDbException>(() => Exec(ace, "DELETE FROM Parents WHERE Id = 1"));
            wait.Stop();
            Assert.Equal("3218", refused.Errors[0].SQLState);
            Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);

            engine.ExecuteNonQuery("COMMIT");
            var related = Assert.Throws<OleDbException>(() => Exec(ace, "DELETE FROM Parents WHERE Id = 1"));
            Assert.Equal("3200", related.Errors[0].SQLState);
            Assert.Equal(2, Convert.ToInt32(Scalar(ace, "SELECT COUNT(*) FROM Parents")));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void ACEs_child_INSERT_in_a_transaction_refuses_LibReds_parent_DELETE_with_3218_and_after_COMMIT_3200()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateParentsAndChildren(engine);

            using OleDbConnection ace = AceTestDatabase.Open(path);
            using (OleDbTransaction transaction = ace.BeginTransaction())
            {
                Exec(ace, "INSERT INTO Children (Id, ParentId) VALUES (10, 1)", transaction);

                var wait = Stopwatch.StartNew();
                var refused = Assert.Throws<InvalidOperationException>(() => engine.ExecuteNonQuery("DELETE FROM Parents WHERE Id = 1"));
                wait.Stop();
                Assert.Equal(CurrentlyLocked, refused.Message);
                Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);

                transaction.Commit();
            }

            var related = Assert.Throws<InvalidOperationException>(() => engine.ExecuteNonQuery("DELETE FROM Parents WHERE Id = 1"));
            Assert.Equal("The record cannot be deleted or changed because table 'Children' includes related records.", related.Message);
            Assert.Equal(2, Convert.ToInt32(engine.ExecuteQuery("SELECT COUNT(*) FROM Parents").Rows.Single()[0]));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // MSysObjects' definition page (docs/format/locking.md §4). An ACE session that has created a table locks it in every
    // INSERT, but goes on at once when another user holds it: beside LibRed's open transaction that created a table —
    // which holds the page to COMMIT — ACE's INSERT neither waits nor fails, while its UPDATE of a page LibRed holds is
    // refused as ever.
    [Fact]
    public void ACEs_switched_on_INSERT_beside_LibRed_holding_the_catalog_definition_page_goes_ahead_at_once()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateRows(engine, 2);
            int objects = database.Catalog.FindTable("MSysObjects")!.DefinitionPage;

            using OleDbConnection ace = AceTestDatabase.Open(path);
            Exec(ace, "CREATE TABLE AceMade (Id LONG)");
            Exec(ace, "INSERT INTO AceMade (Id) VALUES (1)");
            Thread.Sleep(1000);   // ACE's deferred write of that INSERT has gone out

            engine.ExecuteNonQuery("BEGIN TRANSACTION");
            engine.ExecuteNonQuery("CREATE TABLE LibRedMade (Id LONG)");
            using (JetLockFile probe = JetLockFile.Open(JetLockFile.PathFor(path)!, createIfMissing: false))
                Assert.False(probe.TryLock(JetLockLayout.SharedByte(JetLockRegion.WriteRead, objects, OtherUser), 1));

            var wait = Stopwatch.StartNew();
            Exec(ace, "INSERT INTO AceMade (Id) VALUES (2)");
            wait.Stop();
            Assert.InRange(wait.ElapsedMilliseconds, 0, 1000);

            engine.ExecuteNonQuery("COMMIT");
            Assert.Equal(2, Convert.ToInt32(Scalar(ace, "SELECT COUNT(*) FROM AceMade"), System.Globalization.CultureInfo.InvariantCulture));
            Assert.True(Poll(AceSees, () => Convert.ToInt32(engine.ExecuteQuery("SELECT COUNT(*) FROM AceMade").Rows.Single()[0],
                System.Globalization.CultureInfo.InvariantCulture) == 2), "LibRed did not read ACE's rows.");
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // The other way round: ACE's INSERT in a session that has created a table holds the page about 500 ms after it
    // returns (measured), and LibRed's INSERT in a session that has created one, made meanwhile, goes on without it at
    // once, as ACE's does — no retries, no error — and each reads the other's row.
    [Fact]
    public void LibReds_switched_on_INSERT_beside_ACE_holding_the_catalog_definition_page_goes_ahead_at_once()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "libred-pagelock-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(database);
            CreateRows(engine, 2);
            int objects = database.Catalog.FindTable("MSysObjects")!.DefinitionPage;

            using OleDbConnection ace = AceTestDatabase.Open(path);
            Exec(ace, "CREATE TABLE AceMade (Id LONG)");
            Thread.Sleep(1000);
            Exec(ace, "INSERT INTO AceMade (Id) VALUES (1)");
            bool aceHeldIt;
            using (JetLockFile probe = JetLockFile.Open(JetLockFile.PathFor(path)!, createIfMissing: false))
            {
                long at = JetLockLayout.SharedByte(JetLockRegion.WriteRead, objects, OtherUser);
                aceHeldIt = !probe.TryLock(at, 1);
                if (!aceHeldIt) probe.Unlock(at, 1);
            }
            var wait = Stopwatch.StartNew();
            Assert.Equal(1, engine.ExecuteNonQuery("INSERT INTO T (Id, A) VALUES (3, 'libred')"));
            wait.Stop();
            TestContext.Current.TestOutputHelper?.WriteLine($"ACE held the page: {aceHeldIt}; LibRed's INSERT took {wait.ElapsedMilliseconds} ms.");
            Assert.True(aceHeldIt, "ACE's INSERT did not hold MSysObjects' definition page after it returned.");
            Assert.InRange(wait.ElapsedMilliseconds, 0, 1000);

            Assert.True(Poll(AceSees, () => Value(ace, 3) == "libred"), "ACE did not read LibRed's row.");
            Assert.True(Poll(AceSees, () => Convert.ToInt32(engine.ExecuteQuery("SELECT COUNT(*) FROM AceMade").Rows.Single()[0],
                System.Globalization.CultureInfo.InvariantCulture) == 1), "LibRed did not read ACE's row.");
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    private static void CreateParentsAndChildren(QueryEngine engine)
    {
        engine.ExecuteNonQuery("CREATE TABLE Parents (Id LONG PRIMARY KEY)");
        engine.ExecuteNonQuery("CREATE TABLE Children (Id LONG PRIMARY KEY, ParentId LONG REFERENCES Parents (Id))");
        engine.ExecuteNonQuery("INSERT INTO Parents (Id) VALUES (1)");
        engine.ExecuteNonQuery("INSERT INTO Parents (Id) VALUES (2)");
    }

    // The one page holding the start of a chain written with text no compression shortens.
    private static int ChainFirstPage(string path, char first)
    {
        byte[] file = File.ReadAllBytes(path);
        byte[] needle = System.Text.Encoding.Unicode.GetBytes(new string(first, 2));
        return Assert.Single(Enumerable.Range(1, file.Length / 4096 - 1),
            p => file.AsSpan(p * 4096, 4096).IndexOf(needle) >= 0);
    }

    private static object? Scalar(OleDbConnection ace, string sql)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private const int OtherUser = 5;

    // Values a chain holds (over two pages): text no compression shortens.
    private static readonly string LibRedMemo = new('币', 3000);
    private static readonly string AceMemo = new('市', 3000);

    private static void CreateMemo(QueryEngine engine)
    {
        engine.ExecuteNonQuery("CREATE TABLE M (Id INTEGER PRIMARY KEY, Notes MEMO)");
        engine.ExecuteNonQuery("INSERT INTO M (Id, Notes) VALUES (1, @p)", new Dictionary<string, object?> { ["@p"] = LibRedMemo });
    }

    private static string? Memo(QueryEngine engine) =>
        (string?)engine.ExecuteQuery("SELECT Notes FROM M WHERE Id = 1").Rows.Single()[0];

    private static string? AceMemoValue(OleDbConnection ace)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = "SELECT Notes FROM M WHERE Id = 1";
        return (string?)command.ExecuteScalar();
    }

    private static void UpdateMemo(OleDbConnection ace, string value, OleDbTransaction? transaction = null)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = "UPDATE M SET Notes = ? WHERE Id = 1";
        command.Transaction = transaction;
        command.Parameters.Add("notes", OleDbType.LongVarWChar).Value = value;
        command.ExecuteNonQuery();
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
