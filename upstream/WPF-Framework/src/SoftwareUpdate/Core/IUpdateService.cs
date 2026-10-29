using System;
using System.Threading;
using System.Threading.Tasks;

namespace SoftwareUpdate
{
    /// <summary>
    /// Defines the contract for a software update service that can check for,
    /// download, and install application updates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations of this interface provide the core functionality for
    /// software updates, including checking remote sources for new versions,
    /// downloading update packages, and coordinating the installation process.
    /// </para>
    /// <para>
    /// The typical update flow is:
    /// <list type="number">
    ///     <item>Call <see cref="CheckForUpdateAsync"/> to check for available updates</item>
    ///     <item>If update available, call <see cref="DownloadUpdateAsync"/> to download</item>
    ///     <item>Call <see cref="InstallUpdateAndRestart"/> to apply the update</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IUpdateService
    {
        /// <summary>
        /// Gets the update options/configuration for this service.
        /// </summary>
        UpdateOptions Options { get; }

        /// <summary>
        /// Gets the current state of the update service.
        /// </summary>
        UpdateState State { get; }

        /// <summary>
        /// Gets the most recently checked update information, if any.
        /// </summary>
        UpdateInfo? AvailableUpdate { get; }

        /// <summary>
        /// Checks for available updates asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result
        /// contains the <see cref="UpdateCheckResult"/> with update information.
        /// </returns>
        Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads the specified update to a temporary location.
        /// </summary>
        /// <param name="update">The update information to download.</param>
        /// <param name="progress">Optional progress reporter for download progress.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result
        /// contains the <see cref="UpdateDownloadResult"/> with the download path.
        /// </returns>
        Task<UpdateDownloadResult> DownloadUpdateAsync(
            UpdateInfo update,
            IProgress<UpdateDownloadProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Installs the downloaded update and restarts the application.
        /// </summary>
        /// <param name="downloadedFilePath">Path to the downloaded update package.</param>
        /// <remarks>
        /// <para>
        /// This method will launch an external updater process and then exit
        /// the current application. The updater will:
        /// <list type="number">
        ///     <item>Wait for this application to fully exit</item>
        ///     <item>Create a backup of the current installation</item>
        ///     <item>Extract the update package</item>
        ///     <item>Restart the application</item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Lifecycle contract — this method does not return.</b> The reference
        /// implementation <c>GitHubUpdateService.InstallUpdateAndRestart</c> calls
        /// <see cref="System.Environment.Exit(int)"/> after spawning the updater, so
        /// <i>no code in the calling application runs after this method returns</i>:
        /// <c>finally</c> blocks, <c>Application.Exit</c> handlers, <c>OnClosing</c>
        /// overrides, and IDisposable.Dispose calls are all skipped.
        /// </para>
        /// <para>
        /// <b>Callers MUST flush user-visible state before invoking</b> — including but
        /// not limited to:
        /// <list type="bullet">
        ///     <item>Persisting <c>UserSettings</c> / <c>RecentFiles</c> via their <c>Save</c> methods</item>
        ///     <item>Prompting to save dirty projects (the framework's normal close-flow handler is a good model)</item>
        ///     <item>Closing open file/database handles, stopping background workers, and flushing logs</item>
        /// </list>
        /// </para>
        /// <para>
        /// See the FrameworkUI shell's <c>MainWindow.DownloadAndInstallUpdateAsync</c>
        /// for an example consumer that runs the dirty-project save flow before
        /// invoking this method.
        /// </para>
        /// </remarks>
        void InstallUpdateAndRestart(string downloadedFilePath);

        /// <summary>
        /// Marks a specific version as skipped so the user won't be prompted again.
        /// </summary>
        /// <param name="version">The version to skip.</param>
        void SkipVersion(SemanticVersion? version);

        /// <summary>
        /// Checks if a specific version has been marked as skipped.
        /// </summary>
        /// <param name="version">The version to check.</param>
        /// <returns><c>true</c> if the version is skipped; otherwise, <c>false</c>.</returns>
        bool IsVersionSkipped(SemanticVersion? version);

        /// <summary>
        /// Clears all skipped versions.
        /// </summary>
        void ClearSkippedVersions();

        /// <summary>
        /// Occurs when an update check completes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Threading:</b> handlers may be invoked on a thread-pool thread, not on
        /// the UI thread. This event is raised from <see cref="CheckForUpdateAsync"/>
        /// after the asynchronous network I/O completes, so the continuation can run
        /// on any <see cref="System.Threading.SynchronizationContext"/>.
        /// WPF consumers that touch UI elements from the handler MUST marshal back to
        /// the UI thread via <c>Application.Current.Dispatcher.Invoke</c> (or
        /// <c>BeginInvoke</c>) — accessing dependency properties off the dispatcher
        /// thread will throw <see cref="System.InvalidOperationException"/>.
        /// </para>
        /// </remarks>
        event EventHandler<UpdateCheckResult>? UpdateCheckCompleted;

        /// <summary>
        /// Occurs when an error occurs during update operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Threading:</b> handlers may be invoked on a thread-pool thread, not on
        /// the UI thread. This event is raised from any of the asynchronous update
        /// methods (<see cref="CheckForUpdateAsync"/>, <see cref="DownloadUpdateAsync"/>)
        /// when an unrecoverable error occurs, so the continuation can run on any
        /// <see cref="System.Threading.SynchronizationContext"/>.
        /// WPF consumers that touch UI elements (e.g., showing a <c>MessageBox</c>) MUST
        /// marshal back to the UI thread via <c>Application.Current.Dispatcher.Invoke</c>
        /// or <c>BeginInvoke</c>.
        /// </para>
        /// </remarks>
        event EventHandler<Exception>? UpdateError;
    }
}
