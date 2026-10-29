using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using OxyPlot;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls
{
    /// <summary>
    /// A user control that provides UI for editing general plot properties such as title, subtitle,
    /// plot area styling, and background settings for an OxyPlot chart.
    /// </summary>
    public partial class GeneralPlotControl : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Plot"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlotProperty = DependencyProperty.Register(
            nameof(Plot), typeof(Wpf.Plot), typeof(GeneralPlotControl),
            new PropertyMetadata(null, OnPlotPropertyChanged));

        /// <summary>
        /// Gets or sets the OxyPlot Plot control that this control edits.
        /// </summary>
        public Wpf.Plot Plot
        {
            get { return (Wpf.Plot)GetValue(PlotProperty); }
            set { SetValue(PlotProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ExpanderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderStyleProperty = DependencyProperty.Register(
            nameof(ExpanderStyle), typeof(Style), typeof(GeneralPlotControl));

        /// <summary>
        /// Gets or sets the style applied to expander controls within this control.
        /// </summary>
        public Style ExpanderStyle
        {
            get { return (Style)GetValue(ExpanderStyleProperty); }
            set { SetValue(ExpanderStyleProperty, value); }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralPlotControl"/> class.
        /// </summary>
        public GeneralPlotControl()
        {
            InitializeComponent();

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when the Plot dependency property changes.
        /// Forces a layout refresh to sync bindings to the new Plot.
        /// </summary>
        /// <param name="d">The dependency object (GeneralPlotControl instance).</param>
        /// <param name="e">The event arguments containing old and new values.</param>
        private static void OnPlotPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GeneralPlotControl control && e.NewValue != null)
            {
                // Force layout update to sync bindings
                control.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
                {
                    control.UpdateLayout();
                }));
            }
        }

        #endregion
    }

    /// <summary>
    /// A value converter that handles OxyPlot's automatic color representation.
    /// OxyPlot uses ARGB(0,0,0,1) to represent an automatic color, which this converter
    /// translates to black for display purposes.
    /// </summary>
    public class OxyAutomaticColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts an OxyPlot color to a WPF SolidColorBrush, handling the automatic color case.
        /// </summary>
        /// <param name="value">The color value to convert.</param>
        /// <param name="targetType">The target type (not used).</param>
        /// <param name="parameter">Additional parameter (not used).</param>
        /// <param name="culture">The culture to use for conversion.</param>
        /// <returns>A SolidColorBrush representing the color, with automatic colors converted to black.</returns>
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return value;
            if (value.GetType() != typeof(Color)) return null;
            Color c = (Color)value;
            var oxyCol = OxyColor.FromArgb(c.A, c.R, c.G, c.B);
            if (oxyCol.IsAutomatic()) return new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            return new SolidColorBrush(c);
        }

        /// <summary>
        /// Converts a SolidColorBrush back to a Color.
        /// </summary>
        /// <param name="value">The brush to convert.</param>
        /// <param name="targetType">The target type (not used).</param>
        /// <param name="parameter">Additional parameter (not used).</param>
        /// <param name="culture">The culture to use for conversion.</param>
        /// <returns>The Color from the SolidColorBrush.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
                return brush.Color;
            return Color.FromArgb(255, 0, 0, 0);
        }
    }

    /// <summary>
    /// A value converter that provides a default font size when the value is NaN or invalid.
    /// Returns 12.0 as the default font size.
    /// </summary>
    public class OxyDefaultFontSizeConverter : IValueConverter
    {
        /// <summary>
        /// Converts a font size value, returning a default of 12.0 for NaN or invalid values.
        /// </summary>
        /// <param name="value">The font size value to convert.</param>
        /// <param name="targetType">The target type (not used).</param>
        /// <param name="parameter">Additional parameter (not used).</param>
        /// <param name="culture">The culture to use for conversion.</param>
        /// <returns>The font size, or 12.0 if the value is NaN, infinite, or invalid.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 12.0;
            if (value.GetType() != typeof(double)) return 12.0;
            double doubleVal = (double)value;
            if (double.IsNaN(doubleVal) || double.IsInfinity(doubleVal)) return 12.0;
            return doubleVal;
        }

        /// <summary>
        /// Converts a font size back, returning NaN if the value is the default 12.0.
        /// </summary>
        /// <param name="value">The font size value to convert back.</param>
        /// <param name="targetType">The target type (not used).</param>
        /// <param name="parameter">Additional parameter (not used).</param>
        /// <param name="culture">The culture to use for conversion.</param>
        /// <returns>The font size, or NaN if the value is null or invalid.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return double.NaN;
            if (value.GetType() != typeof(double)) return double.NaN;
            return (double)value;
        }
    }

    /// <summary>
    /// A simple value converter that casts between Brush and SolidColorBrush types.
    /// </summary>
    public class SolidColorBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value to a SolidColorBrush.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type (not used).</param>
        /// <param name="parameter">Additional parameter (not used).</param>
        /// <param name="culture">The culture to use for conversion.</param>
        /// <returns>The value as a SolidColorBrush, or null if the value is null or not a SolidColorBrush.</returns>
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return value as SolidColorBrush;
        }

        /// <summary>
        /// Converts a SolidColorBrush back to a Brush.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetType">The target type (not used).</param>
        /// <param name="parameter">Additional parameter (not used).</param>
        /// <param name="culture">The culture to use for conversion.</param>
        /// <returns>The value as a Brush, or null if the value is null or not a Brush.</returns>
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return value as Brush;
        }
    }
}
