using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using DatabaseManager;
using Xunit;

namespace DatabaseManager.Tests
{
    /// <summary>
    /// Tests in this collection mutate <see cref="CultureInfo.CurrentCulture"/>; xunit must
    /// not run them in parallel with anything else in the same assembly that reads culture.
    /// </summary>
    [CollectionDefinition("CultureSensitive", DisableParallelization = true)]
    public class CultureSensitiveCollection { }

    /// <summary>
    /// Verifies DBF file output uses culture-invariant numeric formatting.
    /// </summary>
    /// <remarks>
    /// DBF is a portable binary format. Floating-point values are written as ASCII text
    /// inside the file (e.g. "1.23456789012e+003"). On a German machine without invariant
    /// formatting, this becomes "1,23456789012e+003" — unreadable by Excel, ArcGIS, or any
    /// other DBF consumer.
    /// </remarks>
    [Collection("CultureSensitive")]
    public class DBFCultureRoundTripTests : IDisposable
    {
        private readonly string _testDbfPath;

        public DBFCultureRoundTripTests()
        {
            _testDbfPath = Path.Combine(Path.GetTempPath(), $"culture_test_{Guid.NewGuid():N}.dbf");
        }

        public void Dispose()
        {
            if (File.Exists(_testDbfPath))
            {
                try { File.Delete(_testDbfPath); }
                catch { /* ignore cleanup errors */ }
            }
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("fr-FR")]
        [InlineData("en-US")]
        public void CreateDbf_NumericColumn_FileUsesPeriodAsDecimalSeparator(string cultureName)
        {
            RunUnderCulture(cultureName, () =>
            {
                var dt = new DataTable("Test");
                dt.Columns.Add("Value", typeof(double));
                dt.Rows.Add(1.5);
                dt.Rows.Add(1234.5678);
                dt.Rows.Add(-987.654321);

                DbfReader.CreateDbf(_testDbfPath, dt);

                Assert.True(File.Exists(_testDbfPath));
                byte[] bytes = File.ReadAllBytes(_testDbfPath);
                string asAscii = Encoding.ASCII.GetString(bytes);

                // The numeric column data is encoded as ASCII text inside the DBF.
                // Under any culture, it must use '.' (period) — never ',' for decimal sep.
                // We don't search for the literal "1.5" string because DBF zero-pads numbers
                // to fixed width; instead verify no German-style comma decimals leak through.
                // Look for "0.0" or similar patterns; ensure no "0,0" floating-point patterns.
                Assert.DoesNotContain("0,0000", asAscii);
                Assert.DoesNotContain("1,500000", asAscii);
                Assert.DoesNotContain("1,234567", asAscii);
            });
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void CreateDbf_RoundTrip_PreservesDoubleValues(string cultureName)
        {
            double[] expected = { 1.5, 1234.5678, -987.654321, 0.000001, 1.23456789e10 };

            RunUnderCulture(cultureName, () =>
            {
                var dt = new DataTable("Test");
                dt.Columns.Add("Value", typeof(double));
                foreach (double v in expected)
                {
                    dt.Rows.Add(v);
                }

                DbfReader.CreateDbf(_testDbfPath, dt);

                var reader = new DbfReader(_testDbfPath);
                try
                {
                    reader.Open();
                    var view = reader.GetTableManager(Path.GetFileNameWithoutExtension(_testDbfPath));

                    // DBF's "0.00000000000e+000" format only retains 11 digits of mantissa precision,
                    // so use approximate equality rather than bit-exact.
                    Assert.Equal(expected.Length, view.NumberOfRows);
                    for (int i = 0; i < expected.Length; i++)
                    {
                        double actual = Convert.ToDouble(view.GetCell(0, i), CultureInfo.InvariantCulture);
                        Assert.Equal(expected[i], actual, precision: 8);
                    }
                }
                finally
                {
                    reader.Close();
                }
            });
        }

        [Fact]
        public void CreateDbf_FileWrittenInGerman_ReadableInUS()
        {
            double[] expected = { 1234.5678, -42.0, 0.001 };

            // Write under de-DE
            RunUnderCulture("de-DE", () =>
            {
                var dt = new DataTable("Test");
                dt.Columns.Add("Value", typeof(double));
                foreach (double v in expected) dt.Rows.Add(v);
                DbfReader.CreateDbf(_testDbfPath, dt);
            });

            // Read under en-US — must succeed
            RunUnderCulture("en-US", () =>
            {
                var reader = new DbfReader(_testDbfPath);
                try
                {
                    reader.Open();
                    var view = reader.GetTableManager(Path.GetFileNameWithoutExtension(_testDbfPath));
                    for (int i = 0; i < expected.Length; i++)
                    {
                        double actual = Convert.ToDouble(view.GetCell(0, i), CultureInfo.InvariantCulture);
                        Assert.Equal(expected[i], actual, precision: 8);
                    }
                }
                finally
                {
                    reader.Close();
                }
            });
        }

        [Fact]
        public void CreateDbf_FloatColumn_FileUsesInvariantFormat_UnderGermanCulture()
        {
            RunUnderCulture("de-DE", () =>
            {
                var dt = new DataTable("Test");
                dt.Columns.Add("F", typeof(float));
                dt.Rows.Add(1.5f);
                dt.Rows.Add(2.5f);

                DbfReader.CreateDbf(_testDbfPath, dt);

                byte[] bytes = File.ReadAllBytes(_testDbfPath);
                string asAscii = Encoding.ASCII.GetString(bytes);

                // No comma-decimal floating-point patterns
                Assert.DoesNotContain("1,500", asAscii);
                Assert.DoesNotContain("2,500", asAscii);
            });
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
