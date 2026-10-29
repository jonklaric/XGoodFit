using System;
using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a binary numeric operation node in a parsed expression tree.
    /// Performs a numeric operation (e.g., addition, subtraction, multiplication, division, exponentiation) on two operands.
    /// </summary>
    public class NumericBinaryNode : IParserNode
    {

        private IParserNode _leftNode;
        private IParserNode _rightNode;
        private readonly Func<double, double, double> _operation;
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets a value indicating whether the node is case-sensitive.
        /// Always <c>false</c> for numeric operations.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

        /// <summary>
        /// Gets the result type produced by this node.
        /// The type is Double if either operand is Double, or if the operation is division or exponentiation; otherwise Integer.
        /// </summary>
        public ResultType OutputType { get; private set; }

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
        /// Initializes a new instance of the <see cref="NumericBinaryNode"/> class with specified left and right nodes, operation, and token.
        /// </summary>
        /// <param name="leftNode">The left operand node.</param>
        /// <param name="rightNode">The right operand node.</param>
        /// <param name="operation">The numeric operation to apply (takes two doubles and returns a double).</param>
        /// <param name="token">The token that triggered this binary operation (used for error context).</param>
        public NumericBinaryNode(IParserNode leftNode, IParserNode rightNode, Func<double, double, double> operation, Token token)
        {
            _leftNode = leftNode;
            _rightNode = rightNode;
            _operation = operation;
            // 
            if (_leftNode == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found on the left side of mathematic operator (" + token.TokenString + ")."));
            }
            else
            {
                if (Parser.Parser.IsNumericType(_leftNode) == false)
                    _errorMessages.Add(new ParseError(token, "left hand side of operator (" + token.TokenString + ") is not a numeric value, unable to perform mathematic operations on non-numeric values."));
                _errorMessages.AddRange(_leftNode.GetErrors);
            }
            if (_rightNode == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found on the right side of mathematic operator (" + token.TokenString + ")."));
            }
            else
            {
                if (Parser.Parser.IsNumericType(_rightNode) == false)
                    _errorMessages.Add(new ParseError(token, "right hand side of operator (" + token.TokenString + ") is not a numeric value, unable to perform mathematic operations on non-numeric values."));
                _errorMessages.AddRange(_rightNode.GetErrors);
            }
            if (!(_leftNode == null) && !(_rightNode == null))
            {
                // ResultType is a flags enum — Single, Short, Byte are distinct values
                // from Double. Check FloatingPoint (Double | Single) so float-typed
                // variables widen to Double instead of falling through to Integer
                // arithmetic and silently rounding.
                bool leftIsFloat = (_leftNode.OutputType & ResultType.FloatingPoint) > 0;
                bool rightIsFloat = (_rightNode.OutputType & ResultType.FloatingPoint) > 0;
                OutputType = (leftIsFloat || rightIsFloat) ? ResultType.Double : ResultType.Integer;
            }
            if (token.Type == TokenType.Division || token.Type == TokenType.Exponent)
                OutputType = ResultType.Double;
            // 
            if (_errorMessages.Count > 0)
                OutputType = ResultType.Error;
        }

        /// <summary>
        /// Simplifies the numeric binary node by simplifying its operands and evaluating constant expressions.
        /// If the node contains no variables, it is evaluated and replaced with a constant node.
        /// </summary>
        /// <returns>A simplified node (either this node with simplified children, or a constant DecimalNode or IntegerNode).</returns>
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
            if (OutputType == ResultType.Double)
                return new DecimalNode(Convert.ToDouble(Evaluate().Result));
            try
            {
                return new IntegerNode(Convert.ToInt32(Evaluate().Result));
            }
            catch (OverflowException)
            {
                return this;
            }
        }

        /// <summary>
        /// Determines whether this node or any of its child nodes contain variable references.
        /// </summary>
        /// <returns>true if the node or its children contain variables; otherwise, false.</returns>
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
        /// Evaluates the numeric binary operation by evaluating both operands and applying the operation function.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the computed result, or an error if the node contains errors.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            var leftResult = _leftNode.Evaluate();
            var rightResult = _rightNode.Evaluate();
            if (leftResult.Result == null || leftResult.Type == ResultType.Error)
                return new ParseNodeResult(null, ResultType.Error);
            if (rightResult.Result == null || rightResult.Type == ResultType.Error)
                return new ParseNodeResult(null, ResultType.Error);
            double result = _operation(Convert.ToDouble(leftResult.Result), Convert.ToDouble(rightResult.Result));
            if (OutputType == ResultType.Integer)
            {
                return new ParseNodeResult((int)Math.Round(result), OutputType);
            }
            else
            {
                return new ParseNodeResult(result, OutputType);
            }
        }

        /// <summary>
        /// Gets all variable nodes contained within this node's operands.
        /// </summary>
        /// <returns>A list of all <see cref="VariableNode"/> instances found in the left and right operands.</returns>
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