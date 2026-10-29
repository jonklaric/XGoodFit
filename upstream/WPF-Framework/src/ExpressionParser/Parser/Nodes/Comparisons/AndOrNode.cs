using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a logical AND or OR operation node in a parser tree.
    /// This node evaluates to a Boolean result based on multiple child nodes.
    /// </summary>
    public class AndOrNode : IParserNode
    {

        /// <summary>
        /// Indicates whether this is an AND operation (true) or an OR operation (false).
        /// </summary>
        private readonly bool _isAnd;

        /// <summary>
        /// The collection of parser nodes to test for the logical operation.
        /// </summary>
        private List<IParserNode> _nodesToTest;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets a value indicating the node is case-sensitive.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

        /// <summary>
        /// Gets the type of value produced by this node, always <see cref="ResultType.Boolean"/>.
        /// </summary>
        public ResultType OutputType { get; private set; } = ResultType.Boolean;

        /// <summary>
        /// Gets a value indicating whether this node or any of its children contain parsing errors.
        /// </summary>
        public bool ContainsErrors
        {
            get
            {
                return _errorMessages.Count > 0;
            }
        }

        /// <summary>
        /// Gets a list of parsing errors associated with this node.
        /// </summary>
        public List<ParseError> GetErrors => _errorMessages;

        /// <summary>
        /// Initializes a new instance of <see cref="AndOrNode"/> class with no inputs.
        /// </summary>
        public AndOrNode(IEnumerable<IParserNode> nodesToTest, List<string> parameterErrors, Token token)
        {
            _nodesToTest = nodesToTest?.ToList();
            //
            if (_nodesToTest == null)
            {
                _errorMessages.Add(new ParseError(token, "Operator does not contain anything to test. The " + token.TokenString + " must have at least one logical test to work."));
                _nodesToTest = new List<IParserNode>(); // set as empty list to reduce null exception potential.
            }
            else
            {
                for (int i = 0; i < _nodesToTest.Count; i++)
                {
                    var node = _nodesToTest[i];
                    if (node == null)
                    {
                        _errorMessages.Add(new ParseError(token, "Contains an empty value (entry number " + (i + 1) + ") each entry must be a logical true/false statement."));
                        continue;
                    }
                    // Propagate errors from child nodes
                    _errorMessages.AddRange(node.GetErrors);
                    //
                    if (node.OutputType == ResultType.Boolean)
                        continue;
                    _errorMessages.Add(new ParseError(token, "Contains a value (entry number " + (i + 1) + ") that is not a logical true/false statement. Current return type at entry number " + (i + 1) + " is " + ((int)node.OutputType).ToString()));
                }
            }
            //
            _isAnd = token.Type == TokenType.And;
            //
            foreach (string errorString in parameterErrors)
                _errorMessages.Add(new ParseError(token, errorString + " for the " + token.Type.ToString() + " function"));
            //
            if (_errorMessages.Count > 0)
                OutputType = ResultType.Error;
        }

        /// <summary>
        /// Simplifies the node. If it contains no variables, evaluates to a constant Boolean result.
        /// </summary>
        /// <returns>A simplified <see cref="IParserNode"/> that is either this node or a <see cref="BooleanNode"/></returns>
        public IParserNode Simplify()
        {
            if (ContainsVariable())
            {
                _nodesToTest = _nodesToTest.Select(testNode =>
                {
                    if (testNode == null)
                        return testNode;
                    return testNode.Simplify();
                }).ToList();
                return this;
            }
            // 
            return new BooleanNode(Convert.ToBoolean(Evaluate().Result));
        }

        /// <summary>
        /// Determines whether any child node contains a variable reference.
        /// </summary>
        /// <returns><c>true</c> if any node contains a variable; otherwise <c>false</c>.</returns>
        public bool ContainsVariable()
        {
            bool hasVariable = false;
            foreach (var testNode in _nodesToTest)
            {
                if (!(testNode == null))
                    hasVariable |= testNode.ContainsVariable();
            }
            // 
            return hasVariable;
        }

        /// <summary>
        /// Evaluates the logical AND or OR operation based on the child nodes.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing a Boolean result or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            // 
            bool result;
            if (_isAnd)
            {
                result = _nodesToTest.All(o => Convert.ToBoolean(o.Evaluate().Result) == true);
            }
            else
            {
                result = _nodesToTest.Any(o => Convert.ToBoolean(o.Evaluate().Result) == true);
            }
            // 
            return new ParseNodeResult(result, ResultType.Boolean);
        }

        /// <summary>
        /// Returns all variable nodes found in the subtree of this node.
        /// </summary>
        /// <returns>A list of <see cref="VariableNode"/> instances used within this node.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            var result = new List<VariableNode>();
            if (ContainsVariable())
            {
                foreach (var testNode in _nodesToTest)
                {
                    if (!(testNode == null))
                        result.AddRange(testNode.GetVariableNodes());
                }
            }
            return result;
        }
    }
}