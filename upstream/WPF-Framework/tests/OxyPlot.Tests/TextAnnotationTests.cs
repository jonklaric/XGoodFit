namespace OxyPlot.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;

    [TestFixture]
    public class TextAnnotationTests
    {
        [Test]
        public void Render_DefinedTextPosition_RecomputesAnchorWhenPlotWidthChanges()
        {
            var annotation = new TextAnnotation
            {
                Text = "anchored",
                TextPosition = new DataPoint(5, 5),
                TextHorizontalAlignment = HorizontalAlignment.Left,
                TextVerticalAlignment = VerticalAlignment.Top,
                Padding = new OxyThickness(0),
                StrokeThickness = 0
            };
            var model = CreateModel(annotation);

            var small = RenderAndGetText(model, "anchored", 400, 300);
            var smallExpected = new ScreenPoint(model.DefaultXAxis.Transform(5), model.DefaultYAxis.Transform(5));

            var wide = RenderAndGetText(model, "anchored", 800, 300);
            var wideExpected = new ScreenPoint(model.DefaultXAxis.Transform(5), model.DefaultYAxis.Transform(5));

            Assert.That(small.Point.X, Is.EqualTo(smallExpected.X).Within(1e-6));
            Assert.That(small.Point.Y, Is.EqualTo(smallExpected.Y).Within(1e-6));
            Assert.That(wide.Point.X, Is.EqualTo(wideExpected.X).Within(1e-6));
            Assert.That(wide.Point.Y, Is.EqualTo(wideExpected.Y).Within(1e-6));
            Assert.That(wide.Point.X, Is.GreaterThan(small.Point.X));
        }

        [Test]
        public void Render_UndefinedTextPosition_FallsBackToPlotAreaCenter()
        {
            var annotation = new TextAnnotation
            {
                Text = "centered",
                TextHorizontalAlignment = HorizontalAlignment.Left,
                TextVerticalAlignment = VerticalAlignment.Top,
                Padding = new OxyThickness(0),
                StrokeThickness = 0
            };
            var model = CreateModel(annotation);

            var small = RenderAndGetText(model, "centered", 400, 300);
            var smallExpected = model.PlotArea.Center;

            var wide = RenderAndGetText(model, "centered", 800, 300);
            var wideExpected = model.PlotArea.Center;

            Assert.That(double.IsNaN(small.Point.X), Is.False);
            Assert.That(double.IsNaN(small.Point.Y), Is.False);
            Assert.That(small.Point.X, Is.EqualTo(smallExpected.X).Within(1e-6));
            Assert.That(small.Point.Y, Is.EqualTo(smallExpected.Y).Within(1e-6));
            Assert.That(wide.Point.X, Is.EqualTo(wideExpected.X).Within(1e-6));
            Assert.That(wide.Point.Y, Is.EqualTo(wideExpected.Y).Within(1e-6));
            Assert.That(wide.Point.X, Is.GreaterThan(small.Point.X));
        }

        [Test]
        public void Render_DefinedTextPosition_MatchesArrowAnnotationTextAnchor()
        {
            var textAnnotation = new TextAnnotation
            {
                Text = "box",
                TextPosition = new DataPoint(3, 7),
                TextHorizontalAlignment = HorizontalAlignment.Left,
                TextVerticalAlignment = VerticalAlignment.Top,
                Padding = new OxyThickness(0),
                StrokeThickness = 0
            };
            var arrowAnnotation = new ArrowAnnotation
            {
                Text = "arrow",
                TextPosition = new DataPoint(3, 7),
                StartPoint = new DataPoint(1, 1),
                EndPoint = new DataPoint(2, 2),
                TextHorizontalAlignment = HorizontalAlignment.Left,
                TextVerticalAlignment = VerticalAlignment.Top
            };
            var model = CreateModel(textAnnotation, arrowAnnotation);

            var rc = Render(model, 500, 300);
            var text = rc.DrawnTexts.Single(t => t.Text == "box");
            var arrow = rc.DrawnTexts.Single(t => t.Text == "arrow");

            Assert.That(text.Point.X, Is.EqualTo(arrow.Point.X).Within(1e-6));
            Assert.That(text.Point.Y, Is.EqualTo(arrow.Point.Y).Within(1e-6));
        }

        private static PlotModel CreateModel(params Annotation[] annotations)
        {
            var model = new PlotModel
            {
                PlotMargins = new OxyThickness(40, 20, 20, 30),
                PlotAreaBorderThickness = new OxyThickness(0)
            };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 10, IsAxisVisible = false });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 10, IsAxisVisible = false });

            foreach (var annotation in annotations)
            {
                model.Annotations.Add(annotation);
            }

            return model;
        }

        private static TextDraw RenderAndGetText(PlotModel model, string text, double width, double height)
        {
            return Render(model, width, height).DrawnTexts.Single(t => t.Text == text);
        }

        private static RecordingRenderContext Render(PlotModel model, double width, double height)
        {
            var rc = new RecordingRenderContext();
            ((IPlotModel)model).Update(true);
            ((IPlotModel)model).Render(rc, new OxyRect(0, 0, width, height));
            return rc;
        }

        private sealed class RecordingRenderContext : NullRenderContext
        {
            public List<TextDraw> DrawnTexts { get; } = new List<TextDraw>();

            public override void DrawText(
                ScreenPoint p,
                string text,
                OxyColor fill,
                string fontFamily,
                double fontSize,
                double fontWeight,
                double rotate,
                HorizontalAlignment halign,
                VerticalAlignment valign,
                OxySize? maxSize)
            {
                this.DrawnTexts.Add(new TextDraw(text, p));
            }
        }

        private sealed class TextDraw
        {
            public TextDraw(string text, ScreenPoint point)
            {
                this.Text = text;
                this.Point = point;
            }

            public string Text { get; }

            public ScreenPoint Point { get; }
        }
    }
}
