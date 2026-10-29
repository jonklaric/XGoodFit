using System;
using System.Collections.Generic;

namespace ExpressionParser
{

    /// <summary>
    /// Represents a node that extracts a sub-string from the right side of a string value, 
    /// using the specified number of characters. 
    /// Equivalent to the RIGHT(text, length) function.
    /// </summary>
    public class RightStringNode : IParserNode
    {

        /// <summary>
        /// The parser node representing the source string.
        /// </summary>
        private IParserNode _stringNode = null;

        /// <summary>
        /// The parser node representing the number of characters to extract from the right.
        /// </summary>
        private IParserNode _nCharacters = null;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets a value indicating whether the comparison is case-sensitive.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether this node or its children contain any parsing errors.
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
        /// Gets the output type of this node.
        /// Always <see cref="ResultType.String"/> for RIGHT operations.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.String;

        /// <summary>
        /// Initializes a new instance of the <see cref="RightStringNode"/> class with parsed input and token context.
        /// </summary>
        /// <param name="parameters">List of parameters: [0] = text, [1] = number of characters.</param>
        /// <param name="parameterErrors">List of errors from parameter parsing.</param>
        /// <param name="token">The token used to create this function node.</param>
        public RightStringNode(List<IParserNode> parameters, List<string> parameterErrors, Token token)
        {
            if (parameters.Count >= 1)
                _stringNode = parameters[0];
            if (parameters.Count >= 2)
                _nCharacters = parameters[1];
            if (parameters.Count > 2)
                _errorMessages.Add(new ParseError(token, "Too many parameters defined for the Right function which needs text input and length from right side of text ( e.g. RIGHT(text, length) )."));
            // 
            if (_stringNode == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the text in the RIGHT(text, length) function."));
            }
            else
            {
                _errorMessages.AddRange(_stringNode.GetErrors);
            }
            // 
            if (_nCharacters == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the length in the RIGHT(text, length) function."));
            }
            else
            {
                if (Parser.Parser.IsNumericType(_nCharacters) == false)
                    _errorMessages.Add(new ParseError(token, "The length in the LEFT(text, length) function must be a numeric value. It is currently a " + ((int)_nCharacters.OutputType).ToString() + " type of value."));
                _errorMessages.AddRange(_nCharacters.GetErrors);
            }
            // 
            foreach (string errorString in parameterErrors)
                _errorMessages.Add(new ParseError(token, errorString + " for the RIGHT function"));
            // 
            if (_errorMessages.Count > 0)
                OutputType = ResultType.Error;
        }

        /// <summary>
        /// Simplifies the node. If it contains no variables, evaluates and returns a constant string node.
        /// </summary>
        /// <returns>A simplified <see cref="IParserNode"/>, either this instance or a <see cref="StringNode"/>.</returns>
        public IParserNode Simplify()
        {
            if (ContainsVariable())
            {
                if (!(_stringNode == null))
                    _stringNode = _stringNode.Simplify();
                if (!(_nCharacters == null))
                    _nCharacters = _nCharacters.Simplify();
                return this;
            }
            //
            return new StringNode(Convert.ToString(Evaluate().Result));
        }

        /// <summary>
        /// Determines whether this node or any of its parameters contain variables.
        /// </summary>
        /// <returns><c>true</c> if any parameter contains a variable; otherwise <c>false</c>.</returns>
        public bool ContainsVariable()
        {
            bool hasVariable = false;
            if (!(_stringNode == null))
                hasVariable |= _stringNode.ContainsVariable();
            if (!(_nCharacters == null))
                hasVariable |= _nCharacters.ContainsVariable();
            // 
            return hasVariable;
        }

        /// <summary>
        /// Evaluates the node and returns the specified number of characters from the right side of the string.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the sub-string or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            string value = _stringNode.Evaluate().Result?.ToString() ?? "";
            int length = Convert.ToInt32(_nCharacters.Evaluate().Result);
            // Handle edge cases like VB's Right function does
            if (length < 0)
                length = 0;
            if (length > value.Length)
                length = value.Length;
            string result = value.Substring(value.Length - length, length);
            //
            return new ParseNodeResult(result, ResultType.String);
        }

        /// <summary>
        /// Gets a list of all variable nodes used in the string or length expressions.
        /// </summary>
        /// <returns>A list of <see cref="VariableNode"/> instances.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            var result = new List<VariableNode>();
            if (ContainsVariable())
            {
                result.AddRange(_stringNode.GetVariableNodes());
                result.AddRange(_nCharacters.GetVariableNodes());
            }
            return result;
        }
    }
}