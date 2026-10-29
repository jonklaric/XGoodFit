using System.Collections.Generic;

namespace ExpressionParserControls
{
    /// <summary>
    /// Describes a single expression parser function with its metadata for display in the UI.
    /// </summary>
    public class FunctionDescriptor
    {
        /// <summary>
        /// The primary function name (e.g., "IF", "ROUND").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The category this function belongs to (e.g., "Logical", "String").
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// The function syntax showing parameters (e.g., "IF(condition, true_value, false_value)").
        /// </summary>
        public string Syntax { get; }

        /// <summary>
        /// The return type description (e.g., "Number", "String", "Boolean").
        /// </summary>
        public string Returns { get; }

        /// <summary>
        /// A brief description of what the function does.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// An example usage of the function.
        /// </summary>
        public string Example { get; }

        /// <summary>
        /// Alternative names for this function (e.g., "LEN" and "LENGTH").
        /// </summary>
        public string[] Aliases { get; }

        /// <summary>
        /// The text to insert into the expression when this function is selected.
        /// </summary>
        public string InsertText { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionDescriptor"/> class.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="category">The category this function belongs to.</param>
        /// <param name="syntax">The function syntax.</param>
        /// <param name="returns">Description of the return value.</param>
        /// <param name="description">A description of the function.</param>
        /// <param name="example">An example usage of the function.</param>
        /// <param name="aliases">Alternative names for this function.</param>
        /// <param name="insertText">The text to insert into the expression, or null to use the name followed by an opening parenthesis.</param>
        public FunctionDescriptor(string name, string category, string syntax, string returns, string description, string example, string[] aliases, string? insertText = null)
        {
            Name = name;
            Category = category;
            Syntax = syntax;
            Returns = returns;
            Description = description;
            Example = example;
            Aliases = aliases;
            InsertText = insertText ?? name + "(";
        }
    }

    /// <summary>
    /// Provides metadata for all available expression parser functions.
    /// Drives the AvailableFunctions TreeView grouping and detail panel.
    /// </summary>
    public static class FunctionInfo
    {
        /// <summary>
        /// All function categories in display order.
        /// </summary>
        public static readonly string[] Categories = { "Logical", "String", "Math", "Random", "Conversion" };

