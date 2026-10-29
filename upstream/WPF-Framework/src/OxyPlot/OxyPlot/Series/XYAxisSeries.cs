// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XYAxisSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides an abstract base class for series that are related to an X-axis and a Y-axis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OxyPlot.Axes;

    /// <summary>
    /// Provides an abstract base class for series that are related to an X-axis and a Y-axis.
    /// </summary>
    public abstract class XYAxisSeries : ItemsSeries, ITransposablePlotElement
    {
        /// <summary>
        /// The default tracker format string
        /// </summary>
        public const string DefaultTrackerFormatString = "{0}\n{1}: {2}\n{3}: {4}";

        /// <summary>
        /// The default x-axis title
        /// </summary>
        protected const string DefaultXAxisTitle = "X";

        /// <summary>
        /// The default y-axis title
        /// </summary>
        protected const string DefaultYAxisTitle = "Y";

        /// <summary>
        /// Initializes a new instance of the <see cref="XYAxisSeries"/> class.
        /// </summary>
        protected XYAxisSeries()
        {
            this.TrackerFormatString = DefaultTrackerFormatString;
        }

        /// <summary>
        /// Gets or sets the maximum x-coordinate of the dataset.
        /// </summary>
        /// <value>The maximum x-coordinate.</value>
        public double MaxX { get; protected set; }

        /// <summary>
        /// Gets or sets the maximum y-coordinate of the dataset.
        /// </summary>
        /// <value>The maximum y-coordinate.</value>
        public double MaxY { get; protected set; }

        /// <summary>
        /// Gets or sets the minimum x-coordinate of the dataset.
        /// </summary>
        /// <value>The minimum x-coordinate.</value>
        public double MinX { get; protected set; }

        /// <summary>
        /// Gets or sets the minimum y-coordinate of the dataset.
        /// </summary>
        /// <value>The minimum y-coordinate.</value>
        public double MinY { get; protected set; }

        /// <summary>
        /// Gets the x-axis.
        /// </summary>
        /// <value>The x-axis.</value>
        public Axis XAxis { get; private set; }

        /// <summary>
        /// Gets or sets the x-axis key. The default is <c>null</c>.
        /// </summary>
        /// <value>The x-axis key.</value>
        public string XAxisKey { get; set; }

        /// <summary>
        /// Gets the y-axis.
        /// </summary>
        /// <value>The y-axis.</value>
        public Axis YAxis { get; private set; }

        /// <summary>
        /// Gets or sets the y-axis key. The default is <c>null</c>.
        /// </summary>
        /// <value>The y-axis key.</value>
        public string YAxisKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the X coordinate of all data point increases monotonically.
        /// </summary>
        protected bool IsXMonotonic { get; set; }

        /// <summary>
        /// Gets or sets the last visible window start position in the data points collection.
        /// </summary>
        protected int WindowStartIndex { get; set; }

        /// <summary>
        /// Gets or sets the last visible window end position in the data points collection
        /// (inclusive). For <see cref="IsXMonotonic"/> series, this is computed alongside
        /// <see cref="WindowStartIndex"/> by symmetric bisect on the data X axis. Series may
        /// use it as an explicit upper bound on render-time iteration, eliminating O(N)
        /// behaviour when only a small window is visible. Defaults to <see cref="int.MaxValue"/>
        /// (no upper bound) so series that don't compute it fall back to the existing
        /// per-point clip-count termination.
        /// </summary>
        protected int WindowEndIndex { get; set; } = int.MaxValue;

        /// <inheritdoc/>
        public override OxyRect GetClippingRect()
        {
            return PlotElementUtilities.GetClippingRect(this);
        }

        /// <summary>
        /// Gets the rectangle the series uses on the screen (screen coordinates).
        /// </summary>
        /// <returns>The rectangle.</returns>
        public OxyRect GetScreenRectangle()
        {
            return this.GetClippingRect();
        }

        /// <inheritdoc/>
        public DataPoint InverseTransform(ScreenPoint p)
        {
            return PlotElementUtilities.InverseTransformOrientated(this, p);
        }

        /// <summary>
        /// Renders the legend symbol on the specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        /// <param name="legendBox">The legend rectangle.</param>
        public override void RenderLegend(IRenderContext rc, OxyRect legendBox)
        {
        }

        /// <inheritdoc/>
        public ScreenPoint Transform(DataPoint p)
        {
            return PlotElementUtilities.TransformOrientated(this, p);
        }

        /// <summary>
        /// Check if this data series requires X/Y axes. (e.g. Pie series do not require axes)
        /// </summary>
        /// <returns>The are axes required.</returns>
        protected internal override bool AreAxesRequired()
        {
            return true;
        }

        /// <summary>
        /// Ensures that the axes of the series is defined.
        /// </summary>
        protected internal override void EnsureAxes()
        {
            this.XAxis = this.XAxisKey != null ?
                         this.PlotModel.GetAxis(this.XAxisKey) :
                         this.PlotModel.DefaultXAxis;

            this.YAxis = this.YAxisKey != null ?
                         this.PlotModel.GetAxis(this.YAxisKey) :
                         this.PlotModel.DefaultYAxis;
        }

        /// <summary>
        /// Check if the data series is using the specified axis.
        /// </summary>
        /// <param name="axis">An axis.</param>
        /// <returns>True if the axis is in use.</returns>
        protected internal override bool IsUsing(Axis axis)
        {
            return false;
        }

        /// <summary>
        /// Sets default values from the plot model.
        /// </summary>
        protected internal override void SetDefaultValues()
        {
        }

        /// <summary>
        /// Updates the axes to include the max and min of this series.
        /// </summary>
        protected internal override void UpdateAxisMaxMin()
        {
            this.XAxis.Include(this.MinX);
            this.XAxis.Include(this.MaxX);
            this.YAxis.Include(this.MinY);
            this.YAxis.Include(this.MaxY);
        }

        /// <summary>
        /// Updates the data.
        /// </summary>
        protected internal override void UpdateData()
        {
            this.WindowStartIndex = 0;
        }

        /// <summary>
        /// Updates the maximum and minimum values of the series.
        /// </summary>
        protected internal override void UpdateMaxMin()
        {
            this.MinX = this.MinY = this.MaxX = this.MaxY = double.NaN;
        }

        /// <summary>
        /// Gets the point on the curve that is nearest the specified point.
        /// </summary>
        /// <param name="points">The point list.</param>
        /// <param name="point">The point.</param>
        /// <returns>A tracker hit result if a point was found.</returns>
        /// <remarks>The Text property of the result will not be set, since the formatting depends on the various series.</remarks>
        protected TrackerHitResult GetNearestInterpolatedPointInternal(List<DataPoint> points, ScreenPoint point)
        {
            return this.GetNearestInterpolatedPointInternal(points, 0, point);
        }

        /// <summary>
        /// Gets the point on the curve that is nearest the specified point.
        /// </summary>
        /// <param name="points">The point list.</param>
        /// <param name="startIdx">The index to start from.</param>
        /// <param name="point">The point.</param>
        /// <returns>A tracker hit result if a point was found.</returns>
        /// <remarks>The Text property of the result will not be set, since the formatting depends on the various series.</remarks>
        protected TrackerHitResult GetNearestInterpolatedPointInternal(List<DataPoint> points, int startIdx, ScreenPoint point)
        {
            if (this.XAxis == null || this.YAxis == null || points == null)
            {
                return null;
            }

            var spn = default(ScreenPoint);
            var dpn = default(DataPoint);
            double index = -1;

            double minimumDistance = double.MaxValue;

            for (int i = startIdx; i + 1 < points.Count; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];
                if (!this.IsValidPoint(p1) || !this.IsValidPoint(p2))
                {
                    continue;
                }

                var sp1 = this.Transform(p1);
                var sp2 = this.Transform(p2);

                // Find the nearest point on the line segment.
                var spl = ScreenPointHelper.FindPointOnLine(point, sp1, sp2);

                if (ScreenPoint.IsUndefined(spl))
                {
                    // P1 && P2 coincident
                    continue;
                }

                double l2 = (point - spl).LengthSquared;

                if (l2 < minimumDistance)
                {
                    double segmentLength = (sp2 - sp1).Length;
                    double u = segmentLength > 0 ? (spl - sp1).Length / segmentLength : 0;
                    dpn = this.InverseTransform(spl);
                    spn = spl;
                    minimumDistance = l2;
                    index = i + u;
                }
            }

            if (minimumDistance < double.MaxValue)
            {
                // GetItem may dereference the underlying ItemsSource at the captured index;
                // if the source mutated between scan and this lookup (concurrent producer),
                // the index can be stale. Treat that as "no item" rather than propagating
                // the exception out of the hit-test path.
                object item;
                try
                {
                    item = this.GetItem((int)Math.Round(index));
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    item = null;
                }
                return new TrackerHitResult
                {
                    Series = this,
                    DataPoint = dpn,
                    Position = spn,
                    Item = item,
                    Index = index
                };
            }

            return null;
        }

        /// <summary>
        /// Gets the nearest point.
        /// </summary>
        /// <param name="points">The points (data coordinates).</param>
        /// <param name="point">The point (screen coordinates).</param>
        /// <returns>A <see cref="TrackerHitResult" /> if a point was found, <c>null</c> otherwise.</returns>
        /// <remarks>The Text property of the result will not be set, since the formatting depends on the various series.</remarks>
        protected TrackerHitResult GetNearestPointInternal(IEnumerable<DataPoint> points, ScreenPoint point)
        {
            return this.GetNearestPointInternal(points, 0, point);
        }

        /// <summary>
        /// Gets the nearest point.
        /// </summary>
        /// <param name="points">The points (data coordinates).</param>
        /// <param name="startIdx">The index to start from.</param>
        /// <param name="point">The point (screen coordinates).</param>
        /// <returns>A <see cref="TrackerHitResult" /> if a point was found, <c>null</c> otherwise.</returns>
        /// <remarks>The Text property of the result will not be set, since the formatting depends on the various series.</remarks>
        protected TrackerHitResult GetNearestPointInternal(IEnumerable<DataPoint> points, int startIdx, ScreenPoint point)
        {
            var spn = default(ScreenPoint);
            var dpn = default(DataPoint);
            double index = -1;

            double minimumDistance = double.MaxValue;

            // Fast path for IList<DataPoint> (the common case: series points are stored in a List).
            // Indexed access is both slightly faster than a LINQ iterator state machine and, more
            // importantly, does not throw InvalidOperationException if the underlying collection
            // is mutated on another thread during the scan.
            //
            // Concurrent mutation (still unsafe for List<T> in general, but documented as a rare
            // real-world scenario in OxyPlot upstream issue #1482) is handled with two layers of
            // defense: (1) re-read list.Count on every iteration so a shrink terminates the loop
            // cleanly, and (2) catch ArgumentOutOfRangeException from the indexer for the narrow
            // window between the bounds check and the element access. Both combined let the hit-
            // test survive a race without bubbling an exception up to the controller.
            //
            // Fallback for arbitrary IEnumerable<DataPoint>: snapshot to an array once at entry.
            // This avoids the thread-unsafe `foreach (var p in points.Skip(startIdx))` pattern that
            // builds an enumerator over a potentially mutable source.
            if (points is IList<DataPoint> list)
            {
                // Fast path for IsXMonotonic series: bisect on data X to find a candidate
                // index, then expand outward in both directions stopping when the screen-X
                // distance to the cursor exceeds the running best total distance — no point
                // further out can be closer in Euclidean distance. This converts the per-mouse-
                // move cost from O(N) to O(log N + k), where k is the small set of points
                // within the running best distance. The 16-element threshold avoids overhead
                // for tiny series where the linear scan is already free.
                int remaining = list.Count - startIdx;
                if (this.IsXMonotonic && remaining >= 16)
                {
                    DataPoint queryDp;
                    bool inverseOk;
                    try
                    {
                        queryDp = this.InverseTransform(point);
                        inverseOk = true;
                    }
                    catch
                    {
                        // InverseTransform unavailable (e.g. axis not initialized); fall through
                        // to the linear scan below by leaving inverseOk=false.
                        queryDp = default;
                        inverseOk = false;
                    }

                    if (inverseOk)
                    {
                        double targetX = queryDp.X;

                        // Bisect: smallest index in [startIdx, list.Count-1] whose X >= targetX,
                        // or list.Count-1 if all X < targetX. Defensive against concurrent
                        // mutation: re-read list.Count each iteration (already implicit) and
                        // catch out-of-range from the indexer.
                        int lo = startIdx;
                        int hi = list.Count - 1;
                        bool bisectOk = true;
                        while (lo < hi)
                        {
                            int mid = lo + ((hi - lo) >> 1);
                            DataPoint mp;
                            try
                            {
                                mp = list[mid];
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                bisectOk = false;
                                break;
                            }
                            if (mp.x < targetX) lo = mid + 1;
                            else hi = mid;
                        }

                        if (bisectOk)
                        {
                            int center = lo < list.Count ? lo : list.Count - 1;

                            // Expand outward; early-terminate each direction when screen-X
                            // distance squared exceeds the best total distance squared.
                            int li = center;
                            int ri = center + 1;
                            bool leftDone = li < startIdx;
                            bool rightDone = ri >= list.Count;

                            while (!leftDone || !rightDone)
                            {
                                if (!leftDone)
                                {
                                    DataPoint p;
                                    try
                                    {
                                        p = list[li];
                                    }
                                    catch (ArgumentOutOfRangeException)
                                    {
                                        leftDone = true;
                                        p = default;
                                    }
                                    if (!leftDone && this.IsValidPoint(p))
                                    {
                                        var sp = this.Transform(p.x, p.y);
                                        double dxL = sp.X - point.X;
                                        double dxLSq = dxL * dxL;
                                        if (minimumDistance < double.MaxValue && dxLSq > minimumDistance)
                                        {
                                            leftDone = true;
                                        }
                                        else
                                        {
                                            double d2 = (sp - point).LengthSquared;
                                            if (d2 < minimumDistance)
                                            {
                                                dpn = p;
                                                spn = sp;
                                                minimumDistance = d2;
                                                index = li;
                                            }
                                        }
                                    }
                                    if (!leftDone)
                                    {
                                        li--;
                                        if (li < startIdx) leftDone = true;
                                    }
                                }

                                if (!rightDone)
                                {
                                    DataPoint p;
                                    try
                                    {
                                        p = list[ri];
                                    }
                                    catch (ArgumentOutOfRangeException)
                                    {
                                        rightDone = true;
                                        p = default;
                                    }
                                    if (!rightDone && this.IsValidPoint(p))
                                    {
                                        var sp = this.Transform(p.x, p.y);
                                        double dxR = sp.X - point.X;
                                        double dxRSq = dxR * dxR;
                                        if (minimumDistance < double.MaxValue && dxRSq > minimumDistance)
                                        {
                                            rightDone = true;
                                        }
                                        else
                                        {
                                            double d2 = (sp - point).LengthSquared;
                                            if (d2 < minimumDistance)
                                            {
                                                dpn = p;
                                                spn = sp;
                                                minimumDistance = d2;
                                                index = ri;
                                            }
                                        }
                                    }
                                    if (!rightDone)
                                    {
                                        ri++;
                                        if (ri >= list.Count) rightDone = true;
                                    }
                                }
                            }

                            goto AfterIListScan;
                        }
                    }
                }

                // Linear scan fallback (small series, non-monotonic, or fast path declined).
                for (int i = startIdx; i < list.Count; i++)
                {
                    DataPoint p;
                    try
                    {
                        p = list[i];
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // Concurrent mutation shrank the list under us. Stop cleanly.
                        break;
                    }

                    if (!this.IsValidPoint(p))
                    {
                        continue;
                    }

                    var sp = this.Transform(p.x, p.y);
                    double d2 = (sp - point).LengthSquared;

                    if (d2 < minimumDistance)
                    {
                        dpn = p;
                        spn = sp;
                        minimumDistance = d2;
                        index = i;
                    }
                }
                AfterIListScan:;
            }
            else if (points is IReadOnlyList<DataPoint> roList)
            {
                // IReadOnlyList<DataPoint> fast path: the same indexed scan as IList<DataPoint>,
                // but covers consumers that supply read-only collections (immutable arrays,
                // IReadOnlyList views, ImmutableList, etc.) without falling through to the
                // allocating snapshot fallback below. Re-reads roList.Count each iteration so
                // a shrink terminates the loop cleanly.
                for (int i = startIdx; i < roList.Count; i++)
                {
                    DataPoint p;
                    try
                    {
                        p = roList[i];
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }

                    if (!this.IsValidPoint(p))
                    {
                        continue;
                    }

                    var sp = this.Transform(p.x, p.y);
                    double d2 = (sp - point).LengthSquared;

                    if (d2 < minimumDistance)
                    {
                        dpn = p;
                        spn = sp;
                        minimumDistance = d2;
                        index = i;
                    }
                }
            }
            else
            {
                var snapshot = points.Skip(startIdx).ToArray();
                for (int k = 0; k < snapshot.Length; k++)
                {
                    var p = snapshot[k];
                    if (!this.IsValidPoint(p))
                    {
                        continue;
                    }

                    var sp = this.Transform(p.x, p.y);
                    double d2 = (sp - point).LengthSquared;

                    if (d2 < minimumDistance)
                    {
                        dpn = p;
                        spn = sp;
                        minimumDistance = d2;
                        index = startIdx + k;
                    }
                }
            }

            if (minimumDistance < double.MaxValue)
            {
                // GetItem may dereference the underlying ItemsSource at the captured index;
                // if the source mutated between scan and this lookup (concurrent producer),
                // the index can be stale. Treat that as "no item" rather than propagating
                // the exception out of the hit-test path.
                object item;
                try
                {
                    item = this.GetItem((int)Math.Round(index));
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    item = null;
                }
                return new TrackerHitResult
                {
                    Series = this,
                    DataPoint = dpn,
                    Position = spn,
                    Item = item,
                    Index = index
                };
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified point is valid.
        /// </summary>
        /// <param name="pt">The point.</param>
        /// <returns><c>true</c> if the point is valid; otherwise, <c>false</c> .</returns>
        protected virtual bool IsValidPoint(DataPoint pt)
        {
            return
                this.XAxis != null && this.XAxis.IsValidValue(pt.X) &&
                this.YAxis != null && this.YAxis.IsValidValue(pt.Y);
        }

        /// <summary>
        /// Determines whether the specified point is valid.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns><c>true</c> if the point is valid; otherwise, <c>false</c> . </returns>
        protected bool IsValidPoint(double x, double y)
        {
            return
                this.XAxis != null && this.XAxis.IsValidValue(x) &&
                this.YAxis != null && this.YAxis.IsValidValue(y);
        }

        /// <summary>
        /// Updates the Max/Min limits from the specified <see cref="DataPoint" /> list.
        /// </summary>
        /// <param name="points">The list of points.</param>
        protected void InternalUpdateMaxMin(List<DataPoint> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }

            this.IsXMonotonic = true;

            if (points.Count == 0)
            {
                return;
            }

            double minx = this.MinX;
            double miny = this.MinY;
            double maxx = this.MaxX;
            double maxy = this.MaxY;

            if (double.IsNaN(minx))
            {
                minx = double.MaxValue;
            }

            if (double.IsNaN(miny))
            {
                miny = double.MaxValue;
            }

            if (double.IsNaN(maxx))
            {
                maxx = double.MinValue;
            }

            if (double.IsNaN(maxy))
            {
                maxy = double.MinValue;
            }

            double lastX = double.MinValue;
            foreach (var pt in points)
            {
                double x = pt.X;
                double y = pt.Y;

                // Check if the point is valid
                if (!this.IsValidPoint(pt))
                {
                    continue;
                }

                if (x < lastX)
                {
                    this.IsXMonotonic = false;
                }

                if (x < minx)
                {
                    minx = x;
                }

                if (x > maxx)
                {
                    maxx = x;
                }

                if (y < miny)
                {
                    miny = y;
                }

                if (y > maxy)
                {
                    maxy = y;
                }

                lastX = x;
            }

            if (minx < double.MaxValue)
            {
                if (minx < this.XAxis.FilterMinValue)
                {
                    minx = this.XAxis.FilterMinValue;
                }

                this.MinX = minx;
            }

            if (miny < double.MaxValue)
            {
                if (miny < this.YAxis.FilterMinValue)
                {
                    miny = this.YAxis.FilterMinValue;
                }

                this.MinY = miny;
            }

            if (maxx > double.MinValue)
            {
                if (maxx > this.XAxis.FilterMaxValue)
                {
                    maxx = this.XAxis.FilterMaxValue;
                }

                this.MaxX = maxx;
            }

            if (maxy > double.MinValue)
            {
                if (maxy > this.YAxis.FilterMaxValue)
                {
                    maxy = this.YAxis.FilterMaxValue;
                }

                this.MaxY = maxy;
            }
        }

        /// <summary>
        /// Updates the Max/Min limits from the specified list.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <param name="items">The items.</param>
        /// <param name="xf">A function that provides the x value for each item.</param>
        /// <param name="yf">A function that provides the y value for each item.</param>
        /// <exception cref="System.ArgumentNullException">The items argument cannot be null.</exception>
        protected void InternalUpdateMaxMin<T>(List<T> items, Func<T, double> xf, Func<T, double> yf)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            this.IsXMonotonic = true;

            if (items.Count == 0)
            {
                return;
            }

            double minx = this.MinX;
            double miny = this.MinY;
            double maxx = this.MaxX;
            double maxy = this.MaxY;

            if (double.IsNaN(minx))
            {
                minx = double.MaxValue;
            }

            if (double.IsNaN(miny))
            {
                miny = double.MaxValue;
            }

            if (double.IsNaN(maxx))
            {
                maxx = double.MinValue;
            }

            if (double.IsNaN(maxy))
            {
                maxy = double.MinValue;
            }

            double lastX = double.MinValue;
            foreach (var item in items)
            {
                double x = xf(item);
                double y = yf(item);

                // Check if the point is valid
                if (!this.IsValidPoint(x, y))
                {
                    continue;
                }

                if (x < lastX)
                {
                    this.IsXMonotonic = false;
                }

                if (x < minx)
                {
                    minx = x;
                }

                if (x > maxx)
                {
                    maxx = x;
                }

                if (y < miny)
                {
                    miny = y;
                }

                if (y > maxy)
                {
                    maxy = y;
                }

                lastX = x;
            }

            if (minx < double.MaxValue)
            {
                this.MinX = minx;
            }

            if (miny < double.MaxValue)
            {
                this.MinY = miny;
            }

            if (maxx > double.MinValue)
            {
                this.MaxX = maxx;
            }

            if (maxy > double.MinValue)
            {
                this.MaxY = maxy;
            }
        }

        /// <summary>
        /// Updates the Max/Min limits from the specified collection.
        /// </summary>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <param name="items">The items.</param>
        /// <param name="xmin">A function that provides the x minimum for each item.</param>
        /// <param name="xmax">A function that provides the x maximum for each item.</param>
        /// <param name="ymin">A function that provides the y minimum for each item.</param>
        /// <param name="ymax">A function that provides the y maximum for each item.</param>
        /// <exception cref="System.ArgumentNullException">The items argument cannot be null.</exception>
        protected void InternalUpdateMaxMin<T>(List<T> items, Func<T, double> xmin, Func<T, double> xmax, Func<T, double> ymin, Func<T, double> ymax)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            this.IsXMonotonic = true;

            if (items.Count == 0)
            {
                return;
            }

            double minx = this.MinX;
            double miny = this.MinY;
            double maxx = this.MaxX;
            double maxy = this.MaxY;

            if (double.IsNaN(minx))
            {
                minx = double.MaxValue;
            }

            if (double.IsNaN(miny))
            {
                miny = double.MaxValue;
            }

            if (double.IsNaN(maxx))
            {
                maxx = double.MinValue;
            }

            if (double.IsNaN(maxy))
            {
                maxy = double.MinValue;
            }

            double lastX0 = double.MinValue;
            double lastX1 = double.MinValue;
            foreach (var item in items)
            {
                double x0 = xmin(item);
                double x1 = xmax(item);
                double y0 = ymin(item);
                double y1 = ymax(item);

                if (!this.IsValidPoint(x0, y0) || !this.IsValidPoint(x1, y1))
                {
                    continue;
                }

                if (x0 < lastX0 || x1 < lastX1)
                {
                    this.IsXMonotonic = false;
                }

                if (x0 < minx)
                {
                    minx = x0;
                }

                if (x1 > maxx)
                {
                    maxx = x1;
                }

                if (y0 < miny)
                {
                    miny = y0;
                }

                if (y1 > maxy)
                {
                    maxy = y1;
                }

                lastX0 = x0;
                lastX1 = x1;
            }

            if (minx < double.MaxValue)
            {
                this.MinX = minx;
            }

            if (miny < double.MaxValue)
            {
                this.MinY = miny;
            }

            if (maxx > double.MinValue)
            {
                this.MaxX = maxx;
            }

            if (maxy > double.MinValue)
            {
                this.MaxY = maxy;
            }
        }

        /// <summary>
        /// Verifies that both axes are defined.
        /// </summary>
        protected void VerifyAxes()
        {
            if (this.XAxis == null)
            {
                throw new InvalidOperationException("XAxis not defined.");
            }

            if (this.YAxis == null)
            {
                throw new InvalidOperationException("YAxis not defined.");
            }
        }

        /// <summary>
        /// Updates visible window start index.
        /// </summary>
        /// <typeparam name="T">The type of the list items.</typeparam>
        /// <param name="items">Data points.</param>
        /// <param name="xgetter">Function that gets data point X coordinate.</param>
        /// <param name="targetX">X coordinate of visible window start.</param>
        /// <param name="lastIndex">Last window index.</param>
        /// <returns>The new window start index.</returns>
        protected int UpdateWindowStartIndex<T>(IList<T> items, Func<T, double> xgetter, double targetX, int lastIndex)
        {
            lastIndex = this.FindWindowStartIndex(items, xgetter, targetX, lastIndex);
            if (lastIndex > 0)
            {
                lastIndex--;
            }

            return lastIndex;
        }

        /// <summary>
        /// Finds the index of max(x) &lt;= target x in a list of data points
        /// </summary>
        /// <typeparam name="T">The type of the list items.</typeparam>
        /// <param name="items">vector of data points</param>
        /// <param name="xgetter">Function that gets data point X coordinate.</param>
        /// <param name="targetX">target x.</param>
        /// <param name="initialGuess">initial guess index.</param>
        /// <returns>
        /// index of x with max(x) &lt;= target x or 0 if cannot find
        /// </returns>
        public int FindWindowStartIndex<T>(IList<T> items, Func<T, double> xgetter, double targetX, int initialGuess)
        {
            int start = 0;
            int nominalEnd = items.Count - 1;
            while (nominalEnd > 0 && double.IsNaN(xgetter(items[nominalEnd])))
                nominalEnd -= 1;
            int end = nominalEnd;
            int curGuess = Math.Max(0, Math.Min(end, initialGuess));

            double GetX(int index)
            {
                while (index <= nominalEnd)
                {
                    double guessX = xgetter(items[index]);
                    if (double.IsNaN(guessX))
                        index += 1;
                    else
                        return guessX;
                }
                return xgetter(items[nominalEnd]);
            }

            while (start < end)
            {
                double guessX = GetX(curGuess);
                if (guessX.Equals(targetX))
                {
                    start = curGuess;
                    break;
                }
                else if (guessX > targetX)
                {
                    end = curGuess - 1;
                }
                else
                { 
                    start = curGuess;
                }

                if (start >= end)
                {
                    break;
                }

                double endX = GetX(end);
                double startX = GetX(start);

                if (endX == startX)
                {
                    break;
                }

                var m = (end - start + 1) / (endX - startX);

                curGuess = start + (int)((targetX - startX) * m);
                curGuess = Math.Max(start + 1, Math.Min(curGuess, end));
            }

            // Post-search adjustment: the binary search may be imprecise because
            // GetX() skips NaN values, creating index/value mismatches. Do a linear
            // fixup that properly handles NaN values in both directions.

            // Walk backward past NaN and values > targetX
            while (start > 0)
            {
                double val = xgetter(items[start]);
                if (double.IsNaN(val) || val > targetX)
                    start -= 1;
                else
                    break;
            }

            // Walk forward past NaN to find the tightest position
            // (binary search may have stopped too early due to NaN-skipping)
            for (int i = start + 1; i < items.Count; i++)
            {
                double val = xgetter(items[i]);
                if (double.IsNaN(val))
                    continue;
                if (val <= targetX)
                    start = i;
                else
                    break;
            }

            return start;
        }

        /// <summary>
        /// Specialised <see cref="UpdateWindowStartIndex{T}"/> overload for
        /// <see cref="IList{T}"/> of <see cref="DataPoint"/>. Avoids the
        /// <see cref="Func{T, TResult}"/> delegate dispatch on every binary-search step,
        /// which on a 500k-point series is called dozens of times per render.
        /// </summary>
        /// <param name="items">Data points.</param>
        /// <param name="targetX">X coordinate of visible window start.</param>
        /// <param name="lastIndex">Last window index.</param>
        /// <returns>The new window start index.</returns>
        protected int UpdateWindowStartIndex(IList<DataPoint> items, double targetX, int lastIndex)
        {
            lastIndex = this.FindWindowStartIndex(items, targetX, lastIndex);
            if (lastIndex > 0)
            {
                lastIndex--;
            }

            return lastIndex;
        }

        /// <summary>
        /// Specialised <see cref="FindWindowStartIndex{T}"/> overload for
        /// <see cref="IList{T}"/> of <see cref="DataPoint"/>. Reads <c>DataPoint.x</c>
        /// directly instead of going through a <see cref="Func{T, TResult}"/>; the JIT
        /// can then inline the field access on every binary-search step.
        /// </summary>
        /// <param name="items">vector of data points</param>
        /// <param name="targetX">target x.</param>
        /// <param name="initialGuess">initial guess index.</param>
        /// <returns>
        /// index of x with max(x) &lt;= target x or 0 if cannot find
        /// </returns>
        public int FindWindowStartIndex(IList<DataPoint> items, double targetX, int initialGuess)
        {
            int start = 0;
            int nominalEnd = items.Count - 1;
            while (nominalEnd > 0 && double.IsNaN(items[nominalEnd].x))
                nominalEnd -= 1;
            int end = nominalEnd;
            int curGuess = Math.Max(0, Math.Min(end, initialGuess));

            double GetX(int index)
            {
                while (index <= nominalEnd)
                {
                    double guessX = items[index].x;
                    if (double.IsNaN(guessX))
                        index += 1;
                    else
                        return guessX;
                }
                return items[nominalEnd].x;
            }

            while (start < end)
            {
                double guessX = GetX(curGuess);
                if (guessX.Equals(targetX))
                {
                    start = curGuess;
                    break;
                }
                else if (guessX > targetX)
                {
                    end = curGuess - 1;
                }
                else
                {
                    start = curGuess;
                }

                if (start >= end)
                {
                    break;
                }

                double endX = GetX(end);
                double startX = GetX(start);

                if (endX == startX)
                {
                    break;
                }

                var m = (end - start + 1) / (endX - startX);

                curGuess = start + (int)((targetX - startX) * m);
                curGuess = Math.Max(start + 1, Math.Min(curGuess, end));
            }

            // Post-search adjustment (mirrors the generic overload).
            while (start > 0)
            {
                double val = items[start].x;
                if (double.IsNaN(val) || val > targetX)
                    start -= 1;
                else
                    break;
            }

            for (int i = start + 1; i < items.Count; i++)
            {
                double val = items[i].x;
                if (double.IsNaN(val))
                    continue;
                if (val <= targetX)
                    start = i;
                else
                    break;
            }

            return start;
        }

        /// <summary>
        /// Updates the visible window end index for an <see cref="IList{T}"/> of
        /// <see cref="DataPoint"/>. Symmetric to
        /// <see cref="UpdateWindowStartIndex(IList{DataPoint}, double, int)"/>: returns the
        /// largest index whose X is &lt;= <paramref name="targetX"/>, plus one for line
        /// continuity past the right edge. Returns <see cref="int.MaxValue"/> if the search
        /// can't bound the window (e.g. all values past the target are NaN).
        /// </summary>
        /// <param name="items">Data points (assumed monotonic in X).</param>
        /// <param name="targetX">X coordinate of visible window end.</param>
        /// <param name="lastIndex">Last computed window end index (used as initial guess).</param>
        /// <returns>The new window end index (inclusive).</returns>
        protected int UpdateWindowEndIndex(IList<DataPoint> items, double targetX, int lastIndex)
        {
            int idx = this.FindWindowEndIndex(items, targetX, lastIndex);
            if (idx < items.Count - 1)
            {
                idx++;
            }
            return idx;
        }

        /// <summary>
        /// Finds the largest index in a monotonic-X data-point list whose X &lt;= targetX.
        /// Mirrors <see cref="FindWindowStartIndex(IList{DataPoint}, double, int)"/> but
        /// finds the right boundary of the visible window. Used to bound render-time
        /// iteration so the slow fallback path doesn't scan past the visible edge.
        /// </summary>
        public int FindWindowEndIndex(IList<DataPoint> items, double targetX, int initialGuess)
        {
            if (items.Count == 0) return -1;

            int last = items.Count - 1;
            // Step back over trailing NaN.
            while (last > 0 && double.IsNaN(items[last].x))
                last -= 1;

            // If even the last valid X is <= target, return last directly.
            if (items[last].x <= targetX) return last;

            // Bisect: smallest index where X > targetX, then return idx-1.
            int lo = 0, hi = last;
            int initial = Math.Max(0, Math.Min(last, initialGuess));
            // Use the initial guess as a hint to skip ahead in the common case where
            // the window has only shifted slightly between renders.
            if (items[initial].x > targetX)
            {
                hi = initial;
            }
            else
            {
                lo = initial;
            }

            while (lo < hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                double midX = items[mid].x;
                if (double.IsNaN(midX))
                {
                    // Step over NaN by treating it as "<= targetX" so we keep moving right.
                    lo = mid + 1;
                    continue;
                }
                if (midX <= targetX) lo = mid + 1;
                else hi = mid;
            }

            // lo is the smallest index with X > targetX (or last if not found). Return lo-1
            // unless lo==0 (no points in window).
            return lo > 0 ? lo - 1 : 0;
        }
    }
}
