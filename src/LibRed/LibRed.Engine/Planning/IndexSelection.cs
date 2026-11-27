using LibRed.Catalog;
using LibRed.Engine.Execution;
using LibRed.Engine.Plan;
using LibRed.Sql.Ast;

namespace LibRed.Engine.Planning;

/// <summary>
/// A post-planning optimizer pass that replaces a <c>Filter(pred) over Scan(t)</c> with
/// <c>Filter(pred) over IndexSeek(t, index, key)</c> when <c>pred</c> has an equality on a column that is the
/// sole column of one of <c>t</c>'s indexes. The <see cref="FilterNode"/> is KEPT: the index key encoding is
/// order-preserving but lossy (text/binary collation), so the seek may over-return and the residual predicate
/// re-checks each candidate — the seek only narrows what has to be read.
/// </summary>
internal static class IndexSelection
{
    // IDE0028's only fix here is `[]`, which would silently drop the comparer.
#pragma warning disable IDE0028
    private static readonly HashSet<string> NoOuter = new(StringComparer.OrdinalIgnoreCase);
#pragma warning restore IDE0028

    /// <summary>How a table is read, which decides how ACE takes a comparison an index could answer.</summary>
    internal enum Reading
    {
        /// <summary>On its own, or per outer row of a correlated body.</summary>
        Alone,
        /// <summary>The body of a row-returning positive EXISTS, looked up for each outer row.</summary>
        ExistsLookup,
        /// <summary>The inner table of a join, looked up for each row of the other side: its own comparisons with a
        /// value of the statement are tested on the fetched row as written.</summary>
        JoinInner,
    }

    /// <param name="node">The plan subtree to rewrite.</param>
    /// <param name="database">The database the statement runs on. A table's candidate indexes are read from the
    /// catalog of the database it is in — this one, or the file its FROM names with IN externaldatabase.</param>
    /// <param name="outer">Aliases of the enclosing query (empty at top level). A value referencing only these
    /// is a constant from this query's view, so a correlated predicate <c>t.col = outer.col</c> becomes an
    /// index seek keyed off the outer row — the point of running index selection on correlated subqueries.</param>
    /// <param name="lookup">How the subtree's table is read (see <see cref="SoughtAsIndexed"/>): alone, as the body of
    /// a positive EXISTS, which ACE runs as a lookup of the body's table for each outer row, or as the inner table of a
    /// join. It reaches the filter over that table through whatever leaves the rows what they are: a projection, a
    /// sort, DISTINCT, TOP.</param>
    /// <param name="above">The aliases whose columns are read above <paramref name="node"/> — by a projection, a sort,
    /// a grouping, its aggregates and HAVING, or a filter — which decides which side of a join ACE drives from
    /// (<see cref="InnerIsLeft"/>). Null where nothing above reads a column.</param>
    public static PlanNode Apply(
        PlanNode node, JetDatabase database, HashSet<string>? outer = null, Reading lookup = Reading.Alone, HashSet<string>? above = null)
    {
        outer ??= NoOuter;
        return node switch
        {
            FilterNode { Input: ScanNode scan } filter => RewriteFilterOverScan(filter, scan, database, outer, lookup),
            JoinNode j => RewriteJoin(j, database, outer, above ?? []),

            // `TOP n … ORDER BY <indexed columns>` — read the index in order and stop after n, instead of
            // scanning the table to feed a sort. Must precede the general SortNode arm below.
            SortNode { Input: ScanNode ordered, Limit: not null } sort
                when OrderedIndexRead(sort, ordered, database) is { } indexScan => indexScan,

            FilterNode { Input: DerivedTableNode derived } filter
                when IntoDerived(filter, derived, database, outer) is { } moved => moved,

            // Otherwise rebuild the node with its children rewritten.
            FilterNode f => f with { Input = Apply(f.Input, database, outer, lookup, Read(above, [f.Predicate], f.Input, database)) },
            SortNode s => s with { Input = Apply(s.Input, database, outer, lookup, Read(above, s.Keys.Select(k => k.Value), s.Input, database)) },
            ProjectNode p => p with
            {
                Input = Apply(p.Input, database, outer, lookup, Read(above, p.Projection.Select(i => i.Value), p.Input, database)),
            },
            DistinctNode d => d with { Input = Apply(d.Input, database, outer, lookup, above) },
            DistinctRowNode dr => dr with { Input = Apply(dr.Input, database, outer, lookup, above) },
            LimitNode l => l with { Input = Apply(l.Input, database, outer, lookup, above) },
            AggregateNode a when HavingBeforeGroups(a, database) is var g => g with
            {
                Input = Apply(g.Input, database, outer, above: Read(null, Grouped(g), g.Input, database)),
            },
            DerivedTableNode dt => dt with { Input = Apply(dt.Input, database, outer) },
            // Must be here, not left to the `_` arm below: that arm does not merely decline to rewrite this
            // node, it stops DESCENDING, so the whole subtree loses index selection. A WindowNode sits directly
            // above the Filter-over-Scan that RewriteFilterOverScan turns into a seek, so omitting this would
            // silently make every windowed query a full scan — correct results, catastrophic plans.
            WindowNode w => w with { Input = Apply(w.Input, database, outer, above: above) },
            SetOperationNode so => so with { Left = Apply(so.Left, database, outer), Right = Apply(so.Right, database, outer) },
            _ => node,
        };

        static IEnumerable<Expression> Grouped(AggregateNode a) =>
            [.. a.GroupBy, .. a.Projection.Select(i => i.Value), .. a.OrderBy.Select(o => o.Value), .. Conjuncts(a.Having)];
    }

    /// <summary><paramref name="above"/> with the aliases <paramref name="values"/> read columns of, each column
    /// attributed to the source of <paramref name="input"/> it binds to (<see cref="ResolveColumn"/>). COUNT(*)'s star
    /// reads no column; a bare * reads every source. A subquery reads its own FROM, and a name no source binds (an
    /// output's alias) none of the input's.</summary>
    private static HashSet<string> Read(HashSet<string>? above, IEnumerable<Expression> values, PlanNode input, JetDatabase database)
    {
        var read = new HashSet<string>(above ?? [], database.Catalog.Names);
        foreach (Expression value in values)
            ColumnPruning.Walk(value, node =>
            {
                switch (node)
                {
                    case FunctionCall { Arguments: [StarExpression] }:
                    case ScalarSubquery or ExistsExpression or InSubqueryExpression:
                        return false;
                    case StarExpression:
                        read.UnionWith(QueryPlanner.SubtreeAliases(input, database.Catalog.Names));
                        return false;
                    case QualifiedStarExpression star:
                        read.Add(star.Table);
                        return false;
                    case ColumnReference column:
                        if ((column.Table ?? ResolveColumn(column, input, database)?.Alias) is { } alias)
                            read.Add(alias);
                        return false;
                    default:
                        return true;
                }
            });
        return read;
    }

    /// <summary>
    /// <paramref name="a"/> with each HAVING conjunct that reads only its grouping columns moved into its input, as a
    /// WHERE is placed (an unqualified column on the source it binds to); <paramref name="a"/> itself when none moves.
    /// ACE applies such a condition to the rows before grouping them, and binds an unqualified column in it to the
    /// one source of the FROM that has a column of that name (verified vs ACE: <c>HAVING Y = 2</c> on an indexed
    /// Yes/No column is answered from the index, as <c>WHERE Y = 2</c> is, over a join as over one table).
    /// </summary>
    private static AggregateNode HavingBeforeGroups(AggregateNode a, JetDatabase database)
    {
        if (a.Having is null || a.GroupBy.Count == 0)
            return a;
        var before = new List<Expression>();
        var rest = new List<Expression>();
        foreach (Expression conjunct in Conjuncts(a.Having))
            (!QueryPlanner.HasAggregate(conjunct) && Grouped(conjunct) ? before : rest).Add(conjunct);
        if (before.Count == 0)
            return a;

        return a with
        {
            Input = QueryPlanner.PushPredicates(a.Input, QueryPlanner.CombineAnd(before), database),
            Having = rest.Count == 0 ? null : QueryPlanner.CombineAnd(rest),
        };

        bool Grouped(Expression e) => SubqueryCorrelation.IsGroupingKey(e, a.GroupBy, a.Input, database) || e switch
        {
            LiteralExpression or ParameterExpression => true,
            ColumnReference or ScalarSubquery or ExistsExpression or InSubqueryExpression => false,
            _ => e.Operands()?.All(Grouped) ?? false,
        };
    }

