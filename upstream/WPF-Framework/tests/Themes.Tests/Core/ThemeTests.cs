using Xunit;
using Themes;

namespace Themes.Tests.Core
{
    /// <summary>
    /// Unit tests for the Theme enumeration, which defines the available application themes.
    /// </summary>
    public class ThemeTests
    {
        #region Enum Value Tests

        /// <summary>
        /// Verifies that the Theme enumeration contains a Light value.
        /// </summary>
        [Fact]
        public void Theme_HasLightValue()
        {
            Assert.True(Enum.IsDefined(typeof(Theme), Theme.Light));
        }

        /// <summary>
        /// Verifies that the Theme enumeration contains a Blue value.
        /// </summary>
        [Fact]
        public void Theme_HasBlueValue()
        {
            Assert.True(Enum.IsDefined(typeof(Theme), Theme.Blue));
        }

        /// <summary>
        /// Verifies that the Theme enumeration contains a Dark value.
        /// </summary>
        [Fact]
        public void Theme_HasDarkValue()
        {
            Assert.True(Enum.IsDefined(typeof(Theme), Theme.Dark));
        }

        /// <summary>
        /// Verifies that the Theme enumeration contains exactly three values.
        /// </summary>
        [Fact]
        public void Theme_HasThreeValues()
        {
            var values = Enum.GetValues(typeof(Theme));

            Assert.Equal(3, values.Length);
        }

        #endregion

        #region Enum Ordering Tests

        /// <summary>
        /// Verifies that Theme.Light has an underlying value of 0 (first value).
        /// </summary>
        [Fact]
        public void Theme_Light_IsFirstValue()
        {
            Assert.Equal(0, (int)Theme.Light);
        }

        /// <summary>
        /// Verifies that Theme.Blue has an underlying value of 1 (second value).
        /// </summary>
        [Fact]
        public void Theme_Blue_IsSecondValue()
        {
            Assert.Equal(1, (int)Theme.Blue);
        }

        /// <summary>
        /// Verifies that Theme.Dark has an underlying value of 2 (third value).
        /// </summary>
        [Fact]
        public void Theme_Dark_IsThirdValue()
        {
            Assert.Equal(2, (int)Theme.Dark);
        }

        #endregion

        #region ToString Tests

        /// <summary>
        /// Verifies that Theme.Light.ToString() returns "Light".
        /// </summary>
        [Fact]
        public void Theme_Light_ToStringReturnsLight()
        {
            Assert.Equal("Light", Theme.Light.ToString());
        }

        /// <summary>
        /// Verifies that Theme.Blue.ToString() returns "Blue".
        /// </summary>
        [Fact]
        public void Theme_Blue_ToStringReturnsBlue()
        {
            Assert.Equal("Blue", Theme.Blue.ToString());
        }

        /// <summary>
        /// Verifies that Theme.Dark.ToString() returns "Dark".
        /// </summary>
        [Fact]
        public void Theme_Dark_ToStringReturnsDark()
        {
            Assert.Equal("Dark", Theme.Dark.ToString());
        }

        #endregion

        #region Parse Tests

        /// <summary>
        /// Verifies that Theme values can be parsed from their string names.
        /// </summary>
        /// <param name="name">The string name of the theme.</param>
        /// <param name="expected">The expected Theme value.</param>
        [Theory]
        [InlineData("Light", Theme.Light)]
        [InlineData("Blue", Theme.Blue)]
        [InlineData("Dark", Theme.Dark)]
        public void Theme_CanBeParsedFromString(string name, Theme expected)
        {
            var theme = Enum.Parse<Theme>(name);

            Assert.Equal(expected, theme);
        }

        /// <summary>
        /// Verifies that Theme values can be parsed from their numeric string representations.
        /// </summary>
        /// <param name="value">The numeric string value.</param>
        /// <param name="expected">The expected Theme value.</param>
        [Theory]
        [InlineData("0", Theme.Light)]
        [InlineData("1", Theme.Blue)]
        [InlineData("2", Theme.Dark)]
        public void Theme_CanBeParsedFromNumericString(string value, Theme expected)
        {
            var theme = Enum.Parse<Theme>(value);

            Assert.Equal(expected, theme);
        }

        #endregion

        #region Equality Tests

        /// <summary>
        /// Verifies that two Theme values with the same enumeration value are equal.
        /// </summary>
        [Fact]
        public void Theme_SameValues_AreEqual()
        {
            Theme a = Theme.Dark;
            Theme b = Theme.Dark;

            Assert.Equal(a, b);
            Assert.True(a == b);
        }

        /// <summary>
        /// Verifies that two Theme values with different enumeration values are not equal.
        /// </summary>
        [Fact]
        public void Theme_DifferentValues_AreNotEqual()
        {
            Theme a = Theme.Light;
            Theme b = Theme.Dark;

            Assert.NotEqual(a, b);
            Assert.True(a != b);
        }

        #endregion

        #region Default Value Test

        /// <summary>
        /// Verifies that the default value of the Theme enumeration is Theme.Light.
        /// </summary>
        [Fact]
        public void Theme_DefaultValue_IsLight()
        {
            Theme defaultTheme = default;

            Assert.Equal(Theme.Light, defaultTheme);
        }

        #endregion
    }
}
