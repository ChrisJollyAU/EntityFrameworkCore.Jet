using System.Data.OleDb;
using System.Globalization;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// A column compared with a value of another kind: text beside a number, a date or a BIGINT; a number or Boolean
/// beside text, a GUID or a binary column; a Currency beside a binary one. Each case's tables are made by ACE and by
/// LibRed, each on its own copy of Northwind, and every query is asked of each engine's own file: the tables' making
/// and every answer must agree; a refusal matches a refusal.
/// <list type="bullet">
/// <item>A comparison ACE answers from an index takes the value as storing it in the column would: text that cannot be
/// stored there finds no row under =, and is a type mismatch as a range bound; a number is a GUID's first four bytes or
/// a binary column's own bytes.</item>
/// <item>One no index answers compares by the kinds alone: text beside a computed number as text, text beside a date as
/// date text, text beside a BIGINT as 0, a Currency beside a binary column as text, a number beside a GUID or binary
/// below every value.</item>
/// </list>
/// </summary>
[Collection(AceCollection.Name)]
public class MixedKindComparisonAccessTests(ITestOutputHelper output) : TempDatabaseTest
{
    /// <summary>Table T of <paramref name="columns"/>, indexed on V or not, holding <paramref name="seed"/>.</summary>
    private static string[] Table(string columns, bool indexed, string[] seed) =>
    [
        $"CREATE TABLE T ({columns})",
        .. indexed ? (string[])["CREATE INDEX IX_T ON T (V)"] : [],
        .. seed.Select((value, i) => $"INSERT INTO T (ID, V) VALUES ({i + 1}, {value})"),
    ];

    public static TheoryData<string, bool> TextColumns => new() { { "TEXT(20)", false }, { "TEXT(20)", true }, { "MEMO", false }, { "MEMO", true } };

    // A text column beside a number or Boolean the statement computes, rather than writes, compares as text - the number
    // as its text, in the column's collation - and a written number is a type mismatch.
    [Theory]
    [MemberData(nameof(TextColumns))]
    public void A_text_column_compared_with_a_computed_number_or_Boolean_compares_as_ACE_compares_it(string type, bool indexed)
    {
        string[] table = Table($"ID LONG, V {type}", indexed, ["'-1'", "'1'", "'0'", "'True'", "'False'", "NULL", "'abc'", "'10'", "'2'", "''"]);
        Matches(table,
        [
            .. ((string[])["=", "<>", "<", ">", "<=", ">="]).SelectMany(op => ((string[])["True", "False", "-1", "0", "1", "2", "1.5", "(1=1)", "(1=0)",
                "CInt(True)", "CInt(0)", "CLng(1)", "1+0", "Abs(1)", "CDbl(2)", "CCur(1)"]).Select(x => $"SELECT ID FROM T WHERE V {op} {x} ORDER BY ID")),
            .. ((string[])["V IN (True)", "V IN (False)", "V IN (1)", "V IN (CInt(1))", "V BETWEEN False AND True", "V BETWEEN CInt(0) AND CInt(2)",
                "True = V", "(1=1) = V", "1 = V", "CInt(1) = V", "V = V + 0", "V & '' = True"]).Select(c => $"SELECT ID FROM T WHERE {c} ORDER BY ID"),
        ]);
        foreach (object parameter in (object[])[true, false, 1, 0, (short)-1, 1.5, 1m])
            Matches(table, [.. ((string[])["=", "<", ">"]).Select(op => $"SELECT ID FROM T WHERE V {op} @p ORDER BY ID")], parameter);
    }

