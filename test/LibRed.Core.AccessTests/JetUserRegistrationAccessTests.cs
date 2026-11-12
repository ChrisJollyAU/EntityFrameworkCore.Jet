using System.Data.OleDb;
using LibRed.IO;
using Xunit;

namespace LibRed.Core.Tests;

// LibRed registered as a Jet user, seen by the real engine: ACE refuses an exclusive open naming LibRed's record, and a
// shared ACE connection takes the next user number beside LibRed — in either order. One ACE connection per test: ACE is
// unstable under connection churn.
[Collection(AceCollection.Name)]
public class JetUserRegistrationAccessTests
{
    [Fact]
    public void Ace_refuses_an_exclusive_open_naming_LibReds_record()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "libred-register-ace-");
        try
        {
            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                Assert.Equal(1, database.Channel.UserNumber);
                OleDbException refused = Assert.Throws<OleDbException>(() => OpenExclusive(path).Dispose());
                Assert.Equal("3704", refused.Errors[0].SQLState);   // OLE DB carries the Jet error number here
                Assert.Contains($"already opened by user 'Admin' on machine '{Environment.MachineName}'",
                    refused.Message, StringComparison.Ordinal);
            }

            Assert.False(File.Exists(JetLockFile.PathFor(path)));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void Ace_opening_after_LibRed_is_user_2()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "libred-register-ace-");
        string lockPath = JetLockFile.PathFor(path)!;
        try
        {
            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
            {
                Assert.Equal(1, database.Channel.UserNumber);
                using (OleDbConnection ace = AceTestDatabase.Open(path))
                {
                    using (JetLockFile probe = JetLockFile.Open(lockPath, createIfMissing: false))
                    {
                        Assert.False(probe.TryLock(JetLockLayout.SessionByte(2), 1));   // 0x10000101
                        Assert.False(probe.TryLock(JetLockLayout.UserByte(2), 1));      // 0x10000002
                    }

                    byte[] records = ReadLockFile(lockPath);
                    Assert.Equal(2 * JetLockLayout.UserRecordSize, records.Length);
                    // ACE's record for itself is byte for byte the one LibRed wrote for user 1.
                    Assert.Equal(records[..JetLockLayout.UserRecordSize], records[JetLockLayout.UserRecordSize..]);
                }

                // ACE left first and found LibRed connected, so the file stays; its bytes are free.
                Assert.True(File.Exists(lockPath));
                using (JetLockFile probe = JetLockFile.Open(lockPath, createIfMissing: false))
                {
                    Assert.True(probe.TryLock(JetLockLayout.UserByte(2), 1));
                    probe.Unlock(JetLockLayout.UserByte(2), 1);
                }
            }

            Assert.False(File.Exists(lockPath));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void LibRed_opening_after_Ace_is_user_2()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "libred-register-ace-");
        string lockPath = JetLockFile.PathFor(path)!;
        try
        {
            using (OleDbConnection ace = AceTestDatabase.Open(path))
            {
                byte[] aceRecord = ReadLockFile(lockPath);
                using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
                {
                    Assert.Equal(2, database.Channel.UserNumber);
                    byte[] records = ReadLockFile(lockPath);
                    Assert.Equal(2 * JetLockLayout.UserRecordSize, records.Length);
                    Assert.Equal(aceRecord, records[..JetLockLayout.UserRecordSize]);
                    Assert.Equal(aceRecord, records[JetLockLayout.UserRecordSize..]);   // the same machine and name
                }

                // LibRed left first and found ACE connected, so the file stays.
                Assert.True(File.Exists(lockPath));
            }

            Assert.False(File.Exists(lockPath));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    private static OleDbConnection OpenExclusive(string path)
    {
        Exception? last = null;
        foreach (string provider in new[] { "Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0" })
        {
            var connection = new OleDbConnection(
                $"Provider={provider};Data Source={path};Mode=Share Exclusive;OLE DB Services=-4;");
            try { connection.Open(); return connection; }
            catch (Exception e) { last = e; connection.Dispose(); }
        }
        throw last!;
    }

    // Shared for read and write: both users hold the file open for writing.
    private static byte[] ReadLockFile(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }
}
