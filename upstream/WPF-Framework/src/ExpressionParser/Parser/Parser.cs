using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExpressionParser.Parser
{
    /// <summary>
    /// Provides static methods for parsing tokenized expressions into abstract syntax trees (AST).
    /// The parser uses operator precedence and recursive descent parsing to build a tree of IParserNode objects
    /// that can be evaluated to compute expression results.
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// Parses a list of tokens into a parse tree that can be evaluated to compute the expression result.
        /// The parser builds an abstract syntax tree respecting operator precedence and handles functions, variables, and literals.
        /// </summary>
        /// <param name="tokens">The list of tokens to parse, typically produced by the Lexer.</param>
        /// <param name="ignoreCase">Specifies whether string comparisons should be case-insensitive.</param>
        /// <param name="availableVariables">Optional dictionary mapping variable names to their expected types. Variables not in this dictionary will be marked as undeclared.</param>
        /// <returns>A parse tree root node that can be evaluated; returns null if the token list is null.</returns>
        /// <remarks>
        /// <para>
        /// <b>Threading contract:</b> the static <see cref="Parse(List{Token}, bool, Dictionary{string, ResultType})"/> method is safe to invoke
        /// from multiple threads concurrently because each invocation builds an independent AST. However the
        /// returned <see cref="IParserNode"/> tree is <i>not</i> thread-safe per AST instance — some nodes (notably
        /// <see cref="ExpressionParser.IncrementNode"/>) carry mutable evaluation state. Once an AST is produced, treat it
        /// as a single-threaded resource: confine all <c>Evaluate()</c>/<c>Simplify()</c> calls and variable-value
        /// updates on its <see cref="VariableNode"/>s to one thread, or wrap access in your own synchronization.
        /// </para>
        /// </remarks>
        public static IParserNode Parse(List<Token> tokens, bool ignoreCase = false, Dictionary<string, ResultType> availableVariables = null)
        {
            if (tokens == null)
                return null;
            var variables = availableVariables ?? new Dictionary<string, ResultType>();
            var stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            //
            var tokenStack = new Stack<Token>(tokens);
            tokenStack = new Stack<Token>(tokenStack); // reverse the stack so that popping the tokens will be in front to back order.
            IParserNode parseNode = null;
            while (tokenStack.Count != 0)
                parseNode = Parse(tokenStack, parseNode, 255, variables, stringComparison);
            return parseNode;
        }
        /// <summary>
        /// Parses an expression string into a parse tree that can be evaluated to compute the expression result.
        /// This method first tokenizes the string using the Lexer, then builds an abstract syntax tree respecting operator precedence.
        /// </summary>
        /// <param name="stringToParse">The expression string to parse.</param>
        /// <param name="ignoreCase">Specifies whether string comparisons should be case-insensitive.</param>
        /// <param name="availableVariables">Optional dictionary mapping variable names to their expected types. Variables not in this dictionary will be marked as undeclared.</param>
        /// <returns>A parse tree root node that can be evaluated; returns null if the input string is null.</returns>
        /// <remarks>
        /// <para>
        /// <b>Threading contract:</b> the static <see cref="Parse(string, bool, Dictionary{string, ResultType})"/> method is safe to invoke
        /// from multiple threads concurrently because each invocation builds an independent AST. However the
        /// returned <see cref="IParserNode"/> tree is <i>not</i> thread-safe per AST instance — some nodes (notably
        /// <see cref="ExpressionParser.IncrementNode"/>) carry mutable evaluation state. Once an AST is produced, treat it
        /// as a single-threaded resource: confine all <c>Evaluate()</c>/<c>Simplify()</c> calls and variable-value
        /// updates on its <see cref="VariableNode"/>s to one thread, or wrap access in your own synchronization.
        /// </para>
        /// </remarks>
        public static IParserNode Parse(string stringToParse, bool ignoreCase = false, Dictionary<string, ResultType> availableVariables = null)
        {
            if (stringToParse == null)
                return null;
            var variables = availableVariables ?? new Dictionary<string, ResultType>();
            var stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            //
            var tokens = Lexer.TokenizeStringToList(stringToParse);
            tokens.Reverse();
            var tokenStack = new Stack<Token>(tokens);
            IParserNode parseNode = null;
            while (tokenStack.Count != 0)
                parseNode = Parse(tokenStack, parseNode, 255, variables, stringComparison);
            return parseNode;
        }

        /// <summary>
        /// Parses tokens recursively to construct a parse tree based on operator precedence.
        /// This is the core recursive parsing method that implements operator precedence climbing.
        /// </summary>
        /// <param name="tokenStack">The stack of tokens to process.</param>
        /// <param name="leftNode">The current left node in the parse tree, or null if starting a new subexpression.</param>
        /// <param name="orderID">The operator precedence threshold; operators with precedence greater than or equal to this value will not be consumed.</param>
        /// <param name="availableVariables">Dictionary of known variables and their expected types.</param>
        /// <param name="stringComparison">The string comparison mode for case sensitivity in string operations.</param>
        /// <returns>The root node of the parsed subtree, or null if no tokens could be parsed.</returns>
        private static IParserNode Parse(Stack<Token> tokenStack, IParserNode leftNode, int orderID, Dictionary<string, ResultType> availableVariables, StringComparison stringComparison)
        {
            Token token;
            var functionParameterErrors = new List<string>();
            IParserNode newNode = null;
            do
            {
                if (tokenStack.Count == 0)
                    return leftNode;
                if (tokenStack.Peek().OperationOrder >= orderID)
                    break;
                token = tokenStack.Pop();
                switch (token.TokenGroup)
                {
                    case TokenClass.Value:
                        {
                            switch (token.Type)
                            {
                                case TokenType.IntegerNumber:
                                    {
                                        if (int.TryParse(token.TokenString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                                            newNode = new IntegerNode(intValue);
                                        else if (long.TryParse(token.TokenString, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
                                            // Out of int range but representable as long — promote to a
                                            // double-backed DecimalNode rather than silently producing
                                            // IntegerNode(0), which previously caused 9999999999 * 2 to
                                            // evaluate to 0.
                                            newNode = new DecimalNode((double)longValue);
                                        else if (double.TryParse(token.TokenString, NumberStyles.Integer, CultureInfo.InvariantCulture, out double dblValue))
                                            newNode = new DecimalNode(dblValue);
                                        else
                                        {
                                            var fallback = new IntegerNode(0);
                                            fallback.GetErrors.Add(new ParseError(token, "integer literal '" + token.TokenString + "' is not parseable; using 0 as a placeholder."));
                                            newNode = fallback;
                                        }
                                        break;
                                    }
                                case TokenType.DecimalNumber:
                                    {
                                        if (double.TryParse(token.TokenString, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double doubleValue))
                                            newNode = new DecimalNode(doubleValue);
                                        else
                                            newNode = new DecimalNode(0.0);
                                        break;
                                    }
                                case TokenType.String:
                                    {
                                        newNode = new StringNode(token.TokenString);
                                        break;
                                    }
                                case TokenType.Boolean:
                                    {
                                        newNode = new BooleanNode(bool.Parse(token.TokenString));
                                        break;
                                    }

                                default:
                                    {
                                        newNode = new StringNode(token.TokenString);
                                        break;
                                    }
                            }

                            break;
                        }
                    case TokenClass.Operator:
                        {
                            switch (token.Type)
                            {
                                case TokenType.Addition:
                                    {
                                        if (leftNode == null)
                                            leftNode = new IntegerNode(0); // Solves unary operator (e.g. "-1+2)
                                        leftNode = new NumericBinaryNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), (a, b) => a + b, token);
                                        break;
                                    }
                                case TokenType.Subtraction:
                                    {
                                        if (leftNode == null)
                                            leftNode = new IntegerNode(0); // Solves unary operator (e.g. "-1+2)
                                        leftNode = new NumericBinaryNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), (a, b) => a - b, token);
                                        break;
                                    }
                                case TokenType.Multiplication:
                                    {
                                        leftNode = new NumericBinaryNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), (a, b) => a * b, token);
                                        break;
                                    }
                                case TokenType.Division:
                                    {
                                        leftNode = new NumericBinaryNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), (a, b) => b == 0.0 ? double.NaN : a / b, token);
                                        break;
                                    }
                                case TokenType.Exponent:
                                    {
                                        leftNode = new NumericBinaryNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), (a, b) => Math.Pow(a, b), token);
                                        break;
                                    }
                                case TokenType.Equals:
                                    {
                                        if (leftNode == null && token.StartPosition == 0)
                                            continue; // This essentially fixes the copy and paste from excel error.
                                        leftNode = new BooleanBinaryNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), (a, b) => (a.Evaluate().Result?.ToString() ?? "").Equals(b.Evaluate().Result?.ToString() ?? "", stringComparison), false, token);
                                        break;
                                    }
                                case TokenType.NotEqual:
                                    {
                                        leftNode = new BooleanBinaryNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), (a, b) => (a.Evaluate().Result?.ToString() ?? "") != (b.Evaluate().Result?.ToString() ?? ""), false, token);
                                        break;
                                    }
                                case TokenType.LessThan:
                                    {
                                        leftNode = new BooleanBinaryNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), (a, b) => Convert.ToDouble(a.Evaluate().Result) < Convert.ToDouble(b.Evaluate().Result), true, token);
                                        break;
                                    }
                                case TokenType.LessThanOrEqual:
                                    {
                                        leftNode = new BooleanBinaryNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), (a, b) => Convert.ToDouble(a.Evaluate().Result) <= Convert.ToDouble(b.Evaluate().Result), true, token);
                                        break;
                                    }
                                case TokenType.GreaterThan:
                                    {
                                        leftNode = new BooleanBinaryNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), (a, b) => Convert.ToDouble(a.Evaluate().Result) > Convert.ToDouble(b.Evaluate().Result), true, token);
                                        break;
                                    }
                                case TokenType.GreaterThanOrEqual:
                                    {
                                        leftNode = new BooleanBinaryNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), (a, b) => Convert.ToDouble(a.Evaluate().Result) >= Convert.ToDouble(b.Evaluate().Result), true, token);
                                        break;
                                    }
                                case TokenType.Ampersand:
                                    {
                                        leftNode = new AmpersandNode(leftNode, Parse(tokenStack, null, token.OperationOrder, availableVariables, stringComparison), token);
                                        break;
                                    }
                            }

                            break;
                        }
                    case TokenClass.Function:
                        {
                            switch (token.Type)
                            {
                                case TokenType.If:
                                    {
                                        newNode = new IfNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.And:
                                case TokenType.Or:
                                    {
                                        newNode = new AndOrNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.Right:
                                    {
                                        newNode = new RightStringNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.Left:
                                    {
                                        newNode = new LeftStringNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.Length:
                                    {
                                        newNode = new StringLengthNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.Random:
                                case TokenType.RandomInteger:
                                    {
                                        newNode = new RandomGeneratorNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.RandomBetween:
                                case TokenType.RandomIntegerBetween:
                                    {
                                        newNode = new RandomBetweenNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.Round:
                                case TokenType.RoundUp:
                                case TokenType.RoundDown:
                                    {
                                        newNode = new RoundNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.Increment:
                                    {
                                        newNode = new IncrementNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.NormalInverse:
                                    {
                                        var normalParams = GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison);
                                        newNode = new StringNode(token.TokenString);
                                        newNode.GetErrors.Add(new ParseError(token, "The NormalInverse function is not yet implemented."));
                                        break;
                                    }
                                case TokenType.TriangularInverse:
                                    {
                                        var triangularParams = GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison);
                                        newNode = new StringNode(token.TokenString);
                                        newNode.GetErrors.Add(new ParseError(token, "The TriangularInverse function is not yet implemented."));
                                        break;
                                    }
                                case TokenType.IndexOf:
                                    {
                                        newNode = new InStringNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token, stringComparison);
                                        break;
                                    }
                                case TokenType.Substring:
                                    {
                                        newNode = new SubStringNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.Contains:
                                    {
                                        newNode = new ContainsNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.Concatenate:
                                    {
                                        newNode = new ConcatenateNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }
                                case TokenType.ConvertToDouble:
                                case TokenType.ConvertToBoolean:
                                case TokenType.ConvertToInteger:
                                case TokenType.ConvertToString:
                                    {
                                        newNode = new ConverterNode(GetFunctionParameters(tokenStack, ref functionParameterErrors, availableVariables, stringComparison), functionParameterErrors, token);
                                        break;
                                    }

                                default:
                                    {
                                        newNode = new StringNode(token.TokenString);
                                        break;
                                    }
                            }

                            break;
                        }
                    case TokenClass.Other:
                        {
                            if (token.Type == TokenType.Space)
                                continue; // ignore space tokens
                            switch (token.Type)
                            {
                                case TokenType.LeftParenthesis:
                                    {
                                        newNode = Parse(tokenStack, null, 14, availableVariables, stringComparison);
                                        if (TestNextToken(tokenStack, TokenType.RightParenthesis))
                                        {
                                            tokenStack.Pop(); // remove the right parenthesis from the stack.
                                        }
                                        else if (!(newNode == null))
                                            newNode.GetErrors.Add(new ParseError(token, "Missing an end parenthesis."));
                                        else
                                            continue;
                                        break;
                                    }
                                case TokenType.SingleQuote:
                                    {
                                        newNode = GetEncapsulatedText(tokenStack, "'", TokenType.SingleQuote, "'");
                                        break;
                                    }
                                case TokenType.DoubleQuote:
                                    {
                                        newNode = GetEncapsulatedText(tokenStack, "\"", TokenType.DoubleQuote, "\"");
                                        break;
                                    }
                                case TokenType.LeftBracket:
                                    {
                                        newNode = GetVariable(tokenStack, token, availableVariables);
                                        break;
                                    }

                                default:
                                    {
                                        newNode = new StringNode(token.TokenString);
                                        break;
                                    }
                            }

                            break;
                        }

                    default:
                        {
                            newNode = new StringNode(token.TokenString);
                            break;
                        }
                }
                if (token.TokenGroup != TokenClass.Operator)
                {
                    if (!(leftNode == null) && !(newNode == null))
                        newNode.GetErrors.Add(new ParseError(token, "Cannot have two values next to each other without an operator (e.g. + or -)."));
                    leftNode = newNode;
                }
            }
            while (true);
            // 
            return leftNode;
        }

        /// <summary>
        /// Extracts encapsulated text tokens and validates matching delimiters.
        /// </summary>
        /// <param name="tokenStack">The token stack to evaluate.</param>
        /// <param name="initialTokenString">The starting delimiter string.</param>
        /// <param name="endTokenType">The expected token type.</param>
        /// <param name="endTokenString">The expected ending delimiter string.</param>
        /// <returns>A StringNode with the encapsulated content or error info.</returns>
        private static StringNode GetEncapsulatedText(Stack<Token> tokenStack, string initialTokenString, TokenType endTokenType, string endTokenString)
        {
            StringNode newNode;
            // 
            if (TestNextToken(tokenStack, TokenType.String))
            {
                var stringToken = tokenStack.Pop();
                newNode = new StringNode(stringToken.TokenString);
                // 
                if (TestNextToken(tokenStack, endTokenType) == false)
                {
                    newNode.GetErrors.Add(new ParseError(stringToken, "Encapsulated text " + initialTokenString + "..." + endTokenString + " is missing the ending character " + endTokenString + ". Encapsulated text must have a starting and ending character (e.g. 'The large ocelot')."));
                }
                else
                {
                    tokenStack.Pop();
                }
            }
            else if (TestNextToken(tokenStack, endTokenType))
            {
                newNode = new StringNode("");
                tokenStack.Pop();
            }
            else
            {
                newNode = new StringNode(initialTokenString);
            }
            // 
            return newNode;
        }

        /// <summary>
        /// Parses and validates a variable from bracketed token syntax.
        /// </summary>
        /// <param name="tokenStack">The token stack to parse.</param>
        /// <param name="leftBracketToken">The initial bracket token triggering variable parsing.</param>
        /// <param name="availableVariables">Dictionary of known variable types.</param>
        /// <returns>VariableNode with type and error info if applicable.</returns>
        private static VariableNode GetVariable(Stack<Token> tokenStack, Token leftBracketToken, Dictionary<string, ResultType> availableVariables)
        {
            if (TestNextToken(tokenStack, TokenType.String) == false)
                return new VariableNode(null, default, leftBracketToken);
            // 
            var stringToken = tokenStack.Pop();
            var variableType = availableVariables.ContainsKey(stringToken.TokenString) ? availableVariables[stringToken.TokenString] : ResultType.UnDeclared;
            var newNode = new VariableNode(stringToken.TokenString, variableType, stringToken);
            // 
            if (TestNextToken(tokenStack, TokenType.RightBracket) == false)
            {
                newNode.GetErrors.Add(new ParseError(stringToken, "Encapsulated text [...] is missing the ending character ]. Encapsulated text must have a starting and ending character (e.g. [ocelot])."));
            }
            else
            {
                tokenStack.Pop();
            }
            // 
            return newNode;
        }

        /// <summary>
        /// Extracts function parameters from the token stack.
        /// </summary>
        /// <param name="tokenStack">The token stack to parse.</param>
        /// <param name="errors">A reference list to collect parse error messages.</param>
        /// <param name="availableVariables">Dictionary of known variables and types.</param>
        /// <param name="stringComparison">The string comparison mode for case sensitivity.</param>
        /// <returns>A list of parsed IParserNode parameters.</returns>
        private static List<IParserNode> GetFunctionParameters(Stack<Token> tokenStack, ref List<string> errors, Dictionary<string, ResultType> availableVariables, StringComparison stringComparison)
        {
            errors = new List<string>();
            var parameters = new List<IParserNode>();
            if (TestNextToken(tokenStack, TokenType.LeftParenthesis) == false)
            {
                errors.Add("Missing a start parenthesis");
                return parameters;
            }
            // pop the left parenthesis token
            tokenStack.Pop();
            // Get Parameters assuming they are comma delimited
            while (!TestNextToken(tokenStack, TokenType.RightParenthesis))
            {
                if (tokenStack.Count == 0)
                    break;
                parameters.Add(Parse(tokenStack, null, 14, availableVariables, stringComparison));
                if (TestNextToken(tokenStack, TokenType.Comma))
                    tokenStack.Pop();
                else
                    break;
            }
            //
            if (TestNextToken(tokenStack, TokenType.RightParenthesis) == false)
            {
                errors.Add("Missing an end parenthesis");
            }
            else if (tokenStack.Count > 0)
                tokenStack.Pop(); // pop the right parenthesis to finish out the function.
            //
            return parameters;
        }

        /// <summary>
        /// Tests whether the next token on the stack matches a given type.
        /// </summary>
        /// <param name="tokenStack">The token stack to inspect.</param>
        /// <param name="tokenType">The expected token type.</param>
        /// <returns>True if the next token matches, otherwise false.</returns>
        private static bool TestNextToken(Stack<Token> tokenStack, TokenType tokenType)
        {
            if (tokenStack.Count == 0)
                return false;
            return tokenStack.Peek().Type == tokenType;
        }
        /// <summary>
        /// Determines whether a parse node's output type is numeric.
        /// Numeric types include integers, decimals, floats, shorts, and bytes.
        /// </summary>
        /// <param name="node">The parser node to test for numeric type.</param>
        /// <returns>true if the node produces a numeric output type; otherwise, false.</returns>
        public static bool IsNumericType(IParserNode node)
        {
            return (int)(node.OutputType & ResultType.Number) > 0;
        }

        /// <summary>
        /// Maps a .NET type to its corresponding parser result type.
        /// This is useful for determining the appropriate ResultType for variables based on their .NET type.
        /// </summary>
        /// <param name="t">The .NET Type to map.</param>
        /// <returns>The corresponding ResultType, or ResultType.UnDeclared if the type is not recognized.</returns>
        public static ResultType TypeToResultType(Type t)
        {
            switch (t)
            {
                case var @case when @case == typeof(string):
                    {
                        return ResultType.String;
                    }
                case var case1 when case1 == typeof(double):
                    {
                        return ResultType.Double;
                    }
                case var case2 when case2 == typeof(float):
                    {
                        return ResultType.Single;
                    }
                case var case3 when case3 == typeof(int):
                case var case4 when case4 == typeof(long):
                    {
                        return ResultType.Integer;
                    }
                case var case5 when case5 == typeof(short):
                case var case6 when case6 == typeof(ushort):
                    {
                        return ResultType.Short;
                    }
                case var case7 when case7 == typeof(byte):
                    {
                        return ResultType.Byte;
                    }
                case var case8 when case8 == typeof(bool):
                    {
                        return ResultType.Boolean;
                    }

                default:
                    {
                        return ResultType.UnDeclared;
                    }
            }
        }
    }
}
