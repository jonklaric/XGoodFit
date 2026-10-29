using System;
using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a binary logical comparison between two parser nodes, such as equality or greater-than tests.
    /// </summary>
    public class BooleanBinaryNode : IParserNode
    {

        /// <summary>
        /// The parser node representing the left operand of the comparison.
        /// </summary>
        private IParserNode _leftNode;

        /// <summary>
        /// The parser node representing the right operand of the comparison.
        /// </summary>
        private IParserNode _rightNode;

        /// <summary>
        /// The comparison operation function to apply to the left and right nodes.
        /// </summary>
        private readonly Func<IParserNode, IParserNode, bool> _operation;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets a value indicating whether this node is case-sensitive.
        /// Always <c>false</c> for boolean binary comparisons.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

        /// <summary>
        /// Gets the output type of this node.
        /// Always <see cref="ResultType.Boolean"/> for binary comparisons.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.Boolean;

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
        /// Initializes a new instance of the <see cref="BooleanBinaryNode"/> class with the specified parameters.
        /// </summary>
        /// <param name="leftNode">The left operand of the comparison.</param>
        /// <param name="rightNode">The right operand of the comparison.</param>
        /// <param name="operation">The comparison function to apply.</param>
        /// <param name="numericComparison">Indicates whether this is a numeric comparison that requires numeric operands.</param>
        /// <param name="token">The token representing the comparison operator (used for error reporting).</param>
        public BooleanBinaryNode(IParserNode leftNode, IParserNode rightNode, Func<IParserNode, IParserNode, bool> operation, bool numericComparison, Token token)
        {
            _leftNode = leftNode;
            _rightNode = rightNode;
            _operation = operation;
            // 
            if (_leftNode == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found on the left side of comparison operator (" + token.TokenString + ")."));
            }
            else
            {
                if (numericComparison && Parser.Parser.IsNumericType(_leftNode) == false)
                    _errorMessages.Add(new ParseError(token, "Non-numeric value found on left side of comparison operator (" + token.TokenString + "). Cannot perform a comparison test with non-numeric values."));
                _errorMessages.AddRange(_leftNode.GetErrors);
            }
            if (_rightNode == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found on the right side of comparison operator (" + token.TokenString + ")."));
            }
            else
            {
                if (numericComparison && Parser.Parser.IsNumericType(_rightNode) == false)
                    _errorMessages.Add(new ParseError(token, "Non-numeric value found on right side of comparison operator (" + token.TokenString + "). Cannot perform a comparison test with non-numeric values."));
                _errorMessages.AddRange(_rightNode.GetErrors);
            }
            // 
            if (_errorMessages.Count > 0)
                OutputType = ResultType.Error;
        }

        /// <summary>
        /// Simplifies the expression node. If no variables are present, the node is evaluated and 
        /// replaced with a constant Boolean node.
        /// </summary>
        /// <returns>The simplified node, either this instance or a constant Boolean node.</returns>
        public IParserNode Simplify()
        {
            if (ContainsVariable())
            {
                _leftNode = _leftNode.Simplify();
                _rightNode = _rightNode.Simplify();
                return this;
            }
            //
            return new BooleanNode(Convert.ToBoolean(Evaluate().Result));
        }

        /// <summary>
        /// Determines whether this node or its children contain any variables.
        /// </summary>
        /// <returns><c>true</c> if either operand contains a variable; otherwise <c>false</c>.</returns>
        public bool ContainsVariable()
        {
            bool hasVariable = false;
            if (!(_leftNode == null))
                hasVariable |= _leftNode.ContainsVariable();
            if (!(_rightNode == null))
                hasVariable |= _rightNode.ContainsVariable();
            // 
            return hasVariable;
        }

        /// <summary>
        /// Evaluates the binary comparison operation defined by this node.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the Boolean result or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            bool result = _operation(_leftNode, _rightNode);
            // 
            return new ParseNodeResult(result, ResultType.Boolean);
        }

        /// <summary>
        /// Collects and returns all variable nodes used within this binary comparison.
        /// </summary>
        /// <returns>A list of <see cref="VariableNode"/> instances.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            var result = new List<VariableNode>();
            if (ContainsVariable())
            {
                if (!(_leftNode == null))
                    result.AddRange(_leftNode.GetVariableNodes());
                if (!(_rightNode == null))
                    result.AddRange(_rightNode.GetVariableNodes());
            }
            return result;
        }
    }
}