using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's FittingAnalysisPropertiesControl (GUI/FittingAnalysis/FittingAnalysisPropertiesControl.xaml) until the
    /// Distribution Fitting Analysis view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class FittingAnalysisPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public FittingAnalysisPropertiesControl()
        {
            Content = PortPlaceholder.Create("Distribution Fitting Analysis properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public FittingAnalysis Element { get; set; }
    }
}
