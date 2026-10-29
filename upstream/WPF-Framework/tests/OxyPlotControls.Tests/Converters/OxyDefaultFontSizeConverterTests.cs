using System.Globalization;
using Xunit;

namespace OxyPlotControls.Tests.Converters;

/// <summary>
/// Tests for OxyDefaultFontSizeConverter.
/// Verifies proper conversion of font size values, with NaN/Infinity handling and default value (12.0) support.
/// </summary>
public class OxyDefaultFontSizeConverterTests
{
    /// <summary>
    /// The converter instance being tested.
    /// </summary>
    private readonly OxyDefaultFontSizeConverter _converter = new();

    #region Convert Tests

    /// <summary>
    /// Tests that Convert returns the default font size (12.0) when given a null value.
    /// </summary>
    [Fact]
    public void Convert_NullValue_ReturnsDefault12()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _converter.Convert(value!, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(12.0, result);
    }

    /// <summary>
    /// Tests that Convert returns the default font size (12.0) when given an incorrect type.
    /// </summary>
    [Fact]
    public void Convert_WrongType_ReturnsDefault12()
    {
        // Arrange
        var value = "not a double";

        // Act
        var result = _converter.Convert(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(12.0, result);
    }

    /// <summary>
    /// Tests that Convert returns the default font size (12.0) when given NaN.
    /// </summary>
    [Fact]
    public void Convert_NaNValue_ReturnsDefault12()
    {
        // Arrange
        var value = double.NaN;

        // Act
        var result = _converter.Convert(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(12.0, result);
    }

    /// <summary>
    /// Tests that Convert returns the default font size (12.0) when given positive infinity.
    /// </summary>
    [Fact]
    public void Convert_PositiveInfinity_ReturnsDefault12()
    {
        // Arrange
        var value = double.PositiveInfinity;

        // Act
        var result = _converter.Convert(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(12.0, result);
    }

    /// <summary>
    /// Tests that Convert returns the default font size (12.0) when given negative infinity.
    /// </summary>
    [Fact]
    public void Convert_NegativeInfinity_ReturnsDefault12()
    {
        // Arrange
        var value = double.NegativeInfinity;

        // Act
        var result = _converter.Convert(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(12.0, result);
    }

    /// <summary>
    /// Tests that Convert returns the font size value when given a valid font size.
    /// </summary>
    [Fact]
    public void Convert_ValidFontSize_ReturnsValue()
    {
        // Arrange
        var value = 14.5;

        // Act
        var result = _converter.Convert(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(14.5, result);
    }

    /// <summary>
    /// Tests that Convert returns zero when given a zero font size.
    /// </summary>
    [Fact]
    public void Convert_ZeroValue_ReturnsZero()
    {
        // Arrange
        var value = 0.0;

        // Act
        var result = _converter.Convert(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(0.0, result);
    }

    /// <summary>
    /// Tests that Convert returns the font size value when given a large font size.
    /// </summary>
    [Fact]
    public void Convert_LargeFontSize_ReturnsValue()
    {
        // Arrange
        var value = 72.0;

        // Act
        var result = _converter.Convert(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(72.0, result);
    }

    /// <summary>
    /// Tests that Convert returns the font size value when given a small font size.
    /// </summary>
    [Fact]
    public void Convert_SmallFontSize_ReturnsValue()
    {
        // Arrange
        var value = 8.0;

        // Act
        var result = _converter.Convert(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(8.0, result);
    }

    #endregion

    #region ConvertBack Tests

    /// <summary>
    /// Tests that ConvertBack returns NaN when given a null value.
    /// </summary>
    [Fact]
    public void ConvertBack_NullValue_ReturnsNaN()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _converter.ConvertBack(value!, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(double.IsNaN((double)result));
    }

    /// <summary>
    /// Tests that ConvertBack returns NaN when given an incorrect type.
    /// </summary>
    [Fact]
    public void ConvertBack_WrongType_ReturnsNaN()
    {
        // Arrange
        var value = "not a double";

        // Act
        var result = _converter.ConvertBack(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(double.IsNaN((double)result));
    }

    /// <summary>
    /// Tests that ConvertBack returns 12.0 when given the default font size value.
    /// A deliberately-set 12pt font size must not be cleared to NaN.
    /// </summary>
    [Fact]
    public void ConvertBack_DefaultValue12_ReturnsValue()
    {
        // Arrange
        var value = 12.0;

        // Act
        var result = _converter.ConvertBack(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(12.0, (double)result);
    }

    /// <summary>
    /// Tests that ConvertBack returns the value when given a non-default font size.
    /// </summary>
    [Fact]
    public void ConvertBack_NonDefaultValue_ReturnsValue()
    {
        // Arrange
        var value = 14.0;

        // Act
        var result = _converter.ConvertBack(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(14.0, (double)result);
    }

    /// <summary>
    /// Tests that ConvertBack returns zero when given a zero value.
    /// </summary>
    [Fact]
    public void ConvertBack_ZeroValue_ReturnsZero()
    {
        // Arrange
        var value = 0.0;

        // Act
        var result = _converter.ConvertBack(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(0.0, (double)result);
    }

    /// <summary>
    /// Tests that ConvertBack returns the value when given a large font size.
    /// </summary>
    [Fact]
    public void ConvertBack_LargeValue_ReturnsValue()
    {
        // Arrange
        var value = 72.0;

        // Act
        var result = _converter.ConvertBack(value, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(72.0, (double)result);
    }

    #endregion
}
