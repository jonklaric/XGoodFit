using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace FrameworkUI
{
    /// <summary>
    /// Gives a root dock's panel the margin of the VS2013 dock's empty auto-hide sides: 4 pixels around a
    /// docking manager's panes, none in a floating window.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition, part of the VS2013 dock theme (Themes/VS2013/Dock/Generic.axaml). AvalonDock's
    /// VS2013 theme keeps each side of the docking manager at least 4 pixels wide, so the panes have a
    /// 4-pixel margin when no tool window is auto-hidden. Dock hides its empty sides with a binding a
    /// style can't override, so the theme gives the root dock's panel this margin instead; a side with
    /// auto-hidden tool windows overlaps it. The value is the root dock's window: floating windows' root
    /// docks have one, and floating windows have no sides.
    /// </remarks>
    public sealed class AnchorSideMarginConverter : IValueConverter
    {
        /// <summary>Gets the converter.</summary>
        public static AnchorSideMarginConverter Instance { get; } = new();

        /// <summary>Converts a root dock's window to its panel's margin.</summary>
        /// <param name="value">The root dock's window, null for a docking manager's root.</param>
        /// <param name="targetType">The target type (Thickness).</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>A 4-pixel margin for a docking manager's root, none for a floating window's.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is null ? new Thickness(4) : new Thickness(0);

        /// <summary>Not supported.</summary>
        /// <param name="value">Not used.</param>
        /// <param name="targetType">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Nothing: it throws.</returns>
        /// <exception cref="NotSupportedException">Always.</exception>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
