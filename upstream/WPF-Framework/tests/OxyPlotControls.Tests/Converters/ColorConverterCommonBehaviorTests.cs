using System.Globalization;
using System.Windows.Media;
using OxyPlot;
using Xunit;

namespace OxyPlotControls.Tests.Converters;

/// <summary>
/// Common tests for all color converters to ensure consistent behavior.
/// Validates that all converters handle automatic colors and error conditions consistently.
/// </summary>
public class ColorConverterCommonBehaviorTests
{
    /// <summary>
    /// Tests that all converters handle OxyPlot's automatic color (ARGB 0,0,0,1) consistently.
    /// </summary>
    [Fact]
    public void AllConverters_HandleAutomaticColor_Consistently()
    {
        // Arrange - OxyPlot uses ARGB(0,0,0,1) to represent automatic color
        var automaticColor = Color.FromArgb(0, 0, 0, 1);
        object[] values = { automaticColor, null! };

        // Test LineSeriesColorConverter
        var lineConverter = new LineSeriesColorConverter();
        var lineResult = lineConverter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);
        Assert.IsType<SolidColorBrush>(lineResult);

        // Test AreaSeriesColor2Converter
        var areaColor2Converter = new AreaSeriesColor2Converter();
        var areaColor2Result = areaColor2Converter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);
        Assert.IsType<SolidColorBrush>(areaColor2Result);

        // Test AreaSeriesFillConverter
        var areaFillConverter = new AreaSeriesFillConverter();
        var areaFillResult = areaFillConverter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);
        Assert.IsType<SolidColorBrush>(areaFillResult);

        // Test LineSeriesMarkerFillConverter
        var markerFillConverter = new LineSeriesMarkerFillConverter();
        var markerFillResult = markerFillConverter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);
        Assert.IsType<SolidColorBrush>(markerFillResult);

        // Test BarSeriesFillConverter
        var barConverter = new BarSeriesFillConverter();
        var barResult = barConverter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);
        Assert.IsType<SolidColorBrush>(barResult);

        // Test BoxPlotSeriesFillConverter
        var boxConverter = new BoxPlotSeriesFillConverter();
        var boxResult = boxConverter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);
        Assert.IsType<SolidColorBrush>(boxResult);

        // Test ScatterSeriesMarkerFillConverter
        var scatterFillConverter = new ScatterSeriesMarkerFillConverter();
        var scatterFillResult = scatterFillConverter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);
        Assert.IsType<SolidColorBrush>(scatterFillResult);
    }

    /// <summary>
    /// Tests that all converters return null when given a null color value.
    /// </summary>
    [Fact]
    public void AllConverters_ReturnNull_ForNullColor()
    {
        // Arrange
        object[] values = { null!, null! };

        // Test all converters
        Assert.Null(new LineSeriesColorConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new AreaSeriesColor2Converter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new AreaSeriesFillConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new LineSeriesMarkerFillConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new LineSeriesMarkerStrokeConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new BarSeriesFillConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new BoxPlotSeriesFillConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new ScatterSeriesMarkerFillConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new ScatterSeriesMarkerStrokeConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Tests that all converters return null when given an incorrect type.
    /// </summary>
    [Fact]
    public void AllConverters_ReturnNull_ForWrongType()
    {
        // Arrange
        object[] values = { "not a color", null! };

        // Test all converters
        Assert.Null(new LineSeriesColorConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new AreaSeriesColor2Converter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new AreaSeriesFillConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new LineSeriesMarkerFillConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new LineSeriesMarkerStrokeConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new BarSeriesFillConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new BoxPlotSeriesFillConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new ScatterSeriesMarkerFillConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
        Assert.Null(new ScatterSeriesMarkerStrokeConverter().Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture));
    }
}
