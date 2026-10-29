using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExpressionParser;

namespace ExpressionParser.Tests
{
    /// <summary>
    /// Contains comprehensive unit tests for error handling in the ExpressionParser library.
    /// Tests cover syntax errors, function errors, type mismatches, and error reporting functionality.
    /// </summary>
    [TestClass()]
    public class ErrorHandlingTests
    {
        #region Syntax Errors

        /// <summary>
        /// Tests that an expression with a missing operand after an operator is detected as an error.
        /// Example: "5 +" should produce an error because there is no right operand.
        /// </summary>
        [TestMethod()]
        public void MissingOperand_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("5 +");
            Assert.IsTrue(result.ContainsErrors, "Expression '5 +' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression '5 +' should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression '5 +' should have at least one error message");
        }

        /// <summary>
        /// Tests that an expression with two values adjacent to each other without an operator is detected as an error.
        /// Example: "5 5" should produce an error because there is no operator between the values.
        /// </summary>
        [TestMethod()]
        public void MissingOperator_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("5 5");
            Assert.IsTrue(result.ContainsErrors, "Expression '5 5' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression '5 5' should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression '5 5' should have at least one error message");
        }

        /// <summary>
        /// Tests that an expression with an unclosed left parenthesis is detected as an error.
        /// Example: "(5 + 3" should produce an error because the parenthesis is not closed.
        /// </summary>
        [TestMethod()]
        public void UnclosedParenthesis_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("(5 + 3");
            Assert.IsTrue(result.ContainsErrors, "Expression '(5 + 3' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression '(5 + 3' should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression '(5 + 3' should have at least one error message");
        }

        /// <summary>
        /// Tests that an expression with an extra closing parenthesis is detected as an error.
        /// Example: "5 + 3)" should produce an error because there is no matching opening parenthesis.
        /// </summary>
        [TestMethod()]
        public void ExtraClosingParenthesis_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("5 + 3)");
            Assert.IsTrue(result.ContainsErrors, "Expression '5 + 3)' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression '5 + 3)' should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression '5 + 3)' should have at least one error message");
        }

        /// <summary>
        /// Tests that empty parentheses are handled appropriately.
        /// Example: "()" should produce an error or handle gracefully.
        /// </summary>
        [TestMethod()]
        public void EmptyParentheses_ShouldHandleGracefully()
        {
            var result = ExpressionParser.Parser.Parser.Parse("()");
            // Empty parentheses should either error or return null
            Assert.IsTrue(result == null || result.ContainsErrors, "Expression '()' should be null or contain errors");
        }

        /// <summary>
        /// Tests that adjacent operators without a value between them are detected as an error.
        /// Example: "5 + * 3" should produce an error because there is no operand between + and *.
        /// </summary>
        [TestMethod()]
        public void AdjacentOperators_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("5 + * 3");
            Assert.IsTrue(result.ContainsErrors, "Expression '5 + * 3' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression '5 + * 3' should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression '5 + * 3' should have at least one error message");
        }

        /// <summary>
        /// Tests that a unary plus operator at the start is handled correctly (may be valid).
        /// Example: "+5" should be valid as it represents positive 5.
        /// </summary>
        [TestMethod()]
        public void UnaryPlusOperator_ShouldBeValid()
        {
            var result = ExpressionParser.Parser.Parser.Parse("+5");
            Assert.IsFalse(result.ContainsErrors, "Expression '+5' should not contain errors");
            Assert.AreEqual(5, result.Evaluate().Result, "Expression '+5' should evaluate to 5");
        }

        /// <summary>
        /// Tests that a multiplication operator at the start without an operand is detected as an error.
        /// Example: "* 5" should produce an error because there is no left operand.
        /// </summary>
        [TestMethod()]
        public void MultiplicationAtStart_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("* 5");
            Assert.IsTrue(result.ContainsErrors, "Expression '* 5' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression '* 5' should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression '* 5' should have at least one error message");
        }

        /// <summary>
        /// Tests that an empty expression string is handled gracefully.
        /// Example: "" should return null or handle appropriately without crashing.
        /// </summary>
        [TestMethod()]
        public void EmptyExpression_ShouldHandleGracefully()
        {
            var result = ExpressionParser.Parser.Parser.Parse("");
            // Empty expression should return null or an empty result
            Assert.IsTrue(result == null, "Empty expression should return null");
        }

        /// <summary>
        /// Tests that an expression with only whitespace is handled gracefully.
        /// Example: "   " should return null or handle appropriately.
        /// </summary>
        [TestMethod()]
        public void OnlyWhitespace_ShouldHandleGracefully()
        {
            var result = ExpressionParser.Parser.Parser.Parse("   ");
            // Whitespace-only expression should return null or handle gracefully
            Assert.IsTrue(result == null, "Whitespace-only expression should return null");
        }

        #endregion

        #region Function Errors

        /// <summary>
        /// Tests that a function called with missing required arguments produces an error.
        /// Example: "IF()" should produce an error because IF requires three arguments.
        /// </summary>
        [TestMethod()]
        public void FunctionMissingArguments_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF()");
            Assert.IsTrue(result.ContainsErrors, "Expression 'IF()' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression 'IF()' should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression 'IF()' should have at least one error message");
        }

        /// <summary>
        /// Tests that a function called with too many arguments produces an error.
        /// Example: "LEN('test', 'extra')" should produce an error because LEN only takes one argument.
        /// </summary>
        [TestMethod()]
        public void FunctionTooManyArguments_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("LEN('test', 'extra')");
            Assert.IsTrue(result.ContainsErrors, "Expression 'LEN('test', 'extra')' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression should have at least one error message");
        }

        /// <summary>
        /// Tests that a function called with wrong argument types produces an error.
        /// Example: "ROUND('abc', 2)" should produce an error because ROUND requires a numeric first argument.
        /// </summary>
        [TestMethod()]
        public void FunctionWrongArgumentType_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("ROUND('abc', 2)");
            Assert.IsTrue(result.ContainsErrors, "Expression 'ROUND('abc', 2)' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression should have at least one error message");
        }

        /// <summary>
        /// Tests that a function with missing comma separator between arguments produces an error.
        /// Example: "IF(true false)" should produce an error because arguments must be comma-separated.
        /// </summary>
        [TestMethod()]
        public void FunctionMissingComma_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF(true false)");
            Assert.IsTrue(result.ContainsErrors, "Expression 'IF(true false)' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression should have at least one error message");
        }

        /// <summary>
        /// Tests that IF function with non-boolean test condition produces an error.
        /// Example: "IF('test', 1, 2)" should produce an error because the test must be boolean.
        /// </summary>
        [TestMethod()]
        public void IfFunctionNonBooleanTest_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF('test', 1, 2)");
            Assert.IsTrue(result.ContainsErrors, "Expression 'IF('test', 1, 2)' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression should have at least one error message");
        }

        /// <summary>
        /// Tests that ROUND function with only one argument when missing parenthesis produces an error.
        /// Example: "ROUND(5)" should work but "ROUND 5" should error.
        /// </summary>
        [TestMethod()]
        public void FunctionMissingParenthesis_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("LEN 'test'");
            Assert.IsTrue(result.ContainsErrors, "Expression 'LEN 'test'' should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Expression should have at least one error message");
        }

        #endregion

        #region Type Mismatch Errors

        /// <summary>
        /// Tests that comparing a string to a number produces an error or handles appropriately.
        /// Example: "'abc' > 5" should produce an error because type mismatch in comparison.
        /// </summary>
        [TestMethod()]
        public void CompareStringToNumber_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("'abc' > 5");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression ''abc' > 5' should evaluate to Error type");
        }

        /// <summary>
        /// Tests that adding a string to a number produces an error or concatenates.
        /// Example: "'abc' + 5" behavior depends on parser implementation.
        /// </summary>
        [TestMethod()]
        public void AddStringToNumber_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("'abc' + 5");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression ''abc' + 5' should evaluate to Error type");
        }

        /// <summary>
        /// Tests that performing arithmetic on boolean values produces an error.
        /// Example: "true + 5" should produce an error because booleans cannot be added to numbers.
        /// </summary>
        [TestMethod()]
        public void BooleanArithmetic_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("true + 5");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression 'true + 5' should evaluate to Error type");
        }

        /// <summary>
        /// Tests that multiplying a string produces an error.
        /// Example: "'test' * 3" should produce an error.
        /// </summary>
        [TestMethod()]
        public void MultiplyString_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("'test' * 3");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression ''test' * 3' should evaluate to Error type");
        }

        /// <summary>
        /// Tests that dividing by a string produces an error.
        /// Example: "10 / 'test'" should produce an error.
        /// </summary>
        [TestMethod()]
        public void DivideByString_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("10 / 'test'");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression '10 / 'test'' should evaluate to Error type");
        }

        #endregion

        #region ContainsErrors Property Tests

        /// <summary>
        /// Tests that ContainsErrors property is true for expressions with syntax errors.
        /// </summary>
        [TestMethod()]
        public void ContainsErrors_TrueForInvalidExpression()
        {
            var result = ExpressionParser.Parser.Parser.Parse("5 + ");
            Assert.IsTrue(result.ContainsErrors, "ContainsErrors should be true for invalid expression '5 +'");
        }

        /// <summary>
        /// Tests that ContainsErrors property is false for valid expressions.
        /// </summary>
        [TestMethod()]
        public void ContainsErrors_FalseForValidExpression()
        {
            var result = ExpressionParser.Parser.Parser.Parse("5 + 3");
            Assert.IsFalse(result.ContainsErrors, "ContainsErrors should be false for valid expression '5 + 3'");
        }

        /// <summary>
        /// Tests that GetErrors returns a non-empty list for expressions with errors.
        /// </summary>
        [TestMethod()]
        public void GetErrors_ReturnsErrorsForInvalidExpression()
        {
            var result = ExpressionParser.Parser.Parser.Parse("5 5");
            Assert.IsNotNull(result.GetErrors, "GetErrors should not be null");
            Assert.IsTrue(result.GetErrors.Count > 0, "GetErrors should return at least one error for '5 5'");
        }

        /// <summary>
        /// Tests that GetErrors returns an empty list for valid expressions.
        /// </summary>
        [TestMethod()]
        public void GetErrors_EmptyForValidExpression()
        {
            var result = ExpressionParser.Parser.Parser.Parse("5 + 3");
            Assert.IsNotNull(result.GetErrors, "GetErrors should not be null");
            Assert.AreEqual(0, result.GetErrors.Count, "GetErrors should be empty for valid expression '5 + 3'");
        }

        #endregion

        #region Error Information Tests

        /// <summary>
        /// Tests that error messages provide meaningful descriptions.
        /// </summary>
        [TestMethod()]
        public void ErrorMessages_AreMeaningful()
        {
            var result = ExpressionParser.Parser.Parser.Parse("5 5");
            Assert.IsTrue(result.GetErrors.Count > 0, "Should have at least one error");
            var errorMessage = result.GetErrors[0].Description;
            Assert.IsNotNull(errorMessage, "Error description should not be null");
            Assert.IsTrue(errorMessage.Length > 0, "Error description should not be empty");
            Assert.IsTrue(errorMessage.Contains("operator") || errorMessage.Contains("Operator"),
                "Error message should mention 'operator' for adjacent values error");
        }

        /// <summary>
        /// Tests that error objects contain token position information.
        /// </summary>
        [TestMethod()]
        public void ErrorTokenInfo_IsAvailable()
        {
            var result = ExpressionParser.Parser.Parser.Parse("5 5");
            Assert.IsTrue(result.GetErrors.Count > 0, "Should have at least one error");
            var error = result.GetErrors[0];
            Assert.IsNotNull(error.TokenWithError, "Error should have associated token");
        }

        /// <summary>
        /// Tests that multiple errors can be reported for complex invalid expressions.
        /// </summary>
        [TestMethod()]
        public void MultipleErrors_CanBeReported()
        {
            var result = ExpressionParser.Parser.Parser.Parse("(5 5");
            Assert.IsTrue(result.ContainsErrors, "Expression should contain errors");
            // May have multiple errors: missing operator AND unclosed parenthesis
            Assert.IsTrue(result.GetErrors.Count >= 1, "Should have at least one error");
        }

        /// <summary>
        /// Tests that nested function errors are properly reported.
        /// </summary>
        [TestMethod()]
        public void NestedFunctionErrors_AreReported()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF(true, ROUND('abc', 2), 0)");
            Assert.IsTrue(result.ContainsErrors, "Expression with nested error should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression should evaluate to Error type");
        }

        /// <summary>
        /// Tests that unclosed string literals are detected as errors.
        /// </summary>
        [TestMethod()]
        public void UnclosedStringLiteral_ShouldError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("'unclosed");
            Assert.IsTrue(result.ContainsErrors, "Expression with unclosed string should contain errors");
            Assert.AreEqual(ResultType.Error, result.Evaluate().Type, "Expression should evaluate to Error type");
            Assert.IsTrue(result.GetErrors.Count > 0, "Should have at least one error message");
        }

        #endregion
    }
}
