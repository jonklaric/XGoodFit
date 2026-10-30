// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScatterSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.ScatterSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using Avalonia;
    using Avalonia.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.ScatterSeries{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the scatter points.</typeparam>
    /// <remarks>
    /// This class provides WPF dependency properties for scatter series configuration,
    /// including marker appearance, data field mappings, and color axis support.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor.
    /// </remarks>
    public abstract class ScatterSeries<T> : XYAxisSeries where T : ScatterPoint
    {
        // XGoodFit port: as in the fork, these properties belong to the generic class, so each scatter
        // series type (ScatterSeries, ScatterErrorSeries) has its own. Avalonia advises against generic
        // owners (AVP1002) because XAML can't name a generic type; XAML names the concrete series types.
#pragma warning disable AVP1002
        /// <summary>
        /// Identifies the <see cref="BinSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<int> BinSizeProperty = AvaloniaProperty.Register<ScatterSeries<T>, int>(
            nameof(BinSize),
            0);

        /// <summary>
        /// Identifies the <see cref="ColorAxisKey"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> ColorAxisKeyProperty = AvaloniaProperty.Register<ScatterSeries<T>, string>(
            nameof(ColorAxisKey),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldSizeProperty = AvaloniaProperty.Register<ScatterSeries<T>, string>(
            nameof(DataFieldSize),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldTag"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldTagProperty = AvaloniaProperty.Register<ScatterSeries<T>, string>(
            nameof(DataFieldTag),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldValue"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldValueProperty = AvaloniaProperty.Register<ScatterSeries<T>, string>(
            nameof(DataFieldValue),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldXProperty = AvaloniaProperty.Register<ScatterSeries<T>, string>(
            nameof(DataFieldX),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldYProperty = AvaloniaProperty.Register<ScatterSeries<T>, string>(
            nameof(DataFieldY),
            null);

        /// <summary>
        /// Identifies the <see cref="Mapping"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Func<object, T>> MappingProperty = AvaloniaProperty.Register<ScatterSeries<T>, Func<object, T>>(
            nameof(Mapping),
            null);

        /// <summary>
        /// Identifies the <see cref="MarkerFill"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> MarkerFillProperty = AvaloniaProperty.Register<ScatterSeries<T>, Color>(
            nameof(MarkerFill),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="MarkerOutline"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<ScreenPoint[]> MarkerOutlineProperty = AvaloniaProperty.Register<ScatterSeries<T>, ScreenPoint[]>(
            nameof(MarkerOutline),
            null);

        /// <summary>
        /// Identifies the <see cref="MarkerSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MarkerSizeProperty = AvaloniaProperty.Register<ScatterSeries<T>, double>(
            nameof(MarkerSize),
            5.0);

        /// <summary>
        /// Identifies the <see cref="MarkerStroke"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> MarkerStrokeProperty = AvaloniaProperty.Register<ScatterSeries<T>, Color>(
            nameof(MarkerStroke),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="MarkerStrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MarkerStrokeThicknessProperty = AvaloniaProperty.Register<ScatterSeries<T>, double>(
            nameof(MarkerStrokeThickness),
            1.0);

        /// <summary>
        /// Identifies the <see cref="MarkerType"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<MarkerType> MarkerTypeProperty = AvaloniaProperty.Register<ScatterSeries<T>, MarkerType>(
            nameof(MarkerType),
            MarkerType.Square);

