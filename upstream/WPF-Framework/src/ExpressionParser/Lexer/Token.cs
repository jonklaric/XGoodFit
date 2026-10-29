namespace ExpressionParser
{
    // Order of operations uses the following precedence levels:
    // 1  | ()   []   ->   .   ::                                  | Function call, scope, array/member access
    // 2  | !   ~   -   +   *   &   sizeof   type cast   ++   --   | (most) unary operators, sizeof and type casts (right to left)
    // 3  | *   /   % MOD                                          | Multiplication, division, modulo
    // 4  | +   -                                                  | Addition and subtraction
    // 5  | <<   >>                                                | Bitwise shift left and right
    // 6  | <   <=   >   >=                                        | Comparisons: less-than and greater-than
    // 7  | ==   !=                                                | Comparisons: equal and not equal
    // 8  | &                                                      | Bitwise AND
    // 9  | ^                                                      | Bitwise exclusive OR (XOR)
    // 10 | |                                                      | Bitwise inclusive (normal) OR
    // 11 | &&                                                     | Logical AND
    // 12 | ||                                                     | Logical OR
    // 13 | ? :                                                    | Conditional expression (ternary)
    // 14 | =   +=   -=   *=   /=   %=   &=   |=   ^=   <<=   >>=  | Assignment operators (right to left)
    // 15 | ,                                                      | Comma operator

    /// <summary>
    /// Represents a single token produced by the lexical analysis of an expression string.
    /// A token encapsulates the type, value, and position of a lexical element within the input expression.
    /// </summary>
    public class Token
    {
        /// <summary>
        /// Gets the string value that represents the token.
        /// </summary>
        public string TokenString { get; private set; }

        /// <summary>
        /// Gets the zero-based start position of the token in the expression string.
        /// </summary>
        public int StartPosition { get; private set; }

        /// <summary>
        /// Gets the relative path to the help documentation file for this token type.
        /// </summary>
        public string HelpDocPath { get; private set; }

        /// <summary>
        /// Gets the order of operations precedence for this token type.
        /// Lower values indicate higher precedence in expression evaluation.
        /// </summary>
        public byte OperationOrder { get; private set; }

        /// <summary>
        /// Gets the type of the token.
        /// </summary>
        public TokenType Type { get; private set; }

        /// <summary>
        /// Gets the broad classification group of the token.
        /// </summary>
        public TokenClass TokenGroup { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Token class with the specified properties.
        /// </summary>
        /// <param name="initialPosition">The zero-based start position of the token in the expression string.</param>
        /// <param name="tokenValue">The string value that represents the token.</param>
        /// <param name="helpPath">The relative path to the help documentation file for this token type.</param>
        /// <param name="order">The order of operations precedence for this token type.</param>
        /// <param name="typeOfToken">The type of the token.</param>
        /// <param name="classOfToken">The broad classification group of the token.</param>
        public Token(int initialPosition, string tokenValue, string helpPath, byte order, TokenType typeOfToken, TokenClass classOfToken)
        {
            StartPosition = initialPosition;
            TokenString = tokenValue;
            HelpDocPath = helpPath;
            OperationOrder = order;
            Type = typeOfToken;
            TokenGroup = classOfToken;
        }
    }

    /// <summary>
    /// Represents the broad classification of a token parsed from an expression string.
    /// </summary>
    public enum TokenClass
    {
        /// <summary>
        /// A constant value such as a number, string, or boolean literal.
        /// </summary>
        Value,

        /// <summary>
        /// A function or keyword that performs an operation or transformation.
        /// </summary>
        Function,

        /// <summary>
        /// An operator that modifies values or controls expression evaluation.
        /// </summary>
        Operator,

        /// <summary>
        /// A miscellaneous token that does not fit into the main categories.
        /// </summary>
        Other
    }
}