    /// <summary>
    /// A filter over a derived table — which is how a saved query is read — with each conjunct that reads only the
    /// derived table's plain columns moved into its own FROM, where the seeks of its table can serve it. Left above
    /// it, a WHERE on a saved query read the saved query's table whole. Null when nothing moves.
    /// </summary>
    /// <remarks>A conjunct moves down through a projection, a sort and DISTINCTROW, each of which passes a row on
    /// whole or not at all, so filtering their input drops the same rows; and through a GROUP BY when it reads only the
    /// grouping columns, which drops whole groups (see the AggregateNode arm). Not through TOP, a window, DISTINCT or a
    /// set operation, whose rows depend on the rows that would be filtered out first — so not through a grouping that
    /// numbers its groups with a window, unlike a HAVING beside that window (<see cref="HavingBeforeGroups"/>), which
    /// the window follows either way.</remarks>
    private static PlanNode? IntoDerived(FilterNode filter, DerivedTableNode derived, JetDatabase database, HashSet<string> outer)
    {
        var moved = new List<Expression>();
        var kept = new List<Expression>();
        foreach (Expression conjunct in Conjuncts(filter.Predicate))
        {
            if (InDerived(conjunct) is { } inner)
                moved.Add(inner);
            else
                kept.Add(conjunct);
        }
        if (moved.Count == 0 || Into(derived.Input, QueryPlanner.CombineAnd(moved)) is not { } body)
            return null;

        PlanNode result = derived with { Input = Apply(body, database, outer) };
        return kept.Count == 0 ? result : new FilterNode(result, QueryPlanner.CombineAnd(kept));

        PlanNode? Into(PlanNode node, Expression predicate) => node switch
        {
            ProjectNode p => Into(p.Input, predicate) is { } input ? p with { Input = input } : null,
            SortNode { Limit: null, Ties: null } s => Into(s.Input, predicate) is { } input ? s with { Input = input } : null,
            DistinctRowNode d => Into(d.Input, predicate) is { } input ? d with { Input = input } : null,
            // Only a grouping column of the derived table reaches here as a column (DerivedColumn), and a row its
            // group would be dropped for can be dropped before the grouping (verified vs ACE, which answers a WHERE
            // on the grouping column of a derived table from the table's index).
            AggregateNode { Windows: null or [], Ties: null } a => Into(a.Input, predicate) is { } input ? a with { Input = input } : null,
            ScanNode or JoinNode or FilterNode => QueryPlanner.PushPredicates(node, predicate, database),
            _ => null,
        };

        // The conjunct in the derived table's own columns, or null when it reads anything else — a subquery, which
        // may read the derived table, among them.
        Expression? InDerived(Expression conjunct)
        {
            bool movable = true;
            Expression inner = Map(conjunct);
            return movable ? inner : null;

            Expression Map(Expression e)
            {
                switch (e)
                {
                    case ColumnReference c when c.Table is null || database.Catalog.Names.Equals(c.Table, derived.Alias):
                        if (DerivedColumn(derived, c.Column, database) is { } column)
                            return column;
                        break;
                    case LiteralExpression or ParameterExpression or SystemVariableExpression:
                        return e;
                    case not ColumnReference when e.Operands() is not null:
                        return e.MapOperands(Map);
                }
                movable = false;
                return e;
            }
        }
    }

    /// <summary>The column, in its own FROM's scope, that <paramref name="derived"/> outputs as <paramref name="name"/>:
    /// the one item of its projection with that name, where it is a column — or by position where a column list
    /// renames them — or, where no item has the name, the column a <c>*</c> takes from the one table it reads. Null
    /// for anything else: an expression, a name projected twice (which ACE names apart), a <c>*</c> over more than
    /// one table, or a derived table that is not a projection of its FROM.</summary>
    private static ColumnReference? DerivedColumn(DerivedTableNode derived, string name, JetDatabase database)
    {
        PlanNode top = derived.Input;
        while (top is LimitNode or DistinctNode)
            top = top.Children[0];
        IReadOnlyList<SelectItem>? projection = top switch
        {
            ProjectNode p => p.Projection,
            // Of a grouped query's items only its grouping columns are columns.
            AggregateNode a when a.GroupBy.All(g => g is ColumnReference) => a.Projection,
            // A SELECT * of one source plans no projection.
            ScanNode or JoinNode or FilterNode or SortNode or DerivedTableNode => [new SelectItem(new StarExpression(), null)],
            _ => null,
        };
        if (projection is null)
            return null;

        bool star = projection.Any(i => i.Value is StarExpression or QualifiedStarExpression);
        IEqualityComparer<string> names = database.Catalog.Names;
        if (derived.Columns is { } columns)
        {
            int position = columns.ToList().FindIndex(n => names.Equals(n, name));
            return position >= 0 && position < projection.Count && !star ? projection[position].Value as ColumnReference : null;
        }

        var items = projection.Where(i => i.Value is not (StarExpression or QualifiedStarExpression)
            && names.Equals(i.Alias ?? (i.Value as ColumnReference)?.Column, name)).ToList();
        if (items.Count > 0 || !star)
            return items is [{ Value: ColumnReference column }] ? column : null;

        // The one source the * takes its columns from.
        return (BaseTables(top).ToList(), DerivedTables(top).ToList()) switch
        {
            ([var (alias, table, external)], []) when ExternalDatabases.Open(database, external).Catalog.FindTable(table)?.FindColumn(name) is not null
                => new ColumnReference(alias, name),
            ([], [var source]) when DerivedColumn(source, name, database) is not null => new ColumnReference(source.Alias, name),
            _ => null,
        };
    }

