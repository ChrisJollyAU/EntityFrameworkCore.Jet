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

    // Connection control (locking.md §3): ACE's user 1 setting "Jet OLEDB:Connection Control" to 1 holds its byte of
    // group 5, 0x10000501. A newcomer refused the whole group finds that byte refused, reads user 1's record, lets its
    // own bytes go and fails with 3734 naming the record (measured). Once the bar is lifted it connects.
    [Fact]
    public void An_open_while_a_connected_user_bars_new_connections_is_refused_naming_that_user()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        long bar = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 1);

        try
        {
            using JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: true);
            Assert.True(other.TryLock(JetLockLayout.SessionByte(1), 1));
            Assert.True(other.TryLock(JetLockLayout.UserByte(1), 1));
            other.Write(0, JetLockLayout.UserRecord("OTHERMACHINE", "Admin"));
            Assert.True(other.TryLock(bar, 1));

            IOException refused = Assert.Throws<IOException>(() => JetDatabase.Open(copy.Path, readOnly: false).Dispose());
            Assert.Equal(
                "The database has been placed in a state by user 'Admin' on machine 'OTHERMACHINE' that prevents it from being opened or locked.",
                refused.Message);
            AssertUser2Free(other);

            other.Unlock(bar, 1);
            using (JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false))
                Assert.Equal(2, database.Channel.UserNumber);

            other.Unlock(JetLockLayout.UserByte(1), 1);
            other.Unlock(JetLockLayout.SessionByte(1), 1);
        }
        finally { TemporaryDatabase.Delete(lockPath); }   // the simulated user never closes as a user would
    }

    // A group-5 byte held by no connected user — here byte 7, user 1 alone connected: the newcomer finds user 1's byte
    // free, starts again from the whole group, and after ten rounds fails with 3733 (measured). The rounds do not wait:
    // ACE's refused connect took no longer than an ordinary one.
    [Fact]
    public void An_open_barred_by_no_connected_user_is_refused_by_an_unknown_user()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        long bar = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 7);

        try
        {
            using JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: true);
            Assert.True(other.TryLock(JetLockLayout.SessionByte(1), 1));
            Assert.True(other.TryLock(JetLockLayout.UserByte(1), 1));
            other.Write(0, JetLockLayout.UserRecord("OTHERMACHINE", "Admin"));
            Assert.True(other.TryLock(bar, 1));

            IOException refused = Assert.Throws<IOException>(() => JetDatabase.Open(copy.Path, readOnly: false).Dispose());
            Assert.Equal(
                "The database has been placed in a state by an unknown user that prevents it from being opened or locked.",
                refused.Message);
            AssertUser2Free(other);

            other.Unlock(bar, 1);
            other.Unlock(JetLockLayout.UserByte(1), 1);
            other.Unlock(JetLockLayout.SessionByte(1), 1);
        }
        finally { TemporaryDatabase.Delete(lockPath); }
    }

    // Byte 0 of group 6 held alone bars nothing: the newcomer is granted group 5 and never tries group 6 (measured).
    [Fact]
    public void Group_6_byte_0_held_alone_does_not_stop_an_open()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        long followUp = JetLockLayout.UserGroupStart(JetLockLayout.ConnectionControlFollowUpGroup);

        try
        {
            using JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: true);
            Assert.True(other.TryLock(JetLockLayout.SessionByte(1), 1));
            Assert.True(other.TryLock(JetLockLayout.UserByte(1), 1));
            Assert.True(other.TryLock(followUp, 1));

            using (JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false))
                Assert.Equal(2, database.Channel.UserNumber);

            other.Unlock(followUp, 1);
            other.Unlock(JetLockLayout.UserByte(1), 1);
            other.Unlock(JetLockLayout.SessionByte(1), 1);
        }
        finally { TemporaryDatabase.Delete(lockPath); }
    }

    // Group 6 byte 0 held with user 1's group-5 byte: the newcomer, refused byte 0, tries its own and user 1's group-6
    // bytes instead of group 5's, and after ten rounds fails with 3733 — not 3734, though the user barring connections
    // is connected (measured).
    [Fact]
    public void With_group_6_byte_0_held_a_bar_by_a_connected_user_is_refused_as_an_unknown_users()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        long followUp = JetLockLayout.UserGroupStart(JetLockLayout.ConnectionControlFollowUpGroup);
        long bar = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 1);

        try
        {
            using JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: true);
            Assert.True(other.TryLock(JetLockLayout.SessionByte(1), 1));
            Assert.True(other.TryLock(JetLockLayout.UserByte(1), 1));
            other.Write(0, JetLockLayout.UserRecord("OTHERMACHINE", "Admin"));
            Assert.True(other.TryLock(followUp, 1));
            Assert.True(other.TryLock(bar, 1));

            IOException refused = Assert.Throws<IOException>(() => JetDatabase.Open(copy.Path, readOnly: false).Dispose());
            Assert.Equal(
                "The database has been placed in a state by an unknown user that prevents it from being opened or locked.",
                refused.Message);
            AssertUser2Free(other);

            other.Unlock(bar, 1);
            other.Unlock(followUp, 1);
            other.Unlock(JetLockLayout.UserByte(1), 1);
            other.Unlock(JetLockLayout.SessionByte(1), 1);
        }
        finally { TemporaryDatabase.Delete(lockPath); }
    }

    // Group 6 byte 0 refused: the newcomer tries the group-6 byte of every user number with a record, highest down,
    // its own included, letting each granted one go; another user's refused byte is the refusal, 3734 naming that user
    // from its record, even though group 5 was never walked (measured).
    [Fact]
    public void With_group_6_byte_0_and_a_connected_users_group_6_byte_held_the_open_is_refused_naming_that_user()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        long followUp = JetLockLayout.UserGroupStart(JetLockLayout.ConnectionControlFollowUpGroup);
        long followUpUser1 = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlFollowUpGroup, 1);
        long bar = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 1);

        try
        {
            using JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: true);
            Assert.True(other.TryLock(JetLockLayout.SessionByte(1), 1));
            Assert.True(other.TryLock(JetLockLayout.UserByte(1), 1));
            other.Write(0, JetLockLayout.UserRecord("OTHERMACHINE", "Admin"));
            Assert.True(other.TryLock(followUp, 1));
            Assert.True(other.TryLock(followUpUser1, 1));
            Assert.True(other.TryLock(bar, 1));

            IOException refused = Assert.Throws<IOException>(() => JetDatabase.Open(copy.Path, readOnly: false).Dispose());
            Assert.Equal(
                "The database has been placed in a state by user 'Admin' on machine 'OTHERMACHINE' that prevents it from being opened or locked.",
                refused.Message);

            other.Unlock(bar, 1);
            other.Unlock(followUpUser1, 1);
            other.Unlock(followUp, 1);
            AssertUser2Free(other);
            other.Unlock(JetLockLayout.UserByte(1), 1);
            other.Unlock(JetLockLayout.SessionByte(1), 1);
        }
        finally { TemporaryDatabase.Delete(lockPath); }
    }

    // Group 6 byte 0 refused and the newcomer's own group-6 byte refused too: the walk of that round ends there, naming
    // no one, and after ten rounds the refusal is 3733 (measured).
    [Fact]
    public void With_group_6_byte_0_and_the_newcomers_own_group_6_byte_held_the_open_is_refused_by_an_unknown_user()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        long followUp = JetLockLayout.UserGroupStart(JetLockLayout.ConnectionControlFollowUpGroup);
        long followUpUser2 = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlFollowUpGroup, 2);
        long bar = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 1);

        try
        {
            using JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: true);
            Assert.True(other.TryLock(JetLockLayout.SessionByte(1), 1));
            Assert.True(other.TryLock(JetLockLayout.UserByte(1), 1));
            other.Write(0, JetLockLayout.UserRecord("OTHERMACHINE", "Admin"));
            Assert.True(other.TryLock(followUp, 1));
            Assert.True(other.TryLock(followUpUser2, 1));
            Assert.True(other.TryLock(bar, 1));

            IOException refused = Assert.Throws<IOException>(() => JetDatabase.Open(copy.Path, readOnly: false).Dispose());
            Assert.Equal(
                "The database has been placed in a state by an unknown user that prevents it from being opened or locked.",
                refused.Message);

            other.Unlock(bar, 1);
            other.Unlock(followUpUser2, 1);
            other.Unlock(followUp, 1);
            AssertUser2Free(other);
            other.Unlock(JetLockLayout.UserByte(1), 1);
            other.Unlock(JetLockLayout.SessionByte(1), 1);
        }
        finally { TemporaryDatabase.Delete(lockPath); }
    }

    // Barring new connections (locking.md §3), as ACE's "Jet OLEDB:Connection Control" set to 1 does, a read-only
    // connection's included (measured): user 1 locks and lets go the whole of group 5, then holds its own byte of it,
    // 0x10000501; another open is refused with 3734 naming this machine's record. Set back, the byte is let go and the
    // next open connects.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Barring_new_connections_holds_the_users_group_5_byte_and_refuses_another_open(bool readOnly)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        long group5 = JetLockLayout.UserGroupStart(JetLockLayout.ConnectionControlGroup);
        long bar = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 1);

        using (JetDatabase database = JetDatabase.Open(copy.Path, readOnly))
        {
            Assert.False(database.BarsNewConnections);
            database.BarsNewConnections = true;
            Assert.True(database.BarsNewConnections);
            using (JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: false))
            {
                Assert.False(other.TryLock(group5, JetLockLayout.UserGroupSize));
                Assert.False(other.TryLock(bar, 1));
                Assert.True(other.TryLock(JetLockLayout.UserGroupStart(JetLockLayout.ConnectionControlFollowUpGroup), 1));
                other.Unlock(JetLockLayout.UserGroupStart(JetLockLayout.ConnectionControlFollowUpGroup), 1);
            }

            IOException refused = Assert.Throws<IOException>(() => JetDatabase.Open(copy.Path, readOnly: false).Dispose());
            Assert.Equal(
                $"The database has been placed in a state by user 'Admin' on machine '{Environment.MachineName}' that prevents it from being opened or locked.",
                refused.Message);
            Assert.NotNull(database.OpenTable("Customers"));   // its own statements go on

            database.BarsNewConnections = false;
            Assert.False(database.BarsNewConnections);
            using (JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: false))
            {
                Assert.True(other.TryLock(group5, JetLockLayout.UserGroupSize));
                other.Unlock(group5, JetLockLayout.UserGroupSize);
            }
            using (JetDatabase next = JetDatabase.Open(copy.Path, readOnly: false))
                Assert.Equal(2, next.Channel.UserNumber);
        }

        Assert.False(File.Exists(lockPath));
    }

    // ACE's user barring twice holds its byte until it has set the property back twice, although the property reads 2
    // after the first; an admit holding nothing does nothing (measured).
    [Fact]
    public void Barring_twice_holds_the_bar_until_admitted_twice()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        long bar = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 1);

        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        using JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: false);
        database.BarsNewConnections = false;
        Assert.True(other.TryLock(bar, 1));
        other.Unlock(bar, 1);

        database.BarsNewConnections = true;
        database.BarsNewConnections = true;
        database.BarsNewConnections = false;
        Assert.False(database.BarsNewConnections);
        Assert.False(other.TryLock(bar, 1));
        Assert.Throws<IOException>(() => JetDatabase.Open(copy.Path, readOnly: false).Dispose());

        database.BarsNewConnections = false;
        Assert.True(other.TryLock(bar, 1));
        other.Unlock(bar, 1);
    }

    // Closing while barring lets the byte go, however many times it barred: the bar never outlives the user (measured).
    // Another user stays connected, so the lock file stays to be probed.
    [Fact]
    public void Closing_while_barring_new_connections_lets_the_bar_go()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        long bar = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 1);

        try
        {
            using JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: true);
            Assert.True(other.TryLock(JetLockLayout.SessionByte(2), 1));
            Assert.True(other.TryLock(JetLockLayout.UserByte(2), 1));

            using (JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false))
            {
                Assert.Equal(1, database.Channel.UserNumber);
                database.BarsNewConnections = true;
                database.BarsNewConnections = true;
                Assert.False(other.TryLock(bar, 1));
            }

            Assert.True(other.TryLock(JetLockLayout.UserGroupStart(JetLockLayout.ConnectionControlGroup), JetLockLayout.UserGroupSize));
            other.Unlock(JetLockLayout.UserGroupStart(JetLockLayout.ConnectionControlGroup), JetLockLayout.UserGroupSize);
            Assert.True(other.TryLock(JetLockLayout.UserByte(1), 1));
            other.Unlock(JetLockLayout.UserByte(1), 1);
            other.Unlock(JetLockLayout.UserByte(2), 1);
            other.Unlock(JetLockLayout.SessionByte(2), 1);
        }
        finally { TemporaryDatabase.Delete(lockPath); }   // the simulated user never closes as a user would
    }

    // A second user barring while the first bars: refused the whole group, it finds the first's byte refused and stops,
    // holding nothing and raising nothing, and its property reads as set. When the first lets its bar go, a newcomer
    // connects although the second's still reads as set; barring again then holds its own byte (measured).
    [Fact]
    public void A_second_user_barring_holds_nothing_yet_reads_as_barring()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        long bar1 = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 1);
        long bar2 = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 2);

        using JetDatabase first = JetDatabase.Open(copy.Path, readOnly: false);
        using JetDatabase second = JetDatabase.Open(copy.Path, readOnly: false);
        Assert.Equal(2, second.Channel.UserNumber);
        using JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: false);

        first.BarsNewConnections = true;
        second.BarsNewConnections = true;
        Assert.True(second.BarsNewConnections);
        Assert.False(other.TryLock(bar1, 1));
        Assert.True(other.TryLock(bar2, 1));
        other.Unlock(bar2, 1);

        first.BarsNewConnections = false;
        Assert.True(second.BarsNewConnections);
        using (JetDatabase newcomer = JetDatabase.Open(copy.Path))
            Assert.Equal(3, newcomer.Channel.UserNumber);

        second.BarsNewConnections = true;
        Assert.False(other.TryLock(bar2, 1));
        Assert.Throws<IOException>(() => JetDatabase.Open(copy.Path).Dispose());
        second.BarsNewConnections = false;
        Assert.True(other.TryLock(bar2, 1));
        other.Unlock(bar2, 1);
    }

    // A group-5 byte held by no connected user: the setter is refused the whole group and finds no holder, and holds
    // nothing; the property reads as set (measured).
    [Fact]
    public void Barring_while_a_byte_of_group_5_is_held_by_no_connected_user_holds_nothing()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;
        long stray = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 7);
        long bar = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 1);

        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        using JetLockFile other = JetLockFile.Open(lockPath, createIfMissing: false);
        Assert.True(other.TryLock(stray, 1));
        database.BarsNewConnections = true;
        Assert.True(database.BarsNewConnections);
        Assert.True(other.TryLock(bar, 1));
        other.Unlock(bar, 1);
        other.Unlock(stray, 1);
    }

    // ACE's exclusive connection takes the property and reads it back, with no error and no lock file (measured).
    [Fact]
    public void An_exclusive_open_takes_the_bar_and_holds_nothing()
    {
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-register-");
        string lockPath = JetLockFile.PathFor(copy.Path)!;

        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false, exclusive: true);
        database.BarsNewConnections = true;
        Assert.True(database.BarsNewConnections);
        Assert.False(File.Exists(lockPath));
        database.BarsNewConnections = false;
        Assert.False(database.BarsNewConnections);
    }

    // Nothing of a refused open stays locked: the bytes it took as user 2, and every group-5 and group-6 byte it tried.
    private static void AssertUser2Free(JetLockFile other)
    {
        foreach (long offset in (long[])[
                     JetLockLayout.UserByte(2), JetLockLayout.SessionByte(2),
                     JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 2),
                     JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlFollowUpGroup, 2),
                     JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlFollowUpGroup, 1)])
        {
            Assert.True(other.TryLock(offset, 1));
            other.Unlock(offset, 1);
        }
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
