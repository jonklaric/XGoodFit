using System;

namespace SoftwareUpdate
{
    /// <summary>
    /// Contains information about an available software update.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class UpdateInfo
    {
        /// <summary>
        /// Gets or sets the version of the update.
        /// </summary>
        public SemanticVersion? Version { get; set; }

        /// <summary>
        /// Gets or sets the display name of the release (e.g., "v2.0.0").
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the URL to download the update package.
        /// </summary>
        /// <remarks>
        /// Ideally this would be typed as <see cref="Uri"/> to enforce well-formed URLs at the
        /// type level, but it is kept as <see cref="string"/> to avoid cascading changes across
        /// all call sites that pass it directly to <see cref="System.Net.Http.HttpClient"/> and
        /// <see cref="System.Uri"/> constructors.
        /// </remarks>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the release notes (typically in Markdown format).
        /// </summary>
        public string? ReleaseNotes { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the release was published.
        /// </summary>
        public DateTime PublishedAt { get; set; }

        /// <summary>
        /// Gets or sets the size of the download in bytes.
        /// </summary>
        public long DownloadSize { get; set; }

        /// <summary>
        /// Gets or sets the name of the downloadable asset file.
        /// </summary>
        public string? AssetName { get; set; }

        /// <summary>
        /// Gets or sets whether this is a pre-release version.
        /// </summary>
        public bool IsPreRelease { get; set; }

        /// <summary>
        /// Gets or sets the URL to the release page on GitHub.
        /// </summary>
        public string? ReleasePageUrl { get; set; }

        /// <summary>
        /// Gets or sets the optional SHA256 checksum for verification.
        /// </summary>
        public string? Sha256Checksum { get; set; }

        /// <summary>
        /// Returns a string representation of the update info.
        /// </summary>
        /// <returns>A formatted string showing name, version, and download size.</returns>
        public override string ToString()
        {
            if (DownloadSize <= 0)
            {
                return $"{Name} ({Version})";
            }
            return $"{Name} ({Version}) - {DownloadSize / 1024.0 / 1024.0:F1} MB";
        }
    }
}
