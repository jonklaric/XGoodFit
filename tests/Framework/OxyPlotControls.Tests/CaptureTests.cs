using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Layout;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using Themes;
using static OxyPlotControls.Tests.PlotTesting;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls.Tests;

/// <summary>
/// Renders the toolbar, the properties pages and the image dialog in every theme to
/// <c>artifacts/screenshots</c>, to compare with upstream's gallery screenshots
/// (<c>upstream/WPF-Framework/docs/assets/gallery/oxyplot-*-blue.png</c>, the OxyPlotControls demo) and
/// BestFit's (<c>sample-data/RMC-BestFit/examples/1-time-series-data/1-usgs-download/screenshots</c>):
/// <list type="bullet">
/// <item><c>oxyplot-properties-{page}-{theme}.png</c>: the demo's layout (the line series plot, its
/// toolbar and the properties panel), at the gallery screenshot's 1080 x 580, on each page.</item>
/// <item><c>oxyplot-toolbar-{theme}.png</c>: a plot with its toolbar, 794 x 580 as the gallery's.</item>
/// <item><c>oxyplot-time-series-{theme}.png</c>: a time series plot like BestFit's, 1000 x 650.</item>
/// <item><c>oxyplot-annotation-menu-{theme}.png</c>, <c>oxyplot-save-image-{theme}.png</c>: the Add
/// Annotation menu and the Save Plot Image dialog.</item>
/// </list>
/// Run them alone with <c>OxyPlotControls.Tests --filter "TestCategory=Capture"</c>.
/// </summary>
[TestClass]
public sealed class CaptureTests
{
    private static readonly Theme[] AllThemes = [Theme.Light, Theme.Dark, Theme.Blue];

    private static readonly string[] Pages = ["general", "legend", "axes", "series", "annotations"];

    [TestMethod]
    [TestCategory("Capture")]
    public void CaptureTheDemo_OnEveryPage_InEveryTheme()
    {
        HeadlessSession.Run(() =>
        {
            string folder = ScreenshotFolder();
            using var overlay = OverlayPopups();
            foreach (Theme theme in AllThemes)
            {
                UseAppResources(theme);
                var plot = LinePlot();
                plot.Annotations.Add(new Wpf.TextAnnotation { Text = "Peak", TextPosition = new DataPoint(0, 0.45) });
                var (window, properties) = DemoWindow(plot);
                for (int page = 0; page < Pages.Length; page++)
                {
                    properties.PropertyControlComboBox.SelectedIndex = page;
                    Save(window, Path.Combine(folder, $"oxyplot-properties-{Pages[page]}-{Name(theme)}.png"));
                }
                window.Close();
            }
            UseAppResources(Theme.Light);
        });
    }

    [TestMethod]
    [TestCategory("Capture")]
    public void CaptureThePlotAndToolbar_InEveryTheme()
    {
        HeadlessSession.Run(() =>
        {
            string folder = ScreenshotFolder();
            using var overlay = OverlayPopups();
            foreach (Theme theme in AllThemes)
            {
                UseAppResources(theme);
                var (window, toolbar) = PlotWindow(LinePlot(), 794, 580);
                Save(window, Path.Combine(folder, $"oxyplot-toolbar-{Name(theme)}.png"));

                Click(window, toolbar.AddAnnotationToggleButton);
                Save(window, Path.Combine(folder, $"oxyplot-annotation-menu-{Name(theme)}.png"));
                window.Close();

                (window, _) = PlotWindow(TimeSeriesPlot(), 1000, 650);
                Save(window, Path.Combine(folder, $"oxyplot-time-series-{Name(theme)}.png"));
                window.Close();
            }
            UseAppResources(Theme.Light);
        });
    }

    [TestMethod]
    [TestCategory("Capture")]
    public void CaptureTheSaveImageDialog_InEveryTheme()
    {
        HeadlessSession.Run(() =>
        {
            string folder = ScreenshotFolder();
            foreach (Theme theme in AllThemes)
            {
                UseAppResources(theme);
                var (window, _) = PlotWindow(LinePlot(), 794, 580);
                var dialog = new SavePlotImageDialog(window.GetVisualDescendants().OfType<Wpf.Plot>().Single()) { Owner = window };
                dialog.Show(window);
                Settle();
                Save(dialog, Path.Combine(folder, $"oxyplot-save-image-{Name(theme)}.png"));
                dialog.Close();
                window.Close();
            }
            UseAppResources(Theme.Light);
        });
    }

    private static string Name(Theme theme) => theme.ToString().ToLowerInvariant();

