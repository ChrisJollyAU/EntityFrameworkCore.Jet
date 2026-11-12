namespace LibRed.IO;

/// <summary>
/// Coordinates page access between the handles of <b>one process</b> that share a database file:
/// <see cref="PageChannel"/> acquires a shared lock around every page read from disk and an exclusive lock around
/// every page it publishes, and the implementation decides what that means. <see cref="MonitorLockManager"/> is
/// the one in use — process-local reader/writer locks — and tests inject their own to observe or fail a write.
///
/// <para>Locks are <b>operation-scoped</b>: acquired and released around a single page read or write, which
/// prevents a reader from seeing a half-written page. Transaction overlays use optimistic committed-page
/// validation at publish time, so overlapping writers in one process fail deterministically instead of losing
/// an update.</para>
///
/// <para>The locks other processes see are not here. A registered Jet user takes ACE's byte-range locks on the
/// lock file (<c>docs/format/locking.md</c>): its user bytes in <see cref="JetUserRegistration"/>, and a write
/// lock on each page it changes, taken by <see cref="PageChannel"/> when the page is first written and held
/// until the batch that writes it ends or the change is rolled back, and the table locks of every table a statement
/// uses (<see cref="PageChannel.LockTable"/>), through the per-process table <see cref="JetUserRegistration.Lock"/>
/// keeps — see <c>docs/design/transactions.md</c>.</para>
/// </summary>
/// <remarks>The API is <c>Enter</c>/<c>Exit</c> (not a disposable handle) so the hot path — a page read takes
/// and releases a shared lock — allocates nothing; <see cref="PageChannel"/> pairs each Enter with an Exit in a
/// <c>finally</c>. Locks are non-reentrant: a caller never enters the same page twice on one thread.</remarks>
public interface ILockManager
{
    /// <summary>Takes a shared (read) lock on the page. Multiple readers may hold it concurrently, but not while
    /// a writer does. Pair with <see cref="ExitShared"/>.</summary>
    void EnterShared(int page);

    /// <summary>Releases the shared lock taken by <see cref="EnterShared"/>.</summary>
    void ExitShared(int page);

    /// <summary>Takes an exclusive (write) lock on the page, excluding all other readers and writers of it. Pair
    /// with <see cref="ExitExclusive"/>.</summary>
    void EnterExclusive(int page);

    /// <summary>Releases the exclusive lock taken by <see cref="EnterExclusive"/>.</summary>
    void ExitExclusive(int page);
}