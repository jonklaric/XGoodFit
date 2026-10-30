using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.Models;
using RMC.BestFit.UI;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Point Process and Mixture examples (4-univariate-distribution-analysis), for the tests of
    /// their views (GUI/UnivariateAnalysis/PointProcess and Mixture).
    /// </summary>
    internal static class PointProcessMixtureTesting
    {
        /// <summary>The point process example: annual-maxima GEV fits and the stations' point-process fits.</summary>
        internal const string PointProcessExample = UnivariateAnalysisTesting.Folder + "3-point-process-analysis/point-process-examples.bestfit";

        /// <summary>The mixture example: two- and three-Normal mixtures, one zero-inflated.</summary>
        internal const string MixtureExample = UnivariateAnalysisTesting.Folder + "4-mixture-analysis/mixture-distribution-examples.bestfit";

        /// <summary>Big Bear Lake's point-process fit, which RMC-BestFit exported.</summary>
        internal const string BigBear = "USC00040741 - Point Process";

        /// <summary>De Sabla's point-process fit.</summary>
        internal const string DeSabla = "USC00042402 - Point Process";

        /// <summary>Big Bear Lake's two-season point-process fit.</summary>
        internal const string Seasonal = "USC00040741 - Seasonal Point Process";

        /// <summary>The two-Normal mixture, which RMC-BestFit exported.</summary>
        internal const string TwoNormals = "Mixture Distribution - 2 Normals";

        /// <summary>The three-Normal mixture.</summary>
        internal const string ThreeNormals = "Mixture Distribution - 3 Normals";

        /// <summary>The zero-inflated two-Normal mixture.</summary>
        internal const string ZeroInflated = "Mixture Distribution - 2 Normals - Zero-Inflated";

        /// <summary>
        /// Ticks or clears a property control's check box as a click does: the check box changes, and its binding
        /// writes the state to the control and on to the element.
        /// </summary>
        /// <param name="control">The property control.</param>
        /// <param name="value">The state.</param>
        internal static void Tick(BooleanPropertyControl control, bool value)
        {
            control.GetVisualDescendants().OfType<CheckBox>().Single().IsChecked = value;
            SharedControlsTesting.Settle();
        }

        /// <summary>
        /// Checks a frequency plot whose saved value axis is linear against a walkthrough figure of it, which RMC-BestFit
        /// drew on a factory-default plot with a logarithmic value axis: upstream's view leaves the data's zeros out only
        /// on a logarithmic axis, so the figure's data points are the plot's positive ones. Then the figure as the
        /// saved plot draws it, zeros included (<see cref="UnivariateAnalysisTesting.AssertFigure"/>).
        /// </summary>
        /// <param name="plot">The plot, shown and updated.</param>
        /// <param name="figure">The figure's data.</param>
        /// <param name="label">What the assertions say.</param>
        internal static void AssertFigureOnLinearValueAxis(Wpf.Plot plot, JsonElement figure, string label)
        {
            Assert.AreEqual("log", figure.GetProperty("axes").GetProperty("y").GetProperty("scale").GetString(), label + ": the figure's value axis");
            Assert.IsFalse(plot.Axes.Any(a => a.Key == "Yaxis" && a is Wpf.LogarithmicAxis), label + ": the saved plot's value axis is linear");
            var node = JsonNode.Parse(figure.GetRawText());
            foreach (JsonNode series in node["series"].AsArray().Where(s => (string)s["kind"] == "scatter"))
            {
                string title = (string)series["name"];
                var scatter = (Wpf.ScatterPointSeries)plot.Series.Single(s => s.Title == title);
                var points = ((IEnumerable)scatter.ItemsSource).Cast<ExactData>().Select(d => (X: d.PlottingPosition, Y: d.Value)).ToList();
                var positive = points.Where(p => p.Y > 1E-16).ToList();
                JsonArray x = series["x"].AsArray(), y = series["y"].AsArray();
                Assert.AreEqual(x.Count, positive.Count, label + ": " + title + "'s positive points");
                for (int i = 0; i < positive.Count; i++)
                {
                    UnivariateAnalysisTesting.AssertClose((double)x[i], positive[i].X, $"{label}: {title} {i}.X");
                    UnivariateAnalysisTesting.AssertClose((double)y[i], positive[i].Y, $"{label}: {title} {i}.Y");
                }
                series["x"] = new JsonArray(points.Select(p => (JsonNode)JsonValue.Create(p.X)).ToArray());
                series["y"] = new JsonArray(points.Select(p => (JsonNode)JsonValue.Create(p.Y)).ToArray());
            }
            using var drawn = JsonDocument.Parse(node.ToJsonString());
            UnivariateAnalysisTesting.AssertFigure(plot, drawn.RootElement.Clone(), label);
        }

        /// <summary>Records the property changes of the objects given, as "Type.Property".</summary>
        /// <param name="changes">Where the changes go.</param>
        /// <param name="sources">The objects; those that don't notify are skipped.</param>
        internal static void Watch(List<string> changes, params object[] sources)
        {
            foreach (object source in sources)
                if (source is INotifyPropertyChanged notifier)
                    notifier.PropertyChanged += (sender, e) => changes.Add(sender.GetType().Name + "." + e.PropertyName);
        }
    }

    /// <summary>
    /// XGoodFit addition: an example project that a test class opens once, on the UI thread, when a test first asks for
    /// it, for the tests that only show it; the class closes it in its class cleanup. Tests that edit the example open a
    /// copy of their own.
    /// </summary>
    internal sealed class SharedExample
    {
        private readonly string _relative;
        private BestFitProject _project;

        /// <summary>Names the example.</summary>
        /// <param name="relative">The example's path under the examples folder.</param>
        internal SharedExample(string relative) => _relative = relative;

        /// <summary>The opened copy of the example.</summary>
        internal BestFitProject Project => _project ??= SharedControlsTesting.Open(_relative);

        /// <summary>Closes the example, if a test opened it, on the UI thread.</summary>
        internal void Close()
        {
            BestFitProject project = _project;
            _project = null;
            if (project == null) return;
            if (Dispatcher.UIThread.CheckAccess())
                SharedControlsTesting.Close(project);
            else
                HeadlessSession.Run(() => SharedControlsTesting.Close(project));
            GC.Collect();
        }
    }
}
