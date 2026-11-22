using System.Data.OleDb;
using System.Globalization;
using LibRed.Catalog;
using Xunit;

namespace LibRed.Engine.Tests;

/// <summary>
/// InStr, InStrRev and Replace search text as ACE searches it: not in the database's collation but by ACE's own
/// case table, with the letters their locale ID spells out — the database's when the compare argument is left out,
/// the user's for 1.
/// </summary>
[Collection(AceCollection.Name)]
public class TextSearchAccessTests
{
    private static readonly string[] Expressions =
    [
        // trailing spaces, hyphens, apostrophes, accents, width, kana, ignorable characters: all count
        "InStr(0,Null,'a',1)", "InStr('ab ','ab  ')", "InStr('ab','ab ')", "InStr('ab  ','ab ')", "InStr(1,'ab ','ab  ',1)", "InStr(1,'ab ','ab  ',0)",
        "InStr('co-op','coop')", "InStr('coop','co-op')", "InStr('co-op','O-O')", "InStr('O''Brien','OB')",
        "InStr('é','e')", "InStr('e','é')", "InStr('e' & ChrW(769),'é')", "InStr('abÉ','é')",
        "InStr('Ａ','A')", "InStr('ｶ','カ')", "InStr('カ','か')", "InStr('²','2')",
        "InStr('a' & ChrW(173) & 'b','ab')", "InStr('a' & ChrW(1) & 'b','ab')", "InStr('a' & ChrW(8203) & 'b','ab')",
        // case, by ACE's table rather than a runtime's
        "InStr('σ','Σ')", "InStr('ς','Σ')", "InStr('ı','I')", "InStr('i','İ')", "InStr('µ','Μ')", "InStr('ǆ','Ǆ')",
        "InStr('ǅ','ǆ')", "InStr('ſ','s')", "InStr('ꭰ','Ꭰ')", "InStr('ĳ','ij')", "InStr('ﬁ','fi')", "InStr('ǆ','dž')",
        // a letter spelt out, and a match that begins or ends inside one
        "InStr('ß','ss')", "InStr('ss','ß')", "InStr('ß','s')", "InStr('ß','sss')", "InStr('ßs','sss')", "InStr('sß','sss')",
        "InStr('ßß','sss')", "InStr(2,'ßß','ss')", "InStr(2,'ßß','sss')", "InStr(2,'ßa','s')", "InStr(3,'aßb','s')",
        "InStr('aßb','sb')", "InStr('aßb','as')", "InStr('aßb','asb')", "InStr('assb','aßb')", "InStr('asssb','ß')",
        "InStr('ẞ','ss')", "InStr('æ','e')", "InStr('aæe','ee')", "InStr('aæe','ea')", "InStr('Æ','æ')", "InStr('œ','E')",
        "InStr('þ','H')", "InStr('Þ','th')", "InStr('ß','sz')", "InStr('sz','ß')", "InStr('ß','')", "InStr('','ß')",
        // the compare argument
        "InStr(1,'ß','s',0)", "InStr(1,'ß','ss',1)", "InStr(1,'þ','th',1)", "InStr(1,'ß','ss',1033)", "InStr(1,'ß','ss',1041)",
        "InStr(1,'ß','ss',3079)", "InStr(1,'ß','sz',3079)", "InStr(1,'ß','z',3079)", "InStr(1,'sz','ß',3079)",
        "InStr(1,'æß','es',3079)", "InStr(1,'æ','ae',1030)", "InStr(1,'œ','oe',1030)", "InStr(1,'þ','th',1039)",
        "InStr(1,'þß','hs',1055)", "InStr(1,'þß','s',1055)", "InStr(1,'ß','ss',1029)", "InStr(1,'æ','ae',1029)",
        "InStr(1,'ß','ss',66567)", "InStr(1,'ß','ss',3)", "InStr(1,'i','I',1055)", "InStr(1,'ı','I',1055)",
        "InStr(1,'ß','ss',1034)", "InStr(1,'ß','ss',4103)", "InStrRev('ß','ss',-1,4103)", "Replace('ß','ss','x',1,-1,5127)",
        "InStr(1,'ß','ss',1024)", "InStrRev('ß','ss',-1,1024)", "Replace('ß','ss','x',1,-1,1024)", "InStrRev('ß','ss',-1,2048)",
        // a compare argument that is not one: 2 is a locale ID to InStrRev and Replace alone, and InStr refuses only
        // once there is something to search. (A Null one, which ACE refuses as a type mismatch, gives Null in
        // LibRed as every Null argument does, so is not asked here.)
        "InStr(1,'a','A',2)", "InStr(1,'a','A',-1)", "InStr(1,'a','A',20000)", "InStr(1,'a','A',1.5)", "InStr(1,'a','A',0.5)",
        "InStr(1,'','a',2)", "InStr(1,'a','',2)", "InStr(5,'a','a',2)", "InStr(1,Null,'a',2)",
        "InStrRev('a','A',-1,2)", "InStrRev('ß','ss',-1,2)", "InStrRev('ß','ss',-1,1.5)", "InStrRev('ß','ss',-1,'2')",
        "InStrRev('a','A',-1,-1)", "InStrRev('a','a',5,-1)", "InStrRev('','a',-1,-1)", "InStrRev('a','',-1,-1)",
        "Replace('a','A','x',1,-1,2)", "Replace('ß','ss','x',1,-1,2)", "Replace('a','A','x',1,-1,-1)",
        "Replace('','a','b',1,-1,-1)", "Replace('a','','b',1,-1,-1)", "Replace('a','a','b',5,-1,-1)",
        // InStrRev
        "InStrRev('ab ','ab  ')", "InStrRev('co-op','coop')", "InStrRev('ß','s')", "InStrRev('ßs','ss')", "InStrRev('sß','ss')",
        "InStrRev('ßß','ss')", "InStrRev('ßß','sss')", "InStrRev('ßß','s',1)", "InStrRev('ßß','sss',1)", "InStrRev('aßb','sb',2)",
        "InStrRev('aßb','sb',3)", "InStrRev('aßb','as',1)", "InStrRev('aßb','as',2)", "InStrRev('assb','ß',2)",
        "InStrRev('assb','ß',3)", "InStrRev('ssss','ß')", "InStrRev('ssss','ß',3)", "InStrRev('aæe','ee')", "InStrRev('aæe','ee',2)",
        "InStrRev('ß','s',-1,0)", "InStrRev('ß','s',-1,1041)", "InStrRev('ß','ss',-1,1)", "InStrRev('þß','s',-1,1055)",
        "InStrRev('ß','z',-1,3079)", "InStrRev('abab','AB',3)",
        // Replace takes whole characters only
        "Replace('ab ','ab  ','x')", "Replace('ab ','ab ','x')", "Replace('co-op','coop','x')", "Replace('é','e','x')",
        "Replace('aéb','É','x')", "Replace('Ａ','A','x')", "Replace('ς','Σ','x')", "Replace('σ','Σ','x')", "Replace('ǅ','ǆ','x')",
        "Replace('ß','s','x')", "Replace('ß','ss','x')", "Replace('ßß','ssss','x')", "Replace('ßß','sss','x')",
        "Replace('ßs','sss','x')", "Replace('sß','sss','x')", "Replace('sßs','sss','x')", "Replace('sss','ß','x')",
        "Replace('aßb','sb','x')", "Replace('aßb','as','x')", "Replace('assb','aßb','x')", "Replace('aæe','eae','x')",
        "Replace('aeae','æ','x')", "Replace('ÆÞ','aeth','x')", "Replace('ßß','ss','x',2)", "Replace('ßß','ss','x',1,1)",
        "Replace('ß','ss','x',1,-1,0)", "Replace('ß','ss','x',1,-1,1)", "Replace('ß','ss','x',1,-1,1041)",
        "Replace('æß','sz','x',1,-1,3079)", "Replace('æß','ss','x',1,-1,3079)", "Replace('æß','ae','x',1,-1,3079)",
        "Replace('þß','th','x',1,-1,1055)", "Replace('þß','ss','x',1,-1,1055)", "Replace('a' & ChrW(173) & 'b','ab','x')",
    ];

