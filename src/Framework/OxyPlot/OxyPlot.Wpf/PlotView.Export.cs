// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotBase.Export.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a control that displays a <see cref="PlotModel" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia.Media.Imaging;

    /// <summary>
    /// Represents a control that displays a <see cref="PlotModel" />.
    /// </summary>
    /// <remarks>
    /// XGoodFit port: bitmaps are Avalonia's. <c>SaveXaml</c> and <c>ToXaml</c> aren't ported, as the XAML
    /// exporter isn't (BestFit doesn't use it, and Avalonia can't write WPF XAML).
    /// </remarks>
    public partial class PlotView
    {
        /// <summary>
        /// Saves the PlotView as a bitmap.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public void SaveBitmap(string fileName)
        {
            this.SaveBitmap(fileName, -1, -1);
        }

        /// <summary>
        /// Saves the PlotView as a bitmap.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public void SaveBitmap(string fileName, int width, int height)
        {
            var background = this.ActualModel?.Background ?? OxyColors.Undefined;
            this.SaveBitmap(fileName, width, height, background);
        }

        /// <summary>
        /// Saves the PlotView as a bitmap.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="background">The background color.</param>
        public void SaveBitmap(string fileName, int width, int height, OxyColor background)
        {
            if (width <= 0)
            {
                width = (int)this.Bounds.Width;
            }

            if (height <= 0)
            {
                height = (int)this.Bounds.Height;
            }

            var exporter = new PngExporter { Width = width, Height = height, Background = background };
            using (var stream = System.IO.File.Create(fileName))
            {
                exporter.Export(this.ActualModel, stream);
            }
        }

        /// <summary>
        /// Renders the PlotView to a bitmap.
        /// </summary>
        /// <returns>A bitmap.</returns>
        public Bitmap ToBitmap()
        {
            var exporter = new PngExporter() { Width = (int)this.Bounds.Width, Height = (int)this.Bounds.Height };
            return exporter.ExportToBitmap(this.ActualModel);
        }
    }
}
