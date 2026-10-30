using Avalonia;
using Avalonia.Controls;
using ShapePath = Avalonia.Controls.Shapes.Path;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace OxyPlot.Wpf.Tests;

/// <summary>
/// A plot shows in a window with either rendering backend: the theme (Themes/Generic.axaml) templates
/// the view, the model renders after layout, and the series, axes and annotations are drawn.
/// </summary>
[TestClass]
public sealed class RenderingTests
{
    /// <summary>Shows a plot in a window and returns a frame of it.</summary>
    internal static (Window Window, WriteableBitmap Frame) Show(Plot plot, double width = 640, double height = 480)
    {
        var window = new Window { Width = width, Height = height, Content = plot };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var frame = window.CaptureRenderedFrame()!;
        return (window, frame);
    }

    /// <summary>Counts the frame's pixels close to <paramref name="color"/>.</summary>
    internal static int CountPixels(WriteableBitmap frame, Color color, int tolerance = 40)
    {
        using var buffer = frame.Lock();
        var bytes = new byte[buffer.RowBytes * buffer.Size.Height];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);
        int count = 0;
        for (int y = 0; y < buffer.Size.Height; y++)
        {
            for (int x = 0; x < buffer.Size.Width; x++)
            {
                int i = (y * buffer.RowBytes) + (x * 4);
                bool rgba = buffer.Format == PixelFormat.Rgba8888;
                byte r = bytes[rgba ? i : i + 2], g = bytes[i + 1], b = bytes[rgba ? i + 2 : i];
                if (Math.Abs(r - color.R) <= tolerance && Math.Abs(g - color.G) <= tolerance && Math.Abs(b - color.B) <= tolerance)
                {
                    count++;
                }
            }
        }

