using System.Globalization;
using System.Windows;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for ThicknessToDoubleConverter.
/// </summary>
public class ThicknessToDoubleConverterTests
{
    /// <summary>
    /// Converter instance used for testing.
    /// </summary>
    private readonly ThicknessToDoubleConverter _converter = new();

    /// <summary>
    /// Tests convert withuniformthickness returnsvalue.
    /// </summary>
    [Fact]
    public void Convert_WithUniformThickness_ReturnsValue()
    {
        var thickness = new Thickness(10);
        var result = _converter.Convert(thickness, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(10.0, result);
    }

    /// <summary>
    /// Tests convert withnonuniformthickness returnsaverage.
    /// </summary>
    [Fact]
    public void Convert_WithNonUniformThickness_ReturnsAverage()
    {
        var thickness = new Thickness(10, 20, 30, 40);
        var result = _converter.Convert(thickness, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(25.0, result); // (10 + 20 + 30 + 40) / 4
    }

    /// <summary>
    /// Tests convert withnull returnsnull.
    /// </summary>
    [Fact]
    public void Convert_WithNull_ReturnsNull()
    {
        var result = _converter.Convert(null, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convert withzerothickness returnszero.
    /// </summary>
    [Fact]
    public void Convert_WithZeroThickness_ReturnsZero()
    {
        var thickness = new Thickness(0);
        var result = _converter.Convert(thickness, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(0.0, result);
    }

    /// <summary>
    /// Tests convertback withdouble returnsuniformthickness.
    /// </summary>
    [Fact]
    public void ConvertBack_WithDouble_ReturnsUniformThickness()
    {
        var result = (Thickness)_converter.ConvertBack(15.0, typeof(Thickness), null, CultureInfo.InvariantCulture)!;
        Assert.Equal(new Thickness(15), result);
    }

    /// <summary>
    /// Tests convertback withnull returnsnull.
    /// </summary>
    [Fact]
    public void ConvertBack_WithNull_ReturnsNull()
    {
        var result = _converter.ConvertBack(null, typeof(Thickness), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convertback withinvalidstring returnsnull.
    /// </summary>
    [Fact]
    public void ConvertBack_WithInvalidString_ReturnsNull()
    {
        var result = _converter.ConvertBack("invalid", typeof(Thickness), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convertback withstringnumber returnsthickness.
    /// </summary>
    [Fact]
    public void ConvertBack_WithStringNumber_ReturnsThickness()
    {
        var result = (Thickness)_converter.ConvertBack("20", typeof(Thickness), null, CultureInfo.InvariantCulture)!;
        Assert.Equal(new Thickness(20), result);
    }
}
