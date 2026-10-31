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
    /// XGoodFit addition: the USGS annual peaks walkthrough
    /// (examples/2-input-data/2-usgs-peak-discharge/usgs-peak-download-example.md) followed through the main window: the
    /// explorer and properties, the two saved peak records (79 and 94 rows; 0 and 47 low-outlier flags at 0 and 1,270 cfs),
    /// their chronology and frequency, the figures, and a new peak download set up (the download itself is External).
    /// InputDataViewTests compares the native exports. See docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughUsgsPeakDischargeTests
    {
        private const string Folder = "2-input-data/2-usgs-peak-discharge/";
        private const string Moose = "USGS - 01134500 - Peak Discharge";
        private const string Orestimba = "USGS - 11274500 - Peak Discharge";
        private static readonly WalkthroughSession Session = new WalkthroughSession("usgs-peak-download-example");

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>
        /// "Find the controls" and steps 1-2: the explorer (the walkthrough's capture: two direct peak inputs and no time
        /// series), and the Moose River input's properties (the capture of the panel): Data Entry Method = USGS Peak
        /// Discharge, Site Number = 01134500, the Multiple Grubbs-Beck Test on; the generic Value label on its plots.
        /// </summary>
        [TestMethod]
        public void FindTheControls_TheExplorerAndPropertiesAreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                AssertExplorer(session, Folder + "screenshots/peak-download-project-explorer-current.json.gz");
                Assert.AreEqual(0, session.Project.ElementCollections.OfType<TimeSeriesCollection>().Single().Count, "no daily time series");
                Assert.AreEqual(2, session.Project.ElementCollections.OfType<InputDataCollection>().Single().Count, "two peak inputs");
                var view = session.OpenDocument<InputDataControl>(Moose);
                var properties = (InputDataPropertiesControl)session.Properties;
                AssertPanel(properties, Folder + "screenshots/peak-download-properties-current.json.gz");
                Assert.AreEqual("USGS Peak Discharge", Property(properties, "Data Entry Method"));
                Assert.AreEqual("01134500", Property(properties, "Site Number"));
                Assert.IsTrue(IsTicked(properties.FindControl<BooleanPropertyControl>("MGBTButton")), "the Multiple Grubbs-Beck Test is on");
                Assert.AreEqual("Download", properties.FindControl<TextBlock>("ProcessDataButtonText").Text);
                Assert.AreEqual("Value", Property(properties, "Unit Label"), "the generic label, for cfs");
                foreach (string plot in new[] { "Chronology Plot", "Frequency Plot" })
                    Assert.AreEqual("Value", ShowPlot(view, plot).Axes.Single(a => a.Key == "Yaxis").Title, plot + "'s value axis");
                session.AssertUnchanged(Moose);
            });
        }

        /// <summary>
        /// "Saved configuration and sample" and steps 3-5: Moose River's 79 rows indexed 1947-2025 with no flags and a zero
        /// threshold; Orestimba Creek's 94 rows indexed 1932-2025, 47 of them low-outlier flags, at a 1,270 cfs screening
        /// threshold, all kept in the exact data (the Threshold Data grid is empty). Thirteen of the flagged years are zeros,
        /// which the log-magnitude frequency plot can't show.
        /// </summary>
        [TestMethod]
        public void SavedSamples_AreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (var (name, rows, first, flagged, threshold) in new[] { (Moose, 79, 1947, 0, "0"), (Orestimba, 94, 1932, 47, "1270") })
                {
                    var view = session.OpenDocument<InputDataControl>(name);
                    var properties = (InputDataPropertiesControl)session.Properties;
                    Assert.AreEqual(threshold, Property(properties, "Threshold Value"), name + ": the saved screening threshold");
                    Assert.IsTrue(IsTicked(properties.FindControl<BooleanPropertyControl>("MGBTButton")), name);
                    Assert.AreEqual(rows, ExactData(view).Items.Count, name + ": the exact-series rows");
                    Dictionary<string, string> statistics = SummaryStatistics(view);
                    Assert.AreEqual(rows.ToString(), statistics["Record Length"], name);
                    Assert.AreEqual(flagged.ToString(), statistics["Low Outliers"], name);
                    Assert.AreEqual(0, ThresholdRows(view), name + ": no threshold series");
                    var plot = ShowPlot(view, "Chronology Plot");
                    var indexes = plot.Series.SelectMany(s => Points(s.InternalSeries, "ActualPoints")).Select(p => p.X).ToList();
                    Assert.AreEqual(rows, indexes.Count, name + ": every row is in the chronology");
                    Assert.AreEqual(first, indexes.Min(), name + ": the first year index");
                    Assert.AreEqual(2025, indexes.Max(), name + ": the last year index");
                    if (flagged > 0)
                    {
                        var low = Series(plot, "Low Outlier Data");
                        Assert.AreEqual(flagged, low.Count, name + ": the flagged years, shown apart");
                        Assert.AreEqual(13, low.Count(p => p.Y == 0), name + ": the flagged zeros");
                        Assert.AreEqual(flagged - 13, Series(ShowPlot(view, "Frequency Plot"), "Low Outlier Data").Count, name + ": the zeros aren't on the log axis");
                    }
                    session.CloseDocuments();
                }
                session.AssertUnchanged(Moose, Orestimba);
            });
        }

        /// <summary>The walkthrough's figures (images/*.plotspec.json.gz) are what the views show.</summary>
        [TestMethod]
        public void Figures_AreWhatTheViewsShow()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                CollectionAssert.AreEqual(new[]
                {
                    "usgs-peak-download-example-moose-chronology.plotspec.json.gz", "usgs-peak-download-example-moose-frequency.plotspec.json.gz",
                    "usgs-peak-download-example-orestimba-chronology.plotspec.json.gz", "usgs-peak-download-example-orestimba-frequency.plotspec.json.gz",
                }, AssertFigures(session, Folder, "usgs-peak-download-example-"));
                session.AssertUnchanged(Moose, Orestimba);
            });
        }

        /// <summary>
        /// Step 7: a new Input Data element (New Input Data...) with Data Entry Method = USGS Peak Discharge and the station
        /// number; its Download button is then ready.
        /// </summary>
        [TestMethod]
        public void NewDownload_IsSetUp()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var (element, _) = NewPeakDownload(session);
                session.Remove(element);
                session.AssertUnchanged(Moose, Orestimba);
            });
        }

        /// <summary>Step 7's Download, from USGS's live service: the peaks arrive, at least the saved 79.</summary>
        [TestMethod]
        [TestCategory("External")]
        public void NewDownload_DownloadsThePeaks()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var (element, properties) = NewPeakDownload(session);
                Process(properties);
                Assert.AreEqual(0, session.Dialogs.Count, "no message: " + string.Join(" | ", session.Dialogs));
                Assert.IsTrue(element.DataFrame.ExactSeries.Count >= 79, "at least the saved peaks, " + element.DataFrame.ExactSeries.Count);
                session.Remove(element);
            });
        }

        private static (InputData Element, InputDataPropertiesControl Properties) NewPeakDownload(WalkthroughSession session)
        {
            InputData element = NewInputData(session, "Moose River Peaks");
            var properties = (InputDataPropertiesControl)session.Properties;
            Choose(properties.FindControl<ContentPropertyControl>("ExactDateMethod"), "USGS Peak Discharge");
            WalkthroughTimeSeriesTesting.Type(properties.FindControl<Control>("USGSSiteNumber"), "01134500");
            Assert.AreEqual("USGS Peak Discharge", Property(properties, "Data Entry Method"));
            Assert.AreEqual("01134500", Property(properties, "Site Number"));
            Assert.AreEqual("Download", properties.FindControl<TextBlock>("ProcessDataButtonText").Text);
            Assert.IsTrue(properties.FindControl<Button>("ProcessDataButton").IsEffectivelyEnabled, "Download is ready");
            return (element, properties);
        }
    }
}
