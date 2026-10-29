using System.Globalization;
using Xunit;

namespace GenericControls.Tests.Utilities;

/// <summary>
/// Unit tests for the <see cref="NumberFormatHelper"/> class.
/// </summary>
public class NumberFormatHelperTests
{
    #region Parsing Tests - TryParseDouble

    /// <summary>
    /// Tests that TryParseDouble correctly parses valid numeric strings.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="expected">The expected double value.</param>
    [Theory]
    [InlineData("123", 123.0)]
    [InlineData("123.45", 123.45)]
    [InlineData("-123.45", -123.45)]
    [InlineData("0", 0.0)]
    [InlineData("0.0", 0.0)]
    [InlineData("-0", 0.0)]
    public void TryParseDouble_ValidInput_ReturnsTrueAndCorrectValue(string input, double expected)
    {
        // Act
        var result = NumberFormatHelper.TryParseDouble(input, out double value);

        // Assert
        Assert.True(result);
        Assert.Equal(expected, value);
    }

    /// <summary>
    /// Tests that TryParseDouble correctly parses scientific notation.
    /// </summary>
    /// <param name="input">The input string in scientific notation.</param>
    /// <param name="expected">The expected double value.</param>
    [Theory]
    [InlineData("1e2", 100.0)]
    [InlineData("1.5e2", 150.0)]
    [InlineData("1E2", 100.0)]
    [InlineData("-1e2", -100.0)]
    [InlineData("1e-2", 0.01)]
    public void TryParseDouble_ScientificNotation_ReturnsTrueAndCorrectValue(string input, double expected)
    {
        // Act
        var result = NumberFormatHelper.TryParseDouble(input, out double value);

        // Assert
        Assert.True(result);
        Assert.Equal(expected, value, 10);
    }