    // Text as written - a text column or written text - beside a number, date or Boolean that is not computed either - a
    // column, a written number or True, an aggregate - is a type mismatch, whichever way round and through whatever
    // comparison, a join's included. Text the statement computes beside it reads as a number - a subquery's among it - and
    // an IIf is the kind of the arm it takes.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Text_as_written_beside_a_number_as_written_is_refused_as_ACE_refuses_it(bool indexed) =>
        Matches(
        [
            "CREATE TABLE T (ID LONG, Q LONG, D DOUBLE, C CURRENCY, DT DATETIME, YN YESNO, V TEXT(20))",
            .. indexed ? (string[])["CREATE INDEX IX_Q ON T (Q)", "CREATE INDEX IX_V ON T (V)"] : [],
            "INSERT INTO T (ID, Q, D, C, DT, YN, V) VALUES (1, 7, 7, 7, #2020-01-02#, True, '7')",
            "INSERT INTO T (ID, Q, D, C, DT, YN, V) VALUES (2, 7, 7.5, 7, #2020-01-02#, False, '7.0')",
            "INSERT INTO T (ID, Q, D, C, DT, YN, V) VALUES (3, 1, 1, 1, #2020-01-02#, True, '01')",
        ],
        [
            .. ((string[])["Q = V", "V = Q", "Q < '9'", "V > 6", "7 = V", "'7' = 7", "D = V", "C = V", "DT = V", "DT = '2020-01-02'",
                "YN = V", "True < V", "V < True", "False < V", "Q IN ('7')", "V IN (7)", "Q BETWEEN '1' AND '9'", "V BETWEEN 1 AND 9",
                "Q = (SELECT MAX(V) FROM T)", "(SELECT MAX(Q) FROM T) = V", "Q = Trim(V)", "Q = V & ''", "Q + 0 = V",
                "IIf(Q > 0, Q + 0, 0) = V", "IIf(Q > 0, Q, 0) = V", "IIf(Q > 0, 7, 0) = '7'", "IIf(Q > 0, Q + 0, 0) = '7.0'",
                "IIf(Q < 0, Q + 0, Q) = V"]).Select(c => $"SELECT ID FROM T WHERE {c} ORDER BY ID"),
            "SELECT ID FROM T GROUP BY ID, V HAVING MAX(Q) = V ORDER BY ID",
            "SELECT T.ID FROM T INNER JOIN T AS R ON T.Q = R.V ORDER BY T.ID",
        ]);

    // Text against the literal True or False is False only as the text "0", compared as text is.
    [Theory]
    [InlineData("TEXT(20)", false)]
    [InlineData("TEXT(20)", true)]
    [InlineData("MEMO", false)]
    public void A_text_column_tested_against_True_or_False_is_True_or_False_as_ACE_takes_it(string type, bool indexed) =>
        Matches(Table($"ID LONG, V {type}", indexed, ["'0'", "'0 '", "' 0'", "'00'", "'-0'", "'0.0'", "''", "'False'", "'7'", "NULL"]),
        [
            .. ((string[])["V = False", "V = True", "False = V", "True = V", "V <> False", "V <> True", "NOT (V = False)"])
                .Select(c => $"SELECT ID FROM T WHERE {c} ORDER BY ID"),
            "SELECT ID, IIF(V = False, 'y', 'n') FROM T ORDER BY ID",
        ]);

    public static TheoryData<string, string, string, bool> BinaryColumns
    {
        get
        {
            var data = new TheoryData<string, string, string, bool>();
            foreach ((string type, string one, string minus) in ((string, string, string)[])
                [
                    ("BINARY(4)", "0x01000000", "0xFFFF0000"), ("BINARY(8)", "0x01000000", "0xFFFF0000"), ("VARBINARY(8)", "0x01000000", "0xFFFF0000"),
                    ("GUID", "{guid {00000001-0000-0000-0000-000000000000}}", "{guid {0000FFFF-0000-0000-0000-000000000000}}"),
                ])
            {
                data.Add(type, one, minus, false);
                data.Add(type, one, minus, true);
            }
            return data;
        }
    }

