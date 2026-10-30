using System;
using System.Globalization;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using WpfCompat;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using WpfCompat.Data;
using IMultiValueConverter = WpfCompat.Data.IMultiValueConverter;
using Avalonia.Media;
using WpfCompat.Media;
using System.Xml.Linq;
using OxyPlot;
using static OxyPlot.Wpf.Serialization.PlotSerializer;

namespace OxyPlotControls
{
    /// <summary>
    /// A control for editing bar series properties including fill color, stroke, and bar width.
    /// </summary>
    /// <remarks>
    /// XGoodFit port: the dependency properties are Avalonia styled properties and their change callbacks
    /// class handlers; the expander style is a <see cref="ControlTheme"/>. The two-way multi-value converter is WpfCompat's, and the dispatcher is Avalonia's. The settings XML keeps WPF's text forms of brushes, colours, font weights and thicknesses through WpfCompat's converters.
    /// </remarks>
    public partial class BarSeriesControl : UserControl
    {
        #region Constants

        /// <summary>
        /// The XML tag used for bar series properties in serialization.
        /// </summary>
        public static readonly string BarSeriesPropertiesTag = "BarSeries";

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Series"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<OxyPlot.Wpf.BarSeriesBase> SeriesProperty = AvaloniaProperty.Register<BarSeriesControl, OxyPlot.Wpf.BarSeriesBase>(nameof(Series), null!);

        /// <summary>
        /// Gets or sets the bar series whose properties are being edited.
        /// </summary>
        public OxyPlot.Wpf.BarSeriesBase Series
        {
            get => (OxyPlot.Wpf.BarSeriesBase)GetValue(SeriesProperty);
            set => SetValue(SeriesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ExpanderStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme> ExpanderStyleProperty = AvaloniaProperty.Register<BarSeriesControl, ControlTheme>(nameof(ExpanderStyle), null!);

        /// <summary>
        /// Gets or sets the style to apply to expanders in this control.
        /// </summary>
        public ControlTheme ExpanderStyle
        {
            get => (ControlTheme)GetValue(ExpanderStyleProperty);
            set => SetValue(ExpanderStyleProperty, value);
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="BarSeriesControl"/> class.
        /// </summary>
        public BarSeriesControl()
        {
            InitializeComponent();
        }

        static BarSeriesControl()
        {
            // XGoodFit port: the property-changed callbacks of upstream's metadata.
            SeriesProperty.Changed.AddClassHandler<BarSeriesControl>(OnSeriesChanged);
        }

        /// <summary>
        /// Called when the Series property changes.
        /// Forces a layout update to ensure bindings are properly synchronized.
        /// </summary>
        private static void OnSeriesChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d is BarSeriesControl control)
            {
                if (e.NewValue != null)
                {
                    // Force layout update to sync bindings
                    Dispatcher.UIThread.Post(new Action(() =>
                    {
                        control.UpdateLayout();
                    }), DispatcherPriority.Loaded);
                }
            }
        }

        /// <summary>
        /// Closes all expanders in this control.
        /// </summary>
        public void CloseExpanders()
        {
            DisplayEXP.IsExpanded = false;
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
            }
        }

        #region Serialization

