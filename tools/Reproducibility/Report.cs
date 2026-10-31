using System.Globalization;
using System.Text;

namespace Reproducibility;

/// <summary>
/// The compare step: reads the run files of several machines and writes a markdown report of how far each analysis's
/// results moved, against the results the project saved (RMC-BestFit 2.0.x, on Windows), between OSes, and between
/// XGoodFit and RMC-BestFit's own element layer on Windows.
/// </summary>
/// <remarks>
/// A run is labelled by its platform, e.g. win-x64, and by its application when it isn't XGoodFit, e.g.
/// <c>win-x64 RMC-BestFit 2.0.1</c> for tools/BestFitFileCompat.Wpf's rerun. Every run is compared with the saved results
/// and with the baseline (XGoodFit on win-x64). When RMC-BestFit's own run is there, a value that is unexplained against
/// the saved results, but that RMC-BestFit's own rerun is unexplained against too, is classed
/// <see cref="ValueClass.SinceSaved"/>: RMC-BestFit doesn't reproduce its own saved result either. The step fails if
/// any analysis is unexplained.
/// </remarks>
public sealed class Report
{
    private const int NotableCount = 12;

    private readonly IReadOnlyList<string> _platforms;
    private readonly string _baseline;
    private readonly string? _reference;
    private readonly Dictionary<string, RunFile[]> _runs;
    private readonly List<string> _keys;
    private readonly Dictionary<(string Key, string Platform), AnalysisRecord> _records = new();
    private readonly List<string> _problems = new();

    private Report(IReadOnlyList<RunFile> files, string? baseline)
    {
        _runs = files.GroupBy(f => f.Environment.Label).ToDictionary(g => g.Key, g => g.ToArray());
        _platforms = OrderPlatforms(_runs.Keys);
        _baseline = baseline ?? (_platforms.Contains("win-x64") ? "win-x64" : _platforms[0]);
        if (!_platforms.Contains(_baseline)) throw new ArgumentException($"No run from the baseline platform {_baseline}.");
        _reference = _platforms.FirstOrDefault(p => p.Contains(" RMC-BestFit", StringComparison.Ordinal));

        // Runs split with --only add up; an analysis run twice on one platform keeps its first record.
        foreach (var (platform, runs) in _runs)
            foreach (AnalysisRecord record in runs.SelectMany(r => r.Analyses))
                _records.TryAdd((record.Key, platform), record);
        _keys = _records.Keys.Select(k => k.Key).Distinct()
            .OrderBy(k => k.Split(" | ")[0], StringComparer.Ordinal).ThenBy(k => FirstRecord(k).Collection, StringComparer.Ordinal)
            .ThenBy(k => k, StringComparer.Ordinal).ToList();
    }

    /// <summary>Whether any analysis is unexplained, which fails the step.</summary>
    public bool HasUnexplained { get; private set; }

    /// <summary>Reads run files (or the *.json in folders, recursively) and compares them.</summary>
    public static Report Read(IEnumerable<string> paths, string? baseline)
    {
        var files = new List<RunFile>();
        foreach (string path in paths)
        {
            IEnumerable<string> found = Directory.Exists(path)
                ? Directory.GetFiles(path, "*.json", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal)
                : new[] { path };
            files.AddRange(found.Select(RunFile.Read));
        }
        if (files.Count == 0) throw new ArgumentException("No run files found.");
        return new Report(files, baseline);
    }

