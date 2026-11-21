using System.Reflection;
using LibRed.Crypto;
using LibRed.Storage;
using Xunit;

namespace LibRed.Core.Tests;

/// <summary>
/// The legacy Jet database password is half of the header region the SID keystream is folded from (page-00
/// §2.3), so setting one re-masks every stored SID. LibRed's <see cref="DatabaseEncryption.SetJetPassword"/> has
/// to write exactly what Access's own password change does: DAO's <c>NewPassword</c> on one copy, LibRed on
/// another, and every <c>MSysObjects.Owner</c> and <c>MSysACEs.SID</c> — and the password field — must agree.
/// </summary>
[Collection(AceCollection.Name)]
public class JetPasswordSidAccessTests
{
    private const string Password = "Test1";

    [Fact]
    public void A_jet_password_remasks_a_dao_databases_sids_as_access_does()
    {
        object? engine = AceTestDatabase.CreateDaoEngine();
        Assert.SkipWhen(engine is null, "DAO is not registered in this bitness.");

        string source = TemporaryDatabase.CreatePath("jetpw_dao_", ".mdb");
        try
        {
            object database = Invoke(engine!, "CreateDatabase", source, ";LANGID=0x0409;CP=1252;COUNTRY=0", 64)!; // dbVersion40
            Invoke(database, "Close");
            Compare(engine!, source);
        }
        finally { TemporaryDatabase.Delete(source); }
    }

    [Fact]
    public void A_jet_password_remasks_a_libred_databases_sids_as_access_does()
    {
        object? engine = AceTestDatabase.CreateDaoEngine();
        Assert.SkipWhen(engine is null, "DAO is not registered in this bitness.");

        string source = TemporaryDatabase.CreatePath("jetpw_libred_", ".mdb");
        try
        {
            JetDatabase.Create(source, version: (byte)Formats.JetVersion.Version4);
            Compare(engine!, source);
        }
        finally { TemporaryDatabase.Delete(source); }
    }

    // The password guards the open: LibRed must let in exactly the passwords ACE lets in, on a file with one and on
    // a file without. Each password reaches ACE quoted, so its spaces survive the connection string.
    [Fact]
    public void A_jet_password_admits_the_passwords_access_admits()
    {
        string plain = TemporaryDatabase.CreatePath("jetpw_open_", ".mdb");
        string set = TemporaryDatabase.CreatePath("jetpw_open_set_", ".mdb");
        string setBare = TemporaryDatabase.CreatePath("jetpw_open_bare_", ".mdb");
        try
        {
            JetDatabase.Create(plain, version: (byte)Formats.JetVersion.Version4);
            File.Copy(plain, set, overwrite: true);
            File.Copy(plain, setBare, overwrite: true);
            using (var db = JetDatabase.Open(set, readOnly: false, exclusive: true))
                DatabaseEncryption.SetJetPassword(db, "abc ");
            using (var db = JetDatabase.Open(setBare, readOnly: false, exclusive: true))
                DatabaseEncryption.SetJetPassword(db, "abc");

            foreach ((string path, string? password) in new[]
            {
                (set, "abc "), (set, "abc"), (set, "ABC "), (set, " abc "), (set, ""), (set, null),
                (setBare, "abc"), (setBare, "ABC"), (setBare, "ab"), (setBare, "abc "), (setBare, " abc"),
                (plain, "abc"), (plain, ""), (plain, null),
            })
            {
                string? aceRefusal = null;
                try
                {
                    using var connection = AceTestDatabase.Open(path, password is null ? null : $"\"{password}\"", attempts: 1);
                }
                catch (InvalidOperationException ex) { aceRefusal = ex.InnerException?.Message; }

                string? ourRefusal = null;
                try
                {
                    using var db = JetDatabase.Open(path, password: password);
                }
                catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException) { ourRefusal = ex.Message; }

                Assert.True(aceRefusal == ourRefusal,
                    $"{Path.GetFileName(path)} opened with {(password is null ? "none" : $"'{password}'")}: ACE "
                    + $"{aceRefusal ?? "opened"}, LibRed {ourRefusal ?? "opened"}");
            }
        }
        finally
        {
            TemporaryDatabase.Delete(plain);
            TemporaryDatabase.Delete(set);
            TemporaryDatabase.Delete(setBare);
        }
    }

    private static void Compare(object engine, string source)
    {
        string byAccess = TemporaryDatabase.CopyPath(source, "jetpw_access_");
        string byLibRed = TemporaryDatabase.CopyPath(source, "jetpw_ours_");
        try
        {
            object database = Invoke(engine, "OpenDatabase", byAccess, true, false, "")!;
            Invoke(database, "NewPassword", "", Password);
            Invoke(database, "Close");
            AceTestDatabase.ReleaseAbandonedComObjects();

            using (var db = JetDatabase.Open(byLibRed, readOnly: false, exclusive: true))
                DatabaseEncryption.SetJetPassword(db, Password);

            List<byte[]> access = StoredSids(byAccess), ours = StoredSids(byLibRed);
            Assert.NotEqual(StoredSids(source, null)[0], access[0]); // Access did re-mask them
            Assert.Equal(access, ours);
            Formats.JetFormatBase format = TestDatabases.FormatOf(byAccess);
            Range password = format.PasswordOffset..(format.PasswordOffset + format.PasswordSize);
            Assert.Equal(File.ReadAllBytes(byAccess)[password], File.ReadAllBytes(byLibRed)[password]);
        }
        finally
        {
            TemporaryDatabase.Delete(byAccess);
            TemporaryDatabase.Delete(byLibRed);
        }
    }

    /// <summary>Every MSysObjects.Owner and MSysACEs.SID, in stored order.</summary>
    private static List<byte[]> StoredSids(string path, string? password = Password)
    {
        using var db = JetDatabase.Open(path, password: password);
        var sids = new List<byte[]>();
        foreach ((string table, string column) in new[] { ("MSysObjects", "Owner"), ("MSysACEs", "SID") })
        {
            var t = db.OpenTable(table);
            int index = t.Definition.RequireColumn(column).Index;
            foreach (object?[] row in t.Rows())
                if (row[index] is byte[] sid) sids.Add(sid);
        }
        return sids;
    }

    private static object? Invoke(object target, string member, params object?[] args) =>
        target.GetType().InvokeMember(member, BindingFlags.InvokeMethod, null, target, args);
}