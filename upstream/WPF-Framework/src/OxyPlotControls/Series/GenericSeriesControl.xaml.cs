using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using OxyPlot;

namespace OxyPlotControls
{
    /// <summary>
    /// A generic control for editing series properties that applies to all series types.
    /// Provides UI for common series properties.
    /// </summary>
    public partial class GenericSeriesControl : UserControl
    {
        #region Fields

        /// <summary>
        /// Suppresses PlotChanged events during initial loading and series property updates.
        /// </summary>
        private bool _suppressPlotChanged = true;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a series property value changes through user interaction with the control.
        /// </summary>
        /// <remarks>
        /// This event is raised when binding source updates occur, indicating that the plot
        /// should be refreshed to reflect the property changes. The event is suppressed
        /// during initial control loading and when the Series property is being set.
        /// </remarks>
        public event EventHandler? PlotChanged;

        #endregion


        #region Static Properties

        /// <summary>
        /// Gets the list of available marker types (excluding Custom).
        /// </summary>
        public static List<MarkerType> MarkerTypeOptions
        {
            get
            {
                var types = new List<MarkerType>((MarkerType[])Enum.GetValues(typeof(MarkerType)));
                types.Remove(MarkerType.Custom);
                return types;
            }
        }

        /// <summary>
        /// Gets the list of available line legend positions.
        /// </summary>
        public static List<OxyPlot.Series.LineLegendPosition> LineLegendPositionOptions { get; } =
            new List<OxyPlot.Series.LineLegendPosition>((OxyPlot.Series.LineLegendPosition[])Enum.GetValues(typeof(OxyPlot.Series.LineLegendPosition)));

        /// <summary>
        /// Gets the list of available line join options.
        /// </summary>
        public static List<LineJoin> LineJoinOptions { get; } =
            new List<LineJoin>((LineJoin[])Enum.GetValues(typeof(LineJoin)));

        /// <summary>
        /// Gets the list of available line style options.
        /// </summary>
        public static List<DoubleCollection> LineStyleOptions => GenericControls.LineStyleSelectorControl.LineStyleOptions;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Series"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
            nameof(Series),
            typeof(OxyPlot.Wpf.Series),
            typeof(GenericSeriesControl),
            new PropertyMetadata(null, InitializeControl));

