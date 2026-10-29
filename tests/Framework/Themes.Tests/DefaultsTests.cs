using Avalonia.Controls;
using Avalonia.Media;

namespace Themes.Tests;

/// <summary>
/// The WPF-equivalent defaults that <see cref="ThemeService.Initialize"/> adds (Resources/Defaults.axaml).
/// </summary>
[TestClass]
public sealed class DefaultsTests
{
    [TestMethod]
    public void Windows_DefaultToSegoeUi12_WithSelawikFallback()
    {
        HeadlessSession.Run(() =>
        {
            ThemeService.Instance.Initialize(Theme.Light);
            var text = new TextBlock { Text = "Flood frequency" };
            var window = new Window { Content = text };
            window.Show();
            try
            {
                Assert.AreEqual(12d, text.FontSize, "WPF's default font size");
                CollectionAssert.AreEqual(
                    new[] { "Segoe UI", "Selawik" },
                    text.FontFamily.FamilyNames.ToArray(),
                    "Segoe UI, falling back to the embedded Selawik");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [TestMethod]
    public void Selawik_IsEmbedded()
    {
        HeadlessSession.Run(() =>
        {
            var typeface = new Typeface(new FontFamily("avares://Themes/Assets/Fonts#Selawik"));
            Assert.IsTrue(FontManager.Current.TryGetGlyphTypeface(typeface, out GlyphTypeface? glyphs), "Selawik not found");
            Assert.AreEqual("Selawik", glyphs!.FamilyName);
        });
    }
}
