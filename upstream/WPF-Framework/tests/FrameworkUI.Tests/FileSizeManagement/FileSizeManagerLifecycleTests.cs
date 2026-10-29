using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using FrameworkInterfaces;
using FrameworkUI.Tests.FrameworkUIDemo;
using Xunit;

namespace FrameworkUI.Tests.FileSizeManagement;

/// <summary>Exercises compaction through its real modal window and background worker.</summary>
public class FileSizeManagerLifecycleTests
{
    /// <summary>Closing either phase must not release the caller or the compaction guard.</summary>
    /// <param name="duringOptimize">Whether to attempt closing during optimization.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CloseDuringWork_IsVetoedUntilCompletion(bool duringOptimize)
    {
        DispatcherTestHost.Run(() =>
        {
            using var scenario = new CompactionScenario();
            bool remainedVisible = false;
            bool remainedGuarded = false;
            Action attemptClose = () => scenario.Dispatcher.Invoke(() =>
            {
                scenario.ObserveCompletion();
                var dialog = CompactionScenario.Dialog();
                dialog.Close();
                remainedVisible = dialog.IsVisible;
                remainedGuarded = ShellPublicVariables.CompactionInProgress;
            });
            if (duringOptimize) scenario.Project.OptimizeAction = attemptClose;
            else scenario.Project.CompactAction = attemptClose;

            var error = scenario.Run();

            Assert.Null(error);
            Assert.True(remainedVisible, "Closing the progress window must be vetoed while its worker is running.");
            Assert.True(remainedGuarded);
            Assert.False(scenario.ReturnedBeforeCompletion);
            Assert.Equal(new[] { "Compact", "Optimize" }, scenario.Project.Calls);
            Assert.Single(scenario.Reports);
            scenario.AssertClean();
        });
    }

    /// <summary>Worker failures reach the synchronous caller unchanged and never report success.</summary>
    /// <param name="duringOptimize">Whether optimization should fail after compaction succeeds.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorkerFailure_PreservesExceptionAndAllowsReuse(bool duringOptimize)
    {
        DispatcherTestHost.Run(() =>
        {
            using var scenario = new CompactionScenario();
            var failure = new IOException("Injected compaction failure.");
            Action fail = () => throw failure;
            if (duringOptimize) scenario.Project.OptimizeAction = fail;
            else scenario.Project.CompactAction = fail;

            var error = scenario.Run();

            Assert.Same(failure, error);
            Assert.Contains(nameof(CompactionProject), error!.StackTrace);
            Assert.Equal(duringOptimize ? new[] { "Compact", "Optimize" } : new[] { "Compact" }, scenario.Project.Calls);
            Assert.Empty(scenario.Reports);
            scenario.AssertClean();
            scenario.Project.CompactAction = null;
            scenario.Project.OptimizeAction = null;
            Assert.Null(scenario.Run());
            Assert.Single(scenario.Reports);
            scenario.AssertClean();
        });
    }

    /// <summary>Reentry must reject the second project before touching the first operation.</summary>
    [Fact]
    public void Reentry_LeavesFirstProjectAndDialogUntouched()
    {
        DispatcherTestHost.Run(() =>
        {
            using var scenario = new CompactionScenario();
            var second = new CompactionProject(scenario.Project.FullFileName);
            Exception? rejected = null;
            bool sameDialog = false;
            scenario.Project.CompactAction = () => scenario.Dispatcher.Invoke(() =>
            {
                var dialog = CompactionScenario.Dialog();
                scenario.ObserveCompletion();
                rejected = Record.Exception(() => FileSizeManager.CompactAndOptimizeFile(second));
                sameDialog = ReferenceEquals(dialog, CompactionScenario.Dialog());
            });

            Assert.Null(scenario.Run());
            var rejection = Assert.IsType<InvalidOperationException>(rejected);
            Assert.Equal("Project compaction is already in progress.", rejection.Message);
            Assert.True(sameDialog);
            Assert.Empty(second.Calls);
            Assert.Equal(new[] { "Compact", "Optimize" }, scenario.Project.Calls);
            scenario.AssertClean();
        });
    }