        return count;
    }

    [TestMethod]
    public void FrequencyPlot_RendersWithTheDrawingVisualBackend()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            Assert.AreEqual(RenderingBackend.DrawingVisual, plot.RenderingBackend, "the fork's default backend");
            var (window, frame) = Show(plot);

            Assert.IsNotNull(plot.grid, "the generic theme templates the plot view");
            Assert.IsInstanceOfType<DrawingVisualHost>(plot.grid.Children[0]);
            Assert.AreEqual(Colors.White, ((ISolidColorBrush)plot.Background!).Color, "the generic theme's background");

            var model = plot.ActualModel;
            Assert.IsTrue(model.PlotArea.Width > 300 && model.PlotArea.Height > 200, $"plot area {model.PlotArea}");
            Assert.IsInstanceOfType<Axes.NormalProbabilityAxis>(model.Axes[0]);
            Assert.AreEqual(0.0000001, model.Axes[0].ActualMinimum, 1e-12, "the probability axis is set up as in the fork");
            Assert.IsTrue(CountPixels(frame, TestPlots.FittedColor) > 300, "the fitted line is drawn");
            Assert.IsTrue(CountPixels(frame, Colors.SteelBlue) > 20, "the observed points are drawn");
            Assert.IsTrue(CountPixels(frame, Colors.DarkGreen, 30) > 50, "the line annotation is drawn");

            // The tick labels and titles are text the toolbar can hit-test.
            var title = plot.HitTestRenderedText(new Point(model.TitleArea.Center.X, model.TitleArea.Top + 4));
            Assert.AreEqual("Annual peak flow", (title as DrawingVisualRenderContext.TextHitResult)?.Text);
            window.Close();
        });
    }

    [TestMethod]
    public void FrequencyPlot_RendersWithTheCanvasBackend()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            plot.RenderingBackend = RenderingBackend.Canvas;
            var (window, frame) = Show(plot);

            var canvas = plot.canvas;
            Assert.IsInstanceOfType<Canvas>(plot.grid.Children[0]);
            Assert.AreSame(plot.grid.Children[0], canvas, "the Canvas backend's canvas is the plot's canvas");
            Assert.IsTrue(canvas.Children.OfType<TextBlock>().Any(t => t.Text == "Flow (cfs)"), "the axis title is a text block");
            Assert.IsTrue(canvas.Children.OfType<ShapePath>().Count() > 5, "lines and markers are paths");
            Assert.IsTrue(CountPixels(frame, TestPlots.FittedColor) > 300, "the fitted line is drawn");
            window.Close();
        });
    }

    [TestMethod]
    public void DisableShapeAntiAliasing_LeavesTextAntiAliased()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            plot.DisableShapeAntiAliasing = true;
            var (window, frame) = Show(plot);

            // The title is text of one colour on white: anti-aliased, its edges have shades between the
            // two; aliased, it has just the two colours.
            var title = plot.ActualModel.TitleArea;
            using var buffer = frame.Lock();
            var bytes = new byte[buffer.RowBytes * buffer.Size.Height];
            System.Runtime.InteropServices.Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);
            var colors = new HashSet<int>();
            for (int y = (int)title.Top; y < (int)title.Bottom; y++)
            {
                for (int x = (int)title.Left; x < (int)title.Right; x++)
                {
                    colors.Add(BitConverter.ToInt32(bytes, (y * buffer.RowBytes) + (x * 4)));
                }
            }

            Assert.IsTrue(colors.Count > 10, $"anti-aliased title text: {colors.Count} colours");
            window.Close();
        });
    }

    [TestMethod]
    public void Plot_RendersAgain_WhenItsSizeChanges()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            var (window, _) = Show(plot, 400, 300);
            var small = plot.ActualModel.PlotArea;

            window.Width = 800;
            window.Height = 600;
            Dispatcher.UIThread.RunJobs();
            window.CaptureRenderedFrame();
            Dispatcher.UIThread.RunJobs();

            Assert.IsTrue(plot.ActualModel.PlotArea.Width > small.Width + 200, $"{small} then {plot.ActualModel.PlotArea}");
            window.Close();
        });
    }

    [TestMethod]
    public void HiddenSeries_IsNotDrawn_AndFollowsIsVisible()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            var hidden = (LineSeries)plot.Series[2];
            Show(plot).Window.Close();

            Assert.IsFalse(hidden.IsVisible, "Avalonia's IsVisible follows WPF's Visibility");
            Assert.IsFalse(hidden.InternalSeries.IsVisible);

            hidden.IsVisible = true;
            Assert.AreEqual(WpfCompat.Visibility.Visible, hidden.Visibility);
            Assert.IsTrue(hidden.InternalSeries.IsVisible);

            hidden.IsVisible = false;
            Assert.AreEqual(WpfCompat.Visibility.Collapsed, hidden.Visibility, "hiding through IsVisible collapses, as WPF's IsVisible can't say Hidden");
        });
    }

    [TestMethod]
    public void Tracker_ShowsTheDefaultTemplate_OverTheOverlays()
    {
        HeadlessSession.Run(() =>
        {
            var plot = TestPlots.FrequencyPlot();
            var (window, _) = Show(plot);

            var series = plot.ActualModel.Series[0];
            var hit = series.GetNearestPoint(new ScreenPoint(plot.ActualModel.PlotArea.Center.X, plot.ActualModel.PlotArea.Center.Y), false);
            Assert.IsNotNull(hit);
            plot.ShowTracker(hit);
            Dispatcher.UIThread.RunJobs();
            window.CaptureRenderedFrame();

            var tracker = plot.GetVisualDescendants().OfType<TrackerControl>().Single();
            Assert.AreEqual(hit.Position, tracker.Position);
            var text = tracker.GetVisualDescendants().OfType<TextBlock>().Single();
            StringAssert.StartsWith(text.Text, "Fitted: ", "the default template shows the hit's text");

            plot.HideTracker();
            Assert.IsFalse(plot.GetVisualDescendants().OfType<TrackerControl>().Any());
            window.Close();
        });
    }
}
