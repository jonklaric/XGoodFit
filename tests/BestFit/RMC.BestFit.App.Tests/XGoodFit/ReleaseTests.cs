using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using FrameworkUI;
using FrameworkUI.ProjectExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlotControls;
using RMC.BestFit.App.Tests.GUI.Support;
using RMC.BestFit.UI;
using RMC_BestFit;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: what the tests close is let go, so that a run of every test in one process stays small.
    /// Each check makes the objects in a method of its own, lets the dispatcher and the garbage collector run, and
    /// checks that weak references to them are gone.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class ReleaseTests
    {
        private const string BlockMaximum = "2-input-data/1-block-maximum/usgs-block-max-example.bestfit";

        /// <summary>
        /// A main window <see cref="BestFitShell"/> opened and closed is let go, with its explorer, the example's
        /// element and the element's document: the application's project, a singleton, doesn't keep them
        /// (see <see cref="BestFitShell.Release"/>). Nor does it keep a properties panel that never loaded, as one the
        /// shell replaces before it loads: upstream's Input Data panel stops listening to the project only on Unloaded.
        /// </summary>
        [TestMethod]
        public void ClosingTheMainWindow_LetsGoOfIt_ItsExplorerAndTheExamplesDocuments()
        {
            HeadlessSession.Run(() =>
            {
                WeakReference[] references = OpenAndCloseTheMainWindow();
                Collect();
                Assert.AreEqual("", string.Join(", ", references.Where(r => r.IsAlive).Select(r => r.Target.GetType().Name)), "still alive");
            });
        }

        /// <summary>
        /// A view shown on an example <see cref="ViewTesting"/> opened is let go, with the example, once the window
        /// is closed and the example disposed: the Messenger forgets the closed project's messages.
        /// </summary>
        [STATestMethod]
        public void ClosingAViewAndItsExample_LetsGoOfThem()
        {
            OxyPlotThemeManager.SuppressThemeChangeWarning = true;
            _ = typeof(InputDataControl).Assembly;
            WpfTestHost.EnsureResources();
            try
            {
                using (ViewTesting.ApplyThemeDefaults())
                using (ViewTesting.UseExamplesCulture())
                {
                    WeakReference[] references = ShowAndCloseAView();
                    Collect();
                    Assert.AreEqual("", string.Join(", ", references.Where(r => r.IsAlive).Select(r => r.Target.GetType().Name)), "still alive");
                }
            }
            finally
            {
                WpfTestHost.ReleaseResources();
            }
        }

        /// <summary>A copy-paste grid that is made and never shown is let go.</summary>
        [STATestMethod]
        public void AGridThatIsNeverShown_IsLetGo()
        {
            WeakReference grid = MakeAGrid();
            Collect();
            Assert.IsFalse(grid.IsAlive);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] OpenAndCloseTheMainWindow()
        {
            BestFitShell.Start();
            MainWindow window = null;
            try
            {
                (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example("usgs-block-max-example"));
                InputData element = project.ElementCollections.OfType<InputDataCollection>().Single().OfType<InputData>().First();
                window.DoubleClick(ElementNode.FindElementNode(element, node).NodeHeader);
                Assert.IsTrue(Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(window).OfType<InputDataControl>().Any(control => control.Element == element), "the element's document is shown");
                var unloadedPanel = new InputDataPropertiesControl { Element = element };
                unloadedPanel.Element = null;
                return new[] { new WeakReference(window), new WeakReference(node), new WeakReference(element), new WeakReference(unloadedPanel) };
            }
            finally
            {
                BestFitShell.Close(window);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] ShowAndCloseAView()
        {
            using var opened = ViewTesting.Open(BlockMaximum);
            var element = opened.Element<InputData>("USGS - 01134500 - Block Max - Calendar Year");
            var control = new InputDataControl { Element = element };
            Window window = ViewTesting.Show(control);
            window.Close();
            return new[] { new WeakReference(window), new WeakReference(control), new WeakReference(element), new WeakReference(opened.Project) };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference MakeAGrid() => new WeakReference(new GenericControls.CopyPasteDataGrid());

        // Lets posted work (unloading, the renderer's), due timers and finalizers run, and collects. ProDataGrid's summaries
        // recalculate 100 ms after a change, on a timer that holds its grid until then.
        private static void Collect()
        {
            Thread.Sleep(150);
            for (int i = 0; i < 3; i++)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            GC.Collect();
        }
    }
}
