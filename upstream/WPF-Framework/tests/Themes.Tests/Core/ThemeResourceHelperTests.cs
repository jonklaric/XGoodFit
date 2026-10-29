using Xunit;
using Themes;

namespace Themes.Tests.Core
{
    /// <summary>
    /// Unit tests for the ThemeResourceHelper class, which provides helper methods for managing theme resources and URIs.
    /// </summary>
    public class ThemeResourceHelperTests
    {
        #region Constants Tests

        /// <summary>
        /// Verifies that the ControlTemplatesUri constant is not null or empty.
        /// </summary>
        [Fact]
        public void ControlTemplatesUri_IsNotNullOrEmpty()
        {
            Assert.False(string.IsNullOrEmpty(ThemeResourceHelper.ControlTemplatesUri));
        }

        /// <summary>
        /// Verifies that the ControlTemplatesUri constant is a valid pack URI.
        /// </summary>
        [Fact]
        public void ControlTemplatesUri_IsPackUri()
        {
            Assert.StartsWith("pack://application", ThemeResourceHelper.ControlTemplatesUri);
        }

        /// <summary>
        /// Verifies that the LightColorsUri constant is not null or empty.
        /// </summary>
        [Fact]
        public void LightColorsUri_IsNotNullOrEmpty()
        {
            Assert.False(string.IsNullOrEmpty(ThemeResourceHelper.LightColorsUri));
        }

        /// <summary>
        /// Verifies that the BlueColorsUri constant is not null or empty.
        /// </summary>
        [Fact]
        public void BlueColorsUri_IsNotNullOrEmpty()
        {
            Assert.False(string.IsNullOrEmpty(ThemeResourceHelper.BlueColorsUri));
        }

        /// <summary>
        /// Verifies that the DarkColorsUri constant is not null or empty.
        /// </summary>
        [Fact]
        public void DarkColorsUri_IsNotNullOrEmpty()
        {
            Assert.False(string.IsNullOrEmpty(ThemeResourceHelper.DarkColorsUri));
        }

        /// <summary>
        /// Verifies that all color URI constants are valid pack URIs.
        /// </summary>
        [Fact]
        public void AllColorUris_ArePackUris()
        {
            Assert.StartsWith("pack://application", ThemeResourceHelper.LightColorsUri);
            Assert.StartsWith("pack://application", ThemeResourceHelper.BlueColorsUri);
            Assert.StartsWith("pack://application", ThemeResourceHelper.DarkColorsUri);
        }

        /// <summary>
        /// Verifies that all color URI constants contain the Themes component path.
        /// </summary>
        [Fact]
        public void AllColorUris_ContainThemesComponent()
        {
            Assert.Contains("/Themes;component/", ThemeResourceHelper.LightColorsUri);
            Assert.Contains("/Themes;component/", ThemeResourceHelper.BlueColorsUri);
            Assert.Contains("/Themes;component/", ThemeResourceHelper.DarkColorsUri);
        }

        #endregion

        #region GetColorDictionaryUri Tests

        /// <summary>
        /// Verifies that GetColorDictionaryUri returns the correct URI for the Light theme.
        /// </summary>
        [Fact]
        public void GetColorDictionaryUri_Light_ReturnsLightColorsUri()
        {
            var uri = ThemeResourceHelper.GetColorDictionaryUri(Theme.Light);

            Assert.Equal(ThemeResourceHelper.LightColorsUri, uri);
        }

        /// <summary>
        /// Verifies that GetColorDictionaryUri returns the correct URI for the Blue theme.
        /// </summary>
        [Fact]
        public void GetColorDictionaryUri_Blue_ReturnsBlueColorsUri()
        {
            var uri = ThemeResourceHelper.GetColorDictionaryUri(Theme.Blue);

            Assert.Equal(ThemeResourceHelper.BlueColorsUri, uri);
        }

        /// <summary>
        /// Verifies that GetColorDictionaryUri returns the correct URI for the Dark theme.
        /// </summary>
        [Fact]
        public void GetColorDictionaryUri_Dark_ReturnsDarkColorsUri()
        {
            var uri = ThemeResourceHelper.GetColorDictionaryUri(Theme.Dark);

            Assert.Equal(ThemeResourceHelper.DarkColorsUri, uri);
        }

