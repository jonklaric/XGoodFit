using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using Xunit;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls.Tests;

public class OxyPlotToolbarAnnotationAxisBehaviorTests
{
    private const double RenderWidth = 400;
    private const double RenderHeight = 300;

    [StaTheory]
    [InlineData("AddRectangleAnnotationItem_Click")]
    [InlineData("AddEllipseAnnotationItem_Click")]
    public void TinyRectangleAndEllipsePlacement_OnLogLogAxes_CreatesValidVisibleBounds(string addMethodName)
    {
        var plot = CreateLogLogPlot();
        var toolbar = CreateToolbar(plot);
        RenderPlot(plot);

        InvokePrivate(toolbar, addMethodName, toolbar, new RoutedEventArgs());
        var clickPosition = plot.ActualModel.PlotArea.Center;

        InvokePrivate(toolbar, "PlotModelMouseDown", plot.ActualModel, NewMouseDown(clickPosition));
        InvokePrivate(toolbar, "PlotModelMouseUp", plot.ActualModel, new OxyMouseEventArgs { Position = clickPosition });

        var annotation = Assert.Single(plot.Annotations);
        AssertShapeBoundsAreValidAndVisible(annotation);
    }

    [StaTheory]
    [InlineData("LogLog", "AddArrowAnnotationItem_Click")]
    [InlineData("LogLog", "AddTextAnnotationItem_Click")]
    [InlineData("DateTime", "AddArrowAnnotationItem_Click")]
    [InlineData("DateTime", "AddTextAnnotationItem_Click")]
    [InlineData("Reversed", "AddArrowAnnotationItem_Click")]
    [InlineData("Reversed", "AddTextAnnotationItem_Click")]
    public void ArrowAndTextPlacement_CreatesValidCoordinatesAcrossAxisTypes(string axisKind, string addMethodName)
    {
        var plot = CreatePlot(axisKind);
        var toolbar = CreateToolbar(plot);
        RenderPlot(plot);

        InvokePrivate(toolbar, addMethodName, toolbar, new RoutedEventArgs());
        var startPosition = plot.ActualModel.PlotArea.Center;
        var endPosition = new ScreenPoint(startPosition.X + 18, startPosition.Y - 12);

        InvokePrivate(toolbar, "PlotModelMouseDown", plot.ActualModel, NewMouseDown(startPosition));
        InvokePrivate(toolbar, "PlotModelMouseMove", plot.ActualModel, new OxyMouseEventArgs { Position = endPosition });
        InvokePrivate(toolbar, "PlotModelMouseUp", plot.ActualModel, new OxyMouseEventArgs { Position = endPosition });

        var annotation = Assert.Single(plot.Annotations);
        if (annotation is Wpf.ArrowAnnotation arrow)
        {
            AssertValidPoint(arrow.InternalAnnotation.XAxis!, arrow.InternalAnnotation.YAxis!, arrow.StartPoint);
            AssertValidPoint(arrow.InternalAnnotation.XAxis!, arrow.InternalAnnotation.YAxis!, arrow.EndPoint);
        }
        else
        {
            var text = Assert.IsType<Wpf.TextAnnotation>(annotation);
            AssertValidPoint(text.InternalAnnotation.XAxis!, text.InternalAnnotation.YAxis!, text.TextPosition);
        }
    }

