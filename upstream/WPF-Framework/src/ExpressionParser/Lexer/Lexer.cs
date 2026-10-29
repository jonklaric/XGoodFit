using System;
using System.Collections.Generic;
using System.Text;

namespace ExpressionParser
{

    /// <summary>
    /// Provides lexical analysis functionality to tokenize expression strings into a sequence of tokens.
    /// The lexer recognizes numbers, identifiers, keywords, operators, and string literals.
    /// </summary>
    public class Lexer
    {

        /// <summary>
        /// Takes an expression string and converts it into a collection of tokens.
        /// This method performs lexical analysis to identify numbers, identifiers, keywords, operators, and string literals.
        /// </summary>
        /// <param name="stringToTokenize">The expression string to tokenize.</param>
        /// <returns>A list of tokens parsed from the expression string.</returns>
        public static List<Token> TokenizeStringToList(string stringToTokenize)
        {
            var tokenList = new List<Token>();
            int startPosition;
            var hasDecimal = default(bool);
            for (int i = 0; i < stringToTokenize.Length; i++)
            {
                startPosition = i;
                if (char.IsDigit(stringToTokenize[i]) || stringToTokenize[i] == '.')
                {
                    string num = BuildNumber(stringToTokenize, ref i, ref hasDecimal);
                    if (hasDecimal == true)
                    {
                        if (num != ".")
                        {
                            tokenList.Add(new Token(startPosition, num, "", 0, TokenType.DecimalNumber, TokenClass.Value));
                        }
                        else
                        {
                            tokenList.Add(new Token(startPosition, num, "", 0, TokenType.String, TokenClass.Value));
                        }
                    }
                    else
                    {
                        tokenList.Add(new Token(startPosition, num, "", 0, TokenType.IntegerNumber, TokenClass.Value));
                    }
                }
                else if (char.IsLetter(stringToTokenize[i]) || stringToTokenize[i] == '_')
                {
                    string keyword = BuildString(stringToTokenize, ref i);
                    tokenList.Add(KeywordLookup(keyword, startPosition));
                }
                else
                {
                    var t = CharacterLookupTest(stringToTokenize, ref i);
                    tokenList.Add(t);
                    if (t.Type == TokenType.SingleQuote)
                    {
                        i += 1;
                        tokenList.Add(new Token(startPosition + 1, BuildString(stringToTokenize, '\'', ref i), "", 0, TokenType.String, TokenClass.Value));
                        if (TestForNextChar('\'', stringToTokenize, i))
                        {
                            tokenList.Add(new Token(i + 1, "'", "", 1, TokenType.SingleQuote, TokenClass.Other));
                            i += 1;
                        }
                        else
                        {
                            tokenList.Add(new Token(startPosition, "Unterminated string literal starting with '", "", 0, TokenType.LexerError, TokenClass.Other));
                        }
                    }
                    else if (t.Type == TokenType.DoubleQuote)
                    {
                        i += 1;
                        tokenList.Add(new Token(startPosition + 1, BuildString(stringToTokenize, '"', ref i), "", 0, TokenType.String, TokenClass.Value));
                        if (TestForNextChar('"', stringToTokenize, i))
                        {
                            tokenList.Add(new Token(i + 1, "\"", "", 1, TokenType.DoubleQuote, TokenClass.Other));
                            i += 1;
                        }
                        else
                        {
                            tokenList.Add(new Token(startPosition, "Unterminated string literal starting with \"", "", 0, TokenType.LexerError, TokenClass.Other));
                        }
                    }
                    else if (t.Type == TokenType.LeftBracket)
                    {
                        i += 1;
                        tokenList.Add(new Token(startPosition + 1, BuildString(stringToTokenize, ']', ref i), "", 0, TokenType.String, TokenClass.Value));
                        if (TestForNextChar(']', stringToTokenize, i))
                        {
                            tokenList.Add(new Token(i + 1, "]", "", 1, TokenType.RightBracket, TokenClass.Other));
                            i += 1;
                        }
                        else
                        {
                            tokenList.Add(new Token(startPosition, "Unterminated bracketed identifier starting with [", "", 0, TokenType.LexerError, TokenClass.Other));
                        }
                    }
                }
            }
            // Combine strings when adjacent
            return tokenList;
        }

