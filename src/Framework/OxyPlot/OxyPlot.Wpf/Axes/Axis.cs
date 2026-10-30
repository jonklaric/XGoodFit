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
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Media;

    using OxyPlot.Axes;

    /// <summary>
    /// Provides an abstract base class for WPF axis wrappers that synchronize
    /// WPF dependency properties with OxyPlot core axis objects.
    /// </summary>
    /// <remarks>
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; an Avalonia
    /// <c>Control</c>, where WPF's was a <c>FrameworkElement</c>; <c>PropertyChanged</c> hides Avalonia's
    /// own property-changed event of the same name, and is this class's INotifyPropertyChanged event, as in
    /// WPF.
    /// </remarks>
    public abstract class Axis : Control, INotifyPropertyChanged
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
        public new event PropertyChangedEventHandler? PropertyChanged;

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
        public static readonly StyledProperty<double> AbsoluteMaximumProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(AbsoluteMaximum),
            double.MaxValue);

        /// <summary>
        /// Identifies the <see cref="AbsoluteMinimum"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> AbsoluteMinimumProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(AbsoluteMinimum),
            double.MinValue);

        /// <summary>
        /// Identifies the <see cref="Angle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> AngleProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(Angle),
            0.0);

        /// <summary>
        /// Identifies the <see cref="AxisDistance"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> AxisDistanceProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(AxisDistance),
            0.0);

        /// <summary>
        /// Identifies the <see cref="AxisTickToLabelDistance"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> AxisTickToLabelDistanceProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(AxisTickToLabelDistance),
            4.0);

        /// <summary>
        /// Identifies the <see cref="AxisTitleDistance"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> AxisTitleDistanceProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(AxisTitleDistance),
            4.0);

        /// <summary>
        /// Identifies the <see cref="AxislineColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> AxislineColorProperty = AvaloniaProperty.Register<Axis, Color>(
            nameof(AxislineColor),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="AxislineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> AxislineStyleProperty = AvaloniaProperty.Register<Axis, LineStyle>(
            nameof(AxislineStyle),
            LineStyle.None);

        /// <summary>
        /// Identifies the <see cref="AxislineThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> AxislineThicknessProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(AxislineThickness),
            1.0);

        /// <summary>
        /// Identifies the <see cref="ClipTitle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> ClipTitleProperty = AvaloniaProperty.Register<Axis, bool>(
            nameof(ClipTitle),
            true);

        /// <summary>
        /// Identifies the <see cref="EndPosition"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> EndPositionProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(EndPosition),
            1.0);

        /// <summary>
        /// Identifies the <see cref="ExtraGridlineColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> ExtraGridlineColorProperty = AvaloniaProperty.Register<Axis, Color>(
            nameof(ExtraGridlineColor),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="ExtraGridlineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> ExtraGridlineStyleProperty = AvaloniaProperty.Register<Axis, LineStyle>(
            nameof(ExtraGridlineStyle),
            LineStyle.Solid);

        /// <summary>
        /// Identifies the <see cref="ExtraGridlineThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ExtraGridlineThicknessProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(ExtraGridlineThickness),
            1.0);

        /// <summary>
        /// Identifies the <see cref="ExtraGridlines"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double[]> ExtraGridlinesProperty = AvaloniaProperty.Register<Axis, double[]>(
            nameof(ExtraGridlines),
            null);

        /// <summary>
        /// Identifies the <see cref="FilterFunction"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Func<double, bool>> FilterFunctionProperty = AvaloniaProperty.Register<Axis, Func<double, bool>>(
            nameof(FilterFunction),
            null);

        /// <summary>
        /// Identifies the <see cref="FilterMaxValue"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> FilterMaxValueProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(FilterMaxValue),
            double.MaxValue);

        /// <summary>
        /// Identifies the <see cref="FilterMinValue"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> FilterMinValueProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(FilterMinValue),
            double.MinValue);

        /// <summary>
        /// Identifies the <see cref="Font"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> FontProperty = AvaloniaProperty.Register<Axis, string>(
            nameof(Font),
            null);

        /// <summary>
        /// Identifies the <see cref="FontSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> FontSizeProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(FontSize),
            double.NaN);

        /// <summary>
        /// Identifies the <see cref="FontWeight"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> FontWeightProperty = AvaloniaProperty.Register<Axis, FontWeight>(
            nameof(FontWeight),
            FontWeight.Normal);

        /// <summary>
        /// Identifies the <see cref="IntervalLength"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> IntervalLengthProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(IntervalLength),
            60.0);

        /// <summary>
        /// Identifies the <see cref="IsAxisVisible"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsAxisVisibleProperty = AvaloniaProperty.Register<Axis, bool>(
            nameof(IsAxisVisible),
            true);

        /// <summary>
        /// Identifies the <see cref="IsPanEnabled"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsPanEnabledProperty = AvaloniaProperty.Register<Axis, bool>(
            nameof(IsPanEnabled),
            true);

        /// <summary>
        /// Identifies the <see cref="IsZoomEnabled"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsZoomEnabledProperty = AvaloniaProperty.Register<Axis, bool>(
            nameof(IsZoomEnabled),
            true);

        /// <summary>
        /// Identifies the <see cref="Key"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> KeyProperty = AvaloniaProperty.Register<Axis, string>(
            nameof(Key),
            null);

        /// <summary>
        /// Identifies the <see cref="LabelFormatter"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Func<double, string>> LabelFormatterProperty = AvaloniaProperty.Register<Axis, Func<double, string>>(
            nameof(LabelFormatter),
            null);

        /// <summary>
        /// Identifies the <see cref="Layer"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<AxisLayer> LayerProperty = AvaloniaProperty.Register<Axis, AxisLayer>(
            nameof(Layer),
            AxisLayer.BelowSeries);

        /// <summary>
        /// Identifies the <see cref="MajorGridlineColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> MajorGridlineColorProperty = AvaloniaProperty.Register<Axis, Color>(
            nameof(MajorGridlineColor),
            Color.FromArgb(0x40, 0, 0, 0));

        /// <summary>
        /// Identifies the <see cref="MajorGridlineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> MajorGridlineStyleProperty = AvaloniaProperty.Register<Axis, LineStyle>(
            nameof(MajorGridlineStyle),
            LineStyle.None);

        /// <summary>
        /// Identifies the <see cref="MajorGridlineThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MajorGridlineThicknessProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(MajorGridlineThickness),
            1.0);

        /// <summary>
        /// Identifies the <see cref="MajorStep"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MajorStepProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(MajorStep),
            double.NaN);

        /// <summary>
        /// Identifies the <see cref="MajorTickSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MajorTickSizeProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(MajorTickSize),
            7.0);

        /// <summary>
        /// Identifies the <see cref="Maximum"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(Maximum),
            double.NaN);

        /// <summary>
        /// Identifies the <see cref="MaximumPadding"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumPaddingProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(MaximumPadding),
            0.01);

        /// <summary>
        /// Identifies the <see cref="MaximumRange"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumRangeProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(MaximumRange),
            double.PositiveInfinity);

        /// <summary>
        /// Identifies the <see cref="Minimum"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(Minimum),
            double.NaN);

        /// <summary>
        /// Identifies the <see cref="MinimumPadding"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumPaddingProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(MinimumPadding),
            0.01);

        /// <summary>
        /// Identifies the <see cref="MinimumRange"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumRangeProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(MinimumRange),
            0.0);

        /// <summary>
        /// Identifies the <see cref="MinorGridlineColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> MinorGridlineColorProperty = AvaloniaProperty.Register<Axis, Color>(
            nameof(MinorGridlineColor),
            Color.FromArgb(0x20, 0, 0, 0));

        /// <summary>
        /// Identifies the <see cref="MinorGridlineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> MinorGridlineStyleProperty = AvaloniaProperty.Register<Axis, LineStyle>(
            nameof(MinorGridlineStyle),
            LineStyle.None);

        /// <summary>
        /// Identifies the <see cref="MinorGridlineThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinorGridlineThicknessProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(MinorGridlineThickness),
            1.0);

        /// <summary>
        /// Identifies the <see cref="MinorStep"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinorStepProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(MinorStep),
            double.NaN);

        /// <summary>
        /// Identifies the <see cref="MinorTickSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinorTickSizeProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(MinorTickSize),
            4.0);

        /// <summary>
        /// Identifies the <see cref="Position"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<AxisPosition> PositionProperty = AvaloniaProperty.Register<Axis, AxisPosition>(
            nameof(Position),
            AxisPosition.Left);

        /// <summary>
        /// Identifies the <see cref="PositionAtZeroCrossing"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> PositionAtZeroCrossingProperty = AvaloniaProperty.Register<Axis, bool>(
            nameof(PositionAtZeroCrossing),
            false);

        /// <summary>
        /// Identifies the <see cref="PositionTier"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<int> PositionTierProperty = AvaloniaProperty.Register<Axis, int>(
            nameof(PositionTier),
            0);

        /// <summary>
        /// Identifies the <see cref="StartPosition"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StartPositionProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(StartPosition),
            0.0);

        /// <summary>
        /// Identifies the <see cref="StringFormat"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> StringFormatProperty = AvaloniaProperty.Register<Axis, string>(
            nameof(StringFormat),
            null);

        /// <summary>
        /// Identifies the <see cref="TextColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> TextColorProperty = AvaloniaProperty.Register<Axis, Color>(
            nameof(TextColor),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="TicklineColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> TicklineColorProperty = AvaloniaProperty.Register<Axis, Color>(
            nameof(TicklineColor),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="TickStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<TickStyle> TickStyleProperty = AvaloniaProperty.Register<Axis, TickStyle>(
            nameof(TickStyle),
            TickStyle.Outside);

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<Axis, string>(
            nameof(Title),
            null);

        /// <summary>
        /// Identifies the <see cref="TitleClippingLength"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TitleClippingLengthProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(TitleClippingLength),
            0.9);

        /// <summary>
        /// Identifies the <see cref="TitleColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> TitleColorProperty = AvaloniaProperty.Register<Axis, Color>(
            nameof(TitleColor),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="TitleFont"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TitleFontProperty = AvaloniaProperty.Register<Axis, string>(
            nameof(TitleFont),
            null);

        /// <summary>
        /// Identifies the <see cref="TitleFontSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TitleFontSizeProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(TitleFontSize),
            double.NaN);

        /// <summary>
        /// Identifies the <see cref="TitleFontWeight"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> TitleFontWeightProperty = AvaloniaProperty.Register<Axis, FontWeight>(
            nameof(TitleFontWeight),
            FontWeight.Normal);

        /// <summary>
        /// Identifies the <see cref="TitleFormatString"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TitleFormatStringProperty = AvaloniaProperty.Register<Axis, string>(
            nameof(TitleFormatString),
            "{0} [{1}]");

        /// <summary>
        /// Identifies the <see cref="TitlePosition"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TitlePositionProperty = AvaloniaProperty.Register<Axis, double>(
            nameof(TitlePosition),
            0.5);

        /// <summary>
        /// Identifies the <see cref="Unit"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> UnitProperty = AvaloniaProperty.Register<Axis, string>(
            nameof(Unit),
            null);

        /// <summary>
        /// Identifies the <see cref="UseSuperExponentialFormat"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> UseSuperExponentialFormatProperty = AvaloniaProperty.Register<Axis, bool>(
            nameof(UseSuperExponentialFormat),
            false);

        /// <summary>
        /// Initializes static members of the <see cref="Axis" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static Axis()
        {
            AbsoluteMaximumProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            AbsoluteMinimumProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            AngleProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            AxisDistanceProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            AxisTickToLabelDistanceProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            AxisTitleDistanceProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            AxislineColorProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            AxislineStyleProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            AxislineThicknessProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            ClipTitleProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            EndPositionProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            ExtraGridlineColorProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            ExtraGridlineStyleProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            ExtraGridlineThicknessProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            ExtraGridlinesProperty.Changed.AddClassHandler<Axis>(DataChanged);
            FilterFunctionProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            FilterMaxValueProperty.Changed.AddClassHandler<Axis>(DataChanged);
            FilterMinValueProperty.Changed.AddClassHandler<Axis>(DataChanged);
            FontProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            FontSizeProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            FontWeightProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            IntervalLengthProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            IsAxisVisibleProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            KeyProperty.Changed.AddClassHandler<Axis>(DataChanged);
            LabelFormatterProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            LayerProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MajorGridlineColorProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MajorGridlineStyleProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MajorGridlineThicknessProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MajorStepProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MajorTickSizeProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MaximumProperty.Changed.AddClassHandler<Axis>(MinMaxChanged);
            MaximumPaddingProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MaximumRangeProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MinimumProperty.Changed.AddClassHandler<Axis>(MinMaxChanged);
            MinimumPaddingProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MinimumRangeProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MinorGridlineColorProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MinorGridlineStyleProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MinorGridlineThicknessProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MinorStepProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            MinorTickSizeProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            PositionProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            PositionAtZeroCrossingProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            PositionTierProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            StartPositionProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            StringFormatProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            TextColorProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            TicklineColorProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            TickStyleProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            TitleProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            TitleClippingLengthProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            TitleColorProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            TitleFontProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            TitleFontSizeProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            TitleFontWeightProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            TitleFormatStringProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            TitlePositionProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            UnitProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
            UseSuperExponentialFormatProperty.Changed.AddClassHandler<Axis>(AppearanceChanged);
        }

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
        protected static void AppearanceChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
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
        private static void MinMaxChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
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
        protected static void DataChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
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
