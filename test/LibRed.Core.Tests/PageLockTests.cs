using System.Buffers.Binary;
using System.Diagnostics;
using LibRed.Formats;
using LibRed.IO;
using Xunit;

namespace LibRed.Core.Tests;

// A registered user takes ACE's exclusive page lock on every page it writes (docs/format/locking.md §2, §4, §5): from
// byte 1 of the page's region-2 window, 256 + its user number long, taken when the page is first written and released
// after the batch that writes it. A refused lock is retried as ACE retries it. "The other user" is a second JetLockFile
// handle as user 2, which conflicts with LibRed's own exactly as another process's would.
public class PageLockTests
{
    private const string WindowsOnly = "Lock-file locking is Windows-only for now.";
    private const string CurrentlyLocked = "Could not update; currently locked.";
    private const int OtherUser = 2;

    private static readonly JetFormatBase Format = TestDatabases.FormatOf(TestDatabases.NorthwindAccdb);

    [Fact]
    public void A_transaction_holds_each_page_it_changed_until_commit_against_both_lock_shapes()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        using JetLockFile other = OtherUserOf(copy.Path);
        Assert.Equal(1, channel.UserNumber);

        channel.BeginTransaction();
        WriteMarker(channel, 5, 0x51);
        WriteMarker(channel, 6, 0x61);

        Assert.False(ExclusiveIsFree(other, 5));
        Assert.False(SharedIsFree(other, 5));
        Assert.False(ExclusiveIsFree(other, 6));
        Assert.False(SharedIsFree(other, 6));
        Assert.True(ExclusiveIsFree(other, 7));