    /// <summary>
    /// <paramref name="predicate"/> with each comparison of a column that ACE answers from an index — <c>=</c>,
    /// <c>&lt;</c>, <c>&gt;</c>, <c>&lt;=</c>, <c>&gt;=</c>, IN and BETWEEN — taking the value it compares the column
    /// with as the seek of that index takes it: a Yes/No column's by its truth, and any other but a text column's as
    /// storing it in the column converts it (<see cref="SoughtValue"/>), where it is not written or another column.
    /// The filter kept above a seek then answers as the index does, whichever index LibRed itself reads through.
    /// </summary>
    /// <remarks>
    /// <para>Verified vs ACE, with the column indexed and not: indexed, <c>Y = 2</c>, <c>Y IN (2)</c> and a
    /// correlated <c>s.Y = t.V</c> with <c>t.V</c> 1 or 2 find the True rows, where unindexed they find none.
    /// <c>&lt;&gt;</c> compares as numbers either way (though <c>NOT (Y &lt;&gt; 2)</c> is <c>Y = 2</c>), and so does a
    /// written Decimal (<c>Y = 2.5</c> finds nothing) and a Decimal column, though not a Decimal the statement
    /// computes or is passed.</para>
    /// <para>Which comparisons an index answers, each verified vs ACE:</para>
    /// <list type="bullet">
    /// <item><description>The column leads the index, or follows columns that conjuncts of their own hold equal to a
    /// value: with an index on (ID, Y), <c>ID = 1 AND Y >= 2</c> finds the True row.</description></item>
    /// <item><description>Under OR, only where an index answers every branch whole: <c>Y = 2 OR ID = 5</c> with ID
    /// indexed, but not <c>Y = 2 OR N = 99</c> with N unindexed, nor <c>Y = 2 OR (ID = 5 AND N = 5)</c>.</description></item>
    /// <item><description>Where a value comes from the outer row of a correlated subquery, ACE reads through one
    /// index, and the Yes/No column's is the one only when no other conjunct could use another: beside
    /// <c>ID = 1</c>, <c>ID &lt; 3</c> or <c>ID = t.ID</c> on a primary key, <c>s.Y = t.V</c> and <c>s.Y = 2</c>
    /// compare as numbers. A conjunct the same index takes does not count: a column before the Yes/No one held
    /// equal, or the column next after it when the Yes/No comparison is an equality.</description></item>
    /// <item><description><see cref="Reading.ExistsLookup"/>, a positive EXISTS that holds a column of the table equal to
    /// the outer row's, is a lookup of the table instead: an equality of the Yes/No column with the outer row is by
    /// truth with or without an index, a range against the outer row never is, and a comparison with a value of
    /// the statement's own is answered as if the outer row's conjuncts were not there.</description></item>
    /// </list>
    /// </remarks>
    private static Expression SoughtAsIndexed(
        Expression predicate, string alias, TableDefinition def, HashSet<string> outer, Reading reading, IEqualityComparer<string> names)
    {
        List<Expression> written = [.. Conjuncts(predicate)];
        List<Expression> conjuncts = [.. written.Select(Plain)];
        var restrictions = conjuncts.Select(c => Restriction(c, alias, def, outer, names)).ToList();
        bool lookup = reading == Reading.ExistsLookup && restrictions.Any(r => r is { Joined: true, Equal: true });

        bool changed = false;
        for (int i = 0; i < conjuncts.Count; i++)
        {
            Expression conjunct = conjuncts[i];
            Expression by = conjunct is BinaryExpression { Operator: BinaryOperator.Or }
                ? Indexed(conjunct) ? Branches(conjunct) : conjunct
                : restrictions[i] is { } sought && Classify(sought.Column.Type) is not (null or TypeKind.Text)
                    && !TestedAsWritten(sought.Column, sought.Joined, reading) && FromIndex(def, restrictions, i, lookup)
                    ? AsIndexed(conjunct, sought.Column, sought.ColumnLeft, lookup && sought.Joined)
                : conjunct;
            if (!ReferenceEquals(by, conjunct))
                (written[i], changed) = (by, true);
        }
        return changed ? QueryPlanner.CombineAnd(written) : predicate;

        // NOT (a <> b) is a = b — but for a written Null b, beside which a <> b is a Is Not Null (ExpressionEvaluator).
        // And a column that is not true — a <> True, NOT (a = True), NOT a — is a = False, which an index seeks as it
        // seeks a = False (verified vs ACE on an indexed binary column, where all four find only the row holding the
        // bytes 0x0000; on an indexed number, date, text or Yes/No column the four select the same rows, which
        // YesNoComparisonAccessTests asks).
        static Expression Plain(Expression e) => e switch
        {
            UnaryExpression
            {
                Operator: UnaryOperator.Not,
                Operand: BinaryExpression { Operator: BinaryOperator.NotEqual, Right: not LiteralExpression { Value: null } } unequal,
            } => unequal with { Operator = BinaryOperator.Equal },
            BinaryExpression { Operator: BinaryOperator.NotEqual, Right: LiteralExpression { Value: true } } notTrue =>
                notTrue with { Operator = BinaryOperator.Equal, Right = new LiteralExpression(false) },
            UnaryExpression { Operator: UnaryOperator.Not, Operand: BinaryExpression { Operator: BinaryOperator.Equal, Right: LiteralExpression { Value: true } } isTrue } =>
                isTrue with { Right = new LiteralExpression(false) },
            UnaryExpression { Operator: UnaryOperator.Not, Operand: ColumnReference column } =>
                new BinaryExpression(BinaryOperator.Equal, column, new LiteralExpression(false)),
            _ => e,
        };

        bool Indexed(Expression e) => e is BinaryExpression { Operator: BinaryOperator.And or BinaryOperator.Or } b
            ? Indexed(b.Left) && Indexed(b.Right)
            : Restriction(Plain(e), alias, def, outer, names) is { } r && LeadsIndex(def, r.Column);

        Expression Branches(Expression e) => e is BinaryExpression { Operator: BinaryOperator.And or BinaryOperator.Or } b
            ? b with { Left = Branches(b.Left), Right = Branches(b.Right) }
            : Restriction(Plain(e), alias, def, outer, names) is { } r && Classify(r.Column.Type) is not (null or TypeKind.Text)
                ? AsIndexed(Plain(e), r.Column, r.ColumnLeft, looked: false)
            : e;

        // columnLeft: which side of a comparison Restriction found the column on. looked: the comparison holds the
        // column to the outer row of a positive EXISTS, which ACE looks the row up by.
        Expression AsIndexed(Expression comparison, ColumnDef column, bool columnLeft, bool looked) =>
            column.Type == JetDataType.Boolean ? Truth(comparison, columnLeft, looked) : Sought(comparison, column, columnLeft);

        // Each value that is not written — a parameter, or anything computed, False included — nor another column, in
        // a SoughtValue: a bound under <, <=, >, >= and BETWEEN, an equality under = and IN.
        Expression Sought(Expression comparison, ColumnDef column, bool columnLeft)
        {
            var reference = new ColumnReference(alias, column.Name);
            Expression Value(Expression value, bool bound) =>
                value is ParameterExpression || ExpressionEvaluator.IsComputed(value) ? new SoughtValue(reference, value, bound) : value;
            return comparison switch
            {
                BinaryExpression b => columnLeft
                    ? b with { Right = Value(b.Right, b.Operator != BinaryOperator.Equal) }
                    : b with { Left = Value(b.Left, b.Operator != BinaryOperator.Equal) },
                InListExpression list => list with { Items = [.. list.Items.Select(item => Value(item, bound: false))] },
                BetweenExpression between => between with { Low = Value(between.Low, bound: true), High = Value(between.High, bound: true) },
                _ => comparison,
            };
        }

        Expression Truth(Expression comparison, bool columnLeft, bool looked) => comparison switch
        {
            BinaryExpression b => columnLeft
                ? ByTruth(b, b.Right, value => b with { Right = value }, looked)
                : ByTruth(b, b.Left, value => b with { Left = value }, looked),
            InListExpression list => list with { Items = [.. list.Items.Select(item => ByTruth(item, item, value => value, looked))] },
            BetweenExpression between => between with
            {
                Low = ByTruth(between.Low, between.Low, value => value, looked),
                High = ByTruth(between.High, between.High, value => value, looked),
            },
            _ => comparison,
        };

        // What is written in place of `unchanged` once `value` is taken by its truth. A written Null, Boolean,
        // Decimal or text stays as written. A column's value — an enclosing query's — is not known to be a Decimal or
        // text until a row gives it, so its type decides then (ComparedAsWritten, which asks
        // ExpressionEvaluator.ComparedByTruth): a value that is not compared by its truth keeps what was written — a
        // text column beside a Yes/No column is still the type mismatch it was. Where the outer row of a positive
        // EXISTS is looked up (looked), a value the statement computes is compared as written (verified vs ACE:
        // S.Y = DT.D * 1, S.Y = DT.C * 1, S.Y = DT.ID * 1 and S.Y = CLng(DT.ID) find only what they equal as numbers).
        static Expression ByTruth(Expression unchanged, Expression value, Func<Expression, Expression> withValue, bool looked)
        {
            Expression AsWrittenWhen(Expression test) => new FunctionCall("IIf",
            [
                test,
                unchanged,
                withValue(new FunctionCall("CBool", [value])),
            ]);
            return value switch
            {
                LiteralExpression { Value: null or bool or decimal or string } => unchanged,
                ColumnReference => AsWrittenWhen(new ComparedAsWritten(value)),
                _ when looked => unchanged,
                _ => withValue(new FunctionCall("CBool", [value])),
            };
        }
    }

    /// <summary>Whether an index of <paramref name="def"/> answers the comparison <paramref name="restrictions"/> holds
    /// at <paramref name="at"/>, beside the others (<see cref="SoughtAsIndexed"/>); <paramref name="lookup"/>: the table
    /// is the looked-up body of a positive EXISTS that holds a column equal to the outer row's.</summary>
    /// <remarks>Which index ACE reads through beside the outer row's conjuncts is measured for a Yes/No column only. For
    /// any other column the outer row's other conjuncts are left out, so its comparison is taken alike with a
    /// correlation beside it or without one.</remarks>
    private static bool FromIndex(TableDefinition def, List<ColumnRestriction?> restrictions, int at, bool lookup)
    {
        var sought = restrictions[at]!.Value;
        bool joined = restrictions.Any(r => r is { Joined: true });
        bool byOuter = sought.Column.Type == JetDataType.Boolean;
        if (byOuter && lookup && sought.Joined)
            return sought.Equal && !restrictions.Where((_, j) => j != at).Any(r =>
                r is { Joined: true, Equal: true } other && LeadsIndex(def, other.Column));
        return def.Indexes.Any(index =>
        {
            int position = Position(index, sought.Column);
            if (position < 0 || !Enumerable.Range(0, position).All(before => restrictions.Any(r =>
                    r is { Equal: true } held && !(byOuter ? lookup && held.Joined : held.Joined) && Position(index, held.Column) == before)))
                return false;
            if (!byOuter || lookup || !joined)
                return true;
            for (int j = 0; j < restrictions.Count; j++)
            {
                if (j == at || restrictions[j] is not { } other || !LeadsIndex(def, other.Column))
                    continue;
                int p = Position(index, other.Column);
                if (p != position && !(p >= 0 && p < position && other.Equal) && !(p == position + 1 && sought.Equal))
                    return false;
            }
            return true;
        });
    }

    /// <summary>Whether ACE tests a comparison of <paramref name="column"/> on the fetched row as written rather than
    /// through an index: a number or Yes/No column of a join's inner table compared with a value of the statement's own,
    /// not the outer row's (<paramref name="joined"/>) (verified vs ACE; a binary or GUID column's comparison is still
    /// taken as the index takes it there).</summary>
    private static bool TestedAsWritten(ColumnDef column, bool joined, Reading reading) =>
        reading == Reading.JoinInner && !joined && Classify(column.Type) == TypeKind.Numeric;

