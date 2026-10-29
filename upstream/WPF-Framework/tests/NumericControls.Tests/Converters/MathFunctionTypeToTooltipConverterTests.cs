using System.Globalization;
using System.Windows;
using Numerics.Data;
using NumericControls;
using Xunit;

namespace NumericControls.Tests.Converters
{
    /// <summary>
    /// Tests for <see cref="MathFunctionTypeToTooltipConverter"/>.
    /// </summary>
    public class MathFunctionTypeToTooltipConverterTests
    {
        /// <summary>
        /// The converter instance used for testing.
        /// </summary>
        private readonly MathFunctionTypeToTooltipConverter _converter = new();

        /// <summary>
        /// Tests that GetTooltip returns the correct tooltip for Add function type.
        /// </summary>
        [Fact]
        public void GetTooltip_Add_ReturnsAddTooltip()
        {
            // Act
            var result = MathFunctionTypeToTooltipConverter.GetTooltip(MathFunctionType.Add);

            // Assert
            Assert.Contains("Add a constant to values", result);
            Assert.Contains("Missing values are kept as missing", result);
        }

        /// <summary>
        /// Tests that GetTooltip returns the correct tooltip for Subtract function type.
        /// </summary>
        [Fact]
        public void GetTooltip_Subtract_ReturnsSubtractTooltip()
        {
            // Act
            var result = MathFunctionTypeToTooltipConverter.GetTooltip(MathFunctionType.Subtract);

            // Assert
            Assert.Contains("Subtract a constant from values", result);
        }

        /// <summary>
        /// Tests that GetTooltip returns the correct tooltip for Multiply function type.
        /// </summary>
        [Fact]
        public void GetTooltip_Multiply_ReturnsMultiplyTooltip()
        {
            // Act
            var result = MathFunctionTypeToTooltipConverter.GetTooltip(MathFunctionType.Multiply);

            // Assert
            Assert.Contains("Multiply values by a constant", result);
        }

        /// <summary>
        /// Tests that GetTooltip returns the correct tooltip for Divide function type.
        /// </summary>
        [Fact]
        public void GetTooltip_Divide_ReturnsDivideTooltip()
        {
            // Act
            var result = MathFunctionTypeToTooltipConverter.GetTooltip(MathFunctionType.Divide);

            // Assert
            Assert.Contains("Divide values by a constant", result);
        }

        /// <summary>
        /// Tests that GetTooltip returns the correct tooltip for Exponentiate function type.
        /// </summary>
        [Fact]
        public void GetTooltip_Exponentiate_ReturnsExponentiateTooltip()
        {
            // Act
            var result = MathFunctionTypeToTooltipConverter.GetTooltip(MathFunctionType.Exponentiate);

            // Assert
            Assert.Contains("Raise values to a constant power", result);
        }

        /// <summary>
        /// Tests that GetTooltip returns the correct tooltip for Logarithm function type.
        /// </summary>
        [Fact]
        public void GetTooltip_Logarithm_ReturnsLogTooltip()
        {
            // Act
            var result = MathFunctionTypeToTooltipConverter.GetTooltip(MathFunctionType.Logarithm);

            // Assert
            Assert.Contains("Log transform values", result);
        }

        /// <summary>
        /// Tests that GetTooltip returns the correct tooltip for Inverse function type.
        /// </summary>
        [Fact]
        public void GetTooltip_Inverse_ReturnsInverseTooltip()
        {
            // Act
            var result = MathFunctionTypeToTooltipConverter.GetTooltip(MathFunctionType.Inverse);

            // Assert
            Assert.Contains("Replace values by its inverse (1/x)", result);
            Assert.Contains("Zero values are set to missing", result);
        }

        /// <summary>
        /// Tests that GetTooltip returns the correct tooltip for Replace function type.
        /// </summary>
        [Fact]
        public void GetTooltip_Replace_ReturnsReplaceTooltip()
        {
            // Act
            var result = MathFunctionTypeToTooltipConverter.GetTooltip(MathFunctionType.Replace);

            // Assert
            Assert.Contains("Replace missing data", result);
        }

        /// <summary>
        /// Tests that GetTooltip returns the correct tooltip for Interpolate function type.
        /// </summary>
        [Fact]
        public void GetTooltip_Interpolate_ReturnsInterpolateTooltip()
        {
            // Act
            var result = MathFunctionTypeToTooltipConverter.GetTooltip(MathFunctionType.Interpolate);

            // Assert
            Assert.Contains("Interpolate missing data", result);
        }

        /// <summary>
        /// Tests that Convert returns null when value is null.
        /// </summary>
        [Fact]
        public void Convert_NullValue_ReturnsNull()
        {
            // Act
            var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that Convert returns null when value is not a MathFunctionType.
        /// </summary>
        [Fact]
        public void Convert_InvalidType_ReturnsNull()
        {
            // Act
            var result = _converter.Convert(123, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that Convert returns a tooltip string for a valid MathFunctionType.
        /// </summary>
        [Fact]
        public void Convert_ValidMathFunctionType_ReturnsTooltip()
        {
            // Act
            var result = _converter.Convert(MathFunctionType.Add, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<string>(result);
        }

        /// <summary>
        /// Tests that ConvertBack throws NotImplementedException.
        /// </summary>
        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack("tooltip", typeof(MathFunctionType), null, CultureInfo.InvariantCulture));
        }
    }
}
