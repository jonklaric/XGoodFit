using System;
using System.Linq;
using Avalonia.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTesting;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTimeSeriesTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Australian Water Data Online walkthrough
    /// (examples/1-time-series-data/4-abom-download/abom-download-example.md) followed through the main window: the explorer
    /// and properties, the six records' inventory, the steps (rainfall depth, discharge and stage, missing values), the
    /// figures, and a new ABOM source set up for a download (the download itself is External). The native plot exports are
    /// TimeSeriesDataTests'. See docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughAbomDownloadTests
    {
        private const string Folder = "1-time-series-data/4-abom-download/";
        private const string Precipitation = "ABOM - 410730 - Daily Precipitation";
        private const string Cotter = "ABOM - 410730 - Daily Discharge";
        private static readonly WalkthroughSession Session = new WalkthroughSession("abom-download-example");

        private static readonly Record[] Records =
        {
            new(Precipitation, "Precipitation (mm)", "2003-01-31", "2026-07-15", 8567, 56),
            new(Cotter, "Flow (cms)", "1963-07-03", "2026-07-15", 23024, 0),
            new("ABOM - 410761 - Daily Discharge", "Flow (cms)", "1974-11-13", "2026-07-15", 18873, 98),
            new("ABOM - 409202 - Daily Stage", "Stage (m)", "1974-12-10", "2026-07-15", 18846, 39),
            new("ABOM - 410730 - Instantaneous Discharge", "Flow (cms)", "1963-07-03", "2026-07-15", 222246, 1),
            new("ABOM - 409202 - Instantaneous Stage", "Stage (m)", "1974-12-10", "2026-07-15", 669279, 46),
        };

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>
        /// "Find the controls": the explorer (the walkthrough's capture: the six records, without the removed manual test
        /// element), and the precipitation's properties (the capture of the panel): Data Entry Method = ABOM, Data Type =
        /// Daily Precipitation, Site Number = 410730, Depth Unit = Millimeters; the document's tabs.
        /// </summary>
        [TestMethod]
        public void FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                AssertExplorer(session, Folder + "screenshots/abom-project-explorer-current.json.gz");
                Assert.AreEqual(6, session.Project.ElementCollections.OfType<TimeSeriesCollection>().Single().Count, "the six records, and no manual test element");
                var view = session.OpenDocument<TimeSeriesControl>(Precipitation);
                Control properties = session.Properties;
                AssertPanel(properties, Folder + "screenshots/abom-properties-panel-current.json.gz");
                Assert.AreEqual("ABOM", Property(properties, "Data Entry Method"));
                Assert.AreEqual("Daily Precipitation", Property(properties, "Data Type"));
                Assert.AreEqual("410730", Property(properties, "Site Number"));
                Assert.AreEqual("Millimeters", Property(properties, "Depth Unit"));
                AssertTabs(view);
                session.AssertUnchanged(Precipitation);
            });
        }

        /// <summary>The inventory's daily records: units, first and last dates, ordinates and missing values.</summary>
        [TestMethod]
        public void SavedRecords_AreTheInventorys()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (Record record in Records.Where(r => r.Ordinates <= LargeRecord))
                    AssertRecord(session, record);
                session.AssertUnchanged(Records.Where(r => r.Ordinates <= LargeRecord).Select(r => r.Element).ToArray());
            });
        }

        /// <summary>
        /// The inventory's instantaneous records (222,246 and 669,279 ordinates), whose views take most of a gigabyte; step 5:
        /// their timestamps are irregular.
        /// </summary>
        [TestMethod]
        [TestCategory("Large")]
        public void LargestRecords_AreTheInventorys()
        {
            RequireLarge();
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (Record record in Records.Where(r => r.Ordinates > LargeRecord))
                {
                    AssertRecord(session, record);
                    var view = session.OpenDocument<TimeSeriesControl>(record.Element);
                    var grid = Table(view);
                    Show(view, "Time Series");
                    var steps = Enumerable.Range(0, 50).Select(i => TableDate(grid, i + 1) - TableDate(grid, i)).Distinct().ToList();
                    Assert.IsTrue(steps.Count > 1, record.Element + ": irregular sampling, steps " + string.Join(", ", steps));
                    session.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Steps 1-4: the precipitation is in millimetres; the Cotter discharge in Flow (cms), with its seasonality by month;
        /// the Murrumbidgee's 98 and the Murray stage's 39 missing values against the Cotter's none; the stage in metres.
        /// </summary>
        [TestMethod]
        public void WorkThroughTheExample_UnitsSeasonalityAndMissingValues()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var cotter = session.OpenDocument<TimeSeriesControl>(Cotter);
                Assert.AreEqual("Flow (cms)", Property(session.Properties, "Unit Label"));
                Assert.AreEqual("0", SummaryStatistics(cotter)["Missing Values"]);
                AssertSeasonality(cotter);
                session.CloseDocuments();

                foreach (var (name, unit, missing) in new[] { ("ABOM - 410761 - Daily Discharge", "Flow (cms)", "98"), ("ABOM - 409202 - Daily Stage", "Stage (m)", "39") })
                {
                    var view = session.OpenDocument<TimeSeriesControl>(name);
                    Assert.AreEqual(unit, view.TimeSeriesPlot.Axes.Single(a => a.Key == "Yaxis").Title, name + "'s value axis");
                    Assert.AreEqual(missing, SummaryStatistics(view)["Missing Values"], name);
                    session.CloseDocuments();
                }
                session.AssertUnchanged(Cotter, "ABOM - 410761 - Daily Discharge", "ABOM - 409202 - Daily Stage");
            });
        }

        /// <summary>The walkthrough's figures (figures/*.plotspec.json.gz) are what the views show.</summary>
        [TestMethod]
        public void Figures_AreWhatTheViewsShow()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                CollectionAssert.AreEqual(new[]
                {
                    "abom-download-example-cotter-daily-flow.plotspec.json.gz", "abom-download-example-cotter-seasonality.plotspec.json.gz",
                    "abom-download-example-murray-stage.plotspec.json.gz", "abom-download-example-precipitation.plotspec.json.gz",
                }, AssertFigures(session, Folder));
            });
        }

        /// <summary>Step 6: a new ABOM source with the station identifier and a variable; its Download button is then ready.</summary>
        [TestMethod]
        public void NewSource_IsSetUpForADownload()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var (element, _) = NewDownload(session, "Cotter Rainfall", "ABOM", "410730", "Daily Precipitation", "Millimeters");
                Assert.AreEqual("410730", element.ABOMSiteNumber);
                session.Remove(element);
                session.AssertUnchanged();
            });
        }

        /// <summary>Step 6's Download, from the Bureau of Meteorology's live service: the record arrives.</summary>
        [TestMethod]
        [TestCategory("External")]
        public void NewSource_Downloads()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var (element, properties) = NewDownload(session, "Cotter Rainfall", "ABOM", "410730", "Daily Precipitation", "Millimeters");
                Download(session, properties);
                Assert.IsTrue(element.TimeSeries.Count >= 8567, "at least the saved days, " + element.TimeSeries.Count);
                session.Remove(element);
            });
        }
    }
}
