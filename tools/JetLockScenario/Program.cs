using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Text;

// Interactive ACE driver for lock tracing. See README.md.

const string Usage =
    "usage: jetlockscenario <database> [--exclusive] [--read-only] [--locking page|row] [--name <label>]";

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine(Usage);
    return args.Length == 0 ? 1 : 0;
}

string? databaseArg = null;
bool exclusive = false, readOnly = false;
int? lockingMode = null;
string? name = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--exclusive":
            exclusive = true;
            break;
        case "--read-only":
            readOnly = true;
            break;
        case "--locking" when i + 1 < args.Length:
            string mode = args[++i];
            lockingMode = mode.Equals("page", StringComparison.OrdinalIgnoreCase) ? 0
                : mode.Equals("row", StringComparison.OrdinalIgnoreCase) ? 1
                : null;
            if (lockingMode is null)
            {
                Console.Error.WriteLine("--locking takes 'page' or 'row'.");
                return 1;
            }

            break;
        case "--name" when i + 1 < args.Length:
            name = args[++i];
            break;
        default:
            if (args[i].StartsWith('-') || databaseArg is not null)
            {
                Console.Error.WriteLine($"Unexpected argument '{args[i]}'.");
                Console.Error.WriteLine(Usage);
                return 1;
            }

            databaseArg = args[i];
            break;
    }
}

if (databaseArg is null || (exclusive && readOnly))
{
    Console.Error.WriteLine(databaseArg is null ? Usage : "--exclusive and --read-only cannot be combined.");
    return 1;
}

string database = Path.GetFullPath(databaseArg);

