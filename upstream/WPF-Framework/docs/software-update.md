# Software Update

[<- Previous: DAG Controls](dag-controls.md) | [Back to Index](index.md) | [Next: Migration Guide ->](migration-guide.md)

## Overview

The software update system provides automatic update checking, downloading, and installation for WPF applications using GitHub Releases as the distribution channel. It consists of two components:

- **SoftwareUpdate library** (`SoftwareUpdate` namespace) -- Checks the GitHub Releases API for new versions, downloads update packages with progress reporting, validates SHA256 checksums, and launches the updater process.
- **SoftwareUpdate.Updater** (`SoftwareUpdate.Updater` namespace) -- A standalone console application that performs the actual file replacement while the main application is closed. It creates backups, extracts the update zip, and restarts the application.

## Architecture

```
+---------------------------+          +----------------------------+
|    Main Application       |          |   GitHub Releases API      |
|                           |   HTTPS  |                            |
|  GitHubUpdateService      +--------->|  GET /repos/:owner/:repo/  |
|    CheckForUpdateAsync()  |<---------+       releases             |
|    DownloadUpdateAsync()  |          +----------------------------+
|    InstallUpdateAndRestart()
|         |                 |
|         | launches        |
|         v                 |
|  +--------------------+   |
|  | Environment.Exit() |   |
|  +--------------------+   |
+---------------------------+
          |
          | spawns process
          v
+---------------------------+
| SoftwareUpdate.Updater    |
|                           |
|  1. Wait for PID to exit  |
|  2. Create backup         |
|  3. Extract zip to target |
|  4. Clean old backups     |
|  5. Restart application   |
+---------------------------+
```

## Quick Start

### 1. Add project references

Reference `SoftwareUpdate` from your main application project.

### 2. Configure UpdateOptions

```csharp
var options = new UpdateOptions
{
    GitHubOwner = "USACE-RMC",
    GitHubRepo = "RMC-BestFit",
    CurrentVersion = new SemanticVersion(2, 0, 0),
    AssetNamePattern = "RMC-BestFit.*.zip",
    RequireSha256Checksum = true
};
options.AdditionalPreservedRelativePaths.Add("data/user");
```

### 3. Create the update service

```csharp
var updateService = new GitHubUpdateService(options);
```

The constructor validates options and configures an `HttpClient` with the GitHub API v3 accept header, a user agent, and an optional bearer token.

### 4. Check for updates

```csharp
var result = await updateService.CheckForUpdateAsync();

if (result.IsUpdateAvailable && !result.IsSkippedVersion)
{
    var update = result.Update;
    // Show update dialog with update.Version, update.ReleaseNotes, etc.
}
```

### 5. Download and install

```csharp
var progress = new Progress<UpdateDownloadProgress>(p =>
{
    progressBar.Value = p.ProgressPercentage;
    statusText.Text = p.ProgressText;
});

var downloadResult = await updateService.DownloadUpdateAsync(update, progress);

if (downloadResult.Success)
{
    // This launches the updater and exits the application
    updateService.InstallUpdateAndRestart(downloadResult.FilePath!);
}
```

## Semantic Versioning

The `SemanticVersion` class implements SemVer 2.0 with the format `MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]`.

### Parsing

```csharp
// Parse (throws FormatException on failure)
var version = SemanticVersion.Parse("2.1.0-beta.1");

// Safe parse
if (SemanticVersion.TryParse("v2.1.0", out var version))
{
    // version.Major == 2, version.Minor == 1, version.Patch == 0
}

// From System.Version
var semver = SemanticVersion.FromVersion(new Version(2, 1, 0));
```

The parser accepts an optional `v` prefix (e.g., `v2.0.0`). The patch component defaults to 0 if omitted.

### Constructors

```csharp
// Full constructor
var v1 = new SemanticVersion(major: 2, minor: 1, patch: 3);
var v2 = new SemanticVersion(2, 0, 0, preRelease: "beta.1");
var v3 = new SemanticVersion(2, 0, 0, preRelease: "rc.1", buildMetadata: "build.456");
```

### Comparison

`SemanticVersion` implements `IComparable<SemanticVersion>` and overloads all comparison operators:

