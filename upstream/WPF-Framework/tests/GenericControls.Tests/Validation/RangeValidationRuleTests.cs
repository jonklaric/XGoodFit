using System.Globalization;
using System.Windows.Controls;
using Xunit;

namespace GenericControls.Tests.Validation;

/// <summary>
/// Unit tests for the <see cref="RangeValidationRule"/> class.
/// </summary>
public class RangeValidationRuleTests
{
    #region Constructor and Setup

    /// <summary>
    /// Creates a RangeValidationRule instance with the specified min and max values.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <param name="boundsAreExclusive">Whether the bounds are exclusive.</param>
    /// <returns>A configured RangeValidationRule instance.</returns>
    private static RangeValidationRule CreateRule(double min, double max, bool boundsAreExclusive = false)
    {
        return new RangeValidationRule
        {
            Wrapper = new RangeWrapper
            {
                Minimum = min,
                Maximum = max,
                BoundsAreExclusive = boundsAreExclusive
            }
        };
    }

    #endregion

    #region Validate with values in range (inclusive bounds)

    /// <summary>
    /// Tests that Validate returns valid result for values within range.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    [Theory]
    [InlineData("5", 0, 10)]
    [InlineData("0", 0, 100)]
    [InlineData("100", 0, 100)]
    [InlineData("50", 0, 100)]
    [InlineData("-5", -10, 10)]
    [InlineData("0", -10, 10)]
    public void Validate_ValueInRange_ReturnsValidResult(string value, double min, double max)
    {
        // Arrange
        var rule = CreateRule(min, max);

        // Act
        var result = rule.Validate(value, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(ValidationResult.ValidResult, result);
    }

    /// <summary>
    /// Tests that Validate returns valid result for values at boundary with inclusive bounds.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    [Theory]
    [InlineData("0", 0, 10)]
    [InlineData("10", 0, 10)]
    [InlineData("-100", -100, 100)]
    [InlineData("100", -100, 100)]
    public void Validate_ValueAtBoundary_WithInclusiveBounds_ReturnsValidResult(string value, double min, double max)
    {
        // Arrange
        var rule = CreateRule(min, max, boundsAreExclusive: false);

        // Act
        var result = rule.Validate(value, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Validate with values outside range

    /// <summary>
    /// Tests that Validate returns invalid result for values outside range.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    [Theory]
    [InlineData("-1", 0, 10)]
    [InlineData("11", 0, 10)]
    [InlineData("101", 0, 100)]
    [InlineData("-101", -100, 100)]
    [InlineData("1000", 0, 100)]
    public void Validate_ValueOutsideRange_ReturnsInvalidResult(string value, double min, double max)
    {
        // Arrange
        var rule = CreateRule(min, max);

        // Act
        var result = rule.Validate(value, CultureInfo.InvariantCulture);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorContent);
    }

    /// <summary>
    /// Tests that Validate returns appropriate error message for values below minimum.
    /// </summary>
    [Fact]
    public void Validate_ValueBelowMinimum_ReturnsErrorMessage()
    {
        // Arrange
        var rule = CreateRule(0, 100);

        // Act
        var result = rule.Validate("-5", CultureInfo.InvariantCulture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("greater than or equal to", result.ErrorContent?.ToString());
    }

    /// <summary>
    /// Tests that Validate returns appropriate error message for values above maximum.
    /// </summary>
    [Fact]
    public void Validate_ValueAboveMaximum_ReturnsErrorMessage()
    {
        // Arrange
        var rule = CreateRule(0, 100);

        // Act
        var result = rule.Validate("150", CultureInfo.InvariantCulture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("less than or equal to", result.ErrorContent?.ToString());
    }

    #endregion

    #region Validate with exclusive bounds

    /// <summary>
    /// Tests that Validate returns valid result for values strictly within range with exclusive bounds.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    [Theory]
    [InlineData("5", 0, 10)]
    [InlineData("1", 0, 10)]
    [InlineData("9", 0, 10)]
    [InlineData("-5", -10, 10)]
    public void Validate_ValueStrictlyInRange_WithExclusiveBounds_ReturnsValidResult(string value, double min, double max)
    {
        // Arrange
        var rule = CreateRule(min, max, boundsAreExclusive: true);

        // Act
        var result = rule.Validate(value, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Tests that Validate returns invalid result for values at boundary with exclusive bounds.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    [Theory]
    [InlineData("0", 0, 10)]
    [InlineData("10", 0, 10)]
    [InlineData("-100", -100, 100)]
    [InlineData("100", -100, 100)]
    public void Validate_ValueAtBoundary_WithExclusiveBounds_ReturnsInvalidResult(string value, double min, double max)
    {
        // Arrange
        var rule = CreateRule(min, max, boundsAreExclusive: true);

        // Act
        var result = rule.Validate(value, CultureInfo.InvariantCulture);

        // Assert
        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Tests that Validate returns correct error message for value at min boundary with exclusive bounds.
    /// </summary>
    [Fact]
    public void Validate_ValueAtMinBoundary_WithExclusiveBounds_ReturnsCorrectErrorMessage()
    {
        // Arrange
        var rule = CreateRule(0, 100, boundsAreExclusive: true);

        // Act
        var result = rule.Validate("0", CultureInfo.InvariantCulture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("greater than", result.ErrorContent?.ToString());
        Assert.Contains("less than", result.ErrorContent?.ToString());
    }

    /// <summary>
    /// Tests that Validate returns correct error message for value at max boundary with exclusive bounds.
    /// </summary>
    [Fact]
    public void Validate_ValueAtMaxBoundary_WithExclusiveBounds_ReturnsCorrectErrorMessage()
    {
        // Arrange
        var rule = CreateRule(0, 100, boundsAreExclusive: true);

        // Act
        var result = rule.Validate("100", CultureInfo.InvariantCulture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("greater than", result.ErrorContent?.ToString());
        Assert.Contains("less than", result.ErrorContent?.ToString());
    }

    #endregion

    #region Validate with different numeric types

    /// <summary>
    /// Tests that Validate handles integer and double values correctly.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    [Theory]
    [InlineData("5")]
    [InlineData("5.0")]
    [InlineData("5.5")]
    [InlineData("5.123456789")]
    public void Validate_IntegerAndDoubleValues_HandledCorrectly(string value)
    {
        // Arrange
        var rule = CreateRule(0, 10);

        // Act
        var result = rule.Validate(value, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Tests that Validate handles scientific notation correctly.
    /// </summary>
    /// <param name="value">The value in scientific notation.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    [Theory]
    [InlineData("1e2", 0, 200)]     // 100 in scientific notation
    [InlineData("1.5e1", 0, 20)]    // 15 in scientific notation
    [InlineData("-1e1", -20, 0)]    // -10 in scientific notation
    public void Validate_ScientificNotation_HandledCorrectly(string value, double min, double max)
    {
        // Arrange
        var rule = CreateRule(min, max);

        // Act
        var result = rule.Validate(value, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Tests that Validate handles small double values correctly.
    /// </summary>
    /// <param name="value">The small double value to validate.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    [Theory]
    [InlineData("0.001", 0, 1)]
    [InlineData("0.999", 0, 1)]
    [InlineData("0.5", 0, 1)]
    public void Validate_SmallDoubleValues_HandledCorrectly(string value, double min, double max)
    {
        // Arrange
        var rule = CreateRule(min, max);

        // Act
        var result = rule.Validate(value, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Tests that Validate handles negative double values correctly.
    /// </summary>
    /// <param name="value">The negative double value to validate.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    [Theory]
    [InlineData("-0.5", -1, 0)]
    [InlineData("-0.001", -1, 0)]
    [InlineData("-0.999", -1, 0)]
    public void Validate_NegativeDoubleValues_HandledCorrectly(string value, double min, double max)
    {
        // Arrange
        var rule = CreateRule(min, max);

        // Act
        var result = rule.Validate(value, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Validate with null and empty values

    /// <summary>
    /// Tests that Validate returns valid result for null value.
    /// </summary>
    [Fact]
    public void Validate_NullValue_ReturnsValidResult()
    {
        // Arrange
        var rule = CreateRule(0, 100);

        // Act
        var result = rule.Validate(null, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Tests that Validate returns valid result for empty string.
    /// </summary>
    [Fact]
    public void Validate_EmptyString_ReturnsValidResult()
    {
        // Arrange
        var rule = CreateRule(0, 100);

        // Act
        var result = rule.Validate("", CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Tests that Validate treats whitespace string as invalid number format.
    /// </summary>
    [Fact]
    public void Validate_WhitespaceString_ReturnsValidResult()
    {
        // Arrange
        var rule = CreateRule(0, 100);

        // Act
        var result = rule.Validate("   ", CultureInfo.InvariantCulture);

        // Assert - whitespace is treated as invalid number format
        Assert.False(result.IsValid);
    }

    #endregion

    #region Validate with invalid input

    /// <summary>
    /// Tests that Validate returns invalid result for invalid number format.
    /// </summary>
    /// <param name="value">The invalid value to validate.</param>
    [Theory]
    [InlineData("abc")]
    [InlineData("12abc")]
    [InlineData("abc12")]
    [InlineData("!@#$")]
    public void Validate_InvalidNumberFormat_ReturnsInvalidResult(string value)
    {
        // Arrange
        var rule = CreateRule(0, 100);

        // Act
        var result = rule.Validate(value, CultureInfo.InvariantCulture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Invalid number format", result.ErrorContent?.ToString());
    }

    #endregion

    #region RangeWrapper Tests

    /// <summary>
    /// Tests that RangeWrapper has correct default values.
    /// </summary>
    [Fact]
    public void RangeWrapper_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var wrapper = new RangeWrapper();

        // Assert
        Assert.Equal(double.MaxValue, wrapper.Maximum);
        Assert.Equal(double.MinValue, wrapper.Minimum);
        Assert.False(wrapper.BoundsAreExclusive);
    }

    /// <summary>
    /// Tests that RangeWrapper Minimum property can be set.
    /// </summary>
    [Fact]
    public void RangeWrapper_SetMinimum_UpdatesProperty()
    {
        // Arrange
        var wrapper = new RangeWrapper();

        // Act
        wrapper.Minimum = 10;

        // Assert
        Assert.Equal(10, wrapper.Minimum);
    }

    /// <summary>
    /// Tests that RangeWrapper Maximum property can be set.
    /// </summary>
    [Fact]
    public void RangeWrapper_SetMaximum_UpdatesProperty()
    {
        // Arrange
        var wrapper = new RangeWrapper();

        // Act
        wrapper.Maximum = 100;

        // Assert
        Assert.Equal(100, wrapper.Maximum);
    }

    /// <summary>
    /// Tests that RangeWrapper BoundsAreExclusive property can be set.
    /// </summary>
    [Fact]
    public void RangeWrapper_SetBoundsAreExclusive_UpdatesProperty()
    {
        // Arrange
        var wrapper = new RangeWrapper();

        // Act
        wrapper.BoundsAreExclusive = true;

        // Assert
        Assert.True(wrapper.BoundsAreExclusive);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Tests that Validate handles very large numbers within range correctly.
    /// </summary>
    [Fact]
    public void Validate_VeryLargeNumber_InRange_ReturnsValidResult()
    {
        // Arrange
        var rule = CreateRule(0, double.MaxValue);

        // Act
        var result = rule.Validate("1e308", CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Tests that Validate handles very small numbers within range correctly.
    /// </summary>
    [Fact]
    public void Validate_VerySmallNumber_InRange_ReturnsValidResult()
    {
        // Arrange
        var rule = CreateRule(double.MinValue, 0);

        // Act
        var result = rule.Validate("-1e308", CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Tests that Validate returns invalid result for zero in negative range.
    /// </summary>
    [Fact]
    public void Validate_ZeroInNegativeRange_ReturnsInvalidResult()
    {
        // Arrange
        var rule = CreateRule(-100, -1);

        // Act
        var result = rule.Validate("0", CultureInfo.InvariantCulture);

        // Assert
        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Tests that Validate returns invalid result for zero in positive range.
    /// </summary>
    [Fact]
    public void Validate_ZeroInPositiveRange_ReturnsInvalidResult()
    {
        // Arrange
        var rule = CreateRule(1, 100);

        // Act
        var result = rule.Validate("0", CultureInfo.InvariantCulture);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion
}
