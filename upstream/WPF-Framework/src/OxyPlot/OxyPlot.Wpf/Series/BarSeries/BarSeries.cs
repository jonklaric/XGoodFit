// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BarSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.BarSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.BarSeries"/>.
    /// </summary>
    /// <remarks>
    /// A bar series displays horizontal bars, typically used with a CategoryAxis
    /// on the Y-axis to show values for different categories.
    /// </remarks>
    public class BarSeries : BarSeriesBase<BarItem>
    {
        /// <summary>
        /// Identifies the <see cref="BarWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BarWidthProperty =
            DependencyProperty.Register(
                nameof(BarWidth),
                typeof(double),
                typeof(BarSeries),
                new PropertyMetadata(1.0, DataChanged));

        /// <summary>
        /// Initializes static members of the <see cref="BarSeries"/> class.
        /// </summary>
        static BarSeries()
        {
            TrackerFormatStringProperty.OverrideMetadata(
                typeof(BarSeries),
                new PropertyMetadata(OxyPlot.Series.BarSeries.DefaultTrackerFormatString, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BarSeries"/> class.
        /// </summary>
        public BarSeries()
        {
            this.InternalSeries = new OxyPlot.Series.BarSeries();
        }

        /// <summary>
        /// Gets or sets the width of the bars as a fraction of the available space.
        /// </summary>
        /// <value>The bar width (0-1). The default is <c>1.0</c>.</value>
        public double BarWidth
        {
            get => (double)this.GetValue(BarWidthProperty);
            set => this.SetValue(BarWidthProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>An <see cref="OxyPlot.Series.BarSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.BarSeries s)
            {
                s.BarWidth = this.BarWidth;

                // Synchronize properties from WPF BarSeriesBase that are specific to BarSeries
                // (these properties exist on OxyPlot.Series.BarSeries, not on the generic BarSeriesBase<T>)
                s.BaseValue = this.BaseValue;
                s.ColorField = this.ColorField;
                s.FillColor = this.FillColor.ToOxyColor();
                s.IsStacked = this.IsStacked;
                s.NegativeFillColor = this.NegativeFillColor.ToOxyColor();
                s.StackGroup = this.StackGroup;
                s.ValueField = this.ValueField;
                s.LabelFormatString = this.LabelFormatString;
                s.LabelMargin = this.LabelMargin;
                s.LabelPlacement = this.LabelPlacement;
                s.StrokeColor = this.StrokeColor.ToOxyColor();
                s.StrokeThickness = this.StrokeThickness;
            }
        }
    }
}
