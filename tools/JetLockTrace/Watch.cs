using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using LibRed.Formats;
using Microsoft.Win32.SafeHandles;

namespace JetLockTrace;

/// <summary>
///     <c>watch</c>: samples a database's page-0 commit slots as fast as it can and reports every value they hold,
///     however briefly. Process Monitor shows that ACE writes a slot but not what it writes; this sees the bytes.
/// </summary>
/// <remarks>
///     Never writes to the database. One positional read per sample (<see cref="RandomAccess" />), no buffering of
///     our own, no memory mapping, and nothing allocated or printed inside the loop.
/// </remarks>
public static class Watch
{
    private const int DefaultSeconds = 60;
    private const int MaxTransitions = 500_000;
    private const int StopCheckInterval = 10_000;

    private static volatile bool _cancelled;

    /// <summary><c>watch &lt;database&gt; [--slots &lt;first&gt;[-&lt;last&gt;]] [--seconds &lt;n&gt;] [--out &lt;path&gt;]</c>.</summary>
    public static int Run(string[] args)
    {
        string? database = null, outPath = null;
        int firstSlot = 0, lastSlot = 3, seconds = DefaultSeconds;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slots" when i + 1 < args.Length:
                    if (!TryParseSlots(args[++i], out firstSlot, out lastSlot))
                    {
                        Console.Error.WriteLine("--slots needs <first> or <first>-<last>, with first <= last.");
                        return 1;
                    }

                    break;
                case "--seconds" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], NumberStyles.None, CultureInfo.InvariantCulture, out seconds) || seconds <= 0)
                    {
                        Console.Error.WriteLine("--seconds needs a positive number.");
                        return 1;
                    }

                    break;
                case "--out" when i + 1 < args.Length:
                    outPath = args[++i];
                    break;
                default:
                    if (args[i].StartsWith('-') || database is not null)
                    {
                        Console.Error.WriteLine($"Unexpected argument '{args[i]}'. Try --help.");
                        return 1;
                    }

                    database = args[i];
                    break;
            }
        }

        if (database is null)
        {
            Console.Error.WriteLine("Usage: jetlocktrace watch <database> [--slots <first>[-<last>]] [--seconds <n>] [--out <path>]. Try --help.");
            return 1;
        }

        string databasePath = Path.GetFullPath(database);
        if (!File.Exists(databasePath))
        {
            Console.Error.WriteLine($"'{databasePath}' does not exist.");
            return 1;
        }

        // The commit table's place comes from the database's own format, as probe does.
        JetFormatBase format;
        using (var header = new FileStream(
                   databasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            format = JetFormatBase.Detect(header);
        }

        if (lastSlot >= format.CommitByteTableUsers)
        {
            Console.Error.WriteLine($"This database has {format.CommitByteTableUsers} commit slots: --slots must be within 0-{format.CommitByteTableUsers - 1}.");
            return 1;
        }

        int slotSize = format.CommitByteSlotSize;
        int slotCount = lastSlot - firstSlot + 1;
        int length = slotCount * slotSize;
        long offset = format.CommitByteTableOffset + (long)firstSlot * slotSize;
        string stopFile = databasePath + ".watch.stop";

        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true;
            _cancelled = true;
        };
        Console.CancelKeyPress += onCancel;

        // Bytes of every transition live in one flat array, so recording one allocates nothing.
        var transTicks = new long[MaxTransitions];
        var transSamples = new long[MaxTransitions];
        var transBytes = new byte[MaxTransitions * length];
        var transitions = 0;
        var bufferFull = false;

        var previous = new byte[length];
        byte[] initial;
        var current = new byte[length];
        long samples = 0, lastTransitionSample = 0, maxGap = 0, totalGap = 0;
        long start, end;
        var stopReason = "time";

        using (SafeFileHandle handle = File.OpenHandle(
                   databasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, FileOptions.None))
        {
            Console.Error.WriteLine($"Watching slots {firstSlot}-{lastSlot} of {databasePath} for {seconds}s (stop early: Ctrl+C, or create {stopFile}).");

            long deadline;
            if (RandomAccess.Read(handle, previous, offset) != length)
            {
                Console.Error.WriteLine("The database is shorter than its commit table.");
                Console.CancelKeyPress -= onCancel;
                return 1;
            }

            initial = (byte[])previous.Clone();
            start = Stopwatch.GetTimestamp();
            deadline = start + (long)seconds * Stopwatch.Frequency;
            long lastTick = start;
            samples = 1;

            while (true)
            {
                int read = RandomAccess.Read(handle, current, offset);
                long now = Stopwatch.GetTimestamp();
                samples++;

                long gap = now - lastTick;
                totalGap += gap;
                if (gap > maxGap)
                {
                    maxGap = gap;
                }

                lastTick = now;

                if (read == length && !current.AsSpan().SequenceEqual(previous))
                {
                    transTicks[transitions] = now;
                    transSamples[transitions] = samples - 1 - lastTransitionSample;
                    current.CopyTo(transBytes.AsSpan(transitions * length, length));
                    transitions++;
                    lastTransitionSample = samples - 1;
                    (previous, current) = (current, previous);

                    if (transitions == MaxTransitions)
                    {
                        bufferFull = true;
                        stopReason = "transition buffer full";
                        break;
                    }
                }

                if (now >= deadline)
                {
                    break;
                }

                if (_cancelled)
                {
                    stopReason = "Ctrl+C";
                    break;
                }

                if (samples % StopCheckInterval == 0 && File.Exists(stopFile))
                {
                    stopReason = "stop file";
                    break;
                }
            }

            end = lastTick;
        }

        Console.CancelKeyPress -= onCancel;

        return Report(
            outPath, databasePath, firstSlot, slotCount, slotSize, length, offset, start, end, samples, maxGap, totalGap,
            transitions, transTicks, transSamples, transBytes, bufferFull, stopReason, initial);
    }

    private static bool TryParseSlots(string text, out int first, out int last)
    {
        first = last = 0;
        int dash = text.IndexOf('-', StringComparison.Ordinal);
        if (dash < 0)
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out first) && (last = first) >= 0;
        }

        return int.TryParse(text.AsSpan(0, dash), NumberStyles.None, CultureInfo.InvariantCulture, out first)
               && int.TryParse(text.AsSpan(dash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out last)
               && first <= last;
    }

    private static double Micros(long ticks) => ticks * 1_000_000.0 / Stopwatch.Frequency;

    private static int Report(
        string? outPath, string databasePath, int firstSlot, int slotCount, int slotSize, int length, long offset,
        long start, long end, long samples, long maxGap, long totalGap, int transitions, long[] transTicks,
        long[] transSamples, byte[] transBytes, bool bufferFull, string stopReason, byte[] initial)
    {
        var report = new StringBuilder();
        CultureInfo c = CultureInfo.InvariantCulture;

        report.AppendLine($"Database   {databasePath}");
        report.AppendLine($"Slots      {firstSlot}-{firstSlot + slotCount - 1}  (page 0 +0x{offset:X}, {length} bytes, one read per sample)");
        report.AppendLine($"Stopped    {stopReason}{(bufferFull ? "  !! transition buffer full, later transitions lost" : "")}");
        report.AppendLine();

        // The first value is what the first sample saw: listed per slot with no entry and no hold, because when it
        // began is unknown.
        var perSlot = new SlotStats[slotCount];
        var prevBytes = (byte[])initial.Clone();
        var prevValues = new int[slotCount];
        var lastChange = new long[slotCount];
        for (var s = 0; s < slotCount; s++)
        {
            perSlot[s] = new SlotStats();
            prevValues[s] = BinaryPrimitives.ReadUInt16LittleEndian(initial.AsSpan(s * slotSize, slotSize));
            perSlot[s].Seen(prevValues[s]);
            lastChange[s] = -1;
        }

        long prevTick = start;

        for (var t = 0; t < transitions; t++)
        {
            ReadOnlySpan<byte> bytes = transBytes.AsSpan(t * length, length);
            long tick = transTicks[t];
            var line = new StringBuilder();
            line.Append(c, $"{Micros(tick - start) / 1_000_000.0,12:F6}s  ");
            line.Append(c, $"prev held {Micros(tick - prevTick),12:F1}us / {transSamples[t],8} samples  ");

            for (var s = 0; s < slotCount; s++)
            {
                ReadOnlySpan<byte> slot = bytes.Slice(s * slotSize, slotSize);
                int value = BinaryPrimitives.ReadUInt16LittleEndian(slot);
                bool changed = !slot.SequenceEqual(prevBytes.AsSpan(s * slotSize, slotSize));
                string reserved = value < 256 ? " <<RESERVED<256>>" : "";
                line.Append(c, $"[{firstSlot + s}]{(changed ? '*' : ' ')}{Convert.ToHexString(slot)}={value}{reserved}  ");

                if (changed)
                {
                    SlotStats stats = perSlot[s];
                    if (lastChange[s] >= 0)
                    {
                        stats.Held(prevValues[s], tick - lastChange[s]);
                    }

                    stats.Entered(value);
                    prevValues[s] = value;
                    lastChange[s] = tick;
                }
            }

            report.AppendLine(line.ToString().TrimEnd());
            bytes.CopyTo(prevBytes);
            prevTick = tick;
        }

        double elapsed = (double)(end - start) / Stopwatch.Frequency;
        report.AppendLine();
        report.AppendLine("Summary");
        report.AppendLine(c, $"  samples          {samples}");
        report.AppendLine(c, $"  elapsed          {elapsed:F3}s");
        report.AppendLine(c, $"  samples/second   {samples / elapsed:F0}");
        report.AppendLine(c, $"  gap mean         {Micros(totalGap) / Math.Max(1, samples - 1):F2}us");
        report.AppendLine(c, $"  gap worst        {Micros(maxGap):F1}us");
        report.AppendLine(c, $"  transitions      {transitions}");
        report.AppendLine("  (the first value is not a transition; hold times cover completed holds only, and are measured to the sample that saw the change)");
        report.AppendLine();

        var reservedSeen = false;
        for (var s = 0; s < slotCount; s++)
        {
            report.AppendLine($"Slot {firstSlot + s}: distinct values entered by a transition");
            if (perSlot[s].Values.Count == 0)
            {
                report.AppendLine("  (never changed)");
            }

            foreach ((int value, ValueStats v) in perSlot[s].Values.OrderBy(p => p.Key))
            {
                bool reserved = value < 256;
                reservedSeen |= reserved;
                string held = v.Holds == 0
                    ? "never completed a hold"
                    : string.Create(c, $"held min {Micros(v.Min):F1}us, max {Micros(v.Max):F1}us over {v.Holds} hold(s)");
                report.AppendLine(c, $"  {(reserved ? "!! " : "   ")}{(byte)(value & 0xFF):X2} {(byte)(value >> 8):X2} = {value,6}   entered {v.Entries}x   {held}{(reserved ? "   <<RESERVED VALUE <256>>" : "")}");
            }

            report.AppendLine();
        }

        report.AppendLine(reservedSeen
            ? "!! A value below 256 was seen (see the lines marked !!)."
            : "No value below 256 was seen.");

        if (outPath is null)
        {
            Console.Write(report.ToString());
        }
        else
        {
            File.WriteAllText(outPath, report.ToString());
            Console.Error.WriteLine($"Report written to {outPath}");
        }

        return 0;
    }

    private sealed class ValueStats
    {
        public int Entries;
        public int Holds;
        public long Min = long.MaxValue;
        public long Max;
    }

    private sealed class SlotStats
    {
        public Dictionary<int, ValueStats> Values { get; } = [];

        public void Seen(int value) => Stats(value);

        public void Entered(int value) => Stats(value).Entries++;

        public void Held(int value, long ticks)
        {
            ValueStats v = Stats(value);
            v.Holds++;
            v.Min = Math.Min(v.Min, ticks);
            v.Max = Math.Max(v.Max, ticks);
        }

        private ValueStats Stats(int value)
        {
            if (!Values.TryGetValue(value, out ValueStats? v))
            {
                Values[value] = v = new ValueStats();
            }

            return v;
        }
    }
}
