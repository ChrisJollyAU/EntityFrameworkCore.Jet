using System.Data.OleDb;
using System.Globalization;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// A Boolean meeting a column, and a value meeting a Yes/No column, in a query. Each case's tables are made by ACE and
/// by LibRed, each on its own copy of Northwind, and every query is asked of each engine's own file: the tables'
/// making and every answer must agree; a refusal matches a refusal.
/// <list type="bullet">
/// <item><c>col = True</c> and <c>col &lt;&gt; True</c> test a number, text or date column's truth, with an index or
/// without; every other comparison takes the Boolean as -1 or 0.</item>
/// <item>A value looked up in a Yes/No column — by a join, a subquery or an index the column leads — is taken by its
/// truth, so 1 and 2 find True; looked up the other way, or under <c>&lt;&gt;</c> or NOT, it is a number.</item>
/// <item>An outer join's unmatched Yes/No reads as False in the rows a query returns, and as Null to WHERE.</item>
/// </list>
/// </summary>
[Collection(AceCollection.Name)]
public class YesNoComparisonAccessTests(ITestOutputHelper output) : TempDatabaseTest
{
    private static readonly string[] Types =
        ["BYTE", "SHORT", "LONG", "SINGLE", "DOUBLE", "CURRENCY", "DECIMAL(18,2)", "TEXT(20)", "MEMO", "DATETIME"];

    private static string[] Seed(string type) => type switch
    {
        "BYTE" => ["255", "1", "0", "NULL", "2"],
        "TEXT(20)" or "MEMO" => ["'-1'", "'1'", "'0'", "'True'", "'False'", "NULL"],
        "TEXT(10)" => ["'-1'", "'1'", "'0'", "NULL", "'2'"],
        "DATETIME" => ["#12/29/1899#", "#12/31/1899#", "#12/30/1899#", "NULL", "#1/1/1900#"],
        "YESNO" => ["True", "True", "False", "NULL", "True"],
        _ => ["-1", "1", "0", "NULL", "2"],
    };

    /// <summary>Table T of the type, indexed on V or not, holding <see cref="Seed"/>.</summary>
    private static string[] Table(string type, bool indexed) =>
    [
        $"CREATE TABLE T (ID LONG PRIMARY KEY, V {type})",
        .. indexed ? (string[])["CREATE INDEX IX_T ON T (V)"] : [],
        .. Seed(type).Select((value, i) => $"INSERT INTO T (ID, V) VALUES ({i + 1}, {value})"),
    ];

    /// <summary>S has no index on Y, SI one, SB one over twelve rows.</summary>
    private static readonly string[] YesNoTables =
    [
        "CREATE TABLE S (ID LONG PRIMARY KEY, Y YESNO)", "INSERT INTO S VALUES (1, True)", "INSERT INTO S VALUES (2, False)",
        "CREATE TABLE SI (ID LONG PRIMARY KEY, Y YESNO)", "CREATE INDEX IX_SI ON SI (Y)", "INSERT INTO SI VALUES (1, True)",
        "INSERT INTO SI VALUES (2, False)",
        "CREATE TABLE ST (ID LONG PRIMARY KEY, Y YESNO)", "INSERT INTO ST VALUES (1, True)",
        "CREATE TABLE SB (ID LONG PRIMARY KEY, Y YESNO)", "CREATE INDEX IX_SB ON SB (Y)",
        .. Enumerable.Range(1, 12).Select(i => $"INSERT INTO SB VALUES ({i}, {(i % 2 == 1 ? "True" : "False")})"),
    ];

    public static TheoryData<string, bool> TypesIndexedOrNot
    {
        get
        {
            var data = new TheoryData<string, bool>();
            foreach (string type in Types)
            {
                data.Add(type, false);
                data.Add(type, true);
            }
            return data;
        }
    }

    // `= True` and `<> True` test the column's truth; everything else compares it with -1 or 0.
    [Theory]
    [MemberData(nameof(TypesIndexedOrNot))]
    public void A_Boolean_compared_with_a_column_selects_the_rows_ACE_selects_with_an_index_or_without(string type, bool indexed)
    {
        string[] conditions = type is "TEXT(20)" or "MEMO"
            ? ["V = True", "True = V", "V <> True", "V = False", "V <> False", "V < True", "V IN (True)", "NOT (V = True)", "NOT V"]
            : ["V = True", "True = V", "V <> True", "V = False", "V <> False", "V = (1=1)", "V IN (True)",
               "V < True", "V > True", "V <= True", "V >= True", "V < False", "V > False", "V <= False", "V >= False", "NOT (V = True)", "NOT V"];
        Matches(Table(type, indexed), [.. conditions.Select(c => $"SELECT ID FROM T WHERE {c} ORDER BY ID")]);
        Matches(Table(type, indexed), ["SELECT ID FROM T WHERE V = @p ORDER BY ID"], true);
        Matches(Table(type, indexed), ["SELECT ID FROM T WHERE V = @p ORDER BY ID"], false);
    }

