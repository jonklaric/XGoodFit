using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using WpfCompat;

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
    /// <para>
    /// XGoodFit port: the properties are Avalonia styled properties, and the value's change callback is a
    /// class handler. Avalonia has no Preview events, so the key and text-input handlers tunnel. The tool
    /// tip is WpfCompat's.
    /// </para>
    /// </remarks>
    public partial class NumericTextBox2 : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericTextBox2"/> class.
        /// </summary>
        public NumericTextBox2()
        {
            InitializeComponent();

            // XGoodFit port: Avalonia has no Preview events; these are the tunnelling KeyDown, TextInput and KeyUp.
            this.NumericTBox.AddHandler(KeyDownEvent, TextBox_PreviewKeyDown, RoutingStrategies.Tunnel);
            this.NumericTBox.AddHandler(TextInputEvent, TextBox_PreviewTextInput, RoutingStrategies.Tunnel);
            this.NumericTBox.AddHandler(KeyUpEvent, TextBox_PreviewKeyUp, RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Dependency property for the number.
        /// </summary>
        public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<NumericTextBox2, double>(nameof(Value), 0d);

        static NumericTextBox2()
        {
            // XGoodFit port: the property-changed callback of upstream's metadata.
            ValueProperty.Changed.AddClassHandler<NumericTextBox2>(ValueChangedCallback);
        }

        /// <summary>
        /// Property Changed Callback for the Number property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void ValueChangedCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(NumericTextBox2))
                return;
            NumericTextBox2 thisControl = (NumericTextBox2)d;
            if (e.NewValue == null)
                return;
            double newNumber;
            if (NumberFormatHelper.TryParseDouble(e.NewValue.ToString(), out newNumber) == false)
            {
                if (e.NewValue.GetType() != typeof(double))
                {
                    thisControl.ValueIsValid = false;
                    thisControl.ToolTip = $"Number is not valid.";
                    return;
                }
                newNumber = (double)e.NewValue;
            }

            if (thisControl.BoundsAreExclusive)
            {
                if (newNumber <= thisControl.MinValue || newNumber >= thisControl.MaxValue)
                {
                    thisControl.ValueIsValid = false;
                    thisControl.ToolTip = $"Number must be greater than '{NumberFormatHelper.FormatDouble(thisControl.MinValue)}' and less than '{NumberFormatHelper.FormatDouble(thisControl.MaxValue)}'.";
                    return;
                }
            }
            else if (newNumber < thisControl.MinValue || newNumber > thisControl.MaxValue)
            {
                thisControl.ValueIsValid = false;
                thisControl.ToolTip = $"Number must be greater than or equal to '{NumberFormatHelper.FormatDouble(thisControl.MinValue)}' and less than or equal to '{NumberFormatHelper.FormatDouble(thisControl.MaxValue)}'.";
                return;
            }

            thisControl.ValueIsValid = true;
            thisControl.ToolTip = null;
        }

        /// <summary>
        /// Gets and sets the number.
        /// </summary>
        public double Value
        {
            get
            {
                return (double)this.GetValue(ValueProperty);
            }
            set
            {
                this.SetValue(ValueProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for read-only mode.
        /// </summary>
        public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<NumericTextBox2, bool>(nameof(IsReadOnly), false);
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
        /// Dependency property for the can have negative property.
        /// </summary>
        public static readonly StyledProperty<bool> CanHaveNegativeProperty = AvaloniaProperty.Register<NumericTextBox2, bool>(nameof(CanHaveNegative), true);

        /// <summary>
        /// Determines if the number can be negative.
        /// </summary>
        public bool CanHaveNegative
        {
            get
            {
                return (bool)this.GetValue(CanHaveNegativeProperty);
            }
            set
            {
                this.SetValue(CanHaveNegativeProperty, value);
            }
        }

        private bool _valueIsValid = true;

        /// <summary>
        /// Determines if the value is valid.
        /// </summary>
        public bool ValueIsValid
        {
            get
            {
                return _valueIsValid;
            }
            private set
            {
                if (_valueIsValid != value)
                {
                    _valueIsValid = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueIsValid)));
                }
            }
        }

        /// <summary>
        /// Dependency property for the is whole number property.
        /// </summary>
        public static readonly StyledProperty<bool> IsWholeNumberProperty = AvaloniaProperty.Register<NumericTextBox2, bool>(nameof(IsWholeNumber), false);

        /// <summary>
        /// Gets and sets whether the number must be a whole number.
        /// </summary>
        public bool IsWholeNumber
        {
            get
            {
                return (bool)this.GetValue(IsWholeNumberProperty);
            }
            set
            {
                this.SetValue(IsWholeNumberProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the max value property.
        /// </summary>
        public static readonly StyledProperty<double> MaxValueProperty = AvaloniaProperty.Register<NumericTextBox2, double>(nameof(MaxValue), double.MaxValue);

        /// <summary>
        /// Gets and sets the maximum value allowed.
        /// </summary>
        public double MaxValue
        {
            get
            {
                return (double)this.GetValue(MaxValueProperty);
            }
            set
            {
                this.SetValue(MaxValueProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the min value property.
        /// </summary>
        public static readonly StyledProperty<double> MinValueProperty = AvaloniaProperty.Register<NumericTextBox2, double>(nameof(MinValue), double.MinValue);

        /// <summary>
        /// Gets and sets the minimum value allowed.
        /// </summary>
        public double MinValue
        {
            get
            {
                return (double)this.GetValue(MinValueProperty);
            }
            set
            {
                this.SetValue(MinValueProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the BoundsAreExclusive property.
        /// </summary>
        public static readonly StyledProperty<bool> BoundsAreExclusiveProperty = AvaloniaProperty.Register<NumericTextBox2, bool>(nameof(BoundsAreExclusive), false);

        /// <summary>
        /// Gets/sets whether the bounds (MinValue and MaxValue) are exclusive.
        /// When true, values equal to the bounds are considered invalid (value must be strictly between min and max).
        /// When false (default), values equal to the bounds are valid.
        /// </summary>
        public bool BoundsAreExclusive
        {
            get
            {
                return (bool)this.GetValue(BoundsAreExclusiveProperty);
            }
            set
            {
                this.SetValue(BoundsAreExclusiveProperty, value);
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <remarks>
        /// XGoodFit port: <c>new</c> hides Avalonia's own PropertyChanged event. As upstream's, the class
        /// doesn't implement <see cref="INotifyPropertyChanged"/>, so bindings don't see this event.
        /// </remarks>
        public new event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Textbox preview text input.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The text composition event arguments.</param>
        private void TextBox_PreviewTextInput(object sender, TextInputEventArgs e)
        {
            var tBox = this.NumericTBox;
            // XGoodFit port: WPF's SelectionStart is where the selection starts; Avalonia's is where it was
            // started, which can be its end.
            e.Handled = !NumberFormatHelper.IsValidNumericInput(
                e.Text,
                tBox.Text,
                Math.Min(tBox.SelectionStart, tBox.SelectionEnd),
                tBox.SelectedText,
                CanHaveNegative,
                !IsWholeNumber,
                false);
        }

        /// <summary>
        /// Textbox preview key down.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The key event arguments.</param>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        /// <summary>
        /// Textbox preview key up.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The key event arguments.</param>
        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var tBox = this.NumericTBox;
                var binding = BindingOperations.GetBindingExpressionBase(tBox, TextBox.TextProperty);
                if (binding is not null)
                    binding.UpdateSource();
            }
        }

        /// <summary>
        /// Selects all text in the input box and optionally gives it focus.
        /// </summary>
        public void SelectAll()
        {
            if (!this.NumericTBox.IsKeyboardFocusWithin)
                this.NumericTBox.Focus();
            this.NumericTBox.SelectAll();
        }

    }
}