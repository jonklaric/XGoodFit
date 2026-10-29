using System.Diagnostics;
using System.IO;
using Xunit;

namespace SoftwareUpdate.Updater.Tests
{
    /// <summary>
    /// Regression tests for F-003: the failure-path key-wait used to throw
    /// <see cref="System.InvalidOperationException"/> when the updater was launched
    /// with no console attached (e.g. <c>CreateNoWindow=true</c>). The fix wraps
    /// <c>Console.ReadKey</c> inside the existing <c>WaitForKeyWithTimeout</c> guard
    /// so the failure path exits cleanly instead of crashing.
    /// </summary>
    public class WaitForKeyTimeoutTests
    {
        /// <summary>
        /// Locates the built <c>SoftwareUpdate.Updater.exe</c> alongside the test assembly.
        /// </summary>
        private static string LocateUpdaterExe()
        {
            var dir = Path.GetDirectoryName(typeof(WaitForKeyTimeoutTests).Assembly.Location)!;
            var path = Path.Combine(dir, "SoftwareUpdate.Updater.exe");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"SoftwareUpdate.Updater.exe was expected next to the test assembly at '{path}'. " +
                    "The test depends on the updater build copying its output beside the test runner.");
            }
            return path;
        }

        /// <summary>
        /// F-003: With no attached console, the failure-path Console.ReadKey would throw
        /// InvalidOperationException; the fix swallows it through WaitForKeyWithTimeout.
        /// We invoke the updater with deliberately invalid args so it takes the failure
        /// path, redirect stderr, and confirm no InvalidOperationException is reported.
        /// </summary>
        [Fact]
        public void WaitForKeyWithTimeout_DoesNotThrowWithoutConsole()
        {
            var exe = LocateUpdaterExe();

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "--unknown-arg something",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var proc = Process.Start(psi);
            Assert.NotNull(proc);

            // Force stdin closed so the failure-path key wait cannot read anything.
            proc!.StandardInput.Close();

            // The failure-path keeps the console open up to FailureCloseTimeoutMs (15s).
            // Give it 30s to finish to be safe.
            var exited = proc.WaitForExit(30_000);
            if (!exited)
            {
                try { proc.Kill(true); } catch { /* ignore */ }
                Assert.Fail("Updater did not exit within 30 seconds; failure-path likely blocked on Console.ReadKey.");
            }

            var stderr = proc.StandardError.ReadToEnd();
            var stdout = proc.StandardOutput.ReadToEnd();

            // Must have exited with a non-zero code (it failed to parse args).
            Assert.NotEqual(0, proc.ExitCode);

            // Crucially: must NOT have crashed via InvalidOperationException — the F-003 fix
            // wraps Console.ReadKey to handle the no-console case.
            var combined = stderr + stdout;
            Assert.DoesNotContain("InvalidOperationException", combined);
        }
    }
}
