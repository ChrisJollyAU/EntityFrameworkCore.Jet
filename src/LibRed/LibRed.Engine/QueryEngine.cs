using LibRed.Catalog;
using LibRed.Engine.Execution;
using LibRed.Engine.Plan;
using LibRed.Engine.Planning;
using LibRed.Engine.Schema;
using LibRed.IO;
using LibRed.Sql.Ast;
using LibRed.Sql.Binding;
using LibRed.Sql.Parsing;

namespace LibRed.Engine;

/// <summary>
/// The engine facade: wires the full pipeline parse → bind → plan → execute over an
/// open <see cref="JetDatabase"/>. This is what the ADO provider sits on top of.
/// </summary>
public sealed class QueryEngine
{
    private readonly JetDatabase _database;
    private readonly ISqlParser _parser;
    private readonly Binder _binder;
    private readonly SessionState _session = new();

    public QueryEngine(JetDatabase database, ISqlParser? parser = null)
    {
        _database = database;
        _parser = parser ?? new AntlrSqlParser();
        _binder = new Binder(new CatalogSchemaProvider(database.Catalog));
    }

    public JetDatabase Database => _database;

    /// <summary>Connection-scoped <c>@@ROWCOUNT</c>/<c>@@IDENTITY</c> state, shared across the statements
    /// of a batch (which the ADO command layer splits and runs through this one engine).</summary>
    public SessionState Session => _session;

    public ResultSet ExecuteQuery(string sql, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        // Text with no statement in it (blank, or only comments) has nothing to run and nothing to return.
        // It takes no page scope at all — there is no work to isolate.
        if (_parser.IsStatementless(sql)) return ResultSet.Empty;

        // Parse outside the gate — it touches no pages, and holding a file-wide scope across it would
        // serialize parsing for no isolation benefit. The parsed shape then picks the scope.
        SqlStatement parsed = _parser.ParseStatement(sql);
        return Scoped(CannotWrite(parsed), () => HeldWhileRead(ExecuteQueryCore(parsed, parameters)));
    }

    /// <summary><paramref name="result"/> with the table locks its statement took handed to its rows, which are read
    /// lazily, after the statement returns: held until the enumeration reading them is disposed — an ADO reader's close
    /// — whether or not every row has been read, as ACE's open reader holds the table's read byte until it is closed
    /// (measured, <see cref="PageChannel.KeepStatementTableLocks"/>). Called last in the statement's scope.</summary>
    private ResultSet HeldWhileRead(ResultSet result)
    {
        if (ReferenceEquals(result, ResultSet.Empty) || _database.Channel.KeepStatementTableLocks() is not { } held)
            return result;
        return new ResultSet(result.ColumnNames, new HeldRows(result.Rows, held), result.ColumnTypes, () => result.Columns);
    }

    /// <summary>A result's rows and the table locks they hold (<see cref="HeldWhileRead"/>): the first enumeration
    /// begun keeps them until it is disposed; one after that reads as the Core API does, under locks of its own.</summary>
    private sealed class HeldRows(IEnumerable<object?[]> rows, HeldTableLocks held) : IEnumerable<object?[]>
    {
        public IEnumerator<object?[]> GetEnumerator()
        {
            held.Start();
            return new Enumerator(rows.GetEnumerator(), held);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator(IEnumerator<object?[]> rows, HeldTableLocks held) : IEnumerator<object?[]>
        {
            public object?[] Current => rows.Current;
            object System.Collections.IEnumerator.Current => rows.Current;
            public bool MoveNext() => rows.MoveNext();
            public void Reset() => rows.Reset();

            public void Dispose()
            {
                try { rows.Dispose(); }
                finally { held.Dispose(); }
            }
        }
    }

