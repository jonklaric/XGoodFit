// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GumbelProbabilityAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.GumbelProbabilityAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.GumbelProbabilityAxis"/>.
    /// </summary>
    /// <remarks>
    /// A Gumbel probability axis transforms values using the inverse cumulative
    /// distribution function of the Gumbel distribution. This is useful for
    /// extreme value analysis and flood frequency studies where Gumbel-distributed
    /// data appears as a straight line.
    /// </remarks>
    public class GumbelProbabilityAxis : Axis
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GumbelProbabilityAxis"/> class.
        /// </summary>
        public GumbelProbabilityAxis()
        {
            this.InternalAxis = new Axes.GumbelProbabilityAxis();

            // Set up default exceedance probability plot configuration
            this.Minimum = 0.0000001;
            this.Maximum = 0.99;
            this.StartPosition = 1;
            this.EndPosition = 0;
            this.Title = "Exceedance Probability";
            this.TitleFontSize = 16;
            this.AxisTitleDistance = 15;
            this.FontSize = 12;
            this.MajorGridlineStyle = LineStyle.Solid;
            this.MinorGridlineStyle = LineStyle.None;
            this.TickStyle = Axes.TickStyle.None;
        }

        /// <summary>
        /// Creates the internal axis model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Axes.GumbelProbabilityAxis"/> instance.</returns>
        public override Axes.Axis CreateModel()
        {
            this.SynchronizeProperties();
            return this.InternalAxis;
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot axis.
        /// </summary>
        protected override void SynchronizeProperties()
        {
            base.SynchronizeProperties();
        }
    }
}
