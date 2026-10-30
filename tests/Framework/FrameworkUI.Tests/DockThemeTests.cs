using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FrameworkUI.Tests;

/// <summary>
/// The VS2013 dock theme (Themes/VS2013/Dock): how it is applied and swapped, and that its resources
/// resolve in every theme colour.
/// </summary>
[TestClass]
public sealed class DockThemeTests
{
    private static readonly ThemeColor[] Colours = [ThemeColor.Light, ThemeColor.Dark, ThemeColor.Blue];

    // The dock's background in each theme colour: AvalonDock's VS2013 Background.
    internal static Color Background(ThemeColor colour) => colour switch
    {
        ThemeColor.Dark => Color.Parse("#2D2D30"),
        ThemeColor.Blue => Color.Parse("#293955"),
        _ => Color.Parse("#EEEEF2"),
    };

    [TestMethod]
    public void Apply_ReplacesTheThemeRatherThanAddingOne()
    {
        HeadlessSession.Run(() =>
        {
            var manager = new DockingManager();
            int merged = manager.Resources.MergedDictionaries.Count;

            foreach (ThemeColor colour in Colours.Concat(Colours))
            {
                DockThemeSwitcher.Apply(manager, colour);
                Assert.AreEqual(colour, ((Vs2013DockTheme)manager.Theme!).ThemeColor);
                Assert.AreEqual(merged + 1, manager.Resources.MergedDictionaries.Count, "one dock theme dictionary");
                Assert.AreEqual(Background(colour), BrushColour(manager, "Vs2013Dock.Background"));
                Assert.AreEqual(Background(colour), BrushColour(manager, "DockThemeBackgroundBrush"));
            }

            // As upstream, an unknown colour keeps the current theme.
            DockThemeSwitcher.Apply(manager, (ThemeColor)42);
            Assert.AreEqual(ThemeColor.Blue, ((Vs2013DockTheme)manager.Theme!).ThemeColor);
        });
    }