    /// <summary>Runs <paramref name="action"/> in the page scope its statement needs: shared (concurrent with other
    /// readers) when it <paramref name="reads"/> only — a statement that cannot write (<see cref="CannotWrite"/>) —
    /// exclusive otherwise so its pages publish as one unit. Anything not provably read-only takes the exclusive scope
    /// — a shared scope that then writes is rejected, not silently upgraded.</summary>
    /// <remarks>A statement that took a lock only after another user committed (<see cref="StaleReadException"/>) —
    /// a page's write lock, or a table's lock — read or planned against what may be older than the file: a page the
    /// other user changed, a table whose design it changed. A writing statement has been rolled back as though it had
    /// never run (<see cref="RunAtomically"/>), and the statement is run again in a new scope, which looks at the other
    /// users' slots, re-reads the catalog, and binds and plans afresh — as ACE in its place waits for the lock and goes
    /// ahead on top of the other user's change. After <see cref="StaleReadAttempts"/> runs the exception stands, with
    /// ACE's message for what could not be locked.</remarks>
    private T Scoped<T>(bool reads, Func<T> action)
    {
        for (int attempt = 1; ; attempt++)
        {
            try { return reads ? _database.ReadConsistent(action) : _database.WriteExclusive(action); }
            catch (StaleReadException) when (attempt < StaleReadAttempts) { }
        }
    }

    /// <summary>Whether <paramref name="statement"/> is one that cannot write. A SELECT with INTO is a make-table
    /// query: it looks like a read and writes a table, so it is not one.</summary>
    private static bool CannotWrite(SqlStatement statement) =>
        statement is SelectStatement { Into: null } or SetOperationStatement or SystemVariableSelectStatement or TransformStatement;

    private ResultSet ExecuteQueryCore(SqlStatement parsed, IReadOnlyDictionary<string, object?>? parameters)
    {
        if (parsed is ExecuteStatement exec) return ExecuteProcedure(exec, parameters).Rows;
        // A make-table query is an action query however it was invoked: run it and return nothing, rather
        // than handing the caller the rows it just wrote into a table.
        SqlStatement ast = ViewExpander.Expand(parsed, _database, _parser);
        if (parsed is SelectStatement { Into: not null }) return Route(_binder.Bind(ast).Statement, parameters).Rows;
        BoundStatement bound = _binder.Bind(ast);
        if (bound.Statement is TransactionControlStatement txnControl)
        {
            RunTransactionControl(txnControl.Kind);
            return ResultSet.Empty;
        }
        var executor = new QueryExecutor(_database, parameters, _session);
        return bound.Statement is SystemVariableSelectStatement sysSelect
            ? executor.ExecuteSystemVariableSelect(sysSelect)
            : executor.ExecuteQuery(PlanWithIndexes(bound));
    }

    /// <summary>
    /// The shape <paramref name="sql"/> would return, without running it — what ADO's
    /// <c>CommandBehavior.SchemaOnly</c> asks for. A query is parsed, bound and planned, and its columns come
    /// back with no rows: planning is what knows the shape, and rows are lazy, so nothing is ever read. A
    /// statement that writes is <em>not</em> executed and describes as nothing (measured: ACE leaves an
    /// INSERT's table untouched under SchemaOnly), and a stored procedure describes as the query it holds
    /// rather than running it.
    /// </summary>
    public ResultSet Describe(string sql, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        if (_parser.IsStatementless(sql)) return ResultSet.Empty;

        SqlStatement parsed = _parser.ParseStatement(sql);
        // The shared scope always: describing reads the catalog and writes nothing, whatever the statement
        // would have done had it run.
        return Scoped(reads: true, () => DescribeCore(parsed, parameters));
    }

    private ResultSet DescribeCore(SqlStatement parsed, IReadOnlyDictionary<string, object?>? parameters)
    {
        if (parsed is ExecuteStatement exec)
        {
            // A stored SELECT describes as its own (parameterized) text; a stored action query would have
            // written, so it describes as nothing. Its arguments are never evaluated — values cannot change
            // a shape.
            StoredQuery query = Procedure(exec.Procedure);
            if (query.IsAction) return ResultSet.Empty;
            return DescribeCore(_parser.ParseStatement(query.RequireSql()), parameters);
        }

        // Everything that writes — DML, DDL, a make-table SELECT, transaction control — returns no rows and
        // does not run.
        if (!CannotWrite(parsed))
            return ResultSet.Empty;

        BoundStatement bound = _binder.Bind(ViewExpander.Expand(parsed, _database, _parser));
        var executor = new QueryExecutor(_database, parameters, _session, describing: true);
        ResultSet shape = bound.Statement is SystemVariableSelectStatement sysSelect
            ? executor.ExecuteSystemVariableSelect(sysSelect)
            : executor.ExecuteQuery(PlanWithIndexes(bound));

        // Its rows are lazy and so far untouched; dropping them is what guarantees they stay that way.
        return new ResultSet(shape.ColumnNames, [], shape.ColumnTypes, () => shape.Columns);
    }