    /// <summary>An immediate worker may complete only after its dialog has been prepared and shown.</summary>
    [Fact]
    public void ImmediateCompletion_StartsOnceAndReportsAfterCleanup()
    {
        DispatcherTestHost.Run(() =>
        {
            using var scenario = new CompactionScenario();
            bool dialogWasVisible = false;
            scenario.Project.CompactAction = () => scenario.Dispatcher.Invoke(() =>
            {
                var dialog = CompactionScenario.Dialog();
                dialogWasVisible = dialog.IsVisible;
                // A duplicate Loaded notification must not start a second worker.
                dialog.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            });

            Assert.Null(scenario.Run());
            Assert.True(dialogWasVisible);
            Assert.Equal(new[] { "Compact", "Optimize" }, scenario.Project.Calls);
            Assert.Single(scenario.Reports);
            Assert.True(scenario.ReportsObservedCleanState);
            scenario.AssertClean();
        });
    }

    /// <summary>Failure before showing a dialog must release the guard and permit a later operation.</summary>
    [Fact]
    public void StartupFailure_CleansStateAndAllowsReuse()
    {
        DispatcherTestHost.Run(() =>
        {
            using var scenario = new CompactionScenario();
            var failure = new IOException("Injected startup failure.");
            scenario.Project.PathFailure = failure;

            Assert.Same(failure, scenario.Run());
            Assert.Empty(scenario.Project.Calls);
            Assert.Empty(scenario.Reports);
            scenario.AssertClean();
            scenario.Project.PathFailure = null;
            Assert.Null(scenario.Run());
            scenario.AssertClean();
        });
    }

    /// <summary>Hiding the modal dialog must retain protection until actual worker completion.</summary>
    [Fact]
    public void HideDuringWork_RecoversModalProtectionAndRestoresWindowStates()
    {
        DispatcherTestHost.Run(() =>
        {
            using var scenario = new CompactionScenario();
            using var release = new ManualResetEventSlim();
            var enabledWindow = new Window { Width = 100, Height = 100, ShowInTaskbar = false };
            var disabledWindow = new Window { Width = 100, Height = 100, ShowInTaskbar = false, IsEnabled = false };
            enabledWindow.Show();
            disabledWindow.Show();
            bool protectedDuringRecovery = false;
            bool windowsDisabled = false;
            bool returnedDuringRecovery = false;
            try
            {
                scenario.Project.CompactAction = () =>
                {
                    scenario.Dispatcher.Invoke(() =>
                    {
                        scenario.ObserveCompletion();
                        CompactionScenario.Dialog().Hide();
                        scenario.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                        {
                            protectedDuringRecovery = ShellPublicVariables.CompactionInProgress;
                            windowsDisabled = !enabledWindow.IsEnabled && !disabledWindow.IsEnabled;
                            returnedDuringRecovery = scenario.Returned;
                            release.Set();
                        }));
                    });
                    if (!release.Wait(TimeSpan.FromSeconds(20))) throw new TimeoutException("The recovery dispatcher did not process its probe.");
                };

                Assert.Null(scenario.Run());
                Assert.True(protectedDuringRecovery);
                Assert.True(windowsDisabled);
                Assert.False(returnedDuringRecovery);
                Assert.False(scenario.ReturnedBeforeCompletion);
                Assert.True(enabledWindow.IsEnabled);
                Assert.False(disabledWindow.IsEnabled);
                scenario.AssertClean();
            }
            finally
            {
                release.Set();
                enabledWindow.Close();
                disabledWindow.Close();
            }
        });
    }

    /// <summary>Aborted recovery retains the live writer and rejects another operation until observed completion.</summary>
    [Fact]
    public void AbortedRecovery_RetainsGuardAndResourcesWhileWorkerIsActive()
    {
        DispatcherTestHost.Run(() =>
        {
            using var scenario = new CompactionScenario();
            using var release = new ManualResetEventSlim();
            var completion = new DispatcherFrame();
            bool completed = false;
            scenario.Project.CompactAction = () =>
            {
                scenario.Dispatcher.Invoke(() =>
                {
                    var worker = Assert.IsType<BackgroundWorker>(CompactionScenario.Field("_backgroundWorker"));
                    worker.RunWorkerCompleted += (_, _) => { completed = true; completion.Continue = false; };
                    CompactionScenario.Dialog().Hide();
                    scenario.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                        Assert.IsType<DispatcherFrame>(CompactionScenario.Field("_recoveryFrame")).Continue = false));
                });
                if (!release.Wait(TimeSpan.FromSeconds(20))) throw new TimeoutException("Recovery-abort fixture was not released.");
            };
            try
            {
                var failure = Assert.Throws<InvalidOperationException>(() => FileSizeManager.CompactAndOptimizeFile(scenario.Project));
                Assert.Contains("cannot be saved or closed", failure.Message);
                Assert.True(ShellPublicVariables.CompactionInProgress);
                Assert.NotNull(CompactionScenario.Field("_backgroundWorker"));
                Assert.NotNull(CompactionScenario.Field("_timer"));
                Assert.Same(scenario.Project, CompactionScenario.Field("_project"));
                Assert.False(completed);
                Assert.Empty(scenario.Reports);
                var rejected = Assert.Throws<InvalidOperationException>(() => FileSizeManager.CompactAndOptimizeFile(scenario.Project));
                Assert.Equal("Project compaction is already in progress.", rejected.Message);
            }
            finally
            {
                release.Set();
                var watchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(25) };
                watchdog.Tick += (_, _) => completion.Continue = false;
                watchdog.Start();
                try { if (!completed) Dispatcher.PushFrame(completion); }
                finally { watchdog.Stop(); }
                Assert.True(completed);
                // The production caller intentionally retains the guard after an aborted frame.
                // The isolated fixture releases it only after observing the actual callback.
                typeof(FileSizeManager).GetMethod("CleanupOperation", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
            }
            scenario.AssertClean();
        });
    }

    /// <summary>Success observers run after cleanup and may start a new independent operation.</summary>
    [Fact]
    public void SuccessObserver_CanStartNextOperation()
    {
        DispatcherTestHost.Run(() =>
        {
            using var scenario = new CompactionScenario();
            var second = new CompactionProject(scenario.Project.FullFileName);
            bool entered = false;
            FileSizeManager.ReportProgressEventHandler observer = _ =>
            {
                if (entered) return;
                entered = true;
                FileSizeManager.CompactAndOptimizeFile(second);
            };
            FileSizeManager.ReportProgress += observer;
            try
            {
                Assert.Null(scenario.Run());
                Assert.Equal(new[] { "Compact", "Optimize" }, second.Calls);
                scenario.AssertClean();
            }
            finally
            {
                FileSizeManager.ReportProgress -= observer;
            }
        });
    }
}

