using GenericControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace FrameworkUI
{
    /// <summary>
    /// A standard Terms and Conditions for Use dialog window.
    /// Displays legal terms text and optionally requires user agreement before proceeding.
    /// </summary>
    public partial class TermsAndConditionsWindow : MetroWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TermsAndConditionsWindow"/> class.
        /// </summary>
        public TermsAndConditionsWindow()
        {
            InitializeComponent();
            Loaded += TermsAndConditionsWindow_Loaded;
        }

        /// <summary>
        /// Applies the inherited window icon and default terms document when the dialog loads.
        /// </summary>
        /// <param name="sender">The terms window that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void TermsAndConditionsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Auto-populate window icon from owner or main window
            if (Icon == null)
            {
                if (Owner != null)
                    Icon = Owner.Icon;
                else if (Application.Current?.MainWindow != null)
                    Icon = Application.Current.MainWindow.Icon;
            }

            // Set the terms content
            if (TermsDocument != null)
            {
                TCURichTextBox.Document = TermsDocument;
            }
            else
            {
                TCURichTextBox.Document = BuildDefaultTermsDocument();
            }

            // Apply ShowButtons visibility
            ApplyShowButtons();
        }

        #region Properties

        private bool _showButtons = true;

        /// <summary>
        /// Gets or sets a value indicating whether the agreement controls (I Agree checkbox, OK button, and Cancel button)
        /// should be visible in the window. When false, the window is in read-only view mode.
        /// </summary>
        public bool ShowButtons
        {
            get { return _showButtons; }
            set
            {
                _showButtons = value;
                ApplyShowButtons();
            }
        }

        /// <summary>
        /// Gets or sets the terms and conditions content as a FlowDocument.
        /// If set, this takes priority over the default terms text.
        /// </summary>
        public FlowDocument? TermsDocument { get; set; }

        /// <summary>
        /// Gets a value indicating whether the user agreed to the terms.
        /// </summary>
        public bool UserAgreed { get; private set; }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Accepts the dialog and records whether the user agreed.
        /// </summary>
        /// <param name="sender">The OK button that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            UserAgreed = IAgreeCheckbox.IsChecked == true;
            this.DialogResult = true;
        }

        /// <summary>
        /// Enables agreement once the user has scrolled to the end of the terms.
        /// </summary>
        /// <param name="sender">The terms document viewer that raised the event.</param>
        /// <param name="e">The scroll-change details.</param>
        private void RichTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double verticalOffset = TCURichTextBox.VerticalOffset;
            double viewportHeight = TCURichTextBox.ViewportHeight;
            double extentHeight = TCURichTextBox.ExtentHeight;

            // Enable checkbox when content fits entirely in the viewport (no scrolling needed)
            if (extentHeight <= viewportHeight)
            {
                if (_showButtons)
                {
                    IAgreeCheckbox.IsEnabled = true;
                }
                return;
            }

            if (verticalOffset != 0)
            {
                if (verticalOffset + viewportHeight >= extentHeight - 2.0)
                {
                    if (_showButtons)
                    {
                        IAgreeCheckbox.IsEnabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the OK button based on the agreement checkbox state.
        /// </summary>
        /// <param name="sender">The checkbox that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void IAgreeCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            OKButton.IsEnabled = IAgreeCheckbox.IsChecked == true;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Applies the configured visibility for agreement and command buttons.
        /// </summary>
        private void ApplyShowButtons()
        {
            // Guard against being called before InitializeComponent
            if (IAgreeCheckbox == null) return;

            if (_showButtons)
            {
                IAgreeCheckbox.Visibility = Visibility.Visible;
                OKButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
            }
            else
            {
                IAgreeCheckbox.Visibility = Visibility.Collapsed;
                OKButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Builds the default terms-and-conditions document.
        /// </summary>
        /// <returns>The generated terms document.</returns>
        private FlowDocument BuildDefaultTermsDocument()
        {
            string softwareName = ApplicationAttributes.Title;
            if (string.IsNullOrEmpty(softwareName))
                softwareName = "the Software";

            var doc = new FlowDocument();

            doc.Blocks.Add(CreateHeading("Terms and Conditions for Use"));

            doc.Blocks.Add(CreateParagraph(
                $"The United States Government, US Army Corps of Engineers, Risk Management Center (\"RMC\") grants to the user the rights to install {softwareName} \"the Software\" (either from a copy obtained from RMC, a distributor or another user or by downloading it from a network) and to use, copy and/or distribute copies of the Software to other users, subject to the following Terms and Conditions of Use:"));

            doc.Blocks.Add(CreateParagraph(
                "All copies of the Software received or reproduced by or for user pursuant to the authority of this Terms and Conditions of Use will be and remain the property of RMC. User may reproduce and distribute the Software provided that the recipient agrees to the Terms and Conditions for Use noted herein."));

            doc.Blocks.Add(CreateParagraph(
                "RMC is solely responsible for the content of the Software. The Software may not be modified, abridged, decompiled, disassembled, unobfuscated or reverse engineered. The user is solely responsible for the content, interactions, and effects of any and all amendments, if present, whether they be extension modules, language resource bundles, scripts or any other amendment."));

            doc.Blocks.Add(CreateParagraph(
                $"The name \"{softwareName}\" must not be used to endorse or promote products derived from the Software. Products derived from the Software may not be called \"{softwareName}\" nor may any part of the \"{softwareName}\" name appear within the name of derived products. No part of this Terms and Conditions for Use may be modified, deleted or obliterated from the Software. No part of the Software may be exported or re-exported in contravention of U.S. export laws or regulations."));

            doc.Blocks.Add(CreateHeading("Waiver of Warranty"));

            doc.Blocks.Add(CreateParagraph(
                $"THE UNITED STATES GOVERNMENT AND ITS AGENCIES, OFFICIALS, REPRESENTATIVES, AND EMPLOYEES, INCLUDING ITS CONTRACTORS AND SUPPLIERS PROVIDE {softwareName.ToUpper()} \"AS IS,\" WITHOUT ANY WARRANTY OR CONDITION, EXPRESS, IMPLIED OR STATUTORY, AND SPECIFICALLY DISCLAIM ANY IMPLIED WARRANTIES OF TITLE, MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. Depending on state law, the foregoing disclaimer may not apply to you, and you may also have other legal rights that vary from state to state."));

            doc.Blocks.Add(CreateHeading("Limitation of Liability"));

            doc.Blocks.Add(CreateParagraph(
                $"IN NO EVENT SHALL THE UNITED STATES GOVERNMENT AND ITS AGENCIES, OFFICIALS, REPRESENTATIVES, AND EMPLOYEES, INCLUDING ITS CONTRACTORS AND SUPPLIERS, BE LIABLE FOR LOST PROFITS OR ANY SPECIAL, INCIDENTAL OR CONSEQUENTIAL DAMAGES ARISING OUT OF OR IN CONNECTION WITH USE OF {softwareName.ToUpper()} REGARDLESS OF CAUSE, INCLUDING NEGLIGENCE."));

            doc.Blocks.Add(CreateParagraph(
                $"THE UNITED STATES GOVERNMENT'S LIABILITY, AND THE LIABILITY OF ITS AGENCIES, OFFICIALS, REPRESENTATIVES, AND EMPLOYEES, INCLUDING ITS CONTRACTORS AND SUPPLIERS, TO YOU OR ANY THIRD PARTIES IN ANY CIRCUMSTANCE IS LIMITED TO THE REPLACEMENT OF CERTIFIED COPIES OF {softwareName.ToUpper()} WITH IDENTIFIED ERRORS CORRECTED. Depending on state law, the above limitation or exclusion may not apply to you."));

            doc.Blocks.Add(CreateHeading("Indemnity"));

            doc.Blocks.Add(CreateParagraph(
                $"As a voluntary user of {softwareName} you agree to indemnify and hold the United States Government, and its agencies, officials, representatives, and employees, including its contractors and suppliers, harmless from any claim or demand, including reasonable attorneys' fees, made by any third party due to or arising out of your use of {softwareName} or breach of this Agreement or your violation of any law or the rights of a third party."));

            doc.Blocks.Add(CreateHeading("Assent"));

            doc.Blocks.Add(CreateParagraph(
                "By using this program you voluntarily accept these terms and conditions. If you do not agree to these terms and conditions, uninstall the program and return any program materials to RMC (If you downloaded the program and do not have disk media, please delete all copies, and cease using the program)."));

            return doc;
        }

        /// <summary>
        /// Creates a heading paragraph for the terms document.
        /// </summary>
        /// <param name="text">The heading text.</param>
        /// <returns>The formatted heading paragraph.</returns>
        private static Paragraph CreateHeading(string text)
        {
            var paragraph = new Paragraph(new Run(text))
            {
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0)
            };
            return paragraph;
        }

        /// <summary>
        /// Creates a body paragraph for the terms document.
        /// </summary>
        /// <param name="text">The paragraph text.</param>
        /// <returns>The formatted body paragraph.</returns>
        private static Paragraph CreateParagraph(string text)
        {
            var paragraph = new Paragraph(new Run(text))
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            return paragraph;
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Shows the Terms and Conditions dialog if the user has not yet agreed.
        /// Call this from App.xaml.cs startup before showing the main window.
        /// </summary>
        /// <param name="termsDocument">Optional custom FlowDocument for the terms content.</param>
        /// <returns>True if the user agreed (or had already agreed), false if they declined.</returns>
        public static bool CheckTermsAndConditions(FlowDocument? termsDocument = null)
        {
            if (UserSettings.UserAgreedToTCU) return true;

            var window = new TermsAndConditionsWindow()
            {
                ShowButtons = true,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            if (termsDocument != null)
                window.TermsDocument = termsDocument;

            window.ShowDialog();

            if (window.UserAgreed)
            {
                UserSettings.UserAgreedToTCU = true;
                UserSettings.Save(ShellPublicVariables.UserSettingsFilePath);
                return true;
            }
            return false;
        }

        #endregion
    }
}
