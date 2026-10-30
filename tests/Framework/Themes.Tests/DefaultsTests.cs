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
    public void ControlsOutsideAWindow_HaveTheWindowsFont_SoRemovingOneChangesNoFont()
    {
        HeadlessSession.Run(() =>
        {
            // WPF's controls default to the system font, Segoe UI 12, in a window or not. Avalonia's inherit the
            // window's and fall back to its own default, so removing a control from its window (closing a
            // document) changed its font: a plot series reported the change, and its element's undo recorded
            // it as an edit.
            ThemeService.Instance.Initialize(Theme.Light);
            var text = new TextBlock { Text = "Flood frequency" };
            var changes = new List<string>();
            text.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name is "FontFamily" or "FontSize") changes.Add($"{e.Property.Name} {e.OldValue} -> {e.NewValue}");
            };
            Assert.AreEqual(12d, text.FontSize);
            CollectionAssert.AreEqual(new[] { "Segoe UI", "Selawik" }, text.FontFamily.FamilyNames.ToArray());

            var window = new Window { Content = text };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.Close();

            Assert.AreEqual(0, changes.Count, "the font changed: " + string.Join(", ", changes));
        });
    }

    [TestMethod]
    public void ThemeFonts_RegisterTheSubstitutes_AndStandThemInForWindowsFontsOffWindows()
    {
        HeadlessSession.Run(() =>
        {
            FontManager fonts = FontManager.Current;
            foreach ((string windowsFont, string substitute) in ThemeFonts.Substitutes)
            {
                string family = new FontFamily(substitute).Name;
                Assert.IsTrue(fonts.TryGetGlyphTypeface(new Typeface(substitute), out GlyphTypeface? embedded), $"{family} is embedded");
                Assert.AreEqual(family, embedded.FamilyName);
                Assert.IsTrue(fonts.TryGetGlyphTypeface(new Typeface(substitute, FontStyle.Normal, FontWeight.Bold), out GlyphTypeface? bold));
                Assert.AreEqual(FontWeight.Bold, bold.Weight, $"{family} has a bold style");

                // Outside Windows, upstream's Windows font names draw in their substitutes.
                if (!OperatingSystem.IsWindows())
                {
                    Assert.IsTrue(fonts.TryGetGlyphTypeface(new Typeface(windowsFont), out GlyphTypeface? standIn));
                    Assert.AreEqual(family, standIn.FamilyName, windowsFont);
                }
            }

            // Cambria's italic f(x) in the function icons.
            Assert.IsTrue(fonts.TryGetGlyphTypeface(new Typeface(ThemeFonts.Caladea, FontStyle.Italic, FontWeight.SemiBold), out GlyphTypeface? italic));
            Assert.AreEqual(FontStyle.Italic, italic.Style);
            if (!OperatingSystem.IsWindows())
            {
                Assert.AreEqual("Selawik", fonts.DefaultFontFamily.Name, "text that names no font");
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
