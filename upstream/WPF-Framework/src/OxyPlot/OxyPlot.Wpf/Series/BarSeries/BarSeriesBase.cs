// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BarSeriesBase.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents an abstract base class for bar series types.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;
    using System.Windows.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents an abstract base class for bar series types.
    /// </summary>
    /// <remarks>
    /// This class provides common properties for bar-based chart series,
    /// including fill colors, stroke settings, labels, and stacking options.
    /// </remarks>
    public class BarSeriesBase : CategorizedSeries
    {
        /// <summary>
        /// Identifies the <see cref="BaseValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BaseValueProperty =
            DependencyProperty.Register(
                nameof(BaseValue),
                typeof(double),
                typeof(BarSeriesBase),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ColorField"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorFieldProperty =
            DependencyProperty.Register(
                nameof(ColorField),
                typeof(string),
                typeof(BarSeriesBase),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="FillColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FillColorProperty =
            DependencyProperty.Register(
                nameof(FillColor),
                typeof(Color),
                typeof(BarSeriesBase),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="IsStacked"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStackedProperty =
            DependencyProperty.Register(
                nameof(IsStacked),
                typeof(bool),
                typeof(BarSeriesBase),
                new PropertyMetadata(false, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelFormatString"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFormatStringProperty =
            DependencyProperty.Register(
                nameof(LabelFormatString),
                typeof(string),
                typeof(BarSeriesBase),
                new UIPropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelMarginProperty =
            DependencyProperty.Register(
                nameof(LabelMargin),
                typeof(double),
                typeof(BarSeriesBase),
                new PropertyMetadata(2.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelPlacement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelPlacementProperty =
            DependencyProperty.Register(
                nameof(LabelPlacement),
                typeof(LabelPlacement),
                typeof(BarSeriesBase),
                new PropertyMetadata(LabelPlacement.Outside, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="NegativeFillColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NegativeFillColorProperty =
            DependencyProperty.Register(
                nameof(NegativeFillColor),
                typeof(Color),
                typeof(BarSeriesBase),
                new PropertyMetadata(MoreColors.Undefined, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StackGroup"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StackGroupProperty =
            DependencyProperty.Register(
                nameof(StackGroup),
                typeof(string),
                typeof(BarSeriesBase),
                new PropertyMetadata(string.Empty, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeColorProperty =
            DependencyProperty.Register(
                nameof(StrokeColor),
                typeof(Color),
                typeof(BarSeriesBase),
                new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(BarSeriesBase),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ValueField"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueFieldProperty =
            DependencyProperty.Register(
                nameof(ValueField),
                typeof(string),
                typeof(BarSeriesBase),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Gets or sets the base value for the bars.
        /// </summary>
        /// <value>The base value. The default is <c>0.0</c>.</value>
        /// <remarks>Bars will extend from this value to the data value.</remarks>
        public double BaseValue
        {
            get => (double)this.GetValue(BaseValueProperty);
            set => this.SetValue(BaseValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property containing the color for each item.
        /// </summary>
        /// <value>The color field name. The default is <c>null</c>.</value>
        public string ColorField
        {
            get => (string)this.GetValue(ColorFieldProperty);
            set => this.SetValue(ColorFieldProperty, value);
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
        /// Gets or sets a value indicating whether the series is stacked.
        /// </summary>
        /// <value><c>true</c> if stacked; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool IsStacked
        {
            get => (bool)this.GetValue(IsStackedProperty);
            set => this.SetValue(IsStackedProperty, value);
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
        /// Gets or sets the margin between the bar and its label.
        /// </summary>
        /// <value>The label margin in pixels. The default is <c>2.0</c>.</value>
        public double LabelMargin
        {
            get => (double)this.GetValue(LabelMarginProperty);
            set => this.SetValue(LabelMarginProperty, value);
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
        /// Gets or sets the fill color for negative values.
        /// </summary>
        /// <value>The negative fill color. The default is <see cref="MoreColors.Undefined"/>.</value>
        public Color NegativeFillColor
        {
            get => (Color)this.GetValue(NegativeFillColorProperty);
            set => this.SetValue(NegativeFillColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the stack group identifier for grouped stacking.
        /// </summary>
        /// <value>The stack group name. The default is an empty string.</value>
        public string StackGroup
        {
            get => (string)this.GetValue(StackGroupProperty);
            set => this.SetValue(StackGroupProperty, value);
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
        /// Gets or sets the thickness of the bar borders.
        /// </summary>
        /// <value>The stroke thickness in pixels. The default is <c>0.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property containing the value.
        /// </summary>
        /// <value>The value field name. The default is <c>null</c>.</value>
        public string ValueField
        {
            get => (string)this.GetValue(ValueFieldProperty);
            set => this.SetValue(ValueFieldProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>An <see cref="OxyPlot.Series.Series"/> instance.</returns>
        public override OxyPlot.Series.Series CreateModel()
        {
            this.SynchronizeProperties(this.InternalSeries);
            return this.InternalSeries;
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal series to synchronize.</param>
        /// <remarks>
        /// The properties defined in this class are synchronized in the concrete derived classes
        /// (BarSeries, ColumnSeries) since OxyPlot.Series.BarSeriesBase is generic and cannot be
        /// used directly in pattern matching.
        /// </remarks>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);
        }
    }
}
