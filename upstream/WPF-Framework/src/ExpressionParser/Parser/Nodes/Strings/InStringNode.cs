using System;
using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a node that performs a sub-string search within a string. 
    /// Returns the zero-based index of the search value inside the source string, or -1 if not found.
    /// </summary>
    public class InStringNode : IParserNode
    {

        /// <summary>
        /// The parser node representing the source string to search within.
        /// </summary>
        private IParserNode _nodeToSearch = null;

        /// <summary>
        /// The parser node representing the search value to look for.
        /// </summary>
        private IParserNode _searchNode = null;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// The type of string comparison to use (case-sensitive or case-insensitive).
        /// </summary>
        private readonly StringComparison _stringComparison;

        /// <summary>
        /// Gets a value indicating whether the comparison is case-sensitive.
        /// </summary>
        public bool IsCaseSensitive { get; private set; }

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
        /// Always <see cref="ResultType.Integer"/> representing the index position or -1 if not found.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.Integer;

        /// <summary>
        /// Initializes a new instance of the <see cref="InStringNode"/> class with the specified parameters.
        /// </summary>
        /// <param name="parameters">The list of nodes: the source string and search string.</param>
        /// <param name="parameterErrors">Any string-based parse errors to include.</param>
        /// <param name="token">The token that triggered this function (used for error context).</param>
        /// <param name="comparison">The type of string comparison (case sensitive or not).</param>
        public InStringNode(List<IParserNode> parameters, List<string> parameterErrors, Token token, StringComparison comparison)
        {
            if (parameters.Count >= 1)
                _nodeToSearch = parameters[0];
            if (parameters.Count >= 2)
                _searchNode = parameters[1];
            if (parameters.Count > 2)
            {
                _errorMessages.Add(new ParseError(token, "Too many parameters defined for the " + token.ToString() + " function which needs to have a source_to_search and a search_value input ( e.g. " + token.ToString() + "(source_to_search, search_value) )."));
            }
            _stringComparison = comparison;
            // 
            if (_nodeToSearch == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the source_to_search in the " + token.ToString() + "(source_to_search, search_value) function."));
            }
            else
            {
                _errorMessages.AddRange(_nodeToSearch.GetErrors);
            }
            if (_searchNode == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the search_value in the " + token.ToString() + "(source_to_search, search_value) function."));
            }
            else
            {
                _errorMessages.AddRange(_searchNode.GetErrors);
            }
            // 
            foreach (string errorString in parameterErrors)
                _errorMessages.Add(new ParseError(token, errorString + " for the " + token.ToString() + " function"));
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
                _nodeToSearch = _nodeToSearch.Simplify();
                _searchNode = _searchNode.Simplify();
                return this;
            }
            //
            return new IntegerNode(Convert.ToInt32(Evaluate().Result));
        }

        /// <summary>
        /// Determines whether this node or any of its parameters contain variables.
        /// </summary>
        /// <returns><c>true</c> if any parameter contains a variable; otherwise <c>false</c>.</returns>
        public bool ContainsVariable()
        {
            bool hasVariable = false;
            if (!(_nodeToSearch == null))
                hasVariable |= _nodeToSearch.ContainsVariable();
            if (!(_searchNode == null))
                hasVariable |= _searchNode.ContainsVariable();
            // 
            return hasVariable;
        }

        /// <summary>
        /// Evaluates the node and returns the zero-based index of the search value within the source string.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the index or -1 if not found, or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            int result = (_nodeToSearch.Evaluate().Result?.ToString() ?? "").IndexOf(_searchNode.Evaluate().Result?.ToString() ?? "", _stringComparison);
            // 
            return new ParseNodeResult(result, ResultType.Integer);
        }

        /// <summary>
        /// Gets a list of all variable nodes used in the source or search expressions.
        /// </summary>
        /// <returns>A list of <see cref="VariableNode"/> instances.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            var result = new List<VariableNode>();
            if (ContainsVariable())
            {
                if (!(_nodeToSearch == null))
                    result.AddRange(_nodeToSearch.GetVariableNodes());
                if (!(_searchNode == null))
                    result.AddRange(_searchNode.GetVariableNodes());
            }
            return result;
        }
    }
}