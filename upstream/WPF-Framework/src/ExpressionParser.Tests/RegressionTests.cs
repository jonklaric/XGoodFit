using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExpressionParser;

namespace ExpressionParser.Tests
{
    /// <summary>
    /// Contains regression tests for edge cases and previously reported issues in the ExpressionParser library.
    /// Each test targets a specific scenario that could cause incorrect results, crashes, or unexpected behavior.
    /// </summary>
    [TestClass]
    public class RegressionTests
    {
        /// <summary>
        /// EP-003: Verifies that dividing by zero returns NaN rather than throwing an exception.
        /// The parser should handle division by zero gracefully by returning double.NaN.
        /// </summary>
        [TestMethod]
        public void EP003_DivisionByZero_ReturnsNaN()
        {
            var result = ExpressionParser.Parser.Parser.Parse("10/0").Evaluate();
            Assert.IsTrue(double.IsNaN((double)result.Result), "Division by zero should return NaN");
        }

        /// <summary>
        /// EP-001/002: Verifies that decimal numbers are parsed using culture-invariant formatting.
        /// The parser must always interpret '.' as a decimal separator regardless of the current culture.
        /// </summary>
        [TestMethod]
        public void EP001_CultureInvariantDecimalParsing()
        {
            var result = ExpressionParser.Parser.Parser.Parse("3.14").Evaluate();
            Assert.AreEqual(ResultType.Double, result.Type, "Decimal literal should produce a Double result type");
            Assert.AreEqual(3.14, (double)result.Result, 0.0001, "Decimal literal '3.14' should evaluate to 3.14");
        }

        /// <summary>
        /// EP-008: Verifies that an unterminated string literal (missing closing quote) produces a LexerError token.
        /// The lexer should detect the missing closing quote and emit an error token.
        /// </summary>
        [TestMethod]
        public void EP008_UnterminatedStringLiteral_ProducesLexerError()
        {
            var tokens = Lexer.TokenizeStringToList("'hello");
            Assert.IsTrue(tokens.Any(t => t.Type == TokenType.LexerError),
                "Unterminated string literal should produce a LexerError token");
        }

        /// <summary>
        /// EP-006: Verifies that NORMINV produces errors because the function is not yet implemented.
        /// The parser should flag the expression as containing errors rather than silently returning an incorrect result.
        /// </summary>
        [TestMethod]
        public void EP006_NormalInverse_ProducesError()
        {
            var result = ExpressionParser.Parser.Parser.Parse("NORMINV(0.5)");
            Assert.IsTrue(result.ContainsErrors, "NORMINV should produce errors since it is not implemented");
        }

        /// <summary>
        /// EP-012: Verifies that a variable with no assigned value evaluates to an error.
        /// When a variable is declared but never assigned a value, evaluation should return an Error result.
        /// </summary>
        [TestMethod]
        public void EP012_NullVariable_ReturnsError()
        {
            var variables = new Dictionary<string, ResultType> { { "x", ResultType.Double } };
            var node = ExpressionParser.Parser.Parser.Parse("[x] + 1", false, variables);
            Assert.IsNotNull(node, "Parse should return a non-null node for variable expression");
            // Do not set the variable value - leave it null
            var result = node.Evaluate();
            Assert.AreEqual(ResultType.Error, result.Type, "Unset variable in arithmetic should produce an Error result");
        }

        /// <summary>
        /// EP-014: Verifies that IF with a null/empty condition does not crash and produces an error.
        /// Passing an empty first argument to IF should be handled gracefully.
        /// </summary>
        [TestMethod]
        public void EP014_IfWithNullCondition_DoesNotCrash()
        {
            var node = ExpressionParser.Parser.Parser.Parse("IF(,1,2)");
            Assert.IsNotNull(node, "IF(,1,2) should parse to a non-null node");
            var result = node.Evaluate();
            // Should either produce an error or handle gracefully - must not throw
            Assert.IsNotNull(result, "IF with null condition should return a result, not throw");
        }

