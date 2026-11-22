using EntityFrameworkCore.Jet.Data;
using LibRed.Catalog;
using System.Globalization;
using Operators = LibRed.Storage.Types.ExpressionFunctions<LibRed.Storage.Calculated.CalcNode>;

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
    /// <summary>Evaluates <paramref name="node"/>; <paramref name="value"/> resolves a column by name, and
    /// <paramref name="databaseCollation"/> is the database's collation, which text compares in and an InStr that names
    /// no comparison compares under; without one such an expression is refused.</summary>
    public static object? Evaluate(CalcNode node, Func<string, object?> value, Collation? databaseCollation) => node switch
    {
        CalcLiteral l => l.Value,
        CalcColumn c => value(c.Name),
        CalcUnary u => Unary(u, value, databaseCollation),
        CalcBinary b => Binary(b, value, databaseCollation),
        CalcIsNull i => Evaluate(i.Operand, value, databaseCollation) is null != i.Negated,
        CalcIn i => In(i, value, databaseCollation),
        CalcCall f => Call(f, value, databaseCollation),
        _ => throw new CalculatedExpressionException($"Unsupported expression node {node.GetType().Name}."),
    };

    private static object? Unary(CalcUnary node, Func<string, object?> value, Collation? databaseCollation)
    {
        object? operand = Evaluate(node.Operand, value, databaseCollation);
        if (operand is null) return null;
        return node.Operator switch
        {
            "-" => Operators.Negate(operand),
            "Not" => !Operators.IsTruthy(operand),
            _ => throw new CalculatedExpressionException($"Unsupported unary operator '{node.Operator}'."),
        };
    }

    private static object? Binary(CalcBinary node, Func<string, object?> value, Collation? databaseCollation)
    {
        object? left = Evaluate(node.Left, value, databaseCollation);
        object? right = Evaluate(node.Right, value, databaseCollation);

        // '&' is the one operator that does NOT propagate Null: it concatenates it as "".
        if (node.Operator == "&")
            return (left is null ? "" : Operators.ConcatText(left)) + (right is null ? "" : Operators.ConcatText(right));

        if (left is null || right is null) return null;

        switch (node.Operator)
        {
            // '+' concatenates when BOTH sides are text, and adds otherwise — the overload that makes it the
            // null-propagating sibling of '&'.
            case "+": return Operators.Add(left, right);
            case "-": return Operators.Arithmetic(Operators.NumericOperand(left)!, Operators.NumericOperand(right)!, '-');
            case "*": return Operators.Arithmetic(Operators.NumericOperand(left)!, Operators.NumericOperand(right)!, '*');
            case "/": return Operators.Divide(Operators.NumericOperand(left)!, Operators.NumericOperand(right)!);
            case "^": return Operators.Power(Operators.NumericOperand(left)!, Operators.NumericOperand(right)!);
            case "And": return Operators.IsTruthy(left) && Operators.IsTruthy(right);
            case "Or": return Operators.IsTruthy(left) || Operators.IsTruthy(right);
            case "Like": return Types.LikeMatcher.IsMatch(Operators.ConcatText(left), Operators.ConcatText(right), ansi89: true);
            case "=":
            case "<>":
            case "<":
            case ">":
            case "<=":
            case ">=":
                {
                    int c = Operators.CompareValues(left, right, TextIn(databaseCollation));
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

    private static bool? In(CalcIn node, Func<string, object?> value, Collation? databaseCollation)
    {
        object? operand = Evaluate(node.Operand, value, databaseCollation);
        if (operand is null) return null;
        foreach (CalcNode candidate in node.Values)
        {
            object? other = Evaluate(candidate, value, databaseCollation);
            if (other is not null && Operators.CompareValues(operand, other, TextIn(databaseCollation)) == 0) return true;
        }
        return false;
    }

    /// <summary>
    /// A function call. What a calculated column refuses is decided here — the functions ACE's whitelist lets one
    /// contain, the argument counts it accepts there, and the Null arguments whose cached error LibRed refuses to write.
    /// The function itself is LibRed.Engine's, the one <see cref="Types.ExpressionFunctions{TExpression}"/> holds for
    /// queries and calculated columns alike.
    /// </summary>
    private static object? Call(CalcCall node, Func<string, object?> value, Collation? databaseCollation)
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

        string function = name.ToUpperInvariant();
        int count = node.Arguments.Count;

        // Mid needs all three arguments in a calculated column, optional in VBA or not; InStr needs three
        // or four but not two. A wrong count is ACE's "Syntax error in expression", NOT its policy
        // refusal — measured, and the reverse of what an unknown function name gets. Which optionals may be
        // omitted is per-function and measured, not a rule: Round, Weekday, MonthName and WeekdayName may.
        (int Min, int Max) arity = function switch
        {
            "ISNULL" or "ISEMPTY" or "ABS" or "SGN" or "INT" or "FIX" or "SIN" or "COS" or "TAN" or "ATN" or "EXP"
                or "LOG" or "SQR" or "CDBL" or "LEN" or "LCASE" or "UCASE" or "TRIM" or "SPACE" or "ASC" or "STR"
                or "YEAR" or "MONTH" or "DAY" or "HOUR" or "MINUTE" or "SECOND" => (1, 1),
            "ROUND" or "WEEKDAY" or "MONTHNAME" => (1, 2),
            "WEEKDAYNAME" => (1, 3),
            "STRING" or "LEFT" or "RIGHT" => (2, 2),
            "CHOOSE" => (2, int.MaxValue),
            "IIF" or "MID" or "DATESERIAL" or "TIMESERIAL" or "SLN" => (3, 3),
            "INSTR" => (3, 4),
            "PMT" or "FV" or "PV" or "NPER" => (3, 5),
            "RATE" => (3, 6),
            "SYD" => (4, 4),
            "DDB" => (4, 5),
            "IPMT" or "PPMT" => (4, 6),
            _ => throw new CalculatedExpressionException(
                $"LibRed cannot evaluate '{name}' in a calculated column yet."),
        };
        if (function == "MID" && count != 3)
            throw new CalculatedExpressionException(
                $"{name} needs all three arguments in a calculated column, even though the third is optional in VBA.");
        // Measured against ACE: InStr([A],"a") is a syntax error, InStr(1,[A],"a") and InStr(1,[A],"a",0) are both
        // accepted and both populate.
        if (function == "INSTR" && count is not (3 or 4))
            throw new CalculatedExpressionException(
                $"{name} takes three or four arguments in a calculated column; the two-argument form VBA "
                + "allows is a syntax error here.");
        if (count < arity.Min || count > arity.Max)
            throw new CalculatedExpressionException(arity.Min == arity.Max
                ? $"{name} takes {arity.Min} arguments, not {count}."
                : arity.Max == int.MaxValue
                    ? $"{name} needs at least {arity.Min} arguments, not {count}."
                    : $"{name} takes {arity.Min} to {arity.Max} arguments, not {count}.");

        // The VBA conversions RAISE on a Null argument instead of propagating it, and CDbl is the only
        // conversion a calculated column may contain. ACE caches that failure as VBA error 94 and its own
        // reader then refuses the entire row; LibRed refuses the WRITE instead — a deliberate divergence, so
        // that a row neither engine can read afterwards never reaches the file and no compact-and-repair is
        // needed to get rid of it (page-02e-calculated-columns).
        if (function == "CDBL" && Evaluate(node.Arguments[0], value, databaseCollation) is null)
            throw new CalculatedExpressionException(
                "CDbl cannot convert Null in a calculated column. Give the column a value, or guard the "
                + "expression, e.g. IIf(IsNull([x]), 0, CDbl([x])).");

        // InStr's compare argument does not propagate Null either: ACE caches an error for it, whatever the strings
        // are — Null too — which its own reader then refuses. As with CDbl, LibRed refuses the write (where a query's
        // InStr, which stores nothing, gives Null).
        if (function == "INSTR" && count == 4 && Evaluate(node.Arguments[3], value, databaseCollation) is null)
            throw new CalculatedExpressionException(
                "InStr's compare argument cannot be Null in a calculated column: ACE caches an error for it.");

        // With compare omitted InStr compares under the database's locale — Access's Option Compare Database, not
        // VBA's binary default — which a row encoded outside a database does not have.
        if (function == "INSTR" && count == 3 && databaseCollation is null)
            throw new CalculatedExpressionException(
                $"{name} without a compare argument compares under the database's locale, and this row is being "
                + "encoded outside a database.");

        return new Functions(value, databaseCollation).Function(function, new(name, node.Arguments));
    }

    /// <summary>The functions over a calculated column's expression, its arguments evaluated as the rest of it is.</summary>
    private sealed class Functions(Func<string, object?> value, Collation? databaseCollation) : Types.ExpressionFunctions<CalcNode>
    {
        public override object? Evaluate(CalcNode expression) => CalculatedEvaluator.Evaluate(expression, value, databaseCollation);

        protected override JetTextComparer Text => TextIn(databaseCollation);

        // Asked only by TypeName, VarType, DataLength, Power and Rnd, none of which a calculated column may contain.
        protected override bool IsCurrency(CalcNode expression) => throw new NotSupportedException();

        protected override object? EvaluatePower(CalcNode left, CalcNode right) => throw new NotSupportedException();

        protected override uint? RandSeed
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }

    // ---------------------------------------------------------------- coercion

    /// <summary>How text compares: in the database's collation, which a row encoded outside a database does not have.</summary>
    private static JetTextComparer TextIn(Collation? databaseCollation) => JetTextComparer.For(databaseCollation
        ?? throw new CalculatedExpressionException(
            "A calculated column's text compares in the database's collation, and this row is being encoded outside a database."));

    /// <summary>Coerces an evaluated result to the column's <c>ResultType</c>, which is what the payload is
    /// encoded in.</summary>
    public static object? Coerce(object? value, JetDataType type)
    {
        if (value is null) return null;
        try
        {
            return type switch
            {
                JetDataType.Boolean => Operators.IsTruthy(value),
                JetDataType.Byte => checked((byte)Math.Round(Operators.Dbl(Operators.ConversionNumber(value)), MidpointRounding.ToEven)),
                JetDataType.Int16 => checked((short)Math.Round(Operators.Dbl(Operators.ConversionNumber(value)), MidpointRounding.ToEven)),
                JetDataType.Int32 => checked((int)Math.Round(Operators.Dbl(Operators.ConversionNumber(value)), MidpointRounding.ToEven)),
                JetDataType.Int64 => checked((long)Math.Round(Operators.Dbl(Operators.ConversionNumber(value)), MidpointRounding.ToEven)),
                JetDataType.Single => (float)Operators.Dbl(Operators.ConversionNumber(value)),
                JetDataType.Double => Operators.Dbl(Operators.ConversionNumber(value)),
                // A Decimal or text as storing it takes it, exactly (verified vs ACE: [N] over a Decimal or text 1.00005 is
                // 1.0000, where the Double 1.00005 is 1.0001).
                JetDataType.Currency => Types.JetTypeCodec.CurrencyFromScaled(Types.JetTypeCodec.CurrencyToScaled(
                    value is decimal || value is string s && Types.VbaText.TryTextAsNumber(s) is not null
                        ? Types.JetTypeCodec.NumberFromText(type, value)
                        : Operators.Dbl(Operators.ConversionNumber(value)),
                    CultureInfo.InvariantCulture)),
                JetDataType.DateTime => Operators.ToDate(value),
                JetDataType.Text or JetDataType.Memo => Operators.ConcatText(value),
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
