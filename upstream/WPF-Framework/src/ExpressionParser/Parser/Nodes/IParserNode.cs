using System.Collections.Generic;

namespace ExpressionParser
{
    // Order of operations uses the following precedence levels (source: https://en.wikipedia.org/wiki/Order_of_operations)
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
    /// Defines the contract for all parse tree nodes in the expression parser.
    /// Parse nodes form a tree structure representing the parsed expression and can be evaluated to produce results.
    /// </summary>
    public interface IParserNode
    {
        /// <summary>
        /// Gets a value indicating whether the node performs case-sensitive string comparisons.
        /// </summary>
        bool IsCaseSensitive { get; }

        /// <summary>
        /// Gets a value indicating whether the parse node contains any errors.
        /// If errors exist, the node should not be evaluated as it may produce incorrect or unexpected results.
        /// </summary>
        bool ContainsErrors { get; }

        /// <summary>
        /// Gets the list of errors associated with the parse node.
        /// Each error contains the token where the error occurred and a description of the error condition.
        /// </summary>
        List<ParseError> GetErrors { get; }

        /// <summary>
        /// Simplifies the parse node and its children by performing constant folding and other optimizations.
        /// This is particularly useful when variables are present and the expression will be evaluated multiple times.
        /// </summary>
        /// <returns>A simplified parse node that produces the same results but evaluates more efficiently.</returns>
        IParserNode Simplify();

        /// <summary>
        /// Determines whether the parse node or any of its child nodes contain variable references.
        /// </summary>
        /// <returns>true if the node or its children contain variables; otherwise, false.</returns>
        bool ContainsVariable();

        /// <summary>
        /// Evaluates the parse node and returns the result of the evaluation.
        /// If the node contains errors, the result will have a ResultType of Error.
        /// </summary>
        /// <returns>A ParseNodeResult containing the evaluation result and its type.</returns>
        ParseNodeResult Evaluate();

        /// <summary>
        /// Gets the expected output type that this parse node will produce when evaluated.
        /// </summary>
        ResultType OutputType { get; }

        /// <summary>
        /// Gets all variable nodes contained within this parse node and its children.
        /// This method recursively traverses the parse tree to collect all variable references.
        /// </summary>
        /// <returns>A list of all VariableNode instances found in the parse tree.</returns>
        List<VariableNode> GetVariableNodes();
    }
}