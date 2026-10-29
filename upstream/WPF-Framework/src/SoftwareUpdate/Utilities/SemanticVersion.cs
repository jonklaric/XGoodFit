using System;
using System.Text.RegularExpressions;

namespace SoftwareUpdate
{
    /// <summary>
    /// Represents a semantic version number following the SemVer 2.0 specification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Semantic versioning uses the format MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD].
    /// Examples: 1.0.0, 2.1.0-beta.1, 3.0.0-alpha+build.123
    /// </para>
    /// <para>
    /// Pre-release versions have lower precedence than normal versions.
    /// For example: 1.0.0-alpha &lt; 1.0.0-beta &lt; 1.0.0
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        // Regex pattern for semantic version parsing.
        // Pre-release identifiers (per SemVer 2.0 §9) are dot-separated tokens; numeric tokens
        // MUST NOT include leading zeros (so "1.0.0-alpha.01" is invalid). Build metadata
        // (§10) keeps the broader [0-9A-Za-z-] alphabet — leading zeros are allowed there.
        // Each pre-release identifier must be either a non-zero numeric ("0", or [1-9][0-9]*)
        // or contain at least one non-digit character.
        private const string PreReleaseIdentifier = @"(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*)";
        private static readonly Regex VersionPattern = new Regex(
            @"^v?(?<major>\d+)\.(?<minor>\d+)(?:\.(?<patch>\d+))?(?:-(?<prerelease>" + PreReleaseIdentifier + @"(?:\." + PreReleaseIdentifier + @")*))?(?:\+(?<build>[0-9A-Za-z\-]+(?:\.[0-9A-Za-z\-]+)*))?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the major version number.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Gets the minor version number.
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Gets the patch version number.
        /// </summary>
        public int Patch { get; }

        /// <summary>
        /// Gets the pre-release identifier (e.g., "alpha", "beta.1").
        /// </summary>
        public string? PreRelease { get; }

        /// <summary>
        /// Gets the build metadata.
        /// </summary>
        public string? BuildMetadata { get; }

        /// <summary>
        /// Gets whether this is a pre-release version.
        /// </summary>
        public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);

        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticVersion"/> class.
        /// </summary>
        /// <param name="major">The major version number.</param>
        /// <param name="minor">The minor version number.</param>
        /// <param name="patch">The patch version number.</param>
        /// <param name="preRelease">The pre-release identifier.</param>
        /// <param name="buildMetadata">The build metadata.</param>
        public SemanticVersion(int major, int minor, int patch = 0, string? preRelease = null, string? buildMetadata = null)
        {
            if (major < 0) throw new ArgumentOutOfRangeException(nameof(major));
            if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor));
            if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch));

            Major = major;
            Minor = minor;
            Patch = patch;
            PreRelease = preRelease;
            BuildMetadata = buildMetadata;
        }

        /// <summary>
        /// Parses a version string into a <see cref="SemanticVersion"/>.
        /// </summary>
        /// <param name="versionString">The version string to parse.</param>
        /// <returns>The parsed semantic version.</returns>
        /// <exception cref="FormatException">Thrown if the string is not a valid semantic version.</exception>
        public static SemanticVersion Parse(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                throw new ArgumentNullException(nameof(versionString));

            if (TryParse(versionString, out var version))
                return version!;

            throw new FormatException($"'{versionString}' is not a valid semantic version.");
        }

        /// <summary>
        /// Attempts to parse a version string into a <see cref="SemanticVersion"/>.
        /// </summary>
        /// <param name="versionString">The version string to parse.</param>
        /// <param name="version">The parsed version, or null if parsing failed.</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
        public static bool TryParse(string versionString, out SemanticVersion? version)
        {
            version = null;

            if (string.IsNullOrWhiteSpace(versionString))
                return false;

            var match = VersionPattern.Match(versionString.Trim());
            if (!match.Success)
                return false;

            // Use int.TryParse instead of int.Parse to handle overflow/malformed input gracefully
            // (the regex ensures only digits are present, but version numbers could exceed int.MaxValue)
            if (!int.TryParse(match.Groups["major"].Value, out var major))
                return false;

            if (!int.TryParse(match.Groups["minor"].Value, out var minor))
                return false;

            var patch = 0;
            if (match.Groups["patch"].Success && !int.TryParse(match.Groups["patch"].Value, out patch))
                return false;

            var preRelease = match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : null;
            var build = match.Groups["build"].Success ? match.Groups["build"].Value : null;

            // SemVer 2.0 §9: numeric identifiers in the pre-release portion MUST NOT include
            // leading zeros. The base regex accepts the broader `[0-9A-Za-z-.]+` character
            // class, so re-validate any all-digit dot-separated sub-identifiers here. (We do
            // not enforce this for build metadata per §10, which permits leading zeros.)
            if (preRelease != null)
            {
                foreach (var part in preRelease.Split('.'))
                {
                    // Empty segments (e.g. "alpha..1") are invalid per §9.
                    if (part.Length == 0) return false;
                    // Numeric-only segment longer than 1 character starting with '0' is invalid.
                    bool isAllDigits = true;
                    for (int i = 0; i < part.Length; i++)
                    {
                        if (!char.IsDigit(part[i])) { isAllDigits = false; break; }
                    }
                    if (isAllDigits && part.Length > 1 && part[0] == '0') return false;
                }
            }

            version = new SemanticVersion(major, minor, patch, preRelease, build);
            return true;
        }

        /// <summary>
        /// Creates a <see cref="SemanticVersion"/> from a <see cref="System.Version"/>.
        /// </summary>
        /// <param name="version">The System.Version to convert.</param>
        /// <returns>A SemanticVersion instance.</returns>
        public static SemanticVersion FromVersion(Version version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));
            return new SemanticVersion(
                version.Major,
                Math.Max(0, version.Minor),
                Math.Max(0, version.Build));
        }

        /// <summary>
        /// Compares this version to another.
        /// </summary>
        /// <param name="other">The version to compare to.</param>
        /// <returns>A value indicating the relative order of the versions.</returns>
        public int CompareTo(SemanticVersion? other)
        {
            if (other == null) return 1;

            // Compare major.minor.patch
            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;

            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;

            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;

            // Pre-release versions have lower precedence
            if (IsPreRelease && !other.IsPreRelease) return -1;
            if (!IsPreRelease && other.IsPreRelease) return 1;

            // Compare pre-release identifiers
            if (IsPreRelease && other.IsPreRelease)
            {
                return ComparePreRelease(PreRelease!, other.PreRelease!);
            }

            return 0;
        }

        /// <summary>
        /// Compares two pre-release version strings according to semantic versioning rules.
        /// </summary>
        /// <param name="a">The first pre-release string to compare.</param>
        /// <param name="b">The second pre-release string to compare.</param>
        /// <returns>A negative value if <paramref name="a"/> is less than <paramref name="b"/>,
        /// zero if they are equal, or a positive value if <paramref name="a"/> is greater than <paramref name="b"/>.</returns>
        private static int ComparePreRelease(string a, string b)
        {
            var partsA = a.Split('.');
            var partsB = b.Split('.');

            var maxParts = Math.Max(partsA.Length, partsB.Length);
            for (int i = 0; i < maxParts; i++)
            {
                if (i >= partsA.Length) return -1;
                if (i >= partsB.Length) return 1;

                var partA = partsA[i];
                var partB = partsB[i];

                var aIsNum = int.TryParse(partA, out var numA);
                var bIsNum = int.TryParse(partB, out var numB);

                if (aIsNum && bIsNum)
                {
                    var cmp = numA.CompareTo(numB);
                    if (cmp != 0) return cmp;
                }
                else if (aIsNum)
                {
                    return -1; // Numeric has lower precedence
                }
                else if (bIsNum)
                {
                    return 1;
                }
                else
                {
                    // SemVer 2.0 section 11.4.4: pre-release identifiers are compared in ASCII
                    // sort order, i.e. case-sensitive. Using OrdinalIgnoreCase would treat
                    // "Beta" and "beta" as equal, which is non-conformant.
                    var cmp = string.Compare(partA, partB, StringComparison.Ordinal);
                    if (cmp != 0) return cmp;
                }
            }

            return 0;
        }

        /// <inheritdoc/>
        public bool Equals(SemanticVersion? other)
        {
            if (other == null) return false;
            return CompareTo(other) == 0;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as SemanticVersion);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + Major;
                hash = hash * 31 + Minor;
                hash = hash * 31 + Patch;
                hash = hash * 31 + (PreRelease?.ToLowerInvariant().GetHashCode() ?? 0);
                return hash;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var result = $"{Major}.{Minor}.{Patch}";
            if (!string.IsNullOrEmpty(PreRelease))
                result += $"-{PreRelease}";
            if (!string.IsNullOrEmpty(BuildMetadata))
                result += $"+{BuildMetadata}";
            return result;
        }

        /// <summary>
        /// Returns the version string with a "v" prefix.
        /// </summary>
        public string ToTagString() => $"v{this}";

        /// <summary>
        /// Determines whether two <see cref="SemanticVersion"/> instances are equal.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if the versions are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(SemanticVersion? left, SemanticVersion? right)
        {
            if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="SemanticVersion"/> instances are not equal.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if the versions are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(SemanticVersion? left, SemanticVersion? right) => !(left == right);

        /// <summary>
        /// Determines whether the left <see cref="SemanticVersion"/> is less than the right.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator <(SemanticVersion? left, SemanticVersion? right)
        {
            if (left == null) return right != null;
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the left <see cref="SemanticVersion"/> is greater than the right.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator >(SemanticVersion? left, SemanticVersion? right)
        {
            if (left == null) return false;
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the left <see cref="SemanticVersion"/> is less than or equal to the right.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator <=(SemanticVersion? left, SemanticVersion? right) => !(left > right);

        /// <summary>
        /// Determines whether the left <see cref="SemanticVersion"/> is greater than or equal to the right.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator >=(SemanticVersion? left, SemanticVersion? right) => !(left < right);
    }
}
