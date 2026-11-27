using System.Data.OleDb;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// How ACE tells two object names apart: as text in the database's collation (measured in this General-Legacy
/// fixture over every BMP character with a case mapping, and over characters the collation gives no weight). Each pair
/// is run through ACE and LibRed alike — a table named with the first, then found and created by the second; a column
/// likewise.
/// </summary>
[Collection(AceCollection.Name)]
public class ObjectNameAccessTests : TempDatabaseTest
{
    // Code points, '+' between the characters of a longer name, so a lookalike pair stays readable. aceAddsTwin: ACE's
    // ADD COLUMN takes the second name beside the first though the collation keys them equal (see the spec,
    // system-catalog.md); LibRed refuses it.
    public static TheoryData<string, string, bool, bool> Pairs => new()
    {
        { "0061", "0041", true, false },              // a / A
        { "0061", "FF41", true, false },              // a / fullwidth a
        { "30A2", "FF71", true, false },              // katakana / halfwidth katakana A
        { "3042", "30A2", true, false },              // hiragana / katakana A
        { "0073+0073", "00DF", true, false },         // ss / sharp s
        { "0061+0065", "00E6", true, false },         // ae / ae ligature
        { "0032", "00B2", true, false },              // 2 / superscript 2
        { "00E5", "212B", true, false },              // a-ring / Angstrom sign
        { "13A0", "AB70", true, true },               // Cherokee A, upper / lower
        { "0070+0072+0065", "0070+0072+0065+021B", true, true },   // pre / pret-comma: t-comma has no weight
        { "0063+0064", "0063+3400+0064", true, true },             // cd / c<CJK Ext A>d
        { "0078+D83D+DCDE", "0078+D83D+DCE7", true, true },        // two emoji
        { "0041", "0041+0020", true, true },          // a trailing space
        { "0061", "00E1", false, false },             // a / a-acute
        { "00B5", "039C", false, false },             // micro sign / Greek capital Mu: a case fold pairs them, the collation does not
        { "0180", "0243", false, false },             // b-stroke / B-stroke
        { "0069", "0131", false, false },             // i / dotless i
        { "0061+0062", "0061+00AD+0062", false, false },           // ab / a<soft hyphen>b
    };

    [Theory, MemberData(nameof(Pairs))]
    public void A_name_is_the_same_name_when_the_collation_keys_it_equal(string a, string b, bool same, bool aceAddsTwin)
    {
        string expected = same ? "1" : "0";
        (string[] ace, string[] libRed) = RunBoth(Decode(a), Decode(b));
        Assert.Equal([expected, expected, expected, same && !aceAddsTwin ? "1" : "0"], ace);
        // LibRed refuses every column the collation keys equal to one the table has, ACE's twins included: ACE then
        // resolves both names to the first column, reading and writing it for the second.
        Assert.Equal([expected, expected, expected, expected], libRed);
    }

    // A table ACE made holding two such columns: ACE resolves the second name to the first column (an UPDATE of the
    // second writes the first). LibRed refuses the reference instead, and reads the table by position as ACE does.
    [Fact]
    public void A_reference_that_names_two_columns_ACE_made_is_refused()
    {
        string northwind = Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");
        string path = TemporaryDatabase.CopyPath(northwind, "names-twin-");
        string first = "pre", second = Decode("0070+0072+0065+021B");
        try
        {
            using (OleDbConnection connection = AceTestDatabase.Open(path))
            {
                void Exec(string sql)
                {
                    using var c = connection.CreateCommand();
                    c.CommandText = sql;
                    c.ExecuteNonQuery();
                }
                Exec($"CREATE TABLE [T] ([{first}] LONG, [{second}] LONG)");
                Exec($"INSERT INTO [T] VALUES (1, 2)");
                using var read = connection.CreateCommand();
                read.CommandText = $"SELECT [{second}] FROM [T]";
                Assert.Equal(1, Convert.ToInt32(read.ExecuteScalar()));   // the first column's value
            }
            OleDbConnection.ReleaseObjectPool();

            using var db = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(db);
            Assert.Equal([1, 2], engine.ExecuteQuery("SELECT * FROM [T]").Rows.Single().Select(Convert.ToInt32));
            Assert.Throws<InvalidOperationException>(() => engine.ExecuteQuery($"SELECT [{second}] FROM [T]").Rows.ToList());
            Assert.Throws<InvalidOperationException>(() => engine.ExecuteNonQuery($"UPDATE [T] SET [{second}] = 3"));
        }
        finally
        {
            OleDbConnection.ReleaseObjectPool();
            TemporaryDatabase.Delete(path);
        }
    }

    private static string Decode(string codes) =>
        string.Concat(codes.Split('+').Select(h => (char)Convert.ToInt32(h, 16)));

    private static (string[] Ace, string[] LibRed) RunBoth(string first, string second)
    {
        string northwind = Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");
        string ace = TemporaryDatabase.CopyPath(northwind, "names-ace-"), libred = TemporaryDatabase.CopyPath(northwind, "names-lib-");
        try
        {
            string[] aceResult;
            using (OleDbConnection connection = AceTestDatabase.Open(ace))
                aceResult = Run(first, second, sql =>
                {
                    using var c = connection.CreateCommand();
                    c.CommandText = sql;
                    if (sql.StartsWith("SELECT", StringComparison.Ordinal)) c.ExecuteScalar(); else c.ExecuteNonQuery();
                });
            string[] libResult;
            using (var db = JetDatabase.Open(libred, readOnly: false))
            {
                var engine = new QueryEngine(db);
                libResult = Run(first, second, sql =>
                {
                    if (sql.StartsWith("SELECT", StringComparison.Ordinal)) _ = engine.ExecuteQuery(sql).Rows.ToList();
                    else engine.ExecuteNonQuery(sql);
                });
            }
            return (aceResult, libResult);
        }
        finally
        {
            OleDbConnection.ReleaseObjectPool();
            TemporaryDatabase.Delete(ace);
            TemporaryDatabase.Delete(libred);
        }
    }

    // Table lookup, table create, column lookup, column create: "1" where the second name is taken as the first (found,
    // or refused as existing), "0" where it is another name (not found, or created beside the first).
    private static string[] Run(string first, string second, Action<string> exec)
    {
        string Try(string sql, bool isCreate)
        {
            try { exec(sql); return isCreate ? "0" : "1"; }
            catch (Exception e) when (e.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                || e.Message.Contains("more than once", StringComparison.OrdinalIgnoreCase)) { return "1"; }
            catch (Exception e) when (e.Message.Contains("cannot find", StringComparison.OrdinalIgnoreCase)
                || e.Message.Contains("No value given", StringComparison.OrdinalIgnoreCase)
                || e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                || e.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
                || e.Message.Contains("Could not find", StringComparison.OrdinalIgnoreCase)) { return "0"; }
        }
        exec($"CREATE TABLE [P{first}] (X LONG)");
        string tableLookup = Try($"SELECT COUNT(*) FROM [P{second}]", isCreate: false);
        string tableCreate = Try($"CREATE TABLE [P{second}] (X LONG)", isCreate: true);
        exec($"CREATE TABLE [C] ([c{first}] LONG)");
        exec("INSERT INTO [C] VALUES (1)");   // a row, so a lookup is evaluated, not only bound
        string columnLookup = Try($"SELECT [c{second}] FROM [C]", isCreate: false);
        string columnCreate = Try($"ALTER TABLE [C] ADD COLUMN [c{second}] LONG", isCreate: true);
        return [tableLookup, tableCreate, columnLookup, columnCreate];
    }
}
