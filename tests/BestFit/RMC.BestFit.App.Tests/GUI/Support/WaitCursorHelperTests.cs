using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mouse = WpfCompat.Input.Mouse;
using Cursors = WpfCompat.Input.Cursors;

namespace RMC.BestFit.App.Tests.GUI.Support
{
    /// <summary>
    /// Source-level regression tests for the lazy plot wait-cursor helper.
    /// </summary>
    /// <remarks>
    /// The App test project is often built with dependency builds skipped to avoid unrelated
    /// core-library rebuild failures. These tests pin the helper implementation without forcing
    /// a direct reference to the WinExe assembly metadata.
    /// <para>
    /// XGoodFit port: the audit reads the port (src/BestFit/RMC.BestFit.App). It sets WpfCompat's
    /// Mouse.OverrideCursor, which windows show at once, so it has no Mouse.UpdateCursor() call: the
    /// checks for that call check instead that the cursor is set before the work, and two tests run the
    /// helpers and look at the cursor the windows show. Avalonia's dispatcher Post is WPF's BeginInvoke.
    /// </para>
    /// </remarks>
    [TestClass]
    public class WaitCursorHelperTests
    {
        /// <summary>
        /// Verifies the helper sets a visible wait cursor before synchronous work begins.
        /// </summary>
        [TestMethod]
        public void RunWithVisibleWaitCursor_SetsCursorBeforeAction()
        {
            string source = ReadWaitCursorHelperSource();
            string method = ExtractMethodSource(source, "RunWithVisibleWaitCursor");

            StringAssert.Contains(method, "Mouse.OverrideCursor = Cursors.Wait;");
            Assert.IsTrue(method.IndexOf("Mouse.OverrideCursor = Cursors.Wait;", StringComparison.Ordinal) < method.IndexOf("action();", StringComparison.Ordinal),
                "The wait cursor is set before the work starts.");
            StringAssert.Contains(method, "action();");
        }

        /// <summary>
        /// Verifies the helper restores the previous cursor after protected work completes.
        /// </summary>
        [TestMethod]
        public void RunWithVisibleWaitCursor_RestoresPreviousCursorAtBackgroundPriority()
        {
            string source = ReadWaitCursorHelperSource();
            string method = ExtractMethodSource(source, "RunWithVisibleWaitCursor");

            StringAssert.Contains(method, "Cursor previousCursor = Mouse.OverrideCursor;");
            StringAssert.Contains(method, "dispatcher.Post(");
            StringAssert.Contains(method, "}), DispatcherPriority.Background);");
            StringAssert.Contains(method, "Mouse.OverrideCursor = previousCursor;");
        }

        /// <summary>
        /// Verifies the helper does not push a nested dispatcher frame.
        /// </summary>
        [TestMethod]
        public void RunWithVisibleWaitCursor_DoesNotSynchronouslyInvokeDispatcher()
        {
            string source = ReadWaitCursorHelperSource();
            string method = ExtractMethodSource(source, "RunWithVisibleWaitCursor");

            Assert.IsFalse(method.Contains("dispatcher.Invoke(", StringComparison.Ordinal),
                "Synchronous dispatcher invocation can fail while WPF dispatcher processing is suspended.");
        }

        /// <summary>
        /// Verifies the helper validates its required arguments.
        /// </summary>
        [TestMethod]
        public void RunWithVisibleWaitCursor_ValidatesArguments()
        {
            string source = ReadWaitCursorHelperSource();
            string method = ExtractMethodSource(source, "RunWithVisibleWaitCursor");

            StringAssert.Contains(method, "throw new ArgumentNullException(nameof(dispatcher))");
            StringAssert.Contains(method, "throw new ArgumentNullException(nameof(action))");
        }

