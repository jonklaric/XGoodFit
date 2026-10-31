using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTesting;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTimeSeriesTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the HEC-DSS import walkthrough (examples/1-time-series-data/5-hec-dss-import/hec-dss-import-example.md)
    /// followed through the main window: the explorer and properties, the two hydrographs' inventory and steps, the import
    /// from the supplied DSS file (browse to it, the HEC-DSS Pathname Selector, Set Path, Import), the Alternative Time Series
    /// overlay, the figures and the maxima. The native plot exports are TimeSeriesDataTests'. See
    /// docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughHecDssImportTests
    {
        private const string Folder = "1-time-series-data/5-hec-dss-import/";
        private const string Inflow = "Grapevine Dam - Inflow";
        private const string Outflow = "Grapevine Dam - Outflow";
        private const string InflowPath = "//GRAPEVINE INFLOW/FLOW//1Hour/RUN:2007_MAR-JUL/";
        private const string ReleasePath = "//GRAPEVINE LAKE-RELEASE/FLOW//1Hour/RUN:2007_MAR-JUL/";
        private static readonly WalkthroughSession Session = new WalkthroughSession("hec-dss-import-example");

        private static readonly Record[] Records =
        {
            new(Inflow, "CFS", "2007-03-16", "2007-07-10", 2785, 0),
            new(Outflow, "CFS", "2007-03-16", "2007-07-10", 2785, 0),
        };

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        // The main window keeps the storage provider it gets first: the picker goes in before it opens.
        private static WalkthroughSession Open()
        {
            _ = WalkthroughFilePicker.Instance;
            return Session.Open();
        }

        /// <summary>
        /// "Find the controls": the explorer (the walkthrough's capture), and the inflow's properties (the capture of the
        /// panel): Data Entry Method = HEC-DSS, DSS Filename and DSS Pathname, with their ... buttons; the document's tabs.
        /// </summary>
        [TestMethod]
        public void FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Open();
                AssertExplorer(session, Folder + "screenshots/hec-dss-project-explorer-current.json.gz");
                var view = session.OpenDocument<TimeSeriesControl>(Inflow);
                var properties = (TimeSeriesPropertiesControl)session.Properties;
                AssertPanel(properties, Folder + "screenshots/hec-dss-properties-panel-current.json.gz");
                Assert.AreEqual("HEC-DSS", Property(properties, "Data Entry Method"));
                Assert.AreEqual(InflowPath, Property(properties, "DSS Pathname"));
                StringAssert.EndsWith(Property(properties, "DSS Filename").Replace('\\', '/'), "/dss-example-data.dss", "the author's path to the supplied file");
                Assert.AreEqual("...", Browse(properties.HECDSSFilename).Content as string ?? ((TextBlock)Browse(properties.HECDSSFilename).Content).Text);
                Assert.IsNotNull(Browse(properties.HECDSSPathname));
                Assert.AreEqual("Import", properties.ProcessDataButtonText.Text);
                AssertTabs(view);
                session.AssertUnchanged(Inflow);
            });
        }

        /// <summary>
        /// The inventory, and steps 1-2: hourly values; the outflow's 2,785 timestamps and CFS units are the inflow's. "Check
        /// your understanding": both pathnames are FLOW at a one-hour interval. And the limits: the inflow's maximum is about
        /// 34,118 cfs and the release's 1,382 cfs.
        /// </summary>
        [TestMethod]
        public void SavedRecords_AreTheInventorys_AndShareTheirHourlyClock()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Open();
                foreach (Record record in Records)
                    AssertRecord(session, record);

                var times = new List<List<double>>();
                var maxima = new List<string>();
                var paths = new List<string>();
                foreach (string name in new[] { Inflow, Outflow })
                {
                    var view = session.OpenDocument<TimeSeriesControl>(name);
                    paths.Add(Property(session.Properties, "DSS Pathname"));
                    Show(view, "Time Series");
                    var grid = Table(view);
                    Assert.AreEqual(TimeSpan.FromHours(1), TableDate(grid, 1) - TableDate(grid, 0), name + ": hourly");
                    times.Add(Points(view.TimeSeriesPlot.Series.Single().InternalSeries, "ActualPoints").Select(p => p.X).ToList());
                    maxima.Add(SummaryStatistics(view)["Maximum"]);
                    session.CloseDocuments();
                }
                CollectionAssert.AreEqual(times[0], times[1], "the outflow's timestamps are the inflow's");
                Assert.AreEqual(2785, times[0].Count);
                CollectionAssert.AreEqual(new[] { InflowPath, ReleasePath }, paths);
                foreach (string path in paths)
                    StringAssert.Contains(path, "/FLOW//1Hour/", "FLOW at a one-hour interval");
                AssertRounded(34118, maxima[0], 0, "the maximum inflow");
                AssertRounded(1382, maxima[1], 0, "the maximum release");
                session.AssertUnchanged(Inflow, Outflow);
            });
        }

        /// <summary>
        /// Steps 3-5 on a working copy: browse (DSS Filename's ...) to the supplied file in place of the author's path; open
        /// the HEC-DSS Pathname Selector (DSS Pathname's ...), select the element's record and Set Path; Import; then the
        /// first and last dates, hourly spacing, units and ordinates are the inventory's. The selector is the walkthrough's
        /// capture of it: its title, the file's 199 records, and the inflow record's pathname with its dates.
        /// </summary>
        [TestMethod]
        public void Import_FromTheSuppliedFile_GivesTheSavedRecords()
        {
            HeadlessSession.Run(() =>
            {
                try
                {
                    WalkthroughSession session = Open();
                    JsonCapture selectorCapture = new JsonCapture(Data(Folder + "screenshots/hec-dss-path-selector-current.json.gz"));
                    string dss = Path.Combine(BestFitShell.Folder, "dss-example-data.dss");
                    File.Copy(SharedControlsTesting.Example(Folder + "dss-example-data.dss"), dss, overwrite: true);
                    foreach (var (record, path, part) in new[] { (Records[0], InflowPath, "GRAPEVINE INFLOW"), (Records[1], ReleasePath, "GRAPEVINE LAKE-RELEASE") })
                    {
                        var element = session.Element<TimeSeriesElement>(record.Element);
                        var stored = element.TimeSeries.Select(o => (o.Index, o.Value)).ToList();
                        session.OpenDocument<TimeSeriesControl>(element);
                        var properties = (TimeSeriesPropertiesControl)session.Properties;

                        // Step 3: browse to the supplied file.
                        WalkthroughFilePicker.Instance.OpenPath = dss;
                        Click(Browse(properties.HECDSSFilename));
                        Assert.AreEqual(dss, Property(properties, "DSS Filename"), record.Element + ": the local file replaces the author's path");

                        // Step 4: the pathname selector, the element's record, Set Path.
                        DSSPathSelectorWindow selector = null;
                        string shownPath = null, title = null, file = null;
                        int rows = 0;
                        using (session.Expect<DSSPathSelectorWindow>())
                        using (BestFitShell.WhenOpened<DSSPathSelectorWindow>(opened =>
                        {
                            selector = opened;
                            title = opened.Title;
                            file = opened.FileNameTextBox.Text;
                            rows = opened.MyDataGrid.Items.Count;
                            opened.BPartComboBox.SelectedItem = part;
                            opened.CPartComboBox.SelectedItem = "FLOW";
                            BestFitShell.Frame();
                            opened.MyDataGrid.SelectedItem = opened.MyDataGrid.Items.Cast<object>().Single();
                            BestFitShell.Frame();
                            shownPath = opened.PathNameTextBox.Text;
                            opened.SetPathButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }))
                        {
                            Click(Browse(properties.HECDSSPathname));
                            BestFitShell.WaitUntil(() => selector != null && !selector.IsVisible, TimeSpan.FromSeconds(30), "the selector to close");
                        }
                        Assert.AreEqual(selectorCapture.Title, title);
                        Assert.AreEqual(dss, file, "the selector reads the chosen file");
                        Assert.AreEqual(selectorCapture.Rows, rows, "the file's records");
                        if (part == "GRAPEVINE INFLOW")
                        {
                            Assert.AreEqual(selectorCapture.DisplayedPath, shownPath, "the selected record, with its dates, as the capture shows it");
                            Assert.AreEqual(selectorCapture.SelectedPath, path);
                        }
                        Assert.AreEqual(path, Property(properties, "DSS Pathname"), record.Element);

                        // Step 5: Import; the record is the inventory's.
                        Click(properties.ProcessDataButton);
                        BestFitShell.WaitUntil(() => properties.ProcessDataButton.IsEnabled, TimeSpan.FromMinutes(1), "the import");
                        BestFitShell.Frame();
                        CollectionAssert.AreEqual(stored, element.TimeSeries.Select(o => (o.Index, o.Value)).ToList(), record.Element + ": the import gives the saved record");
                        // The shell asks about the changed element (its document's title has a *): save the working copy.
                        session.CloseDocuments(save: "Save");
                        AssertRecord(session, record);
                        var view = session.OpenDocument<TimeSeriesControl>(element);
                        Show(view, "Time Series");
                        Assert.AreEqual(TimeSpan.FromHours(1), TableDate(Table(view), 1) - TableDate(Table(view), 0), "hourly spacing");
                        session.CloseDocuments();
                    }
                    session.AssertUnchanged();
                }
                finally
                {
                    // The import changed the shared copy: the next test opens the example again.
                    Session.Close();
                }
            });
        }

        /// <summary>
        /// Step 5's comparison: with the inflow shown, the plus control expands Alternative Time Series above the plot;
        /// checking the outflow overlays its hydrograph (its own line, the outflow's points), and clearing it removes it.
        /// </summary>
        [TestMethod]
        public void AlternativeTimeSeries_OverlayTheOutflow_AndClearingItRemovesIt()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Open();
                var view = session.OpenDocument<TimeSeriesControl>(Inflow);
                Show(view, "Time Series");
                Assert.IsFalse(view.AlternativeTimeSeriesSelector.IsVisible, "collapsed at first");
                view.ShowAlternativesToggleButton.IsChecked = true;
                BestFitShell.Frame();
                Assert.IsTrue(view.AlternativeTimeSeriesSelector.IsEffectivelyVisible, "the plus control expands the list");
                var list = view.AlternativeTimeSeriesSelector.AlternativeListBox;
                CollectionAssert.AreEqual(new[] { Outflow }, list.Items.Cast<TimeSeriesAlternativeItem>().Select(i => i.Alternative.Name).ToList());
                var box = list.GetVisualDescendants().OfType<CheckBox>().Single();
                box.IsChecked = true;
                BestFitShell.Frame();
                var plot = view.TimeSeriesPlot;
                CollectionAssert.AreEqual(new[] { Inflow, Outflow }, plot.Series.Select(s => s.Title).ToList(), "the outflow is overlaid");
                var outflow = session.Element<TimeSeriesElement>(Outflow);
                CollectionAssert.AreEqual(outflow.TimeSeries.Select(o => o.Value).ToList(),
                    Points(plot.Series[1].InternalSeries, "ActualPoints").Select(p => p.Y).ToList(), "the outflow's hydrograph");
                box.IsChecked = false;
                BestFitShell.Frame();
                CollectionAssert.AreEqual(new[] { Inflow }, plot.Series.Select(s => s.Title).ToList(), "clearing the box removes the overlay");
                view.ShowAlternativesToggleButton.IsChecked = false;
                session.CloseDocuments();
                session.AssertUnchanged(Inflow, Outflow);
            });
        }

        /// <summary>The walkthrough's figures (figures/*.plotspec.json.gz) are what the views show.</summary>
        [TestMethod]
        public void Figures_AreWhatTheViewsShow()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Open();
                CollectionAssert.AreEqual(new[] { "hec-dss-import-example-inflow.plotspec.json.gz", "hec-dss-import-example-outflow.plotspec.json.gz" },
                    AssertFigures(session, Folder));
            });
        }

        // The ... button of a properties row.
        private static Button Browse(Control row) => row.GetVisualDescendants().OfType<Button>().Single(b => b.IsEffectivelyVisible);

        // The walkthrough's capture of the pathname selector.
        private sealed class JsonCapture
        {
            public JsonCapture(System.Text.Json.JsonElement data)
            {
                System.Text.Json.JsonElement configuration = data.GetProperty("configuration");
                Title = configuration.GetProperty("dialogTitle").GetString();
                Rows = configuration.GetProperty("catalogRows").GetInt32();
                DisplayedPath = configuration.GetProperty("dssDisplayedPath").GetString();
                SelectedPath = configuration.GetProperty("dssSelectedPath").GetString();
            }

            public string Title { get; }
            public int Rows { get; }
            public string DisplayedPath { get; }
            public string SelectedPath { get; }
        }
    }
}
