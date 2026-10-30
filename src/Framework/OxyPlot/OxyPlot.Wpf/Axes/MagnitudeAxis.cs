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
    using Avalonia;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.MagnitudeAxis"/>.
    /// </summary>
    /// <remarks>
    /// A magnitude axis is used for polar plots. It displays the radial coordinate
    /// from the center of the polar plot outward.
    /// XGoodFit port: the defaults WPF overrode for this class are overridden in the same way, and the base
    /// class's change callbacks still run too, as in WPF.
    /// </remarks>
    public class MagnitudeAxis : LinearAxis
    {
        /// <summary>
        /// Initializes static members of the <see cref="MagnitudeAxis"/> class.
        /// </summary>
        static MagnitudeAxis()
        {
            MajorGridlineStyleProperty.OverrideDefaultValue<MagnitudeAxis>(LineStyle.Solid);
            MinorGridlineStyleProperty.OverrideDefaultValue<MagnitudeAxis>(LineStyle.Solid);
            PositionProperty.OverrideDefaultValue<MagnitudeAxis>(AxisPosition.None);
            PositionProperty.Changed.AddClassHandler<MagnitudeAxis>(AppearanceChanged);
            IsPanEnabledProperty.OverrideDefaultValue<MagnitudeAxis>(false);
            IsZoomEnabledProperty.OverrideDefaultValue<MagnitudeAxis>(false);
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
