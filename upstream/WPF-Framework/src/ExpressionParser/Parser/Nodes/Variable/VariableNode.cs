using System.Collections.Generic;

namespace ExpressionParser
{
    /// <summary>
    /// Represents a variable in the expression tree. Holds the variable name, type, and assigned value.
    /// </summary>
    public class VariableNode : IParserNode
    {

        /// <summary>
        /// The name identifier of the variable.
        /// </summary>
        private readonly string _variableName;

        /// <summary>
        /// The declared result type of the variable.
        /// </summary>
        private readonly ResultType _variableType = ResultType.UnDeclared;

        /// <summary>
        /// The list of parse errors encountered during construction or evaluation.
        /// </summary>
        private List<ParseError> _errorMessages = new List<ParseError>();

        /// <summary>
        /// The current value assigned to the variable.
        /// </summary>
        private object _variableValue = null;

        /// <summary>
        /// The starting position in the source text where this variable was referenced.
        /// </summary>
        private readonly int _startPosition;

        /// <summary>
        /// The name, or identification, of the variable.
        /// </summary>
        /// <returns>A string value of the variable name.</returns>
        public string VariableName
        {
            get
            {
                return _variableName;
            }
        }

        /// <summary>
        /// Gets whether the node is case-sensitive.
        /// Always <c>false</c> for variables.
        /// </summary>
        public bool IsCaseSensitive { get; private set; } = false;

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
        /// Gets the result type of this variable.
        /// </summary>
        public ResultType OutputType
        {
            get
            {
                return _variableType;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableNode"/> class with the specified variable name and type.
        /// </summary>
        /// <param name="variableName">The name of the variable.</param>
        /// <param name="variableType">The declared result type of the variable.</param>
        /// <param name="token">The token that references the variable (used for error context).</param>
        public VariableNode(string variableName, ResultType variableType, Token token)
        {
            _variableName = variableName;
            _startPosition = token.StartPosition;
            if (_variableName == null)
            {
                _errorMessages.Add(new ParseError(token, "Variable name can't be empty ( e.g. [variable_name] )."));
            }
            //
            switch (variableType)
                {
                    case ResultType.Boolean:
                        {
                            _variableType = ResultType.Boolean;
                            break;
                        }
                    case ResultType.Byte:
                    case ResultType.Integer:
                    case ResultType.Short:
                        {
                            _variableType = ResultType.Integer;
                            break;
                        }
                    case ResultType.FloatingPoint:
                    case ResultType.Double:
                    case ResultType.Single:
                        {
                            _variableType = ResultType.Double;
                            break;
                        }
                    case ResultType.String:
                        {
                            _variableType = ResultType.String;
                            break;
                        }

                    default:
                        {
                            _errorMessages.Add(new ParseError(token, "The variable type '" + variableType.ToString() + "' is not supported. For variables only numbers, text, and true/false types are allowed."));
                            break;
                        }
                }
        }

        /// <summary>
        /// Sets the value of the variable.
        /// </summary>
        /// <param name="value">The value to assign.</param>
        public void SetValue(object value)
        {
            _variableValue = value;
        }

        /// <summary>
        /// Simplifies the node. Since variables cannot be simplified further, returns itself.
        /// </summary>
        /// <returns>This node instance.</returns>
        public IParserNode Simplify()
        {
            return this;
        }

        /// <summary>
        /// Determines whether this node contains any variables.
        /// Always returns <c>true</c> for variable nodes.
        /// </summary>
        /// <returns><c>true</c>, as this is a variable node.</returns>
        public bool ContainsVariable()
        {
            return true;
        }

        /// <summary>
        /// Evaluates the node and returns the currently assigned variable value.
        /// </summary>
        /// <returns>A <see cref="ParseNodeResult"/> containing the variable's value or an error.</returns>
        public ParseNodeResult Evaluate()
        {
            if (ContainsErrors)
                return new ParseNodeResult(null, ResultType.Error);
            if (_variableValue == null)
                return new ParseNodeResult(null, ResultType.Error);
            return new ParseNodeResult(_variableValue, OutputType);
        }

        /// <summary>
        /// Gets a list of all variable nodes. Returns a list containing only this variable node.
        /// </summary>
        /// <returns>A list containing this <see cref="VariableNode"/> instance.</returns>
        public List<VariableNode> GetVariableNodes()
        {
            return new List<VariableNode>(new[] { this });
        }
    }
}