using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's B17CAnalysisPropertiesControl (GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml) until the
    /// Bulletin 17C Analysis view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class B17CAnalysisPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public B17CAnalysisPropertiesControl()
        {
            Content = PortPlaceholder.Create("Bulletin 17C Analysis properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public B17CAnalysis Element { get; set; }
    }
}
