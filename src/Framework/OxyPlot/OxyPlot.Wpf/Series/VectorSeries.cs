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
    using Avalonia;
    using Avalonia.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.VectorSeries"/>.
    /// </summary>
    /// <remarks>
    /// A vector series displays arrows representing vectors at specified locations,
    /// useful for visualizing vector fields, flow directions, or gradients.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF.
    /// </remarks>
    public class VectorSeries : XYAxisSeries
    {
        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public new static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<VectorSeries, Color>(
            nameof(Color),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<VectorSeries, double>(
            nameof(StrokeThickness),
            2.0);

        /// <summary>
        /// Identifies the <see cref="ArrowHeadLength"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ArrowHeadLengthProperty = AvaloniaProperty.Register<VectorSeries, double>(
            nameof(ArrowHeadLength),
            3.0);

        /// <summary>
        /// Identifies the <see cref="ArrowHeadWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ArrowHeadWidthProperty = AvaloniaProperty.Register<VectorSeries, double>(
            nameof(ArrowHeadWidth),
            2.0);

        /// <summary>
        /// Identifies the <see cref="ArrowHeadPosition"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ArrowHeadPositionProperty = AvaloniaProperty.Register<VectorSeries, double>(
            nameof(ArrowHeadPosition),
            1.0);

        /// <summary>
        /// Identifies the <see cref="ArrowVeeness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ArrowVeenessProperty = AvaloniaProperty.Register<VectorSeries, double>(
            nameof(ArrowVeeness),
            0.0);

        /// <summary>
        /// Identifies the <see cref="ArrowStartPosition"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ArrowStartPositionProperty = AvaloniaProperty.Register<VectorSeries, double>(
            nameof(ArrowStartPosition),
            0.0);

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> LineStyleProperty = AvaloniaProperty.Register<VectorSeries, LineStyle>(
            nameof(LineStyle),
            LineStyle.Solid);

        /// <summary>
        /// Identifies the <see cref="ColorAxisKey"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> ColorAxisKeyProperty = AvaloniaProperty.Register<VectorSeries, string>(
            nameof(ColorAxisKey),
            null);

        /// <summary>
        /// Identifies the <see cref="LabelFormatString"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> LabelFormatStringProperty = AvaloniaProperty.Register<VectorSeries, string>(
            nameof(LabelFormatString),
            "0.00");

        /// <summary>
        /// Identifies the <see cref="LabelFontSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LabelFontSizeProperty = AvaloniaProperty.Register<VectorSeries, double>(
            nameof(LabelFontSize),
            0.0);

        /// <summary>
        /// Identifies the <see cref="Mapping"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Func<object, VectorItem>> MappingProperty = AvaloniaProperty.Register<VectorSeries, Func<object, VectorItem>>(
            nameof(Mapping),
            null);

        /// <summary>
        /// Initializes static members of the <see cref="VectorSeries"/> class.
        /// </summary>
        static VectorSeries()
        {
            ColorProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
            StrokeThicknessProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
            ArrowHeadLengthProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
            ArrowHeadWidthProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
            ArrowHeadPositionProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
            ArrowVeenessProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
            ArrowStartPositionProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
            LineStyleProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
            ColorAxisKeyProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
            LabelFormatStringProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
            LabelFontSizeProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
            MappingProperty.Changed.AddClassHandler<VectorSeries>(DataChanged);
            TrackerFormatStringProperty.OverrideDefaultValue<VectorSeries>(OxyPlot.Series.VectorSeries.DefaultTrackerFormatString);
            TrackerFormatStringProperty.Changed.AddClassHandler<VectorSeries>(AppearanceChanged);
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
