using System.Globalization;
using System.Windows;
using Numerics.Data;
using NumericControls;
using Xunit;

namespace NumericControls.Tests.Converters
{
    /// <summary>
    /// Tests for <see cref="MathFunctionTypeToNameConverter"/>.
    /// </summary>
    public class MathFunctionTypeToNameConverterTests
    {
        /// <summary>
        /// The converter instance used for testing.
        /// </summary>
        private readonly MathFunctionTypeToNameConverter _converter = new();

        /// <summary>
        /// Tests that GetName returns "Logarithmic Transform" for Logarithm function type.
        /// </summary>
        [Fact]
        public void GetName_Logarithm_ReturnsLogarithmicTransform()
        {
            // Act
            var result = MathFunctionTypeToNameConverter.GetName(MathFunctionType.Logarithm);

            // Assert
            Assert.Equal("Logarithmic Transform", result);
        }

        /// <summary>
        /// Tests that GetName returns "Add" for Add function type.
        /// </summary>
        [Fact]
        public void GetName_Add_ReturnsAdd()
        {
            // Act
            var result = MathFunctionTypeToNameConverter.GetName(MathFunctionType.Add);

            // Assert
            Assert.Equal("Add", result);
        }

        /// <summary>
        /// Tests that GetName returns "Subtract" for Subtract function type.
        /// </summary>
        [Fact]
        public void GetName_Subtract_ReturnsSubtract()
        {
            // Act
            var result = MathFunctionTypeToNameConverter.GetName(MathFunctionType.Subtract);

            // Assert
            Assert.Equal("Subtract", result);
        }

        /// <summary>
        /// Tests that GetName returns "Multiply" for Multiply function type.
        /// </summary>
        [Fact]
        public void GetName_Multiply_ReturnsMultiply()
        {
            // Act
            var result = MathFunctionTypeToNameConverter.GetName(MathFunctionType.Multiply);

            // Assert
            Assert.Equal("Multiply", result);
        }

        /// <summary>
        /// Tests that GetName returns "Divide" for Divide function type.
        /// </summary>
        [Fact]
        public void GetName_Divide_ReturnsDivide()
        {
            // Act
            var result = MathFunctionTypeToNameConverter.GetName(MathFunctionType.Divide);

            // Assert
            Assert.Equal("Divide", result);
        }

        /// <summary>
        /// Tests that GetName returns "Exponentiate" for Exponentiate function type.
        /// </summary>
        [Fact]
        public void GetName_Exponentiate_ReturnsExponentiate()
        {
            // Act
            var result = MathFunctionTypeToNameConverter.GetName(MathFunctionType.Exponentiate);

            // Assert
            Assert.Equal("Exponentiate", result);
        }

        /// <summary>
        /// Tests that GetName returns "Inverse" for Inverse function type.
        /// </summary>
        [Fact]
        public void GetName_Inverse_ReturnsInverse()
        {
            // Act
            var result = MathFunctionTypeToNameConverter.GetName(MathFunctionType.Inverse);

            // Assert
            Assert.Equal("Inverse", result);
        }

        /// <summary>
        /// Tests that GetName returns "Replace" for Replace function type.
        /// </summary>
        [Fact]
        public void GetName_Replace_ReturnsReplace()
        {
            // Act
            var result = MathFunctionTypeToNameConverter.GetName(MathFunctionType.Replace);

            // Assert
            Assert.Equal("Replace", result);
        }

        /// <summary>
        /// Tests that GetName returns "Interpolate" for Interpolate function type.
        /// </summary>
        [Fact]
        public void GetName_Interpolate_ReturnsInterpolate()
        {
            // Act
            var result = MathFunctionTypeToNameConverter.GetName(MathFunctionType.Interpolate);

            // Assert
            Assert.Equal("Interpolate", result);
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
            var result = _converter.Convert("not a MathFunctionType", typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that Convert returns the correct name for a valid MathFunctionType.
        /// </summary>
        [Fact]
        public void Convert_ValidMathFunctionType_ReturnsName()
        {
            // Act
            var result = _converter.Convert(MathFunctionType.Add, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal("Add", result);
        }

        /// <summary>
        /// Tests that ConvertBack throws NotImplementedException.
        /// </summary>
        [Fact]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack("Add", typeof(MathFunctionType), null, CultureInfo.InvariantCulture));
        }
    }
}
