using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's PointProcessAnalysisPropertiesControl (GUI/UnivariateAnalysis/PointProcess/PointProcessAnalysisPropertiesControl.xaml) until the
    /// Point Process Analysis view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class PointProcessAnalysisPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public PointProcessAnalysisPropertiesControl()
        {
            Content = PortPlaceholder.Create("Point Process Analysis properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public PointProcessAnalysis Element { get; set; }
    }
}