    [StaTheory]
    [InlineData("LogLog")]
    [InlineData("DateTime")]
    [InlineData("Reversed")]
    public void PointAnnotationDrag_FollowsScreenDeltaAcrossAxisTypes(string axisKind)
    {
        var plot = CreatePlot(axisKind);
        var initialPoint = GetInitialPoint(axisKind);
        var point = new Wpf.PointAnnotation
        {
            Text = "Point",
            X = initialPoint.X,
            Y = initialPoint.Y
        };
        plot.Annotations.Add(point);

        _ = CreateToolbar(plot);
        RenderPlot(plot);

        var startScreenPoint = point.InternalAnnotation.Transform(new DataPoint(point.X, point.Y));
        var endScreenPoint = new ScreenPoint(startScreenPoint.X + 24, startScreenPoint.Y - 17);

        InvokeMouseDown(point.InternalAnnotation, startScreenPoint, 0);
        InvokeMouseMove(point.InternalAnnotation, endScreenPoint);
        InvokeMouseUp(point.InternalAnnotation, endScreenPoint);

        AssertValidPoint(point.InternalAnnotation.XAxis!, point.InternalAnnotation.YAxis!, new DataPoint(point.X, point.Y));
        var movedScreenPoint = point.InternalAnnotation.Transform(new DataPoint(point.X, point.Y));
        Assert.Equal(endScreenPoint.X, movedScreenPoint.X, 1);
        Assert.Equal(endScreenPoint.Y, movedScreenPoint.Y, 1);
    }

    [StaTheory]
    [InlineData("LogLog", LineAnnotationType.Vertical)]
    [InlineData("DateTime", LineAnnotationType.Horizontal)]
    [InlineData("Reversed", LineAnnotationType.Vertical)]
    public void LineAnnotationDrag_FollowsMouseAcrossAxisTypes(string axisKind, LineAnnotationType lineType)
    {
        var plot = CreatePlot(axisKind);
        var initialPoint = GetInitialPoint(axisKind);
        var line = new Wpf.LineAnnotation
        {
            Text = "Line",
            Type = lineType,
            X = initialPoint.X,
            Y = initialPoint.Y
        };
        plot.Annotations.Add(line);

        _ = CreateToolbar(plot);
        RenderPlot(plot);

        var startScreenPoint = line.InternalAnnotation.Transform(new DataPoint(line.X, line.Y));
        var endScreenPoint = new ScreenPoint(startScreenPoint.X + 19, startScreenPoint.Y + 13);

        InvokeMouseDown(line.InternalAnnotation, startScreenPoint, 0);
        InvokeMouseMove(line.InternalAnnotation, endScreenPoint);
        InvokeMouseUp(line.InternalAnnotation, endScreenPoint);

        AssertValidPoint(line.InternalAnnotation.XAxis!, line.InternalAnnotation.YAxis!, new DataPoint(line.X, line.Y));
        var movedScreenPoint = line.InternalAnnotation.Transform(new DataPoint(line.X, line.Y));
        Assert.Equal(endScreenPoint.X, movedScreenPoint.X, 1);
        Assert.Equal(endScreenPoint.Y, movedScreenPoint.Y, 1);
    }

    [StaTheory]
    [InlineData("Polygon")]
    [InlineData("Polyline")]
    public void PolygonAndPolylineVertexDrag_UsesScreenToleranceOnLogLogAxes(string annotationKind)
    {
        var plot = CreateLogLogPlot();
        Wpf.Annotation annotation = annotationKind == "Polygon"
            ? new Wpf.PolygonAnnotation
            {
                Points = new List<DataPoint>
                {
                    new(10, 10),
                    new(100, 10),
                    new(100, 100)
                }
            }
            : new Wpf.PolylineAnnotation
            {
                Points = new List<DataPoint>
                {
                    new(10, 10),
                    new(100, 10),
                    new(100, 100)
                }
            };

        plot.Annotations.Add(annotation);
        _ = CreateToolbar(plot);
        RenderPlot(plot);

        var points = GetPoints(annotation);
        var originalVertexScreenPoint = annotation.InternalAnnotation.Transform(points[0]);
        var mouseDownPoint = new ScreenPoint(originalVertexScreenPoint.X + 6, originalVertexScreenPoint.Y + 4);
        var mouseMovePoint = new ScreenPoint(mouseDownPoint.X + 21, mouseDownPoint.Y - 11);

        InvokeMouseDown(annotation.InternalAnnotation, mouseDownPoint, 0);
        InvokeMouseMove(annotation.InternalAnnotation, mouseMovePoint);
        InvokeMouseUp(annotation.InternalAnnotation, mouseMovePoint);

        var movedVertexScreenPoint = annotation.InternalAnnotation.Transform(GetPoints(annotation)[0]);
        Assert.Equal(originalVertexScreenPoint.X + 21, movedVertexScreenPoint.X, 1);
        Assert.Equal(originalVertexScreenPoint.Y - 11, movedVertexScreenPoint.Y, 1);
    }

