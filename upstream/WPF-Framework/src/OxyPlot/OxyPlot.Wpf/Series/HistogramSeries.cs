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
    using System.Windows;
    using System.Windows.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.HistogramSeries"/>.
    /// </summary>
    /// <remarks>
    /// A histogram series displays the distribution of data by grouping values
    /// into bins and showing the frequency or count of each bin as vertical bars.
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
        public static readonly DependencyProperty FillColorProperty =
            DependencyProperty.Register(
                nameof(FillColor),
                typeof(Color),
                typeof(HistogramSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="NegativeFillColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NegativeFillColorProperty =
            DependencyProperty.Register(
                nameof(NegativeFillColor),
                typeof(Color),
                typeof(HistogramSeries),
                new PropertyMetadata(MoreColors.Undefined, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeColorProperty =
            DependencyProperty.Register(
                nameof(StrokeColor),
                typeof(Color),
                typeof(HistogramSeries),
                new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(HistogramSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelFormatString"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFormatStringProperty =
            DependencyProperty.Register(
                nameof(LabelFormatString),
                typeof(string),
                typeof(HistogramSeries),
                new UIPropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelPlacement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelPlacementProperty =
            DependencyProperty.Register(
                nameof(LabelPlacement),
                typeof(LabelPlacement),
                typeof(HistogramSeries),
                new PropertyMetadata(LabelPlacement.Outside, AppearanceChanged));

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
        /// This property shadows <see cref="System.Windows.Controls.ItemsControl.Items"/> to provide
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
