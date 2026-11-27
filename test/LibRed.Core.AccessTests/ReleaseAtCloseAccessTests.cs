using System.Data.OleDb;
using LibRed.Catalog;
using LibRed.Pages;
using LibRed.Storage;
using Xunit;

namespace LibRed.Core.Tests;

// A session's freed pages go back to the global free map at close, and the close lengthens the released-pages map
// to cover the highest page released (docs/format/page-05-usage-maps.md §9.1). The same DROP TABLE through ACE and
// through LibRed, each on its own copy, must leave both global maps — wherever page 0 points — byte for byte the same.
[Collection(AceCollection.Name)]
public class ReleaseAtCloseAccessTests : TempDatabaseTest
{
    private static readonly Formats.JetFormatBase Format = TestDatabases.FormatOf(TestDatabases.NorthwindAccdb);

    [Fact]
    public void A_dropped_table_leaves_both_global_maps_as_ace_leaves_them()
    {
        string start = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-start-");
        using (OleDbConnection connection = AceTestDatabase.Open(start))
        {
            Exec(connection, "CREATE TABLE Doomed (Id LONG CONSTRAINT pk PRIMARY KEY, M MEMO)");
            for (int i = 1; i <= 40; i++)
            {
                using OleDbCommand insert = connection.CreateCommand();
                insert.CommandText = "INSERT INTO Doomed (Id, M) VALUES (?, ?)";
                insert.Parameters.Add("id", OleDbType.Integer).Value = i;
                insert.Parameters.Add("m", OleDbType.LongVarWChar, 20_000).Value = new string((char)('a' + i % 26), 20_000);
                insert.ExecuteNonQuery();
            }
        }
        // Both maps are wherever page 0's pointers put them; neither engine moves them on a drop.
        (int Row, int Page) free, released;
        int releasedLengthBefore;
        using (var db = JetDatabase.Open(start))
        {
            (free, released) = (db.DefinitionPage.FreePagesMap, db.DefinitionPage.ReleasedPagesMap);
            releasedLengthBefore = TestDatabases.GlobalMap(db.Channel, Format.ReleasedPagesMapPointerOffset).Slot.Length;
        }
        byte[] before = TestDatabases.ReadPage(start, free.Page);

        string ace = TemporaryDatabase.CopyPath(start, "release-ace-");
        using (OleDbConnection connection = AceTestDatabase.Open(ace))
            Exec(connection, "DROP TABLE Doomed");

        string libred = TemporaryDatabase.CopyPath(start, "release-lib-");
        using (var db = JetDatabase.Open(libred, readOnly: false))
            Assert.True(db.DropTable("Doomed"));

        var releasedLengths = new List<int>();
        foreach (string path in new[] { ace, libred })
        {
            using var db = JetDatabase.Open(path);
            Assert.Equal(free, db.DefinitionPage.FreePagesMap);
            Assert.Equal(released, db.DefinitionPage.ReleasedPagesMap);
            releasedLengths.Add(TestDatabases.GlobalMap(db.Channel, Format.ReleasedPagesMapPointerOffset).Slot.Length);
        }
        Assert.True(releasedLengths[0] > releasedLengthBefore, "the drop did not lengthen ACE's released map");
        Assert.Equal(releasedLengths[0], releasedLengths[1]);

        byte[] aceReleased = TestDatabases.ReadPage(ace, released.Page), libredReleased = TestDatabases.ReadPage(libred, released.Page);

        byte[] acePage = TestDatabases.ReadPage(ace, free.Page), libredPage = TestDatabases.ReadPage(libred, free.Page);
        Assert.True(acePage.AsSpan().SequenceEqual(libredPage), FreeMapDifference(start, free, before, acePage, libredPage));
        if (released.Page != free.Page)
            Assert.True(aceReleased.AsSpan().SequenceEqual(libredReleased), "the released map's holder differs");
    }

