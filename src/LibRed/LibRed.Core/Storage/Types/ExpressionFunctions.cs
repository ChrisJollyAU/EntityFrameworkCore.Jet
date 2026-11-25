using EntityFrameworkCore.Jet.Data;
using LibRed.Catalog;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LibRed.Storage.Types;

/// <summary>
/// The VBA/Access and SQL functions, over the arguments of a call as <typeparamref name="TExpression"/>: the one
/// implementation LibRed.Engine's queries and Core's calculated columns both evaluate.
/// </summary>
internal abstract partial class ExpressionFunctions<TExpression>
{
    /// <summary>A function call: its name as written and its arguments.</summary>
    internal readonly record struct Call(string Name, IReadOnlyList<TExpression> Arguments);

    /// <summary>The value of <paramref name="expression"/>.</summary>
    public abstract object? Evaluate(TExpression expression);

    /// <summary>How text compares here: the database's collation.</summary>
    protected abstract JetTextComparer Text { get; }

    /// <summary>Whether an argument is a Currency.</summary>
    protected abstract bool IsCurrency(TExpression expression);

    /// <summary><paramref name="left"/> <c>^</c> <paramref name="right"/>, as the operator evaluates it.</summary>
    protected abstract object? EvaluatePower(TExpression left, TExpression right);

    /// <summary>The connection's <c>Rnd</c> seed, or null when there is no connection.</summary>
    protected abstract uint? RandSeed { get; set; }

    /// <summary>The clock as <c>Now</c> and <c>Time</c> read it: in whole seconds, the fraction dropped rather than
    /// rounded (verified vs ACE).</summary>
    protected static DateTime Clock
    {
        get
        {
            DateTime now = DateTime.Now;
            return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
        }
    }

    // What Trim, LTrim and RTrim strip (verified vs ACE): the space and the ideographic space U+3000, in any mixture —
    // not a tab, CR, LF, no-break space, the other Unicode spaces or a zero-width one.
    private static readonly char[] TrimmedSpaces = [' ', '　'];

    /// <summary>The function <paramref name="name"/> (upper case, any trailing '$' stripped) over the arguments of
    /// <paramref name="f"/>, whose count the caller has checked.</summary>
    internal object? Function(string name, Call f)
    {
        return name switch
        {
            "IIF" => IifArm(f.Arguments) is { } arm ? Evaluate(arm) : null,
            "CHOOSE" => Choose(f),
            "SWITCH" => Switch(f),
            "NULLIF" => NullIf(f),
            "COALESCE" => Coalesce(f),
            "NZ" => Nz(f),
            "GREATEST" => Extreme(f, greatest: true),
            "LEAST" => Extreme(f, greatest: false),
            "DATEPART" => DatePart(f),
            "ROUND" => Round(f),
            "FIX" => Numeric1(f, Math.Truncate, Math.Truncate, keepsDate: true),  // toward zero
            "INT" => Numeric1(f, Math.Floor, Math.Floor, keepsDate: true),        // toward -infinity
            "ABS" => Numeric1(f, Math.Abs, Math.Abs, keepsDate: false),
            // VBA/Access type-conversion functions (verified vs ACE). Each reads its argument as ConversionNumber
            // does: text as a number, a date as its serial, True as -1 — so CByte(True) overflows. CInt/CLng/CByte
            // round half to even, as Convert.ToInt16/Int32/Byte do, and a value past the type is an overflow. ACE
            // raises "Invalid use of Null" for a Null argument; LibRed returns Null. CVar passes its argument
            // through: a Variant keeps its own type while an expression uses it, and the executor writes it out as
            // text where ACE does (QueryExecutor.Variance).
            "CCUR" => DecimalArgument(f, ToCurrency),
            "CBOOL" => Convert1(f, v => VbaBoolean.Of(VbaBool(v))),
            // A Byte's range, an Integer's type (verified vs ACE: VarType(CByte(1)) is 2, and CByte(1) stores 01 00 in a
            // binary column and is a type mismatch in a GUID one).
            "CBYTE" => Convert1(f, v => (short)AsByte(v)),
            "CINT" => Convert1(f, v => (short)AsInteger(v)),
            "CLNG" => Convert1(f, v => AsLong(v)),
            // CLngLng is VBA's LongLong conversion, which the Jet Expression Service does not have (verified: ACE
            // reports it undefined), so a LibRed extension. It reads its argument as CLng does, into an Int64.
            "CLNGLNG" => Convert1(f, v => AsLongLong(v)),
            "CSNG" => Convert1(f, v => Finite(Sng(ConversionNumber(v)))),
            "CDBL" => Convert1(f, v => Dbl(ConversionNumber(v))),
            // CDec has no working ACE equivalent — the Jet Expression Service's takes exactly two arguments and gives a
            // Long 0 whatever they are (verified) — so this is a LibRed extension with no parity contract to honour.
            // CCur is ACE's route to a decimal.
            "CDEC" => DecimalArgument(f, ArithmeticDecimal),
            "CSTR" => Convert1(f, ConcatText),
            // CVDate is VBA's older name for CDate, and in ACE it is CDate: a Date, not a Variant (verified).
            "CDATE" or "CVDATE" => Convert1(f, v => ToDate(v)),
            "CVAR" => Evaluate(f.Arguments[0]),

            // VBA/Access string functions (verified vs ACE). A value that is not text is read as CStr writes it
            // (ConcatText): True is "-1" and a VBA Boolean "True", a date in the regional format, a GUID braced, and a binary value as
            // UTF-16 text, so Len(0x4100) is 1 where LenB is 2. Positions are 1-based and read as the conversion
            // functions read numbers; a Null argument gives Null. StrComp compares in the database sort order unless
            // a compare argument of 0 asks for a binary comparison; InStr, InStrRev and Replace search as TextFold
            // searches, in no sort order.
            "LEN" => Convert1(f, v => ConcatText(v).Length),
            "LCASE" => Convert1(f, v => ConcatText(v).ToLowerInvariant()),
            "UCASE" => Convert1(f, v => ConcatText(v).ToUpperInvariant()),
            "TRIM" => Convert1(f, v => ConcatText(v).Trim(TrimmedSpaces)),
            "LTRIM" => Trim(f, static (s, chars) => s.TrimStart(chars)),
            "RTRIM" => Trim(f, static (s, chars) => s.TrimEnd(chars)),
            "LEFT" => StringInt(f, static (s, n) => n <= 0 ? "" : n >= s.Length ? s : s[..n]),
            "RIGHT" => StringInt(f, static (s, n) => n <= 0 ? "" : n >= s.Length ? s : s[^n..]),
            "MID" => Mid(f),
            "INSTR" => Instr(f),
            "REPLACE" => Replace(f),
            "CONCAT_WS" => ConcatWs(f),
            "TRANSLATE" => Translate(f),

            // Date/time functions (verified vs ACE). A date argument is read as CDate reads it: text as a date in
            // the regional format, otherwise as a number, and a number as the date at that serial. Settings and
            // counts are read as CInt reads them. A Null argument gives Null. LibRed keeps milliseconds where ACE
            // rounds to the second, so Second(0.00001) is 0 here and 1 in ACE.
            "DATEADD" => DateAdd(f),
            "DATEDIFF" => DateDiff(f),
            "DATEDIFF_BIG" => DateDiffBig(f),
            "DATESERIAL" => DateParts(f, DateSerial),
            "TIMESERIAL" => DateParts(f, static (h, m, s) => OaDate((h * 3600 + m * 60 + s) / 86400.0)),
            "NOW" => Clock,
            // SQL Server's: GetUtcDate is a datetime (whole ms), the Sys ones datetime2 (100 ns).
            "GETUTCDATE" => OaDate(DateTime.UtcNow.ToOADate()),
            "SYSDATETIME" => DateTime.Now,
            "SYSUTCDATETIME" => DateTime.UtcNow,
            "DATE" => DateTime.Today,
            "TIME" => DateTime.FromOADate(0).Add(Clock.TimeOfDay),
            "YEAR" => Convert1(f, v => ToDate(v).Year),
            "MONTH" => Convert1(f, v => ToDate(v).Month),
            "DAY" => Convert1(f, v => ToDate(v).Day),
            "HOUR" => Convert1(f, v => ToDate(v).Hour),
            "MINUTE" => Convert1(f, v => ToDate(v).Minute),
            "SECOND" => Convert1(f, v => ToDate(v).Second),
            "WEEKDAY" => Weekday(f),
            // DateValue is the date at midnight and TimeValue the time on 1899-12-30. Unlike CDate they take only a
            // date or text that reads as one; a number is a type mismatch. IsDate is true for exactly those.
            "DATEVALUE" => Convert1(f, v => DateValueArgument(v).Date),
            "TIMEVALUE" => Convert1(f, v => DateTime.FromOADate(0).Add(DateValueArgument(v).TimeOfDay)),
            "ISDATE" => VbaBoolean.Of(TryDateText(Evaluate(f.Arguments[0]), out _)),
            // Jet VBA math functions (double precision). SQR = sqrt, ATN = atan, LOG = natural log.
            // ACE's Tan is its Sin over its Cos, which differs from a direct tangent in the last digit.
            "SIN" => UnaryDouble(f, Trigonometric(Math.Sin)),
            "COS" => UnaryDouble(f, Trigonometric(Math.Cos)),
            "TAN" => UnaryDouble(f, Trigonometric(x => Math.Sin(x) / Math.Cos(x))),
            "ATN" => UnaryDouble(f, Math.Atan),
            "EXP" => UnaryDouble(f, Math.Exp),
            // Log(base, x), the standard's two-argument form, is a LibRed extension beside Access's natural log.
            "LOG" => f.Arguments.Count == 2
                ? BinaryDouble(f, static (b, x) => b <= 0 || b == 1 || x <= 0 ? double.NaN : Math.Log(x, b))
                : UnaryDouble(f, Math.Log),
            "SQR" => UnaryDouble(f, Math.Sqrt),
            // SGN sits apart from the group above: it takes a double but yields an Integer, both in VBA
            // (Sgn returns Variant/Integer) and in .NET (Math.Sign returns int). Going through UnaryDouble
            // widened that int straight back to a double, which only showed once EF projected the value
            // instead of comparing it - GetInt32 on a boxed Double throws.
            "SGN" => Convert1(f, v => Math.Sign(Dbl(ConversionNumber(v)))),
            // The standard SQL math functions, LibRed extensions (ACE has none of them). The ones Access has under
            // another name are that function - Floor is Int, Sqrt is Sqr, Ln is Log, Atan is Atn, Sign is Sgn, and
            // Power is the ^ operator - so they read their arguments and fail as it does.
            "FLOOR" => Numeric1(f, Math.Floor, Math.Floor, keepsDate: true),
            "CEILING" or "CEIL" => Numeric1(f, Math.Ceiling, Math.Ceiling, keepsDate: true),
            "SIGN" => Convert1(f, v => Math.Sign(Dbl(ConversionNumber(v)))),
            "SQRT" => UnaryDouble(f, Math.Sqrt),
            "LN" => UnaryDouble(f, Math.Log),
            "LOG10" => UnaryDouble(f, Math.Log10),
            "POWER" => EvaluatePower(f.Arguments[0], f.Arguments[1]),
            "ASIN" => UnaryDouble(f, Math.Asin),
            "ACOS" => UnaryDouble(f, Math.Acos),
            "ATAN" => UnaryDouble(f, Math.Atan),
            "ATAN2" => BinaryDouble(f, Math.Atan2),
            "SINH" => UnaryDouble(f, Math.Sinh),
            "COSH" => UnaryDouble(f, Math.Cosh),
            "TANH" => UnaryDouble(f, Math.Tanh),
            "DEGREES" => UnaryDouble(f, x => x * 180 / Math.PI),
            "RADIANS" => UnaryDouble(f, x => x * Math.PI / 180),
            "PI" => Math.PI,

            // More VBA/Access built-ins (verified vs ACE via the function-whitelist sweep). All NULL-propagating
            // via Convert1 unless noted; positions are 1-based.
            // Asc and Chr work in the system ANSI code page (Chr takes 0-255; Chr(128) is '€', Asc('Ā') is 65 by
            // best fit); AscW and ChrW in UTF-16 code units, AscW signed and ChrW taking -32768 to 65535. The
            // machine's code page, NOT the database's: in a 1251, 1253 or 932 database ACE on a 1252 machine still
            // gives Chr(192) = 'À', and Asc of a Cyrillic or Greek letter is 63.
            "ASC" => Convert1(f, v => (int)Ansi.GetBytes(FirstCharacter(v))[0]),
            "CHR" => Convert1(f, v => AnsiCharacter(InRange(AsLong(v), 0, 255)).ToString()),
            "SPACE" => Convert1(f, v => new string(' ', Count(v))),
            "STRING" => StringOf(f),                         // String(count, char) → char repeated count times
            "STRREVERSE" => Convert1(f, v => new string(StringArgument(v).Reverse().ToArray())),
            "STRCOMP" => StrComp(f),                         // -1/0/1 (case-insensitive, Access "Compare Database")
            "STR" => Convert1(f, VbaStr),                    // number → text with a leading space when non-negative
            "VAL" => Convert1(f, v => VbaVal(StringArgument(v))),
            "HEX" => Convert1(f, v => RadixText(v, 16)),
            "OCT" => Convert1(f, v => RadixText(v, 8)),
            "INSTRREV" => InstrRev(f),                       // last occurrence, 1-based (0 if none)
            // MonthName(month, [abbreviate]). The second argument was accepted by the arity table and then
            // ignored by Convert1, so MonthName(1, True) returned "January" where ACE returns "Jan" — a
            // silently wrong value, and the arity check that would have caught a stray argument is what let
            // it through. WeekdayName next to it always honoured its own.
            "MONTHNAME" => MonthNameOf(f),
            "TIMER" => (float)(DateTime.Now - DateTime.Today).TotalSeconds,
            "RND" => Rnd(f),
            // Predicates / type inspection. IsError is always false — LibRed has no error-value type — but its argument
            // is still evaluated, so an error in it is raised (verified vs ACE: IsError(1/0) fails). IsEmpty, IsMissing
            // and IsObject are always false too, Null included: no query value is Empty, a missing argument or an
            // object (verified vs ACE). Each is a VBA Boolean (VbaBoolean), as ACE gives it.
            "ISNULL" => VbaBoolean.Of(Evaluate(f.Arguments[0]) is null),
            "ISNUMERIC" => VbaBoolean.Of(IsNumericValue(Evaluate(f.Arguments[0]))),
            "ISERROR" or "ISEMPTY" or "ISMISSING" or "ISOBJECT" => VbaBoolean.Of(IsError(f)),
            // IMEStatus() is the input-method mode, and ACE answers 0 (vbIMENoOp), a Long (verified).
            "IMESTATUS" => 0,
            "TYPENAME" => TypeNameOf(Evaluate(f.Arguments[0]), f.Arguments[0]),
            "VARTYPE" => VarTypeOf(Evaluate(f.Arguments[0]), f.Arguments[0]),
            "STRCONV" => StrConv(f),
            "WEEKDAYNAME" => WeekdayNameOf(f),
            "PARTITION" => PartitionOf(f),
            "FORMAT" => FormatValue(f),
            "FORMATCURRENCY" => FormatStyled(f, NumberStyle.Currency),
            "FORMATNUMBER" => FormatStyled(f, NumberStyle.Number),
            "FORMATPERCENT" => FormatStyled(f, NumberStyle.Percent),
            "FORMATDATETIME" => FormatDateTime(f),
            "RGB" => Rgb(f),
            "QBCOLOR" => Convert1(f, v => QbColors[Setting(v, 0, 15)]),
            // Financial functions (verified vs ACE). rate is per period; pv/fv/pmt sign conventions follow VBA.
            "PMT" => Financial(f, a => Pmt(a[0], a[1], a[2], a[3], Due(a[4]))),
            "FV" => Financial(f, a => Fv(a[0], a[1], a[2], a[3], Due(a[4]))),
            "PV" => Financial(f, a => Pv(a[0], a[1], a[2], a[3], Due(a[4]))),
            "NPER" => Financial(f, a => NPer(a[0], a[1], a[2], a[3], Due(a[4]))),
            "IPMT" => Financial(f, a => IPmt(a[0], a[1], a[2], a[3], a[4], Due(a[5]))),
            "PPMT" => Financial(f, a => PPmt(a[0], a[1], a[2], a[3], a[4], Due(a[5]))),
            "RATE" => Financial(f, a => Rate(a[0], a[1], a[2], a[3], Due(a[4]), f.Arguments.Count > 5 ? a[5] : 0.1)),
            "SLN" => Financial(f, a => Sln(a[0], a[1], a[2])),
            "SYD" => Financial(f, a => Syd(a[0], a[1], a[2], a[3])),
            "DDB" => Financial(f, a => Ddb(a[0], a[1], a[2], a[3], f.Arguments.Count > 4 ? a[4] : 2)),

            "ASCW" => Convert1(f, v => (int)(short)FirstCharacter(v)[0]),
            "CHRW" => Convert1(f, v => ((char)(InRange(AsLong(v), -32768, 65535) & 0xFFFF)).ToString()),
            // Byte variants operate on the UTF-16 byte layout (2 bytes/char): LenB = 2×length, AscB = the low
            // byte of the first char, and Left/Right/Mid/InStr count bytes. Verified vs ACE (LenB('abc')=6,
            // InStrB(1,'abc','b')=3). ChrB is intentionally absent — ACE's expression service has no ChrB.
            "ASCB" => Convert1(f, v => (int)ToBytes(FirstCharacter(v))[0]),
            "LENB" => Convert1(f, v => ToBytes(v).Length),
            "DATALENGTH" => Convert1(f, v => DataLength(v, IsCurrency(f.Arguments[0]))),
            "LEFTB" => ByteLeft(f),
            "RIGHTB" => ByteRight(f),
            "MIDB" => ByteMid(f),
            "INSTRB" => InstrB(f),

            // GenUniqueID(): Access's random-Long generator. Not callable in a SELECT (ACE errors "Undefined
            // function") but valid as a LONG column's DEFAULT, where it yields a random signed Int32 per row —
            // the mechanism behind a "Random" AutoNumber. Accepted on a plain LONG default too (ACE allows it
            // only on a LONG column). AutoNumber columns take their random value in the row inserter instead.
            "GENUNIQUEID" => RandomLong(),
            // GenGUID(): Access's GUID generator, the sibling of GenUniqueID(). Same shape — ACE errors
            // "Undefined function 'GenGUID' in expression" in a SELECT, but it is valid as a GUID column's
            // DEFAULT, where it yields a fresh Guid per row (verified vs ACE). EF Core models it as
            // HasDefaultValueSql("GenGUID()") for store-generated Guid keys.
            "GENGUID" => Guid.NewGuid(),
            _ => throw new NotSupportedException($"Function {f.Name} is not supported."),
        };
    }

