namespace GenericControls.Tests;

/// <summary>
/// Unit tests for the <see cref="ColorPicker.HsvColor"/> struct and related conversion methods.
/// </summary>
/// <remarks>
/// XGoodFit port of upstream's tests/GenericControls.Tests/Controls/HsvColorTests.cs, from xUnit to MSTest:
/// facts and theories are test methods with data rows, and a precision of n decimal places is a delta
/// of half the nth decimal place.
/// </remarks>
[TestClass]
public sealed class HsvColorTests
{
    #region HsvColor Constructor Tests

    /// <summary>
    /// Tests that the HsvColor constructor sets the H, S, and V values correctly.
    /// </summary>
    [TestMethod]
    public void HsvColor_Constructor_SetsValues()
    {
        // Arrange & Act
        var hsv = new ColorPicker.HsvColor(180, 0.5, 0.75);

        // Assert
        Assert.AreEqual(180, hsv.H);
        Assert.AreEqual(0.5, hsv.S);
        Assert.AreEqual(0.75, hsv.V);
    }

    /// <summary>
    /// Tests that the default HsvColor constructor sets all values to zero.
    /// </summary>
    [TestMethod]
    public void HsvColor_DefaultConstructor_SetsZeroValues()
    {
        // Arrange & Act
        var hsv = new ColorPicker.HsvColor();

        // Assert
        Assert.AreEqual(0, hsv.H);
        Assert.AreEqual(0, hsv.S);
        Assert.AreEqual(0, hsv.V);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Primary Colors

    /// <summary>
    /// Tests that ConvertHsvToRgb correctly converts HSV values for red color.
    /// </summary>
    [TestMethod]
    public void ConvertHsvToRgb_Red_ReturnsCorrectColor()
    {
        // Arrange - Red is at Hue = 0
        double h = 0, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.AreEqual(255, color.R);
        Assert.AreEqual(0, color.G);
        Assert.AreEqual(0, color.B);
        Assert.AreEqual(255, color.A);
    }

    /// <summary>
    /// Tests that ConvertHsvToRgb correctly converts HSV values for green color.
    /// </summary>
    [TestMethod]
    public void ConvertHsvToRgb_Green_ReturnsCorrectColor()
    {
        // Arrange - Green is at Hue = 120
        double h = 120, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.AreEqual(0, color.R);
        Assert.AreEqual(255, color.G);
        Assert.AreEqual(0, color.B);
    }

    /// <summary>
    /// Tests that ConvertHsvToRgb correctly converts HSV values for blue color.
    /// </summary>
    [TestMethod]
    public void ConvertHsvToRgb_Blue_ReturnsCorrectColor()
    {
        // Arrange - Blue is at Hue = 240
        double h = 240, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.AreEqual(0, color.R);
        Assert.AreEqual(0, color.G);
        Assert.AreEqual(255, color.B);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Secondary Colors

    [TestMethod]
    public void ConvertHsvToRgb_Yellow_ReturnsCorrectColor()
    {
        // Arrange - Yellow is at Hue = 60
        double h = 60, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.AreEqual(255, color.R);
        Assert.AreEqual(255, color.G);
        Assert.AreEqual(0, color.B);
    }

    [TestMethod]
    public void ConvertHsvToRgb_Cyan_ReturnsCorrectColor()
    {
        // Arrange - Cyan is at Hue = 180
        double h = 180, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.AreEqual(0, color.R);
        Assert.AreEqual(255, color.G);
        Assert.AreEqual(255, color.B);
    }

    [TestMethod]
    public void ConvertHsvToRgb_Magenta_ReturnsCorrectColor()
    {
        // Arrange - Magenta is at Hue = 300
        double h = 300, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.AreEqual(255, color.R);
        Assert.AreEqual(0, color.G);
        Assert.AreEqual(255, color.B);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Grayscale

    [TestMethod]
    public void ConvertHsvToRgb_Black_ReturnsCorrectColor()
    {
        // Arrange - Black is V = 0
        double h = 0, s = 0, v = 0;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.AreEqual(0, color.R);
        Assert.AreEqual(0, color.G);
        Assert.AreEqual(0, color.B);
    }

    [TestMethod]
    public void ConvertHsvToRgb_White_ReturnsCorrectColor()
    {
        // Arrange - White is S = 0, V = 1
        double h = 0, s = 0, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.AreEqual(255, color.R);
        Assert.AreEqual(255, color.G);
        Assert.AreEqual(255, color.B);
    }

    [TestMethod]
    public void ConvertHsvToRgb_MidGray_ReturnsCorrectColor()
    {
        // Arrange - 50% gray is S = 0, V = 0.5
        double h = 0, s = 0, v = 0.5;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.AreEqual(128, color.R);
        Assert.AreEqual(128, color.G);
        Assert.AreEqual(128, color.B);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Saturation Variations

    [TestMethod]
    public void ConvertHsvToRgb_ZeroSaturation_ReturnsGrayscale()
    {
        // Arrange - Any hue with 0 saturation should be gray
        double h = 180, s = 0, v = 0.5;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert - All RGB values should be equal (grayscale)
        Assert.AreEqual(color.R, color.G);
        Assert.AreEqual(color.G, color.B);
    }

    [TestMethod]
    public void ConvertHsvToRgb_PartialSaturation_ReturnsDesaturatedColor()
    {
        // Arrange - Red with 50% saturation
        double h = 0, s = 0.5, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert - Should be a pinkish color
        Assert.AreEqual(255, color.R);
        Assert.IsTrue(color.G > 0);
        Assert.IsTrue(color.B > 0);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Value (Brightness) Variations

    [TestMethod]
    public void ConvertHsvToRgb_HalfBrightness_ReturnsDarkerColor()
    {
        // Arrange - Red at 50% brightness
        double h = 0, s = 1, v = 0.5;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert - Should be a dark red
        Assert.AreEqual(128, color.R);
        Assert.AreEqual(0, color.G);
        Assert.AreEqual(0, color.B);
    }

    [TestMethod]
    public void ConvertHsvToRgb_ZeroBrightness_ReturnsBlack()
    {
        // Arrange - Any color with 0 value should be black
        double h = 120, s = 1, v = 0;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.AreEqual(0, color.R);
        Assert.AreEqual(0, color.G);
        Assert.AreEqual(0, color.B);
    }

    #endregion

    #region ConvertHsvToRgb Tests - Edge Cases

    [TestMethod]
    public void ConvertHsvToRgb_Hue360_SameAsHue0()
    {
        // Arrange - Hue 360 should be the same as Hue 0
        var colorAt0 = ColorPicker.ConvertHsvToRgb(0, 1, 1);
        var colorAt360 = ColorPicker.ConvertHsvToRgb(360, 1, 1);

        // Assert
        Assert.AreEqual(colorAt0.R, colorAt360.R);
        Assert.AreEqual(colorAt0.G, colorAt360.G);
        Assert.AreEqual(colorAt0.B, colorAt360.B);
    }

    [TestMethod]
    [DataRow(30d)]   // Orange region
    [DataRow(90d)]   // Yellow-Green region
    [DataRow(150d)]  // Green-Cyan region
    [DataRow(210d)]  // Cyan-Blue region
    [DataRow(270d)]  // Blue-Magenta region
    [DataRow(330d)]  // Magenta-Red region
    public void ConvertHsvToRgb_VariousHues_ReturnsValidColor(double hue)
    {
        // Act
        var color = ColorPicker.ConvertHsvToRgb(hue, 1, 1);

        // Assert - Color should have at least some non-zero component
        Assert.IsTrue(color.R + color.G + color.B > 0);
        Assert.AreEqual(255, color.A);
    }

    #endregion

    #region ConvertRgbToHsv Tests - Primary Colors

    [TestMethod]
    public void ConvertRgbToHsv_Red_ReturnsCorrectHsv()
    {
        // Arrange
        int r = 255, g = 0, b = 0;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert
        Assert.AreEqual(0, hsv.H, 0.05);
        Assert.AreEqual(1, hsv.S, 0.005);
        Assert.AreEqual(1, hsv.V, 0.005);
    }

    [TestMethod]
    public void ConvertRgbToHsv_Green_ReturnsCorrectHsv()
    {
        // Arrange
        int r = 0, g = 255, b = 0;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert
        Assert.AreEqual(120, hsv.H, 0.05);
        Assert.AreEqual(1, hsv.S, 0.005);
        Assert.AreEqual(1, hsv.V, 0.005);
    }

    [TestMethod]
    public void ConvertRgbToHsv_Blue_ReturnsCorrectHsv()
    {
        // Arrange
        int r = 0, g = 0, b = 255;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert
        Assert.AreEqual(240, hsv.H, 0.05);
        Assert.AreEqual(1, hsv.S, 0.005);
        Assert.AreEqual(1, hsv.V, 0.005);
    }

    #endregion

    #region ConvertRgbToHsv Tests - Grayscale

    [TestMethod]
    public void ConvertRgbToHsv_Black_ReturnsCorrectHsv()
    {
        // Arrange
        int r = 0, g = 0, b = 0;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert
        Assert.AreEqual(0, hsv.H);
        Assert.AreEqual(0, hsv.S);
        Assert.AreEqual(0, hsv.V);
    }

    [TestMethod]
    public void ConvertRgbToHsv_White_ReturnsCorrectHsv()
    {
        // Arrange
        int r = 255, g = 255, b = 255;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert
        Assert.AreEqual(0, hsv.S); // White has no saturation
        Assert.AreEqual(1, hsv.V, 0.005);
    }

    [TestMethod]
    public void ConvertRgbToHsv_Gray_ReturnsSaturationZero()
    {
        // Arrange
        int r = 128, g = 128, b = 128;

        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);

        // Assert - Gray has no saturation
        Assert.AreEqual(0, hsv.S, 0.005);
    }

    #endregion

    #region Round Trip Tests (HSV -> RGB -> HSV and RGB -> HSV -> RGB)

    [TestMethod]
    [DataRow(0d, 1d, 1d)]     // Red
    [DataRow(60d, 1d, 1d)]    // Yellow
    [DataRow(120d, 1d, 1d)]   // Green
    [DataRow(180d, 1d, 1d)]   // Cyan
    [DataRow(240d, 1d, 1d)]   // Blue
    [DataRow(300d, 1d, 1d)]   // Magenta
    public void RoundTrip_HsvToRgbToHsv_PreservesValues(double h, double s, double v)
    {
        // Act
        var rgb = ColorPicker.ConvertHsvToRgb(h, s, v);
        var hsvBack = ColorPicker.ConvertRgbToHsv(rgb.R, rgb.G, rgb.B);

        // Assert - Allow small tolerance for rounding
        Assert.AreEqual(h, hsvBack.H, 0.05);
        Assert.AreEqual(s, hsvBack.S, 0.005);
        Assert.AreEqual(v, hsvBack.V, 0.005);
    }

    [TestMethod]
    [DataRow(255, 0, 0)]   // Red
    [DataRow(0, 255, 0)]   // Green
    [DataRow(0, 0, 255)]   // Blue
    [DataRow(255, 255, 0)] // Yellow
    [DataRow(0, 255, 255)] // Cyan
    [DataRow(255, 0, 255)] // Magenta
    public void RoundTrip_RgbToHsvToRgb_PreservesValues(int r, int g, int b)
    {
        // Act
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);
        var rgbBack = ColorPicker.ConvertHsvToRgb(hsv.H, hsv.S, hsv.V);

        // Assert - Allow small tolerance for rounding
        Assert.AreEqual(r, rgbBack.R);
        Assert.AreEqual(g, rgbBack.G);
        Assert.AreEqual(b, rgbBack.B);
    }

    #endregion

    #region GenerateHsvSpectrum Tests

    [TestMethod]
    public void GenerateHsvSpectrum_ReturnsListOfColors()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert
        Assert.IsNotNull(spectrum);
        Assert.AreNotEqual(0, spectrum.Count);
    }

    [TestMethod]
    public void GenerateHsvSpectrum_FirstAndLastColorAreSame()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert - The spectrum should wrap around (red at both ends)
        var first = spectrum.First();
        var last = spectrum.Last();
        Assert.AreEqual(first.R, last.R);
        Assert.AreEqual(first.G, last.G);
        Assert.AreEqual(first.B, last.B);
    }

    [TestMethod]
    public void GenerateHsvSpectrum_ContainsExpectedNumberOfColors()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert - Based on implementation: 30 colors (0 to 28 + 1 for wrap)
        Assert.AreEqual(30, spectrum.Count);
    }