    [StaFact]
    public void PointPlacement_OnCategoryAxis_CreatesFiniteCoordinates()
    {
        var plot = new Wpf.Plot();
        plot.Axes.Add(new Wpf.CategoryAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = -0.5,
            Maximum = 2.5,
            Labels = new List<string> { "A", "B", "C" }
        });
        plot.Axes.Add(new Wpf.LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = 0,
            Maximum = 100
        });

        var toolbar = CreateToolbar(plot);
        RenderPlot(plot);

        InvokePrivate(toolbar, "AddPointAnnotationItem_Click", toolbar, new RoutedEventArgs());
        var clickPosition = plot.ActualModel.PlotArea.Center;

        InvokePrivate(toolbar, "PlotModelMouseDown", plot.ActualModel, NewMouseDown(clickPosition));

        var point = Assert.IsType<Wpf.PointAnnotation>(Assert.Single(plot.Annotations));
        AssertValidPoint(point.InternalAnnotation.XAxis!, point.InternalAnnotation.YAxis!, new DataPoint(point.X, point.Y));
    }

    private static Wpf.Plot CreatePlot(string axisKind)
    {
        return axisKind switch
        {
            "LogLog" => CreateLogLogPlot(),
            "DateTime" => CreateDateTimePlot(),
            "Reversed" => CreateReversedPlot(),
            _ => throw new ArgumentOutOfRangeException(nameof(axisKind), axisKind, null)
        };
    }

    private static Wpf.Plot CreateLogLogPlot()
    {
        var plot = new Wpf.Plot();
        plot.Axes.Add(new Wpf.LogarithmicAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = 1,
            Maximum = 1000
        });
        plot.Axes.Add(new Wpf.LogarithmicAxis
        {
            Position = AxisPosition.Left,
            Minimum = 1,
            Maximum = 1000
        });
        return plot;
    }

    private static Wpf.Plot CreateDateTimePlot()
    {
        var plot = new Wpf.Plot();
        plot.Axes.Add(new Wpf.DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = OxyPlot.Axes.DateTimeAxis.ToDouble(new DateTime(2026, 1, 1)),
            Maximum = OxyPlot.Axes.DateTimeAxis.ToDouble(new DateTime(2026, 2, 1))
        });
        plot.Axes.Add(new Wpf.LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = 0,
            Maximum = 100
        });
        return plot;
    }

    private static Wpf.Plot CreateReversedPlot()
    {
        var plot = new Wpf.Plot();
        plot.Axes.Add(new Wpf.LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            Maximum = 100,
            StartPosition = 1,
            EndPosition = 0
        });
        plot.Axes.Add(new Wpf.LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = 0,
            Maximum = 100,
            StartPosition = 1,
            EndPosition = 0
        });
        return plot;
    }

    private static DataPoint GetInitialPoint(string axisKind)
    {
        return axisKind switch
        {
            "LogLog" => new DataPoint(10, 10),
            "DateTime" => new DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(new DateTime(2026, 1, 15)), 50),
            "Reversed" => new DataPoint(40, 40),
            _ => throw new ArgumentOutOfRangeException(nameof(axisKind), axisKind, null)
        };
    }

    private static OxyPlotToolbar CreateToolbar(Wpf.Plot plot)
    {
        SetPlotGrid(plot);
        var toolbar = new OxyPlotToolbar { Plot = plot };
        RenderPlot(plot);
        return toolbar;
    }

    private static void AssertShapeBoundsAreValidAndVisible(Wpf.Annotation annotation)
    {
        double minimumX;
        double maximumX;
        double minimumY;
        double maximumY;

        if (annotation is Wpf.RectangleAnnotation rectangle)
        {
            minimumX = rectangle.MinimumX;
            maximumX = rectangle.MaximumX;
            minimumY = rectangle.MinimumY;
            maximumY = rectangle.MaximumY;
        }
        else
        {
            var ellipse = Assert.IsType<Wpf.EllipseAnnotation>(annotation);
            minimumX = ellipse.MinimumX;
            maximumX = ellipse.MaximumX;
            minimumY = ellipse.MinimumY;
            maximumY = ellipse.MaximumY;
        }

        AssertValidPoint(annotation.InternalAnnotation.XAxis!, annotation.InternalAnnotation.YAxis!, new DataPoint(minimumX, minimumY));
        AssertValidPoint(annotation.InternalAnnotation.XAxis!, annotation.InternalAnnotation.YAxis!, new DataPoint(maximumX, maximumY));

        var lowerLeft = annotation.InternalAnnotation.Transform(minimumX, minimumY);
        var upperRight = annotation.InternalAnnotation.Transform(maximumX, maximumY);
        Assert.True(Math.Abs(upperRight.X - lowerLeft.X) >= 9.5);
        Assert.True(Math.Abs(upperRight.Y - lowerLeft.Y) >= 9.5);
    }

    private static IList<DataPoint> GetPoints(Wpf.Annotation annotation)
    {
        return annotation switch
        {
            Wpf.PolygonAnnotation polygon => polygon.Points,
            Wpf.PolylineAnnotation polyline => polyline.Points,
            _ => throw new ArgumentOutOfRangeException(nameof(annotation), annotation, null)
        };
    }

    private static void AssertValidPoint(Axis xAxis, Axis yAxis, DataPoint point)
    {
        Assert.True(point.IsDefined());
        Assert.False(double.IsNaN(point.X));
        Assert.False(double.IsInfinity(point.X));
        Assert.False(double.IsNaN(point.Y));
        Assert.False(double.IsInfinity(point.Y));
        Assert.True(xAxis.IsValidValue(point.X));
        Assert.True(yAxis.IsValidValue(point.Y));
    }

    private static void RenderPlot(Wpf.Plot plot)
    {
        plot.InvalidatePlot(true);
        ((IPlotModel)plot.ActualModel).Render(new PdfRenderContext(RenderWidth, RenderHeight, OxyColors.White), new OxyRect(0, 0, RenderWidth, RenderHeight));
    }

    private static OxyMouseDownEventArgs NewMouseDown(ScreenPoint position)
    {
        return new OxyMouseDownEventArgs
        {
            ChangedButton = OxyMouseButton.Left,
            ClickCount = 1,
            Position = position
        };
    }

    private static void InvokeMouseDown(Element element, ScreenPoint position, double hitIndex)
    {
        var handler = GetHandler<EventHandler<OxyMouseDownEventArgs>>(element, "MouseDown");
        handler(
            element,
            new OxyMouseDownEventArgs
            {
                ChangedButton = OxyMouseButton.Left,
                Position = position,
                HitTestResult = new OxyPlot.HitTestResult(element, position, index: hitIndex)
            });
    }

    private static void InvokeMouseMove(Element element, ScreenPoint position)
    {
        var handler = GetHandler<EventHandler<OxyMouseEventArgs>>(element, "MouseMove");
        handler(element, new OxyMouseEventArgs { Position = position });
    }

    private static void InvokeMouseUp(Element element, ScreenPoint position)
    {
        var handler = GetHandler<EventHandler<OxyMouseEventArgs>>(element, "MouseUp");
        handler(element, new OxyMouseEventArgs { Position = position });
    }

    private static T GetHandler<T>(Element element, string eventName)
        where T : class
    {
        var eventField = typeof(Element).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(eventField);

        var handler = eventField!.GetValue(element) as T;
        Assert.NotNull(handler);
        return handler!;
    }

    private static void InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        method!.Invoke(instance, args);
    }

    private static void SetPlotGrid(Wpf.Plot plot)
    {
        var gridField = plot.GetType().BaseType?.GetField("grid", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(gridField);

        gridField!.SetValue(plot, new System.Windows.Controls.Grid());
    }
}
