namespace JetLockTrace;

/// <summary>
///     Compares, statement by statement, the locks two files in one capture were taken with: the two engines of a
///     parity run, each on its own copy of a database. The statements are the scenario markers
///     (<see cref="TraceEvent.TryParseMarker" />) written around each one, aligned by command label.
/// </summary>
/// <remarks>
///     <para>
///         Only lock calls are compared, as multisets: unlocks and order are ignored. A lock is identified by
///         <see cref="LockDecoder.Classify" /> as (kind, where, mode) — where is a page, or the group and bytes of a
///         user-region lock — and a refused one is its own mode (<c>… REFUSED</c>). The second view drops
///         <c>where</c>, because the two files' page numbers drift apart during a run.
///     </para>
///     <para>
///         Locks between a statement's <c>end</c> marker and the next <c>begin</c> belong to the statement before: ACE
///         writes and locks for about half a second after a command returns. They are counted with it and flagged.
///     </para>
/// </remarks>
public static class StatementComparison
{
    private const string Unmarked = "(unmarked)";

    private readonly record struct Key(string Kind, string Where, string Mode);

    private readonly record struct ShapeKey(string Kind, string Mode);

    private sealed class Step(string label)
    {
        public string Label { get; } = label;

        public Dictionary<Key, int> Locks { get; } = [];

        public Dictionary<Key, int> AfterEnd { get; } = [];

        public int Count => Locks.Values.Sum();

        public int AfterEndCount => AfterEnd.Values.Sum();

        public void Add(Key key, bool afterEnd)
        {
            Locks[key] = Locks.GetValueOrDefault(key) + 1;
            if (afterEnd)
            {
                AfterEnd[key] = AfterEnd.GetValueOrDefault(key) + 1;
            }
        }
    }

    /// <summary>One file's statements, in the order their <c>begin</c> markers appear.</summary>
    private sealed class Side
    {
        private readonly Dictionary<string, int> _occurrences = [];
        private Step? _current;
        private bool _ended;

        public List<Step> Steps { get; } = [];

        public Dictionary<string, Step> ByLabel { get; } = [];

        public void Marker(string phase, string command)
        {
            if (phase == "begin")
            {
                // The same command twice in one run is told apart by how many times it has begun.
                int occurrence = _occurrences[command] = _occurrences.GetValueOrDefault(command) + 1;
                _current = Open(occurrence == 1 ? command : $"{command}#{occurrence}");
                _ended = false;
            }
            else if (phase == "end")
            {
                _ended = true;
            }
        }

        public void Lock(Key key)
        {
            _current ??= Open(Unmarked);
            _current.Add(key, _ended);
        }

        private Step Open(string label)
        {
            var step = new Step(label);
            Steps.Add(step);
            ByLabel[label] = step;
            return step;
        }
    }

