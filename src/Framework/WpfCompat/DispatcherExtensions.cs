using Avalonia.Threading;

namespace WpfCompat;

/// <summary>
/// WPF's <c>Dispatcher.HasShutdownStarted</c> and <c>HasShutdownFinished</c>, which Avalonia keeps
/// internal, followed from its <c>ShutdownStarted</c> and <c>ShutdownFinished</c> events.
/// </summary>
public static class DispatcherExtensions
{
    private static bool _shutdownStarted;
    private static bool _shutdownFinished;

    static DispatcherExtensions()
    {
        Dispatcher.UIThread.ShutdownStarted += (_, _) => _shutdownStarted = true;
        Dispatcher.UIThread.ShutdownFinished += (_, _) => _shutdownFinished = true;
    }

    /// <summary>Starts following the dispatcher's shutdown. Called with command routing's start.</summary>
    public static void Initialize()
    {
    }

    extension(Dispatcher dispatcher)
    {
        /// <summary>Whether the user-interface thread's dispatcher has begun shutting down.</summary>
        public bool HasShutdownStarted => _shutdownStarted;

        /// <summary>Whether the user-interface thread's dispatcher has shut down.</summary>
        public bool HasShutdownFinished => _shutdownFinished;
    }
}