    // A number or Boolean the statement computes or is passed, compared with a GUID or binary column ACE answers from an
    // index, is taken as storing it in the column takes it - a Long (Int32) a GUID's first four bytes, a number its own
    // bytes without a fixed column's padding, a Currency its eight-byte integer - and one the column refuses finds no
    // row, or is a type mismatch as a range bound. Without the index such a value sorts below every GUID, and beside a
    // binary value compares as text.
    [Theory]
    [MemberData(nameof(BinaryColumns))]
    public void A_number_compared_with_a_GUID_or_binary_column_is_taken_as_ACE_takes_it(string type, string one, string minus, bool indexed)
    {
        string[] table = Table($"ID LONG, V {type}", indexed, [one, minus, "NULL"]);
        Matches(table,
        [
            .. ((string[])["V = 1", "V = -1", "V = True", "V = False", "V <> True", "V = CInt(True)", "V = (1=1)", "V > True", "V >= 1", "V IN (True)",
                "V = '1'", "V <> CInt(True)", "V <> (1=0)", "V < CInt(True)", "V >= (1=1)", "V IN (CInt(True))", "V = CLng(1)", "V = CDbl(1)",
                "NOT (V = CInt(True))", "(V = CInt(True)) IS NULL", "V BETWEEN CLng(0) AND CLng(1)", "V IN (CLng(1), CInt(True))", "V > CLng(1)",
                "V < False", "V >= False", "V = CCur(1)"]).Select(c => $"SELECT ID FROM T WHERE {c} ORDER BY ID"),
        ]);
        foreach (object parameter in (object[])[1, (short)-1, true])
            Matches(table, [.. ((string[])["=", "<>", "<", ">=", ">"]).Select(op => $"SELECT ID FROM T WHERE V {op} @p ORDER BY ID")], parameter);
        if (type == "BINARY(8)")
            foreach (byte[] bytes in (byte[][])[[1, 0, 0, 0], [1, 0, 0, 0, 0, 0, 0, 0]])
                Matches(table, ["SELECT ID FROM T WHERE V = @p ORDER BY ID"], bytes);
        if (type == "GUID" && indexed)
            Matches([.. table, "CREATE TABLE S (ID LONG PRIMARY KEY)", "INSERT INTO S VALUES (1)", "INSERT INTO S VALUES (2)"],
                ["SELECT S.ID FROM S WHERE EXISTS (SELECT 1 FROM T WHERE T.V = CLng(S.ID)) ORDER BY S.ID"]);
    }

    // A binary value as a condition — WHERE V, NOT V, AND, OR, = True, = False, <> True — is False when it equals the
    // text "0" in the database's collation (a NUL ignored, trailing spaces dropped, a full-width zero a zero) and True
    // otherwise; with an index, = False, <> True, NOT (V = True) and NOT V find the row whose bytes are False's Int16 0. IIf calls every
    // value True, and a Boolean the statement computes beside it compares as the text "-1" or "0".
    [Theory]
    [InlineData("VARBINARY(16)", false)]
    [InlineData("VARBINARY(16)", true)]
    [InlineData("BINARY(8)", false)]
    [InlineData("BINARY(8)", true)]
    [InlineData("LONGBINARY", false)]
    public void A_binary_column_tested_for_its_truth_is_True_or_False_as_ACE_takes_it(string type, bool indexed) =>
        Matches(Table($"ID LONG, V {type}", indexed,
        [
            "0x30000000", "0x3000", "0x30003000", "0x31000000", "0x2D003100", "0x00000000", "0x41000000", "0x30004100", "0x20003000",
            "0x30002E00", "0x3000000031000000", "0x0000", "0x01000000", "0xFFFF0000", "0x46006100", "0x30", "0x00003000", "0x30002000",
            "0x10FF", "0x460061006C0073006500", "0x2D003000", "0x300000003000", "0x3000000000000000", "0xFFFF", "0x0100",
        ]), TruthQueries);

    // A GUID is True as a condition, whatever it holds.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_GUID_column_tested_for_its_truth_is_True_as_ACE_takes_it(bool indexed) =>
        Matches(Table("ID LONG, V GUID", indexed,
        [
            "{guid {00000000-0000-0000-0000-000000000000}}", "{guid {00000030-0000-0000-0000-000000000000}}",
            "{guid {00000001-0000-0000-0000-000000000000}}",
        ]), TruthQueries);

