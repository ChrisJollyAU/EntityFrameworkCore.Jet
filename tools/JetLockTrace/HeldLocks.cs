namespace JetLockTrace;

/// <summary>
///     Replays lock and unlock rows in trace order and reports what each process still holds at the end.
/// </summary>
/// <remarks>
///     Locks are keyed per process (PID) and file. An unlock removes the held lock with the same offset and length;
///     one that matches nothing is recorded as unmatched. Rows whose result was not <c>SUCCESS</c> change nothing:
///     a failed lock was never taken, and a failed unlock (<c>RANGE NOT LOCKED</c>) released nothing.
/// </remarks>
public sealed class HeldLocks
{
    private readonly record struct Held(long Offset, long Length, bool? Exclusive);

    // Insertion order is kept so the report reads in the order processes and files first appeared.
    private readonly List<(int? Pid, string? Name)> _processes = [];
    // Keyed on the upper-cased path (Windows paths are case-insensitive); the first-seen spelling is kept to print.
    private readonly Dictionary<(int? Pid, string Path), (string Display, List<Held> Locks)> _held = [];
    private readonly List<TraceEvent> _unmatched = [];

    /// <summary>Unlocks that matched no held lock.</summary>
    public IReadOnlyList<TraceEvent> Unmatched => _unmatched;

    /// <summary>Applies one lock or unlock row; other rows are ignored.</summary>
    public void Apply(TraceEvent e)
    {
        if ((!e.IsLock && !e.IsUnlock) || e.Offset is not { } offset)
        {
            return;
        }

        if (!_processes.Exists(p => p.Pid == e.Pid))
        {
            _processes.Add((e.Pid, e.ProcessName));
        }

        if (e.Failed)
        {
            return;
        }

        var key = (e.Pid, e.Path.ToUpperInvariant());
        long length = e.Length ?? 0;

        if (e.IsLock)
        {
            if (!_held.TryGetValue(key, out (string Display, List<Held> Locks) entry))
            {
                _held[key] = entry = (e.Path, []);
            }

            entry.Locks.Add(new Held(offset, length, e.Exclusive));
            return;
        }

        if (_held.TryGetValue(key, out (string Display, List<Held> Locks) found)
            && found.Locks.FindIndex(h => h.Offset == offset && h.Length == length) is >= 0 and var index)
        {
            found.Locks.RemoveAt(index);
            return;
        }

        _unmatched.Add(e);
    }

    /// <summary>Writes the locks still held, per process and file, decoded with <see cref="LockDecoder.DescribeLock" />.</summary>
    public void WriteReport(TextWriter output) => WriteHeld(output, "at end of trace", includeUnmatched: true);

    /// <summary>
    ///     Writes the locks held right now, as the end-of-trace report does, but without the unmatched-unlock list:
    ///     used at a scenario's <c>end</c> markers, to show which locks persist after each step.
    /// </summary>
    public void WriteSnapshot(TextWriter output)
    {
        if (_processes.Count == 0)
        {
            output.WriteLine("  (no lock activity yet)");
            return;
        }

        WriteHeld(output, "now", includeUnmatched: false);
    }

    private void WriteHeld(TextWriter output, string when, bool includeUnmatched)
    {
        foreach ((int? pid, string? name) in _processes)
        {
            output.WriteLine($"{Label(name, pid)} holds {when}:");

            var any = false;
            foreach (((int? keyPid, _), (string display, List<Held> locks)) in _held)
            {
                if (keyPid != pid || locks.Count == 0)
                {
                    continue;
                }

                any = true;
                output.WriteLine($"  {display}");
                foreach (Held h in locks)
                {
                    output.WriteLine($"    {LockDecoder.DescribeLock(h.Offset, h.Length, h.Exclusive)}");
                }
            }

            if (!any)
            {
                output.WriteLine("  (nothing)");
            }
        }

        if (includeUnmatched && _unmatched.Count > 0)
        {
            output.WriteLine();
            output.WriteLine($"{_unmatched.Count} unlock(s) matched no held lock:");
            foreach (TraceEvent e in _unmatched)
            {
                output.WriteLine(
                    $"  {Label(e.ProcessName, e.Pid)} {e.Time} {LockDecoder.DescribeLock(e.Offset!.Value, e.Length)}  {e.Path}");
            }
        }
    }

    private static string Label(string? name, int? pid)
        => name is null && pid is null ? "(unknown process)" : $"{name ?? "?"}({pid?.ToString() ?? "?"})";
}
