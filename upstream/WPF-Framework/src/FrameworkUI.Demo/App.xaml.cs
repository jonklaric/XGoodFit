using GenericControls;
using SoftwareUpdate;
using SoftwareUpdate.GitHub;
using SoftwareUpdate.Utilities;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;
using FrameworkUI.Demo.UI;

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
    /// </remarks>
    public partial class App : Application
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class and configures the Windows jump list.
        /// </summary>
        public App()
        {
            // Register assembly resolver to load DLLs from the Libraries subfolder.
            // This must be done before any external assemblies are referenced.
            AssemblyResolver.Register();

            // Set WPF to use the current culture for all bindings (international number support)
            // This ensures StringFormat in XAML bindings uses the user's locale settings
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(
                        System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag)));


            var jList = new JumpList { ShowRecentCategory = false };
            jList.Apply();
            JumpList.SetJumpList(Application.Current, jList);
        }

        /// <summary>
        /// Handles dispatcher exceptions that are known to be benign.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The dispatcher exception event arguments.</param>
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
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
        private void Application_Startup(object sender, StartupEventArgs e)
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
            // default WPF style instead of throwing ResourceReferenceKeyNotFoundException
            // and crashing before the main window shows.
            DemoProject project = DemoProject.GetInstance();
            var projectNode = new DemoProjectNode(project);
            if (TryFindResource("TreeViewItemStyle") is Style treeItemStyle)
            {
                projectNode.Style = treeItemStyle;
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

            mainWindow.Show();
        }

        /// <summary>
        /// Automatically checks for updates after a delay and prompts the user if available.
        /// </summary>
        /// <param name="mainWindow">The main application window.</param>
        /// <param name="updateService">The update service to use for checking updates.</param>
        /// <param name="delayMs">The delay in milliseconds before checking for updates.</param>
        private async void AutoCheckForUpdatesAsync(FrameworkUI.MainWindow mainWindow, IUpdateService updateService, int delayMs)
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
                    await mainWindow.Dispatcher.InvokeAsync(() =>
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
