// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LinearBarSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.LinearBarSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.LinearBarSeries"/>.
    /// </summary>
    /// <remarks>
    /// A linear bar series displays bars at data point positions on a linear axis,
    /// unlike regular bar series which use categorical axes. This is useful for
    /// showing bars at arbitrary X positions.
    /// </remarks>
    public class LinearBarSeries : DataPointSeries
    {
        /// <summary>
        /// Identifies the <see cref="BarWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BarWidthProperty =
            DependencyProperty.Register(
                nameof(BarWidth),
                typeof(double),
                typeof(LinearBarSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="FillColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FillColorProperty =
            DependencyProperty.Register(
                nameof(FillColor),
                typeof(Color),
                typeof(LinearBarSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeColorProperty =
            DependencyProperty.Register(
                nameof(StrokeColor),
                typeof(Color),
                typeof(LinearBarSeries),
                new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(LinearBarSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="NegativeFillColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NegativeFillColorProperty =
            DependencyProperty.Register(
                nameof(NegativeFillColor),
                typeof(Color),
                typeof(LinearBarSeries),
                new PropertyMetadata(MoreColors.Undefined, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="NegativeStrokeColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NegativeStrokeColorProperty =
            DependencyProperty.Register(
                nameof(NegativeStrokeColor),
                typeof(Color),
                typeof(LinearBarSeries),
                new PropertyMetadata(MoreColors.Undefined, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearBarSeries"/> class.
        /// </summary>
        public LinearBarSeries()
        {
            this.InternalSeries = new OxyPlot.Series.LinearBarSeries();
        }

        /// <summary>
        /// Gets or sets the width of the bars in X-axis units.
        /// </summary>
        /// <value>The bar width. The default is <c>1.0</c>.</value>
        public double BarWidth
        {
            get => (double)this.GetValue(BarWidthProperty);
            set => this.SetValue(BarWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the fill color of the bars.
        /// </summary>
        /// <value>The fill color. The default is <see cref="MoreColors.Automatic"/>.</value>
        public Color FillColor
        {
            get => (Color)this.GetValue(FillColorProperty);
            set => this.SetValue(FillColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke color of the bar borders.
        /// </summary>
        /// <value>The stroke color. The default is <see cref="Colors.Black"/>.</value>
        public Color StrokeColor
        {
            get => (Color)this.GetValue(StrokeColorProperty);
            set => this.SetValue(StrokeColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness of the bar borders.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>1.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the fill color for bars with negative values.
        /// </summary>
        /// <value>The negative fill color. The default is <see cref="MoreColors.Undefined"/>.</value>
        public Color NegativeFillColor
        {
            get => (Color)this.GetValue(NegativeFillColorProperty);
            set => this.SetValue(NegativeFillColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke color for bars with negative values.
        /// </summary>
        /// <value>The negative stroke color. The default is <see cref="MoreColors.Undefined"/>.</value>
        public Color NegativeStrokeColor
        {
            get => (Color)this.GetValue(NegativeStrokeColorProperty);
            set => this.SetValue(NegativeStrokeColorProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.LinearBarSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.LinearBarSeries s)
            {
                s.BarWidth = this.BarWidth;
                s.FillColor = this.FillColor.ToOxyColor();
                s.StrokeColor = this.StrokeColor.ToOxyColor();
                s.StrokeThickness = this.StrokeThickness;
                s.NegativeFillColor = this.NegativeFillColor.ToOxyColor();
                s.NegativeStrokeColor = this.NegativeStrokeColor.ToOxyColor();
            }
        }
    }
}
