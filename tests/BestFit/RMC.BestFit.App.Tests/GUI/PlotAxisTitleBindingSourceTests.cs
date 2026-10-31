using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace RMC.BestFit.App.Tests.GUI
{
    /// <summary>
    /// Source-level regression tests for data-label-bound plot axis title wiring.
    /// </summary>
    /// <remarks>
    /// XGoodFit port of upstream's GUI/PlotAxisTitleBindingSourceTests.cs: the audits read the ports,
    /// src/BestFit/RMC.BestFit.App/GUI/**/*.axaml.cs (upstream's were .xaml.cs under src/RMC.BestFit.App). Run unchanged,
    /// upstream's search for the sources finds upstream/RMC-BestFit/src/RMC.BestFit.App first and would audit
    /// upstream's files. Besides WPF's binding calls, the direct-binding check also looks for Avalonia's: a property
    /// is bound with Bind or the binding indexer ([!Property]) where WPF used BindingOperations.SetBinding, and a
    /// binding goes by disposing of its expression (or ClearValue) where WPF used BindingOperations.ClearBinding (as
    /// the port's PlotAxisTitleDefaults does). The other checks, and the residual plot's lines, are upstream's.
    /// <para>
    /// Constructing every WPF plot control requires the full App resource graph, so these tests
    /// pin the scoped binding policy in source: data/time-series label axes use
    /// PlotAxisTitleDefaults, while Bayesian selector-driven controls remain outside this helper.
    /// </para>
    /// </remarks>
    [TestClass]
    public class PlotAxisTitleBindingSourceTests
    {
        private static readonly string[] ScopedAxisTitleFiles =
        {
            Path.Combine("GUI", "FittingAnalysis", "FittingAnalysisControl.axaml.cs"),
            Path.Combine("GUI", "UnivariateAnalysis", "Univariate", "UnivariateAnalysisControl.axaml.cs"),
            Path.Combine("GUI", "UnivariateAnalysis", "B17C", "B17CAnalysisControl.axaml.cs"),
            Path.Combine("GUI", "UnivariateAnalysis", "PointProcess", "PointProcessAnalysisControl.axaml.cs"),
            Path.Combine("GUI", "UnivariateAnalysis", "Mixture", "MixtureAnalysisControl.axaml.cs"),
            Path.Combine("GUI", "UnivariateAnalysis", "Composite", "CompositeAnalysisControl.axaml.cs"),
            Path.Combine("GUI", "BivariateAnalysis", "Bivariate", "BivariateAnalysisControl.axaml.cs"),
            Path.Combine("GUI", "BivariateAnalysis", "CoincidentFrequency", "CoincidentFrequencyControl.axaml.cs"),
            Path.Combine("GUI", "RatingCurveAnalysis", "RatingCurveAnalysisControl.axaml.cs"),
            Path.Combine("GUI", "TimeSeriesAnalysis", "TimeSeriesAnalysisControl.axaml.cs"),
            Path.Combine("GUI", "TimeSeriesData", "TimeSeries", "TimeSeriesControl.axaml.cs")
        };

        private static readonly string[] BayesianSelectorDrivenFiles =
        {
            Path.Combine("GUI", "Support", "Controls", "HistogramControl.axaml.cs"),
            Path.Combine("GUI", "Support", "Controls", "KernelDensityControl.axaml.cs"),
            Path.Combine("GUI", "Support", "Controls", "MarkovChainTraceControl.axaml.cs"),
            Path.Combine("GUI", "Support", "Controls", "BivariateHeatMapControl.axaml.cs"),
            Path.Combine("GUI", "Support", "Controls", "InfluenceDiagnosticsControl.axaml.cs")
        };

        /// <summary>
        /// XGoodFit port: Avalonia's ways of binding a property (WPF's BindingOperations.SetBinding), and of removing a
        /// title's binding (WPF's BindingOperations.ClearBinding).
        /// </summary>
        private static readonly Regex AvaloniaDirectBinding = new Regex(
            @"\.Bind\(|\[!|\b(?:GetBindingExpressionBase|ClearValue)\([^;]*\bTitleProperty\b",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// Verifies every scoped App control routes data-label axis title updates through the helper.
        /// </summary>
        [TestMethod]
        public void ScopedDataLabelAxisTitleControls_UseDefaultAwareHelper()
        {
            var failures = new List<string>();

            foreach (string relativePath in ScopedAxisTitleFiles)
            {
                string source = ReadAppSource(relativePath);
                if (!source.Contains("PlotAxisTitleDefaults.", StringComparison.Ordinal))
                {
                    failures.Add(relativePath + " does not use PlotAxisTitleDefaults.");
                }
            }

            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// Verifies scoped App controls do not bypass the helper with direct axis-title bindings.
        /// </summary>
        [TestMethod]
        public void ScopedDataLabelAxisTitleControls_DoNotBindAxisTitlePropertyDirectly()
        {
            var failures = new List<string>();

            foreach (string relativePath in ScopedAxisTitleFiles)
            {
                string source = ReadAppSource(relativePath);
                if (source.Contains("BindingOperations.SetBinding", StringComparison.Ordinal)
                    || source.Contains("BindingOperations.ClearBinding", StringComparison.Ordinal)
                    || source.Contains("Axis.TitleProperty", StringComparison.Ordinal)
                    || source.Contains("OxyPlot.Wpf.Axis.TitleProperty", StringComparison.Ordinal)
                    || AvaloniaDirectBinding.IsMatch(source))
                {
                    failures.Add(relativePath + " still binds or clears Axis.TitleProperty directly.");
                }
            }

            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// Verifies the time-series residual plot binds its fitted-value axis to the selected
        /// time-series unit label.
        /// </summary>
        [TestMethod]
        public void TimeSeriesAnalysisResidualPlot_BindsXAxisToTimeSeriesUnitLabel()
        {
            string source = ReadAppSource(Path.Combine("GUI", "TimeSeriesAnalysis", "TimeSeriesAnalysisControl.axaml.cs"));

            StringAssert.Contains(source, "var residualXAxis = ResidualPlot?.Axes.FirstOrDefault(a => a.Key == \"Xaxis\");");
            StringAssert.Contains(source, "PlotAxisTitleDefaults.BindTitleIfDefault(residualXAxis, Element.TimeSeriesData, nameof(TimeSeriesElement.UnitLabel), Element.TimeSeriesData.UnitLabel);");
            StringAssert.Contains(source, "PlotAxisTitleDefaults.SetTitleIfDefault(residualYAxis, \"Residual\");");
        }

        /// <summary>
        /// Verifies Bayesian selector-driven plot controls remain outside the data-label helper scope.
        /// </summary>
        [TestMethod]
        public void BayesianSelectorDrivenControls_DoNotUseDataLabelAxisTitleHelper()
        {
            var failures = new List<string>();

            foreach (string relativePath in BayesianSelectorDrivenFiles)
            {
                string source = ReadAppSource(relativePath);
                if (source.Contains("PlotAxisTitleDefaults", StringComparison.Ordinal))
                {
                    failures.Add(relativePath + " should keep selector-driven axis-title behavior.");
                }
            }

            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// Reads an App source file using a byte-preserving single-byte decoding.
        /// </summary>
        /// <param name="relativePath">The source path relative to the App project root.</param>
        /// <param name="sourceFilePath">The compiler-provided source path for this test file.</param>
        /// <returns>The source file text.</returns>
        private static string ReadAppSource(string relativePath, [CallerFilePath] string sourceFilePath = "")
        {
            string path = Path.Combine(FindRepoRoot(sourceFilePath), "src", "BestFit", "RMC.BestFit.App", relativePath);
            return Encoding.Latin1.GetString(File.ReadAllBytes(path));
        }

        /// <summary>
        /// Finds the repository root from the test output directory.
        /// </summary>
        /// <param name="sourceFilePath">The compiler-provided source path for this test file.</param>
        /// <returns>The absolute repository root.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the source tree cannot be found.</exception>
        private static string FindRepoRoot(string sourceFilePath)
        {
            string[] searchRoots =
            {
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory(),
                Path.GetDirectoryName(sourceFilePath)
            };

            foreach (string searchRoot in searchRoots)
            {
                if (string.IsNullOrEmpty(searchRoot)) continue;
                DirectoryInfo directory = new DirectoryInfo(searchRoot);
                while (directory != null)
                {
                    string candidate = Path.Combine(directory.FullName, "src", "BestFit", "RMC.BestFit.App");
                    if (Directory.Exists(candidate))
                    {
                        return directory.FullName;
                    }

                    directory = directory.Parent;
                }
            }

            throw new DirectoryNotFoundException("Could not locate repository root from the test output directory.");
        }
    }
}
