using System.Reflection;
using SoftwareUpdate.GitHub;
using Xunit;

namespace SoftwareUpdate.Tests.GitHub
{
    /// <summary>
    /// Regression tests pinning the SHA256 release-notes extraction (F-002).
    ///
    /// The audit flagged that <c>UpdateInfo.Sha256Checksum</c> was always null because
    /// no parsing path existed in <see cref="GitHubUpdateService"/>. The fix added a
    /// private static <c>ExtractSha256Checksum</c> helper that scans the release body
    /// for the documented <c>SHA256: &lt;hex&gt;</c> token. These tests exercise that
    /// helper via reflection so accidental removal or regex regression is caught.
    /// </summary>
    public class Sha256ExtractionTests
    {
        /// <summary>
        /// Invokes the private <c>ExtractSha256Checksum(string?)</c> helper via reflection.
        /// </summary>
        private static string? Invoke(string? releaseBody)
        {
            var method = typeof(GitHubUpdateService).GetMethod(
                "ExtractSha256Checksum",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return (string?)method!.Invoke(null, new object?[] { releaseBody });
        }

        [Fact]
        public void Sha256ChecksumIsExtractedFromReleaseNotes()
        {
            // Arrange — typical release body containing the documented SHA256 token.
            var hex = new string('a', 64);
            var body = "Release v1.2.3\n\nSHA256: " + hex + "\n";

            // Act
            var result = Invoke(body);

            // Assert
            Assert.Equal(hex, result);
        }

        [Fact]
        public void Sha256IsExtractedWhenSurroundedByOtherText()
        {
            var hex = "0123456789abcdefABCDEF" + new string('1', 42); // 64 chars
            var body = "## Changes\n\n- Improved foo\n- Fixed bar\n\nSHA256: " + hex + "\n\nThanks!";

            var result = Invoke(body);

            Assert.Equal(hex.ToLowerInvariant(), result);
        }

        [Fact]
        public void Sha256ExtractionReturnsNullForEmptyBody()
        {
            Assert.Null(Invoke(null));
            Assert.Null(Invoke(string.Empty));
        }

        [Fact]
        public void Sha256ExtractionReturnsNullWhenTokenMissing()
        {
            var body = "Release notes that do not contain a checksum line.";
            Assert.Null(Invoke(body));
        }

        [Fact]
        public void Sha256ExtractionRejectsShortHex()
        {
            // 32 hex chars instead of 64 — must not match.
            var body = "SHA256: " + new string('a', 32);
            Assert.Null(Invoke(body));
        }

        [Fact]
        public void Sha256ExtractionResultIsLowercase()
        {
            var hex = new string('A', 64);
            var body = "SHA256: " + hex;
            var result = Invoke(body);
            Assert.Equal(new string('a', 64), result);
        }
    }
}
