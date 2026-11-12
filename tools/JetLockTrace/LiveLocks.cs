using System.Buffers.Binary;
using System.Text;
using LibRed.Formats;
using LibRed.IO;

namespace JetLockTrace;

/// <summary>
///     The two modes that work on a live lock file rather than a trace: <c>probe</c>, which reports who is connected
///     to a database, and <c>hold</c>, which takes one lock and keeps it so Access can be made to run into it.
/// </summary>
/// <remarks>
///     Both lock through <see cref="JetLockFile" /> at offsets from <see cref="JetLockLayout" />, the same copy
///     LibRed uses. Neither writes to the database or the lock file, and neither creates a missing lock file.
/// </remarks>
public static class LiveLocks
{
    /// <summary>
    ///     The user a lock is taken as when <c>--as-user</c> is not given: the highest number, which an Access session
    ///     only reaches when 255 users are connected, so the holder reads as "not a real session".
    /// </summary>
    private const int DefaultHoldUser = JetLockLayout.MaxUserNumber;

    /// <summary><c>probe &lt;database&gt;</c>.</summary>
    public static int Probe(string[] args)
    {
        if (args.Length != 1 || args[0].StartsWith('-'))
        {
            Console.Error.WriteLine("Usage: jetlocktrace probe <database>. Try --help.");
            return 1;
        }

        if (!TryLocateLockFile(args[0], out string databasePath, out string? lockPath))
        {
            return 1;
        }

        if (!File.Exists(lockPath))
        {
            Console.WriteLine($"No lock file at {lockPath}: nobody has the database open (in shared mode).");
            return 0;
        }

        // Page 0's commit-byte table, read with the database's own format. Shared for everything Access may do.
        JetFormatBase format;
        byte[] commitTable;
        using (var database = new FileStream(
                   databasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            format = JetFormatBase.Detect(database);
            commitTable = new byte[format.CommitByteTableUsers * format.CommitByteSlotSize];
            database.Seek(format.CommitByteTableOffset, SeekOrigin.Begin);
            database.ReadExactly(commitTable);
        }

        // The per-user records are at the start of the lock file; the locks are far past its end.
        byte[] records;
        using (var lockStream = new FileStream(
                   lockPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            records = new byte[lockStream.Length];
            lockStream.ReadExactly(records);
        }

        var connected = new List<(int User, bool UserByteHeld, bool SessionByteHeld)>();
        using (JetLockFile lockFile = JetLockFile.Open(lockPath, createIfMissing: false))
        {
            // A connected user holds its byte in group 0 and its byte in group 1 for the whole session, so a lock on
            // either conflicts exactly when that user is connected. Taken and released at once: the probe must not
            // stand in the way of a user connecting meanwhile.
            for (var user = 1; user <= JetLockLayout.MaxUserNumber; user++)
            {
                bool userByteHeld = IsHeld(lockFile, JetLockLayout.UserByte(user));
                bool sessionByteHeld = IsHeld(lockFile, JetLockLayout.SessionByte(user));
                if (userByteHeld || sessionByteHeld)
                {
                    connected.Add((user, userByteHeld, sessionByteHeld));
                }
            }
        }

        Console.WriteLine($"Database   {databasePath}");
        Console.WriteLine($"Lock file  {lockPath} ({records.Length} bytes, {records.Length / JetLockLayout.UserRecordSize} records)");
        Console.WriteLine($"Commit slot 0 (exclusive mode): {DescribeSlot(commitTable, 0, format)}");
        Console.WriteLine();

        if (connected.Count == 0)
        {
            Console.WriteLine("No user byte is locked: nobody is connected.");
            return 0;
        }

        Console.WriteLine($"{connected.Count} connected user(s):");
        foreach ((int user, bool userByteHeld, bool sessionByteHeld) in connected)
        {
            Console.WriteLine();
            Console.WriteLine($"user {user}");
            Console.WriteLine($"  user byte    0x{JetLockLayout.UserByte(user):X8}  {(userByteHeld ? "locked" : "free")}");
            Console.WriteLine($"  session byte 0x{JetLockLayout.SessionByte(user):X8}  {(sessionByteHeld ? "locked" : "free")}");
            Console.WriteLine($"  commit slot {user}:  {DescribeSlot(commitTable, user, format)}");
            Console.WriteLine($"  record (index {user - 1}): {DescribeRecord(records, JetLockLayout.UserRecordOffset(user))}");
        }

        return 0;
    }

    // Taken and released at once; true when another handle holds the byte.
    private static bool IsHeld(JetLockFile lockFile, long offset)
    {
        if (!lockFile.TryLock(offset, 1))
        {
            return true;
        }

        lockFile.Unlock(offset, 1);
        return false;
    }

    /// <summary>
    ///     <c>hold &lt;database&gt; --region &lt;1-6&gt; (--page &lt;n&gt; | --user-slot &lt;n&gt;) [--shared] [--as-user
    ///     &lt;n&gt;]</c>.
    /// </summary>
    public static int Hold(string[] args)
    {
        string? database = null;
        int? region = null, page = null, userSlot = null, asUser = null;
        var shared = false;

        try
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--region" when i + 1 < args.Length:
                        region = ParseNumber(args[++i], "--region");
                        break;
                    case "--page" when i + 1 < args.Length:
                        page = ParseNumber(args[++i], "--page");
                        break;
                    case "--user-slot" when i + 1 < args.Length:
                        userSlot = ParseNumber(args[++i], "--user-slot");
                        break;
                    case "--as-user" when i + 1 < args.Length:
                        asUser = ParseNumber(args[++i], "--as-user");
                        break;
                    case "--shared":
                        shared = true;
                        break;
                    default:
                        if (args[i].StartsWith('-') || database is not null)
                        {
                            Console.Error.WriteLine($"Unexpected argument '{args[i]}'. Try --help.");
                            return 1;
                        }

                        database = args[i];
                        break;
                }
            }
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        if (database is null || region is null)
        {
            Console.Error.WriteLine("Usage: jetlocktrace hold <database> --region <1-6> (--page <n> | --user-slot <n>) ... Try --help.");
            return 1;
        }

        if (region is < 1 or > 6)
        {
            Console.Error.WriteLine("--region must be 1 to 6.");
            return 1;
        }

        long offset, length;
        try
        {
            (offset, length) = Range((JetLockRegion)region.Value, page, userSlot, shared, asUser);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        if (!TryLocateLockFile(database, out _, out string? lockPath))
        {
            return 1;
        }

        // Never create one: a lock file that appears from nowhere tells Access a user is (or was) there.
        if (!File.Exists(lockPath))
        {
            Console.Error.WriteLine($"No lock file at {lockPath}. Open the database in Access first; hold never creates one.");
            return 1;
        }

        using JetLockFile lockFile = JetLockFile.Open(lockPath, createIfMissing: false);
        string description = LockDecoder.DescribeLock(offset, length);

        if (!lockFile.TryLock(offset, length))
        {
            Console.WriteLine($"CONFLICT  {description}  (another lock holds those bytes; nothing taken)");
            return 2;
        }

        try
        {
            Console.WriteLine($"HELD      {description}");
            Console.WriteLine($"          offset 0x{offset:X8}, {length} byte(s), on {lockPath}");
            Console.WriteLine("Press Enter to release.");
            Console.ReadLine();
        }
        finally
        {
            lockFile.Unlock(offset, length);
        }

        Console.WriteLine("Released.");
        return 0;
    }

    /// <summary>The range <c>hold</c> takes for its options, or an <see cref="ArgumentException" /> naming what is wrong.</summary>
    private static (long Offset, long Length) Range(
        JetLockRegion region, int? page, int? userSlot, bool shared, int? asUser)
    {
        if (page is null == userSlot is null)
        {
            throw new ArgumentException("Give exactly one of --page and --user-slot.");
        }

        if (userSlot is { } slot)
        {
            if (region != JetLockRegion.User || shared || asUser is not null)
            {
                throw new ArgumentException("--user-slot is region 1 only, and takes neither --shared nor --as-user.");
            }

            return (JetLockLayout.UserByte(slot), 1);
        }

        if (region == JetLockRegion.User)
        {
            throw new ArgumentException("Region 1 is not page-based: use --user-slot.");
        }

        // Both kinds are taken as a user: a shared lock is that user's own byte, an exclusive one ends on it.
        int user = asUser ?? DefaultHoldUser;
        return shared
            ? (JetLockLayout.SharedByte(region, page!.Value, user), 1)
            : JetLockLayout.ExclusiveRange(region, page!.Value, user);
    }

    /// <summary>
    ///     Resolves the database path and the lock file beside it; reports and returns false when the database is
    ///     missing or has an extension Jet/ACE does not lock.
    /// </summary>
    private static bool TryLocateLockFile(string database, out string databasePath, out string? lockPath)
    {
        databasePath = Path.GetFullPath(database);
        lockPath = JetLockFile.PathFor(databasePath);

        if (!File.Exists(databasePath))
        {
            Console.Error.WriteLine($"'{databasePath}' does not exist.");
            return false;
        }

        if (lockPath is null)
        {
            Console.Error.WriteLine($"'{databasePath}' is not a .accdb, .accde, .mdb or .mdw file.");
            return false;
        }

        return true;
    }

    /// <summary>A decimal or <c>0x</c>-prefixed hex number, or an <see cref="ArgumentException" /> naming the option.</summary>
    private static int ParseNumber(string text, string option)
    {
        bool hex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        return int.TryParse(
            hex ? text[2..] : text,
            hex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.None,
            null,
            out int value)
            ? value
            : throw new ArgumentException($"{option} needs a number, not '{text}'.");
    }

    private static string DescribeSlot(byte[] commitTable, int user, JetFormatBase format)
    {
        ReadOnlySpan<byte> slot = commitTable.AsSpan(user * format.CommitByteSlotSize, format.CommitByteSlotSize);
        return $"{Convert.ToHexString(slot)}  = {BinaryPrimitives.ReadUInt16LittleEndian(slot)}";
    }

    private static string DescribeRecord(byte[] records, long start)
    {
        if (start + JetLockLayout.UserRecordSize > records.Length)
        {
            return "(past the end of the lock file)";
        }

        ReadOnlySpan<byte> record = records.AsSpan((int)start, JetLockLayout.UserRecordSize);
        return $"computer \"{Text(record[..JetLockLayout.RecordNameSize])}\", security \"{Text(record[JetLockLayout.RecordNameSize..])}\"";
    }

    // The names are NUL-padded ANSI; anything unprintable is shown as '.', so a record that is not text is visible.
    private static string Text(ReadOnlySpan<byte> name)
    {
        int end = name.IndexOf((byte)0);
        var text = new StringBuilder();
        foreach (byte b in end < 0 ? name : name[..end])
        {
            text.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
        }

        return text.ToString();
    }
}
