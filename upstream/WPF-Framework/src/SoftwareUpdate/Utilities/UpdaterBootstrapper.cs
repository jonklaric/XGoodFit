using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SoftwareUpdate.GitHub;

namespace SoftwareUpdate.Utilities
{
    /// <summary>
    /// Utility class for launching the external updater process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class UpdaterBootstrapper
    {
        /// <summary>
        /// Launches the external updater process to perform an application update.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method copies the complete updater payload to a disposable directory and launches that copy
        /// to handle file replacement. The updater waits for the current process to exit before applying
        /// staged files, then optionally restarts the main application.
        /// </para>
        /// <para>
        /// If <paramref name="exitApplication"/> is true (the default), this method will call
        /// <see cref="Environment.Exit(int)"/> after launching the updater. Callers should ensure
        /// all resources are properly disposed before calling this method, or set
        /// <paramref name="exitApplication"/> to false and handle application shutdown manually.
        /// </para>
        /// </remarks>
        /// <param name="updaterPath">
        /// The full path to the updater executable (SoftwareUpdate.Updater.exe).
        /// Must be an existing file.
        /// </param>
        /// <param name="zipPath">
        /// The full path to the downloaded update zip file containing the new application files.
        /// Must be an existing file.
        /// </param>
        /// <param name="targetDirectory">
        /// The target installation directory where the update will be extracted.
        /// This is typically the application's installation folder.
        /// </param>
        /// <param name="mainExecutable">
        /// The name of the main executable file to restart after the update completes.
        /// This should be just the filename, not a full path (e.g., "MyApp.exe").
        /// </param>
        /// <param name="createBackup">
        /// If true, the updater will create a backup of the current installation before applying the update.
        /// Defaults to true.
        /// </param>
        /// <param name="exitApplication">
        /// If true, this method will call <see cref="Environment.Exit(int)"/> after launching the updater.
        /// If false, the caller is responsible for terminating the application. Defaults to true.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="updaterPath"/>, <paramref name="zipPath"/>,
        /// <paramref name="targetDirectory"/>, or <paramref name="mainExecutable"/> is null or empty.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the updater executable or update zip file does not exist.
        /// </exception>
        public static void LaunchUpdater(
            string updaterPath,
            string zipPath,
            string targetDirectory,
            string mainExecutable,
            bool createBackup = true,
            bool exitApplication = true)
        {
            LaunchUpdater(
                updaterPath,
                zipPath,
                targetDirectory,
                mainExecutable,
                createBackup,
                exitApplication,
                additionalPreservedRelativePaths: null);
        }

        /// <summary>
        /// Launches the updater while preserving additional installation-relative paths.
        /// </summary>
        /// <param name="updaterPath">The full path to the updater executable.</param>
        /// <param name="zipPath">The full path to the downloaded update archive.</param>
        /// <param name="targetDirectory">The installation directory to update.</param>
        /// <param name="mainExecutable">The main executable filename to restart.</param>
        /// <param name="createBackup">Whether to create a targeted backup.</param>
        /// <param name="exitApplication">Whether to exit the current application after launch.</param>
        /// <param name="additionalPreservedRelativePaths">
        /// Optional installation-relative paths that the updater must preserve.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when a required path or name is empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the updater or update archive is missing.</exception>
        public static void LaunchUpdater(
            string updaterPath,
            string zipPath,
            string targetDirectory,
            string mainExecutable,
            bool createBackup,
            bool exitApplication,
            IEnumerable<string>? additionalPreservedRelativePaths)
        {
            if (string.IsNullOrEmpty(updaterPath))
                throw new ArgumentNullException(nameof(updaterPath));
            if (string.IsNullOrEmpty(zipPath))
                throw new ArgumentNullException(nameof(zipPath));
            if (string.IsNullOrEmpty(targetDirectory))
                throw new ArgumentNullException(nameof(targetDirectory));
            if (string.IsNullOrEmpty(mainExecutable))
                throw new ArgumentNullException(nameof(mainExecutable));

            if (!File.Exists(updaterPath))
                throw new FileNotFoundException("Updater executable not found.", updaterPath);
            if (!File.Exists(zipPath))
                throw new FileNotFoundException("Update zip file not found.", zipPath);

            var preservedPaths = new List<string>();
            if (additionalPreservedRelativePaths != null)
            {
                foreach (var preservedPath in additionalPreservedRelativePaths)
                {
                    ValidatePreservedPath(preservedPath);
                    preservedPaths.Add(preservedPath);
                }
            }

            var currentPid = Process.GetCurrentProcess().Id;
            var runnerUpdaterPath = GitHubUpdateService.CreateUpdaterRunner(updaterPath);

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
            startInfo.ArgumentList.Add(zipPath);
            startInfo.ArgumentList.Add("--target");
            startInfo.ArgumentList.Add(targetDirectory);
            startInfo.ArgumentList.Add("--exe");
            startInfo.ArgumentList.Add(mainExecutable);
            if (createBackup)
                startInfo.ArgumentList.Add("--backup");

            foreach (var preservedPath in preservedPaths)
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
                GitHubUpdateService.DeleteUpdaterRunner(Path.GetDirectoryName(runnerUpdaterPath));
                throw;
            }

            if (process == null)
            {
                GitHubUpdateService.DeleteUpdaterRunner(Path.GetDirectoryName(runnerUpdaterPath));
                throw new InvalidOperationException("Failed to start the updater process.");
            }

            if (exitApplication)
            {
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Validates an installation-relative path passed to the updater.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        private static void ValidatePreservedPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.Contains(':'))
                throw new ArgumentException("Preserved paths must be non-empty relative paths.", nameof(path));

            var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || Array.Exists(segments, segment =>
                    segment is "." or ".." ||
                    segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    segment.EndsWith(" ", StringComparison.Ordinal) ||
                    segment.EndsWith(".", StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    "Preserved paths cannot contain current-directory or parent-directory segments.",
                    nameof(path));
            }
        }
    }
}
