using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SoftwareUpdate.Utilities;

namespace SoftwareUpdate.GitHub
{
    /// <summary>
    /// Implementation of <see cref="IUpdateService"/> that checks for updates on GitHub Releases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class GitHubUpdateService : IUpdateService, IDisposable
    {
        /// <summary>
        /// Maximum number of retry attempts for transient network failures.
        /// </summary>
        private const int MaxRetryAttempts = 3;

        /// <summary>
        /// Base delay in milliseconds between retry attempts.
        /// </summary>
        private const int RetryDelayMs = 500;

        /// <summary>
        /// Maximum allowed download size in bytes (500 MB) to prevent denial-of-service
        /// from a compromised server sending an infinite response body.
        /// </summary>
        private const long MaxDownloadSizeBytes = 500L * 1024 * 1024;

        /// <summary>
        /// Age after which disposable updater runner directories may be removed.
        /// </summary>
        private static readonly TimeSpan StaleRunnerAge = TimeSpan.FromDays(1);

        private static readonly string[] UpdaterPayloadSuffixes =
        {
            ".exe",
            ".dll",
            ".deps.json",
            ".runtimeconfig.json"
        };

        private readonly HttpClient _httpClient;
        private readonly object _lock = new object();
        private UpdateState _state = UpdateState.Idle;
        private UpdateInfo? _availableUpdate;
        private HashSet<string> _skippedVersions = null!; // Assigned by LoadSkippedVersions() in constructor
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubUpdateService"/> class.
        /// </summary>
        /// <param name="options">The update options.</param>
        public GitHubUpdateService(UpdateOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Options.Validate();

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(options.GitHubRepo ?? "", options.CurrentVersion?.ToString() ?? "1.0.0"));
            _httpClient.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);

            if (!string.IsNullOrEmpty(options.GitHubToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.GitHubToken);
            }

