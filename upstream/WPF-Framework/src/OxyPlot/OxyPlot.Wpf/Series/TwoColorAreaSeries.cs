// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TwoColorAreaSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.TwoColorAreaSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.TwoColorAreaSeries"/>.
    /// </summary>
    /// <remarks>
    /// A two-color area series displays an area chart that changes fill color
    /// based on whether values are above or below a specified limit value.
    /// </remarks>
    public class TwoColorAreaSeries : AreaSeries
    {
        /// <summary>
        /// Identifies the <see cref="Dashes2"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Dashes2Property =
            DependencyProperty.Register(
                nameof(Dashes2),
                typeof(DoubleCollection),
                typeof(TwoColorAreaSeries),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Fill2"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Fill2Property =
            DependencyProperty.Register(
                nameof(Fill2),
                typeof(Color),
                typeof(TwoColorAreaSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineStyle2"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStyle2Property =
            DependencyProperty.Register(
                nameof(LineStyle2),
                typeof(LineStyle),
                typeof(TwoColorAreaSeries),
                new PropertyMetadata(LineStyle.Solid, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Limit"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LimitProperty =
            DependencyProperty.Register(
                nameof(Limit),
                typeof(double),
                typeof(TwoColorAreaSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MarkerFill2"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkerFill2Property =
            DependencyProperty.Register(
                nameof(MarkerFill2),
                typeof(Color),
                typeof(TwoColorAreaSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MarkerStroke2"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarkerStroke2Property =
            DependencyProperty.Register(
                nameof(MarkerStroke2),
                typeof(Color),
                typeof(TwoColorAreaSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoColorAreaSeries"/> class.
        /// </summary>
        public TwoColorAreaSeries()
        {
            this.InternalSeries = new OxyPlot.Series.TwoColorAreaSeries();
        }

        /// <summary>
        /// Gets or sets the dash array for the line below the limit.
        /// </summary>
        /// <value>The dash array. The default is <c>null</c>.</value>
        public DoubleCollection Dashes2
        {
            get => (DoubleCollection)this.GetValue(Dashes2Property);
            set => this.SetValue(Dashes2Property, value);
        }

        /// <summary>
        /// Gets or sets the fill color for the area below the limit.
        /// </summary>
        /// <value>The second fill color. The default is <see cref="MoreColors.Automatic"/>.</value>
        public Color Fill2
        {
            get => (Color)this.GetValue(Fill2Property);
            set => this.SetValue(Fill2Property, value);
        }

        /// <summary>
        /// Gets or sets the marker fill color for points below the limit.
        /// </summary>
        /// <value>The second marker fill color. The default is <see cref="MoreColors.Automatic"/>.</value>
        public Color MarkerFill2
        {
            get => (Color)this.GetValue(MarkerFill2Property);
            set => this.SetValue(MarkerFill2Property, value);
        }

        /// <summary>
        /// Gets or sets the marker stroke color for points below the limit.
        /// </summary>
        /// <value>The second marker stroke color. The default is <see cref="MoreColors.Automatic"/>.</value>
        public Color MarkerStroke2
        {
            get => (Color)this.GetValue(MarkerStroke2Property);
            set => this.SetValue(MarkerStroke2Property, value);
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
        /// Gets or sets the limit value that determines when to switch colors.
        /// </summary>
        /// <value>The limit value. The default is <c>0.0</c>.</value>
        public double Limit
        {
            get => (double)this.GetValue(LimitProperty);
            set => this.SetValue(LimitProperty, value);
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal series to synchronize.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.TwoColorAreaSeries s)
            {
                s.Fill = this.Fill.ToOxyColor();
                s.Fill2 = this.Fill2.ToOxyColor();
                s.MarkerFill2 = this.MarkerFill2.ToOxyColor();
                s.MarkerStroke2 = this.MarkerStroke2.ToOxyColor();
                s.Limit = this.Limit;
                s.Dashes2 = this.Dashes2?.ToArray();
                s.LineStyle2 = this.LineStyle2;
            }
        }
    }
}
