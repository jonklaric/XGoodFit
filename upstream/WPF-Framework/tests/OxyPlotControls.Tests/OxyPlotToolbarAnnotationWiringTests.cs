using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Wpf.Serialization;
using Xunit;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls.Tests;

public class OxyPlotToolbarAnnotationWiringTests
{
    [StaFact]
    public void PlotAssignment_AttachesMouseHandlersToDeserializedAnnotations()
    {
        var sourcePlot = new Wpf.Plot();
        sourcePlot.Annotations.Add(new Wpf.ArrowAnnotation
        {
            Text = "Move me",
            StartPoint = new DataPoint(0, 0),
            EndPoint = new DataPoint(1, 1)
        });

        var element = PlotSerializer.ToXElement(sourcePlot);
        var plot = new Wpf.Plot();
        PlotSerializer.FromXElement(plot, element);

        var annotation = Assert.IsType<Wpf.ArrowAnnotation>(Assert.Single(plot.Annotations));
        Assert.Equal(0, CountHandlers(annotation.InternalAnnotation, "MouseDown"));
        Assert.Equal(0, CountHandlers(annotation.InternalAnnotation, "MouseMove"));
        Assert.Equal(0, CountHandlers(annotation.InternalAnnotation, "MouseUp"));

        var toolbar = new OxyPlotToolbar { Plot = plot };

        Assert.Equal(1, CountHandlers(annotation.InternalAnnotation, "MouseDown"));
        Assert.Equal(1, CountHandlers(annotation.InternalAnnotation, "MouseMove"));
        Assert.Equal(1, CountHandlers(annotation.InternalAnnotation, "MouseUp"));

        toolbar.Plot = null!;
        toolbar.Plot = plot;

        Assert.Equal(1, CountHandlers(annotation.InternalAnnotation, "MouseDown"));
        Assert.Equal(1, CountHandlers(annotation.InternalAnnotation, "MouseMove"));
        Assert.Equal(1, CountHandlers(annotation.InternalAnnotation, "MouseUp"));
    }

    [StaFact]
    public void TextAnnotationScreenPoint_UsesPlotAreaCenterWhenTextPositionIsUndefined()
    {
        var plot = new Wpf.Plot();
        var annotation = new Wpf.TextAnnotation { Text = "Move me" };
        plot.Annotations.Add(annotation);

        var toolbar = new OxyPlotToolbar { Plot = plot };
        plot.InvalidatePlot(true);
        ((IPlotModel)plot.ActualModel).Render(new PdfRenderContext(400, 300, OxyColors.White), new OxyRect(0, 0, 400, 300));

        var point = GetCurrentTextAnnotationScreenPoint(toolbar, annotation);

        Assert.False(double.IsNaN(point.X));
        Assert.False(double.IsNaN(point.Y));
        Assert.Equal(plot.ActualModel.PlotArea.Center.X, point.X, 6);
        Assert.Equal(plot.ActualModel.PlotArea.Center.Y, point.Y, 6);
    }

    [StaFact]
    public void AnnotationDrag_DoesNotChangeVisualColor()
    {
        foreach (var testCase in CreateAnnotationColorCases())
        {
            var plot = new Wpf.Plot();
            plot.Annotations.Add(testCase.Annotation);

            var toolbar = new OxyPlotToolbar { Plot = plot };
            Assert.NotNull(toolbar);
            RenderPlot(plot);

            var originalColor = testCase.GetColor(testCase.Annotation);
            var position = plot.ActualModel.PlotArea.Center;

            InvokeMouseDown(testCase.Annotation.InternalAnnotation, position, testCase.HitIndex);
            Assert.Equal(originalColor, testCase.GetColor(testCase.Annotation));

            InvokeMouseUp(testCase.Annotation.InternalAnnotation, position);
            Assert.Equal(originalColor, testCase.GetColor(testCase.Annotation));
            Assert.False(testCase.Annotation.SuppressPropertyChanged);
        }
    }

