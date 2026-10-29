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
    using System.Windows;
    using System.Windows.Markup;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.LinearColorAxis"/>.
    /// </summary>
    /// <remarks>
    /// A linear color axis maps numeric values to colors using a color palette.
    /// This is commonly used with heat maps and scatter series with color-coded points.
    /// </remarks>
    [ContentProperty("GradientStops")]
    public class LinearColorAxis : Axis
    {
        /// <summary>
        /// Identifies the <see cref="GradientStops"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GradientStopsProperty = DependencyProperty.Register(
            nameof(GradientStops),
            typeof(GradientStopCollection),
            typeof(LinearColorAxis),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="HighColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HighColorProperty = DependencyProperty.Register(
            nameof(HighColor),
            typeof(Color),
            typeof(LinearColorAxis),
            new PropertyMetadata(Colors.White, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LowColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LowColorProperty = DependencyProperty.Register(
            nameof(LowColor),
            typeof(Color),
            typeof(LinearColorAxis),
            new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="PaletteSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaletteSizeProperty = DependencyProperty.Register(
            nameof(PaletteSize),
            typeof(int),
            typeof(LinearColorAxis),
            new PropertyMetadata(20, AppearanceChanged),
            ValidatePaletteSize);

        /// <summary>
        /// Identifies the <see cref="InvalidNumberColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InvalidNumberColorProperty = DependencyProperty.Register(
            nameof(InvalidNumberColor),
            typeof(Color),
            typeof(LinearColorAxis),
            new PropertyMetadata(Colors.Gray, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearColorAxis"/> class.
        /// </summary>
        public LinearColorAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.LinearColorAxis();
            this.GradientStops = new GradientStopCollection();
        }

        /// <summary>
        /// Gets or sets the gradient stops for the color palette.
        /// </summary>
        /// <value>A collection of gradient stops defining the color palette.</value>
        public GradientStopCollection GradientStops
        {
            get => (GradientStopCollection)this.GetValue(GradientStopsProperty);
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
