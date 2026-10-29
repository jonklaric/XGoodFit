using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Themes;

namespace ThemeGallery;

// Usage: ThemeGallery                    show the gallery
//        ThemeGallery capture <folder>   render it in every theme to <folder>/theme-gallery-<page>-<Theme>.png
internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "capture")
        {
            return Capture(args[1]);
        }

        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    private static int Capture(string folder)
    {
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHarfBuzz()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();

        // Draw popups (menus, context menus) in the window, so its frame includes them.
        Application.Current!.Styles.Add(new Style(x => x.Is<Popup>())
        {
            Setters = { new Setter(Popup.ShouldUseOverlayLayerProperty, true) },
        });

        Directory.CreateDirectory(folder);
        var window = new GalleryWindow();
        window.Show();
        foreach (Theme theme in Enum.GetValues<Theme>())
        {
            window.SelectTheme(theme);

            window.Pages.SelectedItem = window.ControlsPage;
            Save(window, folder, "controls", theme);

            window.FileMenu.Open();
            Settle();
            window.RecentMenu.Open();
            Save(window, folder, "menu", theme);
            window.MainMenu.Close();

            window.SampleTextBox.ContextMenu!.Placement = PlacementMode.Bottom;
            window.SampleTextBox.ContextMenu.Open(window.SampleTextBox);
            Save(window, folder, "context-menu", theme);
            window.SampleTextBox.ContextMenu.Close();

            window.SampleComboBox.IsDropDownOpen = true;
            Save(window, folder, "combo-box", theme);
            window.SampleComboBox.IsDropDownOpen = false;

            window.Pages.SelectedItem = window.PalettePage;
            Save(window, folder, "palette", theme);
        }
        window.Close();
        return 0;
    }

    private static void Save(Window window, string folder, string page, Theme theme)
    {
        Settle();
        string path = Path.Combine(folder, $"theme-gallery-{page}-{theme}.png");
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
        frame.Save(path, new PngBitmapEncoderOptions());
        Console.WriteLine(path);
    }

    // Lets layout run and fade-in animations finish: the headless render timer uses real time.
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(300);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}

internal sealed class App : Application
{
    public override void Initialize() => Styles.Add(new SimpleTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        ThemeService.Instance.Initialize(Theme.Light);
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new GalleryWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
