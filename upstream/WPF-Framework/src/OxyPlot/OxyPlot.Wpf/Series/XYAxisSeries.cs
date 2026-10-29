// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XYAxisSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides an abstract base class for series that are related to X and Y axes.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    /// <summary>
    /// Provides an abstract base class for series that are related to X and Y axes.
    /// </summary>
    /// <remarks>
    /// This class provides properties for specifying which axes the series should use
    /// through <see cref="XAxisKey"/> and <see cref="YAxisKey"/> properties.
    /// </remarks>
    public abstract class XYAxisSeries : ItemsSeries
    {
        /// <summary>
        /// Identifies the <see cref="XAxisKey"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XAxisKeyProperty = DependencyProperty.Register(
            nameof(XAxisKey),
            typeof(string),
            typeof(XYAxisSeries),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="YAxisKey"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YAxisKeyProperty = DependencyProperty.Register(
            nameof(YAxisKey),
            typeof(string),
            typeof(XYAxisSeries),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="XYAxisSeries"/> class.
        /// </summary>
        protected XYAxisSeries()
        {
        }

        /// <summary>
        /// Gets or sets the X axis key. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The key of the X axis to use for this series. If <c>null</c>, the default X axis is used.
        /// </value>
        /// <remarks>
        /// This key should match the <c>Key</c> property of an axis in the plot's axis collection.
        /// </remarks>
        public string XAxisKey
        {
            get => (string)this.GetValue(XAxisKeyProperty);
            set => this.SetValue(XAxisKeyProperty, value);
        }

        /// <summary>
        /// Gets or sets the Y axis key. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The key of the Y axis to use for this series. If <c>null</c>, the default Y axis is used.
        /// </value>
        /// <remarks>
        /// This key should match the <c>Key</c> property of an axis in the plot's axis collection.
        /// </remarks>
        public string YAxisKey
        {
            get => (string)this.GetValue(YAxisKeyProperty);
            set => this.SetValue(YAxisKeyProperty, value);
        }

        /// <summary>
        /// Synchronizes the wrapper properties to the internal OxyPlot series.
        /// </summary>
        /// <param name="s">The OxyPlot series to synchronize properties to.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series s)
        {
            base.SynchronizeProperties(s);

            if (s is OxyPlot.Series.XYAxisSeries xyAxisSeries)
            {
                xyAxisSeries.XAxisKey = this.XAxisKey;
                xyAxisSeries.YAxisKey = this.YAxisKey;
            }
        }
    }
}
