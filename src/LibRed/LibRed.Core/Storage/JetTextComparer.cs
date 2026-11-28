using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using LibRed.Catalog;

namespace LibRed.Storage;

/// <summary>
/// Text compared as ACE compares it: in one collation, by the keys its index encoder writes for that collation
/// (case folded, accents significant, hyphens and apostrophes weighed only after the letters, trailing spaces
/// ignored, and each locale's own letters where it has them — see <see cref="JetTextCollation"/>).
/// </summary>
/// <remarks>
/// <para>A query compares text in the <b>database's</b> collation — page 0's, never a column's (page-02b §3.4,
/// verified against ACE) — so an engine uses the one comparer <see cref="For"/> gives for
/// <see cref="JetDatabase.Collation"/>.</para>
/// <para>Built on LibRed's own weight tables alone, which cover the whole Basic Multilingual Plane, so the answer
/// is the same on every platform: nothing here consults the runtime's culture data, whose collation differs
/// between ICU and Windows NLS and between their versions. An order the index encoder refuses is refused here
/// too, at the first comparison, rather than answered in some other order.</para>
/// </remarks>
public sealed class JetTextComparer : IEqualityComparer<string>
{
    private static readonly ConcurrentDictionary<Collation, JetTextComparer> Comparers = new();
    private static readonly ConcurrentDictionary<Collation, JetTextComparer> NameComparers = new();

    /// <summary>The comparer for <paramref name="collation"/>.</summary>
    public static JetTextComparer For(Collation collation) => Comparers.GetOrAdd(collation, c => new JetTextComparer(c, keepKeys: false));

    /// <summary>The comparer for <paramref name="collation"/> that keeps each text's <see cref="Key"/>, for object names:
    /// a statement compares the same few names over and over — a reference against every column of a scope, per row
    /// where the scope is built per row — and encoding both sides each time made two different names cost a hundred
    /// times what a case-insensitive compare did. Bounded, since names arrive from SQL text as well as from the
    /// catalog.</summary>
    public static JetTextComparer ForNames(Collation collation) =>
        NameComparers.GetOrAdd(collation, c => new JetTextComparer(c, keepKeys: true));

    private readonly LocaleTailoring? _tailoring;
    private readonly bool _version1;
    private readonly bool _encodable;
    private readonly ConcurrentDictionary<string, (byte[] Key, int Hash)>? _keys;

    private JetTextComparer(Collation collation, bool keepKeys)
    {
        Collation = collation;
        _encodable = collation.IsIndexKeyEncodable;
        _tailoring = _encodable ? JetLocaleTailoring.For(collation) : null;
        _version1 = collation.Version == Collation.GeneralVersion;
        if (keepKeys) _keys = new(StringComparer.Ordinal);
    }

    private (byte[] Key, int Hash) Kept(string text)
    {
        if (_keys!.TryGetValue(text, out var kept)) return kept;
        if (_keys.Count >= 4096) _keys.Clear();
        byte[] key = Key(text);
        var hash = new HashCode();
        hash.AddBytes(key);
        return _keys[text] = (key, hash.ToHashCode());
    }

    /// <summary>The collation this compares in.</summary>
    public Collation Collation { get; }

    // Both sides are encoded into this thread's buffers: a comparison is made per row, or per pair in a sort,
    // and fresh lists each time were most of what a text GROUP BY allocated.
    [ThreadStatic] private static List<byte>? t_left;
    [ThreadStatic] private static List<byte>? t_right;

    /// <summary>The key <paramref name="text"/> sorts by. Two keys compare, byte for byte, as
    /// <see cref="Compare(string, string)"/> compares their texts, so a sort can build each value's key once.</summary>
    public byte[] Key(string text)
    {
        List<byte> key = t_left ??= [];
        Encode(text, key);
        return [.. key];
    }

    /// <summary>The sign of <paramref name="a"/> against <paramref name="b"/>.</summary>
    public int Compare(string a, string b)
    {
        List<byte> left = t_left ??= [], right = t_right ??= [];
        Encode(a, left);
        Encode(b, right);
        return Math.Sign(CollectionsMarshal.AsSpan(left).SequenceCompareTo(CollectionsMarshal.AsSpan(right)));
    }

    /// <summary>The sign of <paramref name="a"/> against the text whose <see cref="Key"/> is
    /// <paramref name="bKey"/> — for a side compared with many values, whose key is made once.</summary>
    public int Compare(string a, byte[] bKey)
    {
        List<byte> left = t_left ??= [];
        Encode(a, left);
        return Math.Sign(CollectionsMarshal.AsSpan(left).SequenceCompareTo(bKey));
    }

    /// <summary>Whether the two texts are one value in this collation.</summary>
    /// <remarks>Texts identical once trailing spaces are dropped are equal in any order, so they are answered
    /// without encoding either.</remarks>
    public bool Equals(string? x, string? y)
    {
        if (x is null || y is null) return x is null && y is null;
        return x.AsSpan().TrimEnd(' ').SequenceEqual(y.AsSpan().TrimEnd(' '))
            || (_keys is null ? Compare(x, y) == 0 : Kept(x).Key.AsSpan().SequenceEqual(Kept(y).Key));
    }

    /// <summary>The spaces <paramref name="text"/> ends in, which its key does not weigh: where ACE tells two texts
    /// apart by them — StrComp's tie, a property owner — it counts them on top of the comparison.</summary>
    internal static int TrailingSpaces(string text) => text.Length - text.AsSpan().TrimEnd(' ').Length;

    /// <summary>A hash that agrees with <see cref="Equals(string?, string?)"/>: taken over the key.</summary>
    public int GetHashCode(string text)
    {
        if (_keys is not null) return Kept(text).Hash;
        List<byte> key = t_left ??= [];
        Encode(text, key);
        var hash = new HashCode();
        hash.AddBytes(CollectionsMarshal.AsSpan(key));
        return hash.ToHashCode();
    }

    private void Encode(string text, List<byte> output)
    {
        if (!_encodable)
            throw new NotSupportedException(
                $"Text is compared in the database's collation, {Collation.Order} version {Collation.Version}" +
                (Collation.SortId == 0 ? "" : $" sort id {Collation.SortId}") + ", which is not implemented yet.");

        output.Clear();
        bool encoded = _version1
            ? JetTextCollationV1.TryEncode(text, output, _tailoring)
            : JetTextCollation.TryEncode(text, output, _tailoring);

        // Both tables cover the whole BMP, and each order handles surrogates (CollationBmpCoverageTests), so
        // this cannot be reached by any string; refusing is still the answer if it ever is.
        if (!encoded)
            throw new NotSupportedException(
                $"'{text}' holds a character the {Collation.Order} v{Collation.Version} order has no weight for.");
    }
}
