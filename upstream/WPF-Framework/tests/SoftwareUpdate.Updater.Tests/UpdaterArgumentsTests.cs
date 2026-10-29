using Xunit;
using SoftwareUpdate.Updater;

namespace SoftwareUpdate.Updater.Tests
{
    /// <summary>
    /// Test suite for the UpdaterArguments class, validating command-line argument parsing and validation functionality.
    /// </summary>
    public class UpdaterArgumentsTests
    {
        #region Test Helpers

        /// <summary>
        /// Helper method to parse command-line arguments into an UpdaterArguments object.
        /// </summary>
        /// <param name="args">The array of command-line arguments to parse.</param>
        /// <returns>A parsed UpdaterArguments instance.</returns>
        private static UpdaterArguments Parse(string[] args)
        {
            return UpdaterArguments.Parse(args);
        }

        /// <summary>
        /// Helper method to validate an UpdaterArguments object.
        /// </summary>
        /// <param name="args">The UpdaterArguments instance to validate.</param>
        /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
        private static void Validate(UpdaterArguments args)
        {
            args.Validate();
        }

        #endregion

        #region Parse Tests

        /// <summary>
        /// Verifies that the --pid argument is correctly parsed and assigned to ProcessId.
        /// </summary>
        [Fact]
        public void Parse_ProcessId()
        {
            var args = Parse(new[] { "--pid", "1234" });

            Assert.Equal(1234, args.ProcessId);
        }

        /// <summary>
        /// Verifies that the --zip argument is correctly parsed and assigned to ZipPath.
        /// </summary>
        [Fact]
        public void Parse_ZipPath()
        {
            var args = Parse(new[] { "--zip", @"C:\path\to\update.zip" });

            Assert.Equal(@"C:\path\to\update.zip", args.ZipPath);
        }

        /// <summary>
        /// Verifies that quoted path arguments are correctly unquoted during parsing.
        /// </summary>
        [Fact]
        public void Parse_QuotedZipPath()
        {
            var args = Parse(new[] { "--zip", "\"C:\\path with spaces\\update.zip\"" });

            Assert.Equal(@"C:\path with spaces\update.zip", args.ZipPath);
        }

        /// <summary>
        /// Verifies that the --target argument is correctly parsed and assigned to TargetDirectory.
        /// </summary>
        [Fact]
        public void Parse_TargetDirectory()
        {
            var args = Parse(new[] { "--target", @"C:\Program Files\MyApp" });

            Assert.Equal(@"C:\Program Files\MyApp", args.TargetDirectory);
        }

        /// <summary>
        /// Verifies that the --exe argument is correctly parsed and assigned to MainExecutable.
        /// </summary>
        [Fact]
        public void Parse_MainExecutable()
        {
            var args = Parse(new[] { "--exe", "MyApp.exe" });

            Assert.Equal("MyApp.exe", args.MainExecutable);
        }

        /// <summary>
        /// Verifies that the --backup flag sets CreateBackup to true when present.
        /// </summary>
        [Fact]
        public void Parse_BackupFlag()
        {
            var args = Parse(new[] { "--backup" });

            Assert.True(args.CreateBackup);
        }

        /// <summary>
        /// Verifies that CreateBackup defaults to false when --backup flag is not present.
        /// </summary>
        [Fact]
        public void Parse_BackupFlagNotPresent()
        {
            var args = Parse(Array.Empty<string>());

            Assert.False(args.CreateBackup);
        }

        /// <summary>
        /// Verifies that all command-line arguments can be parsed together correctly.
        /// </summary>
        [Fact]
        public void Parse_AllArguments()
        {
            var args = Parse(new[]
            {
                "--pid", "5678",
                "--zip", @"C:\updates\app.zip",
                "--target", @"C:\Program Files\App",
                "--exe", "App.exe",
                "--backup"
            });

            Assert.Equal(5678, args.ProcessId);
            Assert.Equal(@"C:\updates\app.zip", args.ZipPath);
            Assert.Equal(@"C:\Program Files\App", args.TargetDirectory);
            Assert.Equal("App.exe", args.MainExecutable);
            Assert.True(args.CreateBackup);
        }

        /// <summary>
        /// Verifies that repeatable preserve arguments are retained in order.
        /// </summary>
        [Fact]
        public void Parse_RepeatablePreserveArguments()
        {
            var args = Parse(new[]
            {
                "--preserve", "data/user",
                "--preserve", @"cache\local"
            });

            Assert.Equal(new[] { "data/user", @"cache\local" }, args.PreservedRelativePaths);
        }