    // LibRed beside a connected ACE user deletes a row whose memo is a chained long value: as ACE beside another ACE user
    // does (measured), the chain goes into neither global map at the statement, into the released map at LibRed's close,
    // and into the free map at ACE's — the last user's — close.
    [Fact]
    public void LibRed_freeing_a_chain_beside_ACE_holds_it_for_the_released_map_and_ACEs_close_frees_it()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-beside-ace-");
        try
        {
            int[][] chains = CreateChainedRows(path, 1);
            using (OleDbConnection ace = AceTestDatabase.Open(path))
            {
                Assert.Equal(1, CountDoc(ace));
                using (var db = JetDatabase.Open(path, readOnly: false))
                {
                    Assert.Equal(2, db.Channel.UserNumber);
                    Table table = db.OpenTable("Doc");
                    table.Delete(table.Rows().WithIds().Single().Id);
                    Assert.Empty(Maps(db.Channel).Free.Intersect(chains[0]));
                    Assert.Empty(Maps(db.Channel).Released.Intersect(chains[0]));
                }

                (HashSet<int> free, HashSet<int> released) = MapsOf(path);
                Assert.Empty(free.Intersect(chains[0]));
                Assert.Superset(chains[0].ToHashSet(), released);
                AceRefreshes(ace);
            }

            Assert.True(Poll(() => MapsOf(path) is var maps && maps.Released.Count == 0 && maps.Free.IsSupersetOf(chains[0])),
                "ACE's close did not move the chain into the free map.");
            using var reopened = JetDatabase.Open(path, readOnly: false);
            TestDatabases.AssertPageOwnership(reopened);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // The reverse: ACE parks a chain in the released map (its DELETE and close beside a second ACE connection); LibRed
    // opens, frees a chain of its own and closes while that ACE connection is still open, and adds its chain to the map
    // without moving ACE's; the last ACE close frees both.
    [Fact]
    public void LibRed_closing_beside_ACE_leaves_the_pages_ACE_parked_in_the_released_map()
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "release-ace-parked-");
        try
        {
            int[][] chains = CreateChainedRows(path, 2);
            using (OleDbConnection stays = AceTestDatabase.Open(path))
            {
                Assert.Equal(2, CountDoc(stays));
                using (OleDbConnection parks = AceTestDatabase.Open(path))
                {
                    using OleDbCommand delete = parks.CreateCommand();
                    delete.CommandText = "DELETE FROM Doc WHERE Id = 1";
                    Assert.Equal(1, delete.ExecuteNonQuery());
                }
                Assert.True(Poll(() => MapsOf(path).Released.IsSupersetOf(chains[0])), "ACE's close did not park its chain.");

                using (var db = JetDatabase.Open(path, readOnly: false))
                {
                    Table table = db.OpenTable("Doc");
                    table.Delete(table.Rows().WithIds().Single().Id);
                }

                (HashSet<int> free, HashSet<int> released) = MapsOf(path);
                Assert.Superset(chains[0].Concat(chains[1]).ToHashSet(), released);
                Assert.Empty(free.Intersect(chains[0].Concat(chains[1])));
                AceRefreshes(stays);
            }

            Assert.True(Poll(() => MapsOf(path) is var maps && maps.Released.Count == 0
                && maps.Free.IsSupersetOf(chains[0]) && maps.Free.IsSupersetOf(chains[1])),
                "ACE's last close did not move both chains into the free map.");
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    /// <summary>Creates table Doc with <paramref name="rows"/> rows, Id 1 up, each with a 20,000-character memo — a
    /// chained long value — through LibRed alone; returns each row's chain.</summary>
    private static int[][] CreateChainedRows(string path, int rows)
    {
        using var db = JetDatabase.Open(path, readOnly: false);
        db.CreateTable("Doc",
            [new("Id", JetDataType.Int32, 4, IsFixedLength: true),
             new("M", JetDataType.Memo, 0, IsFixedLength: false)]);
        var chains = new int[rows][];
        HashSet<int> before = [];
        for (int id = 1; id <= rows; id++)
        {
            Table table = db.OpenTable("Doc");
            table.Insert([id, new string((char)('a' + id), 20000)]);
            (int Row, int Page) owned = Assert.Single(table.Definition.LongValueOwnedMaps.Values);
            HashSet<int> pages = [.. new UsageMap(table.Channel, table.Definition).PagesInMap(owned.Row, owned.Page)];
            chains[id - 1] = [.. pages.Except(before).Order()];
            Assert.True(chains[id - 1].Length > 1);
            before = pages;
        }
        return chains;
    }

    /// <summary>The pages set in the global free and released maps, read on a read-only LibRed open of its own.</summary>
    private static (HashSet<int> Free, HashSet<int> Released) MapsOf(string path)
    {
        using var db = JetDatabase.Open(path);
        return Maps(db.Channel);
    }

    private static (HashSet<int> Free, HashSet<int> Released) Maps(IO.PageChannel channel)
    {
        HashSet<int> Pages(int pointer)
        {
            (_, _, byte[] holder, DataPage.RowSlot slot) = TestDatabases.GlobalMap(channel, pointer);
            return [.. UsageMap.PagesInRecord(channel, holder.AsSpan(slot.Offset, slot.Length), null, "A global map")];
        }
        return (Pages(Format.FreePagesMapPointerOffset), Pages(Format.ReleasedPagesMapPointerOffset));
    }

    // ACE's close merges the released map only once its periodic refresh (5 s) has seen the other user's commit — closed
    // sooner it left the pages parked — so it is given the refresh and a statement first.
    private static void AceRefreshes(OleDbConnection ace)
    {
        Thread.Sleep(TimeSpan.FromSeconds(6));
        CountDoc(ace);
    }

    private static int CountDoc(OleDbConnection ace)
    {
        using OleDbCommand command = ace.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Doc";
        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool Poll(Func<bool> done)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!done())
        {
            if (clock.Elapsed > TimeSpan.FromSeconds(5)) return false;
            Thread.Sleep(200);
        }
        return true;
    }

