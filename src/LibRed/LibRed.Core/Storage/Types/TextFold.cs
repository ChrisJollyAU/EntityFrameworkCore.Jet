using System.Globalization;
using System.Text;

namespace LibRed.Storage.Types;

/// <summary>
/// Text as ACE's <c>LIKE</c>, <c>InStr</c>, <c>InStrRev</c> and <c>Replace</c> compare it (verified vs ACE): case
/// folded by ACE's own table, and certain letters spelt out as two. None of them compares in a collation, so what a
/// collation folds or ignores — accents, width, kana, hyphens, apostrophes, ignorable characters, trailing spaces —
/// all counts here. The same table is ACE's UCase and LCase (verified vs ACE over every BMP character but the
/// surrogates: each changes exactly the 973 characters of its pairs, and no other).
/// </summary>
/// <remarks>
/// <para>The case table is narrower than any runtime's: 973 pairs, leaving out the micro sign, long s, final sigma,
/// the Greek symbol variants, the titlecase digraphs and every letter Unicode added later. It is measured
/// (LikeFoldTableGeneratorTest) and embedded, so the answer is the same on every platform.</para>
/// <para>Seven letters can be spelt out: <c>ß</c> as <c>ss</c>, <c>æ</c> as <c>ae</c>, <c>œ</c> as <c>oe</c> and
/// <c>þ</c> as <c>th</c>, capitals likewise. <c>LIKE</c> spells out all of them, always. The three functions spell
/// out the ones their locale ID does (<see cref="Locale(int, Search)"/>, <see cref="Letters"/>).</para>
/// </remarks>
internal static class TextFold
{
    /// <summary>The function searching: each reads its compare argument, and spells letters out, a little
    /// differently.</summary>
    public enum Search
    {
        InStr,
        InStrRev,
        Replace,
    }

    /// <summary>The letters a locale ID spells out.</summary>
    [Flags]
    public enum Letters
    {
        None = 0,
        Eszett = 1,
        Ae = 2,
        Oe = 4,
        Thorn = 8,
        All = Eszett | Ae | Oe | Thorn,

        /// <summary><c>ß</c> spelt <c>sz</c>, where <see cref="Eszett"/> spells it <c>ss</c>.</summary>
        EszettAsSz = 16,
    }

    /// <summary>What each character folds to, the character that folds to it, and the characters spelt out as
    /// several — ACE's own table.</summary>
    private static readonly (char[] Fold, char[] Lower, string?[] Expansions) Table = LoadTable();

    /// <remarks>Laid out as the sort-key tables are: the two counts, then each section deflated on its own — the
    /// case pairs as (character, what it folds to), the expansions as (character, letter count, letters). Both are
    /// held as arrays indexed by character — the expansions only as far as the highest one — because every
    /// character of every value tested is looked up in each.</remarks>
    private static (char[] Fold, char[] Lower, string?[] Expansions) LoadTable()
    {
        var fold = new char[char.MaxValue + 1];
        for (int c = 0; c <= char.MaxValue; c++) fold[c] = (char)c;
        var lower = (char[])fold.Clone();
        var spelt = new Dictionary<char, string>();

        using Stream stream = typeof(TextFold).Assembly.GetManifestResourceStream("LibRed.Resources.LikeFold.bin")
            ?? throw new InvalidOperationException("The LIKE fold table resource is missing from the assembly.");
        var reader = new BinaryReader(stream);
        int caseCount = reader.ReadInt32(), expansionCount = reader.ReadInt32();
        var cases = new BinaryReader(new MemoryStream(Inflate(reader)));
        var expansionStream = new BinaryReader(new MemoryStream(Inflate(reader)));

        for (int i = 0; i < caseCount; i++)
        {
            char from = (char)cases.ReadUInt16(), to = (char)cases.ReadUInt16();
            fold[from] = to;
            lower[to] = from;
        }
        for (int i = 0; i < expansionCount; i++)
        {
            char c = (char)expansionStream.ReadUInt16();
            var letters = new char[expansionStream.ReadByte()];
            for (int k = 0; k < letters.Length; k++) letters[k] = (char)expansionStream.ReadUInt16();
            spelt[c] = new string(letters);
        }

        var expansions = new string?[spelt.Count == 0 ? 0 : spelt.Keys.Max() + 1];
        foreach ((char c, string letters) in spelt) expansions[c] = letters;
        return (fold, lower, expansions);

        static byte[] Inflate(BinaryReader reader)
        {
            byte[] compressed = reader.ReadBytes(reader.ReadInt32());
            var output = new MemoryStream();
            using (var inflate = new System.IO.Compression.ZLibStream(new MemoryStream(compressed), System.IO.Compression.CompressionMode.Decompress))
                inflate.CopyTo(output);
            return output.ToArray();
        }
    }

