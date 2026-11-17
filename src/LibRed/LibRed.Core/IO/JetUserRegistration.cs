using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibRed.IO;

/// <summary>
/// One connection registered as a Jet user of one database: its lock file held open, its group-0 user byte and
/// group-1 session byte locked, and its record written — what ACE does on a shared or read-only open, so that Access
/// sees the connection in the file (<c>docs/format/locking.md</c> §1, §3). Disposing deregisters it, and deletes the
/// lock file when nobody else is connected.
/// </summary>
/// <remarks>
/// <para>One per <see cref="PageChannel"/>, as each ACE connection is a user of its own, with its own handle on the
/// lock file, its own number, record and commit slot — two connections in one process included (measured). The
/// byte-range locks belong to the handle, and Windows refuses a range another handle of the same process holds as it
/// refuses another process's, so two channels of one process meet each other's locks exactly as two processes
/// do.</para>
/// <para>The commit slot its number names is written by <see cref="PageChannel"/> around each batch, and watched by
/// <see cref="PageCache"/>. The page write locks, the long-value locks and the table locks are taken here, on the
/// connection's handle (<see cref="Lock"/>), on behalf of whatever of the connection holds them: its statement, its
/// transaction, a result being read.</para>
/// </remarks>
internal sealed class JetUserRegistration : IDisposable
{
    /// <summary>The security name in the record. LibRed has no user-level security, and ACE opening a database
    /// without a workgroup file writes this one (measured).</summary>
    public const string SecurityName = "Admin";

    /// <summary>ACE's error 3218, for a write refused a page lock once the retries are spent (measured,
    /// locking.md §7).</summary>
    public const string CurrentlyLockedMessage = "Could not update; currently locked.";

    /// <summary>ACE's error 3624, for a read of a chained long value refused its shared byte on the chain's first page
    /// once the retries are spent (measured, locking.md §7).</summary>
    public const string CouldNotReadMessage = "Could not read the record; currently locked by another user.";

    /// <summary>ACE's error 3202, for an <c>UPDATE</c> replacing a chained long value refused the exclusive lock on the
    /// old chain's first page once the retries are spent (measured, locking.md §7).</summary>
    public const string CouldNotSaveMessage = "Could not save; currently locked by another user.";

    // ACE's retry of a refused lock: 21 attempts in all, about 108 ms apart (measured, locking.md §7); a long-value read
    // makes three such rounds back to back, 63 attempts (measured). The delay is the sleep between attempts; ACE's
    // measured spacing includes its own work between them.
    // A table's read byte refused — another user changing its design — is retried two rounds, about 4.4 s (measured).
    private const int LockAttempts = 21;
    private const int LongValueReadAttempts = 3 * LockAttempts;
    private const int TableReadAttempts = 2 * LockAttempts;
    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly JetLockFile _lockFile;
    private readonly string _lockFilePath;

    // The locks this connection holds, by region and page, with how many holders of each kind — its transaction with
    // a page in its set of changed pages or a table it wrote, its statement, a result being read. The OS lock is
    // taken when a count leaves zero and released when it returns to it: the connection never waits for itself, and
    // every other user — another channel of this process included — waits for it. Under a lock of its own, never
    // held across a retry's sleep: a result can let go from another thread.
    private readonly Dictionary<(JetLockRegion Region, int Page), Holders> _held = [];
    private readonly Lock _heldGate = new();

    private JetUserRegistration(JetLockFile lockFile, string lockFilePath, int user)
    {
        _lockFile = lockFile;
        _lockFilePath = lockFilePath;
        User = user;
    }

    /// <summary>The user number taken: the index of the record, and of the bytes held.</summary>
    public int User { get; }

