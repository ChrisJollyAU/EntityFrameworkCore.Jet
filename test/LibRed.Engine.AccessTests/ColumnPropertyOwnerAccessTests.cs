using System.Data.OleDb;
using System.Reflection;
using LibRed;
using LibRed.Catalog;
using Xunit;

namespace LibRed.Engine.Tests;

// A column's properties sit in an LvProp block whose owner names the column, and an owner need not be spelt as the
// table stores the name. DAO builds the table, one column's owner is respelt in its blob, and ACE and LibRed each run
// the same statements on a copy. A row write finds DefaultValue, Required and AllowZeroLength by the database's
// collation with trailing spaces counted (case, width, kana type and ß for ss fold; a long s and a space at the end do
// not); a calculated column's Expression, and ALTER COLUMN, DROP COLUMN and a rename, find the block by ACE's case
// table (case folds, width and a long s do not), and keep its owner's spelling.
[Collection(AceCollection.Name)]
public class ColumnPropertyOwnerAccessTests
{
    private static readonly string[] Inserts =
    [
        "INSERT INTO T (K) VALUES (1)",               // DefaultValue
        "INSERT INTO T (K, Szxcol) VALUES (2, NULL)", // Required
        "INSERT INTO T (K, Szxcol) VALUES (3, '')",   // AllowZeroLength
        "INSERT INTO T (K, Szxcol) VALUES (4, 'ok')",
    ];

    [Theory]
    [InlineData("Szxcol", "szxcol")]          // case
    [InlineData("Szxcol", "Ｓzxcol")]     // full width
    [InlineData("Szxcol", "ſzxcol")]     // long s
    [InlineData("Szxcol", "Rzxcol")]          // another name
    [InlineData("Szxcol", "Szxcol ")]         // a space more at the end
    [InlineData("Sszx", "ßzx")]          // ß for ss
    [InlineData("カナ", "かな")] // hiragana for katakana
    [InlineData("Tsp ", "Tsp")]               // a space fewer at the end
    [InlineData("Szxcal", "szxcal")]
    [InlineData("Szxcal", "Ｓzxcal")]
    public void An_owner_spelt_unlike_its_column_is_found_where_ACE_finds_it(string owner, string respelt)
    {
        string origin = TemporaryDatabase.CreatePath("owner-origin-");
        var paths = new List<string> { origin };
        string Copy(string prefix)
        {
            string path = TemporaryDatabase.CreatePath(prefix);
            paths.Add(path);
            File.Copy(origin, path);
            return path;
        }
        try
        {
            Build(origin);
            Respell(origin, owner, respelt);

            string ace = Copy("owner-ace-"), libred = Copy("owner-libred-");
            foreach (string sql in Inserts)
                Assert.True(Ace(ace, sql) is null == LibRed(libred, sql) is null, sql);
            Assert.Equal(AceRows(ace), LibRedRows(libred));

            foreach (string sql in (string[])["ALTER TABLE T ALTER COLUMN Szxcol SET DEFAULT 'two'",
                         "ALTER TABLE T ALTER COLUMN Szxcol DROP DEFAULT", "ALTER TABLE T DROP COLUMN Szxcol"])
            {
                string aceDdl = Copy("owner-ace-ddl-"), libredDdl = Copy("owner-libred-ddl-");
                Assert.True(Ace(aceDdl, sql) is null == LibRed(libredDdl, sql) is null, sql);
                Assert.Equal(Owners(aceDdl), Owners(libredDdl));
            }

            string aceRename = Copy("owner-ace-rename-"), libredRename = Copy("owner-libred-rename-");
            Assert.True(DaoRename(aceRename) is null == LibRed(libredRename, "ALTER TABLE T RENAME COLUMN Szxcol TO Szxnew") is null);
            Assert.Equal(Owners(aceRename), Owners(libredRename));
        }
        finally
        {
            foreach (string path in paths) TemporaryDatabase.Delete(path);
        }
    }

    // T: K LONG; Szxcol TEXT(10) with DefaultValue 'dflt' and Required (DAO clears AllowZeroLength); Szxcal = [K]*2;
    // Sszx, カナ and "Tsp " (a name ending in a space) TEXT(10), each with a DefaultValue.
    private static void Build(string path)
    {
        object engine = AceTestDatabase.CreateDaoEngine()!;
        Assert.SkipWhen(engine is null, "DAO is not available in this process.");
        object db = Invoke(engine, "CreateDatabase", path, ";LANGID=0x0409;CP=1252;COUNTRY=0", 128)!;
        try
        {
            object td = Invoke(db, "CreateTableDef", "T")!;
            object fields = Get(td, "Fields");
            Invoke(fields, "Append", Invoke(td, "CreateField", "K", 4));
            object text = Invoke(td, "CreateField", "Szxcol", 10, 10)!;
            Set(text, "DefaultValue", "'dflt'");
            Set(text, "Required", true);
            Invoke(fields, "Append", text);
            object calc = Invoke(td, "CreateField", "Szxcal", 4)!;
            Set(calc, "Expression", "[K]*2");
            Invoke(fields, "Append", calc);
            foreach ((string name, string value) in new[] { ("Sszx", "'ss'"), ("カナ", "'kana'"), ("Tsp ", "'sp'") })
            {
                object field = Invoke(td, "CreateField", name, 10, 10)!;
                Set(field, "DefaultValue", value);
                Invoke(fields, "Append", field);
            }
            Invoke(Get(db, "TableDefs"), "Append", td);
        }
        finally { Invoke(db, "Close"); }
        AceTestDatabase.ReleaseAbandonedComObjects();
    }

