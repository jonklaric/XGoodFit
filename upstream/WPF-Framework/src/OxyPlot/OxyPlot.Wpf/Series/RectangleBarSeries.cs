// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RectangleBarSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.RectangleBarSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.RectangleBarSeries"/>.
    /// </summary>
    /// <remarks>
    /// A rectangle bar series displays bars defined by explicit X0, X1, Y0, Y1 coordinates,
    /// providing full control over bar positioning and dimensions.
    /// </remarks>
    public class RectangleBarSeries : XYAxisSeries
    {
        /// <summary>
        /// Identifies the <see cref="FillColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FillColorProperty =
            DependencyProperty.Register(
                nameof(FillColor),
                typeof(Color),
                typeof(RectangleBarSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeColorProperty =
            DependencyProperty.Register(
                nameof(StrokeColor),
                typeof(Color),
                typeof(RectangleBarSeries),
                new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(RectangleBarSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelFormatString"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFormatStringProperty =
            DependencyProperty.Register(
                nameof(LabelFormatString),
                typeof(string),
                typeof(RectangleBarSeries),
                new PropertyMetadata("{4}", AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="RectangleBarSeries"/> class.
        /// </summary>
        static RectangleBarSeries()
        {
            TrackerFormatStringProperty.OverrideMetadata(
                typeof(RectangleBarSeries),
                new PropertyMetadata(OxyPlot.Series.RectangleBarSeries.DefaultTrackerFormatString, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleBarSeries"/> class.
        /// </summary>
        public RectangleBarSeries()
        {
            this.InternalSeries = new OxyPlot.Series.RectangleBarSeries();
        }

        /// <summary>
        /// Gets the rectangle bar items.
        /// </summary>
        /// <value>The list of rectangle bar items.</value>
        public new IList<RectangleBarItem> Items
        {
            get => ((OxyPlot.Series.RectangleBarSeries)this.InternalSeries).Items;
        }

        /// <summary>
        /// Gets or sets the fill color of the rectangles.
        /// </summary>
        /// <value>The fill color. The default is <see cref="MoreColors.Automatic"/>.</value>
        public Color FillColor
        {
            get => (Color)this.GetValue(FillColorProperty);
            set => this.SetValue(FillColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke color of the rectangle borders.
        /// </summary>
        /// <value>The stroke color. The default is <see cref="Colors.Black"/>.</value>
        public Color StrokeColor
        {
            get => (Color)this.GetValue(StrokeColorProperty);
            set => this.SetValue(StrokeColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness of the rectangle borders.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>1.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string for labels.
        /// </summary>
        /// <value>The label format string. The default is <c>"{4}"</c> (title).</value>
        public string LabelFormatString
        {
            get => (string)this.GetValue(LabelFormatStringProperty);
            set => this.SetValue(LabelFormatStringProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.RectangleBarSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.RectangleBarSeries s)
            {
                s.FillColor = this.FillColor.ToOxyColor();
                s.StrokeColor = this.StrokeColor.ToOxyColor();
                s.StrokeThickness = this.StrokeThickness;
                s.LabelFormatString = this.LabelFormatString;
            }
        }
    }
}
