using EntityFrameworkCore.Jet.Data;
using LibRed.Catalog;
using System.Globalization;

namespace LibRed.Storage.Calculated;

/// <summary>
/// Evaluates a parsed calculated-column expression against one row, the way ACE does.
/// </summary>
/// <remarks>
/// <para>Only the functions ACE actually permits are implemented, and an expression using anything else
/// throws rather than guessing — a wrong cached value is invisible, because neither engine re-derives it on
/// read (page-02b §3.4a).</para>
/// <para><b>Null semantics are the point.</b> Access propagates Null through arithmetic and comparison but
/// <b>not</b> through <c>&amp;</c>, which treats Null as the empty string. That asymmetry is the whole
/// mechanism behind the idiom MSDN uses to introduce the feature —
/// <c>[First] &amp; " " &amp; ([Middle] + " ") &amp; [Last]</c> — where <c>+</c> makes the parenthesised
/// group Null when there is no middle initial and <c>&amp;</c> then drops it, so no double space appears.
/// Get the two operators the same way round and that expression silently gains a space.</para>
/// </remarks>
internal static class CalculatedEvaluator
{
    /// <summary>Evaluates <paramref name="node"/>; <paramref name="value"/> resolves a column by name.</summary>
    public static object? Evaluate(CalcNode node, Func<string, object?> value) => node switch
    {
        CalcLiteral l => l.Value,
        CalcColumn c => value(c.Name),
        CalcUnary u => Unary(u, value),
        CalcBinary b => Binary(b, value),
        CalcIsNull i => Evaluate(i.Operand, value) is null != i.Negated,
        CalcIn i => In(i, value),
        CalcCall f => Call(f, value),
        _ => throw new CalculatedExpressionException($"Unsupported expression node {node.GetType().Name}."),
    };

    private static object? Unary(CalcUnary node, Func<string, object?> value)
    {
        object? operand = Evaluate(node.Operand, value);
        if (operand is null) return null;
        return node.Operator switch
        {
            "-" => -ToNumber(operand),
            "Not" => !ToBoolean(operand),
            _ => throw new CalculatedExpressionException($"Unsupported unary operator '{node.Operator}'."),
        };
    }

    private static object? Binary(CalcBinary node, Func<string, object?> value)
    {
        object? left = Evaluate(node.Left, value);
        object? right = Evaluate(node.Right, value);

        // '&' is the one operator that does NOT propagate Null: it concatenates it as "".
        if (node.Operator == "&") return ToText(left) + ToText(right);

        if (left is null || right is null) return null;

        switch (node.Operator)
        {
            case "+":
                {
                    // '+' concatenates when BOTH sides are text, and adds otherwise — the overload that makes
                    // it the null-propagating sibling of '&'. Adding a number to a date yields a DATE, not the
                    // serial underneath it, so `[D] + 1` stays a date the way ACE stores it.
                    if (left is string a && right is string b) return a + b;
                    double sum = ToNumber(left) + ToNumber(right);
                    return left is DateTime ^ right is DateTime ? DateTime.FromOADate(sum) : sum;
                }
            case "-":
                return left is DateTime && right is DateTime
                    ? ToNumber(left) - ToNumber(right)
                    : left is DateTime d ? d.AddDays(-ToNumber(right)) : (object)(ToNumber(left) - ToNumber(right));
            case "*": return ToNumber(left) * ToNumber(right);
            case "/": return ToNumber(left) / ToNumber(right);
            case "^": return Math.Pow(ToNumber(left), ToNumber(right));
            case "And": return ToBoolean(left) && ToBoolean(right);
            case "Or": return ToBoolean(left) || ToBoolean(right);
            case "Like": return Like(ToText(left), ToText(right));
            case "=":
            case "<>":
            case "<":
            case ">":
            case "<=":
            case ">=":
                {
                    int c = Compare(left, right);
                    return node.Operator switch
                    {
                        "=" => c == 0,
                        "<>" => c != 0,
                        "<" => c < 0,
                        ">" => c > 0,
                        "<=" => c <= 0,
                        _ => c >= 0,
                    };
                }
            default:
                throw new CalculatedExpressionException($"Unsupported operator '{node.Operator}'.");
        }
    }

