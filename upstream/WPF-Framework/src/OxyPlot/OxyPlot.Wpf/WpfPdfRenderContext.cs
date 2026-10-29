// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WpfPdfRenderContext.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// <summary>
//   Provides a PDF render context that uses WPF text geometry for Unicode support.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides a PDF render context that converts text to geometry paths using WPF's
    /// <see cref="System.Windows.Media.FormattedText"/>, enabling correct rendering of
    /// all Unicode characters including Greek letters and mathematical symbols.
    /// </summary>
    public class WpfPdfRenderContext : PdfRenderContext
    {
        /// <summary>
        /// DIP to points conversion factor. OxyPlot fontSize is in DIP (1/96 inch),
        /// PDF coordinates are in points (1/72 inch).
        /// </summary>
        private const double DipToPoints = 72.0 / 96.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="WpfPdfRenderContext"/> class.
        /// </summary>
        /// <param name="width">The page width in points (1/72 inch).</param>
        /// <param name="height">The page height in points (1/72 inch).</param>
        /// <param name="background">The background color.</param>
        public WpfPdfRenderContext(double width, double height, OxyColor background)
            : base(width, height, background)
        {
        }

        /// <inheritdoc/>
        public override void DrawEllipse(
            OxyRect rect,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode)
        {
            base.DrawEllipse(ToPdfRect(rect), fill, stroke, thickness, edgeRenderingMode);
        }

        /// <inheritdoc/>
        public override void DrawLine(
            IList<ScreenPoint> points,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode,
            double[] dashArray,
            LineJoin lineJoin)
        {
            base.DrawLine(ToPdfPoints(points), stroke, thickness, edgeRenderingMode, dashArray, lineJoin);
        }

        /// <inheritdoc/>
        public override void DrawPolygon(
            IList<ScreenPoint> points,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode,
            double[] dashArray,
            LineJoin lineJoin)
        {
            base.DrawPolygon(ToPdfPoints(points), fill, stroke, thickness, edgeRenderingMode, dashArray, lineJoin);
        }

        /// <inheritdoc/>
        public override void DrawRectangle(
            OxyRect rect,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            EdgeRenderingMode edgeRenderingMode)
        {
            base.DrawRectangle(ToPdfRect(rect), fill, stroke, thickness, edgeRenderingMode);
        }

        /// <summary>
        /// Draws text at the specified position by converting it to geometry paths.
        /// </summary>
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
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // fontSize is in DIP (1/96 inch). Measure in DIP then convert to points
            // only for the final PDF path geometry.
            var textSizeDip = TextToGeometryHelper.MeasureText(text, fontFamily, fontSize, fontWeight);
            double width = textSizeDip.Width * DipToPoints;
            double height = textSizeDip.Height * DipToPoints;

            if (maxSize != null)
            {
                var maxSizePoints = ToPdfSize(maxSize.Value);
                if (width > maxSizePoints.Width)
                {
                    width = Math.Max(maxSizePoints.Width, 0);
                }

                if (height > maxSizePoints.Height)
                {
                    height = Math.Max(maxSizePoints.Height, 0);
                }
            }

            // Compute alignment offsets in points (same logic as base PdfRenderContext)
            double dx = 0;
            if (halign == HorizontalAlignment.Center)
            {
                dx = -width / 2;
            }

            if (halign == HorizontalAlignment.Right)
            {
                dx = -width;
            }

            double dy = 0;
            if (valign == VerticalAlignment.Middle)
            {
                dy = -height / 2;
            }

            if (valign == VerticalAlignment.Top)
            {
                dy = -height;
            }

            // Build text geometry in WPF coordinates (Y-down, units = DIP)
            var pathGeometry = TextToGeometryHelper.BuildGeometry(text, fontFamily, fontSize, fontWeight);

            this.doc.SaveState();
            this.doc.SetFillColor(fill);

            // The model renders in WPF DIP coordinates. Convert the anchor point to PDF points.
            // Flip Y for PDF coordinate system (Y-up).
            double pdfX = p.X * DipToPoints;
            double pdfY = this.doc.PageHeight - (p.Y * DipToPoints);
            this.doc.Translate(pdfX, pdfY);

            if (Math.Abs(rotate) > 1e-6)
            {
                this.doc.Rotate(-rotate);
            }

            // Apply alignment offset (in points)
            this.doc.Translate(dx, dy);

            // The geometry is in DIP with Y-down. We need to:
            // 1. Scale DIP to points (multiply by DipToPoints)
            // 2. Flip Y (negate Y scale)
            // 3. Shift up by height so flipped text extends upward from baseline
            // Combined: Transform(scale, 0, 0, -scale, 0, height)
            // This maps WPF (0,0) to PDF (0, height) and WPF (0, h_dip) to PDF (0, 0)
            this.doc.Transform(DipToPoints, 0, 0, -DipToPoints, 0, height);

            TextToGeometryHelper.WriteGeometryToPdf(this.doc, pathGeometry);
            this.doc.Fill();

            this.doc.RestoreState();
        }

        /// <summary>
        /// Measures text using WPF font metrics. Returns size in DIP so model layout
        /// matches the WPF drawing and SVG renderers; drawing converts to PDF points.
        /// </summary>
        public override OxySize MeasureText(string text, string fontFamily, double fontSize, double fontWeight)
        {
            return TextToGeometryHelper.MeasureText(text, fontFamily, fontSize, fontWeight);
        }

        /// <inheritdoc/>
        public override void DrawImage(
            OxyImage source,
            double srcX,
            double srcY,
            double srcWidth,
            double srcHeight,
            double destX,
            double destY,
            double destWidth,
            double destHeight,
            double opacity,
            bool interpolate)
        {
            base.DrawImage(
                source,
                srcX,
                srcY,
                srcWidth,
                srcHeight,
                destX * DipToPoints,
                destY * DipToPoints,
                destWidth * DipToPoints,
                destHeight * DipToPoints,
                opacity,
                interpolate);
        }

        /// <inheritdoc/>
        protected override void SetClip(OxyRect clippingRectangle)
        {
            base.SetClip(ToPdfRect(clippingRectangle));
        }

        /// <summary>
        /// Converts a screen point from WPF device-independent pixels to PDF points.
        /// </summary>
        private static ScreenPoint ToPdfPoint(ScreenPoint point)
        {
            return new ScreenPoint(point.X * DipToPoints, point.Y * DipToPoints);
        }

        /// <summary>
        /// Converts a rectangle from WPF device-independent pixels to PDF points.
        /// </summary>
        private static OxyRect ToPdfRect(OxyRect rect)
        {
            return new OxyRect(
                rect.Left * DipToPoints,
                rect.Top * DipToPoints,
                rect.Width * DipToPoints,
                rect.Height * DipToPoints);
        }

        /// <summary>
        /// Converts a size from WPF device-independent pixels to PDF points.
        /// </summary>
        private static OxySize ToPdfSize(OxySize size)
        {
            return new OxySize(size.Width * DipToPoints, size.Height * DipToPoints);
        }

        /// <summary>
        /// Converts screen points from WPF device-independent pixels to PDF points.
        /// </summary>
        private static IList<ScreenPoint> ToPdfPoints(IList<ScreenPoint> points)
        {
            var scaled = new ScreenPoint[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                scaled[i] = ToPdfPoint(points[i]);
            }

            return scaled;
        }
    }
}
