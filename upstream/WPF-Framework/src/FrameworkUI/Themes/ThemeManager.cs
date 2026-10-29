// Copyright (c) USACE. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows;

namespace FrameworkUI
{
    /// <summary>
    /// A utility class for managing themes in FrameworkUI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides theme management for the ProjectUI library and bridges
    /// to the <see cref="Themes.ThemeService"/> for centralized theme management
    /// across all dependent libraries.
    /// </para>
    /// <para>
    /// When <see cref="SetTheme"/> is called, it:
    /// </para>
    /// <list type="number">
    ///     <item>Updates the <see cref="Themes.ThemeService"/> singleton (initializing it if needed)</item>
    ///     <item>Loads ProjectUI-specific theme resources</item>
    ///     <item>Raises the <see cref="ThemeChanged"/> event</item>
    /// </list>
    /// <para>
    /// Other libraries can subscribe to <see cref="Themes.ThemeService.ThemeChanged"/>
    /// to receive notifications when the application theme changes.
    /// </para>
    /// <para>
    ///     <b>Authors:</b>
    ///     <list type="bullet">
    ///         <item>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</item>
    ///     </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Set the application theme
    /// ThemeManager.SetTheme(ThemeColor.Dark);
    ///
    /// // Subscribe to theme changes
    /// ThemeManager.ThemeChanged += (dict, color) =>
    /// {
    ///     // Handle theme change
    /// };
    /// </code>
    /// </example>
    public class ThemeManager
    {
        #region Events

        /// <summary>
        /// The theme changed event handler delegate.
        /// </summary>
        /// <param name="newThemeDictionary">The new theme dictionary containing ProjectUI-specific resources.</param>
        /// <param name="newThemeColor">The new theme color.</param>
        public delegate void ThemeChangedEventHandler(ResourceDictionary newThemeDictionary, ThemeColor newThemeColor);

        /// <summary>
        /// Occurs when the theme has been changed.
        /// </summary>
        public static event ThemeChangedEventHandler? ThemeChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current theme color.
        /// </summary>
        /// <value>The current <see cref="ThemeColor"/>.</value>
        public static ThemeColor CurrentThemeColor { get; private set; } = ThemeColor.Light;

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the application theme to the specified theme color.
        /// </summary>
        /// <param name="theme">The theme color to set.</param>
        /// <remarks>
        /// <para>
        /// This method performs the following operations:
        /// </para>
        /// <list type="number">
        ///     <item>Initializes or updates the <see cref="Themes.ThemeService"/> singleton</item>
        ///     <item>Removes any previously loaded ProjectUI theme dictionaries</item>
        ///     <item>Loads the new ProjectUI-specific theme dictionary</item>
        ///     <item>Raises the <see cref="ThemeChanged"/> event</item>
        /// </list>
        /// <para>
        /// ProjectUI-specific resources include MainWindow styles, MessageWindow styles,
        /// and other UI components unique to this library.
        /// </para>
        /// </remarks>
        public static void SetTheme(ThemeColor theme)
        {
            // Marshal to UI thread when called from a worker. Mirrors the pattern in
            // Themes.ThemeService.SetTheme — ResourceDictionary mutation and the
            // raised ThemeChanged event must occur on the dispatcher thread.
            var app = Application.Current;
            if (app != null && !app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.Invoke(() => SetTheme(theme));
                return;
            }

            // Convert to Themes.Theme enum and update ThemeService
            var themesTheme = ConvertToThemesTheme(theme);

            // Initialize or set theme on ThemeService
            if (!Themes.ThemeService.Instance.IsInitialized)
            {
                Themes.ThemeService.Instance.Initialize(themesTheme);
            }
            else
            {
                Themes.ThemeService.Instance.SetTheme(themesTheme);
            }

            // Create a new resource dictionary for Framework-specific resources
            var themeDictionary = new ResourceDictionary();
            string blueString = "/FrameworkUI;component/Themes/VS2013/BlueTheme.xaml";
            string darkString = "/FrameworkUI;component/Themes/VS2013/DarkTheme.xaml";
            string lightString = "/FrameworkUI;component/Themes/VS2013/LightTheme.xaml";

            // Remove old ProjectUI theme dictionaries from the current application
            RemoveProjectUIThemeDictionaries(blueString, darkString, lightString);

            // Set the new theme dictionary source
            switch (theme)
            {
                case ThemeColor.Blue:
                    themeDictionary.Source = new Uri(blueString, UriKind.RelativeOrAbsolute);
                    break;
                case ThemeColor.Dark:
                    themeDictionary.Source = new Uri(darkString, UriKind.RelativeOrAbsolute);
                    break;
                case ThemeColor.Light:
                default:
                    themeDictionary.Source = new Uri(lightString, UriKind.RelativeOrAbsolute);
                    break;
            }

            // Reset the application theme dictionary
            Application.Current.Resources.MergedDictionaries.Add(themeDictionary);

            // Update current theme
            CurrentThemeColor = theme;

            // Raise the theme changed event
            ThemeChanged?.Invoke(themeDictionary, theme);
        }

