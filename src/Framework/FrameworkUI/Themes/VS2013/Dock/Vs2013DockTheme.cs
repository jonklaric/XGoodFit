using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvalonDock.Themes;

namespace FrameworkUI
{
    /// <summary>
    /// The VS2013 dock theme of a theme colour, for <see cref="AvalonDock.DockingManager.Theme"/>.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition. Upstream's MainWindow docked with AvalonDock and gave it AvalonDock's VS2013
    /// Light, Dark or Blue theme. XGoodFit docks with Dock's AvalonDock adapter, whose controls this theme
    /// gives the same look (Themes/VS2013/Dock/Generic.axaml). Dock's adapter merges the theme's dictionary
    /// into the manager's resources and those of its floating, auto-hide, navigator and overlay windows,
    /// and assigning another theme replaces it everywhere. See <see cref="DockThemeSwitcher"/>.
    /// </remarks>
    public sealed class Vs2013DockTheme : DictionaryTheme
    {
        /// <summary>
        /// Creates the dock theme of a theme colour, loading its dictionary.
        /// </summary>
        /// <param name="themeColor">The theme colour: Light, Dark or Blue.</param>
        /// <exception cref="ArgumentOutOfRangeException">The colour has no dock theme.</exception>
        public Vs2013DockTheme(ThemeColor themeColor)
            : base((ResourceDictionary)AvaloniaXamlLoader.Load(ResourceUriOf(themeColor)))
        {
            ThemeColor = themeColor;
        }

        /// <summary>
        /// Gets the theme colour.
        /// </summary>
        public ThemeColor ThemeColor { get; }

        /// <summary>
        /// Gets the URI of the theme's dictionary.
        /// </summary>
        /// <returns>The dictionary's avares URI.</returns>
        public override Uri GetResourceUri() => ResourceUriOf(ThemeColor);

        /// <summary>
        /// Gets the URI of the dictionary of a theme colour's dock theme.
        /// </summary>
        /// <param name="themeColor">The theme colour: Light, Dark or Blue.</param>
        /// <returns>The dictionary's avares URI.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The colour has no dock theme.</exception>
        public static Uri ResourceUriOf(ThemeColor themeColor) => themeColor switch
        {
            ThemeColor.Light or ThemeColor.Dark or ThemeColor.Blue => new Uri($"avares://FrameworkUI/Themes/VS2013/Dock/{themeColor}Theme.axaml"),
            _ => throw new ArgumentOutOfRangeException(nameof(themeColor), themeColor, "The theme colour has no dock theme."),
        };
    }
}