    /// <summary>
    ///     Reads <paramref name="csvPath" /> once and writes the comparison of the lock calls on the files whose path
    ///     contains <paramref name="fileA" /> and <paramref name="fileB" /> (a row goes to each side it matches).
    /// </summary>
    /// <returns>The number of statements compared.</returns>
    public static int Run(
        string csvPath, string fileA, string fileB, string? process, int? pid, TextWriter output)
    {
        var a = new Side();
        var b = new Side();

        foreach (TraceEvent e in ProcMonCsv.Read(csvPath))
        {
            if (!TraceFilter.Accepts(e, null, process, pid))
            {
                continue;
            }

            bool inA = e.Path.Contains(fileA, StringComparison.OrdinalIgnoreCase);
            bool inB = e.Path.Contains(fileB, StringComparison.OrdinalIgnoreCase);
            if (!inA && !inB)
            {
                continue;
            }

            if (e.IsMarker)
            {
                if (e.TryParseMarker(out _, out _, out string phase, out string command))
                {
                    if (inA)
                    {
                        a.Marker(phase, command);
                    }

                    if (inB)
                    {
                        b.Marker(phase, command);
                    }
                }

                continue;
            }

            if (!e.IsLock || e.Offset is null)
            {
                continue;
            }

            LockDecoder.LockShape shape = LockDecoder.Classify(e.Offset.Value, e.Length);
            var key = new Key(shape.Kind, shape.Where, e.Failed ? $"{shape.Mode} REFUSED" : shape.Mode);
            if (inA)
            {
                a.Lock(key);
            }

            if (inB)
            {
                b.Lock(key);
            }
        }

        // Side A's order, then whatever only side B has.
        List<string> labels = [.. a.Steps.Select(s => s.Label), .. b.Steps.Select(s => s.Label).Where(l => !a.ByLabel.ContainsKey(l))];

        int same = 0, pagesOnly = 0, kinds = 0, onlyInA = 0, onlyInB = 0;
        var totalsA = new Dictionary<ShapeKey, int>();
        var totalsB = new Dictionary<ShapeKey, int>();

        foreach (string label in labels)
        {
            a.ByLabel.TryGetValue(label, out Step? stepA);
            b.ByLabel.TryGetValue(label, out Step? stepB);
            if (stepA is null)
            {
                onlyInB++;
            }

            if (stepB is null)
            {
                onlyInA++;
            }

            Dictionary<Key, int> locksA = stepA?.Locks ?? [];
            Dictionary<Key, int> locksB = stepB?.Locks ?? [];
            AddTotals(totalsA, locksA);
            AddTotals(totalsB, locksB);

            List<(Key Key, int Count)> pagesA = Only(locksA, locksB), pagesB = Only(locksB, locksA);
            Dictionary<ShapeKey, int> shapesA = Shapes(locksA), shapesB = Shapes(locksB);
            List<(ShapeKey Key, int Count)> kindsA = Only(shapesA, shapesB), kindsB = Only(shapesB, shapesA);

            string status;
            if (pagesA.Count == 0 && pagesB.Count == 0)
            {
                status = "same";
                same++;
            }
            else if (kindsA.Count == 0 && kindsB.Count == 0)
            {
                status = "pages";
                pagesOnly++;
            }
            else
            {
                status = "kinds";
                kinds++;
            }

            output.WriteLine(
                $"=== {label} {status}  A: {Describe(stepA)}  B: {Describe(stepB)}");

            foreach ((Key key, int count) in pagesA)
            {
                output.WriteLine($"{label}  only A  {Line(key, count, stepA)}");
            }

            foreach ((Key key, int count) in pagesB)
            {
                output.WriteLine($"{label}  only B  {Line(key, count, stepB)}");
            }

            if (status == "kinds")
            {
                output.WriteLine($"{label}  by kind (page numbers ignored):");
                foreach ((ShapeKey key, int count) in kindsA)
                {
                    output.WriteLine($"{label}  only A  {count,4}x  {key.Kind,-40} {key.Mode}");
                }

                foreach ((ShapeKey key, int count) in kindsB)
                {
                    output.WriteLine($"{label}  only B  {count,4}x  {key.Kind,-40} {key.Mode}");
                }
            }
        }

        output.WriteLine();
        output.WriteLine("=== summary");
        output.WriteLine($"A: files containing '{fileA}'   B: files containing '{fileB}'");
        output.WriteLine(
            $"statements: {labels.Count}  identical: {same}  differ only in page numbers: {pagesOnly}  differ in kind counts: {kinds}");
        output.WriteLine($"in only one side's markers: A only {onlyInA}, B only {onlyInB}");
        output.WriteLine();
        output.WriteLine($"{"lock calls by kind",-62} {"A",8} {"B",8}");

        foreach (ShapeKey key in totalsA.Keys.Union(totalsB.Keys).Order(Comparer<ShapeKey>.Create(
                     (x, y) => string.CompareOrdinal($"{x.Kind} {x.Mode}", $"{y.Kind} {y.Mode}"))))
        {
            output.WriteLine(
                $"{$"{key.Kind} {key.Mode}",-62} {totalsA.GetValueOrDefault(key),8} {totalsB.GetValueOrDefault(key),8}");
        }

        output.WriteLine($"{"total",-62} {totalsA.Values.Sum(),8} {totalsB.Values.Sum(),8}");
        return labels.Count;
    }

    private static string Describe(Step? step)
        => step is null ? "(no marker)" : $"{step.Count} locks (after end {step.AfterEndCount})";

    private static string Line(Key key, int count, Step? step)
    {
        string text = $"{count,4}x  {key.Kind,-40} {key.Where,-24} {key.Mode}";
        int after = step?.AfterEnd.GetValueOrDefault(key) ?? 0;
        return after > 0 ? $"{text}  [after end: {after} of this side's {step!.Locks[key]}]" : text;
    }

    private static Dictionary<ShapeKey, int> Shapes(Dictionary<Key, int> locks)
    {
        var shapes = new Dictionary<ShapeKey, int>();
        foreach ((Key key, int count) in locks)
        {
            var shape = new ShapeKey(key.Kind, key.Mode);
            shapes[shape] = shapes.GetValueOrDefault(shape) + count;
        }

        return shapes;
    }

    private static void AddTotals(Dictionary<ShapeKey, int> totals, Dictionary<Key, int> locks)
    {
        foreach ((ShapeKey key, int count) in Shapes(locks))
        {
            totals[key] = totals.GetValueOrDefault(key) + count;
        }
    }

    /// <summary>What <paramref name="x" /> has more of than <paramref name="y" />, in key order.</summary>
    private static List<(TKey Key, int Count)> Only<TKey>(Dictionary<TKey, int> x, Dictionary<TKey, int> y)
        where TKey : notnull
        => [.. x
            .Select(p => (p.Key, Count: p.Value - y.GetValueOrDefault(p.Key)))
            .Where(p => p.Count > 0)
            .OrderBy(p => p.Key.ToString(), StringComparer.Ordinal)];
}
