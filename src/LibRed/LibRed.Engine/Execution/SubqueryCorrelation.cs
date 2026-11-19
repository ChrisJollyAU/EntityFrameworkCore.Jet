using LibRed.Catalog;
using LibRed.Engine.Plan;
using LibRed.Engine.Planning;
using LibRed.Sql.Ast;

namespace LibRed.Engine.Execution;

/// <summary>
/// A correlated subquery's WHERE and HAVING split into the equalities that tie it to the outer row and the rest.
/// </summary>
/// <param name="Plan">The body's plan, used to tell inner aliases and column types from outer ones.</param>
/// <param name="InnerKeys">The subquery sides of the correlation equalities.</param>
/// <param name="OuterKeys">The outer sides, positionally matched to <paramref name="InnerKeys" />.</param>
/// <param name="NullSafe">
/// Per key, whether NULL matches NULL there — true for EF's <c>a = b OR (a IS NULL AND b IS NULL)</c> form. A
/// plain <c>=</c> key drops null-bearing rows from the hash and fails a null probe outright, because a null can
/// never satisfy it; a null-safe key must instead hash the null and match it.
/// </param>
/// <param name="KeyTypes">Per key, the type both sides are hashed as, or null to hash them as they are (see
/// <see cref="SubqueryCorrelation.SameKind" />).</param>
/// <param name="Residual">The WHERE conjuncts that were not correlation equalities, re-ANDed, or null if none.</param>
/// <param name="ResidualHaving">
/// The same for HAVING. The key query must use this and not the original HAVING, which still holds the
/// correlation conjunct that was lifted out of it.
/// </param>
internal readonly record struct CorrelationSplit(
    PlanNode Plan,
    IReadOnlyList<Expression> InnerKeys,
    IReadOnlyList<Expression> OuterKeys,
    IReadOnlyList<bool> NullSafe,
    IReadOnlyList<Type?> KeyTypes,
    Expression? Residual,
    Expression? ResidualHaving);

