using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using OxyPlot.Wpf;
using OxyPlot.Wpf.Serialization;
using Xunit;

namespace OxyPlotControls.Tests.Serialization;

/// <summary>
/// Unit tests for GeneralPlotControl serialization functionality.
/// Tests round-trip serialization of general plot properties.
/// </summary>
public class GeneralPropertiesSerializationTests
{
    #region GeneralPropertiesTag Tests

    [StaFact]
    public void GeneralPropertiesTag_HasExpectedValue()
    {
        // Assert
        Assert.Equal("General", PlotSerializer.GeneralPropertiesTag);
    }

    #endregion

    #region Title Properties Round-Trip Tests

    [StaFact]
    public void GeneralPropertiesToXElement_TitleProperties_SerializesCorrectly()
    {
        // Arrange
        var plot = new Plot
        {
            Title = "Test Plot Title",
            TitleColor = Colors.Red,
            TitleFont = "Arial",
            TitleFontSize = 16,
            TitleFontWeight = FontWeights.Bold,
            TitlePadding = 10
        };

        // Act
        var element = PlotSerializer.GeneralPropertiesToXElement(plot);

        // Assert
        Assert.NotNull(element);
        Assert.Equal(PlotSerializer.GeneralPropertiesTag, element.Name.LocalName);

        var titleElement = element.Element("Title");
        Assert.NotNull(titleElement);
        Assert.Equal("Test Plot Title", titleElement.Attribute("Title")?.Value);
        Assert.Equal("Arial", titleElement.Attribute("TitleFont")?.Value);
        Assert.Equal("16", titleElement.Attribute("TitleFontSize")?.Value);
        Assert.Equal("10", titleElement.Attribute("TitlePadding")?.Value);
    }

