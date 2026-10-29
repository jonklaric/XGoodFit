using System.Windows;
using GenericControls;

namespace NumericControls
{
    /// <summary>
    /// A dialog window for entering a single numeric value.
    /// Provides a simple interface for user input of numeric values with validation.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class NumericEntryDialog : MetroDialogWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericEntryDialog"/> class.
        /// </summary>
        public NumericEntryDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the click event for the OK button.
        /// Sets the dialog result to true and closes the window.
        /// </summary>
        /// <param name="sender">The OK button that was clicked.</param>
        /// <param name="e">The routed event arguments.</param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// Handles the window content rendered event.
        /// Selects all text in the value text box for user convenience.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            ValueTextBox.SelectAll();
        }
    }
}
