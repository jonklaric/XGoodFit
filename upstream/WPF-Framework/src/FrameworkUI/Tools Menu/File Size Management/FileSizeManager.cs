using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using FrameworkInterfaces;

namespace FrameworkUI
{
    /// <summary>
    /// A utility class for compacting and optimizing the project file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class FileSizeManager
    {
        #region Constants

        /// <summary>
        /// Bytes in one terabyte (1024^4).
        /// </summary>
        private const ulong BytesPerTerabyte = 1099511627776UL;

        /// <summary>
        /// Bytes in one gigabyte (1024^3).
        /// </summary>
        private const ulong BytesPerGigabyte = 1073741824UL;

        /// <summary>
        /// Bytes in one megabyte (1024^2).
        /// </summary>
        private const ulong BytesPerMegabyte = 1048576UL;

        /// <summary>
        /// Bytes in one kilobyte (1024).
        /// </summary>
        private const ulong BytesPerKilobyte = 1024UL;

        #endregion

        #region Fields

        private static IProject? _project;
        private static string? _fileSizeBefore;
        private static string? _fileSizeAfter;
        private static DispatcherTimer? _timer;
        private static FrameworkUI.CompactProgressControl? _progressControl;
        private static BackgroundWorker? _backgroundWorker;
        private static int _operationActive;
        private static bool _workerStarted;
        private static bool _workerCompleted;
        private static ExceptionDispatchInfo? _workerError;
        private static DispatcherFrame? _recoveryFrame;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the file size management operation reports progress or completion status.
        /// </summary>
        public static event ReportProgressEventHandler? ReportProgress;

        /// <summary>
        /// Delegate for the <see cref="ReportProgress"/> event.
        /// </summary>
        /// <param name="message">A message describing the progress or status.</param>
        public delegate void ReportProgressEventHandler(string message);

        #endregion

        #region Public Methods

        /// <summary>
        /// Compacts and optimizes the project file, returning only after the worker has completed.
        /// </summary>
        /// <param name="project">The project to compact and optimize.</param>
        /// <exception cref="ArgumentNullException">The project is null.</exception>
        /// <exception cref="InvalidOperationException">Another compaction is active, or its completion cannot be observed safely.</exception>
        /// <remarks>Worker errors are rethrown on the caller after owned resources have been released.</remarks>
        public static void CompactAndOptimizeFile(IProject project)
        {
            if (Interlocked.CompareExchange(ref _operationActive, 1, 0) != 0)
                throw new InvalidOperationException("Project compaction is already in progress.");

            ExceptionDispatchInfo? failure = null;
            string? message = null;
            try
            {
                _project = project ?? throw new ArgumentNullException(nameof(project));
                ShellPublicVariables.CompactionInProgress = true;
                _workerStarted = false;
                _workerCompleted = false;
                _workerError = null;
                _fileSizeBefore = !string.IsNullOrEmpty(project.FullFileName) && File.Exists(project.FullFileName)
                    ? GetFileSizeText(project.FullFileName) : string.Empty;

                _progressControl = new CompactProgressControl();
                _progressControl.ProgressText.Text = "Analyzing Project File...";
                _progressControl.ProgressBar.IsIndeterminate = true;
                _progressControl.ProgressBar.Minimum = 0d;
                _progressControl.ProgressBar.Maximum = 100d;
                _progressControl.ProgressBar.Value = 0d;
                _progressControl.Closing += ProgressControl_Closing;
                _progressControl.Loaded += ProgressControl_Loaded;

                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.1d) };
                _timer.Tick += Timer_Tick;
                _backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true };
                _backgroundWorker.DoWork += BackgroundWorker_Dowork;
                _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
                _backgroundWorker.RunWorkerCompleted += BackgroundWorker_WorkerComplete;

                try
                {
                    _progressControl.ShowDialog();
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }

