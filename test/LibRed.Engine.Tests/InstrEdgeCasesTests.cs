using LibRed;
using LibRed.Engine;
using Xunit;

namespace LibRed.Engine.Tests;

// InStr([start,] string1, string2 [, compare]) — the documented edge cases, each verified byte-identical to ACE:
// case-insensitive default, not-found → 0, empty/null args, start beyond length, and the compare modes.
public class InstrEdgeCasesTests : TempDatabaseTest
{
    private static QueryEngine Fresh()
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "instr-");
        var e = new QueryEngine(TemporaryDatabase.OpenTracked(path, readOnly: false));
        e.ExecuteNonQuery("CREATE TABLE T ( K LONG PRIMARY KEY )");
        e.ExecuteNonQuery("INSERT INTO T (K) VALUES (1)");
        return e;
    }

    private static object? Eval(string expr) => Fresh().ExecuteQuery($"SELECT {expr} FROM T").Rows.Single()[0];

    [Theory]
    [InlineData("InStr('abc', 'b')", 2)]
    [InlineData("InStr('ABC', 'b')", 2)]           // case-insensitive by default
    [InlineData("InStr('abc', 'x')", 0)]           // not found
    [InlineData("InStr('', 'a')", 0)]              // string1 zero-length
    [InlineData("InStr('abc', '')", 1)]            // string2 zero-length → start (default 1)
    [InlineData("InStr(2, 'abc', '')", 2)]         // string2 zero-length → start
    [InlineData("InStr(10, 'abc', 'a')", 0)]       // start > len(string1)
    [InlineData("InStr(2, 'abcabc', 'a')", 4)]     // from a start position
    [InlineData("InStr(1, 'aXbXc', 'x', 0)", 0)]   // binary (case-sensitive)
    [InlineData("InStr(1, 'aXbXc', 'x', 1)", 2)]   // textual (case-insensitive)
    public void Instr_matches_ace(string expr, int expected)
        => Assert.Equal(expected, Convert.ToInt32(Eval(expr)));

    // InStr, InStrRev and Replace do not compare in the collation (each verified vs ACE, in a General database):
    // trailing spaces, hyphens, accents and width count, case folds by ACE's own table, and the letters the locale
    // ID spells out match letter by letter — inside one too for InStr and InStrRev, whole for Replace.
    [Theory]
    [InlineData("InStr('ab ', 'ab  ')", "0")]
    [InlineData("InStr('ab  ', 'ab ')", "1")]
    [InlineData("InStr('co-op', 'coop')", "0")]
    [InlineData("InStr('é', 'e')", "0")]
    [InlineData("InStr('Ａ', 'A')", "0")]
    [InlineData("InStr('σ', 'Σ')", "1")]
    [InlineData("InStr('ς', 'Σ')", "0")]            // final sigma is not in ACE's case table
    [InlineData("InStr('ß', 'ss')", "1")]
    [InlineData("InStr('ß', 's')", "1")]            // found inside the spelt-out letter
    [InlineData("InStr('aßb', 'sb')", "2")]         // at the letter the match begins in
    [InlineData("InStr(2, 'ßß', 'sss')", "0")]
    [InlineData("InStr('asssb', 'ß')", "2")]
    [InlineData("InStr('aæe', 'ee')", "2")]
    [InlineData("InStr(1, 'ß', 'ss', 0)", "0")]
    [InlineData("InStr(1, 'ß', 'ss', 1033)", "1")]
    [InlineData("InStr(1, 'ß', 'ss', 1041)", "0")]  // Japanese spells nothing out
    [InlineData("InStr(1, 'ß', 'ss', 66567)", "0")] // nor does German phone book, 0x10407
    [InlineData("InStr(1, 'ß', 'ss', 3079)", "0")]  // Austrian German spells ß as sz
    [InlineData("InStr(1, 'ß', 'sz', 3079)", "1")]
    [InlineData("InStr(1, 'æ', 'ae', 1030)", "0")]  // Danish keeps æ and þ, and spells out ß and œ
    [InlineData("InStr(1, 'œ', 'oe', 1030)", "1")]
    [InlineData("InStr(1, 'þ', 'th', 1055)", "0")]  // Turkish keeps þ alone
    [InlineData("InStr(1, 'æ', 'ae', 1029)", "0")]  // Czech spells out ß alone
    [InlineData("InStr(1, 'ß', 'ss', 1029)", "1")]
    [InlineData("InStr(1, 'ß', 'ss', 1034)", "1")]  // Spanish, traditional sort
    [InlineData("InStr(1, 'ß', 'ss', 4103)", "0")]  // Luxembourg German spells out in InStrRev and Replace alone
    [InlineData("InStrRev('ß', 'ss', -1, 4103)", "1")]
    [InlineData("Replace('ß', 'ss', 'x', 1, -1, 5127)", "x")]
    [InlineData("InStr(1, 'ß', 'ss', 1024)", "0")]  // the user-default ID: a locale of no letters to InStr
    [InlineData("InStr(1, 'a', 'A', 8192)", "1")]   // a locale ID Windows takes and the runtime does not list
    [InlineData("InStr(1, '', 'a', 2)", "0")]       // a compare argument that is not one is refused only once
    [InlineData("InStr(1, 'a', '', 2)", "1")]       // there is something to search
    [InlineData("InStrRev('ab ', 'ab  ')", "0")]
    [InlineData("InStrRev('ßß', 's')", "2")]
    [InlineData("InStrRev('ßß', 'sss')", "1")]
    [InlineData("InStrRev('aßb', 'sb', 2)", "0")]
    [InlineData("InStrRev('aßb', 'as', 2)", "1")]
    [InlineData("InStrRev('ssss', 'ß', 3)", "2")]
    [InlineData("InStrRev('a', 'A', -1, 2)", "1")]  // 2 is a locale ID to InStrRev and Replace
    [InlineData("InStrRev('ß', 'ss', -1, 2)", "0")]
    [InlineData("Replace('ab ', 'ab  ', 'x')", "ab ")]
    [InlineData("Replace('co-op', 'coop', 'x')", "co-op")]
    [InlineData("Replace('ß', 'ss', 'x')", "x")]
    [InlineData("Replace('ß', 's', 'x')", "ß")]     // whole characters only
    [InlineData("Replace('aßb', 'sb', 'x')", "aßb")]
    [InlineData("Replace('sßs', 'sss', 'x')", "xs")]
    [InlineData("Replace('sss', 'ß', 'x')", "xs")]
    [InlineData("Replace('æß', 'sz', 'x', 1, -1, 3079)", "æx")]
    [InlineData("Replace('a', 'A', 'x', 1, -1, 2)", "x")]
    public void Text_is_searched_as_ace_searches_it(string expr, string expected)
        => Assert.Equal(expected, Convert.ToString(Eval(expr), System.Globalization.CultureInfo.InvariantCulture));

    [Theory]
    [InlineData("InStr(1, 'a', 'A', 2)")]           // the documented database comparison
    [InlineData("InStr(5, 'a', 'a', 2)")]           // refused before the start past the end answers 0
    [InlineData("InStr(1, 'a', 'A', -1)")]
    [InlineData("InStrRev('a', 'a', 5, -1)")]       // InStrRev and Replace refuse before anything is answered
    [InlineData("InStrRev('', 'a', -1, -1)")]
    [InlineData("Replace('', 'a', 'b', 1, -1, -1)")]
    public void A_compare_argument_that_is_not_one_is_an_invalid_procedure_call(string expr)
        => Assert.Throws<ArgumentException>(() => Eval(expr));

    // A compare argument of 1 is the user's locale (verified vs ACE with the user's locale Turkish, which spells
    // out ß and not þ); a database in another collation compares under its own when the argument is left out.
    [Fact]
    public void Compare_1_is_the_users_locale_and_an_absent_one_the_databases()
    {
        var before = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
            Assert.Equal(1, Convert.ToInt32(Eval("InStr(1, 'ß', 'ss', 1)")));
            Assert.Equal(0, Convert.ToInt32(Eval("InStr(1, 'þ', 'th', 1)")));
            Assert.Equal(0, Convert.ToInt32(Eval("InStrRev('þ', 'th', -1, 1)")));
            Assert.Equal("þ", Eval("Replace('þ', 'th', 'x', 1, -1, 1)"));
            Assert.Equal(1, Convert.ToInt32(Eval("InStr('þ', 'th')")));   // the General database's, not the user's
            // 1024 is the user's locale to InStrRev and Replace.
            Assert.Equal(0, Convert.ToInt32(Eval("InStrRev('þ', 'th', -1, 1024)")));
            Assert.Equal(1, Convert.ToInt32(Eval("InStrRev('ß', 'ss', -1, 1024)")));
            Assert.Equal("þ", Eval("Replace('þ', 'th', 'x', 1, -1, 1024)"));

            // A database whose own locale ID is 1 searches under the user's too.
            QueryEngine one = In(LibRed.Catalog.CollatingOrder.ArabicNeutral);
            Assert.Equal(1, Convert.ToInt32(one.ExecuteQuery("SELECT InStr('ß', 'ss')").Rows.Single()[0]));
            Assert.Equal(0, Convert.ToInt32(one.ExecuteQuery("SELECT InStr('þ', 'th')").Rows.Single()[0]));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = before; }

        QueryEngine japanese = In(LibRed.Catalog.CollatingOrder.Japanese);
        Assert.Equal(0, Convert.ToInt32(japanese.ExecuteQuery("SELECT InStr('ß', 'ss')").Rows.Single()[0]));
        Assert.Equal(0, Convert.ToInt32(japanese.ExecuteQuery("SELECT InStrRev('ß', 'ss')").Rows.Single()[0]));
        Assert.Equal("ß", japanese.ExecuteQuery("SELECT Replace('ß', 'ss', 'x')").Rows.Single()[0]);
        Assert.Equal(1, Convert.ToInt32(japanese.ExecuteQuery("SELECT InStr(1, 'ß', 'ss', 1033)").Rows.Single()[0]));

        // In a database whose locale ID is 2, InStr with no compare argument is refused as InStr(…, 2) is — once
        // there is something to search — and InStrRev and Replace search under 2, spelling nothing out.
        QueryEngine two = In(LibRed.Catalog.CollatingOrder.BulgarianNeutral);
        Assert.Throws<ArgumentException>(() => two.ExecuteQuery("SELECT InStr('a', 'A')").Rows.Single());
        Assert.Equal(0, Convert.ToInt32(two.ExecuteQuery("SELECT InStr('', 'a')").Rows.Single()[0]));
        Assert.Equal(1, Convert.ToInt32(two.ExecuteQuery("SELECT InStrRev('a', 'A')").Rows.Single()[0]));
        Assert.Equal(0, Convert.ToInt32(two.ExecuteQuery("SELECT InStrRev('ß', 'ss')").Rows.Single()[0]));
        Assert.Equal("x", two.ExecuteQuery("SELECT Replace('a', 'A', 'x')").Rows.Single()[0]);

        static QueryEngine In(LibRed.Catalog.CollatingOrder order)
        {
            string path = TemporaryDatabase.CreatePath("instr-locale-");
            JetDatabase.Create(path, collation: new LibRed.Catalog.Collation(order, 0));
            return new QueryEngine(TemporaryDatabase.OpenTracked(path));
        }
    }

    [Theory]
    [InlineData("InStr(Null, 'a')")]               // string1 null → null
    [InlineData("InStr('abc', Null)")]             // string2 null → null
    [InlineData("InStr(0, Null, 'a', 1)")]         // a Null string wins over a bad start (verified vs ACE)
    [InlineData("InStr(1, '', 'a', Null)")]        // compare null → null, even with nothing to search
    [InlineData("InStr(1, 'abc', 'b', Null)")]     // compare null → null
    public void Instr_propagates_null(string expr)
        => Assert.Null(Eval(expr));
}