```csharp
var v1 = SemanticVersion.Parse("1.0.0-alpha");
var v2 = SemanticVersion.Parse("1.0.0-beta");
var v3 = SemanticVersion.Parse("1.0.0");

// Pre-release versions have lower precedence than release versions
// v1 < v2 < v3
bool result = v1 < v2;  // true
bool result2 = v2 < v3; // true
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Major` | `int` | Major version number |
| `Minor` | `int` | Minor version number |
| `Patch` | `int` | Patch version number |
| `PreRelease` | `string?` | Pre-release identifier (e.g., `"beta.1"`) |
| `BuildMetadata` | `string?` | Build metadata (ignored in comparisons) |
| `IsPreRelease` | `bool` | `true` if `PreRelease` is non-empty |

### Methods

| Method | Description |
|--------|-------------|
| `ToString()` | Returns `"2.1.0"` or `"2.1.0-beta.1"` or `"2.1.0-beta.1+build.123"` |
| `ToTagString()` | Returns `"v2.1.0"` (with `v` prefix, matching GitHub tag format) |

## GitHub Release Setup

For the update service to find your releases, follow these conventions:

### Tag format

Tags must be valid semantic versions, optionally prefixed with `v`:

```
v2.0.0
v2.1.0-beta.1
2.0.0
```

### Asset requirements

Each release must include a zip file whose name matches the configured `AssetNamePattern`. The default pattern is `*.zip`.

Example release structure:

```
Release: v2.1.0
Tag: v2.1.0
Assets:
  - RMC-BestFit.2.1.0.zip    (matches "RMC-BestFit.*.zip")
```

### Pre-release support

Mark pre-release versions as "pre-release" in GitHub. They are excluded by default unless `IncludePreReleases = true`. Draft releases are always excluded.

### SHA256 checksum policy

When release notes contain `SHA256: <64-character hexadecimal value>`, the download is validated against it. Set `RequireSha256Checksum = true` to reject releases that do not provide this token. The default remains optional for compatibility.

## UpdateOptions Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `GitHubOwner` | `string?` | `null` | **Required.** GitHub repository owner (e.g., `"USACE-RMC"`). |
| `GitHubRepo` | `string?` | `null` | **Required.** GitHub repository name (e.g., `"RMC-BestFit"`). |
| `CurrentVersion` | `SemanticVersion?` | `null` | **Required.** The currently installed application version. |
| `AssetNamePattern` | `string` | `"*.zip"` | Glob pattern to match release asset filenames. Supports `*` and `?` wildcards. |
| `IncludePreReleases` | `bool` | `false` | Whether to include pre-release versions in update checks. |
| `GitHubToken` | `string?` | `null` | Optional GitHub personal access token for private repos or higher rate limits. |
| `InstallDirectory` | `string?` | `null` | Installation directory. Defaults to the entry assembly's directory. |
| `MainExecutableName` | `string?` | `null` | Executable to restart after update. Defaults to the current process name. |
| `CreateBackup` | `bool` | `true` | Whether to create a backup before updating. |
| `AdditionalPreservedRelativePaths` | `IList<string>` | Empty | Additional installation-relative paths that must never be changed by an update. |
| `RequireSha256Checksum` | `bool` | `false` | Whether release notes must provide a valid SHA256 checksum. |
| `SkippedVersionsFilePath` | `string?` | `null` | Path for storing skipped versions. Defaults to `%LOCALAPPDATA%/{repo}/skipped_versions.txt`. |
| `RequestTimeoutSeconds` | `int` | `30` | HTTP request timeout in seconds. |
| `UpdaterExecutablePath` | `string?` | `null` | Path to the updater executable. Defaults to `SoftwareUpdate.Updater.exe` in the install directory. |

### Resolved properties

These read-only properties compute default values when the corresponding configurable property is not set:

| Property | Fallback Logic |
|----------|---------------|
| `ResolvedInstallDirectory` | `InstallDirectory` or entry assembly's directory |
| `ResolvedMainExecutableName` | `MainExecutableName` or entry assembly filename |
| `ResolvedUpdaterPath` | `UpdaterExecutablePath` or `{ResolvedInstallDirectory}/SoftwareUpdate.Updater.exe` |
| `ResolvedSkippedVersionsPath` | `SkippedVersionsFilePath` or `%LOCALAPPDATA%/{GitHubRepo}/skipped_versions.txt` |

