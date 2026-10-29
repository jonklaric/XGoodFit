using System.Globalization;
using System.Windows;

namespace GenericControls
{
    /// <summary>
    /// Provides centralized, culture-aware number formatting and parsing utilities
    /// for international number format support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class NumberFormatHelper
    {
        /// <summary>
        /// Gets the decimal separator for the current culture.
        /// </summary>
        public static string DecimalSeparator => CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        /// <summary>
        /// Gets the group (thousands) separator for the current culture.
        /// </summary>
        public static string GroupSeparator => CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;

        /// <summary>
        /// Gets the negative sign for the current culture.
        /// </summary>
        public static string NegativeSign => CultureInfo.CurrentCulture.NumberFormat.NegativeSign;

        /// <summary>
        /// Gets the positive sign for the current culture.
        /// </summary>
        public static string PositiveSign => CultureInfo.CurrentCulture.NumberFormat.PositiveSign;

        /// <summary>
        /// Determines if the given character is a valid decimal separator for the current culture.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is the decimal separator.</returns>
        public static bool IsDecimalSeparator(char c)
        {
            return DecimalSeparator.Length == 1 && c == DecimalSeparator[0];
        }

        /// <summary>
        /// Determines if the given text is the decimal separator for the current culture.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text is the decimal separator.</returns>
        public static bool IsDecimalSeparator(string text)
        {
            return string.Equals(text, DecimalSeparator, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines if the given character is a valid group (thousands) separator for the current culture.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is the group separator.</returns>
        public static bool IsGroupSeparator(char c)
        {
            return GroupSeparator.Length == 1 && c == GroupSeparator[0];
        }

        /// <summary>
        /// Determines if the given text is the group separator for the current culture.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text is the group separator.</returns>
        public static bool IsGroupSeparator(string text)
        {
            return string.Equals(text, GroupSeparator, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines if the given text is the negative sign for the current culture.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text is the negative sign.</returns>
        public static bool IsNegativeSign(string text)
        {
            return string.Equals(text, NegativeSign, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines if the given text is the positive sign for the current culture.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text is the positive sign.</returns>
        public static bool IsPositiveSign(string text)
        {
            return string.Equals(text, PositiveSign, StringComparison.Ordinal);
        }

        /// <summary>
        /// Attempts to parse a string as a double using the current culture.
        /// Supports various number formats including scientific notation.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="result">The parsed double value if successful.</param>
        /// <returns>True if parsing was successful.</returns>
        public static bool TryParseDouble(string text, out double result)
        {
            return double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
        }

        /// <summary>
        /// Attempts to parse a string as a double using the specified culture.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="culture">The culture to use for parsing.</param>
        /// <param name="result">The parsed double value if successful.</param>
        /// <returns>True if parsing was successful.</returns>
        public static bool TryParseDouble(string text, CultureInfo culture, out double result)
        {
            return double.TryParse(text, NumberStyles.Any, culture, out result);
        }

        /// <summary>
        /// Attempts to parse a string as a float using the current culture.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="result">The parsed float value if successful.</param>
        /// <returns>True if parsing was successful.</returns>
        public static bool TryParseSingle(string text, out float result)
        {
            return float.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
        }

        /// <summary>
        /// Attempts to parse a string as an integer using the current culture.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="result">The parsed integer value if successful.</param>
        /// <returns>True if parsing was successful.</returns>
        public static bool TryParseInt(string text, out int result)
        {
            return int.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
        }

        /// <summary>
        /// Formats a double value using the current culture.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>The formatted string.</returns>
        public static string FormatDouble(double value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats a double value using the specified format and current culture.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="format">The format string (e.g., "F2", "N0").</param>
        /// <returns>The formatted string.</returns>
        public static string FormatDouble(double value, string format)
        {
            return value.ToString(format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats a double value with the specified number of decimal places using the current culture.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="decimalPlaces">The number of decimal places.</param>
        /// <param name="useThousandsSeparator">Whether to use thousands separator.</param>
        /// <returns>The formatted string.</returns>
        public static string FormatDouble(double value, int decimalPlaces, bool useThousandsSeparator)
        {
            string format = useThousandsSeparator ? "N" : "F";
            format += decimalPlaces.ToString();
            return value.ToString(format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Checks if the text already contains a decimal separator.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text contains a decimal separator.</returns>
        public static bool ContainsDecimalSeparator(string text)
        {
            return text.IndexOf(DecimalSeparator, StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Checks if the text already contains a negative sign.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text contains a negative sign.</returns>
        public static bool ContainsNegativeSign(string text)
        {
            return text.IndexOf(NegativeSign, StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Checks if the text already contains a positive sign.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text contains a positive sign.</returns>
        public static bool ContainsPositiveSign(string text)
        {
            return text.IndexOf(PositiveSign, StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Determines if the input character/string is valid for numeric input at the given position.
        /// </summary>
        /// <param name="inputText">The text being input.</param>
        /// <param name="currentText">The current text in the control.</param>
        /// <param name="selectionStart">The current cursor position.</param>
        /// <param name="selectedText">The currently selected text (if any).</param>
        /// <param name="allowNegative">Whether negative numbers are allowed.</param>
        /// <param name="allowDecimal">Whether decimal numbers are allowed.</param>
        /// <param name="allowScientific">Whether scientific notation is allowed.</param>
        /// <returns>True if the input should be allowed.</returns>
        public static bool IsValidNumericInput(
            string inputText,
            string currentText,
            int selectionStart,
            string selectedText,
            bool allowNegative,
            bool allowDecimal,
            bool allowScientific = false)
        {
            // Always allow digits
            if (inputText.Length == 1 && char.IsDigit(inputText[0]))
                return true;

            // Allow backspace
            if (inputText == "\b")
                return true;

            // Check for negative sign
            if (allowNegative && IsNegativeSign(inputText))
            {
                // Negative sign only allowed at start if not already present
                // or if the selected text contains the existing negative sign
                if (selectionStart == 0 && !ContainsNegativeSign(currentText))
                    return true;
                if (selectedText != null && selectedText.IndexOf(NegativeSign, StringComparison.Ordinal) >= 0)
                    return true;
                // Also allow after 'e' or 'E' in scientific notation
                if (allowScientific && selectionStart > 0)
                {
                    char prevChar = currentText[selectionStart - 1];
                    if (prevChar == 'e' || prevChar == 'E')
                        return true;
                }
            }

            // Check for decimal separator
            if (allowDecimal && IsDecimalSeparator(inputText))
            {
                // Only allow one decimal separator
                if (!ContainsDecimalSeparator(currentText))
                    return true;
                // Allow if selected text contains the decimal separator (replacing it)
                if (selectedText != null && selectedText.IndexOf(DecimalSeparator, StringComparison.Ordinal) >= 0)
                    return true;
            }

            // Check for scientific notation
            if (allowScientific && (inputText == "e" || inputText == "E"))
            {
                // Only allow one 'e' or 'E'
                if (currentText.IndexOf("e", StringComparison.OrdinalIgnoreCase) < 0)
                    return true;
            }

            // Check for positive sign in scientific notation
            if (allowScientific && IsPositiveSign(inputText))
            {
                if (selectionStart > 0)
                {
                    char prevChar = currentText[selectionStart - 1];
                    if ((prevChar == 'e' || prevChar == 'E') && !ContainsPositiveSign(currentText.Substring(selectionStart)))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if text represents a valid partial number input (e.g., "-", ".", "-." which are valid during typing).
        /// Also handles partial scientific notation like "1e", "1e-", "1E+".
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text is a valid partial number input.</returns>
        public static bool IsValidPartialNumber(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            // Valid partial inputs during typing
            if (text == NegativeSign)
                return true;
            if (text == DecimalSeparator)
                return true;
            if (text == NegativeSign + DecimalSeparator)
                return true;
            if (text == PositiveSign)
                return true;

            // Check for partial scientific notation (e.g., "1e", "1e-", "1E+", "1.5e", "-2e-")
            if (IsValidPartialScientificNotation(text))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if text represents a valid partial scientific notation input during typing.
        /// Examples: "1e", "1e-", "1E+", "1.5e", "-2e-", "1e-", ".5e"
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text is a valid partial scientific notation input.</returns>
        public static bool IsValidPartialScientificNotation(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // Find position of 'e' or 'E'
            int eIndex = text.IndexOf('e');
            if (eIndex < 0)
                eIndex = text.IndexOf('E');

            if (eIndex < 0)
                return false;

            // There must be something before 'e' (mantissa)
            if (eIndex == 0)
                return false;

            string mantissa = text.Substring(0, eIndex);
            string exponent = text.Substring(eIndex + 1);

            // Validate mantissa - must be a valid number or partial number
            if (!IsValidMantissa(mantissa))
                return false;

            // Validate exponent - can be empty, just a sign, or a partial number
            if (string.IsNullOrEmpty(exponent))
                return true; // "1e" is valid partial

            if (exponent == NegativeSign || exponent == PositiveSign)
                return true; // "1e-" or "1e+" is valid partial

            // Check if exponent is digits only (possibly with leading sign)
            string expDigits = exponent;
            if (expDigits.StartsWith(NegativeSign) || expDigits.StartsWith(PositiveSign))
                expDigits = expDigits.Substring(1);

            // Exponent digits can be empty after sign (that's a partial)
            if (string.IsNullOrEmpty(expDigits))
                return true;

            // Otherwise exponent must be all digits
            return IsAllDigits(expDigits);
        }

        /// <summary>
        /// Validates the mantissa part of a number (before 'e' in scientific notation).
        /// </summary>
        private static bool IsValidMantissa(string mantissa)
        {
            if (string.IsNullOrEmpty(mantissa))
                return false;

            // Handle leading negative sign
            string work = mantissa;
            if (work.StartsWith(NegativeSign))
                work = work.Substring(NegativeSign.Length);

            if (string.IsNullOrEmpty(work))
                return true; // Just "-" before e is valid partial

            // Check for decimal separator
            int decIndex = work.IndexOf(DecimalSeparator, StringComparison.Ordinal);

            if (decIndex >= 0)
            {
                // Split by decimal
                string intPart = work.Substring(0, decIndex);
                string fracPart = decIndex + DecimalSeparator.Length < work.Length
                    ? work.Substring(decIndex + DecimalSeparator.Length)
                    : "";

                // Both parts (if present) must be all digits
                if (!string.IsNullOrEmpty(intPart) && !IsAllDigits(intPart))
                    return false;
                if (!string.IsNullOrEmpty(fracPart) && !IsAllDigits(fracPart))
                    return false;

                return true;
            }

            // No decimal - must be all digits
            return IsAllDigits(work);
        }

        /// <summary>
        /// Checks if the text represents a valid infinity value.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text represents infinity.</returns>
        public static bool IsInfinityText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            string[] infinityPatterns = new[]
            {
                "inf", "Inf", "infinity", "Infinity",
                "+inf", "+Inf", "+infinity", "+Infinity",
                "-inf", "-Inf", "-infinity", "-Infinity",
                "∞", "+∞", "-∞"
            };

            foreach (var pattern in infinityPatterns)
            {
                if (string.Equals(text, pattern, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the text is a valid partial infinity input (prefix of an infinity string).
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text could be the start of an infinity value.</returns>
        public static bool IsPartialInfinityText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            string[] infinityPatterns = new[]
            {
                "-inf", "-Inf", "-infinity", "-Infinity",
                "+inf", "+Inf", "+infinity", "+Infinity",
                "inf", "Inf", "infinity", "Infinity"
            };

            foreach (var pattern in infinityPatterns)
            {
                if (pattern.StartsWith(text, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        #region RTL Support

        /// <summary>
        /// Gets whether the current culture uses right-to-left text direction.
        /// </summary>
        public static bool IsRightToLeft => CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;

        /// <summary>
        /// Gets the appropriate FlowDirection for the current culture.
        /// </summary>
        public static FlowDirection CurrentFlowDirection =>
            IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        /// <summary>
        /// Checks if the character is a valid numeric digit in any numeral system.
        /// Supports Western Arabic (0-9), Eastern Arabic (٠-٩), Extended Arabic-Indic (۰-۹),
        /// and other Unicode digit categories.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is a numeric digit.</returns>
        public static bool IsNumericDigit(char c)
        {
            // char.IsDigit covers all Unicode digit categories including:
            // - Western Arabic numerals (0-9)
            // - Eastern Arabic-Indic numerals (٠-٩)
            // - Extended Arabic-Indic numerals (۰-۹)
            // - Devanagari numerals (०-९)
            // - Bengali numerals (০-৯)
            // - And many more...
            return char.IsDigit(c);
        }

        /// <summary>
        /// Checks if all characters in the text are valid numeric digits.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if all characters are numeric digits.</returns>
        public static bool IsAllDigits(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (char c in text)
            {
                if (!IsNumericDigit(c))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the numeric value of a digit character from any numeral system.
        /// </summary>
        /// <param name="c">The digit character.</param>
        /// <returns>The numeric value (0-9) or -1 if not a digit.</returns>
        public static int GetDigitValue(char c)
        {
            if (!char.IsDigit(c))
                return -1;

            // char.GetNumericValue returns the digit value for any Unicode digit
            double value = char.GetNumericValue(c);
            if (value >= 0 && value <= 9)
                return (int)value;

            return -1;
        }

        /// <summary>
        /// Converts a string containing digits from any numeral system to Western Arabic numerals (0-9).
        /// This is useful for parsing numbers entered in non-Western numeral systems.
        /// </summary>
        /// <param name="text">The text containing digits in any numeral system.</param>
        /// <returns>The text with all digits converted to Western Arabic numerals.</returns>
        public static string NormalizeDigits(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            char[] result = text.ToCharArray();
            for (int i = 0; i < result.Length; i++)
            {
                if (char.IsDigit(result[i]))
                {
                    int value = GetDigitValue(result[i]);
                    if (value >= 0)
                    {
                        result[i] = (char)('0' + value);
                    }
                }
            }
            return new string(result);
        }

        /// <summary>
        /// Gets the negative sign position for the current culture.
        /// In some RTL cultures, the negative sign appears on the right side of the number.
        /// </summary>
        /// <returns>True if the negative sign appears before the number (left in LTR, right in RTL display).</returns>
        public static bool NegativeSignIsPrefix
        {
            get
            {
                // NumberNegativePattern values:
                // 0 = (n), 1 = -n, 2 = - n, 3 = n-, 4 = n -
                int pattern = CultureInfo.CurrentCulture.NumberFormat.NumberNegativePattern;
                return pattern == 1 || pattern == 2;
            }
        }

        #endregion
    }
}
