using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using LibRed.Core.Tests;
using LibRed.IO;
using Xunit;

namespace LibRed.Engine.Tests;

// Statements and ACE's page locks (docs/format/locking.md §4, §5, §7): a writing statement locks each page it changes,
// waits as ACE waits for a page another user holds, and — when it gets the lock only after that user committed — runs
// again on the fresh pages rather than writing over the other user's change. "The other user" is a second JetLockFile
// handle as user 2, which conflicts with LibRed's own exactly as another process's would.
public class PageLockStatementTests
{
    private const string WindowsOnly = "Lock-file locking is Windows-only for now.";
    private const string CurrentlyLocked = "Could not update; currently locked.";
    private const string CouldNotRead = "Could not read the record; currently locked by another user.";
    private const string CouldNotSave = "Could not save; currently locked by another user.";
    private const int OtherUser = 2;
    private const string RowOne = "AAAAAAAAAAAA";
    private const string RowTwo = "ZQXJZQXJZQXJ";

    private static string Northwind => Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");

    [Fact]
    public void An_UPDATE_of_a_page_another_user_holds_fails_after_the_retries()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        int page = CreateTwoRows(engine, database);

        using JetLockFile other = OtherUserOf(copy.Path);
        HoldExclusive(other, page);
        var wait = Stopwatch.StartNew();
        var refused = Assert.Throws<InvalidOperationException>(
            () => engine.ExecuteNonQuery($"UPDATE T SET A = 'BBBBBBBBBBBB' WHERE Id = 1"));
        wait.Stop();
        Assert.Equal(CurrentlyLocked, refused.Message);
        Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);
        ReleaseExclusive(other, page);

        AssertNothingLocked(other, copy.Path);
        Assert.Equal(RowOne, ValueOf(engine, 1));
    }

    // Two connections of one process are two Jet users, as ACE's are. ACE, two OLE DB connections in one process
    // (measured, an AutoNumber table with a unique index): the first's INSERT in a transaction draws 1; the second's
    // draws 2 and is refused with 3218 after the retries, 2.2 s or more; after the first's COMMIT the second's INSERT
    // goes ahead and draws 3.
    [Fact]
    public void Another_connections_INSERT_beside_an_open_transaction_is_refused_at_the_statement_and_goes_ahead_after_COMMIT()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase firstDb = JetDatabase.Open(copy.Path, readOnly: false);
        using JetDatabase secondDb = JetDatabase.Open(copy.Path, readOnly: false);
        var first = new QueryEngine(firstDb);
        var second = new QueryEngine(secondDb);
        first.ExecuteNonQuery("CREATE TABLE N (Id COUNTER PRIMARY KEY, K TEXT(30))");
        first.ExecuteNonQuery("CREATE UNIQUE INDEX UxK ON N (K)");
        Assert.Equal((1, 2), (firstDb.Channel.UserNumber, secondDb.Channel.UserNumber));
        int Identity(QueryEngine engine) => Convert.ToInt32(engine.ExecuteQuery("SELECT @@IDENTITY").Rows.Single()[0]);

        first.ExecuteNonQuery("BEGIN TRANSACTION");
        first.ExecuteNonQuery("INSERT INTO N (K) VALUES ('first')");
        Assert.Equal(1, Identity(first));

        var wait = Stopwatch.StartNew();
        var refused = Assert.Throws<InvalidOperationException>(() => second.ExecuteNonQuery("INSERT INTO N (K) VALUES ('second')"));
        wait.Stop();
        Assert.Equal(CurrentlyLocked, refused.Message);
        Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);
        // The first's uncommitted row is not the second's to read, and reading waits for nothing.
        Assert.Empty(second.ExecuteQuery("SELECT Id FROM N").Rows.ToList());

        first.ExecuteNonQuery("COMMIT");
        Assert.Equal(1, second.ExecuteNonQuery("INSERT INTO N (K) VALUES ('second')"));
        Assert.Equal(3, Identity(second));
        Assert.Equal([1, 3], first.ExecuteQuery("SELECT Id FROM N ORDER BY Id").Rows.Select(r => Convert.ToInt32(r[0])).ToArray());

        // Each wrote its own commit slot, and nothing is left locked.
        using JetLockFile other = OtherUserOf(copy.Path);
        AssertNothingLocked(other, copy.Path);
    }

    [Fact]
    public void An_UPDATE_waiting_for_a_page_goes_ahead_once_the_other_user_lets_go()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        int page = CreateTwoRows(engine, database);

        using JetLockFile other = OtherUserOf(copy.Path);
        HoldExclusive(other, page);
        Thread release = OnOwnThread(() =>
        {
            Thread.Sleep(500);
            ReleaseExclusive(other, page);
        });

        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE T SET A = 'BBBBBBBBBBBB' WHERE Id = 1"));
        release.Join();
        Assert.Equal("BBBBBBBBBBBB", ValueOf(engine, 1));
        AssertNothingLocked(other, copy.Path);
    }

    [Fact]
    public void An_UPDATE_that_gets_the_page_after_the_other_users_commit_runs_again_and_keeps_both_changes()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        int page = CreateTwoRows(engine, database);

        // The other user has changed the page and holds it until its deferred write goes out.
        using JetLockFile other = OtherUserOf(copy.Path);
        HoldExclusive(other, page);
        int affected = -1;
        Thread update = OnOwnThread(() => affected = engine.ExecuteNonQuery("UPDATE T SET A = 'BBBBBBBBBBBB' WHERE Id = 1"));

        // LibRed has read the page and is waiting for it. The other user's write lands: row 2, in place, on the page
        // LibRed read; then its slot; then it lets go.
        Thread.Sleep(400);
        Assert.True(update.IsAlive);
        RewriteInFile(copy.Path, RowTwo, "YQXJZQXJZQXJ");
        WriteSlot(copy.Path, OtherUser, (ushort)(Slot(copy.Path, OtherUser) + 1));
        ReleaseExclusive(other, page);

        update.Join();
        Assert.Equal(1, affected);
        Assert.Equal("BBBBBBBBBBBB", ValueOf(engine, 1));
        Assert.Equal("YQXJZQXJZQXJ", ValueOf(engine, 2));
        AssertNothingLocked(other, copy.Path);

        // And so the file says, to a reader that has never seen it. The other user goes first: its handle on the lock
        // file is what stops LibRed's last close deleting it, and nothing else would.
        other.Dispose();
        database.Dispose();
        using JetDatabase fresh = JetDatabase.Open(copy.Path);
        var reader = new QueryEngine(fresh);
        Assert.Equal("BBBBBBBBBBBB", ValueOf(reader, 1));
        Assert.Equal("YQXJZQXJZQXJ", ValueOf(reader, 2));
    }

    [Fact]
    public void An_autocommit_statement_and_a_failed_one_leave_no_lock_behind()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        CreateTwoRows(engine, database);
        using JetLockFile other = OtherUserOf(copy.Path);

        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE T SET A = 'BBBBBBBBBBBB' WHERE Id = 1"));
        AssertNothingLocked(other, copy.Path);

        Assert.ThrowsAny<Exception>(() => engine.ExecuteNonQuery("UPDATE T SET Id = 2, A = 'CCCCCCCCCCCC' WHERE Id = 1"));
        AssertNothingLocked(other, copy.Path);
        Assert.Equal("BBBBBBBBBBBB", ValueOf(engine, 1));
    }

    [Fact]
    public void A_transaction_holds_its_pages_until_COMMIT_and_a_failed_statement_in_it_keeps_the_earlier_ones()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        int page = CreateTwoRows(engine, database);
        using JetLockFile other = OtherUserOf(copy.Path);

        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        engine.ExecuteNonQuery("UPDATE T SET A = 'BBBBBBBBBBBB' WHERE Id = 1");
        Assert.False(ExclusiveIsFree(other, page));
        Assert.False(SharedIsFree(other, page));

        Assert.ThrowsAny<Exception>(() => engine.ExecuteNonQuery("UPDATE T SET Id = 2 WHERE Id = 1"));
        Assert.False(ExclusiveIsFree(other, page));

        engine.ExecuteNonQuery("COMMIT");
        AssertNothingLocked(other, copy.Path);
        Assert.Equal("BBBBBBBBBBBB", ValueOf(engine, 1));
    }

    // A child row's foreign-key check takes ACE's shared byte on the parent's index page holding the key, and holds it as
    // the statement's page locks are held: to COMMIT or ROLLBACK in a transaction, to the statement's end on its own.
    // Another user's DELETE or INSERT of a parent row there needs the page exclusively, and is refused meanwhile. ACE 16,
    // CapParent/CapChild (measured): the INSERT and the foreign-key UPDATE take the parent's table-read byte, then their
    // shared byte on the parent's primary-key index page, held to COMMIT / ROLLBACK; another user's DELETE of either
    // parent row is refused that page exclusively, 21 attempts, then 3218.
    [Fact]
    public void A_child_rows_foreign_key_check_holds_the_parents_index_page_shared_until_the_transaction_ends()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        engine.ExecuteNonQuery("CREATE TABLE P (Id INTEGER PRIMARY KEY)");
        engine.ExecuteNonQuery("CREATE TABLE C (Id INTEGER PRIMARY KEY, P INTEGER REFERENCES P (Id))");
        engine.ExecuteNonQuery("INSERT INTO P (Id) VALUES (1)");
        engine.ExecuteNonQuery("INSERT INTO P (Id) VALUES (2)");
        int leaf = database.Catalog.FindTable("P")!.Indexes.First(i => i.IsPrimaryKey).RootPage;
        long libRedsByte = JetLockLayout.SharedByte(JetLockRegion.WriteRead, leaf, database.Channel.UserNumber!.Value);
        using JetLockFile other = OtherUserOf(copy.Path);

        // On its own, for the statement alone.
        engine.ExecuteNonQuery("INSERT INTO C (Id, P) VALUES (10, 1)");
        AssertNothingLocked(other, copy.Path);

        foreach (string end in (string[])["COMMIT", "ROLLBACK"])
        {
            engine.ExecuteNonQuery("BEGIN TRANSACTION");
            engine.ExecuteNonQuery("INSERT INTO C (Id, P) VALUES (11, 2)");
            Assert.False(other.TryLock(libRedsByte, 1));
            Assert.True(SharedIsFree(other, leaf));        // another reader is not in its way
            Assert.False(ExclusiveIsFree(other, leaf));    // another user's write of the page is
            // The connection's own write of the page goes ahead on top of it, and the page is held still.
            engine.ExecuteNonQuery($"INSERT INTO P (Id) VALUES ({(end == "COMMIT" ? 3 : 4)})");
            Assert.False(ExclusiveIsFree(other, leaf));
            engine.ExecuteNonQuery(end);
            AssertNothingLocked(other, copy.Path);
            if (end == "COMMIT") engine.ExecuteNonQuery("DELETE FROM C WHERE Id = 11");
        }

        // A foreign-key UPDATE takes it too.
        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE C SET P = 2 WHERE Id = 10"));
        Assert.False(other.TryLock(libRedsByte, 1));
        engine.ExecuteNonQuery("ROLLBACK");
        AssertNothingLocked(other, copy.Path);
    }

    // A chained memo is read under the reader's shared byte on the chain's first page, held for the statement and let go
    // when it returns — inside an explicit transaction too (locking.md §4, §5).
    [Fact]
    public void A_statement_reading_a_chained_memo_holds_its_first_pages_shared_byte_until_it_returns()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int first, int[] rest, _) = CreateMemos(engine, copy.Path);
        using JetLockFile other = OtherUserOf(copy.Path);

        database.ReadConsistent(() =>
        {
            Assert.Equal(Chained, (string?)engine.ExecuteQuery("SELECT Notes FROM M WHERE Id = 2").Rows.Single()[0]);
            Assert.False(ExclusiveIsFree(other, first));
            Assert.True(SharedIsFree(other, first));
            foreach (int page in rest) Assert.True(ExclusiveIsFree(other, page));
            return 0;
        });
        AssertNothingLocked(other, copy.Path);

        // A query's rows read after it returned hold it for the read alone.
        Assert.Equal(Chained, (string?)engine.ExecuteQuery("SELECT Notes FROM M WHERE Id = 2").Rows.Single()[0]);
        AssertNothingLocked(other, copy.Path);

        // Inside a transaction it goes when the statement returns, where the page the statement wrote is held to COMMIT.
        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE M SET Tag = 1 WHERE Notes = @p",
            new Dictionary<string, object?> { ["@p"] = Chained }));
        Assert.True(ExclusiveIsFree(other, first));
        Assert.False(AllFree(other, copy.Path));
        engine.ExecuteNonQuery("COMMIT");
        AssertNothingLocked(other, copy.Path);
    }

    [Fact]
    public void A_chained_memo_whose_first_page_another_user_holds_is_refused_with_3624_after_the_retries()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int first, int[] rest, int single) = CreateMemos(engine, copy.Path);
        using JetLockFile other = OtherUserOf(copy.Path);

        HoldExclusive(other, first);
        var wait = Stopwatch.StartNew();
        var refused = Assert.Throws<InvalidOperationException>(
            () => engine.ExecuteQuery("SELECT Notes FROM M WHERE Id = 2").Rows.Single());
        wait.Stop();
        Assert.Equal(CouldNotRead, refused.Message);
        // ACE retries a refused long-value read three rounds of 21, back to back, and fails after about 6.5 s (measured
        // on this table and value).
        Assert.InRange(wait.ElapsedMilliseconds, 6000, 15_000);
        ReleaseExclusive(other, first);

        // The chain's other pages, a value on a single page and the data page holding the rows are read with no lock.
        int dataPage = database.ReadConsistent(() => database.OpenTable("M").Rows().WithIds().Select(r => r.Id.Page).Distinct().Single());
        foreach (int page in (int[])[.. rest, single, dataPage])
        {
            HoldExclusive(other, page);
            var quick = Stopwatch.StartNew();
            Assert.Equal([OnePage, Chained], engine.ExecuteQuery("SELECT Notes FROM M ORDER BY Id").Rows.Select(r => (string?)r[0]));
            Assert.True(quick.ElapsedMilliseconds < 1000, $"Reading with page {page} held took {quick.ElapsedMilliseconds} ms.");
            ReleaseExclusive(other, page);
        }
        AssertNothingLocked(other, copy.Path);
    }

    [Fact]
    public void A_chained_memo_the_other_user_lets_go_of_during_the_retries_is_read()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int first, _, _) = CreateMemos(engine, copy.Path);
        using JetLockFile other = OtherUserOf(copy.Path);

        HoldExclusive(other, first);
        Thread release = OnOwnThread(() =>
        {
            Thread.Sleep(500);
            ReleaseExclusive(other, first);
        });
        var wait = Stopwatch.StartNew();
        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE M SET Tag = 1 WHERE Notes = @p",
            new Dictionary<string, object?> { ["@p"] = Chained }));
        Assert.InRange(wait.ElapsedMilliseconds, 400, 1900);
        release.Join();
        AssertNothingLocked(other, copy.Path);
    }

    // An UPDATE replacing a chained memo locks the old chain's first page exclusively, and another user reading the
    // value — its shared byte on that page — refuses it with ACE's 3202 once the retries are spent (measured).
    [Fact]
    public void An_UPDATE_replacing_a_chained_memo_another_user_is_reading_fails_with_3202_after_the_retries()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int first, _, _) = CreateMemos(engine, copy.Path);
        using JetLockFile other = OtherUserOf(copy.Path);

        long reading = JetLockLayout.SharedByte(JetLockRegion.WriteRead, first, OtherUser);
        Assert.True(other.TryLock(reading, 1));
        var wait = Stopwatch.StartNew();
        var refused = Assert.Throws<InvalidOperationException>(() => engine.ExecuteNonQuery(
            "UPDATE M SET Notes = @p WHERE Id = 2", new Dictionary<string, object?> { ["@p"] = OnePage + "x" }));
        wait.Stop();
        Assert.Equal(CouldNotSave, refused.Message);
        Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);

        // Another of the row's columns is no reason to stop.
        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE M SET Tag = 2 WHERE Id = 2"));
        other.Unlock(reading, 1);
        Assert.Equal(Chained, (string?)engine.ExecuteQuery("SELECT Notes FROM M WHERE Id = 2").Rows.Single()[0]);

        // Let go, it goes ahead; the lock goes when the statement returns, inside a transaction too.
        engine.ExecuteNonQuery("BEGIN TRANSACTION");
        Assert.Equal(1, engine.ExecuteNonQuery("UPDATE M SET Notes = @p WHERE Id = 2",
            new Dictionary<string, object?> { ["@p"] = OnePage + "x" }));
        Assert.True(ExclusiveIsFree(other, first));
        engine.ExecuteNonQuery("COMMIT");
        AssertNothingLocked(other, copy.Path);
        Assert.Equal(OnePage + "x", (string?)engine.ExecuteQuery("SELECT Notes FROM M WHERE Id = 2").Rows.Single()[0]);
    }

    // A DELETE whose chained memo another user is reading waits out the retries and deletes the row anyway, leaving the
    // chain where it is: still allocated, still the column's — as ACE's DELETE does (measured, byte for byte, in
    // PageLockAccessTests).
    [Fact]
    public void A_DELETE_of_a_row_whose_chained_memo_another_user_is_reading_waits_and_deletes_it_leaving_the_chain()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(Northwind, "libred-stmtlock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        var engine = new QueryEngine(database);
        (int first, int[] rest, _) = CreateMemos(engine, copy.Path);
        using JetLockFile other = OtherUserOf(copy.Path);

        long reading = JetLockLayout.SharedByte(JetLockRegion.WriteRead, first, OtherUser);
        Assert.True(other.TryLock(reading, 1));
        var wait = Stopwatch.StartNew();
        Assert.Equal(1, engine.ExecuteNonQuery("DELETE FROM M WHERE Id = 2"));
        wait.Stop();
        Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);
        other.Unlock(reading, 1);
        AssertNothingLocked(other, copy.Path);
        Assert.Empty(engine.ExecuteQuery("SELECT Id FROM M WHERE Id = 2").Rows.ToList());

        // The chain's pages are still the column's, and none of them is free.
        other.Dispose();
        database.Dispose();
        using JetDatabase reopened = JetDatabase.Open(copy.Path);
        TestDatabases.AssertPageOwnership(reopened);
        Catalog.TableDefinition table = reopened.Catalog.FindTable("M")!;
        PageChannel channel = reopened.Channel;
        var maps = new Storage.UsageMap(channel, table);
        (int Row, int Page) owned = table.LongValueOwnedMaps.Single().Value;
        HashSet<int> ownedPages = [.. maps.PagesInMap(owned.Row, owned.Page)];
        (_, _, byte[] holder, Pages.DataPage.RowSlot slot) = TestDatabases.GlobalMap(channel, channel.Format.FreePagesMapPointerOffset);
        HashSet<int> free = [.. Storage.UsageMap.PagesInRecord(channel, holder.AsSpan(slot.Offset, slot.Length), null, "free")];
        foreach (int page in (int[])[first, .. rest])
        {
            Assert.Contains(page, ownedPages);
            Assert.DoesNotContain(page, free);
        }
    }

    // A memo stored on a single page and one chained over three; text no compression shortens, so the chain's first page
    // is the one holding its first character.
    private static readonly string OnePage = new('丁', 500);
    private static readonly string Chained = new string('币', 1000) + new string('市', 4000);

    // CREATE TABLE M with the two memos as rows 1 and 2; returns the chain's first page, its other pages and the single page.
    private static (int First, int[] Others, int Single) CreateMemos(QueryEngine engine, string path)
    {
        engine.ExecuteNonQuery("CREATE TABLE M (Id INTEGER PRIMARY KEY, Tag INTEGER, Notes MEMO)");
        engine.ExecuteNonQuery("INSERT INTO M (Id, Notes) VALUES (1, @p)", new Dictionary<string, object?> { ["@p"] = OnePage });
        engine.ExecuteNonQuery("INSERT INTO M (Id, Notes) VALUES (2, @p)", new Dictionary<string, object?> { ["@p"] = Chained });

        int pageSize = PageSize(path);
        byte[] file;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            file = new byte[stream.Length];
            stream.ReadExactly(file);
        }
        int[] Holding(char c) => [.. Enumerable.Range(1, file.Length / pageSize - 1).Where(p =>
            file.AsSpan(p * pageSize, pageSize).IndexOf(Encoding.Unicode.GetBytes(new string(c, 2))) >= 0)];
        int first = Assert.Single(Holding('币'));
        int[] rest = [.. Holding('市').Where(p => p != first)];
        Assert.Equal(2, rest.Length);
        return (first, rest, Assert.Single(Holding('丁')));
    }

    // CREATE TABLE T and two rows, which share a data page; returns that page.
    private static int CreateTwoRows(QueryEngine engine, JetDatabase database)
    {
        engine.ExecuteNonQuery("CREATE TABLE T (Id INTEGER PRIMARY KEY, A TEXT(20))");
        engine.ExecuteNonQuery($"INSERT INTO T (Id, A) VALUES (1, '{RowOne}')");
        engine.ExecuteNonQuery($"INSERT INTO T (Id, A) VALUES (2, '{RowTwo}')");
        return database.ReadConsistent(() =>
            database.OpenTable("T").Rows().WithIds().Select(r => r.Id.Page).Distinct().Single());
    }

    // A thread of its own rather than the pool's: a statement waiting for a lock blocks its thread, and under the
    // suite's parallel load a pool work item can wait longer than ACE's retries to be started.
    private static Thread OnOwnThread(Action work)
    {
        var thread = new Thread(() => work()) { IsBackground = true };
        thread.Start();
        return thread;
    }

    private static string? ValueOf(QueryEngine engine, int id) =>
        (string?)engine.ExecuteQuery($"SELECT A FROM T WHERE Id = {id}").Rows.Single()[0];

    private static JetLockFile OtherUserOf(string databasePath) =>
        JetLockFile.Open(JetLockFile.PathFor(databasePath)!, createIfMissing: false);

    private static void HoldExclusive(JetLockFile other, int page)
    {
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, page, OtherUser);
        Assert.True(other.TryLock(offset, length));
    }

    private static void ReleaseExclusive(JetLockFile other, int page)
    {
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, page, OtherUser);
        other.Unlock(offset, length);
    }

    private static bool ExclusiveIsFree(JetLockFile other, int page)
    {
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, page, OtherUser);
        if (!other.TryLock(offset, length)) return false;
        other.Unlock(offset, length);
        return true;
    }

    private static bool SharedIsFree(JetLockFile other, int page)
    {
        long shared = JetLockLayout.SharedByte(JetLockRegion.WriteRead, page, OtherUser);
        if (!other.TryLock(shared, 1)) return false;
        other.Unlock(shared, 1);
        return true;
    }

    // No page of the file is locked by LibRed: user 2 can take its exclusive lock on every one.
    private static void AssertNothingLocked(JetLockFile other, string path)
    {
        int pageSize = PageSize(path);
        long pages = new FileInfo(path).Length / pageSize;
        for (int page = 0; page < pages; page++)
            Assert.True(ExclusiveIsFree(other, page), $"Page {page} is still locked.");
    }

    private static bool AllFree(JetLockFile other, string path)
    {
        long pages = new FileInfo(path).Length / PageSize(path);
        for (int page = 0; page < pages; page++)
            if (!ExclusiveIsFree(other, page)) return false;
        return true;
    }

    private static int PageSize(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Formats.JetFormatBase.Detect(stream).PageSize;
    }

    private static int SlotOffset(string path, int user)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Formats.JetFormatBase format = Formats.JetFormatBase.Detect(stream);
        return format.CommitByteTableOffset + user * format.CommitByteSlotSize;
    }

    private static int Slot(string path, int user)
    {
        byte[] slot = new byte[sizeof(ushort)];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(SlotOffset(path, user), SeekOrigin.Begin);
        stream.ReadExactly(slot);
        return BinaryPrimitives.ReadUInt16LittleEndian(slot);
    }

    private static void WriteSlot(string path, int user, ushort value)
    {
        byte[] bytes = new byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        int offset = SlotOffset(path, user);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        stream.Seek(offset, SeekOrigin.Begin);
        stream.Write(bytes);
    }

    // Replaces the text where it is stored in the file, in whichever of its two encodings it was written — what
    // another user's write of that row in place leaves.
    private static void RewriteInFile(string path, string from, string to)
    {
        byte[] file;
        using (var read = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            file = new byte[read.Length];
            read.ReadExactly(file);
        }
        foreach (Encoding encoding in new Encoding[] { Encoding.Latin1, Encoding.Unicode })
        {
            int at = file.AsSpan().IndexOf(encoding.GetBytes(from));
            if (at < 0) continue;
            Assert.Equal(-1, file.AsSpan(at + 1).IndexOf(encoding.GetBytes(from)));
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            stream.Seek(at, SeekOrigin.Begin);
            stream.Write(encoding.GetBytes(to));
            return;
        }
        Assert.Fail($"'{from}' is not in the file.");
    }
}
