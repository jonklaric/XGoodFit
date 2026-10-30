using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlot;
using OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: reads the capture data (.json.gz) beside RMC-BestFit's native screenshots of the
    /// examples' plots, and compares a ported plot's data with it.
    /// </summary>
    /// <remarks>
    /// The examples' screenshots/*-native.json.gz files record what upstream's WPF views plotted: each series'
    /// points (and an area series' second points) and the annotations, from the saved element data.
    /// </remarks>
    internal static class NativeCaptures
    {
        /// <summary>Reads a capture next to an example: its screenshots folder's &lt;name&gt;.json.gz.</summary>
        /// <param name="example">The example's path under the examples folder.</param>
        /// <param name="name">The capture's name, without the extension.</param>
        /// <returns>The capture's JSON.</returns>
        internal static JsonElement Read(string example, string name)
        {
            string path = Path.Combine(Path.GetDirectoryName(ViewTesting.Example(example)), "screenshots", name + ".json.gz");
            using var file = File.OpenRead(path);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            return JsonDocument.Parse(gzip).RootElement.Clone();
        }

        /// <summary>
        /// Checks that a plot has the capture's series, by name, with the same points within a relative
        /// tolerance, and the capture's annotations, by text and position.
        /// </summary>
        /// <param name="plot">The ported plot, shown and updated.</param>
        /// <param name="capture">The capture.</param>
        /// <param name="tolerance">The relative tolerance of a coordinate.</param>
        internal static void AssertPlotMatches(Plot plot, JsonElement capture, double tolerance)
        {
            ((IPlotModel)plot.ActualModel).Update(true);
            string label = capture.GetProperty("element").GetString() + " " + capture.GetProperty("plotId").GetString();
            var expectedSeries = capture.GetProperty("series").EnumerateArray().ToList();
            Assert.AreEqual(string.Join(",", expectedSeries.Select(s => s.GetProperty("name").GetString())),
                string.Join(",", plot.Series.Select(s => s.Name)), label + ": the series");
            foreach (JsonElement expected in expectedSeries)
            {
                string name = expected.GetProperty("name").GetString();
                var series = plot.Series.Single(s => s.Name == name).InternalSeries;
                AssertPoints(expected.GetProperty("points"), Points(series, "ActualPoints"), tolerance, label + " " + name + " points");
                if (expected.GetProperty("points2").GetArrayLength() > 0)
                    AssertPoints(expected.GetProperty("points2"), Points(series, "ActualPoints2"), tolerance, label + " " + name + " points2");
            }

            var expectedAnnotations = capture.GetProperty("annotations").EnumerateArray().ToList();
            Assert.AreEqual(expectedAnnotations.Count, plot.Annotations.Count, label + ": the annotations");
            for (int i = 0; i < expectedAnnotations.Count; i++)
            {
                JsonElement expected = expectedAnnotations[i];
                var annotation = (OxyPlot.Annotations.LineAnnotation)plot.Annotations[i].InternalAnnotation;
                Assert.AreEqual(expected.GetProperty("text").GetString(), annotation.Text, label + ": annotation " + i);
                JsonElement geometry = expected.GetProperty("geometry");
                Assert.AreEqual(geometry.GetProperty("Type").GetString(), annotation.Type.ToString(), label + ": annotation " + i);
                AssertClose(Number(geometry.GetProperty("X")), annotation.X, tolerance, label + ": annotation " + i + " X");
                AssertClose(Number(geometry.GetProperty("Y")), annotation.Y, tolerance, label + ": annotation " + i + " Y");
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

        private static void AssertPoints(JsonElement expected, List<(double X, double Y)> actual, double tolerance, string label)
        {
            var points = expected.EnumerateArray().ToList();
            Assert.AreEqual(points.Count, actual.Count, label + ": the number of points");
            for (int i = 0; i < points.Count; i++)
            {
                AssertClose(Number(points[i].GetProperty("X")), actual[i].X, tolerance, label + "[" + i + "].X");
                AssertClose(Number(points[i].GetProperty("Y")), actual[i].Y, tolerance, label + "[" + i + "].Y");
            }
        }

        // A JSON number, or NaN for null (the capture writes NaN as null).
        private static double Number(JsonElement value) => value.ValueKind == JsonValueKind.Number ? value.GetDouble() : double.NaN;

        private static void AssertClose(double expected, double actual, double tolerance, string label)
        {
            if (double.IsNaN(expected) && double.IsNaN(actual)) return;
            Assert.IsTrue(Math.Abs(expected - actual) <= tolerance * Math.Max(1, Math.Abs(expected)),
                $"{label}: expected {expected:R}, got {actual:R}");
        }
    }
}
