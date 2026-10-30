using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's TimeSeriesAnalysisPropertiesControl (GUI/TimeSeriesAnalysis/TimeSeriesAnalysisPropertiesControl.xaml) until the
    /// Time Series Analysis view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class TimeSeriesAnalysisPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public TimeSeriesAnalysisPropertiesControl()
        {
            Content = PortPlaceholder.Create("Time Series Analysis properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public TimeSeriesAnalysis Element { get; set; }
    }
}
