using Avalonia.Media;
using BestFitFileCompat;
using FrameworkInterfaces;
using Numerics.Utilities;
using OxyPlot.Wpf;
using RMC.BestFit.UI;

namespace RMC.BestFit.FileCompat.Tests;

/// <summary>
/// Changes a few example projects with the port before saving them, as a user does, and keeps the saved
/// files for upstream's WPF RMC-BestFit to open (tools/BestFitFileCompat.Wpf open-saved, CI's file matrix).
/// </summary>
/// <remarks>
/// The round trips in <see cref="FileCompatibilityTests"/> save what the examples hold. These save what the
/// port computed or made: a fit and a bootstrap it estimated, values it edited, an element and an
/// annotation it created. The model library is upstream's, compiled unchanged, so this checks the port's
/// UI layer (elements, plots and their serialization) with the file layer, headlessly, at the element
/// level. Each case keeps, under artifacts/filecompat/&lt;rid&gt;/changed/&lt;case&gt;/, the saved file under the
/// example's own file name (which is the project's name); its description, with the state the port had
/// after saving (state lines) and what the port's own reopening of it changed (reopen lines); and the
/// state's values before and after reopening (<c>.state.txt</c>, <c>.reopened.state.txt</c>), which
/// open-saved compares RMC-BestFit's state with, value by value.
/// </remarks>
[TestClass]
public class ChangedFileTests
{
    private const string RequireVariable = "XGOODFIT_REQUIRE_HECDSS";

    private static readonly string WorkRoot = Directory.CreateTempSubdirectory("xgoodfit-filecompat-changed-").FullName;

    /// <summary>The MSTest context, for the per-test report.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>The cases, by name.</summary>
    public static IEnumerable<object[]> Cases => ChangedCase.All.Select(c => new object[] { c.Name });

    /// <summary>
    /// Opens an example with the port, changes it, saves it, and keeps the file with the port's state
    /// before and after reopening it. The change must survive the port's own save and reopening.
    /// </summary>
    [TestMethod]
    [TestCategory("FileCompat")]
    [DynamicData(nameof(Cases))]
    public void ChangeAndSave_KeepsTheFileForRmcBestFit(string caseName)
    {
        ChangedCase change = ChangedCase.All.Single(c => c.Name == caseName);
        string example = Path.Combine(FileCompatibilityTests.ExamplesFolder, change.Example);
        if (!File.Exists(example))
        {
            string message = $"The file-compatibility tests need RMC-BestFit's example projects (scripts/fetch-sample-data.sh RMC-BestFit): {example}";
            if (Environment.GetEnvironmentVariable(RequireVariable) == "1") Assert.Fail(message);
            Assert.Inconclusive(message);
        }

        string name = $"{ChangedCase.Folder}/{change.Name}/{Path.GetFileName(change.Example)}";
        string kept = FileCompatibilityTests.SavedPath(name);
        (string what, string? problem, List<string> reopenDetails) = HeadlessSession.Await(async () =>
        {
            ProjectRoundTrip.UseCulture();
            string copy = ProjectRoundTrip.CopyToWorkFolder(example, WorkRoot);
            string folder = Path.GetDirectoryName(copy)!;
            try
            {
                BestFitProject project = ProjectRoundTrip.Open(copy);
                string what = await change.Apply(project);
                ProjectRoundTrip.SaveAll(project);
                StateSnapshot had = StateSnapshot.Take(project, folder);
                ProjectRoundTrip.Close(project);

                FileDescription description = FileDescription.Describe(copy, name, folder);
                description.States.AddRange(had.GroupHashes().Select(g => new[] { g.Key, g.Value.Full, g.Value.WithoutNumbers }));
                ProjectRoundTrip.Keep(copy, kept);

                BestFitProject reopened = ProjectRoundTrip.Open(copy);
                StateSnapshot opened = StateSnapshot.Take(reopened, folder);
                string? problem = change.Check(reopened);
                ProjectRoundTrip.Close(reopened);
                ProjectRoundTrip.AddReopened(description, had, opened);

                Examples.Write(description, kept + ".txt");
                had.WriteValues(kept + ".state.txt");
                opened.WriteValues(kept + ".reopened.state.txt");
                List<string> details = description.ReopenDifferences.Select(path =>
                    $"{path}: {had.Values.GetValueOrDefault(path, "(none)")} -> {opened.Values.GetValueOrDefault(path, "(none)")}").ToList();
                return (what, problem, details);
            }
            finally
            {
                ProjectRoundTrip.DeleteWorkFolder(copy);
            }
        });

        TestContext.WriteLine($"{name}: {what}");
        TestContext.WriteLine($"  kept in {kept}");
        TestContext.WriteLine($"  the port's reopening of its save changed {reopenDetails.Count} value(s)");
        foreach (string detail in reopenDetails) TestContext.WriteLine("    " + detail);
        Assert.IsNull(problem, "The port's save lost the change: " + problem);
        List<string> unsaved = reopenDetails.Where(detail => !IsNotSavedUpstream(detail, change)).ToList();
        Assert.AreEqual(0, unsaved.Count, "The port's save didn't keep these values:\n  " + string.Join("\n  ", unsaved));
    }