    [Theory]
    [InlineData(CollatingOrder.General, 0, 0)]
    [InlineData(CollatingOrder.General, 1, 0)]
    [InlineData(CollatingOrder.NorwegianDanish, 0, 0)]
    [InlineData(CollatingOrder.Icelandic, 0, 0)]
    [InlineData(CollatingOrder.Turkish, 0, 0)]
    [InlineData(CollatingOrder.Czech, 0, 0)]
    [InlineData(CollatingOrder.GermanAustria, 0, 0)]
    [InlineData(CollatingOrder.German, 0, 1)]      // German phone book: 0x10407
    [InlineData(CollatingOrder.Japanese, 0, 0)]
    [InlineData(CollatingOrder.Croatian, 1, 0)]
    [InlineData(CollatingOrder.Spanish, 0, 0)]
    [InlineData(CollatingOrder.GermanLuxembourg, 0, 0)]   // spells out in InStrRev and Replace, not in InStr
    [InlineData(CollatingOrder.ArabicNeutral, 0, 0)]      // locale ID 1: the user's locale
    [InlineData(CollatingOrder.BulgarianNeutral, 0, 0)]   // locale ID 2: InStr is an invalid procedure call
    public void Text_is_searched_as_ace_searches_it(CollatingOrder order, byte version, byte sortId)
    {
        var collation = new Collation(order, version, sortId);
        string[] ace = Ace(collation, Expressions), libred = LibRed(collation, Expressions);

        var differences = Expressions.Select((e, i) => (e, i)).Where(x => ace[x.i] != libred[x.i])
            .Select(x => $"{x.e}: ACE {ace[x.i]}, LibRed {libred[x.i]}").ToList();
        Assert.True(differences.Count == 0, string.Join("; ", differences));
        // The instrument is measuring: whatever the database, a longer run of trailing spaces is not found, and
        // locale 1033 spells ß out.
        Assert.Equal("0", ace[Array.IndexOf(Expressions, "InStr(1,'ab ','ab  ',1)")]);
        Assert.Equal("1", ace[Array.IndexOf(Expressions, "InStr(1,'ß','ss',1033)")]);
    }