    /// <summary>What <paramref name="conjunct"/> restricts, where it compares one column of the table read as
    /// <paramref name="alias"/> with values known without a row of it — <c>=</c>, <c>&lt;</c>, <c>&gt;</c>, <c>&lt;=</c>,
    /// <c>&gt;=</c>, IN or BETWEEN, the column on either side: the column, the values, whether it is held to one
    /// value, and whether a value is the outer row's. Null for anything else, and for <c>col = True</c> on a column
    /// that is not Yes/No or <c>col = False</c> on a text column, which test the column's truth rather than compare
    /// it (ExpressionEvaluator.TruthTest; verified vs ACE, with and without an index: -1, 1 and 2 all qualify for
    /// True, '0' for False), so select rows no single key holds.</summary>
    internal static ColumnRestriction? Restriction(
        Expression conjunct, string alias, TableDefinition def, HashSet<string> outer, IEqualityComparer<string> names)
    {
        // ColumnLeft: whether the column is a comparison's left operand (an IN's or BETWEEN's always is).
        (Expression Column, Expression[] Values, bool Equal, bool ColumnLeft)? parts = conjunct switch
        {
            BinaryExpression
            {
                Operator: BinaryOperator.Equal or BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual
                    or BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual
            } b => Column(b.Left, alias, def, names) is not null && Seekable(b.Right, outer)
                ? (b.Left, [b.Right], b.Operator == BinaryOperator.Equal, true)
                : (b.Right, [b.Left], b.Operator == BinaryOperator.Equal, false),
            InListExpression { Negated: false } list => (list.Value, [.. list.Items], false, true),
            BetweenExpression { Negated: false } between => (between.Value, [between.Low, between.High], false, true),
            _ => null,
        };
        if (parts is not { } p || Column(p.Column, alias, def, names) is not { } reference || !p.Values.All(v => Seekable(v, outer)))
            return null;
        ColumnDef column = def.FindColumn(reference.Column)!;
        bool truthTested = p.Equal && column.Type != JetDataType.Boolean && p.Values is [LiteralExpression { Value: bool truth }]
            && (truth || Classify(column.Type) == TypeKind.Text);
        return truthTested ? null : new ColumnRestriction(column, p.Values, p.Equal, p.Values.Any(HasColumnRef), p.ColumnLeft);
    }

    /// <summary>What a conjunct restricts (<see cref="Restriction"/>): the <paramref name="Column"/>, the
    /// <paramref name="Values"/> it is compared with, whether it is held to one value (<paramref name="Equal"/>),
    /// whether a value is the outer row's (<paramref name="Joined"/>), and whether the column is the comparison's left
    /// operand (<paramref name="ColumnLeft"/>; an IN's or BETWEEN's always is).</summary>
    internal readonly record struct ColumnRestriction(ColumnDef Column, Expression[] Values, bool Equal, bool Joined, bool ColumnLeft);

    /// <summary>
    /// Whether a comparison among <paramref name="rest"/> that an index answers is taken differently with
    /// <paramref name="keys"/> — the conjuncts that hold a subquery's columns equal to its outer row's — beside it
    /// than without them (<see cref="SoughtAsIndexed"/>), or whether a key among <paramref name="byTruth"/>, which the
    /// subquery read once compares by its truth, is not compared so by the lookup of a positive EXISTS read for each
    /// outer row. A subquery read once for every outer row, with its keys taken out, would then answer differently
    /// from the same subquery read for each.
    /// </summary>
    internal static bool TruthDependsOn(
        IReadOnlyList<Expression> keys, IReadOnlyList<Expression> rest, IReadOnlyList<Expression> byTruth,
        PlanNode plan, JetDatabase database, HashSet<string> outer)
    {
        if (rest.Count == 0 && byTruth.Count == 0)
            return false;
        Expression beside = QueryPlanner.CombineAnd([.. rest, .. keys]);
        IEqualityComparer<string> names = database.Catalog.Names;
        List<(string Alias, TableDefinition Def)> tables = [.. BaseTables(plan)
            .Select(t => (t.Alias, Def: ExternalDatabases.Open(database, t.Database).Catalog.FindTable(t.Table)))
            .Where(t => t.Def is not null).Select(t => (t.Alias, t.Def!))];
        if (byTruth.Count > 0)
        {
            List<Expression> written = [.. Conjuncts(beside)];
            var taken = new HashSet<Expression>(ReferenceEqualityComparer.Instance);
            foreach (var (alias, def) in tables)
                foreach (var (sought, conjunct) in Conjuncts(SoughtAsIndexed(beside, alias, def, outer, Reading.ExistsLookup, names)).Zip(written))
                    if (!ReferenceEquals(sought, conjunct))
                        taken.Add(conjunct);
            if (byTruth.Any(key => !taken.Contains(key)))
                return true;
        }
        if (rest.Count == 0)
            return false;
        Expression alone = QueryPlanner.CombineAnd(rest);
        return tables.Any(t => !Conjuncts(SoughtAsIndexed(beside, t.Alias, t.Def, outer, Reading.Alone, names)).Take(rest.Count)
            .SequenceEqual(Conjuncts(SoughtAsIndexed(alone, t.Alias, t.Def, NoOuter, Reading.Alone, names))));
    }

    private static FilterNode RewriteFilterOverScan(FilterNode filter, ScanNode scan, JetDatabase database, HashSet<string> outer, Reading reading)
    {
        if (ExternalDatabases.Open(database, scan.Database).Catalog.FindTable(scan.Table) is not { } def)
            return filter;
        string alias = scan.Alias ?? scan.Table;
        IEqualityComparer<string> names = database.Catalog.Names;
        filter = filter with { Predicate = SoughtAsIndexed(filter.Predicate, alias, def, outer, reading, names) };
        var conjuncts = Conjuncts(filter.Predicate).ToList();

        // A seek converts its key itself (ExpressionEvaluator.TryGetSeekKey), so it is keyed by the value a
        // SoughtValue holds; the node is for the filter, which then converts it the same way.
        static Expression Key(Expression value) => value is SoughtValue sought ? sought.Value : value;

        // 1. Equality seek. Collect every `col = value` where col is a column of this scan and value is
        //    evaluable without a row of it (a constant, a parameter, or — in a correlated subquery — an outer
        //    column), then use the index (composite preferred) all of whose columns are so constrained. This is
        //    an exact point seek; a fully-constrained composite key (e.g. a correlated PK match) hits one row.
        var equalities = new Dictionary<string, Expression>(def.Names);
        foreach (Expression conjunct in conjuncts)
        {
            // A comparison tested as written is not one a seek, which converts its key to the column's type, can serve:
            // a Yes/No key is taken by its truth, and a key the column cannot hold seeks nothing where the comparison
            // as written errs.
            if (Restriction(conjunct, alias, def, outer, names) is { Equal: true, Values: [var value] } match
                && !TestedAsWritten(match.Column, match.Joined, reading))
                equalities.TryAdd(match.Column.Name, Key(value));
        }
        if (equalities.Count > 0)
        {
            // Most columns first, so a correlated composite PK match is preferred over a single-column index.
            // Select the widest match without sorting every index. Keep the first on a tie, as the
            // stable ordering did, and retain every index column in its declared key order.
            IndexDef? chosen = null;
            foreach (IndexDef index in def.Indexes)
                if ((chosen is null || index.Columns.Count > chosen.Columns.Count)
                    && index.Columns.All(c => equalities.ContainsKey(c.Column.Name)))
                    chosen = index;
            if (chosen is not null)
            {
                var keys = chosen.Columns.Select(c => equalities[c.Column.Name]).ToList();
                return filter with { Input = new IndexSeekNode(scan.Table, scan.Alias, chosen, keys, scan.Database) { Decode = scan.Decode, RowIds = scan.RowIds } };
            }
        }

        // 2. Range comparison(s) on a single-column index → a range scan (lower and/or upper bound).
        foreach (IndexDef index in def.Indexes.Where(i => i.Columns.Count == 1
            // A bound is never another table's column (Bound, Between), so never the outer row's.
            && !TestedAsWritten(i.Columns[0].Column, joined: false, reading)))
        {
            string colName = index.Columns[0].Column.Name;
            Expression? low = null, high = null;
            bool unordered = false;
            foreach (Expression conjunct in conjuncts)
            {
                // A BETWEEN's two bounds are ordered only when the seek runs, so they are taken as a pair or not at
                // all — never mixed with a bound another conjunct set. The FilterNode above re-checks every conjunct.
                if (Between(conjunct, colName, alias, def, names) is { } range)
                {
                    if (low is null && high is null)
                        (low, high, unordered) = (Key(range.Low), Key(range.High), true);
                    continue;
                }
                if (unordered)
                    continue;
                if (Bound(conjunct, colName, alias, def, names) is not { } b)
                    continue;
                if (b.Op is BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual)
                    low ??= Key(b.Value);
                else
                    high ??= Key(b.Value); // LessThan / LessThanOrEqual
            }
            if (low is not null || high is not null)
                return filter with
                {
                    Input = new IndexRangeSeekNode(scan.Table, scan.Alias, index, low, high, scan.Database)
                    {
                        Unordered = unordered,
                        Decode = scan.Decode,
                        RowIds = scan.RowIds,
                    },
                };
        }

        return filter;
    }

