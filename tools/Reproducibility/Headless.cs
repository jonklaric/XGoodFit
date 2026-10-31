using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Simple;

namespace Reproducibility;

/// <summary>The headless Avalonia application the projects are opened in (as the file-compatibility suite's).</summary>
public sealed class HeadlessApp : Application
{
    /// <inheritdoc/>
    public override void Initialize() => Styles.Add(new SimpleTheme());

    /// <summary>Builds the headless application.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessApp>()
            .UseSkia()
            .UseHarfBuzz()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>
/// One headless Avalonia session for the whole run. The port's plots are Avalonia objects that belong to the
/// application's UI thread, so projects are opened, estimated and read there, as the application does; the analyses'
/// own parallel loops run on the thread pool, as in the application.
/// </summary>
public sealed class Headless : IUiThread, IDisposable
{
    private readonly HeadlessUnitTestSession _session = HeadlessUnitTestSession.StartNew(typeof(HeadlessApp), AvaloniaTestIsolationLevel.PerAssembly);

    /// <summary>Starts the session and its application.</summary>
    public Headless() => Run(() => Application.Current);

    /// <summary>Runs a function on the UI thread and waits for its result.</summary>
    public T Run<T>(Func<T> function) => _session.Dispatch(function, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Runs an asynchronous function on the UI thread and waits for its result.</summary>
    public T Await<T>(Func<Task<T>> function) => _session.Dispatch(function, CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public void Dispose() => _session.Dispose();
}
