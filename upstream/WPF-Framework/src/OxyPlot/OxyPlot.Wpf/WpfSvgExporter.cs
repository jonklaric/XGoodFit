// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WpfSvgExporter.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// <summary>
//   Provides functionality to export plots to SVG with accurate text rendering.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.IO;

    /// <summary>
    /// Provides functionality to export plots to SVG using WPF text geometry
    /// for accurate text positioning and full Unicode support.
    /// </summary>
    public class WpfSvgExporter : IExporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WpfSvgExporter"/> class.
        /// </summary>
        public WpfSvgExporter()
        {
            this.Width = 600;
            this.Height = 400;
            this.IsDocument = true;
        }

        /// <summary>
        /// Gets or sets the width (in user units) of the output area.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Gets or sets the height (in user units) of the output area.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the xml headers should be included.
        /// </summary>
        public bool IsDocument { get; set; }

        /// <summary>
        /// Exports the specified model to a stream.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="stream">The output stream.</param>
        /// <param name="width">The width (points).</param>
        /// <param name="height">The height (points).</param>
        /// <param name="isDocument">if set to <c>true</c>, the xml headers will be included.</param>
        public static void Export(IPlotModel model, Stream stream, double width, double height, bool isDocument)
        {
            using (var rc = new WpfSvgRenderContext(stream, width, height, isDocument, model.Background))
            {
                model.Update(true);
                model.Render(rc, new OxyRect(0, 0, width, height));
                rc.Complete();
                rc.Flush();
            }
        }

        /// <summary>
        /// Exports the specified <see cref="PlotModel" /> to a <see cref="Stream" />.
        /// </summary>
        /// <param name="model">The model to export.</param>
        /// <param name="stream">The target stream.</param>
        public void Export(IPlotModel model, Stream stream)
        {
            Export(model, stream, this.Width, this.Height, this.IsDocument);
        }
    }
}
