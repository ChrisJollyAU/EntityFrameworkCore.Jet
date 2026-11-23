using System.Data.OleDb;
using System.Globalization;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// The expressions a calculated column is held to (LibRed.Core.AccessTests' CalculatedColumnAccessTests), asked as a
/// query instead: the same function and operator, evaluated by the engine over each row of table M. Each query runs
/// through ACE over OLE DB and through LibRed: every value must agree, and a query's column types too where the query is
/// asked whole; a refusal matches a refusal. A VBA Boolean (IsNull, IsEmpty, CBool, Like ...) is written out as "True"/"False" where a Variant becomes
/// text, and as "-1"/"0" where a String parameter takes it; a whole number past a Long's range is a Double, and stored
/// into a plain Long result it overflows.
/// </summary>
/// <remarks>ACE writes a date out in the Windows user locale, so LibRed runs under that culture here; the rest of the
/// suite pins en-US.</remarks>
[Collection(AceCollection.Name)]
public class CalculatedExpressionQueryAccessTests(ITestOutputHelper output) : TempDatabaseTest
{
    private static readonly string[] Setup =
    [
        "CREATE TABLE M (Id LONG PRIMARY KEY, Qty LONG, Price CURRENCY, A TEXT(20), YN YESNO)",
        "INSERT INTO M VALUES (1, 7, 12.5, 'hello', True)",
        "INSERT INTO M VALUES (2, 0, 0, '', False)",
        "INSERT INTO M VALUES (3, -5, -3.25, '  pad  ', False)",
        "INSERT INTO M VALUES (4, NULL, NULL, NULL, False)",
        "INSERT INTO M VALUES (5, 3, 0.0001, 'ß', True)",
        "CREATE TABLE O (Id LONG PRIMARY KEY, Qty LONG, I SHORT)",
        "INSERT INTO O VALUES (1, 7, 200)",
        "CREATE TABLE G (Id LONG PRIMARY KEY, A TEXT(20))",
        "INSERT INTO G VALUES (1, 'a')",
        "INSERT INTO G VALUES (2, NULL)",
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
        "CStr(-([Id]-2)^2)", "Format(-[Qty]^2)",
        // The type of whole-number arithmetic, and a Long past its range becoming a Double.
        "VarType(1+1)", "VarType(2*3+1)", "VarType(IIf(IsNull([A]), 0, 1))", "([Qty] > 1)", "True", "[Qty]*1000000000",
        "VarType([Qty]*1000000000)", "VarType([Qty]*2)", "[Qty]*2147483647", "2147483647*2", "2147483647+[Qty]",
        "VarType(2147483647+[Qty])", "-2147483647-[Qty]", "32767*[Qty]",
        // CInt and CLng round a Decimal through a Double; Currency arithmetic keeps a Currency.
        "CInt(0.5000000000000000000000000001+[Id]*0)", "CLng(2.5000000000000000000000000001+[Id]*0)", "[Price] + [Qty]/32",
        "[Price]/3", "[Price]*[Price]*10000", "[Price]*2", "[Price]+1.00005", "Round([Price],2) + [Qty]/32",
        "[Price] = Left('1.00005',7)", "[Price]*1.11", "([Qty]/32)*1.11",
        // A VBA Boolean: written out as text, taken by a String parameter, and counted as a number.
        "Str(IsNull([A]))", "Len(IsNull([A]))", "IIf([Qty]>0, IsNull([A]), IsNull([Price]))", "IsNull([A]) + 'x'",
        "IsNull([A]) = 'True'", "IsNull([A]) = '-1'", "IsNull([A]) > ''", "IsNull([A]) = 'False'", "IsNull([A]) * 2",
        "Round(IsNull([A]))", "Not IsNull([A])", "VarType(IsNull([A]))", "UCase(IsNull([A]))", "LCase(IsNull([A]))",
        "Trim(IsNull([A]))", "Left(IsNull([A]),2)", "Right(IsNull([A]),2)", "Mid(IsNull([A]),2,2)", "InStr(1,IsNull([A]),'e')",
        "InStr(1,'x-1Truex',IsNull([A]))", "Asc(IsNull([A]))", "String(2,IsNull([A]))", "IsNull([A]) & IsNull([A])",
        "IsNull([A])+1", "IsNull([A])-1", "IsNull([A])/2", "IsNull([A])\\1", "IsNull([A]) Mod 2", "IsNull([A])^2",
        "-IsNull([A])", "IsNull([A])+IsNull([A])", "IsNull([A]) + '1'", "Abs(IsNull([A]))", "Sgn(IsNull([A]))",
        "Int(IsNull([A]))", "Fix(IsNull([A]))", "CDbl(IsNull([A]))", "#2003-09-29#+IsNull([A])", "IsNull([A]) And True",
        "IsNull([A]) Or False", "IsNull([A]) Xor True", "IsNull([A]) And 3", "IsNull([A]) = True", "IsNull([A]) = -1",
        "IsNull([A]) < 0", "IsNull([A]) = ([Id] > 3)", "IsNull([A]) = IsNull([Price])", "IsNull([A]) In (-1)",
        "IsNull([A]) In ('-1')", "IsNull([A]) Between -1 And -1", "[A] > IsNull([Price])", "IIf(IsNull([A]) Like 'T%','y','n')",
        "IIf(IsNull([A]) Like '-1','y','n')", "IIf([A] Like 'h%','y','n')", "IIf(IsNull([A]),'y','n')", "Choose(1,IsNull([A]))",
        "IIf([Qty]>0, IsNull([A]), 5)",
        // The other functions that give a VBA Boolean.
        "IsEmpty([A])", "Str(IsEmpty([A]))", "IsError([A])", "IsMissing([A])", "IsObject([A])", "CBool([Id]-2)",
        "Str(CBool([Id]-2))", "IsNumeric([A])", "IsDate([A])", "Str(IsNumeric([A]))", "[A] Like 'h%'", "Str([A] Like 'h%')",
        // A VBA Boolean converted, formatted and searched.
        "CStr(IsNull([A]))", "CInt(IsNull([A]))", "CByte(IsNull([A]))", "CSng(IsNull([A]))", "CLng(IsNull([A]))",
        "CCur(IsNull([A]))", "CDate(IsNull([A]))", "Val(IsNull([A]))", "Val(IsNull([A])) + 10", "Hex(IsNull([A]))",
        "Format(IsNull([A]))", "Format(IsNull([A]),'Yes/No')", "Format(IsNull([A]),'0.00')", "TypeName(IsNull([A]))",
        "Switch(True,IsNull([A]))", "Replace(IsNull([A]),'u','x')", "Replace('aTrueb-1c', IsNull([A]), 'x')",
        "StrReverse(IsNull([A]))", "InStrRev(IsNull([A]),'e')", "InStrRev('x-1Truex', IsNull([A]))",
        "StrComp(IsNull([A]),'True')", "StrComp(IsNull([A]),'-1')", "LTrim(IsNull([A]))", "RTrim(IsNull([A]))",
        "StrConv(IsNull([A]),1)", "AscW(IsNull([A]))", "AscB(IsNull([A]))", "LenB(IsNull([A]))", "LeftB(IsNull([A]),2)",
        "MidB(IsNull([A]),3,2)", "InStrB(1,IsNull([A]),'e')", "FormatNumber(IsNull([A]))", "FormatCurrency(IsNull([A]))",
        "FormatPercent(IsNull([A]))", "Partition(IsNull([A]),-5,5,2)", "Space(IsNull([A])+1) & '|'", "Chr(IsNull([A])+66)",
        // Like '%' is Is Not Null.
        "[A] Like '%'", "[A] Like '%%'",
        // A Yes/No column is a number, not a VBA Boolean.
        "[YN]", "Str([YN])", "Len([YN])", "UCase([YN])", "Asc([YN])", "[YN] = '-1'", "-[YN]", "IIf([YN],'y','n')",
        // Out of a subquery or a derived table.
        "(SELECT IsNull(X.A) FROM M AS X WHERE X.Id = M.Id)", "Str((SELECT IsNull(X.A) FROM M AS X WHERE X.Id = M.Id))",
        "(SELECT X.B FROM (SELECT Y.Id, IsNull(Y.A) AS B FROM M AS Y) AS X WHERE X.Id = M.Id)",
        "Str((SELECT X.B FROM (SELECT Y.Id, IsNull(Y.A) AS B FROM M AS Y) AS X WHERE X.Id = M.Id))",
        "IsNull([A]) In (SELECT X.A FROM M AS X)", "IsNull([A]) In (SELECT X.YN FROM M AS X)",
    ];

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
        "SELECT Max(IsNull(A)) & '' FROM G",
    ];

    // Each row on its own, so one row's error hides no other, its value written out as text so a Variant compares by
    // what it says. The column's type is not compared here: ACE's OLE DB reader types a Boolean Integer (Int16) and a
    // Variant by its first value, where LibRed's engine has a Boolean and an Object.
    [Theory]
    [MemberData(nameof(Expressions))]
    public void An_expression_is_valued_as_ace_values_it(string expression) =>
        Matches([.. Enumerable.Range(1, 5).Select(id => $"SELECT IIf(IsNull({expression}), 'NULL', ({expression}) & '') FROM M WHERE Id = {id}")]);

    [Theory]
    [MemberData(nameof(Queries))]
    public void A_query_is_typed_and_valued_as_ace_does(string query) => Matches([query]);

    private void Matches(string[] queries)
    {
        string path = Seeded();
        try
        {
            var differences = new List<string>();
            foreach (string query in queries)
            {
                string ace;
                using (OleDbConnection connection = AceTestDatabase.Open(path))
                using (OleDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    try
                    {
                        using OleDbDataReader reader = command.ExecuteReader();
                        var types = Enumerable.Range(0, reader.FieldCount).Select(reader.GetFieldType).ToList();
                        var rows = new List<object?[]>();
                        while (reader.Read())
                            rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? null : reader.GetValue(i))]);
                        ace = Describe(types, rows);
                    }
                    catch (OleDbException) { ace = "refused"; }
                }

                string libred = UnderUserCulture(() =>
                {
                    using var database = JetDatabase.Open(path, readOnly: true);
                    try
                    {
                        var result = new QueryEngine(database).ExecuteQuery(query);
                        return Describe(result.ColumnTypes, result.Rows);
                    }
                    catch (Exception e) when (e is InvalidOperationException or ArgumentException or InvalidCastException or FormatException
                        or OverflowException or DivideByZeroException or NotSupportedException or LibRed.Sql.Binding.SqlBindException
                        or LibRed.Sql.Parsing.SqlParseException) { return "refused"; }
                });

                if (ace != libred) differences.Add($"{query}\n    ACE    {ace}\n    LibRed {libred}");
            }
            foreach (string difference in differences) output.WriteLine(difference);
            Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    /// <summary>A copy of Northwind with tables M, O and G made and filled through ACE.</summary>
    private static string Seeded()
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "calcquery-");
        using OleDbConnection connection = AceTestDatabase.Open(path);
        foreach (string statement in Setup)
        {
            using OleDbCommand setup = connection.CreateCommand();
            setup.CommandText = statement;
            setup.ExecuteNonQuery();
        }
        return path;
    }

    /// <summary>Runs LibRed under the Windows user's own locale and formats, which ACE writes a date out in.</summary>
    private static T UnderUserCulture<T>(Func<T> run)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture =
            Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\International")?.GetValue("LocaleName") is string name
                ? new CultureInfo(name, useUserOverride: true)
                : CultureInfo.InstalledUICulture;
        try { return run(); }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    private static string Describe(IReadOnlyList<Type> types, IEnumerable<object?[]> rows) =>
        $"[{string.Join(", ", types.Select(t => t.Name))}] " + string.Join(" | ", rows.Select(row => string.Join(", ", row.Select(Value))));

    private static string Value(object? value) => value switch
    {
        null => "NULL",
        byte[] bytes => $"{Convert.ToHexString(bytes)}:Byte[]",
        DateTime date => $"{date:o}:DateTime",
        double d => $"{d.ToString("R", CultureInfo.InvariantCulture)}:Double",
        // A Decimal's scale is not compared: LibRed's Currency keeps .NET's (21.0) where ACE's does not (21).
        decimal m => $"{(m / 1.0000000000000000000000000000m).ToString(CultureInfo.InvariantCulture)}:Decimal",
        _ => $"{Convert.ToString(value, CultureInfo.InvariantCulture)}:{value.GetType().Name}",
    };
}
