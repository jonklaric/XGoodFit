using Xunit;
using SoftwareUpdate;
using SoftwareUpdate.GitHub;
using System.IO;

namespace SoftwareUpdate.Tests.GitHub
{
    /// <summary>
    /// Provides unit tests for the <see cref="GitHubUpdateService"/> class, verifying construction,
    /// version skipping, download operations, installation, disposal, and thread safety.
    /// </summary>
    public class GitHubUpdateServiceTests : IDisposable
    {
        /// <summary>
        /// The temporary test directory used for test file operations.
        /// </summary>
        private readonly string _testDir;

        /// <summary>
        /// The GitHubUpdateService instance under test.
        /// </summary>
        private GitHubUpdateService? _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubUpdateServiceTests"/> class.
        /// Sets up a temporary test directory for file operations.
        /// </summary>
        public GitHubUpdateServiceTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "GitHubUpdateServiceTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
        }

        /// <summary>
        /// Performs cleanup operations, disposing the service and deleting the temporary test directory.
        /// </summary>
        public void Dispose()
        {
            _service?.Dispose();

            if (Directory.Exists(_testDir))
            {
                try
                {
                    Directory.Delete(_testDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup failures
                }
            }
        }

        /// <summary>
        /// Creates a valid <see cref="UpdateOptions"/> instance for testing purposes.
        /// </summary>
        /// <returns>A valid UpdateOptions instance.</returns>
        private UpdateOptions CreateValidOptions()
        {
            return new UpdateOptions
            {
                GitHubOwner = "TestOwner",
                GitHubRepo = "TestRepo",
                CurrentVersion = SemanticVersion.Parse("1.0.0"),
                SkippedVersionsFilePath = Path.Combine(_testDir, "skipped.txt")
            };
        }

        #region Constructor Tests

        /// <summary>
        /// Verifies that the constructor throws <see cref="ArgumentNullException"/> when given null options.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new GitHubUpdateService(null!));
        }