    private static bool? In(CalcIn node, Func<string, object?> value)
    {
        object? operand = Evaluate(node.Operand, value);
        if (operand is null) return null;
        foreach (CalcNode candidate in node.Values)
        {
            object? other = Evaluate(candidate, value);
            if (other is not null && Compare(operand, other) == 0) return true;
        }
        return false;
    }

    private static object? Call(CalcCall node, Func<string, object?> value)
    {
        string name = node.Name;

        // Access's Expression Builder lists the '$' name variants (Left$, Trim$, UCase$, Mid$ …) and its
        // designer accepts one in a calculated column — but ACE then fails EVERY insert into that table with
        // "Error evaluating CHECK constraint", while the plain name works. The column is unusable in Access
        // itself, so refusing is matching it, not falling short of it.
        if (name.EndsWith('$'))
            throw new CalculatedExpressionException(
                $"'{name}' cannot be evaluated in a calculated column: the '$' variants are not supported there; "
                + $"use '{name[..^1]}'.");

        // IIf and Choose SHORT-CIRCUIT: only the selected branch is evaluated, matching both the Access
        // expression service and LibRed.Engine's own evaluator. (Access's VBA-side IIf famously does NOT
        // short circuit, but VBA is not what computes a calculated column.) This is more than an
        // optimisation now that an argument can THROW rather than merely be Null —
        // IIf(IsNull([x]), 0, CDbl([x])) is the way to keep CDbl away from a Null, and it only works if the
        // CDbl arm never runs.
        switch (name.ToUpperInvariant())
        {
            case "IIF":
                {
                    if (node.Arguments.Count != 3)
                        throw new CalculatedExpressionException(
                            $"{name} takes 3 arguments, not {node.Arguments.Count}.");
                    object? condition = Evaluate(node.Arguments[0], value);
                    return Evaluate(node.Arguments[condition is not null && IifCondition(condition) ? 1 : 2], value);
                }
            case "CHOOSE":
                {
                    if (node.Arguments.Count < 2)
                        throw new CalculatedExpressionException($"{name} needs at least two arguments.");
                    object? indexValue = Evaluate(node.Arguments[0], value);
                    if (indexValue is null)
                        throw new InvalidOperationException("Data type mismatch in criteria expression: Choose() index is null.");
                    double index = Math.Truncate(ToNumber(indexValue));
                    object?[] choices = node.Arguments.Skip(1).Select(a => Evaluate(a, value)).ToArray();
                    return index < 1 || index > choices.Length ? null : choices[(int)index - 1];
                }
        }

        object?[] args = [.. node.Arguments.Select(a => Evaluate(a, value))];

        switch (name.ToUpperInvariant())
        {
            case "ISNULL":
                Arity(name, args, 1);
                return args[0] is null;
            // VBA's IsEmpty asks whether a Variant was never initialised, which a stored column value never
            // is — measured against ACE, which returns False for a column holding a value AND for one
            // holding Null. It is on the whitelist but is a constant here.
            case "ISEMPTY":
                Arity(name, args, 1);
                return false;
        }

        // The VBA conversions RAISE on a Null argument instead of propagating it, and CDbl is the only
        // conversion a calculated column may contain. ACE caches that failure as VBA error 94 and its own
        // reader then refuses the entire row; LibRed refuses the WRITE instead — a deliberate divergence, so
        // that a row neither engine can read afterwards never reaches the file and no compact-and-repair is
        // needed to get rid of it (page-02e-calculated-columns).
        if (args.Length > 0 && args[0] is null && name.Equals("CDbl", StringComparison.OrdinalIgnoreCase))
            throw new CalculatedExpressionException(
                "CDbl cannot convert Null in a calculated column. Give the column a value, or guard the "
                + "expression, e.g. IIf(IsNull([x]), 0, CDbl([x])).");

        // Everything else propagates Null through any argument.
        if (args.Any(a => a is null)) return null;

