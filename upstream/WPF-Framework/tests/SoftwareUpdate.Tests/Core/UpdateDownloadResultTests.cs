using Xunit;
using SoftwareUpdate;

namespace SoftwareUpdate.Tests.Core
{
    /// <summary>
    /// Provides unit tests for the <see cref="UpdateDownloadResult"/> class, verifying properties,
    /// factory methods, and result states for download operations.
    /// </summary>
    public class UpdateDownloadResultTests
    {
        #region Property Tests

        /// <summary>
        /// Verifies that the Success property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Success_CanBeSetAndRetrieved()
        {
            var result = new UpdateDownloadResult { Success = true };

            Assert.True(result.Success);
        }

        /// <summary>
        /// Verifies that the FilePath property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void FilePath_CanBeSetAndRetrieved()
        {
            var path = "/tmp/update.zip";
            var result = new UpdateDownloadResult { FilePath = path };

            Assert.Equal(path, result.FilePath);
        }

        /// <summary>
        /// Verifies that the Error property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Error_CanBeSetAndRetrieved()
        {
            var exception = new InvalidOperationException("Test error");
            var result = new UpdateDownloadResult { Error = exception };

            Assert.Same(exception, result.Error);
        }

        /// <summary>
        /// Verifies that the WasCancelled property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void WasCancelled_CanBeSetAndRetrieved()
        {
            var result = new UpdateDownloadResult { WasCancelled = true };

            Assert.True(result.WasCancelled);
        }

        /// <summary>
        /// Verifies that the BytesDownloaded property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void BytesDownloaded_CanBeSetAndRetrieved()
        {
            var result = new UpdateDownloadResult { BytesDownloaded = 1024 * 1024 };

            Assert.Equal(1024 * 1024, result.BytesDownloaded);
        }

        /// <summary>
        /// Verifies that the Update property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Update_CanBeSetAndRetrieved()
        {
            var update = new UpdateInfo { Name = "Test Update" };
            var result = new UpdateDownloadResult { Update = update };

            Assert.Same(update, result.Update);
        }

        #endregion

        #region Successful Factory Method Tests

        /// <summary>
        /// Verifies that the Successful factory method sets Success to true.
        /// </summary>
        [Fact]
        public void Successful_SetsSuccessToTrue()
        {
            var update = new UpdateInfo();
            var result = UpdateDownloadResult.Successful("/tmp/update.zip", update, 1024);

            Assert.True(result.Success);
        }

        /// <summary>
        /// Verifies that the Successful factory method sets the FilePath property.
        /// </summary>
        [Fact]
        public void Successful_SetsFilePath()
        {
            var update = new UpdateInfo();
            var result = UpdateDownloadResult.Successful("/tmp/update.zip", update, 1024);

            Assert.Equal("/tmp/update.zip", result.FilePath);
        }

        /// <summary>
        /// Verifies that the Successful factory method sets the Update property.
        /// </summary>
        [Fact]
        public void Successful_SetsUpdate()
        {
            var update = new UpdateInfo { Name = "Test" };
            var result = UpdateDownloadResult.Successful("/tmp/update.zip", update, 1024);

            Assert.Same(update, result.Update);
        }

        /// <summary>
        /// Verifies that the Successful factory method sets the BytesDownloaded property.
        /// </summary>
        [Fact]
        public void Successful_SetsBytesDownloaded()
        {
            var update = new UpdateInfo();
            var result = UpdateDownloadResult.Successful("/tmp/update.zip", update, 1024 * 1024);

            Assert.Equal(1024 * 1024, result.BytesDownloaded);
        }

        /// <summary>
        /// Verifies that the Successful factory method does not set the Error property.
        /// </summary>
        [Fact]
        public void Successful_DoesNotSetError()
        {
            var update = new UpdateInfo();
            var result = UpdateDownloadResult.Successful("/tmp/update.zip", update, 1024);

            Assert.Null(result.Error);
        }

        /// <summary>
        /// Verifies that the Successful factory method does not set WasCancelled.
        /// </summary>
        [Fact]
        public void Successful_DoesNotSetWasCancelled()
        {
            var update = new UpdateInfo();
            var result = UpdateDownloadResult.Successful("/tmp/update.zip", update, 1024);

            Assert.False(result.WasCancelled);
        }

        #endregion

        #region Failed Factory Method Tests

        /// <summary>
        /// Verifies that the Failed factory method sets Success to false.
        /// </summary>
        [Fact]
        public void Failed_SetsSuccessToFalse()
        {
            var error = new Exception("Test");
            var result = UpdateDownloadResult.Failed(error);

            Assert.False(result.Success);
        }

        /// <summary>
        /// Verifies that the Failed factory method sets the Error property.
        /// </summary>
        [Fact]
        public void Failed_SetsError()
        {
            var error = new InvalidOperationException("Test error");
            var result = UpdateDownloadResult.Failed(error);

            Assert.Same(error, result.Error);
        }

        /// <summary>
        /// Verifies that the Failed factory method does not set the FilePath property.
        /// </summary>
        [Fact]
        public void Failed_DoesNotSetFilePath()
        {
            var error = new Exception("Test");
            var result = UpdateDownloadResult.Failed(error);

            Assert.Null(result.FilePath);
        }

        /// <summary>
        /// Verifies that the Failed factory method does not set WasCancelled.
        /// </summary>
        [Fact]
        public void Failed_DoesNotSetWasCancelled()
        {
            var error = new Exception("Test");
            var result = UpdateDownloadResult.Failed(error);

            Assert.False(result.WasCancelled);
        }