        /// <summary>
        /// Builds a string token by consuming consecutive alphanumeric characters and underscores from the current position.
        /// This method is used to extract identifiers and keywords from the expression string.
        /// </summary>
        /// <param name="s">The original string being tokenized.</param>
        /// <param name="currentPosition">The current position in the original string. This parameter is advanced as characters are consumed.</param>
        /// <returns>A string built from the start position, terminating when a non-alphanumeric character (except underscore) is encountered.</returns>
        private static string BuildString(string s, ref int currentPosition)
        {
            var result = new StringBuilder();
            for (; currentPosition < s.Length; currentPosition++)
            {
                if (char.IsLetterOrDigit(s[currentPosition]) == false && s[currentPosition] != '_')
                {
                    currentPosition -= 1;
                    break;
                }
                result.Append(s[currentPosition]);
            }
            // 
            return result.ToString(); // .Trim()
        }
        /// <summary>
        /// Builds a string token by consuming characters from the current position until the specified stop character is found.
        /// This method is used to extract string literals delimited by quotes or brackets.
        /// </summary>
        /// <param name="s">The original string being tokenized.</param>
        /// <param name="stopCharacter">The character that defines the end of the string token.</param>
        /// <param name="currentPosition">The current position in the original string. This parameter is advanced to the position before the stop character.</param>
        /// <returns>A sub-string from the start position to the stop character (exclusive).</returns>
        private static string BuildString(string s, char stopCharacter, ref int currentPosition)
        {
            int testIndex = s.IndexOf(stopCharacter, currentPosition);
            if (testIndex == -1)
                testIndex = s.Length;
            int startPosition = currentPosition;
            currentPosition = testIndex - 1;
            return s.Substring(startPosition, testIndex - startPosition);
        }
        /// <summary>
        /// Builds a number token by consuming consecutive digits and at most one decimal point from the current position.
        /// </summary>
        /// <param name="s">The original string being tokenized.</param>
        /// <param name="currentPosition">The current position in the original string. This parameter is advanced as characters are consumed.</param>
        /// <param name="hasDecimal">Output parameter indicating whether the number contains a decimal point.</param>
        /// <returns>A string representation of the number (integer or decimal).</returns>
        private static string BuildNumber(string s, ref int currentPosition, ref bool hasDecimal)
        {
            var result = new StringBuilder();
            hasDecimal = false;
            bool hasExponent = false;
            for (; currentPosition < s.Length; currentPosition++)
            {
                char c = s[currentPosition];

                // Scientific notation: consume an `e`/`E` followed by optional sign and digits.
                // Without this, `1.5e10` lexed as `1.5` then identifier `e10`, producing the
                // confusing parser error "Cannot have two values next to each other without
                // an operator". A double with `e` is also implicitly a floating-point literal.
                if ((c == 'e' || c == 'E') && !hasExponent && result.Length > 0)
                {
                    int peek = currentPosition + 1;
                    if (peek < s.Length && (s[peek] == '+' || s[peek] == '-')) peek++;
                    if (peek < s.Length && char.IsDigit(s[peek]))
                    {
                        hasExponent = true;
                        hasDecimal = true; // promote to DecimalNumber
                        result.Append(c);
                        if (s[currentPosition + 1] == '+' || s[currentPosition + 1] == '-')
                        {
                            currentPosition++;
                            result.Append(s[currentPosition]);
                        }
                        continue;
                    }
                }

                if (char.IsDigit(c) == false)
                {
                    if (c != '.')
                    {
                        currentPosition -= 1;
                        break;
                    }
                    if (hasDecimal == true)
                    {
                        currentPosition -= 1;
                        break;
                    }
                    hasDecimal = true;
                }
                result.Append(c);
            }
            //
            return result.ToString();
        }

