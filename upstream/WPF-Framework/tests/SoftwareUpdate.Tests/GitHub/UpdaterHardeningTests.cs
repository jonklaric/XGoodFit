using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SoftwareUpdate.GitHub;
using SoftwareUpdate.Utilities;
using Xunit;

namespace SoftwareUpdate.Tests.GitHub
{
    /// <summary>
    /// Tests updater runner preparation and checksum hardening helpers.
    /// </summary>
    public class UpdaterHardeningTests
    {
        /// <summary>
        /// Verifies that a disposable runner contains the complete updater payload.
        /// </summary>
        [Fact]
        public void CreateUpdaterRunner_CopiesCompletePayloadOutsideInstallation()
        {
            var sourceDirectory = Path.Combine(Path.GetTempPath(), "UpdaterRunnerTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(sourceDirectory);
            var payloadNames = new[]
            {
                "SoftwareUpdate.Updater.exe",
                "SoftwareUpdate.Updater.dll",
                "SoftwareUpdate.Updater.deps.json",
                "SoftwareUpdate.Updater.runtimeconfig.json"
            };

            foreach (var payloadName in payloadNames)
            {
                File.WriteAllText(Path.Combine(sourceDirectory, payloadName), payloadName);
            }

            string? runnerPath = null;
            try
            {
                runnerPath = GitHubUpdateService.CreateUpdaterRunner(
                    Path.Combine(sourceDirectory, "SoftwareUpdate.Updater.exe"));
                var runnerDirectory = Path.GetDirectoryName(runnerPath)!;

                Assert.NotEqual(sourceDirectory, runnerDirectory);
                Assert.StartsWith("runner-", Path.GetFileName(runnerDirectory), StringComparison.OrdinalIgnoreCase);
                foreach (var payloadName in payloadNames)
                {
                    Assert.Equal(
                        payloadName,
                        File.ReadAllText(Path.Combine(runnerDirectory, payloadName)));
                }
            }
            finally
            {
                if (runnerPath != null)
                {
                    Directory.Delete(Path.GetDirectoryName(runnerPath)!, recursive: true);
                }

                Directory.Delete(sourceDirectory, recursive: true);
            }
        }

        /// <summary>
        /// Verifies that stale disposable runner directories are removed.
        /// </summary>
        [Fact]
        public void CleanupStaleUpdaterRunners_RemovesOwnedStaleDirectory()
        {
            var runnerDirectory = Path.Combine(
                Path.GetTempPath(),
                "SoftwareUpdate",
                $"runner-{Guid.NewGuid():N}");
            Directory.CreateDirectory(runnerDirectory);
            File.WriteAllText(Path.Combine(runnerDirectory, "payload.txt"), "stale");
            Directory.SetLastWriteTimeUtc(runnerDirectory, DateTime.UtcNow.AddDays(-2));

            GitHubUpdateService.CleanupStaleUpdaterRunners();

            Assert.False(Directory.Exists(runnerDirectory));
        }

        /// <summary>
        /// Verifies required, optional, and malformed checksum metadata behavior.
        /// </summary>
        [Fact]
        public void ValidateChecksumMetadata_EnforcesConfiguredPolicy()
        {
            var update = new UpdateInfo();

            GitHubUpdateService.ValidateChecksumMetadata(update, checksumRequired: false);
            Assert.Throws<InvalidDataException>(() =>
                GitHubUpdateService.ValidateChecksumMetadata(update, checksumRequired: true));

            update.Sha256Checksum = "not-a-checksum";
            Assert.Throws<InvalidDataException>(() =>
                GitHubUpdateService.ValidateChecksumMetadata(update, checksumRequired: false));

            update.Sha256Checksum = new string('a', 64);
            GitHubUpdateService.ValidateChecksumMetadata(update, checksumRequired: true);
        }

        /// <summary>
        /// Verifies successful and mismatched SHA256 content validation.
        /// </summary>
        [Fact]
        public void ValidateDownloadedChecksum_AcceptsMatchAndRejectsMismatch()
        {
            var content = Encoding.UTF8.GetBytes("verified update");
            var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

            using var matchingStream = new MemoryStream(content);
            GitHubUpdateService.ValidateDownloadedChecksum(matchingStream, checksum);

            using var mismatchedStream = new MemoryStream(content);
            Assert.Throws<InvalidOperationException>(() =>
                GitHubUpdateService.ValidateDownloadedChecksum(mismatchedStream, new string('0', 64)));
        }

        /// <summary>
        /// Verifies that the advanced bootstrapper rejects unsafe preserved paths before launch.
        /// </summary>
        [Fact]
        public void UpdaterBootstrapper_UnsafePreservedPath_IsRejectedBeforeLaunch()
        {
            var root = Path.Combine(Path.GetTempPath(), "UpdaterBootstrapperTests", Guid.NewGuid().ToString("N"));
            var updaterDirectory = Path.Combine(root, "updater");
            var targetDirectory = Path.Combine(root, "target");
            Directory.CreateDirectory(updaterDirectory);
            Directory.CreateDirectory(targetDirectory);
            var updaterPath = Path.Combine(updaterDirectory, "SoftwareUpdate.Updater.exe");
            var zipPath = Path.Combine(root, "update.zip");
            File.WriteAllText(updaterPath, "exe");
            File.WriteAllText(Path.Combine(updaterDirectory, "SoftwareUpdate.Updater.dll"), "dll");
            File.WriteAllText(Path.Combine(updaterDirectory, "SoftwareUpdate.Updater.deps.json"), "deps");
            File.WriteAllText(Path.Combine(updaterDirectory, "SoftwareUpdate.Updater.runtimeconfig.json"), "runtime");
            File.WriteAllText(zipPath, "zip");

            try
            {
                Assert.Throws<ArgumentException>(() => UpdaterBootstrapper.LaunchUpdater(
                    updaterPath,
                    zipPath,
                    targetDirectory,
                    "app.exe",
                    createBackup: true,
                    exitApplication: false,
                    additionalPreservedRelativePaths: new[] { "../settings" }));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        /// <summary>
        /// Verifies that the original bootstrapper method remains available for binary compatibility.
        /// </summary>
        [Fact]
        public void UpdaterBootstrapper_OriginalLaunchSignature_RemainsAvailable()
        {
            var method = typeof(UpdaterBootstrapper).GetMethod(
                nameof(UpdaterBootstrapper.LaunchUpdater),
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(string),
                    typeof(string),
                    typeof(bool),
                    typeof(bool)
                });

            Assert.NotNull(method);
        }
    }
}