                // Hide(), dispatcher shutdown, or a dialog exception must not release a live writer.
                if (_workerStarted && !_workerCompleted)
                    WaitForWorkerCompletion();

                if (failure == null && _workerError == null)
                {
                    if (!_workerCompleted)
                        throw new InvalidOperationException("Project compaction did not complete.");
                    _fileSizeAfter = GetFileSizeText(project.FullFileName);
                    message = _fileSizeBefore == _fileSizeAfter
                        ? "The file was optimized. However, there was no unused space to compact in '" + project.Name + "', so the file size remains unchanged at " + _fileSizeAfter + "."
                        : "The file was compacted and optimized. The project '" + project.Name + "' was compacted from " + _fileSizeBefore + " to " + _fileSizeAfter + ".";
                }
            }
            catch (Exception ex)
            {
                PreserveFailure(ref failure, ex, "operation");
            }
            finally
            {
                if (_workerError != null) PreserveFailure(ref failure, _workerError.SourceException, "worker");
                // If recovery itself aborts, retain ownership and the public guard. Callers must
                // not save or close a project while its completion remains unobserved.
                if (!_workerStarted || _workerCompleted)
                {
                    try { CleanupOperation(); }
                    catch (Exception ex)
                    {
                        PreserveFailure(ref failure, ex, "cleanup");
                    }
                }
            }

            failure?.Throw();
            if (message != null) ReportProgress?.Invoke(message);
        }

        /// <summary>Starts the prepared worker once the modal progress window is loaded.</summary>
        /// <param name="sender">The progress window.</param>
        /// <param name="e">The loaded event.</param>
        private static void ProgressControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement progressControl) progressControl.Loaded -= ProgressControl_Loaded;
            if (_workerStarted || _workerCompleted || _backgroundWorker == null) return;
            try
            {
                _timer?.Start();
                _workerStarted = true;
                _backgroundWorker.RunWorkerAsync(_project);
            }
            catch (Exception ex)
            {
                _workerStarted = false;
                _workerError = ExceptionDispatchInfo.Capture(ex);
                _progressControl?.Close();
            }
        }

        /// <summary>Preserves modal protection when the progress dialog returns before its worker.</summary>
        /// <exception cref="InvalidOperationException">The dispatcher stops before completion is observed.</exception>
        private static void WaitForWorkerCompletion()
        {
            var dispatcher = _progressControl!.Dispatcher;
            var disabledWindows = new List<Window>();
            try
            {
                if (Application.Current != null)
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.Dispatcher == dispatcher && window.IsEnabled)
                        {
                            disabledWindows.Add(window);
                            window.SetCurrentValue(UIElement.IsEnabledProperty, false);
                        }
                    }
                }
                _recoveryFrame = new DispatcherFrame();
                if (!dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished && !_workerCompleted)
                    Dispatcher.PushFrame(_recoveryFrame);
                if (!_workerCompleted)
                    throw new InvalidOperationException("Project compaction is still running; the project cannot be saved or closed.");
            }
            finally
            {
                _recoveryFrame = null;
                if (_workerCompleted)
                {
                    ExceptionDispatchInfo? restoreFailure = null;
                    foreach (Window window in disabledWindows)
                        AttemptCleanup(() => window.SetCurrentValue(UIElement.IsEnabledProperty, true), ref restoreFailure);
                    restoreFailure?.Throw();
                }
            }
        }

        /// <summary>Releases the completed operation's handlers, timer, worker, window, and guard.</summary>
        /// <remarks>Every cleanup step is attempted; the first cleanup error is propagated afterward.</remarks>
        private static void CleanupOperation()
        {
            ExceptionDispatchInfo? failure = null;
            try
            {
                var timer = _timer;
                if (timer != null)
                {
                    AttemptCleanup(timer.Stop, ref failure);
                    AttemptCleanup(() => timer.Tick -= Timer_Tick, ref failure);
                }
                var worker = _backgroundWorker;
                if (worker != null)
                {
                    AttemptCleanup(() => worker.DoWork -= BackgroundWorker_Dowork, ref failure);
                    AttemptCleanup(() => worker.ProgressChanged -= BackgroundWorker_ProgressChanged, ref failure);
                    AttemptCleanup(() => worker.RunWorkerCompleted -= BackgroundWorker_WorkerComplete, ref failure);
                    AttemptCleanup(worker.Dispose, ref failure);
                }
                var progressControl = _progressControl;
                if (progressControl != null)
                {
                    AttemptCleanup(() => progressControl.Loaded -= ProgressControl_Loaded, ref failure);
                    AttemptCleanup(() => progressControl.Closing -= ProgressControl_Closing, ref failure);
                    AttemptCleanup(progressControl.Close, ref failure);
                }
            }
            finally
            {
                _timer = null;
                _backgroundWorker = null;
                _progressControl = null;
                _project = null;
                _fileSizeBefore = null;
                _fileSizeAfter = null;
                _workerError = null;
                _workerStarted = false;
                _workerCompleted = false;
                ShellPublicVariables.CompactionInProgress = false;
                Interlocked.Exchange(ref _operationActive, 0);
            }
            failure?.Throw();
        }

        /// <summary>Attempts one cleanup action without preventing subsequent cleanup.</summary>
        /// <param name="cleanup">The resource cleanup or window restoration action.</param>
        /// <param name="failure">The first failure, if one has already occurred.</param>
        private static void AttemptCleanup(Action cleanup, ref ExceptionDispatchInfo? failure)
        {
            try { cleanup(); }
            catch (Exception ex) { PreserveFailure(ref failure, ex, "cleanup"); }
        }

        /// <summary>Preserves the first failure and records secondary diagnostic failures.</summary>
        /// <param name="failure">The first failure delivered to the caller.</param>
        /// <param name="error">The newly observed failure.</param>
        /// <param name="operation">The operation producing this diagnostic.</param>
        private static void PreserveFailure(ref ExceptionDispatchInfo? failure, Exception error, string operation)
        {
            if (failure == null) failure = ExceptionDispatchInfo.Capture(error);
            else if (!ReferenceEquals(failure.SourceException, error))
                Debug.WriteLine($"FileSizeManager secondary {operation} failure: {error}");
        }
        /// <summary>
        /// Background worker for performing file compaction.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private static void BackgroundWorker_Dowork(object? sender, DoWorkEventArgs e)
        {
            if (sender is not BackgroundWorker worker || e.Argument is not IProject project) return;
            // Update progress bar to compacting
            worker.ReportProgress(0);
            project.Compact();
            // Update progress bar to optimizing
            worker.ReportProgress(100);
            project.Optimize();
        }

        /// <summary>
        /// Background worker progress changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private static void BackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (_progressControl == null) return;
            // This gives the perception that the optimization is taking some time and actually doing something meaningful.
            if (e.ProgressPercentage == 0)
            {
                _progressControl.ProgressText.Text = "Compacting...";
                _progressControl.ProgressBar.Value = 0d;
                _progressControl.ProgressBar.IsIndeterminate = false;
                _progressControl.UpdateLayout();
            }
            else if (e.ProgressPercentage == 100)
            {
                _progressControl.ProgressText.Text = "Optimizing...";
                _progressControl.ProgressBar.Value = 100d;
                _progressControl.ProgressBar.IsIndeterminate = true;
                _progressControl.UpdateLayout();
            }
        }

        /// <summary>
        /// Report status when the compression worker has completed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private static void BackgroundWorker_WorkerComplete(object? sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null) _workerError = ExceptionDispatchInfo.Capture(e.Error);
            else if (e.Cancelled) _workerError = ExceptionDispatchInfo.Capture(new OperationCanceledException("Project compaction was canceled."));
            _workerCompleted = true;
            if (_recoveryFrame != null) _recoveryFrame.Continue = false;
            try { _progressControl?.Close(); }
            catch (Exception ex)
            {
                _workerError ??= ExceptionDispatchInfo.Capture(ex);
                Debug.WriteLine($"FileSizeManager progress window close failed: {ex}");
                _progressControl?.Hide();
            }
        }

        /// <summary>Vetoes closing while a non-cancelable compaction worker remains active.</summary>
        /// <param name="sender">The progress window.</param>
        /// <param name="e">The closing event.</param>
        private static void ProgressControl_Closing(object? sender, CancelEventArgs e)
        {
            if (_workerStarted && !_workerCompleted) e.Cancel = true;
        }
        /// <summary>
        /// Timer used to update the progress bar.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private static void Timer_Tick(object? sender, EventArgs e)
        {
            if (_project == null || _progressControl == null) return;

            // Get the temp journal file name
            string tempFilename = _project.FullFileName + "-journal";

            try
            {
                // Look for temp journal file
                if (!File.Exists(tempFilename)) return;

                // Update text and progress bar
                long tempFileSize = GetFileSize(tempFilename);
                long originalFileSize = GetFileSize(_project.FullFileName);

                string tempFileSizeText = (tempFileSize < originalFileSize)
                    ? FormatBytes((ulong)tempFileSize)
                    : _fileSizeBefore ?? string.Empty;

                _progressControl.ProgressBar.IsIndeterminate = false;
                _progressControl.ProgressText.Text = "Compacting " + tempFileSizeText + " of " + (_fileSizeBefore ?? string.Empty);
                if (originalFileSize > 0)
                {
                    _progressControl.ProgressBar.Value = (1d - (originalFileSize - tempFileSize) / (double)originalFileSize) * _progressControl.ProgressBar.Maximum;
                }
            }
            catch (IOException)
            {
                // File may have been deleted or is in use between exists check and read - ignore
            }
            catch (UnauthorizedAccessException)
            {
                // File access denied - ignore
            }
        }

        /// <summary>
        /// Gets the file size of the project file. Returns a long.
        /// </summary>
        /// <param name="fileName">The full file name.</param>
        /// <returns>The file size in bytes.</returns>
        public static long GetFileSize(string fileName)
        {
            return new FileInfo(fileName).Length; 
        }

        /// <summary>
        /// Gets the file size of the file in a standard text format; e.g., 12.14 MB.
        /// </summary>
        /// <param name="fileName">The full file name.</param>
        /// <returns>A formatted string representing the file size.</returns>
        public static string GetFileSizeText(string fileName)
        {
            return FormatBytes((ulong)GetFileSize(fileName));
        }

        /// <summary>
        /// Formats bytes as a human-readable string with appropriate unit (TB, GB, MB, KB, or bytes).
        /// </summary>
        /// <param name="bytes">File size in bytes.</param>
        /// <returns>A formatted string representing the file size (e.g., "1.50 GB").</returns>
        public static string FormatBytes(ulong bytes)
        {
            try
            {
                double value;
                string unit;

                if (bytes >= BytesPerTerabyte)
                {
                    value = bytes / (double)BytesPerTerabyte;
                    unit = "TB";
                }
                else if (bytes >= BytesPerGigabyte)
                {
                    value = bytes / (double)BytesPerGigabyte;
                    unit = "GB";
                }
                else if (bytes >= BytesPerMegabyte)
                {
                    value = bytes / (double)BytesPerMegabyte;
                    unit = "MB";
                }
                else if (bytes >= BytesPerKilobyte)
                {
                    value = bytes / (double)BytesPerKilobyte;
                    unit = "KB";
                }
                else
                {
                    value = bytes;
                    unit = "bytes";
                }

                return value.ToString("N2", CultureInfo.CurrentCulture) + " " + unit;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FileSizeManager.FormatBytes failed for value {bytes}: {ex.Message}");
                return "0 bytes";
            }
        }

        #endregion
    }
}
