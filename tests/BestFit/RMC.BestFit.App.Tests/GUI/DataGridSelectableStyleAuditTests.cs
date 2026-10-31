using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RMC.BestFit.App.Tests.GUI
{
    /// <summary>
    /// Source-level regression tests for App data grid cell style inheritance.
    /// </summary>
    /// <remarks>
    /// XGoodFit port of upstream's GUI/DataGridSelectableStyleAuditTests.cs: the audits read the port,
    /// src/BestFit/RMC.BestFit.App (upstream read src/RMC.BestFit.App, which, run unchanged, would find upstream's own
    /// files under upstream/). Its markup is .axaml, and its C# is the folder's files with the upstream files the App
    /// project compiles unchanged (its UpstreamCompileInclude items). Avalonia's grids take control themes where WPF's
    /// took styles (docs/porting-guide.md): a cell style is a ControlTheme (TargetType="DataGridCell"), a grid's RowStyle
    /// is its RowTheme, and code builds <c>new ControlTheme(typeof(DataGridCell)) { BasedOn = … }</c> where WPF built
    /// <c>new Style(typeof(DataGridCell), basedOn)</c>; the resource keys are upstream's. Each check looks for the WPF
    /// form too. One grid is left out of the row check, as upstream's check left it out: the batch run window's, a
    /// plain WPF DataGrid upstream, which the port shows with a CopyPasteDataGrid themed as a plain grid
    /// (DataGridStyle), its row theme based on the plain row's (see <see cref="PlainGridRowThemes"/>).
    /// </remarks>
    [TestClass]
    public class DataGridSelectableStyleAuditTests
    {
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        private static readonly HashSet<string> SelectableCellStyleBases = new HashSet<string>(StringComparer.Ordinal)
        {
            "{StaticResource Left_CellStyle}",
            "{StaticResource Center_CellStyle}",
            "{StaticResource Right_CellStyle}",
            "{StaticResource Center_ReadOnly_CellStyle}",
            "{StaticResource Right_ReadOnly_CellStyle}",
            "{StaticResource CopyPasteDataGridCellStyle}",
            "{StaticResource ValidationDataGridCellStyle}"
        };

        /// <summary>
        /// XGoodFit port: the CopyPasteDataGrids that are upstream's plain WPF DataGrids (file and grid name), which the
        /// row check leaves out as upstream's did; they are themed as plain grids (DataGridStyle).
        /// </summary>
        private static readonly string[] PlainGridRowThemes =
        {
            "src/BestFit/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.axaml: MyDataGrid"
        };

        /// <summary>
        /// Verifies every App-owned DataGridCell style inherits a selectable framework cell style.
        /// </summary>
        [TestMethod]
        public void AppDataGridCellStyles_InheritSelectableCellStyle()
        {
            var failures = new List<string>();
            int styleCount = 0;

            foreach (string xamlFile in EnumerateAppXamlFiles())
            {
                XDocument document = XDocument.Load(xamlFile);
                foreach (XElement style in document.Descendants().Where(IsDataGridCellStyle))
                {
                    styleCount++;
                    string basedOn = (string)style.Attribute("BasedOn") ?? string.Empty;
                    if (!SelectableCellStyleBases.Contains(basedOn))
                    {
                        failures.Add(RelativePath(xamlFile) + ": DataGridCell style uses BasedOn='" + basedOn + "'.");
                    }
                }
            }

            // XGoodFit port: the audit sees the port's cell themes (upstream's 28 cell styles are all ported).
            Assert.IsTrue(styleCount >= 28, "The audit found only " + styleCount + " DataGridCell themes.");
            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// Verifies App XAML does not use the WPF implicit DataGridCell style as a base.
        /// </summary>
        [TestMethod]
        public void AppXaml_DoesNotUseImplicitDataGridCellBaseStyle()
        {
            var failures = EnumerateAppXamlFiles()
                .Where(file => File.ReadAllText(file).Contains("BasedOn=\"{StaticResource {x:Type DataGridCell}}\"", StringComparison.Ordinal))
                .Select(RelativePath)
                .ToList();

            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// Verifies local CopyPasteDataGrid row styles preserve themed row selection behavior.
        /// </summary>
        [TestMethod]
        public void CopyPasteDataGridRowStyles_InheritThemedRowStyle()
        {
            var failures = new List<string>();
            var plainGrids = new List<string>();
            int rowStyleCount = 0;

            foreach (string xamlFile in EnumerateAppXamlFiles())
            {
                XDocument document = XDocument.Load(xamlFile);
                foreach (XElement rowStyle in document.Descendants().Where(element =>
                    element.Name.LocalName == "CopyPasteDataGrid.RowStyle" || element.Name.LocalName == "CopyPasteDataGrid.RowTheme"))
                {
                    // XGoodFit port: a grid themed as a plain DataGrid is upstream's plain DataGrid (see PlainGridRowThemes).
                    XElement grid = rowStyle.Parent;
                    if (string.Equals((string)grid?.Attribute("Theme"), "{DynamicResource DataGridStyle}", StringComparison.Ordinal))
                    {
                        plainGrids.Add(RelativePath(xamlFile).Replace('\\', '/') + ": " + (string)grid.Attribute(XamlNamespace + "Name"));
                        continue;
                    }

                    rowStyleCount++;
                    XElement style = rowStyle.Elements().FirstOrDefault(element => element.Name.LocalName == "Style" || element.Name.LocalName == "ControlTheme");
                    string basedOn = (string)style?.Attribute("BasedOn") ?? string.Empty;
                    if (!string.Equals(basedOn, "{StaticResource CopyPasteDataGridRowStyle}", StringComparison.Ordinal))
                    {
                        failures.Add(RelativePath(xamlFile) + ": CopyPasteDataGrid.RowStyle uses BasedOn='" + basedOn + "'.");
                    }
                }
            }

            CollectionAssert.AreEqual(PlainGridRowThemes, plainGrids, "Only upstream's plain DataGrids may be left out of the row check.");
            Assert.IsTrue(rowStyleCount >= 1, "The audit found no CopyPasteDataGrid row themes.");
            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// Verifies local KDE summary grid cell styles inherit the centered selectable cell style.
        /// </summary>
        [TestMethod]
        public void KdeCellStyles_InheritCenterCellStyle()
        {
            var failures = new List<string>();
            int styleCount = 0;

            foreach (string xamlFile in EnumerateAppXamlFiles())
            {
                XDocument document = XDocument.Load(xamlFile);
                foreach (XElement style in document.Descendants().Where(IsKdeCellStyle))
                {
                    styleCount++;
                    string basedOn = (string)style.Attribute("BasedOn") ?? string.Empty;
                    if (!string.Equals(basedOn, "{StaticResource Center_CellStyle}", StringComparison.Ordinal))
                    {
                        failures.Add(RelativePath(xamlFile) + ": KDECellStyle uses BasedOn='" + basedOn + "'.");
                    }
                }
            }

            Assert.AreEqual(3, styleCount);
            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// Verifies FittingAnalysis tooltip-heavy summary columns inherit the right-aligned selectable cell style.
        /// </summary>
        [TestMethod]
        public void FittingAnalysisTooltipCellStyles_InheritSelectableCellStyles()
        {
            string xaml = File.ReadAllText(Path.Combine(AppRoot(), "GUI", "FittingAnalysis", "FittingAnalysisControl.axaml"));
            string summaryGrid = ExtractXamlElement(xaml, "<cntrls:CopyPasteDataGrid Name=\"SummaryStatisticsDataGrid\"", "</cntrls:CopyPasteDataGrid>");
            string aicBicGrid = ExtractXamlElement(xaml, "<cntrls:CopyPasteDataGrid x:Name=\"AICBICTable\"", "</cntrls:CopyPasteDataGrid>");

            Assert.AreEqual(15, CountOccurrences(summaryGrid, "BasedOn=\"{StaticResource Right_CellStyle}\""));
            Assert.AreEqual(1, CountOccurrences(aicBicGrid, "BasedOn=\"{StaticResource Left_CellStyle}\""));
            StringAssert.Contains(aicBicGrid, "BasedOn=\"{StaticResource CopyPasteDataGridRowStyle}\"");
        }

        /// <summary>
        /// Verifies the composite weight validation style keeps the themed right-aligned base style.
        /// </summary>
        /// <remarks>
        /// XGoodFit port: the weight cells' theme is based on RightCellTheme(), which finds Right_CellStyle as WPF's
        /// FindResource found it (from the panel, else the application).
        /// </remarks>
        [TestMethod]
        public void CompositeWeightColumnStyle_InheritsRightCellStyle()
        {
            string source = File.ReadAllText(Path.Combine(AppRoot(), "GUI", "UnivariateAnalysis", "Composite", "CompositeAnalysisPropertiesControl.axaml.cs"));

            StringAssert.Contains(source, "new ControlTheme(typeof(DataGridCell)) { BasedOn = RightCellTheme() }");
            StringAssert.Contains(source, "(this.TryFindResource(\"Right_CellStyle\") ?? Application.Current?.TryFindResource(\"Right_CellStyle\")) as ControlTheme");
            Assert.IsFalse(source.Contains("var weightStyle = new Style();", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("var weightStyle = new ControlTheme();", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("WeightColumn.CellStyle.Setters", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("WeightColumn.CellTheme.Setters", StringComparison.Ordinal));
        }

        /// <summary>
        /// Verifies code-behind does not construct unbased DataGridCell styles.
        /// </summary>
        [TestMethod]
        public void AppCodeBehind_DoesNotCreateUnbasedDataGridCellStyles()
        {
            var failures = new List<string>();
            // XGoodFit port: a cell theme built without a BasedOn initializer.
            var unbasedCellTheme = new Regex(@"new\s+ControlTheme\s*\(\s*typeof\s*\(\s*DataGridCell\s*\)\s*\)(?!\s*\{\s*BasedOn\s*=)", RegexOptions.CultureInvariant);

            foreach (string sourceFile in EnumerateAppSourceFiles())
            {
                string source = File.ReadAllText(sourceFile);
                if (source.Contains("new Style(typeof(DataGridCell))", StringComparison.Ordinal) || unbasedCellTheme.IsMatch(source))
                {
                    failures.Add(RelativePath(sourceFile) + ": creates an unbased DataGridCell style.");
                }

                if ((source.Contains("new Style();", StringComparison.Ordinal) || source.Contains("new ControlTheme();", StringComparison.Ordinal))
                    && (source.Contains("CellStyle", StringComparison.Ordinal) || source.Contains("CellTheme", StringComparison.Ordinal)))
                {
                    failures.Add(RelativePath(sourceFile) + ": creates a parameterless style in a file that assigns cell styles.");
                }
            }

            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// Determines whether an element is a DataGridCell style declaration.
        /// </summary>
        /// <param name="element">The element to inspect.</param>
        /// <returns><c>true</c> when the element is a DataGridCell style.</returns>
        /// <remarks>XGoodFit port: a ControlTheme (or a WPF Style).</remarks>
        private static bool IsDataGridCellStyle(XElement element)
        {
            if (element.Name.LocalName != "Style" && element.Name.LocalName != "ControlTheme") return false;

            string targetType = (string)element.Attribute("TargetType") ?? string.Empty;
            return string.Equals(targetType, "DataGridCell", StringComparison.Ordinal)
                || string.Equals(targetType, "{x:Type DataGridCell}", StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether an element is the KDE cell style declaration.
        /// </summary>
        /// <param name="element">The element to inspect.</param>
        /// <returns><c>true</c> when the element is the KDE cell style.</returns>
        /// <remarks>XGoodFit port: a ControlTheme (or a WPF Style).</remarks>
        private static bool IsKdeCellStyle(XElement element)
        {
            return (element.Name.LocalName == "Style" || element.Name.LocalName == "ControlTheme")
                && string.Equals((string)element.Attribute(XamlNamespace + "Key"), "KDECellStyle", StringComparison.Ordinal);
        }

        /// <summary>
        /// Finds App XAML files in the source tree.
        /// </summary>
        /// <returns>The App XAML file paths.</returns>
        private static IEnumerable<string> EnumerateAppXamlFiles()
        {
            return Directory.EnumerateFiles(AppRoot(), "*.axaml", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds App C# source files in the source tree.
        /// </summary>
        /// <returns>The App source file paths.</returns>
        /// <remarks>
        /// XGoodFit port: the port's files, and the upstream files the App project compiles unchanged.
        /// </remarks>
        private static IEnumerable<string> EnumerateAppSourceFiles()
        {
            string appRoot = AppRoot();
            string upstreamRoot = Path.Combine(FindRepoRoot(), "upstream", "RMC-BestFit", "src", "RMC.BestFit.App");
            IEnumerable<string> upstreamFiles = XDocument.Load(Path.Combine(appRoot, "RMC.BestFit.App.csproj"))
                .Descendants().Where(element => element.Name.LocalName == "UpstreamCompileInclude")
                .Select(element => Path.Combine(upstreamRoot, ((string)element.Attribute("Include")).Replace('/', Path.DirectorySeparatorChar)))
                .ToList();
            Assert.IsTrue(upstreamFiles.All(File.Exists), "Every upstream file the App compiles exists.");
            return Directory.EnumerateFiles(appRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Concat(upstreamFiles);
        }

        /// <summary>
        /// Counts non-overlapping occurrences of a pattern.
        /// </summary>
        /// <param name="source">The source text to inspect.</param>
        /// <param name="pattern">The pattern to count.</param>
        /// <returns>The number of occurrences.</returns>
        private static int CountOccurrences(string source, string pattern)
        {
            return source
                .Split(new[] { pattern }, StringSplitOptions.None)
                .Length - 1;
        }

        /// <summary>
        /// Extracts a XAML element from the source text.
        /// </summary>
        /// <param name="source">The XAML source text.</param>
        /// <param name="startToken">The opening token for the element.</param>
        /// <param name="endToken">The closing token for the element.</param>
        /// <returns>The element source including its opening and closing tags.</returns>
        private static string ExtractXamlElement(string source, string startToken, string endToken)
        {
            int start = source.IndexOf(startToken, StringComparison.Ordinal);
            Assert.IsTrue(start >= 0, startToken + " should exist.");

            int end = source.IndexOf(endToken, start, StringComparison.Ordinal);
            Assert.IsTrue(end > start, endToken + " should exist.");

            return source.Substring(start, end - start + endToken.Length);
        }

        /// <summary>
        /// Converts an absolute path to a repository-relative path.
        /// </summary>
        /// <param name="path">The absolute path.</param>
        /// <returns>The repository-relative path.</returns>
        private static string RelativePath(string path)
        {
            return Path.GetRelativePath(FindRepoRoot(), path);
        }

        /// <summary>XGoodFit port: the ported App project's folder, src/BestFit/RMC.BestFit.App.</summary>
        /// <returns>The folder's absolute path.</returns>
        private static string AppRoot() => Path.Combine(FindRepoRoot(), "src", "BestFit", "RMC.BestFit.App");

        /// <summary>
        /// Finds the repository root from the test output directory.
        /// </summary>
        /// <param name="sourceFilePath">The compiler-provided source path for this test file.</param>
        /// <returns>The absolute repository root.</returns>
        private static string FindRepoRoot([CallerFilePath] string sourceFilePath = "")
        {
            string[] searchRoots = new[]
            {
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory(),
                Path.GetDirectoryName(sourceFilePath)
            };

            foreach (string searchRoot in searchRoots.Where(root => !string.IsNullOrEmpty(root)))
            {
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
