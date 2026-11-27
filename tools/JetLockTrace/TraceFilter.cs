namespace JetLockTrace;

/// <summary>The row filters the decode and the statement comparison share.</summary>
public static class TraceFilter
{
    /// <summary>
    ///     Whether <paramref name="e" /> is a row to look at. A marker is a probe of a file that does not exist, so it is
    ///     neither a database nor a lock file; it still has to pass the file and process filters.
    /// </summary>
    /// <param name="e">The row.</param>
    /// <param name="file">Only rows whose path contains this (<c>--file</c>), or null.</param>
    /// <param name="process">Only rows from this process name (<c>--process</c>), or null.</param>
    /// <param name="pid">The process filter read as a PID, when it parsed as one.</param>
    public static bool Accepts(TraceEvent e, string? file, string? process, int? pid)
    {
        if (!e.IsMarker && !LockDecoder.IsDatabaseOrLockFile(e.Path))
        {
            return false;
        }

        if (file is not null && !e.Path.Contains(file, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return process is null
               || e.ProcessName?.Equals(process, StringComparison.OrdinalIgnoreCase) == true
               || (pid is { } wanted && e.Pid == wanted);
    }
}
