using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using OxyPlot.Legends;
using OxyPlot.Wpf;
using OxyPlot.Wpf.Serialization;
using Xunit;

namespace OxyPlotControls.Tests.Serialization;

/// <summary>
/// Unit tests for LegendControl serialization functionality.
/// Tests round-trip serialization of legend properties.
/// </summary>
public class LegendSerializationTests
{
    #region LegendPropertiesTag Tests

    [StaFact]
    public void LegendPropertiesTag_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Legend", PlotSerializer.LegendPropertiesTag);
    }

    #endregion

    #region Legend Area Properties Round-Trip Tests

    [StaFact]
    public void LegendPropertiesToXElement_AreaProperties_SerializesCorrectly()
    {
        // Arrange
        var plot = new Plot
        {
            IsLegendVisible = true,
            LegendBackground = Colors.White,
            LegendBorder = Colors.Black,
            LegendBorderThickness = 1.5,
            LegendPadding = 8
        };

        // Act
        var element = PlotSerializer.LegendPropertiesToXElement(plot);

        // Assert
        Assert.NotNull(element);
        Assert.Equal(PlotSerializer.LegendPropertiesTag, element.Name.LocalName);

        var areaElement = element.Element("Area");
        Assert.NotNull(areaElement);
        Assert.Equal("true", areaElement.Attribute("IsLegendVisible")?.Value);
        Assert.Equal("1.5", areaElement.Attribute("LegendBorderThickness")?.Value);
        Assert.Equal("8", areaElement.Attribute("LegendPadding")?.Value);
    }

    [StaFact]
    public void XElementToLegendProperties_AreaProperties_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.LegendPropertiesTag,
            new XElement("Area",
                new XAttribute("IsLegendVisible", "True"),
                new XAttribute("LegendBackground", "#FFFFFFFF"),
                new XAttribute("LegendBorder", "#FF000000"),
                new XAttribute("LegendBorderThickness", "2"),
                new XAttribute("LegendPadding", "10")));

        // Act
        PlotSerializer.XElementToLegendProperties(plot, element);

        // Assert
        Assert.True(plot.IsLegendVisible);
        Assert.Equal(Colors.White, plot.LegendBackground);
        Assert.Equal(Colors.Black, plot.LegendBorder);
        Assert.Equal(2, plot.LegendBorderThickness);
        Assert.Equal(10, plot.LegendPadding);
    }

    [StaFact]
    public void AreaProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalPlot = new Plot
        {
            IsLegendVisible = false,
            LegendBackground = Colors.LightGray,
            LegendBorder = Colors.DarkGray,
            LegendBorderThickness = 2.5,
            LegendPadding = 12
        };

        // Act
        var element = PlotSerializer.LegendPropertiesToXElement(originalPlot);
        var deserializedPlot = new Plot();
        PlotSerializer.XElementToLegendProperties(deserializedPlot, element);

        // Assert
        Assert.Equal(originalPlot.IsLegendVisible, deserializedPlot.IsLegendVisible);
        Assert.Equal(originalPlot.LegendBackground, deserializedPlot.LegendBackground);
        Assert.Equal(originalPlot.LegendBorder, deserializedPlot.LegendBorder);
        Assert.Equal(originalPlot.LegendBorderThickness, deserializedPlot.LegendBorderThickness);
        Assert.Equal(originalPlot.LegendPadding, deserializedPlot.LegendPadding);
    }

    #endregion

    #region Legend Position Properties Round-Trip Tests

    [StaFact]
    public void LegendPropertiesToXElement_PositionProperties_SerializesCorrectly()
    {
        // Arrange
        var plot = new Plot
        {
            LegendPlacement = LegendPlacement.Inside,
            LegendPosition = LegendPosition.TopRight,
            LegendOrientation = LegendOrientation.Vertical
        };

        // Act
        var element = PlotSerializer.LegendPropertiesToXElement(plot);

        // Assert
        var positionElement = element.Element("Position");
        Assert.NotNull(positionElement);
        Assert.Equal("Inside", positionElement.Attribute("LegendPlacement")?.Value);
        Assert.Equal("TopRight", positionElement.Attribute("LegendPosition")?.Value);
        Assert.Equal("Vertical", positionElement.Attribute("LegendOrientation")?.Value);
    }

    [StaFact]
    public void XElementToLegendProperties_PositionProperties_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.LegendPropertiesTag,
            new XElement("Position",
                new XAttribute("LegendPlacement", "Outside"),
                new XAttribute("LegendPosition", "BottomLeft"),
                new XAttribute("LegendOrientation", "Horizontal")));

        // Act
        PlotSerializer.XElementToLegendProperties(plot, element);

        // Assert
        Assert.Equal(LegendPlacement.Outside, plot.LegendPlacement);
        Assert.Equal(LegendPosition.BottomLeft, plot.LegendPosition);
        Assert.Equal(LegendOrientation.Horizontal, plot.LegendOrientation);
    }

    [StaFact]
    public void PositionProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalPlot = new Plot
        {
            LegendPlacement = LegendPlacement.Outside,
            LegendPosition = LegendPosition.LeftTop,
            LegendOrientation = LegendOrientation.Horizontal
        };

        // Act
        var element = PlotSerializer.LegendPropertiesToXElement(originalPlot);
        var deserializedPlot = new Plot();
        PlotSerializer.XElementToLegendProperties(deserializedPlot, element);

        // Assert
        Assert.Equal(originalPlot.LegendPlacement, deserializedPlot.LegendPlacement);
        Assert.Equal(originalPlot.LegendPosition, deserializedPlot.LegendPosition);
        Assert.Equal(originalPlot.LegendOrientation, deserializedPlot.LegendOrientation);
    }

    [StaTheory]
    [InlineData(LegendPosition.TopLeft)]
    [InlineData(LegendPosition.TopCenter)]
    [InlineData(LegendPosition.TopRight)]
    [InlineData(LegendPosition.BottomLeft)]
    [InlineData(LegendPosition.BottomCenter)]
    [InlineData(LegendPosition.BottomRight)]
    [InlineData(LegendPosition.LeftTop)]
    [InlineData(LegendPosition.LeftMiddle)]
    [InlineData(LegendPosition.LeftBottom)]
    [InlineData(LegendPosition.RightTop)]
    [InlineData(LegendPosition.RightMiddle)]
    [InlineData(LegendPosition.RightBottom)]
    public void AllLegendPositions_RoundTrip_PreservesValues(LegendPosition position)
    {
        // Arrange
        var originalPlot = new Plot { LegendPosition = position };

        // Act
        var element = PlotSerializer.LegendPropertiesToXElement(originalPlot);
        var deserializedPlot = new Plot();
        PlotSerializer.XElementToLegendProperties(deserializedPlot, element);

        // Assert
        Assert.Equal(position, deserializedPlot.LegendPosition);
    }

    #endregion

    #region Legend Title Properties Round-Trip Tests

    [StaFact]
    public void LegendPropertiesToXElement_TitleProperties_SerializesCorrectly()
    {
        // Arrange
        var plot = new Plot
        {
            LegendTitle = "Test Legend",
            LegendTitleColor = Colors.Navy,
            LegendTitleFont = "Arial",
            LegendTitleFontSize = 14,
            LegendTitleFontWeight = FontWeights.Bold
        };

        // Act
        var element = PlotSerializer.LegendPropertiesToXElement(plot);

        // Assert
        var titleElement = element.Element("Title");
        Assert.NotNull(titleElement);
        Assert.Equal("Test Legend", titleElement.Attribute("LegendTitle")?.Value);
        Assert.Equal("Arial", titleElement.Attribute("LegendTitleFont")?.Value);
        Assert.Equal("14", titleElement.Attribute("LegendTitleFontSize")?.Value);
    }

    [StaFact]
    public void XElementToLegendProperties_TitleProperties_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.LegendPropertiesTag,
            new XElement("Title",
                new XAttribute("LegendTitle", "Deserialized Legend"),
                new XAttribute("LegendTitleColor", "#FF000080"),
                new XAttribute("LegendTitleFont", "Verdana"),
                new XAttribute("LegendTitleFontSize", "16"),
                new XAttribute("LegendTitleFontWeight", "Bold")));

        // Act
        PlotSerializer.XElementToLegendProperties(plot, element);

        // Assert
        Assert.Equal("Deserialized Legend", plot.LegendTitle);
        Assert.Equal(Colors.Navy, plot.LegendTitleColor);
        Assert.Equal("Verdana", plot.LegendTitleFont);
        Assert.Equal(16, plot.LegendTitleFontSize);
        Assert.Equal(FontWeights.Bold, plot.LegendTitleFontWeight);
    }

    [StaFact]
    public void TitleProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalPlot = new Plot
        {
            LegendTitle = "Round Trip Legend Title",
            LegendTitleColor = Colors.DarkBlue,
            LegendTitleFont = "Segoe UI",
            LegendTitleFontSize = 18,
            LegendTitleFontWeight = FontWeights.SemiBold
        };

        // Act
        var element = PlotSerializer.LegendPropertiesToXElement(originalPlot);
        var deserializedPlot = new Plot();
        PlotSerializer.XElementToLegendProperties(deserializedPlot, element);

        // Assert
        Assert.Equal(originalPlot.LegendTitle, deserializedPlot.LegendTitle);
        Assert.Equal(originalPlot.LegendTitleColor, deserializedPlot.LegendTitleColor);
        Assert.Equal(originalPlot.LegendTitleFont, deserializedPlot.LegendTitleFont);
        Assert.Equal(originalPlot.LegendTitleFontSize, deserializedPlot.LegendTitleFontSize);
        Assert.Equal(originalPlot.LegendTitleFontWeight, deserializedPlot.LegendTitleFontWeight);
    }

    #endregion

    #region Legend Item Properties Round-Trip Tests

    [StaFact]
    public void LegendPropertiesToXElement_ItemProperties_SerializesCorrectly()
    {
        // Arrange
        var plot = new Plot
        {
            LegendTextColor = Colors.Black,
            LegendSymbolLength = 25,
            LegendSymbolMargin = 5,
            LegendSymbolPlacement = LegendSymbolPlacement.Left,
            LegendColumnSpacing = 10,
            LegendItemAlignment = HorizontalAlignment.Left,
            LegendItemClickTogglesSeriesVisibility = true,
            LegendItemOrder = LegendItemOrder.Normal,
            LegendItemSpacing = 8,
            LegendLineSpacing = 4
        };

        // Act
        var element = PlotSerializer.LegendPropertiesToXElement(plot);

        // Assert
        var itemsElement = element.Element("Items");
        Assert.NotNull(itemsElement);
        Assert.Equal("25", itemsElement.Attribute("LegendSymbolLength")?.Value);
        Assert.Equal("5", itemsElement.Attribute("LegendSymbolMargin")?.Value);
        Assert.Equal("Left", itemsElement.Attribute("LegendSymbolPlacement")?.Value);
        Assert.Equal("10", itemsElement.Attribute("LegendColumnSpacing")?.Value);
        Assert.Equal("Left", itemsElement.Attribute("LegendItemAlignment")?.Value);
        Assert.Equal("true", itemsElement.Attribute("LegendItemClickTogglesSeriesVisibility")?.Value);
        Assert.Equal("Normal", itemsElement.Attribute("LegendItemOrder")?.Value);
        Assert.Equal("8", itemsElement.Attribute("LegendItemSpacing")?.Value);
        Assert.Equal("4", itemsElement.Attribute("LegendLineSpacing")?.Value);
    }

    [StaFact]
    public void XElementToLegendProperties_ItemProperties_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.LegendPropertiesTag,
            new XElement("Items",
                new XAttribute("LegendTextColor", "#FF000000"),
                new XAttribute("LegendSymbolLength", "30"),
                new XAttribute("LegendSymbolMargin", "6"),
                new XAttribute("LegendSymbolPlacement", "Right"),
                new XAttribute("LegendColumnSpacing", "12"),
                new XAttribute("LegendItemAlignment", "Center"),
                new XAttribute("LegendItemClickTogglesSeriesVisibility", "True"),
                new XAttribute("LegendItemOrder", "Reverse"),
                new XAttribute("LegendItemSpacing", "10"),
                new XAttribute("LegendLineSpacing", "5")));

        // Act
        PlotSerializer.XElementToLegendProperties(plot, element);

        // Assert
        Assert.Equal(Colors.Black, plot.LegendTextColor);
        Assert.Equal(30, plot.LegendSymbolLength);
        Assert.Equal(6, plot.LegendSymbolMargin);
        Assert.Equal(LegendSymbolPlacement.Right, plot.LegendSymbolPlacement);
        Assert.Equal(12, plot.LegendColumnSpacing);
        Assert.Equal(HorizontalAlignment.Center, plot.LegendItemAlignment);
        Assert.True(plot.LegendItemClickTogglesSeriesVisibility);
        Assert.Equal(LegendItemOrder.Reverse, plot.LegendItemOrder);
        Assert.Equal(10, plot.LegendItemSpacing);
        Assert.Equal(5, plot.LegendLineSpacing);
    }

    [StaFact]
    public void XElementToLegendProperties_ItemClickToggleMissing_DefaultsToFalse()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.LegendPropertiesTag,
            new XElement("Items",
                new XAttribute("LegendItemAlignment", "Left")));

        // Act
        PlotSerializer.XElementToLegendProperties(plot, element);

        // Assert
        Assert.False(plot.LegendItemClickTogglesSeriesVisibility);
    }

    [StaFact]
    public void ItemProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalPlot = new Plot
        {
            LegendTextColor = Colors.DarkGray,
            LegendSymbolLength = 35,
            LegendSymbolMargin = 7,
            LegendSymbolPlacement = LegendSymbolPlacement.Right,
            LegendColumnSpacing = 15,
            LegendItemAlignment = HorizontalAlignment.Right,
            LegendItemClickTogglesSeriesVisibility = true,
            LegendItemOrder = LegendItemOrder.Reverse,
            LegendItemSpacing = 12,
            LegendLineSpacing = 6
        };

        // Act
        var element = PlotSerializer.LegendPropertiesToXElement(originalPlot);
        var deserializedPlot = new Plot();
        PlotSerializer.XElementToLegendProperties(deserializedPlot, element);

        // Assert
        Assert.Equal(originalPlot.LegendTextColor, deserializedPlot.LegendTextColor);
        Assert.Equal(originalPlot.LegendSymbolLength, deserializedPlot.LegendSymbolLength);
        Assert.Equal(originalPlot.LegendSymbolMargin, deserializedPlot.LegendSymbolMargin);
        Assert.Equal(originalPlot.LegendSymbolPlacement, deserializedPlot.LegendSymbolPlacement);
        Assert.Equal(originalPlot.LegendColumnSpacing, deserializedPlot.LegendColumnSpacing);
        Assert.Equal(originalPlot.LegendItemAlignment, deserializedPlot.LegendItemAlignment);
        Assert.Equal(originalPlot.LegendItemClickTogglesSeriesVisibility, deserializedPlot.LegendItemClickTogglesSeriesVisibility);
        Assert.Equal(originalPlot.LegendItemOrder, deserializedPlot.LegendItemOrder);
        Assert.Equal(originalPlot.LegendItemSpacing, deserializedPlot.LegendItemSpacing);
        Assert.Equal(originalPlot.LegendLineSpacing, deserializedPlot.LegendLineSpacing);
    }

    #endregion

    #region Null and Edge Case Tests

    [StaFact]
    public void XElementToLegendProperties_NullPlot_DoesNotThrow()
    {
        // Arrange
        Plot? plot = null;
        var element = new XElement(PlotSerializer.LegendPropertiesTag);

        // Act & Assert
        var exception = Record.Exception(() => PlotSerializer.XElementToLegendProperties(plot!, element));
        Assert.Null(exception);
    }

    [StaFact]
    public void XElementToLegendProperties_WrongElementName_DoesNotDeserialize()
    {
        // Arrange
        var plot = new Plot { LegendTitle = "Original Legend" };
        var element = new XElement("WrongName",
            new XElement("Title",
                new XAttribute("LegendTitle", "Changed Legend")));

        // Act
        PlotSerializer.XElementToLegendProperties(plot, element);

        // Assert
        Assert.Equal("Original Legend", plot.LegendTitle);
    }

    [StaFact]
    public void XElementToLegendProperties_MissingSubElements_HandlesGracefully()
    {
        // Arrange
        var plot = new Plot
        {
            LegendTitle = "Original Legend",
            IsLegendVisible = true
        };
        var element = new XElement(PlotSerializer.LegendPropertiesTag);

        // Act
        var exception = Record.Exception(() => PlotSerializer.XElementToLegendProperties(plot, element));

        // Assert
        Assert.Null(exception);
        // Original values should be preserved when sub-elements are missing
        Assert.Equal("Original Legend", plot.LegendTitle);
        Assert.True(plot.IsLegendVisible);
    }

    [StaFact]
    public void LegendPropertiesToXElement_NullTitle_SerializesCorrectly()
    {
        // Arrange
        var plot = new Plot
        {
            LegendTitle = null!
        };

        // Act
        var element = PlotSerializer.LegendPropertiesToXElement(plot);

        // Assert
        Assert.NotNull(element);
        var titleElement = element.Element("Title");
        Assert.NotNull(titleElement);
    }

    #endregion

    #region Backward Compatibility Tests

    [StaFact]
    public void XElementToLegendProperties_OldAreaFormat_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.LegendPropertiesTag,
            new XElement("Area",
                new XAttribute("LegendVisible", "True"),
                new XAttribute("BackgroundColor", "#FFFFFFFF"),
                new XAttribute("BorderColor", "#FF000000"),
                new XAttribute("BorderThickness", "1"),
                new XAttribute("Padding", "5")));

        // Act
        PlotSerializer.XElementToLegendProperties(plot, element);

        // Assert
        Assert.True(plot.IsLegendVisible);
        Assert.Equal(Colors.White, plot.LegendBackground);
        Assert.Equal(Colors.Black, plot.LegendBorder);
        Assert.Equal(1, plot.LegendBorderThickness);
        Assert.Equal(5, plot.LegendPadding);
    }

    [StaFact]
    public void XElementToLegendProperties_OldPositionFormat_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.LegendPropertiesTag,
            new XElement("Position",
                new XAttribute("Placement", "Inside"),
                new XAttribute("Position", "TopLeft"),
                new XAttribute("Orientation", "Vertical")));

        // Act
        PlotSerializer.XElementToLegendProperties(plot, element);

        // Assert
        Assert.Equal(LegendPlacement.Inside, plot.LegendPlacement);
        Assert.Equal(LegendPosition.TopLeft, plot.LegendPosition);
        Assert.Equal(LegendOrientation.Vertical, plot.LegendOrientation);
    }

    [StaFact]
    public void XElementToLegendProperties_OldTitleFormat_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.LegendPropertiesTag,
            new XElement("Title",
                new XAttribute("Title", "Old Format Legend"),
                new XAttribute("Color", "#FF800000"),
                new XAttribute("Font", "Times New Roman"),
                new XAttribute("Size", "12"),
                new XAttribute("Weight", "Normal")));

        // Act
        PlotSerializer.XElementToLegendProperties(plot, element);

        // Assert
        Assert.Equal("Old Format Legend", plot.LegendTitle);
        Assert.Equal(Colors.Maroon, plot.LegendTitleColor);
        Assert.Equal("Times New Roman", plot.LegendTitleFont);
        Assert.Equal(12, plot.LegendTitleFontSize);
        Assert.Equal(FontWeights.Normal, plot.LegendTitleFontWeight);
    }

    [StaFact]
    public void XElementToLegendProperties_OldItemsFormat_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.LegendPropertiesTag,
            new XElement("Items",
                new XAttribute("Color", "#FF333333"),
                new XAttribute("SymbolLength", "20"),
                new XAttribute("SymbolMargin", "4"),
                new XAttribute("SymbolPlacement", "Left"),
                new XAttribute("ColumnSpacing", "8"),
                new XAttribute("ItemAlignment", "Left"),
                new XAttribute("ItemOrder", "Normal"),
                new XAttribute("ItemSpacing", "6"),
                new XAttribute("LineSpacing", "3")));

        // Act
        PlotSerializer.XElementToLegendProperties(plot, element);

        // Assert
        Assert.Equal(Color.FromRgb(0x33, 0x33, 0x33), plot.LegendTextColor);
        Assert.Equal(20, plot.LegendSymbolLength);
        Assert.Equal(4, plot.LegendSymbolMargin);
        Assert.Equal(LegendSymbolPlacement.Left, plot.LegendSymbolPlacement);
        Assert.Equal(8, plot.LegendColumnSpacing);
        Assert.Equal(HorizontalAlignment.Left, plot.LegendItemAlignment);
        Assert.Equal(LegendItemOrder.Normal, plot.LegendItemOrder);
        Assert.Equal(6, plot.LegendItemSpacing);
        Assert.Equal(3, plot.LegendLineSpacing);
    }

    #endregion

    #region Complete Round-Trip Test

    [StaFact]
    public void AllLegendProperties_CompleteRoundTrip_PreservesValues()
    {
        // Arrange
        var originalPlot = new Plot
        {
            IsLegendVisible = true,
            LegendBackground = Colors.AliceBlue,
            LegendBorder = Colors.SteelBlue,
            LegendBorderThickness = 1.5,
            LegendPadding = 10,
            LegendPlacement = LegendPlacement.Inside,
            LegendPosition = LegendPosition.TopRight,
            LegendOrientation = LegendOrientation.Vertical,
            LegendTitle = "Complete Legend Test",
            LegendTitleColor = Colors.DarkSlateBlue,
            LegendTitleFont = "Segoe UI",
            LegendTitleFontSize = 14,
            LegendTitleFontWeight = FontWeights.Bold,
            LegendTextColor = Colors.DimGray,
            LegendSymbolLength = 28,
            LegendSymbolMargin = 6,
            LegendSymbolPlacement = LegendSymbolPlacement.Left,
            LegendColumnSpacing = 12,
            LegendItemAlignment = HorizontalAlignment.Left,
            LegendItemClickTogglesSeriesVisibility = true,
            LegendItemOrder = LegendItemOrder.Normal,
            LegendItemSpacing = 8,
            LegendLineSpacing = 4
        };

        // Act
        var element = PlotSerializer.LegendPropertiesToXElement(originalPlot);
        var deserializedPlot = new Plot();
        PlotSerializer.XElementToLegendProperties(deserializedPlot, element);

        // Assert
        Assert.Equal(originalPlot.IsLegendVisible, deserializedPlot.IsLegendVisible);
        Assert.Equal(originalPlot.LegendBackground, deserializedPlot.LegendBackground);
        Assert.Equal(originalPlot.LegendBorder, deserializedPlot.LegendBorder);
        Assert.Equal(originalPlot.LegendBorderThickness, deserializedPlot.LegendBorderThickness);
        Assert.Equal(originalPlot.LegendPadding, deserializedPlot.LegendPadding);
        Assert.Equal(originalPlot.LegendPlacement, deserializedPlot.LegendPlacement);
        Assert.Equal(originalPlot.LegendPosition, deserializedPlot.LegendPosition);
        Assert.Equal(originalPlot.LegendOrientation, deserializedPlot.LegendOrientation);
        Assert.Equal(originalPlot.LegendTitle, deserializedPlot.LegendTitle);
        Assert.Equal(originalPlot.LegendTitleColor, deserializedPlot.LegendTitleColor);
        Assert.Equal(originalPlot.LegendTitleFont, deserializedPlot.LegendTitleFont);
        Assert.Equal(originalPlot.LegendTitleFontSize, deserializedPlot.LegendTitleFontSize);
        Assert.Equal(originalPlot.LegendTitleFontWeight, deserializedPlot.LegendTitleFontWeight);
        Assert.Equal(originalPlot.LegendTextColor, deserializedPlot.LegendTextColor);
        Assert.Equal(originalPlot.LegendSymbolLength, deserializedPlot.LegendSymbolLength);
        Assert.Equal(originalPlot.LegendSymbolMargin, deserializedPlot.LegendSymbolMargin);
        Assert.Equal(originalPlot.LegendSymbolPlacement, deserializedPlot.LegendSymbolPlacement);
        Assert.Equal(originalPlot.LegendColumnSpacing, deserializedPlot.LegendColumnSpacing);
        Assert.Equal(originalPlot.LegendItemAlignment, deserializedPlot.LegendItemAlignment);
        Assert.Equal(originalPlot.LegendItemClickTogglesSeriesVisibility, deserializedPlot.LegendItemClickTogglesSeriesVisibility);
        Assert.Equal(originalPlot.LegendItemOrder, deserializedPlot.LegendItemOrder);
        Assert.Equal(originalPlot.LegendItemSpacing, deserializedPlot.LegendItemSpacing);
        Assert.Equal(originalPlot.LegendLineSpacing, deserializedPlot.LegendLineSpacing);
    }

    #endregion
}
