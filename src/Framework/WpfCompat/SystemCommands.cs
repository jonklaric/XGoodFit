using Avalonia;
using Avalonia.Controls;
using WpfCompat.Input;

namespace WpfCompat;

/// <summary>
/// The window commands and actions that custom window chrome uses: WPF's
/// <c>System.Windows.SystemCommands</c>. A window that draws its own caption buttons binds these
/// commands to handlers that call the matching methods.
/// </summary>
public static class SystemCommands
{
    /// <summary>Closes a window.</summary>
    public static RoutedCommand CloseWindowCommand { get; } = new("CloseWindow", typeof(SystemCommands));

    /// <summary>Maximizes a window.</summary>
    public static RoutedCommand MaximizeWindowCommand { get; } = new("MaximizeWindow", typeof(SystemCommands));

    /// <summary>Minimizes a window.</summary>
    public static RoutedCommand MinimizeWindowCommand { get; } = new("MinimizeWindow", typeof(SystemCommands));

    /// <summary>Restores a window to its normal size.</summary>
    public static RoutedCommand RestoreWindowCommand { get; } = new("RestoreWindow", typeof(SystemCommands));

    /// <summary>Shows a window's system menu.</summary>
    public static RoutedCommand ShowSystemMenuCommand { get; } = new("ShowSystemMenu", typeof(SystemCommands));

    /// <summary>Closes <paramref name="window"/>.</summary>
    public static void CloseWindow(Window window) => window.Close();

    /// <summary>Maximizes <paramref name="window"/>.</summary>
    public static void MaximizeWindow(Window window) => window.WindowState = WindowState.Maximized;

    /// <summary>Minimizes <paramref name="window"/>.</summary>
    public static void MinimizeWindow(Window window) => window.WindowState = WindowState.Minimized;

    /// <summary>Restores <paramref name="window"/> to its normal size.</summary>
    public static void RestoreWindow(Window window) => window.WindowState = WindowState.Normal;

    /// <summary>
    /// Shows <paramref name="window"/>'s system menu. Avalonia has no cross-platform system menu, so
    /// this does nothing; upstream's windows don't call it.
    /// </summary>
    public static void ShowSystemMenu(Window window, Point screenLocation)
    {
    }
}
