using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a constant integer value in the parser tree.
    /// This node always evaluates to the same <c>int</c> and contains no variables.
    /// </summary>
    public class IntegerNode : IParserNode
    {

        private readonly int _value;
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets whether the node is case-sensitive.
        /// Always <c>false</c> for integer literals.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

        /// <summary>
        /// Gets the result type produced by this node.
        /// Always <see cref="ResultType.Integer"/>.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.Integer;

        /// <summary>
        /// Gets a value indicating whether this node contains any parsing or evaluation errors.
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
        /// Initializes a new instance of the <see cref="IntegerNode"/> class with a constant integer value.
        /// </summary>
        /// <param name="value">The constant integer value represented by this node.</param>
        public IntegerNode(int value)
        {
            _value = value;
        }

        /// <summary>
        /// Simplifies the integer node.
        /// Since it is already a constant literal, this returns the current instance. 
        /// </summary>
        /// <returns>This <see cref="IntegerNode"/> instance.</returns>
        public IParserNode Simplify()
        {
            return this;
        }

        /// <summary>
        /// Indicates whether the node contains any variable references.
        /// Always returns <c>false</c> for constant integer nodes.
        /// </summary>
        /// <returns><c>false</c>.</returns>
        public bool ContainsVariable()
        {
            return false;
        }

        /// <summary>
        /// Evaluates the node and returns its constant integer value.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the integer value, or an error if present.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            return new ParseNodeResult(_value, ResultType.Integer);
        }

        /// <summary>
        /// Gets the list of variable nodes used within this expression.
        /// Always returns an empty list for constants.
        /// </summary>
        /// <returns>An empty list of <see cref="VariableNode"/>.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            return new List<VariableNode>();
        }

    }
}