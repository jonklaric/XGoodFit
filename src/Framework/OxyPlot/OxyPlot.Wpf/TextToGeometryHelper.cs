// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextToGeometryHelper.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// <summary>
//   Provides helper methods for converting text to WPF geometry paths.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Avalonia;
    using Avalonia.Media;
    using Avalonia.Media.TextFormatting;
    using SkiaSharp;

    /// <summary>
    /// Provides helper methods for converting text to WPF geometry paths,
    /// enabling Unicode-correct text rendering in PDF and SVG exports.
    /// </summary>
    /// <remarks>
    /// XGoodFit port: the geometry is Avalonia's <see cref="PathGeometry"/>, with the same figures and
    /// segment types, so the PDF and SVG writers are unchanged. Avalonia has no public way to read a
    /// geometry's outline (WPF's <c>GetOutlinedPathGeometry</c>), so <see cref="BuildGeometry"/> lays the
    /// text out with Avalonia (the same shaping, fonts and fallback fonts as on screen) and reads each
    /// glyph's outline, with its Bezier curves, from the font Avalonia drew it with, through SkiaSharp.
    /// Avalonia's formatted text takes no pixels-per-DIP.
    /// </remarks>
    public static class TextToGeometryHelper
    {
        /// <summary>
        /// The SkiaSharp typefaces of Avalonia's glyph typefaces.
        /// </summary>
        private static readonly ConditionalWeakTable<GlyphTypeface, SKTypeface> SkiaTypefaces = new ConditionalWeakTable<GlyphTypeface, SKTypeface>();

        /// <summary>
        /// Creates a <see cref="FormattedText"/> instance for the specified text and font parameters.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="fontFamily">The font family name.</param>
        /// <param name="fontSize">The font size in WPF device-independent pixels (1/96 inch).</param>
        /// <param name="fontWeight">The font weight (values > 500 are bold).</param>
        /// <returns>A <see cref="FormattedText"/> instance.</returns>
        public static FormattedText CreateFormattedText(string text, string fontFamily, double fontSize, double fontWeight)
        {
            var typeface = CreateTypeface(fontFamily, fontWeight);

            return new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black);
        }

        /// <summary>
        /// Creates the typeface for the specified font parameters.
        /// </summary>
        /// <param name="fontFamily">The font family name.</param>
        /// <param name="fontWeight">The font weight (values > 500 are bold).</param>
        /// <returns>The typeface.</returns>
        /// <remarks>XGoodFit addition: shared by <see cref="CreateFormattedText"/> and <see cref="BuildGeometry"/>.</remarks>
        private static Typeface CreateTypeface(string fontFamily, double fontWeight)
        {
            return new Typeface(
                new FontFamily(fontFamily ?? "Segoe UI"),
                FontStyle.Normal,
                fontWeight > 500 ? FontWeight.Bold : FontWeight.Normal,
                FontStretch.Normal);
        }

        /// <summary>
        /// Measures the specified text using WPF font metrics.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="fontFamily">The font family name.</param>
        /// <param name="fontSize">The font size in WPF device-independent pixels (1/96 inch).</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <returns>The text size.</returns>
        public static OxySize MeasureText(string text, string fontFamily, double fontSize, double fontWeight)
        {
            if (string.IsNullOrEmpty(text))
            {
                return OxySize.Empty;
            }

            var ft = CreateFormattedText(text, fontFamily, fontSize, fontWeight);
            return new OxySize(ft.Width, ft.Height);
        }

        /// <summary>
        /// Converts text to a <see cref="PathGeometry"/> representing the text outlines.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="fontFamily">The font family name.</param>
        /// <param name="fontSize">The font size in WPF device-independent pixels (1/96 inch).</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <returns>A <see cref="PathGeometry"/> containing the text outlines.</returns>
        public static PathGeometry BuildGeometry(string text, string fontFamily, double fontSize, double fontWeight)
        {
            // XGoodFit port: lay the text out as Avalonia draws it (TextLayout.Draw), with its top-left
            // corner at (0, 0), and add each glyph's outline at its position.
            var geometry = new PathGeometry { Figures = new PathFigures() };
            using (var layout = new TextLayout(text, CreateTypeface(fontFamily, fontWeight), fontSize, Brushes.Black))
            {
                double lineTop = 0;
                foreach (var line in layout.TextLines)
                {
                    double x = line.Start;
                    foreach (var run in line.TextRuns)
                    {
                        if (run is not DrawableTextRun drawable)
                        {
                            continue;
                        }

                        if (drawable is ShapedTextRun shaped)
                        {
                            // The run's baseline sits on the line's (BaselineAlignment.Baseline).
                            var glyphRun = shaped.GlyphRun;
                            var origin = new Point(
                                x + glyphRun.BaselineOrigin.X,
                                lineTop + line.Baseline - shaped.Baseline + glyphRun.BaselineOrigin.Y);
                            AddGlyphOutlines(geometry, glyphRun, origin);
                        }

                        x += drawable.Size.Width;
                    }

                    lineTop += line.Height;
                }
            }

            // Use GetOutlinedPathGeometry to convert to PathGeometry while preserving
            // the original Bezier curves. GetFlattenedPathGeometry would convert curves
            // to line segments, causing blurry/jagged text rendering.
            // XGoodFit port: the glyph outlines above keep their Bezier curves in the same way.
            return geometry;
        }

        /// <summary>
        /// Adds the outlines of a glyph run's glyphs to a geometry.
        /// </summary>
        /// <param name="geometry">The geometry.</param>
        /// <param name="glyphRun">The glyph run.</param>
        /// <param name="baselineOrigin">The run's baseline origin in the geometry.</param>
        /// <remarks>XGoodFit addition: the outlines come from the SkiaSharp typeface Avalonia drew the run with,
        /// with Avalonia's simulated bold and oblique.</remarks>
        private static void AddGlyphOutlines(PathGeometry geometry, GlyphRun glyphRun, Point baselineOrigin)
        {
            var typeface = GetSkiaTypeface(glyphRun.GlyphTypeface);
            if (typeface == null)
            {
                return;
            }

            var simulations = glyphRun.GlyphTypeface.FontSimulations;
            using (var font = new SKFont(typeface, (float)glyphRun.FontRenderingEmSize, 1, (simulations & FontSimulations.Oblique) != 0 ? -0.3f : 0))
            {
                font.LinearMetrics = true;
                font.Embolden = (simulations & FontSimulations.Bold) != 0;

                double penX = 0;
                foreach (var glyph in glyphRun.GlyphInfos)
                {
                    using (var path = font.GetGlyphPath(glyph.GlyphIndex))
                    {
                        if (path != null)
                        {
                            AddPath(geometry, path, baselineOrigin.X + penX + glyph.GlyphOffset.X, baselineOrigin.Y + glyph.GlyphOffset.Y);
                        }
                    }

                    penX += glyph.GlyphAdvance;
                }
            }
        }

        /// <summary>
        /// Adds a SkiaSharp path, offset, to a geometry: one figure per contour.
        /// </summary>
        /// <param name="geometry">The geometry.</param>
        /// <param name="path">The path.</param>
        /// <param name="dx">The horizontal offset.</param>
        /// <param name="dy">The vertical offset.</param>
        /// <remarks>XGoodFit addition. Conic curves (which glyph outlines don't normally use) become quadratic ones.</remarks>
        private static void AddPath(PathGeometry geometry, SKPath path, double dx, double dy)
        {
            Point P(SKPoint p) => new Point(p.X + dx, p.Y + dy);

            PathFigure figure = null;
            var points = new SKPoint[4];
            using (var iterator = path.CreateRawIterator())
            {
                SKPathVerb verb;
                while ((verb = iterator.Next(points)) != SKPathVerb.Done)
                {
                    switch (verb)
                    {
                        case SKPathVerb.Move:
                            figure = new PathFigure { StartPoint = P(points[0]), IsClosed = false, IsFilled = true, Segments = new PathSegments() };
                            geometry.Figures.Add(figure);
                            break;
                        case SKPathVerb.Line:
                            figure?.Segments.Add(new LineSegment { Point = P(points[1]) });
                            break;
                        case SKPathVerb.Quad:
                            figure?.Segments.Add(new QuadraticBezierSegment { Point1 = P(points[1]), Point2 = P(points[2]) });
                            break;
                        case SKPathVerb.Conic:
                            var quads = SKPath.ConvertConicToQuads(points[0], points[1], points[2], iterator.ConicWeight(), 1);
                            for (int i = 1; i + 1 < quads.Length; i += 2)
                            {
                                figure?.Segments.Add(new QuadraticBezierSegment { Point1 = P(quads[i]), Point2 = P(quads[i + 1]) });
                            }

                            break;
                        case SKPathVerb.Cubic:
                            figure?.Segments.Add(new BezierSegment { Point1 = P(points[1]), Point2 = P(points[2]), Point3 = P(points[3]) });
                            break;
                        case SKPathVerb.Close:
                            if (figure != null)
                            {
                                figure.IsClosed = true;
                            }

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the SkiaSharp typeface of an Avalonia glyph typeface.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface.</param>
        /// <returns>The SkiaSharp typeface, or null if there is none.</returns>
        /// <remarks>XGoodFit addition. Avalonia.Skia's platform typeface holds the typeface it draws with, as a
        /// public property of an internal class, which is read by reflection; failing that, the typeface is
        /// read from the font file Avalonia gives.</remarks>
        private static SKTypeface GetSkiaTypeface(GlyphTypeface glyphTypeface)
        {
            if (SkiaTypefaces.TryGetValue(glyphTypeface, out var cached))
            {
                return cached;
            }

            var platformTypeface = glyphTypeface.PlatformTypeface;
            var typeface = platformTypeface.GetType().GetProperty("SKTypeface")?.GetValue(platformTypeface) as SKTypeface;
            if (typeface == null && platformTypeface.TryGetStream(out var stream))
            {
                using (stream)
                {
                    typeface = SKTypeface.FromStream(stream);
                }
            }

            if (typeface != null)
            {
                SkiaTypefaces.AddOrUpdate(glyphTypeface, typeface);
            }

            return typeface;
        }

        /// <summary>
        /// Writes the path figures of a <see cref="PathGeometry"/> to a <see cref="PortableDocument"/>
        /// as PDF path commands (MoveTo, LineTo, AppendCubicBezier).
        /// </summary>
        /// <param name="doc">The PDF document.</param>
        /// <param name="pathGeometry">The path geometry to write.</param>
        public static void WriteGeometryToPdf(PortableDocument doc, PathGeometry pathGeometry)
        {
            foreach (var figure in pathGeometry.Figures)
            {
                doc.MoveTo(figure.StartPoint.X, figure.StartPoint.Y);
                var lastPoint = figure.StartPoint;

                foreach (var segment in figure.Segments)
                {
                    switch (segment)
                    {
                        case LineSegment line:
                            doc.LineTo(line.Point.X, line.Point.Y);
                            break;

                        case PolyLineSegment polyLine:
                            foreach (var pt in polyLine.Points)
                            {
                                doc.LineTo(pt.X, pt.Y);
                            }
                            break;

                        case BezierSegment bezier:
                            doc.AppendCubicBezier(
                                bezier.Point1.X, bezier.Point1.Y,
                                bezier.Point2.X, bezier.Point2.Y,
                                bezier.Point3.X, bezier.Point3.Y);
                            break;

                        case PolyBezierSegment polyBezier:
                            var pts = polyBezier.Points;
                            for (int i = 0; i + 2 < pts.Count; i += 3)
                            {
                                doc.AppendCubicBezier(
                                    pts[i].X, pts[i].Y,
                                    pts[i + 1].X, pts[i + 1].Y,
                                    pts[i + 2].X, pts[i + 2].Y);
                            }
                            break;

                        case QuadraticBezierSegment quad:
                            // Elevate quadratic to cubic: CP1 = P0 + 2/3*(QCP-P0), CP2 = P1 + 2/3*(QCP-P1)
                            var qp0 = lastPoint;
                            var qcp = quad.Point1;
                            var qp1 = quad.Point2;
                            doc.AppendCubicBezier(
                                qp0.X + (2.0 / 3.0) * (qcp.X - qp0.X), qp0.Y + (2.0 / 3.0) * (qcp.Y - qp0.Y),
                                qp1.X + (2.0 / 3.0) * (qcp.X - qp1.X), qp1.Y + (2.0 / 3.0) * (qcp.Y - qp1.Y),
                                qp1.X, qp1.Y);
                            break;
                    }

                    // Track last point for quadratic-to-cubic elevation
                    lastPoint = GetSegmentEndPoint(segment);
                }

                if (figure.IsClosed)
                {
                    doc.LineTo(figure.StartPoint.X, figure.StartPoint.Y);
                }
            }
        }

        /// <summary>
        /// Converts a <see cref="PathGeometry"/> to an SVG path data string.
        /// </summary>
        /// <param name="pathGeometry">The path geometry.</param>
        /// <returns>An SVG path data string (e.g. "M 0,0 L 10,10 Z").</returns>
        public static string GeometryToSvgPathData(PathGeometry pathGeometry)
        {
            var sb = new StringBuilder();
            foreach (var figure in pathGeometry.Figures)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "M {0:G6},{1:G6} ", figure.StartPoint.X, figure.StartPoint.Y);

                foreach (var segment in figure.Segments)
                {
                    switch (segment)
                    {
                        case LineSegment line:
                            sb.AppendFormat(CultureInfo.InvariantCulture, "L {0:G6},{1:G6} ", line.Point.X, line.Point.Y);
                            break;

                        case PolyLineSegment polyLine:
                            foreach (var pt in polyLine.Points)
                            {
                                sb.AppendFormat(CultureInfo.InvariantCulture, "L {0:G6},{1:G6} ", pt.X, pt.Y);
                            }
                            break;

                        case BezierSegment bezier:
                            sb.AppendFormat(CultureInfo.InvariantCulture, "C {0:G6},{1:G6} {2:G6},{3:G6} {4:G6},{5:G6} ",
                                bezier.Point1.X, bezier.Point1.Y,
                                bezier.Point2.X, bezier.Point2.Y,
                                bezier.Point3.X, bezier.Point3.Y);
                            break;

                        case PolyBezierSegment polyBezier:
                            var pts = polyBezier.Points;
                            for (int i = 0; i + 2 < pts.Count; i += 3)
                            {
                                sb.AppendFormat(CultureInfo.InvariantCulture, "C {0:G6},{1:G6} {2:G6},{3:G6} {4:G6},{5:G6} ",
                                    pts[i].X, pts[i].Y,
                                    pts[i + 1].X, pts[i + 1].Y,
                                    pts[i + 2].X, pts[i + 2].Y);
                            }
                            break;

                        case QuadraticBezierSegment quad:
                            sb.AppendFormat(CultureInfo.InvariantCulture, "Q {0:G6},{1:G6} {2:G6},{3:G6} ",
                                quad.Point1.X, quad.Point1.Y,
                                quad.Point2.X, quad.Point2.Y);
                            break;
                    }
                }

                if (figure.IsClosed)
                {
                    sb.Append("Z ");
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Gets the end point of a path segment (used to track current position for quadratic-to-cubic elevation).
        /// </summary>
        private static Point GetSegmentEndPoint(PathSegment segment)
        {
            return segment switch
            {
                LineSegment line => line.Point,
                BezierSegment bezier => bezier.Point3,
                QuadraticBezierSegment quad => quad.Point2,
                PolyLineSegment polyLine => polyLine.Points.Count > 0 ? polyLine.Points[polyLine.Points.Count - 1] : new Point(),
                PolyBezierSegment polyBezier => polyBezier.Points.Count > 0 ? polyBezier.Points[polyBezier.Points.Count - 1] : new Point(),
                _ => new Point(),
            };
        }
    }
}
