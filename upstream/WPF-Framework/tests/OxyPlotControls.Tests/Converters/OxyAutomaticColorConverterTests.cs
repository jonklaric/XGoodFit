using System.Globalization;
using System.Windows.Media;
using Xunit;

namespace OxyPlotControls.Tests.Converters;

/// <summary>
/// Tests for OxyAutomaticColorConverter.
/// Verifies proper conversion between OxyPlot color values and WPF SolidColorBrush objects.
/// </summary>
public class OxyAutomaticColorConverterTests
{
    /// <summary>
    /// The converter instance being tested.
    /// </summary>
    private readonly OxyAutomaticColorConverter _converter = new();

    #region Convert Tests

    /// <summary>
    /// Tests that Convert returns null when given a null value.
    /// </summary>
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

    /// <summary>
    /// Tests that Convert returns null when given an incorrect type.
    /// </summary>
    [Fact]
    public void Convert_WrongType_ReturnsNull()
    {
        // Arrange
        var value = "not a color";

        // Act
        var result = _converter.Convert(value, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that Convert returns a SolidColorBrush with the correct color for a normal Color value.
    /// </summary>
    [Fact]
    public void Convert_NormalColor_ReturnsSolidColorBrush()
    {
        // Arrange
        var color = Color.FromArgb(255, 100, 150, 200);

        // Act
        var result = _converter.Convert(color, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result!;
        Assert.Equal(color, brush.Color);
    }

    /// <summary>
    /// Tests that Convert returns a black brush when given OxyPlot's automatic color (ARGB 0,0,0,1).
    /// </summary>
    [Fact]
    public void Convert_AutomaticColor_ReturnsBlackBrush()
    {
        // Arrange - OxyPlot uses ARGB(0,0,0,1) to represent automatic color
        var automaticColor = Color.FromArgb(0, 0, 0, 1);

        // Act
        var result = _converter.Convert(automaticColor, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result!;
        Assert.Equal(Color.FromArgb(255, 0, 0, 0), brush.Color);
    }

    /// <summary>
    /// Tests that Convert returns a red brush when given the Red color.
    /// </summary>
    [Fact]
    public void Convert_RedColor_ReturnsRedBrush()
    {
        // Arrange
        var color = Colors.Red;

        // Act
        var result = _converter.Convert(color, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result!;
        Assert.Equal(Colors.Red, brush.Color);
    }

    /// <summary>
    /// Tests that Convert returns a brush with the correct color including transparency.
    /// </summary>
    [Fact]
    public void Convert_TransparentColor_ReturnsSolidColorBrush()
    {
        // Arrange
        var color = Color.FromArgb(0, 255, 0, 0);

        // Act
        var result = _converter.Convert(color, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result!;
        Assert.Equal(color, brush.Color);
    }

    #endregion

    #region ConvertBack Tests

    /// <summary>
    /// Tests that ConvertBack returns the correct Color from a SolidColorBrush.
    /// </summary>
    [Fact]
    public void ConvertBack_SolidColorBrush_ReturnsColor()
    {
        // Arrange
        var brush = new SolidColorBrush(Colors.Blue);

        // Act
        var result = _converter.ConvertBack(brush, typeof(Color), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<Color>(result);
        Assert.Equal(Colors.Blue, (Color)result);
    }

    /// <summary>
    /// Tests that ConvertBack returns Red color from a red brush.
    /// </summary>
    [Fact]
    public void ConvertBack_RedBrush_ReturnsRedColor()
    {
        // Arrange
        var brush = new SolidColorBrush(Colors.Red);

        // Act
        var result = _converter.ConvertBack(brush, typeof(Color), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Colors.Red, (Color)result);
    }

    /// <summary>
    /// Tests that ConvertBack correctly preserves custom color values including alpha channel.
    /// </summary>
    [Fact]
    public void ConvertBack_CustomColor_ReturnsCorrectColor()
    {
        // Arrange
        var color = Color.FromArgb(128, 50, 100, 150);
        var brush = new SolidColorBrush(color);

        // Act
        var result = _converter.ConvertBack(brush, typeof(Color), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(color, (Color)result);
    }

    #endregion
}
