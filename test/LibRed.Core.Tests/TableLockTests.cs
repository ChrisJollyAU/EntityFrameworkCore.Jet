using System.Diagnostics;
using LibRed.IO;
using Xunit;

namespace LibRed.Core.Tests;

// A connection's lock table (docs/format/locking.md §2, §4): one user per connection, whose holders of one lock share
// the operating-system lock, and its own shared byte never stands in the way of its own exclusive lock — which it takes as
// ACE takes an upgrade, the other users' bytes on top of its own. "The other user" is a second JetLockFile handle as
// user 2, which conflicts with LibRed's own exactly as another process's would.
public class TableLockTests
{
    private const string WindowsOnly = "Lock-file locking is Windows-only for now.";
    private const int OtherUser = 2;
    private const int Page = 100;

    [Fact]
    public void A_process_holding_a_tables_shared_byte_takes_it_exclusively_as_an_upgrade_and_keeps_the_byte_after()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-tablelock-");
        using JetUserRegistration registration = JetUserRegistration.Connect(copy.Path)!;
        using JetLockFile other = OtherUserOf(copy.Path);
        Assert.Equal(1, registration.User);
        long window = JetLockLayout.PageWindow(JetLockRegion.TableRead, Page);

        Assert.True(registration.Lock(JetLockRegion.TableRead, Page, exclusive: false, JetLockPurpose.Table, "T"));
        Assert.False(registration.Lock(JetLockRegion.TableRead, Page, exclusive: false, JetLockPurpose.Table, "T"));   // joined
        Assert.False(IsFree(other, window + 1, 1));
        Assert.True(SharedIsFree(other));
        Assert.False(ExclusiveIsFree(other));

        // The upgrade: every other user's byte, window + 2 for 255 bytes for user 1 — not ACE's single range, whose
        // last byte (window + 257) it leaves free.
        Assert.True(registration.Lock(JetLockRegion.TableRead, Page, exclusive: true, JetLockPurpose.Table, "T"));
        Assert.False(SharedIsFree(other));
        Assert.False(IsFree(other, window + 2, 1));
        Assert.False(IsFree(other, window + 256, 1));
        Assert.True(IsFree(other, window + 257, 1));