    /// <summary>The pages whose free bit differs between the two engines, with each page's type, owner and free
    /// bit in the file before the drop.</summary>
    private static string FreeMapDifference(string start, (int Row, int Page) free, byte[] before, byte[] acePage, byte[] libredPage)
    {
        (int beforeOffset, _) = DataPage.ReadSlot(before, Format, free.Row);
        (int offset, _) = DataPage.ReadSlot(acePage, Format, free.Row);
        int first = UsageMap.StartPage(acePage.AsSpan(offset), Format);
        Span<byte> aceBits = UsageMap.InlineBits(acePage.AsSpan(offset), Format);
        Span<byte> libredBits = UsageMap.InlineBits(libredPage.AsSpan(offset), Format);
        Span<byte> beforeBits = UsageMap.InlineBits(before.AsSpan(beforeOffset), Format);
        var lines = new List<string>();
        for (int bit = 0; bit < aceBits.Length * 8; bit++)
            if (BitmapBits.Get(aceBits, bit) != BitmapBits.Get(libredBits, bit))
            {
                int page = first + bit;
                byte[] bytes = TestDatabases.ReadPage(start, page);
                bool freeBefore = BitmapBits.Get(beforeBits, page - first);
                lines.Add($"page {page} (type 0x{bytes[0]:X2} owner {(int)DataPage.ReadOwner(bytes, Format)}, " +
                          $"{(freeBefore ? "free" : "used")} before the drop): " +
                          $"free in {(BitmapBits.Get(aceBits, bit) ? "ACE" : "LibRed")} only");
            }
        return lines.Count == 0 ? "the free map's holder differs outside the free map" : string.Join("; ", lines);
    }

    private static void Exec(OleDbConnection connection, string sql)
    {
        using OleDbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}