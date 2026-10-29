using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SoftwareUpdate
{
    /// <summary>
    /// Configuration options for the software update service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class UpdateOptions
    {
        // GitHub username/repo format: alphanumeric, hyphens, periods, and underscores allowed (not at start/end), max 39 chars for usernames
        private static readonly Regex GitHubNamePattern = new Regex(
            @"^[a-zA-Z0-9]([a-zA-Z0-9._-]{0,37}[a-zA-Z0-9])?$",
            RegexOptions.Compiled);

        /// <summary>
        /// Gets or sets the GitHub repository owner (e.g., "USACE-RMC").
        /// </summary>
        public string? GitHubOwner { get; set; }

        /// <summary>
        /// Gets or sets the GitHub repository name (e.g., "RMC-BestFit").
        /// </summary>
        public string? GitHubRepo { get; set; }

        /// <summary>
        /// Gets or sets the current version of the application.
        /// </summary>
        public SemanticVersion? CurrentVersion { get; set; }

        /// <summary>
        /// Gets or sets a pattern to match the release asset filename.
        /// Supports wildcards (* and ?). Default is "*.zip".
        /// </summary>
        /// <example>
        /// "RMC-BestFit.Version.*.zip"
        /// </example>
        public string AssetNamePattern { get; set; } = "*.zip";

        /// <summary>
        /// Gets or sets whether to include pre-release versions in update checks.
        /// Default is false.
        /// </summary>
        public bool IncludePreReleases { get; set; } = false;

        /// <summary>
        /// Gets or sets an optional GitHub personal access token for authenticated requests.
        /// Useful for private repositories or to avoid rate limiting.
        /// </summary>
        public string? GitHubToken { get; set; }

        /// <summary>
        /// Gets or sets the directory where the application is installed.
        /// If not specified, uses the directory containing the main executable.
        /// </summary>
        public string? InstallDirectory { get; set; }

        /// <summary>
        /// Gets or sets the name of the main executable to restart after update.
        /// If not specified, uses the current process executable name.
        /// </summary>
        public string? MainExecutableName { get; set; }

        /// <summary>
        /// Gets or sets whether to create a backup before updating.
        /// Default is true.
        /// </summary>
        public bool CreateBackup { get; set; } = true;

        /// <summary>
        /// Gets additional installation-relative paths that the updater must preserve.
        /// </summary>
        /// <remarks>
        /// The updater always preserves its built-in runtime paths, including <c>settings</c>,
        /// <c>logs</c>, <c>updates_pending</c>, and backup directories. Entries added here are
        /// validated as relative paths and preserve both the path itself and its descendants.
        /// </remarks>
        public IList<string> AdditionalPreservedRelativePaths { get; } = new List<string>();

        /// <summary>
        /// Gets or sets whether downloads without a valid SHA256 checksum must be rejected.
        /// </summary>
        /// <remarks>
        /// The default is <see langword="false"/> for compatibility. When enabled, GitHub
        /// release notes must contain a <c>SHA256: &lt;64-character hexadecimal value&gt;</c>
        /// token for the selected update asset.
        /// </remarks>
        public bool RequireSha256Checksum { get; set; }

        /// <summary>
        /// Gets or sets the path to store skipped version preferences.
        /// Default is in the application's local app data folder.
        /// </summary>
        public string? SkippedVersionsFilePath { get; set; }

        /// <summary>
        /// Gets or sets the timeout for HTTP requests in seconds.
        /// Default is 30 seconds.
        /// </summary>
        public int RequestTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the path to the external updater executable.
        /// If not specified, looks for "SoftwareUpdate.Updater.exe" in the application directory.
        /// </summary>
        public string? UpdaterExecutablePath { get; set; }

        /// <summary>
        /// Gets the resolved install directory.
        /// </summary>
        public string ResolvedInstallDirectory
        {
            get
            {
                if (!string.IsNullOrEmpty(InstallDirectory))
                    return InstallDirectory;

                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                return Path.GetDirectoryName(assembly.Location) ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the resolved main executable name.
        /// </summary>
        public string ResolvedMainExecutableName
        {
            get
            {
                if (!string.IsNullOrEmpty(MainExecutableName))
                    return MainExecutableName;

                var assembly = Assembly.GetEntryAssembly();
                if (assembly != null)
                    return Path.GetFileName(assembly.Location);

                return AppDomain.CurrentDomain.FriendlyName;
            }
        }

        /// <summary>
        /// Gets the resolved updater executable path.
        /// </summary>
        public string ResolvedUpdaterPath
        {
            get
            {
                if (!string.IsNullOrEmpty(UpdaterExecutablePath))
                    return UpdaterExecutablePath;

                return Path.Combine(ResolvedInstallDirectory, "SoftwareUpdate.Updater.exe");
            }
        }

        /// <summary>
        /// Gets the resolved skipped versions file path.
        /// </summary>
        public string ResolvedSkippedVersionsPath
        {
            get
            {
                if (!string.IsNullOrEmpty(SkippedVersionsFilePath))
                    return SkippedVersionsFilePath;

                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var appName = GitHubRepo ?? "SoftwareUpdate";
                return Path.Combine(appData, appName, "skipped_versions.txt");
            }
        }

        /// <summary>
        /// Validates the options and throws if invalid.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(GitHubOwner))
                throw new ArgumentException("GitHubOwner is required.", nameof(GitHubOwner));

            if (!GitHubNamePattern.IsMatch(GitHubOwner))
                throw new ArgumentException(
                    "GitHubOwner must contain only alphanumeric characters, hyphens, periods, and underscores, " +
                    "cannot start or end with a special character, and must be 1-39 characters.",
                    nameof(GitHubOwner));

            if (string.IsNullOrWhiteSpace(GitHubRepo))
                throw new ArgumentException("GitHubRepo is required.", nameof(GitHubRepo));

            if (!GitHubNamePattern.IsMatch(GitHubRepo))
                throw new ArgumentException(
                    "GitHubRepo must contain only alphanumeric characters, hyphens, periods, and underscores, " +
                    "cannot start or end with a special character, and must be 1-39 characters.",
                    nameof(GitHubRepo));

            if (CurrentVersion == null)
                throw new ArgumentException("CurrentVersion is required.", nameof(CurrentVersion));

            if (RequestTimeoutSeconds < 1 || RequestTimeoutSeconds > 300)
                throw new ArgumentOutOfRangeException(nameof(RequestTimeoutSeconds),
                    "RequestTimeoutSeconds must be between 1 and 300.");

            foreach (var preservedPath in AdditionalPreservedRelativePaths)
            {
                ValidatePreservedPath(preservedPath);
            }
        }

        /// <summary>
        /// Validates an installation-relative path that must be preserved during updates.
        /// </summary>
        /// <param name="path">The relative path to validate.</param>
        private static void ValidatePreservedPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.Contains(':'))
            {
                throw new ArgumentException(
                    "Additional preserved paths must be non-empty relative paths.",
                    nameof(AdditionalPreservedRelativePaths));
            }

            var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || segments.Any(segment =>
                    segment is "." or ".." ||
                    segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    segment.EndsWith(" ", StringComparison.Ordinal) ||
                    segment.EndsWith(".", StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    "Additional preserved paths cannot contain current-directory or parent-directory segments.",
                    nameof(AdditionalPreservedRelativePaths));
            }
        }
    }
}
