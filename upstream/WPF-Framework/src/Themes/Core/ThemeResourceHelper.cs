using System;
using System.Collections.Generic;

namespace Themes
{
    /// <summary>
    /// Provides helper methods and constants for working with theme resources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class contains the URIs for all theme-related resource dictionaries
    /// and provides utility methods for theme resource management.
    /// </para>
    /// <para>
    ///     <b>Authors:</b>
    ///     <list type="bullet">
    ///         <item>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</item>
    ///     </list>
    /// </para>
    /// </remarks>
    public static class ThemeResourceHelper
    {
        #region Constants

        /// <summary>
        /// The pack URI for the merged control templates resource dictionary.
        /// </summary>
        /// <remarks>
        /// This dictionary contains all control template definitions that are
        /// theme-agnostic. Control templates use <c>DynamicResource</c> bindings
        /// to reference color keys defined in the color dictionaries.
        /// </remarks>
        public const string ControlTemplatesUri =
            "pack://application:,,,/Themes;component/Resources/Controls/Merged.xaml";

        /// <summary>
        /// The pack URI for the Light theme color dictionary.
        /// </summary>
        public const string LightColorsUri =
            "pack://application:,,,/Themes;component/Resources/Colors/LightColors.xaml";

        /// <summary>
        /// The pack URI for the Blue theme color dictionary.
        /// </summary>
        public const string BlueColorsUri =
            "pack://application:,,,/Themes;component/Resources/Colors/BlueColors.xaml";

        /// <summary>
        /// The pack URI for the Dark theme color dictionary.
        /// </summary>
        public const string DarkColorsUri =
            "pack://application:,,,/Themes;component/Resources/Colors/DarkColors.xaml";

        #endregion

        #region Static Fields

        /// <summary>
        /// Maps theme values to their corresponding color dictionary URIs.
        /// </summary>
        private static readonly Dictionary<Theme, string> ThemeColorUris = new Dictionary<Theme, string>
        {
            { Theme.Light, LightColorsUri },
            { Theme.Blue, BlueColorsUri },
            { Theme.Dark, DarkColorsUri }
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the color dictionary URI for the specified theme.
        /// </summary>
        /// <param name="theme">The theme to get the color dictionary URI for.</param>
        /// <returns>The pack URI string for the theme's color dictionary.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if an unknown theme value is provided.
        /// </exception>
        /// <example>
        /// <code>
        /// string uri = ThemeResourceHelper.GetColorDictionaryUri(Theme.Dark);
        /// // Returns: "pack://application:,,,/Themes;component/Resources/Colors/DarkColors.xaml"
        /// </code>
        /// </example>
        public static string GetColorDictionaryUri(Theme theme)
        {
            if (ThemeColorUris.TryGetValue(theme, out string uri))
            {
                return uri;
            }

            throw new ArgumentOutOfRangeException(nameof(theme), theme,
                $"Unknown theme value: {theme}. Valid values are: Light, Blue, Dark.");
        }

        /// <summary>
        /// Converts a theme name string to a <see cref="Theme"/> enumeration value.
        /// </summary>
        /// <param name="themeName">
        /// The name of the theme. Valid values are "Light", "Blue", or "Dark" (case-insensitive).
        /// </param>
        /// <returns>The corresponding <see cref="Theme"/> value.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="themeName"/> is null or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="themeName"/> is not a valid theme name.
        /// </exception>
        /// <example>
        /// <code>
        /// Theme theme = ThemeResourceHelper.ParseTheme("Dark");
        /// // Returns: Theme.Dark
        ///
        /// Theme theme2 = ThemeResourceHelper.ParseTheme("LIGHT");
        /// // Returns: Theme.Light
        /// </code>
        /// </example>
        public static Theme ParseTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                throw new ArgumentNullException(nameof(themeName),
                    "Theme name cannot be null or empty.");
            }

            if (Enum.TryParse(themeName, ignoreCase: true, out Theme result))
            {
                return result;
            }

            throw new ArgumentException(
                $"'{themeName}' is not a valid theme name. Valid values are: Light, Blue, Dark.",
                nameof(themeName));
        }

        /// <summary>
        /// Attempts to convert a theme name string to a <see cref="Theme"/> enumeration value.
        /// </summary>
        /// <param name="themeName">
        /// The name of the theme. Valid values are "Light", "Blue", or "Dark" (case-insensitive).
        /// </param>
        /// <param name="theme">
        /// When this method returns <c>true</c>, contains the parsed theme value.
        /// When this method returns <c>false</c>, contains <see cref="Theme.Light"/> as default.
        /// </param>
        /// <returns>
        /// <c>true</c> if the theme name was successfully parsed; otherwise, <c>false</c>.
        /// </returns>
        /// <example>
        /// <code>
        /// if (ThemeResourceHelper.TryParseTheme(userSetting, out Theme theme))
        /// {
        ///     ThemeService.Instance.SetTheme(theme);
        /// }
        /// else
        /// {
        ///     // Use default theme
        ///     ThemeService.Instance.SetTheme(Theme.Light);
        /// }
        /// </code>
        /// </example>
        public static bool TryParseTheme(string themeName, out Theme theme)
        {
            theme = Theme.Light;

            if (string.IsNullOrWhiteSpace(themeName))
            {
                return false;
            }

            // Enum.TryParse will succeed for any integer string (including values outside the enum
            // range), so we additionally check that the parsed value is a defined enum member.
            if (!Enum.TryParse(themeName, ignoreCase: true, out Theme parsed) ||
                !Enum.IsDefined(typeof(Theme), parsed))
            {
                theme = Theme.Light;
                return false;
            }

            theme = parsed;
            return true;
        }

        #endregion
    }
}
