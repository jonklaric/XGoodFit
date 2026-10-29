// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SeriesSerializer.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides serialization and deserialization of OxyPlot series properties to and from XML.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#nullable enable annotations
#nullable disable warnings

namespace OxyPlot.Wpf.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Xml.Linq;
    using static PlotSerializer;

    /// <summary>
    /// Provides serialization and deserialization of OxyPlot <see cref="Series"/> properties to and from XML.
    /// Handles all series types including Line, Area, Bar, BoxPlot, Scatter, HeatMap, HighLow,
    /// CandleStick, Pie, Contour, Vector, Rectangle, TwoColorLine, ThreeColorLine, StairStep, and Stem series.
    /// </summary>
    public static class SeriesSerializer
    {
        /// <summary>
        /// Visual property names on <see cref="Series"/> that are serialized for settings persistence.
        /// Includes properties common to all series types plus type-specific properties.
        /// Consumers can use this list to monitor these properties for change tracking.
        /// </summary>
        public static readonly IReadOnlyList<string> SeriesVisualProperties = new[]
        {
            // General (Series base)
            "Name", "Title", "IsEnabled", "Visibility", "RenderInLegend",
            "Background", "Foreground", "Color", "FontFamily", "FontSize", "FontWeight",
            "Padding", "TrackerFormatString", "TrackerKey",
            // XYAxisSeries
            "XAxisKey", "YAxisKey",
            // DataPointSeries
            "CanTrackerInterpolatePoints", "DataFieldX", "DataFieldY",
            // LineSeries
            "LineJoin", "LineLegendPosition", "LineStyle",
            "MarkerFill", "MarkerResolution", "MarkerSize",
            "MarkerStroke", "MarkerStrokeThickness", "MarkerType",
            "MinimumSegmentLength", "StrokeThickness", "LabelFormatString", "LabelMargin",
            "BrokenLineColor", "BrokenLineStyle", "BrokenLineThickness",
            // ExtrapolationLineSeries
            "ExtrapolationColor", "ExtrapolationLineStyle", "IgnoreExtrapolationForScaling",
            // AreaSeries
            "Color2", "Fill", "DataFieldX2", "DataFieldY2", "Reverse2",
            // BarSeriesBase
            "BaseValue", "FillColor", "ColorField", "ValueField",
            "StrokeColor", "StackGroup", "NegativeFillColor",
            "LabelPlacement", "IsStacked",
            // BarSeries
            "BarWidth",
            // ColumnSeries
            "ColumnWidth",
            // HistogramSeries (FillColor, NegativeFillColor, StrokeColor, StrokeThickness, LabelFormatString, LabelPlacement shared)
            // BoxPlotSeries
            "Stroke", "IsVertical", "OutlierType",
            "WhiskerWidth", "ShowMedianAsDot", "MedianPointSize", "OutlierSize", "BoxWidth", "ShowBox",
            // ScatterPointSeries
            "DataFieldTag", "DataFieldValue", "DataFieldSize", "ColorAxisKey", "BinSize",
            // ScatterErrorSeries
            "ErrorBarStopWidth", "MinimumErrorSize", "ErrorBarStrokeThickness", "ErrorBarColor",
            "DataFieldLowerErrorX", "DataFieldUpperErrorX", "DataFieldLowerErrorY", "DataFieldUpperErrorY",
            // HeatMapSeries
            "Y0", "Y1", "X0", "X1", "HighColor", "CoordinateDefinition", "Interpolate", "LabelFontSize", "LowColor",
            // HighLowSeries
            "TickLength", "DataFieldHigh", "DataFieldLow", "DataFieldOpen", "DataFieldClose",
            // CandleStickSeries
            "IncreasingColor", "DecreasingColor", "CandleWidth",
            // PieSeries
            "Diameter", "InnerDiameter", "StartAngle", "AngleSpan", "AngleIncrement",
            "LegendFormat", "OutsideLabelFormat", "InsideLabelColor", "InsideLabelFormat", "InsideLabelPosition",
            "AreInsideLabelsAngled", "TickDistance", "TickRadialLength", "TickHorizontalLength", "TickLabelDistance",
            "ExplodedDistance", "LabelField", "IsExplodedField",
            // ContourSeries
            "ContourLevelStep", "LabelBackground", "LabelStep",
            // VectorSeries
            "ArrowHeadLength", "ArrowHeadWidth", "ArrowHeadPosition", "ArrowVeeness", "ArrowStartPosition",
            // RectangleSeries (CanTrackerInterpolatePoints, ColorAxisKey, LabelFormatString, LabelFontSize shared)
            // TwoColorLineSeries
            "Limit", "LineStyle2",
            // ThreeColorLineSeries
            "ColorLo", "ColorHi", "LimitLo", "LimitHi", "LineStyleLo", "LineStyleHi",
            // StairStepSeries
            "VerticalLineStyle", "VerticalStrokeThickness",
            // StemSeries
            "Base",
        };

        /// <summary>
        /// Serializes all series from a <see cref="Plot"/> to an <see cref="XElement"/>.
        /// </summary>
        /// <param name="plot">The plot containing series to serialize.</param>
        /// <returns>An <see cref="XElement"/> containing all serialized series.</returns>
        public static XElement SeriesToXElement(Plot plot)
        {
            var seriesProperties = new XElement(SeriesPropertiesTag);
            foreach (Series series in plot.Series)
            {
                seriesProperties.Add(SeriesToXElement(series));
            }
            return seriesProperties;
        }

        /// <summary>
        /// Deserializes series from an <see cref="XElement"/> and applies them to the plot.
        /// </summary>
        /// <param name="plot">The plot to add deserialized series to.</param>
        /// <param name="element">The <see cref="XElement"/> containing serialized series.</param>
        /// <remarks>
        /// This method clears the existing series collection before adding deserialized series.
        /// Only appearance properties are restored; data bindings are not included.
        /// </remarks>
        public static void XElementToSeries(Plot plot, XElement element)
        {
            if (element.Name != SeriesPropertiesTag) return;

            plot.Series.Clear();

            foreach (var el in element.Elements())
            {
                // SeriesPropertiesTag is the outer collection element (parent of these
                // children), not an item tag. Only accept SeriesItemTag for individual
                // series entries; the prior filter would have let a stray nested
                // <Series> element through into XElementToSeries where it would
                // silently fail to resolve a SeriesType and return null.
                if (el.Name != SeriesItemTag) continue;
                var tempSeries = XElementToSeries(el);
                if (tempSeries == null) continue;
                plot.Series.Add(tempSeries);
            }
        }

        /// <summary>
        /// Serializes a single <see cref="Series"/> to an <see cref="XElement"/>.
        /// </summary>
        /// <param name="series">The series to serialize.</param>
        /// <returns>An <see cref="XElement"/> containing the serialized series properties.</returns>
        public static XElement SeriesToXElement(Series series)
        {
            var seriesElement = new XElement(SeriesItemTag);
            var seriesType = series.GetType();
            seriesElement.SetAttributeValue("SeriesType", seriesType.ToString());

            // Serialize general series properties
            var generalProperties = new XElement("General");
            generalProperties.SetAttributeValue(nameof(series.Name), series.Name ?? "");
            generalProperties.SetAttributeValue(nameof(series.Title), series.Title);
            generalProperties.SetAttributeValue(nameof(series.IsEnabled), series.IsEnabled.ToString());
            generalProperties.SetAttributeValue(nameof(series.Visibility), series.Visibility.ToString());
            generalProperties.SetAttributeValue(nameof(series.RenderInLegend), series.RenderInLegend.ToString());

            var bc = new BrushConverter();
            generalProperties.SetAttributeValue(nameof(series.Background), bc.ConvertToInvariantString(series.Background));
            generalProperties.SetAttributeValue(nameof(series.Foreground), bc.ConvertToInvariantString(series.Foreground));

            var fwc = new FontWeightConverter();
            var tc = new ThicknessConverter();
            var ffc = new FontFamilyConverter();
            generalProperties.SetAttributeValue(nameof(series.Color), series.Color.ToString());
            if (series.FontFamily != null)
                generalProperties.SetAttributeValue(nameof(series.FontFamily), ffc.ConvertToInvariantString(series.FontFamily));
            generalProperties.SetAttributeValue(nameof(series.FontSize), series.FontSize.ToString("G17", CultureInfo.InvariantCulture));
            generalProperties.SetAttributeValue(nameof(series.FontWeight), fwc.ConvertToInvariantString(series.FontWeight));
            generalProperties.SetAttributeValue(nameof(series.Padding), tc.ConvertToInvariantString(series.Padding));
            generalProperties.SetAttributeValue(nameof(series.TrackerFormatString), series.TrackerFormatString);
            generalProperties.SetAttributeValue(nameof(series.TrackerKey), series.TrackerKey);
            // User-settable perf opt-ins. Previously not persisted: every Save/Open
            // would silently revert IsHitTestEnabled to true and EdgeRenderingMode to
            // Automatic, undoing the framework's documented dense-trace perf lever.
            generalProperties.SetAttributeValue(nameof(series.IsHitTestEnabled), series.IsHitTestEnabled.ToString());
            generalProperties.SetAttributeValue(nameof(series.EdgeRenderingMode), series.EdgeRenderingMode.ToString());

            seriesElement.Add(generalProperties);

            // Serialize XY axis series properties
            var xyAxisSeries = series as XYAxisSeries;
            if (xyAxisSeries != null)
            {
                var xyAxisSeriesElement = new XElement(nameof(XYAxisSeries));
                xyAxisSeriesElement.SetAttributeValue(nameof(xyAxisSeries.XAxisKey), xyAxisSeries.XAxisKey);
                xyAxisSeriesElement.SetAttributeValue(nameof(xyAxisSeries.YAxisKey), xyAxisSeries.YAxisKey);
                seriesElement.Add(xyAxisSeriesElement);
            }

            // Serialize data point series properties
            var dataPointSeries = series as DataPointSeries;
            if (dataPointSeries != null)
            {
                var dataPointSeriesElement = new XElement(nameof(DataPointSeries));
                dataPointSeriesElement.SetAttributeValue(nameof(dataPointSeries.CanTrackerInterpolatePoints), dataPointSeries.CanTrackerInterpolatePoints.ToString());
                dataPointSeriesElement.SetAttributeValue(nameof(dataPointSeries.DataFieldX), dataPointSeries.DataFieldX);
                dataPointSeriesElement.SetAttributeValue(nameof(dataPointSeries.DataFieldY), dataPointSeries.DataFieldY);
                seriesElement.Add(dataPointSeriesElement);
            }

            // Serialize bar base series properties
            var barBaseSeries = series as BarSeriesBase;
            if (barBaseSeries != null)
            {
                var barBaseElement = new XElement(nameof(BarSeriesBase));
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.BaseValue), barBaseSeries.BaseValue.ToString("G17", CultureInfo.InvariantCulture));
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.FillColor), barBaseSeries.FillColor);
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.ColorField), barBaseSeries.ColorField);
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.ValueField), barBaseSeries.ValueField);
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.LabelMargin), barBaseSeries.LabelMargin.ToString("G17", CultureInfo.InvariantCulture));
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.StrokeColor), barBaseSeries.StrokeColor);
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.StackGroup), barBaseSeries.StackGroup);
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.NegativeFillColor), barBaseSeries.NegativeFillColor);
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.LabelPlacement), barBaseSeries.LabelPlacement);
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.LabelFormatString), barBaseSeries.LabelFormatString);
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.StrokeThickness), barBaseSeries.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                barBaseElement.SetAttributeValue(nameof(barBaseSeries.IsStacked), barBaseSeries.IsStacked);
                seriesElement.Add(barBaseElement);
            }

            // Serialize bar series properties
            var barSeries = series as BarSeries;
            if (barSeries != null)
            {
                var barElement = new XElement(nameof(BarSeries));
                barElement.SetAttributeValue(nameof(barSeries.BarWidth), barSeries.BarWidth.ToString("G17", CultureInfo.InvariantCulture));
                seriesElement.Add(barElement);
            }

            // Serialize column series properties
            var columnSeries = series as ColumnSeries;
            if (columnSeries != null)
            {
                var columnElement = new XElement(nameof(ColumnSeries));
                columnElement.SetAttributeValue(nameof(columnSeries.ColumnWidth), columnSeries.ColumnWidth.ToString("G17", CultureInfo.InvariantCulture));
                seriesElement.Add(columnElement);
            }

            // Serialize histogram series properties
            var histogramSeries = series as HistogramSeries;
            if (histogramSeries != null)
            {
                var histogramElement = new XElement(nameof(HistogramSeries));
                histogramElement.SetAttributeValue(nameof(histogramSeries.FillColor), histogramSeries.FillColor);
                histogramElement.SetAttributeValue(nameof(histogramSeries.NegativeFillColor), histogramSeries.NegativeFillColor);
                histogramElement.SetAttributeValue(nameof(histogramSeries.LabelFormatString), histogramSeries.LabelFormatString);
                histogramElement.SetAttributeValue(nameof(histogramSeries.LabelPlacement), histogramSeries.LabelPlacement);
                histogramElement.SetAttributeValue(nameof(histogramSeries.StrokeColor), histogramSeries.StrokeColor);
                histogramElement.SetAttributeValue(nameof(histogramSeries.StrokeThickness), histogramSeries.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                seriesElement.Add(histogramElement);
            }

            // Serialize line series properties
            var lineSeries = series as LineSeries;
            if (lineSeries != null)
            {
                var lineSeriesElement = new XElement(nameof(LineSeries));
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.LineJoin), lineSeries.LineJoin);
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.LineLegendPosition), lineSeries.LineLegendPosition);
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.LineStyle), lineSeries.LineStyle);
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.MarkerFill), lineSeries.MarkerFill);
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.MarkerResolution), lineSeries.MarkerResolution.ToString(CultureInfo.InvariantCulture));
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.MarkerSize), lineSeries.MarkerSize.ToString("G17", CultureInfo.InvariantCulture));
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.MarkerStroke), lineSeries.MarkerStroke);
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.MarkerStrokeThickness), lineSeries.MarkerStrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.MarkerType), lineSeries.MarkerType);
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.MinimumSegmentLength), lineSeries.MinimumSegmentLength.ToString("G17", CultureInfo.InvariantCulture));
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.StrokeThickness), lineSeries.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.LabelFormatString), lineSeries.LabelFormatString);
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.LabelMargin), lineSeries.LabelMargin.ToString("G17", CultureInfo.InvariantCulture));
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.BrokenLineColor), lineSeries.BrokenLineColor);
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.BrokenLineStyle), lineSeries.BrokenLineStyle);
                lineSeriesElement.SetAttributeValue(nameof(lineSeries.BrokenLineThickness), lineSeries.BrokenLineThickness.ToString("G17", CultureInfo.InvariantCulture));
                seriesElement.Add(lineSeriesElement);
            }

            // Serialize area series properties
            var areaSeries = series as AreaSeries;
            if (areaSeries != null)
            {
                var areaSeriesElement = new XElement(nameof(AreaSeries));
                areaSeriesElement.SetAttributeValue(nameof(areaSeries.Color2), areaSeries.Color2);
                areaSeriesElement.SetAttributeValue(nameof(areaSeries.Fill), areaSeries.Fill);
                areaSeriesElement.SetAttributeValue(nameof(areaSeries.DataFieldX2), areaSeries.DataFieldX2);
                areaSeriesElement.SetAttributeValue(nameof(areaSeries.DataFieldY2), areaSeries.DataFieldY2);
                areaSeriesElement.SetAttributeValue(nameof(areaSeries.Reverse2), areaSeries.Reverse2);
                seriesElement.Add(areaSeriesElement);
            }

            // Serialize boxplot series properties
            var boxPlotSeries = series as BoxPlotSeries;
            if (boxPlotSeries != null)
            {
                var boxPlotElement = new XElement(nameof(BoxPlotSeries));
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.StrokeThickness), boxPlotSeries.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.Stroke), boxPlotSeries.Stroke);
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.LineStyle), boxPlotSeries.LineStyle);
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.IsVertical), boxPlotSeries.IsVertical);
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.Fill), boxPlotSeries.Fill);
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.OutlierType), boxPlotSeries.OutlierType);
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.WhiskerWidth), boxPlotSeries.WhiskerWidth.ToString("G17", CultureInfo.InvariantCulture));
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.ShowMedianAsDot), boxPlotSeries.ShowMedianAsDot);
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.MedianPointSize), boxPlotSeries.MedianPointSize.ToString("G17", CultureInfo.InvariantCulture));
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.OutlierSize), boxPlotSeries.OutlierSize.ToString("G17", CultureInfo.InvariantCulture));
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.BoxWidth), boxPlotSeries.BoxWidth.ToString("G17", CultureInfo.InvariantCulture));
                boxPlotElement.SetAttributeValue(nameof(boxPlotSeries.ShowBox), boxPlotSeries.ShowBox);
                seriesElement.Add(boxPlotElement);
            }

            // Serialize scatter point series properties
            var scatterPointSeries = series as ScatterPointSeries;
            if (scatterPointSeries != null)
            {
                var scatterPointElement = new XElement(nameof(ScatterPointSeries));
                scatterPointElement.SetAttributeValue(nameof(scatterPointSeries.DataFieldTag), scatterPointSeries.DataFieldTag);
                scatterPointElement.SetAttributeValue(nameof(scatterPointSeries.DataFieldValue), scatterPointSeries.DataFieldValue);
                scatterPointElement.SetAttributeValue(nameof(scatterPointSeries.ColorAxisKey), scatterPointSeries.ColorAxisKey);
                scatterPointElement.SetAttributeValue(nameof(scatterPointSeries.BinSize), scatterPointSeries.BinSize.ToString("G17", CultureInfo.InvariantCulture));
                scatterPointElement.SetAttributeValue(nameof(scatterPointSeries.MarkerFill), scatterPointSeries.MarkerFill);
                scatterPointElement.SetAttributeValue(nameof(scatterPointSeries.MarkerSize), scatterPointSeries.MarkerSize.ToString("G17", CultureInfo.InvariantCulture));
                scatterPointElement.SetAttributeValue(nameof(scatterPointSeries.MarkerStroke), scatterPointSeries.MarkerStroke);
                scatterPointElement.SetAttributeValue(nameof(scatterPointSeries.MarkerStrokeThickness), scatterPointSeries.MarkerStrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                scatterPointElement.SetAttributeValue(nameof(scatterPointSeries.MarkerType), scatterPointSeries.MarkerType);
                seriesElement.Add(scatterPointElement);
            }

            // Serialize error bar series properties
            var errorBarsSeries = series as ScatterErrorSeries;
            if (errorBarsSeries != null)
            {
                var element = new XElement(nameof(ScatterErrorSeries));
                element.SetAttributeValue(nameof(errorBarsSeries.DataFieldTag), errorBarsSeries.DataFieldTag);
                element.SetAttributeValue(nameof(errorBarsSeries.DataFieldValue), errorBarsSeries.DataFieldValue);
                element.SetAttributeValue(nameof(errorBarsSeries.ColorAxisKey), errorBarsSeries.ColorAxisKey);
                element.SetAttributeValue(nameof(errorBarsSeries.BinSize), errorBarsSeries.BinSize.ToString("G17", CultureInfo.InvariantCulture));
                element.SetAttributeValue(nameof(errorBarsSeries.MarkerFill), errorBarsSeries.MarkerFill);
                element.SetAttributeValue(nameof(errorBarsSeries.MarkerSize), errorBarsSeries.MarkerSize.ToString("G17", CultureInfo.InvariantCulture));
                element.SetAttributeValue(nameof(errorBarsSeries.MarkerStroke), errorBarsSeries.MarkerStroke);
                element.SetAttributeValue(nameof(errorBarsSeries.MarkerStrokeThickness), errorBarsSeries.MarkerStrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                element.SetAttributeValue(nameof(errorBarsSeries.MarkerType), errorBarsSeries.MarkerType);
                element.SetAttributeValue(nameof(errorBarsSeries.ErrorBarStopWidth), errorBarsSeries.ErrorBarStopWidth.ToString("G17", CultureInfo.InvariantCulture));
                element.SetAttributeValue(nameof(errorBarsSeries.MinimumErrorSize), errorBarsSeries.MinimumErrorSize.ToString("G17", CultureInfo.InvariantCulture));
                element.SetAttributeValue(nameof(errorBarsSeries.ErrorBarStrokeThickness), errorBarsSeries.ErrorBarStrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                element.SetAttributeValue(nameof(errorBarsSeries.ErrorBarColor), errorBarsSeries.ErrorBarColor);
                element.SetAttributeValue(nameof(errorBarsSeries.DataFieldLowerErrorX), errorBarsSeries.DataFieldLowerErrorX);
                element.SetAttributeValue(nameof(errorBarsSeries.DataFieldUpperErrorX), errorBarsSeries.DataFieldUpperErrorX);
                element.SetAttributeValue(nameof(errorBarsSeries.DataFieldLowerErrorY), errorBarsSeries.DataFieldLowerErrorY);
                element.SetAttributeValue(nameof(errorBarsSeries.DataFieldUpperErrorY), errorBarsSeries.DataFieldUpperErrorY);
                seriesElement.Add(element);
            }

            // Serialize heat map series properties
            var heatMapSeries = series as HeatMapSeries;
            if (heatMapSeries != null)
            {
                var heatMapElement = new XElement(nameof(HeatMapSeries));
                heatMapElement.SetAttributeValue(nameof(heatMapSeries.ColorAxisKey), heatMapSeries.ColorAxisKey);
                heatMapElement.SetAttributeValue(nameof(heatMapSeries.Y0), heatMapSeries.Y0.ToString("G17", CultureInfo.InvariantCulture));
                heatMapElement.SetAttributeValue(nameof(heatMapSeries.Y1), heatMapSeries.Y1.ToString("G17", CultureInfo.InvariantCulture));
                heatMapElement.SetAttributeValue(nameof(heatMapSeries.X0), heatMapSeries.X0.ToString("G17", CultureInfo.InvariantCulture));
                heatMapElement.SetAttributeValue(nameof(heatMapSeries.X1), heatMapSeries.X1.ToString("G17", CultureInfo.InvariantCulture));
                heatMapElement.SetAttributeValue(nameof(heatMapSeries.HighColor), heatMapSeries.HighColor);
                heatMapElement.SetAttributeValue(nameof(heatMapSeries.CoordinateDefinition), heatMapSeries.CoordinateDefinition);
                heatMapElement.SetAttributeValue(nameof(heatMapSeries.Interpolate), heatMapSeries.Interpolate);
                heatMapElement.SetAttributeValue(nameof(heatMapSeries.LabelFontSize), heatMapSeries.LabelFontSize.ToString("G17", CultureInfo.InvariantCulture));
                heatMapElement.SetAttributeValue(nameof(heatMapSeries.LowColor), heatMapSeries.LowColor);
                seriesElement.Add(heatMapElement);
            }

            // Serialize high-low series properties
            var highLowSeries = series as HighLowSeries;
            if (highLowSeries != null)
            {
                var highLowElement = new XElement(nameof(HighLowSeries));
                highLowElement.SetAttributeValue(nameof(highLowSeries.Color), highLowSeries.Color);
                highLowElement.SetAttributeValue(nameof(highLowSeries.StrokeThickness), highLowSeries.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                highLowElement.SetAttributeValue(nameof(highLowSeries.TickLength), highLowSeries.TickLength.ToString("G17", CultureInfo.InvariantCulture));
                highLowElement.SetAttributeValue(nameof(highLowSeries.LineStyle), highLowSeries.LineStyle);
                highLowElement.SetAttributeValue(nameof(highLowSeries.DataFieldX), highLowSeries.DataFieldX);
                highLowElement.SetAttributeValue(nameof(highLowSeries.DataFieldHigh), highLowSeries.DataFieldHigh);
                highLowElement.SetAttributeValue(nameof(highLowSeries.DataFieldLow), highLowSeries.DataFieldLow);
                highLowElement.SetAttributeValue(nameof(highLowSeries.DataFieldOpen), highLowSeries.DataFieldOpen);
                highLowElement.SetAttributeValue(nameof(highLowSeries.DataFieldClose), highLowSeries.DataFieldClose);
                seriesElement.Add(highLowElement);
            }

            // Serialize candlestick series properties
            var candleStickSeries = series as CandleStickSeries;
            if (candleStickSeries != null)
            {
                var candleElement = new XElement(nameof(CandleStickSeries));
                candleElement.SetAttributeValue(nameof(candleStickSeries.IncreasingColor), candleStickSeries.IncreasingColor);
                candleElement.SetAttributeValue(nameof(candleStickSeries.DecreasingColor), candleStickSeries.DecreasingColor);
                candleElement.SetAttributeValue(nameof(candleStickSeries.CandleWidth), candleStickSeries.CandleWidth.ToString("G17", CultureInfo.InvariantCulture));
                seriesElement.Add(candleElement);
            }

            // Serialize pie series properties
            var pieSeries = series as PieSeries;
            if (pieSeries != null)
            {
                var pieElement = new XElement(nameof(PieSeries));
                pieElement.SetAttributeValue(nameof(pieSeries.Stroke), pieSeries.Stroke);
                pieElement.SetAttributeValue(nameof(pieSeries.StrokeThickness), pieSeries.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.Diameter), pieSeries.Diameter.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.InnerDiameter), pieSeries.InnerDiameter.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.StartAngle), pieSeries.StartAngle.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.AngleSpan), pieSeries.AngleSpan.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.AngleIncrement), pieSeries.AngleIncrement.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.LegendFormat), pieSeries.LegendFormat);
                pieElement.SetAttributeValue(nameof(pieSeries.OutsideLabelFormat), pieSeries.OutsideLabelFormat);
                pieElement.SetAttributeValue(nameof(pieSeries.InsideLabelColor), pieSeries.InsideLabelColor);
                pieElement.SetAttributeValue(nameof(pieSeries.InsideLabelFormat), pieSeries.InsideLabelFormat);
                pieElement.SetAttributeValue(nameof(pieSeries.InsideLabelPosition), pieSeries.InsideLabelPosition.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.AreInsideLabelsAngled), pieSeries.AreInsideLabelsAngled);
                pieElement.SetAttributeValue(nameof(pieSeries.TickDistance), pieSeries.TickDistance.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.TickRadialLength), pieSeries.TickRadialLength.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.TickHorizontalLength), pieSeries.TickHorizontalLength.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.TickLabelDistance), pieSeries.TickLabelDistance.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.ExplodedDistance), pieSeries.ExplodedDistance.ToString("G17", CultureInfo.InvariantCulture));
                pieElement.SetAttributeValue(nameof(pieSeries.LabelField), pieSeries.LabelField);
                pieElement.SetAttributeValue(nameof(pieSeries.ValueField), pieSeries.ValueField);
                pieElement.SetAttributeValue(nameof(pieSeries.ColorField), pieSeries.ColorField);
                pieElement.SetAttributeValue(nameof(pieSeries.IsExplodedField), pieSeries.IsExplodedField);
                seriesElement.Add(pieElement);
            }

            // Serialize contour series properties
            var contourSeries = series as ContourSeries;
            if (contourSeries != null)
            {
                var contourElement = new XElement(nameof(ContourSeries));
                contourElement.SetAttributeValue(nameof(contourSeries.Color), contourSeries.Color);
                contourElement.SetAttributeValue(nameof(contourSeries.StrokeThickness), contourSeries.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                contourElement.SetAttributeValue(nameof(contourSeries.LineStyle), contourSeries.LineStyle);
                contourElement.SetAttributeValue(nameof(contourSeries.ContourLevelStep), contourSeries.ContourLevelStep.ToString("G17", CultureInfo.InvariantCulture));
                contourElement.SetAttributeValue(nameof(contourSeries.LabelBackground), contourSeries.LabelBackground);
                contourElement.SetAttributeValue(nameof(contourSeries.LabelStep), contourSeries.LabelStep);
                seriesElement.Add(contourElement);
            }

            // Serialize vector series properties
            var vectorSeries = series as VectorSeries;
            if (vectorSeries != null)
            {
                var vectorElement = new XElement(nameof(VectorSeries));
                vectorElement.SetAttributeValue(nameof(vectorSeries.Color), vectorSeries.Color);
                vectorElement.SetAttributeValue(nameof(vectorSeries.StrokeThickness), vectorSeries.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                vectorElement.SetAttributeValue(nameof(vectorSeries.ArrowHeadLength), vectorSeries.ArrowHeadLength.ToString("G17", CultureInfo.InvariantCulture));
                vectorElement.SetAttributeValue(nameof(vectorSeries.ArrowHeadWidth), vectorSeries.ArrowHeadWidth.ToString("G17", CultureInfo.InvariantCulture));
                vectorElement.SetAttributeValue(nameof(vectorSeries.ArrowHeadPosition), vectorSeries.ArrowHeadPosition.ToString("G17", CultureInfo.InvariantCulture));
                vectorElement.SetAttributeValue(nameof(vectorSeries.ArrowVeeness), vectorSeries.ArrowVeeness.ToString("G17", CultureInfo.InvariantCulture));
                vectorElement.SetAttributeValue(nameof(vectorSeries.ArrowStartPosition), vectorSeries.ArrowStartPosition.ToString("G17", CultureInfo.InvariantCulture));
                vectorElement.SetAttributeValue(nameof(vectorSeries.LineStyle), vectorSeries.LineStyle);
                vectorElement.SetAttributeValue(nameof(vectorSeries.ColorAxisKey), vectorSeries.ColorAxisKey);
                vectorElement.SetAttributeValue(nameof(vectorSeries.LabelFormatString), vectorSeries.LabelFormatString);
                vectorElement.SetAttributeValue(nameof(vectorSeries.LabelFontSize), vectorSeries.LabelFontSize.ToString("G17", CultureInfo.InvariantCulture));
                seriesElement.Add(vectorElement);
            }

            // Serialize rectangle series properties
            var rectangleSeries = series as RectangleSeries;
            if (rectangleSeries != null)
            {
                var rectangleElement = new XElement(nameof(RectangleSeries));
                rectangleElement.SetAttributeValue(nameof(rectangleSeries.CanTrackerInterpolatePoints), rectangleSeries.CanTrackerInterpolatePoints);
                rectangleElement.SetAttributeValue(nameof(rectangleSeries.ColorAxisKey), rectangleSeries.ColorAxisKey);
                rectangleElement.SetAttributeValue(nameof(rectangleSeries.LabelFormatString), rectangleSeries.LabelFormatString);
                rectangleElement.SetAttributeValue(nameof(rectangleSeries.LabelFontSize), rectangleSeries.LabelFontSize.ToString("G17", CultureInfo.InvariantCulture));
                seriesElement.Add(rectangleElement);
            }

            // Serialize two-color line series properties
            var twoColorLineSeries = series as TwoColorLineSeries;
            if (twoColorLineSeries != null)
            {
                var twoColorElement = new XElement(nameof(TwoColorLineSeries));
                twoColorElement.SetAttributeValue(nameof(twoColorLineSeries.Color2), twoColorLineSeries.Color2);
                twoColorElement.SetAttributeValue(nameof(twoColorLineSeries.Limit), twoColorLineSeries.Limit.ToString("G17", CultureInfo.InvariantCulture));
                twoColorElement.SetAttributeValue(nameof(twoColorLineSeries.LineStyle2), twoColorLineSeries.LineStyle2);
                seriesElement.Add(twoColorElement);
            }

            // Serialize three-color line series properties
            var threeColorLineSeries = series as ThreeColorLineSeries;
            if (threeColorLineSeries != null)
            {
                var threeColorElement = new XElement(nameof(ThreeColorLineSeries));
                threeColorElement.SetAttributeValue(nameof(threeColorLineSeries.ColorLo), threeColorLineSeries.ColorLo);
                threeColorElement.SetAttributeValue(nameof(threeColorLineSeries.ColorHi), threeColorLineSeries.ColorHi);
                threeColorElement.SetAttributeValue(nameof(threeColorLineSeries.LimitLo), threeColorLineSeries.LimitLo.ToString("G17", CultureInfo.InvariantCulture));
                threeColorElement.SetAttributeValue(nameof(threeColorLineSeries.LimitHi), threeColorLineSeries.LimitHi.ToString("G17", CultureInfo.InvariantCulture));
                threeColorElement.SetAttributeValue(nameof(threeColorLineSeries.LineStyleLo), threeColorLineSeries.LineStyleLo);
                threeColorElement.SetAttributeValue(nameof(threeColorLineSeries.LineStyleHi), threeColorLineSeries.LineStyleHi);
                seriesElement.Add(threeColorElement);
            }

            // Serialize stair-step series properties
            var stairStepSeries = series as StairStepSeries;
            if (stairStepSeries != null)
            {
                var stairStepElement = new XElement(nameof(StairStepSeries));
                stairStepElement.SetAttributeValue(nameof(stairStepSeries.VerticalLineStyle), stairStepSeries.VerticalLineStyle);
                stairStepElement.SetAttributeValue(nameof(stairStepSeries.VerticalStrokeThickness), stairStepSeries.VerticalStrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                seriesElement.Add(stairStepElement);
            }

            // Serialize extrapolation line series properties
            var extrapolationLineSeries = series as ExtrapolationLineSeries;
            if (extrapolationLineSeries != null)
            {
                var extrapolationElement = new XElement(nameof(ExtrapolationLineSeries));
                extrapolationElement.SetAttributeValue(nameof(extrapolationLineSeries.ExtrapolationColor), extrapolationLineSeries.ExtrapolationColor.ToString());
                extrapolationElement.SetAttributeValue(nameof(extrapolationLineSeries.ExtrapolationLineStyle), extrapolationLineSeries.ExtrapolationLineStyle.ToString());
                extrapolationElement.SetAttributeValue(nameof(extrapolationLineSeries.IgnoreExtrapolationForScaling), extrapolationLineSeries.IgnoreExtrapolationForScaling.ToString());
                seriesElement.Add(extrapolationElement);
            }

            // Serialize scatter series properties
            var scatterSeries = series as ScatterSeries;
            if (scatterSeries != null)
            {
                var scatterElement = new XElement(nameof(ScatterSeries));
                scatterElement.SetAttributeValue(nameof(scatterSeries.DataFieldX), scatterSeries.DataFieldX);
                scatterElement.SetAttributeValue(nameof(scatterSeries.DataFieldY), scatterSeries.DataFieldY);
                scatterElement.SetAttributeValue(nameof(scatterSeries.DataFieldTag), scatterSeries.DataFieldTag);
                scatterElement.SetAttributeValue(nameof(scatterSeries.DataFieldValue), scatterSeries.DataFieldValue);
                scatterElement.SetAttributeValue(nameof(scatterSeries.DataFieldSize), scatterSeries.DataFieldSize);
                scatterElement.SetAttributeValue(nameof(scatterSeries.ColorAxisKey), scatterSeries.ColorAxisKey);
                scatterElement.SetAttributeValue(nameof(scatterSeries.BinSize), scatterSeries.BinSize.ToString(CultureInfo.InvariantCulture));
                scatterElement.SetAttributeValue(nameof(scatterSeries.MarkerFill), scatterSeries.MarkerFill);
                scatterElement.SetAttributeValue(nameof(scatterSeries.MarkerSize), scatterSeries.MarkerSize.ToString("G17", CultureInfo.InvariantCulture));
                scatterElement.SetAttributeValue(nameof(scatterSeries.MarkerStroke), scatterSeries.MarkerStroke);
                scatterElement.SetAttributeValue(nameof(scatterSeries.MarkerStrokeThickness), scatterSeries.MarkerStrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                scatterElement.SetAttributeValue(nameof(scatterSeries.MarkerType), scatterSeries.MarkerType);
                seriesElement.Add(scatterElement);
            }

            // Serialize stem series properties
            var stemSeries = series as StemSeries;
            if (stemSeries != null)
            {
                var stemElement = new XElement(nameof(StemSeries));
                stemElement.SetAttributeValue(nameof(stemSeries.Base), stemSeries.Base.ToString("G17", CultureInfo.InvariantCulture));
                seriesElement.Add(stemElement);
            }

            return seriesElement;
        }

        /// <summary>
        /// Deserializes a single <see cref="Series"/> from an <see cref="XElement"/>.
        /// </summary>
        /// <param name="element">The <see cref="XElement"/> containing the serialized series properties.</param>
        /// <returns>A new <see cref="Series"/> with deserialized properties, or null if the element is invalid or the series type is not recognized.</returns>
        public static Series? XElementToSeries(XElement element)
        {
            if (element == null) return null;
            if (element.Name != SeriesItemTag && element.Name != SeriesPropertiesTag) return null;

            var fontWeightConverter = new FontWeightConverter();
            var thicknessConverter = new ThicknessConverter();
            var fontFamilyConverter = new FontFamilyConverter();

            Series? series = null;

            GetStringAttribute(element, "SeriesType", out string? seriesType);

            if (seriesType == typeof(HeatMapSeries).ToString())
                series = new HeatMapSeries();
            else if (seriesType == typeof(LineSeries).ToString())
                series = new LineSeries();
            else if (seriesType == typeof(ColumnSeries).ToString())
                series = new ColumnSeries();
            else if (seriesType == typeof(BarSeries).ToString())
                series = new BarSeries();
            else if (seriesType == typeof(HistogramSeries).ToString())
                series = new HistogramSeries();
            else if (seriesType == typeof(ScatterPointSeries).ToString())
                series = new ScatterPointSeries();
            else if (seriesType == typeof(ScatterErrorSeries).ToString())
                series = new ScatterErrorSeries();
            else if (seriesType == typeof(AreaSeries).ToString())
                series = new AreaSeries();
            else if (seriesType == typeof(BoxPlotSeries).ToString())
                series = new BoxPlotSeries();
            else if (seriesType == typeof(HighLowSeries).ToString())
                series = new HighLowSeries();
            else if (seriesType == typeof(CandleStickSeries).ToString())
                series = new CandleStickSeries();
            else if (seriesType == typeof(PieSeries).ToString())
                series = new PieSeries();
            else if (seriesType == typeof(ContourSeries).ToString())
                series = new ContourSeries();
            else if (seriesType == typeof(VectorSeries).ToString())
                series = new VectorSeries();
            else if (seriesType == typeof(RectangleSeries).ToString())
                series = new RectangleSeries();
            else if (seriesType == typeof(TwoColorLineSeries).ToString())
                series = new TwoColorLineSeries();
            else if (seriesType == typeof(ThreeColorLineSeries).ToString())
                series = new ThreeColorLineSeries();
            else if (seriesType == typeof(StairStepSeries).ToString())
                series = new StairStepSeries();
            else if (seriesType == typeof(StemSeries).ToString())
                series = new StemSeries();
            else if (seriesType == typeof(ExtrapolationLineSeries).ToString())
                series = new ExtrapolationLineSeries();
            else if (seriesType == typeof(ScatterSeries).ToString())
                series = new ScatterSeries();
            else
                return null;

            var bConverter = new BrushConverter();

            // Deserialize General Series Properties
            var generalElement = element.Element("General");
            if (generalElement != null)
            {
                if (GetStringAttribute(generalElement, nameof(series.Name), out var name)) series.Name = name;
                if (GetStringAttribute(generalElement, nameof(series.Title), out var title)) series.Title = title;
                if (GetBooleanAttribute(generalElement, nameof(series.IsEnabled), out var isEnabled)) series.IsEnabled = isEnabled;
                if (generalElement.Attribute(nameof(series.Visibility)) != null)
                {
                    var visibilityString = generalElement.Attribute(nameof(series.Visibility))!.Value;
                    if (visibilityString == "Visible") series.Visibility = Visibility.Visible;
                    if (visibilityString == "Hidden") series.Visibility = Visibility.Hidden;
                    if (visibilityString == "Collapsed") series.Visibility = Visibility.Collapsed;
                }

                if (GetBooleanAttribute(generalElement, nameof(series.RenderInLegend), out var renderInLegend)) series.RenderInLegend = renderInLegend;
                if (GetBrushAttribute(generalElement, nameof(series.Background), bConverter, out var background)) series.Background = background;
                if (GetBrushAttribute(generalElement, nameof(series.Foreground), bConverter, out var foreground)) series.Foreground = foreground;
                if (GetColorAttribute(generalElement, nameof(series.Color), out var color)) series.Color = color;
                if (GetFontFamilyAttribute(generalElement, nameof(series.FontFamily), fontFamilyConverter, out var fontFamily)) series.FontFamily = fontFamily;
                if (GetDoubleAttribute(generalElement, nameof(series.FontSize), out var fontSize)) series.FontSize = fontSize;
                if (GetFontWeightAttribute(generalElement, nameof(series.FontWeight), fontWeightConverter, out var fontWeight)) series.FontWeight = fontWeight;
                if (GetThicknessAttribute(generalElement, nameof(series.Padding), thicknessConverter, out var padding)) series.Padding = padding;
                if (GetStringAttribute(generalElement, nameof(series.TrackerFormatString), out var trackerFormatString)) series.TrackerFormatString = trackerFormatString;
                if (GetStringAttribute(generalElement, nameof(series.TrackerKey), out var trackerKey)) series.TrackerKey = trackerKey;
                if (GetBooleanAttribute(generalElement, nameof(series.IsHitTestEnabled), out var isHitTestEnabled)) series.IsHitTestEnabled = isHitTestEnabled;
                if (GetEnumAttribute(generalElement, nameof(series.EdgeRenderingMode), out OxyPlot.EdgeRenderingMode edgeRenderingMode)) series.EdgeRenderingMode = edgeRenderingMode;
            }

            // Deserialize XY Axis Series Properties
            var xyAxisSeries = series as XYAxisSeries;
            if (xyAxisSeries != null)
            {
                var xyAxesSeriesElement = element.Element(nameof(XYAxisSeries));
                if (xyAxesSeriesElement != null)
                {
                    if (GetStringAttribute(xyAxesSeriesElement, nameof(xyAxisSeries.XAxisKey), out var xAxisKey)) xyAxisSeries.XAxisKey = xAxisKey;
                    if (GetStringAttribute(xyAxesSeriesElement, nameof(xyAxisSeries.YAxisKey), out var yAxisKey)) xyAxisSeries.YAxisKey = yAxisKey;
                }
            }

            // Deserialize Data Series Properties
            var dataPointSeries = series as DataPointSeries;
            if (dataPointSeries != null)
            {
                var dataPointSeriesElement = element.Element(nameof(DataPointSeries));
                if (dataPointSeriesElement != null)
                {
                    if (GetBooleanAttribute(dataPointSeriesElement, nameof(dataPointSeries.CanTrackerInterpolatePoints), out var canTrackerInterpolatePoints)) dataPointSeries.CanTrackerInterpolatePoints = canTrackerInterpolatePoints;
                    if (GetStringAttribute(dataPointSeriesElement, nameof(dataPointSeries.DataFieldX), out var dataFieldX)) dataPointSeries.DataFieldX = dataFieldX;
                    if (GetStringAttribute(dataPointSeriesElement, nameof(dataPointSeries.DataFieldY), out var dataFieldY)) dataPointSeries.DataFieldY = dataFieldY;
                }
            }

            // Deserialize Bar series Base properties
            var barBaseSeries = series as BarSeriesBase;
            if (barBaseSeries != null)
            {
                var barBaseSeriesElement = element.Element(nameof(BarSeriesBase));
                if (barBaseSeriesElement != null)
                {
                    if (GetDoubleAttribute(barBaseSeriesElement, nameof(barBaseSeries.BaseValue), out var baseValue)) barBaseSeries.BaseValue = baseValue;
                    if (GetColorAttribute(barBaseSeriesElement, nameof(barBaseSeries.FillColor), out var fillColor)) barBaseSeries.FillColor = fillColor;
                    if (GetStringAttribute(barBaseSeriesElement, nameof(barBaseSeries.ColorField), out var colorField)) barBaseSeries.ColorField = colorField;
                    if (GetStringAttribute(barBaseSeriesElement, nameof(barBaseSeries.ValueField), out var valueField)) barBaseSeries.ValueField = valueField;
                    if (GetDoubleAttribute(barBaseSeriesElement, nameof(barBaseSeries.LabelMargin), out var labelMargin)) barBaseSeries.LabelMargin = labelMargin;
                    if (GetColorAttribute(barBaseSeriesElement, nameof(barBaseSeries.StrokeColor), out var strokeColor)) barBaseSeries.StrokeColor = strokeColor;
                    if (GetStringAttribute(barBaseSeriesElement, nameof(barBaseSeries.StackGroup), out var stackGroup)) barBaseSeries.StackGroup = stackGroup;
                    if (GetColorAttribute(barBaseSeriesElement, nameof(barBaseSeries.NegativeFillColor), out var negativeFillColor)) barBaseSeries.NegativeFillColor = negativeFillColor;
                    if (GetEnumAttribute(barBaseSeriesElement, nameof(barBaseSeries.LabelPlacement), out OxyPlot.Series.LabelPlacement labelPlacement))
                        barBaseSeries.LabelPlacement = labelPlacement;
                    if (GetStringAttribute(barBaseSeriesElement, nameof(barBaseSeries.LabelFormatString), out var barLabelFormatString)) barBaseSeries.LabelFormatString = barLabelFormatString;
                    if (GetDoubleAttribute(barBaseSeriesElement, nameof(barBaseSeries.StrokeThickness), out var strokeThickness)) barBaseSeries.StrokeThickness = strokeThickness;
                    if (GetBooleanAttribute(barBaseSeriesElement, nameof(barBaseSeries.IsStacked), out var isStacked)) barBaseSeries.IsStacked = isStacked;
                }
            }

            // Deserialize bar series properties
            var barSeries = series as BarSeries;
            if (barSeries != null)
            {
                var barSeriesElement = element.Element(nameof(BarSeries));
                if (barSeriesElement != null)
                {
                    if (GetDoubleAttribute(barSeriesElement, nameof(barSeries.BarWidth), out var barWidth)) barSeries.BarWidth = barWidth;
                }
            }

            // Deserialize column properties
            var columnSeries = series as ColumnSeries;
            if (columnSeries != null)
            {
                var columnSeriesElement = element.Element(nameof(ColumnSeries));
                if (columnSeriesElement != null)
                {
                    if (GetDoubleAttribute(columnSeriesElement, nameof(columnSeries.ColumnWidth), out var columnWidth)) columnSeries.ColumnWidth = columnWidth;
                }
            }

            // Deserialize histogram series properties
            var histogramSeries = series as HistogramSeries;
            if (histogramSeries != null)
            {
                var histogramSeriesElement = element.Element(nameof(HistogramSeries));
                if (histogramSeriesElement != null)
                {
                    if (GetColorAttribute(histogramSeriesElement, nameof(histogramSeries.FillColor), out var histFillColor)) histogramSeries.FillColor = histFillColor;
                    if (GetColorAttribute(histogramSeriesElement, nameof(histogramSeries.NegativeFillColor), out var histNegativeFillColor)) histogramSeries.NegativeFillColor = histNegativeFillColor;
                    if (GetStringAttribute(histogramSeriesElement, nameof(histogramSeries.LabelFormatString), out var histLabelFormatString)) histogramSeries.LabelFormatString = histLabelFormatString;
                    if (GetEnumAttribute(histogramSeriesElement, nameof(histogramSeries.LabelPlacement), out OxyPlot.Series.LabelPlacement histLabelPlacement))
                        histogramSeries.LabelPlacement = histLabelPlacement;
                    if (GetColorAttribute(histogramSeriesElement, nameof(histogramSeries.StrokeColor), out var histStrokeColor)) histogramSeries.StrokeColor = histStrokeColor;
                    if (GetDoubleAttribute(histogramSeriesElement, nameof(histogramSeries.StrokeThickness), out var histStrokeThickness)) histogramSeries.StrokeThickness = histStrokeThickness;
                }
            }

            // Deserialize line series properties
            var lineSeries = series as LineSeries;
            if (lineSeries != null)
            {
                var lineSeriesElement = element.Element(nameof(LineSeries));
                if (lineSeriesElement != null)
                {
                    if (GetEnumAttribute(lineSeriesElement, nameof(lineSeries.LineJoin), out OxyPlot.LineJoin lineJoin))
                        lineSeries.LineJoin = lineJoin;
                    if (GetEnumAttribute(lineSeriesElement, nameof(lineSeries.LineLegendPosition), out OxyPlot.Series.LineLegendPosition lineLegendPosition))
                        lineSeries.LineLegendPosition = lineLegendPosition;
                    if (GetEnumAttribute(lineSeriesElement, nameof(lineSeries.LineStyle), out OxyPlot.LineStyle lineStyle))
                        lineSeries.LineStyle = lineStyle;
                    if (GetColorAttribute(lineSeriesElement, nameof(lineSeries.MarkerFill), out var lineMarkerFill)) lineSeries.MarkerFill = lineMarkerFill;
                    if (GetIntegerAttribute(lineSeriesElement, nameof(lineSeries.MarkerResolution), out var markerResolution)) lineSeries.MarkerResolution = markerResolution;
                    if (GetDoubleAttribute(lineSeriesElement, nameof(lineSeries.MarkerSize), out var lineMarkerSize)) lineSeries.MarkerSize = lineMarkerSize;
                    if (GetColorAttribute(lineSeriesElement, nameof(lineSeries.MarkerStroke), out var lineMarkerStroke)) lineSeries.MarkerStroke = lineMarkerStroke;
                    if (GetDoubleAttribute(lineSeriesElement, nameof(lineSeries.MarkerStrokeThickness), out var lineMarkerStrokeThickness)) lineSeries.MarkerStrokeThickness = lineMarkerStrokeThickness;
                    if (GetEnumAttribute(lineSeriesElement, nameof(lineSeries.MarkerType), out OxyPlot.MarkerType lineMarkerType))
                        lineSeries.MarkerType = lineMarkerType;
                    if (GetDoubleAttribute(lineSeriesElement, nameof(lineSeries.MinimumSegmentLength), out var minimumSegmentLength)) lineSeries.MinimumSegmentLength = minimumSegmentLength;
                    if (GetDoubleAttribute(lineSeriesElement, nameof(lineSeries.StrokeThickness), out var lineStrokeThickness)) lineSeries.StrokeThickness = lineStrokeThickness;
                    if (GetStringAttribute(lineSeriesElement, nameof(lineSeries.LabelFormatString), out var lineLabelFormatString)) lineSeries.LabelFormatString = lineLabelFormatString;
                    if (GetDoubleAttribute(lineSeriesElement, nameof(lineSeries.LabelMargin), out var lineLabelMargin)) lineSeries.LabelMargin = lineLabelMargin;
                    if (GetColorAttribute(lineSeriesElement, nameof(lineSeries.BrokenLineColor), out var brokenLineColor)) lineSeries.BrokenLineColor = brokenLineColor;
                    if (GetEnumAttribute(lineSeriesElement, nameof(lineSeries.BrokenLineStyle), out OxyPlot.LineStyle brokenLineStyle))
                        lineSeries.BrokenLineStyle = brokenLineStyle;
                    if (GetDoubleAttribute(lineSeriesElement, nameof(lineSeries.BrokenLineThickness), out var brokenLineThickness)) lineSeries.BrokenLineThickness = brokenLineThickness;
                }
            }

            // Deserialize area series properties
            var areaSeries = series as AreaSeries;
            if (areaSeries != null)
            {
                var areaSeriesElement = element.Element(nameof(AreaSeries));
                if (areaSeriesElement != null)
                {
                    if (GetColorAttribute(areaSeriesElement, nameof(areaSeries.Color2), out var color2)) areaSeries.Color2 = color2;
                    if (GetColorAttribute(areaSeriesElement, nameof(areaSeries.Fill), out var areaFill)) areaSeries.Fill = areaFill;
                    if (GetStringAttribute(areaSeriesElement, nameof(areaSeries.DataFieldX2), out var dataFieldX2)) areaSeries.DataFieldX2 = dataFieldX2;
                    if (GetStringAttribute(areaSeriesElement, nameof(areaSeries.DataFieldY2), out var dataFieldY2)) areaSeries.DataFieldY2 = dataFieldY2;
                    if (GetBooleanAttribute(areaSeriesElement, nameof(areaSeries.Reverse2), out var reverse2)) areaSeries.Reverse2 = reverse2;
                }
            }

            // Deserialize box plot properties
            var boxPlotSeries = series as BoxPlotSeries;
            if (boxPlotSeries != null)
            {
                var boxPlotseriesElement = element.Element(nameof(BoxPlotSeries));
                if (boxPlotseriesElement != null)
                {
                    if (GetDoubleAttribute(boxPlotseriesElement, nameof(boxPlotSeries.StrokeThickness), out var boxStrokeThickness)) boxPlotSeries.StrokeThickness = boxStrokeThickness;
                    if (GetColorAttribute(boxPlotseriesElement, nameof(boxPlotSeries.Stroke), out var boxStroke)) boxPlotSeries.Stroke = boxStroke;
                    if (GetEnumAttribute(boxPlotseriesElement, nameof(boxPlotSeries.LineStyle), out OxyPlot.LineStyle boxLineStyle))
                        boxPlotSeries.LineStyle = boxLineStyle;
                    if (GetBooleanAttribute(boxPlotseriesElement, nameof(boxPlotSeries.IsVertical), out var isVertical)) boxPlotSeries.IsVertical = isVertical;
                    if (GetColorAttribute(boxPlotseriesElement, nameof(boxPlotSeries.Fill), out var boxFill)) boxPlotSeries.Fill = boxFill;
                    if (GetEnumAttribute(boxPlotseriesElement, nameof(boxPlotSeries.OutlierType), out OxyPlot.MarkerType outlierType))
                        boxPlotSeries.OutlierType = outlierType;
                    if (GetDoubleAttribute(boxPlotseriesElement, nameof(boxPlotSeries.WhiskerWidth), out var whiskerWidth)) boxPlotSeries.WhiskerWidth = whiskerWidth;
                    if (GetBooleanAttribute(boxPlotseriesElement, nameof(boxPlotSeries.ShowMedianAsDot), out var showMedianAsDot)) boxPlotSeries.ShowMedianAsDot = showMedianAsDot;
                    if (GetDoubleAttribute(boxPlotseriesElement, nameof(boxPlotSeries.MedianPointSize), out var medianPointSize)) boxPlotSeries.MedianPointSize = medianPointSize;
                    if (GetDoubleAttribute(boxPlotseriesElement, nameof(boxPlotSeries.OutlierSize), out var outlierSize)) boxPlotSeries.OutlierSize = outlierSize;
                    if (GetDoubleAttribute(boxPlotseriesElement, nameof(boxPlotSeries.BoxWidth), out var boxWidth)) boxPlotSeries.BoxWidth = boxWidth;
                    if (GetBooleanAttribute(boxPlotseriesElement, nameof(boxPlotSeries.ShowBox), out var showBox)) boxPlotSeries.ShowBox = showBox;
                }
            }

            // Deserialize scatter point series properties
            var scatterPointSeries = series as ScatterPointSeries;
            if (scatterPointSeries != null)
            {
                var scatterPointSeriesElement = element.Element(nameof(ScatterPointSeries));
                if (scatterPointSeriesElement != null)
                {
                    if (GetStringAttribute(scatterPointSeriesElement, nameof(scatterPointSeries.DataFieldTag), out var scatterDataFieldTag)) scatterPointSeries.DataFieldTag = scatterDataFieldTag;
                    if (GetStringAttribute(scatterPointSeriesElement, nameof(scatterPointSeries.DataFieldValue), out var scatterDataFieldValue)) scatterPointSeries.DataFieldValue = scatterDataFieldValue;
                    if (GetStringAttribute(scatterPointSeriesElement, nameof(scatterPointSeries.ColorAxisKey), out var scatterColorAxisKey)) scatterPointSeries.ColorAxisKey = scatterColorAxisKey;
                    if (GetIntegerAttribute(scatterPointSeriesElement, nameof(scatterPointSeries.BinSize), out var scatterBinSize)) scatterPointSeries.BinSize = scatterBinSize;
                    if (GetColorAttribute(scatterPointSeriesElement, nameof(scatterPointSeries.MarkerFill), out var scatterMarkerFill)) scatterPointSeries.MarkerFill = scatterMarkerFill;
                    if (GetDoubleAttribute(scatterPointSeriesElement, nameof(scatterPointSeries.MarkerSize), out var scatterMarkerSize)) scatterPointSeries.MarkerSize = scatterMarkerSize;
                    if (GetColorAttribute(scatterPointSeriesElement, nameof(scatterPointSeries.MarkerStroke), out var scatterMarkerStroke)) scatterPointSeries.MarkerStroke = scatterMarkerStroke;
                    if (GetDoubleAttribute(scatterPointSeriesElement, nameof(scatterPointSeries.MarkerStrokeThickness), out var scatterMarkerStrokeThickness)) scatterPointSeries.MarkerStrokeThickness = scatterMarkerStrokeThickness;
                    if (GetEnumAttribute(scatterPointSeriesElement, nameof(scatterPointSeries.MarkerType), out OxyPlot.MarkerType scatterMarkerType))
                        scatterPointSeries.MarkerType = scatterMarkerType;
                }
            }

            // Deserialize scatter error series properties
            var scatterErrorSeries = series as ScatterErrorSeries;
            if (scatterErrorSeries != null)
            {
                var errorSeriesElement = element.Element(nameof(ScatterErrorSeries));
                if (errorSeriesElement != null)
                {
                    if (GetStringAttribute(errorSeriesElement, nameof(scatterErrorSeries.DataFieldTag), out var errDataFieldTag)) scatterErrorSeries.DataFieldTag = errDataFieldTag;
                    if (GetStringAttribute(errorSeriesElement, nameof(scatterErrorSeries.DataFieldValue), out var errDataFieldValue)) scatterErrorSeries.DataFieldValue = errDataFieldValue;
                    if (GetStringAttribute(errorSeriesElement, nameof(scatterErrorSeries.ColorAxisKey), out var errColorAxisKey)) scatterErrorSeries.ColorAxisKey = errColorAxisKey;
                    if (GetIntegerAttribute(errorSeriesElement, nameof(scatterErrorSeries.BinSize), out var errBinSize)) scatterErrorSeries.BinSize = errBinSize;
                    if (GetColorAttribute(errorSeriesElement, nameof(scatterErrorSeries.MarkerFill), out var errMarkerFill)) scatterErrorSeries.MarkerFill = errMarkerFill;
                    if (GetDoubleAttribute(errorSeriesElement, nameof(scatterErrorSeries.MarkerSize), out var errMarkerSize)) scatterErrorSeries.MarkerSize = errMarkerSize;
                    if (GetColorAttribute(errorSeriesElement, nameof(scatterErrorSeries.MarkerStroke), out var errMarkerStroke)) scatterErrorSeries.MarkerStroke = errMarkerStroke;
                    if (GetDoubleAttribute(errorSeriesElement, nameof(scatterErrorSeries.MarkerStrokeThickness), out var errMarkerStrokeThickness)) scatterErrorSeries.MarkerStrokeThickness = errMarkerStrokeThickness;
                    if (GetEnumAttribute(errorSeriesElement, nameof(scatterErrorSeries.MarkerType), out OxyPlot.MarkerType errMarkerType))
                        scatterErrorSeries.MarkerType = errMarkerType;
                    if (GetDoubleAttribute(errorSeriesElement, nameof(scatterErrorSeries.ErrorBarStopWidth), out var errorBarStopWidth)) scatterErrorSeries.ErrorBarStopWidth = errorBarStopWidth;
                    if (GetDoubleAttribute(errorSeriesElement, nameof(scatterErrorSeries.MinimumErrorSize), out var minimumErrorSize)) scatterErrorSeries.MinimumErrorSize = minimumErrorSize;
                    if (GetDoubleAttribute(errorSeriesElement, nameof(scatterErrorSeries.ErrorBarStrokeThickness), out var errorBarStrokeThickness)) scatterErrorSeries.ErrorBarStrokeThickness = errorBarStrokeThickness;
                    if (GetColorAttribute(errorSeriesElement, nameof(scatterErrorSeries.ErrorBarColor), out var errorBarColor)) scatterErrorSeries.ErrorBarColor = errorBarColor;
                    if (GetStringAttribute(errorSeriesElement, nameof(scatterErrorSeries.DataFieldLowerErrorX), out var dataFieldLowerErrorX)) scatterErrorSeries.DataFieldLowerErrorX = dataFieldLowerErrorX;
                    if (GetStringAttribute(errorSeriesElement, nameof(scatterErrorSeries.DataFieldUpperErrorX), out var dataFieldUpperErrorX)) scatterErrorSeries.DataFieldUpperErrorX = dataFieldUpperErrorX;
                    if (GetStringAttribute(errorSeriesElement, nameof(scatterErrorSeries.DataFieldLowerErrorY), out var dataFieldLowerErrorY)) scatterErrorSeries.DataFieldLowerErrorY = dataFieldLowerErrorY;
                    if (GetStringAttribute(errorSeriesElement, nameof(scatterErrorSeries.DataFieldUpperErrorY), out var dataFieldUpperErrorY)) scatterErrorSeries.DataFieldUpperErrorY = dataFieldUpperErrorY;
                }
            }

            // Deserialize heat map series properties
            var heatMapSeries = series as HeatMapSeries;
            if (heatMapSeries != null)
            {
                var heatMapSeriesElement = element.Element(nameof(HeatMapSeries));
                if (heatMapSeriesElement != null)
                {
                    if (GetStringAttribute(heatMapSeriesElement, nameof(heatMapSeries.ColorAxisKey), out var heatColorAxisKey)) heatMapSeries.ColorAxisKey = heatColorAxisKey;
                    if (GetDoubleAttribute(heatMapSeriesElement, nameof(heatMapSeries.Y0), out var y0)) heatMapSeries.Y0 = y0;
                    if (GetDoubleAttribute(heatMapSeriesElement, nameof(heatMapSeries.Y1), out var y1)) heatMapSeries.Y1 = y1;
                    if (GetDoubleAttribute(heatMapSeriesElement, nameof(heatMapSeries.X0), out var x0)) heatMapSeries.X0 = x0;
                    if (GetDoubleAttribute(heatMapSeriesElement, nameof(heatMapSeries.X1), out var x1)) heatMapSeries.X1 = x1;
                    if (GetColorAttribute(heatMapSeriesElement, nameof(heatMapSeries.HighColor), out var highColor)) heatMapSeries.HighColor = highColor;
                    if (GetColorAttribute(heatMapSeriesElement, nameof(heatMapSeries.LowColor), out var lowColor)) heatMapSeries.LowColor = lowColor;
                    if (GetEnumAttribute(heatMapSeriesElement, nameof(heatMapSeries.CoordinateDefinition), out OxyPlot.Series.HeatMapCoordinateDefinition coordinateDefinition))
                        heatMapSeries.CoordinateDefinition = coordinateDefinition;
                    if (GetBooleanAttribute(heatMapSeriesElement, nameof(heatMapSeries.Interpolate), out var interpolate)) heatMapSeries.Interpolate = interpolate;
                    if (GetDoubleAttribute(heatMapSeriesElement, nameof(heatMapSeries.LabelFontSize), out var labelFontSize)) heatMapSeries.LabelFontSize = labelFontSize;
                }
            }

            // Deserialize high-low series properties
            var highLowSeries = series as HighLowSeries;
            if (highLowSeries != null)
            {
                var highLowSeriesElement = element.Element(nameof(HighLowSeries));
                if (highLowSeriesElement != null)
                {
                    if (GetColorAttribute(highLowSeriesElement, nameof(highLowSeries.Color), out var hlColor)) highLowSeries.Color = hlColor;
                    if (GetDoubleAttribute(highLowSeriesElement, nameof(highLowSeries.StrokeThickness), out var hlStrokeThickness)) highLowSeries.StrokeThickness = hlStrokeThickness;
                    if (GetDoubleAttribute(highLowSeriesElement, nameof(highLowSeries.TickLength), out var tickLength)) highLowSeries.TickLength = tickLength;
                    if (GetEnumAttribute(highLowSeriesElement, nameof(highLowSeries.LineStyle), out OxyPlot.LineStyle hlLineStyle))
                        highLowSeries.LineStyle = hlLineStyle;
                    if (GetStringAttribute(highLowSeriesElement, nameof(highLowSeries.DataFieldX), out var hlDataFieldX)) highLowSeries.DataFieldX = hlDataFieldX;
                    if (GetStringAttribute(highLowSeriesElement, nameof(highLowSeries.DataFieldHigh), out var dataFieldHigh)) highLowSeries.DataFieldHigh = dataFieldHigh;
                    if (GetStringAttribute(highLowSeriesElement, nameof(highLowSeries.DataFieldLow), out var dataFieldLow)) highLowSeries.DataFieldLow = dataFieldLow;
                    if (GetStringAttribute(highLowSeriesElement, nameof(highLowSeries.DataFieldOpen), out var dataFieldOpen)) highLowSeries.DataFieldOpen = dataFieldOpen;
                    if (GetStringAttribute(highLowSeriesElement, nameof(highLowSeries.DataFieldClose), out var dataFieldClose)) highLowSeries.DataFieldClose = dataFieldClose;
                }
            }

            // Deserialize candlestick series properties
            var candleStickSeries = series as CandleStickSeries;
            if (candleStickSeries != null)
            {
                var candleSeriesElement = element.Element(nameof(CandleStickSeries));
                if (candleSeriesElement != null)
                {
                    if (GetColorAttribute(candleSeriesElement, nameof(candleStickSeries.IncreasingColor), out var increasingColor)) candleStickSeries.IncreasingColor = increasingColor;
                    if (GetColorAttribute(candleSeriesElement, nameof(candleStickSeries.DecreasingColor), out var decreasingColor)) candleStickSeries.DecreasingColor = decreasingColor;
                    if (GetDoubleAttribute(candleSeriesElement, nameof(candleStickSeries.CandleWidth), out var candleWidth)) candleStickSeries.CandleWidth = candleWidth;
                }
            }

            // Deserialize pie series properties
            var pieSeries = series as PieSeries;
            if (pieSeries != null)
            {
                var pieSeriesElement = element.Element(nameof(PieSeries));
                if (pieSeriesElement != null)
                {
                    if (GetColorAttribute(pieSeriesElement, nameof(pieSeries.Stroke), out var pieStroke)) pieSeries.Stroke = pieStroke;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.StrokeThickness), out var pieStrokeThickness)) pieSeries.StrokeThickness = pieStrokeThickness;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.Diameter), out var diameter)) pieSeries.Diameter = diameter;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.InnerDiameter), out var innerDiameter)) pieSeries.InnerDiameter = innerDiameter;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.StartAngle), out var startAngle)) pieSeries.StartAngle = startAngle;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.AngleSpan), out var angleSpan)) pieSeries.AngleSpan = angleSpan;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.AngleIncrement), out var angleIncrement)) pieSeries.AngleIncrement = angleIncrement;
                    if (GetStringAttribute(pieSeriesElement, nameof(pieSeries.LegendFormat), out var legendFormat)) pieSeries.LegendFormat = legendFormat;
                    if (GetStringAttribute(pieSeriesElement, nameof(pieSeries.OutsideLabelFormat), out var outsideLabelFormat)) pieSeries.OutsideLabelFormat = outsideLabelFormat;
                    if (GetColorAttribute(pieSeriesElement, nameof(pieSeries.InsideLabelColor), out var insideLabelColor)) pieSeries.InsideLabelColor = insideLabelColor;
                    if (GetStringAttribute(pieSeriesElement, nameof(pieSeries.InsideLabelFormat), out var insideLabelFormat)) pieSeries.InsideLabelFormat = insideLabelFormat;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.InsideLabelPosition), out var insideLabelPosition)) pieSeries.InsideLabelPosition = insideLabelPosition;
                    if (GetBooleanAttribute(pieSeriesElement, nameof(pieSeries.AreInsideLabelsAngled), out var areInsideLabelsAngled)) pieSeries.AreInsideLabelsAngled = areInsideLabelsAngled;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.TickDistance), out var tickDistance)) pieSeries.TickDistance = tickDistance;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.TickRadialLength), out var tickRadialLength)) pieSeries.TickRadialLength = tickRadialLength;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.TickHorizontalLength), out var tickHorizontalLength)) pieSeries.TickHorizontalLength = tickHorizontalLength;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.TickLabelDistance), out var tickLabelDistance)) pieSeries.TickLabelDistance = tickLabelDistance;
                    if (GetDoubleAttribute(pieSeriesElement, nameof(pieSeries.ExplodedDistance), out var explodedDistance)) pieSeries.ExplodedDistance = explodedDistance;
                    if (GetStringAttribute(pieSeriesElement, nameof(pieSeries.LabelField), out var labelField)) pieSeries.LabelField = labelField;
                    if (GetStringAttribute(pieSeriesElement, nameof(pieSeries.ValueField), out var valueField)) pieSeries.ValueField = valueField;
                    if (GetStringAttribute(pieSeriesElement, nameof(pieSeries.ColorField), out var colorField)) pieSeries.ColorField = colorField;
                    if (GetStringAttribute(pieSeriesElement, nameof(pieSeries.IsExplodedField), out var isExplodedField)) pieSeries.IsExplodedField = isExplodedField;
                }
            }

            // Deserialize contour series properties
            var contourSeries = series as ContourSeries;
            if (contourSeries != null)
            {
                var contourSeriesElement = element.Element(nameof(ContourSeries));
                if (contourSeriesElement != null)
                {
                    if (GetColorAttribute(contourSeriesElement, nameof(contourSeries.Color), out var contourColor)) contourSeries.Color = contourColor;
                    if (GetDoubleAttribute(contourSeriesElement, nameof(contourSeries.StrokeThickness), out var contourStrokeThickness)) contourSeries.StrokeThickness = contourStrokeThickness;
                    if (GetEnumAttribute(contourSeriesElement, nameof(contourSeries.LineStyle), out OxyPlot.LineStyle contourLineStyle))
                        contourSeries.LineStyle = contourLineStyle;
                    if (GetDoubleAttribute(contourSeriesElement, nameof(contourSeries.ContourLevelStep), out var contourLevelStep)) contourSeries.ContourLevelStep = contourLevelStep;
                    if (GetColorAttribute(contourSeriesElement, nameof(contourSeries.LabelBackground), out var labelBackground)) contourSeries.LabelBackground = labelBackground;
                    if (GetIntegerAttribute(contourSeriesElement, nameof(contourSeries.LabelStep), out var labelStep)) contourSeries.LabelStep = labelStep;
                }
            }

            // Deserialize vector series properties
            var vectorSeries = series as VectorSeries;
            if (vectorSeries != null)
            {
                var vectorSeriesElement = element.Element(nameof(VectorSeries));
                if (vectorSeriesElement != null)
                {
                    if (GetColorAttribute(vectorSeriesElement, nameof(vectorSeries.Color), out var vectorColor)) vectorSeries.Color = vectorColor;
                    if (GetDoubleAttribute(vectorSeriesElement, nameof(vectorSeries.StrokeThickness), out var vectorStrokeThickness)) vectorSeries.StrokeThickness = vectorStrokeThickness;
                    if (GetDoubleAttribute(vectorSeriesElement, nameof(vectorSeries.ArrowHeadLength), out var arrowHeadLength)) vectorSeries.ArrowHeadLength = arrowHeadLength;
                    if (GetDoubleAttribute(vectorSeriesElement, nameof(vectorSeries.ArrowHeadWidth), out var arrowHeadWidth)) vectorSeries.ArrowHeadWidth = arrowHeadWidth;
                    if (GetDoubleAttribute(vectorSeriesElement, nameof(vectorSeries.ArrowHeadPosition), out var arrowHeadPosition)) vectorSeries.ArrowHeadPosition = arrowHeadPosition;
                    if (GetDoubleAttribute(vectorSeriesElement, nameof(vectorSeries.ArrowVeeness), out var arrowVeeness)) vectorSeries.ArrowVeeness = arrowVeeness;
                    if (GetDoubleAttribute(vectorSeriesElement, nameof(vectorSeries.ArrowStartPosition), out var arrowStartPosition)) vectorSeries.ArrowStartPosition = arrowStartPosition;
                    if (GetEnumAttribute(vectorSeriesElement, nameof(vectorSeries.LineStyle), out OxyPlot.LineStyle vectorLineStyle))
                        vectorSeries.LineStyle = vectorLineStyle;
                    if (GetStringAttribute(vectorSeriesElement, nameof(vectorSeries.ColorAxisKey), out var vectorColorAxisKey)) vectorSeries.ColorAxisKey = vectorColorAxisKey;
                    if (GetStringAttribute(vectorSeriesElement, nameof(vectorSeries.LabelFormatString), out var vectorLabelFormatString)) vectorSeries.LabelFormatString = vectorLabelFormatString;
                    if (GetDoubleAttribute(vectorSeriesElement, nameof(vectorSeries.LabelFontSize), out var vectorLabelFontSize)) vectorSeries.LabelFontSize = vectorLabelFontSize;
                }
            }

            // Deserialize rectangle series properties
            var rectangleSeries = series as RectangleSeries;
            if (rectangleSeries != null)
            {
                var rectangleSeriesElement = element.Element(nameof(RectangleSeries));
                if (rectangleSeriesElement != null)
                {
                    if (GetBooleanAttribute(rectangleSeriesElement, nameof(rectangleSeries.CanTrackerInterpolatePoints), out var rectCanInterpolate)) rectangleSeries.CanTrackerInterpolatePoints = rectCanInterpolate;
                    if (GetStringAttribute(rectangleSeriesElement, nameof(rectangleSeries.ColorAxisKey), out var rectColorAxisKey)) rectangleSeries.ColorAxisKey = rectColorAxisKey;
                    if (GetStringAttribute(rectangleSeriesElement, nameof(rectangleSeries.LabelFormatString), out var rectLabelFormatString)) rectangleSeries.LabelFormatString = rectLabelFormatString;
                    if (GetDoubleAttribute(rectangleSeriesElement, nameof(rectangleSeries.LabelFontSize), out var rectLabelFontSize)) rectangleSeries.LabelFontSize = rectLabelFontSize;
                }
            }

            // Deserialize two-color line series properties
            var twoColorLineSeries = series as TwoColorLineSeries;
            if (twoColorLineSeries != null)
            {
                var twoColorElement = element.Element(nameof(TwoColorLineSeries));
                if (twoColorElement != null)
                {
                    if (GetColorAttribute(twoColorElement, nameof(twoColorLineSeries.Color2), out var color2)) twoColorLineSeries.Color2 = color2;
                    if (GetDoubleAttribute(twoColorElement, nameof(twoColorLineSeries.Limit), out var limit)) twoColorLineSeries.Limit = limit;
                    if (GetEnumAttribute(twoColorElement, nameof(twoColorLineSeries.LineStyle2), out OxyPlot.LineStyle lineStyle2))
                        twoColorLineSeries.LineStyle2 = lineStyle2;
                }
            }

            // Deserialize three-color line series properties
            var threeColorLineSeries = series as ThreeColorLineSeries;
            if (threeColorLineSeries != null)
            {
                var threeColorElement = element.Element(nameof(ThreeColorLineSeries));
                if (threeColorElement != null)
                {
                    if (GetColorAttribute(threeColorElement, nameof(threeColorLineSeries.ColorLo), out var colorLo)) threeColorLineSeries.ColorLo = colorLo;
                    if (GetColorAttribute(threeColorElement, nameof(threeColorLineSeries.ColorHi), out var colorHi)) threeColorLineSeries.ColorHi = colorHi;
                    if (GetDoubleAttribute(threeColorElement, nameof(threeColorLineSeries.LimitLo), out var limitLo)) threeColorLineSeries.LimitLo = limitLo;
                    if (GetDoubleAttribute(threeColorElement, nameof(threeColorLineSeries.LimitHi), out var limitHi)) threeColorLineSeries.LimitHi = limitHi;
                    if (GetEnumAttribute(threeColorElement, nameof(threeColorLineSeries.LineStyleLo), out OxyPlot.LineStyle lineStyleLo))
                        threeColorLineSeries.LineStyleLo = lineStyleLo;
                    if (GetEnumAttribute(threeColorElement, nameof(threeColorLineSeries.LineStyleHi), out OxyPlot.LineStyle lineStyleHi))
                        threeColorLineSeries.LineStyleHi = lineStyleHi;
                }
            }

            // Deserialize stair-step series properties
            var stairStepSeries = series as StairStepSeries;
            if (stairStepSeries != null)
            {
                var stairStepElement = element.Element(nameof(StairStepSeries));
                if (stairStepElement != null)
                {
                    if (GetEnumAttribute(stairStepElement, nameof(stairStepSeries.VerticalLineStyle), out OxyPlot.LineStyle verticalLineStyle))
                        stairStepSeries.VerticalLineStyle = verticalLineStyle;
                    if (GetDoubleAttribute(stairStepElement, nameof(stairStepSeries.VerticalStrokeThickness), out var verticalStrokeThickness)) stairStepSeries.VerticalStrokeThickness = verticalStrokeThickness;
                }
            }

            // Deserialize stem series properties
            var stemSeries = series as StemSeries;
            if (stemSeries != null)
            {
                var stemElement = element.Element(nameof(StemSeries));
                if (stemElement != null)
                {
                    if (GetDoubleAttribute(stemElement, nameof(stemSeries.Base), out var stemBase)) stemSeries.Base = stemBase;
                }
            }

            // Deserialize extrapolation line series properties
            var extrapolationLineSeries = series as ExtrapolationLineSeries;
            if (extrapolationLineSeries != null)
            {
                var extrapolationElement = element.Element(nameof(ExtrapolationLineSeries));
                if (extrapolationElement != null)
                {
                    if (GetColorAttribute(extrapolationElement, nameof(extrapolationLineSeries.ExtrapolationColor), out var extrapolationColor)) extrapolationLineSeries.ExtrapolationColor = extrapolationColor;
                    if (GetEnumAttribute(extrapolationElement, nameof(extrapolationLineSeries.ExtrapolationLineStyle), out OxyPlot.LineStyle extrapolationLineStyle)) extrapolationLineSeries.ExtrapolationLineStyle = extrapolationLineStyle;
                    if (GetBooleanAttribute(extrapolationElement, nameof(extrapolationLineSeries.IgnoreExtrapolationForScaling), out var ignoreExtrapolationForScaling)) extrapolationLineSeries.IgnoreExtrapolationForScaling = ignoreExtrapolationForScaling;
                }
            }

            // Deserialize scatter series properties
            var scatterSeries = series as ScatterSeries;
            if (scatterSeries != null)
            {
                var scatterElement = element.Element(nameof(ScatterSeries));
                if (scatterElement != null)
                {
                    if (GetStringAttribute(scatterElement, nameof(scatterSeries.DataFieldX), out var scatDataFieldX)) scatterSeries.DataFieldX = scatDataFieldX;
                    if (GetStringAttribute(scatterElement, nameof(scatterSeries.DataFieldY), out var scatDataFieldY)) scatterSeries.DataFieldY = scatDataFieldY;
                    if (GetStringAttribute(scatterElement, nameof(scatterSeries.DataFieldTag), out var scatDataFieldTag)) scatterSeries.DataFieldTag = scatDataFieldTag;
                    if (GetStringAttribute(scatterElement, nameof(scatterSeries.DataFieldValue), out var scatDataFieldValue)) scatterSeries.DataFieldValue = scatDataFieldValue;
                    if (GetStringAttribute(scatterElement, nameof(scatterSeries.DataFieldSize), out var scatDataFieldSize)) scatterSeries.DataFieldSize = scatDataFieldSize;
                    if (GetStringAttribute(scatterElement, nameof(scatterSeries.ColorAxisKey), out var scatColorAxisKey)) scatterSeries.ColorAxisKey = scatColorAxisKey;
                    if (GetIntegerAttribute(scatterElement, nameof(scatterSeries.BinSize), out var scatBinSize)) scatterSeries.BinSize = scatBinSize;
                    if (GetColorAttribute(scatterElement, nameof(scatterSeries.MarkerFill), out var scatMarkerFill)) scatterSeries.MarkerFill = scatMarkerFill;
                    if (GetDoubleAttribute(scatterElement, nameof(scatterSeries.MarkerSize), out var scatMarkerSize)) scatterSeries.MarkerSize = scatMarkerSize;
                    if (GetColorAttribute(scatterElement, nameof(scatterSeries.MarkerStroke), out var scatMarkerStroke)) scatterSeries.MarkerStroke = scatMarkerStroke;
                    if (GetDoubleAttribute(scatterElement, nameof(scatterSeries.MarkerStrokeThickness), out var scatMarkerStrokeThickness)) scatterSeries.MarkerStrokeThickness = scatMarkerStrokeThickness;
                    if (GetEnumAttribute(scatterElement, nameof(scatterSeries.MarkerType), out OxyPlot.MarkerType scatMarkerType))
                        scatterSeries.MarkerType = scatMarkerType;
                }
            }

            return series;
        }
    }
}
