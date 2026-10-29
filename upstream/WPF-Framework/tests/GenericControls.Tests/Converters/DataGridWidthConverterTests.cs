using System.Globalization;
using System.Windows;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for DataGridWidthConverter.
/// </summary>
public class DataGridWidthConverterTests
{
    /// <summary>
    /// Converter instance used for testing.
    /// </summary>
    private readonly DataGridWidthConverter _converter = new();

    /// <summary>
    /// Tests convert withvalidwidth returnsadjustedwidth.
    /// </summary>
    [Fact]
    public void Convert_WithValidWidth_ReturnsAdjustedWidth()
    {
        var scrollBarWidth = SystemParameters.VerticalScrollBarWidth;
        var result = _converter.Convert(500.0, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(500.0 - scrollBarWidth, result);
    }

    /// <summary>
    /// Tests convert withnull returnszero.
    /// </summary>
    [Fact]
    public void Convert_WithNull_ReturnsZero()
    {
        var result = _converter.Convert(null, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(0.0, result);
    }

    /// <summary>
    /// Tests convert withinvalidstring returnszero.
    /// </summary>
    [Fact]
    public void Convert_WithInvalidString_ReturnsZero()
    {
        var result = _converter.Convert("invalid", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(0.0, result);
    }

    /// <summary>
    /// Tests convert withstringnumber returnsadjustedwidth.
    /// </summary>
    [Fact]
    public void Convert_WithStringNumber_ReturnsAdjustedWidth()
    {
        var scrollBarWidth = SystemParameters.VerticalScrollBarWidth;
        var result = _converter.Convert("300", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(300.0 - scrollBarWidth, result);
    }

    /// <summary>
    /// Tests convert withzero returnsnegativescrollbarwidth.
    /// </summary>
    [Fact]
    public void Convert_WithZero_ReturnsNegativeScrollBarWidth()
    {
        var scrollBarWidth = SystemParameters.VerticalScrollBarWidth;
        var result = _converter.Convert(0.0, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(-scrollBarWidth, result);
    }

    /// <summary>
    /// Tests convertback throwsnotsupportedexception.
    /// </summary>
    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(100.0, typeof(double), null, CultureInfo.InvariantCulture));
    }
}