        /// <summary>
        /// Verifies that GetColorDictionaryUri throws an ArgumentOutOfRangeException for invalid theme values.
        /// </summary>
        [Fact]
        public void GetColorDictionaryUri_InvalidTheme_ThrowsArgumentOutOfRangeException()
        {
            var invalidTheme = (Theme)999;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThemeResourceHelper.GetColorDictionaryUri(invalidTheme));
        }

        /// <summary>
        /// Verifies that GetColorDictionaryUri returns a non-empty string for all valid theme values.
        /// </summary>
        /// <param name="theme">The theme to test.</param>
        [Theory]
        [InlineData(Theme.Light)]
        [InlineData(Theme.Blue)]
        [InlineData(Theme.Dark)]
        public void GetColorDictionaryUri_AllValidThemes_ReturnNonEmptyString(Theme theme)
        {
            var uri = ThemeResourceHelper.GetColorDictionaryUri(theme);

            Assert.False(string.IsNullOrEmpty(uri));
        }

        #endregion

        #region ParseTheme Tests

        /// <summary>
        /// Verifies that ParseTheme returns Theme.Light when given the string "Light".
        /// </summary>
        [Fact]
        public void ParseTheme_Light_ReturnsLightTheme()
        {
            var theme = ThemeResourceHelper.ParseTheme("Light");

            Assert.Equal(Theme.Light, theme);
        }

        /// <summary>
        /// Verifies that ParseTheme returns Theme.Blue when given the string "Blue".
        /// </summary>
        [Fact]
        public void ParseTheme_Blue_ReturnsBlueTheme()
        {
            var theme = ThemeResourceHelper.ParseTheme("Blue");

            Assert.Equal(Theme.Blue, theme);
        }

        /// <summary>
        /// Verifies that ParseTheme returns Theme.Dark when given the string "Dark".
        /// </summary>
        [Fact]
        public void ParseTheme_Dark_ReturnsDarkTheme()
        {
            var theme = ThemeResourceHelper.ParseTheme("Dark");

            Assert.Equal(Theme.Dark, theme);
        }

        /// <summary>
        /// Verifies that ParseTheme is case-insensitive when parsing theme names.
        /// </summary>
        /// <param name="themeName">The theme name to parse in various case formats.</param>
        [Theory]
        [InlineData("light")]
        [InlineData("LIGHT")]
        [InlineData("Light")]
        [InlineData("LiGhT")]
        public void ParseTheme_IsCaseInsensitive(string themeName)
        {
            var theme = ThemeResourceHelper.ParseTheme(themeName);

            Assert.Equal(Theme.Light, theme);
        }

