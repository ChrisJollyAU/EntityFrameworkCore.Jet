using EntityFrameworkCore.Jet.Data;
using LibRed.Catalog;
using LibRed.Storage.Types;
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
/// <b>not</b> through <c>&amp;</c>, which treats Null beside a value as the empty string (<c>Null &amp; Null</c> is
/// still Null). That asymmetry is the whole
/// mechanism behind the idiom MSDN uses to introduce the feature —
/// <c>[First] &amp; " " &amp; ([Middle] + " ") &amp; [Last]</c> — where <c>+</c> makes the parenthesised
/// group Null when there is no middle initial and <c>&amp;</c> then drops it, so no double space appears.
/// Get the two operators the same way round and that expression silently gains a space.</para>
/// </remarks>
internal static class CalculatedEvaluator
{
    /// <summary>Evaluates <paramref name="node"/>; <paramref name="value"/> resolves a column's value by name,
    /// <paramref name="databaseCollation"/> is the database's collation, which text compares in and an InStr that names
    /// no comparison compares under (without one such an expression is refused), and <paramref name="definition"/>
    /// resolves a column's definition by name, which its type is taken from (<see cref="IsCurrency"/>).</summary>
    public static object? Evaluate(CalcNode node, Func<string, object?> value, Collation? databaseCollation,
        Func<string, ColumnDef?> definition) => node switch
    {
        CalcLiteral l => l.Value,
        CalcColumn c => value(c.Name),
        CalcUnary u => Unary(u, value, databaseCollation, definition),
        CalcBinary b => Binary(b, value, databaseCollation, definition),
        CalcIsNull i => Evaluate(i.Operand, value, databaseCollation, definition) is null != i.Negated,
        CalcIn i => In(i, value, databaseCollation, definition),
        CalcCall f => Call(f, value, databaseCollation, definition),
        _ => throw new CalculatedExpressionException($"Unsupported expression node {node.GetType().Name}."),
    };

    /// <summary>Whether ACE takes <paramref name="node"/> to be a Currency (<see cref="Operators.NumberTypeOf"/>, as a
    /// query's is): a column as its definition (<paramref name="definition"/>) declares it, a function its declared
    /// type none.</summary>
    private static bool IsCurrency(CalcNode node, Func<string, ColumnDef?> definition) =>
        Operators.NumberTypeOf(node, n => n switch
        {
            CalcLiteral { Value: null } => new Operators.NumberNode.WrittenNull(),
            CalcLiteral l => new Operators.NumberNode.Known(Operators.WrittenNumberType(l.Value)),
            CalcColumn c => new Operators.NumberNode.Known(definition(c.Name) is { } column
                && JetClrTypeMap.ValueType(column) is var (clrType, currency, scale, precision)
                ? Operators.ColumnNumberType(currency, scale, precision, clrType)
                : new(NumberClass.Other)),
            CalcUnary { Operator: "-" } u => new Operators.NumberNode.Negation(u.Operand),
            CalcBinary { Operator: "*" or "/" } b => new Operators.NumberNode.Multiplied(b.Left, b.Right, b.Operator == "/"),
            CalcBinary { Operator: "+" or "-" } b => new Operators.NumberNode.Added(b.Left, b.Right, b.Operator == "+"),
            CalcBinary { Operator: "^" } => new Operators.NumberNode.Known(new(NumberClass.Double)),
            CalcCall f => new Operators.NumberNode.Function(new(f.Name, f.Arguments), () => new(NumberClass.Other)),
            _ => new Operators.NumberNode.Known(new(NumberClass.Other)),
        }).Class == NumberClass.Currency;

    // DELIBERATE DUPLICATE (the owner's decision, 2026-11-23; CLAUDE.md "One copy of every function"): Unary, Binary, In
    // and CompareAsKinds dispatch the operators over a CalcNode as ExpressionEvaluator.EvaluateUnary, EvaluateBinary,
    // EvaluateInList and CompareAsKinds do over the SQL AST, calling the same ExpressionFunctions helpers. They are two
    // walks because the two trees have different nodes, and kept apart rather than written over a node shape as
    // NumberTypeOf is. Where they differ, ACE does (each measured vs ACE; this side in CalculatedColumnAccessTests): a
    // query's `= True` tests the truth where this compares with -1; a query's
    // written decimal beside a Double result is a Double, outside the expression service a calculated column is always
    // in; a query's `Like '%'` is Is Not Null; a query's written False beside text counts as computed; and a query's IN
    // with a Null item is Null where this is False (LibRed's decision for queries); a query's bare number column and
    // IIf compare with text as numbers, where here a column is computed and an IIf takes the kind of the arm it
    // chooses; and a written number beside written text, which a query compares as numbers, is here an error ACE
    // caches (see CompareAsKinds). A change to one is made to the other. A CHECK that reads no table, and a DEFAULT,
    // compare as this side does, its = True, False, IN and text-beside-a-number rules (measured;
    // ExpressionEvaluator.Stored.RowOnly), but match LIKE as a query does.
    private static object? Unary(CalcUnary node, Func<string, object?> value, Collation? databaseCollation,
        Func<string, ColumnDef?> definition)
    {
        object? operand = VbaBoolean.Plain(Evaluate(node.Operand, value, databaseCollation, definition));
        if (operand is null) return null;
        return node.Operator switch
        {
            "-" => Operators.Negate(operand),
            "Not" => new Functions(value, databaseCollation, definition).Not(operand),
            _ => throw new CalculatedExpressionException($"Unsupported unary operator '{node.Operator}'."),
        };
    }