        // Let go of, the process's readers keep their byte; the last of them lets go of it.
        registration.Unlock(JetLockRegion.TableRead, Page, exclusive: true);
        Assert.True(SharedIsFree(other));
        Assert.False(ExclusiveIsFree(other));
        registration.Unlock(JetLockRegion.TableRead, Page, exclusive: false);
        Assert.False(ExclusiveIsFree(other));
        registration.Unlock(JetLockRegion.TableRead, Page, exclusive: false);
        Assert.True(ExclusiveIsFree(other));
    }

    [Fact]
    public void An_exclusive_table_lock_takes_the_processs_own_byte_first_so_a_reader_can_join_and_outlast_it()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-tablelock-");
        using JetUserRegistration registration = JetUserRegistration.Connect(copy.Path)!;
        using JetLockFile other = OtherUserOf(copy.Path);
        long window = JetLockLayout.PageWindow(JetLockRegion.TableRead, Page);

        Assert.True(registration.Lock(JetLockRegion.TableRead, Page, exclusive: true, JetLockPurpose.Table, "T"));
        Assert.False(registration.Lock(JetLockRegion.TableRead, Page, exclusive: false, JetLockPurpose.Table, "T"));   // joined, no OS call
        Assert.False(IsFree(other, window + 1, 1));
        Assert.False(SharedIsFree(other));

        registration.Unlock(JetLockRegion.TableRead, Page, exclusive: true);
        Assert.False(IsFree(other, window + 1, 1));   // the reader's byte, never given up
        Assert.True(SharedIsFree(other));
        registration.Unlock(JetLockRegion.TableRead, Page, exclusive: false);
        Assert.True(ExclusiveIsFree(other));
    }

    [Fact]
    public void A_page_write_lock_is_still_ACEs_single_range()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-tablelock-");
        using JetUserRegistration registration = JetUserRegistration.Connect(copy.Path)!;
        using JetLockFile other = OtherUserOf(copy.Path);
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.WriteRead, Page, registration.User);

        Assert.True(registration.Lock(JetLockRegion.WriteRead, Page, exclusive: true, JetLockPurpose.PageWrite));
        Assert.False(IsFree(other, offset, 1));
        Assert.False(IsFree(other, offset + length - 1, 1));
        Assert.True(IsFree(other, offset + length, 1));
        registration.Unlock(JetLockRegion.WriteRead, Page, exclusive: true);
        Assert.True(IsFree(other, offset, length));
    }

    [Fact]
    public void A_table_lock_another_user_holds_is_retried_as_ACE_retries_and_then_refused_with_3211()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-tablelock-");
        using JetUserRegistration registration = JetUserRegistration.Connect(copy.Path)!;
        using JetLockFile other = OtherUserOf(copy.Path);
        long shared = JetLockLayout.SharedByte(JetLockRegion.TableRead, Page, OtherUser);
        Assert.True(other.TryLock(shared, 1));

        var wait = Stopwatch.StartNew();
        var refused = Assert.Throws<InvalidOperationException>(
            () => registration.Lock(JetLockRegion.TableRead, Page, exclusive: true, JetLockPurpose.Table, "Orders"));
        wait.Stop();
        Assert.Equal(
            "The database engine could not lock table 'Orders' because it is already in use by another person or process.",
            refused.Message);
        Assert.InRange(wait.ElapsedMilliseconds, 1900, 10_000);

        // Nothing is left behind: its own byte, taken for the attempt, went with it.
        Assert.True(IsFree(other, JetLockLayout.SharedByte(JetLockRegion.TableRead, Page, registration.User), 1));
        other.Unlock(shared, 1);
    }

    [Fact]
    public void Exclusive_and_unregistered_opens_take_no_table_locks()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-tablelock-");
        string lockFile = JetLockFile.PathFor(copy.Path)!;

        using (JetDatabase exclusive = JetDatabase.Open(copy.Path, readOnly: false, exclusive: true))
        {
            Assert.Null(exclusive.Channel.UserNumber);
            exclusive.ReadConsistent(() => exclusive.OpenTable("Customers").Rows().Take(1).ToList());
            exclusive.BeginTransaction();
            exclusive.OpenTable("Customers");
            Assert.False(File.Exists(lockFile));
            exclusive.Rollback();
        }

        // An extension Jet/ACE locks no file for: the open is shared and unregistered.
        string unregistered = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "libred-tablelock-");
        string renamed = Path.ChangeExtension(unregistered, ".dat");
        File.Move(unregistered, renamed);
        try
        {
            using JetDatabase shared = JetDatabase.Open(renamed, readOnly: false);
            using JetDatabase second = JetDatabase.Open(renamed, readOnly: false);
            Assert.Null(shared.Channel.UserNumber);
            shared.BeginTransaction();
            shared.OpenTable("Customers");
            second.CreateIndex("Customers", "IxCity", [("City", false)]);
            shared.Rollback();
        }
        finally { TemporaryDatabase.Delete(renamed); }
    }

    // Outside every statement, an enumeration of a table's rows holds its table-read byte from its first row until it
    // ends or is disposed, in a transaction or not — as ACE's open reader holds it until closed, not to COMMIT (measured,
    // locking.md §4) — and another user's design lock refuses it with 3008.
    [Fact]
    public void A_Core_API_enumeration_outside_a_statement_holds_the_tables_read_byte_until_it_is_disposed()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        using var copy = TemporaryDatabase.CopyOf(TestDatabases.NorthwindAccdb, "libred-tablelock-");
        using JetDatabase database = JetDatabase.Open(copy.Path, readOnly: false);
        using JetLockFile other = OtherUserOf(copy.Path);
        Storage.Table customers = database.OpenTable("Customers");
        int tdef = customers.Definition.DefinitionPage;
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.TableRead, tdef, OtherUser);
        Assert.True(IsFree(other, offset, length));

        foreach (bool inTransaction in (bool[])[false, true])
        {
            if (inTransaction) database.BeginTransaction();
            using (IEnumerator<object?[]> rows = customers.Rows().GetEnumerator())
            {
                Assert.True(rows.MoveNext());
                Assert.False(IsFree(other, offset, length));
                Assert.True(IsFree(other, JetLockLayout.SharedByte(JetLockRegion.TableRead, tdef, OtherUser), 1));
            }
            Assert.True(IsFree(other, offset, length));
            // Read to its end, it lets go as it ends.
            using (IEnumerator<object?[]> rows = customers.Rows().GetEnumerator())
            {
                while (rows.MoveNext()) { }
                Assert.True(IsFree(other, offset, length));
            }
            if (inTransaction) database.Rollback();
        }

        // Another user changing the table's design refuses the enumeration, once ACE's retries are spent: ACE's SELECT
        // from Customers beside another connection's ALTER TABLE Customers in an open transaction fails with 3008
        // after about 4.4 s, two rounds of its retries (measured).
        Assert.True(other.TryLock(offset, length));
        var wait = Stopwatch.StartNew();
        var refused = Assert.Throws<InvalidOperationException>(() => customers.Rows().First());
        wait.Stop();
        Assert.Equal(
            "The table 'Customers' is already opened exclusively by another user, or it is already open through the user interface and cannot be manipulated programmatically.",
            refused.Message);
        Assert.InRange(wait.ElapsedMilliseconds, 3900, 12_000);
        other.Unlock(offset, length);
        Assert.NotEmpty(customers.Rows().Take(1).ToList());
        Assert.True(IsFree(other, offset, length));
    }

    private static JetLockFile OtherUserOf(string databasePath) =>
        JetLockFile.Open(JetLockFile.PathFor(databasePath)!, createIfMissing: false);

    private static bool IsFree(JetLockFile other, long offset, long length)
    {
        if (!other.TryLock(offset, length)) return false;
        other.Unlock(offset, length);
        return true;
    }

    private static bool SharedIsFree(JetLockFile other) =>
        IsFree(other, JetLockLayout.SharedByte(JetLockRegion.TableRead, Page, OtherUser), 1);

    private static bool ExclusiveIsFree(JetLockFile other)
    {
        (long offset, long length) = JetLockLayout.ExclusiveRange(JetLockRegion.TableRead, Page, OtherUser);
        return IsFree(other, offset, length);
    }
}
