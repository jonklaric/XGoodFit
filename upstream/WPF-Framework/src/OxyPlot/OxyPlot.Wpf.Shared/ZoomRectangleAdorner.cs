// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ZoomRectangleAdorner.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Lightweight overlay element for rendering the zoom-rectangle drag affordance with minimal per-mouse-move cost.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Lightweight overlay used to render the zoom-rectangle affordance during a magnifier-glass
    /// drag. Replaces the historical <see cref="System.Windows.Controls.ContentControl"/> +
    /// <see cref="System.Windows.Controls.ControlTemplate"/> approach with a single
    /// <see cref="FrameworkElement"/> that draws via <see cref="OnRender"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists.</b> The previous implementation used a templated <see cref="System.Windows.Controls.ContentControl"/>
    /// resized via <see cref="FrameworkElement.Width"/>/<see cref="FrameworkElement.Height"/> on
    /// every mouse-move during a drag. WPF responded to those property changes by invalidating
    /// measure on the control and arrange on its parent <see cref="System.Windows.Controls.Canvas"/>,
    /// scheduling a layout pass at every mouse-move (60–125 Hz). Combined with the dashed-stroke
    /// tessellation in the default template, that produced visible multi-second drag lag on dense
    /// 20-series plots.
    /// </para>
    /// <para>
    /// <b>What this does instead.</b> The adorner sits at <c>(0, 0)</c> of the overlay canvas
    /// and is sized to the full plot area once, then never resized. Mouse-move updates only
    /// mutate an internal <see cref="Rect"/> field via <see cref="SetBounds"/> and call
    /// <see cref="UIElement.InvalidateVisual"/>, which schedules a render-priority callback
    /// without any layout pass. <see cref="OnRender"/> draws a single rectangle each frame using
    /// frozen, cached <see cref="Pen"/> and <see cref="Brush"/> instances. <see cref="EdgeMode.Aliased"/>
    /// is applied to skip stroke-tessellation anti-aliasing.
    /// </para>
    /// <para>
    /// The adorner is hit-test invisible so it never intercepts the drag's mouse events.
    /// </para>
    /// </remarks>
    public sealed class ZoomRectangleAdorner : FrameworkElement
    {
        /// <summary>
        /// Frozen, shared brush for the rectangle fill (semi-transparent yellow). Frozen brushes
        /// can be reused freely across visuals without copy overhead.
        /// </summary>
        private static readonly Brush FillBrush;

        /// <summary>
        /// Frozen, shared pen for the rectangle outline (1 px black, "3,1" dashed).
        /// </summary>
        private static readonly Pen StrokePen;

        /// <summary>
        /// The current zoom-rectangle bounds in adorner-local coordinates. Updated by
        /// <see cref="SetBounds"/>. Drawn each frame by <see cref="OnRender"/>.
        /// </summary>
        private Rect bounds;

        /// <summary>
        /// True when the adorner has any rectangle to draw.
        /// </summary>
        private bool hasBounds;

        static ZoomRectangleAdorner()
        {
            FillBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0x00));
            FillBrush.Freeze();

            var dash = new DashStyle(new double[] { 3, 1 }, 0);
            dash.Freeze();
            StrokePen = new Pen(Brushes.Black, 1) { DashStyle = dash, DashCap = PenLineCap.Flat };
            StrokePen.Freeze();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomRectangleAdorner"/> class.
        /// </summary>
        public ZoomRectangleAdorner()
        {
            // Aliased edges skip WPF stroke-tessellation AA. The rectangle is a transient drag
            // affordance — slight pixel jaggedness is acceptable and worth the major perf win.
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

            // Drag affordance never intercepts pointer events.
            this.IsHitTestVisible = false;

            // The adorner draws to its own coordinate space; no layout participation needed.
            this.SnapsToDevicePixels = true;
        }

        /// <summary>
        /// Sets the rectangle to draw. Cheap; only invalidates the visual (no layout pass).
        /// </summary>
        /// <param name="r">The rectangle in adorner-local coordinates.</param>
        public void SetBounds(Rect r)
        {
            if (this.hasBounds && this.bounds == r) return;
            this.bounds = r;
            this.hasBounds = true;
            this.InvalidateVisual();
        }

        /// <summary>
        /// Clears the rectangle so the adorner draws nothing on the next render frame.
        /// </summary>
        public void ClearBounds()
        {
            if (!this.hasBounds) return;
            this.hasBounds = false;
            this.InvalidateVisual();
        }

        /// <inheritdoc/>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (!this.hasBounds) return;
            if (this.bounds.Width <= 0 || this.bounds.Height <= 0) return;
            drawingContext.DrawRectangle(FillBrush, StrokePen, this.bounds);
        }
    }
}
