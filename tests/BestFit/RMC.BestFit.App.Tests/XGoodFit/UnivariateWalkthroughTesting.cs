using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using AvalonDock;
using AvalonDock.Layout;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkInterfaces.Messaging;
using FrameworkUI;
using FrameworkUI.ProjectExplorer;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: one of RMC-BestFit's univariate examples (examples/4-univariate-distribution-analysis) opened in
    /// the main window for the tests that follow its walkthrough (the <c>.md</c> beside it), as a user follows it: the
    /// example (a copy) opens once per test class, each test double-clicks the elements it reads in the project explorer,
    /// which opens their documents with their properties beside them, and reads what those show.
    /// </summary>
    /// <remarks>
    /// The window opens with three decimal digits for values (Tools &gt; Options &gt; Defaults), the walkthroughs'
    /// rounding: the frequency tables then show the walkthroughs' numbers as they write them. The tests run in en-US,
    /// in which the examples were saved and the walkthroughs written. Each test closes the documents it opened; the
    /// class closes the window in its class cleanup (<see cref="Close"/>).
    /// </remarks>
    internal sealed class WalkthroughExample
    {
        private readonly string _example;

        /// <summary>Names the example: its project file's name without the extension.</summary>
        internal WalkthroughExample(string example) => _example = example;

        /// <summary>The main window, once a test opened it.</summary>
        internal MainWindow Window { get; private set; }

        /// <summary>The project explorer's node.</summary>
        internal MainProjectNode Node { get; private set; }

        /// <summary>The opened copy of the example.</summary>
        internal BestFitProject Project { get; private set; }

        /// <summary>
        /// Runs a test's steps on the UI thread with the example open (opening it the first time), in en-US, and closes
        /// the documents the steps opened.
        /// </summary>
        internal void Run(Action steps)
        {
            HeadlessSession.Run(() =>
            {
                using IDisposable culture = ViewTesting.UseExamplesCulture();
                // The shell's project is a singleton: a class that follows two examples (Blakely's imperial and metric
                // projects) has one open at a time.
                if (_open != null && _open != this)
                    _open.Close();
                _open = this;
                if (Window == null)
                {
                    BestFitShell.Start();
                    (Window, Node, Project) = BestFitShell.Open(BestFitShell.Example(_example));
                    // As a user who chose three decimal digits in Tools > Options > Defaults (OptionsDialog's OK sets this).
                    UserSettings.DefaultValueDigits = 3;
                    // Upstream's Estimate button disables the main window's menus and panes while an analysis runs.
                    Application.Current!.MainWindow = Window;
                }
                try
                {
                    steps();
                }
                finally
                {
                    CloseDocuments();
                }
            });
        }

        // The example whose window is open.
        private static WalkthroughExample _open;

        /// <summary>Closes the window and the example, if a test opened them, on the UI thread.</summary>
        internal void Close()
        {
            if (_open == this)
                _open = null;
            if (Window == null) return;
            void close()
            {
                Application.Current!.MainWindow = null;
                BestFitShell.Close(Window);
                // The shell's project, a singleton, keeps the example until another opens: let it go.
                BestFitProject.GetInstance().Close();
                Messenger.GetInstance().Clear();
                UserSettings.DefaultValueDigits = 2;
                (Window, Node, Project) = (null, null, null);
                ViewTesting.Collect();
            }
            if (Dispatcher.UIThread.CheckAccess())
                close();
            else
                HeadlessSession.Run(close);
            GC.Collect();
        }

        /// <summary>The project's element of type T named <paramref name="name"/>.</summary>
        internal T Element<T>(string name) where T : class, IElement
        {
            var found = Project.ElementCollections.SelectMany(collection => collection.OfType<T>()).Where(e => e.Name == name).ToList();
            Assert.AreEqual(1, found.Count, $"{_example} has one {typeof(T).Name} named {name}");
            return found[0];
        }

        /// <summary>The project's elements of type T, in the explorer's order.</summary>
        internal List<T> Elements<T>() where T : class, IElement =>
            Project.ElementCollections.SelectMany(collection => collection.OfType<T>()).ToList();

        /// <summary>
        /// Double-clicks the element's node in the project explorer, which opens its document with its properties
        /// beside it.
        /// </summary>
        internal (TView View, TProperties Properties) Open<TView, TProperties>(IElement element) where TView : Control where TProperties : Control
        {
            ElementNode node = ElementNode.FindElementNode(element, Node);
            Assert.IsNotNull(node, element.Name + " is in the project explorer");
            Window.DoubleClick(node.NodeHeader);
            BestFitShell.Frame();
            LayoutDocument document = Documents().SingleOrDefault(d => d.Content is Control c && ReferenceEquals(Node.GetControlElement(c), element));
            Assert.IsNotNull(document, element.Name + ": its document opened");
            Assert.IsInstanceOfType(document.Content, typeof(TView), element.Name);
            Control properties = PropertiesPane()?.Content as Control;
            Assert.IsInstanceOfType(properties, typeof(TProperties), element.Name + ": its properties");
            Assert.AreSame(element, Node.GetControlElement(properties), element.Name + ": the properties are its own");
            return ((TView)document.Content, (TProperties)properties);
        }

        /// <summary>The element's open document and properties, as the window shows them now.</summary>
        internal (TView View, TProperties Properties) Shown<TView, TProperties>(IElement element) where TView : Control where TProperties : Control
        {
            BestFitShell.Frame();
            LayoutDocument document = Documents().SingleOrDefault(d => d.Content is Control c && ReferenceEquals(Node.GetControlElement(c), element));
            Assert.IsNotNull(document, element.Name + ": its document is open");
            return ((TView)document.Content, (TProperties)PropertiesPane().Content);
        }

        /// <summary>Opens the document of the project's element of type T named <paramref name="name"/>.</summary>
        internal (TView View, TProperties Properties) Open<T, TView, TProperties>(string name) where T : class, IElement where TView : Control where TProperties : Control =>
            Open<TView, TProperties>(Element<T>(name));

        /// <summary>
        /// Opens a fresh copy of the example in the window, as File &gt; Open does, after a test that changed it (an
        /// Estimate), so that the class's other tests read the saved results again. The changed copy is saved first, as
        /// <see cref="BestFitShell.Close"/> does, so the shell asks nothing.
        /// </summary>
        internal void Reopen()
        {
            CloseDocuments();
            if (Project.IsDirty)
                Project.Save();
            string copy = Path.Combine(BestFitShell.Folder, _example + "-" + (++_copies) + ".bestfit");
            File.Copy(BestFitShell.Example(_example), copy);
            Window.OpenProject(copy);
            BestFitShell.Frame();
            ViewTesting.Collect();
        }

        private int _copies;

        /// <summary>
        /// Presses an analysis's Estimate button, as a click does, and waits for the run to finish (the button shows again);
        /// no dialog may open.
        /// </summary>
        /// <returns>How long the run took.</returns>
        internal static TimeSpan Estimate(Button estimate, TimeSpan timeout)
        {
            var dialogs = new List<string>();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            using (BestFitShell.WhenOpened<Window>(dialog => { dialogs.Add(dialog.Title); dialog.Close(); }))
            {
                estimate.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                BestFitShell.Frame();
                Assert.IsTrue(ShellPublicVariables.SimulationInProgress, "the analysis runs");
                BestFitShell.WaitUntil(() => estimate.Visibility == Visibility.Visible && !ShellPublicVariables.SimulationInProgress, timeout, "the analysis to finish");
            }
            CollectionAssert.AreEqual(Array.Empty<string>(), dialogs, "no message");
            Console.WriteLine("Estimate took " + watch.Elapsed);
            return watch.Elapsed;
        }

        /// <summary>
        /// Presses an analysis's Estimate button (see <see cref="Estimate(Button, TimeSpan)"/>) and waits for its document's
        /// frequency table to show the new run (the view gives it new rows), checking that the document stayed shown.
        /// </summary>
        internal void Estimate(Control view, DataGrid frequencyTable, Button estimate, TimeSpan timeout)
        {
            object before = frequencyTable.ItemsSource;
            Estimate(estimate, timeout);
            Assert.IsNotNull(TopLevel.GetTopLevel(view), "the document is still shown after the run");
            BestFitShell.WaitUntil(() => frequencyTable.ItemsSource != null && !ReferenceEquals(frequencyTable.ItemsSource, before) && UnivariateWalkthroughTesting.Count(frequencyTable) > 0,
                TimeSpan.FromSeconds(30), "the new frequency curve");
        }

        /// <summary>Closes every open document, as their tabs' close buttons do, without saving changed elements.</summary>
        internal void CloseDocuments()
        {
            // A document whose element changed (an Estimate) asks whether to save it: Don't Save. Any other window is a
            // question nobody expects: close it, so it can't wait.
            using IDisposable answer = Avalonia.Controls.Window.WindowOpenedEvent.AddClassHandler<Avalonia.Controls.Window>((opened, _) =>
            {
                if (opened is MainWindow) return;
                Dispatcher.UIThread.Post(() =>
                {
                    if (opened is SaveElementsDialog)
                        BestFitShell.ClickButton(opened, "Don't Save");
                    else
                        opened.Close();
                }, DispatcherPriority.Background);
            });
            foreach (LayoutDocument document in Documents())
                document.Close();
            BestFitShell.Frame();
            Assert.AreEqual(0, Documents().Count, "the documents closed");
        }

        internal List<LayoutDocument> Documents() =>
            Window.FindControl<DockingManager>("MainDock").Layout.Descendents().OfType<LayoutDocument>().ToList();

        private LayoutAnchorable PropertiesPane() =>
            Window.FindControl<DockingManager>("MainDock").Layout.Descendents().OfType<LayoutAnchorable>()
                .SingleOrDefault(pane => pane.ContentId == ShellPublicVariables.PropertiesWindowContentID);
    }

    /// <summary>The records an Input Data document shows (<see cref="UnivariateWalkthroughTesting.Records"/>).</summary>
    internal sealed record InputRecords(int Exact, int First, int Last, int LowFlags, int Uncertain, int Intervals, int Windows);

    /// <summary>The parts of a Bayesian analysis's document (Univariate, Point Process, Mixture) the walkthroughs read.</summary>
    internal sealed record BayesianView(Control View, DataGrid FrequencyTable, DataGrid Statistics, Control Report,
        KernelDensityControl Density, MarkovChainTraceControl Trace, AutocorrelationControl Autocorrelation);

    /// <summary>The parts of a Bayesian analysis's properties the walkthroughs read.</summary>
    internal sealed record BayesianPanel(Control Panel, BayesianOptionsControl Options, BayesianOutputControl Output, Control Jeffreys, QuantilePriorsControl QuantilePriors);

    /// <summary>
    /// A row of a walkthrough's table of saved Bayesian results, as it writes them: the 1% AEP's point estimate and
    /// interval limits, the interval's width ("90%"), the point estimator as the app names it ("Posterior Mean"), the
    /// largest R-hat and smallest effective sample size, and where the table has them the DIC, chains, thinning interval
    /// and output length.
    /// </summary>
    internal sealed record BayesianResult(string Name, string Estimator, string Point, string Lower, string Upper, string Width, string MaxRhat, string MinEss,
        string Dic = null, string Chains = null, string Thinning = null, string Draws = null);

    /// <summary>
    /// A row of a walkthrough's table of saved Bulletin 17C (GMM) results, as it writes them: the uncertainty method's
    /// saved name, the draws, the saved optimizer status, and the 1% AEP's computed value and confidence limits.
    /// </summary>
    internal sealed record GmmResult(string Name, string Method, string Draws, string Status, string Point, string Lower, string Upper);

    /// <summary>
    /// XGoodFit addition: reads what the univariate views and properties panels show, for the walkthrough tests
    /// (<see cref="WalkthroughExample"/>), and compares it with the walkthroughs' values in their own rounding.
    /// </summary>
    internal static class UnivariateWalkthroughTesting
    {
        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        /// <summary>Selects the tab of <paramref name="root"/> whose header is <paramref name="header"/>, as a click on it does.</summary>
        internal static void SelectTab(Control root, string header)
        {
            TabItem tab = root.GetVisualDescendants().OfType<TabItem>().FirstOrDefault(t => Equals(t.Header, header) && t.IsEffectivelyVisible);
            Assert.IsNotNull(tab, "a tab " + header);
            UnivariateAnalysisTesting.Select(tab);
        }

        /// <summary>The text a grid's cell shows: its text blocks', or a check box's state ("True" or "False").</summary>
        internal static string Text(DataGrid grid, int row, int column)
        {
            DataGridCell cell = ((CopyPasteDataGrid)grid).GetCell(row, column);
            if (cell == null)
            {
                // A grid whose rows were just replaced (a new result) lays its rows out on the next layout pass.
                BestFitShell.Settle();
                cell = ((CopyPasteDataGrid)grid).GetCell(row, column);
            }
            if (cell == null)
                Assert.Fail($"{grid.Name}: the cell at row {row}, column {column} ({Count(grid)} rows, shown {grid.IsEffectivelyVisible}, attached {TopLevel.GetTopLevel(grid) != null})");
            if (cell.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault() is CheckBox box)
                return (box.IsChecked == true).ToString();
            return string.Concat(cell.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text));
        }

        /// <summary>The number of rows a grid shows.</summary>
        internal static int Count(DataGrid grid) => grid.ItemsSource?.Cast<object>().Count() ?? 0;

        /// <summary>A grid's row as its cells show it.</summary>
        internal static string[] Row(DataGrid grid, int row) =>
            Enumerable.Range(0, grid.Columns.Count).Select(column => Text(grid, row, column)).ToArray();

        /// <summary>A grid's rows as their cells show them.</summary>
        internal static List<string[]> Rows(DataGrid grid) => Enumerable.Range(0, Count(grid)).Select(row => Row(grid, row)).ToList();

        /// <summary>The grid's row whose cell in <paramref name="column"/> shows <paramref name="key"/>.</summary>
        internal static string[] Row(DataGrid grid, string key, int column = 0)
        {
            for (int row = 0; row < Count(grid); row++)
                if (Text(grid, row, column) == key)
                    return Row(grid, row);
            throw new AssertFailedException($"{grid.Name} has no row {key}");
        }

        /// <summary>The text a property control shows: its text box's, else its combo box's selection, else its text blocks'.</summary>
        internal static string Shown(Control control)
        {
            if (control.GetVisualDescendants().OfType<TextBox>().FirstOrDefault() is TextBox box)
                return box.Text;
            if (control.GetVisualDescendants().OfType<ComboBox>().FirstOrDefault() is ComboBox combo)
                return string.Concat(combo.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible).Select(t => t.Text));
            return string.Concat(control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text));
        }

        /// <summary>
        /// Whether a property control's check box is ticked; for one in a section the panel hasn't expanded (whose check box
        /// isn't made yet), the state the control holds.
        /// </summary>
        internal static bool Ticked(Control control)
        {
            CheckBox box = control as CheckBox ?? control.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault();
            if (box == null && control is BooleanPropertyControl property)
                return property.IsSelected;
            Assert.IsNotNull(box, control.Name + " has a check box");
            return box.IsChecked == true;
        }

        /// <summary>The text of a report control's page (the MCMC and GMM reports).</summary>
        internal static string ReportText(Control report)
        {
            var box = report.GetVisualDescendants().OfType<WpfCompat.Controls.RichTextBox>().Single();
            return string.Join("\n", box.Document.Blocks.OfType<WpfCompat.Documents.Paragraph>()
                .Select(p => string.Concat(p.Inlines.OfType<WpfCompat.Documents.Run>().Select(r => r.Text)))).Replace("\r\n", "\n");
        }

        /// <summary>What a report's line labelled <paramref name="label"/> says after the label (the first such line).</summary>
        internal static string ReportValue(string report, string label)
        {
            string line = report.Split('\n').FirstOrDefault(l => l.TrimStart().StartsWith(label, StringComparison.Ordinal));
            Assert.IsNotNull(line, "the report has a line " + label);
            return line.Substring(line.IndexOf(label, StringComparison.Ordinal) + label.Length).Trim();
        }

        /// <summary>The first word of what a report's line labelled <paramref name="label"/> says.</summary>
        internal static string ReportWord(string report, string label) => ReportValue(report, label).Split(' ')[0];

        /// <summary>
        /// The numbers of a report table's row for <paramref name="name"/>, in the section headed <paramref name="section"/>:
        /// the words after the name.
        /// </summary>
        internal static string[] ReportRow(string report, string section, string name)
        {
            string[] lines = report.Split('\n');
            int start = Array.FindIndex(lines, l => l.Trim() == section);
            Assert.IsTrue(start >= 0, "the report has a section " + section);
            for (int i = start + 1; i < lines.Length && !lines[i].StartsWith("=", StringComparison.Ordinal); i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith(name + "  ", StringComparison.Ordinal))
                    return line.Substring(name.Length).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (i > start + 1 && lines[i].Length > 0 && char.IsUpper(lines[i][0]))
                    break;
            }
            throw new AssertFailedException($"the report's {section} has no row {name}");
        }

        /// <summary>
        /// Checks that a number a view shows agrees with a walkthrough's, each rounded to its own decimals: they differ
        /// by no more than half a unit in the last place of each. Where the view shows the walkthrough's decimals, they
        /// are equal.
        /// </summary>
        internal static void AssertAgrees(string walkthrough, string shown, string label)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(shown), $"{label}: the view shows nothing, the walkthrough {walkthrough}");
            double expected = Parse(walkthrough), actual = Parse(shown);
            double tolerance = 0.5 * Math.Pow(10, -Decimals(walkthrough)) + 0.5 * Math.Pow(10, -Decimals(shown));
            Assert.IsTrue(Math.Abs(expected - actual) <= tolerance * (1 + 1E-9), $"{label}: the walkthrough says {walkthrough}, the view shows {shown}");
        }

        /// <summary>A number as the walkthroughs and views write it (en-US, with group separators and a Unicode minus).</summary>
        internal static double Parse(string number) =>
            double.Parse(number.Replace('−', '-').Trim(), NumberStyles.Float | NumberStyles.AllowThousands, Us);

        private static int Decimals(string number)
        {
            number = number.Trim();
            int dot = number.IndexOf('.');
            return dot < 0 ? 0 : number.Length - dot - 1;
        }

        /// <summary>
        /// Checks a frequency table's row for an annual exceedance probability (the probability column shows it as
        /// <paramref name="probability"/>): the point estimate's curve, and the interval's lower and upper limits, as the
        /// walkthrough writes them (the table shows three decimals, as <see cref="WalkthroughExample"/> sets).
        /// </summary>
        internal static void AssertQuantile(DataGrid table, string probability, string point, string lower, string upper, string label)
        {
            string[] row = Row(table, probability);
            string shown = " (the row shows " + string.Join(" | ", row) + ")";
            Assert.AreEqual(point, row[4], $"{label}: the {table.Columns[4].Header} at {probability}{shown}");
            Assert.AreEqual(lower, row[2], $"{label}: the lower limit ({table.Columns[2].Header}) at {probability}{shown}");
            Assert.AreEqual(upper, row[1], $"{label}: the upper limit ({table.Columns[1].Header}) at {probability}{shown}");
        }

        /// <summary>Checks the interval headers of a frequency table for a central interval of <paramref name="width"/> (e.g. "90%").</summary>
        internal static void AssertIntervalWidth(DataGrid table, string width, string label)
        {
            double alpha = (100 - Parse(width.TrimEnd('%'))) / 2;
            Assert.AreEqual((100 - alpha).ToString("F1", Us) + "% CI", table.Columns[1].Header, label + ": the upper limit's header");
            Assert.AreEqual(alpha.ToString("F1", Us) + "% CI", table.Columns[2].Header, label + ": the lower limit's header");
        }

        /// <summary>A summary statistics table's value for a measure (the tables show F4).</summary>
        internal static string Statistic(DataGrid table, string measure) => Row(table, measure)[1];

        /// <summary>
        /// Checks an MCMC report's largest R-hat (which it shows to four decimals) and smallest effective sample size (in
        /// whole draws, as the walkthroughs write it).
        /// </summary>
        internal static void AssertDiagnostics(string report, string maxRhat, string minEss, string label)
        {
            AssertAgrees(maxRhat, ReportWord(report, "Max R-hat:"), label + ": the largest R-hat");
            Assert.AreEqual(minEss, ReportWord(report, "Min ESS:"), label + ": the smallest effective sample size");
        }

        /// <summary>
        /// Checks an MCMC report's parameter summary: the posterior mean, median and the interval's limits (the report shows
        /// six significant digits).
        /// </summary>
        internal static void AssertParameterSummary(string report, string parameter, string mean, string median, string lower, string upper, string label)
        {
            // Mean, Std Dev, lower limit, Median, upper limit, R-hat, ESS.
            string[] row = ReportRow(report, "PARAMETER SUMMARY STATISTICS", parameter);
            AssertAgrees(mean, row[0], $"{label}: {parameter}'s posterior mean");
            AssertAgrees(median, row[3], $"{label}: {parameter}'s posterior median");
            AssertAgrees(lower, row[2], $"{label}: {parameter}'s lower limit");
            AssertAgrees(upper, row[4], $"{label}: {parameter}'s upper limit");
        }

        /// <summary>
        /// The records an Input Data document's data frame shows, as the walkthroughs' input tables count them: the exact
        /// data's rows with their first and last index (a date's year) and the rows whose Low Outlier box is ticked, and the
        /// rows of the uncertain, interval and threshold data.
        /// </summary>
        internal static InputRecords Records(InputDataControl view)
        {
            SelectTab(view, "Data Frame");
            SelectTab(view, "Exact Data");
            DataGrid exact = view.ExactDataGrid;
            int rows = Count(exact);
            int lowOutlier = exact.Columns.IndexOf(exact.Columns.Single(c => Equals(c.Header, "Low Outlier")));
            int flags = Enumerable.Range(0, rows).Count(row => Text(exact, row, lowOutlier) == "True");
            // Each tab is shown before its rows are counted: until a grid generates its columns it holds the dummy row
            // upstream adds for that (InputDataControl's Bind...DataGrid), as WPF's grid on a tab never shown does.
            SelectTab(view, "Uncertain Data");
            int uncertain = Count(view.UncertainDataGrid);
            SelectTab(view, "Interval Data");
            int intervals = Count(view.IntervalDataGrid);
            SelectTab(view, "Threshold Data");
            int windows = Count(view.ThresholdDataGrid);
            SelectTab(view, "Exact Data");
            return new InputRecords(rows, rows == 0 ? 0 : Year(Text(exact, 0, 0)), rows == 0 ? 0 : Year(Text(exact, rows - 1, 0)), flags,
                uncertain, intervals, windows);
        }

        /// <summary>
        /// An index as the data grids show it: a year, or a date and time whose year is taken (ICU's en-US, on Linux and
        /// macOS, puts a narrow no-break space before AM and PM).
        /// </summary>
        internal static int Year(string index) =>
            int.TryParse(index, NumberStyles.Integer, Us, out int year) ? year : DateTime.Parse(index.Replace((char)0x202F, ' '), Us).Year;

        /// <summary>Checks an input's records against a row of a walkthrough's input table.</summary>
        internal static void AssertRecords(InputRecords shown, int exact, int first, int last, int uncertain, int intervals, int windows, int lowFlags, string label)
        {
            Assert.AreEqual(exact, shown.Exact, label + ": the exact rows");
            Assert.AreEqual(first, shown.First, label + ": the first index");
            Assert.AreEqual(last, shown.Last, label + ": the last index");
            Assert.AreEqual(uncertain, shown.Uncertain, label + ": the uncertain rows");
            Assert.AreEqual(intervals, shown.Intervals, label + ": the interval rows");
            Assert.AreEqual(windows, shown.Windows, label + ": the threshold windows");
            Assert.AreEqual(lowFlags, shown.LowFlags, label + ": the low outlier flags");
        }

        /// <summary>The parts of a Univariate analysis's document the walkthroughs read.</summary>
        internal static BayesianView Parts(UnivariateAnalysisControl view) => new BayesianView(view, view.FrequencyCurveTable, view.SummaryStatisticsTable,
            view.MCMCReportControl, view.KernelDensityControl, view.MarkovChainTraceControl, view.AutocorrelationControl);

        /// <summary>The parts of a Point Process analysis's document the walkthroughs read.</summary>
        internal static BayesianView Parts(PointProcessAnalysisControl view) => new BayesianView(view, view.FrequencyCurveTable, view.SummaryStatisticsTable,
            view.MCMCReportControl, view.KernelDensityControl, view.MarkovChainTraceControl, view.AutocorrelationControl);

        /// <summary>The parts of a Mixture analysis's document the walkthroughs read.</summary>
        internal static BayesianView Parts(MixtureAnalysisControl view) => new BayesianView(view, view.FrequencyCurveTable, view.SummaryStatisticsTable,
            view.MCMCReportControl, view.KernelDensityControl, view.MarkovChainTraceControl, view.AutocorrelationControl);

        /// <summary>The parts of a Univariate analysis's properties the walkthroughs read.</summary>
        internal static BayesianPanel Parts(UnivariateAnalysisPropertiesControl panel) =>
            new BayesianPanel(panel, panel.BayesianOptionsControl, panel.BayesianOutputControl, panel.UseJeffreysRule, panel.QuantilePriorsControl);

        /// <summary>The parts of a Point Process analysis's properties the walkthroughs read.</summary>
        internal static BayesianPanel Parts(PointProcessAnalysisPropertiesControl panel) =>
            new BayesianPanel(panel, panel.BayesianOptionsControl, panel.BayesianOutputControl, panel.UseJeffreysRule, panel.QuantilePriorsControl);

        /// <summary>The parts of a Mixture analysis's properties the walkthroughs read.</summary>
        internal static BayesianPanel Parts(MixtureAnalysisPropertiesControl panel) =>
            new BayesianPanel(panel, panel.BayesianOptionsControl, panel.BayesianOutputControl, panel.UseJeffreysRule, panel.QuantilePriorsControl);

        /// <summary>
        /// Checks a row of a walkthrough's table of saved Bayesian results where the walkthrough tells the user to read it:
        /// the 1% AEP's point estimate and limits, and the interval and point estimator, in Tabular Results; the DIC in its
        /// summary statistics; R-hat and the effective sample size in the MCMC Report; and the output options and chains in
        /// the properties.
        /// </summary>
        internal static void AssertResult(BayesianView view, BayesianPanel panel, BayesianResult expected)
        {
            string label = expected.Name;
            SelectTab(view.View, "Tabular Results");
            AssertIntervalWidth(view.FrequencyTable, expected.Width, label);
            Assert.AreEqual(expected.Estimator, view.FrequencyTable.Columns[4].Header, label + ": the point estimate's column");
            AssertQuantile(view.FrequencyTable, "0.01", expected.Point, expected.Lower, expected.Upper, label);
            if (expected.Dic != null)
                AssertAgrees(expected.Dic, Statistic(view.Statistics, "DIC"), label + ": the DIC");
            SelectTab(view.View, "MCMC Report");
            AssertDiagnostics(ReportText(view.Report), expected.MaxRhat, expected.MinEss, label);
            SelectTab(view.View, "Distribution Results");

            SelectTab(panel.Panel, "Output");
            Assert.AreEqual(expected.Width, Shown(panel.Output.CredibleInterval), label + ": the credible interval");
            Assert.AreEqual(expected.Estimator, Shown(panel.Output.PointEstimator), label + ": the point estimator");
            if (expected.Draws != null)
                Assert.AreEqual(expected.Draws, Shown(panel.Output.OutputLength), label + ": the output length");
            if (expected.Chains != null)
            {
                SelectTab(panel.Panel, "Options");
                Assert.AreEqual(expected.Chains, Shown(panel.Options.NumberOfChains), label + ": the chains");
                Assert.AreEqual(expected.Thinning, Shown(panel.Options.ThinningInterval), label + ": the thinning interval");
            }
            SelectTab(panel.Panel, "General");
        }

        /// <summary>
        /// Checks the sampler settings the walkthroughs say every Bayesian alternative retains: the MCMC Report's sampler
        /// (DE-MCzs, the walkthroughs' "DEMCzs") and seed, and the properties' simulation options, output length and
        /// Jeffreys rule. Null skips a setting.
        /// </summary>
        internal static void AssertSampler(BayesianView view, BayesianPanel panel, string label, string chains = null, string thinning = null,
            string warmup = null, string iterations = null, string draws = null, string seed = "12345", bool? jeffreys = true)
        {
            SelectTab(view.View, "MCMC Report");
            string report = ReportText(view.Report);
            Assert.AreEqual("DE-MCzs", ReportValue(report, "Sampler:"), label + ": the sampler");
            Assert.AreEqual(seed, ReportValue(report, "PRNG Seed:"), label + ": the report's seed");
            SelectTab(view.View, "Distribution Results");
            if (jeffreys != null)
                Assert.AreEqual(jeffreys.Value, Ticked(panel.Jeffreys), label + ": Jeffreys' rule for scale");
            SelectTab(panel.Panel, "Options");
            Assert.AreEqual(seed, Shown(panel.Options.PRNGSeed), label + ": the seed");
            foreach (var (control, value, what) in new[]
            {
                ((Control)panel.Options.NumberOfChains, chains, "chains"), (panel.Options.ThinningInterval, thinning, "thinning interval"),
                (panel.Options.WarmupIterations, warmup, "warmup iterations"), (panel.Options.Iterations, iterations, "iterations"),
            })
            {
                if (value != null)
                    Assert.AreEqual(value, Shown(control), label + ": the " + what);
            }
            if (draws != null)
            {
                SelectTab(panel.Panel, "Output");
                Assert.AreEqual(draws, Shown(panel.Output.OutputLength), label + ": the output length");
            }
            SelectTab(panel.Panel, "General");
        }

        /// <summary>
        /// "Use Select Parameter to choose <paramref name="parameter"/>" in the Kernel Density, Markov Chain Traces and
        /// Autocorrelation views (their plots are compared with RMC-BestFit's exports by the area tests), then checks the
        /// density's summary row against the walkthrough's parameter summary: the posterior mean, median and limits.
        /// </summary>
        internal static void SelectParameter(BayesianView view, string parameter, string mean, string median, string lower, string upper)
        {
            foreach (var (tab, combo) in new (string, ComboBox)[]
            {
                ("Kernel Density", view.Density.ParameterComboBox), ("Markov Chain Traces", view.Trace.ParameterComboBox),
                ("Autocorrelation", view.Autocorrelation.ParameterComboBox),
            })
            {
                SelectTab(view.View, tab);
                object item = combo.Items.Cast<object>().SingleOrDefault(i => i.ToString() == parameter);
                Assert.IsNotNull(item, $"{tab}'s Select Parameter offers {parameter}: " + string.Join(", ", combo.Items.Cast<object>()));
                combo.SelectedItem = item;
                SharedControlsTesting.Settle();
                Assert.AreEqual(parameter, combo.SelectedItem?.ToString(), tab + ": the parameter");
            }
            SelectTab(view.View, "Kernel Density");
            // Rhat, ESS, Mean, Std Dev, lower limit, median, upper limit.
            string[] density = Row(view.Density.KDEDataGrid, 0);
            AssertAgrees(mean, density[2], parameter + ": the density's mean");
            AssertAgrees(median, density[5], parameter + ": the density's median");
            AssertAgrees(lower, density[4], parameter + ": the density's lower limit");
            AssertAgrees(upper, density[6], parameter + ": the density's upper limit");
            SelectTab(view.View, "Distribution Results");
        }

        /// <summary>"Use Select Parameter to choose <paramref name="parameter"/>", for walkthroughs without a parameter summary.</summary>
        internal static void SelectParameter(BayesianView view, string parameter)
        {
            foreach (var (tab, combo) in new (string, ComboBox)[]
            {
                ("Kernel Density", view.Density.ParameterComboBox), ("Markov Chain Traces", view.Trace.ParameterComboBox),
                ("Autocorrelation", view.Autocorrelation.ParameterComboBox),
            })
            {
                SelectTab(view.View, tab);
                object item = combo.Items.Cast<object>().SingleOrDefault(i => i.ToString() == parameter);
                Assert.IsNotNull(item, $"{tab}'s Select Parameter offers {parameter}: " + string.Join(", ", combo.Items.Cast<object>()));
                combo.SelectedItem = item;
                SharedControlsTesting.Settle();
                Assert.AreEqual(parameter, combo.SelectedItem?.ToString(), tab + ": the parameter");
            }
            SelectTab(view.View, "Distribution Results");
        }

        /// <summary>
        /// Checks a Univariate analysis's model options (Properties &gt; Options), as the nonstationary walkthroughs' trend
        /// tables list them: whether the model is nonstationary, each parameter's trend model in the model's order, the
        /// time index the frequency curve is evaluated at, and alpha (the chronology's return level). The trend models'
        /// start index, which the walkthroughs list too, isn't shown (upstream sets it to the data's first index, the Time
        /// Index's minimum): it's read from the element's model.
        /// </summary>
        internal static void AssertTrends(UnivariateAnalysis element, UnivariateAnalysisPropertiesControl panel, bool nonstationary, string[] types,
            int start, string timeIndex, string[] parameters = null, string alpha = "0.5")
        {
            string label = element.Name;
            SelectTab(panel, "Options");
            Assert.AreEqual(nonstationary, Ticked(panel.IsNonstationary), label + ": nonstationary");
            var rows = Rows(panel.TrendModelDataGrid);
            CollectionAssert.AreEqual(types, rows.Select(r => r[1]).ToArray(), label + ": the trend models, " + string.Join(" / ", rows.Select(r => r[1])));
            if (parameters != null)
                CollectionAssert.AreEqual(parameters, rows.Select(r => r[0]).ToArray(), label + ": the trend models' parameters");
            Assert.AreEqual(timeIndex, Shown(panel.ParameterTimeStep), label + ": the time index");
            Assert.AreEqual(alpha, Shown(panel.Alpha), label + ": alpha");
            foreach (var trend in element.UnivariateDistribution.TrendModels)
                Assert.AreEqual(start, trend.StartIndex, label + ": " + trend.OwnerName + "'s trend start index");
            SelectTab(panel, "General");
        }

        /// <summary>The uncertainty methods as the GMM Report names them, by the walkthroughs' (saved) names.</summary>
        internal static readonly Dictionary<string, string> GmmMethods = new Dictionary<string, string>
        {
            ["MultivariateNormal"] = "Multivariate Normal", ["LinkedMultivariateNormal"] = "Linked Multivariate Normal",
            ["BiasCorrectedBootstrap"] = "Bias-Corrected Bootstrap",
        };

        /// <summary>
        /// Checks a row of a walkthrough's table of saved Bulletin 17C (GMM) results where it tells the user to read it: the
        /// 1% AEP's computed curve and confidence limits in Tabular Results; the uncertainty method, draws (output length),
        /// optimizer status, confidence level, seed and GMM strategy in the GMM Report; and the options in the properties.
        /// </summary>
        internal static void AssertGmmResult(B17CAnalysisControl view, B17CAnalysisPropertiesControl panel, GmmResult expected, string width = "90%")
        {
            string label = expected.Name;
            SelectTab(view, "Tabular Results");
            AssertIntervalWidth(view.FrequencyCurveTable, width, label);
            AssertQuantile(view.FrequencyCurveTable, "0.01", expected.Point, expected.Lower, expected.Upper, label);
            SelectTab(view, "GMM Report");
            string report = ReportText(view.GMMReportControl);
            Assert.AreEqual(GmmMethods[expected.Method], ReportValue(report, "Uncertainty Method:"), label + ": the report's uncertainty method");
            Assert.AreEqual(Parse(expected.Draws).ToString("N0", Us), ReportValue(report, "Output Length:"), label + ": the report's draws");
            Assert.AreEqual(expected.Status, ReportValue(report, "Optimizer Status:"), label + ": the optimizer status");
            // Reports saved by earlier versions keep the legacy label "Credible Interval" (the walkthroughs say so).
            string interval = report.Contains("Confidence Interval:", StringComparison.Ordinal) ? "Confidence Interval:" : "Credible Interval:";
            Assert.AreEqual(width, ReportValue(report, interval), label + ": the report's confidence interval");
            Assert.AreEqual("12345", ReportValue(report, "PRNG Seed:"), label + ": the report's seed");
            Assert.AreEqual("Iterative", ReportValue(report, "GMM Strategy:"), label + ": the GMM strategy");
            SelectTab(view, "Distribution Results");
            SelectTab(panel, "Options");
            Assert.AreEqual(GmmMethods[expected.Method], Shown(panel.UncertaintyMethod), label + ": the uncertainty method");
            Assert.AreEqual(expected.Draws, Shown(panel.OutputLength), label + ": the output length");
            Assert.AreEqual("12345", Shown(panel.PRNGSeed), label + ": the seed");
            Assert.AreEqual(width, Shown(panel.ConfidenceInterval), label + ": the confidence interval");
            SelectTab(panel, "General");
        }

        /// <summary>
        /// Checks a Bulletin 17C analysis's GMM settings that the walkthroughs list but the app doesn't show (they're read
        /// from the element's model): the optimizer, the most GMM iterations, the tolerances and convergence within them.
        /// </summary>
        internal static void AssertGmmSettings(B17CAnalysis element, string optimizer = "BFGS")
        {
            var gmm = element.GMM;
            Assert.AreEqual(optimizer, gmm.OptimizerMethod.ToString(), element.Name + ": the optimizer");
            Assert.AreEqual("Iterative", gmm.EstimationStrategy.ToString(), element.Name + ": the GMM strategy");
            Assert.AreEqual(100, gmm.MaxGMMIterations, element.Name + ": the most GMM iterations");
            Assert.AreEqual(1E-8, gmm.AbsoluteTolerance, element.Name + ": the absolute tolerance");
            Assert.AreEqual(1E-8, gmm.RelativeTolerance, element.Name + ": the relative tolerance");
        }

        /// <summary>The rows of a Bulletin 17C analysis's penalty table (General tab) whose Enabled box is ticked.</summary>
        internal static List<string[]> EnabledPenalties(DataGrid table) => Rows(table).Where(r => r[0] == "True").ToList();

        /// <summary>The rows of a priors control's table (Parameter or Probability, and Distribution), as shown.</summary>
        internal static List<string[]> Priors(Control priors) =>
            Rows(priors.GetVisualDescendants().OfType<DataGrid>().Single(g => g.Name == "PriorDistributionsDataGrid"));

        /// <summary>Whether a quantile priors control's "Enable Priors on Quantiles" box is ticked.</summary>
        internal static bool QuantilePriorsEnabled(QuantilePriorsControl priors) => Ticked(priors.EnablePriorQuantileDistributions);

        /// <summary>
        /// Checks one series of a walkthrough figure (by its name) against the plot's series of that title, point for
        /// point, for figures whose other series the plot doesn't show.
        /// </summary>
        internal static void AssertSeries(OxyPlot.Wpf.Plot plot, JsonElement figure, string title, string label)
        {
            ((OxyPlot.IPlotModel)plot.ActualModel).Update(true);
            JsonElement expected = figure.GetProperty("series").EnumerateArray().Single(s => s.GetProperty("name").GetString() == title);
            var series = plot.Series.Where(s => s.Title == title).ToList();
            Assert.AreEqual(1, series.Count, $"{label}: one series titled {title}, among " + string.Join(", ", plot.Series.Select(s => s.Title)));
            var points = UnivariateAnalysisTesting.Points(series[0].InternalSeries, "ActualPoints");
            JsonElement x = expected.GetProperty("x"), y = expected.GetProperty("y");
            Assert.AreEqual(x.GetArrayLength(), points.Count, $"{label}: {title}'s points");
            for (int i = 0; i < points.Count; i++)
            {
                UnivariateAnalysisTesting.AssertClose(x[i].GetDouble(), points[i].X, $"{label}: {title} {i}.X");
                UnivariateAnalysisTesting.AssertClose(y[i].GetDouble(), points[i].Y, $"{label}: {title} {i}.Y");
            }
        }

        /// <summary>
        /// Checks a walkthrough figure of bars (a histogram's, a seasonality plot's) against the plot's histogram series of
        /// the same title: each bar's centre (the figure's x), width and height.
        /// </summary>
        /// <param name="plot">The plot, shown.</param>
        /// <param name="figure">The figure's data.</param>
        /// <param name="label">What the assertions say.</param>
        /// <param name="title">The plot's title for the figure's series, where the figure relabels it.</param>
        internal static void AssertBars(OxyPlot.Wpf.Plot plot, JsonElement figure, string label, string title = null)
        {
            ((OxyPlot.IPlotModel)plot.ActualModel).Update(true);
            foreach (JsonElement expected in figure.GetProperty("series").EnumerateArray())
            {
                title ??= expected.GetProperty("name").GetString();
                var series = plot.Series.OfType<OxyPlot.Wpf.HistogramSeries>().Where(s => s.Title == title).ToList();
                Assert.AreEqual(1, series.Count, $"{label}: one histogram titled {title}, among " + string.Join(", ", plot.Series.Select(s => s.Title)));
                var bars = ((System.Collections.IEnumerable)series[0].ItemsSource).Cast<OxyPlot.Series.HistogramItem>().ToList();
                JsonElement x = expected.GetProperty("x"), y = expected.GetProperty("y"), width = expected.GetProperty("width");
                Assert.AreEqual(x.GetArrayLength(), bars.Count, $"{label}: {title}'s bars");
                for (int i = 0; i < bars.Count; i++)
                {
                    UnivariateAnalysisTesting.AssertClose(x[i].GetDouble(), (bars[i].RangeStart + bars[i].RangeEnd) / 2, $"{label}: {title} bar {i}'s centre");
                    UnivariateAnalysisTesting.AssertClose(width[i].GetDouble(), bars[i].RangeEnd - bars[i].RangeStart, $"{label}: {title} bar {i}'s width");
                    UnivariateAnalysisTesting.AssertClose(y[i].GetDouble(), bars[i].Value, $"{label}: {title} bar {i}");
                }
            }
        }

        /// <summary>The number of points a plot's series titled <paramref name="title"/> draws.</summary>
        internal static int PointCount(OxyPlot.Wpf.Plot plot, string title)
        {
            ((OxyPlot.IPlotModel)plot.ActualModel).Update(true);
            return plot.Series.Where(s => s.Title == title).Sum(s => UnivariateAnalysisTesting.Points(s.InternalSeries, "ActualPoints").Count);
        }

        /// <summary>The walkthrough figure's data (an images/*.plotspec.json.gz or screenshots/*.json.gz under the examples folder).</summary>
        internal static JsonElement Figure(string relative)
        {
            string path = SharedControlsTesting.Example(relative);
            using var stream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
            using var document = JsonDocument.Parse(stream);
            return document.RootElement.Clone();
        }
    }
}
