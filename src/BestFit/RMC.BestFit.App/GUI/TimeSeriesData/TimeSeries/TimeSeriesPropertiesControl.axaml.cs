using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's TimeSeriesPropertiesControl (GUI/TimeSeriesData/TimeSeries/TimeSeriesPropertiesControl.xaml) until the
    /// Time Series Data view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class TimeSeriesPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public TimeSeriesPropertiesControl()
        {
            Content = PortPlaceholder.Create("Time Series Data properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public TimeSeriesElement Element { get; set; }
    }
}
