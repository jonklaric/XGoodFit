// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AngleAxisFullPlotArea.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.AngleAxisFullPlotArea.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using OxyPlot.Axes;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.AngleAxisFullPlotArea"/>.
    /// </summary>
    /// <remarks>
    /// An angle axis that covers the whole plot area, used in conjunction with
    /// <see cref="MagnitudeAxisFullPlotArea"/> for polar plots that can pan within the plot area.
    /// </remarks>
    public class AngleAxisFullPlotArea : AngleAxis
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AngleAxisFullPlotArea"/> class.
        /// </summary>
        public AngleAxisFullPlotArea()
        {
            this.InternalAxis = new OxyPlot.Axes.AngleAxisFullPlotArea();
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Axes.AngleAxisFullPlotArea"/> model.</returns>
        public override OxyPlot.Axes.Axis CreateModel()
        {
            this.SynchronizeProperties();
            return this.InternalAxis;
        }
    }
}
