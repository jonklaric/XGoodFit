using System.ComponentModel;
using Avalonia.Media;
using OxyPlot.Axes;

namespace OxyPlot.Wpf.Tests;

/// <summary>
/// Setting a property of the plot or of its series, axes and annotations updates the internal OxyPlot
/// model, through the property-changed callbacks ported from WPF, and the plot relays the change as
/// INotifyPropertyChanged, which BestFit's undo system records.
/// </summary>
[TestClass]
public sealed class PropertySyncTests
{
    [TestMethod]
    public void PlotProperties_UpdateTheModel_AndAreRelayed()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            var changed = new List<string>();
            ((INotifyPropertyChanged)plot).PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

            plot.Title = "Updated";
            Assert.AreEqual("Updated", plot.ActualModel.Title, "synchronised as soon as it is set");

            plot.TitleFontWeight = FontWeight.Normal;
            Assert.AreEqual(400, plot.ActualModel.TitleFontWeight);

            plot.LegendTextColor = Colors.Purple;
            Assert.AreEqual(OxyColors.Purple, plot.ActualModel.Legends[0].LegendTextColor);

            plot.Padding = new Avalonia.Thickness(20);
            Assert.AreEqual(new OxyThickness(20), plot.ActualModel.Padding, "an Avalonia property with the fork's callback");

            CollectionAssert.AreEqual(new[] { "Title", "TitleFontWeight", "LegendTextColor", "Padding" }, changed);
        });
    }

    [TestMethod]
    public void SeriesAxisAndAnnotationProperties_UpdateTheModel_AndAreRelayedWithAPrefix()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            plot.InvalidatePlot(true);
            var changed = new List<string>();
            plot.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

            var series = (LineSeries)plot.Series[0];
            series.Color = Colors.Orange;
            series.StrokeThickness = 5;
            var lineModel = (OxyPlot.Series.LineSeries)plot.ActualModel.Series[0];
            Assert.AreSame(series.InternalSeries, lineModel);
            Assert.AreEqual(OxyColors.Orange, lineModel.Color);
            Assert.AreEqual(5, lineModel.StrokeThickness);

            var axis = (LinearAxis)plot.Axes[1];
            axis.Title = "Discharge";
            axis.Position = AxisPosition.Right;
            var axisModel = plot.ActualModel.Axes[1];
            Assert.AreEqual("Discharge", axisModel.Title);
            Assert.AreEqual(AxisPosition.Right, axisModel.Position);

            var annotation = (LineAnnotation)plot.Annotations[0];
            annotation.X = 0.5;
            Assert.AreEqual(0.5, ((OxyPlot.Annotations.LineAnnotation)plot.ActualModel.Annotations[0]).X);

            CollectionAssert.IsSubsetOf(new[] { "Series.Color", "Series.StrokeThickness", "Axis.Title", "Axis.Position", "Annotation.X" }, changed);
        });
    }

    [TestMethod]
    public void SuppressPropertyChanged_DefersSynchronisation_UntilTheNextDataInvalidate()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            plot.InvalidatePlot(true);
            var changed = new List<string>();
            plot.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

            plot.SuppressPropertyChanged = true;
            plot.Title = "Batch";
            plot.Series.Add(new LineSeries { Title = "Added" });
            Assert.AreEqual("Annual peak flow", plot.ActualModel.Title, "not synchronised while suppressed");
            Assert.AreEqual(3, plot.ActualModel.Series.Count);

            // As the fork documents, suppression ends with an explicit InvalidatePlot; a data update
            // synchronises everything.
            plot.SuppressPropertyChanged = false;
            plot.InvalidatePlot(true);
            Assert.AreEqual("Batch", plot.ActualModel.Title);
            Assert.AreEqual(4, plot.ActualModel.Series.Count);
            Assert.AreEqual(0, changed.Count, "no events for suppressed changes");
        });
    }

    [TestMethod]
    public void CollectionChanges_KeepTheLogicalTree_AndTheModel_InStep()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            var series = plot.Series[0];
            Assert.AreSame(plot, series.Parent, "series are the plot's logical children, as in WPF");

            plot.Series.Clear();
            Assert.IsNull(series.Parent, "cleared series leave the logical tree");
            Assert.AreEqual(0, plot.ActualModel.Series.Count);

            plot.Series.Add(series);
            plot.Series.Add(new LineSeries());
            Assert.AreSame(plot, series.Parent);
            Assert.AreEqual(2, plot.ActualModel.Series.Count);

            var oldAxis = plot.Axes[1];
            var newAxis = new LogarithmicAxis { Key = "y", Position = AxisPosition.Left };
            Axis? replacedOld = null, replacedNew = null;
            plot.AxisReplaced += (o, n) => (replacedOld, replacedNew) = (o, n);
            plot.ReplaceAxis(oldAxis, newAxis);
            Assert.AreSame(newAxis, plot.Axes[1]);
            Assert.AreSame(oldAxis, replacedOld);
            Assert.AreSame(newAxis, replacedNew);
            Assert.IsInstanceOfType<OxyPlot.Axes.LogarithmicAxis>(plot.ActualModel.Axes[1]);
        });
    }

    [TestMethod]
    public void ItemsSourceChanges_UpdateTheData()
    {
        HeadlessSession.Run(() =>
        {
            var plot = new Plot();
            var points = new System.Collections.ObjectModel.ObservableCollection<DataPoint> { new(0, 0), new(1, 1) };
            var series = new LineSeries { ItemsSource = points };
            plot.Series.Add(series);
            plot.InvalidatePlot(true);
            var model = (OxyPlot.Series.LineSeries)plot.ActualModel.Series[0];
            Assert.AreSame(points, model.ItemsSource);

            Assert.AreEqual(1, plot.ActualModel.DefaultXAxis.DataMaximum);

            points.Add(new DataPoint(2, 4));
            Assert.AreEqual(2, plot.ActualModel.DefaultXAxis.DataMaximum, "a change to the collection updates the model's data (weak subscription)");

            var replacement = new List<DataPoint> { new(5, 5) };
            series.ItemsSource = replacement;
            Assert.AreSame(replacement, ((OxyPlot.Series.LineSeries)plot.ActualModel.Series[0]).ItemsSource, "a new items source is synchronised");
            Assert.AreEqual(5, plot.ActualModel.DefaultXAxis.DataMaximum);
        });
    }
}