    public int ExecuteNonQuery(string sql, IReadOnlyDictionary<string, object?>? parameters = null)
        => Run(sql, parameters, rowsRead: false).RecordsAffected;

    /// <summary>
    /// True when <paramref name="sql"/> holds no statement — blank, or nothing but comments. The ADO batch
    /// splitter uses this to drop such a fragment instead of running it, so a trailing <c>-- comment</c> in a
    /// batch cannot become the batch's "last statement" and mask the real one's rows-affected.
    /// </summary>
    public bool IsStatementless(string sql) => _parser.IsStatementless(sql);

    /// <summary>Executes a stored action query (a CREATE PROCEDURE body that is not a SELECT) by name — the
    /// read-back counterpart of <see cref="JetDatabase.CreateActionQuery"/>. The query is reconstructed from
    /// its catalog rows and run; a kind LibRed cannot execute (e.g. INSERT … SELECT) throws
    /// <see cref="NotSupportedException"/>, and an unknown name throws <see cref="InvalidOperationException"/>.</summary>
    public int ExecuteStoredActionQuery(string name)
    {
        StoredQuery query = Procedure(name);
        if (!query.IsAction)
            throw new InvalidOperationException($"No stored action query named '{name}'.");
        return ExecuteNonQuery(query.RequireSql());
    }

    /// <summary>
    /// Parses and binds once, then routes by statement kind: a query (<c>SELECT</c> / set operation)
    /// is planned and executed into a <see cref="ResultSet"/>; a DML/DDL statement (<c>INSERT</c>,
    /// <c>CREATE TABLE</c>) runs directly against storage and reports rows affected. This is the
    /// single entry point the ADO layer uses so that <c>ExecuteReader</c> works for *any* statement —
    /// EF Core executes inserts through the reader path to read results back.
    /// </summary>
    public CommandResult Execute(string sql, IReadOnlyDictionary<string, object?>? parameters = null)
        => Run(sql, parameters, rowsRead: true);

    /// <param name="sql">The statement.</param>
    /// <param name="parameters">Its parameter values.</param>
    /// <param name="rowsRead">Whether the caller reads the rows: they then hold the statement's table locks until read
    /// (<see cref="HeldWhileRead"/>); a caller that drops them lets them go with the statement.</param>
    private CommandResult Run(string sql, IReadOnlyDictionary<string, object?>? parameters, bool rowsRead)
    {
        // As in ExecuteQuery: nothing to parse, nothing to run. Reported as zero rows affected rather than
        // the -1 a query returns, since a comment is an action that did nothing, not a result set.
        if (_parser.IsStatementless(sql)) return new CommandResult(ResultSet.Empty, RecordsAffected: 0);

        SqlStatement parsed = _parser.ParseStatement(sql);
        return Scoped(CannotWrite(parsed), () =>
        {
            CommandResult result = ExecuteCore(parsed, parameters);
            return rowsRead ? result with { Rows = HeldWhileRead(result.Rows) } : result;
        });
    }

    private CommandResult ExecuteCore(SqlStatement parsed, IReadOnlyDictionary<string, object?>? parameters)
    {
        if (parsed is ExecuteStatement exec) return ExecuteProcedure(exec, parameters);
        SqlStatement ast = ViewExpander.Expand(parsed, _database, _parser);
        BoundStatement bound = _binder.Bind(ast);
        return Route(bound.Statement, parameters);
    }

    /// <summary>Drives the nested-transaction controller on the shared <see cref="JetDatabase"/> — the same L2
    /// the ADO front door uses, so the two can't open parallel transactions.</summary>
    private void RunTransactionControl(TransactionControlKind kind)
    {
        switch (kind)
        {
            case TransactionControlKind.Begin: _database.BeginNested(); break;
            case TransactionControlKind.Commit: _database.CommitNested(); break;
            case TransactionControlKind.Rollback: _database.RollbackNested(); break;
        }
    }

