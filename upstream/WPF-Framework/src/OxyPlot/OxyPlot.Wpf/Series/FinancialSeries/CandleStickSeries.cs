// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CandleStickSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.CandleStickSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.CandleStickSeries"/>.
    /// </summary>
    /// <remarks>
    /// A candlestick series displays financial data as candlesticks with colored bodies
    /// indicating whether the close price was higher or lower than the open price.
    /// </remarks>
    public class CandleStickSeries : HighLowSeries
    {
        /// <summary>
        /// Identifies the <see cref="IncreasingColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IncreasingColorProperty =
            DependencyProperty.Register(
                nameof(IncreasingColor),
                typeof(Color),
                typeof(CandleStickSeries),
                new PropertyMetadata(Colors.DarkGreen, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="DecreasingColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DecreasingColorProperty =
            DependencyProperty.Register(
                nameof(DecreasingColor),
                typeof(Color),
                typeof(CandleStickSeries),
                new PropertyMetadata(Colors.Red, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="CandleWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CandleWidthProperty =
            DependencyProperty.Register(
                nameof(CandleWidth),
                typeof(double),
                typeof(CandleStickSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="CandleStickSeries"/> class.
        /// </summary>
        public CandleStickSeries()
        {
            this.InternalSeries = new OxyPlot.Series.CandleStickSeries();
        }

        /// <summary>
        /// Gets or sets the color for increasing (bullish) candles.
        /// </summary>
        /// <value>The increasing color. The default is <see cref="Colors.DarkGreen"/>.</value>
        public Color IncreasingColor
        {
            get => (Color)this.GetValue(IncreasingColorProperty);
            set => this.SetValue(IncreasingColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the color for decreasing (bearish) candles.
        /// </summary>
        /// <value>The decreasing color. The default is <see cref="Colors.Red"/>.</value>
        public Color DecreasingColor
        {
            get => (Color)this.GetValue(DecreasingColorProperty);
            set => this.SetValue(DecreasingColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the candlestick bodies in data units.
        /// </summary>
        /// <value>The candle width. The default is <c>0.0</c> (auto-calculated).</value>
        public double CandleWidth
        {
            get => (double)this.GetValue(CandleWidthProperty);
            set => this.SetValue(CandleWidthProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.CandleStickSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.CandleStickSeries s)
            {
                s.IncreasingColor = this.IncreasingColor.ToOxyColor();
                s.DecreasingColor = this.DecreasingColor.ToOxyColor();
                s.CandleWidth = this.CandleWidth;
            }
        }
    }
}
