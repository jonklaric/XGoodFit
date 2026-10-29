using Xunit;
using SoftwareUpdate.Updater;
using System.IO.Compression;
using System.Reflection;
using System.Security;

namespace SoftwareUpdate.Updater.Tests
{
    /// <summary>
    /// Test suite for the InstallationManager class, validating update installation, backup, and restore functionality.
    /// Implements IDisposable to ensure proper cleanup of test directories.
    /// </summary>
    public class InstallationManagerTests : IDisposable
    {
        /// <summary>
        /// The temporary directory path used for all test file operations. Created uniquely for each test instance.
        /// </summary>
        private readonly string _testDir;

        /// <summary>
        /// Collection of log messages captured during test execution for verification purposes.
        /// </summary>
        private readonly List<string> _logs = new();

        /// <summary>
        /// Initializes a new instance of the InstallationManagerTests class.
        /// Creates a unique temporary directory for test isolation.
        /// </summary>
        public InstallationManagerTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "InstallManagerTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
        }

        /// <summary>
        /// Performs cleanup operations after test execution.
        /// Deletes the temporary test directory and all its contents.
        /// </summary>
        public void Dispose()
        {
            // Clean up test directory
            if (Directory.Exists(_testDir))
            {
                try
                {
                    Directory.Delete(_testDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup failures in tests
                }
            }
        }

        /// <summary>
        /// Logs a message to the internal log collection for test verification.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void Log(string message) => _logs.Add(message);

        #region Constructor Tests

        /// <summary>
        /// Verifies that the InstallationManager constructor throws ArgumentNullException when args parameter is null.
        /// </summary>
        /// <exception cref="ArgumentNullException">Expected exception when args is null.</exception>
        [Fact]
        public void Constructor_NullArgs_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new InstallationManager(null!, Log));
        }

        /// <summary>
        /// Verifies that the InstallationManager constructor accepts a null log action without throwing.
        /// </summary>
        [Fact]
        public void Constructor_NullLog_DoesNotThrow()
        {
            var args = new UpdaterArguments
            {
                ProcessId = 1,
                ZipPath = "test.zip",
                TargetDirectory = _testDir,
                MainExecutable = "test.exe"
            };

            var exception = Record.Exception(() => new InstallationManager(args, null!));

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that the InstallationManager constructor successfully creates an instance with valid arguments.
        /// </summary>
        [Fact]
        public void Constructor_ValidArgs_CreatesInstance()
        {
            var args = new UpdaterArguments
            {
                ProcessId = 1,
                ZipPath = "test.zip",
                TargetDirectory = _testDir,
                MainExecutable = "test.exe"
            };

            var manager = new InstallationManager(args, Log);

            Assert.NotNull(manager);
        }

        #endregion

        #region ParseBackupTimestamp Tests (via Reflection)

        /// <summary>
        /// Verifies that ParseBackupTimestamp correctly parses backup directory names with valid timestamp format.
        /// </summary>
        /// <param name="dirName">The backup directory name to parse.</param>
        /// <param name="year">Expected year value.</param>
        /// <param name="month">Expected month value.</param>
        /// <param name="day">Expected day value.</param>
        /// <param name="hour">Expected hour value.</param>
        /// <param name="minute">Expected minute value.</param>
        /// <param name="second">Expected second value.</param>
        [Theory]
        [InlineData(".backup_20240115_120000", 2024, 1, 15, 12, 0, 0)]
        [InlineData(".backup_20231225_235959", 2023, 12, 25, 23, 59, 59)]
        [InlineData(".backup_20240101_000000", 2024, 1, 1, 0, 0, 0)]
        public void ParseBackupTimestamp_ValidFormat_ReturnsDateTime(
            string dirName, int year, int month, int day, int hour, int minute, int second)
        {
            var result = InvokeParseBackupTimestamp(dirName);

            Assert.NotNull(result);
            Assert.Equal(new DateTime(year, month, day, hour, minute, second), result.Value);
        }

        /// <summary>
        /// Verifies that ParseBackupTimestamp correctly handles backup directories with numeric suffixes.
        /// </summary>
        /// <param name="dirName">The backup directory name to parse.</param>
        /// <param name="year">Expected year value.</param>
        /// <param name="month">Expected month value.</param>
        /// <param name="day">Expected day value.</param>
        /// <param name="hour">Expected hour value.</param>
        /// <param name="minute">Expected minute value.</param>
        /// <param name="second">Expected second value.</param>
        [Theory]
        [InlineData(".backup_20240115_120000_1", 2024, 1, 15, 12, 0, 0)]
        [InlineData(".backup_20240115_120000_99", 2024, 1, 15, 12, 0, 0)]
        public void ParseBackupTimestamp_WithSuffix_ReturnsDateTime(
            string dirName, int year, int month, int day, int hour, int minute, int second)
        {
            var result = InvokeParseBackupTimestamp(dirName);

            Assert.NotNull(result);
            Assert.Equal(new DateTime(year, month, day, hour, minute, second), result.Value);
        }

        /// <summary>
        /// Verifies that ParseBackupTimestamp returns null for invalid or malformed backup directory names.
        /// </summary>
        /// <param name="dirName">The invalid backup directory name to parse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("backup_20240115_120000")]
        [InlineData(".backup_")]
        [InlineData(".backup_invalid")]
        [InlineData(".backup_2024")]
        [InlineData(".backup_20241315_120000")] // Invalid month 13
        public void ParseBackupTimestamp_InvalidFormat_ReturnsNull(string dirName)
        {
            var result = InvokeParseBackupTimestamp(dirName);

            Assert.Null(result);
        }

        /// <summary>
        /// Verifies that ParseBackupTimestamp returns null when given a null path.
        /// </summary>
        [Fact]
        public void ParseBackupTimestamp_NullPath_ReturnsNull()
        {
            var result = InvokeParseBackupTimestamp(null!);

            Assert.Null(result);
        }

        /// <summary>
        /// Helper method to invoke the private static ParseBackupTimestamp method via reflection.
        /// </summary>
        /// <param name="backupPath">The backup directory path to parse.</param>
        /// <returns>The parsed DateTime if successful; otherwise, null.</returns>
        private static DateTime? InvokeParseBackupTimestamp(string backupPath)
        {
            var method = typeof(InstallationManager)
                .GetMethod("ParseBackupTimestamp", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return (DateTime?)method!.Invoke(null, new object[] { backupPath });
        }

        #endregion

        #region CreateBackup Tests (Integration)

        /// <summary>
        /// Verifies that the CreateBackup functionality creates a backup directory with all target files.
        /// </summary>
        [Fact]
        public void CreateBackup_CreatesBackupDirectory()
        {
            var targetDir = CreateSubDir("target");
            var zipPath = CreateTestZip("update.zip", new[]
            {
                ("existing.txt", "updated"),
                ("subdir/nested.txt", "updated nested")
            });

            // Create some files in target directory
            File.WriteAllText(Path.Combine(targetDir, "existing.txt"), "original");
            Directory.CreateDirectory(Path.Combine(targetDir, "subdir"));
            File.WriteAllText(Path.Combine(targetDir, "subdir", "nested.txt"), "nested");

            var args = new UpdaterArguments
            {
                ProcessId = -1, // Invalid PID - process won't be found (fast)
                ZipPath = zipPath,
                TargetDirectory = targetDir,
                MainExecutable = "test.exe",
                CreateBackup = true
            };

            var manager = new InstallationManager(args, Log);

            // Execute will fail after backup is created (process not found)
            try { manager.Execute(); } catch { }

            // Check backup was created
            var backupDirs = Directory.GetDirectories(targetDir, ".backup_*");
            Assert.NotEmpty(backupDirs);

            // Check backup contains only files targeted for replacement
            var backupDir = backupDirs[0];
            Assert.True(File.Exists(Path.Combine(backupDir, "existing.txt")));
            Assert.True(File.Exists(Path.Combine(backupDir, "subdir", "nested.txt")));
            Assert.Equal("original", File.ReadAllText(Path.Combine(backupDir, "existing.txt")));
            Assert.Equal("nested", File.ReadAllText(Path.Combine(backupDir, "subdir", "nested.txt")));
        }

        /// <summary>
        /// Verifies that CreateBackup skips existing backup directories and does not include them in new backups.
        /// </summary>
        [Fact]
        public void CreateBackup_SkipsExistingBackupDirectories()
        {
            var targetDir = CreateSubDir("target");
            var zipPath = CreateTestZip("update.zip", new[] { ("current.txt", "updated") });

            // Create an existing backup directory
            var existingBackup = Path.Combine(targetDir, ".backup_20240101_000000");
            Directory.CreateDirectory(existingBackup);
            File.WriteAllText(Path.Combine(existingBackup, "old.txt"), "old backup");

            // Create a new file to backup
            File.WriteAllText(Path.Combine(targetDir, "current.txt"), "current");

            var args = new UpdaterArguments
            {
                ProcessId = -1, // Invalid PID - process won't be found (fast)
                ZipPath = zipPath,
                TargetDirectory = targetDir,
                MainExecutable = "test.exe",
                CreateBackup = true
            };

            var manager = new InstallationManager(args, Log);
            try { manager.Execute(); } catch { }

            // Find the new backup (not the existing one)
            var backupDirs = Directory.GetDirectories(targetDir, ".backup_*")
                .Where(d => d != existingBackup)
                .ToList();

            Assert.Single(backupDirs);

            // The new backup should NOT contain the old backup directory
            var newBackup = backupDirs[0];
            Assert.False(Directory.Exists(Path.Combine(newBackup, ".backup_20240101_000000")));
            Assert.True(File.Exists(Path.Combine(newBackup, "current.txt")));
        }

        /// <summary>
        /// Verifies that protected settings are neither overwritten nor copied into a targeted backup.
        /// </summary>
        [Fact]
        public void CreateBackup_ExcludesProtectedSettings()
        {
            var targetDir = CreateSubDir("target");
            var settingsDir = Directory.CreateDirectory(Path.Combine(targetDir, "settings")).FullName;
            File.WriteAllText(Path.Combine(settingsDir, "UserSettings.xml"), "original-settings");
            File.WriteAllText(Path.Combine(targetDir, "app.dll"), "original-app");
            var zipPath = CreateTestZip("settings-backup.zip", new[]
            {
                ("settings/UserSettings.xml", "malicious-settings"),
                ("app.dll", "updated-app")
            });
            var arguments = CreateArguments(zipPath, targetDir);
            arguments.CreateBackup = true;

            new InstallationManager(arguments, Log).Execute();

            var backupDir = Assert.Single(Directory.GetDirectories(targetDir, ".backup_*"));
            Assert.Equal("original-settings", File.ReadAllText(Path.Combine(settingsDir, "UserSettings.xml")));
            Assert.False(Directory.Exists(Path.Combine(backupDir, "settings")));
            Assert.Equal("original-app", File.ReadAllText(Path.Combine(backupDir, "app.dll")));
        }

        #endregion

        #region ExtractUpdate Path Traversal Tests

        /// <summary>
        /// Verifies that ExtractUpdate protects against path traversal attacks by throwing a SecurityException.
        /// </summary>
        [Fact]
        public void ExtractUpdate_PathTraversalAttempt_ThrowsSecurityException()
        {
            var targetDir = CreateSubDir("target");
            var parentFile = Path.Combine(_testDir, "escaped.txt");

            // Create a zip with path traversal attempt
            var zipPath = CreatePathTraversalZip("malicious.zip");

            var args = new UpdaterArguments
            {
                ProcessId = -1, // Invalid PID - process won't be found
                ZipPath = zipPath,
                TargetDirectory = targetDir,
                MainExecutable = "test.exe",
                CreateBackup = false
            };

            var manager = new InstallationManager(args, Log);
            var ex = Assert.Throws<SecurityException>(() => manager.Execute());

            // The escaped file should NOT exist outside target directory
            Assert.False(File.Exists(parentFile));

            // Verify the exception message indicates path traversal was detected
            Assert.Contains("Path traversal", ex.Message);
        }

        /// <summary>
        /// Verifies that ExtractUpdate correctly extracts files with normal, safe paths.
        /// </summary>
        [Fact]
        public void ExtractUpdate_NormalPaths_ExtractsCorrectly()
        {
            var targetDir = CreateSubDir("target");
            var zipPath = CreateTestZip("update.zip", new[]
            {
                ("file.txt", "root file"),
                ("subdir/nested.txt", "nested file"),
                ("subdir/deep/file.txt", "deep file")
            });

            var args = new UpdaterArguments
            {
                ProcessId = -1, // Invalid PID - process won't be found
                ZipPath = zipPath,
                TargetDirectory = targetDir,
                MainExecutable = "test.exe",
                CreateBackup = false
            };

            var manager = new InstallationManager(args, Log);
            try { manager.Execute(); } catch { }

            // Verify files were extracted
            Assert.True(File.Exists(Path.Combine(targetDir, "file.txt")));
            Assert.True(File.Exists(Path.Combine(targetDir, "subdir", "nested.txt")));
            Assert.True(File.Exists(Path.Combine(targetDir, "subdir", "deep", "file.txt")));
        }

        /// <summary>
        /// Verifies that ExtractUpdate strips a single root folder from archive entries (common in GitHub releases).
        /// </summary>
        [Fact]
        public void ExtractUpdate_SingleRootFolder_StripsRoot()
        {
            var targetDir = CreateSubDir("target");

            // Create a zip with single root folder (common for GitHub releases)
            var zipPath = CreateTestZip("release.zip", new[]
            {
                ("MyApp-v1.0.0/app.exe", "executable"),
                ("MyApp-v1.0.0/data/config.json", "config")
            });

            var args = new UpdaterArguments
            {
                ProcessId = -1,
                ZipPath = zipPath,
                TargetDirectory = targetDir,
                MainExecutable = "app.exe",
                CreateBackup = false
            };

            var manager = new InstallationManager(args, Log);
            try { manager.Execute(); } catch { }

            // Files should be at root of target, not in MyApp-v1.0.0 subfolder
            Assert.True(File.Exists(Path.Combine(targetDir, "app.exe")));
            Assert.True(File.Exists(Path.Combine(targetDir, "data", "config.json")));
            Assert.False(Directory.Exists(Path.Combine(targetDir, "MyApp-v1.0.0")));
        }

        /// <summary>
        /// Verifies that a known installation root is not mistaken for a release wrapper.
        /// </summary>
        [Fact]
        public void ExtractUpdate_SingleKnownInstallRoot_DoesNotFlattenFiles()
        {
            var targetDir = CreateSubDir("target");
            var zipPath = CreateTestZip("libraries-only.zip", new[]
            {
                ("libraries/dependency.dll", "dependency")
            });

            new InstallationManager(CreateArguments(zipPath, targetDir), Log).Execute();

            Assert.True(File.Exists(Path.Combine(targetDir, "libraries", "dependency.dll")));
            Assert.False(File.Exists(Path.Combine(targetDir, "dependency.dll")));
        }

        /// <summary>
        /// Verifies that an existing installation directory is not mistaken for a release wrapper.
        /// </summary>
        [Fact]
        public void ExtractUpdate_SingleExistingInstallRoot_DoesNotFlattenFiles()
        {
            var targetDir = CreateSubDir("target");
            Directory.CreateDirectory(Path.Combine(targetDir, "plugins"));
            var zipPath = CreateTestZip("plugins-only.zip", new[]
            {
                ("plugins/new-plugin.dll", "plugin")
            });

            new InstallationManager(CreateArguments(zipPath, targetDir), Log).Execute();

            Assert.True(File.Exists(Path.Combine(targetDir, "plugins", "new-plugin.dll")));
            Assert.False(File.Exists(Path.Combine(targetDir, "new-plugin.dll")));
        }

        /// <summary>
        /// Verifies that ExtractUpdate skips backup directories found within update archives.
        /// </summary>
        [Fact]
        public void ExtractUpdate_SkipsBackupDirectories()
        {
            var targetDir = CreateSubDir("target");

            // Create a zip that somehow contains backup directories
            var zipPath = CreateTestZip("update.zip", new[]
            {
                ("file.txt", "content"),
                (".backup_old/file.txt", "should be skipped"),
                ("subdir/.backup_test/file.txt", "also skipped")
            });

            var args = new UpdaterArguments
            {
                ProcessId = -1,
                ZipPath = zipPath,
                TargetDirectory = targetDir,
                MainExecutable = "test.exe",
                CreateBackup = false
            };

            var manager = new InstallationManager(args, Log);
            try { manager.Execute(); } catch { }

            // Normal file should exist
            Assert.True(File.Exists(Path.Combine(targetDir, "file.txt")));

            // Backup directories should not be created
            Assert.False(Directory.Exists(Path.Combine(targetDir, ".backup_old")));
            Assert.False(Directory.Exists(Path.Combine(targetDir, "subdir", ".backup_test")));
        }

        #endregion

        #region Hardened Update Transaction Tests

        /// <summary>
        /// Verifies that built-in runtime paths remain unchanged for direct and wrapped archives.
        /// </summary>
        /// <param name="wrapped">Whether entries are nested beneath a release wrapper.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_BuiltInProtectedPaths_AreNeverOverwritten(bool wrapped)
        {
            var targetDir = CreateSubDir("target");
            var settingsDir = Directory.CreateDirectory(Path.Combine(targetDir, "settings")).FullName;
            var logsDir = Directory.CreateDirectory(Path.Combine(targetDir, "logs")).FullName;
            var pendingDir = Directory.CreateDirectory(Path.Combine(targetDir, "updates_pending")).FullName;
            var backupDir = Directory.CreateDirectory(Path.Combine(targetDir, ".backup_user")).FullName;
            File.WriteAllText(Path.Combine(settingsDir, "UserSettings.xml"), "settings-original");
            File.WriteAllText(Path.Combine(logsDir, "history.log"), "log-original");
            File.WriteAllText(Path.Combine(pendingDir, "keep.dat"), "pending-original");
            File.WriteAllText(Path.Combine(backupDir, "backup.txt"), "backup-original");
            File.WriteAllText(Path.Combine(targetDir, "app.dll"), "app-original");

            var prefix = wrapped ? "release\\" : string.Empty;
            var zipPath = CreateTestZip("protected.zip", new[]
            {
                ($"{prefix}SeTtInGs/UserSettings.xml", "settings-malicious"),
                ($"{prefix}LOGS\\history.log", "log-malicious"),
                ($"{prefix}updates_pending/keep.dat", "pending-malicious"),
                ($"{prefix}.backup_user/backup.txt", "backup-malicious"),
                ($"{prefix}nested/.backup_hidden/file.txt", "hidden-malicious"),
                ($"{prefix}app.dll", "app-updated")
            });

            var manager = new InstallationManager(CreateArguments(zipPath, targetDir), Log);
            manager.Execute();

            Assert.Equal("settings-original", File.ReadAllText(Path.Combine(settingsDir, "UserSettings.xml")));
            Assert.Equal("log-original", File.ReadAllText(Path.Combine(logsDir, "history.log")));
            Assert.Equal("pending-original", File.ReadAllText(Path.Combine(pendingDir, "keep.dat")));
            Assert.Equal("backup-original", File.ReadAllText(Path.Combine(backupDir, "backup.txt")));
            Assert.False(Directory.Exists(Path.Combine(targetDir, "nested")));
            Assert.Equal("app-updated", File.ReadAllText(Path.Combine(targetDir, "app.dll")));
        }

        /// <summary>
        /// Verifies that caller-supplied preserved paths and descendants remain unchanged.
        /// </summary>
        [Fact]
        public void Execute_AdditionalPreservedPath_IsNeverOverwritten()
        {
            var targetDir = CreateSubDir("target");
            var dataDir = Directory.CreateDirectory(Path.Combine(targetDir, "data", "user")).FullName;
            File.WriteAllText(Path.Combine(dataDir, "config.json"), "original");
            var zipPath = CreateTestZip("preserve.zip", new[]
            {
                ("data/user/config.json", "malicious"),
                ("app.dll", "updated")
            });
            var arguments = CreateArguments(zipPath, targetDir);
            arguments.PreservedRelativePaths.Add("data/user");

            new InstallationManager(arguments, Log).Execute();

            Assert.Equal("original", File.ReadAllText(Path.Combine(dataDir, "config.json")));
            Assert.Equal("updated", File.ReadAllText(Path.Combine(targetDir, "app.dll")));
        }

        /// <summary>
        /// Verifies that unsafe absolute, traversal, and alternate-stream paths are rejected.
        /// </summary>
        /// <param name="entryPath">The unsafe entry path.</param>
        [Theory]
        [InlineData("../escaped.txt")]
        [InlineData("/absolute.txt")]
        [InlineData("file.txt:stream")]
        public void Execute_UnsafeArchivePath_IsRejectedBeforeWrites(string entryPath)
        {
            var targetDir = CreateSubDir("target");
            File.WriteAllText(Path.Combine(targetDir, "app.dll"), "original");
            var zipPath = CreateTestZip("unsafe.zip", new[]
            {
                ("app.dll", "updated"),
                (entryPath, "malicious")
            });

            Assert.Throws<SecurityException>(() =>
                new InstallationManager(CreateArguments(zipPath, targetDir), Log).Execute());
            Assert.Equal("original", File.ReadAllText(Path.Combine(targetDir, "app.dll")));
        }

        /// <summary>
        /// Verifies that duplicate case-insensitive destinations are rejected before writes.
        /// </summary>
        [Fact]
        public void Execute_DuplicateDestinations_AreRejectedBeforeWrites()
        {
            var targetDir = CreateSubDir("target");
            var zipPath = CreateTestZip("duplicate.zip", new[]
            {
                ("App.dll", "one"),
                ("app.dll", "two")
            });

            Assert.Throws<InvalidDataException>(() =>
                new InstallationManager(CreateArguments(zipPath, targetDir), Log).Execute());
            Assert.False(File.Exists(Path.Combine(targetDir, "app.dll")));
        }

        /// <summary>
        /// Verifies that archive file-directory collisions are rejected before writes.
        /// </summary>
        [Fact]
        public void Execute_FileDirectoryCollision_IsRejectedBeforeWrites()
        {
            var targetDir = CreateSubDir("target");
            var zipPath = CreateTestZip("collision.zip", new[]
            {
                ("item", "file"),
                ("item/child.txt", "child")
            });

            Assert.Throws<InvalidDataException>(() =>
                new InstallationManager(CreateArguments(zipPath, targetDir), Log).Execute());
            Assert.False(File.Exists(Path.Combine(targetDir, "item")));
        }

        /// <summary>
        /// Verifies that an archive file cannot replace an existing installation directory.
        /// </summary>
        [Fact]
        public void Execute_ExistingDirectoryFileCollision_IsRejectedBeforeWrites()
        {
            var targetDir = CreateSubDir("target");
            Directory.CreateDirectory(Path.Combine(targetDir, "app.dll"));
            var zipPath = CreateTestZip("existing-collision.zip", new[] { ("app.dll", "file") });

            Assert.Throws<InvalidDataException>(() =>
                new InstallationManager(CreateArguments(zipPath, targetDir), Log).Execute());
            Assert.True(Directory.Exists(Path.Combine(targetDir, "app.dll")));
        }

        /// <summary>
        /// Verifies that file-count and uncompressed-size limits are enforced during preflight.
        /// </summary>
        [Fact]
        public void Execute_ArchiveSafetyLimits_AreEnforcedBeforeWrites()
        {
            var targetDir = CreateSubDir("target");
            var countZipPath = CreateTestZip("count.zip", new[]
            {
                ("one.txt", "1"),
                ("two.txt", "2")
            });
            var sizeZipPath = CreateTestZip("size.zip", new[] { ("large.txt", "1234") });

            Assert.Throws<InvalidDataException>(() =>
                new InstallationManager(
                    CreateArguments(countZipPath, targetDir),
                    Log,
                    maximumArchiveFileCount: 1).Execute());
            Assert.Throws<InvalidDataException>(() =>
                new InstallationManager(
                    CreateArguments(sizeZipPath, targetDir),
                    Log,
                    maximumUncompressedBytes: 3).Execute());
            Assert.Empty(Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories));
        }

        /// <summary>
        /// Verifies that rollback restores overwritten files and removes newly introduced files.
        /// </summary>
        [Fact]
        public void Execute_MidApplyFailure_RestoresOriginalInstallation()
        {
            var targetDir = CreateSubDir("target");
            var settingsDir = Directory.CreateDirectory(Path.Combine(targetDir, "settings")).FullName;
            File.WriteAllText(Path.Combine(targetDir, "app.dll"), "original-app");
            File.WriteAllText(Path.Combine(settingsDir, "UserSettings.xml"), "original-settings");
            var zipPath = CreateTestZip("rollback.zip", new[]
            {
                ("app.dll", "updated-app"),
                ("newdir/new.txt", "new-file"),
                ("fail.txt", "trigger"),
                ("settings/UserSettings.xml", "malicious-settings")
            });
            var arguments = CreateArguments(zipPath, targetDir);
            arguments.CreateBackup = true;
            var manager = new InstallationManager(
                arguments,
                Log,
                relativePath =>
                {
                    if (relativePath == "fail.txt")
                        throw new IOException("Injected apply failure.");
                });

            Assert.Throws<IOException>(() => manager.Execute());

            Assert.Equal("original-app", File.ReadAllText(Path.Combine(targetDir, "app.dll")));
            Assert.Equal("original-settings", File.ReadAllText(Path.Combine(settingsDir, "UserSettings.xml")));
            Assert.False(File.Exists(Path.Combine(targetDir, "newdir", "new.txt")));
            Assert.False(Directory.Exists(Path.Combine(targetDir, "newdir")));
        }

        /// <summary>
        /// Verifies that successful installation removes updater-owned package staging.
        /// </summary>
        [Fact]
        public void Execute_Success_CleansStagedPackageAndPendingDirectory()
        {
            var targetDir = CreateSubDir("target");
            var pendingDir = Directory.CreateDirectory(Path.Combine(targetDir, "updates_pending")).FullName;
            var zipPath = CreateTestZipAtPath(Path.Combine(pendingDir, "update.zip"), new[]
            {
                ("app.dll", "updated")
            });

            new InstallationManager(CreateArguments(zipPath, targetDir), Log).Execute();

            Assert.Equal("updated", File.ReadAllText(Path.Combine(targetDir, "app.dll")));
            Assert.False(File.Exists(zipPath));
            Assert.False(Directory.Exists(pendingDir));
        }

        /// <summary>
        /// Verifies that all installed updater payload files can be replaced by an update.
        /// </summary>
        [Fact]
        public void Execute_UpdateContainsUpdaterPayload_ReplacesAllInstalledPayloadFiles()
        {
            var targetDir = CreateSubDir("target");
            var payloadNames = new[]
            {
                "SoftwareUpdate.Updater.exe",
                "SoftwareUpdate.Updater.dll",
                "SoftwareUpdate.Updater.deps.json",
                "SoftwareUpdate.Updater.runtimeconfig.json"
            };
            foreach (var payloadName in payloadNames)
            {
                File.WriteAllText(Path.Combine(targetDir, payloadName), "old");
            }

            var zipPath = CreateTestZip(
                "self-update.zip",
                payloadNames.Select(name => (name, "new")).ToArray());

            new InstallationManager(CreateArguments(zipPath, targetDir), Log).Execute();

            foreach (var payloadName in payloadNames)
            {
                Assert.Equal("new", File.ReadAllText(Path.Combine(targetDir, payloadName)));
            }
        }

        /// <summary>
        /// Verifies that an existing directory reparse point is rejected before extraction.
        /// </summary>
        [Fact]
        public void Execute_ReparsePointInDestination_IsRejected()
        {
            var targetDir = CreateSubDir("target");
            var outsideDir = CreateSubDir("outside");
            var linkPath = Path.Combine(targetDir, "linked");
            try
            {
                Directory.CreateSymbolicLink(linkPath, outsideDir);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                return;
            }

            var zipPath = CreateTestZip("reparse.zip", new[] { ("linked/escaped.txt", "malicious") });

            Assert.Throws<SecurityException>(() =>
                new InstallationManager(CreateArguments(zipPath, targetDir), Log).Execute());
            Assert.False(File.Exists(Path.Combine(outsideDir, "escaped.txt")));
        }

        #endregion

        #region RestoreFromBackup Tests

        /// <summary>
        /// Verifies that RestoreFromBackup successfully restores files from a backup directory to the target.
        /// </summary>
        [Fact]
        public void RestoreFromBackup_RestoresFiles()
        {
            var targetDir = CreateSubDir("target");
            var backupDir = CreateSubDir("backup");

            // Create backup files
            File.WriteAllText(Path.Combine(backupDir, "restored.txt"), "backup content");
            var backupSubDir = Path.Combine(backupDir, "subdir");
            Directory.CreateDirectory(backupSubDir);
            File.WriteAllText(Path.Combine(backupSubDir, "nested.txt"), "nested backup");

            // Call RestoreFromBackup via reflection
            var args = new UpdaterArguments
            {
                ProcessId = 1,
                ZipPath = "test.zip",
                TargetDirectory = targetDir,
                MainExecutable = "test.exe"
            };

            var manager = new InstallationManager(args, Log);
            InvokeRestoreFromBackup(manager, backupDir);

            // Verify files were restored
            Assert.True(File.Exists(Path.Combine(targetDir, "restored.txt")));
            Assert.Equal("backup content", File.ReadAllText(Path.Combine(targetDir, "restored.txt")));
            Assert.True(File.Exists(Path.Combine(targetDir, "subdir", "nested.txt")));
        }

        /// <summary>
        /// Helper method to invoke the private RestoreFromBackup method via reflection.
        /// </summary>
        /// <param name="manager">The InstallationManager instance.</param>
        /// <param name="backupDir">The backup directory path to restore from.</param>
        private static void InvokeRestoreFromBackup(InstallationManager manager, string backupDir)
        {
            var method = typeof(InstallationManager)
                .GetMethod("RestoreFromBackup", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(manager, new object[] { backupDir });
        }

        #endregion

        #region CopyDirectory Tests

        /// <summary>
        /// Verifies that CopyDirectory recursively copies all files and subdirectories.
        /// </summary>
        [Fact]
        public void CopyDirectory_CopiesAllContents()
        {
            var sourceDir = CreateSubDir("source");
            var destDir = Path.Combine(_testDir, "dest");

            // Create source structure
            File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content1");
            File.WriteAllText(Path.Combine(sourceDir, "file2.txt"), "content2");
            var subDir = Path.Combine(sourceDir, "sub");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested");
            var deepDir = Path.Combine(subDir, "deep");
            Directory.CreateDirectory(deepDir);
            File.WriteAllText(Path.Combine(deepDir, "deep.txt"), "deep content");

            // Call CopyDirectory via reflection
            var args = new UpdaterArguments
            {
                ProcessId = 1,
                ZipPath = "test.zip",
                TargetDirectory = _testDir,
                MainExecutable = "test.exe"
            };

            var manager = new InstallationManager(args, Log);
            InvokeCopyDirectory(manager, sourceDir, destDir);

            // Verify all files were copied
            Assert.True(File.Exists(Path.Combine(destDir, "file1.txt")));
            Assert.True(File.Exists(Path.Combine(destDir, "file2.txt")));
            Assert.True(File.Exists(Path.Combine(destDir, "sub", "nested.txt")));
            Assert.True(File.Exists(Path.Combine(destDir, "sub", "deep", "deep.txt")));

            // Verify content
            Assert.Equal("content1", File.ReadAllText(Path.Combine(destDir, "file1.txt")));
            Assert.Equal("deep content", File.ReadAllText(Path.Combine(destDir, "sub", "deep", "deep.txt")));
        }

        /// <summary>
        /// Helper method to invoke the private CopyDirectory method via reflection.
        /// </summary>
        /// <param name="manager">The InstallationManager instance.</param>
        /// <param name="source">The source directory path to copy from.</param>
        /// <param name="dest">The destination directory path to copy to.</param>
        private static void InvokeCopyDirectory(InstallationManager manager, string source, string dest)
        {
            var method = typeof(InstallationManager)
                .GetMethod("CopyDirectory", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(manager, new object[] { source, dest });
        }

        #endregion

        #region Logging Tests

        /// <summary>
        /// Verifies that Execute method logs appropriate progress messages during execution.
        /// </summary>
        [Fact]
        public void Execute_LogsProgressMessages()
        {
            var targetDir = CreateSubDir("target");
            var zipPath = CreateTestZip("update.zip", new[] { ("file.txt", "content") });

            var args = new UpdaterArguments
            {
                ProcessId = -1,
                ZipPath = zipPath,
                TargetDirectory = targetDir,
                MainExecutable = "test.exe",
                CreateBackup = false
            };

            var manager = new InstallationManager(args, Log);
            try { manager.Execute(); } catch { }

            Assert.Contains(_logs, l => l.Contains("Starting update process"));
            Assert.Contains(_logs, l => l.Contains("Target directory"));
            Assert.Contains(_logs, l => l.Contains("Update package"));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a subdirectory within the test directory.
        /// </summary>
        /// <param name="name">The name of the subdirectory to create.</param>
        /// <returns>The full path to the created subdirectory.</returns>
        private string CreateSubDir(string name)
        {
            var path = Path.Combine(_testDir, name);
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Creates a test ZIP archive with the specified entries.
        /// </summary>
        /// <param name="name">The name of the ZIP file to create.</param>
        /// <param name="entries">An array of tuples containing entry paths and their content.</param>
        /// <returns>The full path to the created ZIP file.</returns>
        private string CreateTestZip(string name, (string path, string content)[] entries)
        {
            var zipPath = Path.Combine(_testDir, name);

            return CreateTestZipAtPath(zipPath, entries);
        }

        /// <summary>
        /// Creates a test ZIP archive at an explicit path.
        /// </summary>
        /// <param name="zipPath">The output ZIP path.</param>
        /// <param name="entries">The archive entries.</param>
        /// <returns>The ZIP path.</returns>
        private static string CreateTestZipAtPath(string zipPath, (string path, string content)[] entries)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var (entryPath, content) in entries)
                {
                    var entry = archive.CreateEntry(entryPath);
                    using var writer = new StreamWriter(entry.Open());
                    writer.Write(content);
                }
            }

            return zipPath;
        }

        /// <summary>
        /// Creates standard arguments for an installation test.
        /// </summary>
        /// <param name="zipPath">The update package.</param>
        /// <param name="targetDirectory">The target installation.</param>
        /// <returns>The updater arguments.</returns>
        private static UpdaterArguments CreateArguments(string zipPath, string targetDirectory)
        {
            return new UpdaterArguments
            {
                ProcessId = -1,
                ZipPath = zipPath,
                TargetDirectory = targetDirectory,
                MainExecutable = "missing.exe",
                CreateBackup = false
            };
        }

        /// <summary>
        /// Creates a malicious ZIP archive containing path traversal attempts for security testing.
        /// </summary>
        /// <param name="name">The name of the ZIP file to create.</param>
        /// <returns>The full path to the created ZIP file.</returns>
        private string CreatePathTraversalZip(string name)
        {
            var zipPath = Path.Combine(_testDir, name);

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                // Normal file
                var normalEntry = archive.CreateEntry("normal.txt");
                using (var writer = new StreamWriter(normalEntry.Open()))
                {
                    writer.Write("normal content");
                }

                // Path traversal attempt
                var maliciousEntry = archive.CreateEntry("../escaped.txt");
                using (var writer = new StreamWriter(maliciousEntry.Open()))
                {
                    writer.Write("malicious content");
                }

                // Another traversal attempt
                var deepTraversal = archive.CreateEntry("subdir/../../also_escaped.txt");
                using (var writer = new StreamWriter(deepTraversal.Open()))
                {
                    writer.Write("also malicious");
                }
            }

            return zipPath;
        }

        #endregion
    }
}
