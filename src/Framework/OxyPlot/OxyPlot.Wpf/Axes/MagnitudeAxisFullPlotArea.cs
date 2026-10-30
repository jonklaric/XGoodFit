// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MagnitudeAxisFullPlotArea.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.MagnitudeAxisFullPlotArea.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.MagnitudeAxisFullPlotArea"/>.
    /// </summary>
    /// <remarks>
    /// A magnitude axis that covers the whole plot area, allowing the polar plot
    /// center to be shifted within the plot area.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF.
    /// </remarks>
    public class MagnitudeAxisFullPlotArea : MagnitudeAxis
    {
        /// <summary>
        /// Identifies the <see cref="MidshiftH"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MidshiftHProperty = AvaloniaProperty.Register<MagnitudeAxisFullPlotArea, double>(
            nameof(MidshiftH),
            0.0);

        /// <summary>
        /// Identifies the <see cref="MidshiftV"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MidshiftVProperty = AvaloniaProperty.Register<MagnitudeAxisFullPlotArea, double>(
            nameof(MidshiftV),
            0.0);

        /// <summary>
        /// Initializes static members of the <see cref="MagnitudeAxisFullPlotArea"/> class.
        /// </summary>
        static MagnitudeAxisFullPlotArea()
        {
            MidshiftHProperty.Changed.AddClassHandler<MagnitudeAxisFullPlotArea>(AppearanceChanged);
            MidshiftVProperty.Changed.AddClassHandler<MagnitudeAxisFullPlotArea>(AppearanceChanged);
            IsPanEnabledProperty.OverrideDefaultValue<MagnitudeAxisFullPlotArea>(true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MagnitudeAxisFullPlotArea"/> class.
        /// </summary>
        public MagnitudeAxisFullPlotArea()
        {
            this.InternalAxis = new OxyPlot.Axes.MagnitudeAxisFullPlotArea();
        }

        /// <summary>
        /// Gets or sets the horizontal shift of the center as a fraction of the plot width.
        /// </summary>
        /// <value>The horizontal shift (-0.5 to 0.5). The default is <c>0.0</c>.</value>
        /// <remarks>
        /// A value of -0.5 shifts the center to the left edge, 0.5 to the right edge.
        /// </remarks>
        public double MidshiftH
        {
            get => (double)this.GetValue(MidshiftHProperty);
            set => this.SetValue(MidshiftHProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical shift of the center as a fraction of the plot height.
        /// </summary>
        /// <value>The vertical shift (-0.5 to 0.5). The default is <c>0.0</c>.</value>
        /// <remarks>
        /// A value of -0.5 shifts the center to the top edge, 0.5 to the bottom edge.
        /// </remarks>
        public double MidshiftV
        {
            get => (double)this.GetValue(MidshiftVProperty);
            set => this.SetValue(MidshiftVProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Axes.MagnitudeAxisFullPlotArea"/> model.</returns>
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

            if (this.InternalAxis is OxyPlot.Axes.MagnitudeAxisFullPlotArea a)
            {
                a.MidshiftH = this.MidshiftH;
                a.MidshiftV = this.MidshiftV;
            }
        }
    }
}
