using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.VisualTree;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC_BestFit;
using WpfCompat.Documents;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Bayesian fits of the bivariate walkthroughs (examples/5-bivariate-distribution-analysis/*/*.md)
    /// as the main window shows them: an analysis's Tabular Results (its frequency or joint results, and its summary
    /// statistics), its MCMC Report, and the walkthroughs' "Saved diagnostics" tables checked against them.
    /// </summary>
    internal static class WalkthroughBayesianTesting
    {
        /// <summary>The MCMC settings every fit of these walkthroughs retains ("DEMCzs, seed 12345, 1,750 warmup ...").</summary>
        internal static readonly (string Label, string Value)[] Settings =
        {
            ("Sampler:", "DE-MCzs"), ("PRNG Seed:", "12345"), ("Warmup Iterations:", "1,750"), ("Iterations (thinned):", "3,500"),
            ("Output Length:", "10,000"), ("Credible Interval:", "90%"),
        };

        /// <summary>A row of a walkthrough's "Saved diagnostics" table.</summary>
        /// <param name="Name">The fit (an analysis).</param>
        /// <param name="Chains">Its chains.</param>
        /// <param name="Thinning">Its thinning interval.</param>
        /// <param name="Point">Its point parameters: Mean or Mode.</param>
        /// <param name="Dic">Its DIC, as the walkthrough writes it.</param>
        /// <param name="MaxRhat">Its largest R-hat, as the walkthrough writes it (five decimals).</param>
        /// <param name="MinEss">Its smallest ESS.</param>
        internal sealed record Fit(string Name, int Chains, int Thinning, string Point, string Dic, string MaxRhat, int MinEss);

        /// <summary>Selects a tab of a view or panel by its header, among the tabs it shows.</summary>
        internal static void ShowTab(Control root, string header)
        {
            BestFitShell.Frame();
            TabItem tab = root.GetVisualDescendants().OfType<TabItem>().FirstOrDefault(t => t.Header as string == header && t.IsVisible);
            Assert.IsNotNull(tab, "a tab headed " + header);
            Select(tab);
        }

        /// <summary>
        /// Shows an analysis view's Tabular Results: the Distribution Results tab's inner tab, or (a coincident frequency
        /// analysis's) a tab of the view's own.
        /// </summary>
        internal static void ShowTabularResults(Control view)
        {
            BestFitShell.Frame();
            if (view.GetVisualDescendants().OfType<TabItem>().Any(t => t.Header as string == "Distribution Results" && t.IsVisible))
                ShowTab(view, "Distribution Results");
            ShowTab(view, "Tabular Results");
        }

        /// <summary>The text an analysis view's MCMC Report shows.</summary>
        internal static string Report(Control view)
        {
            ShowTab(view, "MCMC Report");
            var report = view.FindControl<MCMCReportControl>("MCMCReportControl");
            var paragraph = report.ReportTextBox.Document.Blocks.OfType<Paragraph>().Single();
            string text = ((Run)paragraph.Inlines.Single()).Text;
            ShowTab(view, "Distribution Results");
            return text;
        }

        /// <summary>A report line's value: the text after its label.</summary>
        internal static string ReportValue(string report, string label)
        {
            Match match = Regex.Match(report, "^\\s*" + Regex.Escape(label) + "\\s*(.*?)\\s*$", RegexOptions.Multiline);
            Assert.IsTrue(match.Success, "the report has a line " + label);
            return match.Groups[1].Value;
        }

        /// <summary>The Tabular Results' summary statistics an analysis view shows, by measure, and their column's header.</summary>
        internal static (Dictionary<string, string> Rows, List<string> Names, string Header) Summary(Control view)
        {
            ShowTabularResults(view);
            var table = view.FindControl<CopyPasteDataGrid>("SummaryStatisticsTable");
            var names = Enumerable.Range(0, table.Items.Count).Select(i => CellText(table, i, 0)).ToList();
            return (Statistics(table), names, table.Columns[1].Header as string);
        }

        /// <summary>The Tabular Results' frequency (or joint) results table an analysis view shows.</summary>
        internal static CopyPasteDataGrid Results(Control view)
        {
            ShowTabularResults(view);
            return view.FindControl<CopyPasteDataGrid>("FrequencyCurveTable");
        }

        /// <summary>
        /// Opens a fit's document and checks its row of a walkthrough's "Saved diagnostics" table, as its views show it: the
        /// MCMC Report's chains, thinning interval, the retained settings, largest R-hat and smallest ESS, and the Tabular
        /// Results' point estimator (the summary's column) and DIC.
        /// </summary>
        internal static void AssertFit(WalkthroughSession session, Fit fit)
        {
            Control view = session.OpenDocument<Control>(fit.Name);
            try
            {
                string report = Report(view);
                Assert.AreEqual(fit.Chains.ToString(CultureInfo.InvariantCulture), ReportValue(report, "Number of Chains:"), fit.Name + ": chains");
                Assert.AreEqual(fit.Thinning.ToString(CultureInfo.InvariantCulture), ReportValue(report, "Thinning Interval:"), fit.Name + ": thinning");
                foreach (var (label, value) in Settings)
                    Assert.AreEqual(value, ReportValue(report, label), fit.Name + ": " + label);
                string rhat = Regex.Match(ReportValue(report, "Max R-hat:"), "^[0-9.]+").Value;
                AssertRounded(UsNumber(fit.MaxRhat), rhat, 5, fit.Name + ": the largest R-hat");
                string ess = Regex.Match(ReportValue(report, "Min ESS:"), "^[0-9,]+").Value;
                Assert.AreEqual(fit.MinEss.ToString("N0", Us), ess, fit.Name + ": the smallest ESS");

                // DIC: the report's (two decimals), and a bivariate fit's summary statistics (four).
                int decimals = fit.Dic.Contains('.') ? fit.Dic.Length - fit.Dic.IndexOf('.') - 1 : 0;
                AssertRounded(UsNumber(fit.Dic), ReportValue(report, "DIC:"), decimals, fit.Name + ": the report's DIC");
                var (rows, _, header) = Summary(view);
                Assert.AreEqual("Posterior " + fit.Point, header, fit.Name + ": the point parameters");
                if (rows.TryGetValue("DIC", out string dic))
                    AssertRounded(UsNumber(fit.Dic), dic, decimals, fit.Name + ": DIC");
            }
            finally
            {
                session.CloseDocuments();
            }
        }

        /// <summary>
        /// The parameters a fit's summary statistics show (the rows above a copula's AIC, or a univariate distribution's
        /// Minimum), at the walkthrough's rounding: a marginal's mean and standard deviation, a copula's dependence.
        /// </summary>
        internal static void AssertParameters(Control view, string name, params string[] walkthrough)
        {
            var (rows, names, _) = Summary(view);
            int aic = names.FindIndex(n => n is "AIC" or "Minimum");
            Assert.AreEqual(walkthrough.Length, aic, name + ": the parameters, " + string.Join(", ", names.Take(Math.Max(aic, 0))));
            for (int i = 0; i < walkthrough.Length; i++)
            {
                int decimals = walkthrough[i].Contains('.') ? walkthrough[i].Length - walkthrough[i].IndexOf('.') - 1 : 0;
                AssertRounded(UsNumber(walkthrough[i]), rows[names[i]], decimals, name + ": " + names[i]);
            }
        }
    }
}