// ACE writes to any file it opens (it has grown the Northwind fixture before), so never open the fixture itself.
if (database.Contains("JetProviderExceptionTests", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine(
        "Refusing to open a database under JetProviderExceptionTests: ACE writes to any file it opens. "
        + "Work on a copy.");
    return 1;
}

if (!File.Exists(database))
{
    Console.Error.WriteLine($"'{database}' does not exist.");
    return 1;
}

int pid = Environment.ProcessId;
string label = Sanitize(name ?? pid.ToString(), 40);

// Same provider order and "OLE DB Services=-4" (no pooling) as test/LibRed.Shared/AceTestDatabase.cs. That file is not
// linked: its Open has no Mode / locking-mode parameters and the file also depends on TemporaryDatabase.cs.
string[] providers = ["Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0"];
string modePart = exclusive ? "Mode=Share Exclusive;" : readOnly ? "Mode=Read;" : "";
string lockPart = lockingMode is { } lm ? $"Jet OLEDB:Database Locking Mode={lm};" : "";

OleDbConnection? connection = null;
string connectionString = "";
Exception? openFailure = null;
foreach (string provider in providers)
{
    connectionString = $"Provider={provider};Data Source={database};{modePart}{lockPart}OLE DB Services=-4;";
    try
    {
        connection = new OleDbConnection(connectionString);
        connection.Open();
        break;
    }
    catch (Exception ex) when (ex is OleDbException or InvalidOperationException)
    {
        openFailure = ex;
        Console.Error.WriteLine($"Could not open with {provider}:");
        PrintException(ex, Console.Error);
        connection?.Dispose();
        connection = null;
    }
}

if (connection is null)
{
    Console.Error.WriteLine("No ACE OLE DB provider could open the database.");
    _ = openFailure;
    return 1;
}

Console.WriteLine($"process id : {pid}");
Console.WriteLine($"label      : {label}");
Console.WriteLine($"provider   : {connection.Provider} (server version {connection.ServerVersion})");
Console.WriteLine($"connection : {connectionString}");
Console.WriteLine($"bitness    : {(Environment.Is64BitProcess ? "x64" : "x86")}");
Console.WriteLine("type 'help' for commands.");

OleDbTransaction? transaction = null;
OleDbCommand? heldCommand = null;
OleDbDataReader? heldReader = null;
int sequence = 0;
int alterCounter = 0;
Random random = new(12345);

while (true)
{
    Console.Write($"[{label}{(transaction is null ? "" : " tx")}]> ");
    string? line = Console.ReadLine();
    if (line is null)
    {
        line = "quit";
        Console.WriteLine(line);
    }

    line = line.Trim();
    if (line.Length == 0)
    {
        continue;
    }

    int space = line.IndexOf(' ');
    string command = (space < 0 ? line : line[..space]).ToLowerInvariant();
    string rest = space < 0 ? "" : line[(space + 1)..].Trim();

    if (command == "help")
    {
        Console.WriteLine(
            """
            setup                  create LockT (Id, Grp, Txt, Notes) + index on Grp, 200 rows with ~3000-char memos
            select [table]         SELECT * reading every row and column (default LockT)
            memo <id>              read Notes of one row
            update <id>            UPDATE LockT SET Txt = ... WHERE Id = <id>
            insert [n]             n single-row INSERTs (default 1); prints @@IDENTITY
            delete <id>            DELETE FROM LockT WHERE Id = <id>
            begin | commit | rollback   a transaction on the connection; later commands run inside it
            ddl-index              CREATE INDEX on LockT(Txt)
            ddl-alter              ALTER TABLE LockT ADD COLUMN (a new one each time)
            ddl-drop               create LockScratch, then DROP TABLE it
            sql <statement>        arbitrary SQL (a reader if it starts with SELECT)
            reader-open [sql]      ExecuteReader (default SELECT * FROM LockT), read one row, keep the reader open
            reader-close           dispose the held reader and its command
            idle <seconds>         sleep
            mark <label>           emit a trace marker only
            kill                   terminate this process at once, closing nothing (simulates a crash)
            close | quit           close the connection and exit
            """);
        continue;
    }

    Marker("begin", line);
    int exitCode = -1;
    Stopwatch stopwatch = Stopwatch.StartNew();
    try
    {
        switch (command)
        {
            case "setup":
                Setup();
                break;
            case "select":
                Select(rest.Length == 0 ? "LockT" : rest);
                break;
            case "memo":
                Memo(RequireId(rest));
                break;
            case "update":
                Update(RequireId(rest));
                break;
            case "insert":
                Insert(rest.Length == 0 ? 1 : int.Parse(rest));
                break;
            case "delete":
                Execute($"DELETE FROM LockT WHERE Id = {RequireId(rest)}");
                break;
            case "begin":
                if (transaction is not null)
                {
                    throw new InvalidOperationException("A transaction is already open.");
                }

                transaction = connection.BeginTransaction();
                Console.WriteLine("transaction begun");
                break;
            case "commit":
                RequireTransaction().Commit();
                transaction!.Dispose();
                transaction = null;
                Console.WriteLine("committed");
                break;
            case "rollback":
                RequireTransaction().Rollback();
                transaction!.Dispose();
                transaction = null;
                Console.WriteLine("rolled back");
                break;
            case "ddl-index":
                Execute("CREATE INDEX IX_LockT_Txt ON LockT (Txt)");
                break;
            case "ddl-alter":
                Execute($"ALTER TABLE LockT ADD COLUMN Extra{++alterCounter}_{pid} LONG");
                break;
            case "ddl-drop":
                if (!TableExists("LockScratch"))
                {
                    Execute("CREATE TABLE LockScratch (Id LONG)");
                }

                Execute("DROP TABLE LockScratch");
                break;
            case "sql":
                if (rest.Length == 0)
                {
                    throw new ArgumentException("sql needs a statement.");
                }

                if (rest.StartsWith("select", StringComparison.OrdinalIgnoreCase))
                {
                    ReadAll(rest);
                }
                else
                {
                    Execute(rest);
                }

                break;
            case "reader-open":
                OpenReader(rest.Length == 0 ? "SELECT * FROM LockT" : rest);
                break;
            case "reader-close":
                CloseReader();
                break;
            case "idle":
                Console.WriteLine($"sleeping {rest} s");
                Thread.Sleep(TimeSpan.FromSeconds(double.Parse(rest, System.Globalization.CultureInfo.InvariantCulture)));
                break;
            case "mark":
                Console.WriteLine($"marker '{rest}' (the begin/end markers around this command carry it)");
                break;
            case "kill":
                Console.WriteLine("killing the process without closing anything");
                Process.GetCurrentProcess().Kill();
                break;
            case "close":
            case "quit":
                exitCode = 0;
                break;
            default:
                Console.WriteLine($"unknown command '{command}'. Try help.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR in '{line}':");
        PrintException(ex, Console.Out);
    }

    stopwatch.Stop();
    Console.WriteLine($"({line}: {stopwatch.Elapsed.TotalMilliseconds:F1} ms)");

    if (exitCode == 0)
    {
        try
        {
            // Closing with a transaction open rolls it back; say so, do not hide it.
            if (transaction is not null)
            {
                Console.WriteLine("closing with a transaction still open (it is rolled back by the close)");
            }

            if (heldReader is not null)
            {
                Console.WriteLine("closing with a reader still open (it is disposed first)");
                CloseReader();
            }

            connection.Close();
            connection.Dispose();
            Console.WriteLine("connection closed");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR closing:");
            PrintException(ex, Console.Out);
        }

        Marker("end", line);
        return 0;
    }

    Marker("end", line);
}

// ---------------------------------------------------------------------------------------------------------------

void Marker(string phase, string commandLine)
{
    // Probes a file that does not exist: ProcMon records a CreateFile / QueryOpen with NAME NOT FOUND on a path
    // containing the database file name, and nothing is created.
    string cmd = Sanitize(commandLine, 60);
    string path = $"{database}.mark.{label}.{++sequence:D4}.{phase}.{cmd}";
    try
    {
        _ = File.Exists(path);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"(marker failed: {ex.Message})");
    }
}

static string Sanitize(string text, int max)
{
    StringBuilder sb = new();
    foreach (char c in text)
    {
        sb.Append(char.IsAsciiLetterOrDigit(c) || c is '-' ? c : '_');
        if (sb.Length >= max)
        {
            break;
        }
    }

    return sb.ToString();
}

static void PrintException(Exception ex, TextWriter output)
{
    for (Exception? e = ex; e is not null; e = e.InnerException)
    {
        output.WriteLine($"  {e.GetType().FullName}: {e.Message}");
        if (e is OleDbException ole)
        {
            output.WriteLine($"    ErrorCode (HRESULT): 0x{ole.ErrorCode:X8}");
            for (int i = 0; i < ole.Errors.Count; i++)
            {
                OleDbError err = ole.Errors[i];
                output.WriteLine(
                    $"    Errors[{i}]: NativeError={err.NativeError} SQLState={err.SQLState} Source={err.Source}");
                output.WriteLine($"      Message: {err.Message}");
            }
        }
    }
}

static int RequireId(string text)
    => int.TryParse(text, out int id) ? id : throw new ArgumentException("This command needs a numeric <id>.");

OleDbTransaction RequireTransaction()
    => transaction ?? throw new InvalidOperationException("No transaction is open.");

OleDbCommand NewCommand(string sql)
{
    OleDbCommand cmd = connection!.CreateCommand();
    cmd.CommandText = sql;
    cmd.Transaction = transaction;
    return cmd;
}

void Execute(string sql)
{
    Console.WriteLine($"run: {sql}");
    using OleDbCommand cmd = NewCommand(sql);
    int affected = cmd.ExecuteNonQuery();
    Console.WriteLine($"rows affected: {affected}");
}

void OpenReader(string sql)
{
    if (heldReader is not null)
    {
        throw new InvalidOperationException("A reader is already open; reader-close it first.");
    }

    Console.WriteLine($"run: {sql}");
    OleDbCommand cmd = NewCommand(sql);
    try
    {
        OleDbDataReader reader = cmd.ExecuteReader();
        heldCommand = cmd;
        heldReader = reader;
        Console.WriteLine($"reader open; first row read: {reader.Read()} ({reader.FieldCount} columns)");
    }
    catch
    {
        if (heldReader is null)
        {
            cmd.Dispose();
        }

        throw;
    }
}

void CloseReader()
{
    if (heldReader is null)
    {
        throw new InvalidOperationException("No reader is open.");
    }

    heldReader.Dispose();
    heldCommand!.Dispose();
    heldReader = null;
    heldCommand = null;
    Console.WriteLine("reader closed");
}

int ReadAll(string sql)
{
    Console.WriteLine($"run: {sql}");
    using OleDbCommand cmd = NewCommand(sql);
    using OleDbDataReader reader = cmd.ExecuteReader();
    int rows = 0;
    long chars = 0;
    while (reader.Read())
    {
        rows++;
        for (int i = 0; i < reader.FieldCount; i++)
        {
            object value = reader.GetValue(i);
            chars += value is string s ? s.Length : value is byte[] b ? b.Length : 0;
        }
    }

    Console.WriteLine($"rows read: {rows} ({reader.FieldCount} columns, {chars} chars/bytes of text and binary)");
    return rows;
}

bool TableExists(string table)
{
    DataTable tables = connection!.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, [null, null, table, "TABLE"])!;
    return tables.Rows.Count > 0;
}

