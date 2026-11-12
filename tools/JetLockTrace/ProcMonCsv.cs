using System.Globalization;

namespace JetLockTrace;

/// <summary>One row of a Process Monitor CSV export, reduced to the fields that matter here.</summary>
/// <param name="Operation">e.g. <c>LockFile</c>, <c>ReadFile</c>.</param>
/// <param name="Path">The file the operation was against.</param>
/// <param name="Offset">Byte offset from the <c>Detail</c> column, if it had one.</param>
/// <param name="Length">Byte length from the <c>Detail</c> column, if it had one.</param>
/// <param name="Exclusive">For a lock, whether <c>Detail</c> said <c>Exclusive: True</c>; null when it carried no flag.</param>
/// <param name="FailImmediately">For a lock, whether <c>Detail</c> said <c>FailImmediately: True</c>; null when absent.</param>
/// <param name="Time">The <c>Time of Day</c> column as exported, or null when the CSV has none.</param>
/// <param name="ProcessName">The <c>Process Name</c> column, or null when the CSV has none.</param>
/// <param name="Pid">The <c>PID</c> column, or null when the CSV has none (or it did not parse).</param>
/// <param name="Result">The <c>Result</c> column (<c>SUCCESS</c>, <c>NOT GRANTED</c> for a refused lock, …), or null when the CSV has none.</param>
public sealed record TraceEvent(
    string Operation,
    string Path,
    long? Offset,
    long? Length,
    bool? Exclusive,
    bool? FailImmediately = null,
    string? Time = null,
    string? ProcessName = null,
    int? Pid = null,
    string? Result = null)
{
    /// <summary>Whether this row is a byte-range lock.</summary>
    public bool IsLock => Operation.Equals("LockFile", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether this row is a byte-range unlock.</summary>
    public bool IsUnlock
        => Operation.Equals("UnlockFile", StringComparison.OrdinalIgnoreCase)
           || Operation.Equals("UnlockFileSingle", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the operation reported anything other than <c>SUCCESS</c>. Unknown (no column) counts as success.</summary>
    public bool Failed
        => !string.IsNullOrWhiteSpace(Result) && !Result.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Whether this row is a scenario marker: a probe of the non-existent file
    ///     <c>&lt;database&gt;.mark.&lt;name&gt;.&lt;seq&gt;.&lt;begin|end&gt;.&lt;command&gt;</c> that jetlockscenario makes
    ///     before and after each command. Whatever the operation was.
    /// </summary>
    public bool IsMarker => Path.Contains(ProcMonCsv.MarkerTag, StringComparison.OrdinalIgnoreCase);

    /// <summary>Splits a marker path into its name, sequence, phase (<c>begin</c>/<c>end</c>) and command; false when it is not one.</summary>
    public bool TryParseMarker(out string name, out string sequence, out string phase, out string command)
    {
        name = sequence = phase = command = "";

        int at = Path.IndexOf(ProcMonCsv.MarkerTag, StringComparison.OrdinalIgnoreCase);
        if (at < 0)
        {
            return false;
        }

        // The name, sequence and phase contain no dots (the scenario tool sanitises them); the command is the rest.
        string[] parts = Path[(at + ProcMonCsv.MarkerTag.Length)..].Split('.', 4);
        if (parts.Length < 3)
        {
            return false;
        }

        (name, sequence, phase) = (parts[0], parts[1], parts[2].ToLowerInvariant());
        command = parts.Length > 3 ? parts[3] : "";
        return true;
    }
}

/// <summary>Reads a Process Monitor CSV export.</summary>
/// <remarks>
///     ProcMon quotes every field, and the <c>Detail</c> column contains commas inside those quotes
///     (<c>"Offset: 536,905,217, Length: 257"</c>), so the fields have to be split with a real CSV reader rather
///     than by splitting on commas. Numbers carry thousands separators for the same reason.
/// </remarks>
public static class ProcMonCsv
{
    /// <summary>The operations worth decoding; everything else in a trace is noise for this purpose.</summary>
    private static readonly HashSet<string> InterestingOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "LockFile", "UnlockFile", "UnlockFileSingle", "ReadFile", "WriteFile",
    };

    /// <summary>
    ///     Marks a path as a scenario marker (see <see cref="TraceEvent.IsMarker" />). Marker rows are kept whatever
    ///     their operation (<c>CreateFile</c>, <c>QueryOpen</c>, …), because they are probes of a file that is not there.
    /// </summary>
    public const string MarkerTag = ".mark.";

    /// <summary>
    ///     Parses <paramref name="path" />, keeping lock/unlock/read/write rows and marker rows. A marker the file
    ///     probe produced several rows for (open, then query) is collapsed into one.
    /// </summary>
    public static IEnumerable<TraceEvent> Read(string path)
    {
        using var reader = new StreamReader(path);

        string[]? header = ReadRecord(reader);
        if (header is null)
        {
            yield break;
        }

        int operationColumn = IndexOf(header, "Operation");
        int pathColumn = IndexOf(header, "Path");
        int detailColumn = IndexOf(header, "Detail");

        // Optional: older or trimmed exports may not carry these, and the tool still works without them.
        int timeColumn = IndexOf(header, "Time of Day");
        int processColumn = IndexOf(header, "Process Name");
        int pidColumn = IndexOf(header, "PID");
        int resultColumn = IndexOf(header, "Result");

        if (operationColumn < 0 || pathColumn < 0 || detailColumn < 0)
        {
            throw new InvalidDataException(
                "The CSV has no Operation/Path/Detail columns. Export from Process Monitor with "
                + "File > Save > Comma-Separated Values, leaving the default columns in place.");
        }

        // The last marker path seen per PID, so two processes writing markers at once do not defeat the collapse.
        var lastMarkerPath = new Dictionary<string, string>();

        while (ReadRecord(reader) is { } fields)
        {
            if (fields.Length <= detailColumn)
            {
                continue;
            }

            string operation = fields[operationColumn];
            string rowPath = fields[pathColumn];
            bool marker = rowPath.Contains(MarkerTag, StringComparison.OrdinalIgnoreCase);
            if (!marker && !InterestingOperations.Contains(operation))
            {
                continue;
            }

            if (marker)
            {
                // Consecutive rows for one marker path are one marker. Sequence numbers make a path unique, so a
                // genuine repeat only ever follows a different marker.
                string who = Field(fields, pidColumn) ?? "";
                if (lastMarkerPath.TryGetValue(who, out string? previous)
                    && rowPath.Equals(previous, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                lastMarkerPath[who] = rowPath;
            }

            string detail = fields[detailColumn];

            yield return new TraceEvent(
                operation,
                rowPath,
                FindNumber(detail, "Offset:"),
                FindNumber(detail, "Length:"),
                FindFlag(detail, "Exclusive:"),
                // Spelled "Fail Immediately" in some ProcMon versions' Detail text, "FailImmediately" in others.
                FindFlag(detail, "FailImmediately:") ?? FindFlag(detail, "Fail Immediately:"),
                Field(fields, timeColumn),
                Field(fields, processColumn),
                int.TryParse(Field(fields, pidColumn), NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int pid)
                    ? pid
                    : null,
                Field(fields, resultColumn));
        }
    }

    private static string? Field(string[] fields, int column)
        => column >= 0 && column < fields.Length ? fields[column] : null;

    /// <summary>Reads a <c>Label: True</c> / <c>Label: False</c> flag from <c>Detail</c>; null when it is not there.</summary>
    private static bool? FindFlag(string detail, string label)
        => detail.Contains($"{label} True", StringComparison.OrdinalIgnoreCase)
            ? true
            : detail.Contains($"{label} False", StringComparison.OrdinalIgnoreCase)
                ? false
                : null;

    private static int IndexOf(string[] header, string name)
        => Array.FindIndex(header, h => h.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Pulls the number following <paramref name="label" />, e.g. <c>Offset: 536,905,217</c> → 536905217.</summary>
    private static long? FindNumber(string detail, string label)
    {
        int start = detail.IndexOf(label, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        start += label.Length;

        // Skip to the first digit, then take digits and thousands separators.
        while (start < detail.Length && !char.IsDigit(detail[start]))
        {
            // A non-digit, non-space before the number means we are looking at a different field.
            if (detail[start] is not (' ' or '\t'))
            {
                return null;
            }

            start++;
        }

        int end = start;
        while (end < detail.Length && (char.IsDigit(detail[end]) || detail[end] == ','))
        {
            end++;
        }

        return end > start
               && long.TryParse(
                   detail.AsSpan(start, end - start),
                   NumberStyles.AllowThousands,
                   CultureInfo.InvariantCulture,
                   out long value)
            ? value
            : null;
    }

    /// <summary>Reads one CSV record, honouring quotes and embedded newlines.</summary>
    private static string[]? ReadRecord(StreamReader reader)
    {
        if (reader.EndOfStream)
        {
            return null;
        }

        var fields = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;

        while (true)
        {
            int next = reader.Read();

            if (next < 0)
            {
                fields.Add(field.ToString());
                return fields.Count == 1 && fields[0].Length == 0 ? null : fields.ToArray();
            }

            var c = (char)next;

            if (quoted)
            {
                if (c != '"')
                {
                    field.Append(c);
                }
                else if (reader.Peek() == '"')
                {
                    reader.Read();
                    field.Append('"');
                }
                else
                {
                    quoted = false;
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    quoted = true;
                    break;
                case ',':
                    fields.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    fields.Add(field.ToString());
                    return fields.ToArray();
                default:
                    field.Append(c);
                    break;
            }
        }
    }
}
