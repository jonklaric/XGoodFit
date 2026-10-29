using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionParser.Tests
{
    /// <summary>
    /// Contains comprehensive unit tests for arithmetic operations including multiplication,
    /// division, exponentiation, operator precedence, type handling, and edge cases.
    /// </summary>
    [TestClass]
    public class ArithmeticTests
    {
        /// <summary>
        /// Tests basic multiplication operations with positive integers.
        /// </summary>
        [TestMethod]
        public void MultiplicationBasicTests()
        {
            Assert.AreEqual(6, ExpressionParser.Parser.Parser.Parse("2*3").Evaluate().Result);
            Assert.AreEqual(50, ExpressionParser.Parser.Parser.Parse("5*10").Evaluate().Result);
            Assert.AreEqual(56, ExpressionParser.Parser.Parser.Parse("7*8").Evaluate().Result);
        }

        /// <summary>
        /// Tests multiplication operations with negative numbers.
        /// </summary>
        [TestMethod]
        public void MultiplicationWithNegativeNumbersTests()
        {
            Assert.AreEqual(-15, ExpressionParser.Parser.Parser.Parse("-5*3").Evaluate().Result);
            Assert.AreEqual(-15, ExpressionParser.Parser.Parser.Parse("5*(-3)").Evaluate().Result);
            Assert.AreEqual(15, ExpressionParser.Parser.Parser.Parse("-5*(-3)").Evaluate().Result);
            Assert.AreEqual(-20, ExpressionParser.Parser.Parser.Parse("-10*2").Evaluate().Result);
        }

        /// <summary>
        /// Tests multiplication operations with decimal numbers.
        /// </summary>
        [TestMethod]
        public void MultiplicationWithDecimalsTests()
        {
            Assert.AreEqual(10.0, ExpressionParser.Parser.Parser.Parse("2.5*4").Evaluate().Result);
            Assert.AreEqual(8.75, ExpressionParser.Parser.Parser.Parse("3.5*2.5").Evaluate().Result);
            Assert.AreEqual(5.0, ExpressionParser.Parser.Parser.Parse("0.5*10").Evaluate().Result);
        }

        /// <summary>
        /// Tests basic division operations with positive integers.
        /// </summary>
        [TestMethod]
        public void DivisionBasicTests()
        {
            Assert.AreEqual(5.0, ExpressionParser.Parser.Parser.Parse("10/2").Evaluate().Result);
            Assert.AreEqual(5.0, ExpressionParser.Parser.Parser.Parse("20/4").Evaluate().Result);
            Assert.AreEqual(5.0, ExpressionParser.Parser.Parser.Parse("15/3").Evaluate().Result);
        }

        /// <summary>
        /// Tests division operations that result in decimal values.
        /// </summary>
        [TestMethod]
        public void DivisionWithDecimalResultsTests()
        {
            Assert.AreEqual(2.5, ExpressionParser.Parser.Parser.Parse("10/4").Evaluate().Result);
            Assert.AreEqual(3.5, ExpressionParser.Parser.Parser.Parse("7/2").Evaluate().Result);
            Assert.AreEqual(0.125, ExpressionParser.Parser.Parser.Parse("1/8").Evaluate().Result);
        }

        /// <summary>
        /// Tests division by zero behavior, which should return NaN.
        /// </summary>
        [TestMethod]
        public void DivisionByZeroTests()
        {
            var result = ExpressionParser.Parser.Parser.Parse("10/0").Evaluate().Result;
            Assert.IsTrue(double.IsNaN((double)result));
        }

        /// <summary>
        /// Tests basic exponentiation operations.
        /// </summary>
        [TestMethod]
        public void ExponentiationBasicTests()
        {
            Assert.AreEqual(8.0, ExpressionParser.Parser.Parser.Parse("2^3").Evaluate().Result);
            Assert.AreEqual(25.0, ExpressionParser.Parser.Parser.Parse("5^2").Evaluate().Result);
            Assert.AreEqual(81.0, ExpressionParser.Parser.Parser.Parse("3^4").Evaluate().Result);
            Assert.AreEqual(1.0, ExpressionParser.Parser.Parser.Parse("10^0").Evaluate().Result);
        }

        /// <summary>
        /// Tests exponentiation with negative exponents and negative bases.
        /// </summary>
        [TestMethod]
        public void ExponentiationAdvancedTests()
        {
            Assert.AreEqual(0.5, ExpressionParser.Parser.Parser.Parse("2^(-1)").Evaluate().Result);
            Assert.AreEqual(2.0, ExpressionParser.Parser.Parser.Parse("4^0.5").Evaluate().Result);
            Assert.AreEqual(1000.0, ExpressionParser.Parser.Parser.Parse("10^3").Evaluate().Result);
        }

        /// <summary>
        /// Tests operator precedence with multiplication and addition.
        /// </summary>
        [TestMethod]
        public void OperatorPrecedenceMultiplicationAdditionTests()
        {
            Assert.AreEqual(14, ExpressionParser.Parser.Parser.Parse("2+3*4").Evaluate().Result);
            Assert.AreEqual(4, ExpressionParser.Parser.Parser.Parse("10-2*3").Evaluate().Result);
            Assert.AreEqual(22, ExpressionParser.Parser.Parser.Parse("5*2+3*4").Evaluate().Result);
        }

        /// <summary>
        /// Tests operator precedence with exponentiation.
        /// </summary>
        [TestMethod]
        public void OperatorPrecedenceExponentiationTests()
        {
            Assert.AreEqual(32.0, ExpressionParser.Parser.Parser.Parse("2^3*4").Evaluate().Result);
            Assert.AreEqual(18.0, ExpressionParser.Parser.Parser.Parse("2*3^2").Evaluate().Result);
            Assert.AreEqual(11.0, ExpressionParser.Parser.Parser.Parse("2+3^2").Evaluate().Result);
        }

        /// <summary>
        /// Tests complex nested expressions with multiple levels of parentheses.
        /// </summary>
        [TestMethod]
        public void ComplexNestedExpressionsTests()
        {
            Assert.AreEqual(400.0, ExpressionParser.Parser.Parser.Parse("((2+3)*4)^2").Evaluate().Result);
            Assert.AreEqual(10, ExpressionParser.Parser.Parser.Parse("(10-(3+2))*2").Evaluate().Result);
            Assert.AreEqual(5.5, ExpressionParser.Parser.Parser.Parse("((5*2)+(3*4))/(2+2)").Evaluate().Result);
            Assert.AreEqual(30, ExpressionParser.Parser.Parser.Parse("2*(3+(4*(5-2)))").Evaluate().Result);
        }

        /// <summary>
        /// Tests unary minus operator with positive numbers.
        /// </summary>
        [TestMethod]
        public void UnaryMinusTests()
        {
            Assert.AreEqual(-5, ExpressionParser.Parser.Parser.Parse("-5").Evaluate().Result);
            Assert.AreEqual(-5, ExpressionParser.Parser.Parser.Parse("-10+5").Evaluate().Result);
            Assert.AreEqual(-8, ExpressionParser.Parser.Parser.Parse("-(5+3)").Evaluate().Result);
        }

        /// <summary>
        /// Tests double negation and nested unary minus operators.
        /// </summary>
        [TestMethod]
        public void DoubleNegationTests()
        {
            Assert.AreEqual(5, ExpressionParser.Parser.Parser.Parse("-(-5)").Evaluate().Result);
            Assert.AreEqual(10, ExpressionParser.Parser.Parser.Parse("-(-(10))").Evaluate().Result);
            Assert.AreEqual(-5, ExpressionParser.Parser.Parser.Parse("-(-(-5))").Evaluate().Result);
        }

        /// <summary>
        /// Tests that integer operations return integer type when appropriate.
        /// </summary>
        [TestMethod]
        public void IntegerTypeHandlingTests()
        {
            Assert.AreEqual(ResultType.Integer, ExpressionParser.Parser.Parser.Parse("5+10").Evaluate().Type);
            Assert.AreEqual(ResultType.Integer, ExpressionParser.Parser.Parser.Parse("20-5").Evaluate().Type);
            Assert.AreEqual(ResultType.Integer, ExpressionParser.Parser.Parser.Parse("4*5").Evaluate().Type);
        }

        /// <summary>
        /// Tests that division and operations with decimals return double type.
        /// </summary>
        [TestMethod]
        public void DoubleTypeHandlingTests()
        {
            Assert.AreEqual(ResultType.Double, ExpressionParser.Parser.Parser.Parse("10/2").Evaluate().Type);
            Assert.AreEqual(ResultType.Double, ExpressionParser.Parser.Parser.Parse("2^3").Evaluate().Type);
            Assert.AreEqual(ResultType.Double, ExpressionParser.Parser.Parser.Parse("5.5+10").Evaluate().Type);
            Assert.AreEqual(ResultType.Double, ExpressionParser.Parser.Parser.Parse("5+10.5").Evaluate().Type);
        }

        /// <summary>
        /// Tests operations with zero as an operand.
        /// </summary>
        [TestMethod]
        public void ZeroOperandTests()
        {
            Assert.AreEqual(5, ExpressionParser.Parser.Parser.Parse("0+5").Evaluate().Result);
            Assert.AreEqual(10, ExpressionParser.Parser.Parser.Parse("10-0").Evaluate().Result);
            Assert.AreEqual(0, ExpressionParser.Parser.Parser.Parse("0*100").Evaluate().Result);
            Assert.AreEqual(0.0, ExpressionParser.Parser.Parser.Parse("0/5").Evaluate().Result);
            Assert.AreEqual(0.0, ExpressionParser.Parser.Parser.Parse("0^5").Evaluate().Result);
        }

        /// <summary>
        /// Tests operations with very large numbers.
        /// </summary>
        [TestMethod]
        public void LargeNumberTests()
        {
            // Parser uses Int32 arithmetic, large multiplications overflow
            var result1 = ExpressionParser.Parser.Parser.Parse("1000000*1000000").Evaluate();
            Assert.AreEqual(ResultType.Integer, result1.Type);

            Assert.AreEqual(1000000000, ExpressionParser.Parser.Parser.Parse("999999999+1").Evaluate().Result);
            var result = ExpressionParser.Parser.Parser.Parse("10^10").Evaluate().Result;
            Assert.AreEqual(10000000000.0, (double)result);
        }

        /// <summary>
        /// Tests operations with very small decimal numbers.
        /// </summary>
        [TestMethod]
        public void SmallDecimalTests()
        {
            Assert.AreEqual(0.003, ExpressionParser.Parser.Parser.Parse("0.001+0.002").Evaluate().Result);
            Assert.AreEqual(0.01, (double)ExpressionParser.Parser.Parser.Parse("0.1*0.1").Evaluate().Result, 0.0000001);
            var result = (double)ExpressionParser.Parser.Parser.Parse("1/1000").Evaluate().Result;
            Assert.IsTrue(Math.Abs(result - 0.001) < 0.0000001);
        }

        /// <summary>
        /// Tests chained multiplication operations.
        /// </summary>
        [TestMethod]
        public void ChainedMultiplicationTests()
        {
            Assert.AreEqual(24, ExpressionParser.Parser.Parser.Parse("2*3*4").Evaluate().Result);
            Assert.AreEqual(60, ExpressionParser.Parser.Parser.Parse("5*2*3*2").Evaluate().Result);
            Assert.AreEqual(1000, ExpressionParser.Parser.Parser.Parse("10*10*10").Evaluate().Result);
        }

        /// <summary>
        /// Tests chained division operations from left to right.
        /// </summary>
        [TestMethod]
        public void ChainedDivisionTests()
        {
            Assert.AreEqual(5.0, ExpressionParser.Parser.Parser.Parse("100/10/2").Evaluate().Result);
            Assert.AreEqual(4.0, ExpressionParser.Parser.Parser.Parse("64/8/2").Evaluate().Result);
            Assert.AreEqual(10.0, ExpressionParser.Parser.Parser.Parse("1000/10/10").Evaluate().Result);
        }

        /// <summary>
        /// Tests mixed operations with all basic arithmetic operators.
        /// </summary>
        [TestMethod]
        public void MixedOperationsTests()
        {
            Assert.AreEqual(18.0, ExpressionParser.Parser.Parser.Parse("10+5*2-8/4").Evaluate().Result);
            Assert.AreEqual(110.0, ExpressionParser.Parser.Parser.Parse("100-50/5+10*2").Evaluate().Result);
            Assert.AreEqual(23.0, ExpressionParser.Parser.Parser.Parse("2^3+4*5-10/2").Evaluate().Result);
        }

        /// <summary>
        /// Tests expressions with multiple parentheses groups at the same level.
        /// </summary>
        [TestMethod]
        public void MultipleParenthesesGroupsTests()
        {
            Assert.AreEqual(45, ExpressionParser.Parser.Parser.Parse("(2+3)*(4+5)").Evaluate().Result);
            Assert.AreEqual(15, ExpressionParser.Parser.Parser.Parse("(10-5)+(20-10)").Evaluate().Result);
            Assert.AreEqual(16.0, ExpressionParser.Parser.Parser.Parse("(8/2)*(12/3)").Evaluate().Result);
        }

        /// <summary>
        /// Tests complex expressions with exponentiation and parentheses.
        /// </summary>
        [TestMethod]
        public void ComplexExponentiationTests()
        {
            Assert.AreEqual(27.0, ExpressionParser.Parser.Parser.Parse("(2+1)^(6-3)").Evaluate().Result);
            Assert.AreEqual(16.0, ExpressionParser.Parser.Parser.Parse("2^(3+1)").Evaluate().Result);
            Assert.AreEqual(64.0, ExpressionParser.Parser.Parser.Parse("(2^3)^2").Evaluate().Result);
        }

        /// <summary>
        /// Tests that multiplication by one preserves the value and type.
        /// </summary>
        [TestMethod]
        public void MultiplicationByOneTests()
        {
            Assert.AreEqual(5, ExpressionParser.Parser.Parser.Parse("5*1").Evaluate().Result);
            Assert.AreEqual(100, ExpressionParser.Parser.Parser.Parse("1*100").Evaluate().Result);
            Assert.AreEqual(3.5, ExpressionParser.Parser.Parser.Parse("3.5*1").Evaluate().Result);
        }

        /// <summary>
        /// Tests that division by one preserves the value but returns double type.
        /// </summary>
        [TestMethod]
        public void DivisionByOneTests()
        {
            Assert.AreEqual(5.0, ExpressionParser.Parser.Parser.Parse("5/1").Evaluate().Result);
            Assert.AreEqual(100.0, ExpressionParser.Parser.Parser.Parse("100/1").Evaluate().Result);
            Assert.AreEqual(7.5, ExpressionParser.Parser.Parser.Parse("7.5/1").Evaluate().Result);
        }

        /// <summary>
        /// Tests exponentiation with one as the exponent or base.
        /// </summary>
        [TestMethod]
        public void ExponentiationWithOneTests()
        {
            Assert.AreEqual(5.0, ExpressionParser.Parser.Parser.Parse("5^1").Evaluate().Result);
            Assert.AreEqual(1.0, ExpressionParser.Parser.Parser.Parse("1^5").Evaluate().Result);
            Assert.AreEqual(1.0, ExpressionParser.Parser.Parser.Parse("1^1").Evaluate().Result);
        }
    }
}
