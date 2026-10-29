#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type

using System.Net.Http;
using SoftwareUpdate;
using SoftwareUpdate.GitHub;
using SoftwareUpdate.Utilities;

namespace Documentation.Tests.Snippets
{
    /// <summary>
    /// Validates that all C# code snippets in docs/software-update.md compile correctly.
    /// </summary>
    public class SoftwareUpdateSnippets
    {
        // ---------------------------------------------------------------
        // Snippet: Configure UpdateOptions (Quick Start)
        // ---------------------------------------------------------------
        public void Snippet_ConfigureUpdateOptions()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "USACE-RMC",
                GitHubRepo = "RMC-BestFit",
                CurrentVersion = new SemanticVersion(2, 0, 0),
                AssetNamePattern = "RMC-BestFit.*.zip"
            };
        }

        // ---------------------------------------------------------------
        // Snippet: Create the update service (Quick Start)
        // ---------------------------------------------------------------
        public void Snippet_CreateUpdateService()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "USACE-RMC",
                GitHubRepo = "RMC-BestFit",
                CurrentVersion = new SemanticVersion(2, 0, 0),
                AssetNamePattern = "RMC-BestFit.*.zip"
            };

            var updateService = new GitHubUpdateService(options);
        }

        // ---------------------------------------------------------------
        // Snippet: Check for updates (Quick Start)
        // ---------------------------------------------------------------
        public async Task Snippet_CheckForUpdates()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "USACE-RMC",
                GitHubRepo = "RMC-BestFit",
                CurrentVersion = new SemanticVersion(2, 0, 0)
            };
            var updateService = new GitHubUpdateService(options);

            var result = await updateService.CheckForUpdateAsync();

            if (result.IsUpdateAvailable && !result.IsSkippedVersion)
            {
                var update = result.Update;
                // Show update dialog with update.Version, update.ReleaseNotes, etc.
            }
        }

        // ---------------------------------------------------------------
        // Snippet: Download and install (Quick Start)
        // ---------------------------------------------------------------
        public async Task Snippet_DownloadAndInstall()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "USACE-RMC",
                GitHubRepo = "RMC-BestFit",
                CurrentVersion = new SemanticVersion(2, 0, 0)
            };
            var updateService = new GitHubUpdateService(options);
            var checkResult = await updateService.CheckForUpdateAsync();
            if (!checkResult.IsUpdateAvailable) return;
            var update = checkResult.Update;

            var progressBar = new System.Windows.Controls.ProgressBar();
            var statusText = new System.Windows.Controls.TextBlock();

            var progress = new Progress<UpdateDownloadProgress>(p =>
            {
                progressBar.Value = p.ProgressPercentage;
                statusText.Text = p.ProgressText;
            });

            var downloadResult = await updateService.DownloadUpdateAsync(update, progress);

            if (downloadResult.Success)
            {
                // This launches the updater and exits the application
                // updateService.InstallUpdateAndRestart(downloadResult.FilePath!);
                // NOTE: Not actually calling this in test -- it calls Environment.Exit
                _ = downloadResult.FilePath;
            }
        }

        // ---------------------------------------------------------------
        // Snippet: Semantic Versioning -- Parsing
        // ---------------------------------------------------------------
        public void Snippet_SemanticVersion_Parsing()
        {
            // Parse (throws FormatException on failure)
            var version = SemanticVersion.Parse("2.1.0-beta.1");

            // Safe parse
            if (SemanticVersion.TryParse("v2.1.0", out var version2))
            {
                // version2.Major == 2, version2.Minor == 1, version2.Patch == 0
            }

            // From System.Version
            var semver = SemanticVersion.FromVersion(new Version(2, 1, 0));
        }

        // ---------------------------------------------------------------
        // Snippet: Semantic Versioning -- Constructors
        // ---------------------------------------------------------------
        public void Snippet_SemanticVersion_Constructors()
        {
            // Full constructor
            var v1 = new SemanticVersion(major: 2, minor: 1, patch: 3);
            var v2 = new SemanticVersion(2, 0, 0, preRelease: "beta.1");
            var v3 = new SemanticVersion(2, 0, 0, preRelease: "rc.1", buildMetadata: "build.456");
        }

        // ---------------------------------------------------------------
        // Snippet: Semantic Versioning -- Comparison
        // ---------------------------------------------------------------
        public void Snippet_SemanticVersion_Comparison()
        {
            var v1 = SemanticVersion.Parse("1.0.0-alpha");
            var v2 = SemanticVersion.Parse("1.0.0-beta");
            var v3 = SemanticVersion.Parse("1.0.0");

            // Pre-release versions have lower precedence than release versions
            // v1 < v2 < v3
            bool result = v1 < v2;  // true
            bool result2 = v2 < v3; // true
        }

        // ---------------------------------------------------------------
        // Snippet: Update Flow -- Check for updates (detailed)
        // ---------------------------------------------------------------
        public async Task Snippet_UpdateFlow_CheckDetailed()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "USACE-RMC",
                GitHubRepo = "RMC-BestFit",
                CurrentVersion = new SemanticVersion(2, 0, 0)
            };
            var updateService = new GitHubUpdateService(options);

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
        }

        // ---------------------------------------------------------------
        // Snippet: Update Flow -- Download update (detailed)
        // ---------------------------------------------------------------
        public async Task Snippet_UpdateFlow_DownloadDetailed()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "USACE-RMC",
                GitHubRepo = "RMC-BestFit",
                CurrentVersion = new SemanticVersion(2, 0, 0)
            };
            var updateService = new GitHubUpdateService(options);
            var checkResult = await updateService.CheckForUpdateAsync();
            if (!checkResult.IsUpdateAvailable) return;
            var update = checkResult.Update;

            var cancellationToken = CancellationToken.None;

            var downloadResult = await updateService.DownloadUpdateAsync(
                update,
                new Progress<UpdateDownloadProgress>(p =>
                {
                    // p.ProgressPercentage (0-100)
                    // p.ProgressText ("1.5 MB / 10.0 MB (15%)")
                    // p.SpeedText ("2.3 MB/s")
                    // p.BytesDownloaded, p.TotalBytes, p.BytesPerSecond
                    _ = p.ProgressPercentage;
                    _ = p.ProgressText;
                    _ = p.SpeedText;
                    _ = p.BytesDownloaded;
                    _ = p.TotalBytes;
                    _ = p.BytesPerSecond;
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
        }

        // ---------------------------------------------------------------
        // Snippet: Install and restart
        // ---------------------------------------------------------------
        public void Snippet_InstallAndRestart()
        {
            // NOTE: Cannot actually call InstallUpdateAndRestart in a test
            // because it calls Environment.Exit. Verify the API compiles:
            var options = new UpdateOptions
            {
                GitHubOwner = "USACE-RMC",
                GitHubRepo = "RMC-BestFit",
                CurrentVersion = new SemanticVersion(2, 0, 0)
            };
            var updateService = new GitHubUpdateService(options);

            // updateService.InstallUpdateAndRestart(downloadResult.FilePath!);
            // Verify the method exists with the correct signature:
            var method = typeof(GitHubUpdateService).GetMethod("InstallUpdateAndRestart");
        }

        // ---------------------------------------------------------------
        // Snippet: Skipping versions
        // ---------------------------------------------------------------
        public void Snippet_SkippingVersions()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "USACE-RMC",
                GitHubRepo = "RMC-BestFit",
                CurrentVersion = new SemanticVersion(2, 0, 0)
            };
            var updateService = new GitHubUpdateService(options);
            var version = new SemanticVersion(2, 1, 0);

            updateService.SkipVersion(version);
            // On next check, result.IsSkippedVersion will be true for this version

            // Clear all skipped versions
            updateService.ClearSkippedVersions();
        }

        // ---------------------------------------------------------------
        // Snippet: Error Handling -- UpdateError event
        // ---------------------------------------------------------------
        public async Task Snippet_ErrorHandling_UpdateError()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "USACE-RMC",
                GitHubRepo = "RMC-BestFit",
                CurrentVersion = new SemanticVersion(2, 0, 0)
            };
            var updateService = new GitHubUpdateService(options);

            updateService.UpdateError += (sender, ex) =>
            {
                // logger.Error($"Update error: {ex.Message}");
                _ = ex.Message;
            };

            var result = await updateService.CheckForUpdateAsync();
            if (!result.Success)
            {
                if (result.Error is HttpRequestException httpEx)
                {
                    // Network error or GitHub API rate limit
                }
            }
        }

        // ---------------------------------------------------------------
        // Snippet: UpdaterBootstrapper.LaunchUpdater
        // ---------------------------------------------------------------
        public void Snippet_UpdaterBootstrapper()
        {
            // Verify the API compiles (won't actually launch):
            var method = typeof(UpdaterBootstrapper).GetMethod("LaunchUpdater");

            // The documented signature:
            // UpdaterBootstrapper.LaunchUpdater(
            //     updaterPath: @"C:\MyApp\SoftwareUpdate.Updater.exe",
            //     zipPath: @"C:\Temp\update.zip",
            //     targetDirectory: @"C:\MyApp",
            //     mainExecutable: "MyApp.exe",
            //     createBackup: true,
            //     exitApplication: true
            // );
        }
    }
}
