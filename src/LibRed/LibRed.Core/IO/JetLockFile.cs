using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace LibRed.IO;

/// <summary>
/// A handle on a Jet/ACE lock file (<c>.laccdb</c>/<c>.ldb</c>) and the operating-system byte-range locks taken
/// through it — the primitive Jet's locking is built on. The offsets come from <see cref="JetLockLayout"/>.
/// </summary>
/// <remarks>
/// <para>Windows only for now: anything else throws <see cref="PlatformNotSupportedException"/>. Every lock is an
/// exclusive, fail-immediately <c>LockFileEx</c> lock, which is the only kind Access takes, so these conflict with
/// Access's own. Jet's "shared" and "exclusive" are different byte ranges (see <see cref="JetLockLayout"/>), not
/// different lock modes. <c>LockFileEx</c> is called directly rather than through <c>FileStream.Lock</c> so that a
/// conflict comes back as a result and is not an <see cref="IOException"/> to be told apart from a real failure by its
/// error code.</para>
/// <para>Windows semantics this relies on: an exclusive lock conflicts with any other lock on overlapping bytes —
/// including one taken through another handle in the same process. A range past end-of-file can be locked. Locks
/// belong to the handle; closing it releases whatever it still holds, though Windows does not promise to do so
/// immediately.</para>
/// <para>Deliberately minimal: no waiting, no retry, no record of what is held.</para>
/// </remarks>
internal sealed class JetLockFile : IDisposable
{
    /// <summary>The lock file beside an ACE database (<c>.accdb</c>, <c>.accde</c>).</summary>
    public const string AceExtension = ".laccdb";

    /// <summary>The lock file beside a Jet database or workgroup file (<c>.mdb</c>, <c>.mdw</c>).</summary>
    public const string JetExtension = ".ldb";

    private readonly SafeFileHandle _handle;

    private JetLockFile(SafeFileHandle handle) => _handle = handle;

    /// <summary>The lock file Jet/ACE uses for <paramref name="databasePath"/>: the same name with the database's
    /// extension replaced, or null for an extension Jet/ACE does not lock.</summary>
    public static string? PathFor(string databasePath)
    {
        string extension = Path.GetExtension(databasePath);
        string? lockExtension =
            extension.Equals(".accdb", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".accde", StringComparison.OrdinalIgnoreCase) ? AceExtension
            : extension.Equals(".mdb", StringComparison.OrdinalIgnoreCase)
              || extension.Equals(".mdw", StringComparison.OrdinalIgnoreCase) ? JetExtension
            : null;
        return lockExtension is null ? null : Path.ChangeExtension(databasePath, lockExtension);
    }

    /// <summary>Opens the lock file at <paramref name="path"/> for locking. It is opened read-only and shared for
    /// read, write and delete, so it never stands in the way of Access opening, writing or deleting it.</summary>
    /// <param name="path">The lock file.</param>
    /// <param name="createIfMissing">Create the file when it does not exist; otherwise a missing file throws
    /// <see cref="FileNotFoundException"/>.</param>
    public static JetLockFile Open(string path, bool createIfMissing)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Jet lock-file locking is only implemented on Windows.");

        // Read access is enough: LockFileEx needs the handle to have either read or write access.
        return new JetLockFile(File.OpenHandle(
            path,
            createIfMissing ? FileMode.OpenOrCreate : FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete));
    }

    /// <summary>Tries to lock <paramref name="length"/> bytes at <paramref name="offset"/> exclusively, without
    /// waiting.</summary>
    /// <returns>True when the lock was taken; false when another lock on those bytes conflicts with it.</returns>
    /// <exception cref="IOException">The operating system refused the lock for any other reason.</exception>
    public bool TryLock(long offset, long length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        ObjectDisposedException.ThrowIf(_handle.IsClosed, this);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Jet lock-file locking is only implemented on Windows.");

        NativeOverlapped overlapped = At(offset);
        if (NativeMethods.LockFileEx(_handle, LockfileFailImmediately | LockfileExclusiveLock, 0, Low(length), High(length), ref overlapped))
            return true;

        int error = Marshal.GetLastPInvokeError();
        if (error == ErrorLockViolation)
            return false;
        throw Failure(error, "lock", offset, length);
    }

    /// <summary>Releases a lock this handle took with exactly <paramref name="offset"/> and
    /// <paramref name="length"/>.</summary>
    /// <exception cref="IOException">No such lock is held through this handle, or the release failed.</exception>
    public void Unlock(long offset, long length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        ObjectDisposedException.ThrowIf(_handle.IsClosed, this);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Jet lock-file locking is only implemented on Windows.");

        NativeOverlapped overlapped = At(offset);
        if (!NativeMethods.UnlockFileEx(_handle, 0, Low(length), High(length), ref overlapped))
            throw Failure(Marshal.GetLastPInvokeError(), "unlock", offset, length);
    }

    /// <summary>Closes the handle, which releases every lock still held through it.</summary>
    public void Dispose() => _handle.Dispose();

    private const uint LockfileFailImmediately = 0x1;
    private const uint LockfileExclusiveLock = 0x2;
    private const int ErrorLockViolation = 33;

    // A synchronous handle ignores the overlapped structure's event; only its offset fields are read.
    [SupportedOSPlatform("windows")]
    private static NativeOverlapped At(long offset) => new() { OffsetLow = (int)Low(offset), OffsetHigh = (int)High(offset) };

    private static uint Low(long value) => (uint)value;

    private static uint High(long value) => (uint)(value >> 32);

    private static IOException Failure(int error, string verb, long offset, long length) =>
        new($"Could not {verb} {length} byte(s) at 0x{offset:X} of the lock file: {Marshal.GetPInvokeErrorMessage(error)} (Win32 error {error})");

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [SupportedOSPlatform("windows")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LockFileEx(
            SafeFileHandle file, uint flags, uint reserved, uint lengthLow, uint lengthHigh, ref NativeOverlapped overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [SupportedOSPlatform("windows")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnlockFileEx(
            SafeFileHandle file, uint reserved, uint lengthLow, uint lengthHigh, ref NativeOverlapped overlapped);
    }
}