    /// <summary>Writes the report.</summary>
    public string Write()
    {
        var rows = _keys.Select(MakeRow).ToList();
        HasUnexplained = rows.Any(r => r.Class == ValueClass.Unexplained);
        var md = new StringBuilder();
        md.AppendLine("# Numerical reproducibility of RMC-BestFit's examples");
        md.AppendLine();
        md.AppendLine("Each analysis that an example project holds as estimated, estimated again with its saved settings and seed " +
            "(tools/Reproducibility; docs/reproducibility.md explains the check). *Saved* is what RMC-BestFit 2.0.x saved on Windows. " +
            "An analysis's class is its farthest value's, by relative difference: " +
            $"**identical**; **last-bit** (at most {Classes.LastBit:0e0}); **small** (at most {Classes.Small:0e0}); " +
            $"**optimizer** (at most {Classes.OptimizerBound:0e0}, in a value an iterative optimizer located); " +
            $"**Monte Carlo** (at most {Classes.MonteCarloBound:P0}, in a value from random sampling); " +
            "**since saved** (against the saved results, where RMC-BestFit's own rerun differs from them too); " +
            "**unexplained** (anything else, which fails the check).");
        md.AppendLine();

        md.AppendLine("## Runs");
        md.AppendLine();
        md.AppendLine("| Run | Application | OS | .NET | Processors | Machine | Started | Run time |");
        md.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (string platform in _platforms)
            foreach (RunFile run in _runs[platform])
                md.AppendLine($"| {platform} | {run.Environment.Application ?? Runner.XGoodFit} | {run.Environment.OsDescription} " +
                    $"({run.Environment.Architecture}) | {run.Environment.Framework} | {run.Environment.ProcessorCount} | {run.Environment.Machine} | " +
                    $"{run.Environment.Started} | {Minutes(run.Seconds)}" +
                    (run.Only.Length > 0 ? $" (only {string.Join(", ", run.Only.Select(o => "`" + Escape(o) + "`"))})" : "") + " |");
        md.AppendLine();

        md.AppendLine("## Summary");
        md.AppendLine();
        md.AppendLine("Analyses by class, in each comparison.");
        md.AppendLine();
        md.AppendLine("| Comparison | " + string.Join(" | ", Enum.GetValues<ValueClass>().Select(c => c.Label())) + " | not compared |");
        md.AppendLine("|---|" + string.Concat(Enum.GetValues<ValueClass>().Select(_ => "---:|")) + "---:|");
        foreach (string platform in _platforms)
            md.AppendLine(SummaryRow($"{platform} vs saved", rows.Select(r => r.VsSaved.GetValueOrDefault(platform)).ToList()));
        foreach (string platform in _platforms.Where(p => p != _baseline))
            md.AppendLine(SummaryRow($"{platform} vs {_baseline}", rows.Select(r => r.VsBaseline.GetValueOrDefault(platform)).ToList()));
        md.AppendLine();
        if (_reference != null)
        {
            var port = rows.Select(r => r.VsBaseline.GetValueOrDefault(_reference)).Where(c => c != null).ToList();
            md.AppendLine(port.All(c => c!.Class == ValueClass.Identical)
                ? $"**XGoodFit and {_reference.Split(' ', 2)[1]} give identical results on {_baseline}:** all {port.Count} analyses compared, bit for bit."
                : $"**XGoodFit and {_reference.Split(' ', 2)[1]} differ on {_baseline} in {port.Count(c => c!.Class != ValueClass.Identical)} of {port.Count} analyses:** " +
                  "the port should compute exactly what RMC-BestFit does; see below.");
        }
        else
        {
            md.AppendLine("No run of RMC-BestFit's own element layer (tools/BestFitFileCompat.Wpf rerun), so differences from the saved results " +
                "can't be classed *since saved*.");
        }
        md.AppendLine();
        md.AppendLine(HasUnexplained
            ? $"**{rows.Count(r => r.Class == ValueClass.Unexplained)} analyses are unexplained: see the notable cases.**"
            : "Nothing is unexplained.");
        int knownCount = rows.Count(r => r.All().Any(x => x.Comparison.Class == ValueClass.Known));
        if (knownCount > 0)
            md.AppendLine().AppendLine($"**{knownCount} analyses differ as known upstream issues: see the notable cases.**");
        md.AppendLine();

        md.AppendLine("## Against the saved results");
        md.AppendLine();
        md.AppendLine("Each cell: the class, and the value that decides it, with its relative difference.");
        md.AppendLine();
        AppendTable(md, rows, _platforms, r => r.VsSaved);

        if (_platforms.Count > 1)
        {
            md.AppendLine($"## Against {_baseline}");
            md.AppendLine();
            md.AppendLine("Between OSes, and between XGoodFit and RMC-BestFit's own element layer.");
            md.AppendLine();
            AppendTable(md, rows, _platforms.Where(p => p != _baseline).ToList(), r => r.VsBaseline);
        }

        md.AppendLine("## Run times (seconds)");
        md.AppendLine();
        md.AppendLine("| Example | Analysis | " + string.Join(" | ", _platforms) + " |");
        md.AppendLine("|---|---|" + string.Concat(_platforms.Select(_ => "---:|")));
        foreach (AnalysisRow row in rows.Where(r => _platforms.Any(p => r.Records.GetValueOrDefault(p)?.Seconds != null)))
            md.AppendLine($"| {Short(row.Example)} | {Escape(row.Name)} | " +
                string.Join(" | ", _platforms.Select(p => Seconds(row.Records.GetValueOrDefault(p)?.Seconds))) + " |");
        md.AppendLine();

        AppendNotable(md, rows);
        return md.ToString().ReplaceLineEndings("\n");
    }

