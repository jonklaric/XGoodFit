using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using SoftwareUpdate;
using WpfCompat;
using XGoodFit.Platform;

namespace RMC_BestFit
{
    /// <summary>
    /// Main application class for the RMC-BestFit WPF application. Handles application initialization,
    /// startup procedures, exception handling, and Windows Jump List management.
    /// </summary>
    /// <remarks>
    /// XGoodFit port: Avalonia's desktop lifetime raises Startup and closes with the main window
    /// (<see cref="OnFrameworkInitializationCompleted"/>), and its dispatcher reports unhandled
    /// exceptions. Files to open come from <see cref="FileOpenRequests"/>: the command line, as
    /// upstream's, and the files Finder opens on macOS. Updates are XGoodFit's own, through Velopack
    /// (<see cref="VelopackUpdateService"/>).
    /// </remarks>
    public partial class App : Application
    {
        internal const string ProductVersion = "2.0.1";
        internal const string ProductVersionDate = "October 2026";

        /// <summary>The files the operating system asks the application to open.</summary>
        private FileOpenRequests _fileOpenRequests;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class. Configures the Windows Jump List
        /// to hide recent items for improved user privacy and cleaner task bar integration.
        /// </summary>
        /// <remarks>
        /// XGoodFit port: upstream registered its assembly resolver here (AssemblyResolver.cs, for the
        /// Windows installer's libraries folder) and applied an empty Windows jump list, which FrameworkUI's
        /// RecentFiles doesn't keep on any platform. Neither is needed.
        /// </remarks>
        public App()
        {
            Dispatcher.UIThread.UnhandledException += Application_DispatcherUnhandledException;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// XGoodFit port: App.xaml's BestFit and USACE pictures are added here, as bitmaps. BestFit is the
        /// application's icon (XGoodFitIcon in Directory.Build.props, RMC-BestFit's until XGoodFit has
        /// its own), so the About box shows the application's own.
        /// </remarks>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            Resources["BestFit"] = LoadBitmap("avares://XGoodFit/Resources/BestFit_Icon.ico");
            Resources["USACE"] = LoadBitmap("avares://XGoodFit/Resources/USACE.png");
        }

