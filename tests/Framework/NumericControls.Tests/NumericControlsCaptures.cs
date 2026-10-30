using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GenericControls;
using Numerics.Data;
using Numerics.Distributions;
using NumericControls.Distributions.Univariate;
using Themes;
using static NumericControls.Tests.TestSupport;

namespace NumericControls.Tests;

/// <summary>
/// Renders the controls in every theme to <c>artifacts/screenshots</c>, to compare with upstream's gallery
/// screenshots (<c>upstream/WPF-Framework/docs/assets/gallery/numeric-*-blue.png</c>), which show
/// NumericControls.Demo's pages: <c>numeric-*-{theme}.png</c> has a page's control with the demo's data and
/// about the screenshot's size, without the page's heading. The other files show the controls as RMC-BestFit
/// sets them up, and states the screenshots don't show. They format in en-US, as the screenshots' machine
/// did (the time series' dates follow the current culture, as upstream's do). Run them alone with
/// <c>NumericControls.Tests --filter "TestCategory=Capture"</c>.
/// </summary>
[TestClass]
public sealed class NumericControlsCaptures
{
    private static readonly Theme[] AllThemes = [Theme.Light, Theme.Blue, Theme.Dark];

    [TestMethod]
    [TestCategory("Capture")]
    public void CaptureDistributionSelector_InEveryTheme()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            foreach (Theme theme in AllThemes)
            {
                UseAppResources(theme);
                string name = Name(theme);

                // The demo's selector: a Normal(100, 15) and a thousand of its values, the statistics expanded.
                var selector = new DistributionSelectorControl
                {
                    Width = 400,
                    BorderThickness = new Thickness(1),
                    ShowAxisTitle = true,
                    ShowTickLines = true,
                    ShowAxisLabel = true,
                    DistributionTitle = "Sample Distribution",
                };
                selector.Bind(TemplatedControl.BorderBrushProperty, selector.GetResourceObservable("Button.Static.Border"));
                selector.Bind(DistributionSelectorControl.BackgroundColorProperty, selector.GetResourceObservable("EnvironmentWindowBackground"));
                selector.SelectedDistribution = new Normal(100, 15);
                selector.SampleData = new Normal(100, 15).GenerateRandomValues(1000, 12345);
                selector.FindControl<Expander>("SummaryStatisticstExpander")!.IsExpanded = true;
                var scroll = new ScrollViewer { Content = selector, HorizontalAlignment = HorizontalAlignment.Left, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden };
                Window window = Page(scroll, 430, 840);
                Save(window, $"numeric-distribution-selector-{name}.png");
                window.Close();

                // RMC-BestFit's popup (InputDataControl, the priors): 300 wide, with its priors. It sets ExpandPlot
                // False, which upstream's control doesn't use: the plot stays expanded.
                var popup = new DistributionSelectorControl
                {
                    Margin = new Thickness(5),
                    Width = 300,
                    ShowAxisTitle = true,
                    ShowTickLines = true,
                    ShowAxisLabel = true,
                    ExpandPlot = false,
                    ShowPlot = true,
                    ShowStatistics = true,
                    DistributionTitle = "Uncertain Data Point",
                    Distributions =
                    [
                        new GammaDistribution(), new GeneralizedBeta(), new LnNormal(), new LogNormal(), new Normal(),
                        new Pert(), new StudentT(), new Triangular(), new TruncatedNormal(), new Uniform(),
                    ],
                };
                popup.Bind(DistributionSelectorControl.BackgroundColorProperty, popup.GetResourceObservable("EnvironmentWindowBackground"));
                popup.SelectedDistribution = new Normal(100, 15);
                var border = new Border { BorderThickness = new Thickness(1), Child = popup, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                border.Bind(Border.BackgroundProperty, border.GetResourceObservable("EnvironmentWindowBackground"));
                border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("Button.Static.Border"));
                window = Page(border, 330, 560);
                Save(window, $"numeric-distribution-selector-popup-{name}.png");

                // A parameter edited out of range: the cell's error, and the message in place of the plot.
                DistributionSelectorControlTests.EditParameter(window, popup, 1, "-15");
                Save(window, $"numeric-distribution-selector-invalid-{name}.png");
                window.Close();
            }
            UseAppResources(Theme.Light);
        });
    }

    [TestMethod]
    [TestCategory("Capture")]
    public void CaptureProbabilityOrdinates_InEveryTheme()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            foreach (Theme theme in AllThemes)
            {
                UseAppResources(theme);
                string name = Name(theme);

                // The demo's default ordinates.
                var control = new ProbabilityOrdinatesControl { ProbabilityOrdinates = new ProbabilityOrdinates() };
                Window window = Page(control, 1085, 607, margin: 0);
                Save(window, $"numeric-probability-ordinates-{name}.png");
                window.Close();

                // In a panel as narrow as RMC-BestFit's properties, with ordinates out of order and out of range,
                // and a cell selected.
                control = new ProbabilityOrdinatesControl { ProbabilityOrdinates = new ProbabilityOrdinates(new[] { 0.5, 0.1, 1.5, 0.01 }) };
                window = Page(control, 330, 260, margin: 0);
                var grid = control.GetVisualDescendants().OfType<ValidationDataGrid>().Single();
                grid.Focus();
                Select(grid, (1, 0));
                Save(window, $"numeric-probability-ordinates-invalid-{name}.png");
                window.Close();
            }
            UseAppResources(Theme.Light);
        });
    }

    [TestMethod]
    [TestCategory("Capture")]
    public void CaptureTimeSeriesTable_InEveryTheme()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            foreach (Theme theme in AllThemes)
            {
                UseAppResources(theme);
                string name = Name(theme);

                // The demo's hourly series.
                var table = new TimeSeriesTable
                {
                    XColumnHeader = "Date",
                    YColumnHeader = "Data",
                    Series = new TimeSeries(TimeInterval.OneHour, new DateTime(1980, 7, 30), new DateTime(1980, 8, 1), 15),
                };
                Window window = Page(table, 1085, 490);
                Save(window, $"numeric-time-series-{name}.png");
                window.Close();

                // As RMC-BestFit's time series editor sets it up, with values missing and a value cell selected.
                table = new TimeSeriesTable
                {
                    XColumnHeader = "Date Time",
                    YColumnHeader = "Value",
                    Series = new TimeSeries(TimeInterval.OneDay, new DateTime(1949, 1, 1), [112, 118, double.NaN, 129, 121, 135, 148, 148]),
                };
                window = Page(table, 500, 330);
                var grid = table.GetVisualDescendants().OfType<ValidationDataGrid>().Single();
                grid.Focus();
                Select(grid, (1, 2), (3, 2));
                Save(window, $"numeric-time-series-bestfit-{name}.png");

                // The math functions' popup, open.
                table.FindControl<ToggleButton>("PresetToggleButton")!.IsChecked = true;
                Settle();
                var popup = table.FindControl<Popup>("MathPopup")!;
                if (popup.Child is { } child && TopLevel.GetTopLevel(child) is { } root)
                {
                    Save(root, $"numeric-time-series-math-popup-{name}.png");
                }
                table.FindControl<ToggleButton>("PresetToggleButton")!.IsChecked = false;
                Settle();
                window.Close();

                // The operand dialog, as Multiply opens it.
                var dialog = new NumericEntryDialog { Title = "Multiply" };
                dialog.NameLabel.Text = "x * ";
                dialog.Show();
                Settle();
                Save(dialog, $"numeric-entry-dialog-{name}.png");
                dialog.Close();
            }
            UseAppResources(Theme.Light);
        });
    }

    [TestMethod]
    [TestCategory("Capture")]
    public void CaptureUncertainOrderedTableEditor_InEveryTheme()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            foreach (Theme theme in AllThemes)
            {
                UseAppResources(theme);
                string name = Name(theme);

                // The demo's table (its Y in no order), with its Normal distribution chosen.
                (double X, double Mean)[] values = [(0, 30), (2, 24), (4, 19), (6, 15), (8, 12), (10, 10)];
                var editor = new UncertainOrderedTableEditor { OrderY = SortOrder.None, XColumnHeader = "Depth", YColumnHeader = "Velocity" };
                editor.UncertainOrderedData = Data(values);
                Window window = Page(editor, 1085, 250);
                Save(window, $"numeric-uncertain-table-editor-{name}.png");
                window.Close();

                // As RMC-BestFit's bivariate analysis sets it up: no merged header, centred headers, a value
                // out of order.
                editor = new UncertainOrderedTableEditor
                {
                    IsMergedHeaderVisible = false,
                    OrderX = SortOrder.Ascending,
                    OrderY = SortOrder.Ascending,
                    XColumnHeader = "X",
                    YColumnHeader = "Y",
                };
                editor.Bind(UncertainOrderedTableEditor.ColumnHeaderStyleProperty, editor.GetResourceObservable("Center_ColumnHeaderStyle"));
                editor.UncertainOrderedData = new UncertainOrderedPairedData(
                    new[] { (1.0, 10.0), (2, 12), (3, 11), (4, 15) }.Select(v => new UncertainOrdinate(v.Item1, new Normal(v.Item2, 1))).ToList(),
                    true, SortOrder.Ascending, true, SortOrder.Ascending, UnivariateDistributionType.Normal);
                window = Page(editor, 400, 220, margin: 0);
                Save(window, $"numeric-uncertain-table-editor-bestfit-{name}.png");
                window.Close();
            }
            UseAppResources(Theme.Light);
        });
    }

    private static UncertainOrderedPairedData Data((double X, double Mean)[] values) =>
        new(values.Select(v => new UncertainOrdinate(v.X, new Normal(v.Mean, 1))).ToList(),
            false, SortOrder.Ascending, false, SortOrder.Descending, UnivariateDistributionType.Normal);

    private static string Name(Theme theme) => theme.ToString().ToLowerInvariant();

    // A window of this size showing the content on the theme's window background, as the demo's pages do.
    // (TestSupport.Show sets the Light theme.)
    private static Window Page(Control content, double width, double height, double margin = 10)
    {
        var page = new Border { Padding = new Thickness(margin), Child = content };
        page.Bind(Border.BackgroundProperty, page.GetResourceObservable("EnvironmentWindowBackground"));
        var window = new Window { Width = width, Height = height, Content = page };
        window.Show();
        window.Activate();
        Settle();
        return window;
    }

    private static void Save(TopLevel topLevel, string file)
    {
        // Lets layout run and the plot render: the headless render timer uses real time.
        Settle();
        Thread.Sleep(200);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        using var frame = topLevel.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
        frame.Save(Path.Combine(ScreenshotFolder(), file), new PngBitmapEncoderOptions());
    }

    // artifacts/screenshots under the repository root (git-ignored).
    private static string ScreenshotFolder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "XGoodFit.slnx")))
        {
            directory = directory.Parent;
        }
        string folder = Path.Combine(directory?.FullName ?? AppContext.BaseDirectory, "artifacts", "screenshots");
        Directory.CreateDirectory(folder);
        return folder;
    }
}
