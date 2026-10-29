// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogarithmicAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.LogarithmicAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.LogarithmicAxis"/>.
    /// </summary>
    /// <remarks>
    /// A logarithmic axis displays values on a logarithmic scale. This is useful for
    /// visualizing data that spans multiple orders of magnitude.
    /// </remarks>
    public class LogarithmicAxis : Axis
    {
        /// <summary>
        /// Identifies the <see cref="Base"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BaseProperty = DependencyProperty.Register(
            nameof(Base),
            typeof(double),
            typeof(LogarithmicAxis),
            new PropertyMetadata(10.0, DataChanged));

        /// <summary>
        /// Identifies the <see cref="PowerPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PowerPaddingProperty = DependencyProperty.Register(
            nameof(PowerPadding),
            typeof(bool),
            typeof(LogarithmicAxis),
            new PropertyMetadata(true, DataChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="LogarithmicAxis"/> class.
        /// </summary>
        public LogarithmicAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.LogarithmicAxis();
            this.FilterMinValue = 0;
        }

        /// <summary>
        /// Gets or sets the logarithmic base. The default is <c>10</c>.
        /// </summary>
        /// <value>The base of the logarithm (e.g., 10 for common logarithm, Math.E for natural logarithm).</value>
        public double Base
        {
            get => (double)this.GetValue(BaseProperty);
            set => this.SetValue(BaseProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to pad the axis range to the nearest power of the base. The default is <c>true</c>.
        /// </summary>
        /// <value>
        /// <c>true</c> if the minimum and maximum should be padded to the nearest power of the base; otherwise, <c>false</c>.
        /// </value>
        public bool PowerPadding
        {
            get => (bool)this.GetValue(PowerPaddingProperty);
            set => this.SetValue(PowerPaddingProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Axes.LogarithmicAxis"/> model.</returns>
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

            if (this.InternalAxis is OxyPlot.Axes.LogarithmicAxis a)
            {
                a.Base = this.Base;
                a.PowerPadding = this.PowerPadding;
            }
        }
    }
}
