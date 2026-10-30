// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataPointSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides an abstract base class for series that contain a collection of DataPoints.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using Avalonia;

    /// <summary>
    /// Provides an abstract base class for series that contain a collection of <see cref="DataPoint"/>s.
    /// </summary>
    /// <remarks>
    /// This class provides data binding support through <see cref="DataFieldX"/> and <see cref="DataFieldY"/>
    /// properties, or through a custom <see cref="Mapping"/> delegate.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor.
    /// </remarks>
    public abstract class DataPointSeries : XYAxisSeries
    {
        /// <summary>
        /// Identifies the <see cref="CanTrackerInterpolatePoints"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> CanTrackerInterpolatePointsProperty = AvaloniaProperty.Register<DataPointSeries, bool>(
            nameof(CanTrackerInterpolatePoints),
            false);

        /// <summary>
        /// Identifies the <see cref="DataFieldX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldXProperty = AvaloniaProperty.Register<DataPointSeries, string>(
            nameof(DataFieldX),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldYProperty = AvaloniaProperty.Register<DataPointSeries, string>(
            nameof(DataFieldY),
            null);

        /// <summary>
        /// Identifies the <see cref="Mapping"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Func<object, DataPoint>> MappingProperty = AvaloniaProperty.Register<DataPointSeries, Func<object, DataPoint>>(
            nameof(Mapping),
            null);

        /// <summary>
        /// Initializes static members of the <see cref="DataPointSeries" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static DataPointSeries()
        {
            CanTrackerInterpolatePointsProperty.Changed.AddClassHandler<DataPointSeries>(AppearanceChanged);
            DataFieldXProperty.Changed.AddClassHandler<DataPointSeries>(DataChanged);
            DataFieldYProperty.Changed.AddClassHandler<DataPointSeries>(DataChanged);
            MappingProperty.Changed.AddClassHandler<DataPointSeries>(DataChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPointSeries"/> class.
        /// </summary>
        protected DataPointSeries()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tracker can interpolate points.
        /// The default is <c>false</c>.
        /// </summary>
        /// <value>
        /// <c>true</c> if the tracker should interpolate between data points; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// When enabled, the tracker will show interpolated values between actual data points.
        /// </remarks>
        public bool CanTrackerInterpolatePoints
        {
            get => (bool)this.GetValue(CanTrackerInterpolatePointsProperty);
            set => this.SetValue(CanTrackerInterpolatePointsProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property that provides X values. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The name of the property on items in the <c>ItemsControl.ItemsSource</c>
        /// that provides the X coordinate values.
        /// </value>
        /// <remarks>
        /// This property is used when binding to a collection of custom objects.
        /// Both <see cref="DataFieldX"/> and <see cref="DataFieldY"/> must be set for data field binding to work.
        /// </remarks>
        public string DataFieldX
        {
            get => (string)this.GetValue(DataFieldXProperty);
            set => this.SetValue(DataFieldXProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property that provides Y values. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The name of the property on items in the <c>ItemsControl.ItemsSource</c>
        /// that provides the Y coordinate values.
        /// </value>
        /// <remarks>
        /// This property is used when binding to a collection of custom objects.
        /// Both <see cref="DataFieldX"/> and <see cref="DataFieldY"/> must be set for data field binding to work.
        /// </remarks>
        public string DataFieldY
        {
            get => (string)this.GetValue(DataFieldYProperty);
            set => this.SetValue(DataFieldYProperty, value);
        }

        /// <summary>
        /// Gets or sets a custom mapping function. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// A function that converts items from the <c>ItemsControl.ItemsSource</c>
        /// to <see cref="DataPoint"/> instances.
        /// </value>
        /// <example>
        /// <code>
        /// series.Mapping = item => new DataPoint(((MyType)item).Time, ((MyType)item).Value);
        /// </code>
        /// </example>
        public Func<object, DataPoint> Mapping
        {
            get => (Func<object, DataPoint>)this.GetValue(MappingProperty);
            set => this.SetValue(MappingProperty, value);
        }

        /// <summary>
        /// Synchronizes the wrapper properties to the internal OxyPlot series.
        /// </summary>
        /// <param name="s">The OxyPlot series to synchronize properties to.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series s)
        {
            base.SynchronizeProperties(s);

            if (s is OxyPlot.Series.DataPointSeries dataPointSeries)
            {
                dataPointSeries.CanTrackerInterpolatePoints = this.CanTrackerInterpolatePoints;
                dataPointSeries.DataFieldX = this.DataFieldX;
                dataPointSeries.DataFieldY = this.DataFieldY;
                dataPointSeries.Mapping = this.Mapping;
            }
        }
    }
}
