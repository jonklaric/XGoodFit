using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;

namespace OxyPlot.Wpf.Tests;

/// <summary>
/// The exporters BestFit uses: PNG (also for previews and the clipboard), SVG and PDF with text as
/// outlines.
/// </summary>
[TestClass]
public sealed class ExportTests
{
    /// <summary>The plot's model, synchronised from its elements.</summary>
    private static PlotModel Model(Plot plot)
    {
        plot.InvalidatePlot(true);
        return plot.ActualModel;
    }

    [TestMethod]
    public void PngExporter_WritesAPngOfTheRequestedSize()
    {
        HeadlessSession.Run(() =>
        {
            var model = Model(TestPlots.FrequencyPlot());
            using var stream = new MemoryStream();
            new PngExporter { Width = 320, Height = 200 }.Export(model, stream);
            var png = stream.ToArray();

            CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, png[..8], "the PNG signature");
            Assert.AreEqual("IHDR", Encoding.ASCII.GetString(png, 12, 4));
            Assert.AreEqual(320, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)), "width");
            Assert.AreEqual(200, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)), "height");

            stream.Position = 0;
            var bitmap = new Bitmap(stream);
            Assert.AreEqual(new Avalonia.PixelSize(320, 200), bitmap.PixelSize, "it decodes");
        });
    }

    [TestMethod]
    public void PngExporter_ExportToBitmap_DrawsThePlot_AtTheResolution()
    {
        HeadlessSession.Run(() =>
        {
            var model = Model(TestPlots.FrequencyPlot());
            var bitmap = new PngExporter { Width = 600, Height = 400, Resolution = 192, Background = OxyColors.White }.ExportToBitmap(model);
            Assert.AreEqual(new Avalonia.PixelSize(600, 400), bitmap.PixelSize);
            Assert.AreEqual(192, bitmap.Dpi.X);

            using var png = new MemoryStream();
            bitmap.Save(png, PngBitmapEncoderOptions.Default);
            png.Position = 0;
            var writeable = WriteableBitmap.Decode(png);

            Assert.IsTrue(RenderingTests.CountPixels(writeable, TestPlots.FittedColor) > 300, "the fitted line is drawn");
            Assert.IsTrue(RenderingTests.CountPixels(writeable, Avalonia.Media.Colors.White, 5) > 100000, "on the white background");
        });
    }

    [TestMethod]
    public void WpfSvgExporter_WritesTextAsPathOutlines()
    {
        HeadlessSession.Run(() =>
        {
            var model = Model(TestPlots.FrequencyPlot());
            using var stream = new MemoryStream();
            WpfSvgExporter.Export(model, stream, 640, 480, true);
            var svg = Encoding.UTF8.GetString(stream.ToArray()).TrimStart('﻿');

            StringAssert.StartsWith(svg, "<?xml", "a document");
            StringAssert.Contains(svg, "<svg");
            Assert.IsFalse(svg.Contains("<text"), "no text elements: text is drawn as outlines");

            // Text paths carry a translate transform; glyph outlines have curves.
            var textPaths = Regex.Matches(svg, "<path d=\"M [^\"]*[CQ] [^\"]*\"[^>]*transform=\"translate");
            Assert.IsTrue(textPaths.Count >= 10, $"text outlines: {textPaths.Count}");
        });
    }

    [TestMethod]
    public void WpfPdfExporter_WritesAPdfPage_WithTextOutlines()
    {
        HeadlessSession.Run(() =>
        {
            var model = Model(TestPlots.FrequencyPlot());
            using var stream = new MemoryStream();
            WpfPdfExporter.Export(model, stream, 640, 480);
            var pdf = Encoding.Latin1.GetString(stream.ToArray());

            StringAssert.StartsWith(pdf, "%PDF");
            StringAssert.Matches(pdf, new Regex(@"/MediaBox\s*\[\s*0\s+0\s+480\s+360\s*\]"), "640 x 480 DIPs is 480 x 360 points");
            Assert.IsTrue(Regex.Matches(pdf, @" c\r?\n").Count > 100, "Bezier curves of the glyph outlines");
        });
    }

    [TestMethod]
    public void TextToGeometryHelper_OutlinesTheTextWithinItsMeasuredBox()
    {
        HeadlessSession.Run(() =>
        {
            var size = TextToGeometryHelper.MeasureText("Flow α 123", "Segoe UI", 16, 400);
            var geometry = TextToGeometryHelper.BuildGeometry("Flow α 123", "Segoe UI", 16, 400);

            Assert.IsTrue(geometry.Figures!.Count > 8, "a figure per glyph contour");
            var bounds = geometry.Bounds;
            Assert.IsTrue(bounds.Left >= -1 && bounds.Top >= -1, $"{bounds} starts at the text's top left");
            Assert.IsTrue(bounds.Right <= size.Width + 1 && bounds.Bottom <= size.Height + 1, $"{bounds} within {size.Width} x {size.Height}");
            Assert.IsTrue(bounds.Width > size.Width * 0.8, $"{bounds} spans the text");

            var svg = TextToGeometryHelper.GeometryToSvgPathData(geometry);
            StringAssert.StartsWith(svg, "M ");
            StringAssert.Contains(svg, "Z");
        });
    }
}
