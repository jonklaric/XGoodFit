using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using RMC.BestFit.UI;

namespace BestFitFileCompat;

/// <summary>
/// The file-compatibility suite's reference: what upstream's WPF RMC-BestFit saves (see the project file).
/// </summary>
public static class Program
{
    private const string Usage =
        "usage:\n" +
        "  BestFitFileCompat.Wpf reference <examples folder> <descriptions folder> [--runs N] [--only TEXT] [--dump FOLDER]\n" +
        "      Opens and saves each example with upstream's WPF RMC.BestFit.UI, N times (default 2), and\n" +
        "      writes a description of the saved file and of the in-memory state before and after\n" +
        "      reopening it; anything that differs between the runs is volatile.\n" +
        "      --dump writes the content of every hashed cell of the first run to FOLDER/<hash>.txt.\n" +
        "  BestFitFileCompat.Wpf describe <examples folder> <descriptions folder> [--only TEXT] [--dump FOLDER]\n" +
        "      Describes the examples as they are, without opening them in RMC-BestFit.\n" +
        "  BestFitFileCompat.Wpf compare <expected descriptions> <actual descriptions>\n" +
        "      Compares two folders of descriptions, cell by cell. Fails on mismatches and reopen differences.\n" +
        "  BestFitFileCompat.Wpf state <examples folder> <output folder> [--only TEXT]\n" +
        "      Writes the in-memory state each example opens to, value by value (what the state hashes hash).\n" +
        "Every command runs in the en-US culture, the examples', unless --culture NAME names another.";

