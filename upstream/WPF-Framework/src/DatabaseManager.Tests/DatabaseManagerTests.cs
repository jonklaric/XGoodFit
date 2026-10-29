using System;
using Xunit;
using DatabaseManager;

namespace DatabaseManager.Tests
{
    /// <summary>
    /// Tests for the DatabaseManager base class static methods.
    /// </summary>
    public class DatabaseManagerTests
    {
        #region IsNumericType Tests

        /// <summary>
        /// Verifies that IsNumericType returns true for all numeric types.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData(typeof(byte), true)]
        [InlineData(typeof(sbyte), true)]
        [InlineData(typeof(short), true)]
        [InlineData(typeof(ushort), true)]
        [InlineData(typeof(int), true)]
        [InlineData(typeof(uint), true)]
        [InlineData(typeof(long), true)]
        [InlineData(typeof(ulong), true)]
        [InlineData(typeof(float), true)]
        [InlineData(typeof(double), true)]
        [InlineData(typeof(decimal), true)]
        public void IsNumericType_WithNumericTypes_ReturnsTrue(Type type, bool expected)
        {
            var result = DatabaseManager.IsNumericType(type);
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that IsNumericType returns false for non-numeric types.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData(typeof(string), false)]
        [InlineData(typeof(bool), false)]
        [InlineData(typeof(char), false)]
        [InlineData(typeof(DateTime), false)]
        [InlineData(typeof(object), false)]
        public void IsNumericType_WithNonNumericTypes_ReturnsFalse(Type type, bool expected)
        {
            var result = DatabaseManager.IsNumericType(type);
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that IsNumericType returns false when passed null.
        /// </summary>
        [Fact]
        public void IsNumericType_WithNull_ReturnsFalse()
        {
            var result = DatabaseManager.IsNumericType(null);
            Assert.False(result);
        }

        /// <summary>
        /// Verifies that IsNumericType returns true for nullable numeric types.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData(typeof(int?), true)]
        [InlineData(typeof(double?), true)]
        [InlineData(typeof(decimal?), true)]
        [InlineData(typeof(long?), true)]
        [InlineData(typeof(float?), true)]
        [InlineData(typeof(byte?), true)]
        [InlineData(typeof(short?), true)]
        public void IsNumericType_WithNullableNumericTypes_ReturnsTrue(Type type, bool expected)
        {
            var result = DatabaseManager.IsNumericType(type);
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that IsNumericType returns false for nullable non-numeric types.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <param name="expected">The expected result.</param>
        [Theory]
        [InlineData(typeof(bool?), false)]
        [InlineData(typeof(char?), false)]
        [InlineData(typeof(DateTime?), false)]
        public void IsNumericType_WithNullableNonNumericTypes_ReturnsFalse(Type type, bool expected)
        {
            var result = DatabaseManager.IsNumericType(type);
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
