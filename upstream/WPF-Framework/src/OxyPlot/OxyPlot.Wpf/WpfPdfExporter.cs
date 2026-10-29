// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WpfPdfExporter.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// <summary>
//   Provides functionality to export plots to PDF with full Unicode support.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.IO;

    /// <summary>
    /// Provides functionality to export plots to PDF using WPF text geometry
    /// for correct rendering of all Unicode characters.
    /// </summary>
    public class WpfPdfExporter : IExporter
    {
        /// <summary>
        /// DIP to points conversion factor.
        /// </summary>
        private const double DipToPoints = 72.0 / 96.0;

        /// <summary>
        /// Gets or sets the width in DIP (device-independent pixels, 1/96 inch).
        /// The PDF page will be sized to match the physical dimensions at 96 DPI.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Gets or sets the height in DIP (device-independent pixels, 1/96 inch).
        /// The PDF page will be sized to match the physical dimensions at 96 DPI.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Exports the specified model to a stream.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="stream">The output stream.</param>
        /// <param name="width">The width in DIP.</param>
        /// <param name="height">The height in DIP.</param>
        public static void Export(IPlotModel model, Stream stream, double width, double height)
        {
            var exporter = new WpfPdfExporter { Width = width, Height = height };
            exporter.Export(model, stream);
        }

        /// <summary>
        /// Exports the specified <see cref="PlotModel" /> to the specified <see cref="Stream" />.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="stream">The stream.</param>
        public void Export(IPlotModel model, Stream stream)
        {
            // Convert DIP dimensions to points for the PDF page size.
            // This ensures the PDF physical size matches the on-screen size at 96 DPI,
            // and text proportions match the PNG/SVG exports.
            double pageWidth = this.Width * DipToPoints;
            double pageHeight = this.Height * DipToPoints;

            var rc = new WpfPdfRenderContext(pageWidth, pageHeight, model.Background);
            model.Update(true);
            model.Render(rc, new OxyRect(0, 0, this.Width, this.Height));
            rc.Save(stream);
        }
    }
}
