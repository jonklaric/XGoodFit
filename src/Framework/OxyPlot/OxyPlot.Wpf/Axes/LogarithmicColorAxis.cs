// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogarithmicColorAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.LogarithmicColorAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia;
    using Avalonia.Metadata;
    using Avalonia.Media;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.LogarithmicColorAxis"/>.
    /// </summary>
    /// <remarks>
    /// A logarithmic color axis maps numeric values to colors using a color palette
    /// with a logarithmic scale. This is useful for data spanning multiple orders of magnitude.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF; the XAML content property is marked on the property (Avalonia's
    /// <c>[Content]</c>) rather than on the class; the gradient stops are Avalonia's <c>GradientStops</c>.
    /// </remarks>
    public class LogarithmicColorAxis : LogarithmicAxis
    {
        /// <summary>
        /// Identifies the <see cref="GradientStops"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<GradientStops> GradientStopsProperty = AvaloniaProperty.Register<LogarithmicColorAxis, GradientStops>(
            nameof(GradientStops),
            null);

        /// <summary>
        /// Identifies the <see cref="HighColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> HighColorProperty = AvaloniaProperty.Register<LogarithmicColorAxis, Color>(
            nameof(HighColor),
            MoreColors.Undefined);

        /// <summary>
        /// Identifies the <see cref="LowColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> LowColorProperty = AvaloniaProperty.Register<LogarithmicColorAxis, Color>(
            nameof(LowColor),
            MoreColors.Undefined);

        /// <summary>
        /// Identifies the <see cref="InvalidNumberColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> InvalidNumberColorProperty = AvaloniaProperty.Register<LogarithmicColorAxis, Color>(
            nameof(InvalidNumberColor),
            Colors.Gray);

        /// <summary>
        /// Identifies the <see cref="PaletteSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<int> PaletteSizeProperty = AvaloniaProperty.Register<LogarithmicColorAxis, int>(
            nameof(PaletteSize),
            20,
            validate: v => ValidatePaletteSize(v));

        /// <summary>
        /// Identifies the <see cref="RenderAsImage"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> RenderAsImageProperty = AvaloniaProperty.Register<LogarithmicColorAxis, bool>(
            nameof(RenderAsImage),
            false);

        /// <summary>
        /// Initializes static members of the <see cref="LogarithmicColorAxis"/> class.
        /// </summary>
        static LogarithmicColorAxis()
        {
            GradientStopsProperty.Changed.AddClassHandler<LogarithmicColorAxis>(AppearanceChanged);
            HighColorProperty.Changed.AddClassHandler<LogarithmicColorAxis>(AppearanceChanged);
            LowColorProperty.Changed.AddClassHandler<LogarithmicColorAxis>(AppearanceChanged);
            InvalidNumberColorProperty.Changed.AddClassHandler<LogarithmicColorAxis>(AppearanceChanged);
            PaletteSizeProperty.Changed.AddClassHandler<LogarithmicColorAxis>(AppearanceChanged);
            RenderAsImageProperty.Changed.AddClassHandler<LogarithmicColorAxis>(AppearanceChanged);
            PositionProperty.OverrideDefaultValue<LogarithmicColorAxis>(AxisPosition.None);
            PositionProperty.Changed.AddClassHandler<LogarithmicColorAxis>(AppearanceChanged);
            IsPanEnabledProperty.OverrideDefaultValue<LogarithmicColorAxis>(false);
            IsZoomEnabledProperty.OverrideDefaultValue<LogarithmicColorAxis>(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogarithmicColorAxis"/> class.
        /// </summary>
        public LogarithmicColorAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.LogarithmicColorAxis();
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
        /// Gets or sets the color for values above the maximum.
        /// </summary>
        /// <value>The high color. The default is <see cref="MoreColors.Undefined"/>.</value>
        public Color HighColor
        {
            get => (Color)this.GetValue(HighColorProperty);
            set => this.SetValue(HighColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the color for values below the minimum.
        /// </summary>
        /// <value>The low color. The default is <see cref="MoreColors.Undefined"/>.</value>
        public Color LowColor
        {
            get => (Color)this.GetValue(LowColorProperty);
            set => this.SetValue(LowColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the color used for NaN values.
        /// </summary>
        /// <value>The invalid number color. The default is <see cref="Colors.Gray"/>.</value>
        public Color InvalidNumberColor
        {
            get => (Color)this.GetValue(InvalidNumberColorProperty);
            set => this.SetValue(InvalidNumberColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the color palette.
        /// </summary>
        /// <value>The number of colors in the interpolated palette. The default is <c>20</c>.</value>
        public int PaletteSize
        {
            get => (int)this.GetValue(PaletteSizeProperty);
            set => this.SetValue(PaletteSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to render the color scale as an image.
        /// </summary>
        /// <value><c>true</c> if rendering as image; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool RenderAsImage
        {
            get => (bool)this.GetValue(RenderAsImageProperty);
            set => this.SetValue(RenderAsImageProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Axes.LogarithmicColorAxis"/> model.</returns>
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

            if (this.InternalAxis is OxyPlot.Axes.LogarithmicColorAxis a)
            {
                if (this.GradientStops != null && this.GradientStops.Count >= 2)
                {
                    a.Palette = Interpolate(this.GradientStops.ToList(), this.PaletteSize);
                }

                a.HighColor = this.HighColor.ToOxyColor();
                a.LowColor = this.LowColor.ToOxyColor();
                a.InvalidNumberColor = this.InvalidNumberColor.ToOxyColor();
                a.RenderAsImage = this.RenderAsImage;
            }
        }

        /// <summary>
        /// Interpolates a collection of gradient stops to create an OxyPalette.
        /// </summary>
        private static OxyPalette Interpolate(List<GradientStop> stops, int paletteSize)
        {
            if (stops.Count < 2 || paletteSize < 1)
            {
                return OxyPalettes.Viridis();
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