    /// <summary>A character folded by ACE's case table, which is its upper case as UCase gives it.</summary>
    public static char FoldChar(char c) => Table.Fold[c];

    /// <summary>A character's lower case as LCase gives it: the one that folds to it in ACE's case table, else
    /// itself.</summary>
    public static char LowerChar(char c) => Table.Lower[c];

    /// <summary>Whether two names are one once ACE's case table folds both: how ACE matches a function's name, and
    /// a property block's owner to its column for the calculated <c>Expression</c> lookup and the DDL.</summary>
    public static bool SameName(string a, string b) =>
        string.Equals(Fold(a, Letters.None, out _), Fold(b, Letters.None, out _), StringComparison.Ordinal);

    /// <summary>Text in lower case as LCase gives it, by ACE's case table; <see cref="Fold"/> with
    /// <see cref="Letters.None"/> is its upper case, as UCase gives it.</summary>
    public static string Lower(string text) => Mapped(text, Table.Lower);

    /// <summary>Each character of <paramref name="text"/> replaced by its entry in <paramref name="map"/>.</summary>
    private static string Mapped(string text, char[] map) => string.Create(text.Length, (text, map), static (span, state) =>
    {
        for (int i = 0; i < span.Length; i++) span[i] = state.map[state.text[i]];
    });

    /// <summary>The letters a character is spelt out as, already folded (ß as SS), or null for itself. Every one the
    /// table holds when <paramref name="letters"/> is <see cref="Letters.All"/>, as LIKE spells them.</summary>
    private static string? Expansion(char c, Letters letters) =>
        c >= Table.Expansions.Length || Table.Expansions[c] is not { } spelling
            || letters != Letters.All && (letters & LetterOf(c)) == 0 ? null
        : c == 'ß' && (letters & Letters.EszettAsSz) != 0 ? "SZ"
        : spelling;

    private static Letters LetterOf(char c) => c switch
    {
        'ß' => Letters.Eszett | Letters.EszettAsSz,
        'æ' or 'Æ' => Letters.Ae,
        'œ' or 'Œ' => Letters.Oe,
        'þ' or 'Þ' => Letters.Thorn,
        _ => Letters.None,
    };

    /// <summary>
    /// The letters InStr, InStrRev and Replace spell out under a locale ID (verified vs ACE, over every ID it
    /// accepts): all of them for English, German, French, Italian, Dutch, Portuguese, Swedish, Finnish and Spanish —
    /// but only some sublanguages of each, and for Luxembourg and Liechtenstein German (4103, 5127) not in InStr;
    /// <c>ß</c> alone for Czech, Hungarian, Polish and Slovak; <c>ß</c> and <c>œ</c> for Danish, Icelandic and
    /// Norwegian Bokmål; all but <c>þ</c> for Turkish; all for Austrian German, which spells <c>ß</c> as <c>sz</c>;
    /// and none for any other — a neutral ID and an alternate sort of a listed one (German phone book) included.
    /// </summary>
    private static Letters SpeltOut(int locale, Search function) => locale switch
    {
        1031 or 1033 or 1034 or 1035 or 1036 or 1040 or 1043 or 1046 or 1053 or 2055 or 2057 or 2058 or 2060 or 2064
            or 2067 or 2070 or 3081 or 3082 or 3084 or 4105 or 4108 or 5129 => Letters.All,
        4103 or 5127 => function == Search.InStr ? Letters.None : Letters.All,
        3079 => Letters.EszettAsSz | Letters.Ae | Letters.Oe | Letters.Thorn,
        1055 => Letters.Eszett | Letters.Ae | Letters.Oe,
        1030 or 1039 or 1044 => Letters.Eszett | Letters.Oe,
        1029 or 1038 or 1045 or 1051 => Letters.Eszett,
        _ => Letters.None,
    };

    /// <summary>The user's locale ID: ACE reads the user's default locale (verified vs ACE), which LibRed takes, on
    /// every platform, as .NET's current culture.</summary>
    private static int UserLocale => CultureInfo.CurrentCulture.LCID;

