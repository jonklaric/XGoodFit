using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using OxyPlot;
using static OxyPlotControls.Tests.PlotTesting;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls.Tests;

/// <summary>
/// The toolbar's Add Annotation menu: pick an annotation, then click or drag on the plot to place it.
/// The new annotation goes to the plot, and the toolbar asks for its properties, as upstream's does.
/// </summary>
[TestClass]
public sealed class AddAnnotationTests
{
    private static readonly string[] MenuHeaders =
    [
        "Arrow Annotation", "Text Annotation", "Vertical Line Annotation", "Horizontal Line Annotation", "Rectangle Annotation",
        "Ellipse Annotation", "Point Annotation", "Polygon Annotation", "Polyline Annotation",
    ];

    // Opens the toolbar's annotation menu and picks the item with the header.
    private static void Pick(Window window, OxyPlotToolbar toolbar, string header)
    {
        ContextMenu? menu = null;
        using (ContextMenu.OpenedEvent.AddClassHandler<ContextMenu>((m, _) => menu = m))
        {
            Click(window, toolbar.AddAnnotationToggleButton);
        }
        Assert.IsNotNull(menu, "the menu opens");
        Assert.IsTrue(menu.IsOpen);
        CollectionAssert.AreEqual(MenuHeaders, menu.Items.Cast<MenuItem>().Select(i => (string)i.Header!).ToArray());
        Assert.IsTrue(menu.Items.Cast<MenuItem>().All(i => i.Icon != null), "with the annotations' icons");
        RaiseClick(menu.Items.Cast<MenuItem>().Single(i => (string)i.Header! == header));
        menu.Close();
        Settle();
    }

    private static List<(Wpf.Plot Plot, bool Open, OxyPlotPropertiesControl.PropertyEXP? Expander, object Selected)> RecordPropertiesCalls(OxyPlotToolbar toolbar)
    {
        var calls = new List<(Wpf.Plot, bool, OxyPlotPropertiesControl.PropertyEXP?, object)>();
        toolbar.PropertiesCalled += (p, open, expander, selected) => calls.Add((p, open, expander, selected));
        return calls;
    }

    [TestMethod]
    public void HorizontalLine_IsAddedWhereThePlotIsClicked()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, toolbar, _) = ShowDemo();
            var calls = RecordPropertiesCalls(toolbar);
            Pick(window, toolbar, "Horizontal Line Annotation");
            Assert.AreSame(plot.DefaultPlotCursor, plot.Cursor);
            Assert.AreNotSame(WpfCompat.Input.Cursors.Arrow, plot.Cursor, "the add-point cursor");

            Point at = PlotAreaPoint(window, plot, 0.4, 0.3);
            DataPoint data = DataPointAt(window, plot, at);
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Settle();

            var line = (Wpf.LineAnnotation)plot.Annotations.Single();
            Assert.AreEqual(OxyPlot.Annotations.LineAnnotationType.Horizontal, line.Type);
            Assert.AreEqual("Horizontal Line Annotation", line.Text);
            Assert.AreEqual(data.Y, line.Y, Math.Abs(data.Y) * 0.01 + 1e-6);
            Assert.AreEqual(1, plot.ActualModel.Annotations.Count, "the annotation is in the model");
            Assert.AreEqual(1, calls.Count);
            Assert.AreEqual(OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, calls[0].Expander);
            Assert.AreSame(line, calls[0].Selected);

            // The next click doesn't add another: adding stops once the annotation is placed.
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Settle();
            Assert.AreEqual(1, plot.Annotations.Count);
            window.Close();
        });
    }

    [TestMethod]
    public void Text_IsAddedWhereThePlotIsClicked()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, toolbar, _) = ShowDemo();
            Pick(window, toolbar, "Text Annotation");

            Point at = PlotAreaPoint(window, plot, 0.6, 0.5);
            DataPoint data = DataPointAt(window, plot, at);
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Settle();

            var text = (Wpf.TextAnnotation)plot.Annotations.Single();
            Assert.AreEqual("Text Annotation", text.Text);
            Assert.AreEqual(data.X, text.TextPosition.X, 0.05);
            Assert.AreEqual(data.Y, text.TextPosition.Y, 0.005);
            window.Close();
        });
    }

    [TestMethod]
    public void Rectangle_IsTheDraggedRectangle()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, toolbar, _) = ShowDemo();
            Pick(window, toolbar, "Rectangle Annotation");

            Point from = PlotAreaPoint(window, plot, 0.2, 0.2), to = PlotAreaPoint(window, plot, 0.5, 0.6);
            DataPoint first = DataPointAt(window, plot, from), second = DataPointAt(window, plot, to);
            Drag(window, from, to);

            // As upstream: the corner pressed is the minimum, the corner released the maximum.
            var rectangle = (Wpf.RectangleAnnotation)plot.Annotations.Single();
            Assert.AreEqual(first.X, rectangle.MinimumX, 0.05);
            Assert.AreEqual(first.Y, rectangle.MinimumY, 0.005);
            Assert.AreEqual(second.X, rectangle.MaximumX, 0.05);
            Assert.AreEqual(second.Y, rectangle.MaximumY, 0.005);
            window.Close();
        });
    }

    [TestMethod]
    public void Arrow_PointsWhereTheDragEnds()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, toolbar, _) = ShowDemo();
            Pick(window, toolbar, "Arrow Annotation");

            Point from = PlotAreaPoint(window, plot, 0.3, 0.3), to = PlotAreaPoint(window, plot, 0.6, 0.5);
            DataPoint start = DataPointAt(window, plot, from), end = DataPointAt(window, plot, to);
            Drag(window, from, to);

            var arrow = (Wpf.ArrowAnnotation)plot.Annotations.Single();
            Assert.AreEqual(start.X, arrow.StartPoint.X, 0.05);
            Assert.AreEqual(start.Y, arrow.StartPoint.Y, 0.005);
            Assert.AreEqual(end.X, arrow.EndPoint.X, 0.05);
            Assert.AreEqual(end.Y, arrow.EndPoint.Y, 0.005);
            window.Close();
        });
    }

    [TestMethod]
    public void Escape_CancelsAdding()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, toolbar, _) = ShowDemo();
            Pick(window, toolbar, "Text Annotation");
            plot.Focus();
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Settle();

            Point at = PlotAreaPoint(window, plot, 0.6, 0.5);
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Settle();
            Assert.AreEqual(0, plot.Annotations.Count);
            window.Close();
        });
    }
}
