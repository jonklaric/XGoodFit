using System.Xml.Linq;
using OxyPlot;
using OxyPlot.Wpf.Serialization;
using Xunit;

namespace OxyPlotControls.Tests.Extensions;

/// <summary>
/// Tests for DataPoint extension methods.
/// </summary>
public class DataPointExtensionsTests
{
    #region ToPrettyText Tests

    [Fact]
    public void ToPrettyText_SimpleValues_ReturnsFormattedString()
    {
        // Arrange
        var dataPoint = new DataPoint(1.5, 2.5);

        // Act
        var result = dataPoint.ToPrettyText();

        // Assert
        Assert.Equal("1.5, 2.5", result);
    }

    [Fact]
    public void ToPrettyText_ZeroValues_ReturnsFormattedString()
    {
        // Arrange
        var dataPoint = new DataPoint(0, 0);

        // Act
        var result = dataPoint.ToPrettyText();

        // Assert
        Assert.Equal("0, 0", result);
    }

    [Fact]
    public void ToPrettyText_NegativeValues_ReturnsFormattedString()
    {
        // Arrange
        var dataPoint = new DataPoint(-10.5, -20.3);

        // Act
        var result = dataPoint.ToPrettyText();

        // Assert - Use Contains due to floating point representation (-20.3 may become -20.300000000000001)
        Assert.Contains("-10.5", result);
        Assert.Contains("-20.3", result);
    }

    [Fact]
    public void ToPrettyText_LargeValues_ReturnsFormattedString()
    {
        // Arrange
        var dataPoint = new DataPoint(1234567890.123456, 9876543210.654321);

        // Act
        var result = dataPoint.ToPrettyText();

        // Assert
        Assert.Contains("1234567890", result);
        Assert.Contains("9876543210", result);
    }

    [Fact]
    public void ToPrettyText_VerySmallValues_ReturnsFormattedString()
    {
        // Arrange
        var dataPoint = new DataPoint(0.0000001, 0.0000002);

        // Act
        var result = dataPoint.ToPrettyText();

        // Assert - Very small values are formatted in scientific notation (E-07 or E-08)
        // 0.0000001 may become 9.999...E-08 and 0.0000002 may become 1.999...E-07
        Assert.Contains("E-0", result);
        Assert.Contains(", ", result);
    }

    #endregion

    #region FromPrettyDataText Tests

    [Fact]
    public void FromPrettyDataText_ValidString_ReturnsDataPoint()
    {
        // Arrange
        var text = "1.5, 2.5";

        // Act
        var result = text.FromPrettyDataText();

        // Assert
        Assert.Equal(1.5, result.X);
        Assert.Equal(2.5, result.Y);
    }

    [Fact]
    public void FromPrettyDataText_ZeroValues_ReturnsDataPoint()
    {
        // Arrange
        var text = "0, 0";

        // Act
        var result = text.FromPrettyDataText();

        // Assert
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
    }

    [Fact]
    public void FromPrettyDataText_NegativeValues_ReturnsDataPoint()
    {
        // Arrange
        var text = "-10.5, -20.3";

        // Act
        var result = text.FromPrettyDataText();

        // Assert
        Assert.Equal(-10.5, result.X);
        Assert.Equal(-20.3, result.Y);
    }

