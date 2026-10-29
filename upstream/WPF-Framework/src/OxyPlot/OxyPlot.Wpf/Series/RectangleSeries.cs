// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RectangleSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.RectangleSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Windows;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.RectangleSeries"/>.
    /// </summary>
    /// <remarks>
    /// A rectangle series displays colored rectangles where the color is
    /// determined by a value mapped to a color axis. This is useful for
    /// showing categorical or continuous data on a 2D grid.
    /// </remarks>
    public class RectangleSeries : XYAxisSeries
    {
        /// <summary>
        /// Identifies the <see cref="CanTrackerInterpolatePoints"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanTrackerInterpolatePointsProperty =
            DependencyProperty.Register(
                nameof(CanTrackerInterpolatePoints),
                typeof(bool),
                typeof(RectangleSeries),
                new PropertyMetadata(false, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Mapping"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MappingProperty =
            DependencyProperty.Register(
                nameof(Mapping),
                typeof(Func<object, RectangleItem>),
                typeof(RectangleSeries),
                new UIPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="ColorAxisKey"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorAxisKeyProperty =
            DependencyProperty.Register(
                nameof(ColorAxisKey),
                typeof(string),
                typeof(RectangleSeries),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="LabelFormatString"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFormatStringProperty =
            DependencyProperty.Register(
                nameof(LabelFormatString),
                typeof(string),
                typeof(RectangleSeries),
                new PropertyMetadata("0.00", AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFontSizeProperty =
            DependencyProperty.Register(
                nameof(LabelFontSize),
                typeof(double),
                typeof(RectangleSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="RectangleSeries"/> class.
        /// </summary>
        static RectangleSeries()
        {
            TrackerFormatStringProperty.OverrideMetadata(
                typeof(RectangleSeries),
                new PropertyMetadata(OxyPlot.Series.RectangleSeries.DefaultTrackerFormatString, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleSeries"/> class.
        /// </summary>
        public RectangleSeries()
        {
            this.InternalSeries = new OxyPlot.Series.RectangleSeries();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tracker can interpolate points.
        /// </summary>
        /// <value><c>true</c> if interpolation is enabled; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool CanTrackerInterpolatePoints
        {
            get => (bool)this.GetValue(CanTrackerInterpolatePointsProperty);
            set => this.SetValue(CanTrackerInterpolatePointsProperty, value);
        }

        /// <summary>
        /// Gets or sets the mapping function from data items to rectangle items.
        /// </summary>
        /// <value>The mapping function. The default is <c>null</c>.</value>
        public Func<object, RectangleItem> Mapping
        {
            get => (Func<object, RectangleItem>)this.GetValue(MappingProperty);
            set => this.SetValue(MappingProperty, value);
        }

        /// <summary>
        /// Gets or sets the key of the color axis to use for coloring rectangles.
        /// </summary>
        /// <value>The color axis key. The default is <c>null</c>.</value>
        public string ColorAxisKey
        {
            get => (string)this.GetValue(ColorAxisKeyProperty);
            set => this.SetValue(ColorAxisKeyProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string for rectangle labels.
        /// </summary>
        /// <value>The label format string. The default is <c>"0.00"</c>.</value>
        public string LabelFormatString
        {
            get => (string)this.GetValue(LabelFormatStringProperty);
            set => this.SetValue(LabelFormatStringProperty, value);
        }

        /// <summary>
        /// Gets or sets the font size for rectangle labels.
        /// </summary>
        /// <value>The label font size. The default is <c>0.0</c> (no labels).</value>
        public double LabelFontSize
        {
            get => (double)this.GetValue(LabelFontSizeProperty);
            set => this.SetValue(LabelFontSizeProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.RectangleSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.RectangleSeries s)
            {
                s.ItemsSource = this.ItemsSource;
                s.CanTrackerInterpolatePoints = this.CanTrackerInterpolatePoints;
                s.Mapping = this.Mapping;
                s.ColorAxisKey = this.ColorAxisKey;
                s.LabelFormatString = this.LabelFormatString;
                s.LabelFontSize = this.LabelFontSize;
            }
        }
    }
}