/// <summary>
/// The analysis every decorrelation rewrite starts from: which conjuncts of a subquery's WHERE are
/// <c>inner = outer</c> equalities, and whether removing them leaves a body that means the same thing for every
/// outer row. What each rewrite then does with the split differs — <see cref="ExistsSemiJoin" /> hashes the keys
/// for a membership test, <see cref="ScalarAggregateSemiJoin" /> maps them to one aggregate value each — but this
/// part, and the reasons it declines, is common to all of them.
/// </summary>
internal static class SubqueryCorrelation
{
    /// <summary>
    ///     Splits <paramref name="subquery" />'s WHERE, or returns null when decorrelating would not be sound.
    ///     <paramref name="outerColumns" /> supplies the outer side's types, since a hash is only consistent with
    ///     the evaluator's <c>=</c> within one type kind.
    /// </summary>
    internal static CorrelationSplit? TrySplit(
        SelectStatement subquery,
        IReadOnlyList<OutputColumn> outerColumns,
        HashSet<string> outerAliases,
        JetDatabase database,
        bool lookup = false)
    {
        if (subquery is not { From: not null })
        {
            return null;
        }

        PlanNode plan;
        try
        {
            plan = QueryPlanner.PlanSelect(subquery, database);
        }
        catch (Exception)
        {
            return null; // Unplannable here is not our problem to report; let the normal path raise it.
        }

        HashSet<string> innerAliases = QueryPlanner.SubtreeAliases(plan);

        var innerKeys = new List<Expression>();
        var outerKeys = new List<Expression>();
        var nullSafe = new List<bool>();
        var keyTypes = new List<Type?>();
        var keys = new List<Expression>();
        var asWritten = new List<Expression>();
        var residual = new List<Expression>();
        var residualHaving = new List<Expression>();
        // The keys hashed by their truth (SameKind), which the lookup read for each outer row must take so too.
        var truthKeys = new List<Expression>();

        // HAVING is searched for correlations too, but only for conjuncts whose subquery side is a GROUPING KEY.
        // Such a predicate is constant within a group, so it selects whole groups rather than filtering rows
        // inside them — which is exactly what makes lifting it out equivalent to a WHERE correlation, and what
        // leaves the remaining aggregates (COUNT(*) and friends) computed over the same rows as before. A
        // correlation against an aggregate or a non-grouping column would change which rows the aggregate sees,
        // so it stays put. EF emits the grouping-key form: `GROUP BY o0.CustomerID HAVING COUNT(*) > 30 AND
        // o0.CustomerID = o.CustomerID` (its Contains-over-a-GroupBy shape), which without this ran the whole
        // grouping once per outer row — 3.0 s on Northwind.
        foreach ((Expression conjunct, bool fromHaving) in
            IndexSelection.Conjuncts(subquery.Where).Select(c => (c, false))
                .Concat(IndexSelection.Conjuncts(subquery.Having).Select(c => (c, true))))
        {
            // A correlation is either a plain equality or EF's null-safe form. Both give the same pair of
            // operands; they differ only in what a null on either side means.
            (Expression Left, Expression Right)? operands = conjunct switch
            {
                BinaryExpression { Operator: BinaryOperator.Equal } eq => (eq.Left, eq.Right),
                _ => NullSafeEquality(conjunct),
            };

            if (operands is var (left, right)
                && Orient(left, right, innerAliases, outerAliases) is var (inner, outer)
                && inner is ColumnReference innerCol
                && (!fromHaving || IsGroupingKey(inner, subquery.GroupBy, plan, database))
                && SameKind(innerCol, plan, database, outer, outerColumns, lookup, out Type? keyType, out bool byTruth))
            {
                innerKeys.Add(inner);
                outerKeys.Add(outer);
                nullSafe.Add(conjunct is not BinaryExpression { Operator: BinaryOperator.Equal });
                keyTypes.Add(keyType);
                // A Yes/No key beside a value a lookup takes by its truth is hashed as written where the body is not a
                // lookup, which compares it by truth only where it reads an index for it: it is checked below.
                (!lookup && byTruth ? asWritten : keys).Add(conjunct);
                if (keyType == typeof(bool))
                    truthKeys.Add(conjunct);
            }
            else
            {
                (fromHaving ? residualHaving : residual).Add(conjunct);
            }
        }

        // The body is read for each outer row until reading it once pays (DecorrelationGate), so the two readings must
        // agree: where a Yes/No comparison left in the body is answered from one index beside the keys and from
        // another without them, or a key hashed as written is compared by truth when the body is read for each outer
        // row, or a key hashed by truth is not (beside another key on a column that leads an index ACE looks the row
        // up by that index and compares the Yes/No column as a number — verified vs ACE: s.ID = t.ID AND s.Y = t.V
        // with ID the primary key), the body stays read for each outer row.
        if (IndexSelection.TruthDependsOn(keys, [.. residual, .. residualHaving, .. asWritten], truthKeys, plan, database, outerAliases))
            return null;

        // A body that aggregates is read once grouped by its keys, which refuses a Yes/No key an outer join leaves
        // Null for an unmatched row (QueryExecutor); read for each outer row, the key's own condition drops that row
        // before any grouping does, so the body stays read for each outer row.
        if (QueryPlanner.IsGrouped(subquery)
            && OuterJoined(plan)
            && innerKeys.Any(key => key is ColumnReference column
                && IndexSelection.ResolveColumn(column, plan, database) is { Column.Type: JetDataType.Boolean }))
            return null;

        // Every outer reference must have been consumed as a key, or the body would still vary per outer row.
        //
        // The test is "references no OUTER alias", not "references only inner aliases": the latter (ReferencesOnly)
        // treats a nested subquery as opaque and refuses, which declined an EXISTS whose residual merely holds an
        // outer-INDEPENDENT `IN (…)` — the Delete_Where_predicate_with_GroupBy_aggregate_2 shape. MayReferenceOuter
        // descends into nested subqueries instead, so such a residual is recognised as outer-independent.
        //
        // Unqualified columns are refused outright. A bare name may bind outward and only the evaluator's resolver
        // can tell; the hoisting path settles that with a trial run, but these rewrites commit before the body is
        // ever executed (the key set is built lazily on first probe), so a wrong guess would surface as a query
        // error rather than a fallback. EF always qualifies, so nothing real is lost.
        //
        // The GROUP BY keys and whatever is left of HAVING stay in the key query, so they face the same test as a
        // residual WHERE conjunct.
        if (innerKeys.Count == 0
            || residual.Concat(residualHaving).Concat(subquery.GroupBy)
                .Any(r => SubqueryHoisting.MayReferenceOuter(r, outerAliases)
                    || SubqueryHoisting.HasUnqualifiedColumn(r)))
        {
            return null;
        }

        return new CorrelationSplit(plan, innerKeys, outerKeys, nullSafe, keyTypes, And(residual), And(residualHaving));

        static Expression? And(List<Expression> conjuncts)
            => conjuncts.Count == 0 ? null : conjuncts.Aggregate((a, b) => new BinaryExpression(BinaryOperator.And, a, b));

        static bool OuterJoined(PlanNode node)
            => node is JoinNode { Kind: not (JoinKind.Inner or JoinKind.Cross or JoinKind.CrossApply) }
                    or HashJoinNode { Kind: not (JoinKind.Inner or JoinKind.Cross or JoinKind.CrossApply) }
                || node.Children.Any(OuterJoined);
    }

