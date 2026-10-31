using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Reproducibility;
using RMC.BestFit.UI;

namespace BestFitFileCompat;

/// <summary>
/// The reproducibility check's reference on Windows (docs/reproducibility.md): estimates every example's estimated
/// analyses again with upstream's own WPF RMC.BestFit.UI, exactly as tools/Reproducibility does with the port (its
/// Runner, Extract and Records files are compiled here), and writes the same run file. Its compare step then sets
/// XGoodFit's Windows rerun against RMC-BestFit's own, which must be identical.
/// </summary>
public static class Rerun
{
    /// <summary>Runs the command.</summary>
    public static int Run(string examplesFolder, string output, string[] only, TimeSpan limit)
    {
        Version version = typeof(BestFitProject).Assembly.GetName().Version!;
        string application = $"RMC-BestFit {version.Major}.{version.Minor}.{version.Build}";
        return Runner.Run(examplesFolder, output, only, limit, new WpfUiThread(), application);
    }

    /// <summary>
    /// RMC-BestFit's UI thread: this STA thread with a WPF dispatcher, whose synchronization context an analysis's
    /// awaits return to, as in the application.
    /// </summary>
    private sealed class WpfUiThread : IUiThread
    {
        public WpfUiThread() =>
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

        public T Run<T>(Func<T> function) => function();

        // Runs the dispatcher until the task completes, as the application's message loop would.
        public T Await<T>(Func<Task<T>> function)
        {
            Task<T> task = function();
            var frame = new DispatcherFrame();
            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
            Dispatcher.PushFrame(frame);
            return task.GetAwaiter().GetResult();
        }
    }
}
