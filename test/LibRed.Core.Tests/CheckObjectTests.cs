using LibRed.Catalog;
using Xunit;

namespace LibRed.Core.Tests;

/// <summary>
/// Every CHECK is also an <c>MSysObjects</c> object in the Relationships container, whose <c>Database</c> records
/// what its expression reads, and what it reads is held: it cannot be dropped, altered or renamed while the check
/// stands (docs/format/system-catalog.md, measured against ACE in <c>CheckConstraintCreationAccessTests</c>).
/// </summary>
public class CheckObjectTests : TempDatabaseTest
{
    private const string Refusal =
        "DDL cannot be completed on this table because it is referenced by constraint chkX on table chkX.";

    private static string C(int count) => ((char)count).ToString();

    public static TheoryData<string[][]> Reads => new()
    {
        // NUL-free: one table, its columns in the table's order.
        new[] { new[] { "Setting", "Level", "Note" } },
        // A COUNT(*) read: a table for no column, so a 0 count.
        new[] { new[] { "Owner", "A" }, new[] { "Src" } },
        // Several tables.
        new[] { new[] { "Setting", "Level" }, new[] { "Customer", "City", "Credit" }, new[] { "Orders", "OrderID" } },
    };

    [Theory]
    [MemberData(nameof(Reads))]
    public void What_a_check_reads_round_trips_through_its_database_text(string[][] tables)
    {
        List<(string Table, IReadOnlyList<string> Columns)> reads =
            [.. tables.Select(t => (t[0], (IReadOnlyList<string>)t[1..]))];

        var back = SchemaEditor.ReadCheckReads(SchemaEditor.CheckReadsText(reads));

        Assert.Equal(reads.Select(r => r.Table), back.Select(r => r.Table));
        Assert.Equal(reads.Select(r => string.Join(",", r.Columns)), back.Select(r => string.Join(",", r.Columns)));
    }

    [Fact]
    public void The_specs_example_reads_its_own_table_then_the_subquerys()
    {
        // [Level] < (SELECT MAX(Credit) FROM Customer WHERE City = 'Rome') + 10 on Setting.
        string text = C(2) + C(7) + "Setting" + C(1) + C(5) + "Level" + C(8) + "Customer" + C(2) + C(4) + "City" + C(6) + "Credit";

        var reads = SchemaEditor.ReadCheckReads(text);

        Assert.Equal(text, SchemaEditor.CheckReadsText(reads));
        Assert.Equal(["Setting", "Customer"], reads.Select(r => r.Table));
        Assert.Equal(["Level"], reads[0].Columns);
        Assert.Equal(["City", "Credit"], reads[1].Columns);
    }

    [Theory]
    [InlineData("truncated")]
    [InlineData("trailing")]
    [InlineData("count past the end")]
    public void A_damaged_database_text_is_malformed(string damage)
    {
        string text = C(2) + C(7) + "Setting" + C(1) + C(5) + "Level" + C(8) + "Customer" + C(2) + C(4) + "City" + C(6) + "Credit";
        text = damage switch
        {
            "truncated" => text[..^1],
            "trailing" => text + C(0),
            _ => C(3) + text[1..],
        };

        var refused = Assert.Throws<InvalidDataException>(() => SchemaEditor.ReadCheckReads(text));
        Assert.Equal("A CHECK constraint's MSysObjects.Database is malformed.", refused.Message);
    }

    /// <summary>Src (Col, Other) and Owner (A, B), with chkX on Owner reading Owner.A and Src.Col — or, with
    /// <paramref name="countOnly"/>, reading Src for no column (<c>COUNT(*)</c>).</summary>
    private static JetDatabase Database(bool countOnly = false)
    {
        string path = TemporaryDatabase.CopyPath(TestDatabases.NorthwindAccdb, "chkobj-");
        var db = JetDatabase.Open(path, readOnly: false);
        db.CreateTable("Src", [Long("Col"), Long("Other")]);
        db.CreateTable("Owner", [Long("A"), Long("B")]);
        if (countOnly)
            db.AddCheckConstraint("Owner", "chkX", "(SELECT COUNT(*) FROM Src) < 10", [("Owner", []), ("Src", [])]);
        else
            db.AddCheckConstraint("Owner", "chkX", "A IN (SELECT Col FROM Src)", [("Owner", ["A"]), ("Src", ["Col"])]);
        return db;
    }

