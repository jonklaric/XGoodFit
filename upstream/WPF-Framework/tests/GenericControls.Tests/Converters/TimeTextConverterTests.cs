using System.Globalization;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for TimeTextConverter.
/// </summary>
public class TimeTextConverterTests
{
    [Fact]
    public void Convert_Hour12Hour_ReturnsFormattedHour()
    {
        var converter = new TimeTextConverter { TimeType = TimeTextConverter.TimeTarget.Hour, Is24Hour = false };
        var dateTime = new DateTime(2023, 1, 1, 14, 30, 45);
        var result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("02", result);
    }

    /// <summary>
    /// Tests convert hour24hour returnsformattedhour.
    /// </summary>
    [Fact]
    public void Convert_Hour24Hour_ReturnsFormattedHour()
    {
        var converter = new TimeTextConverter { TimeType = TimeTextConverter.TimeTarget.Hour, Is24Hour = true };
        var dateTime = new DateTime(2023, 1, 1, 14, 30, 45);
        var result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("14", result);
    }

    /// <summary>
    /// Tests convert minute returnsformattedminute.
    /// </summary>
    [Fact]
    public void Convert_Minute_ReturnsFormattedMinute()
    {
        var converter = new TimeTextConverter { TimeType = TimeTextConverter.TimeTarget.Minute };
        var dateTime = new DateTime(2023, 1, 1, 14, 5, 45);
        var result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("05", result);
    }

    /// <summary>
    /// Tests convert second returnsformattedsecond.
    /// </summary>
    [Fact]
    public void Convert_Second_ReturnsFormattedSecond()
    {
        var converter = new TimeTextConverter { TimeType = TimeTextConverter.TimeTarget.Second };
        var dateTime = new DateTime(2023, 1, 1, 14, 30, 9);
        var result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("09", result);
    }

    /// <summary>
    /// Tests convert meridian12hour returnsmeridian.
    /// </summary>
    [Fact]
    public void Convert_Meridian12Hour_ReturnsMeridian()
    {
        var converter = new TimeTextConverter { TimeType = TimeTextConverter.TimeTarget.Meridian, Is24Hour = false };
        var dateTime = new DateTime(2023, 1, 1, 14, 30, 45);
        var result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("PM", result);
    }

    /// <summary>
    /// Tests convert meridian24hour returnsempty.
    /// </summary>
    [Fact]
    public void Convert_Meridian24Hour_ReturnsEmpty()
    {
        var converter = new TimeTextConverter { TimeType = TimeTextConverter.TimeTarget.Meridian, Is24Hour = true };
        var dateTime = new DateTime(2023, 1, 1, 14, 30, 45);
        var result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("", result);
    }

    /// <summary>
    /// Tests convert withinvalidtype returnsemptystring.
    /// </summary>
    [Fact]
    public void Convert_WithInvalidType_ReturnsEmptyString()
    {
        var converter = new TimeTextConverter { TimeType = TimeTextConverter.TimeTarget.Hour };
        var result = converter.Convert("not a datetime", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("", result);
    }

    /// <summary>
    /// Tests convert morningmeridian returnsam.
    /// </summary>
    [Fact]
    public void Convert_MorningMeridian_ReturnsAM()
    {
        var converter = new TimeTextConverter { TimeType = TimeTextConverter.TimeTarget.Meridian, Is24Hour = false };
        var dateTime = new DateTime(2023, 1, 1, 9, 30, 0);
        var result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("AM", result);
    }

    /// <summary>
    /// Tests convert midnight returnscorrecthour12hour.
    /// </summary>
    [Fact]
    public void Convert_Midnight_ReturnsCorrectHour12Hour()
    {
        var converter = new TimeTextConverter { TimeType = TimeTextConverter.TimeTarget.Hour, Is24Hour = false };
        var dateTime = new DateTime(2023, 1, 1, 0, 0, 0);
        var result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("12", result);
    }

    /// <summary>
    /// Tests convert midnight returnscorrecthour24hour.
    /// </summary>
    [Fact]
    public void Convert_Midnight_ReturnsCorrectHour24Hour()
    {
        var converter = new TimeTextConverter { TimeType = TimeTextConverter.TimeTarget.Hour, Is24Hour = true };
        var dateTime = new DateTime(2023, 1, 1, 0, 0, 0);
        var result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("00", result);
    }

    /// <summary>
    /// Tests convertback throwsnotimplementedexception.
    /// </summary>
    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var converter = new TimeTextConverter();
        Assert.Throws<NotImplementedException>(() =>
            converter.ConvertBack("12", typeof(DateTime), null, CultureInfo.InvariantCulture));
    }
}
