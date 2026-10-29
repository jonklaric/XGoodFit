// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LineSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a line series.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OxyPlot.Axes;

    /// <summary>
    /// Represents a line series.
    /// </summary>
    public class LineSeries : DataPointSeries
    {
        /// <summary>
        /// The divisor value used to calculate tolerance for line smoothing.
        /// </summary>
        private const double ToleranceDivisor = 200;

        /// <summary>
        /// The output buffer.
        /// </summary>
        private List<ScreenPoint> outputBuffer;

        /// <summary>
        /// The buffer for contiguous screen points.
        /// </summary>
        private List<ScreenPoint> contiguousScreenPointsBuffer;

        /// <summary>
        /// The buffer for decimated points.
        /// </summary>
        private List<ScreenPoint> decimatorBuffer;

        /// <summary>
        /// Set to true during <see cref="RenderPoints"/> when the fused decimation path is active.
        /// Used by <see cref="RenderLine"/> to skip the redundant ReducePoints pass.
        /// </summary>
        private bool decimationActive;

        /// <summary>
        /// Single-element marker-size array passed to <c>IRenderContext.DrawMarkers</c>.
        /// Cached so we don't allocate a new <c>double[]</c> on every render call. Rebuilt
        /// when <see cref="MarkerSize"/> changes.
        /// </summary>
        private double[] markerSizeArray;

        /// <summary>
        /// The <see cref="MarkerSize"/> value reflected in <see cref="markerSizeArray"/>.
        /// Compared on each render to detect a marker-size change and rebuild the array.
        /// NaN sentinel forces initialization on first use.
        /// </summary>
        private double markerSizeArrayValue = double.NaN;

        /// <summary>
        /// The default color.
        /// </summary>
        private OxyColor defaultColor;

        /// <summary>
        /// The default marker fill color.
        /// </summary>
        private OxyColor defaultMarkerFill;

        /// <summary>
        /// The default line style.
        /// </summary>
        private LineStyle defaultLineStyle;

        /// <summary>
        /// The smoothed points.
        /// </summary>
        private List<DataPoint> smoothedPoints;

        /// <summary>
        /// Initializes a new instance of the <see cref = "LineSeries" /> class.
        /// </summary>
        public LineSeries()
        {
            this.StrokeThickness = 2;
            this.LineJoin = LineJoin.Bevel;
            this.LineStyle = LineStyle.Automatic;

            this.Color = OxyColors.Automatic;
            this.BrokenLineColor = OxyColors.Undefined;

            this.MarkerFill = OxyColors.Automatic;
            this.MarkerStroke = OxyColors.Automatic;
            this.MarkerResolution = 0;
            this.MarkerSize = 3;
            this.MarkerStrokeThickness = 1;
            this.MarkerType = MarkerType.None;

            this.MinimumSegmentLength = 2;

            this.CanTrackerInterpolatePoints = true;
            this.LabelMargin = 6;
            this.smoothedPoints = new List<DataPoint>();
        }

        /// <summary>
        /// Gets or sets the color of the curve.
        /// </summary>
        /// <value>The color.</value>
        public OxyColor Color { get; set; }

        /// <summary>
        /// Gets or sets the color of the broken line segments. The default is <see cref="OxyColors.Undefined"/>. Set it to <see cref="OxyColors.Automatic"/> if it should follow the <see cref="Color" />.
        /// </summary>
        /// <remarks>Add <c>DataPoint.Undefined</c> in the Points collection to create breaks in the line.</remarks>
        public OxyColor BrokenLineColor { get; set; }

        /// <summary>
        /// Gets or sets the broken line style. The default is <see cref="OxyPlot.LineStyle.Solid" />.
        /// </summary>
        public LineStyle BrokenLineStyle { get; set; }

        /// <summary>
        /// Gets or sets the broken line thickness. The default is <c>0</c> (no line).
        /// </summary>
        public double BrokenLineThickness { get; set; }

        /// <summary>
        /// Gets or sets the dash array for the rendered line (overrides <see cref="LineStyle" />). The default is <c>null</c>.
        /// </summary>
        /// <value>The dash array.</value>
        /// <remarks>If this is not <c>null</c> it overrides the <see cref="LineStyle" /> property.</remarks>
        public double[] Dashes { get; set; }

        /// <summary>
        /// Gets or sets the decimator.
        /// </summary>
        /// <value>
        /// The decimator action.
        /// </value>
        /// <remarks>The decimator can be used to improve the performance of the rendering. See the example.</remarks>
        public Action<List<ScreenPoint>, List<ScreenPoint>> Decimator { get; set; }

        /// <summary>
        /// Gets or sets the label format string. The default is <c>null</c> (no labels).
        /// </summary>
        /// <value>The label format string.</value>
        public string LabelFormatString { get; set; }

        /// <summary>
        /// Gets or sets the label margins. The default is <c>6</c>.
        /// </summary>
        public double LabelMargin { get; set; }

        /// <summary>
        /// Gets or sets the line join. The default is <see cref="OxyPlot.LineJoin.Bevel" />.
        /// </summary>
        /// <value>The line join.</value>
        public LineJoin LineJoin { get; set; }

        /// <summary>
        /// Gets or sets the line style. The default is <see cref="OxyPlot.LineStyle.Automatic" />.
        /// </summary>
        /// <value>The line style.</value>
        public LineStyle LineStyle { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the position of a legend rendered on the line. The default is <c>LineLegendPosition.None</c>.
        /// </summary>
        /// <value>A value specifying the position of the legend.</value>
        public LineLegendPosition LineLegendPosition { get; set; }

        /// <summary>
        /// Gets or sets the marker fill color. The default is <see cref="OxyColors.Automatic" />.
        /// </summary>
        /// <value>The marker fill.</value>
        public OxyColor MarkerFill { get; set; }

        /// <summary>
        /// Gets or sets the a custom polygon outline for the markers. Set <see cref="MarkerType" /> to <see cref="OxyPlot.MarkerType.Custom" /> to use this property. The default is <c>null</c>.
        /// </summary>
        /// <value>A polyline.</value>
        public ScreenPoint[] MarkerOutline { get; set; }

        /// <summary>
        /// Gets or sets the marker resolution. The default is <c>0</c>.
        /// </summary>
        /// <value>The marker resolution.</value>
        public int MarkerResolution { get; set; }

        /// <summary>
        /// Gets or sets the size of the marker. The default is <c>3</c>.
        /// </summary>
        /// <value>The size of the marker.</value>
        public double MarkerSize { get; set; }

        /// <summary>
        /// Gets or sets the marker stroke. The default is <c>OxyColors.Automatic</c>.
        /// </summary>
        /// <value>The marker stroke.</value>
        public OxyColor MarkerStroke { get; set; }

        /// <summary>
        /// Gets or sets the marker stroke thickness. The default is <c>2</c>.
        /// </summary>
        /// <value>The marker stroke thickness.</value>
        public double MarkerStrokeThickness { get; set; }

        /// <summary>
        /// Gets or sets the type of the marker. The default is <c>MarkerType.None</c>.
        /// </summary>
        /// <value>The type of the marker.</value>
        /// <remarks>If MarkerType.Custom is used, the MarkerOutline property must be specified.</remarks>
        public MarkerType MarkerType { get; set; }

        /// <summary>
        /// Gets or sets the minimum length of the segment.
        /// Increasing this number will increase performance,
        /// but make the curve less accurate. The default is <c>2</c>.
        /// </summary>
        /// <value>The minimum length of the segment.</value>
        public double MinimumSegmentLength { get; set; }

        /// <summary>
        /// Gets or sets a type of interpolation algorithm used for smoothing this <see cref = "DataPointSeries" />.
        /// </summary>
        /// <value>Type of interpolation algorithm.</value>
        public IInterpolationAlgorithm InterpolationAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of points per render segment for which spline
        /// smoothing (<see cref="InterpolationAlgorithm"/>) is applied. When set to a positive
        /// value, <see cref="RenderLineAndMarkers"/> skips smoothing for segments larger than
        /// this threshold and renders the raw decimated line directly. The default <c>0</c>
        /// disables the threshold (smoothing always runs when an interpolation algorithm is
        /// set). Recommended value for large-data plots: <c>1000</c>.
        /// </summary>
        /// <remarks>
        /// Spline smoothing is O(N) on its input plus a fresh allocation of the smoothed
        /// point list every render. On a 100k-point segment this is a noticeable per-frame
        /// cost; on dense telemetry data it is rarely visible since the smoothing operates
        /// on already-decimated screen points. Setting <c>MaxSmoothingPoints = 1000</c> turns
        /// off smoothing automatically once the visible point count crosses that threshold.
        /// </remarks>
        public int MaxSmoothingPoints { get; set; }

        /// <summary>
        /// Gets or sets the thickness of the curve.
        /// </summary>
        /// <value>The stroke thickness.</value>
        public double StrokeThickness { get; set; }

        /// <summary>
        /// Gets the actual color.
        /// </summary>
        /// <value>The actual color.</value>
        public OxyColor ActualColor
        {
            get
            {
                return this.Color.GetActualColor(this.defaultColor);
            }
        }

        /// <summary>
        /// Gets the actual marker fill color.
        /// </summary>
        /// <value>The actual color.</value>
        public OxyColor ActualMarkerFill
        {
            get
            {
                return this.MarkerFill.GetActualColor(this.defaultMarkerFill);
            }
        }

        /// <summary>
        /// Gets the actual line style.
        /// </summary>
        /// <value>The actual line style.</value>
        protected LineStyle ActualLineStyle
        {
            get
            {
                return this.LineStyle != LineStyle.Automatic ? this.LineStyle : this.defaultLineStyle;
            }
        }

        /// <summary>
        /// Gets the actual dash array for the line.
        /// </summary>
        protected double[] ActualDashArray
        {
            get
            {
                return this.Dashes ?? this.ActualLineStyle.GetDashArray();
            }
        }

        /// <summary>
        /// Gets the smoothed points.
        /// </summary>
        /// <value>The smoothed points.</value>
        protected List<DataPoint> SmoothedPoints
        {
            get
            {
                return this.smoothedPoints;
            }
        }

        /// <summary>
        /// Gets the point on the series that is nearest the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="interpolate">Interpolate the series if this flag is set to <c>true</c>.</param>
        /// <returns>A TrackerHitResult for the current hit.</returns>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            if (interpolate)
            {
                // Cannot interpolate if there is no line
                if (this.ActualColor.IsInvisible() || this.StrokeThickness.Equals(0))
                {
                    return null;
                }

                if (!this.CanTrackerInterpolatePoints)
                {
                    return null;
                }
            }

            if (interpolate && this.InterpolationAlgorithm != null)
            {
                var result = this.GetNearestInterpolatedPointInternal(this.SmoothedPoints, point);
                if (result != null)
                {
                    result.Text = StringHelper.Format(
                        this.ActualCulture,
                        this.TrackerFormatString,
                        result.Item,
                        this.Title,
                        this.XAxis.Title ?? XYAxisSeries.DefaultXAxisTitle,
                        this.XAxis.GetValue(result.DataPoint.X),
                        this.YAxis.Title ?? XYAxisSeries.DefaultYAxisTitle,
                        this.YAxis.GetValue(result.DataPoint.Y));
                }

                return result;
            }

            return base.GetNearestPoint(point, interpolate);
        }

        /// <summary>
        /// Renders the series on the specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        public override void Render(IRenderContext rc)
        {
            var actualPoints = this.ActualPoints;
            if (actualPoints == null || actualPoints.Count == 0)
            {
                return;
            }

#if DEBUG
            using (PlotDiagnostics.Trace("LineSeries.Render",
                $"title=\"{this.Title ?? "(no title)"}\" pts={actualPoints.Count}"))
#endif
            {
                this.VerifyAxes();

                this.RenderPoints(rc, actualPoints);

                if (this.LabelFormatString != null)
                {
                    // render point labels (not optimized for performance)
                    this.RenderPointLabels(rc);
                }

                if (this.LineLegendPosition != LineLegendPosition.None && !string.IsNullOrEmpty(this.Title))
                {
                    // renders a legend on the line
                    this.RenderLegendOnLine(rc);
                }
            }
        }

        /// <summary>
        /// Renders the legend symbol for the line series on the
        /// specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        /// <param name="legendBox">The bounding rectangle of the legend box.</param>
        public override void RenderLegend(IRenderContext rc, OxyRect legendBox)
        {
            double xmid = (legendBox.Left + legendBox.Right) / 2;
            double ymid = (legendBox.Top + legendBox.Bottom) / 2;
            var pts = new[] { new ScreenPoint(legendBox.Left, ymid), new ScreenPoint(legendBox.Right, ymid) };
            rc.DrawLine(
                pts,
                this.GetSelectableColor(this.ActualColor),
                this.StrokeThickness,
                this.EdgeRenderingMode,
                this.ActualDashArray);
            var midpt = new ScreenPoint(xmid, ymid);
            rc.DrawMarker(
                midpt,
                this.MarkerType,
                this.MarkerOutline,
                this.MarkerSize,
                this.ActualMarkerFill,
                this.MarkerStroke,
                this.MarkerStrokeThickness,
                this.EdgeRenderingMode);
        }

        /// <summary>
        /// Sets default values from the plot model.
        /// </summary>
        protected internal override void SetDefaultValues()
        {
            if (this.LineStyle == LineStyle.Automatic)
            {
                this.defaultLineStyle = this.PlotModel.GetDefaultLineStyle();
            }

            if (this.Color.IsAutomatic())
            {
                this.defaultColor = this.PlotModel.GetDefaultColor();
            }

            if (this.MarkerFill.IsAutomatic())
            {
                // No color was explicitly provided. Use the line color if it was set, else use default.
                this.defaultMarkerFill = this.Color.IsAutomatic() ? this.defaultColor : this.Color;
            }
        }

        /// <summary>
        /// Updates the maximum and minimum values of the series.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            if (this.InterpolationAlgorithm != null)
            {
                // Update the max/min from the control points
                base.UpdateMaxMin();

                // Make sure the smooth points are re-evaluated.
                this.ResetSmoothedPoints();

                if (this.SmoothedPoints.All(x => double.IsNaN(x.X)))
                {
                    return;
                }

                // Update the max/min from the smoothed points
                this.MinX = this.SmoothedPoints.Where(x => !double.IsNaN(x.X)).Min(x => x.X);
                this.MinY = this.SmoothedPoints.Where(x => !double.IsNaN(x.Y)).Min(x => x.Y);
                this.MaxX = this.SmoothedPoints.Where(x => !double.IsNaN(x.X)).Max(x => x.X);
                this.MaxY = this.SmoothedPoints.Where(x => !double.IsNaN(x.Y)).Max(x => x.Y);
            }
            else
            {
                base.UpdateMaxMin();
            }
        }

        /// <summary>
        /// Renders the points as line, broken line and markers.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        /// <param name="points">The points to render.</param>
        protected void RenderPoints(IRenderContext rc, IList<DataPoint> points)
        {
            var lastValidPoint = new ScreenPoint?();
            var areBrokenLinesRendered = this.BrokenLineThickness > 0 && this.BrokenLineStyle != LineStyle.None;
            var dashArray = areBrokenLinesRendered ? this.BrokenLineStyle.GetDashArray() : null;
            var broken = areBrokenLinesRendered ? new List<ScreenPoint>(2) : null;

            int startIdx = 0;
            int endIdx = points.Count;
            double xmax = double.MaxValue;

            if (this.IsXMonotonic)
            {
                var xmin = this.XAxis.ClipMinimum;
                xmax = this.XAxis.ClipMaximum;
                // Specialised IList<DataPoint> overload — no Func<T,double> delegate, the
                // JIT inlines the .x field read on every binary-search step.
                this.WindowStartIndex = this.UpdateWindowStartIndex(points, xmin, this.WindowStartIndex);
                startIdx = this.WindowStartIndex;

                // Symmetric upper bound: cap outer-loop iteration at the visible window edge
                // for the slow fallback path (custom decimator, non-base-10 log axis,
                // probability axes). The fused path already exits cleanly when X > xmax inside
                // FusedExtractAndDecimate, so this bound only tightens the worst case for the
                // fallback — and never affects correctness because ExtractNextContiguousLineSegment
                // also returns false when X > xmax.
                this.WindowEndIndex = this.UpdateWindowEndIndex(points, xmax, this.WindowEndIndex);
                if (this.WindowEndIndex >= 0 && this.WindowEndIndex < points.Count)
                {
                    // Add one for line-continuity past the right edge (matches how Window
                    // StartIndex steps back by one for left-edge continuity).
                    endIdx = Math.Min(points.Count, this.WindowEndIndex + 2);
                }
            }

            // Determine if fused extract-decimate-transform is possible.
            // Requires: rendering to screen, monotonic X, standard decimator. The fused path
            // supports linear axes plus base-10 LogarithmicAxis (covers the overwhelming
            // common case; LogarithmicAxis.Base defaults to 10). Non-base-10 log axes,
            // GumbelProbabilityAxis, and NormalProbabilityAxis use transforms that cannot be
            // cheaply inlined and fall back to the slow path.
            bool useFused = rc.RendersToScreen
                && this.IsXMonotonic
                && this.Decimator == OxyPlot.Decimator.Decimate
                && !(this.XAxis is GumbelProbabilityAxis)
                && !(this.XAxis is NormalProbabilityAxis)
                && !(this.YAxis is GumbelProbabilityAxis)
                && !(this.YAxis is NormalProbabilityAxis)
                && !(this.XAxis is LogarithmicAxis xLogAxisCheck && xLogAxisCheck.Base != 10)
                && !(this.YAxis is LogarithmicAxis yLogAxisCheck && yLogAxisCheck.Base != 10);

            this.decimationActive = useFused || this.Decimator != null;

#if DEBUG
            if (PlotDiagnostics.IsActive)
            {
                PlotDiagnostics.Log(
                    $"LineSeries.RenderPoints title=\"{this.Title ?? "(no title)"}\" useFused={useFused} " +
                    $"isXMonotonic={this.IsXMonotonic} decimator={(this.Decimator == OxyPlot.Decimator.Decimate ? "std" : (this.Decimator == null ? "none" : "custom"))} " +
                    $"rendersToScreen={rc.RendersToScreen} startIdx={startIdx} pts={points.Count}");
            }
#endif

            if (useFused)
            {
                // Fast path: single-pass fused extract + decimate + transform.
                // Only transforms the 1-4 surviving points per pixel column.
                if (this.decimatorBuffer == null)
                {
                    this.decimatorBuffer = new List<ScreenPoint>(4096);
                }

                for (int i = startIdx; i < points.Count; i++)
                {
                    this.decimatorBuffer.Clear();

                    if (!this.FusedExtractAndDecimate(points, ref i, ref lastValidPoint, xmax, broken, this.decimatorBuffer))
                    {
                        break;
                    }

                    if (areBrokenLinesRendered && broken?.Count > 0)
                    {
                        var actualBrokenLineColor = this.BrokenLineColor.IsAutomatic()
                            ? this.ActualColor : this.BrokenLineColor;
                        rc.DrawLineSegments(broken, actualBrokenLineColor, this.BrokenLineThickness,
                            this.EdgeRenderingMode, dashArray, this.LineJoin);
                        broken.Clear();
                    }
                    else if (!areBrokenLinesRendered)
                    {
                        lastValidPoint = null;
                    }

                    this.RenderLineAndMarkers(rc, this.decimatorBuffer);
                }
            }
            else
            {
                // Fallback path: existing extract + optional decimate pipeline.
                if (this.contiguousScreenPointsBuffer == null)
                {
                    this.contiguousScreenPointsBuffer = new List<ScreenPoint>(points.Count);
                }

                // Cap outer iteration at endIdx (== WindowEndIndex+2 for monotonic, points.Count
                // otherwise). For non-monotonic data we still scan the full collection because
                // X is unsorted and ExtractNextContiguousLineSegment's internal clipCount is
                // the only safe terminator.
                for (int i = startIdx; i < endIdx; i++)
                {
                    if (!this.ExtractNextContiguousLineSegment(points, ref i, ref lastValidPoint, xmax, broken, this.contiguousScreenPointsBuffer))
                    {
                        break;
                    }

                    if (areBrokenLinesRendered && broken?.Count > 0)
                    {
                        var actualBrokenLineColor = this.BrokenLineColor.IsAutomatic()
                            ? this.ActualColor : this.BrokenLineColor;
                        rc.DrawLineSegments(broken, actualBrokenLineColor, this.BrokenLineThickness,
                            this.EdgeRenderingMode, dashArray, this.LineJoin);
                        broken.Clear();
                    }
                    else if (!areBrokenLinesRendered)
                    {
                        lastValidPoint = null;
                    }

                    if (this.Decimator != null)
                    {
                        if (this.decimatorBuffer == null)
                        {
                            this.decimatorBuffer = new List<ScreenPoint>(this.contiguousScreenPointsBuffer.Count);
                        }
                        else
                        {
                            this.decimatorBuffer.Clear();
                        }

                        this.Decimator(this.contiguousScreenPointsBuffer, this.decimatorBuffer);
                        this.RenderLineAndMarkers(rc, this.decimatorBuffer);
                    }
                    else
                    {
                        this.RenderLineAndMarkers(rc, this.contiguousScreenPointsBuffer);
                    }

                    this.contiguousScreenPointsBuffer.Clear();
                }
            }
        }

	    /// <summary>
	    /// Extracts a single contiguous line segment beginning with the element at the position of the enumerator when the method
	    /// is called. Initial invalid data points are ignored.
	    /// </summary>
	    /// <param name="pointIdx">Current point index</param>
	    /// <param name="previousContiguousLineSegmentEndPoint">Initially set to null, but I will update I won't give a broken line if this is null</param>
	    /// <param name="xmax">Maximum visible X value</param>
	    /// <param name="broken">place to put broken segment</param>
	    /// <param name="contiguous">place to put contiguous segment</param>
	    /// <param name="points">Points collection</param>
	    /// <returns>
	    ///   <c>true</c> if line segments are extracted, <c>false</c> if reached end.
	    /// </returns>
	    protected bool ExtractNextContiguousLineSegment(
			IList<DataPoint> points,
			ref int pointIdx,
			ref ScreenPoint? previousContiguousLineSegmentEndPoint,
			double xmax,
            // ReSharper disable SuggestBaseTypeForParameter
            List<ScreenPoint> broken,
            List<ScreenPoint> contiguous)
        // ReSharper restore SuggestBaseTypeForParameter
        {
            DataPoint currentPoint = default(DataPoint);
		    bool hasValidPoint = false;

            // Detect the fast-path conditions once, outside the per-point loop. When both axes
            // use default filters and no FilterFunction is set (the common case for telemetry
            // data), validity reduces to a NaN/Infinity check that can be inlined as a struct
            // comparison rather than a virtual IsValidPoint dispatch — multiplied by N points
            // this is a measurable speedup for large datasets in the fallback path.
            bool fastValidity = this.XAxis != null && this.YAxis != null
                && this.XAxis.FilterFunction == null && this.YAxis.FilterFunction == null
                && this.XAxis.FilterMinValue == double.MinValue
                && this.XAxis.FilterMaxValue == double.MaxValue
                && this.YAxis.FilterMinValue == double.MinValue
                && this.YAxis.FilterMaxValue == double.MaxValue;

            // Skip all undefined points
		    for (; pointIdx < points.Count; pointIdx++)
		    {
				currentPoint = points[pointIdx];
			    if (currentPoint.X > xmax)
			    {
				    return false;
			    }

				// ReSharper disable once AssignmentInConditionalExpression
			    bool isValid = fastValidity
                    ? IsFiniteFast(currentPoint.X) && IsFiniteFast(currentPoint.Y)
                    : this.IsValidPoint(currentPoint);
                if (hasValidPoint = isValid)
			    {
				    break;
			    }
		    }

		    if (!hasValidPoint)
		    {
			    return false;
		    }

            // First valid point
            var screenPoint = this.Transform(currentPoint);

            // Handle broken line segment if exists
            if (previousContiguousLineSegmentEndPoint.HasValue)
            {
                broken.Add(previousContiguousLineSegmentEndPoint.Value);
                broken.Add(screenPoint);
            }

            // Add first point
            contiguous.Add(screenPoint);

			// Add all points up until the next invalid one
			int clipCount = 0;
			for (pointIdx++; pointIdx < points.Count; pointIdx++)
		    {
				currentPoint = points[pointIdx];
				clipCount += currentPoint.X > xmax ? 1 : 0;
				if (clipCount > 1)
				{
					break;
				}
				bool isValidLoop = fastValidity
                    ? IsFiniteFast(currentPoint.X) && IsFiniteFast(currentPoint.Y)
                    : this.IsValidPoint(currentPoint);
                if (!isValidLoop)
			    {
				    break;
			    }

				screenPoint = this.Transform(currentPoint);
				contiguous.Add(screenPoint);
			}

			previousContiguousLineSegmentEndPoint = screenPoint;

            return true;
        }

        /// <summary>
        /// Inlinable finite-value check used by the validity-fast-path in
        /// <see cref="ExtractNextContiguousLineSegment"/>. Equivalent to
        /// <c>!double.IsNaN(value) &amp;&amp; !double.IsInfinity(value)</c> but written as a
        /// pair of struct comparisons so the JIT can inline it inside hot loops.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool IsFiniteFast(double value)
        {
            // value == value rejects NaN; the bound check rejects ±Infinity. The constants are
            // double.MaxValue / double.MinValue so finite values pass and infinities fail.
            // CS1718 suppression: the self-comparison is intentional — that's the canonical
            // NaN check; mirrors Axis.IsValidValue.
#pragma warning disable 1718
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return value == value && value <= double.MaxValue && value >= double.MinValue;
#pragma warning restore 1718
        }

        /// <summary>
        /// Single-pass fused extraction, decimation, and transformation.
        /// Computes pixel columns from data coordinates and only transforms the 1-4 surviving
        /// points per column (first, min, max, last), avoiding ~90% of Transform() calls.
        /// Supports both linear and logarithmic axes.
        /// </summary>
        private bool FusedExtractAndDecimate(
            IList<DataPoint> points,
            ref int pointIdx,
            ref ScreenPoint? previousContiguousLineSegmentEndPoint,
            double xmax,
            List<ScreenPoint> broken,
            List<ScreenPoint> output)
        {
            // Skip invalid/pre-window points
            DataPoint currentPoint = default;
            bool hasValidPoint = false;
            for (; pointIdx < points.Count; pointIdx++)
            {
                currentPoint = points[pointIdx];
                if (currentPoint.X > xmax) return false;
                if (hasValidPoint = this.IsValidPoint(currentPoint)) break;
            }
            if (!hasValidPoint) return false;

            // Cache axis transform parameters for inline computation. Linear axes use the
            // base Axis.Transform formula (v - offset) * scale; logarithmic axes use the
            // same formula composed with Math.Log10. The xIsLog/yIsLog booleans are captured
            // by the local functions below — the resulting per-point branch is far cheaper
            // than the virtual Transform() call it replaces (~10ns → ~1ns per point).
            // IsValidPoint already filters non-positive values when an axis is logarithmic,
            // so Math.Log10 is never called on invalid inputs here.
            //
            // Implicit coupling with the useFused gate above: if xIsLog or yIsLog is true,
            // the gate has already verified Base == 10 (LogarithmicAxis subclasses with a
            // non-10 base fall through to the slow path). If a future axis subclass overrides
            // IsLogarithmic() to return true without honoring the Base==10 contract, the
            // gate must be updated alongside this inline.
            bool xIsLog = this.XAxis.IsLogarithmic();
            bool yIsLog = this.YAxis.IsLogarithmic();
            double xOffset = this.XAxis.Offset;
            double xScale = this.XAxis.Scale;
            double yOffset = this.YAxis.Offset;
            double yScale = this.YAxis.Scale;

            double TransformX(double v) => xIsLog ? (System.Math.Log10(v) - xOffset) * xScale : (v - xOffset) * xScale;
            double TransformY(double v) => yIsLog ? (System.Math.Log10(v) - yOffset) * yScale : (v - yOffset) * yScale;

            // First valid point
            double firstSX = TransformX(currentPoint.X);
            double firstSY = TransformY(currentPoint.Y);
            var firstSP = new ScreenPoint(firstSX, firstSY);

            if (previousContiguousLineSegmentEndPoint.HasValue)
            {
                broken?.Add(previousContiguousLineSegmentEndPoint.Value);
                broken?.Add(firstSP);
            }

            // Min-max decimation: accumulate per pixel column
            double currentCol = Math.Round(firstSX);
            double colFirstY = firstSY;
            double colLastY = firstSY;
            double colMinY = firstSY;
            double colMaxY = firstSY;

            int clipCount = 0;
            var lastScreenPoint = firstSP;

            for (pointIdx++; pointIdx < points.Count; pointIdx++)
            {
                currentPoint = points[pointIdx];
                clipCount += currentPoint.X > xmax ? 1 : 0;
                if (clipCount > 1) break;
                if (!this.IsValidPoint(currentPoint)) break;

                double sx = TransformX(currentPoint.X);
                double newCol = Math.Round(sx);

                if (newCol != currentCol)
                {
                    // Flush current column
                    OxyPlot.Decimator.AddVerticalPoints(output, currentCol, colFirstY, colLastY, colMinY, colMaxY);
                    currentCol = newCol;
                    double sy = TransformY(currentPoint.Y);
                    colFirstY = colLastY = colMinY = colMaxY = sy;
                    lastScreenPoint = new ScreenPoint(sx, sy);
                }
                else
                {
                    double sy = TransformY(currentPoint.Y);
                    if (sy < colMinY) colMinY = sy;
                    if (sy > colMaxY) colMaxY = sy;
                    colLastY = sy;
                    lastScreenPoint = new ScreenPoint(sx, sy);
                }
            }

            // Flush last column
            OxyPlot.Decimator.AddVerticalPoints(output, currentCol, colFirstY, colLastY, colMinY, colMaxY);
            previousContiguousLineSegmentEndPoint = lastScreenPoint;
            return true;
        }

        /// <summary>
        /// Renders the point labels.
        /// </summary>
        /// <param name="rc">The render context.</param>
        protected void RenderPointLabels(IRenderContext rc)
        {
            // Restrict label rendering to the visible window for monotonic series. On a
            // 100k-point dataset, labels for off-screen points were previously formatted
            // and submitted to DrawText anyway — even though they would clip outside the
            // plot area. The window indices were already computed by RenderPoints upstream.
            var actualPoints = this.ActualPoints;
            int startIdx = 0;
            int endIdx = actualPoints.Count;
            if (this.IsXMonotonic)
            {
                startIdx = Math.Max(0, this.WindowStartIndex);
                if (this.WindowEndIndex >= 0 && this.WindowEndIndex < actualPoints.Count)
                {
                    endIdx = Math.Min(actualPoints.Count, this.WindowEndIndex + 2);
                }
            }

            for (int index = startIdx; index < endIdx; index++)
            {
                var point = actualPoints[index];
                if (!this.IsValidPoint(point))
                {
                    continue;
                }

                var pt = this.Transform(point) + new ScreenVector(0, -this.LabelMargin);

                var item = this.GetItem(index);
                var s = StringHelper.Format(this.ActualCulture, this.LabelFormatString, item, point.X, point.Y);

#if SUPPORTLABELPLACEMENT
                    switch (this.LabelPlacement)
                    {
                        case LabelPlacement.Inside:
                            pt = new ScreenPoint(rect.Right - this.LabelMargin, (rect.Top + rect.Bottom) / 2);
                            ha = HorizontalAlignment.Right;
                            break;
                        case LabelPlacement.Middle:
                            pt = new ScreenPoint((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
                            ha = HorizontalAlignment.Center;
                            break;
                        case LabelPlacement.Base:
                            pt = new ScreenPoint(rect.Left + this.LabelMargin, (rect.Top + rect.Bottom) / 2);
                            ha = HorizontalAlignment.Left;
                            break;
                        default: // Outside
                            pt = new ScreenPoint(rect.Right + this.LabelMargin, (rect.Top + rect.Bottom) / 2);
                            ha = HorizontalAlignment.Left;
                            break;
                    }
#endif

                rc.DrawText(
                    pt,
                    s,
                    this.ActualTextColor,
                    this.ActualFont,
                    this.ActualFontSize,
                    this.ActualFontWeight,
                    0,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Bottom);
            }
        }

        /// <summary>
        /// Renders a legend on the line.
        /// </summary>
        /// <param name="rc">The render context.</param>
        protected void RenderLegendOnLine(IRenderContext rc)
        {
            // Find the position
            DataPoint point;
            HorizontalAlignment ha;
            var va = VerticalAlignment.Middle;
            double dx = 4;

            switch (this.LineLegendPosition)
            {
                case LineLegendPosition.Start:
                    point = this.ActualPoints[0];
                    ha = HorizontalAlignment.Right;
                    dx = -dx;
                    break;
                case LineLegendPosition.End:
                    point = this.ActualPoints[this.ActualPoints.Count - 1];
                    ha = HorizontalAlignment.Left;
                    break;
                case LineLegendPosition.None:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.Orientate(ref ha, ref va);
            var pt = this.Transform(point) + this.Orientate(new ScreenVector(dx, 0));

            // Render the legend
            rc.DrawText(
                pt,
                this.Title,
                this.ActualTextColor,
                this.ActualFont,
                this.ActualFontSize,
                this.ActualFontWeight,
                0,
                ha,
                va);
        }

        /// <summary>
        /// Renders the transformed points as a line (smoothed if <see cref="InterpolationAlgorithm"/> isn’t <c>null</c>) and markers (if <see cref="MarkerType"/> is not <c>None</c>).
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="pointsToRender">The points to render.</param>
        protected virtual void RenderLineAndMarkers(IRenderContext rc, IList<ScreenPoint> pointsToRender)
        {
            var screenPoints = pointsToRender;
            if (this.InterpolationAlgorithm != null)
            {
                // Spline smoothing (should only be used on small datasets). When
                // MaxSmoothingPoints is set and the current segment exceeds it, skip the
                // smooth — the decimated raw line is visually indistinguishable at that
                // density, and skipping eliminates the per-render allocation and O(N) walk.
                if (this.MaxSmoothingPoints > 0 && pointsToRender.Count > this.MaxSmoothingPoints)
                {
                    // Smoothing skipped — the raw points are used as-is.
                }
                else
                {
                    var resampledPoints = ScreenPointHelper.ResamplePoints(pointsToRender, this.MinimumSegmentLength);
                    screenPoints = this.InterpolationAlgorithm.CreateSpline(resampledPoints, false, 0.25);
                }
            }

            // clip the line segments with the clipping rectangle
            if (this.StrokeThickness > 0 && this.ActualLineStyle != LineStyle.None)
            {
                this.RenderLine(rc, screenPoints);
            }

            if (this.MarkerType != MarkerType.None)
            {
                var markerBinOffset = this.MarkerResolution > 0 ? this.Transform(this.MinX, this.MinY) : default(ScreenPoint);

                // Reuse the cached single-element size array across renders; only rebuild when
                // MarkerSize actually changes. Eliminates a per-render-per-series double[]
                // allocation that adds up across many-series plots.
                if (this.markerSizeArray == null || this.markerSizeArrayValue != this.MarkerSize)
                {
                    this.markerSizeArray = new[] { this.MarkerSize };
                    this.markerSizeArrayValue = this.MarkerSize;
                }

                rc.DrawMarkers(
                    pointsToRender,
                    this.MarkerType,
                    this.MarkerOutline,
                    this.markerSizeArray,
                    this.ActualMarkerFill,
                    this.MarkerStroke,
                    this.MarkerStrokeThickness,
                    this.EdgeRenderingMode,
                    this.MarkerResolution,
                    markerBinOffset);
            }
        }

        /// <summary>
        /// Renders a continuous line.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="pointsToRender">The points to render.</param>
        protected virtual void RenderLine(IRenderContext rc, IList<ScreenPoint> pointsToRender)
        {
            var dashArray = this.ActualDashArray;
            var color = this.GetSelectableColor(this.ActualColor);

            if (this.decimationActive)
            {
                // Decimation already reduced points to pixel-level spacing.
                // Skip the redundant ReducePoints pass and draw directly.
                rc.DrawLine(pointsToRender, color, this.StrokeThickness,
                    this.EdgeRenderingMode, dashArray, this.LineJoin);
            }
            else
            {
                if (this.outputBuffer == null)
                {
                    this.outputBuffer = new List<ScreenPoint>(pointsToRender.Count);
                }

                rc.DrawReducedLine(pointsToRender,
                    this.MinimumSegmentLength * this.MinimumSegmentLength,
                    color, this.StrokeThickness, this.EdgeRenderingMode,
                    dashArray, this.LineJoin, this.outputBuffer);
            }
        }

        /// <summary>
        /// Force the smoothed points to be re-evaluated.
        /// </summary>
        protected virtual void ResetSmoothedPoints()
        {
            double tolerance = Math.Abs(Math.Max(this.MaxX - this.MinX, this.MaxY - this.MinY) / ToleranceDivisor);
            this.smoothedPoints = this.InterpolationAlgorithm.CreateSpline(this.ActualPoints, false, tolerance);
        }

        /// <summary>
        /// Represents a line segment.
        /// </summary>
        protected class Segment
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Segment" /> class.
            /// </summary>
            /// <param name="point1">The first point of the segment.</param>
            /// <param name="point2">The second point of the segment.</param>
            public Segment(DataPoint point1, DataPoint point2)
            {
                this.Point1 = point1;
                this.Point2 = point2;
            }

            /// <summary>
            /// Gets the first point1 of the segment.
            /// </summary>
            public DataPoint Point1 { get; private set; }

            /// <summary>
            /// Gets the second point of the segment.
            /// </summary>
            public DataPoint Point2 { get; private set; }
        }
    }
}