    [TestMethod]
    public void Switcher_FollowsThemeManagerUntilDisposed()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Blue);
            var manager = new DockingManager();
            try
            {
                using (var switcher = new DockThemeSwitcher(manager))
                {
                    Assert.AreEqual(Background(ThemeColor.Blue), BrushColour(manager, "Vs2013Dock.Background"));

                    ThemeManager.SetTheme(ThemeColor.Dark);
                    Assert.AreEqual(ThemeColor.Dark, ((Vs2013DockTheme)manager.Theme!).ThemeColor);
                    Assert.AreEqual(Background(ThemeColor.Dark), BrushColour(manager, "Vs2013Dock.Background"));
                }

                ThemeManager.SetTheme(ThemeColor.Light);
                Assert.AreEqual(ThemeColor.Dark, ((Vs2013DockTheme)manager.Theme!).ThemeColor, "a disposed switcher no longer follows");
            }
            finally
            {
                ThemeManager.SetTheme(ThemeColor.Light);
            }
        });
    }

    [TestMethod]
    public void EveryThemeColour_ResolvesTheResourcesItsStylesUse()
    {
        HeadlessSession.Run(() =>
        {
            var palettes = Colours.ToDictionary(c => c, c => new Vs2013DockTheme(c).ThemeResourceDictionary!);
            var keys = palettes.ToDictionary(p => p.Key, p => PaletteKeys(p.Value).ToHashSet());
            Assert.IsTrue(keys[ThemeColor.Light].Count > 90);
            CollectionAssert.AreEquivalent(keys[ThemeColor.Light].ToList(), keys[ThemeColor.Dark].ToList(), "Light and Dark give the same keys");
            CollectionAssert.AreEquivalent(keys[ThemeColor.Light].ToList(), keys[ThemeColor.Blue].ToList(), "Light and Blue give the same keys");

            // Every resource the theme's control themes ask for, in every colour.
            List<object> used = DynamicResourceKeys(palettes[ThemeColor.Light]).Distinct().ToList();
            Assert.IsTrue(used.Count > 60, $"{used.Count} keys");
            foreach ((ThemeColor colour, ResourceDictionary palette) in palettes)
            {
                foreach (object key in used)
                {
                    Assert.IsTrue(palette.TryGetResource(key, null, out object? value) && value != null, $"{colour}: {key}");
                }
            }
        });
    }

    [TestMethod]
    public void FloatingWindow_TakesTheThemeAndItsSwaps()
    {
        HeadlessSession.Run(() =>
        {
            Window window = Show(out DockingManager manager);
            try
            {
                DockThemeSwitcher.Apply(manager, ThemeColor.Dark);
                manager.Layout.Descendents().OfType<LayoutAnchorable>().First(a => a.ContentId == ShellPublicVariables.PropertiesWindowContentID).Float();
                Dispatcher.UIThread.RunJobs();
                Window floating = manager.FloatingWindows.OfType<Window>().Single();
                Assert.AreEqual(Background(ThemeColor.Dark), BrushColour(floating, "Vs2013Dock.Background"));

                DockThemeSwitcher.Apply(manager, ThemeColor.Blue);
                Dispatcher.UIThread.RunJobs();
                Assert.AreEqual(Background(ThemeColor.Blue), BrushColour(floating, "Vs2013Dock.Background"));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [TestMethod]
    public void DocumentTabs_ShowTheirCloseButtonWhenSelectedOrHovered()
    {
        HeadlessSession.Run(() =>
        {
            Window window = Show(out DockingManager manager, documents: true);
            try
            {
                DockThemeSwitcher.Apply(manager, ThemeColor.Light);
                Dispatcher.UIThread.RunJobs();
                Dock.Avalonia.Controls.DocumentTabStripItem selected = Tab(window, "First");
                Dock.Avalonia.Controls.DocumentTabStripItem other = Tab(window, "Second");
                Assert.IsTrue(CloseGlyphVisible(selected), "the selected tab's");
                Assert.IsFalse(CloseGlyphVisible(other), "an unselected tab's");

                window.MouseMove(other.TranslatePoint(new Point(5, 5), window)!.Value);
                Dispatcher.UIThread.RunJobs();
                Assert.IsTrue(CloseGlyphVisible(other), "a hovered tab's");
            }
            finally
            {
                window.Close();
            }
        });
    }

    private const string Layout = """
        <?xml version="1.0" encoding="utf-8"?>
        <LayoutRoot>
          <RootPanel Orientation="Horizontal">
            <LayoutAnchorablePane DockWidth="200">
              <LayoutAnchorable Title="Properties" ContentId="PropertiesWindow" IsSelected="True" CanClose="False" />
            </LayoutAnchorablePane>
            <LayoutDocumentPaneGroup Orientation="Horizontal">
              <LayoutDocumentPane>DOCUMENTS</LayoutDocumentPane>
            </LayoutDocumentPaneGroup>
          </RootPanel>
          <TopSide />
          <RightSide />
          <LeftSide />
          <BottomSide />
          <FloatingWindows />
          <Hidden />
        </LayoutRoot>
        """;

    private static Window Show(out DockingManager manager, bool documents = false)
    {
        manager = new DockingManager();
        var window = new Window { Width = 900, Height = 500, Content = manager };
        window.Show();
        var serializer = new XmlLayoutSerializer(manager);
        serializer.LayoutSerializationCallback += (_, e) => e.Content = new TextBlock { Text = e.Model.Title };
        serializer.Deserialize(new StringReader(Layout.Replace("DOCUMENTS", documents
            ? """<LayoutDocument Title="First" ContentId="First" IsSelected="True" CanClose="True" /><LayoutDocument Title="Second" ContentId="Second" CanClose="True" />"""
            : "")));
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static Dock.Avalonia.Controls.DocumentTabStripItem Tab(Window window, string title) =>
        window.GetVisualDescendants().OfType<Dock.Avalonia.Controls.DocumentTabStripItem>()
            .First(t => t.GetVisualDescendants().OfType<TextBlock>().Any(b => b.Text == title));

    // The close button's glyph: its border is hidden (keeping its space) unless the tab is selected or
    // hovered. (Dock's compact close button, in the icon's place, stays hidden.)
    private static bool CloseGlyphVisible(Dock.Avalonia.Controls.DocumentTabStripItem tab) =>
        tab.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ClosePresenter")
            .GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("avalondock-close"))
            .GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Border").IsVisible;

    internal static Color BrushColour(StyledElement element, string key) =>
        ((ISolidColorBrush)element.FindResource(key)!).Color;

    // The keys a theme dictionary's palette defines (its own entries and its merged dictionaries').
    private static IEnumerable<object> PaletteKeys(ResourceDictionary dictionary) =>
        dictionary.Keys.Concat(dictionary.MergedDictionaries.OfType<ResourceDictionary>().SelectMany(PaletteKeys))
            .Where(k => k is string s && (s.StartsWith("Vs2013Dock.", StringComparison.Ordinal) && !s.EndsWith("Geometry", StringComparison.Ordinal)
                || s.StartsWith("Dock", StringComparison.Ordinal) && s.EndsWith("Brush", StringComparison.Ordinal)));

    // The resource keys of the DynamicResource setters in a dictionary's control themes (and their styles).
    private static IEnumerable<object> DynamicResourceKeys(IResourceProvider provider)
    {
        if (provider is not ResourceDictionary dictionary)
        {
            yield break;
        }
        foreach (object key in dictionary.Keys)
        {
            if (dictionary.TryGetResource(key, null, out object? value) && value is ControlTheme theme)
            {
                foreach (object used in SetterKeys(theme))
                {
                    yield return used;
                }
            }
        }
        foreach (IResourceProvider merged in dictionary.MergedDictionaries)
        {
            foreach (object used in DynamicResourceKeys(merged))
            {
                yield return used;
            }
        }
    }

    private static IEnumerable<object> SetterKeys(StyleBase style)
    {
        foreach (Setter setter in style.Setters.OfType<Setter>())
        {
            if (setter.Value is DynamicResourceExtension { ResourceKey: { } key } && key is string s && s.StartsWith("Vs2013Dock.", StringComparison.Ordinal))
            {
                yield return key;
            }
        }
        foreach (StyleBase child in style.Children.OfType<StyleBase>())
        {
            foreach (object key in SetterKeys(child))
            {
                yield return key;
            }
        }
    }
}
