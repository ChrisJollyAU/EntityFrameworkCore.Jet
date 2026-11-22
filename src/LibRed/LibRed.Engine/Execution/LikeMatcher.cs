using LibRed.Storage.Types;

namespace LibRed.Engine.Execution;

/// <summary>
/// The <c>LIKE</c> match with ANSI-92 wildcards, the set EF Core emits over both OLE DB and ODBC (verified vs ACE).
/// </summary>
/// <remarks>
/// <para><b>The wildcard set belongs to the connection, not to the database file</b> — measured over ACE
/// across nine combinations. OLE DB is ANSI-92 for ad-hoc SQL and for saved queries alike; ODBC is too once
/// <c>ExtendedAnsiSQL=1</c> is set, which <c>JetConnection</c> sets on every ODBC connection it makes. Only a
/// bare ODBC connection — one this repo never opens — reads a saved query's pattern as ANSI-89. The
/// <c>MSysDb</c> property <c>ANSI Query Mode</c> changes none of it: it is an Access application setting the
/// engine does not consult, so there is one wildcard set here, not two. Access's own saved queries are full
/// of <c>*</c> patterns, which makes the opposite look true; it isn't, and this is the expensive way to find
/// out. See <c>system-catalog.md</c>.</para>
/// <list type="bullet">
/// <item><c>%</c> matches any run and <c>_</c> any one character. <c>*</c>, <c>?</c> and <c>#</c> are plain
/// characters.</item>
/// <item><c>[abc]</c> matches one of the characters listed, <c>[!abc]</c> one character not listed, and <c>a-c</c>
/// inside the brackets a range. A <c>-</c> at either end is itself listed, <c>[!]</c> is the character <c>!</c>, and
/// <c>[]</c> matches nothing at all, so <c>[]]</c> is a plain <c>]</c> and <c>[[]]</c> the text <c>[]</c>. The
/// Access documentation gives <c>^</c> as the ANSI-92 negation, but ACE does not treat it so: <c>[^ae]</c> lists
/// <c>^</c>, <c>a</c> and <c>e</c>, and <c>!</c> negates as it does in ANSI-89.</item>
/// <item>Case is ignored and accents are not, by ACE's own table rather than any runtime's casing: it folds 973
/// case pairs and leaves out what a runtime would add — the micro sign, long s, final sigma, the Greek symbol
/// variants, the titlecase digraphs and every letter Unicode added later. Four letters are spelt out: <c>ß</c> as
/// <c>ss</c>, <c>æ</c> as <c>ae</c>, <c>œ</c> as <c>oe</c> and <c>þ</c> as <c>th</c>, capitals likewise — in the
/// pattern, in a bracket list and in the value, so <c>'aßb' LIKE 'a[s]sb'</c> is True; <c>_</c> still takes the whole
/// character. The table is <see cref="TextFold"/>'s: measured and embedded, so LIKE answers the same on every
/// platform.</item>
/// <item>None of it depends on the database's collation: the same pairs match in every one of the 405 non-CJK orders
/// LibRed can create, Turkish included (verified vs ACE; the CJK orders are not yet screened). Width, kana, superscripts, hyphens and apostrophes, which the
/// collation folds or ignores, all count here, and so do trailing spaces: <c>'a ' LIKE 'a'</c> is False where
/// <c>'a ' = 'a'</c> is True.</item>
/// <item>A bracket that is never closed, or a range written backwards, is an invalid pattern. It is only reported
/// when the match reaches it with a character left to test: <c>'' LIKE '['</c> and <c>'z' LIKE 'x[z-a]'</c> are
/// both False.</item>
/// <item>There is no <c>ESCAPE</c> clause; ACE rejects it as a syntax error. A wildcard is matched literally by
/// bracketing it.</item>
/// </list>
/// <para>Documentation: https://support.microsoft.com/en-us/office/like-operator-b2f7ef03-9085-4ffb-9829-eef18358e931,
/// https://support.microsoft.com/en-us/office/access-wildcard-character-reference-af00c501-7972-40ee-8889-e18abaad12d1
/// and https://support.microsoft.com/en-us/office/use-wildcards-in-queries-and-parameters-in-access-ec057a45-78b1-4d16-8c20-242cde582e0b.</para>
/// </remarks>
internal static class LikeMatcher
{
    public static bool IsMatch(string value, string pattern)
    {
        string text = TextFold.Fold(value, TextFold.Letters.All, out int[]? source);
        int[]? characterEnd = null;

        // reach[p] is whether the pattern so far can consume exactly the first p characters of the text.
        var reach = new bool[text.Length + 1];
        reach[0] = true;
        int i = 0;
        while (i < pattern.Length)
        {
            int first = Array.IndexOf(reach, true);
            if (first < 0)
                return false;

            if (pattern[i] == '%')
            {
                Array.Fill(reach, true, first, reach.Length - first);
                i++;
                continue;
            }

            var next = new bool[reach.Length];
            if (pattern[i] == '_')
            {
                // One character of the value, however many it folds to (verified vs ACE: 'ß' LIKE '_' is True).
                characterEnd ??= CharacterEnds(source, text.Length);
                for (int p = first; p < text.Length; p++)
                    next[characterEnd[p]] |= reach[p];
                i++;
            }
            else if (pattern[i] == '[')
            {
                int close = pattern.IndexOf(']', i + 1);
                if (close < 0)
                    return Invalid(first, text);
                if (close == i + 1)
                {
                    i += 2; // [] matches nothing
                    continue;
                }
                if (BracketList.Parse(pattern.AsSpan(i + 1, close - i - 1)) is not { } list)
                    return Invalid(first, text);
                for (int p = first; p < text.Length; p++)
                {
                    if (reach[p])
                        list.Match(text, p, next);
                }
                i = close + 1;
            }
            else
            {
                int end = pattern.IndexOfAny(['%', '_', '['], i);
                if (end < 0)
                    end = pattern.Length;
                string run = TextFold.Fold(pattern[i..end], TextFold.Letters.All, out _);
                for (int p = first; p + run.Length <= text.Length; p++)
                {
                    if (reach[p] && text.AsSpan(p).StartsWith(run, StringComparison.Ordinal))
                        next[p + run.Length] = true;
                }
                i = end;
            }
            reach = next;
        }
        return reach[text.Length];
    }

