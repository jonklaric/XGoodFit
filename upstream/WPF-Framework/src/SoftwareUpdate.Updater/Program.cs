using System;
using System.IO;
using System.Threading;

namespace SoftwareUpdate.Updater
{
    /// <summary>
    /// Main entry point for the external updater application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This application is launched by the main application to perform the actual
    /// update installation. It waits for the main application to exit, then extracts
    /// the update package and restarts the application.
    /// </para>
    /// <para>
    /// Usage: SoftwareUpdate.Updater.exe --pid [pid] --zip [path] --target [dir] --exe [name]
    /// [--backup] [--preserve relative-path]
    /// </para>
    /// </remarks>
    internal class Program
    {
        /// <summary>
        /// Time in milliseconds to wait for user input before auto-closing the console window
        /// after a successful update. This provides users a brief opportunity to review the
        /// update log while not blocking unattended updates indefinitely.
        /// </summary>
        private const int AutoCloseTimeoutMs = 3000;

        /// <summary>
        /// Timeout for the failure-path "press any key to exit" prompt. Longer than the
        /// success-path timeout so users with an attached console have time to read the
        /// error message. The same guarded WaitForKeyWithTimeout helper handles the
        /// no-console case (when launched with CreateNoWindow=true) by returning
        /// immediately rather than throwing InvalidOperationException.
        /// </summary>
        private const int FailureCloseTimeoutMs = 15000;

        /// <summary>
        /// Polling interval in milliseconds when checking for user key input.
        /// A small value provides responsive key detection without excessive CPU usage.
        /// </summary>
        private const int KeyPollIntervalMs = 100;

        /// <summary>
        /// The path to the current log file, or <c>null</c> if logging has not been initialized.
        /// </summary>
        private static string? _logFilePath;

        /// <summary>
        /// Entry point for the updater application.
        /// </summary>
        /// <param name="args">Command-line arguments specifying the update configuration.</param>
        /// <returns>0 on success, 1 on failure.</returns>
        static int Main(string[] args)
        {
            try
            {
                Console.Title = "Software Update";
                Console.WriteLine("========================================");
                Console.WriteLine("       Software Update in Progress");
                Console.WriteLine("========================================");
                Console.WriteLine();

                // Parse arguments
                var arguments = UpdaterArguments.Parse(args);

                // Validate first (ensures TargetDirectory is not null)
                arguments.Validate();

                // Set up logging (after validation guarantees non-null)
                SetupLogging(arguments.TargetDirectory!);

                Log("Validating arguments...");
                Log("Arguments validated.");

                // Execute update
                var installer = new InstallationManager(arguments, Log);
                installer.Execute();

                Log("Update completed successfully!");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");

                // Auto-close after timeout if no input
                if (!WaitForKeyWithTimeout(AutoCloseTimeoutMs))
                {
                    Console.WriteLine("Auto-closing...");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log($"FATAL ERROR: {ex.Message}");
                if (ex.StackTrace != null)
                    Log(ex.StackTrace);

                Console.WriteLine();
                try
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("========================================");
                    Console.WriteLine("           UPDATE FAILED");
                    Console.WriteLine("========================================");
                }
                finally
                {
                    Console.ResetColor();
                }
                Console.WriteLine();
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("The application may need to be reinstalled manually.");
                Console.WriteLine("Check the log file for details.");
                Console.WriteLine();
                Console.WriteLine($"Press any key to exit (auto-closing in {FailureCloseTimeoutMs / 1000} seconds)...");
                // Use the same guarded wait as the success path. With CreateNoWindow=true the
                // console is not attached and Console.ReadKey throws InvalidOperationException;
                // the helper catches that case and returns immediately rather than crashing
                // the updater mid-failure (which previously left the install in a corrupt state
                // with no UI feedback). Longer timeout gives users with consoles time to read.
                if (!WaitForKeyWithTimeout(FailureCloseTimeoutMs))
                {
                    Console.WriteLine("Auto-closing...");
                }

                return 1;
            }
        }

        /// <summary>
        /// Sets up logging to file.
        /// </summary>
        /// <param name="targetDirectory">The target installation directory.</param>
        private static void SetupLogging(string targetDirectory)
        {
            try
            {
                var logDir = Path.Combine(targetDirectory, "logs");
                Directory.CreateDirectory(logDir);

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                _logFilePath = Path.Combine(logDir, $"update_{timestamp}.log");
            }
            catch (Exception ex)
            {
                // Fall back to temp directory
                _logFilePath = Path.Combine(Path.GetTempPath(), $"update_{Guid.NewGuid():N}.log");
                Console.WriteLine($"Warning: Could not create log in target directory: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs a message to console and file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private static void Log(string message)
        {
            var timestamp = DateTime.UtcNow.ToString("HH:mm:ss");
            var logLine = $"[{timestamp}] {message}";

            Console.WriteLine(logLine);

            try
            {
                if (!string.IsNullOrEmpty(_logFilePath))
                {
                    File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // Log to console if file logging fails
                Console.WriteLine($"Warning: Could not write to log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Waits for a key press with timeout.
        /// </summary>
        /// <param name="milliseconds">The timeout in milliseconds.</param>
        /// <returns>True if a key was pressed, false if timeout occurred.</returns>
        private static bool WaitForKeyWithTimeout(int milliseconds)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < milliseconds)
            {
                try
                {
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                        return true;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Console input is not available (e.g., redirected input stream)
                    return false;
                }
                Thread.Sleep(KeyPollIntervalMs);
            }
            return false;
        }
    }
}
