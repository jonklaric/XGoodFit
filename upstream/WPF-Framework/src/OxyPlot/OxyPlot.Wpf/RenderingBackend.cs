// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RenderingBackend.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Specifies the rendering backend used by <see cref="PlotView"/>.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    /// <summary>
    /// Specifies the rendering backend used by <see cref="PlotView"/>.
    /// </summary>
    public enum RenderingBackend
    {
        /// <summary>
        /// Canvas-based rendering (default, existing behavior).
        /// Each plot element is a separate WPF FrameworkElement on a Canvas.
        /// </summary>
        Canvas,

        /// <summary>
        /// DrawingVisual-based rendering (higher performance).
        /// All plot elements are drawn to a single DrawingVisual using DrawingContext,
        /// eliminating per-element WPF layout overhead.
        /// </summary>
        DrawingVisual
    }
}