        /// <summary>
        /// Converts a <see cref="ThemeColor"/> to a <see cref="Themes.Theme"/>.
        /// </summary>
        /// <param name="themeColor">The ProjectUI theme color.</param>
        /// <returns>The corresponding <see cref="Themes.Theme"/> value.</returns>
        public static Themes.Theme ConvertToThemesTheme(ThemeColor themeColor)
        {
            switch (themeColor)
            {
                case ThemeColor.Blue:
                    return Themes.Theme.Blue;
                case ThemeColor.Dark:
                    return Themes.Theme.Dark;
                case ThemeColor.Light:
                default:
                    return Themes.Theme.Light;
            }
        }

        /// <summary>
        /// Converts a <see cref="Themes.Theme"/> to a <see cref="ThemeColor"/>.
        /// </summary>
        /// <param name="theme">The Themes library theme.</param>
        /// <returns>The corresponding <see cref="ThemeColor"/> value.</returns>
        public static ThemeColor ConvertFromThemesTheme(Themes.Theme theme)
        {
            switch (theme)
            {
                case Themes.Theme.Blue:
                    return ThemeColor.Blue;
                case Themes.Theme.Dark:
                    return ThemeColor.Dark;
                case Themes.Theme.Light:
                default:
                    return ThemeColor.Light;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Removes existing ProjectUI theme dictionaries from the application resources.
        /// </summary>
        /// <param name="blueString">The blue theme URI string.</param>
        /// <param name="darkString">The dark theme URI string.</param>
        /// <param name="lightString">The light theme URI string.</param>
        private static void RemoveProjectUIThemeDictionaries(string blueString, string darkString, string lightString)
        {
            // Only consider dictionaries whose Source explicitly references FrameworkUI's pack URI segment.
            // Without the assembly-segment guard, EndsWith could match dictionaries with the same relative
            // suffix from a different assembly (e.g. RmcCore;component/Themes/VS2013/BlueTheme.xaml).
            const string assemblySegment = "/FrameworkUI;component/";
            for (int i = Application.Current.Resources.MergedDictionaries.Count - 1; i >= 0; i -= 1)
            {
                var source = Application.Current.Resources.MergedDictionaries[i].Source;
                if (source != null)
                {
                    var originalString = source.OriginalString;
                    if (!originalString.Contains(assemblySegment, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (originalString.EndsWith(blueString, StringComparison.OrdinalIgnoreCase) ||
                        originalString.EndsWith(darkString, StringComparison.OrdinalIgnoreCase) ||
                        originalString.EndsWith(lightString, StringComparison.OrdinalIgnoreCase))
                    {
                        Application.Current.Resources.MergedDictionaries.RemoveAt(i);
                    }
                }
            }
        }

        #endregion
    }
}
