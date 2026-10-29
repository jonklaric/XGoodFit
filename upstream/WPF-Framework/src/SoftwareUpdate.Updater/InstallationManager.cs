using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Threading;

namespace SoftwareUpdate.Updater
{
    /// <summary>
    /// Manages validation, staging, installation, rollback, and cleanup of software updates.
    /// </summary>
    internal class InstallationManager
    {
        private const int ProcessExitTimeoutMs = 60 * 1000;
        private const int MaximumArchiveFileCount = 100_000;
        private const long MaximumUncompressedBytes = 2L * 1024 * 1024 * 1024;

        private static readonly string[] BuiltInProtectedRoots =
        {
            "settings",
            "logs",
            "updates_pending"
        };

        private static readonly string[] KnownInstallRoots =
        {
            "libraries",
            "runtimes"
        };

        private readonly UpdaterArguments _args;
        private readonly Action<string> _log;
        private readonly Action<string>? _beforeApplyFile;
        private readonly int _maximumArchiveFileCount;
        private readonly long _maximumUncompressedBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallationManager"/> class.
        /// </summary>
        /// <param name="args">The updater arguments.</param>
        /// <param name="log">The logging action.</param>
        /// <param name="beforeApplyFile">An optional test hook invoked before applying each file.</param>
        /// <param name="maximumArchiveFileCount">The archive file-count limit.</param>
        /// <param name="maximumUncompressedBytes">The archive uncompressed-size limit.</param>
        public InstallationManager(
            UpdaterArguments args,
            Action<string> log,
            Action<string>? beforeApplyFile = null,
            int maximumArchiveFileCount = MaximumArchiveFileCount,
            long maximumUncompressedBytes = MaximumUncompressedBytes)
        {
            _args = args ?? throw new ArgumentNullException(nameof(args));
            _log = log ?? Console.WriteLine;
            _beforeApplyFile = beforeApplyFile;
            _maximumArchiveFileCount = maximumArchiveFileCount > 0
                ? maximumArchiveFileCount
                : throw new ArgumentOutOfRangeException(nameof(maximumArchiveFileCount));
            _maximumUncompressedBytes = maximumUncompressedBytes > 0
                ? maximumUncompressedBytes
                : throw new ArgumentOutOfRangeException(nameof(maximumUncompressedBytes));
        }

        /// <summary>
        /// Executes the complete update transaction.
        /// </summary>
        public void Execute()
        {
            _log("Starting update process...");
            _log($"Target directory: {_args.TargetDirectory}");
            _log($"Update package: {_args.ZipPath}");

            WaitForProcessExit(_args.ProcessId);

            PreparedUpdate? preparedUpdate = null;
            string? backupDirectory = null;
            var transaction = new TransactionState();

            try
            {
                preparedUpdate = PrepareUpdate();

                if (_args.CreateBackup)
                {
                    backupDirectory = CreateBackup(preparedUpdate.Files);
                }

                ApplyUpdate(preparedUpdate.Files, transaction);
                CleanUp(preparedUpdate.StagingDirectory, backupDirectory, success: true);
                RestartApplication();
            }
            catch (Exception ex)
            {
                _log($"ERROR: {ex.Message}");

                try
                {
                    Rollback(preparedUpdate?.Files, transaction, backupDirectory);
                }
                catch (Exception rollbackException)
                {
                    _log($"Failed to roll back update: {rollbackException.Message}");
                    if (!string.IsNullOrEmpty(backupDirectory))
                    {
                        _log($"Backup location: {backupDirectory}");
                    }
                }

                DeleteStagingDirectory(preparedUpdate?.StagingDirectory);
                throw;
            }
        }

        /// <summary>
        /// Waits for the main application process to exit.
        /// </summary>
        /// <param name="processId">The process identifier.</param>
        private void WaitForProcessExit(int processId)
        {
            _log($"Waiting for process {processId} to exit...");
            var processWasRunning = false;

            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    processWasRunning = true;
                    if (!process.WaitForExit(ProcessExitTimeoutMs))
                    {
                        throw new TimeoutException(
                            $"Process {processId} did not exit within {ProcessExitTimeoutMs / 1000} seconds. " +
                            "Update aborted to prevent file corruption from overwriting locked files.");
                    }

                    _log("Process has exited.");
                }
            }
            catch (ArgumentException)
            {
                _log("Process has already exited.");
            }

