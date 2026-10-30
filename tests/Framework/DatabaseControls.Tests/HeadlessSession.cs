using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Simple;
using Themes;

[assembly: DoNotParallelize]

namespace DatabaseControls.Tests;

/// <summary>
/// The headless Avalonia application the tests run in (see Themes.Tests), with the resources RMC-BestFit's
/// App.xaml merges for these controls: GenericControls' and DatabaseControls' themes, then the theme
/// palettes and control themes that ThemeService adds after them.
/// </summary>
public sealed class TestApp : Application
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        Styles.Add(new SimpleTheme());
        // Avalonia's asset loader takes the first loaded assembly whose file name starts with an avares://
        // authority, shortest first: until DatabaseControls.dll is loaded, "DatabaseControls" finds this
        // test assembly, and the loader caches that. Loading DatabaseControls first avoids it.
        _ = typeof(TableViewer).Assembly;
        foreach (string theme in new[]
        {
            "avares://GenericControls/Themes/GenericControlsTheme.axaml",
            "avares://DatabaseControls/Themes/DatabaseControlsTheme.axaml",
        })
        {
            Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://DatabaseControls.Tests")) { Source = new Uri(theme) });
        }
    }

    /// <summary>Builds the headless application.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHarfBuzz()
            .UseThemeFonts()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>Hosts one headless Avalonia session for the test assembly (see Themes.Tests).</summary>
[TestClass]
public static class HeadlessSession
{
    private static HeadlessUnitTestSession? _session;

    /// <summary>Starts the session and the theme service.</summary>
    [AssemblyInitialize]
    public static void Start(TestContext context)
    {
        _session = HeadlessUnitTestSession.StartNew(typeof(TestApp), AvaloniaTestIsolationLevel.PerAssembly);
        Run(() => ThemeService.Instance.Initialize(Theme.Light));
    }

    /// <summary>Stops the session.</summary>
    [AssemblyCleanup]
    public static void Stop() => _session?.Dispose();

    /// <summary>Runs an action on the Avalonia UI thread and waits for it.</summary>
    public static void Run(Action action) =>
        _session!.Dispatch(action, CancellationToken.None).GetAwaiter().GetResult();
}