    // Looked up in the Yes/No column — an outer join's optional side, or the table ACE makes the inner one (the only one
    // indexed on its column, else the larger) — it is compared by its truth; looked up the other way the Yes/No is -1
    // or 0. A Decimal is always a number.
    [Theory]
    [MemberData(nameof(JoinedTypes))]
    public void A_column_joined_to_a_YesNo_column_joins_the_rows_ACE_joins(string type, bool indexed) =>
        Matches([.. YesNoTables, .. Table(type, indexed)],
        [
            .. ((string[])["S", "SI", "SB"]).SelectMany(s => (string[])
            [
                $"SELECT T.ID, {s}.ID FROM T INNER JOIN {s} ON T.V = {s}.Y ORDER BY T.ID, {s}.ID",
                $"SELECT T.ID, {s}.ID FROM T INNER JOIN {s} ON {s}.Y = T.V ORDER BY T.ID, {s}.ID",
                $"SELECT T.ID, {s}.ID FROM {s} INNER JOIN T ON T.V = {s}.Y ORDER BY T.ID, {s}.ID",
                $"SELECT T.ID, {s}.ID FROM T, {s} WHERE T.V = {s}.Y ORDER BY T.ID, {s}.ID",
                $"SELECT T.ID, {s}.ID FROM T LEFT JOIN {s} ON T.V = {s}.Y ORDER BY T.ID, {s}.ID",
                $"SELECT T.ID, {s}.ID FROM {s} LEFT JOIN T ON T.V = {s}.Y ORDER BY T.ID, {s}.ID",
                $"SELECT T.ID, {s}.ID FROM T INNER JOIN {s} ON T.V <> {s}.Y WHERE {s}.ID < 3 ORDER BY T.ID, {s}.ID",
            ]),
        ]);

    public static TheoryData<string, bool> JoinedTypes
    {
        get
        {
            var data = new TheoryData<string, bool>();
            foreach (string type in (string[])["BYTE", "SHORT", "LONG", "DOUBLE", "CURRENCY", "DECIMAL(18,2)", "DATETIME", "YESNO"])
            {
                data.Add(type, false);
                data.Add(type, true);
            }
            return data;
        }
    }

    // EXISTS, IN and = ANY look the outer value up in the subquery's Yes/No column, so compare it by its truth; NOT IN,
    // <> ALL, a correlated IN and a Boolean the subquery computes compare as numbers, a Decimal always does, and a text
    // column beside a Yes/No column is a type mismatch. NOT EXISTS and a correlated scalar subquery compare as numbers
    // over an unindexed Yes/No column and by truth over one that leads an index, which ACE seeks. "BIG" is sixty rows,
    // enough for a correlated EXISTS to leave the row-by-row path for the hashed one.
    [Theory]
    [MemberData(nameof(LookedUpTypes))]
    public void A_value_looked_up_in_a_YesNo_column_by_a_subquery_finds_the_rows_ACE_finds(string type, bool indexed)
    {
        string[] table = type == "BIG"
            ? ["CREATE TABLE T (ID LONG PRIMARY KEY, V LONG)",
               .. Enumerable.Range(1, 60).Select(i => $"INSERT INTO T VALUES ({i}, {(i % 5) switch { 0 => "-1", 1 => "1", 2 => "0", 3 => "2", _ => "NULL" }})")]
            : Table(type, indexed);
        Matches([.. YesNoTables, .. table],
        [
            .. ((string[])["S", "SI", "ST"]).SelectMany(s => (string[])
            [
                $"SELECT T.ID FROM T WHERE EXISTS (SELECT 1 FROM {s} WHERE {s}.Y = T.V) ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE EXISTS (SELECT 1 FROM {s} WHERE T.V = {s}.Y) ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE EXISTS (SELECT 1 FROM {s} WHERE {s}.Y = T.V AND {s}.ID = 1) ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE EXISTS (SELECT 1 FROM {s} WHERE {s}.Y <> T.V) ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE T.V IN (SELECT Y FROM {s}) ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE T.V NOT IN (SELECT Y FROM {s}) ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE T.V IN (SELECT Y FROM {s} WHERE ID = 1) ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE T.V IN (SELECT (ID = 1) FROM {s}) ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE T.V IN (SELECT CInt(Y) FROM {s}) ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE T.V IN (SELECT Y FROM {s} WHERE {s}.ID = T.ID) ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE T.V = ANY (SELECT Y FROM {s}) ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE T.V <> ALL (SELECT Y FROM {s}) ORDER BY T.ID",
                $"SELECT {s}.ID FROM {s} WHERE EXISTS (SELECT 1 FROM T WHERE T.V = {s}.Y) ORDER BY {s}.ID",
                $"SELECT {s}.ID FROM {s} WHERE {s}.Y IN (SELECT V FROM T) ORDER BY {s}.ID",
                $"SELECT {s}.ID, (SELECT COUNT(*) FROM T WHERE T.V = {s}.Y) FROM {s} ORDER BY {s}.ID",
                $"SELECT T.ID FROM T WHERE NOT EXISTS (SELECT 1 FROM {s} WHERE {s}.Y = T.V) ORDER BY T.ID",
                $"SELECT T.ID, (SELECT COUNT(*) FROM {s} WHERE {s}.Y = T.V) FROM T ORDER BY T.ID",
                $"SELECT T.ID, (SELECT MAX({s}.ID) FROM {s} WHERE {s}.Y = T.V) FROM T ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE (SELECT COUNT(*) FROM {s} WHERE {s}.Y = T.V) > 0 ORDER BY T.ID",
            ]),
        ]);
    }

