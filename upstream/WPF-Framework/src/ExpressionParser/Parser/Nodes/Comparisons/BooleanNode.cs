using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a constant Boolean value in the parser tree.
    /// This node always evaluates to a specific true/false value and contains no variables.
    /// </summary>
    public class BooleanNode : IParserNode
    {

        /// <summary>
        /// The boolean literal value stored by this node.
        /// </summary>
        private readonly bool _value;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets a value indicating whether the node is case-sensitive.
        /// Always <c>false</c> for boolean literals.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

        /// <summary>
        /// Gets the output type of this node.
        /// Always <see cref="ResultType.Boolean"/> for boolean literals.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.Boolean;

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
        /// Initializes a new instance of <see cref="BooleanNode"/> class with the specified Boolean value.
        /// </summary>
        /// <param name="value">The Boolean value this node represents.</param>
        public BooleanNode(bool value)
        {
            _value = value;
        }

        /// <summary>
        /// Simplifies the Boolean node.
        /// Since it's already a literal, it returns itself.
        /// </summary>
        /// <returns>This node instance.</returns>
        public IParserNode Simplify()
        {
            return this;
        }

        /// <summary>
        /// Determines whether this node contains any variables. 
        /// Always returns <c>false</c> for Boolean literals.
        /// </summary>
        /// <returns><c>false</c></returns>
        public bool ContainsVariable()
        {
            return false;
        }

        /// <summary>
        /// Evaluates the node and returns its stored Boolean value.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the Boolean value or an error if applicable</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            return new ParseNodeResult(_value, ResultType.Boolean);
        }

        /// <summary>
        /// Gets a list of all variable nodes used by this node.
        /// Always returns an empty list for Boolean literals.
        /// </summary>
        /// <returns>An empty list of <see cref="VariableNode"/>.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            return new List<VariableNode>();
        }
    }
}