    [StaFact]
    public void XElementToGeneralProperties_TitleProperties_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.GeneralPropertiesTag,
            new XAttribute("IsEnabled", "True"),
            new XElement("Title",
                new XAttribute("Title", "Deserialized Title"),
                new XAttribute("TitleColor", "#FFFF0000"),
                new XAttribute("TitleFont", "Verdana"),
                new XAttribute("TitleFontSize", "18"),
                new XAttribute("TitleFontWeight", "Bold"),
                new XAttribute("TitlePadding", "15")));

        // Act
        PlotSerializer.XElementToGeneralProperties(plot, element);

        // Assert
        Assert.Equal("Deserialized Title", plot.Title);
        Assert.Equal(Colors.Red, plot.TitleColor);
        Assert.Equal("Verdana", plot.TitleFont);
        Assert.Equal(18, plot.TitleFontSize);
        Assert.Equal(FontWeights.Bold, plot.TitleFontWeight);
        Assert.Equal(15, plot.TitlePadding);
    }

    [StaFact]
    public void TitleProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalPlot = new Plot
        {
            Title = "Round Trip Title",
            TitleColor = Colors.Blue,
            TitleFont = "Courier New",
            TitleFontSize = 20,
            TitleFontWeight = FontWeights.Light,
            TitlePadding = 25
        };

        // Act
        var element = PlotSerializer.GeneralPropertiesToXElement(originalPlot);
        var deserializedPlot = new Plot();
        PlotSerializer.XElementToGeneralProperties(deserializedPlot, element);

        // Assert
        Assert.Equal(originalPlot.Title, deserializedPlot.Title);
        Assert.Equal(originalPlot.TitleColor, deserializedPlot.TitleColor);
        Assert.Equal(originalPlot.TitleFont, deserializedPlot.TitleFont);
        Assert.Equal(originalPlot.TitleFontSize, deserializedPlot.TitleFontSize);
        Assert.Equal(originalPlot.TitleFontWeight, deserializedPlot.TitleFontWeight);
        Assert.Equal(originalPlot.TitlePadding, deserializedPlot.TitlePadding);
    }

    #endregion

    #region Subtitle Properties Round-Trip Tests

    [StaFact]
    public void GeneralPropertiesToXElement_SubtitleProperties_SerializesCorrectly()
    {
        // Arrange
        var plot = new Plot
        {
            Subtitle = "Test Subtitle",
            SubtitleColor = Colors.Green,
            SubtitleFont = "Times New Roman",
            SubtitleFontSize = 12,
            SubtitleFontWeight = FontWeights.Normal
        };

        // Act
        var element = PlotSerializer.GeneralPropertiesToXElement(plot);

        // Assert
        var subtitleElement = element.Element("Subtitle");
        Assert.NotNull(subtitleElement);
        Assert.Equal("Test Subtitle", subtitleElement.Attribute("Subtitle")?.Value);
        Assert.Equal("Times New Roman", subtitleElement.Attribute("SubtitleFont")?.Value);
        Assert.Equal("12", subtitleElement.Attribute("SubtitleFontSize")?.Value);
    }

    [StaFact]
    public void XElementToGeneralProperties_SubtitleProperties_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.GeneralPropertiesTag,
            new XAttribute("IsEnabled", "True"),
            new XElement("Subtitle",
                new XAttribute("Subtitle", "Deserialized Subtitle"),
                new XAttribute("SubtitleColor", "#FF008000"),
                new XAttribute("SubtitleFont", "Georgia"),
                new XAttribute("SubtitleFontSize", "14"),
                new XAttribute("SubtitleFontWeight", "Normal")));

        // Act
        PlotSerializer.XElementToGeneralProperties(plot, element);

        // Assert
        Assert.Equal("Deserialized Subtitle", plot.Subtitle);
        Assert.Equal(Colors.Green, plot.SubtitleColor);
        Assert.Equal("Georgia", plot.SubtitleFont);
        Assert.Equal(14, plot.SubtitleFontSize);
        Assert.Equal(FontWeights.Normal, plot.SubtitleFontWeight);
    }

    [StaFact]
    public void SubtitleProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalPlot = new Plot
        {
            Subtitle = "Round Trip Subtitle",
            SubtitleColor = Colors.Purple,
            SubtitleFont = "Tahoma",
            SubtitleFontSize = 11,
            SubtitleFontWeight = FontWeights.SemiBold
        };

        // Act
        var element = PlotSerializer.GeneralPropertiesToXElement(originalPlot);
        var deserializedPlot = new Plot();
        PlotSerializer.XElementToGeneralProperties(deserializedPlot, element);

        // Assert
        Assert.Equal(originalPlot.Subtitle, deserializedPlot.Subtitle);
        Assert.Equal(originalPlot.SubtitleColor, deserializedPlot.SubtitleColor);
        Assert.Equal(originalPlot.SubtitleFont, deserializedPlot.SubtitleFont);
        Assert.Equal(originalPlot.SubtitleFontSize, deserializedPlot.SubtitleFontSize);
        Assert.Equal(originalPlot.SubtitleFontWeight, deserializedPlot.SubtitleFontWeight);
    }

    #endregion

    #region Chart Area Properties Round-Trip Tests

    [StaFact]
    public void GeneralPropertiesToXElement_ChartAreaProperties_SerializesCorrectly()
    {
        // Arrange
        var plot = new Plot
        {
            Background = new SolidColorBrush(Colors.LightGray),
            BorderBrush = new SolidColorBrush(Colors.Black),
            BorderThickness = new Thickness(2)
        };

        // Act
        var element = PlotSerializer.GeneralPropertiesToXElement(plot);

        // Assert
        var chartElement = element.Element("Chart");
        Assert.NotNull(chartElement);
        Assert.NotNull(chartElement.Attribute("Background"));
        Assert.NotNull(chartElement.Attribute("BorderBrush"));
        Assert.Equal("2,2,2,2", chartElement.Attribute("BorderThickness")?.Value);
    }

    [StaFact]
    public void XElementToGeneralProperties_ChartAreaProperties_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.GeneralPropertiesTag,
            new XAttribute("IsEnabled", "True"),
            new XElement("Chart",
                new XAttribute("Background", "#FFD3D3D3"),
                new XAttribute("BorderBrush", "#FF000000"),
                new XAttribute("BorderThickness", "3,3,3,3")));

        // Act
        PlotSerializer.XElementToGeneralProperties(plot, element);

        // Assert
        Assert.IsType<SolidColorBrush>(plot.Background);
        var bgBrush = (SolidColorBrush)plot.Background;
        Assert.Equal(Colors.LightGray, bgBrush.Color);
        Assert.Equal(new Thickness(3), plot.BorderThickness);
    }

    [StaFact]
    public void ChartAreaProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalPlot = new Plot
        {
            Background = new SolidColorBrush(Colors.White),
            BorderBrush = new SolidColorBrush(Colors.DarkGray),
            BorderThickness = new Thickness(1, 2, 3, 4)
        };

        // Act
        var element = PlotSerializer.GeneralPropertiesToXElement(originalPlot);
        var deserializedPlot = new Plot();
        PlotSerializer.XElementToGeneralProperties(deserializedPlot, element);

        // Assert
        Assert.IsType<SolidColorBrush>(deserializedPlot.Background);
        var originalBg = (SolidColorBrush)originalPlot.Background;
        var deserializedBg = (SolidColorBrush)deserializedPlot.Background;
        Assert.Equal(originalBg.Color, deserializedBg.Color);
        Assert.Equal(originalPlot.BorderThickness, deserializedPlot.BorderThickness);
    }

    #endregion

    #region Plot Area Properties Round-Trip Tests

    [StaFact]
    public void GeneralPropertiesToXElement_PlotAreaProperties_SerializesCorrectly()
    {
        // Arrange
        var plot = new Plot
        {
            PlotAreaBackground = new SolidColorBrush(Colors.AliceBlue),
            PlotAreaBorderColor = Colors.Navy,
            PlotAreaBorderThickness = new Thickness(1, 1, 1, 1)
        };

        // Act
        var element = PlotSerializer.GeneralPropertiesToXElement(plot);

        // Assert
        var plotAreaElement = element.Element("Plot");
        Assert.NotNull(plotAreaElement);
        Assert.NotNull(plotAreaElement.Attribute("PlotAreaBackground"));
        Assert.Contains("000080", plotAreaElement.Attribute("PlotAreaBorderColor")?.Value);
    }

    [StaFact]
    public void XElementToGeneralProperties_PlotAreaProperties_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.GeneralPropertiesTag,
            new XAttribute("IsEnabled", "True"),
            new XElement("Plot",
                new XAttribute("PlotAreaBackground", "#FFF0F8FF"),
                new XAttribute("PlotAreaBorderColor", "#FF000080"),
                new XAttribute("PlotAreaBorderThickness", "2,2,2,2")));

        // Act
        PlotSerializer.XElementToGeneralProperties(plot, element);

        // Assert
        Assert.IsType<SolidColorBrush>(plot.PlotAreaBackground);
        Assert.Equal(Colors.Navy, plot.PlotAreaBorderColor);
        Assert.Equal(new Thickness(2), plot.PlotAreaBorderThickness);
    }

    [StaFact]
    public void PlotAreaProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalPlot = new Plot
        {
            PlotAreaBackground = new SolidColorBrush(Colors.Beige),
            PlotAreaBorderColor = Colors.Brown,
            PlotAreaBorderThickness = new Thickness(2, 2, 2, 2)
        };

        // Act
        var element = PlotSerializer.GeneralPropertiesToXElement(originalPlot);
        var deserializedPlot = new Plot();
        PlotSerializer.XElementToGeneralProperties(deserializedPlot, element);

        // Assert
        Assert.IsType<SolidColorBrush>(deserializedPlot.PlotAreaBackground);
        var originalBg = (SolidColorBrush)originalPlot.PlotAreaBackground;
        var deserializedBg = (SolidColorBrush)deserializedPlot.PlotAreaBackground;
        Assert.Equal(originalBg.Color, deserializedBg.Color);
        Assert.Equal(originalPlot.PlotAreaBorderColor, deserializedPlot.PlotAreaBorderColor);
        Assert.Equal(originalPlot.PlotAreaBorderThickness, deserializedPlot.PlotAreaBorderThickness);
    }

    #endregion

    #region IsEnabled Property Tests

    [StaFact]
    public void GeneralPropertiesToXElement_IsEnabled_SerializesCorrectly()
    {
        // Arrange
        var plotEnabled = new Plot { IsEnabled = true };
        var plotDisabled = new Plot { IsEnabled = false };

        // Act
        var elementEnabled = PlotSerializer.GeneralPropertiesToXElement(plotEnabled);
        var elementDisabled = PlotSerializer.GeneralPropertiesToXElement(plotDisabled);

        // Assert
        Assert.Equal("True", elementEnabled.Attribute("IsEnabled")?.Value);
        Assert.Equal("False", elementDisabled.Attribute("IsEnabled")?.Value);
    }

    [StaFact]
    public void XElementToGeneralProperties_IsEnabled_DeserializesCorrectly()
    {
        // Arrange
        var plotTrue = new Plot { IsEnabled = false };
        var plotFalse = new Plot { IsEnabled = true };
        var elementTrue = new XElement(PlotSerializer.GeneralPropertiesTag,
            new XAttribute("IsEnabled", "True"));
        var elementFalse = new XElement(PlotSerializer.GeneralPropertiesTag,
            new XAttribute("IsEnabled", "False"));

        // Act
        PlotSerializer.XElementToGeneralProperties(plotTrue, elementTrue);
        PlotSerializer.XElementToGeneralProperties(plotFalse, elementFalse);

        // Assert
        Assert.True(plotTrue.IsEnabled);
        Assert.False(plotFalse.IsEnabled);
    }

    #endregion

    #region Null and Edge Case Tests

    [StaFact]
    public void XElementToGeneralProperties_NullPlot_DoesNotThrow()
    {
        // Arrange
        Plot? plot = null;
        var element = new XElement(PlotSerializer.GeneralPropertiesTag);

        // Act & Assert
        var exception = Record.Exception(() => PlotSerializer.XElementToGeneralProperties(plot!, element));
        Assert.Null(exception);
    }

    [StaFact]
    public void XElementToGeneralProperties_WrongElementName_DoesNotDeserialize()
    {
        // Arrange
        var plot = new Plot { Title = "Original Title" };
        var element = new XElement("WrongName",
            new XElement("Title",
                new XAttribute("Title", "Changed Title")));

        // Act
        PlotSerializer.XElementToGeneralProperties(plot, element);

        // Assert
        Assert.Equal("Original Title", plot.Title);
    }

    [StaFact]
    public void XElementToGeneralProperties_MissingSubElements_HandlesGracefully()
    {
        // Arrange
        var plot = new Plot
        {
            Title = "Original Title",
            Subtitle = "Original Subtitle"
        };
        var element = new XElement(PlotSerializer.GeneralPropertiesTag,
            new XAttribute("IsEnabled", "True"));

        // Act
        var exception = Record.Exception(() => PlotSerializer.XElementToGeneralProperties(plot, element));

        // Assert
        Assert.Null(exception);
        // Original values should be preserved when sub-elements are missing
        Assert.Equal("Original Title", plot.Title);
        Assert.Equal("Original Subtitle", plot.Subtitle);
    }

    [StaFact]
    public void GeneralPropertiesToXElement_EmptyTitle_SerializesCorrectly()
    {
        // Arrange
        var plot = new Plot
        {
            Title = null!,
            Subtitle = null!
        };

        // Act
        var element = PlotSerializer.GeneralPropertiesToXElement(plot);

        // Assert
        Assert.NotNull(element);
        var titleElement = element.Element("Title");
        Assert.NotNull(titleElement);
    }

    #endregion

    #region Backward Compatibility Tests

    [StaFact]
    public void XElementToGeneralProperties_OldTitleFormat_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.GeneralPropertiesTag,
            new XAttribute("IsEnabled", "True"),
            new XElement("Title",
                new XAttribute("Title", "Test Title"),
                new XAttribute("Color", "#FFFF0000"),
                new XAttribute("Font", "Arial"),
                new XAttribute("Size", "14"),
                new XAttribute("Weight", "Bold"),
                new XAttribute("Padding", "8")));

        // Act
        PlotSerializer.XElementToGeneralProperties(plot, element);

        // Assert
        Assert.Equal("Test Title", plot.Title);
        Assert.Equal(Colors.Red, plot.TitleColor);
        Assert.Equal("Arial", plot.TitleFont);
        Assert.Equal(14, plot.TitleFontSize);
        Assert.Equal(FontWeights.Bold, plot.TitleFontWeight);
        Assert.Equal(8, plot.TitlePadding);
    }

    [StaFact]
    public void XElementToGeneralProperties_OldSubtitleFormat_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.GeneralPropertiesTag,
            new XAttribute("IsEnabled", "True"),
            new XElement("Subtitle",
                new XAttribute("Title", "Test Subtitle"),
                new XAttribute("Color", "#FF008000"),
                new XAttribute("Font", "Verdana"),
                new XAttribute("Size", "10"),
                new XAttribute("Weight", "Normal")));

        // Act
        PlotSerializer.XElementToGeneralProperties(plot, element);

        // Assert
        Assert.Equal("Test Subtitle", plot.Subtitle);
        Assert.Equal(Colors.Green, plot.SubtitleColor);
        Assert.Equal("Verdana", plot.SubtitleFont);
        Assert.Equal(10, plot.SubtitleFontSize);
        Assert.Equal(FontWeights.Normal, plot.SubtitleFontWeight);
    }

    [StaFact]
    public void XElementToGeneralProperties_OldPlotAreaFormat_DeserializesCorrectly()
    {
        // Arrange
        var plot = new Plot();
        var element = new XElement(PlotSerializer.GeneralPropertiesTag,
            new XAttribute("IsEnabled", "True"),
            new XElement("Plot",
                new XAttribute("BorderColor", "#FF800000"),
                new XAttribute("BorderThickness", "1,1,1,1")));

        // Act
        PlotSerializer.XElementToGeneralProperties(plot, element);

        // Assert
        Assert.Equal(Colors.Maroon, plot.PlotAreaBorderColor);
        Assert.Equal(new Thickness(1), plot.PlotAreaBorderThickness);
    }

    #endregion

    #region Complete Round-Trip Test

    [StaFact]
    public void AllGeneralProperties_CompleteRoundTrip_PreservesValues()
    {
        // Arrange
        var originalPlot = new Plot
        {
            IsEnabled = true,
            Title = "Complete Test Title",
            TitleColor = Colors.DarkBlue,
            TitleFont = "Segoe UI",
            TitleFontSize = 22,
            TitleFontWeight = FontWeights.Bold,
            TitlePadding = 12,
            Subtitle = "Complete Test Subtitle",
            SubtitleColor = Colors.DarkGreen,
            SubtitleFont = "Segoe UI",
            SubtitleFontSize = 14,
            SubtitleFontWeight = FontWeights.Regular,
            Background = new SolidColorBrush(Colors.WhiteSmoke),
            BorderBrush = new SolidColorBrush(Colors.Gray),
            BorderThickness = new Thickness(1, 2, 1, 2),
            PlotAreaBackground = new SolidColorBrush(Colors.White),
            PlotAreaBorderColor = Colors.Black,
            PlotAreaBorderThickness = new Thickness(1)
        };

        // Act
        var element = PlotSerializer.GeneralPropertiesToXElement(originalPlot);
        var deserializedPlot = new Plot();
        PlotSerializer.XElementToGeneralProperties(deserializedPlot, element);

        // Assert
        Assert.Equal(originalPlot.IsEnabled, deserializedPlot.IsEnabled);
        Assert.Equal(originalPlot.Title, deserializedPlot.Title);
        Assert.Equal(originalPlot.TitleColor, deserializedPlot.TitleColor);
        Assert.Equal(originalPlot.TitleFont, deserializedPlot.TitleFont);
        Assert.Equal(originalPlot.TitleFontSize, deserializedPlot.TitleFontSize);
        Assert.Equal(originalPlot.TitleFontWeight, deserializedPlot.TitleFontWeight);
        Assert.Equal(originalPlot.TitlePadding, deserializedPlot.TitlePadding);
        Assert.Equal(originalPlot.Subtitle, deserializedPlot.Subtitle);
        Assert.Equal(originalPlot.SubtitleColor, deserializedPlot.SubtitleColor);
        Assert.Equal(originalPlot.SubtitleFont, deserializedPlot.SubtitleFont);
        Assert.Equal(originalPlot.SubtitleFontSize, deserializedPlot.SubtitleFontSize);
        Assert.Equal(originalPlot.SubtitleFontWeight, deserializedPlot.SubtitleFontWeight);
        Assert.Equal(originalPlot.BorderThickness, deserializedPlot.BorderThickness);
        Assert.Equal(originalPlot.PlotAreaBorderColor, deserializedPlot.PlotAreaBorderColor);
        Assert.Equal(originalPlot.PlotAreaBorderThickness, deserializedPlot.PlotAreaBorderThickness);
    }

    #endregion
}

