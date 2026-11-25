using LibRed.Formats;
using LibRed.IO;

namespace JetLockTrace;

/// <summary>
///     Decodes the extended byte-range lock offsets Jet/ACE places on the lock file, and the page/field targets of
///     reads and writes against the database file.
/// </summary>
/// <remarks>
///     <para>
///         The layout itself — regions, page windows, user bytes — is <see cref="JetLockLayout" /> in LibRed.Core,
///         the same copy the engine locks through; this class only words what it decodes. Region names come from the
///         Microsoft Jet locking white paper (<c>docs/JetWhitePapers_UPDATE1/Jetlock.docx</c>) and are the Jet
///         development team's own.
///     </para>
/// </remarks>
public static class LockDecoder
{
    /// <summary>
    ///     The page-0 layout the commit-byte table is read from. A trace carries no database header, so this is the
    ///     Jet 4 layout every ACE version shares.
    /// </summary>
    private static readonly JetFormatBase PageZeroFormat = JetFormatBase.FromVersionByte((byte)JetVersion.Version4);

    private static string RegionName(JetLockRegion region)
        => region switch
        {
            JetLockRegion.User => "user-lock",
            // Jet 3.0 moved read locks into the write-lock range; Jet's "shared" one is a single byte there.
            JetLockRegion.WriteRead => "write/read",
            JetLockRegion.ReadCommit => "read/commit",
            JetLockRegion.TableRead => "table-read",
            JetLockRegion.TableWrite => "table-write",
            JetLockRegion.TableDenyWrite => "deny-write",
            _ => $"region-{(int)region:X}",
        };

    /// <summary>Describes a lock or unlock at <paramref name="offset" /> spanning <paramref name="length" /> bytes.</summary>
    /// <param name="offset">The lock offset.</param>
    /// <param name="length">The lock length, if known.</param>
    /// <param name="exclusive">
    ///     The <c>Exclusive</c> flag ProcMon recorded, when it recorded one (unlock rows carry none). ACE takes only
    ///     operating-system exclusive locks, so <c>false</c> is the one thing marked as unexpected.
    /// </param>
    public static string DescribeLock(long offset, long? length, bool? exclusive = null)
    {
        JetLockAddress a = JetLockLayout.Decode(offset, length);
        string kind = RegionName(a.Region);
        string size = $"({length?.ToString() ?? "?"})";
        string where = $"[0x{a.WithinRegion:X}]";
        string mark = exclusive == false ? "  !! OS-SHARED lock: ACE takes none" : "";

        // Region 1 is not page-based: groups of 256 bytes, one byte per user. A connected user holds its byte in
        // group 0 and its byte in group 1 (one lower) for its whole session. Group 5 is connection control: a user
        // barring new connections holds its own byte there, and every user connecting tries the whole group; group 6
        // is tried only by a user group 5 refused, and what it is for is not known. A range over several bytes of
        // group 0 or 1 is a presence probe or the first attempt on open: it succeeds only when nobody else holds a byte
        // in it.
        if (a.Region == JetLockRegion.User)
        {
            long group = a.Window;
            long slot = a.ByteInWindow;
            string text = (group, length, a.User) switch
            {
                (JetLockLayout.UserGroup, _, { } user) => $"group 0  user {user} byte",
                (JetLockLayout.SessionGroup, _, { } user) => $"group 1  session byte, user {user}",
                (JetLockLayout.UserGroup or JetLockLayout.SessionGroup, > 1, _)
                    => $"group {group}  bytes {slot}..{slot + (length ?? 1) - 1}: presence probe over several users' bytes",
                (JetLockLayout.ConnectionControlGroup, JetLockLayout.UserGroupSize, _) when slot == 0
                    => "group 5  connection control, whole group: is anyone barring new connections?",
                (JetLockLayout.ConnectionControlGroup, _, { } user)
                    => $"group 5  connection control, user {user} byte (held while user {user} bars new connections)",
                (JetLockLayout.ConnectionControlGroup, _, _)
                    => $"group 5  connection control, bytes {slot}..{slot + (length ?? 1) - 1}",
                (JetLockLayout.ConnectionControlFollowUpGroup, _, { } user)
                    => $"group 6  user {user} byte (tried after group 5 and group 6 byte 0 refused; meaning unknown)",
                (JetLockLayout.ConnectionControlFollowUpGroup, 1, _) when slot == 0
                    => "group 6  byte 0 (tried after group 5 refused; meaning unknown)",
                _ => $"group {group}  byte {slot}",
            };

            return $"{kind,-11} {text} {size}  {where}{mark}";
        }

        // A one-byte lock on a page is Jet's "shared" lock, at the holder's own user number.
        if (length == 1 && a.User is { } sharer)
        {
            return $"{kind,-11} page {a.Window,-6} shared, user {sharer,-3} {size}  {where}{mark}";
        }

        // An exclusive lock starts at byte 1 and covers every user's shared byte, then runs on to the holder's own
        // number, so the USER is identified by where the range ends, not where it starts.
        if (a.User is { } holder)
        {
            return $"{kind,-11} page {a.Window,-6} exclusive, user {holder,-3} {size}  {where}{mark}";
        }

        // An upgrade: a user already holding its own byte takes the rest of the shared bytes, 2..256.
        if (length is > 1 && a.ByteInWindow >= 2 && a.ByteInWindow + length.Value - 1 == JetLockLayout.ExclusiveLengthBase)
        {
            return $"{kind,-11} page {a.Window,-6} upgrade, bytes {a.ByteInWindow}..{JetLockLayout.ExclusiveLengthBase}, every other user's byte {size}  {where}{mark}";
        }

        return $"{kind,-11} page {a.Window,-6} +{a.ByteInWindow,-5} {size}  {where}{mark}";
    }

    /// <summary>Describes a read or write against the database file.</summary>
    public static string DescribeDatabaseIo(long offset, long? length, int pageSize)
    {
        long page = offset / pageSize;
        long inPage = offset % pageSize;

        if (page == 0 && inPage >= PageZeroFormat.CommitByteTableOffset)
        {
            long slot = (inPage - PageZeroFormat.CommitByteTableOffset) / PageZeroFormat.CommitByteSlotSize;
            return $"commit-byte user {slot}   (page 0 +0x{inPage:X3}, {length} bytes)";
        }

        if (inPage == 0 && length == pageSize)
        {
            return $"page {page,-6} (full)";
        }

        // A single read can span many pages — Access pulls 64 KB at once when opening a database.
        if (inPage == 0 && length > pageSize)
        {
            long lastPage = page + ((length.Value + pageSize - 1) / pageSize) - 1;
            return $"pages {page}..{lastPage}   ({length} bytes)";
        }

        return $"page {page,-6} +{inPage,-5} ({length} bytes)";
    }

    /// <summary>Describes a read or write against the lock file.</summary>
    /// <remarks>User <c>n</c>'s record is the <c>n - 1</c>th, so user 1's is at offset 0.</remarks>
    public static string DescribeLockFileIo(long offset, long? length)
        => length == JetLockLayout.UserRecordSize && JetLockLayout.UserAtRecordOffset(offset) is { } user
            ? $"user {user} record   (computer + security name)"
            : $"offset {offset} ({length} bytes)";

    /// <summary>Whether <paramref name="path" /> names a lock file rather than a database.</summary>
    public static bool IsLockFile(string path)
        => path.EndsWith(JetLockFile.AceExtension, StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(JetLockFile.JetExtension, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether <paramref name="path" /> names a database or its lock file.</summary>
    public static bool IsDatabaseOrLockFile(string path)
        => IsLockFile(path) || JetLockFile.PathFor(path) is not null;
}
