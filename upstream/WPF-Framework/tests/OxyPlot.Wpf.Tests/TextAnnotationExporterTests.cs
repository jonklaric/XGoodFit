namespace OxyPlot.Wpf.Tests
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using NUnit.Framework;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;

    [TestFixture]
    public class TextAnnotationExporterTests
    {
        private static readonly OxyColor UniqueTextColor = OxyColor.FromRgb(0x12, 0x34, 0x56);

        [Test]
        public void WpfSvgExporter_TextAnnotationAnchor_ChangesWithExportWidth()
        {
            var model = CreateModel(new DataPoint(5, 5));

            var small = ExportSvgAndReadTextTranslate(model, 400, 300);
            var smallExpected = new ScreenPoint(model.DefaultXAxis.Transform(5), model.DefaultYAxis.Transform(5));

            var wide = ExportSvgAndReadTextTranslate(model, 800, 300);
            var wideExpected = new ScreenPoint(model.DefaultXAxis.Transform(5), model.DefaultYAxis.Transform(5));

            Assert.That(small.X, Is.EqualTo(smallExpected.X).Within(1e-3));
            Assert.That(small.Y, Is.EqualTo(smallExpected.Y).Within(1e-3));
            Assert.That(wide.X, Is.EqualTo(wideExpected.X).Within(1e-3));
            Assert.That(wide.Y, Is.EqualTo(wideExpected.Y).Within(1e-3));
            Assert.That(wide.X, Is.GreaterThan(small.X));
        }

        [Test]
        public void WpfPdfExporter_TextAnnotationAnchor_ChangesWithExportWidth()
        {
            var model = CreateModel(new DataPoint(5, 5));

            var small = ExportPdfAndReadTextAnchor(model, 400, 300);
            var smallExpected = ToPdfPoint(new ScreenPoint(model.DefaultXAxis.Transform(5), model.DefaultYAxis.Transform(5)), 300);

            var wide = ExportPdfAndReadTextAnchor(model, 800, 300);
            var wideExpected = ToPdfPoint(new ScreenPoint(model.DefaultXAxis.Transform(5), model.DefaultYAxis.Transform(5)), 300);

            Assert.That(small.X, Is.EqualTo(smallExpected.X).Within(1e-3));
            Assert.That(small.Y, Is.EqualTo(smallExpected.Y).Within(1e-3));
            Assert.That(wide.X, Is.EqualTo(wideExpected.X).Within(1e-3));
            Assert.That(wide.Y, Is.EqualTo(wideExpected.Y).Within(1e-3));
            Assert.That(wide.X, Is.GreaterThan(small.X));
        }

        [Test]
        public void PngExporter_TextAnnotationWithUndefinedPosition_Exports()
        {
            var model = CreateModel(DataPoint.Undefined);
            var exporter = new PngExporter { Width = 400, Height = 300, Background = OxyColors.White };

            using (var stream = new MemoryStream())
            {
                exporter.Export(model, stream);

                Assert.That(stream.Length, Is.GreaterThan(0));
                Assert.That(double.IsNaN(model.PlotArea.Center.X), Is.False);
                Assert.That(double.IsNaN(model.PlotArea.Center.Y), Is.False);
            }
        }

        private static PlotModel CreateModel(DataPoint textPosition)
        {
            var model = new PlotModel
            {
                Background = OxyColors.White,
                PlotMargins = new OxyThickness(40, 20, 20, 30),
                PlotAreaBorderThickness = new OxyThickness(0)
            };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 10, IsAxisVisible = false });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 10, IsAxisVisible = false });
            model.Annotations.Add(new TextAnnotation
            {
                Text = "Anchor",
                TextColor = UniqueTextColor,
                TextPosition = textPosition,
                TextHorizontalAlignment = HorizontalAlignment.Left,
                TextVerticalAlignment = VerticalAlignment.Top,
                Padding = new OxyThickness(0),
                StrokeThickness = 0
            });

            return model;
        }

        private static ScreenPoint ExportSvgAndReadTextTranslate(PlotModel model, int width, int height)
        {
            using (var stream = new MemoryStream())
            {
                WpfSvgExporter.Export(model, stream, width, height, true);
                stream.Position = 0;
                var document = XDocument.Load(stream);
                var transforms = document.Descendants()
                    .Where(e => e.Name.LocalName == "path")
                    .Where(e => string.Equals((string)e.Attribute("fill"), "rgb(18,52,86)", StringComparison.OrdinalIgnoreCase))
                    .Select(e => (string)e.Attribute("transform"))
                    .Where(t => t != null)
                    .ToArray();

                Assert.That(transforms.Length, Is.EqualTo(1));
                return ParseTranslate(transforms[0]);
            }
        }

        private static ScreenPoint ExportPdfAndReadTextAnchor(PlotModel model, int width, int height)
        {
            using (var stream = new MemoryStream())
            {
                WpfPdfExporter.Export(model, stream, width, height);
                var pdf = Encoding.ASCII.GetString(stream.ToArray());
                var colorIndex = pdf.IndexOf("0.0706 0.2039 0.3373 rg", StringComparison.Ordinal);

                Assert.That(colorIndex, Is.GreaterThanOrEqualTo(0));

                var match = Regex.Match(
                    pdf.Substring(colorIndex),
                    @"1 0 0 1 (?<x>[-+0-9.]+) (?<y>[-+0-9.]+) cm");

                Assert.That(match.Success, Is.True);
                return new ScreenPoint(ParseInvariant(match.Groups["x"].Value), ParseInvariant(match.Groups["y"].Value));
            }
        }

        private static ScreenPoint ParseTranslate(string transform)
        {
            var match = Regex.Match(
                transform,
                @"translate\((?<x>[-+0-9.Ee]+),(?<y>[-+0-9.Ee]+)\)");

            Assert.That(match.Success, Is.True);
            return new ScreenPoint(ParseInvariant(match.Groups["x"].Value), ParseInvariant(match.Groups["y"].Value));
        }

        private static ScreenPoint ToPdfPoint(ScreenPoint point, double height)
        {
            const double DipToPoints = 72.0 / 96.0;
            return new ScreenPoint(point.X * DipToPoints, (height - point.Y) * DipToPoints);
        }

        private static double ParseInvariant(string value)
        {
            return double.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}
