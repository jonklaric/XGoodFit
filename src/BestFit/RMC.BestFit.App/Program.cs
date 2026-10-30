using System;
using Avalonia;
using Themes;
using XGoodFit.Platform;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit addition: the entry point, which WPF's build generated from App.xaml (upstream's
    /// StartupObject, RMC_BestFit.App).
    /// </summary>
    internal static class Program
    {
        /// <summary>Starts the application.</summary>
        /// <param name="args">The command line: a project to open, as upstream's.</param>
        /// <returns>The exit code.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            // Velopack's startup hook comes first: it handles the installer's and updater's runs of the
            // application, which exit at once (packaging/README.md). Outside a package there's none.
            PlatformStartup.BuildVelopackApp(args)?.Run();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return 0;
        }

        /// <summary>Configures the application (also for Avalonia's previewer).</summary>
        /// <returns>The application's builder.</returns>
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>().UsePlatformDetect().UseThemeFonts().LogToTrace();
    }
}
