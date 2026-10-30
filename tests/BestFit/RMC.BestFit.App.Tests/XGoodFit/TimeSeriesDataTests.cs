using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameworkUI;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Numerics.Data;
using NumericControls;
using OxyPlot;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.TimeSeriesDataTesting;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Time Series Data views (GUI/TimeSeriesData/TimeSeries) on the chapter's examples,
    /// checked against what RMC-BestFit showed (the native screenshots' capture data beside each example) and
    /// against upstream's logic: the plots, table and statistics, the properties of each data source, row edits
    /// with upstream's warning, manual entry, the DSS pathname selector and import, the alternatives, and that
    /// showing the views changes nothing in the element.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class TimeSeriesDataTests
    {
        private readonly List<BestFitProject> _projects = new List<BestFitProject>();
        private readonly List<Window> _windows = new List<Window>();
        private readonly List<string> _folders = new List<string>();
        private CultureScope _culture;

        // The examples were saved in en-US (see CultureScope).
        [TestInitialize]
        public void Initialize() => _culture = new CultureScope("en-US");

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var window in _windows) window.Close();
            _windows.Clear();
            foreach (var project in _projects) TimeSeriesDataTesting.CloseProject(project);
            _projects.Clear();
            foreach (string folder in _folders) DeleteFolder(folder);
            _folders.Clear();
            ReleaseAppResources();
            _culture?.Dispose();
            _culture = null;
        }

        /// <summary>
        /// Views of the largest records (over 200,000 observations) are left to <see cref="LargestRecords_DrawAsRmcBestFitDid_AndShowingThemChangesNothing"/>:
        /// NumericControls' table makes a validating row for each observation, as upstream's does, so a view of a
        /// million holds over a gigabyte, too much for the default run on a small CI machine.
        /// </summary>
        private const int LargeRecord = 200000;

        [STATestMethod]
        public void Plots_DrawTheStoredSeries_AsRmcBestFitDid()
        {
            UseAppResources(ThemeColor.Light);
            ForEachNativePlot(large: false);
        }

        [STATestMethod]
        [TestCategory("Large")]
        public void LargestRecords_DrawAsRmcBestFitDid_AndShowingThemChangesNothing()
        {
            if (Environment.GetEnvironmentVariable("XGOODFIT_LARGE_TESTS") != "1")
                Assert.Inconclusive("Views of the examples' largest records take several gigabytes: set XGOODFIT_LARGE_TESTS=1 to run this test.");
            UseAppResources(ThemeColor.Light);
            ForEachNativePlot(large: true);
            foreach (string example in Examples)
            {
                var project = OpenProject(example);
                foreach (var element in Elements(project).OfType<TimeSeriesElement>().Where(e => e.TimeSeries.Count > LargeRecord).ToList())
                    AssertShowingChangesNothing(example, element);
                CloseProject(project);
            }
        }

        // Shows the view of each native plot screenshot's element (the largest records, or the others), and checks
        // its plot against RMC-BestFit's capture data.
        private void ForEachNativePlot(bool large)
        {
            BestFitProject project = null;
            foreach (var shot in NativePlots)
            {
                // One example at a time: they hold millions of observations.
                if (project == null || Path.GetFileName(project.FullFileName) != Path.GetFileName(shot.Example))
                {
                    if (project != null) CloseProject(project);
                    project = OpenProject(shot.Example);
                }
                var element = Element<TimeSeriesElement>(project, shot.Element);
                if (element.TimeSeries.Count > LargeRecord != large) continue;
                var savedTitles = PlotOf(element, shot.Tab).Axes.ToDictionary(a => a.Key, a => a.Title);
                var (view, window) = Host(element, shot.Tab);
                try
                {
                    AssertNativePlot(shot, element, view, savedTitles);
                }
                finally
                {
                    window.Close();
                    _windows.Remove(window);
                    view.Element = null;
                    Release();
                }
            }
        }

        // The plot a view shows, against the capture data of RMC-BestFit's export of it.
        private static void AssertNativePlot((string Example, string Name, string Element, string Tab, string Alternative) shot, TimeSeriesElement element,
            TimeSeriesControl view, Dictionary<string, string> savedTitles)
        {
            if (shot.Alternative != null)
            {
                view.ShowAlternativesToggleButton.IsChecked = true;
                view.AlternativeTimeSeriesSelector.TimeSeriesList.Single(item => item.Alternative.Name == shot.Alternative).IsChecked = true;
                Settle();
            }
            using JsonDocument capture = NativeCapture(NativeScreenshot(shot.Example, shot.Name + "-native.json.gz"));
            JsonElement native = capture.RootElement;
            Wpf.Plot plot = view.GetCurrentPlot();
            string what = shot.Name;
            Assert.AreEqual(native.GetProperty("title").GetString(), plot.Title, what);

            // The series RMC-BestFit drew, in order, with the points it plotted (its export recorded the model's).
            var nativeSeries = native.GetProperty("series").EnumerateArray().ToList();
            Assert.AreEqual(nativeSeries.Count, plot.Series.Count, $"{what}: series");
            for (int i = 0; i < nativeSeries.Count; i++)
            {
                JsonElement expected = nativeSeries[i];
                Wpf.Series series = plot.Series[i];
                Assert.AreEqual(expected.GetProperty("name").GetString(), series.Name, $"{what}: series {i}");
                Assert.AreEqual(expected.GetProperty("IsVisible").GetBoolean(), series.IsVisible, $"{what}: {series.Name} visible");
                if (series.InternalSeries is OxyPlot.Series.DataPointSeries)
                    AssertPoints(expected.GetProperty("points"), Member<IEnumerable<DataPoint>>(series.InternalSeries, "ActualPoints"), $"{what}: {series.Name}");
                if (series is Wpf.AreaSeries)
                    AssertPoints(expected.GetProperty("points2"), Member<IEnumerable<DataPoint>>(series.InternalSeries, "ActualPoints2"), $"{what}: {series.Name} (second line)");
                if (series is Wpf.HistogramSeries)
                {
                    var items = Member<IEnumerable<OxyPlot.Series.HistogramItem>>(series.InternalSeries, "ActualItems").ToList();
                    var expectedItems = expected.GetProperty("actualItems").EnumerateArray().ToList();
                    Assert.AreEqual(expectedItems.Count, items.Count, $"{what}: {series.Name} items");
                    for (int j = 0; j < items.Count; j++)
                    {
                        AssertClose(expectedItems[j].GetProperty("Value").GetDouble(), items[j].Value, $"{what}: item {j}", 1e-12);
                        Assert.AreEqual(expectedItems[j].GetProperty("RangeStart").GetDouble(), items[j].RangeStart, what);
                        Assert.AreEqual(expectedItems[j].GetProperty("RangeEnd").GetDouble(), items[j].RangeEnd, what);
                    }
                }
            }
            var annotations = native.GetProperty("annotations").EnumerateArray().ToList();
            Assert.AreEqual(annotations.Count, plot.Annotations.Count, $"{what}: annotations");
            for (int i = 0; i < annotations.Count; i++)
            {
                var annotation = (Wpf.LineAnnotation)plot.Annotations[i];
                Assert.AreEqual(annotations[i].GetProperty("text").GetString(), annotation.Text, what);
                AssertClose(annotations[i].GetProperty("geometry").GetProperty("Y").GetDouble(), annotation.Y, $"{what}: {annotation.Text}", 1e-12);
            }

            // The axes' titles, and the ranges the data gave them. RMC-BestFit's exports reset the plot first: a
            // title the project saved, edited away from the data's label, stays (as the application keeps it).
            foreach (JsonElement axis in native.GetProperty("axes").EnumerateArray())
            {
                var actual = plot.Axes.Single(a => a.Key == axis.GetProperty("Key").GetString());
                string title = axis.GetProperty("Title").GetString();
                string saved = savedTitles[actual.Key];
                if (!string.IsNullOrEmpty(saved) && saved != element.UnitLabel) title = saved;
                Assert.AreEqual(title, actual.Title, $"{what}: {actual.Key} title");
                AssertClose(axis.GetProperty("ActualMinimum").GetDouble(), actual.InternalAxis.ActualMinimum, $"{what}: {actual.Key} minimum", 1e-6);
                AssertClose(axis.GetProperty("ActualMaximum").GetDouble(), actual.InternalAxis.ActualMaximum, $"{what}: {actual.Key} maximum", 1e-6);
            }

            // The view picks the date format for the span shown (TimeSeriesPlotModelUpdated), as upstream's does.
            if (shot.Tab == "Time Series")
            {
                var dates = (OxyPlot.Axes.DateTimeAxis)plot.Axes.Single(a => a.Key == "Xaxis").InternalAxis;
                double days = dates.ActualMaximum - dates.ActualMinimum;
                string format = days > 365 * 4 ? "yyyy" : days > 90 ? "MMM-yyyy" : days > 2 ? "dd-MMM" : "HH:mm";
                Assert.AreEqual(format, dates.StringFormat, $"{what}: date format");
            }

        }

        [STATestMethod]
        public void TimeSeriesTab_ShowsTheTableAndTheSummaryStatistics()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<TimeSeriesElement>(OpenProject(Manual), "Airline Passengers");
            var (view, _) = Host(element);

            var grid = view.TimeSeriesTable.GetVisualDescendants().OfType<ValidationDataGrid>().Single();
            Assert.AreSame(element.TimeSeries, view.TimeSeriesTable.Series);
            Assert.AreEqual(144, element.TimeSeries.Count);
            Assert.AreEqual(element.TimeSeries.Count, grid.Items.Count);
            Assert.AreEqual("Date Time", (string)grid.Columns[1].Header);
            Assert.AreEqual("Value", (string)grid.Columns[2].Header);

            // The statistics tab: upstream's measures, in US formats, with lines above Minimum and below Kurtosis.
            var dataTabs = view.FindControl<TabControl>("DataTabControl");
            dataTabs.SelectedIndex = 1;
            Settle();
            var stats = view.SummaryStatisticsDataGrid;
            var expected = element.TimeSeries.SummaryStatistics().ToList();
            Assert.AreEqual(expected.Count, stats.Items.Count);
            var rows = stats.GetVisualDescendants().OfType<DataGridRow>().Where(r => r.IsVisible).OrderBy(r => r.Index).ToList();
            foreach (var row in rows)
            {
                var statistic = (SummaryStatistic)row.DataContext;
                var cells = row.GetVisualDescendants().OfType<DataGridCell>().OrderBy(c => c.OwningColumn.DisplayIndex).ToList();
                Assert.AreEqual(statistic.Name, CellText(cells[0]));
                string value = double.IsNaN(statistic.Value) ? "N/A" : statistic.Value.ToString("#,##0.####", CultureInfo.GetCultureInfo("en-US"));
                Assert.AreEqual(value, CellText(cells[1]), statistic.Name);
                var border = statistic.Name == "Minimum" ? new Thickness(0, 2, 0, 0) : statistic.Name == "Kurtosis" ? new Thickness(0, 0, 0, 2) : new Thickness(0);
                Assert.AreEqual(border, row.BorderThickness, statistic.Name);
            }
            Assert.IsTrue(rows.Any(r => ((SummaryStatistic)r.DataContext).Name == "Minimum"));
        }

        [STATestMethod]
        public void UsgsTextTab_ShowsTheDownloadedText_ForPeakSeriesOnly()
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(Usgs);
            var peaks = Element<TimeSeriesElement>(project, "USGS - 01614000 - Peak Discharge");
            var (view, window) = Host(peaks);
            Assert.AreEqual(Visibility.Visible, view.USGSTextFileTab.Visibility);
            SelectTab(view, "USGS Text File");
            string[] lines = peaks.USGSRawText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            CollectionAssert.AreEqual(lines, view.USGSRawTextBox.Document.Blocks.Cast<WpfCompat.Documents.Paragraph>()
                .Select(p => ((WpfCompat.Documents.Run)p.Inlines.Single()).Text).ToArray(), "a paragraph for each line, as WPF's TextRange makes");
            Assert.IsTrue(lines.Length > 69, "the RDB file's comments and rows");

            // Daily values have no text tab (MainProjectNode makes a view for each element).
            var (daily, _) = Host(Element<TimeSeriesElement>(project, "USGS - 01134500 - Daily Discharge"));
            Assert.AreEqual(Visibility.Collapsed, daily.USGSTextFileTab.Visibility);
            Assert.AreEqual(1, daily.USGSRawTextBox.Document.Blocks.Count);
            Assert.AreEqual("", ((WpfCompat.Documents.Run)((WpfCompat.Documents.Paragraph)daily.USGSRawTextBox.Document.Blocks[0]).Inlines.Single()).Text);
            window.Close();
        }

        [STATestMethod]
        public void PropertiesPanels_ShowEachSourcesSettings_AsRmcBestFitDid()
        {
            UseAppResources(ThemeColor.Light);
            foreach (var shot in NativeProperties)
            {
                var element = Element<TimeSeriesElement>(OpenProject(shot.Example), shot.Element);
                var properties = new TimeSeriesPropertiesControl { Element = element };
                Window window = Host(properties, 650, 576);
                JsonElement native = NativePlot(NativeScreenshot(shot.Example, shot.Name + "-current.json.gz"));

                // The texts RMC-BestFit showed, in order (a combo box's text is its item's text block). The dates
                // show in this machine's time zone: they're compared with the element's, in WPF's binding culture.
                var expected = native.GetProperty("configuration").GetProperty("visibleControls").EnumerateArray()
                    .Where(c => c.GetProperty("type").GetString() != "ComboBox")
                    .Select(c => c.GetProperty("text").GetString())
                    .ToList();
                expected[expected.IndexOf("Created On") + 1] = UsDate(element.CreationDate);
                expected[expected.IndexOf("Last Modified") + 1] = UsDate(element.LastModified);
                var actual = VisibleTexts(properties);
                Assert.AreEqual(string.Join(" | ", expected), string.Join(" | ", actual), shot.Name);
                Assert.AreEqual(element.EntryMethod == TimeSeriesElement.TimeSeriesEntryMethod.HECDSS ? "Import" : "Download",
                    properties.ProcessDataButtonText.Text);
                window.Close();
            }
        }

        [STATestMethod]
        public void PropertiesPanel_ShowsTheFieldsOfTheEntryMethod_AsUpstream()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<TimeSeriesElement>(OpenProject(Manual), "Nile River Flows");
            var properties = new TimeSeriesPropertiesControl { Element = element };
            Host(properties, 650, 700);
            var entry = (ComboBox)properties.EntryMethod.InnerContent;
            var dataType = (ComboBox)properties.DataType.InnerContent;

            string Shown() => string.Join(",", new Control[]
            {
                properties.TimeIntervalSelector, properties.StartDateTime, properties.HECDSSFilename, properties.HECDSSPathname,
                properties.GHCNSiteNumber, properties.DepthUnitSelector, properties.USGSSiteNumber, properties.CHMNSiteNumber,
                properties.ABOMSiteNumber, properties.DataType, properties.ProcessDataButton,
            }.Where(c => c.Visibility == Visibility.Visible).Select(c => c.Name));

            Assert.AreEqual("Manual Entry", ((TimeSeriesEntryMethodItem)entry.SelectedItem).DisplayName);
            Assert.AreEqual("TimeIntervalSelector,StartDateTime", Shown());

            // Each source shows its own settings and data types; the element picks the source's first type
            // (RMC.BestFit.UI's EntryMethod).
            Choose(entry, TimeSeriesElement.TimeSeriesEntryMethod.USGS);
            Settle();
            Assert.AreEqual(TimeSeriesElement.TimeSeriesEntryMethod.USGS, element.EntryMethod);
            Assert.AreEqual(TimeSeriesDownload.TimeSeriesType.DailyDischarge, element.SeriesType);
            Assert.AreEqual("USGSSiteNumber,DataType,ProcessDataButton", Shown());
            Assert.AreEqual(8, dataType.ItemCount);
            Assert.AreEqual(element.SeriesType, dataType.SelectedValue);
            Assert.AreEqual("Download", properties.ProcessDataButtonText.Text);

            Choose(entry, TimeSeriesElement.TimeSeriesEntryMethod.GHCN);
            Settle();
            Assert.AreEqual("GHCNSiteNumber,DepthUnitSelector,DataType,ProcessDataButton", Shown());
            Assert.AreEqual(TimeSeriesDownload.TimeSeriesType.DailyPrecipitation, element.SeriesType, "a type GHCN has");

            Choose(entry, TimeSeriesElement.TimeSeriesEntryMethod.ABOM);
            Settle();
            Assert.AreEqual(TimeSeriesDownload.TimeSeriesType.DailyDischarge, element.SeriesType);
            Assert.AreEqual("ABOMSiteNumber,DataType,ProcessDataButton", Shown());
            Choose(dataType, TimeSeriesDownload.TimeSeriesType.DailyPrecipitation);
            Assert.AreEqual(TimeSeriesDownload.TimeSeriesType.DailyPrecipitation, element.SeriesType);
            Assert.AreEqual("DepthUnitSelector,ABOMSiteNumber,DataType,ProcessDataButton", Shown(), "ABOM's precipitation has a depth unit");

            Choose(entry, TimeSeriesElement.TimeSeriesEntryMethod.CHMN);
            Settle();
            Assert.AreEqual("CHMNSiteNumber,DataType,ProcessDataButton", Shown());
            Assert.AreEqual(TimeSeriesDownload.TimeSeriesType.DailyDischarge, element.SeriesType);
            Assert.AreEqual(TimeSeriesDownload.TimeSeriesType.DailyDischarge, dataType.SelectedValue);

            Choose(entry, TimeSeriesElement.TimeSeriesEntryMethod.HECDSS);
            Settle();
            Assert.AreEqual("HECDSSFilename,HECDSSPathname,ProcessDataButton", Shown());
            Assert.AreEqual("Import", properties.ProcessDataButtonText.Text);
            Assert.AreEqual("Import Time Series Data", ToolTip.GetTip(properties.ProcessDataButton));

            // The edits are undoable, as the element records them.
            while (element.UndoManager.CanUndo) element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(TimeSeriesElement.TimeSeriesEntryMethod.Manual, element.EntryMethod);
            Assert.AreEqual("Manual Entry", ((TimeSeriesEntryMethodItem)entry.SelectedItem).DisplayName);
        }

        [STATestMethod]
        public void EditingRowsOfARegularSeries_WarnsAsUpstream()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<TimeSeriesElement>(OpenProject(Manual), "Airline Passengers");
            var (view, window) = Host(element);
            var grid = view.TimeSeriesTable.GetVisualDescendants().OfType<ValidationDataGrid>().Single();
            var line = view.TimeSeriesPlot.Series.OfType<Wpf.LineSeries>().Single();
            int count = element.TimeSeries.Count;
            double first = element.TimeSeries[0].Value;
            var seen = new List<string>();

            // Inserting before the end asks first; No leaves the series as it was.
            Select(grid, (2, 2));
            using (AnswerMessageBox("No", (title, text) => seen.Add(title + ": " + text)))
                grid.InsertRows();
            Assert.AreEqual(TimeSeriesControl.RegularRowInsertWarningTitle + ": " + TimeSeriesControl.RegularRowInsertWarningMessage, seen.Single());
            Assert.AreEqual(count, element.TimeSeries.Count);
            Assert.IsFalse(element.UndoManager.CanUndo);

            // Yes inserts a missing value and moves the later dates on; the plot and statistics follow at once.
            using (AnswerMessageBox("Yes", (title, text) => seen.Add(title)))
                grid.InsertRows();
            Assert.AreEqual(count + 1, element.TimeSeries.Count);
            Assert.IsTrue(double.IsNaN(element.TimeSeries[2].Value));
            Assert.AreEqual(new DateTime(1949, 3, 1), element.TimeSeries[2].Index);
            Assert.AreEqual(count + 1, Member<IEnumerable<DataPoint>>(line.InternalSeries, "ActualPoints").Count());
            Assert.AreEqual(element.TimeSeries.SummaryStatistics().Count(), view.SummaryStatisticsDataGrid.Items.Count);
            Assert.AreEqual(1, UndoCount(element), "one undo step for the insert");

            // Deleting asks too.
            Select(grid, (0, 2));
            using (AnswerMessageBox("Yes", (title, text) => seen.Add(title + ": " + text)))
                grid.DeleteRows();
            Assert.AreEqual(TimeSeriesControl.RegularRowDeleteWarningTitle + ": " + TimeSeriesControl.RegularRowDeleteWarningMessage, seen.Last());
            Assert.AreEqual(count, element.TimeSeries.Count);
            Assert.AreEqual(new DateTime(1949, 1, 1), element.TimeSeries[0].Index, "the dates close up from the start");
            Assert.AreEqual(2, UndoCount(element));

            // Adding rows at the end doesn't ask.
            int asked = seen.Count;
            grid.AddRows(2);
            Settle();
            Assert.AreEqual(asked, seen.Count);
            Assert.AreEqual(count + 2, element.TimeSeries.Count);

            while (element.UndoManager.CanUndo) element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(count, element.TimeSeries.Count);
            Assert.AreEqual(first, element.TimeSeries[0].Value);
            Assert.AreEqual(count, Member<IEnumerable<DataPoint>>(line.InternalSeries, "ActualPoints").Count(), "the plot follows the undo");
            window.Close();
        }

        [STATestMethod]
        public void ManualEntry_EditsTheSeries_AndTheViewFollows()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<TimeSeriesElement>(OpenProject(Manual), "Mauna Loa CO2");
            var (view, window) = Host(element);
            var grid = view.TimeSeriesTable.GetVisualDescendants().OfType<ValidationDataGrid>().Single();
            var line = view.TimeSeriesPlot.Series.OfType<Wpf.LineSeries>().Single();
            double before = element.TimeSeries[1].Value;

            EditCell(window, grid, 1, 2, "500.25");
            Assert.AreEqual(500.25, element.TimeSeries[1].Value);
            Assert.AreEqual(500.25, Member<IEnumerable<DataPoint>>(line.InternalSeries, "ActualPoints").ElementAt(1).Y, "the plot follows the edit");
            var maximum = element.TimeSeries.SummaryStatistics().Single(s => s.Key == "Maximum").Value;
            Assert.AreEqual(500.25, maximum);
            Assert.IsTrue(view.SummaryStatisticsDataGrid.Items.Cast<SummaryStatistic>().Any(s => s.Name == "Maximum" && s.Value == 500.25), "the statistics follow the edit");

            element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(before, element.TimeSeries[1].Value);
            Assert.AreEqual(before, Member<IEnumerable<DataPoint>>(line.InternalSeries, "ActualPoints").ElementAt(1).Y, "the plot follows the undo");

            // The properties panel's interval and start date make the series' dates.
            var properties = new TimeSeriesPropertiesControl { Element = element };
            Window panel = Host(properties, 650, 600);
            Choose((ComboBox)properties.TimeIntervalSelector.InnerContent, TimeInterval.OneYear);
            Settle();
            Assert.AreEqual(TimeInterval.OneYear, element.TimeSeries.TimeInterval);
            Assert.AreEqual(element.TimeSeries[0].Index.AddYears(1), element.TimeSeries[1].Index);
            Assert.AreEqual(element.TimeSeries.Count, grid.Items.Count);
            Assert.AreEqual(element.TimeSeries[1].Index.ToString("g", CultureInfo.GetCultureInfo("en-US")), GridText(grid, 1, 1), "the table shows the new dates");
            panel.Close();
            window.Close();
        }

        [STATestMethod]
        public void DssPathSelector_ListsTheFilesPaths_AndGivesTheDatelessPath()
        {
            UseAppResources(ThemeColor.Light);
            string dss = CopyExampleDss();
            var selector = new DSSPathSelectorWindow { FullFileName = dss };
            selector.Show();
            _windows.Add(selector);
            Settle();

            List<Hec.Dss.DssPath> catalog;
            using (var reader = new Hec.Dss.DssReader(dss)) catalog = reader.GetCatalog().ToList();
            Assert.AreEqual(dss, selector.FileNameTextBox.Text);
            Assert.AreEqual(catalog.Count, selector.MyDataGrid.Items.Count);
            Assert.AreEqual(catalog.Select(p => p.Bpart).Distinct().Count() + 1, selector.BPartComboBox.ItemCount, "each B part, and none");
            Assert.IsFalse(selector.SetPathButton.IsEnabled);

            // Filtering by parts, as the native screenshot's grid shows; small sets show their data's actual range.
            selector.BPartComboBox.SelectedItem = "GRAPEVINE INFLOW";
            selector.CPartComboBox.SelectedItem = "FLOW";
            Settle();
            var row = selector.MyDataGrid.Items.Cast<DssPathSelectorRow>().Single();
            Assert.AreEqual("16Mar2007-10Jul2007", row.DisplayDpart, "the inflow's data range, as the native screenshot shows");
            selector.MyDataGrid.SelectedItem = row;
            Settle();
            Assert.AreEqual("//GRAPEVINE INFLOW/FLOW/16Mar2007-10Jul2007/1Hour/RUN:2007_MAR-JUL/", selector.PathNameTextBox.Text);
            Assert.IsTrue(selector.SetPathButton.IsEnabled);

            selector.SetPathButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.AreEqual(true, selector.DialogResult);
            Assert.AreEqual("//GRAPEVINE INFLOW/FLOW//1Hour/RUN:2007_MAR-JUL/", selector.SelectedPathname);

            // HEC's test files list too; a file HEC-DSS 7 can't open says so.
            foreach (string name in new[] { "MoRiverObs7.dss", "BALDEAGLE7.DSS" })
            {
                string file = Path.Combine(RepositoryRoot, "sample-data", "dss-test-data", name);
                if (!File.Exists(file)) Assert.Inconclusive($"These tests need HEC's DSS test data (scripts/fetch-sample-data.sh dss-test-data): {file}");
                var other = new DSSPathSelectorWindow { FullFileName = file };
                using var reader = new Hec.Dss.DssReader(file);
                Assert.AreEqual(reader.GetCatalog().Count, other.MyDataGrid.Items.Count, name);
                other.Close();
            }
        }

        [STATestMethod]
        public void PathnameButton_OpensTheSelector_AndImportReadsTheChosenSeries()
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(Dss);
            var element = Element<TimeSeriesElement>(project, "Grapevine Dam - Inflow");
            var stored = element.TimeSeries.Select(o => (o.Index, o.Value)).ToList();
            string dss = Path.Combine(Path.GetDirectoryName(project.FullFileName), "dss-example-data.dss");
            File.Copy(Example("1-time-series-data/5-hec-dss-import/dss-example-data.dss"), dss);
            var properties = new TimeSeriesPropertiesControl { Element = element };
            Host(properties, 650, 600);

            // The stored file name is the author's: the button does nothing until the file exists, as upstream.
            var browse = properties.HECDSSPathname.GetVisualDescendants().OfType<Button>().Single();
            ClickButton(browse);
            element.HECDSSFullFilename = dss;
            element.HECDSSDataPathname = "";
            Settle();
            using (WhenOpened<DSSPathSelectorWindow>(selector =>
            {
                selector.BPartComboBox.SelectedItem = "GRAPEVINE INFLOW";
                selector.CPartComboBox.SelectedItem = "FLOW";
                selector.MyDataGrid.SelectedItem = selector.MyDataGrid.Items.Cast<object>().Single();
                selector.SetPathButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }))
            {
                ClickButton(browse);
            }
            Assert.AreEqual("//GRAPEVINE INFLOW/FLOW//1Hour/RUN:2007_MAR-JUL/", element.HECDSSDataPathname);

            // Import reads the series from the file: the example's stored series is that path's data.
            WithUiContext(() =>
            {
                ClickButton(properties.ProcessDataButton);
                for (int i = 0; i < 200 && !properties.ProcessDataButton.IsEnabled; i++)
                {
                    System.Threading.Thread.Sleep(50);
                    Settle();
                }
            });
            Assert.IsTrue(properties.ProcessDataButton.IsEnabled, "the import finished");
            Assert.IsNull(WpfCompat.Input.Mouse.OverrideCursor);
            CollectionAssert.AreEqual(stored, element.TimeSeries.Select(o => (o.Index, o.Value)).ToList());
        }

        [STATestMethod]
        [TestCategory("External")]
        public void Download_ReplacesTheSeriesWithTheProvidersRecord_AndTheViewFollows()
        {
            // USGS's annual peaks at Back Creek, a small record.
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(Usgs);
            var element = Element<TimeSeriesElement>(project, "USGS - 01614000 - Peak Discharge");
            var stored = element.TimeSeries.Select(o => (o.Index, o.Value)).Take(20).ToList();
            var (view, _) = Host(element);
            var properties = new TimeSeriesPropertiesControl { Element = element };
            Host(properties, 650, 600);
            Assert.AreEqual("Download", properties.ProcessDataButtonText.Text);

            WithUiContext(() =>
            {
                ClickButton(properties.ProcessDataButton);
                Assert.IsFalse(properties.ProcessDataButton.IsEnabled, "the button waits for the download");
                for (int i = 0; i < 600 && !properties.ProcessDataButton.IsEnabled; i++)
                {
                    System.Threading.Thread.Sleep(100);
                    Settle();
                }
            });
            Assert.IsTrue(properties.ProcessDataButton.IsEnabled, "the download finished, or timed out with a message");
            Assert.IsTrue(element.TimeSeries.Count >= 69, "the record has at least the saved peaks");
            CollectionAssert.AreEqual(stored, element.TimeSeries.Select(o => (o.Index, o.Value)).Take(20).ToList(), "the early peaks are the saved ones");
            var line = view.TimeSeriesPlot.Series.OfType<Wpf.LineSeries>().Single();
            Assert.AreSame(element.TimeSeries, line.ItemsSource, "the plot shows the new record");
            Assert.IsTrue(view.USGSRawTextBox.Document.Blocks.Count > element.TimeSeries.Count, "the text tab shows the new file");
        }

        [STATestMethod]
        public void AlternativeTimeSeries_AreAddedRemovedAndRefreshed()
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(Dss);
            var inflow = Element<TimeSeriesElement>(project, "Grapevine Dam - Inflow");
            var outflow = Element<TimeSeriesElement>(project, "Grapevine Dam - Outflow");
            var (view, window) = Host(inflow);
            var selector = view.AlternativeTimeSeriesSelector;
            var plot = view.TimeSeriesPlot;

            // The label toggles the panel, as the plus/minus button does.
            Assert.AreEqual(Visibility.Collapsed, selector.Visibility);
            Click(window, Centre(window, view.AlternativesTextBlock));
            Assert.AreEqual(true, view.ShowAlternativesToggleButton.IsChecked);
            Assert.AreEqual(Visibility.Visible, selector.Visibility);

            // Every other time series, by name; checking one adds its line in a colour of its own.
            var list = selector.AlternativeListBox;
            CollectionAssert.AreEqual(new[] { "Grapevine Dam - Outflow" }, list.Items.Cast<TimeSeriesAlternativeItem>().Select(i => i.Alternative.Name).ToList());
            var item = selector.TimeSeriesList.Single();
            var box = list.GetVisualDescendants().OfType<CheckBox>().Single();
            Assert.IsTrue(box.IsEnabled, "it has data");
            Click(window, Centre(window, box));
            Assert.IsTrue(item.IsChecked);
            Assert.AreEqual(2, plot.Series.Count);
            Assert.AreSame(item.TimeSeriesLine, plot.Series[1]);
            Assert.AreEqual("Grapevine Dam - Outflow", item.TimeSeriesLine.Title);
            Assert.AreSame(outflow.TimeSeries, item.TimeSeriesLine.ItemsSource);
            Assert.AreEqual(Color(GeneralMethods.RandomColorsLongList[4]), item.TimeSeriesLine.Color, "the first of the list's colours from the fifth that no line has");
            Assert.AreEqual(outflow.TimeSeries.Count, Member<IEnumerable<DataPoint>>(item.TimeSeriesLine.InternalSeries, "ActualPoints").Count());

            // New data for the alternative refreshes its line; a redraw of the element's plot keeps it.
            outflow.TimeSeries = new TimeSeries(TimeInterval.OneHour, outflow.TimeSeries[0].Index, outflow.TimeSeries.Select(o => o.Value * 2).ToArray());
            Settle();
            Assert.AreSame(outflow.TimeSeries, item.TimeSeriesLine.ItemsSource);
            Assert.AreEqual(2, plot.Series.Count, "the line is replaced, not added again");
            inflow.TimeSeries = new TimeSeries(TimeInterval.OneHour, inflow.TimeSeries[0].Index, inflow.TimeSeries.Select(o => o.Value).ToArray());
            Settle();
            Assert.AreEqual(2, plot.Series.Count);
            Assert.AreSame(item.TimeSeriesLine, plot.Series[1]);

            // Unchecking removes it.
            Click(window, Centre(window, box));
            Assert.IsFalse(item.IsChecked);
            Assert.AreEqual(1, plot.Series.Count);

            // The list follows the project's time series, sorted by name as they're added and renamed.
            var added = new TimeSeriesElement("A New Series", inflow.ParentCollection);
            inflow.ParentCollection.Add(added);
            Settle();
            CollectionAssert.AreEqual(new[] { "A New Series", "Grapevine Dam - Outflow" }, list.Items.Cast<TimeSeriesAlternativeItem>().Select(i => i.Alternative.Name).ToList());
            var newBox = list.GetVisualDescendants().OfType<CheckBox>().Single(c => ((TimeSeriesAlternativeItem)c.DataContext).Alternative == added);
            Assert.AreEqual(added.TimeSeries != null && added.TimeSeries.Count > 0, newBox.IsEnabled, "a series can be compared when it has data");
            added.Name = "Zebra Series";
            Settle();
            CollectionAssert.AreEqual(new[] { "Grapevine Dam - Outflow", "Zebra Series" }, list.Items.Cast<TimeSeriesAlternativeItem>().Select(i => i.Alternative.Name).ToList());

            // A removed series leaves the list, and the plot.
            item.IsChecked = true;
            Settle();
            Assert.AreEqual(2, plot.Series.Count);
            inflow.ParentCollection.Remove(outflow);
            Settle();
            CollectionAssert.AreEqual(new[] { "Zebra Series" }, list.Items.Cast<TimeSeriesAlternativeItem>().Select(i => i.Alternative.Name).ToList());
            Assert.AreEqual(1, plot.Series.Count);
            window.Close();
        }

        [STATestMethod]
        public void ShowingTheViews_ChangesNothingInTheElement()
        {
            UseAppResources(ThemeColor.Light);
            foreach (string example in Examples)
            {
                var project = OpenProject(example);
                foreach (var element in Elements(project).OfType<TimeSeriesElement>().Where(e => e.TimeSeries.Count <= LargeRecord).ToList())
                    AssertShowingChangesNothing(example, element);
                CloseProject(project);
            }
        }

        [STATestMethod]
        public void PreviewClicks_AndToolbarProperties_ReachTheShell()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<TimeSeriesElement>(OpenProject(Manual), "Airline Passengers");
            var (view, window) = Host(element);
            var clicks = new List<(bool Plot, bool Toolbar, Wpf.Plot Clicked)>();
            view.PreviewControlClicked += (plotClicked, toolbarClicked, plot) => clicks.Add((plotClicked, toolbarClicked, plot));
            var calls = new List<(Wpf.Plot Plot, bool Open)>();
            view.PlotPropertiesCalled += (plot, open, expander, selected) => calls.Add((plot, open));

            Click(window, Centre(window, element.TimeSeriesPlot));
            Assert.AreEqual((true, false, element.TimeSeriesPlot), clicks.Last());
            Assert.IsTrue(view.PlotClicked);

            Point toolbar = HitPoint(window, view.PlotToolbar);
            window.MouseDown(toolbar, MouseButton.Left);
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            Settle();
            Assert.AreEqual((false, true, element.TimeSeriesPlot), clicks.Last());
            Assert.IsFalse(view.PlotClicked);

            // Each tab's toolbar asks for its own plot's properties.
            foreach (var (tab, plot, bar) in new[]
            {
                ("Time Series", element.TimeSeriesPlot, view.PlotToolbar),
                ("Seasonality Plot", element.SeasonalityPlot, view.SeasonalityPlotToolbar),
                ("ACF Plot", element.ACFPlot, view.ACFPlotToolbar),
                ("PACF Plot", element.PACFPlot, view.PACFPlotToolbar),
            })
            {
                SelectTab(view, tab);
                Assert.AreSame(plot, view.GetCurrentPlot(), tab);
                bar.FindControl<Button>("PropertiesButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Settle();
                Assert.AreEqual((plot, true), calls.Last(), tab);
            }
            Assert.AreEqual(4, calls.Count);

            // Clicking the data tab's toolbar from another tab isn't possible; a click away from the plots reports neither.
            Click(window, new Point(5, 200));
            Assert.AreEqual((false, false, element.PACFPlot), clicks.Last());
        }

        [STATestMethod]
        public void ADocumentOpenedAgain_TakesThePlotsFromTheClosedOne()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<TimeSeriesElement>(OpenProject(Manual), "Airline Passengers");

            // The shell closes a document by taking it out of its window, and upstream's MainProjectNode leaves
            // the closed view holding the element.
            var (first, window) = Host(element);
            ((Border)window.Content).Child = null;
            Settle();
            window.Close();
            _windows.Remove(window);

            var (second, _) = Host(element);
            Assert.AreSame(element.TimeSeriesPlot, second.TimeSeriesPlotHost.Content);
            Assert.AreSame(element.SeasonalityPlot, second.SeasonalityPlotHost.Content);
            Assert.AreSame(element.ACFPlot, second.ACFPlotHost.Content);
            Assert.AreSame(element.PACFPlot, second.PACFPlotHost.Content);
            Assert.IsNull(first.TimeSeriesPlotHost.Content, "the plot moved");
        }

        // Helpers.

        private BestFitProject OpenProject(string relative)
        {
            var project = Open(relative);
            _projects.Add(project);
            return project;
        }

        private (TimeSeriesControl View, Window Window) Host(TimeSeriesElement element, string tab = "Time Series")
        {
            var shown = ShowView(element, tab);
            _windows.Add(shown.Window);
            return shown;
        }

        // Shows the view of an element, visits each of its tabs and shows its properties, as a user does, then closes
        // them as the shell closes a document: nothing in the element may change.
        private void AssertShowingChangesNothing(string example, TimeSeriesElement element)
        {
            var changes = new List<string>();
            PropertyChangedEventHandler watch = (sender, e) => changes.Add(e.PropertyName);
            NotifyCollectionChangedEventHandler data = (sender, e) => changes.Add("TimeSeries " + e.Action);
            element.PropertyChanged += watch;
            element.TimeSeries.CollectionChanged += data;
            var (view, window) = Host(element);
            foreach (var tab in view.MainTabControl.Items.OfType<TabItem>().Where(t => t.IsVisible).ToList())
                SelectTab(view, (string)tab.Header);
            var properties = new TimeSeriesPropertiesControl { Element = element };
            Window panel = Host(properties, 650, 576);

            // The shell closes a document or properties panel by taking it out of its window.
            ((Border)panel.Content).Child = null;
            ((Border)window.Content).Child = null;
            Settle();
            panel.Close();
            window.Close();
            properties.Element = null;
            view.Element = null;
            element.PropertyChanged -= watch;
            element.TimeSeries.CollectionChanged -= data;
            _windows.Remove(window);
            _windows.Remove(panel);
            Release();

            string name = $"{Path.GetFileName(example)}: {element.Name}";
            Assert.AreEqual(0, changes.Count, $"{name}: showing the views changed " + string.Join(", ", changes));
            Assert.IsFalse(element.UndoManager.CanUndo, $"{name}: showing the views recorded an undo");
            Assert.IsFalse(element.IsDirty, $"{name}: showing the views made the element dirty");
        }

        // Runs an action with Avalonia's synchronization context, as the application's UI thread has it, so that the
        // properties panel's async Download/Import handler continues on the UI thread ([STATestMethod]s run without one,
        // and the handler would then go on in the thread pool and touch the controls from there).
        private static void WithUiContext(Action action)
        {
            var previous = System.Threading.SynchronizationContext.Current;
            System.Threading.SynchronizationContext.SetSynchronizationContext(null);
            AvaloniaSynchronizationContext.InstallIfNeeded();
            try
            {
                action();
            }
            finally
            {
                System.Threading.SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        // Lets a closed view go: a view of a million observations holds a gigabyte (NumericControls' table makes a
        // row for each), so the tests don't keep two.
        private static void Release()
        {
            Settle();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // Closes a project OpenProject opened, before the test ends.
        private void CloseProject(BestFitProject project)
        {
            TimeSeriesDataTesting.CloseProject(project);
            _projects.Remove(project);
            GC.Collect();
        }

        // A native screenshot's capture data (screenshots/*.json.gz beside an example), to dispose after use: some
        // hold a million points.
        private static JsonDocument NativeCapture(string relative)
        {
            using var stream = new System.IO.Compression.GZipStream(File.OpenRead(Example(relative)), System.IO.Compression.CompressionMode.Decompress);
            return JsonDocument.Parse(stream);
        }

        private Window Host(Control control, double width, double height)
        {
            var window = Show(control, width, height);
            _windows.Add(window);
            return window;
        }

        private string CopyExampleDss()
        {
            string folder = Path.Combine(Path.GetTempPath(), "XGoodFitTimeSeriesData", Guid.NewGuid().ToString("N").Substring(0, 12));
            Directory.CreateDirectory(folder);
            _folders.Add(folder);
            string copy = Path.Combine(folder, "dss-example-data.dss");
            File.Copy(Example("1-time-series-data/5-hec-dss-import/dss-example-data.dss"), copy);
            return copy;
        }

        private static int UndoCount(TimeSeriesElement element)
        {
            int count = 0;
            while (element.UndoManager.CanUndo)
            {
                element.UndoManager.Undo();
                count++;
            }
            for (int i = 0; i < count; i++) element.UndoManager.Redo();
            Settle();
            return count;
        }

        private static Avalonia.Media.Color Color(string hex) => (Avalonia.Media.Color)WpfCompat.Media.ColorConverter.ConvertFromString(hex);

        private static T Member<T>(object owner, string name)
        {
            for (Type type = owner.GetType(); type != null; type = type.BaseType)
            {
                var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property != null) return (T)property.GetValue(owner);
            }
            throw new MissingMemberException(owner.GetType().Name, name);
        }

        private static void AssertPoints(JsonElement expected, IEnumerable<DataPoint> actual, string what)
        {
            // A hidden series is never updated, so it has no points, as RMC-BestFit's export recorded.
            var points = actual?.ToList() ?? new List<DataPoint>();
            Assert.AreEqual(expected.GetArrayLength(), points.Count, $"{what}: points");
            int i = 0;
            foreach (JsonElement point in expected.EnumerateArray())
            {
                double x = Number(point.GetProperty("X")), y = Number(point.GetProperty("Y"));
                if (!(x.Equals(points[i].X) && (y.Equals(points[i].Y) || Math.Abs(y - points[i].Y) <= 1e-12 * Math.Max(1, Math.Abs(y)))))
                    Assert.Fail($"{what}: point {i} is {Text(points[i])}, RMC-BestFit's {x.ToString("R", CultureInfo.InvariantCulture)},{y.ToString("R", CultureInfo.InvariantCulture)}");
                i++;
            }
        }

        // The element's plot a tab of the view shows.
        private static Wpf.Plot PlotOf(TimeSeriesElement element, string tab) => tab switch
        {
            "Seasonality Plot" => element.SeasonalityPlot,
            "ACF Plot" => element.ACFPlot,
            "PACF Plot" => element.PACFPlot,
            _ => element.TimeSeriesPlot,
        };

        // Picks the item with this value, as a user's click does.
        private static void Choose(ComboBox box, object value)
        {
            box.SelectedItem = box.Items.Cast<object>().Single(item => Equals(item.GetType().GetProperty("Value").GetValue(item), value));
            Settle();
        }

        private static double Number(JsonElement value) =>
            value.ValueKind == JsonValueKind.Number ? value.GetDouble() : double.Parse(value.GetString() ?? "NaN", CultureInfo.InvariantCulture);

        private static void ClickButton(Button button)
        {
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
        }

        private static void Select(DataGrid grid, params (int Row, int Column)[] cells)
        {
            var items = grid.CollectionView.Cast<object>().ToList();
            grid.SelectedCells = cells
                .Select(c =>
                {
                    DataGridColumn column = grid.Columns.Single(col => col.DisplayIndex == c.Column);
                    return new DataGridCellInfo(items[c.Row], column, c.Row, grid.Columns.IndexOf(column));
                })
                .ToList();
            Settle();
        }

        private static DataGridCell Cell(DataGrid grid, int row, int column) =>
            grid.GetVisualDescendants().OfType<DataGridCell>()
                .Single(c => c.OwningRow is { } r && r.IsVisible && r.Index == row && c.OwningColumn?.DisplayIndex == column && grid.Columns.Contains(c.OwningColumn));

        private static string GridText(DataGrid grid, int row, int column) => CellText(Cell(grid, row, column));

        private static string CellText(DataGridCell cell) => cell.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()?.Text ?? "";

        // Edits a cell as a user does: F2, the text, Enter.
        private static void EditCell(Window window, CopyPasteDataGrid grid, int row, int column, string text)
        {
            grid.Focus();
            grid.CurrentCell = new DataGridCellInfo(grid.Items[row], grid.Columns[column], row, column);
            Settle();
            window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);
            Settle();
            var editor = (TextBox)Cell(grid, row, column).Content;
            editor.SelectAll();
            foreach (char c in text) window.KeyTextInput(c.ToString());
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Settle();
        }

        // Runs an answer on the next window of type T that opens, once it's ready (a modal window runs its own loop).
        private static IDisposable WhenOpened<T>(Action<T> answer) where T : Window
        {
            IDisposable handler = null;
            handler = Window.WindowOpenedEvent.AddClassHandler<T>((window, _) =>
            {
                handler?.Dispose();
                Dispatcher.UIThread.Post(() => answer(window), DispatcherPriority.Background);
            });
            return handler;
        }

        // Answers the next message box with the button given, and gives back its title and text.
        private static IDisposable AnswerMessageBox(string button, Action<string, string> seen) =>
            WhenOpened<MessageBoxWindow>(window =>
            {
                seen(window.Title, window.FindControl<TextBlock>("MessageText")?.Text ?? "");
                window.FindControl<StackPanel>("ButtonPanel").Children.OfType<Button>()
                    .Single(b => (b.Content as string ?? (b.Content as TextBlock)?.Text) == button)
                    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            });
    }
}
