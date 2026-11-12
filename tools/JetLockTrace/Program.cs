using JetLockTrace;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine(
        """
        jetlocktrace — decode Jet/ACE byte-range locks in a Process Monitor trace, or probe and hold
        them on a live lock file.

          jetlocktrace <trace.csv> [options]
          jetlocktrace probe <database>
          jetlocktrace hold <database> --region <1-6> (--page <n> | --user-slot <n>)
                                       [--shared] [--as-user <n>]

        Options
          --page-size <n>   Database page size. Default 4096 (Jet 4 / ACE); use 2048 for Jet 3.
          --canonical       Emit only the decoded operations, with no time, process or file column, so
                            two traces can be diffed directly. Failed operations still show their result.
          --file <substr>   Only rows whose path contains <substr>, for tracing one database at a time.
          --process <p>     Only rows from process <p>: a name (case-insensitive) or an exact PID.
          --locks-only      Only lock/unlock rows, dropping reads and writes (cache-driven I/O noise).
          --held            Instead of listing rows, replay the locks in order and print, per process
                            (PID) and file, the locks still held at the end, plus any unlock that matched
                            no held lock. Failed attempts never count. At every scenario 'end' marker
                            it also prints the locks held at that moment, per process.
          --out [path]      Write to a file instead of stdout. With no path, writes alongside the CSV as
                            <trace>.locks.txt (or <trace>.canonical.txt with --canonical).

        Scenario markers (from tools/JetLockScenario): rows whose path contains '.mark.' are printed as
        '--- <name> <seq> <begin|end> <command> ---' separators, kept under --canonical and --locks-only, and
        subject to --file and --process.

        Every lock ACE takes is an OS-exclusive, fail-immediately lock; Jet's "shared" is a one-byte lock on
        the holder's own user byte, "exclusive" a range over every user's byte. The only thing flagged with
        '!!' is an OS-shared lock (ProcMon 'Exclusive: False'), which ACE has not been seen to take.

        Capture a trace with Process Monitor, filtered to your database name (and to the process, or one
        trace per process), then File > Save > Comma-Separated Values with the default columns. Time of Day,
        Process Name, PID and Result are used when present and optional otherwise.

        To compare two scenarios, or two processes on one scenario:
          jetlocktrace open.CSV   --canonical --out
          jetlocktrace update.CSV --canonical --out
          diff open.canonical.txt update.canonical.txt

          jetlocktrace both.CSV --process msaccess.exe --locks-only --canonical --out access.txt
          jetlocktrace both.CSV --process 4242         --locks-only --canonical --out ours.txt
          diff access.txt ours.txt

        Live lock files (Windows only; neither mode writes to the database or the lock file, nor
        creates a missing lock file):
          probe <database>  Lists the connected users: for each user number 1..255, tries a lock on its
                            group-0 byte and on its group-1 (session) byte (released at once), and a
                            conflict on either means connected. Prints each one's lock-file record
                            (index user-1) and its page-0 commit slot, plus slot 0.
          hold <database>   Takes one lock, says whether it got it, and holds it until Enter.
            --region <1-6>    The lock region (1 user, 2 write/read, 4/5/6 table read/write/deny-write).
            --page <n>        The page to lock (regions 2-6).
            --user-slot <n>   The group-0 user byte to lock (region 1 only), 1 byte.
            --shared          A Jet "shared" lock: the one byte of the page at the --as-user number.
                            Without it, an exclusive range: from byte 1, 256 + the --as-user number long.
            --as-user <n>     The user the page lock is taken as. Default 255, a number no real Access
                            session has unless 255 users are connected.
          Exit code 2 means the lock conflicted and nothing was taken.
          Numbers may be decimal or 0x-prefixed hex.
        """);
    return 0;
}

try
{
    switch (args[0])
    {
        case "probe":
            return LiveLocks.Probe(args[1..]);
        case "hold":
            return LiveLocks.Hold(args[1..]);
    }
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException
                               or NotSupportedException)
{
    // PlatformNotSupportedException is a NotSupportedException: the live modes are Windows-only for now.
    Console.Error.WriteLine(ex.Message);
    return 1;
}

string? csvPath = null;
var pageSize = 4096;
var canonical = false;
string? fileFilter = null;
string? processFilter = null;
int? processPid = null;
var locksOnly = false;
var replayHeld = false;
var toFile = false;
string? outPath = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--page-size" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out pageSize) || pageSize <= 0)
            {
                Console.Error.WriteLine("--page-size needs a positive number.");
                return 1;
            }

            break;
        case "--canonical":
            canonical = true;
            break;
        case "--file" when i + 1 < args.Length:
            fileFilter = args[++i];
            break;
        case "--process" when i + 1 < args.Length:
            processFilter = args[++i];
            processPid = int.TryParse(processFilter, out int pid) ? pid : null;
            break;
        case "--locks-only":
            locksOnly = true;
            break;
        case "--held":
            replayHeld = true;
            break;
        case "--out":
            toFile = true;

            // The path is optional. Don't swallow the trace itself when it is written after --out, which is the
            // one realistic way to get this wrong.
            if (i + 1 < args.Length
                && !args[i + 1].StartsWith('-')
                && !args[i + 1].EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                outPath = args[++i];
            }

            break;
        default:
            if (args[i].StartsWith('-'))
            {
                Console.Error.WriteLine($"Unknown option '{args[i]}'. Try --help.");
                return 1;
            }

            csvPath = args[i];
            break;
    }
}

