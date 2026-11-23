using System.Data.OleDb;
using System.Globalization;
using Xunit;

namespace LibRed.Engine.Tests;

// A Boolean stored into a column that is not Yes/No, and a Null stored into one that is, against ACE.
//
// A Boolean bound for a column is the Int16 -1 or 0, and from there exactly that number: 255 in a BYTE, -1 in a number,
// "-1" in text, 1899-12-29 in a date — by INSERT, UPDATE, INSERT ... SELECT, UPDATE through a join and ALTER COLUMN
// alike. A function's Boolean (IsNull, Like) is stored the same way. A Null written to a Yes/No column is False before
// a required column, an index or a CHECK sees it: a second Null is a duplicate in a unique index, and IGNORE NULL
// ignores nothing.
//
// Each case runs the same statements through ACE and through LibRed on copies of Northwind: every statement's
// outcome — the rows it affected, or its refusal; the two engines word a refusal differently — and then table T as
// each engine reads its own file must agree.
[Collection(AceCollection.Name)]
public class YesNoStorageAccessTests : TempDatabaseTest
{
    public static TheoryData<string, bool> TypesIndexedOrNot => new()
    {
        { "BYTE", false }, { "BYTE", true }, { "SHORT", false }, { "SHORT", true }, { "LONG", false }, { "LONG", true },
        { "SINGLE", false }, { "SINGLE", true }, { "DOUBLE", false }, { "DOUBLE", true }, { "CURRENCY", false }, { "CURRENCY", true },
        { "DECIMAL(18,2)", false }, { "DECIMAL(18,2)", true }, { "TEXT(20)", false }, { "TEXT(20)", true }, { "MEMO", false },
        { "MEMO", true }, { "DATETIME", false }, { "DATETIME", true },
    };

