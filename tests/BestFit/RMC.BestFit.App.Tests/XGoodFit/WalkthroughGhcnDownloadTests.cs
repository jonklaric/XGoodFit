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
    /// XGoodFit addition: the GHCN daily climate records walkthrough
    /// (examples/1-time-series-data/2-ghcn-download/ghcn-download-example.md) followed through the main window: the explorer
    /// and properties, the two records' inventory, the steps (zeros and missing values, the snowfall's source-unit finding),
    /// the figures, and a new GHCN source set up for a download (the download itself is External). The native plot exports
    /// are TimeSeriesDataTests'. See docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughGhcnDownloadTests
    {
        private const string Folder = "1-time-series-data/2-ghcn-download/";
        private const string Precipitation = "GHCN - USC00040741 - Daily Precipitation";
        private const string Snow = "GHCN - USC00046685 - Daily Snow";
        private static readonly WalkthroughSession Session = new WalkthroughSession("ghcn-download-example");

        private static readonly Record[] Records =
        {
            new(Precipitation, "Precipitation (in)", "1960-07-01", "2026-06-30", 24106, 482),
            new(Snow, "Snow (in)", "1957-05-01", "2022-05-31", 23772, 2197),
        };

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>
        /// "Find the controls" and step 1: the explorer (the walkthrough's capture), and the precipitation's properties (the
        /// capture of the panel): Data Entry Method = GHCN, Data Type = Daily Precipitation, Site Number = USC00040741,
        /// Depth Unit = Inches; the document's tabs.
        /// </summary>
        [TestMethod]
        public void FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                AssertExplorer(session, Folder + "screenshots/ghcn-project-explorer-current.json.gz");
                var view = session.OpenDocument<TimeSeriesControl>(Precipitation);
                Control properties = session.Properties;
                AssertPanel(properties, Folder + "screenshots/ghcn-properties-panel-current.json.gz");
                Assert.AreEqual("GHCN", Property(properties, "Data Entry Method"));
                Assert.AreEqual("Daily Precipitation", Property(properties, "Data Type"));
                Assert.AreEqual("USC00040741", Property(properties, "Site Number"));
                Assert.AreEqual("Inches", Property(properties, "Depth Unit"));
                AssertTabs(view);
                session.AssertUnchanged(Precipitation);
            });
        }

        /// <summary>The inventory: units, first and last dates, ordinates and missing values (482 and 2,197).</summary>
        [TestMethod]
        public void SavedRecords_AreTheInventorys()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (Record record in Records)
                    AssertRecord(session, record);
                session.AssertUnchanged(Precipitation, Snow);
            });
        }

        /// <summary>
        /// Step 2: a zero is a stored zero and a missing value is unavailable, and the table keeps them apart (0 and N/A);
        /// the seasonality groups the days by month. Step 3 and the source-unit finding: the snowfall's 38 nonzero values are
        /// NOAA's millimetres divided by 254, so 445 mm on 29 January 1975 shows as 1.752 inches where 17.52 is right.
        /// </summary>
        [TestMethod]
        public void WorkThroughTheExample_ZerosMissingValuesAndTheSnowfallsUnits()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var element = session.Element<TimeSeriesElement>(Precipitation);
                var view = session.OpenDocument<TimeSeriesControl>(element);
                Show(view, "Time Series");
                var grid = Table(view);
                int zero = Enumerable.Range(0, element.TimeSeries.Count).First(i => element.TimeSeries[i].Value == 0);
                int missing = Enumerable.Range(0, element.TimeSeries.Count).First(i => double.IsNaN(element.TimeSeries[i].Value));
                Assert.AreEqual("0", CellText(grid, zero, 2), "a stored zero");
                Assert.AreEqual("N/A", CellText(grid, missing, 2), "a missing value");
                AssertSeasonality(view);
                session.CloseDocuments();

                var snow = session.OpenDocument<TimeSeriesControl>(Snow);
                Show(snow, "Time Series");
                var line = snow.TimeSeriesPlot.Series.Single();
                var nonzero = Points(line.InternalSeries, "ActualPoints").Where(p => double.IsFinite(p.Y) && p.Y != 0).ToList();
                Assert.AreEqual(38, nonzero.Count, "the nonzero snowfalls the plot shows");
                var snowGrid = Table(snow);
                var snowElement = session.Element<TimeSeriesElement>(Snow);
                int day = Enumerable.Range(0, snowElement.TimeSeries.Count).Single(i => snowElement.TimeSeries[i].Index.Date == new DateTime(1975, 1, 29));
                Assert.AreEqual(new DateTime(1975, 1, 29), TableDate(snowGrid, day).Date);
                double shown = UsNumber(CellText(snowGrid, day, 2));
                Assert.AreEqual(1.752, Math.Round(shown, 3), "the saved inches, " + shown);
                Assert.AreEqual(445 / 254.0, shown, 1E-9, "NOAA's 445 mm divided by 254");
                Assert.AreEqual(17.52, Math.Round(445 / 25.4, 2), "the source amount in inches");
                Assert.AreEqual(17.52, Math.Round(shown * 10, 2), "ten times the saved value");
                session.CloseDocuments();
                session.AssertUnchanged(Precipitation, Snow);
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
                    "ghcn-download-example-precipitation-seasonality.plotspec.json.gz", "ghcn-download-example-precipitation.plotspec.json.gz",
                    "ghcn-download-example-snowfall-source-check.plotspec.json.gz",
                }, AssertFigures(session, Folder));
            });
        }

        /// <summary>
        /// Step 4: a new download is a new time series with the 11-character station identifier, the variable and the depth
        /// unit; its Download button is then ready.
        /// </summary>
        [TestMethod]
        public void NewSource_IsSetUpForADownload()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var (element, _) = NewDownload(session, "Big Bear Lake Precipitation", "GHCN", "USC00040741", "Daily Precipitation", "Inches");
                Assert.AreEqual(11, element.GHCNSiteNumber.Length, "an 11-character station identifier");
                session.Remove(element);
                session.AssertUnchanged();
            });
        }

        /// <summary>Step 4's Download, from NOAA's live service: the record arrives, from 1960 on.</summary>
        [TestMethod]
        [TestCategory("External")]
        public void NewSource_Downloads()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var (element, properties) = NewDownload(session, "Big Bear Lake Precipitation", "GHCN", "USC00040741", "Daily Precipitation", "Inches");
                Download(session, properties);
                Assert.IsTrue(element.TimeSeries.Count >= 24106, "at least the saved days, " + element.TimeSeries.Count);
                Assert.AreEqual(new DateTime(1960, 7, 1), element.TimeSeries[0].Index.Date, "the record starts as the saved one");
                session.Remove(element);
            });
        }
    }
}
