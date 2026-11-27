using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// The expressions a calculated column is held to (LibRed.Core.AccessTests' CalculatedColumnAccessTests), asked as a
/// query instead: the same function and operator, evaluated by the engine over each row of table M. Only what ACE
/// accepts in a calculated column is asked here; the functions it refuses there, and what only a query can say, are
/// VbaValueQueryAccessTests'. Each query runs through ACE over OLE DB and through LibRed, and every value must agree.
/// A VBA Boolean (IsNull, IsEmpty, Like ...) is written out as "True"/"False" where a Variant becomes text, and as
/// "-1"/"0" where a String parameter takes it.
/// </summary>
[Collection(AceCollection.Name)]
public class CalculatedExpressionQueryAccessTests(ITestOutputHelper output) : AceQueryParityTest(output)
{
    internal static readonly string[] Setup =
    [
        "CREATE TABLE M (Id LONG PRIMARY KEY, Qty LONG, Price CURRENCY, A TEXT(20), YN YESNO)",
        "INSERT INTO M VALUES (1, 7, 12.5, 'hello', True)",
        "INSERT INTO M VALUES (2, 0, 0, '', False)",
        "INSERT INTO M VALUES (3, -5, -3.25, '  pad  ', False)",
        "INSERT INTO M VALUES (4, NULL, NULL, NULL, False)",
        "INSERT INTO M VALUES (5, 3, 0.0001, 'ß', True)",
    ];

    public static TheoryData<string> Expressions =>
    [
        // Operators over a column, and their refusals.
        "[Qty] And False", "[Qty] Or True", "([Id]<0) And (CDbl([Qty])>0)", "[Qty] And 2", "Not [Qty]", "[Qty] <> Null",
        "[A] & [A]", "IsNull([A] & [A])", "IsNull([Qty])", "[Qty]/0", "[A] * [Qty]", "'abc' * [Qty]", "-[A]", "[A] > 1",
        "[A] <> False", "[A] = [Qty]+0", "[A] > - 1", "[A] > -1", "[A] In ([Qty]+0)", "String(3,65)", "Choose(1.6,'a','b')",
        "Choose([Qty],'a','b')", "String(3,[Qty])", "[Qty] = True",
        // A sign and ^: a minus written against a digit is the number's, ^ is left-associative.
        "-2^2", "-[Qty]^2", "Str(-[Qty]^2)", "2^3^2", "2^-2^2", "- 2^2", "-.5^2", "2^+3", "2^+3^2", "2^-[Qty]^2",
        // Whole-number arithmetic, and a Long past its range becoming a Double.
        "([Qty] > 1)", "True", "[Qty]*1000000000", "[Qty]*2147483647", "2147483647*2", "2147483647+[Qty]",
        "-2147483647-[Qty]", "32767*[Qty]",
        // Currency arithmetic keeps a Currency.
        "[Price] + [Qty]/32", "[Price]/3", "[Price]*[Price]*10000", "[Price]*2", "[Price]+1.00005",
        "Round([Price],2) + [Qty]/32", "[Price] = Left('1.00005',7)", "[Price]*1.11", "([Qty]/32)*1.11",
        // A VBA Boolean: written out as text, taken by a String parameter, and counted as a number.
        "Str(IsNull([A]))", "Len(IsNull([A]))", "IIf([Qty]>0, IsNull([A]), IsNull([Price]))", "IsNull([A]) + 'x'",
        "IsNull([A]) = 'True'", "IsNull([A]) = '-1'", "IsNull([A]) > ''", "IsNull([A]) = 'False'", "IsNull([A]) * 2",
        "Round(IsNull([A]))", "Not IsNull([A])", "UCase(IsNull([A]))", "LCase(IsNull([A]))",
        "Trim(IsNull([A]))", "Left(IsNull([A]),2)", "Right(IsNull([A]),2)", "Mid(IsNull([A]),2,2)", "InStr(1,IsNull([A]),'e')",
        "InStr(1,'x-1Truex',IsNull([A]))", "Asc(IsNull([A]))", "String(2,IsNull([A]))", "IsNull([A]) & IsNull([A])",
        "IsNull([A])+1", "IsNull([A])-1", "IsNull([A])/2", "IsNull([A])\\1", "IsNull([A]) Mod 2", "IsNull([A])^2",
        "-IsNull([A])", "IsNull([A])+IsNull([A])", "IsNull([A]) + '1'", "Abs(IsNull([A]))", "Sgn(IsNull([A]))",
        "Int(IsNull([A]))", "Fix(IsNull([A]))", "CDbl(IsNull([A]))", "#2003-09-29#+IsNull([A])", "IsNull([A]) And True",
        "IsNull([A]) Or False", "IsNull([A]) Xor True", "IsNull([A]) And 3", "IsNull([A]) = True", "IsNull([A]) = -1",
        "IsNull([A]) < 0", "IsNull([A]) = ([Id] > 3)", "IsNull([A]) = IsNull([Price])", "IsNull([A]) In (-1)",
        "IsNull([A]) In ('-1')", "IsNull([A]) Between -1 And -1", "[A] > IsNull([Price])", "IIf(IsNull([A]) Like 'T%','y','n')",
        "IIf(IsNull([A]) Like '-1','y','n')", "IIf([A] Like 'h%','y','n')", "IIf(IsNull([A]),'y','n')", "Choose(1,IsNull([A]))",
        "IIf([Qty]>0, IsNull([A]), 5)", "Space(IsNull([A])+1) & '|'",
        // The other VBA Booleans a calculated column can give.
        "IsEmpty([A])", "Str(IsEmpty([A]))", "[A] Like 'h%'", "Str([A] Like 'h%')",
        // Like '%' is Is Not Null.
        "[A] Like '%'", "[A] Like '%%'",
        // A Yes/No column is a number, not a VBA Boolean.
        "[YN]", "Str([YN])", "Len([YN])", "UCase([YN])", "Asc([YN])", "[YN] = '-1'", "-[YN]", "IIf([YN],'y','n')",
        // Case by ACE's own table (VbaValueQueryAccessTests asks the letters ACE leaves as they are).
        "UCase([A])", "LCase([A])",
    ];

    // Each row on its own, so one row's error hides no other, its value written out as text so a Variant compares by
    // what it says. The column's type is not compared here: ACE's OLE DB reader types a Boolean Integer (Int16) and a
    // Variant by its first value, where LibRed's engine has a Boolean and an Object.
    [Theory]
    [MemberData(nameof(Expressions))]
    public void An_expression_is_valued_as_ace_values_it(string expression) =>
        Matches("calcquery-", Setup, EachRow(expression));

    /// <summary><paramref name="expression"/> asked of each row of table M on its own, written out as text.</summary>
    internal static string[] EachRow(string expression) =>
        [.. Enumerable.Range(1, 5).Select(id => $"SELECT IIf(IsNull({expression}), 'NULL', ({expression}) & '') FROM M WHERE Id = {id}")];
}
