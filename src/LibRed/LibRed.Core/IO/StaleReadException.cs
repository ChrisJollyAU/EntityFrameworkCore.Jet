namespace LibRed.IO;

/// <summary>
/// A statement took a lock only after another user had committed — a page's write lock on a page that user changed
/// since this process read it, or one of a table's locks: what the statement read or planned against before it may be
/// older than the file, so it cannot go ahead on it
/// (<c>docs/format/locking.md</c> §4). Thrown by <see cref="PageChannel"/> with what that call took let go; the
/// caller rolls back what the statement did — its transaction, or its savepoint frame — and runs it again, bound and
/// planned afresh, as ACE in its place would have waited for the lock and then read. <c>QueryEngine</c> does that for
/// every statement. A caller working through the Core API cannot be re-run, and sees ACE's message for what it could
/// not lock.
/// </summary>
public sealed class StaleReadException : InvalidOperationException
{
    /// <summary>Creates the exception with ACE's message for a write that could not be made (error 3218), or with
    /// <paramref name="message"/> — ACE's for a table it could not lock.</summary>
    public StaleReadException(string? message = null)
        : base(message ?? JetUserRegistration.CurrentlyLockedMessage)
    {
    }
}
