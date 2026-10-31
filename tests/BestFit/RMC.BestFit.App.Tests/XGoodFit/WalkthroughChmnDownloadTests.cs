using System;
using System.Linq;
using Avalonia.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Numerics.Data;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTesting;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTimeSeriesTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Canadian hydrometric records walkthrough
    /// (examples/1-time-series-data/3-chmn-download/chmn-download-example.md) followed through the main window: the explorer
    /// and properties, the six records' inventory, the steps (units, the five-minute records, the peak counts, seasonality),
    /// the figures, and a new CHMN source set up for a download (the download itself is External). The native plot exports
    /// are TimeSeriesDataTests'. See docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughChmnDownloadTests
    {
        private const string Folder = "1-time-series-data/3-chmn-download/";
        private const string Daily = "CHMN - 08MG005 - Daily Discharge";
        private static readonly WalkthroughSession Session = new WalkthroughSession("chmn-download-example");

        private static readonly Record[] Records =
        {
            new(Daily, "Flow (cms)", "1914-01-01", "2025-02-20", 40594, 2656),
            new("CHMN - 08MG005 - Daily Stage", "Stage (m)", "2011-03-10", "2025-02-20", 5097, 159),
            new("CHMN - 08MG005 - Instantaneous Discharge", "Flow (cms)", "2025-01-15", "2026-07-15", 157213, 0),
            new("CHMN - 08MG005 - Instantaneous Stage", "Stage (m)", "2025-01-15", "2026-07-15", 157213, 0),
            new("CHMN - 08MG005 - Peak Discharge", "Flow (cms)", "1949-08-17", "2024-01-30", 64, 0),
            new("CHMN - 08MG005 - Peak Stage", "Stage (m)", "2012-07-17", "2024-01-30", 13, 0),
        };

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>
        /// "Find the controls": the explorer (the walkthrough's capture), and the daily discharge's properties (the capture
        /// of the panel): Data Entry Method = CHMN, Data Type = Daily Discharge, Site Number = 08MG005; the document's tabs.
        /// </summary>
        [TestMethod]
        public void FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                AssertExplorer(session, Folder + "screenshots/chmn-project-explorer-current.json.gz");
                var view = session.OpenDocument<TimeSeriesControl>(Daily);
                Control properties = session.Properties;
                AssertPanel(properties, Folder + "screenshots/chmn-properties-panel-current.json.gz");
                Assert.AreEqual("CHMN", Property(properties, "Data Entry Method"));
                Assert.AreEqual("Daily Discharge", Property(properties, "Data Type"));
                Assert.AreEqual("08MG005", Property(properties, "Site Number"));
                AssertTabs(view);
                session.AssertUnchanged(Daily);
            });
        }

        /// <summary>The inventory: units, first and last dates, ordinates and missing values of the six records.</summary>
        [TestMethod]
        public void SavedRecords_AreTheInventorys()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (Record record in Records)
                    AssertRecord(session, record);
                session.AssertUnchanged(Records.Select(r => r.Element).ToArray());
            });
        }

        /// <summary>
        /// Step 3: the instantaneous records' saved interval is five minutes, and their timestamps are five minutes apart, but
        /// they have gaps: fewer ordinates than a full five-minute grid from the first date to the last. Step 4: 64 discharge
        /// peaks but 13 stage peaks. Step 5: the daily discharge's seasonality by month.
        /// </summary>
        [TestMethod]
        public void WorkThroughTheExample_IntervalsPeaksAndSeasonality()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (string name in new[] { "CHMN - 08MG005 - Instantaneous Discharge", "CHMN - 08MG005 - Instantaneous Stage" })
                {
                    var element = session.Element<TimeSeriesElement>(name);
                    Assert.AreEqual(TimeInterval.FiveMinute, element.TimeSeries.TimeInterval, name + ": the saved interval, which the CHMN properties don't show");
                    var view = session.OpenDocument<TimeSeriesControl>(element);
                    var grid = Table(view);
                    (DateTime first, DateTime last) = Dates(view);
                    Assert.AreEqual(TimeSpan.FromMinutes(5), TableDate(grid, 1) - TableDate(grid, 0), name + ": five minutes apart");
                    double grid5 = (last - first).TotalMinutes / 5 + 1;
                    Assert.IsTrue(grid.Items.Count < grid5, $"{name}: {grid.Items.Count} ordinates, fewer than the {grid5} of a full five-minute grid");
                    session.CloseDocuments();
                }

                var counts = new[] { "CHMN - 08MG005 - Peak Discharge", "CHMN - 08MG005 - Peak Stage" }.Select(name =>
                {
                    var view = session.OpenDocument<TimeSeriesControl>(name);
                    string count = SummaryStatistics(view)["Record Length"];
                    session.CloseDocuments();
                    return count;
                }).ToList();
                CollectionAssert.AreEqual(new[] { "64", "13" }, counts);

                var daily = session.OpenDocument<TimeSeriesControl>(Daily);
                AssertSeasonality(daily);
                session.CloseDocuments();
                session.AssertUnchanged(Records.Select(r => r.Element).ToArray());
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
                    "chmn-download-example-annual-peaks.plotspec.json.gz", "chmn-download-example-daily-flow.plotspec.json.gz",
                    "chmn-download-example-seasonality.plotspec.json.gz",
                }, AssertFigures(session, Folder));
            });
        }

        /// <summary>Step 6: a new CHMN source for station 08MG005 and a series type; its Download button is then ready.</summary>
        [TestMethod]
        public void NewSource_IsSetUpForADownload()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var (element, _) = NewDownload(session, "Lillooet Peaks", "CHMN", "08MG005", "Peak Discharge");
                Assert.AreEqual("08MG005", element.CHMNSiteNumber);
                session.Remove(element);
                session.AssertUnchanged();
            });
        }

        /// <summary>Step 6's Download, from the Water Survey of Canada's live service: the peaks arrive.</summary>
        [TestMethod]
        [TestCategory("External")]
        public void NewSource_Downloads()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var (element, properties) = NewDownload(session, "Lillooet Peaks", "CHMN", "08MG005", "Peak Discharge");
                Download(session, properties);
                Assert.IsTrue(element.TimeSeries.Count >= 64, "at least the saved peaks, " + element.TimeSeries.Count);
                session.Remove(element);
            });
        }
    }
}
