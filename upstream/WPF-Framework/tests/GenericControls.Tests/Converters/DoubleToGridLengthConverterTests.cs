using System.Globalization;
using System.Windows;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for DoubleToGridLengthConverter.
/// </summary>
public class DoubleToGridLengthConverterTests
{
    /// <summary>
    /// Converter instance used for testing.
    /// </summary>
    private readonly DoubleToGridLengthConverter _converter = new();

    /// <summary>
    /// Tests convert doubletogridlength returnsgridlength.
    /// </summary>
    [Fact]
    public void Convert_DoubleToGridLength_ReturnsGridLength()
    {
        var result = _converter.Convert(100.0, typeof(GridLength), null, CultureInfo.InvariantCulture);
        Assert.IsType<GridLength>(result);
        Assert.Equal(100.0, ((GridLength)result).Value);
    }

    /// <summary>
    /// Tests convert gridlengthtodouble returnsdouble.
    /// </summary>
    [Fact]
    public void Convert_GridLengthToDouble_ReturnsDouble()
    {
        var gridLength = new GridLength(150.0);
        var result = _converter.Convert(gridLength, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(150.0, result);
    }

    /// <summary>
    /// Tests convert nulltogridlength returnsauto.
    /// </summary>
    [Fact]
    public void Convert_NullToGridLength_ReturnsAuto()
    {
        var result = _converter.Convert(null, typeof(GridLength), null, CultureInfo.InvariantCulture);
        Assert.Equal(GridLength.Auto, result);
    }

    /// <summary>
    /// Tests convert invalidtypetogridlength returnsauto.
    /// </summary>
    [Fact]
    public void Convert_InvalidTypeToGridLength_ReturnsAuto()
    {
        var result = _converter.Convert("invalid", typeof(GridLength), null, CultureInfo.InvariantCulture);
        Assert.Equal(GridLength.Auto, result);
    }

    /// <summary>
    /// Tests convert invalidtypetodouble returnsnan.
    /// </summary>
    [Fact]
    public void Convert_InvalidTypeToDouble_ReturnsNaN()
    {
        var result = _converter.Convert("invalid", typeof(double), null, CultureInfo.InvariantCulture);
        Assert.True(double.IsNaN((double)result!));
    }

    /// <summary>
    /// Tests convert withothertargettype returnsnull.
    /// </summary>
    [Fact]
    public void Convert_WithOtherTargetType_ReturnsNull()
    {
        var result = _converter.Convert(100.0, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convertback doubletogridlength returnsgridlength.
    /// </summary>
    [Fact]
    public void ConvertBack_DoubleToGridLength_ReturnsGridLength()
    {
        var result = _converter.ConvertBack(75.0, typeof(GridLength), null, CultureInfo.InvariantCulture);
        Assert.IsType<GridLength>(result);
        Assert.Equal(75.0, ((GridLength)result).Value);
    }

    /// <summary>
    /// Tests convertback gridlengthtodouble returnsdouble.
    /// </summary>
    [Fact]
    public void ConvertBack_GridLengthToDouble_ReturnsDouble()
    {
        var gridLength = new GridLength(200.0);
        var result = _converter.ConvertBack(gridLength, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(200.0, result);
    }

    /// <summary>
    /// Tests convert zerodouble returnsgridlengthwithzero.
    /// </summary>
    [Fact]
    public void Convert_ZeroDouble_ReturnsGridLengthWithZero()
    {
        var result = _converter.Convert(0.0, typeof(GridLength), null, CultureInfo.InvariantCulture);
        Assert.IsType<GridLength>(result);
        Assert.Equal(0.0, ((GridLength)result).Value);
    }
}
