using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Xunit;

namespace OxyPlotControls.Tests.Series;

/// <summary>
/// Tests for the <see cref="OxyPlot.Series.Series.IsHitTestEnabled"/> property added in the
/// Phase 1 OxyPlot performance overhaul. Verifies per-series opt-out from the tracker
/// hit-test path.
/// </summary>
public class SeriesIsHitTestEnabledTests
{
    /// <summary>
    /// A fresh <see cref="LineSeries"/> must default <see cref="OxyPlot.Series.Series.IsHitTestEnabled"/>
    /// to <c>true</c> so Phase 1 is purely additive — existing consumers unaware of the flag
    /// get identical tracker behavior to before.
    /// </summary>
    [Fact]
    public void IsHitTestEnabled_DefaultsToTrue_OnFreshLineSeries()
    {
        var series = new LineSeries();
        Assert.True(series.IsHitTestEnabled);
    }

    /// <summary>
    /// When <c>IsHitTestEnabled=false</c>, <see cref="PlotModel.GetSeriesFromPoint"/> must skip
    /// the series in its filter. The filter runs BEFORE any hit-test work, so
    /// <see cref="OxyPlot.Series.Series.GetNearestPoint"/> must not be invoked on a disabled series.
    /// </summary>
    [Fact]
    public void GetSeriesFromPoint_DoesNotCallGetNearestPoint_WhenHitTestDisabled()
    {
        var model = new PlotModel();
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 10 });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 10 });

        var series = new CountingLineSeries { IsHitTestEnabled = false };
        series.Points.Add(new DataPoint(0, 0));
        series.Points.Add(new DataPoint(10, 10));
        model.Series.Add(series);

        // Must update the model so visible series list is populated correctly.
        ((IPlotModel)model).Update(updateData: true);

        // Arbitrary screen point — the filter short-circuits before coordinates matter.
        _ = model.GetSeriesFromPoint(new ScreenPoint(50, 50), limit: 1000);

        Assert.Equal(0, series.GetNearestPointCallCount);
    }

    /// <summary>
    /// When <c>IsHitTestEnabled=true</c> (the default), <see cref="PlotModel.GetSeriesFromPoint"/>
    /// must still invoke <see cref="OxyPlot.Series.Series.GetNearestPoint"/> on the series —
    /// the flag is the ONLY gate added by 1.1.
    /// </summary>
    [Fact]
    public void GetSeriesFromPoint_CallsGetNearestPoint_WhenHitTestEnabled()
    {
        var model = new PlotModel();
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 10 });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 10 });

        var series = new CountingLineSeries();  // IsHitTestEnabled defaults to true
        series.Points.Add(new DataPoint(0, 0));
        series.Points.Add(new DataPoint(10, 10));
        model.Series.Add(series);

        ((IPlotModel)model).Update(updateData: true);

        _ = model.GetSeriesFromPoint(new ScreenPoint(50, 50), limit: 1000);

        Assert.True(series.GetNearestPointCallCount > 0,
            "IsHitTestEnabled=true should route through GetNearestPoint at least once.");
    }

    /// <summary>
    /// Test-only <see cref="LineSeries"/> subclass that records the number of times
    /// <see cref="OxyPlot.Series.LineSeries.GetNearestPoint"/> is invoked.
    /// </summary>
    private sealed class CountingLineSeries : LineSeries
    {
        /// <summary>Gets the number of <see cref="GetNearestPoint"/> invocations since construction.</summary>
        public int GetNearestPointCallCount { get; private set; }

        /// <inheritdoc/>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            this.GetNearestPointCallCount++;
            return base.GetNearestPoint(point, interpolate);
        }
    }
}
