using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FrameworkInterfaces;
using FrameworkUI.Tests.FrameworkUIDemo;
using Xunit;

namespace FrameworkUI.Tests.FileSizeManagement;

/// <summary>Exercises shell compaction callers in isolated processes because closing shuts down WPF.</summary>
public class MainWindowCompactionTests
{
    private const string ChildCaseVariable = "FRAMEWORK_COMPACTION_CHILD_CASE";

    /// <summary>Preserves final save ordering and errors while reporting the actual compaction failure.</summary>
    /// <param name="testCase">The isolated close or manual-command scenario.</param>
    /// <returns>The asynchronous child-process verification.</returns>
    [Theory]
    [InlineData("success")]
    [InlineData("compact-failure")]
    [InlineData("optimize-failure")]
    [InlineData("save-failure")]
    [InlineData("manual-failure")]
    [InlineData("active-guard")]
    [InlineData("modal-worker-failure")]
    [InlineData("recovery-worker-failure")]
    [InlineData("cleanup-failure")]
    [InlineData("worker-cleanup-failure")]
    public async Task ShellCompaction_PreservesSaveAndErrorContracts(string testCase)
    {
        var childCase = Environment.GetEnvironmentVariable(ChildCaseVariable);
        if (childCase != null)
        {
            if (childCase == testCase) DispatcherTestHost.Run(() => RunChild(testCase));
            return;
        }

        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("vstest");
        start.ArgumentList.Add(typeof(MainWindowCompactionTests).Assembly.Location);
        start.ArgumentList.Add("--TestCaseFilter:FullyQualifiedName~MainWindowCompactionTests.ShellCompaction_PreservesSaveAndErrorContracts");
        start.Environment[ChildCaseVariable] = testCase;
        using var child = Process.Start(start)!;
        Task<string> output = child.StandardOutput.ReadToEndAsync();
        Task<string> error = child.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try { await child.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException)
        {
            child.Kill(entireProcessTree: true);
            throw new TimeoutException($"The isolated compaction scenario '{testCase}' did not finish.");
        }
        Assert.True(child.ExitCode == 0, $"{testCase}: {await output}\n{await error}");
    }

