// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TwoColorLineSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.TwoColorLineSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.TwoColorLineSeries"/>.
    /// </summary>
    /// <remarks>
    /// A two-color line series displays a line that changes color based on
    /// whether values are above or below a specified limit value.
    /// </remarks>
    public class TwoColorLineSeries : LineSeries
    {
        /// <summary>
        /// Identifies the <see cref="Color2"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Color2Property =
            DependencyProperty.Register(
                nameof(Color2),
                typeof(Color),
                typeof(TwoColorLineSeries),
                new UIPropertyMetadata(Colors.Blue, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Limit"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LimitProperty =
            DependencyProperty.Register(
                nameof(Limit),
                typeof(double),
                typeof(TwoColorLineSeries),
                new UIPropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineStyle2"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStyle2Property =
            DependencyProperty.Register(
                nameof(LineStyle2),
                typeof(LineStyle),
                typeof(TwoColorLineSeries),
                new UIPropertyMetadata(LineStyle.Solid, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoColorLineSeries"/> class.
        /// </summary>
        public TwoColorLineSeries()
        {
            this.InternalSeries = new OxyPlot.Series.TwoColorLineSeries();
        }

        /// <summary>
        /// Gets or sets the color for the line below the limit value.
        /// </summary>
        /// <value>The second color. The default is <see cref="Colors.Blue"/>.</value>
        public Color Color2
        {
            get => (Color)this.GetValue(Color2Property);
            set => this.SetValue(Color2Property, value);
        }

        /// <summary>
        /// Gets or sets the limit value that determines when to switch colors.
        /// </summary>
        /// <value>The limit value. The default is <c>0.0</c>.</value>
        public double Limit
        {
            get => (double)this.GetValue(LimitProperty);
            set => this.SetValue(LimitProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style for the portion below the limit.
        /// </summary>
        /// <value>The second line style. The default is <see cref="LineStyle.Solid"/>.</value>
        public LineStyle LineStyle2
        {
            get => (LineStyle)this.GetValue(LineStyle2Property);
            set => this.SetValue(LineStyle2Property, value);
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal series to synchronize.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.TwoColorLineSeries s)
            {
                s.Limit = this.Limit;
                s.Color2 = this.Color2.ToOxyColor();
                s.LineStyle2 = this.LineStyle2;
            }
        }
    }
}