        /// <summary>
        /// Verifies that the constructor throws <see cref="ArgumentException"/> when given invalid options.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when options are invalid.</exception>
        [Fact]
        public void Constructor_InvalidOptions_ThrowsArgumentException()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = null,
                GitHubRepo = "TestRepo",
                CurrentVersion = SemanticVersion.Parse("1.0.0")
            };

            Assert.Throws<ArgumentException>(() => new GitHubUpdateService(options));
        }

        /// <summary>
        /// Verifies that the constructor creates an instance successfully with valid options.
        /// </summary>
        [Fact]
        public void Constructor_ValidOptions_CreatesInstance()
        {
            var options = CreateValidOptions();

            _service = new GitHubUpdateService(options);

            Assert.NotNull(_service);
        }

        /// <summary>
        /// Verifies that the constructor sets the Options property correctly.
        /// </summary>
        [Fact]
        public void Constructor_SetsOptionsProperty()
        {
            var options = CreateValidOptions();

            _service = new GitHubUpdateService(options);

            Assert.Same(options, _service.Options);
        }

        /// <summary>
        /// Verifies that the initial state is Idle after construction.
        /// </summary>
        [Fact]
        public void Constructor_InitialStateIsIdle()
        {
            var options = CreateValidOptions();

            _service = new GitHubUpdateService(options);

            Assert.Equal(UpdateState.Idle, _service.State);
        }

        /// <summary>
        /// Verifies that AvailableUpdate is null after construction.
        /// </summary>
        [Fact]
        public void Constructor_AvailableUpdateIsNull()
        {
            var options = CreateValidOptions();

            _service = new GitHubUpdateService(options);

            Assert.Null(_service.AvailableUpdate);
        }

        #endregion

        #region SkipVersion Tests

        /// <summary>
        /// Verifies that SkipVersion adds a version to the skipped list.
        /// </summary>
        [Fact]
        public void SkipVersion_AddsVersionToSkippedList()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);
            var version = SemanticVersion.Parse("2.0.0");

            _service.SkipVersion(version);

            Assert.True(_service.IsVersionSkipped(version));
        }

        /// <summary>
        /// Verifies that SkipVersion does not throw when given a null version.
        /// </summary>
        [Fact]
        public void SkipVersion_NullVersion_DoesNotThrow()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            var exception = Record.Exception(() => _service.SkipVersion(null));

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that SkipVersion persists skipped versions to a file.
        /// </summary>
        [Fact]
        public void SkipVersion_PersistsToFile()
        {
            var skippedFilePath = Path.Combine(_testDir, "skipped.txt");
            var options = CreateValidOptions();
            options.SkippedVersionsFilePath = skippedFilePath;

            _service = new GitHubUpdateService(options);
            _service.SkipVersion(SemanticVersion.Parse("2.0.0"));

            Assert.True(File.Exists(skippedFilePath));
            var content = File.ReadAllText(skippedFilePath);
            Assert.Contains("2.0.0", content);
        }

        /// <summary>
        /// Verifies that multiple versions can be skipped and all are marked as skipped.
        /// </summary>
        [Fact]
        public void SkipVersion_MultipleVersions_AllSkipped()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            _service.SkipVersion(SemanticVersion.Parse("2.0.0"));
            _service.SkipVersion(SemanticVersion.Parse("2.1.0"));
            _service.SkipVersion(SemanticVersion.Parse("3.0.0"));

            Assert.True(_service.IsVersionSkipped(SemanticVersion.Parse("2.0.0")));
            Assert.True(_service.IsVersionSkipped(SemanticVersion.Parse("2.1.0")));
            Assert.True(_service.IsVersionSkipped(SemanticVersion.Parse("3.0.0")));
        }

        #endregion

        #region IsVersionSkipped Tests

        /// <summary>
        /// Verifies that IsVersionSkipped returns false for a null version.
        /// </summary>
        [Fact]
        public void IsVersionSkipped_NullVersion_ReturnsFalse()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            Assert.False(_service.IsVersionSkipped(null));
        }

        /// <summary>
        /// Verifies that IsVersionSkipped returns false for a version that has not been skipped.
        /// </summary>
        [Fact]
        public void IsVersionSkipped_NotSkippedVersion_ReturnsFalse()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            Assert.False(_service.IsVersionSkipped(SemanticVersion.Parse("99.0.0")));
        }

        /// <summary>
        /// Verifies that IsVersionSkipped loads skipped versions from file during construction.
        /// </summary>
        [Fact]
        public void IsVersionSkipped_LoadsFromFile()
        {
            var skippedFilePath = Path.Combine(_testDir, "skipped.txt");
            File.WriteAllLines(skippedFilePath, new[] { "2.0.0", "3.0.0" });

            var options = CreateValidOptions();
            options.SkippedVersionsFilePath = skippedFilePath;

            _service = new GitHubUpdateService(options);

            Assert.True(_service.IsVersionSkipped(SemanticVersion.Parse("2.0.0")));
            Assert.True(_service.IsVersionSkipped(SemanticVersion.Parse("3.0.0")));
            Assert.False(_service.IsVersionSkipped(SemanticVersion.Parse("1.0.0")));
        }

        /// <summary>
        /// Verifies that IsVersionSkipped performs case-insensitive comparison for prerelease identifiers.
        /// </summary>
        [Fact]
        public void IsVersionSkipped_CaseInsensitive()
        {
            var skippedFilePath = Path.Combine(_testDir, "skipped.txt");
            File.WriteAllLines(skippedFilePath, new[] { "2.0.0-BETA" });

            var options = CreateValidOptions();
            options.SkippedVersionsFilePath = skippedFilePath;

            _service = new GitHubUpdateService(options);

            // SemanticVersion.ToString() should match regardless of case
            Assert.True(_service.IsVersionSkipped(SemanticVersion.Parse("2.0.0-beta")));
        }

        #endregion

        #region ClearSkippedVersions Tests

        /// <summary>
        /// Verifies that ClearSkippedVersions removes all skipped versions.
        /// </summary>
        [Fact]
        public void ClearSkippedVersions_RemovesAllSkippedVersions()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            _service.SkipVersion(SemanticVersion.Parse("2.0.0"));
            _service.SkipVersion(SemanticVersion.Parse("3.0.0"));
            _service.ClearSkippedVersions();

            Assert.False(_service.IsVersionSkipped(SemanticVersion.Parse("2.0.0")));
            Assert.False(_service.IsVersionSkipped(SemanticVersion.Parse("3.0.0")));
        }

        /// <summary>
        /// Verifies that ClearSkippedVersions clears the skipped versions file.
        /// </summary>
        [Fact]
        public void ClearSkippedVersions_ClearsFile()
        {
            var skippedFilePath = Path.Combine(_testDir, "skipped.txt");
            var options = CreateValidOptions();
            options.SkippedVersionsFilePath = skippedFilePath;

            _service = new GitHubUpdateService(options);
            _service.SkipVersion(SemanticVersion.Parse("2.0.0"));
            _service.ClearSkippedVersions();

            var content = File.ReadAllText(skippedFilePath);
            Assert.Empty(content.Trim());
        }

        /// <summary>
        /// Verifies that ClearSkippedVersions does not throw when there are no skipped versions.
        /// </summary>
        [Fact]
        public void ClearSkippedVersions_WhenEmpty_DoesNotThrow()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            var exception = Record.Exception(() => _service.ClearSkippedVersions());

            Assert.Null(exception);
        }

        #endregion

        #region DownloadUpdateAsync Validation Tests

        /// <summary>
        /// Verifies that DownloadUpdateAsync throws <see cref="ArgumentNullException"/> when given a null update.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when update is null.</exception>
        [Fact]
        public async Task DownloadUpdateAsync_NullUpdate_ThrowsArgumentNullException()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.DownloadUpdateAsync(null!));
        }

        /// <summary>
        /// Verifies that DownloadUpdateAsync rejects calls made from the Idle state with an
        /// <see cref="InvalidOperationException"/>. Per the F-006 state-machine contract, callers
        /// must run a successful <see cref="GitHubUpdateService.CheckForUpdateAsync"/> first so
        /// the service is in the <see cref="UpdateState.UpdateAvailable"/> state before download.
        /// Previously this test asserted that a malformed UpdateInfo (null DownloadUrl) returned
        /// a failed result; that path is now unreachable because the state guard fires first,
        /// regardless of the supplied UpdateInfo's contents.
        /// </summary>
        [Fact]
        public async Task DownloadUpdateAsync_FromIdleState_ThrowsInvalidOperationException()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            var update = new UpdateInfo
            {
                AssetName = "valid.zip",
                DownloadUrl = "https://example.com/valid.zip"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DownloadUpdateAsync(update));
        }

        #endregion

        #region InstallUpdateAndRestart Validation Tests

        /// <summary>
        /// Verifies that InstallUpdateAndRestart throws <see cref="ArgumentNullException"/> when given a null file path.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
        [Fact]
        public void InstallUpdateAndRestart_NullFilePath_ThrowsArgumentNullException()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            Assert.Throws<ArgumentNullException>(() =>
                _service.InstallUpdateAndRestart(null!));
        }

        /// <summary>
        /// Verifies that InstallUpdateAndRestart throws <see cref="ArgumentNullException"/> when given an empty file path.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when filePath is empty.</exception>
        [Fact]
        public void InstallUpdateAndRestart_EmptyFilePath_ThrowsArgumentNullException()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            Assert.Throws<ArgumentNullException>(() =>
                _service.InstallUpdateAndRestart(string.Empty));
        }

        /// <summary>
        /// Verifies that InstallUpdateAndRestart throws <see cref="FileNotFoundException"/> when given a non-existent file.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        [Fact]
        public void InstallUpdateAndRestart_NonExistentFile_ThrowsFileNotFoundException()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            Assert.Throws<FileNotFoundException>(() =>
                _service.InstallUpdateAndRestart("/nonexistent/file.zip"));
        }

        #endregion

        #region Dispose Tests

        /// <summary>
        /// Verifies that Dispose can be called multiple times without throwing.
        /// </summary>
        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            var exception = Record.Exception(() =>
            {
                _service.Dispose();
                _service.Dispose();
            });

            Assert.Null(exception);
        }

        #endregion

        #region State Property Tests

        /// <summary>
        /// Verifies that the State property can be safely accessed from multiple threads.
        /// </summary>
        [Fact]
        public async Task State_IsThreadSafe()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            // Simple test - just verify reading state from multiple threads doesn't throw
            var tasks = Enumerable.Range(0, 10)
                .Select(i => Task.Run(() => { var state = _service.State; }))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        #endregion

        #region AvailableUpdate Property Tests

        /// <summary>
        /// Verifies that the AvailableUpdate property can be safely accessed from multiple threads.
        /// </summary>
        [Fact]
        public async Task AvailableUpdate_IsThreadSafe()
        {
            var options = CreateValidOptions();
            _service = new GitHubUpdateService(options);

            // Simple test - just verify reading from multiple threads doesn't throw
            var tasks = Enumerable.Range(0, 10)
                .Select(i => Task.Run(() => { var update = _service.AvailableUpdate; }))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        #endregion

        #region Skipped Versions File Handling Tests

        /// <summary>
        /// Verifies that the constructor does not throw when the skipped versions file is missing.
        /// </summary>
        [Fact]
        public void Constructor_MissingSkippedVersionsFile_DoesNotThrow()
        {
            var options = CreateValidOptions();
            options.SkippedVersionsFilePath = Path.Combine(_testDir, "nonexistent", "skipped.txt");

            var exception = Record.Exception(() => _service = new GitHubUpdateService(options));

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that the constructor does not throw when the skipped versions file is empty.
        /// </summary>
        [Fact]
        public void Constructor_EmptySkippedVersionsFile_DoesNotThrow()
        {
            var skippedFilePath = Path.Combine(_testDir, "skipped.txt");
            File.WriteAllText(skippedFilePath, "");

            var options = CreateValidOptions();
            options.SkippedVersionsFilePath = skippedFilePath;

            var exception = Record.Exception(() => _service = new GitHubUpdateService(options));

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that the constructor ignores empty lines and whitespace in the skipped versions file.
        /// </summary>
        [Fact]
        public void Constructor_SkippedVersionsFileWithWhitespace_IgnoresEmpty()
        {
            var skippedFilePath = Path.Combine(_testDir, "skipped.txt");
            File.WriteAllLines(skippedFilePath, new[] { "", "  ", "2.0.0", "  ", "" });

            var options = CreateValidOptions();
            options.SkippedVersionsFilePath = skippedFilePath;

            _service = new GitHubUpdateService(options);

            Assert.True(_service.IsVersionSkipped(SemanticVersion.Parse("2.0.0")));
        }

        /// <summary>
        /// Verifies that SkipVersion creates the necessary directory structure if it doesn't exist.
        /// </summary>
        [Fact]
        public void SkipVersion_CreatesDirectoryIfNeeded()
        {
            var skippedFilePath = Path.Combine(_testDir, "subdir", "deep", "skipped.txt");
            var options = CreateValidOptions();
            options.SkippedVersionsFilePath = skippedFilePath;

            _service = new GitHubUpdateService(options);
            _service.SkipVersion(SemanticVersion.Parse("2.0.0"));

            Assert.True(File.Exists(skippedFilePath));
        }

        #endregion
    }
}
