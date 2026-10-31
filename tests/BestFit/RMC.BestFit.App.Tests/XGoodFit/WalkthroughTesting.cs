using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AvalonDock;
using AvalonDock.Layout;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkInterfaces.Messaging;
using FrameworkUI;
using FrameworkUI.ProjectExplorer;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlot;
using RMC.BestFit.UI;
using RMC_BestFit;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: an example project in the main window for a walkthrough's test class (Walkthrough*Tests): the
    /// window opens a copy of the example when a test first asks for it, on the UI thread, and the class closes it in its
    /// class cleanup, so the tests that only look share it. Each test starts with no documents open (the example's saved
    /// layout reopens some), as a user who closed them would.
    /// </summary>
    internal sealed class WalkthroughSession
    {
        private readonly string _example;

        /// <summary>Names the example.</summary>
        /// <param name="example">The example project's file name, without the extension (see <see cref="BestFitShell.Example"/>).</param>
        internal WalkthroughSession(string example) => _example = example;

        /// <summary>The main window.</summary>
        internal MainWindow Window { get; private set; }

        /// <summary>The explorer's project node.</summary>
        internal MainProjectNode Node { get; private set; }

        /// <summary>The application's project, the example's copy.</summary>
        internal BestFitProject Project { get; private set; }

        /// <summary>The windows that opened unexpected since the test began (see <see cref="Expect{T}"/>), by title.</summary>
        internal List<string> Dialogs { get; } = new List<string>();

        private readonly HashSet<Type> _expected = new HashSet<Type>();
        private IDisposable _guard;

        /// <summary>
        /// Opens the example in the main window if no test has yet, and closes the documents open. Any other window that
        /// opens, a test doesn't expect (<see cref="Expect{T}"/>) and nobody answers would wait for an answer and hang the
        /// test: it's noted in <see cref="Dialogs"/> and closed, and <see cref="AssertUnchanged"/> fails on it.
        /// </summary>
        /// <returns>This session.</returns>
        internal WalkthroughSession Open()
        {
            _guard ??= Avalonia.Controls.Window.WindowOpenedEvent.AddClassHandler<Window>((opened, _) =>
            {
                if (opened is MainWindow || _expected.Contains(opened.GetType())) return;
                int at = Dialogs.Count;
                Dialogs.Add(opened.Title ?? opened.GetType().Name);
                // Its texts once it's laid out, then close it.
                Dispatcher.UIThread.Post(() =>
                {
                    Dialogs[at] += ": " + string.Join(" ", opened.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Where(t => !string.IsNullOrEmpty(t)));
                    opened.Close();
                }, DispatcherPriority.Background);
            });
            Dialogs.Clear();
            if (Window == null)
            {
                BestFitShell.Start();
                (Window, Node, Project) = BestFitShell.Open(BestFitShell.Example(_example));
            }
            CloseDocuments();
            return this;
        }

        /// <summary>Lets windows of type T open, for a test that answers them (with <see cref="BestFitShell.WhenOpened{T}"/>), until disposed.</summary>
        internal IDisposable Expect<T>() where T : Window
        {
            _expected.Add(typeof(T));
            return new Expected(() => _expected.Remove(typeof(T)));
        }

        private sealed class Expected(Action end) : IDisposable
        {
            public void Dispose() => end();
        }

        /// <summary>Closes the window, if a test opened it, on the UI thread.</summary>
        internal void Close()
        {
            _guard?.Dispose();
            _guard = null;
            MainWindow window = Window;
            Window = null;
            Node = null;
            Project = null;
            if (window == null) return;
            void Close()
            {
                BestFitShell.Close(window);
                // The shell clears the Message Window when it opens a project (MainWindow.OpenProject).
                Messenger.GetInstance().Clear();
            }
            if (Dispatcher.UIThread.CheckAccess())
                Close();
            else
                HeadlessSession.Run(Close);
            GC.Collect();
        }

        /// <summary>The project's element named <paramref name="name"/>.</summary>
        internal T Element<T>(string name) where T : class, IElement =>
            Project.ElementCollections.SelectMany(c => c.OfType<T>()).Single(e => e.Name == name);

        /// <summary>
        /// Fails if an element that a test only looked at changed: it's dirty, or has something to undo. (The project can be
        /// dirty: another test of the class may have added an element and taken it out again.)
        /// </summary>
        internal void AssertUnchanged(params string[] names)
        {
            BestFitShell.Frame();
            Assert.AreEqual(0, Dialogs.Count, "no window opened" + DialogsText());
            foreach (string name in names)
            {
                IElement element = Element<IElement>(name);
                Assert.IsFalse(element.IsDirty, name + " is unchanged");
                if (element is ElementBase undoable)
                    Assert.IsFalse(undoable.UndoManager.CanUndo, name + " has nothing to undo: " + undoable.UndoManager.UndoDescription);
            }
        }

        /// <summary>
        /// Deletes an element a test added from the shared project again, as a user does: Delete... on its node's context
        /// menu, and OK to the question; the shell closes its document. Then closes the other documents.
        /// </summary>
        internal void Remove(IElement element)
        {
            IElementCollection collection = element.ParentCollection;
            ElementNode node = NodeOf(element);
            Window.RightClick(node.NodeHeader);
            Assert.IsTrue(node.ContextMenu!.IsOpen, "a right click opens the node's menu");
            MessageBoxWindow question = null;
            using (Expect<MessageBoxWindow>())
            using (BestFitShell.WhenOpened<MessageBoxWindow>(opened =>
            {
                question = opened;
                BestFitShell.ClickButton(opened, "OK");
            }))
            {
                BestFitShell.Click(node.ContextMenu.Items.OfType<MenuItem>().Single(m => m.Header as string == "Delete..."));
            }
            node.ContextMenu.Close();
            BestFitShell.Frame();
            Assert.IsNotNull(question, "Delete asks first");
            Assert.IsFalse(collection.Contains(element), element.Name + " is deleted");
            CloseDocuments();
        }

        private string DialogsText() => Dialogs.Count == 0 ? "" : "; windows that opened: " + string.Join(" | ", Dialogs);

        /// <summary>The explorer's node of <paramref name="element"/>, its collection expanded so that it shows.</summary>
        internal ElementNode NodeOf(IElement element)
        {
            ElementNode node = ElementNode.FindElementNode(element, Node);
            Assert.IsNotNull(node, element.Name + " is in the project explorer");
            for (Node parent = node.ParentNode; parent != null; parent = parent.ParentNode)
                parent.IsExpanded = true;
            BestFitShell.Frame();
            return node;
        }

        /// <summary>
        /// Opens <paramref name="element"/>'s document as a user does, with a double click on its node in the explorer,
        /// and gives its view; its properties show beside it (<see cref="Properties"/>).
        /// </summary>
        internal T OpenDocument<T>(IElement element) where T : Control
        {
            Window.DoubleClick(NodeOf(element).NodeHeader);
            BestFitShell.Frame();
            LayoutDocument document = Documents().SingleOrDefault(d => d.Content is Control c && ReferenceEquals(Node.GetControlElement(c), element));
            Assert.IsNotNull(document, element.Name + ": its document opened");
            Assert.IsTrue(document.IsActive, element.Name + ": its document is the active one");
            return (T)document.Content;
        }

        /// <summary>Opens the document of the element named <paramref name="name"/> (see <see cref="OpenDocument{T}(IElement)"/>).</summary>
        internal T OpenDocument<T>(string name) where T : Control => OpenDocument<T>(Element<IElement>(name));

        /// <summary>The properties panel's content: the properties of the active document's element.</summary>
        internal Control Properties
        {
            get
            {
                BestFitShell.Frame();
                LayoutAnchorable pane = Dock().Layout.Descendents().OfType<LayoutAnchorable>()
                    .SingleOrDefault(p => p.ContentId == ShellPublicVariables.PropertiesWindowContentID);
                Assert.IsNotNull(pane, "the properties pane");
                return (Control)pane.Content;
            }
        }

        /// <summary>The documents open.</summary>
        internal List<LayoutDocument> Documents() => Dock().Layout.Descendents().OfType<LayoutDocument>().ToList();

        /// <summary>
        /// Closes every document, as their close buttons do, and lets their views go. Three leaks would otherwise keep every
        /// view a test opened, with a row for each observation of its table (about 1.5 KB each), as long as the window:
        /// <list type="bullet">
        /// <item>upstream's MainProjectNode leaves a closed view subscribed to its element (IssuesToRaise: views not
        /// released after close): the closed views let go of their elements, as upstream's fix would have DocumentClosed
        /// do;</item>
        /// <item>upstream's OxyPlotToolbar attaches handlers to its plot's annotations and never detaches them, so an
        /// element's plot (its annotations, which the element's plot undo manager keeps too) holds every toolbar that showed
        /// it, and the toolbar its view: the closed toolbars' handlers come off (<see cref="ReleaseClosedToolbars"/>);</item>
        /// <item>XGoodFit's docking (Dock.Avalonia.AvalonDock) leaves a closed document's content with the DockingManager
        /// as its logical parent, so the manager's inheritance children keep it (WPF's AvalonDock holds its logical
        /// children by weak reference): the package's own removal takes it out (<see cref="ReleaseFromDock"/>).</item>
        /// </list>
        /// </summary>
        /// <param name="save">
        /// The answer to the question about the changes of a document's element that a test changed on purpose: Save or
        /// Don't Save; null when there should be none.
        /// </param>
        internal void CloseDocuments(string save = null)
        {
            List<LayoutDocument> open = Documents();
            if (open.Count == 0) return;
            var views = open.Select(d => d.Content).OfType<Control>().ToList();
            var elements = views.Select(v => v.GetType().GetProperty("Element")?.GetValue(v)).OfType<IElement>().Distinct().ToList();
            using (save == null ? null : Expect<SaveElementsDialog>())
            using (save == null ? null : BestFitShell.WhenOpened<SaveElementsDialog>(dialog => BestFitShell.ClickButton(dialog, save)))
            {
                foreach (LayoutDocument document in open)
                    document.Close();
                BestFitShell.Frame();
            }
            foreach (Control view in views)
            {
                view.GetType().GetProperty("Element")?.SetValue(view, null);
                ReleaseFromDock(view);
            }
            BestFitShell.Frame();
            foreach (IElement element in elements)
                ReleaseClosedToolbars(element);
            Assert.AreEqual(0, Documents().Count, "the documents closed: " + string.Join(", ", Documents().Select(d => d.Title)) + DialogsText());
            ViewTesting.Collect();
        }

        /// <summary>
        /// Takes a closed document's content out of the DockingManager's logical children, which Dock.Avalonia.AvalonDock
        /// (12.1.0.13) leaves it in: its internal InternalRemoveLogicalChild, or, should a later version not have it, the
        /// logical parent cleared.
        /// </summary>
        internal static void ReleaseFromDock(Control view)
        {
            if (((Avalonia.LogicalTree.ILogical)view).LogicalParent is not DockingManager dock) return;
            MethodInfo remove = typeof(DockingManager).GetMethod("InternalRemoveLogicalChild", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (remove != null)
                remove.Invoke(dock, new object[] { view });
            else
                ((ISetLogicalParent)view).SetParent(null);
        }

        /// <summary>
        /// Takes the handlers of toolbars no longer in a window (closed views') off the annotations of an element's plots
        /// (its properties of type Plot, and those of its BayesianPlots): upstream's OxyPlotToolbar.AttachAnnotationHandlers
        /// adds lambdas that capture the toolbar to each annotation's MouseDown, MouseMove and MouseUp, and nothing removes
        /// them. An open view's toolbar keeps its handlers.
        /// </summary>
        internal static void ReleaseClosedToolbars(object element)
        {
            var owners = new List<object> { element };
            if (element.GetType().GetProperty("BayesianPlots")?.GetValue(element) is { } bayesian) owners.Add(bayesian);
            foreach (object owner in owners)
            {
                foreach (PropertyInfo property in owner.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => typeof(Wpf.Plot).IsAssignableFrom(p.PropertyType) && p.GetIndexParameters().Length == 0))
                {
                    if (property.GetValue(owner) is not Wpf.Plot plot) continue;
                    foreach (Wpf.Annotation annotation in plot.Annotations)
                        if (annotation.InternalAnnotation is { } core)
                            RemoveClosedToolbarHandlers(core);
                }
            }
        }

        // Removes the delegates whose closures capture a toolbar outside any window from the object's event fields.
        private static void RemoveClosedToolbarHandlers(object target)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(f => typeof(Delegate).IsAssignableFrom(f.FieldType)))
                {
                    if (field.GetValue(target) is not Delegate handlers) continue;
                    Delegate kept = handlers;
                    foreach (Delegate handler in handlers.GetInvocationList())
                        if (ToolbarOf(handler.Target, 0) is { } toolbar && TopLevel.GetTopLevel(toolbar) == null)
                            kept = Delegate.Remove(kept, handler);
                    if (!ReferenceEquals(kept, handlers)) field.SetValue(target, kept);
                }
            }
        }

        // The toolbar a compiler-made closure of OxyPlotToolbar captures, directly or through an outer closure.
        private static OxyPlotControls.OxyPlotToolbar ToolbarOf(object closure, int depth)
        {
            if (closure is OxyPlotControls.OxyPlotToolbar toolbar) return toolbar;
            if (closure == null || depth > 3 || closure.GetType().DeclaringType != typeof(OxyPlotControls.OxyPlotToolbar)) return null;
            foreach (FieldInfo field in closure.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (ToolbarOf(field.GetValue(closure), depth + 1) is { } found) return found;
            return null;
        }

        /// <summary>
        /// Expands every collection in the explorer, as the walkthroughs' explorer captures show it, and gives the headers
        /// it shows, in order.
        /// </summary>
        internal List<string> ExplorerHeaders()
        {
            foreach (Node collection in Node.ChildNodes)
                collection.IsExpanded = true;
            BestFitShell.Frame();
            var tree = Node.FindAncestorOfType<TreeView>();
            Assert.IsNotNull(tree, "the explorer's tree");
            return tree.GetVisualDescendants().OfType<TextBlock>()
                .Where(t => t.Name == "HeaderTextBlock" && t.IsEffectivelyVisible && !string.IsNullOrEmpty(t.Text))
                .Select(t => t.Text).ToList();
        }

        private DockingManager Dock() => Window.FindControl<DockingManager>("MainDock");
    }

    /// <summary>
    /// XGoodFit addition: what the walkthroughs' tests read from the views (texts, as a user reads them), and the
    /// walkthroughs' captures and figures beside the examples.
    /// </summary>
    internal static class WalkthroughTesting
    {
        /// <summary>The walkthroughs' (and XGoodFit's bound fields') formats: en-US (decision D8).</summary>
        internal static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        /// <summary>A capture or figure beside an example: its data, from the .json.gz file under the examples folder.</summary>
        /// <param name="relative">The file's path under the examples folder.</param>
        internal static JsonElement Data(string relative)
        {
            using var stream = new GZipStream(File.OpenRead(SharedControlsTesting.Example(relative)), CompressionMode.Decompress);
            using var document = JsonDocument.Parse(stream);
            return document.RootElement.Clone();
        }

        /// <summary>
        /// The explorer shows what RMC-BestFit's explorer showed in a walkthrough's capture of it
        /// (screenshots/*-project-explorer-current.json.gz): every collection expanded, its elements in order.
        /// </summary>
        internal static void AssertExplorer(WalkthroughSession session, string capture)
        {
            JsonElement data = Data(capture);
            JsonElement configuration = data.GetProperty("configuration");
            Assert.AreEqual(session.Node.ChildNodes.Count, configuration.GetProperty("expandedFolders").GetArrayLength(), "every collection is expanded in the capture");
            var expected = configuration.GetProperty("visibleControls").EnumerateArray().Select(c => c.GetProperty("text").GetString()).ToList();
            // The project's name is the copy's file name, which is the example's.
            Assert.AreEqual(string.Join(" | ", expected), string.Join(" | ", session.ExplorerHeaders()), capture);
        }

        /// <summary>
        /// A properties panel shows the texts RMC-BestFit's panel showed in a walkthrough's capture of it
        /// (screenshots/*-current.json.gz), in order: the titles, the values, and a combo box's selected item (its text
        /// block). The Created On and Last Modified dates aren't compared: the examples store them in en-US, which this
        /// machine's culture may not read (an upstream issue, IssuesToRaise).
        /// </summary>
        internal static void AssertPanel(Control panel, string capture)
        {
            JsonElement data = Data(capture);
            var expected = data.GetProperty("configuration").GetProperty("visibleControls").EnumerateArray()
                .Where(c => c.GetProperty("type").GetString() != "ComboBox")
                .Select(c => c.GetProperty("text").GetString())
                .ToList();
            Assert.AreEqual(string.Join(" | ", WithoutDates(expected)), Plain(string.Join(" | ", WithoutDates(TimeSeriesDataTesting.VisibleTexts(panel)))), capture);
        }

        private static List<string> WithoutDates(List<string> texts)
        {
            var result = new List<string>(texts);
            foreach (string title in new[] { "Created On", "Last Modified" })
            {
                int at = result.IndexOf(title);
                if (at >= 0 && at + 1 < result.Count) result[at + 1] = "(date)";
            }
            return result;
        }

        /// <summary>The text a properties panel shows after its row titled <paramref name="title"/>: the row's value.</summary>
        internal static string Property(Control panel, string title)
        {
            List<string> texts = TimeSeriesDataTesting.VisibleTexts(panel);
            int at = texts.IndexOf(title);
            Assert.IsTrue(at >= 0 && at + 1 < texts.Count, "the panel shows a row titled " + title + ": " + string.Join(" | ", texts));
            return texts[at + 1];
        }

        /// <summary>The text a grid cell shows.</summary>
        internal static string CellText(CopyPasteDataGrid grid, int row, int column)
        {
            DataGridCell cell = grid.GetCell(row, column);
            Assert.IsNotNull(cell, $"the grid's cell {row}, {column}");
            return cell.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()?.Text ?? "";
        }

        /// <summary>
        /// The statistics a grid of (name, value) rows shows (the Summary Statistics tabs), by name: the first two columns'
        /// texts.
        /// </summary>
        internal static Dictionary<string, string> Statistics(CopyPasteDataGrid grid)
        {
            var shown = new Dictionary<string, string>();
            for (int row = 0; row < grid.Items.Count; row++)
                shown[CellText(grid, row, 0)] = CellText(grid, row, 1);
            return shown;
        }

        /// <summary>Selects a tab, as a click on its header does, and lets the view update.</summary>
        internal static void Select(TabItem tab)
        {
            Assert.IsTrue(tab.IsVisible, (tab.Header as string) + " is shown");
            ((TabControl)tab.Parent).SelectedItem = tab;
            BestFitShell.Frame();
        }

        /// <summary>The tab of <paramref name="tabs"/> headed <paramref name="header"/>.</summary>
        internal static TabItem Tab(TabControl tabs, string header) =>
            tabs.Items.OfType<TabItem>().Single(t => t.Header as string == header);

        /// <summary>The headers of the tabs <paramref name="tabs"/> shows.</summary>
        internal static List<string> Headers(TabControl tabs) =>
            tabs.Items.OfType<TabItem>().Where(t => t.IsVisible).Select(t => t.Header as string).ToList();

        /// <summary>Raises a button's Click, as a click on it does (see the shell tests), and lets the shell respond.</summary>
        internal static void Click(Button button)
        {
            Assert.IsTrue(button.IsEffectivelyEnabled, (button.Name ?? "the button") + " is enabled");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            BestFitShell.Frame();
        }

        /// <summary>
        /// A text as Windows writes it: ICU's en-US times (Linux, macOS) put a narrow no-break space before AM and PM where
        /// Windows has a plain space.
        /// </summary>
        internal static string Plain(string text) => text?.Replace((char)0x202F, ' ');

        /// <summary>A date the views show (WPF's binding culture, en-US), as a date.</summary>
        internal static DateTime UsDate(string text) => DateTime.Parse(Plain(text), Us);

        /// <summary>A number the views show (en-US), as a number.</summary>
        internal static double UsNumber(string text) => double.Parse(text, NumberStyles.Float | NumberStyles.AllowThousands, Us);

        /// <summary>
        /// Fails unless <paramref name="shown"/>, a number a view shows, and the walkthrough's <paramref name="walkthrough"/>,
        /// written to <paramref name="decimals"/> places, can be roundings of the same value: they differ by no more than
        /// half a unit of each one's last place.
        /// </summary>
        internal static void AssertRounded(double walkthrough, string shown, int decimals, string what)
        {
            double value = UsNumber(shown);
            double tolerance = (Math.Pow(10, -decimals) + Math.Pow(10, -Decimals(shown))) / 2 * (1 + 1E-9) + Math.Abs(walkthrough) * 1E-15;
            Assert.AreEqual(walkthrough, value, tolerance, $"{what}: the walkthrough's {walkthrough.ToString(Us)}, shown as {shown}");
        }

        // The decimal places a number's text shows.
        private static int Decimals(string text)
        {
            int point = text.IndexOf('.');
            if (point < 0) return 0;
            int end = point + 1;
            while (end < text.Length && char.IsDigit(text[end])) end++;
            return end - point - 1;
        }

        /// <summary>
        /// Checks a plot against a walkthrough's figure of it (figures/ or images/*.plotspec.json.gz, drawn from the desktop's
        /// coordinates): each of the figure's series, by title, with its points (dates as OxyPlot's date numbers; an area's or
        /// a band's lower and upper values). The plot's empty series (an input's missing kinds of data, the seasonality's
        /// minimum and maximum lines) aren't in the figures. The figures relabel some series (their display corrections): the
        /// seasonality bands, which RMC-BestFit titles confidence intervals, and any "labeled X rather than Y": the titles are
        /// mapped back to RMC-BestFit's.
        /// </summary>
        internal static void AssertFigureData(Wpf.Plot plot, JsonElement figure, string label)
        {
            ((IPlotModel)plot.ActualModel).Update(true);
            var relabelled = new Dictionary<string, string>();
            if (figure.TryGetProperty("displayCorrections", out JsonElement corrections) && corrections.ValueKind == JsonValueKind.Array)
                foreach (JsonElement correction in corrections.EnumerateArray())
                {
                    var match = System.Text.RegularExpressions.Regex.Match(correction.GetString(), @"labeled (.+) rather than (.+)\.$");
                    if (match.Success) relabelled[match.Groups[1].Value] = match.Groups[2].Value;
                }
            string Title(JsonElement series)
            {
                string name = series.GetProperty("name").GetString();
                if (relabelled.TryGetValue(name, out string original)) return original;
                return name.EndsWith("% Observed Range", StringComparison.Ordinal) ? name.Replace("Observed Range", "Confidence Interval", StringComparison.Ordinal) : name;
            }
            var expected = figure.GetProperty("series").EnumerateArray().ToList();
            var drawn = plot.Series.Where(s => Points(s.InternalSeries, "ActualPoints").Count > 0).ToList();
            // A series the project saved with its own title (Grapevine Dam's hydrographs): its factory title's name.
            Wpf.Series Shown(string title) => drawn.SingleOrDefault(s => s.Title == title)
                ?? (FactoryNames.TryGetValue(title, out string name) ? drawn.SingleOrDefault(s => s.Name == name) : null);
            // A figure's straight line (two points) that no series draws is one of the plot's line annotations (the POT
            // diagnostics' threshold), by its text.
            Wpf.LineAnnotation Line(JsonElement series) => series.GetProperty("x").GetArrayLength() == 2 && Shown(Title(series)) == null
                ? plot.Annotations.OfType<Wpf.LineAnnotation>().SingleOrDefault(a => a.Text == Title(series))
                : null;
            var lines = expected.Where(s => Line(s) != null).ToList();
            var others = expected.Except(lines).ToList();
            CollectionAssert.AreEquivalent(others.Select(Title).Select(Shown).ToList(), drawn,
                label + ": the series, " + string.Join(", ", drawn.Select(s => s.Title)) + " for " + string.Join(", ", others.Select(Title)));
            foreach (JsonElement series in lines)
            {
                Wpf.LineAnnotation line = Line(series);
                var x = series.GetProperty("x").EnumerateArray().Select(v => v.GetDouble()).ToList();
                var y = series.GetProperty("y").EnumerateArray().Select(v => v.GetDouble()).ToList();
                if (line.Type == OxyPlot.Annotations.LineAnnotationType.Vertical)
                    foreach (double at in x) UnivariateAnalysisTesting.AssertClose(at, line.X, label + ": " + line.Text + "'s X");
                else if (line.Type == OxyPlot.Annotations.LineAnnotationType.Horizontal)
                    foreach (double at in y) UnivariateAnalysisTesting.AssertClose(at, line.Y, label + ": " + line.Text + "'s Y");
                else
                    for (int i = 0; i < 2; i++) UnivariateAnalysisTesting.AssertClose(y[i], line.Slope * x[i] + line.Intercept, $"{label}: {line.Text} at {x[i]}");
            }
            foreach (JsonElement series in others)
            {
                string title = Title(series);
                Wpf.Series shown = Shown(title);
                bool area = series.GetProperty("kind").GetString() is "area" or "band";
                var x = series.GetProperty("x").EnumerateArray().Select(v => v.ValueKind == JsonValueKind.String ? DateNumber(v) : v.GetDouble()).ToList();
                AssertPoints(x, series.GetProperty(area ? "yLower" : "y"), Points(shown.InternalSeries, "ActualPoints"), label + ": " + title);
                if (area)
                    AssertPoints(x, series.GetProperty("yUpper"), Points(shown.InternalSeries, "ActualPoints2"), label + ": " + title + "'s upper line");
            }
        }

        /// <summary>
        /// The names of the series whose factory titles the figures use, where a project saved a title of its own: the
        /// figures and native exports are drawn from factory-reset plots, the application shows the saved ones.
        /// </summary>
        private static readonly Dictionary<string, string> FactoryNames = new Dictionary<string, string> { ["Time Series Data"] = "TimeSeriesLine" };

        private static double DateNumber(JsonElement value) =>
            OxyPlot.Axes.DateTimeAxis.ToDouble(DateTime.Parse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

        private static void AssertPoints(List<double> x, JsonElement y, List<(double X, double Y)> actual, string label)
        {
            Assert.AreEqual(x.Count, actual.Count, label + ": the number of points");
            for (int i = 0; i < actual.Count; i++)
            {
                UnivariateAnalysisTesting.AssertClose(x[i], actual[i].X, $"{label} {i}.X");
                UnivariateAnalysisTesting.AssertClose(y[i].ValueKind == JsonValueKind.Number ? y[i].GetDouble() : double.NaN, actual[i].Y, $"{label} {i}.Y");
            }
        }

        /// <summary>The (X, Y) of a core series' points: data points or scatter points, from a property that may be protected.</summary>
        internal static List<(double X, double Y)> Points(object series, string property)
        {
            PropertyInfo info = null;
            for (Type type = series.GetType(); type != null && info == null; type = type.BaseType)
                info = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (info == null) return new List<(double X, double Y)>();
            return ((IEnumerable)info.GetValue(series) ?? Array.Empty<object>()).Cast<object>().Select(point => point switch
            {
                DataPoint p => (p.X, p.Y),
                OxyPlot.Series.ScatterPoint p => (p.X, p.Y),
                _ => throw new AssertFailedException("Unexpected point type " + point.GetType().Name),
            }).ToList();
        }
    }
}
