namespace LibRed.Storage.Types;

/// <summary>What ACE takes a result to be, as far as its decimal places go (see
/// <see cref="ExpressionFunctions{TExpression}.Product"/> and <see cref="ExpressionFunctions{TExpression}.Sum"/>).</summary>
internal enum NumberClass
{
    Other,
    Whole,
    Text,
    Date,
    Decimal,
    Currency,
    Double,
}

/// <summary>A result's <see cref="NumberClass"/>, with its places when it is a Decimal, and the whole digits it counts
/// beside a Decimal: a Decimal's precision is <c>Integers + Places</c>.</summary>
internal readonly record struct NumberType(NumberClass Class, int Places = 0, int Integers = 0)
{
    /// <summary>A Decimal's precision.</summary>
    public int Precision => Integers + Places;
}

internal abstract partial class ExpressionFunctions<TExpression>
{
    /// <summary>What an expression is to <see cref="NumberTypeOf"/>, as the evaluator over it reads its node.</summary>
    internal abstract record NumberNode
    {
        /// <summary>A value whose type is known as it stands: a written value, a column, <c>^</c>, <c>\</c>.</summary>
        internal sealed record Known(NumberType Type) : NumberNode;

        /// <summary>A Null as written, which a choice's type passes over.</summary>
        internal sealed record WrittenNull : NumberNode;

        internal sealed record Negation(TExpression Operand) : NumberNode;

        internal sealed record Multiplied(TExpression Left, TExpression Right, bool Divide) : NumberNode;

        internal sealed record Added(TExpression Left, TExpression Right, bool Add) : NumberNode;

        /// <summary>An argument whose type the result is, as it is.</summary>
        internal sealed record Same(TExpression Argument) : NumberNode;

        /// <summary>A choice among <paramref name="Arms"/> that is not a function: a CASE.</summary>
        internal sealed record Choice(IEnumerable<TExpression> Arms) : NumberNode;

        /// <summary>A function call; <paramref name="Declared"/> gives its type where nothing below decides it.</summary>
        internal sealed record Function(Call Call, Func<NumberType> Declared) : NumberNode;
    }

    /// <summary>
    /// The type ACE gives <paramref name="expression"/>, as far as its places go, its nodes read by
    /// <paramref name="node"/>: a negation its operand's, <c>*</c> and <c>/</c> a <see cref="Product"/>'s, <c>+</c> and
    /// <c>-</c> a <see cref="Sum"/>'s. <c>CCur</c> is a Currency; <c>Sum</c> keeps its argument's places at a precision of
    /// 28; <c>Min</c>, <c>Max</c>, <c>First</c> and <c>Last</c> keep its type, and so do <c>Round</c>, <c>Abs</c>, <c>Int</c>
    /// and <c>Fix</c> of a Decimal or Currency (verified vs ACE: <c>Round([Price],2) + [Qty]/32</c> over a Currency
    /// <c>Price</c> is a Currency, 12.7188 where a Double would be 12.71875), and a choice its Decimal or Currency arms'.
    /// Any other function is its declared type.
    /// </summary>
    internal static NumberType NumberTypeOf(TExpression expression, Func<TExpression, NumberNode> node)
    {
        NumberType Of(TExpression e) => NumberTypeOf(e, node);

        switch (node(expression))
        {
            case NumberNode.Known known:
                return known.Type;
            case NumberNode.Negation negation:
                return Of(negation.Operand);
            case NumberNode.Multiplied product:
                return Product(Of(product.Left), Of(product.Right), product.Divide);
            case NumberNode.Added sum:
                return Sum(Of(sum.Left), Of(sum.Right), sum.Add);
            case NumberNode.Same same:
                return Of(same.Argument);
            case NumberNode.Choice choice:
                return Chosen(choice.Arms);
            case NumberNode.Function { Call: var call } function:
                string name = TextFold.Fold(call.Name.TrimEnd('$'), TextFold.Letters.None, out _);
                if (name == "CCUR")
                    return new(NumberClass.Currency);
                if (call.Arguments is [var argument] && name == "SUM")
                    return Of(argument) is { Class: NumberClass.Decimal } summed ? summed with { Integers = Most - summed.Places } : Of(argument);
                if (call.Arguments is [var only] && KeepsArgumentType(name))
                    return Of(only);
                // A query's result column declares the same (QueryExecutor.KeptNumberType).
                if (call.Arguments is [var kept, ..] && KeepsNumberType(name)
                    && Of(kept) is { Class: NumberClass.Decimal or NumberClass.Currency } keeps)
                    return keeps;
                return ChoiceArms(name, call.Arguments) is { } arms ? Chosen(arms) : function.Declared();
            default:
                return new(NumberClass.Other);
        }

        // A choice's arms taken together as + takes them, but for two Decimals past 28 digits, which + makes a Double:
        // a choice computes nothing, so it is the Decimal of the larger places that holds either arm, its whole digits
        // cut to fit. A Double arm, or anything else that is not a Decimal or a Currency, leaves the choice to its
        // declared type.
        NumberType Chosen(IEnumerable<TExpression> arms)
        {
            NumberType? chosen = null;
            foreach (TExpression arm in arms)
            {
                if (node(arm) is NumberNode.WrittenNull) continue;
                NumberType type = Of(arm);
                if (type.Class is NumberClass.Double or NumberClass.Other) return new(NumberClass.Other);
                if (chosen is not { } before)
                {
                    chosen = type;
                    continue;
                }
                NumberType both = Sum(before, type, add: false);
                int places = Math.Max(before.Places, type.Places);
                chosen = both.Class == NumberClass.Double ? new(NumberClass.Decimal, places, Most - places) : both;
            }
            return chosen is { Class: NumberClass.Decimal or NumberClass.Currency } kept ? kept : new(NumberClass.Other);
        }
    }