    /// <summary>
    /// Whether a value that differed after reopening is one upstream doesn't save, so that RMC-BestFit loses
    /// it too: the fractions of a second of a date stamped in this session (dates are stored as
    /// <c>DateTime.ToString()</c>, to the second), and what the case lists.
    /// </summary>
    private static bool IsNotSavedUpstream(string detail, ChangedCase change)
    {
        string path = detail.Substring(0, detail.IndexOf(": ", StringComparison.Ordinal));
        string[] values = detail.Substring(path.Length + 2).Split(" -> ");
        if ((path.EndsWith("/CreationDate", StringComparison.Ordinal) || path.EndsWith("/LastModified", StringComparison.Ordinal)) &&
            values.Length == 2 && values[0].Length > 19 && values[1].Length > 19 && values[0][..19] == values[1][..19])
            return true;
        return change.Unsaved.Any(suffix => path.EndsWith("/" + suffix, StringComparison.Ordinal));
    }

    /// <summary>Removes the work folder.</summary>
    [ClassCleanup]
    public static void DeleteWorkRoot()
    {
        try
        {
            Directory.Delete(WorkRoot, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>One change a user makes before saving.</summary>
    /// <param name="Name">The case's name, its folder under changed/.</param>
    /// <param name="Example">The example it changes, relative to the examples folder.</param>
    /// <param name="Apply">Makes the change, on the UI thread; returns what it did, for the report.</param>
    /// <param name="Check">Checks that the reopened project has the change; returns what's wrong, or null.</param>
    /// <param name="Unsaved">
    /// The ends of the state paths that may differ after reopening besides dates (<see cref="IsNotSavedUpstream"/>):
    /// what upstream's elements don't save, so RMC-BestFit doesn't either.
    /// </param>
    private sealed record ChangedCase(string Name, string Example, Func<BestFitProject, Task<string>> Apply,
        Func<BestFitProject, string?> Check, string[] Unsaved)
    {
        public const string Folder = "changed";

        // A Bulletin 17C run's diagnostics that its element doesn't save (upstream's B17CAnalysis.Save): its
        // timings, and its GMM optimizer's state (reopening starts the optimizer from the saved data again).
        private static readonly string[] BootstrapRunDiagnostics =
        {
            "ElapsedTime", "GMMElapsedTime", "UncertaintyElapsedTime", "GMM/ConvergenceHistory", "GMM/CovarianceStatus",
            "GMM/InitialValues", "GMM/Optimizer", "GMM/TotalFunctionEvaluations",
        };

        // Declared after the fields its initializer reads.
        public static readonly ChangedCase[] All =
        {
            // A new Distribution Fitting Analysis on the example's first Input Data, fitted by maximum likelihood.
            new("add-fitting-analysis", "2-input-data/1-block-maximum/usgs-block-max-example.bestfit",
                async project =>
                {
                    var fits = Collection<FittingAnalysisCollection>(project);
                    var input = (InputData)Collection<InputDataCollection>(project)[0];
                    var analysis = new FittingAnalysis($"Fitting Analysis_{fits.Count + 1}", fits);
                    fits.Add(analysis);
                    analysis.InputData = input;
                    await analysis.RunAsync(new SafeProgressReporter());
                    Assert.IsTrue(analysis.IsEstimated, "the new fitting analysis didn't estimate");
                    return $"added '{analysis.Name}' on '{input.Name}' and fitted {analysis.FittedDistributions.Count} distributions by maximum likelihood";
                },
                project => Collection<FittingAnalysisCollection>(project).OfType<FittingAnalysis>().SingleOrDefault() is { } fit
                    ? fit.IsEstimated && fit.InputData?.Name == ((InputData)Collection<InputDataCollection>(project)[0]).Name
                        ? null : $"'{fit.Name}' reopened with IsEstimated {fit.IsEstimated} on '{fit.InputData?.Name}'"
                    : "the new fitting analysis is missing",
                Array.Empty<string>()),

            // Bulletin 17C Example #1's bias-corrected bootstrap, estimated again (about 5 s).
            new("rerun-bootstrap", "4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/bulletin-17c-examples.bestfit",
                async project =>
                {
                    var analysis = (B17CAnalysis)Collection<UnivariateAnalysisCollection>(project).Single(e => e.Name == BootstrapName);
                    await analysis.RunAsync(new SafeProgressReporter());
                    Assert.IsTrue(analysis.IsEstimated, "the bootstrap didn't estimate");
                    return $"estimated '{analysis.Name}' again ({analysis.UncertaintyMethod})";
                },
                project => Collection<UnivariateAnalysisCollection>(project).Single(e => e.Name == BootstrapName) is B17CAnalysis { IsEstimated: true }
                    ? null : "the bootstrap reopened unestimated",
                BootstrapRunDiagnostics),

            // A value of a time series and its description, edited as in the Time Series Data table and the
            // properties panel (a description typed in a text box has the OS's line ends).
            new("edit-time-series", "1-time-series-data/6-manual-entry/manual-entry-example.bestfit",
                project =>
                {
                    var series = (TimeSeriesElement)Collection<TimeSeriesCollection>(project).Single(e => e.Name == SeriesName);
                    series.TimeSeries[0].Value += SeriesEdit;
                    _editedValue = series.TimeSeries[0].Value;
                    series.Description = EditedDescription;
                    return Task.FromResult($"added {SeriesEdit} to '{series.Name}' at {series.TimeSeries[0].Index:yyyy-MM-dd} and edited its description");
                },
                project => Collection<TimeSeriesCollection>(project).Single(e => e.Name == SeriesName) is TimeSeriesElement series &&
                    series.Description == EditedDescription && series.TimeSeries[0].Value == _editedValue
                    ? null : "the edited value or description didn't survive",
                Array.Empty<string>()),

            // A univariate analysis's frequency plot restyled, and a line annotation added, as with the plot's
            // properties panel and toolbar.
            new("edit-plot", "4-univariate-distribution-analysis/1-univariate-analysis/4-measurement-errors/sinnemahoning-move3-bayesian.bestfit",
                project =>
                {
                    var analysis = (UnivariateAnalysis)Collection<UnivariateAnalysisCollection>(project)[0];
                    Plot plot = analysis.FrequencyPlot;
                    plot.Title = PlotTitle;
                    plot.PlotAreaBackground = new SolidColorBrush(Color.FromArgb(0x40, 0x12, 0x34, 0x56));
                    plot.LegendBackground = Color.FromArgb(0x80, 0xFF, 0xEE, 0xDD);
                    plot.Series[0].Color = Colors.DarkOrange;
                    plot.Annotations.Add(new LineAnnotation
                    {
                        Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
                        Y = 25000,
                        Text = AnnotationText,
                        Color = Color.FromRgb(0xC0, 0x20, 0x20),
                        StrokeThickness = 2.5,
                    });
                    return Task.FromResult($"restyled '{analysis.Name}''s frequency plot ('{plot.Series[0].Title}' series) and added a line annotation");
                },
                project => Collection<UnivariateAnalysisCollection>(project)[0] is UnivariateAnalysis analysis &&
                    analysis.FrequencyPlot.Title == PlotTitle && analysis.FrequencyPlot.Annotations.OfType<LineAnnotation>().Any(a => a.Text == AnnotationText)
                    ? null : "the plot's title or annotation didn't survive",
                Array.Empty<string>()),
        };

        private const string BootstrapName = "Example #1 - BCB";
        private const string SeriesName = "Nile River Flows";
        private const double SeriesEdit = 12.5;
        private const string PlotTitle = "Edited in XGoodFit";
        private const string AnnotationText = "Levee crest (XGoodFit)";
        private static readonly string EditedDescription = "Edited in XGoodFit:" + Environment.NewLine + "the first flow is 12.5 higher.";
        private static double _editedValue = double.NaN;

        private static T Collection<T>(BestFitProject project) where T : IElementCollection => project.ElementCollections!.OfType<T>().Single();
    }
}