    /// <summary>
    /// Turns <c>Sort(keys, bound) over Scan(t)</c> into an ordered read of an index whose key IS those sort keys,
    /// dropping the sort: the rows already arrive in the wanted order, so the enclosing <c>TOP n</c> stops after
    /// n instead of the sort consuming the whole table to find them. Returns null when no index qualifies.
    /// </summary>
    /// <remarks>
    /// <para>Only applied under a bound (an enclosing <c>TOP n</c>), which is where the win is. Unbounded, it is
    /// a wash: an ordered index read costs ~2.7 µs/row against ~0.44 µs/row for a scan plus ~2.4 µs/row to sort,
    /// so reading the whole table through the index buys nothing but a smaller peak footprint (measured — see
    /// test/LibRed.Benchmarks, `sort.by_indexed` against `StorageBenchmarks.RangeSeek`). With the bound, only n
    /// rows are ever read.</para>
    /// <para>The conditions are what make the substitution produce the <b>same rows in the same order</b>, not
    /// merely a valid order:</para>
    /// <list type="bullet">
    /// <item><description><b>Every index column is a sort key, in the same order, all ascending.</b> A mere
    /// prefix match would leave the index ordering ties by its remaining columns while the sort leaves them in
    /// input order — a legal but different answer, and one that would silently change results EF asserts on.</description></item>
    /// <item><description><b>Numeric or temporal columns only.</b> Their key encoding is order-preserving with no
    /// collation involved, so index order and <see cref="ExpressionEvaluator.CompareForSort"/> cannot disagree.
    /// Text keys are NLS sort keys and lossy; whether they order identically to the evaluator is a separate
    /// question that wants checking against ACE before being relied on here.</description></item>
    /// <item><description><b>Not a WITH IGNORE NULL index</b>, which omits all-null-keyed rows from the B-tree
    /// altogether — reading it in order would silently drop rows. An ordinary index stores nulls, sorted first
    /// ascending (<c>AscNull</c> 0x00 &lt; <c>AscStart</c> 0x7F), exactly as CompareForSort puts them.</description></item>
    /// </list>
    /// <para>Ties then match too: equal-keyed index entries are ordered by row pointer, i.e. physical position,
    /// which is the order a scan feeds the sort in — and the sort breaks ties by input position.</para>
    /// </remarks>
    private static IndexRangeSeekNode? OrderedIndexRead(SortNode sort, ScanNode scan, JetDatabase database)
    {
        if (ExternalDatabases.Open(database, scan.Database).Catalog.FindTable(scan.Table) is not { } def)
            return null;
        string alias = scan.Alias ?? scan.Table;

        foreach (IndexDef index in def.Indexes)
        {
            if (index.IgnoreNulls || index.RootPage <= 0 || index.Columns.Count != sort.Keys.Count)
                continue;

            bool matches = true;
            for (int i = 0; i < sort.Keys.Count && matches; i++)
            {
                (ColumnDef indexColumn, bool ascending) = index.Columns[i];
                matches = ascending
                    && sort.Keys[i].Direction == SortDirection.Ascending
                    && Classify(indexColumn.Type) is TypeKind.Numeric or TypeKind.Temporal
                    && Column(sort.Keys[i].Value, alias, def, database.Catalog.Names) is { } key
                    && def.Names.Equals(key.Column, indexColumn.Name);
            }

            // Both bounds open: the range seek descends to the leftmost leaf and walks the chain, which is the
            // whole index in key order — lazily, so the enclosing limit ends the read.
            if (matches)
                return new IndexRangeSeekNode(scan.Table, scan.Alias, index, Low: null, High: null, scan.Database) { RowIds = scan.RowIds };
        }

        return null;
    }

    /// <summary>Whether <paramref name="e"/> can be evaluated as a seek key without a row of the table being
    /// scanned — a constant, a parameter, or a column of the enclosing (outer) query in a correlated subquery.</summary>
    private static bool Seekable(Expression e, HashSet<string> outer) => ReferencesOnly(e, outer);

    /// <summary>If <paramref name="conjunct"/> is a range comparison of column <paramref name="colName"/> against
    /// a value (either orientation), returns the operator as if the column were on the left (so <c>5 &lt; K</c>
    /// yields <c>K &gt; 5</c>) and the value expression; else null.</summary>
    private static (BinaryOperator Op, Expression Value)? Bound(
        Expression conjunct, string colName, string alias, TableDefinition def, IEqualityComparer<string> names)
    {
        if (conjunct is not BinaryExpression { Operator: var op } cmp
            || op is not (BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual
                or BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual))
            return null;

        bool IsCol(Expression e) => Column(e, alias, def, names) is { } c && def.Names.Equals(c.Column, colName);
        if (IsCol(cmp.Left) && !HasColumnRef(cmp.Right)) return (op, cmp.Right);
        if (IsCol(cmp.Right) && !HasColumnRef(cmp.Left)) return (Flip(op), cmp.Left);
        return null;
    }

    /// <summary>If <paramref name="conjunct"/> is <c>col BETWEEN a AND b</c> on column <paramref name="colName"/>, its
    /// two bounds as written; else null.</summary>
    /// <remarks>BETWEEN takes its bounds in either order, and the executor puts them in the index's order once their
    /// values are known (<see cref="IndexRangeSeekNode.Unordered"/>), so a bound is anything without a column in it, as
    /// for <see cref="Bound"/>. A Null bound makes the whole test Null, which a seek has no way to say, so it is left
    /// to the filter; so is text on a column whose collation has no index keys to order it by.</remarks>
    private static (Expression Low, Expression High)? Between(
        Expression conjunct, string colName, string alias, TableDefinition def, IEqualityComparer<string> names)
    {
        if (conjunct is not BetweenExpression { Negated: false } between
            || Column(between.Value, alias, def, names) is not { } column
            || !def.Names.Equals(column.Column, colName)
            || HasColumnRef(between.Low) || HasColumnRef(between.High)
            || between.Low is LiteralExpression { Value: null } || between.High is LiteralExpression { Value: null }
            || def.FindColumn(colName) is not { } indexed
            || Classify(indexed.Type) == TypeKind.Text && !indexed.Collation.IsIndexKeyEncodable)
            return null;
        return (between.Low, between.High);
    }

    private static BinaryOperator Flip(BinaryOperator op) => op switch
    {
        BinaryOperator.GreaterThan => BinaryOperator.LessThan,
        BinaryOperator.GreaterThanOrEqual => BinaryOperator.LessThanOrEqual,
        BinaryOperator.LessThan => BinaryOperator.GreaterThan,
        BinaryOperator.LessThanOrEqual => BinaryOperator.GreaterThanOrEqual,
        _ => op,
    };