    private static readonly string[] TruthQueries =
    [
        "SELECT ID FROM T WHERE V = True ORDER BY ID", "SELECT ID FROM T WHERE V = False ORDER BY ID",
        "SELECT ID FROM T WHERE V <> True ORDER BY ID", "SELECT ID FROM T WHERE True = V ORDER BY ID",
        "SELECT ID, IIF(V, 'y', 'n') FROM T ORDER BY ID", "SELECT ID FROM T WHERE NOT V ORDER BY ID", "SELECT ID FROM T WHERE V ORDER BY ID",
        "SELECT ID FROM T WHERE V <> False ORDER BY ID", "SELECT ID FROM T WHERE False = V ORDER BY ID",
        "SELECT ID FROM T WHERE NOT (V = True) ORDER BY ID", "SELECT ID FROM T WHERE V AND ID > 0 ORDER BY ID",
        "SELECT ID FROM T WHERE V OR ID < 0 ORDER BY ID", "SELECT ID FROM T WHERE NOT V AND ID > 0 ORDER BY ID",
        "SELECT ID, IIF(NOT V, 'y', 'n') FROM T ORDER BY ID", "SELECT ID, IIF(V = True, 'y', 'n') FROM T ORDER BY ID",
        "SELECT ID, IIF(V = False, 'y', 'n') FROM T ORDER BY ID",
        "SELECT ID FROM T WHERE V = (1=1) ORDER BY ID", "SELECT ID FROM T WHERE V = (1=0) ORDER BY ID",
        "SELECT ID FROM T WHERE V = CInt(True) ORDER BY ID", "SELECT ID FROM T WHERE V = CLng(1) ORDER BY ID",
        "SELECT ID FROM T WHERE V = CDbl(1) ORDER BY ID", "SELECT ID FROM T WHERE V = CLng(1) + 0 ORDER BY ID",
        "SELECT ID FROM T WHERE V = CSng(1) ORDER BY ID", "SELECT ID FROM T WHERE V = CByte(1) ORDER BY ID",
        "SELECT ID FROM T WHERE V = 0.5 + 0.5 ORDER BY ID",
    ];

    public static TheoryData<string, bool> NumberAndDateColumns
    {
        get
        {
            var data = new TheoryData<string, bool>();
            foreach (string type in (string[])["BYTE", "SHORT", "LONG", "SINGLE", "DOUBLE", "CURRENCY", "DECIMAL(18,2)"])
            {
                data.Add(type, false);
                data.Add(type, true);
            }
            data.Add("DATETIME", true);
            return data;
        }
    }

    // Text a parameter or function gives, compared with a number or date column ACE answers from an index, is taken as
    // storing it in the column takes it: text that cannot be stored there finds no row under =, and is a type mismatch
    // as a range bound. Without the index the comparison reads the text as a number.
    [Theory]
    [MemberData(nameof(NumberAndDateColumns))]
    public void A_text_compared_with_a_number_or_date_column_finds_what_ACE_finds(string type, bool indexed)
    {
        string[] table = Table($"ID LONG PRIMARY KEY, V {type}", indexed, type switch
        {
            "BYTE" => ["255", "1", "0", "NULL", "2"],
            "DATETIME" => ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL"],
            _ => ["-1", "1", "0", "NULL", "2"],
        });
        string[] texts = ["True", .. type == "DATETIME" ? ["1", "36526", "2000-01-01"] : (string[])[]];
        if (type is "BYTE" or "LONG" or "DOUBLE" or "DATETIME")
            texts = [.. texts, "abc", " 1 ", ""];
        // The row '1.5' rounds to is re-tested against 1.5 rather than the date it was read as, or rounded.
        if (type is "DOUBLE" or "DATETIME" || (!indexed && type is "BYTE" or "LONG"))
            texts = [.. texts, "1.5"];
        foreach (string text in texts)
            Matches(table, ["SELECT ID FROM T WHERE V = @p ORDER BY ID"], text);
        if (type is "BYTE" or "LONG" or "DOUBLE" or "DATETIME")
            foreach (string text in (string[])["True", "abc", "1.5", "1"])
                if (!(type == "DATETIME" && text == "1.5"))
                    Matches(table, ["SELECT ID FROM T WHERE V < @p ORDER BY ID", "SELECT ID FROM T WHERE V >= @p ORDER BY ID"], text);
        if (type is "LONG" or "DATETIME")
            Matches(table, ["SELECT ID FROM T WHERE V = CStr(1) ORDER BY ID", "SELECT ID FROM T WHERE V = 'Tr' & 'ue' ORDER BY ID"]);
        if (type == "LONG")
            Matches(table, ["SELECT ID FROM T WHERE V = @p & '' ORDER BY ID", "SELECT ID FROM T WHERE V = '' & @p ORDER BY ID"], "1");
        if (type == "DATETIME")
            Matches(table, ["SELECT ID FROM T WHERE V > @p ORDER BY ID"], "12/31/1899");
        // Text beside a number the statement computes from the column compares as text, the number as its text: a
        // parameter, a function's result and a literal alike.
        if (type != "DATETIME")
            foreach (string text in (string[])["1.0", "01", "1", "9", "-1"])
            {
                Matches(table, [.. ((string[])["=", "<", ">"]).SelectMany(op => ((string[])["@p", "Trim(@p)", "@p & ''"])
                    .Select(rhs => $"SELECT ID FROM T WHERE V + 0 {op} {rhs} ORDER BY ID"))], text);
                Matches(table, [.. ((string[])["=", "<", ">"]).Select(op => $"SELECT ID FROM T WHERE V + 0 {op} '{text}' ORDER BY ID")]);
                // An aggregate and an IIf are taken as the column is; arithmetic over them, and other functions, are computed.
                Matches(table, ["SELECT ID FROM T WHERE IIF(V > 0, V, 0) = @p ORDER BY ID", "SELECT ID FROM T WHERE IIF(V > 0, 5, 0) = @p ORDER BY ID",
                    "SELECT ID FROM T WHERE IIF(V > 0, V, 0) + 0 = @p ORDER BY ID", "SELECT ID FROM T WHERE Abs(V) = @p ORDER BY ID",
                    "SELECT ID FROM T WHERE @p = V + 0 ORDER BY ID", "SELECT MAX(ID) FROM T GROUP BY V HAVING MAX(V) = @p ORDER BY MAX(ID)",
                    "SELECT MAX(ID) FROM T GROUP BY V HAVING SUM(V) + 0 = @p ORDER BY MAX(ID)"], text);
            }
    }

