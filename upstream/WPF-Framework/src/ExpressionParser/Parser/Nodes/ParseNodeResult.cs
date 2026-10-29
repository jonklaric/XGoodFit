using System;

namespace ExpressionParser
{
    /// <summary>
    /// Encapsulates the result of evaluating a parse node, including both the computed value and its type.
    /// This class provides type safety and conversion utilities for working with expression evaluation results.
    /// </summary>
    public class ParseNodeResult
    {
        /// <summary>
        /// Gets the type of the result value.
        /// For example, ResultType.Double indicates that the Result object is of type double.
        /// </summary>
        public ResultType Type { get; private set; }

        /// <summary>
        /// Gets the result value of the parse node evaluation.
        /// The runtime type of this object corresponds to the Type property.
        /// </summary>
        public object Result { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ParseNodeResult class with the specified value and type.
        /// </summary>
        /// <param name="resultValue">The computed result value.</param>
        /// <param name="resultType">The type classification of the result.</param>
        public ParseNodeResult(object resultValue, ResultType resultType)
        {
            Result = resultValue;
            Type = resultType;
        }

        /// <summary>
        /// Converts a native CLR <see cref="Type"/> to the corresponding <see cref="ResultType"/> used in parsing.
        /// </summary>
        /// <param name="t">The CLR type to convert.</param>
        /// <returns>The corresponding ResultType, or ResultType.UnDeclared if the type is not recognized.</returns>
        public static ResultType TypeToParserResultType(Type t)
        {
            switch (t)
            {
                case var @case when @case == typeof(double):
                    {
                        return ResultType.Double;
                    }
                case var case1 when case1 == typeof(bool):
                    {
                        return ResultType.Boolean;
                    }
                case var case2 when case2 == typeof(string):
                    {
                        return ResultType.String;
                    }
                case var case3 when case3 == typeof(int):
                case var case4 when case4 == typeof(long):
                    {
                        return ResultType.Integer;
                    }
                case var case5 when case5 == typeof(float):
                    {
                        return ResultType.Single;
                    }
                case var case6 when case6 == typeof(short):
                    {
                        return ResultType.Short;
                    }
                case var case7 when case7 == typeof(byte):
                    {
                        return ResultType.Byte;
                    }

                default:
                    {
                        return ResultType.UnDeclared;
                    }
            }
        }

        /// <summary>
        /// Converts a <see cref="ResultType"/> to the corresponding CLR <see cref="Type"/>.
        /// </summary>
        /// <param name="t">The parser result type to convert.</param>
        /// <returns>The corresponding CLR Type, or typeof(object) if the ResultType is not recognized.</returns>
        public static Type ParserResultTypeToType(ResultType t)
        {
            switch (t)
            {
                case ResultType.Double:
                    {
                        return typeof(double);
                    }
                case ResultType.Boolean:
                    {
                        return typeof(bool);
                    }
                case ResultType.String:
                    {
                        return typeof(string);
                    }
                case ResultType.Integer:
                    {
                        return typeof(int);
                    }
                case ResultType.Single:
                    {
                        return typeof(float);
                    }
                case ResultType.Short:
                    {
                        return typeof(short);
                    }
                case ResultType.Byte:
                    {
                        return typeof(byte);
                    }

                default:
                    {
                        return typeof(object);
                    }
            }
        }
    }
}