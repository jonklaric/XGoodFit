// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BoxPlotSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.BoxPlotSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.BoxPlotSeries"/>.
    /// </summary>
    /// <remarks>
    /// A box plot series displays statistical distributions through quartiles.
    /// Each box shows the median, first and third quartiles, with whiskers
    /// extending to show the range of the data and individual outliers.
    /// </remarks>
    public class BoxPlotSeries : XYAxisSeries
    {
        /// <summary>
        /// The items collection for manually added items.
        /// </summary>
        private readonly List<OxyPlot.Series.BoxPlotItem> items = new List<OxyPlot.Series.BoxPlotItem>();

        /// <summary>
        /// Identifies the <see cref="BoxWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BoxWidthProperty =
            DependencyProperty.Register(
                nameof(BoxWidth),
                typeof(double),
                typeof(BoxPlotSeries),
                new PropertyMetadata(0.3, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Fill"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(
                nameof(Fill),
                typeof(Color),
                typeof(BoxPlotSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStyleProperty =
            DependencyProperty.Register(
                nameof(LineStyle),
                typeof(LineStyle),
                typeof(BoxPlotSeries),
                new PropertyMetadata(LineStyle.Solid, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MedianPointSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MedianPointSizeProperty =
            DependencyProperty.Register(
                nameof(MedianPointSize),
                typeof(double),
                typeof(BoxPlotSeries),
                new PropertyMetadata(2.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MedianThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MedianThicknessProperty =
            DependencyProperty.Register(
                nameof(MedianThickness),
                typeof(double),
                typeof(BoxPlotSeries),
                new PropertyMetadata(2.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="OutlierSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OutlierSizeProperty =
            DependencyProperty.Register(
                nameof(OutlierSize),
                typeof(double),
                typeof(BoxPlotSeries),
                new PropertyMetadata(2.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="OutlierType"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OutlierTypeProperty =
            DependencyProperty.Register(
                nameof(OutlierType),
                typeof(MarkerType),
                typeof(BoxPlotSeries),
                new PropertyMetadata(MarkerType.Circle, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="OutlierOutline"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OutlierOutlineProperty =
            DependencyProperty.Register(
                nameof(OutlierOutline),
                typeof(Point[]),
                typeof(BoxPlotSeries),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ShowBox"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowBoxProperty =
            DependencyProperty.Register(
                nameof(ShowBox),
                typeof(bool),
                typeof(BoxPlotSeries),
                new PropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ShowMedianAsDot"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowMedianAsDotProperty =
            DependencyProperty.Register(
                nameof(ShowMedianAsDot),
                typeof(bool),
                typeof(BoxPlotSeries),
                new PropertyMetadata(false, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Stroke"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(
                nameof(Stroke),
                typeof(Color),
                typeof(BoxPlotSeries),
                new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(BoxPlotSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="WhiskerWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WhiskerWidthProperty =
            DependencyProperty.Register(
                nameof(WhiskerWidth),
                typeof(double),
                typeof(BoxPlotSeries),
                new PropertyMetadata(0.5, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="IsVertical"/> dependency property.
        /// </summary>
        /// <remarks>
        /// This property is provided for backward compatibility. In the current OxyPlot version,
        /// box plots are always rendered vertically.
        /// </remarks>
        public static readonly DependencyProperty IsVerticalProperty =
            DependencyProperty.Register(
                nameof(IsVertical),
                typeof(bool),
                typeof(BoxPlotSeries),
                new PropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="BoxPlotSeries"/> class.
        /// </summary>
        static BoxPlotSeries()
        {
            TrackerFormatStringProperty.OverrideMetadata(
                typeof(BoxPlotSeries),
                new PropertyMetadata(OxyPlot.Series.BoxPlotSeries.DefaultTrackerFormatString, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxPlotSeries"/> class.
        /// </summary>
        public BoxPlotSeries()
        {
            this.InternalSeries = new OxyPlot.Series.BoxPlotSeries();
        }

        /// <summary>
        /// Gets the items collection.
        /// </summary>
        /// <value>A list of items that can be manually added when not using ItemsSource.</value>
        /// <remarks>
        /// This property shadows <see cref="System.Windows.Controls.ItemsControl.Items"/> to provide
        /// a strongly-typed collection for box plot series items.
        /// </remarks>
        public new List<OxyPlot.Series.BoxPlotItem> Items => this.items;

        /// <summary>
        /// Gets or sets the width of the boxes (specified in x-axis units).
        /// </summary>
        /// <value>The box width. The default is <c>0.3</c>.</value>
        public double BoxWidth
        {
            get => (double)this.GetValue(BoxWidthProperty);
            set => this.SetValue(BoxWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the fill color of the boxes.
        /// </summary>
        /// <value>The fill color. The default is <see cref="MoreColors.Automatic"/>.</value>
        public Color Fill
        {
            get => (Color)this.GetValue(FillProperty);
            set => this.SetValue(FillProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style for the box borders.
        /// </summary>
        /// <value>The line style. The default is <see cref="LineStyle.Solid"/>.</value>
        public LineStyle LineStyle
        {
            get => (LineStyle)this.GetValue(LineStyleProperty);
            set => this.SetValue(LineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the median point when shown as a dot.
        /// </summary>
        /// <value>The median point size. The default is <c>2.0</c>.</value>
        public double MedianPointSize
        {
            get => (double)this.GetValue(MedianPointSizeProperty);
            set => this.SetValue(MedianPointSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the median line thickness relative to the stroke thickness.
        /// </summary>
        /// <value>The median thickness. The default is <c>2.0</c>.</value>
        public double MedianThickness
        {
            get => (double)this.GetValue(MedianThicknessProperty);
            set => this.SetValue(MedianThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the diameter of the outlier markers.
        /// </summary>
        /// <value>The outlier size in points. The default is <c>2.0</c>.</value>
        public double OutlierSize
        {
            get => (double)this.GetValue(OutlierSizeProperty);
            set => this.SetValue(OutlierSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the marker type for outliers.
        /// </summary>
        /// <value>The outlier marker type. The default is <see cref="MarkerType.Circle"/>.</value>
        public MarkerType OutlierType
        {
            get => (MarkerType)this.GetValue(OutlierTypeProperty);
            set => this.SetValue(OutlierTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets the custom polygon outline for outlier markers.
        /// </summary>
        /// <value>The custom outline points. The default is <c>null</c>.</value>
        /// <remarks>Set <see cref="OutlierType"/> to <see cref="MarkerType.Custom"/> to use this.</remarks>
        public Point[] OutlierOutline
        {
            get => (Point[])this.GetValue(OutlierOutlineProperty);
            set => this.SetValue(OutlierOutlineProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the boxes.
        /// </summary>
        /// <value><c>true</c> if boxes are shown; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool ShowBox
        {
            get => (bool)this.GetValue(ShowBoxProperty);
            set => this.SetValue(ShowBoxProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the median as a dot instead of a line.
        /// </summary>
        /// <value><c>true</c> if median is shown as a dot; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool ShowMedianAsDot
        {
            get => (bool)this.GetValue(ShowMedianAsDotProperty);
            set => this.SetValue(ShowMedianAsDotProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke color for box borders.
        /// </summary>
        /// <value>The stroke color. The default is <see cref="Colors.Black"/>.</value>
        public Color Stroke
        {
            get => (Color)this.GetValue(StrokeProperty);
            set => this.SetValue(StrokeProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness for box borders.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>1.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the whiskers relative to the box width.
        /// </summary>
        /// <value>The whisker width. The default is <c>0.5</c>.</value>
        public double WhiskerWidth
        {
            get => (double)this.GetValue(WhiskerWidthProperty);
            set => this.SetValue(WhiskerWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the box plots are rendered vertically.
        /// </summary>
        /// <value><c>true</c> if vertical; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        /// <remarks>
        /// This property is provided for backward compatibility. In the current OxyPlot version,
        /// box plots are always rendered vertically.
        /// </remarks>
        public bool IsVertical
        {
            get => (bool)this.GetValue(IsVerticalProperty);
            set => this.SetValue(IsVerticalProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.BoxPlotSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.BoxPlotSeries s)
            {
                s.Fill = this.Fill.ToOxyColor();
                s.LineStyle = this.LineStyle;
                s.MedianPointSize = this.MedianPointSize;
                s.OutlierSize = this.OutlierSize;
                s.OutlierType = this.OutlierType;
                s.OutlierOutline = this.OutlierOutline?.ToScreenPointArray();
                s.ShowBox = this.ShowBox;
                s.BoxWidth = this.BoxWidth;
                s.ShowMedianAsDot = this.ShowMedianAsDot;
                s.Stroke = this.Stroke.ToOxyColor();
                s.StrokeThickness = this.StrokeThickness;
                s.WhiskerWidth = this.WhiskerWidth;
                s.MedianThickness = this.MedianThickness;

                if (this.ItemsSource == null)
                {
                    s.Items.Clear();
                    foreach (var item in this.items)
                    {
                        s.Items.Add(item);
                    }
                }
            }
        }
    }
}
