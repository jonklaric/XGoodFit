using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughBayesianTesting;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTesting;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the bivariate distributions walkthrough
    /// (examples/5-bivariate-distribution-analysis/1-bivariate-distributions/bivariate-distribution-examples.md) followed
    /// through the main window: the twelve inputs, the twelve Normal marginals and six copulas with their saved parameters,
    /// the X-Y Plot's plot and axis types, the AMH parameter views, the saved diagnostics of all eighteen fits, the one-row
    /// joint results, and the figures. BivariateAnalysisViewTests compares the AMH exports and the Normal contour figures.
    /// See docs/walkthrough-coverage/time-series-input-bivariate.md.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughBivariateDistributionsTests
    {
        private const string Folder = "5-bivariate-distribution-analysis/1-bivariate-distributions/";
        private static readonly string[] Families = { "AMH", "Clayton", "Frank", "Gumbel", "Joe", "Normal" };
        private static readonly WalkthroughSession Session = new WalkthroughSession("bivariate-distribution-examples");

        /// <summary>The walkthrough's marginal fits: distribution, saved mean and SD.</summary>
        private static readonly (string Name, string Mean, string Sd)[] Marginals =
        {
            ("AMH - Marginal X", "99.98", "15.067"), ("AMH - Marginal Y", "79.977", "25.22"),
            ("Clayton - Marginal X", "99.976", "15.264"), ("Clayton - Marginal Y", "79.886", "25.237"),
            ("Frank - Marginal X", "100.018", "14.922"), ("Frank - Marginal Y", "79.801", "27.497"),
            ("Gumbel - Marginal X", "100.08", "15.061"), ("Gumbel - Marginal Y", "79.786", "25.141"),
            ("Joe - Marginal X", "100.029", "15.126"), ("Joe - Marginal Y", "79.788", "25.575"),
            ("Normal - Marginal X", "100.002", "15.083"), ("Normal - Marginal Y", "80.097", "25.394"),
        };

        /// <summary>The walkthrough's copulas: family, saved dependence parameter, marginals.</summary>
        private static readonly (string Name, string Family, string Theta)[] Copulas =
        {
            ("AMH Copula", "AliMikhailHaq", "0.813"), ("Clayton Copula", "Clayton", "1.602"), ("Frank Copula", "Frank", "8.958"),
            ("Gumbel Copula", "Gumbel", "1.922"), ("Joe Copula", "Joe", "3.001"), ("Normal Copula", "Normal", "0.785"),
        };

        /// <summary>The walkthrough's "Saved diagnostics".</summary>
        private static readonly Fit[] Fits =
        {
            new("AMH Copula", 4, 10, "Mean", "-12.336", "0.99990", 7475),
            new("Clayton Copula", 4, 10, "Mean", "-60.997", "1.00015", 9485),
            new("Frank Copula", 4, 10, "Mean", "-110.19", "1.00032", 9455),
            new("Gumbel Copula", 4, 10, "Mean", "-66.052", "1.00050", 9422),
            new("Joe Copula", 4, 10, "Mean", "-92.864", "0.99991", 9766),
            new("Normal Copula", 4, 10, "Mean", "-95.687", "1.00009", 9500),
            new("AMH - Marginal X", 6, 30, "Mean", "827.518", "1.00002", 9485),
            new("AMH - Marginal Y", 4, 20, "Mean", "930.65", "1.00022", 9234),
            new("Clayton - Marginal X", 4, 20, "Mean", "830.523", "1.00054", 9560),
            new("Clayton - Marginal Y", 4, 20, "Mean", "930.99", "1.00058", 9306),
            new("Frank - Marginal X", 4, 20, "Mean", "825.835", "1.00013", 9290),
            new("Frank - Marginal Y", 4, 20, "Mean", "947.903", "0.99992", 9551),
            new("Gumbel - Marginal X", 4, 20, "Mean", "827.792", "1.00039", 9388),
            new("Gumbel - Marginal Y", 4, 20, "Mean", "930.551", "1.00002", 9376),
            new("Joe - Marginal X", 4, 20, "Mean", "828.757", "1.00053", 9406),
            new("Joe - Marginal Y", 4, 20, "Mean", "933.421", "1.00050", 9370),
            new("Normal - Marginal X", 4, 20, "Mean", "827.918", "1.00016", 9364),
            new("Normal - Marginal Y", 4, 20, "Mean", "932.321", "1.00002", 9405),
        };

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Session.Close();

        /// <summary>
        /// "Identify the inputs": each family's X Data and Y Data have 100 exact observations indexed 1-100 in generic Value
        /// units, with no low outliers and no uncertain, interval or threshold data.
        /// </summary>
        [TestMethod]
        public void Inputs_Are100ExactValuesEach()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var names = Families.SelectMany(f => new[] { f + " - X Data", f + " - Y Data" }).ToArray();
                CollectionAssert.AreEquivalent(names, session.Project.ElementCollections.OfType<InputDataCollection>().Single().Select(e => e.Name).ToList());
                foreach (string name in names)
                {
                    var view = session.OpenDocument<InputDataControl>(name);
                    Assert.AreEqual("Value", Property(session.Properties, "Unit Label"), name);
                    var statistics = WalkthroughInputDataTesting.SummaryStatistics(view);
                    Assert.AreEqual("100", statistics["Record Length"], name);
                    Assert.AreEqual("0", statistics["Low Outliers"], name);
                    var indexes = WalkthroughInputDataTesting.Series(WalkthroughInputDataTesting.ShowPlot(view, "Chronology Plot"), "Exact Data").Select(p => p.X).ToList();
                    CollectionAssert.AreEqual(Enumerable.Range(1, 100).Select(i => (double)i).ToList(), indexes, name + ": indexed 1-100");
                    var element = session.Element<InputData>(name);
                    Assert.AreEqual(0, element.DataFrame.UncertainSeries.Count + element.DataFrame.IntervalSeries.Count + element.DataFrame.ThresholdSeries.Count, name + ": only exact data");
                    session.CloseDocuments();
                }
                session.AssertUnchanged(names);
            });
        }

        /// <summary>
        /// The walkthrough's marginal fits: twelve Normal fits, each on its family's X or Y data, with the saved mean and SD
        /// parameters the summary statistics show (at the walkthrough's rounding).
        /// </summary>
        [TestMethod]
        public void MarginalFits_AreNormal_WithTheSavedParameters()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (var (name, mean, sd) in Marginals)
                {
                    var view = session.OpenDocument<Control>(name);
                    Control properties = session.Properties;
                    Assert.AreEqual(name.Replace("Marginal ", "") .Replace(" - X", " - X Data").Replace(" - Y", " - Y Data"), Property(properties, "Input Data"), name + ": its input");
                    Assert.AreEqual("Normal", Property(properties, "Distribution"), name + ": the distribution");
                    AssertParameters(view, name, mean, sd);
                    session.CloseDocuments();
                }
                session.AssertUnchanged(Marginals.Select(m => m.Name).ToArray());
            });
        }

        /// <summary>
        /// The walkthrough's copulas: each family's analysis on its own two marginals, by inference from margins, with the
        /// saved dependence parameter its summary statistics show; and "each bivariate result table contains one saved XY
        /// query near probability one".
        /// </summary>
        [TestMethod]
        public void Copulas_HaveTheirFamiliesMarginalsAndParameters()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (var (name, family, theta) in Copulas)
                {
                    var view = session.OpenDocument<BivariateAnalysisControl>(name);
                    Control properties = session.Properties;
                    string prefix = name.Replace(" Copula", "");
                    Assert.AreEqual(prefix + " - Marginal X", Property(properties, "Marginal X"), name);
                    Assert.AreEqual(prefix + " - Marginal Y", Property(properties, "Marginal Y"), name);
                    var element = session.Element<BivariateAnalysis>(name);
                    Assert.AreEqual(family, element.BivariateDistribution.CopulaType.ToString(), name + ": the saved family");
                    Assert.AreEqual("InferenceFromMargins", element.BivariateDistribution.CopulaEstimationMethod.ToString(), name + ": the saved method");
                    AssertParameters(view, name, theta);

                    var results = Results(view);
                    Assert.AreEqual(1, results.Items.Count, name + ": one saved XY query");
                    double probability = UsNumber(CellText(results, 0, 5));
                    Assert.IsTrue(probability > 0.99 && probability <= 1, name + ": near probability one, " + CellText(results, 0, 5));
                    session.CloseDocuments();
                }
                session.AssertUnchanged(Copulas.Select(c => c.Name).ToArray());
            });
        }

        /// <summary>The walkthrough's "Saved diagnostics" of the six copulas and twelve marginals, as their MCMC Reports and summaries show them.</summary>
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
        /// "Work through the views", steps 2-3: the Normal Copula's X-Y Plot with Plot Type = Scatter Plot and Axis Type = Value
        /// shows the simulated points and the 100 observed pairs in value space; Probability puts them in marginal-CDF space
        /// (each axis between zero and one); the Density Contour Plot's labels are natural-log densities (negative here), and
        /// the Probability Contour Plot's are joint-exceedance probabilities.
        /// </summary>
        [TestMethod]
        public void XYPlot_PlotAndAxisTypes_AreTheWalkthroughs()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var view = session.OpenDocument<BivariateAnalysisControl>("Normal Copula");
                ShowTab(view, "Distribution Results");
                ShowTab(view, "X-Y Plot");
                Wpf.Plot plot = session.Element<BivariateAnalysis>("Normal Copula").CopulaPlot;
                Assert.AreSame(plot, view.GetCurrentPlot());
                Assert.AreEqual("Scatter Plot", ((ComboBoxItem)view.PlotTypeComboBox.SelectedItem).Content);
                Assert.AreEqual("Value", ((ComboBoxItem)view.AxisTypeComboBox.SelectedItem).Content);
                var simulated = Points(plot.Series.Single(s => s.Title == "Simulated Data").InternalSeries, "ActualPoints");
                var observed = Points(plot.Series.Single(s => s.Title == "Exact Data").InternalSeries, "ActualPoints");
                Assert.AreEqual(10000, simulated.Count, "the simulated points");
                Assert.AreEqual(100, observed.Count, "the observed pairs");
                Assert.IsTrue(observed.Max(p => p.X) > 1, "value space");

                Choose(view.AxisTypeComboBox, "Probability");
                observed = Points(plot.Series.Single(s => s.Title == "Exact Data").InternalSeries, "ActualPoints");
                Assert.IsTrue(observed.All(p => p.X > 0 && p.X < 1 && p.Y > 0 && p.Y < 1), "marginal-CDF space");
                Choose(view.AxisTypeComboBox, "Value");

                Choose(view.PlotTypeComboBox, "Density Contour Plot");
                var density = plot.Series.OfType<Wpf.ContourSeries>().Single();
                Assert.IsTrue(((OxyPlot.Series.ContourSeries)density.InternalSeries).ContourLevels.All(l => l < 0), "natural-log densities, negative here");
                Choose(view.PlotTypeComboBox, "Probability Contour Plot");
                var exceedance = plot.Series.OfType<Wpf.ContourSeries>().Single();
                Assert.IsTrue(((OxyPlot.Series.ContourSeries)exceedance.InternalSeries).ContourLevels.All(l => l > 0 && l < 1), "joint-exceedance probabilities");
                Choose(view.PlotTypeComboBox, "Scatter Plot");
                session.CloseDocuments();
            });
        }

        /// <summary>
        /// The AMH Copula's parameter views: Kernel Density of Dependency (θ), its saved Markov Chain Traces, and the MCMC
        /// Report's parameter summary (the exports of the first two are BivariateAnalysisViewTests').
        /// </summary>
        [TestMethod]
        public void AmhParameterViews_ShowTheDependency()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                var view = session.OpenDocument<BivariateAnalysisControl>("AMH Copula");
                ShowTab(view, "Kernel Density");
                CollectionAssert.Contains(view.KernelDensityControl.Plot.Axes.Select(a => a.Title).ToList(), "Dependency (θ)", "the posterior density of Dependency (θ)");
                ShowTab(view, "Markov Chain Traces");
                Assert.AreEqual(4, view.MarkovChainTraceControl.Plot.Series.Count, "the four saved chains");
                StringAssert.Contains(Report(view), "Dependency (θ)", "the report's parameter summary");
                session.CloseDocuments();
            });
        }

        /// <summary>
        /// "Compare the figures": each family's marginal-CDF scatter (Axis Type = Probability) and the Normal dependence
        /// parameter's chains are what the views show. The Normal contour figures and the AMH exports are
        /// BivariateAnalysisViewTests'.
        /// </summary>
        [TestMethod]
        public void Figures_AreWhatTheViewsShow()
        {
            HeadlessSession.Run(() =>
            {
                WalkthroughSession session = Session.Open();
                foreach (string family in Families)
                {
                    JsonElement figure = Data(Folder + "images/bivariate-distribution-examples-" + family.ToLowerInvariant() + "-cdf.plotspec.json.gz");
                    var view = session.OpenDocument<BivariateAnalysisControl>(family + " Copula");
                    ShowTab(view, "Distribution Results");
                    ShowTab(view, "X-Y Plot");
                    Choose(view.AxisTypeComboBox, "Probability");
                    AssertFigureData(view.GetCurrentPlot(), figure, family + "-cdf");
                    Choose(view.AxisTypeComboBox, "Value");
                    session.CloseDocuments();
                }
                var normal = session.OpenDocument<BivariateAnalysisControl>("Normal Copula");
                ShowTab(normal, "Markov Chain Traces");
                AssertFigureData(normal.MarkovChainTraceControl.Plot, Data(Folder + "images/bivariate-distribution-examples-normal-trace.plotspec.json.gz"), "normal-trace");
                session.CloseDocuments();
                session.AssertUnchanged(Copulas.Select(c => c.Name).ToArray());
            });
        }

        // Picks a combo box item by its text, as a user does.
        private static void Choose(ComboBox box, string text)
        {
            box.SelectedItem = box.Items.OfType<ComboBoxItem>().Single(i => i.Content as string == text);
            BestFitShell.Frame();
        }
    }
}
