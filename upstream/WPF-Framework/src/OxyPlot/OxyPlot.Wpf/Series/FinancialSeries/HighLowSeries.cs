// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HighLowSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.HighLowSeries.
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
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.HighLowSeries"/>.
    /// </summary>
    /// <remarks>
    /// A high-low series displays financial data showing the high, low, open, and close
    /// values for each time period, rendered as vertical bars with tick marks.
    /// </remarks>
    public class HighLowSeries : XYAxisSeries
    {
        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public new static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(HighLowSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(HighLowSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TickLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickLengthProperty =
            DependencyProperty.Register(
                nameof(TickLength),
                typeof(double),
                typeof(HighLowSeries),
                new PropertyMetadata(4.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStyleProperty =
            DependencyProperty.Register(
                nameof(LineStyle),
                typeof(LineStyle),
                typeof(HighLowSeries),
                new PropertyMetadata(LineStyle.Solid, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="DataFieldX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataFieldXProperty =
            DependencyProperty.Register(
                nameof(DataFieldX),
                typeof(string),
                typeof(HighLowSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="DataFieldHigh"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataFieldHighProperty =
            DependencyProperty.Register(
                nameof(DataFieldHigh),
                typeof(string),
                typeof(HighLowSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="DataFieldLow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataFieldLowProperty =
            DependencyProperty.Register(
                nameof(DataFieldLow),
                typeof(string),
                typeof(HighLowSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="DataFieldOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataFieldOpenProperty =
            DependencyProperty.Register(
                nameof(DataFieldOpen),
                typeof(string),
                typeof(HighLowSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="DataFieldClose"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataFieldCloseProperty =
            DependencyProperty.Register(
                nameof(DataFieldClose),
                typeof(string),
                typeof(HighLowSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="Mapping"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MappingProperty =
            DependencyProperty.Register(
                nameof(Mapping),
                typeof(Func<object, HighLowItem>),
                typeof(HighLowSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Initializes static members of the <see cref="HighLowSeries"/> class.
        /// </summary>
        static HighLowSeries()
        {
            TrackerFormatStringProperty.OverrideMetadata(
                typeof(HighLowSeries),
                new PropertyMetadata(OxyPlot.Series.HighLowSeries.DefaultTrackerFormatString, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HighLowSeries"/> class.
        /// </summary>
        public HighLowSeries()
        {
            this.InternalSeries = new OxyPlot.Series.HighLowSeries();
        }

        /// <summary>
        /// Gets the high-low items.
        /// </summary>
        /// <value>The list of high-low items.</value>
        public new List<HighLowItem> Items
        {
            get => ((OxyPlot.Series.HighLowSeries)this.InternalSeries).Items;
        }

        /// <summary>
        /// Gets or sets the color of the series.
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
        /// <value>The stroke thickness. The default is <c>1.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the length of the open/close tick marks.
        /// </summary>
        /// <value>The tick length. The default is <c>4.0</c>.</value>
        public double TickLength
        {
            get => (double)this.GetValue(TickLengthProperty);
            set => this.SetValue(TickLengthProperty, value);
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
        /// Gets or sets the data field for the X (time) value.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldX
        {
            get => (string)this.GetValue(DataFieldXProperty);
            set => this.SetValue(DataFieldXProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for the High value.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldHigh
        {
            get => (string)this.GetValue(DataFieldHighProperty);
            set => this.SetValue(DataFieldHighProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for the Low value.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldLow
        {
            get => (string)this.GetValue(DataFieldLowProperty);
            set => this.SetValue(DataFieldLowProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for the Open value.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldOpen
        {
            get => (string)this.GetValue(DataFieldOpenProperty);
            set => this.SetValue(DataFieldOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for the Close value.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldClose
        {
            get => (string)this.GetValue(DataFieldCloseProperty);
            set => this.SetValue(DataFieldCloseProperty, value);
        }

        /// <summary>
        /// Gets or sets the mapping function from data items to high-low items.
        /// </summary>
        /// <value>The mapping function. The default is <c>null</c>.</value>
        public Func<object, HighLowItem> Mapping
        {
            get => (Func<object, HighLowItem>)this.GetValue(MappingProperty);
            set => this.SetValue(MappingProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.HighLowSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.HighLowSeries s)
            {
                s.Color = this.Color.ToOxyColor();
                s.StrokeThickness = this.StrokeThickness;
                s.TickLength = this.TickLength;
                s.LineStyle = this.LineStyle;
                s.DataFieldX = this.DataFieldX;
                s.DataFieldHigh = this.DataFieldHigh;
                s.DataFieldLow = this.DataFieldLow;
                s.DataFieldOpen = this.DataFieldOpen;
                s.DataFieldClose = this.DataFieldClose;
                s.Mapping = this.Mapping;
            }
        }
    }
}