/// <summary>Owns a temporary file and observes a real compaction invocation on the shared dispatcher.</summary>
internal sealed class CompactionScenario : IDisposable
{
    private bool completionObserved;
    private bool completionSubscribed;
    private DispatcherFrame? completionFrame;
    private readonly string path = Path.Combine(Path.GetTempPath(), $"framework-compaction-{Guid.NewGuid():N}.sqlite");

    /// <summary>Creates a file-backed fake project and subscribes to final reporting.</summary>
    public CompactionScenario()
    {
        File.WriteAllText(path, "Compaction lifecycle fixture");
        Project = new CompactionProject(path);
        FileSizeManager.ReportProgress += OnReport;
    }

    /// <summary>Gets the UI dispatcher on which the operation runs.</summary>
    public Dispatcher Dispatcher { get; } = System.Windows.Threading.Dispatcher.CurrentDispatcher;
    /// <summary>Gets the controlled project.</summary>
    public CompactionProject Project { get; }
    /// <summary>Gets the final reports.</summary>
    public List<string> Reports { get; } = new();
    /// <summary>Gets whether the public method returned before the completion callback.</summary>
    public bool ReturnedBeforeCompletion { get; private set; }
    /// <summary>Gets whether the invocation has returned.</summary>
    public bool Returned { get; private set; }
    /// <summary>Gets whether all reporting observed released resources.</summary>
    public bool ReportsObservedCleanState { get; private set; } = true;

