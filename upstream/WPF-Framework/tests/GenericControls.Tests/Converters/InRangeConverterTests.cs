using System.Globalization;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for InRangeConverter.
/// </summary>
public class InRangeConverterTests
{
    [Fact]
    public void Convert_WithValueInRange_ReturnsTrue()
    {
        var converter = new InRangeConverter { LowerBound = 0, UpperBound = 100 };
        var result = converter.Convert(50.0, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(true, result);
    }

    /// <summary>
    /// Tests convert withvalueatlowerbound returnstrue.
    /// </summary>
    [Fact]
    public void Convert_WithValueAtLowerBound_ReturnsTrue()
    {
        var converter = new InRangeConverter { LowerBound = 0, UpperBound = 100 };
        var result = converter.Convert(0.0, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(true, result);
    }

    /// <summary>
    /// Tests convert withvalueatupperbound returnstrue.
    /// </summary>
    [Fact]
    public void Convert_WithValueAtUpperBound_ReturnsTrue()
    {
        var converter = new InRangeConverter { LowerBound = 0, UpperBound = 100 };
        var result = converter.Convert(100.0, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(true, result);
    }

    /// <summary>
    /// Tests convert withvaluebelowrange returnsfalse.
    /// </summary>
    [Fact]
    public void Convert_WithValueBelowRange_ReturnsFalse()
    {
        var converter = new InRangeConverter { LowerBound = 0, UpperBound = 100 };
        var result = converter.Convert(-1.0, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(false, result);
    }

    /// <summary>
    /// Tests convert withvalueaboverange returnsfalse.
    /// </summary>
    [Fact]
    public void Convert_WithValueAboveRange_ReturnsFalse()
    {
        var converter = new InRangeConverter { LowerBound = 0, UpperBound = 100 };
        var result = converter.Convert(101.0, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(false, result);
    }

    /// <summary>
    /// Tests convert withnull returnsfalse.
    /// </summary>
    [Fact]
    public void Convert_WithNull_ReturnsFalse()
    {
        var converter = new InRangeConverter { LowerBound = 0, UpperBound = 100 };
        var result = converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(false, result);
    }

    /// <summary>
    /// Tests convert withinvalidtype returnsfalse.
    /// </summary>
    [Fact]
    public void Convert_WithInvalidType_ReturnsFalse()
    {
        var converter = new InRangeConverter { LowerBound = 0, UpperBound = 100 };
        var result = converter.Convert("not a number", typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(false, result);
    }

    /// <summary>
    /// Tests convert withdefaultbounds valueinrange returnstrue.
    /// </summary>
    [Fact]
    public void Convert_WithDefaultBounds_ValueInRange_ReturnsTrue()
    {
        var converter = new InRangeConverter();
        var result = converter.Convert(0.0, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(true, result);
    }

    /// <summary>
    /// Tests convert withnegativebounds returnscorrectresult.
    /// </summary>
    [Fact]
    public void Convert_WithNegativeBounds_ReturnsCorrectResult()
    {
        var converter = new InRangeConverter { LowerBound = -100, UpperBound = -10 };
        var result = converter.Convert(-50.0, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(true, result);
    }

    /// <summary>
    /// Tests convert withstringnumber returnstrue.
    /// </summary>
    [Fact]
    public void Convert_WithStringNumber_ReturnsTrue()
    {
        var converter = new InRangeConverter { LowerBound = 0, UpperBound = 100 };
        var result = converter.Convert("50", typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(true, result);
    }

    /// <summary>
    /// Tests convertback throwsnotimplementedexception.
    /// </summary>
    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var converter = new InRangeConverter();
        Assert.Throws<NotImplementedException>(() =>
            converter.ConvertBack(true, typeof(double), null, CultureInfo.InvariantCulture));
    }
}
