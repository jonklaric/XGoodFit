using System.Globalization;
using System.Windows;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for HorizontalAlignmentToTextAlignmentConverter.
/// </summary>
public class HorizontalAlignmentToTextAlignmentConverterTests
{
    /// <summary>
    /// Converter instance used for testing.
    /// </summary>
    private readonly HorizontalAlignmentToTextAlignmentConverter _converter = HorizontalAlignmentToTextAlignmentConverter.Instance;

    /// <summary>
    /// Tests convert left returnstextalignmentleft.
    /// </summary>
    [Fact]
    public void Convert_Left_ReturnsTextAlignmentLeft()
    {
        var result = _converter.Convert(HorizontalAlignment.Left, typeof(TextAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(TextAlignment.Left, result);
    }

    /// <summary>
    /// Tests convert right returnstextalignmentright.
    /// </summary>
    [Fact]
    public void Convert_Right_ReturnsTextAlignmentRight()
    {
        var result = _converter.Convert(HorizontalAlignment.Right, typeof(TextAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(TextAlignment.Right, result);
    }

    /// <summary>
    /// Tests convert center returnstextalignmentcenter.
    /// </summary>
    [Fact]
    public void Convert_Center_ReturnsTextAlignmentCenter()
    {
        var result = _converter.Convert(HorizontalAlignment.Center, typeof(TextAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(TextAlignment.Center, result);
    }

    /// <summary>
    /// Tests convert stretch returnstextalignmentcenter.
    /// </summary>
    [Fact]
    public void Convert_Stretch_ReturnsTextAlignmentCenter()
    {
        var result = _converter.Convert(HorizontalAlignment.Stretch, typeof(TextAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(TextAlignment.Center, result);
    }

    /// <summary>
    /// Tests convert invalidtype returnstextalignmentcenter.
    /// </summary>
    [Fact]
    public void Convert_InvalidType_ReturnsTextAlignmentCenter()
    {
        var result = _converter.Convert("invalid", typeof(TextAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(TextAlignment.Center, result);
    }

    /// <summary>
    /// Tests convert null returnstextalignmentcenter.
    /// </summary>
    [Fact]
    public void Convert_Null_ReturnsTextAlignmentCenter()
    {
        var result = _converter.Convert(null, typeof(TextAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(TextAlignment.Center, result);
    }

    /// <summary>
    /// Tests convertback left returnshorizontalalignmentleft.
    /// </summary>
    [Fact]
    public void ConvertBack_Left_ReturnsHorizontalAlignmentLeft()
    {
        var result = _converter.ConvertBack(TextAlignment.Left, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Left, result);
    }

    /// <summary>
    /// Tests convertback right returnshorizontalalignmentright.
    /// </summary>
    [Fact]
    public void ConvertBack_Right_ReturnsHorizontalAlignmentRight()
    {
        var result = _converter.ConvertBack(TextAlignment.Right, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Right, result);
    }

    /// <summary>
    /// Tests convertback center returnshorizontalalignmentcenter.
    /// </summary>
    [Fact]
    public void ConvertBack_Center_ReturnsHorizontalAlignmentCenter()
    {
        var result = _converter.ConvertBack(TextAlignment.Center, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Center, result);
    }

    /// <summary>
    /// Tests convertback justify returnshorizontalalignmentcenter.
    /// </summary>
    [Fact]
    public void ConvertBack_Justify_ReturnsHorizontalAlignmentCenter()
    {
        var result = _converter.ConvertBack(TextAlignment.Justify, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Center, result);
    }

    /// <summary>
    /// Tests convertback invalidtype returnshorizontalalignmentcenter.
    /// </summary>
    [Fact]
    public void ConvertBack_InvalidType_ReturnsHorizontalAlignmentCenter()
    {
        var result = _converter.ConvertBack("invalid", typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Center, result);
    }

    /// <summary>
    /// Tests convertback null returnshorizontalalignmentcenter.
    /// </summary>
    [Fact]
    public void ConvertBack_Null_ReturnsHorizontalAlignmentCenter()
    {
        var result = _converter.ConvertBack(null, typeof(HorizontalAlignment), null, CultureInfo.InvariantCulture);
        Assert.Equal(HorizontalAlignment.Center, result);
    }

    /// <summary>
    /// Tests instance issingleton.
    /// </summary>
    [Fact]
    public void Instance_IsSingleton()
    {
        Assert.Same(HorizontalAlignmentToTextAlignmentConverter.Instance, HorizontalAlignmentToTextAlignmentConverter.Instance);
    }
}
