using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace WpfCompat;

/// <summary>
/// WPF's <c>Application.MainWindow</c>, <c>Windows</c> and <c>Shutdown()</c>, from Avalonia's desktop
/// lifetime, so ported code can keep writing <c>Application.Current.MainWindow</c>.
/// </summary>
/// <remarks>
/// WPF makes the first window an application creates its main window; Avalonia needs the main window
/// set, which the ported App does at startup. Without a desktop lifetime (as in tests) there is no
/// window list, and the main window is the one last set, as WPF's property holds it.
/// </remarks>
public static class ApplicationExtensions
{
    // The main window set without a desktop lifetime.
    private static Window? s_mainWindow;

    extension(Application application)
    {
        /// <summary>The application's main window.</summary>
        public Window? MainWindow
        {
            get => Desktop(application) is { } desktop ? desktop.MainWindow : s_mainWindow;
            set
            {
                if (Desktop(application) is { } desktop)
                {
                    desktop.MainWindow = value;
                }
                else
                {
                    s_mainWindow = value;
                }
            }
        }

        /// <summary>The application's open windows.</summary>
        public IReadOnlyList<Window> Windows => Desktop(application)?.Windows ?? [];

        /// <summary>Closes the application.</summary>
        public void Shutdown() => Desktop(application)?.Shutdown();
    }

    private static IClassicDesktopStyleApplicationLifetime? Desktop(Application application) =>
        application.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
}
