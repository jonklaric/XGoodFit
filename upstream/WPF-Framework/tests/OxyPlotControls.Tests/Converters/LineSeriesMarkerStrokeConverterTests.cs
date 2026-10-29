using System.Globalization;
using System.Windows.Media;
using OxyPlot;
using Xunit;

namespace OxyPlotControls.Tests.Converters;

/// <summary>
/// Tests for LineSeriesMarkerStrokeConverter (IMultiValueConverter).
/// Verifies proper conversion between Color values and WPF SolidColorBrush objects for line series marker stroke.
/// </summary>
public class LineSeriesMarkerStrokeConverterTests
{
    /// <summary>
    /// The converter instance being tested.
    /// </summary>
    private readonly LineSeriesMarkerStrokeConverter _converter = new();

    #region Convert Tests

    /// <summary>
    /// Tests that Convert returns null when given a null color value.
    /// </summary>
    [Fact]
    public void Convert_NullColor_ReturnsNull()
    {
        // Arrange
        object[] values = { null!, null! };

        // Act
        var result = _converter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that Convert returns null when given an incorrect type.
    /// </summary>
    [Fact]
    public void Convert_WrongColorType_ReturnsNull()
    {
        // Arrange
        object[] values = { "not a color", null! };

        // Act
        var result = _converter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that Convert returns a SolidColorBrush with the correct color when given a valid color with no series object.
    /// </summary>
    [Fact]
    public void Convert_ValidColorNoSeries_ReturnsBrush()
    {
        // Arrange
        var color = Color.FromArgb(255, 200, 100, 50);
        object[] values = { color, null! };

        // Act
        var result = _converter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result!;
        Assert.Equal(color, brush.Color);
    }

    /// <summary>
    /// Tests that Convert returns a magenta brush when given the Magenta color.
    /// </summary>
    [Fact]
    public void Convert_MagentaColor_ReturnsMagentaBrush()
    {
        // Arrange
        var color = Colors.Magenta;
        object[] values = { color, null! };

        // Act
        var result = _converter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result!;
        Assert.Equal(Colors.Magenta, brush.Color);
    }

    #endregion

    #region ConvertBack Tests

    /// <summary>
    /// Tests that ConvertBack returns black color when given an incorrect brush type.
    /// </summary>
    [Fact]
    public void ConvertBack_WrongBrushType_ReturnsBlackColor()
    {
        // Arrange
        var color = Colors.Lime;
        object[] values = { color, null! };
        _converter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        var value = 456.78;

        // Act
        var result = _converter.ConvertBack(value, new[] { typeof(Color), typeof(object) }, null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(Color.FromArgb(255, 0, 0, 0), result[0]);
    }

    #endregion
}
