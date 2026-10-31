using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RMC.BestFit.UI;

namespace BestFitFileCompat;

/// <summary>
/// The bidirectional file matrix's second direction (docs/migration-plan.md, Phase 9): upstream's WPF
/// RMC-BestFit opens the files the port saved, on any OS, and must open them to the state it opens its
/// own saves to, and save them as it saves its own.
/// </summary>
/// <remarks>
/// The port's file-compatibility suite keeps each file it saves beside its description, under
/// artifacts/filecompat/&lt;rid&gt;/ (tests/BestFit/RMC.BestFit.FileCompat.Tests): the examples it opened
/// and saved, and under <c>changed/</c> a few it changed first (re-estimated, edited, added to), with the
/// port's own description of each and its in-memory state. For each file this command:
/// <list type="number">
/// <item>describes the file as the port saved it, and compares it with RMC-BestFit's save of the same
/// example (the golden data), or for a changed file with the port's own description;</item>
/// <item>opens it with RMC-BestFit as File &gt; Open does, and compares the in-memory state with the state
/// RMC-BestFit's own save opens to (the golden data's state after reopening), or for a changed file with
/// the state the port opened it to, value by value;</item>
/// <item>saves it again with RMC-BestFit, every element and then the project, and compares that save with
/// RMC-BestFit's own (or the port's, for a changed file): a platform difference still there survived
/// RMC-BestFit's open and save;</item>
/// <item>opens that save again, which should change nothing.</item>
/// </list>
/// Files the port saved on Windows must match exactly. Files saved on macOS and Linux may differ in the
/// accepted platform differences (<see cref="IsAccepted"/>): line ends in XML that RMC-BestFit stores
/// (XElement.ToString() writes Environment.NewLine), ICU's narrow no-break space before AM and PM in dates,
/// the path separator in FullFileName, compressed bytes, and in-memory statistics that differ only in
/// their numbers. Anything else is unexpected, and the command fails.
/// </remarks>
internal static class OpenSaved
{
    /// <summary>The folder, under the port's output, of the files the port changed before saving.</summary>
    public const string ChangedFolder = "changed/";

    private static readonly DifferenceKind[] PlatformKinds =
        { DifferenceKind.LineEnds, DifferenceKind.NarrowNoBreakSpace, DifferenceKind.PathSeparator, DifferenceKind.Compression };