        /// <summary>
        /// Gets or sets the series whose properties are being edited.
        /// </summary>
        public OxyPlot.Wpf.Series Series
        {
            get => (OxyPlot.Wpf.Series)GetValue(SeriesProperty);
            set => SetValue(SeriesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ExpanderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderStyleProperty = DependencyProperty.Register(
            nameof(ExpanderStyle),
            typeof(Style),
            typeof(GenericSeriesControl));

        /// <summary>
        /// Gets or sets the style to apply to expanders in this control.
        /// </summary>
        public Style ExpanderStyle
        {
            get => (Style)GetValue(ExpanderStyleProperty);
            set => SetValue(ExpanderStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowSpecializedSettings"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowSpecializedSettingsProperty = DependencyProperty.Register(
            nameof(ShowSpecializedSettings),
            typeof(Visibility),
            typeof(GenericSeriesControl),
            new PropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Gets or sets the visibility for the specialized series settings expander.
        /// </summary>
        /// <remarks>
        /// Returns <see cref="Visibility.Visible"/> for series types that have specialized properties:
        /// TwoColorLineSeries, ThreeColorLineSeries, CandleStickSeries, StairStepSeries.
        /// </remarks>
        public Visibility ShowSpecializedSettings
        {
            get => (Visibility)GetValue(ShowSpecializedSettingsProperty);
            set => SetValue(ShowSpecializedSettingsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowStandardColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStandardColorProperty = DependencyProperty.Register(
            nameof(ShowStandardColor),
            typeof(Visibility),
            typeof(GenericSeriesControl),
            new PropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Gets or sets the visibility for the standard color control.
        /// Hidden for PieSeries, TwoColorLineSeries, ThreeColorLineSeries, and CandleStickSeries.
        /// </summary>
        public Visibility ShowStandardColor
        {
            get => (Visibility)GetValue(ShowStandardColorProperty);
            set => SetValue(ShowStandardColorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowTwoColorControls"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTwoColorControlsProperty = DependencyProperty.Register(
            nameof(ShowTwoColorControls),
            typeof(Visibility),
            typeof(GenericSeriesControl),
            new PropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Gets or sets the visibility for TwoColorLineSeries color controls.
        /// </summary>
        public Visibility ShowTwoColorControls
        {
            get => (Visibility)GetValue(ShowTwoColorControlsProperty);
            set => SetValue(ShowTwoColorControlsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowThreeColorControls"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowThreeColorControlsProperty = DependencyProperty.Register(
            nameof(ShowThreeColorControls),
            typeof(Visibility),
            typeof(GenericSeriesControl),
            new PropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Gets or sets the visibility for ThreeColorLineSeries color controls.
        /// </summary>
        public Visibility ShowThreeColorControls
        {
            get => (Visibility)GetValue(ShowThreeColorControlsProperty);
            set => SetValue(ShowThreeColorControlsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowCandleStickColors"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowCandleStickColorsProperty = DependencyProperty.Register(
            nameof(ShowCandleStickColors),
            typeof(Visibility),
            typeof(GenericSeriesControl),
            new PropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Gets or sets the visibility for CandleStickSeries color controls.
        /// </summary>
        public Visibility ShowCandleStickColors
        {
            get => (Visibility)GetValue(ShowCandleStickColorsProperty);
            set => SetValue(ShowCandleStickColorsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowStairStepControls"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStairStepControlsProperty = DependencyProperty.Register(
            nameof(ShowStairStepControls),
            typeof(Visibility),
            typeof(GenericSeriesControl),
            new PropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Gets or sets the visibility for StairStepSeries line controls.
        /// </summary>
        public Visibility ShowStairStepControls
        {
            get => (Visibility)GetValue(ShowStairStepControlsProperty);
            set => SetValue(ShowStairStepControlsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowStandardLineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStandardLineStyleProperty = DependencyProperty.Register(
            nameof(ShowStandardLineStyle),
            typeof(Visibility),
            typeof(GenericSeriesControl),
            new PropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Gets or sets the visibility for the standard line style control.
        /// Hidden for StairStepSeries (which shows horizontal/vertical controls instead).
        /// </summary>
        public Visibility ShowStandardLineStyle
        {
            get => (Visibility)GetValue(ShowStandardLineStyleProperty);
            set => SetValue(ShowStandardLineStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowStandardLineThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStandardLineThicknessProperty = DependencyProperty.Register(
            nameof(ShowStandardLineThickness),
            typeof(Visibility),
            typeof(GenericSeriesControl),
            new PropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Gets or sets the visibility for the standard line thickness control.
        /// Hidden for series that don't use StrokeThickness (PieSeries, HeatMapSeries, ScatterErrorSeries).
        /// Separate from ShowStandardLineStyle so CandleStickSeries can hide line style but keep thickness.
        /// </summary>
        public Visibility ShowStandardLineThickness
        {
            get => (Visibility)GetValue(ShowStandardLineThicknessProperty);
            set => SetValue(ShowStandardLineThicknessProperty, value);
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericSeriesControl"/> class.
        /// </summary>
        public GenericSeriesControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Enables plot-change notifications after the control has loaded.
        /// </summary>
        /// <param name="sender">The control that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void OnLoaded(object sender, RoutedEventArgs e) => _suppressPlotChanged = false;
        /// <summary>
        /// Suppresses plot-change notifications while the control is unloaded.
        /// </summary>
        /// <param name="sender">The control that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void OnUnloaded(object sender, RoutedEventArgs e) { _suppressPlotChanged = true; }

        /// <summary>
        /// Raises the <see cref="PlotChanged"/> event.
        /// </summary>
        /// <remarks>
        /// This method checks the <see cref="_suppressPlotChanged"/> flag before raising the event.
        /// The event will not be raised during initial loading or when the Series property is being updated.
        /// </remarks>
        protected virtual void OnPlotChanged()
        {
            if (!_suppressPlotChanged)
            {
                PlotChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handles the SourceUpdated event for bindings in this control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data containing information about the binding that was updated.</param>
        /// <remarks>
        /// This method is called when any TwoWay binding with NotifyOnSourceUpdated=True
        /// updates its source. It triggers the <see cref="PlotChanged"/> event to signal
        /// that the plot should be refreshed.
        /// </remarks>
        private void OnBindingSourceUpdated(object sender, DataTransferEventArgs e)
        {
            OnPlotChanged();
        }

        /// <summary>
        /// Handles changes to the Series property.
        /// </summary>
        private static void InitializeControl(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(GenericSeriesControl)) return;
            var thisControl = (GenericSeriesControl)d;

            // Suppress PlotChanged events during series property updates
            thisControl._suppressPlotChanged = true;

            // Reset all visibility properties to defaults
            thisControl.ResetSeriesTypeVisibility();

            if (e.NewValue == null)
            {
                // Re-enable PlotChanged events if no new value
                thisControl._suppressPlotChanged = false;
                return;
            }

            var wpfSeries = e.NewValue as OxyPlot.Wpf.Series;
            if (wpfSeries == null)
            {
                // Re-enable PlotChanged events if series is null
                thisControl._suppressPlotChanged = false;
                return;
            }

            // Update visibility based on series type
            thisControl.UpdateSeriesTypeVisibility(wpfSeries);

            // Force layout update to sync bindings after series change
            thisControl.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                thisControl.UpdateLayout();
                // Re-enable PlotChanged events after layout is complete
                thisControl._suppressPlotChanged = false;
            }));
        }

        /// <summary>
        /// Resets all series type visibility properties to their default values.
        /// </summary>
        private void ResetSeriesTypeVisibility()
        {
            ShowSpecializedSettings = Visibility.Collapsed;
            ShowStandardColor = Visibility.Visible;
            ShowTwoColorControls = Visibility.Collapsed;
            ShowThreeColorControls = Visibility.Collapsed;
            ShowCandleStickColors = Visibility.Collapsed;
            ShowStairStepControls = Visibility.Collapsed;
            ShowStandardLineStyle = Visibility.Visible;
            ShowStandardLineThickness = Visibility.Visible;
        }

        /// <summary>
        /// Updates visibility properties based on the series type.
        /// </summary>
        /// <param name="wpfSeries">The series to check.</param>
        private void UpdateSeriesTypeVisibility(OxyPlot.Wpf.Series wpfSeries)
        {
            var seriesType = wpfSeries.GetType();

            // TwoColorLineSeries
            if (seriesType == typeof(OxyPlot.Wpf.TwoColorLineSeries))
            {
                ShowSpecializedSettings = Visibility.Visible;
                ShowStandardColor = Visibility.Collapsed;
                ShowTwoColorControls = Visibility.Visible;
            }
            // ThreeColorLineSeries
            else if (seriesType == typeof(OxyPlot.Wpf.ThreeColorLineSeries))
            {
                ShowSpecializedSettings = Visibility.Visible;
                ShowStandardColor = Visibility.Collapsed;
                ShowThreeColorControls = Visibility.Visible;
            }
            // CandleStickSeries - line style hidden (doesn't affect rendering), thickness stays visible
            else if (seriesType == typeof(OxyPlot.Wpf.CandleStickSeries))
            {
                ShowSpecializedSettings = Visibility.Visible;
                ShowStandardColor = Visibility.Collapsed;
                ShowCandleStickColors = Visibility.Visible;
                ShowStandardLineStyle = Visibility.Collapsed;
            }
            // StairStepSeries - specialized controls in Display section (no Specialized Settings needed)
            else if (seriesType == typeof(OxyPlot.Wpf.StairStepSeries))
            {
                ShowStairStepControls = Visibility.Visible;
                ShowStandardLineStyle = Visibility.Collapsed;
            }
            // PieSeries - hide color, line style, line thickness
            else if (seriesType == typeof(OxyPlot.Wpf.PieSeries))
            {
                ShowStandardColor = Visibility.Collapsed;
                ShowStandardLineStyle = Visibility.Collapsed;
                ShowStandardLineThickness = Visibility.Collapsed;
            }
            // HeatMapSeries - hide color, line style, line thickness
            else if (seriesType == typeof(OxyPlot.Wpf.HeatMapSeries))
            {
                ShowStandardColor = Visibility.Collapsed;
                ShowStandardLineStyle = Visibility.Collapsed;
                ShowStandardLineThickness = Visibility.Collapsed;
            }
            // ScatterErrorSeries - hide line style, line thickness (color stays visible)
            else if (seriesType == typeof(OxyPlot.Wpf.ScatterErrorSeries))
            {
                ShowStandardLineStyle = Visibility.Collapsed;
                ShowStandardLineThickness = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Closes all expanders in this control.
        /// </summary>
        public void CloseExpanders()
        {
            DisplayEXP.IsExpanded = false;
            MarkersEXP.IsExpanded = false;
            ErrorBarSettingsEXP.IsExpanded = false;
            BoxAndWhiskerEXP.IsExpanded = false;
            SpecializedSeriesEXP.IsExpanded = false;
        }

        /// <summary>
        /// Expands the specified property section.
        /// </summary>
        /// <param name="expansionZone">The property section to expand.</param>
        public void Expand(OxyPlotPropertiesControl.PropertyEXP expansionZone)
        {
            switch (expansionZone)
            {
                case OxyPlotPropertiesControl.PropertyEXP.Series_General:
                    DisplayEXP.IsExpanded = true;
                    break;
                case OxyPlotPropertiesControl.PropertyEXP.Series_Display:
                    DisplayEXP.IsExpanded = true;
                    break;
                case OxyPlotPropertiesControl.PropertyEXP.Series_Markers:
                    MarkersEXP.IsExpanded = true;
                    break;
                case OxyPlotPropertiesControl.PropertyEXP.Series_BoxAndWhiskers:
                    BoxAndWhiskerEXP.IsExpanded = true;
                    break;
                case OxyPlotPropertiesControl.PropertyEXP.Series_ErrorBarSettings:
                    ErrorBarSettingsEXP.IsExpanded = true;
                    break;
                case OxyPlotPropertiesControl.PropertyEXP.Series_SpecializedSettings:
                    SpecializedSeriesEXP.IsExpanded = true;
                    break;
            }
        }
    }

    #region Converters

    /// <summary>
    /// Converts series fill color to/from a SolidColorBrush, handling automatic colors.
    /// Used for series with a FillColor property (e.g., HistogramSeries, BarSeries, ColumnSeries).
    /// </summary>
    public class GenericSeriesFillConverter : IMultiValueConverter
    {
        private OxyPlot.Series.Series? _series;

        /// <summary>
        /// Converts a fill color and series to a SolidColorBrush, resolving automatic colors
        /// to the actual rendered fill color.
        /// </summary>
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Get Color
            if (values[0] == null) return null;
            if (values[0].GetType() != typeof(Color)) return null;
            var c = (Color)values[0];
            var oxyCol = OxyColor.FromArgb(c.A, c.R, c.G, c.B);

            // Get Series (this should only be set on convert with one-way binding)
            if (values[1] == null) return new SolidColorBrush(c);
            _series = ((OxyPlot.Wpf.Series)values[1]).InternalSeries;
            if (_series == null) return new SolidColorBrush(c);

            // Convert — resolve automatic color to actual fill color
            if (oxyCol.IsAutomatic())
            {
                if (_series is OxyPlot.Series.HistogramSeries histSeries)
                {
                    var actualColor = histSeries.ActualFillColor;
                    return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
                }
                else if (_series is OxyPlot.Series.BarSeries barSeries)
                {
                    var actualColor = barSeries.ActualFillColor;
                    return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
                }
                else if (_series is OxyPlot.Series.ColumnSeries columnSeries)
                {
                    var actualColor = columnSeries.ActualFillColor;
                    return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
                }
                else if (_series is OxyPlot.Series.IntervalBarSeries intervalBarSeries)
                {
                    var actualColor = intervalBarSeries.ActualFillColor;
                    return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
                }
                else if (_series is OxyPlot.Series.RectangleBarSeries rectBarSeries)
                {
                    var actualColor = rectBarSeries.ActualFillColor;
                    return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
                }
            }

            return new SolidColorBrush(c);
        }

        /// <summary>
        /// Converts a SolidColorBrush back to fill color and series.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (_series == null) return new object[] { Color.FromArgb(255, 0, 0, 0), null! };
            if (value.GetType() != typeof(SolidColorBrush)) return new object[] { Color.FromArgb(255, 0, 0, 0), null! };
            var c = ((SolidColorBrush)value).Color;
            return new object[] { c, _series };
        }
    }

    /// <summary>
    /// Converts series color to/from a SolidColorBrush, handling automatic colors.
    /// </summary>
    public class OxySeriesColorConverter : IMultiValueConverter
    {
        private OxyPlot.Series.Series? _series;

        /// <summary>
        /// Converts a color and series to a SolidColorBrush.
        /// </summary>
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Get Color
            if (values[0] == null) return null;
            if (values[0].GetType() != typeof(Color)) return null;
            var c = (Color)values[0];
            var oxyCol = OxyColor.FromArgb(c.A, c.R, c.G, c.B);

            // Get Series (this should only be set on convert with one-way binding)
            if (values[1] == null) { _series = null; return null!; }
            _series = ((OxyPlot.Wpf.Series)values[1]).InternalSeries;

            // Convert
            if (oxyCol.IsAutomatic())
            {
                if (_series.GetType() == typeof(OxyPlot.Series.LineSeries))
                {
                    var actualColor = ((OxyPlot.Series.LineSeries)_series).ActualColor;
                    return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
                }
                else if (_series.GetType() == typeof(OxyPlot.Series.ContourSeries))
                {
                    var actualColor = ((OxyPlot.Series.ContourSeries)_series).ActualColor;
                    return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
                }
                else if (_series.GetType() == typeof(OxyPlot.Series.HighLowSeries))
                {
                    var actualColor = ((OxyPlot.Series.HighLowSeries)_series).ActualColor;
                    return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
                }
                else if (_series.GetType() == typeof(OxyPlot.Series.CandleStickSeries))
                {
                    var actualColor = ((OxyPlot.Series.CandleStickSeries)_series).ActualColor;
                    return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
                }
            }

            return new SolidColorBrush(c);
        }

        /// <summary>
        /// Converts a SolidColorBrush back to color and series.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (_series == null) return new object[] { Color.FromArgb(255, 0, 0, 0), null! };
            if (value.GetType() != typeof(SolidColorBrush)) return new object[] { Color.FromArgb(255, 0, 0, 0), null! };
            var c = ((SolidColorBrush)value).Color;

            var oxyCol = OxyColor.FromArgb(c.A, c.R, c.G, c.B);
            if (_series.GetType() == typeof(OxyPlot.Series.LineSeries))
            {
                if (OxyColor.ColorDifference(oxyCol, ((OxyPlot.Series.LineSeries)_series).ActualColor) == 0)
                {
                    var actualColor = ((OxyPlot.Series.LineSeries)_series).ActualColor;
                    return new object[] { Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B), _series };
                }
            }
            else if (_series.GetType() == typeof(OxyPlot.Series.ContourSeries))
            {
                if (OxyColor.ColorDifference(oxyCol, ((OxyPlot.Series.ContourSeries)_series).ActualColor) == 0)
                {
                    var actualColor = ((OxyPlot.Series.ContourSeries)_series).ActualColor;
                    return new object[] { Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B), _series };
                }
            }
            else if (_series.GetType() == typeof(OxyPlot.Series.HighLowSeries))
            {
                if (OxyColor.ColorDifference(oxyCol, ((OxyPlot.Series.HighLowSeries)_series).ActualColor) == 0)
                {
                    var actualColor = ((OxyPlot.Series.HighLowSeries)_series).ActualColor;
                    return new object[] { Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B), _series };
                }
            }
            else if (_series.GetType() == typeof(OxyPlot.Series.CandleStickSeries))
            {
                if (OxyColor.ColorDifference(oxyCol, ((OxyPlot.Series.CandleStickSeries)_series).ActualColor) == 0)
                {
                    var actualColor = ((OxyPlot.Series.CandleStickSeries)_series).ActualColor;
                    return new object[] { Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B), _series };
                }
            }

            return new object[] { c, _series };
        }
    }

    /// <summary>
    /// Converts series marker fill color to/from a SolidColorBrush, handling automatic colors.
    /// </summary>
    public class OxySeriesMarkerFillConverter : IMultiValueConverter
    {
        private OxyPlot.Series.Series? _series;

        /// <summary>
        /// Converts a marker fill color and series to a SolidColorBrush.
        /// </summary>
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Get Color
            if (values[0] == null) return null;
            if (values[0].GetType() != typeof(Color)) return null;
            var c = (Color)values[0];
            var oxyCol = OxyColor.FromArgb(c.A, c.R, c.G, c.B);

            // Get Series
            if (values[1] == null) { _series = null; return null!; }
            _series = ((OxyPlot.Wpf.Series)values[1]).InternalSeries;

            // Convert
            if (oxyCol.IsAutomatic())
            {
                if (_series.GetType() == typeof(OxyPlot.Series.LineSeries))
                {
                    var actualColor = ((OxyPlot.Series.LineSeries)_series).ActualMarkerFill;
                    return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
                }
                else if (_series.GetType() == typeof(OxyPlot.Series.ScatterSeries))
                {
                    var actualColor = ((OxyPlot.Series.ScatterSeries)_series).ActualMarkerFillColor;
                    return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
                }
            }

            return new SolidColorBrush(c);
        }

        /// <summary>
        /// Converts a SolidColorBrush back to marker fill color and series.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (_series == null) return new object[] { Color.FromArgb(255, 0, 0, 0), null! };
            if (value.GetType() != typeof(SolidColorBrush)) return new object[] { Color.FromArgb(255, 0, 0, 0), null! };
            var c = ((SolidColorBrush)value).Color;

            var oxyCol = OxyColor.FromArgb(c.A, c.R, c.G, c.B);
            if (_series.GetType() == typeof(OxyPlot.Series.LineSeries))
            {
                if (OxyColor.ColorDifference(oxyCol, ((OxyPlot.Series.LineSeries)_series).ActualMarkerFill) == 0)
                {
                    var actualColor = ((OxyPlot.Series.LineSeries)_series).ActualMarkerFill;
                    return new object[] { Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B), _series };
                }
            }
            else if (_series.GetType() == typeof(OxyPlot.Series.ScatterSeries))
            {
                if (OxyColor.ColorDifference(oxyCol, ((OxyPlot.Series.ScatterSeries)_series).ActualMarkerFillColor) == 0)
                {
                    var actualColor = ((OxyPlot.Series.ScatterSeries)_series).ActualMarkerFillColor;
                    return new object[] { Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B), _series };
                }
            }

            return new object[] { c, _series };
        }
    }

    /// <summary>
    /// Converts series marker stroke color to/from a SolidColorBrush, handling automatic colors.
    /// </summary>
    public class OxySeriesMarkerStrokeConverter : IMultiValueConverter
    {
        private OxyPlot.Series.Series? _series;

        /// <summary>
        /// Converts a marker stroke color and series to a SolidColorBrush.
        /// </summary>
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Get Color
            if (values[0] == null) return null;
            if (values[0].GetType() != typeof(Color)) return null;
            var c = (Color)values[0];
            var oxyCol = OxyColor.FromArgb(c.A, c.R, c.G, c.B);

            // Get Series
            if (values[1] == null) { _series = null; return null!; }
            _series = ((OxyPlot.Wpf.Series)values[1]).InternalSeries;

            // Display a neutral black fallback when the stroke is Automatic so the
            // Stroke picker is visually independent from the Fill picker. Editing
            // stroke explicitly overrides Automatic.
            if (oxyCol.IsAutomatic())
                return new SolidColorBrush(Colors.Black);

            return new SolidColorBrush(c);
        }

        /// <summary>
        /// Converts a SolidColorBrush back to marker stroke color and series.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (_series == null) return new object[] { Color.FromArgb(255, 0, 0, 0), null! };
            if (value.GetType() != typeof(SolidColorBrush)) return new object[] { Color.FromArgb(255, 0, 0, 0), null! };
            var c = ((SolidColorBrush)value).Color;

            return new object[] { c, _series };
        }
    }

    /// <summary>
    /// Converts StairStepSeries VerticalStrokeThickness (which defaults to NaN) to a display value.
    /// When NaN, returns the horizontal StrokeThickness as the resolved value.
    /// </summary>
    public class StairStepVerticalThicknessConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts VerticalStrokeThickness and StrokeThickness to a display value.
        /// Returns StrokeThickness when VerticalStrokeThickness is NaN.
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 1.0;
            if (values[0] is double vertical && values[1] is double horizontal)
            {
                return double.IsNaN(vertical) ? horizontal : vertical;
            }
            return 1.0;
        }

        /// <summary>
        /// Converts the display value back to VerticalStrokeThickness.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return new object[] { d, Binding.DoNothing };
            }
            return new object[] { double.NaN, Binding.DoNothing };
        }
    }

    #endregion
}