    public static TheoryData<string, bool> LookedUpTypes
    {
        get
        {
            var data = new TheoryData<string, bool>();
            foreach (string type in (string[])["BYTE", "SHORT", "LONG", "DOUBLE", "CURRENCY", "DECIMAL(18,2)", "DATETIME", "YESNO", "TEXT(10)"])
            {
                data.Add(type, false);
                data.Add(type, true);
            }
            data.Add("BIG", false);
            return data;
        }
    }

    /// <summary>S: no index. SI: an index on Y. SU: a unique one. SC: a composite (Y, ID). SK: (ID, Y), which Y does not
    /// lead. SB: an index on Y over twelve rows.</summary>
    private static readonly string[] IndexedYesNoTables =
    [
        .. ((string[])["S", "SI", "SU", "SC", "SK"]).SelectMany(s => (string[])
            [$"CREATE TABLE {s} (ID LONG PRIMARY KEY, Y YESNO)", $"INSERT INTO {s} VALUES (1, True)", $"INSERT INTO {s} VALUES (2, False)"]),
        "CREATE INDEX IX_SI ON SI (Y)", "CREATE UNIQUE INDEX IX_SU ON SU (Y)", "CREATE INDEX IX_SC ON SC (Y, ID)", "CREATE INDEX IX_SK ON SK (ID, Y)",
        "CREATE TABLE SB (ID LONG PRIMARY KEY, Y YESNO)", "CREATE INDEX IX_SB ON SB (Y)",
        .. Enumerable.Range(1, 12).Select(i => $"INSERT INTO SB VALUES ({i}, {(i % 2 == 1 ? "True" : "False")})"),
    ];

    public static TheoryData<string> IndexedYesNo => ["S", "SI", "SU", "SC", "SK", "SB"];

    // A comparison ACE answers from an index a Yes/No column leads - =, <, >, <=, >=, IN, BETWEEN, alone or under AND
    // and OR - takes the other value by its truth, so Y = 2 finds the True rows; without such an index, under <> or
    // NOT, and for a written Decimal, it compares as a number. Written text is a type mismatch.
    [Theory]
    [MemberData(nameof(IndexedYesNo))]
    public void A_value_compared_with_a_YesNo_column_that_leads_an_index_is_taken_by_its_truth_as_ACE_takes_it(string s)
    {
        Matches(IndexedYesNoTables,
        [
            .. ((string[])["=", "<>"]).SelectMany(op => ((string[])["2", "1", "-1", "0", "2.5", "0.0", "-1.0", "CLng(2)", "CInt(2)", "CInt(-1)",
                "CDbl(2)", "CSng(2)", "CCur(2)", "CByte(2)", "#1/1/1900#", "#12/30/1899#", "'2'", "'-1'", "'abc'", "1+1", "(1=1)"])
                .Select(x => $"SELECT ID FROM {s} WHERE Y {op} {x} ORDER BY ID")),
            .. ((string[])["Y IN (2)", "Y IN (CLng(2))", "Y BETWEEN 1 AND 3", "Y > 0", "Y < 0", "Y >= CLng(2)", "2 = Y", "CLng(2) = Y",
                "Y = 2 OR ID = 2", "NOT (Y = CLng(2))", "Y + 0 = 2", "CLng(Y) = 2", "Y = NULL",
                "Y = False", "Y <> True", "NOT (Y = True)", "NOT Y"]).Select(c => $"SELECT ID FROM {s} WHERE {c} ORDER BY ID"),
            // Where Y only follows ID in an index, ACE still answers this pair from it; LibRed does not.
            .. s == "SK" ? [] : (string[])[$"SELECT ID FROM {s} WHERE Y = 2 AND ID = 1 ORDER BY ID", $"SELECT ID FROM {s} WHERE Y = CLng(2) AND ID = 1 ORDER BY ID"],
        ]);
        foreach (object parameter in (object[])[2, (short)2, (byte)2, 2d, 2f, 2m, 2.5m, -1, 0, true, "2"])
            Matches(IndexedYesNoTables, [$"SELECT ID FROM {s} WHERE Y = @p ORDER BY ID", $"SELECT ID FROM {s} WHERE Y <> @p ORDER BY ID"], parameter);
    }