    /// <summary>
    /// Turns a nested-loop join whose inner (right) side is a base-table scan into an <b>index-nested-loop</b>
    /// join: if the join's ON has an equality <c>right.col = leftExpr</c> where <c>right.col</c> is the sole
    /// column of an index of the inner table and <c>leftExpr</c> references only the outer (left) side, the
    /// inner scan becomes an <see cref="IndexSeekNode"/> keyed off the outer row. Executed per left row, it
    /// seeks the index instead of scanning the whole inner table. The join keeps its ON as the residual check.
    /// </summary>
    // Not a HashJoinNode as CA1859 reads it: a join that cannot be hashed is returned as the join it was.
#pragma warning disable CA1859
    private static PlanNode RewriteJoin(JoinNode j, JetDatabase database, HashSet<string> outer, HashSet<string> above)
    {
        IEqualityComparer<string> names = database.Catalog.Names;
        var pairs = Conjuncts(j.On).Select(c => EqualColumn(c, j.Left, j.Right, database)).Where(p => p.HasValue).Select(p => p!.Value).ToList();
        // What reads a column of either side besides the join's own equalities: what is above the join, a sort the
        // planner put on a side (an ORDER BY of its columns), and the rest of the ON.
        var read = Conjuncts(j.On).Where(c => !pairs.Any(p => ReferenceEquals(p.Eq, c))).ToList();
        Unsorted(j.Left, read);
        Unsorted(j.Right, read);
        HashSet<string> reads = Read(above, read, j, database);
        // Which side ACE looks rows up in, decided before either side's filter is rewritten.
        bool? innerLeft = j.Kind is JoinKind.Inner or JoinKind.Cross or JoinKind.Left or JoinKind.Right
            ? InnerIsLeft(pairs, j.Kind, j.Left, j.Right, database, outer, reads)
            : null;
        // A join below this one is read for what this one reads, its whole ON included.
        HashSet<string> below = Read(above, Conjuncts(j.On), j, database);
        // An outer join's sides are read as they were: nothing of its WHERE is placed on its optional side.
        bool inner = j.Kind is JoinKind.Inner or JoinKind.Cross;
        PlanNode left = Apply(j.Left, database, outer, inner && innerLeft == true ? Reading.JoinInner : Reading.Alone, below);
        if (j.On is { } joinOn && innerLeft is { } looked)
            j = j with { On = InInnerType(joinOn, pairs, looked) };

        // A LEFT join preserves the probing left row: ExecuteJoin's seek loop emits its null-padded result
        // when no candidate passes ON. RIGHT/FULL must also preserve unmatched inner rows, so still hash/scan.
        // Keep hashing an unconstrained LEFT outer scan; a point-constrained outer input avoids building the
        // entire inner table for just its few matching keys.
        bool seekJoin = j.Kind is JoinKind.Inner or JoinKind.Cross
            || j.Kind == JoinKind.Left && left is FilterNode { Input: IndexSeekNode };
        if (seekJoin && j.On is { } on
            && j.Right is ScanNode scan && ExternalDatabases.Open(database, scan.Database).Catalog.FindTable(scan.Table) is { } def
            // The seek key may reference the join's own left side or the enclosing (outer) query. A Single beside a
            // Double left as written compares as Singles (InInnerType), which a seek of the Double's index cannot do:
            // its keys are Doubles, so that conjunct seeks nothing.
            && Lookup(on, scan, def,
                QueryPlanner.SubtreeAliases(left, names).Union(outer, names).ToHashSet(names),
                key => ResolveColumn(key, left, database)?.Column.Type, names) is { } seek)
            return j with { Left = left, Right = seek }; // ON kept as residual

        // A lateral (APPLY) right side is re-executed per left row with the left row in scope, so its own
        // index selection may treat the left side's columns as seekable constants — exactly as it does for a
        // correlated subquery. Without this the right side rescans a base table for every left row, which is
        // the one shape where that cost is paid over and over.
        HashSet<string> rightOuter = j.Kind is JoinKind.CrossApply or JoinKind.OuterApply
            ? QueryPlanner.SubtreeAliases(left, names).Union(outer, names).ToHashSet(names)
            : outer;
        PlanNode right = Apply(j.Right, database, rightOuter, inner && innerLeft == false ? Reading.JoinInner : Reading.Alone, below);

        // No index-nested-loop available: if this is an equi-join whose keys are same-kind columns, hash it
        // (O(n+m)) instead of leaving the O(n·m) nested loop. See HashJoinNode for why same-kind is required.
        // RIGHT joins hash too (the executor builds the left and probes with the right), and so does FULL, which
        // additionally emits the build rows the probe never matched.
        if (j.Kind is (JoinKind.Inner or JoinKind.Left or JoinKind.Right or JoinKind.Full) && j.On is { } cond
            && TryHashKeys(cond, left, right, database) is { } keys)
            return new HashJoinNode(left, right, j.Kind, keys.Left, keys.Right, cond) { KeyTypes = keys.Types };

        return j with { Left = left, Right = right };
    }
#pragma warning restore CA1859

    /// <summary>
    /// How a table read once for each row of the tables <paramref name="keySources"/> names — the join's left side
    /// and the enclosing query for a SELECT, every table read before it for an UPDATE or DELETE — is looked up by its
    /// join's <paramref name="on"/>: the single-column index of a column an equality of the ON holds to a value of
    /// those rows (<see cref="Restriction"/>), and that value as the key, as a seek of <paramref name="scan"/>'s table
    /// read as <paramref name="scan"/> reads it; null when no such index. The join keeps its ON as the test of each row
    /// the seek finds. <paramref name="keyType"/> gives the type a key column compares in, where known: a Single key
    /// beside a Double column seeks nothing. <paramref name="names"/> tells the scan's alias apart from others.
    /// </summary>
    internal static IndexSeekNode? Lookup(
        Expression on, ScanNode scan, TableDefinition def, HashSet<string> keySources, Func<ColumnReference, JetDataType?> keyType,
        IEqualityComparer<string> names)
    {
        foreach (Expression conjunct in Conjuncts(on))
        {
            if (Restriction(conjunct, scan.Alias ?? scan.Table, def, keySources, names) is not { Equal: true, Values: [var key] } match
                || match.Column.Type == JetDataType.Double && key is ColumnReference keyColumn && keyType(keyColumn) == JetDataType.Single)
                continue;
            if (def.Indexes.FirstOrDefault(i => i.RootPage > 0 && i.Columns.Count == 1 && Position(i, match.Column) == 0) is { } index)
                return new IndexSeekNode(scan.Table, scan.Alias, index, [key], scan.Database) { Decode = scan.Decode, RowIds = scan.RowIds };
        }
        return null;
    }

    /// <summary>
    /// <paramref name="on"/> with each <c>Single = Double</c> between the join's two sides compared as ACE compares it:
    /// in the type of the join's inner table, the one ACE looks rows up in. Where that is the Double, the Single is
    /// widened; where it is the Single, the conjunct compares as Singles as written. Likewise a number or date beside
    /// a Yes/No column: where the inner table is the Yes/No's, the number is compared by its truth (verified vs
    /// ACE: 1 and 2 join True); where it is the number's, the Yes/No is -1 or 0 as written.
    /// </summary>
    /// <remarks>Which side is the inner table is <see cref="InnerIsLeft"/>'s (<paramref name="innerLeft"/>), and
    /// <paramref name="pairs"/> are the ON's equalities of a column of each side (<see cref="EqualColumn"/>). A
    /// comparison ACE makes row by row narrows to single precision, as LibRed's does, and a seek of a real index
    /// converts to that index's type, as LibRed's does.</remarks>
    private static Expression InInnerType(Expression on, List<ColumnPair> pairs, bool innerLeft)
    {
        bool widened = false;
        List<Expression> conjuncts = [.. Conjuncts(on).Select(InnerType)];
        return widened ? QueryPlanner.CombineAnd(conjuncts) : on;

        Expression InnerType(Expression conjunct)
        {
            int at = pairs.FindIndex(p => ReferenceEquals(p.Eq, conjunct));
            if (at < 0)
                return conjunct;
            var (eq, l, r, lc, rc) = pairs[at];

            bool singleLeft = lc.Column.Type == JetDataType.Single;
            bool singleDouble = singleLeft ? rc.Column.Type == JetDataType.Double
                : lc.Column.Type == JetDataType.Double && rc.Column.Type == JetDataType.Single;
            // A number or date beside a Yes/No column: looked up in the Yes/No column it is compared by its truth,
            // so 1 and 2 join True as -1 does; looked up the other way the Yes/No is the number -1 or 0. Which values
            // are compared by truth is ExpressionEvaluator.ComparedByTruth's (a Decimal column is not). A column of no
            // comparable kind (a complex column's id) is not.
            bool yesNoLeft = lc.Column.Type == JetDataType.Boolean;
            JetDataType beside = (yesNoLeft ? rc : lc).Column.Type;
            bool yesNo = yesNoLeft != (rc.Column.Type == JetDataType.Boolean) && Classify(beside) is not null
                && ExpressionEvaluator.ComparedByTruth(JetClrTypeMap.ToClrType(beside), beside == JetDataType.Currency);
            if (!singleDouble && !yesNo)
                return conjunct;
            if (yesNo)
            {
                if (innerLeft != yesNoLeft)
                    return conjunct;
                widened = true;
                ColumnReference number = yesNoLeft ? r : l;
                Expression truth = new FunctionCall("CBool", [number]);
                return ReferenceEquals(eq.Left, number) ? eq with { Left = truth } : eq with { Right = truth };
            }
            if (innerLeft == singleLeft)
                return conjunct;

            widened = true;
            ColumnReference single = singleLeft ? l : r;
            Expression wide = new FunctionCall("CDbl", [single]);
            return ReferenceEquals(eq.Left, single) ? eq with { Left = wide } : eq with { Right = wide };
        }
    }

