// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StairStepSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.StairStepSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.StairStepSeries"/>.
    /// </summary>
    /// <remarks>
    /// A stair-step series renders data as horizontal and vertical line segments,
    /// creating a stepped appearance. This is useful for displaying discrete changes or step functions.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor.
    /// </remarks>
    public class StairStepSeries : LineSeries
    {
        /// <summary>
        /// Identifies the <see cref="VerticalLineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> VerticalLineStyleProperty = AvaloniaProperty.Register<StairStepSeries, LineStyle>(
            nameof(VerticalLineStyle),
            LineStyle.Automatic);

        /// <summary>
        /// Identifies the <see cref="VerticalStrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> VerticalStrokeThicknessProperty = AvaloniaProperty.Register<StairStepSeries, double>(
            nameof(VerticalStrokeThickness),
            double.NaN);

        /// <summary>
        /// Initializes static members of the <see cref="StairStepSeries" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static StairStepSeries()
        {
            VerticalLineStyleProperty.Changed.AddClassHandler<StairStepSeries>(AppearanceChanged);
            VerticalStrokeThicknessProperty.Changed.AddClassHandler<StairStepSeries>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StairStepSeries"/> class.
        /// </summary>
        public StairStepSeries()
        {
            this.InternalSeries = new OxyPlot.Series.StairStepSeries();
        }

        /// <summary>
        /// Gets or sets the line style for vertical line segments. The default is <see cref="OxyPlot.LineStyle.Automatic"/>.
        /// </summary>
        /// <value>The line style for vertical segments.</value>
        public LineStyle VerticalLineStyle
        {
            get => (LineStyle)this.GetValue(VerticalLineStyleProperty);
            set => this.SetValue(VerticalLineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness for vertical line segments. The default is <see cref="double.NaN"/>.
        /// </summary>
        /// <value>
        /// The thickness of vertical segments in pixels. Set to <see cref="double.NaN"/> to use the same
        /// thickness as horizontal segments (<see cref="LineSeries.StrokeThickness"/>).
        /// </value>
        /// <remarks>
        /// Using a separate vertical stroke thickness has a small performance impact.
        /// </remarks>
        public double VerticalStrokeThickness
        {
            get => (double)this.GetValue(VerticalStrokeThicknessProperty);
            set => this.SetValue(VerticalStrokeThicknessProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Series.StairStepSeries"/> model.</returns>
        public override OxyPlot.Series.Series CreateModel()
        {
            this.SynchronizeProperties(this.InternalSeries);
            return this.InternalSeries;
        }

        /// <summary>
        /// Synchronizes the WPF properties to the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal OxyPlot series to update.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.StairStepSeries s)
            {
                s.VerticalLineStyle = this.VerticalLineStyle;
                s.VerticalStrokeThickness = this.VerticalStrokeThickness;
            }
        }
    }
}