    [StaFact]
    public void AddArrowAnnotation_UsesToolbarArrowHeadDefaults()
    {
        var plot = new Wpf.Plot();
        plot.Axes.Add(new Wpf.LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 1 });
        plot.Axes.Add(new Wpf.LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1 });
        SetPlotGrid(plot);

        var toolbar = new OxyPlotToolbar { Plot = plot };
        RenderPlot(plot);

        InvokePrivate(toolbar, "AddArrowAnnotationItem_Click", toolbar, new RoutedEventArgs());
        InvokePrivate(
            toolbar,
            "PlotModelMouseDown",
            plot.ActualModel,
            new OxyMouseDownEventArgs
            {
                ChangedButton = OxyMouseButton.Left,
                ClickCount = 1,
                Position = plot.ActualModel.PlotArea.Center
            });

        var arrow = Assert.IsType<Wpf.ArrowAnnotation>(Assert.Single(plot.Annotations));
        Assert.Equal(6, arrow.HeadLength);
        Assert.Equal(2, arrow.HeadWidth);
        Assert.Equal(1, arrow.Veeness);
    }

    private static IEnumerable<AnnotationColorCase> CreateAnnotationColorCases()
    {
        yield return new AnnotationColorCase(
            new Wpf.ArrowAnnotation
            {
                Text = "Arrow",
                Color = Colors.SteelBlue,
                StartPoint = new DataPoint(0, 0),
                EndPoint = new DataPoint(1, 1)
            },
            annotation => ((Wpf.ArrowAnnotation)annotation).Color,
            1);

        yield return new AnnotationColorCase(
            new Wpf.TextAnnotation
            {
                Text = "Text",
                Background = Colors.LightGoldenrodYellow,
                TextPosition = new DataPoint(0.5, 0.5)
            },
            annotation => ((Wpf.TextAnnotation)annotation).Background,
            0);

        yield return new AnnotationColorCase(
            new Wpf.RectangleAnnotation
            {
                Text = "Rectangle",
                Fill = Colors.LightGreen,
                MinimumX = 0,
                MaximumX = 1,
                MinimumY = 0,
                MaximumY = 1
            },
            annotation => ((Wpf.RectangleAnnotation)annotation).Fill,
            0);

        yield return new AnnotationColorCase(
            new Wpf.EllipseAnnotation
            {
                Text = "Ellipse",
                Fill = Colors.LightSkyBlue,
                MinimumX = 0,
                MaximumX = 1,
                MinimumY = 0,
                MaximumY = 1
            },
            annotation => ((Wpf.EllipseAnnotation)annotation).Fill,
            0);

        yield return new AnnotationColorCase(
            new Wpf.PointAnnotation
            {
                Text = "Point",
                Fill = Colors.Gold,
                X = 0.5,
                Y = 0.5
            },
            annotation => ((Wpf.PointAnnotation)annotation).Fill,
            0);

        yield return new AnnotationColorCase(
            new Wpf.PolygonAnnotation
            {
                Text = "Polygon",
                Fill = Colors.MediumPurple,
                Points = new List<DataPoint>
                {
                    new(0, 0),
                    new(1, 0),
                    new(1, 1)
                }
            },
            annotation => ((Wpf.PolygonAnnotation)annotation).Fill,
            0);

        yield return new AnnotationColorCase(
            new Wpf.PolylineAnnotation
            {
                Text = "Polyline",
                Color = Colors.DarkCyan,
                Points = new List<DataPoint>
                {
                    new(0, 0),
                    new(1, 0),
                    new(1, 1)
                }
            },
            annotation => ((Wpf.PolylineAnnotation)annotation).Color,
            0);

        yield return new AnnotationColorCase(
            new Wpf.LineAnnotation
            {
                Text = "Line",
                Color = Colors.DarkOrange,
                Type = LineAnnotationType.Vertical,
                X = 0.5,
                Y = 0.5
            },
            annotation => ((Wpf.LineAnnotation)annotation).Color,
            0);
    }

    private static void RenderPlot(Wpf.Plot plot)
    {
        plot.InvalidatePlot(true);
        ((IPlotModel)plot.ActualModel).Render(new PdfRenderContext(400, 300, OxyColors.White), new OxyRect(0, 0, 400, 300));
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

    private static void InvokeMouseUp(Element element, ScreenPoint position)
    {
        var handler = GetHandler<EventHandler<OxyMouseEventArgs>>(element, "MouseUp");
        handler(element, new OxyMouseEventArgs { Position = position });
    }

    private static int CountHandlers(Element element, string eventName)
    {
        var eventField = typeof(Element).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(eventField);

        var handler = eventField!.GetValue(element) as MulticastDelegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private static ScreenPoint GetCurrentTextAnnotationScreenPoint(OxyPlotToolbar toolbar, Wpf.TextAnnotation annotation)
    {
        var method = typeof(OxyPlotToolbar).GetMethod(
            "GetCurrentTextAnnotationScreenPoint",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        return Assert.IsType<ScreenPoint>(method!.Invoke(toolbar, new object[] { annotation }));
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

    private sealed record AnnotationColorCase(
        Wpf.Annotation Annotation,
        Func<Wpf.Annotation, Color> GetColor,
        double HitIndex);
}
