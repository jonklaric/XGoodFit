// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VectorSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.VectorSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.VectorSeries"/>.
    /// </summary>
    /// <remarks>
    /// A vector series displays arrows representing vectors at specified locations,
    /// useful for visualizing vector fields, flow directions, or gradients.
    /// </remarks>
    public class VectorSeries : XYAxisSeries
    {
        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public new static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(VectorSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(VectorSeries),
                new PropertyMetadata(2.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ArrowHeadLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArrowHeadLengthProperty =
            DependencyProperty.Register(
                nameof(ArrowHeadLength),
                typeof(double),
                typeof(VectorSeries),
                new PropertyMetadata(3.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ArrowHeadWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArrowHeadWidthProperty =
            DependencyProperty.Register(
                nameof(ArrowHeadWidth),
                typeof(double),
                typeof(VectorSeries),
                new PropertyMetadata(2.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ArrowHeadPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArrowHeadPositionProperty =
            DependencyProperty.Register(
                nameof(ArrowHeadPosition),
                typeof(double),
                typeof(VectorSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ArrowVeeness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArrowVeenessProperty =
            DependencyProperty.Register(
                nameof(ArrowVeeness),
                typeof(double),
                typeof(VectorSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ArrowStartPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArrowStartPositionProperty =
            DependencyProperty.Register(
                nameof(ArrowStartPosition),
                typeof(double),
                typeof(VectorSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStyleProperty =
            DependencyProperty.Register(
                nameof(LineStyle),
                typeof(LineStyle),
                typeof(VectorSeries),
                new PropertyMetadata(LineStyle.Solid, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ColorAxisKey"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorAxisKeyProperty =
            DependencyProperty.Register(
                nameof(ColorAxisKey),
                typeof(string),
                typeof(VectorSeries),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelFormatString"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFormatStringProperty =
            DependencyProperty.Register(
                nameof(LabelFormatString),
                typeof(string),
                typeof(VectorSeries),
                new PropertyMetadata("0.00", AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFontSizeProperty =
            DependencyProperty.Register(
                nameof(LabelFontSize),
                typeof(double),
                typeof(VectorSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Mapping"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MappingProperty =
            DependencyProperty.Register(
                nameof(Mapping),
                typeof(Func<object, VectorItem>),
                typeof(VectorSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Initializes static members of the <see cref="VectorSeries"/> class.
        /// </summary>
        static VectorSeries()
        {
            TrackerFormatStringProperty.OverrideMetadata(
                typeof(VectorSeries),
                new PropertyMetadata(OxyPlot.Series.VectorSeries.DefaultTrackerFormatString, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VectorSeries"/> class.
        /// </summary>
        public VectorSeries()
        {
            this.InternalSeries = new OxyPlot.Series.VectorSeries();
        }

        /// <summary>
        /// Gets the vector items.
        /// </summary>
        /// <value>The list of vector items.</value>
        public new IList<VectorItem> Items
        {
            get => ((OxyPlot.Series.VectorSeries)this.InternalSeries).Items;
        }

        /// <summary>
        /// Gets or sets the color of the arrows.
        /// </summary>
        /// <value>The color. The default is <see cref="MoreColors.Automatic"/>.</value>
        public new Color Color
        {
            get => (Color)this.GetValue(ColorProperty);
            set => this.SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>2.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the length of the arrow heads relative to stroke thickness.
        /// </summary>
        /// <value>The arrow head length. The default is <c>3.0</c>.</value>
        public double ArrowHeadLength
        {
            get => (double)this.GetValue(ArrowHeadLengthProperty);
            set => this.SetValue(ArrowHeadLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the arrow heads relative to stroke thickness.
        /// </summary>
        /// <value>The arrow head width. The default is <c>2.0</c>.</value>
        public double ArrowHeadWidth
        {
            get => (double)this.GetValue(ArrowHeadWidthProperty);
            set => this.SetValue(ArrowHeadWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the position of the arrow head relative to vector end.
        /// </summary>
        /// <value>The position (0-1). The default is <c>1.0</c>.</value>
        public double ArrowHeadPosition
        {
            get => (double)this.GetValue(ArrowHeadPositionProperty);
            set => this.SetValue(ArrowHeadPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the "veeness" of the arrow head.
        /// </summary>
        /// <value>The veeness. The default is <c>0.0</c>.</value>
        public double ArrowVeeness
        {
            get => (double)this.GetValue(ArrowVeenessProperty);
            set => this.SetValue(ArrowVeenessProperty, value);
        }

        /// <summary>
        /// Gets or sets the start position of arrows relative to vector origin.
        /// </summary>
        /// <value>The start position (0-1). The default is <c>0.0</c>.</value>
        public double ArrowStartPosition
        {
            get => (double)this.GetValue(ArrowStartPositionProperty);
            set => this.SetValue(ArrowStartPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style.
        /// </summary>
        /// <value>The line style. The default is <see cref="OxyPlot.LineStyle.Solid"/>.</value>
        public LineStyle LineStyle
        {
            get => (LineStyle)this.GetValue(LineStyleProperty);
            set => this.SetValue(LineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the color axis key.
        /// </summary>
        /// <value>The color axis key. The default is <c>null</c>.</value>
        public string ColorAxisKey
        {
            get => (string)this.GetValue(ColorAxisKeyProperty);
            set => this.SetValue(ColorAxisKeyProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string for labels.
        /// </summary>
        /// <value>The label format string. The default is <c>"0.00"</c>.</value>
        public string LabelFormatString
        {
            get => (string)this.GetValue(LabelFormatStringProperty);
            set => this.SetValue(LabelFormatStringProperty, value);
        }

        /// <summary>
        /// Gets or sets the font size for labels.
        /// </summary>
        /// <value>The label font size. The default is <c>0.0</c> (no labels).</value>
        public double LabelFontSize
        {
            get => (double)this.GetValue(LabelFontSizeProperty);
            set => this.SetValue(LabelFontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the mapping function from data items to vector items.
        /// </summary>
        /// <value>The mapping function. The default is <c>null</c>.</value>
        public Func<object, VectorItem> Mapping
        {
            get => (Func<object, VectorItem>)this.GetValue(MappingProperty);
            set => this.SetValue(MappingProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.VectorSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.VectorSeries s)
            {
                s.Color = this.Color.ToOxyColor();
                s.StrokeThickness = this.StrokeThickness;
                s.ArrowHeadLength = this.ArrowHeadLength;
                s.ArrowHeadWidth = this.ArrowHeadWidth;
                s.ArrowHeadPosition = this.ArrowHeadPosition;
                s.ArrowVeeness = this.ArrowVeeness;
                s.ArrowStartPosition = this.ArrowStartPosition;
                s.LineStyle = this.LineStyle;
                s.ColorAxisKey = this.ColorAxisKey;
                s.LabelFormatString = this.LabelFormatString;
                s.LabelFontSize = this.LabelFontSize;
                s.Mapping = this.Mapping;
            }
        }
    }
}
