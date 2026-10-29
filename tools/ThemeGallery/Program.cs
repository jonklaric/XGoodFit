using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Themes;

namespace ThemeGallery;

// Usage: ThemeGallery                    show the gallery
//        ThemeGallery capture <folder>   render it in every theme to <folder>/theme-gallery-<Theme>.png
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

        Directory.CreateDirectory(folder);
        var window = new GalleryWindow { SizeToContent = SizeToContent.Height };
        window.Show();
        foreach (Theme theme in Enum.GetValues<Theme>())
        {
            window.SelectTheme(theme);
            Dispatcher.UIThread.RunJobs();
            string path = Path.Combine(folder, $"theme-gallery-{theme}.png");
            using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
            frame.Save(path, new PngBitmapEncoderOptions());
            Console.WriteLine(path);
        }
        window.Close();
        return 0;
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
