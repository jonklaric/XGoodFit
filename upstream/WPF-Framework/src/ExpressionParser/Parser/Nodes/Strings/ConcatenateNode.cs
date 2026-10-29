using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a node that concatenates multiple sub-nodes into a single string.
    /// This supports functions like CONCAT().
    /// </summary>
    public class ConcatenateNode : IParserNode
    {

        /// <summary>
        /// The collection of parser nodes whose results will be concatenated together.
        /// </summary>
        private IEnumerable<IParserNode> _nodesToConcatenate;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets whether the node is case-sensitive.
        /// Always <c>false</c> for concatenation operations.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

        /// <summary>
        /// Gets the output type of this node.
        /// Always <see cref="ResultType.String"/> for concatenation.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.String;

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
        /// Initializes a new instance of the <see cref="ConcatenateNode"/> class with a collection of nodes to concatenate and any parameter errors.
        /// </summary>
        /// <param name="nodesConcatenate">The nodes to concatenate into a string.</param>
        /// <param name="parameterErrors">Any errors found while parsing the parameters.</param>
        /// <param name="token">The token associated with the CONCAT function (used for error context).</param>
        public ConcatenateNode(IEnumerable<IParserNode> nodesConcatenate, List<string> parameterErrors, Token token)
        {
            _nodesToConcatenate = nodesConcatenate;
            // 
            if (_nodesToConcatenate == null)
            {
                _errorMessages.Add(new ParseError(token, token.TokenString + " operator does not contain anything to concatenate. The " + token.TokenString + " must have at least one value to work."));
                _nodesToConcatenate = new IParserNode[] { }; // set as empty array to reduce null exception potential.
            }
            else
            {
                for (int i = 0; i < _nodesToConcatenate.Count(); i++)
                {
                    if (_nodesToConcatenate.ElementAtOrDefault(i) == null)
                    {
                        _errorMessages.Add(new ParseError(token, token.TokenString + " contains an empty value (entry number " + (i + 1) + ") each entry must have a value."));
                        continue;
                    }
                }
            }
            // 
            foreach (string errorString in parameterErrors)
                _errorMessages.Add(new ParseError(token, errorString + " for the IF function"));
        }

        /// <summary>
        /// Simplifies the node. If it contains no variables, evaluates and returns a constant string node.
        /// </summary>
        /// <returns>A simplified <see cref="IParserNode"/>, either this instance or a <see cref="StringNode"/>.</returns>
        public IParserNode Simplify()
        {
            if (ContainsVariable())
            {
                // Reassign the simplified children. The previous implementation discarded
                // Simplify()'s return value, so CONCATENATE never folded constant sub-expressions
                // when it had variable children.
                _nodesToConcatenate = _nodesToConcatenate
                    .Select(n => n == null ? n : n.Simplify())
                    .ToList();
                return this;
            }
            //
            return new StringNode(Evaluate().Result?.ToString() ?? "");
        }

        /// <summary>
        /// Determines whether this node or any of its child nodes contain variables.
        /// </summary>
        /// <returns><c>true</c> if any child node contains a variable; otherwise <c>false</c>.</returns>
        public bool ContainsVariable()
        {
            bool hasVariable = false;
            foreach (var testNode in _nodesToConcatenate)
            {
                if (!(testNode == null))
                    hasVariable |= testNode.ContainsVariable();
            }
            // 
            return hasVariable;
        }

        /// <summary>
        /// Evaluates the node and concatenates all child node results into a single string.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the concatenated string or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            // 
            var result = new StringBuilder();
            foreach (var testNode in _nodesToConcatenate)
                result.Append(testNode.Evaluate().Result?.ToString() ?? "");
            // 
            return new ParseNodeResult(result.ToString(), ResultType.String);
        }

        /// <summary>
        /// Gets a list of all variable nodes used in any of the child nodes.
        /// </summary>
        /// <returns>A list of <see cref="VariableNode"/> instances.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            var result = new List<VariableNode>();
            if (ContainsVariable())
            {
                foreach (var testNode in _nodesToConcatenate)
                {
                    if (!(testNode == null))
                        result.AddRange(testNode.GetVariableNodes());
                }
            }
            return result;
        }
    }
}