    // The rows an index finds for text it rounds or reads as a date are the rows returned: they are not tested again
    // against the text as written.
    [Fact]
    public void A_row_an_index_finds_for_converted_text_is_not_rejected_by_the_text_as_written()
    {
        string[] dates = Table("ID LONG PRIMARY KEY, V DATETIME", indexed: true, ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL"]);
        Matches(Table("ID LONG PRIMARY KEY, V BYTE", indexed: true, ["255", "1", "0", "NULL", "2"]), ["SELECT ID FROM T WHERE V = @p ORDER BY ID"], "1.5");
        Matches(Table("ID LONG PRIMARY KEY, V LONG", indexed: true, ["-1", "1", "0", "NULL", "2"]), ["SELECT ID FROM T WHERE V = @p ORDER BY ID"], "1.5");
        Matches(dates, ["SELECT ID FROM T WHERE V < @p ORDER BY ID", "SELECT ID FROM T WHERE V >= @p ORDER BY ID"], "1.5");
        Matches(dates, ["SELECT ID FROM T WHERE V = @p ORDER BY ID", "SELECT ID FROM T WHERE V = @p & '' ORDER BY ID",
            "SELECT ID FROM T WHERE V = '' & @p ORDER BY ID"], "12/31/1899");
        Matches(dates, ["SELECT ID FROM T WHERE V = CStr(#12/31/1899#) ORDER BY ID"]);
    }

    // Text a parameter or function gives, compared with a date column no index answers, is read as date text, and text
    // that is not a date is a type mismatch. Text written in the statement, and an expression over the column, are not.
    [Fact]
    public void A_text_compared_with_an_unindexed_date_column_is_read_as_a_date()
    {
        string[] table = Table("ID LONG PRIMARY KEY, V DATETIME", indexed: false, ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL"]);
        foreach (string text in (string[])["True", "abc", "1.5", "1"])
            Matches(table, ["SELECT ID FROM T WHERE V < @p ORDER BY ID", "SELECT ID FROM T WHERE V >= @p ORDER BY ID"], text);
        foreach (string text in (string[])["abc", " 1 ", "1.5", "", "1", "True", "36526", "2000-01-01", "12/31/1899"])
            Matches(table, ["SELECT ID FROM T WHERE V = @p ORDER BY ID"], text);
        Matches(table, ["SELECT ID FROM T WHERE V > @p ORDER BY ID", "SELECT ID FROM T WHERE V = @p & '' ORDER BY ID",
            "SELECT ID FROM T WHERE V = '' & @p ORDER BY ID"], "12/31/1899");
        Matches(table, ["SELECT ID FROM T WHERE V = CStr(1) ORDER BY ID", "SELECT ID FROM T WHERE V = 'Tr' & 'ue' ORDER BY ID",
            "SELECT ID FROM T WHERE V = CStr(#12/31/1899#) ORDER BY ID"]);
    }

    // Text compared with a BIGINT column: an index takes it as the column stores it; without one it reads as 0, whatever
    // it holds, and an expression over the column compares with it as text.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_text_compared_with_a_BIGINT_column_finds_what_ACE_finds(bool indexed)
    {
        Assert.SkipUnless(
            AceTestDatabase.SupportsColumnType(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "BIGINT"),
            AceTestDatabase.UnsupportedColumnTypeReason("BIGINT"));

        string[] table = Table("ID LONG PRIMARY KEY, V BIGINT", indexed, ["-1", "1", "0", "NULL", "2"]);
        foreach (string text in indexed ? (string[])["2", "-1", "abc", "", "True"] : ["2", "-1", "abc", "", "1.5", "1", "True"])
            Matches(table, ["SELECT ID FROM T WHERE V = @p ORDER BY ID"], text);
        Matches(table, ["SELECT ID FROM T WHERE V > @p ORDER BY ID", "SELECT ID FROM T WHERE V = @p & '' ORDER BY ID",
            "SELECT ID FROM T WHERE V = '' & @p ORDER BY ID", "SELECT ID FROM T WHERE V + 0 = @p ORDER BY ID",
            "SELECT ID, IIF(V = @p, 'T', 'F') FROM T ORDER BY ID"], "1");
        Matches(table, ["SELECT ID FROM T WHERE V = CStr(1) ORDER BY ID"]);

        // Past a Double's 2^53, text beside an expression over the column compares as text, so it loses no digit: a
        // parameter, a function's result and a literal alike.
        string[] wide = Table("ID LONG PRIMARY KEY, V BIGINT", indexed, ["9007199254740992", "9007199254740993", "9223372036854775807", "5"]);
        foreach (string text in (string[])["9007199254740993", "9223372036854775807"])
            Matches(wide, ["SELECT ID FROM T WHERE V + 0 = @p ORDER BY ID", "SELECT ID FROM T WHERE V + 0 < @p ORDER BY ID",
                "SELECT ID FROM T WHERE V * 1 = Trim(@p) ORDER BY ID"], text);
        Matches(wide, ["SELECT ID FROM T WHERE V + 0 = '9007199254740993' ORDER BY ID"]);
        // As text, so text equal as a number but not as text finds nothing.
        foreach (string text in (string[])["9007199254740993.0", "09007199254740993"])
            Matches(wide, ["SELECT ID FROM T WHERE V + 0 = @p ORDER BY ID", "SELECT ID FROM T WHERE V + 0 > @p ORDER BY ID"], text);
    }

    // A text or Double parameter compared with a CURRENCY column is rounded to a Currency's four places first, with an
    // index or without, by =, an IN list of one or two items and BETWEEN alike; a DECIMAL(18,2) column takes it as it is.
    [Theory]
    [InlineData("CURRENCY", true)]
    [InlineData("CURRENCY", false)]
    [InlineData("DECIMAL(18,2)", true)]
    [InlineData("DECIMAL(18,2)", false)]
    public void A_text_or_Double_parameter_compared_with_a_Currency_column_is_rounded_to_a_Currency(string type, bool indexed)
    {
        bool currency = type == "CURRENCY";
        string[] table = Table($"ID LONG PRIMARY KEY, V {type}", indexed, currency ? ["1.2345", "1.2346", "2"] : ["1.23", "1.24", "2"]);
        foreach (object value in currency ? (object[])["1.23455", "1.23456", "1.234549", 1.23455] : ["1.234", "1.235", "1.236", 1.235])
            Matches(table, ["SELECT ID FROM T WHERE V = @p ORDER BY ID", "SELECT ID FROM T WHERE V IN (@p) ORDER BY ID",
                "SELECT ID FROM T WHERE V IN (@p, @p) ORDER BY ID", "SELECT ID FROM T WHERE V BETWEEN @p AND @p ORDER BY ID"], value);
    }

    // A Currency value or a Decimal parameter compared with a binary column compares as text when no index answers - the
    // number's text against the binary read as UTF-16 - and as its eight-byte integer when one does. BINARY(8) holds
    // Currency 1 and the text '1'.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_Currency_compared_with_a_binary_column_compares_as_ACE_compares_it(bool indexed)
    {
        string[] table = Table("ID LONG, V BINARY(8)", indexed, ["0x1027000000000000", "0x3100000000000000"]);
        Matches(table, ["SELECT ID FROM T WHERE V = CCur(1) ORDER BY ID", "SELECT ID FROM T WHERE V >= CCur(1) ORDER BY ID",
            "SELECT ID FROM T WHERE V IN (CCur(1)) ORDER BY ID", "SELECT ID FROM T WHERE V IN (CCur(1), CCur(1)) ORDER BY ID",
            "SELECT ID FROM T WHERE V BETWEEN CCur(1) AND CCur(1) ORDER BY ID"]);
        Matches(table, ["SELECT ID FROM T WHERE V = @p ORDER BY ID"], 1m);
    }

    // A value beside a binary column in a UNION is its own bytes, as a binary column stores it, and a parameter of a type
    // Access has no column for is its text. A Decimal whose text is wider than the column is whole in a UNION.
    [Fact]
    public void A_value_in_a_UNION_with_a_binary_column_is_its_bytes_as_ACE_writes_them()
    {
        string[] tables =
        [
            "CREATE TABLE B (ID LONG, V BINARY(4))", "INSERT INTO B VALUES (1, 0x01000000)",
            "CREATE TABLE B16 (ID LONG, V BINARY(16))", "INSERT INTO B16 VALUES (1, 0x01000000)",
            "CREATE TABLE VB16 (ID LONG, V VARBINARY(16))", "INSERT INTO VB16 VALUES (1, 0x01000000)",
            "CREATE TABLE ONE (ID LONG)", "INSERT INTO ONE VALUES (1)",
            "CREATE TABLE D (ID LONG, X DECIMAL(18,3))", "INSERT INTO D VALUES (1, 1234567.891)",
        ];
        Matches(tables,
        [
            .. ((string[])["CInt(-1)", "CLng(1)", "CSng(1)", "CDbl(1)", "#1/1/2000#", "'A'", "True", "CCur(1)", "1.5", "3000000000",
                "{guid {00000001-0002-0003-0004-000000000005}}"]).Select(x => $"SELECT V FROM B UNION ALL SELECT {x} FROM ONE"),
            .. ((string[])["B", "B16", "VB16"]).SelectMany(b => (string[])
                [$"SELECT V FROM {b} UNION ALL SELECT X FROM D", $"SELECT X FROM D UNION ALL SELECT V FROM {b}"]),
        ]);
        foreach (object parameter in (object[])[(sbyte)1, (ushort)1, 1u, 1ul, 'A'])
            Matches(tables, ["SELECT V FROM B UNION ALL SELECT @p FROM ONE"], parameter);
    }

    // A binary column compared with a GUID or BIGINT column's value, as it is and computed, with an index on the binary
    // column and without.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_binary_column_compared_with_a_GUID_column_matches_as_ACE_matches_it(bool indexed) =>
        Matches(
        [
            "CREATE TABLE G (ID LONG, G GUID)", "INSERT INTO G VALUES (1, {guid {00000001-0002-0003-0004-000000000005}})",
            "INSERT INTO G VALUES (2, {guid {00000001-0000-0000-0000-000000000000}})",
            "CREATE TABLE T (ID LONG, B VARBINARY(50))", .. indexed ? (string[])["CREATE INDEX IX_T ON T (B)"] : [],
            "INSERT INTO T (ID, B) SELECT ID, G FROM G", "INSERT INTO T VALUES (20, 0x01000000)",
        ],
        ["SELECT T.ID, G.ID FROM T, G WHERE T.B = G.G ORDER BY T.ID, G.ID", "SELECT T.ID, G.ID FROM T, G WHERE G.G = T.B ORDER BY T.ID, G.ID"]);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_binary_column_compared_with_a_BIGINT_column_matches_as_ACE_matches_it(bool indexed)
    {
        Assert.SkipUnless(
            AceTestDatabase.SupportsColumnType(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "BIGINT"),
            AceTestDatabase.UnsupportedColumnTypeReason("BIGINT"));
        Matches(
        [
            "CREATE TABLE L (ID LONG, L BIGINT)", "INSERT INTO L VALUES (1, 123456789)", "INSERT INTO L VALUES (2, 1)",
            "CREATE TABLE T (ID LONG, B VARBINARY(50))", .. indexed ? (string[])["CREATE INDEX IX_T ON T (B)"] : [],
            "INSERT INTO T (ID, B) SELECT ID, L FROM L", "INSERT INTO T VALUES (20, 0x01000000)", "INSERT INTO T VALUES (21, 0x15CD5B0700000000)",
            "CREATE TABLE Q (ID LONG, X BIGINT)", "INSERT INTO Q VALUES (1, 1)", "CREATE TABLE B (ID LONG, V BINARY(4))", "INSERT INTO B VALUES (1, 0x01000000)",
            "CREATE TABLE B16 (ID LONG, V BINARY(16))", "INSERT INTO B16 VALUES (1, 0x01000000)",
            "CREATE TABLE VB16 (ID LONG, V VARBINARY(16))", "INSERT INTO VB16 VALUES (1, 0x01000000)",
        ],
        [
            .. ((string[])["T.B = L.L", "T.B = L.L + 0", "L.L = T.B", "L.L + 0 = T.B"])
                .Select(c => $"SELECT T.ID, L.ID FROM T, L WHERE {c} ORDER BY T.ID, L.ID"),
            "SELECT V FROM B UNION ALL SELECT X FROM Q", "SELECT V FROM B16 UNION ALL SELECT X FROM Q", "SELECT V FROM VB16 UNION ALL SELECT X FROM Q",
        ]);
    }

    /// <summary>Makes the tables through ACE and through LibRed, each on its own copy of Northwind, then asks each query
    /// of each engine's own file, binding <paramref name="parameter"/> to @p when there is one.</summary>
    private void Matches(string[] setup, string[] queries, object? parameter = null)
    {
        string[] ace = Describe(setup, queries, parameter, Ace), libred = Describe(setup, queries, parameter, LibRed);
        var differences = Enumerable.Range(0, ace.Length).Where(i => ace[i] != libred[i])
            .Select(i => $"{(i == 0 ? "setup" : queries[i - 1])} [@p = {parameter}]: ACE {ace[i]}, LibRed {libred[i]}").ToList();
        foreach (string difference in differences) output.WriteLine(difference);
        Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences));
    }

