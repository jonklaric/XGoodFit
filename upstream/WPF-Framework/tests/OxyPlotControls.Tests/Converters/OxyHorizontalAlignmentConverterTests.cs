using System.Globalization;
using System.Windows;
using Xunit;

namespace OxyPlotControls.Tests.Converters;

/// <summary>
/// Tests for OxyHorizontalAlignmentConverter.
/// Verifies pass-through and swap behavior for WPF HorizontalAlignment values.
/// </summary>
public class OxyHorizontalAlignmentConverterTests
{
    private readonly OxyHorizontalAlignmentConverter _converter = new();

    #region Convert Tests

    [Fact]
    public void Convert_NullValue_ReturnsCenter()
    {
        var result = _converter.Convert(null, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Center, result);
    }

    [Fact]
    public void Convert_WrongType_ReturnsCenter()
    {
        var result = _converter.Convert("not an alignment", typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Center, result);
    }

    [Fact]
    public void Convert_Left_ReturnsLeft()
    {
        var result = _converter.Convert(HorizontalAlignment.Left, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Left, result);
    }

    [Fact]
    public void Convert_Center_ReturnsCenter()
    {
        var result = _converter.Convert(HorizontalAlignment.Center, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Center, result);
    }

    [Fact]
    public void Convert_Right_ReturnsRight()
    {
        var result = _converter.Convert(HorizontalAlignment.Right, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Right, result);
    }

    [Fact]
    public void Convert_Stretch_ReturnsStretch()
    {
        var result = _converter.Convert(HorizontalAlignment.Stretch, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Stretch, result);
    }

    #endregion

    #region Convert with Swap Tests

    [Fact]
    public void Convert_SwapLeft_ReturnsRight()
    {
        var result = _converter.Convert(HorizontalAlignment.Left, typeof(HorizontalAlignment), "Swap", CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Right, result);
    }

    [Fact]
    public void Convert_SwapRight_ReturnsLeft()
    {
        var result = _converter.Convert(HorizontalAlignment.Right, typeof(HorizontalAlignment), "Swap", CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Left, result);
    }

    [Fact]
    public void Convert_SwapCenter_ReturnsCenter()
    {
        var result = _converter.Convert(HorizontalAlignment.Center, typeof(HorizontalAlignment), "Swap", CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Center, result);
    }

    #endregion

    #region ConvertBack Tests

    [Fact]
    public void ConvertBack_NullValue_ReturnsCenter()
    {
        var result = _converter.ConvertBack(null, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Center, result);
    }

    [Fact]
    public void ConvertBack_WrongType_ReturnsCenter()
    {
        var result = _converter.ConvertBack("not an alignment", typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Center, result);
    }

    [Fact]
    public void ConvertBack_Left_ReturnsLeft()
    {
        var result = _converter.ConvertBack(HorizontalAlignment.Left, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Left, result);
    }

    [Fact]
    public void ConvertBack_Right_ReturnsRight()
    {
        var result = _converter.ConvertBack(HorizontalAlignment.Right, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Right, result);
    }

    [Fact]
    public void ConvertBack_SwapLeft_ReturnsRight()
    {
        var result = _converter.ConvertBack(HorizontalAlignment.Left, typeof(HorizontalAlignment), "Swap", CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Right, result);
    }

    [Fact]
    public void ConvertBack_SwapRight_ReturnsLeft()
    {
        var result = _converter.ConvertBack(HorizontalAlignment.Right, typeof(HorizontalAlignment), "Swap", CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Left, result);
    }

    #endregion

    #region Round Trip Tests

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    [InlineData(HorizontalAlignment.Stretch)]
    public void Convert_ConvertBack_RoundTrip(HorizontalAlignment alignment)
    {
        var converted = _converter.Convert(alignment, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        var roundTripped = _converter.ConvertBack(converted, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(alignment, roundTripped);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void Convert_ConvertBack_SwapRoundTrip(HorizontalAlignment alignment)
    {
        var converted = _converter.Convert(alignment, typeof(HorizontalAlignment), "Swap", CultureInfo.InvariantCulture);
        var roundTripped = _converter.ConvertBack(converted, typeof(HorizontalAlignment), "Swap", CultureInfo.InvariantCulture);
        Assert.Equal(alignment, roundTripped);
    }

    #endregion
}
