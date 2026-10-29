using Xunit;
using SoftwareUpdate;

namespace SoftwareUpdate.Tests.Core
{
    /// <summary>
    /// Provides unit tests for the <see cref="UpdateOptions"/> class, verifying default values, property setters,
    /// validation logic, and resolved properties for software update configuration.
    /// </summary>
    public class UpdateOptionsTests
    {
        #region Default Values Tests

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateOptions"/> has GitHubOwner set to null.
        /// </summary>
        [Fact]
        public void DefaultInstance_GitHubOwner_IsNull()
        {
            var options = new UpdateOptions();

            Assert.Null(options.GitHubOwner);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateOptions"/> has GitHubRepo set to null.
        /// </summary>
        [Fact]
        public void DefaultInstance_GitHubRepo_IsNull()
        {
            var options = new UpdateOptions();

            Assert.Null(options.GitHubRepo);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateOptions"/> has CurrentVersion set to null.
        /// </summary>
        [Fact]
        public void DefaultInstance_CurrentVersion_IsNull()
        {
            var options = new UpdateOptions();

            Assert.Null(options.CurrentVersion);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateOptions"/> has AssetNamePattern set to "*.zip".
        /// </summary>
        [Fact]
        public void DefaultInstance_AssetNamePattern_IsDefaultZip()
        {
            var options = new UpdateOptions();

            Assert.Equal("*.zip", options.AssetNamePattern);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateOptions"/> has IncludePreReleases set to false.
        /// </summary>
        [Fact]
        public void DefaultInstance_IncludePreReleases_IsFalse()
        {
            var options = new UpdateOptions();

            Assert.False(options.IncludePreReleases);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateOptions"/> has CreateBackup set to true.
        /// </summary>
        [Fact]
        public void DefaultInstance_CreateBackup_IsTrue()
        {
            var options = new UpdateOptions();

            Assert.True(options.CreateBackup);
        }

        /// <summary>
        /// Verifies that checksum enforcement is opt-in for compatibility.
        /// </summary>
        [Fact]
        public void DefaultInstance_RequireSha256Checksum_IsFalse()
        {
            var options = new UpdateOptions();

            Assert.False(options.RequireSha256Checksum);
        }

        /// <summary>
        /// Verifies that additional preserved paths are empty by default.
        /// </summary>
        [Fact]
        public void DefaultInstance_AdditionalPreservedRelativePaths_IsEmpty()
        {
            var options = new UpdateOptions();

            Assert.Empty(options.AdditionalPreservedRelativePaths);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateOptions"/> has RequestTimeoutSeconds set to 30.
        /// </summary>
        [Fact]
        public void DefaultInstance_RequestTimeoutSeconds_Is30()
        {
            var options = new UpdateOptions();

            Assert.Equal(30, options.RequestTimeoutSeconds);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateOptions"/> has GitHubToken set to null.
        /// </summary>
        [Fact]
        public void DefaultInstance_GitHubToken_IsNull()
        {
            var options = new UpdateOptions();

            Assert.Null(options.GitHubToken);
        }

        #endregion

        #region Property Set Tests

        /// <summary>
        /// Verifies that the GitHubOwner property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void GitHubOwner_SetAndGet()
        {
            var options = new UpdateOptions { GitHubOwner = "USACE-RMC" };

            Assert.Equal("USACE-RMC", options.GitHubOwner);
        }

        /// <summary>
        /// Verifies that the GitHubRepo property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void GitHubRepo_SetAndGet()
        {
            var options = new UpdateOptions { GitHubRepo = "RMC-BestFit" };

            Assert.Equal("RMC-BestFit", options.GitHubRepo);
        }

        /// <summary>
        /// Verifies that the CurrentVersion property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void CurrentVersion_SetAndGet()
        {
            var version = new SemanticVersion(1, 0, 0);
            var options = new UpdateOptions { CurrentVersion = version };

            Assert.Same(version, options.CurrentVersion);
        }

        /// <summary>
        /// Verifies that the AssetNamePattern property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void AssetNamePattern_SetAndGet()
        {
            var options = new UpdateOptions { AssetNamePattern = "MyApp-*.zip" };

            Assert.Equal("MyApp-*.zip", options.AssetNamePattern);
        }

        /// <summary>
        /// Verifies that the IncludePreReleases property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void IncludePreReleases_SetAndGet()
        {
            var options = new UpdateOptions { IncludePreReleases = true };

            Assert.True(options.IncludePreReleases);
        }

        /// <summary>
        /// Verifies that the GitHubToken property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void GitHubToken_SetAndGet()
        {
            var options = new UpdateOptions { GitHubToken = "ghp_token123" };

            Assert.Equal("ghp_token123", options.GitHubToken);
        }

        /// <summary>
        /// Verifies that the CreateBackup property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void CreateBackup_SetAndGet()
        {
            var options = new UpdateOptions { CreateBackup = false };

            Assert.False(options.CreateBackup);
        }

        /// <summary>
        /// Verifies that checksum enforcement can be enabled.
        /// </summary>
        [Fact]
        public void RequireSha256Checksum_SetAndGet()
        {
            var options = new UpdateOptions { RequireSha256Checksum = true };

            Assert.True(options.RequireSha256Checksum);
        }

        /// <summary>
        /// Verifies that additional preserved paths can be configured.
        /// </summary>
        [Fact]
        public void AdditionalPreservedRelativePaths_AddAndGet()
        {
            var options = new UpdateOptions();
            options.AdditionalPreservedRelativePaths.Add("data/user");

            Assert.Equal("data/user", Assert.Single(options.AdditionalPreservedRelativePaths));
        }

        /// <summary>
        /// Verifies that the RequestTimeoutSeconds property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void RequestTimeoutSeconds_SetAndGet()
        {
            var options = new UpdateOptions { RequestTimeoutSeconds = 60 };

            Assert.Equal(60, options.RequestTimeoutSeconds);
        }

        #endregion

        #region Validate Tests

        /// <summary>
        /// Verifies that the Validate method throws <see cref="ArgumentException"/> when GitHubOwner is null.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when GitHubOwner is null.</exception>
        [Fact]
        public void Validate_NullGitHubOwner_ThrowsArgumentException()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = null,
                GitHubRepo = "ValidRepo",
                CurrentVersion = new SemanticVersion(1, 0, 0)
            };

            var ex = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("GitHubOwner", ex.Message);
        }

        /// <summary>
        /// Verifies that the Validate method throws <see cref="ArgumentException"/> when GitHubOwner is empty.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when GitHubOwner is empty.</exception>
        [Fact]
        public void Validate_EmptyGitHubOwner_ThrowsArgumentException()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "",
                GitHubRepo = "ValidRepo",
                CurrentVersion = new SemanticVersion(1, 0, 0)
            };

            var ex = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("GitHubOwner", ex.Message);
        }

        /// <summary>
        /// Verifies that the Validate method throws <see cref="ArgumentException"/> when GitHubOwner contains invalid characters.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when GitHubOwner contains invalid characters.</exception>
        [Fact]
        public void Validate_InvalidGitHubOwner_ThrowsArgumentException()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "-invalid",
                GitHubRepo = "ValidRepo",
                CurrentVersion = new SemanticVersion(1, 0, 0)
            };

            var ex = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("alphanumeric", ex.Message);
        }

        /// <summary>
        /// Verifies that the Validate method throws <see cref="ArgumentException"/> when GitHubRepo is null.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when GitHubRepo is null.</exception>
        [Fact]
        public void Validate_NullGitHubRepo_ThrowsArgumentException()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "ValidOwner",
                GitHubRepo = null,
                CurrentVersion = new SemanticVersion(1, 0, 0)
            };