        /// <summary>
        /// Determines the appropriate token for a given character in the string.
        /// May also consume an additional character if part of a compound operator.
        /// </summary>
        /// <param name="s">The full string being parsed.</param>
        /// <param name="currentPosition">The current character index. This may be updated if a multi-character operator is found.</param>
        /// <returns>A Token representing the symbol or operator.</returns>
        private static Token CharacterLookupTest(string s, ref int currentPosition)
        {
            int startPosition = currentPosition;
            switch (s[startPosition])
            {
                case ' ':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "", 0, TokenType.Space, TokenClass.Other);
                    }
                case '(':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "", 1, TokenType.LeftParenthesis, TokenClass.Other); // LeftParenthesisToken(startPosition)
                    }
                case ')':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "", 15, TokenType.RightParenthesis, TokenClass.Other); // RightParenthesisToken(currentPosition)
                    }
                case '{':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "", 1, TokenType.LeftCurlyBracket, TokenClass.Other); // LeftCurlyBracketToken(startPosition)
                    }
                case '}':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "", 15, TokenType.RightCurlyBracket, TokenClass.Other); // RightCurlyBracketToken(startPosition)
                    }
                case '[':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "", 1, TokenType.LeftBracket, TokenClass.Other); // LeftBracketToken(startPosition)
                    }
                case ']':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "", 15, TokenType.RightBracket, TokenClass.Other); // RightBracketToken(startPosition)
                    }
                case '+':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "Parser Help/PLUSHelp.html", 4, TokenType.Addition, TokenClass.Operator); // AdditionToken(startPosition)
                    }
                case '-':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "Parser Help/SUBTRACTHelp.html", 4, TokenType.Subtraction, TokenClass.Operator); // SubtractionToken(startPosition)
                    }
                case '*':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "Parser Help/MULTIPLYHelp.html", 3, TokenType.Multiplication, TokenClass.Operator); // MultiplicationToken(startPosition)
                    }
                case '/':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "Parser Help/DIVISIONHelp.html", 3, TokenType.Division, TokenClass.Operator); // DivisionToken(startPosition)
                    }
                case '^':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "Parser Help/EXPONENTHelp.html", 2, TokenType.Exponent, TokenClass.Operator); // ExponentToken(startPosition)
                    }
                case '=':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "Parser Help/EQUALSHelp.html", 7, TokenType.Equals, TokenClass.Operator); // EqualsToken(startPosition)
                    }
                case '!':
                    {
                        if (TestForNextChar('=', s, startPosition))
                        {
                            currentPosition += 1;
                            return new Token(startPosition, "!=", "Parser Help/NOT EQUAL.html", 7, TokenType.NotEqual, TokenClass.Operator); // NotEqualToken(startPosition)
                        }
                        else
                        {
                            return new Token(startPosition, s[startPosition].ToString(), "", 0, TokenType.String, TokenClass.Value);
                        } 
                    }
                case '<':
                    {
                        if (TestForNextChar('=', s, startPosition))
                        {
                            currentPosition += 1;
                            return new Token(startPosition, "<=", "Parser Help/LE.html", 6, TokenType.LessThanOrEqual, TokenClass.Operator); // LessThanOrEqualToken(startPosition)
                        }
                        else if (TestForNextChar('>', s, startPosition))
                        {
                            currentPosition += 1;
                            return new Token(startPosition, "<>", "Parser Help/NOT EQUAL.html", 7, TokenType.NotEqual, TokenClass.Operator); // NotEqualToken(startPosition)
                        }
                        else
                        {
                            return new Token(startPosition, s[startPosition].ToString(), "Parser Help/LT.html", 6, TokenType.LessThan, TokenClass.Operator);
                        } // LessThanToken(startPosition)
                    }
                case '>':
                    {
                        if (TestForNextChar('=', s, startPosition))
                        {
                            currentPosition += 1;
                            return new Token(startPosition, ">=", "Parser Help/GE.html", 6, TokenType.GreaterThanOrEqual, TokenClass.Operator); // GreaterThanOrEqualToken(startPosition)
                        }
                        else
                        {
                            return new Token(startPosition, s[startPosition].ToString(), "Parser Help/GT.html", 6, TokenType.GreaterThan, TokenClass.Operator);
                        } // GreaterThanToken(startPosition)
                    }
                case ',':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "", 15, TokenType.Comma, TokenClass.Other); // CommaToken(startPosition)
                    }
                case '\'':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "", 1, TokenType.SingleQuote, TokenClass.Other); // SingleQuoteToken(startPosition)
                    }

                case '"':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "", 1, TokenType.DoubleQuote, TokenClass.Other); // DoubleQuoteToken(startPosition)
                    }
                case '&':
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "Parser Help/CONCATENATEHelp.html", 2, TokenType.Ampersand, TokenClass.Operator); // AmpersandToken(startPosition)
                    }

                default:
                    {
                        return new Token(startPosition, s[startPosition].ToString(), "", 0, TokenType.String, TokenClass.Value); // StringToken(startPosition, s(startPosition))
                    }
            }
        }

        /// <summary>
        /// Checks whether the character after the current position matches the specified character.
        /// This method is used to detect multi-character operators like "!=", "&lt;=", "&gt;=", and "&lt;&gt;".
        /// </summary>
        /// <param name="c">The character to look for.</param>
        /// <param name="s">The string to examine.</param>
        /// <param name="currentPosition">The current index in the string.</param>
        /// <returns>true if the next character matches and is within bounds; otherwise, false.</returns>
        private static bool TestForNextChar(char c, string s, int currentPosition)
        {
            if (currentPosition >= s.Length - 1)
                return false;
            if (s[currentPosition + 1] == c)
                return true;
            return false;
        }

        // The following is for an idea to store keywords, mostly function names, in a dictionary for quicker lookup by keyword. One reason it is faster than the
        // select case statement is that it can ignore case. It also has the advantage of better performance as more functions are added. 
        // Another advantage is that the keys can be used outside the lexer for looking up function keywords.

        /// <summary>
        /// Gets a dictionary of recognized keywords and functions identified by the lexer.
        /// The dictionary uses case-insensitive string comparison for keyword matching.
        /// Each keyword maps to a tuple containing: help path, operation order, token type, and token class.
        /// </summary>
        /// <remarks>
        /// The tuple components are:
        /// - Item1: Relative path to the help documentation file
        /// - Item2: Operation order precedence (byte)
        /// - Item3: Token type
        /// - Item4: Token classification
        /// </remarks>
        public static Dictionary<string, Tuple<string, byte, TokenType, TokenClass>> Keywords { get; private set; } = new Dictionary<string, Tuple<string, byte, TokenType, TokenClass>>(StringComparer.OrdinalIgnoreCase)
        {
            // Logical operators
            { "IF",             new Tuple<string, byte, TokenType, TokenClass>("Parser Help/IFHelp.html",  1, TokenType.If,  TokenClass.Function) },
            { "AND",            new Tuple<string, byte, TokenType, TokenClass>("Parser Help/ANDHelp.html", 1, TokenType.And, TokenClass.Function) },
            { "OR",             new Tuple<string, byte, TokenType, TokenClass>("Parser Help/ORHelp.html",  1, TokenType.Or,  TokenClass.Function) },

            // Boolean literals
            { "TRUE",           new Tuple<string, byte, TokenType, TokenClass>("", 0, TokenType.Boolean, TokenClass.Value) },
            { "FALSE",          new Tuple<string, byte, TokenType, TokenClass>("", 0, TokenType.Boolean, TokenClass.Value) },

            // String functions
            { "RIGHT",          new Tuple<string, byte, TokenType, TokenClass>("Parser Help/RIGHTHelp.html",     1, TokenType.Right,   TokenClass.Function) },
            { "LEFT",           new Tuple<string, byte, TokenType, TokenClass>("Parser Help/LEFTHelp.html",      1, TokenType.Left,    TokenClass.Function) },
            { "LEN",            new Tuple<string, byte, TokenType, TokenClass>("Parser Help/LENHelp.html",       1, TokenType.Length,  TokenClass.Function) },
            { "LENGTH",         new Tuple<string, byte, TokenType, TokenClass>("Parser Help/LENHelp.html",       1, TokenType.Length,  TokenClass.Function) },
            { "INDEXOF",        new Tuple<string, byte, TokenType, TokenClass>("Parser Help/INSTRINGHelp.html",  1, TokenType.IndexOf, TokenClass.Function) },
            { "INSTRING",       new Tuple<string, byte, TokenType, TokenClass>("Parser Help/INSTRINGHelp.html",  1, TokenType.IndexOf, TokenClass.Function) },
            { "SUBSTRING",      new Tuple<string, byte, TokenType, TokenClass>("Parser Help/SUBSTRINGHelp.html", 1, TokenType.Substring,    TokenClass.Function) },
            { "CONTAINS",       new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONTAINSHelp.html",  1, TokenType.Contains,     TokenClass.Function) },
            { "CONCATENATE",    new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONCATENATEHelp.html", 1, TokenType.Concatenate, TokenClass.Function) },

            // Numeric / random functions
            { "RAND",           new Tuple<string, byte, TokenType, TokenClass>("Parser Help/RANDHelp.html",           1, TokenType.Random,              TokenClass.Function) },
            { "RANDBETWEEN",    new Tuple<string, byte, TokenType, TokenClass>("Parser Help/RANDBETWEENHelp.html",    1, TokenType.RandomBetween,        TokenClass.Function) },
            { "RANDINT",        new Tuple<string, byte, TokenType, TokenClass>("Parser Help/RANDINTHelp.html",        1, TokenType.RandomInteger,        TokenClass.Function) },
            { "RANDINTBETWEEN", new Tuple<string, byte, TokenType, TokenClass>("Parser Help/RANDINTBETWEENHelp.html", 1, TokenType.RandomIntegerBetween, TokenClass.Function) },
            { "INCREMENT",      new Tuple<string, byte, TokenType, TokenClass>("Parser Help/INCREMENTHelp.html",      1, TokenType.Increment,            TokenClass.Function) },

            // Rounding functions
            { "ROUND",          new Tuple<string, byte, TokenType, TokenClass>("Parser Help/ROUNDHelp.html",     1, TokenType.Round,     TokenClass.Function) },
            { "ROUNDDOWN",      new Tuple<string, byte, TokenType, TokenClass>("Parser Help/ROUNDDOWNHelp.html", 1, TokenType.RoundDown, TokenClass.Function) },
            { "FLOOR",          new Tuple<string, byte, TokenType, TokenClass>("Parser Help/ROUNDDOWNHelp.html", 1, TokenType.RoundDown, TokenClass.Function) },
            { "ROUNDUP",        new Tuple<string, byte, TokenType, TokenClass>("Parser Help/ROUNDUPHelp.html",   1, TokenType.RoundUp,   TokenClass.Function) },
            { "CEILING",        new Tuple<string, byte, TokenType, TokenClass>("Parser Help/ROUNDUPHelp.html",   1, TokenType.RoundUp,   TokenClass.Function) },

            // Type conversion functions
            { "DBL",            new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTODOUBLEHelp.html",   1, TokenType.ConvertToDouble,  TokenClass.Function) },
            { "TODOUBLE",       new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTODOUBLEHelp.html",   1, TokenType.ConvertToDouble,  TokenClass.Function) },
            { "CDBL",           new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTODOUBLEHelp.html",   1, TokenType.ConvertToDouble,  TokenClass.Function) },
            { "INT",            new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTOINTHelp.html",      1, TokenType.ConvertToInteger, TokenClass.Function) },
            { "CINT",           new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTOINTHelp.html",      1, TokenType.ConvertToInteger, TokenClass.Function) },
            { "TOINTEGER",      new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTOINTHelp.html",      1, TokenType.ConvertToInteger, TokenClass.Function) },
            { "STR",            new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTOSTRINGHelp.html",   1, TokenType.ConvertToString,  TokenClass.Function) },
            { "CSTR",           new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTOSTRINGHelp.html",   1, TokenType.ConvertToString,  TokenClass.Function) },
            { "TOSTRING",       new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTOSTRINGHelp.html",   1, TokenType.ConvertToString,  TokenClass.Function) },
            { "BOOL",           new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTOBOOLEANHelp.html", 1, TokenType.ConvertToBoolean, TokenClass.Function) },
            { "CBOOL",          new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTOBOOLEANHelp.html", 1, TokenType.ConvertToBoolean, TokenClass.Function) },
            { "TOBOOLEAN",      new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTOBOOLEANHelp.html", 1, TokenType.ConvertToBoolean, TokenClass.Function) },
            { "TOLOGICAL",      new Tuple<string, byte, TokenType, TokenClass>("Parser Help/CONVERTTOBOOLEANHelp.html", 1, TokenType.ConvertToBoolean, TokenClass.Function) },
        };
        
        /// <summary>
        /// Determines the appropriate token for a given keyword string by performing a case-insensitive lookup in the Keywords dictionary.
        /// This method is used when the scanner encounters an alphabetic character sequence that is not a digit or special character.
        /// </summary>
        /// <param name="keyword">The keyword text to look up.</param>
        /// <param name="startposition">The start position of the keyword in the expression string.</param>
        /// <returns>A Token representing the keyword if found in the dictionary; otherwise, a string token.</returns>
        private static Token KeywordLookup(string keyword, int startposition)
        {
            Tuple<string, byte, TokenType, TokenClass> item = null;
            if (Keywords.TryGetValue(keyword, out item))
                return new Token(startposition, keyword, item.Item1, item.Item2, item.Item3, item.Item4);
            return new Token(startposition, keyword, "", 0, TokenType.String, TokenClass.Value);
        }
    }
}