    /// <summary>Invokes compaction and drains any prematurely detached worker before assertions.</summary>
    /// <returns>The exception delivered to the synchronous caller.</returns>
    public Exception? Run()
    {
        Returned = false;
        var error = Record.Exception(() => FileSizeManager.CompactAndOptimizeFile(Project));
        Returned = true;
        ReturnedBeforeCompletion = completionSubscribed && !completionObserved;
        if (ReturnedBeforeCompletion)
        {
            completionFrame = new DispatcherFrame();
            var watchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(25) };
            watchdog.Tick += (_, _) => completionFrame.Continue = false;
            watchdog.Start();
            try { Dispatcher.PushFrame(completionFrame); }
            finally { watchdog.Stop(); }
            Assert.True(completionObserved, "The detached worker failed to complete.");
        }
        return error;
    }

    /// <summary>Attaches a test observer to the currently active real worker.</summary>
    public void ObserveCompletion()
    {
        if (completionSubscribed) return;
        completionSubscribed = true;
        var worker = Assert.IsType<BackgroundWorker>(Field("_backgroundWorker"));
        worker.RunWorkerCompleted += (_, _) =>
        {
            completionObserved = true;
            if (completionFrame != null) completionFrame.Continue = false;
        };
    }

    /// <summary>Gets the currently visible compaction dialog.</summary>
    /// <returns>The operation's progress window.</returns>
    public static CompactProgressControl Dialog() => Application.Current.Windows.OfType<CompactProgressControl>().Single();

    /// <summary>Asserts that a completed operation released all owned state.</summary>
    public void AssertClean()
    {
        Assert.False(ShellPublicVariables.CompactionInProgress);
        Assert.Null(Field("_backgroundWorker"));
        Assert.Null(Field("_timer"));
        Assert.Null(Field("_progressControl"));
        Assert.Null(Field("_project"));
    }

    /// <summary>Reads owned resource fields without introducing production test seams.</summary>
    /// <param name="name">The field name.</param>
    /// <returns>The current field value.</returns>
    public static object? Field(string name) => typeof(FileSizeManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);

    /// <summary>Records whether success is emitted only after operation cleanup.</summary>
    /// <param name="message">The final success message.</param>
    private void OnReport(string message)
    {
        Reports.Add(message);
        ReportsObservedCleanState &= !ShellPublicVariables.CompactionInProgress && Field("_project") == null && Field("_backgroundWorker") == null && Field("_timer") == null && Field("_progressControl") == null;
    }

    /// <summary>Unsubscribes reporting and deletes the temporary file.</summary>
    public void Dispose()
    {
        FileSizeManager.ReportProgress -= OnReport;
        File.Delete(path);
    }
}

/// <summary>Provides controlled Compact and Optimize operations without real database work.</summary>
internal sealed class CompactionProject : ProjectBase, IProject
{
    /// <summary>Creates a project with a real temporary file.</summary>
    /// <param name="path">The owned fixture path.</param>
    public CompactionProject(string path)
    {
        FullFileName = path;
        IsDirty = false;
        ElementCollections = new ReadOnlyCollection<IElementCollection>(new List<IElementCollection>());
    }
    /// <summary>Sets a clean model with a pending layout save for shell close tests.</summary>
    public void RequireLayoutSave() { IsDirty = false; LayoutDirty = true; }
    /// <summary>Gets or sets the compaction operation.</summary>
    public Action? CompactAction { get; set; }
    /// <summary>Gets or sets the optimization operation.</summary>
    public Action? OptimizeAction { get; set; }
    /// <summary>Gets or sets the startup failure raised when reading the project path.</summary>
    public Exception? PathFailure { get; set; }
    /// <summary>Gets or sets a failure raised by the final save.</summary>
    public Exception? SaveFailure { get; set; }
    /// <summary>Gets the ordered operations requested by production code.</summary>
    public List<string> Calls { get; } = new();
    /// <inheritdoc/>
    public override string Name { get; set; } = "Compaction fixture";
    /// <inheritdoc/>
    public override string Description { get; set; } = string.Empty;
    /// <inheritdoc/>
    public override string SoftwareVersion => "1.0";
    /// <inheritdoc/>
    public override ImageSource ProjectImage => null!;
    /// <inheritdoc/>
    string IProject.FullFileName { get => PathFailure == null ? FullFileName : throw PathFailure; set => FullFileName = value; }
    /// <inheritdoc/>
    public override bool IsValid() => true;
    /// <inheritdoc/>
    public override void CreateNew(string newFullFileName) => FullFileName = newFullFileName;
    /// <inheritdoc/>
    public override void Open() => throw new NotSupportedException();
    /// <inheritdoc/>
    public override void Save() { Calls.Add("Save"); if (SaveFailure != null) throw SaveFailure; }
    /// <inheritdoc/>
    public override void Close() => Calls.Add("Close");
    /// <inheritdoc/>
    public override void Compact() { Calls.Add("Compact"); CompactAction?.Invoke(); }
    /// <inheritdoc/>
    public override void Optimize() { Calls.Add("Optimize"); OptimizeAction?.Invoke(); }
}