        /// <summary>
        /// Verifies that ParseTheme throws an ArgumentNullException when given a null value.
        /// </summary>
        [Fact]
        public void ParseTheme_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ThemeResourceHelper.ParseTheme(null!));
        }

        /// <summary>
        /// Verifies that ParseTheme throws an ArgumentNullException when given an empty string.
        /// </summary>
        [Fact]
        public void ParseTheme_EmptyString_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ThemeResourceHelper.ParseTheme(""));
        }

        /// <summary>
        /// Verifies that ParseTheme throws an ArgumentNullException when given a whitespace-only string.
        /// </summary>
        [Fact]
        public void ParseTheme_WhitespaceOnly_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ThemeResourceHelper.ParseTheme("   "));
        }

        /// <summary>
        /// Verifies that ParseTheme throws an ArgumentException when given an invalid theme name.
        /// </summary>
        [Fact]
        public void ParseTheme_InvalidThemeName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                ThemeResourceHelper.ParseTheme("InvalidTheme"));
        }

        /// <summary>
        /// Verifies that ParseTheme's exception message contains valid theme values when an invalid name is provided.
        /// </summary>
        [Fact]
        public void ParseTheme_InvalidThemeName_ExceptionMessageContainsValidValues()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                ThemeResourceHelper.ParseTheme("InvalidTheme"));

            Assert.Contains("Light", ex.Message);
            Assert.Contains("Blue", ex.Message);
            Assert.Contains("Dark", ex.Message);
        }

        #endregion

        #region TryParseTheme Tests

        /// <summary>
        /// Verifies that TryParseTheme returns true and outputs Theme.Light when given the string "Light".
        /// </summary>
        [Fact]
        public void TryParseTheme_Light_ReturnsTrue()
        {
            var result = ThemeResourceHelper.TryParseTheme("Light", out var theme);

            Assert.True(result);
            Assert.Equal(Theme.Light, theme);
        }

        /// <summary>
        /// Verifies that TryParseTheme returns true and outputs Theme.Blue when given the string "Blue".
        /// </summary>
        [Fact]
        public void TryParseTheme_Blue_ReturnsTrue()
        {
            var result = ThemeResourceHelper.TryParseTheme("Blue", out var theme);

            Assert.True(result);
            Assert.Equal(Theme.Blue, theme);
        }

        /// <summary>
        /// Verifies that TryParseTheme returns true and outputs Theme.Dark when given the string "Dark".
        /// </summary>
        [Fact]
        public void TryParseTheme_Dark_ReturnsTrue()
        {
            var result = ThemeResourceHelper.TryParseTheme("Dark", out var theme);

            Assert.True(result);
            Assert.Equal(Theme.Dark, theme);
        }

        /// <summary>
        /// Verifies that TryParseTheme is case-insensitive when parsing theme names.
        /// </summary>
        /// <param name="themeName">The theme name to parse in various case formats.</param>
        [Theory]
        [InlineData("dark")]
        [InlineData("DARK")]
        [InlineData("Dark")]
        [InlineData("DaRk")]
        public void TryParseTheme_IsCaseInsensitive(string themeName)
        {
            var result = ThemeResourceHelper.TryParseTheme(themeName, out var theme);

            Assert.True(result);
            Assert.Equal(Theme.Dark, theme);
        }

        /// <summary>
        /// Verifies that TryParseTheme returns false when given a null value.
        /// </summary>
        [Fact]
        public void TryParseTheme_Null_ReturnsFalse()
        {
            var result = ThemeResourceHelper.TryParseTheme(null!, out var theme);

            Assert.False(result);
            Assert.Equal(Theme.Light, theme); // Default
        }

        /// <summary>
        /// Verifies that TryParseTheme returns false when given an empty string.
        /// </summary>
        [Fact]
        public void TryParseTheme_EmptyString_ReturnsFalse()
        {
            var result = ThemeResourceHelper.TryParseTheme("", out var theme);

            Assert.False(result);
            Assert.Equal(Theme.Light, theme); // Default
        }

        /// <summary>
        /// Verifies that TryParseTheme returns false when given a whitespace-only string.
        /// </summary>
        [Fact]
        public void TryParseTheme_WhitespaceOnly_ReturnsFalse()
        {
            var result = ThemeResourceHelper.TryParseTheme("   ", out var theme);

            Assert.False(result);
            Assert.Equal(Theme.Light, theme); // Default
        }

        /// <summary>
        /// Verifies that TryParseTheme returns false when given an invalid theme name.
        /// </summary>
        [Fact]
        public void TryParseTheme_InvalidThemeName_ReturnsFalse()
        {
            var result = ThemeResourceHelper.TryParseTheme("InvalidTheme", out var theme);

            Assert.False(result);
            Assert.Equal(Theme.Light, theme); // Default
        }

        /// <summary>
        /// Verifies that TryParseTheme does not throw exceptions for any invalid input.
        /// </summary>
        [Fact]
        public void TryParseTheme_DoesNotThrow()
        {
            var exception = Record.Exception(() =>
            {
                ThemeResourceHelper.TryParseTheme(null!, out _);
                ThemeResourceHelper.TryParseTheme("", out _);
                ThemeResourceHelper.TryParseTheme("InvalidTheme", out _);
            });

            Assert.Null(exception);
        }

        #endregion
    }
}