#pragma warning restore AVP1002

        /// <summary>
        /// Initializes static members of the <see cref="ScatterSeries{T}" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static ScatterSeries()
        {
            BinSizeProperty.Changed.AddClassHandler<ScatterSeries<T>>(AppearanceChanged);
            ColorAxisKeyProperty.Changed.AddClassHandler<ScatterSeries<T>>(AppearanceChanged);
            DataFieldSizeProperty.Changed.AddClassHandler<ScatterSeries<T>>(DataChanged);
            DataFieldTagProperty.Changed.AddClassHandler<ScatterSeries<T>>(DataChanged);
            DataFieldValueProperty.Changed.AddClassHandler<ScatterSeries<T>>(DataChanged);
            DataFieldXProperty.Changed.AddClassHandler<ScatterSeries<T>>(DataChanged);
            DataFieldYProperty.Changed.AddClassHandler<ScatterSeries<T>>(DataChanged);
            MappingProperty.Changed.AddClassHandler<ScatterSeries<T>>(DataChanged);
            MarkerFillProperty.Changed.AddClassHandler<ScatterSeries<T>>(AppearanceChanged);
            MarkerOutlineProperty.Changed.AddClassHandler<ScatterSeries<T>>(AppearanceChanged);
            MarkerSizeProperty.Changed.AddClassHandler<ScatterSeries<T>>(AppearanceChanged);
            MarkerStrokeProperty.Changed.AddClassHandler<ScatterSeries<T>>(AppearanceChanged);
            MarkerStrokeThicknessProperty.Changed.AddClassHandler<ScatterSeries<T>>(AppearanceChanged);
            MarkerTypeProperty.Changed.AddClassHandler<ScatterSeries<T>>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterSeries{T}"/> class.
        /// </summary>
        protected ScatterSeries()
        {
        }

        /// <summary>
        /// Gets or sets the bin size for performance optimization. The default is <c>0</c>.
        /// </summary>
        /// <value>
        /// The bin size. A value greater than 0 enables binning for improved performance with large datasets.
        /// </value>
        public int BinSize
        {
            get => (int)this.GetValue(BinSizeProperty);
            set => this.SetValue(BinSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the color axis key. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The key of the color axis to use for coloring points based on their value.
        /// </value>
        public string ColorAxisKey
        {
            get => (string)this.GetValue(ColorAxisKeyProperty);
            set => this.SetValue(ColorAxisKeyProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for point size. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The name of the property on items in the ItemsSource that provides size values.
        /// </value>
        public string DataFieldSize
        {
            get => (string)this.GetValue(DataFieldSizeProperty);
            set => this.SetValue(DataFieldSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for point tags. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The name of the property on items in the ItemsSource that provides tag values.
        /// </value>
        public string DataFieldTag
        {
            get => (string)this.GetValue(DataFieldTagProperty);
            set => this.SetValue(DataFieldTagProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for point values (color). The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The name of the property on items in the ItemsSource that provides value (color) data.
        /// </value>
        public string DataFieldValue
        {
            get => (string)this.GetValue(DataFieldValueProperty);
            set => this.SetValue(DataFieldValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for X coordinates. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The name of the property on items in the ItemsSource that provides X coordinate values.
        /// </value>
        public string DataFieldX
        {
            get => (string)this.GetValue(DataFieldXProperty);
            set => this.SetValue(DataFieldXProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for Y coordinates. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The name of the property on items in the ItemsSource that provides Y coordinate values.
        /// </value>
        public string DataFieldY
        {
            get => (string)this.GetValue(DataFieldYProperty);
            set => this.SetValue(DataFieldYProperty, value);
        }

        /// <summary>
        /// Gets or sets the custom mapping function. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// A function that converts items from the ItemsSource to scatter points.
        /// </value>
        public Func<object, T> Mapping
        {
            get => (Func<object, T>)this.GetValue(MappingProperty);
            set => this.SetValue(MappingProperty, value);
        }

        /// <summary>
        /// Gets or sets the marker fill color. The default is <see cref="MoreColors.Automatic"/>.
        /// </summary>
        /// <value>The fill color for markers.</value>
        public Color MarkerFill
        {
            get => (Color)this.GetValue(MarkerFillProperty);
            set => this.SetValue(MarkerFillProperty, value);
        }

        /// <summary>
        /// Gets or sets the custom marker outline. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// An array of screen points defining a custom marker shape.
        /// Set <see cref="MarkerType"/> to <see cref="OxyPlot.MarkerType.Custom"/> to use this.
        /// </value>
        public ScreenPoint[] MarkerOutline
        {
            get => (ScreenPoint[])this.GetValue(MarkerOutlineProperty);
            set => this.SetValue(MarkerOutlineProperty, value);
        }

        /// <summary>
        /// Gets or sets the marker size. The default is <c>5</c>.
        /// </summary>
        /// <value>The size of the markers in pixels.</value>
        public double MarkerSize
        {
            get => (double)this.GetValue(MarkerSizeProperty);
            set => this.SetValue(MarkerSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the marker stroke color. The default is <see cref="MoreColors.Automatic"/>.
        /// </summary>
        /// <value>The stroke (outline) color for markers.</value>
        public Color MarkerStroke
        {
            get => (Color)this.GetValue(MarkerStrokeProperty);
            set => this.SetValue(MarkerStrokeProperty, value);
        }

        /// <summary>
        /// Gets or sets the marker stroke thickness. The default is <c>1</c>.
        /// </summary>
        /// <value>The thickness of the marker outline in pixels.</value>
        public double MarkerStrokeThickness
        {
            get => (double)this.GetValue(MarkerStrokeThicknessProperty);
            set => this.SetValue(MarkerStrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the marker type. The default is <see cref="OxyPlot.MarkerType.Square"/>.
        /// </summary>
        /// <value>The type of marker to display at each data point.</value>
        public MarkerType MarkerType
        {
            get => (MarkerType)this.GetValue(MarkerTypeProperty);
            set => this.SetValue(MarkerTypeProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>The OxyPlot series model.</returns>
        public override OxyPlot.Series.Series CreateModel()
        {
            this.SynchronizeProperties(this.InternalSeries);
            return this.InternalSeries;
        }

        /// <summary>
        /// Synchronizes the WPF properties to the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal OxyPlot series to update.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.ScatterSeries<T> s)
            {
                s.BinSize = this.BinSize;
                s.ColorAxisKey = this.ColorAxisKey;
                s.DataFieldSize = this.DataFieldSize;
                s.DataFieldTag = this.DataFieldTag;
                s.DataFieldValue = this.DataFieldValue;
                s.DataFieldX = this.DataFieldX;
                s.DataFieldY = this.DataFieldY;
                s.ItemsSource = this.ItemsSource;
                s.Mapping = this.Mapping;
                s.MarkerFill = this.MarkerFill.ToOxyColor();
                s.MarkerOutline = this.MarkerOutline;
                s.MarkerSize = this.MarkerSize;
                s.MarkerStroke = this.MarkerStroke.ToOxyColor();
                s.MarkerStrokeThickness = this.MarkerStrokeThickness;
                s.MarkerType = this.MarkerType;
            }
        }
    }

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.ScatterSeries"/>.
    /// </summary>
    /// <remarks>
    /// This is a concrete implementation of ScatterSeries using <see cref="ScatterPoint"/>.
    /// </remarks>
    public class ScatterSeries : ScatterSeries<ScatterPoint>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterSeries"/> class.
        /// </summary>
        public ScatterSeries()
        {
            this.InternalSeries = new OxyPlot.Series.ScatterSeries();
        }
    }
}
