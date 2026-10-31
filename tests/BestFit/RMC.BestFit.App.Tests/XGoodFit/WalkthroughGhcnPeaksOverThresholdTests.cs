using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Numerics.Data;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughInputDataTesting;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Big Bear precipitation POT walkthrough
    /// (examples/2-input-data/3-peaks-over-threshold/ghcn-peaks-over-threshold-example.md) followed through the main window:
    /// the explorer and properties, the precipitation source, the saved settings and sample (233 events, λ = 233 / 67), the
    /// chronology, frequency and POT diagnostics, and the figures. InputDataViewTests compares the native exports of the
    /// input's plots; the source's export is here. See docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughGhcnPeaksOverThresholdTests
    {
        private const string Folder = "2-input-data/3-peaks-over-threshold/";
        private const string Source = "GHCN-USC00040741-Precipitation";
        private const string Pot = "GHCN-USC00040741-POT";
        private static readonly WalkthroughSession Session = new WalkthroughSession("ghcn-peaks-over-threshold-example");

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>
        /// "Find the controls" and step 2: the explorer (the walkthrough's capture), and the input's properties (the capture
        /// of the panel): Data Entry Method = Peaks-Over-Threshold Series, Time Series = GHCN-USC00040741-Precipitation, the
        /// 1.0 inch Threshold, 5 Minimum Steps and Smoothing = None (its retained Period of 2 is inactive: no Period row). The
        /// saved name is GHCN's.
        /// </summary>
        [TestMethod]
        public void FindTheControls_TheExplorerAndPropertiesAreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                AssertExplorer(session, Folder + "screenshots/ghcn-pot-project-explorer-current.json.gz");
                session.OpenDocument<InputDataControl>(Pot);
                var properties = (InputDataPropertiesControl)session.Properties;
                AssertPanel(properties, Folder + "screenshots/ghcn-pot-properties-current.json.gz");
                Assert.AreEqual("Peaks-Over-Threshold Series", Property(properties, "Data Entry Method"));
                Assert.AreEqual(Source, Property(properties, "Time Series"));
                Assert.AreEqual(1.0, UsNumber(Property(properties, "Threshold")), "the threshold, in inches");
                Assert.AreEqual("5", Property(properties, "Minimum Steps"));
                Assert.AreEqual("None", Property(properties, "Smoothing"));
                Assert.IsFalse(properties.FindControl<Control>("Period").IsVisible, "no smoothing period while Smoothing = None");
                var element = session.Element<InputData>(Pot);
                Assert.AreEqual(2, element.Period, "the retained, inactive period");
                Assert.AreEqual(SmoothingFunctionType.None, element.SmoothingFunction);
                Assert.IsTrue(Pot.StartsWith("GHCN-"), "GHCN, not GHCH");
                session.AssertUnchanged(Pot);
            });
        }

        /// <summary>
        /// "Open and inspect the source" and step 1: 24,106 daily ordinates from 1960-07-01 to 2026-06-30 (partial boundary
        /// years), 482 of them missing, in inches; and its native export (screenshots/ghcn-pot-daily-precipitation-native).
        /// </summary>
        [TestMethod]
        public void Source_IsTheDailyPrecipitation()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                WalkthroughTimeSeriesTesting.AssertRecord(session, new WalkthroughTimeSeriesTesting.Record(Source, "depth (in)", "1960-07-01", "2026-06-30", 24106, 482));
                AssertNativeTimeSeries(session, Folder, "ghcn-pot-daily-precipitation-native.json.gz");
                session.AssertUnchanged(Source);
            });
        }

        /// <summary>
        /// "Saved configuration and sample" and steps 3-6: 233 event rows indexed 1960-2026, several in one year; the saved
        /// λ of 3.4776119403 events a year is 233 / 67, the stored exposure of 67 inclusive calendar years (which the input's
        /// views don't show); the POT Diagnostics offer Mean Residual Life, Modified Scale and Shape below the plot.
        /// </summary>
        [TestMethod]
        public void SavedSample_AndItsDiagnostics_AreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var view = session.OpenDocument<InputDataControl>(Pot);
                Assert.AreEqual(233, ExactData(view).Items.Count, "the event rows");
                Dictionary<string, string> statistics = SummaryStatistics(view);
                Assert.AreEqual("233", statistics["Record Length"]);
                AssertRounded(3.4776119403, statistics["Events Per Index (λ)"], 10, "the saved λ");
                var element = session.Element<InputData>(Pot);
                Assert.AreEqual(67, element.DataFrame.PointProcessObservationYears, "the stored exposure");
                Assert.AreEqual(233 / 67.0, element.DataFrame.Lambda, 1E-12, "233 / 67");
                Assert.AreEqual(3.4776119403, System.Math.Round(233 / 67.0, 10));

                var chronology = Series(ShowPlot(view, "Chronology Plot"), "Exact Data");
                Assert.AreEqual(233, chronology.Count);
                Assert.AreEqual(1960, chronology.Min(p => p.X));
                Assert.AreEqual(2026, chronology.Max(p => p.X));
                Assert.AreEqual(2026 - 1960 + 1, 67, "the inclusive span of the indexes");
                Assert.IsTrue(chronology.GroupBy(p => p.X).Any(g => g.Count() > 1), "several events in one year");

                Assert.IsTrue(Tab(MainTabs(view), "POT Diagnostics").IsVisible, "POT Diagnostics on the left");
                Assert.AreSame(element.MRLPlot, ShowPlot(view, "Mean Residual Life"));
                Assert.AreSame(element.ModifiedScalePlot, ShowPlot(view, "Modified Scale"));
                Assert.AreSame(element.ShapePlot, ShowPlot(view, "Shape"));
                session.CloseDocuments();
                session.AssertUnchanged(Pot);
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
                    "ghcn-peaks-over-threshold-example-chronology.plotspec.json.gz", "ghcn-peaks-over-threshold-example-frequency.plotspec.json.gz",
                    "ghcn-peaks-over-threshold-example-mean-residual-life.plotspec.json.gz", "ghcn-peaks-over-threshold-example-modified-scale.plotspec.json.gz",
                }, AssertFigures(session, Folder, "ghcn-peaks-over-threshold-example-"));
                session.AssertUnchanged(Pot);
            });
        }
    }
}
