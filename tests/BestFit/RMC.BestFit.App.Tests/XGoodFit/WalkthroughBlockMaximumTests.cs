using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughInputDataTesting;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the annual maxima walkthrough (examples/2-input-data/1-block-maximum/usgs-block-max-example.md)
    /// followed through the main window: the explorer and properties, the daily source, the saved calendar- and water-year
    /// samples (80 maxima each, 16 different), their chronology and frequency, the figures, and the extraction repeated on a
    /// new input. InputDataViewTests compares the native exports of the inputs' plots; the daily source's export is here.
    /// See docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughBlockMaximumTests
    {
        private const string Folder = "2-input-data/1-block-maximum/";
        private const string Daily = "USGS - 01134500 - Daily Discharge";
        private const string Calendar = "USGS - 01134500 - Block Max - Calendar Year";
        private const string WaterYear = "USGS - 01134500 - Block Max - Water Year";
        private static readonly WalkthroughSession Session = new WalkthroughSession("usgs-block-max-example");

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>
        /// "Find the controls" and step 2: the explorer (the walkthrough's capture), and the calendar-year input's properties
        /// (the capture of the panel): Data Entry Method = Block Series, the Moose River source, Block Function = Maximum, Time
        /// Block = Calendar Year; Data Frame on the left, Exact Data above the grid, and the Chronology Plot and Frequency Plot
        /// selectors below the plot.
        /// </summary>
        [TestMethod]
        public void FindTheControls_TheExplorerPropertiesAndViewAreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                AssertExplorer(session, Folder + "screenshots/block-max-project-explorer-current.json.gz");
                var view = session.OpenDocument<InputDataControl>(Calendar);
                Control properties = session.Properties;
                Assert.IsInstanceOfType<InputDataPropertiesControl>(properties);
                AssertPanel(properties, Folder + "screenshots/block-max-properties-current.json.gz");
                Assert.AreEqual("Block Series", Property(properties, "Data Entry Method"));
                Assert.AreEqual(Daily, Property(properties, "Time Series"));
                Assert.AreEqual("Maximum", Property(properties, "Block Function"));
                Assert.AreEqual("Calendar Year", Property(properties, "Time Block"));
                Assert.AreEqual("Process", Property(properties, "None"), "the Process button follows the smoothing row");
                Assert.AreEqual("Data Frame", Headers(MainTabs(view)).First(), "Data Frame on the left");
                Assert.AreEqual(Avalonia.Controls.Dock.Left, MainTabs(view).TabStripPlacement);
                Assert.AreEqual("Exact Data", Headers(DataTabs(view)).First(), "Exact Data above the grid");
                Assert.AreEqual(Avalonia.Controls.Dock.Top, DataTabs(view).TabStripPlacement);
                Assert.AreSame(((InputData)session.Element<InputData>(Calendar)).ChronologyPlot, ShowPlot(view, "Chronology Plot"));
                Assert.AreSame(((InputData)session.Element<InputData>(Calendar)).FrequencyPlot, ShowPlot(view, "Frequency Plot"));
                ShowPlot(view, "Chronology Plot");
                session.AssertUnchanged(Calendar);
            });
        }

        /// <summary>
        /// "Open and inspect the source" and step 1: the daily series' 29,050 daily means from 1947-01-01 to 2026-07-14, in
        /// cfs, with no missing values; and its native export (screenshots/block-max-daily-discharge-native).
        /// </summary>
        [TestMethod]
        public void Source_IsTheDailyRecord()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                WalkthroughTimeSeriesTesting.AssertRecord(session, new WalkthroughTimeSeriesTesting.Record(Daily, "Flow (cfs)", "1947-01-01", "2026-07-14", 29050, 0));
                AssertNativeTimeSeries(session, Folder, "block-max-daily-discharge-native.json.gz");
                session.AssertUnchanged(Daily);
            });
        }

        /// <summary>
        /// "Saved configuration and sample" and steps 3-4: each input has 80 maxima indexed 1947 to 2026 (the incomplete 2026
        /// block among them) and no low outliers, with the Multiple Grubbs-Beck Test on and a zero threshold; the water-year
        /// input's Time Block is Water Year. Both keep Start Month 10 and End Month 9: the calendar year's properties show
        /// neither, the water year's its Start Month (October), as upstream's panel does.
        /// </summary>
        /// <remarks>
        /// The walkthrough says the water-year blocks are "named by ending year" and that "an October event belongs to the
        /// following water year". RMC-BestFit groups the water years so, but indexes each maximum by its event's calendar
        /// year (DataFrame.CreateBlockSeries makes each ExactData from the event's date, and that constructor takes the
        /// date's Year): the seven maxima that fell in October to December (1959-11-29, 1973-12-22, 1990-12-24, 1996-12-03,
        /// 2003-10-28, 2005-10-17 and 2021-11-01) carry their water year's starting year, so the saved sample, and the one
        /// XGoodFit extracts again, repeats seven year indexes and lacks 1960, 1974, 1991, 1997, 2004, 2006 and 2022. The
        /// test checks what the application shows; see the coverage notes.
        /// </remarks>
        [TestMethod]
        public void SavedSamples_AreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (var (name, block) in new[] { (Calendar, "Calendar Year"), (WaterYear, "Water Year") })
                {
                    var view = session.OpenDocument<InputDataControl>(name);
                    var properties = (InputDataPropertiesControl)session.Properties;
                    Assert.AreEqual(block, Property(properties, "Time Block"), name);
                    Assert.AreEqual(block == "Water Year", properties.FindControl<Control>("StartMonth").IsVisible, name + ": the Start Month row");
                    Assert.IsFalse(properties.FindControl<Control>("EndMonth").IsVisible, name + ": no End Month row");
                    if (block == "Water Year") Assert.AreEqual("October", Property(properties, "Start Month"));
                    Assert.IsTrue(IsTicked(properties.FindControl<BooleanPropertyControl>("MGBTButton")), name + ": the low-outlier screening is on");
                    Assert.AreEqual("0", Property(properties, "Threshold Value"), name);
                    var element = session.Element<InputData>(name);
                    Assert.AreEqual((10, 9), (element.StartMonth, element.EndMonth), name + ": the stored months");

                    ValidationDataGrid grid = ExactData(view);
                    Assert.AreEqual(80, grid.Items.Count, name + ": the rows");
                    Dictionary<string, string> statistics = SummaryStatistics(view);
                    Assert.AreEqual("80", statistics["Record Length"], name);
                    Assert.AreEqual("0", statistics["Low Outliers"], name);
                    var chronology = Series(ShowPlot(view, "Chronology Plot"), "Exact Data");
                    Assert.AreEqual(80, chronology.Count, name + ": the maxima in the chronology");
                    Assert.AreEqual(1947, chronology.Min(p => p.X), name + ": the first year index");
                    Assert.AreEqual(2026, chronology.Max(p => p.X), name + ": the last year index");

                    // Each event's date (the grid) and its year index (the chronology): the event's calendar year, also for
                    // a water year's October-December maxima (the walkthrough expects the following year; see above).
                    grid = ExactData(view);
                    var events = Enumerable.Range(0, grid.Items.Count).Select(i => UsDate(CellText(grid, i, 0))).ToList();
                    var indexes = chronology.Select(p => (int)p.X).ToList();
                    for (int i = 0; i < events.Count; i++)
                        Assert.AreEqual(events[i].Year, indexes[i], $"{name}: the event of {events[i]:d}'s index");
                    var autumn = events.Where(e => e.Month >= 10).Select(e => e.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)).ToList();
                    if (block == "Water Year")
                    {
                        CollectionAssert.AreEqual(new[] { "1959-11-29", "1973-12-22", "1990-12-24", "1996-12-03", "2003-10-28", "2005-10-17", "2021-11-01" }, autumn,
                            "the water-year maxima of October to December");
                        Assert.AreEqual(73, indexes.Distinct().Count(), name + ": seven year indexes repeat");
                        CollectionAssert.AreEqual(new[] { 1960, 1974, 1991, 1997, 2004, 2006, 2022 }, Enumerable.Range(1947, 80).Except(indexes).ToList(), name + ": the indexes missing");
                    }
                    else
                    {
                        Assert.AreEqual(80, indexes.Distinct().Count(), name + ": one maximum a year");
                    }
                    session.CloseDocuments();
                }
                session.AssertUnchanged(Calendar, WaterYear);
            });
        }

        /// <summary>
        /// Step 5: sixteen of the 80 paired maxima differ between the calendar and water years, paired in order (as the
        /// grids list them) or by water year named by its ending year (from the event dates the grid shows).
        /// </summary>
        [TestMethod]
        public void TheTwoSamples_DifferIn16Years()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var samples = new[] { Calendar, WaterYear }.Select(name =>
                {
                    var view = session.OpenDocument<InputDataControl>(name);
                    var grid = ExactData(view);
                    var rows = Enumerable.Range(0, grid.Items.Count).Select(i => (Date: UsDate(CellText(grid, i, 0)), Value: UsNumber(CellText(grid, i, 1)))).ToList();
                    session.CloseDocuments();
                    return rows;
                }).ToList();
                Assert.AreEqual(80, samples[0].Count);
                Assert.AreEqual(80, samples[1].Count);
                Assert.AreEqual(16, samples[0].Zip(samples[1]).Count(p => p.First.Value != p.Second.Value), "the paired magnitudes that differ, in order");
                var waterYears = samples[1].ToDictionary(r => r.Date.Month >= 10 ? r.Date.Year + 1 : r.Date.Year, r => r.Value);
                Assert.AreEqual(80, waterYears.Count, "a maximum for each water year");
                Assert.AreEqual(16, samples[0].Count(r => waterYears[r.Date.Year] != r.Value), "the magnitudes that differ, by year");
                session.AssertUnchanged(Calendar, WaterYear);
            });
        }

        /// <summary>Step 6 and "Read the plots": the walkthrough's figures (images/*.plotspec.json.gz) are what the views show.</summary>
        [TestMethod]
        public void Figures_AreWhatTheViewsShow()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                CollectionAssert.AreEqual(new[]
                {
                    "usgs-block-max-example-calendar-chronology.plotspec.json.gz", "usgs-block-max-example-calendar-frequency.plotspec.json.gz",
                    "usgs-block-max-example-water-year-chronology.plotspec.json.gz",
                }, AssertFigures(session, Folder, "usgs-block-max-example-"));
                session.AssertUnchanged(Calendar, WaterYear);
            });
        }

        /// <summary>
        /// Step 7: a new Input Data element (New Input Data...) with Data Entry Method = Block Series, the daily source, the
        /// maximum and the water year: Process extracts the saved water-year sample.
        /// </summary>
        [TestMethod]
        public void RepeatTheExtraction_OnANewInput_GivesTheSavedSample()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                InputData element = NewInputData(session, "Water Year Maxima");
                var properties = (InputDataPropertiesControl)session.Properties;
                Choose(properties.FindControl<ContentPropertyControl>("ExactDateMethod"), "Block Series");
                Choose(properties.FindControl<ContentPropertyControl>("TimeSeriesElement"), Daily);
                Choose(properties.FindControl<ContentPropertyControl>("BlockFunction"), "Maximum");
                Choose(properties.FindControl<ContentPropertyControl>("TimeBlock"), "Water Year");
                Assert.AreEqual("Process", properties.FindControl<TextBlock>("ProcessDataButtonText").Text);
                Process(properties);
                Assert.AreEqual(0, session.Dialogs.Count, "no message: " + string.Join(" | ", session.Dialogs));

                var saved = session.Element<InputData>(WaterYear);
                var view = (InputDataControl)session.Documents().Single(d => d.IsActive).Content;
                Assert.AreEqual("80", SummaryStatistics(view)["Record Length"]);
                CollectionAssert.AreEqual(saved.DataFrame.ExactSeries.Select(d => (d.Index, d.Value)).ToList(),
                    element.DataFrame.ExactSeries.Select(d => (d.Index, d.Value)).ToList(), "the saved water-year maxima");
                session.Remove(element);
                session.AssertUnchanged(Calendar, WaterYear, Daily);
            });
        }
    }
}
