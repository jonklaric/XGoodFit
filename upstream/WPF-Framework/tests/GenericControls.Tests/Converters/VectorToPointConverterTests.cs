using System.Globalization;
using System.Windows;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for VectorToPointConverter.
/// </summary>
public class VectorToPointConverterTests
{
    /// <summary>
    /// Converter instance used for testing.
    /// </summary>
    private readonly VectorToPointConverter _converter = new();

    /// <summary>
    /// Tests convert withvector returnspoint.
    /// </summary>
    [Fact]
    public void Convert_WithVector_ReturnsPoint()
    {
        var vector = new Vector(10, 20);
        var result = _converter.Convert(vector, typeof(Point), null, CultureInfo.InvariantCulture);
        Assert.IsType<Point>(result);
        var point = (Point)result;
        Assert.Equal(10, point.X);
        Assert.Equal(20, point.Y);
    }

    /// <summary>
    /// Tests convert withnull returnsnull.
    /// </summary>
    [Fact]
    public void Convert_WithNull_ReturnsNull()
    {
        var result = _converter.Convert(null, typeof(Point), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convert withinvalidtype returnsnull.
    /// </summary>
    [Fact]
    public void Convert_WithInvalidType_ReturnsNull()
    {
        var result = _converter.Convert("invalid", typeof(Point), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convert withzerovector returnszeropoint.
    /// </summary>
    [Fact]
    public void Convert_WithZeroVector_ReturnsZeroPoint()
    {
        var vector = new Vector(0, 0);
        var result = _converter.Convert(vector, typeof(Point), null, CultureInfo.InvariantCulture);
        Assert.IsType<Point>(result);
        var point = (Point)result;
        Assert.Equal(0, point.X);
        Assert.Equal(0, point.Y);
    }

    /// <summary>
    /// Tests convert withnegativevector returnsnegativepoint.
    /// </summary>
    [Fact]
    public void Convert_WithNegativeVector_ReturnsNegativePoint()
    {
        var vector = new Vector(-5, -15);
        var result = _converter.Convert(vector, typeof(Point), null, CultureInfo.InvariantCulture);
        Assert.IsType<Point>(result);
        var point = (Point)result;
        Assert.Equal(-5, point.X);
        Assert.Equal(-15, point.Y);
    }

    /// <summary>
    /// Tests convertback withpoint returnsvector.
    /// </summary>
    [Fact]
    public void ConvertBack_WithPoint_ReturnsVector()
    {
        var point = new Point(30, 40);
        var result = _converter.ConvertBack(point, typeof(Vector), null, CultureInfo.InvariantCulture);
        Assert.IsType<Vector>(result);
        var vector = (Vector)result;
        Assert.Equal(30, vector.X);
        Assert.Equal(40, vector.Y);
    }

    /// <summary>
    /// Tests convertback withnull returnsnull.
    /// </summary>
    [Fact]
    public void ConvertBack_WithNull_ReturnsNull()
    {
        var result = _converter.ConvertBack(null, typeof(Vector), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests convertback withinvalidtype returnsnull.
    /// </summary>
    [Fact]
    public void ConvertBack_WithInvalidType_ReturnsNull()
    {
        var result = _converter.ConvertBack("invalid", typeof(Vector), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }
}
