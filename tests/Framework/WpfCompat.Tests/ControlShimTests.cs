using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WpfCompat.Input;
using WpfCompat.Media;
using WpfLine = WpfCompat.Shapes.Line;

namespace WpfCompat.Tests;

/// <summary>
/// WpfCompat's control shims: WPF's stretched lines, slider track presses, focus moves, font weight names,
/// template part lookup and dash arrays.
/// </summary>
[TestClass]
public sealed class ControlShimTests
{
    private static Window Show(Control content, double width = 400, double height = 100)
    {
        var window = new Window { Width = width, Height = height, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // The lines a control draws, as (start, end) pairs, by rendering it into a drawing group.
    private static List<(Point Start, Point End, IPen Pen)> RenderLines(Control control)
    {
        var group = new DrawingGroup();
        using (DrawingContext context = group.Open())
        {
            control.Render(context);
        }
        return group.Children.OfType<GeometryDrawing>()
            .Select(d => (d.Geometry!.Bounds.TopLeft, d.Geometry.Bounds.BottomRight, d.Pen!))
            .ToList();
    }

    [TestMethod]
    public void Line_StretchesAThinLineAsWpf_InsetByHalfItsStroke()
    {
        HeadlessSession.Run(() =>
        {
            // A property row's dot leader: a 0-to-1 line with a uniform stretch, at the bottom of a 22-pixel row.
            var line = new WpfLine
            {
                X1 = 0, Y1 = 0, X2 = 1, Y2 = 0, Stroke = Brushes.Gray, StrokeThickness = 1,
                Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Bottom,
                StrokeDashArray = new DoubleCollection([2d, 2d]),
            };
            Window window = Show(new Border { Width = 100, Height = 22, Child = line });

            Assert.AreEqual(new Rect(0, 21, 100, 1), line.Bounds, "as long as the row is wide, and as tall as its stroke");
            var (start, end, pen) = RenderLines(line).Single();
            Assert.AreEqual(new Point(0.5, 0.5), start, "inset by half the stroke, as WPF's GetStretchMetrics does");
            Assert.AreEqual(new Point(99.5, 0.5), end);
            CollectionAssert.AreEqual(new[] { 2d, 2d }, pen.DashStyle!.Dashes!.ToArray());
            window.Close();
        });
    }

    [TestMethod]
    public void Line_DrawsNothingForADashArrayOfZeros_AndKeepsItsCoordinatesUnstretched()
    {
        HeadlessSession.Run(() =>
        {
            var none = new WpfLine { X2 = 1, Stroke = Brushes.Black, StrokeThickness = 2, Stretch = Stretch.Uniform, StrokeDashArray = new DoubleCollection([0d]) };
            var solid = new WpfLine { X2 = 1, Stroke = Brushes.Black, StrokeThickness = 2, Stretch = Stretch.Uniform, StrokeDashArray = new DoubleCollection() };
            var plain = new WpfLine { X1 = 6, Y1 = 18, X2 = 18, Y2 = 6, Stroke = Brushes.Black };
            Window window = Show(new StackPanel { Width = 50, Children = { none, solid, plain } });

            Assert.AreEqual(0, RenderLines(none).Count, "WPF's \"no line\" dash style draws nothing");
            var (start, end, pen) = RenderLines(solid).Single();
            Assert.AreEqual(new Point(1, 1), start, "a 2-pixel line is inset by 1");
            Assert.AreEqual(new Point(49, 1), end);
            Assert.IsNull(pen.DashStyle, "an empty dash array is a solid line");
            Assert.AreEqual(2d, solid.Bounds.Height);

            var (plainStart, plainEnd, _) = RenderLines(plain).Single();
            Assert.AreEqual(new Point(6, 6), plainStart, "without a stretch, the line is drawn where it is");
            Assert.AreEqual(new Point(18, 18), plainEnd);
            window.Close();
        });
    }

    // A horizontal slider whose track presses behave as WPF's, with the press point on its track.
    private static (Window Window, Slider Slider, Track Track) ShowSlider(bool? moveToPoint)
    {
        var slider = new Slider { Width = 300, Minimum = 0, Maximum = 100, LargeChange = 10, SmallChange = 1 };
        if (moveToPoint is not null)
        {
            WpfSlider.SetIsMoveToPointEnabled(slider, moveToPoint);
        }
        Window window = Show(slider);
        Track track = slider.GetVisualDescendants().OfType<Track>().Single();
        return (window, slider, track);
    }

    private static Point OnTrack(Window window, Track track, double x) =>
        track.TranslatePoint(new Point(x, track.Bounds.Height / 2), window)!.Value;

    // Runs the dispatcher, timers included, for a while: the test itself runs as a dispatcher job.
    internal static void Wait(int milliseconds)
    {
        var frame = new DispatcherFrame();
        DispatcherTimer.RunOnce(() => frame.Continue = false, TimeSpan.FromMilliseconds(milliseconds));
        Dispatcher.UIThread.PushFrame(frame);
    }

    [TestMethod]
    public void Slider_WpfDefault_PagesTowardsThePointer_RepeatingUntilTheThumbReachesIt()
    {
        HeadlessSession.Run(() =>
        {
            var (window, slider, track) = ShowSlider(moveToPoint: false);
            foreach (RepeatButton button in track.GetVisualDescendants().OfType<RepeatButton>())
            {
                button.Delay = 500;
                button.Interval = 33;
            }
            Point point = OnTrack(window, track, track.Bounds.Width * 0.6);

            window.MouseDown(point, MouseButton.Left);
            Assert.AreEqual(10d, slider.Value, "the press moves the thumb by LargeChange, as WPF's track repeat button does");

            Wait(300);
            Assert.AreEqual(10d, slider.Value, "nothing more before the repeat delay");

            Wait(900);
            double reached = slider.Value;
            Assert.IsTrue(reached is >= 50 and <= 70, $"the thumb pages until it reaches the pointer, near 60, not to the end: {reached}");

            Wait(300);
            Assert.AreEqual(reached, slider.Value, "it stops once the pointer is no longer over the track button");

            window.MouseUp(point, MouseButton.Left);
            Wait(100);
            Assert.AreEqual(reached, slider.Value);
            window.Close();
        });
    }

    [TestMethod]
    public void Slider_WpfDefault_PagesDownToo_AndSnapsToTicks()
    {
        HeadlessSession.Run(() =>
        {
            var (window, slider, track) = ShowSlider(moveToPoint: false);
            slider.Value = 47;
            slider.IsSnapToTickEnabled = true;
            slider.TickFrequency = 5;
            Dispatcher.UIThread.RunJobs();

            Point point = OnTrack(window, track, 5);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Assert.AreEqual(35d, slider.Value, "47 - 10 snapped to the nearest 5 (WPF's MoveToNextTick)");
            window.Close();
        });
    }

    [TestMethod]
    public void Slider_WpfMoveToPoint_MovesTheThumbOnce_WithoutDraggingIt()
    {
        HeadlessSession.Run(() =>
        {
            var (window, slider, track) = ShowSlider(moveToPoint: true);
            Point point = OnTrack(window, track, track.Bounds.Width / 2);

            window.MouseDown(point, MouseButton.Left);
            Assert.AreEqual(50d, slider.Value, 1, "the thumb moves to the pointer");

            window.MouseMove(OnTrack(window, track, track.Bounds.Width * 0.8), RawInputModifiers.LeftMouseButton);
            Assert.AreEqual(50d, slider.Value, 1, "and doesn't follow it: WPF drags only a pressed thumb");
            window.MouseUp(point, MouseButton.Left);
            window.Close();
        });
    }

    [TestMethod]
    public void Slider_Unset_KeepsAvaloniasMoveAndDrag()
    {
        HeadlessSession.Run(() =>
        {
            var (window, slider, track) = ShowSlider(moveToPoint: null);
            window.MouseDown(OnTrack(window, track, track.Bounds.Width / 2), MouseButton.Left);
            window.MouseMove(OnTrack(window, track, track.Bounds.Width * 0.8), RawInputModifiers.LeftMouseButton);
            Assert.IsTrue(slider.Value > 70, $"Avalonia's slider drags the thumb from the press: {slider.Value}");
            window.MouseUp(OnTrack(window, track, track.Bounds.Width * 0.8), MouseButton.Left);
            window.Close();
        });
    }

    [TestMethod]
    public void MoveFocus_MovesToTheNextOrPreviousElement()
    {
        HeadlessSession.Run(() =>
        {
            TextBox first = new(), second = new(), third = new();
            Window window = Show(new StackPanel { Children = { first, second, third } });
            first.Focus();

            Assert.IsTrue(first.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
            Assert.IsTrue(second.IsFocused);
            Assert.IsTrue(second.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)));
            Assert.IsTrue(first.IsFocused);
            Assert.IsTrue(first.MoveFocus(new TraversalRequest(FocusNavigationDirection.Last)));
            Assert.IsTrue(third.IsFocused);
            window.Close();
        });
    }

    [TestMethod]
    public void FontWeights_AreAvaloniasWeights_WithWpfsNames()
    {
        Assert.AreEqual(FontWeight.SemiBold, FontWeights.SemiBold);
        Assert.AreEqual(FontWeight.Heavy, FontWeights.Heavy);
        CollectionAssert.AreEqual(
            new[] { "Thin", "ExtraLight", "Light", "Normal", "Medium", "SemiBold", "Bold", "ExtraBold", "Black", "ExtraBlack", "350" },
            new[] { FontWeights.Thin, FontWeights.UltraLight, FontWeights.Light, FontWeights.Regular, FontWeights.Medium, FontWeights.DemiBold,
                    FontWeights.Bold, FontWeights.UltraBold, FontWeights.Heavy, FontWeights.UltraBlack, (FontWeight)350 }
                .Select(FontWeights.ToString).ToArray());
        Assert.AreEqual("SemiBold", FontWeights.NameConverter.Convert(FontWeight.SemiBold, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void TemplateFindName_FindsATemplatePart()
    {
        HeadlessSession.Run(() =>
        {
            var comboBox = new ComboBox();
            Window window = Show(comboBox);
            Assert.IsInstanceOfType<Popup>(comboBox.Template.FindName("PART_Popup", comboBox));
            Assert.IsNull(comboBox.Template.FindName("NoSuchPart", comboBox));
            window.Close();
        });
    }

    [TestMethod]
    public void DoubleCollection_IsADashArray_ComparedByReference()
    {
        var dashes = new DoubleCollection([4d, 1d]);
        DoubleCollection copy = dashes.Clone();
        CollectionAssert.AreEqual(dashes.ToArray(), copy.ToArray());
        Assert.AreNotSame(dashes, copy);
        Assert.IsFalse(dashes.Equals(copy), "as WPF's, equal only to itself");
        Assert.AreEqual(System.Globalization.CultureInfo.GetCultureInfo("en-US"), FormattingCulture.Current);
    }
}
