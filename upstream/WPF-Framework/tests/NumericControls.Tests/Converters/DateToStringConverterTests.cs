using System.Globalization;
using System.Windows;
using Numerics.Data;
using NumericControls;
using Xunit;

namespace NumericControls.Tests.Converters
{
    /// <summary>
    /// Tests for <see cref="DateToStringConverter"/>.
    /// </summary>
    public class DateToStringConverterTests
    {
        /// <summary>
        /// The converter instance used for testing.
        /// </summary>
        private readonly DateToStringConverter _converter = new();

        /// <summary>
        /// Tests that converting a valid date returns a formatted string.
        /// </summary>
        [Fact]
        public void Convert_ValidDate_ReturnsFormattedString()
        {
            // Arrange
            var date = new DateTime(2024, 6, 15, 14, 30, 0);

            // Act
            var result = _converter.Convert(date, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<string>(result);
            var resultString = (string)result;
            Assert.Contains("2024", resultString); // Year should be present
        }

        /// <summary>
        /// Tests that converting DateTime.MinValue returns a formatted string.
        /// </summary>
        [Fact]
        public void Convert_MinDate_ReturnsFormattedString()
        {
            // Arrange
            var date = DateTime.MinValue;

            // Act
            var result = _converter.Convert(date, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<string>(result);
        }

        /// <summary>
        /// Tests that converting DateTime.MaxValue returns a formatted string.
        /// </summary>
        [Fact]
        public void Convert_MaxDate_ReturnsFormattedString()
        {
            // Arrange
            var date = DateTime.MaxValue;

            // Act
            var result = _converter.Convert(date, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<string>(result);
        }

        /// <summary>
        /// Tests that converting back a valid string returns a DateTime.
        /// </summary>
        [Fact]
        public void ConvertBack_ValidString_ReturnsDateTime()
        {
            // Arrange
            var date = new DateTime(2024, 6, 15, 14, 30, 0);
            var dateString = (string)_converter.Convert(date, typeof(string), null, CultureInfo.InvariantCulture);

            // Act
            var result = _converter.ConvertBack(dateString, typeof(DateTime), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.IsType<DateTime>(result);
        }

        /// <summary>
        /// Tests that converting back an invalid string returns default DateTime.
        /// </summary>
        [Fact]
        public void ConvertBack_InvalidString_ReturnsDefaultDateTime()
        {
            // Arrange
            var invalidString = "not a date";

            // Act
            var result = _converter.ConvertBack(invalidString, typeof(DateTime), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.IsType<DateTime>(result);
            Assert.Equal(default(DateTime), result);
        }

        /// <summary>
        /// Tests that converting back an empty string returns default DateTime.
        /// </summary>
        [Fact]
        public void ConvertBack_EmptyString_ReturnsDefaultDateTime()
        {
            // Arrange
            var emptyString = "";

            // Act
            var result = _converter.ConvertBack(emptyString, typeof(DateTime), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.IsType<DateTime>(result);
            Assert.Equal(default(DateTime), result);
        }

        /// <summary>
        /// Tests that round-trip conversion preserves the DateTime value.
        /// </summary>
        [Fact]
        public void RoundTrip_DateTimeConversion_PreservesValue()
        {
            // Arrange
            var originalDate = new DateTime(2024, 3, 21, 10, 45, 0);

            // Act
            var stringValue = _converter.Convert(originalDate, typeof(string), null, CultureInfo.InvariantCulture);
            var roundTrippedDate = (DateTime)_converter.ConvertBack(stringValue, typeof(DateTime), null, CultureInfo.InvariantCulture);

            // Assert - comparing up to minutes (ignoring seconds for format compatibility)
            Assert.Equal(originalDate.Year, roundTrippedDate.Year);
            Assert.Equal(originalDate.Month, roundTrippedDate.Month);
            Assert.Equal(originalDate.Day, roundTrippedDate.Day);
            Assert.Equal(originalDate.Hour, roundTrippedDate.Hour);
            Assert.Equal(originalDate.Minute, roundTrippedDate.Minute);
        }
    }
}
