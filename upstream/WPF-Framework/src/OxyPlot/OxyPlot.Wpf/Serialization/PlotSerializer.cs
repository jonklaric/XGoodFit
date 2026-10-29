// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotSerializer.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides top-level serialization and deserialization of OxyPlot plot settings to XML,
//   including general properties, legend properties, and shared XML attribute helper methods.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#nullable enable annotations
#nullable disable warnings

namespace OxyPlot.Wpf.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Xml.Linq;
    using OxyPlot.Legends;

    /// <summary>
    /// Provides top-level serialization and deserialization of OxyPlot <see cref="Plot"/> settings to XML.
    /// Contains the orchestrator methods, shared XML attribute helper methods, and serialization
    /// for general plot properties and legend properties.
    /// </summary>
    public static class PlotSerializer
    {
        #region Tag Constants

        /// <summary>
        /// The XML tag name used for the root element containing OxyPlot properties.
        /// </summary>
        public const string OxyplotPropertiesTag = "OxyplotProperties";

        /// <summary>
        /// The XML tag name used for general plot properties.
        /// </summary>
        public const string GeneralPropertiesTag = "General";

        /// <summary>
        /// The XML tag name used for legend properties.
        /// </summary>
        public const string LegendPropertiesTag = "Legend";

        /// <summary>
        /// The XML tag name used for the axes collection.
        /// </summary>
        public const string AxesPropertiesTag = "Axes";

        /// <summary>
        /// The XML tag name used for a single axis.
        /// </summary>
        public const string AxisPropertiesTag = "Axis";

        /// <summary>
        /// The XML tag name used for the annotations collection.
        /// </summary>
        public const string AnnotationsPropertiesTag = "Annotations";

        /// <summary>
        /// The XML tag name used for a single annotation.
        /// </summary>
        public const string AnnotationPropertiesTag = "Annotation";

        /// <summary>
        /// The XML tag name used for the series collection.
        /// </summary>
        public const string SeriesPropertiesTag = "Series";

        /// <summary>
        /// The XML tag name used for a single series item.
        /// </summary>
        public const string SeriesItemTag = "SeriesItem";

        #endregion

        #region Visual Property Names

        /// <summary>
        /// Visual property names on <see cref="Plot"/> that are serialized for settings persistence.
        /// Consumers can use this list to monitor these properties for change tracking.
        /// </summary>
        public static readonly IReadOnlyList<string> PlotVisualProperties = new[]
        {
            // General
            "Title", "TitleColor", "TitleFont", "TitleFontSize", "TitleFontWeight", "TitlePadding",
            "Subtitle", "SubtitleColor", "SubtitleFont", "SubtitleFontSize", "SubtitleFontWeight",
            "Background", "BorderBrush", "BorderThickness", "Padding",
            "PlotAreaBackground", "PlotAreaBorderColor", "PlotAreaBorderThickness",
            // Legend
            "IsLegendVisible", "LegendBackground", "LegendBorder", "LegendBorderThickness",
            "LegendPadding", "LegendMargin", "LegendMaxHeight", "LegendMaxWidth",
            "LegendPlacement", "LegendPosition", "LegendOrientation",
            "LegendTitle", "LegendTitleColor", "LegendTitleFont", "LegendTitleFontSize", "LegendTitleFontWeight",
            "LegendTextColor", "LegendFont", "LegendFontSize", "LegendFontWeight",
            "LegendSymbolLength", "LegendSymbolMargin", "LegendSymbolPlacement",
            "LegendColumnSpacing", "LegendItemAlignment", "LegendItemClickTogglesSeriesVisibility",
            "LegendItemOrder", "LegendItemSpacing", "LegendLineSpacing",
        };

        #endregion

        #region Orchestrator Methods

        /// <summary>
        /// Serializes all plot properties to an <see cref="XElement"/> for persistence.
        /// </summary>
        /// <param name="plot">The OxyPlot <see cref="Plot"/> control to serialize.</param>
        /// <returns>An <see cref="XElement"/> containing all serialized plot properties including
        /// general settings, legend, axes, annotations, and series.</returns>
        public static XElement ToXElement(Plot plot)
        {
            var plotPropertiesElement = new XElement(OxyplotPropertiesTag);

            plotPropertiesElement.Add(GeneralPropertiesToXElement(plot));
            plotPropertiesElement.Add(LegendPropertiesToXElement(plot));
            plotPropertiesElement.Add(AxisSerializer.AxesToXElement(plot));
            plotPropertiesElement.Add(AnnotationSerializer.AnnotationsToXElement(plot));
            plotPropertiesElement.Add(SeriesSerializer.SeriesToXElement(plot));

            return plotPropertiesElement;
        }

        /// <summary>
        /// Deserializes plot properties from an <see cref="XElement"/> and applies them to the plot.
        /// </summary>
        /// <param name="plot">The OxyPlot <see cref="Plot"/> control to apply settings to.</param>
        /// <param name="element">The <see cref="XElement"/> containing serialized plot properties.</param>
        public static void FromXElement(Plot plot, XElement element)
        {
            // General
            var generalElement = element.Element(GeneralPropertiesTag);
            if (generalElement != null) XElementToGeneralProperties(plot, generalElement);

            // Legend
            var legendElement = element.Element(LegendPropertiesTag);
            if (legendElement != null) XElementToLegendProperties(plot, legendElement);

            // Axes
            var axesElement = element.Element(AxesPropertiesTag);
            if (axesElement != null) AxisSerializer.XElementToAxes(plot, axesElement);

            // Annotations
            var annotationsElement = element.Element(AnnotationsPropertiesTag);
            if (annotationsElement != null) AnnotationSerializer.XElementToAnnotations(plot, annotationsElement);

            // Series
            var seriesElement = element.Element(SeriesPropertiesTag);
            if (seriesElement != null) SeriesSerializer.XElementToSeries(plot, seriesElement);

            // Update the plot.
            plot.InvalidatePlot(true);
        }

        #endregion

        #region General Properties

        /// <summary>
        /// Serializes general plot properties (title, subtitle, chart area, plot area) to an <see cref="XElement"/>.
        /// </summary>
        /// <param name="plot">The OxyPlot <see cref="Plot"/> control whose properties will be serialized.</param>
        /// <returns>An <see cref="XElement"/> containing all serialized general plot properties.</returns>
        public static XElement GeneralPropertiesToXElement(Plot plot)
        {
            var generalProperties = new XElement(GeneralPropertiesTag);
            generalProperties.SetAttributeValue(nameof(plot.IsEnabled), plot.IsEnabled.ToString());
            // Global text color applies to every default-colored text element on the plot.
            // Previously not persisted: round-trip silently reset TextColor to default.
            generalProperties.SetAttributeValue(nameof(plot.TextColor), plot.TextColor.ToString());

            var weightConverter = new FontWeightConverter();

            // Title Properties
            var titleProperties = new XElement("Title");
            titleProperties.SetAttributeValue(nameof(plot.Title), plot.Title);
            titleProperties.SetAttributeValue(nameof(plot.TitleColor), plot.TitleColor.ToString());
            titleProperties.SetAttributeValue(nameof(plot.TitleFont), plot.TitleFont);
            titleProperties.SetAttributeValue(nameof(plot.TitleFontSize), plot.TitleFontSize.ToString("G17", CultureInfo.InvariantCulture));
            titleProperties.SetAttributeValue(nameof(plot.TitleFontWeight), weightConverter.ConvertToInvariantString(plot.TitleFontWeight));
            titleProperties.SetAttributeValue(nameof(plot.TitlePadding), plot.TitlePadding.ToString("G17", CultureInfo.InvariantCulture));
            generalProperties.Add(titleProperties);

            // SubTitle Properties
            var subTitleProperties = new XElement("Subtitle");
            subTitleProperties.SetAttributeValue(nameof(plot.Subtitle), plot.Subtitle);
            subTitleProperties.SetAttributeValue(nameof(plot.SubtitleColor), plot.SubtitleColor.ToString());
            subTitleProperties.SetAttributeValue(nameof(plot.SubtitleFont), plot.SubtitleFont);
            subTitleProperties.SetAttributeValue(nameof(plot.SubtitleFontSize), plot.SubtitleFontSize.ToString("G17", CultureInfo.InvariantCulture));
            subTitleProperties.SetAttributeValue(nameof(plot.SubtitleFontWeight), weightConverter.ConvertToInvariantString(plot.SubtitleFontWeight));
            generalProperties.Add(subTitleProperties);

            // Chart Area Properties
            var chartProperties = new XElement("Chart");
            var bc = new BrushConverter();
            var tc = new ThicknessConverter();
            chartProperties.SetAttributeValue(nameof(plot.Background), bc.ConvertToInvariantString(plot.Background));
            chartProperties.SetAttributeValue(nameof(plot.BorderBrush), bc.ConvertToInvariantString(plot.BorderBrush));
            chartProperties.SetAttributeValue(nameof(plot.BorderThickness), tc.ConvertToInvariantString(plot.BorderThickness));
            chartProperties.SetAttributeValue(nameof(plot.Padding), tc.ConvertToInvariantString(plot.Padding));
            generalProperties.Add(chartProperties);

            // Plot Area Properties
            var plotAreaProperties = new XElement("Plot");
            plotAreaProperties.SetAttributeValue(nameof(plot.PlotAreaBackground), bc.ConvertToInvariantString(plot.PlotAreaBackground));
            plotAreaProperties.SetAttributeValue(nameof(plot.PlotAreaBorderColor), plot.PlotAreaBorderColor.ToString());
            plotAreaProperties.SetAttributeValue(nameof(plot.PlotAreaBorderThickness), tc.ConvertToInvariantString(plot.PlotAreaBorderThickness));
            generalProperties.Add(plotAreaProperties);

            return generalProperties;
        }

        /// <summary>
        /// Deserializes general plot properties from an <see cref="XElement"/> and applies them to the plot.
        /// </summary>
        /// <param name="plot">The OxyPlot <see cref="Plot"/> control to apply settings to.</param>
        /// <param name="element">The <see cref="XElement"/> containing serialized general plot properties.</param>
        /// <remarks>
        /// Supports backward-compatible attribute names for older XML formats.
        /// </remarks>
        public static void XElementToGeneralProperties(Plot plot, XElement element)
        {
            // Early Exit
            if (plot == null) return;
            if (element.Name != GeneralPropertiesTag) return;

            // Get enabled or not. Only assign when the attribute is actually present; otherwise
            // leave plot.IsEnabled at its current (typically true) default. The previous code
            // force-set IsEnabled=false on any XML that predates this attribute, silently
            // disabling loaded plots.
            if (GetBooleanAttribute(element, nameof(plot.IsEnabled), out bool isEnabled))
                plot.IsEnabled = isEnabled;
            if (GetColorAttribute(element, nameof(plot.TextColor), out Color textColor))
                plot.TextColor = textColor;

            // Set up converters
            var weightConverter = new FontWeightConverter();
            var thicknessConverter = new ThicknessConverter();
            var brushConverter = new BrushConverter();

            // Title Properties
            var titleElement = element.Element("Title");
            if (titleElement != null)
            {
                string? titleStr;
                if (GetStringAttribute(titleElement, nameof(plot.Title), out titleStr)) plot.Title = titleStr!;

                Color titleColor;
                if (GetColorAttribute(titleElement, nameof(plot.TitleColor), out titleColor)) plot.TitleColor = titleColor;

                string? titleFont;
                if (GetStringAttribute(titleElement, nameof(plot.TitleFont), out titleFont)) plot.TitleFont = titleFont!;

                double titleFontSize;
                if (GetDoubleAttribute(titleElement, nameof(plot.TitleFontSize), out titleFontSize)) plot.TitleFontSize = titleFontSize;

                FontWeight titleFontWeight;
                if (GetFontWeightAttribute(titleElement, nameof(plot.TitleFontWeight), weightConverter, out titleFontWeight)) plot.TitleFontWeight = titleFontWeight;

                double titlePadding;
                if (GetDoubleAttribute(titleElement, nameof(plot.TitlePadding), out titlePadding)) plot.TitlePadding = titlePadding;

                // Backward compatibility
                if (GetColorAttribute(titleElement, "Color", out titleColor)) plot.TitleColor = titleColor;
                if (GetStringAttribute(titleElement, "Font", out titleFont)) plot.TitleFont = titleFont!;
                if (GetDoubleAttribute(titleElement, "Size", out titleFontSize)) plot.TitleFontSize = titleFontSize;
                if (GetFontWeightAttribute(titleElement, "Weight", weightConverter, out titleFontWeight)) plot.TitleFontWeight = titleFontWeight;
                if (GetDoubleAttribute(titleElement, "Padding", out titlePadding)) plot.TitlePadding = titlePadding;
            }

            // SubTitle Properties
            var subTitleElement = element.Element("Subtitle");
            if (subTitleElement != null)
            {
                string? subtitle;
                if (GetStringAttribute(subTitleElement, nameof(plot.Subtitle), out subtitle)) plot.Subtitle = subtitle!;

                Color subtitleColor;
                if (GetColorAttribute(subTitleElement, nameof(plot.SubtitleColor), out subtitleColor)) plot.SubtitleColor = subtitleColor;

                string? subtitleFont;
                if (GetStringAttribute(subTitleElement, nameof(plot.SubtitleFont), out subtitleFont)) plot.SubtitleFont = subtitleFont!;

                double subtitleFontSize;
                if (GetDoubleAttribute(subTitleElement, nameof(plot.SubtitleFontSize), out subtitleFontSize)) plot.SubtitleFontSize = subtitleFontSize;

                FontWeight subtitleFontWeight;
                if (GetFontWeightAttribute(subTitleElement, nameof(plot.SubtitleFontWeight), weightConverter, out subtitleFontWeight)) plot.SubtitleFontWeight = subtitleFontWeight;

                // Backward compatibility
                if (GetStringAttribute(subTitleElement, "Title", out subtitle)) plot.Subtitle = subtitle!;
                if (GetColorAttribute(subTitleElement, "Color", out subtitleColor)) plot.SubtitleColor = subtitleColor;
                if (GetStringAttribute(subTitleElement, "Font", out subtitleFont)) plot.SubtitleFont = subtitleFont!;
                if (GetDoubleAttribute(subTitleElement, "Size", out subtitleFontSize)) plot.SubtitleFontSize = subtitleFontSize;
                if (GetFontWeightAttribute(subTitleElement, "Weight", weightConverter, out subtitleFontWeight)) plot.SubtitleFontWeight = subtitleFontWeight;
            }

            // Chart Area Properties
            var chartElement = element.Element("Chart");
            if (chartElement != null)
            {
                Brush? background;
                if (GetBrushAttribute(chartElement, nameof(plot.Background), brushConverter, out background)) plot.Background = background!;

                Brush? borderBrush;
                if (GetBrushAttribute(chartElement, nameof(plot.BorderBrush), brushConverter, out borderBrush)) plot.BorderBrush = borderBrush!;

                Thickness borderThickness;
                if (GetThicknessAttribute(chartElement, nameof(plot.BorderThickness), thicknessConverter, out borderThickness)) plot.BorderThickness = borderThickness;

                Thickness padding;
                if (GetThicknessAttribute(chartElement, nameof(plot.Padding), thicknessConverter, out padding)) plot.Padding = padding;

                // Backward compatibility
                var backgroundElement = chartElement.Element("BackgroundBrush");
                if (backgroundElement != null)
                {
                    // FirstOrDefault avoids the manual GetEnumerator() pattern (which leaks the
                    // enumerator if not disposed) and reads cleanly.
                    var firstChild = backgroundElement.Elements().FirstOrDefault();
                    if (firstChild != null)
                    {
                        var bg = DeserializeFromXElement(firstChild) as Brush;
                        if (bg != null) plot.Background = bg;
                    }
                }

                var borderElement = chartElement.Element("BorderBrush");
                if (borderElement != null)
                {
                    var firstChild = borderElement.Elements().FirstOrDefault();
                    if (firstChild != null)
                    {
                        var bg = DeserializeFromXElement(firstChild) as Brush;
                        if (bg != null) plot.BorderBrush = bg;
                    }
                }
            }

            // Plot Area Properties
            var plotAreaElement = element.Element("Plot");
            if (plotAreaElement != null)
            {
                Brush? plotAreaBackground;
                if (GetBrushAttribute(plotAreaElement, nameof(plot.PlotAreaBackground), brushConverter, out plotAreaBackground)) plot.PlotAreaBackground = plotAreaBackground!;

                Color plotAreaBorderColor;
                if (GetColorAttribute(plotAreaElement, nameof(plot.PlotAreaBorderColor), out plotAreaBorderColor)) plot.PlotAreaBorderColor = plotAreaBorderColor;

                Thickness plotAreaBorderThickness;
                if (GetThicknessAttribute(plotAreaElement, nameof(plot.PlotAreaBorderThickness), thicknessConverter, out plotAreaBorderThickness)) plot.PlotAreaBorderThickness = plotAreaBorderThickness;

                // Backward compatibility
                var backgroundElement = plotAreaElement.Element("BackgroundBrush");
                if (backgroundElement != null)
                {
                    var firstChild = backgroundElement.Elements().FirstOrDefault();
                    if (firstChild != null)
                    {
                        var bg = DeserializeFromXElement(firstChild) as Brush;
                        if (bg != null) plot.PlotAreaBackground = bg;
                    }
                }

                if (GetColorAttribute(plotAreaElement, "BorderColor", out plotAreaBorderColor)) plot.PlotAreaBorderColor = plotAreaBorderColor;
                if (GetThicknessAttribute(plotAreaElement, "BorderThickness", thicknessConverter, out plotAreaBorderThickness)) plot.PlotAreaBorderThickness = plotAreaBorderThickness;
            }
        }

        #endregion

        #region Legend Properties

        /// <summary>
        /// Serializes legend properties to an <see cref="XElement"/> for persistence.
        /// </summary>
        /// <param name="plot">The OxyPlot <see cref="Plot"/> control whose legend properties will be serialized.</param>
        /// <returns>An <see cref="XElement"/> containing all serialized legend properties.</returns>
        public static XElement LegendPropertiesToXElement(Plot plot)
        {
            var legendProperties = new XElement(LegendPropertiesTag);

            var fwc = new FontWeightConverter();

            // Legend Area
            var legendAreaProperties = new XElement("Area");
            legendAreaProperties.SetAttributeValue(nameof(plot.IsLegendVisible), plot.IsLegendVisible);
            legendAreaProperties.SetAttributeValue(nameof(plot.LegendBackground), plot.LegendBackground.ToString());
            legendAreaProperties.SetAttributeValue(nameof(plot.LegendBorder), plot.LegendBorder.ToString());
            legendAreaProperties.SetAttributeValue(nameof(plot.LegendBorderThickness), plot.LegendBorderThickness.ToString("G17", CultureInfo.InvariantCulture));
            legendAreaProperties.SetAttributeValue(nameof(plot.LegendPadding), plot.LegendPadding.ToString("G17", CultureInfo.InvariantCulture));
            legendAreaProperties.SetAttributeValue(nameof(plot.LegendMargin), plot.LegendMargin.ToString("G17", CultureInfo.InvariantCulture));
            legendAreaProperties.SetAttributeValue(nameof(plot.LegendMaxHeight), plot.LegendMaxHeight.ToString("G17", CultureInfo.InvariantCulture));
            legendAreaProperties.SetAttributeValue(nameof(plot.LegendMaxWidth), plot.LegendMaxWidth.ToString("G17", CultureInfo.InvariantCulture));
            legendProperties.Add(legendAreaProperties);

            // Legend Position Properties
            var positionProperties = new XElement("Position");
            positionProperties.SetAttributeValue(nameof(plot.LegendPlacement), plot.LegendPlacement.ToString());
            positionProperties.SetAttributeValue(nameof(plot.LegendPosition), plot.LegendPosition.ToString());
            positionProperties.SetAttributeValue(nameof(plot.LegendOrientation), plot.LegendOrientation.ToString());
            legendProperties.Add(positionProperties);

            // Title Properties
            var titleProperties = new XElement("Title");
            titleProperties.SetAttributeValue(nameof(plot.LegendTitle), plot.LegendTitle);
            titleProperties.SetAttributeValue(nameof(plot.LegendTitleColor), plot.LegendTitleColor.ToString());
            titleProperties.SetAttributeValue(nameof(plot.LegendTitleFont), plot.LegendTitleFont);
            titleProperties.SetAttributeValue(nameof(plot.LegendTitleFontSize), plot.LegendTitleFontSize.ToString("G17", CultureInfo.InvariantCulture));
            titleProperties.SetAttributeValue(nameof(plot.LegendTitleFontWeight), fwc.ConvertToInvariantString(plot.LegendTitleFontWeight));
            legendProperties.Add(titleProperties);

            // Legend Item Properties
            var itemProperties = new XElement("Items");
            itemProperties.SetAttributeValue(nameof(plot.LegendTextColor), plot.LegendTextColor.ToString());
            itemProperties.SetAttributeValue(nameof(plot.LegendFont), plot.LegendFont);
            itemProperties.SetAttributeValue(nameof(plot.LegendFontSize), plot.LegendFontSize.ToString("G17", CultureInfo.InvariantCulture));
            itemProperties.SetAttributeValue(nameof(plot.LegendFontWeight), fwc.ConvertToInvariantString(plot.LegendFontWeight));
            itemProperties.SetAttributeValue(nameof(plot.LegendSymbolLength), plot.LegendSymbolLength.ToString("G17", CultureInfo.InvariantCulture));
            itemProperties.SetAttributeValue(nameof(plot.LegendSymbolMargin), plot.LegendSymbolMargin.ToString("G17", CultureInfo.InvariantCulture));
            itemProperties.SetAttributeValue(nameof(plot.LegendSymbolPlacement), plot.LegendSymbolPlacement.ToString());
            itemProperties.SetAttributeValue(nameof(plot.LegendColumnSpacing), plot.LegendColumnSpacing.ToString("G17", CultureInfo.InvariantCulture));
            itemProperties.SetAttributeValue(nameof(plot.LegendItemAlignment), plot.LegendItemAlignment.ToString());
            itemProperties.SetAttributeValue(nameof(plot.LegendItemClickTogglesSeriesVisibility), plot.LegendItemClickTogglesSeriesVisibility);
            itemProperties.SetAttributeValue(nameof(plot.LegendItemOrder), plot.LegendItemOrder.ToString());
            itemProperties.SetAttributeValue(nameof(plot.LegendItemSpacing), plot.LegendItemSpacing.ToString("G17", CultureInfo.InvariantCulture));
            itemProperties.SetAttributeValue(nameof(plot.LegendLineSpacing), plot.LegendLineSpacing.ToString("G17", CultureInfo.InvariantCulture));
            legendProperties.Add(itemProperties);

            return legendProperties;
        }

        /// <summary>
        /// Deserializes legend properties from an <see cref="XElement"/> and applies them to the plot.
        /// </summary>
        /// <param name="plot">The OxyPlot <see cref="Plot"/> control to apply settings to.</param>
        /// <param name="element">The <see cref="XElement"/> containing serialized legend properties.</param>
        /// <remarks>
        /// Supports backward-compatible attribute names for older XML formats.
        /// </remarks>
        public static void XElementToLegendProperties(Plot plot, XElement element)
        {
            // Early Exit
            if (plot == null) return;
            if (element.Name != LegendPropertiesTag) return;

            // Set up converters
            var fontWeightConverter = new FontWeightConverter();

            // Area Properties
            var areaElement = element.Element("Area");
            if (areaElement != null)
            {
                bool isLegendVisible;
                if (GetBooleanAttribute(areaElement, nameof(plot.IsLegendVisible), out isLegendVisible)) plot.IsLegendVisible = isLegendVisible;

                Color legendBackground;
                if (GetColorAttribute(areaElement, nameof(plot.LegendBackground), out legendBackground)) plot.LegendBackground = legendBackground;

                Color legendBorder;
                if (GetColorAttribute(areaElement, nameof(plot.LegendBorder), out legendBorder)) plot.LegendBorder = legendBorder;

                double legendBorderThickness;
                if (GetDoubleAttribute(areaElement, nameof(plot.LegendBorderThickness), out legendBorderThickness)) plot.LegendBorderThickness = legendBorderThickness;

                double legendPadding;
                if (GetDoubleAttribute(areaElement, nameof(plot.LegendPadding), out legendPadding)) plot.LegendPadding = legendPadding;

                double legendMargin;
                if (GetDoubleAttribute(areaElement, nameof(plot.LegendMargin), out legendMargin)) plot.LegendMargin = legendMargin;

                double legendMaxHeight;
                if (GetDoubleAttribute(areaElement, nameof(plot.LegendMaxHeight), out legendMaxHeight)) plot.LegendMaxHeight = legendMaxHeight;

                double legendMaxWidth;
                if (GetDoubleAttribute(areaElement, nameof(plot.LegendMaxWidth), out legendMaxWidth)) plot.LegendMaxWidth = legendMaxWidth;

                // Backward compatibility
                if (GetBooleanAttribute(areaElement, "LegendVisible", out isLegendVisible)) plot.IsLegendVisible = isLegendVisible;
                if (GetColorAttribute(areaElement, "BackgroundColor", out legendBackground)) plot.LegendBackground = legendBackground;
                if (GetColorAttribute(areaElement, "BorderColor", out legendBorder)) plot.LegendBorder = legendBorder;
                if (GetDoubleAttribute(areaElement, "BorderThickness", out legendBorderThickness)) plot.LegendBorderThickness = legendBorderThickness;
                if (GetDoubleAttribute(areaElement, "Padding", out legendPadding)) plot.LegendPadding = legendPadding;
            }

            // Position Properties
            var positionElement = element.Element("Position");
            if (positionElement != null)
            {
                LegendPlacement legendPlacement;
                if (GetEnumAttribute(positionElement, nameof(plot.LegendPlacement), out legendPlacement)) plot.LegendPlacement = legendPlacement;

                LegendPosition legendPosition;
                if (GetEnumAttribute(positionElement, nameof(plot.LegendPosition), out legendPosition)) plot.LegendPosition = legendPosition;

                LegendOrientation legendOrientation;
                if (GetEnumAttribute(positionElement, nameof(plot.LegendOrientation), out legendOrientation)) plot.LegendOrientation = legendOrientation;

                // Backward compatibility
                if (GetEnumAttribute(positionElement, "Placement", out legendPlacement)) plot.LegendPlacement = legendPlacement;
                if (GetEnumAttribute(positionElement, "Position", out legendPosition)) plot.LegendPosition = legendPosition;
                if (GetEnumAttribute(positionElement, "Orientation", out legendOrientation)) plot.LegendOrientation = legendOrientation;
            }

            // Title Properties
            var titleElement = element.Element("Title");
            if (titleElement != null)
            {
                string? legendTitle;
                if (GetStringAttribute(titleElement, nameof(plot.LegendTitle), out legendTitle)) plot.LegendTitle = legendTitle;

                Color legendTitleColor;
                if (GetColorAttribute(titleElement, nameof(plot.LegendTitleColor), out legendTitleColor)) plot.LegendTitleColor = legendTitleColor;

                string? legendTitleFont;
                if (GetStringAttribute(titleElement, nameof(plot.LegendTitleFont), out legendTitleFont)) plot.LegendTitleFont = legendTitleFont;

                double legendTitleFontSize;
                if (GetDoubleAttribute(titleElement, nameof(plot.LegendTitleFontSize), out legendTitleFontSize)) plot.LegendTitleFontSize = legendTitleFontSize;

                FontWeight legendTitleFontWeight;
                if (GetFontWeightAttribute(titleElement, nameof(plot.LegendTitleFontWeight), fontWeightConverter, out legendTitleFontWeight)) plot.LegendTitleFontWeight = legendTitleFontWeight;

                // Backward compatibility
                if (GetStringAttribute(titleElement, "Title", out legendTitle)) plot.LegendTitle = legendTitle;
                if (GetColorAttribute(titleElement, "Color", out legendTitleColor)) plot.LegendTitleColor = legendTitleColor;
                if (GetStringAttribute(titleElement, "Font", out legendTitleFont)) plot.LegendTitleFont = legendTitleFont;
                if (GetDoubleAttribute(titleElement, "Size", out legendTitleFontSize)) plot.LegendTitleFontSize = legendTitleFontSize;
                if (GetFontWeightAttribute(titleElement, "Weight", fontWeightConverter, out legendTitleFontWeight)) plot.LegendTitleFontWeight = legendTitleFontWeight;
            }

            // Legend Item Properties
            var itemsElement = element.Element("Items");
            if (itemsElement != null)
            {
                Color legendTextColor;
                if (GetColorAttribute(itemsElement, nameof(plot.LegendTextColor), out legendTextColor)) plot.LegendTextColor = legendTextColor;

                string? legendFont;
                if (GetStringAttribute(itemsElement, nameof(plot.LegendFont), out legendFont)) plot.LegendFont = legendFont;

                double legendFontSize;
                if (GetDoubleAttribute(itemsElement, nameof(plot.LegendFontSize), out legendFontSize)) plot.LegendFontSize = legendFontSize;

                FontWeight legendFontWeight;
                if (GetFontWeightAttribute(itemsElement, nameof(plot.LegendFontWeight), fontWeightConverter, out legendFontWeight)) plot.LegendFontWeight = legendFontWeight;

                double legendSymbolLength;
                if (GetDoubleAttribute(itemsElement, nameof(plot.LegendSymbolLength), out legendSymbolLength)) plot.LegendSymbolLength = legendSymbolLength;

                double legendSymbolMargin;
                if (GetDoubleAttribute(itemsElement, nameof(plot.LegendSymbolMargin), out legendSymbolMargin)) plot.LegendSymbolMargin = legendSymbolMargin;

                LegendSymbolPlacement legendSymbolPlacement;
                if (GetEnumAttribute(itemsElement, nameof(plot.LegendSymbolPlacement), out legendSymbolPlacement)) plot.LegendSymbolPlacement = legendSymbolPlacement;

                double legendColumnSpacing;
                if (GetDoubleAttribute(itemsElement, nameof(plot.LegendColumnSpacing), out legendColumnSpacing)) plot.LegendColumnSpacing = legendColumnSpacing;

                System.Windows.HorizontalAlignment legendItemAlignment;
                if (GetEnumAttribute(itemsElement, nameof(plot.LegendItemAlignment), out legendItemAlignment)) plot.LegendItemAlignment = legendItemAlignment;

                bool legendItemClickTogglesSeriesVisibility;
                if (GetBooleanAttribute(itemsElement, nameof(plot.LegendItemClickTogglesSeriesVisibility), out legendItemClickTogglesSeriesVisibility)) plot.LegendItemClickTogglesSeriesVisibility = legendItemClickTogglesSeriesVisibility;

                LegendItemOrder legendItemOrder;
                if (GetEnumAttribute(itemsElement, nameof(plot.LegendItemOrder), out legendItemOrder)) plot.LegendItemOrder = legendItemOrder;

                double legendItemSpacing;
                if (GetDoubleAttribute(itemsElement, nameof(plot.LegendItemSpacing), out legendItemSpacing)) plot.LegendItemSpacing = legendItemSpacing;

                double legendLineSpacing;
                if (GetDoubleAttribute(itemsElement, nameof(plot.LegendLineSpacing), out legendLineSpacing)) plot.LegendLineSpacing = legendLineSpacing;

                // Backward compatibility
                if (GetColorAttribute(itemsElement, "Color", out legendTextColor)) plot.LegendTextColor = legendTextColor;
                if (GetDoubleAttribute(itemsElement, "SymbolLength", out legendSymbolLength)) plot.LegendSymbolLength = legendSymbolLength;
                if (GetDoubleAttribute(itemsElement, "SymbolMargin", out legendSymbolMargin)) plot.LegendSymbolMargin = legendSymbolMargin;
                if (GetEnumAttribute(itemsElement, "SymbolPlacement", out legendSymbolPlacement)) plot.LegendSymbolPlacement = legendSymbolPlacement;
                if (GetDoubleAttribute(itemsElement, "ColumnSpacing", out legendColumnSpacing)) plot.LegendColumnSpacing = legendColumnSpacing;
                if (GetEnumAttribute(itemsElement, "ItemAlignment", out legendItemAlignment)) plot.LegendItemAlignment = legendItemAlignment;
                if (GetEnumAttribute(itemsElement, "ItemOrder", out legendItemOrder)) plot.LegendItemOrder = legendItemOrder;
                if (GetDoubleAttribute(itemsElement, "ItemSpacing", out legendItemSpacing)) plot.LegendItemSpacing = legendItemSpacing;
                if (GetDoubleAttribute(itemsElement, "LineSpacing", out legendLineSpacing)) plot.LegendLineSpacing = legendLineSpacing;
            }
        }

        #endregion

        #region XML Attribute Helper Methods

        /// <summary>
        /// Attempts to get a <see cref="Color"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="c">When this method returns, contains the parsed Color if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool GetColorAttribute(XElement el, string attributeName, out Color c)
        {
            c = default(Color);
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            try
            {
                c = (Color)ColorConverter.ConvertFromString(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to get a <see cref="Brush"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="converter">The <see cref="BrushConverter"/> to use for parsing.</param>
        /// <param name="b">When this method returns, contains the parsed Brush if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool GetBrushAttribute(XElement el, string attributeName, BrushConverter converter, out Brush? b)
        {
            b = null;
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            try
            {
                b = (Brush?)converter.ConvertFromInvariantString(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to get a string value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="s">When this method returns, contains the attribute value if successful.</param>
        /// <returns><c>true</c> if the attribute exists; otherwise, <c>false</c>.</returns>
        public static bool GetStringAttribute(XElement el, string attributeName, out string? s)
        {
            s = null;
            if (el.Attribute(attributeName) == null) return false;

            s = el.Attribute(attributeName)!.Value;
            return true;
        }

        /// <summary>
        /// Attempts to get a <see cref="double"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="d">When this method returns, contains the parsed double if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool GetDoubleAttribute(XElement el, string attributeName, out double d)
        {
            d = 0;
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
        }

        /// <summary>
        /// Attempts to get an <see cref="int"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="i">When this method returns, contains the parsed integer if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool GetIntegerAttribute(XElement el, string attributeName, out int i)
        {
            i = 0;
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out i);
        }

        /// <summary>
        /// Attempts to get a <see cref="bool"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="b">When this method returns, contains the parsed boolean if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool GetBooleanAttribute(XElement el, string attributeName, out bool b)
        {
            b = false;
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            return bool.TryParse(value, out b);
        }

        /// <summary>
        /// Attempts to get a <see cref="FontFamily"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="converter">The <see cref="FontFamilyConverter"/> to use for parsing.</param>
        /// <param name="ff">When this method returns, contains the parsed FontFamily if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool GetFontFamilyAttribute(XElement el, string attributeName, FontFamilyConverter converter, out FontFamily? ff)
        {
            ff = null;
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            try
            {
                ff = (FontFamily?)converter.ConvertFromInvariantString(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to get a <see cref="FontWeight"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="converter">The <see cref="FontWeightConverter"/> to use for parsing.</param>
        /// <param name="fw">When this method returns, contains the parsed FontWeight if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool GetFontWeightAttribute(XElement el, string attributeName, FontWeightConverter converter, out FontWeight fw)
        {
            fw = default(FontWeight);
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            try
            {
                fw = (FontWeight)converter.ConvertFromInvariantString(value)!;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to get a <see cref="Thickness"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="converter">The <see cref="ThicknessConverter"/> to use for parsing.</param>
        /// <param name="t">When this method returns, contains the parsed Thickness if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool GetThicknessAttribute(XElement el, string attributeName, System.Windows.ThicknessConverter converter, out Thickness t)
        {
            t = default(Thickness);
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            try
            {
                t = (Thickness)converter.ConvertFromInvariantString(value)!;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to get an enum value from an XML element attribute.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to parse.</typeparam>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="e">When this method returns, contains the parsed enum value if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool GetEnumAttribute<TEnum>(XElement el, string attributeName, out TEnum e) where TEnum : struct
        {
            e = default(TEnum);
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            return Enum.TryParse(value, out e);
        }

        /// <summary>
        /// Attempts to get an OxyPlot <see cref="OxyPlot.DataPoint"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="dp">When this method returns, contains the parsed DataPoint if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool GetDataPointAttribute(XElement el, string attributeName, out OxyPlot.DataPoint dp)
        {
            dp = OxyPlot.DataPoint.Undefined;
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            dp = value.FromPrettyDataText();
            return !dp.Equals(OxyPlot.DataPoint.Undefined);
        }

        /// <summary>
        /// Attempts to get an OxyPlot <see cref="OxyPlot.ScreenVector"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="vp">When this method returns, contains the parsed ScreenVector if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Distinguishes a genuine <c>(0,0)</c> value from a malformed attribute: returns
        /// <c>true</c> only when the attribute exists and parses to a valid pair of doubles.
        /// </remarks>
        public static bool GetScreenVectorAttribute(XElement el, string attributeName, out OxyPlot.ScreenVector vp)
        {
            vp = default(OxyPlot.ScreenVector);
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            return value.TryFromPrettyVectorText(out vp);
        }

        /// <summary>
        /// Attempts to get an OxyPlot <see cref="OxyPlot.ScreenPoint"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="vp">When this method returns, contains the parsed ScreenPoint if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool GetScreenPointAttribute(XElement el, string attributeName, out OxyPlot.ScreenPoint vp)
        {
            vp = OxyPlot.ScreenPoint.Undefined;
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            vp = value.FromPrettyScreenText();
            return !vp.Equals(OxyPlot.ScreenPoint.Undefined);
        }

        /// <summary>
        /// Attempts to get a WPF <see cref="Vector"/> value from an XML element attribute.
        /// </summary>
        /// <param name="el">The <see cref="XElement"/> to read from.</param>
        /// <param name="attributeName">The name of the attribute to read.</param>
        /// <param name="v">When this method returns, contains the parsed Vector if successful.</param>
        /// <returns><c>true</c> if the attribute exists and was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Distinguishes a genuine <c>(0,0)</c> value from a malformed attribute: returns
        /// <c>true</c> only when the attribute exists and parses to a valid pair of doubles.
        /// </remarks>
        public static bool GetVectorAttribute(XElement el, string attributeName, out Vector v)
        {
            v = default(Vector);
            if (el.Attribute(attributeName) == null) return false;
            string value = el.Attribute(attributeName)!.Value;
            if (string.IsNullOrEmpty(value)) return false;

            return value.TryFromPrettyVectorString(out v);
        }

        #endregion

        #region Internal Helpers

        /// <summary>
        /// Deserializes a XAML element from an <see cref="XElement"/>.
        /// </summary>
        /// <param name="element">The <see cref="XElement"/> to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        /// <remarks>
        /// Used for backward compatibility when deserializing legacy BackgroundBrush/BorderBrush elements
        /// that were serialized as full XAML markup.
        /// </remarks>
        internal static object DeserializeFromXElement(XElement element)
        {
            try
            {
                var converter = new BrushConverter();
                return converter.ConvertFromInvariantString(element.Value);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
