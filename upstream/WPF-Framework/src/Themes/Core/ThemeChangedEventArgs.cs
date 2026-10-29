using System;
using System.Windows;

namespace Themes
{
    /// <summary>
    /// Provides data for the <see cref="IThemeService.ThemeChanged"/> event.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class contains information about a theme change, including the new theme
    /// and the associated color resource dictionary. Subscribers can use this information
    /// to perform additional theme-related updates that cannot be handled through
    /// <c>DynamicResource</c> bindings alone.
    /// </para>
    /// <para>
    ///     <b>Authors:</b>
    ///     <list type="bullet">
    ///         <item>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</item>
    ///     </list>
    /// </para>
    /// </remarks>
    public class ThemeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldTheme">The previous theme before the change.</param>
        /// <param name="newTheme">The new theme after the change.</param>
        /// <param name="colorDictionary">The resource dictionary containing the new theme colors.</param>
        public ThemeChangedEventArgs(Theme oldTheme, Theme newTheme, ResourceDictionary colorDictionary)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme;
            ColorDictionary = colorDictionary;
        }

        /// <summary>
        /// Gets the previous theme before the change.
        /// </summary>
        /// <value>The <see cref="Theme"/> that was active before the theme change.</value>
        public Theme OldTheme { get; }

        /// <summary>
        /// Gets the new theme after the change.
        /// </summary>
        /// <value>The <see cref="Theme"/> that is now active after the theme change.</value>
        public Theme NewTheme { get; }

        /// <summary>
        /// Gets the resource dictionary containing the new theme colors.
        /// </summary>
        /// <value>
        /// The <see cref="ResourceDictionary"/> containing color brush definitions for the new theme.
        /// This can be used by subscribers that need direct access to theme resources.
        /// </value>
        public ResourceDictionary ColorDictionary { get; }
    }
}
