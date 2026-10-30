using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;

namespace FrameworkUI.Tests;

/// <summary>FrameworkUI's icon dictionary, which the application's resources merge.</summary>
[TestClass]
public sealed class IconDictionaryTests
{
    [TestMethod]
    public void CopyIcon_IsANewElementForEachLookup()
    {
        HeadlessSession.Run(() =>
        {
            // GenericControls' CopyPasteDataGrid puts the "CopyIcon" resource in every grid's context menu, and
            // FrameworkUI's dictionary, merged after GenericControls', provides it. A single shared element can
            // have one parent: it kept the first grid's menu, and with it that grid's whole document, alive.
            var source = new Uri("avares://FrameworkUI/Icons/IconDictionary.axaml");
            var icons = new ResourceInclude(source) { Source = source };
            Assert.IsTrue(icons.TryGetResource("CopyIcon", null, out object? first));
            Assert.IsTrue(icons.TryGetResource("CopyIcon", null, out object? second));
            Assert.IsInstanceOfType<Viewbox>(first);
            Assert.AreNotSame(first, second);
        });
    }
}
