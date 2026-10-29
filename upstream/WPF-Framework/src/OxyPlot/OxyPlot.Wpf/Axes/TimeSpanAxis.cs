// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimeSpanAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.TimeSpanAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.TimeSpanAxis"/>.
    /// </summary>
    /// <remarks>
    /// A time span axis displays TimeSpan values. The axis values are internally stored as
    /// the total number of days in the TimeSpan.
    /// </remarks>
    public class TimeSpanAxis : Axis
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanAxis"/> class.
        /// </summary>
        public TimeSpanAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.TimeSpanAxis();
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Axes.TimeSpanAxis"/> model.</returns>
        public override OxyPlot.Axes.Axis CreateModel()
        {
            this.SynchronizeProperties();
            return this.InternalAxis;
        }

        /// <summary>
        /// Synchronizes the WPF properties to the internal OxyPlot axis.
        /// </summary>
        protected override void SynchronizeProperties()
        {
            base.SynchronizeProperties();
        }
    }
}