    // NOT EXISTS and a correlated scalar subquery over each of those tables, with the outer value from a column of each type.
    [Theory]
    [InlineData("BYTE")]
    [InlineData("LONG")]
    [InlineData("DOUBLE")]
    [InlineData("CURRENCY")]
    [InlineData("DECIMAL(18,2)")]
    [InlineData("DATETIME")]
    [InlineData("YESNO")]
    // S and SI, which the subquery theory above asks the same of over the same rows, are asked here only what it does not.
    public void A_correlated_lookup_in_a_YesNo_column_answers_as_ACE_answers_it_whichever_index_it_has(string type) =>
        Matches([.. IndexedYesNoTables, .. Table(type, indexed: false)],
        [
            .. ((string[])["S", "SI", "SU", "SC", "SK", "SB"]).SelectMany(s => (string[])
            [
                $"SELECT T.ID FROM T WHERE NOT EXISTS (SELECT 1 FROM {s} WHERE T.V = {s}.Y) ORDER BY T.ID",
                $"SELECT T.ID, (SELECT COUNT(*) FROM {s} WHERE T.V = {s}.Y) FROM T ORDER BY T.ID",
                $"SELECT T.ID, (SELECT TOP 1 {s}.ID FROM {s} WHERE {s}.Y = T.V ORDER BY {s}.ID) FROM T ORDER BY T.ID",
                $"SELECT T.ID, (SELECT COUNT(*) FROM {s} WHERE {s}.Y <> T.V) FROM T ORDER BY T.ID",
                $"SELECT T.ID, (SELECT COUNT(*) FROM {s} WHERE {s}.Y = T.V + 0) FROM T ORDER BY T.ID",
            ]),
            .. ((string[])["SU", "SC", "SK", "SB"]).SelectMany(s => (string[])
            [
                $"SELECT T.ID FROM T WHERE NOT EXISTS (SELECT 1 FROM {s} WHERE {s}.Y = T.V) ORDER BY T.ID",
                $"SELECT T.ID, (SELECT COUNT(*) FROM {s} WHERE {s}.Y = T.V) FROM T ORDER BY T.ID",
                $"SELECT T.ID, (SELECT MAX({s}.ID) FROM {s} WHERE {s}.Y = T.V) FROM T ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE (SELECT COUNT(*) FROM {s} WHERE {s}.Y = T.V) > 0 ORDER BY T.ID",
                $"SELECT T.ID FROM T WHERE EXISTS (SELECT 1 FROM {s} WHERE {s}.Y = T.V) ORDER BY T.ID",
            ]),
        ]);

