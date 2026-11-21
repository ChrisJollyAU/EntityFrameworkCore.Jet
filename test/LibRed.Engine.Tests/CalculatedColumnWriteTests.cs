using LibRed;
using LibRed.Engine;
using Xunit;

namespace LibRed.Engine.Tests;

// What a calculated column writes, against what ACE writes for the same input (CalculatedColumnSqlAccessTests and
// docs/format/page-02e-calculated-columns.md hold the measurements).
public class CalculatedColumnWriteTests : TempDatabaseTest
{
    private static QueryEngine Fresh()
    {
        string path = TemporaryDatabase.CreatePath("calc-write-");
        JetDatabase.Create(path);
        return new QueryEngine(TemporaryDatabase.OpenTracked(path, readOnly: false));
    }

    // ACE caches VBA error 6 for a result past its result type, and then cannot read the row back. LibRed
    // refuses the row, its rule for every expression error. It used to narrow unchecked and store 44 or 255 for
    // 300 — a value no evaluation of the expression gives.
    [Theory]
    [InlineData("BYTE", "[A]*100", 3)]
    [InlineData("SHORT", "[A]*1000", 40)]
    [InlineData("LONG", "[A]*1000000", 3000)]
    public void A_result_past_its_type_is_an_overflow_and_the_row_is_refused(string type, string expression, int a)
    {
        QueryEngine engine = Fresh();
        engine.ExecuteNonQuery($"CREATE TABLE T (Id COUNTER PRIMARY KEY, A LONG, C {type} AS ({expression}))");

        var refused = Assert.ThrowsAny<Exception>(() => engine.ExecuteNonQuery($"INSERT INTO T (A) VALUES ({a})"));
        Assert.Contains("overflow", refused.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(engine.ExecuteQuery("SELECT Id FROM T").Rows);
    }

    // A Currency result over a Double is its exact binary value to four places, half to even, as storing the Double
    // into a Currency column takes it (measured through DAO: a CURRENCY field with the expression [D]).
    [Theory]
    [InlineData("0.00005", "0.0001")]
    [InlineData("1.00005", "1.0001")]
    [InlineData("2.00005", "2")]
    [InlineData("0.03125", "0.0312")]
    [InlineData("0.09375", "0.0938")]
    [InlineData("-0.00005", "-0.0001")]
    public void A_currency_result_over_a_double_is_its_exact_binary_value(string value, string expected)
    {
        QueryEngine engine = Fresh();
        engine.ExecuteNonQuery("CREATE TABLE T (Id LONG PRIMARY KEY, D DOUBLE, K CURRENCY AS ([D]))");
        engine.ExecuteNonQuery($"INSERT INTO T (Id, D) VALUES (1, CDBL('{value}'))");
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture),
            engine.ExecuteQuery("SELECT K FROM T").Rows.Single()[0]);
    }

    // Over a Decimal or text it is that decimal value exactly, half to even (measured through DAO: CURRENCY fields with
    // the expressions [N] and [T] over a DECIMAL(18,5) and a TEXT column).
    [Theory]
    [InlineData("0.00005", "0")]
    [InlineData("1.00005", "1")]
    [InlineData("2.00005", "2")]
    [InlineData("0.03125", "0.0312")]
    [InlineData("0.00015", "0.0002")]
    public void A_currency_result_over_a_decimal_or_text_is_its_exact_value(string value, string expected)
    {
        QueryEngine engine = Fresh();
        engine.ExecuteNonQuery("CREATE TABLE T (Id LONG PRIMARY KEY, N DECIMAL(18,5), X TEXT(20), K CURRENCY AS ([N]), KT CURRENCY AS ([X]))");
        engine.ExecuteNonQuery($"INSERT INTO T (Id, N, X) VALUES (1, {value}, '{value}')");
        object?[] row = engine.ExecuteQuery("SELECT K, KT FROM T").Rows.Single();
        decimal stored = decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(stored, row[0]);
        Assert.Equal(stored, row[1]);
    }

    // ACE computes a calculated column added to a table that already has rows (measured through DAO); left alone,
    // those rows would read Null for good, since nothing re-derives a cached value on read.
    [Fact]
    public void A_calculated_column_added_to_a_table_with_rows_is_computed_for_them()
    {
        QueryEngine engine = Fresh();
        engine.ExecuteNonQuery("CREATE TABLE T (Id LONG PRIMARY KEY, A LONG, M MEMO)");
        engine.ExecuteNonQuery($"INSERT INTO T (Id, A, M) VALUES (1, 21, '{new string('x', 3000)}')");
        engine.ExecuteNonQuery("INSERT INTO T (Id, A) VALUES (2, NULL)");

        engine.ExecuteNonQuery("ALTER TABLE T ADD COLUMN C LONG AS ([A]*2)");

        var rows = engine.ExecuteQuery("SELECT Id, C, Len(M) FROM T ORDER BY Id").Rows.ToList();
        Assert.Equal(42, rows[0][1]);
        Assert.Equal(3000, rows[0][2]);   // the row's other values survive the rewrite
        Assert.Null(rows[1][1]);
    }

    // The column and its values arrive together or not at all: a row the expression fails on takes the added
    // column back out, rather than leaving it computed for some rows and missing from others.
    [Fact]
    public void A_row_the_new_expression_fails_on_refuses_the_whole_add()
    {
        QueryEngine engine = Fresh();
        engine.ExecuteNonQuery("CREATE TABLE T (Id LONG PRIMARY KEY, A LONG)");
        engine.ExecuteNonQuery("INSERT INTO T (Id, A) VALUES (1, 1)");
        engine.ExecuteNonQuery("INSERT INTO T (Id, A) VALUES (2, 3)");

        Assert.ThrowsAny<Exception>(() => engine.ExecuteNonQuery("ALTER TABLE T ADD COLUMN C BYTE AS ([A]*100)"));
        Assert.Null(engine.Database.Catalog.FindTable("T")!.FindColumn("C"));
    }
}
