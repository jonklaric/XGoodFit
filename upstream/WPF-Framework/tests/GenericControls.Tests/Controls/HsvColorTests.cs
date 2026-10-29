using System.Windows.Media;
using Xunit;

namespace GenericControls.Tests.Controls;

/// <summary>
/// Unit tests for the <see cref="ColorPicker.HsvColor"/> struct and related conversion methods.
/// </summary>
public class HsvColorTests
{
    #region HsvColor Constructor Tests

    /// <summary>
    /// Tests that the HsvColor constructor sets the H, S, and V values correctly.
    /// </summary>
    [Fact]
    public void HsvColor_Constructor_SetsValues()
    {
        // Arrange & Act
        var hsv = new ColorPicker.HsvColor(180, 0.5, 0.75);

        // Assert
        Assert.Equal(180, hsv.H);
        Assert.Equal(0.5, hsv.S);
        Assert.Equal(0.75, hsv.V);
    }

    /// <summary>
    /// Tests that the default HsvColor constructor sets all values to zero.
    /// </summary>
    [Fact]
    public void HsvColor_DefaultConstructor_SetsZeroValues()
    {
        // Arrange & Act
        var hsv = new ColorPicker.HsvColor();

        // Assert
        Assert.Equal(0, hsv.H);
        Assert.Equal(0, hsv.S);
        Assert.Equal(0, hsv.V);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Primary Colors

    /// <summary>
    /// Tests that ConvertHsvToRgb correctly converts HSV values for red color.
    /// </summary>
    [Fact]
    public void ConvertHsvToRgb_Red_ReturnsCorrectColor()
    {
        // Arrange - Red is at Hue = 0
        double h = 0, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal(255, color.A);
    }

    /// <summary>
    /// Tests that ConvertHsvToRgb correctly converts HSV values for green color.
    /// </summary>
    [Fact]
    public void ConvertHsvToRgb_Green_ReturnsCorrectColor()
    {
        // Arrange - Green is at Hue = 120
        double h = 120, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.Equal(0, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(0, color.B);
    }

    /// <summary>
    /// Tests that ConvertHsvToRgb correctly converts HSV values for blue color.
    /// </summary>
    [Fact]
    public void ConvertHsvToRgb_Blue_ReturnsCorrectColor()
    {
        // Arrange - Blue is at Hue = 240
        double h = 240, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(255, color.B);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Secondary Colors

    [Fact]
    public void ConvertHsvToRgb_Yellow_ReturnsCorrectColor()
    {
        // Arrange - Yellow is at Hue = 60
        double h = 60, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void ConvertHsvToRgb_Cyan_ReturnsCorrectColor()
    {
        // Arrange - Cyan is at Hue = 180
        double h = 180, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.Equal(0, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(255, color.B);
    }

    [Fact]
    public void ConvertHsvToRgb_Magenta_ReturnsCorrectColor()
    {
        // Arrange - Magenta is at Hue = 300
        double h = 300, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(255, color.B);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Grayscale

    [Fact]
    public void ConvertHsvToRgb_Black_ReturnsCorrectColor()
    {
        // Arrange - Black is V = 0
        double h = 0, s = 0, v = 0;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void ConvertHsvToRgb_White_ReturnsCorrectColor()
    {
        // Arrange - White is S = 0, V = 1
        double h = 0, s = 0, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(255, color.B);
    }

    [Fact]
    public void ConvertHsvToRgb_MidGray_ReturnsCorrectColor()
    {
        // Arrange - 50% gray is S = 0, V = 0.5
        double h = 0, s = 0, v = 0.5;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.Equal(128, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(128, color.B);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Saturation Variations

    [Fact]
    public void ConvertHsvToRgb_ZeroSaturation_ReturnsGrayscale()
    {
        // Arrange - Any hue with 0 saturation should be gray
        double h = 180, s = 0, v = 0.5;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert - All RGB values should be equal (grayscale)
        Assert.Equal(color.R, color.G);
        Assert.Equal(color.G, color.B);
    }

    [Fact]
    public void ConvertHsvToRgb_PartialSaturation_ReturnsDesaturatedColor()
    {
        // Arrange - Red with 50% saturation
        double h = 0, s = 0.5, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert - Should be a pinkish color
        Assert.Equal(255, color.R);
        Assert.True(color.G > 0);
        Assert.True(color.B > 0);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Value (Brightness) Variations

    [Fact]
    public void ConvertHsvToRgb_HalfBrightness_ReturnsDarkerColor()
    {
        // Arrange - Red at 50% brightness
        double h = 0, s = 1, v = 0.5;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert - Should be a dark red
        Assert.Equal(128, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void ConvertHsvToRgb_ZeroBrightness_ReturnsBlack()
    {
        // Arrange - Any color with 0 value should be black
        double h = 120, s = 1, v = 0;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Edge Cases

    [Fact]
    public void ConvertHsvToRgb_Hue360_SameAsHue0()
    {
        // Arrange - Hue 360 should be the same as Hue 0
        var colorAt0 = ColorPicker.ConvertHsvToRgb(0, 1, 1);
        var colorAt360 = ColorPicker.ConvertHsvToRgb(360, 1, 1);

        // Assert
        Assert.Equal(colorAt0.R, colorAt360.R);
        Assert.Equal(colorAt0.G, colorAt360.G);
        Assert.Equal(colorAt0.B, colorAt360.B);
    }

    [Theory]
    [InlineData(30)]   // Orange region
    [InlineData(90)]   // Yellow-Green region
    [InlineData(150)]  // Green-Cyan region
    [InlineData(210)]  // Cyan-Blue region
    [InlineData(270)]  // Blue-Magenta region
    [InlineData(330)]  // Magenta-Red region
    public void ConvertHsvToRgb_VariousHues_ReturnsValidColor(double hue)
    {
        // Act
        var color = ColorPicker.ConvertHsvToRgb(hue, 1, 1);

        // Assert - Color should have at least some non-zero component
        Assert.True(color.R + color.G + color.B > 0);
        Assert.Equal(255, color.A);
    }

    #endregion

    #region ConvertRgbToHsv Tests - Primary Colors

    [Fact]
    public void ConvertRgbToHsv_Red_ReturnsCorrectHsv()
    {
        // Arrange
        int r = 255, g = 0, b = 0;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert
        Assert.Equal(0, hsv.H, 1);
        Assert.Equal(1, hsv.S, 2);
        Assert.Equal(1, hsv.V, 2);
    }

    [Fact]
    public void ConvertRgbToHsv_Green_ReturnsCorrectHsv()
    {
        // Arrange
        int r = 0, g = 255, b = 0;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert
        Assert.Equal(120, hsv.H, 1);
        Assert.Equal(1, hsv.S, 2);
        Assert.Equal(1, hsv.V, 2);
    }

    [Fact]
    public void ConvertRgbToHsv_Blue_ReturnsCorrectHsv()
    {
        // Arrange
        int r = 0, g = 0, b = 255;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert
        Assert.Equal(240, hsv.H, 1);
        Assert.Equal(1, hsv.S, 2);
        Assert.Equal(1, hsv.V, 2);
    }

    #endregion

    #region ConvertRgbToHsv Tests - Grayscale

    [Fact]
    public void ConvertRgbToHsv_Black_ReturnsCorrectHsv()
    {
        // Arrange
        int r = 0, g = 0, b = 0;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert
        Assert.Equal(0, hsv.H);
        Assert.Equal(0, hsv.S);
        Assert.Equal(0, hsv.V);
    }

    [Fact]
    public void ConvertRgbToHsv_White_ReturnsCorrectHsv()
    {
        // Arrange
        int r = 255, g = 255, b = 255;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert
        Assert.Equal(0, hsv.S); // White has no saturation
        Assert.Equal(1, hsv.V, 2);
    }

    [Fact]
    public void ConvertRgbToHsv_Gray_ReturnsSaturationZero()
    {
        // Arrange
        int r = 128, g = 128, b = 128;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert - Gray has no saturation
        Assert.Equal(0, hsv.S, 2);
    }

    #endregion

    #region Round Trip Tests (HSV -> RGB -> HSV and RGB -> HSV -> RGB)

    [Theory]
    [InlineData(0, 1, 1)]     // Red
    [InlineData(60, 1, 1)]    // Yellow
    [InlineData(120, 1, 1)]   // Green
    [InlineData(180, 1, 1)]   // Cyan
    [InlineData(240, 1, 1)]   // Blue
    [InlineData(300, 1, 1)]   // Magenta
    public void RoundTrip_HsvToRgbToHsv_PreservesValues(double h, double s, double v)
    {
        // Act
        var rgb = ColorPicker.ConvertHsvToRgb(h, s, v);
        var hsvBack = ColorPicker.ConvertRgbToHsv(rgb.R, rgb.G, rgb.B);

        // Assert - Allow small tolerance for rounding
        Assert.Equal(h, hsvBack.H, 1);
        Assert.Equal(s, hsvBack.S, 2);
        Assert.Equal(v, hsvBack.V, 2);
    }

    [Theory]
    [InlineData(255, 0, 0)]   // Red
    [InlineData(0, 255, 0)]   // Green
    [InlineData(0, 0, 255)]   // Blue
    [InlineData(255, 255, 0)] // Yellow
    [InlineData(0, 255, 255)] // Cyan
    [InlineData(255, 0, 255)] // Magenta
    public void RoundTrip_RgbToHsvToRgb_PreservesValues(int r, int g, int b)
    {
        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);
        var rgbBack = ColorPicker.ConvertHsvToRgb(hsv.H, hsv.S, hsv.V);

        // Assert - Allow small tolerance for rounding
        Assert.Equal(r, rgbBack.R);
        Assert.Equal(g, rgbBack.G);
        Assert.Equal(b, rgbBack.B);
    }

    #endregion

    #region GenerateHsvSpectrum Tests

    [Fact]
    public void GenerateHsvSpectrum_ReturnsListOfColors()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert
        Assert.NotNull(spectrum);
        Assert.NotEmpty(spectrum);
    }

    [Fact]
    public void GenerateHsvSpectrum_FirstAndLastColorAreSame()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert - The spectrum should wrap around (red at both ends)
        var first = spectrum.First();
        var last = spectrum.Last();
        Assert.Equal(first.R, last.R);
        Assert.Equal(first.G, last.G);
        Assert.Equal(first.B, last.B);
    }

    [Fact]
    public void GenerateHsvSpectrum_ContainsExpectedNumberOfColors()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert - Based on implementation: 30 colors (0 to 28 + 1 for wrap)
        Assert.Equal(30, spectrum.Count);
    }

    [Fact]
    public void GenerateHsvSpectrum_AllColorsHaveFullAlpha()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert
        foreach (var color in spectrum)
        {
            Assert.Equal(255, color.A);
        }
    }

    [Fact]
    public void GenerateHsvSpectrum_ContainsRed()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert - Should contain pure red (at Hue = 0)
        Assert.Contains(spectrum, c => c.R == 255 && c.G == 0 && c.B == 0);
    }

    [Fact]
    public void GenerateHsvSpectrum_ContainsGreen()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert - Should contain pure green (at Hue = 120)
        Assert.Contains(spectrum, c => c.R == 0 && c.G == 255 && c.B == 0);
    }

    [Fact]
    public void GenerateHsvSpectrum_ContainsBlue()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert - Should contain pure blue (at Hue = 240)
        Assert.Contains(spectrum, c => c.R == 0 && c.G == 0 && c.B == 255);
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(180)]
    [InlineData(240)]
    [InlineData(300)]
    [InlineData(360)]
    public void ConvertHsvToRgb_HueBoundaries_DoesNotThrow(double hue)
    {
        // Act & Assert - Should not throw
        var color = ColorPicker.ConvertHsvToRgb(hue, 1, 1);
        Assert.True(color.R >= 0 && color.R <= 255);
        Assert.True(color.G >= 0 && color.G <= 255);
        Assert.True(color.B >= 0 && color.B <= 255);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1)]
    public void ConvertHsvToRgb_SaturationBoundaries_DoesNotThrow(double saturation)
    {
        // Act & Assert - Should not throw
        var color = ColorPicker.ConvertHsvToRgb(180, saturation, 1);
        Assert.True(color.R >= 0 && color.R <= 255);
        Assert.True(color.G >= 0 && color.G <= 255);
        Assert.True(color.B >= 0 && color.B <= 255);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1)]
    public void ConvertHsvToRgb_ValueBoundaries_DoesNotThrow(double value)
    {
        // Act & Assert - Should not throw
        var color = ColorPicker.ConvertHsvToRgb(180, 1, value);
        Assert.True(color.R >= 0 && color.R <= 255);
        Assert.True(color.G >= 0 && color.G <= 255);
        Assert.True(color.B >= 0 && color.B <= 255);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(255, 255, 255)]
    [InlineData(128, 128, 128)]
    [InlineData(255, 0, 0)]
    [InlineData(0, 255, 0)]
    [InlineData(0, 0, 255)]
    public void ConvertRgbToHsv_BoundaryValues_DoesNotThrow(int r, int g, int b)
    {
        // Act & Assert - Should not throw
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);
        Assert.True(hsv.H >= 0 && hsv.H <= 360);
        Assert.True(hsv.S >= 0 && hsv.S <= 1);
        Assert.True(hsv.V >= 0 && hsv.V <= 1);
    }

    #endregion

    #region Specific Color Tests

    [Fact]
    public void ConvertHsvToRgb_Orange_ReturnsCorrectColor()
    {
        // Arrange - Orange is approximately Hue = 30
        double h = 30, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.Equal(255, color.R);
        Assert.True(color.G > 100 && color.G < 150); // Orange green component
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void ConvertHsvToRgb_Purple_ReturnsCorrectColor()
    {
        // Arrange - Purple is approximately Hue = 270
        double h = 270, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.True(color.R > 100);
        Assert.Equal(0, color.G);
        Assert.Equal(255, color.B);
    }

    #endregion
}
