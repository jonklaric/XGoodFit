using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlot;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: compares the Univariate and Bulletin 17C analysis views' plots with what RMC-BestFit drew
    /// for the univariate examples (4-univariate-distribution-analysis): the native exports beside the examples
    /// (screenshots/*.json.gz) and the walkthroughs' figures (images/*.plotspec.json.gz, drawn from the desktop's
    /// coordinates).
    /// </summary>
    internal static class UnivariateAnalysisTesting
    {
        /// <summary>The univariate examples folder, under the examples.</summary>
        internal const string Folder = "4-univariate-distribution-analysis/";

        /// <summary>The native exports of an example project's plots: screenshots/*.json.gz beside it, made from it.</summary>
        /// <param name="example">The example's path under the examples folder.</param>
        /// <returns>The exports, by file name.</returns>
        internal static IEnumerable<(string Name, JsonElement Capture)> NativeExports(string example)
        {
            string path = SharedControlsTesting.Example(example);
            string folder = Path.Combine(Path.GetDirectoryName(path), "screenshots");
            foreach (string file in Directory.GetFiles(folder, "*.json.gz").OrderBy(f => f, StringComparer.Ordinal))
            {
                JsonElement capture = Read(file);
                string project = capture.GetProperty("project").GetString().Replace('\\', '/');
                if (project.EndsWith("/" + Path.GetFileName(path), StringComparison.Ordinal))
                    yield return (Path.GetFileName(file), capture);
            }
        }

        /// <summary>The walkthrough's figures of an example project's plots: images/*.plotspec.json.gz beside it.</summary>
        /// <param name="example">The example's path under the examples folder.</param>
        /// <returns>The figures, by file name.</returns>
        internal static IEnumerable<(string Name, JsonElement Figure)> Figures(string example)
        {
            string path = SharedControlsTesting.Example(example);
            string folder = Path.Combine(Path.GetDirectoryName(path), "images");
            foreach (string file in Directory.GetFiles(folder, "*.plotspec.json.gz").OrderBy(f => f, StringComparer.Ordinal))
            {
                JsonElement figure = Read(file);
                string project = figure.GetProperty("desktopSource").GetProperty("project").GetString();
                if (project.EndsWith("/" + Path.GetFileName(path), StringComparison.Ordinal))
                    yield return (Path.GetFileName(file), figure);
            }
        }

        private static JsonElement Read(string file)
        {
            using var stream = new GZipStream(File.OpenRead(file), CompressionMode.Decompress);
            using var document = JsonDocument.Parse(stream);
            return document.RootElement.Clone();
        }

        /// <summary>
        /// Checks a plot against RMC-BestFit's native export of it: the series by name, with their titles and points
        /// (or a histogram's bars), and the annotations. The exports were made from factory-reset plots, so their
        /// styling (the plot's and axes' titles, formats and ranges) isn't the saved plots' and isn't compared.
        /// </summary>
        /// <param name="plot">The plot, shown and updated.</param>
        /// <param name="capture">The export's capture data.</param>
        /// <param name="label">What the assertions say.</param>
        internal static void AssertNative(Wpf.Plot plot, JsonElement capture, string label)
        {
            ((IPlotModel)plot.ActualModel).Update(true);
            var expectedSeries = capture.GetProperty("series").EnumerateArray().ToList();
            Assert.AreEqual(string.Join(",", expectedSeries.Select(s => s.GetProperty("name").GetString())),
                string.Join(",", plot.Series.Select(s => s.Name)), label + ": the series");
            foreach (JsonElement expected in expectedSeries)
            {
                string name = expected.GetProperty("name").GetString();
                var series = plot.Series.Single(s => s.Name == name);
                Assert.AreEqual(expected.GetProperty("Title").GetString(), series.Title, label + ": " + name + "'s title");
                if (series is Wpf.HistogramSeries histogram)
                {
                    // The bars the series shows: its items.
                    var items = ((IEnumerable)histogram.ItemsSource).Cast<OxyPlot.Series.HistogramItem>().ToList();
                    JsonElement bars = expected.GetProperty("actualItems");
                    Assert.AreEqual(bars.GetArrayLength(), items.Count, label + ": " + name + "'s bars");
                    for (int i = 0; i < items.Count; i++)
                    {
                        AssertClose(bars[i].GetProperty("RangeStart").GetDouble(), items[i].RangeStart, $"{label}: {name} bar {i}'s start");
                        AssertClose(bars[i].GetProperty("RangeEnd").GetDouble(), items[i].RangeEnd, $"{label}: {name} bar {i}'s end");
                        AssertClose(bars[i].GetProperty("Value").GetDouble(), items[i].Value, $"{label}: {name} bar {i}");
                    }
                    continue;
                }
                AssertPoints(expected.GetProperty("points"), Points(series.InternalSeries, "ActualPoints"), label + ": " + name);
                if (expected.GetProperty("points2").GetArrayLength() > 0)
                    AssertPoints(expected.GetProperty("points2"), Points(series.InternalSeries, "ActualPoints2"), label + ": " + name + "'s second points");
            }
            var annotations = capture.GetProperty("annotations").EnumerateArray().ToList();
            Assert.AreEqual(annotations.Count, plot.Annotations.Count, label + ": the annotations");
            foreach (JsonElement expected in annotations)
            {
                var line = plot.Annotations.OfType<Wpf.LineAnnotation>().Single(a => a.Text == expected.GetProperty("text").GetString());
                AssertClose(expected.GetProperty("geometry").GetProperty("Y").GetDouble(), line.Y, label + ": " + line.Text);
            }
        }

        /// <summary>
        /// Checks a plot against a walkthrough's figure of it: each series of the figure by title, with its points
        /// (a band's or an area's lower and upper values too). As the exports, the figures' styling isn't the saved
        /// plots'.
        /// </summary>
        /// <param name="plot">The plot, shown and updated.</param>
        /// <param name="figure">The figure's data.</param>
        /// <param name="label">What the assertions say.</param>
        internal static void AssertFigure(Wpf.Plot plot, JsonElement figure, string label)
        {
            ((IPlotModel)plot.ActualModel).Update(true);
            var figureSeries = figure.GetProperty("series").EnumerateArray().ToList();
            CollectionAssert.AreEquivalent(figureSeries.Select(s => s.GetProperty("name").GetString()).ToList(),
                plot.Series.Select(s => s.Title).ToList(), label + ": the series " + string.Join(", ", plot.Series.Select(s => s.Title)));
            foreach (JsonElement expected in figureSeries)
            {
                string title = expected.GetProperty("name").GetString();
                var series = plot.Series.Where(s => s.Title == title).ToList();
                Assert.AreEqual(1, series.Count, label + ": one series titled " + title);
                var points = Points(series[0].InternalSeries, "ActualPoints");
                JsonElement x = expected.GetProperty("x");
                // A band's or an area's lower values are its first points; a scatter's are its error bars.
                bool area = expected.GetProperty("kind").GetString() is "band" or "area";
                JsonElement y = area ? expected.GetProperty("yLower") : expected.GetProperty("y");
                Assert.AreEqual(x.GetArrayLength(), points.Count, label + ": " + title + "'s points");
                for (int i = 0; i < points.Count; i++)
                {
                    AssertClose(x[i].GetDouble(), points[i].X, $"{label}: {title} {i}.X");
                    AssertClose(y[i].GetDouble(), points[i].Y, $"{label}: {title} {i}.Y");
                }
                if (area)
                {
                    JsonElement upper = expected.GetProperty("yUpper");
                    var upperPoints = Points(series[0].InternalSeries, "ActualPoints2");
                    Assert.AreEqual(upper.GetArrayLength(), upperPoints.Count, label + ": " + title + "'s upper points");
                    for (int i = 0; i < upperPoints.Count; i++)
                    {
                        AssertClose(x[i].GetDouble(), upperPoints[i].X, $"{label}: {title} upper {i}.X");
                        AssertClose(upper[i].GetDouble(), upperPoints[i].Y, $"{label}: {title} upper {i}.Y");
                    }
                }
            }
        }

        // The (X, Y) of a core series' points: data points or scatter points, from a property that may be protected.
        private static List<(double X, double Y)> Points(object series, string property)
        {
            PropertyInfo info = null;
            for (Type type = series.GetType(); type != null && info == null; type = type.BaseType)
                info = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.IsNotNull(info, series.GetType().Name + " has no " + property);
            return ((IEnumerable)info.GetValue(series)).Cast<object>().Select(point => point switch
            {
                DataPoint p => (p.X, p.Y),
                OxyPlot.Series.ScatterPoint p => (p.X, p.Y),
                _ => throw new AssertFailedException("Unexpected point type " + point.GetType().Name),
            }).ToList();
        }

        private static void AssertPoints(JsonElement expected, List<(double X, double Y)> actual, string label)
        {
            Assert.AreEqual(expected.GetArrayLength(), actual.Count, label + ": the number of points");
            for (int i = 0; i < actual.Count; i++)
            {
                AssertClose(Number(expected[i].GetProperty("X")), actual[i].X, $"{label} {i}.X");
                AssertClose(Number(expected[i].GetProperty("Y")), actual[i].Y, $"{label} {i}.Y");
            }
        }

        // A JSON number, or NaN for null (the exports write NaN as null).
        private static double Number(JsonElement value) => value.ValueKind == JsonValueKind.Number ? value.GetDouble() : double.NaN;

        /// <summary>
        /// Doubles the desktop wrote with the same .NET code: equal but for the last bits of another OS's maths.
        /// </summary>
        internal static void AssertClose(double expected, double actual, string label)
        {
            if (double.IsNaN(expected) && double.IsNaN(actual)) return;
            Assert.IsTrue(Math.Abs(expected - actual) <= Math.Max(1E-12, Math.Abs(expected) * 1E-9), $"{label}: expected {expected:R}, got {actual:R}");
        }

        /// <summary>Selects a tab, as a click on its header does, and lets the view update.</summary>
        internal static void Select(TabItem tab)
        {
            ((TabControl)tab.Parent).SelectedItem = tab;
            SharedControlsTesting.Settle();
        }
    }
}
