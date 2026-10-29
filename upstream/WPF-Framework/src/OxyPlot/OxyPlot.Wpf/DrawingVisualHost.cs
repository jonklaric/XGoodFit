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
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// A <see cref="FrameworkElement"/> that hosts a single <see cref="DrawingVisual"/>
    /// for immediate-mode rendering via <see cref="DrawingVisualRenderContext"/>.
    /// </summary>
    /// <remarks>
    /// This is the standard WPF pattern for hosting a DrawingVisual inside the element tree.
    /// One element, one visual, zero layout overhead per rendered shape.
    /// </remarks>
    public class DrawingVisualHost : FrameworkElement
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
            // Match CanvasRenderContext's text quality: use Display mode for pixel-snapped,
            // crisp text rendering. Without this, FormattedText drawn inside the DrawingVisual
            // uses Ideal mode, which produces blurrier text than the Canvas backend.
            // Set on both the FrameworkElement (this) and the DrawingVisual — WPF reads
            // TextFormattingMode from the nearest UIElement ancestor during DrawingContext.DrawText().
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextFormattingMode(this.visual, TextFormattingMode.Display);

            // Enable ClearType sub-pixel rendering even when transforms (e.g., RotateTransform
            // for Y-axis titles) are applied. Without this hint, WPF falls back to grayscale
            // anti-aliasing for transformed text, producing noticeably blurrier results.
            // The Canvas backend sets ClearTypeHint.Enabled per TextBlock (CanvasRenderContext line 392);
            // setting it on the host element and visual achieves the same effect for DrawingVisual.
            RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);
            RenderOptions.SetClearTypeHint(this.visual, ClearTypeHint.Enabled);

            this.UseLayoutRounding = true;
            this.SnapsToDevicePixels = true;
            this.AddVisualChild(this.visual);
            this.AddLogicalChild(this.visual);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the rasterized output of this element is
        /// cached as a bitmap in video memory via <see cref="UIElement.CacheMode"/> +
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
        /// </remarks>
        public bool UseBitmapCache
        {
            get => this.CacheMode is BitmapCache;
            set
            {
                if (value)
                {
                    var dpiInfo = VisualTreeHelper.GetDpi(this);
                    var scale = dpiInfo.PixelsPerDip > 0 ? dpiInfo.PixelsPerDip : 1.0;
                    this.CacheMode = new BitmapCache
                    {
                        RenderAtScale = scale,
                        SnapsToDevicePixels = true,
                        EnableClearType = false,
                    };
                }
                else
                {
                    this.CacheMode = null;
                }
            }
        }

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
            get => RenderOptions.GetEdgeMode(this.visual) == EdgeMode.Aliased;
            set
            {
                RenderOptions.SetEdgeMode(this.visual, value ? EdgeMode.Aliased : EdgeMode.Unspecified);
                RenderOptions.SetEdgeMode(this, value ? EdgeMode.Aliased : EdgeMode.Unspecified);
            }
        }

        /// <summary>
        /// Gets the hosted <see cref="DrawingVisual"/>.
        /// </summary>
        public DrawingVisual Visual => this.visual;

        /// <summary>
        /// Gets the number of visual child elements within this element.
        /// </summary>
        protected override int VisualChildrenCount => 1;

        /// <summary>
        /// Returns the specified visual child.
        /// </summary>
        /// <param name="index">The index of the visual child.</param>
        /// <returns>The visual child at the specified index.</returns>
        protected override Visual GetVisualChild(int index)
        {
            return this.visual;
        }

        /// <summary>
        /// Re-applies <see cref="BitmapCache.RenderAtScale"/> when the host moves between
        /// monitors with different DPI scales. Without this, an active <see cref="BitmapCache"/>
        /// retains the original monitor's <c>PixelsPerDip</c> and renders blurry on the new
        /// monitor (low-DPI cache scaled up) or unnecessarily oversharp (high-DPI cache scaled
        /// down) until <see cref="UseBitmapCache"/> is toggled off and on.
        /// </summary>
        /// <param name="oldDpi">The DPI scale before the change.</param>
        /// <param name="newDpi">The DPI scale after the change.</param>
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            if (this.CacheMode is BitmapCache bc)
            {
                bc.RenderAtScale = newDpi.PixelsPerDip > 0 ? newDpi.PixelsPerDip : 1.0;
            }
        }
    }
}
