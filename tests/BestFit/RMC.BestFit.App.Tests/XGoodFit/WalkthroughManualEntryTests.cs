using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    /// XGoodFit addition: the manual time-series entry walkthrough
    /// (examples/1-time-series-data/6-manual-entry/manual-entry-example.md) followed through the main window: the explorer and
    /// properties, the three records' inventory, the supplied CSV files against the saved records (the Nile's 26-year date
    /// shift), the views, a new manually entered series, and the figures. The native plot exports are TimeSeriesDataTests'.
    /// See docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughManualEntryTests
    {
        private const string Folder = "1-time-series-data/6-manual-entry/";
        private const string Airline = "Airline Passengers";
        private const string Nile = "Nile River Flows";
        private const string Co2 = "Mauna Loa CO2";
        private static readonly WalkthroughSession Session = new WalkthroughSession("manual-entry-example");

        private static readonly Record[] Records =
        {
            new(Airline, "Passengers (thousands)", "1949-01-01", "1960-12-01", 144, 0),
            new(Nile, "Flow (10⁸ m³)", "1897-01-01", "1996-01-01", 100, 0),
            new(Co2, "CO2 (ppm)", "1958-03-01", "2023-12-01", 790, 0),
        };

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>
        /// "Find the controls": the explorer (the walkthrough's capture), and the airline series' properties (the capture of
        /// the panel): Data Entry Method = Manual Entry, Time Interval = 1-Month and its Start Date; the document's tabs.
        /// </summary>
        [TestMethod]
        public void FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                AssertExplorer(session, Folder + "screenshots/manual-entry-project-explorer-current.json.gz");
                var view = session.OpenDocument<TimeSeriesControl>(Airline);
                Control properties = session.Properties;
                AssertPanel(properties, Folder + "screenshots/manual-entry-properties-current.json.gz");
                Assert.AreEqual("Manual Entry", Property(properties, "Data Entry Method"));
                Assert.AreEqual("1-Month", Property(properties, "Time Interval"));
                Assert.AreEqual(new DateTime(1949, 1, 1), UsDate(Property(properties, "Start Date")));
                AssertTabs(view);
                session.AssertUnchanged(Airline);
            });
        }

        /// <summary>The inventory: units, first and last dates, ordinates and missing values of the three records.</summary>
        [TestMethod]
        public void SavedRecords_AreTheInventorys()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (Record record in Records)
                    AssertRecord(session, record);
                session.AssertUnchanged(Airline, Nile, Co2);
            });
        }

        /// <summary>
        /// Steps 1, 3 and 4 against the supplied CSV files: the airline series' 144 monthly values (January 1949 to December
        /// 1960) and the CO2 series' 790 are the files' values on the files' dates; the Nile's 100 values are the file's, in
        /// order, but dated 1897-1996 where the file has 1871-1970 (the walkthrough's date discrepancy, 26 years). The series
        /// keep the generic DailyDischarge series type, which the manual entry's properties don't show.
        /// </summary>
        [TestMethod]
        public void SavedRecords_AreTheSuppliedFilesValues_AndTheNileIsShiftedBy26Years()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (var (name, file, years) in new[] { (Airline, "airline-passengers.csv", 0), (Co2, "mauna-loa-co2.csv", 0), (Nile, "nile-river-flow.csv", 26) })
                {
                    List<(DateTime Date, double Value)> csv = Csv(file);
                    var view = session.OpenDocument<TimeSeriesControl>(name);
                    Show(view, "Time Series");
                    var grid = Table(view);
                    Assert.AreEqual(csv.Count, grid.Items.Count, name + ": one row per line of " + file);
                    // The table's dates and values at the start, the end and through the record.
                    foreach (int row in new[] { 0, 1, 2, csv.Count / 2, csv.Count - 2, csv.Count - 1 })
                    {
                        Assert.AreEqual(csv[row].Date.AddYears(years), TableDate(grid, row), $"{name}, row {row}: the file's date{(years == 0 ? "" : " shifted by " + years + " years")}");
                        Assert.AreEqual(csv[row].Value, UsNumber(CellText(grid, row, 2)), $"{name}, row {row}: the file's value");
                    }
                    // Every value, as the plot shows the series.
                    CollectionAssert.AreEqual(csv.Select(r => r.Value).ToList(),
                        Points(view.TimeSeriesPlot.Series.Single().InternalSeries, "ActualPoints").Select(p => p.Y).ToList(), name + ": the file's values in order");
                    if (name == Airline)
                        Assert.AreEqual(1, (TableDate(grid, 1).Year - TableDate(grid, 0).Year) * 12 + TableDate(grid, 1).Month - TableDate(grid, 0).Month, "monthly");
                    session.CloseDocuments();
                    Assert.AreEqual(TimeSeriesDownload.TimeSeriesType.DailyDischarge, session.Element<TimeSeriesElement>(name).SeriesType, name + "'s stored series type");
                }
                Assert.AreEqual(1871, Csv("nile-river-flow.csv")[0].Date.Year);
                Assert.AreEqual(1970, Csv("nile-river-flow.csv")[^1].Date.Year);
                session.AssertUnchanged(Airline, Nile, Co2);
            });
        }

        /// <summary>
        /// Step 5: the Time Series, Seasonality Plot and ACF Plot views of the airline series: the seasonality by month, and
        /// the autocorrelations of the unchanged monthly series (the walkthrough's native ACF export is TimeSeriesDataTests').
        /// </summary>
        [TestMethod]
        public void Views_ShowTheTimeSeriesSeasonalityAndAcf()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var view = session.OpenDocument<TimeSeriesControl>(Airline);
                AssertSeasonality(view);
                Show(view, "ACF Plot");
                Assert.AreSame(view.ACFPlot, view.GetCurrentPlot());
                var bars = view.ACFPlot.Series.OfType<OxyPlot.Wpf.HistogramSeries>().Single();
                Assert.IsTrue(((System.Collections.IEnumerable)bars.ItemsSource).Cast<object>().Count() > 12, "the autocorrelations are drawn, lag by lag");
                session.CloseDocuments();
                session.AssertUnchanged(Airline);
            });
        }

        /// <summary>
        /// Step 2 and "Check your understanding": a new working element (New Time Series...) is entered by hand; with the
        /// interval (1-Month) and first date set before the paste, five pasted values make five rows, a month apart from the
        /// first date.
        /// </summary>
        [TestMethod]
        public void NewManualEntry_FiveMonthlyValues_GetTheirDates()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                TimeSeriesElement element = NewTimeSeries(session, "Five Months");
                var properties = (TimeSeriesPropertiesControl)session.Properties;
                Assert.AreEqual("Manual Entry", Property(properties, "Data Entry Method"));
                Choose(properties.TimeIntervalSelector, "1-Month");
                properties.StartDateTime.SelectedDateTime = new DateTime(1949, 1, 1);
                BestFitShell.Frame();
                Assert.AreEqual("1-Month", Property(properties, "Time Interval"));
                Assert.AreEqual(new DateTime(1949, 1, 1), UsDate(Property(properties, "Start Date")));

                var view = (TimeSeriesControl)session.Documents().Single(d => d.IsActive).Content;
                Show(view, "Time Series");
                var grid = Table(view);
                grid.SelectedCells = new[] { new DataGridCellInfo(grid.Items[0], grid.Columns[2], 0, 2, true) };
                grid.SelectedCells = null;
                WpfCompat.Clipboard.SetText("112\r\n118\r\n132\r\n129\r\n121\r\n");
                grid.PasteClipboard();
                BestFitShell.Frame();
                Assert.AreEqual(5, grid.Items.Count, "a row per pasted value");
                for (int row = 0; row < 5; row++)
                {
                    Assert.AreEqual(new DateTime(1949, 1, 1).AddMonths(row), TableDate(grid, row), "row " + row + "'s date");
                    Assert.AreEqual(new[] { 112d, 118, 132, 129, 121 }[row], UsNumber(CellText(grid, row, 2)), "row " + row + "'s value");
                }
                Assert.AreEqual("5", SummaryStatistics(view)["Record Length"]);
                session.Remove(element);
                session.AssertUnchanged(Airline, Nile, Co2);
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
                    "manual-entry-example-airline.plotspec.json.gz", "manual-entry-example-co2.plotspec.json.gz", "manual-entry-example-nile-stored-dates.plotspec.json.gz",
                }, AssertFigures(session, Folder));
            });
        }

        // A supplied CSV file's rows: an ISO date and a value.
        private static List<(DateTime Date, double Value)> Csv(string file) =>
            File.ReadAllLines(SharedControlsTesting.Example(Folder + file)).Skip(1).Where(l => l.Length > 0)
                .Select(l => l.Split(','))
                .Select(f => (DateTime.ParseExact(f[0], "yyyy-MM-dd", CultureInfo.InvariantCulture), double.Parse(f[1], CultureInfo.InvariantCulture)))
                .ToList();
    }
}
