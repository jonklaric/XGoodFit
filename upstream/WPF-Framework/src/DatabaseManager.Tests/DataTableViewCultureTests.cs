using System;
using System.Globalization;
using System.Threading;
using DatabaseManager;
using Xunit;

namespace DatabaseManager.Tests
{
    /// <summary>
    /// Verifies the dual-culture fallback in <see cref="DataTableView.ConvertToColumnType"/>.
    /// </summary>
    /// <remarks>
    /// The fallback tries <see cref="CultureInfo.CurrentCulture"/> first (matching how the
    /// user typed/pasted data in their own locale), then falls back to
    /// <see cref="CultureInfo.InvariantCulture"/> for cross-machine data. The fallback only
    /// fires for strings the CurrentCulture parser REJECTS — strings that succeed-but-misparse
    /// under CurrentCulture (e.g., "1.5" on de-DE, where "." is thousands separator) are NOT
    /// recovered. This is a documented limitation of the dual-culture pattern.
    /// </remarks>
    [Collection("CultureSensitive")]
    public class DataTableViewCultureTests
    {
        [Fact]
        public void ConvertToColumnType_Double_InvariantInfinity_FallsThroughToInvariantUnderGermanCulture()
        {
            // "Infinity" is rejected by de-DE parser (de-DE uses "∞") but accepted by Invariant.
            // Without the dual-culture fallback, ConvertToColumnType would return false here.
            RunUnderCulture("de-DE", () =>
            {
                object value = "Infinity";
                Assert.True(DataTableView.ConvertToColumnType(typeof(double), ref value));
                Assert.True(double.IsPositiveInfinity((double)value));
            });
        }

        [Fact]
        public void ConvertToColumnType_Double_NegativeInvariantInfinity_FallsThroughToInvariantUnderGermanCulture()
        {
            RunUnderCulture("de-DE", () =>
            {
                object value = "-Infinity";
                Assert.True(DataTableView.ConvertToColumnType(typeof(double), ref value));
                Assert.True(double.IsNegativeInfinity((double)value));
            });
        }

        [Fact]
        public void ConvertToColumnType_Double_USFormatWithThousandsSeparator_FallsThroughToInvariantUnderGermanCulture()
        {
            // "1,234.56" is rejected by de-DE (comma=decimal, can't have a period after) but
            // accepted by Invariant (comma=thousands, period=decimal).
            RunUnderCulture("de-DE", () =>
            {
                object value = "1,234.56";
                Assert.True(DataTableView.ConvertToColumnType(typeof(double), ref value));
                Assert.Equal(1234.56, (double)value);
            });
        }

        [Fact]
        public void ConvertToColumnType_Double_GermanFormat_ParsedViaCurrentCultureUnderGermanCulture()
        {
            // "1,5" parses as 1.5 under de-DE (comma=decimal). This validates the
            // CurrentCulture leg of the fallback (no fall-through needed).
            RunUnderCulture("de-DE", () =>
            {
                object value = "1,5";
                Assert.True(DataTableView.ConvertToColumnType(typeof(double), ref value));
                Assert.Equal(1.5, (double)value);
            });
        }

        [Fact]
        public void ConvertToColumnType_Int_USFormatWithMultipleThousands_FallsThroughToInvariantUnderGermanCulture()
        {
            // Non-double numeric path: "1,234,567" is rejected by de-DE (commas as decimal
            // can only appear once), accepted by Invariant (commas as thousands separator).
            // Single-comma cases like "1,234" succeed under both cultures with DIFFERENT
            // values (1.234 vs 1234) — using multi-comma forces the fall-through path.
            RunUnderCulture("de-DE", () =>
            {
                object value = "1,234,567";
                Assert.True(DataTableView.ConvertToColumnType(typeof(int), ref value));
                Assert.Equal(1234567, (int)value);
            });
        }

        [Fact]
        public void ConvertToColumnType_Long_InvariantInfinity_RejectedDueToOverflowConversion()
        {
            // Even though "Infinity" parses via Invariant fallback, Convert.ToInt64 of
            // double.PositiveInfinity throws OverflowException → ConvertToColumnType catches
            // and returns false. Documents the chain's behavior.
            RunUnderCulture("de-DE", () =>
            {
                object value = "Infinity";
                Assert.False(DataTableView.ConvertToColumnType(typeof(long), ref value));
            });
        }

        [Fact]
        public void ConvertToColumnType_Float_USFormatWithThousandsAndDecimal_FallsThroughToInvariantUnderGermanCulture()
        {
            // "1,234.56" rejected by de-DE (period is thousands; comma already used as decimal)
            // → falls through to Invariant which parses correctly as 1234.56.
            RunUnderCulture("de-DE", () =>
            {
                object value = "1,234.56";
                Assert.True(DataTableView.ConvertToColumnType(typeof(float), ref value));
                Assert.Equal(1234.56f, (float)value);
            });
        }

        [Fact]
        public void ConvertToColumnType_Double_Garbage_ReturnsFalseUnderAllCultures()
        {
            foreach (string culture in new[] { "en-US", "de-DE", "fr-FR" })
            {
                RunUnderCulture(culture, () =>
                {
                    object value = "not a number";
                    Assert.False(DataTableView.ConvertToColumnType(typeof(double), ref value),
                        $"Garbage string should not parse under {culture}");
                });
            }
        }

        [Fact]
        public void ConvertToColumnType_Double_NullValue_ReturnsTrueAsDBNull()
        {
            // Sanity check: pre-existing null-handling not regressed by helper introduction.
            object? value = null;
            Assert.True(DataTableView.ConvertToColumnType(typeof(double), ref value!));
            Assert.Equal(DBNull.Value, value);
        }

        private static void RunUnderCulture(string cultureName, Action body)
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
}