    // The rows a query returns read a Yes/No column from an outer join's unmatched side as False - the column, the
    // expressions over it, ORDER BY and DISTINCT - while WHERE and an aggregate see the Null, and grouping by the column
    // is refused.
    [Fact]
    public void A_YesNo_column_on_the_unmatched_side_of_an_outer_join_reads_as_ACE_reads_it()
    {
        const string oj = "FROM L LEFT JOIN YS ON L.ID = YS.ID";
        Matches(
        [
            "CREATE TABLE S (ID LONG PRIMARY KEY, Y YESNO)", "INSERT INTO S VALUES (1, True)", "INSERT INTO S VALUES (2, False)",
            "CREATE TABLE YS (ID LONG PRIMARY KEY, F YESNO, G YESNO, T TEXT(10), N LONG)",
            "INSERT INTO YS VALUES (1, True, False, 'a', 5)", "INSERT INTO YS VALUES (2, False, True, NULL, NULL)",
            "CREATE TABLE L (ID LONG PRIMARY KEY)", "INSERT INTO L VALUES (1)", "INSERT INTO L VALUES (2)", "INSERT INTO L VALUES (3)",
            "INSERT INTO L VALUES (4)",
            "CREATE VIEW VOJ AS SELECT L.ID, YS.F, YS.T FROM L LEFT JOIN YS ON L.ID = YS.ID",
        ],
        [
            $"SELECT L.ID, YS.F, YS.G, YS.T, YS.N {oj} ORDER BY L.ID",
            $"SELECT * {oj} ORDER BY L.ID",
            $"SELECT YS.* {oj} ORDER BY L.ID",
            $"SELECT L.ID, IIF(YS.F IS NULL, 'n', 'v') {oj} ORDER BY L.ID",
            $"SELECT L.ID, IIF(YS.F, 'y', 'n') {oj} ORDER BY L.ID",
            $"SELECT L.ID, YS.F & '' {oj} ORDER BY L.ID",
            $"SELECT L.ID, YS.F + 0 {oj} ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE YS.F IS NULL ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE YS.F IS NOT NULL ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE YS.F = False ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE YS.F = True ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE YS.F <> True ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE NOT YS.F ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE YS.F ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE IIF(YS.F IS NULL, 1, 0) = 1 ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE IsNull(YS.F) ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE YS.F + 0 = 0 ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE YS.F = YS.G ORDER BY L.ID",
            $"SELECT L.ID {oj} WHERE YS.F IS NULL OR L.ID = 1 ORDER BY L.ID",
            $"SELECT L.ID, YS.F {oj} WHERE YS.F IS NULL ORDER BY L.ID",
            $"SELECT L.ID, YS.F {oj} ORDER BY YS.F, L.ID",
            $"SELECT L.ID, YS.F {oj} ORDER BY YS.F DESC, L.ID",
            $"SELECT L.ID, YS.T {oj} ORDER BY YS.T, L.ID",
            $"SELECT L.ID {oj} ORDER BY YS.F, L.ID DESC",
            $"SELECT L.ID {oj} ORDER BY NOT YS.F, L.ID",
            $"SELECT L.ID {oj} ORDER BY IIF(YS.F IS NULL, 0, 1), L.ID DESC",
            $"SELECT L.ID, YS.F AS X {oj} ORDER BY YS.F, L.ID",
            $"SELECT TOP 1 L.ID {oj} ORDER BY YS.F DESC, L.ID DESC",
            $"SELECT DISTINCT YS.F {oj}",
            $"SELECT DISTINCT YS.F, YS.T {oj}",
            $"SELECT YS.F, COUNT(*) {oj} GROUP BY YS.F",
            $"SELECT YS.G, COUNT(*) {oj} GROUP BY YS.G",
            $"SELECT L.ID, YS.F {oj} GROUP BY L.ID, YS.F ORDER BY L.ID",
            $"SELECT L.ID {oj} GROUP BY L.ID, YS.F HAVING YS.F IS NULL ORDER BY L.ID",
            $"SELECT YS.F, COUNT(*) {oj} WHERE L.ID < 3 GROUP BY YS.F",
            "SELECT YS.F, COUNT(*) FROM L INNER JOIN YS ON L.ID = YS.ID GROUP BY YS.F",
            $"SELECT YS.T, COUNT(*) {oj} GROUP BY YS.T",
            $"SELECT IIF(YS.F, 1, 0), COUNT(*) {oj} GROUP BY IIF(YS.F, 1, 0)",
            $"SELECT COUNT(YS.F), COUNT(YS.T), COUNT(*) {oj}",
            $"SELECT D.ID FROM (SELECT L.ID, YS.F {oj}) AS D WHERE D.F IS NULL ORDER BY D.ID",
            $"SELECT D.ID FROM (SELECT L.ID, YS.F {oj}) AS D WHERE D.F = False ORDER BY D.ID",
            $"SELECT D.ID, D.F FROM (SELECT L.ID, YS.F {oj}) AS D ORDER BY D.ID",
            $"SELECT E.ID, E.F FROM (SELECT D.ID, D.F FROM (SELECT L.ID, YS.F {oj}) AS D) AS E ORDER BY E.ID",
            $"SELECT D.ID FROM (SELECT L.ID, NOT YS.F AS X {oj}) AS D WHERE D.X IS NULL ORDER BY D.ID",
            "SELECT * FROM VOJ ORDER BY ID",
            "SELECT ID FROM VOJ WHERE F IS NULL ORDER BY ID",
            "SELECT ID FROM VOJ WHERE F = False ORDER BY ID",
            "SELECT L.ID, YS.F FROM YS RIGHT JOIN L ON L.ID = YS.ID ORDER BY L.ID",
            "SELECT L.ID FROM YS RIGHT JOIN L ON L.ID = YS.ID WHERE YS.F IS NULL ORDER BY L.ID",
            "SELECT L.ID, YS.F FROM L LEFT JOIN YS ON (L.ID = YS.ID AND YS.F = True) ORDER BY L.ID",
            "SELECT L.ID, S.ID FROM (L LEFT JOIN YS ON L.ID = YS.ID) LEFT JOIN S ON YS.F = S.Y ORDER BY L.ID, S.ID",
            "SELECT L.ID, YS.F, S.ID FROM (L LEFT JOIN YS ON L.ID = YS.ID) LEFT JOIN S ON YS.F = S.Y ORDER BY L.ID, S.ID",
            "SELECT YS.ID, YS.F, L.ID FROM YS LEFT JOIN L ON L.ID = YS.ID + 2 ORDER BY YS.ID",
        ]);
    }

    /// <summary>S has no index beside its primary key; SI has one on Y, SC on (Y, ID), SK on (ID, Y), SN on (N, Y), SX
    /// one on Y and one on X. TV holds the values looked up in them.</summary>
    private static readonly string[] MixedIndexTables =
    [
        .. ((string[])["S", "SI", "SC", "SK", "SN", "SX"]).SelectMany(s => (string[])
            [$"CREATE TABLE {s} (ID LONG PRIMARY KEY, Y YESNO, N LONG, X LONG, T TEXT(10), D DATETIME)",
             $"INSERT INTO {s} VALUES (1, True, 1, 1, 'a', #1/1/2000#)", $"INSERT INTO {s} VALUES (2, False, 2, 2, NULL, NULL)"]),
        "CREATE INDEX IX_SI ON SI (Y)", "CREATE INDEX IX_SC ON SC (Y, ID)", "CREATE INDEX IX_SK ON SK (ID, Y)", "CREATE INDEX IX_SN ON SN (N, Y)",
        "CREATE INDEX IX_SXY ON SX (Y)", "CREATE INDEX IX_SXX ON SX (X)",
        "CREATE TABLE TV (ID LONG PRIMARY KEY, V LONG)", "INSERT INTO TV VALUES (1, -1)", "INSERT INTO TV VALUES (2, 1)",
        "INSERT INTO TV VALUES (3, 0)", "INSERT INTO TV VALUES (4, NULL)", "INSERT INTO TV VALUES (5, 2)",
    ];

