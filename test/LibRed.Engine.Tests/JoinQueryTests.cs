using LibRed;
using LibRed.Engine;
using LibRed.Engine.Execution;
using Xunit;

namespace LibRed.Engine.Tests;

public class JoinQueryTests
{
    private static readonly string Northwind = Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb");

    private static List<object?[]> Query(string sql, out IReadOnlyList<string> columns)
    {
        using var db = JetDatabase.Open(Northwind);
        var rs = new QueryEngine(db).ExecuteQuery(sql);
        columns = rs.ColumnNames;
        return rs.Rows.ToList();
    }

    [Fact]
    public void Order_by_descending()
    {
        var rows = Query("SELECT CategoryName FROM Categories ORDER BY CategoryName DESC", out _);
        Assert.Equal("Seafood", rows[0][0]);
        Assert.Equal("Beverages", rows[^1][0]);
    }

    [Fact]
    public void Mod_operator_in_where()
    {
        var rows = Query("SELECT ProductID FROM Products WHERE ProductID MOD 17 = 5", out _);
        Assert.Equal([5, 22, 39, 56, 73], rows.Select(r => (int)r[0]!).OrderBy(x => x));
    }

    [Fact]
    public void Inner_join_with_aliases_and_qualified_columns()
    {
        var rows = Query(
            "SELECT p.ProductName, c.CategoryName FROM Products AS p " +
            "INNER JOIN Categories AS c ON p.CategoryID = c.CategoryID WHERE p.ProductID = 1",
            out var columns);

        Assert.Equal(["ProductName", "CategoryName"], columns);
        var only = Assert.Single(rows);
        Assert.Equal("Chai", only[0]);
        Assert.Equal("Beverages", only[1]);
    }

    [Fact]
    public void Left_join_over_a_derived_table()
    {
        // The full EF-generated query: LEFT JOIN to a subquery (Order Details INNER JOIN Orders),
        // with backtick identifiers, MOD, a projection alias, and a multi-key ORDER BY.
        const string sql = """
            SELECT `p`.`ProductID`, `p`.`ProductName`, `p`.`UnitPrice`, `s`.`OrderID`, `s`.`ProductID`, `s`.`Quantity`, `s`.`OrderID0`, `s`.`OrderDate`
            FROM `Products` AS `p`
            LEFT JOIN (
                SELECT `o`.`OrderID`, `o`.`ProductID`, `o`.`Quantity`, `o0`.`OrderID` AS `OrderID0`, `o0`.`OrderDate`
                FROM `Order Details` AS `o`
                INNER JOIN `Orders` AS `o0` ON `o`.`OrderID` = `o0`.`OrderID`
            ) AS `s` ON `p`.`ProductID` = `s`.`ProductID`
            WHERE (`p`.`ProductID` MOD 17) = 5 AND `p`.`UnitPrice` < 20.0
            ORDER BY `p`.`ProductID`, `s`.`OrderID`, `s`.`ProductID`
            """;

        var rows = Query(sql, out var columns);

        // The two ProductIDs are told apart by their tables, as ACE names them for this query (verified vs ACE).
        Assert.Equal(["p.ProductID", "ProductName", "UnitPrice", "OrderID", "s.ProductID", "Quantity", "OrderID0", "OrderDate"], columns);

        // Only products 39 and 73 satisfy (id MOD 17 = 5) AND (price < 20).
        Assert.Equal([39, 73], rows.Select(r => (int)r[0]!).Distinct().OrderBy(x => x));
        Assert.All(rows, r => Assert.Equal(r[0], r[4])); // join key: p.ProductID == s.ProductID

        // Ordered by p.ProductID then s.OrderID.
        var keys = rows.Select(r => ((int)r[0]!, (int)r[3]!)).ToList();
        Assert.Equal(keys.OrderBy(k => k.Item1).ThenBy(k => k.Item2), keys);

        // OrderID0 (the alias from the inner Orders join) equals OrderID for every matched row.
        Assert.All(rows, r => Assert.Equal(r[3], r[6]));
    }