        /// <summary>
        /// Verifies that argument parsing is case-insensitive.
        /// </summary>
        [Fact]
        public void Parse_CaseInsensitive()
        {
            var args = Parse(new[]
            {
                "--PID", "1234",
                "--ZIP", @"C:\test.zip",
                "--TARGET", @"C:\target",
                "--EXE", "test.exe"
            });

            Assert.Equal(1234, args.ProcessId);
            Assert.Equal(@"C:\test.zip", args.ZipPath);
            Assert.Equal(@"C:\target", args.TargetDirectory);
            Assert.Equal("test.exe", args.MainExecutable);
        }

        /// <summary>
        /// Verifies that an invalid process ID value defaults to zero.
        /// </summary>
        [Fact]
        public void Parse_InvalidPid_DefaultsToZero()
        {
            var args = Parse(new[] { "--pid", "invalid" });

            Assert.Equal(0, args.ProcessId);
        }

        /// <summary>
        /// Verifies that an argument without a value is ignored during parsing.
        /// </summary>
        [Fact]
        public void Parse_MissingValue_IgnoresArgument()
        {
            var args = Parse(new[] { "--pid" });

            Assert.Equal(0, args.ProcessId);
        }

        /// <summary>
        /// Verifies that parsing empty arguments returns an object with default values.
        /// </summary>
        [Fact]
        public void Parse_EmptyArgs_ReturnsDefaults()
        {
            var args = Parse(Array.Empty<string>());

            Assert.Equal(0, args.ProcessId);
            Assert.Null(args.ZipPath);
            Assert.Null(args.TargetDirectory);
            Assert.Null(args.MainExecutable);
            Assert.False(args.CreateBackup);
        }

        /// <summary>
        /// Verifies that unknown or unrecognized arguments are ignored during parsing.
        /// </summary>
        [Fact]
        public void Parse_UnknownArguments_Ignored()
        {
            var args = Parse(new[]
            {
                "--unknown", "value",
                "--pid", "1234"
            });

            Assert.Equal(1234, args.ProcessId);
        }

        #endregion

        #region Validate Tests

