using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlot;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Rating Curve and Time Series Analysis examples (6-rating-curve-analysis,
    /// 7-time-series-analysis), for the tests of their views (GUI/RatingCurveAnalysis, GUI/TimeSeriesAnalysis), and the
    /// comparison of a plot with a walkthrough's figure of it.
    /// </summary>
    internal static class RatingCurveTimeSeriesTesting
    {
        /// <summary>The synthetic rating curves: one, two and three hydraulic controls.</summary>
        internal const string SyntheticRatings = "6-rating-curve-analysis/synthetic-rating-curve-examples.bestfit";

        /// <summary>The Mississippi at New Madrid: a rating from 96 paired field measurements.</summary>
        internal const string Mississippi = "6-rating-curve-analysis/usgs-07024175-mississippi-rating-curve.bestfit";

        /// <summary>The synthetic time series: twelve models of one monthly series.</summary>
        internal const string SyntheticSeries = "7-time-series-analysis/1-synthetic-data-examples/synthetic-time-series-examples.bestfit";

        /// <summary>The classic series: airline passengers, the Nile's flows and Mauna Loa's CO2.</summary>
        internal const string ClassicSeries = "7-time-series-analysis/2-classic-time-series-examples/classic-time-series-examples.bestfit";

        /// <summary>The regression example: consumption on income, and on four covariates.</summary>
        internal const string Regression = "7-time-series-analysis/3-time-series-regression-example/time-series-regression-example.bestfit";

        /// <summary>The one-control synthetic rating, which RMC-BestFit exported.</summary>
        internal const string OneSegment = "1 Segment Rating Curve";

        /// <summary>The two-control synthetic rating.</summary>
        internal const string TwoSegments = "2 Segment Rating Curve";

        /// <summary>The three-control synthetic rating.</summary>
        internal const string ThreeSegments = "3 Segment Rating Curve";

        /// <summary>The Mississippi's rating, which RMC-BestFit exported.</summary>
        internal const string NewMadrid = "USGS 07024175 Rating Curve";

        /// <summary>The synthetic series' log-transformed model, which RMC-BestFit exported.</summary>
        internal const string LogTransform = "Intercept + Linear Trend + LogTransform";

        /// <summary>The airline passengers' ARIMA(1,1,1), which RMC-BestFit exported.</summary>
        internal const string Airline = "Airline Passengers - TSA";

        /// <summary>The Nile's ARIMA(1,1,0).</summary>
        internal const string Nile = "Nile River Flows - TSA";

        /// <summary>Mauna Loa's seasonal model with a quadratic trend.</summary>
        internal const string MaunaLoa = "Mauna Loa - CO2";

        /// <summary>Consumption on income, which RMC-BestFit exported.</summary>
        internal const string SimpleRegression = "Simple Linear Regression";

        /// <summary>Consumption on income, production, savings and unemployment.</summary>
        internal const string MultipleRegression = "Multiple Linear Regression";

        /// <summary>
        /// Checks a plot against RMC-BestFit's native export of it (<see cref="UnivariateAnalysisTesting.AssertNative"/>),
        /// and its vertical lines' positions too (the time series plot's end of training), which that check doesn't read.
        /// </summary>
        /// <param name="plot">The plot, shown and updated.</param>
        /// <param name="capture">The export's capture data.</param>
        /// <param name="label">What the assertions say.</param>
        internal static void AssertNativeExport(Wpf.Plot plot, JsonElement capture, string label)
        {
            UnivariateAnalysisTesting.AssertNative(plot, capture, label);
            foreach (JsonElement expected in capture.GetProperty("annotations").EnumerateArray())
            {
                JsonElement geometry = expected.GetProperty("geometry");
                if (geometry.GetProperty("Type").GetString() != "Vertical") continue;
                var line = plot.Annotations.OfType<Wpf.LineAnnotation>().Single(a => a.Text == expected.GetProperty("text").GetString());
                UnivariateAnalysisTesting.AssertClose(geometry.GetProperty("X").GetDouble(), line.X, label + ": " + line.Text + "'s X");
            }
        }

        /// <summary>
        /// Checks a plot against a walkthrough's figure of it (images/*.plotspec.json.gz), drawn from the desktop's
        /// coordinates: each series of the figure by its title, with its points (a band's two edges, a bar's range and
        /// height), and the figure's straight lines as the plot's annotations (a vertical line's position, a horizontal
        /// line's level, a sloped line's equation). Dates are the plot's OLE dates.
        /// </summary>
        /// <param name="plot">The plot, shown and updated.</param>
        /// <param name="figure">The figure's data.</param>
        /// <param name="label">What the assertions say.</param>
        /// <remarks>
        /// The figures call the curves' bands prediction intervals, which the views title credible intervals (their
        /// display corrections say so); and they name the residual plots' zero line Reference.
        /// </remarks>
        internal static void AssertWalkthroughFigure(Wpf.Plot plot, JsonElement figure, string label)
        {
            ((IPlotModel)plot.ActualModel).Update(true);
            var corrections = figure.TryGetProperty("displayCorrections", out JsonElement c) && c.ValueKind == JsonValueKind.Array
                ? c.EnumerateArray().Select(e => e.GetString()).ToList() : new List<string>();
            bool intervals = corrections.Any(s => s.StartsWith("Prediction intervals include residual", StringComparison.Ordinal));
            var relabelled = new Dictionary<string, string>();
            foreach (string correction in corrections)
            {
                var match = Regex.Match(correction, @"labeled (.+) rather than (.+)\.$");
                if (match.Success) relabelled[match.Groups[1].Value] = match.Groups[2].Value;
            }
            string Title(string name)
            {
                if (relabelled.TryGetValue(name, out string original)) return original;
                return intervals ? name.Replace("Prediction Intervals", "Credible Intervals", StringComparison.Ordinal) : name;
            }

            var matched = new List<string>();
            foreach (JsonElement expected in figure.GetProperty("series").EnumerateArray())
            {
                string name = expected.GetProperty("name").GetString();
                string title = Title(name);
                string kind = expected.GetProperty("kind").GetString();
                var series = plot.Series.Where(s => s.Title == title).ToList();
                if (series.Count == 0 && kind == "line")
                {
                    AssertLine(plot, expected, name, $"{label}: {name}");
                    continue;
                }
                Assert.AreEqual(1, series.Count, $"{label}: one series titled {title}, of " + string.Join(", ", plot.Series.Select(s => s.Title)));
                matched.Add(title);
                double[] x = Numbers(expected.GetProperty("x"));
                switch (kind)
                {
                    case "line":
                    case "scatter":
                        AssertPoints(x, Numbers(expected.GetProperty("y")), Points(series[0].InternalSeries, "ActualPoints"), $"{label}: {title}");
                        break;
                    case "band" when expected.TryGetProperty("xLower", out JsonElement xLower):
                        // A horizontal band (the rating curve's): its edges are discharges at each stage.
                        double[] stages = Numbers(expected.GetProperty("y"));
                        AssertPoints(Numbers(xLower), stages, Points(series[0].InternalSeries, "ActualPoints"), $"{label}: {title}'s lower edge");
                        AssertPoints(Numbers(expected.GetProperty("xUpper")), stages, Points(series[0].InternalSeries, "ActualPoints2"), $"{label}: {title}'s upper edge");
                        break;
                    case "band":
                        AssertPoints(x, Numbers(expected.GetProperty("yLower")), Points(series[0].InternalSeries, "ActualPoints"), $"{label}: {title}'s lower edge");
                        AssertPoints(x, Numbers(expected.GetProperty("yUpper")), Points(series[0].InternalSeries, "ActualPoints2"), $"{label}: {title}'s upper edge");
                        break;
                    case "bars":
                        // A bar's centre and width, and its height: the histogram item's range and value.
                        var items = ((IEnumerable)((Wpf.HistogramSeries)series[0]).ItemsSource).Cast<OxyPlot.Series.HistogramItem>().ToList();
                        double[] heights = Numbers(expected.GetProperty("y")), widths = Numbers(expected.GetProperty("width"));
                        Assert.AreEqual(x.Length, items.Count, $"{label}: {title}'s bars");
                        for (int i = 0; i < items.Count; i++)
                        {
                            UnivariateAnalysisTesting.AssertClose(x[i] - widths[i] / 2, items[i].RangeStart, $"{label}: {title} bar {i}'s start");
                            UnivariateAnalysisTesting.AssertClose(x[i] + widths[i] / 2, items[i].RangeEnd, $"{label}: {title} bar {i}'s end");
                            UnivariateAnalysisTesting.AssertClose(heights[i], items[i].Value, $"{label}: {title} bar {i}");
                        }
                        break;
                    default:
                        throw new AssertFailedException($"{label}: a figure series of a kind this test doesn't know, {kind}");
                }
            }
            CollectionAssert.AreEquivalent(plot.Series.Select(s => s.Title).ToList(), matched, label + ": the plot's series are the figure's");
        }

        // A straight line of a figure (two points): the plot's annotation of that text (the zero line has none).
        private static void AssertLine(Wpf.Plot plot, JsonElement expected, string name, string label)
        {
            var line = plot.Annotations.OfType<Wpf.LineAnnotation>().SingleOrDefault(a => name == "Reference" ? a.Name == "ZeroLine" : a.Text == name);
            Assert.IsNotNull(line, label + ": a series or line of the plot, of " + string.Join(", ", plot.Annotations.OfType<Wpf.LineAnnotation>().Select(a => a.Text)));
            double[] x = Numbers(expected.GetProperty("x")), y = Numbers(expected.GetProperty("y"));
            Assert.AreEqual(2, x.Length, label + ": a straight line");
            switch (line.Type)
            {
                case OxyPlot.Annotations.LineAnnotationType.Vertical:
                    UnivariateAnalysisTesting.AssertClose(x[0], line.X, label + "'s X");
                    UnivariateAnalysisTesting.AssertClose(x[1], line.X, label + "'s X");
                    break;
                case OxyPlot.Annotations.LineAnnotationType.Horizontal:
                    UnivariateAnalysisTesting.AssertClose(y[0], line.Y, label + "'s Y");
                    UnivariateAnalysisTesting.AssertClose(y[1], line.Y, label + "'s Y");
                    break;
                default:
                    for (int i = 0; i < 2; i++)
                        Assert.AreEqual(y[i], line.Slope * x[i] + line.Intercept, Math.Max(1E-12, Math.Abs(y[i]) * 1E-9), $"{label} at {x[i]}");
                    break;
            }
        }

        // A figure's numbers; a date's are its OLE date, as the plots' date axes take it.
        private static double[] Numbers(JsonElement values) =>
            values.EnumerateArray().Select(v => v.ValueKind switch
            {
                JsonValueKind.Number => v.GetDouble(),
                JsonValueKind.String => DateTime.Parse(v.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToOADate(),
                _ => double.NaN,
            }).ToArray();

        private static void AssertPoints(double[] x, double[] y, List<(double X, double Y)> actual, string label)
        {
            Assert.AreEqual(x.Length, actual.Count, label + ": the number of points");
            for (int i = 0; i < actual.Count; i++)
            {
                UnivariateAnalysisTesting.AssertClose(x[i], actual[i].X, $"{label} {i}.X");
                UnivariateAnalysisTesting.AssertClose(y[i], actual[i].Y, $"{label} {i}.Y");
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
    }
}
