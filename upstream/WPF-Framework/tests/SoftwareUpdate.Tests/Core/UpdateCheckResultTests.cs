using Xunit;
using SoftwareUpdate;

namespace SoftwareUpdate.Tests.Core
{
    /// <summary>
    /// Provides unit tests for the <see cref="UpdateCheckResult"/> class, verifying the behavior of properties,
    /// factory methods, and default values.
    /// </summary>
    public class UpdateCheckResultTests
    {
        #region Default Property Tests

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateCheckResult"/> has IsUpdateAvailable set to false.
        /// </summary>
        [Fact]
        public void DefaultInstance_IsUpdateAvailable_IsFalse()
        {
            var result = new UpdateCheckResult();

            Assert.False(result.IsUpdateAvailable);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateCheckResult"/> has Update set to null.
        /// </summary>
        [Fact]
        public void DefaultInstance_Update_IsNull()
        {
            var result = new UpdateCheckResult();

            Assert.Null(result.Update);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateCheckResult"/> has CurrentVersion set to null.
        /// </summary>
        [Fact]
        public void DefaultInstance_CurrentVersion_IsNull()
        {
            var result = new UpdateCheckResult();

            Assert.Null(result.CurrentVersion);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateCheckResult"/> has Error set to null.
        /// </summary>
        [Fact]
        public void DefaultInstance_Error_IsNull()
        {
            var result = new UpdateCheckResult();

            Assert.Null(result.Error);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateCheckResult"/> has Success set to true.
        /// </summary>
        [Fact]
        public void DefaultInstance_Success_IsTrue()
        {
            var result = new UpdateCheckResult();

            Assert.True(result.Success);
        }

        /// <summary>
        /// Verifies that a default instance of <see cref="UpdateCheckResult"/> has IsSkippedVersion set to false.
        /// </summary>
        [Fact]
        public void DefaultInstance_IsSkippedVersion_IsFalse()
        {
            var result = new UpdateCheckResult();

            Assert.False(result.IsSkippedVersion);
        }

        #endregion

        #region Property Set Tests

        /// <summary>
        /// Verifies that the IsUpdateAvailable property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void IsUpdateAvailable_SetAndGet()
        {
            var result = new UpdateCheckResult { IsUpdateAvailable = true };

            Assert.True(result.IsUpdateAvailable);
        }

        /// <summary>
        /// Verifies that the Update property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Update_SetAndGet()
        {
            var updateInfo = new UpdateInfo { Version = new SemanticVersion(2, 0, 0) };
            var result = new UpdateCheckResult { Update = updateInfo };

            Assert.Same(updateInfo, result.Update);
        }

        /// <summary>
        /// Verifies that the CurrentVersion property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void CurrentVersion_SetAndGet()
        {
            var version = new SemanticVersion(1, 0, 0);
            var result = new UpdateCheckResult { CurrentVersion = version };

            Assert.Same(version, result.CurrentVersion);
        }

        /// <summary>
        /// Verifies that the Error property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Error_SetAndGet()
        {
            var error = new Exception("Test error");
            var result = new UpdateCheckResult { Error = error };

            Assert.Same(error, result.Error);
        }

        /// <summary>
        /// Verifies that the IsSkippedVersion property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void IsSkippedVersion_SetAndGet()
        {
            var result = new UpdateCheckResult { IsSkippedVersion = true };

            Assert.True(result.IsSkippedVersion);
        }

        #endregion

        #region Success Property Tests

        /// <summary>
        /// Verifies that the Success property returns true when Error is null.
        /// </summary>
        [Fact]
        public void Success_WhenErrorIsNull_ReturnsTrue()
        {
            var result = new UpdateCheckResult { Error = null };

            Assert.True(result.Success);
        }

        /// <summary>
        /// Verifies that the Success property returns false when Error is set.
        /// </summary>
        [Fact]
        public void Success_WhenErrorIsSet_ReturnsFalse()
        {
            var result = new UpdateCheckResult { Error = new Exception("Error") };

            Assert.False(result.Success);
        }

        #endregion

        #region NoUpdateAvailable Factory Tests

        /// <summary>
        /// Verifies that the NoUpdateAvailable factory method sets the CurrentVersion property.
        /// </summary>
        [Fact]
        public void NoUpdateAvailable_SetsCurrentVersion()
        {
            var version = new SemanticVersion(1, 0, 0);

            var result = UpdateCheckResult.NoUpdateAvailable(version);

            Assert.Same(version, result.CurrentVersion);
        }

        /// <summary>
        /// Verifies that the NoUpdateAvailable factory method sets IsUpdateAvailable to false.
        /// </summary>
        [Fact]
        public void NoUpdateAvailable_SetsIsUpdateAvailableToFalse()
        {
            var version = new SemanticVersion(1, 0, 0);

            var result = UpdateCheckResult.NoUpdateAvailable(version);

            Assert.False(result.IsUpdateAvailable);
        }

        /// <summary>
        /// Verifies that the NoUpdateAvailable factory method sets Update to null.
        /// </summary>
        [Fact]
        public void NoUpdateAvailable_UpdateIsNull()
        {
            var version = new SemanticVersion(1, 0, 0);

            var result = UpdateCheckResult.NoUpdateAvailable(version);

            Assert.Null(result.Update);
        }

        /// <summary>
        /// Verifies that the NoUpdateAvailable factory method sets Error to null and Success to true.
        /// </summary>
        [Fact]
        public void NoUpdateAvailable_ErrorIsNull()
        {
            var version = new SemanticVersion(1, 0, 0);

            var result = UpdateCheckResult.NoUpdateAvailable(version);

            Assert.Null(result.Error);
            Assert.True(result.Success);
        }

        #endregion

        #region UpdateAvailable Factory Tests

        /// <summary>
        /// Verifies that the UpdateAvailable factory method sets the CurrentVersion property.
        /// </summary>
        [Fact]
        public void UpdateAvailable_SetsCurrentVersion()
        {
            var currentVersion = new SemanticVersion(1, 0, 0);
            var updateInfo = new UpdateInfo { Version = new SemanticVersion(2, 0, 0) };

            var result = UpdateCheckResult.UpdateAvailable(currentVersion, updateInfo);

            Assert.Same(currentVersion, result.CurrentVersion);
        }

        /// <summary>
        /// Verifies that the UpdateAvailable factory method sets the Update property.
        /// </summary>
        [Fact]
        public void UpdateAvailable_SetsUpdate()
        {
            var currentVersion = new SemanticVersion(1, 0, 0);
            var updateInfo = new UpdateInfo { Version = new SemanticVersion(2, 0, 0) };

            var result = UpdateCheckResult.UpdateAvailable(currentVersion, updateInfo);

            Assert.Same(updateInfo, result.Update);
        }

        /// <summary>
        /// Verifies that the UpdateAvailable factory method sets IsUpdateAvailable to true.
        /// </summary>
        [Fact]
        public void UpdateAvailable_SetsIsUpdateAvailableToTrue()
        {
            var currentVersion = new SemanticVersion(1, 0, 0);
            var updateInfo = new UpdateInfo { Version = new SemanticVersion(2, 0, 0) };

            var result = UpdateCheckResult.UpdateAvailable(currentVersion, updateInfo);

            Assert.True(result.IsUpdateAvailable);
        }

        /// <summary>
        /// Verifies that the UpdateAvailable factory method sets IsSkippedVersion to false by default.
        /// </summary>
        [Fact]
        public void UpdateAvailable_DefaultIsSkipped_IsFalse()
        {
            var currentVersion = new SemanticVersion(1, 0, 0);
            var updateInfo = new UpdateInfo { Version = new SemanticVersion(2, 0, 0) };

            var result = UpdateCheckResult.UpdateAvailable(currentVersion, updateInfo);

            Assert.False(result.IsSkippedVersion);
        }

        /// <summary>
        /// Verifies that the UpdateAvailable factory method correctly sets the IsSkippedVersion property when specified.
        /// </summary>
        [Fact]
        public void UpdateAvailable_WithIsSkipped_SetsIsSkippedVersion()
        {
            var currentVersion = new SemanticVersion(1, 0, 0);
            var updateInfo = new UpdateInfo { Version = new SemanticVersion(2, 0, 0) };

            var result = UpdateCheckResult.UpdateAvailable(currentVersion, updateInfo, isSkipped: true);

            Assert.True(result.IsSkippedVersion);
        }

        /// <summary>
        /// Verifies that the UpdateAvailable factory method sets Error to null and Success to true.
        /// </summary>
        [Fact]
        public void UpdateAvailable_ErrorIsNull()
        {
            var currentVersion = new SemanticVersion(1, 0, 0);
            var updateInfo = new UpdateInfo { Version = new SemanticVersion(2, 0, 0) };

            var result = UpdateCheckResult.UpdateAvailable(currentVersion, updateInfo);

            Assert.Null(result.Error);
            Assert.True(result.Success);
        }

        #endregion

        #region Failed Factory Tests

        /// <summary>
        /// Verifies that the Failed factory method sets the CurrentVersion property.
        /// </summary>
        [Fact]
        public void Failed_SetsCurrentVersion()
        {
            var version = new SemanticVersion(1, 0, 0);
            var error = new Exception("Test error");

            var result = UpdateCheckResult.Failed(version, error);

            Assert.Same(version, result.CurrentVersion);
        }

        /// <summary>
        /// Verifies that the Failed factory method sets the Error property.
        /// </summary>
        [Fact]
        public void Failed_SetsError()
        {
            var version = new SemanticVersion(1, 0, 0);
            var error = new Exception("Test error");

            var result = UpdateCheckResult.Failed(version, error);

            Assert.Same(error, result.Error);
        }

        /// <summary>
        /// Verifies that the Failed factory method sets IsUpdateAvailable to false.
        /// </summary>
        [Fact]
        public void Failed_SetsIsUpdateAvailableToFalse()
        {
            var version = new SemanticVersion(1, 0, 0);
            var error = new Exception("Test error");

            var result = UpdateCheckResult.Failed(version, error);

            Assert.False(result.IsUpdateAvailable);
        }

        /// <summary>
        /// Verifies that the Failed factory method sets Success to false.
        /// </summary>
        [Fact]
        public void Failed_SuccessIsFalse()
        {
            var version = new SemanticVersion(1, 0, 0);
            var error = new Exception("Test error");

            var result = UpdateCheckResult.Failed(version, error);

            Assert.False(result.Success);
        }

        /// <summary>
        /// Verifies that the Failed factory method sets Update to null.
        /// </summary>
        [Fact]
        public void Failed_UpdateIsNull()
        {
            var version = new SemanticVersion(1, 0, 0);
            var error = new Exception("Test error");

            var result = UpdateCheckResult.Failed(version, error);

            Assert.Null(result.Update);
        }

        #endregion
    }
}
