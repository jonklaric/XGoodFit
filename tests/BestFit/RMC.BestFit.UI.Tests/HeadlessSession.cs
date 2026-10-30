using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Simple;

namespace RMC.BestFit.UI.Tests;

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
[TestClass]
public static class HeadlessSession
{
    private static HeadlessUnitTestSession? _session;

    /// <summary>Starts the session and its application.</summary>
    /// <remarks>
    /// Upstream's tests run in parallel, and Avalonia makes the first thread that creates a dispatcher
    /// its UI thread. So the application starts here, before any test can create Avalonia objects on
    /// its own thread.
    /// </remarks>
    [AssemblyInitialize]
    public static void Start(TestContext context)
    {
        _session = HeadlessUnitTestSession.StartNew(typeof(TestApp), AvaloniaTestIsolationLevel.PerAssembly);
        Run(() => Application.Current);
    }

    /// <summary>Stops the session.</summary>
    /// <remarks>
    /// MSTest 3.6 can run the assembly cleanup more than once, on several threads, when parallel tests
    /// finish together, so only the first call disposes the session (a second Dispose throws).
    /// </remarks>
    [AssemblyCleanup]
    public static void Stop() => Interlocked.Exchange(ref _session, null)?.Dispose();

    /// <summary>Runs a function on the Avalonia UI thread and waits for its result.</summary>
    public static T Run<T>(Func<T> function) =>
        _session!.Dispatch(function, CancellationToken.None).GetAwaiter().GetResult();
}
