using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Themes;
using WpfCompat;

namespace DatabaseControls.Tests;

/// <summary>
/// Renders the table viewer and its windows in every theme to <c>artifacts/screenshots</c>, to compare with
/// upstream's gallery screenshot of DatabaseControls.Demo
/// (<c>upstream/WPF-Framework/docs/assets/gallery/database-table-viewer-blue.png</c>, the demo with
/// <c>samples/synthetic-gallery-sample.csv</c> loaded). Run them alone with
/// <c>DatabaseControls.Tests --filter "TestCategory=Capture"</c>.
/// </summary>
[TestClass]
public sealed class Captures
{
    private static readonly string SampleCsv = Path.Combine(AppContext.BaseDirectory, "TestData", "synthetic-gallery-sample.csv");

    [TestMethod]
    [TestCategory("Capture")]
    public void Demo_WithTheGallerySample()
    {
        HeadlessSession.Run(() =>
        {
            UsePopupOverlay();
            string folder = ScreenshotFolder();
            foreach (Theme theme in Enum.GetValues<Theme>())
            {
                var demo = ShowDemo(theme);
                demo.LoadCsvFile(SampleCsv);
                Settle();
                Save(demo, Path.Combine(folder, $"database-demo-{theme}.png"));
                demo.Close();
            }
            ThemeService.Instance.SetTheme(Theme.Light);
        });
    }

    [TestMethod]
    [TestCategory("Capture")]
    public void TableViewerStates_AndItsWindows()
    {
        HeadlessSession.Run(() =>
        {
            UsePopupOverlay();
            string folder = ScreenshotFolder();
            foreach (Theme theme in Enum.GetValues<Theme>())
            {
                var demo = ShowDemo(theme);
                // The gallery sample with its Value column as numbers (the CSV reader reads text), so the
                // calculator and the statistics show numbers as with a database table.
                demo.TestViewer.DataView = new DatabaseManager.InMemoryReader(TypedGallerySample()).GetTableManager("synthetic-gallery-sample");
                Settle();
                TableViewer viewer = demo.TestViewer;

                // Rows selected from the row headers, the active cell, and the column menu.
                var rowHeaders = viewer.RowHeadersGrid.Children.OfType<Border>().ToArray();
                demo.Click(rowHeaders[2].Centre());
                demo.Click(rowHeaders[4].Centre(), Avalonia.Input.RawInputModifiers.Shift);
                demo.Click(viewer.CellCentre(6, 2));
                demo.RightClick(viewer.ColumnHeadersGrid.Children.OfType<Border>().ElementAt(2).Centre());
                Save(demo, Path.Combine(folder, $"database-viewer-selection-menu-{theme}.png"));
                demo.GetVisualDescendants().OfType<ContextMenu>().Single(m => m.IsOpen).Close();

                // A cell being edited.
                demo.Click(viewer.CellCentre(8, 0));
                demo.Type("DEMO-X");
                Save(demo, Path.Combine(folder, $"database-viewer-editing-{theme}.png"));
                demo.Press(Avalonia.Input.Key.Escape);

                var find = new FindAndReplace(viewer, 1, 0);
                find.Show();
                find.FindText.Text = "Group B";
                Save(find, Path.Combine(folder, $"database-find-{theme}.png"));
                find.Close();

                var calculator = new FieldCalculator(viewer.DataView, null, null);
                calculator.Show();
                Settle();
                calculator.NewFieldName.Text = "Doubled";
                calculator.ExpressionCalculator.SetExpressionText("[Value] * 2");
                Save(calculator, Path.Combine(folder, $"database-field-calculator-{theme}.png"));
                calculator.ExpressionCalculator.SetFunctionsToggle(true);
                Save(calculator, Path.Combine(folder, $"database-field-calculator-functions-{theme}.png"));
                calculator.Close();

                var selector = new FieldCalculator(viewer.DataView, new List<int> { 2, 3, 4 }, null, null, true);
                selector.Show();
                Settle();
                selector.ExpressionCalculator.SetExpressionText("[Value] > 20");
                Save(selector, Path.Combine(folder, $"database-select-by-attribute-{theme}.png"));
                selector.Close();

                var numericStats = new ColumnStatsWindow(viewer, 2);
                numericStats.Show();
                Save(numericStats, Path.Combine(folder, $"database-column-stats-numeric-{theme}.png"));
                numericStats.NumericColumnViewer.PropertiesControl.Visibility = Visibility.Visible;
                Settle();
                Save(numericStats, Path.Combine(folder, $"database-column-stats-numeric-properties-{theme}.png"));
                numericStats.Close();

                var textStats = new ColumnStatsWindow(viewer, 1);
                textStats.Show();
                Save(textStats, Path.Combine(folder, $"database-column-stats-text-{theme}.png"));
                textStats.Close();

                var help = new CalculatorHelpWindow();
                help.Show();
                Settle();
                Thread.Sleep(500); // HtmlRenderer loads the pictures on the thread pool.
                Save(help, Path.Combine(folder, $"database-calculator-help-{theme}.png"));
                help.Close();

                demo.Close();
            }
            ThemeService.Instance.SetTheme(Theme.Light);
        });
    }

    // The demo in a theme, picked with its own radio buttons, so they show the theme too.
    private static DatabaseControls.Demo.MainWindow ShowDemo(Theme theme)
    {
        ThemeService.Instance.SetTheme(theme);
        var demo = new DatabaseControls.Demo.MainWindow();
        demo.Show();
        (theme switch { Theme.Blue => demo.BlueThemeRadio, Theme.Dark => demo.DarkThemeRadio, _ => demo.LightThemeRadio }).IsChecked = true;
        Settle();
        return demo;
    }

    // upstream's gallery sample, typed: Value is a double.
    private static System.Data.DataTable TypedGallerySample()
    {
        var table = new System.Data.DataTable("synthetic-gallery-sample");
        string[] lines = File.ReadAllLines(SampleCsv);
        string[] names = lines[0].Split(',');
        foreach (string name in names)
        {
            table.Columns.Add(name, name == "Value" ? typeof(double) : typeof(string));
        }
        foreach (string line in lines.Skip(1))
        {
            string[] values = line.Split(',');
            table.Rows.Add(values.Select((v, i) => names[i] == "Value" ? (object)double.Parse(v, System.Globalization.CultureInfo.InvariantCulture) : v).ToArray());
        }
        return table;
    }

    internal static void UsePopupOverlay()
    {
        if (!Application.Current!.Styles.OfType<Style>().Any(s => s.Setters.OfType<Setter>().Any(x => x.Property == Popup.ShouldUseOverlayLayerProperty)))
        {
            // Draw popups (menus, context menus) in the window, so its frame includes them.
            Application.Current.Styles.Add(new Style(x => x.Is<Popup>())
            {
                Setters = { new Setter(Popup.ShouldUseOverlayLayerProperty, true) },
            });
            // Show the Metro title bar, which the window hides where the platform draws its own, as the
            // headless one does (as the theme gallery's captures).
            Application.Current.Styles.Add(new Style(x => x.Is<Window>().PropertyEquals(Window.IsExtendedIntoWindowDecorationsProperty, false).Template().OfType<Grid>().Name("TitleBar"))
            {
                Setters = { new Setter(Visual.IsVisibleProperty, true) },
            });
        }
    }

    internal static void Save(Window window, string path)
    {
        Settle();
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
        frame.Save(path, new PngBitmapEncoderOptions());
    }

    // Lets layout run and animations finish: the headless render timer uses real time.
    internal static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(200);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    // artifacts/screenshots under the repository root (git-ignored).
    internal static string ScreenshotFolder()
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
