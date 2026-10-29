using System.Globalization;
using System.Windows;
using Numerics.Data;
using NumericControls;
using Xunit;

namespace NumericControls.Tests.Converters
{
    /// <summary>
    /// Tests for <see cref="DoubleToFontFamilyConverter"/>.
    /// </summary>
    public class DoubleToFontFamilyConverterTests
    {
        /// <summary>
        /// The converter instance used for testing.
        /// </summary>
        private readonly DoubleToFontFamilyConverter _converter = new();

        /// <summary>
        /// Tests that converting a normal value returns Normal font style.
        /// </summary>
        [Fact]
        public void Convert_NormalValue_ReturnsNormalFontStyle()
        {
            // Arrange
            double value = 42.0;

            // Act
            var result = _converter.Convert(value, typeof(FontStyle), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(FontStyles.Normal, result);
        }

        /// <summary>
        /// Tests that converting NaN returns Italic font style.
        /// </summary>
        [Fact]
        public void Convert_NaN_ReturnsItalicFontStyle()
        {
            // Arrange
            double value = double.NaN;

            // Act
            var result = _converter.Convert(value, typeof(FontStyle), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(FontStyles.Italic, result);
        }

        /// <summary>
        /// Tests that converting Positive Infinity returns Italic font style.
        /// </summary>
        [Fact]
        public void Convert_PositiveInfinity_ReturnsItalicFontStyle()
        {
            // Arrange
            double value = double.PositiveInfinity;

            // Act
            var result = _converter.Convert(value, typeof(FontStyle), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(FontStyles.Italic, result);
        }

        /// <summary>
        /// Tests that converting Negative Infinity returns Italic font style.
        /// </summary>
        [Fact]
        public void Convert_NegativeInfinity_ReturnsItalicFontStyle()
        {
            // Arrange
            double value = double.NegativeInfinity;

            // Act
            var result = _converter.Convert(value, typeof(FontStyle), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(FontStyles.Italic, result);
        }

        /// <summary>
        /// Tests that converting zero returns Normal font style.
        /// </summary>
        [Fact]
        public void Convert_Zero_ReturnsNormalFontStyle()
        {
            // Arrange
            double value = 0.0;

            // Act
            var result = _converter.Convert(value, typeof(FontStyle), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(FontStyles.Normal, result);
        }

        /// <summary>
        /// Tests that converting a negative value returns Normal font style.
        /// </summary>
        [Fact]
        public void Convert_NegativeValue_ReturnsNormalFontStyle()
        {
            // Arrange
            double value = -123.456;

            // Act
            var result = _converter.Convert(value, typeof(FontStyle), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(FontStyles.Normal, result);
        }

        /// <summary>
        /// Tests that converting Double.MaxValue returns Normal font style.
        /// </summary>
        [Fact]
        public void Convert_MaxValue_ReturnsNormalFontStyle()
        {
            // Arrange
            double value = double.MaxValue;

            // Act
            var result = _converter.Convert(value, typeof(FontStyle), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(FontStyles.Normal, result);
        }

        /// <summary>
        /// Tests that converting Double.MinValue returns Normal font style.
        /// </summary>
        [Fact]
        public void Convert_MinValue_ReturnsNormalFontStyle()
        {
            // Arrange
            double value = double.MinValue;

            // Act
            var result = _converter.Convert(value, typeof(FontStyle), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(FontStyles.Normal, result);
        }

        /// <summary>
        /// Tests that ConvertBack throws NotImplementedException.
        /// </summary>
        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(FontStyles.Normal, typeof(double), null, CultureInfo.InvariantCulture));
        }
    }
}