    /// <summary>
    /// A compare argument as the locale ID it searches under, 0 for a binary search (verified vs ACE, over every
    /// value from -2 to 65535 and sort IDs 1 to 15 of every language ID it accepts, for each of the three
    /// functions): 0 is binary, 1 the user's locale and a locale ID itself.
    /// 2, the documented database comparison, is not one for InStr; InStrRev and Replace take it as the locale ID
    /// it also is, and take 1024 — the user-default ID, which to InStr is a locale that spells nothing out — as the
    /// user's locale. Anything else, the documented -1 included, is an invalid procedure call.
    /// </summary>
    /// <remarks>A search with no compare argument is made under the database's locale ID (<paramref name="database"/>),
    /// its sort ID included, read as an argument is (verified vs ACE in databases of each set of letters spelt out, a
    /// sort ID, and locale IDs 1 and 2) — so a German phone book database searches under 0x10407, one whose locale ID
    /// is 1 under the user's, and in one whose ID is 2 InStr is an invalid procedure call. The database's own ID is
    /// not asked of <see cref="IsLocale"/>: it is one already, and the runtime's culture data — absent altogether
    /// where globalization is invariant — must not decide whether a database can be searched.</remarks>
    public static int Locale(int compare, Search function, bool database = false) => compare switch
    {
        0 => 0,
        1 => UserLocale,
        1024 when function != Search.InStr => UserLocale,
        _ when compare >= (function == Search.InStr ? 3 : 2) && (database || IsLocale(compare)) => compare,
        _ => throw new ArgumentException($"Invalid procedure call: {compare} is not a comparison."),
    };

    /// <summary>Whether a number is a locale ID ACE accepts as a compare argument (verified vs ACE over every value from
    /// -2 to 65535 and sort IDs 1 to 15 of every language ID it accepts): one the runtime knows, or one of those
    /// Windows takes besides — the default and custom IDs, the transient ones, and five locales the runtime does not
    /// list.</summary>
    public static bool IsLocale(int id)
    {
        if (id is 140 or 1024 or 1127 or 1137 or 1142 or 1164 or 2048 or 2305 or 3072 or 4096 or 19465
            || id is >= 0x2000 and <= 0x4C00 && id % 0x400 == 0)
            return true;
        try
        {
            return id > 0 && CultureInfo.GetCultureInfo(id).LCID == id;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Text as the match compares it: the letters in <paramref name="letters"/> spelt out, every character folded —
    /// with, when a letter was spelt out, for each position of the folded text, the index in <paramref name="text"/>
    /// of the character it came from. Without one the folded text is the same length and <paramref name="source"/> is
    /// null: positions line up.
    /// </summary>
    public static string Fold(string text, Letters letters, out int[]? source)
    {
        // Almost no text holds a character that is spelt out, and without one the folded text is the same length:
        // written straight into the result, a table read per character.
        int extra = 0;
        if (letters != Letters.None)
        {
            foreach (char c in text)
                extra += (Expansion(c, letters)?.Length ?? 1) - 1;
        }
        if (extra == 0)
        {
            source = null;
            return Mapped(text, Table.Fold);
        }

        var folded = new StringBuilder(text.Length + extra);
        source = new int[text.Length + extra];
        for (int i = 0; i < text.Length; i++)
        {
            int at = folded.Length;
            if (Expansion(text[i], letters) is { } expansion)
                folded.Append(expansion);
            else
                folded.Append(FoldChar(text[i]));
            source.AsSpan(at, folded.Length - at).Fill(i);
        }
        return folded.ToString();
    }

    /// <summary>
    /// Every match of a non-empty <paramref name="find"/> in <paramref name="text"/>, overlapping ones included, in
    /// order, as a position and length in <paramref name="text"/>; the text is folded once, however many are read.
    /// <paramref name="locale"/> is 0 for a binary match, else the locale ID whose letters are spelt out
    /// (<see cref="Locale(int, Search)"/>). (Verified vs ACE.) InStr and InStrRev match inside a spelt-out letter —
    /// <c>InStr('aßb', 'sb')</c> is 2, the letter the match begins in — where Replace takes only whole characters,
    /// so <c>'ss'</c> finds <c>ß</c> and <c>'s'</c> does not.
    /// </summary>
    public static IEnumerable<(int Index, int Length)> Matches(string text, string find, int locale, Search function)
    {
        // The table folds ASCII as ordinal case-insensitivity does, and spells no ASCII letter out.
        StringComparison? plain = locale == 0 ? StringComparison.Ordinal
            : Ascii.IsValid(text) && Ascii.IsValid(find) ? StringComparison.OrdinalIgnoreCase
            : null;
        if (plain is { } comparison)
        {
            for (int at = text.IndexOf(find, comparison); at >= 0; at = text.IndexOf(find, at + 1, comparison))
                yield return (at, find.Length);
            yield break;
        }

        Letters letters = SpeltOut(locale, function);
        string hay = Fold(text, letters, out int[]? source);
        string needle = Fold(find, letters, out _);
        for (int at = hay.IndexOf(needle, StringComparison.Ordinal); at >= 0; at = hay.IndexOf(needle, at + 1, StringComparison.Ordinal))
        {
            if (source is null)
            {
                yield return (at, needle.Length);
                continue;
            }
            int end = at + needle.Length;
            bool whole = (at == 0 || source[at - 1] != source[at]) && (end == hay.Length || source[end] != source[end - 1]);
            if (whole || function != Search.Replace)
                yield return (source[at], source[end - 1] - source[at] + 1);
        }
    }
}
