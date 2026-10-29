using System.Globalization;
using System.Windows;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for AlwaysVisibleConverter.
/// </summary>
public class AlwaysVisibleConverterTests
{
    /// <summary>
    /// Converter instance used for testing.
    /// </summary>
    private readonly AlwaysVisibleConverter _converter = new();

    /// <summary>
    /// Tests convert withanyvalue returnsvisible.
    /// </summary>
    [Fact]
    public void Convert_WithAnyValue_ReturnsVisible()
    {
        var result = _converter.Convert(123, typeof(Visibility), null, CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Visible, result);
    }

    /// <summary>
    /// Tests convert withnull returnsvisible.
    /// </summary>
    [Fact]
    public void Convert_WithNull_ReturnsVisible()
    {
        var result = _converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Visible, result);
    }

    /// <summary>
    /// Tests convert withstring returnsvisible.
    /// </summary>
    [Fact]
    public void Convert_WithString_ReturnsVisible()
    {
        var result = _converter.Convert("any string", typeof(Visibility), null, CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Visible, result);
    }

    /// <summary>
    /// Tests convert withboolean returnsvisible.
    /// </summary>
    [Fact]
    public void Convert_WithBoolean_ReturnsVisible()
    {
        var result = _converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Visible, result);
    }

    /// <summary>
    /// Tests convert withobject returnsvisible.
    /// </summary>
    [Fact]
    public void Convert_WithObject_ReturnsVisible()
    {
        var result = _converter.Convert(new object(), typeof(Visibility), null, CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Visible, result);
    }

    /// <summary>
    /// Tests convertback throwsnotimplementedexception.
    /// </summary>
    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack(Visibility.Visible, typeof(object), null, CultureInfo.InvariantCulture));
    }
}
