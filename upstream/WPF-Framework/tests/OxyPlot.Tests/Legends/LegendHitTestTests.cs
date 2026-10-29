namespace OxyPlot.Tests.Legends
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using OxyPlot.Legends;
    using OxyPlot.Series;

    [TestFixture]
    public class LegendHitTestTests
    {
        [Test]
        public void HandleMouseDown_LegendItemClick_DefaultDoesNotToggleSeriesVisibilityAndBubbles()
        {
            var model = CreateModel(out var series, out var legend);
            legend.LegendItemClickTogglesSeriesVisibility = false;
            var clickPosition = GetLegendTextClickPosition(model, series.Title);
            var mouseDownRaised = false;
            model.MouseDown += (s, e) => mouseDownRaised = true;

            model.HandleMouseDown(
                this,
                new OxyMouseDownEventArgs
                {
                    ChangedButton = OxyMouseButton.Left,
                    Position = clickPosition
                });

            Assert.IsTrue(mouseDownRaised);
            Assert.IsTrue(series.IsVisible);
        }

        [Test]
        public void HandleMouseDown_LegendItemClick_WhenEnabledTogglesSeriesVisibilityAndBubbles()
        {
            var model = CreateModel(out var series, out var legend);
            legend.LegendItemClickTogglesSeriesVisibility = true;
            var clickPosition = GetLegendTextClickPosition(model, series.Title);
            var mouseDownRaised = false;
            model.MouseDown += (s, e) => mouseDownRaised = true;

            model.HandleMouseDown(
                this,
                new OxyMouseDownEventArgs
                {
                    ChangedButton = OxyMouseButton.Left,
                    Position = clickPosition
                });

            Assert.IsTrue(mouseDownRaised);
            Assert.IsFalse(series.IsVisible);
        }

        private static PlotModel CreateModel(out LineSeries series, out Legend legend)
        {
            var model = new PlotModel();
            legend = new Legend();
            model.Legends.Add(legend);

            series = new LineSeries { Title = "Series 1" };
            series.Points.Add(new DataPoint(0, 0));
            series.Points.Add(new DataPoint(1, 1));
            model.Series.Add(series);

            return model;
        }

        private static ScreenPoint GetLegendTextClickPosition(PlotModel model, string title)
        {
            var rc = new RecordingRenderContext();
            ((IPlotModel)model).Update(true);
            ((IPlotModel)model).Render(rc, new OxyRect(0, 0, 500, 300));

            var legendText = rc.DrawnTexts.Single(t => t.Text == title);
            return new ScreenPoint(legendText.Point.X + 1, legendText.Point.Y + 1);
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
