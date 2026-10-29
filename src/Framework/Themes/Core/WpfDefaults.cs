using System;
using System.Globalization;
using Avalonia.Controls.Shapes;
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
            _applied = true;
        }
    }
}