            LoadSkippedVersions();
            CleanupStaleUpdaterRunners();
        }

        /// <inheritdoc/>
        public UpdateOptions Options { get; }

        /// <inheritdoc/>
        public UpdateState State
        {
            get { lock (_lock) return _state; }
            private set { lock (_lock) _state = value; }
        }

        /// <inheritdoc/>
        public UpdateInfo? AvailableUpdate
        {
            get { lock (_lock) return _availableUpdate; }
            private set { lock (_lock) _availableUpdate = value; }
        }

        /// <inheritdoc/>
        public event EventHandler<UpdateCheckResult>? UpdateCheckCompleted;

        /// <inheritdoc/>
        public event EventHandler<Exception>? UpdateError;

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if the current <see cref="State"/> is
        /// not in <paramref name="allowed"/>. Public methods call this at entry to enforce the
        /// state machine: <see cref="CheckForUpdateAsync"/> rejects re-entry while a previous
        /// install is in flight; <see cref="DownloadUpdateAsync"/> requires an UpdateAvailable
        /// outcome from a prior check; <see cref="InstallUpdateAndRestart"/> requires a
        /// completed download (ReadyToInstall).
        /// </summary>
        /// <param name="caller">The calling method's name (used in the exception message).</param>
        /// <param name="allowed">The set of states that permit the call to proceed.</param>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="State"/> is not allowed.</exception>
        private void EnsureState(string caller, params UpdateState[] allowed)
        {
            UpdateState current;
            lock (_lock) { current = _state; }
            if (Array.IndexOf(allowed, current) < 0)
            {
                throw new InvalidOperationException(
                    $"{caller} cannot run while the update service is in the {current} state. " +
                    $"Expected one of: {string.Join(", ", allowed)}.");
            }
        }

        /// <inheritdoc/>
        public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            // A check may run from any state EXCEPT Installing (the updater process is mid-handoff
            // and the in-process service should not race it).
            EnsureState(nameof(CheckForUpdateAsync),
                UpdateState.Idle,
                UpdateState.Checking,
                UpdateState.UpdateAvailable,
                UpdateState.UpToDate,
                UpdateState.Downloading,
                UpdateState.ReadyToInstall,
                UpdateState.Error);

            State = UpdateState.Checking;

            try
            {
                var apiUrl = $"https://api.github.com/repos/{Options.GitHubOwner}/{Options.GitHubRepo}/releases";
                var response = await GetWithRetryAsync(apiUrl, cancellationToken).ConfigureAwait(false);

                // 403 can mean rate-limited OR forbidden (bad token, private repo). Distinguish
                // by the X-RateLimit-Remaining header: 0 means we're rate limited; absent or > 0
                // means authorization failure.
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    var isRateLimited = false;
                    if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var rateHeader))
                    {
                        foreach (var value in rateHeader)
                        {
                            if (int.TryParse(value, out var remaining) && remaining <= 0)
                            {
                                isRateLimited = true;
                                break;
                            }
                        }
                    }

                    string message;
                    if (isRateLimited)
                    {
                        message = "GitHub API rate limit exceeded. ";
                        message += string.IsNullOrEmpty(Options.GitHubToken)
                            ? "Consider providing a GitHubToken to increase the rate limit."
                            : "Please wait before making additional requests.";
                    }
                    else
                    {
                        message =
                            "GitHub API returned 403 Forbidden. " +
                            "The provided token may be invalid, lack required scopes, or the repository may be private. " +
                            $"Repository: {Options.GitHubOwner}/{Options.GitHubRepo}";
                    }
                    throw new HttpRequestException(message);
                }

                response.EnsureSuccessStatusCode();

                var jsonStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var releases = await DeserializeReleasesAsync(jsonStream, cancellationToken).ConfigureAwait(false);

                // Filter releases
                var validReleases = releases
                    .Where(r => !r.Draft)
                    .Where(r => Options.IncludePreReleases || !r.PreRelease)
                    .Where(r => r.TagName != null && SemanticVersion.TryParse(r.TagName, out _))
                    .ToList();

                if (!validReleases.Any())
                {
                    State = UpdateState.UpToDate;
                    var result = UpdateCheckResult.NoUpdateAvailable(Options.CurrentVersion!);
                    RaiseEvent(UpdateCheckCompleted, result);
                    return result;
                }

                // Find matching asset in latest release
                foreach (var release in validReleases.OrderByDescending(r => SemanticVersion.Parse(r.TagName!)))
                {
                    var asset = FindMatchingAsset(release);
                    if (asset == null) continue;

                    var releaseVersion = SemanticVersion.Parse(release.TagName!);

                    if (releaseVersion > Options.CurrentVersion)
                    {
                        var updateInfo = new UpdateInfo
                        {
                            Version = releaseVersion,
                            Name = release.Name ?? release.TagName,
                            DownloadUrl = asset.BrowserDownloadUrl,
                            DownloadSize = asset.Size,
                            AssetName = asset.Name,
                            ReleaseNotes = release.Body,
                            PublishedAt = release.GetPublishedDateTime(),
                            IsPreRelease = release.PreRelease,
                            ReleasePageUrl = release.HtmlUrl,
                            Sha256Checksum = ExtractSha256Checksum(release.Body)
                        };

                        // Atomically update AvailableUpdate, IsSkipped, and State in a single lock
                        // to prevent another thread from observing an inconsistent state (e.g.,
                        // AvailableUpdate set but State still Checking).
                        bool isSkipped;
                        lock (_lock)
                        {
                            _availableUpdate = updateInfo;
                            // Use the metadata-stripped key so two builds of "1.2.3" with different
                            // build metadata still match the same skip entry.
                            isSkipped = _skippedVersions.Contains(GetSkipKey(releaseVersion));
                            _state = UpdateState.UpdateAvailable;
                        }

                        var result = UpdateCheckResult.UpdateAvailable(Options.CurrentVersion!, updateInfo, isSkipped);
                        RaiseEvent(UpdateCheckCompleted, result);
                        return result;
                    }
                }

                State = UpdateState.UpToDate;
                var noUpdateResult = UpdateCheckResult.NoUpdateAvailable(Options.CurrentVersion!);
                RaiseEvent(UpdateCheckCompleted, noUpdateResult);
                return noUpdateResult;
            }
            catch (HttpRequestException ex)
            {
                State = UpdateState.Error;
                RaiseEvent(UpdateError, ex);
                var result = UpdateCheckResult.Failed(Options.CurrentVersion!, ex);
                RaiseEvent(UpdateCheckCompleted, result);
                return result;
            }
            catch (JsonException ex)
            {
                State = UpdateState.Error;
                var wrappedException = new InvalidOperationException("Failed to parse GitHub API response.", ex);
                RaiseEvent(UpdateError, wrappedException);
                var result = UpdateCheckResult.Failed(Options.CurrentVersion!, wrappedException);
                RaiseEvent(UpdateCheckCompleted, result);
                return result;
            }
            catch (OperationCanceledException)
            {
                State = UpdateState.Idle;
                throw;
            }
            catch (Exception ex)
            {
                State = UpdateState.Error;
                RaiseEvent(UpdateError, ex);
                var result = UpdateCheckResult.Failed(Options.CurrentVersion!, ex);
                RaiseEvent(UpdateCheckCompleted, result);
                return result;
            }
        }

        /// <inheritdoc/>
        public async Task<UpdateDownloadResult> DownloadUpdateAsync(
            UpdateInfo update,
            IProgress<UpdateDownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // Argument validation runs before the state check so that misuse (a null update)
            // surfaces as an ArgumentNullException regardless of state — callers shouldn't have
            // to drive the state machine just to get the right exception type for a bad arg.
            if (update == null)
                throw new ArgumentNullException(nameof(update));
            EnsureState(nameof(DownloadUpdateAsync), UpdateState.UpdateAvailable);

            State = UpdateState.Downloading;

            string? tempDir = null;
            bool downloadSucceeded = false;

            try
            {
                ValidateChecksumMetadata(update, Options.RequireSha256Checksum);

                // Security: Enforce HTTPS on download URL to prevent MITM attacks
                if (!string.IsNullOrEmpty(update.DownloadUrl))
                {
                    var downloadUri = new Uri(update.DownloadUrl);
                    if (!downloadUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new SecurityException(
                            $"Download URL must use HTTPS. Received: {downloadUri.Scheme}");
                    }
                }

                // Create temp directory with GUID to prevent predictable path attacks
                tempDir = Path.Combine(Path.GetTempPath(), "SoftwareUpdate", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                // Validate asset name to prevent path traversal attacks
                var safeAssetName = Path.GetFileName(update.AssetName);
                if (string.IsNullOrEmpty(safeAssetName) ||
                    safeAssetName.Contains("..") ||
                    safeAssetName != update.AssetName)
                {
                    throw new ArgumentException($"Invalid asset name: {update.AssetName}", nameof(update));
                }

                var tempFilePath = Path.Combine(tempDir, safeAssetName);

                // FileMode.Create overwrites any existing file atomically — no need
                // for a separate File.Exists/File.Delete check (avoids TOCTOU race).

                using (var response = await _httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? update.DownloadSize;
                    var downloadProgress = new UpdateDownloadProgress { TotalBytes = totalBytes };

                    // FileAccess.ReadWrite (not Write) so the stream can be seeked back to
                    // position 0 and re-read for SHA256 validation below.
                    using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int bytesRead;
                        var stopwatch = Stopwatch.StartNew();
                        long lastReportedBytes = 0;
                        var lastReportTime = stopwatch.Elapsed;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            // Security: Enforce maximum download size BEFORE writing the chunk to disk.
                            // Checking after WriteAsync meant the over-limit chunk was already on disk
                            // before the throw, partially defeating the DoS guard.
                            if (totalRead + bytesRead > MaxDownloadSizeBytes)
                            {
                                throw new InvalidOperationException(
                                    $"Download exceeded maximum allowed size of {MaxDownloadSizeBytes / 1024 / 1024} MB.");
                            }

                            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                            totalRead += bytesRead;

                            // Report progress
                            if (progress != null)
                            {
                                var elapsed = stopwatch.Elapsed - lastReportTime;
                                if (elapsed.TotalMilliseconds >= 100) // Report every 100ms max
                                {
                                    var bytesInInterval = totalRead - lastReportedBytes;
                                    downloadProgress.BytesDownloaded = totalRead;
                                    downloadProgress.BytesPerSecond = bytesInInterval / elapsed.TotalSeconds;
                                    progress.Report(downloadProgress);

                                    lastReportedBytes = totalRead;
                                    lastReportTime = stopwatch.Elapsed;
                                }
                            }
                        }

                        // Final progress report
                        if (progress != null)
                        {
                            downloadProgress.BytesDownloaded = totalRead;
                            progress.Report(downloadProgress);
                        }

                        // Validate SHA256 checksum if provided
                        if (!string.IsNullOrEmpty(update.Sha256Checksum))
                        {
                            ValidateDownloadedChecksum(fileStream, update.Sha256Checksum);
                        }
                        else
                        {
                            Debug.WriteLine("[GitHubUpdateService] WARNING: No SHA256 checksum provided. Download integrity was not verified.");
                        }

                        State = UpdateState.ReadyToInstall;
                        downloadSucceeded = true;
                        return UpdateDownloadResult.Successful(tempFilePath, update, totalRead);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                State = UpdateState.Idle;
                return UpdateDownloadResult.Cancelled();
            }
            catch (Exception ex)
            {
                State = UpdateState.Error;
                RaiseEvent(UpdateError, ex);
                return UpdateDownloadResult.Failed(ex);
            }
            finally
            {
                // Clean up the temp directory if the download didn't complete successfully
                // (cancellation, checksum mismatch, MaxDownloadSize overrun, network failure, etc.).
                // Best-effort: swallow cleanup failures so the original error is what the caller
                // sees, not a secondary cleanup IOException.
                if (!downloadSucceeded && !string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, recursive: true); }
                    catch (Exception cleanupEx)
                    {
                        Debug.WriteLine($"[GitHubUpdateService] Failed to clean up temp dir '{tempDir}': {cleanupEx.Message}");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void InstallUpdateAndRestart(string downloadedFilePath)
        {
            if (string.IsNullOrEmpty(downloadedFilePath))
                throw new ArgumentNullException(nameof(downloadedFilePath));

            if (!File.Exists(downloadedFilePath))
                throw new FileNotFoundException("Downloaded update file not found.", downloadedFilePath);

            Options.Validate();

            EnsureState(nameof(InstallUpdateAndRestart), UpdateState.ReadyToInstall);

            State = UpdateState.Installing;

            var updaterPath = Options.ResolvedUpdaterPath;
            if (!File.Exists(updaterPath))
            {
                throw new FileNotFoundException(
                    $"Updater executable not found at '{updaterPath}'. " +
                    "Ensure SoftwareUpdate.Updater.exe is deployed with your application.",
                    updaterPath);
            }

            // Security: Validate the updater path resides inside the install directory
            var fullUpdaterPath = Path.GetFullPath(updaterPath);
            var fullInstallDir = Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(Options.ResolvedInstallDirectory));
            if ((File.GetAttributes(fullInstallDir) & FileAttributes.ReparsePoint) != 0 ||
                (File.GetAttributes(fullUpdaterPath) & FileAttributes.ReparsePoint) != 0)
            {
                throw new SecurityException("Updater launch paths cannot be reparse points.");
            }

            if (!fullUpdaterPath.StartsWith(fullInstallDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Path.GetDirectoryName(fullUpdaterPath), fullInstallDir, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException(
                    $"Updater executable must reside within the install directory. " +
                    $"Updater: '{fullUpdaterPath}', Install dir: '{fullInstallDir}'");
            }

            var currentPid = Process.GetCurrentProcess().Id;
            var targetDir = Options.ResolvedInstallDirectory;
            var mainExe = Options.ResolvedMainExecutableName;

            // Stage the zip into the install directory under a stable name BEFORE we exit. If
            // the download landed in %TEMP% (typical), AV scanners and Windows temp-cleanup can
            // delete it during the 60-second window between parent exit and updater extraction,
            // leaving the updater unable to find its payload. Copying into the install dir
            // (which is generally not subject to %TEMP% sweeps) closes that window. We use a
            // GUID-suffixed name so concurrent or repeated update attempts don't collide.
            var stagedZipPath = downloadedFilePath;
            try
            {
                var stagedDir = Path.Combine(targetDir, "updates_pending");
                if (Directory.Exists(stagedDir) &&
                    (File.GetAttributes(stagedDir) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new SecurityException("Update staging directory cannot be a reparse point.");
                }

                Directory.CreateDirectory(stagedDir);
                var stagedName = $"update_{Guid.NewGuid():N}{Path.GetExtension(downloadedFilePath)}";
                stagedZipPath = Path.Combine(stagedDir, stagedName);
                File.Copy(downloadedFilePath, stagedZipPath, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                // If staging fails (read-only install dir, permission issue), fall back to the
                // original temp path. The softer Validate failure in UpdaterArguments will then
                // surface a clearer message if the zip is later swept up.
                Debug.WriteLine($"[Update] Failed to stage zip into install dir, falling back to temp path: {ex.Message}");
                stagedZipPath = downloadedFilePath;
            }

            var runnerUpdaterPath = CreateUpdaterRunner(fullUpdaterPath);

            // Use ArgumentList for safe argument passing (no manual quoting/escaping)
            var startInfo = new ProcessStartInfo
            {
                FileName = runnerUpdaterPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("--pid");
            startInfo.ArgumentList.Add(currentPid.ToString());
            startInfo.ArgumentList.Add("--zip");
            startInfo.ArgumentList.Add(stagedZipPath);
            startInfo.ArgumentList.Add("--target");
            startInfo.ArgumentList.Add(targetDir);
            startInfo.ArgumentList.Add("--exe");
            startInfo.ArgumentList.Add(mainExe);
            if (Options.CreateBackup)
                startInfo.ArgumentList.Add("--backup");

            foreach (var preservedPath in Options.AdditionalPreservedRelativePaths)
            {
                startInfo.ArgumentList.Add("--preserve");
                startInfo.ArgumentList.Add(preservedPath);
            }

            Process? process;
            try
            {
                process = Process.Start(startInfo);
            }
            catch
            {
                DeleteUpdaterRunner(Path.GetDirectoryName(runnerUpdaterPath));
                throw;
            }

            if (process == null)
            {
                DeleteUpdaterRunner(Path.GetDirectoryName(runnerUpdaterPath));
                throw new InvalidOperationException("Failed to start the updater process.");
            }

            // Dispose resources before exiting the current application
            Dispose();
            Environment.Exit(0);
        }

        /// <summary>
        /// Copies the complete updater payload to a disposable directory outside the installation.
        /// </summary>
        /// <param name="updaterPath">The installed updater executable path.</param>
        /// <returns>The disposable updater executable path.</returns>
        internal static string CreateUpdaterRunner(string updaterPath)
        {
            var fullUpdaterPath = Path.GetFullPath(updaterPath);
            var sourceDirectory = Path.GetDirectoryName(fullUpdaterPath)
                ?? throw new InvalidOperationException("The updater executable has no parent directory.");
            if ((File.GetAttributes(sourceDirectory) & FileAttributes.ReparsePoint) != 0)
                throw new SecurityException("Updater payload directory cannot be a reparse point.");

            var payloadBaseName = Path.GetFileNameWithoutExtension(fullUpdaterPath);
            var payloadFiles = UpdaterPayloadSuffixes
                .Select(suffix => Path.Combine(sourceDirectory, payloadBaseName + suffix))
                .ToArray();

            foreach (var payloadFile in payloadFiles)
            {
                if (!File.Exists(payloadFile))
                {
                    throw new FileNotFoundException(
                        $"Updater payload file not found at '{payloadFile}'. Ensure the complete updater payload is deployed.",
                        payloadFile);
                }

                if ((File.GetAttributes(payloadFile) & FileAttributes.ReparsePoint) != 0)
                    throw new SecurityException($"Updater payload file cannot be a reparse point: '{payloadFile}'.");
            }

            var runnerRoot = Path.Combine(Path.GetTempPath(), "SoftwareUpdate");
            Directory.CreateDirectory(runnerRoot);
            if ((File.GetAttributes(runnerRoot) & FileAttributes.ReparsePoint) != 0)
                throw new SecurityException("Updater runner root cannot be a reparse point.");

            var runnerDirectory = Path.Combine(runnerRoot, $"runner-{Guid.NewGuid():N}");

            try
            {
                Directory.CreateDirectory(runnerDirectory);
                foreach (var payloadFile in payloadFiles)
                {
                    File.Copy(
                        payloadFile,
                        Path.Combine(runnerDirectory, Path.GetFileName(payloadFile)),
                        overwrite: false);
                }

                return Path.Combine(runnerDirectory, Path.GetFileName(fullUpdaterPath));
            }
            catch
            {
                DeleteUpdaterRunner(runnerDirectory);
                throw;
            }
        }

        /// <summary>
        /// Removes disposable updater runners left by completed updates.
        /// </summary>
        internal static void CleanupStaleUpdaterRunners()
        {
            var runnerRoot = Path.Combine(Path.GetTempPath(), "SoftwareUpdate");
            if (!Directory.Exists(runnerRoot))
                return;

            try
            {
                if ((File.GetAttributes(runnerRoot) & FileAttributes.ReparsePoint) != 0)
                    return;

                foreach (var directory in Directory.GetDirectories(runnerRoot, "runner-*"))
                {
                    var name = Path.GetFileName(directory);
                    if (!Guid.TryParseExact(name.Substring("runner-".Length), "N", out _))
                        continue;

                    if (Directory.GetLastWriteTimeUtc(directory) <= DateTime.UtcNow - StaleRunnerAge)
                    {
                        DeleteUpdaterRunner(directory);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                Debug.WriteLine($"[GitHubUpdateService] Failed to clean stale updater runners: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates checksum metadata before a download begins.
        /// </summary>
        /// <param name="update">The update metadata.</param>
        /// <param name="checksumRequired">Whether a checksum is required.</param>
        internal static void ValidateChecksumMetadata(UpdateInfo update, bool checksumRequired)
        {
            if (string.IsNullOrWhiteSpace(update.Sha256Checksum))
            {
                if (checksumRequired)
                {
                    throw new InvalidDataException(
                        "The selected update does not provide the required SHA256 checksum in its release notes.");
                }

                return;
            }

            if (!Regex.IsMatch(update.Sha256Checksum, "^[0-9a-fA-F]{64}$"))
                throw new InvalidDataException("The selected update provides an invalid SHA256 checksum.");
        }

        /// <summary>
        /// Validates downloaded content against an expected SHA256 checksum.
        /// </summary>
        /// <param name="stream">The seekable downloaded-content stream.</param>
        /// <param name="expectedChecksum">The expected hexadecimal checksum.</param>
        internal static void ValidateDownloadedChecksum(Stream stream, string expectedChecksum)
        {
            stream.Position = 0;
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(stream);
            var actualChecksum = Convert.ToHexString(hashBytes).ToLowerInvariant();
            if (!string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Checksum validation failed. Expected: {expectedChecksum}, Actual: {actualChecksum}");
            }
        }

        /// <summary>
        /// Best-effort deletion restricted to a disposable updater runner directory.
        /// </summary>
        /// <param name="directory">The runner directory.</param>
        internal static void DeleteUpdaterRunner(string? directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            try
            {
                var fullDirectory = Path.GetFullPath(directory);
                var runnerRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SoftwareUpdate"));
                if (!string.Equals(
                        Path.GetDirectoryName(fullDirectory),
                        runnerRoot,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var name = Path.GetFileName(directory);
                if (!name.StartsWith("runner-", StringComparison.OrdinalIgnoreCase) ||
                    !Guid.TryParseExact(name.Substring("runner-".Length), "N", out _))
                {
                    return;
                }

                var attributes = File.GetAttributes(directory);
                Directory.Delete(directory, recursive: (attributes & FileAttributes.ReparsePoint) == 0);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                Debug.WriteLine($"[GitHubUpdateService] Failed to delete updater runner '{directory}': {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void SkipVersion(SemanticVersion? version)
        {
            if (version == null) return;

            lock (_lock)
            {
                _skippedVersions.Add(GetSkipKey(version));
            }
            SaveSkippedVersions();
        }

        /// <inheritdoc/>
        public bool IsVersionSkipped(SemanticVersion? version)
        {
            if (version == null) return false;

            lock (_lock)
            {
                return _skippedVersions.Contains(GetSkipKey(version));
            }
        }

        /// <summary>
        /// Builds the persisted "skip" key for a <see cref="SemanticVersion"/>.
        /// </summary>
        /// <remarks>
        /// SemVer 2.0 §10 declares that build metadata MUST be ignored when determining version
        /// precedence — two versions that differ only in <c>+build</c> metadata represent the
        /// same release. <see cref="SemanticVersion.ToString"/> includes that metadata, so using
        /// it directly would let "1.2.3+build.42" be marked skipped while "1.2.3+build.43" is
        /// still surfaced. This helper returns <c>MAJOR.MINOR.PATCH[-PRERELEASE]</c> so a single
        /// skip entry covers every metadata variant of the same release.
        /// </remarks>
        /// <param name="version">The version whose skip key should be computed.</param>
        /// <returns>The metadata-stripped persistence key.</returns>
        private static string GetSkipKey(SemanticVersion version)
        {
            var key = $"{version.Major}.{version.Minor}.{version.Patch}";
            if (version.IsPreRelease)
                key += "-" + version.PreRelease;
            return key;
        }

        /// <inheritdoc/>
        public void ClearSkippedVersions()
        {
            lock (_lock)
            {
                _skippedVersions.Clear();
            }
            SaveSkippedVersions();
        }

        /// <summary>
        /// Finds an asset matching the configured pattern.
        /// </summary>
        /// <param name="release">The GitHub release to search.</param>
        /// <returns>The matching asset, or null if no match found.</returns>
        private GitHubReleaseAsset? FindMatchingAsset(GitHubRelease release)
        {
            if (release.Assets == null || !release.Assets.Any())
                return null;

            var pattern = Options.AssetNamePattern;
            if (string.IsNullOrEmpty(pattern))
                pattern = "*.zip";

            // Convert glob pattern to regex
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            return release.Assets.FirstOrDefault(a => a.Name != null && regex.IsMatch(a.Name));
        }

        /// <summary>
        /// Deserializes the GitHub releases JSON response.
        /// </summary>
        /// <param name="jsonStream">The JSON stream to deserialize.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>List of GitHub releases.</returns>
        private static async Task<List<GitHubRelease>> DeserializeReleasesAsync(Stream jsonStream, CancellationToken cancellationToken)
        {
            return await JsonSerializer.DeserializeAsync<List<GitHubRelease>>(jsonStream, cancellationToken: cancellationToken).ConfigureAwait(false)
                ?? new List<GitHubRelease>();
        }

        /// <summary>
        /// Computes the SHA256 checksum of a file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>The SHA256 checksum as a lowercase hexadecimal string.</returns>
        private static string ComputeSha256Checksum(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        // Pre-compiled regex for SHA256 checksum extraction. Matches "SHA256:" (case-insensitive)
        // followed by optional whitespace and exactly 64 hex characters. The release-notes convention
        // documented for this framework is `SHA256: <hex>` somewhere in the body of the GitHub release.
        private static readonly Regex Sha256ChecksumPattern = new Regex(
            @"SHA256\s*[:=]\s*([0-9a-fA-F]{64})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Extracts a SHA256 checksum from GitHub release notes when present.
        /// </summary>
        /// <param name="releaseBody">The body of the GitHub release (markdown).</param>
        /// <returns>
        /// The lowercase 64-character hex checksum if a <c>SHA256: &lt;hex&gt;</c> token is present
        /// in the release notes; otherwise <c>null</c>. Absence is treated as silent-skip per the
        /// documented contract on <see cref="UpdateInfo.Sha256Checksum"/>.
        /// </returns>
        private static string? ExtractSha256Checksum(string? releaseBody)
        {
            if (string.IsNullOrEmpty(releaseBody)) return null;
            var match = Sha256ChecksumPattern.Match(releaseBody);
            return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
        }

        /// <summary>
        /// Loads skipped versions from disk.
        /// </summary>
        /// <remarks>
        /// Migration: prior to F-005 the file persisted full <see cref="SemanticVersion.ToString"/>
        /// output, which embedded any build metadata (<c>+build.42</c>). Each line is now reduced
        /// to a metadata-stripped key (<c>MAJOR.MINOR.PATCH[-PRERELEASE]</c>) so old entries from
        /// previous versions still match against skip checks computed by <c>GetSkipKey</c>.
        /// </remarks>
        private void LoadSkippedVersions()
        {
            _skippedVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var filePath = Options.ResolvedSkippedVersionsPath;
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var trimmed = line.Trim();

                        // Migrate legacy "1.2.3+build.42" entries to "1.2.3" so the in-memory set
                        // always uses the same key shape that SkipVersion / IsVersionSkipped emit.
                        if (SemanticVersion.TryParse(trimmed, out var parsed) && parsed != null)
                        {
                            _skippedVersions.Add(GetSkipKey(parsed));
                        }
                        else
                        {
                            _skippedVersions.Add(trimmed);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log errors loading preferences for debugging
                Debug.WriteLine($"[GitHubUpdateService] Failed to load skipped versions: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves skipped versions to disk.
        /// </summary>
        /// <remarks>
        /// Data is copied inside the lock and written outside to minimize lock contention.
        /// A temp-file-then-rename strategy is used so the file is never left half-written.
        /// </remarks>
        private void SaveSkippedVersions()
        {
            try
            {
                var filePath = Options.ResolvedSkippedVersionsPath;
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                // Copy the set while holding the lock, then do I/O outside the lock.
                string[] versionsCopy;
                lock (_lock)
                {
                    versionsCopy = new string[_skippedVersions.Count];
                    _skippedVersions.CopyTo(versionsCopy);
                }

                // Write to a temp file then atomically rename to prevent a partial write
                // from corrupting the persisted file. Use a per-call unique temp suffix so two
                // concurrent saves never collide on the same on-disk path: a fixed ".tmp" suffix
                // would let a second writer truncate or rename a temp file the first writer is
                // still using, producing zero-byte or corrupt output.
                var tempPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllLines(tempPath, versionsCopy);
                File.Move(tempPath, filePath, overwrite: true);
            }
            catch (Exception ex)
            {
                // Log errors saving preferences for debugging
                Debug.WriteLine($"[GitHubUpdateService] Failed to save skipped versions: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes of the HTTP client and releases managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="GitHubUpdateService"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();

                    // Drop subscriber references so the service does not pin handlers (and any
                    // captured target objects) past disposal. Without this, a subscriber's
                    // closure can keep a reference to the service alive — and vice versa — for
                    // the lifetime of the host process.
                    UpdateCheckCompleted = null;
                    UpdateError = null;
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Safely raises an event, catching any exceptions from subscribers.
        /// </summary>
        /// <typeparam name="T">The event argument type.</typeparam>
        /// <param name="handler">The event handler to invoke.</param>
        /// <param name="args">The event arguments.</param>
        private void RaiseEvent<T>(EventHandler<T>? handler, T args)
        {
            if (handler == null) return;

            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GitHubUpdateService] Event handler threw exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines whether an HTTP status code is transient and worth retrying.
        /// Only 5xx server errors and 429 (Too Many Requests) are retried; other 4xx
        /// client errors are permanent and should not be retried.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to check.</param>
        /// <returns><c>true</c> if the request should be retried; otherwise, <c>false</c>.</returns>
        private static bool IsTransientStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.TooManyRequests ||
                   ((int)statusCode >= 500 && (int)statusCode < 600);
        }

        /// <summary>
        /// Performs an HTTP GET request with retry logic for transient failures.
        /// Only retries on 5xx server errors, 429 (Too Many Requests), or network-level
        /// failures (<see cref="HttpRequestException"/> with no HTTP status code).
        /// 4xx client errors (except 429) are returned immediately without retrying.
        /// </summary>
        /// <param name="url">The URL to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        private async Task<HttpResponseMessage> GetWithRetryAsync(string url, CancellationToken cancellationToken)
        {
            HttpRequestException? lastException = null;

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

                    // Return immediately for success or non-transient errors (e.g., 4xx other than 429)
                    if (response.IsSuccessStatusCode || !IsTransientStatusCode(response.StatusCode))
                        return response;

                    // Transient HTTP error (5xx or 429) — treat like a network failure and retry
                    Debug.WriteLine($"[GitHubUpdateService] Transient HTTP {(int)response.StatusCode} (attempt {attempt}/{MaxRetryAttempts}), retrying.");

                    if (attempt == MaxRetryAttempts)
                        return response; // Return the error response after all attempts are exhausted

                    response.Dispose();
                }
                catch (HttpRequestException ex)
                {
                    // Network-level failure (no HTTP response received) — always retry
                    lastException = ex;
                    Debug.WriteLine($"[GitHubUpdateService] Network failure (attempt {attempt}/{MaxRetryAttempts}): {ex.Message}");

                    if (attempt == MaxRetryAttempts)
                        throw;
                }

                // Exponential backoff: 500ms, 1000ms, etc.
                var delay = RetryDelayMs * attempt;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            // Should not reach here, but satisfy the compiler
            throw lastException!;
        }
    }
}