        return name.ToUpperInvariant() switch
        {
            "ABS" => Math.Abs(Num(name, args, 1)),
            "SGN" => (double)Math.Sign(Num(name, args, 1)),
            "INT" => Math.Floor(Num(name, args, 1)),
            "FIX" => Math.Truncate(Num(name, args, 1)),
            // Jet VBA math functions (double precision). SQR = sqrt, ATN = atan, LOG = natural log.
            // ACE's Tan is its Sin over its Cos, which differs from a direct tangent in the last digit.
            "SIN" => UnaryDouble(name, args, Trigonometric(Math.Sin)),
            "COS" => UnaryDouble(name, args, Trigonometric(Math.Cos)),
            "TAN" => UnaryDouble(name, args, Trigonometric(x => Math.Sin(x) / Math.Cos(x))),
            "ATN" => UnaryDouble(name, args, Math.Atan),
            "EXP" => UnaryDouble(name, args, Math.Exp),
            "LOG" => UnaryDouble(name, args, Math.Log),
            "SQR" => UnaryDouble(name, args, Math.Sqrt),
            "CDBL" => Num(name, args, 1),
            // Round is banker's rounding in VBA, and its second argument is genuinely optional here.
            "ROUND" => Round(args, name),

            "LEN" => (double)ToText(args[0]).Length,
            "LCASE" => ToText(Single(name, args)).ToLowerInvariant(),
            "UCASE" => ToText(Single(name, args)).ToUpperInvariant(),
            "TRIM" => ToText(Single(name, args)).Trim(TrimmedSpaces),
            "SPACE" => new string(' ', Count(Single(name, args))),
            "ASC" => (int)Ansi.GetBytes(FirstCharacter(Single(name, args)))[0],
            "STR" => Single(name, args) is DateTime date ? ToText(date) : Types.VbaText.Str(args[0] is string s ? ToNumber(s) : args[0]!),
            "STRING" => Repeat(args),
            "LEFT" => Take(args, name, fromLeft: true),
            "RIGHT" => Take(args, name, fromLeft: false),
            // Mid needs all three arguments in a calculated column, optional in VBA or not; InStr needs three
            // or four but not two. A wrong count is ACE's "Syntax error in expression", NOT its policy
            // refusal — measured, and the reverse of what an unknown function name gets.
            "MID" => Mid(args, name),
            "INSTR" => InStr(args, name),

            "YEAR" => (double)ToDate(Single(name, args)).Year,
            "MONTH" => (double)ToDate(Single(name, args)).Month,
            "DAY" => (double)ToDate(Single(name, args)).Day,
            "HOUR" => (double)ToDate(Single(name, args)).Hour,
            "MINUTE" => (double)ToDate(Single(name, args)).Minute,
            "SECOND" => (double)ToDate(Single(name, args)).Second,
            "WEEKDAY" => DaysIntoWeek(ToDate(args[0]!), FirstDayOfWeek(args, 1)) + 1,
            "DATESERIAL" => DateSerial(args, name),
            "TIMESERIAL" => TimeSerial(args, name),
            "MONTHNAME" => MonthName(args, name),
            "WEEKDAYNAME" => WeekdayName(args, name),

            "PMT" => Pmt(args, name),
            "FV" => Fv(args, name),
            "PV" => Pv(args, name),
            "NPER" => NPer(args, name),
            "IPMT" => IPmt(args, name),
            "PPMT" => PPmt(args, name),
            "RATE" => Rate(args, name),
            "SLN" => Sln(args, name),
            "SYD" => Syd(args, name),
            "DDB" => Ddb(args, name),

            _ => throw new CalculatedExpressionException(
                $"LibRed cannot evaluate '{name}' in a calculated column yet."),
        };
    }

    // ---------------------------------------------------------------- function helpers

    // What Trim, LTrim and RTrim strip (verified vs ACE): the space and the ideographic space U+3000, in any mixture —
    // not a tab, CR, LF, no-break space, the other Unicode spaces or a zero-width one.
    private static readonly char[] TrimmedSpaces = [' ', '　'];

    private static void Arity(string name, object?[] args, int expected)
    {
        if (args.Length != expected)
            throw new CalculatedExpressionException($"{name} takes {expected} arguments, not {args.Length}.");
    }