        /// <summary>
        /// Deserializes bar series properties from an XElement and applies them to a bar series.
        /// </summary>
        /// <param name="barSeries">The bar series to populate with properties.</param>
        /// <param name="element">The XElement containing bar series properties.</param>
        public static void XElementToBarSeriesProperties(OxyPlot.Wpf.BarSeries barSeries, XElement element)
        {
            // Early Exit
            if (barSeries == null) return;
            if (element.Name != BarSeriesPropertiesTag) return;

            // get enabled or not
            if (GetBooleanAttribute(element, "IsEnabled", out bool isEnabled)) barSeries.IsEnabled = isEnabled;

            // Set up converters
            var fontWeightConverter = new FontWeightConverter();
            var thicknessConverter = new ThicknessConverter();
            var oxycolorConverter = new OxyPlot.Wpf.OxyColorConverter();
            var brushConverter = new WpfCompat.Media.BrushConverter();

            // Labeling Properties
            var labelingElement = element.Element("Labeling");
            if (labelingElement != null)
            {
                if (labelingElement.Attribute("Title") != null)
                    barSeries.Title = labelingElement.Attribute("Title")!.Value;
                if (labelingElement.Attribute("LabelPlacement") != null)
                {
                    if (GetEnumAttribute(labelingElement, "LabelPlacement", out OxyPlot.Series.LabelPlacement placement))
                        barSeries.LabelPlacement = placement;
                }
                if (labelingElement.Attribute("TextColor") != null)
                    barSeries.Foreground = (IBrush)brushConverter.ConvertFromString(labelingElement.Attribute("TextColor")!.Value)!;
                if (labelingElement.Attribute("Font") != null)
                    barSeries.InternalSeries.Font = labelingElement.Attribute("Font")!.Value;
                if (GetDoubleAttribute(labelingElement, "FontSize", out double fontSize))
                    barSeries.FontSize = fontSize;
                if (labelingElement.Attribute("FontWeight") != null)
                    barSeries.FontWeight = (FontWeight)fontWeightConverter.ConvertFromString(labelingElement.Attribute("FontWeight")!.Value)!;
                if (labelingElement.Attribute("Padding") != null)
                    barSeries.Padding = (Thickness)thicknessConverter.ConvertFromString(labelingElement.Attribute("Padding")!.Value)!;
                if (labelingElement.Attribute("RenderInLegend") != null)
                    barSeries.RenderInLegend = Convert.ToBoolean(labelingElement.Attribute("RenderInLegend")!.Value);
                if (labelingElement.Attribute("XAxisKey") != null)
                    barSeries.XAxisKey = labelingElement.Attribute("XAxisKey")!.Value;
                if (labelingElement.Attribute("YAxisKey") != null)
                    barSeries.YAxisKey = labelingElement.Attribute("YAxisKey")!.Value;
                if (labelingElement.Attribute("TrackerKey") != null)
                    barSeries.TrackerKey = labelingElement.Attribute("TrackerKey")!.Value;
                if (labelingElement.Attribute("TrackerFormat") != null)
                    barSeries.TrackerFormatString = labelingElement.Attribute("TrackerFormat")!.Value;
                if (labelingElement.Attribute("LabelFormat") != null)
                    barSeries.LabelFormatString = labelingElement.Attribute("LabelFormat")!.Value;
            }

            // Display Properties
            var displayElement = element.Element("Display");
            if (displayElement != null)
            {
                if (displayElement.Attribute("Background") != null)
                    barSeries.Background = (IBrush)brushConverter.ConvertFromString(displayElement.Attribute("Background")!.Value)!;
                if (displayElement.Attribute("Color") != null)
                    barSeries.Color = (Color)ColorConverter.ConvertFromString(displayElement.Attribute("Color")!.Value)!;
                if (displayElement.Attribute("Fill") != null)
                    barSeries.FillColor = (Color)ColorConverter.ConvertFromString(displayElement.Attribute("Fill")!.Value)!;
                if (GetDoubleAttribute(displayElement, "LineThickness", out double strokeThickness))
                    barSeries.StrokeThickness = strokeThickness;
            }
        }

