namespace OxyPlot.Wpf.Tests
{
    using System.IO;
    using System.Text;

    using NUnit.Framework;
    using OxyPlot.Legends;
    using OxyPlot.Series;

    [TestFixture]
    public class WpfPdfRenderContextTests
    {
        [Test]
        public void DrawLine_ConvertsCoordinatesAndDashPatternFromDipToPoints()
        {
            var rc = new WpfPdfRenderContext(300, 270, OxyColors.White);

            rc.DrawLine(
                new[] { new ScreenPoint(10, 20), new ScreenPoint(30, 40) },
                OxyColors.Blue,
                2,
                EdgeRenderingMode.PreferGeometricAccuracy,
                new[] { 4d, 1d },
                LineJoin.Bevel);

            var pdf = SaveToString(rc);

            Assert.That(pdf, Does.Contain("1.5 w"));
            Assert.That(pdf, Does.Contain("[6 1.5]0 d"));
            Assert.That(pdf, Does.Contain("2 j"));
            Assert.That(pdf, Does.Contain("7.5 255 m"));
            Assert.That(pdf, Does.Contain("22.5 240 l"));
        }

        [Test]
        public void DrawRectangle_ConvertsMarkerSizedGeometryFromDipToPoints()
        {
            var rc = new WpfPdfRenderContext(300, 270, OxyColors.White);

            rc.DrawRectangle(
                new OxyRect(10, 20, 8, 6),
                OxyColors.Red,
                OxyColors.Black,
                1,
                EdgeRenderingMode.PreferGeometricAccuracy);

            var pdf = SaveToString(rc);

            Assert.That(pdf, Does.Contain("0.75 w"));
            Assert.That(pdf, Does.Contain("7.5 250.5 6 4.5 re"));
        }

        [Test]
        public void MeasureText_ReturnsWpfDipSizeForPlotLayout()
        {
            var rc = new WpfPdfRenderContext(300, 270, OxyColors.White);

            var expected = TextToGeometryHelper.MeasureText("Expected Probability", "Segoe UI", 12, 400);
            var actual = rc.MeasureText("Expected Probability", "Segoe UI", 12, 400);

            Assert.That(actual.Width, Is.EqualTo(expected.Width).Within(1e-6));
            Assert.That(actual.Height, Is.EqualTo(expected.Height).Within(1e-6));
        }

        [Test]
        public void Export_DashedLineSeriesLegend_UsesStrokeRelativeDashPattern()
        {
            var model = new PlotModel { Background = OxyColors.White };
            model.Legends.Add(new Legend { LegendPosition = LegendPosition.LeftTop });
            var series = new LineSeries
            {
                Title = "Expected Probability",
                Color = OxyColors.Blue,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 2,
                MarkerType = MarkerType.None
            };
            series.Points.Add(new DataPoint(0, 0));
            series.Points.Add(new DataPoint(1, 1));
            model.Series.Add(series);

            using (var stream = new MemoryStream())
            {
                WpfPdfExporter.Export(model, stream, 600, 480);
                var pdf = Encoding.ASCII.GetString(stream.ToArray());

                Assert.That(CountOccurrences(pdf, "[6 1.5]0 d"), Is.GreaterThanOrEqualTo(2));
                Assert.That(pdf, Does.Not.Contain("[3 0.75]0 d"));
            }
        }

        [Test]
        public void PushClip_EmitsScaledPdfClipRectangle()
        {
            var rc = new WpfPdfRenderContext(300, 270, OxyColors.White);

            rc.PushClip(new OxyRect(10, 20, 100, 50));
            rc.DrawRectangle(
                new OxyRect(0, 0, 200, 200),
                OxyColors.Red,
                OxyColors.Undefined,
                0,
                EdgeRenderingMode.PreferGeometricAccuracy);
            rc.PopClip();

            var pdf = SaveToString(rc);

            Assert.That(pdf, Does.Contain("7.5 217.5 75 37.5 re W n"));
        }

        private static string SaveToString(WpfPdfRenderContext rc)
        {
            using (var stream = new MemoryStream())
            {
                rc.Save(stream);
                return Encoding.ASCII.GetString(stream.ToArray());
            }
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }
    }
}