    private static string[] Describe(string[] setup, string[] queries, object? parameter, Func<string, string[], string[], object?, string[]> run)
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "mixedkind-");
        try { return run(path, setup, queries, parameter); }
        finally
        {
            OleDbConnection.ReleaseObjectPool();
            TemporaryDatabase.Delete(path);
        }
    }

    /// <summary>Each setup statement's outcome through ACE — the rows it affected, or "refused" — then the rows each
    /// query returns, or "refused".</summary>
    private static string[] Ace(string path, string[] setup, string[] queries, object? parameter)
    {
        using OleDbConnection connection = AceTestDatabase.Open(path);
        var made = new List<string>();
        foreach (string statement in setup)
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = statement;
            try { made.Add(command.ExecuteNonQuery().ToString(CultureInfo.InvariantCulture)); }
            catch (OleDbException) { made.Add("refused"); }
        }
        return [string.Join(" ", made), .. queries.Select(query =>
        {
            using OleDbCommand command = connection.CreateCommand();
            command.CommandText = query;
            if (parameter is not null) command.Parameters.AddWithValue("@p", parameter);
            try
            {
                using OleDbDataReader reader = command.ExecuteReader();
                var rows = new List<object?[]>();
                while (reader.Read())
                    rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(reader.GetValue)]);
                return Rows(rows);
            }
            catch (OleDbException) { return "refused"; }
        })];
    }

    /// <summary>The same through LibRed's engine. A refusal is one of the exceptions LibRed refuses a statement with; any
    /// other is a fault, and fails the test.</summary>
    private static string[] LibRed(string path, string[] setup, string[] queries, object? parameter)
    {
        using var database = JetDatabase.Open(path, readOnly: false);
        var engine = new QueryEngine(database);
        var made = new List<string>();
        foreach (string statement in setup)
        {
            try { made.Add(engine.ExecuteNonQuery(statement).ToString(CultureInfo.InvariantCulture)); }
            catch (Exception e) when (IsLibRedRefusal(e)) { made.Add("refused"); }
        }
        Dictionary<string, object?>? parameters = parameter is null ? null : new() { ["p"] = parameter };
        return [string.Join(" ", made), .. queries.Select(query =>
        {
            try { return Rows(engine.ExecuteQuery(query, parameters).Rows); }
            catch (Exception e) when (IsLibRedRefusal(e)) { return "refused"; }
        })];
    }

    private static bool IsLibRedRefusal(Exception e) =>
        e is InvalidOperationException or ArgumentException or InvalidCastException or FormatException or OverflowException
            or DivideByZeroException or NotSupportedException or global::LibRed.Sql.Binding.SqlBindException
            or global::LibRed.Sql.Parsing.SqlParseException;

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
