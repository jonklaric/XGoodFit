// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScatterErrorSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.ScatterErrorSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;
    using Avalonia.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.ScatterErrorSeries"/>.
    /// </summary>
    /// <remarks>
    /// A scatter error series displays scatter points with error bars showing
    /// the uncertainty or variability of the data in both X and Y directions.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor.
    /// </remarks>
    public class ScatterErrorSeries : ScatterSeries<ScatterErrorPoint>
    {
        /// <summary>
        /// Identifies the <see cref="DataFieldErrorX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldErrorXProperty = AvaloniaProperty.Register<ScatterErrorSeries, string>(
            nameof(DataFieldErrorX),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldErrorY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldErrorYProperty = AvaloniaProperty.Register<ScatterErrorSeries, string>(
            nameof(DataFieldErrorY),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldLowerErrorX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldLowerErrorXProperty = AvaloniaProperty.Register<ScatterErrorSeries, string>(
            nameof(DataFieldLowerErrorX),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldUpperErrorX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldUpperErrorXProperty = AvaloniaProperty.Register<ScatterErrorSeries, string>(
            nameof(DataFieldUpperErrorX),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldLowerErrorY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldLowerErrorYProperty = AvaloniaProperty.Register<ScatterErrorSeries, string>(
            nameof(DataFieldLowerErrorY),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldUpperErrorY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldUpperErrorYProperty = AvaloniaProperty.Register<ScatterErrorSeries, string>(
            nameof(DataFieldUpperErrorY),
            null);

        /// <summary>
        /// Identifies the <see cref="ErrorBarColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> ErrorBarColorProperty = AvaloniaProperty.Register<ScatterErrorSeries, Color>(
            nameof(ErrorBarColor),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="ErrorBarStopWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ErrorBarStopWidthProperty = AvaloniaProperty.Register<ScatterErrorSeries, double>(
            nameof(ErrorBarStopWidth),
            4.0);

        /// <summary>
        /// Identifies the <see cref="ErrorBarStrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ErrorBarStrokeThicknessProperty = AvaloniaProperty.Register<ScatterErrorSeries, double>(
            nameof(ErrorBarStrokeThickness),
            1.0);

        /// <summary>
        /// Identifies the <see cref="MinimumErrorSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumErrorSizeProperty = AvaloniaProperty.Register<ScatterErrorSeries, double>(
            nameof(MinimumErrorSize),
            0.0);

        /// <summary>
        /// Initializes static members of the <see cref="ScatterErrorSeries" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static ScatterErrorSeries()
        {
            DataFieldErrorXProperty.Changed.AddClassHandler<ScatterErrorSeries>(DataChanged);
            DataFieldErrorYProperty.Changed.AddClassHandler<ScatterErrorSeries>(DataChanged);
            DataFieldLowerErrorXProperty.Changed.AddClassHandler<ScatterErrorSeries>(DataChanged);
            DataFieldUpperErrorXProperty.Changed.AddClassHandler<ScatterErrorSeries>(DataChanged);
            DataFieldLowerErrorYProperty.Changed.AddClassHandler<ScatterErrorSeries>(DataChanged);
            DataFieldUpperErrorYProperty.Changed.AddClassHandler<ScatterErrorSeries>(DataChanged);
            ErrorBarColorProperty.Changed.AddClassHandler<ScatterErrorSeries>(AppearanceChanged);
            ErrorBarStopWidthProperty.Changed.AddClassHandler<ScatterErrorSeries>(AppearanceChanged);
            ErrorBarStrokeThicknessProperty.Changed.AddClassHandler<ScatterErrorSeries>(AppearanceChanged);
            MinimumErrorSizeProperty.Changed.AddClassHandler<ScatterErrorSeries>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterErrorSeries"/> class.
        /// </summary>
        public ScatterErrorSeries()
        {
            this.InternalSeries = new OxyPlot.Series.ScatterErrorSeries();
        }

        /// <summary>
        /// Gets or sets the data field for symmetric X error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldErrorX
        {
            get => (string)this.GetValue(DataFieldErrorXProperty);
            set => this.SetValue(DataFieldErrorXProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for symmetric Y error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldErrorY
        {
            get => (string)this.GetValue(DataFieldErrorYProperty);
            set => this.SetValue(DataFieldErrorYProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for lower X error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldLowerErrorX
        {
            get => (string)this.GetValue(DataFieldLowerErrorXProperty);
            set => this.SetValue(DataFieldLowerErrorXProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for upper X error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldUpperErrorX
        {
            get => (string)this.GetValue(DataFieldUpperErrorXProperty);
            set => this.SetValue(DataFieldUpperErrorXProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for lower Y error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldLowerErrorY
        {
            get => (string)this.GetValue(DataFieldLowerErrorYProperty);
            set => this.SetValue(DataFieldLowerErrorYProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for upper Y error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldUpperErrorY
        {
            get => (string)this.GetValue(DataFieldUpperErrorYProperty);
            set => this.SetValue(DataFieldUpperErrorYProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of the error bars.
        /// </summary>
        /// <value>The error bar color. The default is <see cref="Colors.Black"/>.</value>
        public Color ErrorBarColor
        {
            get => (Color)this.GetValue(ErrorBarColorProperty);
            set => this.SetValue(ErrorBarColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the error bar end caps.
        /// </summary>
        /// <value>The stop width in pixels. The default is <c>4.0</c>.</value>
        public double ErrorBarStopWidth
        {
            get => (double)this.GetValue(ErrorBarStopWidthProperty);
            set => this.SetValue(ErrorBarStopWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness of the error bars.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>1.0</c>.</value>
        public double ErrorBarStrokeThickness
        {
            get => (double)this.GetValue(ErrorBarStrokeThicknessProperty);
            set => this.SetValue(ErrorBarStrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum error size relative to marker size.
        /// </summary>
        /// <value>The minimum error size. The default is <c>0.0</c>.</value>
        /// <remarks>Error bars smaller than this size (relative to marker size) will not be shown.</remarks>
        public double MinimumErrorSize
        {
            get => (double)this.GetValue(MinimumErrorSizeProperty);
            set => this.SetValue(MinimumErrorSizeProperty, value);
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal series to synchronize.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.ScatterErrorSeries s)
            {
                s.DataFieldErrorX = this.DataFieldErrorX;
                s.DataFieldErrorY = this.DataFieldErrorY;
                s.DataFieldLowerErrorX = this.DataFieldLowerErrorX;
                s.DataFieldUpperErrorX = this.DataFieldUpperErrorX;
                s.DataFieldLowerErrorY = this.DataFieldLowerErrorY;
                s.DataFieldUpperErrorY = this.DataFieldUpperErrorY;
                s.ErrorBarColor = this.ErrorBarColor.ToOxyColor();
                s.ErrorBarStopWidth = this.ErrorBarStopWidth;
                s.ErrorBarStrokeThickness = this.ErrorBarStrokeThickness;
                s.MinimumErrorSize = this.MinimumErrorSize;
            }
        }
    }
}