    private static string SummaryRow(string label, IReadOnlyList<Comparison?> comparisons)
    {
        var counts = Enum.GetValues<ValueClass>().ToDictionary(c => c, c => comparisons.Count(x => x?.Class == c));
        return $"| {label} | " + string.Join(" | ", Enum.GetValues<ValueClass>().Select(c => counts[c].ToString(CultureInfo.InvariantCulture))) +
            $" | {comparisons.Count(c => c == null)} |";
    }

    private static void AppendTable(StringBuilder md, IReadOnlyList<AnalysisRow> rows, IReadOnlyList<string> platforms, Func<AnalysisRow, Dictionary<string, Comparison>> pick)
    {
        md.AppendLine("| Example | Analysis | Kind | Method | " + string.Join(" | ", platforms) + " |");
        md.AppendLine("|---|---|---|---|" + string.Concat(platforms.Select(_ => "---|")));
        foreach (AnalysisRow row in rows)
        {
            var cells = platforms.Select(p =>
            {
                AnalysisRecord? record = row.Records.GetValueOrDefault(p);
                if (record == null) return "not in this run";
                if (record.Status != AnalysisStatus.Rerun) return StatusText(record);
                Comparison? c = pick(row).GetValueOrDefault(p);
                if (c == null) return "-";
                string text = c.Class == ValueClass.Unexplained ? "**unexplained**" : c.Class.Label();
                if (c.Worst is { } worst) text += $" {Number(worst.Relative)} `{Escape(worst.Name)}`";
                return text;
            });
            md.AppendLine($"| {Short(row.Example)} | {Escape(row.Name)} | {row.Kind} | {Escape(row.Method)} | {string.Join(" | ", cells)} |");
        }
        md.AppendLine();
    }

    private void AppendNotable(StringBuilder md, IReadOnlyList<AnalysisRow> rows)
    {
        md.AppendLine("## Notable cases");
        md.AppendLine();

        var unexplained = rows.Where(r => r.Class == ValueClass.Unexplained).ToList();
        if (unexplained.Count > 0)
        {
            md.AppendLine("### Unexplained");
            md.AppendLine();
            foreach (AnalysisRow row in unexplained)
            {
                md.AppendLine($"- **{Escape(row.Name)}** ({Short(row.Example)}):");
                foreach (string problem in row.Problems) md.AppendLine($"  - {problem}");
                foreach (var (what, c) in row.All().Where(x => x.Comparison.Class == ValueClass.Unexplained))
                    foreach (Difference d in c.Unexplained.Take(NotableCount))
                        md.AppendLine($"  - {what}: `{Escape(d.Name)}` {Value(d.Left)} against {Value(d.Right)} ({Number(d.Relative)})");
            }
            md.AppendLine();
        }

        var known = rows.Where(r => r.All().Any(x => x.Comparison.Class == ValueClass.Known)).ToList();
        if (known.Count > 0)
        {
            md.AppendLine("### Known issues");
            md.AppendLine();
            md.AppendLine("Differences already explained as upstream issues (tools/Reproducibility/KnownIssues.cs): reported, not failed.");
            md.AppendLine();
            foreach (AnalysisRow row in known)
                foreach (var (platform, issue) in row.Known)
                {
                    Comparison? c = row.VsBaseline.GetValueOrDefault(platform) is { Class: ValueClass.Known } b ? b : row.VsSaved.GetValueOrDefault(platform);
                    string farthest = c?.Largest is { } d ? $": `{Escape(d.Name)}` {Value(d.Left)} against {Value(d.Right)} ({Number(d.Relative)})" : "";
                    md.AppendLine($"- **{Escape(row.Name)}** ({Short(row.Example)}), {platform}{farthest}. See {issue}.");
                }
            md.AppendLine();
        }

        var all = rows.SelectMany(r => r.All().Select(x => (Row: r, x.What, x.Comparison))).ToList();

        md.AppendLine("### Largest differences in deterministic values");
        md.AppendLine();
        md.AppendLine("Values that only rounding should move: closed-form results, and fitting analyses' log-likelihoods (AIC, BIC).");
        md.AppendLine();
        var deterministic = all.Where(x => x.Comparison.LargestEstimate != null)
            .OrderByDescending(x => x.Comparison.MaxEstimateRelative).Take(NotableCount).ToList();
        if (deterministic.Count == 0) md.AppendLine("None: every deterministic value is identical.");
        foreach (var (row, what, c) in deterministic)
            md.AppendLine(Line(row, what, c.LargestEstimate!));
        md.AppendLine();

        md.AppendLine("### Largest differences in values an optimizer located");
        md.AppendLine();
        md.AppendLine("Bulletin 17C's GMM fits, their curves and statistics: within the optimizer's tolerance, they may differ up to " +
            $"{Classes.OptimizerBound:0e0}.");
        md.AppendLine();
        var optimizer = all.Where(x => x.Comparison.LargestOptimizer != null)
            .OrderByDescending(x => x.Comparison.LargestOptimizer!.Relative).Take(NotableCount).ToList();
        if (optimizer.Count == 0) md.AppendLine("None: every such value is identical.");
        foreach (var (row, what, c) in optimizer)
            md.AppendLine(Line(row, what, c.LargestOptimizer!));
        md.AppendLine();

        foreach (var (cls, title) in new[]
        {
            (ValueClass.MonteCarlo, "Largest Monte Carlo differences"),
            (ValueClass.SinceSaved, "Differences since the examples were saved"),
        })
        {
            md.AppendLine($"### {title}");
            md.AppendLine();
            var cases = all.Where(x => x.Comparison.Class == cls).OrderByDescending(x => x.Comparison.Worst!.Relative).ToList();
            md.AppendLine(cases.Count == 0 ? "None." : $"{cases.Count} comparisons; the farthest:");
            md.AppendLine();
            foreach (var (row, what, c) in cases.Take(NotableCount))
                md.AppendLine(Line(row, what, c.Worst!));
            md.AppendLine();
        }

        md.AppendLine("### Not rerun");
        md.AppendLine();
        var notRerun = rows.SelectMany(r => r.Records.Where(p => p.Value.Status != AnalysisStatus.Rerun)
                .Select(p => (Row: r, Platform: p.Key, Record: p.Value)))
            .GroupBy(x => (x.Row.Key, x.Record.Status, x.Record.Reason)).ToList();
        if (notRerun.Count == 0) md.AppendLine("Every analysis was rerun.");
        foreach (var group in notRerun)
        {
            var first = group.First();
            string where = group.Count() == _platforms.Count ? "every run" : string.Join(", ", group.Select(x => x.Platform));
            md.AppendLine($"- {Escape(first.Row.Name)} ({Short(first.Row.Example)}): {StatusText(first.Record)} ({where})");
        }
        md.AppendLine();

        if (_problems.Count > 0)
        {
            md.AppendLine("### Run problems");
            md.AppendLine();
            foreach (string problem in _problems) md.AppendLine("- " + problem);
            md.AppendLine();
        }
    }

