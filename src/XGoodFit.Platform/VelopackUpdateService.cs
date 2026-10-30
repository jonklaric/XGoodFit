using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using SoftwareUpdate;
using Velopack;
using Velopack.Locators;
using Velopack.Logging;
using Velopack.Sources;
using SemanticVersion = SoftwareUpdate.SemanticVersion;
using UpdateInfo = SoftwareUpdate.UpdateInfo;
using UpdateOptions = SoftwareUpdate.UpdateOptions;
using VelopackUpdateInfo = Velopack.UpdateInfo;

namespace XGoodFit.Platform;

/// <summary>
/// XGoodFit addition: upstream's update service contract (SoftwareUpdate's <see cref="IUpdateService"/>)
/// over Velopack, which installs updates from this repository's GitHub Releases on every OS
/// (decision D7, migration plan §9.3).
/// </summary>
/// <remarks>
/// <para>
/// It takes the place of upstream's <c>GitHubUpdateService</c>, which downloads a zip from a release
/// and swaps the application's files with <c>SoftwareUpdate.Updater.exe</c>: that works only on
/// Windows, and would break a signed macOS bundle. FrameworkUI's main window drives either one
/// through the interface ("Check for Updates…", download with progress, install and restart), so
/// the application only constructs this one instead, with <see cref="CreateOptions"/> in place of
/// RMC-BestFit's <c>App.CreateUpdateOptions</c>. What differs:
/// </para>
/// <list type="bullet">
/// <item>Updates come from the release files <c>scripts/package.sh</c> builds: Velopack's
/// <c>releases.&lt;channel&gt;.json</c> and packages, where each runtime is a channel of its own
/// (<c>win-x64</c>, <c>osx-arm64</c>, …), so a copy only sees its own platform's packages. Velopack
/// checks each package's size and SHA-256 itself, so <c>RequireSha256Checksum</c>,
/// <c>AssetNamePattern</c> and the install and backup options don't apply.</item>
/// <item>Only a copy installed from an XGoodFit package can update (<see cref="IsInstalled"/>);
/// in any other copy the check fails, saying so.</item>
/// <item><see cref="DownloadUpdateAsync"/> puts the package in Velopack's packages folder, and
/// <see cref="InstallUpdateAndRestart"/> hands it to Velopack's updater, which waits for the
/// application to exit, installs it and starts the new version. The application exits at once, as
/// upstream's does.</item>
/// </list>
/// <para>
/// The states, events and skipped versions behave as upstream's.
/// </para>
/// </remarks>
public sealed class VelopackUpdateService : IUpdateService
{
    private const string NotInstalledMessage =
        "This copy of XGoodFit wasn't installed from an XGoodFit package, so it can't update itself. " +
        "The latest version is on the releases page: ";

    private readonly object _lock = new();
    private readonly UpdateManager? _manager;
    private readonly IVelopackLocator? _locator;
    private readonly GitHubReleaseSource? _gitHub;
    private UpdateState _state = UpdateState.Idle;
    private UpdateInfo? _availableUpdate;
    private VelopackUpdateInfo? _availableRelease;
    private VelopackAsset? _downloadedPackage;
    private HashSet<string> _skippedVersions = null!; // Assigned by LoadSkippedVersions() in the constructor.

    /// <summary>
    /// Creates the service for the running application, using the Velopack installation that the
    /// startup hook found (<see cref="PlatformStartup.BuildVelopackApp"/>; none in a copy that isn't packaged).
    /// </summary>
    /// <param name="options">Where to look for updates: the GitHub repository and the current version
    /// (see <see cref="CreateOptions"/>).</param>
    public VelopackUpdateService(UpdateOptions options)
        : this(options, source: null, locator: null)
    {
    }

    /// <summary>Creates the service over an update source and a Velopack installation of the caller's (tests).</summary>
    internal VelopackUpdateService(UpdateOptions options, IUpdateSource? source, IVelopackLocator? locator)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Options.Validate();

