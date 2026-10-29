// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DrawingVisualRenderContext.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides an <see cref="IRenderContext"/> implementation that renders to a <see cref="DrawingVisual"/>
//   using <see cref="DrawingContext"/>, eliminating per-element WPF layout overhead.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using FontWeights = OxyPlot.FontWeights;
    using HorizontalAlignment = OxyPlot.HorizontalAlignment;
    using VerticalAlignment = OxyPlot.VerticalAlignment;

    /// <summary>
    /// Provides an <see cref="IRenderContext"/> implementation that renders to a <see cref="DrawingVisual"/>
    /// using <see cref="DrawingContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This renderer draws all plot elements (lines, polygons, text, images) via a single
    /// <see cref="DrawingVisual"/>, avoiding the per-element WPF Measure/Arrange/Render overhead
    /// that <see cref="CanvasRenderContext"/> incurs from creating and destroying
    /// Path/TextBlock/Image elements every frame.
    /// </para>
    /// <para>
    /// Text positions are recorded during rendering to support toolbar hit-testing via
    /// <see cref="HitTestText"/>. This provides backend-agnostic text click detection
    /// equivalent to Canvas.InputHitTest for the <see cref="CanvasRenderContext"/>.
    /// </para>
    /// </remarks>
    public class DrawingVisualRenderContext : ClippingRenderContext
    {
        /// <summary>
        /// The brush cache, keyed by OxyColor.
        /// </summary>
        private readonly Dictionary<OxyColor, Brush> brushCache = new Dictionary<OxyColor, Brush>();

        /// <summary>
        /// The font family cache, keyed by family name.
        /// </summary>
        private readonly Dictionary<string, FontFamily> fontFamilyCache = new Dictionary<string, FontFamily>();

        /// <summary>
        /// The pen cache, keyed by composite pen parameters.
        /// Avoids repeated Pen allocation and Freeze() calls for identical stroke settings.
        /// </summary>
        private readonly Dictionary<long, Pen> penCache = new Dictionary<long, Pen>();

        /// <summary>
        /// The image cache, keyed by OxyImage.
        /// </summary>
        private readonly Dictionary<OxyImage, BitmapSource> imageCache = new Dictionary<OxyImage, BitmapSource>();

        /// <summary>
        /// The set of images used in the current rendering cycle.
        /// </summary>
        private readonly HashSet<OxyImage> imagesInUse = new HashSet<OxyImage>();

        /// <summary>
        /// The list of rendered text elements for hit-testing.
        /// </summary>
        private readonly List<TextHitResult> renderedText = new List<TextHitResult>();

        /// <summary>
        /// The typeface cache, keyed by (fontFamily, isBold).
        /// Avoids allocating a new Typeface per DrawText/MeasureText call.
        /// </summary>
        private readonly Dictionary<(string fontFamily, bool isBold), Typeface> typefaceCache = new Dictionary<(string, bool), Typeface>();

        /// <summary>
        /// The text measurement cache, keyed by (text, fontFamily, fontSize, isBold).
        /// Avoids redundant FormattedText allocations during margin-adjustment iterations
        /// and across frames when axis tick labels haven't changed.
        /// </summary>
        /// <summary>
        /// Maximum number of entries retained in <see cref="measureCache"/> and
        /// <see cref="drawTextCache"/> before LRU eviction removes the least-recently-used entry.
        /// On a real-time / streaming plot the unique-label count grows monotonically with time
        /// (each frame produces fresh numeric labels). An unbounded dictionary would leak —
        /// 512 entries comfortably accommodates a frame's worth of axis ticks plus repeating
        /// legend / annotation text, while capping retained <see cref="FormattedText"/> objects
        /// at a fixed memory ceiling.
        /// </summary>
        private const int TextCacheCapacity = 512;

        private readonly LruCache<(string text, string fontFamily, double fontSize, bool isBold, TextFormattingMode mode, string cultureName), OxySize> measureCache =
            new LruCache<(string, string, double, bool, TextFormattingMode, string), OxySize>(TextCacheCapacity);

        /// <summary>
        /// The DrawText FormattedText cache, keyed by (text, fontFamily, fontSize, isBold, color, mode, cultureName).
        /// Axis tick labels, axis titles, and legend entries typically repeat across frames;
        /// caching the <see cref="FormattedText"/> eliminates ~100 allocations per render on
        /// a typical plot and measurably reduces GC pressure on long-running dashboards.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: Do NOT mutate cached <see cref="FormattedText"/> instances after
        /// construction (no <c>SetForegroundBrush</c>, <c>SetFontWeight</c>, <c>TextDecorations</c>,
        /// <c>SetMaxTextWidth</c>, etc.). <see cref="DrawText"/> only reads <c>Width</c>/<c>Height</c>
        /// and passes the instance to <see cref="DrawingContext.DrawText"/> — safe reuse.
        /// The key includes <see cref="TextFormattingMode"/> and the current UI culture name so
        /// mode switches and culture changes (e.g. user toggling RTL locale) produce a fresh
        /// <see cref="FormattedText"/> instead of returning a stale layout. Cache is bounded to
        /// <see cref="TextCacheCapacity"/> entries via LRU eviction and cleared when
        /// <see cref="DpiScale"/> or <see cref="TextFormattingMode"/> changes.
        /// </remarks>
        private readonly LruCache<(string text, string fontFamily, double fontSize, bool isBold, OxyColor color, TextFormattingMode mode, string cultureName), FormattedText> drawTextCache =
            new LruCache<(string, string, double, bool, OxyColor, TextFormattingMode, string), FormattedText>(TextCacheCapacity);

        /// <summary>
        /// The active drawing context, or null if not currently rendering.
        /// </summary>
        private DrawingContext dc;

        /// <summary>
        /// Whether a clipping region is currently pushed on the drawing context.
        /// </summary>
        private bool clipPushed;


        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingVisualRenderContext"/> class.
        /// </summary>
        public DrawingVisualRenderContext()
        {
            this.RendersToScreen = true;
        }

        /// <summary>
        /// Gets or sets the DPI scale factor.
        /// </summary>
        private double dpiScale = 1;

        /// <summary>
        /// Gets or sets the DPI scale factor.
        /// Changing this value clears the text measurement cache.
        /// </summary>
        public double DpiScale
        {
            get => this.dpiScale;
            set
            {
                if (this.dpiScale != value)
                {
                    this.dpiScale = value;
                    this.measureCache.Clear();
                    this.drawTextCache.Clear();
                }
            }
        }

        private TextFormattingMode textFormattingMode = TextFormattingMode.Display;

        /// <summary>
        /// Gets or sets the text formatting mode.
        /// Changing this value clears both text caches so cached <see cref="FormattedText"/>
        /// and measurement results that were built under the old mode aren't returned.
        /// </summary>
        /// <value>The text formatting mode. The default value is <see cref="System.Windows.Media.TextFormattingMode.Display"/>.</value>
        public TextFormattingMode TextFormattingMode
        {
            get => this.textFormattingMode;
            set
            {
                if (this.textFormattingMode != value)
                {
                    this.textFormattingMode = value;
                    this.measureCache.Clear();
                    this.drawTextCache.Clear();
                }
            }
        }

        /// <summary>
        /// Gets or sets the visual offset for pixel snapping calculations.
        /// </summary>
        public Point VisualOffset { get; set; }

        /// <summary>
        /// Opens a <see cref="DrawingContext"/> on the specified <see cref="DrawingVisual"/> for rendering.
        /// Must be paired with a call to <see cref="CloseDrawing"/>.
        /// </summary>
        /// <param name="visual">The drawing visual to render into.</param>
        public void OpenDrawing(DrawingVisual visual)
        {
            this.dc = visual.RenderOpen();
            this.clipPushed = false;
            this.renderedText.Clear();
        }

        /// <summary>
        /// Closes the current <see cref="DrawingContext"/>, finalizing the rendered content.
        /// </summary>
        public void CloseDrawing()
        {
            if (this.dc != null)
            {
                this.dc.Close();
                this.dc = null;
            }
        }

        /// <summary>
        /// Sets the clipping area to the specified rectangle.
        /// Called by <see cref="ClippingRenderContext"/> when the active clip region changes.
        /// </summary>
        /// <summary>
        /// Cached frozen <see cref="RectangleGeometry"/> reused across <see cref="SetClip"/>
        /// calls. The plot-area clipping rect is the same value across most calls within a
        /// render (each series clips to the same plot area) and the same across most renders
        /// (only changes on resize).
        /// </summary>
        private RectangleGeometry cachedClipGeometry;

        /// <summary>
        /// The <see cref="OxyRect"/> reflected in <see cref="cachedClipGeometry"/>. Compared on
        /// each <see cref="SetClip"/> call to detect when the cache must rebuild.
        /// </summary>
        private OxyRect cachedClipRect;

        /// <param name="clippingRect">The clipping rectangle.</param>
        protected override void SetClip(OxyRect clippingRect)
        {
            if (this.dc == null)
            {
                return;
            }

            // Reuse the cached frozen geometry when the clip rect hasn't changed since the
            // last call. On a 20-series plot, SetClip is called once per series with the same
            // plot-area rect — without this, 20 RectangleGeometry allocations per render.
            if (this.cachedClipGeometry == null || !this.cachedClipRect.Equals(clippingRect))
            {
                this.cachedClipGeometry = new RectangleGeometry(ToRect(clippingRect));
                this.cachedClipGeometry.Freeze();
                this.cachedClipRect = clippingRect;
            }

            this.dc.PushClip(this.cachedClipGeometry);
            this.clipPushed = true;
        }

        /// <summary>
        /// Resets the clipping area.
        /// Called by <see cref="ClippingRenderContext"/> when the active clip region is removed.
        /// </summary>
        protected override void ResetClip()
        {
            if (this.dc == null || !this.clipPushed)
            {
                return;
            }

            this.dc.Pop();
            this.clipPushed = false;
        }

        /// <summary>
        /// Maximum number of vertices per StreamGeometry tile.
        /// </summary>
        /// <remarks>
        /// Earlier versions split at 1024 to "keep stroke tessellation within L2 cache." In
        /// practice, 20-LineSeries plots with a fused decimator output ~1000–4000 points each
        /// and the per-tile <c>StreamGeometry</c> + <c>DrawGeometry</c> overhead was the dominant
        /// per-series cost. With <see cref="StreamGeometryContext.PolyLineTo"/> (bulk
        /// native-side append) replacing the <c>LineTo</c> loop, large geometries are cheap and
        /// further splitting hurts more than it helps. The tile size is now large enough that a
        /// typical fused-decimated chain renders as a single geometry. Pathological inputs
        /// (>16k screen-space points per series) still fall back to tiling for safety.
        /// </remarks>
        private const int StreamGeometryTileSize = 16384;

        /// <summary>
        /// Reusable buffer for <see cref="StreamGeometryContext.PolyLineTo"/> calls. Avoids
        /// allocating a fresh <see cref="Point"/>[] per series per render. Grown as needed and
        /// reused across all <c>DrawLine</c>/<c>DrawLineSegments</c> calls within and across
        /// frames. Per-render-context (one context per plot) so no concurrency concerns.
        /// </summary>
        private Point[] polyPointBuffer = new Point[2048];

        /// <summary>
        /// Ensures <see cref="polyPointBuffer"/> has at least <paramref name="capacity"/> slots.
        /// </summary>
        private void EnsurePolyBuffer(int capacity)
        {
            if (this.polyPointBuffer.Length < capacity)
            {
                int newSize = this.polyPointBuffer.Length;
                while (newSize < capacity)
                {
                    newSize *= 2;
                }

                this.polyPointBuffer = new Point[newSize];
            }
        }

        /// <inheritdoc/>
        public override void DrawLine(
            IList<ScreenPoint> points,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode,
            double[] dashArray,
            LineJoin lineJoin)
        {
            if (this.dc == null || points.Count < 2)
            {
                return;
            }

            var pen = this.CreatePen(stroke, thickness, edgeRenderingMode, lineJoin, dashArray);
            if (pen == null)
            {
                return;
            }

            var actualThickness = this.GetActualStrokeThickness(thickness, edgeRenderingMode);
            bool snap = this.ShouldSnapPoints(edgeRenderingMode, points);

            int n = points.Count;
            if (n > StreamGeometryTileSize)
            {
                // Pathological case: extremely long polylines fall back to tiled rendering for
                // safety. With StreamGeometryTileSize = 16384, only inputs above that size hit
                // this path. Most fused-decimated series never do.
                int start = 0;
                while (start < n - 1)
                {
                    int end = Math.Min(start + StreamGeometryTileSize, n);
                    this.DrawLineRange(points, start, end, pen, actualThickness, snap);
                    start = end - 1; // one-point overlap for visual continuity
                }
            }
            else
            {
                this.DrawLineRange(points, 0, n, pen, actualThickness, snap);
            }
        }

        /// <summary>
        /// Draws a line segment range as a single StreamGeometry.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="StreamGeometryContext.PolyLineTo(IList{Point}, bool, bool)"/> for the
        /// bulk of the points instead of a per-point <see cref="StreamGeometryContext.LineTo"/>
        /// loop. <c>PolyLineTo</c> hands the entire vertex array to the native MIL layer in one
        /// call, eliminating the managed-call overhead that dominates for large polylines.
        /// Reuses <see cref="polyPointBuffer"/> across calls to avoid per-render allocations.
        /// </remarks>
        private void DrawLineRange(IList<ScreenPoint> points, int from, int to, Pen pen, double actualThickness, bool snap)
        {
            int count = to - from;
            if (count < 2)
            {
                return;
            }

            this.EnsurePolyBuffer(count);
            var buf = this.polyPointBuffer;
            for (int i = 0; i < count; i++)
            {
                buf[i] = this.ToPoint(points[from + i], actualThickness, snap);
            }

            var sg = new StreamGeometry();
            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(buf[0], false, false);

                // PolyLineTo accepts an IList<Point>; pass an ArraySegment-equivalent via
                // a slice-list wrapper to avoid allocating a trimmed array. Since WPF's
                // implementation copies internally, slicing isn't required for correctness,
                // but a wrapper keeps the call zero-alloc.
                sgc.PolyLineTo(new ArraySegmentList<Point>(buf, 1, count - 1), true, false);
            }

            sg.Freeze();
            this.dc.DrawGeometry(null, pen, sg);
        }

        /// <inheritdoc/>
        public override void DrawLineSegments(
            IList<ScreenPoint> points,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode,
            double[] dashArray,
            LineJoin lineJoin)
        {
            if (this.dc == null || points.Count < 2)
            {
                return;
            }

            var pen = this.CreatePen(stroke, thickness, edgeRenderingMode, lineJoin, dashArray);
            if (pen == null)
            {
                return;
            }

            var actualThickness = this.GetActualStrokeThickness(thickness, edgeRenderingMode);
            bool snap = this.ShouldSnapPoints(edgeRenderingMode, points);

            // DrawLineSegments uses alternating stroked/unstroked figures (each segment is a
            // separate move-line pair), so PolyLineTo can't batch the whole list. The LineTo
            // loop here is unavoidable, but the geometry is still built in one StreamGeometry.
            var sg = new StreamGeometry();
            using (var sgc = sg.Open())
            {
                var p0 = this.ToPoint(points[0], actualThickness, snap);
                sgc.BeginFigure(p0, false, false);
                for (int i = 1; i < points.Count; i++)
                {
                    // Alternate stroked/unstroked: odd indices (segment endpoints) are stroked,
                    // even indices (start of next segment) are unstroked moves.
                    sgc.LineTo(this.ToPoint(points[i], actualThickness, snap), (i & 1) != 0, true);
                }
            }

            sg.Freeze();
            this.dc.DrawGeometry(null, pen, sg);
        }

        /// <summary>
        /// Lightweight zero-alloc <see cref="IList{T}"/> view over an array slice.
        /// Used to pass a sub-range of <see cref="polyPointBuffer"/> to
        /// <see cref="StreamGeometryContext.PolyLineTo(IList{Point}, bool, bool)"/> without
        /// allocating a new array each call.
        /// </summary>
        /// <remarks>
        /// The indexer performs an unchecked offset+index access. The single caller
        /// (<see cref="DrawLineRange"/>) early-exits when <c>count &lt; 2</c>, so the segment
        /// length passed here is always <c>count - 1 &gt;= 1</c> and the empty-slice path is
        /// unreachable. If a future caller bypasses that guard and passes <c>count == 0</c>,
        /// <c>PolyLineTo</c> will receive an empty <c>IList&lt;Point&gt;</c> — WPF's
        /// implementation iterates via <c>Count</c> and the indexer, both of which are safe
        /// here, but no figure will be drawn.
        /// </remarks>
        private sealed class ArraySegmentList<T> : IList<T>
        {
            private readonly T[] array;
            private readonly int offset;
            private readonly int count;

            public ArraySegmentList(T[] array, int offset, int count)
            {
                this.array = array;
                this.offset = offset;
                this.count = count;
            }

            public T this[int index]
            {
                get => this.array[this.offset + index];
                set => throw new NotSupportedException();
            }

            public int Count => this.count;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < this.count; i++)
                {
                    yield return this.array[this.offset + i];
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();

            public bool Contains(T item)
            {
                int end = this.offset + this.count;
                var cmp = EqualityComparer<T>.Default;
                for (int i = this.offset; i < end; i++)
                {
                    if (cmp.Equals(this.array[i], item)) return true;
                }
                return false;
            }

            public void CopyTo(T[] target, int targetIndex) => Array.Copy(this.array, this.offset, target, targetIndex, this.count);

            public int IndexOf(T item)
            {
                int end = this.offset + this.count;
                var cmp = EqualityComparer<T>.Default;
                for (int i = this.offset; i < end; i++)
                {
                    if (cmp.Equals(this.array[i], item)) return i - this.offset;
                }
                return -1;
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public void Insert(int index, T item) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
            public void RemoveAt(int index) => throw new NotSupportedException();
        }

        /// <summary>
        /// Bounded least-recently-used cache. Used by <see cref="measureCache"/> and
        /// <see cref="drawTextCache"/> so live-data plots (real-time dashboards, oscilloscope
        /// views) cannot accumulate a per-frame stream of distinct numeric labels indefinitely.
        /// On lookup, hits are moved to the front of the recency list; on insert, the oldest
        /// entry is evicted if the capacity is exceeded.
        /// </summary>
        /// <remarks>
        /// Single-threaded by design — all access is from the WPF render thread holding the
        /// DrawingContext. No locking; introducing concurrency here would require external
        /// synchronization at the call site.
        /// </remarks>
        private sealed class LruCache<TKey, TValue>
        {
            private readonly int capacity;
            private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> map;
            private readonly LinkedList<KeyValuePair<TKey, TValue>> list = new LinkedList<KeyValuePair<TKey, TValue>>();

            public LruCache(int capacity)
            {
                if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
                this.capacity = capacity;
                this.map = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(capacity);
            }

            public int Count => this.map.Count;

            public bool TryGetValue(TKey key, out TValue value)
            {
                if (this.map.TryGetValue(key, out var node))
                {
                    // Move-to-front: this entry is now the most recently used.
                    this.list.Remove(node);
                    this.list.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }
                value = default!;
                return false;
            }

            public TValue this[TKey key]
            {
                set
                {
                    if (this.map.TryGetValue(key, out var existing))
                    {
                        // Replace existing entry; move to front.
                        this.list.Remove(existing);
                        this.map.Remove(key);
                    }

                    var newNode = new LinkedListNode<KeyValuePair<TKey, TValue>>(
                        new KeyValuePair<TKey, TValue>(key, value));
                    this.list.AddFirst(newNode);
                    this.map[key] = newNode;

                    // Evict oldest if over capacity.
                    if (this.map.Count > this.capacity)
                    {
                        var oldest = this.list.Last!;
                        this.list.RemoveLast();
                        this.map.Remove(oldest.Value.Key);
                    }
                }
            }

            public void Clear()
            {
                this.map.Clear();
                this.list.Clear();
            }
        }

        /// <inheritdoc/>
        public override void DrawPolygon(
            IList<ScreenPoint> points,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode,
            double[] dashArray,
            LineJoin lineJoin)
        {
            if (this.dc == null || points.Count < 2)
            {
                return;
            }

            var pen = this.CreatePen(stroke, thickness, edgeRenderingMode, lineJoin, dashArray);
            var brush = fill.IsUndefined() ? null : this.GetCachedBrush(fill);
            if (pen == null && brush == null)
            {
                return;
            }

            var actualThickness = pen != null ? pen.Thickness : 0;
            bool snap = this.ShouldSnapPoints(edgeRenderingMode, points);

            var sg = new StreamGeometry { FillRule = FillRule.Nonzero };
            using (var sgc = sg.Open())
            {
                var p0 = this.ToPoint(points[0], actualThickness, snap);
                sgc.BeginFigure(p0, brush != null, true);
                for (int i = 1; i < points.Count; i++)
                {
                    sgc.LineTo(this.ToPoint(points[i], actualThickness, snap), pen != null, false);
                }
            }

            sg.Freeze();
            this.dc.DrawGeometry(brush, pen, sg);
        }

        /// <inheritdoc/>
        public override void DrawPolygons(
            IList<IList<ScreenPoint>> polygons,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode,
            double[] dashArray,
            LineJoin lineJoin)
        {
            if (this.dc == null || polygons.Count == 0)
            {
                return;
            }

            var pen = this.CreatePen(stroke, thickness, edgeRenderingMode, lineJoin, dashArray);
            var brush = fill.IsUndefined() ? null : this.GetCachedBrush(fill);
            if (pen == null && brush == null)
            {
                return;
            }

            var actualThickness = pen != null ? pen.Thickness : 0;

            var sg = new StreamGeometry { FillRule = FillRule.Nonzero };
            using (var sgc = sg.Open())
            {
                foreach (var polygon in polygons)
                {
                    if (polygon.Count == 0)
                    {
                        continue;
                    }

                    bool snap = this.ShouldSnapPoints(edgeRenderingMode, polygon);
                    var p0 = this.ToPoint(polygon[0], actualThickness, snap);
                    sgc.BeginFigure(p0, brush != null, true);
                    for (int i = 1; i < polygon.Count; i++)
                    {
                        sgc.LineTo(this.ToPoint(polygon[i], actualThickness, snap), pen != null, false);
                    }
                }
            }

            sg.Freeze();
            this.dc.DrawGeometry(brush, pen, sg);
        }

        /// <inheritdoc/>
        public override void DrawRectangle(
            OxyRect rect,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode)
        {
            if (this.dc == null)
            {
                return;
            }

            var brush = fill.IsUndefined() ? null : this.GetCachedBrush(fill);
            var pen = this.CreatePen(stroke, thickness, edgeRenderingMode);
            if (brush == null && pen == null)
            {
                return;
            }

            var actualRect = this.GetActualRect(rect, thickness, edgeRenderingMode);
            this.dc.DrawRectangle(brush, pen, actualRect);
        }

        /// <inheritdoc/>
        public override void DrawRectangles(
            IList<OxyRect> rectangles,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode)
        {
            if (this.dc == null || rectangles.Count == 0)
            {
                return;
            }

            var brush = fill.IsUndefined() ? null : this.GetCachedBrush(fill);
            var pen = this.CreatePen(stroke, thickness, edgeRenderingMode);
            if (brush == null && pen == null)
            {
                return;
            }

            var sg = new StreamGeometry { FillRule = FillRule.Nonzero };
            using (var sgc = sg.Open())
            {
                foreach (var rect in rectangles)
                {
                    var r = this.GetActualRect(rect, thickness, edgeRenderingMode);
                    sgc.BeginFigure(r.TopLeft, brush != null, true);
                    sgc.PolyLineTo(new[] { r.TopRight, r.BottomRight, r.BottomLeft }, pen != null, false);
                }
            }

            sg.Freeze();
            this.dc.DrawGeometry(brush, pen, sg);
        }

        /// <inheritdoc/>
        public override void DrawEllipse(
            OxyRect rect,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode)
        {
            if (this.dc == null)
            {
                return;
            }

            var brush = fill.IsUndefined() ? null : this.GetCachedBrush(fill);
            var pen = this.CreatePen(stroke, thickness, edgeRenderingMode);
            if (brush == null && pen == null)
            {
                return;
            }

            var center = new Point(rect.Center.X, rect.Center.Y);
            this.dc.DrawEllipse(brush, pen, center, rect.Width / 2, rect.Height / 2);
        }

        /// <inheritdoc/>
        public override void DrawEllipses(
            IList<OxyRect> rectangles,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode)
        {
            if (this.dc == null || rectangles.Count == 0)
            {
                return;
            }

            var brush = fill.IsUndefined() ? null : this.GetCachedBrush(fill);
            var pen = this.CreatePen(stroke, thickness, edgeRenderingMode);
            if (brush == null && pen == null)
            {
                return;
            }

            // Each ellipse generates 3 path commands (BeginFigure + 2 ArcTo). Tile at 4096
            // ellipses (~12k path commands) per StreamGeometry — well below the line-tile
            // ceiling of 16384 and large enough that high-density marker plots (5k+ markers
            // across 20 series) emit a small number of frozen geometries instead of dozens.
            // The previous 500-ellipse tile dated from the canvas-renderer era; the
            // DrawingVisual path handles much larger geometry batches efficiently.
            const int ellipseTileSize = 4096;
            int count = rectangles.Count;
            int start = 0;

            while (start < count)
            {
                int end = Math.Min(start + ellipseTileSize, count);
                var sg = new StreamGeometry { FillRule = FillRule.Nonzero };
                using (var sgc = sg.Open())
                {
                    for (int i = start; i < end; i++)
                    {
                        var rect = rectangles[i];
                        var centerY = rect.Center.Y;
                        sgc.BeginFigure(new Point(rect.Right, centerY), brush != null, true);
                        var size = new Size(rect.Width / 2, rect.Height / 2);
                        sgc.ArcTo(new Point(rect.Left, centerY), size, 180, false, SweepDirection.Clockwise, pen != null, false);
                        sgc.ArcTo(new Point(rect.Right, centerY), size, 180, false, SweepDirection.Clockwise, pen != null, false);
                    }
                }

                sg.Freeze();
                this.dc.DrawGeometry(brush, pen, sg);
                start = end;
            }
        }

        /// <inheritdoc/>
        public override void DrawText(
            ScreenPoint p,
            string text,
            OxyColor fill,
            string fontFamily,
            double fontSize,
            double fontWeight,
            double rotate,
            HorizontalAlignment halign,
            VerticalAlignment valign,
            OxySize? maxSize)
        {
            if (this.dc == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            var brush = this.GetCachedBrush(fill);
            if (brush == null)
            {
                return;
            }

            // Cache lookup: same (text, font, size, weight, color, mode, culture) produces an
            // identical FormattedText render. Axis tick labels and repeated legend text hit this
            // path on every frame — caching saves ~100 allocations per render on a typical plot.
            // NEVER mutate cached FormattedText instances (see drawTextCache docs).
            var isBold = fontWeight > FontWeights.Normal;
            var culture = CultureInfo.CurrentUICulture;
            var drawKey = (text, fontFamily ?? "Segoe UI", fontSize > 0 ? fontSize : 12, isBold, fill, this.TextFormattingMode, culture.Name);
            if (!this.drawTextCache.TryGetValue(drawKey, out var ft))
            {
                var typeface = this.CreateTypeface(fontFamily, fontWeight);
                ft = new FormattedText(
                    text,
                    culture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize > 0 ? fontSize : 12,
                    brush,
                    null,
                    this.TextFormattingMode,
                    this.DpiScale);
                this.drawTextCache[drawKey] = ft;
            }

            double textWidth = ft.Width;
            double textHeight = ft.Height;

            if (maxSize != null)
            {
                if (textWidth > maxSize.Value.Width + 1e-3)
                {
                    textWidth = Math.Max(maxSize.Value.Width, 0);
                }

                if (textHeight > maxSize.Value.Height + 1e-3)
                {
                    textHeight = Math.Max(maxSize.Value.Height, 0);
                }
            }

            double dx = 0;
            double dy = 0;

            if (halign == HorizontalAlignment.Center)
            {
                dx = -textWidth / 2;
            }
            else if (halign == HorizontalAlignment.Right)
            {
                dx = -textWidth;
            }

            if (valign == VerticalAlignment.Middle)
            {
                dy = -textHeight / 2;
            }
            else if (valign == VerticalAlignment.Bottom)
            {
                dy = -textHeight;
            }

            bool hasRotation = Math.Abs(rotate) > double.Epsilon;

            if (hasRotation)
            {
                // IMPORTANT: do NOT reuse a cached Transform here. DrawingContext.PushTransform
                // captures the Transform by reference; the DrawingVisual command stream
                // re-evaluates the Transform's current matrix at composition time. Mutating a
                // cached transform between draws causes every previously-recorded rotated text
                // (axis titles, annotation labels) to render at the latest position+angle —
                // visible as "Y-axis title appears at the polyline annotation" / "labels jump
                // and overlap." Allocating fresh transforms per call is the correct behaviour;
                // any caching would have to freeze immediately after construction (defeating
                // the cache).
                this.dc.PushTransform(new TranslateTransform(p.X, p.Y));
                this.dc.PushTransform(new RotateTransform(rotate));
                this.dc.DrawText(ft, new Point(dx, dy));
                this.dc.Pop();
                this.dc.Pop();
            }
            else
            {
                this.dc.DrawText(ft, new Point(p.X + dx, p.Y + dy));
            }

            // Record text position for hit testing
            var bounds = ComputeTextBounds(p.X, p.Y, dx, dy, textWidth, textHeight, rotate);
            this.renderedText.Add(new TextHitResult(text, bounds, fontSize));
        }

        /// <inheritdoc/>
        public override OxySize MeasureText(string text, string fontFamily, double fontSize, double fontWeight)
        {
            if (string.IsNullOrEmpty(text))
            {
                return OxySize.Empty;
            }

            var isBold = fontWeight > FontWeights.Normal;
            var culture = CultureInfo.CurrentUICulture;
            var cacheKey = (text, fontFamily ?? "Segoe UI", fontSize > 0 ? fontSize : 12, isBold, this.TextFormattingMode, culture.Name);
            if (this.measureCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var typeface = this.CreateTypeface(fontFamily, fontWeight);
            var ft = new FormattedText(
                text,
                culture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize > 0 ? fontSize : 12,
                Brushes.Black,
                null,
                this.TextFormattingMode,
                this.DpiScale);

            var result = new OxySize(ft.Width, ft.Height);
            this.measureCache[cacheKey] = result;
            return result;
        }

        /// <inheritdoc/>
        public override void DrawImage(
            OxyImage source,
            double srcX,
            double srcY,
            double srcWidth,
            double srcHeight,
            double destX,
            double destY,
            double destWidth,
            double destHeight,
            double opacity,
            bool interpolate)
        {
            if (this.dc == null || destWidth <= 0 || destHeight <= 0 || srcWidth <= 0 || srcHeight <= 0)
            {
                return;
            }

            var bitmapChain = this.GetImageSource(source);
            if (bitmapChain == null)
            {
                return;
            }

            // Crop the source region if it differs from the full image.
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (srcX != 0 || srcY != 0 || srcWidth != bitmapChain.PixelWidth || srcHeight != bitmapChain.PixelHeight)
            // ReSharper restore CompareOfFloatsByEqualityOperator
            {
                bitmapChain = new CroppedBitmap(bitmapChain, new Int32Rect((int)srcX, (int)srcY, (int)srcWidth, (int)srcHeight));
            }

            bool applyOpacity = Math.Abs(opacity - 1.0) > double.Epsilon;
            if (applyOpacity)
            {
                this.dc.PushOpacity(opacity);
            }

            this.dc.DrawImage(bitmapChain, new Rect(destX, destY, destWidth, destHeight));

            if (applyOpacity)
            {
                this.dc.Pop();
            }
        }

        /// <inheritdoc/>
        public override void SetToolTip(string text)
        {
            // No-op: tooltips are handled at the model level via TrackerManipulator.
            // The tracker popup is rendered on the overlays Canvas, which is independent of this render context.
        }

        /// <summary>
        /// Reusable buffer for image-cache eviction in <see cref="CleanUp"/>. Avoids the
        /// per-render LINQ + List allocation that would otherwise fire on every render even
        /// for plots that don't use images.
        /// </summary>
        private List<OxyImage> imageEvictBuffer;

        /// <inheritdoc/>
        public override void CleanUp()
        {
            // Fast path: no images cached, nothing to evict.
            if (this.imageCache.Count == 0)
            {
                this.imagesInUse.Clear();
                return;
            }

            // Fast path: every cached image was used this frame — clear the in-use set and
            // exit without walking the cache.
            if (this.imageCache.Count == this.imagesInUse.Count)
            {
                this.imagesInUse.Clear();
                return;
            }

            // Slow path: at least one cached image was not used this frame. Reuse a private
            // List buffer (Clear+Add) instead of the LINQ Where().ToList() that would allocate
            // a fresh list on every render.
            if (this.imageEvictBuffer == null)
            {
                this.imageEvictBuffer = new List<OxyImage>();
            }
            else
            {
                this.imageEvictBuffer.Clear();
            }

            foreach (var key in this.imageCache.Keys)
            {
                if (!this.imagesInUse.Contains(key))
                {
                    this.imageEvictBuffer.Add(key);
                }
            }

            for (int i = 0; i < this.imageEvictBuffer.Count; i++)
            {
                this.imageCache.Remove(this.imageEvictBuffer[i]);
            }

            this.imageEvictBuffer.Clear();
            this.imagesInUse.Clear();
        }

        /// <summary>
        /// Returns the topmost rendered text element at the given point, or null.
        /// Searches back-to-front (topmost first) for the text element at the specified point.
        /// </summary>
        /// <param name="point">The point to test, in plot coordinates.</param>
        /// <returns>A <see cref="TextHitResult"/> if a text element was hit; otherwise, null.</returns>
        /// <remarks>
        /// This provides backend-agnostic text hit-testing equivalent to
        /// <c>Canvas.InputHitTest()</c> returning a <c>TextBlock</c> in the Canvas backend.
        /// Used by the OxyPlotToolbar for text editing initiation.
        /// </remarks>
        public TextHitResult HitTestText(ScreenPoint point)
        {
            for (int i = this.renderedText.Count - 1; i >= 0; i--)
            {
                if (this.renderedText[i].Bounds.Contains(point.X, point.Y))
                {
                    return this.renderedText[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a cached frozen brush for the specified color.
        /// </summary>
        /// <param name="color">The OxyPlot color.</param>
        /// <returns>A frozen <see cref="SolidColorBrush"/>, or null if the color is fully transparent.</returns>
        private Brush GetCachedBrush(OxyColor color)
        {
            if (color.A == 0)
            {
                return null;
            }

            if (!this.brushCache.TryGetValue(color, out var brush))
            {
                brush = new SolidColorBrush(color.ToColor());
                brush.Freeze();
                this.brushCache.Add(color, brush);
            }

            return brush;
        }

        /// <summary>
        /// Gets or creates a cached frozen <see cref="Pen"/> for the specified stroke parameters.
        /// Pens with identical parameters are reused within and across frames, avoiding repeated
        /// allocation and Freeze() overhead for gridlines, axes, and series that share the same stroke.
        /// </summary>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="edgeRenderingMode">The edge rendering mode (affects thickness snapping).</param>
        /// <param name="lineJoin">The line join style.</param>
        /// <param name="dashArray">The dash pattern, or null for solid lines.</param>
        /// <returns>A frozen <see cref="Pen"/>, or null if the stroke is undefined or thickness is zero.</returns>
        private Pen CreatePen(
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode,
            LineJoin lineJoin = LineJoin.Miter,
            double[] dashArray = null)
        {
            if (stroke.IsUndefined() || thickness <= 0)
            {
                return null;
            }

            var actualThickness = this.GetActualStrokeThickness(thickness, edgeRenderingMode);

            // Compute cache key from pen parameters
            long key = ComputePenCacheKey(stroke, actualThickness, lineJoin, dashArray);
            if (this.penCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var brush = this.GetCachedBrush(stroke);
            if (brush == null)
            {
                return null;
            }

            var pen = new Pen(brush, actualThickness);

            switch (lineJoin)
            {
                case LineJoin.Round:
                    pen.LineJoin = PenLineJoin.Round;
                    break;
                case LineJoin.Bevel:
                    pen.LineJoin = PenLineJoin.Bevel;
                    break;
            }

            if (dashArray != null)
            {
                pen.DashStyle = new DashStyle(dashArray, 0);

                // WPF Pen.DashCap defaults to PenLineCap.Square, which extends each dash
                // by half the stroke thickness at each end. OxyPlot's dash arrays use
                // 1-unit gaps (e.g., {4,1} for Dash), so square caps fill the gap entirely,
                // making all dash styles render as solid lines.
                // Shape.StrokeDashCap (used by CanvasRenderContext) defaults to Flat.
                // Match that behavior here for 1:1 parity.
                pen.DashCap = PenLineCap.Flat;
            }

            pen.Freeze();
            this.penCache[key] = pen;
            return pen;
        }

        /// <summary>
        /// Computes a cache key for pen parameters.
        /// Combines color, thickness, line join, and dash array hash into a single long value.
        /// </summary>
        private static long ComputePenCacheKey(OxyColor color, double thickness, LineJoin lineJoin, double[] dashArray)
        {
            unchecked
            {
                int colorHash = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
                int dashHash = 0;
                if (dashArray != null)
                {
                    dashHash = dashArray.Length;
                    for (int i = 0; i < dashArray.Length; i++)
                    {
                        dashHash = dashHash * 31 + dashArray[i].GetHashCode();
                    }
                }

                // Use multiplicative hash combining instead of XOR to avoid collisions.
                // Each component is mixed with a prime multiplier before combining.
                long upper = (long)colorHash << 32;
                int lower = thickness.GetHashCode();
                lower = lower * 397 + (int)lineJoin;
                lower = lower * 397 + dashHash;
                return upper | (lower & 0xFFFFFFFFL);
            }
        }

        /// <summary>
        /// Creates a typeface for the specified font family and weight.
        /// </summary>
        /// <param name="fontFamily">The font family name, or null for the default font.</param>
        /// <param name="fontWeight">The font weight value.</param>
        /// <returns>A <see cref="Typeface"/> instance.</returns>
        private Typeface CreateTypeface(string fontFamily, double fontWeight)
        {
            var isBold = fontWeight > FontWeights.Normal;
            var key = (fontFamily ?? "Segoe UI", isBold);
            if (!this.typefaceCache.TryGetValue(key, out var typeface))
            {
                var ff = fontFamily != null ? this.GetCachedFontFamily(fontFamily) : new FontFamily("Segoe UI");
                var fw = isBold ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal;
                typeface = new Typeface(ff, FontStyles.Normal, fw, FontStretches.Normal);
                this.typefaceCache[key] = typeface;
            }

            return typeface;
        }

        /// <summary>
        /// Gets a cached font family for the specified name.
        /// </summary>
        /// <param name="familyName">The font family name.</param>
        /// <returns>A <see cref="FontFamily"/> instance.</returns>
        private FontFamily GetCachedFontFamily(string familyName)
        {
            if (familyName == null)
            {
                return null;
            }

            if (!this.fontFamilyCache.TryGetValue(familyName, out var ff))
            {
                ff = new FontFamily(familyName);
                this.fontFamilyCache.Add(familyName, ff);
            }

            return ff;
        }

        /// <summary>
        /// Gets a cached bitmap source for the specified image.
        /// </summary>
        /// <param name="image">The OxyPlot image.</param>
        /// <returns>A <see cref="BitmapSource"/>, or null if the image is null.</returns>
        private BitmapSource GetImageSource(OxyImage image)
        {
            if (image == null)
            {
                return null;
            }

            if (!this.imagesInUse.Contains(image))
            {
                this.imagesInUse.Add(image);
            }

            if (this.imageCache.TryGetValue(image, out var src))
            {
                return src;
            }

            using (var ms = new MemoryStream(image.GetData()))
            {
                var btm = new BitmapImage();
                btm.BeginInit();
                btm.StreamSource = ms;
                btm.CacheOption = BitmapCacheOption.OnLoad;
                btm.EndInit();
                btm.Freeze();
                this.imageCache.Add(image, btm);
                return btm;
            }
        }

        /// <summary>
        /// Gets the actual stroke thickness, snapped to device pixels for <see cref="EdgeRenderingMode.PreferSharpness"/>.
        /// </summary>
        /// <param name="thickness">The requested stroke thickness.</param>
        /// <param name="edgeRenderingMode">The edge rendering mode.</param>
        /// <returns>The actual stroke thickness.</returns>
        private double GetActualStrokeThickness(double thickness, EdgeRenderingMode edgeRenderingMode)
        {
            switch (edgeRenderingMode)
            {
                case EdgeRenderingMode.PreferSharpness:
                    return PixelLayout.SnapStrokeThickness(thickness, this.DpiScale);
                default:
                    return thickness;
            }
        }

        /// <summary>
        /// Gets the actual rectangle, snapped to device pixels based on edge rendering mode.
        /// </summary>
        /// <param name="rect">The requested rectangle.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="edgeRenderingMode">The edge rendering mode.</param>
        /// <returns>The actual rectangle.</returns>
        private Rect GetActualRect(OxyRect rect, double thickness, EdgeRenderingMode edgeRenderingMode)
        {
            switch (edgeRenderingMode)
            {
                case EdgeRenderingMode.PreferGeometricAccuracy:
                case EdgeRenderingMode.PreferSpeed:
                    return ToRect(rect);
                default:
                    return PixelLayout.Snap(ToRect(rect), thickness, this.VisualOffset, this.DpiScale);
            }
        }

        /// <summary>
        /// Determines whether points should be snapped to device pixels.
        /// </summary>
        /// <param name="edgeRenderingMode">The edge rendering mode.</param>
        /// <param name="points">The points to check.</param>
        /// <returns>True if points should be pixel-snapped.</returns>
        private bool ShouldSnapPoints(EdgeRenderingMode edgeRenderingMode, IList<ScreenPoint> points)
        {
            switch (edgeRenderingMode)
            {
                case EdgeRenderingMode.PreferSharpness:
                    return true;
                case EdgeRenderingMode.Adaptive:
                case EdgeRenderingMode.Automatic:
                    return IsStraightLine(points);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Converts a <see cref="ScreenPoint"/> to a <see cref="Point"/>, optionally snapping to device pixels.
        /// </summary>
        /// <param name="sp">The screen point.</param>
        /// <param name="strokeThickness">The stroke thickness (for pixel snapping offset calculation).</param>
        /// <param name="snap">Whether to snap to device pixels.</param>
        /// <returns>The WPF point.</returns>
        private Point ToPoint(ScreenPoint sp, double strokeThickness, bool snap)
        {
            if (snap)
            {
                return PixelLayout.Snap(sp.X, sp.Y, strokeThickness, this.VisualOffset, this.DpiScale);
            }

            return new Point(sp.X, sp.Y);
        }

        /// <summary>
        /// Computes the axis-aligned bounding box of a text element, accounting for rotation.
        /// </summary>
        /// <param name="px">The text position X.</param>
        /// <param name="py">The text position Y.</param>
        /// <param name="dx">The alignment offset X.</param>
        /// <param name="dy">The alignment offset Y.</param>
        /// <param name="width">The text width.</param>
        /// <param name="height">The text height.</param>
        /// <param name="rotate">The rotation angle in degrees.</param>
        /// <returns>The bounding rectangle of the text.</returns>
        private static OxyRect ComputeTextBounds(
            double px, double py, double dx, double dy, double width, double height, double rotate)
        {
            if (Math.Abs(rotate) < double.Epsilon)
            {
                return new OxyRect(px + dx, py + dy, width, height);
            }

            double rad = rotate * Math.PI / 180;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            // The four corners of the text rect, relative to the rotation origin.
            double x0 = dx, y0 = dy;
            double x1 = dx + width, y1 = dy;
            double x2 = dx + width, y2 = dy + height;
            double x3 = dx, y3 = dy + height;

            // Rotate each corner and translate to position.
            double rx0 = x0 * cos - y0 * sin + px;
            double ry0 = x0 * sin + y0 * cos + py;
            double rx1 = x1 * cos - y1 * sin + px;
            double ry1 = x1 * sin + y1 * cos + py;
            double rx2 = x2 * cos - y2 * sin + px;
            double ry2 = x2 * sin + y2 * cos + py;
            double rx3 = x3 * cos - y3 * sin + px;
            double ry3 = x3 * sin + y3 * cos + py;

            double minX = Math.Min(Math.Min(rx0, rx1), Math.Min(rx2, rx3));
            double minY = Math.Min(Math.Min(ry0, ry1), Math.Min(ry2, ry3));
            double maxX = Math.Max(Math.Max(rx0, rx1), Math.Max(rx2, rx3));
            double maxY = Math.Max(Math.Max(ry0, ry1), Math.Max(ry2, ry3));

            return new OxyRect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Converts an <see cref="OxyRect"/> to a WPF <see cref="Rect"/>.
        /// </summary>
        /// <param name="r">The OxyPlot rectangle.</param>
        /// <returns>The WPF rectangle.</returns>
        private static Rect ToRect(OxyRect r)
        {
            return new Rect(r.Left, r.Top, r.Width, r.Height);
        }

        /// <summary>
        /// Represents a rendered text element for hit-testing.
        /// </summary>
        public class TextHitResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TextHitResult"/> class.
            /// </summary>
            /// <param name="text">The text content.</param>
            /// <param name="bounds">The bounding rectangle of the text.</param>
            /// <param name="fontSize">The font size.</param>
            public TextHitResult(string text, OxyRect bounds, double fontSize)
            {
                this.Text = text;
                this.Bounds = bounds;
                this.FontSize = fontSize;
            }

            /// <summary>
            /// Gets the text content.
            /// </summary>
            public string Text { get; }

            /// <summary>
            /// Gets the axis-aligned bounding rectangle of the rendered text.
            /// </summary>
            public OxyRect Bounds { get; }

            /// <summary>
            /// Gets the font size used to render the text.
            /// </summary>
            public double FontSize { get; }
        }
    }
}