    // The demo's main content: the plot and its toolbar in a bordered box, a splitter's gap, and the
    // properties panel under its "Plot Properties" header (upstream's OxyPlotControls.Demo MainWindow).
    private static (Window Window, OxyPlotPropertiesControl Properties) DemoWindow(Wpf.Plot plot)
    {
        plot.BorderBrush = Brushes.Transparent;
        plot.BorderThickness = new Thickness(0);
        var toolbar = new OxyPlotToolbar { Plot = plot, ToolBarOrientation = Orientation.Vertical, Margin = new Thickness(4), VerticalAlignment = VerticalAlignment.Top };
        Grid.SetColumn(toolbar, 1);
        var plotHost = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { plot, toolbar } };
        var plotBorder = new Border { BorderThickness = new Thickness(1), Child = plotHost };
        plotBorder.Bind(Border.BorderBrushProperty, plotBorder.GetResourceObservable("Button.Static.Border"));

        var header = new Border
        {
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(10, 5),
            Child = new TextBlock { Text = "Plot Properties", FontSize = 13, FontWeight = FontWeight.SemiBold },
        };
        header.Bind(Border.BackgroundProperty, header.GetResourceObservable("EnvironmentWindowBackground"));
        header.Bind(Border.BorderBrushProperty, header.GetResourceObservable("Button.Static.Border"));
        ((TextBlock)header.Child).Bind(TextBlock.ForegroundProperty, header.GetResourceObservable("EnvironmentWindowText"));
        var properties = new OxyPlotPropertiesControl { Plot = plot, ShowCloseButton = true, Margin = new Thickness(5, 0) };
        Grid.SetRow(properties, 1);
        var panel = new Border
        {
            BorderThickness = new Thickness(1),
            Child = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Children = { header, properties } },
        };
        panel.Bind(Border.BorderBrushProperty, panel.GetResourceObservable("Button.Static.Border"));
        panel.Bind(Border.BackgroundProperty, panel.GetResourceObservable("PropertyGrid.Content.Background"));
        Grid.SetColumn(panel, 2);

        var content = new Grid
        {
            Margin = new Thickness(10),
            ColumnDefinitions = new ColumnDefinitions("*,6,280"),
            Children = { plotBorder, panel },
        };
        var root = new Border { Child = content };
        root.Bind(Border.BackgroundProperty, root.GetResourceObservable("EnvironmentWindowBackground"));
        Window window = Show(root, 1080, 580);
        return (window, properties);
    }

    // A plot with its toolbar on the right, filling a window of the size given.
    private static (Window Window, OxyPlotToolbar Toolbar) PlotWindow(Wpf.Plot plot, double width, double height)
    {
        var toolbar = new OxyPlotToolbar { Plot = plot, ToolBarOrientation = Orientation.Vertical, Margin = new Thickness(4), VerticalAlignment = VerticalAlignment.Top };
        Grid.SetColumn(toolbar, 1);
        var root = new Border { Child = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { plot, toolbar } } };
        root.Bind(Border.BackgroundProperty, root.GetResourceObservable("EnvironmentWindowBackground"));
        return (Show(root, width, height), toolbar);
    }

    // A daily flow record like BestFit's USGS example: dates on the bottom axis, flow on the left, the
    // series in the legend at the top left.
    private static Wpf.Plot TimeSeriesPlot()
    {
        var plot = new Wpf.Plot { Title = "Time Series", IsLegendVisible = true, LegendPosition = LegendPosition.TopLeft };
        plot.Axes.Add(new Wpf.DateTimeAxis { Position = AxisPosition.Bottom, Title = "Date", MajorGridlineStyle = LineStyle.Solid, MinorGridlineStyle = LineStyle.None });
        plot.Axes.Add(new Wpf.LinearAxis { Position = AxisPosition.Left, Title = "Flow (cfs)", MajorGridlineStyle = LineStyle.Solid, StringFormat = "N0" });
        var random = new Random(1134500);
        var points = new List<DataPoint>();
        var start = new DateTime(1947, 1, 1);
        double flow = 200;
        for (int day = 0; day < 365 * 79; day += 3)
        {
            flow = Math.Max(20, flow * 0.9 + 25 + (random.NextDouble() < 0.04 ? random.NextDouble() * 3500 : random.NextDouble() * 60));
            points.Add(new DataPoint(DateTimeAxis.ToDouble(start.AddDays(day)), flow));
        }
        plot.Series.Add(new Wpf.LineSeries { Title = "Time Series Data", ItemsSource = points, Color = Color.FromRgb(0x5A, 0xC8, 0xFA) });
        return plot;
    }

    // Draws popups (the toolbar's menu) in the window, so its frame includes them (see ThemeGallery).
    private static IDisposable OverlayPopups()
    {
        var style = new Style(x => x.Is<Popup>()) { Setters = { new Setter(Popup.ShouldUseOverlayLayerProperty, true) } };
        Application.Current!.Styles.Add(style);
        return new Removal(() => Application.Current.Styles.Remove(style));
    }

    private sealed class Removal(Action remove) : IDisposable
    {
        public void Dispose() => remove();
    }

    private static void Save(Window window, string path)
    {
        // The headless render timer uses real time: let animations (the hover fades) finish.
        Settle();
        Thread.Sleep(250);
        Settle();
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
        frame.Save(path, new PngBitmapEncoderOptions());
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
