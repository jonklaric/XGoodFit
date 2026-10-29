using System.Globalization;
using OxyPlot;
using Xunit;

namespace OxyPlotControls.Tests.Converters;

/// <summary>
/// Tests for DataPointToPointConverter.
/// Verifies proper conversion between OxyPlot DataPoint and WPF Point objects.
/// </summary>
public class DataPointToPointConverterTests
{
    /// <summary>
    /// The converter instance being tested.
    /// </summary>
    private readonly DataPointToPointConverter _converter = new();

    #region Convert Tests

    [Fact]
    public void Convert_NullValue_ReturnsNull()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _converter.Convert(value, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Convert_WrongType_ReturnsNull()
    {
        // Arrange
        var value = "not a DataPoint";

        // Act
        var result = _converter.Convert(value, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that Convert returns a WPF Point when given a valid DataPoint.
    /// </summary>
    [Fact]
    public void Convert_ValidDataPoint_ReturnsPoint()
    {
        // Arrange
        var dataPoint = new DataPoint(10.5, 20.5);

        // Act
        var result = _converter.Convert(dataPoint, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<System.Windows.Point>(result);
        var point = (System.Windows.Point)result!;
        Assert.Equal(10.5, point.X);
        Assert.Equal(20.5, point.Y);
    }

    /// <summary>
    /// Tests that Convert returns a zero Point when given a zero DataPoint.
    /// </summary>
    [Fact]
    public void Convert_ZeroDataPoint_ReturnsZeroPoint()
    {
        // Arrange
        var dataPoint = new DataPoint(0, 0);

        // Act
        var result = _converter.Convert(dataPoint, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<System.Windows.Point>(result);
        var point = (System.Windows.Point)result!;
        Assert.Equal(0, point.X);
        Assert.Equal(0, point.Y);
    }

    /// <summary>
    /// Tests that Convert correctly handles negative DataPoint values.
    /// </summary>
    [Fact]
    public void Convert_NegativeDataPoint_ReturnsNegativePoint()
    {
        // Arrange
        var dataPoint = new DataPoint(-10.5, -20.5);

        // Act
        var result = _converter.Convert(dataPoint, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<System.Windows.Point>(result);
        var point = (System.Windows.Point)result!;
        Assert.Equal(-10.5, point.X);
        Assert.Equal(-20.5, point.Y);
    }

    /// <summary>
    /// Tests that Convert correctly handles large DataPoint values.
    /// </summary>
    [Fact]
    public void Convert_LargeValuesDataPoint_ReturnsCorrectPoint()
    {
        // Arrange
        var dataPoint = new DataPoint(1000000.123, 2000000.456);

        // Act
        var result = _converter.Convert(dataPoint, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<System.Windows.Point>(result);
        var point = (System.Windows.Point)result!;
        Assert.Equal(1000000.123, point.X);
        Assert.Equal(2000000.456, point.Y);
    }

    #endregion

    #region ConvertBack Tests

    [Fact]
    public void ConvertBack_NullValue_ReturnsNull()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _converter.ConvertBack(value, typeof(DataPoint), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertBack_WrongType_ReturnsNull()
    {
        // Arrange
        var value = "not a Point";

        // Act
        var result = _converter.ConvertBack(value, typeof(DataPoint), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that ConvertBack returns a DataPoint when given a valid WPF Point.
    /// </summary>
    [Fact]
    public void ConvertBack_ValidPoint_ReturnsDataPoint()
    {
        // Arrange
        var point = new System.Windows.Point(10.5, 20.5);

        // Act
        var result = _converter.ConvertBack(point, typeof(DataPoint), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<DataPoint>(result);
        var dataPoint = (DataPoint)result!;
        Assert.Equal(10.5, dataPoint.X);
        Assert.Equal(20.5, dataPoint.Y);
    }

    /// <summary>
    /// Tests that ConvertBack returns a zero DataPoint when given a zero WPF Point.
    /// </summary>
    [Fact]
    public void ConvertBack_ZeroPoint_ReturnsZeroDataPoint()
    {
        // Arrange
        var point = new System.Windows.Point(0, 0);

        // Act
        var result = _converter.ConvertBack(point, typeof(DataPoint), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<DataPoint>(result);
        var dataPoint = (DataPoint)result!;
        Assert.Equal(0, dataPoint.X);
        Assert.Equal(0, dataPoint.Y);
    }

    /// <summary>
    /// Tests that ConvertBack correctly handles negative WPF Point values.
    /// </summary>
    [Fact]
    public void ConvertBack_NegativePoint_ReturnsNegativeDataPoint()
    {
        // Arrange
        var point = new System.Windows.Point(-10.5, -20.5);

        // Act
        var result = _converter.ConvertBack(point, typeof(DataPoint), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<DataPoint>(result);
        var dataPoint = (DataPoint)result!;
        Assert.Equal(-10.5, dataPoint.X);
        Assert.Equal(-20.5, dataPoint.Y);
    }

    #endregion

    #region Round Trip Tests

    [Fact]
    public void Convert_ConvertBack_RoundTrip()
    {
        // Arrange
        var originalDataPoint = new DataPoint(123.456, 789.012);

        // Act
        var converted = _converter.Convert(originalDataPoint, typeof(System.Windows.Point), null, CultureInfo.InvariantCulture);
        var roundTripped = _converter.ConvertBack(converted, typeof(DataPoint), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<DataPoint>(roundTripped);
        var result = (DataPoint)roundTripped!;
        Assert.Equal(originalDataPoint.X, result.X);
        Assert.Equal(originalDataPoint.Y, result.Y);
    }

    #endregion
}
