using Xunit;
using Themes;
using System.Windows;
using System.Globalization;

namespace Themes.Tests.Converters
{
    /// <summary>
    /// Unit tests for the CutoffConverter class, which converts numeric values to boolean based on a cutoff threshold.
    /// </summary>
    public class CutoffConverterTests
    {
        #region Property Tests

        /// <summary>
        /// Verifies that the Cutoff property has a default value of zero.
        /// </summary>
        [Fact]
        public void Cutoff_DefaultIsZero()
        {
            var converter = new CutoffConverter();

            Assert.Equal(0, converter.Cutoff);
        }

        /// <summary>
        /// Verifies that the Cutoff property can be set and retrieved with various numeric values.
        /// </summary>
        /// <param name="value">The cutoff value to test.</param>
        [Theory]
        [InlineData(100)]
        [InlineData(50.5)]
        [InlineData(0)]
        [InlineData(-10)]
        public void Cutoff_SetAndGet(double value)
        {
            var converter = new CutoffConverter { Cutoff = value };

            Assert.Equal(value, converter.Cutoff);
        }

        #endregion

        #region Convert with Double Tests

        /// <summary>
        /// Verifies that Convert returns true when a double value is below the cutoff.
        /// </summary>
        [Fact]
        public void Convert_DoubleBelowCutoff_ReturnsTrue()
        {
            var converter = new CutoffConverter { Cutoff = 100 };

            var result = converter.Convert(50.0, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.True((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns false when a double value is equal to the cutoff.
        /// </summary>
        [Fact]
        public void Convert_DoubleEqualToCutoff_ReturnsFalse()
        {
            var converter = new CutoffConverter { Cutoff = 100 };

            var result = converter.Convert(100.0, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns false when a double value is above the cutoff.
        /// </summary>
        [Fact]
        public void Convert_DoubleAboveCutoff_ReturnsFalse()
        {
            var converter = new CutoffConverter { Cutoff = 100 };

            var result = converter.Convert(150.0, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns true when a negative double value is below the cutoff.
        /// </summary>
        [Fact]
        public void Convert_NegativeDouble_BelowCutoff()
        {
            var converter = new CutoffConverter { Cutoff = 0 };

            var result = converter.Convert(-10.0, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.True((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns false when a zero double value is equal to a zero cutoff.
        /// </summary>
        [Fact]
        public void Convert_ZeroDouble_EqualToZeroCutoff()
        {
            var converter = new CutoffConverter { Cutoff = 0 };

            var result = converter.Convert(0.0, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        #endregion

        #region Convert with GridLength Tests

        /// <summary>
        /// Verifies that Convert returns true when a GridLength value is below the cutoff.
        /// </summary>
        [Fact]
        public void Convert_GridLengthBelowCutoff_ReturnsTrue()
        {
            var converter = new CutoffConverter { Cutoff = 100 };
            var gridLength = new GridLength(50);

            var result = converter.Convert(gridLength, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.True((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns false when a GridLength value is equal to the cutoff.
        /// </summary>
        [Fact]
        public void Convert_GridLengthEqualToCutoff_ReturnsFalse()
        {
            var converter = new CutoffConverter { Cutoff = 100 };
            var gridLength = new GridLength(100);

            var result = converter.Convert(gridLength, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns false when a GridLength value is above the cutoff.
        /// </summary>
        [Fact]
        public void Convert_GridLengthAboveCutoff_ReturnsFalse()
        {
            var converter = new CutoffConverter { Cutoff = 100 };
            var gridLength = new GridLength(200);

            var result = converter.Convert(gridLength, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        #endregion

        #region Convert with Null and Invalid Types Tests

        /// <summary>
        /// Verifies that Convert returns false when provided with a null value.
        /// </summary>
        [Fact]
        public void Convert_NullValue_ReturnsFalse()
        {
            var converter = new CutoffConverter { Cutoff = 100 };

            var result = converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns false when provided with a string value.
        /// </summary>
        [Fact]
        public void Convert_StringValue_ReturnsFalse()
        {
            var converter = new CutoffConverter { Cutoff = 100 };

            var result = converter.Convert("50", typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns false when provided with an integer value.
        /// </summary>
        [Fact]
        public void Convert_IntValue_ReturnsFalse()
        {
            var converter = new CutoffConverter { Cutoff = 100 };

            var result = converter.Convert(50, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns false when provided with a generic object value.
        /// </summary>
        [Fact]
        public void Convert_ObjectValue_ReturnsFalse()
        {
            var converter = new CutoffConverter { Cutoff = 100 };

            var result = converter.Convert(new object(), typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        #endregion

        #region ConvertBack Tests

        /// <summary>
        /// Verifies that ConvertBack throws a NotImplementedException when called.
        /// </summary>
        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            var converter = new CutoffConverter();

            Assert.Throws<NotImplementedException>(() =>
                converter.ConvertBack(true, typeof(double), null, CultureInfo.InvariantCulture));
        }

        #endregion

        #region Edge Cases Tests

        /// <summary>
        /// Verifies that Convert returns true when a very small double value is below the cutoff.
        /// </summary>
        [Fact]
        public void Convert_VerySmallDouble_BelowCutoff()
        {
            var converter = new CutoffConverter { Cutoff = 1 };

            var result = converter.Convert(0.0001, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.True((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns true when a very large double value is just below the cutoff.
        /// </summary>
        [Fact]
        public void Convert_VeryLargeDouble_AboveCutoff()
        {
            var converter = new CutoffConverter { Cutoff = 1000000 };

            var result = converter.Convert(999999.99, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.True((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns false when the value is Double.MaxValue.
        /// </summary>
        [Fact]
        public void Convert_DoubleMaxValue_AboveCutoff()
        {
            var converter = new CutoffConverter { Cutoff = 1000 };

            var result = converter.Convert(double.MaxValue, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.False((bool)result);
        }

        /// <summary>
        /// Verifies that Convert returns true when the value is Double.MinValue.
        /// </summary>
        [Fact]
        public void Convert_DoubleMinValue_BelowCutoff()
        {
            var converter = new CutoffConverter { Cutoff = 1000 };

            var result = converter.Convert(double.MinValue, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.True((bool)result);
        }

        #endregion
    }
}
