using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BestFitFileCompat;

namespace RMC.BestFit.FileCompat.Tests;

/// <summary>
/// Opens and saves each example project with the port, and compares the saved file and the in-memory
/// state with what upstream's WPF RMC-BestFit gives for the same file (see the project file).
/// </summary>
[TestClass]
public class FileCompatibilityTests
{
    /// <summary>The environment variable that turns missing example projects into a failure (as in CI).</summary>
    private const string RequireVariable = "XGOODFIT_REQUIRE_HECDSS";

    internal static readonly string ExamplesFolder = Metadata("BestFitExamplesDir");

    private static readonly string GoldenFolder = Path.Combine(AppContext.BaseDirectory, "Golden");

    /// <summary>Where the port's descriptions and saved files go: artifacts/filecompat/&lt;rid&gt;/ (see <see cref="WriteSummary"/>).</summary>
    internal static readonly string OutputFolder = Path.Combine(Metadata("FileCompatOutputDir"), RuntimeInformation.RuntimeIdentifier);

    private static readonly ConcurrentDictionary<string, string> Reports = new(StringComparer.Ordinal);

    /// <summary>The MSTest context, for the per-test report.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>The examples the golden data describes.</summary>
    public static IEnumerable<object[]> GoldenExamples => Examples.DescribedNames(GoldenFolder).Select(name => new object[] { name });

    /// <summary>
    /// Opens and saves an example with the port. The saved file must hold what upstream's WPF RMC-BestFit
    /// saves, cell by cell; the example must open to the state it opens to in WPF; and the saved file must
    /// open again to that same state, apart from the differences WPF shows too.
    /// </summary>
    /// <remarks>
    /// Text, plot XML included, and numbers compare exactly. Upstream's volatile cells aren't compared (it
    /// saved none). On macOS and Linux, differences that disappear in the canonical form are expected and
    /// reported, not failed: line ends (XElement.ToString() writes Environment.NewLine), ICU's narrow
    /// no-break space in dates, path separators; so are in-memory statistics that differ only in their
    /// numbers, which the platform's maths library computes. On Windows all of these fail. Compressed
    /// bytes may differ with the runtime's zlib, as long as what they hold doesn't.
    /// </remarks>
    [TestMethod]
    [TestCategory("FileCompat")]
    [DynamicData(nameof(GoldenExamples), DynamicDataDisplayName = nameof(DisplayName))]
    public void OpenSave_WritesWhatWpfWrites(string name)
    {
        string example = Path.Combine(ExamplesFolder, name);
        if (!File.Exists(example))
        {
            string message = $"The file-compatibility tests need RMC-BestFit's example projects (scripts/fetch-sample-data.sh RMC-BestFit): {example}";
            if (Environment.GetEnvironmentVariable(RequireVariable) == "1") Assert.Fail(message);
            Assert.Inconclusive(message);
        }

        FileDescription expected = Examples.Read(Examples.DescriptionPath(GoldenFolder, name));
        var reopenDetails = new List<string>();
        StateSnapshot? openedState = null;
        FileDescription actual = HeadlessSession.Run(() => SaveAndDescribe(name, example, null, reopenDetails, state => openedState = state,
            keepAs: SavedPath(name)));
        Examples.Write(actual, Examples.DescriptionPath(OutputFolder, name));

        bool onWindows = OperatingSystem.IsWindows();
        List<Difference> differences = DescriptionComparison.Compare(expected, actual);
        List<Difference> failures = differences.Where(d => d.Kind switch
        {
            DifferenceKind.Volatile or DifferenceKind.Compression => false,
            DifferenceKind.LineEnds or DifferenceKind.NarrowNoBreakSpace or DifferenceKind.PathSeparator => onWindows,
            _ => true,
        }).ToList();
        List<(string Group, DifferenceKind Kind)> states = DescriptionComparison.CompareStates(expected, actual);
        List<string> stateFailures = states
            .Where(s => s.Kind == DifferenceKind.Mismatch || (s.Kind == DifferenceKind.Numbers && onWindows))
            .Select(s => $"{s.Kind}: {s.Group}")
            .ToList();
        List<(string Group, DifferenceKind Kind)> reopenedStates = DescriptionComparison.CompareReopenedStates(expected, actual);
        List<string> reopenedFailures = reopenedStates
            .Where(s => s.Kind == DifferenceKind.Mismatch || (s.Kind == DifferenceKind.Numbers && onWindows))
            .Select(s => $"{s.Kind}: {s.Group}")
            .ToList();
        bool sameReopen = expected.ReopenDifferences.SequenceEqual(actual.ReopenDifferences);

        string report = Report(name, expected, actual, differences, states, reopenedStates, reopenDetails);
        Reports[name] = report;
        TestContext.WriteLine(report);

        if (failures.Count > 0)
            DumpCells(name, example, failures);
        if (stateFailures.Count > 0 && openedState != null)
        {
            openedState.WriteValues(Path.Combine(OutputFolder, name + ".state.txt"));
        }

        Assert.AreEqual(0, failures.Count, "The port saved something WPF doesn't:\n" + DescriptionComparison.Summarize(failures, 10) +
            $"The port's description and the content of the differing cells are in {OutputFolder}.");
        Assert.AreEqual(0, stateFailures.Count, "The port opened the example to a different in-memory state from WPF's:\n  " +
            string.Join("\n  ", stateFailures) + $"\nThe port's state is in {OutputFolder}; tools/BestFitFileCompat.Wpf's state command writes WPF's.");
        Assert.IsTrue(sameReopen, "Opening the port's save again changed the in-memory state differently from WPF:\n" + report);
        Assert.AreEqual(0, reopenedFailures.Count, "The port's save opened again to a different in-memory state from WPF's save:\n  " +
            string.Join("\n  ", reopenedFailures));
    }

