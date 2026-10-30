using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Data.Converters;

namespace Themes
{
    /// <summary>
    /// Gives Avalonia the WPF property defaults that the ported XAML relies on.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition: WPF needs no such class. <see cref="ThemeService.Initialize"/> applies these
    /// defaults, together with the styles in Resources/Defaults.axaml, before any themed control is
    /// created.
    /// </remarks>
    public static class WpfDefaults
    {
        private static bool _applied;

        /// <summary>
        /// WPF's default font, Segoe UI, falling back to the embedded Selawik: windows have it (Resources/Defaults.axaml)
        /// and so do controls outside them. One instance, so that a control that leaves a window keeps an equal font.
        /// </summary>
        public static FontFamily SystemFontFamily { get; } = FontFamily.Parse("Segoe UI, avares://Themes/Assets/Fonts#Selawik");

        /// <summary>WPF's default font size, in device-independent pixels.</summary>
        public const double SystemFontSize = 12;

        /// <summary>
        /// Converts a shape's stroke brush to the stroke thickness WPF uses by default: 1 with a
        /// stroke, and none without one. Resources/Defaults.axaml uses it for rectangles and ellipses.
        /// </summary>
        public static IValueConverter StrokeThickness { get; } =
            new FuncValueConverter<object, double>(stroke => stroke is null ? 0.0 : 1.0);

        /// <summary>
        /// Changes the default values that differ between the frameworks. Safe to call more than once.
        /// </summary>
        /// <remarks>
        /// WPF shapes draw their stroke 1 pixel wide unless told otherwise. Avalonia's draw none,
        /// except <see cref="Line"/> and <see cref="Polyline"/>, so a ported shape with a stroke and
        /// no thickness would draw nothing. Paths and polygons get WPF's default here. Rectangles and
        /// ellipses get theirs from a style, as Avalonia insets their fill by half the stroke thickness
        /// even without a stroke, which WPF doesn't.
        /// </remarks>
        public static void Apply()
        {
            if (_applied)
            {
                return;
            }

            Shape.StrokeThicknessProperty.OverrideDefaultValue<Avalonia.Controls.Shapes.Path>(1.0);
            Shape.StrokeThicknessProperty.OverrideDefaultValue<Polygon>(1.0);

            // WPF's controls have the system font outside a window too, the font Resources/Defaults.axaml gives
            // windows. Avalonia's fall back to its own default: removing a control from its window (closing a
            // document) would change its font, and a plot series reports that as an edit of its element.
            TextElement.FontFamilyProperty.OverrideDefaultValue<Control>(SystemFontFamily);
            TextElement.FontSizeProperty.OverrideDefaultValue<Control>(SystemFontSize);
            _applied = true;
        }
    }
}
