namespace ExpressionParser
{
    /// <summary>
    /// Represents the possible result types of a parse node evaluation.
    /// Bitwise flags are used to allow type grouping (e.g., numeric types).
    /// </summary>
    public enum ResultType
    {
        /// <summary>
        /// Indicates that the result is invalid due to an error.
        /// </summary>
        Error = 0,

        /// <summary>
        /// Represents a double-precision floating-point number.
        /// </summary>
        Double = 1 << 0,

        /// <summary>
        /// Represents a single-precision floating-point number.
        /// </summary>
        Single = 1 << 1,

        /// <summary>
        /// Represents any floating-point type (Double or Single).
        /// </summary>
        FloatingPoint = Double + Single,

        /// <summary>
        /// Represents a 16-bit integer (short).
        /// </summary>
        Short = 1 << 3,

        /// <summary>
        /// Represents a 32-bit integer (int).
        /// </summary>
        Integer = 1 << 4,

        /// <summary>
        /// Represents an 8-bit integer (byte).
        /// </summary>
        Byte = 1 << 5,

        /// <summary>
        /// Represents any whole number type (Short, Integer, or Byte).
        /// </summary>
        IntegerValue = Short + Integer + Byte,

        /// <summary>
        /// Represents any numeric type (floating point or integer).
        /// </summary>
        Number = Double + Single + Short + Integer + Byte,

        /// <summary>
        /// Represents a string value.
        /// </summary>
        String = 1 << 6,

        /// <summary>
        /// Represents a boolean value (true/false).
        /// </summary>
        Boolean = 1 << 7,

        /// <summary>
        /// Represents an undeclared or unknown result type.
        /// </summary>
        UnDeclared = 1 << 8,

        /// <summary>
        /// Represents any valid result type (excluding Error).
        /// </summary>
        Valid = Double + Single + Short + Integer + Byte + String + Boolean + UnDeclared
    }
}