    /// <summary>Whether the aggregate <paramref name="name"/> (upper case) gives its argument's type: Min, Max, First and
    /// Last.</summary>
    internal static bool KeepsArgumentType(string name) => name is "MIN" or "MAX" or "FIRST" or "LAST";

    /// <summary>Whether the function <paramref name="name"/> (upper case) keeps its operand's type: Round, Abs, Int, Fix,
    /// Floor and Ceiling.</summary>
    internal static bool KeepsNumberType(string name) =>
        name is "ROUND" or "ABS" or "INT" or "FIX" or "FLOOR" or "CEILING" or "CEIL";

    /// <summary>The values a choice picks among — IIf's, Switch's, Choose's, Coalesce's, Greatest's and Least's — or null
    /// for a function (<paramref name="name"/>, as written) that is not one.</summary>
    internal static IEnumerable<TExpression>? ChoiceArms(string name, IReadOnlyList<TExpression> arguments) =>
        TextFold.Fold(name.TrimEnd('$'), TextFold.Letters.None, out _) switch
    {
        "IIF" when arguments.Count == 3 => arguments.Skip(1),
        "SWITCH" => arguments.Where((_, i) => i % 2 == 1),
        "CHOOSE" => arguments.Skip(1),
        "COALESCE" or "GREATEST" or "LEAST" => arguments,
        _ => null,
    };

    /// <summary>The type of a value as written: a number with a decimal point a Decimal of the places written less
    /// trailing zeros and a precision of 28, a whole number too big for a Long a Decimal with none, a smaller whole
    /// number or a Boolean a whole number, a Double or Single a Double, text and a date as text and a date count.</summary>
    internal static NumberType WrittenNumberType(object? value) => value switch
    {
        decimal written when (written / 1.0000000000000000000000000000m).Scale is var places =>   // trailing zeros dropped
            new(NumberClass.Decimal, places, Most - places),
        // A whole number too big for a Long is a Decimal to ACE, so Currency * 864000000000 is a Double
        // there rather than an overflowing Currency (verified vs ACE); LibRed keeps the value an Int64.
        long => new(NumberClass.Decimal, 0, Most),
        _ => ColumnNumberType(currency: false, scale: null, precision: null, value?.GetType()),
    };

    /// <summary>The most digits a Decimal holds.</summary>
    protected const int Most = JetTypeCodec.MaxNumericPrecision;

    /// <summary>The type of a column: a Currency column a Currency, a Decimal column of its scale and precision,
    /// otherwise as its CLR <paramref name="type"/> counts.</summary>
    internal static NumberType ColumnNumberType(bool currency, int? scale, int? precision, Type? type)
    {
        if (currency)
            return new(NumberClass.Currency);
        if (scale is int places)
            return new(NumberClass.Decimal, places, (precision ?? Most) - places);
        return type == typeof(double) || type == typeof(float) ? new(NumberClass.Double)
            : type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
                || type == typeof(bool) ? new(NumberClass.Whole, Integers: WholeDigits(type))
            : type == typeof(string) ? new(NumberClass.Text, Integers: Most)
            : type == typeof(DateTime) ? new(NumberClass.Date, Integers: Most)
            : new(NumberClass.Other);
    }

