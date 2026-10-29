// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StemSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.StemSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.StemSeries"/>.
    /// </summary>
    /// <remarks>
    /// A stem series displays discrete data points as vertical lines (stems)
    /// from a base value to the data point, commonly used for signal processing visualization.
    /// </remarks>
    public class StemSeries : LineSeries
    {
        /// <summary>
        /// Identifies the <see cref="Base"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BaseProperty =
            DependencyProperty.Register(
                nameof(Base),
                typeof(double),
                typeof(StemSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="StemSeries"/> class.
        /// </summary>
        public StemSeries()
        {
            this.InternalSeries = new OxyPlot.Series.StemSeries();
        }

        /// <summary>
        /// Gets or sets the base value where stems originate.
        /// </summary>
        /// <value>The base value. The default is <c>0.0</c>.</value>
        public double Base
        {
            get => (double)this.GetValue(BaseProperty);
            set => this.SetValue(BaseProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.StemSeries"/> instance.</returns>
        public override OxyPlot.Series.Series CreateModel()
        {
            this.SynchronizeProperties(this.InternalSeries);
            return this.InternalSeries;
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal series to synchronize.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.StemSeries s)
            {
                s.Base = this.Base;
            }
        }
    }
}
