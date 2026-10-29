using OxyPlot;
using OxyPlot.Wpf.Serialization;
using Xunit;

namespace OxyPlotControls.Tests.Extensions;

/// <summary>
/// Tests for ScreenVector extension methods.
/// </summary>
public class ScreenVectorExtensionsTests
{
    [Fact]
    public void ToPrettyText_SimpleValues_ReturnsFormattedString()
    {
        // Arrange
        var vector = new ScreenVector(10.5, 20.5);

        // Act
        var result = vector.ToPrettyText();

        // Assert
        Assert.Equal("10.5, 20.5", result);
    }

    [Fact]
    public void ToPrettyText_ZeroValues_ReturnsFormattedString()
    {
        // Arrange
        var vector = new ScreenVector(0, 0);

        // Act
        var result = vector.ToPrettyText();

        // Assert
        Assert.Equal("0, 0", result);
    }

    [Fact]
    public void ToPrettyText_NegativeValues_ReturnsFormattedString()
    {
        // Arrange
        var vector = new ScreenVector(-10.5, -20.5);

        // Act
        var result = vector.ToPrettyText();

        // Assert
        Assert.Equal("-10.5, -20.5", result);
    }

    [Fact]
    public void FromPrettyVectorText_ValidString_ReturnsScreenVector()
    {
        // Arrange
        var text = "10.5, 20.5";

        // Act
        var result = text.FromPrettyVectorText();

        // Assert
        Assert.Equal(10.5, result.X);
        Assert.Equal(20.5, result.Y);
    }

    [Fact]
    public void FromPrettyVectorText_ZeroValues_ReturnsScreenVector()
    {
        // Arrange
        var text = "0, 0";

        // Act
        var result = text.FromPrettyVectorText();

        // Assert
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void FromPrettyVectorText_NegativeValues_ReturnsScreenVector()
    {
        // Arrange
        var text = "-10.5, -20.5";

        // Act
        var result = text.FromPrettyVectorText();

        // Assert
        Assert.Equal(-10.5, result.X);
        Assert.Equal(-20.5, result.Y);
    }

    [Fact]
    public void FromPrettyVectorText_InvalidFormat_ReturnsDefault()
    {
        // Arrange
        var text = "invalid";

        // Act
        var result = text.FromPrettyVectorText();

        // Assert
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void FromPrettyVectorText_InvalidXValue_ReturnsDefault()
    {
        // Arrange
        var text = "abc, 20.5";

        // Act
        var result = text.FromPrettyVectorText();

        // Assert
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void FromPrettyVectorText_InvalidYValue_ReturnsDefault()
    {
        // Arrange
        var text = "10.5, xyz";

        // Act
        var result = text.FromPrettyVectorText();

        // Assert
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void ToPrettyText_FromPrettyVectorText_RoundTrip()
    {
        // Arrange
        var original = new ScreenVector(123.456, 789.012);

        // Act
        var text = original.ToPrettyText();
        var result = text.FromPrettyVectorText();

        // Assert
        Assert.Equal(original.X, result.X, 10);
        Assert.Equal(original.Y, result.Y, 10);
    }
}
