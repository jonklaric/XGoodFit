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
    using Avalonia;
    using Avalonia.Collections;
    using Avalonia.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.TwoColorAreaSeries"/>.
    /// </summary>
    /// <remarks>
    /// A two-color area series displays an area chart that changes fill color
    /// based on whether values are above or below a specified limit value.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the second line's
    /// dashes are Avalonia's list of doubles, where WPF's were a <c>DoubleCollection</c>.
    /// </remarks>
    public class TwoColorAreaSeries : AreaSeries
    {
        /// <summary>
        /// Identifies the <see cref="Dashes2"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<AvaloniaList<double>> Dashes2Property = AvaloniaProperty.Register<TwoColorAreaSeries, AvaloniaList<double>>(
            nameof(Dashes2),
            null);

        /// <summary>
        /// Identifies the <see cref="Fill2"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> Fill2Property = AvaloniaProperty.Register<TwoColorAreaSeries, Color>(
            nameof(Fill2),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="LineStyle2"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> LineStyle2Property = AvaloniaProperty.Register<TwoColorAreaSeries, LineStyle>(
            nameof(LineStyle2),
            LineStyle.Solid);

        /// <summary>
        /// Identifies the <see cref="Limit"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LimitProperty = AvaloniaProperty.Register<TwoColorAreaSeries, double>(
            nameof(Limit),
            0.0);

        /// <summary>
        /// Identifies the <see cref="MarkerFill2"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> MarkerFill2Property = AvaloniaProperty.Register<TwoColorAreaSeries, Color>(
            nameof(MarkerFill2),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="MarkerStroke2"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> MarkerStroke2Property = AvaloniaProperty.Register<TwoColorAreaSeries, Color>(
            nameof(MarkerStroke2),
            MoreColors.Automatic);

        /// <summary>
        /// Initializes static members of the <see cref="TwoColorAreaSeries" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static TwoColorAreaSeries()
        {
            Dashes2Property.Changed.AddClassHandler<TwoColorAreaSeries>(AppearanceChanged);
            Fill2Property.Changed.AddClassHandler<TwoColorAreaSeries>(AppearanceChanged);
            LineStyle2Property.Changed.AddClassHandler<TwoColorAreaSeries>(AppearanceChanged);
            LimitProperty.Changed.AddClassHandler<TwoColorAreaSeries>(AppearanceChanged);
            MarkerFill2Property.Changed.AddClassHandler<TwoColorAreaSeries>(AppearanceChanged);
            MarkerStroke2Property.Changed.AddClassHandler<TwoColorAreaSeries>(AppearanceChanged);
        }

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
        public AvaloniaList<double> Dashes2
        {
            get => (AvaloniaList<double>)this.GetValue(Dashes2Property);
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
