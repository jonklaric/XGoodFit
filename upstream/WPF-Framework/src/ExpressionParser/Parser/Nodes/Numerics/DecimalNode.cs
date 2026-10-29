using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a constant double-precision numeric value in the parser tree.
    /// This node always evaluates to the same <c>double</c> value and contains no variables.
    /// </summary>
    public class DecimalNode : IParserNode
    {

        private readonly double _value;
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets a value indicating whether the node is case-sensitive.
        /// Always <c>false</c> for decimal literals.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

        /// <summary>
        /// Gets the type of result returned by this node.
        /// Always <see cref="ResultType.Double"/>.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.Double;

        /// <summary>
        /// Gets a value indicating whether the node contains any errors. 
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
        /// Initializes a new instance of the <see cref="DecimalNode"/> class with the specified double value.
        /// </summary>
        /// <param name="value">The constant double value represented by this node.</param>
        public DecimalNode(double value)
        {
            _value = value;
        }

        /// <summary>
        /// Simplifies the decimal node.
        /// As it is already a literal, this returns the current instance.
        /// </summary>
        /// <returns>This <see cref="DecimalNode"/> instance.</returns>
        public IParserNode Simplify()
        {
            return this;
        }

        /// <summary>
        /// Indicates whether this node contains a variable.
        /// Always returns <c>false</c> for decimal literals.
        /// </summary>
        /// <returns><c>false</c>.</returns>
        public bool ContainsVariable()
        {
            return false;
        }

        /// <summary>
        /// Evaluates the decimal node.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the constant double value or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            return new ParseNodeResult(_value, ResultType.Double);
        }

        /// <summary>
        /// Returns a list of variable nodes used in this expression.
        /// Always returns an empty list for constant nodes.
        /// </summary>
        /// <returns>An empty list of <see cref="VariableNode"/>.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            return new List<VariableNode>();
        }
    }
}