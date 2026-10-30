using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using WpfCompat;

namespace GenericControls
{
    /// <summary>
    /// A custom TextBox control that validates user input based on a character limit,
    /// disallowed characters, disallowed duplicate strings, and blank input rules.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class NameTextBox : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<NameTextBox, string>(nameof(Text), "");

        /// <summary>
        /// Gets or sets the user-entered text.
        /// </summary>
        public string Text
        {
            get
            {
                return (this.GetValue(TextProperty)?.ToString());
            }
            set
            {
                this.SetValue(TextProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="CharacterLimit"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<int> CharacterLimitProperty = AvaloniaProperty.Register<NameTextBox, int>(nameof(CharacterLimit), 64);

        /// <summary>
        /// Gets or sets the maximum number of characters allowed in the text. Default is 64.
        /// </summary>
        public int CharacterLimit
        {
            get
            {
                return this.GetValue(CharacterLimitProperty);
            }
            set
            {
                this.SetValue(CharacterLimitProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="CanBeBlank"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> CanBeBlankProperty = AvaloniaProperty.Register<NameTextBox, bool>(nameof(CanBeBlank), false);

        /// <summary>
        /// Gets or sets a value indicating if the name string can be blank/empty. Default is false.
        /// </summary>
        public bool CanBeBlank
        {
            get
            {
                return this.GetValue(CanBeBlankProperty);
            }
            set
            {
                this.SetValue(CanBeBlankProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="InvalidCharacters"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<char[]> InvalidCharactersProperty = AvaloniaProperty.Register<NameTextBox, char[]>(nameof(InvalidCharacters), GetDefaultInvalidCharacters());

        /// <summary>
        /// Gets or sets the array of characters that are invalid. Default is invalid filename characters plus apostrophe and brackets.
        /// </summary>
        public char[] InvalidCharacters
        {
            get
            {
                return this.GetValue(InvalidCharactersProperty);
            }
            set
            {
                this.SetValue(InvalidCharactersProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="InvalidStrings"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string[]> InvalidStringsProperty = AvaloniaProperty.Register<NameTextBox, string[]>(nameof(InvalidStrings), new string[] { });
        /// <summary>
        /// Gets or sets the array of strings that are invalid. Default is no invalid strings.
        /// </summary>
        public string[] InvalidStrings
        {
            get
            {
                return this.GetValue(InvalidStringsProperty);
            }
            set
            {
                this.SetValue(InvalidStringsProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="IsValid"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsValidProperty = AvaloniaProperty.Register<NameTextBox, bool>(nameof(IsValid), true);
        /// <summary>
        /// Gets or sets a value indicating whether the current text is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return this.GetValue(IsValidProperty);
            }
            set
            {
                this.SetValue(IsValidProperty, value);
            }
        }

        static NameTextBox()
        {
            // XGoodFit port: the property-changed callbacks of upstream's metadata.
            CharacterLimitProperty.Changed.AddClassHandler<NameTextBox>(ValidationProperty_Callback);
            CanBeBlankProperty.Changed.AddClassHandler<NameTextBox>(ValidationProperty_Callback);
            InvalidCharactersProperty.Changed.AddClassHandler<NameTextBox>(ValidationProperty_Callback);
            InvalidStringsProperty.Changed.AddClassHandler<NameTextBox>(ValidationProperty_Callback);
        }

        /// <summary>
        /// Invoked when validation-related properties are changed.
        /// Re-validates the text.
        /// </summary>
        /// <param name="d">The dependency object that triggered the callback.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void ValidationProperty_Callback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(NameTextBox))
                return;
            NameTextBox thisControl = (NameTextBox)d;
            thisControl.ValidateText();
        }

        /// <summary>
        /// Shared function to get the default invalid characters for the name textbox. Invalid characters include invalid file name characters, apostrophe, left bracket, and right bracket.
        /// </summary>
        /// <returns>An array of default invalid name characters.</returns>
        public static char[] GetDefaultInvalidCharacters()
        {
            var invalidCharsList = new List<char>(System.IO.Path.GetInvalidFileNameChars());
            invalidCharsList.Add('\'');
            invalidCharsList.Add('[');
            invalidCharsList.Add(']');
            return invalidCharsList.ToArray();
        }

        /// <summary>
        /// Raised when the user modifies the text in the control.
        /// </summary>
        public event TextChangedEventHandler TextChanged;

        /// <summary>
        /// Delegate signature for the TextChanged event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);

        /// <summary>
        /// Initializes a new instance of the <see cref="NameTextBox"/> control. 
        /// </summary>
        public NameTextBox()
        {

            // This call is required by the designer.
            this.InitializeComponent();

            // XGoodFit port: Avalonia has no Preview events; these are the tunnelling KeyDown and KeyUp.
            // Avalonia raises TextChanged after the change is rendered; WPF raises it as the text
            // changes, which TextChanging matches.
            this.NameTBox.AddHandler(KeyDownEvent, TextBox_PreviewKeyDown, RoutingStrategies.Tunnel);
            this.NameTBox.AddHandler(KeyUpEvent, TextBox_PreviewKeyUp, RoutingStrategies.Tunnel);
            this.NameTBox.TextChanging += (sender, e) => NameTBox_TextChanged(sender, new TextChangedEventArgs(TextBox.TextChangedEvent, this.NameTBox));

            // Add any initialization after the InitializeComponent() call.
            ValidateText();
        }

        /// <summary>
        /// Handles the internal TextBox's text change event. Triggers validation.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Text changed event arguments.</param>
        private void NameTBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            TextChanged?.Invoke(sender, e);

            ValidateText();
        }

        /// <summary>
        /// Validates the text based on rules for character limit, blank string, 
        /// duplicate names, and invalid characters. Updates the IsValid flag and tooltip.
        /// </summary>
        public void ValidateText()
        {
            this.NameTBox.ToolTip = (object)null;
            // 
            if (this.NameTBox.Text.Length > CharacterLimit && CharacterLimit > -1)
            {
                this.NameTBox.ToolTip = "The name entered is longer (" + this.NameTBox.Text.Length + " characters) than the maximum name length (" + CharacterLimit + " characters).";
                IsValid = false;
                return;
            }
            //
            if (string.IsNullOrEmpty(this.NameTBox.Text) && CanBeBlank == false)
            {
                this.NameTBox.ToolTip = "The name entered cannot be blank. The name entered must not be blank and must be less than " + CharacterLimit + " characters.";
                IsValid = false;
                return;
            }
            // 
            if (InvalidStrings.Contains(this.NameTBox.Text))
            {
                this.NameTBox.ToolTip = "Name entered already exists and must be unique.";
                IsValid = false;
                return;
            }
            // 
            foreach (char badChar in InvalidCharacters)
            {
                if (this.NameTBox.Text.Contains(badChar))
                {
                    this.NameTBox.ToolTip = "Invalid character in name: '" + badChar + "'";
                    IsValid = false;
                    return;
                }
            }
            // 
            IsValid = true;
        }
        /// <summary>
        /// Get all error messages associated with the text in the name textbox.
        /// </summary>
        /// <returns>A list of error messages.</returns>
        public List<string> GetErrorMessages()
        {
            var errorList = new List<string>();
            if (IsValid == true)
                return errorList;
            // 
            if (this.NameTBox.Text.Length > CharacterLimit && CharacterLimit > -1)
            {
                errorList.Add("The name entered is longer (" + this.NameTBox.Text.Length + " characters) than the maximum name length (" + CharacterLimit + " characters).");
            }
            //
            if (string.IsNullOrEmpty(this.NameTBox.Text) && CanBeBlank == false)
            {
                errorList.Add("The name entered cannot be blank. The name entered must not be blank and must be less than " + CharacterLimit + " characters.");
            }
            // 
            if (InvalidStrings.Contains(this.NameTBox.Text))
            {
                errorList.Add("Name entered already exists and must be unique.");
            }
            // 
            foreach (char badChar in InvalidCharacters)
            {
                if (this.NameTBox.Text.Contains(badChar))
                {
                    errorList.Add("Invalid character in name: '" + badChar + "'");
                }
            }
            // 
            return errorList;
        }

        /// <summary>
        /// Selects all text in the internal TextBox.
        /// </summary>
        public void SelectAll()
        {
            this.NameTBox.SelectAll();
        }

        /// <summary>
        /// Updates the source binding when Enter is pressed.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Key event arguments.</param>
        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                var binding = BindingOperations.GetBindingExpressionBase(tBox, TextBox.TextProperty);
                if (binding is not null)
                    binding.UpdateSource();
            }
        }

        /// <summary>
        /// Suppresses numpad minus and plus keys to prevent WPF TreeView's
        /// built-in collapse/expand behavior while the user is typing.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Key event arguments.</param>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Subtract || e.Key == Key.Add)
            {
                // Mark handled to prevent WPF TreeView collapse/expand,
                // then manually insert the character since handling the
                // event also suppresses the normal text input pipeline.
                e.Handled = true;
                var textBox = (TextBox)sender;
                string ch = e.Key == Key.Subtract ? "-" : "+";
                // XGoodFit port: WPF's CaretIndex is where the selection starts; Avalonia's caret can
                // be at either end. SelectionLength is WpfCompat's.
                int caretIndex = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
                int selectionLength = textBox.SelectionLength;
                textBox.Text = textBox.Text.Remove(caretIndex, selectionLength).Insert(caretIndex, ch);
                textBox.CaretIndex = caretIndex + 1;
            }
        }

    }
}