    /// <summary>
    /// Tests that TryParseDouble returns false for invalid input strings.
    /// </summary>
    /// <param name="input">The invalid input string.</param>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12abc")]
    [InlineData("abc12")]
    [InlineData("--123")]
    public void TryParseDouble_InvalidInput_ReturnsFalse(string input)
    {
        // Act
        var result = NumberFormatHelper.TryParseDouble(input, out _);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that TryParseDouble uses the specified culture for parsing.
    /// </summary>
    [Fact]
    public void TryParseDouble_WithCulture_UsesSpecifiedCulture()
    {
        // Arrange
        var frenchCulture = new CultureInfo("fr-FR");
        var input = "123,45"; // French format uses comma as decimal separator

        // Act
        var result = NumberFormatHelper.TryParseDouble(input, frenchCulture, out double value);

        // Assert
        Assert.True(result);
        Assert.Equal(123.45, value);
    }

    #endregion

    #region Parsing Tests - TryParseSingle

    /// <summary>
    /// Tests that TryParseSingle correctly parses valid numeric strings to float.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="expected">The expected float value.</param>
    [Theory]
    [InlineData("123", 123.0f)]
    [InlineData("123.45", 123.45f)]
    [InlineData("-123.45", -123.45f)]
    [InlineData("0", 0.0f)]
    public void TryParseSingle_ValidInput_ReturnsTrueAndCorrectValue(string input, float expected)
    {
        // Act
        var result = NumberFormatHelper.TryParseSingle(input, out float value);

        // Assert
        Assert.True(result);
        Assert.Equal(expected, value, 4);
    }

    /// <summary>
    /// Tests that TryParseSingle returns false for invalid input strings.
    /// </summary>
    /// <param name="input">The invalid input string.</param>
    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    public void TryParseSingle_InvalidInput_ReturnsFalse(string input)
    {
        // Act
        var result = NumberFormatHelper.TryParseSingle(input, out _);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Parsing Tests - TryParseInt

    /// <summary>
    /// Tests that TryParseInt correctly parses valid integer strings.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="expected">The expected integer value.</param>
    [Theory]
    [InlineData("123", 123)]
    [InlineData("-123", -123)]
    [InlineData("0", 0)]
    public void TryParseInt_ValidInput_ReturnsTrueAndCorrectValue(string input, int expected)
    {
        // Act
        var result = NumberFormatHelper.TryParseInt(input, out int value);

        // Assert
        Assert.True(result);
        Assert.Equal(expected, value);
    }

    /// <summary>
    /// Tests that TryParseInt returns false for invalid input strings.
    /// </summary>
    /// <param name="input">The invalid input string.</param>
    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("123.45")]
    public void TryParseInt_InvalidInput_ReturnsFalse(string input)
    {
        // Act
        var result = NumberFormatHelper.TryParseInt(input, out _);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Formatting Tests - FormatDouble

    /// <summary>
    /// Tests that FormatDouble formats a double value correctly.
    /// </summary>
    [Fact]
    public void FormatDouble_BasicValue_FormatsCorrectly()
    {
        // Arrange
        var value = 123.45;

        // Act
        var result = NumberFormatHelper.FormatDouble(value);

        // Assert
        Assert.Contains("123", result);
    }

    /// <summary>
    /// Tests that FormatDouble formats a double value with a specified format string.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The format string.</param>
    /// <param name="expected">The expected formatted string.</param>
    [Theory]
    [InlineData(123.456, "F2", "123.46")]
    [InlineData(1000.0, "N0", "1,000")]
    public void FormatDouble_WithFormat_FormatsCorrectly(double value, string format, string expected)
    {
        // Act
        var result = NumberFormatHelper.FormatDouble(value, format);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that FormatDouble formats a double value with specified decimal places.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="decimalPlaces">The number of decimal places.</param>
    /// <param name="useThousands">Whether to use thousand separators.</param>
    /// <param name="expected">The expected formatted string.</param>
    [Theory]
    [InlineData(123.456, 2, false, "123.46")]
    [InlineData(123.456, 0, false, "123")]
    [InlineData(1234.5, 1, true, "1,234.5")]
    public void FormatDouble_WithDecimalPlaces_FormatsCorrectly(double value, int decimalPlaces, bool useThousands, string expected)
    {
        // Act
        var result = NumberFormatHelper.FormatDouble(value, decimalPlaces, useThousands);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Separator Tests

    /// <summary>
    /// Tests that DecimalSeparator returns the current culture's decimal separator.
    /// </summary>
    [Fact]
    public void DecimalSeparator_ReturnsCurrentCultureSeparator()
    {
        // Assert
        var expected = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        Assert.Equal(expected, NumberFormatHelper.DecimalSeparator);
    }

    /// <summary>
    /// Tests that GroupSeparator returns the current culture's group separator.
    /// </summary>
    [Fact]
    public void GroupSeparator_ReturnsCurrentCultureSeparator()
    {
        // Assert
        var expected = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
        Assert.Equal(expected, NumberFormatHelper.GroupSeparator);
    }

    /// <summary>
    /// Tests that NegativeSign returns the current culture's negative sign.
    /// </summary>
    [Fact]
    public void NegativeSign_ReturnsCurrentCultureSign()
    {
        // Assert
        var expected = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
        Assert.Equal(expected, NumberFormatHelper.NegativeSign);
    }

    /// <summary>
    /// Tests that PositiveSign returns the current culture's positive sign.
    /// </summary>
    [Fact]
    public void PositiveSign_ReturnsCurrentCultureSign()
    {
        // Assert
        var expected = CultureInfo.CurrentCulture.NumberFormat.PositiveSign;
        Assert.Equal(expected, NumberFormatHelper.PositiveSign);
    }

    #endregion

    #region IsDecimalSeparator Tests

    /// <summary>
    /// Tests that IsDecimalSeparator returns true for the actual decimal separator character.
    /// </summary>
    [Fact]
    public void IsDecimalSeparator_Char_WithActualSeparator_ReturnsTrue()
    {
        // Arrange
        var separator = NumberFormatHelper.DecimalSeparator;
        if (separator.Length == 1)
        {
            // Act
            var result = NumberFormatHelper.IsDecimalSeparator(separator[0]);

            // Assert
            Assert.True(result);
        }
    }

    /// <summary>
    /// Tests that IsDecimalSeparator returns false for other characters.
    /// </summary>
    [Fact]
    public void IsDecimalSeparator_Char_WithOtherChar_ReturnsFalse()
    {
        // Act
        var result = NumberFormatHelper.IsDecimalSeparator('x');

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsDecimalSeparator returns true for the actual decimal separator string.
    /// </summary>
    [Fact]
    public void IsDecimalSeparator_String_WithActualSeparator_ReturnsTrue()
    {
        // Arrange
        var separator = NumberFormatHelper.DecimalSeparator;

        // Act
        var result = NumberFormatHelper.IsDecimalSeparator(separator);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsDecimalSeparator returns false for other strings.
    /// </summary>
    [Fact]
    public void IsDecimalSeparator_String_WithOtherString_ReturnsFalse()
    {
        // Act
        var result = NumberFormatHelper.IsDecimalSeparator("xyz");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsGroupSeparator Tests

    /// <summary>
    /// Tests that IsGroupSeparator returns true for the actual group separator character.
    /// </summary>
    [Fact]
    public void IsGroupSeparator_Char_WithActualSeparator_ReturnsTrue()
    {
        // Arrange
        var separator = NumberFormatHelper.GroupSeparator;
        if (separator.Length == 1)
        {
            // Act
            var result = NumberFormatHelper.IsGroupSeparator(separator[0]);

            // Assert
            Assert.True(result);
        }
    }

    /// <summary>
    /// Tests that IsGroupSeparator returns true for the actual group separator string.
    /// </summary>
    [Fact]
    public void IsGroupSeparator_String_WithActualSeparator_ReturnsTrue()
    {
        // Arrange
        var separator = NumberFormatHelper.GroupSeparator;

        // Act
        var result = NumberFormatHelper.IsGroupSeparator(separator);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region ContainsDecimalSeparator Tests

    /// <summary>
    /// Tests that ContainsDecimalSeparator returns true for text containing the decimal separator.
    /// </summary>
    [Fact]
    public void ContainsDecimalSeparator_TextWithSeparator_ReturnsTrue()
    {
        // Arrange
        var text = $"123{NumberFormatHelper.DecimalSeparator}45";

        // Act
        var result = NumberFormatHelper.ContainsDecimalSeparator(text);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that ContainsDecimalSeparator returns false for text without the decimal separator.
    /// </summary>
    [Fact]
    public void ContainsDecimalSeparator_TextWithoutSeparator_ReturnsFalse()
    {
        // Arrange
        var text = "12345";

        // Act
        var result = NumberFormatHelper.ContainsDecimalSeparator(text);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ContainsNegativeSign Tests

    /// <summary>
    /// Tests that ContainsNegativeSign returns true for text containing the negative sign.
    /// </summary>
    [Fact]
    public void ContainsNegativeSign_TextWithNegativeSign_ReturnsTrue()
    {
        // Arrange
        var text = $"{NumberFormatHelper.NegativeSign}123";

        // Act
        var result = NumberFormatHelper.ContainsNegativeSign(text);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that ContainsNegativeSign returns false for text without the negative sign.
    /// </summary>
    [Fact]
    public void ContainsNegativeSign_TextWithoutNegativeSign_ReturnsFalse()
    {
        // Arrange
        var text = "123";

        // Act
        var result = NumberFormatHelper.ContainsNegativeSign(text);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ContainsPositiveSign Tests

    /// <summary>
    /// Tests that ContainsPositiveSign returns true for text containing the positive sign.
    /// </summary>
    [Fact]
    public void ContainsPositiveSign_TextWithPositiveSign_ReturnsTrue()
    {
        // Arrange
        var text = $"{NumberFormatHelper.PositiveSign}123";

        // Act
        var result = NumberFormatHelper.ContainsPositiveSign(text);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that ContainsPositiveSign returns false for text without the positive sign.
    /// </summary>
    [Fact]
    public void ContainsPositiveSign_TextWithoutPositiveSign_ReturnsFalse()
    {
        // Arrange
        var text = "123";

        // Act
        var result = NumberFormatHelper.ContainsPositiveSign(text);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsValidNumericInput Tests

    /// <summary>
    /// Tests that IsValidNumericInput returns true for digit input.
    /// </summary>
    [Fact]
    public void IsValidNumericInput_Digit_ReturnsTrue()
    {
        // Act
        var result = NumberFormatHelper.IsValidNumericInput("5", "123", 3, "", true, true);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidNumericInput returns true for backspace input.
    /// </summary>
    [Fact]
    public void IsValidNumericInput_Backspace_ReturnsTrue()
    {
        // Act
        var result = NumberFormatHelper.IsValidNumericInput("\b", "123", 3, "", true, true);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidNumericInput returns true for negative sign at start when allowed.
    /// </summary>
    [Fact]
    public void IsValidNumericInput_NegativeSignAtStart_WhenAllowed_ReturnsTrue()
    {
        // Act
        var result = NumberFormatHelper.IsValidNumericInput(
            NumberFormatHelper.NegativeSign,
            "123",
            0,
            "",
            allowNegative: true,
            allowDecimal: true);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidNumericInput returns false for negative sign at start when not allowed.
    /// </summary>
    [Fact]
    public void IsValidNumericInput_NegativeSignAtStart_WhenNotAllowed_ReturnsFalse()
    {
        // Act
        var result = NumberFormatHelper.IsValidNumericInput(
            NumberFormatHelper.NegativeSign,
            "123",
            0,
            "",
            allowNegative: false,
            allowDecimal: true);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsValidNumericInput returns true for decimal separator when allowed.
    /// </summary>
    [Fact]
    public void IsValidNumericInput_DecimalSeparator_WhenAllowed_ReturnsTrue()
    {
        // Act
        var result = NumberFormatHelper.IsValidNumericInput(
            NumberFormatHelper.DecimalSeparator,
            "123",
            3,
            "",
            allowNegative: true,
            allowDecimal: true);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidNumericInput returns false for decimal separator when not allowed.
    /// </summary>
    [Fact]
    public void IsValidNumericInput_DecimalSeparator_WhenNotAllowed_ReturnsFalse()
    {
        // Act
        var result = NumberFormatHelper.IsValidNumericInput(
            NumberFormatHelper.DecimalSeparator,
            "123",
            3,
            "",
            allowNegative: true,
            allowDecimal: false);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsValidNumericInput returns false for a second decimal separator.
    /// </summary>
    [Fact]
    public void IsValidNumericInput_SecondDecimalSeparator_ReturnsFalse()
    {
        // Arrange
        var currentText = $"123{NumberFormatHelper.DecimalSeparator}45";

        // Act
        var result = NumberFormatHelper.IsValidNumericInput(
            NumberFormatHelper.DecimalSeparator,
            currentText,
            currentText.Length,
            "",
            allowNegative: true,
            allowDecimal: true);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsValidNumericInput returns false for letter input.
    /// </summary>
    [Fact]
    public void IsValidNumericInput_Letter_ReturnsFalse()
    {
        // Act
        var result = NumberFormatHelper.IsValidNumericInput("a", "123", 3, "", true, true);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsValidNumericInput returns true for scientific notation 'e' when allowed.
    /// </summary>
    [Fact]
    public void IsValidNumericInput_ScientificE_WhenAllowed_ReturnsTrue()
    {
        // Act
        var result = NumberFormatHelper.IsValidNumericInput(
            "e",
            "123",
            3,
            "",
            allowNegative: true,
            allowDecimal: true,
            allowScientific: true);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region IsValidPartialNumber Tests

    /// <summary>
    /// Tests that IsValidPartialNumber returns true for empty string.
    /// </summary>
    [Fact]
    public void IsValidPartialNumber_EmptyString_ReturnsTrue()
    {
        // Act
        var result = NumberFormatHelper.IsValidPartialNumber("");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidPartialNumber returns true for null string.
    /// </summary>
    [Fact]
    public void IsValidPartialNumber_NullString_ReturnsTrue()
    {
        // Act
        var result = NumberFormatHelper.IsValidPartialNumber(null!);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidPartialNumber returns true for just negative sign.
    /// </summary>
    [Fact]
    public void IsValidPartialNumber_JustNegativeSign_ReturnsTrue()
    {
        // Act
        var result = NumberFormatHelper.IsValidPartialNumber(NumberFormatHelper.NegativeSign);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidPartialNumber returns true for just decimal separator.
    /// </summary>
    [Fact]
    public void IsValidPartialNumber_JustDecimalSeparator_ReturnsTrue()
    {
        // Act
        var result = NumberFormatHelper.IsValidPartialNumber(NumberFormatHelper.DecimalSeparator);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidPartialNumber returns true for negative sign and decimal separator.
    /// </summary>
    [Fact]
    public void IsValidPartialNumber_NegativeSignAndDecimalSeparator_ReturnsTrue()
    {
        // Act
        var result = NumberFormatHelper.IsValidPartialNumber(
            NumberFormatHelper.NegativeSign + NumberFormatHelper.DecimalSeparator);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidPartialNumber returns true for partial scientific notation.
    /// </summary>
    [Fact]
    public void IsValidPartialNumber_PartialScientificNotation_ReturnsTrue()
    {
        // Act
        var result = NumberFormatHelper.IsValidPartialNumber("1e");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsValidPartialNumber returns true for partial scientific notation with sign.
    /// </summary>
    [Fact]
    public void IsValidPartialNumber_PartialScientificNotationWithSign_ReturnsTrue()
    {
        // Act
        var result1 = NumberFormatHelper.IsValidPartialNumber("1e-");
        var result2 = NumberFormatHelper.IsValidPartialNumber("1e+");

        // Assert
        Assert.True(result1);
        Assert.True(result2);
    }

    #endregion

    #region IsValidPartialScientificNotation Tests

    /// <summary>
    /// Tests that IsValidPartialScientificNotation returns expected results for various inputs.
    /// </summary>
    /// <param name="input">The input string to test.</param>
    /// <param name="expected">The expected result.</param>
    [Theory]
    [InlineData("1e", true)]
    [InlineData("1E", true)]
    [InlineData("1e-", true)]
    [InlineData("1E+", true)]
    [InlineData("1.5e", true)]
    [InlineData("-2e-", true)]
    [InlineData("e", false)]      // No mantissa
    [InlineData("e1", false)]     // No mantissa
    [InlineData("", false)]
    public void IsValidPartialScientificNotation_VariousInputs_ReturnsExpected(string input, bool expected)
    {
        // Act
        var result = NumberFormatHelper.IsValidPartialScientificNotation(input);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsInfinityText Tests

    /// <summary>
    /// Tests that IsInfinityText returns true for valid infinity text.
    /// </summary>
    /// <param name="input">The input string to test.</param>
    /// <param name="expected">The expected result.</param>
    [Theory]
    [InlineData("inf", true)]
    [InlineData("Inf", true)]
    [InlineData("infinity", true)]
    [InlineData("Infinity", true)]
    [InlineData("+inf", true)]
    [InlineData("+Inf", true)]
    [InlineData("-inf", true)]
    [InlineData("-Inf", true)]
    [InlineData("-infinity", true)]
    [InlineData("-Infinity", true)]
    public void IsInfinityText_ValidInfinityText_ReturnsTrue(string input, bool expected)
    {
        // Act
        var result = NumberFormatHelper.IsInfinityText(input);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that IsInfinityText returns false for invalid infinity text.
    /// </summary>
    /// <param name="input">The input string to test.</param>
    [Theory]
    [InlineData("")]
    [InlineData("infin")]
    [InlineData("infinite")]
    [InlineData("123")]
    [InlineData("abc")]
    public void IsInfinityText_InvalidInfinityText_ReturnsFalse(string? input)
    {
        // Act
        var result = NumberFormatHelper.IsInfinityText(input!);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsInfinityText returns false for null input.
    /// </summary>
    [Fact]
    public void IsInfinityText_Null_ReturnsFalse()
    {
        // Act
        var result = NumberFormatHelper.IsInfinityText(null!);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsPartialInfinityText Tests

    /// <summary>
    /// Tests that IsPartialInfinityText returns true for valid partial infinity text.
    /// </summary>
    /// <param name="input">The input string to test.</param>
    /// <param name="expected">The expected result.</param>
    [Theory]
    [InlineData("i", true)]
    [InlineData("in", true)]
    [InlineData("inf", true)]
    [InlineData("infi", true)]
    [InlineData("infin", true)]
    [InlineData("-i", true)]
    [InlineData("-inf", true)]
    [InlineData("+i", true)]
    [InlineData("+inf", true)]
    public void IsPartialInfinityText_ValidPartialInfinity_ReturnsTrue(string input, bool expected)
    {
        // Act
        var result = NumberFormatHelper.IsPartialInfinityText(input);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that IsPartialInfinityText returns false for invalid partial infinity text.
    /// </summary>
    /// <param name="input">The input string to test.</param>
    [Theory]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("ix")]
    [InlineData("123")]
    public void IsPartialInfinityText_InvalidPartialInfinity_ReturnsFalse(string? input)
    {
        // Act
        var result = NumberFormatHelper.IsPartialInfinityText(input!);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsNumericDigit and IsAllDigits Tests

    /// <summary>
    /// Tests that IsNumericDigit returns expected results for various characters.
    /// </summary>
    /// <param name="input">The character to test.</param>
    /// <param name="expected">The expected result.</param>
    [Theory]
    [InlineData('0', true)]
    [InlineData('5', true)]
    [InlineData('9', true)]
    [InlineData('a', false)]
    [InlineData('!', false)]
    [InlineData(' ', false)]
    public void IsNumericDigit_VariousChars_ReturnsExpected(char input, bool expected)
    {
        // Act
        var result = NumberFormatHelper.IsNumericDigit(input);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that IsAllDigits returns expected results for various strings.
    /// </summary>
    /// <param name="input">The string to test.</param>
    /// <param name="expected">The expected result.</param>
    [Theory]
    [InlineData("123", true)]
    [InlineData("0", true)]
    [InlineData("000123", true)]
    [InlineData("12a3", false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    public void IsAllDigits_VariousStrings_ReturnsExpected(string input, bool expected)
    {
        // Act
        var result = NumberFormatHelper.IsAllDigits(input);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that IsAllDigits returns false for null input.
    /// </summary>
    [Fact]
    public void IsAllDigits_Null_ReturnsFalse()
    {
        // Act
        var result = NumberFormatHelper.IsAllDigits(null!);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetDigitValue Tests

    /// <summary>
    /// Tests that GetDigitValue returns correct values for valid digits.
    /// </summary>
    /// <param name="input">The digit character.</param>
    /// <param name="expected">The expected numeric value.</param>
    [Theory]
    [InlineData('0', 0)]
    [InlineData('1', 1)]
    [InlineData('5', 5)]
    [InlineData('9', 9)]
    public void GetDigitValue_ValidDigit_ReturnsValue(char input, int expected)
    {
        // Act
        var result = NumberFormatHelper.GetDigitValue(input);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that GetDigitValue returns -1 for non-digit characters.
    /// </summary>
    /// <param name="input">The non-digit character.</param>
    [Theory]
    [InlineData('a')]
    [InlineData('!')]
    [InlineData(' ')]
    public void GetDigitValue_NonDigit_ReturnsNegativeOne(char input)
    {
        // Act
        var result = NumberFormatHelper.GetDigitValue(input);

        // Assert
        Assert.Equal(-1, result);
    }

    #endregion

    #region NormalizeDigits Tests

    /// <summary>
    /// Tests that NormalizeDigits returns unchanged for Western digits.
    /// </summary>
    [Fact]
    public void NormalizeDigits_WesternDigits_ReturnsUnchanged()
    {
        // Arrange
        var input = "0123456789";

        // Act
        var result = NumberFormatHelper.NormalizeDigits(input);

        // Assert
        Assert.Equal(input, result);
    }

    /// <summary>
    /// Tests that NormalizeDigits returns empty for empty string.
    /// </summary>
    [Fact]
    public void NormalizeDigits_EmptyString_ReturnsEmpty()
    {
        // Act
        var result = NumberFormatHelper.NormalizeDigits("");

        // Assert
        Assert.Equal("", result);
    }

    /// <summary>
    /// Tests that NormalizeDigits returns null for null input.
    /// </summary>
    [Fact]
    public void NormalizeDigits_Null_ReturnsNull()
    {
        // Act
        var result = NumberFormatHelper.NormalizeDigits(null!);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that NormalizeDigits normalizes only digits in mixed content.
    /// </summary>
    [Fact]
    public void NormalizeDigits_MixedContent_NormalizesOnlyDigits()
    {
        // Arrange
        var input = "abc123xyz";

        // Act
        var result = NumberFormatHelper.NormalizeDigits(input);

        // Assert - Non-digit characters should remain unchanged
        Assert.Equal("abc123xyz", result);
    }

    #endregion

    #region RTL Support Tests

    /// <summary>
    /// Tests that IsRightToLeft returns the current culture's direction.
    /// </summary>
    [Fact]
    public void IsRightToLeft_ReturnsCurrentCultureDirection()
    {
        // Assert
        var expected = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
        Assert.Equal(expected, NumberFormatHelper.IsRightToLeft);
    }

    /// <summary>
    /// Tests that CurrentFlowDirection returns the correct flow direction.
    /// </summary>
    [Fact]
    public void CurrentFlowDirection_ReturnsCorrectDirection()
    {
        // Assert
        if (NumberFormatHelper.IsRightToLeft)
        {
            Assert.Equal(System.Windows.FlowDirection.RightToLeft, NumberFormatHelper.CurrentFlowDirection);
        }
        else
        {
            Assert.Equal(System.Windows.FlowDirection.LeftToRight, NumberFormatHelper.CurrentFlowDirection);
        }
    }

    /// <summary>
    /// Tests that NegativeSignIsPrefix returns a boolean value without throwing.
    /// </summary>
    [Fact]
    public void NegativeSignIsPrefix_ReturnsBasedOnCulture()
    {
        // Assert - Should match the expected pattern from the current culture
        var result = NumberFormatHelper.NegativeSignIsPrefix;
        int pattern = CultureInfo.CurrentCulture.NumberFormat.NumberNegativePattern;
        bool expected = pattern == 1 || pattern == 2;
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsNegativeSign and IsPositiveSign Tests

    /// <summary>
    /// Tests that IsNegativeSign returns true for the negative sign.
    /// </summary>
    [Fact]
    public void IsNegativeSign_WithNegativeSign_ReturnsTrue()
    {
        // Arrange
        var negativeSign = NumberFormatHelper.NegativeSign;

        // Act
        var result = NumberFormatHelper.IsNegativeSign(negativeSign);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsNegativeSign returns false for other text.
    /// </summary>
    [Fact]
    public void IsNegativeSign_WithOtherText_ReturnsFalse()
    {
        // Act
        var result = NumberFormatHelper.IsNegativeSign("xyz");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsPositiveSign returns true for the positive sign.
    /// </summary>
    [Fact]
    public void IsPositiveSign_WithPositiveSign_ReturnsTrue()
    {
        // Arrange
        var positiveSign = NumberFormatHelper.PositiveSign;

        // Act
        var result = NumberFormatHelper.IsPositiveSign(positiveSign);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsPositiveSign returns false for other text.
    /// </summary>
    [Fact]
    public void IsPositiveSign_WithOtherText_ReturnsFalse()
    {
        // Act
        var result = NumberFormatHelper.IsPositiveSign("xyz");

        // Assert
        Assert.False(result);
    }

    #endregion
}
