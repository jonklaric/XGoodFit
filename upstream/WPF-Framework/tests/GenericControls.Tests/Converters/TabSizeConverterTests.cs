using System.Globalization;
using Xunit;

namespace GenericControls.Tests.Converters;

/// <summary>
/// Unit tests for TabSizeConverter.
/// </summary>
public class TabSizeConverterTests
{
    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var converter = new TabSizeConverter();
        Assert.Throws<NotImplementedException>(() =>
            converter.ConvertBack(100.0, new[] { typeof(object) }, null, CultureInfo.InvariantCulture));
    }

    // Note: Testing Convert requires a TabControl with ActualWidth set, which is difficult
    // to test in unit tests without a UI context. The converter calculation is:
    // width = tabControl.ActualWidth / tabControl.Items.Count
    // if width < 12, return 0
    // return width - (tabControl.Items.Count + 1)
}
