using System.Globalization;
using System.Windows;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for DoubleToCornerRadiusConverter.
/// </summary>
public class DoubleToCornerRadiusConverterTests
{
    /// <summary>
    /// Converter instance used for testing.
    /// </summary>
    private readonly DoubleToCornerRadiusConverter _converter = new();

    /// <summary>
    /// Tests convert doubletocornerradius returnsuniformcornerradius.
    /// </summary>
    [Fact]
    public void Convert_DoubleToCornerRadius_ReturnsUniformCornerRadius()
    {
        var result = _converter.Convert(10.0, typeof(CornerRadius), null, CultureInfo.InvariantCulture);
        Assert.IsType<CornerRadius>(result);
        var cr = (CornerRadius)result;
        Assert.Equal(10.0, cr.TopLeft);
        Assert.Equal(10.0, cr.TopRight);
        Assert.Equal(10.0, cr.BottomRight);
        Assert.Equal(10.0, cr.BottomLeft);
    }

    /// <summary>
    /// Tests convert cornerradiustodouble returnsbottomleft.
    /// </summary>
    [Fact]
    public void Convert_CornerRadiusToDouble_ReturnsBottomLeft()
    {
        var cornerRadius = new CornerRadius(5, 10, 15, 20);
        var result = _converter.Convert(cornerRadius, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(20.0, result); // BottomLeft
    }

    /// <summary>
    /// Tests convert nulltocornerradius returnsemptycornerradius.
    /// </summary>
    [Fact]
    public void Convert_NullToCornerRadius_ReturnsEmptyCornerRadius()
    {
        var result = _converter.Convert(null, typeof(CornerRadius), null, CultureInfo.InvariantCulture);
        Assert.Equal(new CornerRadius(), result);
    }

    /// <summary>
    /// Tests convert invalidtypetocornerradius returnsemptycornerradius.
    /// </summary>
    [Fact]
    public void Convert_InvalidTypeToCornerRadius_ReturnsEmptyCornerRadius()
    {
        var result = _converter.Convert("invalid", typeof(CornerRadius), null, CultureInfo.InvariantCulture);
        Assert.Equal(new CornerRadius(), result);
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
        var result = _converter.Convert(10.0, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convertback doubletocornerradius returnscornerradius.
    /// </summary>
    [Fact]
    public void ConvertBack_DoubleToCornerRadius_ReturnsCornerRadius()
    {
        var result = _converter.ConvertBack(15.0, typeof(CornerRadius), null, CultureInfo.InvariantCulture);
        Assert.IsType<CornerRadius>(result);
        var cr = (CornerRadius)result;
        Assert.Equal(15.0, cr.TopLeft);
    }

    /// <summary>
    /// Tests convertback cornerradiustodouble returnsdouble.
    /// </summary>
    [Fact]
    public void ConvertBack_CornerRadiusToDouble_ReturnsDouble()
    {
        var cornerRadius = new CornerRadius(25);
        var result = _converter.ConvertBack(cornerRadius, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(25.0, result);
    }

    /// <summary>
    /// Tests convert zerodouble returnszerocornerradius.
    /// </summary>
    [Fact]
    public void Convert_ZeroDouble_ReturnsZeroCornerRadius()
    {
        var result = _converter.Convert(0.0, typeof(CornerRadius), null, CultureInfo.InvariantCulture);
        Assert.IsType<CornerRadius>(result);
        var cr = (CornerRadius)result;
        Assert.Equal(0.0, cr.TopLeft);
    }
}