    /// <summary>
    ///     Whether <paramref name="inner" /> is one of the GROUP BY key expressions — the condition for lifting a
    ///     correlation out of HAVING. Two columns are the same key when they name the same column of the same
    ///     source of <paramref name="from" />, written with or without its qualifier (verified vs ACE) — the one
    ///     output column the executor's grouping check finds for each (QueryExecutor.RequireGrouped). A name more
    ///     than one source has binds to none, and two names for one stored column are two columns (verified vs ACE:
    ///     <c>HAVING d.B &gt; 1</c> where d's A and B both read ID is refused under GROUP BY d.A).
    /// </summary>
    internal static bool IsGroupingKey(Expression inner, IReadOnlyList<Expression> groupBy, PlanNode from, JetDatabase database)
        => groupBy.Any(k => SameOperand(k, inner)
            || (k is ColumnReference key && inner is ColumnReference column
                && string.Equals(key.Column, column.Column, StringComparison.OrdinalIgnoreCase)
                && IndexSelection.ResolveColumn(key, from, database) is { } a
                && IndexSelection.ResolveColumn(column, from, database) is { } b
                && string.Equals(a.Alias, b.Alias, StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    ///     Recognises EF's null-safe equality — <c>a = b OR (a IS NULL AND b IS NULL)</c> — returning the two
    ///     operands, or null when the conjunct is not that shape.
    /// </summary>
    /// <remarks>
    ///     EF emits this wherever a correlation involves a nullable column, so it is not an edge case but the
    ///     common form. Read as one opaque disjunction it looked like a residual mentioning the outer row, which
    ///     declined the whole rewrite: measured on Northwind, the <c>Late_subquery_pushdown</c> shape took
    ///     <b>9,632 ms</b> written this way against <b>55 ms</b> with a plain <c>=</c>, the SQL being otherwise
    ///     identical.
    ///     <para>
    ///         It really is null-safe equality: <c>a = b</c> is UNKNOWN if either side is null, so the disjunction
    ///         is true exactly when both are non-null and equal, or both are null.
    ///     </para>
    /// </remarks>
    private static (Expression Left, Expression Right)? NullSafeEquality(Expression conjunct)
    {
        if (conjunct is not BinaryExpression { Operator: BinaryOperator.Or } or)
        {
            return null;
        }

        // EF puts the equality first, but don't depend on the operand order of either connective.
        foreach ((Expression eqSide, Expression nullSide) in new[] { (or.Left, or.Right), (or.Right, or.Left) })
        {
            if (eqSide is not BinaryExpression { Operator: BinaryOperator.Equal } eq
                || nullSide is not BinaryExpression { Operator: BinaryOperator.And } and)
            {
                continue;
            }

            if ((IsNullTestOf(and.Left, eq.Left) && IsNullTestOf(and.Right, eq.Right))
                || (IsNullTestOf(and.Left, eq.Right) && IsNullTestOf(and.Right, eq.Left)))
            {
                return (eq.Left, eq.Right);
            }
        }

        return null;
    }

    /// <summary>Whether <paramref name="test" /> is <c>operand IS NULL</c> for that same operand.</summary>
    private static bool IsNullTestOf(Expression test, Expression operand)
        => test is UnaryExpression { Operator: UnaryOperator.IsNull, Operand: { } tested } && SameOperand(tested, operand);

    /// <summary>
    ///     Whether two operands of the null-safe form are the same expression. Column references compare
    ///     case-insensitively, since SQL identifiers are; anything else falls back to the AST's own structural
    ///     equality (the expression records are value types by construction).
    /// </summary>
    private static bool SameOperand(Expression a, Expression b)
        => (a, b) switch
        {
            (ColumnReference x, ColumnReference y) =>
                string.Equals(x.Column, y.Column, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Table, y.Table, StringComparison.OrdinalIgnoreCase),
            _ => a.Equals(b),
        };

    /// <summary>Orients an equality so the first element is the subquery side and the second the outer side.</summary>
    private static (Expression Inner, Expression Outer)? Orient(
        Expression left, Expression right, HashSet<string> innerAliases, HashSet<string> outerAliases)
        => IndexSelection.ReferencesOnly(left, innerAliases) && IndexSelection.ReferencesOnly(right, outerAliases)
            ? (left, right)
            : IndexSelection.ReferencesOnly(right, innerAliases) && IndexSelection.ReferencesOnly(left, outerAliases)
                ? (right, left)
                : null;

    /// <summary>
    ///     Whether both sides of a correlation equality are the same type kind. A hash agreeing with the
    ///     evaluator's <c>=</c> only exists within a kind (<c>5 = '5'</c> and <c>5 = 5.0</c>, but
    ///     <c>'5' ≠ '5.0'</c>), so a cross-kind correlation must stay a per-row comparison.
    ///     <paramref name="keyType" /> is the type both sides are hashed as where a Single meets another number, as
    ///     the per-row comparison compares them (measured against ACE): as Singles, but as Doubles where the
    ///     subquery's column is an indexed Double, which the comparison seeks. <paramref name="lookup" /> says the
    ///     subquery asks only whether a matching row exists — EXISTS, IN — which ACE answers by looking the outer
    ///     value up in the subquery's column, in that column's type. <paramref name="byTruth" /> says such a lookup
    ///     compares the two by the outer value's truth, whether or not this is one.
    /// </summary>
    internal static bool SameKind(
        ColumnReference innerCol,
        PlanNode innerPlan,
        JetDatabase database,
        Expression outer,
        IReadOnlyList<OutputColumn> outerColumns,
        bool lookup,
        out Type? keyType,
        out bool byTruth)
    {
        keyType = null;
        byTruth = false;
        if (IndexSelection.ResolveColumn(innerCol, innerPlan, database) is not { } inner
            || IndexSelection.Classify(inner.Column.Type) is not { } innerKind)
        {
            return false;
        }

        // The outer side's kind comes from the scope's CLR types rather than the catalog: it may be any
        // expression over already-materialised rows, not necessarily a base-table column.
        if (outer is not ColumnReference outerCol)
        {
            return false;
        }

        // An outer name more than one outer column has is the evaluator's ambiguity to report, row by row.
        if (OutputColumn.Find(outerColumns, outerCol) is not { } outerColumn)
        {
            return false;
        }
        Type? clr = outerColumn.ClrType;

        // A number or date looked up in a Yes/No column is compared by its truth, so both sides are hashed as
        // Booleans (verified vs ACE: EXISTS (… WHERE s.Y = t.V) holds for a t.V of 1 or 2 where s.Y is True). A
        // Decimal is compared as the number it is; a Currency is not a Decimal here.
        byTruth = inner.Column.Type == JetDataType.Boolean && ExpressionEvaluator.ComparedByTruth(clr, outerColumn.Currency);
        if (lookup && byTruth)
        {
            keyType = typeof(bool);
            return true;
        }

        if (clr is null || KindOf(clr) != innerKind)
        {
            return false;
        }

        if (inner.Column.Type == JetDataType.Single != ((Nullable.GetUnderlyingType(clr) ?? clr) == typeof(float)))
        {
            keyType = inner.Column.Type == JetDataType.Double && IndexSelection.LeadsIndex(inner.Table, inner.Column)
                ? typeof(double)
                : typeof(float);
        }

        return true;
    }

    private static IndexSelection.TypeKind? KindOf(Type t)
    {
        Type u = Nullable.GetUnderlyingType(t) ?? t;
        if (u == typeof(string)) return IndexSelection.TypeKind.Text;
        if (u == typeof(byte[])) return IndexSelection.TypeKind.Binary;
        if (u == typeof(DateTime)) return IndexSelection.TypeKind.Temporal;
        if (u == typeof(Guid)) return IndexSelection.TypeKind.Guid;
        if (u == typeof(bool) || u == typeof(byte) || u == typeof(short) || u == typeof(int) || u == typeof(long)
            || u == typeof(float) || u == typeof(double) || u == typeof(decimal)) return IndexSelection.TypeKind.Numeric;
        return null;
    }
}