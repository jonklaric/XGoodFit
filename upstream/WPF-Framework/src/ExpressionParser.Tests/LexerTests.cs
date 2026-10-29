using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionParser.Tests
{
    /// <summary>
    /// Contains comprehensive unit tests for the Lexer class to validate tokenization
    /// of expression strings including numbers, operators, identifiers, keywords, and literals.
    /// </summary>
    [TestClass()]
    public class LexerTests
    {
        /// <summary>
        /// Tests tokenization of a simple integer number.
        /// </summary>
        [TestMethod()]
        public void TestIntegerNumber()
        {
            var tokens = Lexer.TokenizeStringToList("123");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[0].Type);
            Assert.AreEqual("123", tokens[0].TokenString);
            Assert.AreEqual(TokenClass.Value, tokens[0].TokenGroup);
        }

        /// <summary>
        /// Tests tokenization of a decimal number with a fractional part.
        /// </summary>
        [TestMethod()]
        public void TestDecimalNumber()
        {
            var tokens = Lexer.TokenizeStringToList("123.456");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.DecimalNumber, tokens[0].Type);
            Assert.AreEqual("123.456", tokens[0].TokenString);
            Assert.AreEqual(TokenClass.Value, tokens[0].TokenGroup);
        }

        /// <summary>
        /// Tests tokenization of basic arithmetic operators.
        /// </summary>
        [TestMethod()]
        public void TestBasicOperators()
        {
            var tokens = Lexer.TokenizeStringToList("1+2-3*4/5^6");
            Assert.AreEqual(11, tokens.Count);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[0].Type);
            Assert.AreEqual(TokenType.Addition, tokens[1].Type);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[2].Type);
            Assert.AreEqual(TokenType.Subtraction, tokens[3].Type);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[4].Type);
            Assert.AreEqual(TokenType.Multiplication, tokens[5].Type);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[6].Type);
            Assert.AreEqual(TokenType.Division, tokens[7].Type);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[8].Type);
            Assert.AreEqual(TokenType.Exponent, tokens[9].Type);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[10].Type);
        }

        /// <summary>
        /// Tests tokenization of comparison operators including equals, less than, and greater than.
        /// </summary>
        [TestMethod()]
        public void TestComparisonOperators()
        {
            var tokens = Lexer.TokenizeStringToList("x=5");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.Equals, tokens[1].Type);

            tokens = Lexer.TokenizeStringToList("x<5");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.LessThan, tokens[1].Type);

            tokens = Lexer.TokenizeStringToList("x>5");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.GreaterThan, tokens[1].Type);
        }

        /// <summary>
        /// Tests tokenization of multi-character comparison operators: not equal, less than or equal,
        /// greater than or equal, and alternate not equal syntax.
        /// </summary>
        [TestMethod()]
        public void TestMultiCharacterOperators()
        {
            var tokens = Lexer.TokenizeStringToList("1!=2");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.NotEqual, tokens[1].Type);
            Assert.AreEqual("!=", tokens[1].TokenString);

            tokens = Lexer.TokenizeStringToList("3<=5");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.LessThanOrEqual, tokens[1].Type);
            Assert.AreEqual("<=", tokens[1].TokenString);

            tokens = Lexer.TokenizeStringToList("7>=5");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.GreaterThanOrEqual, tokens[1].Type);
            Assert.AreEqual(">=", tokens[1].TokenString);

            tokens = Lexer.TokenizeStringToList("1<>2");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.NotEqual, tokens[1].Type);
            Assert.AreEqual("<>", tokens[1].TokenString);
        }

        /// <summary>
        /// Tests tokenization of string literals enclosed in single quotes.
        /// </summary>
        [TestMethod()]
        public void TestSingleQuoteString()
        {
            var tokens = Lexer.TokenizeStringToList("'hello'");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.SingleQuote, tokens[0].Type);
            Assert.AreEqual(TokenType.String, tokens[1].Type);
            Assert.AreEqual("hello", tokens[1].TokenString);
            Assert.AreEqual(TokenType.SingleQuote, tokens[2].Type);
        }

        /// <summary>
        /// Tests tokenization of string literals enclosed in double quotes.
        /// </summary>
        [TestMethod()]
        public void TestDoubleQuoteString()
        {
            var tokens = Lexer.TokenizeStringToList("\"world\"");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.DoubleQuote, tokens[0].Type);
            Assert.AreEqual(TokenType.String, tokens[1].Type);
            Assert.AreEqual("world", tokens[1].TokenString);
            Assert.AreEqual(TokenType.DoubleQuote, tokens[2].Type);
        }

        /// <summary>
        /// Tests tokenization of variable names using bracket notation.
        /// </summary>
        [TestMethod()]
        public void TestBracketNotation()
        {
            var tokens = Lexer.TokenizeStringToList("[variable]");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.LeftBracket, tokens[0].Type);
            Assert.AreEqual(TokenType.String, tokens[1].Type);
            Assert.AreEqual("variable", tokens[1].TokenString);
            Assert.AreEqual(TokenType.RightBracket, tokens[2].Type);
        }

        /// <summary>
        /// Tests tokenization of the IF keyword, verifying it is recognized as a function.
        /// </summary>
        [TestMethod()]
        public void TestIfKeyword()
        {
            var tokens = Lexer.TokenizeStringToList("IF");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.If, tokens[0].Type);
            Assert.AreEqual("IF", tokens[0].TokenString);
            Assert.AreEqual(TokenClass.Function, tokens[0].TokenGroup);
        }

        /// <summary>
        /// Tests tokenization of the AND keyword, verifying it is recognized as a function.
        /// </summary>
        [TestMethod()]
        public void TestAndKeyword()
        {
            var tokens = Lexer.TokenizeStringToList("AND");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.And, tokens[0].Type);
            Assert.AreEqual("AND", tokens[0].TokenString);
            Assert.AreEqual(TokenClass.Function, tokens[0].TokenGroup);
        }

        /// <summary>
        /// Tests tokenization of the OR keyword, verifying it is recognized as a function.
        /// </summary>
        [TestMethod()]
        public void TestOrKeyword()
        {
            var tokens = Lexer.TokenizeStringToList("OR");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.Or, tokens[0].Type);
            Assert.AreEqual("OR", tokens[0].TokenString);
            Assert.AreEqual(TokenClass.Function, tokens[0].TokenGroup);
        }

        /// <summary>
        /// Tests tokenization of the ROUND function name.
        /// </summary>
        [TestMethod()]
        public void TestRoundFunction()
        {
            var tokens = Lexer.TokenizeStringToList("ROUND");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.Round, tokens[0].Type);
            Assert.AreEqual("ROUND", tokens[0].TokenString);
            Assert.AreEqual(TokenClass.Function, tokens[0].TokenGroup);
        }

        /// <summary>
        /// Tests tokenization of TRUE and FALSE boolean literals.
        /// </summary>
        [TestMethod()]
        public void TestBooleanLiterals()
        {
            var tokens = Lexer.TokenizeStringToList("TRUE");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.Boolean, tokens[0].Type);
            Assert.AreEqual("TRUE", tokens[0].TokenString);

            tokens = Lexer.TokenizeStringToList("FALSE");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.Boolean, tokens[0].Type);
            Assert.AreEqual("FALSE", tokens[0].TokenString);
        }

        /// <summary>
        /// Tests that whitespace is properly tokenized and can be used as a delimiter.
        /// </summary>
        [TestMethod()]
        public void TestWhitespaceHandling()
        {
            var tokens = Lexer.TokenizeStringToList("1 + 2");
            Assert.AreEqual(5, tokens.Count);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[0].Type);
            Assert.AreEqual(TokenType.Space, tokens[1].Type);
            Assert.AreEqual(TokenType.Addition, tokens[2].Type);
            Assert.AreEqual(TokenType.Space, tokens[3].Type);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[4].Type);
        }

        /// <summary>
        /// Tests tokenization of expressions with multiple consecutive spaces.
        /// </summary>
        [TestMethod()]
        public void TestMultipleSpaces()
        {
            var tokens = Lexer.TokenizeStringToList("1  +  2");
            // Should have multiple space tokens
            Assert.IsTrue(tokens.Count > 5);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[0].Type);
            Assert.AreEqual(TokenType.Space, tokens[1].Type);
            Assert.AreEqual(TokenType.Space, tokens[2].Type);
        }

        /// <summary>
        /// Tests tokenization of parentheses for grouping expressions.
        /// </summary>
        [TestMethod()]
        public void TestParentheses()
        {
            var tokens = Lexer.TokenizeStringToList("(1+2)");
            Assert.AreEqual(5, tokens.Count);
            Assert.AreEqual(TokenType.LeftParenthesis, tokens[0].Type);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[1].Type);
            Assert.AreEqual(TokenType.Addition, tokens[2].Type);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[3].Type);
            Assert.AreEqual(TokenType.RightParenthesis, tokens[4].Type);
        }

        /// <summary>
        /// Tests tokenization of comma separators used in function arguments.
        /// </summary>
        [TestMethod()]
        public void TestCommaInExpression()
        {
            var tokens = Lexer.TokenizeStringToList("1,2,3");
            Assert.AreEqual(5, tokens.Count);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[0].Type);
            Assert.AreEqual(TokenType.Comma, tokens[1].Type);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[2].Type);
            Assert.AreEqual(TokenType.Comma, tokens[3].Type);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[4].Type);
        }

        /// <summary>
        /// Tests tokenization of the ampersand operator used for string concatenation.
        /// </summary>
        [TestMethod()]
        public void TestAmpersandOperator()
        {
            var tokens = Lexer.TokenizeStringToList("a&b");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.String, tokens[0].Type);
            Assert.AreEqual(TokenType.Ampersand, tokens[1].Type);
            Assert.AreEqual("&", tokens[1].TokenString);
            Assert.AreEqual(TokenType.String, tokens[2].Type);
        }

        /// <summary>
        /// Tests tokenization of a complex expression containing functions, operators, and literals.
        /// </summary>
        [TestMethod()]
        public void TestComplexExpression()
        {
            var tokens = Lexer.TokenizeStringToList("IF(x>5,TRUE,FALSE)");
            Assert.IsTrue(tokens.Count > 0);
            Assert.AreEqual(TokenType.If, tokens[0].Type);
            Assert.AreEqual(TokenType.LeftParenthesis, tokens[1].Type);
            Assert.AreEqual(TokenType.String, tokens[2].Type);
            Assert.AreEqual("x", tokens[2].TokenString);
            Assert.AreEqual(TokenType.GreaterThan, tokens[3].Type);
            // Verify the presence of TRUE and FALSE keywords
            Assert.IsTrue(tokens.Any(t => t.Type == TokenType.Boolean && t.TokenString == "TRUE"));
            Assert.IsTrue(tokens.Any(t => t.Type == TokenType.Boolean && t.TokenString == "FALSE"));
        }

        /// <summary>
        /// Tests that keywords are recognized in a case-insensitive manner.
        /// </summary>
        [TestMethod()]
        public void TestCaseInsensitiveKeywords()
        {
            var tokens = Lexer.TokenizeStringToList("if");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.If, tokens[0].Type);

            tokens = Lexer.TokenizeStringToList("If");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.If, tokens[0].Type);

            tokens = Lexer.TokenizeStringToList("true");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.Boolean, tokens[0].Type);
        }

        /// <summary>
        /// Tests that token positions are correctly tracked during tokenization.
        /// </summary>
        [TestMethod()]
        public void TestTokenPositions()
        {
            var tokens = Lexer.TokenizeStringToList("1+2");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(0, tokens[0].StartPosition);
            Assert.AreEqual(1, tokens[1].StartPosition);
            Assert.AreEqual(2, tokens[2].StartPosition);
        }

        /// <summary>
        /// Tests tokenization of identifiers that are not keywords.
        /// </summary>
        [TestMethod()]
        public void TestIdentifiers()
        {
            var tokens = Lexer.TokenizeStringToList("myVariable");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.String, tokens[0].Type);
            Assert.AreEqual("myVariable", tokens[0].TokenString);

            tokens = Lexer.TokenizeStringToList("_test123");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.String, tokens[0].Type);
            Assert.AreEqual("_test123", tokens[0].TokenString);
        }

        /// <summary>
        /// Tests tokenization of additional function names such as ROUND, ROUNDUP, and ROUNDDOWN.
        /// </summary>
        [TestMethod()]
        public void TestAdditionalFunctions()
        {
            var tokens = Lexer.TokenizeStringToList("ROUNDUP");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.RoundUp, tokens[0].Type);

            tokens = Lexer.TokenizeStringToList("ROUNDDOWN");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.RoundDown, tokens[0].Type);

            tokens = Lexer.TokenizeStringToList("CONCATENATE");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.Concatenate, tokens[0].Type);

            tokens = Lexer.TokenizeStringToList("RAND");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.Random, tokens[0].Type);
        }

        /// <summary>
        /// Tests tokenization of curly brackets.
        /// </summary>
        [TestMethod()]
        public void TestCurlyBrackets()
        {
            var tokens = Lexer.TokenizeStringToList("{1}");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.LeftCurlyBracket, tokens[0].Type);
            Assert.AreEqual(TokenType.IntegerNumber, tokens[1].Type);
            Assert.AreEqual(TokenType.RightCurlyBracket, tokens[2].Type);
        }

        /// <summary>
        /// Tests tokenization of a standalone decimal point (edge case).
        /// </summary>
        [TestMethod()]
        public void TestStandaloneDecimalPoint()
        {
            var tokens = Lexer.TokenizeStringToList(".");
            Assert.AreEqual(1, tokens.Count);
            // A standalone decimal point should be tokenized as a String per the lexer logic
            Assert.AreEqual(TokenType.String, tokens[0].Type);
            Assert.AreEqual(".", tokens[0].TokenString);
        }

        /// <summary>
        /// Tests tokenization of conversion functions such as CINT, CDBL, CSTR, and CBOOL.
        /// </summary>
        [TestMethod()]
        public void TestConversionFunctions()
        {
            var tokens = Lexer.TokenizeStringToList("CINT");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.ConvertToInteger, tokens[0].Type);

            tokens = Lexer.TokenizeStringToList("CDBL");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.ConvertToDouble, tokens[0].Type);

            tokens = Lexer.TokenizeStringToList("CSTR");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.ConvertToString, tokens[0].Type);

            tokens = Lexer.TokenizeStringToList("CBOOL");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.ConvertToBoolean, tokens[0].Type);
        }

        /// <summary>
        /// Tests tokenization of string functions such as LEFT, RIGHT, LEN, and SUBSTRING.
        /// </summary>
        [TestMethod()]
        public void TestStringFunctions()
        {
            var tokens = Lexer.TokenizeStringToList("LEFT");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.Left, tokens[0].Type);

            tokens = Lexer.TokenizeStringToList("RIGHT");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.Right, tokens[0].Type);

            tokens = Lexer.TokenizeStringToList("LEN");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.Length, tokens[0].Type);

            tokens = Lexer.TokenizeStringToList("SUBSTRING");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.Substring, tokens[0].Type);
        }

        /// <summary>
        /// Tests tokenization of expressions with numbers that start with a decimal point.
        /// </summary>
        [TestMethod()]
        public void TestNumbersStartingWithDecimal()
        {
            var tokens = Lexer.TokenizeStringToList(".5");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.DecimalNumber, tokens[0].Type);
            Assert.AreEqual(".5", tokens[0].TokenString);

            tokens = Lexer.TokenizeStringToList(".123");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.DecimalNumber, tokens[0].Type);
            Assert.AreEqual(".123", tokens[0].TokenString);
        }

        /// <summary>
        /// Tests tokenization with mixed strings and brackets to ensure proper variable name extraction.
        /// </summary>
        [TestMethod()]
        public void TestBracketWithSpaces()
        {
            var tokens = Lexer.TokenizeStringToList("[my variable]");
            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual(TokenType.LeftBracket, tokens[0].Type);
            Assert.AreEqual(TokenType.String, tokens[1].Type);
            Assert.AreEqual("my variable", tokens[1].TokenString);
            Assert.AreEqual(TokenType.RightBracket, tokens[2].Type);
        }

        /// <summary>
        /// Tests tokenization of unclosed string literals to verify behavior.
        /// </summary>
        [TestMethod()]
        public void TestUnclosedStringLiterals()
        {
            var tokens = Lexer.TokenizeStringToList("'hello");
            // Should have single quote and string but no closing quote
            Assert.IsTrue(tokens.Count >= 2);
            Assert.AreEqual(TokenType.SingleQuote, tokens[0].Type);
            Assert.AreEqual(TokenType.String, tokens[1].Type);
            Assert.AreEqual("hello", tokens[1].TokenString);

            tokens = Lexer.TokenizeStringToList("\"world");
            Assert.IsTrue(tokens.Count >= 2);
            Assert.AreEqual(TokenType.DoubleQuote, tokens[0].Type);
            Assert.AreEqual(TokenType.String, tokens[1].Type);
            Assert.AreEqual("world", tokens[1].TokenString);
        }

        /// <summary>
        /// Tests tokenization with operation order verification for different operator types.
        /// </summary>
        [TestMethod()]
        public void TestOperationOrder()
        {
            var tokens = Lexer.TokenizeStringToList("+");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(4, tokens[0].OperationOrder);

            tokens = Lexer.TokenizeStringToList("*");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(3, tokens[0].OperationOrder);

            tokens = Lexer.TokenizeStringToList("^");
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(2, tokens[0].OperationOrder);
        }
    }
}
