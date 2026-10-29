using System.Globalization;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for StringToDoubleConverter.
/// </summary>
public class StringToDoubleConverterTests
{
    /// <summary>
    /// Converter instance used for testing.
    /// </summary>
    private readonly StringToDoubleConverter _converter = new();

    /// <summary>
    /// Tests convert withdouble returnsstring.
    /// </summary>
    [Fact]
    public void Convert_WithDouble_ReturnsString()
    {
        var result = _converter.Convert(123.45, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("123.45", result);
    }

    /// <summary>
    /// Tests convert withinteger returnsstring.
    /// </summary>
    [Fact]
    public void Convert_WithInteger_ReturnsString()
    {
        var result = _converter.Convert(42, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("42", result);
    }

    /// <summary>
    /// Tests convert withnull returnsnull.
    /// </summary>
    [Fact]
    public void Convert_WithNull_ReturnsNull()
    {
        var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convert withzero returnszerostring.
    /// </summary>
    [Fact]
    public void Convert_WithZero_ReturnsZeroString()
    {
        var result = _converter.Convert(0.0, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("0", result);
    }

    /// <summary>
    /// Tests convert withnegativenumber returnsnegativestring.
    /// </summary>
    [Fact]
    public void Convert_WithNegativeNumber_ReturnsNegativeString()
    {
        var result = _converter.Convert(-99.5, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("-99.5", result);
    }

    /// <summary>
    /// Tests convertback withvalidstring returnsdouble.
    /// </summary>
    [Fact]
    public void ConvertBack_WithValidString_ReturnsDouble()
    {
        var result = _converter.ConvertBack("123.45", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(123.45, result);
    }

    /// <summary>
    /// Tests convertback withnull returnsnull.
    /// </summary>
    [Fact]
    public void ConvertBack_WithNull_ReturnsNull()
    {
        var result = _converter.ConvertBack(null, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convertback withinvalidstring returnszero.
    /// </summary>
    [Fact]
    public void ConvertBack_WithInvalidString_ReturnsZero()
    {
        var result = _converter.ConvertBack("not a number", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(0.0, result);
    }

    /// <summary>
    /// Tests convertback withemptystring returnszero.
    /// </summary>
    [Fact]
    public void ConvertBack_WithEmptyString_ReturnsZero()
    {
        var result = _converter.ConvertBack("", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(0.0, result);
    }

    /// <summary>
    /// Tests convertback withscientificnotation returnsdouble.
    /// </summary>
    [Fact]
    public void ConvertBack_WithScientificNotation_ReturnsDouble()
    {
        var result = _converter.ConvertBack("1.5e2", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(150.0, result);
    }

    /// <summary>
    /// Tests convertback withnegativenumber returnsnegativedouble.
    /// </summary>
    [Fact]
    public void ConvertBack_WithNegativeNumber_ReturnsNegativeDouble()
    {
        var result = _converter.ConvertBack("-42.5", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(-42.5, result);
    }
}