    // Written, computed and read from a Yes/No column, by every statement that stores a value.
    [Theory]
    [MemberData(nameof(TypesIndexedOrNot))]
    public void A_Boolean_written_to_a_column_that_is_not_YesNo_is_stored_as_ACE_stores_it(string type, bool indexed)
    {
        string[] statements =
        [
            "CREATE TABLE S (ID LONG PRIMARY KEY, Y YESNO)",
            "INSERT INTO S VALUES (51, True)",
            "INSERT INTO S VALUES (52, False)",
            $"CREATE TABLE T (ID LONG PRIMARY KEY, V {type})",
            .. indexed ? (string[])["CREATE INDEX IX_T ON T (V)"] : [],
            "INSERT INTO T (ID, V) VALUES (1, True)",
            "INSERT INTO T (ID, V) VALUES (2, False)",
            "INSERT INTO T (ID, V) VALUES (3, (1=1))",
            "INSERT INTO T (ID, V) VALUES (4, (1=0))",
            "INSERT INTO T (ID) VALUES (11)",
            "INSERT INTO T (ID) VALUES (12)",
            "INSERT INTO T (ID) VALUES (13)",
            "INSERT INTO T (ID) VALUES (14)",
            "UPDATE T SET V = True WHERE ID = 11",
            "UPDATE T SET V = False WHERE ID = 12",
            "UPDATE T SET V = (1=1) WHERE ID = 13",
            "UPDATE T SET V = (1=0) WHERE ID = 14",
            "INSERT INTO T (ID, V) SELECT ID - 30, Y FROM S",
            "INSERT INTO T (ID, V) SELECT ID - 20, True FROM S",
            "INSERT INTO T (ID, V) SELECT ID - 10, (ID = 51) FROM S",
            "INSERT INTO T (ID) VALUES (51)",
            "INSERT INTO T (ID) VALUES (52)",
            "UPDATE T INNER JOIN S ON T.ID = S.ID SET T.V = S.Y",
        ];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // A Yes/No column retyped: its True is the same -1, and 255 in a BYTE.
    [Theory]
    [MemberData(nameof(TypesIndexedOrNot))]
    public void A_YesNo_column_retyped_keeps_its_Boolean_as_ACE_keeps_it(string type, bool indexed)
    {
        string[] statements =
        [
            "CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO)",
            .. indexed ? (string[])["CREATE INDEX IX_T ON T (V)"] : [],
            "INSERT INTO T VALUES (1, True)",
            "INSERT INTO T VALUES (2, False)",
            $"ALTER TABLE T ALTER COLUMN V {type}",
        ];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    [Fact]
    public void A_function_Boolean_stored_into_a_number_or_text_column_is_stored_as_ACE_stores_it()
    {
        string[] statements =
        [
            "CREATE TABLE T (ID LONG PRIMARY KEY, V BYTE, I2 SHORT, Tx TEXT(10))",
            "INSERT INTO T (ID, I2, Tx) VALUES (1, IsNull(Null), IsNull(Null))",
            "INSERT INTO T (ID, I2, Tx) VALUES (2, IsNull(1), ('a' Like 'b'))",
            "INSERT INTO T (ID, V) VALUES (3, IsNull(Null))",
            "INSERT INTO T (ID, V) VALUES (4, (1=1))",
            "INSERT INTO T (ID, V) VALUES (5, IsNull(1))",
            "UPDATE T SET Tx = ('a' Like 'a'), V = IsNull(Null) WHERE ID = 5",
            "INSERT INTO T (ID, Tx) VALUES (6, -(0^2))",
        ];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    [Theory]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO)", "INSERT INTO T (ID, V) VALUES (1, NULL)")]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO)", "INSERT INTO T (ID) VALUES (1)")]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO)", "INSERT INTO T (ID, V) VALUES (1, True)", "UPDATE T SET V = NULL")]
    [InlineData("CREATE TABLE NS (ID LONG PRIMARY KEY, N LONG)", "INSERT INTO NS VALUES (10, NULL)", "INSERT INTO NS VALUES (11, 5)",
        "INSERT INTO NS VALUES (12, 0)", "CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO)", "INSERT INTO T (ID, V) SELECT ID, N FROM NS")]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO NOT NULL)", "INSERT INTO T (ID, V) VALUES (1, NULL)")]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO NOT NULL)", "INSERT INTO T (ID) VALUES (1)")]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO NOT NULL)", "INSERT INTO T (ID, V) VALUES (1, True)", "UPDATE T SET V = NULL")]
    [InlineData("CREATE TABLE NS (ID LONG PRIMARY KEY, N LONG)", "INSERT INTO NS VALUES (10, NULL)", "INSERT INTO NS VALUES (11, 5)",
        "INSERT INTO NS VALUES (12, 0)", "CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO NOT NULL)", "INSERT INTO T (ID, V) SELECT ID, N FROM NS")]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO)", "ALTER TABLE T ALTER COLUMN V YESNO NOT NULL", "INSERT INTO T (ID, V) VALUES (1, NULL)")]
    // A unique index holds the False the row stores: a second Null is a duplicate, and IGNORE NULL ignores nothing.
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "CREATE UNIQUE INDEX UX_T ON T (V)", "INSERT INTO T (ID, V) VALUES (1, False)",
        "INSERT INTO T (ID, V) VALUES (2, NULL)", "INSERT INTO T (ID) VALUES (3)", "INSERT INTO T (ID, V) VALUES (4, True)")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "CREATE UNIQUE INDEX UX_T ON T (V)", "INSERT INTO T (ID, V) VALUES (1, NULL)",
        "INSERT INTO T (ID, V) VALUES (2, NULL)", "INSERT INTO T (ID, V) VALUES (3, False)")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "CREATE UNIQUE INDEX UX_T ON T (V) WITH IGNORE NULL", "INSERT INTO T (ID, V) VALUES (1, False)",
        "INSERT INTO T (ID, V) VALUES (2, NULL)", "INSERT INTO T (ID) VALUES (3)", "INSERT INTO T (ID, V) VALUES (4, True)")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "CREATE UNIQUE INDEX UX_T ON T (V) WITH IGNORE NULL", "INSERT INTO T (ID, V) VALUES (1, NULL)",
        "INSERT INTO T (ID, V) VALUES (2, NULL)", "INSERT INTO T (ID, V) VALUES (3, False)")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "CREATE UNIQUE INDEX UX_T ON T (V) WITH DISALLOW NULL", "INSERT INTO T (ID, V) VALUES (1, False)",
        "INSERT INTO T (ID, V) VALUES (2, NULL)", "INSERT INTO T (ID) VALUES (3)", "INSERT INTO T (ID, V) VALUES (4, True)")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "CREATE UNIQUE INDEX UX_T ON T (V) WITH DISALLOW NULL", "INSERT INTO T (ID, V) VALUES (1, NULL)",
        "INSERT INTO T (ID, V) VALUES (2, NULL)", "INSERT INTO T (ID, V) VALUES (3, False)")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "CREATE UNIQUE INDEX UX_T ON T (V) WITH PRIMARY", "INSERT INTO T (ID, V) VALUES (1, False)",
        "INSERT INTO T (ID, V) VALUES (2, NULL)", "INSERT INTO T (ID) VALUES (3)", "INSERT INTO T (ID, V) VALUES (4, True)")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "CREATE UNIQUE INDEX UX_T ON T (V) WITH PRIMARY", "INSERT INTO T (ID, V) VALUES (1, NULL)",
        "INSERT INTO T (ID, V) VALUES (2, NULL)", "INSERT INTO T (ID, V) VALUES (3, False)")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO, X TEXT(10))", "CREATE UNIQUE INDEX UX_T ON T (V, X)", "INSERT INTO T (ID, V, X) VALUES (1, NULL, NULL)",
        "INSERT INTO T (ID, V, X) VALUES (2, NULL, NULL)", "INSERT INTO T (ID, V, X) VALUES (3, NULL, 'x')", "INSERT INTO T (ID, V, X) VALUES (4, False, 'x')")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "CREATE INDEX IX_T ON T (V) WITH IGNORE NULL", "INSERT INTO T (ID, V) VALUES (1, NULL)",
        "INSERT INTO T (ID, V) VALUES (2, True)")]
    // A CHECK sees the False too.
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO, CONSTRAINT ck CHECK (V IS NOT NULL))", "INSERT INTO T (ID, V) VALUES (1, NULL)",
        "INSERT INTO T (ID) VALUES (2)", "INSERT INTO T (ID, V) VALUES (3, True)", "INSERT INTO T (ID, V) VALUES (4, False)")]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO, CONSTRAINT ck CHECK (V IS NULL))", "INSERT INTO T (ID, V) VALUES (1, NULL)",
        "INSERT INTO T (ID) VALUES (2)", "INSERT INTO T (ID, V) VALUES (3, True)", "INSERT INTO T (ID, V) VALUES (4, False)")]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO, CONSTRAINT ck CHECK (V = False))", "INSERT INTO T (ID, V) VALUES (1, NULL)",
        "INSERT INTO T (ID) VALUES (2)", "INSERT INTO T (ID, V) VALUES (3, True)", "INSERT INTO T (ID, V) VALUES (4, False)")]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO, CONSTRAINT ck CHECK (V = True))", "INSERT INTO T (ID, V) VALUES (1, NULL)",
        "INSERT INTO T (ID) VALUES (2)", "INSERT INTO T (ID, V) VALUES (3, True)", "INSERT INTO T (ID, V) VALUES (4, False)")]
    [InlineData("CREATE TABLE T (ID LONG PRIMARY KEY, V YESNO, CONSTRAINT ck CHECK (V <> True))", "INSERT INTO T (ID, V) VALUES (1, NULL)",
        "INSERT INTO T (ID) VALUES (2)", "INSERT INTO T (ID, V) VALUES (3, True)", "INSERT INTO T (ID, V) VALUES (4, False)")]
    public void A_Null_written_to_a_YesNo_column_is_False_to_a_required_column_an_index_and_a_check(params string[] statements)
    {
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // An outer join's unmatched Yes/No, written into a table, keeps the Null a LONG or TEXT column can hold. L's rows 3
    // and 4 have no match.
    [Theory]
    [InlineData("CREATE TABLE T (ID LONG, V LONG)", "INSERT INTO T (ID, V) SELECT L.ID, YS.F FROM L LEFT JOIN YS ON L.ID = YS.ID")]
    [InlineData("CREATE TABLE T (ID LONG, V YESNO)", "INSERT INTO T (ID, V) SELECT L.ID, YS.F FROM L LEFT JOIN YS ON L.ID = YS.ID")]
    [InlineData("CREATE TABLE T (ID LONG, V TEXT(10))", "INSERT INTO T (ID, V) SELECT L.ID, YS.F FROM L LEFT JOIN YS ON L.ID = YS.ID")]
    [InlineData("CREATE TABLE T (ID LONG, V LONG, W LONG)", "INSERT INTO T (ID, V, W) SELECT L.ID, NOT YS.F, YS.F + 0 FROM L LEFT JOIN YS ON L.ID = YS.ID")]
    [InlineData("SELECT L.ID, YS.F AS V INTO T FROM L LEFT JOIN YS ON L.ID = YS.ID")]
    public void An_outer_joins_unmatched_YesNo_written_into_a_table_is_stored_as_ACE_stores_it(params string[] write)
    {
        string[] statements =
        [
            "CREATE TABLE YS (ID LONG PRIMARY KEY, F YESNO)", "INSERT INTO YS VALUES (1, True)", "INSERT INTO YS VALUES (2, False)",
            "CREATE TABLE L (ID LONG PRIMARY KEY)", "INSERT INTO L VALUES (1)", "INSERT INTO L VALUES (2)", "INSERT INTO L VALUES (3)",
            "INSERT INTO L VALUES (4)",
            .. write,
        ];
        Assert.Equal(Describe(statements, AceRun), Describe(statements, LibRedRun));
    }

    // Table T's rows, and which rows read as True, False and Null.
    private static readonly string[] ReadBack =
    [
        "SELECT * FROM T ORDER BY ID", "SELECT ID FROM T WHERE V = True ORDER BY ID",
        "SELECT ID FROM T WHERE V = False ORDER BY ID", "SELECT ID FROM T WHERE V IS NULL ORDER BY ID",
    ];

    /// <summary>Each statement's outcome through ACE — the rows an INSERT, UPDATE or DELETE affected, "ok" for any other,
    /// or "refused" — then <see cref="ReadBack"/> as ACE reads it.</summary>
    private static string AceRun(string path, string[] statements)
    {
        var outcome = new List<string>();
        using OleDbConnection connection = AceTestDatabase.Open(path);
        foreach (string s in statements)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = s;
            try { outcome.Add(Affected(s, command.ExecuteNonQuery())); }
            catch (OleDbException) { outcome.Add("refused"); }
        }
        foreach (string query in ReadBack)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = query;
            try
            {
                using OleDbDataReader reader = command.ExecuteReader();
                var rows = new List<object?[]>();
                while (reader.Read())
                    rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(reader.GetValue)]);
                outcome.Add(Rows(rows));
            }
            catch (OleDbException) { outcome.Add("refused"); }
        }
        return string.Join(" | ", outcome);
    }

    /// <summary>The same through LibRed's engine. A refusal is one of the exceptions LibRed refuses a statement with; any
    /// other is a fault, and fails the test.</summary>
    private static string LibRedRun(string path, string[] statements)
    {
        var outcome = new List<string>();
        using var database = JetDatabase.Open(path, readOnly: false);
        var engine = new QueryEngine(database);
        foreach (string s in statements)
        {
            try { outcome.Add(Affected(s, engine.ExecuteNonQuery(s))); }
            catch (Exception e) when (IsLibRedRefusal(e)) { outcome.Add("refused"); }
        }
        foreach (string query in ReadBack)
        {
            try { outcome.Add(Rows(engine.ExecuteQuery(query).Rows)); }
            catch (Exception e) when (IsLibRedRefusal(e)) { outcome.Add("refused"); }
        }
        return string.Join(" | ", outcome);
    }

    private static bool IsLibRedRefusal(Exception e) =>
        e is InvalidOperationException or ArgumentException or InvalidCastException or FormatException or OverflowException
            or DivideByZeroException or NotSupportedException or LibRed.Sql.Binding.SqlBindException or LibRed.Sql.Parsing.SqlParseException;

    private static string Describe(string[] statements, Func<string, string[], string> run)
    {
        string path = TemporaryDatabase.CopyPath(
            Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "yesno-store-");
        try { return run(path, statements); }
        finally { TemporaryDatabase.Delete(path); }
    }

    private static string Affected(string statement, int rows) =>
        statement.StartsWith("INSERT", StringComparison.Ordinal) || statement.StartsWith("UPDATE", StringComparison.Ordinal)
            || statement.StartsWith("DELETE", StringComparison.Ordinal)
            ? rows.ToString(CultureInfo.InvariantCulture) : "ok";

    private static string Rows(IEnumerable<object?[]> rows) =>
        string.Join(" ", rows.Select(row => string.Join(",", row.Select(Format))));

    private static string Format(object? value) => value switch
    {
        null or DBNull => "NULL",
        byte[] bytes => "Byte[]:" + Convert.ToHexString(bytes),
        // By value, not scale: the two readers scale a CURRENCY zero differently whatever wrote it.
        decimal m => "Decimal:" + m.ToString("G29", CultureInfo.InvariantCulture),
        DateTime date => "DateTime:" + date.ToString("o", CultureInfo.InvariantCulture),
        _ => $"{value.GetType().Name}:{Convert.ToString(value, CultureInfo.InvariantCulture)}",
    };
}