    /// <summary>The compare arguments each candidate pair is asked under: the database's locale (left out), the
    /// user's (1), and one locale ID from each set that spells out different letters — Luxembourg German (4103)
    /// among them, which does in InStrRev and Replace and not in InStr. 2 is a locale ID to those two alone.</summary>
    private static readonly string[] Compares = ["", "1", "1033", "3079", "1029", "1030", "1055", "1041", "4103", "1024"];

    [Fact]
    public void Every_candidate_pair_is_found_as_ace_finds_it()
    {
        string[] predicates = [.. Compares.SelectMany(compare => (string[])
        [
            compare.Length == 0 ? "InStr(V,P)=1" : $"InStr(1,V,P,{compare})=1",
            compare.Length == 0 ? "InStrRev(V,P)=1" : $"InStrRev(V,P,-1,{compare})=1",
            compare.Length == 0 ? "Len(Replace(V,P,''))=0" : $"Len(Replace(V,P,'',1,-1,{compare}))=0",
        ]), "InStrRev(V,P,-1,2)=1", "Len(Replace(V,P,'',1,-1,2))=0"];
        var pairs = LikeCandidates.Pairs;
        HashSet<int>[] ace = LikeCandidates.AceMatches(Collation.GeneralLegacy, predicates);
        HashSet<int>[] libred = LikeCandidates.LibRedMatches(Collation.GeneralLegacy, predicates);

        string Show(int i) => $"'{pairs[i].Value}'~'{pairs[i].Pattern}'";
        var wrong = new List<string>();
        for (int p = 0; p < predicates.Length; p++)
        {
            var missed = ace[p].Except(libred[p]).Take(10).Select(Show).ToList();
            var extra = libred[p].Except(ace[p]).Take(10).Select(Show).ToList();
            if (missed.Count + extra.Count > 0)
                wrong.Add($"{predicates[p]} — ACE only: {string.Join(", ", missed)}; LibRed only: {string.Join(", ", extra)}");
            Assert.True(ace[p].Count > 1_900, $"ACE matched only {ace[p].Count} for {predicates[p]} — the instrument is not measuring");
        }
        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong));
    }

    /// <summary>Every compare argument from -2 to 65535, and sort IDs 1 to 15 of every language ID ACE accepts: each
    /// of InStr, InStrRev and Replace accepts the ones ACE does, and spells out under each the letters ACE does. The
    /// whole range takes minutes, so it is asked with LIBRED_SCREEN_COMPARE=1; otherwise the range is -2 to 300,
    /// every ID the runtime knows, and the ones ACE takes that the runtime does not list.</summary>
    [Fact]
    public void Every_compare_argument_is_read_as_ace_reads_it()
    {
        int[] range = Environment.GetEnvironmentVariable("LIBRED_SCREEN_COMPARE") == "1"
            ? [.. Enumerable.Range(-2, 0x10002)]
            : [.. Enumerable.Range(-2, 303)
                .Concat(CultureInfo.GetCultures(CultureTypes.AllCultures).Select(c => c.LCID))
                .Concat([1024, 1034, 1127, 1137, 1142, 1164, 2048, 2305, 3072, 4096, 4103, 5127, 19465, 20000])
                .Concat(Enumerable.Range(8, 12).Select(i => i * 0x400))
                .Distinct().Order()];
        string[] accepting = [.. range.Select(compare => $"InStrRev('a','A',-1,{compare})")];
        string[] aceAccepts = Ace(Collation.GeneralLegacy, accepting);
        int[] languages = [.. range.Where((compare, i) => compare > 1 && aceAccepts[i] != "E")];
        int[] every = [.. range, .. languages.SelectMany(language => Enumerable.Range(1, 15).Select(sort => (sort << 16) | language))];

        string[] probes = ["'ß','ss'", "'ß','sz'", "'ß','s'", "'ß','z'", "'æ','ae'", "'Æ','ae'", "'æ','a'", "'œ','oe'", "'Œ','oe'",
            "'þ','th'", "'Þ','th'", "'þ','t'", "'i','I'", "'ı','I'", "'i','İ'", "'ä','ae'", "'å','aa'", "'ø','oe'", "'ü','ue'", "'ĳ','ij'"];
        (string Name, Func<string, int, string> Call)[] functions =
        [
            ("InStr", (p, compare) => $"InStr(1,{p},{compare})"),
            ("InStrRev", (p, compare) => $"InStrRev({p},-1,{compare})"),
            ("Replace", (p, compare) => $"Len(Replace({p},'',1,-1,{compare}))"),
        ];
        var wrong = new List<string>();
        foreach ((string name, Func<string, int, string> call) in functions)
        {
            int[] compares = every;
            string[] expressions = [.. compares.Select(compare => string.Join(" & ", probes.Select(p => call(p, compare))))];
            string[] ace = Ace(Collation.GeneralLegacy, expressions), libred = LibRed(Collation.GeneralLegacy, expressions);
            wrong.AddRange(compares.Select((compare, i) => (compare, i)).Where(x => ace[x.i] != libred[x.i])
                .Select(x => $"{name} {x.compare}: ACE {ace[x.i]}, LibRed {libred[x.i]}"));

            Assert.True(ace.Count(a => a != "E") > 400, $"ACE accepted too few compare arguments for {name} — the instrument is not measuring");
            Assert.Equal(7, ace.Where(a => a != "E").Distinct().Count());   // binary, no letters, and the five sets of them
        }
        Assert.True(wrong.Count == 0, string.Join("; ", wrong.Take(40)));
    }

    private static string[] Ace(Collation collation, string[] expressions)
    {
        string path = TemporaryDatabase.CreatePath("text-search-", ".accdb");
        try
        {
            JetDatabase.Create(path, collation: collation);
            using OleDbConnection ace = AceTestDatabase.Open(path);
            return [.. expressions.Select(expression =>
            {
                try
                {
                    using OleDbCommand command = ace.CreateCommand();
                    command.CommandText = "SELECT " + expression;
                    return command.ExecuteScalar() is { } value and not DBNull ? Convert.ToString(value, CultureInfo.InvariantCulture)! : "N";
                }
                catch (OleDbException) { return "E"; }
            })];
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    private static string[] LibRed(Collation collation, string[] expressions)
    {
        string path = TemporaryDatabase.CreatePath("text-search-libred-", ".accdb");
        try
        {
            JetDatabase.Create(path, collation: collation);
            using var db = JetDatabase.Open(path, readOnly: false);
            var engine = new QueryEngine(db);
            return [.. expressions.Select(expression =>
            {
                try
                {
                    return engine.ExecuteQuery("SELECT " + expression).Rows.Single()[0] is { } value
                        ? Convert.ToString(value, CultureInfo.InvariantCulture)! : "N";
                }
                catch (Exception e) when (e is ArgumentException or InvalidOperationException) { return "E"; }
            })];
        }
        finally { TemporaryDatabase.Delete(path); }
    }
}
