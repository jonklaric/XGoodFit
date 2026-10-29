using System.Globalization;
using System.Windows;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for DoubleToThicknessConverter.
/// </summary>
public class DoubleToThicknessConverterTests
{
    [Fact]
    public void Convert_WithDouble_AllSidesEnabled_ReturnsUniformThickness()
    {
        var converter = new DoubleToThicknessConverter
        {
            IsLeft = true,
            IsTop = true,
            IsRight = true,
            IsBottom = true
        };
        var result = (Thickness)converter.Convert(10.0, typeof(Thickness), null, CultureInfo.InvariantCulture)!;
        Assert.Equal(new Thickness(10, 10, 10, 10), result);
    }

    /// <summary>
    /// Tests convert withdouble onlyleftenabled returnsleftonlythickness.
    /// </summary>
    [Fact]
    public void Convert_WithDouble_OnlyLeftEnabled_ReturnsLeftOnlyThickness()
    {
        var converter = new DoubleToThicknessConverter
        {
            IsLeft = true,
            IsTop = false,
            IsRight = false,
            IsBottom = false,
            Top = 5,
            Right = 5,
            Bottom = 5
        };
        var result = (Thickness)converter.Convert(10.0, typeof(Thickness), null, CultureInfo.InvariantCulture)!;
        Assert.Equal(10, result.Left);
        Assert.Equal(5, result.Top);
        Assert.Equal(5, result.Right);
        Assert.Equal(5, result.Bottom);
    }

    /// <summary>
    /// Tests convert withnull returnsnull.
    /// </summary>
    [Fact]
    public void Convert_WithNull_ReturnsNull()
    {
        var converter = new DoubleToThicknessConverter();
        var result = converter.Convert(null, typeof(Thickness), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convert withinvalidstring returnsnull.
    /// </summary>
    [Fact]
    public void Convert_WithInvalidString_ReturnsNull()
    {
        var converter = new DoubleToThicknessConverter();
        var result = converter.Convert("invalid", typeof(Thickness), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convert withstringnumber returnsthickness.
    /// </summary>
    [Fact]
    public void Convert_WithStringNumber_ReturnsThickness()
    {
        var converter = new DoubleToThicknessConverter();
        var result = (Thickness)converter.Convert("15", typeof(Thickness), null, CultureInfo.InvariantCulture)!;
        Assert.Equal(new Thickness(15, 15, 15, 15), result);
    }

    /// <summary>
    /// Tests convertback withthickness leftenabled returnsleft.
    /// </summary>
    [Fact]
    public void ConvertBack_WithThickness_LeftEnabled_ReturnsLeft()
    {
        var converter = new DoubleToThicknessConverter { IsLeft = true };
        var thickness = new Thickness(10, 20, 30, 40);
        var result = converter.ConvertBack(thickness, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(10.0, result);
    }

    /// <summary>
    /// Tests convertback withthickness rightenabled returnsright.
    /// </summary>
    [Fact]
    public void ConvertBack_WithThickness_RightEnabled_ReturnsRight()
    {
        var converter = new DoubleToThicknessConverter
        {
            IsLeft = false,
            IsRight = true
        };
        var thickness = new Thickness(10, 20, 30, 40);
        var result = converter.ConvertBack(thickness, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(30.0, result);
    }

    /// <summary>
    /// Tests convertback withthickness topenabled returnstop.
    /// </summary>
    [Fact]
    public void ConvertBack_WithThickness_TopEnabled_ReturnsTop()
    {
        var converter = new DoubleToThicknessConverter
        {
            IsLeft = false,
            IsRight = false,
            IsTop = true
        };
        var thickness = new Thickness(10, 20, 30, 40);
        var result = converter.ConvertBack(thickness, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(20.0, result);
    }

    /// <summary>
    /// Tests convertback withthickness bottomenabled returnsbottom.
    /// </summary>
    [Fact]
    public void ConvertBack_WithThickness_BottomEnabled_ReturnsBottom()
    {
        var converter = new DoubleToThicknessConverter
        {
            IsLeft = false,
            IsRight = false,
            IsTop = false,
            IsBottom = true
        };
        var thickness = new Thickness(10, 20, 30, 40);
        var result = converter.ConvertBack(thickness, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(40.0, result);
    }

    /// <summary>
    /// Tests convertback withthickness nosidesenabled returnsaverage.
    /// </summary>
    [Fact]
    public void ConvertBack_WithThickness_NoSidesEnabled_ReturnsAverage()
    {
        var converter = new DoubleToThicknessConverter
        {
            IsLeft = false,
            IsRight = false,
            IsTop = false,
            IsBottom = false
        };
        var thickness = new Thickness(10, 20, 30, 40);
        var result = converter.ConvertBack(thickness, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Equal(25.0, result); // (10 + 20 + 30 + 40) / 4
    }

    /// <summary>
    /// Tests convertback withnull returnsnull.
    /// </summary>
    [Fact]
    public void ConvertBack_WithNull_ReturnsNull()
    {
        var converter = new DoubleToThicknessConverter();
        var result = converter.ConvertBack(null, typeof(double), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }
}
