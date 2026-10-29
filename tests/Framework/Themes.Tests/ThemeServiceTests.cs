using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Themes.Tests;

/// <summary>
/// Behaviour of the ported <see cref="ThemeService"/>. The service is a process-wide singleton, so
/// each test sets the theme it starts from.
/// </summary>
[TestClass]
public sealed class ThemeServiceTests
{
    // EnvironmentWindowBackground in upstream's LightColors, BlueColors and DarkColors.xaml.
    private static readonly Dictionary<Theme, Color> WindowBackground = new()
    {
        [Theme.Light] = Color.Parse("#EEEEF2"),
        [Theme.Blue] = Color.Parse("#D6DBE9"),
        [Theme.Dark] = Color.Parse("#FF2D2D30"),
    };

    private static void Initialize()
    {
        ThemeService.Instance.Initialize(Theme.Light);
        ThemeService.Instance.SetTheme(Theme.Light);
    }

    private static Color FindBrushColor(string key)
    {
        Assert.IsTrue(Application.Current!.TryGetResource(key, null, out object? value), $"{key} not found");
        return ((ISolidColorBrush)value!).Color;
    }

    [TestMethod]
    public void Initialize_MergesControlTemplatesAndPalette()
    {
        HeadlessSession.Run(() =>
        {
            Initialize();

            Assert.IsTrue(ThemeService.Instance.IsInitialized);
            Assert.AreEqual(Theme.Light, ThemeService.Instance.CurrentTheme);
            Assert.AreEqual(2, Application.Current!.Resources.MergedDictionaries.Count);
            Assert.IsTrue(Application.Current.TryGetResource("RepeatButtonTransparent", null, out _), "control templates");
            Assert.AreEqual(WindowBackground[Theme.Light], FindBrushColor("EnvironmentWindowBackground"));
            Assert.AreEqual(ThemeVariant.Light, Application.Current.RequestedThemeVariant);
        });
    }

    [TestMethod]
    [DataRow(Theme.Blue)]
    [DataRow(Theme.Dark)]
    public void SetTheme_SwapsThePaletteAndRaisesThemeChanged(Theme theme)
    {
        HeadlessSession.Run(() =>
        {
            Initialize();
            var raised = new List<ThemeChangedEventArgs>();
            void OnChanged(object? sender, ThemeChangedEventArgs e) => raised.Add(e);
            ThemeService.Instance.ThemeChanged += OnChanged;
            try
            {
                ThemeService.Instance.SetTheme(theme);
                ThemeService.Instance.SetTheme(theme);
            }
            finally
            {
                ThemeService.Instance.ThemeChanged -= OnChanged;
            }

            Assert.AreEqual(1, raised.Count, "setting the same theme again must not raise ThemeChanged");
            Assert.AreEqual(Theme.Light, raised[0].OldTheme);
            Assert.AreEqual(theme, raised[0].NewTheme);
            Assert.IsTrue(raised[0].ColorDictionary.ContainsKey("EnvironmentWindowBackground"));
            Assert.AreEqual(theme, ThemeService.Instance.CurrentTheme);
            Assert.AreEqual(2, Application.Current!.Resources.MergedDictionaries.Count, "the old palette must be removed");
            Assert.AreEqual(WindowBackground[theme], FindBrushColor("EnvironmentWindowBackground"));
            Assert.AreEqual(theme == Theme.Dark ? ThemeVariant.Dark : ThemeVariant.Light,
                Application.Current.RequestedThemeVariant);
        });
    }

    [TestMethod]
    public void SetTheme_UpdatesDynamicResourcesInOpenWindows()
    {
        HeadlessSession.Run(() =>
        {
            Initialize();
            var border = new Border();
            border.Bind(Border.BackgroundProperty, border.GetResourceObservable("EnvironmentWindowBackground"));
            var window = new Window { Content = border };
            window.Show();
            try
            {
                Assert.AreEqual(WindowBackground[Theme.Light], ((ISolidColorBrush)border.Background!).Color);

                ThemeService.Instance.SetTheme(Theme.Dark);
                Dispatcher.UIThread.RunJobs();

                Assert.AreEqual(WindowBackground[Theme.Dark], ((ISolidColorBrush)border.Background!).Color);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [TestMethod]
    public void ThemeNames_ParseAsUpstream()
    {
        Assert.AreEqual(Theme.Dark, ThemeResourceHelper.ParseTheme("dark"));
        Assert.IsTrue(ThemeResourceHelper.TryParseTheme("BLUE", out Theme blue));
        Assert.AreEqual(Theme.Blue, blue);
        Assert.IsFalse(ThemeResourceHelper.TryParseTheme("5", out Theme fallback), "numbers outside the enum are rejected");
        Assert.AreEqual(Theme.Light, fallback);
        Assert.ThrowsException<ArgumentNullException>(() => ThemeResourceHelper.ParseTheme(" "));
        Assert.ThrowsException<ArgumentException>(() => ThemeResourceHelper.ParseTheme("Purple"));
    }
}