    /// <summary>Access <c>Choose(index, choice-1, choice-2, …)</c> (verified vs ACE): the choice at the index, read as
    /// a number and truncated (1.5 and 1.9 are 1, True is -1), or Null when there is no such choice. Every choice is
    /// evaluated, so an error in any of them is raised. A Null index is a type mismatch.</summary>
    private object? Choose(Call f)
    {
        object? indexValue = Evaluate(f.Arguments[0]);
        if (indexValue is null)
            throw new InvalidOperationException("Data type mismatch in criteria expression: Choose() index is null.");
        double index = Math.Truncate(Dbl(ConversionNumber(indexValue)));
        object?[] choices = f.Arguments.Skip(1).Select(Evaluate).ToArray();
        return index < 1 || index > choices.Length ? null : choices[(int)index - 1];
    }

    /// <summary>
    /// <c>NULLIF(a, b)</c>: NULL when the two are equal, otherwise <c>a</c>.
    /// </summary>
    /// <remarks>
    /// A deliberate divergence from ACE, which has no such function — it answers "Undefined function 'NULLIF'
    /// in expression" (verified). Access's own spelling of this is <c>IIF(a = b, NULL, a)</c>, and that is what
    /// this evaluates to; the difference is only that LibRed also accepts the name EF Core emits, so a query
    /// using it runs here rather than failing at the engine.
    ///
    /// Equality follows the same comparison as the <c>=</c> operator, which makes the NULL cases fall out
    /// correctly without special-casing: comparing with a NULL is unknown rather than equal, so
    /// <c>NULLIF(x, NULL)</c> is <c>x</c> and <c>NULLIF(NULL, y)</c> is NULL — matching the IIF form, where an
    /// unknown condition takes the false branch.
    ///
    /// Unlike the IIF spelling, <c>a</c> is evaluated once.
    /// </remarks>
    private object? NullIf(Call f)
    {
        object? left = Evaluate(f.Arguments[0]);
        if (left is null) return null;

        object? right = Evaluate(f.Arguments[1]);
        return right is not null && Compare(left, right, Text) == 0 ? null : left;
    }

    /// <summary>
    /// <c>COALESCE(a, b, …)</c> — the arguments in order, and the value of the first that is not NULL; NULL
    /// when every one of them is. Access/ACE has no COALESCE (its nearest equivalent is <c>Nz</c>, which takes
    /// only two), so this is reachable from LibRed's extended SQL mode and from hand-written SQL.
    /// </summary>
    /// <remarks>
    /// The standard defines COALESCE as shorthand for
    /// <c>CASE WHEN a IS NOT NULL THEN a WHEN b IS NOT NULL THEN b … END</c>, and SQL Server implements it by
    /// literally rewriting to that — which is why its docs warn that arguments are evaluated more than once
    /// and a subquery argument can yield different values between evaluations. Evaluating each argument once
    /// here gives the same answer with none of that, so the shorthand is honoured without inheriting the
    /// rewrite's cost or its instability.
    /// </remarks>
    private object? Coalesce(Call f)
    {
        foreach (TExpression argument in f.Arguments)
        {
            object? value = Evaluate(argument);
            if (value is not null)
                return value;
        }

        return null;
    }

    /// <summary>
    /// Access's <c>Nz(value [, valueIfNull])</c>: <c>value</c>, or when it is Null <c>valueIfNull</c>, or VBA's
    /// <see cref="VbaEmpty"/> when there is none. ACE's expression service has no Nz — it is the Access application's,
    /// so it runs in queries opened in Access but not over OLE DB — and it is here for the queries written in Access.
    /// </summary>
    /// <remarks>
    /// In Access the result is a Variant, and LibRed makes it one (QueryExecutor.VarianceOf), with what that brings
    /// (verified vs Access): it is written out as text — <c>Nz(K, 0)</c> is <c>"0"</c> and <c>Nz(Null, 5)</c> is
    /// <c>"5"</c> — and sorts and groups as its text, so <c>ORDER BY Nz(K, 0)</c> puts 10 before 2; as an operand it
    /// keeps its own value, so <c>Nz(K, 0) + 1</c> adds and <c>Nz(K, 0) &gt; 2</c> compares as a number.
    /// </remarks>
    private object? Nz(Call f) =>
        Evaluate(f.Arguments[0]) ?? (f.Arguments.Count == 2 ? Evaluate(f.Arguments[1]) : VbaEmpty.Value);

    /// <summary>
    /// <c>GREATEST(a, b, …)</c> and <c>LEAST(a, b, …)</c> — the largest or smallest of the arguments, compared
    /// as the <c>&lt;</c> and <c>&gt;</c> operators compare. Access/ACE has neither, so like COALESCE they are
    /// reachable from LibRed's extended SQL mode and from hand-written SQL.
    /// </summary>
    /// <remarks>
    /// NULL arguments are ignored and the answer is NULL only when every argument is NULL — SQL Server's and
    /// PostgreSQL's rule, and the one EF Core translates <c>Math.Max</c>/<c>Math.Min</c> and a <c>Max()</c>/
    /// <c>Min()</c> over an inline collection against. (MySQL and Oracle instead return NULL when any argument
    /// is NULL.) Every argument is evaluated, each once. Of equal values the first is returned.
    /// </remarks>
    private object? Extreme(Call f, bool greatest)
    {
        object? result = null;
        foreach (TExpression argument in f.Arguments)
        {
            object? value = Evaluate(argument);
            if (value is null)
                continue;
            if (result is null || (greatest ? Compare(value, result, Text) > 0 : Compare(value, result, Text) < 0))
                result = value;
        }

        return result;
    }

