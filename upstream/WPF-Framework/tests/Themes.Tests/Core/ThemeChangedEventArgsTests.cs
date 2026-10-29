using Xunit;
using Themes;
using System.Windows;

namespace Themes.Tests.Core
{
    /// <summary>
    /// Unit tests for the ThemeChangedEventArgs class, which provides data for theme change events.
    /// </summary>
    public class ThemeChangedEventArgsTests
    {
        #region Constructor Tests

        /// <summary>
        /// Verifies that the constructor sets the OldTheme property correctly.
        /// </summary>
        [Fact]
        public void Constructor_SetsOldTheme()
        {
            var args = new ThemeChangedEventArgs(Theme.Light, Theme.Dark, null!);

            Assert.Equal(Theme.Light, args.OldTheme);
        }

        /// <summary>
        /// Verifies that the constructor sets the NewTheme property correctly.
        /// </summary>
        [Fact]
        public void Constructor_SetsNewTheme()
        {
            var args = new ThemeChangedEventArgs(Theme.Light, Theme.Dark, null!);

            Assert.Equal(Theme.Dark, args.NewTheme);
        }

        /// <summary>
        /// Verifies that the constructor sets the ColorDictionary property correctly.
        /// </summary>
        [Fact]
        public void Constructor_SetsColorDictionary()
        {
            var dictionary = new ResourceDictionary();
            var args = new ThemeChangedEventArgs(Theme.Light, Theme.Dark, dictionary);

            Assert.Same(dictionary, args.ColorDictionary);
        }

        /// <summary>
        /// Verifies that the constructor accepts a null value for the ColorDictionary parameter.
        /// </summary>
        [Fact]
        public void Constructor_AcceptsNullColorDictionary()
        {
            var args = new ThemeChangedEventArgs(Theme.Light, Theme.Dark, null!);

            Assert.Null(args.ColorDictionary);
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Verifies that the properties preserve different combinations of theme changes.
        /// </summary>
        /// <param name="oldTheme">The old theme value.</param>
        /// <param name="newTheme">The new theme value.</param>
        [Theory]
        [InlineData(Theme.Light, Theme.Dark)]
        [InlineData(Theme.Dark, Theme.Light)]
        [InlineData(Theme.Blue, Theme.Dark)]
        [InlineData(Theme.Light, Theme.Blue)]
        public void Properties_PreserveDifferentThemeCombinations(Theme oldTheme, Theme newTheme)
        {
            var args = new ThemeChangedEventArgs(oldTheme, newTheme, null!);

            Assert.Equal(oldTheme, args.OldTheme);
            Assert.Equal(newTheme, args.NewTheme);
        }

        /// <summary>
        /// Verifies that the properties allow the same value for both OldTheme and NewTheme.
        /// </summary>
        [Fact]
        public void Properties_AllowSameOldAndNewTheme()
        {
            var args = new ThemeChangedEventArgs(Theme.Blue, Theme.Blue, null!);

            Assert.Equal(Theme.Blue, args.OldTheme);
            Assert.Equal(Theme.Blue, args.NewTheme);
        }

        #endregion

        #region Inheritance Tests

        /// <summary>
        /// Verifies that ThemeChangedEventArgs inherits from EventArgs.
        /// </summary>
        [Fact]
        public void ThemeChangedEventArgs_InheritsFromEventArgs()
        {
            var args = new ThemeChangedEventArgs(Theme.Light, Theme.Dark, null!);

            Assert.IsAssignableFrom<EventArgs>(args);
        }

        #endregion
    }
}
