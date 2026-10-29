// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AngleAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.AngleAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.AngleAxis"/>.
    /// </summary>
    /// <remarks>
    /// An angle axis is used for polar plots. It displays the angular coordinate
    /// around the circumference of the polar plot.
    /// </remarks>
    public class AngleAxis : LinearAxis
    {
        /// <summary>
        /// Identifies the <see cref="StartAngle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartAngleProperty = DependencyProperty.Register(
            nameof(StartAngle),
            typeof(double),
            typeof(AngleAxis),
            new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="EndAngle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EndAngleProperty = DependencyProperty.Register(
            nameof(EndAngle),
            typeof(double),
            typeof(AngleAxis),
            new PropertyMetadata(360.0, AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="AngleAxis"/> class.
        /// </summary>
        static AngleAxis()
        {
            MajorGridlineStyleProperty.OverrideMetadata(typeof(AngleAxis), new PropertyMetadata(LineStyle.Solid));
            MinorGridlineStyleProperty.OverrideMetadata(typeof(AngleAxis), new PropertyMetadata(LineStyle.Solid));
            PositionProperty.OverrideMetadata(typeof(AngleAxis), new PropertyMetadata(AxisPosition.None, AppearanceChanged));
            TickStyleProperty.OverrideMetadata(typeof(AngleAxis), new PropertyMetadata(TickStyle.None, AppearanceChanged));
            IsPanEnabledProperty.OverrideMetadata(typeof(AngleAxis), new PropertyMetadata(false));
            IsZoomEnabledProperty.OverrideMetadata(typeof(AngleAxis), new PropertyMetadata(false));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AngleAxis"/> class.
        /// </summary>
        public AngleAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.AngleAxis();
        }

        /// <summary>
        /// Gets or sets the start angle in degrees. The default is <c>0</c>.
        /// </summary>
        /// <value>The angle in degrees where the axis starts (typically 0 = right, 90 = top).</value>
        public double StartAngle
        {
            get => (double)this.GetValue(StartAngleProperty);
            set => this.SetValue(StartAngleProperty, value);
        }

        /// <summary>
        /// Gets or sets the end angle in degrees. The default is <c>360</c>.
        /// </summary>
        /// <value>The angle in degrees where the axis ends.</value>
        public double EndAngle
        {
            get => (double)this.GetValue(EndAngleProperty);
            set => this.SetValue(EndAngleProperty, value);
        }

        /// <summary>
        /// Synchronizes the WPF properties to the internal OxyPlot axis.
        /// </summary>
        protected override void SynchronizeProperties()
        {
            base.SynchronizeProperties();

            if (this.InternalAxis is OxyPlot.Axes.AngleAxis a)
            {
                a.StartAngle = this.StartAngle;
                a.EndAngle = this.EndAngle;
            }
        }
    }
}
