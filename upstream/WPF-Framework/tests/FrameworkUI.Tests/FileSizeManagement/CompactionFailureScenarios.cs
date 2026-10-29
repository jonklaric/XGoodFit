using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Xunit;

namespace FrameworkUI.Tests.FileSizeManagement;

/// <summary>Injects actual dispatcher and disposal failures in isolated compaction test processes.</summary>
internal static class CompactionFailureScenarios
{
    /// <summary>Exercises the selected error through modal recovery or owned resource cleanup.</summary>
    /// <param name="testCase">The failure scenario selected by the parent test.</param>
    public static void Run(string testCase)
    {
        if (testCase == "cleanup-failure") { AssertCleanupContinues(); return; }
        if (testCase == "worker-cleanup-failure") { AssertWorkerErrorSurvivesCleanup(); return; }
        using var scenario = new CompactionScenario();
        using var release = new ManualResetEventSlim();
        var primary = new IOException("The modal or recovery operation failed first.");
        var secondary = new IOException("The worker failure must remain secondary.");
        var window = new Window { Width = 100, Height = 100, ShowInTaskbar = false };
        window.Show();
        var secondWindow = new Window { Width = 100, Height = 100, ShowInTaskbar = false };
        secondWindow.Show();
        DependencyPropertyChangedEventHandler restoreFailure = (_, e) =>
        {
            if ((bool)e.NewValue) throw primary;
        };
        if (testCase == "recovery-worker-failure") window.IsEnabledChanged += restoreFailure;
        scenario.Project.CompactAction = () =>
        {
            scenario.Dispatcher.Invoke(() =>
            {
                scenario.ObserveCompletion();
                if (testCase == "modal-worker-failure")
                    scenario.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => throw primary));
                else
                    CompactionScenario.Dialog().Hide();
                scenario.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(release.Set));
            });
            if (!release.Wait(TimeSpan.FromSeconds(20))) throw new TimeoutException("The recovery frame did not release its worker.");
            throw secondary;
        };
        try
        {
            Assert.Same(primary, scenario.Run());
            Assert.True(secondWindow.IsEnabled, "Later window states must be restored even when an earlier restoration raises an error.");
            Assert.Empty(scenario.Reports);
            scenario.AssertClean();
        }
        finally
        {
            release.Set();
            window.IsEnabledChanged -= restoreFailure;
            window.Close();
            secondWindow.Close();
        }
    }

    /// <summary>Verifies that a completed worker error survives a later owned-resource disposal error.</summary>
    private static void AssertWorkerErrorSurvivesCleanup()
    {
        using var scenario = new CompactionScenario();
        var primary = new IOException("The worker failed before cleanup.");
        var secondary = new IOException("Cleanup must not replace the worker failure.");
        scenario.Project.CompactAction = () =>
        {
            scenario.Dispatcher.Invoke(() =>
            {
                var worker = Assert.IsType<BackgroundWorker>(CompactionScenario.Field("_backgroundWorker"));
                worker.RunWorkerCompleted += (_, _) =>
                {
                    // Substitute only after the real callback has completed its work.
                    SetField("_backgroundWorker", new ThrowingDisposeWorker(secondary));
                    worker.Dispose();
                };
            });
            throw primary;
        };
        Assert.Same(primary, scenario.Run());
        Assert.Empty(scenario.Reports);
        scenario.AssertClean();
    }
    /// <summary>Verifies that disposal failure cannot skip closing later owned resources.</summary>
    private static void AssertCleanupContinues()
    {
        using var scenario = new CompactionScenario();
        var failure = new IOException("Disposing the completed worker failed.");
        var timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
        var dialog = new CompactProgressControl();
        var worker = new ThrowingDisposeWorker(failure);
        timer.Start();
        dialog.Show();
        SetField("_timer", timer);
        SetField("_progressControl", dialog);
        SetField("_backgroundWorker", worker);
        SetField("_project", scenario.Project);
        SetField("_operationActive", 1);
        ShellPublicVariables.CompactionInProgress = true;
        try
        {
            var thrown = Assert.Throws<TargetInvocationException>(() =>
                typeof(FileSizeManager).GetMethod("CleanupOperation", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null));
            Assert.Same(failure, thrown.InnerException);
            Assert.False(timer.IsEnabled);
            Assert.False(dialog.IsVisible, "The progress window must close even when worker disposal fails.");
            scenario.AssertClean();
        }
        finally { timer.Stop(); dialog.Close(); }
    }

    /// <summary>Sets an owned resource for a focused cleanup fault without exposing production hooks.</summary>
    /// <param name="name">The existing private field.</param>
    /// <param name="value">The test-owned resource or state.</param>
    private static void SetField(string name, object value) => typeof(FileSizeManager).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, value);

    /// <summary>Raises a disposal failure after normal BackgroundWorker cleanup.</summary>
    private sealed class ThrowingDisposeWorker : BackgroundWorker
    {
        private readonly Exception failure;
        /// <summary>Stores the exact error instance to raise during disposal.</summary>
        /// <param name="failure">The injected cleanup failure.</param>
        public ThrowingDisposeWorker(Exception failure) => this.failure = failure;
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) throw failure;
        }
    }
}
