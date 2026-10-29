// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WpfSvgRenderContext.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// <summary>
//   Provides an SVG render context that uses WPF text geometry for accurate text rendering.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Windows.Media;

    /// <summary>
    /// Provides an SVG render context that converts text to geometry paths using WPF's
    /// <see cref="FormattedText"/>, eliminating font metric mismatches between measurement
    /// and rendering, and enabling correct Unicode character support.
    /// </summary>
    public class WpfSvgRenderContext : SvgRenderContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WpfSvgRenderContext"/> class.
        /// </summary>
        /// <param name="s">The output stream.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="isDocument">Whether to create a full SVG document with xml headers.</param>
        /// <param name="background">The background color.</param>
        public WpfSvgRenderContext(Stream s, double width, double height, bool isDocument, OxyColor background)
            : base(s, width, height, isDocument, new DummyTextMeasurer(), background)
        {
            // The base class requires a non-null textMeasurer, but we override MeasureText
            // to use WPF metrics instead, so the dummy is never actually called.
        }

        /// <summary>
        /// Draws text at the specified position by converting it to SVG path geometry.
        /// </summary>
        public override void DrawText(
            ScreenPoint p,
            string text,
            OxyColor c,
            string fontFamily,
            double fontSize,
            double fontWeight,
            double rotate,
            HorizontalAlignment halign,
            VerticalAlignment valign,
            OxySize? maxSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var lines = StringHelper.SplitLines(text);

            var fullSize = this.MeasureText(text, fontFamily, fontSize, fontWeight);
            var lineHeight = fullSize.Height / lines.Length;
            var lineOffset = new ScreenVector(
                -Math.Sin(rotate / 180.0 * Math.PI) * lineHeight,
                +Math.Cos(rotate / 180.0 * Math.PI) * lineHeight);

            // Compute initial offset for vertical alignment
            double offsetRatio;
            switch (valign)
            {
                case VerticalAlignment.Top:
                    offsetRatio = 0;
                    break;
                case VerticalAlignment.Middle:
                    offsetRatio = -(lines.Length - 1) / 2.0;
                    break;
                case VerticalAlignment.Bottom:
                    offsetRatio = -(lines.Length - 1);
                    break;
                default:
                    offsetRatio = 0;
                    break;
            }

            var currentPos = p + lineOffset * offsetRatio;

            foreach (var line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    this.DrawTextLine(currentPos, line, c, fontFamily, fontSize, fontWeight, rotate, halign, valign);
                }

                currentPos += lineOffset;
            }
        }

        /// <summary>
        /// Measures text using WPF font metrics for consistency with geometry rendering.
        /// </summary>
        public override OxySize MeasureText(string text, string fontFamily, double fontSize, double fontWeight)
        {
            return TextToGeometryHelper.MeasureText(text, fontFamily, fontSize, fontWeight);
        }

        /// <summary>
        /// Draws a single line of text as an SVG path element.
        /// </summary>
        private void DrawTextLine(
            ScreenPoint p,
            string text,
            OxyColor fill,
            string fontFamily,
            double fontSize,
            double fontWeight,
            double rotate,
            HorizontalAlignment halign,
            VerticalAlignment valign)
        {
            var lineSize = TextToGeometryHelper.MeasureText(text, fontFamily, fontSize, fontWeight);

            // Compute horizontal alignment offset
            double dx = 0;
            if (halign == HorizontalAlignment.Center)
            {
                dx = -lineSize.Width / 2;
            }
            else if (halign == HorizontalAlignment.Right)
            {
                dx = -lineSize.Width;
            }

            // Compute vertical alignment offset
            double dy = 0;
            if (valign == VerticalAlignment.Middle)
            {
                dy = -lineSize.Height / 2;
            }
            else if (valign == VerticalAlignment.Top)
            {
                // No offset needed — geometry origin is at top-left
                dy = 0;
            }
            else if (valign == VerticalAlignment.Bottom)
            {
                dy = -lineSize.Height;
            }

            // Build geometry for this line
            var pathGeometry = TextToGeometryHelper.BuildGeometry(text, fontFamily, fontSize, fontWeight);
            var pathData = TextToGeometryHelper.GeometryToSvgPathData(pathGeometry);

            if (string.IsNullOrEmpty(pathData))
            {
                return;
            }

            // Build transform: translate to position, rotate, then offset for alignment
            string transform;
            if (Math.Abs(rotate) > 1e-6)
            {
                transform = string.Format(
                    CultureInfo.InvariantCulture,
                    "translate({0:G6},{1:G6}) rotate({2:G6}) translate({3:G6},{4:G6})",
                    p.X, p.Y, rotate, dx, dy);
            }
            else
            {
                transform = string.Format(
                    CultureInfo.InvariantCulture,
                    "translate({0:G6},{1:G6})",
                    p.X + dx, p.Y + dy);
            }

            // Write SVG <path> element via SvgWriter
            this.w.WritePath(pathData, fill, transform);
        }

        /// <summary>
        /// A minimal text measurer used as a placeholder for the base <see cref="SvgRenderContext"/>
        /// constructor, which requires a non-null text measurer. This is never actually called for
        /// measurement because <see cref="WpfSvgRenderContext"/> overrides MeasureText.
        /// </summary>
        private class DummyTextMeasurer : RenderContextBase
        {
            public override void DrawLine(IList<ScreenPoint> points, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode, double[] dashArray, LineJoin lineJoin) { }
            public override void DrawPolygon(IList<ScreenPoint> points, OxyColor fill, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode, double[] dashArray, LineJoin lineJoin) { }
            public override void DrawText(ScreenPoint p, string text, OxyColor fill, string fontFamily, double fontSize, double fontWeight, double rotate, HorizontalAlignment halign, VerticalAlignment valign, OxySize? maxSize) { }
            public override OxySize MeasureText(string text, string fontFamily, double fontSize, double fontWeight) => OxySize.Empty;
            public override void PushClip(OxyRect clippingRectangle) { }
            public override void PopClip() { }
            public override int ClipCount => 0;
        }
    }
}
