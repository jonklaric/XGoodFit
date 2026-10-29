using System.Text.Json.Serialization;

namespace SoftwareUpdate.GitHub
{
    /// <summary>
    /// Represents a downloadable asset attached to a GitHub release.
    /// </summary>
    internal class GitHubReleaseAsset
    {
        /// <summary>
        /// Gets or sets the unique identifier of the asset.
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the file name of the asset.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the asset (e.g., "application/zip").
        /// </summary>
        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [JsonPropertyName("size")]
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the number of times this asset has been downloaded.
        /// </summary>
        [JsonPropertyName("download_count")]
        public int DownloadCount { get; set; }

        /// <summary>
        /// Gets or sets the direct download URL for this asset.
        /// </summary>
        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}
