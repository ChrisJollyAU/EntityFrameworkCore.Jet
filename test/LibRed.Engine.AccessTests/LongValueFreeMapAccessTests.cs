using System.Data.OleDb;
using LibRed.Catalog;
using LibRed.Pages;
using LibRed.Storage;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// A long-value page leaves its column's free-pages map once it cannot hold a 256-byte value and its slot — 257
/// bytes free or fewer — the same through ACE and LibRed (docs/format/long-values.md). Two values fill the page to
/// just either side of the line: a 2000-byte one, then one that leaves <c>remaining</c> bytes.
/// </summary>
[Collection(AceCollection.Name)]
public class LongValueFreeMapAccessTests(ITestOutputHelper output) : TempDatabaseTest
{
    [Theory]
    [InlineData(256)]
    [InlineData(257)]
    [InlineData(258)]
    [InlineData(259)]
    public void A_page_leaves_the_free_map_where_ace_takes_it_out(int remaining) =>
        _ = Matches(
        [
            "CREATE TABLE L (Id LONG, M LONGBINARY)",
            $"INSERT INTO L VALUES (1, 0x{new string('A', 4000)})",
            $"INSERT INTO L VALUES (2, 0x{new string('B', 2 * (2078 - remaining))})",
        ]);

    // A memo set to the value it holds is written again, with a fresh stamp (docs/format/long-values.md): its rows
    // change where its pages and slots do not.
    [Fact]
    public void A_memo_set_to_itself_is_written_again_as_ace_writes_it()
    {
        string[] stored = ["CREATE TABLE L (Id LONG, M LONGTEXT)", $"INSERT INTO L VALUES (1, '{new string('x', 3000)}')"];
        string once = Matches(stored);
        Assert.Equal(once, Matches(stored));   // the same statements leave the same bytes, so a difference is the UPDATE's
        Assert.NotEqual(once, Matches([.. stored, "UPDATE L SET M = M"]));
    }

    /// <summary>Runs <paramref name="statements"/> through both engines, asserts their pages match, and returns ACE's.</summary>
    private string Matches(string[] statements)
    {
        string northwind = Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");
        string ace = TemporaryDatabase.CopyPath(northwind, "lvfree-ace-"), libred = TemporaryDatabase.CopyPath(northwind, "lvfree-lib-");
        try
        {
            using (OleDbConnection connection = AceTestDatabase.Open(ace))
                foreach (string sql in statements)
                {
                    using OleDbCommand command = connection.CreateCommand();
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            using (var db = JetDatabase.Open(libred, readOnly: false))
            {
                var engine = new QueryEngine(db);
                foreach (string sql in statements) engine.ExecuteNonQuery(sql);
            }

            string expected = Maps(ace), actual = Maps(libred);
            output.WriteLine($"ACE {expected}, LibRed {actual}");
            Assert.Equal(expected, actual);
            return expected;
        }
        finally
        {
            TemporaryDatabase.Delete(ace);
            TemporaryDatabase.Delete(libred);
        }
    }

    /// <summary>Each of the column's long-value pages in turn: its free space, slot directory and a hash of its rows'
    /// bytes, and whether its free-pages map still names it.</summary>
    private static string Maps(string path)
    {
        using var db = JetDatabase.Open(path, readOnly: true);
        Table table = db.OpenTable("L");
        var tdef = new TableDefinition();
        tdef.Read(table.Channel.ReadPage(table.Definition.DefinitionPage), table.Channel.Format);
        int id = table.Definition.RequireColumn("M").ColumnId;
        var maps = new UsageMap(table.Channel, table.Definition);
        var freePages = maps.PagesInMap(tdef.LongValueFreeMaps[id].Row, tdef.LongValueFreeMaps[id].Page).ToHashSet();
        return string.Join(" | ", maps.PagesInMap(tdef.LongValueOwnedMaps[id].Row, tdef.LongValueOwnedMaps[id].Page).Order().Select(page =>
        {
            byte[] bytes = table.Channel.ReadPage(page).Span.ToArray();
            int space = DataPage.ReadFreeSpace(bytes, table.Channel.Format);
            var directory = Enumerable.Range(0, DataPage.ReadRowCount(bytes, table.Channel.Format))
                .Select(i => DataPage.ReadSlot(bytes, table.Channel.Format, i)).ToList();
            string slots = string.Join(" ", directory
                .Select(s => ((int)s.Flags | s.Offset).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)));
            // And the rows themselves, whose header carries the stamp a rewrite of the chain mints (docs/format/long-values.md).
            string rows = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                bytes.AsSpan(directory.Count == 0 ? bytes.Length : directory.Min(s => s.Offset))))[..12];
            return $"{space} bytes free, slots [{slots}], rows {rows}, {(freePages.Contains(page) ? "in the free map" : "out of it")}";
        }));
    }
}