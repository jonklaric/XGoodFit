// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AxisSerializer.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides serialization and deserialization of OxyPlot axis properties to and from XML.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#nullable enable annotations
#nullable disable warnings

namespace OxyPlot.Wpf.Serialization
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media;
    using System.Xml.Linq;
    using static PlotSerializer;

    /// <summary>
    /// Provides serialization and deserialization of OxyPlot <see cref="Axis"/> properties to and from XML.
    /// Handles all axis types including Linear, Logarithmic, DateTime, Category, Angle, Magnitude,
    /// LinearColor, TimeSpan, NormalProbability, and GumbelProbability axes.
    /// </summary>
    public static class AxisSerializer
    {
        /// <summary>
        /// Shared <see cref="FontWeightConverter"/> instance used for serializing and deserializing font weights.
        /// </summary>
        private static readonly FontWeightConverter FontWeightConverterInstance = new FontWeightConverter();

        /// <summary>
        /// Visual property names on <see cref="Axis"/> that are serialized for settings persistence.
        /// Includes properties common to all axis types. Consumers can use this list to monitor
        /// these properties for change tracking.
        /// </summary>
        public static readonly IReadOnlyList<string> AxisVisualProperties = new[]
        {
            // General
            "Name", "IsEnabled", "IsAxisVisible", "Layer", "StartPosition", "EndPosition", "IsPanEnabled", "IsZoomEnabled",
            "Maximum", "Minimum", "AbsoluteMaximum", "AbsoluteMinimum", "FilterMaxValue", "FilterMinValue",
            // Style
            "AxislineColor", "AxislineStyle", "AxislineThickness",
            // Position
            "AxisDistance", "PositionAtZeroCrossing", "Position", "Key", "PositionTier",
            // Title
            "Title", "TitleColor", "TitleFont", "TitleFontSize", "TitleFontWeight", "AxisTitleDistance", "Unit",
            // Labels
            "TextColor", "Font", "FontSize", "FontWeight", "Angle", "AxisTickToLabelDistance",
            "StringFormat", "UseSuperExponentialFormat",
            // Major gridlines
            "MajorGridlineColor", "MajorGridlineStyle", "MajorGridlineThickness", "MajorStep", "MajorTickSize",
            // Minor gridlines
            "MinorGridlineColor", "MinorGridlineStyle", "MinorGridlineThickness", "MinorStep", "MinorTickSize",
            // Extra gridlines
            "ExtraGridlineColor", "ExtraGridlineStyle", "ExtraGridlineThickness",
            // Ticks
            "TickStyle", "TicklineColor",
        };

        /// <summary>
        /// Serializes all axes from a <see cref="Plot"/> to an <see cref="XElement"/>.
        /// </summary>
        /// <param name="plot">The plot containing axes to serialize.</param>
        /// <returns>An <see cref="XElement"/> containing all serialized axes.</returns>
        public static XElement AxesToXElement(Plot plot)
        {
            var axesProperties = new XElement(AxesPropertiesTag);
            foreach (var axis in plot.Axes)
            {
                axesProperties.Add(AxisToXElement(axis));
            }

            return axesProperties;
        }

        /// <summary>
        /// Deserializes axes from an <see cref="XElement"/> and applies them to the plot.
        /// </summary>
        /// <param name="plot">The plot to add deserialized axes to.</param>
        /// <param name="element">The <see cref="XElement"/> containing serialized axes.</param>
        /// <remarks>
        /// This method clears the existing axes collection before adding deserialized axes.
        /// </remarks>
        public static void XElementToAxes(Plot plot, XElement element)
        {
            if (element.Name != AxesPropertiesTag) return;

            plot.Axes.Clear();
            Axis? tempAxis;
            foreach (var el in element.Elements(AxisPropertiesTag))
            {
                tempAxis = XElementToAxis(el);
                if (tempAxis == null) continue;
                plot.Axes.Add(tempAxis);
            }
        }

        /// <summary>
        /// Serializes a single <see cref="Axis"/> to an <see cref="XElement"/>.
        /// </summary>
        /// <param name="axis">The axis to serialize.</param>
        /// <returns>An <see cref="XElement"/> containing the serialized axis properties.</returns>
        public static XElement AxisToXElement(Axis axis)
        {
            var axisProperties = new XElement(AxisPropertiesTag);
            axisProperties.SetAttributeValue("AxisType", axis.GetType().ToString());

            // General Properties
            var generalProperties = new XElement("General");
            generalProperties.SetAttributeValue(nameof(Axis.Name), axis.Name ?? "");
            generalProperties.SetAttributeValue(nameof(axis.IsEnabled), axis.IsEnabled.ToString());
            generalProperties.SetAttributeValue(nameof(axis.IsAxisVisible), axis.IsAxisVisible.ToString());
            generalProperties.SetAttributeValue(nameof(axis.Layer), axis.Layer.ToString());
            generalProperties.SetAttributeValue(nameof(axis.StartPosition), axis.StartPosition.ToString("G17", CultureInfo.InvariantCulture));
            generalProperties.SetAttributeValue(nameof(axis.EndPosition), axis.EndPosition.ToString("G17", CultureInfo.InvariantCulture));
            generalProperties.SetAttributeValue(nameof(axis.IsPanEnabled), axis.IsPanEnabled.ToString());
            generalProperties.SetAttributeValue(nameof(axis.IsZoomEnabled), axis.IsZoomEnabled.ToString());
            axisProperties.Add(generalProperties);

            // Numeric Properties
            var numericProperties = new XElement("Numbers");
            numericProperties.SetAttributeValue(nameof(axis.Maximum), axis.Maximum.ToString("G17", CultureInfo.InvariantCulture));
            numericProperties.SetAttributeValue(nameof(axis.Minimum), axis.Minimum.ToString("G17", CultureInfo.InvariantCulture));
            numericProperties.SetAttributeValue(nameof(axis.AbsoluteMaximum), axis.AbsoluteMaximum.ToString("G17", CultureInfo.InvariantCulture));
            numericProperties.SetAttributeValue(nameof(axis.AbsoluteMinimum), axis.AbsoluteMinimum.ToString("G17", CultureInfo.InvariantCulture));
            numericProperties.SetAttributeValue(nameof(axis.FilterMaxValue), axis.FilterMaxValue.ToString("G17", CultureInfo.InvariantCulture));
            numericProperties.SetAttributeValue(nameof(axis.FilterMinValue), axis.FilterMinValue.ToString("G17", CultureInfo.InvariantCulture));
            // User-settable padding/range properties — previously not persisted. Only the
            // properties exposed as DPs on the WPF Axis wrapper are serialized here:
            // MaximumPadding, MinimumPadding, MaximumRange, MinimumRange.
            // Margin and data-margin properties exist on the model but are not WPF DPs;
            // adding them as wrapper DPs is tracked as a future enhancement.
            numericProperties.SetAttributeValue(nameof(axis.MaximumPadding), axis.MaximumPadding.ToString("G17", CultureInfo.InvariantCulture));
            numericProperties.SetAttributeValue(nameof(axis.MinimumPadding), axis.MinimumPadding.ToString("G17", CultureInfo.InvariantCulture));
            numericProperties.SetAttributeValue(nameof(axis.MaximumRange), axis.MaximumRange.ToString("G17", CultureInfo.InvariantCulture));
            numericProperties.SetAttributeValue(nameof(axis.MinimumRange), axis.MinimumRange.ToString("G17", CultureInfo.InvariantCulture));
            axisProperties.Add(numericProperties);

            // Style Properties
            var styleProperties = new XElement("Style");
            styleProperties.SetAttributeValue(nameof(axis.AxislineColor), axis.AxislineColor.ToString());
            styleProperties.SetAttributeValue(nameof(axis.AxislineStyle), axis.AxislineStyle.ToString());
            styleProperties.SetAttributeValue(nameof(axis.AxislineThickness), axis.AxislineThickness.ToString("G17", CultureInfo.InvariantCulture));
            axisProperties.Add(styleProperties);

            // Position Properties
            var positionProperties = new XElement("Position");
            positionProperties.SetAttributeValue(nameof(axis.AxisDistance), axis.AxisDistance.ToString("G17", CultureInfo.InvariantCulture));
            positionProperties.SetAttributeValue(nameof(axis.PositionAtZeroCrossing), axis.PositionAtZeroCrossing.ToString());
            positionProperties.SetAttributeValue(nameof(axis.Position), axis.Position.ToString());
            positionProperties.SetAttributeValue(nameof(axis.Key), axis.Key);
            positionProperties.SetAttributeValue(nameof(axis.PositionTier), axis.PositionTier);
            axisProperties.Add(positionProperties);

            // Title Properties
            var titleProperties = new XElement("Title");
            titleProperties.SetAttributeValue(nameof(axis.Title), axis.Title);
            titleProperties.SetAttributeValue(nameof(axis.TitleColor), axis.TitleColor.ToString());
            titleProperties.SetAttributeValue(nameof(axis.TitleFont), axis.TitleFont);
            titleProperties.SetAttributeValue(nameof(axis.TitleFontSize), axis.TitleFontSize.ToString("G17", CultureInfo.InvariantCulture));
            titleProperties.SetAttributeValue(nameof(axis.TitleFontWeight), FontWeightConverterInstance.ConvertToInvariantString(axis.TitleFontWeight));
            titleProperties.SetAttributeValue(nameof(axis.AxisTitleDistance), axis.AxisTitleDistance.ToString("G17", CultureInfo.InvariantCulture));
            titleProperties.SetAttributeValue(nameof(axis.Unit), axis.Unit);
            titleProperties.SetAttributeValue(nameof(axis.ClipTitle), axis.ClipTitle.ToString());
            titleProperties.SetAttributeValue(nameof(axis.TitleClippingLength), axis.TitleClippingLength.ToString("G17", CultureInfo.InvariantCulture));
            axisProperties.Add(titleProperties);

            // Label Properties
            var labelProperties = new XElement("Labels");
            labelProperties.SetAttributeValue(nameof(axis.TextColor), axis.TextColor.ToString());
            labelProperties.SetAttributeValue(nameof(axis.Font), axis.Font);
            labelProperties.SetAttributeValue(nameof(axis.FontSize), axis.FontSize.ToString("G17", CultureInfo.InvariantCulture));
            labelProperties.SetAttributeValue(nameof(axis.FontWeight), FontWeightConverterInstance.ConvertToInvariantString(axis.FontWeight));
            labelProperties.SetAttributeValue(nameof(axis.Angle), axis.Angle.ToString("G17", CultureInfo.InvariantCulture));
            labelProperties.SetAttributeValue(nameof(axis.AxisTickToLabelDistance), axis.AxisTickToLabelDistance.ToString("G17", CultureInfo.InvariantCulture));
            labelProperties.SetAttributeValue(nameof(axis.StringFormat), axis.StringFormat);
            labelProperties.SetAttributeValue(nameof(axis.UseSuperExponentialFormat), axis.UseSuperExponentialFormat.ToString());
            // IntervalLength controls label-density target — previously not persisted.
            labelProperties.SetAttributeValue(nameof(axis.IntervalLength), axis.IntervalLength.ToString("G17", CultureInfo.InvariantCulture));
            axisProperties.Add(labelProperties);

            // Major Gridline Properties
            var majorGridlineProperties = new XElement("MajorGridlines");
            majorGridlineProperties.SetAttributeValue(nameof(axis.MajorGridlineColor), axis.MajorGridlineColor.ToString());
            majorGridlineProperties.SetAttributeValue(nameof(axis.MajorGridlineStyle), axis.MajorGridlineStyle.ToString());
            majorGridlineProperties.SetAttributeValue(nameof(axis.MajorGridlineThickness), axis.MajorGridlineThickness.ToString("G17", CultureInfo.InvariantCulture));
            majorGridlineProperties.SetAttributeValue(nameof(axis.MajorStep), axis.MajorStep.ToString("G17", CultureInfo.InvariantCulture));
            majorGridlineProperties.SetAttributeValue(nameof(axis.MajorTickSize), axis.MajorTickSize.ToString("G17", CultureInfo.InvariantCulture));
            // MinimumMajorStep, MaximumMajorIntervalCount, MinimumMajorIntervalCount,
            // CropGridlines exist on the model but not as WPF Axis wrapper DPs; not
            // serialized here. Add them as wrapper DPs (with Plot.SynchronizeProperties
            // wiring) before persisting via this serializer.
            axisProperties.Add(majorGridlineProperties);

            // Minor Gridline Properties
            var minorGridlineProperties = new XElement("MinorGridlines");
            minorGridlineProperties.SetAttributeValue(nameof(axis.MinorGridlineColor), axis.MinorGridlineColor.ToString());
            minorGridlineProperties.SetAttributeValue(nameof(axis.MinorGridlineStyle), axis.MinorGridlineStyle.ToString());
            minorGridlineProperties.SetAttributeValue(nameof(axis.MinorGridlineThickness), axis.MinorGridlineThickness.ToString("G17", CultureInfo.InvariantCulture));
            minorGridlineProperties.SetAttributeValue(nameof(axis.MinorStep), axis.MinorStep.ToString("G17", CultureInfo.InvariantCulture));
            minorGridlineProperties.SetAttributeValue(nameof(axis.MinorTickSize), axis.MinorTickSize.ToString("G17", CultureInfo.InvariantCulture));
            // MinimumMinorStep exists on the model but not as a WPF Axis wrapper DP;
            // not serialized here.
            axisProperties.Add(minorGridlineProperties);

            // Extra Gridline Properties
            var extraGridlineProperties = new XElement("ExtraGridlines");
            extraGridlineProperties.SetAttributeValue(nameof(axis.ExtraGridlineColor), axis.ExtraGridlineColor.ToString());
            extraGridlineProperties.SetAttributeValue(nameof(axis.ExtraGridlineStyle), axis.ExtraGridlineStyle.ToString());
            extraGridlineProperties.SetAttributeValue(nameof(axis.ExtraGridlineThickness), axis.ExtraGridlineThickness.ToString("G17", CultureInfo.InvariantCulture));
            // Explicit gridline values (the axis.ExtraGridlines double[]) — previously not persisted.
            extraGridlineProperties.Add(axis.ExtraGridlines.ToXElement(nameof(axis.ExtraGridlines)));
            axisProperties.Add(extraGridlineProperties);

            // Tick Style Properties
            var tickStyleProperties = new XElement("Tick");
            tickStyleProperties.SetAttributeValue(nameof(axis.TickStyle), axis.TickStyle.ToString());
            tickStyleProperties.SetAttributeValue(nameof(axis.TicklineColor), axis.TicklineColor.ToString());
            // MinorTicklineColor exists on the model but not as a WPF Axis wrapper DP;
            // not serialized here.
            axisProperties.Add(tickStyleProperties);

            // Concrete axis implementation properties
            var axisType = axis.GetType();
            if (axisType == typeof(LinearAxis))
            {
                var linearAxis = (LinearAxis)axis;
                var linearAxisProperties = new XElement("LinearAxis");
                linearAxisProperties.SetAttributeValue(nameof(linearAxis.FormatAsFractions), linearAxis.FormatAsFractions.ToString());
                axisProperties.Add(linearAxisProperties);
            }
            else if (axisType == typeof(CategoryAxis))
            {
                var categoryAxis = (CategoryAxis)axis;
                var categoryAxisProperties = new XElement("CategoryAxis");
                categoryAxisProperties.SetAttributeValue(nameof(categoryAxis.IsTickCentered), categoryAxis.IsTickCentered.ToString());
                categoryAxisProperties.SetAttributeValue(nameof(categoryAxis.GapWidth), categoryAxis.GapWidth.ToString("G17", CultureInfo.InvariantCulture));
                axisProperties.Add(categoryAxisProperties);
            }
            else if (axisType == typeof(LogarithmicAxis))
            {
                var logAxis = (LogarithmicAxis)axis;
                var logAxisProperties = new XElement("LogarithmicAxis");
                logAxisProperties.SetAttributeValue(nameof(logAxis.Base), logAxis.Base.ToString("G17", CultureInfo.InvariantCulture));
                logAxisProperties.SetAttributeValue(nameof(logAxis.PowerPadding), logAxis.PowerPadding.ToString());
                axisProperties.Add(logAxisProperties);
            }
            else if (axisType == typeof(DateTimeAxis))
            {
                var dateAxis = (DateTimeAxis)axis;
                var dateAxisProperties = new XElement("DateTimeAxis");
                dateAxisProperties.SetAttributeValue(nameof(dateAxis.CalendarWeekRule), dateAxis.CalendarWeekRule.ToString());
                axisProperties.Add(dateAxisProperties);
            }
            else if (axisType == typeof(TimeSpanAxis))
            {
                var timeSpanAxisProperties = new XElement("TimeSpanAxis");
                axisProperties.Add(timeSpanAxisProperties);
            }
            else if (axisType == typeof(AngleAxis))
            {
                var angleAxis = (AngleAxis)axis;
                var angleAxisProperties = new XElement("AngleAxis");
                angleAxisProperties.SetAttributeValue(nameof(angleAxis.StartAngle), angleAxis.StartAngle.ToString("G17", CultureInfo.InvariantCulture));
                angleAxisProperties.SetAttributeValue(nameof(angleAxis.EndAngle), angleAxis.EndAngle.ToString("G17", CultureInfo.InvariantCulture));
                axisProperties.Add(angleAxisProperties);
            }
            else if (axisType == typeof(MagnitudeAxis))
            {
                var magnitudeAxis = (MagnitudeAxis)axis;
                var magnitudeAxisProperties = new XElement("MagnitudeAxis");
                magnitudeAxisProperties.SetAttributeValue(nameof(magnitudeAxis.FormatAsFractions), magnitudeAxis.FormatAsFractions.ToString());
                axisProperties.Add(magnitudeAxisProperties);
            }
            else if (axisType == typeof(LinearColorAxis))
            {
                var linearColorAxis = (LinearColorAxis)axis;
                var linearColorAxisProperties = new XElement("LinearColorAxis");
                linearColorAxisProperties.SetAttributeValue(nameof(linearColorAxis.HighColor), linearColorAxis.HighColor.ToString());
                linearColorAxisProperties.SetAttributeValue(nameof(linearColorAxis.LowColor), linearColorAxis.LowColor.ToString());
                linearColorAxisProperties.SetAttributeValue(nameof(linearColorAxis.PaletteSize), linearColorAxis.PaletteSize.ToString());
                linearColorAxisProperties.SetAttributeValue(nameof(linearColorAxis.InvalidNumberColor), linearColorAxis.InvalidNumberColor.ToString());
                axisProperties.Add(linearColorAxisProperties);
            }
            else if (axisType == typeof(NormalProbabilityAxis))
            {
                var normalProbabilityAxisProperties = new XElement("NormalProbabilityAxis");
                axisProperties.Add(normalProbabilityAxisProperties);
            }
            else if (axisType == typeof(GumbelProbabilityAxis))
            {
                var gumbelProbabilityAxisProperties = new XElement("GumbelProbabilityAxis");
                axisProperties.Add(gumbelProbabilityAxisProperties);
            }
            else if (axis is CategoryColorAxis categoryColorAxis)
            {
                var categoryColorAxisProperties = new XElement("CategoryColorAxis");
                categoryColorAxisProperties.SetAttributeValue(nameof(categoryColorAxis.IsTickCentered), categoryColorAxis.IsTickCentered.ToString());
                categoryColorAxisProperties.SetAttributeValue(nameof(categoryColorAxis.GapWidth), categoryColorAxis.GapWidth.ToString("G17", CultureInfo.InvariantCulture));
                categoryColorAxisProperties.SetAttributeValue(nameof(categoryColorAxis.InvalidCategoryColor), categoryColorAxis.InvalidCategoryColor.ToString());
                axisProperties.Add(categoryColorAxisProperties);
            }
            else if (axis is LogarithmicColorAxis logarithmicColorAxis)
            {
                var logColorAxisProperties = new XElement("LogarithmicColorAxis");
                logColorAxisProperties.SetAttributeValue(nameof(logarithmicColorAxis.HighColor), logarithmicColorAxis.HighColor.ToString());
                logColorAxisProperties.SetAttributeValue(nameof(logarithmicColorAxis.LowColor), logarithmicColorAxis.LowColor.ToString());
                logColorAxisProperties.SetAttributeValue(nameof(logarithmicColorAxis.PaletteSize), logarithmicColorAxis.PaletteSize.ToString());
                logColorAxisProperties.SetAttributeValue(nameof(logarithmicColorAxis.InvalidNumberColor), logarithmicColorAxis.InvalidNumberColor.ToString());
                logColorAxisProperties.SetAttributeValue(nameof(logarithmicColorAxis.RenderAsImage), logarithmicColorAxis.RenderAsImage.ToString());
                logColorAxisProperties.SetAttributeValue(nameof(logarithmicColorAxis.Base), logarithmicColorAxis.Base.ToString("G17", CultureInfo.InvariantCulture));
                logColorAxisProperties.SetAttributeValue(nameof(logarithmicColorAxis.PowerPadding), logarithmicColorAxis.PowerPadding.ToString());
                axisProperties.Add(logColorAxisProperties);
            }
            else if (axis is RangeColorAxis rangeColorAxis)
            {
                var rangeColorAxisProperties = new XElement("RangeColorAxis");
                rangeColorAxisProperties.SetAttributeValue(nameof(rangeColorAxis.HighColor), rangeColorAxis.HighColor.ToString());
                rangeColorAxisProperties.SetAttributeValue(nameof(rangeColorAxis.LowColor), rangeColorAxis.LowColor.ToString());
                rangeColorAxisProperties.SetAttributeValue(nameof(rangeColorAxis.InvalidNumberColor), rangeColorAxis.InvalidNumberColor.ToString());
                rangeColorAxisProperties.SetAttributeValue(nameof(rangeColorAxis.FormatAsFractions), rangeColorAxis.FormatAsFractions.ToString());
                axisProperties.Add(rangeColorAxisProperties);
            }
            else if (axis is AngleAxisFullPlotArea angleAxisFullPlotArea)
            {
                var angleFullProperties = new XElement("AngleAxisFullPlotArea");
                angleFullProperties.SetAttributeValue(nameof(angleAxisFullPlotArea.StartAngle), angleAxisFullPlotArea.StartAngle.ToString("G17", CultureInfo.InvariantCulture));
                angleFullProperties.SetAttributeValue(nameof(angleAxisFullPlotArea.EndAngle), angleAxisFullPlotArea.EndAngle.ToString("G17", CultureInfo.InvariantCulture));
                axisProperties.Add(angleFullProperties);
            }
            else if (axis is MagnitudeAxisFullPlotArea magnitudeAxisFullPlotArea)
            {
                var magFullProperties = new XElement("MagnitudeAxisFullPlotArea");
                magFullProperties.SetAttributeValue(nameof(magnitudeAxisFullPlotArea.FormatAsFractions), magnitudeAxisFullPlotArea.FormatAsFractions.ToString());
                magFullProperties.SetAttributeValue(nameof(magnitudeAxisFullPlotArea.MidshiftH), magnitudeAxisFullPlotArea.MidshiftH.ToString("G17", CultureInfo.InvariantCulture));
                magFullProperties.SetAttributeValue(nameof(magnitudeAxisFullPlotArea.MidshiftV), magnitudeAxisFullPlotArea.MidshiftV.ToString("G17", CultureInfo.InvariantCulture));
                axisProperties.Add(magFullProperties);
            }

            return axisProperties;
        }

        /// <summary>
        /// Deserializes axis properties from an <see cref="XElement"/>.
        /// </summary>
        /// <param name="element">The <see cref="XElement"/> containing the axis properties.</param>
        /// <param name="targetAxis">Optional target axis to apply properties to. If null, a new axis is created.</param>
        /// <returns>A new or updated <see cref="Axis"/> with the deserialized properties, or null if the element is invalid.</returns>
        /// <remarks>
        /// Supports backward-compatible attribute names for older XML formats.
        /// </remarks>
        public static Axis? XElementToAxis(XElement element, Axis? targetAxis = null)
        {
            if (element.Name != AxisPropertiesTag) return null;

            Axis axis;
            string axisType = "";
            var axisTypeAttr = element.Attribute("AxisType");
            if (axisTypeAttr != null) axisType = axisTypeAttr.Value;

            if (targetAxis != null)
            {
                axis = targetAxis;
            }
            else
            {
                if (axisType == typeof(LinearAxis).ToString())
                    axis = new LinearAxis();
                else if (axisType == typeof(CategoryAxis).ToString())
                    axis = new CategoryAxis();
                else if (axisType == typeof(LogarithmicAxis).ToString())
                    axis = new LogarithmicAxis();
                else if (axisType == typeof(DateTimeAxis).ToString())
                    axis = new DateTimeAxis();
                else if (axisType == typeof(AngleAxis).ToString())
                    axis = new AngleAxis();
                else if (axisType == typeof(LinearColorAxis).ToString())
                    axis = new LinearColorAxis();
                else if (axisType == typeof(MagnitudeAxis).ToString())
                    axis = new MagnitudeAxis();
                else if (axisType == typeof(TimeSpanAxis).ToString())
                    axis = new TimeSpanAxis();
                else if (axisType == typeof(NormalProbabilityAxis).ToString())
                    axis = new NormalProbabilityAxis();
                else if (axisType == typeof(GumbelProbabilityAxis).ToString())
                    axis = new GumbelProbabilityAxis();
                else if (axisType == typeof(CategoryColorAxis).ToString())
                    axis = new CategoryColorAxis();
                else if (axisType == typeof(LogarithmicColorAxis).ToString())
                    axis = new LogarithmicColorAxis();
                else if (axisType == typeof(RangeColorAxis).ToString())
                    axis = new RangeColorAxis();
                else if (axisType == typeof(AngleAxisFullPlotArea).ToString())
                    axis = new AngleAxisFullPlotArea();
                else if (axisType == typeof(MagnitudeAxisFullPlotArea).ToString())
                    axis = new MagnitudeAxisFullPlotArea();
                else
                    axis = new LinearAxis();
            }

            // General Properties
            var generalElement = element.Element("General");
            if (generalElement != null)
            {
                if (GetStringAttribute(generalElement, nameof(axis.Name), out var name)) axis.Name = name;
                if (GetBooleanAttribute(generalElement, nameof(axis.IsEnabled), out var isEnabled)) axis.IsEnabled = isEnabled;
                if (GetBooleanAttribute(generalElement, nameof(axis.IsAxisVisible), out var isAxisVisible)) axis.IsAxisVisible = isAxisVisible;
                if (GetEnumAttribute(generalElement, nameof(axis.Layer), out OxyPlot.Axes.AxisLayer layer)) axis.Layer = layer;
                if (GetDoubleAttribute(generalElement, nameof(axis.StartPosition), out var startPosition)) axis.StartPosition = startPosition;
                if (GetDoubleAttribute(generalElement, nameof(axis.EndPosition), out var endPosition)) axis.EndPosition = endPosition;
                if (GetBooleanAttribute(generalElement, nameof(axis.IsPanEnabled), out var isPanEnabled)) axis.IsPanEnabled = isPanEnabled;
                if (GetBooleanAttribute(generalElement, nameof(axis.IsZoomEnabled), out var isZoomEnabled)) axis.IsZoomEnabled = isZoomEnabled;

                // Backward compatibility
                if (GetBooleanAttribute(generalElement, "AxisVisible", out isAxisVisible)) axis.IsAxisVisible = isAxisVisible;
                if (GetBooleanAttribute(generalElement, "CanPan", out isPanEnabled)) axis.IsPanEnabled = isPanEnabled;
                if (GetBooleanAttribute(generalElement, "CanZoom", out isZoomEnabled)) axis.IsZoomEnabled = isZoomEnabled;
            }

            // Number Properties
            var numbersElement = element.Element("Numbers");
            if (numbersElement != null)
            {
                if (GetDoubleAttribute(numbersElement, nameof(axis.Maximum), out var maximum)) axis.Maximum = maximum;
                if (GetDoubleAttribute(numbersElement, nameof(axis.Minimum), out var minimum)) axis.Minimum = minimum;
                if (GetDoubleAttribute(numbersElement, nameof(axis.AbsoluteMaximum), out var absoluteMaximum)) axis.AbsoluteMaximum = absoluteMaximum;
                if (GetDoubleAttribute(numbersElement, nameof(axis.AbsoluteMinimum), out var absoluteMinimum)) axis.AbsoluteMinimum = absoluteMinimum;
                if (GetDoubleAttribute(numbersElement, nameof(axis.FilterMaxValue), out var filterMaxValue)) axis.FilterMaxValue = filterMaxValue;
                if (GetDoubleAttribute(numbersElement, nameof(axis.FilterMinValue), out var filterMinValue)) axis.FilterMinValue = filterMinValue;
                if (GetDoubleAttribute(numbersElement, nameof(axis.MaximumPadding), out var maximumPadding)) axis.MaximumPadding = maximumPadding;
                if (GetDoubleAttribute(numbersElement, nameof(axis.MinimumPadding), out var minimumPadding)) axis.MinimumPadding = minimumPadding;
                if (GetDoubleAttribute(numbersElement, nameof(axis.MaximumRange), out var maximumRange)) axis.MaximumRange = maximumRange;
                if (GetDoubleAttribute(numbersElement, nameof(axis.MinimumRange), out var minimumRange)) axis.MinimumRange = minimumRange;
            }

            // Style Properties
            var styleElement = element.Element("Style");
            if (styleElement != null)
            {
                if (GetColorAttribute(styleElement, nameof(axis.AxislineColor), out var axislineColor)) axis.AxislineColor = axislineColor;
                if (GetEnumAttribute(styleElement, nameof(axis.AxislineStyle), out OxyPlot.LineStyle axislineStyle)) axis.AxislineStyle = axislineStyle;
                if (GetDoubleAttribute(styleElement, nameof(axis.AxislineThickness), out var axislineThickness)) axis.AxislineThickness = axislineThickness;

                // Backward compatibility
                if (GetColorAttribute(styleElement, "Color", out axislineColor)) axis.AxislineColor = axislineColor;
                if (GetEnumAttribute(styleElement, "Style", out axislineStyle)) axis.AxislineStyle = axislineStyle;
                if (GetDoubleAttribute(styleElement, "Thickness", out axislineThickness)) axis.AxislineThickness = axislineThickness;
            }

            // Position Properties
            var positionElement = element.Element("Position");
            if (positionElement != null)
            {
                if (GetDoubleAttribute(positionElement, nameof(axis.AxisDistance), out var axisDistance)) axis.AxisDistance = axisDistance;
                if (GetBooleanAttribute(positionElement, nameof(axis.PositionAtZeroCrossing), out var positionAtZeroCrossing)) axis.PositionAtZeroCrossing = positionAtZeroCrossing;
                if (GetEnumAttribute(positionElement, nameof(axis.Position), out OxyPlot.Axes.AxisPosition position)) axis.Position = position;
                if (GetStringAttribute(positionElement, nameof(axis.Key), out var key)) axis.Key = key;
                if (GetIntegerAttribute(positionElement, nameof(axis.PositionTier), out var positionTier)) axis.PositionTier = positionTier;

                // Backward compatibility
                if (GetDoubleAttribute(positionElement, "Distance", out axisDistance)) axis.AxisDistance = axisDistance;
                if (GetBooleanAttribute(positionElement, "ZeroCrossing", out positionAtZeroCrossing)) axis.PositionAtZeroCrossing = positionAtZeroCrossing;
                if (GetIntegerAttribute(positionElement, "Tier", out positionTier)) axis.PositionTier = positionTier;
            }

            // Title Properties
            var titleElement = element.Element("Title");
            if (titleElement != null)
            {
                if (GetStringAttribute(titleElement, nameof(axis.Title), out var title)) axis.Title = title;
                if (GetColorAttribute(titleElement, nameof(axis.TitleColor), out var titleColor)) axis.TitleColor = titleColor;
                if (GetStringAttribute(titleElement, nameof(axis.TitleFont), out var titleFont)) axis.TitleFont = titleFont;
                if (GetDoubleAttribute(titleElement, nameof(axis.TitleFontSize), out var titleFontSize)) axis.TitleFontSize = titleFontSize;
                if (GetFontWeightAttribute(titleElement, nameof(axis.TitleFontWeight), FontWeightConverterInstance, out var titleFontWeight)) axis.TitleFontWeight = titleFontWeight;
                if (GetDoubleAttribute(titleElement, nameof(axis.AxisTitleDistance), out var axisTitleDistance)) axis.AxisTitleDistance = axisTitleDistance;
                if (GetStringAttribute(titleElement, nameof(axis.Unit), out var unit)) axis.Unit = unit;
                if (GetBooleanAttribute(titleElement, nameof(axis.ClipTitle), out var clipTitle)) axis.ClipTitle = clipTitle;
                if (GetDoubleAttribute(titleElement, nameof(axis.TitleClippingLength), out var titleClippingLength)) axis.TitleClippingLength = titleClippingLength;

                // Backward compatibility
                if (GetColorAttribute(titleElement, "Color", out titleColor)) axis.TitleColor = titleColor;
                if (GetStringAttribute(titleElement, "Font", out titleFont)) axis.TitleFont = titleFont;
                if (GetDoubleAttribute(titleElement, "Size", out titleFontSize)) axis.TitleFontSize = titleFontSize;
                if (GetFontWeightAttribute(titleElement, "Weight", FontWeightConverterInstance, out titleFontWeight)) axis.TitleFontWeight = titleFontWeight;
                if (GetDoubleAttribute(titleElement, "Distance", out axisTitleDistance)) axis.AxisTitleDistance = axisTitleDistance;
            }

            // Label Properties
            var labelElement = element.Element("Labels");
            if (labelElement != null)
            {
                if (GetColorAttribute(labelElement, nameof(axis.TextColor), out var textColor)) axis.TextColor = textColor;
                if (GetStringAttribute(labelElement, nameof(axis.Font), out var font)) axis.Font = font;
                if (GetDoubleAttribute(labelElement, nameof(axis.FontSize), out var fontSize)) axis.FontSize = fontSize;
                if (GetFontWeightAttribute(labelElement, nameof(axis.FontWeight), FontWeightConverterInstance, out var fontWeight)) axis.FontWeight = fontWeight;
                if (GetDoubleAttribute(labelElement, nameof(axis.Angle), out var angle)) axis.Angle = angle;
                if (GetDoubleAttribute(labelElement, nameof(axis.AxisTickToLabelDistance), out var axisTickToLabelDistance)) axis.AxisTickToLabelDistance = axisTickToLabelDistance;
                if (GetStringAttribute(labelElement, nameof(axis.StringFormat), out var stringFormat)) axis.StringFormat = stringFormat;
                if (GetBooleanAttribute(labelElement, nameof(axis.UseSuperExponentialFormat), out var useSuperExponentialFormat)) axis.UseSuperExponentialFormat = useSuperExponentialFormat;
                if (GetDoubleAttribute(labelElement, nameof(axis.IntervalLength), out var intervalLength)) axis.IntervalLength = intervalLength;

                // Backward compatibility
                if (GetColorAttribute(labelElement, "Color", out textColor)) axis.TextColor = textColor;
                if (GetDoubleAttribute(labelElement, "Size", out fontSize)) axis.FontSize = fontSize;
                if (GetFontWeightAttribute(labelElement, "Weight", FontWeightConverterInstance, out fontWeight)) axis.FontWeight = fontWeight;
                if (GetDoubleAttribute(labelElement, "TickDistance", out axisTickToLabelDistance)) axis.AxisTickToLabelDistance = axisTickToLabelDistance;
                if (GetBooleanAttribute(labelElement, "Superscript", out useSuperExponentialFormat)) axis.UseSuperExponentialFormat = useSuperExponentialFormat;
            }

            // Major Gridline Properties
            var majorGridlineElement = element.Element("MajorGridlines");
            if (majorGridlineElement != null)
            {
                if (GetColorAttribute(majorGridlineElement, nameof(axis.MajorGridlineColor), out var majorGridlineColor)) axis.MajorGridlineColor = majorGridlineColor;
                if (GetEnumAttribute(majorGridlineElement, nameof(axis.MajorGridlineStyle), out OxyPlot.LineStyle majorGridlineStyle)) axis.MajorGridlineStyle = majorGridlineStyle;
                if (GetDoubleAttribute(majorGridlineElement, nameof(axis.MajorGridlineThickness), out var majorGridlineThickness)) axis.MajorGridlineThickness = majorGridlineThickness;
                if (GetDoubleAttribute(majorGridlineElement, nameof(axis.MajorStep), out var majorStep)) axis.MajorStep = majorStep;
                if (GetDoubleAttribute(majorGridlineElement, nameof(axis.MajorTickSize), out var majorTickSize)) axis.MajorTickSize = majorTickSize;

                // Backward compatibility
                if (GetColorAttribute(majorGridlineElement, "Color", out majorGridlineColor)) axis.MajorGridlineColor = majorGridlineColor;
                if (GetEnumAttribute(majorGridlineElement, "Style", out majorGridlineStyle)) axis.MajorGridlineStyle = majorGridlineStyle;
                if (GetDoubleAttribute(majorGridlineElement, "Thickness", out majorGridlineThickness)) axis.MajorGridlineThickness = majorGridlineThickness;
                if (GetDoubleAttribute(majorGridlineElement, "Step", out majorStep)) axis.MajorStep = majorStep;
                if (GetDoubleAttribute(majorGridlineElement, "TickSize", out majorTickSize)) axis.MajorTickSize = majorTickSize;
            }

            // Minor Gridline Properties
            var minorGridlineElement = element.Element("MinorGridlines");
            if (minorGridlineElement != null)
            {
                if (GetColorAttribute(minorGridlineElement, nameof(axis.MinorGridlineColor), out var minorGridlineColor)) axis.MinorGridlineColor = minorGridlineColor;
                if (GetEnumAttribute(minorGridlineElement, nameof(axis.MinorGridlineStyle), out OxyPlot.LineStyle minorGridlineStyle)) axis.MinorGridlineStyle = minorGridlineStyle;
                if (GetDoubleAttribute(minorGridlineElement, nameof(axis.MinorGridlineThickness), out var minorGridlineThickness)) axis.MinorGridlineThickness = minorGridlineThickness;
                if (GetDoubleAttribute(minorGridlineElement, nameof(axis.MinorStep), out var minorStep)) axis.MinorStep = minorStep;
                if (GetDoubleAttribute(minorGridlineElement, nameof(axis.MinorTickSize), out var minorTickSize)) axis.MinorTickSize = minorTickSize;

                // Backward compatibility
                if (GetColorAttribute(minorGridlineElement, "Color", out minorGridlineColor)) axis.MinorGridlineColor = minorGridlineColor;
                if (GetEnumAttribute(minorGridlineElement, "Style", out minorGridlineStyle)) axis.MinorGridlineStyle = minorGridlineStyle;
                if (GetDoubleAttribute(minorGridlineElement, "Thickness", out minorGridlineThickness)) axis.MinorGridlineThickness = minorGridlineThickness;
                if (GetDoubleAttribute(minorGridlineElement, "Step", out minorStep)) axis.MinorStep = minorStep;
                if (GetDoubleAttribute(minorGridlineElement, "TickSize", out minorTickSize)) axis.MinorTickSize = minorTickSize;
            }

            // Extra Gridline Properties
            var extraGridlineElement = element.Element("ExtraGridlines");
            if (extraGridlineElement != null)
            {
                if (GetColorAttribute(extraGridlineElement, nameof(axis.ExtraGridlineColor), out var extraGridlineColor)) axis.ExtraGridlineColor = extraGridlineColor;
                if (GetEnumAttribute(extraGridlineElement, nameof(axis.ExtraGridlineStyle), out OxyPlot.LineStyle extraGridlineStyle)) axis.ExtraGridlineStyle = extraGridlineStyle;
                if (GetDoubleAttribute(extraGridlineElement, nameof(axis.ExtraGridlineThickness), out var extraGridlineThickness)) axis.ExtraGridlineThickness = extraGridlineThickness;
                // Read explicit gridline values (the double[] array). Empty array if absent.
                var extraGridlinesValuesElement = extraGridlineElement.Element(nameof(axis.ExtraGridlines));
                if (extraGridlinesValuesElement != null)
                {
                    axis.ExtraGridlines = extraGridlinesValuesElement.DoublesFromXElement();
                }
            }

            // Tick Style Properties
            var tickStyleElement = element.Element("Tick");
            if (tickStyleElement != null)
            {
                if (GetColorAttribute(tickStyleElement, nameof(axis.TicklineColor), out var ticklineColor)) axis.TicklineColor = ticklineColor;
                if (GetEnumAttribute(tickStyleElement, nameof(axis.TickStyle), out OxyPlot.Axes.TickStyle tickStyle)) axis.TickStyle = tickStyle;

                // Backward compatibility
                if (GetColorAttribute(tickStyleElement, "Color", out ticklineColor)) axis.TicklineColor = ticklineColor;
                if (GetEnumAttribute(tickStyleElement, "Style", out tickStyle)) axis.TickStyle = tickStyle;
            }

            // Concrete axis implementation properties
            var currentAxisType = axis.GetType();
            if (currentAxisType == typeof(LinearAxis))
            {
                var linearAxisElement = element.Element("LinearAxis");
                if (linearAxisElement != null)
                {
                    if (GetBooleanAttribute(linearAxisElement, nameof(LinearAxis.FormatAsFractions), out var formatAsFractions))
                        ((LinearAxis)axis).FormatAsFractions = formatAsFractions;
                    // Backward compatibility
                    if (GetBooleanAttribute(linearAxisElement, "FractionFormat", out formatAsFractions))
                        ((LinearAxis)axis).FormatAsFractions = formatAsFractions;
                }
            }
            else if (currentAxisType == typeof(CategoryAxis))
            {
                var categoryAxisElement = element.Element("CategoryAxis");
                if (categoryAxisElement != null)
                {
                    if (GetBooleanAttribute(categoryAxisElement, nameof(CategoryAxis.IsTickCentered), out var isTickCentered))
                        ((CategoryAxis)axis).IsTickCentered = isTickCentered;
                    if (GetDoubleAttribute(categoryAxisElement, nameof(CategoryAxis.GapWidth), out var gapWidth))
                        ((CategoryAxis)axis).GapWidth = gapWidth;

                    // Backward compatibility
                    if (GetBooleanAttribute(categoryAxisElement, "Centered", out isTickCentered))
                        ((CategoryAxis)axis).IsTickCentered = isTickCentered;
                }
            }
            else if (currentAxisType == typeof(LogarithmicAxis))
            {
                var logAxisElement = element.Element("LogarithmicAxis");
                if (logAxisElement != null)
                {
                    if (GetDoubleAttribute(logAxisElement, nameof(LogarithmicAxis.Base), out var logBase))
                        ((LogarithmicAxis)axis).Base = logBase;
                    if (GetBooleanAttribute(logAxisElement, nameof(LogarithmicAxis.PowerPadding), out var powerPadding))
                        ((LogarithmicAxis)axis).PowerPadding = powerPadding;

                    // Backward compatibility
                    if (GetDoubleAttribute(logAxisElement, "LogBase", out logBase))
                        ((LogarithmicAxis)axis).Base = logBase;
                }
            }
            else if (currentAxisType == typeof(DateTimeAxis))
            {
                var dateAxisElement = element.Element("DateTimeAxis");
                if (dateAxisElement != null)
                {
                    if (GetEnumAttribute(dateAxisElement, nameof(DateTimeAxis.CalendarWeekRule), out System.Globalization.CalendarWeekRule calendarWeekRule))
                        ((DateTimeAxis)axis).CalendarWeekRule = calendarWeekRule;

                    // Backward compatibility
                    if (GetEnumAttribute(dateAxisElement, "CalendarWeek", out calendarWeekRule))
                        ((DateTimeAxis)axis).CalendarWeekRule = calendarWeekRule;
                }
            }
            else if (currentAxisType == typeof(TimeSpanAxis))
            {
                // TimeSpanAxis has no specific properties beyond base Axis
            }
            else if (currentAxisType == typeof(AngleAxis))
            {
                var angleAxisElement = element.Element("AngleAxis");
                if (angleAxisElement != null)
                {
                    if (GetDoubleAttribute(angleAxisElement, nameof(AngleAxis.StartAngle), out var startAngle))
                        ((AngleAxis)axis).StartAngle = startAngle;
                    if (GetDoubleAttribute(angleAxisElement, nameof(AngleAxis.EndAngle), out var endAngle))
                        ((AngleAxis)axis).EndAngle = endAngle;
                }
            }
            else if (currentAxisType == typeof(MagnitudeAxis))
            {
                var magnitudeAxisElement = element.Element("MagnitudeAxis");
                if (magnitudeAxisElement != null)
                {
                    if (GetBooleanAttribute(magnitudeAxisElement, nameof(MagnitudeAxis.FormatAsFractions), out var formatAsFractions))
                        ((MagnitudeAxis)axis).FormatAsFractions = formatAsFractions;
                }
            }
            else if (currentAxisType == typeof(LinearColorAxis))
            {
                var linearColorAxisElement = element.Element("LinearColorAxis");
                if (linearColorAxisElement != null)
                {
                    if (GetColorAttribute(linearColorAxisElement, nameof(LinearColorAxis.HighColor), out var highColor))
                        ((LinearColorAxis)axis).HighColor = highColor;
                    if (GetColorAttribute(linearColorAxisElement, nameof(LinearColorAxis.LowColor), out var lowColor))
                        ((LinearColorAxis)axis).LowColor = lowColor;
                    if (GetIntegerAttribute(linearColorAxisElement, nameof(LinearColorAxis.PaletteSize), out var paletteSize))
                        ((LinearColorAxis)axis).PaletteSize = paletteSize;
                    if (GetColorAttribute(linearColorAxisElement, nameof(LinearColorAxis.InvalidNumberColor), out var invalidNumberColor))
                        ((LinearColorAxis)axis).InvalidNumberColor = invalidNumberColor;
                }
            }
            else if (currentAxisType == typeof(NormalProbabilityAxis))
            {
                // NormalProbabilityAxis has no specific properties beyond base Axis
            }
            else if (currentAxisType == typeof(GumbelProbabilityAxis))
            {
                // GumbelProbabilityAxis has no specific properties beyond base Axis
            }
            else if (currentAxisType == typeof(CategoryColorAxis))
            {
                var categoryColorAxisElement = element.Element("CategoryColorAxis");
                if (categoryColorAxisElement != null)
                {
                    if (GetBooleanAttribute(categoryColorAxisElement, nameof(CategoryColorAxis.IsTickCentered), out var isTickCentered))
                        ((CategoryColorAxis)axis).IsTickCentered = isTickCentered;
                    if (GetDoubleAttribute(categoryColorAxisElement, nameof(CategoryColorAxis.GapWidth), out var gapWidth))
                        ((CategoryColorAxis)axis).GapWidth = gapWidth;
                    if (GetColorAttribute(categoryColorAxisElement, nameof(CategoryColorAxis.InvalidCategoryColor), out var invalidCategoryColor))
                        ((CategoryColorAxis)axis).InvalidCategoryColor = invalidCategoryColor;
                }
            }
            else if (currentAxisType == typeof(LogarithmicColorAxis))
            {
                var logColorAxisElement = element.Element("LogarithmicColorAxis");
                if (logColorAxisElement != null)
                {
                    if (GetColorAttribute(logColorAxisElement, nameof(LogarithmicColorAxis.HighColor), out var highColor))
                        ((LogarithmicColorAxis)axis).HighColor = highColor;
                    if (GetColorAttribute(logColorAxisElement, nameof(LogarithmicColorAxis.LowColor), out var lowColor))
                        ((LogarithmicColorAxis)axis).LowColor = lowColor;
                    if (GetIntegerAttribute(logColorAxisElement, nameof(LogarithmicColorAxis.PaletteSize), out var paletteSize))
                        ((LogarithmicColorAxis)axis).PaletteSize = paletteSize;
                    if (GetColorAttribute(logColorAxisElement, nameof(LogarithmicColorAxis.InvalidNumberColor), out var invalidNumberColor))
                        ((LogarithmicColorAxis)axis).InvalidNumberColor = invalidNumberColor;
                    if (GetBooleanAttribute(logColorAxisElement, nameof(LogarithmicColorAxis.RenderAsImage), out var renderAsImage))
                        ((LogarithmicColorAxis)axis).RenderAsImage = renderAsImage;
                    if (GetDoubleAttribute(logColorAxisElement, nameof(LogarithmicColorAxis.Base), out var logBase))
                        ((LogarithmicColorAxis)axis).Base = logBase;
                    if (GetBooleanAttribute(logColorAxisElement, nameof(LogarithmicColorAxis.PowerPadding), out var powerPadding))
                        ((LogarithmicColorAxis)axis).PowerPadding = powerPadding;
                }
            }
            else if (currentAxisType == typeof(RangeColorAxis))
            {
                var rangeColorAxisElement = element.Element("RangeColorAxis");
                if (rangeColorAxisElement != null)
                {
                    if (GetColorAttribute(rangeColorAxisElement, nameof(RangeColorAxis.HighColor), out var highColor))
                        ((RangeColorAxis)axis).HighColor = highColor;
                    if (GetColorAttribute(rangeColorAxisElement, nameof(RangeColorAxis.LowColor), out var lowColor))
                        ((RangeColorAxis)axis).LowColor = lowColor;
                    if (GetColorAttribute(rangeColorAxisElement, nameof(RangeColorAxis.InvalidNumberColor), out var invalidNumberColor))
                        ((RangeColorAxis)axis).InvalidNumberColor = invalidNumberColor;
                    if (GetBooleanAttribute(rangeColorAxisElement, nameof(RangeColorAxis.FormatAsFractions), out var formatAsFractions))
                        ((RangeColorAxis)axis).FormatAsFractions = formatAsFractions;
                }
            }
            else if (currentAxisType == typeof(AngleAxisFullPlotArea))
            {
                var angleFullElement = element.Element("AngleAxisFullPlotArea");
                if (angleFullElement != null)
                {
                    if (GetDoubleAttribute(angleFullElement, nameof(AngleAxisFullPlotArea.StartAngle), out var startAngle))
                        ((AngleAxisFullPlotArea)axis).StartAngle = startAngle;
                    if (GetDoubleAttribute(angleFullElement, nameof(AngleAxisFullPlotArea.EndAngle), out var endAngle))
                        ((AngleAxisFullPlotArea)axis).EndAngle = endAngle;
                }
            }
            else if (currentAxisType == typeof(MagnitudeAxisFullPlotArea))
            {
                var magFullElement = element.Element("MagnitudeAxisFullPlotArea");
                if (magFullElement != null)
                {
                    if (GetBooleanAttribute(magFullElement, nameof(MagnitudeAxisFullPlotArea.FormatAsFractions), out var formatAsFractions))
                        ((MagnitudeAxisFullPlotArea)axis).FormatAsFractions = formatAsFractions;
                    if (GetDoubleAttribute(magFullElement, nameof(MagnitudeAxisFullPlotArea.MidshiftH), out var midshiftH))
                        ((MagnitudeAxisFullPlotArea)axis).MidshiftH = midshiftH;
                    if (GetDoubleAttribute(magFullElement, nameof(MagnitudeAxisFullPlotArea.MidshiftV), out var midshiftV))
                        ((MagnitudeAxisFullPlotArea)axis).MidshiftV = midshiftV;
                }
            }

            return axis;
        }
    }
}
