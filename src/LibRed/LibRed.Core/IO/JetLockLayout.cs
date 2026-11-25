using System.Text;

namespace LibRed.IO;

/// <summary>
/// The bands of the Jet/ACE lock address space, by the Jet development team's own names (Microsoft's Jet locking
/// white paper). The region is the top nibble of a lock offset; see <see cref="JetLockLayout"/>.
/// </summary>
internal enum JetLockRegion
{
    /// <summary>The user locks: one byte per connected user in each group of 256 (<c>0x10000001</c> onwards).
    /// Not page-based.</summary>
    User = 0x1,

    /// <summary>Write locks (exclusive) and, since Jet 3.0, read locks (Jet's "shared") on data, index and
    /// long-value pages.</summary>
    WriteRead = 0x2,

    /// <summary>Read/commit locks on index and long-value pages — Jet 2.x only.</summary>
    ReadCommit = 0x3,

    /// <summary>Table-read locks, on the table's definition page.</summary>
    TableRead = 0x4,

    /// <summary>Table-write locks, on the table's definition page.</summary>
    TableWrite = 0x5,

    /// <summary>Table deny-write locks, on the table's definition page.</summary>
    TableDenyWrite = 0x6,
}

/// <summary>
/// Where Jet/ACE places its byte-range locks on the lock file (<c>.laccdb</c>/<c>.ldb</c>), and the lock file's
/// per-user records. The one copy of this layout: the lock manager that will share a live file with Access encodes
/// through it, and <c>tools/JetLockTrace</c> decodes traces through it.
/// </summary>
/// <remarks>
/// <para>An offset packs three fields:</para>
/// <code>
///     offset = (region &lt;&lt; 28) | (page &lt;&lt; 9) | byteInWindow
/// </code>
/// <para>The <c>&lt;&lt; 9</c> gives every page a 512-byte window in lock space — not the page size: Jet 3.5 used
/// 2 KB pages with the same shift, and ACE uses 4 KB. Byte 0 of a window is not used. A <b>shared</b> lock (Jet's
/// term) is the one byte at the holder's own user number, <c>window + user</c>, so users share a page because each
/// locks a different byte. An <b>exclusive</b> lock starts at byte 1 and is <see cref="ExclusiveLengthBase"/> plus
/// the holder's user number long: it covers every user's shared byte, and where it ends names the holder.</para>
/// <para>Every one of these is an operating-system <i>exclusive</i> <c>LockFileEx</c> lock, taken without waiting;
/// ACE takes no operating-system shared lock. The distinction between Jet's shared and exclusive is made by which
/// bytes are locked, not by the lock mode.</para>
/// <para>Region <see cref="JetLockRegion.User"/> is not page-based: it is laid out in groups of
/// <see cref="UserGroupSize"/> bytes. A connected user holds its byte in group 0 (<see cref="UserByte"/>, the user
/// array the white paper documents) and its byte in group 1 (<see cref="SessionByte"/>) for the whole session. Group
/// 5 is connection control (<see cref="ConnectionControlGroup"/>): a user that bars new connections holds its byte
/// there, and every user connecting tries the whole group. Group 6 (<see cref="ConnectionControlFollowUpGroup"/>) is
/// tried only by a user that group 5 refused.</para>
/// <para>The locks lie far past the end of the lock file; nothing in it is really locked, the ranges are pure
/// semaphores. Measured against ACE 16 over OLE DB with users 1 and 2 only (see <c>tools/JetLockTrace/README.md</c>).
/// The upper bound of 255 users, and how row-level locking mode places its locks, are from the white paper and
/// unmeasured.</para>
/// </remarks>
internal static class JetLockLayout
{
    /// <summary>The region's position in an offset: its top nibble.</summary>
    public const int RegionShift = 28;

    /// <summary>A page's position in an offset: every page has a <see cref="PageWindowSize"/>-byte window.</summary>
    public const int PageWindowShift = 9;

    /// <summary>Bytes of lock space per page.</summary>
    public const int PageWindowSize = 1 << PageWindowShift;

