// Copyright (c) USACE. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace FrameworkUI
{
    /// <summary>
    /// Enumeration of available theme colors for the ProjectUI library.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enumeration defines the available visual themes that can be applied
    /// to the ProjectUI library. Each theme follows the Visual Studio 2013 design
    /// language and provides a consistent color palette for all controls.
    /// </para>
    /// <para>
    /// Use <see cref="ThemeManager.SetTheme"/> to change the current theme at runtime.
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
    /// // Apply the dark theme
    /// ThemeManager.SetTheme(ThemeColor.Dark);
    /// </code>
    /// </example>
    public enum ThemeColor
    {
        /// <summary>
        /// Blue theme with blue accent colors and a light background.
        /// This theme is inspired by Visual Studio 2013 Blue theme.
        /// </summary>
        Blue,

        /// <summary>
        /// Dark theme with dark gray backgrounds and light text.
        /// This theme is inspired by Visual Studio 2013 Dark theme.
        /// </summary>
        Dark,

        /// <summary>
        /// Light theme with light gray backgrounds and dark text.
        /// This theme is inspired by Visual Studio 2013 Light theme.
        /// </summary>
        Light
    }
}
