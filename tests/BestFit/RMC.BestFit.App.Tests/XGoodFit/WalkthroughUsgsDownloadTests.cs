using System.Collections.Generic;
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
    /// XGoodFit addition: the USGS records walkthrough (examples/1-time-series-data/1-usgs-download/usgs-download-example.md)
    /// followed through the main window: the explorer and properties it shows, its inventory of the eight saved records, its
    /// steps, its figures, and a new USGS source set up for a download (the download itself is External). The native plot
    /// exports are TimeSeriesDataTests' (Plots_DrawTheStoredSeries_AsRmcBestFitDid, and the Large test for the instantaneous
    /// records). See docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughUsgsDownloadTests
    {
        private const string Folder = "1-time-series-data/1-usgs-download/";
        private const string Daily = "USGS - 01134500 - Daily Discharge";
        private static readonly WalkthroughSession Session = new WalkthroughSession("usgs-download-example");

        /// <summary>The walkthrough's inventory ("Source and saved records").</summary>
        private static readonly Record[] Records =
        {
            new(Daily, "Flow (cfs)", "1947-01-01", "2026-07-14", 29050, 0),
            new("USGS - 07010000 - Daily Stage", "Value", "1982-10-01", "2026-07-15", 15994, 197),
            new("USGS - 01646500 - Instantaneous Discharge", "Flow (cfs)", "1972-06-09", "2026-07-15", 1272826, 0),
            new("USGS - 01646500 - Instantaneous Stage", "Stage (ft)", "2007-10-01", "2026-07-15", 723856, 0),
            new("USGS - 01570500 - Measured Discharge", "Flow (cfs)", "1897-03-31", "2026-07-15", 412, 0),
            new("USGS - 01570500 - Measured Stage", "Stage (ft)", "1897-03-31", "2026-07-15", 939, 0),
            new("USGS - 01614000 - Peak Discharge", "Flow (cfs)", "1929-04-17", "2025-05-14", 69, 0),
            new("USGS - 01614000 - Peak Stage", "Stage (ft)", "1929-04-17", "2025-05-14", 69, 0),
        };

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>
        /// "Find the controls": the explorer lists the eight records (the walkthrough's capture of it), and the daily
        /// discharge's properties show its source (Data Entry Method = USGS, Data Type = Daily Discharge, Site Number =
        /// 01134500; the capture of the panel); its document has the Time Series tab, with the Time Series Data table and
        /// Summary Statistics beside it, and the Seasonality, ACF and PACF plots' tabs on the left.
        /// </summary>
        [TestMethod]
        public void FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                AssertExplorer(session, Folder + "screenshots/usgs-project-explorer-current.json.gz");
                var view = session.OpenDocument<TimeSeriesControl>(Daily);
                Control properties = session.Properties;
                Assert.IsInstanceOfType<TimeSeriesPropertiesControl>(properties);
                AssertPanel(properties, Folder + "screenshots/usgs-properties-panel-current.json.gz");
                Assert.AreEqual("USGS", Property(properties, "Data Entry Method"));
                Assert.AreEqual("Daily Discharge", Property(properties, "Data Type"));
                Assert.AreEqual("01134500", Property(properties, "Site Number"));
                AssertTabs(view);
                session.AssertUnchanged(Daily);
            });
        }

        /// <summary>The inventory's records of up to 200,000 ordinates: units, first and last dates, ordinates and missing values.</summary>
        [TestMethod]
        public void SavedRecords_AreTheInventorys()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (Record record in Records.Where(r => r.Ordinates <= LargeRecord))
                    AssertRecord(session, record);
            });
        }

        /// <summary>The inventory's instantaneous records (723,856 and 1,272,826 ordinates), whose views take over a gigabyte.</summary>
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
                    // Step 5: the instantaneous records resolve times within a day.
                    var view = session.OpenDocument<TimeSeriesControl>(record.Element);
                    var grid = Table(view);
                    Show(view, "Time Series");
                    var times = Enumerable.Range(0, 3).Select(i => TableDate(grid, i)).ToList();
                    Assert.IsTrue(times[1] - times[0] < System.TimeSpan.FromDays(1), record.Element + ": timestamps within a day, " + string.Join(", ", times));
                    session.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// "Work through the example", steps 1-4: the daily discharge's date and value columns and its plot (daily means in
        /// cfs); its seasonality by month; the peak record's 69 events from 1929 to 2025, with missing years; the measured
        /// discharge and stage, whose counts differ (412 and 939). And the limits: the St. Louis stage's generic Value label
        /// (its saved height unit is feet, a setting the properties don't show) and its 197 missing values.
        /// </summary>
        [TestMethod]
        public void WorkThroughTheExample_TheRecordsAreWhatTheStepsSay()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();

                // Step 1: daily mean discharges, in cfs, in the table and the chronological plot.
                var daily = session.OpenDocument<TimeSeriesControl>(Daily);
                Show(daily, "Time Series");
                Assert.AreEqual("Flow (cfs)", daily.TimeSeriesPlot.Axes.Single(a => a.Key == "Yaxis").Title);
                Assert.AreEqual("Date", daily.TimeSeriesPlot.Axes.Single(a => a.Key == "Xaxis").Title);
                var grid = Table(daily);
                Assert.AreEqual(1, (TableDate(grid, 1) - TableDate(grid, 0)).TotalDays, "a value a day");

                // Step 2: the seasonality groups the observations by month.
                AssertSeasonality(daily);
                session.CloseDocuments();

                // Step 3: 69 annual peaks from 1929 to 2025: an event record with missing years.
                var peaks = session.OpenDocument<TimeSeriesControl>("USGS - 01614000 - Peak Discharge");
                Show(peaks, "Time Series");
                var peakGrid = Table(peaks);
                Assert.AreEqual(69, peakGrid.Items.Count);
                var years = Enumerable.Range(0, peakGrid.Items.Count).Select(i => TableDate(peakGrid, i).Year).ToList();
                Assert.AreEqual(1929, years.First());
                Assert.AreEqual(2025, years.Last());
                Assert.IsTrue(years.Distinct().Count() < 2025 - 1929 + 1, "years are missing from the record");
                AssertTabs(peaks, usgsText: true);
                session.CloseDocuments();

                // Step 4: the measured discharge and stage have different counts, so their rows don't pair by position.
                var counts = new List<string>();
                foreach (string name in new[] { "USGS - 01570500 - Measured Discharge", "USGS - 01570500 - Measured Stage" })
                {
                    var view = session.OpenDocument<TimeSeriesControl>(name);
                    counts.Add(SummaryStatistics(view)["Record Length"]);
                    session.CloseDocuments();
                }
                CollectionAssert.AreEqual(new[] { "412", "939" }, counts);

                // The limits: St. Louis's stage keeps the generic label Value, and its height unit is feet.
                var stage = session.OpenDocument<TimeSeriesControl>("USGS - 07010000 - Daily Stage");
                Assert.AreEqual("Value", Property(session.Properties, "Unit Label"));
                Assert.AreEqual("Value", stage.TimeSeriesPlot.Axes.Single(a => a.Key == "Yaxis").Title, "the saved axis label");
                Assert.AreEqual("197", SummaryStatistics(stage)["Missing Values"]);
                Assert.AreEqual(Numerics.Data.TimeSeriesDownload.HeightUnit.Feet, session.Element<TimeSeriesElement>("USGS - 07010000 - Daily Stage").HeightUnit,
                    "the saved height unit, which RMC-BestFit's properties don't show");
                session.CloseDocuments();
                session.AssertUnchanged(Records.Where(r => r.Ordinates <= LargeRecord).Select(r => r.Element).ToArray());
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
                    "usgs-download-example-annual-peaks.plotspec.json.gz", "usgs-download-example-daily-flow.plotspec.json.gz",
                    "usgs-download-example-field-measurements.plotspec.json.gz", "usgs-download-example-seasonality.plotspec.json.gz",
                }, AssertFigures(session, Folder));
            });
        }

        /// <summary>
        /// Step 6 and "Find the controls": a new source in a working copy is a new time series (Time Series Data's New Time
        /// Series...) with Data Entry Method = USGS, its site number and series type; its Download button is then ready.
        /// </summary>
        [TestMethod]
        public void NewSource_IsSetUpForADownload()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var (element, _) = NewDownload(session, "Back Creek Peaks", "USGS", "01614000", "Peak Discharge");
                Assert.AreEqual(TimeSeriesElement.TimeSeriesEntryMethod.USGS, element.EntryMethod);
                Assert.AreEqual("01614000", element.USGSSiteNumber);
                session.Remove(element);
                session.AssertUnchanged();
            });
        }

        /// <summary>Step 6's Download, from USGS's live service: the record arrives, at least the saved 69 peaks.</summary>
        [TestMethod]
        [TestCategory("External")]
        public void NewSource_Downloads()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var (element, properties) = NewDownload(session, "Back Creek Peaks", "USGS", "01614000", "Peak Discharge");
                Download(session, properties);
                Assert.IsTrue(element.TimeSeries.Count >= 69, "at least the saved peaks, " + element.TimeSeries.Count);
                var view = session.OpenDocument<TimeSeriesControl>(element);
                Assert.AreEqual(element.TimeSeries.Count.ToString("N0", Us), SummaryStatistics(view)["Record Length"]);
                session.Remove(element);
            });
        }

    }
}