    /// <summary><paramref name="conjunct"/> where it is an equality of a column of <paramref name="left"/> with a column
    /// of <paramref name="right"/>, each resolved to its base table; else null.</summary>
    private static ColumnPair? EqualColumn(Expression conjunct, PlanNode left, PlanNode right, JetDatabase database)
    {
        if (conjunct is not BinaryExpression { Operator: BinaryOperator.Equal, Left: ColumnReference a, Right: ColumnReference b } eq)
            return null;
        var (l, r) = ResolveColumn(a, left, database) is not null ? (a, b) : (b, a);
        if (ResolveColumn(l, left, database) is not { } lc || ResolveColumn(r, right, database) is not { } rc
            || ResolveColumn(l, right, database) is not null || ResolveColumn(r, left, database) is not null)
            return null;
        return new ColumnPair(eq, l, r, lc, rc);
    }

    /// <summary>An equality <paramref name="Eq"/> of a column <paramref name="L"/> of a join's left side with a column
    /// <paramref name="R"/> of its right side, each resolved to its base table (<see cref="EqualColumn"/>).</summary>
    private readonly record struct ColumnPair(BinaryExpression Eq, ColumnReference L, ColumnReference R, ResolvedColumn Lc, ResolvedColumn Rc);

    // R, the one side with a restriction an index answers, drives only when the other side O has more rows than these,
    // e_R being the rows R's restriction is expected to leave: A + B·e_R where a column of R and none of O is read above
    // the join, twice that where a column of O is read, Alpha·e_R where no column of either is (COUNT(*)).
    private const double DriveRowsA = 42.5;
    private const double DriveRowsB = 0.70;
    private const double DriveRowsAlpha = 0.19;

    /// <summary>
    /// Whether ACE looks rows up in the left side of a join whose ON holds <paramref name="pairs"/> equal; null when it
    /// holds no two base columns equal. Measured against ACE: an outer join's inner table is its optional
    /// side. Of an inner or comma join, a side is lookup-able when an index of it leads with a column of a pair; where
    /// one side is, it is the inner. Where both are and exactly one, R, has a restriction an index answers, R is the
    /// inner unless the other side O has more rows than <see cref="DriveRowsA"/> + <see cref="DriveRowsB"/>·e_R — twice
    /// that where a column of O is read (in <paramref name="reads"/>, the aliases read besides the join's equalities, or
    /// by a condition on O), <see cref="DriveRowsAlpha"/>·e_R where nothing is; where neither is lookup-able, R drives.
    /// Where both are restricted, one by an equality on its primary key and the other by an equality on a Yes/No column,
    /// with the key's table read above the join, the key's table drives. Otherwise the side with more rows is the inner,
    /// on a tie the first.
    /// </summary>
    private static bool? InnerIsLeft(
        List<ColumnPair> pairs, JoinKind kind, PlanNode left, PlanNode right, JetDatabase database, HashSet<string> outer, HashSet<string> reads)
    {
        if (pairs.Count == 0)
            return null;
        if (kind == JoinKind.Left)
            return false;
        if (kind == JoinKind.Right)
            return true;
        bool leftLooked = pairs.Any(p => LeadsIndex(p.Lc.Table, p.Lc.Column));
        bool rightLooked = pairs.Any(p => LeadsIndex(p.Rc.Table, p.Rc.Column));
        if (leftLooked != rightLooked)
            return leftLooked;
        var leftRestricted = Restricted(left, database, outer);
        var rightRestricted = Restricted(right, database, outer);
        var (lc, rc) = (pairs[0].Lc, pairs[0].Rc);
        if (leftRestricted.HasValue != rightRestricted.HasValue)
        {
            bool restrictedLeft = leftRestricted.HasValue;
            if (!leftLooked)
                return !restrictedLeft;
            double e = (leftRestricted ?? rightRestricted)!.Value.E;
            var o = restrictedLeft ? rc : lc;
            bool oRead = o.Alias is { } oAlias && reads.Contains(oAlias) || Unsorted(restrictedLeft ? right : left) is FilterNode;
            double rows = Rows(o.Database, o.Table);
            bool drives = oRead ? rows > 2 * (DriveRowsA + DriveRowsB * e)
                : reads.Count == 0 ? rows > DriveRowsAlpha * e
                : rows > DriveRowsA + DriveRowsB * e;
            return drives != restrictedLeft;
        }
        if (leftLooked && leftRestricted is { } l && rightRestricted is { } r && l.KeyEqual != r.KeyEqual
            && (l.KeyEqual ? r : l).YesNoEqual && (l.KeyEqual ? lc : rc).Alias is { } keyAlias && reads.Contains(keyAlias))
            return !l.KeyEqual;
        int leftRows = Rows(lc.Database, lc.Table), rightRows = Rows(rc.Database, rc.Table);
        return leftRows != rightRows ? leftRows > rightRows : true;
    }

    /// <summary>Of <paramref name="side"/>, a filtered base table, the restrictions an index answers
    /// (<see cref="FromIndex"/>): the rows they are expected to leave — 1 for an equality on a unique index, half the
    /// table for an equality on a Yes/No column's index (ACE counts 2 values), 0 where unmeasured — whether one is an
    /// equality on the single-column primary key, and whether one is an equality on a Yes/No column; null when it has
    /// none.</summary>
    private static (double E, bool KeyEqual, bool YesNoEqual)? Restricted(PlanNode side, JetDatabase database, HashSet<string> outer)
    {
        if (Unsorted(side) is not FilterNode { Input: ScanNode scan } filter)
            return null;
        JetDatabase source = ExternalDatabases.Open(database, scan.Database);
        if (source.Catalog.FindTable(scan.Table) is not { } def)
            return null;
        string alias = scan.Alias ?? scan.Table;
        var restrictions = Conjuncts(filter.Predicate).Select(c => Restriction(c, alias, def, outer, database.Catalog.Names)).ToList();
        (double E, bool KeyEqual, bool YesNoEqual)? found = null;
        for (int i = 0; i < restrictions.Count; i++)
        {
            if (restrictions[i] is not { Joined: false } r || !FromIndex(def, restrictions, i, lookup: false))
                continue;
            var alone = def.Indexes.Where(x => x.Columns.Count == 1 && Position(x, r.Column) == 0).ToList();
            double e = !r.Equal ? 0
                : alone.Any(x => x.IsUnique) ? 1
                : r.Column.Type == JetDataType.Boolean ? Rows(source, def) / 2.0
                : 0;
            bool key = r.Equal && alone.Any(x => x.IsPrimaryKey);
            bool yesNo = r.Equal && r.Column.Type == JetDataType.Boolean;
            found = found is { } f ? (Math.Min(f.E, e), f.KeyEqual || key, f.YesNoEqual || yesNo) : (e, key, yesNo);
        }
        return found;
    }

    /// <summary><paramref name="side"/> below the sorts the planner put on it for an ORDER BY of its columns, which leave
    /// its rows what they are; their keys are added to <paramref name="keys"/>, where given.</summary>
    private static PlanNode Unsorted(PlanNode side, List<Expression>? keys = null)
    {
        while (side is SortNode { Limit: null } sorted)
        {
            keys?.AddRange(sorted.Keys.Select(k => k.Value));
            side = sorted.Input;
        }
        return side;
    }

    private static int Rows(JetDatabase database, TableDefinition table) => database.OpenTable(table.Name).RowCount;

    /// <summary>Whether an index of <paramref name="table"/> has <paramref name="column"/> first, so ACE can seek it for
    /// that column.</summary>
    internal static bool LeadsIndex(TableDefinition table, ColumnDef column) => table.Indexes.Any(i => Position(i, column) == 0);

    /// <summary>Where <paramref name="column"/> comes among the columns of <paramref name="index"/>, or -1.</summary>
    private static int Position(IndexDef index, ColumnDef column)
    {
        for (int k = 0; k < index.Columns.Count; k++)
            if (index.Columns[k].Column.ColumnId == column.ColumnId)
                return k;
        return -1;
    }

