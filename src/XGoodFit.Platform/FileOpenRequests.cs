using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace XGoodFit.Platform;

/// <summary>Where a request to open files came from.</summary>
public enum FileOpenSource
{
    /// <summary>The command line the application started with: how Windows and Linux open files with it.</summary>
    CommandLine,

    /// <summary>The operating system, while the application runs: macOS sends the files Finder opens.</summary>
    Activation,
}

/// <summary>Files the operating system asks the application to open.</summary>
public sealed class FileOpenEventArgs : EventArgs
{
    /// <summary>Creates the event's arguments.</summary>
    /// <param name="paths">The files' paths, in the order given.</param>
    /// <param name="source">Where the request came from.</param>
    public FileOpenEventArgs(IReadOnlyList<string> paths, FileOpenSource source)
    {
        Paths = paths;
        Source = source;
    }

    /// <summary>The files' paths, in the order given (at least one).</summary>
    public IReadOnlyList<string> Paths { get; }

    /// <summary>Where the request came from.</summary>
    public FileOpenSource Source { get; }
}

/// <summary>
/// XGoodFit addition: the files the operating system asks the application to open, from each
/// platform's own mechanism, as one event (migration plan §9.1, file association).
/// </summary>
/// <remarks>
/// <para>
/// Windows and Linux start the application with the file on its command line, at startup and while
/// another copy runs (each opened file starts a new copy, as with RMC-BestFit). macOS starts the
/// application without arguments and sends the files Finder opens as activations (Avalonia's
/// <see cref="IActivatableLifetime"/>), both the ones it starts the application with and the ones
/// opened while it runs.
/// </para>
/// <para>
/// Create it with <see cref="For(Application)"/> before the main loop starts (in
/// <see cref="Application.OnFrameworkInitializationCompleted"/>, as upstream's App handles its
/// startup arguments), so that the files macOS starts the application with aren't missed; then
/// handle <see cref="FilesOpened"/> once the main window can open a project. Requests that arrive
/// before the first handler is added are kept, and that handler gets them when it's added, on the
/// thread adding it. Later requests are raised on the UI thread.
/// </para>
/// </remarks>
public sealed class FileOpenRequests
{
    private readonly object _lock = new();
    private readonly List<FileOpenEventArgs> _pending = [];
    private EventHandler<FileOpenEventArgs>? _filesOpened;
    private IActivatableLifetime? _lifetime;

    /// <summary>Creates the requests, starting with the files on <paramref name="commandLineArgs"/>.</summary>
    /// <param name="commandLineArgs">The application's command-line arguments (see <see cref="FromCommandLine"/>).</param>
    public FileOpenRequests(IReadOnlyList<string> commandLineArgs)
    {
        ArgumentNullException.ThrowIfNull(commandLineArgs);
        IReadOnlyList<string> paths = FromCommandLine(commandLineArgs);
        if (paths.Count > 0)
            _pending.Add(new FileOpenEventArgs(paths, FileOpenSource.CommandLine));
    }

    /// <summary>
    /// Creates the requests for <paramref name="application"/>: the arguments of its desktop lifetime,
    /// and, where the platform has them (macOS), its activations.
    /// </summary>
    /// <param name="application">The application, after its lifetime is set.</param>
    public static FileOpenRequests For(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);
        string[] args = (application.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Args ?? [];
        var requests = new FileOpenRequests(args);
        requests.Attach(application.TryGetFeature<IActivatableLifetime>());
        return requests;
    }

    /// <summary>
    /// Raised when the operating system asks the application to open files. The first handler added
    /// also gets the requests that came before it.
    /// </summary>
    public event EventHandler<FileOpenEventArgs>? FilesOpened
    {
        add
        {
            FileOpenEventArgs[] earlier;
            lock (_lock)
            {
                bool first = _filesOpened == null;
                _filesOpened += value;
                earlier = first && value != null ? [.. _pending] : [];
                if (earlier.Length > 0)
                    _pending.Clear();
            }
            foreach (FileOpenEventArgs request in earlier)
                value!(this, request);
        }
        remove
        {
            lock (_lock)
                _filesOpened -= value;
        }
    }

    /// <summary>
    /// Listens to <paramref name="lifetime"/>'s file activations, which macOS sends for the files
    /// Finder opens. Without one (Windows and Linux) there are none.
    /// </summary>
    /// <param name="lifetime">The platform's activatable lifetime, or null.</param>
    public void Attach(IActivatableLifetime? lifetime)
    {
        if (_lifetime != null)
            _lifetime.Activated -= OnActivated;
        _lifetime = lifetime;
        if (lifetime != null)
            lifetime.Activated += OnActivated;
    }

    /// <summary>
    /// The files on a command line, as RMC-BestFit's <c>App.Application_Startup</c> reads them: one
    /// argument is the file (opened from Explorer or a file manager); several are one path with
    /// spaces that wasn't quoted, joined with spaces (opened from the Jump List). macOS's old
    /// process-serial-number argument (<c>-psn_…</c>) is ignored.
    /// </summary>
    /// <param name="args">The command-line arguments, without the program's name.</param>
    /// <returns>The path to open, or none.</returns>
    public static IReadOnlyList<string> FromCommandLine(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        string[] used = args.Where(arg => !arg.StartsWith("-psn_", StringComparison.Ordinal)).ToArray();
        if (used.Length == 0)
            return [];
        string path = used.Length == 1 ? used[0] : string.Join(" ", used);
        return string.IsNullOrWhiteSpace(path) ? [] : [path];
    }

    /// <summary>Handles an activation of the lifetime <see cref="Attach"/> listens to.</summary>
    internal void OnActivated(object? sender, ActivatedEventArgs e)
    {
        if (e is not FileActivatedEventArgs files)
            return;
        string[] paths = files.Files.Select(file => file.TryGetLocalPath()).OfType<string>().ToArray();
        if (paths.Length > 0)
            Raise(new FileOpenEventArgs(paths, FileOpenSource.Activation));
    }

    private void Raise(FileOpenEventArgs request)
    {
        EventHandler<FileOpenEventArgs>? handlers;
        lock (_lock)
        {
            handlers = _filesOpened;
            if (handlers == null)
            {
                _pending.Add(request);
                return;
            }
        }
        handlers(this, request);
    }
}