        /// <summary>
        /// Serializes bar series properties to an XElement.
        /// </summary>
        /// <param name="barSeries">The bar series to serialize.</param>
        /// <returns>An XElement containing the bar series properties.</returns>
        public static XElement BarSeriesPropertiesToXElement(OxyPlot.Wpf.BarSeries barSeries)
        {
            var properties = new XElement(BarSeriesPropertiesTag);
            properties.SetAttributeValue("IsEnabled", barSeries.IsEnabled.ToString());

            // XGoodFit port: WPF's text forms (WPF's ToString on brushes, colours, font weights and
            // thicknesses), through WpfCompat's converters.
            var brushConverter = new WpfCompat.Media.BrushConverter();
            var fontWeightConverter = new FontWeightConverter();
            var thicknessConverter = new ThicknessConverter();

            // Labeling properties
            var labelProps = new XElement("Labeling");
            labelProps.SetAttributeValue("Title", barSeries.Title);
            labelProps.SetAttributeValue("LabelPlacement", barSeries.LabelPlacement.ToString());
            labelProps.SetAttributeValue("TextColor", brushConverter.ConvertToInvariantString(barSeries.Foreground));
            labelProps.SetAttributeValue("Font", barSeries.FontFamily);
            labelProps.SetAttributeValue("FontSize", barSeries.FontSize.ToString("G17", CultureInfo.InvariantCulture));
            labelProps.SetAttributeValue("FontWeight", fontWeightConverter.ConvertToInvariantString(barSeries.FontWeight));
            labelProps.SetAttributeValue("Padding", thicknessConverter.ConvertToInvariantString(barSeries.Padding));
            labelProps.SetAttributeValue("RenderInLegend", barSeries.RenderInLegend);
            labelProps.SetAttributeValue("XAxisKey", barSeries.XAxisKey);
            labelProps.SetAttributeValue("YAxisKey", barSeries.YAxisKey);
            labelProps.SetAttributeValue("TrackerKey", barSeries.TrackerKey);
            labelProps.SetAttributeValue("TrackerFormat", barSeries.TrackerFormatString);
            labelProps.SetAttributeValue("LabelFormat", barSeries.LabelFormatString);
            properties.Add(labelProps);

            var displayProps = new XElement("Display");
            displayProps.SetAttributeValue("Background", barSeries.Background == null ? null : brushConverter.ConvertToInvariantString(barSeries.Background));
            displayProps.SetAttributeValue("Color", barSeries.Color.ToWpfString());
            displayProps.SetAttributeValue("Fill", barSeries.FillColor.ToWpfString());
            displayProps.SetAttributeValue("LineThickness", barSeries.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
            properties.Add(displayProps);

            return properties;
        }

        #endregion
    }

    /// <summary>
    /// Converts bar series fill color to/from a SolidColorBrush, handling automatic colors.
    /// </summary>
    public class BarSeriesFillConverter : IMultiValueConverter
    {
        private dynamic? _series;

        /// <summary>
        /// Converts a fill color and series to a SolidColorBrush.
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
            _series = ((OxyPlot.Wpf.BarSeriesBase)values[1]).InternalSeries;
            if (_series == null) return new SolidColorBrush(c);

            // Convert
            if (oxyCol.IsAutomatic())
            {
                OxyColor actualColor = _series.ActualFillColor;
                return new SolidColorBrush(Color.FromArgb(actualColor.A, actualColor.R, actualColor.G, actualColor.B));
            }

            return new SolidColorBrush(c);
        }

        /// <summary>
        /// Converts a SolidColorBrush back to fill color and series.
        /// </summary>
        public object?[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (_series == null) return new object?[] { Color.FromArgb(255, 0, 0, 0), null };
            // Get color value
            if (value == null || value.GetType() != typeof(SolidColorBrush)) return new object?[] { Color.FromArgb(255, 0, 0, 0), null };
            var c = ((SolidColorBrush)value).Color;
            var oxyCol = OxyColor.FromArgb(c.A, c.R, c.G, c.B);

            OxyColor seriesActualFillColor = _series.ActualFillColor;
            if (OxyColor.ColorDifference(oxyCol, seriesActualFillColor) == 0)
            {
                return new object?[] { Color.FromArgb(seriesActualFillColor.A, seriesActualFillColor.R, seriesActualFillColor.G, seriesActualFillColor.B), _series };
            }

            return new object?[] { c, _series };
        }
    }
}