    private static string Line(AnalysisRow row, string what, Difference d) =>
        $"- {Number(d.Relative)}: {Escape(row.Name)} ({Short(row.Example)}), {what}: `{Escape(d.Name)}` {Value(d.Left)} against {Value(d.Right)}";

    private AnalysisRow MakeRow(string key)
    {
        var records = _platforms.Where(p => _records.ContainsKey((key, p))).ToDictionary(p => p, p => _records[(key, p)]);
        AnalysisRecord first = records.Values.First();
        var row = new AnalysisRow(key, first.Example, first.Name, first.Kind, first.Method, records);

        // The values RMC-BestFit's own rerun doesn't reproduce from the saved results: against them, any run's unexplained
        // difference is one since the example was saved.
        var sinceSaved = new HashSet<string>(StringComparer.Ordinal);
        if (_reference != null && records.TryGetValue(_reference, out AnalysisRecord? reference) && reference.Status == AnalysisStatus.Rerun &&
            reference.Values != null)
            foreach (ResultValue v in reference.Values.Where(v => v.Saved != null))
                if (Classes.Classify(v.Saved, v.Rerun, v.Role, v.Scale) == ValueClass.Unexplained)
                    sinceSaved.Add(v.Name);

        foreach (var (platform, record) in records)
        {
            if (record.Status == AnalysisStatus.Failed)
                row.Problems.Add($"{platform}: the rerun failed: {Escape(record.Reason ?? "")}");
            if (record.Status != AnalysisStatus.Rerun || record.Values == null) continue;
            // Against the saved results: only the values the project holds (some results are never saved, such as a
            // run's timings), and the rerun must have each of them.
            var saved = record.Values.Where(v => v.Saved != null).ToList();
            string? known = KnownIssues.For(row.Example, row.Name, platform);
            row.VsSaved[platform] = Comparison.Of(Side(saved, v => v.Saved), Side(saved, v => v.Rerun),
                d => sinceSaved.Contains(d.Name) ? ValueClass.SinceSaved : known != null ? ValueClass.Known : null);
            if (known != null) row.Known[platform] = known;
        }

        // The saved results are read from the same file everywhere, so every run must read them alike: the stored
        // numbers exactly, and what is computed from them on each machine (a quantile of the saved distribution, say)
        // to rounding.
        var savedReads = records.Where(p => p.Value.Values != null).ToList();
        foreach (var (platform, record) in savedReads.Skip(1))
        {
            Comparison read = Comparison.Of(Side(savedReads[0].Value.Values!, v => v.Saved), Side(record.Values!, v => v.Saved));
            if (read.Class > ValueClass.Small)
                row.Problems.Add($"{platform} reads the saved results differently from {savedReads[0].Key}: `{Escape(read.Largest?.Name ?? "")}` " +
                    $"{Value(read.Largest?.Left)} against {Value(read.Largest?.Right)}");
        }

        if (records.TryGetValue(_baseline, out AnalysisRecord? baseline) && baseline.Status == AnalysisStatus.Rerun && baseline.Values != null)
        {
            foreach (var (platform, record) in records)
            {
                if (platform == _baseline || record.Status != AnalysisStatus.Rerun || record.Values == null) continue;
                string? known = KnownIssues.For(row.Example, row.Name, platform);
                row.VsBaseline[platform] = Comparison.Of(Side(baseline.Values, v => v.Rerun), Side(record.Values, v => v.Rerun),
                    d => known != null ? ValueClass.Known : null);
            }
        }

        // A rerun on some runs only (the others skipped or failed it) is reported, and a failure is unexplained.
        if (records.Values.Select(r => r.Status).Distinct().Count() > 1)
            _problems.Add($"{Escape(first.Name)} ({Short(first.Example)}): " +
                string.Join(", ", records.Select(p => $"{p.Key} {StatusText(p.Value)}")));
        row.Class = row.Problems.Count > 0 ? ValueClass.Unexplained
            : row.All().Select(x => x.Comparison.Class).DefaultIfEmpty(ValueClass.Identical).Max();
        return row;
    }

