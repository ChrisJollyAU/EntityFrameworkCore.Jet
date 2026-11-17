using System.Text;
using LibRed.IO;
using Xunit;

namespace LibRed.Core.Tests;

// A shared open registers its channel as a Jet user of the database, as ACE does (docs/format/locking.md §1, §3): the
// lock file beside it, the user's group-0 and group-1 bytes, and its 64-byte record. "The other user" is a second
// JetLockFile handle, which conflicts with LibRed's own exactly as another process's would.
public class JetUserRegistrationTests
{
    private const string WindowsOnly = "Lock-file registration is Windows-only for now.";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_shared_open_creates_the_lock_file_holds_user_1_and_writes_its_record(bool readOnly)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;

        using (JetDatabase database = JetDatabase.Open(copy.Path, readOnly))
        {
            Assert.True(File.Exists(lockPath));
            Assert.Equal(1, database.Channel.UserNumber);
            using (JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: false))
            {
                Assert.False(other.TryLock(JetLockLayout.UserByte(1), 1));
                Assert.False(other.TryLock(JetLockLayout.SessionByte(1), 1));
            }

            byte[] records = ReadLockFile(lockPath);
            Assert.Equal(JetLockLayout.UserRecordSize, records.Length);
            Assert.Equal(MeasuredRecord(Environment.MachineName), records);
        }

        Assert.False(File.Exists(lockPath));
    }

    // ACE, two and three OLE DB connections in one process on a copy of this file (measured): each has a handle of its
    // own on the lock file, users 1, 2 and 3 are held, the file is 64 bytes a user; a read-only connection is a user
    // too. The one closed lets its number go, the file keeps its length, and the last to close deletes it.
    [Fact]
    public void Every_channel_on_the_file_in_one_process_is_a_user_of_its_own()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;

        JetDatabase first = JetDatabase.Open(copy.Path, readOnly: false);
        try
        {
            using (JetDatabase second = JetDatabase.Open(copy.Path))
            {
                Assert.Equal(1, first.Channel.UserNumber);
                Assert.Equal(2, second.Channel.UserNumber);
                byte[] records = ReadLockFile(lockPath);
                Assert.Equal(2 * JetLockLayout.UserRecordSize, records.Length);
                Assert.Equal(MeasuredRecord(Environment.MachineName), records[JetLockLayout.UserRecordSize..]);
                using JetLockFile both = JetLockFile.Open(lockPath, createIfMissing: false);
                Assert.False(both.TryLock(JetLockLayout.UserByte(2), 1));
                Assert.False(both.TryLock(JetLockLayout.SessionByte(2), 1));
            }

            // The second's close gave its own number back and left the first's alone.
            Assert.True(File.Exists(lockPath));
            Assert.Equal(2 * JetLockLayout.UserRecordSize, ReadLockFile(lockPath).Length);
            using (JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: false))
            {
                Assert.False(other.TryLock(JetLockLayout.UserByte(1), 1));
                Assert.False(other.TryLock(JetLockLayout.SessionByte(1), 1));
                Assert.True(other.TryLock(JetLockLayout.UserByte(2), 1));
                other.Unlock(JetLockLayout.UserByte(2), 1);
            }

            // The next to open takes the lowest free number again.
            using JetDatabase third = JetDatabase.Open(copy.Path, readOnly: false);
            Assert.Equal(2, third.Channel.UserNumber);
        }
        finally { first.Dispose(); }

        Assert.False(File.Exists(lockPath));
    }

    [Fact]
    public void With_user_1_taken_the_open_becomes_user_2_and_leaves_the_lock_file_to_the_other_user()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;

        using (JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: true))
        {
            Assert.True(other.TryLock(JetLockLayout.SessionByte(1), 1));
            Assert.True(other.TryLock(JetLockLayout.UserByte(1), 1));
            other.Write(0, JetLockLayout.UserRecord("OTHERMACHINE", "Admin"));

            using (JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false))
            {
                Assert.Equal(2, database.Channel.UserNumber);
                byte[] records = ReadLockFile(lockPath);
                Assert.Equal(2 * JetLockLayout.UserRecordSize, records.Length);
                Assert.Equal(MeasuredRecord("OTHERMACHINE"), records[..JetLockLayout.UserRecordSize]);
                Assert.Equal(MeasuredRecord(Environment.MachineName), records[JetLockLayout.UserRecordSize..]);
                Assert.False(other.TryLock(JetLockLayout.UserByte(2), 1));
                Assert.False(other.TryLock(JetLockLayout.SessionByte(2), 1));
            }

            // Not alone at close: the file stays, and user 2's bytes are free again.
            Assert.True(File.Exists(lockPath));
            Assert.True(other.TryLock(JetLockLayout.UserByte(2), 1));
            Assert.True(other.TryLock(JetLockLayout.SessionByte(2), 1));
            other.Unlock(JetLockLayout.UserByte(2), 1);
            other.Unlock(JetLockLayout.SessionByte(2), 1);
            other.Unlock(JetLockLayout.UserByte(1), 1);
            other.Unlock(JetLockLayout.SessionByte(1), 1);
        }

        // Alone, it is user 1 again, and the last to close deletes the file.
        using (JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false))
            Assert.Equal(1, database.Channel.UserNumber);
        Assert.False(File.Exists(lockPath));
    }

    [Fact]
    public void Closing_frees_both_bytes_and_keeps_the_file_while_another_user_is_connected()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;

        try
        {
            using JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: true);
            Assert.True(other.TryLock(JetLockLayout.SessionByte(2), 1));
            Assert.True(other.TryLock(JetLockLayout.UserByte(2), 1));

            using (JetDatabase database = JetDatabase.Open(copy.Path))
                Assert.Equal(1, database.Channel.UserNumber);

            Assert.True(File.Exists(lockPath));
            Assert.True(other.TryLock(JetLockLayout.UserByte(1), 1));
            Assert.True(other.TryLock(JetLockLayout.SessionByte(1), 1));
            other.Unlock(JetLockLayout.UserByte(1), 1);
            other.Unlock(JetLockLayout.SessionByte(1), 1);
            other.Unlock(JetLockLayout.UserByte(2), 1);
            other.Unlock(JetLockLayout.SessionByte(2), 1);
        }
        finally { TemporaryDatabase.Delete(lockPath); }   // the simulated user never closes as a user would
    }

    [Fact]
    public void A_lock_file_left_by_a_crashed_user_is_reused_and_then_deleted()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;

        // Two dead users' records, nothing locked: what a killed process leaves.
        byte[] stale = new byte[2 * JetLockLayout.UserRecordSize];
        stale.AsSpan().Fill((byte)'X');
        File.WriteAllBytes(lockPath, stale);

        using (JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false))
        {
            Assert.Equal(1, database.Channel.UserNumber);
            byte[] records = ReadLockFile(lockPath);
            Assert.Equal(MeasuredRecord(Environment.MachineName), records[..JetLockLayout.UserRecordSize]);
            Assert.Equal(stale[JetLockLayout.UserRecordSize..], records[JetLockLayout.UserRecordSize..]);
        }

        Assert.False(File.Exists(lockPath));
    }

    [Fact]
    public void An_exclusive_open_creates_no_lock_file()
    {
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;

        using (JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false, exclusive: true))
        {
            Assert.Null(database.Channel.UserNumber);
            Assert.False(File.Exists(lockPath));
        }

        Assert.False(File.Exists(lockPath));
    }

    [Fact]
    public void Off_Windows_a_shared_open_registers_nothing()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Lock-file registration is implemented on Windows.");
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");

        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        Assert.Null(database.Channel.UserNumber);
        Assert.False(File.Exists(JetLockFile.PathFor(copy.Path)));
    }

    // The record ACE 16 writes (measured, locking.md §1): the computer name and then "Admin", each in a 32-byte field,
    // NUL-terminated and padded with spaces. Spelled out byte by byte rather than through JetLockLayout.UserRecord,
    // which is what is under test.
    private static byte[] MeasuredRecord(string computerName)
    {
        static byte[] Field(string name)
        {
            byte[] field = new byte[32];
            field.AsSpan().Fill(0x20);
            Encoding.ASCII.GetBytes(name).CopyTo(field, 0);
            field[name.Length] = 0x00;
            return field;
        }

        return [.. Field(computerName), .. Field("Admin")];
    }

    // Shared for read and write: the registration holds the file open for writing.
    private static byte[] ReadLockFile(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }
}