            var ex = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("GitHubRepo", ex.Message);
        }

        /// <summary>
        /// Verifies that the Validate method throws <see cref="ArgumentException"/> when GitHubRepo contains invalid characters.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when GitHubRepo contains invalid characters.</exception>
        [Fact]
        public void Validate_InvalidGitHubRepo_ThrowsArgumentException()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "ValidOwner",
                GitHubRepo = "invalid-",
                CurrentVersion = new SemanticVersion(1, 0, 0)
            };

            var ex = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("alphanumeric", ex.Message);
        }

        /// <summary>
        /// Verifies that the Validate method throws <see cref="ArgumentException"/> when CurrentVersion is null.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when CurrentVersion is null.</exception>
        [Fact]
        public void Validate_NullCurrentVersion_ThrowsArgumentException()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "ValidOwner",
                GitHubRepo = "ValidRepo",
                CurrentVersion = null
            };

            var ex = Assert.Throws<ArgumentException>(() => options.Validate());
            Assert.Contains("CurrentVersion", ex.Message);
        }

        /// <summary>
        /// Verifies that the Validate method does not throw an exception when all required properties are valid.
        /// </summary>
        [Fact]
        public void Validate_ValidOptions_DoesNotThrow()
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "USACE-RMC",
                GitHubRepo = "RMC-BestFit",
                CurrentVersion = new SemanticVersion(1, 0, 0)
            };

            var exception = Record.Exception(() => options.Validate());

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that unsafe additional preserved paths are rejected.
        /// </summary>
        /// <param name="preservedPath">The invalid path.</param>
        [Theory]
        [InlineData(@"C:\settings")]
        [InlineData("../settings")]
        [InlineData("data/./settings")]
        [InlineData("file:stream")]
        public void Validate_InvalidAdditionalPreservedPath_ThrowsArgumentException(string preservedPath)
        {
            var options = new UpdateOptions
            {
                GitHubOwner = "USACE-RMC",
                GitHubRepo = "RMC-BestFit",
                CurrentVersion = new SemanticVersion(1, 0, 0)
            };
            options.AdditionalPreservedRelativePaths.Add(preservedPath);

            Assert.Throws<ArgumentException>(() => options.Validate());
        }

        /// <summary>
        /// Verifies that the Validate method accepts various valid GitHub owner name formats.
        /// </summary>
        /// <param name="owner">The GitHub owner name to validate.</param>
        [Theory]
        [InlineData("a")]
        [InlineData("ValidOwner")]
        [InlineData("valid-owner")]
        [InlineData("valid123")]
        [InlineData("a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8")]
        public void Validate_ValidOwnerFormats_DoesNotThrow(string owner)
        {
            var options = new UpdateOptions
            {
                GitHubOwner = owner,
                GitHubRepo = "ValidRepo",
                CurrentVersion = new SemanticVersion(1, 0, 0)
            };

            var exception = Record.Exception(() => options.Validate());

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that the Validate method throws <see cref="ArgumentException"/> for invalid GitHub owner name formats.
        /// </summary>
        /// <param name="owner">The invalid GitHub owner name.</param>
        /// <exception cref="ArgumentException">Thrown when the owner name format is invalid.</exception>
        [Theory]
        [InlineData("-invalid")]
        [InlineData("invalid-")]
        [InlineData("in valid")]
        [InlineData("invalid@owner")]
        public void Validate_InvalidOwnerFormats_ThrowsArgumentException(string owner)
        {
            var options = new UpdateOptions
            {
                GitHubOwner = owner,
                GitHubRepo = "ValidRepo",
                CurrentVersion = new SemanticVersion(1, 0, 0)
            };

            Assert.Throws<ArgumentException>(() => options.Validate());
        }

        #endregion

        #region Resolved Properties Tests

        /// <summary>
        /// Verifies that ResolvedInstallDirectory returns the set value when InstallDirectory is explicitly set.
        /// </summary>
        [Fact]
        public void ResolvedInstallDirectory_WhenSet_ReturnsSetValue()
        {
            var options = new UpdateOptions { InstallDirectory = @"C:\MyApp" };

            Assert.Equal(@"C:\MyApp", options.ResolvedInstallDirectory);
        }

        /// <summary>
        /// Verifies that ResolvedInstallDirectory returns the executing assembly directory when InstallDirectory is null.
        /// </summary>
        [Fact]
        public void ResolvedInstallDirectory_WhenNull_ReturnsExecutingAssemblyDirectory()
        {
            var options = new UpdateOptions { InstallDirectory = null };

            Assert.False(string.IsNullOrEmpty(options.ResolvedInstallDirectory));
        }

        /// <summary>
        /// Verifies that ResolvedMainExecutableName returns the set value when MainExecutableName is explicitly set.
        /// </summary>
        [Fact]
        public void ResolvedMainExecutableName_WhenSet_ReturnsSetValue()
        {
            var options = new UpdateOptions { MainExecutableName = "MyApp.exe" };

            Assert.Equal("MyApp.exe", options.ResolvedMainExecutableName);
        }

        /// <summary>
        /// Verifies that ResolvedMainExecutableName returns a non-empty value when MainExecutableName is null.
        /// </summary>
        [Fact]
        public void ResolvedMainExecutableName_WhenNull_ReturnsSomething()
        {
            var options = new UpdateOptions { MainExecutableName = null };

            Assert.False(string.IsNullOrEmpty(options.ResolvedMainExecutableName));
        }

        /// <summary>
        /// Verifies that ResolvedUpdaterPath returns the set value when UpdaterExecutablePath is explicitly set.
        /// </summary>
        [Fact]
        public void ResolvedUpdaterPath_WhenSet_ReturnsSetValue()
        {
            var options = new UpdateOptions { UpdaterExecutablePath = @"C:\Updater\Updater.exe" };

            Assert.Equal(@"C:\Updater\Updater.exe", options.ResolvedUpdaterPath);
        }

        /// <summary>
        /// Verifies that ResolvedUpdaterPath combines with install directory when UpdaterExecutablePath is null.
        /// </summary>
        [Fact]
        public void ResolvedUpdaterPath_WhenNull_CombinesWithInstallDirectory()
        {
            var options = new UpdateOptions { UpdaterExecutablePath = null };

            Assert.Contains("SoftwareUpdate.Updater.exe", options.ResolvedUpdaterPath);
        }

        /// <summary>
        /// Verifies that ResolvedSkippedVersionsPath returns the set value when SkippedVersionsFilePath is explicitly set.
        /// </summary>
        [Fact]
        public void ResolvedSkippedVersionsPath_WhenSet_ReturnsSetValue()
        {
            var options = new UpdateOptions { SkippedVersionsFilePath = @"C:\Custom\skipped.txt" };

            Assert.Equal(@"C:\Custom\skipped.txt", options.ResolvedSkippedVersionsPath);
        }

        /// <summary>
        /// Verifies that ResolvedSkippedVersionsPath uses AppData and repo name when SkippedVersionsFilePath is null.
        /// </summary>
        [Fact]
        public void ResolvedSkippedVersionsPath_WhenNull_UsesAppDataAndRepo()
        {
            var options = new UpdateOptions
            {
                SkippedVersionsFilePath = null,
                GitHubRepo = "MyTestRepo"
            };

            Assert.Contains("MyTestRepo", options.ResolvedSkippedVersionsPath);
            Assert.Contains("skipped_versions.txt", options.ResolvedSkippedVersionsPath);
        }

        /// <summary>
        /// Verifies that ResolvedSkippedVersionsPath uses "SoftwareUpdate" as default when repo name is null.
        /// </summary>
        [Fact]
        public void ResolvedSkippedVersionsPath_WhenRepoNull_UsesSoftwareUpdate()
        {
            var options = new UpdateOptions
            {
                SkippedVersionsFilePath = null,
                GitHubRepo = null
            };

            Assert.Contains("SoftwareUpdate", options.ResolvedSkippedVersionsPath);
        }

        #endregion
    }
}
