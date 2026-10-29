using System;
using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a node that concatenates two values using the ampersand (&amp;) operator.
    /// </summary>
    public class AmpersandNode : IParserNode
    {

        /// <summary>
        /// The parser node representing the left operand of the ampersand operator.
        /// </summary>
        private IParserNode _leftNode;

        /// <summary>
        /// The parser node representing the right operand of the ampersand operator.
        /// </summary>
        private IParserNode _rightNode;

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
        /// Always <see cref="ResultType.String"/> for ampersand concatenation.
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
        /// Initializes a new instance of the <see cref="AmpersandNode"/> class using two operand nodes and a token.
        /// </summary>
        /// <param name="leftNode">The node on the left-hand side of the ampersand operator.</param>
        /// <param name="rightNode">The node on the right-hand side of the ampersand operator.</param>
        /// <param name="token">The token associated with the ampersand operation (used for error messages).</param>
        public AmpersandNode(IParserNode leftNode, IParserNode rightNode, Token token)
        {
            _leftNode = leftNode;
            _rightNode = rightNode;
            // 
            if (_leftNode == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found on the left side of ampersand operator (" + token.TokenString + ")."));
            }
            else
            {
                _errorMessages.AddRange(_leftNode.GetErrors);
            }
            if (_rightNode == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found on the right side of ampersand operator (" + token.TokenString + ")."));
            }
            else
            {
                _errorMessages.AddRange(_rightNode.GetErrors);
            }
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
                if (!(_leftNode == null))
                    _leftNode = _leftNode.Simplify();
                if (!(_rightNode == null))
                    _rightNode = _rightNode.Simplify();
                return this;
            }
            //
            return new StringNode(Convert.ToString(Evaluate().Result));
        }

        /// <summary>
        /// Determines whether this node or any of its operands contain variables.
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
        /// Evaluates the node and concatenates the left and right operands into a single string.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the concatenated string or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            string result = (_leftNode.Evaluate().Result?.ToString() ?? "") + (_rightNode.Evaluate().Result?.ToString() ?? "");
            // 
            return new ParseNodeResult(result, ResultType.String);
        }

        /// <summary>
        /// Gets a list of all variable nodes used in the left or right operands.
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