    /// <summary>Runs one real main-window scenario with isolated settings and a controlled project.</summary>
    /// <param name="testCase">The scenario to execute.</param>
    private static void RunChild(string testCase)
    {
        if (testCase is "modal-worker-failure" or "recovery-worker-failure" or "cleanup-failure" or "worker-cleanup-failure")
        {
            CompactionFailureScenarios.Run(testCase);
            return;
        }
        string folder = Path.Combine(Path.GetTempPath(), $"framework-shell-compaction-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            foreach (var field in typeof(ShellPublicVariables).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsInitOnly || field.FieldType != typeof(string) || !field.Name.EndsWith("Path", StringComparison.Ordinal)) continue;
                field.SetValue(null, field.Name.EndsWith("FolderPath", StringComparison.Ordinal) ? folder + Path.DirectorySeparatorChar : Path.Combine(folder, field.Name + ".xml"));
            }
            ThemeManager.SetTheme(ThemeColor.Light);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/GenericControls;component/Themes/GenericControlsTheme.xaml") });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/FrameworkUI;component/Icons/IconDictionary.xaml") });
            using var scenario = new CompactionScenario();
            var window = new MainWindow { ProjectNode = new CompactionController(scenario.Project), ShowInTaskbar = false };
            UserSettings.SaveWindowLayout = false;
            UserSettings.CreateAutoRecoverBackup = false;
            UserSettings.CompressProjectFileOnClose = true;
            window.Show();
            // Shutdown clears application resources; queued undo command queries are outside this lifecycle test.
            window.CommandBindings.Clear();
            scenario.Project.RequireLayoutSave();
            var injected = new IOException($"Actual {testCase} error.");
            string? displayedError = null;
            bool saveWasBlockedDuringWork = false;
            Action fail = () =>
            {
                scenario.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => displayedError = RespondToMessage("OK")));
                throw injected;
            };
            scenario.Project.CompactAction = () => scenario.Dispatcher.Invoke(() =>
            {
                CompactionScenario.Dialog().Close();
                saveWasBlockedDuringWork = !scenario.Project.Calls.Contains("Save") && !scenario.Project.Calls.Contains("Close") && ShellPublicVariables.CompactionInProgress;
            });
            if (testCase is "compact-failure" or "manual-failure") scenario.Project.CompactAction = fail;
            if (testCase == "optimize-failure") scenario.Project.OptimizeAction = fail;
            if (testCase == "save-failure") scenario.Project.SaveFailure = injected;

            if (testCase == "manual-failure")
            {
                scenario.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => RespondToMessage("Yes")));
                Invoke(window, "CompactProjectFile_Click", window, new RoutedEventArgs());
                Assert.Contains(injected.Message, displayedError);
                Assert.Equal(new[] { "Compact" }, scenario.Project.Calls);
                Assert.Empty(scenario.Reports);
                scenario.AssertClean();
                return;
            }
            if (testCase == "active-guard")
            {
                scenario.Project.CompactAction = () => scenario.Dispatcher.Invoke(() =>
                {
                    var closing = new CancelEventArgs();
                    Invoke(window, "MainWindow_Closing", window, closing);
                    Assert.True(closing.Cancel);
                    Assert.DoesNotContain("Save", scenario.Project.Calls);
                    Assert.DoesNotContain("Close", scenario.Project.Calls);
                });
                Assert.Null(scenario.Run());
                Assert.Equal(new[] { "Compact", "Optimize" }, scenario.Project.Calls);
                return;
            }

            Exception? failure = Record.Exception(window.Close);
            if (testCase == "save-failure")
            {
                Assert.Same(injected, failure);
                Assert.Equal(new[] { "Compact", "Optimize", "Save" }, scenario.Project.Calls);
            }
            else
            {
                Assert.Null(failure);
                Assert.Equal(testCase == "compact-failure" ? new[] { "Compact", "Save", "Close" } : new[] { "Compact", "Optimize", "Save", "Close" }, scenario.Project.Calls);
            }
            if (testCase is "compact-failure" or "optimize-failure")
            {
                Assert.Contains(injected.Message, displayedError);
                Assert.Empty(scenario.Reports);
            }
            else
            {
                Assert.True(saveWasBlockedDuringWork);
                Assert.Single(scenario.Reports);
            }
            scenario.AssertClean();
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    /// <summary>Reads and responds to the actual themed modal message without user interaction.</summary>
    /// <param name="button">The response button text.</param>
    /// <returns>The message shown to the user.</returns>
    private static string RespondToMessage(string button)
    {
        var dialog = Application.Current.Windows.OfType<GenericControls.MessageBoxWindow>().Single();
        string message = ((TextBlock)dialog.FindName("MessageText")).Text;
        var panel = (StackPanel)dialog.FindName("ButtonPanel");
        panel.Children.OfType<Button>().Single(item => (string)item.Content == button).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        return message;
    }

    /// <summary>Calls an existing private event handler without adding production test seams.</summary>
    /// <param name="window">The shell instance.</param>
    /// <param name="method">The event-handler name.</param>
    /// <param name="arguments">The original event arguments.</param>
    private static void Invoke(MainWindow window, string method, params object[] arguments)
    {
        try { typeof(MainWindow).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, arguments); }
        catch (TargetInvocationException ex) when (ex.InnerException != null) { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); }
    }
}

/// <summary>Supplies a project-only controller for exercising shell lifecycle code.</summary>
internal sealed class CompactionController : FrameworkUIController
{
    /// <summary>Creates a controller for the controlled project.</summary>
    /// <param name="project">The project owned by the test fixture.</param>
    public CompactionController(IProject project) : base(project) { }
    /// <inheritdoc/>
    public override bool CanMultiSelect => false;
    /// <inheritdoc/>
    protected override void DefineProjectExplorerMenuItems() { }
    /// <inheritdoc/>
    public override Control GetDocumentControl(IElement element) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override void DocumentClosed(UIElement documentControl) { }
    /// <inheritdoc/>
    public override void PropertiesClosed(UIElement propertiesControl) { }
    /// <inheritdoc/>
    public override Control GetPropertiesControl(UIElement documentControl) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override Control GetPropertiesControl(IElement element) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override IElement GetControlElement(UIElement control) => throw new NotSupportedException();
}
