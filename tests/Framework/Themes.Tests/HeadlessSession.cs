using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Simple;

[assembly: DoNotParallelize]

namespace Themes.Tests;

/// <summary>
/// The Avalonia application the tests run in: headless, but rendering with Skia so tests can capture frames.
/// </summary>
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

/// <summary>
/// Hosts one headless Avalonia session for the test assembly. MSTest has no Avalonia integration, so
/// tests run their Avalonia code on the session's UI thread through <see cref="Run"/>.
/// </summary>
/// <remarks>
/// All tests share one <see cref="Application"/>, as the real app does: <see cref="ThemeService"/> is a
/// process-wide singleton bound to the application it was initialized in. Avalonia's default
/// isolation would create a new application for every test.
/// </remarks>
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

/// <summary>Locates the repository, so tests can compare the port with the vendored upstream files.</summary>
public static class RepoPaths
{
    /// <summary>The repository root: the first folder above the test binaries that holds upstream.lock.</summary>
    public static string Root { get; } = FindRoot();

    /// <summary>The vendored WPF-Framework sources.</summary>
    public static string UpstreamFrameworkSrc => Path.Combine(Root, "upstream", "WPF-Framework", "src");

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "upstream.lock")))
            {
                return directory.FullName;
            }
        }
        throw new DirectoryNotFoundException("Could not find the repository root (upstream.lock).");
    }
}
