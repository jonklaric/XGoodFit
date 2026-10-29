using System.Globalization;
using System.Threading;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for StringToNAConverter.
/// </summary>
[Collection("CultureSensitive")]
public class StringToNAConverterTests
{
    /// <summary>
    /// Converter instance used for testing.
    /// </summary>
    private readonly StringToNAConverter _converter = new();

    /// <summary>
    /// Tests convert withvalidnumericstring returnsstring.
    /// </summary>
    [Fact]
    public void Convert_WithValidNumericString_ReturnsString()
    {
        var result = _converter.Convert("123.45", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("123.45", result);
    }

    /// <summary>
    /// Tests convert withnull returnsna.
    /// </summary>
    [Fact]
    public void Convert_WithNull_ReturnsNA()
    {
        var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("N/A", result);
    }

    /// <summary>
    /// Tests convert withnonstring returnsna.
    /// </summary>
    [Fact]
    public void Convert_WithNonString_ReturnsNA()
    {
        var result = _converter.Convert(123.45, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("N/A", result);
    }

    /// <summary>
    /// Tests convert withnanstring returnsna.
    /// </summary>
    [Fact]
    public void Convert_WithNaNString_ReturnsNA()
    {
        var result = _converter.Convert("NaN", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("N/A", result);
    }

    /// <summary>
    /// Tests convert withinfinitystring returnsna.
    /// </summary>
    [Fact]
    public void Convert_WithInfinityString_ReturnsNA()
    {
        var result = _converter.Convert("Infinity", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("N/A", result);
    }

    /// <summary>
    /// Tests convert withnegativeinfinitystring returnsna.
    /// </summary>
    [Fact]
    public void Convert_WithNegativeInfinityString_ReturnsNA()
    {
        var result = _converter.Convert("-Infinity", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("N/A", result);
    }

    /// <summary>
    /// Tests convert withinvalidnumericstring returnsna.
    /// </summary>
    [Fact]
    public void Convert_WithInvalidNumericString_ReturnsNA()
    {
        var result = _converter.Convert("not a number", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("N/A", result);
    }

    /// <summary>
    /// Tests convert withemptystring returnsna.
    /// </summary>
    [Fact]
    public void Convert_WithEmptyString_ReturnsNA()
    {
        var result = _converter.Convert("", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("N/A", result);
    }

    /// <summary>
    /// Tests convert withzerostring returnszerostring.
    /// </summary>
    [Fact]
    public void Convert_WithZeroString_ReturnsZeroString()
    {
        var result = _converter.Convert("0", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("0", result);
    }

    /// <summary>
    /// Tests convert withnegativenumberstring returnsstring.
    /// </summary>
    [Fact]
    public void Convert_WithNegativeNumberString_ReturnsString()
    {
        var result = _converter.Convert("-42.5", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("-42.5", result);
    }

    /// <summary>
    /// Tests convertback throwsnotimplementedexception.
    /// </summary>
    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack("123", typeof(string), null, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies the converter respects the culture parameter passed by WPF rather than
    /// using <see cref="Thread.CurrentCulture"/>. Uses the string "1.5e308" because:
    /// - de-DE parses it as <c>∞</c> (period interpreted as thousands separator → "15e308" overflows)
    /// - Invariant parses it as the finite <c>1.5e308</c>
    /// The converter returns "N/A" for infinite values and the original string for finite.
    /// Setting thread to en-US and passing culture=de-DE forces the converter to honor the
    /// parameter (and produce "N/A"), distinguishing it from the thread-culture path that
    /// would produce "1.5e308".
    /// </summary>
    [Fact]
    public void Convert_FiniteUnderInvariantInfiniteUnderGerman_HonorsCultureParameter()
    {
        RunUnderCulture("en-US", () =>
        {
            // With param=de-DE: parses "1.5e308" as ∞ → IsInfinity → "N/A"
            var result = _converter.Convert("1.5e308", typeof(string), null, new CultureInfo("de-DE"));
            Assert.Equal("N/A", result);
        });
    }

    /// <summary>
    /// Mirror test: thread is de-DE, parameter is Invariant. With the fix, the converter
    /// uses Invariant which gives finite 1.5e308 → returns the original string. Without
    /// the fix, thread culture (de-DE) gives ∞ → "N/A".
    /// </summary>
    [Fact]
    public void Convert_FiniteUnderInvariantInfiniteUnderGerman_ReturnsFiniteUnderInvariantParameter()
    {
        RunUnderCulture("de-DE", () =>
        {
            var result = _converter.Convert("1.5e308", typeof(string), null, CultureInfo.InvariantCulture);
            Assert.Equal("1.5e308", result);
        });
    }

    /// <summary>
    /// US-formatted "1.5" parses successfully under Invariant. Confirms the symmetric
    /// case: providing Invariant explicitly works regardless of thread culture.
    /// </summary>
    [Fact]
    public void Convert_USFormat_ParsesUsingInvariantCulture_RegardlessOfThreadCulture()
    {
        RunUnderCulture("de-DE", () =>
        {
            var result = _converter.Convert("1.5", typeof(string), null, CultureInfo.InvariantCulture);
            Assert.Equal("1.5", result);
        });
    }

    private static void RunUnderCulture(string cultureName, System.Action body)
    {
        var prevCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentCulture = culture;
            CultureInfo.CurrentCulture = culture;
            body();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = prevCulture;
            CultureInfo.CurrentCulture = prevCulture;
        }
    }
}
