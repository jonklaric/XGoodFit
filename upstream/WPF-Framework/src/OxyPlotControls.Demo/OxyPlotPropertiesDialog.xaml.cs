using System.ComponentModel;
using System.Windows;
using GenericControls;
using OxyPlot.Wpf;

namespace OxyPlotControls.Demo
{
    /// <summary>
    /// Interaction logic for OxyPlotPropertiesDialog.xaml
    /// </summary>
    public partial class OxyPlotPropertiesDialog : MetroDialogWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OxyPlotPropertiesDialog"/> class.
        /// </summary>
        /// <param name="plot">The OxyPlot Plot control to configure properties for.</param>
        public OxyPlotPropertiesDialog(Plot plot)
        {
            InitializeComponent();
            PropertiesControl.Plot = plot;
        }

        /// <summary>
        /// Reactivates the owner window after the properties dialog closes.
        /// </summary>
        /// <param name="sender">The dialog that raised the event.</param>
        /// <param name="e">The closing event data.</param>
        private void OxyPlotPropertiesDialog_Closing(object sender, CancelEventArgs e)
        {
            if (Owner != null)
            {
                Owner.Activate();
            }
        }
    }
}