    private static QueryEngine LinqDataTypes(JetDatabase db)
    {
        var e = new QueryEngine(db);
        e.ExecuteNonQuery("CREATE TABLE LinqDataTypes (ID int, MoneyValue decimal(10,4), DateTimeValue datetime, DateTimeValue2 datetime, "
            + "BoolValue bit, GuidValue uniqueidentifier, BinaryValue OleObject NULL, SmallIntValue smallint, IntValue int NULL, "
            + "BigIntValue long NULL, StringValue Text(50) NULL)");
        string[] strings = ["NULL", "''", "'1'", "'2'", "'3'", "'4'", "'5'", "'6'", "'7'", "'8'", "'9'", "'0'"];
        string[] bools = ["True", "False", "True", "False", "True", "False", "True", "False", "True", "False", "True", "True"];
        for (int i = 0; i < 12; i++)
            e.ExecuteNonQuery($"INSERT INTO LinqDataTypes (ID, BoolValue, SmallIntValue, StringValue) VALUES ({i + 1}, {bools[i]}, {i + 1}, {strings[i]})");
        return e;
    }

    [Fact]
    public void A_join_whose_left_side_has_no_rows_does_not_evaluate_its_right_side()
    {
        // linq2db's Issue2608 statement and data. [f]'s NOT IN reads '' as a number, a type mismatch, but the
        // contradictory [x] predicates leave no row to join it to. ACE runs this exact statement and returns no
        // rows (measured over OLE DB), while it refuses the same NOT IN on [f] alone.
        const string sql = """
            SELECT
            	[f].[StringValue]
            FROM
            	((([LinqDataTypes] [x]
            		INNER JOIN [LinqDataTypes] [o] ON ([x].[IntValue] = [o].[IntValue] OR [x].[IntValue] IS NULL AND [o].[IntValue] IS NULL))
            		INNER JOIN [LinqDataTypes] [u] ON ([o].[IntValue] = [u].[IntValue] OR [o].[IntValue] IS NULL AND [u].[IntValue] IS NULL))
            		INNER JOIN [LinqDataTypes] [r] ON (([u].[IntValue] = [r].[IntValue] OR [u].[IntValue] IS NULL AND [r].[IntValue] IS NULL) AND [r].[BoolValue] = False))
            		INNER JOIN [LinqDataTypes] [f] ON ([r].[IntValue] = [f].[IntValue] OR [r].[IntValue] IS NULL AND [f].[IntValue] IS NULL)
            WHERE
            	[x].[BoolValue] = False AND
            	[x].[IntValue] = @code AND
            	[x].[IntValue] = @site AND
            	[o].[BoolValue] = False AND
            	[u].[BoolValue] = False AND
            	([f].[StringValue] NOT IN (11, 18, 19, 20, 21, 22, 23, 24, 26, 29, 28) OR [f].[StringValue] IS NULL)
            """;
        string path = TemporaryDatabase.CopyPath(Northwind, "emptyleft-");
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            QueryEngine e = LinqDataTypes(db);
            var parameters = new Dictionary<string, object?> { ["code"] = 1, ["site"] = 2 };

            Assert.Empty(e.ExecuteQuery(sql, parameters).Rows);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void A_right_join_whose_left_side_has_no_rows_keeps_every_right_row()
    {
        string path = TemporaryDatabase.CopyPath(Northwind, "emptyleft-");
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            QueryEngine e = LinqDataTypes(db);
            var rows = e.ExecuteQuery(
                "SELECT [x].[ID], [f].[ID] FROM (SELECT * FROM [LinqDataTypes] WHERE [ID] < 0) AS [x] "
                + "RIGHT JOIN [LinqDataTypes] [f] ON ([x].[IntValue] = [f].[IntValue] OR [x].[IntValue] IS NULL AND [f].[IntValue] IS NULL)")
                .Rows.ToList();

            Assert.All(rows, r => Assert.Null(r[0]));
            Assert.Equal(Enumerable.Range(1, 12), rows.Select(r => (int)r[1]!).Order());
        }
        finally { TemporaryDatabase.Delete(path); }
    }
}
