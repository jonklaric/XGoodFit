// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThreeColorLineSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.ThreeColorLineSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.ThreeColorLineSeries"/>.
    /// </summary>
    /// <remarks>
    /// A three-color line series displays a line that changes between three colors
    /// based on high and low limit values. Values above the high limit use one color,
    /// values below the low limit use another, and values in between use the base color.
    /// </remarks>
    public class ThreeColorLineSeries : LineSeries
    {
        /// <summary>
        /// Identifies the <see cref="ColorLo"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorLoProperty =
            DependencyProperty.Register(
                nameof(ColorLo),
                typeof(Color),
                typeof(ThreeColorLineSeries),
                new UIPropertyMetadata(Colors.Blue, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ColorHi"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorHiProperty =
            DependencyProperty.Register(
                nameof(ColorHi),
                typeof(Color),
                typeof(ThreeColorLineSeries),
                new UIPropertyMetadata(Colors.Red, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LimitLo"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LimitLoProperty =
            DependencyProperty.Register(
                nameof(LimitLo),
                typeof(double),
                typeof(ThreeColorLineSeries),
                new UIPropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LimitHi"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LimitHiProperty =
            DependencyProperty.Register(
                nameof(LimitHi),
                typeof(double),
                typeof(ThreeColorLineSeries),
                new UIPropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineStyleLo"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStyleLoProperty =
            DependencyProperty.Register(
                nameof(LineStyleLo),
                typeof(LineStyle),
                typeof(ThreeColorLineSeries),
                new UIPropertyMetadata(LineStyle.Solid, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineStyleHi"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStyleHiProperty =
            DependencyProperty.Register(
                nameof(LineStyleHi),
                typeof(LineStyle),
                typeof(ThreeColorLineSeries),
                new UIPropertyMetadata(LineStyle.Solid, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeColorLineSeries"/> class.
        /// </summary>
        public ThreeColorLineSeries()
        {
            this.InternalSeries = new OxyPlot.Series.ThreeColorLineSeries();
        }

        /// <summary>
        /// Gets or sets the color for the line below the low limit.
        /// </summary>
        /// <value>The low color. The default is <see cref="Colors.Blue"/>.</value>
        public Color ColorLo
        {
            get => (Color)this.GetValue(ColorLoProperty);
            set => this.SetValue(ColorLoProperty, value);
        }

        /// <summary>
        /// Gets or sets the color for the line above the high limit.
        /// </summary>
        /// <value>The high color. The default is <see cref="Colors.Red"/>.</value>
        public Color ColorHi
        {
            get => (Color)this.GetValue(ColorHiProperty);
            set => this.SetValue(ColorHiProperty, value);
        }

        /// <summary>
        /// Gets or sets the low limit value.
        /// </summary>
        /// <value>The low limit. The default is <c>0.0</c>.</value>
        public double LimitLo
        {
            get => (double)this.GetValue(LimitLoProperty);
            set => this.SetValue(LimitLoProperty, value);
        }

        /// <summary>
        /// Gets or sets the high limit value.
        /// </summary>
        /// <value>The high limit. The default is <c>0.0</c>.</value>
        public double LimitHi
        {
            get => (double)this.GetValue(LimitHiProperty);
            set => this.SetValue(LimitHiProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style for the portion below the low limit.
        /// </summary>
        /// <value>The low line style. The default is <see cref="LineStyle.Solid"/>.</value>
        public LineStyle LineStyleLo
        {
            get => (LineStyle)this.GetValue(LineStyleLoProperty);
            set => this.SetValue(LineStyleLoProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style for the portion above the high limit.
        /// </summary>
        /// <value>The high line style. The default is <see cref="LineStyle.Solid"/>.</value>
        public LineStyle LineStyleHi
        {
            get => (LineStyle)this.GetValue(LineStyleHiProperty);
            set => this.SetValue(LineStyleHiProperty, value);
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal series to synchronize.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.ThreeColorLineSeries s)
            {
                s.LimitLo = this.LimitLo;
                s.ColorLo = this.ColorLo.ToOxyColor();
                s.LineStyleLo = this.LineStyleLo;
                s.LimitHi = this.LimitHi;
                s.ColorHi = this.ColorHi.ToOxyColor();
                s.LineStyleHi = this.LineStyleHi;
            }
        }
    }
}
