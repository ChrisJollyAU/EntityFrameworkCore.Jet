using LibRed;
using LibRed.Engine;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>The Jet-flavoured INFORMATION_SCHEMA views are engine-native virtual tables over the catalog, so EF's
/// migration existence checks (<c>SELECT * FROM `INFORMATION_SCHEMA.TABLES` WHERE `TABLE_NAME` = '…'</c>) work.</summary>
public class InformationSchemaTests : TempDatabaseTest
{
    private static QueryEngine Seeded()
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "is-");
        var e = new QueryEngine(TemporaryDatabase.OpenTracked(path, readOnly: false));
        e.ExecuteNonQuery("CREATE TABLE Widget (Id LONG PRIMARY KEY, Name TEXT(50))");
        return e;
    }

    [Fact]
    public void Tables_view_lists_user_tables_and_filters_by_name()
    {
        var e = Seeded();
        int hit = e.ExecuteQuery("SELECT * FROM `INFORMATION_SCHEMA.TABLES` WHERE `TABLE_NAME` = 'Widget'").Rows.Count();
        Assert.Equal(1, hit);
        int miss = e.ExecuteQuery("SELECT * FROM `INFORMATION_SCHEMA.TABLES` WHERE `TABLE_NAME` = 'Nope'").Rows.Count();
        Assert.Equal(0, miss);
    }

    // RELATION_TYPE is grbit's one-to-one bit, which SQL never sets — not the uniqueness of whatever index covers
    // the child columns. A foreign key from one primary key to another is still one-to-many.
    [Fact]
    public void Relations_view_reports_a_sql_foreign_key_between_primary_keys_as_many()
    {
        var e = Seeded();
        e.ExecuteNonQuery("CREATE TABLE Part (Id LONG PRIMARY KEY, CONSTRAINT fk_part FOREIGN KEY (Id) REFERENCES Widget (Id))");
        var row = e.ExecuteQuery("SELECT `RELATION_TYPE` FROM `INFORMATION_SCHEMA.RELATIONS` WHERE `RELATION_NAME` = 'fk_part'").Rows.Single();
        Assert.Equal("MANY", row[0]);
    }

    [Fact]
    public void Columns_view_lists_columns()
    {
        var e = Seeded();
        var rows = e.ExecuteQuery("SELECT `COLUMN_NAME` FROM `INFORMATION_SCHEMA.COLUMNS` WHERE `TABLE_NAME` = 'Widget'").Rows.ToList();
        Assert.Equal(2, rows.Count);
    }

    // A calculated column is as nullable as the type it reports, so one yielding Yes/No is not nullable although it is
    // stored in two bytes, and a calculated Long or Text one is: ACE's Columns rowset says Id:N A:N B:R L:N X:N for
    // this table (measured, CalculatedColumnSqlAccessTests), and so do the Columns rowset and the scaffolder.
    [Fact]
    public void Columns_view_calls_a_calculated_YesNo_column_not_nullable_as_ACE_does()
    {
        var e = Seeded();
        e.ExecuteNonQuery(
            "CREATE TABLE T (Id LONG PRIMARY KEY, A LONG, B YESNO AS ([A] > 0), L LONG AS ([A]*2), X TEXT(20) AS (\"n\" & [A]))");
        var rows = e.ExecuteQuery(
            "SELECT `COLUMN_NAME`, `IS_NULLABLE` FROM `INFORMATION_SCHEMA.COLUMNS` WHERE `TABLE_NAME` = 'T' ORDER BY `ORDINAL_POSITION`")
            .Rows.Select(r => $"{r[0]}:{((bool)r[1]! ? "N" : "R")}");
        Assert.Equal("Id:N A:N B:R L:N X:N", string.Join(" ", rows));
    }

    // A calculated column reports the type its result has, on every facet, not the promoted type its descriptor stores
    // — and the scaffolder's JetStoreType gives the same. ACE's Columns rowset reports these columns as Long, Long,
    // Yes/No (not nullable), Long, long text (length 0) and Double (measured, CalculatedColumnSqlAccessTests).
    [Fact]
    public void Columns_view_reports_calculated_columns_by_the_type_they_report_on_every_facet()
    {
        var e = Seeded();
        e.ExecuteNonQuery(
            "CREATE TABLE T (Id LONG PRIMARY KEY, A LONG, B YESNO AS ([A] > 0), L LONG AS ([A]*2), X TEXT(20) AS (\"n\" & [A]), "
            + "D DOUBLE AS ([A]/3))");
        string view = string.Join(" ", e.ExecuteQuery(
            "SELECT `COLUMN_NAME`, `DATA_TYPE`, `CHARACTER_MAXIMUM_LENGTH`, `IS_NULLABLE` FROM `INFORMATION_SCHEMA.COLUMNS` "
            + "WHERE `TABLE_NAME` = 'T' ORDER BY `ORDINAL_POSITION`")
            .Rows.Select(r => $"{r[0]}:{r[1]}/{r[2] ?? "-"}/{((bool)r[3]! ? "N" : "R")}"));
        string storeType = string.Join(" ", e.Database.Catalog.FindTable("T")!.Columns.Select(c =>
            $"{c.Name}:{Schema.JetStoreType.TypeName(c)}/{(object?)Schema.JetStoreType.MaxLength(c) ?? "-"}/{(Schema.JetStoreType.IsNullable(c) ? "N" : "R")}"));

        Assert.Equal("Id:integer/-/N A:integer/-/N B:bit/-/R L:integer/-/N X:longchar/-/N D:double/-/N", view);
        Assert.Equal(view, storeType);
    }

    [Fact]
    public void All_seven_views_bind_and_execute()
    {
        var e = Seeded();
        foreach (string v in new[] { "TABLES", "COLUMNS", "INDEXES", "INDEX_COLUMNS", "RELATIONS", "RELATION_COLUMNS", "CHECK_CONSTRAINTS" })
            _ = e.ExecuteQuery($"SELECT * FROM `INFORMATION_SCHEMA.{v}`").Rows.ToList(); // must not throw
    }
}