    private static ColumnSpec Long(string name) => new(name, JetDataType.Int32, 4, IsFixedLength: true);

    [Fact]
    public void What_a_check_reads_cannot_be_dropped_altered_or_renamed()
    {
        using JetDatabase db = Database();

        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.DropTable("Src")).Message);
        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.DropColumn("Src", "Col")).Message);
        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.AlterColumn("Src", "Col", Long("Col"))).Message);
        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.AlterColumnTypeInPlace("Src", "Col", Long("Col"))).Message);
        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.RenameTable("Src", "Src2")).Message);
        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.RenameTable("Src", "Src")).Message);
        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.RenameColumn("Src", "Col", "Col2")).Message);
        // Its own table: the column it reads, and the table itself for a rename, but not for a drop.
        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.DropColumn("Owner", "A")).Message);
        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.RenameColumn("Owner", "A", "A")).Message);
        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.RenameTable("Owner", "Owner2")).Message);

        Assert.NotNull(db.Catalog.FindTable("Src")?.FindColumn("Col"));
        Assert.NotNull(db.Catalog.FindTable("Owner")?.FindColumn("A"));
    }

    [Fact]
    public void A_count_read_holds_the_table_but_none_of_its_columns()
    {
        using JetDatabase db = Database(countOnly: true);

        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.DropTable("Src")).Message);
        Assert.True(db.DropColumn("Src", "Col"));
        Assert.Null(db.Catalog.FindTable("Src")?.FindColumn("Col"));
    }

    [Fact]
    public void Of_several_checks_reading_it_the_earliest_created_is_named()
    {
        using JetDatabase db = Database();
        db.CreateTable("Owner2", [Long("A")]);
        db.AddCheckConstraint("Owner2", "chkA", "A IN (SELECT Col FROM Src)", [("Owner2", ["A"]), ("Src", ["Col"])]);

        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.DropTable("Src")).Message);
        Assert.Equal(Refusal, Assert.Throws<InvalidOperationException>(() => db.DropColumn("Src", "Col")).Message);
    }

    [Fact]
    public void Dropping_an_indexed_column_a_check_reads_gives_the_index_refusal()
    {
        using JetDatabase db = Database();
        db.CreateIndex("Src", "ix", [("Col", false)]);

        var refused = Assert.Throws<InvalidOperationException>(() => db.DropColumn("Src", "Col"));
        Assert.Equal("Cannot drop column 'Col': it is part of an index or key — drop the index/constraint first.", refused.Message);
    }

    [Fact]
    public void What_no_check_reads_and_a_reads_default_and_nullability_are_free()
    {
        using JetDatabase db = Database();

        db.SetColumnDefault("Src", "Col", "5");
        db.DropColumnDefault("Src", "Col");
        // DAO's Field.Required: ACE sets and clears it on a column a check reads, another table's or its own.
        db.SetColumnRequired("Src", "Col", true);
        db.SetColumnRequired("Src", "Col", false);
        db.SetColumnRequired("Owner", "A", true);
        db.SetColumnRequired("Owner", "A", false);
        // An unread column renamed to its own name, or a case-only change.
        Assert.True(db.RenameColumn("Src", "Other", "Other"));
        Assert.True(db.RenameColumn("Src", "Other", "OTHER"));
        Assert.True(db.RenameColumn("Src", "OTHER", "Other2"));
        Assert.True(db.DropColumn("Src", "Other2"));
        Assert.True(db.RenameColumn("Owner", "B", "B2"));
        Assert.True(db.DropColumn("Owner", "B2"));

        Assert.True(db.DropTable("Owner"));
        Assert.True(db.DropTable("Src"));
    }
}
