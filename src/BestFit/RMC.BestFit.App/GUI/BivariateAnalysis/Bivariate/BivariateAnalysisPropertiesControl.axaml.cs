using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's BivariateAnalysisPropertiesControl (GUI/BivariateAnalysis/Bivariate/BivariateAnalysisPropertiesControl.xaml) until the
    /// Bivariate Analysis view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class BivariateAnalysisPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public BivariateAnalysisPropertiesControl()
        {
            Content = PortPlaceholder.Create("Bivariate Analysis properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public BivariateAnalysis Element { get; set; }
    }
}
