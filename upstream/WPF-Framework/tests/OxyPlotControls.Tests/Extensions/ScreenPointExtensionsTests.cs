using OxyPlot;
using OxyPlot.Wpf.Serialization;
using Xunit;

namespace OxyPlotControls.Tests.Extensions;

/// <summary>
/// Tests for ScreenPoint extension methods.
/// </summary>
public class ScreenPointExtensionsTests
{
    [Fact]
    public void ToPrettyText_SimpleValues_ReturnsFormattedString()
    {
        // Arrange
        var point = new ScreenPoint(100.5, 200.5);

        // Act
        var result = point.ToPrettyText();

        // Assert
        Assert.Equal("100.5, 200.5", result);
    }

    [Fact]
    public void ToPrettyText_ZeroValues_ReturnsFormattedString()
    {
        // Arrange
        var point = new ScreenPoint(0, 0);

        // Act
        var result = point.ToPrettyText();

        // Assert
        Assert.Equal("0, 0", result);
    }

    [Fact]
    public void ToPrettyText_NegativeValues_ReturnsFormattedString()
    {
        // Arrange
        var point = new ScreenPoint(-100.5, -200.5);

        // Act
        var result = point.ToPrettyText();

        // Assert
        Assert.Equal("-100.5, -200.5", result);
    }

    [Fact]
    public void FromPrettyScreenText_ValidString_ReturnsScreenPoint()
    {
        // Arrange
        var text = "100.5, 200.5";

        // Act
        var result = text.FromPrettyScreenText();

        // Assert
        Assert.Equal(100.5, result.X);
        Assert.Equal(200.5, result.Y);
    }

    [Fact]
    public void FromPrettyScreenText_ZeroValues_ReturnsScreenPoint()
    {
        // Arrange
        var text = "0, 0";

        // Act
        var result = text.FromPrettyScreenText();

        // Assert
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void FromPrettyScreenText_InvalidFormat_ReturnsUndefined()
    {
        // Arrange
        var text = "invalid";

        // Act
        var result = text.FromPrettyScreenText();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void FromPrettyScreenText_InvalidXValue_ReturnsUndefined()
    {
        // Arrange
        var text = "abc, 200.5";

        // Act
        var result = text.FromPrettyScreenText();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void FromPrettyScreenText_InvalidYValue_ReturnsUndefined()
    {
        // Arrange
        var text = "100.5, xyz";

        // Act
        var result = text.FromPrettyScreenText();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void ToPrettyText_FromPrettyScreenText_RoundTrip()
    {
        // Arrange
        var original = new ScreenPoint(123.456, 789.012);

        // Act
        var text = original.ToPrettyText();
        var result = text.FromPrettyScreenText();

        // Assert
        Assert.Equal(original.X, result.X, 10);
        Assert.Equal(original.Y, result.Y, 10);
    }
}
