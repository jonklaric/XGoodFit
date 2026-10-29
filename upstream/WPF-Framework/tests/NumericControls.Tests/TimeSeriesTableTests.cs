using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using GenericControls;
using Numerics.Data;
using NumericControls;
using Xunit;

namespace NumericControls.Tests
{
    /// <summary>
    /// Tests for <see cref="TimeSeriesTable"/>.
    /// </summary>
    public class TimeSeriesTableTests
    {
        /// <summary>
        /// Tracks whether the generic control resource dictionary has been loaded for this test process.
        /// </summary>
        private static bool _resourcesLoaded;

        private static readonly object _dispatcherGate = new();
        private static Dispatcher? _dispatcher;

        /// <summary>
        /// Ensures a WPF application exists for control construction.
        /// </summary>
        private static void EnsureApplication()
        {
            var application = Application.Current ?? throw new InvalidOperationException("The test dispatcher must own the WPF application.");

            if (_resourcesLoaded) return;

            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/GenericControls;component/Themes/GenericControlsTheme.xaml", UriKind.Relative)
            });
            _resourcesLoaded = true;
        }

        /// <summary>
        /// Runs a test body on the single STA dispatcher that owns the process-wide WPF application.
        /// </summary>
        /// <param name="body">The test body to run.</param>
        private static void RunOnDispatcher(Action body)
        {
            Dispatcher dispatcher;
            lock (_dispatcherGate)
            {
                if (_dispatcher == null)
                {
                    using var ready = new ManualResetEventSlim(false);
                    Dispatcher? capturedDispatcher = null;
                    var thread = new Thread(() =>
                    {
                        _ = new Application();
                        capturedDispatcher = Dispatcher.CurrentDispatcher;
                        ready.Set();
                        Dispatcher.Run();
                    })
                    {
                        IsBackground = true,
                        Name = "TimeSeriesTableTestsDispatcher"
                    };
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    ready.Wait();
                    _dispatcher = capturedDispatcher;
                }

                dispatcher = _dispatcher!;
            }

            dispatcher.Invoke(body);
        }

        /// <summary>
        /// Creates a sample irregular time series.
        /// </summary>
        /// <returns>A sample time series with distinct values.</returns>
        private static TimeSeries CreateSampleSeries()
        {
            var start = new DateTime(2020, 1, 1);
            var series = new TimeSeries(TimeInterval.Irregular);
            series.Add(new SeriesOrdinate<DateTime, double>(start, 10d));
            series.Add(new SeriesOrdinate<DateTime, double>(start.AddDays(1), 30d));
            series.Add(new SeriesOrdinate<DateTime, double>(start.AddDays(2), 20d));
            return series;
        }

        /// <summary>
        /// Verifies that preview delete maps a visually sorted row index to the correct backing row.
        /// </summary>
        [Fact]
        public void PreviewDeleteRows_AfterValueSort_RemovesVisualRowFromBackingSeries()
        {
            RunOnDispatcher(() =>
            {
                EnsureApplication();
                var series = CreateSampleSeries();
                var table = new TimeSeriesTable { Series = series };
                var grid = Assert.IsType<ValidationDataGrid>(table.FindName("TimeSeriesDataGrid"));

                grid.Items.SortDescriptions.Clear();
                grid.Items.SortDescriptions.Add(new SortDescription(nameof(TimeSeriesRowItem.Value), ListSortDirection.Descending));
                grid.Items.Refresh();

                Assert.Equal(30d, ((TimeSeriesRowItem)grid.Items[0]).Value);

                var deleteMethod = typeof(TimeSeriesTable).GetMethod(
                    "TimeSeriesDataGrid_PreviewDeleteRows",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(deleteMethod);

                object?[] arguments = { new List<int> { 0 }, false };
                deleteMethod.Invoke(table, arguments);

                var values = Enumerable.Range(0, series.Count).Select(i => series[i].Value).ToList();
                Assert.True((bool)arguments[1]!);
                Assert.Equal(2, series.Count);
                Assert.DoesNotContain(30d, values);
            });
        }

        /// <summary>
        /// Verifies that initial row validation is deferred until the data grid has loaded.
        /// </summary>
        [Fact]
        public void SeriesAssignment_BeforeLoad_DefersValidationUntilGridLoaded()
        {
            RunOnDispatcher(() =>
            {
                EnsureApplication();
                var start = new DateTime(2020, 1, 1);
                var series = new TimeSeries(TimeInterval.Irregular);
                series.Add(new SeriesOrdinate<DateTime, double>(start.AddDays(1), 1d));
                series.Add(new SeriesOrdinate<DateTime, double>(start, 2d));
                var table = new TimeSeriesTable { Series = series };
                var grid = Assert.IsType<ValidationDataGrid>(table.FindName("TimeSeriesDataGrid"));
                var secondRow = Assert.IsType<TimeSeriesRowItem>(grid.Items[1]);

                Assert.False(secondRow.RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);

                grid.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                Assert.True(secondRow.RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
            });
        }

        /// <summary>
        /// Verifies that changing a date-time revalidates the following row's ordering relationship.
        /// </summary>
        [Fact]
        public void DateTimeEdit_RevalidatesFollowingRow()
        {
            RunOnDispatcher(() =>
            {
                EnsureApplication();
                var series = CreateSampleSeries();
                var table = new TimeSeriesTable { Series = series };
                var grid = Assert.IsType<ValidationDataGrid>(table.FindName("TimeSeriesDataGrid"));
                grid.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var firstRow = Assert.IsType<TimeSeriesRowItem>(grid.Items[0]);
                var secondRow = Assert.IsType<TimeSeriesRowItem>(grid.Items[1]);

                firstRow.DateTime = series[1].Index.AddDays(1);

                Assert.True(secondRow.RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
            });
        }

        /// <summary>
        /// Verifies direct series replacements revalidate replaced rows and the following row.
        /// </summary>
        [Fact]
        public void SeriesReplace_RevalidatesReplacedRowsAndFollowingRow()
        {
            RunOnDispatcher(() =>
            {
                EnsureApplication();
                var series = CreateSampleSeries();
                var table = new TimeSeriesTable { Series = series };
                var grid = Assert.IsType<ValidationDataGrid>(table.FindName("TimeSeriesDataGrid"));
                grid.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var firstRow = Assert.IsType<TimeSeriesRowItem>(grid.Items[0]);
                var secondRow = Assert.IsType<TimeSeriesRowItem>(grid.Items[1]);

                Assert.False(firstRow.RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
                Assert.False(secondRow.RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);

                series[1] = new SeriesOrdinate<DateTime, double>(series[0].Index.AddDays(-1), series[1].Value);

                Assert.True(secondRow.RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);

                series[1] = new SeriesOrdinate<DateTime, double>(series[0].Index.AddDays(1), series[1].Value);

                Assert.False(secondRow.RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);

                series[0] = new SeriesOrdinate<DateTime, double>(series[1].Index.AddDays(1), series[0].Value);

                Assert.False(firstRow.RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
                Assert.True(secondRow.RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
            });
        }

        /// <summary>
        /// Verifies validation suspension scopes nest and restore the grid state safely.
        /// </summary>
        [Fact]
        public void SuspendValidation_NestedScopes_RestoreValidationState()
        {
            RunOnDispatcher(() =>
            {
                EnsureApplication();
                var table = new TimeSeriesTable();
                var grid = Assert.IsType<ValidationDataGrid>(table.FindName("TimeSeriesDataGrid"));

                using (grid.SuspendValidation(validateOnResume: false))
                {
                    Assert.True(grid.SuppressValidation);
                    using (grid.SuspendValidation(validateOnResume: false))
                    {
                        Assert.True(grid.SuppressValidation);
                    }

                    Assert.True(grid.SuppressValidation);
                }

                Assert.False(grid.SuppressValidation);
            });
        }
    }
}
