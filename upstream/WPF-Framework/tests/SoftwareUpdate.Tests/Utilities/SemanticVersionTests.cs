using Xunit;
using SoftwareUpdate;

namespace SoftwareUpdate.Tests.Utilities
{
    /// <summary>
    /// Provides comprehensive unit tests for the <see cref="SemanticVersion"/> class, verifying
    /// constructors, parsing, comparison, equality, operators, and string formatting for semantic versioning.
    /// </summary>
    public class SemanticVersionTests
    {
        #region Constructor Tests

        /// <summary>
        /// Verifies that the constructor sets Major, Minor, and Patch properties correctly.
        /// </summary>
        [Fact]
        public void Constructor_SetsProperties()
        {
            var version = new SemanticVersion(1, 2, 3);

            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Patch);
            Assert.Null(version.PreRelease);
            Assert.Null(version.BuildMetadata);
        }

        /// <summary>
        /// Verifies that the constructor accepts a prerelease identifier.
        /// </summary>
        [Fact]
        public void Constructor_WithPreRelease()
        {
            var version = new SemanticVersion(1, 0, 0, "alpha");

            Assert.Equal("alpha", version.PreRelease);
            Assert.True(version.IsPreRelease);
        }

        /// <summary>
        /// Verifies that the constructor accepts build metadata.
        /// </summary>
        [Fact]
        public void Constructor_WithBuildMetadata()
        {
            var version = new SemanticVersion(1, 0, 0, null, "build.123");

            Assert.Equal("build.123", version.BuildMetadata);
        }

