using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Xunit;

namespace OxyPlotControls.Tests.Series;

/// <summary>
/// Thread-safety tests for the Phase 1 Item 1.6 refactor of
/// <see cref="OxyPlot.Series.XYAxisSeries.GetNearestPointInternal"/>.
/// </summary>
/// <remarks>
/// The original implementation used <c>foreach (var p in points.Skip(startIdx))</c> over the
/// backing <see cref="List{DataPoint}"/>, which constructs a state-machine enumerator.
/// If the underlying list is mutated by another thread mid-scan, that enumerator throws
/// <c>InvalidOperationException: Collection was modified</c>. After 1.6 the method uses
/// an indexed <c>for</c> loop over <see cref="IList{DataPoint}"/>, which does not observe
/// the enumerator version flag and so is safe against concurrent mutation (reads are still
/// torn, but no exception).
/// </remarks>
public class GetNearestPointThreadSafetyTests
{
    /// <summary>
    /// Drives <see cref="LineSeries.GetNearestPoint"/> in a tight loop on the UI thread while
    /// a worker thread mutates the series' <c>Points</c> list. The legacy implementation
    /// threw <c>InvalidOperationException</c> within a few iterations; the 1.6 indexed scan
    /// completes without throwing.
    /// </summary>
    [Fact]
    public async Task GetNearestPoint_DoesNotThrow_UnderConcurrentPointsMutation()
    {
        var series = new LineSeries();
        for (int i = 0; i < 1000; i++)
        {
            series.Points.Add(new DataPoint(i, i));
        }

        // Attach axes so Transform does not throw.
        var model = new PlotModel();
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 1000 });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1000 });
        model.Series.Add(series);
        ((IPlotModel)model).Update(updateData: true);

        using var cts = new CancellationTokenSource();
        var mutator = Task.Run(() =>
        {
            var rng = new Random(12345);
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // Rapid add/remove on the list to maximize chances of catching a
                    // version-sensitive enumerator mid-iteration.
                    if (series.Points.Count > 500 && rng.Next(2) == 0)
                    {
                        series.Points.RemoveAt(series.Points.Count - 1);
                    }
                    else
                    {
                        int n = series.Points.Count;
                        series.Points.Add(new DataPoint(n, n));
                    }
                }
            }
            catch
            {
                // Mutation races on List<T> itself may produce internal exceptions;
                // they are not the subject of this test and must not fail the assertion.
            }
        });

        try
        {
            for (int iter = 0; iter < 2000; iter++)
            {
                // Any screen point — the scan is what we care about, not the returned hit.
                _ = series.GetNearestPoint(new ScreenPoint(500, 500), interpolate: false);
            }
        }
        finally
        {
            cts.Cancel();
            await mutator.WaitAsync(TimeSpan.FromSeconds(5));
        }

        // Reaching this assertion means no InvalidOperationException bubbled out of
        // GetNearestPointInternal while the mutator thread was churning Points.
        Assert.True(true);
    }
}
