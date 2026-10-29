using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionParser.Tests
{
    /// <summary>
    /// Contains comprehensive unit tests for string manipulation functions including edge cases
    /// for LEFT, RIGHT, SUBSTRING, LENGTH, INSTRING, CONCATENATE, ampersand operator, and CONTAINS.
    /// </summary>
    [TestClass]
    public class StringFunctionTests
    {
        /// <summary>
        /// Tests the LEFT function with count greater than string length.
        /// When the count exceeds the string length, LEFT should return the entire string.
        /// </summary>
        [TestMethod]
        public void Left_CountGreaterThanLength_ReturnsEntireString()
        {
            Assert.AreEqual("Hello", ExpressionParser.Parser.Parser.Parse("LEFT('Hello', 100)").Evaluate().Result);
            Assert.AreEqual("Test", ExpressionParser.Parser.Parser.Parse("LEFT('Test', 10)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the LEFT function with count of zero.
        /// Should return an empty string.
        /// </summary>
        [TestMethod]
        public void Left_CountZero_ReturnsEmptyString()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("LEFT('Hello', 0)").Evaluate().Result);
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("LEFT('World', 0)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the LEFT function with negative count.
        /// Should treat negative count as zero and return empty string.
        /// </summary>
        [TestMethod]
        public void Left_NegativeCount_ReturnsEmptyString()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("LEFT('Hello', -1)").Evaluate().Result);
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("LEFT('World', -5)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the RIGHT function with count greater than string length.
        /// When the count exceeds the string length, RIGHT should return the entire string.
        /// </summary>
        [TestMethod]
        public void Right_CountGreaterThanLength_ReturnsEntireString()
        {
            Assert.AreEqual("Hello", ExpressionParser.Parser.Parser.Parse("RIGHT('Hello', 100)").Evaluate().Result);
            Assert.AreEqual("Test", ExpressionParser.Parser.Parser.Parse("RIGHT('Test', 10)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the RIGHT function with count of zero.
        /// Should return an empty string.
        /// </summary>
        [TestMethod]
        public void Right_CountZero_ReturnsEmptyString()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("RIGHT('Hello', 0)").Evaluate().Result);
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("RIGHT('World', 0)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the RIGHT function with negative count.
        /// Should treat negative count as zero and return empty string.
        /// </summary>
        [TestMethod]
        public void Right_NegativeCount_ReturnsEmptyString()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("RIGHT('Hello', -1)").Evaluate().Result);
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("RIGHT('World', -5)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the SUBSTRING function with start index of zero.
        /// Should extract substring from the beginning.
        /// </summary>
        [TestMethod]
        public void Substring_StartAtZero_ExtractsFromBeginning()
        {
            Assert.AreEqual("Hel", ExpressionParser.Parser.Parser.Parse("SUBSTRING('Hello', 0, 3)").Evaluate().Result);
            Assert.AreEqual("Test", ExpressionParser.Parser.Parser.Parse("SUBSTRING('Testing', 0, 4)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the SUBSTRING function with start index beyond string length.
        /// Should return empty string.
        /// </summary>
        [TestMethod]
        public void Substring_StartBeyondLength_ReturnsEmptyString()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("SUBSTRING('Hello', 10, 5)").Evaluate().Result);
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("SUBSTRING('Test', 100, 3)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the SUBSTRING function with negative start index.
        /// Should treat negative index as zero.
        /// </summary>
        [TestMethod]
        public void Substring_NegativeStartIndex_TreatsAsZero()
        {
            Assert.AreEqual("Hel", ExpressionParser.Parser.Parser.Parse("SUBSTRING('Hello', -1, 3)").Evaluate().Result);
            Assert.AreEqual("Tes", ExpressionParser.Parser.Parser.Parse("SUBSTRING('Test', -5, 3)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the SUBSTRING function with length extending beyond string end.
        /// Should return substring up to the end of the string.
        /// </summary>
        [TestMethod]
        public void Substring_LengthBeyondEnd_ReturnsToEnd()
        {
            Assert.AreEqual("lo", ExpressionParser.Parser.Parser.Parse("SUBSTRING('Hello', 3, 100)").Evaluate().Result);
            Assert.AreEqual("sting", ExpressionParser.Parser.Parser.Parse("SUBSTRING('Testing', 2, 50)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the SUBSTRING function with zero length.
        /// Should return empty string.
        /// </summary>
        [TestMethod]
        public void Substring_ZeroLength_ReturnsEmptyString()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("SUBSTRING('Hello', 2, 0)").Evaluate().Result);
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("SUBSTRING('World', 0, 0)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the SUBSTRING function with negative length.
        /// Should treat negative length as zero and return empty string.
        /// </summary>
        [TestMethod]
        public void Substring_NegativeLength_ReturnsEmptyString()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("SUBSTRING('Hello', 2, -1)").Evaluate().Result);
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("SUBSTRING('World', 1, -5)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the LENGTH function with empty string.
        /// Should return zero.
        /// </summary>
        [TestMethod]
        public void Length_EmptyString_ReturnsZero()
        {
            Assert.AreEqual(0, ExpressionParser.Parser.Parser.Parse("LENGTH('')").Evaluate().Result);
            Assert.AreEqual(0, ExpressionParser.Parser.Parser.Parse("LEN('')").Evaluate().Result);
        }

        /// <summary>
        /// Tests the LEFT function with empty string.
        /// Should return empty string regardless of count.
        /// </summary>
        [TestMethod]
        public void Left_EmptyString_ReturnsEmptyString()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("LEFT('', 5)").Evaluate().Result);
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("LEFT('', 0)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the RIGHT function with empty string.
        /// Should return empty string regardless of count.
        /// </summary>
        [TestMethod]
        public void Right_EmptyString_ReturnsEmptyString()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("RIGHT('', 5)").Evaluate().Result);
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("RIGHT('', 0)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the SUBSTRING function with empty string.
        /// Should return empty string.
        /// </summary>
        [TestMethod]
        public void Substring_EmptyString_ReturnsEmptyString()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("SUBSTRING('', 0, 5)").Evaluate().Result);
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("SUBSTRING('', 0, 0)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the INSTRING function when substring is not found.
        /// Should return -1 when the search value is not present.
        /// </summary>
        [TestMethod]
        public void InString_SubstringNotFound_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, ExpressionParser.Parser.Parser.Parse("INSTRING('Hello World', 'xyz')").Evaluate().Result);
            Assert.AreEqual(-1, ExpressionParser.Parser.Parser.Parse("INDEXOF('Testing', 'abc')").Evaluate().Result);
        }

        /// <summary>
        /// Tests the INSTRING function with empty search string.
        /// Should return zero as empty string is found at the beginning.
        /// </summary>
        [TestMethod]
        public void InString_EmptySearchString_ReturnsZero()
        {
            Assert.AreEqual(0, ExpressionParser.Parser.Parser.Parse("INSTRING('Hello', '')").Evaluate().Result);
            Assert.AreEqual(0, ExpressionParser.Parser.Parser.Parse("INDEXOF('World', '')").Evaluate().Result);
        }

        /// <summary>
        /// Tests the INSTRING function with empty source string and non-empty search.
        /// Should return -1 as substring cannot be found in empty string.
        /// </summary>
        [TestMethod]
        public void InString_EmptySourceString_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, ExpressionParser.Parser.Parser.Parse("INSTRING('', 'test')").Evaluate().Result);
            Assert.AreEqual(-1, ExpressionParser.Parser.Parser.Parse("INDEXOF('', 'abc')").Evaluate().Result);
        }

        /// <summary>
        /// Tests the CONCATENATE function with empty strings.
        /// Should concatenate empty strings properly.
        /// </summary>
        [TestMethod]
        public void Concatenate_EmptyStrings_WorksCorrectly()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("CONCATENATE('', '')").Evaluate().Result);
            Assert.AreEqual("Hello", ExpressionParser.Parser.Parser.Parse("CONCATENATE('Hello', '')").Evaluate().Result);
            Assert.AreEqual("World", ExpressionParser.Parser.Parser.Parse("CONCATENATE('', 'World')").Evaluate().Result);
            Assert.AreEqual("HelloWorld", ExpressionParser.Parser.Parser.Parse("CONCATENATE('', 'Hello', '', 'World', '')").Evaluate().Result);
        }

        /// <summary>
        /// Tests the ampersand operator with mixed types (string and number).
        /// Should convert numbers to strings and concatenate.
        /// </summary>
        [TestMethod]
        public void Ampersand_MixedTypes_ConcatenatesCorrectly()
        {
            Assert.AreEqual("Value: 42", ExpressionParser.Parser.Parser.Parse("'Value: ' & 42").Evaluate().Result);
            Assert.AreEqual("123.45Test", ExpressionParser.Parser.Parser.Parse("123.45 & 'Test'").Evaluate().Result);
            Assert.AreEqual("1020", ExpressionParser.Parser.Parser.Parse("10 & 20").Evaluate().Result);
        }

        /// <summary>
        /// Tests the ampersand operator with empty strings.
        /// Should handle empty strings correctly.
        /// </summary>
        [TestMethod]
        public void Ampersand_EmptyStrings_WorksCorrectly()
        {
            Assert.AreEqual("", ExpressionParser.Parser.Parser.Parse("'' & ''").Evaluate().Result);
            Assert.AreEqual("Hello", ExpressionParser.Parser.Parser.Parse("'Hello' & ''").Evaluate().Result);
            Assert.AreEqual("World", ExpressionParser.Parser.Parser.Parse("'' & 'World'").Evaluate().Result);
        }

        /// <summary>
        /// Tests the CONTAINS function with empty search string.
        /// Should return true as every string contains an empty string.
        /// </summary>
        [TestMethod]
        public void Contains_EmptySearchString_ReturnsTrue()
        {
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("CONTAINS('Hello World', '')").Evaluate().Result);
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("CONTAINS('', '')").Evaluate().Result);
        }

        /// <summary>
        /// Tests the CONTAINS function with empty source string and non-empty search.
        /// Should return false.
        /// </summary>
        [TestMethod]
        public void Contains_EmptySourceString_ReturnsFalse()
        {
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("CONTAINS('', 'test')").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("CONTAINS('', 'a')").Evaluate().Result);
        }

        /// <summary>
        /// Tests the CONTAINS function with partial matches.
        /// Should correctly identify partial matches within a string.
        /// </summary>
        [TestMethod]
        public void Contains_PartialMatches_WorksCorrectly()
        {
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("CONTAINS('Hello World', 'Wor')").Evaluate().Result);
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("CONTAINS('Testing 123', '123')").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("CONTAINS('Hello', 'Goodbye')").Evaluate().Result);
        }

        /// <summary>
        /// Tests nested string function calls with LEFT and RIGHT.
        /// Verifies that functions can be composed together correctly.
        /// </summary>
        [TestMethod]
        public void NestedFunctions_LeftRight_WorksCorrectly()
        {
            // LEFT('Hello World', 5) = "Hello", RIGHT("Hello", 3) = "llo"
            Assert.AreEqual("llo", ExpressionParser.Parser.Parser.Parse("RIGHT(LEFT('Hello World', 5), 3)").Evaluate().Result);
            // RIGHT('Hello World', 5) = "World", LEFT("World", 3) = "Wor"
            Assert.AreEqual("Wor", ExpressionParser.Parser.Parser.Parse("LEFT(RIGHT('Hello World', 5), 3)").Evaluate().Result);
        }

        /// <summary>
        /// Tests nested string function calls with SUBSTRING and LENGTH.
        /// Verifies complex function compositions work correctly.
        /// </summary>
        [TestMethod]
        public void NestedFunctions_SubstringLength_WorksCorrectly()
        {
            Assert.AreEqual(5, ExpressionParser.Parser.Parser.Parse("LENGTH(SUBSTRING('Hello World', 0, 5))").Evaluate().Result);
            Assert.AreEqual("Hello", ExpressionParser.Parser.Parser.Parse("SUBSTRING('Hello World', 0, LENGTH('Hello'))").Evaluate().Result);
        }

        /// <summary>
        /// Tests nested string function calls with CONCATENATE and other functions.
        /// Verifies that CONCATENATE works correctly when nested.
        /// </summary>
        [TestMethod]
        public void NestedFunctions_Concatenate_WorksCorrectly()
        {
            Assert.AreEqual("He...ld", ExpressionParser.Parser.Parser.Parse("CONCATENATE(LEFT('Hello', 2), '...', RIGHT('World', 2))").Evaluate().Result);
            Assert.AreEqual(10, ExpressionParser.Parser.Parser.Parse("LENGTH(CONCATENATE('Hello', 'World'))").Evaluate().Result);
        }

        /// <summary>
        /// Tests INSTRING with zero-based indexing verification.
        /// Confirms that INSTRING returns correct zero-based indices.
        /// </summary>
        [TestMethod]
        public void InString_ZeroBasedIndexing_ReturnsCorrectIndex()
        {
            Assert.AreEqual(0, ExpressionParser.Parser.Parser.Parse("INSTRING('Hello', 'H')").Evaluate().Result);
            Assert.AreEqual(6, ExpressionParser.Parser.Parser.Parse("INSTRING('Hello World', 'World')").Evaluate().Result);
            Assert.AreEqual(4, ExpressionParser.Parser.Parser.Parse("INDEXOF('Hello', 'o')").Evaluate().Result);
        }
    }
}
