using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareUpdate.Updater
{
    /// <summary>
    /// Parses and holds command-line arguments for the updater.
    /// </summary>
    internal class UpdaterArguments
    {
        /// <summary>
        /// Gets or sets the process ID of the main application to wait for.
        /// </summary>
        /// <value>The process ID. Defaults to 0 if not specified.</value>
        public int ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the path to the update zip file.
        /// </summary>
        /// <value>The zip file path, or <c>null</c> if not specified. Required by <see cref="Validate"/>.</value>
        public string? ZipPath { get; set; }

        /// <summary>
        /// Gets or sets the target installation directory.
        /// </summary>
        /// <value>The target directory path, or <c>null</c> if not specified. Required by <see cref="Validate"/>.</value>
        public string? TargetDirectory { get; set; }

        /// <summary>
        /// Gets or sets the main executable name to restart.
        /// </summary>
        /// <value>The executable name, or <c>null</c> if not specified. Required by <see cref="Validate"/>.</value>
        public string? MainExecutable { get; set; }

        /// <summary>
        /// Gets or sets whether to create a backup before updating.
        /// </summary>
        public bool CreateBackup { get; set; }

        /// <summary>
        /// Gets additional installation-relative paths that must be preserved.
        /// </summary>
        public IList<string> PreservedRelativePaths { get; } = new List<string>();

        /// <summary>
        /// Parses command-line arguments.
        /// </summary>
        /// <param name="args">The command-line arguments array.</param>
        /// <returns>Parsed updater arguments.</returns>
        public static UpdaterArguments Parse(string[] args)
        {
            var result = new UpdaterArguments();
            var argDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.StartsWith("--") && arg.Length > 2)
                {
                    var key = arg.Substring(2).ToLower();

                    // Check for flags without values
                    if (key == "backup")
                    {
                        result.CreateBackup = true;
                        continue;
                    }

                    if (key == "preserve")
                    {
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                        {
                            result.PreservedRelativePaths.Add(UnquoteArgument(args[++i]));
                        }
                        continue;
                    }

                    // Get the value
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        argDict[key] = args[++i];
                    }
                }
            }

            // Parse values
            if (argDict.TryGetValue("pid", out var pidStr) && int.TryParse(pidStr, out var pid))
            {
                result.ProcessId = pid;
            }

            if (argDict.TryGetValue("zip", out var zip))
            {
                result.ZipPath = UnquoteArgument(zip);
            }

            if (argDict.TryGetValue("target", out var target))
            {
                result.TargetDirectory = UnquoteArgument(target);
            }

            if (argDict.TryGetValue("exe", out var exe))
            {
                result.MainExecutable = UnquoteArgument(exe);
            }

            return result;
        }

        /// <summary>
        /// Validates the arguments.
        /// </summary>
        /// <remarks>
        /// Note: File existence checks are point-in-time validations. The zip file or target directory
        /// could be modified or deleted between validation and actual use (TOCTOU). Callers should
        /// handle <see cref="System.IO.FileNotFoundException"/> during installation.
        /// </remarks>
        public void Validate()
        {
            var errors = new List<string>();

            if (ProcessId <= 0)
                errors.Add("--pid is required and must be a valid process ID");

            if (string.IsNullOrEmpty(ZipPath))
            {
                errors.Add("--zip is required");
            }
            else if (!System.IO.File.Exists(ZipPath))
            {
                // The updater waits up to 60 seconds for the parent process to exit before it
                // does anything. During that window the zip can disappear: AV scanners, Windows
                // %TEMP% cleanup, OneDrive sync, or even the user manually deleting it can wipe
                // the file before we ever get to extraction. Surface a clearer message so support
                // logs (and the user) understand what happened instead of seeing only the bare
                // path. The fix is informational; the underlying race is unchanged here. The
                // larger-touch remediation is to copy the zip into the install dir before the
                // parent exits — see GitHubUpdateService.InstallUpdateAndRestart for the staging
                // approach.
                errors.Add(
                    $"Zip file not found at the expected path: {ZipPath}. " +
                    "It may have been removed by anti-virus, %TEMP% cleanup, or another process " +
                    "between download and the parent application's exit. Try the update again.");
            }

            if (string.IsNullOrEmpty(TargetDirectory))
                errors.Add("--target is required");
            else if (!System.IO.Directory.Exists(TargetDirectory))
                errors.Add($"Target directory not found: {TargetDirectory}");

            if (string.IsNullOrEmpty(MainExecutable))
                errors.Add("--exe is required");

            foreach (var preservedPath in PreservedRelativePaths)
            {
                if (!IsValidRelativePath(preservedPath))
                {
                    errors.Add($"--preserve must be a safe relative path: {preservedPath}");
                }
            }

            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join(Environment.NewLine, errors));
            }
        }

        /// <summary>
        /// Properly unquotes an argument value, handling quoted strings safely.
        /// </summary>
        /// <param name="value">The argument value to unquote.</param>
        /// <returns>The unquoted value.</returns>
        private static string UnquoteArgument(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Only strip quotes if they are balanced at start and end
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                return value.Substring(1, value.Length - 2);
            }

            return value;
        }

        /// <summary>
        /// Determines whether a preserved path is relative and traversal-free.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns><see langword="true"/> when the path is safe.</returns>
        private static bool IsValidRelativePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || System.IO.Path.IsPathRooted(path) || path.Contains(':'))
                return false;

            var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0 && !segments.Any(segment =>
                segment is "." or ".." ||
                segment.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0 ||
                segment.EndsWith(" ", StringComparison.Ordinal) ||
                segment.EndsWith(".", StringComparison.Ordinal));
        }
    }
}