        /// <summary>
        /// All function descriptors, ordered by category then name.
        /// </summary>
        public static readonly List<FunctionDescriptor> Functions = new List<FunctionDescriptor>
        {
            // ── Logical ──
            new FunctionDescriptor(
                "IF", "Logical",
                "IF(condition, true_value, false_value)",
                "Varies (matches value type)",
                "Evaluates a condition and returns one value if true, another if false.",
                "IF([Age] > 18, \"Adult\", \"Minor\")",
                System.Array.Empty<string>()),

            new FunctionDescriptor(
                "AND", "Logical",
                "AND(condition1, condition2, ...)",
                "Boolean",
                "Returns TRUE if all conditions are true, FALSE otherwise.",
                "AND([Age] > 18, [Status] = \"Active\")",
                System.Array.Empty<string>()),

            new FunctionDescriptor(
                "OR", "Logical",
                "OR(condition1, condition2, ...)",
                "Boolean",
                "Returns TRUE if any condition is true, FALSE otherwise.",
                "OR([Status] = \"Active\", [Status] = \"Pending\")",
                System.Array.Empty<string>()),

            new FunctionDescriptor(
                "CONTAINS", "Logical",
                "CONTAINS(text, search_text)",
                "Boolean",
                "Returns TRUE if the text contains the search text.",
                "CONTAINS([Name], \"Smith\")",
                System.Array.Empty<string>()),

            // ── String ──
            new FunctionDescriptor(
                "LEFT", "String",
                "LEFT(text, num_chars)",
                "String",
                "Returns the specified number of characters from the start of a text string.",
                "LEFT([Name], 3)",
                System.Array.Empty<string>()),

            new FunctionDescriptor(
                "RIGHT", "String",
                "RIGHT(text, num_chars)",
                "String",
                "Returns the specified number of characters from the end of a text string.",
                "RIGHT([Code], 4)",
                System.Array.Empty<string>()),

            new FunctionDescriptor(
                "LEN", "String",
                "LEN(text)",
                "Integer",
                "Returns the number of characters in a text string.",
                "LEN([Name])",
                new[] { "LENGTH" }),

            new FunctionDescriptor(
                "SUBSTRING", "String",
                "SUBSTRING(text, start_index, length)",
                "String",
                "Returns a portion of a text string beginning at start_index for the specified length.",
                "SUBSTRING([Code], 0, 3)",
                System.Array.Empty<string>()),

            new FunctionDescriptor(
                "INDEXOF", "String",
                "INDEXOF(text, search_text)",
                "Integer",
                "Returns the zero-based position of the first occurrence of search_text within text, or -1 if not found.",
                "INDEXOF([Name], \"son\")",
                new[] { "INSTRING" }),

            new FunctionDescriptor(
                "CONCATENATE", "String",
                "CONCATENATE(text1, text2, ...)",
                "String",
                "Joins multiple text strings into one. The & operator can also be used.",
                "CONCATENATE([First], \" \", [Last])",
                System.Array.Empty<string>()),

            // ── Math ──
            new FunctionDescriptor(
                "ROUND", "Math",
                "ROUND(number, num_digits)",
                "Number",
                "Rounds a number to a specified number of decimal places.",
                "ROUND([Value], 2)",
                System.Array.Empty<string>()),

            new FunctionDescriptor(
                "ROUNDUP", "Math",
                "ROUNDUP(number, num_digits)",
                "Number",
                "Rounds a number up (away from zero) to the specified number of decimal places.",
                "ROUNDUP([Value], 0)",
                new[] { "CEILING" }),

            new FunctionDescriptor(
                "ROUNDDOWN", "Math",
                "ROUNDDOWN(number, num_digits)",
                "Number",
                "Rounds a number down (toward zero) to the specified number of decimal places.",
                "ROUNDDOWN([Value], 0)",
                new[] { "FLOOR" }),

            new FunctionDescriptor(
                "INCREMENT", "Math",
                "INCREMENT(start, step)",
                "Number",
                "Generates an incrementing sequence starting at start with the given step for each row.",
                "INCREMENT(1, 1)",
                System.Array.Empty<string>()),

            // ── Random ──
            new FunctionDescriptor(
                "RAND", "Random",
                "RAND()",
                "Double",
                "Returns a random decimal number between 0 and 1.",
                "RAND()",
                System.Array.Empty<string>()),

            new FunctionDescriptor(
                "RANDBETWEEN", "Random",
                "RANDBETWEEN(min, max, seed)",
                "Double",
                "Returns a random decimal number between min and max using the specified seed.",
                "RANDBETWEEN(0, 100, 42)",
                System.Array.Empty<string>()),

            new FunctionDescriptor(
                "RANDINT", "Random",
                "RANDINT()",
                "Integer",
                "Returns a random integer.",
                "RANDINT()",
                System.Array.Empty<string>()),

            new FunctionDescriptor(
                "RANDINTBETWEEN", "Random",
                "RANDINTBETWEEN(min, max, seed)",
                "Integer",
                "Returns a random integer between min and max using the specified seed.",
                "RANDINTBETWEEN(1, 100, 42)",
                System.Array.Empty<string>()),

            // ── Conversion ──
            new FunctionDescriptor(
                "DBL", "Conversion",
                "DBL(value)",
                "Double",
                "Converts a value to a double-precision floating-point number.",
                "DBL([IntColumn])",
                new[] { "CDBL", "TODOUBLE" }),

            new FunctionDescriptor(
                "INT", "Conversion",
                "INT(value)",
                "Integer",
                "Converts a value to an integer (truncates decimal portion).",
                "INT([DoubleColumn])",
                new[] { "CINT", "TOINTEGER" }),

            new FunctionDescriptor(
                "STR", "Conversion",
                "STR(value)",
                "String",
                "Converts a value to its string representation.",
                "STR([NumericColumn])",
                new[] { "CSTR", "TOSTRING" }),

            new FunctionDescriptor(
                "BOOL", "Conversion",
                "BOOL(value)",
                "Boolean",
                "Converts a value to a boolean (true/false).",
                "BOOL([NumericColumn])",
                new[] { "CBOOL", "TOBOOLEAN", "TOLOGICAL" }),
        };

        /// <summary>
        /// Returns all functions grouped by category.
        /// </summary>
        public static Dictionary<string, List<FunctionDescriptor>> GetGroupedFunctions()
        {
            var grouped = new Dictionary<string, List<FunctionDescriptor>>();
            foreach (var category in Categories)
            {
                grouped[category] = new List<FunctionDescriptor>();
            }
            foreach (var func in Functions)
            {
                if (grouped.ContainsKey(func.Category))
                    grouped[func.Category].Add(func);
            }
            return grouped;
        }
    }
}
