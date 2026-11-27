namespace LibRed.Storage;

/// <summary>
/// The kana section of an index key, which both sort-order versions build identically.
/// </summary>
/// <remarks>
/// A kana weighs <c>7F &lt;sound&gt;</c> with voicing as an ordinary secondary, and the small/normal
/// distinction lives in a section of its own rather than in either weight. That is measured behaviour for
/// General Legacy (v0), and it holds byte-for-byte for General (v1) as well: ACE encodes <c>U+304C</c> as
/// <c>7F 7F0A 01 03 0101 FF 02 80 FF 80 00</c> under both, the same sound weights and the same section. The
/// two versions disagree about a great deal in the base table, and about kana not at all — so this is shared
/// rather than duplicated, and a fix to it necessarily reaches both.
/// </remarks>
internal static class JetKanaSection
{
    /// <summary>The page byte every kana primary starts with: a kana weighs <c>7F &lt;sound&gt;</c>.</summary>
    public const byte KanaPage = 0x7F;

    /// <summary>One byte of the section that closes the kana section, after the mark codes: <c>02</c>, then a
    /// <c>80</c> for every seven kana or part of seven, <c>FF</c>, and the same <c>80</c>s again — one up to seven
    /// kana, two up to fourteen, three up to 21 (measured to 44). Counted over the kana alone, a prolonged sound
    /// mark among them, whatever else the string holds; the same across hiragana, katakana, halfwidth, small and
    /// voiced forms. Each <c>80</c> reads as seven one-bit codes under the marker bit, all clear in every string
    /// measured; what they record is not established.</summary>
    private const byte TailGroup = 0x80;

    /// <summary>The mark code of a kana that is a letter in its own right.</summary>
    public const byte Letter = 0b01;

    /// <summary>The mark code of a prolonged sound mark <c>ー</c> lengthening the kana before it.</summary>
    public const byte Prolonged = 0b11;

    /// <summary>The mark code of an iteration mark (<c>ゝ</c>, <c>ヽ</c>, <c>々</c> …) repeating the kana before
    /// it — <c>かゝ</c> closes <c>FF 98</c>, where <c>かー</c> closes <c>FF 9C</c> and <c>かあ</c> a bare <c>FF</c>.</summary>
    public const byte Repeat = 0b10;

    /// <summary>
    /// Whether <paramref name="character"/> is an iteration mark — [MS-UCODEREF]'s <c>PW_REPEAT</c> — and the
    /// secondary it adds to the weight it repeats.
    /// </summary>
    /// <remarks>
    /// An iteration mark weighs as a copy of the weight the character before it contributed, with its OWN
    /// secondary: <c>人々</c> is <c>9FD4 9FD4</c> with secondaries <c>02 05</c>, and <c>がゝ</c> is <c>が</c> twice
    /// with secondaries <c>03 02</c> — the mark does not inherit the voicing, <c>ゞ</c> adds its own. The long
    /// vowel mark is one too wherever it has no kana to lengthen: <c>人ー</c> and <c>aー</c> double what came
    /// before. Measured against ACE under both versions, which agree except that <c>〻</c> and <c>ꀕ</c> are
    /// repeat marks only in version 1 — version 0 ignores both.
    /// </remarks>
    /// <param name="character">The character.</param>
    /// <param name="version1">Whether the order is version 1.</param>
    /// <param name="secondary">The secondary the mark adds.</param>
    public static bool TryGetIterationMark(char character, bool version1, out byte secondary)
    {
        secondary = character switch
        {
            (char)0x3005 => 0x05,                                                             // 々
            (char)0x309D or (char)0x30FD or (char)0x3031 => 0x02,                             // ゝ ヽ 〱
            _ when IsProlongedSoundMark(character) => 0x02,                                   // ー ｰ, with no kana to lengthen
            (char)0x309E or (char)0x30FE or (char)0x3032 => 0x03,                             // ゞ ヾ 〲 — voiced
            (char)0x303B when version1 => 0x05,                                               // 〻
            (char)0xA015 when version1 => 0x07,                                               // ꀕ
            _ => 0,
        };
        return secondary != 0;
    }