// -- IF [NOT] EXISTS (...) THEN <statement> --
public class IfThenStatementTests
{
    private static QueryEngine Seeded()
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "if-");
        var e = new QueryEngine(TemporaryDatabase.OpenTracked(path, readOnly: false));
        e.ExecuteNonQuery("CREATE TABLE Existing (Id LONG PRIMARY KEY)");
        return e;
    }

    private static bool TableExists(QueryEngine e, string t) =>
        e.ExecuteQuery($"SELECT * FROM `INFORMATION_SCHEMA.TABLES` WHERE `TABLE_NAME` = '{t}'").Rows.Any();

    [Fact]
    public void If_not_exists_creates_when_absent()
    {
        var e = Seeded();
        e.ExecuteNonQuery("IF NOT EXISTS (SELECT * FROM `INFORMATION_SCHEMA.TABLES` WHERE `TABLE_NAME` = 'Fresh') THEN CREATE TABLE `Fresh` (`Id` LONG PRIMARY KEY)");
        Assert.True(TableExists(e, "Fresh"));
    }

    [Fact]
    public void If_not_exists_skips_when_present()
    {
        var e = Seeded();
        // Existing already has a table; the guarded CREATE must be skipped (no "already exists" throw).
        e.ExecuteNonQuery("IF NOT EXISTS (SELECT * FROM `INFORMATION_SCHEMA.TABLES` WHERE `TABLE_NAME` = 'Existing') THEN CREATE TABLE `Existing` (`Id` LONG PRIMARY KEY)");
        Assert.True(TableExists(e, "Existing"));
    }

    [Fact]
    public void If_exists_runs_body_when_present()
    {
        var e = Seeded();
        e.ExecuteNonQuery("IF EXISTS (SELECT * FROM `INFORMATION_SCHEMA.TABLES` WHERE `TABLE_NAME` = 'Existing') THEN CREATE TABLE `Made` (`Id` LONG PRIMARY KEY)");
        Assert.True(TableExists(e, "Made"));
    }
}
