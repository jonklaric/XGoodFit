using System.Globalization;
using System.Windows.Media;
using OxyPlot;
using Xunit;

namespace OxyPlotControls.Tests.Converters;

/// <summary>
/// Tests for LineSeriesMarkerFillConverter (IMultiValueConverter).
/// Verifies proper conversion between Color values and WPF SolidColorBrush objects for line series marker fill.
/// </summary>
public class LineSeriesMarkerFillConverterTests
{
    /// <summary>
    /// The converter instance being tested.
    /// </summary>
    private readonly LineSeriesMarkerFillConverter _converter = new();

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
        var color = Color.FromArgb(255, 50, 100, 150);
        object[] values = { color, null! };

        // Act
        var result = _converter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result!;
        Assert.Equal(color, brush.Color);
    }

    /// <summary>
    /// Tests that Convert returns a yellow brush when given the Yellow color.
    /// </summary>
    [Fact]
    public void Convert_YellowColor_ReturnsYellowBrush()
    {
        // Arrange
        var color = Colors.Yellow;
        object[] values = { color, null! };

        // Act
        var result = _converter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result!;
        Assert.Equal(Colors.Yellow, brush.Color);
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
        var color = Colors.Cyan;
        object[] values = { color, null! };
        _converter.Convert(values, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        var value = 123;

        // Act
        var result = _converter.ConvertBack(value, new[] { typeof(Color), typeof(object) }, null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(Color.FromArgb(255, 0, 0, 0), result[0]);
    }

    #endregion
}
