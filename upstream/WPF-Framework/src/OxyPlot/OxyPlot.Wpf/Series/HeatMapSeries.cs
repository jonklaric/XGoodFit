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
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.HeatMapSeries"/>.
    /// </summary>
    /// <remarks>
    /// A heat map series displays 2D data as a rectangular grid of colored cells,
    /// where the color intensity represents the value at each grid position.
    /// </remarks>
    public class HeatMapSeries : XYAxisSeries
    {
        /// <summary>
        /// Identifies the <see cref="Data"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(double[,]),
                typeof(HeatMapSeries),
                new PropertyMetadata(new double[0, 0], DataChanged),
                value => value != null);

        /// <summary>
        /// Identifies the <see cref="X0"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty X0Property =
            DependencyProperty.Register(
                nameof(X0),
                typeof(double),
                typeof(HeatMapSeries),
                new PropertyMetadata(default(double), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="X1"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty X1Property =
            DependencyProperty.Register(
                nameof(X1),
                typeof(double),
                typeof(HeatMapSeries),
                new PropertyMetadata(default(double), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Y0"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Y0Property =
            DependencyProperty.Register(
                nameof(Y0),
                typeof(double),
                typeof(HeatMapSeries),
                new PropertyMetadata(default(double), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Y1"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Y1Property =
            DependencyProperty.Register(
                nameof(Y1),
                typeof(double),
                typeof(HeatMapSeries),
                new PropertyMetadata(default(double), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ColorAxisKey"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorAxisKeyProperty =
            DependencyProperty.Register(
                nameof(ColorAxisKey),
                typeof(string),
                typeof(HeatMapSeries),
                new PropertyMetadata(default(string), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LowColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LowColorProperty =
            DependencyProperty.Register(
                nameof(LowColor),
                typeof(Color),
                typeof(HeatMapSeries),
                new PropertyMetadata(default(Color), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="HighColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HighColorProperty =
            DependencyProperty.Register(
                nameof(HighColor),
                typeof(Color),
                typeof(HeatMapSeries),
                new PropertyMetadata(default(Color), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="CoordinateDefinition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CoordinateDefinitionProperty =
            DependencyProperty.Register(
                nameof(CoordinateDefinition),
                typeof(OxyPlot.Series.HeatMapCoordinateDefinition),
                typeof(HeatMapSeries),
                new PropertyMetadata(OxyPlot.Series.HeatMapCoordinateDefinition.Center, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Interpolate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InterpolateProperty =
            DependencyProperty.Register(
                nameof(Interpolate),
                typeof(bool),
                typeof(HeatMapSeries),
                new PropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFontSizeProperty =
            DependencyProperty.Register(
                nameof(LabelFontSize),
                typeof(double),
                typeof(HeatMapSeries),
                new PropertyMetadata(default(double), AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="HeatMapSeries"/> class.
        /// </summary>
        static HeatMapSeries()
        {
            TrackerFormatStringProperty.OverrideMetadata(
                typeof(HeatMapSeries),
                new PropertyMetadata(OxyPlot.Series.HeatMapSeries.DefaultTrackerFormatString, AppearanceChanged));
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
