// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtrapolationLineSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.ExtrapolationLineSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Collections.Generic;
    using Avalonia;
    using Avalonia.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.ExtrapolationLineSeries"/>.
    /// </summary>
    /// <remarks>
    /// An extrapolation line series renders portions of the line with different styles
    /// based on defined intervals, useful for distinguishing interpolated from extrapolated data.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF.
    /// </remarks>
    public class ExtrapolationLineSeries : LineSeries
    {
        /// <summary>
        /// Identifies the <see cref="ExtrapolationColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> ExtrapolationColorProperty = AvaloniaProperty.Register<ExtrapolationLineSeries, Color>(
            nameof(ExtrapolationColor),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="ExtrapolationLineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> ExtrapolationLineStyleProperty = AvaloniaProperty.Register<ExtrapolationLineSeries, LineStyle>(
            nameof(ExtrapolationLineStyle),
            LineStyle.Automatic);

        /// <summary>
        /// Identifies the <see cref="ExtrapolationDashes"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double[]> ExtrapolationDashesProperty = AvaloniaProperty.Register<ExtrapolationLineSeries, double[]>(
            nameof(ExtrapolationDashes),
            null);

        /// <summary>
        /// Identifies the <see cref="IgnoreExtrapolationForScaling"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IgnoreExtrapolationForScalingProperty = AvaloniaProperty.Register<ExtrapolationLineSeries, bool>(
            nameof(IgnoreExtrapolationForScaling),
            false);

        /// <summary>
        /// Initializes static members of the <see cref="ExtrapolationLineSeries"/> class.
        /// </summary>
        static ExtrapolationLineSeries()
        {
            ExtrapolationColorProperty.Changed.AddClassHandler<ExtrapolationLineSeries>(AppearanceChanged);
            ExtrapolationLineStyleProperty.Changed.AddClassHandler<ExtrapolationLineSeries>(AppearanceChanged);
            ExtrapolationDashesProperty.Changed.AddClassHandler<ExtrapolationLineSeries>(AppearanceChanged);
            IgnoreExtrapolationForScalingProperty.Changed.AddClassHandler<ExtrapolationLineSeries>(AppearanceChanged);
            LineStyleProperty.OverrideDefaultValue<ExtrapolationLineSeries>(LineStyle.Dot);
            LineStyleProperty.Changed.AddClassHandler<ExtrapolationLineSeries>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtrapolationLineSeries"/> class.
        /// </summary>
        public ExtrapolationLineSeries()
        {
            this.InternalSeries = new OxyPlot.Series.ExtrapolationLineSeries();
        }

        /// <summary>
        /// Gets the list of X intervals where the line is rendered with extrapolation style.
        /// </summary>
        /// <value>The list of data ranges.</value>
        public IList<DataRange> Intervals
        {
            get => ((OxyPlot.Series.ExtrapolationLineSeries)this.InternalSeries).Intervals;
        }

        /// <summary>
        /// Gets or sets the color for the extrapolated portions of the line.
        /// </summary>
        /// <value>The extrapolation color. The default is <see cref="Colors.Black"/>.</value>
        public Color ExtrapolationColor
        {
            get => (Color)this.GetValue(ExtrapolationColorProperty);
            set => this.SetValue(ExtrapolationColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style for the extrapolated portions.
        /// </summary>
        /// <value>The extrapolation line style. The default is <see cref="LineStyle.Automatic"/>.</value>
        public LineStyle ExtrapolationLineStyle
        {
            get => (LineStyle)this.GetValue(ExtrapolationLineStyleProperty);
            set => this.SetValue(ExtrapolationLineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the dash array for the extrapolated portions.
        /// </summary>
        /// <value>The dash array. The default is <c>null</c>.</value>
        public double[] ExtrapolationDashes
        {
            get => (double[])this.GetValue(ExtrapolationDashesProperty);
            set => this.SetValue(ExtrapolationDashesProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore extrapolated regions for axis scaling.
        /// </summary>
        /// <value><c>true</c> to ignore extrapolated regions; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool IgnoreExtrapolationForScaling
        {
            get => (bool)this.GetValue(IgnoreExtrapolationForScalingProperty);
            set => this.SetValue(IgnoreExtrapolationForScalingProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.ExtrapolationLineSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.ExtrapolationLineSeries s)
            {
                s.ExtrapolationColor = this.ExtrapolationColor.ToOxyColor();
                s.ExtrapolationLineStyle = this.ExtrapolationLineStyle;
                s.ExtrapolationDashes = this.ExtrapolationDashes;
                s.IgnoreExtrapolationForScaling = this.IgnoreExtrapolationForScaling;
            }
        }
    }
}
