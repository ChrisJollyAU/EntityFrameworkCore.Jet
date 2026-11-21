using LibRed;
using LibRed.Catalog;
using LibRed.Engine;
using Xunit;

namespace LibRed.Engine.Tests;

// A UNIQUE or CHECK constraint written without a name gets one made from the table's: UQ_<table>_<n>. A name
// over 64 characters is not merely rejected by ACE — it makes ACE refuse the whole file ("Unrecognized
// database format"), so the generated name has to fit whatever the table is called, and a table may use all 64
// characters itself. The table's name is truncated to make room rather than the constraint being refused.
public class UnnamedConstraintNameTests : TempDatabaseTest
{
    private static (QueryEngine Engine, JetDatabase Db) Fresh()
    {
        string path = TemporaryDatabase.CopyPath(
            Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "unnamed-constraint-");
        var db = TemporaryDatabase.OpenTracked(path, readOnly: false);
        return (new QueryEngine(db), db);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(JetName.MaxLength)]   // a table using every character it is allowed
    public void An_unnamed_unique_constraint_gets_a_name_that_fits(int tableNameLength)
    {
        var (e, db) = Fresh();
        string table = new('T', tableNameLength);

        e.ExecuteNonQuery($"CREATE TABLE [{table}] (Id LONG PRIMARY KEY, V TEXT(10), UNIQUE (V))");

        IndexDef unique = Assert.Single(
            db.Catalog.FindTable(table)!.Indexes.Where(i => i.IsUnique && !i.IsPrimaryKey));
        Assert.StartsWith("UQ_", unique.Name, StringComparison.Ordinal);
        Assert.True(unique.Name.Length <= JetName.MaxLength,
            $"the generated name is {unique.Name.Length} characters: '{unique.Name}'");

        // And it is a working unique index, not just a name.
        e.ExecuteNonQuery($"INSERT INTO [{table}] (Id, V) VALUES (1, 'x')");
        Assert.Throws<ConstraintViolationException>(() =>
            e.ExecuteNonQuery($"INSERT INTO [{table}] (Id, V) VALUES (2, 'x')"));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(JetName.MaxLength)]
    public void An_unnamed_check_constraint_gets_a_name_that_fits(int tableNameLength)
    {
        var (e, db) = Fresh();
        string table = new('C', tableNameLength);

        e.ExecuteNonQuery($"CREATE TABLE [{table}] (Id LONG PRIMARY KEY, V LONG, CHECK (V > 0))");

        var check = Assert.Single(db.Catalog.FindTable(table)!.CheckConstraints);
        Assert.StartsWith("CK_", check.Name, StringComparison.Ordinal);
        Assert.True(check.Name.Length <= JetName.MaxLength,
            $"the generated name is {check.Name.Length} characters: '{check.Name}'");

        e.ExecuteNonQuery($"INSERT INTO [{table}] (Id, V) VALUES (1, 5)");
        Assert.ThrowsAny<Exception>(() => e.ExecuteNonQuery($"INSERT INTO [{table}] (Id, V) VALUES (2, -1)"));
    }

    // ACE names every unnamed foreign key — inline REFERENCES, table-level FOREIGN KEY, ALTER TABLE ADD FOREIGN KEY
    // and ADD COLUMN ... REFERENCES alike — Rel_ and the first three groups of a fresh GUID (e.g.
    // Rel_6CF170F8_F50A_4F5C), whatever the tables are called, and gives the child's index the same name; so any
    // number of them between one pair of tables are created (measured).
    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    public void An_unnamed_foreign_key_is_named_rel_and_a_guid(int tableNameLength)
    {
        var (e, db) = Fresh();
        string parent = new('P', tableNameLength), child = new('C', tableNameLength);

        e.ExecuteNonQuery($"CREATE TABLE [{parent}] (Id LONG PRIMARY KEY)");
        e.ExecuteNonQuery($"CREATE TABLE [{child}] (Id LONG PRIMARY KEY, A LONG REFERENCES [{parent}] (Id), "
            + $"B LONG, D LONG, FOREIGN KEY (B) REFERENCES [{parent}] (Id))");
        e.ExecuteNonQuery($"ALTER TABLE [{child}] ADD FOREIGN KEY (D) REFERENCES [{parent}] (Id)");
        e.ExecuteNonQuery($"ALTER TABLE [{child}] ADD COLUMN E LONG REFERENCES [{parent}]");

        var names = db.Catalog.Relationships.Where(r => r.Table == child).Select(r => r.Name).ToList();
        Assert.Equal(4, names.Distinct().Count());
        var table = db.Catalog.FindTable(child)!;
        foreach (string name in names)
        {
            Assert.Matches("^Rel_[0-9A-F]{8}_[0-9A-F]{4}_[0-9A-F]{4}$", name);
            Assert.Contains(table.Indexes, ix => ix.Name == name);
        }
    }
}