    /// <summary>What an exclusive lock's length adds the holder's user number to: an exclusive lock by user
    /// <c>n</c> is <c>ExclusiveLengthBase + n</c> bytes long, starting at byte 1 of the window.</summary>
    public const int ExclusiveLengthBase = 256;

    /// <summary>Bytes per group in the <see cref="JetLockRegion.User"/> region.</summary>
    public const int UserGroupSize = 256;

    /// <summary>The group of the <see cref="JetLockRegion.User"/> region in which user <c>n</c> holds byte
    /// <c>n</c>.</summary>
    public const int UserGroup = 0;

    /// <summary>The group of the <see cref="JetLockRegion.User"/> region in which user <c>n</c> holds byte
    /// <c>n - 1</c>.</summary>
    public const int SessionGroup = 1;

    /// <summary>The group of the <see cref="JetLockRegion.User"/> region that is connection control: user <c>n</c>
    /// barring new connections (ACE's <c>Jet OLEDB:Connection Control</c> set to 1, "no new users") locks the whole
    /// group, releases it, and holds byte <c>n</c> until it allows them again or closes (measured for user 1, which
    /// holds <c>0x10000501</c>). Every user connecting tries
    /// the whole group once it has its user number, and a refusal is what keeps it out
    /// (<c>docs/format/locking.md</c> §3).</summary>
    public const int ConnectionControlGroup = 5;

    /// <summary>The group of the <see cref="JetLockRegion.User"/> region that a connecting user tries only when
    /// <see cref="ConnectionControlGroup"/> has refused it: byte 0, and when that is refused too, the byte of each
    /// user number that has a record in the lock file, from the highest down to 1, <c>n</c> for user <c>n</c>. What
    /// the group is for, and what holds it in ACE, is not known.</summary>
    public const int ConnectionControlFollowUpGroup = 6;

    /// <summary>The highest user number (the white paper's bound; only users 1 and 2 are measured). User numbers run
    /// from 1.</summary>
    public const int MaxUserNumber = 255;

    /// <summary>The highest page a window can address: the page field is the 19 bits between the region and the
    /// window.</summary>
    public const int MaxPage = (1 << (RegionShift - PageWindowShift)) - 1;

    /// <summary>Bytes per user record in the lock file: <see cref="RecordNameSize"/> for the computer name, then
    /// <see cref="RecordNameSize"/> for the security name.</summary>
    public const int UserRecordSize = 2 * RecordNameSize;

    /// <summary>Bytes per name in a lock-file user record.</summary>
    public const int RecordNameSize = 32;

    private const long WithinRegionMask = (1L << RegionShift) - 1;
    private const int RegionMask = 0xF;

