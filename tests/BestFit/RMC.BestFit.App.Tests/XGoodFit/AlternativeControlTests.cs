using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.App.Tests.GUI.Support;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the shared AlternativeControl (GUI/Support/Controls), on its own. Upstream's
    /// AlternativeControlRefreshTests checks it through each analysis view that hosts it, which are ported after
    /// it: these run the same checks on the selector directly (a result or status notification from a worker
    /// thread refreshes a checked comparison once, on the selector's dispatcher; a queued one can't revive a stale
    /// comparison; rebinding doesn't duplicate subscriptions), and the list's contents and order.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class AlternativeControlTests
    {
        [TestCleanup]
        public void Cleanup() => WpfTestHost.ReleaseResources();

        [STATestMethod]
        [DataRow("AnalysisResults")]
        [DataRow("IsEstimated")]
        public void WorkerNotification_RefreshesTheComparison_OnTheSelectorsDispatcher(string propertyName)
        {
            using var fixture = new Fixture();
            int owningThread = Environment.CurrentManagedThreadId;
            int callbackThread = -1;
            int calls = 0;
            fixture.Selector.AnalysisAdded += _ => { callbackThread = Environment.CurrentManagedThreadId; calls++; };
            fixture.Item.IsChecked = true;
            Assert.AreEqual(1, calls, "Checking an alternative notifies synchronously on the owning thread.");
            calls = 0;

            OnWorkerThread(() => fixture.Notify(propertyName));
            Assert.AreEqual(0, calls, "the worker's notification waits for the selector's dispatcher");
            WpfTestHost.DrainDispatcher();

            Assert.AreEqual(1, calls, "One source notification produces one comparison refresh.");
            Assert.AreEqual(owningThread, callbackThread);
        }

        [STATestMethod]
        [DataRow("Uncheck")]
        [DataRow("Remove")]
        [DataRow("ClearOwner")]
        [DataRow("ReplaceOwner")]
        public void QueuedNotification_IgnoresAStaleComparison(string change)
        {
            using var fixture = new Fixture();
            fixture.Item.IsChecked = true;
            int calls = 0;
            fixture.Selector.AnalysisAdded += _ => calls++;
            OnWorkerThread(() => fixture.Notify(nameof(IAnalysisElement.AnalysisResults)));

            switch (change)
            {
                case "Uncheck": fixture.Item.IsChecked = false; break;
                case "Remove": fixture.Collection.Remove(fixture.Alternative); break;
                case "ClearOwner": fixture.Selector.Element = null; break;
                case "ReplaceOwner": fixture.Selector.Element = new MixtureAnalysis("Replacement", fixture.Collection); break;
                default: throw new ArgumentOutOfRangeException(nameof(change));
            }
            WpfTestHost.DrainDispatcher();
            Assert.AreEqual(0, calls, "A completed background update must not resurrect an old comparison.");
        }

        [STATestMethod]
        public void Rebinding_RefreshesACheckedAlternativeOnce()
        {
            using var fixture = new Fixture();
            var owner = fixture.Selector.Element;
            for (int i = 0; i < 3; i++)
            {
                fixture.Selector.Element = null;
                fixture.Selector.Element = owner;
            }
            fixture.Selector.AnalysisList[0].IsChecked = true;
            int calls = 0;
            fixture.Selector.AnalysisAdded += _ => calls++;
            OnWorkerThread(() => fixture.Notify(nameof(IAnalysisElement.AnalysisResults)));
            WpfTestHost.DrainDispatcher();
            Assert.AreEqual(1, calls);
        }

        [STATestMethod]
        public void List_ShowsTheOtherAnalyses_AsUpstreamOrdersThem()
        {
            UseAppResources(ThemeColor.Light);
            try
            {
                using var fixture = new Fixture("Zeta", "Alpha", "Mid");
                var window = Show(fixture.Selector, 220, 200);
                try
                {
                    // Upstream sorts by "Name", a property its items don't have, so they keep the collection's order
                    // (an upstream quirk, kept: the time series selector sorts by Alternative.Name).
                    var list = fixture.Selector.AlternativeListBox;
                    CollectionAssert.AreEqual(new[] { "Zeta", "Alpha", "Mid" }, list.Items.Cast<AnalysisAlternativeItem>().Select(i => i.Alternative.Name).ToList());
                    Assert.IsFalse(fixture.Selector.AnalysisList.Any(i => i.Alternative == fixture.Selector.Element), "the analysis itself isn't an alternative");
                    var boxes = list.GetVisualDescendants().OfType<CheckBox>().ToList();
                    Assert.AreEqual(3, boxes.Count);
                    // Upstream binds IsEnabled to Alternative.Analysis.IsEstimated, a path analysis elements don't have:
                    // the binding fails and every box stays enabled, estimated or not (an upstream bug, kept).
                    Assert.IsTrue(boxes.All(b => b.IsEnabled));
                    Assert.IsTrue(fixture.Selector.AnalysisList.All(i => !i.Alternative.IsEstimated));
                    Assert.IsNull(list.SelectedItem, "nothing is selected (upstream moves the view's current item before the first)");

                    // A new analysis of the collection joins the list; checking and unchecking add and remove it.
                    var added = fixture.AddAnalysis("Beta");
                    Assert.AreEqual("Zeta,Alpha,Mid,Beta", string.Join(",", list.Items.Cast<AnalysisAlternativeItem>().Select(i => i.Alternative.Name)));
                    var events = new List<string>();
                    fixture.Selector.AnalysisAdded += i => events.Add("added " + i.Alternative.Name);
                    fixture.Selector.AnalysisRemoved += i => events.Add("removed " + i.Alternative.Name);
                    var beta = fixture.Selector.AnalysisList.Single(i => i.Alternative == added);
                    beta.IsChecked = true;
                    fixture.Selector.AddAllChecked();
                    fixture.Selector.RemoveAll();
                    beta.IsChecked = false;
                    CollectionAssert.AreEqual(new[] { "added Beta", "added Beta", "removed Zeta", "removed Alpha", "removed Mid", "removed Beta", "removed Beta" }, events);
                }
                finally
                {
                    window.Close();
                }
            }
            finally
            {
                ReleaseAppResources();
            }
        }

        [STATestMethod]
        public void FilterType_KeepsOnlyAnalysesOfThatType()
        {
            var project = BestFitProject.GetInstance();
            var collection = new UnivariateAnalysisCollection(project);
            var owner = new CompositeAnalysis("Current", collection, false);
            Fixture.Store(collection, new MixtureAnalysis("Mixture", collection, false), new CompositeAnalysis("Composite", collection, false));
            var selector = new AlternativeControl { AlternativeFilterType = typeof(CompositeAnalysis), Element = owner };
            CollectionAssert.AreEqual(new[] { "Composite" }, selector.AnalysisList.Select(i => i.Alternative.Name).ToList());
            selector.Element = null;
        }

        /// <summary>
        /// Runs an action on a thread of its own and waits for it. (Upstream's test uses
        /// <c>Task.Run(…).GetAwaiter().GetResult()</c>; the headless session's UI thread is a thread-pool thread, so
        /// .NET runs such a task inline on it, and the notification wouldn't come from another thread.)
        /// </summary>
        private static void OnWorkerThread(Action action)
        {
            Exception failure = null;
            var thread = new System.Threading.Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.Start();
            thread.Join();
            if (failure != null) throw new AssertFailedException("The worker thread failed: " + failure);
        }

        /// <summary>
        /// A selector on an analysis whose collection holds others (stored without SQLite, as upstream's test does).
        /// </summary>
        private sealed class Fixture : IDisposable
        {
            private static readonly MethodInfo RaisePropertyChange =
                typeof(ElementBase).GetMethod("RaisePropertyChange", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            internal Fixture(params string[] alternatives)
            {
                WpfTestHost.EnsureResources();
                Collection = new UnivariateAnalysisCollection(BestFitProject.GetInstance());
                var owner = new MixtureAnalysis("Current", Collection, false);
                var stored = (alternatives.Length == 0 ? new[] { "Alternative" } : alternatives)
                    .Select(name => (IElement)new MixtureAnalysis(name, Collection, false)).ToArray();
                Store(Collection, stored);
                Alternative = (IAnalysisElement)stored[0];
                Selector = new AlternativeControl { Element = owner };
                Assert.AreEqual(stored.Length, Selector.AnalysisList.Count);
                Item = Selector.AnalysisList[0];
            }

            internal ElementCollectionBase Collection { get; }

            internal IAnalysisElement Alternative { get; }

            internal AlternativeControl Selector { get; }

            internal AnalysisAlternativeItem Item { get; }

            internal static void Store(ElementCollectionBase collection, params IElement[] elements)
            {
                var list = (List<IElement>)typeof(ElementCollectionBase)
                    .GetField("ElementList", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(collection);
                list.AddRange(elements);
            }

            // Adds an analysis as the collection does, raising ElementAdded, without saving it.
            internal IAnalysisElement AddAnalysis(string name)
            {
                var analysis = new MixtureAnalysis(name, Collection, false);
                Store(Collection, analysis);
                typeof(ElementCollectionBase).GetMethod("RaiseElementAddedEvent", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(Collection, new object[] { analysis });
                return analysis;
            }

            // Raises a result or status notification through the element-to-selector event chain.
            internal void Notify(string propertyName) => RaisePropertyChange.Invoke(Alternative, new object[] { propertyName, false });

            public void Dispose()
            {
                Selector.Element = null;
                WpfTestHost.DrainDispatcher();
            }
        }
    }
}
