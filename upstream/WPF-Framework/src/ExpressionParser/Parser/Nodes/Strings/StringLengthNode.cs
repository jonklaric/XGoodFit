using System;
using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a node that calculates the length of a string input.
    /// Equivalent to LENGTH(text).
    /// </summary>
    public class StringLengthNode : IParserNode
    {

        /// <summary>
        /// The parser node representing the string whose length will be calculated.
        /// </summary>
        private IParserNode _stringNode;

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
        /// Always <see cref="ResultType.Integer"/> representing the string length.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.Integer;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringLengthNode"/> class with the specified parameters.
        /// </summary>
        /// <param name="parameters">List of parameters: [0] = text to measure the length of.</param>
        /// <param name="parameterErrors">List of parameter parsing error messages.</param>
        /// <param name="token">The token associated with the LENGTH function (used for error messages).</param>
        public StringLengthNode(List<IParserNode> parameters, List<string> parameterErrors, Token token)
        {
            if (parameters.Count >= 1)
                _stringNode = parameters[0];
            if (parameters.Count > 1)
            {
                _errorMessages.Add(new ParseError(token, "Too many parameters defined for the Length function which needs only one text input ( e.g. LENGTH(text) )."));
            }
            // 
            if (_stringNode == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the text in the LENGTH(text) function."));
            }
            else
            {
                _errorMessages.AddRange(_stringNode.GetErrors);
            }
            // 
            foreach (string errorString in parameterErrors)
                _errorMessages.Add(new ParseError(token, errorString + " for the LENGTH function"));
            // 
            if (_errorMessages.Count > 0)
                OutputType = ResultType.Error;
        }

        /// <summary>
        /// Simplifies the node. If it contains no variables, evaluates and returns a constant integer node.
        /// </summary>
        /// <returns>A simplified <see cref="IParserNode"/>, either this instance or an <see cref="IntegerNode"/>.</returns>
        public IParserNode Simplify()
        {
            if (ContainsVariable())
            {
                _stringNode = _stringNode.Simplify();
                return this;
            }
            //
            return new IntegerNode(Convert.ToInt32(Evaluate().Result));
        }

        /// <summary>
        /// Determines whether this node or its string parameter contains a variable.
        /// </summary>
        /// <returns><c>true</c> if the string parameter contains a variable; otherwise <c>false</c>.</returns>
        public bool ContainsVariable()
        {
            if (!(_stringNode == null))
                return _stringNode.ContainsVariable();
            else
                return false;
        }

        /// <summary>
        /// Evaluates the node and returns the length of the string.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the string length or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            int result = (_stringNode.Evaluate().Result?.ToString() ?? "").Length;
            // 
            return new ParseNodeResult(result, ResultType.Integer);
        }

        /// <summary>
        /// Gets a list of all variable nodes used in the string expression.
        /// </summary>
        /// <returns>A list of <see cref="VariableNode"/> instances, or an empty list if no variables exist.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            if (ContainsVariable())
                return _stringNode.GetVariableNodes();
            return new List<VariableNode>();
        }
    }
}