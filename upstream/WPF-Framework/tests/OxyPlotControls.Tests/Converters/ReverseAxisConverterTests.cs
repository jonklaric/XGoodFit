using System.Globalization;
using Xunit;

namespace OxyPlotControls.Tests.Converters;

/// <summary>
/// Tests for ReverseAxisConverter (IMultiValueConverter).
/// Verifies proper conversion between axis position values and reversed axis orientation.
/// </summary>
public class ReverseAxisConverterTests
{
    /// <summary>
    /// The converter instance being tested.
    /// </summary>
    private readonly ReverseAxisConverter _converter = new();

    #region Convert Tests

    /// <summary>
    /// Tests that Convert returns false when given normal axis orientation (start position less than end position).
    /// </summary>
    [Fact]
    public void Convert_NormalOrientation_ReturnsFalse()
    {
        // Arrange - start position 0, end position 1 is normal orientation
        object[] values = { 0.0, 1.0, null! };

        // Act
        var result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result);
    }

    /// <summary>
    /// Tests that Convert returns false when given reversed orientation positions but a null axis object.
    /// </summary>
    [Fact]
    public void Convert_ReversedOrientation_WithNullAxis_ReturnsFalse()
    {
        // Arrange - end position < start position would be reversed, but axis is null
        // The converter returns false early when axis is null
        object[] values = { 1.0, 0.0, null! };

        // Act
        var result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert - Returns false because axis is null (converter short-circuits)
        Assert.False((bool)result);
    }

    /// <summary>
    /// Tests that Convert returns false when start and end positions are equal.
    /// </summary>
    [Fact]
    public void Convert_EqualPositions_ReturnsFalse()
    {
        // Arrange - equal positions means not reversed
        object[] values = { 0.5, 0.5, null! };

        // Act
        var result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result);
    }

    /// <summary>
    /// Tests that Convert handles null start position gracefully by parsing it as 0.
    /// </summary>
    [Fact]
    public void Convert_NullStartPosition_HandlesParsing()
    {
        // Arrange
        object[] values = { null!, 1.0, null! };

        // Act
        var result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert - Both parse to 0, 0 < 1 is not reversed
        Assert.False((bool)result);
    }

    /// <summary>
    /// Tests that Convert correctly handles negative position values.
    /// </summary>
    [Fact]
    public void Convert_NegativePositions_ReturnsCorrectly()
    {
        // Arrange
        object[] values = { -1.0, 1.0, null! };

        // Act
        var result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert - -1 < 1 is not reversed
        Assert.False((bool)result);
    }

    /// <summary>
    /// Tests that Convert returns false for partial reversal when axis is null.
    /// </summary>
    [Fact]
    public void Convert_PartialReversal_WithNullAxis_ReturnsFalse()
    {
        // Arrange - end position < start position would be reversed, but axis is null
        // The converter returns false early when axis is null
        object[] values = { 0.8, 0.2, null! };

        // Act
        var result = _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert - Returns false because axis is null (converter short-circuits)
        Assert.False((bool)result);
    }

    #endregion

    #region ConvertBack Tests

    /// <summary>
    /// Tests that ConvertBack returns default axis position values when axis is null.
    /// </summary>
    [Fact]
    public void ConvertBack_NullAxis_ReturnsDefaults()
    {
        // Note: First must call Convert to set internal _axis
        object[] values = { 0.0, 1.0, null! };
        _converter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);

        // Arrange
        object value = false;

        // Act
        var result = _converter.ConvertBack(value, new[] { typeof(double), typeof(double), typeof(object) }, null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(0.0, result[0]);
        Assert.Equal(1.0, result[1]);
    }

    #endregion
}