    /// <summary>Whether <paramref name="character"/> is a prolonged sound mark — <c>ー</c> or halfwidth <c>ｰ</c>. Right
    /// after a kana it lengthens that kana's vowel, taking the vowel's primary and the kana's small flag and marking
    /// itself <see cref="Prolonged"/>; with no kana before it, it is an iteration mark.</summary>
    public static bool IsProlongedSoundMark(char character) => character is (char)0x30FC or (char)0xFF70;

    /// <summary>Whether <paramref name="character"/> is a halfwidth voicing mark, and the secondary it gives the kana
    /// right before it: <c>ﾞ</c> voices it (<c>03</c>), <c>ﾟ</c> semi-voices it (<c>04</c>). The marks are combining —
    /// ACE folds them into that kana rather than weighing them — and with no kana before them they are weighed alone.</summary>
    public static bool TryGetHalfwidthVoicing(char character, out byte secondary)
    {
        secondary = character switch
        {
            (char)0xFF9E => 0x03,
            (char)0xFF9F => 0x04,
            _ => 0,
        };
        return secondary != 0;
    }

    /// <summary>
    /// Appends <c>01 01</c>, the packed small/normal flags, the mark codes and the closing section (<see cref="TailGroup"/>).
    /// Emitted whenever the string holds any kana at all, even if every one of them is a normal form.
    /// </summary>
    /// <param name="output">The key being built.</param>
    /// <param name="small">Per kana, whether it is a small form.</param>
    /// <param name="marks">Per kana, <see cref="Letter"/>, <see cref="Prolonged"/> or <see cref="Repeat"/>.</param>
    public static void Append(List<byte> output, List<bool> small, List<byte> marks)
    {
        output.Add(IndexKeyCodec.SectionSeparator); // ends the diacritics
        output.Add(IndexKeyCodec.SectionSeparator); // ends the empty case section
        AddCodes(output, small.Count, i => small[i] ? (byte)0b10 : (byte)0b11, unmarked: 0b11);
        output.Add(IndexKeyCodec.KanaRunSeparator);
        AddCodes(output, marks.Count, i => marks[i], unmarked: Letter);
        int groups = (small.Count + 6) / 7;
        output.Add(0x02);
        for (int i = 0; i < groups; i++) output.Add(TailGroup);
        output.Add(IndexKeyCodec.KanaRunSeparator);
        for (int i = 0; i < groups; i++) output.Add(TailGroup);
    }

    /// <summary>
    /// Packs one two-bit code per kana, three to a byte, <b>most significant first</b>, under a <c>10</c> marker
    /// in the top two bits, <c>00</c> padding the last byte. The small flags are <c>11</c> normal and <c>10</c>
    /// small, so one small kana is <c>A0</c>, "normal small" is <c>B8</c>, and four kana take two bytes, the
    /// second repeating the marker — verified against ACE over all 30 combinations up to four kana. The mark
    /// codes pack the same way.
    /// <para>
    /// Codes are written only up to the last one that is not <paramref name="unmarked"/>, so nothing at all is
    /// emitted when every kana is unmarked, which is why a lone normal kana closes straight into the tail.
    /// </para>
    /// </summary>
    private static void AddCodes(List<byte> output, int count, Func<int, byte> code, byte unmarked)
    {
        int last = count - 1;
        while (last >= 0 && code(last) == unmarked) last--;
        for (int start = 0; start <= last; start += 3)
        {
            int packed = 0x80;
            for (int slot = 0; slot < 3; slot++)
            {
                int index = start + slot;
                packed |= (index > last ? 0b00 : code(index)) << (4 - 2 * slot);
            }
            output.Add((byte)packed);
        }
    }
}