        /// <summary>
        /// Verifies that the Failed factory method does not set the Update property.
        /// </summary>
        [Fact]
        public void Failed_DoesNotSetUpdate()
        {
            var error = new Exception("Test");
            var result = UpdateDownloadResult.Failed(error);

            Assert.Null(result.Update);
        }

        /// <summary>
        /// Verifies that the Failed factory method preserves the exception type when using HttpRequestException.
        /// </summary>
        [Fact]
        public void Failed_WithHttpRequestException_PreservesExceptionType()
        {
            var error = new System.Net.Http.HttpRequestException("Network error");
            var result = UpdateDownloadResult.Failed(error);

            Assert.IsType<System.Net.Http.HttpRequestException>(result.Error);
        }

        #endregion

        #region Cancelled Factory Method Tests

        /// <summary>
        /// Verifies that the Cancelled factory method sets Success to false.
        /// </summary>
        [Fact]
        public void Cancelled_SetsSuccessToFalse()
        {
            var result = UpdateDownloadResult.Cancelled();

            Assert.False(result.Success);
        }

        /// <summary>
        /// Verifies that the Cancelled factory method sets WasCancelled to true.
        /// </summary>
        [Fact]
        public void Cancelled_SetsWasCancelledToTrue()
        {
            var result = UpdateDownloadResult.Cancelled();

            Assert.True(result.WasCancelled);
        }

        /// <summary>
        /// Verifies that the Cancelled factory method defaults BytesDownloaded to zero when not specified.
        /// </summary>
        [Fact]
        public void Cancelled_WithZeroBytesDownloaded_DefaultsToZero()
        {
            var result = UpdateDownloadResult.Cancelled();

            Assert.Equal(0, result.BytesDownloaded);
        }

        /// <summary>
        /// Verifies that the Cancelled factory method sets BytesDownloaded when specified.
        /// </summary>
        [Fact]
        public void Cancelled_WithBytesDownloaded_SetsBytesDownloaded()
        {
            var result = UpdateDownloadResult.Cancelled(bytesDownloaded: 5000);

            Assert.Equal(5000, result.BytesDownloaded);
        }

        /// <summary>
        /// Verifies that the Cancelled factory method does not set the Error property.
        /// </summary>
        [Fact]
        public void Cancelled_DoesNotSetError()
        {
            var result = UpdateDownloadResult.Cancelled();

            Assert.Null(result.Error);
        }

        /// <summary>
        /// Verifies that the Cancelled factory method does not set the FilePath property.
        /// </summary>
        [Fact]
        public void Cancelled_DoesNotSetFilePath()
        {
            var result = UpdateDownloadResult.Cancelled();

            Assert.Null(result.FilePath);
        }

        /// <summary>
        /// Verifies that the Cancelled factory method does not set the Update property.
        /// </summary>
        [Fact]
        public void Cancelled_DoesNotSetUpdate()
        {
            var result = UpdateDownloadResult.Cancelled();

            Assert.Null(result.Update);
        }

        #endregion

        #region Default Values Tests

        /// <summary>
        /// Verifies that a new instance has Success initialized to false.
        /// </summary>
        [Fact]
        public void NewInstance_HasFalseSuccess()
        {
            var result = new UpdateDownloadResult();

            Assert.False(result.Success);
        }

        /// <summary>
        /// Verifies that a new instance has FilePath initialized to null.
        /// </summary>
        [Fact]
        public void NewInstance_HasNullFilePath()
        {
            var result = new UpdateDownloadResult();

            Assert.Null(result.FilePath);
        }

        /// <summary>
        /// Verifies that a new instance has Error initialized to null.
        /// </summary>
        [Fact]
        public void NewInstance_HasNullError()
        {
            var result = new UpdateDownloadResult();

            Assert.Null(result.Error);
        }

        /// <summary>
        /// Verifies that a new instance has WasCancelled initialized to false.
        /// </summary>
        [Fact]
        public void NewInstance_HasFalseWasCancelled()
        {
            var result = new UpdateDownloadResult();

            Assert.False(result.WasCancelled);
        }

        /// <summary>
        /// Verifies that a new instance has BytesDownloaded initialized to zero.
        /// </summary>
        [Fact]
        public void NewInstance_HasZeroBytesDownloaded()
        {
            var result = new UpdateDownloadResult();

            Assert.Equal(0, result.BytesDownloaded);
        }

        /// <summary>
        /// Verifies that a new instance has Update initialized to null.
        /// </summary>
        [Fact]
        public void NewInstance_HasNullUpdate()
        {
            var result = new UpdateDownloadResult();

            Assert.Null(result.Update);
        }

        #endregion

        #region Combination Tests

        /// <summary>
        /// Verifies that a successful result is not cancelled and has no error.
        /// </summary>
        [Fact]
        public void SuccessfulResult_IsNotCancelledOrFailed()
        {
            var update = new UpdateInfo();
            var result = UpdateDownloadResult.Successful("/tmp/file.zip", update, 1024);

            Assert.True(result.Success);
            Assert.False(result.WasCancelled);
            Assert.Null(result.Error);
        }

        /// <summary>
        /// Verifies that a failed result is not successful or cancelled.
        /// </summary>
        [Fact]
        public void FailedResult_IsNotSuccessfulOrCancelled()
        {
            var result = UpdateDownloadResult.Failed(new Exception());

            Assert.False(result.Success);
            Assert.False(result.WasCancelled);
            Assert.NotNull(result.Error);
        }

        /// <summary>
        /// Verifies that a cancelled result is not successful but has no error.
        /// </summary>
        [Fact]
        public void CancelledResult_IsNotSuccessfulButHasNoError()
        {
            var result = UpdateDownloadResult.Cancelled();

            Assert.False(result.Success);
            Assert.True(result.WasCancelled);
            Assert.Null(result.Error);
        }

        #endregion
    }
}
