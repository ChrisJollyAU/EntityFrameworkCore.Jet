using System.Data.OleDb;
using LibRed;
using LibRed.Catalog;
using LibRed.Storage;
using Xunit;

namespace LibRed.Core.Tests;

[Collection(AceCollection.Name)]
public class TextKeyEncodingTests
{
    private static OleDbConnection OpenOleDb(string path) => AceTestDatabase.Open(path);

    [Fact]
    public void Encoded_text_keys_match_access_byte_for_byte()
    {
        // Build a real text-PK table via Access, then check our encoder reproduces each stored key.
        string[] values =
        [
            "A", "B", "C", "M", "Z", "AA", "AB", "ABC", "BA",
            "Apple", "Banana", "Cherry", "A B", " A",
            "0", "1", "9", "Order123", "Customer", "Z9",
            "(paren)", "a.b", "x/y", "p+q", "k=v",
            // Ignorable apostrophe/hyphen in various positions.
            "'", "-", "A'", "'A", "A-B", "O'Brien", "IT'S", "ANNE-MARIE", "A''B", "x-y-z",
            // Accented Latin-1: the accent sorts with its base letter and adds a secondary weight.
            "México D.F.", "Montréal", "München", "São Paulo", "Résumé", "Café",
            "Niño", "Zürich", "Åre", "Ça", "É", "Ø", "Ñ", "Ç", "Ü", "Ã", "Æ",
            // Multi-letter expansions (ß=SS, Þ=TH), incl. a ligature followed by ignorable hyphens.
            "ß", "Straße", "Þor", "NuNuCa Nuß-Nougat-Creme", "Aß-B",
        ];

        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "libred-textkey-");
        try
        {
            using (var conn = OpenOleDb(path))
            {
                using (var c = conn.CreateCommand())
                { c.CommandText = "CREATE TABLE TKey (K varchar(30) PRIMARY KEY, V int)"; c.ExecuteNonQuery(); }
                for (int i = 0; i < values.Length; i++)
                {
                    using var c = conn.CreateCommand();
                    c.CommandText = "INSERT INTO TKey (K, V) VALUES (?, ?)";
                    c.Parameters.AddWithValue("k", values[i]);
                    c.Parameters.AddWithValue("v", i);
                    c.ExecuteNonQuery();
                }
            }

            using var db = JetDatabase.Open(path);
            var table = db.OpenTable("TKey");
            var def = table.Definition;
            IndexDef pk = def.Indexes.Single(i => i.IsPrimaryKey);
            int kIdx = def.FindColumn("K")!.Index;
            var decoder = new RowCodec(def.Columns, db.Format);

            int checkd = 0;
            foreach (var (accessKey, rowId) in new IndexCursor(table.Channel, pk.RootPage).RawEntries())
            {
                string k = (string)decoder.Decode(db.ReadDataPage(rowId.Page).GetRow(rowId.Row))[kIdx]!;

                var values2 = new object?[def.Columns.Count];
                values2[kIdx] = k;
                byte[] ours = IndexKeyCodec.Encode(pk.Columns, values2);

                Assert.True(accessKey.AsSpan().SequenceEqual(ours),
                    $"'{k}': access={Convert.ToHexString(accessKey)} ours={Convert.ToHexString(ours)}");
                checkd++;
            }
            Assert.Equal(values.Length, checkd);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // Two things only a longer string shows, in both General orders. A run of kana closes with 02, a 80 for every seven
    // kana or part of seven, FF and the 80s again — counted over the kana, a prolonged mark among them, not the
    // characters. A combining mark with nothing before it gives its secondary to the first weight, or sets it beside
    // the first letter's own accent, and takes no position: an apostrophe after U+0301 + a sits at 0B, as after a alone.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Kana_runs_and_leading_marks_match_access(bool version1)
    {
        string R(string s, int n) => string.Concat(Enumerable.Repeat(s, n));
        string[] values =
        [
            R("あ", 7), R("あ", 8), R("あ", 14), R("あ", 15), R("あ", 22), R("ア", 8), R("ぁ", 15), R("が", 8),
            "a" + R("あ", 7), "a" + R("あ", 8), R("あa", 8), R("a", 14) + "あ", R("かー", 4), R("かー", 8),
            "́", "́a", "́́a", "́a'b", "́'b", "́a-b", "̀a'b", "́a'b'c",
            "́ab'c", "á'b", "x́a'b", "'́a", "-́a", "'́a'b",
            "́é", "́é'b", "́が", "́がa", "́ア'b", "々́a", "́ß", "́Ä-b", "́̀é",
            "́́ä-b", "̈̈á-b", "́ǖ-b", "̧ç-b",
        ];

        string path = TemporaryDatabase.CreatePath("libred-textkey-tail-");
        JetDatabase.Create(path, collation: version1 ? Collation.General : Collation.GeneralLegacy);
        try
        {
            using (var conn = OpenOleDb(path))
            {
                using (var c = conn.CreateCommand())
                { c.CommandText = "CREATE TABLE TKey (V int, K varchar(60))"; c.ExecuteNonQuery(); }
                using (var c = conn.CreateCommand())
                { c.CommandText = "CREATE INDEX IK ON TKey (K)"; c.ExecuteNonQuery(); }
                for (int i = 0; i < values.Length; i++)
                {
                    using var c = conn.CreateCommand();
                    c.CommandText = "INSERT INTO TKey (V, K) VALUES (?, ?)";
                    c.Parameters.AddWithValue("v", i);
                    c.Parameters.AddWithValue("k", values[i]);
                    c.ExecuteNonQuery();
                }
            }
            OleDbConnection.ReleaseObjectPool();

            using var db = JetDatabase.Open(path);
            var table = db.OpenTable("TKey");
            IndexDef index = table.Definition.Indexes.Single(i => i.Name == "IK");
            var byRow = table.RowsWhere([0, 1], _ => true).ToDictionary(r => r.Id, r => r.Values);
            int checkd = 0;
            foreach (var (accessKey, rowId) in new IndexCursor(table.Channel, index.RootPage).RawEntries())
            {
                byte[] ours = IndexKeyCodec.Encode(index.Columns, byRow[rowId]);
                Assert.True(accessKey.AsSpan().SequenceEqual(ours),
                    $"'{byRow[rowId][1]}': access={Convert.ToHexString(accessKey)} ours={Convert.ToHexString(ours)}");
                checkd++;
            }
            Assert.Equal(values.Length, checkd);
        }
        finally
        {
            OleDbConnection.ReleaseObjectPool();
            TemporaryDatabase.Delete(path);
        }
    }

    [Fact]
    public void Encoded_descending_text_keys_match_access_byte_for_byte()
    {
        string[] values = ["A", "B", "AB", "Z", "Apple", "A-B", "O'Brien", "0", "Order9"];

        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "libred-textdesc-");
        try
        {
            using (var conn = OpenOleDb(path))
            {
                using (var c = conn.CreateCommand())
                { c.CommandText = "CREATE TABLE TD (Id int PRIMARY KEY, K varchar(30))"; c.ExecuteNonQuery(); }
                using (var c = conn.CreateCommand())
                { c.CommandText = "CREATE INDEX ixK ON TD (K DESC)"; c.ExecuteNonQuery(); }
                for (int i = 0; i < values.Length; i++)
                {
                    using var c = conn.CreateCommand();
                    c.CommandText = "INSERT INTO TD (Id, K) VALUES (?, ?)";
                    c.Parameters.AddWithValue("id", i);
                    c.Parameters.AddWithValue("k", values[i]);
                    c.ExecuteNonQuery();
                }
            }

            using var db = JetDatabase.Open(path);
            var table = db.OpenTable("TD");
            var def = table.Definition;
            IndexDef ixK = def.Indexes.First(i => i.Columns.Any(c => c.Column.Name == "K"));
            Assert.False(ixK.Columns.First(c => c.Column.Name == "K").Ascending); // descending
            int kIdx = def.FindColumn("K")!.Index;
            var decoder = new RowCodec(def.Columns, db.Format);

            int checkd = 0;
            foreach (var (accessKey, rowId) in new IndexCursor(table.Channel, ixK.RootPage).RawEntries())
            {
                string k = (string)decoder.Decode(db.ReadDataPage(rowId.Page).GetRow(rowId.Row))[kIdx]!;
                var vals = new object?[def.Columns.Count];
                vals[kIdx] = k;
                byte[] ours = IndexKeyCodec.Encode(ixK.Columns, vals);
                Assert.True(accessKey.AsSpan().SequenceEqual(ours),
                    $"'{k}': access={Convert.ToHexString(accessKey)} ours={Convert.ToHexString(ours)}");
                checkd++;
            }
            Assert.Equal(values.Length, checkd);
        }
        finally { TemporaryDatabase.Delete(path); }
    }
}