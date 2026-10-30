using System;
using System.Threading;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Simple;
using AvalonDock.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Themes;

namespace RMC.BestFit.App.Tests
{
    /// <summary>
    /// The headless Avalonia application the tests run in (see Themes.Tests): Avalonia's base control
    /// themes and the dock's, as the application has. Upstream's tests used a bare WPF Application, and
    /// the tests that need the application's resources merge them (GUI/Support/WpfTestHost.cs).
    /// </summary>
    public sealed class TestApp : Application
    {
        /// <inheritdoc/>
        public override void Initialize()
        {
            Styles.Add(new SimpleTheme());
            Styles.Add(new AvalonDockTheme());
        }

        /// <summary>Builds the headless application.</summary>
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<TestApp>()
                .UseSkia()
                .UseHarfBuzz()
                .UseThemeFonts()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }

    /// <summary>Hosts one headless Avalonia session for the test assembly (see Themes.Tests).</summary>
    [TestClass]
    public static class HeadlessSession
    {
        private static HeadlessUnitTestSession _session;

        /// <summary>Starts the session and its application.</summary>
        /// <param name="context">The test context.</param>
        /// <remarks>
        /// Upstream's tests run in parallel, and Avalonia makes the first thread that creates a dispatcher
        /// its UI thread. So the application starts here, before any test can create Avalonia objects on
        /// its own thread.
        /// </remarks>
        [AssemblyInitialize]
        public static void Start(TestContext context)
        {
            _session = HeadlessUnitTestSession.StartNew(typeof(TestApp), AvaloniaTestIsolationLevel.PerAssembly);
            Run(() => Application.Current);
        }

        /// <summary>Stops the session.</summary>
        [AssemblyCleanup]
        public static void Stop() => _session?.Dispose();

        /// <summary>Runs a function on the Avalonia UI thread and waits for its result.</summary>
        /// <typeparam name="T">The function's result type.</typeparam>
        /// <param name="function">The function.</param>
        /// <returns>The function's result.</returns>
        public static T Run<T>(Func<T> function) =>
            _session.Dispatch(function, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>Runs an action on the Avalonia UI thread and waits for it.</summary>
        /// <param name="action">The action.</param>
        public static void Run(Action action) =>
            _session.Dispatch(action, CancellationToken.None).GetAwaiter().GetResult();
    }
}
