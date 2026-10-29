namespace SoftwareUpdate
{
    /// <summary>
    /// Represents the progress of an update download operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class UpdateDownloadProgress
    {
        /// <summary>
        /// Number of bytes in a kilobyte (1024 bytes).
        /// </summary>
        private const long BytesPerKB = 1024;

        /// <summary>
        /// Number of bytes in a megabyte (1024 KB).
        /// </summary>
        private const long BytesPerMB = BytesPerKB * 1024;

        /// <summary>
        /// Number of bytes in a gigabyte (1024 MB).
        /// </summary>
        private const long BytesPerGB = BytesPerMB * 1024;

        /// <summary>
        /// Gets or sets the number of bytes downloaded so far.
        /// </summary>
        public long BytesDownloaded { get; set; }

        /// <summary>
        /// Gets or sets the total number of bytes to download.
        /// May be -1 if the total size is unknown.
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets the download progress as a percentage (0-100).
        /// Returns -1 if total size is unknown.
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                if (TotalBytes <= 0) return -1;
                return (double)BytesDownloaded / TotalBytes * 100.0;
            }
        }

        /// <summary>
        /// Gets or sets the current download speed in bytes per second.
        /// </summary>
        public double BytesPerSecond { get; set; }

        /// <summary>
        /// Gets a formatted string of the download progress.
        /// </summary>
        public string ProgressText
        {
            get
            {
                var downloaded = FormatBytes(BytesDownloaded);
                if (TotalBytes > 0)
                {
                    var total = FormatBytes(TotalBytes);
                    return $"{downloaded} / {total} ({ProgressPercentage:F0}%)";
                }
                return downloaded;
            }
        }

        /// <summary>
        /// Gets a formatted string of the download speed.
        /// </summary>
        public string SpeedText
        {
            get
            {
                if (BytesPerSecond <= 0) return "";
                return $"{FormatBytes((long)BytesPerSecond)}/s";
            }
        }

        /// <summary>
        /// Formats a byte count into a human-readable string.
        /// </summary>
        /// <param name="bytes">The number of bytes to format.</param>
        /// <returns>A human-readable string representation (e.g., "1.5 MB").</returns>
        private static string FormatBytes(long bytes)
        {
            if (bytes < BytesPerKB) return $"{bytes} B";
            if (bytes < BytesPerMB) return $"{bytes / (double)BytesPerKB:F1} KB";
            if (bytes < BytesPerGB) return $"{bytes / (double)BytesPerMB:F1} MB";
            return $"{bytes / (double)BytesPerGB:F2} GB";
        }
    }
}