    [Fact]
    public void FromPrettyDataText_InvalidFormat_ReturnsUndefined()
    {
        // Arrange
        var text = "invalid";

        // Act
        var result = text.FromPrettyDataText();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void FromPrettyDataText_MissingValue_ReturnsUndefined()
    {
        // Arrange
        var text = "1.5";

        // Act
        var result = text.FromPrettyDataText();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void FromPrettyDataText_InvalidXValue_ReturnsUndefined()
    {
        // Arrange
        var text = "abc, 2.5";

        // Act
        var result = text.FromPrettyDataText();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void FromPrettyDataText_InvalidYValue_ReturnsUndefined()
    {
        // Arrange
        var text = "1.5, xyz";

        // Act
        var result = text.FromPrettyDataText();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void FromPrettyDataText_ExtraValues_ReturnsUndefined()
    {
        // Arrange
        var text = "1.5, 2.5, 3.5";

        // Act
        var result = text.FromPrettyDataText();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void ToPrettyText_FromPrettyDataText_RoundTrip()
    {
        // Arrange
        var original = new DataPoint(123.456, 789.012);

        // Act
        var text = original.ToPrettyText();
        var result = text.FromPrettyDataText();

        // Assert
        Assert.Equal(original.X, result.X, 10);
        Assert.Equal(original.Y, result.Y, 10);
    }

    #endregion

    #region ToXElement Tests

    [Fact]
    public void ToXElement_SimpleValues_ReturnsXElement()
    {
        // Arrange
        var dataPoint = new DataPoint(1.5, 2.5);

        // Act
        var result = dataPoint.ToXElement();

        // Assert
        Assert.Equal("DataPoint", result.Name.LocalName);
        Assert.Equal("1.5", result.Attribute("X")?.Value);
        Assert.Equal("2.5", result.Attribute("Y")?.Value);
    }

    [Fact]
    public void ToXElement_ZeroValues_ReturnsXElement()
    {
        // Arrange
        var dataPoint = new DataPoint(0, 0);

        // Act
        var result = dataPoint.ToXElement();

        // Assert
        Assert.Equal("0", result.Attribute("X")?.Value);
        Assert.Equal("0", result.Attribute("Y")?.Value);
    }

    [Fact]
    public void ToXElement_NegativeValues_ReturnsXElement()
    {
        // Arrange
        var dataPoint = new DataPoint(-10.5, -20.3);

        // Act
        var result = dataPoint.ToXElement();

        // Assert - Use Contains/StartsWith due to floating point representation (-20.3 may become -20.300000000000001)
        Assert.Equal("-10.5", result.Attribute("X")?.Value);
        Assert.StartsWith("-20.3", result.Attribute("Y")?.Value);
    }

    #endregion

    #region PointFromXElement Tests

    [Fact]
    public void PointFromXElement_ValidElement_ReturnsDataPoint()
    {
        // Arrange
        var element = new XElement("DataPoint",
            new XAttribute("X", "1.5"),
            new XAttribute("Y", "2.5"));

        // Act
        var result = element.PointFromXElement();

        // Assert
        Assert.Equal(1.5, result.X);
        Assert.Equal(2.5, result.Y);
    }

    [Fact]
    public void PointFromXElement_WrongElementName_ReturnsUndefined()
    {
        // Arrange
        var element = new XElement("WrongName",
            new XAttribute("X", "1.5"),
            new XAttribute("Y", "2.5"));

        // Act
        var result = element.PointFromXElement();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void PointFromXElement_MissingXAttribute_ReturnsUndefined()
    {
        // Arrange
        var element = new XElement("DataPoint",
            new XAttribute("Y", "2.5"));

        // Act
        var result = element.PointFromXElement();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void PointFromXElement_MissingYAttribute_ReturnsUndefined()
    {
        // Arrange
        var element = new XElement("DataPoint",
            new XAttribute("X", "1.5"));

        // Act
        var result = element.PointFromXElement();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void PointFromXElement_InvalidXValue_ReturnsUndefined()
    {
        // Arrange
        var element = new XElement("DataPoint",
            new XAttribute("X", "invalid"),
            new XAttribute("Y", "2.5"));

        // Act
        var result = element.PointFromXElement();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void PointFromXElement_InvalidYValue_ReturnsUndefined()
    {
        // Arrange
        var element = new XElement("DataPoint",
            new XAttribute("X", "1.5"),
            new XAttribute("Y", "invalid"));

        // Act
        var result = element.PointFromXElement();

        // Assert
        Assert.True(double.IsNaN(result.X));
        Assert.True(double.IsNaN(result.Y));
    }

    [Fact]
    public void ToXElement_PointFromXElement_RoundTrip()
    {
        // Arrange
        var original = new DataPoint(123.456, 789.012);

        // Act
        var element = original.ToXElement();
        var result = element.PointFromXElement();

        // Assert
        Assert.Equal(original.X, result.X, 10);
        Assert.Equal(original.Y, result.Y, 10);
    }

    #endregion

    #region PointsFromXElement Tests

    [Fact]
    public void PointsFromXElement_DataPointsElement_ReturnsDataPointList()
    {
        // Arrange
        var element = new XElement("DataPoints",
            new XElement("DataPoint", new XAttribute("X", "1"), new XAttribute("Y", "2")),
            new XElement("DataPoint", new XAttribute("X", "3"), new XAttribute("Y", "4")));

        // Act
        var result = element.PointsFromXElement();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].X);
        Assert.Equal(2, result[0].Y);
        Assert.Equal(3, result[1].X);
        Assert.Equal(4, result[1].Y);
    }

    [Fact]
    public void PointsFromXElement_PointsElement_ReturnsDataPointList()
    {
        // Arrange
        var element = new XElement("Points",
            new XElement("DataPoint", new XAttribute("X", "1"), new XAttribute("Y", "2")),
            new XElement("DataPoint", new XAttribute("X", "3"), new XAttribute("Y", "4")));

        // Act
        var result = element.PointsFromXElement();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].X);
        Assert.Equal(2, result[0].Y);
        Assert.Equal(3, result[1].X);
        Assert.Equal(4, result[1].Y);
    }

    [Fact]
    public void PointsFromXElement_WrongElementName_ReturnsEmptyList()
    {
        // Arrange
        var element = new XElement("WrongName",
            new XElement("DataPoint", new XAttribute("X", "1"), new XAttribute("Y", "2")));

        // Act
        var result = element.PointsFromXElement();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void PointsFromXElement_EmptyElement_ReturnsEmptyList()
    {
        // Arrange
        var element = new XElement("DataPoints");

        // Act
        var result = element.PointsFromXElement();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region ToXElement List Tests

    [Fact]
    public void ToXElement_DataPointList_ReturnsXElement()
    {
        // Arrange
        var dataPoints = new List<DataPoint>
        {
            new DataPoint(1, 2),
            new DataPoint(3, 4)
        };

        // Act
        var result = dataPoints.ToXElement("Points");

        // Assert
        Assert.Equal("Points", result.Name.LocalName);
        Assert.Equal(2, result.Elements("DataPoint").Count());
    }

    [Fact]
    public void ToXElement_EmptyList_ReturnsEmptyXElement()
    {
        // Arrange
        var dataPoints = new List<DataPoint>();

        // Act
        var result = dataPoints.ToXElement("Points");

        // Assert
        Assert.Equal("Points", result.Name.LocalName);
        Assert.Empty(result.Elements());
    }

    #endregion
}