    /// <summary>Arity for a function whose optional arguments really may be omitted. Which functions those
    /// are is per-function and measured, not a rule: <c>Mid</c> and <c>InStr</c> demand every argument while
    /// <c>Round</c>, <c>Weekday</c>, <c>MonthName</c> and <c>WeekdayName</c> do not.</summary>
    private static void Arity(string name, object?[] args, int min, int max)
    {
        if (args.Length < min || args.Length > max)
            throw new CalculatedExpressionException(
                $"{name} takes {min} to {max} arguments, not {args.Length}.");
    }

    private static object Single(string name, object?[] args) { Arity(name, args, 1); return args[0]!; }

    /// <summary>
    /// Sqr, Exp, Log, Sin, Cos, Tan and Atn, on a Double read as the conversion functions read it; NULL-propagating.
    /// A result that is not a number — the square root or log of a number below its domain, or a trigonometric
    /// function of a number too large to reduce — is an invalid procedure call, and a result past a Double an
    /// overflow (verified vs ACE).
    /// </summary>
    private static double UnaryDouble(string name, object?[] args, Func<double, double> op)
    {
        double x = Num(name, args, 1);
        double result = op(x);
        return double.IsNaN(result) || double.IsNegativeInfinity(result) && x == 0
            ? throw new ArgumentException($"Invalid procedure call: the function is not defined at {x}.")
            : Finite(result);
    }

    /// <summary>A trigonometric function, which ACE refuses for an argument of 9.223372E18 or more (verified).</summary>
    private static Func<double, double> Trigonometric(Func<double, double> op) =>
        x => Math.Abs(x) >= 9.223372E18 ? double.NaN : op(x);

    /// <summary>
    /// Access <c>Round(number[, places])</c> (verified vs ACE): half to even on the number's decimal form — a Double or
    /// Single as OLE Automation writes it, so <c>Round(2.675, 2)</c> is 2.68 and <c>Round('2.55', 1)</c> 2.6 — and the
    /// number unchanged when nothing is cut. The operand's type is kept (the EF contract); text and dates give a
    /// Double and True is -1. Places are read as a number and
    /// rounded; Null places give Null, fewer than 0 are an invalid procedure call, and more than a Decimal holds leave
    /// the number as it is.
    /// </summary>
    private static object Round(object?[] args, string name)
    {
        Arity(name, args, 1, 2);
        object value = args[0]!;
        int places = args.Length > 1 ? Count(args[1]!) : 0;
        places = Math.Min(places, 28);

        return value switch
        {
            decimal m => decimal.Round(m, places, MidpointRounding.ToEven),
            double d => RoundDouble(d, places),
            float s => RoundWritten(s, places, () => JetDecimalConverter.FromSingle(s), r => (float)JetDecimalConverter.FromDecimal(r)),
            long or ulong => Convert.ToInt64(value is bool b ? (b ? -1 : 0) : value, CultureInfo.InvariantCulture),
            int or short or byte or bool => Convert.ToInt32(value is bool b ? (b ? -1 : 0) : value, CultureInfo.InvariantCulture),
            _ => RoundDouble(ToNumber(value), places),
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
    /// IIf's condition, which unlike the others is rounded to a whole number first, half to even (verified vs ACE:
    /// 0.4, 0.5, '0.4' and a date at 06:00 on 1899-12-30 are False, 0.6 and 1E+20 are True). Text that is not a
    /// number is True.
    /// </summary>
    private static bool IifCondition(object value) => value switch
    {
        bool b => b,
        string or char => !(Types.VbaText.TryTextAsNumber(value.ToString()!) is { } number && Math.Round(number, MidpointRounding.ToEven) == 0),
        DateTime d => Math.Round(d.ToOADate(), MidpointRounding.ToEven) != 0,
        Guid or byte[] => true,
        _ => Math.Round(ToNumber(value), MidpointRounding.ToEven) != 0,
    };

    /// <summary>VBA <c>CBool</c> (verified vs ACE): "True" and "False" as written, otherwise whether the value
    /// read as a number is non-zero, so 0.5, '$5' and a date are True.</summary>
    private static bool VbaBool(object v) =>
        v is bool b ? b
        : v is string s && bool.TryParse(s, out bool parsed) ? parsed
        : ToNumber(v) != 0;

    private static double Finite(double value) =>
        double.IsFinite(value) ? value : throw new OverflowException("Overflow: the result is too large for a number.");

    /// <summary>The system ANSI code page, which Asc and Chr work in; unmappable characters take their best fit.</summary>
    private static System.Text.Encoding Ansi =>
        System.Text.CodePagesEncodingProvider.Instance.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage) ?? System.Text.Encoding.Latin1;

