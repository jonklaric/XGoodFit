using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a node that contains a string literal value in the parse tree.
    /// Always evaluates to a fixed string value and does not contain any variables.
    /// </summary>
    public class StringNode : IParserNode
    {

        /// <summary>
        /// The string literal value stored by this node.
        /// </summary>
        private readonly string _value;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets whether the node is case-sensitive.
        /// Always <c>false</c> for string literals.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

        /// <summary>
        /// Gets the output type of this node.
        /// Always <see cref="ResultType.String"/> for string literals.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.String;

        /// <summary>
        /// Gets a value indicating whether this node contains any parsing errors.
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
        /// Initializes a new instance of the <see cref="StringNode"/> class with a specific string value.
        /// </summary>
        /// <param name="value">The string literal represented by this node.</param>
        public StringNode(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Simplifies the node. Since it's already a literal, it returns itself.
        /// </summary>
        /// <returns>This node instance.</returns>
        public IParserNode Simplify()
        {
            return this;
        }

        /// <summary>
        /// Determines whether this node contains any variables.
        /// Always returns <c>false</c> for string literals.
        /// </summary>
        /// <returns><c>false</c>, as string literals contain no variables.</returns>
        public bool ContainsVariable()
        {
            return false;
        }

        /// <summary>
        /// Evaluates the node and returns its stored string value.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the string value or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            return new ParseNodeResult(_value, ResultType.String);
        }

        /// <summary>
        /// Gets a list of all variable nodes used by this node.
        /// Always returns an empty list for string literals.
        /// </summary>
        /// <returns>An empty list of <see cref="VariableNode"/>.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            return new List<VariableNode>();
        }
    }
}