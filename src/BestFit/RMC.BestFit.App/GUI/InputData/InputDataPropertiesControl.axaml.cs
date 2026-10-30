using Avalonia.Controls;
using RMC.BestFit.UI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's InputDataPropertiesControl (GUI/InputData/InputDataPropertiesControl.xaml) until the
    /// Input Data view is ported: it has the members MainProjectNode uses, and says the view isn't ported.
    /// </summary>
    public partial class InputDataPropertiesControl : UserControl
    {
        /// <summary>Creates the placeholder.</summary>
        public InputDataPropertiesControl()
        {
            Content = PortPlaceholder.Create("Input Data properties");
        }

        /// <summary>The element whose properties the control edits.</summary>
        public InputData Element { get; set; }
    }
}
