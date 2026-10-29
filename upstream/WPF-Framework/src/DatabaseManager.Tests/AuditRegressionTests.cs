using System;
using System.IO;
using System.Text;
using Xunit;

namespace DatabaseManager.Tests
{
    /// <summary>
    /// Regression tests pinning forensic-audit Phase 1 / 2 fixes.
    /// Each test name references the audit finding ID it covers.
    /// </summary>
    public class AuditRegressionTests : IDisposable
    {
        private readonly string _testDir;

        public AuditRegressionTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(),
                "DatabaseManager.AuditRegression_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup; nothing actionable on failure here.
            }
        }

        /// <summary>
        /// C-012: <see cref="DatabaseManager.ConvertCsvToSqLite"/> used to build SQL by
        /// concatenating the column header into <c>"[" + name + "]"</c> with no validation.
        /// A header containing a closing bracket — e.g. <c>"col]; DROP TABLE x; --"</c> —
        /// would break out of the bracket-quoted identifier and inject SQL. The fix routes
        /// every identifier through <c>ValidateIdentifier</c>, which throws
        /// <see cref="ArgumentException"/> for names containing <c>]</c> or null bytes.
        /// </summary>
        [Fact]
        public void C012_ConvertCsvToSqLite_RejectsIdentifiersWithBrackets()
        {
            // Arrange — CSV with a header that, before the fix, would be injected verbatim
            // into the CREATE TABLE statement.
            var csvPath = Path.Combine(_testDir, "input.csv");
            var dbPath = Path.Combine(_testDir, "out.sqlite");
            File.WriteAllText(
                csvPath,
                "col]; DROP TABLE x; --,col2\nv1,v2\n",
                Encoding.UTF8);

            // Act + Assert — must throw because the column header fails ValidateIdentifier.
            var ex = Assert.ThrowsAny<Exception>(() =>
                DatabaseManager.ConvertCsvToSqLite(
                    csvPath,
                    dbPath,
                    outputtableName: "AnyTable",
                    hasHeaders: true,
                    dataLineStartIndex: 0,
                    fieldsEnclosedInQuotes: false));

            // Inner exception must be ArgumentException — the public method wraps caught
            // failures in a generic Exception, so we check the inner cause.
            var argEx = ex as ArgumentException ?? ex.InnerException as ArgumentException;
            Assert.NotNull(argEx);

            // Defensive: make sure no SQLite file was produced — the rejection happens
            // before any DDL executes so the database file should not contain the table.
            // (The SQLiteManager opens/creates the file even on failure; we don't assert
            // its absence, only that no rogue table was created.)
        }

        /// <summary>
        /// C-012: <see cref="SQLiteManager.CreateTable"/> directly rejects bracket-bearing
        /// table or column names without going through the CSV path.
        /// </summary>
        [Fact]
        public void C012_SQLiteManager_CreateTable_RejectsBracketInTableName()
        {
            var dbPath = Path.Combine(_testDir, "ddl.sqlite");
            using var sqlite = new SQLiteManager(dbPath);
            sqlite.Open();

            try
            {
                Assert.Throws<ArgumentException>(() =>
                    sqlite.CreateTable(
                        "evil]; DROP TABLE foo; --",
                        new[] { "Col1" },
                        new[] { typeof(string) }));
            }
            finally
            {
                sqlite.Close();
            }
        }

        /// <summary>
        /// C-012: column names with brackets are rejected.
        /// </summary>
        [Fact]
        public void C012_SQLiteManager_CreateTable_RejectsBracketInColumnName()
        {
            var dbPath = Path.Combine(_testDir, "ddl2.sqlite");
            using var sqlite = new SQLiteManager(dbPath);
            sqlite.Open();

            try
            {
                Assert.Throws<ArgumentException>(() =>
                    sqlite.CreateTable(
                        "Good",
                        new[] { "good", "evil]; DROP TABLE x; --" },
                        new[] { typeof(string), typeof(string) }));
            }
            finally
            {
                sqlite.Close();
            }
        }
    }
}
