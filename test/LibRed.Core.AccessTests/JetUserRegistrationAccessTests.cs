using System.Data.OleDb;
using System.Runtime.InteropServices;
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

    // ACE barring new connections — "Jet OLEDB:Connection Control" set to 1 on an open ADO connection, which holds its
    // group-5 byte (locking.md §3) — refuses LibRed's open with the very error it gives its own second connection, 3734
    // naming its record; set back to 2, LibRed connects. ADO rather than System.Data.OleDb: the property can only be
    // set on an open session, which OleDbConnection does not expose.
    [Fact]
    public void LibRed_is_refused_while_Ace_bars_new_connections_as_Aces_own_connection_is()
    {
        Type? adoConnection = Type.GetTypeFromProgID("ADODB.Connection");
        Assert.SkipWhen(adoConnection is null, "ADODB is not registered in this process.");
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "libred-register-ace-");
        AceTestDatabase.ReleaseAbandonedComObjects();
        dynamic ace = Activator.CreateInstance(adoConnection)!;
        try
        {
            OpenAdo(ace, path);
            ace.Properties.Item("Jet OLEDB:Connection Control").Value = 1;

            var aceRefused = Assert.Throws<InvalidOperationException>(() => AceTestDatabase.Open(path, attempts: 1).Dispose());
            OleDbException aceRefusal = Assert.IsType<OleDbException>(aceRefused.InnerException);
            Assert.Equal("3734", aceRefusal.Errors[0].SQLState);   // OLE DB carries the Jet error number here

            IOException refused = Assert.Throws<IOException>(() => JetDatabase.Open(path, readOnly: false).Dispose());
            Assert.Equal(aceRefusal.Errors[0].Message, refused.Message);
            Assert.Contains($"by user 'Admin' on machine '{Environment.MachineName}'", refused.Message, StringComparison.Ordinal);

            ace.Properties.Item("Jet OLEDB:Connection Control").Value = 2;
            using (JetDatabase database = JetDatabase.Open(path, readOnly: false))
                Assert.Equal(2, database.Channel.UserNumber);
        }
        finally
        {
            try { ace.Close(); } catch (Exception) { /* never opened */ }
            Marshal.FinalReleaseComObject((object)ace);
            AceTestDatabase.ReleaseAbandonedComObjects();
            TemporaryDatabase.Delete(path);
        }
    }

    // LibRed barring new connections, as ACE's "Jet OLEDB:Connection Control" set to 1 does: ACE's next connection is
    // refused with 3734 naming LibRed's record, an ACE connection already open goes on working, and once LibRed admits
    // new connections again ACE connects.
    [Fact]
    public void Ace_is_refused_while_LibRed_bars_new_connections()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "libred-register-ace-");
        try
        {
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            using OleDbConnection connected = AceTestDatabase.Open(path);
            database.BarsNewConnections = true;

            var aceRefused = Assert.Throws<InvalidOperationException>(() => AceTestDatabase.Open(path, attempts: 1).Dispose());
            OleDbException aceRefusal = Assert.IsType<OleDbException>(aceRefused.InnerException);
            Assert.Equal("3734", aceRefusal.Errors[0].SQLState);   // OLE DB carries the Jet error number here
            Assert.Equal(
                $"The database has been placed in a state by user 'Admin' on machine '{Environment.MachineName}' that prevents it from being opened or locked.",
                aceRefusal.Errors[0].Message);

            using (OleDbCommand command = connected.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM Customers";
                Assert.Equal(91, Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
            }

            database.BarsNewConnections = false;
            AceTestDatabase.Open(path, attempts: 1).Dispose();
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // LibRed setting the bar while ACE already bars: it holds nothing, raises nothing and reads as set, as ACE's second
    // setter does; once ACE lets its bar go, ACE's next connection connects though LibRed's property still reads true.
    [Fact]
    public void LibRed_barring_behind_Aces_bar_holds_nothing()
    {
        Type? adoConnection = Type.GetTypeFromProgID("ADODB.Connection");
        Assert.SkipWhen(adoConnection is null, "ADODB is not registered in this process.");
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "libred-register-ace-");
        AceTestDatabase.ReleaseAbandonedComObjects();
        dynamic ace = Activator.CreateInstance(adoConnection)!;
        try
        {
            OpenAdo(ace, path);
            using JetDatabase database = JetDatabase.Open(path, readOnly: false);
            Assert.Equal(2, database.Channel.UserNumber);
            long libRedBar = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, 2);

            ace.Properties.Item("Jet OLEDB:Connection Control").Value = 1;
            database.BarsNewConnections = true;
            Assert.True(database.BarsNewConnections);
            using (JetLockFile probe = JetLockFile.Open(JetLockFile.PathFor(path)!, createIfMissing: false))
            {
                Assert.True(probe.TryLock(libRedBar, 1));
                probe.Unlock(libRedBar, 1);
            }

            ace.Properties.Item("Jet OLEDB:Connection Control").Value = 2;
            Assert.True(database.BarsNewConnections);
            AceTestDatabase.Open(path, attempts: 1).Dispose();
        }
        finally
        {
            try { ace.Close(); } catch (Exception) { /* never opened */ }
            Marshal.FinalReleaseComObject((object)ace);
            AceTestDatabase.ReleaseAbandonedComObjects();
            TemporaryDatabase.Delete(path);
        }
    }

    private static void OpenAdo(dynamic connection, string path)
    {
        Exception? last = null;
        foreach (string provider in new[] { "Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0" })
        {
            try { connection.Open($"Provider={provider};Data Source={path};"); return; }
            catch (Exception e) { last = e; }
        }
        throw last!;
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