            if (processWasRunning)
            {
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Preflights and extracts allowed archive entries into an isolated staging directory.
        /// </summary>
        /// <returns>The prepared update manifest.</returns>
        private PreparedUpdate PrepareUpdate()
        {
            _log("Validating update package...");

            var targetDirectory = Path.GetFullPath(_args.TargetDirectory!);
            EnsureNoReparsePoints(targetDirectory, targetDirectory);

            using var archive = ZipFile.OpenRead(_args.ZipPath!);
            var archiveFiles = new List<ArchiveFile>();

            foreach (var entry in archive.Entries)
            {
                var isDirectory = entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
                    entry.FullName.EndsWith("\\", StringComparison.Ordinal);
                var normalizedPath = NormalizeArchivePath(entry.FullName);
                if (isDirectory || string.IsNullOrEmpty(normalizedPath))
                    continue;

                archiveFiles.Add(new ArchiveFile(entry, normalizedPath));
            }

            if (archiveFiles.Count > _maximumArchiveFileCount)
            {
                throw new InvalidDataException(
                    $"Update archive contains more than {_maximumArchiveFileCount:N0} files.");
            }

            var preservedPaths = BuildPreservedPaths();
            var stripPrefix = DetermineWrapperPrefix(
                archiveFiles.Select(file => file.OriginalPath),
                preservedPaths,
                targetDirectory);
            var plans = new List<UpdateFilePlan>();
            long totalUncompressedBytes = 0;

            foreach (var archiveFile in archiveFiles)
            {
                if (IsProtectedPath(archiveFile.OriginalPath, preservedPaths))
                    continue;

                var relativePath = stripPrefix != null &&
                    archiveFile.OriginalPath.StartsWith(stripPrefix, StringComparison.OrdinalIgnoreCase)
                        ? archiveFile.OriginalPath.Substring(stripPrefix.Length)
                        : archiveFile.OriginalPath;

                if (string.IsNullOrEmpty(relativePath) || IsProtectedPath(relativePath, preservedPaths))
                    continue;

                totalUncompressedBytes = checked(totalUncompressedBytes + archiveFile.Entry.Length);
                if (totalUncompressedBytes > _maximumUncompressedBytes)
                {
                    throw new InvalidDataException(
                        $"Update archive exceeds the {_maximumUncompressedBytes:N0}-byte uncompressed limit.");
                }

                var destinationPath = GetContainedPath(targetDirectory, relativePath);
                EnsureNoReparsePoints(targetDirectory, destinationPath);
                ValidateExistingPathShape(targetDirectory, destinationPath, relativePath);

                plans.Add(new UpdateFilePlan(
                    archiveFile.Entry,
                    relativePath,
                    destinationPath,
                    File.Exists(destinationPath)));
            }

            if (plans.Count == 0)
                throw new InvalidDataException("Update archive contains no installable files.");

            ValidateManifestCollisions(plans);

            var stagingRoot = Path.Combine(targetDirectory, "updates_pending");
            EnsureNoReparsePoints(targetDirectory, stagingRoot);
            Directory.CreateDirectory(stagingRoot);

            var stagingDirectory = Path.Combine(stagingRoot, $"extract-{Guid.NewGuid():N}");
            Directory.CreateDirectory(stagingDirectory);

            try
            {
                foreach (var plan in plans)
                {
                    var stagedPath = GetContainedPath(stagingDirectory, plan.RelativePath);
                    var stagedParent = Path.GetDirectoryName(stagedPath);
                    if (!string.IsNullOrEmpty(stagedParent))
                    {
                        Directory.CreateDirectory(stagedParent);
                    }

                    ExtractWithRetry(plan.Entry, stagedPath);
                    plan.StagedPath = stagedPath;
                }
            }
            catch
            {
                DeleteStagingDirectory(stagingDirectory);
                throw;
            }

            _log($"Validated and staged {plans.Count:N0} files.");
            return new PreparedUpdate(stagingDirectory, plans);
        }

        /// <summary>
        /// Normalizes and validates an archive entry path.
        /// </summary>
        /// <param name="entryPath">The raw archive entry path.</param>
        /// <returns>The normalized path.</returns>
        private static string NormalizeArchivePath(string entryPath)
        {
            if (string.IsNullOrWhiteSpace(entryPath))
                return string.Empty;

            var normalized = entryPath.Replace('\\', '/');
            if (Path.IsPathRooted(entryPath) || normalized.StartsWith("/", StringComparison.Ordinal) ||
                normalized.Contains(':'))
            {
                throw new SecurityException($"Unsafe absolute or alternate-stream path in update package: '{entryPath}'.");
            }

            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment is "." or ".."))
            {
                throw new SecurityException(
                    $"Path traversal attack detected in update package entry '{entryPath}'. Update aborted.");
            }

