using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GenericControls
{
    /// <summary>
    /// A custom WPF control for numeric input with validation for blank, negative, range, and format.
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
    public partial class NumericTextBox:UserControl
    {
        /// <summary>
        /// gets/sets whether the input can be left blank.
        /// </summary>
        public bool CanBeBlank { get; set; }

        /// <summary>
        /// gets/sets whether the negative numbers are allowed. 
        /// </summary>
        public bool CanBeNegative { get; set; }

        /// <summary>
        /// Gets/sets whether only whole numbers are allowed.
        /// </summary>
        public bool IsWholeNumber { get; set; }

        /// <summary>
        /// Gets/sets whether scientific notation (e.g., 1e-6, 1E2) is allowed.
        /// </summary>
        public bool AllowScientificNotation { get; set; }

        /// <summary>
        /// gets/sets the maximum allowable value.
        /// </summary>
        public double MaxValue { get; set; } = double.MaxValue;

        /// <summary>
        /// gets/sets the minimum allowable value.
        /// </summary>
        public double MinValue { get; set; } = double.MinValue;

        /// <summary>
        /// Gets/sets whether the bounds (MinValue and MaxValue) are exclusive.
        /// When true, values equal to the bounds are considered invalid.
        /// When false (default), values equal to the bounds are valid.
        /// </summary>
        public bool BoundsAreExclusive { get; set; }

        /// <summary>
        /// Dependency property for read-only mode.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(NumericTextBox), new UIPropertyMetadata(false));
        /// <summary>
        /// gets/sets whether the textbox is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return (bool)this.GetValue(IsReadOnlyProperty);
            }
            set
            {
                this.SetValue(IsReadOnlyProperty, value);
            }
        }

        /// <summary>
        /// Dependency property indicating if the value exceeds the maximum allowed.
        /// </summary>
        public static readonly DependencyProperty AboveMaxValueProperty = DependencyProperty.Register(nameof(AboveMaxValue), typeof(bool), typeof(NumericTextBox), new FrameworkPropertyMetadata(false));
        /// <summary>
        /// gets/sets whether the value is above MaxValue.
        /// </summary>
        public bool AboveMaxValue
        {
            get
            {
                return (bool)this.GetValue(AboveMaxValueProperty);
            }
            private set
            {
                this.SetValue(AboveMaxValueProperty, value);
            }
        }

        /// <summary>
        /// Dependency property indicating if the value is below MinValue.
        /// </summary>
        public static readonly DependencyProperty BelowMinValueProperty = DependencyProperty.Register(nameof(BelowMinValue), typeof(bool), typeof(NumericTextBox), new FrameworkPropertyMetadata(false));
        /// <summary>
        /// gets/sets whether the value is below MinValue.
        /// </summary>
        public bool BelowMinValue
        {
            get
            {
                return (bool)this.GetValue(BelowMinValueProperty);
            }
            private set
            {
                this.SetValue(BelowMinValueProperty, value);
            }
        }

        /// <summary>
        /// Dependency property indicating if the current value is valid.
        /// </summary>
        public static readonly DependencyProperty ValueIsValidProperty = DependencyProperty.Register(nameof(ValueIsValid), typeof(bool), typeof(NumericTextBox), new FrameworkPropertyMetadata(true));
        /// <summary>
        /// gets/sets whether the current value is valid.
        /// </summary>
        public bool ValueIsValid
        {
            get
            {
                return (bool)this.GetValue(ValueIsValidProperty);
            }
            private set
            {
                this.SetValue(ValueIsValidProperty, value);
            }
        }

        /// <summary>
        /// Dependency property indicating if the current text is not numeric. 
        /// </summary>
        public static readonly DependencyProperty InvalidTextProperty = DependencyProperty.Register(nameof(InvalidText), typeof(bool), typeof(NumericTextBox), new FrameworkPropertyMetadata(false));
        /// <summary>
        /// gets/sets whether the text content is not a valid number.
        /// </summary>
        public bool InvalidText
        {
            get
            {
                return (bool)this.GetValue(InvalidTextProperty);
            }
            private set
            {
                this.SetValue(InvalidTextProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the Text content.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(NumericTextBox), new UIPropertyMetadata(""));
        /// <summary>
        /// gets/sets the raw text content of the control.
        /// </summary>
        public string Text
        {
            get
            {
                return (string)this.GetValue(TextProperty);
            }
            set
            {
                this.SetValue(TextProperty, value);
            }
        }

        /// <summary>
        /// Raised when the text in the numeric box changes. 
        /// </summary>
        public event TextChangedEventHandler TextChanged;

        /// <summary>
        /// Delegate for the TextChanged event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The text changed event arguments.</param>
        public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);

        /// <summary>
        /// Intializes a new instance of the <see cref="NumericTextBox"/> control.
        /// </summary>
        public NumericTextBox()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Focuses the numeric textbox.
        /// </summary>
        public void SetFocus()
        {
            this.NumericTBox.Focus();
        }

        /// <summary>
        /// Handles the PreviewKeyDown event to prevent spacebar input.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Key event arguments.</param>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        /// <summary>
        /// Handles the PreviewTextInput event to restrict input to valid numeric characters,
        /// including handling for decimals and negative signs depending on the control settings.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">The text composition event arguments.</param>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !NumberFormatHelper.IsValidNumericInput(
                e.Text,
                this.NumericTBox.Text,
                this.NumericTBox.SelectionStart,
                this.NumericTBox.SelectedText,
                CanBeNegative,
                !IsWholeNumber,
                AllowScientificNotation);
        }

        /// <summary>
        /// Handles the TextChanged event to validate the current text value based on configured constraints.
        /// Sets flags for validity, bounds, and format.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The text changed event arguments.</param>
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double doubleValue;
            if (NumberFormatHelper.TryParseDouble(this.NumericTBox.Text, out doubleValue) == false)
            {
                // Allow valid partial inputs during typing (e.g., "-", ".", "-.")
                if (string.IsNullOrEmpty(this.NumericTBox.Text) && CanBeBlank)
                {
                    ValueIsValid = true;
                    InvalidText = false;
                }
                else if (NumberFormatHelper.IsValidPartialNumber(this.NumericTBox.Text))
                {
                    ValueIsValid = true;
                    InvalidText = false;
                }
                else
                {
                    InvalidText = true;
                    ValueIsValid = false;
                }
            }
            else
            {
                InvalidText = false;
                if (BoundsAreExclusive)
                {
                    AboveMaxValue = doubleValue >= MaxValue;
                    BelowMinValue = doubleValue <= MinValue;
                }
                else
                {
                    AboveMaxValue = doubleValue > MaxValue;
                    BelowMinValue = doubleValue < MinValue;
                }
                ValueIsValid = !(AboveMaxValue || BelowMinValue);
            }
            TextChanged?.Invoke(sender, e);
        }

        /// <summary>
        /// Selects all text in the textbox.
        /// </summary>
        public void SelectAll()
        {
            this.NumericTBox.SelectAll();
        }

        /// <summary>
        /// Checks if the current value can be parsed as a valid double.
        /// </summary>
        /// <returns>True if the value is a valid double; otherwise, false.</returns>
        public bool IsValidDouble()
        {
            double doubleValue;
            return NumberFormatHelper.TryParseDouble(this.NumericTBox.Text, out doubleValue);
        }

        /// <summary>
        /// Checks if the current value can be parsed as a valid float (single).
        /// </summary>
        /// <returns>True if the value is a valid float; otherwise, false.</returns>
        public bool IsValidSingle()
        {
            float singleValue;
            return NumberFormatHelper.TryParseSingle(this.NumericTBox.Text, out singleValue);
        }

        /// <summary>
        /// Checks if the current value can be parsed and is within integer bounds.
        /// </summary>
        /// <returns>True if the value is a valid integer; otherwise, false.</returns>
        public bool IsValidInteger()
        {
            if (IsValidDouble() == false)
                return false;
            double dblValue = GetValueAsDouble();

            if (dblValue > int.MaxValue || dblValue < int.MinValue)
                return false;
            return true;
        }

        /// <summary>
        /// Returns the current value parsed as a double.
        /// </summary>
        /// <returns>The parsed double value, or 0 if parsing fails.</returns>
        public double GetValueAsDouble()
        {
            double doubleValue;
            NumberFormatHelper.TryParseDouble(this.NumericTBox.Text, out doubleValue);
            return doubleValue;
        }

        /// <summary>
        /// Returns the current value parsed as a float.
        /// </summary>
        /// <returns>The parsed float value, or 0 if parsing fails.</returns>
        public float GetValueAsSingle()
        {
            float singleValue;
            NumberFormatHelper.TryParseSingle(this.NumericTBox.Text, out singleValue);
            return singleValue;
        }

        /// <summary>
        /// Returns the current value parsed and rounded to an integer, clamped to int range.
        /// </summary>
        /// <returns>The parsed and rounded integer value, clamped to int.MinValue and int.MaxValue.</returns>
        public int GetValueAsInteger()
        {
            double dblValue = GetValueAsDouble();

            if (dblValue > int.MaxValue)
                return int.MaxValue;
            if (dblValue < int.MinValue)
                return int.MinValue;
            // 
            return (int)Math.Round(dblValue);
        }

        /// <summary>
        /// Handles the KeyUp event for the numeric text box. When the Enter key is pressed, 
        /// this method forces the current text binding to update its source value.
        /// </summary>
        /// <param name="sender">Source of the event (should be a TextBox)</param>
        /// <param name="e">The key event arguments containing information about the key press.</param>
        private void NumericTBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                var prop = TextBox.TextProperty;
                var binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding is not null)
                {
                    binding.UpdateSource();
                }
            }
        }

    }
}