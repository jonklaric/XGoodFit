using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's CoincidentFrequencyPropertiesControl (GUI/BivariateAnalysis/CoincidentFrequency/CoincidentFrequencyPropertiesControl.xaml) until the
    /// Coincident Frequency Analysis view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class CoincidentFrequencyPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public CoincidentFrequencyPropertiesControl()
        {
            Content = PortPlaceholder.Create("Coincident Frequency Analysis properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public CoincidentFrequencyAnalysis Element { get; set; }
    }
}
