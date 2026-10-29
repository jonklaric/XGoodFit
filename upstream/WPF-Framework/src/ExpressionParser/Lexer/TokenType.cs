namespace ExpressionParser
{
    /// <summary>
    /// Specifies  the specific type of a token parsed from an expression string.
    /// This is used by the parser to identify the syntax and semantics of each token.
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// Represents a whitespace character.
        /// </summary>
        Space,

        /// <summary>
        /// Represents a left parenthesis '('.
        /// </summary>
        LeftParenthesis,

        /// <summary>
        /// Represents a right parenthesis ')'.
        /// </summary>
        RightParenthesis,

        /// <summary>
        /// Represents a left square bracket '['.
        /// </summary>
        LeftBracket,

        /// <summary>
        /// Represents a right square bracket ']'.
        /// </summary>
        RightBracket,

        /// <summary>
        /// Represents a left curly bracket '{'.
        /// </summary>
        LeftCurlyBracket,

        /// <summary>
        /// Represents a right curly bracket '}'.
        /// </summary>
        RightCurlyBracket,

        /// <summary>
        /// Represents the addition operator '+'.
        /// </summary>
        Addition,

        /// <summary>
        /// Represents the subtraction operator '-'.
        /// </summary>
        Subtraction,

        /// <summary>
        /// Represents the multiplication operator '*'.
        /// </summary>
        Multiplication,

        /// <summary>
        /// Represents the division operator '/'.
        /// </summary>
        Division,

        /// <summary>
        /// Represents the exponentiation operator '^'.
        /// </summary>
        Exponent,

        /// <summary>
        /// Represents the equals operator '='.
        /// </summary>
        Equals,

        /// <summary>
        /// Represents the not equal operator ('!=').
        /// </summary>
        NotEqual,

        /// <summary>
        /// Represents the less than operator.
        /// </summary>
        LessThan,

        /// <summary>
        /// Represents the greater than operator '>'.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Represents the less than or equal operator.
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// Represents the greater than or equal operator '>='.
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Represents an identifier (e.g., a variable name).
        /// </summary>
        ID,

        /// <summary>
        /// Represents an IF function.
        /// </summary>
        If,

        /// <summary>
        /// Represents a logical AND operation.
        /// </summary>
        And,

        /// <summary>
        /// Represents a logical OR operation.
        /// </summary>
        Or,

        /// <summary>
        /// Represents an integer number (no decimal point).
        /// </summary>
        DecimalNumber,

        /// <summary>
        /// Represents an integer number (no decimal point).
        /// </summary>
        IntegerNumber,

        /// <summary>
        /// Represents a CONTAINS function or operation.
        /// </summary>
        Contains,

        /// <summary>
        /// Represents a CONCATENATE function.
        /// </summary>
        Concatenate,

        /// <summary>
        /// Represents an OFFSET reference or function (if implemented).
        /// </summary>
        Offset,

        /// <summary>
        /// Represents a string literal.
        /// </summary>
        String,

        /// <summary>
        /// Represents a Boolean literal (TRUE/FALSE).
        /// </summary>
        Boolean,

        /// <summary>
        /// Represents a comma ',' used for separating arguments or values.
        /// </summary>
        Comma,

        /// <summary>
        /// Represents a single quote character.
        /// </summary>
        SingleQuote,

        /// <summary>
        /// Represents a double quote '"' character.
        /// </summary>
        DoubleQuote,

        /// <summary>
        /// Represents the ampersand '&amp;' operator for string concatenation.
        /// </summary>
        Ampersand,

        /// <summary>
        /// Represents a RIGHT function call.
        /// </summary>
        Right,

        /// <summary>
        /// Represents a LEFT function call.
        /// </summary>
        Left,

        /// <summary>
        /// Represents a LENGTH or LEN function call.
        /// </summary>
        Length,

        /// <summary>
        /// Represents a RAND function that generates a random value.
        /// </summary>
        Random,

        /// <summary>
        /// Represents a RANDBETWEEN function that generates a random number between two specified values.
        /// </summary>
        RandomBetween,

        /// <summary>
        /// Represents a RANDINT function that returns a random integer.
        /// </summary>
        RandomInteger,

        /// <summary>
        /// Represents a RANDINBETWEEN function.
        /// </summary>
        RandomIntegerBetween,

        /// <summary>
        /// Represents a ROUNDUP function.
        /// </summary>
        RoundUp,

        /// <summary>
        /// Represents a ROUNDDOWN or FLOOR function.
        /// </summary>
        RoundDown,

        /// <summary>
        /// Represents a ROUND function.
        /// </summary>
        Round,

        /// <summary>
        /// Represents an INCREMENT function.
        /// </summary>
        Increment,

        /// <summary>
        /// Represents the inverse cumulative distribution function for the normal distribution.
        /// </summary>
        NormalInverse,

        /// <summary>
        /// Represents the inverse cumulative distribution function for the triangular distribution.
        /// </summary>
        TriangularInverse,

        /// <summary>
        /// Represents a SUBSTRING function.
        /// </summary>
        Substring,

        /// <summary>
        /// Represents an INDEXOF or INSTRING function.
        /// </summary>
        IndexOf,

        /// <summary>
        /// Represents a conversion to integer (e.g., CINT, TOINTEGER).
        /// </summary>
        ConvertToInteger,

        /// <summary>
        /// Represents a conversion to double (e.g., CDBL, TODOUBLE).
        /// </summary>
        ConvertToDouble,

        /// <summary>
        /// Represents a conversion to string (e.g., CSTR, TOSTRING).
        /// </summary>
        ConvertToString,

        /// <summary>
        /// Represents a conversion to boolean (e.g., CBOOL, TOBOOLEAN).
        /// </summary>
        ConvertToBoolean,

        /// <summary>
        /// Represents a lexer error such as an unterminated string literal.
        /// </summary>
        LexerError

    }
}