// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MagnitudeAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.MagnitudeAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.MagnitudeAxis"/>.
    /// </summary>
    /// <remarks>
    /// A magnitude axis is used for polar plots. It displays the radial coordinate
    /// from the center of the polar plot outward.
    /// </remarks>
    public class MagnitudeAxis : LinearAxis
    {
        /// <summary>
        /// Initializes static members of the <see cref="MagnitudeAxis"/> class.
        /// </summary>
        static MagnitudeAxis()
        {
            MajorGridlineStyleProperty.OverrideMetadata(typeof(MagnitudeAxis), new PropertyMetadata(LineStyle.Solid));
            MinorGridlineStyleProperty.OverrideMetadata(typeof(MagnitudeAxis), new PropertyMetadata(LineStyle.Solid));
            PositionProperty.OverrideMetadata(typeof(MagnitudeAxis), new PropertyMetadata(AxisPosition.None, AppearanceChanged));
            IsPanEnabledProperty.OverrideMetadata(typeof(MagnitudeAxis), new PropertyMetadata(false));
            IsZoomEnabledProperty.OverrideMetadata(typeof(MagnitudeAxis), new PropertyMetadata(false));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MagnitudeAxis"/> class.
        /// </summary>
        public MagnitudeAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.MagnitudeAxis();
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
