using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughBayesianTesting;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Waimea and Makaweli walkthrough
    /// (examples/5-bivariate-distribution-analysis/2-coincident-frequency/waimea-river-stage-frequency.md) followed through the
    /// main window: the four inputs (counts, flags, perception windows), the seven LP3 alternatives and their priors, the
    /// copulas and their eligible pairs, the conditional CFA, the saved diagnostics and 1% AEP quantiles, the retained legacy
    /// material, and the figures. CoincidentFrequencyViewTests compares the CFA's frequency figure and export. See
    /// docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughWaimeaRiverStageFrequencyTests
    {
        private const string Folder = "5-bivariate-distribution-analysis/2-coincident-frequency/";
        private const string Waimea = "16031000_Waimea_Peaks";
        private const string Makaweli = "16036000_Makaweli_Peaks";
        private const string Conditional = "16036000_Makaweli_Conditional_Peaks";
        private const string Proof = "Simulated Proof";
        private const string XMarginal = "WaimeaPk - Exact + Historical + RR Prior_RSkew";
        private const string YMarginal = "MakaweliPk - Exact + RR Prior_RSkew";
        private const string ConditionalY = "MakaweliPk - Cond - Exact";
        private const string Cfa = "CFA - Normal - Conditional";
        private static readonly WalkthroughSession Session = new WalkthroughSession("waimea-river-stage-frequency");

        /// <summary>The walkthrough's copulas: family, saved dependence parameter, marginals.</summary>
        private static readonly (string Name, string Family, string Theta, string Y)[] Copulas =
        {
            ("Normal Copula", "Normal", "0.689", YMarginal), ("Gumbel Copula", "Gumbel", "1.708", YMarginal),
            ("Clayton Copula", "Clayton", "1.982", YMarginal), ("Joe Copula", "Joe", "1.829", YMarginal),
            ("Frank Copula", "Frank", "5.388", YMarginal), ("AMH Copula", "AliMikhailHaq", "1", YMarginal),
            ("Normal Copula - Conditional", "Normal", "0.167", ConditionalY),
        };

        /// <summary>The walkthrough's "Saved diagnostics".</summary>
        private static readonly Fit[] Fits =
        {
            new("Normal Copula", 4, 10, "Mean", "-31.426", "0.99988", 9314),
            new("Gumbel Copula", 4, 10, "Mode", "-23.767", "1.00109", 9694),
            new("Clayton Copula", 4, 10, "Mode", "-31.725", "0.99991", 9145),
            new("Joe Copula", 4, 10, "Mode", "-18.594", "1.00022", 9359),
            new("Frank Copula", 4, 10, "Mode", "-29.873", "1.00065", 9767),
            new("AMH Copula", 4, 10, "Mode", "-24.041", "1.00100", 5590),
            new("Normal Copula - Conditional", 4, 10, "Mean", "0.323", "0.99977", 9349),
            new("MakaweliPk - Exact", 4, 20, "Mode", "1,476.621", "1.00058", 8841),
            new("WaimeaPk - Exact + Historical + RR Prior", 6, 30, "Mean", "1,280.318", "1.00036", 9272),
            new("WaimeaPk - Exact + Historical", 4, 20, "Mode", "1,280.653", "1.00068", 8843),
            new("MakaweliPk - Exact + RR Prior", 4, 20, "Mode", "1,476.345", "1.00078", 9030),
            new(YMarginal, 6, 30, "Mode", "1,476.071", "1.00046", 9258),
            new(XMarginal, 6, 30, "Mean", "1,280.42", "1.00041", 9010),
            new(ConditionalY, 6, 30, "Mean", "1,060.354", "1.00058", 9374),
        };

        /// <summary>The walkthrough's 1% AEP quantiles: the point estimate and its 90% bounds.</summary>
        private static readonly (string Name, string Point, string Lower, string Upper)[] Quantiles =
        {
            ("MakaweliPk - Exact", "24,635.987", "21,434.962", "36,905.435"),
            ("WaimeaPk - Exact + Historical + RR Prior", "39,832.633", "33,513.611", "50,415.124"),
            ("WaimeaPk - Exact + Historical", "36,587.137", "33,339.818", "51,693.798"),
            ("MakaweliPk - Exact + RR Prior", "24,781.808", "21,502.94", "34,741.177"),
            (YMarginal, "25,442.471", "21,797.812", "35,103.51"),
            (XMarginal, "41,247.679", "34,184.13", "51,990.154"),
            (ConditionalY, "30,308.621", "22,039.376", "50,429.454"),
        };

        /// <summary>The walkthrough's saved CFA rows: a fixed response Z, its AEP and probability bounds.</summary>
        private static readonly (string Z, string Aep, string Lower, string Upper)[] Rows =
        {
            ("12.285", "0.776525", "0.710759", "0.843447"), ("17.859", "0.208193", "0.151021", "0.27062"), ("23.005", "0.0139775", "0.0022503", "0.0321982"),
        };

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>
        /// "Inspect the four inputs": Waimea's 64 exact peaks (1914-2023, cfs) with three low flags (1973, 1984, 1987) and
        /// two perception windows (1919-1943 and 1997-2016) at 37,100 cfs, 25 and 20 below and none above; Makaweli's 75
        /// (1945-2019) and the conditional 54 (1946-2019) in Value units, without flags or windows; Simulated Proof's 10,000
        /// indexed 1-10,000. None has uncertain or interval data.
        /// </summary>
        [TestMethod]
        public void Inputs_AreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (var (name, unit, count, first, last, flags, windows) in new[]
                {
                    (Waimea, "Annual Peak Flow (cfs)", 64, 1914, 2023, 3, 2), (Makaweli, "Value", 75, 1945, 2019, 0, 0),
                    (Conditional, "Value", 54, 1946, 2019, 0, 0), (Proof, "Value", 10000, 1, 10000, 0, 0),
                })
                {
                    var view = session.OpenDocument<InputDataControl>(name);
                    Assert.AreEqual(unit, Property(session.Properties, "Unit Label"), name);
                    var statistics = WalkthroughInputDataTesting.SummaryStatistics(view);
                    Assert.AreEqual(count.ToString("N0", Us), statistics["Record Length"], name + ": exact values");
                    Assert.AreEqual(flags.ToString(CultureInfo.InvariantCulture), statistics["Low Outliers"], name + ": low flags");
                    var plot = WalkthroughInputDataTesting.ShowPlot(view, "Chronology Plot");
                    var exact = WalkthroughInputDataTesting.Series(plot, "Exact Data");
                    var low = flags > 0 ? WalkthroughInputDataTesting.Series(plot, "Low Outlier Data") : new List<(double X, double Y)>();
                    var indexes = exact.Concat(low).Select(p => p.X).ToList();
                    Assert.AreEqual(count, indexes.Count, name + ": every exact value in the chronology");
                    Assert.AreEqual(first, indexes.Min(), name + ": the first index");
                    Assert.AreEqual(last, indexes.Max(), name + ": the last index");
                    if (flags > 0)
                        CollectionAssert.AreEquivalent(new[] { 1973d, 1984, 1987 }, low.Select(p => p.X).ToList(), name + ": the flagged years");
                    Assert.AreEqual(windows, WalkthroughInputDataTesting.ThresholdRows(view), name + ": perception windows");
                    var element = session.Element<InputData>(name);
                    Assert.AreEqual(0, element.DataFrame.UncertainSeries.Count + element.DataFrame.IntervalSeries.Count, name + ": no uncertain or interval data");
                    if (windows > 0)
                    {
                        var rows = ThresholdRows(view);
                        // The grid shows each window's start, end, threshold and number above, as upstream's does; the
                        // number below is the saved threshold series', which no column shows.
                        CollectionAssert.AreEqual(new[] { "1919", "1943", "37,100", "0" }, rows[0], "the first window: " + string.Join(" | ", rows[0]));
                        CollectionAssert.AreEqual(new[] { "1997", "2016", "37,100", "0" }, rows[1], "the second window: " + string.Join(" | ", rows[1]));
                        CollectionAssert.AreEqual(new[] { 25, 20 }, element.DataFrame.ThresholdSeries.OfType<RMC.BestFit.Models.ThresholdData>().Select(t => (int)t.NumberBelow).ToList(), "the windows' saved numbers below");
                    }
                    session.CloseDocuments();
                }
                session.AssertUnchanged(Waimea, Makaweli, Conditional, Proof);
            });
        }

        /// <summary>
        /// "Trace the marginal information": seven LP3 alternatives; the RR alternatives enable three LogNormal quantile priors
        /// (base-10 log means and SDs at AEP 0.1, 0.01 and 0.002); the two RSkew alternatives disable the default flat priors
        /// and put a Normal prior (mean -0.157, SD 0.46) on the log skew, with Uniform bounds on the mean of log flow (0-6)
        /// and the scale (positive-2), and Jeffreys' rule for the scale. The MCMC Reports show the parameter priors.
        /// </summary>
        [TestMethod]
        public void MarginalAlternatives_AndTheirPriors_AreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var analyses = session.Project.ElementCollections.OfType<UnivariateAnalysisCollection>().Single().ToList();
                Assert.AreEqual(7, analyses.Count, "seven alternatives");
                var priors = new Dictionary<string, (string Aep, string Mean, string Sd)[]>
                {
                    ["Waimea"] = new[] { ("0.1", "4.3234", "0.1758"), ("0.01", "4.6408", "0.163401346"), ("0.002", "4.8037", "0.1578") },
                    ["Makaweli"] = new[] { ("0.1", "4.2028", "0.1758"), ("0.01", "4.474", "0.1634"), ("0.002", "4.6175", "0.1578") },
                };
                foreach (IElement analysis in analyses)
                {
                    var view = session.OpenDocument<Control>(analysis.Name);
                    Control properties = session.Properties;
                    Assert.AreEqual("Log-Pearson Type III", Property(properties, "Distribution"), analysis.Name + ": LP3");
                    var element = (UnivariateAnalysis)analysis;
                    bool rr = analysis.Name.Contains("RR Prior");
                    var quantilePriors = element.UnivariateDistribution.QuantilePriors;
                    Assert.AreEqual(rr, IsTicked(properties, "Enable Priors on Quantiles"), analysis.Name + ": quantile priors on");
                    if (rr)
                    {
                        var expected = priors[analysis.Name.StartsWith("Waimea") ? "Waimea" : "Makaweli"];
                        var shown = QuantilePriorTexts(properties);
                        Assert.AreEqual(3, shown.Count, analysis.Name + ": three quantile priors, " + string.Join(" | ", shown));
                        for (int i = 0; i < 3; i++)
                        {
                            StringAssert.Contains(shown[i], "Log", analysis.Name + ": a LogNormal prior");
                            foreach (string number in new[] { expected[i].Mean, expected[i].Sd })
                                Assert.IsTrue(shown[i].Contains(number.TrimEnd('0'), StringComparison.Ordinal) || shown[i].Contains(Math.Round(UsNumber(number), 4).ToString(Us), StringComparison.Ordinal),
                                    $"{analysis.Name}: prior {i} shows {number}: {shown[i]}");
                        }
                        Assert.AreEqual(3, quantilePriors.Count);
                        for (int i = 0; i < 3; i++)
                            Assert.AreEqual(UsNumber(expected[i].Aep), quantilePriors[i].Alpha, 1E-12, analysis.Name + ": prior " + i + "'s AEP");
                    }
                    string report = Report(view);
                    if (analysis.Name.EndsWith("RSkew", StringComparison.Ordinal))
                    {
                        Assert.IsFalse(IsTicked(properties, "Use Default Flat Priors"), analysis.Name + ": the default flat priors are off");
                        // The report's PRIOR CONFIGURATION: each parameter's prior, as "N (mean, SD)" or "U (lower, upper)".
                        StringAssert.Matches(report, new System.Text.RegularExpressions.Regex(@"^\s*Skew \(of log\).*\sN \(-0\.157, 0\.46\)", System.Text.RegularExpressions.RegexOptions.Multiline),
                            analysis.Name + ": a Normal prior on the log skew, mean -0.157 and SD 0.46");
                        StringAssert.Matches(report, new System.Text.RegularExpressions.Regex(@"^\s*Mean \(of log\).*\sU \(0, 6\)", System.Text.RegularExpressions.RegexOptions.Multiline),
                            analysis.Name + ": Uniform bounds 0-6 on the mean of log flow");
                        StringAssert.Matches(report, new System.Text.RegularExpressions.Regex(@"^\s*Std Dev \(of log\).*\sU \(0, 2\)", System.Text.RegularExpressions.RegexOptions.Multiline),
                            analysis.Name + ": Uniform bounds up to 2 on the scale");
                        Assert.IsTrue(IsTicked(properties, "Use Jeffreys' Rule for Scale"), analysis.Name + ": Jeffreys' rule for the scale");
                    }
                    session.CloseDocuments();
                }
                session.AssertUnchanged(analyses.Select(a => a.Name).ToArray());
            });
        }

        /// <summary>
        /// "Follow the analysis connections" and the copula table: every copula's family, saved dependence parameter (AMH's
        /// near its upper bound of 1) and marginals; the ordinary copulas' 52 eligible pairs and the conditional one's 51; the
        /// saved query at (0, 0) returns probability one.
        /// </summary>
        [TestMethod]
        public void Copulas_HaveTheirFamiliesMarginalsParametersAndPairs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (var (name, family, theta, y) in Copulas)
                {
                    var view = session.OpenDocument<BivariateAnalysisControl>(name);
                    Assert.AreEqual(XMarginal, Property(session.Properties, "Marginal X"), name);
                    Assert.AreEqual(y, Property(session.Properties, "Marginal Y"), name);
                    var element = session.Element<BivariateAnalysis>(name);
                    Assert.AreEqual(family, element.BivariateDistribution.CopulaType.ToString(), name + ": the family");
                    Assert.AreEqual("InferenceFromMargins", element.BivariateDistribution.CopulaEstimationMethod.ToString(), name);
                    AssertParameters(view, name, theta);
                    if (family == "AliMikhailHaq")
                        Assert.AreEqual(0.999995, element.BivariateDistribution.Parameters[0].Value, 5E-7, "AMH's point estimate, about 0.999995");

                    ShowTab(view, "Distribution Results");
                    ShowTab(view, "X-Y Plot");
                    int pairs = Points(view.GetCurrentPlot().Series.Single(s => s.Title == "Exact Data").InternalSeries, "ActualPoints").Count;
                    Assert.AreEqual(y == ConditionalY ? 51 : 52, pairs, name + ": the eligible pairs");

                    var results = Results(view);
                    Assert.AreEqual(1, results.Items.Count, name + ": the saved query");
                    Assert.AreEqual(0, UsNumber(CellText(results, 0, 0)), name + ": at X = 0");
                    Assert.AreEqual(0, UsNumber(CellText(results, 0, 1)), name + ": and Y = 0");
                    Assert.AreEqual(1, UsNumber(CellText(results, 0, 5)), 5E-5, name + ": probability one");
                    session.CloseDocuments();
                }
                session.AssertUnchanged(Copulas.Select(c => c.Name).ToArray());
            });
        }

        /// <summary>
        /// "Follow the analysis connections", step 5, and the CFA rows: CFA - Normal - Conditional uses Normal Copula -
        /// Conditional, with an empty Input Data binding; its 10x5 response grid spans 7.14-28.15; its 50 response outputs
        /// carry 90% bounds from 10,000 draws; the walkthrough's rows are an AEP and bounds at a fixed response.
        /// </summary>
        [TestMethod]
        public void ConditionalCfa_IsTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var view = session.OpenDocument<CoincidentFrequencyControl>(Cfa);
                Control properties = session.Properties;
                Assert.AreEqual("Normal Copula - Conditional", Property(properties, "Bivariate Analysis"));
                var element = session.Element<CoincidentFrequencyAnalysis>(Cfa);
                Assert.IsNull(element.InputData, "no observed-response input");
                ShowTab(properties, "Output");
                Assert.AreEqual("50", Property(properties, "Z Bins"));
                StringAssert.StartsWith(Property(properties, "Credible Interval"), "90%");
                ShowTab(properties, "General");
                Assert.AreEqual(10000, element.BayesianAnalysis.OutputLength, "10,000 output draws");

                ShowTab(view, "Bivariate Response");
                var grid = view.BivariateDataGrid;
                Assert.AreEqual(10, grid.Items.Count, "ten X ordinates");
                Assert.AreEqual(5, grid.Columns.Count, "five Y ordinates");
                var cells = Enumerable.Range(0, 10).SelectMany(i => Enumerable.Range(0, 5).Select(j => UsNumber(CellText(grid, i, j)))).ToList();
                Assert.AreEqual(7.14, Math.Round(cells.Min(), 2), "the response's minimum");
                Assert.AreEqual(28.15, Math.Round(cells.Max(), 2), "the response's maximum");

                CopyPasteDataGrid table = Results(view);
                Assert.AreEqual(50, table.Items.Count, "50 response outputs");
                Assert.AreEqual("Response Value", table.Columns[0].Header as string, "each row fixes the response");
                var responses = Enumerable.Range(0, table.Items.Count).Select(k => UsNumber(CellText(table, k, 0))).ToList();
                foreach (var row in Rows)
                {
                    int k = responses.FindIndex(z => Math.Round(z, 3) == UsNumber(row.Z));
                    Assert.IsTrue(k >= 0, "a row at Z = " + row.Z);
                    AssertRounded(UsNumber(row.Upper), CellText(table, k, 1), 7, $"at {row.Z}: the upper bound");
                    AssertRounded(UsNumber(row.Lower), CellText(table, k, 2), 7, $"at {row.Z}: the lower bound");
                    AssertRounded(UsNumber(row.Aep), CellText(table, k, 4), 7, $"at {row.Z}: the AEP");
                }
                session.CloseDocuments();
                session.AssertUnchanged(Cfa);
            });
        }

        /// <summary>The walkthrough's "Saved diagnostics" of the seven copulas and seven marginals.</summary>
        [TestMethod]
        public void SavedDiagnostics_AreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (Fit fit in Fits)
                    AssertFit(session, fit);
                session.AssertUnchanged(Fits.Select(f => f.Name).ToArray());
            });
        }

        /// <summary>The walkthrough's 1% AEP quantiles and 90% bounds, as each alternative's Tabular Results show them.</summary>
        [TestMethod]
        public void OnePercentQuantiles_AreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (var (name, point, lower, upper) in Quantiles)
                {
                    var view = session.OpenDocument<Control>(name);
                    CopyPasteDataGrid table = Results(view);
                    int row = Enumerable.Range(0, table.Items.Count).Single(i => UsNumber(CellText(table, i, 0)) == 0.01);
                    AssertRounded(UsNumber(upper), CellText(table, row, 1), 3, name + ": the upper bound");
                    AssertRounded(UsNumber(lower), CellText(table, row, 2), 3, name + ": the lower bound");
                    AssertRounded(UsNumber(point), CellText(table, row, 4), 3, name + ": the 1% AEP quantile");
                    session.CloseDocuments();
                }
                session.AssertUnchanged(Quantiles.Select(q => q.Name).ToArray());
            });
        }

        /// <summary>
        /// "Retained legacy material": the database keeps ten legacy Bayesian Estimation Analysis records (which the
        /// application doesn't list), and the fitting analysis 16031000_WaimeaPk binds the Waimea input with 15 successful,
        /// visible fits.
        /// </summary>
        [TestMethod]
        public void LegacyMaterial_IsRetained()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                using (var sqlite = new DatabaseManager.SQLiteManager(session.Project.FullFileName))
                {
                    sqlite.Open();
                    try
                    {
                        Assert.AreEqual(10, sqlite.GetTableManager("Bayesian Estimation Analysis").NumberOfRows, "the legacy records");
                    }
                    finally
                    {
                        sqlite.Close();
                    }
                }
                var view = session.OpenDocument<Control>("16031000_WaimeaPk");
                Assert.AreEqual(Waimea, Property(session.Properties, "Input Data"));
                var table = view.FindControl<CopyPasteDataGrid>("AICBICTable");
                Assert.AreEqual(15, table.Items.Count, "15 fits");
                for (int i = 0; i < table.Items.Count; i++)
                {
                    Assert.IsTrue(table.GetRow(i).IsEnabled, "fit " + i + " succeeded");
                    Assert.AreEqual(true, table.GetRow(i).GetVisualDescendants().OfType<CheckBox>().First().IsChecked, "fit " + i + " is shown");
                }
                session.CloseDocuments();
                session.AssertUnchanged("16031000_WaimeaPk");
            });
        }

        /// <summary>
        /// "Read the current figures": Waimea's chronology, the RR-prior/RSkew Waimea marginal's frequency curve, the
        /// conditional Makaweli marginal's, and the conditional pairs in marginal-CDF space are what the views show; the
        /// conditional copula's Value-space scatter is RMC-BestFit's export of it. The conditional joint-exceedance contours
        /// and the CFA's figure and export are BivariateAnalysisViewTests' kind of check and CoincidentFrequencyViewTests'.
        /// </summary>
        [TestMethod]
        public void Figures_AreWhatTheViewsShow()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var input = session.OpenDocument<InputDataControl>(Waimea);
                AssertFigureData(WalkthroughInputDataTesting.ShowPlot(input, "Chronology Plot"), Data(Folder + "images/waimea-river-stage-frequency-waimea-chronology.plotspec.json.gz"), "waimea-chronology");
                session.CloseDocuments();

                foreach (var (name, figure) in new[] { (XMarginal, "waimea-priors"), (ConditionalY, "makaweli-conditional") })
                {
                    var view = session.OpenDocument<UnivariateAnalysisControl>(name);
                    ShowTab(view, "Distribution Results");
                    ShowTab(view, "Frequency Plot");
                    AssertFigureData(view.GetCurrentPlot(), Data(Folder + "images/waimea-river-stage-frequency-" + figure + ".plotspec.json.gz"), figure);
                    session.CloseDocuments();
                }

                var copula = session.OpenDocument<BivariateAnalysisControl>("Normal Copula - Conditional");
                ShowTab(copula, "Distribution Results");
                ShowTab(copula, "X-Y Plot");
                NativeCaptures.AssertPlotMatches(copula.GetCurrentPlot(), Data(Folder + "screenshots/waimea-conditional-scatter.json.gz"), 1E-9);
                copula.AxisTypeComboBox.SelectedItem = copula.AxisTypeComboBox.Items.OfType<ComboBoxItem>().Single(i => i.Content as string == "Probability");
                BestFitShell.Frame();
                AssertFigureData(copula.GetCurrentPlot(), Data(Folder + "images/waimea-river-stage-frequency-conditional-cdf.plotspec.json.gz"), "conditional-cdf");
                copula.AxisTypeComboBox.SelectedItem = copula.AxisTypeComboBox.Items.OfType<ComboBoxItem>().Single(i => i.Content as string == "Value");
                session.CloseDocuments();
                session.AssertUnchanged(Waimea, XMarginal, ConditionalY, "Normal Copula - Conditional");
            });
        }

        // The texts of the Threshold Data grid's rows, cell by cell.
        private static List<string[]> ThresholdRows(InputDataControl view)
        {
            Select(Tab(WalkthroughInputDataTesting.MainTabs(view), "Data Frame"));
            Select(Tab(WalkthroughInputDataTesting.DataTabs(view), "Threshold Data"));
            var grid = view.FindControl<ValidationDataGrid>("ThresholdDataGrid");
            var rows = Enumerable.Range(0, grid.Items.Count)
                .Select(i => Enumerable.Range(0, grid.Columns.Count).Select(j => CellText(grid, i, j)).Where(t => t.Length > 0).ToArray()).ToList();
            Select(Tab(WalkthroughInputDataTesting.DataTabs(view), "Exact Data"));
            return rows;
        }

        // The quantile priors' distributions as the properties panel's table shows them.
        private static List<string> QuantilePriorTexts(Control properties)
        {
            var control = properties.FindControl<QuantilePriorsControl>("QuantilePriorsControl");
            var grid = control.GetVisualDescendants().OfType<CopyPasteDataGrid>().First();
            return Enumerable.Range(0, grid.Items.Count).Select(i => CellText(grid, i, 1)).ToList();
        }

        // Whether a properties panel's check box row titled title is ticked.
        private static bool IsTicked(Control properties, string title) =>
            properties.GetVisualDescendants().OfType<BooleanPropertyControl>().Single(b => b.Title == title)
                .GetVisualDescendants().OfType<CheckBox>().Single().IsChecked == true;
    }
}