    /// <summary>Access <c>Switch(cond-1, value-1, cond-2, value-2, …)</c> (verified vs ACE): the value paired with the
    /// first true condition, or Null if none is. A condition is read as CBool reads it, so 'False' is False and text
    /// that is neither a Boolean nor a number is a type mismatch; Null is False. Every argument is evaluated, so an
    /// error in any of them is raised. An odd number of arguments is an error ("Wrong number of arguments").</summary>
    private object? Switch(Call f)
    {
        if (f.Arguments.Count % 2 != 0)
            throw new InvalidOperationException(
                "Wrong number of arguments used with function Switch (expects condition/value pairs).");
        object?[] values = f.Arguments.Select(Evaluate).ToArray();
        for (int i = 0; i < values.Length; i += 2)
            if (values[i] is { } condition && VbaBool(condition))
                return values[i + 1];
        return null;
    }

    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    /// <summary>The system ANSI code page, which Asc and Chr work in; unmappable characters take their best fit.</summary>
    private static Encoding Ansi =>
        CodePagesEncodingProvider.Instance.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage) ?? Encoding.Latin1;

    private static char AnsiCharacter(int code) => Ansi.GetString([(byte)code])[0];

    /// <summary>The first character of a value as a String argument takes it (<see cref="StringArgument"/>); an empty
    /// text is an invalid procedure call.</summary>
    private static string FirstCharacter(object v)
    {
        string text = StringArgument(v);
        return text.Length > 0
            ? text[..1]
            : throw new ArgumentException("Invalid procedure call: the text is empty.");
    }

    /// <summary>A value read as CByte reads it, and as a Byte stores it: as <see cref="AsInteger"/> reads one, past a Byte
    /// an overflow, and a VBA Boolean True 255 where any other -1 overflows (verified vs ACE: <c>CByte(IsNull(Null))</c>
    /// and a calculated Byte column over <c>IsNull</c> or <c>Like</c> are 255).</summary>
    internal static byte AsByte(object v) =>
        v is VbaBoolean b ? (byte)(b.Value ? 255 : 0) : Convert.ToByte(Dbl(ConversionNumber(v)));

    /// <summary>A value read as CInt reads it, and as an Integer stores it: through a Double, half to even, and past an
    /// Integer an overflow (verified vs ACE: <c>CInt(0.5000000000000000000000000001)</c> and an Integer calculated column
    /// over it are 0, as the Double 0.5 rounds).</summary>
    internal static int AsInteger(object v) => Convert.ToInt16(Dbl(ConversionNumber(v)));

    /// <summary>A value read as CLng reads it, and as a Long stores it: as <see cref="AsInteger"/> reads one, past a Long
    /// an overflow (verified vs ACE: <c>CLng(2.5000000000000000000000000001)</c> and a Long calculated column over it are
    /// 2).</summary>
    internal static int AsLong(object v) => Convert.ToInt32(Dbl(ConversionNumber(v)));

    /// <summary>A value read as CLngLng reads it: as CLng, half to even, and past an Int64 an overflow. Text that
    /// reads as a number is read exactly, not through a Double, so '9223372036854775807' loses no digit.</summary>
    private static long AsLongLong(object v) =>
        v is string or char && VbaText.TextAsDecimal(v.ToString()!) is decimal exact
            ? Convert.ToInt64(exact, CultureInfo.InvariantCulture)
            : Lng(ConversionNumber(v));

    /// <summary>A whole-number argument, where outside <paramref name="least"/>-<paramref name="most"/> is an invalid
    /// procedure call.</summary>
    private static int InRange(int n, int least, int most = int.MaxValue) =>
        n >= least && n <= most ? n : throw new ArgumentException($"Invalid procedure call: {n} is out of range.");

    /// <summary>A count, length or position read as CLng reads it; below <paramref name="least"/> is an invalid
    /// procedure call.</summary>
    private static int Count(object v, int least = 0) => InRange(AsLong(v), least);

    /// <summary>
    /// A function's argument at <paramref name="index"/>, read by <paramref name="read"/>: <paramref name="absent"/>
    /// when the call leaves it out, and null — the call gives Null — when it is Null.
    /// </summary>
    private T? Optional<T>(Call f, int index, T absent, Func<object, T> read) where T : struct =>
        f.Arguments.Count <= index ? absent
        : Evaluate(f.Arguments[index]) is { } v ? read(v)
        : null;

    /// <summary>A count, length or position argument (<see cref="Count"/>), or null when the argument is Null.</summary>
    private int? CountArgument(Call f, int index, int least = 0, int absent = 0) =>
        Optional(f, index, absent, v => Count(v, least));

    /// <summary>
    /// The locale ID InStr, InStrRev or Replace searches under, 0 for a binary search: its evaluated compare
    /// argument, or when the argument is <paramref name="absent"/> the database's locale ID, as
    /// <see cref="TextFold.Locale(int, TextFold.Search)"/> reads it. Null when the argument is Null.
    /// </summary>
    private int? SearchLocale(object? compare, bool absent, TextFold.Search function) =>
        absent ? TextFold.Locale(Text.Collation.Lcid, function, database: true)
        : compare is { } given ? TextFold.Locale(AsLong(given), function)
        : null;

    /// <summary>
    /// Whether a compare argument asks for a binary comparison: 0 is binary, and 1 (the default when the argument is
    /// absent) or a locale ID is textual. Anything else is an invalid procedure call — the documented
    /// vbUseCompareOption (-1) and database comparison (2) included (verified vs ACE: 1031, 1033 and the neutral IDs
    /// 3 and 4 are accepted, 20000 is not). Null when the argument is Null.
    /// <para>StrComp is its one reader, and compares textually in the database order whatever locale ID it is given.
    /// InStr, InStrRev and Replace read theirs with <see cref="SearchLocale"/>.</para>
    /// </summary>
    private bool? BinaryCompare(Call f, int index) =>
        Optional(f, index, false, v => AsLong(v) switch
        {
            0 => true,
            1 => false,
            var mode when mode > 2 && IsLocaleId(mode) => false,
            var mode => throw new ArgumentException($"Invalid procedure call: {mode} is not a comparison."),
        });

    private static bool IsLocaleId(int lcid)
    {
        try
        {
            return lcid > 0 && CultureInfo.GetCultureInfo(lcid).LCID == lcid;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Access <c>String(count, character)</c>: the character repeated. A text gives its first character (an empty
    /// one is an invalid procedure call); a number is a character code in the ANSI code page, taken modulo 256 (verified
    /// vs ACE: String(3, 321) is 'AAA', String(3, True) 'ÿÿÿ').
    /// </summary>
    private string? StringOf(Call f)
    {
        if (CountArgument(f, 0) is not { } count || Evaluate(f.Arguments[1]) is not { } charValue)
            return null;
        char ch = charValue is string or Guid or byte[]? FirstCharacter(charValue)[0]
            : AnsiCharacter(AsLong(charValue) & 0xFF);
        return new string(ch, count);
    }

    /// <summary>
    /// Access <c>StrComp(a, b, [compare])</c>: -1, 0 or 1. A binary comparison orders the UTF-16 code units; a textual
    /// one the database sort order, with trailing spaces breaking a tie (verified vs ACE: StrComp('ß', 'ss') is 0,
    /// StrComp('a-b', 'ab') 1, StrComp('a', 'a ') -1).
    /// </summary>
    private int? StrComp(Call f)
    {
        object? a = Evaluate(f.Arguments[0]);
        object? b = Evaluate(f.Arguments[1]);
        if (a is null || b is null || BinaryCompare(f, 2) is not { } binary)
            return null;
        string left = ConcatText(a), right = ConcatText(b);
        if (binary)
            return Math.Sign(string.CompareOrdinal(left, right));
        int order = Text.Compare(left, right);
        return order != 0 ? order : Math.Sign(TrailingSpaces(left) - TrailingSpaces(right));

        static int TrailingSpaces(string s) => s.Length - s.TrimEnd(' ').Length;
    }

    /// <summary>Access <c>InStrRev(string1, string2, [start=-1], [compare])</c>: the 1-based position of the last
    /// occurrence of string2 in string1 (0 if not found), searching within the first <c>start</c> characters
    /// (the match must end at or before <c>start</c>; <c>start</c>=-1 means the whole string). The text is searched
    /// as <see cref="TextFold.Matches"/> searches it, under the compare argument's locale
    /// (<see cref="SearchLocale"/>). Semantics verified vs ACE, including its quirks: an empty needle returns the
    /// effective start position; <c>start</c>=0 (or &lt;-1) → "Invalid procedure call", and one past the end of
    /// string1 gives 0; and — unlike <c>InStr</c> — a NULL string raises "Data type mismatch" rather than propagating
    /// NULL.</summary>
    private int? InstrRev(Call f)
    {
        object? s1v = Evaluate(f.Arguments[0]);
        object? s2v = Evaluate(f.Arguments[1]);
        if (s1v is null || s2v is null)
            throw new InvalidOperationException("Data type mismatch in criteria expression: InStrRev() argument is null.");
        string s1 = StringArgument(s1v), s2 = StringArgument(s2v);

        if (CountArgument(f, 2, least: -1, absent: -1) is not { } given)
            return null;
        if (given == 0)
            throw new ArgumentException("Invalid procedure call: InStrRev() start must be -1 or a positive position.");
        // Read before anything is answered: a compare argument that is not a comparison is an invalid procedure
        // call even where the answer is 0 without searching (verified vs ACE). A Null one gives Null, where ACE
        // raises "Data type mismatch".
        if (SearchLocale(f.Arguments.Count > 3 ? Evaluate(f.Arguments[3]) : null, f.Arguments.Count <= 3,
                TextFold.Search.InStrRev) is not { } locale)
            return null;
        if (given > s1.Length)
            return 0;
        int start = given > 0 ? given : s1.Length;

        if (s1.Length == 0) return 0;
        string window = s1[..start];                        // search within Left(string1, start)
        if (s2.Length == 0) return start;                   // empty needle → the effective start position
        int last = -1;
        foreach ((int index, int _) in TextFold.Matches(window, s2, locale, TextFold.Search.InStrRev))
            last = index;
        return last + 1;
    }

    /// <summary>
    /// Access <c>Str(number)</c>: the number read as the conversion functions read it and written as
    /// <see cref="VbaText.Str"/> writes it. A date is written as CStr writes it.
    /// </summary>
    private static string VbaStr(object v) => v is DateTime or VbaBoolean ? ConcatText(v) : VbaText.Str(NumericOperand(v)!);

    /// <summary>
    /// Access <c>Val(string)</c> (verified vs ACE): the number at the start of the text, as a Double, or 0. Spaces,
    /// tabs, carriage returns and line feeds are removed first, wherever they are (other whitespace is not). The
    /// number may have a sign, a decimal point and an <c>e</c> or <c>d</c> exponent. <c>&amp;H</c> hex and
    /// <c>&amp;O</c> octal take no sign and keep their low 32 bits, read as an Integer when they fit 16 bits
    /// (<c>&amp;HFFFF</c> is -1) and otherwise as a Long. A number past a Double is an overflow.
    /// </summary>
    private static double VbaVal(string s)
    {
        string t = string.Concat(s.Where(c => c is not (' ' or '\t' or '\r' or '\n')));
        if (t.Length > 1 && t[0] == '&' && (char.ToUpperInvariant(t[1]) is var prefix && prefix is 'H' or 'O'))
        {
            int radix = prefix == 'H' ? 16 : 8;
            ulong bits = 0;
            foreach (char c in t[2..])
            {
                int digit = char.IsAsciiDigit(c) ? c - '0' : char.IsAsciiHexDigit(c) ? char.ToUpperInvariant(c) - 'A' + 10 : radix;
                if (digit >= radix)
                    break;
                bits = unchecked(bits * (ulong)radix + (ulong)digit);
            }
            bits &= 0xFFFF_FFFF;
            return bits <= 0xFFFF ? (short)bits : (int)bits;
        }

        Match number = Regex.Match(t, @"^[+-]?(\d+\.?\d*|\.\d+)([eEdD][+-]?\d+)?");
        if (!number.Success)
            return 0;
        double value = double.Parse(number.Value.Replace('d', 'e').Replace('D', 'e'), NumberStyles.Float, CultureInfo.InvariantCulture);
        return double.IsFinite(value) ? value : throw new OverflowException($"Overflow: '{s}' is too large for a number.");
    }

    /// <summary>
    /// Access <c>Hex</c> and <c>Oct</c> (verified vs ACE): the bits of the number read as the conversion functions
    /// read it — 16 of them for an Integer or a Boolean (<c>Hex(True)</c> is FFFF), 32 for a Long, and otherwise the
    /// value rounded half to even as a 64-bit whole number (<c>Hex(-1.5)</c> is FFFFFFFFFFFFFFFE).
    /// </summary>
    private static string RadixText(object v, int radix)
    {
        long bits = NumericOperand(v)! switch
        {
            bool b => b ? 0xFFFF : 0,
            short s => (ushort)s,
            int i => (uint)i,
            var n => Lng(Serial(n)),
        };
        return Convert.ToString(bits, radix).ToUpperInvariant();
    }

    private bool IsError(Call f)
    {
        Evaluate(f.Arguments[0]);
        return false;
    }

    /// <summary>Access <c>IsNumeric(value)</c> (verified vs ACE): true for a number or a Boolean, and for text that
    /// reads as a number the way <c>+</c> reads it ('$5', '&amp;HFF', '(1)' and '1d2' do; '1e400' does not). A date, a
    /// GUID, a binary value and Null are not numeric.</summary>
    private static bool IsNumericValue(object? v) => v switch
    {
        null or DateTime or Guid or byte[] => false,
        string or char => VbaText.TryTextAsNumber(v.ToString()!) is not null,
        _ => true,
    };

    /// <summary>VBA <c>TypeName(value)</c> — the Access type name of the value's type (e.g. an Int32 literal → "Long").
    /// A Boolean is "Boolean" and a Byte "Byte", as LibRed holds them; ACE reports both as integers. Currency and
    /// Decimal share <see cref="decimal"/>, so which one a value is comes from its <paramref name="expression"/>.</summary>
    private string TypeNameOf(object? v, TExpression expression) => VbaBoolean.Plain(v) switch
    {
        decimal when IsCurrency(expression) => "Currency",
        decimal => "Decimal",
        null => "Null",
        bool => "Boolean",
        byte => "Byte",
        short => "Integer",
        int => "Long",
        long or ulong => "LongLong",
        float => "Single",
        double => "Double",
        DateTime => "Date",
        string => "String",
        // A binary column reports as String, not Byte[] — ACE's expression service sees the value as a
        // UTF-16 string (VarType 8 = VT_BSTR), so TypeName must say so even though LibRed holds a byte[].
        byte[] or Guid or char => "String",
        var other => other.GetType().Name,
    };

    /// <summary>VBA <c>VarType(value)</c> — the Access variant type code (vbLong=3, vbString=8, …), with the same
    /// mapping as <see cref="TypeNameOf"/>.</summary>
    private int VarTypeOf(object? v, TExpression expression) => VbaBoolean.Plain(v) switch
    {
        decimal when IsCurrency(expression) => 6, // vbCurrency
        decimal => 14,      // vbDecimal
        null => 1,          // vbNull
        bool => 11,         // vbBoolean
        byte => 17,         // vbByte
        short => 2,         // vbInteger
        int => 3,           // vbLong
        long or ulong => 20, // vbLongLong
        float => 4,         // vbSingle
        double => 5,        // vbDouble
        DateTime => 7,      // vbDate
        _ => 8,             // vbString
    };

    /// <summary>
    /// Access <c>StrConv(string, conversion, [LCID])</c> (verified vs ACE). 0 leaves the text as it is. 1, 2 and 3
    /// work in the ANSI code page, so a character it lacks becomes its best fit or '?': 1 is upper case, 2 lower case,
    /// and 3 upper-cases the first letter and each letter after a space, tab, line break, form feed or NUL and
    /// lower-cases the rest. 64 (vbUnicode) reads the text's UTF-16 bytes as ANSI characters, and 128 (vbFromUnicode)
    /// packs the text's ANSI bytes two to a character. The East Asian conversions (4 to 32) and other combinations are
    /// an invalid procedure call, but give Null for Null text; a conversion outside 0-255, or an LCID that is not a
    /// locale, is an invalid procedure call even then. ACE keeps an odd trailing byte from 128 inside an expression
    /// (<c>LenB(StrConv("abc", 128))</c> is 3); LibRed text has no odd bytes, so it drops it.
    /// </summary>
    private string? StrConv(Call f)
    {
        object? sv = Evaluate(f.Arguments[0]);
        if (Evaluate(f.Arguments[1]) is not { } modeV)
            return null;
        int mode = InRange(AsLong(modeV), 0, 255);
        if (Optional(f, 2, 0, AsLong) is not { } locale)
            return null;
        // A locale ID as InStr's compare argument takes one (verified vs ACE: 140, 1024, 1164 and 0x2000 are locales
        // to StrConv too, though the runtime does not list them).
        if (locale != 0 && !TextFold.IsLocale(locale))
            throw new ArgumentException($"Invalid procedure call: {locale} is not a locale.");
        if (sv is null)
            return null;
        if (mode is not (0 or 1 or 2 or 3 or 64 or 128))
            throw new ArgumentException($"Invalid procedure call: {mode} is not a StrConv conversion.");

        // vbUnicode (64) on binary widens each byte to one Unicode char — Jet's binary→string conversion.
        // This is the byte-array path EF emits for `byte[].Contains(x)`: INSTR(1, STRCONV(arr, 64), 0xXX, 0).
        if (mode == 64 && sv is byte[] binary)
            return ByteArrayToString(binary);

        string s = ConcatText(sv);
        Encoding ansi = Ansi;
        return mode switch
        {
            0 => s,
            1 => ansi.GetString(ansi.GetBytes(s)).ToUpperInvariant(),
            2 => ansi.GetString(ansi.GetBytes(s)).ToLowerInvariant(),
            3 => ProperCase(ansi.GetString(ansi.GetBytes(s))),
            64 => ansi.GetString(Encoding.Unicode.GetBytes(s)),
            _ => FromBytes(ansi.GetBytes(s)),
        };
    }

    private static string ProperCase(string s)
    {
        var chars = new char[s.Length];
        bool wordStart = true;
        for (int i = 0; i < s.Length; i++)
        {
            chars[i] = wordStart ? char.ToUpperInvariant(s[i]) : char.ToLowerInvariant(s[i]);
            wordStart = s[i] is ' ' or '\t' or '\n' or '\v' or '\f' or '\r' or '\0';
        }
        return new string(chars);
    }

    /// <summary>Jet coerces a binary value to a string by mapping each byte to a single char (the value it
    /// widens back to via <c>STRCONV(…, 64)</c>). A <c>byte[]</c> reaching a string function — e.g. a
    /// <c>0xNN</c> hex literal used as an <c>INSTR</c> needle — is coerced this way, not via
    /// <c>ToString()</c> (which would yield "System.Byte[]").</summary>
    private static string ByteArrayToString(byte[] bytes) => new(Array.ConvertAll(bytes, b => (char)b));

    private static string ToJetString(object value) => value is byte[] b ? ByteArrayToString(b) : ConcatText(value);

    /// <summary>Access <c>MonthName(month, [abbreviate])</c>: the English month name, abbreviated when asked. A month
    /// outside 1-12 is an invalid procedure call (verified vs ACE, True included).</summary>
    private string? MonthNameOf(Call f)
    {
        if (Evaluate(f.Arguments[0]) is not { } month || Abbreviate(f) is not { } abbreviate)
            return null;
        int number = Setting(month, 1, 12);
        return abbreviate
            ? EnUs.DateTimeFormat.GetAbbreviatedMonthName(number)
            : EnUs.DateTimeFormat.GetMonthName(number);
    }

    /// <summary>Access <c>WeekdayName(weekday, [abbreviate], [firstdayofweek])</c>: the English name of the day at
    /// 1-based position <c>weekday</c> in a week starting on <c>firstdayofweek</c>, which defaults to the system's
    /// first day (verified vs ACE: WeekdayName(1) is Monday under en-AU). A weekday outside 1-7 is an invalid
    /// procedure call.</summary>
    private string? WeekdayNameOf(Call f)
    {
        if (Evaluate(f.Arguments[0]) is not { } weekday || Abbreviate(f) is not { } abbreviate
            || FirstDayOfWeek(f, 2, absent: 0) is not { } first)
            return null;
        int index = ((int)first + Setting(weekday, 1, 7) - 1) % 7;
        return (abbreviate ? EnUs.DateTimeFormat.AbbreviatedDayNames : EnUs.DateTimeFormat.DayNames)[index];
    }

    /// <summary>MonthName's and WeekdayName's abbreviate argument, read as CBool reads it; false when absent.</summary>
    private bool? Abbreviate(Call f) => Optional(f, 1, false, VbaBool);

    /// <summary>Access <c>Partition(number, start, stop, interval)</c>: a <c>"lower:upper"</c> range label, both
    /// sides right-justified to a fixed width. Below the range → lower blank, upper = <c>start-1</c>; above →
    /// lower = <c>stop+1</c>, upper blank; otherwise the interval bucket (verified vs ACE). The arguments are read as
    /// the conversion functions read them (a date as its serial); a negative start, a stop not after the start or an
    /// interval below 1 is an invalid procedure call. NULL-propagating.</summary>
    private string? PartitionOf(Call f)
    {
        object?[] args = f.Arguments.Select(Evaluate).ToArray();
        if (args.Any(a => a is null)) return null;
        long number = Lng(ConversionNumber(args[0]!));
        long start = Lng(ConversionNumber(args[1]!));
        long stop = Lng(ConversionNumber(args[2]!));
        long interval = Lng(ConversionNumber(args[3]!));
        if (start < 0 || stop <= start || interval < 1)
            throw new ArgumentException("Invalid procedure call: Partition needs 0 <= start < stop and an interval of at least 1.");

        // Fixed field width = the widest boundary that can appear (the below/above sentinels).
        int width = Math.Max((start - 1).ToString(CultureInfo.InvariantCulture).Length,
                             (stop + 1).ToString(CultureInfo.InvariantCulture).Length);

        string lower, upper;
        if (number < start) { lower = ""; upper = (start - 1).ToString(CultureInfo.InvariantCulture); }
        else if (number > stop) { lower = (stop + 1).ToString(CultureInfo.InvariantCulture); upper = ""; }
        else
        {
            long lo = start + (number - start) / interval * interval;
            long hi = Math.Min(lo + interval - 1, stop);
            lower = lo.ToString(CultureInfo.InvariantCulture);
            upper = hi.ToString(CultureInfo.InvariantCulture);
        }
        return $"{lower.PadLeft(width)}:{upper.PadLeft(width)}";
    }

    /// <summary>VBA <c>LeftB(string, bytes)</c>: the leading <c>bytes</c> bytes of the UTF-16 layout, i.e. the
    /// first <c>bytes/2</c> characters. NULL-propagating.</summary>
    private object? ByteLeft(Call f)
    {
        object? sv = Evaluate(f.Arguments[0]);
        if (sv is null || CountArgument(f, 1) is not { } count) return null;
        byte[] b = ToBytes(sv);
        return ByteResult(sv, b[..Math.Min(count, b.Length)]);
    }

    /// <summary>VBA <c>RightB(string, bytes)</c>: the trailing <c>bytes</c> bytes. NULL-propagating.</summary>
    private object? ByteRight(Call f)
    {
        object? sv = Evaluate(f.Arguments[0]);
        if (sv is null || CountArgument(f, 1) is not { } count) return null;
        byte[] b = ToBytes(sv);
        return ByteResult(sv, b[^Math.Min(count, b.Length)..]);
    }

    /// <summary>VBA <c>MidB(string, startByte[, lenBytes])</c>: a 1-based **byte** slice (may start/end
    /// mid-character). NULL-propagating; a start below 1 or a negative length is an invalid procedure call.</summary>
    private object? ByteMid(Call f)
    {
        object? sv = Evaluate(f.Arguments[0]);
        if (sv is null || CountArgument(f, 1, least: 1) is not { } first
            || CountArgument(f, 2, absent: int.MaxValue) is not { } length)
            return null;
        byte[] b = ToBytes(sv);
        int start = first - 1;
        if (start >= b.Length) return ByteResult(sv, []);
        int len = Math.Min(length, b.Length - start);
        return ByteResult(sv, b[start..(start + len)]);
    }

    /// <summary>The result of a byte-slice function: a **byte[]** when the input was binary (so a further byte
    /// function like <c>ASCB(RIGHTB(x,1))</c> can read the raw byte — the mechanism EFCore.Jet's ByteArrayLength
    /// relies on), or the decoded string (dropping a trailing odd byte) when the input was text.</summary>
    private static object ByteResult(object input, byte[] slice) => input is byte[]? slice : FromBytes(slice);

    /// <summary>VBA <c>InStrB([start,] string1, string2)</c>: the 1-based **byte** position of string2's bytes in
    /// string1's bytes (0 if not found). NULL-propagating.</summary>
    private int? InstrB(Call f)
    {
        int argc = f.Arguments.Count;
        object? s1v = Evaluate(f.Arguments[argc >= 3 ? 1 : 0]);
        object? s2v = Evaluate(f.Arguments[argc >= 3 ? 2 : 1]);
        if (s1v is null || s2v is null || (argc >= 3 ? CountArgument(f, 0, least: 1) : 1) is not { } start)
            return null;
        int idx = IndexOfBytes(ToBytes(s1v), ToBytes(s2v), start - 1);
        return idx < 0 ? 0 : idx + 1;
    }

    private static int IndexOfBytes(byte[] hay, byte[] needle, int start)
    {
        if (needle.Length == 0) return start <= hay.Length ? start : -1;
        for (int i = Math.Max(0, start); i + needle.Length <= hay.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
                if (hay[i + j] != needle[j]) { match = false; break; }
            if (match) return i;
        }
        return -1;
    }

    /// <summary>The UTF-16LE **byte** representation a byte function operates on: a string's encoded bytes, or a
    /// binary value's bytes laid out as UTF-16 (<see cref="VbaText.Utf16Layout"/>).</summary>
    private static byte[] ToBytes(object v) =>
        v is byte[] bytes ? VbaText.Utf16Layout(bytes) : Encoding.Unicode.GetBytes(ConcatText(v));

    /// <summary>
    /// SQL Server's <c>DATALENGTH</c>, a LibRed extension (Access has none): the bytes a value takes as Access stores
    /// it. Text is two per character — Access text is UTF-16, as <c>nvarchar</c> is — with trailing spaces counted and
    /// no account of the on-disk Unicode compression; a binary value is its length, unpadded (LenB pads an odd one).
    /// A Byte is 1, an Integer 2, a Long and a Single 4, a Double, a Currency, a date and a BIGINT 8, a GUID 16 and a
    /// Decimal 17 — its sign byte and 16-byte magnitude. A Boolean is stored as a bit of the row's null bitmap and so
    /// takes no byte of its own; it counts 1, as SQL Server counts a <c>bit</c>.
    /// </summary>
    private static int DataLength(object value, bool currency) => VbaBoolean.Plain(value) switch
    {
        string s => checked(s.Length * 2),
        char => 2,
        byte[] bytes => bytes.Length,
        bool or byte or sbyte => 1,
        short or ushort => 2,
        int or uint or float => 4,
        long or ulong or double or DateTime => 8,
        decimal => currency ? 8 : 17,
        Guid => 16,
        _ => checked(ConcatText(value).Length * 2),
    };

    /// <summary>Successive byte pairs as UTF-16 code units (low byte first), dropping a trailing odd byte — matching
    /// ACE (MidB(x, 1, 3) yields one character from three bytes). A lone surrogate is kept as it is.</summary>
    private static string FromBytes(byte[] bytes)
    {
        var chars = new char[bytes.Length / 2];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = (char)(bytes[2 * i] | (bytes[2 * i + 1] << 8));
        return new string(chars);
    }

    // --- Colour functions ---

    // QBColor maps 0..15 to fixed BGR Long values (verified vs ACE: QBColor(4) = 128).
    private static readonly int[] QbColors =
    [
        0x000000, 0x800000, 0x008000, 0x808000, 0x000080, 0x800080, 0x008080, 0xC0C0C0,
        0x808080, 0xFF0000, 0x00FF00, 0xFFFF00, 0x0000FF, 0xFF00FF, 0x00FFFF, 0xFFFFFF,
    ];

    /// <summary>
    /// Access <c>RGB(red, green, blue)</c>: the colour as a Long, red in the low byte. NULL-propagating. Each part is a
    /// setting (<see cref="Setting"/>) where above 255 is taken as 255 (verified vs ACE: RGB(256, 0, 0) is 255, and
    /// RGB(-0.6, 0, 0) fails).
    /// </summary>
    private object? Rgb(Call f)
    {
        if (Evaluate(f.Arguments[0]) is not { } red || Evaluate(f.Arguments[1]) is not { } green
            || Evaluate(f.Arguments[2]) is not { } blue)
            return null;
        return Part(red) | Part(green) << 8 | Part(blue) << 16;

        static int Part(object v) => Math.Min(Setting(v, 0, short.MaxValue), 255);
    }

    // --- Financial functions ---

    /// <summary>
    /// A financial function (verified vs ACE, to the last bit). The arguments are read as the conversion functions read
    /// them (text as a number, a date as its serial, True as -1); an omitted one is 0, except Rate's guess and DDB's
    /// factor. A "type" argument means payment at the start of each period whenever it is not 0. Arguments out of range
    /// are an invalid procedure call; a result past a Double comes back as infinity or NaN, as ACE returns it. A Null
    /// argument gives Null where ACE raises an error.
    /// <para>The algorithms and the order of their arithmetic are the VBA runtime's, as Microsoft.VisualBasic's
    /// Financial module carries them; the order decides the last digit.</para>
    /// </summary>
    private double? Financial(Call f, Func<double[], double> compute)
    {
        var arguments = new double[6];
        for (int i = 0; i < f.Arguments.Count; i++)
        {
            if (Evaluate(f.Arguments[i]) is not { } value)
                return null;
            arguments[i] = Dbl(ConversionNumber(value));
        }
        return compute(arguments);
    }

    /// <summary>A "type" argument: payment at the start of each period when it rounds (half to even) to anything but 0
    /// (verified vs ACE: 0.5 is the end, 1.5, 2 and -1 the start).</summary>
    private static bool Due(double type) => Math.Round(type, MidpointRounding.ToEven) != 0;

    private static ArgumentException OutOfRange(string argument) =>
        new($"Invalid procedure call: {argument} is out of range.");

    /// <summary>Access <c>Pmt(rate, nper, pv, [fv], [type])</c>: the payment each period. An nper of 0 is out of range.</summary>
    private static double Pmt(double rate, double nper, double pv, double fv, bool due)
    {
        if (nper == 0)
            throw OutOfRange("NPer");
        if (rate == 0)
            return (-fv - pv) / nper;
        double start = due ? 1 + rate : 1;
        double growth = Math.Pow(rate + 1, nper);
        return (-fv - pv * growth) / (start * (growth - 1)) * rate;
    }

    /// <summary>Access <c>FV(rate, nper, pmt, [pv], [type])</c>: the value after the last payment.</summary>
    private static double Fv(double rate, double nper, double pmt, double pv, bool due)
    {
        if (rate == 0)
            return -pv - pmt * nper;
        double start = due ? 1 + rate : 1;
        double growth = Math.Pow(1 + rate, nper);
        return -(pv * growth + pmt * start * (growth - 1) / rate);
    }

    /// <summary>Access <c>PV(rate, nper, pmt, [fv], [type])</c>: the value before the first payment.</summary>
    private static double Pv(double rate, double nper, double pmt, double fv, bool due)
    {
        if (rate == 0)
            return -fv - pmt * nper;
        double start = due ? 1 + rate : 1;
        double growth = Math.Pow(1 + rate, nper);
        return -(fv + pmt * start * (growth - 1) / rate) / growth;
    }

    /// <summary>Access <c>NPer(rate, pmt, pv, [fv], [type])</c>: the number of payments. A rate of -1 or less, no
    /// payment at a rate of 0, or payments that never reach the future value are out of range.</summary>
    private static double NPer(double rate, double pmt, double pv, double fv, bool due)
    {
        if (rate <= -1)
            throw OutOfRange("Rate");
        if (rate == 0)
            return pmt == 0 ? throw OutOfRange("Pmt") : -(pv + fv) / pmt;
        double payment = due ? pmt * (1 + rate) / rate : pmt / rate;
        double future = -fv + payment, present = pv + payment;
        if (future < 0 && present < 0)
            (future, present) = (-future, -present);
        else if (future <= 0 || present <= 0)
            throw OutOfRange("Pmt");
        return (Math.Log(future) - Math.Log(present)) / Math.Log(rate + 1);
    }

    /// <summary>Access <c>IPmt(rate, per, nper, pv, [fv], [type])</c>: the interest in payment <c>per</c>, which must be
    /// above 0 and below nper + 1.</summary>
    private static double IPmt(double rate, double per, double nper, double pv, double fv, bool due)
    {
        if (per <= 0 || per >= nper + 1)
            throw OutOfRange("Per");
        if (due && per == 1)
            return 0;
        double pmt = Pmt(rate, nper, pv, fv, due);
        if (due)
            pv += pmt;
        return Fv(rate, per - (due ? 2 : 1), pmt, pv, false) * rate;
    }

    /// <summary>Access <c>PPmt(rate, per, nper, pv, [fv], [type])</c>: the principal in payment <c>per</c>.</summary>
    private static double PPmt(double rate, double per, double nper, double pv, double fv, bool due)
    {
        if (per <= 0 || per >= nper + 1)
            throw OutOfRange("Per");
        return Pmt(rate, nper, pv, fv, due) - IPmt(rate, per, nper, pv, fv, due);
    }

    /// <summary>
    /// Access <c>Rate(nper, pmt, pv, [fv], [type], [guess])</c>: the rate per period, found by the secant method from the
    /// guess (0.1 when omitted). An nper of 0 or less, or no answer within 40 steps, is out of range.
    /// </summary>
    private static double Rate(double nper, double pmt, double pv, double fv, bool due, double guess)
    {
        const double step = 0.00001, epsilon = 0.0000001;
        if (nper <= 0)
            throw OutOfRange("NPer");
        double Error(double rate)
        {
            if (rate == 0)
                return pv + pmt * nper + fv;
            double growth = Math.Pow(rate + 1, nper);
            double start = due ? 1 + rate : 1;
            return pv * growth + pmt * start * (growth - 1) / rate + fv;
        }

        double rate0 = guess, error0 = Error(rate0);
        double rate1 = error0 > 0 ? rate0 / 2 : rate0 * 2, error1 = Error(rate1);
        for (int i = 0; i < 40; i++)
        {
            if (error1 == error0)
            {
                rate0 = rate1 > rate0 ? rate0 - step : rate0 + step;
                error0 = Error(rate0);
                if (error1 == error0)
                    throw OutOfRange("Rate");
            }
            rate0 = rate1 - (rate1 - rate0) * error1 / (error1 - error0);
            error0 = Error(rate0);
            if (Math.Abs(error0) < epsilon)
                return rate0;
            (error0, error1) = (error1, error0);
            (rate0, rate1) = (rate1, rate0);
        }
        throw OutOfRange("Rate");
    }

    /// <summary>Access <c>SLN(cost, salvage, life)</c>: straight-line depreciation. A life of 0 is out of range.</summary>
    private static double Sln(double cost, double salvage, double life) =>
        life == 0 ? throw OutOfRange("Life") : (cost - salvage) / life;

    /// <summary>Access <c>SYD(cost, salvage, life, period)</c>: sum-of-years'-digits depreciation. A negative salvage, or a
    /// period of 0 or less or past the life, is out of range.</summary>
    private static double Syd(double cost, double salvage, double life, double period)
    {
        if (salvage < 0 || period > life || period <= 0)
            throw OutOfRange("Period");
        return (cost - salvage) * (life - period + 1) / (life * (life + 1) / 2);
    }

    /// <summary>
    /// Access <c>DDB(cost, salvage, life, period, [factor])</c>: declining-balance depreciation at <c>factor</c> (2 when
    /// omitted) over the life. A factor of 0 or less, a negative salvage, or a period of 0 or less or past the life is
    /// out of range. A cost of 0 or less depreciates nothing, and a period's depreciation never takes the value below
    /// the salvage.
    /// </summary>
    private static double Ddb(double cost, double salvage, double life, double period, double factor)
    {
        if (factor <= 0 || salvage < 0 || period <= 0 || period > life)
            throw OutOfRange("Period");
        if (cost <= 0)
            return 0;
        if (life < 2)
            return cost - salvage;
        if (life == 2)
            return period > 1 ? 0 : cost - salvage;
        if (period <= 1)
        {
            double first = cost * factor / life, most = cost - salvage;
            return first > most ? most : first;
        }
        double remaining = (life - factor) / life;
        double depreciation = factor * cost / life * Math.Pow(remaining, period - 1);
        double excess = cost * (1 - Math.Pow(remaining, period)) - cost + salvage;
        if (excess > 0)
            depreciation -= excess;
        return depreciation >= 0 ? depreciation : 0;
    }

    private uint _localRandSeed = 0x50000;   // used when no connection-scoped SessionState is available

    /// <summary>VBA <c>Rnd([number])</c> using VBA's own 24-bit LCG, so the sequence matches ACE (verified:
    /// <c>Rnd(-1)</c> = 0.2240070104598999). <c>number &gt; 0</c> (or omitted) advances the generator;
    /// <c>number = 0</c> repeats the last value; <c>number &lt; 0</c> reseeds deterministically from the
    /// argument's Single bit pattern. The seed is connection-scoped (via <see cref="SessionState"/>) since the
    /// JES has no <c>Randomize</c>. Result is a Single widened to Double, matching ACE.</summary>
    private object? Rnd(Call f)
    {
        uint seed = RandSeed ?? _localRandSeed;
        // The argument is read as a Single, as conversion functions read it; Null gives Null (ACE raises
        // "Invalid use of Null").
        if (Optional(f, 0, 1.0, a => Finite(Sng(ConversionNumber(a)))) is not { } arg)
            return null;
        if (arg != 0)
        {
            if (arg < 0)
            {
                uint ni = BitConverter.SingleToUInt32Bits((float)arg);
                seed = (ni + (ni >> 24)) & 0xFFFFFF;
            }
            seed = (uint)((seed * 0xFD43FDUL + 0xC39EC3UL) & 0xFFFFFF);
        }
        if (RandSeed is not null) RandSeed = seed; else _localRandSeed = seed;
        return (double)((float)seed / (float)0x1000000);
    }

    /// <summary>A random non-zero signed Int32 — Access's <c>GenUniqueID()</c>.</summary>
    private static int RandomLong()
    {
        int value;
        do { value = Random.Shared.Next(int.MinValue, int.MaxValue); } while (value == 0);
        return value;
    }

    /// <summary>
    /// Access <c>Round(number[, places])</c> (verified vs ACE): half to even on the number's decimal form — a Double or
    /// Single as OLE Automation writes it, so <c>Round(2.675, 2)</c> is 2.68 and <c>Round('2.55', 1)</c> 2.6 — and the
    /// number unchanged when nothing is cut. The operand's type is kept (the EF contract); text and dates give a
    /// Double and True is -1. Places are read as a number and
    /// rounded; Null places give Null, fewer than 0 are an invalid procedure call, and more than a Decimal holds leave
    /// the number as it is.
    /// </summary>
    private object? Round(Call f)
    {
        if (Evaluate(f.Arguments[0]) is not { } value || Optional(f, 1, 0, v => Count(v)) is not { } places)
            return null;
        places = Math.Min(places, 28);

        return VbaBoolean.Plain(value)! switch
        {
            decimal m => decimal.Round(m, places, MidpointRounding.ToEven),
            double d => RoundDouble(d, places),
            float s => RoundWritten(s, places, () => JetDecimalConverter.FromSingle(s), r => (float)JetDecimalConverter.FromDecimal(r)),
            long or ulong => Lng(value),
            int or short or byte or bool => Int(value),
            _ => RoundDouble(Dbl(Serial(NumericOperand(value)!)), places),
        };

        static object RoundDouble(double d, int places) =>
            RoundWritten(d, places, () => JetDecimalConverter.FromDouble(d), JetDecimalConverter.FromDecimal);

        // A number past a Decimal has no places left to cut.
        static object RoundWritten<T>(T original, int places, Func<decimal> written, Func<decimal, T> back) where T : notnull
        {
            decimal exact;
            try
            {
                exact = written();
            }
            catch (OverflowException)
            {
                return original;
            }
            decimal rounded = decimal.Round(exact, places, MidpointRounding.ToEven);
            return rounded == exact ? original : back(rounded);
        }
    }

    /// <summary>
    /// <c>LTrim</c> / <c>RTrim</c>. With one argument they are Access's, stripping <see cref="TrimmedSpaces"/>.
    /// With two they are SQL Server 2022's: every leading (or trailing) character that appears anywhere in the
    /// second argument is removed, so the second argument is a SET of characters and not a substring — a
    /// LibRed extension, since ACE takes only the one argument. Either argument Null gives Null, and an empty
    /// set of characters strips nothing.
    /// </summary>
    private string? Trim(Call f, Func<string, char[], string> trim)
    {
        object? value = Evaluate(f.Arguments[0]);
        if (value is null) return null;
        if (f.Arguments.Count == 1) return trim(ConcatText(value), TrimmedSpaces);

        object? characters = Evaluate(f.Arguments[1]);
        return characters is null ? null : trim(ConcatText(value), ConcatText(characters).ToCharArray());
    }

    /// <summary>SQL Server's <c>CONCAT_WS</c>: Null values are skipped, a Null separator is empty.</summary>
    private string ConcatWs(Call f) =>
        string.Join(Evaluate(f.Arguments[0]) is { } separator ? ConcatText(separator) : "",
            f.Arguments.Skip(1).Select(Evaluate).OfType<object>().Select(ConcatText));

    /// <summary>SQL Server's <c>TRANSLATE</c>: characters match as Replace matches; a surrogate pair is one.</summary>
    private string? Translate(Call f)
    {
        object? text = Evaluate(f.Arguments[0]), from = Evaluate(f.Arguments[1]), to = Evaluate(f.Arguments[2]);
        if (text is null || from is null || to is null) return null;

        string[] characters = [.. ConcatText(from).EnumerateRunes().Select(r => r.ToString())];
        string[] translations = [.. ConcatText(to).EnumerateRunes().Select(r => r.ToString())];
        if (characters.Length != translations.Length)
            throw new ArgumentException("Invalid procedure call: TRANSLATE's lists differ in length.");

        int locale = TextFold.Locale(Text.Collation.Lcid, TextFold.Search.Replace, database: true);
        var result = new StringBuilder();
        foreach (Rune rune in ConcatText(text).EnumerateRunes())
        {
            string character = rune.ToString();
            int at = Array.FindIndex(characters,
                c => TextFold.Matches(character, c, locale, TextFold.Search.Replace).FirstOrDefault((-1, 0)) == (0, character.Length));
            result.Append(at >= 0 ? translations[at] : character);
        }
        return result.ToString();
    }

    /// <summary>Applies a conversion to a single argument, propagating NULL.</summary>
    private object? Convert1(Call f, Func<object, object?> convert)
    {
        object? value = Evaluate(f.Arguments[0]);
        return value is null ? null : convert(value);
    }

    /// <summary>
    /// Access <c>CDate</c> (verified vs ACE): a date passes through; text is read as a date and time the way OLE
    /// Automation reads it (<see cref="VbaDateText"/>), and otherwise as a number; a number is the date at that
    /// serial (True is -1, 1899-12-29).
    /// </summary>
    internal static DateTime ToDate(object v) => v switch
    {
        DateTime d => d,
        _ when TryDateText(v, out DateTime parsed) => parsed,
        _ => OaDate(Dbl(ConversionNumber(v))),
    };

    /// <summary>A conversion function's argument as a number: text read as one (<see cref="VbaText.TextAsNumber"/>), a date
    /// as its serial and a Boolean as -1 or 0; a GUID or binary value is a type mismatch.</summary>
    internal static object ConversionNumber(object v) => Numeric(Serial(NumericOperand(v)!));

    /// <summary>
    /// A <c>CCur</c> or <c>CDec</c> argument as a Decimal, exactly where it can be: a Decimal as it is, and text that
    /// reads as a number within a Decimal's range as it reads (verified vs ACE: CCur('12345678901234.5678') and
    /// CCur(12345678901234.5678) keep every place). Anything else is read as the conversion functions read it and
    /// handed over as it is, so a Double or Single reaches CCur unrounded. Null for a Null argument.
    /// </summary>
    private object? DecimalArgument(Call f, Func<object, decimal> convert)
    {
        return Evaluate(f.Arguments[0]) switch
        {
            null => null,
            (string or char) and var text when VbaText.TextAsDecimal(text.ToString()!) is decimal exact => convert(exact),
            var value => convert(ConversionNumber(value)),
        };
    }

    /// <summary>Access <c>CCur</c>: the value to four places, half to even — a Double or Single from its exact binary
    /// value, as storing it in a Currency column takes it (<see cref="JetTypeCodec.CurrencyToScaled(object, IFormatProvider)"/>);
    /// past a Currency is an overflow.</summary>
    internal static decimal ToCurrency(object number) =>
        JetTypeCodec.CurrencyFromScaled(JetTypeCodec.CurrencyToScaled(number, CultureInfo.InvariantCulture));

    /// <summary>A date, or text that reads as one (<see cref="VbaDateText"/>); a number or Null is not.</summary>
    private static bool TryDateText(object? v, out DateTime value)
    {
        value = v as DateTime? ?? default;
        return v is DateTime || v is string text && VbaDateText.TryParse(text, CultureInfo.CurrentCulture, out value);
    }

    /// <summary>A DateValue or TimeValue argument: a date or text that reads as one, else a type mismatch.</summary>
    private static DateTime DateValueArgument(object v) =>
        TryDateText(v, out DateTime value) ? value : throw new InvalidCastException($"Type mismatch: '{ConcatText(v)}' is not a date.");

    /// <summary>Left and Right: the text's first or last characters. A Null length raises "Data type mismatch" as
    /// ACE does, and a negative one is an invalid procedure call.</summary>
    private string? StringInt(Call f, Func<string, int, string> op)
    {
        object? s = Evaluate(f.Arguments[0]);
        if (s is null) return null;
        object? nv = Evaluate(f.Arguments[1]);
        if (nv is null)
            throw new InvalidOperationException("Data type mismatch in criteria expression: length argument is null.");
        return op(ConcatText(s), Count(nv));
    }

    /// <summary>Access MID(string, start[, length]) — a 1-based substring; length omitted means to the end. A start
    /// below 1 or a negative length is an invalid procedure call.</summary>
    private string? Mid(Call f)
    {
        object? sv = Evaluate(f.Arguments[0]);
        if (sv is null || CountArgument(f, 1, least: 1) is not { } start
            || CountArgument(f, 2, absent: int.MaxValue) is not { } length)
            return null;
        string s = ConcatText(sv);
        int from = start - 1;
        if (from >= s.Length) return "";
        return s.Substring(from, Math.Min(length, s.Length - from));
    }

    /// <summary>Access INSTR([start,] string1, string2[, compare]) — the 1-based position of string2 in string1, 0 if
    /// it is not there. An empty string2 is found at start, wherever that is, unless string1 is empty; a start below 1
    /// is an invalid procedure call. The text is searched as <see cref="TextFold.Matches"/> searches it, under the
    /// compare argument's locale (<see cref="TextFold.Locale(int, TextFold.Search)"/>) — the database's when there
    /// is none.</summary>
    private int? Instr(Call f)
    {
        int argc = f.Arguments.Count;
        // 2 args: (s1, s2); 3+: (start, s1, s2[, compare]). A Null string gives Null before the start is checked
        // (verified vs ACE: InStr(0, Null, 'a', 1) is Null).
        object? s1v = Evaluate(f.Arguments[argc >= 3 ? 1 : 0]);
        object? s2v = Evaluate(f.Arguments[argc >= 3 ? 2 : 1]);
        bool absent = argc < 4;
        object? compare = absent ? null : Evaluate(f.Arguments[3]);
        if (s1v is null || s2v is null || !absent && compare is null
            || (argc >= 3 ? CountArgument(f, 0, least: 1) : 1) is not { } start)
            return null;

        // A byte[] argument (e.g. a 0xNN hex literal needle, or a binary haystack) coerces to a string one
        // char per byte, matching Jet — not "System.Byte[]".
        string s1 = ToJetString(s1v), s2 = ToJetString(s2v);
        if (s1.Length == 0) return 0;
        if (s2.Length == 0) return start;
        // A compare argument that is not a comparison is an invalid procedure call only once there is something to
        // search (verified vs ACE: InStr(1, '', 'a', 2) is 0, InStr(1, Null, 'a', 2) Null). A Null one has given Null
        // above, as a Null argument does throughout; ACE raises "Data type mismatch" for it.
        if (SearchLocale(compare, absent, TextFold.Search.InStr) is not { } locale)
            return null;
        if (start > s1.Length) return 0;
        foreach ((int index, int _) in TextFold.Matches(s1, s2, locale, TextFold.Search.InStr))
            if (index >= start - 1) return index + 1;
        return 0;
    }

    /// <summary>Access REPLACE(string, find, replace[, start[, count[, compare]]]) — the text from start on, with
    /// find replaced at most count times (all when count is -1). A start below 1 is an invalid procedure call. The
    /// text is searched as <see cref="TextFold.Matches"/> searches it for whole characters, under the compare
    /// argument's locale (<see cref="SearchLocale"/>) — the database's when there is none.</summary>
    private string? Replace(Call f)
    {
        object? sv = Evaluate(f.Arguments[0]), findv = Evaluate(f.Arguments[1]), replv = Evaluate(f.Arguments[2]);
        // ACE raises "Data type mismatch" here (unlike InStr, which propagates NULL), but LibRed propagates
        // instead, matching every other dialect - SQL Server documents REPLACE as returning NULL if any
        // argument is NULL. A deliberate divergence, and a widening: no query that returns a value today
        // changes, only ones that error start returning NULL.
        if (sv is null || findv is null || replv is null)
            return null;
        string s = StringArgument(sv), find = StringArgument(findv), repl = StringArgument(replv);

        if (CountArgument(f, 3, least: 1, absent: 1) is not { } start
            || CountArgument(f, 4, least: -1, absent: -1) is not { } count
            || SearchLocale(f.Arguments.Count > 5 ? Evaluate(f.Arguments[5]) : null, f.Arguments.Count <= 5,
                TextFold.Search.Replace) is not { } locale)
            return null;

        if (start > s.Length) return "";
        s = s[(start - 1)..];
        if (find.Length == 0) return s;

        var sb = new StringBuilder();
        int pos = 0, replaced = 0;
        foreach ((int j, int length) in TextFold.Matches(s, find, locale, TextFold.Search.Replace))
        {
            if (count >= 0 && replaced >= count) break;
            if (j < pos) continue;                          // overlaps the match just replaced
            sb.Append(s, pos, j - pos).Append(repl);
            pos = j + length;
            replaced++;
        }
        sb.Append(s.AsSpan(pos));
        return sb.ToString();
    }

    /// <summary>
    /// Fix, Int and Abs, keeping the operand's type (double→double, single→single, decimal→decimal, long→long, and
    /// the narrower integers and Booleans→int) so it matches EF's Math.* return type; NULL-propagating. Text is read
    /// as a number and gives a Double, True is -1, and an Integer result past a Long widens to a Double (verified vs
    /// ACE: Abs(-2147483648) is 2147483648). A date is its serial: Int and Fix give a date back
    /// (<paramref name="keepsDate"/>), Abs a Double.
    /// </summary>
    private object? Numeric1(Call f, Func<double, double> dOp, Func<decimal, decimal> mOp, bool keepsDate) =>
        Evaluate(f.Arguments[0]) switch
        {
            null => null,
            decimal m => mOp(m),
            double d => dOp(d),
            float s => (float)dOp(s),
            long l => checked((long)mOp(l)),
            DateTime d => keepsDate ? OaDate(dOp(d.ToOADate())) : dOp(d.ToOADate()),
            var v when v is string or char or Guid or byte[] => dOp(Dbl(NumericOperand(v)!)),
            var v => mOp(Dec(v)) is var r && r >= int.MinValue && r <= int.MaxValue ? (object)(int)r : JetDecimalConverter.FromDecimal(r),
        };

    /// <summary>
    /// Sqr, Exp, Log, Sin, Cos, Tan and Atn, on a Double read as the conversion functions read it; NULL-propagating.
    /// A result that is not a number — the square root or log of a number below its domain, or a trigonometric
    /// function of a number too large to reduce — is an invalid procedure call, and a result past a Double an
    /// overflow (verified vs ACE).
    /// </summary>
    private double? UnaryDouble(Call f, Func<double, double> op)
    {
        if (Evaluate(f.Arguments[0]) is not { } value)
            return null;
        double x = Dbl(ConversionNumber(value));
        double result = op(x);
        return double.IsNaN(result) || double.IsNegativeInfinity(result) && x == 0
            ? throw new ArgumentException($"Invalid procedure call: the function is not defined at {x}.")
            : Finite(result);
    }

    /// <summary>
    /// A function of two Doubles, read as <see cref="UnaryDouble"/> reads one; NULL-propagating. A result that is not
    /// a number is an invalid procedure call, and a result past a Double an overflow.
    /// </summary>
    private double? BinaryDouble(Call f, Func<double, double, double> op)
    {
        if (Evaluate(f.Arguments[0]) is not { } left || Evaluate(f.Arguments[1]) is not { } right)
            return null;
        double x = Dbl(ConversionNumber(left)), y = Dbl(ConversionNumber(right));
        double result = op(x, y);
        return double.IsNaN(result)
            ? throw new ArgumentException($"Invalid procedure call: {f.Name} is not defined at {x}, {y}.")
            : Finite(result);
    }

    /// <summary>A trigonometric function, which ACE refuses for an argument of 9.223372E18 or more (verified).</summary>
    private static Func<double, double> Trigonometric(Func<double, double> op) =>
        x => Math.Abs(x) >= 9.223372E18 ? double.NaN : op(x);

    /// <summary>Access <c>DatePart(interval, date, [firstdayofweek], [firstweekofyear])</c>: a component of a date.
    /// "ms", "mcs" and "ns" are LibRed extensions.</summary>
    private int? DatePart(Call f)
    {
        if (Evaluate(f.Arguments[0]) is not { } interval || Evaluate(f.Arguments[1]) is not { } date
            || FirstDayOfWeek(f, 2) is not { } first || FirstWeekOfYear(f, 3) is not { } rule)
            return null;
        DateTime d = ToDate(date);
        return ConcatText(interval).ToLowerInvariant() switch
        {
            "yyyy" => d.Year,
            "q" => (d.Month + 2) / 3,
            "m" => d.Month,
            "y" => d.DayOfYear,
            "d" => d.Day,
            "w" => DaysIntoWeek(d, first) + 1,
            "ww" => WeekOfYear(d, first, rule),
            "h" => d.Hour,
            "n" => d.Minute,
            "s" => d.Second,
            "ms" => d.Millisecond,
            "mcs" => d.Microsecond,
            "ns" => d.Nanosecond,
            _ => throw UnknownInterval(interval),
        };
    }

    private static ArgumentException UnknownInterval(object interval) =>
        new($"Invalid procedure call: '{interval}' is not a date interval.");

    /// <summary>Access <c>Weekday(date, [firstdayofweek])</c>: the day's 1-based position in the week.</summary>
    private object? Weekday(Call f) =>
        Evaluate(f.Arguments[0]) is { } date && FirstDayOfWeek(f, 1) is { } first
            ? DaysIntoWeek(ToDate(date), first) + 1
            : null;

    /// <summary>
    /// A firstdayofweek argument: 1 (Sunday) to 7 (Saturday), or 0 for the system's first day; anything else is an
    /// invalid procedure call (verified vs ACE: 0 is Monday under en-AU, and 8 and -1 are refused). Null when the
    /// argument is Null.
    /// </summary>
    private DayOfWeek? FirstDayOfWeek(Call f, int index, int absent = 1) =>
        Optional(f, index, absent, v => Setting(v, 0, 7)) switch
        {
            null => null,
            0 => CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek,
            var day => (DayOfWeek)(day - 1),
        };

    /// <summary>A firstweekofyear argument: 1 (the week of January 1), 2 (the first week with four days), 3 (the first
    /// full week), or 0 for the system's rule; anything else is an invalid procedure call. Null when it is Null.</summary>
    private CalendarWeekRule? FirstWeekOfYear(Call f, int index) =>
        Optional(f, index, 1, v => Setting(v, 0, 3)) switch
        {
            null => null,
            0 => CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule,
            1 => CalendarWeekRule.FirstDay,
            2 => CalendarWeekRule.FirstFourDayWeek,
            _ => CalendarWeekRule.FirstFullWeek,
        };

    /// <summary>A setting read as CInt reads it (half to even; verified vs ACE: '2' and 1.5 are 2); outside
    /// <paramref name="lowest"/>-<paramref name="highest"/> is an invalid procedure call.</summary>
    private static int Setting(object v, int lowest, int highest) => InRange(AsInteger(v), lowest, highest);

    /// <summary>How many days of the week come before the date's day, for a week starting on <paramref name="first"/>.</summary>
    private static int DaysIntoWeek(DateTime date, DayOfWeek first) => ((int)date.DayOfWeek - (int)first + 7) % 7;

    /// <summary>
    /// The date's week number (verified vs ACE). Days before the year's first week belong to the last week of the year
    /// before, and under the four-day rule a week with four days in the next year is that year's week 1.
    /// </summary>
    private static int WeekOfYear(DateTime date, DayOfWeek first, CalendarWeekRule rule)
    {
        date = date.Date;
        if (date < WeekOne(date.Year))
            return (date - WeekOne(date.Year - 1)).Days / 7 + 1;
        if (rule == CalendarWeekRule.FirstFourDayWeek && date.Year < 9999 && date >= WeekOne(date.Year + 1))
            return 1;
        return (date - WeekOne(date.Year)).Days / 7 + 1;

        DateTime WeekOne(int year)
        {
            var january1 = new DateTime(year, 1, 1);
            int before = DaysIntoWeek(january1, first);
            return january1.AddDays(rule switch
            {
                CalendarWeekRule.FirstDay => -before,
                CalendarWeekRule.FirstFourDayWeek => before <= 3 ? -before : 7 - before,
                _ => before == 0 ? 0 : 7 - before,
            });
        }
    }

    /// <summary>DateSerial and TimeSerial: a date or time from three Integer parts, read as CInt reads them (verified vs
    /// ACE: 32768 is an overflow). Parts out of their range carry into the next.</summary>
    private DateTime? DateParts(Call f, Func<int, int, int, DateTime> build)
    {
        if (Evaluate(f.Arguments[0]) is not { } a || Evaluate(f.Arguments[1]) is not { } b || Evaluate(f.Arguments[2]) is not { } c)
            return null;
        return build(AsInteger(a), AsInteger(b), AsInteger(c));
    }

    /// <summary>
    /// Access <c>DateSerial</c> (verified vs ACE): the month carries into the year first, then a year below 100 takes
    /// its century from the calendar's two-digit year window (so 49 is 2049, 50 is 1950, -1 is 1999 and
    /// DateSerial(100, 0, 1) is 1999-12-01), then the day carries. A year outside 100-9999 is an invalid procedure call.
    /// </summary>
    private static DateTime DateSerial(int year, int month, int day)
    {
        int months = year * 12 + month - 1;
        year = (int)Math.Floor(months / 12.0);
        month = months - year * 12 + 1;
        if (year < 100)
        {
            int latest = CultureInfo.CurrentCulture.Calendar.TwoDigitYearMax;
            int century = latest - latest % 100;
            year += year <= latest % 100 ? century : century - 100;
        }
        return InDateRange((year, month, day), static parts =>
            new DateTime(parts.year, parts.month, 1).AddDays(parts.day - 1));
    }

    /// <summary>A computed date, or an invalid procedure call when it falls outside 100-01-01 … 9999-12-31.</summary>
    private static DateTime InDateRange<T>(T state, Func<T, DateTime> compute)
    {
        try
        {
            DateTime date = compute(state);
            if (date.Year >= 100)
                return date;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        throw new ArgumentException("Invalid procedure call: the date is out of range.");
    }

    /// <summary>
    /// Access <c>DateAdd(interval, number, date)</c> (verified vs ACE): the number is truncated (True is -1), a month
    /// step keeps the day where the month has it and takes the month's last day otherwise, and a result outside
    /// 100-9999 is an invalid procedure call. "ms" is a LibRed extension.
    /// </summary>
    private DateTime? DateAdd(Call f)
    {
        if (Evaluate(f.Arguments[0]) is not { } interval || Evaluate(f.Arguments[1]) is not { } number
            || Evaluate(f.Arguments[2]) is not { } date)
            return null;
        double n = Math.Truncate(Dbl(ConversionNumber(number)));
        DateTime d = ToDate(date);
        return InDateRange((d, n, interval), static parts => ConcatText(parts.interval).ToLowerInvariant() switch
        {
            "yyyy" => parts.d.AddYears(Int(parts.n)),
            "q" => parts.d.AddMonths(checked(Int(parts.n) * 3)),
            "m" => parts.d.AddMonths(Int(parts.n)),
            "y" or "d" or "w" => parts.d.AddDays(parts.n),
            "ww" => parts.d.AddDays(parts.n * 7),
            "h" => parts.d.AddHours(parts.n),
            "n" => parts.d.AddMinutes(parts.n),
            "s" => parts.d.AddSeconds(parts.n),
            "ms" => parts.d.AddMilliseconds(parts.n),
            _ => throw UnknownInterval(parts.interval),
        });
    }

    /// <summary>
    /// Access <c>DateDiff(interval, date1, date2, [firstdayofweek], [firstweekofyear])</c> (verified vs ACE): the
    /// number of interval boundaries from date1 to date2, as a Long Integer. "w" is whole weeks of days, "ww" counts
    /// the weeks' first days, and "h", "n" and "s" count hour, minute and second boundaries. The first day of the
    /// week matters only to "ww", and the first week of the year to none.
    /// <para>A count past a Long Integer is Null</para>
    /// </summary>
    private int? DateDiff(Call f)
    {
        if (Evaluate(f.Arguments[0]) is not { } intervalV || Evaluate(f.Arguments[1]) is not { } d1V
            || Evaluate(f.Arguments[2]) is not { } d2V)
            return null;
        DateTime d1 = ToDate(d1V), d2 = ToDate(d2V);
        string interval = ConcatText(intervalV).ToLowerInvariant();

        if (interval == "ww")
        {
            if (FirstDayOfWeek(f, 3) is not { } first)
                return null;
            return (d2.Date.AddDays(-DaysIntoWeek(d2, first)) - d1.Date.AddDays(-DaysIntoWeek(d1, first))).Days / 7;
        }

        return interval switch
        {
            "yyyy" => d2.Year - d1.Year,
            "q" => (d2.Year - d1.Year) * 4 + (d2.Month - 1) / 3 - (d1.Month - 1) / 3,
            "m" => (d2.Year - d1.Year) * 12 + d2.Month - d1.Month,
            "y" or "d" => (d2.Date - d1.Date).Days,
            "w" => (d2.Date - d1.Date).Days / 7,
            // Hours cannot pass a Long Integer between any two dates Access represents. Minutes and seconds
            // can, and a count that does is Null — what ACE's result column gives, measured over OLE DB.
            "h" => (int)(d2.Ticks / TimeSpan.TicksPerHour - d1.Ticks / TimeSpan.TicksPerHour),
            "n" => d2.Ticks / TimeSpan.TicksPerMinute - d1.Ticks / TimeSpan.TicksPerMinute
                is >= int.MinValue and <= int.MaxValue and var minutes ? (int)minutes : null,
            "s" => d2.Ticks / TimeSpan.TicksPerSecond - d1.Ticks / TimeSpan.TicksPerSecond
                is >= int.MinValue and <= int.MaxValue and var seconds ? (int)seconds : null,
            "ms" => (long)(d2 - d1).TotalMilliseconds
                is >= int.MinValue and <= int.MaxValue and var milliseconds ? (int)milliseconds : null,
            _ => throw UnknownInterval(intervalV),
        };
    }

    /// <summary>
    /// <c>DateDiff_Big(interval, date1, date2, [firstdayofweek], [firstweekofyear])</c> — the same
    /// boundaries <see cref="DateDiff"/> counts, over the same interval table, counted into an <b>Int64</b>. A
    /// LibRed extension: ACE has no such function, and the Jet-dialect generator never emits it.
    /// </summary>
    private long? DateDiffBig(Call f)
    {
        if (Evaluate(f.Arguments[0]) is not { } intervalV || Evaluate(f.Arguments[1]) is not { } d1V
            || Evaluate(f.Arguments[2]) is not { } d2V)
            return null;
        DateTime d1 = ToDate(d1V), d2 = ToDate(d2V);
        string interval = ConcatText(intervalV).ToLowerInvariant();

        if (interval == "ww")
        {
            if (FirstDayOfWeek(f, 3) is not { } first)
                return null;
            return (long)((d2.Date.AddDays(-DaysIntoWeek(d2, first)) - d1.Date.AddDays(-DaysIntoWeek(d1, first))).Days / 7);
        }

        return interval switch
        {
            "yyyy" => (long)(d2.Year - d1.Year),
            "q" => (long)((d2.Year - d1.Year) * 4 + (d2.Month - 1) / 3 - (d1.Month - 1) / 3),
            "m" => (long)((d2.Year - d1.Year) * 12 + d2.Month - d1.Month),
            "y" or "d" => (long)(d2.Date - d1.Date).Days,
            "w" => (long)((d2.Date - d1.Date).Days / 7),
            "h" => d2.Ticks / TimeSpan.TicksPerHour - d1.Ticks / TimeSpan.TicksPerHour,
            "n" => d2.Ticks / TimeSpan.TicksPerMinute - d1.Ticks / TimeSpan.TicksPerMinute,
            "s" => d2.Ticks / TimeSpan.TicksPerSecond - d1.Ticks / TimeSpan.TicksPerSecond,
            "ms" => (long)(d2 - d1).TotalMilliseconds,
            _ => throw UnknownInterval(intervalV),
        };
    }

    /// <summary>The arm an IIf over <paramref name="arguments"/> takes, or null for a two-argument IIf whose condition
    /// does not hold.</summary>
    internal TExpression? IifArm(IReadOnlyList<TExpression> arguments) =>
        Evaluate(arguments[0]) is { } condition && IifCondition(condition) ? arguments[1]
            : arguments.Count == 3 ? arguments[2] : default;

    /// <summary>
    /// IIf's condition, which unlike the others is rounded to a whole number first, half to even (verified vs ACE:
    /// 0.4, 0.5, '0.4' and a date at 06:00 on 1899-12-30 are False, 0.6 and 1E+20 are True). Text that is not a
    /// number is True, as <see cref="IsTruthy"/> has it.
    /// </summary>
    protected static bool IifCondition(object value) => VbaBoolean.Plain(value)! switch
    {
        bool b => b,
        string or char => !(VbaText.TryTextAsNumber(value.ToString()!) is { } number && Math.Round(number, MidpointRounding.ToEven) == 0),
        DateTime d => Math.Round(d.ToOADate(), MidpointRounding.ToEven) != 0,
        Guid or byte[] => true,
        _ => !IsNumeric(value) || Math.Round(Dbl(value), MidpointRounding.ToEven) != 0,
    };

    internal static bool IsTruthy(object value) => VbaBoolean.Plain(value)! switch
    {
        bool b => b,
        string s => !ReadsAsZero(s),
        char c => !ReadsAsZero(c.ToString()),
        DateTime d => d != OaEpoch,
        Guid or byte[] => true,
        _ => !IsNumeric(value) || Dbl(value) != 0,
    };

    private static bool ReadsAsZero(string text)
    {
        try
        {
            return VbaText.TextAsDecimal(text) == 0m;
        }
        catch (InvalidCastException)
        {
            return false;   // not a number, so not 0
        }
    }

    /// <summary>Text, or a Double or Single, compared with a Currency column: read as storing it in the column reads it,
    /// then a Currency as <see cref="ToCurrency"/> makes one — with an index or without. Past a Currency is an
    /// overflow.</summary>
    protected static decimal AsCurrencyColumn(object value) =>
        ToCurrency(JetTypeCodec.NumberFromText(JetDataType.Currency, value));

    /// <summary>The operands of a <c>+</c> or <c>-</c> whose result is a Currency (<paramref name="currency"/>), before
    /// anything else reads them. A Currency plus or less a Double, a Single or text takes the other as a Currency column
    /// takes it, first (verified vs ACE: Currency 0.0001 + Double 0.03125 is 0.0313, where the exact sum rounds to
    /// 0.0314, and 0.0001 + '1.00005' is 1.0001, the text read exactly).</summary>
    internal static (object? Left, object? Right) CurrencySumOperands(object? left, object? right, bool currency)
    {
        if (left is not null && right is not null && currency)
        {
            if (left is double or float or string or char) left = AsCurrencyColumn(left);
            if (right is double or float or string or char) right = AsCurrencyColumn(right);
        }
        return (left, right);
    }

    /// <summary>The operands, neither Null, of a <c>+</c>, <c>-</c>, <c>*</c> or <c>/</c> (<paramref name="divide"/>
    /// for <c>/</c>), a side's flag saying it is a Currency. A Currency beside a Double or Single, or divided, is worked
    /// out as a Double, as OLE Automation does (verified vs ACE: Currency 1.1 * Double 0.1 is 0.11000000000000001, and
    /// Currency / 3 a Double).</summary>
    internal static (object Left, object Right) CurrencyProductOperands(
        object left, object right, bool divide, bool leftCurrency, bool rightCurrency)
    {
        if ((divide || right is double or float) && left is decimal money && leftCurrency)
            left = JetDecimalConverter.FromDecimal(money);
        if ((divide || left is double or float) && right is decimal moneyRight && rightCurrency)
            right = JetDecimalConverter.FromDecimal(moneyRight);
        return (left, right);
    }

    /// <summary>The result of a <c>+</c>, <c>-</c> or <c>*</c>, a Currency where the operation's result is one
    /// (<paramref name="currency"/>). A result that is a Currency is one at every step, not only in the result column:
    /// its four places and its range apply to it where it is worked out, as CCur applies them (verified vs ACE: Currency
    /// 1.2345 * 1.2345 is 1.524, 0.0003 * 1.2345 is 0.0004, and the largest Currency + 1 or * 3 is an overflow). A
    /// Currency with a written decimal is not one, nor one times a Double; one plus or less a Double is (verified vs
    /// ACE: Currency 1.2345 + Double 1.5 is the Currency 2.7345).</summary>
    internal static object CurrencyStep(object result, bool currency) =>
        result is decimal or double or float && currency ? ToCurrency(result) : result;

    /// <summary>Text compared with a date column, read as storing it in the column reads it — with an index or
    /// without. Text that is not a date is a type mismatch.</summary>
    protected static DateTime AsDateColumn(object text)
    {
        try
        {
            return JetTypeCodec.ToDate(text.ToString()!, CultureInfo.InvariantCulture);
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
        {
            throw new InvalidCastException($"Data type mismatch: '{text}' cannot be read as a date.", e);
        }
    }

    /// <summary>
    /// <c>BAND</c>, <c>BOR</c> and <c>BXOR</c> on the operands' bits. When either operand is 16 bits, only the low 16
    /// bits are combined and the rest are the left operand's: its own upper bits, or the sign of the 16-bit result when
    /// it is 16 bits itself (verified vs ACE: 1 BOR TRUE is 65535, CLNG(-1) BAND CINT(1) is -65535, CINT(1) BOR 70000
    /// is 4465, CINT(-2) BOR 1 is -1). Otherwise the result has the wider operand's width. The result is an Integer
    /// only when both operands are 16 bits, and otherwise a Long or an Int64 (verified vs ACE: TRUE BAND CINT(-2) is
    /// an Integer, CINT(-2) BAND 70000 and a Byte BAND a Byte are Longs).
    /// </summary>
    protected static object BitwiseOp(object a, object b, Func<long, long, long> op)
    {
        (long left, int leftWidth) = BitOperand(a);
        (long right, int rightWidth) = BitOperand(b);
        if (leftWidth != 16 && rightWidth != 16)
            return Signed(op(left, right), Math.Max(leftWidth, rightWidth));

        long low = op(left, right) & 0xFFFF;
        return leftWidth != 16 ? Signed(left & ~0xFFFFL | low, leftWidth)
            : rightWidth == 16 ? Signed(low, 16)
            : Signed((short)low, 32);
    }

    /// <summary>
    /// Unary minus, keeping the operand's type as C# does (an Integer, a Byte or a Boolean gives a Long). Text is read as
    /// a number first, and a date negates its serial and stays a date (verified vs ACE: -#2020-01-02# is 1779-12-27). A
    /// result the operand's type cannot hold is an overflow (verified vs ACE: -CINT(-32768) and -CLNG(-2147483648)).
    /// </summary>
    internal static object Negate(object v) => NumericOperand(v)! switch
    {
        decimal d => -d,
        double d => -d,
        float f => -f,
        long l => checked(-l),
        ulong u => checked(-(long)u),
        DateTime d => OaDate(-d.ToOADate()),
        short.MinValue => throw new OverflowException("Overflow: an Integer cannot hold 32768."),
        var n => checked(-Int(n)),
    };

    /// <summary><c>BNOT</c>: every bit of the operand flipped, an Integer for a Boolean or an Integer (verified vs ACE:
    /// BNOT of a Yes/No column is an Integer, of a Byte a Long).</summary>
    protected static object BitNot(object v)
    {
        (long bits, int width) = BitOperand(v);
        return Signed(~bits, width);
    }

    /// <summary>
    /// An operand's bits, unsigned, and how many there are: 16 for a Boolean (True is all 16 set) or an Integer, 32 for a
    /// Byte or a Long, 64 for an Int64. Anything else is first read as a whole number, as <c>\</c> reads it: text as a
    /// number, a date as its serial, then rounded half to even, and it must fit a Long.
    /// </summary>
    private static (long Bits, int Width) BitOperand(object v) => VbaBoolean.Plain(v)! switch
    {
        bool b => (b ? 0xFFFF : 0, 16),
        short s => ((ushort)s, 16),
        byte b => (b, 32),
        int i => ((uint)i, 32),
        long l => (l, 64),
        ulong u => ((long)u, 64),
        _ => ((uint)Int(Serial(NumericOperand(v)!)), 32),
    };

    /// <summary>The low <paramref name="width"/> bits read as a signed number: an Int16 for 16 bits, an Int32 for 32,
    /// an Int64 for 64.</summary>
    private static object Signed(long bits, int width) => width switch
    {
        // Each arm boxed on its own, or the switch would widen them all to Int64.
        16 => (object)(short)bits,
        32 => (object)(int)bits,
        _ => (object)bits,
    };

    /// <summary>A date moved by a span, to the tick; outside the dates Jet holds, an overflow.</summary>
    protected static DateTime Shift(DateTime date, TimeSpan by)
    {
        long ticks = date.Ticks + by.Ticks;
        return ticks >= new DateTime(100, 1, 1).Ticks && ticks <= DateTime.MaxValue.Ticks
            ? new DateTime(ticks)
            : throw new OverflowException($"Overflow: {date} moved by {by} is outside the range of a date.");
    }

    /// <summary>
    /// The two sides of a comparison, brought to a kind they compare as (verified vs ACE, except as noted).
    /// <list type="bullet">
    /// <item>Text against a number, Boolean or date: the text reads as a number (<see cref="VbaText.TextAsNumber"/>), a type
    /// mismatch when it is not one — exactly, against a Decimal (verified vs ACE: '9007199254740993' finds that row of a
    /// DECIMAL(20,0) column and not 9007199254740992's). In a query ACE does this for text a function returns; for a
    /// text literal or column beside a number that is not computed it raises a type mismatch, which the engine's
    /// comparison raises before it gets here.</item>
    /// <item>A date against a number: its serial.</item>
    /// <item>A GUID or binary value against text: both as text (a GUID braced upper case, binary as UTF-16). A GUID
    /// against binary: the GUID's sixteen bytes, as a binary column stores it, against the binary's (verified vs ACE,
    /// with an index on the binary column or without). Against a number or date: a type mismatch.</item>
    /// </list>
    /// </summary>
    internal static (object Left, object Right) Comparable(object left, object right)
    {
        (left, right) = ComparedBooleans(left, right);
        if (left is char leftChar) left = leftChar.ToString();
        if (right is char rightChar) right = rightChar.ToString();
        bool leftText = left is string, rightText = right is string;
        bool leftBlob = left is Guid or byte[], rightBlob = right is Guid or byte[];
        if ((leftText && rightBlob) || (leftBlob && rightText))
            return (ConcatText(left), ConcatText(right));
        if (leftBlob && rightBlob && left.GetType() != right.GetType())
            return (JetTypeCodec.AsBinary(left, currency: false)!, JetTypeCodec.AsBinary(right, currency: false)!);
        if (leftBlob || rightBlob)
            return leftBlob && rightBlob
                ? (left, right)
                : throw new InvalidCastException("Type mismatch: a GUID or binary value cannot be compared with a number or date.");
        if (leftText == rightText)
            return left is DateTime ^ right is DateTime ? (Serial(left), Serial(right)) : (left, right);
        return (leftText ? JetTypeCodec.NumberFromText(right is decimal ? JetDataType.FixedPoint : JetDataType.Double, left) : Serial(left),
            rightText ? JetTypeCodec.NumberFromText(left is decimal ? JetDataType.FixedPoint : JetDataType.Double, right) : Serial(right));
    }

    /// <summary>The order of two values once <see cref="Comparable"/> has brought them to a common kind, text in
    /// <paramref name="text"/>'s collation.</summary>
    internal static int CompareValues(object left, object right, JetTextComparer text)
    {
        (object l, object r) = Comparable(left, right);
        return Compare(l, r, text);
    }

    /// <summary>Whether <paramref name="e"/> is an error an expression raised — a division by zero, an overflow, an
    /// invalid procedure call, a type mismatch — which a statement reports as that expression's error.</summary>
    internal static bool IsExpressionError(Exception e) =>
        e is ArithmeticException or InvalidCastException or ArgumentException or InvalidOperationException;

    // '&' treats a single Null as "" but is Null when both sides are (verified vs ACE).
    internal static string? Concat(object? left, object? right) =>
        left is null && right is null
            ? null
            : (left is null ? "" : ConcatText(left)) + (right is null ? "" : ConcatText(right));

    /// <summary><c>NOT</c>: the value's truth negated (<see cref="AsBool"/>), so a -1/0 integer Boolean is negated
    /// too; Null when Null.</summary>
    internal bool? Not(object? v) => AsBool(v) is bool b ? !b : null;

    // AND/OR use Kleene three-valued logic, and short-circuit: `false AND x` is false and `true OR x`
    // is true regardless of x — so the right operand (which may be an expensive correlated subquery) is
    // only evaluated when it can affect the result.
    internal object? And(TExpression left, TExpression right)
    {
        bool? l = AsBool(Evaluate(left));
        return l == false ? false : l & AsBool(Evaluate(right));
    }

    internal object? Or(TExpression left, TExpression right)
    {
        bool? l = AsBool(Evaluate(left));
        return l == true ? true : l | AsBool(Evaluate(right));
    }

    /// <summary>A value as a condition — for <c>NOT</c>, <c>AND</c>, <c>OR</c>, <c>XOR</c>, <c>EQV</c>, <c>IMP</c>, a
    /// <c>WHERE</c> and <c>= True</c> or <c>= False</c>: False when it reads as 0, True otherwise, Null when Null (verified
    /// vs ACE: <c>'0'</c> is False, <c>'abc'</c>, <c>''</c> and a date are True). A binary value or a GUID is False when it
    /// equals the text "0", a False beside text, in the database's collation, so a GUID never is (verified vs ACE: 0x30,
    /// 0x3000, 0x30000000, 0x00003000, 0x30002000 and 0x10FF, which is a full-width zero, are False; 0x30003000,
    /// 0x00000000, 0x0000 and 0x3000200000000000 are True). IIf's condition is not this one (<see cref="IifCondition"/>).</summary>
    internal bool? AsBool(object? v) => v is null ? null : v is Guid or byte[] ? CompareValues(v, "0", Text) != 0 : IsTruthy(v);

    /// <summary><c>+ - *</c> with C# widest-operand type promotion, so the result CLR type matches what EF
    /// expects (int+int→int, …): decimal &gt; double &gt; single &gt; long &gt; int. (Contract: like
    /// <c>Enumerable</c> arithmetic — LibRed emits the operand type, not an ACE-widened one.) A result past its type's range is an Overflow
    /// error, Double and Single alike — a Single result is not widened to a Double as ACE widens it, because the result
    /// keeps its operand type. A Long past its range is a Double instead, as OLE Automation's arithmetic makes it
    /// (verified vs ACE: <c>VarType([Qty]*1000000000)</c> and <c>VarType(2147483647*2)</c> are 5, Double, and so in a
    /// calculated column); where a result column is a Long, storing it there is the overflow.</summary>
    internal static object Arithmetic(object left, object right, char op)
    {
        // Date/time arithmetic operates on the OLE Automation serial (days since 1899-12-30; the fractional
        // part is the time), verified vs ACE: date+time and date±N days yield a DateTime, but date−date yields
        // a plain day count (Double). A number operand is taken as a count of days. The result is rounded to a
        // whole second (Jet has no sub-second) to shed the tiny floating-point drift the serial round-trip adds.
        if (left is DateTime || right is DateTime)
        {
            double a = Oa(left), b = Oa(right);
            bool bothDates = left is DateTime && right is DateTime;
            return op switch
            {
                '-' when bothDates => a - b,                       // date − date → number of days
                '+' => OaDate(a + b),
                '-' => OaDate(a - b),
                // date × n has no date meaning → numeric; times a Decimal, a Decimal (verified vs ACE: a date times
                // 0.25 is 10958.125 as a Decimal)
                _ when left is decimal || right is decimal => ArithmeticDecimal(left) * ArithmeticDecimal(right),
                _ => Finite(a * b),
            };
        }
        // A result past its type is an error, not a wrapped or infinite value (verified vs ACE: 1E300 * 1E300 fails, and
        // as result values 2147483647 + 1, 2147483647 * 2 and -2147483647 - 2, whose Long result column cannot hold them).
        if (left is decimal || right is decimal) { decimal a = ArithmeticDecimal(left), b = ArithmeticDecimal(right); return op == '+' ? a + b : op == '-' ? a - b : a * b; }
        if (left is double || right is double) { double a = Dbl(left), b = Dbl(right); return Finite(op == '+' ? a + b : op == '-' ? a - b : a * b); }
        if (left is float || right is float) { float a = (float)Dbl(left), b = (float)Dbl(right); return Finite(op == '+' ? a + b : op == '-' ? a - b : a * b); }
        if (left is long or ulong || right is long or ulong) { long a = Lng(left), b = Lng(right); return checked(op == '+' ? a + b : op == '-' ? a - b : a * b); }
        int x = Int(left), y = Int(right);
        long whole = op == '+' ? (long)x + y : op == '-' ? (long)x - y : (long)x * y;
        if (whole is >= int.MinValue and <= int.MaxValue)
            return (int)whole;
        return (double)whole;
    }

    /// <summary>
    /// Access <c>^</c> (verified vs ACE): Double, a date read as its serial. A negative base with a fractional
    /// exponent is an invalid procedure call, zero to a negative power a division by zero, and a result past a
    /// Double an overflow.
    /// </summary>
    internal static double Power(object left, object right)
    {
        double x = Oa(left), y = Oa(right);
        double result = Math.Pow(x, y);
        if (double.IsNaN(result))
            throw new ArgumentException($"Invalid procedure call: {x} cannot be raised to the power {y}.");
        if (double.IsInfinity(result))
            throw x == 0 ? new DivideByZeroException("Division by zero: zero raised to a negative power.") : new OverflowException("Overflow: the result is too large for a number.");
        return result;
    }

    /// <summary>
    /// Access <c>+</c> (verified vs ACE). Two text operands concatenate — a GUID or binary value counts as text.
    /// Otherwise the operands add, text read as a number (<see cref="VbaText.TextAsNumber"/>); so <c>'1' + 1</c> is 2 and
    /// <c>'1' + #2020-01-02#</c> is the next day. A GUID or binary value with anything but text is a type
    /// mismatch. Null has already propagated.
    /// </summary>
    internal static object Add(object left, object right) =>
        IsConcatText(left) && IsConcatText(right)
            ? ConcatText(left) + ConcatText(right)
            : Arithmetic(NumericOperand(left)!, NumericOperand(right)!, '+');

    private static bool IsConcatText(object v) => v is string or char or Guid or byte[];

    /// <summary>Access <c>/</c> is floating division — Decimal when either operand is Decimal/Currency,
    /// otherwise Double (never integer division; that is <c>\</c>). A date divides as its serial; dividing by
    /// zero is an error and so is a result past a Double (verified vs ACE: 1 / 0 and 0 / 0 fail).</summary>
    internal static object Divide(object left, object right)
    {
        if (left is decimal || right is decimal)
            return ArithmeticDecimal(left) / ArithmeticDecimal(right);
        // A Single divided with only Singles, Integers or Booleans is a Single (verified vs ACE: TRUE / a Single 1.5
        // is -0.6666667, and Str(CSng(1) / CSng(10)) is " .1"). OLE DB reports it as a Double, as it does CSng.
        if ((left is float || right is float) && IsSingleWidth(left) && IsSingleWidth(right))
        {
            float singleDivisor = Sng(right);
            if (singleDivisor == 0)
                throw new DivideByZeroException("Division by zero.");
            return Finite(Sng(left) / singleDivisor);
        }
        double divisor = Oa(right);
        if (divisor == 0)
            throw new DivideByZeroException("Division by zero.");
        return Finite(Oa(left) / divisor);
    }

    // Not Byte: ACE treats a Byte column as a Long here (verified: a Byte 1 / a Single 1.5 is 0.6666666666666666).
    private static bool IsSingleWidth(object v) => v is float or short or bool;

    /// <summary>Access integer operators <c>\</c> (int division) and <c>MOD</c>: operands round to an
    /// integer (half to even), a date as its serial, and the result keeps the operand's integer type (int, or
    /// long if either is Int64) — so <c>int \ int</c> is Int32, matching the EF contract. Anything MOD -1 is 0,
    /// even the smallest Long (verified vs ACE).</summary>
    /// <remarks>An Int32 result is worked out in Int64 all the same, and only the result has to fit: ACE squeezes
    /// each operand into a Long first, so a Double, Decimal or Currency past one overflows even where the answer
    /// would not — <c>1E12 MOD 7</c>, whose remainder is below 7. Here that is 1, and a quotient past a Long is still
    /// an overflow, since the column's type is settled before any value is seen. A LibRed extension.</remarks>
    // CA1859 reads the two branches below as one `long`. They are not: the Int32 branch is boxed as an Int32
    // on purpose, and narrowing the return type would widen it back — see the remark above.
#pragma warning disable CA1859
    protected static object IntegerOp(object left, object right, char op)
    {
        left = Serial(left);
        right = Serial(right);
        long a = Lng(left), b = Lng(right);
        long result = op == '%' ? (b == -1 ? 0L : a % b) : a / b;
        // Each branch boxed on its own: a bare `? result : (int)result` is a long, and would widen the Int32 back.
        return left is long or ulong || right is long or ulong ? (object)result : checked((int)result);
    }
#pragma warning restore CA1859

    // For date arithmetic: a DateTime becomes its OLE Automation serial; a number is taken verbatim (as days).
    protected static double Oa(object v) => v is DateTime d ? d.ToOADate() : Dbl(v);

    protected static object Serial(object value) => value is DateTime d ? d.ToOADate() : value;

    /// <summary>A number as a Decimal for arithmetic with a Decimal: a Double or Single converted the OLE Automation
    /// way (<see cref="JetDecimalConverter"/>), a date as its serial.</summary>
    protected static decimal ArithmeticDecimal(object v) => v is DateTime date ? SerialDecimal(date) : Dec(v);

    protected static double Finite(double value) =>
        double.IsFinite(value) ? value : throw new OverflowException("Overflow: the result is too large for a number.");

    protected static float Finite(float value) =>
        float.IsFinite(value) ? value : throw new OverflowException("Overflow: the result is too large for a number.");

    /// <summary>The date at an OLE Automation serial; a serial outside 100-01-01 … 9999-12-31 is an overflow.</summary>
    protected static DateTime OaDate(double serial)
    {
        try
        {
            return DateTime.FromOADate(serial);
        }
        catch (ArgumentException)
        {
            throw new OverflowException($"Overflow: {serial} is outside the range of a date.");
        }
    }


    /// <summary>A date's day number as a Decimal, built from its parts rather than converted from the Double serial
    /// (before the epoch the time fraction still counts away from zero, as in the OLE Automation serial).</summary>
    private static decimal SerialDecimal(DateTime d)
    {
        decimal days = (d.Date - OaEpoch).Days;
        decimal time = d.TimeOfDay.Ticks / (decimal)TimeSpan.TicksPerDay;
        return days >= 0 ? days + time : days - time;
    }


    /// <summary>An arithmetic operand: text read as a number (<see cref="VbaText.TextAsNumber"/>); a GUID or binary value
    /// is a type mismatch; anything else, Null included, as it is. A <see cref="char"/> (a parameter can carry one)
    /// is one character of text.</summary>
    internal static object? NumericOperand(object? v) => VbaBoolean.Plain(v) switch
    {
        VbaEmpty => (short)0,
        string s => VbaText.TextAsNumber(s),
        char c => VbaText.TextAsNumber(c.ToString()),
        Guid or byte[] => throw new InvalidCastException("Type mismatch: a GUID or binary value is not a number."),
        var plain => plain,
    };

    /// <summary>A value as <c>&amp;</c> (and <c>+</c> between two texts) writes it: as CStr writes it
    /// (<see cref="VbaText.Of"/>), VBA's Empty as "", and a VBA Boolean as "True" or "False".</summary>
    internal static string ConcatText(object v) => v switch
    {
        VbaEmpty => "",
        VbaBoolean b => b.Value ? "True" : "False",
        _ => VbaText.Of(v),
    };

    /// <summary>A value as a function taking a String reads it (verified vs ACE: Asc, AscW, AscB, Val, Replace,
    /// StrReverse and InStrRev): as <see cref="ConcatText"/> writes it, but a VBA Boolean as "-1" or "0".</summary>
    internal static string StringArgument(object v) => ConcatText(VbaBoolean.Plain(v)!);

    /// <summary>The two sides of a comparison with a VBA Boolean taken out: beside text it is the text "-1" or "0"
    /// (verified vs ACE: <c>IsNull(x) = '-1'</c> holds where <c>= 'True'</c> does not, and <c>&gt; ''</c> always), and
    /// anywhere else the Boolean it is.</summary>
    internal static (object Left, object Right) ComparedBooleans(object left, object right) =>
        (Compared(left, right), Compared(right, left));

    private static object Compared(object side, object other) =>
        side is VbaBoolean b ? other is string or char ? StringArgument(b) : b.Value : side;

    protected static readonly DateTime OaEpoch = new(1899, 12, 30);

    /// <summary>VBA <c>CBool</c> (verified vs ACE): "True" and "False" as written, otherwise whether the value
    /// read as a number (<see cref="ConversionNumber"/>) is non-zero, so 0.5, '$5' and a date are True.</summary>
    private static bool VbaBool(object v) =>
        v is bool b ? b
        : v is string s && bool.TryParse(s, out bool parsed) ? parsed
        : Dbl(ConversionNumber(v)) != 0;

    // Jet's boolean convention (true = -1, false = 0) so a bool matches the numeric column it is stored in.
    protected static object Numeric(object v) => VbaBoolean.Plain(v)! switch
    {
        bool b => b ? -1 : 0,
        VbaEmpty => (short)0,
        var plain => plain,
    };
    protected static decimal Dec(object v) => v switch
    {
        int n => n,
        decimal d => d,
        long n => n,
        bool b => b ? -1m : 0m,
        _ => JetDecimalConverter.ToDecimal(Numeric(v), CultureInfo.InvariantCulture),
    };
    internal static double Dbl(object v) => JetDecimalConverter.ToDouble(Numeric(v), CultureInfo.InvariantCulture);
    // Narrow to single precision (the cast yields ±Infinity for an out-of-range double rather than throwing).
    protected static float Sng(object v) => (float)Dbl(v);
    protected static long Lng(object v) => Convert.ToInt64(Numeric(v), CultureInfo.InvariantCulture);
    protected static int Int(object v) => Convert.ToInt32(Numeric(v), CultureInfo.InvariantCulture);

    internal static int Compare(object left, object right, JetTextComparer text)
    {
        // Already done where Comparable came first; NullIf, Greatest and Least compare here directly.
        (left, right) = ComparedBooleans(left, right);
        // Empty is "" beside text and 0 beside anything else, as VBA compares it.
        if (left is VbaEmpty) left = right is string ? "" : (short)0;
        if (right is VbaEmpty) right = left is string ? "" : (short)0;

        // The common stored LONG/LONG comparison needs no coercion. Its integer ordering is exactly
        // the Decimal ordering used below, including the full signed range, without two object conversions.
        if (left is int leftInteger && right is int rightInteger)
            return leftInteger.CompareTo(rightInteger);
        if (left is double leftDouble && right is double rightDouble)
            return leftDouble.CompareTo(rightDouble);

        if (IsNumeric(left) && IsNumeric(right))
        {
            // A single-precision operand (a Single column value, a CSNG result, a SUM of singles) compares in
            // single precision — narrow the other side to Single too. Widening the single to double instead
            // exposes its rounding (a stored -1.234f is -1.2339999675… as a double) and breaks equality against
            // a double literal, whereas ACE compares the literal in the column's single precision (no CSNG in
            // the SQL). Only a Single present, no genuine double column, triggers this.
            if (left is float || right is float)
                return Sng(left).CompareTo(Sng(right));

            // A Double beside a Decimal compares as a Decimal, the Double converted the OLE Automation way (verified vs
            // ACE: a Double 0.1 is less than 0.10000000000000001); two Doubles, or a Double beside a whole number,
            // compare as Doubles. Where a Double is past a Decimal's range they compare as Doubles too. Whole numbers
            // and Decimals compare as Decimals, which hold 64-bit integers and exact decimals without loss.
            if (left is double && right is not decimal || right is double && left is not decimal)
                return Dbl(left).CompareTo(Dbl(right));
            try
            {
                return Dec(left).CompareTo(Dec(right));
            }
            catch (OverflowException)
            {
                return Dbl(left).CompareTo(Dbl(right));
            }
        }

        // Binary (byte[]) columns: structural, length-sensitive byte compare — lexicographic then by
        // length, so a shorter value sorts before a longer one sharing its prefix (Jet's binary order,
        // matching IndexKeyCodec). Without this, byte[] falls through to ToString() ("System.Byte[]"
        // for every array) and all binaries compare *equal* — so `WHERE binKey = @p` matches every row.
        if (left is byte[] lb && right is byte[] rb)
            return CompareBytes(lb, rb);

        if (left is string || right is string)
            return text.Compare(left.ToString()!, right.ToString()!);

        // Dates compare by their OLE Automation serial rather than chronologically. Below the epoch
        // (1899-12-30) the day count is negative while the time fraction stays positive, so 1899-12-29 06:00 is
        // -1.25 and 18:00 is -1.75 — later in the day is the SMALLER serial. ACE compares and orders on that raw
        // serial and therefore puts later pre-epoch times first (verified in
        // LibRed.Core.Tests.AcePreEpochDateProbeTest: `06:00 < 18:00` is False, ORDER BY gives 1,3,2,4,5,6).
        //
        // Matching it is not only about ACE parity: IndexKeyCodec writes this same serial as the index key,
        // and that encoding cannot change because ACE writes those keys too. Comparing chronologically here
        // while the index compares by serial made an index seek and a table scan return DIFFERENT rows for a
        // pre-epoch range (see PreEpochDateOrderingTests). From the epoch onward the two orders are identical,
        // so this only affects pre-1899 dates.
        //
        // ToOADate keeps whole milliseconds, which is all a Date/Time column holds, but a DATETIME2 keeps 100-ns
        // ticks: two of those in the same millisecond are settled by their ticks, the way the serial would run —
        // later first below the epoch — so they neither compare equal nor sort as a tie.
        if (left is DateTime leftDate && right is DateTime rightDate)
        {
            int bySerial = leftDate.ToOADate().CompareTo(rightDate.ToOADate());
            return bySerial != 0 ? bySerial
                : leftDate.Ticks.CompareTo(rightDate.Ticks) * (leftDate < OaEpoch ? -1 : 1);
        }

        if (left is IComparable c && left.GetType() == right.GetType())
            return c.CompareTo(right);

        return text.Compare(left.ToString()!, right.ToString()!);
    }

    /// <summary>Lexicographic byte comparison, then by length (shorter prefix sorts first).</summary>
    private static int CompareBytes(byte[] a, byte[] b) => a.AsSpan().SequenceCompareTo(b);

    // Booleans count as numeric for comparison: EF maps CLR bool to a numeric (smallint) column, and
    // a boolean predicate (e.g. IS NOT NULL) must compare equal to that stored value. The comparison
    // coercions (Dec/Dbl) use Jet's convention (false = 0, true = -1) so a bool matches the numeric value
    // it is stored as.
    internal static bool IsNumeric(object v) => VbaBoolean.Plain(v) is bool || JetTypeCodec.IsNumber(v);
}
