using GenericControls;
using SoftwareUpdate;
using SoftwareUpdate.GitHub;
using SoftwareUpdate.Utilities;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using FrameworkUI.Demo.UI;
using WpfCompat;

#nullable enable

namespace FrameworkUI.Demo
{
    /// <summary>
    /// Main application class for the FrameworkUI.Demo application. Handles application startup, initialization of the theme system, and configuration of the software update service.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// XGoodFit port: Avalonia's desktop lifetime raises Startup and closes with the main window
    /// (OnFrameworkInitializationCompleted), and its dispatcher reports unhandled exceptions.
    /// </para>
    /// </remarks>
    public partial class App : Application
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        /// <remarks>
        /// XGoodFit port: upstream registered its assembly resolver here (AssemblyResolver.cs, for the
        /// Windows installer's Libraries folder), set WPF's binding language to the current culture,
        /// which Avalonia's bindings use already, and applied an empty Windows jump list, which
        /// FrameworkUI's RecentFiles doesn't keep on any platform. None of these is needed.
        /// </remarks>
        public App()
        {
            Dispatcher.UIThread.UnhandledException += Application_DispatcherUnhandledException;
        }

        /// <inheritdoc/>
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        /// <inheritdoc/>
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                desktop.Startup += Application_Startup;
            }
            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Handles dispatcher exceptions that are known to be benign.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The dispatcher exception event arguments.</param>
        private void Application_DispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Application.DispatcherUnhandledException: {e.Exception}");
            if (e.Exception is COMException comException && comException.ErrorCode == -2147221040)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the application startup event. Initializes the theme system, creates the project model, configures the main window, and sets up the software update service.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The startup event arguments.</param>
        private void Application_Startup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
        {
            // XGoodFit port: the main window is made as upstream made it here (CreateMainWindow), which
            // the capture mode shares, and becomes the lifetime's main window, as WPF's first window did.
            FrameworkUI.MainWindow mainWindow = CreateMainWindow();
            ((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).MainWindow = mainWindow;
            mainWindow.Show();
        }

        /// <summary>
        /// Initializes the theme system, creates the project model and the main window for it, and sets
        /// up the software update service: the body of upstream's Application_Startup.
        /// </summary>
        /// <returns>The main window, not yet shown.</returns>
        /// <remarks>XGoodFit port: split from <see cref="Application_Startup"/> for the capture mode.</remarks>
        internal static FrameworkUI.MainWindow CreateMainWindow()
        {
            // Initialize theme system - this must be done before creating any UI
            // ThemeService loads control templates and color palettes from the Themes library
            // ThemeManager bridges to ThemeService and loads ProjectUI-specific resources
            FrameworkUI.ThemeManager.SetTheme(FrameworkUI.ThemeColor.Light);

            FrameworkUI.ShellPublicVariables.SoftwareVersionDate = "July 2026";
            FrameworkUI.ShellPublicVariables.SoftwareExtension = ".demo";
            FrameworkUI.UserSettings.CreateAutoRecoverBackup = false;

            // Create the project model. Use TryFindResource so a missing TreeViewItemStyle
            // (e.g., if theme resource loading didn't complete) degrades gracefully to the
            // default style instead of throwing and crashing before the main window shows.
            // XGoodFit port: the style is a control theme, the node's Theme.
            DemoProject project = DemoProject.GetInstance();
            var projectNode = new DemoProjectNode(project);
            if (Current!.TryFindResource("TreeViewItemStyle") is ControlTheme treeItemStyle)
            {
                projectNode.Theme = treeItemStyle;
            }

            project.CreateNewDummyProject();

            // Create and Show the Main Window
            FrameworkUI.MainWindow mainWindow = new FrameworkUI.MainWindow(); //{ ProjectNode = projectNode };
            mainWindow.ProjectNode = projectNode;

            // =================================================================
            // SOFTWARE UPDATE SERVICE EXAMPLE
            // =================================================================
            // This demonstrates how to configure the SoftwareUpdate library
            // for checking updates from GitHub releases.
            //
            // For RMC-BestFit, configure with actual values:
            //   GitHubOwner = "USACE-RMC"
            //   GitHubRepo = "RMC-BestFit"
            //   CurrentVersion = your app's current version
            //   AssetNamePattern = "RMC-BestFit.Version.*.zip"
            // =================================================================

            // Auto-check settings (not part of UpdateOptions, handled locally)
            bool autoCheckOnStartup = false;
            int autoCheckDelayMs = 5000;  // 5 second delay after startup

            // Configure update options for your GitHub repository
            var updateOptions = new UpdateOptions
            {
                // GitHub repository settings
                GitHubOwner = "USACE-RMC",          // Your GitHub organization/user
                GitHubRepo = "Demo-Project",         // Your repository name (change to actual repo)

                // Current application version (parse from assembly or hardcode)
                CurrentVersion = new SemanticVersion(1, 0, 0),

                // Pattern to match release assets (supports * wildcard)
                AssetNamePattern = "Demo-Project.*.zip",

                // Include pre-release versions (beta, alpha, rc)
                IncludePreReleases = false,

                // Create backup before updating
                CreateBackup = true,

                // Name of main executable to restart after update
                MainExecutableName = "FrameworkUI.Demo.exe"
            };

            // Create the update service
            var updateService = new GitHubUpdateService(updateOptions);

            // Assign to MainWindow - this enables the "Check for Updates" menu item
            mainWindow.UpdateService = updateService;

            // Optional: Subscribe to update events for logging or custom handling
            updateService.UpdateCheckCompleted += (s, result) =>
            {
                if (result.IsUpdateAvailable)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Update available: {result.Update?.Version}");
                }
            };

            updateService.UpdateError += (s, ex) =>
            {
                System.Diagnostics.Debug.WriteLine($"Update error: {ex.Message}");
            };

            // Optional: Trigger auto-check after startup delay
            if (autoCheckOnStartup)
            {
                AutoCheckForUpdatesAsync(mainWindow, updateService, autoCheckDelayMs);
            }

            return mainWindow;
        }

        /// <summary>
        /// Automatically checks for updates after a delay and prompts the user if available.
        /// </summary>
        /// <param name="mainWindow">The main application window.</param>
        /// <param name="updateService">The update service to use for checking updates.</param>
        /// <param name="delayMs">The delay in milliseconds before checking for updates.</param>
        private static async void AutoCheckForUpdatesAsync(FrameworkUI.MainWindow mainWindow, IUpdateService updateService, int delayMs)
        {
            try
            {
                // Wait for the application to fully initialize
                await Task.Delay(delayMs);

                // Check for updates
                var result = await updateService.CheckForUpdateAsync();

                if (result.IsUpdateAvailable && result.Update != null)
                {
                    // Don't prompt if the user has chosen to skip this version
                    if (updateService.IsVersionSkipped(result.Update.Version))
                        return;

                    // Show update notification on the UI thread
                    // XGoodFit port: Avalonia's UI thread dispatcher, which WPF's window had.
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var message = $"A new version ({result.Update.Version}) is available.\n\n" +
                                     $"You are currently running version {updateService.Options.CurrentVersion}.\n\n" +
                                     "Would you like to download and install the update now?";

                        var msgResult = GenericControls.MessageBox.Show(
                            mainWindow,
                            message,
                            "Update Available",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (msgResult == MessageBoxResult.Yes)
                        {
                            // Trigger the same update flow as the menu item
                            // The MainWindow's CheckForUpdates_Click handler will show the full dialog
                            // For auto-update, we just notify - user can use menu to proceed
                            GenericControls.MessageBox.Show(
                                mainWindow,
                                $"Use Tools ? Check for Updates to download version {result.Update.Version}.",
                                "Update Available",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Silently fail for auto-check - don't interrupt user's workflow
                System.Diagnostics.Debug.WriteLine($"Auto-update check failed: {ex.Message}");
            }
        }
    }
}
