using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// The values a query's expression gives and the types its columns declare, where a calculated column cannot follow:
/// the functions ACE refuses in a calculated column, a subquery and a derived table, aggregates, grouping and ordering.
/// Each query runs through ACE over OLE DB and through LibRed: every value must agree, and a query's column types too
/// where the query is asked whole; a refusal matches a refusal. A VBA Boolean (IsNull, CBool, IsNumeric ...) is
/// written out as "True"/"False" where a Variant becomes text, and as "-1"/"0" where a String parameter takes it; a
/// whole number past a Long's range is a Double, and stored into a plain Long result it overflows.
/// </summary>
[Collection(AceCollection.Name)]
public class VbaValueQueryAccessTests(ITestOutputHelper output) : AceQueryParityTest(output)
{
    private static readonly string[] Setup =
    [
        .. CalculatedExpressionQueryAccessTests.Setup,
        "CREATE TABLE O (Id LONG PRIMARY KEY, Qty LONG, I SHORT)",
        "INSERT INTO O VALUES (1, 7, 200)",
        "CREATE TABLE G (Id LONG PRIMARY KEY, A TEXT(20))",
        "INSERT INTO G VALUES (1, 'a')",
        "INSERT INTO G VALUES (2, NULL)",
    ];

    public static TheoryData<string> Expressions =>
    [
        // A sign and ^, converted and formatted.
        "CStr(-([Id]-2)^2)", "Format(-[Qty]^2)",
        // The type of whole-number arithmetic, and a Long past its range becoming a Double.
        "VarType(1+1)", "VarType(2*3+1)", "VarType(IIf(IsNull([A]), 0, 1))", "VarType([Qty]*1000000000)", "VarType([Qty]*2)",
        "VarType(2147483647+[Qty])",
        // CInt and CLng round a Decimal through a Double.
        "CInt(0.5000000000000000000000000001+[Id]*0)", "CLng(2.5000000000000000000000000001+[Id]*0)",
        // The VBA Boolean's own type, and the other functions that give one.
        "VarType(IsNull([A]))", "IsError([A])", "IsMissing([A])", "IsObject([A])", "CBool([Id]-2)", "Str(CBool([Id]-2))",
        "IsNumeric([A])", "IsDate([A])", "Str(IsNumeric([A]))",
        // A VBA Boolean converted, formatted and searched.
        "CStr(IsNull([A]))", "CInt(IsNull([A]))", "CByte(IsNull([A]))", "CSng(IsNull([A]))", "CLng(IsNull([A]))",
        "CCur(IsNull([A]))", "CDate(IsNull([A]))", "Val(IsNull([A]))", "Val(IsNull([A])) + 10", "Hex(IsNull([A]))",
        "Format(IsNull([A]))", "Format(IsNull([A]),'Yes/No')", "Format(IsNull([A]),'0.00')", "TypeName(IsNull([A]))",
        "Switch(True,IsNull([A]))", "Replace(IsNull([A]),'u','x')", "Replace('aTrueb-1c', IsNull([A]), 'x')",
        "StrReverse(IsNull([A]))", "InStrRev(IsNull([A]),'e')", "InStrRev('x-1Truex', IsNull([A]))",
        "StrComp(IsNull([A]),'True')", "StrComp(IsNull([A]),'-1')", "LTrim(IsNull([A]))", "RTrim(IsNull([A]))",
        "StrConv(IsNull([A]),1)", "AscW(IsNull([A]))", "AscB(IsNull([A]))", "LenB(IsNull([A]))", "LeftB(IsNull([A]),2)",
        "MidB(IsNull([A]),3,2)", "InStrB(1,IsNull([A]),'e')", "FormatNumber(IsNull([A]))", "FormatCurrency(IsNull([A]))",
        "FormatPercent(IsNull([A]))", "Partition(IsNull([A]),-5,5,2)", "Chr(IsNull([A])+66)",
        // Digits other than ASCII's — Arabic-Indic three (U+0663), full-width five (U+FF15) — as a number and a date.
        "Val(ChrW(1635))", "Val('1' & ChrW(1635))", "Val(ChrW(65301))", "Val('2' & ChrW(65301) & '7')", "CDbl(ChrW(1635))",
        "IsNumeric(ChrW(65301))", "IsDate(ChrW(65297) & '/' & ChrW(65298) & '/2024')", "CDate(ChrW(65297) & '/2/2024')",
        "IsDate('1/2/' & ChrW(1634) & '024')",
        // Val skips the ideographic space as it skips a space, and no other: a no-break space and an em space stop it.
        "Val(ChrW(12288) & '12')", "Val('1' & ChrW(12288) & '2')", "Val(ChrW(160) & '12')", "Val(ChrW(8195) & '12')",
        // A function's name is matched by ACE's own case table, the same on every platform: a long s is no s and a
        // dotless i no i, so these are no functions.
        "Abſ(-3)", "Mıd('abc', 2)", "aBS(-3)",
        // Case by ACE's own table, which leaves Cherokee, dotless and dotted i, ß, µ, ǅ and final sigma as they are.
        $"UCase({CaseSample})", $"LCase({CaseSample})",
        .. ((string[])["StrConv({0}, 1)", "StrConv({0}, 2)", "StrConv({0}, 3)", "Format({0}, '<')", "Format({0}, '>')"])
            .SelectMany(f => (string[])[string.Format(System.Globalization.CultureInfo.InvariantCulture, f, "[A]"),
                string.Format(System.Globalization.CultureInfo.InvariantCulture, f, CaseSample)]),
        // Out of a subquery or a derived table.
        "(SELECT IsNull(X.A) FROM M AS X WHERE X.Id = M.Id)", "Str((SELECT IsNull(X.A) FROM M AS X WHERE X.Id = M.Id))",
        "(SELECT X.B FROM (SELECT Y.Id, IsNull(Y.A) AS B FROM M AS Y) AS X WHERE X.Id = M.Id)",
        "Str((SELECT X.B FROM (SELECT Y.Id, IsNull(Y.A) AS B FROM M AS Y) AS X WHERE X.Id = M.Id))",
        "IsNull([A]) In (SELECT X.A FROM M AS X)", "IsNull([A]) In (SELECT X.YN FROM M AS X)",
    ];

