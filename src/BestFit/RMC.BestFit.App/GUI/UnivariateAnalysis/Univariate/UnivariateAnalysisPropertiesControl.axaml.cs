using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's UnivariateAnalysisPropertiesControl (GUI/UnivariateAnalysis/Univariate/UnivariateAnalysisPropertiesControl.xaml) until the
    /// Univariate Analysis view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class UnivariateAnalysisPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public UnivariateAnalysisPropertiesControl()
        {
            Content = PortPlaceholder.Create("Univariate Analysis properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public UnivariateAnalysis Element { get; set; }
    }
}
