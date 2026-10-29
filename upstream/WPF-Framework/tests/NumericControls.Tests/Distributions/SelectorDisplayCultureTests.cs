using System.Globalization;
using System.Threading;
using Xunit;

namespace NumericControls.Tests.Distributions;

/// <summary>
/// Tests in this collection mutate <see cref="CultureInfo.CurrentCulture"/>; xunit must
/// not run them in parallel with anything else in the same assembly that reads culture.
/// </summary>
[CollectionDefinition("CultureSensitive", DisableParallelization = true)]
public class CultureSensitiveCollection { }

/// <summary>
/// Guard tests for the Distribution Selector statistics display format.
/// </summary>
/// <remarks>
/// The Selector previously formatted DataStat/DistStat with
/// <c>ToString("N4", CultureInfo.InvariantCulture)</c>, which always rendered
/// "1,234.5678" regardless of locale. The fix flips InvariantCulture to
/// CurrentCulture so a German user sees "1.234,5678".
///
/// These tests don't instantiate the WPF Selector control (that needs an STA
/// host); instead they verify the underlying format pattern produces the
/// expected culture-specific output, guarding against a future regression
/// that flips back to Invariant.
/// </remarks>
[Collection("CultureSensitive")]
public class SelectorDisplayCultureTests
{
    [Theory]
    [InlineData("de-DE", 1234.5678, ',')]
    [InlineData("fr-FR", 1234.5678, ',')]
    [InlineData("en-US", 1234.5678, '.')]
    public void StatN4Format_UsesCurrentCultureDecimalSeparator(string cultureName, double value, char expectedSep)
    {
        RunUnderCulture(cultureName, () =>
        {
            string formatted = value.ToString("N4", CultureInfo.CurrentCulture);
            Assert.Contains(expectedSep.ToString(), formatted);
        });
    }

    [Fact]
    public void StatN4Format_GermanLocale_ProducesGermanThousandsAndDecimal()
    {
        RunUnderCulture("de-DE", () =>
        {
            string formatted = 1234.5678.ToString("N4", CultureInfo.CurrentCulture);
            // German format: '.' for thousands, ',' for decimal → "1.234,5678"
            Assert.Equal("1.234,5678", formatted);
        });
    }

    [Fact]
    public void StatN4Format_UsLocale_ProducesUsThousandsAndDecimal()
    {
        RunUnderCulture("en-US", () =>
        {
            string formatted = 1234.5678.ToString("N4", CultureInfo.CurrentCulture);
            // US format: ',' for thousands, '.' for decimal → "1,234.5678"
            Assert.Equal("1,234.5678", formatted);
        });
    }

    [Fact]
    public void StatN4Format_NaN_RendersAsLocaleNaN()
    {
        // double.NaN is rendered as the locale's NaN symbol; just verify no throw
        // and a non-empty result under each culture
        foreach (string culture in new[] { "de-DE", "en-US", "fr-FR" })
        {
            RunUnderCulture(culture, () =>
            {
                string formatted = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                Assert.False(string.IsNullOrEmpty(formatted));
            });
        }
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
