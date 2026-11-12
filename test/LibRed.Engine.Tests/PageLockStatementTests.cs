using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
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