if (csvPath is null)
{
    Console.Error.WriteLine("No CSV given. Try --help.");
    return 1;
}

if (!File.Exists(csvPath))
{
    Console.Error.WriteLine($"'{csvPath}' does not exist.");
    return 1;
}

// Auto-name alongside the trace, keeping the suffix distinct per mode so a canonical run never silently
// overwrites a full one.
if (toFile)
{
    outPath ??= Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(csvPath)) ?? ".",
        Path.GetFileNameWithoutExtension(csvPath) + (canonical ? ".canonical.txt" : ".locks.txt"));
}

// The confirmation goes to stderr so it never lands in a redirected stdout.
if (outPath is not null)
{
    Console.Error.WriteLine($"Writing to {outPath}");
}

try
{
    using TextWriter output = outPath is null ? Console.Out : new StreamWriter(outPath, append: false);
    var decodedCount = 0;
    HeldLocks? held = replayHeld ? new HeldLocks() : null;

    foreach (TraceEvent e in ProcMonCsv.Read(csvPath))
    {
        // A marker is a probe of a file that does not exist, so it is neither a database nor a lock file and has no
        // offset; it still has to pass --file and --process, and it survives --locks-only.
        bool isMarker = e.IsMarker;

        if (!isMarker && !LockDecoder.IsDatabaseOrLockFile(e.Path))
        {
            continue;
        }

        if (fileFilter is not null && !e.Path.Contains(fileFilter, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!isMarker && e.Offset is null)
        {
            continue;
        }

        if (processFilter is not null
            && !(e.ProcessName?.Equals(processFilter, StringComparison.OrdinalIgnoreCase) == true
                 || (processPid is { } wanted && e.Pid == wanted)))
        {
            continue;
        }

        if (isMarker)
        {
            if (!e.TryParseMarker(out string markerName, out string markerSeq, out string phase, out string markerCommand))
            {
                continue;
            }

            if (held is null)
            {
                output.WriteLine($"--- {markerName} {markerSeq} {phase} {markerCommand} ---");
            }
            else if (phase == "end")
            {
                // The point of --held with markers: what still stands after each step.
                output.WriteLine($"--- {markerName} {markerSeq} {phase} {markerCommand} ---");
                held.WriteSnapshot(output);
            }

            continue;
        }

        long offset = e.Offset!.Value;

        bool isLockOperation = e.IsLock || e.IsUnlock;

        if (locksOnly && !isLockOperation)
        {
            continue;
        }

        if (held is not null)
        {
            held.Apply(e);
            decodedCount++;
            continue;
        }

        bool isLockFile = LockDecoder.IsLockFile(e.Path);

        string decoded = isLockOperation
            ? LockDecoder.DescribeLock(offset, e.Length, e.Exclusive)
            : isLockFile
                ? LockDecoder.DescribeLockFileIo(offset, e.Length)
                : LockDecoder.DescribeDatabaseIo(offset, e.Length, pageSize);

        // A lock row says whether the caller would wait; a failed row says why it did not succeed. Both are
        // behaviour, so they stay in canonical output too.
        if (e.IsLock && e.FailImmediately is { } failImmediately)
        {
            decoded += failImmediately ? "  [failimm]" : "  [wait]";
        }

        if (e.Failed)
        {
            decoded += $"  {e.Result}";
        }

        // The lock/unlock pairing is the interesting rhythm, so keep the verb but drop ProcMon's noise.
        string verb = e.Operation switch
        {
            "UnlockFileSingle" or "UnlockFile" => "unlock",
            "LockFile" => "lock",
            "ReadFile" => "read",
            "WriteFile" => "write",
            _ => e.Operation.ToLowerInvariant(),
        };

        // Canonical output carries no time, process or file, so two runs diff cleanly.
        string who = e.ProcessName is null && e.Pid is null
            ? ""
            : $"{e.ProcessName ?? "?"}({e.Pid?.ToString() ?? "?"}) ";
        string prefix = $"{(e.Time is null ? "" : e.Time + " ")}{who}";

        output.WriteLine(
            canonical
                ? $"{verb,-6} {decoded}"
                : $"{prefix}{verb,-6} {(isLockFile ? "lck" : "db "),-3} {decoded}");

        decodedCount++;
    }

    held?.WriteReport(output);

    if (outPath is not null)
    {
        Console.Error.WriteLine($"{decodedCount} operations decoded.");
    }
}
catch (InvalidDataException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
catch (IOException ex)
{
    Console.Error.WriteLine($"Could not write '{outPath}': {ex.Message}");
    return 1;
}

return 0;
