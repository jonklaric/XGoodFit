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
    using Avalonia;
    using Avalonia.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.LinearBarSeries"/>.
    /// </summary>
    /// <remarks>
    /// A linear bar series displays bars at data point positions on a linear axis,
    /// unlike regular bar series which use categorical axes. This is useful for
    /// showing bars at arbitrary X positions.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor.
    /// </remarks>
    public class LinearBarSeries : DataPointSeries
    {
        /// <summary>
        /// Identifies the <see cref="BarWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> BarWidthProperty = AvaloniaProperty.Register<LinearBarSeries, double>(
            nameof(BarWidth),
            1.0);

        /// <summary>
        /// Identifies the <see cref="FillColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> FillColorProperty = AvaloniaProperty.Register<LinearBarSeries, Color>(
            nameof(FillColor),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="StrokeColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> StrokeColorProperty = AvaloniaProperty.Register<LinearBarSeries, Color>(
            nameof(StrokeColor),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<LinearBarSeries, double>(
            nameof(StrokeThickness),
            1.0);

        /// <summary>
        /// Identifies the <see cref="NegativeFillColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> NegativeFillColorProperty = AvaloniaProperty.Register<LinearBarSeries, Color>(
            nameof(NegativeFillColor),
            MoreColors.Undefined);

        /// <summary>
        /// Identifies the <see cref="NegativeStrokeColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> NegativeStrokeColorProperty = AvaloniaProperty.Register<LinearBarSeries, Color>(
            nameof(NegativeStrokeColor),
            MoreColors.Undefined);

        /// <summary>
        /// Initializes static members of the <see cref="LinearBarSeries" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static LinearBarSeries()
        {
            BarWidthProperty.Changed.AddClassHandler<LinearBarSeries>(AppearanceChanged);
            FillColorProperty.Changed.AddClassHandler<LinearBarSeries>(AppearanceChanged);
            StrokeColorProperty.Changed.AddClassHandler<LinearBarSeries>(AppearanceChanged);
            StrokeThicknessProperty.Changed.AddClassHandler<LinearBarSeries>(AppearanceChanged);
            NegativeFillColorProperty.Changed.AddClassHandler<LinearBarSeries>(AppearanceChanged);
            NegativeStrokeColorProperty.Changed.AddClassHandler<LinearBarSeries>(AppearanceChanged);
        }

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
