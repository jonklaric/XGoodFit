// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LinearColorAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.LinearColorAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia;
    using Avalonia.Metadata;
    using Avalonia.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.LinearColorAxis"/>.
    /// </summary>
    /// <remarks>
    /// A linear color axis maps numeric values to colors using a color palette.
    /// This is commonly used with heat maps and scatter series with color-coded points.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the XAML content
    /// property is marked on the property (Avalonia's <c>[Content]</c>) rather than on the class; the
    /// gradient stops are Avalonia's <c>GradientStops</c>.
    /// </remarks>
    public class LinearColorAxis : Axis
    {
        /// <summary>
        /// Identifies the <see cref="GradientStops"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<GradientStops> GradientStopsProperty = AvaloniaProperty.Register<LinearColorAxis, GradientStops>(
            nameof(GradientStops),
            null);

        /// <summary>
        /// Identifies the <see cref="HighColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> HighColorProperty = AvaloniaProperty.Register<LinearColorAxis, Color>(
            nameof(HighColor),
            Colors.White);

        /// <summary>
        /// Identifies the <see cref="LowColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> LowColorProperty = AvaloniaProperty.Register<LinearColorAxis, Color>(
            nameof(LowColor),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="PaletteSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<int> PaletteSizeProperty = AvaloniaProperty.Register<LinearColorAxis, int>(
            nameof(PaletteSize),
            20,
            validate: v => ValidatePaletteSize(v));

        /// <summary>
        /// Identifies the <see cref="InvalidNumberColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> InvalidNumberColorProperty = AvaloniaProperty.Register<LinearColorAxis, Color>(
            nameof(InvalidNumberColor),
            Colors.Gray);

        /// <summary>
        /// Initializes static members of the <see cref="LinearColorAxis" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static LinearColorAxis()
        {
            GradientStopsProperty.Changed.AddClassHandler<LinearColorAxis>(AppearanceChanged);
            HighColorProperty.Changed.AddClassHandler<LinearColorAxis>(AppearanceChanged);
            LowColorProperty.Changed.AddClassHandler<LinearColorAxis>(AppearanceChanged);
            PaletteSizeProperty.Changed.AddClassHandler<LinearColorAxis>(AppearanceChanged);
            InvalidNumberColorProperty.Changed.AddClassHandler<LinearColorAxis>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearColorAxis"/> class.
        /// </summary>
        public LinearColorAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.LinearColorAxis();
            this.GradientStops = new GradientStops();
        }

        /// <summary>
        /// Gets or sets the gradient stops for the color palette.
        /// </summary>
        /// <value>A collection of gradient stops defining the color palette.</value>
        [Content]
        public GradientStops GradientStops
        {
            get => (GradientStops)this.GetValue(GradientStopsProperty);
            set => this.SetValue(GradientStopsProperty, value);
        }

        /// <summary>
        /// Gets or sets the color for values above the maximum. The default is <see cref="Colors.White"/>.
        /// </summary>
        public Color HighColor
        {
            get => (Color)this.GetValue(HighColorProperty);
            set => this.SetValue(HighColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the color for values below the minimum. The default is <see cref="Colors.Black"/>.
        /// </summary>
        public Color LowColor
        {
            get => (Color)this.GetValue(LowColorProperty);
            set => this.SetValue(LowColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the color palette. The default is <c>20</c>.
        /// </summary>
        /// <value>The number of colors in the interpolated palette.</value>
        public int PaletteSize
        {
            get => (int)this.GetValue(PaletteSizeProperty);
            set => this.SetValue(PaletteSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the color used for invalid (NaN) values. The default is <see cref="Colors.Gray"/>.
        /// </summary>
        public Color InvalidNumberColor
        {
            get => (Color)this.GetValue(InvalidNumberColorProperty);
            set => this.SetValue(InvalidNumberColorProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Axes.LinearColorAxis"/> model.</returns>
        public override OxyPlot.Axes.Axis CreateModel()
        {
            this.SynchronizeProperties();
            return this.InternalAxis;
        }

        /// <summary>
        /// Synchronizes the WPF properties to the internal OxyPlot axis.
        /// </summary>
        protected override void SynchronizeProperties()
        {
            base.SynchronizeProperties();

            if (this.InternalAxis is OxyPlot.Axes.LinearColorAxis a)
            {
                if (this.GradientStops != null && this.GradientStops.Count >= 2)
                {
                    a.Palette = Interpolate(this.GradientStops.ToList(), this.PaletteSize);
                }

                a.HighColor = this.HighColor.ToOxyColor();
                a.LowColor = this.LowColor.ToOxyColor();
                a.InvalidNumberColor = this.InvalidNumberColor.ToOxyColor();
            }
        }

        /// <summary>
        /// Interpolates a collection of gradient stops to create an OxyPalette.
        /// </summary>
        /// <param name="stops">The gradient stops to interpolate.</param>
        /// <param name="paletteSize">The number of colors in the resulting palette.</param>
        /// <returns>An interpolated OxyPalette.</returns>
        private static OxyPalette Interpolate(List<GradientStop> stops, int paletteSize)
        {
            if (stops.Count < 2 || paletteSize < 1)
            {
                return new OxyPalette();
            }

            var palette = new List<OxyColor>();
            stops.Sort((x1, x2) => x1.Offset.CompareTo(x2.Offset));

            if (paletteSize == 1)
            {
                palette.Add(GetColorAtPosition(stops, 0.5));
                return new OxyPalette(palette);
            }

            var step = 1.0 / (paletteSize - 1);

            for (int i = 0; i < paletteSize; i++)
            {
                var position = i * step;
                var color = GetColorAtPosition(stops, position);
                palette.Add(color);
            }

            return new OxyPalette(palette);
        }

        /// <summary>
        /// Gets the interpolated color at the specified position.
        /// </summary>
        private static OxyColor GetColorAtPosition(List<GradientStop> stops, double position)
        {
            for (int i = 0; i < stops.Count - 1; i++)
            {
                if (position >= stops[i].Offset && position <= stops[i + 1].Offset)
                {
                    var t = (position - stops[i].Offset) / (stops[i + 1].Offset - stops[i].Offset);
                    return OxyColor.Interpolate(
                        stops[i].Color.ToOxyColor(),
                        stops[i + 1].Color.ToOxyColor(),
                        t);
                }
            }

            return stops.Last().Color.ToOxyColor();
        }

        /// <summary>
        /// Validates the palette size value.
        /// </summary>
        private static bool ValidatePaletteSize(object value)
        {
            return (int)value >= 1;
        }
    }
}
