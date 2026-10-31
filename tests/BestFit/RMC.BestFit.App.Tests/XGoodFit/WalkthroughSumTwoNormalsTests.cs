using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughBayesianTesting;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the sum of two Normals walkthrough
    /// (examples/5-bivariate-distribution-analysis/2-coincident-frequency/sum-two-normals.md) followed through the main window:
    /// the three scenarios' inputs, marginals, copulas and coincident frequency analyses (their grids, bins, intervals and
    /// saved rows), the saved diagnostics, the copula scatter, and the figures. CoincidentFrequencyViewTests compares the
    /// response figures and export. See docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughSumTwoNormalsTests
    {
        private const string Folder = "5-bivariate-distribution-analysis/2-coincident-frequency/";
        private static readonly WalkthroughSession Session = new WalkthroughSession("sum-two-normals");

        /// <summary>The walkthrough's scenarios: the analyses' names, saved X and Y means and SDs, fitted rho and response bins.</summary>
        private static readonly (string Rho, string X, string Y, string Copula, string Cfa, string XMean, string XSd, string YMean, string YSd, string FittedRho, int Bins, string Width)[] Scenarios =
        {
            ("0.0", "X Marginal - Rho = 0.0", "Y Marginal - Rho = 0.0", "Normal Copula - Rho = 0.0", "CFA - Rho = 0.0", "100.637403", "14.781253", "80.618735", "24.484764", "0.088550744", 20, "95%"),
            ("-0.5", "X Marginal - Rho = -0.5", "Y Marginal - Rho = -0.5", "Normal Copula - Rho = -0.5", "CFA - Rho = -0.5", "96.680088", "15.606327", "80.238168", "27.424265", "-0.454016064", 50, "90%"),
            ("+0.5", "X Marginal - Rho = +0.5", "Y Marginal - Rho =+0.5", "Normal Copula - Rho =+0.5", "CFA - Rho = +0.5", "102.259104", "14.709635", "84.477595", "25.259022", "0.398218880", 50, "90%"),
        };

        /// <summary>The walkthrough's saved response rows: a fixed response Z, its AEP and probability bounds.</summary>
        private static readonly (string Cfa, string Z, string Aep, string Lower, string Upper)[] Rows =
        {
            ("CFA - Rho = 0.0", "135.947", "0.931373", "0.890485", "0.96115"),
            ("CFA - Rho = 0.0", "184.895", "0.449537", "0.375522", "0.524639"),
            ("CFA - Rho = 0.0", "233.842", "0.0426435", "0.0214771", "0.073852"),
            ("CFA - Rho = -0.5", "132.551", "0.9621", "0.932147", "0.98229"),
            ("CFA - Rho = -0.5", "181.898", "0.418483", "0.336907", "0.500623"),
            ("CFA - Rho = -0.5", "227.449", "0.0200105", "0.00805294", "0.0399749"),
            ("CFA - Rho = +0.5", "132.551", "0.939866", "0.912128", "0.961259"),
            ("CFA - Rho = +0.5", "181.898", "0.555414", "0.499386", "0.609683"),
            ("CFA - Rho = +0.5", "227.449", "0.122184", "0.0891043", "0.160763"),
        };

        /// <summary>The walkthrough's "Saved diagnostics".</summary>
        private static readonly Fit[] Fits =
        {
            new("Normal Copula - Rho = 0.0", 4, 10, "Mean", "1.021", "0.99998", 9457),
            new("Normal Copula - Rho = -0.5", 4, 10, "Mean", "-22.153", "1.00026", 9189),
            new("Normal Copula - Rho =+0.5", 4, 10, "Mean", "-16.217", "1.00088", 9290),
            new("X Marginal - Rho = 0.0", 4, 20, "Mean", "824.045", "1.00030", 9321),
            new("Y Marginal - Rho = 0.0", 4, 20, "Mean", "924.774", "1.00079", 8771),
            new("X Marginal - Rho = -0.5", 4, 20, "Mean", "834.874", "0.99996", 9769),
            new("Y Marginal - Rho = -0.5", 4, 20, "Mean", "947.472", "0.99995", 9278),
            new("X Marginal - Rho = +0.5", 4, 20, "Mean", "822.94", "1.00061", 9437),
            new("Y Marginal - Rho =+0.5", 4, 20, "Mean", "931.152", "1.00018", 9486),
        };

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>"Inspect the scenarios": each scenario's X, Y and X+Y inputs have 100 exact values indexed 1-100, in generic units.</summary>
        [TestMethod]
        public void Inputs_Are100ExactValuesEach()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (var scenario in Scenarios)
                {
                    foreach (string kind in new[] { "X", "Y", "X+Y" })
                    {
                        string name = kind + " Data - Rho = " + scenario.Rho;
                        var view = session.OpenDocument<InputDataControl>(name);
                        Assert.AreEqual("Value", Property(session.Properties, "Unit Label"), name);
                        Assert.AreEqual("100", WalkthroughInputDataTesting.SummaryStatistics(view)["Record Length"], name);
                        var indexes = WalkthroughInputDataTesting.Series(WalkthroughInputDataTesting.ShowPlot(view, "Chronology Plot"), "Exact Data").Select(p => p.X).ToList();
                        CollectionAssert.AreEqual(Enumerable.Range(1, 100).Select(i => (double)i).ToList(), indexes, name + ": indexed 1-100");
                        session.CloseDocuments();
                    }
                }
                session.AssertUnchanged();
            });
        }

        /// <summary>
        /// "Inspect the scenarios" and step 1: the marginals' saved means and SDs and the copulas' fitted rho, as their
        /// summary statistics show them (four decimals), each fit on its scenario's inputs.
        /// </summary>
        [TestMethod]
        public void MarginalsAndCopulas_HaveTheSavedParameters()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (var scenario in Scenarios)
                {
                    foreach (var (name, data, mean, sd) in new[] { (scenario.X, "X Data - Rho = " + scenario.Rho, scenario.XMean, scenario.XSd), (scenario.Y, "Y Data - Rho = " + scenario.Rho, scenario.YMean, scenario.YSd) })
                    {
                        var view = session.OpenDocument<Control>(name);
                        Assert.AreEqual(data, Property(session.Properties, "Input Data"), name + ": its exact input");
                        Assert.AreEqual("Normal", Property(session.Properties, "Distribution"), name);
                        AssertParameters(view, name, mean, sd);
                        session.CloseDocuments();
                    }
                    var copula = session.OpenDocument<BivariateAnalysisControl>(scenario.Copula);
                    Assert.AreEqual(scenario.X, Property(session.Properties, "Marginal X"), scenario.Copula);
                    Assert.AreEqual(scenario.Y, Property(session.Properties, "Marginal Y"), scenario.Copula);
                    AssertParameters(copula, scenario.Copula, scenario.FittedRho);
                    session.CloseDocuments();
                }
                session.AssertUnchanged(Scenarios.SelectMany(s => new[] { s.X, s.Y, s.Copula }).ToArray());
            });
        }

        /// <summary>
        /// Steps 2-5: each CFA's tabs (Bivariate Response, Frequency Plot, Tabular Results, and no MCMC run of its own); its
        /// General properties (its Bivariate Analysis and X+Y Input Data, the PRIMARY (X) and SECONDARY (Y) ORDINATES) and
        /// Output properties (20 or 50 Z Bins, the 95% or 90% interval); the 7x7 grid of X+Y on X 65-135 and Y 22-138, its
        /// responses spanning 87-273; and the walkthrough's saved rows, each an AEP and bounds at a fixed response.
        /// </summary>
        [TestMethod]
        public void CoincidentFrequencyAnalyses_AreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                double[] x = { 65, 75, 90, 100, 110, 125, 135 };
                double[] y = { 22, 39, 63, 80, 97, 121, 138 };
                foreach (var scenario in Scenarios)
                {
                    var view = session.OpenDocument<CoincidentFrequencyControl>(scenario.Cfa);
                    Control properties = session.Properties;
                    CollectionAssert.AreEqual(new[] { "Bivariate Response", "Frequency Plot", "Tabular Results" },
                        Headers(view.GetVisualDescendants().OfType<TabControl>().First(t => t.Items.OfType<TabItem>().Any(i => i.Header as string == "Bivariate Response"))),
                        scenario.Cfa + ": its views, without an MCMC run");
                    Assert.AreEqual(scenario.Copula, Property(properties, "Bivariate Analysis"), scenario.Cfa);
                    Assert.AreEqual("X+Y Data - Rho = " + scenario.Rho, Property(properties, "Input Data"), scenario.Cfa);
                    var texts = TimeSeriesDataTesting.VisibleTexts(properties);
                    CollectionAssert.IsSubsetOf(new[] { "PRIMARY (X) ORDINATES", "SECONDARY (Y) ORDINATES" }, texts, scenario.Cfa + ": the ordinates' sections");
                    ShowTab(properties, "Output");
                    Assert.AreEqual(scenario.Bins.ToString(CultureInfo.InvariantCulture), Property(properties, "Z Bins"), scenario.Cfa + ": the bins");
                    StringAssert.StartsWith(Property(properties, "Credible Interval"), scenario.Width, scenario.Cfa + ": the interval");
                    ShowTab(properties, "General");

                    // The response grid: the Y ordinates head the columns, the X ordinates the rows, every cell X + Y.
                    ShowTab(view, "Bivariate Response");
                    var grid = view.BivariateDataGrid;
                    CollectionAssert.AreEqual(y.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToList(), grid.Columns.Select(c => c.Header as string).ToList(), scenario.Cfa);
                    var cells = new List<double>();
                    for (int i = 0; i < x.Length; i++)
                    {
                        Assert.AreEqual(x[i].ToString(CultureInfo.InvariantCulture), grid.GetRow(i).Header as string, scenario.Cfa + ": row " + i);
                        for (int j = 0; j < y.Length; j++)
                        {
                            double cell = UsNumber(CellText(grid, i, j));
                            Assert.AreEqual(x[i] + y[j], cell, $"{scenario.Cfa}: cell {i}, {j}");
                            cells.Add(cell);
                        }
                    }
                    Assert.AreEqual(87, cells.Min());
                    Assert.AreEqual(273, cells.Max());

                    // The saved rows: AEP at a fixed response, with its probability bounds.
                    CopyPasteDataGrid table = Results(view);
                    Assert.AreEqual(scenario.Bins, table.Items.Count, scenario.Cfa + ": a row per response bin");
                    Assert.AreEqual("Response Value", table.Columns[0].Header as string);
                    var responses = Enumerable.Range(0, table.Items.Count).Select(k => UsNumber(CellText(table, k, 0))).ToList();
                    foreach (var row in Rows.Where(r => r.Cfa == scenario.Cfa))
                    {
                        int k = responses.FindIndex(z => Math.Round(z, 3) == UsNumber(row.Z));
                        Assert.IsTrue(k >= 0, $"{scenario.Cfa}: a row at Z = {row.Z}");
                        AssertRounded(UsNumber(row.Upper), CellText(table, k, 1), 7, $"{scenario.Cfa} at {row.Z}: the upper bound");
                        AssertRounded(UsNumber(row.Lower), CellText(table, k, 2), 7, $"{scenario.Cfa} at {row.Z}: the lower bound");
                        AssertRounded(UsNumber(row.Aep), CellText(table, k, 4), 7, $"{scenario.Cfa} at {row.Z}: the AEP");
                    }
                    var element = session.Element<CoincidentFrequencyAnalysis>(scenario.Cfa);
                    Assert.AreEqual(0, element.AnalysisResults.DIC, scenario.Cfa + ": the wrapper's zero DIC, not a measured score");
                    session.CloseDocuments();
                }
                session.AssertUnchanged(Scenarios.Select(s => s.Cfa).ToArray());
            });
        }

        /// <summary>The walkthrough's "Saved diagnostics" of the three copulas and six marginals.</summary>
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

        /// <summary>
        /// The Normal Copula - Rho = 0.0's X-Y Plot (Scatter Plot, Value) is RMC-BestFit's export of it
        /// (screenshots/sum-near-zero-scatter), and the negative and positive scenarios' scatters are the walkthrough's
        /// figures (images/sum-two-normals-*-scatter.plotspec.json.gz).
        /// </summary>
        [TestMethod]
        public void CopulaScatters_AreRmcBestFitsAndTheFigures()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (var (copula, native, figure) in new[]
                {
                    ("Normal Copula - Rho = 0.0", "screenshots/sum-near-zero-scatter.json.gz", (string)null),
                    ("Normal Copula - Rho = -0.5", null, "images/sum-two-normals-negative-scatter.plotspec.json.gz"),
                    ("Normal Copula - Rho =+0.5", null, "images/sum-two-normals-positive-scatter.plotspec.json.gz"),
                })
                {
                    var view = session.OpenDocument<BivariateAnalysisControl>(copula);
                    ShowTab(view, "Distribution Results");
                    ShowTab(view, "X-Y Plot");
                    Assert.AreEqual("Scatter Plot", ((ComboBoxItem)view.PlotTypeComboBox.SelectedItem).Content);
                    Assert.AreEqual("Value", ((ComboBoxItem)view.AxisTypeComboBox.SelectedItem).Content);
                    var plot = view.GetCurrentPlot();
                    if (native != null) NativeCaptures.AssertPlotMatches(plot, Data(Folder + native), 1E-9);
                    if (figure != null) AssertFigureData(plot, Data(Folder + figure), figure);
                    session.CloseDocuments();
                }
                session.AssertUnchanged(Scenarios.Select(s => s.Copula).ToArray());
            });
        }
    }
}