        /// <inheritdoc/>
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                desktop.Startup += Application_Startup;
                // Before the main loop starts, so the files macOS starts the application with aren't missed.
                _fileOpenRequests = FileOpenRequests.For(this);
            }
            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Handles the application startup event. Initializes software version information, creates the project model,
        /// configures version compatibility handlers, creates the main window, and processes command line arguments
        /// to open project files if specified.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The startup event data, including command line arguments.</param>
        /// <remarks>
        /// This method performs the following initialization tasks:
        /// <list type="bullet">
        /// <item>Sets software version and extension information</item>
        /// <item>Creates and configures the project model instance</item>
        /// <item>Registers a handler for opening version 1.0 projects with appropriate warnings</item>
        /// <item>Creates a temporary dummy project for initialization</item>
        /// <item>Ensures the user settings folder exists</item>
        /// <item>Creates and displays the main window</item>
        /// <item>Processes command line arguments to open specified project files</item>
        /// </list>
        /// Command line arguments are handled to support opening projects from Windows Explorer or the Jump List.
        /// </remarks>
        private void Application_Startup(object sender, ControlledApplicationLifetimeStartupEventArgs e)
        {
            FrameworkUI.MainWindow mainWindow = CreateMainWindow();
            // XGoodFit port: the main window is the lifetime's, as WPF's first window was the application's.
            ((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime).MainWindow = mainWindow;

            // Wire up the Check-for-Updates service so the Tools menu entry becomes visible.
            // XGoodFit port: XGoodFit's own releases, through Velopack, in place of upstream's
            // GitHubUpdateService. A copy that wasn't installed from a package can't update itself, so
            // it has no service: the menu item stays hidden, and there's no check.
            var updateService = new VelopackUpdateService(CreateUpdateOptions());
            if (updateService.IsInstalled)
            {
                mainWindow.UpdateService = updateService;

                // Background auto-check 5 seconds after launch — silent unless an update is found.
                // Task is intentionally discarded; all failures are caught inside the method.
                _ = AutoCheckForUpdatesAsync(mainWindow, updateService, delayMs: 5000);
            }

            // If the file is opened from the command line, then open from file
            // XGoodFit port: FileOpenRequests reads the command line as upstream did here (one argument
            // from a file manager, or several joined with spaces), and adds the files macOS's Finder
            // opens, now and while the application runs. As upstream, a file opened from the command
            // line that another copy has open ends this copy.
            if (_fileOpenRequests != null)
            {
                _fileOpenRequests.FilesOpened += (_, request) =>
                {
                    string fullFileName = request.Paths.LastOrDefault(path => path.EndsWith(FrameworkUI.ShellPublicVariables.SoftwareExtension, StringComparison.OrdinalIgnoreCase));
                    if (fullFileName != null)
                        mainWindow.OpenRecentProject(fullFileName, request.Source == FileOpenSource.CommandLine);
                };
            }

            mainWindow.Show();
        }

        /// <summary>
        /// Initializes the theme system and the software information, creates the project model and the
        /// main window for it: the body of upstream's Application_Startup before its update service.
        /// </summary>
        /// <returns>The main window, not yet shown.</returns>
        /// <remarks>XGoodFit port: split from <see cref="Application_Startup"/>, for the headless check.</remarks>
        internal static FrameworkUI.MainWindow CreateMainWindow()
        {
            // Initialize theme system - must be done before creating any UI
            FrameworkUI.ThemeManager.SetTheme(FrameworkUI.ThemeColor.Light);

            FrameworkUI.ShellPublicVariables.SoftwareVersionDate = ProductVersionDate;
            FrameworkUI.ShellPublicVariables.SoftwareVersion = ProductVersion;
            FrameworkUI.ShellPublicVariables.SoftwareExtension = ".bestfit";

            // Create the project model
            var project = RMC.BestFit.UI.BestFitProject.GetInstance();
            // Check if opening from version 1.0.
            project.OpenedVersion1 += (bool isVersion1, ref bool cancel) =>
            {
                if (isVersion1 == true)
                {
                    if (GenericControls.MessageBox.Show("You are opening a project created with version 1.0 of RMC-BestFit. All analyses must be rerun with version 2.0. Note that any edits made to this project can prevent it from being editable in version 1.0.", "Warning!", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                    {
                        cancel = true;
                    }
                }
            };

            // Create temp dummy project
            project.CreateNewDummyProject();

            // Create settings folder if it doesn't exist (used by event tree templates and styles).
            if (Directory.Exists(FrameworkUI.ShellPublicVariables.UserSettingsFolderPath) == false)
                Directory.CreateDirectory(FrameworkUI.ShellPublicVariables.UserSettingsFolderPath);

            // Create and Show the Main Window
            // XGoodFit port: the tree item style is a control theme, the node's Theme.
            FrameworkUI.MainWindow mainWindow = new FrameworkUI.MainWindow();
            mainWindow.ProjectNode = new MainProjectNode(project) { Theme = (ControlTheme)Current!.FindResource("TreeViewItemStyle") };

            // Sync theme warning suppression from persisted settings to OxyPlotThemeManager
            OxyPlotControls.OxyPlotThemeManager.SuppressThemeChangeWarning = FrameworkUI.UserSettings.SuppressThemeChangeWarning;

            // Sync back before UserSettings is saved to disk
            FrameworkUI.UserSettings.Saving += () =>
                FrameworkUI.UserSettings.SuppressThemeChangeWarning = OxyPlotControls.OxyPlotThemeManager.SuppressThemeChangeWarning;

            return mainWindow;
        }

        /// <summary>
        /// Handles unhandled exceptions that occur on the dispatcher thread. Provides special handling for
        /// COM exceptions and creates backup files for unexpected errors when auto-backup is enabled.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DispatcherUnhandledExceptionEventArgs"/> instance containing the exception data.</param>
        /// <remarks>
        /// This method handles two specific scenarios:
        /// <list type="bullet">
        /// <item>COM Exception with error code -2147221040 (CLIPBRD_E_CANT_OPEN): The exception is marked as handled and suppressed.</item>
        /// <item>All other unhandled exceptions: If backup retention is enabled, a backup project file is automatically created before the application terminates.</item>
        /// </list>
        /// This provides a safety mechanism to preserve user work in the event of unexpected application crashes.
        /// </remarks>
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Always log the unhandled exception so bug-report submitters can capture the
            // detail from a crash dump or attached debugger. Without this, the user only
            // sees the auto-backup behavior with no exception text recorded anywhere.
            System.Diagnostics.Debug.WriteLine($"Application.DispatcherUnhandledException: {e.Exception}");

            var comException = e.Exception as COMException;
            if (comException != null && comException.ErrorCode == -2147221040)
                e.Handled = true;
            else if (FrameworkUI.UserSettings.KeepLastBackupVersion == true)
                FrameworkUI.AutoBackup.CreateBackupProjectFile();
        }

        /// <summary>
        /// Creates the application update configuration for the public RMC-BestFit release repository.
        /// </summary>
        /// <returns>The checksum-enforcing update options for the running application version.</returns>
        /// <remarks>
        /// Keeping release-sensitive updater values in one factory makes their consistency directly testable.
        /// XGoodFit port: XGoodFit's own releases (XGoodFitUpdateRepository in Directory.Build.props), with
        /// the version read as upstream's ResolveCurrentVersion read it, or the installed package's
        /// (<see cref="VelopackUpdateService.CreateOptions"/>). Velopack finds the packages, so there's no
        /// asset name pattern, backup or executable name to give.
        /// </remarks>
        internal static UpdateOptions CreateUpdateOptions() => VelopackUpdateService.CreateOptions();

        /// <summary>
        /// Checks for application updates in the background after a startup delay. Prompts the
        /// user with Yes/No if a newer release is available and has not been previously skipped.
        /// </summary>
        /// <param name="mainWindow">The main application window (used as dialog owner and dispatcher host).</param>
        /// <param name="updateService">The configured update service.</param>
        /// <param name="delayMs">Milliseconds to wait after startup before running the check.</param>
        /// <remarks>
        /// Yes triggers the existing Tools-menu download/install flow by raising a click on the
        /// <c>CheckForUpdatesMenuItem</c> looked up through the window's name scope. No persists the
        /// version to the skipped-versions file so the user is not re-prompted on subsequent launches
        /// for the same version. All failures (network outage, rate limit, missing repo, parse error)
        /// are swallowed to a Debug line so launch is never disrupted.
        /// </remarks>
        private async Task AutoCheckForUpdatesAsync(FrameworkUI.MainWindow mainWindow,
                                                     IUpdateService updateService, int delayMs)
        {
            try
            {
                await Task.Delay(delayMs);
                var result = await updateService.CheckForUpdateAsync();
                if (!result.IsUpdateAvailable || result.Update == null) return;
                if (updateService.IsVersionSkipped(result.Update.Version)) return;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var newVer = result.Update.Version;
                    var msg = $"A new version ({newVer}) is available.\n\n" +
                              $"You are currently running version {updateService.Options.CurrentVersion}.\n\n" +
                              "Would you like to download and install the update now?\n" +
                              "(Select No to keep your current version — you won't be prompted about this version again.)";
                    var choice = GenericControls.MessageBox.Show(mainWindow, msg, "Update Available",
                        MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (choice == MessageBoxResult.Yes)
                    {
                        // Re-use the MainWindow's built-in Click handler (handles progress UI,
                        // download, checksum, backup, install + restart). CheckForUpdatesMenuItem
                        // is an internal field in FrameworkUI, so look it up via the name scope.
                        if (mainWindow.FindControl<MenuItem>("CheckForUpdatesMenuItem") is MenuItem mi)
                            mi.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    }
                    else
                    {
                        updateService.SkipVersion(newVer);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-update check failed: {ex.Message}");
            }
        }

        /// <summary>Loads a bitmap from the application's resources.</summary>
        /// <param name="uri">The resource's avares URI.</param>
        /// <returns>The bitmap.</returns>
        private static Bitmap LoadBitmap(string uri)
        {
            using Stream stream = AssetLoader.Open(new Uri(uri));
            return new Bitmap(stream);
        }

    }
}