    /// <summary>Runs a command (see <see cref="Usage"/>).</summary>
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }
        Console.Out.NewLine = "\n";
        ProjectRoundTrip.UseCulture(Option(args, "--culture") ?? ProjectRoundTrip.CultureName);
        string? only = Option(args, "--only");
        string? dump = Option(args, "--dump");
        return args[0] switch
        {
            "reference" => Reference(args[1], args[2], int.Parse(Option(args, "--runs") ?? "2", CultureInfo.InvariantCulture), only, dump),
            "describe" => Describe(args[1], args[2], only, dump),
            "compare" => Compare(args[1], args[2]),
            "state" => State(args[1], args[2], only),
            _ => Fail(Usage),
        };
    }

    private static int Reference(string examplesFolder, string outputFolder, int runs, string? only, string? dump)
    {
        IReadOnlyList<(string Name, string Path)> examples = Select(examplesFolder, only);
        Console.WriteLine($"# upstream WPF RMC.BestFit.UI {typeof(BestFitProject).Assembly.GetName().Version}, .NET {Environment.Version}, " +
            $"culture {CultureInfo.CurrentCulture.Name}, time zone {TimeZoneInfo.Local.Id}");
        string workRoot = Directory.CreateTempSubdirectory("xgoodfit-filecompat-wpf-").FullName;
        Action<string, byte[]>? store = dump == null ? null : Examples.ContentStore(Path.GetFullPath(dump));
        var descriptions = new FileDescription[examples.Count];
        var reopenDetails = new List<string>[examples.Count];
        var seconds = new double[examples.Count];
        try
        {
            for (int run = 0; run < runs; run++)
            {
                for (int i = 0; i < examples.Count; i++)
                {
                    var watch = Stopwatch.StartNew();
                    var details = new List<string>();
                    FileDescription description = ProjectRoundTrip.SaveAndDescribe(examples[i].Name, examples[i].Path, workRoot, run == 0 ? store : null, details);
                    seconds[i] += watch.Elapsed.TotalSeconds;
                    if (run == 0)
                    {
                        descriptions[i] = description;
                        reopenDetails[i] = details;
                        continue;
                    }
                    foreach (Difference volatileCell in DescriptionComparison.MarkVolatile(descriptions[i], description))
                        Console.WriteLine($"  volatile: {examples[i].Name}: {volatileCell.Location}: {volatileCell.Expected} / {volatileCell.Actual}");
                    if (!descriptions[i].ReopenDifferences.SequenceEqual(description.ReopenDifferences))
                        Console.WriteLine($"  the reopen differences of {examples[i].Name} changed between runs");
                }
                Console.WriteLine($"# run {run + 1} of {runs} done");
                if (run == 0) Console.WriteLine("# " + LoadedAssemblies());
            }
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
        }

        for (int i = 0; i < examples.Count; i++)
        {
            FileDescription description = descriptions[i];
            Examples.Write(description, Examples.DescriptionPath(outputFolder, description.Name));
            int volatileCells = description.Tables.Sum(t => t.Rows.Sum(r => r.Cells.Count(c => c.StartsWith('~'))));
            Console.WriteLine($"{description.Name}\ttables={description.Tables.Count}\tcells={description.CellCount}\tvolatile={volatileCells}\t" +
                $"reopen={description.ReopenDifferences.Count}\tsha={description.Fingerprint}\t{seconds[i] / runs:F1}s");
            foreach (string detail in reopenDetails[i])
                Console.WriteLine($"  differs after reopening: {detail}");
        }
        return 0;
    }

    /// <summary>The versions and builds of the libraries that did the work, for the record.</summary>
    private static string LoadedAssemblies() =>
        string.Join(", ", AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name is "RMC.BestFit" or "Numerics" or "FrameworkInterfaces" or "DatabaseManager" or "OxyPlot" or "OxyPlot.Wpf" or "System.Data.SQLite")
            .OrderBy(a => a.GetName().Name, StringComparer.Ordinal)
            .Select(a => $"{a.GetName().Name} {a.GetName().Version} ({FileVersionInfo.GetVersionInfo(a.Location).ProductVersion})"));

    private static int Describe(string examplesFolder, string outputFolder, string? only, string? dump)
    {
        string workRoot = Directory.CreateTempSubdirectory("xgoodfit-filecompat-describe-").FullName;
        Action<string, byte[]>? store = dump == null ? null : Examples.ContentStore(Path.GetFullPath(dump));
        try
        {
            foreach ((string name, string path) in Select(examplesFolder, only))
            {
                // A copy, because SQLite may create side files next to a database it opens.
                string copy = ProjectRoundTrip.CopyToWorkFolder(path, workRoot);
                FileDescription description = FileDescription.Describe(copy, name, null, store);
                ProjectRoundTrip.DeleteWorkFolder(copy);
                Examples.Write(description, Examples.DescriptionPath(outputFolder, name));
                Console.WriteLine($"{name}\ttables={description.Tables.Count}\tcells={description.CellCount}\tsha={description.Fingerprint}");
            }
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
        }
        return 0;
    }

    private static int Compare(string expectedFolder, string actualFolder)
    {
        int different = 0, failed = 0;
        IReadOnlyList<string> expectedNames = Examples.DescribedNames(expectedFolder);
        foreach (string name in expectedNames.Union(Examples.DescribedNames(actualFolder)).OrderBy(n => n, StringComparer.Ordinal))
        {
            string expectedPath = Examples.DescriptionPath(expectedFolder, name), actualPath = Examples.DescriptionPath(actualFolder, name);
            if (!File.Exists(expectedPath) || !File.Exists(actualPath))
            {
                Console.WriteLine($"{name}: only in {(File.Exists(expectedPath) ? "expected" : "actual")}");
                different++;
                failed++;
                continue;
            }
            FileDescription expected = Examples.Read(expectedPath), actual = Examples.Read(actualPath);
            List<Difference> differences = DescriptionComparison.Compare(expected, actual);
            List<(string Group, DifferenceKind Kind)> states = DescriptionComparison.CompareStates(expected, actual);
            bool sameReopen = expected.ReopenDifferences.SequenceEqual(actual.ReopenDifferences);
            if (differences.Count == 0 && states.Count == 0 && sameReopen)
            {
                Console.WriteLine($"{name}: identical");
                continue;
            }
            different++;
            if (!sameReopen || differences.Any(d => d.Kind == DifferenceKind.Mismatch) || states.Any(s => s.Kind == DifferenceKind.Mismatch))
                failed++;
            Console.WriteLine($"{name}: {differences.Count} difference(s) in the file, {states.Count} in the state it opens to");
            Console.Write(DescriptionComparison.Summarize(differences));
            foreach ((string group, DifferenceKind kind) in states)
                Console.WriteLine($"  {kind}: state of {group}");
            if (!sameReopen)
                Console.WriteLine($"  reopen differences: expected [{string.Join(", ", expected.ReopenDifferences)}], actual [{string.Join(", ", actual.ReopenDifferences)}]");
        }
        Console.WriteLine($"# {different} of {expectedNames.Count} differ, {failed} with mismatches");
        return failed == 0 ? 0 : 1;
    }

    private static int State(string examplesFolder, string outputFolder, string? only)
    {
        string workRoot = Directory.CreateTempSubdirectory("xgoodfit-filecompat-state-").FullName;
        try
        {
            foreach ((string name, string path) in Select(examplesFolder, only))
            {
                string copy = ProjectRoundTrip.CopyToWorkFolder(path, workRoot);
                BestFitProject project = ProjectRoundTrip.Open(copy);
                StateSnapshot state = StateSnapshot.Take(project, Path.GetDirectoryName(copy));
                ProjectRoundTrip.Close(project);
                ProjectRoundTrip.DeleteWorkFolder(copy);
                string output = Path.Combine(outputFolder, name + ".state.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                File.WriteAllLines(output, state.Values.Select(entry => entry.Key + "\t" + entry.Value));
                Console.WriteLine($"{name}\t{state.Values.Count} values");
            }
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
        }
        return 0;
    }

    private static IReadOnlyList<(string Name, string Path)> Select(string examplesFolder, string? only) =>
        Examples.Find(Path.GetFullPath(examplesFolder))
            .Where(example => only == null || example.Name.Contains(only, StringComparison.OrdinalIgnoreCase))
            .ToList();

    private static string? Option(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 2;
    }
}