        _locator = locator ?? (VelopackLocator.IsCurrentSet ? VelopackLocator.Current : null);
        if (_locator != null)
        {
            if (source == null)
            {
                _gitHub = new GitHubReleaseSource(RepositoryUrl, options.GitHubToken, options.IncludePreReleases);
                source = _gitHub;
            }
            _manager = new UpdateManager(source, options: null, _locator);
        }

        LoadSkippedVersions();
    }

    /// <summary>
    /// The options for XGoodFit's own releases (<see cref="ProductInfo.UpdateRepository"/>), as
    /// RMC-BestFit's <c>App.CreateUpdateOptions</c> makes them for RMC-BestFit's.
    /// </summary>
    /// <param name="currentVersion">The running version (default: the installed package's, else
    /// the entry assembly's informational version, as RMC-BestFit's <c>App.ResolveCurrentVersion</c>
    /// reads it).</param>
    /// <returns>The options. A pre-release version also looks at pre-releases.</returns>
    public static UpdateOptions CreateOptions(SemanticVersion? currentVersion = null)
    {
        string[] path = new Uri(ProductInfo.UpdateRepository).AbsolutePath.Trim('/').Split('/');
        if (path.Length != 2)
            throw new InvalidOperationException($"The update repository isn't a GitHub repository's URL: {ProductInfo.UpdateRepository}");

        SemanticVersion version = currentVersion ?? ResolveCurrentVersion();
        return new UpdateOptions
        {
            GitHubOwner = path[0],
            GitHubRepo = path[1],
            CurrentVersion = version,
            IncludePreReleases = version.IsPreRelease,
            RequireSha256Checksum = true,
        };
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
    }

    /// <summary>
    /// True when this copy was installed from an XGoodFit package, so it can update itself. The main
    /// window shows "Check for Updates…" whenever it has an update service, so an application can
    /// leave the service out where this is false.
    /// </summary>
    public bool IsInstalled => _manager?.IsInstalled == true;

    /// <summary>The installed package's version, else the options' current version.</summary>
    public SemanticVersion CurrentVersion =>
        _manager?.CurrentVersion is { } installed ? SemanticVersion.Parse(installed.ToFullString()) : Options.CurrentVersion!;

    /// <summary>The page that lists every release, for a copy that can't update itself.</summary>
    public string ReleasesPageUrl => RepositoryUrl + "/releases";

    private string RepositoryUrl => $"https://github.com/{Options.GitHubOwner}/{Options.GitHubRepo}";

    /// <inheritdoc/>
    public event EventHandler<UpdateCheckResult>? UpdateCheckCompleted;

    /// <inheritdoc/>
    public event EventHandler<Exception>? UpdateError;

    /// <inheritdoc/>
    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        // As upstream: a check may run from any state except Installing.
        EnsureState(nameof(CheckForUpdateAsync),
            UpdateState.Idle,
            UpdateState.Checking,
            UpdateState.UpdateAvailable,
            UpdateState.UpToDate,
            UpdateState.Downloading,
            UpdateState.ReadyToInstall,
            UpdateState.Error);

        State = UpdateState.Checking;
        SemanticVersion current = CurrentVersion;
        try
        {
            if (!IsInstalled)
                throw new InvalidOperationException(NotInstalledMessage + ReleasesPageUrl);

            VelopackUpdateInfo? release = await _manager!.CheckForUpdatesAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            if (release == null)
            {
                lock (_lock)
                {
                    _availableUpdate = null;
                    _availableRelease = null;
                    _state = UpdateState.UpToDate;
                }
                var noUpdate = UpdateCheckResult.NoUpdateAvailable(current);
                RaiseEvent(UpdateCheckCompleted, noUpdate);
                return noUpdate;
            }

            UpdateInfo update = ToUpdateInfo(release);
            bool isSkipped;
            lock (_lock)
            {
                _availableUpdate = update;
                _availableRelease = release;
                isSkipped = _skippedVersions.Contains(GetSkipKey(update.Version!));
                _state = UpdateState.UpdateAvailable;
            }
            var result = UpdateCheckResult.UpdateAvailable(current, update, isSkipped);
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
            var result = UpdateCheckResult.Failed(current, ex);
            RaiseEvent(UpdateCheckCompleted, result);
            return result;
        }
    }

    /// <inheritdoc/>
    /// <remarks>Downloads the update the last check found: <paramref name="update"/> must be its <see cref="AvailableUpdate"/>.</remarks>
    public async Task<UpdateDownloadResult> DownloadUpdateAsync(
        UpdateInfo update,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        EnsureState(nameof(DownloadUpdateAsync), UpdateState.UpdateAvailable);

        VelopackUpdateInfo release;
        lock (_lock)
        {
            if (!ReferenceEquals(update, _availableUpdate) || _availableRelease == null)
                throw new ArgumentException("The update isn't the one the last check found.", nameof(update));
            release = _availableRelease;
            _state = UpdateState.Downloading;
        }

        long total = release.TargetFullRelease.Size;
        try
        {
            void Report(int percent)
            {
                progress?.Report(new UpdateDownloadProgress { TotalBytes = total, BytesDownloaded = total * Math.Clamp(percent, 0, 100) / 100 });
            }

            await _manager!.DownloadUpdatesAsync(release, Report, cancellationToken).ConfigureAwait(false);
            string path = LocalPackagePath(release.TargetFullRelease);
            if (!File.Exists(path))
                throw new FileNotFoundException("Velopack didn't leave the downloaded package where expected.", path);
            Report(100);
            lock (_lock)
            {
                _downloadedPackage = release.TargetFullRelease;
                _state = UpdateState.ReadyToInstall;
            }
            return UpdateDownloadResult.Successful(path, update, total);
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
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Starts Velopack's updater, which waits for this process to exit, installs the package and
    /// starts the new version, and then exits this process, as upstream's does: save first.
    /// </remarks>
    public void InstallUpdateAndRestart(string downloadedFilePath)
    {
        if (string.IsNullOrEmpty(downloadedFilePath))
            throw new ArgumentNullException(nameof(downloadedFilePath));
        if (!File.Exists(downloadedFilePath))
            throw new FileNotFoundException("Downloaded update file not found.", downloadedFilePath);
        EnsureState(nameof(InstallUpdateAndRestart), UpdateState.ReadyToInstall);

        VelopackAsset package;
        lock (_lock)
        {
            if (_downloadedPackage == null
                || !string.Equals(Path.GetFullPath(downloadedFilePath), Path.GetFullPath(LocalPackagePath(_downloadedPackage)), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The file isn't the update this service downloaded.", nameof(downloadedFilePath));
            package = _downloadedPackage;
            _state = UpdateState.Installing;
        }

        try
        {
            _manager!.ApplyUpdatesAndRestart(package);
        }
        catch
        {
            State = UpdateState.Error;
            throw;
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

    /// <inheritdoc/>
    public void ClearSkippedVersions()
    {
        lock (_lock)
        {
            _skippedVersions.Clear();
        }
        SaveSkippedVersions();
    }

    private UpdateInfo ToUpdateInfo(VelopackUpdateInfo release)
    {
        VelopackAsset package = release.TargetFullRelease;
        SemanticVersion version = SemanticVersion.Parse(package.Version.ToFullString());
        GithubRelease? gitHubRelease = _gitHub?.ReleaseOf(package);
        return new UpdateInfo
        {
            Version = version,
            Name = string.IsNullOrWhiteSpace(gitHubRelease?.Name) ? $"{ProductInfo.Name} {version}" : gitHubRelease.Name,
            ReleaseNotes = package.NotesMarkdown,
            PublishedAt = gitHubRelease?.PublishedAt?.ToLocalTime() ?? default,
            DownloadSize = package.Size,
            AssetName = package.FileName,
            IsPreRelease = version.IsPreRelease || gitHubRelease?.Prerelease == true,
            ReleasePageUrl = ReleasesPageUrl,
            Sha256Checksum = package.SHA256,
        };
    }

    // Where Velopack keeps a downloaded package (its internal GetLocalPackagePath).
    private string LocalPackagePath(VelopackAsset package)
    {
        string name = string.Concat(Path.GetFileName(package.FileName).Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.Combine(_locator!.PackagesDir!, name);
    }

    private static SemanticVersion ResolveCurrentVersion()
    {
        if (VelopackLocator.IsCurrentSet && VelopackLocator.Current.CurrentlyInstalledVersion is { } installed)
            return SemanticVersion.Parse(installed.ToFullString());

        // As RMC-BestFit's App.ResolveCurrentVersion, from the application's assembly.
        Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(VelopackUpdateService).Assembly;
        string? info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info) && SemanticVersion.TryParse(info, out SemanticVersion? parsed) && parsed != null)
            return parsed;
        return SemanticVersion.FromVersion(assembly.GetName().Version ?? new Version(0, 0, 0));
    }

    private void EnsureState(string caller, params UpdateState[] allowed)
    {
        UpdateState current = State;
        if (Array.IndexOf(allowed, current) < 0)
        {
            throw new InvalidOperationException(
                $"{caller} cannot run while the update service is in the {current} state. " +
                $"Expected one of: {string.Join(", ", allowed)}.");
        }
    }

    private void RaiseEvent<T>(EventHandler<T>? handler, T args)
    {
        if (handler == null) return;

        try
        {
            handler(this, args);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VelopackUpdateService] Event handler threw exception: {ex.Message}");
        }
    }

    // As upstream's GitHubUpdateService: one key per line, MAJOR.MINOR.PATCH[-PRERELEASE] (build
    // metadata doesn't make a different release), in Options.ResolvedSkippedVersionsPath.
    private static string GetSkipKey(SemanticVersion version)
    {
        var key = $"{version.Major}.{version.Minor}.{version.Patch}";
        if (version.IsPreRelease)
            key += "-" + version.PreRelease;
        return key;
    }

    private void LoadSkippedVersions()
    {
        _skippedVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            string filePath = Options.ResolvedSkippedVersionsPath;
            if (!File.Exists(filePath))
                return;
            foreach (string line in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string trimmed = line.Trim();
                _skippedVersions.Add(SemanticVersion.TryParse(trimmed, out SemanticVersion? parsed) && parsed != null ? GetSkipKey(parsed) : trimmed);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VelopackUpdateService] Failed to load skipped versions: {ex.Message}");
        }
    }

    private void SaveSkippedVersions()
    {
        try
        {
            string filePath = Options.ResolvedSkippedVersionsPath;
            string? folder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            string[] versions;
            lock (_lock)
            {
                versions = [.. _skippedVersions];
            }

            // A temporary file, then a rename, so the file is never half-written.
            string tempPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllLines(tempPath, versions);
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VelopackUpdateService] Failed to save skipped versions: {ex.Message}");
        }
    }

    /// <summary>Velopack's GitHub source, keeping each package's release for its name and date.</summary>
    private sealed class GitHubReleaseSource(string repoUrl, string? accessToken, bool prerelease)
        : GithubSource(repoUrl, accessToken, prerelease)
    {
        private readonly ConcurrentDictionary<string, GithubRelease> _releases = new(StringComparer.OrdinalIgnoreCase);

        public GithubRelease? ReleaseOf(VelopackAsset package) =>
            _releases.TryGetValue(package.FileName, out GithubRelease? release) ? release : null;

        public override async Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string? appId, string channel,
            Guid? stagingId = null, VelopackAsset? latestLocalRelease = null)
        {
            VelopackAssetFeed feed = await base.GetReleaseFeed(logger, appId, channel, stagingId, latestLocalRelease).ConfigureAwait(false);
            foreach (VelopackAsset asset in feed.Assets)
            {
                if (asset is GitBaseAsset fromRelease)
                    _releases.TryAdd(asset.FileName, fromRelease.Release);
            }
            return feed;
        }
    }
}
