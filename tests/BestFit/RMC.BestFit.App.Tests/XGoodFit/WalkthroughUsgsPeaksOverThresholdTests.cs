using System;
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
    /// XGoodFit addition: the Orestimba peaks-over-threshold walkthrough
    /// (examples/2-input-data/3-peaks-over-threshold/usgs-peaks-over-threshold-example.md) followed through the main window:
    /// the explorer and properties, the daily source, the saved settings and sample (78 events; the shipped file's λ is 78 / 95,
    /// see below), the chronology, frequency and POT diagnostics, and the figures. InputDataViewTests compares
    /// the native exports of the input's plots; the source's export is here. See
    /// docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughUsgsPeaksOverThresholdTests
    {
        private const string Folder = "2-input-data/3-peaks-over-threshold/";
        private const string Source = "USGS - 11274500 - Daily Discharge";
        private const string Pot = "USGS - 11274500 - Peaks-Over-Threshold";
        private static readonly WalkthroughSession Session = new WalkthroughSession("usgs-peaks-over-threshold-example");

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>
        /// "Find the controls" and step 2: the explorer (the walkthrough's capture), and the input's properties (the capture
        /// of the panel): Data Entry Method = Peaks-Over-Threshold Series, the daily source, the 650 cfs Threshold, 5 Minimum
        /// Steps and Smoothing = None (the retained Period of 1 has no row).
        /// </summary>
        [TestMethod]
        public void FindTheControls_TheExplorerAndPropertiesAreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                AssertExplorer(session, Folder + "screenshots/usgs-pot-project-explorer-current.json.gz");
                session.OpenDocument<InputDataControl>(Pot);
                var properties = (InputDataPropertiesControl)session.Properties;
                AssertPanel(properties, Folder + "screenshots/usgs-pot-properties-current.json.gz");
                Assert.AreEqual("Peaks-Over-Threshold Series", Property(properties, "Data Entry Method"));
                Assert.AreEqual(Source, Property(properties, "Time Series"));
                Assert.AreEqual("650", Property(properties, "Threshold"));
                Assert.AreEqual("5", Property(properties, "Minimum Steps"));
                Assert.AreEqual("None", Property(properties, "Smoothing"));
                Assert.IsFalse(properties.FindControl<Control>("Period").IsVisible, "no smoothing period while Smoothing = None");
                var element = session.Element<InputData>(Pot);
                Assert.AreEqual(1, element.Period, "the retained period");
                Assert.AreEqual(SmoothingFunctionType.None, element.SmoothingFunction);
                session.AssertUnchanged(Pot);
            });
        }

        /// <summary>
        /// "Open and inspect the source" and step 1: 34,438 daily means in cfs from 1932-04-01 to 2026-07-14, none missing,
        /// with zero-flow days; and its native export (screenshots/usgs-pot-daily-discharge-native).
        /// </summary>
        [TestMethod]
        public void Source_IsTheDailyDischarge()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                WalkthroughTimeSeriesTesting.AssertRecord(session, new WalkthroughTimeSeriesTesting.Record(Source, "Flow (cfs)", "1932-04-01", "2026-07-14", 34438, 0));
                var view = session.OpenDocument<TimeSeriesControl>(Source);
                WalkthroughTimeSeriesTesting.Show(view, "Time Series");
                Assert.IsTrue(Points(view.TimeSeriesPlot.Series.Single().InternalSeries, "ActualPoints").Any(p => p.Y == 0), "zero-flow days");
                Assert.AreEqual("0", WalkthroughTimeSeriesTesting.SummaryStatistics(view)["Minimum"], "the minimum is zero flow");
                session.CloseDocuments();
                AssertNativeTimeSeries(session, Folder, "usgs-pot-daily-discharge-native.json.gz");
                session.AssertUnchanged(Source);
            });
        }

        /// <summary>
        /// "Saved configuration and sample" and steps 3-6: 78 event rows indexed 1937-2025 (89 indexed years), several sharing a
        /// year; the POT Diagnostics offer Mean Residual Life, Modified Scale and Shape below the plot.
        /// </summary>
        /// <remarks>
        /// The walkthrough's "Saved Lambda 0.8764044944 per year (78 / 89)" and "Explicit source-exposure field: Absent in this
        /// older saved input" describe an earlier file. The shipped project (saved 2026-09-29, the file the native exports were
        /// made from) holds Lambda = 0.82105263157894737 and PointProcessObservationYears = 95: the current extraction's
        /// inclusive source span, 1932-2026, which the walkthrough says a rerun would give. RMC-BestFit's loader reads both, and
        /// the summary statistics show λ = 0.8211, in XGoodFit as in RMC-BestFit.
        /// </remarks>
        [TestMethod]
        public void SavedSample_AndItsDiagnostics_AreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var view = session.OpenDocument<InputDataControl>(Pot);
                Assert.AreEqual(78, ExactData(view).Items.Count, "the event rows");
                Dictionary<string, string> statistics = SummaryStatistics(view);
                Assert.AreEqual("78", statistics["Record Length"]);
                // The walkthrough's 0.8764044944 (78 / 89) is an earlier file's: see the remarks.
                AssertRounded(78 / 95.0, statistics["Events Per Index (λ)"], 10, "the shipped file's λ");
                var element = session.Element<InputData>(Pot);
                Assert.AreEqual(95, element.DataFrame.PointProcessObservationYears, "the stored source exposure, 1932-2026");
                Assert.AreEqual(78 / 95.0, element.DataFrame.Lambda, 1E-12, "78 / 95");
                Assert.AreEqual(0.8764044944, Math.Round(78 / 89.0, 10), "the walkthrough's rate is 78 over the event span");

                var chronology = Series(ShowPlot(view, "Chronology Plot"), "Exact Data");
                Assert.AreEqual(78, chronology.Count);
                Assert.AreEqual(1937, chronology.Min(p => p.X));
                Assert.AreEqual(2025, chronology.Max(p => p.X));
                Assert.AreEqual(89, chronology.Max(p => p.X) - chronology.Min(p => p.X) + 1, "the 89 indexed years");
                Assert.IsTrue(chronology.GroupBy(p => p.X).Any(g => g.Count() > 1), "several events share a year index");

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
                    "usgs-peaks-over-threshold-example-chronology.plotspec.json.gz", "usgs-peaks-over-threshold-example-frequency.plotspec.json.gz",
                    "usgs-peaks-over-threshold-example-mean-residual-life.plotspec.json.gz", "usgs-peaks-over-threshold-example-shape-stability.plotspec.json.gz",
                }, AssertFigures(session, Folder, "usgs-peaks-over-threshold-example-"));
                session.AssertUnchanged(Pot);
            });
        }
    }
}