    public static TheoryData<string> MixedIndexes => ["S", "SI", "SC", "SK", "SN", "SX"];

    [Fact]
    public void A_written_Null_on_the_right_of_unequal_selects_the_rows_ACE_selects()
    {
        string[] columns = ["Y", "N", "T", "D", "ID"];
        Matches(MixedIndexTables,
        [
            .. columns.SelectMany(c => (string[])
            [
                $"SELECT ID FROM S WHERE {c} <> NULL ORDER BY ID", $"SELECT ID FROM S WHERE NULL <> {c} ORDER BY ID",
                $"SELECT ID FROM S WHERE NOT ({c} <> NULL) ORDER BY ID", $"SELECT ID FROM S WHERE {c} = NULL ORDER BY ID",
            ]),
        ]);
        Matches(MixedIndexTables, [.. columns.Select(c => $"SELECT ID FROM S WHERE {c} <> @p ORDER BY ID")], DBNull.Value);
    }

    [Theory]
    [MemberData(nameof(MixedIndexes))]
    public void A_YesNo_column_compared_beside_other_conditions_selects_the_rows_ACE_selects_whichever_index_it_has(string s) =>
        Matches(MixedIndexTables,
        [
            .. ((string[])["ID = 1 AND Y >= 2", "ID < 3 AND Y >= 2", "N = 1 AND Y >= 2", "N = 1 AND ID = 1 AND Y >= 2", "X = 1 AND Y >= 2",
                "Y = 2 AND ID = 1", "Y = 2 OR N = 99", "Y = 2 OR ID = 5", "Y = 2 OR Y = 0", "(Y = 2 OR N = 99) AND ID = 1", "Y = 2 OR X = 99",
                "Y >= 2 OR N = 99", "Y IN (2) OR N = 99", "NOT (Y <> 2)", "Y = 2 AND (N = 1 OR N = 99)", "Y = 2 OR (ID = 5 AND N = 5)"])
                .Select(w => $"SELECT ID FROM {s} WHERE {w} ORDER BY ID"),
            .. ((string[])[$"{s}.Y = TV.V", $"{s}.Y >= TV.V", $"{s}.Y = 2", $"{s}.Y = 2 AND {s}.ID = 1",
                $"{s}.ID = 1 AND {s}.Y = TV.V", $"{s}.ID = 1 AND {s}.Y >= TV.V", $"{s}.ID < 3 AND {s}.Y = TV.V", $"{s}.ID < 3 AND {s}.Y >= TV.V",
                $"{s}.N = 1 AND {s}.Y = TV.V", $"{s}.N = 1 AND {s}.ID = 1 AND {s}.Y = TV.V", $"{s}.X = 1 AND {s}.Y = TV.V", $"{s}.X < 3 AND {s}.Y >= TV.V",
                $"{s}.ID = TV.ID AND {s}.Y = 2", $"{s}.N = TV.ID AND {s}.Y = 2", $"{s}.X = TV.ID AND {s}.Y = 2", $"{s}.ID = TV.V AND {s}.Y = 2",
                $"{s}.ID = TV.ID AND {s}.Y >= 2", $"{s}.ID = TV.ID AND {s}.Y = TV.V", $"{s}.N = TV.ID AND {s}.Y = TV.V", $"{s}.ID = TV.ID AND {s}.Y >= TV.V",
                $"{s}.ID < TV.ID + 9 AND {s}.Y = 2", $"{s}.ID < TV.ID + 9 AND {s}.Y = TV.V", $"{s}.N = 1 AND {s}.ID = TV.ID AND {s}.Y = 2"])
                .SelectMany(body => (string[])
                [
                    $"SELECT TV.ID, (SELECT COUNT(*) FROM {s} WHERE {body}) FROM TV ORDER BY TV.ID",
                    $"SELECT TV.ID FROM TV WHERE EXISTS (SELECT * FROM {s} WHERE {body}) ORDER BY TV.ID",
                    $"SELECT TV.ID FROM TV WHERE NOT EXISTS (SELECT * FROM {s} WHERE {body}) ORDER BY TV.ID",
                ]),
        ]);