    [TestMethod]
    public void GenerateHsvSpectrum_AllColorsHaveFullAlpha()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert
        foreach (var color in spectrum)
        {
            Assert.AreEqual(255, color.A);
        }
    }

    [TestMethod]
    public void GenerateHsvSpectrum_ContainsRed()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert - Should contain pure red (at Hue = 0)
        Assert.IsTrue(spectrum.Any(c => c.R == 255 && c.G == 0 && c.B == 0));
    }

    [TestMethod]
    public void GenerateHsvSpectrum_ContainsGreen()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert - Should contain pure green (at Hue = 120)
        Assert.IsTrue(spectrum.Any(c => c.R == 0 && c.G == 255 && c.B == 0));
    }

    [TestMethod]
    public void GenerateHsvSpectrum_ContainsBlue()
    {
        // Act
        var spectrum = ColorPicker.GenerateHsvSpectrum();

        // Assert - Should contain pure blue (at Hue = 240)
        Assert.IsTrue(spectrum.Any(c => c.R == 0 && c.G == 0 && c.B == 255));
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [TestMethod]
    [DataRow(0d)]
    [DataRow(60d)]
    [DataRow(120d)]
    [DataRow(180d)]
    [DataRow(240d)]
    [DataRow(300d)]
    [DataRow(360d)]
    public void ConvertHsvToRgb_HueBoundaries_DoesNotThrow(double hue)
    {
        // Act & Assert - Should not throw
        var color = ColorPicker.ConvertHsvToRgb(hue, 1, 1);
        Assert.IsTrue(color.R >= 0 && color.R <= 255);
        Assert.IsTrue(color.G >= 0 && color.G <= 255);
        Assert.IsTrue(color.B >= 0 && color.B <= 255);
    }

    [TestMethod]
    [DataRow(0d)]
    [DataRow(0.25)]
    [DataRow(0.5)]
    [DataRow(0.75)]
    [DataRow(1d)]
    public void ConvertHsvToRgb_SaturationBoundaries_DoesNotThrow(double saturation)
    {
        // Act & Assert - Should not throw
        var color = ColorPicker.ConvertHsvToRgb(180, saturation, 1);
        Assert.IsTrue(color.R >= 0 && color.R <= 255);
        Assert.IsTrue(color.G >= 0 && color.G <= 255);
        Assert.IsTrue(color.B >= 0 && color.B <= 255);
    }

    [TestMethod]
    [DataRow(0d)]
    [DataRow(0.25)]
    [DataRow(0.5)]
    [DataRow(0.75)]
    [DataRow(1d)]
    public void ConvertHsvToRgb_ValueBoundaries_DoesNotThrow(double value)
    {
        // Act & Assert - Should not throw
        var color = ColorPicker.ConvertHsvToRgb(180, 1, value);
        Assert.IsTrue(color.R >= 0 && color.R <= 255);
        Assert.IsTrue(color.G >= 0 && color.G <= 255);
        Assert.IsTrue(color.B >= 0 && color.B <= 255);
    }

    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(255, 255, 255)]
    [DataRow(128, 128, 128)]
    [DataRow(255, 0, 0)]
    [DataRow(0, 255, 0)]
    [DataRow(0, 0, 255)]
    public void ConvertRgbToHsv_BoundaryValues_DoesNotThrow(int r, int g, int b)
    {
        // Act & Assert - Should not throw
        var hsv = ColorPicker.ConvertRgbToHsv(r, g, b);
        Assert.IsTrue(hsv.H >= 0 && hsv.H <= 360);
        Assert.IsTrue(hsv.S >= 0 && hsv.S <= 1);
        Assert.IsTrue(hsv.V >= 0 && hsv.V <= 1);
    }

    #endregion

    #region Specific Color Tests

    [TestMethod]
    public void ConvertHsvToRgb_Orange_ReturnsCorrectColor()
    {
        // Arrange - Orange is approximately Hue = 30
        double h = 30, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.AreEqual(255, color.R);
        Assert.IsTrue(color.G > 100 && color.G < 150); // Orange green component
        Assert.AreEqual(0, color.B);
    }

    [TestMethod]
    public void ConvertHsvToRgb_Purple_ReturnsCorrectColor()
    {
        // Arrange - Purple is approximately Hue = 270
        double h = 270, s = 1, v = 1;

        // Act
        var color = ColorPicker.ConvertHsvToRgb(h, s, v);

        // Assert
        Assert.IsTrue(color.R > 100);
        Assert.AreEqual(0, color.G);
        Assert.AreEqual(255, color.B);
    }

    #endregion
}
