using System.Windows;
using OxyPlot.Wpf.Serialization;
using Xunit;

namespace OxyPlotControls.Tests.Extensions;

/// <summary>
/// Tests for WPF Vector extension methods.
/// </summary>
public class VectorExtensionsTests
{
    [Fact]
    public void ToPrettyText_SimpleValues_ReturnsFormattedString()
    {
        // Arrange
        var vector = new Vector(5.5, 10.5);

        // Act
        var result = vector.ToPrettyText();

        // Assert
        Assert.Equal("5.5, 10.5", result);
    }

    [Fact]
    public void ToPrettyText_ZeroValues_ReturnsFormattedString()
    {
        // Arrange
        var vector = new Vector(0, 0);

        // Act
        var result = vector.ToPrettyText();

        // Assert
        Assert.Equal("0, 0", result);
    }

    [Fact]
    public void ToPrettyText_NegativeValues_ReturnsFormattedString()
    {
        // Arrange
        var vector = new Vector(-5.5, -10.5);

        // Act
        var result = vector.ToPrettyText();

        // Assert
        Assert.Equal("-5.5, -10.5", result);
    }

    [Fact]
    public void FromPrettyVectorString_ValidString_ReturnsVector()
    {
        // Arrange
        var text = "5.5, 10.5";

        // Act
        var result = text.FromPrettyVectorString();

        // Assert
        Assert.Equal(5.5, result.X);
        Assert.Equal(10.5, result.Y);
    }

    [Fact]
    public void FromPrettyVectorString_ZeroValues_ReturnsVector()
    {
        // Arrange
        var text = "0, 0";

        // Act
        var result = text.FromPrettyVectorString();

        // Assert
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void FromPrettyVectorString_InvalidFormat_ReturnsDefault()
    {
        // Arrange
        var text = "invalid";

        // Act
        var result = text.FromPrettyVectorString();

        // Assert
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void FromPrettyVectorString_InvalidXValue_ReturnsDefault()
    {
        // Arrange
        var text = "abc, 10.5";

        // Act
        var result = text.FromPrettyVectorString();

        // Assert
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void FromPrettyVectorString_InvalidYValue_ReturnsDefault()
    {
        // Arrange
        var text = "5.5, xyz";

        // Act
        var result = text.FromPrettyVectorString();

        // Assert
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void ToPrettyText_FromPrettyVectorString_RoundTrip()
    {
        // Arrange
        var original = new Vector(123.456, 789.012);

        // Act
        var text = original.ToPrettyText();
        var result = text.FromPrettyVectorString();

        // Assert
        Assert.Equal(original.X, result.X, 10);
        Assert.Equal(original.Y, result.Y, 10);
    }
}