    // Respells the owner of the column's LvProp block, rewriting T's blob, and not the column itself.
    private static void Respell(string path, string owner, string respelt)
    {
        using var db = JetDatabase.Open(path, readOnly: false);
        var objects = db.OpenTable("MSysObjects");
        int name = objects.Definition.FindColumn("Name")!.Index, lvProp = objects.Definition.FindColumn("LvProp")!.Index;
        var (id, values) = objects.Rows().WithIds().Single(r => (string)r.Values[name]! == "T");
        byte[] blob = (byte[])values[lvProp]!;
        Assert.Contains(PropertyBlob.Read(blob), p => p.Owner == owner);
        object?[] updated = (object?[])values.Clone();
        updated[lvProp] = PropertyBlob.Write(
            [.. PropertyBlob.Read(blob).Select(p => p.Owner == owner && p.Block == 0x0001 ? p with { Owner = respelt } : p)], blob);
        objects.Update(id, updated);
    }

    private static string? Ace(string path, string sql)
    {
        try
        {
            using OleDbConnection connection = AceTestDatabase.Open(path);
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
            return null;
        }
        catch (OleDbException e) { return e.Message; }
        finally { OleDbConnection.ReleaseObjectPool(); }
    }

    private static string? LibRed(string path, string sql)
    {
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            new QueryEngine(db).ExecuteNonQuery(sql);
            return null;
        }
        catch (Exception e) when (e is InvalidOperationException or InvalidDataException) { return e.Message; }
    }

    private static string? DaoRename(string path)
    {
        object engine = AceTestDatabase.CreateDaoEngine()!;
        object db = Invoke(engine, "OpenDatabase", path)!;
        try
        {
            Set(Item(Get(Item(Get(db, "TableDefs"), "T"), "Fields"), "Szxcol"), "Name", "Szxnew");
            return null;
        }
        catch (TargetInvocationException e) { return e.InnerException?.Message; }
        finally
        {
            Invoke(db, "Close");
            AceTestDatabase.ReleaseAbandonedComObjects();
        }
    }

    private const string Select = "SELECT K, Szxcol, Szxcal, Sszx, [カナ], [Tsp ] FROM T ORDER BY K";

    private static string AceRows(string path)
    {
        try
        {
            using OleDbConnection connection = AceTestDatabase.Open(path);
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = Select;
            using OleDbDataReader reader = command.ExecuteReader();
            var rows = new List<string>();
            while (reader.Read())
                rows.Add(string.Join(":", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i)))));
            return string.Join(", ", rows);
        }
        catch (OleDbException) { return "refused"; }
        finally { OleDbConnection.ReleaseObjectPool(); }
    }

    private static string LibRedRows(string path)
    {
        try
        {
            using var db = JetDatabase.Open(path, readOnly: true);
            return string.Join(", ", new QueryEngine(db).ExecuteQuery(Select).Rows
                .Select(r => string.Join(":", r.Select(v => v is null ? "NULL" : Convert.ToString(v)))));
        }
        catch (InvalidDataException) { return "refused"; }
    }

    // Every property of T, owner and value, in the order the blob holds them.
    private static string Owners(string path)
    {
        using var db = JetDatabase.Open(path, readOnly: true);
        var objects = db.OpenTable("MSysObjects");
        var od = objects.Definition;
        object?[] row = objects.Rows().Single(r => (string)r[od.FindColumn("Name")!.Index]! == "T");
        return row[od.FindColumn("LvProp")!.Index] is byte[] { Length: > 0 } lv
            ? string.Join("; ", PropertyBlob.Read(lv).Select(p => $"[{p.Owner}].{p.Name}={p.Value}"))
            : "";
    }

    private static object? Invoke(object target, string member, params object?[] args) =>
        target.GetType().InvokeMember(member, BindingFlags.InvokeMethod, null, target, args);
    private static object Get(object target, string member) =>
        target.GetType().InvokeMember(member, BindingFlags.GetProperty, null, target, null)!;
    private static object Item(object collection, string name) =>
        collection.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, collection, [name])!;
    private static void Set(object target, string member, object value) =>
        target.GetType().InvokeMember(member, BindingFlags.SetProperty, null, target, [value]);
}