    /// <summary>The whole digits a whole number of <paramref name="type"/> counts beside a Decimal: an Integer's or a
    /// Boolean's five, a Byte's or a Long's ten (verified vs ACE: DECIMAL(4,1) * INTEGER and * BIT are DECIMAL(6,1), *
    /// BYTE and * LONG DECIMAL(11,1)). A BigInt's nineteen are its own digits: ACE makes a BigInt beside a Decimal a
    /// BigInt, where LibRed keeps the Decimal .NET gives.</summary>
    internal static int WholeDigits(Type type) =>
        type == typeof(short) || type == typeof(bool) ? 5 : type == typeof(long) ? 19 : 10;

    private static bool IsWhole(NumberType type) => type.Class is NumberClass.Whole or NumberClass.Text or NumberClass.Date;

    /// <summary>A whole number with a whole number: at least a Long's digits, as ACE works an Integer times an
    /// Integer out as a Long (verified vs ACE: DECIMAL(10,2) * (INTEGER * INTEGER) is DECIMAL(12,2)).</summary>
    private static NumberType Whole(NumberType left, NumberType right) =>
        new(NumberClass.Whole, Integers: Math.Max(WholeDigits(typeof(int)), Math.Max(left.Integers, right.Integers)));

    /// <summary>A Decimal or Currency beside a whole number, a date or text: its own places, the whole digits the
    /// two count between them, up to a precision of 28 (DECIMAL(4,1) * TEXT is DECIMAL(28,1)).</summary>
    private static NumberType Beside(NumberType number, NumberType whole) => number.Class != NumberClass.Decimal ? number
        : number with
        {
            Integers = Math.Min(JetTypeCodec.MaxNumericPrecision - number.Places, Math.Max(number.Integers, whole.Integers)),
        };

    /// <summary>Two Decimals: the larger places and the more whole digits, or a Double where they come to more than
    /// 28 digits.</summary>
    private static NumberType Decimals(NumberType left, NumberType right)
    {
        int places = Math.Max(left.Places, right.Places), integers = Math.Max(left.Integers, right.Integers);
        return integers + places > JetTypeCodec.MaxNumericPrecision
            ? new(NumberClass.Double)
            : new(NumberClass.Decimal, places, integers);
    }

    internal static NumberType Product(NumberType left, NumberType right, bool divide)
    {
        if (left.Class == NumberClass.Other || right.Class == NumberClass.Other)
            return new(NumberClass.Other);
        if (left.Class == NumberClass.Double || right.Class == NumberClass.Double
            || divide && (left.Class == NumberClass.Currency || right.Class == NumberClass.Currency))
            return new(NumberClass.Double);
        if (IsWhole(left) && IsWhole(right))
            return divide ? new(NumberClass.Double) : Whole(left, right);
        if (IsWhole(left)) return Beside(right, left);
        if (IsWhole(right)) return Beside(left, right);
        if (left.Class == NumberClass.Currency && right.Class == NumberClass.Currency)
            return new(NumberClass.Currency);
        if (left.Class == NumberClass.Currency || right.Class == NumberClass.Currency)
            return new(NumberClass.Double);
        return Decimals(left, right);
    }

    internal static NumberType Sum(NumberType left, NumberType right, bool add)
    {
        if (left.Class is NumberClass.Other or NumberClass.Date || right.Class is NumberClass.Other or NumberClass.Date
            || add && left.Class == NumberClass.Text && right.Class == NumberClass.Text)
            return new(NumberClass.Other);
        if (left.Class == NumberClass.Currency && right.Class == NumberClass.Double
            || left.Class == NumberClass.Double && right.Class == NumberClass.Currency)
            return new(NumberClass.Currency);
        if (left.Class == NumberClass.Double || right.Class == NumberClass.Double)
            return new(NumberClass.Double);
        if (IsWhole(left) && IsWhole(right))
            return Whole(left, right);
        if (IsWhole(left)) return Beside(right, left);
        if (IsWhole(right)) return Beside(left, right);
        if (left.Class == NumberClass.Currency && right.Class == NumberClass.Currency)
            return new(NumberClass.Currency);
        if (left.Class == NumberClass.Currency || right.Class == NumberClass.Currency)
        {
            int places = Math.Max(4, Math.Max(left.Places, right.Places));
            return new(NumberClass.Decimal, places, JetTypeCodec.MaxNumericPrecision - places);
        }
        return Decimals(left, right);
    }
}
