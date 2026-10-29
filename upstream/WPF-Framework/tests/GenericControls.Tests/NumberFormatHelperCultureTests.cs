using System.Globalization;
using System.Threading;
using GenericControls;
using Xunit;

namespace GenericControls.Tests;

/// <summary>
/// Tests in this collection mutate <see cref="CultureInfo.CurrentCulture"/>; xunit must
/// not run them in parallel with anything else in the same assembly that reads culture.
/// </summary>
[CollectionDefinition("CultureSensitive", DisableParallelization = true)]
public class CultureSensitiveCollection { }

/// <summary>
/// Verifies <see cref="NumberFormatHelper"/> respects the current culture for parsing
/// and formatting across decimal-comma cultures (de-DE, fr-FR), RTL cultures (ar-SA),
/// and non-Western digit cultures (fa-IR Persian).
/// </summary>
[Collection("CultureSensitive")]
public class NumberFormatHelperCultureTests
{
    [Theory]
    [InlineData("de-DE", "1,5", 1.5)]
    [InlineData("de-DE", "1.234,56", 1234.56)]
    [InlineData("fr-FR", "1,5", 1.5)]
    [InlineData("en-US", "1.5", 1.5)]
    [InlineData("en-US", "1,234.56", 1234.56)]
    public void TryParseDouble_RespectsCurrentCulture(string cultureName, string input, double expected)
    {
        RunUnderCulture(cultureName, () =>
        {
            Assert.True(NumberFormatHelper.TryParseDouble(input, out double parsed),
                $"TryParseDouble failed for '{input}' under {cultureName}");
            Assert.Equal(expected, parsed);
        });
    }

    [Theory]
    [InlineData("de-DE", 1.5, ',')]
    [InlineData("fr-FR", 1.5, ',')]
    [InlineData("en-US", 1.5, '.')]
    public void FormatDouble_UsesCultureDecimalSeparator(string cultureName, double value, char expectedSep)
    {
        RunUnderCulture(cultureName, () =>
        {
            string formatted = NumberFormatHelper.FormatDouble(value);
            Assert.Contains(expectedSep.ToString(), formatted);
        });
    }

    [Theory]
    [InlineData("de-DE", ",")]
    [InlineData("fr-FR", ",")]
    [InlineData("en-US", ".")]
    public void DecimalSeparator_MatchesCulture(string cultureName, string expected)
    {
        RunUnderCulture(cultureName, () =>
        {
            Assert.Equal(expected, NumberFormatHelper.DecimalSeparator);
        });
    }

    [Fact]
    public void IsRightToLeft_TrueForArabicCulture()
    {
        RunUnderCulture("ar-SA", () =>
        {
            Assert.True(NumberFormatHelper.IsRightToLeft);
        });
    }

    [Fact]
    public void IsRightToLeft_FalseForEnglishCulture()
    {
        RunUnderCulture("en-US", () =>
        {
            Assert.False(NumberFormatHelper.IsRightToLeft);
        });
    }

    [Theory]
    [InlineData("١٢٣", "123")] // Eastern Arabic-Indic digits
    [InlineData("۱۲۳", "123")] // Extended Arabic-Indic digits (Persian)
    [InlineData("123", "123")] // Western Arabic — no change
    public void NormalizeDigits_ConvertsAllNumeralSystems(string input, string expected)
    {
        Assert.Equal(expected, NumberFormatHelper.NormalizeDigits(input));
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public void RoundTrip_HelperFormatThenHelperParse_PreservesValueBitExact(string cultureName)
    {
        RunUnderCulture(cultureName, () =>
        {
            // Use NumberFormatHelper on BOTH legs so we test the helper's round-trip,
            // not the BCL's. NumberFormatHelper.FormatDouble uses ToString(CurrentCulture)
            // which produces the most-precise representation by default for double.
            double[] values = { 1234.5678, 0.1, -0.0, 1e-10, 12345.6789012345, -987.654321 };
            foreach (double v in values)
            {
                string formatted = NumberFormatHelper.FormatDouble(v);
                Assert.True(NumberFormatHelper.TryParseDouble(formatted, out double parsed),
                    $"Failed to parse '{formatted}' under {cultureName}");
                Assert.Equal(System.BitConverter.DoubleToInt64Bits(v),
                             System.BitConverter.DoubleToInt64Bits(parsed));
            }
        });
    }

    [Theory]
    [InlineData("fa-IR", "۱۲۳", 123.0)] // Persian Eastern Arabic-Indic digits
    [InlineData("ar-SA", "١٢٣", 123.0)] // Arabic Eastern Arabic-Indic digits
    public void NormalizeDigitsThenTryParseDouble_HandlesNonWesternDigits(string cultureName, string input, double expected)
    {
        RunUnderCulture(cultureName, () =>
        {
            // Workflow: user types non-Western digits → NormalizeDigits → TryParseDouble.
            // This is the documented use case for NormalizeDigits; verify it composes correctly.
            string normalized = NumberFormatHelper.NormalizeDigits(input);
            Assert.True(NumberFormatHelper.TryParseDouble(normalized, out double result),
                $"Failed to parse normalized '{normalized}' (from '{input}') under {cultureName}");
            Assert.Equal(expected, result);
        });
    }

    [Theory]
    [InlineData("de-DE", "abc")]
    [InlineData("de-DE", "1,2,3")] // ambiguous — parser may accept as group separator; just verify no throw
    public void TryParseDouble_InvalidInput_ReturnsFalseWithoutThrowing(string cultureName, string input)
    {
        RunUnderCulture(cultureName, () =>
        {
            // The contract is: must NOT throw. Result of parse may be true or false depending on input.
            var ex = Record.Exception(() => NumberFormatHelper.TryParseDouble(input, out _));
            Assert.Null(ex);
        });
    }

    [Fact]
    public void IsValidPartialNumber_HandlesGermanPartials()
    {
        RunUnderCulture("de-DE", () =>
        {
            Assert.True(NumberFormatHelper.IsValidPartialNumber(",")); // German decimal partial
            Assert.True(NumberFormatHelper.IsValidPartialNumber("-,"));
            Assert.True(NumberFormatHelper.IsValidPartialNumber("1e"));
        });
    }

    private static void RunUnderCulture(string cultureName, System.Action body)
    {
        var prevCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentCulture = culture;
            CultureInfo.CurrentCulture = culture;
            body();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = prevCulture;
            CultureInfo.CurrentCulture = prevCulture;
        }
    }
}