        /// <summary>
        /// Verifies the asynchronous helper sets the wait cursor before yielding and running work.
        /// </summary>
        [TestMethod]
        public void RunWithVisibleWaitCursorAsync_SetsCursorBeforeAwaitingAction()
        {
            string source = ReadWaitCursorHelperSource();
            string method = ExtractMethodSource(source, "RunWithVisibleWaitCursorAsync");

            StringAssert.Contains(method, "Mouse.OverrideCursor = Cursors.Wait;");
            Assert.IsTrue(method.IndexOf("Mouse.OverrideCursor = Cursors.Wait;", StringComparison.Ordinal) < method.IndexOf("await dispatcher.InvokeAsync(", StringComparison.Ordinal),
                "The wait cursor is set before the helper yields.");
            StringAssert.Contains(method, "await dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);");
            StringAssert.Contains(method, "await action();");
        }

        /// <summary>
        /// Verifies the asynchronous helper restores the previous cursor at background priority.
        /// </summary>
        [TestMethod]
        public void RunWithVisibleWaitCursorAsync_RestoresPreviousCursorAtBackgroundPriority()
        {
            string source = ReadWaitCursorHelperSource();
            string method = ExtractMethodSource(source, "RunWithVisibleWaitCursorAsync");

            StringAssert.Contains(method, "Cursor previousCursor = Mouse.OverrideCursor;");
            StringAssert.Contains(method, "dispatcher.Post(");
            StringAssert.Contains(method, "}), DispatcherPriority.Background);");
            StringAssert.Contains(method, "Mouse.OverrideCursor = previousCursor;");
        }

        /// <summary>
        /// Verifies the asynchronous helper avoids synchronous dispatcher invocation.
        /// </summary>
        [TestMethod]
        public void RunWithVisibleWaitCursorAsync_DoesNotSynchronouslyInvokeDispatcher()
        {
            string source = ReadWaitCursorHelperSource();
            string method = ExtractMethodSource(source, "RunWithVisibleWaitCursorAsync");

            Assert.IsFalse(method.Contains("dispatcher.Invoke(", StringComparison.Ordinal),
                "Synchronous dispatcher invocation can block render updates before the batch starts.");
        }

        /// <summary>
        /// Verifies the asynchronous helper validates its required arguments.
        /// </summary>
        [TestMethod]
        public void RunWithVisibleWaitCursorAsync_ValidatesArguments()
        {
            string source = ReadWaitCursorHelperSource();
            string method = ExtractMethodSource(source, "RunWithVisibleWaitCursorAsync");

            StringAssert.Contains(method, "throw new ArgumentNullException(nameof(dispatcher))");
            StringAssert.Contains(method, "throw new ArgumentNullException(nameof(action))");
        }

        /// <summary>
        /// XGoodFit addition: the synchronous helper shows the wait cursor in the windows while the work runs,
        /// and restores the previous cursor once the dispatcher reaches background priority.
        /// </summary>
        [STATestMethod]
        public void RunWithVisibleWaitCursor_ShowsTheWaitCursorWhileTheWorkRuns()
        {
            // WpfCompat's override cursor reaches the window that has the focus when there's no desktop
            // lifetime; its command manager follows the focus once it has started.
            WpfCompat.Input.CommandManager.InvalidateRequerySuggested();
            var button = new Button();
            var window = new Window { Width = 200, Height = 100, Content = button };
            window.Show();
            window.Activate();
            button.Focus();
            try
            {
                Cursor cursorDuringWork = null;
                RMC_BestFit.WaitCursorHelper.RunWithVisibleWaitCursor(Dispatcher.UIThread, () => cursorDuringWork = window.Cursor);

                Assert.AreSame(Cursors.Wait, cursorDuringWork, "the window shows the wait cursor during the work");
                Assert.AreSame(Cursors.Wait, Mouse.OverrideCursor, "the cursor is restored only at background priority");
                Dispatcher.UIThread.RunJobs();
                Assert.IsNull(Mouse.OverrideCursor);
                Assert.AreNotSame(Cursors.Wait, window.Cursor);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                window.Close();
            }
        }

