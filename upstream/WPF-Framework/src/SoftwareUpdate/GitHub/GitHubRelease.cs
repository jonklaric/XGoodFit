using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;

namespace SoftwareUpdate.GitHub
{
    /// <summary>
    /// Represents a GitHub release from the GitHub API.
    /// </summary>
    internal class GitHubRelease
    {
        /// <summary>
        /// Gets or sets the unique identifier of the release.
        /// </summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the Git tag name for this release (e.g., "v1.0.0").
        /// </summary>
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        /// <summary>
        /// Gets or sets the display name of the release.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the release notes body in Markdown format.
        /// </summary>
        [JsonPropertyName("body")]
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a draft release.
        /// </summary>
        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a pre-release version.
        /// </summary>
        [JsonPropertyName("prerelease")]
        public bool PreRelease { get; set; }

        /// <summary>
        /// Gets or sets the ISO 8601 timestamp when the release was published.
        /// </summary>
        [JsonPropertyName("published_at")]
        public string? PublishedAt { get; set; }

        /// <summary>
        /// Gets or sets the URL to the release page on GitHub.
        /// </summary>
        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        /// <summary>
        /// Gets or sets the list of downloadable assets attached to this release.
        /// </summary>
        [JsonPropertyName("assets")]
        public List<GitHubReleaseAsset> Assets { get; set; } = new List<GitHubReleaseAsset>();

        /// <summary>
        /// Gets the published date as a DateTime.
        /// </summary>
        /// <returns>The published date, or <see cref="DateTime.MinValue"/> if parsing fails.</returns>
        public DateTime GetPublishedDateTime()
        {
            if (string.IsNullOrEmpty(PublishedAt))
                return DateTime.MinValue;

            // GitHub returns ISO 8601 timestamps, e.g. "2024-01-15T12:34:56Z"
            // TryParseExact is more precise and avoids locale-dependent parsing ambiguity.
            var iso8601Formats = new[]
            {
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mm:sszzz",
                "yyyy-MM-ddTHH:mm:ss.fffZ",
                "yyyy-MM-ddTHH:mm:ss.fffzzz"
            };

            if (DateTime.TryParseExact(PublishedAt, iso8601Formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var date))
            {
                return date;
            }

            return DateTime.MinValue;
        }
    }
}
