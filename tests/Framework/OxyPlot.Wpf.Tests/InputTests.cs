using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace OxyPlot.Wpf.Tests;

/// <summary>
/// Mouse input reaches OxyPlot's controller as in WPF: the wheel zooms (coalesced per dispatcher
/// tick), a right drag pans, and the context menu opens only on a right click without a drag.
/// </summary>
[TestClass]
public sealed class InputTests
{
    private static Point PlotCenter(Plot plot) =>
        new Point(plot.ActualModel.PlotArea.Center.X, plot.ActualModel.PlotArea.Center.Y);

    [TestMethod]
    public void MouseWheel_ZoomsTheAxes()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            var (window, _) = RenderingTests.Show(plot);
            var axis = plot.ActualModel.Axes[1];
            double range = axis.ActualMaximum - axis.ActualMinimum;

            window.MouseWheel(PlotCenter(plot), new Vector(0, 1));
            window.MouseWheel(PlotCenter(plot), new Vector(0, 1));
            Dispatcher.UIThread.RunJobs();

            double zoomed = axis.ActualMaximum - axis.ActualMinimum;
            Assert.IsTrue(zoomed < range * 0.9, $"zoomed in: {range} to {zoomed}");
            window.Close();
        });
    }

    [TestMethod]
    public void RightDrag_Pans_AndDoesNotOpenTheContextMenu()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            var menu = new ContextMenu { ItemsSource = new[] { "Reset axes" } };
            plot.ContextMenu = menu;
            var (window, _) = RenderingTests.Show(plot);
            var axis = plot.ActualModel.Axes[1];
            double minimum = axis.ActualMinimum;

            var start = PlotCenter(plot);
            window.MouseDown(start, MouseButton.Right);
            window.MouseMove(start + new Vector(0, 40));
            window.MouseUp(start + new Vector(0, 40), MouseButton.Right);
            Dispatcher.UIThread.RunJobs();

            Assert.AreNotEqual(minimum, axis.ActualMinimum, "panned");
            Assert.IsFalse(menu.IsOpen, "a right drag doesn't open the menu");

            window.MouseDown(start, MouseButton.Right);
            window.MouseUp(start, MouseButton.Right);
            Dispatcher.UIThread.RunJobs();
            Assert.IsTrue(menu.IsOpen, "a right click does");
            Assert.AreSame(plot, menu.PlacementTarget);
            menu.Close();
            window.Close();
        });
    }

    [TestMethod]
    public void LeftPress_ShowsTheTracker_UntilRelease()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            var (window, _) = RenderingTests.Show(plot);
            var model = plot.ActualModel;
            var point = model.Series[0].GetNearestPoint(new ScreenPoint(model.PlotArea.Center.X, model.PlotArea.Center.Y), false)!.Position;

            window.MouseDown(new Point(point.X, point.Y), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual(1, plot.GetVisualDescendants().OfType<TrackerControl>().Count(), "the tracker shows while pressed");

            window.MouseUp(new Point(point.X, point.Y), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual(0, plot.GetVisualDescendants().OfType<TrackerControl>().Count(), "and hides on release");
            window.Close();
        });
    }

    [TestMethod]
    public void ResetAxesCommand_ResetsTheAxes()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            var (window, _) = RenderingTests.Show(plot);
            var axis = plot.ActualModel.Axes[1];
            double range = axis.ActualMaximum - axis.ActualMinimum;
            window.MouseWheel(PlotCenter(plot), new Vector(0, 2));
            Dispatcher.UIThread.RunJobs();
            Assert.AreNotEqual(range, axis.ActualMaximum - axis.ActualMinimum);

            ((WpfCompat.Input.RoutedCommand)PlotCommands.ResetAxes).Execute(null, plot);
            Assert.AreEqual(range, axis.ActualMaximum - axis.ActualMinimum, 1e-9, "the plot's command binding resets the axes");
            window.Close();
        });
    }
}