        /// <summary>
        /// XGoodFit addition: the asynchronous helper shows the wait cursor before it yields and while the
        /// work runs, and restores the previous cursor afterwards.
        /// </summary>
        [STATestMethod]
        public void RunWithVisibleWaitCursorAsync_ShowsTheWaitCursorWhileTheWorkRuns()
        {
            // WpfCompat's override cursor reaches the window that has the focus when there's no desktop
            // lifetime; its command manager follows the focus once it has started.
            WpfCompat.Input.CommandManager.InvalidateRequerySuggested();
            var button = new Button();
            var window = new Window { Width = 200, Height = 100, Content = button };
            window.Show();
            window.Activate();
            button.Focus();
            try
            {
                Cursor cursorDuringWork = null;
                // The test has no synchronization context, so the work runs on a pool thread: it reads the
                // override cursor, not the window's.
                Task task = RMC_BestFit.WaitCursorHelper.RunWithVisibleWaitCursorAsync(Dispatcher.UIThread, () =>
                {
                    cursorDuringWork = Mouse.OverrideCursor;
                    return Task.CompletedTask;
                });
                Assert.AreSame(Cursors.Wait, window.Cursor, "the wait cursor shows before the helper yields");

                for (int i = 0; i < 500 && !task.IsCompleted; i++)
                {
                    Dispatcher.UIThread.RunJobs();
                    System.Threading.Thread.Sleep(10);
                }
                Dispatcher.UIThread.RunJobs();
                Assert.IsTrue(task.IsCompletedSuccessfully);
                Assert.AreSame(Cursors.Wait, cursorDuringWork, "the wait cursor shows during the work");
                Assert.IsNull(Mouse.OverrideCursor);
                Assert.AreNotSame(Cursors.Wait, window.Cursor);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                window.Close();
            }
        }

        /// <summary>
        /// Reads the wait-cursor helper source file.
        /// </summary>
        /// <param name="sourceFilePath">The compiler-provided source path for this test file.</param>
        /// <returns>The source text for <c>WaitCursorHelper.cs</c>.</returns>
        private static string ReadWaitCursorHelperSource([CallerFilePath] string sourceFilePath = "")
        {
            return File.ReadAllText(Path.Combine(
                FindRepoRoot(sourceFilePath),
                "src",
                "BestFit",
                "RMC.BestFit.App",
                "GUI",
                "Support",
                "WaitCursorHelper.cs"));
        }

        /// <summary>
        /// Extracts a method body from a C# source file using brace counting.
        /// </summary>
        /// <param name="source">The C# source text.</param>
        /// <param name="methodName">The method name to extract.</param>
        /// <returns>The method source, including its signature and braces.</returns>
        private static string ExtractMethodSource(string source, string methodName)
        {
            int start = source.IndexOf(methodName + "(", StringComparison.Ordinal);
            Assert.IsTrue(start >= 0, methodName + " should exist.");

            int openingBrace = source.IndexOf('{', start);
            Assert.IsTrue(openingBrace > start, methodName + " should have an opening brace.");

            int depth = 0;
            for (int i = openingBrace; i < source.Length; i++)
            {
                if (source[i] == '{')
                {
                    depth++;
                }
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(start, i - start + 1);
                    }
                }
            }

            Assert.Fail(methodName + " should have a closing brace.");
            return string.Empty;
        }

        /// <summary>
        /// Finds the repository root from the test output directory.
        /// </summary>
        /// <param name="sourceFilePath">The compiler-provided source path for this test file.</param>
        /// <returns>The absolute repository root.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the source tree cannot be found.</exception>
        private static string FindRepoRoot(string sourceFilePath)
        {
            string[] searchRoots = new[]
            {
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory(),
                Path.GetDirectoryName(sourceFilePath)
            };

            foreach (string searchRoot in searchRoots)
            {
                if (string.IsNullOrEmpty(searchRoot)) continue;
                DirectoryInfo directory = new DirectoryInfo(searchRoot);
                while (directory != null)
                {
                    string candidate = Path.Combine(directory.FullName, "src", "BestFit", "RMC.BestFit.App");
                    if (Directory.Exists(candidate))
                    {
                        return directory.FullName;
                    }

                    directory = directory.Parent;
                }
            }

            throw new DirectoryNotFoundException("Could not locate repository root from the test output directory.");
        }
    }
}
