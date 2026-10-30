using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using Path = Avalonia.Controls.Shapes.Path;

namespace Themes.Tests;

/// <summary>
/// The WPF-equivalent defaults that <see cref="ThemeService.Initialize"/> adds (<see cref="WpfDefaults"/> and
/// Resources/Defaults.axaml).
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
    public void ThemeFonts_RegisterSelawik_AndStandItInForSegoeUiOffWindows()
    {
        HeadlessSession.Run(() =>
        {
            FontManager fonts = FontManager.Current;
            Assert.IsTrue(fonts.TryGetGlyphTypeface(new Typeface(ThemeFonts.Selawik), out GlyphTypeface? selawik), "the embedded collection is registered");
            Assert.AreEqual("Selawik", selawik.FamilyName);

            // Outside Windows, upstream's "Segoe UI", and text that names no font, draw in Selawik.
            if (!OperatingSystem.IsWindows())
            {
                Assert.AreEqual("Selawik", fonts.DefaultFontFamily.Name);
                Assert.IsTrue(fonts.TryGetGlyphTypeface(new Typeface("Segoe UI"), out GlyphTypeface? segoe));
                Assert.AreEqual("Selawik", segoe.FamilyName);
            }
        });
    }

    [TestMethod]
    public void Shapes_DrawWpfsDefaultStroke_OnlyWhenTheyHaveOne()
    {
        HeadlessSession.Run(() =>
        {
            ThemeService.Instance.Initialize(Theme.Light);
            var path = new Path { Stroke = Brushes.Black };
            var stroked = new Rectangle { Stroke = Brushes.Black };
            var filled = new Rectangle { Fill = Brushes.Black };
            var ellipse = new Ellipse { Stroke = Brushes.Black };
            var explicitWidth = new Rectangle { Stroke = Brushes.Black, StrokeThickness = 3 };
            var window = new Window { Content = new StackPanel { Children = { path, stroked, filled, ellipse, explicitWidth } } };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            try
            {
                Assert.AreEqual(1d, path.StrokeThickness, "Path");
                Assert.AreEqual(1d, new Polygon().StrokeThickness, "Polygon");
                Assert.AreEqual(1d, new Polyline().StrokeThickness, "Polyline");
                Assert.AreEqual(1d, stroked.StrokeThickness, "Rectangle with a stroke");
                Assert.AreEqual(0d, filled.StrokeThickness, "Rectangle without a stroke keeps its whole fill, as in WPF");
                Assert.AreEqual(1d, ellipse.StrokeThickness, "Ellipse with a stroke");
                Assert.AreEqual(3d, explicitWidth.StrokeThickness, "an explicit thickness wins");

                filled.Stroke = Brushes.Red;
                Assert.AreEqual(1d, filled.StrokeThickness, "follows the stroke");
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
