using System;
using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a node that performs a sub-string operation on a string value,
    /// extracting a portion of the string starting from a given index and length.
    /// Equivalent to SUBSTRING(source_value, start_index, length).
    /// </summary>
    public class SubStringNode : IParserNode
    {

        /// <summary>
        /// The parser node representing the source string.
        /// </summary>
        private IParserNode _stringValue = null;

        /// <summary>
        /// The parser node representing the starting index for the sub-string operation.
        /// </summary>
        private IParserNode _startIndex = null;

        /// <summary>
        /// The parser node representing the length of the substring to extract.
        /// </summary>
        private IParserNode _length = null;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets whether the node is case-sensitive.
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
        /// Always <see cref="ResultType.String"/> for sub-string operations.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.String;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubStringNode"/> class with the specified parameters.
        /// </summary>
        /// <param name="parameters">List of parser nodes: [0] = string, [1] = start index, [2] length.</param>
        /// <param name="parameterErrors">List of parameter-related parsing errors.</param>
        /// <param name="token">The token associated with the SUBSTRING function (used for error reporting).</param>
        public SubStringNode(List<IParserNode> parameters, List<string> parameterErrors, Token token)
        {
            if (parameters.Count >= 1)
                _stringValue = parameters[0];
            if (parameters.Count >= 2)
                _startIndex = parameters[1];
            if (parameters.Count >= 3)
                _length = parameters[2];
            if (parameters.Count > 3)
            {
                _errorMessages.Add(new ParseError(token, "Too many parameters defined for the Substring function which needs to have a source_value, start_index, and length value ( e.g. SUBSTRING(source_value, start_index, length) )."));
            }
            // 
            if (_stringValue == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the source value of the Substring function."));
            }
            else
            {
                _errorMessages.AddRange(_stringValue.GetErrors);
            }
            if (_startIndex == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the start index of the Substring function."));
            }
            else
            {
                if (Parser.Parser.IsNumericType(_startIndex) == false)
                    _errorMessages.Add(new ParseError(token, "Start_index value of Substring function is not a numeric value, unable to perform function without a start index."));
                _errorMessages.AddRange(_startIndex.GetErrors);
            }
            if (_length == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the length of the Substring function."));
            }
            else
            {
                if (Parser.Parser.IsNumericType(_length) == false)
                    _errorMessages.Add(new ParseError(token, "Length value of Substring function is not a numeric value, unable to perform function without a length."));
                _errorMessages.AddRange(_length.GetErrors);
            }
            //
            foreach (string errorString in parameterErrors)
                _errorMessages.Add(new ParseError(token, errorString + " for the Substring function"));
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
                if (!(_stringValue == null))
                    _stringValue = _stringValue.Simplify();
                if (!(_startIndex == null))
                    _startIndex = _startIndex.Simplify();
                if (!(_length == null))
                    _length = _length.Simplify();
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
            if (!(_stringValue == null))
                hasVariable |= _stringValue.ContainsVariable();
            if (!(_startIndex == null))
                hasVariable |= _startIndex.ContainsVariable();
            if (!(_length == null))
                hasVariable |= _length.ContainsVariable();
            // 
            return hasVariable;
        }

        /// <summary>
        /// Evaluates the node and extracts a substring from the source string.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the extracted substring or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            //
            int startIndex = Convert.ToInt32(_startIndex.Evaluate().Result);
            int length = Convert.ToInt32(_length.Evaluate().Result);
            string value = _stringValue.Evaluate().Result?.ToString() ?? "";
            // Handle negative indices by treating them as 0
            if (startIndex < 0)
                startIndex = 0;
            if (length < 0)
                length = 0;
            if (startIndex > value.Length)
                startIndex = value.Length;
            if (startIndex + length > value.Length)
                length = value.Length - startIndex;
            string result = value.Substring(startIndex, length);
            //
            return new ParseNodeResult(result, ResultType.String);
        }

        /// <summary>
        /// Gets a list of all variable nodes used in the string value, start index, or length expressions.
        /// </summary>
        /// <returns>A list of <see cref="VariableNode"/> instances.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            var result = new List<VariableNode>();
            if (ContainsVariable())
            {
                if (!(_length == null))
                    result.AddRange(_length.GetVariableNodes());
                if (!(_stringValue == null))
                    result.AddRange(_stringValue.GetVariableNodes());
                if (!(_startIndex == null))
                    result.AddRange(_startIndex.GetVariableNodes());
            }
            return result;
        }
    }
}