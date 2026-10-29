namespace ExpressionParser
{
    /// <summary>
    /// Represents an error encountered during parsing or evaluation of an expression.
    /// Associates a descriptive message with the token that caused the error.
    /// </summary>
    public class ParseError
    {
        /// <summary>
        /// Gets the token associated with the error.
        /// </summary>
        public Token TokenWithError { get; private set; }

        /// <summary>
        /// Gets the error message describing the issue.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseError"/> class.
        /// </summary>
        /// <param name="t">The token where the error occurred.</param>
        /// <param name="errorDescription">The description of the error.</param>
        public ParseError(Token t, string errorDescription)
        {
            TokenWithError = t;
            Description = errorDescription;
        }
    }
}