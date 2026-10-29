using System;
using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a node that generates a random number between two bounds, optionally using a seed.
    /// </summary>
    public class RandomBetweenNode : IParserNode
    {

        /// <summary>
        /// The parser node representing the minimum value for the random range.
        /// </summary>
        private IParserNode _startValue = null;

        /// <summary>
        /// The parser node representing the maximum value for the random range.
        /// </summary>
        private IParserNode _endValue = null;

        /// <summary>
        /// The parser node representing an optional seed value for the random generator.
        /// </summary>
        private IParserNode _seed = null;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// Gets whether the node is case-sensitive.
        /// Always <c>false</c> for random number generation.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

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
        /// Gets the output type of this node.
        /// Either <see cref="ResultType.Double"/> or <see cref="ResultType.Integer"/> depending on the token type.
        /// </summary>
        public ResultType OutputType { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="RandomBetweenNode"/> class with parameters and error tracking.
        /// </summary>
        /// <param name="parameters">List of parsed nodes: minimum value, maximum value, and optional seed.</param>
        /// <param name="parameterErrors">List of string error messages from parsing.</param>
        /// <param name="token">The token used to identify the function (used for error reporting).</param>
        public RandomBetweenNode(List<IParserNode> parameters, List<string> parameterErrors, Token token)
        {
            if (parameters.Count >= 1)
                _startValue = parameters[0];
            if (parameters.Count >= 2)
                _endValue = parameters[1];
            if (parameters.Count >= 3)
                _seed = parameters[2];
            if (parameters.Count > 3)
            {
                _errorMessages.Add(new ParseError(token, "Too many parameters defined for the random number generator function which needs to have a minimum_value, maximum_value, and can optionally have a seed value ( e.g. RANDBETWEEN(minimum_value, maximum_value) or RANDBETWEEN(minimum_value, maximum_value,seed) )."));
            }
            // 
            if (_startValue == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the bottom value of the random number generator."));
            }
            else
            {
                if (Parser.Parser.IsNumericType(_startValue) == false)
                    _errorMessages.Add(new ParseError(token, "Bottom value of random number generator is not a numeric value, unable to perform mathematic operations on non-numeric values."));
                _errorMessages.AddRange(_startValue.GetErrors);
            }
            if (_endValue == null)
            {
                _errorMessages.Add(new ParseError(token, "Nothing found for the top value of the random number generator."));
            }
            else
            {
                if (Parser.Parser.IsNumericType(_endValue) == false)
                    _errorMessages.Add(new ParseError(token, "Top value of random number generator is not a numeric value, unable to perform mathematic operations on non-numeric values."));
                _errorMessages.AddRange(_endValue.GetErrors);
            }
            if (!(_seed == null))
            {
                if (Parser.Parser.IsNumericType(_seed) == false)
                    _errorMessages.Add(new ParseError(token, "The seed supplied to the random number generator is not a valid number."));
                _errorMessages.AddRange(_seed.GetErrors);
            }
            // 
            foreach (string errorString in parameterErrors)
                _errorMessages.Add(new ParseError(token, errorString + " for the random number function"));
            // 
            OutputType = ResultType.Double;
            if (token.Type == TokenType.RandomIntegerBetween)
                OutputType = ResultType.Integer;
            // 
            if (_errorMessages.Count > 0)
                OutputType = ResultType.Error;
        }

        /// <summary>
        /// Simplifies the node. If it contains no variables, evaluates and returns a constant numeric node.
        /// </summary>
        /// <returns>A simplified <see cref="IParserNode"/>, either this instance or a constant value node.</returns>
        public IParserNode Simplify()
        {
            if (ContainsVariable())
            {
                if (!(_startValue == null))
                    _startValue = _startValue.Simplify();
                if (!(_endValue == null))
                    _endValue = _endValue.Simplify();
                if (!(_seed == null))
                    _seed = _seed.Simplify();
                return this;
            }
            //
            if (OutputType == ResultType.Integer)
            {
                return new IntegerNode(Convert.ToInt32(Evaluate().Result));
            }
            else
            {
                return new DecimalNode(Convert.ToDouble(Evaluate().Result));
            }
        }

        /// <summary>
        /// Determines whether this node or any of its parameters contain variables.
        /// </summary>
        /// <returns><c>true</c> if any parameter contains a variable; otherwise <c>false</c>.</returns>
        public bool ContainsVariable()
        {
            bool hasVariable = false;
            if (!(_startValue == null))
                hasVariable |= _startValue.ContainsVariable();
            if (!(_endValue == null))
                hasVariable |= _endValue.ContainsVariable();
            if (!(_seed == null))
                hasVariable |= _seed.ContainsVariable();
            // 
            return hasVariable;
        }

        /// <summary>
        /// Evaluates the node and generates a random number between the specified minimum and maximum values.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the random value or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            // 
            // When no seed is supplied, use Random.Shared (a thread-safe, lock-free instance
            // introduced in .NET 6). It is preferable to `new Random()` because consecutive
            // `new Random()` calls in tight loops can produce identical seeds (clock granularity)
            // and the resulting sequence is non-thread-safe.
            Random randy = _seed == null
                ? Random.Shared
                : new Random(Convert.ToInt32(_seed.Evaluate().Result));
            //
            if (OutputType == ResultType.Double)
            {
                double min = Convert.ToDouble(_startValue.Evaluate().Result);
                double max = Convert.ToDouble(_endValue.Evaluate().Result);
                if (min > max)
                {
                    double temp = min;
                    min = max;
                    max = temp;
                }
                return new ParseNodeResult(min + (max - min) * randy.NextDouble(), OutputType);
            }
            else
            {
                int min = Convert.ToInt32(_startValue.Evaluate().Result);
                int max = Convert.ToInt32(_endValue.Evaluate().Result);
                if (min > max)
                {
                    int temp = min;
                    min = max;
                    max = temp;
                }
                // Excel's RANDBETWEEN(min, max) is inclusive on both ends; .NET's
                // Random.Next(min, max) is exclusive on max. Bump by 1 so the
                // upper bound can occur — required for Excel-formula compatibility.
                // Guard against int.MaxValue overflow.
                int upperExclusive = (max == int.MaxValue) ? int.MaxValue : max + 1;
                return new ParseNodeResult(randy.Next(min, upperExclusive), OutputType);
            }
        }

        /// <summary>
        /// Gets a list of all variable nodes used in the minimum, maximum, or seed expressions.
        /// </summary>
        /// <returns>A list of <see cref="VariableNode"/> instances.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            var result = new List<VariableNode>();
            if (ContainsVariable())
            {
                if (!(_seed == null))
                    result.AddRange(_seed.GetVariableNodes());
                if (!(_startValue == null))
                    result.AddRange(_startValue.GetVariableNodes());
                if (!(_endValue == null))
                    result.AddRange(_endValue.GetVariableNodes());
            }
            return result;
        }
    }
}