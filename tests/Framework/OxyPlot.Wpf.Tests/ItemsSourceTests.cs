using System.Collections;
using System.Collections.Specialized;
using OxyPlot.Wpf;

namespace OxyPlot.Wpf.Tests;

/// <summary>
/// A series takes as its ItemsSource any collection, as WPF's ItemsControl does: also one that notifies changes
/// but isn't a list, as RMC-BestFit's data series are, which Avalonia's ItemsControl rejects.
/// </summary>
[TestClass]
public sealed class ItemsSourceTests
{
    // A collection that notifies changes but isn't a list, as RMC-BestFit's DataSeries (an IList<Data>).
    private sealed class NotifyingPoints : IEnumerable<DataPoint>, INotifyCollectionChanged
    {
        private readonly List<DataPoint> _points = new();
        private NotifyCollectionChangedEventHandler? _handlers;

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add { _handlers += value; Subscribers++; }
            remove { _handlers -= value; Subscribers--; }
        }

        public int Subscribers { get; private set; }

        public void Add(DataPoint point)
        {
            _points.Add(point);
            _handlers?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, point, _points.Count - 1));
        }

        public IEnumerator<DataPoint> GetEnumerator() => _points.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [TestMethod]
    public void ANotifyingCollectionThatIsNotAList_IsTheSeriesData_AndFollowedLive()
    {
        HeadlessSession.Run(() =>
        {
            var points = new NotifyingPoints();
            points.Add(new DataPoint(1, 10));
            var series = new LineSeries { ItemsSource = points };
            var plot = new Plot();
            plot.Series.Add(series);
            plot.InvalidatePlot(true);

            var view = (IList)series.ItemsSource!;
            Assert.AreEqual(1, view.Count);
            Assert.AreEqual(new DataPoint(1, 10), view[0]);
            CollectionAssert.AreEqual(new[] { new DataPoint(1, 10) }, ((OxyPlot.Series.ItemsSeries)series.InternalSeries).ItemsSource.Cast<DataPoint>().ToList(),
                "the OxyPlot series reads the collection");
            Assert.IsTrue(points.Subscribers > 0, "the series follows the collection's changes");

            points.Add(new DataPoint(2, 20));
            Assert.AreEqual(2, view.Count, "the view reads the collection afresh");
            plot.InvalidatePlot(true);
            Assert.AreEqual(2, ((OxyPlot.Series.ItemsSeries)series.InternalSeries).ItemsSource.Cast<object>().Count());

            series.ItemsSource = null;
            Assert.AreEqual(0, points.Subscribers, "a series given other items stops following the collection");

            var list = new List<DataPoint> { new DataPoint(0, 0) };
            series.ItemsSource = list;
            Assert.AreSame(list, series.ItemsSource, "a list is the ItemsSource as it is");
        });
    }

    [TestMethod]
    public void ASeriesInheritingTheThemeFont_ReportsNoFontChange_WhenItsViewLeavesTheWindow()
    {
        HeadlessSession.Run(() =>
        {
            var series = new LineSeries();
            var plot = new Plot();
            plot.Series.Add(series);
            var view = new Avalonia.Controls.ContentControl { Content = plot };
            var window = new Avalonia.Controls.Window { FontFamily = new Avalonia.Media.FontFamily("Segoe UI, avares://Themes/Assets/Fonts#Selawik"), Content = view };
            window.Show();
            try
            {
                var changes = new List<string>();
                ((System.ComponentModel.INotifyPropertyChanged)series).PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

                window.Content = null;
                window.Content = view;
                CollectionAssert.DoesNotContain(changes, "FontFamily", "the same font as WPF's default, Segoe UI, as WPF compares families by their text");

                window.Content = null;
                window.FontFamily = new Avalonia.Media.FontFamily("Arial");
                window.Content = view;
                CollectionAssert.Contains(changes, "FontFamily", "another font is a change");
            }
            finally
            {
                window.Close();
            }
        });
    }
}
