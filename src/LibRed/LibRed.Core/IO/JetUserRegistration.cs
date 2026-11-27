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
    /// once the retries are spent, and for a child row's foreign-key check refused its shared byte on the parent's index
    /// page (measured, locking.md §7).</summary>
    public const string CouldNotReadMessage = "Could not read the record; currently locked by another user.";

    /// <summary>ACE's error 3202, for an <c>UPDATE</c> replacing a chained long value refused the exclusive lock on the
    /// old chain's first page once the retries are spent (measured, locking.md §7).</summary>
    public const string CouldNotSaveMessage = "Could not save; currently locked by another user.";

    // ACE's retry of a refused lock: 21 attempts in all, about 108 ms apart (measured, locking.md §7); a long-value read
    // makes three such rounds back to back, 63 attempts (measured). The delay is the sleep between attempts; ACE's
    // measured spacing includes its own work between them.
    // A table's read byte refused — another user changing its design — is retried two rounds, about 4.4 s, when the
    // statement opens the table for reading (a SELECT, an UPDATE, a DELETE), and one when it opens it for writing (an
    // INSERT) (measured: 42 attempts and 21).
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

    // The users found connected when this one connected — the numbers below its own whose group-0 byte its search for
    // a number was refused — whose group-5 bytes its connection-control check tries (PassConnectionControl).
    private readonly bool[] _connected;

    // How many times this connection has barred new connections while holding its group-5 byte, less the times it has
    // admitted them since: the byte is held while this is above zero (SetConnectionControl).
    private int _bars;

    private JetUserRegistration(JetLockFile lockFile, string lockFilePath, int user, bool[] connected)
    {
        _lockFile = lockFile;
        _lockFilePath = lockFilePath;
        User = user;
        _connected = connected;
    }

    /// <summary>The user number taken: the index of the record, and of the bytes held.</summary>
    public int User { get; }

    /// <summary>Registers as a user of <paramref name="databasePath"/>, or returns null — and changes nothing a later
    /// open would trip over — where it cannot: off Windows, for an extension Jet/ACE does not lock, when the lock
    /// file cannot be created or opened (a read-only directory, say), and when every user number is taken. What ACE
    /// does in the last two cases is not measured; this continues unregistered rather than refusing the open.</summary>
    /// <exception cref="IOException">Another user has barred new connections (connection control,
    /// <see cref="PassConnectionControl"/>): ACE's 3734 naming that user from its record, or 3733 when no connected
    /// user is found holding the bar. The open is refused, with this connection's bytes let go first.</exception>
    public static JetUserRegistration? Connect(string databasePath)
    {
        if (!OperatingSystem.IsWindows()) return null;
        string? lockFilePath = JetLockFile.PathFor(databasePath);
        if (lockFilePath is null) return null;

        JetLockFile lockFile;
        try { lockFile = JetLockFile.Open(lockFilePath, createIfMissing: true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return null; }

        string refusal;
        try
        {
            // The numbers below the one taken whose group-0 byte was refused on the way: the users found connected.
            bool[] connected = new bool[JetLockLayout.MaxUserNumber + 1];
            if (TakeUserNumber(lockFile, connected) is not { } user)
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

            string? refused;
            lock (ConnectionControlGate) refused = PassConnectionControl(lockFile, user, connected, bar: false);
            if (refused is null)
                return new JetUserRegistration(lockFile, lockFilePath, user, connected);

            // Refused: ACE lets its session byte go, then its user byte, and leaves its record (measured).
            lockFile.Unlock(JetLockLayout.SessionByte(user), 1);
            lockFile.Unlock(JetLockLayout.UserByte(user), 1);
            refusal = refused;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            lockFile.Dispose();   // releases whatever it took
            return null;
        }

        // The lock file is not deleted: whoever holds the bar holds it through a handle of its own, which refuses the
        // delete.
        lockFile.Dispose();
        throw new IOException(refusal);
    }

    /// <summary>ACE's handshake: the whole of group 0 first, and alone, user 1; otherwise the whole of group 1, then
    /// each number from 1 in turn — its session byte, then its user byte — until both are taken. Each number whose
    /// user byte was refused on the way is marked in <paramref name="connected"/>.</summary>
    private static int? TakeUserNumber(JetLockFile lockFile, Span<bool> connected)
    {
        long users = JetLockLayout.UserByte(1);
        long sessions = JetLockLayout.SessionByte(1);
        const int all = JetLockLayout.MaxUserNumber;

        if (lockFile.TryLock(users, all))
        {
            lockFile.Unlock(users, all);
            if (TryTake(lockFile, 1, connected)) return 1;
        }
        else if (lockFile.TryLock(sessions, all))
        {
            // Not measured: ACE was only seen refused here, with another user connected.
            lockFile.Unlock(sessions, all);
        }

        for (int user = 1; user <= all; user++)
            if (TryTake(lockFile, user, connected)) return user;
        return null;
    }

    // Both bytes are tried even when the first is refused, as ACE tries them; a number is taken only with both.
    private static bool TryTake(JetLockFile lockFile, int user, Span<bool> connected)
    {
        bool session = lockFile.TryLock(JetLockLayout.SessionByte(user), 1);
        bool userByte = lockFile.TryLock(JetLockLayout.UserByte(user), 1);
        if (session && userByte) return true;
        if (session) lockFile.Unlock(JetLockLayout.SessionByte(user), 1);
        if (userByte) lockFile.Unlock(JetLockLayout.UserByte(user), 1);
        else connected[user] = true;
        return false;
    }

    // A connecting user's tries of connection control, in all, before it gives up (measured: 10).
    private const int ConnectionControlRounds = 10;

    // Every connection-control check of the process's connections, a connecting one's and a setter's, one at a time:
    // one's momentary hold of the whole of group 5 covers another's own byte there, and would read to it as a bar.
    private static readonly Lock ConnectionControlGate = new();

    /// <summary>ACE's connection-control check (locking.md §3), made by a user once it has its number and has written
    /// its record, and again by a user setting connection control — <paramref name="bar"/>: the whole of group 5
    /// granted — nobody bars new connections — is let go, the setter then takes its own byte of the group and holds it,
    /// and null is returned: the user is connected, or bars new connections. Refused, it tries byte 0 of group 6.
    /// Granted, that is let go and each other connected user's group-5 byte is tried in turn: the first refused names the user barring connections, and the refusal is
    /// ACE's 3734 with the names in that user's record; one granted is let go. Refused byte 0, the group-6 byte of
    /// each user number that has a record in the lock file is tried from the highest down to 1, the user's own
    /// included, each one granted being let go at once: a refused byte of another user is the refusal, ACE's 3734
    /// naming that user from its record, and a refused byte of the user's own ends the round naming no one. Either way,
    /// with no holder found, it starts again from the whole of group 5, ten rounds in all, and then the refusal is
    /// ACE's 3733, an unknown user. No round waits for the next. A newcomer refused fails its open with the refusal; a
    /// setter refused holds nothing and raises nothing (measured).</summary>
    /// <remarks>A setter's own byte refused just after the whole group was granted — another user's momentary try of
    /// it in between — runs the round again; ACE's handling of that race is not measured. A setter refused by no holder
    /// found makes the ten rounds too; how many ACE's makes is not measured — it holds nothing either way.
    /// Group 5's walk is measured as far as its first byte: with byte 0 of group 6 granted, the first byte
    /// tried is that of the lowest connected user, whether or not the newcomer is user 3 and other users are
    /// connected, and it was refused in every such case. The order beyond it with several users barring, whether the
    /// newcomer's own byte is tried, whether the walk covers every record or only the connected users, and which
    /// users a newcomer counts as connected when its own number is not the highest (here: those its search for a
    /// number was refused below its own), are not measured. Group 6's walk is measured with one to three records in
    /// the file.</remarks>
    private static string? PassConnectionControl(JetLockFile lockFile, int user, ReadOnlySpan<bool> connected, bool bar)
    {
        long control = JetLockLayout.UserGroupStart(JetLockLayout.ConnectionControlGroup);
        long followUp = JetLockLayout.UserGroupStart(JetLockLayout.ConnectionControlFollowUpGroup);

        for (int round = 0; round < ConnectionControlRounds; round++)
        {
            if (lockFile.TryLock(control, JetLockLayout.UserGroupSize))
            {
                lockFile.Unlock(control, JetLockLayout.UserGroupSize);
                if (!bar || lockFile.TryLock(JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, user), 1))
                    return null;
                continue;
            }

            if (lockFile.TryLock(followUp, 1))
            {
                lockFile.Unlock(followUp, 1);
                for (int other = 1; other < connected.Length; other++)
                {
                    if (!connected[other]) continue;
                    long barring = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, other);
                    if (!lockFile.TryLock(barring, 1)) return BarredByMessage(lockFile, other);
                    lockFile.Unlock(barring, 1);
                }
            }
            else
            {
                int records = (int)Math.Min(lockFile.Length / JetLockLayout.UserRecordSize, JetLockLayout.MaxUserNumber);
                for (int other = records; other >= 1; other--)
                {
                    long held = JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlFollowUpGroup, other);
                    if (lockFile.TryLock(held, 1))
                    {
                        lockFile.Unlock(held, 1);
                        continue;
                    }

                    if (other == user) break;
                    return BarredByMessage(lockFile, other);
                }
            }
        }

        return BarredByUnknownUserMessage;
    }

    // ACE's 3734, the names read from the barring user's record (measured: 'Admin' on machine 'DESKTOP-D1RBTPV' for a
    // record holding them).
    private static string BarredByMessage(JetLockFile lockFile, int holder)
    {
        Span<byte> record = stackalloc byte[JetLockLayout.UserRecordSize];
        int read = lockFile.Read(JetLockLayout.UserRecordOffset(holder), record);
        (string computerName, string securityName) = JetLockLayout.RecordNames(record[..read]);
        return BarredByUserMessage(securityName, computerName);
    }

    /// <summary>ACE's error 3734, for an open refused because the connected user named holds the database barred to
    /// new connections — connection control (measured, locking.md §7).</summary>
    public static string BarredByUserMessage(string securityName, string computerName) =>
        $"The database has been placed in a state by user '{securityName}' on machine '{computerName}' that prevents it from being opened or locked.";

    /// <summary>ACE's error 3733, for an open refused by connection control when no connected user is found holding
    /// it (measured, locking.md §7).</summary>
    public const string BarredByUnknownUserMessage =
        "The database has been placed in a state by an unknown user that prevents it from being opened or locked.";

    /// <summary>Bars new connections, or admits them again, as ACE's session property <c>Jet OLEDB:Connection
    /// Control</c> does set to 1 or 2 (measured, locking.md §3). Barring runs the connection-control check
    /// (<see cref="PassConnectionControl"/>): with nobody barring, this user takes its group-5 byte and holds it; with
    /// another user barring, it holds nothing and raises nothing. Barring again while the byte is held only counts,
    /// and the byte is let go by as many admits — or by <see cref="Dispose"/>; an admit while it holds nothing does
    /// nothing.</summary>
    /// <remarks>Not thread-safe, like the rest of a channel's state: one caller at a time.</remarks>
    public void SetConnectionControl(bool bar)
    {
        if (!bar)
        {
            if (_bars > 0 && --_bars == 0)
                _lockFile.Unlock(JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, User), 1);
            return;
        }

        if (_bars > 0)
        {
            _bars++;
            return;
        }

        lock (ConnectionControlGate)
            if (PassConnectionControl(_lockFile, User, _connected, bar: true) is null) _bars = 1;
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
    /// apart; 63 for a long-value read, 42 for the read byte of a table opened for reading — and then refused with
    /// ACE's outcome for the purpose: an <see cref="InvalidOperationException"/> with 3218 for a page write, 3624 for a
    /// long-value read and for a foreign key's referenced key, 3202 for an <c>UPDATE</c> replacing a chained long
    /// value, for a table 3211 naming <paramref name="table"/> — 3008 when it is its read byte that is refused; for a
    /// <c>DELETE</c> removing a chained long value, no exception — the caller goes ahead without the lock.</summary>
    /// <returns>True when this call took an operating-system lock; false when the connection already held the lock and
    /// the caller has only joined it — it has then been held throughout, and no other user can have taken it
    /// meanwhile; null when <see cref="JetLockPurpose.LongValueRemove"/> was refused, and nothing is held.</returns>
    /// <remarks>
    /// <para>Pair every call that returns true or false with one <see cref="Unlock"/> of the same lock.</para>
    /// <para>Windows refuses an exclusive range overlapping a byte the same handle holds. So the connection's own
    /// shared and exclusive holders of one window never stand in each other's way: a shared holder joins an exclusive
    /// lock the connection holds, and an exclusive lock taken while the connection holds its shared byte is ACE's
    /// upgrade — the other users' bytes (<see cref="JetLockLayout.UpgradeRanges"/>) on top of its own. An exclusive
    /// lock taken while the connection holds no byte of its own is ACE's single range, in every region, the table-read
    /// region included (a design change takes it so); a shared holder that joins it has no byte under it. In the
    /// table-read region the range then stays held, with no byte taken, until the last shared holder lets go — as ACE
    /// keeps a design lock while a reader of the connection outlives it (measured) — and an exclusive lock asked for
    /// meanwhile joins it. In the other regions, not measured, the shared holders take their byte as the range is let
    /// go. To every other user's lock either form conflicts as ACE's single range does:
    /// all their bytes are covered. Which of
    /// its own holders refuse a connection's design change, as ACE refuses it, is <see cref="PageChannel.LockTable"/>'s
    /// to say.</para>
    /// </remarks>
    public bool? Lock(JetLockRegion region, int page, bool exclusive, JetLockPurpose purpose, string? table = null)
    {
        bool tableRead = (purpose is JetLockPurpose.Table or JetLockPurpose.TableRead) && region == JetLockRegion.TableRead && !exclusive;
        int attempts = purpose switch
        {
            JetLockPurpose.LongValueRead => LongValueReadAttempts,
            JetLockPurpose.TableRead => TableReadAttempts,
            _ => LockAttempts,
        };
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
                    _ when tableRead => throw new InvalidOperationException(
                        TableOpenedExclusivelyMessage(table ?? throw new ArgumentNullException(nameof(table)))),
                    JetLockPurpose.Table => throw new InvalidOperationException(
                        TableInUseMessage(table ?? throw new ArgumentNullException(nameof(table)))),
                    JetLockPurpose.LongValueRead or JetLockPurpose.ReferencedKey => throw new InvalidOperationException(CouldNotReadMessage),
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
                else if (region != JetLockRegion.TableRead || held.Shared == 0)
                {
                    UnlockRange(ref held, region, page);
                    // Shared holders that joined the single range — a long-value read of a page the connection was
                    // writing — have no byte of their own under it, and take it now.
                    if (held.Shared > 0) held.ByteHeld = _lockFile.TryLock(JetLockLayout.SharedByte(region, page, User), 1);
                }
                // else: a table's design lock with readers of the connection that joined it. ACE keeps the whole range
                // and lets it go when the last of them closes (measured), taking no byte of its own.
            }
            else if (!exclusive && --held.Shared == 0 && held.Exclusive == 0)
            {
                if (held.ByteHeld) UnlockByte(ref held, region, page);
                else if (held.RangeHeld) UnlockRange(ref held, region, page);
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

        if (held.RangeHeld)
        {
            // The range is still held for readers that outlived the last exclusive holder: this one joins it, as
            // Windows refuses the same handle an overlapping range.
            held.Exclusive = 1;
            return false;
        }

        if (!held.ByteHeld)
        {
            (long offset, long length) = JetLockLayout.ExclusiveRange(region, page, User);
            if (!_lockFile.TryLock(offset, length)) return null;
            held.Exclusive = 1;
            held.RangeHeld = true;
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

    private void UnlockRange(ref Holders held, JetLockRegion region, int page)
    {
        (long offset, long length) = JetLockLayout.ExclusiveRange(region, page, User);
        _lockFile.Unlock(offset, length);
        held.RangeHeld = false;
    }

    // How many holders in the connection have a lock, by kind, and what it holds of the operating system's under them:
    // its own shared byte (with shared holders, and under an exclusive lock taken as an upgrade), or the single
    // exclusive range (under an exclusive lock taken as one, and — in the table-read region — under the shared holders
    // that outlast it).
    private struct Holders
    {
        public int Shared;
        public int Exclusive;
        public bool ByteHeld;
        public bool RangeHeld;
    }

    /// <summary>Deregisters as ACE does at close: tries every other user's group-0 byte and lets them go, releases
    /// its group-5 byte when it bars new connections, however many times it barred them, then its own session and user
    /// bytes, closes the lock file, and deletes it when no other user was found. Never
    /// throws: a delete another process's open handle refuses is the ordinary case of someone having connected
    /// meanwhile, and a failed lock call leaves the file in place rather than risk deleting a live user's.</summary>
    public void Dispose()
    {
        bool alone = false;
        try
        {
            alone = TryLockOthers();
            if (alone) UnlockOthers();
            if (_bars > 0)
            {
                _bars = 0;
                _lockFile.Unlock(JetLockLayout.UserGroupByte(JetLockLayout.ConnectionControlGroup, User), 1);
            }
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

    /// <summary>ACE's test of whether anyone else is connected (locking.md §3): every other user's group-0 byte, those
    /// below this user's own as one range and those above as another, each tried once, the second only when the first
    /// is granted — user 1 tries bytes 2–255 as one range; user 2 tries user 1's byte, and with it refused tries nothing
    /// more (measured). Granted, both ranges are held, nobody else being connected, until <see cref="UnlockOthers"/>; a
    /// refusal holds nothing and returns false.</summary>
    /// <remarks>ACE holds them across its lock and read of page 1 when it gives pages back, at a statement and at close
    /// (measured), and lets them go before it writes the page.</remarks>
    public bool TryLockOthers()
    {
        long below = JetLockLayout.UserByte(1), above = JetLockLayout.UserByte(User + 1);
        int belowLength = User - 1, aboveLength = JetLockLayout.MaxUserNumber - User;
        if (belowLength > 0 && !_lockFile.TryLock(below, belowLength)) return false;
        if (aboveLength > 0 && !_lockFile.TryLock(above, aboveLength))
        {
            if (belowLength > 0) _lockFile.Unlock(below, belowLength);
            return false;
        }
        return true;
    }

    /// <summary>Lets go of the other users' group-0 bytes <see cref="TryLockOthers"/> took.</summary>
    public void UnlockOthers()
    {
        if (User > 1) _lockFile.Unlock(JetLockLayout.UserByte(1), User - 1);
        if (User < JetLockLayout.MaxUserNumber)
            _lockFile.Unlock(JetLockLayout.UserByte(User + 1), JetLockLayout.MaxUserNumber - User);
    }
}

/// <summary>What a lock is taken for, which decides how long ACE retries it when refused and what the refusal ends in
/// (<see cref="JetUserRegistration.Lock"/>; locking.md §7).</summary>
internal enum JetLockPurpose
{
    /// <summary>A page about to be written: region 2, exclusive; refused with 3218.</summary>
    PageWrite,

    /// <summary>A table read, write or design lock: regions 4–6; refused with 3211 naming the table — with 3008 when it
    /// is the read byte of a table opened for writing (an <c>INSERT</c>'s target), after one round of retries.</summary>
    Table,

    /// <summary>The read byte of a table opened for reading — a query's, an <c>UPDATE</c>'s or a <c>DELETE</c>'s tables,
    /// which ACE opens twice: region 4, shared; refused with 3008 naming the table after two rounds of retries.</summary>
    TableRead,

    /// <summary>A chained long value read: region 2, the shared byte on the chain's first page; refused with 3624 after
    /// three rounds of retries.</summary>
    LongValueRead,

    /// <summary>A chained long value an <c>UPDATE</c> replaces: region 2, exclusive on the old chain's first page;
    /// refused with 3202.</summary>
    LongValueReplace,

    /// <summary>A chained long value a <c>DELETE</c> removes: region 2, exclusive on the chain's first page; once
    /// refused, the row is deleted without it and the chain is left where it is.</summary>
    LongValueRemove,

    /// <summary>The key a foreign key references, found for a child row's check: region 2, the shared byte on the
    /// parent's index page holding it; refused with 3624 after one round of retries.</summary>
    ReferencedKey,
}