    private static char AnsiCharacter(int code) => Ansi.GetString([(byte)code])[0];

    /// <summary>The first character of a value as text; an empty text is an invalid procedure call.</summary>
    private static string FirstCharacter(object v)
    {
        string text = ToText(v);
        return text.Length > 0
            ? text[..1]
            : throw new ArgumentException("Invalid procedure call: the text is empty.");
    }

    /// <summary>A whole-number argument, where outside <paramref name="least"/>-<paramref name="most"/> is an invalid
    /// procedure call.</summary>
    private static int InRange(int n, int least, int most = int.MaxValue) =>
        n >= least && n <= most ? n : throw new ArgumentException($"Invalid procedure call: {n} is out of range.");

    /// <summary>A count, length or position read as CLng reads it; below <paramref name="least"/> is an invalid
    /// procedure call.</summary>
    private static int Count(object v, int least = 0) => InRange(AsLong(v), least);

    /// <summary>A value read as CInt reads it: half to even, and past an Integer is an overflow.</summary>
    private static int AsInteger(object v) => Convert.ToInt16(ToNumber(v), CultureInfo.InvariantCulture);

    /// <summary>A value read as CLng reads it: half to even, and past a Long is an overflow.</summary>
    private static int AsLong(object v) => Convert.ToInt32(ToNumber(v), CultureInfo.InvariantCulture);

    /// <summary>A setting read as CInt reads it (half to even; verified vs ACE: '2' and 1.5 are 2); outside
    /// <paramref name="lowest"/>-<paramref name="highest"/> is an invalid procedure call.</summary>
    private static int Setting(object v, int lowest, int highest) => InRange(AsInteger(v), lowest, highest);

    private static double Num(string name, object?[] args, int expected)
    {
        Arity(name, args, expected);
        return ToNumber(args[0]!);
    }

    private static string Repeat(object?[] args)
    {
        if (args.Length != 2) throw new CalculatedExpressionException("String takes two arguments.");
        int count = Count(args[0]!);
        object charValue = args[1]!;
        char ch = charValue is string or Guid or byte[]? FirstCharacter(charValue)[0]
            : AnsiCharacter(AsLong(charValue) & 0xFF);
        return new string(ch, count);
    }

    private static string Take(object?[] args, string name, bool fromLeft)
    {
        if (args.Length != 2) throw new CalculatedExpressionException($"{name} takes two arguments.");
        string s = ToText(args[0]);
        int n = Count(args[1]!);
        return fromLeft
            ? (n <= 0 ? "" : n >= s.Length ? s : s[..n])
            : (n <= 0 ? "" : n >= s.Length ? s : s[^n..]);
    }

    private static string Mid(object?[] args, string name)
    {
        if (args.Length != 3)
            throw new CalculatedExpressionException(
                $"{name} needs all three arguments in a calculated column, even though the third is optional in VBA.");
        string s = ToText(args[0]);
        int start = Count(args[1]!, least: 1);
        int length = Count(args[2]!);
        int from = start - 1;
        if (from >= s.Length) return "";
        return s.Substring(from, Math.Min(length, s.Length - from));
    }