    /// <summary>Runs the command; returns 0 when nothing unexpected differed.</summary>
    public static int Run(string savedFolder, string goldenFolder, string outputFolder, string? rid, string? only)
    {
        savedFolder = Path.GetFullPath(savedFolder);
        rid ??= Path.GetFileName(savedFolder.TrimEnd('/', '\\'));
        bool fromWindows = rid.StartsWith("win", StringComparison.OrdinalIgnoreCase);
        List<(string Name, string Path)> files = Examples.Find(savedFolder)
            .Where(file => only == null || file.Name.Contains(only, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var report = new StringBuilder();
        void Line(string text)
        {
            Console.WriteLine(text);
            report.Append(text).Append('\n');
        }

        Line($"# upstream WPF RMC.BestFit.UI {typeof(BestFitProject).Assembly.GetName().Version} opens the files the port saved on {rid} " +
            $"({savedFolder}), .NET {Environment.Version}, culture {CultureInfo.CurrentCulture.Name}");
        if (files.Count == 0)
        {
            Line($"# no saved project files in {savedFolder}");
            return 1;
        }

        var tally = new Tally();
        string workRoot = Directory.CreateTempSubdirectory("xgoodfit-filematrix-").FullName;
        try
        {
            foreach ((string name, string path) in files)
            {
                var lines = new List<string>();
                int unexpected;
                try
                {
                    unexpected = name.StartsWith(ChangedFolder, StringComparison.Ordinal)
                        ? OpenChanged(name, path, fromWindows, workRoot, outputFolder, lines, tally)
                        : OpenRoundTripped(name, path, goldenFolder, fromWindows, workRoot, outputFolder, lines, tally);
                }
                catch (Exception ex)
                {
                    lines.Add($"  RMC-BestFit failed: {ex.GetType().Name}: {ex.Message}");
                    unexpected = 1;
                }
                tally.Files++;
                if (unexpected > 0) tally.FilesWithUnexpected++;
                Line($"{name}: {(unexpected == 0 ? "ok" : $"{unexpected} unexpected difference(s)")}");
                foreach (string line in lines) Line(line);
            }
        }
        finally
        {
            SQLiteConnection.ClearAllPools();
            Directory.Delete(workRoot, recursive: true);
        }

        Line($"# {tally.Files} file(s) saved by the port on {rid}, {tally.FilesWithUnexpected} with unexpected differences");
        Line($"#   opened to the state RMC-BestFit's own save opens to (changed files: the state the port opens them to): {tally.SameState} of {tally.Files}");
        Line($"#   saved again by RMC-BestFit exactly as it saves the example itself (changed files: as the port saved them): {tally.SameResave} of {tally.Files}");
        Line($"#   accepted platform differences in the port's saves of the examples: {tally.Kinds(tally.Saved)}");
        Line($"#   of those, still in RMC-BestFit's save of them: {tally.Kinds(tally.Survived)}");
        Directory.CreateDirectory(outputFolder);
        File.WriteAllText(Path.Combine(outputFolder, "summary.txt"), report.ToString(), new UTF8Encoding(false));
        return tally.FilesWithUnexpected == 0 ? 0 : 1;
    }

    /// <summary>Whether a difference is one of the accepted platform differences for a file saved on that OS.</summary>
    public static bool IsAccepted(DifferenceKind kind, bool fromWindows) => kind switch
    {
        DifferenceKind.Volatile or DifferenceKind.Compression => true,
        DifferenceKind.LineEnds or DifferenceKind.NarrowNoBreakSpace or DifferenceKind.PathSeparator or DifferenceKind.Numbers => !fromWindows,
        _ => false,
    };

    private static int OpenRoundTripped(string name, string path, string goldenFolder, bool fromWindows, string workRoot, string outputFolder,
        List<string> lines, Tally tally)
    {
        string goldenPath = Examples.DescriptionPath(goldenFolder, name);
        if (!File.Exists(goldenPath))
        {
            lines.Add($"  no golden description at {goldenPath}");
            return 1;
        }
        FileDescription golden = Examples.Read(goldenPath);
        int unexpected = 0;

        // 1. The file as the port saved it, against RMC-BestFit's save of the same example.
        List<Difference> saved = DescriptionComparison.Compare(golden, DescribeSaved(path, name, workRoot));
        unexpected += Report(lines, "saved by the port", "RMC-BestFit's save of the example", saved, fromWindows);
        tally.Add(tally.Saved, saved);

        // 2-4. RMC-BestFit opens it, saves it and opens that again.
        var reopenDetails = new List<string>();
        StateSnapshot? opened = null;
        FileDescription resaved = ProjectRoundTrip.SaveAndDescribe(name, path, workRoot, null, reopenDetails, state => opened = state);
        Examples.Write(resaved, Examples.DescriptionPath(outputFolder, name));

        List<(string Group, DifferenceKind Kind)> states = DescriptionComparison.CompareStates(golden.StatesAfterReopening(), resaved.States);
        int stateUnexpected = states.Count(s => !IsAccepted(s.Kind, fromWindows));
        if (states.Count == 0)
        {
            lines.Add($"  opened in RMC-BestFit: the state its own save opens to ({resaved.States.Count} groups)");
            tally.SameState++;
        }
        else
        {
            lines.Add($"  opened in RMC-BestFit: {states.Count} of {resaved.States.Count} groups differ from the state its own save opens to");
            lines.AddRange(states.Select(s => $"    {s.Kind}{(IsAccepted(s.Kind, fromWindows) ? "" : " (unexpected)")}: {s.Group}"));
            if (stateUnexpected > 0 && opened != null)
            {
                string statePath = Path.Combine(outputFolder, name + ".state.txt");
                opened.WriteValues(statePath);
                lines.Add($"    RMC-BestFit's state is in {statePath}; compare it with the state command's output for the example");
            }
        }
        unexpected += stateUnexpected;

        List<Difference> survived = DescriptionComparison.Compare(golden, resaved);
        unexpected += Report(lines, "saved again by RMC-BestFit", "its own save of the example", survived, fromWindows,
            "still there (the port's platform form survives RMC-BestFit's open and save)");
        if (survived.Count == 0) tally.SameResave++;
        tally.Add(tally.Survived, survived);

        unexpected += ReportReopen(lines, reopenDetails);
        return unexpected;
    }

    private static int OpenChanged(string name, string path, bool fromWindows, string workRoot, string outputFolder,
        List<string> lines, Tally tally)
    {
        string descriptionPath = path + ".txt", statePath = path + ".state.txt", reopenedPath = path + ".reopened.state.txt";
        foreach (string required in new[] { descriptionPath, statePath, reopenedPath })
        {
            if (!File.Exists(required))
            {
                lines.Add($"  the port's description or state is missing: {required}");
                return 1;
            }
        }
        FileDescription port = Examples.Read(descriptionPath);
        int unexpected = 0;

        // 1. The file is the one the port described (a check on the artifacts).
        List<Difference> saved = DescriptionComparison.Compare(port, DescribeSaved(path, name, workRoot));
        if (saved.Count > 0)
        {
            lines.Add("  the file differs from the port's description of it:");
            lines.Add(Indent(DescriptionComparison.Summarize(saved)));
            unexpected += saved.Count;
        }
        if (port.ReopenDifferences.Count > 0)
            lines.Add($"  not kept by saving (upstream's elements don't save them, so the port's reopening lost them too): " +
                string.Join(", ", port.ReopenDifferences));

        // 2. RMC-BestFit opens it to the state the port opens it to, value by value.
        var reopenDetails = new List<string>();
        StateSnapshot? opened = null;
        FileDescription resaved = ProjectRoundTrip.SaveAndDescribe(name, path, workRoot, null, reopenDetails, state => opened = state);
        Examples.Write(resaved, Examples.DescriptionPath(outputFolder, name));
        opened!.WriteValues(Path.Combine(outputFolder, name + ".state.txt"));

        var values = StateSnapshot.CompareValues(StateSnapshot.ReadValues(reopenedPath), opened.Values);
        int valueUnexpected = values.Count(v => !IsAccepted(v.Kind, fromWindows));
        if (values.Count == 0)
        {
            lines.Add($"  opened in RMC-BestFit: the state the port opens it to ({opened.Values.Count} values)");
            tally.SameState++;
        }
        else
        {
            lines.Add($"  opened in RMC-BestFit: {values.Count} of {opened.Values.Count} values differ from the state the port opens it to");
            lines.AddRange(values.Take(20).Select(v =>
                $"    {v.Kind}{(IsAccepted(v.Kind, fromWindows) ? "" : " (unexpected)")}: {v.Path}: port {Shorten(v.Expected)}, RMC-BestFit {Shorten(v.Actual)}"));
            if (values.Count > 20) lines.Add($"    ... and {values.Count - 20} more");
        }
        unexpected += valueUnexpected;

        // ... and so to the state the port had before closing it, apart from what neither application saves.
        var had = StateSnapshot.CompareValues(StateSnapshot.ReadValues(statePath), opened.Values)
            .Where(v => !port.ReopenDifferences.Contains(v.Path) && !values.Any(other => other.Path == v.Path)).ToList();
        if (had.Count > 0 && valueUnexpected == 0)
            lines.Add($"  {had.Count} more value(s) differ from the state the port had before closing it: " + string.Join(", ", had.Take(10).Select(v => v.Path)));

        // 3. RMC-BestFit's save of it, against the port's.
        List<Difference> resave = DescriptionComparison.Compare(port, resaved);
        unexpected += Report(lines, "saved again by RMC-BestFit", "the port's save", resave, fromWindows,
            "rewritten by RMC-BestFit in its own platform's form");
        if (resave.Count == 0) tally.SameResave++;

        unexpected += ReportReopen(lines, reopenDetails);
        return unexpected;
    }

    /// <summary>Reports the differences between two saves; returns how many are unexpected.</summary>
    private static int Report(List<string> lines, string what, string reference, List<Difference> differences, bool fromWindows,
        string acceptedNote = "")
    {
        if (differences.Count == 0)
        {
            lines.Add($"  {what}: identical to {reference}");
            return 0;
        }
        List<Difference> accepted = differences.Where(d => IsAccepted(d.Kind, fromWindows)).ToList();
        List<Difference> unexpected = differences.Where(d => !IsAccepted(d.Kind, fromWindows)).ToList();
        lines.Add($"  {what}: {differences.Count} cell(s) differ from {reference}, " +
            (unexpected.Count == 0 ? "all accepted platform differences" : $"{unexpected.Count} unexpected") +
            (accepted.Count > 0 && acceptedNote.Length > 0 ? $"; accepted ones {acceptedNote}" : ""));
        if (unexpected.Count > 0)
            lines.Add(Indent(DescriptionComparison.Summarize(unexpected, 5)));
        if (accepted.Count > 0)
            lines.Add(Indent(DescriptionComparison.Summarize(accepted, 0)));
        return unexpected.Count;
    }

    // DescriptionComparison.Summarize's lines, two more spaces in.
    private static string Indent(string summary) =>
        string.Join("\n", summary.TrimEnd('\n').Split('\n').Select(line => "  " + line));

    // RMC-BestFit's own save of a file it opened should open again to the same state.
    private static int ReportReopen(List<string> lines, List<string> reopenDetails)
    {
        if (reopenDetails.Count == 0)
        {
            lines.Add("  opened again after that save: no differences in memory");
            return 0;
        }
        lines.Add($"  opened again after that save: {reopenDetails.Count} difference(s) in memory (unexpected)");
        lines.AddRange(reopenDetails.Select(detail => "    " + detail));
        return reopenDetails.Count;
    }

    /// <summary>
    /// Describes a saved file as its own folder named it: the folder in its stored FullFileName (on the
    /// machine that saved it) becomes <c>{dir}</c>, as in the descriptions the suite wrote there.
    /// </summary>
    public static FileDescription DescribeSaved(string path, string name, string workRoot)
    {
        // A copy, because SQLite may create side files next to a database it opens (its journal is WAL).
        string copy = ProjectRoundTrip.CopyToWorkFolder(path, workRoot);
        try
        {
            string? stored = StoredFullFileName(copy);
            string? savedIn = stored == null ? null : stored.Substring(0, Math.Max(0, stored.LastIndexOfAny(new[] { '/', '\\' })));
            return FileDescription.Describe(copy, name, string.IsNullOrEmpty(savedIn) ? null : savedIn);
        }
        finally
        {
            ProjectRoundTrip.DeleteWorkFolder(copy);
        }
    }

    /// <summary>The project's own path, as stored in its Project table.</summary>
    public static string? StoredFullFileName(string file)
    {
        string connectionString = new SQLiteConnectionStringBuilder { DataSource = file, ReadOnly = true, Pooling = false }.ConnectionString;
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();
        using var command = new SQLiteCommand("SELECT FullFileName FROM Project LIMIT 1", connection);
        try
        {
            return command.ExecuteScalar() as string;
        }
        catch (SQLiteException)
        {
            return null;
        }
    }

    private static string Shorten(string value) => value.Length <= 100 ? value : value.Substring(0, 97) + "...";

    /// <summary>Counts over every file, for the summary.</summary>
    private sealed class Tally
    {
        public int Files, FilesWithUnexpected, SameState, SameResave;
        public readonly Dictionary<DifferenceKind, int> Saved = new(), Survived = new();

        public void Add(Dictionary<DifferenceKind, int> counts, IEnumerable<Difference> differences)
        {
            foreach (Difference difference in differences.Where(d => PlatformKinds.Contains(d.Kind)))
                counts[difference.Kind] = counts.GetValueOrDefault(difference.Kind) + 1;
        }

        public string Kinds(Dictionary<DifferenceKind, int> counts) =>
            string.Join(", ", PlatformKinds.Select(kind => $"{kind} {counts.GetValueOrDefault(kind).ToString(CultureInfo.InvariantCulture)} cell(s)"));
    }
}
