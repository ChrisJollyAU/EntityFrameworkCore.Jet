using LibRed.IO;
using Xunit;

namespace LibRed.Core.Tests;

// The Jet/ACE lock layout (JetLockLayout) and the operating-system byte-range locks it is placed with (JetLockFile).
// The layout examples are offsets ACE 16 was measured taking, for users 1 and 2; the lock tests pin the Windows
// semantics the Jet lock scheme depends on, through two handles on one file as two users would have.
public class JetLockFileTests
{
    [Theory]
    [InlineData(0x40026C01L, 1)]
    [InlineData(0x40026C02L, 2)]
    public void A_shared_lock_is_the_one_byte_at_the_holders_user_number(long offset, int user)
    {
        JetLockAddress address = JetLockLayout.Decode(offset, 1);

        Assert.Equal(JetLockRegion.TableRead, address.Region);
        Assert.Equal(310, address.Window);
        Assert.Equal(user, address.ByteInWindow);
        Assert.Equal(user, address.User);
        Assert.Equal(offset, JetLockLayout.SharedByte(JetLockRegion.TableRead, 310, user));
    }

    [Theory]
    [InlineData(1, 257L)]
    [InlineData(2, 258L)]
    public void An_exclusive_lock_starts_at_byte_1_and_ends_where_its_holder_is_named(int user, long length)
    {
        JetLockAddress address = JetLockLayout.Decode(0x2002CA01, length);

        Assert.Equal(JetLockRegion.WriteRead, address.Region);
        Assert.Equal(357, address.Window);
        Assert.Equal(1, address.ByteInWindow);
        Assert.Equal(user, address.User);
        Assert.Equal((0x2002CA01L, length), JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, 357, user));
    }

    [Fact]
    public void The_user_lock_decodes_to_its_user_in_group_0()
    {
        JetLockAddress address = JetLockLayout.Decode(0x10000002, 1);

        Assert.Equal(JetLockRegion.User, address.Region);
        Assert.Equal(JetLockLayout.UserGroup, address.Window);
        Assert.Equal(2, address.User);
        Assert.Equal(0x10000002L, JetLockLayout.UserByte(2));
    }

    [Theory]
    [InlineData(1, 0x10000100L)]
    [InlineData(2, 0x10000101L)]
    public void The_session_byte_is_in_group_1_one_below_the_user_number(int user, long offset)
    {
        JetLockAddress address = JetLockLayout.Decode(offset, 1);

        Assert.Equal(offset, JetLockLayout.SessionByte(user));
        Assert.Equal(JetLockLayout.SessionGroup, address.Window);
        Assert.Equal(user, address.User);
    }

    [Fact]
    public void A_user_lock_that_is_not_one_byte_in_group_0_or_1_names_no_user()
    {
        Assert.Null(JetLockLayout.Decode(0x10000001, 255).User);
        Assert.Null(JetLockLayout.Decode(0x10000100, 255).User);
        Assert.Null(JetLockLayout.Decode(0x10000500, 256).User);
        Assert.Null(JetLockLayout.Decode(0x10000500, 1).User);
        Assert.Null(JetLockLayout.Decode(0x10000000, 1).User);
    }

    [Fact]
    public void Locks_on_a_page_that_are_neither_one_byte_nor_an_exclusive_range_name_no_user()
    {
        // The upgrade shape: bytes 2..256 of the window.
        Assert.Null(JetLockLayout.Decode(0x2002CA02, 255).User);
        Assert.Null(JetLockLayout.Decode(0x2002CA01, null).User);
        Assert.Null(JetLockLayout.Decode(0x2002CA00, 1).User);
    }

    [Fact]
    public void The_lock_file_holds_the_record_of_user_n_at_index_n_minus_1()
    {
        Assert.Equal(0, JetLockLayout.UserRecordOffset(1));
        Assert.Equal(64, JetLockLayout.UserRecordOffset(2));
        Assert.Equal(1, JetLockLayout.UserAtRecordOffset(0));
        Assert.Equal(2, JetLockLayout.UserAtRecordOffset(64));
        Assert.Null(JetLockLayout.UserAtRecordOffset(32));
    }

    [Theory]
    [InlineData((int)JetLockRegion.WriteRead, 0, 1)]
    [InlineData((int)JetLockRegion.WriteRead, 357, 2)]
    [InlineData((int)JetLockRegion.TableDenyWrite, JetLockLayout.MaxPage, JetLockLayout.MaxUserNumber)]
    public void A_shared_byte_round_trips(int regionValue, int page, int user)
    {
        var region = (JetLockRegion)regionValue;
        JetLockAddress address = JetLockLayout.Decode(JetLockLayout.SharedByte(region, page, user), 1);

        Assert.Equal((region, (long)page, (int?)user), (address.Region, address.Window, address.User));
    }

    [Theory]
    [InlineData((int)JetLockRegion.WriteRead, 0, 1)]
    [InlineData((int)JetLockRegion.TableWrite, 4000, 128)]
    [InlineData((int)JetLockRegion.TableDenyWrite, JetLockLayout.MaxPage, JetLockLayout.MaxUserNumber)]
    public void An_exclusive_range_round_trips_and_stays_inside_its_page_window(int regionValue, int page, int user)
    {
        var region = (JetLockRegion)regionValue;
        (long offset, long length) = JetLockLayout.ExclusiveRange(region, page, user);
        JetLockAddress address = JetLockLayout.Decode(offset, length);

        Assert.Equal((region, (long)page, (int?)user), (address.Region, address.Window, address.User));
        Assert.True(offset + length <= JetLockLayout.PageWindow(region, page) + JetLockLayout.PageWindowSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(JetLockLayout.MaxUserNumber)]
    public void A_user_byte_round_trips(int user)
    {
        Assert.Equal(user, JetLockLayout.Decode(JetLockLayout.UserByte(user), 1).User);
        Assert.Equal(user, JetLockLayout.Decode(JetLockLayout.SessionByte(user), 1).User);
    }

    [Fact]
    public void Fields_out_of_range_are_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => JetLockLayout.UserByte(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => JetLockLayout.UserByte(JetLockLayout.MaxUserNumber + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => JetLockLayout.SessionByte(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => JetLockLayout.SharedByte(JetLockRegion.WriteRead, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, 0, JetLockLayout.MaxUserNumber + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => JetLockLayout.PageWindow(JetLockRegion.WriteRead, JetLockLayout.MaxPage + 1));
    }

    [Theory]
    [InlineData(@"dir/db.accdb", @"dir/db.laccdb")]
    [InlineData(@"dir/db.ACCDE", @"dir/db.laccdb")]
    [InlineData(@"dir/db.mdb", @"dir/db.ldb")]
    [InlineData(@"dir/System.mdw", @"dir/System.ldb")]
    [InlineData(@"dir/db.txt", null)]
    public void The_lock_file_sits_beside_the_database(string database, string? expected)
    {
        Assert.Equal(expected, JetLockFile.PathFor(database));
    }

    [Fact]
    public void Shared_bytes_of_two_users_on_one_page_coexist()
    {
        WithTwoHandles((first, second) =>
        {
            long user1 = JetLockLayout.SharedByte(JetLockRegion.TableRead, 310, 1);
            long user2 = JetLockLayout.SharedByte(JetLockRegion.TableRead, 310, 2);

            Assert.True(first.TryLock(user1, 1));
            Assert.True(second.TryLock(user2, 1));
            first.Unlock(user1, 1);
            second.Unlock(user2, 1);
        });
    }

    [Fact]
    public void The_same_shared_byte_conflicts()
    {
        WithTwoHandles((first, second) =>
        {
            long user1 = JetLockLayout.SharedByte(JetLockRegion.TableRead, 310, 1);

            Assert.True(first.TryLock(user1, 1));
            Assert.False(second.TryLock(user1, 1));
            first.Unlock(user1, 1);
            Assert.True(second.TryLock(user1, 1));
            second.Unlock(user1, 1);
        });
    }

    [Fact]
    public void An_exclusive_range_conflicts_with_any_users_shared_byte_both_ways()
    {
        WithTwoHandles((first, second) =>
        {
            // User 2's exclusive range covers user 1's shared byte, and user 1's covers user 2's: the range runs
            // over every user's byte, not just those below the holder.
            long user1 = JetLockLayout.SharedByte(JetLockRegion.WriteRead, 357, 1);
            long user200 = JetLockLayout.SharedByte(JetLockRegion.WriteRead, 357, 200);
            (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, 357, 2);

            Assert.True(first.TryLock(user1, 1));
            Assert.False(second.TryLock(offset, length));
            first.Unlock(user1, 1);

            Assert.True(first.TryLock(user200, 1));
            Assert.False(second.TryLock(offset, length));
            first.Unlock(user200, 1);

            Assert.True(second.TryLock(offset, length));
            Assert.False(first.TryLock(user1, 1));
            Assert.False(first.TryLock(user200, 1));
            second.Unlock(offset, length);
        });
    }

    [Fact]
    public void Exclusive_ranges_of_two_users_on_one_page_conflict_but_a_neighbouring_page_does_not()
    {
        WithTwoHandles((first, second) =>
        {
            (long offset1, long length1) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, 357, 1);
            (long offset2, long length2) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, 357, 2);

            Assert.True(first.TryLock(offset1, length1));
            Assert.False(second.TryLock(offset2, length2));
            Assert.False(second.TryLock(offset1, length1));

            (long next, long nextLength) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, 358, 2);
            Assert.True(second.TryLock(next, nextLength));
            second.Unlock(next, nextLength);
            first.Unlock(offset1, length1);
        });
    }

    [Fact]
    public void A_user_holds_its_group_0_and_session_bytes_and_a_second_user_takes_its_own()
    {
        WithTwoHandles((first, second) =>
        {
            Assert.True(first.TryLock(JetLockLayout.UserByte(1), 1));
            Assert.True(first.TryLock(JetLockLayout.SessionByte(1), 1));

            Assert.False(second.TryLock(JetLockLayout.UserByte(1), 1));
            Assert.False(second.TryLock(JetLockLayout.SessionByte(1), 1));
            Assert.True(second.TryLock(JetLockLayout.UserByte(2), 1));
            Assert.True(second.TryLock(JetLockLayout.SessionByte(2), 1));

            first.Unlock(JetLockLayout.UserByte(1), 1);
            first.Unlock(JetLockLayout.SessionByte(1), 1);
            second.Unlock(JetLockLayout.UserByte(2), 1);
            second.Unlock(JetLockLayout.SessionByte(2), 1);
        });
    }

    [Fact]
    public void A_lock_far_past_the_end_of_an_empty_file_works_and_unlocking_frees_it()
    {
        WithTwoHandles((first, second) =>
        {
            long user = JetLockLayout.UserByte(JetLockLayout.MaxUserNumber);
            (long page, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.TableDenyWrite, JetLockLayout.MaxPage, 1);

            Assert.True(first.TryLock(user, 1));
            Assert.True(first.TryLock(page, length));
            Assert.False(second.TryLock(user, 1));
            Assert.False(second.TryLock(page, length));

            first.Unlock(user, 1);
            first.Unlock(page, length);
            Assert.True(second.TryLock(user, 1));
            Assert.True(second.TryLock(page, length));
            second.Unlock(user, 1);
            second.Unlock(page, length);
        });
    }

    [Fact]
    public void Unlocking_a_range_not_held_is_an_error_not_a_conflict()
    {
        WithTwoHandles((first, _) => Assert.Throws<IOException>(() => first.Unlock(JetLockLayout.UserByte(1), 1)));
    }

    [Fact]
    public void Opening_a_missing_lock_file_does_not_create_it_unless_asked()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Lock-file locking is Windows-only for now.");
        string path = TemporaryDatabase.CreatePath("libred-lockfile-", JetLockFile.AceExtension);
        try
        {
            Assert.Throws<FileNotFoundException>(() => JetLockFile.Open(path, createIfMissing: false));
            Assert.False(File.Exists(path));

            using (JetLockFile.Open(path, createIfMissing: true))
                Assert.True(File.Exists(path));
            Assert.Equal(0, new FileInfo(path).Length);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void Off_Windows_opening_is_refused_before_touching_the_file()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Lock-file locking is implemented on Windows.");
        string path = TemporaryDatabase.CreatePath("libred-lockfile-", JetLockFile.AceExtension);
        Assert.Throws<PlatformNotSupportedException>(() => JetLockFile.Open(path, createIfMissing: true));
        Assert.False(File.Exists(path));
    }

    // Two handles on one empty lock file, as two users have; the file is shared for read and write, so the second
    // open does not conflict with the first, and both are closed before the deletion afterwards.
    private static void WithTwoHandles(Action<JetLockFile, JetLockFile> body)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Lock-file locking is Windows-only for now.");
        string path = TemporaryDatabase.CreatePath("libred-lockfile-", JetLockFile.AceExtension);
        try
        {
            File.WriteAllBytes(path, []);
            using JetLockFile first = JetLockFile.Open(path, createIfMissing: false);
            using JetLockFile second = JetLockFile.Open(path, createIfMissing: false);
            body(first, second);
            Assert.Equal(0, new FileInfo(path).Length);
        }
        finally { TemporaryDatabase.Delete(path); }
    }
}
