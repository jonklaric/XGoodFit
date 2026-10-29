using Xunit;
using SoftwareUpdate;

namespace SoftwareUpdate.Tests.Core
{
    /// <summary>
    /// Provides unit tests for the <see cref="UpdateDownloadProgress"/> class, verifying progress tracking,
    /// percentage calculations, and text formatting for download progress and speed.
    /// </summary>
    public class UpdateDownloadProgressTests
    {
        #region Property Tests

        /// <summary>
        /// Verifies that the BytesDownloaded property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void BytesDownloaded_CanBeSetAndRetrieved()
        {
            var progress = new UpdateDownloadProgress { BytesDownloaded = 1024 };

            Assert.Equal(1024, progress.BytesDownloaded);
        }

        /// <summary>
        /// Verifies that the TotalBytes property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void TotalBytes_CanBeSetAndRetrieved()
        {
            var progress = new UpdateDownloadProgress { TotalBytes = 2048 };

            Assert.Equal(2048, progress.TotalBytes);
        }

        /// <summary>
        /// Verifies that the BytesPerSecond property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void BytesPerSecond_CanBeSetAndRetrieved()
        {
            var progress = new UpdateDownloadProgress { BytesPerSecond = 1024 * 1024 };

            Assert.Equal(1024 * 1024, progress.BytesPerSecond);
        }

        #endregion

        #region ProgressPercentage Tests

        /// <summary>
        /// Verifies that ProgressPercentage returns -1 when TotalBytes is zero.
        /// </summary>
        [Fact]
        public void ProgressPercentage_WhenTotalBytesZero_ReturnsNegativeOne()
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = 1024,
                TotalBytes = 0
            };