        /// <summary>
        /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException"/> when major version is negative.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when major version is negative.</exception>
        [Fact]
        public void Constructor_NegativeMajor_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SemanticVersion(-1, 0, 0));
        }

        /// <summary>
        /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException"/> when minor version is negative.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when minor version is negative.</exception>
        [Fact]
        public void Constructor_NegativeMinor_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SemanticVersion(1, -1, 0));
        }

        /// <summary>
        /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException"/> when patch version is negative.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when patch version is negative.</exception>
        [Fact]
        public void Constructor_NegativePatch_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SemanticVersion(1, 0, -1));
        }

        /// <summary>
        /// Verifies that the constructor defaults the patch version to zero when not specified.
        /// </summary>
        [Fact]
        public void Constructor_DefaultPatchIsZero()
        {
            var version = new SemanticVersion(1, 2);

            Assert.Equal(0, version.Patch);
        }

        #endregion

        #region Parse Tests

        /// <summary>
        /// Verifies that the Parse method correctly parses valid version strings.
        /// </summary>
        /// <param name="input">The version string to parse.</param>
        /// <param name="expectedMajor">The expected major version number.</param>
        /// <param name="expectedMinor">The expected minor version number.</param>
        /// <param name="expectedPatch">The expected patch version number.</param>
        [Theory]
        [InlineData("1.0.0", 1, 0, 0)]
        [InlineData("2.1.3", 2, 1, 3)]
        [InlineData("10.20.30", 10, 20, 30)]
        [InlineData("1.0", 1, 0, 0)]
        public void Parse_ValidVersionStrings(string input, int expectedMajor, int expectedMinor, int expectedPatch)
        {
            var version = SemanticVersion.Parse(input);

            Assert.Equal(expectedMajor, version.Major);
            Assert.Equal(expectedMinor, version.Minor);
            Assert.Equal(expectedPatch, version.Patch);
        }

        /// <summary>
        /// Verifies that the Parse method handles version strings with a "v" or "V" prefix.
        /// </summary>
        /// <param name="input">The version string to parse.</param>
        /// <param name="expectedMajor">The expected major version number.</param>
        /// <param name="expectedMinor">The expected minor version number.</param>
        /// <param name="expectedPatch">The expected patch version number.</param>
        [Theory]
        [InlineData("v1.0.0", 1, 0, 0)]
        [InlineData("V2.1.3", 2, 1, 3)]
        public void Parse_WithVPrefix(string input, int expectedMajor, int expectedMinor, int expectedPatch)
        {
            var version = SemanticVersion.Parse(input);

            Assert.Equal(expectedMajor, version.Major);
            Assert.Equal(expectedMinor, version.Minor);
            Assert.Equal(expectedPatch, version.Patch);
        }

        /// <summary>
        /// Verifies that the Parse method correctly extracts prerelease identifiers.
        /// </summary>
        /// <param name="input">The version string to parse.</param>
        /// <param name="expectedPreRelease">The expected prerelease identifier.</param>
        [Theory]
        [InlineData("1.0.0-alpha", "alpha")]
        [InlineData("1.0.0-beta.1", "beta.1")]
        [InlineData("1.0.0-rc.1.2", "rc.1.2")]
        public void Parse_WithPreRelease(string input, string expectedPreRelease)
        {
            var version = SemanticVersion.Parse(input);

            Assert.Equal(expectedPreRelease, version.PreRelease);
        }

        /// <summary>
        /// Verifies that the Parse method correctly extracts build metadata.
        /// </summary>
        /// <param name="input">The version string to parse.</param>
        /// <param name="expectedBuild">The expected build metadata.</param>
        [Theory]
        [InlineData("1.0.0+build.123", "build.123")]
        [InlineData("1.0.0+20130313144700", "20130313144700")]
        public void Parse_WithBuildMetadata(string input, string expectedBuild)
        {
            var version = SemanticVersion.Parse(input);

            Assert.Equal(expectedBuild, version.BuildMetadata);
        }

        /// <summary>
        /// Verifies that the Parse method handles both prerelease and build metadata.
        /// </summary>
        [Fact]
        public void Parse_WithPreReleaseAndBuildMetadata()
        {
            var version = SemanticVersion.Parse("1.0.0-alpha+build.123");

            Assert.Equal("alpha", version.PreRelease);
            Assert.Equal("build.123", version.BuildMetadata);
        }

        /// <summary>
        /// Verifies that the Parse method throws <see cref="ArgumentNullException"/> when given a null string.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        [Fact]
        public void Parse_NullString_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SemanticVersion.Parse(null!));
        }

        /// <summary>
        /// Verifies that the Parse method throws <see cref="ArgumentNullException"/> when given an empty string.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when input is empty.</exception>
        [Fact]
        public void Parse_EmptyString_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SemanticVersion.Parse(string.Empty));
        }

        /// <summary>
        /// Verifies that the Parse method throws <see cref="FormatException"/> for invalid version strings.
        /// </summary>
        /// <param name="input">The invalid version string.</param>
        /// <exception cref="FormatException">Thrown when the version string format is invalid.</exception>
        [Theory]
        [InlineData("invalid")]
        [InlineData("1")]
        [InlineData("1.0.0.0")]
        [InlineData("a.b.c")]
        public void Parse_InvalidFormat_ThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(() => SemanticVersion.Parse(input));
        }

        #endregion

        #region TryParse Tests

        /// <summary>
        /// Verifies that TryParse returns true and outputs a valid version for valid input.
        /// </summary>
        [Fact]
        public void TryParse_ValidString_ReturnsTrue()
        {
            var result = SemanticVersion.TryParse("1.0.0", out var version);

            Assert.True(result);
            Assert.NotNull(version);
            Assert.Equal(1, version!.Major);
        }

        /// <summary>
        /// Verifies that TryParse returns false for invalid input.
        /// </summary>
        [Fact]
        public void TryParse_InvalidString_ReturnsFalse()
        {
            var result = SemanticVersion.TryParse("invalid", out var version);

            Assert.False(result);
            Assert.Null(version);
        }

        /// <summary>
        /// Verifies that TryParse returns false for null input.
        /// </summary>
        [Fact]
        public void TryParse_NullString_ReturnsFalse()
        {
            var result = SemanticVersion.TryParse(null!, out var version);

            Assert.False(result);
            Assert.Null(version);
        }

        /// <summary>
        /// Verifies that TryParse returns false for empty string input.
        /// </summary>
        [Fact]
        public void TryParse_EmptyString_ReturnsFalse()
        {
            var result = SemanticVersion.TryParse(string.Empty, out var version);

            Assert.False(result);
            Assert.Null(version);
        }

        #endregion

        #region FromVersion Tests

        /// <summary>
        /// Verifies that FromVersion converts a System.Version to SemanticVersion correctly.
        /// </summary>
        [Fact]
        public void FromVersion_ConvertsSystemVersion()
        {
            var systemVersion = new Version(1, 2, 3);
            var semver = SemanticVersion.FromVersion(systemVersion);

            Assert.Equal(1, semver.Major);
            Assert.Equal(2, semver.Minor);
            Assert.Equal(3, semver.Patch);
        }

        /// <summary>
        /// Verifies that FromVersion throws <see cref="ArgumentNullException"/> when given a null version.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when version is null.</exception>
        [Fact]
        public void FromVersion_NullVersion_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SemanticVersion.FromVersion(null!));
        }

        /// <summary>
        /// Verifies that FromVersion treats a negative build number as zero.
        /// </summary>
        [Fact]
        public void FromVersion_HandlesNegativeBuildAsZero()
        {
            var systemVersion = new Version(1, 2);
            var semver = SemanticVersion.FromVersion(systemVersion);

            Assert.Equal(0, semver.Patch);
        }

        #endregion

        #region CompareTo Tests

        /// <summary>
        /// Verifies that CompareTo returns a positive value when compared to null.
        /// </summary>
        [Fact]
        public void CompareTo_NullReturnsPositive()
        {
            var version = new SemanticVersion(1, 0, 0);

            Assert.True(version.CompareTo(null) > 0);
        }

        /// <summary>
        /// Verifies that CompareTo returns zero for equal versions.
        /// </summary>
        [Fact]
        public void CompareTo_EqualVersionsReturnsZero()
        {
            var v1 = new SemanticVersion(1, 2, 3);
            var v2 = new SemanticVersion(1, 2, 3);

            Assert.Equal(0, v1.CompareTo(v2));
        }

        /// <summary>
        /// Verifies that CompareTo returns a positive value when the version is greater.
        /// </summary>
        /// <param name="m1">Major version of first version.</param>
        /// <param name="mi1">Minor version of first version.</param>
        /// <param name="p1">Patch version of first version.</param>
        /// <param name="m2">Major version of second version.</param>
        /// <param name="mi2">Minor version of second version.</param>
        /// <param name="p2">Patch version of second version.</param>
        [Theory]
        [InlineData(2, 0, 0, 1, 0, 0)]
        [InlineData(1, 2, 0, 1, 1, 0)]
        [InlineData(1, 0, 2, 1, 0, 1)]
        public void CompareTo_GreaterVersionReturnsPositive(int m1, int mi1, int p1, int m2, int mi2, int p2)
        {
            var v1 = new SemanticVersion(m1, mi1, p1);
            var v2 = new SemanticVersion(m2, mi2, p2);

            Assert.True(v1.CompareTo(v2) > 0);
        }

        /// <summary>
        /// Verifies that CompareTo returns a negative value when the version is lesser.
        /// </summary>
        /// <param name="m1">Major version of first version.</param>
        /// <param name="mi1">Minor version of first version.</param>
        /// <param name="p1">Patch version of first version.</param>
        /// <param name="m2">Major version of second version.</param>
        /// <param name="mi2">Minor version of second version.</param>
        /// <param name="p2">Patch version of second version.</param>
        [Theory]
        [InlineData(1, 0, 0, 2, 0, 0)]
        [InlineData(1, 1, 0, 1, 2, 0)]
        [InlineData(1, 0, 1, 1, 0, 2)]
        public void CompareTo_LesserVersionReturnsNegative(int m1, int mi1, int p1, int m2, int mi2, int p2)
        {
            var v1 = new SemanticVersion(m1, mi1, p1);
            var v2 = new SemanticVersion(m2, mi2, p2);

            Assert.True(v1.CompareTo(v2) < 0);
        }

        /// <summary>
        /// Verifies that a prerelease version is considered less than a release version.
        /// </summary>
        [Fact]
        public void CompareTo_PreReleaseIsLessThanRelease()
        {
            var prerelease = new SemanticVersion(1, 0, 0, "alpha");
            var release = new SemanticVersion(1, 0, 0);

            Assert.True(prerelease.CompareTo(release) < 0);
            Assert.True(release.CompareTo(prerelease) > 0);
        }

        /// <summary>
        /// Verifies that prerelease versions are compared correctly.
        /// </summary>
        /// <param name="less">The lesser version string.</param>
        /// <param name="greater">The greater version string.</param>
        [Theory]
        [InlineData("1.0.0-alpha", "1.0.0-beta")]
        [InlineData("1.0.0-alpha.1", "1.0.0-alpha.2")]
        [InlineData("1.0.0-1", "1.0.0-2")]
        public void CompareTo_PreReleaseComparison(string less, string greater)
        {
            var v1 = SemanticVersion.Parse(less);
            var v2 = SemanticVersion.Parse(greater);

            Assert.True(v1.CompareTo(v2) < 0);
        }

        /// <summary>
        /// Verifies that numeric prerelease identifiers come before alphanumeric ones.
        /// </summary>
        [Fact]
        public void CompareTo_NumericPreReleaseComesBeforeAlphanumeric()
        {
            var numeric = SemanticVersion.Parse("1.0.0-1");
            var alpha = SemanticVersion.Parse("1.0.0-alpha");

            Assert.True(numeric.CompareTo(alpha) < 0);
        }

        /// <summary>
        /// Verifies that shorter prerelease identifiers are considered less than longer ones.
        /// </summary>
        [Fact]
        public void CompareTo_ShorterPreReleaseIsLess()
        {
            var shorter = SemanticVersion.Parse("1.0.0-alpha");
            var longer = SemanticVersion.Parse("1.0.0-alpha.1");

            Assert.True(shorter.CompareTo(longer) < 0);
        }

        #endregion

        #region Equality Tests

        /// <summary>
        /// Verifies that Equals returns true for identical versions.
        /// </summary>
        [Fact]
        public void Equals_SameVersionReturnsTrue()
        {
            var v1 = new SemanticVersion(1, 2, 3);
            var v2 = new SemanticVersion(1, 2, 3);

            Assert.True(v1.Equals(v2));
        }

        /// <summary>
        /// Verifies that Equals returns false for different versions.
        /// </summary>
        [Fact]
        public void Equals_DifferentVersionReturnsFalse()
        {
            var v1 = new SemanticVersion(1, 2, 3);
            var v2 = new SemanticVersion(1, 2, 4);

            Assert.False(v1.Equals(v2));
        }

        /// <summary>
        /// Verifies that Equals returns false when compared to null.
        /// </summary>
        [Fact]
        public void Equals_NullReturnsFalse()
        {
            var v1 = new SemanticVersion(1, 2, 3);

            Assert.False(v1.Equals(null));
        }

        /// <summary>
        /// Verifies that Equals returns true when comparing to an object of the same version.
        /// </summary>
        [Fact]
        public void Equals_Object_SameVersionReturnsTrue()
        {
            var v1 = new SemanticVersion(1, 2, 3);
            object v2 = new SemanticVersion(1, 2, 3);

            Assert.True(v1.Equals(v2));
        }

        /// <summary>
        /// Verifies that Equals returns false when comparing to a non-SemanticVersion object.
        /// </summary>
        [Fact]
        public void Equals_Object_NonSemanticVersionReturnsFalse()
        {
            var v1 = new SemanticVersion(1, 2, 3);

            Assert.False(v1.Equals("1.2.3"));
        }

        /// <summary>
        /// Verifies that identical versions produce the same hash code.
        /// </summary>
        [Fact]
        public void GetHashCode_SameVersionsSameHashCode()
        {
            var v1 = new SemanticVersion(1, 2, 3);
            var v2 = new SemanticVersion(1, 2, 3);

            Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        }

        /// <summary>
        /// Verifies that different versions produce different hash codes.
        /// </summary>
        [Fact]
        public void GetHashCode_DifferentVersionsDifferentHashCode()
        {
            var v1 = new SemanticVersion(1, 2, 3);
            var v2 = new SemanticVersion(1, 2, 4);

            Assert.NotEqual(v1.GetHashCode(), v2.GetHashCode());
        }

        #endregion

        #region Operator Tests

        /// <summary>
        /// Verifies that the equality operator returns true for equal versions.
        /// </summary>
        [Fact]
        public void OperatorEquals_EqualVersionsReturnsTrue()
        {
            var v1 = new SemanticVersion(1, 2, 3);
            var v2 = new SemanticVersion(1, 2, 3);

            Assert.True(v1 == v2);
        }

        /// <summary>
        /// Verifies that the equality operator returns true when both versions are null.
        /// </summary>
        [Fact]
        public void OperatorEquals_BothNullReturnsTrue()
        {
            SemanticVersion? v1 = null;
            SemanticVersion? v2 = null;

            Assert.True(v1 == v2);
        }

        /// <summary>
        /// Verifies that the equality operator returns false when left operand is null.
        /// </summary>
        [Fact]
        public void OperatorEquals_LeftNullReturnsFalse()
        {
            SemanticVersion? v1 = null;
            var v2 = new SemanticVersion(1, 2, 3);

            Assert.False(v1 == v2);
        }

        /// <summary>
        /// Verifies that the inequality operator returns true for different versions.
        /// </summary>
        [Fact]
        public void OperatorNotEquals_DifferentVersionsReturnsTrue()
        {
            var v1 = new SemanticVersion(1, 2, 3);
            var v2 = new SemanticVersion(1, 2, 4);

            Assert.True(v1 != v2);
        }

        /// <summary>
        /// Verifies that the less-than operator returns true for a lesser version.
        /// </summary>
        [Fact]
        public void OperatorLessThan_LesserVersionReturnsTrue()
        {
            var v1 = new SemanticVersion(1, 0, 0);
            var v2 = new SemanticVersion(2, 0, 0);

            Assert.True(v1 < v2);
        }

        /// <summary>
        /// Verifies that the less-than operator returns true when left operand is null.
        /// </summary>
        [Fact]
        public void OperatorLessThan_LeftNullReturnsTrue()
        {
            SemanticVersion? v1 = null;
            var v2 = new SemanticVersion(1, 0, 0);

            Assert.True(v1 < v2);
        }

        /// <summary>
        /// Verifies that the less-than operator returns false when both operands are null.
        /// </summary>
        [Fact]
        public void OperatorLessThan_BothNullReturnsFalse()
        {
            SemanticVersion? v1 = null;
            SemanticVersion? v2 = null;

            Assert.False(v1 < v2);
        }

        /// <summary>
        /// Verifies that the greater-than operator returns true for a greater version.
        /// </summary>
        [Fact]
        public void OperatorGreaterThan_GreaterVersionReturnsTrue()
        {
            var v1 = new SemanticVersion(2, 0, 0);
            var v2 = new SemanticVersion(1, 0, 0);

            Assert.True(v1 > v2);
        }

        /// <summary>
        /// Verifies that the greater-than operator returns false when left operand is null.
        /// </summary>
        [Fact]
        public void OperatorGreaterThan_LeftNullReturnsFalse()
        {
            SemanticVersion? v1 = null;
            var v2 = new SemanticVersion(1, 0, 0);

            Assert.False(v1 > v2);
        }

        /// <summary>
        /// Verifies that the less-than-or-equal operator returns true for equal versions.
        /// </summary>
        [Fact]
        public void OperatorLessThanOrEqual_EqualVersionsReturnsTrue()
        {
            var v1 = new SemanticVersion(1, 0, 0);
            var v2 = new SemanticVersion(1, 0, 0);

            Assert.True(v1 <= v2);
        }

        /// <summary>
        /// Verifies that the greater-than-or-equal operator returns true for equal versions.
        /// </summary>
        [Fact]
        public void OperatorGreaterThanOrEqual_EqualVersionsReturnsTrue()
        {
            var v1 = new SemanticVersion(1, 0, 0);
            var v2 = new SemanticVersion(1, 0, 0);

            Assert.True(v1 >= v2);
        }

        #endregion

        #region ToString Tests

        /// <summary>
        /// Verifies that ToString returns the correct string representation for a basic version.
        /// </summary>
        [Fact]
        public void ToString_BasicVersion()
        {
            var version = new SemanticVersion(1, 2, 3);

            Assert.Equal("1.2.3", version.ToString());
        }

        /// <summary>
        /// Verifies that ToString includes the prerelease identifier.
        /// </summary>
        [Fact]
        public void ToString_WithPreRelease()
        {
            var version = new SemanticVersion(1, 0, 0, "alpha");

            Assert.Equal("1.0.0-alpha", version.ToString());
        }

        /// <summary>
        /// Verifies that ToString includes the build metadata.
        /// </summary>
        [Fact]
        public void ToString_WithBuildMetadata()
        {
            var version = new SemanticVersion(1, 0, 0, null, "build.123");

            Assert.Equal("1.0.0+build.123", version.ToString());
        }

        /// <summary>
        /// Verifies that ToString includes both prerelease and build metadata.
        /// </summary>
        [Fact]
        public void ToString_WithPreReleaseAndBuildMetadata()
        {
            var version = new SemanticVersion(1, 0, 0, "alpha", "build.123");

            Assert.Equal("1.0.0-alpha+build.123", version.ToString());
        }

        /// <summary>
        /// Verifies that ToTagString adds the "v" prefix to the version string.
        /// </summary>
        [Fact]
        public void ToTagString_AddsVPrefix()
        {
            var version = new SemanticVersion(1, 2, 3);

            Assert.Equal("v1.2.3", version.ToTagString());
        }

        #endregion

        #region IsPreRelease Tests

        /// <summary>
        /// Verifies that IsPreRelease returns true when a prerelease identifier is present.
        /// </summary>
        [Fact]
        public void IsPreRelease_WithPreRelease_ReturnsTrue()
        {
            var version = new SemanticVersion(1, 0, 0, "alpha");

            Assert.True(version.IsPreRelease);
        }

        /// <summary>
        /// Verifies that IsPreRelease returns false when no prerelease identifier is present.
        /// </summary>
        [Fact]
        public void IsPreRelease_WithoutPreRelease_ReturnsFalse()
        {
            var version = new SemanticVersion(1, 0, 0);

            Assert.False(version.IsPreRelease);
        }

        /// <summary>
        /// Verifies that IsPreRelease returns false when the prerelease identifier is empty.
        /// </summary>
        [Fact]
        public void IsPreRelease_EmptyPreRelease_ReturnsFalse()
        {
            var version = new SemanticVersion(1, 0, 0, "");

            Assert.False(version.IsPreRelease);
        }

        #endregion
    }
}