### Validation

Call `options.Validate()` to verify required properties. It throws `ArgumentException` if `GitHubOwner`, `GitHubRepo`, or `CurrentVersion` is missing, or if owner/repo names contain invalid characters.

## IUpdateService Interface

```csharp
public interface IUpdateService
{
    UpdateOptions Options { get; }
    UpdateState State { get; }
    UpdateInfo? AvailableUpdate { get; }

    Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    Task<UpdateDownloadResult> DownloadUpdateAsync(
        UpdateInfo update,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    void InstallUpdateAndRestart(string downloadedFilePath);

    void SkipVersion(SemanticVersion? version);
    bool IsVersionSkipped(SemanticVersion? version);
    void ClearSkippedVersions();

    event EventHandler<UpdateCheckResult>? UpdateCheckCompleted;
    event EventHandler<Exception>? UpdateError;
}
```

### UpdateState enum

| Value | Description |
|-------|-------------|
| `Idle` | No check in progress |
| `Checking` | Currently checking the GitHub API |
| `UpdateAvailable` | A newer version was found |
| `Downloading` | Download is in progress |
| `ReadyToInstall` | Download completed, ready for installation |
| `Installing` | Updater has been launched |
| `Error` | An error occurred |
| `UpToDate` | Current version is the latest |

## Update Flow

### 1. Check for updates

`CheckForUpdateAsync()` queries `https://api.github.com/repos/{owner}/{repo}/releases`, filters out drafts and (optionally) pre-releases, parses tags as semantic versions, finds the first release with a matching asset that is newer than `CurrentVersion`, and returns an `UpdateCheckResult`.

The service retries transient HTTP failures up to 3 times with exponential backoff (500ms, 1000ms).

```csharp
var result = await updateService.CheckForUpdateAsync();

if (result.Success && result.IsUpdateAvailable)
{
    UpdateInfo update = result.Update!;
    // update.Version, update.ReleaseNotes, update.DownloadSize, etc.
}
else if (!result.Success)
{
    Exception error = result.Error!;
}
```

### 2. Download update

`DownloadUpdateAsync()` downloads the asset to a GUID-named directory under `%TEMP%/SoftwareUpdate/` with progress reporting every 100ms. If a SHA256 checksum is provided, it validates the download; required-checksum mode fails before network transfer when metadata is missing or malformed.

```csharp
var downloadResult = await updateService.DownloadUpdateAsync(
    update,
    new Progress<UpdateDownloadProgress>(p =>
    {
        // p.ProgressPercentage (0-100)
        // p.ProgressText ("1.5 MB / 10.0 MB (15%)")
        // p.SpeedText ("2.3 MB/s")
        // p.BytesDownloaded, p.TotalBytes, p.BytesPerSecond
    }),
    cancellationToken);

if (downloadResult.Success)
{
    string filePath = downloadResult.FilePath!;
}
else if (downloadResult.WasCancelled)
{
    // User cancelled
}
else
{
    Exception error = downloadResult.Error!;
}
```

### 3. Install and restart

`InstallUpdateAndRestart()` copies the complete updater payload to a disposable `%TEMP%/SoftwareUpdate/runner-{guid}` directory, launches that copy, and calls `Environment.Exit(0)`. Running outside the installation allows the update to replace all installed updater files. No code executes after this call.

```csharp
updateService.InstallUpdateAndRestart(downloadResult.FilePath!);
// Application exits here
```

### Skipping versions

Allow users to skip a specific version:

```csharp
updateService.SkipVersion(update.Version);
// On next check, result.IsSkippedVersion will be true for this version

// Clear all skipped versions
updateService.ClearSkippedVersions();
```

Skipped versions are persisted to `ResolvedSkippedVersionsPath`.

## Backup and Recovery

When `CreateBackup` is `true` (the default), the updater backs up only existing files that the validated update manifest will overwrite.

- **Backup location:** `{TargetDirectory}/.backup_{timestamp}/`
- **Retention:** Only the 2 most recent backups are kept. Older backups are deleted during cleanup.
- **Recovery:** If file replacement fails, overwritten files are restored and files or directories introduced by the failed update are removed.