    /// <summary>Measured against ACE: three or four arguments, never two — <c>InStr([A],"a")</c> is a syntax
    /// error, <c>InStr(1,[A],"a")</c> and <c>InStr(1,[A],"a",0)</c> are both accepted and both populate. With
    /// <c>compare</c> omitted the match is case-<b>insensitive</b>: <c>InStr(1,"hello","H")</c> is 1 while an
    /// explicit <c>,0</c> gives 0. That is Access's <c>Option Compare Database</c> default rather than VBA's
    /// binary one, so defaulting to 0 the way VBA does would silently miss matches.</summary>
    private static double InStr(object?[] args, string name)
    {
        if (args.Length is not (3 or 4))
            throw new CalculatedExpressionException(
                $"{name} takes three or four arguments in a calculated column; the two-argument form VBA "
                + "allows is a syntax error here.");
        int start = Count(args[0]!, least: 1);
        bool binary = args.Length > 3 && AsLong(args[3]!) switch
        {
            0 => true,
            1 => false,
            var mode when mode > 2 && IsLocaleId(mode) => false,
            var mode => throw new ArgumentException($"Invalid procedure call: {mode} is not a comparison."),
        };
        string s1 = args[1] is byte[] b1 ? new(Array.ConvertAll(b1, b => (char)b)) : ToText(args[1]);
        string s2 = args[2] is byte[] b2 ? new(Array.ConvertAll(b2, b => (char)b)) : ToText(args[2]);
        if (s1.Length == 0) return 0;
        if (s2.Length == 0) return start;
        if (start > s1.Length) return 0;
        int index = s1.IndexOf(s2, start - 1, binary ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        return index + 1;
    }

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

    private static DateTime DateSerial(object?[] args, string name)
    {
        if (args.Length != 3) throw new CalculatedExpressionException($"{name} takes three arguments.");
        int year = AsInteger(args[0]!), month = AsInteger(args[1]!), day = AsInteger(args[2]!);
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
    /// A firstdayofweek argument: 1 (Sunday) to 7 (Saturday), or 0 for the system's first day; anything else is an
    /// invalid procedure call (verified vs ACE: 0 is Monday under en-AU, and 8 and -1 are refused).
    /// </summary>
    private static DayOfWeek FirstDayOfWeek(object?[] args, int index, int absent = 1) =>
        (args.Length <= index ? absent : Setting(args[index]!, 0, 7)) switch
        {
            0 => CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek,
            var day => (DayOfWeek)(day - 1),
        };

    /// <summary>How many days of the week come before the date's day, for a week starting on <paramref name="first"/>.</summary>
    private static int DaysIntoWeek(DateTime date, DayOfWeek first) => ((int)date.DayOfWeek - (int)first + 7) % 7;

    private static DateTime TimeSerial(object?[] args, string name)
    {
        if (args.Length != 3) throw new CalculatedExpressionException($"{name} takes three arguments.");
        int h = AsInteger(args[0]!), m = AsInteger(args[1]!), s = AsInteger(args[2]!);
        return JetEpoch.AddDays((h * 3600 + m * 60 + s) / 86400.0);
    }

    /// <summary>Access <c>MonthName(month, [abbreviate])</c>. Measured accepted with the optional omitted,
    /// unlike <c>Mid</c> and <c>InStr</c> — the "supply every optional" rule is per-function.</summary>
    private static string MonthName(object?[] args, string name)
    {
        Arity(name, args, 1, 2);
        int number = Setting(args[0]!, 1, 12);
        return args.Length > 1 && VbaBool(args[1]!)
            ? EnUs.DateTimeFormat.GetAbbreviatedMonthName(number)
            : EnUs.DateTimeFormat.GetMonthName(number);
    }

    /// <summary>Access <c>WeekdayName(weekday, [abbreviate], [firstDayOfWeek])</c>, likewise measured
    /// accepted with its optionals omitted.</summary>
    private static string WeekdayName(object?[] args, string name)
    {
        Arity(name, args, 1, 3);
        DayOfWeek first = FirstDayOfWeek(args, 2, absent: 0);
        int index = ((int)first + Setting(args[0]!, 1, 7) - 1) % 7;
        bool abbreviate = args.Length > 1 && VbaBool(args[1]!);
        return (abbreviate ? EnUs.DateTimeFormat.AbbreviatedDayNames : EnUs.DateTimeFormat.DayNames)[index];
    }

    // ---------------------------------------------------------------- financial
    //
    // All ten of these are on ACE's calculated-column whitelist. They are ported from LibRed.Engine's
    // ExpressionEvaluator rather than re-derived, so a formula cannot answer one way through SQL and another
    // through a calculated column. Two copies exist only because of the layering: this evaluation happens in
    // Core at row-encode time, and Core cannot reach Engine (docs/format/page-02e-calculated-columns.md).

    /// <summary>VBA <c>Pmt(rate, nper, pv, [fv], [type])</c>: the constant payment for an annuity.</summary>
    private static double Pmt(object?[] args, string name)
    {
        Arity(name, args, 3, 5);
        return Pmt(Fin(args, 0), Fin(args, 1), Fin(args, 2), Fin(args, 3, 0), Due(Fin(args, 4, 0)));
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

    /// <summary>VBA <c>FV(rate, nper, pmt, [pv], [type])</c>: the future value of an annuity.</summary>
    private static double Fv(object?[] args, string name)
    {
        Arity(name, args, 3, 5);
        return Fv(Fin(args, 0), Fin(args, 1), Fin(args, 2), Fin(args, 3, 0), Due(Fin(args, 4, 0)));
    }

    /// <summary>VBA <c>PV(rate, nper, pmt, [fv], [type])</c>: the present value of an annuity.</summary>
    private static double Pv(object?[] args, string name)
    {
        Arity(name, args, 3, 5);
        return Pv(Fin(args, 0), Fin(args, 1), Fin(args, 2), Fin(args, 3, 0), Due(Fin(args, 4, 0)));
    }

    /// <summary>VBA <c>NPer(rate, pmt, pv, [fv], [type])</c>: the number of periods for an annuity.</summary>
    private static double NPer(object?[] args, string name)
    {
        Arity(name, args, 3, 5);
        return NPer(Fin(args, 0), Fin(args, 1), Fin(args, 2), Fin(args, 3, 0), Due(Fin(args, 4, 0)));
    }

    /// <summary>VBA <c>IPmt(rate, per, nper, pv, [fv], [type])</c>: the interest portion of one payment.</summary>
    private static double IPmt(object?[] args, string name)
    {
        Arity(name, args, 4, 6);
        return IPmt(Fin(args, 0), Fin(args, 1), Fin(args, 2), Fin(args, 3), Fin(args, 4, 0), Due(Fin(args, 5, 0)));
    }

    /// <summary>VBA <c>PPmt(rate, per, nper, pv, [fv], [type])</c>: the principal portion, Pmt minus IPmt.</summary>
    private static double PPmt(object?[] args, string name)
    {
        Arity(name, args, 4, 6);
        return PPmt(Fin(args, 0), Fin(args, 1), Fin(args, 2), Fin(args, 3), Fin(args, 4, 0), Due(Fin(args, 5, 0)));
    }

    /// <summary>VBA <c>SLN(cost, salvage, life)</c>: straight-line depreciation.</summary>
    private static double Sln(object?[] args, string name)
    {
        Arity(name, args, 3);
        return Sln(Fin(args, 0), Fin(args, 1), Fin(args, 2));
    }

    /// <summary>VBA <c>SYD(cost, salvage, life, period)</c>: sum-of-years'-digits depreciation.</summary>
    private static double Syd(object?[] args, string name)
    {
        Arity(name, args, 4);
        return Syd(Fin(args, 0), Fin(args, 1), Fin(args, 2), Fin(args, 3));
    }

    /// <summary>VBA <c>DDB(cost, salvage, life, period, [factor])</c>: double-declining-balance depreciation,
    /// clamped so the book value never drops below salvage.</summary>
    private static double Ddb(object?[] args, string name)
    {
        Arity(name, args, 4, 5);
        return Ddb(Fin(args, 0), Fin(args, 1), Fin(args, 2), Fin(args, 3), Fin(args, 4, 2));
    }

    /// <summary>VBA <c>Rate(nper, pmt, pv, [fv], [type], [guess])</c>: the per-period rate. There is no closed
    /// form, so it is solved by Newton-Raphson on the annuity equation, from the caller's guess.</summary>
    private static double Rate(object?[] args, string name)
    {
        Arity(name, args, 3, 6);
        return Rate(Fin(args, 0), Fin(args, 1), Fin(args, 2), Fin(args, 3, 0), Due(Fin(args, 4, 0)), Fin(args, 5, 0.1));
    }

    private static double Fin(object?[] args, int index) => ToNumber(args[index]!);

    private static double Fin(object?[] args, int index, double fallback)
        => args.Length > index ? ToNumber(args[index]!) : fallback;

    // ---------------------------------------------------------------- coercion

    /// <summary>Month and day names come from a fixed en-US, not the ambient culture: the value is written
    /// into the file, so it must not depend on who wrote the row.</summary>
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    /// <summary>The OLE Automation epoch Jet dates count from (see the heritage note in CLAUDE.md).</summary>
    private static readonly DateTime JetEpoch = new(1899, 12, 30);

    private static double ToNumber(object value) => value switch
    {
        double d => d,
        float f => f,
        decimal m => JetDecimalConverter.FromDecimal(m),
        bool b => b ? -1 : 0,                        // VARIANT_TRUE is -1, not 1
        DateTime t => t.ToOADate(),
        string s when Types.VbaText.TryTextAsNumber(s) is double parsed => parsed,
        byte or short or int or long => Convert.ToDouble(value, CultureInfo.InvariantCulture),
        _ => throw new CalculatedExpressionException($"Cannot use '{value}' as a number."),
    };

    private static bool ToBoolean(object value) => value is bool b ? b : ToNumber(value) != 0;

    // As CStr writes it, which is what ACE caches.
    private static string ToText(object? value) => value is null ? "" : Types.VbaText.Of(value);

    private static DateTime ToDate(object value) => value is DateTime d ? d : DateTime.FromOADate(ToNumber(value));

    private static int Compare(object left, object right)
    {
        if (left is string || right is string)
            return string.Compare(ToText(left), ToText(right), StringComparison.OrdinalIgnoreCase);
        if (left is DateTime || right is DateTime)
            return ToDate(left).CompareTo(ToDate(right));
        return ToNumber(left).CompareTo(ToNumber(right));
    }

    /// <summary>Access <c>Like</c>: <c>*</c> any run, <c>?</c> one character, <c>#</c> one digit.</summary>
    private static bool Like(string text, string pattern)
    {
        var regex = new System.Text.StringBuilder("^");
        foreach (char c in pattern)
            regex.Append(c switch
            {
                '*' => ".*",
                '?' => ".",
                '#' => "[0-9]",
                _ => System.Text.RegularExpressions.Regex.Escape(c.ToString()),
            });
        regex.Append('$');
        return System.Text.RegularExpressions.Regex.IsMatch(
            text, regex.ToString(), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>Coerces an evaluated result to the column's <c>ResultType</c>, which is what the payload is
    /// encoded in.</summary>
    public static object? Coerce(object? value, JetDataType type)
    {
        if (value is null) return null;
        try
        {
            return type switch
            {
                JetDataType.Boolean => ToBoolean(value),
                JetDataType.Byte => checked((byte)Math.Round(ToNumber(value), MidpointRounding.ToEven)),
                JetDataType.Int16 => checked((short)Math.Round(ToNumber(value), MidpointRounding.ToEven)),
                JetDataType.Int32 => checked((int)Math.Round(ToNumber(value), MidpointRounding.ToEven)),
                JetDataType.Int64 => checked((long)Math.Round(ToNumber(value), MidpointRounding.ToEven)),
                JetDataType.Single => (float)ToNumber(value),
                JetDataType.Double => ToNumber(value),
                // A Decimal or text as storing it takes it, exactly (verified vs ACE: [N] over a Decimal or text 1.00005 is
                // 1.0000, where the Double 1.00005 is 1.0001).
                JetDataType.Currency => Types.JetTypeCodec.CurrencyFromScaled(Types.JetTypeCodec.CurrencyToScaled(
                    value is decimal || value is string s && Types.VbaText.TryTextAsNumber(s) is not null
                        ? Types.JetTypeCodec.NumberFromText(type, value)
                        : ToNumber(value),
                    CultureInfo.InvariantCulture)),
                JetDataType.DateTime => ToDate(value),
                JetDataType.Text or JetDataType.Memo => ToText(value),
                _ => throw new CalculatedExpressionException($"Cannot store a calculated {type} value."),
            };
        }
        catch (OverflowException)
        {
            throw new CalculatedExpressionException(
                $"VBA error 6, overflow: the expression's result {value} does not fit its {type} result type.");
        }
    }
}