    /// <summary>Splits the join condition into equi-key column pairs (left-referencing = right-referencing)
    /// whose columns are of the same type kind, or null if there is no such pair. The full ON is still applied
    /// as a residual, so a partial split (extra non-equi conjuncts) is fine.</summary>
    private static (IReadOnlyList<Expression> Left, IReadOnlyList<Expression> Right, IReadOnlyList<Type?>? Types)? TryHashKeys(
        Expression on, PlanNode left, PlanNode right, JetDatabase database)
    {
        HashSet<string> leftAliases = QueryPlanner.SubtreeAliases(left, database.Catalog.Names);
        HashSet<string> rightAliases = QueryPlanner.SubtreeAliases(right, database.Catalog.Names);
        var leftKeys = new List<Expression>();
        var rightKeys = new List<Expression>();
        var types = new List<Type?>();

        foreach (Expression conjunct in Conjuncts(on))
        {
            if (conjunct is not BinaryExpression { Operator: BinaryOperator.Equal } eq)
                continue;

            // Orient the equality so L is the left-subtree side and R the right-subtree side.
            (Expression L, Expression R)? oriented =
                ReferencesOnly(eq.Left, leftAliases) && ReferencesOnly(eq.Right, rightAliases) ? (eq.Left, eq.Right)
                : ReferencesOnly(eq.Right, leftAliases) && ReferencesOnly(eq.Left, rightAliases) ? (eq.Right, eq.Left)
                : null;
            // A key is a column, or a Single widened to a Double (InInnerType).
            if (oriented is not { } o || KeyColumn(o.L) is not { } lc || KeyColumn(o.R) is not { } rc)
                continue;

            // A hash consistent with the evaluator's equality only exists within one type kind (5 = '5' and
            // 5 = 5.0 but '5' ≠ '5.0'), so both key columns must resolve to the same kind.
            var lt = ResolveColumn(lc, left, database);
            var rt = ResolveColumn(rc, right, database);
            if (lt is null || rt is null || Classify(lt.Value.Column.Type) is not { } kind || Classify(rt.Value.Column.Type) != kind)
                continue;

            leftKeys.Add(o.L);
            rightKeys.Add(o.R);
            // A Single beside another number left as written compares as Singles, so both are hashed as Singles.
            types.Add(o.L is ColumnReference && o.R is ColumnReference
                && lt.Value.Column.Type == JetDataType.Single != (rt.Value.Column.Type == JetDataType.Single)
                ? typeof(float) : null);
        }

        return leftKeys.Count > 0 ? (leftKeys, rightKeys, types.Any(t => t is not null) ? types : null) : null;

        static ColumnReference? KeyColumn(Expression key) => key switch
        {
            ColumnReference c => c,
            FunctionCall { Name: "CDbl", Arguments: [ColumnReference c] } => c,
            _ => null,
        };
    }

    internal enum TypeKind { Numeric, Text, Binary, Temporal, Guid }

    /// <summary>The kind a value compares as — the value-side partner of <see cref="Classify(JetDataType)"/>.
    /// Null for <c>Null</c>, which has no kind of its own.</summary>
    internal static TypeKind? KindOf(object? value) => value switch
    {
        null => null,
        string or char => TypeKind.Text,
        DateTime => TypeKind.Temporal,
        Guid => TypeKind.Guid,
        byte[] => TypeKind.Binary,
        _ when ExpressionEvaluator.IsNumeric(value) => TypeKind.Numeric,
        _ => null,
    };

    internal static TypeKind? Classify(JetDataType t) => t switch
    {
        JetDataType.Boolean or JetDataType.Byte or JetDataType.Int16 or JetDataType.Int32 or JetDataType.Int64
            or JetDataType.Single or JetDataType.Double or JetDataType.Currency or JetDataType.FixedPoint => TypeKind.Numeric,
        JetDataType.Text or JetDataType.Memo => TypeKind.Text,
        JetDataType.Binary or JetDataType.BigBinary or JetDataType.Ole => TypeKind.Binary,
        JetDataType.DateTime or JetDataType.DateTimeExtended => TypeKind.Temporal,
        JetDataType.Guid => TypeKind.Guid,
        _ => null,
    };

    /// <summary>The base-table column <paramref name="col"/> is in <paramref name="subtree"/> — directly, or a
    /// derived-table column followed through its projection — with its table, that table's database and the alias of
    /// the source it binds to (a derived table's own alias for a derived-table column); null if it can't be pinned
    /// down (e.g. the projection is a computed expression, or an unqualified name more than one source has), in which
    /// case a join on it is left as a nested loop rather than risk an unsound hash.</summary>
    internal static ResolvedColumn? ResolveColumn(ColumnReference col, PlanNode subtree, JetDatabase database)
    {
        ResolvedColumn? found = null;
        int sources = 0;
        IEqualityComparer<string> names = database.Catalog.Names;
        foreach ((string alias, string table, ExternalDatabase? external) in BaseTables(subtree))
        {
            if (col.Table is { } t && !names.Equals(t, alias))
                continue;
            JetDatabase source = ExternalDatabases.Open(database, external);
            if (source.Catalog.FindTable(table) is { } def && def.FindColumn(col.Column) is { } c)
            {
                if (col.Table is not null)
                    return new ResolvedColumn(source, def, c, alias);
                if (sources++ == 0)
                    found = new ResolvedColumn(source, def, c, alias);
            }
        }

        // A derived-table column: match its alias, find what its SELECT list projects for this name, and resolve
        // that expression against the derived query's own tables (only if it is itself a plain column).
        foreach (DerivedTableNode d in DerivedTables(subtree))
        {
            if (col.Table is { } t && !names.Equals(t, d.Alias))
                continue;
            if (DerivedColumn(d, col.Column, database) is { } inner)
            {
                ResolvedColumn? resolved = ResolveColumn(inner, d.Input, database) is { } r ? r with { Alias = d.Alias } : null;
                if (col.Table is not null)
                    return resolved;
                if (sources++ == 0)
                    found = resolved;
            }
        }
        return sources == 1 ? found : null;
    }

    /// <summary>A base-table <paramref name="Column"/> a column reference resolves to (<see cref="ResolveColumn"/>): its
    /// <paramref name="Table"/>, that table's <paramref name="Database"/>, and the <paramref name="Alias"/> of the source
    /// it binds to.</summary>
    internal readonly record struct ResolvedColumn(JetDatabase Database, TableDefinition Table, ColumnDef Column, string? Alias);

    // A derived table is an opaque boundary: its inner tables/aliases are not visible outside it, so BaseTables
    // stops there (DerivedTables handles the derived level separately).
    private static IEnumerable<(string Alias, string Table, ExternalDatabase? Database)> BaseTables(PlanNode node) => node switch
    {
        ScanNode s => [(s.Alias ?? s.Table, s.Table, s.Database)],
        IndexSeekNode s => [(s.Alias ?? s.Table, s.Table, s.Database)],
        IndexRangeSeekNode s => [(s.Alias ?? s.Table, s.Table, s.Database)],
        DerivedTableNode => [],
        _ => node.Children.SelectMany(BaseTables),
    };

    /// <summary>The derived tables directly visible in <paramref name="node"/> (not descending into a derived
    /// table's own body, which is its private scope).</summary>
    private static IEnumerable<DerivedTableNode> DerivedTables(PlanNode node) => node switch
    {
        DerivedTableNode d => [d],
        _ => node.Children.SelectMany(DerivedTables),
    };

    /// <summary>Whether every column an expression references is one of the given aliases (and it has no
    /// subquery) — i.e. it can be evaluated from the outer row alone, as an index-seek key.</summary>
    internal static bool ReferencesOnly(Expression e, HashSet<string> aliases) => e switch
    {
        ColumnReference { Table: { } t } => aliases.Contains(t),
        ColumnReference => false, // unqualified — can't attribute it to the outer side safely
        LiteralExpression or ParameterExpression or SystemVariableExpression => true,
        _ => e.Operands()?.All(a => ReferencesOnly(a, aliases)) ?? false,
    };

    /// <summary>The column reference if <paramref name="e"/> is a column of the given scan (its qualifier is
    /// the scan's alias, or it is unqualified and the table has such a column), else null.</summary>
    private static ColumnReference? Column(Expression e, string alias, TableDefinition def, IEqualityComparer<string> names)
    {
        if (e is not ColumnReference c) return null;
        if (c.Table is { } t && !names.Equals(t, alias)) return null;
        return def.FindColumn(c.Column) is not null ? c : null;
    }

    private static bool HasColumnRef(Expression e) => e switch
    {
        ColumnReference => true,
        // A subquery is opaque: it may be CORRELATED to this scan's own row (e.g. `t.Id > (SELECT … WHERE
        // t.Id = …)`), so it must never be used as a seek key/bound — treat it as referencing a column. (A
        // seek bound is evaluated once, with no row scope; a correlated subquery would fail to resolve there.)
        ScalarSubquery or ExistsExpression or InSubqueryExpression => true,
        // Same reasoning, and the same reason to be explicit: this switch's default means "no column reference"
        // — i.e. usable as a seek bound — so an unlisted node fails towards a WRONG seek, not a missed one.
        WindowFunction => true,
        _ => e.Operands()?.Any(HasColumnRef) ?? false, // no operands: literals, parameters, system vars
    };

    /// <summary>The AND-conjuncts of a predicate; none at all for an absent one (a missing WHERE or HAVING).</summary>
    internal static IEnumerable<Expression> Conjuncts(Expression? e)
    {
        if (e is null)
            return [];
        if (e is BinaryExpression { Operator: BinaryOperator.And } b)
            return Conjuncts(b.Left).Concat(Conjuncts(b.Right));
        return [e];
    }
}