    /// <summary>The offset of byte 0 of <paramref name="page"/>'s window in <paramref name="region"/>.</summary>
    public static long PageWindow(JetLockRegion region, int page)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(page);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(page, MaxPage);
        return ((long)region << RegionShift) | ((long)page << PageWindowShift);
    }

    /// <summary>The one byte a shared lock on <paramref name="page"/> by <paramref name="user"/> takes: the byte at
    /// the user's own number in the page's window.</summary>
    public static long SharedByte(JetLockRegion region, int page, int user)
    {
        ThrowIfNotUser(user);
        return PageWindow(region, page) + user;
    }

    /// <summary>The range an exclusive lock on <paramref name="page"/> by <paramref name="user"/> takes: from byte 1,
    /// <see cref="ExclusiveLengthBase"/> plus the user's number long, so it covers every user's shared byte.</summary>
    public static (long Offset, long Length) ExclusiveRange(JetLockRegion region, int page, int user)
    {
        ThrowIfNotUser(user);
        return (PageWindow(region, page) + 1, ExclusiveLengthBase + user);
    }

    /// <summary>The ranges <paramref name="user"/> locks to make the shared byte it holds on <paramref name="page"/>
    /// exclusive — an upgrade: every other user's byte of the window, bytes 1 to <see cref="ExclusiveLengthBase"/> but
    /// its own, as the range below its byte (none for user 1) and the range above it. Measured for user 1 only, as
    /// <c>window + 2</c>, length 255 (<c>docs/format/locking.md</c> §2); the shape for a higher number is that rule
    /// carried on, not measured.</summary>
    public static ((long Offset, long Length)? Below, (long Offset, long Length) Above) UpgradeRanges(
        JetLockRegion region, int page, int user)
    {
        ThrowIfNotUser(user);
        long window = PageWindow(region, page);
        return (user > 1 ? (window + 1, user - 1) : null, (window + user + 1, ExclusiveLengthBase - user));
    }

    /// <summary>The one byte in group <see cref="UserGroup"/> of the <see cref="JetLockRegion.User"/> region that
    /// marks <paramref name="user"/> as connected.</summary>
    public static long UserByte(int user) => UserGroupByte(UserGroup, user);

    /// <summary>The byte of <paramref name="user"/> in <paramref name="group"/> of the <see cref="JetLockRegion.User"/>
    /// region: byte <c>n</c> for user <c>n</c>, as in <see cref="UserGroup"/>, <see cref="ConnectionControlGroup"/> and
    /// <see cref="ConnectionControlFollowUpGroup"/> — not in <see cref="SessionGroup"/>, which is one lower
    /// (<see cref="SessionByte"/>).</summary>
    public static long UserGroupByte(int group, int user)
    {
        ThrowIfNotUser(user);
        if (group == SessionGroup)
            throw new ArgumentOutOfRangeException(nameof(group), "A user's session byte is SessionByte.");
        return UserGroupStart(group) + user;
    }

    /// <summary>The offset of byte 0 of <paramref name="group"/> in the <see cref="JetLockRegion.User"/>
    /// region.</summary>
    public static long UserGroupStart(int group)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(group);
        return ((long)JetLockRegion.User << RegionShift) + (long)group * UserGroupSize;
    }

    /// <summary>The one byte in group <see cref="SessionGroup"/> of the <see cref="JetLockRegion.User"/> region that
    /// <paramref name="user"/> holds for its whole session: <c>user - 1</c> into the group.</summary>
    public static long SessionByte(int user)
    {
        ThrowIfNotUser(user);
        return UserGroupStart(SessionGroup) + user - 1;
    }

    /// <summary>The offset of <paramref name="user"/>'s record in the lock file: the record index is
    /// <c>user - 1</c>.</summary>
    public static long UserRecordOffset(int user)
    {
        ThrowIfNotUser(user);
        return (long)(user - 1) * UserRecordSize;
    }

    /// <summary>The <see cref="UserRecordSize"/> bytes of a user's record: each name in its own
    /// <see cref="RecordNameSize"/>-byte field, as single-byte characters, ended by a NUL and padded with spaces —
    /// the shape ACE 16 writes (<c>docs/format/locking.md</c> §1). A name too long for its field is cut to leave room
    /// for the NUL, and a character outside ASCII is written as <c>?</c>; ACE's handling of either is not
    /// measured.</summary>
    public static byte[] UserRecord(string computerName, string securityName)
    {
        byte[] record = new byte[UserRecordSize];
        WriteRecordName(record.AsSpan(0, RecordNameSize), computerName);
        WriteRecordName(record.AsSpan(RecordNameSize, RecordNameSize), securityName);
        return record;
    }

    private static void WriteRecordName(Span<byte> field, string name)
    {
        field.Fill((byte)' ');
        int length = Math.Min(name.Length, RecordNameSize - 1);
        for (int i = 0; i < length; i++)
            field[i] = name[i] is > '\0' and < (char)0x80 ? (byte)name[i] : (byte)'?';
        field[length] = 0;
    }

    /// <summary>The computer name and the security name in a user's record as <see cref="UserRecord"/> lays it out,
    /// <paramref name="record"/> being its <see cref="UserRecordSize"/> bytes, or fewer where the lock file ends: each
    /// field up to its NUL (the whole field when it has none), byte for character, and empty where the file does not
    /// reach the field. What ACE writes for a character outside ASCII is not measured.</summary>
    public static (string ComputerName, string SecurityName) RecordNames(ReadOnlySpan<byte> record)
        => (RecordName(record[..Math.Min(record.Length, RecordNameSize)]),
            RecordName(record.Length > RecordNameSize ? record[RecordNameSize..Math.Min(record.Length, UserRecordSize)] : []));

    private static string RecordName(ReadOnlySpan<byte> field)
    {
        int end = field.IndexOf((byte)0);
        return Encoding.Latin1.GetString(end < 0 ? field : field[..end]);
    }

    /// <summary>The user whose record starts at <paramref name="offset"/> of the lock file, or null when the offset is
    /// not the start of a record.</summary>
    public static int? UserAtRecordOffset(long offset)
        => offset >= 0 && offset % UserRecordSize == 0 && offset / UserRecordSize < MaxUserNumber
            ? (int)(offset / UserRecordSize) + 1
            : null;

    /// <summary>Splits a lock of <paramref name="length"/> bytes (null when unknown) at <paramref name="offset"/>
    /// back into the fields <see cref="SharedByte"/>, <see cref="ExclusiveRange"/>, <see cref="UserGroupByte"/> and
    /// <see cref="SessionByte"/> pack. Any offset decodes; a lock that has none of the known shapes leaves
    /// <see cref="JetLockAddress.User"/> null.</summary>
    public static JetLockAddress Decode(long offset, long? length)
    {
        var region = (JetLockRegion)((offset >> RegionShift) & RegionMask);
        long withinRegion = offset & WithinRegionMask;

        if (region == JetLockRegion.User)
        {
            long group = withinRegion / UserGroupSize;
            long slot = withinRegion % UserGroupSize;
            long user = length is null or 1
                ? group switch
                {
                    UserGroup or ConnectionControlGroup or ConnectionControlFollowUpGroup => slot,
                    SessionGroup => slot + 1,
                    _ => 0,
                }
                : 0;
            return new JetLockAddress(region, withinRegion, group, slot, IsUser(user) ? (int)user : null);
        }

        long page = withinRegion / PageWindowSize;
        long byteInWindow = withinRegion % PageWindowSize;

        if (length is null or 1)
            return new JetLockAddress(region, withinRegion, page, byteInWindow,
                length is 1 && IsUser(byteInWindow) ? (int)byteInWindow : null);

        long holder = byteInWindow == 1 ? length.Value - ExclusiveLengthBase : 0;
        return new JetLockAddress(region, withinRegion, page, byteInWindow, IsUser(holder) ? (int)holder : null);
    }

    private static bool IsUser(long user) => user is >= 1 and <= MaxUserNumber;

    private static void ThrowIfNotUser(int user)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(user, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(user, MaxUserNumber);
    }
}

/// <summary>A lock offset split into its fields by <see cref="JetLockLayout.Decode"/>.</summary>
/// <param name="Region">The region, from the top nibble. Not necessarily a named <see cref="JetLockRegion"/>.</param>
/// <param name="WithinRegion">The offset with the region removed.</param>
/// <param name="Window">The page; in the <see cref="JetLockRegion.User"/> region, the 256-byte group.</param>
/// <param name="ByteInWindow">The byte within the page's window; in the <see cref="JetLockRegion.User"/> region,
/// within the group.</param>
/// <param name="User">The user the lock names: for a one-byte lock on a page, the user whose byte it is; for an
/// exclusive page lock, the user its end names; in the <see cref="JetLockRegion.User"/> region, for a one-byte (or
/// unknown-length) lock in <see cref="JetLockLayout.UserGroup"/>, <see cref="JetLockLayout.SessionGroup"/>,
/// <see cref="JetLockLayout.ConnectionControlGroup"/> or <see cref="JetLockLayout.ConnectionControlFollowUpGroup"/>,
/// that user. Null for any other shape.</param>
internal readonly record struct JetLockAddress(
    JetLockRegion Region, long WithinRegion, long Window, long ByteInWindow, int? User);
