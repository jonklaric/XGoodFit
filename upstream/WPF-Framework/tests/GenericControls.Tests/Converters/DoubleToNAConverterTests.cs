using System.Globalization;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for DoubleToNAConverter.
/// </summary>
public class DoubleToNAConverterTests
{
    /// <summary>
    /// Converter instance used for testing.
    /// </summary>
    private readonly DoubleToNAConverter _converter = new();

    /// <summary>
    /// Tests convert withvaliddouble returnsdouble.
    /// </summary>
    [Fact]
    public void Convert_WithValidDouble_ReturnsDouble()
    {
        var result = _converter.Convert(123.45, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal(123.45, result);
    }

    /// <summary>
    /// Tests convert withnull returnsna.
    /// </summary>
    [Fact]
    public void Convert_WithNull_ReturnsNA()
    {
        var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("N/A", result);
    }

    /// <summary>
    /// Tests convert withnan returnsna.
    /// </summary>
    [Fact]
    public void Convert_WithNaN_ReturnsNA()
    {
        var result = _converter.Convert(double.NaN, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("N/A", result);
    }

    /// <summary>
    /// Tests convert withpositiveinfinity returnsplusinfinity.
    /// </summary>
    [Fact]
    public void Convert_WithPositiveInfinity_ReturnsPlusInfinity()
    {
        var result = _converter.Convert(double.PositiveInfinity, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("+\u221E", result); // +infinity symbol
    }

    /// <summary>
    /// Tests convert withnegativeinfinity returnsminusinfinity.
    /// </summary>
    [Fact]
    public void Convert_WithNegativeInfinity_ReturnsMinusInfinity()
    {
        var result = _converter.Convert(double.NegativeInfinity, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("-\u221E", result); // -infinity symbol
    }

    /// <summary>
    /// Tests convert withinvalidtype returnsna.
    /// </summary>
    [Fact]
    public void Convert_WithInvalidType_ReturnsNA()
    {
        var result = _converter.Convert("not a double", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("N/A", result);
    }

    /// <summary>
    /// Tests convert withzero returnszero.
    /// </summary>
    [Fact]
    public void Convert_WithZero_ReturnsZero()
    {
        var result = _converter.Convert(0.0, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal(0.0, result);
    }

    /// <summary>
    /// Tests convert withnegativenumber returnsnumber.
    /// </summary>
    [Fact]
    public void Convert_WithNegativeNumber_ReturnsNumber()
    {
        var result = _converter.Convert(-50.5, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal(-50.5, result);
    }

    /// <summary>
    /// Tests convertback withnull returnsnan.
    /// </summary>
    [Fact]
    public void ConvertBack_WithNull_ReturnsNaN()
    {
        var result = _converter.ConvertBack(null, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.True(double.IsNaN((double)result!));
    }

    /// <summary>
    /// Tests convertback withna returnsnan.
    /// </summary>
    [Fact]
    public void ConvertBack_WithNA_ReturnsNaN()
    {
        var result = _converter.ConvertBack("N/A", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.True(double.IsNaN((double)result!));
    }

    /// <summary>
    /// Tests convertback withnanoslash returnsnan.
    /// </summary>
    [Fact]
    public void ConvertBack_WithNANoSlash_ReturnsNaN()
    {
        var result = _converter.ConvertBack("NA", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.True(double.IsNaN((double)result!));
    }

    /// <summary>
    /// Tests convertback withinfinitysymbol returnspositiveinfinity.
    /// </summary>
    [Fact]
    public void ConvertBack_WithInfinitySymbol_ReturnsPositiveInfinity()
    {
        var result = _converter.ConvertBack("\u221E", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.True(double.IsPositiveInfinity((double)result!));
    }

    /// <summary>
    /// Tests convertback withnegativeinfinitysymbol returnsnegativeinfinity.
    /// </summary>
    [Fact]
    public void ConvertBack_WithNegativeInfinitySymbol_ReturnsNegativeInfinity()
    {
        var result = _converter.ConvertBack("-\u221E", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.True(double.IsNegativeInfinity((double)result!));
    }

    /// <summary>
    /// Tests convertback withvalidnumber returnsdouble.
    /// </summary>
    [Fact]
    public void ConvertBack_WithValidNumber_ReturnsDouble()
    {
        var result = _converter.ConvertBack("42.5", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(42.5, result);
    }

    /// <summary>
    /// Tests convertback withinvalidstring returnsnan.
    /// </summary>
    [Fact]
    public void ConvertBack_WithInvalidString_ReturnsNaN()
    {
        var result = _converter.ConvertBack("invalid", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.True(double.IsNaN((double)result!));
    }

    /// <summary>
    /// Tests convertback withnonstringtype returnsvalue.
    /// </summary>
    [Fact]
    public void ConvertBack_WithNonStringType_ReturnsValue()
    {
        var result = _converter.ConvertBack(123, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(123, result);
    }

    /// <summary>
    /// Tests convertback withinftext returnspositiveinfinity.
    /// </summary>
    [Fact]
    public void ConvertBack_WithInfText_ReturnsPositiveInfinity()
    {
        var result = _converter.ConvertBack("inf", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.True(double.IsPositiveInfinity((double)result!));
    }

    /// <summary>
    /// Tests convertback withnegativeinftext returnsnegativeinfinity.
    /// </summary>
    [Fact]
    public void ConvertBack_WithNegativeInfText_ReturnsNegativeInfinity()
    {
        var result = _converter.ConvertBack("-inf", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.True(double.IsNegativeInfinity((double)result!));
    }
}
