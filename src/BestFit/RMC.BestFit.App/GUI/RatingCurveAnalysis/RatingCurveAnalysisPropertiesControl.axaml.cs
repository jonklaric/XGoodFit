using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's RatingCurveAnalysisPropertiesControl (GUI/RatingCurveAnalysis/RatingCurveAnalysisPropertiesControl.xaml) until the
    /// Rating Curve Analysis view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class RatingCurveAnalysisPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public RatingCurveAnalysisPropertiesControl()
        {
            Content = PortPlaceholder.Create("Rating Curve Analysis properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public RatingCurveAnalysis Element { get; set; }
    }
}
