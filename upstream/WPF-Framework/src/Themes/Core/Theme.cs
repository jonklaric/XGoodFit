namespace Themes
{
    /// <summary>
    /// Enumeration of available application themes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These themes follow the Visual Studio 2013 color palette design, providing
    /// consistent styling across all WPF controls in the application.
    /// </para>
    /// <para>
    ///     <b>Authors:</b>
    ///     <list type="bullet">
    ///         <item>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</item>
    ///     </list>
    /// </para>
    /// </remarks>
    public enum Theme
    {
        /// <summary>
        /// Light theme with a white/light gray color palette.
        /// Best suited for well-lit environments and standard office use.
        /// </summary>
        Light,

        /// <summary>
        /// Blue theme with blue accent colors and a professional appearance.
        /// Provides a balanced visual experience with blue highlights.
        /// </summary>
        Blue,

        /// <summary>
        /// Dark theme with a dark gray/black color palette.
        /// Best suited for low-light environments and extended screen use.
        /// </summary>
        Dark
    }
}
