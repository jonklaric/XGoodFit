using Xunit;
using Themes;
using System.Globalization;

namespace Themes.Tests.Converters
{
    /// <summary>
    /// Tests for TabSizeConverter.
    /// Note: This converter requires a WPF TabControl as input, making it difficult to test
    /// in isolation. These tests verify basic behavior like ConvertBack throwing.
    /// Full integration testing would require WPF context.
    /// </summary>
    public class TabSizeConverterTests
    {
        #region ConvertBack Tests

        /// <summary>
        /// Verifies that ConvertBack throws a NotImplementedException when called.
        /// </summary>
        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            var converter = new TabSizeConverter();

            Assert.Throws<NotImplementedException>(() =>
                converter.ConvertBack(50.0, new[] { typeof(object), typeof(double) }, null, CultureInfo.InvariantCulture));
        }

        #endregion

        #region Convert Edge Cases

        /// <summary>
        /// Verifies that Convert returns 0.0 when provided with null values.
        /// </summary>
        [Fact]
        public void Convert_NullValues_ReturnsZero()
        {
            var converter = new TabSizeConverter();

            var result = converter.Convert(null!, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(0.0, result);
        }

        /// <summary>
        /// Verifies that Convert returns 0.0 when provided with an empty values array.
        /// </summary>
        [Fact]
        public void Convert_EmptyValues_ReturnsZero()
        {
            var converter = new TabSizeConverter();

            var result = converter.Convert(Array.Empty<object>(), typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(0.0, result);
        }

        /// <summary>
        /// Verifies that Convert returns 0.0 when the first value is not a TabControl.
        /// </summary>
        [Fact]
        public void Convert_NonTabControlValue_ReturnsZero()
        {
            var converter = new TabSizeConverter();

            var result = converter.Convert(new object[] { 100.0, 5 }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(0.0, result);
        }

        #endregion
    }
}
