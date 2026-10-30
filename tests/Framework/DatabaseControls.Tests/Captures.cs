using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Themes;

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
                ThemeService.Instance.SetTheme(theme);
                var demo = new DatabaseControls.Demo.MainWindow();
                demo.Show();
                Settle();
                demo.LoadCsvFile(SampleCsv);
                Settle();
                Save(demo, Path.Combine(folder, $"database-demo-{theme}.png"));
                demo.Close();
            }
            ThemeService.Instance.SetTheme(Theme.Light);
        });
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
