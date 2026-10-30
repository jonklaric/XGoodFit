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
    using Avalonia;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.AngleAxis"/>.
    /// </summary>
    /// <remarks>
    /// An angle axis is used for polar plots. It displays the angular coordinate
    /// around the circumference of the polar plot.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF.
    /// </remarks>
    public class AngleAxis : LinearAxis
    {
        /// <summary>
        /// Identifies the <see cref="StartAngle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StartAngleProperty = AvaloniaProperty.Register<AngleAxis, double>(
            nameof(StartAngle),
            0.0);

        /// <summary>
        /// Identifies the <see cref="EndAngle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> EndAngleProperty = AvaloniaProperty.Register<AngleAxis, double>(
            nameof(EndAngle),
            360.0);

        /// <summary>
        /// Initializes static members of the <see cref="AngleAxis"/> class.
        /// </summary>
        static AngleAxis()
        {
            StartAngleProperty.Changed.AddClassHandler<AngleAxis>(AppearanceChanged);
            EndAngleProperty.Changed.AddClassHandler<AngleAxis>(AppearanceChanged);
            MajorGridlineStyleProperty.OverrideDefaultValue<AngleAxis>(LineStyle.Solid);
            MinorGridlineStyleProperty.OverrideDefaultValue<AngleAxis>(LineStyle.Solid);
            PositionProperty.OverrideDefaultValue<AngleAxis>(AxisPosition.None);
            PositionProperty.Changed.AddClassHandler<AngleAxis>(AppearanceChanged);
            TickStyleProperty.OverrideDefaultValue<AngleAxis>(TickStyle.None);
            TickStyleProperty.Changed.AddClassHandler<AngleAxis>(AppearanceChanged);
            IsPanEnabledProperty.OverrideDefaultValue<AngleAxis>(false);
            IsZoomEnabledProperty.OverrideDefaultValue<AngleAxis>(false);
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
