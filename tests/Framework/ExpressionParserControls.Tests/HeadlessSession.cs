using Avalonia;
using Avalonia.Headless;
using Themes;
using Avalonia.Themes.Simple;

[assembly: DoNotParallelize]

namespace ExpressionParserControls.Tests;

/// <summary>The headless Avalonia application the tests run in (see Themes.Tests).</summary>
public sealed class TestApp : Application
{
    /// <inheritdoc/>
    public override void Initialize() => Styles.Add(new SimpleTheme());

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

    /// <summary>Starts the session.</summary>
    [AssemblyInitialize]
    public static void Start(TestContext context) =>
        _session = HeadlessUnitTestSession.StartNew(typeof(TestApp), AvaloniaTestIsolationLevel.PerAssembly);

    /// <summary>Stops the session.</summary>
    [AssemblyCleanup]
    public static void Stop() => _session?.Dispose();

    /// <summary>Runs an action on the Avalonia UI thread and waits for it.</summary>
    public static void Run(Action action) =>
        _session!.Dispatch(action, CancellationToken.None).GetAwaiter().GetResult();
}
