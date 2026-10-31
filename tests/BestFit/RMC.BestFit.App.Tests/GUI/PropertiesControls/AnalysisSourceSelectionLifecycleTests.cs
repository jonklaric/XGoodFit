using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using AvalonDock;
using AvalonDock.Layout;
using FrameworkInterfaces;
using FrameworkUI;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Numerics.Data;
using Numerics.Distributions;
using Numerics.Mathematics.Optimization;
using Numerics.Sampling.MCMC;
using RMC.BestFit.Analyses;
using RMC.BestFit.Models;
using RMC.BestFit.UI;
using RMC_BestFit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UiTimeSeriesAnalysis = RMC.BestFit.UI.TimeSeriesAnalysis;
using UiCompositeAnalysis = RMC.BestFit.UI.CompositeAnalysis;
using UiCoincidentAnalysis = RMC.BestFit.UI.CoincidentFrequencyAnalysis;

namespace RMC.BestFit.App.Tests.GUI.PropertiesControls
{
    /// <summary>Protects analysis source selections when docking templates unload and reload properties panels.</summary>
    /// <remarks>
    /// XGoodFit port of upstream's GUI/PropertiesControls/AnalysisSourceSelectionLifecycleTests.cs, run against the
    /// Avalonia panels, docked as the application docks them: in Dock.Avalonia's AvalonDock (the port's docking
    /// manager, with AvalonDock's API) in a headless window, where upstream realized WPF's AvalonDock in an HwndSource.
    /// <list type="bullet">
    /// <item>The cases run in the test's own process, on the headless session's UI thread. Upstream ran them in a
    /// child test process, to give AvalonDock's overlay window one WPF Application for its lifetime; the headless
    /// session has one application for the whole run. The run still reports each case and checks that all 21 ran.</item>
    /// <item>A theme change goes through FrameworkUI's ThemeManager.SetTheme, as the application's View menu changes
    /// it: the application's theme dictionaries change, and the dock follows with its VS2013 dock theme
    /// (FrameworkUI's DockThemeSwitcher, as the main window's ThemeChanged applies it). Upstream replaced the dock's
    /// AvalonDock theme dictionary, as its main window's ThemeChanged did, which made WPF's AvalonDock replace its
    /// templates and so unload the docked panel and load it again. The port's dock theme changes only resources: its
    /// docked panels stay loaded (as in the main window). So, after the theme, the panel is re-templated as upstream's
    /// was, taken out of the dock and put back (the anchorable's content), and each case's theme changes still put
    /// the panel through an unload and a load, in the new theme.</item>
    /// <item>The panel goes into the dock once the dock is in its window, as the main window's panels do: given to an
    /// anchorable before, the port's dock makes it its logical child without attaching it to the logical tree, and
    /// it never loads.</item>
    /// <item>The application's resources are App.axaml's, merged once for the run, with the theme as ThemeManager
    /// sets it (the cases start in the Blue theme, as upstream's dock did). After each case its views and the
    /// Messenger's messages go, where upstream released its test dictionaries.</item>
    /// <item>FindName is FindControl; the visual tree is Avalonia's; and layout and the dispatcher's work settle with
    /// the headless session's render ticks (WPF's UpdateLayout and dispatcher drain).</item>
    /// </list>
    /// The checks are upstream's.
    /// <para>
    /// Uses realized WPF selectors and the production AvalonDock theme dictionaries. Posterior and
    /// uncertainty results are restored deterministic fixtures; no estimator, sampler, or reprocessing runs.
    /// Each project is private and all source additions use the existing in-memory load path.
    /// </para>
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class AnalysisSourceSelectionLifecycleTests
    {
        /// <summary>Reflection scope used exclusively for isolated fixture restoration.</summary>
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        /// <summary>Runs all docked properties lifecycle contracts in one isolated application and STA.</summary>
        /// <remarks>
        /// XGoodFit port: in the test's own process (see the class's remarks). Upstream: AvalonDock owns an internal
        /// overlay Window. Closing its final instance can shut down a WPF Application, while retaining that Application
        /// between MSTest's separate STA threads leaves its dispatcher unavailable. One child test process gives these
        /// realized-window contracts a single application lifetime without changing other tests' shared resources.
        /// </remarks>
        [STATestMethod]
        public void IsolatedDockedProperties_LifecycleContracts()
        {
            XGoodFit.SharedControlsTesting.UseAppResources(ThemeColor.Blue);
            try
            {
                RunIsolatedCases();
            }
            finally
            {
                ThemeManager.SetTheme(ThemeColor.Light);
                XGoodFit.SharedControlsTesting.ReleaseAppResources();
            }
        }

        /// <summary>Executes all deterministic scenarios on the child runner's one STA and reports each result.</summary>
        private static void RunIsolatedCases()
        {
            var failures = new List<string>();
            int passed = 0;
            foreach (string kind in new[] { "TimeSeries", "Composite", "Coincident" })
            {
                RunCase("Theme selection and fit: " + kind, () => ThemeChanges_PreserveSourceResultsAndEditState(kind), failures, ref passed);
                RunCase("Unloaded collection changes: " + kind, () => Reload_ReconcilesUnloadedChangesAndReattachesCollectionHandlers(kind), failures, ref passed);
                RunCase("Loaded source deletion: " + kind, () => SourceDeletion_AfterThemeChange_ClearsDependentSelection(kind, false), failures, ref passed);
                RunCase("Unloaded source deletion: " + kind, () => SourceDeletion_AfterThemeChange_ClearsDependentSelection(kind, true), failures, ref passed);
                RunCase("New-project element replacement: " + kind, () => ElementReplacement_UsesNewProjectSourcesAndIgnoresPreviousCollection(kind), failures, ref passed);
            }
            foreach (string kind in new[] { "Composite", "Coincident" })
            {
                RunCase("None overlay selection: " + kind, () => ThemeChanges_PreserveNoneOverlayAndAllowSubsequentSelection(kind), failures, ref passed);
                RunCase("Secondary analysis selection: " + kind, () => ThemeChanges_PreserveSecondaryAnalysisSelection(kind), failures, ref passed);
            }
            RunCase("Genuine time-series source edit", TimeSeries_UserSelectionAfterReload_StillInvalidatesResults, failures, ref passed);
            RunCase("Realized covariate selection", TimeSeries_RealizedCovariateSelector_PreservesSourceAcrossThemes, failures, ref passed);
            Report($"Lifecycle cases passed: {passed}/21");
            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
            // XGoodFit port: upstream's parent process checked the child's count in its transcript.
            Assert.AreEqual(21, passed, "The worker must execute every named scenario, rather than succeed with an empty filter.");
        }

        /// <summary>Records an individual scenario without preventing independent scenarios from executing.</summary>
        /// <param name="name">The human-readable scenario identity.</param>
        /// <param name="action">The scenario body.</param>
        /// <param name="failures">The accumulated failure details returned by the child process.</param>
        /// <param name="passed">The number of completed successful scenarios.</param>
        private static void RunCase(string name, Action action, List<string> failures, ref int passed)
        {
            try
            {
                action();
                passed++;
                Report("PASS: " + name);
            }
            catch (Exception error)
            {
                string failure = "FAIL: " + name + Environment.NewLine + error;
                failures.Add(failure);
                Report(failure);
            }
            finally
            {
                Cleanup();
            }
        }

        /// <summary>Reports each scenario.</summary>
        /// <param name="message">The scenario result or final completion count.</param>
        /// <remarks>XGoodFit port: to the test's output (upstream's child wrote a receipt for its parent too).</remarks>
        private static void Report(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// XGoodFit port: lets a case's closed views go, with the Messenger's messages (which keep their sources, the
        /// elements, and so the projects and the views on them), where upstream released its test dictionaries.
        /// </summary>
        private static void Cleanup()
        {
            Drain();
            FrameworkInterfaces.Messaging.Messenger.GetInstance().Clear();
            XGoodFit.ViewTesting.Collect();
        }

        /// <summary>
        /// XGoodFit port: runs the dispatcher's work and the headless session's layout and rendering (WPF's
        /// UpdateLayout and dispatcher drain).
        /// </summary>
        private static void Drain() => XGoodFit.SharedControlsTesting.Settle();

        /// <summary>Repeated real docking theme changes must not become source edits or clear a retained fit.</summary>
        /// <param name="kind">The analysis properties panel under test.</param>
        private static void ThemeChanges_PreserveSourceResultsAndEditState(string kind)
        {
            using var fixture = new SelectionFixture(kind);
            foreach (string theme in new[] { "Dark", "Light", "Blue", "Dark" })
            {
                int before = fixture.UnloadCount;
                fixture.ChangeTheme(theme);
                Assert.IsTrue(fixture.UnloadCount > before,
                    "The actual AvalonDock template replacement must unload the panel.");
                Assert.IsTrue(fixture.Control.IsLoaded);
                Assert.IsTrue(fixture.Combo.IsLoaded);
                fixture.AssertSelection(fixture.Source);
                fixture.AssertUnedited();
                fixture.AssertRetainedFit();
                Assert.AreEqual(2, fixture.SourceChoices.Count);
            }
        }

        /// <summary>An optional None overlay stays selectable across reload and retains all available choices.</summary>
        /// <param name="kind">The optional-overlay analysis panel.</param>
        private static void ThemeChanges_PreserveNoneOverlayAndAllowSubsequentSelection(string kind)
        {
            using var fixture = new SelectionFixture(kind);
            fixture.Select(null);
            fixture.ResetEditState();
            fixture.ChangeTheme("Dark");
            fixture.ChangeTheme("Light");
            fixture.AssertSelection(null);
            Assert.AreEqual(3, fixture.Combo.Items.Count, "None and both source choices must remain available.");
            Assert.IsInstanceOfType(fixture.Combo.SelectedItem, typeof(InputDataSelectionItem));
            Assert.IsNull(((InputDataSelectionItem)fixture.Combo.SelectedItem).Value);
            fixture.AssertUnedited();
            fixture.Select(fixture.Alternative);
            fixture.AssertSelection(fixture.Alternative);
            Assert.IsTrue(fixture.Element.IsDirty);
            Assert.IsTrue(fixture.Element.UndoManager.CanUndo);
        }

        /// <summary>Reload reconciles real project changes made while detached and resumes collection notifications.</summary>
        /// <param name="kind">The analysis properties panel under test.</param>
        private static void Reload_ReconcilesUnloadedChangesAndReattachesCollectionHandlers(string kind)
        {
            using var fixture = new SelectionFixture(kind);
            fixture.Detach();
            Assert.IsFalse(fixture.Control.IsLoaded);
            IElement added = fixture.AddSource("Added while detached");
            Assert.IsTrue(fixture.Collection.Remove(fixture.Alternative));
            fixture.Attach();
            fixture.AssertSelection(fixture.Source);
            CollectionAssert.AreEquivalent(new[] { fixture.Source, added }, fixture.SourceChoices.ToArray());
            fixture.AssertUnedited();
            fixture.AssertRetainedFit();

            IElement afterReload = fixture.AddSource("Added after reload");
            Assert.IsTrue(fixture.SourceChoices.Contains(afterReload), "Reload must reattach the collection subscription.");
            Assert.IsTrue(fixture.Collection.Remove(added));
            Assert.IsFalse(fixture.SourceChoices.Contains(added));
            fixture.AssertSelection(fixture.Source);
            fixture.AssertUnedited();
        }

        /// <summary>A real source edit after reload still invalidates the fit and records normal undo state.</summary>
        private static void TimeSeries_UserSelectionAfterReload_StillInvalidatesResults()
        {
            using var fixture = new SelectionFixture("TimeSeries");
            fixture.ChangeTheme("Dark");
            fixture.AssertRetainedFit();
            fixture.Select(fixture.Alternative);
            fixture.AssertSelection(fixture.Alternative);
            var analysis = (UiTimeSeriesAnalysis)fixture.Element;
            Assert.IsFalse(analysis.IsEstimated);
            Assert.IsNull(analysis.BayesianAnalysis.Results);
            Assert.IsNull(analysis.AnalysisResults);
            Assert.IsTrue(analysis.IsDirty);
            Assert.IsTrue(analysis.UndoManager.CanUndo);
        }

        /// <summary>Source deletion notifications still remove choices and clear the actual dependent selection.</summary>
        /// <param name="kind">The analysis properties panel under test.</param>
        /// <param name="unloaded">Whether the deletion arrives while the properties panel is detached.</param>
        private static void SourceDeletion_AfterThemeChange_ClearsDependentSelection(string kind, bool unloaded)
        {
            using var fixture = new SelectionFixture(kind);
            fixture.ChangeTheme("Dark");
            if (unloaded) fixture.Detach();
            // Deliver the genuine post-delete notification without introducing database I/O into this UI contract.
            typeof(ElementBase).GetMethod("RaiseDeleted", PrivateInstance)
                .Invoke(fixture.Source, new object[] { fixture.Source });
            Drain();
            if (unloaded) fixture.Attach();
            fixture.AssertSelection(null);
            Assert.IsFalse(fixture.SourceChoices.Contains(fixture.Source));
            if (fixture.Element is UiTimeSeriesAnalysis analysis)
            {
                Assert.IsFalse(analysis.IsEstimated);
                Assert.IsNull(analysis.BayesianAnalysis.Results);
                Assert.IsNull(analysis.AnalysisResults);
            }
        }

        /// <summary>Realized covariate selectors survive the same template replacements as the response selector.</summary>
        private static void TimeSeries_RealizedCovariateSelector_PreservesSourceAcrossThemes()
        {
            using var fixture = new SelectionFixture("TimeSeries");
            var analysis = (UiTimeSeriesAnalysis)fixture.Element;
            var covariate = new CovariateData { TimeSeriesElement = (TimeSeriesElement)fixture.Alternative };
            analysis.Covariates.Add(covariate);
            fixture.Control.FindControl<TabItem>("Options_TabItem").IsSelected = true;
            var grid = fixture.Control.FindControl<DataGrid>("CovariateDataGrid");
            grid.UpdateLayout();
            grid.ScrollIntoView(covariate, null);
            Drain();
            Assert.IsTrue(Descendants<ComboBox>(grid).Any(combo => ReferenceEquals(combo.SelectedItem, fixture.Alternative)),
                "The covariate selector must be realized, not merely declared in an unloaded template.");
            fixture.RestoreFit();
            fixture.ResetEditState();

            foreach (string theme in new[] { "Dark", "Light", "Blue" })
            {
                fixture.ChangeTheme(theme);
                Assert.AreSame(covariate, analysis.Covariates.Single());
                Assert.AreSame(fixture.Alternative, covariate.TimeSeriesElement);
                Assert.AreSame(fixture.Source, analysis.TimeSeriesData);
                Assert.IsTrue(Descendants<ComboBox>(grid).Any(combo => ReferenceEquals(combo.SelectedItem, fixture.Alternative)));
                fixture.AssertRetainedFit();
                fixture.AssertUnedited();
            }
        }

        /// <summary>Refreshing a panel preserves its child-analysis selections as well as its optional overlay.</summary>
        /// <param name="kind">The Composite or Coincident properties panel.</param>
        private static void ThemeChanges_PreserveSecondaryAnalysisSelection(string kind)
        {
            using var fixture = new SelectionFixture(kind);
            IElement child = fixture.AddSecondarySource();
            ComboBox combo;
            Func<object> getSelection;
            int writes = 0;
            if (fixture.Element is UiCompositeAnalysis composite)
            {
                var row = new RMC.BestFit.UI.WeightedUnivariateAnalysis
                {
                    UnivariateAnalysis = (RMC.BestFit.UI.UnivariateAnalysis)child,
                    Weight = 1
                };
                composite.Analyses.Add(row);
                var grid = fixture.Control.FindControl<DataGrid>("CompositeFunctionDataGrid");
                grid.UpdateLayout();
                grid.ScrollIntoView(row, null);
                Drain();
                combo = Descendants<ComboBox>(grid).Single(candidate => ReferenceEquals(candidate.SelectedValue, child));
                getSelection = () => row.UnivariateAnalysis;
                row.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(row.UnivariateAnalysis)) writes++;
                };
            }
            else
            {
                var coincident = (UiCoincidentAnalysis)fixture.Element;
                coincident.BivariateAnalysis = (RMC.BestFit.UI.BivariateAnalysis)child;
                combo = (ComboBox)((Border)fixture.Control.FindControl<ContentPropertyControl>("AnalysisComboBox").InnerContent).Child;
                getSelection = () => coincident.BivariateAnalysis;
                coincident.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(coincident.BivariateAnalysis)) writes++;
                };
            }
            Drain();
            Assert.IsTrue(combo.IsLoaded, "The secondary selector must participate in the real visual lifecycle.");
            Assert.AreSame(child, combo.SelectedValue);
            fixture.ResetEditState();
            foreach (string theme in new[] { "Dark", "Light", "Blue" })
            {
                fixture.ChangeTheme(theme);
                Assert.AreSame(child, getSelection());
                fixture.AssertSelection(fixture.Source);
                fixture.AssertUnedited();
                Assert.AreEqual(0, writes, "A child reference must never be cleared and then restored during refresh.");
                if (fixture.Control is CompositeAnalysisPropertiesControl properties)
                {
                    Assert.IsTrue(properties.UnivariateAnalysisList.Contains((RMC.BestFit.UI.IUnivariate)child));
                    var grid = fixture.Control.FindControl<DataGrid>("CompositeFunctionDataGrid");
                    Assert.IsTrue(Descendants<ComboBox>(grid).Any(candidate => ReferenceEquals(candidate.SelectedValue, child)));
                }
                else
                {
                    Assert.AreSame(child, combo.SelectedValue);
                    Assert.IsTrue(((CoincidentFrequencyPropertiesControl)fixture.Control).BivariateAnalysisList.Contains((RMC.BestFit.UI.BivariateAnalysis)child));
                }
            }
        }

        /// <summary>Reusing a loaded panel switches source collections without mutating either selected element.</summary>
        /// <param name="kind">The analysis properties panel under test.</param>
        private static void ElementReplacement_UsesNewProjectSourcesAndIgnoresPreviousCollection(string kind)
        {
            using var fixture = new SelectionFixture(kind);
            using var replacement = new SelectionFixture(kind);
            replacement.Detach();
            fixture.SetControlElement(replacement.Element);
            Drain();
            Assert.AreSame(replacement.Source, fixture.Combo.SelectedValue);
            Assert.AreSame(replacement.Source, replacement.SelectedSource);
            CollectionAssert.AreEquivalent(new[] { replacement.Source, replacement.Alternative }, fixture.SourceChoices.ToArray());
            replacement.AssertUnedited();
            replacement.AssertRetainedFit();
            fixture.AssertRetainedFit();
            // XGoodFit addition: nor is the previous element edited, of any kind (upstream checks the time series' fit,
            // which the port's panels lost, with the Composite and Coincident panels' input data, to the selectors'
            // write-back; see the panels' ReconcileWithoutPreviousElement).
            Assert.AreSame(fixture.Source, fixture.SelectedSource);
            fixture.AssertUnedited();
            IElement stale = fixture.AddSource("Old project source");
            Assert.IsFalse(fixture.SourceChoices.Contains(stale));
            IElement current = replacement.AddSource("Current project source");
            Assert.IsTrue(fixture.SourceChoices.Contains(current));
            fixture.ChangeTheme("Dark");
            Assert.AreSame(replacement.Source, fixture.Combo.SelectedValue);
            replacement.AssertUnedited();
            replacement.AssertRetainedFit();
        }

        /// <summary>Enumerates realized visual descendants, excluding deferred templates.</summary>
        /// <typeparam name="T">The visual type requested.</typeparam>
        /// <param name="root">The realized visual root.</param>
        /// <returns>Matching descendants in the actual visual tree.</returns>
        /// <remarks>XGoodFit port: Avalonia's visual tree (WPF's VisualTreeHelper).</remarks>
        private static IEnumerable<T> Descendants<T>(Visual root) where T : Visual => root.GetVisualDescendants().OfType<T>();

        /// <summary>Owns one private project and a fully realized docked properties panel.</summary>
        private sealed class SelectionFixture : IDisposable
        {
            /// <summary>The analysis kind used to create matching sources and controls.</summary>
            private readonly string _kind;
            /// <summary>The isolated project; the application singleton is never used.</summary>
            private readonly BestFitProject _project;
            /// <summary>
            /// XGoodFit port: the headless window that realizes the dock's templates (upstream's native presentation
            /// source).
            /// </summary>
            private readonly Window _window;
            /// <summary>The real docking manager whose theme changes rebuild the dock templates.</summary>
            private readonly DockingManager _dock;
            /// <summary>The actual dock content container used for detach and reattach.</summary>
            private readonly LayoutAnchorable _anchor;
            /// <summary>
            /// XGoodFit port: gives the dock the application's dock theme as the theme changes, as the main window's
            /// ThemeChanged does (upstream: the AvalonDock dictionary replaced as the application shell replaced it).
            /// </summary>
            private readonly DockThemeSwitcher _dockTheme;
            /// <summary>The retained posterior object restored into a time-series analysis.</summary>
            private MCMCResults _posterior;
            /// <summary>The retained uncertainty result object restored into a time-series analysis.</summary>
            private UncertaintyAnalysisResults _results;
            /// <summary>Original posterior values for detecting in-place mutation.</summary>
            private double[] _draws;
            /// <summary>Original uncertainty values for detecting in-place mutation.</summary>
            private double[] _intervals;
            /// <summary>The number of source-property writes after initial setup.</summary>
            private int _sourceWrites;

            /// <summary>Creates and realizes a control with two valid in-memory source choices.</summary>
            /// <param name="kind">TimeSeries, Composite, or Coincident.</param>
            public SelectionFixture(string kind)
            {
                _kind = kind;
                _project = (BestFitProject)Activator.CreateInstance(typeof(BestFitProject), nonPublic: true);
                Collection = kind == "TimeSeries"
                    ? _project.ElementCollections.OfType<TimeSeriesCollection>().Single()
                    : _project.ElementCollections.OfType<InputDataCollection>().Single();
                Source = AddSource("Primary source");
                Alternative = AddSource("Alternative source");
                if (kind == "TimeSeries")
                {
                    Element = new UiTimeSeriesAnalysis("Time series", _project.ElementCollections.OfType<TimeSeriesAnalysisCollection>().Single())
                    { TimeSeriesData = (TimeSeriesElement)Source };
                    Control = new TimeSeriesAnalysisPropertiesControl { Element = (UiTimeSeriesAnalysis)Element };
                }
                else if (kind == "Composite")
                {
                    Element = new UiCompositeAnalysis("Composite", _project.ElementCollections.OfType<UnivariateAnalysisCollection>().Single())
                    { InputData = (InputData)Source };
                    Control = new CompositeAnalysisPropertiesControl { Element = (UiCompositeAnalysis)Element };
                }
                else
                {
                    Element = new UiCoincidentAnalysis("Coincident", _project.ElementCollections.OfType<BivariateAnalysisCollection>().Single())
                    { InputData = (InputData)Source };
                    Control = new CoincidentFrequencyPropertiesControl { Element = (UiCoincidentAnalysis)Element };
                }
                string comboName = kind == "TimeSeries" ? "TimeSeriesDataComboBox" : "InputDataComboBox";
                Combo = (ComboBox)((Border)Control.FindControl<ContentPropertyControl>(comboName).InnerContent).Child;
                Control.Unloaded += (_, _) => UnloadCount++;
                _anchor = new LayoutAnchorable { Title = "Properties", IsSelected = true };
                var pane = new LayoutAnchorablePane();
                pane.Children.Add(_anchor);
                // XGoodFit port: the dock starts in the Blue theme, as upstream's did.
                if (ThemeManager.CurrentThemeColor != ThemeColor.Blue) ThemeManager.SetTheme(ThemeColor.Blue);
                _dock = new DockingManager();
                _dockTheme = new DockThemeSwitcher(_dock);
                // Realize templates in a native presentation source without opening or activating
                // a desktop shell. All scenarios share the isolated worker's application and STA.
                // XGoodFit port: a headless window of the same size. The layout, then the panel, go into the dock once
                // it is in the window, as the main window's do (see the class's remarks).
                _window = new Window { Width = 650, Height = 900, Content = _dock };
                XGoodFit.ViewTesting.ReleaseContentOnClose(_window);
                _window.Show();
                Drain();
                _dock.Layout = new LayoutRoot { RootPanel = new LayoutPanel(pane) };
                Drain();
                Attach();
                // Establish the retained state after the initial realization; the regression concerns a later reload.
                Select(Source);
                RestoreFit();
                Element.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == "TimeSeriesData" || e.PropertyName == "InputData") _sourceWrites++;
                };
                ResetEditState();
                Assert.IsTrue(Control.IsLoaded);
                Assert.IsTrue(Combo.IsLoaded);
                AssertSelection(Source);
            }

            /// <summary>Gets the collection supplying the source selector.</summary>
            public ElementCollectionBase Collection { get; }
            /// <summary>Gets the source initially assigned to the analysis.</summary>
            public IElement Source { get; }
            /// <summary>Gets a second valid source for deliberate selection changes.</summary>
            public IElement Alternative { get; }
            /// <summary>Gets the original analysis object.</summary>
            public ElementBase Element { get; }
            /// <summary>Gets the realized properties control.</summary>
            public UserControl Control { get; }
            /// <summary>Gets the actual two-way-bound source selector.</summary>
            public ComboBox Combo { get; }
            /// <summary>Gets the number of real Unloaded events observed by the properties panel.</summary>
            public int UnloadCount { get; private set; }
            /// <summary>Gets the source stored by the original analysis model.</summary>
            public IElement SelectedSource => Element switch
            {
                UiTimeSeriesAnalysis analysis => analysis.TimeSeriesData,
                UiCompositeAnalysis analysis => analysis.InputData,
                UiCoincidentAnalysis analysis => analysis.InputData,
                _ => throw new InvalidOperationException("Unexpected analysis type.")
            };
            /// <summary>Gets the current non-None choices presented by the properties panel.</summary>
            public IReadOnlyList<IElement> SourceChoices => Control switch
            {
                TimeSeriesAnalysisPropertiesControl properties => properties.TimeSeriesElements.Cast<IElement>().ToArray(),
                CompositeAnalysisPropertiesControl properties => properties.InputDataList.Where(item => item.Value != null).Select(item => (IElement)item.Value).ToArray(),
                CoincidentFrequencyPropertiesControl properties => properties.InputDataList.Where(item => item.Value != null).Select(item => (IElement)item.Value).ToArray(),
                _ => throw new InvalidOperationException("Unexpected properties type.")
            };

            /// <summary>Creates and adds a valid source using the ordinary collection load path without saving.</summary>
            /// <param name="name">A unique source name in this private project.</param>
            /// <returns>The newly added source.</returns>
            public IElement AddSource(string name)
            {
                IElement source;
                if (_kind == "TimeSeries")
                {
                    var data = new TimeSeries(TimeInterval.OneYear, new DateTime(1960, 1, 1), new DateTime(2019, 1, 1));
                    for (int i = 0; i < data.Count; i++) data[i].Value = 100 + i + i % 3;
                    source = new TimeSeriesElement(name, Collection) { TimeSeries = data };
                }
                else
                {
                    var frame = new DataFrame();
                    for (int i = 0; i < 20; i++) frame.ExactSeries.Add(new ExactData(1980 + i, 100 + 3 * i));
                    frame.CalculatePlottingPositions();
                    source = new InputData(name, Collection) { DataFrame = frame };
                }
                AddWithoutSaving(Collection, source);
                Assert.IsTrue(source.IsValid, "The source fixture must be a valid selectable project element.");
                Drain();
                return source;
            }

            /// <summary>Adds a child analysis for a Composite or Coincident panel without estimating or saving it.</summary>
            /// <returns>The child analysis available to the panel's secondary selector.</returns>
            public IElement AddSecondarySource()
            {
                ElementCollectionBase collection;
                IElement source;
                if (_kind == "Composite")
                {
                    collection = _project.ElementCollections.OfType<UnivariateAnalysisCollection>().Single();
                    source = new RMC.BestFit.UI.UnivariateAnalysis("Univariate child", collection)
                    { InputData = (InputData)Source };
                }
                else
                {
                    collection = _project.ElementCollections.OfType<BivariateAnalysisCollection>().Single();
                    source = new RMC.BestFit.UI.BivariateAnalysis("Bivariate child", collection);
                }
                AddWithoutSaving(collection, source);
                Drain();
                return source;
            }

            /// <summary>Performs a user-equivalent selection through the actual two-way binding.</summary>
            /// <param name="source">The desired source, or null for None.</param>
            public void Select(IElement source)
            {
                if (_kind == "TimeSeries") Combo.SelectedItem = source;
                else Combo.SelectedItem = Combo.Items.Cast<InputDataSelectionItem>().Single(item => ReferenceEquals(item.Value, source));
                Drain();
            }

            /// <summary>Rebinds the existing properties control to another same-kind analysis.</summary>
            /// <param name="element">The replacement analysis, or null during cleanup.</param>
            public void SetControlElement(ElementBase element)
            {
                if (Control is TimeSeriesAnalysisPropertiesControl time) time.Element = (UiTimeSeriesAnalysis)element;
                else if (Control is CompositeAnalysisPropertiesControl composite) composite.Element = (UiCompositeAnalysis)element;
                else ((CoincidentFrequencyPropertiesControl)Control).Element = (UiCoincidentAnalysis)element;
            }

            /// <summary>Replaces the real dock theme exactly as MainWindow.ThemeChanged replaces its dictionary.</summary>
            /// <param name="theme">The Blue, Dark, or Light theme resource name.</param>
            /// <remarks>
            /// XGoodFit port: the application's theme changes (ThemeManager.SetTheme), and the dock follows as the main
            /// window's does; then the panel is re-templated as upstream's dock re-templated it, out of the dock and
            /// back in (see the class's remarks).
            /// </remarks>
            public void ChangeTheme(string theme)
            {
                ThemeManager.SetTheme(Enum.Parse<ThemeColor>(theme));
                Drain();
                Detach();
                Attach();
            }

            /// <summary>Unloads the complete control tree while retaining the same reusable properties object.</summary>
            public void Detach()
            {
                _anchor.Content = null;
                Drain();
            }

            /// <summary>Realizes the retained control again and processes its bindings and lifecycle callbacks.</summary>
            public void Attach()
            {
                _anchor.Content = Control;
                Drain();
            }

            /// <summary>Checks that UI selection and model source agree by reference.</summary>
            /// <param name="expected">The expected source, or null.</param>
            public void AssertSelection(IElement expected)
            {
                Assert.AreSame(expected, SelectedSource, "A view lifecycle transition must not edit the selected model source.");
                Assert.AreSame(expected, Combo.SelectedValue);
            }

            /// <summary>Marks the restored fixture clean and removes setup-only undo entries and source notifications.</summary>
            public void ResetEditState()
            {
                typeof(ElementBase).GetMethod("SetIsDirty", PrivateInstance).Invoke(Element, new object[] { false });
                Element.UndoManager.Clear();
                _sourceWrites = 0;
            }

            /// <summary>Checks that lifecycle work did not masquerade as a user edit.</summary>
            public void AssertUnedited()
            {
                Assert.IsFalse(Element.IsDirty);
                Assert.IsFalse(Element.UndoManager.CanUndo);
                Assert.IsFalse(Element.UndoManager.CanRedo);
                Assert.AreEqual(0, _sourceWrites, "Reload must not write null and then restore the original source.");
            }

            /// <summary>Restores deterministic posterior and uncertainty data without running numerical methods.</summary>
            public void RestoreFit()
            {
                if (Element is not UiTimeSeriesAnalysis time) return;
                var model = (ARIMAXAnalysis)time.InnerAnalysis;
                double[] values = time.ARIMAX.Parameters.Select(parameter => parameter.Value).ToArray();
                _posterior = new MCMCResults(new ParameterSet(values, 0),
                    Enumerable.Range(0, 6).Select(i => new ParameterSet((double[])values.Clone(), -i)).ToList(), alpha: 0.1);
                model.BayesianAnalysis.SetCustomMCMCResults(_posterior, skipInformationCriteria: true);
                int count = ((TimeSeriesElement)Source).TimeSeries.Count;
                var intervals = new double[count, 3];
                for (int i = 0; i < count; i++)
                {
                    intervals[i, 0] = i;
                    intervals[i, 1] = 90 + i;
                    intervals[i, 2] = 130 + i;
                }
                _results = new UncertaintyAnalysisResults
                {
                    ModeCurve = Enumerable.Range(0, count).Select(i => 105.0 + i).ToArray(),
                    MeanCurve = Enumerable.Range(0, count).Select(i => 110.0 + i).ToArray(),
                    ConfidenceIntervals = intervals
                };
                typeof(ARIMAXAnalysis).GetProperty(nameof(ARIMAXAnalysis.AnalysisResults)).GetSetMethod(true)
                    .Invoke(model, new object[] { _results });
                typeof(AnalysisBase).GetProperty(nameof(AnalysisBase.IsEstimated)).GetSetMethod(true)
                    .Invoke(model, new object[] { true });
                _draws = _posterior.Output.SelectMany(draw => draw.Values).ToArray();
                _intervals = intervals.Cast<double>().ToArray();
            }

            /// <summary>Checks both retained object identity and payload contents for the restored time-series fit.</summary>
            public void AssertRetainedFit()
            {
                if (Element is not UiTimeSeriesAnalysis time) return;
                Assert.IsTrue(time.IsEstimated);
                Assert.AreSame(_posterior, time.BayesianAnalysis.Results);
                Assert.AreSame(_results, time.AnalysisResults);
                CollectionAssert.AreEqual(_draws, time.BayesianAnalysis.Results.Output.SelectMany(draw => draw.Values).ToArray());
                CollectionAssert.AreEqual(_intervals, time.AnalysisResults.ConfidenceIntervals.Cast<double>().ToArray());
                CollectionAssert.AreEqual(Enumerable.Range(0, 60).Select(i => 105.0 + i).ToArray(), time.AnalysisResults.ModeCurve);
                CollectionAssert.AreEqual(Enumerable.Range(0, 60).Select(i => 110.0 + i).ToArray(), time.AnalysisResults.MeanCurve);
            }

            /// <summary>Unloads the control, removes its model references, and disposes its native presentation source.</summary>
            /// <remarks>XGoodFit port: the dock stops following the theme, and its window closes.</remarks>
            public void Dispose()
            {
                Detach();
                SetControlElement(null);
                _dockTheme.Dispose();
                _window.Close();
                Drain();
            }

            /// <summary>Adds a fixture element through the production collection's in-memory load branch.</summary>
            /// <param name="collection">The isolated project collection receiving the element.</param>
            /// <param name="source">The new source or child analysis.</param>
            private static void AddWithoutSaving(ElementCollectionBase collection, IElement source)
            {
                FieldInfo opening = typeof(ElementCollectionBase).GetField("_opening", PrivateInstance);
                bool previous = (bool)opening.GetValue(collection);
                opening.SetValue(collection, true);
                try { collection.Add(source); }
                finally { opening.SetValue(collection, previous); }
            }
        }
    }
}
