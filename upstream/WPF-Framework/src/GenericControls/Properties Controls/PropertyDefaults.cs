using System.Windows;

namespace GenericControls
{
    /// <summary>
    /// Provides default constants for property control dimensions and widths.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class PropertyDefaults
    {
        /// <summary>
        /// Default maximum width for property controls.
        /// </summary>
        public static double DefaultMaxPropertyWidth = double.PositiveInfinity;
        /// <summary>
        /// Default minimum width for property controls.
        /// </summary>
        public static double DefaultMinPropertyWidth = 22d;
        /// <summary>
        /// Default width for property controls.
        /// </summary>
        public static GridLength DefaultPropertyWidth = new GridLength(10d, GridUnitType.Star);

        /// <summary>
        /// Default height for property controls.
        /// </summary>
        public static double DefaultPropertyHeight = 22d;
    }
}