    /// <summary>Routes an already-bound statement to the right executor (system-variable select, IF…THEN,
    /// query, or a DML/DDL non-query). Split out so IF…THEN can run its guarded body through the same path.</summary>
    private CommandResult Route(SqlStatement statement, IReadOnlyDictionary<string, object?>? parameters)
    {
        switch (statement)
        {
            case SystemVariableSelectStatement sysSelect:
                return new CommandResult(
                    new QueryExecutor(_database, parameters, _session).ExecuteSystemVariableSelect(sysSelect), -1);
            case IfThenStatement ifThen:
                return ExecuteIfThen(ifThen, parameters);
            // A make-table query looks like a SELECT but IS an action query: it writes a table and returns no
            // rows, so it belongs below with the writing statements — inside the implicit transaction, so a
            // failure part-way cannot leave a half-populated table behind.
            case SelectStatement { Into: not null }:
                return ExecuteWritingStatement(statement, parameters);
            case SelectStatement or SetOperationStatement or TransformStatement:
                return new CommandResult(
                    new QueryExecutor(_database, parameters, _session).ExecuteQuery(PlanWithIndexes(new BoundStatement(statement))), -1);
            case TransactionControlStatement txnControl:
                // Transaction control manages the transaction itself — it must NOT go through the implicit
                // per-statement wrap below (which would open/commit a transaction around it).
                RunTransactionControl(txnControl.Kind);
                return new CommandResult(ResultSet.Empty, 0);
            default:
                return ExecuteWritingStatement(statement, parameters);
        }
    }

    /// <summary>
    /// Runs a DML/DDL statement so that it is atomic on its own: with no user transaction open it runs inside
    /// an implicit transaction (commit on success, roll back on any failure — a late constraint/I/O error can
    /// never leave a half-written row, index, LVAL chain, or catalog entry); inside a user transaction it runs
    /// under a savepoint, so a statement failure undoes just that statement and leaves the user's transaction
    /// intact. Only writing statements reach here — reads never open a transaction. A statement that took a lock only
    /// after another user committed is rolled back as though it had never run — nothing a failure keeps is kept — and
    /// <see cref="Scoped"/> runs it again.
    /// </summary>
    private CommandResult ExecuteWritingStatement(SqlStatement statement, IReadOnlyDictionary<string, object?>? parameters) =>
        new(ResultSet.Empty, RunAtomically(() => new StatementExecutor(_database, parameters, _parser, _session).Execute(statement)));

    /// <summary>How many times a statement is run when each run meets another user's commit (see
    /// <see cref="Scoped"/>). Chosen, not measured: ACE waits for a lock rather than re-running.</summary>
    private const int StaleReadAttempts = 3;

    /// <summary>One run of a writing statement, atomic on its own (see <see cref="ExecuteWritingStatement"/>).</summary>
    private int RunAtomically(Func<int> run)
    {
        if (_database.InTransaction)
        {
            Savepoint savepoint = _database.CreateSavepoint();
            try
            {
                int affected = run();
                _database.ReleaseSavepoint(savepoint);
                return affected;
            }
            catch (Exception failure)
            {
                // Rolling back to a savepoint leaves it open for reuse, so the statement's own frame has to be
                // closed too. Left open it sits above the savepoint the enclosing BEGIN pushed, and that level's
                // COMMIT then refuses — only the innermost savepoint can be released — so one failed statement
                // makes every later nested COMMIT throw.
                _database.RollbackToSavepoint(savepoint, failed: failure is not StaleReadException);
                _database.ReleaseSavepoint(savepoint);
                throw;
            }
        }

        _database.BeginTransaction();
        try
        {
            int affected = run();
            _database.Commit(flush: false); // autocommit: durability stays on Dispose, as before transactions existed
            return affected;
        }
        catch (Exception failure)
        {
            _database.Rollback(failed: failure is not StaleReadException);
            throw;
        }
    }

    /// <summary>Evaluates the condition subquery; runs the guarded THEN statement only when the EXISTS test holds
    /// (or, for NOT EXISTS, when it doesn't). @@ROWCOUNT reflects the THEN, or 0 when the guard skips it.</summary>
    private CommandResult ExecuteIfThen(IfThenStatement ifThen, IReadOnlyDictionary<string, object?>? parameters)
    {
        var condPlan = IndexSelection.Apply(QueryPlanner.PlanStatement(ifThen.Condition), _database);
        bool hasRows = new QueryExecutor(_database, parameters, _session).ExecuteQuery(condPlan).Rows.Any();
        if (hasRows == ifThen.Negated)
            return new CommandResult(ResultSet.Empty, 0); // guard not satisfied — skip the body
        return Route(ifThen.Then, parameters);
    }

