// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HeatMapSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.HeatMapSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;
    using Avalonia.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.HeatMapSeries"/>.
    /// </summary>
    /// <remarks>
    /// A heat map series displays 2D data as a rectangular grid of colored cells,
    /// where the color intensity represents the value at each grid position.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF.
    /// </remarks>
    public class HeatMapSeries : XYAxisSeries
    {
        /// <summary>
        /// Identifies the <see cref="Data"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double[,]> DataProperty = AvaloniaProperty.Register<HeatMapSeries, double[,]>(
            nameof(Data),
            new double[0, 0],
            validate: value => value != null);

        /// <summary>
        /// Identifies the <see cref="X0"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> X0Property = AvaloniaProperty.Register<HeatMapSeries, double>(
            nameof(X0),
            default(double));

        /// <summary>
        /// Identifies the <see cref="X1"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> X1Property = AvaloniaProperty.Register<HeatMapSeries, double>(
            nameof(X1),
            default(double));

        /// <summary>
        /// Identifies the <see cref="Y0"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> Y0Property = AvaloniaProperty.Register<HeatMapSeries, double>(
            nameof(Y0),
            default(double));

        /// <summary>
        /// Identifies the <see cref="Y1"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> Y1Property = AvaloniaProperty.Register<HeatMapSeries, double>(
            nameof(Y1),
            default(double));

        /// <summary>
        /// Identifies the <see cref="ColorAxisKey"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> ColorAxisKeyProperty = AvaloniaProperty.Register<HeatMapSeries, string>(
            nameof(ColorAxisKey),
            default(string));

        /// <summary>
        /// Identifies the <see cref="LowColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> LowColorProperty = AvaloniaProperty.Register<HeatMapSeries, Color>(
            nameof(LowColor),
            default(Color));

        /// <summary>
        /// Identifies the <see cref="HighColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> HighColorProperty = AvaloniaProperty.Register<HeatMapSeries, Color>(
            nameof(HighColor),
            default(Color));

        /// <summary>
        /// Identifies the <see cref="CoordinateDefinition"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<OxyPlot.Series.HeatMapCoordinateDefinition> CoordinateDefinitionProperty = AvaloniaProperty.Register<HeatMapSeries, OxyPlot.Series.HeatMapCoordinateDefinition>(
            nameof(CoordinateDefinition),
            OxyPlot.Series.HeatMapCoordinateDefinition.Center);

        /// <summary>
        /// Identifies the <see cref="Interpolate"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> InterpolateProperty = AvaloniaProperty.Register<HeatMapSeries, bool>(
            nameof(Interpolate),
            true);

        /// <summary>
        /// Identifies the <see cref="LabelFontSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LabelFontSizeProperty = AvaloniaProperty.Register<HeatMapSeries, double>(
            nameof(LabelFontSize),
            default(double));

        /// <summary>
        /// Initializes static members of the <see cref="HeatMapSeries"/> class.
        /// </summary>
        static HeatMapSeries()
        {
            DataProperty.Changed.AddClassHandler<HeatMapSeries>(DataChanged);
            X0Property.Changed.AddClassHandler<HeatMapSeries>(AppearanceChanged);
            X1Property.Changed.AddClassHandler<HeatMapSeries>(AppearanceChanged);
            Y0Property.Changed.AddClassHandler<HeatMapSeries>(AppearanceChanged);
            Y1Property.Changed.AddClassHandler<HeatMapSeries>(AppearanceChanged);
            ColorAxisKeyProperty.Changed.AddClassHandler<HeatMapSeries>(AppearanceChanged);
            LowColorProperty.Changed.AddClassHandler<HeatMapSeries>(AppearanceChanged);
            HighColorProperty.Changed.AddClassHandler<HeatMapSeries>(AppearanceChanged);
            CoordinateDefinitionProperty.Changed.AddClassHandler<HeatMapSeries>(AppearanceChanged);
            InterpolateProperty.Changed.AddClassHandler<HeatMapSeries>(AppearanceChanged);
            LabelFontSizeProperty.Changed.AddClassHandler<HeatMapSeries>(AppearanceChanged);
            TrackerFormatStringProperty.OverrideDefaultValue<HeatMapSeries>(OxyPlot.Series.HeatMapSeries.DefaultTrackerFormatString);
            TrackerFormatStringProperty.Changed.AddClassHandler<HeatMapSeries>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HeatMapSeries"/> class.
        /// </summary>
        public HeatMapSeries()
        {
            this.Data = new double[0, 0];
            this.InternalSeries = new OxyPlot.Series.HeatMapSeries { Data = this.Data };
        }

        /// <summary>
        /// Gets or sets the 2D data array.
        /// </summary>
        /// <value>The data array. The default is an empty array.</value>
        public double[,] Data
        {
            get => (double[,])this.GetValue(DataProperty);
            set => this.SetValue(DataProperty, value);
        }

        /// <summary>
        /// Gets or sets the X coordinate of the first column.
        /// </summary>
        /// <value>The X coordinate. The default is <c>0.0</c>.</value>
        public double X0
        {
            get => (double)this.GetValue(X0Property);
            set => this.SetValue(X0Property, value);
        }

        /// <summary>
        /// Gets or sets the X coordinate of the last column.
        /// </summary>
        /// <value>The X coordinate. The default is <c>0.0</c>.</value>
        public double X1
        {
            get => (double)this.GetValue(X1Property);
            set => this.SetValue(X1Property, value);
        }

        /// <summary>
        /// Gets or sets the Y coordinate of the first row.
        /// </summary>
        /// <value>The Y coordinate. The default is <c>0.0</c>.</value>
        public double Y0
        {
            get => (double)this.GetValue(Y0Property);
            set => this.SetValue(Y0Property, value);
        }

        /// <summary>
        /// Gets or sets the Y coordinate of the last row.
        /// </summary>
        /// <value>The Y coordinate. The default is <c>0.0</c>.</value>
        public double Y1
        {
            get => (double)this.GetValue(Y1Property);
            set => this.SetValue(Y1Property, value);
        }

        /// <summary>
        /// Gets or sets the key of the color axis to use.
        /// </summary>
        /// <value>The color axis key. The default is <c>null</c>.</value>
        public string ColorAxisKey
        {
            get => (string)this.GetValue(ColorAxisKeyProperty);
            set => this.SetValue(ColorAxisKeyProperty, value);
        }

        /// <summary>
        /// Gets or sets the color for values below the minimum.
        /// </summary>
        /// <value>The low color. The default is transparent.</value>
        public Color LowColor
        {
            get => (Color)this.GetValue(LowColorProperty);
            set => this.SetValue(LowColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the color for values above the maximum.
        /// </summary>
        /// <value>The high color. The default is transparent.</value>
        public Color HighColor
        {
            get => (Color)this.GetValue(HighColorProperty);
            set => this.SetValue(HighColorProperty, value);
        }

        /// <summary>
        /// Gets or sets how coordinates are defined relative to data cells.
        /// </summary>
        /// <value>The coordinate definition. The default is <see cref="OxyPlot.Series.HeatMapCoordinateDefinition.Center"/>.</value>
        public OxyPlot.Series.HeatMapCoordinateDefinition CoordinateDefinition
        {
            get => (OxyPlot.Series.HeatMapCoordinateDefinition)this.GetValue(CoordinateDefinitionProperty);
            set => this.SetValue(CoordinateDefinitionProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to interpolate between cells.
        /// </summary>
        /// <value><c>true</c> if interpolating; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool Interpolate
        {
            get => (bool)this.GetValue(InterpolateProperty);
            set => this.SetValue(InterpolateProperty, value);
        }

        /// <summary>
        /// Gets or sets the font size for cell labels.
        /// </summary>
        /// <value>The label font size. The default is <c>0.0</c> (no labels).</value>
        public double LabelFontSize
        {
            get => (double)this.GetValue(LabelFontSizeProperty);
            set => this.SetValue(LabelFontSizeProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.HeatMapSeries"/> instance.</returns>
        public override OxyPlot.Series.Series CreateModel()
        {
            this.SynchronizeProperties(this.InternalSeries);
            return this.InternalSeries;
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal series to synchronize.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.HeatMapSeries s)
            {
                s.Data = this.Data ?? new double[0, 0];
                s.X0 = this.X0;
                s.X1 = this.X1;
                s.Y0 = this.Y0;
                s.Y1 = this.Y1;
                s.CoordinateDefinition = this.CoordinateDefinition;
                s.LabelFontSize = this.LabelFontSize;
                s.Interpolate = this.Interpolate;
                s.ColorAxisKey = this.ColorAxisKey;
            }
        }
    }
}
