using System.Globalization;
using System.Windows.Controls;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for DoubleToDataGridLengthConverter.
/// </summary>
public class DoubleToDataGridLengthConverterTests
{
    /// <summary>
    /// Converter instance used for testing.
    /// </summary>
    private readonly DoubleToDataGridLengthConverter _converter = new();

    /// <summary>
    /// Tests convert doubletodatagridlength returnsdatagridlength.
    /// </summary>
    [Fact]
    public void Convert_DoubleToDataGridLength_ReturnsDataGridLength()
    {
        var result = _converter.Convert(100.0, typeof(DataGridLength), null, CultureInfo.InvariantCulture);
        Assert.IsType<DataGridLength>(result);
        Assert.Equal(100.0, ((DataGridLength)result).Value);
    }

    /// <summary>
    /// Tests convert datagridlengthtodouble returnsdouble.
    /// </summary>
    [Fact]
    public void Convert_DataGridLengthToDouble_ReturnsDouble()
    {
        var dgLength = new DataGridLength(150.0);
        var result = _converter.Convert(dgLength, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(150.0, result);
    }

    /// <summary>
    /// Tests convert nulltodatagridlength returnsauto.
    /// </summary>
    [Fact]
    public void Convert_NullToDataGridLength_ReturnsAuto()
    {
        var result = _converter.Convert(null, typeof(DataGridLength), null, CultureInfo.InvariantCulture);
        Assert.Equal(DataGridLength.Auto, result);
    }

    /// <summary>
    /// Tests convert invalidtypetodatagridlength returnsauto.
    /// </summary>
    [Fact]
    public void Convert_InvalidTypeToDataGridLength_ReturnsAuto()
    {
        var result = _converter.Convert("invalid", typeof(DataGridLength), null, CultureInfo.InvariantCulture);
        Assert.Equal(DataGridLength.Auto, result);
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
    /// Tests convertback doubletodatagridlength returnsdatagridlength.
    /// </summary>
    [Fact]
    public void ConvertBack_DoubleToDataGridLength_ReturnsDataGridLength()
    {
        var result = _converter.ConvertBack(75.0, typeof(DataGridLength), null, CultureInfo.InvariantCulture);
        Assert.IsType<DataGridLength>(result);
        Assert.Equal(75.0, ((DataGridLength)result).Value);
    }

    /// <summary>
    /// Tests convertback datagridlengthtodouble returnsdouble.
    /// </summary>
    [Fact]
    public void ConvertBack_DataGridLengthToDouble_ReturnsDouble()
    {
        var dgLength = new DataGridLength(200.0);
        var result = _converter.ConvertBack(dgLength, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(200.0, result);
    }

    /// <summary>
    /// Tests convert zerodouble returnsdatagridlengthwithzero.
    /// </summary>
    [Fact]
    public void Convert_ZeroDouble_ReturnsDataGridLengthWithZero()
    {
        var result = _converter.Convert(0.0, typeof(DataGridLength), null, CultureInfo.InvariantCulture);
        Assert.IsType<DataGridLength>(result);
        Assert.Equal(0.0, ((DataGridLength)result).Value);
    }
}