    /// <summary>Plans a bound statement, then applies index selection (turning scans into index seeks where a
    /// predicate allows). Root-query column pruning runs at the executor's shared entry.</summary>
    private PlanNode PlanWithIndexes(BoundStatement bound) =>
        IndexSelection.Apply(QueryPlanner.Plan(bound), _database);

    /// <summary>The optimised plan for a query — exposed for tests to assert the chosen access path/strategy
    /// (e.g. that an unindexed equi-join becomes a hash join).</summary>
    internal PlanNode PlanFor(string sql)
    {
        SqlStatement ast = ViewExpander.Expand(_parser.ParseStatement(sql), _database, _parser);
        return ColumnPruning.Apply(PlanWithIndexes(_binder.Bind(ast)));
    }

    /// <summary>The stored query <paramref name="name"/> names, for running it and describing it alike; a missing one
    /// is refused with ACE's message for a missing input (verified, for <c>EXEC</c>).</summary>
    private StoredQuery Procedure(string name) =>
        _database.Catalog.FindQuery(name) ?? throw new InvalidOperationException(SqlBindException.InputNotFound(name));

    /// <summary>
    /// Runs an <c>EXECUTE|EXEC procedure arg, …</c>: resolves the stored procedure/query by name, binds the
    /// positional argument values to its declared parameters (in declaration order), and runs it — a stored
    /// SELECT returns its rows, a stored action query (INSERT/DDL) returns its rows-affected count.
    /// </summary>
    private CommandResult ExecuteProcedure(ExecuteStatement exec, IReadOnlyDictionary<string, object?>? parameters)
    {
        StoredQuery query = Procedure(exec.Procedure);
        var bag = new ParameterBag(parameters);
        var executor = new QueryExecutor(_database, parameters, _session);
        var evaluator = new ExpressionEvaluator(new EvalScope([], [], null), executor, bag, _session);

        List<string> paramNames = query.Parameters.Select(p => p.Name).ToList();

        // Access EXEC arguments take three shapes (EF emits all of them):
        //   procParam = value  → a NAMED argument (bind the proc's named parameter to the value)
        //   name               → a bare identifier REFERENCING a supplied command parameter (`EXEC p CustomerID`)
        //   literal / @param   → a positional value
        var named = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var positional = new List<object?>();
        foreach (Expression arg in exec.Arguments)
        {
            switch (arg)
            {
                case BinaryExpression { Operator: BinaryOperator.Equal, Left: ColumnReference { Table: null } target } eq:
                    named[target.Column] = evaluator.Evaluate(eq.Right);
                    break;
                case ColumnReference { Table: null } paramRef:
                    positional.Add(bag.Resolve(paramRef.Column));
                    break;
                default:
                    positional.Add(evaluator.Evaluate(arg));
                    break;
            }
        }

        Dictionary<string, object?> args;
        if (named.Count > 0)
        {
            if (positional.Count > 0)
                throw new InvalidOperationException(
                    $"Procedure '{exec.Procedure}' was executed with a mix of named and positional arguments.");
            args = named; // keyed by the procedure's parameter names directly
        }
        else
        {
            if (positional.Count != paramNames.Count)
                throw new InvalidOperationException(
                    $"Procedure '{exec.Procedure}' declares {paramNames.Count} parameter(s) but was executed with {positional.Count} argument(s).");
            // IDE0028's only fix here is `[]`, which would silently drop the comparer.
#pragma warning disable IDE0028
            args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore IDE0028
            for (int i = 0; i < paramNames.Count; i++) args[paramNames[i]] = positional[i];
        }

        // A stored SELECT (with its PARAMETERS clause) → rows; a stored action query → rows-affected.
        string sql = query.RequireSql();
        return query.IsAction
            ? new CommandResult(ResultSet.Empty, ExecuteNonQuery(sql, args))
            : Execute(sql, args);
    }
}

/// <summary>The outcome of <see cref="QueryEngine.Execute"/>: query rows (with
/// <see cref="RecordsAffected"/> = -1) or an empty set with the DML rows-affected count.</summary>
public sealed record CommandResult(ResultSet Rows, int RecordsAffected);