            foreach (var segment in segments)
            {
                if (segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    segment.EndsWith(" ", StringComparison.Ordinal) ||
                    segment.EndsWith(".", StringComparison.Ordinal))
                {
                    throw new SecurityException($"Unsafe file name in update package entry '{entryPath}'.");
                }
            }

            return string.Join("/", segments);
        }

        /// <summary>
        /// Determines whether all archive files share a wrapper directory.
        /// </summary>
        /// <param name="paths">The normalized archive file paths.</param>
        /// <param name="preservedPaths">The protected installation paths.</param>
        /// <param name="targetDirectory">The installation root.</param>
        /// <returns>The wrapper prefix, or <see langword="null"/>.</returns>
        private static string? DetermineWrapperPrefix(
            IEnumerable<string> paths,
            IReadOnlyList<string> preservedPaths,
            string targetDirectory)
        {
            var pathList = paths.ToList();
            if (pathList.Count == 0)
                return null;

            var splitPaths = pathList.Select(path => path.Split('/')).ToList();
            if (splitPaths.Any(segments => segments.Length < 2))
                return null;

            var root = splitPaths[0][0];
            if (KnownInstallRoots.Any(installRoot =>
                    string.Equals(installRoot, root, StringComparison.OrdinalIgnoreCase)) ||
                preservedPaths.Any(path =>
                    string.Equals(path, root, StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)) ||
                File.Exists(Path.Combine(targetDirectory, root)) ||
                Directory.Exists(Path.Combine(targetDirectory, root)))
            {
                return null;
            }

            return splitPaths.All(segments =>
                    string.Equals(segments[0], root, StringComparison.OrdinalIgnoreCase))
                ? root + "/"
                : null;
        }

        /// <summary>
        /// Builds the immutable and caller-supplied protected path list.
        /// </summary>
        /// <returns>Normalized protected paths.</returns>
        private IReadOnlyList<string> BuildPreservedPaths()
        {
            var paths = new List<string>(BuiltInProtectedRoots);
            foreach (var path in _args.PreservedRelativePaths)
            {
                var normalized = NormalizeArchivePath(path).TrimEnd('/');
                if (string.IsNullOrEmpty(normalized))
                    throw new ArgumentException($"Invalid preserved path: '{path}'.", nameof(_args));

                paths.Add(normalized);
            }

            return paths;
        }

        /// <summary>
        /// Determines whether a relative path is protected from update writes.
        /// </summary>
        /// <param name="relativePath">The normalized relative path.</param>
        /// <param name="preservedPaths">The protected paths.</param>
        /// <returns><see langword="true"/> when the path must be preserved.</returns>
        private static bool IsProtectedPath(string relativePath, IReadOnlyList<string> preservedPaths)
        {
            var normalized = relativePath.Replace('\\', '/').Trim('/');
            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Any(segment => segment.StartsWith(".backup_", StringComparison.OrdinalIgnoreCase)))
                return true;

            return preservedPaths.Any(path =>
                string.Equals(normalized, path, StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith(path + "/", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Resolves a relative path and verifies that it stays within the supplied root.
        /// </summary>
        /// <param name="rootDirectory">The containing root directory.</param>
        /// <param name="relativePath">The normalized relative path.</param>
        /// <returns>The contained full path.</returns>
        private static string GetContainedPath(string rootDirectory, string relativePath)
        {
            var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootDirectory));
            var fullPath = Path.GetFullPath(Path.Combine(
                fullRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));

            if (!fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException(
                    $"Path traversal attack detected: '{relativePath}' resolves outside the update root.");
            }

            return fullPath;
        }

        /// <summary>
        /// Rejects duplicate destinations and file-directory conflicts within the manifest.
        /// </summary>
        /// <param name="plans">The update file plans.</param>
        private static void ValidateManifestCollisions(IReadOnlyList<UpdateFilePlan> plans)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var plan in plans)
            {
                if (!paths.Add(plan.RelativePath))
                    throw new InvalidDataException($"Duplicate update destination: '{plan.RelativePath}'.");
            }

            foreach (var path in paths)
            {
                var parent = path;
                while (parent.Contains('/'))
                {
                    parent = parent.Substring(0, parent.LastIndexOf('/'));
                    if (paths.Contains(parent))
                    {
                        throw new InvalidDataException(
                            $"Update archive contains a file-directory collision at '{parent}'.");
                    }
                }
            }
        }

        /// <summary>
        /// Rejects existing file-directory shape conflicts before any writes occur.
        /// </summary>
        /// <param name="targetDirectory">The installation root.</param>
        /// <param name="destinationPath">The planned destination file.</param>
        /// <param name="relativePath">The relative destination path.</param>
        private static void ValidateExistingPathShape(
            string targetDirectory,
            string destinationPath,
            string relativePath)
        {
            if (Directory.Exists(destinationPath))
            {
                throw new InvalidDataException(
                    $"Update file '{relativePath}' conflicts with an existing directory.");
            }

            var parent = Path.GetDirectoryName(destinationPath);
            while (!string.IsNullOrEmpty(parent) &&
                !string.Equals(parent, targetDirectory, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(parent))
                {
                    throw new InvalidDataException(
                        $"Update file '{relativePath}' has an existing file in its directory path.");
                }

                parent = Path.GetDirectoryName(parent);
            }
        }

        /// <summary>
        /// Rejects existing reparse points between an installation root and candidate path.
        /// </summary>
        /// <param name="targetDirectory">The installation root.</param>
        /// <param name="candidatePath">The path to inspect.</param>
        private static void EnsureNoReparsePoints(string targetDirectory, string candidatePath)
        {
            var fullTarget = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetDirectory));
            var fullCandidate = Path.GetFullPath(candidatePath);
            if (!string.Equals(fullCandidate, fullTarget, StringComparison.OrdinalIgnoreCase) &&
                !fullCandidate.StartsWith(fullTarget + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException($"Path '{candidatePath}' is outside the installation directory.");
            }

            var relative = Path.GetRelativePath(fullTarget, fullCandidate);
            var current = fullTarget;
            ThrowIfReparsePoint(current);

            if (relative == ".")
                return;

            foreach (var segment in relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                if (!File.Exists(current) && !Directory.Exists(current))
                    break;

                ThrowIfReparsePoint(current);
            }
        }

        /// <summary>
        /// Throws when an existing file-system entry is a reparse point.
        /// </summary>
        /// <param name="path">The existing path.</param>
        private static void ThrowIfReparsePoint(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new SecurityException($"Update path cannot traverse reparse point '{path}'.");
        }

        /// <summary>
        /// Creates a targeted backup of files that the update will overwrite.
        /// </summary>
        /// <param name="plans">The update manifest.</param>
        /// <returns>The backup directory.</returns>
        private string CreateBackup(IReadOnlyList<UpdateFilePlan> plans)
        {
            _log("Creating targeted backup...");
            var backupDirectory = CreateUniqueBackupDirectory();
            Directory.CreateDirectory(backupDirectory);

            foreach (var plan in plans.Where(plan => plan.ExistedBefore))
            {
                EnsureNoReparsePoints(_args.TargetDirectory!, plan.DestinationPath);
                if (!File.Exists(plan.DestinationPath))
                    throw new IOException($"File disappeared before backup: '{plan.DestinationPath}'.");

                var backupPath = GetContainedPath(backupDirectory, plan.RelativePath);
                var backupParent = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrEmpty(backupParent))
                {
                    Directory.CreateDirectory(backupParent);
                }

                CopyFileWithRetry(plan.DestinationPath, backupPath, overwrite: false);
            }

            _log($"Backup created at: {backupDirectory}");
            return backupDirectory;
        }

        /// <summary>
        /// Creates a collision-free backup directory path.
        /// </summary>
        /// <returns>The backup directory path.</returns>
        private string CreateUniqueBackupDirectory()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var basePath = Path.Combine(_args.TargetDirectory!, $".backup_{timestamp}");
            var candidate = basePath;
            var suffix = 1;

            while (Directory.Exists(candidate))
            {
                candidate = basePath + "_" + suffix++;
            }

            return candidate;
        }

        /// <summary>
        /// Applies staged files to the installation and records rollback state.
        /// </summary>
        /// <param name="plans">The update manifest.</param>
        /// <param name="transaction">The transaction state.</param>
        private void ApplyUpdate(IReadOnlyList<UpdateFilePlan> plans, TransactionState transaction)
        {
            _log("Applying staged update...");

            foreach (var plan in plans)
            {
                _beforeApplyFile?.Invoke(plan.RelativePath);
                EnsureNoReparsePoints(_args.TargetDirectory!, plan.DestinationPath);
                EnsureDestinationDirectory(plan.DestinationPath, transaction);

                if (!plan.ExistedBefore)
                {
                    transaction.CreatedFiles.Add(plan.DestinationPath);
                }

                CopyFileWithRetry(plan.StagedPath!, plan.DestinationPath, overwrite: true);
            }

            _log("Update applied successfully.");
        }

        /// <summary>
        /// Creates missing destination directories and records those introduced by the update.
        /// </summary>
        /// <param name="destinationPath">The destination file path.</param>
        /// <param name="transaction">The transaction state.</param>
        private void EnsureDestinationDirectory(string destinationPath, TransactionState transaction)
        {
            var targetDirectory = Path.GetFullPath(_args.TargetDirectory!);
            var directory = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrEmpty(directory))
                return;

            var missingDirectories = new Stack<string>();
            var current = directory;
            while (!Directory.Exists(current))
            {
                if (File.Exists(current))
                    throw new IOException($"A file blocks creation of update directory '{current}'.");

                missingDirectories.Push(current);
                current = Path.GetDirectoryName(current)
                    ?? throw new IOException($"Cannot resolve parent directory for '{destinationPath}'.");

                if (!current.StartsWith(targetDirectory, StringComparison.OrdinalIgnoreCase))
                    throw new SecurityException($"Destination directory escapes the installation: '{directory}'.");
            }

            while (missingDirectories.Count > 0)
            {
                var createdDirectory = missingDirectories.Pop();
                Directory.CreateDirectory(createdDirectory);
                transaction.CreatedDirectories.Add(createdDirectory);
            }
        }

        /// <summary>
        /// Extracts an archive entry with retry handling.
        /// </summary>
        /// <param name="entry">The archive entry.</param>
        /// <param name="destinationPath">The staging destination.</param>
        /// <param name="maximumRetries">The maximum attempts.</param>
        private void ExtractWithRetry(
            ZipArchiveEntry entry,
            string destinationPath,
            int maximumRetries = 3)
        {
            for (var attempt = 1; attempt <= maximumRetries; attempt++)
            {
                try
                {
                    entry.ExtractToFile(destinationPath, overwrite: false);
                    return;
                }
                catch (IOException) when (attempt < maximumRetries)
                {
                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }
                    Thread.Sleep(250 * attempt);
                }
            }
        }

        /// <summary>
        /// Copies a file with retry handling for transient locks.
        /// </summary>
        /// <param name="sourcePath">The source file.</param>
        /// <param name="destinationPath">The destination file.</param>
        /// <param name="overwrite">Whether an existing destination may be replaced.</param>
        /// <param name="maximumRetries">The maximum attempts.</param>
        private void CopyFileWithRetry(
            string sourcePath,
            string destinationPath,
            bool overwrite,
            int maximumRetries = 3)
        {
            for (var attempt = 1; attempt <= maximumRetries; attempt++)
            {
                try
                {
                    File.Copy(sourcePath, destinationPath, overwrite);
                    return;
                }
                catch (IOException) when (attempt < maximumRetries)
                {
                    if (!overwrite && File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }
                    _log($"File locked, retrying ({attempt}/{maximumRetries}): {Path.GetFileName(destinationPath)}");
                    Thread.Sleep(1000 * attempt);
                }
            }
        }

        /// <summary>
        /// Restores overwritten files and removes files and directories introduced by a failed update.
        /// </summary>
        /// <param name="plans">The update manifest, when preparation completed.</param>
        /// <param name="transaction">The transaction state.</param>
        /// <param name="backupDirectory">The targeted backup directory.</param>
        private void Rollback(
            IReadOnlyList<UpdateFilePlan>? plans,
            TransactionState transaction,
            string? backupDirectory)
        {
            if (plans == null)
                return;

            _log("Rolling back failed update...");
            var preservedPaths = BuildPreservedPaths();
            var targetDirectory = Path.GetFullPath(_args.TargetDirectory!);

            foreach (var file in transaction.CreatedFiles.OrderByDescending(path => path.Length))
            {
                var relativePath = Path.GetRelativePath(targetDirectory, file).Replace('\\', '/');
                if (IsProtectedPath(relativePath, preservedPaths))
                    continue;

                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }

            if (!string.IsNullOrEmpty(backupDirectory) && Directory.Exists(backupDirectory))
            {
                RestoreFromBackup(backupDirectory);
            }

            foreach (var directory in transaction.CreatedDirectories.OrderByDescending(path => path.Length))
            {
                var relativePath = Path.GetRelativePath(targetDirectory, directory).Replace('\\', '/');
                if (!IsProtectedPath(relativePath, preservedPaths) && Directory.Exists(directory) &&
                    Directory.GetFileSystemEntries(directory).Length == 0)
                {
                    Directory.Delete(directory);
                }
            }

            _log("Rollback completed.");
        }

        /// <summary>
        /// Restores all files from a targeted backup directory.
        /// </summary>
        /// <param name="backupDirectory">The backup directory.</param>
        private void RestoreFromBackup(string backupDirectory)
        {
            var targetDirectory = Path.GetFullPath(_args.TargetDirectory!);
            foreach (var backupFile in EnumerateFilesWithoutReparsePoints(backupDirectory))
            {
                var relativePath = Path.GetRelativePath(backupDirectory, backupFile).Replace('\\', '/');
                var destinationPath = GetContainedPath(targetDirectory, relativePath);
                EnsureNoReparsePoints(targetDirectory, destinationPath);

                var destinationParent = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationParent))
                {
                    Directory.CreateDirectory(destinationParent);
                }

                CopyFileWithRetry(backupFile, destinationPath, overwrite: true);
            }
        }

        /// <summary>
        /// Enumerates files recursively while refusing file and directory reparse points.
        /// </summary>
        /// <param name="directory">The directory to enumerate.</param>
        /// <returns>The contained regular files.</returns>
        private static IEnumerable<string> EnumerateFilesWithoutReparsePoints(string directory)
        {
            ThrowIfReparsePoint(directory);

            foreach (var file in Directory.GetFiles(directory))
            {
                ThrowIfReparsePoint(file);
                yield return file;
            }

            foreach (var childDirectory in Directory.GetDirectories(directory))
            {
                ThrowIfReparsePoint(childDirectory);
                foreach (var file in EnumerateFilesWithoutReparsePoints(childDirectory))
                {
                    yield return file;
                }
            }
        }

        /// <summary>
        /// Removes update staging data and rotates successful backups.
        /// </summary>
        /// <param name="stagingDirectory">The extraction staging directory.</param>
        /// <param name="backupDirectory">The current backup directory.</param>
        /// <param name="success">Whether installation succeeded.</param>
        private void CleanUp(string stagingDirectory, string? backupDirectory, bool success)
        {
            _log("Cleaning up...");
            DeleteStagingDirectory(stagingDirectory);

            try
            {
                if (File.Exists(_args.ZipPath))
                {
                    File.Delete(_args.ZipPath);
                    _log("Deleted update package.");
                }

                DeleteEmptyPackageDirectory();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                _log($"Warning: Could not delete update package: {ex.Message}");
            }

            if (!success || string.IsNullOrEmpty(backupDirectory))
                return;

            try
            {
                var oldBackups = Directory.GetDirectories(_args.TargetDirectory!, ".backup_*")
                    .Select(path => new { Path = path, Timestamp = ParseBackupTimestamp(path) })
                    .Where(backup => backup.Timestamp.HasValue)
                    .OrderByDescending(backup => backup.Timestamp!.Value)
                    .Skip(2)
                    .Select(backup => backup.Path);

                foreach (var oldBackup in oldBackups)
                {
                    DeleteDirectoryWithoutFollowingReparsePoints(oldBackup);
                    _log($"Deleted old backup: {Path.GetFileName(oldBackup)}");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                _log($"Warning: Could not clean old backups: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes an empty updater-owned package directory.
        /// </summary>
        private void DeleteEmptyPackageDirectory()
        {
            var packageDirectory = Path.GetDirectoryName(_args.ZipPath);
            if (string.IsNullOrEmpty(packageDirectory) || !Directory.Exists(packageDirectory) ||
                Directory.GetFileSystemEntries(packageDirectory).Length != 0)
            {
                return;
            }

            var fullPackageDirectory = Path.GetFullPath(packageDirectory);
            var targetPendingDirectory = Path.GetFullPath(Path.Combine(_args.TargetDirectory!, "updates_pending"));
            if (string.Equals(fullPackageDirectory, targetPendingDirectory, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Delete(fullPackageDirectory);
                return;
            }

            var tempRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SoftwareUpdate"));
            var parent = Path.GetDirectoryName(fullPackageDirectory);
            if (parent != null && string.Equals(Path.GetFullPath(parent), tempRoot, StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParseExact(Path.GetFileName(fullPackageDirectory), "N", out _))
            {
                Directory.Delete(fullPackageDirectory);
            }
        }

        /// <summary>
        /// Best-effort deletion of an updater-owned extraction staging directory.
        /// </summary>
        /// <param name="stagingDirectory">The staging directory.</param>
        private void DeleteStagingDirectory(string? stagingDirectory)
        {
            if (string.IsNullOrEmpty(stagingDirectory) || !Directory.Exists(stagingDirectory))
                return;

            try
            {
                var fullStagingDirectory = Path.GetFullPath(stagingDirectory);
                var pendingDirectory = Path.GetFullPath(Path.Combine(_args.TargetDirectory!, "updates_pending"));
                var parent = Path.GetDirectoryName(fullStagingDirectory);
                var name = Path.GetFileName(fullStagingDirectory);

                if (parent == null || !string.Equals(parent, pendingDirectory, StringComparison.OrdinalIgnoreCase) ||
                    !name.StartsWith("extract-", StringComparison.OrdinalIgnoreCase) ||
                    !Guid.TryParseExact(name.Substring("extract-".Length), "N", out _))
                {
                    return;
                }

                DeleteDirectoryWithoutFollowingReparsePoints(fullStagingDirectory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                _log($"Warning: Could not delete extraction staging directory: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a directory tree without traversing directory reparse points.
        /// </summary>
        /// <param name="directory">The directory to delete.</param>
        private static void DeleteDirectoryWithoutFollowingReparsePoints(string directory)
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                File.Delete(file);
            }

            foreach (var childDirectory in Directory.GetDirectories(directory))
            {
                var attributes = File.GetAttributes(childDirectory);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    Directory.Delete(childDirectory);
                }
                else
                {
                    DeleteDirectoryWithoutFollowingReparsePoints(childDirectory);
                }
            }

            Directory.Delete(directory);
        }

        /// <summary>
        /// Restarts the main application after a successful update.
        /// </summary>
        private void RestartApplication()
        {
            _log("Restarting application...");
            var safeExecutable = Path.GetFileName(_args.MainExecutable);
            var executablePath = Path.Combine(_args.TargetDirectory!, safeExecutable!);

            if (!File.Exists(executablePath))
            {
                _log($"Warning: Main executable not found at {executablePath}");
                _log("Update completed. Please restart the application manually.");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = _args.TargetDirectory,
                UseShellExecute = true
            };

            try
            {
                var started = Process.Start(startInfo);
                if (started == null)
                {
                    _log($"Warning: Could not restart {executablePath}. Please launch it manually.");
                    return;
                }

                started.Dispose();
                _log("Application restarted.");
            }
            catch (Exception ex)
            {
                _log($"Error: Failed to restart application at {executablePath}: {ex.Message}. " +
                    "The update was applied; please launch the application manually.");
            }
        }

        /// <summary>
        /// Recursively copies a directory while refusing reparse points.
        /// </summary>
        /// <param name="sourceDirectory">The source directory.</param>
        /// <param name="destinationDirectory">The destination directory.</param>
        private void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            ThrowIfReparsePoint(sourceDirectory);
            Directory.CreateDirectory(destinationDirectory);

            foreach (var file in Directory.GetFiles(sourceDirectory))
            {
                ThrowIfReparsePoint(file);
                File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), overwrite: true);
            }

            foreach (var childDirectory in Directory.GetDirectories(sourceDirectory))
            {
                CopyDirectory(
                    childDirectory,
                    Path.Combine(destinationDirectory, Path.GetFileName(childDirectory)));
            }
        }

        /// <summary>
        /// Parses the timestamp from a backup directory name.
        /// </summary>
        /// <param name="backupPath">The backup directory path.</param>
        /// <returns>The parsed timestamp, or <see langword="null"/>.</returns>
        private static DateTime? ParseBackupTimestamp(string backupPath)
        {
            var directoryName = Path.GetFileName(backupPath);
            if (string.IsNullOrEmpty(directoryName) ||
                !directoryName.StartsWith(".backup_", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var timestampPart = directoryName.Substring(".backup_".Length);
            if (timestampPart.Length > 15 && timestampPart[15] == '_')
            {
                timestampPart = timestampPart.Substring(0, 15);
            }

            return DateTime.TryParseExact(
                timestampPart,
                "yyyyMMdd_HHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var timestamp)
                ? timestamp
                : null;
        }

        /// <summary>
        /// Associates an archive entry with its normalized original path.
        /// </summary>
        private sealed class ArchiveFile
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ArchiveFile"/> class.
            /// </summary>
            /// <param name="entry">The archive entry.</param>
            /// <param name="originalPath">The normalized path.</param>
            public ArchiveFile(ZipArchiveEntry entry, string originalPath)
            {
                Entry = entry;
                OriginalPath = originalPath;
            }

            /// <summary>Gets the archive entry.</summary>
            public ZipArchiveEntry Entry { get; }

            /// <summary>Gets the normalized original path.</summary>
            public string OriginalPath { get; }
        }

        /// <summary>
        /// Describes one file in the prepared update manifest.
        /// </summary>
        private sealed class UpdateFilePlan
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="UpdateFilePlan"/> class.
            /// </summary>
            /// <param name="entry">The archive entry.</param>
            /// <param name="relativePath">The destination-relative path.</param>
            /// <param name="destinationPath">The full destination path.</param>
            /// <param name="existedBefore">Whether the destination existed before application.</param>
            public UpdateFilePlan(
                ZipArchiveEntry entry,
                string relativePath,
                string destinationPath,
                bool existedBefore)
            {
                Entry = entry;
                RelativePath = relativePath;
                DestinationPath = destinationPath;
                ExistedBefore = existedBefore;
            }

            /// <summary>Gets the archive entry.</summary>
            public ZipArchiveEntry Entry { get; }

            /// <summary>Gets the destination-relative path.</summary>
            public string RelativePath { get; }

            /// <summary>Gets the full destination path.</summary>
            public string DestinationPath { get; }

            /// <summary>Gets whether the destination existed before installation.</summary>
            public bool ExistedBefore { get; }

            /// <summary>Gets or sets the staged source path.</summary>
            public string? StagedPath { get; set; }
        }

        /// <summary>
        /// Holds the staged directory and prepared file manifest.
        /// </summary>
        private sealed class PreparedUpdate
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PreparedUpdate"/> class.
            /// </summary>
            /// <param name="stagingDirectory">The staging directory.</param>
            /// <param name="files">The prepared files.</param>
            public PreparedUpdate(string stagingDirectory, IReadOnlyList<UpdateFilePlan> files)
            {
                StagingDirectory = stagingDirectory;
                Files = files;
            }

            /// <summary>Gets the staging directory.</summary>
            public string StagingDirectory { get; }

            /// <summary>Gets the prepared update files.</summary>
            public IReadOnlyList<UpdateFilePlan> Files { get; }
        }

        /// <summary>
        /// Tracks installation entries that must be removed during rollback.
        /// </summary>
        private sealed class TransactionState
        {
            /// <summary>Gets files introduced by the update.</summary>
            public HashSet<string> CreatedFiles { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>Gets directories introduced by the update.</summary>
            public HashSet<string> CreatedDirectories { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
