using System.Globalization;
using System.Windows.Media;
using Xunit;

namespace OxyPlotControls.Tests.Converters;

/// <summary>
/// Tests for SolidColorBrushConverter.
/// Verifies proper bidirectional conversion between SolidColorBrush and Brush types.
/// </summary>
public class SolidColorBrushConverterTests
{
    /// <summary>
    /// The converter instance being tested.
    /// </summary>
    private readonly SolidColorBrushConverter _converter = new();

    #region Convert Tests

    [Fact]
    public void Convert_NullValue_ReturnsNull()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _converter.Convert(value!, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Convert_SolidColorBrush_ReturnsSameBrush()
    {
        // Arrange
        var brush = new SolidColorBrush(Colors.Red);

        // Act
        var result = _converter.Convert(brush, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Same(brush, result);
    }

    [Fact]
    public void Convert_BlueColorBrush_ReturnsCorrectBrush()
    {
        // Arrange
        var brush = new SolidColorBrush(Colors.Blue);

        // Act
        var result = _converter.Convert(brush, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Colors.Blue, ((SolidColorBrush)result!).Color);
    }

    [Fact]
    public void Convert_CustomColorBrush_ReturnsCorrectBrush()
    {
        // Arrange
        var color = Color.FromArgb(200, 100, 150, 200);
        var brush = new SolidColorBrush(color);

        // Act
        var result = _converter.Convert(brush, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(color, ((SolidColorBrush)result!).Color);
    }

    #endregion

    #region ConvertBack Tests

    [Fact]
    public void ConvertBack_NullValue_ReturnsNull()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _converter.ConvertBack(value!, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertBack_SolidColorBrush_ReturnsBrush()
    {
        // Arrange
        var brush = new SolidColorBrush(Colors.Green);

        // Act
        var result = _converter.ConvertBack(brush, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Colors.Green, ((SolidColorBrush)result!).Color);
    }

    [Fact]
    public void ConvertBack_CustomColorBrush_ReturnsBrush()
    {
        // Arrange
        var color = Color.FromArgb(128, 50, 100, 150);
        var brush = new SolidColorBrush(color);

        // Act
        var result = _converter.ConvertBack(brush, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(color, ((SolidColorBrush)result!).Color);
    }

    #endregion

    #region Round Trip Tests

    [Fact]
    public void Convert_ConvertBack_RoundTrip()
    {
        // Arrange
        var originalBrush = new SolidColorBrush(Colors.Purple);

        // Act
        var converted = _converter.Convert(originalBrush, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);
        var roundTripped = _converter.ConvertBack(converted!, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(roundTripped);
        Assert.Equal(originalBrush.Color, ((SolidColorBrush)roundTripped!).Color);
    }

    #endregion
}
