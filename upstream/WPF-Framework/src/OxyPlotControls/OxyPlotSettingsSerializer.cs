using System.Xml.Linq;
using OxyPlot.Wpf;
using OxyPlot.Wpf.Serialization;

namespace OxyPlotControls
{
    /// <summary>
    /// Provides serialization and deserialization functionality for OxyPlot plot settings.
    /// Delegates to <see cref="PlotSerializer"/> for the actual implementation.
    /// </summary>
    public static class OxyPlotSettingsSerializer
    {
        /// <summary>
        /// The XML tag name used for the root element containing OxyPlot properties.
        /// </summary>
        public static readonly string OxyplotPropertiesTag = PlotSerializer.OxyplotPropertiesTag;

        /// <summary>
        /// Serializes all plot properties to an XElement for persistence.
        /// </summary>
        /// <param name="plot">The OxyPlot Plot control to serialize.</param>
        /// <returns>An XElement containing all serialized plot properties including general settings, legend, axes, annotations, and series.</returns>
        public static XElement ToXelement(Plot plot)
        {
            return PlotSerializer.ToXElement(plot);
        }

        /// <summary>
        /// Deserializes plot properties from an XElement and applies them to the plot.
        /// </summary>
        /// <param name="plot">The OxyPlot Plot control to apply settings to.</param>
        /// <param name="element">The XElement containing serialized plot properties.</param>
        public static void FromXelement(Plot plot, XElement element)
        {
            PlotSerializer.FromXElement(plot, element);
        }
    }
}