void Setup()
{
    if (TableExists("LockT"))
    {
        Console.WriteLine("LockT already exists; skipping.");
        return;
    }

    Execute("CREATE TABLE LockT (Id AUTOINCREMENT PRIMARY KEY, Grp LONG, Txt TEXT(50), Notes MEMO)");
    Execute("CREATE INDEX IX_LockT_Grp ON LockT (Grp)");
    Insert(200);
}

void Insert(int count)
{
    Console.WriteLine($"run: {count} x INSERT INTO LockT (Grp, Txt, Notes) VALUES (?, ?, ?)");
    for (int i = 0; i < count; i++)
    {
        using OleDbCommand cmd = NewCommand("INSERT INTO LockT (Grp, Txt, Notes) VALUES (?, ?, ?)");
        int n = random.Next(1, 1000);
        cmd.Parameters.Add("grp", OleDbType.Integer).Value = n % 20;
        cmd.Parameters.Add("txt", OleDbType.VarWChar, 50).Value = $"row {n} from {label}";
        // A literal in Jet SQL tops out at 255 characters, so the memo goes in as a parameter.
        cmd.Parameters.Add("notes", OleDbType.LongVarWChar).Value = new string((char)('a' + n % 26), 3000);
        cmd.ExecuteNonQuery();
    }

    using OleDbCommand identity = NewCommand("SELECT @@IDENTITY");
    Console.WriteLine($"inserted {count}; @@IDENTITY = {identity.ExecuteScalar()}");
}

void Select(string table)
{
    ReadAll($"SELECT * FROM [{table.Trim('[', ']')}]");
}

void Memo(int id)
{
    Console.WriteLine($"run: SELECT Notes FROM LockT WHERE Id = {id}");
    using OleDbCommand cmd = NewCommand($"SELECT Notes FROM LockT WHERE Id = {id}");
    object? value = cmd.ExecuteScalar();
    Console.WriteLine(value is null ? "no such row" : $"Notes: {(value as string)?.Length ?? 0} chars");
}

void Update(int id)
{
    string text = $"upd {label} {DateTime.Now:HH:mm:ss.fff}";
    Execute($"UPDATE LockT SET Txt = '{text}' WHERE Id = {id}");
}
