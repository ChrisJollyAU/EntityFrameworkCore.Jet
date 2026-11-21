using LibRed;
using LibRed.Engine;
using LibRed.IO;
using LibRed.Storage;
using Xunit;

namespace LibRed.Engine.Tests;

public class CreateIndexTests
{
    private static string Fresh()
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "cidx-");
        return path;
    }

    private static void Table(QueryEngine e) =>
        e.ExecuteNonQuery("CREATE TABLE `T` (`Id` INTEGER PRIMARY KEY, `Name` VARCHAR(50), `Age` INTEGER)");

    [Fact]
    public void Create_index_round_trips_and_inserts_maintain_it()
    {
        string path = Fresh();
        try
        {
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                var e = new QueryEngine(db);
                Table(e);
                e.ExecuteNonQuery("CREATE INDEX `IX_Name` ON `T` (`Name`)");
                e.ExecuteNonQuery("CREATE UNIQUE INDEX `UX_Age` ON `T` (`Age`)");
                e.ExecuteNonQuery("INSERT INTO `T` (`Id`, `Name`, `Age`) VALUES (1, 'a', 30)");
                e.ExecuteNonQuery("INSERT INTO `T` (`Id`, `Name`, `Age`) VALUES (2, 'b', 40)");
            }
            using (var db = JetDatabase.Open(path))
            {
                var t = db.Catalog.FindTable("T")!;
                Assert.Contains(t.Indexes, ix => ix.Name == "IX_Name" && !ix.IsUnique && !ix.IsPrimaryKey
                    && ix.Columns.Select(c => c.Column.Name).SequenceEqual(["Name"]));
                Assert.Contains(t.Indexes, ix => ix.Name == "UX_Age" && ix.IsUnique && !ix.IsPrimaryKey
                    && ix.Columns.Select(c => c.Column.Name).SequenceEqual(["Age"]));
                Assert.Equal(2, new QueryEngine(db).ExecuteQuery("SELECT `Id` FROM `T`").Rows.Count());
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void Create_index_multi_column_and_with_primary()
    {
        string path = Fresh();
        try
        {
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                var e = new QueryEngine(db);
                e.ExecuteNonQuery("CREATE TABLE `T` (`A` INTEGER, `B` INTEGER, `C` INTEGER)");
                e.ExecuteNonQuery("CREATE INDEX `IX_AB` ON `T` (`A`, `B`)");
                e.ExecuteNonQuery("CREATE INDEX `PK_T` ON `T` (`C`) WITH PRIMARY");
            }
            using (var db = JetDatabase.Open(path))
            {
                var t = db.Catalog.FindTable("T")!;
                Assert.Contains(t.Indexes, ix => ix.Name == "IX_AB"
                    && ix.Columns.Select(c => c.Column.Name).SequenceEqual(["A", "B"]));
                Assert.Contains(t.Indexes, ix => ix.Name == "PK_T" && ix.IsPrimaryKey && ix.IsUnique);
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // WITH IGNORE NULL: the index is created (flag 0x02, reader exposes IgnoreNulls), and a row whose
    // indexed column is null is excluded from the index — the insert still succeeds.
    [Fact]
    public void With_ignore_null_creates_sparse_index_and_skips_null_rows()
    {
        string path = Fresh();
        try
        {
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                var e = new QueryEngine(db);
                Table(e);
                e.ExecuteNonQuery("CREATE INDEX `IX_Age` ON `T` (`Age`) WITH IGNORE NULL");
                e.ExecuteNonQuery("INSERT INTO `T` (`Id`, `Name`, `Age`) VALUES (1, 'a', 30)"); // indexed
                e.ExecuteNonQuery("INSERT INTO `T` (`Id`, `Name`, `Age`) VALUES (2, 'b', NULL)"); // excluded
                e.ExecuteNonQuery("INSERT INTO `T` (`Id`, `Name`, `Age`) VALUES (3, 'c', 40)"); // indexed
            }
            using (var db = JetDatabase.Open(path))
            {
                var ix = db.Catalog.FindTable("T")!.Indexes.Single(x => x.Name == "IX_Age");
                Assert.True(ix.IgnoreNulls);
                // All rows are still in the table (the null one is just absent from the index).
                Assert.Equal(3, new QueryEngine(db).ExecuteQuery("SELECT `Id` FROM `T`").Rows.Count());
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A multi-column WITH IGNORE NULL index leaves out only a key whose every column is Null. Rows (1, Null),
    // (Null, Null), (1, 2) by Id 1-3, filled by inserts or built over the rows, then one statement; the index holds
    // the listed rows in key order, with the unique-entry count beside it. Values measured against ACE in
    // IndexUniqueEntryCountAccessTests.An_ignore_null_index_leaves_out_only_an_all_null_key.
    [Theory]
    [InlineData(true, "", "1,3", 2)]
    [InlineData(false, "", "1,3", 2)]
    [InlineData(true, "DELETE FROM T WHERE Id = 1", "3", 2)]
    [InlineData(false, "DELETE FROM T WHERE Id = 1", "3", 1)]
    [InlineData(true, "UPDATE T SET A = NULL WHERE Id = 1", "3", 2)]                // held row becomes all-Null, leaves
    [InlineData(false, "UPDATE T SET A = NULL WHERE Id = 1", "3", 1)]
    [InlineData(true, "UPDATE T SET A = 1 WHERE Id = 2", "1,2,3", 2)]               // all-Null row enters
    [InlineData(false, "UPDATE T SET A = 1 WHERE Id = 2", "1,2,3", 2)]
    [InlineData(true, "UPDATE T SET A = NULL, B = 2 WHERE Id = 1", "1,3", 2)]        // partly-Null to partly-Null
    [InlineData(false, "UPDATE T SET A = NULL, B = 2 WHERE Id = 1", "1,3", 1)]
    public void With_ignore_null_multi_column_leaves_out_only_all_null_keys(bool indexFirst, string sql, string ids, int unique)
    {
        string path = Fresh();
        try
        {
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                var e = new QueryEngine(db);
                string[] rows = ["INSERT INTO T VALUES (1, 1, NULL)", "INSERT INTO T VALUES (2, NULL, NULL)", "INSERT INTO T VALUES (3, 1, 2)"];
                const string index = "CREATE INDEX ixAB ON T (A, B) WITH IGNORE NULL";
                foreach (string statement in (string[])
                    ["CREATE TABLE T (Id LONG CONSTRAINT pkT PRIMARY KEY, A LONG, B LONG)",
                     .. indexFirst ? [index, .. rows] : (string[])[.. rows, index],
                     .. sql.Length > 0 ? [sql] : (string[])[]])
                    e.ExecuteNonQuery(statement);
            }
            using (var db = JetDatabase.Open(path))
            {
                var table = db.Catalog.FindTable("T")!;
                var ix = table.Indexes.Single(x => x.Name == "ixAB");
                int idColumn = table.FindColumn("Id")!.Index;
                var idByRow = db.OpenTable("T").Rows().WithIds().ToDictionary(r => r.Id.Packed, r => r.Values[idColumn]);
                using var channel = PageChannel.Open(path, readOnly: true);
                Assert.Equal(ids, string.Join(",", new IndexCursor(channel, ix.RootPage).RawEntries().Select(x => idByRow[x.Row.Packed])));
                Assert.Equal(unique, ix.UniqueEntryCount);
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A UNIQUE WITH IGNORE NULL index accepts the partly-Null key (1, Null) twice and holds both rows, built over
    // the rows or filled by inserts. Measured against ACE in
    // IndexUniqueEntryCountAccessTests.A_unique_ignore_null_index_given_a_partly_null_key_twice.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_unique_ignore_null_index_accepts_a_partly_null_key_twice(bool indexFirst)
    {
        string path = Fresh();
        try
        {
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                var e = new QueryEngine(db);
                string[] rows = ["INSERT INTO T VALUES (1, 1, NULL)", "INSERT INTO T VALUES (2, 1, NULL)"];
                const string index = "CREATE UNIQUE INDEX ixAB ON T (A, B) WITH IGNORE NULL";
                foreach (string statement in (string[])
                    ["CREATE TABLE T (Id LONG CONSTRAINT pkT PRIMARY KEY, A LONG, B LONG)",
                     .. indexFirst ? [index, .. rows] : (string[])[.. rows, index]])
                    e.ExecuteNonQuery(statement);
            }
            using (var db = JetDatabase.Open(path))
            {
                var table = db.Catalog.FindTable("T")!;
                var ix = table.Indexes.Single(x => x.Name == "ixAB");
                int idColumn = table.FindColumn("Id")!.Index;
                var idByRow = db.OpenTable("T").Rows().WithIds().ToDictionary(r => r.Id.Packed, r => r.Values[idColumn]);
                using var channel = PageChannel.Open(path, readOnly: true);
                Assert.Equal("1,2", string.Join(",", new IndexCursor(channel, ix.RootPage).RawEntries().Select(x => idByRow[x.Row.Packed])));
                Assert.Equal(1, ix.UniqueEntryCount);
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A descending index: the index-data block records the column as descending (Ascending = false),
    // and inserts encode reversed key bytes (IndexKeyCodec handles the inversion).
    [Fact]
    public void Descending_index_records_direction_and_inserts()
    {
        string path = Fresh();
        try
        {
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                var e = new QueryEngine(db);
                Table(e);
                e.ExecuteNonQuery("CREATE INDEX `IX_AgeDesc` ON `T` (`Age` DESC)");
                e.ExecuteNonQuery("INSERT INTO `T` (`Id`, `Name`, `Age`) VALUES (1, 'a', 30)");
                e.ExecuteNonQuery("INSERT INTO `T` (`Id`, `Name`, `Age`) VALUES (2, 'b', 40)");
            }
            using (var db = JetDatabase.Open(path))
            {
                var ix = db.Catalog.FindTable("T")!.Indexes.Single(x => x.Name == "IX_AgeDesc");
                var (col, ascending) = ix.Columns.Single();
                Assert.Equal("Age", col.Name);
                Assert.False(ascending); // recorded as descending
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // CREATE INDEX on a populated table back-fills the new index over the existing rows.
    [Fact]
    public void Create_index_on_non_empty_table_backfills()
    {
        string path = Fresh();
        try
        {
            using (var db = JetDatabase.Open(path, readOnly: false))
            {
                var e = new QueryEngine(db);
                Table(e);
                e.ExecuteNonQuery("INSERT INTO `T` (`Id`, `Name`, `Age`) VALUES (1, 'a', 30)");
                e.ExecuteNonQuery("INSERT INTO `T` (`Id`, `Name`, `Age`) VALUES (2, 'b', 40)");
                e.ExecuteNonQuery("CREATE INDEX `IX_Name` ON `T` (`Name`)"); // no throw
            }

            using (var db = JetDatabase.Open(path))
            {
                var e = new QueryEngine(db);
                Assert.Equal(2, e.ExecuteQuery("SELECT * FROM `T`").Rows.Count()); // rows intact
                Assert.Equal(1, e.ExecuteQuery("SELECT `Id` FROM `T` WHERE `Name` = 'b'").Rows.Count());
            }
        }
        finally { TemporaryDatabase.Delete(path); }
    }
}