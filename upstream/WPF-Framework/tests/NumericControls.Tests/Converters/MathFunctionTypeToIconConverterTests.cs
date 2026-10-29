using System.Globalization;
using System.Windows;
using Numerics.Data;
using NumericControls;
using Xunit;

namespace NumericControls.Tests.Converters
{
    /// <summary>
    /// Tests for <see cref="MathFunctionTypeToIconConverter"/>.
    /// </summary>
    public class MathFunctionTypeToIconConverterTests
    {
        /// <summary>
        /// Tests that GetIconResourceKey returns "PlusIcon" for Add function type.
        /// </summary>
        [Fact]
        public void GetIconResourceKey_Add_ReturnsPlusIcon()
        {
            // Act
            var result = MathFunctionTypeToIconConverter.GetIconResourceKey(MathFunctionType.Add);

            // Assert
            Assert.Equal("PlusIcon", result);
        }

        [Fact]
        public void GetIconResourceKey_Subtract_ReturnsMinusIcon()
        {
            // Act
            var result = MathFunctionTypeToIconConverter.GetIconResourceKey(MathFunctionType.Subtract);

            // Assert
            Assert.Equal("MinusIcon", result);
        }

        [Fact]
        public void GetIconResourceKey_Multiply_ReturnsMultiplyIcon()
        {
            // Act
            var result = MathFunctionTypeToIconConverter.GetIconResourceKey(MathFunctionType.Multiply);

            // Assert
            Assert.Equal("MultiplyIcon", result);
        }

        [Fact]
        public void GetIconResourceKey_Divide_ReturnsDivideIcon()
        {
            // Act
            var result = MathFunctionTypeToIconConverter.GetIconResourceKey(MathFunctionType.Divide);

            // Assert
            Assert.Equal("DivideIcon", result);
        }

        [Fact]
        public void GetIconResourceKey_Exponentiate_ReturnsExpIcon()
        {
            // Act
            var result = MathFunctionTypeToIconConverter.GetIconResourceKey(MathFunctionType.Exponentiate);

            // Assert
            Assert.Equal("ExpIcon", result);
        }

        [Fact]
        public void GetIconResourceKey_Logarithm_ReturnsLogIcon()
        {
            // Act
            var result = MathFunctionTypeToIconConverter.GetIconResourceKey(MathFunctionType.Logarithm);

            // Assert
            Assert.Equal("LogIcon", result);
        }

        [Fact]
        public void GetIconResourceKey_Inverse_ReturnsInvertIcon()
        {
            // Act
            var result = MathFunctionTypeToIconConverter.GetIconResourceKey(MathFunctionType.Inverse);

            // Assert
            Assert.Equal("InvertIcon", result);
        }

        [Fact]
        public void GetIconResourceKey_Replace_ReturnsReplaceIcon()
        {
            // Act
            var result = MathFunctionTypeToIconConverter.GetIconResourceKey(MathFunctionType.Replace);

            // Assert
            Assert.Equal("ReplaceIcon", result);
        }

        [Fact]
        public void GetIconResourceKey_Interpolate_ReturnsInterpolateIcon()
        {
            // Act
            var result = MathFunctionTypeToIconConverter.GetIconResourceKey(MathFunctionType.Interpolate);

            // Assert
            Assert.Equal("InterpolateIcon", result);
        }

        [Fact]
        public void GetIconResourceKey_AllFunctionTypes_ReturnValidKeys()
        {
            // Act & Assert
            foreach (MathFunctionType functionType in Enum.GetValues(typeof(MathFunctionType)))
            {
                var result = MathFunctionTypeToIconConverter.GetIconResourceKey(functionType);
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            }
        }
    }
}