    [Theory]
    [MemberData(nameof(MixedIndexes))]
    public void A_HAVING_condition_on_a_grouping_YesNo_column_keeps_the_groups_ACE_keeps(string s)
    {
        Matches(MixedIndexTables,
        [
            .. ((string[])["Y = 2", "Y = 2 AND COUNT(*) > 0", "COUNT(*) > 0 AND Y = 2", "Y = CLng(2)", "Y <> 2", "Y = 2 OR COUNT(*) > 99", "2 = Y",
                "Y IN (2)", "MAX(ID) > 0 AND Y = 2", "Y >= 2", "Y BETWEEN 2 AND 3", "NOT (Y = 2)", "Y = 2 OR Y = 0", "Y + 0 = 2"])
                .Select(h => $"SELECT Y, COUNT(*) FROM {s} GROUP BY Y HAVING {h}"),
            .. ((string[])["Y = 2", "Y = 2 AND N = 1", "(Y = 2 OR N = 99)", "Y = 2 AND SUM(N) > 0 AND N > 0"])
                .Select(h => $"SELECT Y, N, COUNT(*) FROM {s} GROUP BY Y, N HAVING {h}"),
            $"SELECT Y, COUNT(*) FROM {s} WHERE ID = 1 GROUP BY Y HAVING Y = 2",
            $"SELECT Y, COUNT(*) FROM {s} WHERE N > 0 GROUP BY Y HAVING Y = 2",
            $"SELECT COUNT(*) FROM {s} GROUP BY Y HAVING Y = 2",
            $"SELECT Y, COUNT(*) FROM {s} GROUP BY Y, ID + 0 HAVING Y = 2",
            $"SELECT d.Y, d.C FROM (SELECT Y, COUNT(*) AS C FROM {s} GROUP BY Y) AS d WHERE d.Y = 2",
            $"SELECT d.Y FROM (SELECT DISTINCT Y FROM {s}) AS d WHERE d.Y = 2",
            $"SELECT TV.ID, (SELECT COUNT(*) FROM (SELECT Y FROM {s} GROUP BY Y HAVING Y = 2) AS g) FROM TV WHERE TV.ID = 1",
            // Written with or without its table.
            $"SELECT {s}.Y, COUNT(*) FROM {s} GROUP BY {s}.Y HAVING Y = 2",
            $"SELECT Y, COUNT(*) FROM {s} GROUP BY Y HAVING {s}.Y = 2",
            $"SELECT a.Y, COUNT(*) FROM {s} AS a GROUP BY a.Y HAVING Y = 2",
            $"SELECT Y, COUNT(*) FROM {s} AS a GROUP BY Y HAVING a.Y = 2",
            $"SELECT {s}.Y, COUNT(*) FROM {s} GROUP BY {s}.Y HAVING {s}.Y = 2",
            $"SELECT {s}.Y, COUNT(*) FROM {s} INNER JOIN TV ON {s}.N = TV.ID GROUP BY {s}.Y HAVING Y = 2",
        ]);
        Matches(MixedIndexTables, [$"SELECT Y, COUNT(*) FROM {s} GROUP BY Y HAVING Y = @p"], 2);
    }

    // The one row of SL a subquery can match is the last outer row read, 20,000 rows in: by then LibRed has stopped
    // reading the subquery for each outer row and reads it once for them all, and the two readings must agree.
    [Fact]
    public void A_correlated_subquery_over_a_YesNo_column_answers_as_ACE_answers_it_over_a_large_outer_table() =>
        Matches(
        [
            "CREATE TABLE DG (D LONG)", .. Enumerable.Range(0, 10).Select(d => $"INSERT INTO DG VALUES ({d})"),
            "CREATE TABLE TL (ID LONG PRIMARY KEY, V LONG)",
            "INSERT INTO TL (ID, V) SELECT q.K, q.K FROM (SELECT a.D + 10 * b.D + 100 * c.D + 1000 * d.D + 10000 * e.D + 1 AS K"
                + " FROM DG AS a, DG AS b, DG AS c, DG AS d, DG AS e) AS q WHERE q.K <= 20000",
            "CREATE TABLE SL (ID LONG PRIMARY KEY, Y YESNO, N LONG)", "CREATE INDEX IX_SL ON SL (Y)",
            "INSERT INTO SL VALUES (20000, True, 20000)", "INSERT INTO SL VALUES (2, False, 2)",
        ],
        [
            .. ((string[])["SL.ID = TL.ID AND SL.Y = 2", "SL.N = TL.ID AND SL.Y = 2", "SL.ID = TL.ID AND SL.Y = True", "SL.ID = TL.ID AND SL.N > 0"])
                .SelectMany(body => ((string[])["TL.ID > 19990", "TL.ID > 0"]).SelectMany(outer => (string[])
                [
                    $"SELECT COUNT(*) FROM TL WHERE {outer} AND NOT EXISTS (SELECT * FROM SL WHERE {body})",
                    $"SELECT COUNT(*) FROM TL WHERE {outer} AND (SELECT COUNT(*) FROM SL WHERE {body}) > 0",
                ])),
        ]);