    private static object? Binary(CalcBinary node, Func<string, object?> value, Collation? databaseCollation,
        Func<string, ColumnDef?> definition)
    {
        if (node.Operator is "And")
            return new Functions(value, databaseCollation, definition).And(node.Left, node.Right);
        if (node.Operator is "Or")
            return new Functions(value, databaseCollation, definition).Or(node.Left, node.Right);

        object? left = Evaluate(node.Left, value, databaseCollation, definition);
        object? right = Evaluate(node.Right, value, databaseCollation, definition);

        if (node.Operator == "&")
            return Operators.Concat(left, right);

        // The Currency rules are a query's (ExpressionEvaluator.EvaluateBinary), at the same points.
        if (node.Operator is "+" or "-")
            (left, right) = Operators.CurrencySumOperands(left, right, IsCurrency(node, definition));

        // The arithmetic operators other than '+' read text as a number even when the other side is Null (as a query's
        // do: ExpressionEvaluator.EvaluateBinary).
        if (node.Operator is "-" or "*" or "/" or "^")
        {
            left = Operators.NumericOperand(left);
            right = Operators.NumericOperand(right);
        }

        // `x <> Null`, with the Null written on the right, is `x Is Not Null` (as a query's is).
        if (node.Operator == "<>" && node.Right is CalcLiteral { Value: null })
            return left is not null;

        if (left is null || right is null) return null;

        if (node.Operator is "+" or "-" or "*" or "/")
            (left, right) = Operators.CurrencyProductOperands(
                left, right, node.Operator == "/", IsCurrency(node.Left, definition), IsCurrency(node.Right, definition));

        if (node.Operator is "+" or "-" or "*")
        {
            object result = node.Operator switch
            {
                // '+' concatenates when BOTH sides are text, and adds otherwise — the overload that makes it the
                // null-propagating sibling of '&'.
                "+" => Operators.Add(left, right),
                "-" => Operators.Arithmetic(left, right, '-'),
                _ => Operators.Arithmetic(left, right, '*'),
            };
            return Operators.CurrencyStep(result, IsCurrency(node, definition));
        }

        switch (node.Operator)
        {
            case "/": return Operators.Divide(left, right);
            case "^": return Operators.Power(left, right);
            case "Like": return VbaBoolean.Of(Types.LikeMatcher.IsMatch(Operators.ConcatText(left), Operators.ConcatText(right), ansi89: true));
            case "=":
            case "<>":
            case "<":
            case ">":
            case "<=":
            case ">=":
                {
                    int c = CompareAsKinds(node.Left, left, node.Right, right, value, databaseCollation, definition);
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

    /// <summary>Whether an operand's value is one the expression writes: a literal, or the arm an <c>IIf</c> takes when
    /// that arm is written (measured vs ACE: an IIf takes the kind of the arm it chooses, row by row). Anything else — a
    /// column among them, unlike a query's (ExpressionEvaluator.IsComputed) — the expression computes; and unlike a
    /// query's, a written False is written (measured vs ACE: [A] &lt;&gt; False over text is a type mismatch here).</summary>
    private static bool Written(CalcNode operand, Func<string, object?> value, Collation? databaseCollation,
        Func<string, ColumnDef?> definition) => operand switch
        {
            CalcLiteral => true,
            CalcCall call when call.Name.Equals("IIf", StringComparison.OrdinalIgnoreCase) => Written(
                new Functions(value, databaseCollation, definition).IifArm(call.Arguments)!,
                value, databaseCollation, definition),
            _ => false,
        };

    /// <summary>The order of two operands of a comparison or an <c>In</c>, as ACE's expression service orders them
    /// (<see cref="Operators.CompareInService"/>, which a CHECK that reads no table compares through too). Text beside a
    /// number the expression computes, a column's included, compares as text (verified vs ACE: <c>[A] In ([Qty]+0)</c>
    /// over 'hello' and 7 is False, <c>[Qty]+10 &gt; "9"</c> False, and <c>[Qty] = [Qty] &amp; ".0"</c> False). A written
    /// number beside text the expression computes, or a text column, compares as the number the text reads as
    /// (<c>IIf([Qty]&gt;=0,[Qty],0) = [Qty] &amp; ".0"</c> is True for a Null Qty); beside written text it is a type
    /// mismatch ACE caches as an error, which its own reader then refuses — so LibRed refuses the write
    /// (<c>IIf([Qty]&gt;=0,0,1) = "0.0"</c>).</summary>
    private static int CompareAsKinds(CalcNode leftNode, object left, CalcNode rightNode, object right,
        Func<string, object?> value, Collation? databaseCollation, Func<string, ColumnDef?> definition) =>
        Operators.CompareInService(leftNode, left, rightNode, right,
            node => Written(node, value, databaseCollation, definition), TextIn(databaseCollation));

    private static bool? In(CalcIn node, Func<string, object?> value, Collation? databaseCollation,
        Func<string, ColumnDef?> definition)
    {
        object? operand = Evaluate(node.Operand, value, databaseCollation, definition);
        if (operand is null) return null;
        foreach (CalcNode candidate in node.Values)
        {
            object? other = Evaluate(candidate, value, databaseCollation, definition);
            if (other is not null && CompareAsKinds(node.Operand, operand, candidate, other, value, databaseCollation, definition) == 0) return true;
        }
        return false;
    }

    /// <summary>
    /// A function call. What a calculated column refuses is decided here — the functions ACE's whitelist lets one
    /// contain, the argument counts it accepts there, and the Null arguments whose cached error LibRed refuses to write.
    /// The function itself is LibRed.Engine's, the one <see cref="Types.ExpressionFunctions{TExpression}"/> holds for
    /// queries and calculated columns alike.
    /// </summary>
    private static object? Call(CalcCall node, Func<string, object?> value, Collation? databaseCollation,
        Func<string, ColumnDef?> definition)
    {
        string name = node.Name;
        string function = name.TrimEnd('$').ToUpperInvariant();
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
        if (function == "CDBL" && Evaluate(node.Arguments[0], value, databaseCollation, definition) is null)
            throw new CalculatedExpressionException(
                "CDbl cannot convert Null in a calculated column. Give the column a value, or guard the "
                + "expression, e.g. IIf(IsNull([x]), 0, CDbl([x])).");

        // InStr's compare argument does not propagate Null either: ACE caches an error for it, whatever the strings
        // are — Null too — which its own reader then refuses. As with CDbl, LibRed refuses the write (where a query's
        // InStr, which stores nothing, gives Null).
        if (function == "INSTR" && count == 4 && Evaluate(node.Arguments[3], value, databaseCollation, definition) is null)
            throw new CalculatedExpressionException(
                "InStr's compare argument cannot be Null in a calculated column: ACE caches an error for it.");

        // With compare omitted InStr compares under the database's locale — Access's Option Compare Database, not
        // VBA's binary default — which a row encoded outside a database does not have.
        if (function == "INSTR" && count == 3 && databaseCollation is null)
            throw new CalculatedExpressionException(
                $"{name} without a compare argument compares under the database's locale, and this row is being "
                + "encoded outside a database.");

        return new Functions(value, databaseCollation, definition).Function(function, new(name, node.Arguments));
    }

    /// <summary>The functions over a calculated column's expression, its arguments evaluated as the rest of it is.</summary>
    private sealed class Functions(Func<string, object?> value, Collation? databaseCollation,
        Func<string, ColumnDef?> definition) : Types.ExpressionFunctions<CalcNode>
    {
        public override object? Evaluate(CalcNode expression) =>
            CalculatedEvaluator.Evaluate(expression, value, databaseCollation, definition);

        protected override JetTextComparer Text => TextIn(databaseCollation);

        protected override bool IsCurrency(CalcNode expression) => CalculatedEvaluator.IsCurrency(expression, definition);

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
                JetDataType.Byte => Operators.AsByte(value),
                JetDataType.Int16 => (short)Operators.AsInteger(value),
                JetDataType.Int32 => Operators.AsLong(value),
                JetDataType.Int64 => checked((long)Math.Round(Operators.Dbl(Operators.ConversionNumber(value)), MidpointRounding.ToEven)),
                JetDataType.Single => (float)Operators.Dbl(Operators.ConversionNumber(value)),
                JetDataType.Double => Operators.Dbl(Operators.ConversionNumber(value)),
                // A Decimal or text as storing it takes it, exactly (verified vs ACE: [N] over a Decimal or text 1.00005 is
                // 1.0000, where the Double 1.00005 is 1.0001).
                JetDataType.Currency => Operators.ToCurrency(
                    value is decimal || value is string s && Types.VbaText.TryTextAsNumber(s) is not null
                        ? Types.JetTypeCodec.NumberFromText(type, value)
                        : Operators.Dbl(Operators.ConversionNumber(value))),
                JetDataType.DateTime => Operators.ToDate(value),
                // As a String argument takes it: a VBA Boolean as "-1" or "0" (verified vs ACE).
                JetDataType.Text or JetDataType.Memo => Operators.StringArgument(value),
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