    /// <summary>Cherokee Ꭰ and ꭰ, ı, İ, ß, µ, ǅ and ς, then a space, ÿ and Ÿ, and letters ACE does case. ChrW is refused
    /// in a calculated column, so these are asked here.</summary>
    private const string CaseSample = "(ChrW(5024) & ChrW(43888) & ChrW(305) & ChrW(304) & ChrW(223) & ChrW(181) & ChrW(453) "
        + "& ChrW(962) & ' ' & ChrW(255) & ChrW(376) & 'aZ xY')";

    public static TheoryData<string> Queries =>
    [
        // Through an aggregate.
        "SELECT Sum(IsNull([A])) & '' FROM M", "SELECT Max(IsNull([A])) & '' FROM M", "SELECT Min(IsNull([A])) & '' FROM M",
        "SELECT First(IsNull([A])) & '' FROM M", "SELECT Avg(IsNull([A])) & '' FROM M",
        // Whole-number arithmetic past a Long's range: a Double as a value and as text, an overflow stored into a plain
        // Long result. (2147483647*2 alone is not read as a value: OLE DB cannot hand back the column ACE gives it,
        // "Unknown OLEDB data type: 0x0005", so its VarType and text stand for it.)
        "SELECT [Qty]*1000000000 FROM O", "SELECT 2147483647+[Qty] FROM O", "SELECT -2147483647-[Qty] FROM O", "SELECT [I]*[I] FROM O",
        "SELECT CInt(200)*CInt(200) FROM O", "SELECT [I]+32767 FROM O", "SELECT [Qty]*[Qty] FROM O",
        "SELECT VarType([Qty]*1000000000), ([Qty]*1000000000) & '' FROM O", "SELECT VarType(2147483647*2), (2147483647*2) & '' FROM O",
        "SELECT VarType(2147483647+[Qty]), (2147483647+[Qty]) & '' FROM O", "SELECT VarType(-2147483647-[Qty]), (-2147483647-[Qty]) & '' FROM O",
        "SELECT VarType([I]*[I]), ([I]*[I]) & '' FROM O", "SELECT VarType(CInt(200)*CInt(200)), (CInt(200)*CInt(200)) & '' FROM O",
        "SELECT VarType([I]+32767), ([I]+32767) & '' FROM O", "SELECT VarType([Qty]*[Qty]), ([Qty]*[Qty]) & '' FROM O",
        "SELECT IIf([Qty]>0, [Qty]*1000000000, 0) FROM O",
        // CByte gives an Integer, inside an aggregate and arithmetic too. (Its negation and sum are not asked: whole-number
        // arithmetic takes C#'s types, an Integer a Long — ExpressionFunctions.Arithmetic and Negate.)
        "SELECT VarType(CByte([Qty])), TypeName(CByte([Qty])) FROM O", "SELECT VarType(Max(CByte([Qty]))), Max(CByte([Qty])) & '' FROM O",
        "SELECT VarType(CByte([Qty]) * 1.5), (CByte([Qty]) * 1.5) & '' FROM O",
        // StrConv gives text.
        "SELECT StrConv([A], 1), StrConv([A], 3) FROM G ORDER BY Id",
        // A VBA Boolean as a grouping key and an ordering key is the plain number.
        "SELECT IsNull(A) & '' FROM G GROUP BY IsNull(A) ORDER BY IsNull(A)",
        "SELECT IsNull(A) & '', Id FROM G ORDER BY IsNull(A), Id",
        // An aggregate's name too: ſum is no Sum.
        "SELECT ſum(Qty) FROM O",
        "SELECT Max(IsNull(A)) & '' FROM G",
    ];

    [Theory]
    [MemberData(nameof(Expressions))]
    public void An_expression_is_valued_as_ace_values_it(string expression) =>
        Matches("vbaquery-", Setup, CalculatedExpressionQueryAccessTests.EachRow(expression));

    [Theory]
    [MemberData(nameof(Queries))]
    public void A_query_is_typed_and_valued_as_ace_does(string query) => Matches("vbaquery-", Setup, [query]);
}
