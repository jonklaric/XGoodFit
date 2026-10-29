// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LineSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.LineSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.LineSeries"/>.
    /// </summary>
    /// <remarks>
    /// This class provides WPF dependency properties for all LineSeries properties,
    /// enabling data binding and XAML support for line series configuration.
    /// </remarks>
    public class LineSeries : DataPointSeries
    {
        /// <summary>
        /// Identifies the <see cref="BrokenLineColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BrokenLineColorProperty = DependencyProperty.Register(
            nameof(BrokenLineColor),
            typeof(Color),
            typeof(LineSeries),
            new PropertyMetadata(MoreColors.Undefined, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="BrokenLineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BrokenLineStyleProperty = DependencyProperty.Register(
            nameof(BrokenLineStyle),
            typeof(LineStyle),
            typeof(LineSeries),
            new PropertyMetadata(LineStyle.Solid, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="BrokenLineThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BrokenLineThicknessProperty = DependencyProperty.Register(
            nameof(BrokenLineThickness),
            typeof(double),
            typeof(LineSeries),
            new PropertyMetadata(0d, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Dashes"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DashesProperty = DependencyProperty.Register(
            nameof(Dashes),
            typeof(double[]),
            typeof(LineSeries),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Decimator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DecimatorProperty = DependencyProperty.Register(
            nameof(Decimator),
            typeof(Action<List<ScreenPoint>, List<ScreenPoint>>),
            typeof(LineSeries),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelFormatString"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFormatStringProperty = DependencyProperty.Register(
            nameof(LabelFormatString),
            typeof(string),
            typeof(LineSeries),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelMarginProperty = DependencyProperty.Register(
            nameof(LabelMargin),
            typeof(double),
            typeof(LineSeries),
            new PropertyMetadata(6.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineJoin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineJoinProperty = DependencyProperty.Register(
            nameof(LineJoin),
            typeof(LineJoin),
            typeof(LineSeries),
            new PropertyMetadata(LineJoin.Bevel, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineLegendPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineLegendPositionProperty = DependencyProperty.Register(
            nameof(LineLegendPosition),
            typeof(LineLegendPosition),
            typeof(LineSeries),
            new PropertyMetadata(LineLegendPosition.None, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStyleProperty = DependencyProperty.Register(
            nameof(LineStyle),
            typeof(LineStyle),
            typeof(LineSeries),
            new PropertyMetadata(LineStyle.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MarkerFill"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkerFillProperty = DependencyProperty.Register(
            nameof(MarkerFill),
            typeof(Color),
            typeof(LineSeries),
            new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MarkerOutline"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkerOutlineProperty = DependencyProperty.Register(
            nameof(MarkerOutline),
            typeof(Point[]),
            typeof(LineSeries),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MarkerResolution"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkerResolutionProperty = DependencyProperty.Register(
            nameof(MarkerResolution),
            typeof(int),
            typeof(LineSeries),
            new PropertyMetadata(0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MarkerSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkerSizeProperty = DependencyProperty.Register(
            nameof(MarkerSize),
            typeof(double),
            typeof(LineSeries),
            new PropertyMetadata(3.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MarkerStroke"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkerStrokeProperty = DependencyProperty.Register(
            nameof(MarkerStroke),
            typeof(Color),
            typeof(LineSeries),
            new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MarkerStrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkerStrokeThicknessProperty = DependencyProperty.Register(
            nameof(MarkerStrokeThickness),
            typeof(double),
            typeof(LineSeries),
            new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MarkerType"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkerTypeProperty = DependencyProperty.Register(
            nameof(MarkerType),
            typeof(MarkerType),
            typeof(LineSeries),
            new PropertyMetadata(MarkerType.None, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumSegmentLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumSegmentLengthProperty = DependencyProperty.Register(
            nameof(MinimumSegmentLength),
            typeof(double),
            typeof(LineSeries),
            new PropertyMetadata(2.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="InterpolationAlgorithm"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InterpolationAlgorithmProperty = DependencyProperty.Register(
            nameof(InterpolationAlgorithm),
            typeof(IInterpolationAlgorithm),
            typeof(LineSeries),
            new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(LineSeries),
            new PropertyMetadata(2.0, AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="LineSeries" /> class.
        /// </summary>
        static LineSeries()
        {
            CanTrackerInterpolatePointsProperty.OverrideMetadata(
                typeof(LineSeries),
                new PropertyMetadata(true, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineSeries" /> class.
        /// </summary>
        public LineSeries()
        {
            this.InternalSeries = new OxyPlot.Series.LineSeries();
        }

        /// <summary>
        /// Gets or sets the color of broken line segments. The default is <see cref="MoreColors.Undefined"/>.
        /// </summary>
        /// <value>The color used for broken line segments.</value>
        /// <remarks>
        /// Set to <see cref="MoreColors.Automatic"/> to use the same color as the main line.
        /// Add <see cref="DataPoint.Undefined"/> to the points collection to create breaks in the line.
        /// </remarks>
        public Color BrokenLineColor
        {
            get => (Color)this.GetValue(BrokenLineColorProperty);
            set => this.SetValue(BrokenLineColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style for broken line segments. The default is <see cref="OxyPlot.LineStyle.Solid"/>.
        /// </summary>
        /// <value>The line style for broken segments.</value>
        public LineStyle BrokenLineStyle
        {
            get => (LineStyle)this.GetValue(BrokenLineStyleProperty);
            set => this.SetValue(BrokenLineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the thickness for broken line segments. The default is <c>0</c> (no broken line rendered).
        /// </summary>
        /// <value>The thickness of broken line segments in pixels.</value>
        public double BrokenLineThickness
        {
            get => (double)this.GetValue(BrokenLineThicknessProperty);
            set => this.SetValue(BrokenLineThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the dash array for the line. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// An array of dash lengths, or <c>null</c> to use the <see cref="LineStyle"/> property.
        /// </value>
        /// <remarks>
        /// If this property is set, it overrides the <see cref="LineStyle"/> property.
        /// </remarks>
        public double[] Dashes
        {
            get => (double[])this.GetValue(DashesProperty);
            set => this.SetValue(DashesProperty, value);
        }

        /// <summary>
        /// Gets or sets the decimator function for performance optimization. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// A function that reduces the number of points to render for improved performance.
        /// </value>
        /// <remarks>
        /// The decimator can significantly improve rendering performance for large datasets.
        /// </remarks>
        public Action<List<ScreenPoint>, List<ScreenPoint>> Decimator
        {
            get => (Action<List<ScreenPoint>, List<ScreenPoint>>)this.GetValue(DecimatorProperty);
            set => this.SetValue(DecimatorProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string for point labels. The default is <c>null</c> (no labels).
        /// </summary>
        /// <value>The format string for labels displayed at each data point.</value>
        public string LabelFormatString
        {
            get => (string)this.GetValue(LabelFormatStringProperty);
            set => this.SetValue(LabelFormatStringProperty, value);
        }

        /// <summary>
        /// Gets or sets the margin between the point and its label. The default is <c>6</c>.
        /// </summary>
        /// <value>The margin in pixels.</value>
        public double LabelMargin
        {
            get => (double)this.GetValue(LabelMarginProperty);
            set => this.SetValue(LabelMarginProperty, value);
        }

        /// <summary>
        /// Gets or sets the line join type. The default is <see cref="OxyPlot.LineJoin.Bevel"/>.
        /// </summary>
        /// <value>The line join style used when connecting line segments.</value>
        public LineJoin LineJoin
        {
            get => (LineJoin)this.GetValue(LineJoinProperty);
            set => this.SetValue(LineJoinProperty, value);
        }

        /// <summary>
        /// Gets or sets the position of a legend rendered on the line. The default is <see cref="OxyPlot.Series.LineLegendPosition.None"/>.
        /// </summary>
        /// <value>The position where the series title is rendered on the line.</value>
        public LineLegendPosition LineLegendPosition
        {
            get => (LineLegendPosition)this.GetValue(LineLegendPositionProperty);
            set => this.SetValue(LineLegendPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style. The default is <see cref="OxyPlot.LineStyle.Automatic"/>.
        /// </summary>
        /// <value>The style of the line (solid, dashed, etc.).</value>
        public LineStyle LineStyle
        {
            get => (LineStyle)this.GetValue(LineStyleProperty);
            set => this.SetValue(LineStyleProperty, value);
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
        /// An array of points defining a custom marker shape.
        /// Set <see cref="MarkerType"/> to <see cref="OxyPlot.MarkerType.Custom"/> to use this property.
        /// </value>
        public Point[] MarkerOutline
        {
            get => (Point[])this.GetValue(MarkerOutlineProperty);
            set => this.SetValue(MarkerOutlineProperty, value);
        }

        /// <summary>
        /// Gets or sets the marker resolution for performance optimization. The default is <c>0</c>.
        /// </summary>
        /// <value>
        /// The resolution value. Higher values reduce the number of markers drawn.
        /// </value>
        public int MarkerResolution
        {
            get => (int)this.GetValue(MarkerResolutionProperty);
            set => this.SetValue(MarkerResolutionProperty, value);
        }

        /// <summary>
        /// Gets or sets the marker size. The default is <c>3</c>.
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
        /// Gets or sets the marker type. The default is <see cref="OxyPlot.MarkerType.None"/>.
        /// </summary>
        /// <value>The type of marker to display at each data point.</value>
        /// <remarks>
        /// If <see cref="OxyPlot.MarkerType.Custom"/> is used, the <see cref="MarkerOutline"/> property must be set.
        /// </remarks>
        public MarkerType MarkerType
        {
            get => (MarkerType)this.GetValue(MarkerTypeProperty);
            set => this.SetValue(MarkerTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum segment length for rendering. The default is <c>2</c>.
        /// </summary>
        /// <value>
        /// The minimum length in pixels. Segments shorter than this are not rendered.
        /// </value>
        /// <remarks>
        /// Increasing this value improves performance but reduces curve accuracy.
        /// </remarks>
        public double MinimumSegmentLength
        {
            get => (double)this.GetValue(MinimumSegmentLengthProperty);
            set => this.SetValue(MinimumSegmentLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the interpolation algorithm for smoothing. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// An interpolation algorithm (e.g., <see cref="CanonicalSpline"/>) for smoothing the line.
        /// </value>
        public IInterpolationAlgorithm InterpolationAlgorithm
        {
            get => (IInterpolationAlgorithm)this.GetValue(InterpolationAlgorithmProperty);
            set => this.SetValue(InterpolationAlgorithmProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness of the line. The default is <c>2</c>.
        /// </summary>
        /// <value>The thickness of the line in pixels.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Series.LineSeries"/> model.</returns>
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

            if (series is OxyPlot.Series.LineSeries s)
            {
                s.Color = this.Color.ToOxyColor();
                s.StrokeThickness = this.StrokeThickness;
                s.LineStyle = this.LineStyle;
                s.LineJoin = this.LineJoin;
                s.LineLegendPosition = this.LineLegendPosition;

                s.MarkerFill = this.MarkerFill.ToOxyColor();
                s.MarkerOutline = this.MarkerOutline?.ToScreenPointArray();
                s.MarkerResolution = this.MarkerResolution;
                s.MarkerSize = this.MarkerSize;
                s.MarkerStroke = this.MarkerStroke.ToOxyColor();
                s.MarkerStrokeThickness = this.MarkerStrokeThickness;
                s.MarkerType = this.MarkerType;

                s.MinimumSegmentLength = this.MinimumSegmentLength;
                s.Dashes = this.Dashes;
                s.Decimator = this.Decimator;

                s.LabelFormatString = this.LabelFormatString;
                s.LabelMargin = this.LabelMargin;

                s.BrokenLineColor = this.BrokenLineColor.ToOxyColor();
                s.BrokenLineStyle = this.BrokenLineStyle;
                s.BrokenLineThickness = this.BrokenLineThickness;

                s.InterpolationAlgorithm = this.InterpolationAlgorithm;
            }
        }
    }
}
