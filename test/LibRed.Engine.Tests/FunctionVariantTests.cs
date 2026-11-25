using LibRed;
using LibRed.Engine;
using Xunit;

namespace LibRed.Engine.Tests;

// VBA string-function variants, verified against ACE's expression service:
//   $ = String-returning alias (same value; the grammar allows a trailing '$', the evaluator strips it)
//   B = byte-based (UTF-16, 2 bytes/char): LenB('abc')=6, InStrB(1,'abc','b')=3, LeftB('abc',2)='a'
//   W = wide/Unicode code point: ChrW(233)='é'
// Also covers base Asc(), which needed a grammar fix (ASC is a reserved keyword).
public class FunctionVariantTests : TempDatabaseTest
{
    private static QueryEngine Fresh()
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "fv-");
        var e = new QueryEngine(TemporaryDatabase.OpenTracked(path, readOnly: false));
        e.ExecuteNonQuery("CREATE TABLE T ( K LONG PRIMARY KEY, Q LONG, A TEXT(20) )");
        e.ExecuteNonQuery("INSERT INTO T (K) VALUES (1)");
        return e;
    }

    private static string Eval(string expr) => Fresh().ExecuteQuery($"SELECT {expr} FROM T").Rows.Single()[0]?.ToString()!;

    [Theory]
    // base / grammar-unblocked
    [InlineData("Asc('A')", "65")]
    // byte variants
    [InlineData("AscB('A')", "65")]
    [InlineData("LenB('abc')", "6")]
    [InlineData("LeftB('abc', 2)", "a")]
    [InlineData("RightB('abc', 2)", "c")]
    [InlineData("MidB('abc', 1, 2)", "a")]
    [InlineData("InStrB(1, 'abc', 'b')", "3")]
    // wide variants
    [InlineData("AscW('A')", "65")]
    [InlineData("ChrW(65)", "A")]
    [InlineData("ChrW(233)", "é")]
    // $ variants (string-returning aliases)
    [InlineData("Chr$(65)", "A")]
    [InlineData("UCase$('ab')", "AB")]
    [InlineData("Left$('abc', 2)", "ab")]
    [InlineData("Mid$('abc', 2)", "bc")]
    [InlineData("Space$(3)", "   ")]
    [InlineData("String$(3, 'x')", "xxx")]
    [InlineData("Hex$(255)", "FF")]
    public void Variant_matches_ace(string expr, string expected)
        => Assert.Equal(expected, Eval(expr));

    // A '$' form returns a String, which cannot hold Null: over a Null column, where the plain name gives Null, ACE
    // raises "Data type mismatch" for the text functions and "Invalid use of Null" for Hex$, Oct$, Str$ and String$
    // (measured against ACE 2010 and Microsoft 365 alike).
    [Theory]
    [InlineData("LCase$(A)")]
    [InlineData("UCase$(A)")]
    [InlineData("Left$(A, 2)")]
    [InlineData("Right$(A, 2)")]
    [InlineData("Mid$(A, 2, 2)")]
    [InlineData("Trim$(A)")]
    [InlineData("LTrim$(A)")]
    [InlineData("RTrim$(A)")]
    [InlineData("LeftB$(A, 2)")]
    [InlineData("RightB$(A, 2)")]
    [InlineData("MidB$(A, 2, 2)")]
    public void A_text_dollar_variant_over_null_is_a_type_mismatch(string expr)
        => Assert.Throws<InvalidCastException>(() => Eval(expr));

    [Theory]
    [InlineData("Hex$(Q)")]
    [InlineData("Oct$(Q)")]
    [InlineData("Str$(Q)")]
    [InlineData("String$(3, A)")]
    public void A_vba_dollar_variant_over_null_is_an_invalid_use_of_null(string expr)
        => Assert.Contains("Invalid use of Null", Assert.Throws<InvalidOperationException>(() => Eval(expr)).Message);

    [Theory]
    [InlineData("LCase(A)")]
    [InlineData("Left(A, 2)")]
    [InlineData("Hex(Q)")]
    [InlineData("Str(Q)")]
    [InlineData("String(3, A)")]
    public void The_plain_name_over_null_is_null(string expr)
        => Assert.Null(Fresh().ExecuteQuery($"SELECT {expr} FROM T").Rows.Single()[0]);

    // Format$ gives "" over Null, as Format does.
    [Fact]
    public void Format_dollar_over_null_is_empty()
        => Assert.Equal("", Eval("Format$(A)"));

    // Date$ and Time$ are text in a fixed form, whatever the locale.
    [Fact]
    public void Date_and_time_dollar_are_text()
    {
        Assert.Matches(@"^\d\d-\d\d-\d\d\d\d$", Eval("Date$()"));
        Assert.Matches(@"^\d\d:\d\d:\d\d$", Eval("Time$()"));
    }

    // The '$' grammar tweak must not disturb ORDER BY ... ASC (ASC is now also a function name, but only when
    // followed by '(').
    [Fact]
    public void Order_by_asc_still_parses()
    {
        var e = Fresh();
        e.ExecuteNonQuery("INSERT INTO T (K) VALUES (2)");
        var ks = e.ExecuteQuery("SELECT K FROM T ORDER BY K ASC").Rows.Select(r => Convert.ToInt32(r[0])).ToArray();
        Assert.Equal(new[] { 1, 2 }, ks);
    }
}
