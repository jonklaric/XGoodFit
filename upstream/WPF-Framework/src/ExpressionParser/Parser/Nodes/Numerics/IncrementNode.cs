using System;
using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a stateful increment node in the parser tree that returns a number which increases with each evaluation.
    /// </summary>
    public class IncrementNode : IParserNode
    {

        /// <summary>
        /// The parser node representing the initial value for the increment operation.
        /// </summary>
        private IParserNode _initialNumber = null;

        /// <summary>
        /// The parser node representing the step value to add on each increment.
        /// </summary>
        private IParserNode _step = null;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// The current numeric value maintained by the increment node.
        /// </summary>
        private double _currentNumber;

        /// <summary>
        /// Indicates whether the initial number has been set during the first evaluation.
        /// </summary>
        private bool _initialNumberSet = false;

        /// <summary>
        /// Gets whether the node is case-sensitive.
        /// Always <c>false</c> for increment operations.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

        /// <summary>
        /// Gets whether the node or any of its parameters have errors.
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
        /// Gets the output type of this node. Either <see cref="ResultType.Integer"/> or <see cref="ResultType.Double"/>.
        /// </summary>
        public ResultType OutputType { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="IncrementNode"/> with parameters for the initial value and increment step.
        /// </summary>
        /// <param name="parameters">A list containing the initial value and optional increment step.</param>
        /// <param name="parameterErrors">Additional parameter-related errors to attach to this node.</param>
        /// <param name="token">The token representing the INCREMENT keyword (used for error messages).</param>
        public IncrementNode(List<IParserNode> parameters, List<string> parameterErrors, Token token)
        {
            if (parameters.Count >= 1)
                _initialNumber = parameters[0];
            if (parameters.Count >= 2)
                _step = parameters[1];
            if (parameters.Count > 2)
            {
                _errorMessages.Add(new ParseError(token, "Too many parameters defined for increment function which requires the number to start incrementing from and an optional increment step (e.g. INCREMENT(initial_number, increment_step))"));
            }
            // 
            if (_step == null)
                _step = new IntegerNode(1);
            // 
            if (_initialNumber == null)
            {
                _errorMessages.Add(new ParseError(token, "No initial number defined for the increment function."));
            }
            else
            {
                if (Parser.Parser.IsNumericType(_initialNumber) == false)
                    _errorMessages.Add(new ParseError(token, "initial number to be incremented is not a numeric value, unable to perform numeric operations on non-numeric values."));
                _errorMessages.AddRange(_initialNumber.GetErrors);
            }
            // 
            if (Parser.Parser.IsNumericType(_step) == false)
                _errorMessages.Add(new ParseError(token, "increment step value is not a valid number, unable to perform numeric operations on non-numeric values."));
            _errorMessages.AddRange(_step.GetErrors);
            // 
            foreach (string errorString in parameterErrors)
                _errorMessages.Add(new ParseError(token, errorString + " for the increment function"));
            //
            if (!(_initialNumber == null))
            {
                // ResultType is a flags enum — check FloatingPoint (Double | Single)
                // so float-typed start/step values widen to Double instead of falling
                // through to Integer arithmetic and silently rounding.
                bool initialIsFloat = (_initialNumber.OutputType & ResultType.FloatingPoint) > 0;
                bool stepIsFloat = (_step.OutputType & ResultType.FloatingPoint) > 0;
                OutputType = (initialIsFloat || stepIsFloat) ? ResultType.Double : ResultType.Integer;
            }
            // 
            if (_errorMessages.Count > 0)
                OutputType = ResultType.Error;
        }

        /// <summary>
        /// Simplifies the node by simplifying its child nodes (initial and step).
        /// </summary>
        /// <returns>This node with simplified parameters.</returns>
        public IParserNode Simplify()
        {
            if (!(_initialNumber == null))
                _initialNumber = _initialNumber.Simplify();
            _step = _step.Simplify();
            return this;
        }

        /// <summary>
        /// Determines whether this node contains any variable references.
        /// </summary>
        /// <returns><c>true</c> if the initial value or step expression contains a variable; otherwise <c>false</c>.</returns>
        public bool ContainsVariable()
        {
            bool hasVariable = false;
            if (!(_initialNumber == null))
                hasVariable = _initialNumber.ContainsVariable();
            if (_step.ContainsVariable() == true)
                hasVariable = true;
            return hasVariable;
        }

        /// <summary>
        /// Evaluates the current value, applying the increment logic.
        /// If first call, sets the initial value. Otherwise, adds the increment step to the running value.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> with the current incremented value or an error result.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            //
            if (_initialNumberSet == false)
            {
                _currentNumber = Convert.ToDouble(_initialNumber.Evaluate().Result);
                _initialNumberSet = true;
            }
            else
            {
                _currentNumber += Convert.ToDouble(_step.Evaluate().Result);
            }
            // 
            if (OutputType == ResultType.Integer)
            {
                return new ParseNodeResult((int)Math.Round(_currentNumber), OutputType);
            }
            else
            {
                return new ParseNodeResult(_currentNumber, OutputType);
            }
        }

        /// <summary>
        /// Returns a list of all variable nodes used in the initial or step expressions.
        /// </summary>
        /// <returns>A list of <see cref="VariableNode"/> objects used by this node.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            var result = new List<VariableNode>();
            if (ContainsVariable())
            {
                if (!(_initialNumber == null))
                    result.AddRange(_initialNumber.GetVariableNodes());
                if (!(_step == null))
                    result.AddRange(_step.GetVariableNodes());
            }
            return result;
        }
    }
}