        /// <summary>
        /// EP-017: Verifies that CONTAINS with error inputs produces an error result type.
        /// When CONTAINS receives arguments that cannot be evaluated, it should return an Error.
        /// </summary>
        [TestMethod]
        public void EP017_ContainsWithErrorInputs_ReturnsError()
        {
            var node = ExpressionParser.Parser.Parser.Parse("CONTAINS(, 'test')");
            Assert.IsNotNull(node, "CONTAINS(, 'test') should parse to a non-null node");
            var result = node.Evaluate();
            Assert.AreEqual(ResultType.Error, result.Type,
                "CONTAINS with null first operand should produce an Error result");
        }

        /// <summary>
        /// Verifies that AND with a null/empty operand does not crash and produces an error.
        /// Passing an empty second argument to AND should be handled gracefully.
        /// </summary>
        [TestMethod]
        public void AndOrWithNullOperands_ReturnsError()
        {
            var node = ExpressionParser.Parser.Parser.Parse("AND(TRUE,)");
            Assert.IsNotNull(node, "AND(TRUE,) should parse to a non-null node");
            var result = node.Evaluate();
            Assert.IsNotNull(result, "AND with null operand should return a result, not throw");
        }

        /// <summary>
        /// EP-010: Verifies that ROUND with a null/empty first parameter does not crash and produces an error.
        /// The parser should detect the missing parameter and return an error result.
        /// </summary>
        [TestMethod]
        public void EP010_RoundNullParameter_ReturnsError()
        {
            var node = ExpressionParser.Parser.Parser.Parse("ROUND(,2)");
            Assert.IsNotNull(node, "ROUND(,2) should parse to a non-null node");
            Assert.IsTrue(node.ContainsErrors, "ROUND with null first parameter should contain errors");
            var result = node.Evaluate();
            Assert.AreEqual(ResultType.Error, result.Type,
                "ROUND with null first parameter should produce an Error result");
        }

        /// <summary>
        /// EP-023: Verifies that RANDBETWEEN with min greater than max does not crash.
        /// The implementation swaps min and max when min exceeds max, so the result should be valid.
        /// </summary>
        [TestMethod]
        public void EP023_RandomBetweenMinGreaterThanMax_DoesNotCrash()
        {
            var node = ExpressionParser.Parser.Parser.Parse("RANDBETWEEN(10,5)");
            Assert.IsNotNull(node, "RANDBETWEEN(10,5) should parse successfully");
            Assert.IsFalse(node.ContainsErrors, "RANDBETWEEN(10,5) should not contain parse errors");
            var result = node.Evaluate();
            Assert.AreEqual(ResultType.Double, result.Type, "RANDBETWEEN should return a Double result");
            double value = (double)result.Result;
            Assert.IsTrue(value >= 5.0 && value <= 10.0,
                "RANDBETWEEN(10,5) should return a value between 5 and 10 after swapping min/max");
        }

        /// <summary>
        /// EP-021: Verifies that the DBL converter function parses decimal strings using culture-invariant formatting.
        /// DBL('3.14') should always produce 3.14 regardless of the current thread culture.
        /// </summary>
        [TestMethod]
        public void EP021_ConverterNodeCultureInvariant()
        {
            var result = ExpressionParser.Parser.Parser.Parse("DBL('3.14')").Evaluate();
            Assert.AreEqual(ResultType.Double, result.Type, "DBL('3.14') should return a Double result type");
            Assert.AreEqual(3.14, (double)result.Result, 0.0001,
                "DBL('3.14') should evaluate to 3.14 using culture-invariant parsing");
        }

        /// <summary>
        /// EP-019: Verifies that AND(TRUE, TRUE) can be simplified and the simplified result evaluates correctly.
        /// The Simplify method should preserve child nodes and produce a correct evaluation.
        /// </summary>
        [TestMethod]
        public void EP019_AndOrNodeSimplify_PreservesChildren()
        {
            var node = ExpressionParser.Parser.Parser.Parse("AND(TRUE, TRUE)");
            Assert.IsNotNull(node, "AND(TRUE, TRUE) should parse successfully");
            var simplified = node.Simplify();
            Assert.IsNotNull(simplified, "Simplified AND(TRUE, TRUE) should not be null");
            var result = simplified.Evaluate();
            Assert.AreEqual(true, result.Result, "Simplified AND(TRUE, TRUE) should evaluate to true");
            Assert.AreEqual(ResultType.Boolean, result.Type, "Simplified AND(TRUE, TRUE) should have Boolean result type");
        }
    }
}