    /// <summary>Registers as a user of <paramref name="databasePath"/>, or returns null — and changes nothing a later
    /// open would trip over — where it cannot: off Windows, for an extension Jet/ACE does not lock, when the lock
    /// file cannot be created or opened (a read-only directory, say), and when every user number is taken. What ACE
    /// does in the last two cases is not measured; this continues unregistered rather than refusing the open.</summary>
    public static JetUserRegistration? Connect(string databasePath)
    {
        if (!OperatingSystem.IsWindows()) return null;
        string? lockFilePath = JetLockFile.PathFor(databasePath);
        if (lockFilePath is null) return null;

        JetLockFile lockFile;
        try { lockFile = JetLockFile.Open(lockFilePath, createIfMissing: true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return null; }

        try
        {
            if (TakeUserNumber(lockFile) is not { } user)
            {
                lockFile.Dispose();
                return null;
            }

            // ACE writes its record over whatever the number's last user left (measured). One that already holds
            // exactly these bytes — this machine's last user of the number, as every connection a process opens after
            // its first finds — is left unwritten: the file is the same either way, and the write is most of what
            // registering costs (about 0.6 ms of 0.8, measured), a connection being a user of its own.
            byte[] record = JetLockLayout.UserRecord(Environment.MachineName, SecurityName);
            long offset = JetLockLayout.UserRecordOffset(user);
            Span<byte> present = stackalloc byte[JetLockLayout.UserRecordSize];
            if (lockFile.Read(offset, present) != record.Length || !present.SequenceEqual(record))
                lockFile.Write(offset, record);

            long connect = JetLockLayout.UserGroupStart(JetLockLayout.ConnectGroup);
            if (lockFile.TryLock(connect, JetLockLayout.UserGroupSize))
                lockFile.Unlock(connect, JetLockLayout.UserGroupSize);

            return new JetUserRegistration(lockFile, lockFilePath, user);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            lockFile.Dispose();   // releases whatever it took
            return null;
        }
    }

    /// <summary>ACE's handshake: the whole of group 0 first, and alone, user 1; otherwise the whole of group 1, then
    /// each number from 1 in turn — its session byte, then its user byte — until both are taken.</summary>
    private static int? TakeUserNumber(JetLockFile lockFile)
    {
        long users = JetLockLayout.UserByte(1);
        long sessions = JetLockLayout.SessionByte(1);
        const int all = JetLockLayout.MaxUserNumber;

        if (lockFile.TryLock(users, all))
        {
            lockFile.Unlock(users, all);
            if (TryTake(lockFile, 1)) return 1;
        }
        else if (lockFile.TryLock(sessions, all))
        {
            // Not measured: ACE was only seen refused here, with another user connected.
            lockFile.Unlock(sessions, all);
        }

        for (int user = 1; user <= all; user++)
            if (TryTake(lockFile, user)) return user;
        return null;
    }

    // Both bytes are tried even when the first is refused, as ACE tries them; a number is taken only with both.
    private static bool TryTake(JetLockFile lockFile, int user)
    {
        bool session = lockFile.TryLock(JetLockLayout.SessionByte(user), 1);
        bool connected = lockFile.TryLock(JetLockLayout.UserByte(user), 1);
        if (session && connected) return true;
        if (session) lockFile.Unlock(JetLockLayout.SessionByte(user), 1);
        if (connected) lockFile.Unlock(JetLockLayout.UserByte(user), 1);
        return false;
    }

    /// <summary>ACE's error 3211, for a table lock refused once the retries are spent (measured for <c>ALTER
    /// TABLE</c> and <c>CREATE INDEX</c>, locking.md §7).</summary>
    public static string TableInUseMessage(string table) =>
        $"The database engine could not lock table '{table}' because it is already in use by another person or process.";

    /// <summary>ACE's error 3008, for a table's read byte refused once the retries are spent: another user holds the
    /// table for a design change (measured: a <c>SELECT</c> beside another connection's <c>ALTER TABLE</c> in an open
    /// transaction, locking.md §7).</summary>
    public static string TableOpenedExclusivelyMessage(string table) =>
        $"The table '{table}' is already opened exclusively by another user, or it is already open through the user interface and cannot be manipulated programmatically.";

    /// <summary>Takes this user's lock on <paramref name="page"/>'s window in <paramref name="region"/> for one more
    /// holder in the connection, for <paramref name="purpose"/> (locking.md §2, §4): <paramref name="exclusive"/>, or
    /// the shared byte. A lock another user holds refuses it, and it is retried as ACE retries it — 21 attempts, 100 ms
    /// apart; 63 for a long-value read, 42 for a table's read byte — and then refused with ACE's outcome for the
    /// purpose: an <see cref="InvalidOperationException"/> with 3218 for a page write, 3624 for a long-value read, 3202
    /// for an <c>UPDATE</c> replacing a chained long value, for a table 3211 naming <paramref name="table"/> — 3008
    /// when it is its read byte that is refused; for a <c>DELETE</c> removing a chained long value, no exception — the
    /// caller goes ahead without the lock.</summary>
    /// <returns>True when this call took an operating-system lock; false when the connection already held the lock and
    /// the caller has only joined it — it has then been held throughout, and no other user can have taken it
    /// meanwhile; null when <see cref="JetLockPurpose.LongValueRemove"/> was refused, and nothing is held.</returns>
    /// <remarks>
    /// <para>Pair every call that returns true or false with one <see cref="Unlock"/> of the same lock.</para>
    /// <para>Windows refuses an exclusive range overlapping a byte the same handle holds. So the connection's own
    /// shared and exclusive holders of one window never stand in each other's way: a shared holder joins an exclusive
    /// lock the connection holds, and an exclusive lock taken while the connection holds its shared byte is ACE's
    /// upgrade — the other users' bytes (<see cref="JetLockLayout.UpgradeRanges"/>) on top of its own. In the
    /// table-read region, where both kinds are taken, an exclusive lock is always taken that way — its own byte, then
    /// the upgrade — so a shared holder arriving or leaving while it is held never takes or gives back the byte under
    /// it. To every other user's lock it conflicts as ACE's single range does: all their bytes are covered. Which of
    /// its own holders refuse a connection's design change, as ACE refuses it, is <see cref="PageChannel.LockTable"/>'s
    /// to say.</para>
    /// </remarks>
    public bool? Lock(JetLockRegion region, int page, bool exclusive, JetLockPurpose purpose, string? table = null)
    {
        bool tableRead = purpose == JetLockPurpose.Table && region == JetLockRegion.TableRead && !exclusive;
        int attempts = purpose == JetLockPurpose.LongValueRead ? LongValueReadAttempts : tableRead ? TableReadAttempts : LockAttempts;
        for (int attempt = 1; ; attempt++)
        {
            lock (_heldGate)
            {
                ref Holders held = ref CollectionsMarshal.GetValueRefOrAddDefault(_held, (region, page), out _);
                bool? took = exclusive ? TryHoldExclusive(ref held, region, page) : TryHoldShared(ref held, region, page);
                if (took is { } taken) return taken;
                if (held is { Shared: 0, Exclusive: 0 }) _held.Remove((region, page));
            }

            if (attempt == attempts)
            {
                return purpose switch
                {
                    JetLockPurpose.LongValueRemove => null,
                    JetLockPurpose.Table when tableRead => throw new InvalidOperationException(
                        TableOpenedExclusivelyMessage(table ?? throw new ArgumentNullException(nameof(table)))),
                    JetLockPurpose.Table => throw new InvalidOperationException(
                        TableInUseMessage(table ?? throw new ArgumentNullException(nameof(table)))),
                    JetLockPurpose.LongValueRead => throw new InvalidOperationException(CouldNotReadMessage),
                    JetLockPurpose.LongValueReplace => throw new InvalidOperationException(CouldNotSaveMessage),
                    _ => throw new InvalidOperationException(CurrentlyLockedMessage),
                };
            }
            Thread.Sleep(LockRetryDelay);
        }
    }

    /// <summary>Lets go of a lock <see cref="Lock"/> took or joined, for one holder; the last to let go releases the
    /// operating-system lock.</summary>
    public void Unlock(JetLockRegion region, int page, bool exclusive)
    {
        lock (_heldGate)
        {
            ref Holders held = ref CollectionsMarshal.GetValueRefOrNullRef(_held, (region, page));
            if (Unsafe.IsNullRef(ref held) || (exclusive ? held.Exclusive : held.Shared) == 0)
                throw new InvalidOperationException($"No {(exclusive ? "exclusive" : "shared")} region-{(int)region} lock is held on page {page}.");

            if (exclusive && --held.Exclusive == 0)
            {
                if (held.ByteHeld)
                {
                    UnlockUpgrade(region, page);
                    if (held.Shared == 0) UnlockByte(ref held, region, page);
                }
                else
                {
                    (long offset, long length) = JetLockLayout.ExclusiveRange(region, page, User);
                    _lockFile.Unlock(offset, length);
                    // Shared holders that joined the single range — a long-value read of a page the connection was
                    // writing — have no byte of their own under it, and take it now.
                    if (held.Shared > 0) held.ByteHeld = _lockFile.TryLock(JetLockLayout.SharedByte(region, page, User), 1);
                }
            }
            else if (!exclusive && --held.Shared == 0 && held is { Exclusive: 0, ByteHeld: true })
            {
                UnlockByte(ref held, region, page);
            }

            if (held is { Shared: 0, Exclusive: 0 }) _held.Remove((region, page));
        }
    }

    private bool? TryHoldShared(ref Holders held, JetLockRegion region, int page)
    {
        if (held.Shared > 0 || held.Exclusive > 0)
        {
            held.Shared++;
            return false;
        }
        if (!_lockFile.TryLock(JetLockLayout.SharedByte(region, page, User), 1)) return null;
        held.Shared = 1;
        held.ByteHeld = true;
        return true;
    }

    private bool? TryHoldExclusive(ref Holders held, JetLockRegion region, int page)
    {
        if (held.Exclusive > 0)
        {
            held.Exclusive++;
            return false;
        }

        if (!held.ByteHeld && region != JetLockRegion.TableRead)
        {
            (long offset, long length) = JetLockLayout.ExclusiveRange(region, page, User);
            if (!_lockFile.TryLock(offset, length)) return null;
            held.Exclusive = 1;
            return true;
        }

        bool tookByte = !held.ByteHeld;
        if (tookByte && !_lockFile.TryLock(JetLockLayout.SharedByte(region, page, User), 1)) return null;
        var (below, above) = JetLockLayout.UpgradeRanges(region, page, User);
        bool upgraded = below is not { } low || _lockFile.TryLock(low.Offset, low.Length);
        if (upgraded && !_lockFile.TryLock(above.Offset, above.Length))
        {
            if (below is { } taken) _lockFile.Unlock(taken.Offset, taken.Length);
            upgraded = false;
        }
        if (!upgraded)
        {
            if (tookByte) _lockFile.Unlock(JetLockLayout.SharedByte(region, page, User), 1);
            return null;
        }
        held.ByteHeld = true;
        held.Exclusive = 1;
        return true;
    }

    private void UnlockUpgrade(JetLockRegion region, int page)
    {
        var (below, above) = JetLockLayout.UpgradeRanges(region, page, User);
        _lockFile.Unlock(above.Offset, above.Length);
        if (below is { } low) _lockFile.Unlock(low.Offset, low.Length);
    }

    private void UnlockByte(ref Holders held, JetLockRegion region, int page)
    {
        _lockFile.Unlock(JetLockLayout.SharedByte(region, page, User), 1);
        held.ByteHeld = false;
    }

    // How many holders in the connection have a lock, by kind, and whether it holds its own shared byte under them:
    // with shared holders, and under an exclusive lock taken as an upgrade.
    private struct Holders
    {
        public int Shared;
        public int Exclusive;
        public bool ByteHeld;
    }

    /// <summary>Deregisters as ACE does at close: tries every other user's group-0 byte and lets them go, releases
    /// its own session and user bytes, closes the lock file, and deletes it when no other user was found. Never
    /// throws: a delete another process's open handle refuses is the ordinary case of someone having connected
    /// meanwhile, and a failed lock call leaves the file in place rather than risk deleting a live user's.</summary>
    public void Dispose()
    {
        bool alone = false;
        try
        {
            alone = OthersAbsent();
            _lockFile.Unlock(JetLockLayout.SessionByte(User), 1);
            _lockFile.Unlock(JetLockLayout.UserByte(User), 1);
        }
        catch (IOException)
        {
            alone = false;   // closing the handle below releases whatever is still held
        }
        finally
        {
            _lockFile.Dispose();
        }

        if (!alone) return;
        try { File.Delete(_lockFilePath); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* another user has it open */ }
    }

    // The group-0 bytes below and above this user's own, each as one range, taken and released at once.
    private bool OthersAbsent()
    {
        bool absent = true;
        if (User > 1) absent &= Probe(JetLockLayout.UserByte(1), User - 1);
        if (User < JetLockLayout.MaxUserNumber) absent &= Probe(JetLockLayout.UserByte(User + 1), JetLockLayout.MaxUserNumber - User);
        return absent;
    }

    private bool Probe(long offset, long length)
    {
        if (!_lockFile.TryLock(offset, length)) return false;
        _lockFile.Unlock(offset, length);
        return true;
    }
}

/// <summary>What a lock is taken for, which decides how long ACE retries it when refused and what the refusal ends in
/// (<see cref="JetUserRegistration.Lock"/>; locking.md §7).</summary>
internal enum JetLockPurpose
{
    /// <summary>A page about to be written: region 2, exclusive; refused with 3218.</summary>
    PageWrite,

    /// <summary>A table read, write or design lock: regions 4–6; refused with 3211 naming the table.</summary>
    Table,

    /// <summary>A chained long value read: region 2, the shared byte on the chain's first page; refused with 3624 after
    /// three rounds of retries.</summary>
    LongValueRead,

    /// <summary>A chained long value an <c>UPDATE</c> replaces: region 2, exclusive on the old chain's first page;
    /// refused with 3202.</summary>
    LongValueReplace,

    /// <summary>A chained long value a <c>DELETE</c> removes: region 2, exclusive on the chain's first page; once
    /// refused, the row is deleted without it and the chain is left where it is.</summary>
    LongValueRemove,
}