            Assert.Equal(-1, progress.ProgressPercentage);
        }

        /// <summary>
        /// Verifies that ProgressPercentage returns -1 when TotalBytes is negative.
        /// </summary>
        [Fact]
        public void ProgressPercentage_WhenTotalBytesNegative_ReturnsNegativeOne()
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = 1024,
                TotalBytes = -1
            };

            Assert.Equal(-1, progress.ProgressPercentage);
        }

        /// <summary>
        /// Verifies that ProgressPercentage returns 50.0 when the download is half complete.
        /// </summary>
        [Fact]
        public void ProgressPercentage_WhenHalfComplete_ReturnsFifty()
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = 500,
                TotalBytes = 1000
            };

            Assert.Equal(50.0, progress.ProgressPercentage);
        }

        /// <summary>
        /// Verifies that ProgressPercentage returns 100.0 when the download is complete.
        /// </summary>
        [Fact]
        public void ProgressPercentage_WhenComplete_ReturnsHundred()
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = 1000,
                TotalBytes = 1000
            };

            Assert.Equal(100.0, progress.ProgressPercentage);
        }

        /// <summary>
        /// Verifies that ProgressPercentage returns 0.0 when the download has not started.
        /// </summary>
        [Fact]
        public void ProgressPercentage_WhenNotStarted_ReturnsZero()
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = 0,
                TotalBytes = 1000
            };

            Assert.Equal(0.0, progress.ProgressPercentage);
        }

        /// <summary>
        /// Verifies that ProgressPercentage calculates correctly for various download and total byte combinations.
        /// </summary>
        /// <param name="downloaded">The number of bytes downloaded.</param>
        /// <param name="total">The total number of bytes.</param>
        /// <param name="expected">The expected progress percentage.</param>
        [Theory]
        [InlineData(250, 1000, 25.0)]
        [InlineData(750, 1000, 75.0)]
        [InlineData(333, 1000, 33.3)]
        [InlineData(1, 3, 33.333333333333336)]
        public void ProgressPercentage_CalculatesCorrectly(long downloaded, long total, double expected)
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = downloaded,
                TotalBytes = total
            };

            Assert.Equal(expected, progress.ProgressPercentage, precision: 10);
        }

        #endregion

        #region ProgressText Tests

        /// <summary>
        /// Verifies that ProgressText includes all relevant information when the total size is known.
        /// </summary>
        [Fact]
        public void ProgressText_WhenTotalKnown_IncludesAllInfo()
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = 512 * 1024, // 512 KB
                TotalBytes = 1024 * 1024 // 1 MB
            };

            var text = progress.ProgressText;

            Assert.Contains("512", text);
            Assert.Contains("KB", text);
            Assert.Contains("1", text);
            Assert.Contains("MB", text);
            Assert.Contains("50%", text);
        }

        /// <summary>
        /// Verifies that ProgressText only shows downloaded bytes when total size is unknown.
        /// </summary>
        [Fact]
        public void ProgressText_WhenTotalUnknown_ShowsOnlyDownloaded()
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = 1024 * 1024,
                TotalBytes = 0
            };

            var text = progress.ProgressText;

            Assert.Contains("1", text);
            Assert.Contains("MB", text);
            Assert.DoesNotContain("/", text);
            Assert.DoesNotContain("%", text);
        }

        /// <summary>
        /// Verifies that ProgressText formats small byte values correctly as bytes.
        /// </summary>
        [Fact]
        public void ProgressText_SmallBytes_FormatsAsBytes()
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = 500,
                TotalBytes = 0
            };

            var text = progress.ProgressText;

            Assert.Contains("500 B", text);
        }

        /// <summary>
        /// Verifies that ProgressText formats kilobyte values correctly.
        /// </summary>
        [Fact]
        public void ProgressText_Kilobytes_FormatsAsKB()
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = 2048,
                TotalBytes = 0
            };

            var text = progress.ProgressText;

            Assert.Contains("KB", text);
        }

        /// <summary>
        /// Verifies that ProgressText formats megabyte values correctly.
        /// </summary>
        [Fact]
        public void ProgressText_Megabytes_FormatsAsMB()
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = 5 * 1024 * 1024,
                TotalBytes = 0
            };

            var text = progress.ProgressText;

            Assert.Contains("5.0 MB", text);
        }

        /// <summary>
        /// Verifies that ProgressText formats gigabyte values correctly.
        /// </summary>
        [Fact]
        public void ProgressText_Gigabytes_FormatsAsGB()
        {
            var progress = new UpdateDownloadProgress
            {
                BytesDownloaded = 2L * 1024 * 1024 * 1024,
                TotalBytes = 0
            };

            var text = progress.ProgressText;

            Assert.Contains("GB", text);
        }

        #endregion

        #region SpeedText Tests

        /// <summary>
        /// Verifies that SpeedText returns an empty string when BytesPerSecond is zero.
        /// </summary>
        [Fact]
        public void SpeedText_WhenZero_ReturnsEmpty()
        {
            var progress = new UpdateDownloadProgress { BytesPerSecond = 0 };

            Assert.Equal("", progress.SpeedText);
        }

        /// <summary>
        /// Verifies that SpeedText returns an empty string when BytesPerSecond is negative.
        /// </summary>
        [Fact]
        public void SpeedText_WhenNegative_ReturnsEmpty()
        {
            var progress = new UpdateDownloadProgress { BytesPerSecond = -100 };

            Assert.Equal("", progress.SpeedText);
        }

        /// <summary>
        /// Verifies that SpeedText includes "/s" suffix when BytesPerSecond is positive.
        /// </summary>
        [Fact]
        public void SpeedText_WhenPositive_IncludesPerSecond()
        {
            var progress = new UpdateDownloadProgress { BytesPerSecond = 1024 * 1024 };

            var text = progress.SpeedText;

            Assert.Contains("/s", text);
        }

        /// <summary>
        /// Verifies that SpeedText formats low speeds as bytes per second.
        /// </summary>
        [Fact]
        public void SpeedText_LowSpeed_FormatsAsBytes()
        {
            var progress = new UpdateDownloadProgress { BytesPerSecond = 500 };

            var text = progress.SpeedText;

            Assert.Contains("B/s", text);
        }

        /// <summary>
        /// Verifies that SpeedText formats medium speeds as kilobytes per second.
        /// </summary>
        [Fact]
        public void SpeedText_MediumSpeed_FormatsAsKB()
        {
            var progress = new UpdateDownloadProgress { BytesPerSecond = 50 * 1024 };

            var text = progress.SpeedText;

            Assert.Contains("KB/s", text);
        }

        /// <summary>
        /// Verifies that SpeedText formats high speeds as megabytes per second.
        /// </summary>
        [Fact]
        public void SpeedText_HighSpeed_FormatsAsMB()
        {
            var progress = new UpdateDownloadProgress { BytesPerSecond = 10 * 1024 * 1024 };

            var text = progress.SpeedText;

            Assert.Contains("MB/s", text);
        }

        /// <summary>
        /// Verifies that SpeedText formats very high speeds as gigabytes per second.
        /// </summary>
        [Fact]
        public void SpeedText_VeryHighSpeed_FormatsAsGB()
        {
            var progress = new UpdateDownloadProgress { BytesPerSecond = 2L * 1024 * 1024 * 1024 };

            var text = progress.SpeedText;

            Assert.Contains("GB/s", text);
        }

        #endregion

        #region Default Values Tests

        /// <summary>
        /// Verifies that a new instance has BytesDownloaded initialized to zero.
        /// </summary>
        [Fact]
        public void NewInstance_HasZeroBytesDownloaded()
        {
            var progress = new UpdateDownloadProgress();

            Assert.Equal(0, progress.BytesDownloaded);
        }

        /// <summary>
        /// Verifies that a new instance has TotalBytes initialized to zero.
        /// </summary>
        [Fact]
        public void NewInstance_HasZeroTotalBytes()
        {
            var progress = new UpdateDownloadProgress();

            Assert.Equal(0, progress.TotalBytes);
        }

        /// <summary>
        /// Verifies that a new instance has BytesPerSecond initialized to zero.
        /// </summary>
        [Fact]
        public void NewInstance_HasZeroBytesPerSecond()
        {
            var progress = new UpdateDownloadProgress();

            Assert.Equal(0, progress.BytesPerSecond);
        }

        #endregion
    }
}
