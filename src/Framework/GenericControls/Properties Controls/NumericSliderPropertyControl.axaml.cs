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
    /// A property control for editing numeric values using a slider with increment/decrement buttons.
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
    /// XGoodFit port: the properties are Avalonia styled properties, and the number's change callback a
    /// class handler. Avalonia has no Preview events, so the key and text-input handlers tunnel. The
    /// tool tip is WpfCompat's.
    /// </para>
    /// </remarks>
    public partial class NumericSliderPropertyControl : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericSliderPropertyControl"/> class.
        /// </summary>
        public NumericSliderPropertyControl()
        {
            InitializeComponent();

            // XGoodFit port: Avalonia has no Preview events; these are tunnelling handlers.
            this.NumericTBox.AddHandler(TextInputEvent, TextBox_PreviewTextInput, RoutingStrategies.Tunnel);
            this.NumericTBox.AddHandler(KeyDownEvent, TextBox_PreviewKeyDown, RoutingStrategies.Tunnel);
            this.NumericTBox.AddHandler(KeyUpEvent, TextBox_PreviewKeyUp, RoutingStrategies.Tunnel);
        }

        #region Members

        /// <summary>
        /// Dependency property for the number.
        /// </summary>
        public static readonly StyledProperty<double> NumberProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, double>(nameof(Number), 0d);

        /// <summary>
        /// Gets and sets the number.
        /// </summary>
        public double Number
        {
            get
            {
                return (double)this.GetValue(NumberProperty);
            }
            set
            {
                this.SetValue(NumberProperty, value);
            }
        }

        static NumericSliderPropertyControl()
        {
            // XGoodFit port: the property-changed callbacks of upstream's metadata.
            NumberProperty.Changed.AddClassHandler<NumericSliderPropertyControl>(NumberChangedCallback);
        }

        /// <summary>
        /// Property Changed Callback for the Number property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event arguments.</param>
        private static void NumberChangedCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(NumericSliderPropertyControl))
                return;
            NumericSliderPropertyControl thisControl = (NumericSliderPropertyControl)d;
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
            if (newNumber < thisControl.MinValue || newNumber > thisControl.MaxValue)
            {
                thisControl.ValueIsValid = false;
                thisControl.ToolTip = $"Number must be within range '{NumberFormatHelper.FormatDouble(thisControl.MinValue)}' to '{NumberFormatHelper.FormatDouble(thisControl.MaxValue)}'.";
                return;
            }
            thisControl.ValueIsValid = true;
            thisControl.ToolTip = (object)null;
        }

        /// <summary>
        /// Dependency property for the can have negative property.
        /// </summary>
        public static readonly StyledProperty<bool> CanHaveNegativeProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, bool>(nameof(CanHaveNegative), true);

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

        /// <summary>
        /// Dependency property for the is text box enabled property.
        /// </summary>
        public static readonly StyledProperty<bool> IsTextBoxEnabledProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, bool>(nameof(IsTextBoxEnabled), true);

        /// <summary>
        /// Gets and sets whether the text box is enabled.
        /// </summary>
        public bool IsTextBoxEnabled
        {
            get
            {
                return (bool)this.GetValue(IsTextBoxEnabledProperty);
            }
            set
            {
                this.SetValue(IsTextBoxEnabledProperty, value);
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
                    RaisePropertyChanged(nameof(ValueIsValid));
                }
            }
        }

        /// <summary>
        /// Dependency property for the is whole number property.
        /// </summary>
        public static readonly StyledProperty<bool> IsWholeNumberProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, bool>(nameof(IsWholeNumber), false);

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
        public static readonly StyledProperty<double> MaxValueProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, double>(nameof(MaxValue), double.MaxValue);

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
        public static readonly StyledProperty<double> MinValueProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, double>(nameof(MinValue), double.MinValue);

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
        /// Dependency property for the increment value.
        /// </summary>
        public static readonly StyledProperty<double> IncrementProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, double>(nameof(Increment), 1d);

        /// <summary>
        /// Gets and sets the increment value for the slider.
        /// </summary>
        public double Increment
        {
            get
            {
                return (double)this.GetValue(IncrementProperty);
            }
            set
            {
                this.SetValue(IncrementProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the title property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, string>(nameof(Title), "Title");

        /// <summary>
        /// Gets and sets the title of the property.
        /// </summary>
        public string Title
        {
            get
            {
                return (string)this.GetValue(TitleProperty);
            }
            set
            {
                this.SetValue(TitleProperty, value);
            }
        }

        #region Control Width

        /// <summary>
        /// Dependency property for the max property width.
        /// </summary>
        public static readonly StyledProperty<double> MaxPropertyWidthProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, double>(nameof(MaxPropertyWidth), PropertyDefaults.DefaultMaxPropertyWidth);

        /// <summary>
        /// Gets and sets the maximum property width.
        /// </summary>
        public double MaxPropertyWidth
        {
            get
            {
                return (double)this.GetValue(MaxPropertyWidthProperty);
            }
            set
            {
                this.SetValue(MaxPropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the min property width.
        /// </summary>
        public static readonly StyledProperty<double> MinPropertyWidthProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, double>(nameof(MinPropertyWidth), PropertyDefaults.DefaultMinPropertyWidth);

        /// <summary>
        /// Gets and sets the minimum property width.
        /// </summary>
        public double MinPropertyWidth
        {
            get
            {
                return (double)this.GetValue(MinPropertyWidthProperty);
            }
            set
            {
                this.SetValue(MinPropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the property width.
        /// </summary>
        public static readonly StyledProperty<GridLength> PropertyWidthProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, GridLength>(nameof(PropertyWidth), PropertyDefaults.DefaultPropertyWidth);

        /// <summary>
        /// Gets and sets the property width.
        /// </summary>
        public GridLength PropertyWidth
        {
            get
            {
                return (GridLength)this.GetValue(PropertyWidthProperty);
            }
            set
            {
                this.SetValue(PropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="TextPropertyWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<GridLength> TextPropertyWidthProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, GridLength>(nameof(TextPropertyWidth), PropertyDefaults.DefaultPropertyWidth);

        /// <summary>
        /// Gets and sets the width of the text portion of the property control.
        /// </summary>
        public GridLength TextPropertyWidth
        {
            get
            {
                return (GridLength)this.GetValue(TextPropertyWidthProperty);
            }
            set
            {
                this.SetValue(TextPropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the show leader line property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowLeaderLineProperty = AvaloniaProperty.Register<NumericSliderPropertyControl, bool>(nameof(ShowLeaderLine), true);

        /// <summary>
        /// Gets and sets whether to show the leader line.
        /// </summary>
        public bool ShowLeaderLine
        {
            get
            {
                return (bool)this.GetValue(ShowLeaderLineProperty);
            }
            set
            {
                this.SetValue(ShowLeaderLineProperty, value);
            }
        }

        private double _actualWidth = 0d;

        /// <summary>
        /// Gets and sets the actual property width.
        /// </summary>
        public double ActualPropertyWidth
        {
            get
            {
                return _actualWidth;
            }
            private set
            {
                if (_actualWidth != value)
                {
                    _actualWidth = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActualPropertyWidth)));
                }
            }
        }

        #endregion

        /// <summary>
        /// The property changed event.
        /// </summary>
        /// <remarks>XGoodFit port: <c>new</c> hides Avalonia's own PropertyChanged event; this class's
        /// <see cref="INotifyPropertyChanged"/> implementation raises this one.</remarks>
        public new event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        /// <summary>
        /// Raise the property changed event.
        /// </summary>
        /// <param name="propertyName">The name of the property to change.</param>
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Update the actual property width with the control size changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // XGoodFit port: WPF's FrameworkElement and ActualWidth are Avalonia's Control and Bounds.Width.
            Control el = sender as Control;
            ActualPropertyWidth = el.Bounds.Width;
        }

        /// <summary>
        /// Textbox preview text input.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void TextBox_PreviewTextInput(object sender, TextInputEventArgs e)
        {
            TextBox tBox = (TextBox)sender;
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
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        /// <summary>
        /// Textbox preview key up.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
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

        #endregion
    }
}