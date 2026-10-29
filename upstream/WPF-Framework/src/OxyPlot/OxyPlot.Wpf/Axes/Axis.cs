// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Axis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides an abstract base class for WPF axis wrappers.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#nullable enable annotations
#nullable disable warnings

namespace OxyPlot.Wpf
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;

    using OxyPlot.Axes;

    /// <summary>
    /// Provides an abstract base class for WPF axis wrappers that synchronize
    /// WPF dependency properties with OxyPlot core axis objects.
    /// </summary>
    public abstract class Axis : FrameworkElement, INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when an appearance-related dependency property value changes.
        /// </summary>
        /// <remarks>
        /// This event is raised by the <see cref="AppearanceChanged"/> callback for visual
        /// properties (title, colors, grid lines, tick styles, fonts, etc.). It is not raised
        /// for data-level changes such as filter values or extra gridlines.
        /// This enables integration with <c>UndoableStateBridge</c> for non-destructive
        /// undo/redo of axis visual settings.
        /// </remarks>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets whether <see cref="PropertyChanged"/> events are suppressed.
        /// When true, no PropertyChanged events fire from this axis instance.
        /// </summary>
        /// <remarks>
        /// Use this to suppress events during bulk operations such as theme application
        /// where the undo system should not record individual changes.
        /// </remarks>
        public bool SuppressPropertyChanged { get; set; }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event if not suppressed.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (SuppressPropertyChanged) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Identifies the <see cref="AbsoluteMaximum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AbsoluteMaximumProperty = DependencyProperty.Register(
            nameof(AbsoluteMaximum), typeof(double), typeof(Axis), new PropertyMetadata(double.MaxValue, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="AbsoluteMinimum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AbsoluteMinimumProperty = DependencyProperty.Register(
            nameof(AbsoluteMinimum), typeof(double), typeof(Axis), new PropertyMetadata(double.MinValue, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Angle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
            nameof(Angle), typeof(double), typeof(Axis), new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="AxisDistance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AxisDistanceProperty = DependencyProperty.Register(
            nameof(AxisDistance), typeof(double), typeof(Axis), new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="AxisTickToLabelDistance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AxisTickToLabelDistanceProperty = DependencyProperty.Register(
            nameof(AxisTickToLabelDistance), typeof(double), typeof(Axis), new PropertyMetadata(4.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="AxisTitleDistance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AxisTitleDistanceProperty = DependencyProperty.Register(
            nameof(AxisTitleDistance), typeof(double), typeof(Axis), new PropertyMetadata(4.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="AxislineColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AxislineColorProperty = DependencyProperty.Register(
            nameof(AxislineColor), typeof(Color), typeof(Axis), new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="AxislineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AxislineStyleProperty = DependencyProperty.Register(
            nameof(AxislineStyle), typeof(LineStyle), typeof(Axis), new PropertyMetadata(LineStyle.None, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="AxislineThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AxislineThicknessProperty = DependencyProperty.Register(
            nameof(AxislineThickness), typeof(double), typeof(Axis), new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ClipTitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClipTitleProperty = DependencyProperty.Register(
            nameof(ClipTitle), typeof(bool), typeof(Axis), new PropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="EndPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EndPositionProperty = DependencyProperty.Register(
            nameof(EndPosition), typeof(double), typeof(Axis), new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ExtraGridlineColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExtraGridlineColorProperty = DependencyProperty.Register(
            nameof(ExtraGridlineColor), typeof(Color), typeof(Axis), new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ExtraGridlineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExtraGridlineStyleProperty = DependencyProperty.Register(
            nameof(ExtraGridlineStyle), typeof(LineStyle), typeof(Axis), new PropertyMetadata(LineStyle.Solid, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ExtraGridlineThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExtraGridlineThicknessProperty = DependencyProperty.Register(
            nameof(ExtraGridlineThickness), typeof(double), typeof(Axis), new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ExtraGridlines"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExtraGridlinesProperty = DependencyProperty.Register(
            nameof(ExtraGridlines), typeof(double[]), typeof(Axis), new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="FilterFunction"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilterFunctionProperty = DependencyProperty.Register(
            nameof(FilterFunction), typeof(Func<double, bool>), typeof(Axis), new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="FilterMaxValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilterMaxValueProperty = DependencyProperty.Register(
            nameof(FilterMaxValue), typeof(double), typeof(Axis), new PropertyMetadata(double.MaxValue, DataChanged));

        /// <summary>
        /// Identifies the <see cref="FilterMinValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilterMinValueProperty = DependencyProperty.Register(
            nameof(FilterMinValue), typeof(double), typeof(Axis), new PropertyMetadata(double.MinValue, DataChanged));

        /// <summary>
        /// Identifies the <see cref="Font"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontProperty = DependencyProperty.Register(
            nameof(Font), typeof(string), typeof(Axis), new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="FontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            nameof(FontSize), typeof(double), typeof(Axis), new PropertyMetadata(double.NaN, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="FontWeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(
            nameof(FontWeight), typeof(FontWeight), typeof(Axis), new PropertyMetadata(FontWeights.Normal, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="IntervalLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IntervalLengthProperty = DependencyProperty.Register(
            nameof(IntervalLength), typeof(double), typeof(Axis), new PropertyMetadata(60.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="IsAxisVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAxisVisibleProperty = DependencyProperty.Register(
            nameof(IsAxisVisible), typeof(bool), typeof(Axis), new PropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="IsPanEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPanEnabledProperty = DependencyProperty.Register(
            nameof(IsPanEnabled), typeof(bool), typeof(Axis), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="IsZoomEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsZoomEnabledProperty = DependencyProperty.Register(
            nameof(IsZoomEnabled), typeof(bool), typeof(Axis), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="Key"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
            nameof(Key), typeof(string), typeof(Axis), new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="LabelFormatter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFormatterProperty = DependencyProperty.Register(
            nameof(LabelFormatter), typeof(Func<double, string>), typeof(Axis), new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Layer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LayerProperty = DependencyProperty.Register(
            nameof(Layer), typeof(AxisLayer), typeof(Axis), new PropertyMetadata(AxisLayer.BelowSeries, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MajorGridlineColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MajorGridlineColorProperty = DependencyProperty.Register(
            nameof(MajorGridlineColor), typeof(Color), typeof(Axis), new PropertyMetadata(Color.FromArgb(0x40, 0, 0, 0), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MajorGridlineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MajorGridlineStyleProperty = DependencyProperty.Register(
            nameof(MajorGridlineStyle), typeof(LineStyle), typeof(Axis), new PropertyMetadata(LineStyle.None, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MajorGridlineThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MajorGridlineThicknessProperty = DependencyProperty.Register(
            nameof(MajorGridlineThickness), typeof(double), typeof(Axis), new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MajorStep"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MajorStepProperty = DependencyProperty.Register(
            nameof(MajorStep), typeof(double), typeof(Axis), new PropertyMetadata(double.NaN, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MajorTickSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MajorTickSizeProperty = DependencyProperty.Register(
            nameof(MajorTickSize), typeof(double), typeof(Axis), new PropertyMetadata(7.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Maximum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum), typeof(double), typeof(Axis), new PropertyMetadata(double.NaN, MinMaxChanged));

        /// <summary>
        /// Identifies the <see cref="MaximumPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumPaddingProperty = DependencyProperty.Register(
            nameof(MaximumPadding), typeof(double), typeof(Axis), new PropertyMetadata(0.01, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MaximumRange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumRangeProperty = DependencyProperty.Register(
            nameof(MaximumRange), typeof(double), typeof(Axis), new PropertyMetadata(double.PositiveInfinity, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Minimum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum), typeof(double), typeof(Axis), new PropertyMetadata(double.NaN, MinMaxChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumPaddingProperty = DependencyProperty.Register(
            nameof(MinimumPadding), typeof(double), typeof(Axis), new PropertyMetadata(0.01, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumRange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumRangeProperty = DependencyProperty.Register(
            nameof(MinimumRange), typeof(double), typeof(Axis), new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinorGridlineColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinorGridlineColorProperty = DependencyProperty.Register(
            nameof(MinorGridlineColor), typeof(Color), typeof(Axis), new PropertyMetadata(Color.FromArgb(0x20, 0, 0, 0), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinorGridlineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinorGridlineStyleProperty = DependencyProperty.Register(
            nameof(MinorGridlineStyle), typeof(LineStyle), typeof(Axis), new PropertyMetadata(LineStyle.None, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinorGridlineThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinorGridlineThicknessProperty = DependencyProperty.Register(
            nameof(MinorGridlineThickness), typeof(double), typeof(Axis), new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinorStep"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinorStepProperty = DependencyProperty.Register(
            nameof(MinorStep), typeof(double), typeof(Axis), new PropertyMetadata(double.NaN, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinorTickSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinorTickSizeProperty = DependencyProperty.Register(
            nameof(MinorTickSize), typeof(double), typeof(Axis), new PropertyMetadata(4.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Position"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position), typeof(AxisPosition), typeof(Axis), new PropertyMetadata(AxisPosition.Left, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="PositionAtZeroCrossing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PositionAtZeroCrossingProperty = DependencyProperty.Register(
            nameof(PositionAtZeroCrossing), typeof(bool), typeof(Axis), new PropertyMetadata(false, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="PositionTier"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PositionTierProperty = DependencyProperty.Register(
            nameof(PositionTier), typeof(int), typeof(Axis), new PropertyMetadata(0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StartPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartPositionProperty = DependencyProperty.Register(
            nameof(StartPosition), typeof(double), typeof(Axis), new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StringFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register(
            nameof(StringFormat), typeof(string), typeof(Axis), new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TextColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextColorProperty = DependencyProperty.Register(
            nameof(TextColor), typeof(Color), typeof(Axis), new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TicklineColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TicklineColorProperty = DependencyProperty.Register(
            nameof(TicklineColor), typeof(Color), typeof(Axis), new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TickStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickStyleProperty = DependencyProperty.Register(
            nameof(TickStyle), typeof(TickStyle), typeof(Axis), new PropertyMetadata(TickStyle.Outside, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(Axis), new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TitleClippingLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleClippingLengthProperty = DependencyProperty.Register(
            nameof(TitleClippingLength), typeof(double), typeof(Axis), new PropertyMetadata(0.9, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TitleColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleColorProperty = DependencyProperty.Register(
            nameof(TitleColor), typeof(Color), typeof(Axis), new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TitleFont"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleFontProperty = DependencyProperty.Register(
            nameof(TitleFont), typeof(string), typeof(Axis), new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TitleFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleFontSizeProperty = DependencyProperty.Register(
            nameof(TitleFontSize), typeof(double), typeof(Axis), new PropertyMetadata(double.NaN, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TitleFontWeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleFontWeightProperty = DependencyProperty.Register(
            nameof(TitleFontWeight), typeof(FontWeight), typeof(Axis), new PropertyMetadata(FontWeights.Normal, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TitleFormatString"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleFormatStringProperty = DependencyProperty.Register(
            nameof(TitleFormatString), typeof(string), typeof(Axis), new PropertyMetadata("{0} [{1}]", AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TitlePosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitlePositionProperty = DependencyProperty.Register(
            nameof(TitlePosition), typeof(double), typeof(Axis), new PropertyMetadata(0.5, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Unit"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
            nameof(Unit), typeof(string), typeof(Axis), new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="UseSuperExponentialFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UseSuperExponentialFormatProperty = DependencyProperty.Register(
            nameof(UseSuperExponentialFormat), typeof(bool), typeof(Axis), new PropertyMetadata(false, AppearanceChanged));

        /// <summary>
        /// Gets or sets the internal OxyPlot axis model.
        /// </summary>
        public OxyPlot.Axes.Axis InternalAxis { get; protected set; }

        /// <summary>
        /// Gets or sets the absolute maximum value. Cannot zoom/pan beyond this limit.
        /// </summary>
        public double AbsoluteMaximum
        {
            get => (double)this.GetValue(AbsoluteMaximumProperty);
            set => this.SetValue(AbsoluteMaximumProperty, value);
        }

        /// <summary>
        /// Gets or sets the absolute minimum value. Cannot zoom/pan beyond this limit.
        /// </summary>
        public double AbsoluteMinimum
        {
            get => (double)this.GetValue(AbsoluteMinimumProperty);
            set => this.SetValue(AbsoluteMinimumProperty, value);
        }

        /// <summary>
        /// Gets the actual maximum value after rendering.
        /// </summary>
        public double ActualMaximum => this.InternalAxis?.ActualMaximum ?? double.NaN;

        /// <summary>
        /// Gets the actual minimum value after rendering.
        /// </summary>
        public double ActualMinimum => this.InternalAxis?.ActualMinimum ?? double.NaN;

        /// <summary>
        /// Gets or sets the angle for axis labels in degrees.
        /// </summary>
        public double Angle
        {
            get => (double)this.GetValue(AngleProperty);
            set => this.SetValue(AngleProperty, value);
        }

        /// <summary>
        /// Gets or sets the distance from the plot area to the axis.
        /// </summary>
        public double AxisDistance
        {
            get => (double)this.GetValue(AxisDistanceProperty);
            set => this.SetValue(AxisDistanceProperty, value);
        }

        /// <summary>
        /// Gets or sets the distance from tick marks to labels.
        /// </summary>
        public double AxisTickToLabelDistance
        {
            get => (double)this.GetValue(AxisTickToLabelDistanceProperty);
            set => this.SetValue(AxisTickToLabelDistanceProperty, value);
        }

        /// <summary>
        /// Gets or sets the distance from axis line to title.
        /// </summary>
        public double AxisTitleDistance
        {
            get => (double)this.GetValue(AxisTitleDistanceProperty);
            set => this.SetValue(AxisTitleDistanceProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of the axis line.
        /// </summary>
        public Color AxislineColor
        {
            get => (Color)this.GetValue(AxislineColorProperty);
            set => this.SetValue(AxislineColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the style of the axis line.
        /// </summary>
        public LineStyle AxislineStyle
        {
            get => (LineStyle)this.GetValue(AxislineStyleProperty);
            set => this.SetValue(AxislineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the thickness of the axis line.
        /// </summary>
        public double AxislineThickness
        {
            get => (double)this.GetValue(AxislineThicknessProperty);
            set => this.SetValue(AxislineThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to clip the title.
        /// </summary>
        public bool ClipTitle
        {
            get => (bool)this.GetValue(ClipTitleProperty);
            set => this.SetValue(ClipTitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the end position (0-1) of the axis on the plot area.
        /// </summary>
        public double EndPosition
        {
            get => (double)this.GetValue(EndPositionProperty);
            set => this.SetValue(EndPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of extra gridlines.
        /// </summary>
        public Color ExtraGridlineColor
        {
            get => (Color)this.GetValue(ExtraGridlineColorProperty);
            set => this.SetValue(ExtraGridlineColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the style of extra gridlines.
        /// </summary>
        public LineStyle ExtraGridlineStyle
        {
            get => (LineStyle)this.GetValue(ExtraGridlineStyleProperty);
            set => this.SetValue(ExtraGridlineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the thickness of extra gridlines.
        /// </summary>
        public double ExtraGridlineThickness
        {
            get => (double)this.GetValue(ExtraGridlineThicknessProperty);
            set => this.SetValue(ExtraGridlineThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the values for extra gridlines.
        /// </summary>
        public double[] ExtraGridlines
        {
            get => (double[])this.GetValue(ExtraGridlinesProperty);
            set => this.SetValue(ExtraGridlinesProperty, value);
        }

        /// <summary>
        /// Gets or sets the filter function for tick values.
        /// </summary>
        public Func<double, bool> FilterFunction
        {
            get => (Func<double, bool>)this.GetValue(FilterFunctionProperty);
            set => this.SetValue(FilterFunctionProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum value for the filter.
        /// </summary>
        public double FilterMaxValue
        {
            get => (double)this.GetValue(FilterMaxValueProperty);
            set => this.SetValue(FilterMaxValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum value for the filter.
        /// </summary>
        public double FilterMinValue
        {
            get => (double)this.GetValue(FilterMinValueProperty);
            set => this.SetValue(FilterMinValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the font name for axis labels.
        /// </summary>
        public string Font
        {
            get => (string)this.GetValue(FontProperty);
            set => this.SetValue(FontProperty, value);
        }

        /// <summary>
        /// Gets or sets the font size for axis labels.
        /// </summary>
        public double FontSize
        {
            get => (double)this.GetValue(FontSizeProperty);
            set => this.SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the font weight for axis labels.
        /// </summary>
        public FontWeight FontWeight
        {
            get => (FontWeight)this.GetValue(FontWeightProperty);
            set => this.SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the desired interval length in pixels.
        /// </summary>
        public double IntervalLength
        {
            get => (double)this.GetValue(IntervalLengthProperty);
            set => this.SetValue(IntervalLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the axis is visible.
        /// </summary>
        public bool IsAxisVisible
        {
            get => (bool)this.GetValue(IsAxisVisibleProperty);
            set => this.SetValue(IsAxisVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether panning is enabled.
        /// </summary>
        public bool IsPanEnabled
        {
            get => (bool)this.GetValue(IsPanEnabledProperty);
            set => this.SetValue(IsPanEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether zooming is enabled.
        /// </summary>
        public bool IsZoomEnabled
        {
            get => (bool)this.GetValue(IsZoomEnabledProperty);
            set => this.SetValue(IsZoomEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the axis key for identification.
        /// </summary>
        public string Key
        {
            get => (string)this.GetValue(KeyProperty);
            set => this.SetValue(KeyProperty, value);
        }

        /// <summary>
        /// Gets or sets the label formatter function.
        /// </summary>
        public Func<double, string> LabelFormatter
        {
            get => (Func<double, string>)this.GetValue(LabelFormatterProperty);
            set => this.SetValue(LabelFormatterProperty, value);
        }

        /// <summary>
        /// Gets or sets the axis layer (above or below series).
        /// </summary>
        public AxisLayer Layer
        {
            get => (AxisLayer)this.GetValue(LayerProperty);
            set => this.SetValue(LayerProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of major gridlines.
        /// </summary>
        public Color MajorGridlineColor
        {
            get => (Color)this.GetValue(MajorGridlineColorProperty);
            set => this.SetValue(MajorGridlineColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the style of major gridlines.
        /// </summary>
        public LineStyle MajorGridlineStyle
        {
            get => (LineStyle)this.GetValue(MajorGridlineStyleProperty);
            set => this.SetValue(MajorGridlineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the thickness of major gridlines.
        /// </summary>
        public double MajorGridlineThickness
        {
            get => (double)this.GetValue(MajorGridlineThicknessProperty);
            set => this.SetValue(MajorGridlineThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the interval between major ticks.
        /// </summary>
        public double MajorStep
        {
            get => (double)this.GetValue(MajorStepProperty);
            set => this.SetValue(MajorStepProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of major tick marks.
        /// </summary>
        public double MajorTickSize
        {
            get => (double)this.GetValue(MajorTickSizeProperty);
            set => this.SetValue(MajorTickSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum value of the axis.
        /// </summary>
        public double Maximum
        {
            get => (double)this.GetValue(MaximumProperty);
            set => this.SetValue(MaximumProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum padding.
        /// </summary>
        public double MaximumPadding
        {
            get => (double)this.GetValue(MaximumPaddingProperty);
            set => this.SetValue(MaximumPaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum range (zoom limit).
        /// </summary>
        public double MaximumRange
        {
            get => (double)this.GetValue(MaximumRangeProperty);
            set => this.SetValue(MaximumRangeProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum value of the axis.
        /// </summary>
        public double Minimum
        {
            get => (double)this.GetValue(MinimumProperty);
            set => this.SetValue(MinimumProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum padding.
        /// </summary>
        public double MinimumPadding
        {
            get => (double)this.GetValue(MinimumPaddingProperty);
            set => this.SetValue(MinimumPaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum range (zoom limit).
        /// </summary>
        public double MinimumRange
        {
            get => (double)this.GetValue(MinimumRangeProperty);
            set => this.SetValue(MinimumRangeProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of minor gridlines.
        /// </summary>
        public Color MinorGridlineColor
        {
            get => (Color)this.GetValue(MinorGridlineColorProperty);
            set => this.SetValue(MinorGridlineColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the style of minor gridlines.
        /// </summary>
        public LineStyle MinorGridlineStyle
        {
            get => (LineStyle)this.GetValue(MinorGridlineStyleProperty);
            set => this.SetValue(MinorGridlineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the thickness of minor gridlines.
        /// </summary>
        public double MinorGridlineThickness
        {
            get => (double)this.GetValue(MinorGridlineThicknessProperty);
            set => this.SetValue(MinorGridlineThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the interval between minor ticks.
        /// </summary>
        public double MinorStep
        {
            get => (double)this.GetValue(MinorStepProperty);
            set => this.SetValue(MinorStepProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of minor tick marks.
        /// </summary>
        public double MinorTickSize
        {
            get => (double)this.GetValue(MinorTickSizeProperty);
            set => this.SetValue(MinorTickSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the position of the axis.
        /// </summary>
        public AxisPosition Position
        {
            get => (AxisPosition)this.GetValue(PositionProperty);
            set => this.SetValue(PositionProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to position at zero crossing.
        /// </summary>
        public bool PositionAtZeroCrossing
        {
            get => (bool)this.GetValue(PositionAtZeroCrossingProperty);
            set => this.SetValue(PositionAtZeroCrossingProperty, value);
        }

        /// <summary>
        /// Gets or sets the position tier for multiple axes.
        /// </summary>
        public int PositionTier
        {
            get => (int)this.GetValue(PositionTierProperty);
            set => this.SetValue(PositionTierProperty, value);
        }

        /// <summary>
        /// Gets or sets the start position (0-1) of the axis on the plot area.
        /// </summary>
        public double StartPosition
        {
            get => (double)this.GetValue(StartPositionProperty);
            set => this.SetValue(StartPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the string format for axis labels.
        /// </summary>
        public string StringFormat
        {
            get => (string)this.GetValue(StringFormatProperty);
            set => this.SetValue(StringFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets the text color for axis labels.
        /// </summary>
        public Color TextColor
        {
            get => (Color)this.GetValue(TextColorProperty);
            set => this.SetValue(TextColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of tick lines.
        /// </summary>
        public Color TicklineColor
        {
            get => (Color)this.GetValue(TicklineColorProperty);
            set => this.SetValue(TicklineColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the tick style.
        /// </summary>
        public TickStyle TickStyle
        {
            get => (TickStyle)this.GetValue(TickStyleProperty);
            set => this.SetValue(TickStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the axis title.
        /// </summary>
        public string Title
        {
            get => (string)this.GetValue(TitleProperty);
            set => this.SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the title clipping length as a fraction.
        /// </summary>
        public double TitleClippingLength
        {
            get => (double)this.GetValue(TitleClippingLengthProperty);
            set => this.SetValue(TitleClippingLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of the title.
        /// </summary>
        public Color TitleColor
        {
            get => (Color)this.GetValue(TitleColorProperty);
            set => this.SetValue(TitleColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the title font name.
        /// </summary>
        public string TitleFont
        {
            get => (string)this.GetValue(TitleFontProperty);
            set => this.SetValue(TitleFontProperty, value);
        }

        /// <summary>
        /// Gets or sets the title font size.
        /// </summary>
        public double TitleFontSize
        {
            get => (double)this.GetValue(TitleFontSizeProperty);
            set => this.SetValue(TitleFontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the title font weight.
        /// </summary>
        public FontWeight TitleFontWeight
        {
            get => (FontWeight)this.GetValue(TitleFontWeightProperty);
            set => this.SetValue(TitleFontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the title format string.
        /// </summary>
        public string TitleFormatString
        {
            get => (string)this.GetValue(TitleFormatStringProperty);
            set => this.SetValue(TitleFormatStringProperty, value);
        }

        /// <summary>
        /// Gets or sets the title position (0-1).
        /// </summary>
        public double TitlePosition
        {
            get => (double)this.GetValue(TitlePositionProperty);
            set => this.SetValue(TitlePositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the unit label.
        /// </summary>
        public string Unit
        {
            get => (string)this.GetValue(UnitProperty);
            set => this.SetValue(UnitProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use super exponential format.
        /// </summary>
        public bool UseSuperExponentialFormat
        {
            get => (bool)this.GetValue(UseSuperExponentialFormatProperty);
            set => this.SetValue(UseSuperExponentialFormatProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The OxyPlot axis model.</returns>
        public abstract OxyPlot.Axes.Axis CreateModel();

        /// <summary>
        /// Handles changes to appearance-related properties.
        /// </summary>
        protected static void AppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = (Axis)d;
            if (axis.SuppressPropertyChanged) return;
            axis.OnVisualChanged();
            axis.OnPropertyChanged(e.Property.Name);
        }

        /// <summary>
        /// Handles changes to the Minimum/Maximum properties.
        /// Resets zoom state when user explicitly sets a value, then triggers a synchronized re-render.
        /// </summary>
        /// <remarks>
        /// Min/Max is an axis-range change, not a data change. Previously this called
        /// <see cref="OnDataChanged"/> which routed through <c>InvalidatePlot()</c> (default
        /// <c>updateData=true</c>) — that walked every visible series via
        /// <c>series.UpdateData()</c> for what is purely a range adjustment. Routing through
        /// <see cref="OnVisualChanged"/> instead sets <c>_needsSynchronization = true</c> (so
        /// the next sync pushes the new Min/Max into the internal axis) and then issues
        /// <c>InvalidatePlot(false)</c>, skipping the per-series data walk.
        /// </remarks>
        private static void MinMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = (Axis)d;
            if (axis.SuppressPropertyChanged) return;

            // When user explicitly sets a numeric value (not NaN), clear zoom state
            // so the base class uses their value instead of ViewMin/ViewMax from a prior zoom
            if (!double.IsNaN((double)e.NewValue) && axis.InternalAxis != null)
            {
                axis.InternalAxis.Reset();
            }

            axis.OnVisualChanged();
        }

        /// <summary>
        /// Handles changes to data-related properties.
        /// </summary>
        protected static void DataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = (Axis)d;
            if (axis.SuppressPropertyChanged) return;
            axis.OnDataChanged();
        }

        /// <summary>
        /// Called when data has changed.
        /// </summary>
        protected void OnDataChanged()
        {
            var plot = this.Parent as IPlotView;
            plot?.InvalidatePlot();
        }

        /// <summary>
        /// Called when visual appearance has changed.
        /// </summary>
        protected void OnVisualChanged()
        {
            // Load-bearing ordering: _needsSynchronization MUST be set before InvalidatePlot.
            // If suppression is active, Plot.InvalidatePlot short-circuits at the gate but the
            // _needsSynchronization=true survives, so the next non-gated invalidation runs the
            // sync that picks up this axis's WPF DP change. Reordering (or omitting) the flag
            // would silently lose the visual update across a suppression window.
            // OnVisualChanged is only called from WPF DP change callbacks (not zoom/pan),
            // so this does not affect zoom/pan performance.
            if (this.Parent is Plot plot)
            {
                plot._needsSynchronization = true;
                plot.InvalidatePlot(false);
            }
        }

        /// <summary>
        /// Synchronizes the WPF properties to the internal OxyPlot axis.
        /// </summary>
        protected virtual void SynchronizeProperties()
        {
            var a = this.InternalAxis;
            if (a == null)
            {
                return;
            }

            a.AbsoluteMaximum = this.AbsoluteMaximum;
            a.AbsoluteMinimum = this.AbsoluteMinimum;
            a.Angle = this.Angle;
            a.AxisDistance = this.AxisDistance;
            a.AxislineColor = this.AxislineColor.ToOxyColor();
            a.AxislineStyle = this.AxislineStyle;
            a.AxislineThickness = this.AxislineThickness;
            a.AxisTitleDistance = this.AxisTitleDistance;
            a.AxisTickToLabelDistance = this.AxisTickToLabelDistance;
            a.ClipTitle = this.ClipTitle;
            a.EndPosition = this.EndPosition;
            a.ExtraGridlineColor = this.ExtraGridlineColor.ToOxyColor();
            a.ExtraGridlineStyle = this.ExtraGridlineStyle;
            a.ExtraGridlineThickness = this.ExtraGridlineThickness;
            a.ExtraGridlines = this.ExtraGridlines;
            a.FilterFunction = this.FilterFunction;
            a.FilterMaxValue = this.FilterMaxValue;
            a.FilterMinValue = this.FilterMinValue;
            a.Font = this.Font;
            a.FontSize = this.FontSize;
            a.FontWeight = this.FontWeight.ToOpenTypeWeight();
            a.IntervalLength = this.IntervalLength;
            a.IsAxisVisible = this.IsAxisVisible;
            a.IsPanEnabled = this.IsPanEnabled;
            a.IsZoomEnabled = this.IsZoomEnabled;
            a.Key = this.Key;
            a.LabelFormatter = this.LabelFormatter;
            a.Layer = this.Layer;
            a.MajorGridlineColor = this.MajorGridlineColor.ToOxyColor();
            a.MajorGridlineStyle = this.MajorGridlineStyle;
            a.MajorGridlineThickness = this.MajorGridlineThickness;
            a.MajorStep = this.MajorStep;
            a.MajorTickSize = this.MajorTickSize;
            a.Maximum = this.Maximum;
            a.MaximumPadding = this.MaximumPadding;
            a.MaximumRange = this.MaximumRange;
            a.Minimum = this.Minimum;
            a.MinimumPadding = this.MinimumPadding;
            a.MinimumRange = this.MinimumRange;
            a.MinorGridlineColor = this.MinorGridlineColor.ToOxyColor();
            a.MinorGridlineStyle = this.MinorGridlineStyle;
            a.MinorGridlineThickness = this.MinorGridlineThickness;
            a.MinorStep = this.MinorStep;
            a.MinorTickSize = this.MinorTickSize;
            a.Position = this.Position;
            a.PositionAtZeroCrossing = this.PositionAtZeroCrossing;
            a.PositionTier = this.PositionTier;
            a.StartPosition = this.StartPosition;
            a.StringFormat = this.StringFormat;
            a.TextColor = this.TextColor.ToOxyColor();
            a.TicklineColor = this.TicklineColor.ToOxyColor();
            a.TickStyle = this.TickStyle;
            a.Title = this.Title;
            a.TitleClippingLength = this.TitleClippingLength;
            a.TitleColor = this.TitleColor.ToOxyColor();
            a.TitleFont = this.TitleFont;
            a.TitleFontSize = this.TitleFontSize;
            a.TitleFontWeight = this.TitleFontWeight.ToOpenTypeWeight();
            a.TitleFormatString = this.TitleFormatString;
            a.TitlePosition = this.TitlePosition;
            a.Unit = this.Unit;
            a.UseSuperExponentialFormat = this.UseSuperExponentialFormat;
        }
    }
}
