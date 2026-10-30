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
    using Avalonia;
    using Avalonia.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.LineSeries"/>.
    /// </summary>
    /// <remarks>
    /// This class provides WPF dependency properties for all LineSeries properties,
    /// enabling data binding and XAML support for line series configuration.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF.
    /// </remarks>
    public class LineSeries : DataPointSeries
    {
        /// <summary>
        /// Identifies the <see cref="BrokenLineColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> BrokenLineColorProperty = AvaloniaProperty.Register<LineSeries, Color>(
            nameof(BrokenLineColor),
            MoreColors.Undefined);

        /// <summary>
        /// Identifies the <see cref="BrokenLineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> BrokenLineStyleProperty = AvaloniaProperty.Register<LineSeries, LineStyle>(
            nameof(BrokenLineStyle),
            LineStyle.Solid);

        /// <summary>
        /// Identifies the <see cref="BrokenLineThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> BrokenLineThicknessProperty = AvaloniaProperty.Register<LineSeries, double>(
            nameof(BrokenLineThickness),
            0d);

        /// <summary>
        /// Identifies the <see cref="Dashes"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double[]> DashesProperty = AvaloniaProperty.Register<LineSeries, double[]>(
            nameof(Dashes),
            null);

        /// <summary>
        /// Identifies the <see cref="Decimator"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Action<List<ScreenPoint>, List<ScreenPoint>>> DecimatorProperty = AvaloniaProperty.Register<LineSeries, Action<List<ScreenPoint>, List<ScreenPoint>>>(
            nameof(Decimator),
            null);

        /// <summary>
        /// Identifies the <see cref="LabelFormatString"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> LabelFormatStringProperty = AvaloniaProperty.Register<LineSeries, string>(
            nameof(LabelFormatString),
            null);

        /// <summary>
        /// Identifies the <see cref="LabelMargin"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LabelMarginProperty = AvaloniaProperty.Register<LineSeries, double>(
            nameof(LabelMargin),
            6.0);

        /// <summary>
        /// Identifies the <see cref="LineJoin"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineJoin> LineJoinProperty = AvaloniaProperty.Register<LineSeries, LineJoin>(
            nameof(LineJoin),
            LineJoin.Bevel);

        /// <summary>
        /// Identifies the <see cref="LineLegendPosition"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineLegendPosition> LineLegendPositionProperty = AvaloniaProperty.Register<LineSeries, LineLegendPosition>(
            nameof(LineLegendPosition),
            LineLegendPosition.None);

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> LineStyleProperty = AvaloniaProperty.Register<LineSeries, LineStyle>(
            nameof(LineStyle),
            LineStyle.Automatic);

        /// <summary>
        /// Identifies the <see cref="MarkerFill"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> MarkerFillProperty = AvaloniaProperty.Register<LineSeries, Color>(
            nameof(MarkerFill),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="MarkerOutline"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Point[]> MarkerOutlineProperty = AvaloniaProperty.Register<LineSeries, Point[]>(
            nameof(MarkerOutline),
            null);

        /// <summary>
        /// Identifies the <see cref="MarkerResolution"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<int> MarkerResolutionProperty = AvaloniaProperty.Register<LineSeries, int>(
            nameof(MarkerResolution),
            0);

        /// <summary>
        /// Identifies the <see cref="MarkerSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MarkerSizeProperty = AvaloniaProperty.Register<LineSeries, double>(
            nameof(MarkerSize),
            3.0);

        /// <summary>
        /// Identifies the <see cref="MarkerStroke"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> MarkerStrokeProperty = AvaloniaProperty.Register<LineSeries, Color>(
            nameof(MarkerStroke),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="MarkerStrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MarkerStrokeThicknessProperty = AvaloniaProperty.Register<LineSeries, double>(
            nameof(MarkerStrokeThickness),
            1.0);

        /// <summary>
        /// Identifies the <see cref="MarkerType"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<MarkerType> MarkerTypeProperty = AvaloniaProperty.Register<LineSeries, MarkerType>(
            nameof(MarkerType),
            MarkerType.None);

        /// <summary>
        /// Identifies the <see cref="MinimumSegmentLength"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumSegmentLengthProperty = AvaloniaProperty.Register<LineSeries, double>(
            nameof(MinimumSegmentLength),
            2.0);

        /// <summary>
        /// Identifies the <see cref="InterpolationAlgorithm"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<IInterpolationAlgorithm> InterpolationAlgorithmProperty = AvaloniaProperty.Register<LineSeries, IInterpolationAlgorithm>(
            nameof(InterpolationAlgorithm),
            null);

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<LineSeries, double>(
            nameof(StrokeThickness),
            2.0);

        /// <summary>
        /// Initializes static members of the <see cref="LineSeries" /> class.
        /// </summary>
        static LineSeries()
        {
            BrokenLineColorProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            BrokenLineStyleProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            BrokenLineThicknessProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            DashesProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            DecimatorProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            LabelFormatStringProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            LabelMarginProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            LineJoinProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            LineLegendPositionProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            LineStyleProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            MarkerFillProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            MarkerOutlineProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            MarkerResolutionProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            MarkerSizeProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            MarkerStrokeProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            MarkerStrokeThicknessProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            MarkerTypeProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            MinimumSegmentLengthProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            InterpolationAlgorithmProperty.Changed.AddClassHandler<LineSeries>(DataChanged);
            StrokeThicknessProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
            CanTrackerInterpolatePointsProperty.OverrideDefaultValue<LineSeries>(true);
            CanTrackerInterpolatePointsProperty.Changed.AddClassHandler<LineSeries>(AppearanceChanged);
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
