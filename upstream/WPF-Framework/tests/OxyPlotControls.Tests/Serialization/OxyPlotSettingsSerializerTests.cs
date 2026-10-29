using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using OxyPlot;
using OxyPlot.Wpf.Serialization;
using OxyPlotControls;
using Xunit;

namespace OxyPlotControls.Tests.Serialization;

/// <summary>
/// Unit tests for the OxyPlotSettingsSerializer class.
/// Tests all the Get*Attribute helper methods used for XML deserialization.
/// </summary>
public class OxyPlotSettingsSerializerTests
{
    #region GetColorAttribute Tests

    [Fact]
    public void GetColorAttribute_ValidColor_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Color", "#FF0000"));

        // Act
        bool result = PlotSerializer.GetColorAttribute(element, "Color", out Color color);

        // Assert
        Assert.True(result);
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void GetColorAttribute_ValidNamedColor_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Color", "Blue"));

        // Act
        bool result = PlotSerializer.GetColorAttribute(element, "Color", out Color color);

        // Assert
        Assert.True(result);
        Assert.Equal(Colors.Blue, color);
    }

    [Fact]
    public void GetColorAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");

        // Act
        bool result = PlotSerializer.GetColorAttribute(element, "Color", out Color color);

        // Assert
        Assert.False(result);
        Assert.Equal(default(Color), color);
    }

    [Fact]
    public void GetColorAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Color", ""));

        // Act
        bool result = PlotSerializer.GetColorAttribute(element, "Color", out Color color);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetColorAttribute_ArgbColor_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Color", "#80FF0000"));

        // Act
        bool result = PlotSerializer.GetColorAttribute(element, "Color", out Color color);

        // Assert
        Assert.True(result);
        Assert.Equal(128, color.A);
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    #endregion

    #region GetBrushAttribute Tests

    [Fact]
    public void GetBrushAttribute_ValidColor_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Brush", "#FF0000"));
        var converter = new BrushConverter();

        // Act
        bool result = PlotSerializer.GetBrushAttribute(element, "Brush", converter, out Brush? brush);

        // Assert
        Assert.True(result);
        Assert.NotNull(brush);
        Assert.IsType<SolidColorBrush>(brush);
        var solidBrush = (SolidColorBrush)brush;
        Assert.Equal(255, solidBrush.Color.R);
    }

    [Fact]
    public void GetBrushAttribute_NamedColor_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Brush", "Green"));
        var converter = new BrushConverter();

        // Act
        bool result = PlotSerializer.GetBrushAttribute(element, "Brush", converter, out Brush? brush);

        // Assert
        Assert.True(result);
        Assert.NotNull(brush);
    }

    [Fact]
    public void GetBrushAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");
        var converter = new BrushConverter();

        // Act
        bool result = PlotSerializer.GetBrushAttribute(element, "Brush", converter, out Brush? brush);

        // Assert
        Assert.False(result);
        Assert.Null(brush);
    }

    [Fact]
    public void GetBrushAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Brush", ""));
        var converter = new BrushConverter();

        // Act
        bool result = PlotSerializer.GetBrushAttribute(element, "Brush", converter, out Brush? brush);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetStringAttribute Tests

    [Fact]
    public void GetStringAttribute_ValidString_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Name", "TestValue"));

        // Act
        bool result = PlotSerializer.GetStringAttribute(element, "Name", out string? value);

        // Assert
        Assert.True(result);
        Assert.Equal("TestValue", value);
    }

    [Fact]
    public void GetStringAttribute_EmptyString_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Name", ""));

        // Act
        bool result = PlotSerializer.GetStringAttribute(element, "Name", out string? value);

        // Assert
        Assert.True(result);
        Assert.Equal("", value);
    }

    [Fact]
    public void GetStringAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");

        // Act
        bool result = PlotSerializer.GetStringAttribute(element, "Name", out string? value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void GetStringAttribute_SpecialCharacters_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Name", "Value with spaces & special <chars>"));

        // Act
        bool result = PlotSerializer.GetStringAttribute(element, "Name", out string? value);

        // Assert
        Assert.True(result);
        Assert.Equal("Value with spaces & special <chars>", value);
    }

    #endregion

    #region GetDoubleAttribute Tests

    [Fact]
    public void GetDoubleAttribute_ValidInteger_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Value", "42"));

        // Act
        bool result = PlotSerializer.GetDoubleAttribute(element, "Value", out double value);

        // Assert
        Assert.True(result);
        Assert.Equal(42.0, value);
    }

    [Fact]
    public void GetDoubleAttribute_ValidDecimal_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Value", "3.14159"));

        // Act
        bool result = PlotSerializer.GetDoubleAttribute(element, "Value", out double value);

        // Assert
        Assert.True(result);
        Assert.Equal(3.14159, value, 5);
    }

    [Fact]
    public void GetDoubleAttribute_NegativeValue_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Value", "-123.456"));

        // Act
        bool result = PlotSerializer.GetDoubleAttribute(element, "Value", out double value);

        // Assert
        Assert.True(result);
        Assert.Equal(-123.456, value, 3);
    }

    [Fact]
    public void GetDoubleAttribute_ScientificNotation_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Value", "1.23E+10"));

        // Act
        bool result = PlotSerializer.GetDoubleAttribute(element, "Value", out double value);

        // Assert
        Assert.True(result);
        Assert.Equal(1.23E+10, value);
    }

    [Fact]
    public void GetDoubleAttribute_NaN_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Value", "NaN"));

        // Act
        bool result = PlotSerializer.GetDoubleAttribute(element, "Value", out double value);

        // Assert
        Assert.True(result);
        Assert.True(double.IsNaN(value));
    }

    [Fact]
    public void GetDoubleAttribute_Infinity_ReturnsTrue()
    {
        // Arrange - Use the invariant culture representation
        var element = new XElement("Test", new XAttribute("Value", double.PositiveInfinity.ToString(CultureInfo.InvariantCulture)));

        // Act
        bool result = PlotSerializer.GetDoubleAttribute(element, "Value", out double value);

        // Assert
        Assert.True(result);
        Assert.True(double.IsPositiveInfinity(value));
    }

    [Fact]
    public void GetDoubleAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");

        // Act
        bool result = PlotSerializer.GetDoubleAttribute(element, "Value", out double value);

        // Assert
        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void GetDoubleAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Value", ""));

        // Act
        bool result = PlotSerializer.GetDoubleAttribute(element, "Value", out double value);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetDoubleAttribute_InvalidValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Value", "not a number"));

        // Act
        bool result = PlotSerializer.GetDoubleAttribute(element, "Value", out double value);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetDoubleAttribute_G17Precision_ReturnsTrue()
    {
        // Arrange - Test round-trip with G17 format
        double originalValue = 1.23456789012345678;
        var element = new XElement("Test", new XAttribute("Value", originalValue.ToString("G17", CultureInfo.InvariantCulture)));

        // Act
        bool result = PlotSerializer.GetDoubleAttribute(element, "Value", out double value);

        // Assert
        Assert.True(result);
        Assert.Equal(originalValue, value);
    }

    #endregion

    #region GetIntegerAttribute Tests

    [Fact]
    public void GetIntegerAttribute_ValidPositive_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Count", "42"));

        // Act
        bool result = PlotSerializer.GetIntegerAttribute(element, "Count", out int value);

        // Assert
        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void GetIntegerAttribute_ValidNegative_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Count", "-100"));

        // Act
        bool result = PlotSerializer.GetIntegerAttribute(element, "Count", out int value);

        // Assert
        Assert.True(result);
        Assert.Equal(-100, value);
    }

    [Fact]
    public void GetIntegerAttribute_Zero_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Count", "0"));

        // Act
        bool result = PlotSerializer.GetIntegerAttribute(element, "Count", out int value);

        // Assert
        Assert.True(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void GetIntegerAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");

        // Act
        bool result = PlotSerializer.GetIntegerAttribute(element, "Count", out int value);

        // Assert
        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void GetIntegerAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Count", ""));

        // Act
        bool result = PlotSerializer.GetIntegerAttribute(element, "Count", out int value);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetIntegerAttribute_DecimalValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Count", "3.14"));

        // Act
        bool result = PlotSerializer.GetIntegerAttribute(element, "Count", out int value);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetIntegerAttribute_InvalidValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Count", "not an integer"));

        // Act
        bool result = PlotSerializer.GetIntegerAttribute(element, "Count", out int value);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetBooleanAttribute Tests

    [Fact]
    public void GetBooleanAttribute_True_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Enabled", "True"));

        // Act
        bool result = PlotSerializer.GetBooleanAttribute(element, "Enabled", out bool value);

        // Assert
        Assert.True(result);
        Assert.True(value);
    }

    [Fact]
    public void GetBooleanAttribute_False_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Enabled", "False"));

        // Act
        bool result = PlotSerializer.GetBooleanAttribute(element, "Enabled", out bool value);

        // Assert
        Assert.True(result);
        Assert.False(value);
    }

    [Fact]
    public void GetBooleanAttribute_LowercaseTrue_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Enabled", "true"));

        // Act
        bool result = PlotSerializer.GetBooleanAttribute(element, "Enabled", out bool value);

        // Assert
        Assert.True(result);
        Assert.True(value);
    }

    [Fact]
    public void GetBooleanAttribute_LowercaseFalse_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Enabled", "false"));

        // Act
        bool result = PlotSerializer.GetBooleanAttribute(element, "Enabled", out bool value);

        // Assert
        Assert.True(result);
        Assert.False(value);
    }

    [Fact]
    public void GetBooleanAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");

        // Act
        bool result = PlotSerializer.GetBooleanAttribute(element, "Enabled", out bool value);

        // Assert
        Assert.False(result);
        Assert.False(value);
    }

    [Fact]
    public void GetBooleanAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Enabled", ""));

        // Act
        bool result = PlotSerializer.GetBooleanAttribute(element, "Enabled", out bool value);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetBooleanAttribute_InvalidValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Enabled", "yes"));

        // Act
        bool result = PlotSerializer.GetBooleanAttribute(element, "Enabled", out bool value);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetFontFamilyAttribute Tests

    [Fact]
    public void GetFontFamilyAttribute_ValidFont_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Font", "Arial"));
        var converter = new FontFamilyConverter();

        // Act
        bool result = PlotSerializer.GetFontFamilyAttribute(element, "Font", converter, out FontFamily? fontFamily);

        // Assert
        Assert.True(result);
        Assert.NotNull(fontFamily);
        Assert.Equal("Arial", fontFamily.Source);
    }

    [Fact]
    public void GetFontFamilyAttribute_SegoeUI_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Font", "Segoe UI"));
        var converter = new FontFamilyConverter();

        // Act
        bool result = PlotSerializer.GetFontFamilyAttribute(element, "Font", converter, out FontFamily? fontFamily);

        // Assert
        Assert.True(result);
        Assert.NotNull(fontFamily);
        Assert.Equal("Segoe UI", fontFamily.Source);
    }

    [Fact]
    public void GetFontFamilyAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");
        var converter = new FontFamilyConverter();

        // Act
        bool result = PlotSerializer.GetFontFamilyAttribute(element, "Font", converter, out FontFamily? fontFamily);

        // Assert
        Assert.False(result);
        Assert.Null(fontFamily);
    }

    [Fact]
    public void GetFontFamilyAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Font", ""));
        var converter = new FontFamilyConverter();

        // Act
        bool result = PlotSerializer.GetFontFamilyAttribute(element, "Font", converter, out FontFamily? fontFamily);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetFontWeightAttribute Tests

    [Fact]
    public void GetFontWeightAttribute_Normal_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Weight", "Normal"));
        var converter = new FontWeightConverter();

        // Act
        bool result = PlotSerializer.GetFontWeightAttribute(element, "Weight", converter, out FontWeight fontWeight);

        // Assert
        Assert.True(result);
        Assert.Equal(System.Windows.FontWeights.Normal, fontWeight);
    }

    [Fact]
    public void GetFontWeightAttribute_Bold_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Weight", "Bold"));
        var converter = new FontWeightConverter();

        // Act
        bool result = PlotSerializer.GetFontWeightAttribute(element, "Weight", converter, out FontWeight fontWeight);

        // Assert
        Assert.True(result);
        Assert.Equal(System.Windows.FontWeights.Bold, fontWeight);
    }

    [Fact]
    public void GetFontWeightAttribute_Light_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Weight", "Light"));
        var converter = new FontWeightConverter();

        // Act
        bool result = PlotSerializer.GetFontWeightAttribute(element, "Weight", converter, out FontWeight fontWeight);

        // Assert
        Assert.True(result);
        Assert.Equal(System.Windows.FontWeights.Light, fontWeight);
    }

    [Fact]
    public void GetFontWeightAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");
        var converter = new FontWeightConverter();

        // Act
        bool result = PlotSerializer.GetFontWeightAttribute(element, "Weight", converter, out FontWeight fontWeight);

        // Assert
        Assert.False(result);
        Assert.Equal(default(FontWeight), fontWeight);
    }

    [Fact]
    public void GetFontWeightAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Weight", ""));
        var converter = new FontWeightConverter();

        // Act
        bool result = PlotSerializer.GetFontWeightAttribute(element, "Weight", converter, out FontWeight fontWeight);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetThicknessAttribute Tests

    [Fact]
    public void GetThicknessAttribute_UniformValue_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Thickness", "5"));
        var converter = new ThicknessConverter();

        // Act
        bool result = PlotSerializer.GetThicknessAttribute(element, "Thickness", converter, out Thickness thickness);

        // Assert
        Assert.True(result);
        Assert.Equal(5, thickness.Left);
        Assert.Equal(5, thickness.Top);
        Assert.Equal(5, thickness.Right);
        Assert.Equal(5, thickness.Bottom);
    }

    [Fact]
    public void GetThicknessAttribute_FourValues_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Thickness", "1,2,3,4"));
        var converter = new ThicknessConverter();

        // Act
        bool result = PlotSerializer.GetThicknessAttribute(element, "Thickness", converter, out Thickness thickness);

        // Assert
        Assert.True(result);
        Assert.Equal(1, thickness.Left);
        Assert.Equal(2, thickness.Top);
        Assert.Equal(3, thickness.Right);
        Assert.Equal(4, thickness.Bottom);
    }

    [Fact]
    public void GetThicknessAttribute_TwoValues_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Thickness", "10,20"));
        var converter = new ThicknessConverter();

        // Act
        bool result = PlotSerializer.GetThicknessAttribute(element, "Thickness", converter, out Thickness thickness);

        // Assert
        Assert.True(result);
        Assert.Equal(10, thickness.Left);
        Assert.Equal(20, thickness.Top);
        Assert.Equal(10, thickness.Right);
        Assert.Equal(20, thickness.Bottom);
    }

    [Fact]
    public void GetThicknessAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");
        var converter = new ThicknessConverter();

        // Act
        bool result = PlotSerializer.GetThicknessAttribute(element, "Thickness", converter, out Thickness thickness);

        // Assert
        Assert.False(result);
        Assert.Equal(default(Thickness), thickness);
    }

    [Fact]
    public void GetThicknessAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Thickness", ""));
        var converter = new ThicknessConverter();

        // Act
        bool result = PlotSerializer.GetThicknessAttribute(element, "Thickness", converter, out Thickness thickness);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetEnumAttribute Tests

    [Fact]
    public void GetEnumAttribute_ValidLineStyle_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Style", "Dash"));

        // Act
        bool result = PlotSerializer.GetEnumAttribute(element, "Style", out LineStyle style);

        // Assert
        Assert.True(result);
        Assert.Equal(LineStyle.Dash, style);
    }

    [Fact]
    public void GetEnumAttribute_ValidAxisPosition_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Position", "Left"));

        // Act
        bool result = PlotSerializer.GetEnumAttribute(element, "Position", out OxyPlot.Axes.AxisPosition position);

        // Assert
        Assert.True(result);
        Assert.Equal(OxyPlot.Axes.AxisPosition.Left, position);
    }

    [Fact]
    public void GetEnumAttribute_ValidTickStyle_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("TickStyle", "Inside"));

        // Act
        bool result = PlotSerializer.GetEnumAttribute(element, "TickStyle", out OxyPlot.Axes.TickStyle tickStyle);

        // Assert
        Assert.True(result);
        Assert.Equal(OxyPlot.Axes.TickStyle.Inside, tickStyle);
    }

    [Fact]
    public void GetEnumAttribute_ValidLegendPosition_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Position", "TopRight"));

        // Act
        bool result = PlotSerializer.GetEnumAttribute(element, "Position", out OxyPlot.Legends.LegendPosition position);

        // Assert
        Assert.True(result);
        Assert.Equal(OxyPlot.Legends.LegendPosition.TopRight, position);
    }

    [Fact]
    public void GetEnumAttribute_ValidLegendPlacement_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Placement", "Inside"));

        // Act
        bool result = PlotSerializer.GetEnumAttribute(element, "Placement", out OxyPlot.Legends.LegendPlacement placement);

        // Assert
        Assert.True(result);
        Assert.Equal(OxyPlot.Legends.LegendPlacement.Inside, placement);
    }

    [Fact]
    public void GetEnumAttribute_ValidLegendOrientation_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Orientation", "Horizontal"));

        // Act
        bool result = PlotSerializer.GetEnumAttribute(element, "Orientation", out OxyPlot.Legends.LegendOrientation orientation);

        // Assert
        Assert.True(result);
        Assert.Equal(OxyPlot.Legends.LegendOrientation.Horizontal, orientation);
    }

    [Fact]
    public void GetEnumAttribute_ValidHorizontalAlignment_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Alignment", "Center"));

        // Act
        bool result = PlotSerializer.GetEnumAttribute(element, "Alignment", out System.Windows.HorizontalAlignment alignment);

        // Assert
        Assert.True(result);
        Assert.Equal(System.Windows.HorizontalAlignment.Center, alignment);
    }

    [Fact]
    public void GetEnumAttribute_ValidCalendarWeekRule_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("WeekRule", "FirstDay"));

        // Act
        bool result = PlotSerializer.GetEnumAttribute(element, "WeekRule", out CalendarWeekRule weekRule);

        // Assert
        Assert.True(result);
        Assert.Equal(CalendarWeekRule.FirstDay, weekRule);
    }

    [Fact]
    public void GetEnumAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");

        // Act
        bool result = PlotSerializer.GetEnumAttribute(element, "Style", out LineStyle style);

        // Assert
        Assert.False(result);
        Assert.Equal(default(LineStyle), style);
    }

    [Fact]
    public void GetEnumAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Style", ""));

        // Act
        bool result = PlotSerializer.GetEnumAttribute(element, "Style", out LineStyle style);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetEnumAttribute_InvalidValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Style", "InvalidStyle"));

        // Act
        bool result = PlotSerializer.GetEnumAttribute(element, "Style", out LineStyle style);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetDataPointAttribute Tests

    [Fact]
    public void GetDataPointAttribute_ValidPoint_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Point", "10.5, 20.5"));

        // Act
        bool result = PlotSerializer.GetDataPointAttribute(element, "Point", out DataPoint dp);

        // Assert
        Assert.True(result);
        Assert.Equal(10.5, dp.X);
        Assert.Equal(20.5, dp.Y);
    }

    [Fact]
    public void GetDataPointAttribute_NegativeValues_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Point", "-5.5, -10.5"));

        // Act
        bool result = PlotSerializer.GetDataPointAttribute(element, "Point", out DataPoint dp);

        // Assert
        Assert.True(result);
        Assert.Equal(-5.5, dp.X);
        Assert.Equal(-10.5, dp.Y);
    }

    [Fact]
    public void GetDataPointAttribute_Zero_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Point", "0, 0"));

        // Act
        bool result = PlotSerializer.GetDataPointAttribute(element, "Point", out DataPoint dp);

        // Assert
        Assert.True(result);
        Assert.Equal(0, dp.X);
        Assert.Equal(0, dp.Y);
    }

    [Fact]
    public void GetDataPointAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");

        // Act
        bool result = PlotSerializer.GetDataPointAttribute(element, "Point", out DataPoint dp);

        // Assert
        Assert.False(result);
        Assert.Equal(DataPoint.Undefined, dp);
    }

    [Fact]
    public void GetDataPointAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Point", ""));

        // Act
        bool result = PlotSerializer.GetDataPointAttribute(element, "Point", out DataPoint dp);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetScreenVectorAttribute Tests

    [Fact]
    public void GetScreenVectorAttribute_ValidVector_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Vector", "100.5, 200.5"));

        // Act
        bool result = PlotSerializer.GetScreenVectorAttribute(element, "Vector", out ScreenVector sv);

        // Assert
        Assert.True(result);
        Assert.Equal(100.5, sv.X);
        Assert.Equal(200.5, sv.Y);
    }

    [Fact]
    public void GetScreenVectorAttribute_NegativeValues_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Vector", "-50.5, -75.5"));

        // Act
        bool result = PlotSerializer.GetScreenVectorAttribute(element, "Vector", out ScreenVector sv);

        // Assert
        Assert.True(result);
        Assert.Equal(-50.5, sv.X);
        Assert.Equal(-75.5, sv.Y);
    }

    [Fact]
    public void GetScreenVectorAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");

        // Act
        bool result = PlotSerializer.GetScreenVectorAttribute(element, "Vector", out ScreenVector sv);

        // Assert
        Assert.False(result);
        Assert.Equal(default(ScreenVector), sv);
    }

    [Fact]
    public void GetScreenVectorAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Vector", ""));

        // Act
        bool result = PlotSerializer.GetScreenVectorAttribute(element, "Vector", out ScreenVector sv);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetScreenPointAttribute Tests

    [Fact]
    public void GetScreenPointAttribute_ValidPoint_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Point", "150.5, 250.5"));

        // Act
        bool result = PlotSerializer.GetScreenPointAttribute(element, "Point", out ScreenPoint sp);

        // Assert
        Assert.True(result);
        Assert.Equal(150.5, sp.X);
        Assert.Equal(250.5, sp.Y);
    }

    [Fact]
    public void GetScreenPointAttribute_Zero_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Point", "0, 0"));

        // Act
        bool result = PlotSerializer.GetScreenPointAttribute(element, "Point", out ScreenPoint sp);

        // Assert
        Assert.True(result);
        Assert.Equal(0, sp.X);
        Assert.Equal(0, sp.Y);
    }

    [Fact]
    public void GetScreenPointAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");

        // Act
        bool result = PlotSerializer.GetScreenPointAttribute(element, "Point", out ScreenPoint sp);

        // Assert
        Assert.False(result);
        Assert.Equal(ScreenPoint.Undefined, sp);
    }

    [Fact]
    public void GetScreenPointAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Point", ""));

        // Act
        bool result = PlotSerializer.GetScreenPointAttribute(element, "Point", out ScreenPoint sp);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetVectorAttribute Tests

    [Fact]
    public void GetVectorAttribute_ValidVector_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Vector", "30.5, 40.5"));

        // Act
        bool result = PlotSerializer.GetVectorAttribute(element, "Vector", out Vector v);

        // Assert
        Assert.True(result);
        Assert.Equal(30.5, v.X);
        Assert.Equal(40.5, v.Y);
    }

    [Fact]
    public void GetVectorAttribute_NegativeValues_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Vector", "-15.5, -25.5"));

        // Act
        bool result = PlotSerializer.GetVectorAttribute(element, "Vector", out Vector v);

        // Assert
        Assert.True(result);
        Assert.Equal(-15.5, v.X);
        Assert.Equal(-25.5, v.Y);
    }

    [Fact]
    public void GetVectorAttribute_Zero_ReturnsTrue()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Vector", "0, 0"));

        // Act
        bool result = PlotSerializer.GetVectorAttribute(element, "Vector", out Vector v);

        // Assert
        Assert.True(result);
        Assert.Equal(0, v.X);
        Assert.Equal(0, v.Y);
    }

    [Fact]
    public void GetVectorAttribute_MissingAttribute_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test");

        // Act
        bool result = PlotSerializer.GetVectorAttribute(element, "Vector", out Vector v);

        // Assert
        Assert.False(result);
        Assert.Equal(default(Vector), v);
    }

    [Fact]
    public void GetVectorAttribute_EmptyValue_ReturnsFalse()
    {
        // Arrange
        var element = new XElement("Test", new XAttribute("Vector", ""));

        // Act
        bool result = PlotSerializer.GetVectorAttribute(element, "Vector", out Vector v);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region OxyplotPropertiesTag Tests

    [Fact]
    public void OxyplotPropertiesTag_HasExpectedValue()
    {
        // Assert
        Assert.Equal("OxyplotProperties", OxyPlotSettingsSerializer.OxyplotPropertiesTag);
    }

    #endregion
}