        // The range is ACE's for user 1: byte 1 of the window, 257 bytes.
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, 5, 1);
        Assert.False(other.TryLock(offset + length - 1, 1));
        Assert.True(other.TryLock(offset + length, 1));
        other.Unlock(offset + length, 1);
        Assert.True(other.TryLock(offset - 1, 1));
        other.Unlock(offset - 1, 1);

        channel.CommitTransaction();
        Assert.True(ExclusiveIsFree(other, 5));
        Assert.True(SharedIsFree(other, 5));
        Assert.True(ExclusiveIsFree(other, 6));
    }

    [Fact]
    public void A_rollback_lets_go_of_every_page()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        using JetLockFile other = OtherUserOf(copy.Path);

        channel.BeginTransaction();
        WriteMarker(channel, 5, 0x51);
        Assert.False(ExclusiveIsFree(other, 5));

        channel.RollbackTransaction();
        Assert.True(ExclusiveIsFree(other, 5));
        Assert.True(SharedIsFree(other, 5));
    }

    [Fact]
    public void A_savepoint_rollback_lets_go_of_the_pages_first_written_after_it_and_keeps_the_rest()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        using JetLockFile other = OtherUserOf(copy.Path);

        channel.BeginTransaction();
        WriteMarker(channel, 5, 0x51);
        Savepoint savepoint = channel.CreateSavepoint();
        WriteMarker(channel, 5, 0x52);   // first written before the savepoint
        WriteMarker(channel, 6, 0x61);   // first written after it

        channel.RollbackToSavepoint(savepoint);
        channel.ReleaseSavepoint(savepoint);
        Assert.False(ExclusiveIsFree(other, 5));
        Assert.True(ExclusiveIsFree(other, 6));

        channel.CommitTransaction();
        Assert.True(ExclusiveIsFree(other, 5));
    }

    [Fact]
    public void A_write_outside_a_transaction_leaves_no_lock_behind()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        using JetLockFile other = OtherUserOf(copy.Path);

        WriteMarker(channel, 5, 0x51);
        Assert.True(ExclusiveIsFree(other, 5));
        Assert.Equal(0x51, channel.ReadPage(5).Span[100]);
    }

    [Fact]
    public void A_page_appended_by_the_transaction_is_not_locked()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        using JetLockFile other = OtherUserOf(copy.Path);

        channel.BeginTransaction();
        int appended = channel.Allocator.Append();
        WriteMarker(channel, appended, 0x71);
        Assert.True(ExclusiveIsFree(other, appended));
        channel.CommitTransaction();
    }

    [Fact]
    public void Two_channels_of_one_process_stay_optimistic_and_the_lock_goes_when_both_let_go()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var first = PageChannel.Open(copy.Path, readOnly: false);
        using var second = PageChannel.Open(copy.Path, readOnly: false);
        using JetLockFile other = OtherUserOf(copy.Path);

        first.BeginTransaction();
        second.BeginTransaction();
        WriteMarker(first, 5, 0x51);
        var wait = Stopwatch.StartNew();
        WriteMarker(second, 5, 0x52);   // one user: no wait
        Assert.True(wait.ElapsedMilliseconds < 1000, $"The second channel waited {wait.ElapsedMilliseconds} ms.");

        first.CommitTransaction();
        Assert.False(ExclusiveIsFree(other, 5));   // the second still has it

        // As before page locks: the second's commit finds the first's change and is refused.
        var conflict = Assert.Throws<InvalidOperationException>(() => second.CommitTransaction());
        Assert.Contains("write conflict on page 5", conflict.Message, StringComparison.Ordinal);
        Assert.False(ExclusiveIsFree(other, 5));

        second.RollbackTransaction();
        Assert.True(ExclusiveIsFree(other, 5));
        Assert.Equal(0x51, first.ReadPage(5).Span[100]);
    }

    [Fact]
    public void A_page_another_user_holds_is_refused_after_ACEs_retries_and_nothing_is_left_locked()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        using JetLockFile other = OtherUserOf(copy.Path);
        HoldExclusive(other, 5);

        channel.BeginTransaction();
        WriteMarker(channel, 6, 0x61);
        var wait = Stopwatch.StartNew();
        var refused = Assert.Throws<InvalidOperationException>(() => WriteMarker(channel, 5, 0x51));
        wait.Stop();
        Assert.Equal(CurrentlyLocked, refused.Message);
        // 21 attempts, 100 ms apart: 20 waits.
        Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);

        channel.RollbackTransaction();
        ReleaseExclusive(other, 5);
        Assert.True(ExclusiveIsFree(other, 6));
        Assert.Equal(TestDatabases.ReadPage(copy.Path, 5)[100], channel.ReadPage(5).Span[100]);
    }

    [Fact]
    public void A_page_another_user_holds_only_its_shared_byte_on_is_refused_the_same_way()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        using JetLockFile other = OtherUserOf(copy.Path);
        long shared = JetLockLayout.SharedByte(JetLockRegion.WriteRead, 5, OtherUser);
        Assert.True(other.TryLock(shared, 1));

        var refused = Assert.Throws<InvalidOperationException>(() => WriteMarker(channel, 5, 0x51));
        Assert.Equal(CurrentlyLocked, refused.Message);
        other.Unlock(shared, 1);
    }

    [Fact]
    public void A_page_the_other_user_lets_go_of_during_the_retries_is_written()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        using JetLockFile other = OtherUserOf(copy.Path);
        HoldExclusive(other, 5);

        Thread release = OnOwnThread(() =>
        {
            Thread.Sleep(500);
            ReleaseExclusive(other, 5);
        });

        var wait = Stopwatch.StartNew();
        WriteMarker(channel, 5, 0x51);   // outside a transaction: lock, write, slot, unlock
        Assert.InRange(wait.ElapsedMilliseconds, 400, 1900);
        release.Join();

        Assert.Equal(0x51, TestDatabases.ReadPage(copy.Path, 5)[100]);
        Assert.True(ExclusiveIsFree(other, 5));
    }

    [Fact]
    public void A_lock_taken_after_another_users_commit_refuses_the_write_made_on_what_was_read_before()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        using JetLockFile other = OtherUserOf(copy.Path);
        HoldExclusive(other, 5);

        channel.BeginTransaction();
        byte[] read = channel.ReadPage(5).Span.ToArray();   // read while the other user holds the page

        // The other user writes the page, ends its batch with its slot, and lets go — while LibRed waits.
        Thread otherWrites = OnOwnThread(() =>
        {
            Thread.Sleep(300);
            using (var stream = new FileStream(copy.Path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                stream.Seek(5L * Format.PageSize + 200, SeekOrigin.Begin);
                stream.WriteByte(0x77);
            }
            WriteSlot(copy.Path, OtherUser, (ushort)(Slot(copy.Path, OtherUser) + 1));
            ReleaseExclusive(other, 5);
        });

        read[100] = 0x51;
        // Through the Core API nothing can run the caller again: the write made on the stale image is refused.
        var stale = Assert.Throws<StaleReadException>(() => channel.WritePage(5, read));
        Assert.Equal(CurrentlyLocked, stale.Message);
        otherWrites.Join();
        Assert.True(ExclusiveIsFree(other, 5));

        // The cache was dropped with it: the page now reads as the other user left it, and writing on that goes.
        WriteMarker(channel, 5, 0x51);
        channel.CommitTransaction();
        byte[] page = TestDatabases.ReadPage(copy.Path, 5);
        Assert.Equal(0x77, page[200]);
        Assert.Equal(0x51, page[100]);
    }

    [Fact]
    public void An_appended_zero_page_another_user_has_since_written_is_left_as_that_user_wrote_it()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        channel.BeginTransaction();
        int filler = channel.Allocator.Append();   // zeros, to keep the file contiguous below the page this user takes
        int mine = channel.Allocator.Append();
        WriteMarker(channel, mine, 0x71);

        // Meanwhile another user writes the filler's page — one of a run it reserved — and one past this user's.
        WriteAsOtherUser(copy.Path, filler, Filled(0xAB));
        WriteAsOtherUser(copy.Path, mine + 1, Filled(0xCD));
        channel.CommitTransaction();

        Assert.Equal(Filled(0xAB), TestDatabases.ReadPage(copy.Path, filler));
        Assert.Equal(0x71, TestDatabases.ReadPage(copy.Path, mine)[100]);
        Assert.Equal(Filled(0xCD), TestDatabases.ReadPage(copy.Path, mine + 1));
        Assert.Equal((long)(mine + 2) * Format.PageSize, new FileInfo(copy.Path).Length);
    }

    [Fact]
    public void Growing_the_file_to_a_page_never_cuts_off_pages_another_user_appended_past_it()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false);
        int end = channel.PageCount;
        channel.BeginTransaction();
        byte[] mine = new byte[Format.PageSize];
        mine[100] = 0x71;
        channel.WritePage(end + 3, mine);

        WriteAsOtherUser(copy.Path, end + 6, Filled(0xCD));   // another user's append, past this user's page
        channel.CommitTransaction();

        Assert.Equal((long)(end + 7) * Format.PageSize, new FileInfo(copy.Path).Length);
        Assert.Equal(Filled(0xCD), TestDatabases.ReadPage(copy.Path, end + 6));
        Assert.Equal(0x71, TestDatabases.ReadPage(copy.Path, end + 3)[100]);
        Assert.True(channel.PageCount >= end + 7);
    }

    [Fact]
    public void An_exclusive_open_takes_no_lock()
    {
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");

        using var channel = PageChannel.Open(copy.Path, readOnly: false, exclusive: true);
        Assert.Null(channel.UserNumber);
        channel.BeginTransaction();
        WriteMarker(channel, 5, 0x51);
        Assert.False(File.Exists(JetLockFile.PathFor(copy.Path)));
        channel.CommitTransaction();
    }

    [Fact]
    public void An_unregistered_channel_takes_no_lock_and_waits_for_nobody()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-pagelock-");
        // A directory where the lock file goes: the registration cannot open it and carries on unregistered, as it
        // does off Windows.
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        Directory.CreateDirectory(lockPath);
        try
        {
            using var channel = PageChannel.Open(copy.Path, readOnly: false);
            Assert.Null(channel.UserNumber);
            var wait = Stopwatch.StartNew();
            channel.BeginTransaction();
            WriteMarker(channel, 5, 0x51);
            channel.CommitTransaction();
            WriteMarker(channel, 6, 0x61);
            Assert.True(wait.ElapsedMilliseconds < 1000, $"The writes took {wait.ElapsedMilliseconds} ms.");
        }
        finally { Directory.Delete(lockPath); }
    }

    // A thread of its own rather than the pool's: a write waiting for a lock blocks its thread, and under the suite's
    // parallel load a pool work item can wait longer than ACE's retries to be started.
    private static Thread OnOwnThread(Action work)
    {
        var thread = new Thread(() => work()) { IsBackground = true };
        thread.Start();
        return thread;
    }

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

    // Whether user 2 could take its exclusive lock on the page now; taken, it is let go at once.
    private static bool ExclusiveIsFree(JetLockFile other, int page)
    {
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, page, OtherUser);
        if (!other.TryLock(offset, length)) return false;
        other.Unlock(offset, length);
        return true;
    }

    // Whether user 2 could take its shared byte on the page now.
    private static bool SharedIsFree(JetLockFile other, int page)
    {
        long shared = JetLockLayout.SharedByte(JetLockRegion.WriteRead, page, OtherUser);
        if (!other.TryLock(shared, 1)) return false;
        other.Unlock(shared, 1);
        return true;
    }

    private static void WriteMarker(PageChannel channel, int page, byte marker)
    {
        byte[] bytes = channel.ReadPage(page).Span.ToArray();
        bytes[100] = marker;
        channel.WritePage(page, bytes);
    }

    // A page written into the file while LibRed has it open, as another process's write lands.
    private static void WriteAsOtherUser(string path, int page, byte[] bytes)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        stream.Seek((long)page * Format.PageSize, SeekOrigin.Begin);
        stream.Write(bytes);
    }

    private static byte[] Filled(byte value)
    {
        byte[] page = new byte[Format.PageSize];
        page.AsSpan().Fill(value);
        return page;
    }

    private static int Slot(string path, int user) =>
        BinaryPrimitives.ReadUInt16LittleEndian(
            TestDatabases.ReadPage(path, 0).AsSpan(Format.CommitByteTableOffset + user * Format.CommitByteSlotSize));

    private static void WriteSlot(string path, int user, ushort value)
    {
        byte[] bytes = new byte[Format.CommitByteSlotSize];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        stream.Seek(Format.CommitByteTableOffset + user * Format.CommitByteSlotSize, SeekOrigin.Begin);
        stream.Write(bytes);
    }
}
