using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RMC.BestFit.App.Tests
{
    /// <summary>
    /// Runs a test on the Avalonia UI thread, where upstream ran it on an STA thread for WPF.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition, as in RMC.BestFit.UI.Tests. Upstream marks the tests that create WPF controls
    /// with MSTest's <c>[STATestMethod]</c>. The ported controls are Avalonia's, which use the
    /// application's resources and so belong to its UI thread. This attribute has MSTest's name and is
    /// declared in this namespace, which the test files' namespaces (RMC.BestFit.App.Tests.*) search
    /// before their usings, so upstream's test files use it unchanged.
    /// <para>
    /// The test, with its initialize and cleanup methods, runs on the UI thread without a
    /// synchronization context, as on MSTest's STA threads: an async test's continuations then run on
    /// the thread pool, as upstream's did, instead of waiting for the UI thread that is blocked running
    /// the test.
    /// </para>
    /// </remarks>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public sealed class STATestMethodAttribute : TestMethodAttribute
    {
        /// <summary>Creates the attribute.</summary>
        public STATestMethodAttribute()
        {
        }

        /// <summary>Creates the attribute with a display name.</summary>
        /// <param name="displayName">The test's display name.</param>
        public STATestMethodAttribute(string displayName)
            : base(displayName)
        {
        }

        /// <inheritdoc/>
        public override TestResult[] Execute(ITestMethod testMethod) =>
            HeadlessSession.Run(() =>
            {
                SynchronizationContext context = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(null);
                try
                {
                    return base.Execute(testMethod);
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(context);
                }
            });
    }
}
