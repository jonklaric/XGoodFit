using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's CompositeAnalysisPropertiesControl (GUI/UnivariateAnalysis/Composite/CompositeAnalysisPropertiesControl.xaml) until the
    /// Composite Analysis view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class CompositeAnalysisPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public CompositeAnalysisPropertiesControl()
        {
            Content = PortPlaceholder.Create("Composite Analysis properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public CompositeAnalysis Element { get; set; }
    }
}
