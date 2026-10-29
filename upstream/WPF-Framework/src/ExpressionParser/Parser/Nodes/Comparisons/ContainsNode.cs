using System;
using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a node in the parser tree that checks whether one string contains another.
    /// Implements the CONTAINS(source, value) functionality.
    /// </summary>
    public class ContainsNode : IParserNode
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
        /// Always <see cref="ResultType.Boolean"/> for contains operations.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.Boolean;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainsNode"/> class with the specified parameters.
        /// </summary>
        /// <param name="parameters">The list of parser nodes: the source string and the search value.</param>
        /// <param name="parameterErrors">Additional parameter-level error messages.</param>
        /// <param name="token">The token representing the CONTAINS keyword (used for error reporting).</param>
        public ContainsNode(List<IParserNode> parameters, List<string> parameterErrors, Token token)
        {
            if (parameters.Count >= 1)
                _nodeToSearch = parameters[0];
            if (parameters.Count >= 2)
                _searchNode = parameters[1];
            if (parameters.Count > 2)
            {
                _errorMessages.Add(new ParseError(token, "Too many parameters defined for the Contains function which needs to have a source_to_search and a search_value input ( e.g. CONTAINS(source_to_search, search_value) )."));
            }
            // 
            if (_nodeToSearch == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the source_to_search in the CONTAINS(source_to_search, search_value) function."));
            }
            else
            {
                _errorMessages.AddRange(_nodeToSearch.GetErrors);
            }
            if (_searchNode == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the search_value in the CONTAINS(source_to_search, search_value) function."));
            }
            else
            {
                _errorMessages.AddRange(_searchNode.GetErrors);
            }
            // 
            foreach (string errorString in parameterErrors)
                _errorMessages.Add(new ParseError(token, errorString + " for the CONTAINS function"));
            // 
            if (_errorMessages.Count > 0)
                OutputType = ResultType.Error;
        }

        /// <summary>
        /// Simplifies the expression. If it contains no variables, evaluates it to a constant <see cref="BooleanNode"/>
        /// </summary>
        /// <returns>The simplified <see cref="IParserNode"/>.</returns>
        public IParserNode Simplify()
        {
            if (ContainsVariable())
            {
                _nodeToSearch = _nodeToSearch.Simplify();
                _searchNode = _searchNode.Simplify();
                return this;
            }
            //
            return new BooleanNode(Convert.ToBoolean(Evaluate().Result));
        }

        /// <summary>
        /// Determines whether this node or any of its children contain variables.
        /// </summary>
        /// <returns><c>true</c> if any variable nodes are present; otherwise, <c>false</c>.</returns>
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
        /// Evaluates whether the source string contains the target string.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> indicating whether the search string was found.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            bool result = (_nodeToSearch.Evaluate().Result?.ToString() ?? "").Contains(_searchNode.Evaluate().Result?.ToString() ?? "");
            // 
            return new ParseNodeResult(result, ResultType.Boolean);
        }

        /// <summary>
        /// Gets a list of all variable nodes used in the source and search expressions.
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