// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TornadoBarSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.TornadoBarSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.TornadoBarSeries"/>.
    /// </summary>
    /// <remarks>
    /// A tornado bar series displays dual-direction bars showing minimum and maximum
    /// values, commonly used for sensitivity analysis and tornado diagrams.
    /// </remarks>
    public class TornadoBarSeries : CategorizedSeries
    {
        /// <summary>
        /// Identifies the <see cref="BarWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BarWidthProperty =
            DependencyProperty.Register(
                nameof(BarWidth),
                typeof(double),
                typeof(TornadoBarSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="BaseValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BaseValueProperty =
            DependencyProperty.Register(
                nameof(BaseValue),
                typeof(double),
                typeof(TornadoBarSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MaximumFillColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumFillColorProperty =
            DependencyProperty.Register(
                nameof(MaximumFillColor),
                typeof(Color),
                typeof(TornadoBarSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumFillColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumFillColorProperty =
            DependencyProperty.Register(
                nameof(MinimumFillColor),
                typeof(Color),
                typeof(TornadoBarSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeColorProperty =
            DependencyProperty.Register(
                nameof(StrokeColor),
                typeof(Color),
                typeof(TornadoBarSeries),
                new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(TornadoBarSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelMarginProperty =
            DependencyProperty.Register(
                nameof(LabelMargin),
                typeof(double),
                typeof(TornadoBarSeries),
                new PropertyMetadata(4.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumLabelFormatString"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumLabelFormatStringProperty =
            DependencyProperty.Register(
                nameof(MinimumLabelFormatString),
                typeof(string),
                typeof(TornadoBarSeries),
                new PropertyMetadata("{0}", AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MaximumLabelFormatString"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumLabelFormatStringProperty =
            DependencyProperty.Register(
                nameof(MaximumLabelFormatString),
                typeof(string),
                typeof(TornadoBarSeries),
                new PropertyMetadata("{0}", AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumField"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumFieldProperty =
            DependencyProperty.Register(
                nameof(MinimumField),
                typeof(string),
                typeof(TornadoBarSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="MaximumField"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumFieldProperty =
            DependencyProperty.Register(
                nameof(MaximumField),
                typeof(string),
                typeof(TornadoBarSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="BaseField"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BaseFieldProperty =
            DependencyProperty.Register(
                nameof(BaseField),
                typeof(string),
                typeof(TornadoBarSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Initializes static members of the <see cref="TornadoBarSeries"/> class.
        /// </summary>
        static TornadoBarSeries()
        {
            TrackerFormatStringProperty.OverrideMetadata(
                typeof(TornadoBarSeries),
                new PropertyMetadata(OxyPlot.Series.TornadoBarSeries.DefaultTrackerFormatString, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TornadoBarSeries"/> class.
        /// </summary>
        public TornadoBarSeries()
        {
            this.InternalSeries = new OxyPlot.Series.TornadoBarSeries();
        }

        /// <summary>
        /// Gets or sets the width of the bars as a fraction of available space.
        /// </summary>
        /// <value>The bar width (0-1). The default is <c>1.0</c>.</value>
        public double BarWidth
        {
            get => (double)this.GetValue(BarWidthProperty);
            set => this.SetValue(BarWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the base value from which bars extend.
        /// </summary>
        /// <value>The base value. The default is <c>0.0</c>.</value>
        public double BaseValue
        {
            get => (double)this.GetValue(BaseValueProperty);
            set => this.SetValue(BaseValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the fill color for maximum value bars.
        /// </summary>
        /// <value>The maximum fill color. The default is <see cref="MoreColors.Automatic"/>.</value>
        public Color MaximumFillColor
        {
            get => (Color)this.GetValue(MaximumFillColorProperty);
            set => this.SetValue(MaximumFillColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the fill color for minimum value bars.
        /// </summary>
        /// <value>The minimum fill color. The default is <see cref="MoreColors.Automatic"/>.</value>
        public Color MinimumFillColor
        {
            get => (Color)this.GetValue(MinimumFillColorProperty);
            set => this.SetValue(MinimumFillColorProperty, value);
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
        /// <value>The stroke thickness. The default is <c>1.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the margin between bars and labels.
        /// </summary>
        /// <value>The label margin. The default is <c>4.0</c>.</value>
        public double LabelMargin
        {
            get => (double)this.GetValue(LabelMarginProperty);
            set => this.SetValue(LabelMarginProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string for minimum value labels.
        /// </summary>
        /// <value>The format string. The default is <c>"{0}"</c>.</value>
        public string MinimumLabelFormatString
        {
            get => (string)this.GetValue(MinimumLabelFormatStringProperty);
            set => this.SetValue(MinimumLabelFormatStringProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string for maximum value labels.
        /// </summary>
        /// <value>The format string. The default is <c>"{0}"</c>.</value>
        public string MaximumLabelFormatString
        {
            get => (string)this.GetValue(MaximumLabelFormatStringProperty);
            set => this.SetValue(MaximumLabelFormatStringProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property containing the minimum value.
        /// </summary>
        /// <value>The field name. The default is <c>null</c>.</value>
        public string MinimumField
        {
            get => (string)this.GetValue(MinimumFieldProperty);
            set => this.SetValue(MinimumFieldProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property containing the maximum value.
        /// </summary>
        /// <value>The field name. The default is <c>null</c>.</value>
        public string MaximumField
        {
            get => (string)this.GetValue(MaximumFieldProperty);
            set => this.SetValue(MaximumFieldProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property containing the base value.
        /// </summary>
        /// <value>The field name. The default is <c>null</c>.</value>
        public string BaseField
        {
            get => (string)this.GetValue(BaseFieldProperty);
            set => this.SetValue(BaseFieldProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.TornadoBarSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.TornadoBarSeries s)
            {
                s.BarWidth = this.BarWidth;
                s.BaseValue = this.BaseValue;
                s.MaximumFillColor = this.MaximumFillColor.ToOxyColor();
                s.MinimumFillColor = this.MinimumFillColor.ToOxyColor();
                s.StrokeColor = this.StrokeColor.ToOxyColor();
                s.StrokeThickness = this.StrokeThickness;
                s.LabelMargin = this.LabelMargin;
                s.MinimumLabelFormatString = this.MinimumLabelFormatString;
                s.MaximumLabelFormatString = this.MaximumLabelFormatString;
                s.MinimumField = this.MinimumField;
                s.MaximumField = this.MaximumField;
                s.BaseField = this.BaseField;
            }
        }
    }
}
