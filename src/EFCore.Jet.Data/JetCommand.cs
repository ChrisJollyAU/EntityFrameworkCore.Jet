using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using EntityFrameworkCore.Jet.Data.JetStoreSchemaDefinition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace EntityFrameworkCore.Jet.Data
{
    public class JetCommand : DbCommand, ICloneable
    {
#if DEBUG
        private static int _activeObjectsCount;
#endif
        private JetConnection _connection;
        private JetTransaction? _transaction;

        private int _outerSelectSkipEmulationViaDataReaderSkipCount;

        private static readonly Regex _createProcedureExpression = new(@"^\s*create\s*procedure\b", RegexOptions.IgnoreCase);
        private static readonly Regex _topParameterRegularExpression = new(@"(?<=(?:^|\s)select\s+(distinct\s+)??top\s+)(?:@\w+|\?)(?=\s)", RegexOptions.IgnoreCase);
        private static readonly Regex _topMultiParameterRegularExpression = new(@"(?<=(?:^|\s)select\s+(distinct\s+)??top\s+)(?'first'@\w+|\?)(\s)(\+)(\s+)(?'sec'@\w+|\?)", RegexOptions.IgnoreCase);
        private static readonly Regex _topMultiArgumentRegularExpression = new(@"(?<=(?:^|\s)SELECT\s+(distinct\s+)??TOP\s+)(?'first'\d+|\?)(\s)(\+)(\s+)(?'sec'\d+|\?)", RegexOptions.IgnoreCase);
        private static readonly Regex _outerSelectTopValueRegularExpression = new(@"(?<=^\s*select\s+(distinct\s+)??top\s+)\d+(?=\s)", RegexOptions.IgnoreCase);
        private static readonly Regex _outerSelectSkipValueOrParameterRegularExpression = new(@"(?<=^\s*select)\s+skip\s+(?<SkipValueOrParameter>@\w+|\?|\d+)(?=\s)", RegexOptions.IgnoreCase);
        private static readonly Regex _selectRowCountRegularExpression = new(@"^\s*select\s*@@rowcount\s*;?\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex _ifStatementRegex = new(@"^\s*if\s*(?<not>not)?\s*exists\s*\((?<sqlCheckCommand>.+)\)\s*then\s*(?<sqlCommand>.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        protected JetCommand(JetCommand source)
        {
#if DEBUG
            Interlocked.Increment(ref _activeObjectsCount);
#endif
            _connection = source._connection;
            _transaction = source._transaction;

            InnerCommand = (DbCommand)((ICloneable)source.InnerCommand).Clone();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JetCommand"/> class.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="transaction">The transaction.</param>
        internal JetCommand(JetConnection connection, string? commandText = null, JetTransaction? transaction = null)
        {
#if DEBUG
            Interlocked.Increment(ref _activeObjectsCount);
#endif
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _transaction = transaction;

            InnerCommand = connection.JetFactory.InnerFactory.CreateCommand();
            InnerCommand.CommandText = commandText;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                InnerCommand.Dispose();
                foreach (var scl in SplitCommandList)
                {
                    if (scl != this) scl.Dispose(disposing);
                }
            }

            base.Dispose(disposing);

#if DEBUG
            Interlocked.Decrement(ref _activeObjectsCount);
#endif
        }

        internal DbCommand InnerCommand { get; }

        internal IList<JetCommand> SplitCommandList { get; set; } = [];

        /// <summary>
        /// Attempts to Cancels the command execution
        /// </summary>
        public override void Cancel()
            => InnerCommand.Cancel();

        /// <summary>
        /// Gets or sets the command text.
        /// </summary>
        /// <value>
        /// The command text.
        /// </value>
        [AllowNull]
        public override string CommandText
        {
            get => InnerCommand.CommandText;
            set => InnerCommand.CommandText = value;
        }

        /// <summary>
        /// Gets or sets the command timeout.
        /// </summary>
        /// <value>
        /// The command timeout.
        /// </value>
        public override int CommandTimeout
        {
            get => InnerCommand.CommandTimeout;
            set => InnerCommand.CommandTimeout = value;
        }

        /// <summary>
        /// Gets or sets the type of the command.
        /// </summary>
        /// <value>
        /// The type of the command.
        /// </value>
        public override CommandType CommandType
        {
            get => InnerCommand.CommandType;
            set => InnerCommand.CommandType = value;
        }

        /// <summary>
        /// Creates the database parameter.
        /// </summary>
        /// <returns></returns>
        protected override DbParameter CreateDbParameter()
            => InnerCommand.CreateParameter();

        /// <summary>
        /// Gets or sets the database connection.
        /// </summary>
        /// <value>
        /// The database connection.
        /// </value>
        protected override DbConnection? DbConnection
        {
            get => _connection;
            set
            {
                if (value != _connection)
                    throw new NotSupportedException($"The {DbConnection} property cannot be changed.");
            }
        }

        /// <summary>
        /// Gets the database parameter collection.
        /// </summary>
        /// <value>
        /// The database parameter collection.
        /// </value>
        protected override DbParameterCollection DbParameterCollection
            => InnerCommand.Parameters;

        /// <summary>
        /// Gets or sets the database transaction.
        /// </summary>
        /// <value>
        /// The database transaction.
        /// </value>
        protected override DbTransaction? DbTransaction
        {
            get => _transaction;
            set => _transaction = (JetTransaction?)value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether is design time visible.
        /// </summary>
        /// <value>
        ///   <c>true</c> if design time visible; otherwise, <c>false</c>.
        /// </value>
        public override bool DesignTimeVisible { get; set; }

        /// <summary>
        /// Executes the database data reader.
        /// </summary>
        /// <param name="behavior">The behavior.</param>
        /// <returns></returns>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            if (Connection == null)
                throw new InvalidOperationException(Messages.PropertyNotInitialized(nameof(Connection)));

            if (Connection.State != ConnectionState.Open)
                throw new InvalidOperationException(Messages.CannotCallMethodInThisConnectionState("ExecuteReader", ConnectionState.Open, Connection.State));

            ExpandParameters();

            SplitCommandList = SplitCommands();

            for (var i = 0; i < SplitCommandList.Count - 1; i++)
            {
                SplitCommandList[i]
                    .ExecuteNonQueryCore();
            }

            return SplitCommandList[^1]
                .ExecuteDbDataReaderCore(behavior);
        }

        protected virtual DbDataReader ExecuteDbDataReaderCore(CommandBehavior behavior)
        {
            InnerCommand.Connection = _connection.InnerConnection;

            // OLE DB forces us to use an existing active transaction, if one is available.
            InnerCommand.Transaction = _transaction?.WrappedTransaction ?? _connection.ActiveTransaction?.WrappedTransaction;

            LogHelper.ShowCommandText("ExecuteDbDataReader", InnerCommand);

            if (JetInformationSchema.TryGetDataReaderFromInformationSchemaCommand(this, out var dataReader))
                // Retrieve from store schema definition.
                return dataReader;

            if (InnerCommand.CommandType != CommandType.Text)
                return new JetDataReader(InnerCommand.ExecuteReader(behavior));

            if ((dataReader = TryGetDataReaderForSelectRowCount(InnerCommand.CommandText)) == null)
            {
                FixupGlobalVariables();
                PrepareOuterSelectSkipEmulationViaDataReader();
                InlineTopParameters();
                ModifyOuterSelectTopValueForOuterSelectSkipEmulationViaDataReader();
                FixParameters();

                dataReader = new JetDataReader(InnerCommand.ExecuteReader(behavior), _outerSelectSkipEmulationViaDataReaderSkipCount);

                _connection.RowCount = dataReader.RecordsAffected;
            }

            return dataReader;
        }

        /// <summary>
        /// Executes the non query.
        /// </summary>
        /// <returns></returns>
        public override int ExecuteNonQuery()
        {
            if (Connection == null)
                throw new InvalidOperationException(Messages.PropertyNotInitialized(nameof(Connection)));

            ExpandParameters();
            SplitCommandList = SplitCommands();
            return SplitCommandList
                .Aggregate(0, (_, command) => command.ExecuteNonQueryCore());
        }

        protected virtual int ExecuteNonQueryCore()
        {
            if (Connection == null)
                throw new InvalidOperationException(Messages.PropertyNotInitialized(nameof(Connection)));

            LogHelper.ShowCommandText("ExecuteNonQuery", InnerCommand);

            if (JetStoreDatabaseHandling.ProcessDatabaseOperation(this))
                return 1;

            if (JetSchemaOperationsHandling.TryDatabaseOperation((JetConnection)Connection, InnerCommand.CommandText))
                return 1;

            if (Connection.State != ConnectionState.Open)
                throw new InvalidOperationException(Messages.CannotCallMethodInThisConnectionState(nameof(ExecuteNonQuery), ConnectionState.Open, Connection.State));

            InnerCommand.Connection = _connection.InnerConnection;

            // OLE DB forces us to use an existing active transaction, if one is available.
            InnerCommand.Transaction = _transaction?.WrappedTransaction ?? _connection.ActiveTransaction?.WrappedTransaction;

            if (InnerCommand.CommandType != CommandType.Text)
                return InnerCommand.ExecuteNonQuery();

            if (_selectRowCountRegularExpression.Match(InnerCommand.CommandText)
                .Success)
            {
                return _connection.RowCount;
            }

            FixupGlobalVariables();

            if (!CheckExists(InnerCommand.CommandText, out var newCommandText))
                return 0;
            bool isexistssql = newCommandText != InnerCommand.CommandText && (InnerCommand.CommandText.StartsWith("IF EXISTS",StringComparison.OrdinalIgnoreCase) || InnerCommand.CommandText.StartsWith("IF NOT EXISTS", StringComparison.OrdinalIgnoreCase));

            InnerCommand.CommandText = newCommandText;

            InlineTopParameters();
            FixParameters();

            try
            {
                _connection.RowCount = InnerCommand.ExecuteNonQuery();
            }
            catch (DbException e)
            {
                if (!e.Message.Contains("already exists") || !isexistssql)
                {
                    throw;
                }

                return 0;
            }

            // For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command.
            // For all other types of statements, the return value is -1. If a rollback occurs, the return value is also -1.
            // This is how it is stated in the docs, however, the underlying connection actually seems to be returning 0
            // for statements like CREATE TABLE/INDEX, EXEC etc.
            var commandType = newCommandText.Trim()[..6];
            if (commandType.Contains("INSERT", StringComparison.OrdinalIgnoreCase) ||
                commandType.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                commandType.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                return _connection.RowCount;
            }
            return -1;
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored
        /// </summary>
        /// <returns></returns>
        public override object ExecuteScalar()
        {
            if (Connection == null)
                throw new InvalidOperationException(Messages.PropertyNotInitialized(nameof(Connection)));

            if (Connection.State != ConnectionState.Open)
                throw new InvalidOperationException(Messages.CannotCallMethodInThisConnectionState(nameof(ExecuteScalar), ConnectionState.Open, Connection.State));

            ExpandParameters();

            SplitCommandList = SplitCommands();

            for (var i = 0; i < SplitCommandList.Count - 1; i++)
            {
                SplitCommandList[i]
                    .ExecuteNonQueryCore();
            }

            return SplitCommandList[^1]
                .ExecuteScalarCore();
        }

        protected virtual object ExecuteScalarCore()
        {
            if (Connection == null)
                throw new InvalidOperationException(Messages.PropertyNotInitialized(nameof(Connection)));

            if (Connection.State != ConnectionState.Open)
                throw new InvalidOperationException(Messages.CannotCallMethodInThisConnectionState(nameof(ExecuteScalar), ConnectionState.Open, Connection.State));

            InnerCommand.Connection = _connection.InnerConnection;

            // OLE DB forces us to use an existing active transaction, if one is available.
            InnerCommand.Transaction = _transaction?.WrappedTransaction ?? _connection.ActiveTransaction?.WrappedTransaction;

            LogHelper.ShowCommandText("ExecuteScalar", InnerCommand);

            if (JetInformationSchema.TryGetDataReaderFromInformationSchemaCommand(this, out var dataReader))
            {
                // Retrieve from store schema definition.
                if (dataReader.HasRows)
                {
                    dataReader.Read();
                    return dataReader[0];
                }

                return DBNull.Value;
            }

            FixupGlobalVariables();
            InlineTopParameters();
            FixParameters();

            return InnerCommand.ExecuteScalar();
        }

        protected virtual IList<JetCommand> SplitCommands()
        {
            //Remove any tag lines from the sql created by ef core. Jet doesn't like comments/tags in the SQL
            var lines = CommandText.Split([Environment.NewLine], StringSplitOptions.None);
            var filteredLines = lines.Where(line => !line.TrimStart().StartsWith("--"));
            CommandText = string.Join(Environment.NewLine, filteredLines).TrimStart();

            // At this point, all parameters have already been expanded.

            var parser = new JetCommandParser(CommandText);
            var commandDelimiters = parser.GetStateIndices(';');
            var currentCommandStart = 0;
            var usedParameterCount = 0;
            var commands = new List<JetCommand>();

            if (commandDelimiters.Count > 0)
            {
                foreach (var commandDelimiter in commandDelimiters)
                {
                    var commandText = CommandText.Substring(currentCommandStart, commandDelimiter - currentCommandStart)
                        .Trim();

                    if (!string.IsNullOrEmpty(commandText))
                    {
                        var command = (JetCommand)((ICloneable)this).Clone();
                        command.CommandText = commandText;

                        if (_createProcedureExpression.IsMatch(command.CommandText))
                        {
                            command.Parameters.Clear();
                        }
                        else
                        {
                            //don't touch parameters if EXEC stored procedure. Jet parameters do not use the @ symbol with parameter arguments in EXEC.
                            //As such this code would not find any parameters and clear them all out.
                            if (!commandText.StartsWith("EXEC", StringComparison.InvariantCultureIgnoreCase))
                            {
                                for (var i = 0; i < usedParameterCount && command.Parameters.Count > 0; i++)
                                {
                                    command.Parameters.RemoveAt(0);
                                }

                                var parameterIndices = parser.GetStateIndices(
                                    ['@', '?'],
                                    currentCommandStart,
                                    commandDelimiter - currentCommandStart);

                                while (command.Parameters.Count > parameterIndices.Count)
                                {
                                    command.Parameters.RemoveAt(parameterIndices.Count);
                                }

                                usedParameterCount += parameterIndices.Count;
                            }
                        }

                        commands.Add(command);
                    }

                    currentCommandStart = commandDelimiter + 1;
                }
            }
            else
            {
                var commandText = CommandText.Trim();
                if (!string.IsNullOrEmpty(commandText))
                {
                    commands.Add(this);
                }
            }

            return commands;
        }

        private DbDataReader TryGetDataReaderForSelectRowCount(string commandText)
        {
            if (_selectRowCountRegularExpression.Match(commandText)
                .Success)
            {
                var dataTable = new DataTable("Rowcount");
                dataTable.Columns.Add("ROWCOUNT", typeof(int));
                dataTable.Rows.Add(_connection.RowCount);
                return new DataTableReader(dataTable);
            }

            return null;
        }

        private bool CheckExists(string commandText, out string newCommandText)
        {
            var match = _ifStatementRegex.Match(commandText);
            newCommandText = commandText;
            if (!match.Success)
                return true;

            var not = match.Groups["not"]
                .Value;
            var sqlCheckCommand = match.Groups["sqlCheckCommand"]
                .Value;
            newCommandText = match.Groups["sqlCommand"]
                .Value;

            bool hasRows;
            using (var command = (JetCommand)((ICloneable)this).Clone())
            {
                command.CommandText = sqlCheckCommand;
                using (var reader = command.ExecuteReader())
                {
                    hasRows = reader.HasRows;
                }
            }

            if (!string.IsNullOrWhiteSpace(not))
                return !hasRows;
            return hasRows;
        }

        private void FixParameters()
        {
            var parameters = InnerCommand.Parameters;

            if (parameters.Count == 0)
                return;

            foreach (DbParameter parameter in parameters)
            {
                /*if (parameter.Value is TimeSpan ts)
                    parameter.Value = JetConfiguration.TimeSpanOffset + ts;*/
                if (parameter.Value is DateTime dt)
                {
                    // Hack: https://github.com/fsprojects/SQLProvider/issues/191
                    parameter.Value = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
                }
            }
        }

        protected virtual void FixupGlobalVariables()
        {
            var commandText = InnerCommand.CommandText;

            commandText = FixupIdentity(commandText);
            commandText = FixupRowCount(commandText);

            InnerCommand.CommandText = commandText;
        }

        protected virtual string FixupIdentity(string commandText)
            => FixupGlobalVariablePlaceholder(
                commandText, "@@identity", (outerCommand, placeholder) =>
                {
                    var command = (DbCommand)((ICloneable)outerCommand.InnerCommand).Clone();
                    command.CommandText = $"SELECT {placeholder}";
                    command.Parameters.Clear();

                    var identityValue = Convert.ToInt32(command.ExecuteScalar());

                    LogHelper.ShowInfo($"{placeholder} = {identityValue}");

                    return identityValue;
                });

        protected virtual string FixupRowCount(string commandText)
            => FixupGlobalVariablePlaceholder(commandText, "@@rowcount", (outerCommand, placeholder) => outerCommand._connection.RowCount);

        protected virtual string FixupGlobalVariablePlaceholder<T>(string commandText, string placeholder, Func<JetCommand, string, T> valueFactory)
            where T : struct
        {
            var parser = new JetCommandParser(commandText);
            var globalVariableIndices = parser.GetStateIndices('$').Reverse();
            var placeholderValue = new Lazy<T>(() => valueFactory(this, placeholder));
            var newCommandText = new StringBuilder(commandText);

            foreach (var globalVariableIndex in globalVariableIndices)
            {
                if (commandText.IndexOf(placeholder, globalVariableIndex, placeholder.Length, StringComparison.OrdinalIgnoreCase) > -1)
                {
                    newCommandText.Remove(globalVariableIndex, placeholder.Length);
                    newCommandText.Insert(globalVariableIndex, placeholderValue.Value);
                }
            }

            return newCommandText.ToString();
        }

        private void InlineTopParameters()
        {
            // We inline all TOP clause parameters of all SELECT statements, because Jet does not support parameters
            // in TOP clauses.
            var parameters = InnerCommand.Parameters.Cast<DbParameter>()
                .ToList();
            var lastCommandText = InnerCommand.CommandText;
            var commandText = lastCommandText;

            if (parameters.Count > 0)
            {
                while ((commandText = _topMultiParameterRegularExpression.Replace(
                           lastCommandText,
                           match =>
                           {
                               var first = match.Groups["first"];
                               var sec = match.Groups["sec"];
                               var sp = Convert.ToInt32(ExtractParameter(commandText, sec.Index, parameters).Value);
                               var fp = Convert.ToInt32(ExtractParameter(commandText, first.Index, parameters).Value);
                               var total = fp + sp;
                               return total.ToString();
                           },
                           1)) != lastCommandText)
                {
                    lastCommandText = commandText;
                }

                while ((commandText = _topParameterRegularExpression.Replace(
                    lastCommandText,
                    match => Convert.ToInt32(
                            ExtractParameter(commandText, match.Index, parameters)
                                .Value)
                        .ToString(),
                    1)) != lastCommandText)
                {
                    lastCommandText = commandText;
                }

                InnerCommand.Parameters.Clear();
                InnerCommand.Parameters.AddRange(parameters.ToArray());
            }
            while ((commandText = _topMultiArgumentRegularExpression.Replace(
                       lastCommandText,
                       match =>
                       {
                           var first = match.Groups["first"];
                           var sec = match.Groups["sec"];
                           return (Convert.ToInt32(first.Value) + Convert.ToInt32(sec.Value)).ToString();
                       },
                       1)) != lastCommandText)
            {
                lastCommandText = commandText;
            }

            InnerCommand.CommandText = commandText;
        }

        private void ModifyOuterSelectTopValueForOuterSelectSkipEmulationViaDataReader()
        {
            // We modify the TOP clause parameter of the outer most SELECT statement if a SKIP clause was also
            // specified, because Jet does not support skipping records at all, but we can optionally emulate skipping
            // behavior for the outer most SELECT statement by controlling how the records are being fetched in
            // JetDataReader.

            if (_outerSelectSkipEmulationViaDataReaderSkipCount > 0)
            {
                InnerCommand.CommandText = _outerSelectTopValueRegularExpression.Replace(
                    InnerCommand.CommandText,
                    match => (int.Parse(match.Value) + _outerSelectSkipEmulationViaDataReaderSkipCount).ToString());
            }
        }

        private void PrepareOuterSelectSkipEmulationViaDataReader()
        {
            // We inline the SKIP clause parameter of the outer most SELECT statement, because Jet does not support
            // skipping records at all, but we can optionally emulate skipping behavior for the outer most SELECT
            // statement by controlling how the records are being fetched in JetDataReader.

            var match = _outerSelectSkipValueOrParameterRegularExpression.Match(InnerCommand.CommandText);

            if (!match.Success)
            {
                return;
            }

            var skipValueOrParameter = match.Groups["SkipValueOrParameter"];

            if (IsParameter(skipValueOrParameter.Value))
            {
                var parameters = InnerCommand.Parameters.Cast<DbParameter>()
                    .ToList();

                if (parameters.Count <= 0)
                {
                    throw new InvalidOperationException($"""Cannot find "{skipValueOrParameter.Value}" parameter for SKIP clause.""");
                }

                var parameter = ExtractParameter(InnerCommand.CommandText, match.Index, parameters);
                _outerSelectSkipEmulationViaDataReaderSkipCount = Convert.ToInt32(parameter.Value);

                InnerCommand.Parameters.Clear();
                InnerCommand.Parameters.AddRange(parameters.ToArray());
            }
            else
            {
                _outerSelectSkipEmulationViaDataReaderSkipCount = int.Parse(skipValueOrParameter.Value);
            }

            InnerCommand.CommandText = InnerCommand.CommandText.Remove(match.Index, match.Length);
        }

        protected virtual bool IsParameter(string fragment)
            => fragment.Equals("?") ||
               fragment.Length >= 2 && fragment[0] == '@' && fragment[1] != '@';

        protected virtual DbParameter ExtractParameter(string commandText, int count, List<DbParameter> parameters)
        {
            var indices = GetParameterIndices(commandText[..count]);
            var parameter = parameters[indices.Count];

            parameters.RemoveAt(indices.Count);

            return parameter;
        }

        protected virtual void ExpandParameters()
        {
            if (_createProcedureExpression.IsMatch(InnerCommand.CommandText))
            {
                return;
            }

            var indices = GetParameterIndices(InnerCommand.CommandText);

            if (indices.Count <= 0)
            {
                return;
            }

            var placeholders = GetParameterPlaceholders(InnerCommand.CommandText, indices);

            if (placeholders.All(t => t.Name.StartsWith("@")))
            {
                MatchParametersAndPlaceholders(placeholders);

                if (JetConnection.GetDataAccessProviderType(_connection.DataAccessProviderFactory) == DataAccessProviderType.Odbc)
                {
                    foreach (var placeholder in placeholders.Reverse())
                    {
                        InnerCommand.CommandText = InnerCommand.CommandText
                            .Remove(placeholder.Index, placeholder.Name.Length)
                            .Insert(placeholder.Index, "?");
                    }
                }

                InnerCommand.Parameters.Clear();
                InnerCommand.Parameters.AddRange(
                    placeholders.Select(p => p.Parameter)
                        .ToArray());
            }
            else if (placeholders.All(t => t.Name == "?"))
            {
                throw new InvalidOperationException("Parameter placeholder count does not match parameter count.");
            }
            else
            {
                throw new InvalidOperationException("Inconsistent parameter placeholder naming used.");
            }
        }

        protected virtual void MatchParametersAndPlaceholders(IReadOnlyList<ParameterPlaceholder> placeholders)
        {
            var unusedParameters = InnerCommand.Parameters
                .Cast<DbParameter>()
                .ToList();

            foreach (var placeholder in placeholders)
            {
                var parameter = unusedParameters
                    .FirstOrDefault(p => placeholder.Name.Equals(p.ParameterName, StringComparison.Ordinal));

                parameter ??= unusedParameters
                        .FirstOrDefault(p => !p.ParameterName.StartsWith('@') && placeholder.Name[1..].Equals(p.ParameterName, StringComparison.Ordinal));

                if (parameter != null)
                {
                    placeholder.Parameter = parameter;
                    unusedParameters.Remove(parameter);
                }
                else
                {
                    parameter = placeholders
                        .FirstOrDefault(p => placeholder.Name.Equals(p.Name, StringComparison.Ordinal))
                        ?.Parameter;

                    if (parameter == null)
                    {
                        throw new InvalidOperationException($"Cannot find parameter with same name as parameter placeholder \"{placeholder.Name}\".");
                    }

                    var newParameter = (DbParameter)(parameter as ICloneable)?.Clone() ?? throw new InvalidOperationException($"Cannot clone parameter \"{parameter.ParameterName}\".");
                    placeholder.Parameter = newParameter;
                }
            }
        }

        protected virtual IReadOnlyList<ParameterPlaceholder> GetParameterPlaceholders(string commandText, IEnumerable<int> indices)
        {
            var placeholders = new List<ParameterPlaceholder>();

            foreach (var index in indices)
            {
                var match = Regex.Match(commandText[index..], @"^(?:\?|@\w+)");

                if (!match.Success)
                {
                    throw new InvalidOperationException("Invalid parameter placeholder found.");
                }

                placeholders.Add(new ParameterPlaceholder { Index = index, Name = match.Value });
            }

            return placeholders.AsReadOnly();
        }

        protected virtual IReadOnlyList<int> GetParameterIndices(string sqlFragment)
            => new JetCommandParser(sqlFragment)
                .GetStateIndices(['@', '?']);

        /// <summary>
        /// Creates a prepared (or compiled) version of the command on the data source
        /// </summary>
        public override void Prepare()
        {
            if (Connection == null)
                throw new InvalidOperationException(Messages.PropertyNotInitialized(nameof(Connection)));

            if (Connection.State != ConnectionState.Open)
                throw new InvalidOperationException(Messages.CannotCallMethodInThisConnectionState(nameof(Prepare), ConnectionState.Open, Connection.State));

            InnerCommand.Connection = _connection.InnerConnection;

            // OLE DB forces us to use an existing active transaction, if one is available.
            InnerCommand.Transaction = _transaction?.WrappedTransaction ?? _connection.ActiveTransaction?.WrappedTransaction;

            InnerCommand.Prepare();
        }

        /// <summary>
        /// Gets or sets how command results are applied to the DataRow when used by the Update method of a DbDataAdapter.
        /// </summary>
        /// <value>
        /// The updated row source.
        /// </value>
        public override UpdateRowSource UpdatedRowSource
        {
            get => InnerCommand.UpdatedRowSource;
            set => InnerCommand.UpdatedRowSource = value;
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>The created object</returns>
        object ICloneable.Clone()
            => new JetCommand(this);

        protected class ParameterPlaceholder
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public DbParameter Parameter { get; set; }
        }
    }
}