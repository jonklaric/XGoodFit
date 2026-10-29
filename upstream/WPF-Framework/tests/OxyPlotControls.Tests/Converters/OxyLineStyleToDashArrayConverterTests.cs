using System.Globalization;
using System.Windows.Media;
using OxyPlot;
using Xunit;

namespace OxyPlotControls.Tests.Converters;

/// <summary>
/// Tests for OxyLineStyleToDashArrayConverter.
/// Verifies proper conversion between OxyPlot LineStyle values and WPF DoubleCollection dash arrays.
/// </summary>
public class OxyLineStyleToDashArrayConverterTests
{
    /// <summary>
    /// The converter instance being tested.
    /// </summary>
    private readonly OxyLineStyleToDashArrayConverter _converter = new();

    #region Convert Tests

    /// <summary>
    /// Tests that Convert returns a default dash array when given a null value.
    /// </summary>
    [Fact]
    public void Convert_NullValue_ReturnsDefaultDashArray()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<DoubleCollection>(result);
    }

    /// <summary>
    /// Tests that Convert returns a default dash array when given an incorrect type.
    /// </summary>
    [Fact]
    public void Convert_WrongType_ReturnsDefaultDashArray()
    {
        // Arrange
        var value = "not a LineStyle";

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<DoubleCollection>(result);
    }

    /// <summary>
    /// Tests that Convert returns an empty dash array for Solid line style.
    /// </summary>
    [Fact]
    public void Convert_SolidLineStyle_ReturnsEmptyDashArray()
    {
        // Arrange
        var value = OxyPlot.LineStyle.Solid;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var dashArray = result as DoubleCollection;
        Assert.NotNull(dashArray);
        Assert.Empty(dashArray);
    }

    /// <summary>
    /// Tests that Convert returns an empty dash array for Automatic line style.
    /// </summary>
    [Fact]
    public void Convert_AutomaticLineStyle_ReturnsEmptyDashArray()
    {
        // Arrange
        var value = OxyPlot.LineStyle.Automatic;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var dashArray = result as DoubleCollection;
        Assert.NotNull(dashArray);
        Assert.Empty(dashArray);
    }

    /// <summary>
    /// Tests that Convert returns an empty dash array for None line style.
    /// </summary>
    [Fact]
    public void Convert_NoneLineStyle_ReturnsEmptyDashArray()
    {
        // Arrange
        var value = OxyPlot.LineStyle.None;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var dashArray = result as DoubleCollection;
        Assert.NotNull(dashArray);
        // None also renders as solid (empty dash array) in this implementation
        Assert.Empty(dashArray);
    }

    /// <summary>
    /// Tests that Convert returns a non-empty dash array for Dash line style.
    /// </summary>
    [Fact]
    public void Convert_DashLineStyle_ReturnsNonEmptyDashArray()
    {
        // Arrange
        var value = OxyPlot.LineStyle.Dash;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var dashArray = result as DoubleCollection;
        Assert.NotNull(dashArray);
        Assert.NotEmpty(dashArray);
    }

    /// <summary>
    /// Tests that Convert returns a non-empty dash array for Dot line style.
    /// </summary>
    [Fact]
    public void Convert_DotLineStyle_ReturnsNonEmptyDashArray()
    {
        // Arrange
        var value = OxyPlot.LineStyle.Dot;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var dashArray = result as DoubleCollection;
        Assert.NotNull(dashArray);
        Assert.NotEmpty(dashArray);
    }

    /// <summary>
    /// Tests that Convert returns a non-empty dash array for DashDot line style.
    /// </summary>
    [Fact]
    public void Convert_DashDotLineStyle_ReturnsNonEmptyDashArray()
    {
        // Arrange
        var value = OxyPlot.LineStyle.DashDot;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var dashArray = result as DoubleCollection;
        Assert.NotNull(dashArray);
        Assert.NotEmpty(dashArray);
    }

    /// <summary>
    /// Tests that Convert returns a non-empty dash array for DashDashDot line style.
    /// </summary>
    [Fact]
    public void Convert_DashDashDotLineStyle_ReturnsNonEmptyDashArray()
    {
        // Arrange
        var value = OxyPlot.LineStyle.DashDashDot;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var dashArray = result as DoubleCollection;
        Assert.NotNull(dashArray);
        Assert.NotEmpty(dashArray);
    }

    /// <summary>
    /// Tests that Convert returns a non-empty dash array for DashDotDot line style.
    /// </summary>
    [Fact]
    public void Convert_DashDotDotLineStyle_ReturnsNonEmptyDashArray()
    {
        // Arrange
        var value = OxyPlot.LineStyle.DashDotDot;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var dashArray = result as DoubleCollection;
        Assert.NotNull(dashArray);
        Assert.NotEmpty(dashArray);
    }

    /// <summary>
    /// Tests that Convert returns a non-empty dash array for LongDash line style.
    /// </summary>
    [Fact]
    public void Convert_LongDashLineStyle_ReturnsNonEmptyDashArray()
    {
        // Arrange
        var value = OxyPlot.LineStyle.LongDash;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var dashArray = result as DoubleCollection;
        Assert.NotNull(dashArray);
        Assert.NotEmpty(dashArray);
    }

    /// <summary>
    /// Tests that Convert returns a non-empty dash array for LongDashDot line style.
    /// </summary>
    [Fact]
    public void Convert_LongDashDotLineStyle_ReturnsNonEmptyDashArray()
    {
        // Arrange
        var value = OxyPlot.LineStyle.LongDashDot;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var dashArray = result as DoubleCollection;
        Assert.NotNull(dashArray);
        Assert.NotEmpty(dashArray);
    }

    /// <summary>
    /// Tests that Convert returns a non-empty dash array for LongDashDotDot line style.
    /// </summary>
    [Fact]
    public void Convert_LongDashDotDotLineStyle_ReturnsNonEmptyDashArray()
    {
        // Arrange
        var value = OxyPlot.LineStyle.LongDashDotDot;

        // Act
        var result = _converter.Convert(value, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        var dashArray = result as DoubleCollection;
        Assert.NotNull(dashArray);
        Assert.NotEmpty(dashArray);
    }

    #endregion

    #region ConvertBack Tests

    /// <summary>
    /// Tests that ConvertBack returns None line style when given a null value.
    /// </summary>
    [Fact]
    public void ConvertBack_NullValue_ReturnsNone()
    {
        // Arrange
        object? value = null;

        // Act
        var result = _converter.ConvertBack(value, typeof(OxyPlot.LineStyle), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(OxyPlot.LineStyle.None, result);
    }

    /// <summary>
    /// Tests that ConvertBack returns None line style when given an incorrect type.
    /// </summary>
    [Fact]
    public void ConvertBack_WrongType_ReturnsNone()
    {
        // Arrange
        var value = "not a DoubleCollection";

        // Act
        var result = _converter.ConvertBack(value, typeof(OxyPlot.LineStyle), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(OxyPlot.LineStyle.None, result);
    }

    /// <summary>
    /// Tests that ConvertBack returns Solid line style when given an empty dash array.
    /// </summary>
    [Fact]
    public void ConvertBack_EmptyDashArray_ReturnsSolid()
    {
        // Arrange
        var value = new DoubleCollection();

        // Act
        var result = _converter.ConvertBack(value, typeof(OxyPlot.LineStyle), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(OxyPlot.LineStyle.Solid, result);
    }

    /// <summary>
    /// Tests that ConvertBack returns Dash line style when given a dash pattern (4, 1).
    /// </summary>
    [Fact]
    public void ConvertBack_DashPattern_ReturnsDash()
    {
        // Arrange - Dash pattern is 4, 1
        var value = new DoubleCollection { 4, 1 };

        // Act
        var result = _converter.ConvertBack(value, typeof(OxyPlot.LineStyle), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(OxyPlot.LineStyle.Dash, result);
    }

    /// <summary>
    /// Tests that ConvertBack returns Dot line style when given a dot pattern (1, 1).
    /// </summary>
    [Fact]
    public void ConvertBack_DotPattern_ReturnsDot()
    {
        // Arrange - Dot pattern is 1, 1
        var value = new DoubleCollection { 1, 1 };

        // Act
        var result = _converter.ConvertBack(value, typeof(OxyPlot.LineStyle), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(OxyPlot.LineStyle.Dot, result);
    }

    /// <summary>
    /// Tests that ConvertBack returns DashDot line style when given a dash-dot pattern (4, 1, 1, 1).
    /// </summary>
    [Fact]
    public void ConvertBack_DashDotPattern_ReturnsDashDot()
    {
        // Arrange - DashDot pattern is 4, 1, 1, 1
        var value = new DoubleCollection { 4, 1, 1, 1 };

        // Act
        var result = _converter.ConvertBack(value, typeof(OxyPlot.LineStyle), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(OxyPlot.LineStyle.DashDot, result);
    }

    /// <summary>
    /// Tests that ConvertBack returns None line style when given an unrecognized dash pattern.
    /// </summary>
    [Fact]
    public void ConvertBack_UnrecognizedPattern_ReturnsNone()
    {
        // Arrange - Custom pattern that doesn't match any known style
        var value = new DoubleCollection { 7, 7, 7, 7, 7 };

        // Act
        var result = _converter.ConvertBack(value, typeof(OxyPlot.LineStyle), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(OxyPlot.LineStyle.None, result);
    }

    #endregion

    #region Round Trip Tests

    /// <summary>
    /// Tests that Convert and ConvertBack operations round-trip correctly for various line styles.
    /// </summary>
    /// <param name="lineStyle">The line style to test.</param>
    [Theory]
    [InlineData(LineStyle.Solid)]
    [InlineData(LineStyle.Dash)]
    [InlineData(LineStyle.Dot)]
    [InlineData(LineStyle.DashDot)]
    [InlineData(LineStyle.DashDotDot)]
    [InlineData(LineStyle.LongDash)]
    [InlineData(LineStyle.LongDashDot)]
    [InlineData(LineStyle.LongDashDotDot)]
    public void Convert_ConvertBack_RoundTrip(LineStyle lineStyle)
    {
        // Act
        var converted = _converter.Convert(lineStyle, typeof(DoubleCollection), null, CultureInfo.InvariantCulture);
        var roundTripped = _converter.ConvertBack(converted, typeof(LineStyle), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(lineStyle, roundTripped);
    }

    #endregion
}
