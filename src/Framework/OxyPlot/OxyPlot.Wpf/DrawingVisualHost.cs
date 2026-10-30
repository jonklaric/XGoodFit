// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DrawingVisualHost.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   A FrameworkElement that hosts a single DrawingVisual for immediate-mode rendering.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia.Controls;
    using Avalonia.Media;

    /// <summary>
    /// A <c>FrameworkElement</c> that hosts a single <see cref="DrawingVisual"/>
    /// for immediate-mode rendering via <see cref="DrawingVisualRenderContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the standard WPF pattern for hosting a DrawingVisual inside the element tree.
    /// One element, one visual, zero layout overhead per rendered shape.
    /// </para>
    /// <para>
    /// XGoodFit port: an Avalonia <see cref="Control"/> that replays its <see cref="DrawingVisual"/>
    /// (XGoodFit's stand-in for WPF's) in <see cref="Render"/>, and asks to render whenever the drawing
    /// changes; WPF's visual was its child and redrew by itself. Avalonia has no text formatting modes or
    /// ClearType hints, and no SnapsToDevicePixels, so those settings aren't ported. The edge mode
    /// (<see cref="DisableShapeAntiAliasing"/>) is the host's own, which its drawing uses; text stays
    /// anti-aliased, as in WPF. The bitmap cache needs no scale of its own, as Avalonia's renders at the
    /// top level's scaling.
    /// </para>
    /// </remarks>
    public class DrawingVisualHost : Control
    {
        /// <summary>
        /// The hosted drawing visual.
        /// </summary>
        private readonly DrawingVisual visual = new DrawingVisual();

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingVisualHost"/> class.
        /// </summary>
        public DrawingVisualHost()
        {
            // XGoodFit port: WPF's text formatting mode and ClearType hints, set here for crisp text,
            // have no Avalonia equivalents.

            this.UseLayoutRounding = true;

            // XGoodFit port: the drawing isn't a child visual; this host draws it (Render), and renders
            // again when it changes.
            this.visual.Changed += (s, e) => this.InvalidateVisual();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the rasterized output of this element is
        /// cached as a bitmap in video memory via <c>UIElement.CacheMode</c> +
        /// <see cref="BitmapCache"/>. Default <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, WPF renders this element's visual subtree once into a GPU bitmap
        /// and reuses the bitmap on subsequent compositor frames. The cache regenerates only
        /// when the structure of the subtree changes — which for a plot means whenever the
        /// inner <see cref="DrawingVisual"/>'s content is rewritten via
        /// <see cref="DrawingVisual.RenderOpen"/> (i.e., on every <c>Plot.InvalidatePlot</c>).
        /// </para>
        /// <para>
        /// <b>When this helps.</b> Scenarios where the plot is static but the surface is
        /// repeatedly composited: dragging a transient overlay (e.g., the magnifier-glass zoom
        /// rectangle) over the plot, parent panel resize animations, dock-splitter drags, or
        /// the plot scrolling into and out of viewport. Without the cache, WPF re-rasterizes
        /// the plot's geometry under the dirty region on every compositor frame.
        /// </para>
        /// <para>
        /// <b>When this doesn't help (and may slightly regress).</b> Scenarios where every
        /// frame re-renders the plot (e.g., real-time streaming data). The cache regenerates
        /// each render, paying a small overhead with no reuse benefit.
        /// </para>
        /// <para>
        /// <see cref="BitmapCache.RenderAtScale"/> is set to the current per-monitor DPI scale
        /// to keep the cached bitmap sharp. <see cref="BitmapCache.SnapsToDevicePixels"/> is
        /// enabled to align with the existing pixel-snapping configuration.
        /// </para>
        /// <para>
        /// XGoodFit port: the preference is kept, but no bitmap cache is set. Avalonia 12's
        /// <see cref="BitmapCache"/> keeps the first bitmap of a visual that draws itself, as this host
        /// does, so a cached plot never showed what changed after it first showed (BestFit's Markov chain
        /// traces stayed empty). Avalonia's compositor keeps the plot's drawing between frames, so an
        /// overlay dragged over the plot doesn't run its rendering again.
        /// </para>
        /// </remarks>
        public bool UseBitmapCache { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anti-aliasing is disabled for shape rendering
        /// inside the hosted <see cref="DrawingVisual"/>. Default is <c>false</c> (anti-aliasing on).
        /// </summary>
        /// <remarks>
        /// <para>
        /// When set to <c>true</c>, applies <see cref="EdgeMode.Aliased"/> to the visual via
        /// <c>RenderOptions.SetEdgeMode</c>. This disables WPF's stroke-tessellation
        /// anti-aliasing for the entire visual.
        /// </para>
        /// <para>
        /// <b>Why it matters for performance.</b> WPF's stroked-geometry tessellation runs on the
        /// render thread and is the dominant per-series cost for plots with many <see cref="OxyPlot.Series.LineSeries"/>
        /// instances (e.g. a 20-chain MCMC trace at 1751 points each). Disabling anti-aliasing
        /// skips the tessellation entirely and yields a multi-x speedup for line rendering.
        /// Markers, fills, and text are unaffected (text uses its own rendering path). For thin
        /// strokes (≤2 px) the visual difference is barely perceptible; for trace/density plots
        /// the perf gain is decisive.
        /// </para>
        /// <para>
        /// Background: this is the same root cause flagged by OxyPlot upstream issue #1286
        /// ("LineSeries has bad performance with large amount of data due to aliased=false").
        /// </para>
        /// </remarks>
        public bool DisableShapeAntiAliasing
        {
            get => RenderOptions.GetEdgeMode(this) == EdgeMode.Aliased;
            set
            {
                RenderOptions.SetEdgeMode(this, value ? EdgeMode.Aliased : EdgeMode.Unspecified);

                // XGoodFit port: Avalonia's edge mode also aliases text whose rendering mode is unset,
                // which WPF's doesn't (see PlotViewBase.DisableShapeAntiAliasing).
                TextOptions.SetTextRenderingMode(this, value ? TextRenderingMode.SubpixelAntialias : TextRenderingMode.Unspecified);
            }
        }

        /// <summary>
        /// Gets the hosted <see cref="DrawingVisual"/>.
        /// </summary>
        public DrawingVisual Visual => this.visual;

        /// <summary>
        /// Draws the hosted drawing.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <remarks>XGoodFit port: replaces WPF's visual child (VisualChildrenCount and GetVisualChild).</remarks>
        public override void Render(DrawingContext context)
        {
            base.Render(context);
            this.visual.Render(context);
        }

        // XGoodFit port: WPF's OnDpiChanged override, which set the bitmap cache's RenderAtScale to the
        // new monitor's scale, isn't needed: Avalonia's cache follows the top level's scaling itself.
    }
}
