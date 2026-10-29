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
    using System.Text;
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Provides helper methods for converting text to WPF geometry paths,
    /// enabling Unicode-correct text rendering in PDF and SVG exports.
    /// </summary>
    public static class TextToGeometryHelper
    {
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
            var typeface = new Typeface(
                new FontFamily(fontFamily ?? "Segoe UI"),
                FontStyles.Normal,
                fontWeight > 500 ? FontWeights.Bold : FontWeights.Normal,
                FontStretches.Normal);

            return new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                1.0); // pixelsPerDip
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
            var ft = CreateFormattedText(text, fontFamily, fontSize, fontWeight);
            var geometry = ft.BuildGeometry(new Point(0, 0));

            // Use GetOutlinedPathGeometry to convert to PathGeometry while preserving
            // the original Bezier curves. GetFlattenedPathGeometry would convert curves
            // to line segments, causing blurry/jagged text rendering.
            return geometry.GetOutlinedPathGeometry();
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
