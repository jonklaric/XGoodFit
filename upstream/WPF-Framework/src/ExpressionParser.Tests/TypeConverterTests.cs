using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionParser.Tests
{
    /// <summary>
    /// Contains comprehensive unit tests for type conversion functions.
    /// Tests integer, double, string, and boolean conversion operations
    /// including all function aliases.
    /// </summary>
    [TestClass()]
    public class TypeConverterTests
    {
        #region Integer Conversion Tests

        /// <summary>
        /// Tests converting a double value to integer using rounding.
        /// </summary>
        [TestMethod()]
        public void ToInteger_ConvertDoubleToInt_RoundsDecimal()
        {
            Assert.AreEqual(3, ExpressionParser.Parser.Parser.Parse("TOINTEGER(3.14)").Evaluate().Result);
            Assert.AreEqual(4, ExpressionParser.Parser.Parser.Parse("TOINTEGER(3.99)").Evaluate().Result);
            Assert.AreEqual(-3, ExpressionParser.Parser.Parser.Parse("TOINTEGER(-3.14)").Evaluate().Result);
            Assert.AreEqual(-4, ExpressionParser.Parser.Parser.Parse("TOINTEGER(-3.99)").Evaluate().Result);
        }

        /// <summary>
        /// Tests converting a valid numeric string to integer.
        /// </summary>
        [TestMethod()]
        public void ToInteger_ConvertValidStringToInt_ReturnsInteger()
        {
            Assert.AreEqual(123, ExpressionParser.Parser.Parser.Parse("TOINTEGER(\"123\")").Evaluate().Result);
            Assert.AreEqual(-456, ExpressionParser.Parser.Parser.Parse("TOINTEGER(\"-456\")").Evaluate().Result);
            Assert.AreEqual(0, ExpressionParser.Parser.Parser.Parse("TOINTEGER(\"0\")").Evaluate().Result);
        }

        /// <summary>
        /// Tests converting a valid double string to integer with rounding.
        /// </summary>
        [TestMethod()]
        public void ToInteger_ConvertDoubleStringToInt_RoundsDecimal()
        {
            Assert.AreEqual(43, ExpressionParser.Parser.Parser.Parse("TOINTEGER(\"42.75\")").Evaluate().Result);
            Assert.AreEqual(-99, ExpressionParser.Parser.Parser.Parse("TOINTEGER(\"-99.123\")").Evaluate().Result);
        }

        /// <summary>
        /// Tests that converting an invalid string to integer results in an error.
        /// </summary>
        [TestMethod()]
        public void ToInteger_ConvertInvalidStringToInt_ReturnsError()
        {
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOINTEGER(\"abc\")").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOINTEGER(\"12.34.56\")").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOINTEGER(\"one\")").Evaluate().Type);
        }

        /// <summary>
        /// Tests converting boolean values to integer.
        /// Note: This tests the actual behavior where boolean string conversion is attempted.
        /// </summary>
        [TestMethod()]
        public void ToInteger_ConvertBooleanToInt_ReturnsError()
        {
            // Boolean values convert to "True" or "False" strings, which cannot parse to integers
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOINTEGER(TRUE)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOINTEGER(FALSE)").Evaluate().Type);
        }

        /// <summary>
        /// Tests integer conversion with large values.
        /// Note: Parser has Int32 limit during lexing, so very large literals overflow.
        /// </summary>
        [TestMethod()]
        public void ToInteger_LargeValues_HandlesCorrectly()
        {
            // Test with values within Int32 parsing range
            Assert.AreEqual(1000000, ExpressionParser.Parser.Parser.Parse("TOINTEGER(1000000)").Evaluate().Result);
            Assert.AreEqual(-1000000, ExpressionParser.Parser.Parser.Parse("TOINTEGER(-1000000)").Evaluate().Result);
            // 1000000.9 rounds to 1000001
            Assert.AreEqual(1000001, ExpressionParser.Parser.Parser.Parse("TOINTEGER(1000000.9)").Evaluate().Result);
        }

        /// <summary>
        /// Tests that CINT is an alias for TOINTEGER and behaves identically.
        /// </summary>
        [TestMethod()]
        public void CInt_Alias_WorksIdenticallyToToInteger()
        {
            Assert.AreEqual(43, ExpressionParser.Parser.Parser.Parse("CINT(42.9)").Evaluate().Result);
            Assert.AreEqual(100, ExpressionParser.Parser.Parser.Parse("CINT(\"100\")").Evaluate().Result);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("CINT(\"abc\")").Evaluate().Type);
        }

        /// <summary>
        /// Tests that INT is an alias for TOINTEGER and behaves identically.
        /// </summary>
        [TestMethod()]
        public void Int_Alias_WorksIdenticallyToToInteger()
        {
            Assert.AreEqual(99, ExpressionParser.Parser.Parser.Parse("INT(99.1)").Evaluate().Result);
            Assert.AreEqual(50, ExpressionParser.Parser.Parser.Parse("INT(\"50\")").Evaluate().Result);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("INT(\"xyz\")").Evaluate().Type);
        }

        #endregion

        #region Double Conversion Tests

        /// <summary>
        /// Tests converting an integer value to double.
        /// </summary>
        [TestMethod()]
        public void ToDouble_ConvertIntegerToDouble_ReturnsDouble()
        {
            Assert.AreEqual(42.0, ExpressionParser.Parser.Parser.Parse("TODOUBLE(42)").Evaluate().Result);
            Assert.AreEqual(-100.0, ExpressionParser.Parser.Parser.Parse("TODOUBLE(-100)").Evaluate().Result);
            Assert.AreEqual(0.0, ExpressionParser.Parser.Parser.Parse("TODOUBLE(0)").Evaluate().Result);
        }

        /// <summary>
        /// Tests converting a valid numeric string to double.
        /// </summary>
        [TestMethod()]
        public void ToDouble_ConvertValidStringToDouble_ReturnsDouble()
        {
            Assert.AreEqual(3.14, ExpressionParser.Parser.Parser.Parse("TODOUBLE(\"3.14\")").Evaluate().Result);
            Assert.AreEqual(-99.99, ExpressionParser.Parser.Parser.Parse("TODOUBLE(\"-99.99\")").Evaluate().Result);
            Assert.AreEqual(123.456, ExpressionParser.Parser.Parser.Parse("TODOUBLE(\"123.456\")").Evaluate().Result);
        }

        /// <summary>
        /// Tests that converting an invalid string to double results in an error.
        /// </summary>
        [TestMethod()]
        public void ToDouble_ConvertInvalidStringToDouble_ReturnsError()
        {
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TODOUBLE(\"abc\")").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TODOUBLE(\"12.34.56\")").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TODOUBLE(\"not a number\")").Evaluate().Type);
        }

        /// <summary>
        /// Tests converting boolean values to double.
        /// </summary>
        [TestMethod()]
        public void ToDouble_ConvertBooleanToDouble_ReturnsError()
        {
            // Boolean values convert to "True" or "False" strings, which cannot parse to doubles
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TODOUBLE(TRUE)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TODOUBLE(FALSE)").Evaluate().Type);
        }

        /// <summary>
        /// Tests that CDBL is an alias for TODOUBLE and behaves identically.
        /// </summary>
        [TestMethod()]
        public void CDbl_Alias_WorksIdenticallyToToDouble()
        {
            Assert.AreEqual(3.14159, ExpressionParser.Parser.Parser.Parse("CDBL(\"3.14159\")").Evaluate().Result);
            Assert.AreEqual(100.0, ExpressionParser.Parser.Parser.Parse("CDBL(100)").Evaluate().Result);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("CDBL(\"invalid\")").Evaluate().Type);
        }

        /// <summary>
        /// Tests that DBL is an alias for TODOUBLE and behaves identically.
        /// </summary>
        [TestMethod()]
        public void Dbl_Alias_WorksIdenticallyToToDouble()
        {
            Assert.AreEqual(2.71828, ExpressionParser.Parser.Parser.Parse("DBL(\"2.71828\")").Evaluate().Result);
            Assert.AreEqual(50.0, ExpressionParser.Parser.Parser.Parse("DBL(50)").Evaluate().Result);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("DBL(\"xyz\")").Evaluate().Type);
        }

        #endregion

        #region String Conversion Tests

        /// <summary>
        /// Tests converting an integer value to string.
        /// </summary>
        [TestMethod()]
        public void ToString_ConvertIntegerToString_ReturnsString()
        {
            Assert.AreEqual("42", ExpressionParser.Parser.Parser.Parse("TOSTRING(42)").Evaluate().Result);
            Assert.AreEqual("-100", ExpressionParser.Parser.Parser.Parse("TOSTRING(-100)").Evaluate().Result);
            Assert.AreEqual("0", ExpressionParser.Parser.Parser.Parse("TOSTRING(0)").Evaluate().Result);
        }

        /// <summary>
        /// Tests converting a double value to string.
        /// </summary>
        [TestMethod()]
        public void ToString_ConvertDoubleToString_ReturnsString()
        {
            Assert.AreEqual("3.14", ExpressionParser.Parser.Parser.Parse("TOSTRING(3.14)").Evaluate().Result);
            Assert.AreEqual("-99.99", ExpressionParser.Parser.Parser.Parse("TOSTRING(-99.99)").Evaluate().Result);
        }

        /// <summary>
        /// Tests converting boolean values to string representation.
        /// </summary>
        [TestMethod()]
        public void ToString_ConvertBooleanToString_ReturnsString()
        {
            Assert.AreEqual("True", ExpressionParser.Parser.Parser.Parse("TOSTRING(TRUE)").Evaluate().Result);
            Assert.AreEqual("False", ExpressionParser.Parser.Parser.Parse("TOSTRING(FALSE)").Evaluate().Result);
            Assert.AreEqual("True", ExpressionParser.Parser.Parser.Parse("TOSTRING(3>2)").Evaluate().Result);
            Assert.AreEqual("False", ExpressionParser.Parser.Parser.Parse("TOSTRING(5<2)").Evaluate().Result);
        }

        /// <summary>
        /// Tests that CSTR is an alias for TOSTRING and behaves identically.
        /// </summary>
        [TestMethod()]
        public void CStr_Alias_WorksIdenticallyToToString()
        {
            Assert.AreEqual("123", ExpressionParser.Parser.Parser.Parse("CSTR(123)").Evaluate().Result);
            Assert.AreEqual("3.14", ExpressionParser.Parser.Parser.Parse("CSTR(3.14)").Evaluate().Result);
            Assert.AreEqual("True", ExpressionParser.Parser.Parser.Parse("CSTR(TRUE)").Evaluate().Result);
        }

        /// <summary>
        /// Tests that STR is an alias for TOSTRING and behaves identically.
        /// </summary>
        [TestMethod()]
        public void Str_Alias_WorksIdenticallyToToString()
        {
            Assert.AreEqual("456", ExpressionParser.Parser.Parser.Parse("STR(456)").Evaluate().Result);
            Assert.AreEqual("2.718", ExpressionParser.Parser.Parser.Parse("STR(2.718)").Evaluate().Result);
            Assert.AreEqual("False", ExpressionParser.Parser.Parser.Parse("STR(FALSE)").Evaluate().Result);
        }

        #endregion

        #region Boolean Conversion Tests

        /// <summary>
        /// Tests converting valid boolean strings "True" and "False" to boolean values.
        /// </summary>
        [TestMethod()]
        public void ToBoolean_ConvertValidStringToBoolean_ReturnsBoolean()
        {
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"True\")").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"False\")").Evaluate().Result);
        }

        /// <summary>
        /// Tests case-insensitive boolean string conversion.
        /// </summary>
        [TestMethod()]
        public void ToBoolean_CaseInsensitiveConversion_ReturnsBoolean()
        {
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"true\")").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"false\")").Evaluate().Result);
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"TRUE\")").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"FALSE\")").Evaluate().Result);
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"TrUe\")").Evaluate().Result);
        }

        /// <summary>
        /// Tests that converting invalid strings to boolean results in an error.
        /// </summary>
        [TestMethod()]
        public void ToBoolean_ConvertInvalidStringToBoolean_ReturnsError()
        {
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"yes\")").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"no\")").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"1\")").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"0\")").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(\"abc\")").Evaluate().Type);
        }

        /// <summary>
        /// Tests converting numeric values to boolean.
        /// Note: Numbers convert to their string representation, which fails boolean parsing.
        /// </summary>
        [TestMethod()]
        public void ToBoolean_ConvertNumberToBoolean_ReturnsError()
        {
            // Numbers are converted to strings first, which cannot be parsed as boolean
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(1)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(0)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(-1)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(3.14)").Evaluate().Type);
        }

        /// <summary>
        /// Tests that CBOOL is an alias for TOBOOLEAN and behaves identically.
        /// </summary>
        [TestMethod()]
        public void CBool_Alias_WorksIdenticallyToToBoolean()
        {
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("CBOOL(\"True\")").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("CBOOL(\"False\")").Evaluate().Result);
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("CBOOL(\"true\")").Evaluate().Result);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("CBOOL(\"invalid\")").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, ExpressionParser.Parser.Parser.Parse("CBOOL(1)").Evaluate().Type);
        }

        #endregion

        #region Cross-Type Conversion Tests

        /// <summary>
        /// Tests chaining multiple type conversions together.
        /// </summary>
        [TestMethod()]
        public void Converters_ChainedConversions_WorksCorrectly()
        {
            // Integer to String to Integer
            Assert.AreEqual(42, ExpressionParser.Parser.Parser.Parse("TOINTEGER(TOSTRING(42))").Evaluate().Result);

            // Double to Integer (rounds) to Double: 3.99 rounds to 4
            Assert.AreEqual(4.0, ExpressionParser.Parser.Parser.Parse("TODOUBLE(TOINTEGER(3.99))").Evaluate().Result);

            // Boolean to String to Boolean
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("TOBOOLEAN(TOSTRING(TRUE))").Evaluate().Result);
        }

        /// <summary>
        /// Tests conversion of arithmetic expression results.
        /// </summary>
        [TestMethod()]
        public void Converters_ConvertArithmeticResults_WorksCorrectly()
        {
            Assert.AreEqual(15, ExpressionParser.Parser.Parser.Parse("TOINTEGER(10.5 + 4.8)").Evaluate().Result);
            Assert.AreEqual("30", ExpressionParser.Parser.Parser.Parse("TOSTRING(10 + 20)").Evaluate().Result);
            Assert.AreEqual(100.0, ExpressionParser.Parser.Parser.Parse("TODOUBLE(50 * 2)").Evaluate().Result);
        }

        #endregion
    }
}
