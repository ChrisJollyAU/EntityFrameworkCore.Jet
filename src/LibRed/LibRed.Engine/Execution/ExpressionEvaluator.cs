using EntityFrameworkCore.Jet.Data;
using LibRed.Catalog;
using LibRed.Engine.Planning;
using LibRed.Sql.Ast;
using LibRed.Storage;
using LibRed.Storage.Types;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace LibRed.Engine.Execution;

/// <summary>
/// Evaluates an AST <see cref="Expression"/> against a single row, resolving column
/// references through an <see cref="EvalScope"/> (which chains to outer scopes for
/// correlation). Comparisons coerce numeric operands; SQL nulls propagate (a comparison
/// involving null yields null, treated as "not true" by filters).
/// </summary>
internal sealed partial class ExpressionEvaluator(
    EvalScope scope,
    IScalarSubqueryRunner subqueries,
    ParameterBag? parameters = null,
    SessionState? session = null) : ExpressionFunctions<Expression>
{
    /// <summary>Rebinds this evaluator's scope to a new row of the same schema and returns the evaluator, so a
    /// hot loop can reuse one evaluator across rows instead of allocating a fresh evaluator + scope per row.</summary>
    public ExpressionEvaluator Rebind(object?[] row)
    {
        scope.Rebind(row);
        return this;
    }

    /// <summary>Rebinds to another group of a grouped query: its key row and its aggregates' values.</summary>
    public ExpressionEvaluator Rebind(object?[] row, IReadOnlyDictionary<FunctionCall, object?> aggregates)
    {
        scope.Rebind(row, aggregates);
        return this;
    }

    /// <summary>How text compares here: the database's collation, for every comparison whatever its operands.</summary>
    protected override JetTextComparer Text => subqueries.TextComparer;

    /// <summary>
    /// The value of <paramref name="expression"/>. ACE works a value out in one of two ways (verified vs ACE). A
    /// comparison, a logical operator, and every function's arguments go to its expression service, which does
    /// OLE Automation arithmetic on the values as they are — a number written with a decimal point a Decimal, which
    /// beats a Double (<c>WHERE F * 1.11 = 0.111</c> holds for a Double 0.1). Anything else is a result value — a
    /// column, a sort or group key, an aggregate's argument, a value written — whose arithmetic is the same but for
    /// the written numbers beside which the result is a Double (<see cref="NumberTypeOf"/>): those are Doubles, so
    /// <c>F * 1.11</c> is 0.11100000000000002 there. A VBA Boolean (<see cref="VbaBoolean"/>) is one only while the
    /// expression uses it: what an evaluation hands out is the plain Boolean (verified vs ACE: a subquery's or derived
    /// table's <c>IsNull(x)</c> is written "-1" by the query around it, where the expression's own <c>&amp;</c> writes
    /// "True").
    /// </summary>
    public override object? Evaluate(Expression expression)
    {
        if (_evaluating)
            return _grouped?.Contains(expression) == true ? VbaBoolean.Plain(Value(expression)) : Value(expression);
        if (expression is LiteralExpression or ColumnReference)
            return Value(expression);
        _evaluating = true;
        try
        {
            return VbaBoolean.Plain(Value(expression));
        }
        finally
        {
            _evaluating = false;
        }
    }

    /// <summary>Whether an evaluation is under way (<see cref="Evaluate"/>), so a value is handed to the expression
    /// around it rather than out.</summary>
    private bool _evaluating;

    /// <summary>The expressions a grouped query's group keys are (<see cref="Grouping"/>), by node.</summary>
    private IReadOnlySet<Expression>? _grouped;

    /// <summary>Evaluates <paramref name="grouped"/> — the nodes of a grouped query's projection that are its GROUP BY
    /// expressions — as the group keys they are: values handed out of their own evaluation, so a VBA Boolean among
    /// them is plain (verified vs ACE: <c>IsNull(A) &amp; ''</c> grouped by <c>IsNull(A)</c> is "-1", not "True").</summary>
    public ExpressionEvaluator Grouping(IReadOnlySet<Expression> grouped)
    {
        _grouped = grouped;
        return this;
    }

    private object? Value(Expression expression) => expression switch
    {
        LiteralExpression l => l.Value,
        ColumnReference c => scope.TryResolve(c, out object? v) ? v
            : IsNiladicFunction(c) ? Clock
            : throw new InvalidOperationException($"Column '{c.QualifiedName}' was not found."),
        ParameterExpression p => parameters is not null
            ? parameters.Resolve(p.Name)
            : throw new InvalidOperationException($"No parameters were supplied for '{p.Name}'."),
        // Leaves never open an expression-service boundary. Resolve them directly rather than
        // testing every service-opening node type before each literal or column read.
        _ when !_service && OpensService(expression) => InService(expression),
        BinaryExpression b => EvaluateBinary(b),
        FunctionCall f => EvaluateFunction(f),
        ScalarSubquery s => subqueries.ExecuteScalar(s.Query, scope),
        ExistsExpression e => subqueries.ExecuteExists(e.Query, scope),
        InSubqueryExpression i => EvaluateInSubquery(i),
        InListExpression i => EvaluateInList(i),
        BetweenExpression be => EvaluateBetween(be),
        CaseExpression c => EvaluateCase(c),
        UnaryExpression u => EvaluateUnary(u),
        SystemVariableExpression v => ResolveSystemVariable(v.Name),
        OutputColumnPosition p => scope.At(p.Position),
        SoughtValue s => Sought(s),
        ComparedAsWritten w => !ComparedByTruth(VbaBoolean.Plain(Evaluate(w.Value))?.GetType(), IsCurrency(w.Value)),
        _ => throw new NotSupportedException($"Cannot evaluate {expression.GetType().Name}."),
    };

    // The stored column each sought value is converted for, by node: where a node resolves does not change between rows.
    private Dictionary<SoughtValue, ColumnDef?>? _soughtColumns;

    /// <summary>The value as the index on its column takes it (<see cref="TryGetSeekKey"/>): converted as storing it in
    /// the column converts it, Null where the column cannot hold it, and as it is where the index cannot answer for it.</summary>
    private object? Sought(SoughtValue s)
    {
        object? value = VbaBoolean.Plain(Evaluate(s.Value));
        // IDE0028's only fix here is `[]`, which would drop the comparer and key the nodes structurally.
#pragma warning disable IDE0028
        _soughtColumns ??= new Dictionary<SoughtValue, ColumnDef?>(ReferenceEqualityComparer.Instance);
#pragma warning restore IDE0028
        if (!_soughtColumns.TryGetValue(s, out ColumnDef? column))
            _soughtColumns[s] = column = OutputColumn.Find(scope.AllColumns(), s.Column) is { Source: { } source } ? source.Column : null;
        if (value is null || column is null)
            return value;
        return TryGetSeekKey(column, s.Value, value, s.Bound, out object? key) switch
        {
            SeekKey.Key => key,
            SeekKey.OtherKind => value,
            _ => null,
        };
    }

    /// <summary>Whether this evaluation is inside ACE's expression service (<see cref="Evaluate"/>).</summary>
    private bool _service;

    private object? InService(Expression expression)
    {
        _service = true;
        try
        {
            return Evaluate(expression);
        }
        finally
        {
            _service = false;
        }
    }

    /// <summary>Whether <paramref name="expression"/> hands what it holds to ACE's expression service: a comparison, a
    /// logical operator, a function, or a choice.</summary>
    private static bool OpensService(Expression expression) => expression is FunctionCall or CaseExpression
        or InSubqueryExpression or InListExpression or BetweenExpression
        or UnaryExpression { Operator: not (UnaryOperator.Negate or UnaryOperator.BitNot) }
        or BinaryExpression { Operator: not (BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply
            or BinaryOperator.Divide or BinaryOperator.IntDivide or BinaryOperator.Modulo or BinaryOperator.Power
            or BinaryOperator.Concat or BinaryOperator.BitAnd or BinaryOperator.BitOr or BinaryOperator.BitXor) };

    /// <summary>Access's <c>Now</c> is a niladic function callable without parentheses — so a bare unqualified
    /// identifier that isn't a column but names it evaluates as the current timestamp. Matches ACE, which accepts
    /// e.g. <c>DATETIME DEFAULT Now</c> and <c>SELECT Now</c>. Only tried after column resolution fails, so a real
    /// column named "Now" still wins. Note <c>Date</c>/<c>Time</c> are NOT included: they are reserved type
    /// keywords in Jet SQL and ACE rejects them bare ("Type mismatch") — they require parentheses (<c>Date()</c>,
    /// <c>Time()</c>), which parse as function calls and are handled in <see cref="EvaluateFunction"/>.</summary>
    internal static bool IsNiladicFunction(ColumnReference c) =>
        c.Table is null && c.Column.Equals("Now", StringComparison.OrdinalIgnoreCase);

    /// <summary>Resolves a connection-scoped system variable from the session state: <c>@@ROWCOUNT</c>
    /// (rows affected by the previous statement) and <c>@@IDENTITY</c> (the last AutoNumber generated on
    /// this connection, or NULL if none). EF Core's insert round-trip reads both in the SELECT that
    /// follows the INSERT within the same batch.</summary>
    private object? ResolveSystemVariable(string name)
    {
        if (session is null)
            throw new InvalidOperationException($"System variable '@@{name}' is not available in this context.");

        return name.ToUpperInvariant() switch
        {
            "ROWCOUNT" => session.RowCount,
            "IDENTITY" => session.LastIdentity,
            _ => throw new NotSupportedException($"Unknown system variable '@@{name}'."),
        };
    }

    /// <summary><c>x [NOT] IN (subquery)</c>, and any quantified comparison <c>x op ANY | ALL (subquery)</c>, with SQL
    /// three-valued semantics (verified vs ACE): ANY holds when x compares so with one of the rows, ALL when with every
    /// one. Over no rows ANY is False and ALL True, whatever x, so <c>Null IN</c> an empty subquery is False; otherwise
    /// a Null x, or a Null row that leaves it undecided, makes it Null. NOT IN, and a NOT over it, is its negation —
    /// where ACE turns <c>NOT x &lt; ALL (…)</c> into <c>x &gt;= ALL (…)</c>, which is not, for any comparison but
    /// <c>=</c> (measured; deliberately not followed).</summary>
    private bool? EvaluateInSubquery(InSubqueryExpression inq)
    {
        object? val = VbaBoolean.Plain(Evaluate(inq.Value));
        bool membership = inq is { Comparison: BinaryOperator.Equal, All: false };
        bool? result = null;

        // A correlated IN is a semi-join: hash the body's values once instead of re-running it for every outer row.
        if (val is not null && membership && subqueries.ExecuteInSubquery(inq.Query, inq.Value, val, scope) is var (semiFound, semiNull))
        {
            result = semiFound ? true : semiNull ? null : false;
        }
        else
        {
            // An uncorrelated body is hoisted by ExecuteColumn and runs once; what used to cost outer × inner
            // was the membership test over its values, repeated per outer row. Hoisting also builds a hash set,
            // so ask that first and only walk the values when it declines (mixed or non-hashable kinds).
            IEnumerable<object?> items = subqueries.ExecuteColumn(inq.Query, scope);
            // A number or date looked up in a Yes/No column is compared by its truth (ComparedByTruth; verified vs
            // ACE: 1 and 2 are IN a subquery's Yes/No column holding True) — not under NOT IN, not against a Boolean
            // the subquery computes, not where the subquery reads the outer row, and not a value the statement computes
            // (verified vs ACE: DT.ID * 1 and CLng(DT.ID) are not IN a Yes/No column holding True), which ACE compares
            // as numbers.
            if (membership && !inq.Negated && !IsComputed(inq.Value)
                && val is not null && ComparedByTruth(val.GetType(), inq.Value is ColumnReference && IsCurrency(inq.Value))
                && inq.Query is SelectStatement { IsSelectStar: false, Projection: [{ Value: ColumnReference }] }
                && items.FirstOrDefault(item => item is not null) is bool
                && !SubqueryHoisting.MayReferenceOuter(inq.Query, scope.VisibleAliases().ToHashSet(StringComparer.OrdinalIgnoreCase)))
                val = IsTruthy(val);
            if (val is not null && membership && subqueries.LookupHoistedIn(inq.Query, val) is var (setFound, setNull))
            {
                result = setFound ? true : setNull ? null : false;
            }
            else
            {
                // ANY is decided by the first row it holds for, ALL by the first it fails for.
                bool any = !inq.All, unknown = false;
                foreach (object? item in items)
                {
                    if (val is null || item is null)
                    {
                        unknown = true;
                        if (val is null) break;
                    }
                    // A date beside a number or Boolean is its serial, as = compares them (Comparable), and a Boolean
                    // beside text the number it is — unless both are columns, which is CompareAsKinds' type mismatch.
                    else if (Holds(inq.Comparison,
                            val is string && item is bool || val is bool && item is string
                                ? inq.Query is SelectStatement { IsSelectStar: false, Projection: [{ Value: ColumnReference body }] }
                                    ? CompareAsKinds(inq.Value, val, body, item)
                                    : Compare(Numeric(val), Numeric(item), Text)
                            : val is DateTime != item is DateTime ? Compare(Serial(val), Serial(item), Text)
                            : Compare(val, item, Text)) == any)
                    {
                        result = any;
                        break;
                    }
                }
                result ??= unknown ? null : !any;
            }
        }

        return inq.Negated ? !result : result;
    }

    /// <summary><c>x [NOT] IN (a, b, …)</c> over a list, evaluated iteratively (not as a recursive OR-tree) so a huge
    /// list can't overflow the stack. Null when x is; otherwise whether an item equals x, with each pair brought to a
    /// common kind as <c>=</c> does (verified vs ACE). A miss is Null when an item is Null, as the standard has it:
    /// <c>5 IN (1, NULL)</c> and <c>5 NOT IN (1, NULL)</c> are both Null. This departs from ACE, which skips a Null
    /// item and makes them False and True. A <c>True</c> or <c>False</c> item is compared as -1 or 0, not as a truth
    /// test.</summary>
    private object? EvaluateInList(InListExpression inl)
    {
        object? val = Evaluate(inl.Value);
        if (val is null) return null;

        bool found = false, hasNull = false;
        foreach (Expression itemExpr in inl.Items)
        {
            if (Evaluate(itemExpr) is not { } item) hasNull = true;
            else if (CompareAsKinds(inl.Value, val, itemExpr, item) == 0) { found = true; break; }
        }
        return !found && hasNull ? null : found != inl.Negated;
    }

    /// <summary><c>x [NOT] BETWEEN a AND b</c>: whether x lies between the two bounds inclusive, in whichever order
    /// they are written, with each pair brought to a common kind as a comparison does. Null when any of the three is
    /// (verified vs ACE: <c>15 BETWEEN NULL AND 10</c> is Null, not False).</summary>
    private object? EvaluateBetween(BetweenExpression be)
    {
        object? val = Evaluate(be.Value), low = Evaluate(be.Low), high = Evaluate(be.High);
        if (val is null || low is null || high is null) return null;

        int toLow = CompareAsKinds(be.Value, val, be.Low, low), toHigh = CompareAsKinds(be.Value, val, be.High, high);
        bool inside = (toLow >= 0 && toHigh <= 0) || (toLow <= 0 && toHigh >= 0);
        return inside != be.Negated;
    }

    /// <summary>Standard SQL <c>CASE</c>. Arms are tested in order and the first whose condition is true wins;
    /// an arm is skipped when its condition is false <em>or</em> NULL, since only true selects. Evaluation
    /// short-circuits — later conditions and every unselected result go unevaluated, which matters because a
    /// result can divide by zero or otherwise throw in a branch the condition exists to avoid. With nothing
    /// matched and no ELSE, the answer is NULL rather than an error, per the standard.</summary>
    private object? EvaluateCase(CaseExpression c)
    {
        foreach (CaseWhen arm in c.WhenClauses)
        {
            if (IsTrue(arm.Condition))
                return Evaluate(arm.Result);
        }

        return c.ElseResult is null ? null : Evaluate(c.ElseResult);
    }


    private Dictionary<FunctionCall, string>? _functions;

    private object? EvaluateFunction(FunctionCall f)
    {
        // Aggregate calls are precomputed per group and resolved by reference — including an outer
        // aggregate found in an enclosing scope (a correlated subquery referencing MAX(o.Col), etc.).
        if (scope.TryResolveAggregate(f, out object? aggregate))
            return aggregate;

        // VBA "$" variants (Left$, UCase$, Chr$, …) return a String instead of a Variant but compute the same
        // value in the Jet expression service — so a trailing "$" is stripped and dispatched to the base name.
        // Names, FILTER eligibility and arity belong to the AST, not the current row. Validate on the
        // first reached evaluation so lazy branches still defer their errors, then retain only that shape.
#pragma warning disable IDE0028 // AST calls are keyed by identity, as in the other per-execution caches.
        _functions ??= new Dictionary<FunctionCall, string>(ReferenceEqualityComparer.Instance);
#pragma warning restore IDE0028
        if (!_functions.TryGetValue(f, out string? name))
        {
            name = f.Name.ToUpperInvariant();
            if (name.Length > 1 && name[^1] == '$') name = name[..^1];
            if (f.Filter is not null && !Planning.QueryPlanner.IsAggregate(name))
                throw new InvalidOperationException($"{f.Name} takes no FILTER; only an aggregate does.");
            ValidateArity(name, f.Arguments.Count);
            _functions[f] = name;
        }

        return Function(name, new Call(f.Name, f.Arguments));
    }

    protected override object? EvaluatePower(Expression left, Expression right) =>
        Evaluate(new BinaryExpression(BinaryOperator.Power, left, right));

    protected override uint? RandSeed
    {
        get => session?.RandSeed;
        set => session!.RandSeed = value!.Value;
    }

    /// <summary>Rejects argument counts verified against ACE. Keep this table evidence-driven: add a function
    /// only after its minimum/maximum have been exercised through ACE, since Jet includes quirks such as IIf's
    /// accepted two-argument form (the omitted false branch is Null).</summary>
    internal static void ValidateArity(string name, int count)
    {
        (int Min, int Max)? range = name switch
        {
            // Conversion, unary numeric/string/date/inspection functions and single-argument aliases.
            "CBOOL" or "CBYTE" or "CINT" or "CLNG" or "CLNGLNG" or "CSNG" or "CDBL" or "CCUR" or "CDEC"
                or "CSTR" or "CDATE" or "CVDATE" or "CVAR"
                or "ABS" or "SGN" or "INT" or "FIX" or "SQR" or "EXP" or "SIN" or "COS"
                or "TAN" or "ATN"
                or "FLOOR" or "CEILING" or "CEIL" or "SIGN" or "SQRT" or "LN" or "LOG10" or "ASIN" or "ACOS"
                or "ATAN" or "SINH" or "COSH" or "TANH" or "DEGREES" or "RADIANS"
                or "LEN" or "LCASE" or "UCASE" or "TRIM" or "SPACE"
                or "STRREVERSE" or "STR" or "VAL" or "CHR" or "ASC" or "HEX" or "OCT"
                or "DATEVALUE" or "TIMEVALUE" or "YEAR" or "MONTH" or "DAY" or "HOUR" or "MINUTE"
                or "SECOND" or "ISDATE" or "ISNULL" or "ISNUMERIC" or "ISERROR" or "ISEMPTY" or "ISMISSING"
                or "ISOBJECT" or "TYPENAME" or "VARTYPE"
                or "QBCOLOR" or "ASCW" or "CHRW" or "ASCB" or "LENB" or "DATALENGTH" => (1, 1),

            "LEFT" or "RIGHT" or "STRING" or "LEFTB" or "RIGHTB" => (2, 2),
            "MID" or "MIDB" => (2, 3),
            "INSTR" or "INSTRREV" or "INSTRB" => (2, 4),
            "STRCOMP" => (2, 3),
            "STRCONV" => (2, 3),
            "IIF" => (2, 3),
            "NULLIF" => (2, 2),
            "CHOOSE" => (2, int.MaxValue),
            "SWITCH" => (2, int.MaxValue),
            // COALESCE(expression [, ...n]). SQL Server insists on two, but one is harmless and the standard's
            // own grammar allows it, so only an empty list is rejected.
            "COALESCE" => (1, int.MaxValue),
            "NZ" => (1, 2),
            // GREATEST/LEAST(expression [, ...n]), as SQL Server and PostgreSQL take them: one argument or more.
            "GREATEST" or "LEAST" => (1, int.MaxValue),

            "NOW" or "DATE" or "TIME" or "TIMER" or "IMESTATUS" or "GENUNIQUEID" or "GENGUID"
                or "GETUTCDATE" or "SYSDATETIME" or "SYSUTCDATETIME" => (0, 0),
            "DATEADD" => (3, 3),
            "DATEDIFF" or "DATEDIFF_BIG" => (3, 5),
            "DATEPART" => (2, 4),
            "DATESERIAL" or "TIMESERIAL" => (3, 3),
            "WEEKDAY" or "MONTHNAME" => (1, 2),
            "WEEKDAYNAME" => (1, 3),

            "RGB" => (3, 3),
            "ROUND" => (1, 2),
            "LOG" => (1, 2),
            // Access takes one argument; the second, a set of characters to strip, is SQL Server 2022's.
            "LTRIM" or "RTRIM" => (1, 2),
            "POWER" or "ATAN2" => (2, 2),
            "PI" => (0, 0),
            "RND" => (0, 1),
            "REPLACE" => (3, 6),
            "CONCAT_WS" => (3, int.MaxValue),
            "TRANSLATE" => (3, 3),
            "FORMAT" => (1, 4),
            "FORMATCURRENCY" or "FORMATNUMBER" or "FORMATPERCENT" => (1, 5),
            "FORMATDATETIME" => (1, 2),
            "PARTITION" => (4, 4),

            "PMT" or "FV" or "PV" or "NPER" => (3, 5),
            "IPMT" or "PPMT" => (4, 6),
            "RATE" => (3, 6),
            "SLN" => (3, 3),
            "SYD" => (4, 4),
            "DDB" => (4, 5),

            "FIRST" or "LAST" => (1, 1),
            _ when RunningAggregate.Supports(name) => RunningAggregate.IsPair(name) ? (2, 2) : (1, 1),
            // The fraction and the WITHIN GROUP key, which the parser appends; it has checked the call's shape.
            "PERCENTILE_CONT" or "PERCENTILE_DISC" => (2, 2),
            _ => null,
        };
        bool invalidPairs = name == "SWITCH" && count % 2 != 0;
        if (range is { } valid && (count < valid.Min || count > valid.Max || invalidPairs))
            throw new InvalidOperationException(
                $"Wrong number of arguments used with function {name} (expected " +
                (name == "SWITCH" ? "condition/value pairs" : valid.Min == valid.Max
                    ? valid.Min.ToString(CultureInfo.InvariantCulture)
                    : valid.Max == int.MaxValue ? $"at least {valid.Min}" : $"{valid.Min} to {valid.Max}") + ").");
    }

    /// <summary>Whether a value of <paramref name="type"/> looked up in a Yes/No column is compared by its truth: a
    /// number or a date, not a Boolean, and a Decimal only when it is a Currency column's (<paramref name="currencyColumn"/>;
    /// verified vs ACE: a Currency column is, a Decimal column and a Decimal or Currency the statement computes are
    /// compared as numbers). The IN subquery here and the hashed EXISTS key (SubqueryCorrelation.SameKind) ask this.</summary>
    internal static bool ComparedByTruth(Type? type, bool currencyColumn)
    {
        Type? t = type is null ? null : Nullable.GetUnderlyingType(type) ?? type;
        return t == typeof(DateTime) || t == typeof(byte) || t == typeof(short) || t == typeof(int) || t == typeof(long)
            || t == typeof(float) || t == typeof(double) || (t == typeof(decimal) && currencyColumn);
    }

    /// <summary>Whether an expression is a Currency over this scope's columns (<see cref="ClassOf"/>).</summary>
    protected override bool IsCurrency(Expression expression) =>ClassOf(expression) == NumberClass.Currency;

    /// <summary>Whether an expression is a Currency: a Currency column, CCur, or arithmetic that keeps one —
    /// <paramref name="expression"/> over <paramref name="columns"/> (<see cref="NumberTypeOf"/>), with
    /// <paramref name="declaredType"/> where its caller knows declared types.</summary>
    internal static bool IsCurrency(
        Expression expression, IReadOnlyList<OutputColumn> columns, Func<Expression, Type?>? declaredType = null) =>
        NumberTypeOf(expression, columns, declaredType ?? (_ => null)).Class == NumberClass.Currency;

    /// <summary>The expression's <see cref="NumberClass"/>.</summary>
    /// <remarks>Decided by the expression and the scope's columns, never the row, so it is worked out once per node
    /// for this evaluator — which is reused across rows. Working it out walks the schema, and did so for every row
    /// of every Currency-typed arithmetic result.</remarks>
    private NumberClass ClassOf(Expression expression)
    {
        // IDE0028's only fix here is `[]`, which would drop the comparer and key the nodes structurally.
#pragma warning disable IDE0028
        _classes ??= new Dictionary<Expression, NumberClass>(ReferenceEqualityComparer.Instance);
#pragma warning restore IDE0028
        if (!_classes.TryGetValue(expression, out NumberClass numberClass))
            _classes[expression] = numberClass = NumberTypeOf(expression, scope.AllColumns(), _ => null).Class;
        return numberClass;
    }

    private Dictionary<Expression, NumberClass>? _classes;

    // ParameterBag snapshots the parameter values for this execution. Which operand is a duration is
    // consequently structural, like the numeric class above, and does not change as rows are rebound.
    private Dictionary<BinaryExpression, bool?>? _spanSides;

    // Access truthiness: a filter/logical context treats any non-zero number as true (so a boolean stored
    // as a -1/0 integer — the nullable-bool convention — works as a bare predicate), a null as not-true.
    public bool IsTrue(Expression expression) => AsBool(Evaluate(expression)) is true;

    // DELIBERATE DUPLICATE (the owner's decision, 2026-11-23; CLAUDE.md "One copy of every function"): EvaluateUnary,
    // EvaluateBinary, EvaluateInList and CompareAsKinds dispatch the operators over the SQL AST as CalculatedEvaluator's
    // Unary, Binary, In and CompareAsKinds do over a CalcNode, calling the same ExpressionFunctions helpers; where they
    // differ, and why, is listed at CalculatedEvaluator.Unary. A change to one is made to the other.
    private object? EvaluateUnary(UnaryExpression u)
    {
        // NOT EXISTS is not the lookup EXISTS is: ACE compares its correlation as written (ExecuteExists).
        if (u is { Operator: UnaryOperator.Not, Operand: ExistsExpression notExists })
            return !subqueries.ExecuteExists(notExists.Query, scope, negated: true);

        object? v = VbaBoolean.Plain(Evaluate(u.Operand));
        return u.Operator switch
        {
            UnaryOperator.Not => Not(v),
            UnaryOperator.Negate => v is null ? null : Negate(v),
            UnaryOperator.BitNot => v is null ? null : BitNot(v),
            UnaryOperator.IsNull => v is null,
            UnaryOperator.IsNotNull => v is not null,
            UnaryOperator.IsTrue => AsBool(v) is true,
            UnaryOperator.IsNotTrue => AsBool(v) is not true,
            UnaryOperator.IsFalse => AsBool(v) is false,
            UnaryOperator.IsNotFalse => AsBool(v) is not false,
            _ => throw new NotSupportedException($"Unary operator {u.Operator}."),
        };
    }

    // A comparison's result as an object without boxing a new bool for every row it is asked of: a filter or a
    // join's residual ON compares once per row, and each answer was an allocation.
    private static readonly object BoxedTrue = true, BoxedFalse = false;

    private static object Boxed(bool value) => value ? BoxedTrue : BoxedFalse;

    private object? EvaluateBinary(BinaryExpression b)
    {
        if (b.Operator is BinaryOperator.And)
            return And(b.Left, b.Right);
        if (b.Operator is BinaryOperator.Or)
            return Or(b.Left, b.Right);
        // XOR and EQV are Null when either side is; IMP is True whenever its left side is False or its right side
        // is True, and Null otherwise when a side is Null (the VBA truth tables; verified vs ACE).
        if (b.Operator is BinaryOperator.Xor or BinaryOperator.Eqv or BinaryOperator.Imp)
        {
            bool? l = AsBool(Evaluate(b.Left)), r = AsBool(Evaluate(b.Right));
            return b.Operator switch
            {
                BinaryOperator.Xor => l ^ r,
                BinaryOperator.Eqv => l is null || r is null ? null : l == r,
                _ => l == false || r == true ? true : l is null || r is null ? null : false,
            };
        }

        // A date plus or less a span (IsSpan) is the date moved by it. Which side is the span is settled before
        // either is evaluated, so neither side is worked out twice; beside anything but a date, the span side is
        // evaluated as it always is — as the time on the epoch.
        object? left, right;
        if (b.Operator is BinaryOperator.Add or BinaryOperator.Subtract && SpanSide(b) is { } spanOnRight)
        {
            object? other = Evaluate(spanOnRight ? b.Left : b.Right);
            if (other is DateTime date)
                return SpanOf(spanOnRight ? b.Right : b.Left) is { } span
                    ? Shift(date, spanOnRight && b.Operator == BinaryOperator.Subtract ? -span : span)
                    : null;
            (left, right) = spanOnRight ? (other, Evaluate(b.Right)) : (Evaluate(b.Left), other);
        }
        else
        {
            left = Evaluate(b.Left);
            right = Evaluate(b.Right);
        }

        if (b.Operator == BinaryOperator.Concat)
            return Concat(left, right);


        // IS [NOT] DISTINCT FROM is '=' with Null taken as a value, so it is never Null: two Nulls are not
        // distinct, a Null and a value are.
        if (b.Operator is BinaryOperator.IsDistinctFrom or BinaryOperator.IsNotDistinctFrom)
        {
            bool distinct = left is null || right is null
                ? (left is null) != (right is null)
                : CompareAsKinds(b.Left, left, b.Right, right) != 0;
            return distinct == (b.Operator == BinaryOperator.IsDistinctFrom);
        }

        if (b.Operator is BinaryOperator.Add or BinaryOperator.Subtract)
            (left, right) = CurrencySumOperands(left, right, IsCurrency(b));

        // The arithmetic operators other than '+' read text as a number even when the other side is Null, so text
        // that is not a number, a GUID or a binary value is a type mismatch before Null propagates (verified vs
        // ACE: 'abc' * NULL fails, '1' * NULL is Null).
        if (b.Operator is BinaryOperator.Subtract or BinaryOperator.Multiply or BinaryOperator.Divide
            or BinaryOperator.IntDivide or BinaryOperator.Modulo or BinaryOperator.Power)
        {
            left = NumericOperand(left);
            right = NumericOperand(right);
        }

        // A pattern written as just '%' is IS NOT NULL, a comparison's -1 or 0 rather than a Like's VBA Boolean, and False
        // for Null rather than Null (verified vs ACE; '%%', or a '%' the pattern is computed to, stays Null).
        if (b.Operator == BinaryOperator.Like && b.Right is LiteralExpression { Value: "%" })
            return Boxed(left is not null);

        // `x <> Null`, with the Null written on the right, is `x Is Not Null` (verified vs ACE, for a Yes/No, number,
        // text and date column alike; `Null <> x` and a Null parameter stay Null).
        if (b.Operator == BinaryOperator.NotEqual && b.Right is LiteralExpression { Value: null })
            return Boxed(left is not null);

        if (left is null || right is null)
            return null;

        if (b.Operator is BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply or BinaryOperator.Divide)
        {
            // In a result value, a written number beside which the result is a Double is a Double (Evaluate; verified
            // vs ACE: F * 1.11, 1.11 + 0.3 and Currency * 1.11 work in Doubles, where L * 1.11 stays a Decimal and
            // DECIMAL(18,4) * 1.11 takes the Decimal column's arithmetic).
            if (!_service && (b.Left is LiteralExpression { Value: decimal } || b.Right is LiteralExpression { Value: decimal })
                && ClassOf(b) == NumberClass.Double)
            {
                if (b.Left is LiteralExpression && left is decimal written) left = JetDecimalConverter.FromDecimal(written);
                if (b.Right is LiteralExpression && right is decimal writtenRight) right = JetDecimalConverter.FromDecimal(writtenRight);
            }
            (left, right) = CurrencyProductOperands(
                left, right, b.Operator == BinaryOperator.Divide, IsCurrency(b.Left), IsCurrency(b.Right));
        }

        if (b.Operator is BinaryOperator.Equal or BinaryOperator.NotEqual && TruthTest(b, left, right) is bool truth)
            return Boxed(b.Operator == BinaryOperator.Equal ? truth : !truth);

        // A result that is a Currency (NumberTypeOf) is one at every step (CurrencyStep).
        if (b.Operator is BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply)
        {
            object result = b.Operator switch
            {
                BinaryOperator.Add => Add(left, right),
                BinaryOperator.Subtract => Arithmetic(left, right, '-'),
                _ => Arithmetic(left, right, '*'),
            };
            return CurrencyStep(result, IsCurrency(b));
        }

        return b.Operator switch
        {
            BinaryOperator.Equal or BinaryOperator.NotEqual or BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual
                or BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual =>
                Boxed(Holds(b.Operator, CompareOperands(b, left, right))),
            // LIKE reads any other value as the text CStr gives it (verified vs ACE: TRUE LIKE '-1' is True). A binary
            // value becomes text too, so LIKE is case-insensitive over a binary column even though '=' on the same
            // column is byte-wise: `B LIKE 'A%'` matches both 0x4100 ('A') and 0x6100 ('a').
            BinaryOperator.Like => VbaBoolean.Of(LikeMatcher.IsMatch(ConcatText(left), ConcatText(right))),
            BinaryOperator.Divide => Divide(left, right), // Access '/' is floating division
            BinaryOperator.Modulo => IntegerOp(left, right, '%'),
            BinaryOperator.IntDivide => IntegerOp(left, right, '\\'),
            BinaryOperator.Power => Power(left, right),
            BinaryOperator.BitAnd => BitwiseOp(left, right, (x, y) => x & y),
            BinaryOperator.BitOr => BitwiseOp(left, right, (x, y) => x | y),
            BinaryOperator.BitXor => BitwiseOp(left, right, (x, y) => x ^ y),
            _ => throw new NotSupportedException($"Binary operator {b.Operator}."),
        };
    }

    /// <summary>Whether a comparison operator holds for two values in the given order (negative, zero or positive).</summary>
    private static bool Holds(BinaryOperator comparison, int order) => comparison switch
    {
        BinaryOperator.Equal => order == 0,
        BinaryOperator.NotEqual => order != 0,
        BinaryOperator.LessThan => order < 0,
        BinaryOperator.LessThanOrEqual => order <= 0,
        BinaryOperator.GreaterThan => order > 0,
        BinaryOperator.GreaterThanOrEqual => order >= 0,
        _ => throw new NotSupportedException($"Comparison operator {comparison}."),
    };

    /// <summary>
    /// Whether <paramref name="expression"/> is a span — known before any row is read, as a Currency result is.
    /// A span starts from a TimeSpan or TimeOnly parameter (<paramref name="duration"/>); only a parameter can say it
    /// is one, since a time column or a time written into the SQL is a date on the epoch, as Jet stores it. A span
    /// stays one under negation, added to or less another span, times or divided by something that is not a span,
    /// and chosen by IIF, COALESCE or CASE from spans and Nulls. A date plus or less a span is the date moved by it
    /// (<see cref="SpanOf"/>) — a date, where a date less a date is a day count.
    /// </summary>
    internal static bool IsSpan(Expression expression, Func<string, TimeSpan?> duration)
    {
        bool Span(Expression e) => IsSpan(e, duration);
        bool SpanOrNull(Expression e) => e is LiteralExpression { Value: null } || Span(e);
        bool Choice(IEnumerable<Expression> results) => results.All(SpanOrNull) && results.Any(Span);

        return expression switch
        {
            ParameterExpression parameter => duration(parameter.Name) is not null,
            UnaryExpression { Operator: UnaryOperator.Negate } negation => Span(negation.Operand),
            BinaryExpression { Operator: BinaryOperator.Add or BinaryOperator.Subtract } sum => Span(sum.Left) && Span(sum.Right),
            BinaryExpression { Operator: BinaryOperator.Multiply } product => Span(product.Left) != Span(product.Right),
            BinaryExpression { Operator: BinaryOperator.Divide } quotient => Span(quotient.Left) && !Span(quotient.Right),
            FunctionCall call when call.Name.Equals("IIF", StringComparison.OrdinalIgnoreCase) && call.Arguments.Count >= 2 =>
                Choice(call.Arguments.Skip(1)),
            FunctionCall call when call.Name.Equals("COALESCE", StringComparison.OrdinalIgnoreCase) => Choice(call.Arguments),
            CaseExpression @case => Choice(@case.WhenClauses.Select(w => w.Result)
                .Concat(@case.ElseResult is { } otherwise ? [otherwise] : [])),
            _ => false,
        };
    }

    /// <summary>Which side of a <c>+</c> or <c>-</c> is a span that a date beside it would move by: true for the right,
    /// false for the left (only for <c>+</c>, since a span less a date is no date), null when neither.</summary>
    private bool? SpanSide(BinaryExpression b)
    {
        if (parameters is not { HasDurations: true })
            return null;
#pragma warning disable IDE0028 // Preserve reference identity for AST nodes, as in the numeric-class cache.
        _spanSides ??= new Dictionary<BinaryExpression, bool?>(ReferenceEqualityComparer.Instance);
#pragma warning restore IDE0028
        if (_spanSides.TryGetValue(b, out bool? held)) return held;
        bool? side = IsSpan(b.Right, parameters.Duration) ? true
            : b.Operator == BinaryOperator.Add && IsSpan(b.Left, parameters.Duration) ? false : null;
        _spanSides[b] = side;
        return side;
    }

    /// <summary>The value of a span (<see cref="IsSpan"/>), to the tick; null when it is Null. A factor or divisor is read
    /// as a number as the arithmetic operators read one.</summary>
    private TimeSpan? SpanOf(Expression expression)
    {
        switch (expression)
        {
            case ParameterExpression parameter:
                return parameters!.Duration(parameter.Name);
            case LiteralExpression { Value: null }:
                return null;
            case UnaryExpression negation:
                return -SpanOf(negation.Operand);
            case BinaryExpression { Operator: BinaryOperator.Add or BinaryOperator.Subtract } sum:
                return SpanOf(sum.Left) is { } a && SpanOf(sum.Right) is { } b
                    ? checked(sum.Operator == BinaryOperator.Add ? a + b : a - b)
                    : null;
            case BinaryExpression product:
                {
                    bool spanOnLeft = product.Operator == BinaryOperator.Divide || IsSpan(product.Left, parameters!.Duration);
                    if (SpanOf(spanOnLeft ? product.Left : product.Right) is not { } span
                        || Evaluate(spanOnLeft ? product.Right : product.Left) is not { } by)
                        return null;
                    double factor = Dbl(ConversionNumber(by));
                    if (product.Operator == BinaryOperator.Divide)
                        factor = factor != 0 ? 1 / factor : throw new DivideByZeroException("Division by zero.");
                    return TimeSpan.FromTicks(checked((long)Math.Round(span.Ticks * factor)));
                }
            case FunctionCall call when call.Name.Equals("IIF", StringComparison.OrdinalIgnoreCase):
                return IifArm(call.Arguments) is { } arm ? SpanOf(arm) : null;
            case FunctionCall coalesce:
                foreach (Expression argument in coalesce.Arguments)
                    if (SpanOf(argument) is { } first)
                        return first;
                return null;
            case CaseExpression @case:
                foreach (CaseWhen when in @case.WhenClauses)
                    if (IsTrue(when.Condition))
                        return SpanOf(when.Result);
                return @case.ElseResult is { } otherwise ? SpanOf(otherwise) : null;
            default:
                throw new InvalidOperationException($"{expression.GetType().Name} is not a span.");
        }
    }

    /// <summary>
    /// <c>=</c> or <c>&lt;&gt;</c> against the literal <c>True</c> or <c>False</c> tests the other side's truth
    /// rather than comparing it (verified vs ACE): a value that reads as 0 is False and anything else is True, so
    /// <c>2 = True</c> and <c>0.0 = False</c> are True. Text is False only as the text "0", compared as text is, as a
    /// GUID or binary value is (verified vs ACE 2010 and Microsoft 365: <c>'0' = False</c> and <c>'0 ' = False</c> are
    /// True, <c>'0.0'</c>, <c>' 0'</c>, <c>'00'</c>, <c>'-0'</c> and <c>''</c> are not, and each is <c>= True</c>).
    /// Null for any other comparison.
    /// </summary>
    private bool? TruthTest(BinaryExpression b, object left, object right) =>
        b.Right is LiteralExpression { Value: bool rightLiteral } ? Truth(left) == rightLiteral
        : b.Left is LiteralExpression { Value: bool leftLiteral } ? Truth(right) == leftLiteral
        : null;

    private bool? Truth(object value) => value is string ? CompareValues(value, "0", Text) != 0 : AsBool(value);

    /// <summary>The order of a comparison operator's two operands. Two texts compare by collation key, as
    /// <see cref="CompareAsKinds"/> would compare them, with two shortcuts that change no answer: texts identical
    /// once trailing spaces go are equal in any order, and the key of a literal or parameter side is made once for
    /// this evaluator — which is reused across rows — rather than for every row it is compared with.</summary>
    private int CompareOperands(BinaryExpression b, object left, object right)
    {
        if (left is string l && right is string r)
        {
            if (l.AsSpan().TrimEnd(' ').SequenceEqual(r.AsSpan().TrimEnd(' ')))
                return 0;
            if (b.Right is LiteralExpression or ParameterExpression)
                return Text.Compare(l, ConstantKey(b.Right, r));
            if (b.Left is LiteralExpression or ParameterExpression)
                return -Text.Compare(r, ConstantKey(b.Left, l));
        }

        return CompareAsKinds(b.Left, left, b.Right, right);
    }

    // Collation keys of the literal and parameter operands met so far, by node: a statement's constants do not
    // change between the rows its evaluator is rebound to.
    private Dictionary<Expression, byte[]>? _constantKeys;

    private byte[] ConstantKey(Expression constant, string text)
    {
        // IDE0028's only fix here is `[]`, which would drop the comparer and key the nodes structurally.
#pragma warning disable IDE0028
        _constantKeys ??= new Dictionary<Expression, byte[]>(ReferenceEqualityComparer.Instance);
#pragma warning restore IDE0028
        if (!_constantKeys.TryGetValue(constant, out byte[]? key))
            _constantKeys[constant] = key = Text.Key(text);
        return key;
    }

    /// <summary>The order of two values once <see cref="Comparable"/> has brought them to a common kind. A parameter
    /// compared with text takes the text's type (verified vs ACE: a numeric parameter against a text column compares
    /// as text, so <c>[S] &gt; ?</c> with 100 counts 'abc' and '11'). So does a number or Boolean the statement
    /// computes beside any text — a text column, a literal, a parameter or a function's result alike (verified vs ACE:
    /// <c>[S] = CInt(True)</c> and <c>[S] = (1=1)</c> find '-1', <c>[S] &gt; CInt(0)</c> every text sorting after '0',
    /// and over a number column holding 5 and 10, <c>[L] + 0 = '5.0'</c> finds nothing and <c>[L] + 0 &lt; '9'</c>
    /// both, a parameter, <c>Trim(?)</c> and <c>? &amp; ''</c> as the literal), where a written one beside a text column
    /// is ACE's type mismatch. Text
    /// that is not written, beside a Large Number (Int64) or date column itself, is 0 to the one and a date to the
    /// other (verified vs ACE). Text a parameter or function gives, or a Double or Single parameter, beside a
    /// Currency column is that Currency first, to four places half to even as <c>CCur</c> rounds (verified vs ACE for
    /// a parameter, with an index or without; a Decimal column takes it as it is). A Currency value, a number or Boolean
    /// the statement is passed, or one it computes other than a Large Number (Int64), beside a binary value
    /// compares as text, the number's text — a Boolean's "-1" or "0" — against the binary read as UTF-16 (verified vs
    /// ACE, where no index answers: <c>[B] = (1=1)</c> and <c>[B] = CInt(True)</c> find 0x2D003100, <c>[B] = CLng(1)</c>,
    /// <c>= CDbl(1)</c> and <c>= CLng(1) + 0</c> 0x31000000, <c>[B] = (1=0)</c> what reads as "0", and a Byte, Integer,
    /// Long, Single, Double or Decimal parameter 1 finds 0x31000000 and 0x3100, and so does a Large Number one, sent as
    /// OLE DB's Numeric since ACE's OLE DB provider carries no BigInt parameter). <c>=</c> and the other comparison operators, an IN list's items,
    /// BETWEEN's bounds and IS [NOT] DISTINCT FROM compare through here.</summary>
    private int CompareAsKinds(Expression leftOperand, object left, Expression rightOperand, object right)
    {
        // Text as written — a text column or written text — beside a number, date or Boolean that is not computed
        // either — a column, a written number or True, an aggregate, an IIf choosing one — is ACE's
        // type mismatch, whichever operator compares them (verified vs ACE 2010 and Microsoft 365: [q] = [a], 7 = '7',
        // [a] < 10, [dt] = '2020-01-02', a Yes/No column beside a text column, MAX(q) = '7', [q] IN ('7'),
        // [q] BETWEEN '1' AND '9', with an index or without). Text the statement computes beside it reads as a number.
        var (text, textOperand, plain, plainOperand) = left is string
            ? (left, leftOperand, right, rightOperand) : (right, rightOperand, left, leftOperand);
        if (text is string && (IsNumeric(plain) || plain is DateTime)
            && Written(textOperand) && Written(plainOperand))
            throw new InvalidCastException("Data type mismatch in criteria expression.");

        (left, right) = ComparedBooleans(left, right);
        bool currencyOnLeft = CurrencyBeside(left, leftOperand, right, rightOperand);
        if (currencyOnLeft || CurrencyBeside(right, rightOperand, left, leftOperand))
        {
            object given = currencyOnLeft ? right : left;
            try
            {
                given = AsCurrencyColumn(given);
            }
            catch (OverflowException)
            {
                // Past a Currency, the value is compared as it is.
            }
            (left, right) = currencyOnLeft ? (left, given) : (given, right);
        }
        else if ((left is byte[] ? (Number: right, NumberOperand: rightOperand) : (Number: left, NumberOperand: leftOperand))
            is var beside && (left is byte[] || right is byte[])
            && (beside.Number is decimal && IsCurrency(beside.NumberOperand)
                || IsNumeric(beside.Number)
                    && (beside.Number is not long && IsComputed(beside.NumberOperand, Arm) || beside.NumberOperand is ParameterExpression)))
            (left, right) = (ConcatText(left), ConcatText(right));

        bool textOnRight = right is string;
        var (textSide, other, otherOperand) = textOnRight ? (right, left, leftOperand) : (left, right, rightOperand);
        if (textSide is string && other is not string
            && (otherOperand is ParameterExpression || (IsNumeric(other) && IsComputed(otherOperand, Arm))))
        {
            object converted = ConcatText(other);
            (left, right) = textOnRight ? (converted, right) : (left, converted);
        }
        // Text a parameter or function gives, beside a Large Number (Int64) or date column read as it is rather than
        // in an expression, is what ACE makes of it outside an index (verified vs ACE): any text is 0 to the Large
        // Number, and the date column reads it as storing it there reads it, text that is not a date being a type
        // mismatch.
        bool columnOnLeft = TextBesideColumn(left, leftOperand, right, rightOperand);
        if (columnOnLeft || TextBesideColumn(right, rightOperand, left, leftOperand))
        {
            object column = columnOnLeft ? left : right, given = columnOnLeft ? right : left;
            object read = column is long ? 0L : AsDateColumn(given);
            (left, right) = columnOnLeft ? (left, read) : (read, right);
        }
        // A GUID sorts below any number or Boolean the statement computes or is passed (verified vs ACE: [G] = CInt(True)
        // and [G] >= (1=1) select nothing, [G] <> ? and [G] < ? every row with a value; one a binary value is beside
        // compares as text, above); beside a written number it stays Comparable's type mismatch, and so does a Large
        // Number (Int64) the statement computes beside a binary value (verified vs ACE: [B] = [L] + 0 is "Type mismatch
        // in expression").
        // Written once with the GUID first; the order is the other way round when it is on the right.
        bool keyOnLeft = left is Guid;
        var (key, number, numberOperand) = keyOnLeft ? (left, right, rightOperand) : (right, left, leftOperand);
        if (key is Guid && IsNumeric(number) && (numberOperand is ParameterExpression || IsComputed(numberOperand, Arm)))
            return keyOnLeft ? -1 : 1;
        return CompareValues(left, right, Text);

        Expression? Arm(FunctionCall iif) => IifArm(iif.Arguments);
        bool Written(Expression operand) => operand is not ParameterExpression && !IsComputed(operand, Arm);

        // Each rule is written with the column first; the caller asks it both ways round.
        bool CurrencyBeside(object column, Expression columnOperand, object given, Expression givenOperand) =>
            column is decimal && given is string or char or double or float && columnOperand is ColumnReference
            && (given is string or char ? NotWritten(givenOperand) : givenOperand is ParameterExpression) && IsCurrency(columnOperand);

        static bool TextBesideColumn(object column, Expression columnOperand, object given, Expression givenOperand) =>
            column is long or DateTime && columnOperand is ColumnReference && given is string or char && NotWritten(givenOperand);
    }

    /// <summary>Whether an operand is a value the statement computes, rather than a number as it writes one —
    /// <c>1</c>, <c>-1.5</c>, <c>True</c> — or another column. <c>False</c> counts as computed: beside a text
    /// column ACE takes it as a computed 0 (verified: <c>[S] &lt; False</c> answers where <c>[S] &lt; True</c> and
    /// <c>[S] &lt; 0</c> are a type mismatch, as a number column beside it is). An aggregate is taken as a column is,
    /// whatever it holds (verified vs ACE: a parameter '5.0' finds <c>MAX(V)</c>, <c>MAX(V + 0)</c> and <c>SUM(V)</c>
    /// holding 5, where it finds <c>V + 0</c>, <c>Abs(V)</c>, <c>CLng(V)</c>, <c>Switch(V &gt; 0, V)</c>,
    /// <c>Choose(1, V)</c> and <c>SUM(V) + 0</c> nowhere; in a WHERE a subquery's value is computed, beside a text column
    /// or a number one). An IIf is what the arm it takes is, as <paramref name="iifArm"/> names it (verified vs ACE: beside text
    /// <c>IIf(q &gt; 0, q + 0, 0)</c> compares as text, <c>IIf(q &gt; 0, q, 0)</c> and <c>IIf(q &gt; 0, 7, 0)</c> as a
    /// column and a written number do); without a row to choose by, it is taken as a column.</summary>
    internal static bool IsComputed(Expression operand, Func<FunctionCall, Expression?>? iifArm = null) => operand switch
    {
        LiteralExpression { Value: not false } or ColumnReference or ParameterExpression => false,
        FunctionCall call when call.Name.Equals("IIF", StringComparison.OrdinalIgnoreCase) =>
            iifArm?.Invoke(call) is { } arm && IsComputed(arm, iifArm),
        FunctionCall call => !Planning.QueryPlanner.IsAggregate(call.Name),
        _ => true,
    };

    /// <summary>Whether an operand is a value the statement does not write: a parameter, a function's result, or an
    /// expression holding either.</summary>
    private static bool NotWritten(Expression e) =>
        e is ParameterExpression or FunctionCall || (e.Operands()?.Any(NotWritten) ?? false);

    /// <summary>What a value compared with an indexed column is to that column's index — every seek, whether by
    /// <c>=</c>, by a join, or by a range bound, asks here before it builds a key.</summary>
    internal enum SeekKey
    {
        /// <summary>A key: the seek selects exactly the rows the comparison does.</summary>
        Key,
        /// <summary>A value of another kind: no key range holds the rows, so the caller scans.</summary>
        OtherKind,
        /// <summary>A value the column cannot hold: the index answers that no row matches.</summary>
        None,
    }

    /// <summary>The key an index seek must use for <paramref name="value"/>, and whether it is one. With
    /// <paramref name="bound"/> the value is a range bound, and one the column cannot hold is the error ACE raises
    /// for it rather than <see cref="SeekKey.None"/>.</summary>
    /// <remarks>
    /// An index answers only in its column's own kind — its keys are encoded and ordered as that type — so a
    /// comparison that happens in a different kind has no key range to seek. <c>S = 1</c> on text compares as
    /// a number (see <see cref="Comparable"/>), matching <c>' 1 '</c>, <c>'1.0'</c> and <c>'+1'</c> as well as
    /// <c>'1'</c>, which are scattered through the index rather than adjacent in it. The one cross-kind case
    /// that IS seekable is a parameter against text, which <see cref="CompareAsKinds"/> converts to text before
    /// comparing; the seek converts it the same way and so asks the index the same question.
    /// <para>A number becomes a whole-number column's key only when the column's type holds it without losing any
    /// of it (verified vs ACE, through its index: no row holds such a value, so <c>=</c> selects nothing, while a
    /// range bound is an error — "Overflow" past the range, "Data type mismatch" for a fraction). A Boolean is the
    /// number -1 or 0 here as everywhere, so True is past a BYTE's range.</para>
    /// <para>Text a parameter or function gives, compared with a number or date column, is converted as storing it
    /// in the column converts it — read as a number and rounded to a whole-number column, or read as a date — and
    /// text that cannot be stored there finds no row, or is "Data type mismatch" as a bound (verified vs ACE,
    /// through its index). Text written in the statement is left as it is. Into a Currency column that text, and a
    /// Double or Single parameter, is a Currency, to four places half to even (verified vs ACE).</para>
    /// <para>A number or Boolean the statement computes or is passed, compared with a GUID or binary column, is
    /// likewise the value storing it there gives: a Long (Int32) a GUID's first four bytes, a number or Boolean its
    /// own bytes, a Currency its eight-byte scaled integer — and one the column refuses finds no row, or is "Data type
    /// mismatch" as a bound, except bytes too few for a GUID, which are the refusal storing them gives either way
    /// (verified vs ACE, through its index: <c>[G] = 0.5 + 0.5</c> is refused).</para>
    /// </remarks>
    internal static SeekKey TryGetSeekKey(ColumnDef column, Expression operand, object? value, bool bound, out object? key)
    {
        key = value;
        if (value is null) return SeekKey.Key;

        IndexSelection.TypeKind? columnKind = IndexSelection.Classify(column.Type);
        if (columnKind == IndexSelection.TypeKind.Text && value is not string
            && (operand is ParameterExpression || value is char || (IsNumeric(value) && IsComputed(operand))))
            key = ConcatText(value);
        else if (value is string or char
                ? columnKind is IndexSelection.TypeKind.Numeric or IndexSelection.TypeKind.Temporal
                    && column.Type != JetDataType.Boolean && NotWritten(operand)
                : (columnKind is IndexSelection.TypeKind.Binary or IndexSelection.TypeKind.Guid
                    && IsNumeric(value) && (operand is ParameterExpression || IsComputed(operand)))
                    || (column.Type == JetDataType.Currency && value is double or float && operand is ParameterExpression))
        {
            try
            {
                key = columnKind switch
                {
                    IndexSelection.TypeKind.Temporal => AsDateColumn(value),
                    IndexSelection.TypeKind.Guid => JetTypeCodec.ToStoredValue(column, column.Type,
                        JetTypeCodec.CurrencyAsStored(column.Type, value, IsCurrency(operand, []))),
                    // Without the zero padding a fixed-length column adds when it stores the value (verified vs ACE).
                    IndexSelection.TypeKind.Binary => JetTypeCodec.AsBinary(JetTypeCodec.NumberFromText(
                            column.Type, JetTypeCodec.CurrencyAsStored(column.Type, value, IsCurrency(operand, []))), currency: false)
                        ?? throw JetTypeCodec.CannotStore(column, column.Type, value),
                    _ => column.Type switch
                    {
                        JetDataType.Byte or JetDataType.Int16 or JetDataType.Int32 or JetDataType.Int64 =>
                            JetTypeCodec.ToStoredValue(column, column.Type, value),
                        JetDataType.Currency => AsCurrencyColumn(value),
                        _ => JetTypeCodec.NumberFromText(column.Type, value),
                    },
                };
            }
            // Bytes too few for a GUID are let through as the refusal (see the remarks).
            catch (Exception e) when ((e is InvalidCastException or FormatException or NotSupportedException
                or OverflowException or InvalidOperationException) && !(e is InvalidOperationException && columnKind == IndexSelection.TypeKind.Guid))
            {
                if (bound)
                    throw new InvalidCastException($"Data type mismatch: '{value}' cannot be a bound of column '{column.Name}'.", e);
                return SeekKey.None;
            }
        }
        if (IndexSelection.KindOf(key) != columnKind)
            return SeekKey.OtherKind;

        (decimal Min, decimal Max)? range = column.Type switch
        {
            JetDataType.Byte => (byte.MinValue, byte.MaxValue),
            JetDataType.Int16 => (short.MinValue, short.MaxValue),
            JetDataType.Int32 => (int.MinValue, int.MaxValue),
            JetDataType.Int64 => (long.MinValue, long.MaxValue),
            _ => null,
        };
        if (range is not { } r)
            return SeekKey.Key;
        decimal exact = 0;
        bool past;
        try
        {
            exact = Dec(key!);
            past = exact < r.Min || exact > r.Max;
        }
        catch (OverflowException)
        {
            past = true;
        }
        if (!past && exact == decimal.Truncate(exact))
            return SeekKey.Key;
        if (!bound)
            return SeekKey.None;
        if (past)
            throw new OverflowException($"Overflow: {key} is past the range of column '{column.Name}'.");
        throw new InvalidCastException($"Data type mismatch: {key} cannot be a bound of the whole-number column '{column.Name}'.");
    }

    /// <summary>
    /// The type ACE gives a result column before reading any row, as far as its places and precision go (verified
    /// vs ACE, by the column a make-table query creates and the precision and scale OLE DB reports). ACE works the
    /// expression out in full and cuts the value only as it goes into a Decimal result column — so
    /// <c>Pmt(0.05 / 12, …)</c> uses the whole rate while <c>SELECT 1 / 1.5</c> is 0.6.
    /// <list type="bullet">
    /// <item>A number written with a decimal point is a Decimal of the places written less trailing zeros
    /// (<c>334.90</c> has one; <c>3.0</c> is a whole number) and a precision of 28, and so is a whole number too big
    /// for a Long, with none; a Decimal column has its precision and scale; Currency counts as four places.</item>
    /// <item>Beside a Decimal, an Integer or a Boolean counts five whole digits, a Byte, a Long and any whole result
    /// ten (an Integer times an Integer is a Long), text and a date 28.</item>
    /// <item><c>*</c> and <c>/</c>: a Decimal with whole numbers, dates, Booleans or text stays a Decimal of its
    /// places (DECIMAL(18,4) 4.5 / 7 is 0.6428; 1.5 / '2.5' is 0.6), its whole digits the most either side counts,
    /// up to a precision of 28 (DECIMAL(10,2) * LONG is DECIMAL(12,2)). Two Decimals are one of the larger places
    /// and the most whole digits where that fits 28 digits (DECIMAL(18,4) * DECIMAL(10,2) is DECIMAL(18,4); 1.5 *
    /// 1.5 is 2.2), and a Double where it does not (1.5 * 1.25 is 1.875, 27 whole digits and two places; DECIMAL
    /// 1.1 * 1.11). A Double or Single, a Currency with a Decimal, or a Currency divided give a Double (DECIMAL 1.1
    /// * Currency 1.1 is 1.21).</item>
    /// <item><c>+</c> and <c>-</c> keep the same rule, except that Currency with a Decimal is a Decimal of the
    /// larger places and precision 28 (1.5 - Currency 3.25 is -1.7500), and Currency with a Double is a
    /// Currency.</item>
    /// <item><c>Sum</c> keeps its argument's places at a precision of 28; <c>Min</c>, <c>Max</c>, <c>First</c> and
    /// <c>Last</c> keep its type, and so do <c>Round</c>, <c>Abs</c>, <c>Int</c> and <c>Fix</c> of a Decimal or Currency
    /// (verified vs ACE: <c>Round([Price],2) + [Qty]/32</c> is a Currency), and — where ACE makes it a Double, LibRed
    /// keeping the type .NET does — a choice its Decimal or Currency arms'. Any other function, a parameter or anything else is not a Decimal; a function
    /// returning text or a date counts as text or a date (1.5 / Left('12', 2) is 0.1), as
    /// <paramref name="declaredType"/> says.</item>
    /// </list>
    /// </summary>
    internal static NumberType NumberTypeOf(
        Expression expression, IReadOnlyList<OutputColumn> columns, Func<Expression, Type?> declaredType)
    {
        return NumberTypeOf(expression, e => e switch
        {
            LiteralExpression { Value: null } => new NumberNode.WrittenNull(),
            LiteralExpression literal => new NumberNode.Known(WrittenNumberType(literal.Value)),
            ColumnReference reference => new NumberNode.Known(OutputColumn.Find(columns, reference) is { } column
                ? ColumnNumberType(column.Currency, column.Scale, column.Precision, column.ClrType)
                : new(NumberClass.Other)),
            UnaryExpression { Operator: UnaryOperator.Negate } negation => new NumberNode.Negation(negation.Operand),
            BinaryExpression { Operator: BinaryOperator.Multiply or BinaryOperator.Divide } product =>
                new NumberNode.Multiplied(product.Left, product.Right, product.Operator == BinaryOperator.Divide),
            BinaryExpression { Operator: BinaryOperator.Add or BinaryOperator.Subtract } sum =>
                new NumberNode.Added(sum.Left, sum.Right, sum.Operator == BinaryOperator.Add),
            BinaryExpression { Operator: BinaryOperator.IntDivide or BinaryOperator.Modulo } =>
                new NumberNode.Known(new(NumberClass.Whole, Integers: WholeDigits(typeof(int)))),
            BinaryExpression { Operator: BinaryOperator.Power } => new NumberNode.Known(new(NumberClass.Double)),
            // A choice keeps its Decimal or Currency arms' type beside whole numbers, as its declared type does
            // (QueryExecutor.UnifiedType), where ACE makes it a Double.
            CaseExpression @case => new NumberNode.Choice(QueryExecutor.CaseResults(@case)),
            FunctionCall { WithinGroup: not null, Arguments: [_, var key] } function
                when function.Name.Equals("PERCENTILE_DISC", StringComparison.OrdinalIgnoreCase) => new NumberNode.Same(key),
            FunctionCall function => new NumberNode.Function(new(function.Name, function.Arguments), () =>
                declaredType(function) is var returns && returns == typeof(string) ? new(NumberClass.Text, Integers: Most)
                    : returns == typeof(DateTime) ? new(NumberClass.Date, Integers: Most)
                    : new(NumberClass.Other)),
            _ => new NumberNode.Known(new(NumberClass.Other)),
        });
    }

    /// <summary>
    /// A value as it goes into a result column of <paramref name="type"/>, converted the OLE Automation way (verified
    /// vs ACE): a Decimal column holds exactly its places, the rest cut off (DECIMAL(18,4) 1.1 / 1.1 is 1.0000), a
    /// Double or Single becoming a Decimal first
    /// (<see cref="JetDecimalConverter"/>), so binary rounding (0.6 held as 0.5999…) cannot drop a digit; a Double
    /// column takes a Decimal as a Double (DECIMAL 1.1 * 1.11 is 1.221); a Currency column takes a Double to four
    /// places, as <see cref="ToCurrency"/> makes one.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object? ToResultPlaces(object? value, NumberType type) => (type.Class, value) switch
    {
        (NumberClass.Decimal, decimal d) =>
            decimal.Round(d, type.Places, MidpointRounding.ToZero) + new decimal(0, 0, 0, false, (byte)type.Places),
        (NumberClass.Decimal, double or float) => CutFloating(value, type.Places),
        (NumberClass.Double, decimal d) => JetDecimalConverter.FromDecimal(d),
        (NumberClass.Currency, double or float) => ToCurrency(value),
        _ => value,
    };

    /// <summary>
    /// A value converted to the type its result column declares, when it has another: a number to a wider number
    /// (a Boolean as -1 or 0, a Double into a Decimal the OLE Automation way), a date to a number as its serial and a
    /// number to a date as CDate reads it, anything to text as <c>&amp;</c> writes it, and anything to binary as its
    /// bytes as a binary column stores them (<see cref="JetTypeCodec.AsBinary"/>), and anything that has none as its text. Null, or no
    /// <paramref name="type"/>, leaves the value as it is.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object? AsColumnType(object? value, Type? type, bool currency)
    {
        if (value is null || type is null || value.GetType() == type)
            return value;
        if (type == typeof(string)) return ConcatText(value);
        if (type == typeof(byte[]))
            return JetTypeCodec.AsBinary(value, currency) ?? Encoding.Unicode.GetBytes(ConcatText(value));
        if (type == typeof(decimal)) return ArithmeticDecimal(value);
        if (type == typeof(double)) return Oa(value);
        if (type == typeof(float)) return (float)Oa(value);   // a Decimal through its OLE Automation double
        if (type == typeof(DateTime)) return ToDate(value);
        if (type == typeof(bool)) return IsTruthy(value);
        return Convert.ChangeType(Numeric(value), type, CultureInfo.InvariantCulture);
    }

    private static object CutFloating(object value, int places)
    {
        try
        {
            decimal exact = value is float f ? JetDecimalConverter.FromSingle(f) : JetDecimalConverter.FromDouble((double)value);
            decimal cut = decimal.Round(exact, places, MidpointRounding.ToZero);
            return value is float ? (float)JetDecimalConverter.FromDecimal(cut) : JetDecimalConverter.FromDecimal(cut);
        }
        catch (OverflowException)
        {
            return value;   // past a Decimal: ACE could not have held it as one either
        }
    }

    /// <summary>Whether two non-null values are equal under the same coercions as <c>=</c> (used by the hash
    /// join to re-check a bucket candidate). Only meaningful within one type kind — see <see cref="KeyHash"/>.</summary>
    public static bool KeyEqual(object a, object b, JetTextComparer text) =>
        a is string sa && b is string sb ? text.Equals(sa, sb) : Compare(a, b, text) == 0;

    /// <summary>A hash for a non-null join key that agrees with <see cref="KeyEqual"/> within a type kind: values
    /// the evaluator treats as equal hash the same (numeric via the Decimal a Double compares as beside one, or the
    /// Double past a Decimal's range; text by its collation key, binary structurally). The planner only builds a
    /// hash join over same-kind key columns, so this is total over the keys it actually sees.</summary>
    public static int KeyHash(object v, JetTextComparer text) => v switch
    {
        byte[] b => BinaryHash(b),
        string s => text.GetHashCode(s),
        _ when IsNumeric(v) => NumericHash(v),
        _ => v.GetHashCode(),
    };

    private static int NumericHash(object v)
    {
        try
        {
            return Dec(v).GetHashCode();
        }
        catch (OverflowException)
        {
            return Dbl(v).GetHashCode();
        }
    }

    private static int BinaryHash(byte[] b)
    {
        var h = new HashCode();
        h.AddBytes(b);
        return h.ToHashCode();
    }

    /// <summary>Orders two values for SORT (nulls first), using the same coercion as comparisons, and text in
    /// <paramref name="text"/>'s collation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CompareForSort(object? a, object? b, JetTextComparer text) => (a, b) switch
    {
        (null, null) => 0,
        (null, _) => -1,
        (_, null) => 1,
        // Two sort keys compare their collation keys, which is what comparing the texts would encode again;
        // one against anything else unwraps to its text and compares as text always does.
        (CollatedText x, CollatedText y) => Math.Sign(x.Key.AsSpan().SequenceCompareTo(y.Key)),
        (CollatedText x, _) => CompareForSort(x.Text, b, text),
        (_, CollatedText y) => CompareForSort(a, y.Text, text),
        _ => Compare(a, b, text),
    };

    /// <summary>A value as a sort key: text carries its collation key, made once, so sorting n rows encodes n
    /// strings rather than two per comparison; anything else is itself. Only for values that are compared with
    /// <see cref="CompareForSort"/> in the same collation and never returned.</summary>
    internal static object? SortKey(object? value, JetTextComparer text) =>
        value is string s ? new CollatedText(s, text.Key(s)) : value;

    /// <summary>A text sort key and its collation key (<see cref="JetTextComparer.Key"/>).</summary>
    internal sealed class CollatedText(string text, byte[] key)
    {
        public string Text { get; } = text;
        public byte[] Key { get; } = key;
        public override string ToString() => Text;
    }
}