    // A correlated count over a Yes/No column, with an index and without, compared with the outer row's number or Yes/No
    // value, and the same column on an outer join's unmatched side, over 5 outer rows and over 20,000.
    [Fact]
    public void A_correlated_count_over_a_YesNo_column_answers_as_ACE_answers_it_over_a_small_and_a_large_outer_table() =>
        Matches(
        [
            "CREATE TABLE DG (D LONG)", .. Enumerable.Range(0, 10).Select(d => $"INSERT INTO DG VALUES ({d})"),
            "CREATE TABLE TL (ID LONG PRIMARY KEY, V LONG, B YESNO)",
            "INSERT INTO TL (ID, V, B) SELECT q.K, IIf(q.K Mod 5 = 0, Null, (q.K Mod 4) - 1), (q.K Mod 2 = 0) FROM (SELECT a.D"
                + " + 10 * b.D + 100 * c.D + 1000 * d.D + 10000 * e.D + 1 AS K FROM DG AS a, DG AS b, DG AS c, DG AS d, DG AS e) AS q WHERE q.K <= 20000",
            "CREATE TABLE TS (ID LONG PRIMARY KEY, V LONG, B YESNO)",
            .. ((string[])["1, 0, False", "2, 1, True", "3, 2, True", "4, -1, True", "5, Null, False"]).Select(row => $"INSERT INTO TS VALUES ({row})"),
            .. ((string[])["SI", "SU"]).SelectMany(s => (string[])
                [$"CREATE TABLE {s} (ID LONG PRIMARY KEY, Y YESNO)", $"INSERT INTO {s} VALUES (1, True)", $"INSERT INTO {s} VALUES (2, False)"]),
            "CREATE INDEX IX_SI ON SI (Y)",
            "CREATE TABLE A (ID LONG)", "INSERT INTO A VALUES (1)", "INSERT INTO A VALUES (2)", "INSERT INTO A VALUES (3)",
        ],
        [
            .. ((string[])["SI", "SU"]).SelectMany(s => ((string[])["V", "B"]).SelectMany(column =>
                ((string[])[s, $"A LEFT JOIN {s} ON A.ID = {s}.ID"]).SelectMany(from => (string[])
                [
                    $"SELECT TS.ID, (SELECT COUNT(*) FROM {from} WHERE {s}.Y = TS.{column}) FROM TS ORDER BY TS.ID",
                    $"SELECT TL.{column}, COUNT(*) FROM TL WHERE (SELECT COUNT(*) FROM {from} WHERE {s}.Y = TL.{column}) > 0 GROUP BY TL.{column} ORDER BY TL.{column}",
                    $"SELECT TL.{column}, COUNT(*) FROM TL WHERE (SELECT COUNT(*) FROM {from} WHERE {s}.Y = TL.{column}) = 0 GROUP BY TL.{column} ORDER BY TL.{column}",
                ]))),
        ]);

    // Which table of a join ACE reads first decides how a Yes/No restriction on it is compared: read first, JS.Y = 2 is
    // answered from the index (the True row); looked up for each row of the other table, it is tested on the fetched row
    // as written (no row). JS, of 2 rows, joins tables of 30, 60 and 200 rows.
    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(200)]
    public void Which_table_of_a_join_is_read_first_decides_how_a_YesNo_restriction_on_it_is_compared(int size) =>
        Matches(
        [
            "CREATE TABLE DG (D LONG)", .. Enumerable.Range(0, 10).Select(d => $"INSERT INTO DG VALUES ({d})"),
            "CREATE TABLE JS (ID LONG PRIMARY KEY, Y YESNO)", "CREATE INDEX IX_JS ON JS (Y)",
            "INSERT INTO JS VALUES (1, True)", "INSERT INTO JS VALUES (2, False)",
            "CREATE TABLE JT (ID LONG PRIMARY KEY)",
            $"INSERT INTO JT (ID) SELECT q.K FROM (SELECT a.D + 10 * b.D + 100 * c.D + 1 AS K FROM DG AS a, DG AS b, DG AS c) AS q WHERE q.K <= {size}",
        ],
        [
            "SELECT JS.ID FROM JS INNER JOIN JT ON JS.ID = JT.ID WHERE JS.Y = 2 ORDER BY JS.ID",
            "SELECT JT.ID FROM JS INNER JOIN JT ON JS.ID = JT.ID WHERE JS.Y = 2 ORDER BY JT.ID",
            "SELECT JT.ID FROM JS INNER JOIN JT ON JS.ID = JT.ID WHERE JS.Y = 2 AND JT.ID = 1",
        ]);

    /// <summary>Makes the tables through ACE and through LibRed, each on its own copy of Northwind, then asks each query
    /// of each engine's own file, binding <paramref name="parameter"/> to @p when there is one.</summary>
    private void Matches(string[] setup, string[] queries, object? parameter = null)
    {
        string[] ace = Describe(setup, queries, parameter, Ace), libred = Describe(setup, queries, parameter, LibRed);
        var differences = Enumerable.Range(0, ace.Length).Where(i => ace[i] != libred[i])
            .Select(i => $"{(i == 0 ? "setup" : queries[i - 1])}: ACE {ace[i]}, LibRed {libred[i]}").ToList();
        foreach (string difference in differences) output.WriteLine(difference);
        Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences));
    }

    private static string[] Describe(string[] setup, string[] queries, object? parameter, Func<string, string[], string[], object?, string[]> run)
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "yesno-compare-");
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
        // The engine takes a Null as null; DBNull is ADO's spelling of it.
        Dictionary<string, object?>? parameters = parameter is null ? null : new() { ["p"] = parameter is DBNull ? null : parameter };
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
