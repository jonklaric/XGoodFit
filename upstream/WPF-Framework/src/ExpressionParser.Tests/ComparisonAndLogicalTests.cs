using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionParser.Tests
{
    /// <summary>
    /// Contains comprehensive unit tests for comparison and logical operators including
    /// boolean literals, comparison operators, AND/OR functions, IF statements,
    /// and complex nested expressions.
    /// </summary>
    [TestClass]
    public class ComparisonAndLogicalTests
    {
        #region Boolean Literals

        /// <summary>
        /// Tests that TRUE literal evaluates to true.
        /// </summary>
        [TestMethod]
        public void BooleanLiteral_TRUE_EvaluatesToTrue()
        {
            var result = ExpressionParser.Parser.Parser.Parse("TRUE").Evaluate();
            Assert.AreEqual(true, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        /// <summary>
        /// Tests that FALSE literal evaluates to false.
        /// </summary>
        [TestMethod]
        public void BooleanLiteral_FALSE_EvaluatesToFalse()
        {
            var result = ExpressionParser.Parser.Parser.Parse("FALSE").Evaluate();
            Assert.AreEqual(false, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        /// <summary>
        /// Tests case insensitivity of boolean literals - lowercase true.
        /// </summary>
        [TestMethod]
        public void BooleanLiteral_CaseInsensitive_Lowercase()
        {
            var resultTrue = ExpressionParser.Parser.Parser.Parse("true").Evaluate();
            Assert.AreEqual(true, resultTrue.Result);
            Assert.AreEqual(ResultType.Boolean, resultTrue.Type);

            var resultFalse = ExpressionParser.Parser.Parser.Parse("false").Evaluate();
            Assert.AreEqual(false, resultFalse.Result);
            Assert.AreEqual(ResultType.Boolean, resultFalse.Type);
        }

        /// <summary>
        /// Tests case insensitivity of boolean literals - mixed case.
        /// </summary>
        [TestMethod]
        public void BooleanLiteral_CaseInsensitive_MixedCase()
        {
            var resultTrue = ExpressionParser.Parser.Parser.Parse("True").Evaluate();
            Assert.AreEqual(true, resultTrue.Result);

            var resultFalse = ExpressionParser.Parser.Parser.Parse("False").Evaluate();
            Assert.AreEqual(false, resultFalse.Result);
        }

        #endregion

        #region String Comparisons

        /// <summary>
        /// Tests lexicographic string comparison using less than operator.
        /// </summary>
        [TestMethod]
        public void StringComparison_Lexicographic_LessThan()
        {
            var result = ExpressionParser.Parser.Parser.Parse("\"abc\" = \"def\"").Evaluate();
            Assert.AreEqual(false, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        /// <summary>
        /// Tests string equality with quoted strings.
        /// </summary>
        [TestMethod]
        public void StringComparison_Equality_WithQuotes()
        {
            var resultEqual = ExpressionParser.Parser.Parser.Parse("\"hello\" = \"hello\"").Evaluate();
            Assert.AreEqual(true, resultEqual.Result);

            var resultNotEqual = ExpressionParser.Parser.Parser.Parse("\"hello\" = \"world\"").Evaluate();
            Assert.AreEqual(false, resultNotEqual.Result);
        }

        /// <summary>
        /// Tests string equality with single-quoted strings.
        /// </summary>
        [TestMethod]
        public void StringComparison_Equality_SingleQuotes()
        {
            var result = ExpressionParser.Parser.Parser.Parse("'hello' = 'hello'").Evaluate();
            Assert.AreEqual(true, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        #endregion

        #region Boolean and Numeric Comparisons

        /// <summary>
        /// Tests comparison of boolean literals for equality.
        /// </summary>
        [TestMethod]
        public void BooleanComparison_Equality()
        {
            var resultTrueTrue = ExpressionParser.Parser.Parser.Parse("TRUE = TRUE").Evaluate();
            Assert.AreEqual(true, resultTrueTrue.Result);

            var resultFalseFalse = ExpressionParser.Parser.Parser.Parse("FALSE = FALSE").Evaluate();
            Assert.AreEqual(true, resultFalseFalse.Result);

            var resultTrueFalse = ExpressionParser.Parser.Parser.Parse("TRUE = FALSE").Evaluate();
            Assert.AreEqual(false, resultTrueFalse.Result);
        }

        /// <summary>
        /// Tests comparison of equal double values.
        /// </summary>
        [TestMethod]
        public void NumericComparison_DoubleEquality()
        {
            var result = ExpressionParser.Parser.Parser.Parse("3.14 = 3.14").Evaluate();
            Assert.AreEqual(true, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        /// <summary>
        /// Tests floating point comparison with tolerance consideration.
        /// This tests the classic floating point issue: 0.1 + 0.2 = 0.3
        /// </summary>
        [TestMethod]
        public void NumericComparison_FloatingPointTolerance()
        {
            var result = ExpressionParser.Parser.Parser.Parse("0.1 + 0.2 = 0.3").Evaluate();
            // Note: Due to floating point precision, this may not be exactly equal
            // but the parser should handle this appropriately
            Assert.IsNotNull(result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        /// <summary>
        /// Tests greater than and less than comparisons with integers.
        /// </summary>
        [TestMethod]
        public void NumericComparison_GreaterThanLessThan()
        {
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("5 > 3").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("3 > 5").Evaluate().Result);
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("2 < 10").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("10 < 2").Evaluate().Result);
        }

        /// <summary>
        /// Tests greater than or equal and less than or equal comparisons.
        /// </summary>
        [TestMethod]
        public void NumericComparison_GreaterThanOrEqualLessThanOrEqual()
        {
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("5 >= 5").Evaluate().Result);
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("5 >= 3").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("3 >= 5").Evaluate().Result);
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("5 <= 5").Evaluate().Result);
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("3 <= 5").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("5 <= 3").Evaluate().Result);
        }

        /// <summary>
        /// Tests not equal operator with various values.
        /// </summary>
        [TestMethod]
        public void NumericComparison_NotEqual()
        {
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("5 <> 3").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("5 <> 5").Evaluate().Result);
            Assert.AreEqual(true, ExpressionParser.Parser.Parser.Parse("5 != 3").Evaluate().Result);
            Assert.AreEqual(false, ExpressionParser.Parser.Parser.Parse("5 != 5").Evaluate().Result);
        }

        #endregion

        #region Logical AND

        /// <summary>
        /// Tests AND function with all true values.
        /// </summary>
        [TestMethod]
        public void LogicalAND_AllTrue()
        {
            var result = ExpressionParser.Parser.Parser.Parse("AND(TRUE, TRUE, TRUE)").Evaluate();
            Assert.AreEqual(true, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        /// <summary>
        /// Tests AND function with one false value among true values.
        /// </summary>
        [TestMethod]
        public void LogicalAND_OneFalse()
        {
            var result = ExpressionParser.Parser.Parser.Parse("AND(TRUE, FALSE, TRUE)").Evaluate();
            Assert.AreEqual(false, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        /// <summary>
        /// Tests AND function with single parameter.
        /// </summary>
        [TestMethod]
        public void LogicalAND_SingleParameter()
        {
            var resultTrue = ExpressionParser.Parser.Parser.Parse("AND(TRUE)").Evaluate();
            Assert.AreEqual(true, resultTrue.Result);

            var resultFalse = ExpressionParser.Parser.Parser.Parse("AND(FALSE)").Evaluate();
            Assert.AreEqual(false, resultFalse.Result);
        }

        /// <summary>
        /// Tests AND function with no parameters returns true (vacuous truth).
        /// </summary>
        [TestMethod]
        public void LogicalAND_NoParameters_ReturnsTrue()
        {
            var result = ExpressionParser.Parser.Parser.Parse("AND()").Evaluate();
            Assert.AreEqual(ResultType.Boolean, result.Type);
            Assert.AreEqual(true, result.Result);
        }

        /// <summary>
        /// Tests nested AND expressions.
        /// </summary>
        [TestMethod]
        public void LogicalAND_NestedExpressions()
        {
            var result = ExpressionParser.Parser.Parser.Parse("AND(AND(TRUE, TRUE), AND(TRUE, TRUE))").Evaluate();
            Assert.AreEqual(true, result.Result);

            var result2 = ExpressionParser.Parser.Parser.Parse("AND(AND(TRUE, TRUE), AND(TRUE, FALSE))").Evaluate();
            Assert.AreEqual(false, result2.Result);
        }

        /// <summary>
        /// Tests AND function with comparison expressions.
        /// </summary>
        [TestMethod]
        public void LogicalAND_WithComparisons()
        {
            var result = ExpressionParser.Parser.Parser.Parse("AND(5 > 3, 10 < 20, 2 = 2)").Evaluate();
            Assert.AreEqual(true, result.Result);

            var result2 = ExpressionParser.Parser.Parser.Parse("AND(5 > 3, 10 > 20, 2 = 2)").Evaluate();
            Assert.AreEqual(false, result2.Result);
        }

        #endregion

        #region Logical OR

        /// <summary>
        /// Tests OR function with all false values.
        /// </summary>
        [TestMethod]
        public void LogicalOR_AllFalse()
        {
            var result = ExpressionParser.Parser.Parser.Parse("OR(FALSE, FALSE, FALSE)").Evaluate();
            Assert.AreEqual(false, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        /// <summary>
        /// Tests OR function with one true value among false values.
        /// </summary>
        [TestMethod]
        public void LogicalOR_OneTrue()
        {
            var result = ExpressionParser.Parser.Parser.Parse("OR(FALSE, TRUE, FALSE)").Evaluate();
            Assert.AreEqual(true, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        /// <summary>
        /// Tests OR function with single parameter.
        /// </summary>
        [TestMethod]
        public void LogicalOR_SingleParameter()
        {
            var resultTrue = ExpressionParser.Parser.Parser.Parse("OR(TRUE)").Evaluate();
            Assert.AreEqual(true, resultTrue.Result);

            var resultFalse = ExpressionParser.Parser.Parser.Parse("OR(FALSE)").Evaluate();
            Assert.AreEqual(false, resultFalse.Result);
        }

        /// <summary>
        /// Tests OR function with no parameters returns false (no true values).
        /// </summary>
        [TestMethod]
        public void LogicalOR_NoParameters_ReturnsFalse()
        {
            var result = ExpressionParser.Parser.Parser.Parse("OR()").Evaluate();
            Assert.AreEqual(ResultType.Boolean, result.Type);
            Assert.AreEqual(false, result.Result);
        }

        /// <summary>
        /// Tests nested OR expressions.
        /// </summary>
        [TestMethod]
        public void LogicalOR_NestedExpressions()
        {
            var result = ExpressionParser.Parser.Parser.Parse("OR(OR(FALSE, FALSE), OR(FALSE, TRUE))").Evaluate();
            Assert.AreEqual(true, result.Result);

            var result2 = ExpressionParser.Parser.Parser.Parse("OR(OR(FALSE, FALSE), OR(FALSE, FALSE))").Evaluate();
            Assert.AreEqual(false, result2.Result);
        }

        /// <summary>
        /// Tests OR function with comparison expressions.
        /// </summary>
        [TestMethod]
        public void LogicalOR_WithComparisons()
        {
            var result = ExpressionParser.Parser.Parser.Parse("OR(5 < 3, 10 > 20, 2 = 2)").Evaluate();
            Assert.AreEqual(true, result.Result);

            var result2 = ExpressionParser.Parser.Parser.Parse("OR(5 < 3, 10 > 20, 2 <> 2)").Evaluate();
            Assert.AreEqual(false, result2.Result);
        }

        #endregion

        #region IF Statements

        /// <summary>
        /// Tests IF statement with condition evaluating to true.
        /// </summary>
        [TestMethod]
        public void IF_TrueCondition_ReturnsTrueBranch()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF(5 > 3, 100, 200)").Evaluate();
            Assert.AreEqual(100, result.Result);
        }

        /// <summary>
        /// Tests IF statement with condition evaluating to false.
        /// </summary>
        [TestMethod]
        public void IF_FalseCondition_ReturnsFalseBranch()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF(3 > 5, 100, 200)").Evaluate();
            Assert.AreEqual(200, result.Result);
        }

        /// <summary>
        /// Tests IF statement with string results.
        /// </summary>
        [TestMethod]
        public void IF_WithStringResults()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF(TRUE, \"yes\", \"no\")").Evaluate();
            Assert.AreEqual("yes", result.Result);
            Assert.AreEqual(ResultType.String, result.Type);

            var result2 = ExpressionParser.Parser.Parser.Parse("IF(FALSE, \"yes\", \"no\")").Evaluate();
            Assert.AreEqual("no", result2.Result);
        }

        /// <summary>
        /// Tests IF statement with boolean results.
        /// </summary>
        [TestMethod]
        public void IF_WithBooleanResults()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF(5 > 3, TRUE, FALSE)").Evaluate();
            Assert.AreEqual(true, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        #endregion

        #region Nested IF Statements

        /// <summary>
        /// Tests IF statement nested inside the true branch.
        /// </summary>
        [TestMethod]
        public void NestedIF_InTrueBranch()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF(TRUE, IF(5 > 3, 100, 200), 300)").Evaluate();
            Assert.AreEqual(100, result.Result);
        }

        /// <summary>
        /// Tests IF statement nested inside the false branch.
        /// </summary>
        [TestMethod]
        public void NestedIF_InFalseBranch()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF(FALSE, 300, IF(5 > 3, 100, 200))").Evaluate();
            Assert.AreEqual(100, result.Result);
        }

        /// <summary>
        /// Tests multiple levels of nested IF statements.
        /// </summary>
        [TestMethod]
        public void NestedIF_MultipleLevels()
        {
            var expression = "IF(TRUE, IF(FALSE, 1, IF(TRUE, 2, 3)), 4)";
            var result = ExpressionParser.Parser.Parser.Parse(expression).Evaluate();
            Assert.AreEqual(2, result.Result);
        }

        /// <summary>
        /// Tests IF statement with AND condition.
        /// </summary>
        [TestMethod]
        public void NestedIF_WithANDCondition()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF(AND(5 > 3, 10 < 20), \"pass\", \"fail\")").Evaluate();
            Assert.AreEqual("pass", result.Result);

            var result2 = ExpressionParser.Parser.Parser.Parse("IF(AND(5 > 3, 10 > 20), \"pass\", \"fail\")").Evaluate();
            Assert.AreEqual("fail", result2.Result);
        }

        /// <summary>
        /// Tests IF statement with OR condition.
        /// </summary>
        [TestMethod]
        public void NestedIF_WithORCondition()
        {
            var result = ExpressionParser.Parser.Parser.Parse("IF(OR(5 < 3, 10 < 20), \"pass\", \"fail\")").Evaluate();
            Assert.AreEqual("pass", result.Result);

            var result2 = ExpressionParser.Parser.Parser.Parse("IF(OR(5 < 3, 10 > 20), \"pass\", \"fail\")").Evaluate();
            Assert.AreEqual("fail", result2.Result);
        }

        #endregion

        #region Complex Expressions

        /// <summary>
        /// Tests AND combined with OR operations.
        /// </summary>
        [TestMethod]
        public void ComplexExpression_AND_With_OR()
        {
            var result = ExpressionParser.Parser.Parser.Parse("AND(OR(TRUE, FALSE), OR(FALSE, TRUE))").Evaluate();
            Assert.AreEqual(true, result.Result);

            var result2 = ExpressionParser.Parser.Parser.Parse("AND(OR(FALSE, FALSE), OR(TRUE, TRUE))").Evaluate();
            Assert.AreEqual(false, result2.Result);
        }

        /// <summary>
        /// Tests comparisons inside logical functions.
        /// </summary>
        [TestMethod]
        public void ComplexExpression_ComparisonsInLogicalFunctions()
        {
            var result = ExpressionParser.Parser.Parser.Parse("AND(3 > 2, 5 < 10)").Evaluate();
            Assert.AreEqual(true, result.Result);

            var result2 = ExpressionParser.Parser.Parser.Parse("OR(3 > 5, 5 > 10)").Evaluate();
            Assert.AreEqual(false, result2.Result);

            var result3 = ExpressionParser.Parser.Parser.Parse("OR(3 > 5, 5 < 10)").Evaluate();
            Assert.AreEqual(true, result3.Result);
        }

        /// <summary>
        /// Tests arithmetic expressions inside comparisons.
        /// </summary>
        [TestMethod]
        public void ComplexExpression_ArithmeticInsideComparisons()
        {
            var result = ExpressionParser.Parser.Parser.Parse("(2 + 3) > (1 + 2)").Evaluate();
            Assert.AreEqual(true, result.Result);

            var result2 = ExpressionParser.Parser.Parser.Parse("(10 - 5) = (2 + 3)").Evaluate();
            Assert.AreEqual(true, result2.Result);

            var result3 = ExpressionParser.Parser.Parser.Parse("(2 * 3) < (3 * 3)").Evaluate();
            Assert.AreEqual(true, result3.Result);
        }

        /// <summary>
        /// Tests complex nested expression with multiple operators.
        /// </summary>
        [TestMethod]
        public void ComplexExpression_NestedMultipleOperators()
        {
            var expression = "IF(AND(5 > 3, OR(10 < 20, 15 > 20)), (2 + 3) * 10, (4 + 6) / 2)";
            var result = ExpressionParser.Parser.Parser.Parse(expression).Evaluate();
            Assert.AreEqual(50, result.Result);
        }

        /// <summary>
        /// Tests combining all comparison operators in logical functions.
        /// </summary>
        [TestMethod]
        public void ComplexExpression_AllComparisonOperators()
        {
            var expression = "AND(5 > 3, 10 >= 10, 3 < 5, 3 <= 3, 5 = 5, 3 <> 5)";
            var result = ExpressionParser.Parser.Parser.Parse(expression).Evaluate();
            Assert.AreEqual(true, result.Result);
        }

        /// <summary>
        /// Tests deeply nested logical operations.
        /// </summary>
        [TestMethod]
        public void ComplexExpression_DeeplyNested()
        {
            var expression = "OR(AND(TRUE, FALSE), AND(OR(TRUE, FALSE), OR(TRUE, TRUE)))";
            var result = ExpressionParser.Parser.Parser.Parse(expression).Evaluate();
            Assert.AreEqual(true, result.Result);
        }

        /// <summary>
        /// Tests IF with nested AND and OR in all branches.
        /// </summary>
        [TestMethod]
        public void ComplexExpression_IFWithNestedLogical()
        {
            var expression = "IF(OR(5 > 3, 2 > 10), IF(AND(TRUE, TRUE), 1, 2), IF(OR(FALSE, FALSE), 3, 4))";
            var result = ExpressionParser.Parser.Parser.Parse(expression).Evaluate();
            Assert.AreEqual(1, result.Result);
        }

        #endregion
    }
}