To disable backups, set `UpdateOptions.CreateBackup = false`. Newly introduced files are still removed after failure, but overwritten files cannot be restored.

The updater always preserves root-level `settings`, `logs`, and `updates_pending` paths plus every `.backup_*` path. These built-in protections cannot be disabled by callers or archive layout.

## Deployment

### File layout

Deploy the complete updater payload alongside your main application:

```
MyApp/
  MyApp.exe
  SoftwareUpdate.Updater.exe
  SoftwareUpdate.Updater.dll
  SoftwareUpdate.Updater.deps.json
  SoftwareUpdate.Updater.runtimeconfig.json
  SoftwareUpdate.dll
  ... other assemblies ...
```

If the updater is in a different location, set `UpdateOptions.UpdaterExecutablePath` to its path.

### Updater CLI arguments

The updater accepts these command-line arguments (built automatically by `InstallUpdateAndRestart` and `UpdaterBootstrapper.LaunchUpdater`):

| Argument | Required | Description |
|----------|----------|-------------|
| `--pid <id>` | Yes | Process ID of the main application to wait for |
| `--zip <path>` | Yes | Path to the downloaded update zip file |
| `--target <dir>` | Yes | Target installation directory |
| `--exe <name>` | Yes | Main executable name to restart |
| `--backup` | No | Flag to create a backup before updating |
| `--preserve <path>` | No | Repeatable installation-relative path to preserve |

### UpdaterBootstrapper

For advanced scenarios where you want to control the updater launch independently of `GitHubUpdateService`, use `UpdaterBootstrapper.LaunchUpdater`:

```csharp
UpdaterBootstrapper.LaunchUpdater(
    updaterPath: @"C:\MyApp\SoftwareUpdate.Updater.exe",
    zipPath: @"C:\Temp\update.zip",
    targetDirectory: @"C:\MyApp",
    mainExecutable: "MyApp.exe",
    createBackup: true,
    exitApplication: true,  // calls Environment.Exit(0)
    additionalPreservedRelativePaths: new[] { "data/user" }
);
```

Set `exitApplication: false` if you need to perform cleanup before exiting.

## Error Handling

### Update check errors

`CheckForUpdateAsync` catches all exceptions and returns them in `UpdateCheckResult.Error`. It also raises the `UpdateError` event.

```csharp
updateService.UpdateError += (sender, ex) =>
{
    logger.Error($"Update error: {ex.Message}");
};

var result = await updateService.CheckForUpdateAsync();
if (!result.Success)
{
    if (result.Error is HttpRequestException httpEx)
    {
        // Network error or GitHub API rate limit
    }
}
```

### Rate limiting

Unauthenticated GitHub API requests are limited to 60 per hour. When the rate limit is exceeded, the service throws `HttpRequestException` with a descriptive message. To increase the limit to 5,000 per hour, set `UpdateOptions.GitHubToken` to a personal access token.

### Download errors

Download failures are returned in `UpdateDownloadResult`:

```csharp
var downloadResult = await updateService.DownloadUpdateAsync(update, progress, cts.Token);

if (downloadResult.WasCancelled)
{
    // Cancellation via CancellationToken
}
else if (!downloadResult.Success)
{
    // downloadResult.Error contains the exception
    // Checksum validation failures throw InvalidOperationException
}
```

### Installation errors

`InstallUpdateAndRestart` throws immediately if the updater executable or downloaded file is not found:

- `FileNotFoundException` -- Updater executable or zip file does not exist
- `ArgumentNullException` -- Downloaded file path is null or empty

The updater process itself logs all operations to `{TargetDirectory}/logs/update_{timestamp}.log` and displays errors in the console window. On failure, it attempts to restore from backup (if `--backup` was specified).

### Updater process considerations

- The updater waits up to 60 seconds for the main application to exit and aborts without changing files if the timeout expires.
- The updater detects a single wrapper folder and removes it while applying protection checks both before and after removal.
- The entire archive is preflighted and staged before installation; unsafe paths, collisions, reparse points, excessive file counts, and excessive uncompressed size abort the update.
- Settings, logs, backups, and update staging remain untouched by archive content.
- The updater console auto-closes after 3 seconds on success. On failure, it waits for a key press.
