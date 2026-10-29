using System.Globalization;
using OxyPlot;
using Xunit;

namespace OxyPlotControls.Tests.Converters;

/// <summary>
/// Tests for ScreenVectorToPointConverter.
/// Verifies proper conversion between OxyPlot ScreenVector and WPF Point objects.
/// </summary>
public class ScreenVectorToPointConverterTests
{
    /// <summary>
    /// The converter instance being tested.
    /// </summary>
    private readonly ScreenVectorToPointConverter _converter = new();

    #region Convert Tests

    [Fact]
    public void Convert_NullValue_ReturnsNull()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _converter.Convert(value, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Convert_WrongType_ReturnsNull()
    {
        // Arrange
        var value = "not a ScreenVector";

        // Act
        var result = _converter.Convert(value, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Convert_ValidScreenVector_ReturnsPoint()
    {
        // Arrange
        var screenVector = new ScreenVector(10.5, 20.5);

        // Act
        var result = _converter.Convert(screenVector, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<System.Windows.Point>(result);
        var point = (System.Windows.Point)result!;
        Assert.Equal(10.5, point.X);
        Assert.Equal(20.5, point.Y);
    }

    [Fact]
    public void Convert_ZeroScreenVector_ReturnsZeroPoint()
    {
        // Arrange
        var screenVector = new ScreenVector(0, 0);

        // Act
        var result = _converter.Convert(screenVector, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<System.Windows.Point>(result);
        var point = (System.Windows.Point)result!;
        Assert.Equal(0, point.X);
        Assert.Equal(0, point.Y);
    }

    [Fact]
    public void Convert_NegativeScreenVector_ReturnsNegativePoint()
    {
        // Arrange
        var screenVector = new ScreenVector(-10.5, -20.5);

        // Act
        var result = _converter.Convert(screenVector, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<System.Windows.Point>(result);
        var point = (System.Windows.Point)result!;
        Assert.Equal(-10.5, point.X);
        Assert.Equal(-20.5, point.Y);
    }

    #endregion

    #region ConvertBack Tests

    [Fact]
    public void ConvertBack_NullValue_ReturnsNull()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _converter.ConvertBack(value, typeof(ScreenVector), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertBack_WrongType_ReturnsNull()
    {
        // Arrange
        var value = "not a Point";

        // Act
        var result = _converter.ConvertBack(value, typeof(ScreenVector), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertBack_ValidPoint_ReturnsScreenVector()
    {
        // Arrange
        var point = new System.Windows.Point(10.5, 20.5);

        // Act
        var result = _converter.ConvertBack(point, typeof(ScreenVector), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<ScreenVector>(result);
        var screenVector = (ScreenVector)result!;
        Assert.Equal(10.5, screenVector.X);
        Assert.Equal(20.5, screenVector.Y);
    }

    [Fact]
    public void ConvertBack_ZeroPoint_ReturnsZeroScreenVector()
    {
        // Arrange
        var point = new System.Windows.Point(0, 0);

        // Act
        var result = _converter.ConvertBack(point, typeof(ScreenVector), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<ScreenVector>(result);
        var screenVector = (ScreenVector)result!;
        Assert.Equal(0, screenVector.X);
        Assert.Equal(0, screenVector.Y);
    }

    [Fact]
    public void ConvertBack_NegativePoint_ReturnsNegativeScreenVector()
    {
        // Arrange
        var point = new System.Windows.Point(-10.5, -20.5);

        // Act
        var result = _converter.ConvertBack(point, typeof(ScreenVector), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<ScreenVector>(result);
        var screenVector = (ScreenVector)result!;
        Assert.Equal(-10.5, screenVector.X);
        Assert.Equal(-20.5, screenVector.Y);
    }

    #endregion

    #region Round Trip Tests

    [Fact]
    public void Convert_ConvertBack_RoundTrip()
    {
        // Arrange
        var originalVector = new ScreenVector(123.456, 789.012);

        // Act
        var converted = _converter.Convert(originalVector, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);
        var roundTripped = _converter.ConvertBack(converted, typeof(ScreenVector), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<ScreenVector>(roundTripped);
        var result = (ScreenVector)roundTripped!;
        Assert.Equal(originalVector.X, result.X);
        Assert.Equal(originalVector.Y, result.Y);
    }

    #endregion
}
