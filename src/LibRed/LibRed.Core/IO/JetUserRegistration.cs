using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibRed.IO;

/// <summary>
/// This process registered as a Jet user of one database: its lock file held open, its group-0 user byte and group-1
/// session byte locked, and its record written — what ACE does on a shared or read-only open, so that Access sees the
/// process in the file (<c>docs/format/locking.md</c> §1, §3). Disposing deregisters it, and deletes the lock file
/// when nobody else is connected.
/// </summary>
/// <remarks>
/// <para>One per file per process: <see cref="PageCache"/> holds it, so every channel on the file shares one user
/// number, and the byte-range locks — which belong to a handle — live on one handle for as long as any channel is
/// open.</para>
/// <para>The commit slot its number names is written by <see cref="PageChannel"/> around each batch, and watched by
/// <see cref="PageCache"/>. The page write locks and the table locks are taken here, on the one handle
/// (<see cref="Lock"/>): held for the process, on behalf of every channel that holds them.</para>
/// </remarks>
internal sealed class JetUserRegistration : IDisposable
{
    /// <summary>The security name in the record. LibRed has no user-level security, and ACE opening a database
    /// without a workgroup file writes this one (measured).</summary>
    public const string SecurityName = "Admin";

    /// <summary>ACE's error 3218, for a write refused a page lock once the retries are spent (measured,
    /// locking.md §7).</summary>
    public const string CurrentlyLockedMessage = "Could not update; currently locked.";

    // ACE's retry of a refused page lock: 21 attempts in all, about 108 ms apart (measured, locking.md §7). The delay
    // is the sleep between attempts; ACE's measured spacing includes its own work between them.
    private const int LockAttempts = 21;
    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly JetLockFile _lockFile;
    private readonly string _lockFilePath;

    // The locks this process holds, by region and page, with how many holders of each kind — channels with a page in a
    // set of changed pages, channels with a table in use or under a design change. The OS lock is taken when a count
    // leaves zero and released when it returns to it: the channels of one process are one user and stay optimistic
    // among themselves (PageChannel's baseline check at commit, its publish gate), so only another user waits. Under a
    // lock of its own, never held across a retry's sleep.
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

            lockFile.Write(JetLockLayout.UserRecordOffset(user),
                JetLockLayout.UserRecord(Environment.MachineName, SecurityName));

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

    /// <summary>Takes this user's lock on <paramref name="page"/>'s window in <paramref name="region"/> for one more
    /// holder in the process — a page write lock (region 2) or a table lock (regions 4–6, on the table's definition
    /// page; locking.md §2, §4): <paramref name="exclusive"/>, or the shared byte. A lock another user holds refuses it,
    /// and it is retried as ACE retries it: 21 attempts, 100 ms apart, and then <see cref="InvalidOperationException"/>
    /// with ACE's message — 3218 for a page, 3211 naming <paramref name="table"/> for a table.</summary>
    /// <returns>True when this call took an operating-system lock; false when the process already held the lock and
    /// the caller has only joined it — it has then been held throughout, and no other user can have taken it
    /// meanwhile.</returns>
    /// <remarks>
    /// <para>Pair every call that returns, either way, with one <see cref="Unlock"/> of the same lock.</para>
    /// <para>One process is one user, and Windows refuses an exclusive range overlapping a byte the same handle holds.
    /// So the process's shared and exclusive holders of one window never stand in each other's way: a shared holder
    /// joins an exclusive lock the process holds, and an exclusive lock taken while the process holds its shared byte
    /// is ACE's upgrade — the other users' bytes (<see cref="JetLockLayout.UpgradeRanges"/>) on top of its own. In the
    /// table-read region, where both kinds are taken, an exclusive lock is always taken that way — its own byte, then
    /// the upgrade — so a shared holder arriving or leaving while it is held never takes or gives back the byte under
    /// it. To every other user's lock it conflicts as ACE's single range does: all their bytes are covered.</para>
    /// </remarks>
    public bool Lock(JetLockRegion region, int page, bool exclusive, string? table = null)
    {
        for (int attempt = 1; ; attempt++)
        {
            lock (_heldGate)
            {
                ref Holders held = ref CollectionsMarshal.GetValueRefOrAddDefault(_held, (region, page), out _);
                bool? took = exclusive ? TryHoldExclusive(ref held, region, page) : TryHoldShared(ref held, region, page);
                if (took is { } taken) return taken;
                if (held is { Shared: 0, Exclusive: 0 }) _held.Remove((region, page));
            }

            if (attempt == LockAttempts)
                throw new InvalidOperationException(region == JetLockRegion.WriteRead
                    ? CurrentlyLockedMessage
                    : TableInUseMessage(table ?? throw new ArgumentNullException(nameof(table))));
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
                    // Shared holders that joined the single range have no byte of their own under it. LibRed takes
                    // that range only where it takes no shared byte, so this is the ordinary case of none.
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

    // How many holders in the process have a lock, by kind, and whether the process holds its own shared byte under
    // them: with shared holders, and under an exclusive lock taken as an upgrade.
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