    /// <summary>For each position of the folded value, where the value's character it belongs to ends — from the
    /// characters <see cref="TextFold.Fold"/> says each position came from (null: one position per character).</summary>
    private static int[] CharacterEnds(int[]? source, int foldedLength)
    {
        var ends = new int[foldedLength];
        for (int p = foldedLength - 1; p >= 0; p--)
            ends[p] = source is not null && p + 1 < foldedLength && source[p + 1] == source[p] ? ends[p + 1] : p + 1;
        return ends;
    }

    private static bool Invalid(int first, string text) =>
        first < text.Length ? throw new ArgumentException("Invalid pattern string.") : false;

    private sealed class BracketList(bool negated, List<string> members, List<(char Low, char High)> ranges)
    {
        /// <summary>The list between the brackets, or null when a range is written backwards.</summary>
        public static BracketList? Parse(ReadOnlySpan<char> body)
        {
            bool negated = body.Length > 1 && body[0] == '!';
            if (negated)
                body = body[1..];

            var members = new List<string>();
            var ranges = new List<(char, char)>();
            for (int i = 0; i < body.Length; i++)
            {
                if (i + 2 < body.Length && body[i + 1] == '-')
                {
                    if (body[i] > body[i + 2])
                        return null;
                    ranges.Add((TextFold.FoldChar(body[i]), TextFold.FoldChar(body[i + 2])));
                    i += 2;
                }
                else
                {
                    members.Add(TextFold.Fold(body[i].ToString(), TextFold.Letters.All, out _));
                }
            }
            return new BracketList(negated, members, ranges);
        }

        /// <summary>Marks in <paramref name="next"/> each position the list can advance <paramref name="p"/> to. A
        /// member that expands matches two characters, where anything else matches one.</summary>
        public void Match(string text, int p, bool[] next)
        {
            bool any = false;
            foreach (string member in members)
            {
                if (text.AsSpan(p).StartsWith(member, StringComparison.Ordinal))
                {
                    any = true;
                    next[p + member.Length] |= !negated;
                }
            }
            foreach ((char low, char high) in ranges)
            {
                if (text[p] >= low && text[p] <= high)
                {
                    any = true;
                    next[p + 1] |= !negated;
                }
            }
            if (negated && !any)
                next[p + 1] = true;
        }
    }
}
