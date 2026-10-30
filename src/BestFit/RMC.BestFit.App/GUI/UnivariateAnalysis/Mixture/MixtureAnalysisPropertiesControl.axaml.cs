using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's MixtureAnalysisPropertiesControl (GUI/UnivariateAnalysis/Mixture/MixtureAnalysisPropertiesControl.xaml) until the
    /// Mixture Analysis view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class MixtureAnalysisPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public MixtureAnalysisPropertiesControl()
        {
            Content = PortPlaceholder.Create("Mixture Analysis properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public MixtureAnalysis Element { get; set; }
    }
}