    /// <summary>
    /// Where the port's save of an example is kept: beside its description, under the example's own path,
    /// so that tools/BestFitFileCompat.Wpf's open-saved command can open it with RMC-BestFit (CI's file matrix).
    /// </summary>
    internal static string SavedPath(string name) => Path.Combine(OutputFolder, name);

    /// <summary>The test's name in reports: the example's file name.</summary>
    public static string DisplayName(MethodInfo method, object[] data) => $"{method.Name} ({Path.GetFileName((string)data[0])})";

    /// <summary>Writes every test's report to artifacts/filecompat/&lt;rid&gt;/summary.txt, which CI shows.</summary>
    [ClassCleanup]
    public static void WriteSummary()
    {
        if (!Reports.IsEmpty)
        {
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText(Path.Combine(OutputFolder, "summary.txt"),
                $"File compatibility on {RuntimeInformation.RuntimeIdentifier} ({RuntimeInformation.OSDescription}), .NET {Environment.Version}\n" +
                string.Concat(Reports.OrderBy(r => r.Key, StringComparer.Ordinal).Select(r => r.Value)), new UTF8Encoding(false));
        }
        try
        {
            Directory.Delete(WorkRoot, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private static string Report(string name, FileDescription expected, FileDescription actual, List<Difference> differences,
        List<(string Group, DifferenceKind Kind)> states, List<(string Group, DifferenceKind Kind)> reopenedStates, List<string> reopenDetails)
    {
        var report = new StringBuilder();
        report.Append($"{name}: {actual.Tables.Count} tables, {actual.CellCount} cells, port sha {actual.Fingerprint}, WPF sha {expected.Fingerprint}\n");
        report.Append(differences.Count == 0 ? "  saved file: identical to WPF's\n" : "  saved file:\n" + DescriptionComparison.Summarize(differences));
        report.Append(states.Count == 0
            ? $"  opened state: identical to WPF's ({actual.States.Count} groups)\n"
            : "  opened state:\n" + string.Concat(states.Select(s => $"    {s.Kind}: {s.Group}\n")));
        report.Append($"  reopened: {actual.ReopenDifferences.Count} difference(s) in memory; WPF had {expected.ReopenDifferences.Count}\n");
        foreach (string detail in reopenDetails)
            report.Append($"    {detail}{(expected.ReopenDifferences.Any(path => detail.StartsWith(path + ": ", StringComparison.Ordinal)) ? "" : " (port only)")}\n");
        foreach (string path in expected.ReopenDifferences.Except(actual.ReopenDifferences))
            report.Append($"    {path} (WPF only)\n");
        foreach ((string group, DifferenceKind kind) in reopenedStates)
            report.Append($"    {kind}: reopened state of {group}\n");
        return report.ToString();
    }

    /// <summary>Runs <see cref="ProjectRoundTrip.SaveAndDescribe"/> in the suite's culture and work folder. Call it on the UI thread.</summary>
    private static FileDescription SaveAndDescribe(string name, string example, Action<string, byte[]>? store,
        List<string>? reopenDetails, Action<StateSnapshot>? opened = null, string? keepAs = null)
    {
        ProjectRoundTrip.UseCulture();
        return ProjectRoundTrip.SaveAndDescribe(name, example, WorkRoot, store, reopenDetails, opened, keepAs);
    }

    /// <summary>Saves the example again, writing the content of the differing cells to the output folder for review.</summary>
    private void DumpCells(string name, string example, List<Difference> failures)
    {
        var wanted = new HashSet<string>(failures.SelectMany(f => Hashes(f.Actual)), StringComparer.Ordinal);
        if (wanted.Count == 0) return;
        Action<string, byte[]> store = Examples.ContentStore(Path.Combine(OutputFolder, "cells"));
        try
        {
            HeadlessSession.Run(() => SaveAndDescribe(name, example, (sha, content) =>
            {
                if (wanted.Contains(sha)) store(sha, content);
            }, null));
        }
        catch (Exception ex)
        {
            TestContext.WriteLine("Could not save the differing cells: " + ex.Message);
        }
    }

    // The hashes in a token (t#len:sha/canonical:flags, b#len:sha>z#len:sha).
    private static IEnumerable<string> Hashes(string token) =>
        token.Split('>').Select(part => part.Split('/')[0]).Where(part => part.Contains('#') && part.Contains(':')).Select(part => part.Split(':')[1]);

    private static string WorkRoot { get; } = Directory.CreateTempSubdirectory("xgoodfit-filecompat-").FullName;

    private static string Metadata(string key) =>
        typeof(FileCompatibilityTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == key).Value!;
}
