// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorBarSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.ErrorBarSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.ErrorBarSeries"/>.
    /// </summary>
    /// <remarks>
    /// An error bar series displays horizontal bars with error indicators,
    /// useful for showing uncertainty or variability in the data values.
    /// </remarks>
    public class ErrorBarSeries : BarSeriesBase<ErrorBarItem>
    {
        /// <summary>
        /// Identifies the <see cref="BarWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BarWidthProperty =
            DependencyProperty.Register(
                nameof(BarWidth),
                typeof(double),
                typeof(ErrorBarSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ErrorWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorWidthProperty =
            DependencyProperty.Register(
                nameof(ErrorWidth),
                typeof(double),
                typeof(ErrorBarSeries),
                new PropertyMetadata(0.4, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ErrorStrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorStrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(ErrorStrokeThickness),
                typeof(double),
                typeof(ErrorBarSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="ErrorBarSeries"/> class.
        /// </summary>
        static ErrorBarSeries()
        {
            TrackerFormatStringProperty.OverrideMetadata(
                typeof(ErrorBarSeries),
                new PropertyMetadata(OxyPlot.Series.ErrorBarSeries.DefaultTrackerFormatString, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorBarSeries"/> class.
        /// </summary>
        public ErrorBarSeries()
        {
            this.InternalSeries = new OxyPlot.Series.ErrorBarSeries();
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
        /// Gets or sets the width of the error end lines as a fraction of the bar width.
        /// </summary>
        /// <value>The error width (0-1). The default is <c>0.4</c>.</value>
        public double ErrorWidth
        {
            get => (double)this.GetValue(ErrorWidthProperty);
            set => this.SetValue(ErrorWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness of the error lines.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>1.0</c>.</value>
        public double ErrorStrokeThickness
        {
            get => (double)this.GetValue(ErrorStrokeThicknessProperty);
            set => this.SetValue(ErrorStrokeThicknessProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>An <see cref="OxyPlot.Series.ErrorBarSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.ErrorBarSeries s)
            {
                s.BarWidth = this.BarWidth;
                s.ErrorWidth = this.ErrorWidth;
                s.ErrorStrokeThickness = this.ErrorStrokeThickness;
            }
        }
    }
}
