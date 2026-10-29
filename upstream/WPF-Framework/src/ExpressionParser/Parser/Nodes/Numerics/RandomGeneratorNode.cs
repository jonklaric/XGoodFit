using System;
using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a node that generates a random number using an optional seed value.
    /// </summary>
    public class RandomGeneratorNode : IParserNode
    {

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
        /// Initializes a new instance of the <see cref="RandomGeneratorNode"/> class with the specified parameters and errors.
        /// </summary>
        /// <param name="parameters">The parsed seed node, if present.</param>
        /// <param name="parameterErrors">List of additional parse-time errors.</param>
        /// <param name="token">The token associated with the RAND function (used for error context).</param>
        public RandomGeneratorNode(List<IParserNode> parameters, List<string> parameterErrors, Token token)
        {
            if (parameters.Count >= 1)
                _seed = parameters[0];
            if (parameters.Count > 1)
            {
                _errorMessages.Add(new ParseError(token, "Too many parameters defined for the random number generator function which can optionally have a seed value or no seed ( e.g. RAND() or RAND(seed) )."));
            }
            // 
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
            if (token.Type == TokenType.RandomInteger)
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
        /// Determines whether this node or its seed parameter contains a variable.
        /// </summary>
        /// <returns><c>true</c> if the seed parameter contains a variable; otherwise <c>false</c>.</returns>
        public bool ContainsVariable()
        {
            if (!(_seed == null))
                return _seed.ContainsVariable();
            return false;
        }

        /// <summary>
        /// Evaluates the node and generates a random number.
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
                return new ParseNodeResult(randy.NextDouble(), OutputType);
            }
            else
            {
                return new ParseNodeResult(randy.Next(), OutputType);
            }
        }

        /// <summary>
        /// Gets a list of all variable nodes used in the seed expression.
        /// </summary>
        /// <returns>A list of <see cref="VariableNode"/> instances, or an empty list if no seed variable exists.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            if (ContainsVariable())
                return _seed.GetVariableNodes();
            return new List<VariableNode>();
        }
    }
}