    private static List<Value> Side(IEnumerable<ResultValue> values, Func<ResultValue, double?> pick) =>
        values.Select(v => new Value(v.Name, v.Role, pick(v), v.Scale)).ToList();

    private AnalysisRecord FirstRecord(string key) => _records.First(p => p.Key.Key == key).Value;

    private static IReadOnlyList<string> OrderPlatforms(IEnumerable<string> platforms)
    {
        // XGoodFit's runs first, by OS, then upstream's (RMC-BestFit's own WPF element layer, on Windows).
        string[] order = { "win-x64", "linux-x64", "osx-arm64", "osx-x64" };
        return platforms.OrderBy(p => p.Contains(' ', StringComparison.Ordinal) ? 1 : 0)
            .ThenBy(p => Array.IndexOf(order, p.Split(' ')[0]) is int i && i >= 0 ? i : order.Length)
            .ThenBy(p => p, StringComparer.Ordinal).ToList();
    }

    private static string StatusText(AnalysisRecord record) => record.Status switch
    {
        AnalysisStatus.NotEstimated => "not estimated in the project",
        AnalysisStatus.Skipped => "skipped: " + Escape(record.Reason ?? ""),
        AnalysisStatus.Failed => "**failed**: " + Escape(record.Reason ?? ""),
        _ => "rerun",
    };

    private static string Short(string example) => Escape(Path.GetFileNameWithoutExtension(example));

    private static string Escape(string text) => text.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

    private static string Number(double relative) =>
        double.IsPositiveInfinity(relative) ? "∞" : relative.ToString("0.0e0", CultureInfo.InvariantCulture);

    private static string Value(double? value) => value is double v ? v.ToString("R", CultureInfo.InvariantCulture) : "none";

    private static string Seconds(double? seconds) => seconds is double s ? s.ToString("0.0", CultureInfo.InvariantCulture) : "";

    private static string Minutes(double seconds) => TimeSpan.FromSeconds(seconds).ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);

    private sealed class AnalysisRow(string key, string example, string name, string kind, string method, Dictionary<string, AnalysisRecord> records)
    {
        public string Key { get; } = key;
        public string Example { get; } = example;
        public string Name { get; } = name;
        public string Kind { get; } = kind;
        public string Method { get; } = method;
        public Dictionary<string, AnalysisRecord> Records { get; } = records;
        public Dictionary<string, Comparison> VsSaved { get; } = new();
        public Dictionary<string, string> Known { get; } = new();
        public Dictionary<string, Comparison> VsBaseline { get; } = new();
        public List<string> Problems { get; } = new();
        public ValueClass Class { get; set; }

        public IEnumerable<(string What, Comparison Comparison)> All() =>
            VsSaved.Select(p => ($"{p.Key} vs saved", p.Value)).Concat(VsBaseline.Select(p => ($"{p.Key} vs the baseline", p.Value)));
    }
}
