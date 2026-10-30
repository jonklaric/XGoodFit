using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using GenericControls;
using Themes;
using WpfCompat;

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

        // Show the Metro title bar, which the window hides where the platform draws its own, as the
        // headless one does.
        Application.Current.Styles.Add(new Style(x => x.Is<Window>().PropertyEquals(Window.IsExtendedIntoWindowDecorationsProperty, false).Template().OfType<Grid>().Name("TitleBar"))
        {
            Setters = { new Setter(Visual.IsVisibleProperty, true) },
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

            var messageBox = new MessageBoxWindow();
            messageBox.Configure("Save changes to the project before closing?", "XGoodFit", MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question, MessageBoxResult.None, showSuppressCheckBox: true);
            Save(messageBox, folder, "message-box", theme, show: true);

            // An existing name: the dialog shows it's invalid.
            Save(new NameDialog(64, "Analysis 1", false, ["Analysis 1"]), folder, "name-dialog", theme, show: true);
        }
        window.Close();
        return 0;
    }

    private static void Save(Window window, string folder, string page, Theme theme, bool show = false)
    {
        if (show)
        {
            window.Show();
        }
        Settle();
        string path = Path.Combine(folder, $"theme-gallery-{page}-{theme}.png");
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
        frame.Save(path, new PngBitmapEncoderOptions());
        Console.WriteLine(path);
        if (show)
        {
            window.Close();
        }
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
    public override void Initialize()
    {
        Styles.Add(new SimpleTheme());

        // As upstream's App.xaml: GenericControls' theme, then ThemeService adds Themes after it.
        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://ThemeGallery"))
        {
            Source = new Uri("avares://GenericControls/Themes/GenericControlsTheme.axaml"),
        });
    }

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
