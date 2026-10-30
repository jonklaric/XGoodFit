using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using OxyPlot;
using OxyPlot.Axes;
using static OxyPlotControls.Tests.PlotTesting;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls.Tests;

/// <summary>
/// The toolbar's modes set up the plot's controller as upstream's do: the pointer tracks, pan drags the
/// axes, zoom draws a rectangle to zoom to, and zoom extents resets them. Each mode has its cursor.
/// </summary>
[TestClass]
public sealed class ToolbarTests
{
    private static Axis XAxis(Wpf.Plot plot) => plot.ActualModel.Axes.Single(a => a.Position == AxisPosition.Bottom);

    private static Axis YAxis(Wpf.Plot plot) => plot.ActualModel.Axes.Single(a => a.Position == AxisPosition.Left);

    [TestMethod]
    public void PointerMode_IsTheDefault_AndALeftPressTracks()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, toolbar, _) = ShowDemo();
            Assert.IsTrue(toolbar.PointerButton.IsChecked);

            var model = plot.ActualModel;
            ScreenPoint onSeries = model.Series[0].GetNearestPoint(new ScreenPoint(model.PlotArea.Center.X, model.PlotArea.Center.Y), false)!.Position;
            Point point = plot.TranslatePoint(new Point(onSeries.X, onSeries.Y), window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            Settle();
            Assert.AreEqual(1, plot.GetVisualDescendants().OfType<Wpf.TrackerControl>().Count(), "the tracker shows while pressed");
            window.MouseUp(point, MouseButton.Left);
            Settle();
            Assert.AreEqual(0, plot.GetVisualDescendants().OfType<Wpf.TrackerControl>().Count());
            window.Close();
        });
    }

    [TestMethod]
    public void PanMode_ALeftDragMovesTheAxes()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, toolbar, _) = ShowDemo();
            Click(window, toolbar.PanButton);
            Assert.IsTrue(toolbar.PanButton.IsChecked);
            double minimum = XAxis(plot).ActualMinimum, range = XAxis(plot).ActualMaximum - minimum;

            Drag(window, PlotAreaPoint(window, plot, 0.5, 0.5), PlotAreaPoint(window, plot, 0.75, 0.5));

            Assert.IsTrue(XAxis(plot).ActualMinimum < minimum - range * 0.2, $"panned left: {minimum} to {XAxis(plot).ActualMinimum}");
            Assert.AreEqual(range, XAxis(plot).ActualMaximum - XAxis(plot).ActualMinimum, range * 1e-9, "without zooming");
            window.Close();
        });
    }

    [TestMethod]
    public void ZoomMode_ALeftDragZoomsToTheRectangle_AndZoomExtentsResets()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, toolbar, _) = ShowDemo();
            double xMinimum = XAxis(plot).ActualMinimum, xMaximum = XAxis(plot).ActualMaximum;
            double yMinimum = YAxis(plot).ActualMinimum, yMaximum = YAxis(plot).ActualMaximum;
            Click(window, toolbar.ZoomButton);
            Assert.IsTrue(toolbar.ZoomButton.IsChecked);

            Point from = PlotAreaPoint(window, plot, 0.25, 0.25), to = PlotAreaPoint(window, plot, 0.5, 0.75);
            DataPoint topLeft = DataPointAt(window, plot, from), bottomRight = DataPointAt(window, plot, to);
            Drag(window, from, to);

            double tolerance = (xMaximum - xMinimum) * 0.01;
            Assert.AreEqual(topLeft.X, XAxis(plot).ActualMinimum, tolerance);
            Assert.AreEqual(bottomRight.X, XAxis(plot).ActualMaximum, tolerance);
            Assert.AreEqual(bottomRight.Y, YAxis(plot).ActualMinimum, (yMaximum - yMinimum) * 0.01);
            Assert.AreEqual(topLeft.Y, YAxis(plot).ActualMaximum, (yMaximum - yMinimum) * 0.01);

            Click(window, toolbar.ZoomAllButton);
            Assert.AreEqual(xMinimum, XAxis(plot).ActualMinimum, 1e-9);
            Assert.AreEqual(xMaximum, XAxis(plot).ActualMaximum, 1e-9);
            Assert.AreEqual(yMinimum, YAxis(plot).ActualMinimum, 1e-9);
            Assert.AreEqual(yMaximum, YAxis(plot).ActualMaximum, 1e-9);
            Assert.IsTrue(toolbar.ZoomButton.IsChecked, "zoom extents doesn't change the mode");
            window.Close();
        });
    }

    [TestMethod]
    public void EachMode_SetsItsCursor()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, toolbar, _) = ShowDemo();
            Click(window, toolbar.PanButton);
            Cursor? pan = plot.Cursor;
            Assert.AreSame(pan, plot.PanCursor);
            Assert.AreSame(pan, plot.DefaultPlotCursor);

            Click(window, toolbar.ZoomButton);
            Cursor? zoom = plot.Cursor;
            Assert.IsNotNull(zoom);
            Assert.AreNotSame(pan, zoom);
            Assert.AreSame(zoom, plot.DefaultPlotCursor);

            Click(window, toolbar.PointerButton);
            Assert.AreSame(WpfCompat.Input.Cursors.Arrow, plot.Cursor);
            window.Close();
        });
    }

    /// <summary>The toolbar's cursors are upstream's cursor files, read with their hot spots.</summary>
    [TestMethod]
    [DataRow(nameof(Properties.Resources.AddPointCursor), 7, 7)]
    [DataRow(nameof(Properties.Resources.Pan_Hand), 13, 13)]
    [DataRow(nameof(Properties.Resources.Pan_Hand_Closed), 13, 13)]
    [DataRow(nameof(Properties.Resources.SelectPointCursor), 7, 7)]
    [DataRow(nameof(Properties.Resources.ZoomIn), 10, 10)]
    public void CursorFiles_AreEmbedded_AndRead(string name, int hotSpotX, int hotSpotY)
    {
        var data = (byte[])typeof(Properties.Resources).GetProperty(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
        Assert.IsTrue(WpfCompat.Input.WpfCursor.TryDecode(data, out PixelSize size, out byte[]? pixels, out PixelPoint hotSpot));
        Assert.AreEqual(new PixelSize(32, 32), size);
        Assert.AreEqual(new PixelPoint(hotSpotX, hotSpotY), hotSpot);
        Assert.IsTrue(pixels!.Where((_, i) => i % 4 == 3).Any(alpha => alpha != 0), "the cursor draws something");
    }

    [TestMethod]
    public void PropertiesButton_AsksForTheProperties()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, toolbar, _) = ShowDemo();
            var calls = new List<(Wpf.Plot Plot, bool Open, OxyPlotPropertiesControl.PropertyEXP? Expander, object Selected)>();
            toolbar.PropertiesCalled += (p, open, expander, selected) => calls.Add((p, open, expander, selected));

            Click(window, toolbar.PropertiesButton);

            Assert.AreEqual(1, calls.Count);
            Assert.AreSame(plot, calls[0].Plot);
            Assert.IsTrue(calls[0].Open);
            Assert.IsNull(calls[0].Expander);
            Assert.IsNull(calls[0].Selected);
            window.Close();
        });
    }
}
