// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HistogramSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.HistogramSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Collections.Generic;
    using Avalonia;
    using Avalonia.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.HistogramSeries"/>.
    /// </summary>
    /// <remarks>
    /// A histogram series displays the distribution of data by grouping values
    /// into bins and showing the frequency or count of each bin as vertical bars.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor.
    /// </remarks>
    public class HistogramSeries : XYAxisSeries
    {
        /// <summary>
        /// The items collection for manually added items.
        /// </summary>
        private readonly List<HistogramItem> items = new List<HistogramItem>();

        /// <summary>
        /// Identifies the <see cref="FillColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> FillColorProperty = AvaloniaProperty.Register<HistogramSeries, Color>(
            nameof(FillColor),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="NegativeFillColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> NegativeFillColorProperty = AvaloniaProperty.Register<HistogramSeries, Color>(
            nameof(NegativeFillColor),
            MoreColors.Undefined);

        /// <summary>
        /// Identifies the <see cref="StrokeColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> StrokeColorProperty = AvaloniaProperty.Register<HistogramSeries, Color>(
            nameof(StrokeColor),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<HistogramSeries, double>(
            nameof(StrokeThickness),
            0.0);

        /// <summary>
        /// Identifies the <see cref="LabelFormatString"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> LabelFormatStringProperty = AvaloniaProperty.Register<HistogramSeries, string>(
            nameof(LabelFormatString),
            null);

        /// <summary>
        /// Identifies the <see cref="LabelPlacement"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LabelPlacement> LabelPlacementProperty = AvaloniaProperty.Register<HistogramSeries, LabelPlacement>(
            nameof(LabelPlacement),
            LabelPlacement.Outside);

        /// <summary>
        /// Initializes static members of the <see cref="HistogramSeries" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static HistogramSeries()
        {
            FillColorProperty.Changed.AddClassHandler<HistogramSeries>(AppearanceChanged);
            NegativeFillColorProperty.Changed.AddClassHandler<HistogramSeries>(AppearanceChanged);
            StrokeColorProperty.Changed.AddClassHandler<HistogramSeries>(AppearanceChanged);
            StrokeThicknessProperty.Changed.AddClassHandler<HistogramSeries>(AppearanceChanged);
            LabelFormatStringProperty.Changed.AddClassHandler<HistogramSeries>(AppearanceChanged);
            LabelPlacementProperty.Changed.AddClassHandler<HistogramSeries>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistogramSeries"/> class.
        /// </summary>
        public HistogramSeries()
        {
            this.InternalSeries = new OxyPlot.Series.HistogramSeries();
        }

        /// <summary>
        /// Gets the items collection.
        /// </summary>
        /// <value>A list of items that can be manually added when not using ItemsSource.</value>
        /// <remarks>
        /// This property shadows <c>ItemsControl.Items</c> to provide
        /// a strongly-typed collection for histogram series items.
        /// </remarks>
        public new List<HistogramItem> Items => this.items;

        /// <summary>
        /// Gets or sets the fill color of the histogram bars.
        /// </summary>
        /// <value>The fill color. The default is <see cref="MoreColors.Automatic"/>.</value>
        public Color FillColor
        {
            get => (Color)this.GetValue(FillColorProperty);
            set => this.SetValue(FillColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the fill color for negative values.
        /// </summary>
        /// <value>The negative fill color. The default is <see cref="MoreColors.Undefined"/>.</value>
        public Color NegativeFillColor
        {
            get => (Color)this.GetValue(NegativeFillColorProperty);
            set => this.SetValue(NegativeFillColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke color for bar borders.
        /// </summary>
        /// <value>The stroke color. The default is <see cref="Colors.Black"/>.</value>
        public Color StrokeColor
        {
            get => (Color)this.GetValue(StrokeColorProperty);
            set => this.SetValue(StrokeColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness for bar borders.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>0.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string for bar labels.
        /// </summary>
        /// <value>The label format string. The default is <c>null</c>.</value>
        public string LabelFormatString
        {
            get => (string)this.GetValue(LabelFormatStringProperty);
            set => this.SetValue(LabelFormatStringProperty, value);
        }

        /// <summary>
        /// Gets or sets the placement of bar labels.
        /// </summary>
        /// <value>The label placement. The default is <see cref="LabelPlacement.Outside"/>.</value>
        public LabelPlacement LabelPlacement
        {
            get => (LabelPlacement)this.GetValue(LabelPlacementProperty);
            set => this.SetValue(LabelPlacementProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.HistogramSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.HistogramSeries s)
            {
                s.FillColor = this.FillColor.ToOxyColor();
                s.NegativeFillColor = this.NegativeFillColor.ToOxyColor();
                s.StrokeColor = this.StrokeColor.ToOxyColor();
                s.StrokeThickness = this.StrokeThickness;
                s.LabelFormatString = this.LabelFormatString;
                s.LabelPlacement = this.LabelPlacement;

                if (this.ItemsSource == null)
                {
                    s.Items.Clear();
                    foreach (var item in this.items)
                    {
                        s.Items.Add(item);
                    }
                }
            }
        }
    }
}
