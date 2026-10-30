using System;
using Avalonia.Controls;
using AvalonDock;

namespace FrameworkUI
{
    /// <summary>
    /// Gives a docking manager the VS2013 dock theme of <see cref="ThemeManager"/>'s theme colour, and
    /// swaps it when <see cref="ThemeManager.ThemeChanged"/> is raised.
    /// </summary>
    /// <remarks>
    /// <para>
    /// XGoodFit addition. Upstream's MainWindow merged the dictionary of AvalonDock's VS2013 Light, Dark
    /// or Blue theme into its own resources in its ThemeChanged handler, removing the previous one. Dock's
    /// AvalonDock adapter applies a manager's <see cref="DockingManager.Theme"/> to the manager and to its
    /// floating, auto-hide, navigator and overlay windows, which in Avalonia don't share the main window's
    /// resources. So the dock theme (<see cref="Vs2013DockTheme"/>) is the manager's Theme, and assigning
    /// another one replaces the previous theme's dictionary everywhere.
    /// </para>
    /// <para>
    /// MainWindow's port: in ThemeChanged, replace the dictionary swap (the three pack URIs, and the
    /// removal and addition of <c>_avalonDockThemeDictionary</c>) with
    /// <c>DockThemeSwitcher.Apply(MainDock, newThemeColor)</c>, and drop the constructor's dummy
    /// <c>Vs2013BlueTheme</c>, which only made upstream copy the theme's assembly. Alternatively, create a
    /// <see cref="DockThemeSwitcher"/> for MainDock in the constructor, before the theme is set, and
    /// dispose of it in MainWindow_Closed, where upstream unsubscribes from ThemeChanged.
    /// </para>
    /// </remarks>
    public sealed class DockThemeSwitcher : IDisposable
    {
        private readonly DockingManager _manager;
        private bool _disposed;

        /// <summary>
        /// Gives a docking manager the dock theme of the current theme colour, and follows its changes
        /// until disposed of.
        /// </summary>
        /// <param name="manager">The docking manager to theme.</param>
        public DockThemeSwitcher(DockingManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            Apply(manager, ThemeManager.CurrentThemeColor);
            ThemeManager.ThemeChanged += ThemeChanged;
        }

        /// <summary>
        /// Creates the VS2013 dock theme of a theme colour.
        /// </summary>
        /// <param name="themeColor">The theme colour.</param>
        /// <returns>The dock theme, or null for a colour without one.</returns>
        public static Vs2013DockTheme? CreateTheme(ThemeColor themeColor)
        {
            switch (themeColor)
            {
                case ThemeColor.Blue:
                case ThemeColor.Dark:
                case ThemeColor.Light:
                    return new Vs2013DockTheme(themeColor);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gives a docking manager the VS2013 dock theme of a theme colour, replacing its theme. As upstream,
        /// an unknown colour keeps the current theme.
        /// </summary>
        /// <param name="manager">The docking manager to theme.</param>
        /// <param name="themeColor">The theme colour.</param>
        public static void Apply(DockingManager manager, ThemeColor themeColor)
        {
            ArgumentNullException.ThrowIfNull(manager);
            if (CreateTheme(themeColor) is { } theme)
            {
                manager.Theme = theme;
            }
        }

        /// <summary>
        /// Stops following theme changes. The manager keeps its current theme.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                ThemeManager.ThemeChanged -= ThemeChanged;
            }
        }

        private void ThemeChanged(ResourceDictionary newThemeDictionary, ThemeColor newThemeColor) =>
            Apply(_manager, newThemeColor);
    }
}
