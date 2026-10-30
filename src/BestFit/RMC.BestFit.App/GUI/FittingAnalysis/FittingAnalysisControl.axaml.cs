using Avalonia.Controls;
using OxyPlot.Wpf;
using OxyPlotControls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's FittingAnalysisControl (GUI/FittingAnalysis/FittingAnalysisControl.xaml) until the
    /// Distribution Fitting Analysis view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class FittingAnalysisControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public FittingAnalysisControl()
        {
            Content = PortPlaceholder.Create("Distribution Fitting Analysis");
        }

        /// <summary>The element the view shows.</summary>
        public FittingAnalysis Element { get; set; }

#pragma warning disable CS0067 // The placeholder never raises them.
        /// <summary>Event raised when the preview control or toolbar is clicked.</summary>
        public event PreviewControlClickedEventHandler PreviewControlClicked;

        /// <summary>Raised when plot properties are requested to be displayed or modified.</summary>
        public event PlotPropertiesCalledEventHandler PlotPropertiesCalled;
#pragma warning restore CS0067

        /// <summary>Delegate for the PreviewControlClicked event.</summary>
        /// <param name="plotClicked">Indicates whether the plot was clicked.</param>
        /// <param name="toolbarClicked">Indicates whether the toolbar was clicked.</param>
        /// <param name="plot">The plot that was clicked.</param>
        public delegate void PreviewControlClickedEventHandler(bool plotClicked, bool toolbarClicked, Plot plot);

        /// <summary>Delegate for the <see cref="PlotPropertiesCalled"/> event.</summary>
        /// <param name="plot">The plot whose properties are being accessed.</param>
        /// <param name="openProperties">Whether to open the properties panel.</param>
        /// <param name="propertyExpander">The property section to expand.</param>
        /// <param name="selectedObject">The selected object in the plot.</param>
        public delegate void PlotPropertiesCalledEventHandler(Plot plot, bool openProperties, OxyPlotPropertiesControl.PropertyEXP? propertyExpander, object selectedObject);

        /// <summary>The plot the view shows: none in the placeholder.</summary>
        /// <returns>Null.</returns>
        public Plot GetCurrentPlot() => null;
    }
}
