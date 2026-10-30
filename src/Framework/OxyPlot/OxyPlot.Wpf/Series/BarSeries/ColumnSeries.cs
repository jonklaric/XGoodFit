// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColumnSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.ColumnSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.ColumnSeries"/>.
    /// </summary>
    /// <remarks>
    /// A column series displays vertical bars, typically used with a CategoryAxis
    /// on the X-axis to show values for different categories.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF.
    /// </remarks>
    public class ColumnSeries : BarSeriesBase<ColumnItem>
    {
        /// <summary>
        /// Identifies the <see cref="ColumnWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ColumnWidthProperty = AvaloniaProperty.Register<ColumnSeries, double>(
            nameof(ColumnWidth),
            1.0);

        /// <summary>
        /// Initializes static members of the <see cref="ColumnSeries"/> class.
        /// </summary>
        static ColumnSeries()
        {
            ColumnWidthProperty.Changed.AddClassHandler<ColumnSeries>(DataChanged);
            TrackerFormatStringProperty.OverrideDefaultValue<ColumnSeries>(OxyPlot.Series.ColumnSeries.DefaultTrackerFormatString);
            TrackerFormatStringProperty.Changed.AddClassHandler<ColumnSeries>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnSeries"/> class.
        /// </summary>
        public ColumnSeries()
        {
            this.InternalSeries = new OxyPlot.Series.ColumnSeries();
        }

        /// <summary>
        /// Gets or sets the width of the columns as a fraction of the available space.
        /// </summary>
        /// <value>The column width (0-1). The default is <c>1.0</c>.</value>
        public double ColumnWidth
        {
            get => (double)this.GetValue(ColumnWidthProperty);
            set => this.SetValue(ColumnWidthProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>An <see cref="OxyPlot.Series.ColumnSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.ColumnSeries s)
            {
                s.ColumnWidth = this.ColumnWidth;

                // Synchronize properties from WPF BarSeriesBase that are specific to ColumnSeries
                // (these properties exist on OxyPlot.Series.ColumnSeries, not on the generic BarSeriesBase<T>)
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
