using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a node that converts the result of another expression node into a specified type.
    /// </summary>
    public class ConverterNode : IParserNode
    {

        /// <summary>
        /// The parser node whose result will be converted to a different type.
        /// </summary>
        private IParserNode _nodeToConvert;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets whether the node is case-sensitive. Always <c>false</c> for conversion nodes.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether the node has any parsing or evaluation errors.
        /// </summary>
        public bool ContainsErrors
        {
            get
            {
                return _errorMessages.Count > 0;
            }
        }

        /// <summary>
        /// Gets the list of parse errors associated with this node.
        /// </summary>
        public List<ParseError> GetErrors => _errorMessages;

        /// <summary>
        /// Gets the result type of the converted value.
        /// </summary>
        public ResultType OutputType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConverterNode"/> class based on its parameters and source token.
        /// </summary>
        /// <param name="parameters">The list of child nodes. Only the first item is converted.</param>
        /// <param name="parameterErrors">Any error messages collected during parameter parsing.</param>
        /// <param name="token">The originating token identifying the type of conversion requested.</param>
        public ConverterNode(List<IParserNode> parameters, List<string> parameterErrors, Token token)
        {
            if (parameters.Count >= 1)
                _nodeToConvert = parameters[0];
            if (parameters.Count > 1)
            {
                _errorMessages.Add(new ParseError(token, "Only one parameter can be defined for the value converter function ( e.g. " + token.TokenString + "(value_to_convert) )."));
            }
            // 
            if (_nodeToConvert == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the value_to_convert in the convert function " + token.TokenString + "(value_to_convert)."));
            }
            else
            {
                _errorMessages.AddRange(_nodeToConvert.GetErrors);
            }
            // 
            foreach (string errorString in parameterErrors)
                _errorMessages.Add(new ParseError(token, errorString + " for the " + token.TokenString + " converter function."));
            // 
            OutputType = ResultType.UnDeclared;
            switch (token.Type)
            {
                case TokenType.ConvertToBoolean:
                    {
                        OutputType = ResultType.Boolean;
                        break;
                    }
                case TokenType.ConvertToDouble:
                    {
                        OutputType = ResultType.Double;
                        break;
                    }
                case TokenType.ConvertToInteger:
                    {
                        OutputType = ResultType.Integer;
                        break;
                    }
                case TokenType.ConvertToString:
                    {
                        OutputType = ResultType.String;
                        break;
                    }

                default:
                    {
                        _errorMessages.Add(new ParseError(token, "The convert function " + token.TokenString + "(value_to_convert) is not currently supported."));
                        break;
                    }
            }
            //
            if (_errorMessages.Count > 0)
                OutputType = ResultType.Error;
        }

        /// <summary>
        /// Simplifies the conversion node.
        /// If the node contains no variables, the conversion is evaluated and replaced with a constant value node.
        /// </summary>
        /// <returns>A simplified <see cref="IParserNode"/>, either this instance or a constant literal node.</returns>
        public IParserNode Simplify()
        {
            if (ContainsVariable())
            {
                _nodeToConvert = _nodeToConvert.Simplify();
                return this;
            }
            //
            switch (OutputType)
            {
                case ResultType.Boolean:
                    {
                        return new BooleanNode(Convert.ToBoolean(Evaluate().Result));
                    }
                case ResultType.Double:
                    {
                        return new DecimalNode(Convert.ToDouble(Evaluate().Result));
                    }
                case ResultType.Integer:
                    {
                        return new IntegerNode(Convert.ToInt32(Evaluate().Result));
                    }
                case ResultType.String:
                    {
                        return new StringNode(Convert.ToString(Evaluate().Result));
                    }

                default:
                    {
                        return this;
                    }
            }
        }

        /// <summary>
        /// Determines whether this node contains any variables.
        /// </summary>
        /// <returns><c>true</c> if the input node contains a variable; otherwise, <c>false</c>.</returns>
        public bool ContainsVariable()
        {
            if (!(_nodeToConvert == null))
                return _nodeToConvert.ContainsVariable();
            return false;
        }

        /// <summary>
        /// Evaluates the conversion by applying the desired output type to the input value.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the converted value or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            // 
            switch (OutputType)
            {
                case ResultType.Boolean:
                    {
                        bool booleanValue;
                        if (bool.TryParse(Convert.ToString(_nodeToConvert.Evaluate().Result), out booleanValue))
                        {
                            return new ParseNodeResult(booleanValue, ResultType.Boolean);
                        }
                        else
                        {
                            return new ParseNodeResult(null, ResultType.Error);
                        }
                    }
                case ResultType.Double:
                    {
                        double doubleValue;
                        if (double.TryParse(Convert.ToString(_nodeToConvert.Evaluate().Result), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out doubleValue))
                        {
                            return new ParseNodeResult(doubleValue, ResultType.Double);
                        }
                        else
                        {
                            return new ParseNodeResult(null, ResultType.Error);
                        }
                    }
                case ResultType.Integer:
                    {
                        int intValue;
                        if (int.TryParse(Convert.ToString(_nodeToConvert.Evaluate().Result), NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
                        {
                            return new ParseNodeResult(intValue, ResultType.Integer);
                        }
                        else
                        {
                            double doubleValue;
                            if (double.TryParse(Convert.ToString(_nodeToConvert.Evaluate().Result), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out doubleValue))
                            {
                                return new ParseNodeResult(Convert.ToInt32(doubleValue), ResultType.Integer);
                            }
                            else
                            {
                                return new ParseNodeResult(null, ResultType.Error);
                            }
                        }
                    }
                case ResultType.String:
                    {
                        return new ParseNodeResult(Convert.ToString(_nodeToConvert.Evaluate().Result), ResultType.String);
                    }

                default:
                    {
                        return new ParseNodeResult(null, ResultType.Error);
                    }
            }
        }

        /// <summary>
        /// Retrieves all variable nodes referenced in the node being converted.
        /// </summary>
        /// <returns>A list of <see cref="VariableNode"/> objects, or an empty list if none exist.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            if (ContainsVariable())
                return _nodeToConvert.GetVariableNodes();
            return new List<VariableNode>();
        }
    }
}