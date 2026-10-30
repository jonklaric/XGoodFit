using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Simple;

namespace RMC.BestFit.FileCompat.Tests;

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
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>Hosts one headless Avalonia session for the test assembly (see Themes.Tests).</summary>
/// <remarks>
/// The port's plots are Avalonia controls, which belong to the application's UI thread, so the tests
/// open and save projects there, as upstream ran them on an STA thread for WPF.
/// </remarks>
[TestClass]
public static class HeadlessSession
{
    private static HeadlessUnitTestSession? _session;

    /// <summary>Starts the session and its application.</summary>
    [AssemblyInitialize]
    public static void Start(TestContext context)
    {
        _session = HeadlessUnitTestSession.StartNew(typeof(TestApp), AvaloniaTestIsolationLevel.PerAssembly);
        Run(() => Application.Current);
    }

    /// <summary>Stops the session.</summary>
    [AssemblyCleanup]
    public static void Stop() => _session?.Dispose();

    /// <summary>Runs a function on the Avalonia UI thread and waits for its result.</summary>
    public static T Run<T>(Func<T> function) =>
        _session!.Dispatch(function, CancellationToken.None).GetAwaiter().GetResult();
}