        /// <summary>
        /// Verifies that validation throws ArgumentException when ProcessId is missing or zero.
        /// </summary>
        /// <exception cref="ArgumentException">Expected exception when ProcessId is invalid.</exception>
        [Fact]
        public void Validate_MissingProcessId_ThrowsArgumentException()
        {
            var tempFile = Path.GetTempFileName();
            var tempDir = Path.GetTempPath();
            try
            {
                var args = new UpdaterArguments
                {
                    ProcessId = 0,
                    ZipPath = tempFile,
                    TargetDirectory = tempDir,
                    MainExecutable = "test.exe"
                };

                var ex = Assert.Throws<ArgumentException>(() => Validate(args));
                Assert.Contains("--pid", ex.Message);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies that validation throws ArgumentException when ZipPath is null or empty.
        /// </summary>
        /// <exception cref="ArgumentException">Expected exception when ZipPath is missing.</exception>
        [Fact]
        public void Validate_MissingZipPath_ThrowsArgumentException()
        {
            var tempDir = Path.GetTempPath();
            var args = new UpdaterArguments
            {
                ProcessId = 1234,
                ZipPath = null,
                TargetDirectory = tempDir,
                MainExecutable = "test.exe"
            };

            var ex = Assert.Throws<ArgumentException>(() => Validate(args));
            Assert.Contains("--zip", ex.Message);
        }

        /// <summary>
        /// Verifies that validation throws ArgumentException when the specified zip file does not exist.
        /// </summary>
        /// <exception cref="ArgumentException">Expected exception when zip file is not found.</exception>
        [Fact]
        public void Validate_NonExistentZipFile_ThrowsArgumentException()
        {
            var tempDir = Path.GetTempPath();
            var args = new UpdaterArguments
            {
                ProcessId = 1234,
                ZipPath = @"C:\nonexistent\file.zip",
                TargetDirectory = tempDir,
                MainExecutable = "test.exe"
            };

            var ex = Assert.Throws<ArgumentException>(() => Validate(args));
            Assert.Contains("Zip file not found", ex.Message);
        }

        /// <summary>
        /// Verifies that validation throws ArgumentException when TargetDirectory is null or empty.
        /// </summary>
        /// <exception cref="ArgumentException">Expected exception when TargetDirectory is missing.</exception>
        [Fact]
        public void Validate_MissingTargetDirectory_ThrowsArgumentException()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var args = new UpdaterArguments
                {
                    ProcessId = 1234,
                    ZipPath = tempFile,
                    TargetDirectory = null,
                    MainExecutable = "test.exe"
                };

                var ex = Assert.Throws<ArgumentException>(() => Validate(args));
                Assert.Contains("--target", ex.Message);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies that validation throws ArgumentException when the target directory does not exist.
        /// </summary>
        /// <exception cref="ArgumentException">Expected exception when target directory is not found.</exception>
        [Fact]
        public void Validate_NonExistentTargetDirectory_ThrowsArgumentException()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var args = new UpdaterArguments
                {
                    ProcessId = 1234,
                    ZipPath = tempFile,
                    TargetDirectory = @"C:\nonexistent\directory",
                    MainExecutable = "test.exe"
                };

                var ex = Assert.Throws<ArgumentException>(() => Validate(args));
                Assert.Contains("Target directory not found", ex.Message);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies that validation throws ArgumentException when MainExecutable is null or empty.
        /// </summary>
        /// <exception cref="ArgumentException">Expected exception when MainExecutable is missing.</exception>
        [Fact]
        public void Validate_MissingMainExecutable_ThrowsArgumentException()
        {
            var tempFile = Path.GetTempFileName();
            var tempDir = Path.GetTempPath();
            try
            {
                var args = new UpdaterArguments
                {
                    ProcessId = 1234,
                    ZipPath = tempFile,
                    TargetDirectory = tempDir,
                    MainExecutable = null
                };

                var ex = Assert.Throws<ArgumentException>(() => Validate(args));
                Assert.Contains("--exe", ex.Message);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies that validation concatenates multiple error messages when multiple arguments are invalid.
        /// </summary>
        /// <exception cref="ArgumentException">Expected exception containing all validation errors.</exception>
        [Fact]
        public void Validate_MultipleErrors_ConcatenatesMessages()
        {
            var args = new UpdaterArguments
            {
                ProcessId = 0,
                ZipPath = null,
                TargetDirectory = null,
                MainExecutable = null
            };

            var ex = Assert.Throws<ArgumentException>(() => Validate(args));
            Assert.Contains("--pid", ex.Message);
            Assert.Contains("--zip", ex.Message);
            Assert.Contains("--target", ex.Message);
            Assert.Contains("--exe", ex.Message);
        }

        /// <summary>
        /// Verifies that validation succeeds without throwing when all arguments are valid.
        /// </summary>
        [Fact]
        public void Validate_ValidArguments_DoesNotThrow()
        {
            var tempFile = Path.GetTempFileName();
            var tempDir = Path.GetTempPath();
            try
            {
                var args = new UpdaterArguments
                {
                    ProcessId = 1234,
                    ZipPath = tempFile,
                    TargetDirectory = tempDir,
                    MainExecutable = "test.exe"
                };

                var exception = Record.Exception(() => Validate(args));

                Assert.Null(exception);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies that absolute and traversal-based preserved paths are rejected.
        /// </summary>
        /// <param name="preservedPath">The invalid preserved path.</param>
        [Theory]
        [InlineData(@"C:\settings")]
        [InlineData("../settings")]
        [InlineData("data/./settings")]
        [InlineData("file:stream")]
        public void Validate_InvalidPreservedPath_ThrowsArgumentException(string preservedPath)
        {
            var tempFile = Path.GetTempFileName();
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var args = new UpdaterArguments
                {
                    ProcessId = 1234,
                    ZipPath = tempFile,
                    TargetDirectory = tempDirectory,
                    MainExecutable = "app.exe"
                };
                args.PreservedRelativePaths.Add(preservedPath);

                Assert.Throws<ArgumentException>(() => Validate(args));
            }
            finally
            {
                File.Delete(tempFile);
                Directory.Delete(tempDirectory);
            }
        }

        #endregion

        #region UnquoteArgument Tests (Via Parse)

        /// <summary>
        /// Verifies that the argument unquoting functionality correctly handles various input formats.
        /// </summary>
        /// <param name="input">The input string that may contain quotes.</param>
        /// <param name="expected">The expected unquoted result.</param>
        [Theory]
        [InlineData("simple", "simple")]
        [InlineData("\"quoted\"", "quoted")]
        [InlineData("\"path with spaces\"", "path with spaces")]
        [InlineData("unbalanced\"", "unbalanced\"")]
        [InlineData("\"also unbalanced", "\"also unbalanced")]
        public void Parse_UnquotesArgumentsCorrectly(string input, string expected)
        {
            var args = Parse(new[] { "--zip", input });

            Assert.Equal(expected, args.ZipPath);
        }

        #endregion
    }
}
