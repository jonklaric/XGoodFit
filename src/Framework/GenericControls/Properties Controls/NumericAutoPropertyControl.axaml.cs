using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using WpfCompat;
using WpfCompat.Input;

namespace GenericControls
{
    /// <summary>
    /// A property control for editing numeric values with an optional "Auto" checkbox.
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
    /// XGoodFit port: the properties are Avalonia styled properties, and their change callbacks class
    /// handlers; the read-only NumberIsDefault is a direct property. Avalonia has no Preview events, so
    /// the key and text-input handlers tunnel. The tool tip, MoveFocus and TraversalRequest are
    /// WpfCompat's.
    /// </para>
    /// </remarks>
    public partial class NumericAutoPropertyControl :UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericAutoPropertyControl"/> class.
        /// </summary>
        public NumericAutoPropertyControl()
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
        public static readonly StyledProperty<double> NumberProperty = AvaloniaProperty.Register<NumericAutoPropertyControl, double>(nameof(Number), 0d);

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

        static NumericAutoPropertyControl()
        {
            // XGoodFit port: the property-changed callbacks of upstream's metadata.
            NumberProperty.Changed.AddClassHandler<NumericAutoPropertyControl>(NumberChangedCallback);
            DefaultNumberProperty.Changed.AddClassHandler<NumericAutoPropertyControl>(DefaultNumberChangedCallback);
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
            if (d.GetType() != typeof(NumericAutoPropertyControl))
                return;
            NumericAutoPropertyControl thisControl = (NumericAutoPropertyControl)d;
            // 
            if (e.NewValue == null)
                return;
            thisControl._cancelChange = false;
            thisControl.RaisePreviewEvent(e.OldValue, e.NewValue, ref thisControl._cancelChange);
            if (thisControl._cancelChange == true)
                return;
            // 
            if (e.NewValue.GetType() != typeof(double))
            {
                thisControl.ValueIsValid = false;
                thisControl.ToolTip = $"Number is not valid.";
                return;
            }
            double newNumber = (double)e.NewValue;
            // 
            if (double.IsNaN(newNumber) || double.IsNaN(thisControl.DefaultNumber))
            {
                thisControl.NumberIsDefault = double.IsNaN(newNumber) && double.IsNaN(thisControl.DefaultNumber);
            }
            else
            {
                thisControl.NumberIsDefault = Math.Abs(newNumber - thisControl.DefaultNumber) < Math.Pow(2d, -53);
            }
            // 
            if (newNumber < thisControl.MinValue || newNumber > thisControl.MaxValue)
            {
                thisControl.ValueIsValid = false;
                thisControl.ToolTip = $"Number must be within range '{NumberFormatHelper.FormatDouble(thisControl.MinValue)}' to '{NumberFormatHelper.FormatDouble(thisControl.MaxValue)}'.";
                return;
            }


            thisControl.ValueIsValid = true;
            thisControl.ToolTip = (object)null;
            // 
            thisControl.RaisePropertyChanged(nameof(Number));
        }

        /// <summary>
        /// Raises the <see cref="PreviewNumberChanged"/> event with the old and new values, allowing cancellation. 
        /// </summary>
        /// <param name="oldValue">The previous numeric value.</param>
        /// <param name="newValue">The new numeric value.</param>
        /// <param name="cancel">A reference to a boolean that can be set to true to cancel the change.</param>
        private void RaisePreviewEvent(object oldValue, object newValue, ref bool cancel)
        {
            PreviewNumberChanged?.Invoke(oldValue, newValue, ref cancel);
        }

        /// <summary>
        /// Identifies the <see cref="NumberIsDefault"/> dependency property.
        /// </summary>
        /// <remarks>
        /// XGoodFit port: upstream's read-only dependency property is an Avalonia direct property, which only
        /// this class sets.
        /// </remarks>
        public static readonly DirectProperty<NumericAutoPropertyControl, bool> NumberIsDefaultProperty = AvaloniaProperty.RegisterDirect<NumericAutoPropertyControl, bool>(nameof(NumberIsDefault), o => o.NumberIsDefault);

        private bool _numberIsDefault = true;

        /// <summary>
        /// Determines if the number is the default.
        /// </summary>
        public bool NumberIsDefault
        {
            get
            {
                return _numberIsDefault;
            }
            protected set
            {
                this.SetAndRaise(NumberIsDefaultProperty, ref _numberIsDefault, value);
            }
        }

        /// <summary>
        /// Dependency property for the default number.
        /// </summary>
        public static readonly StyledProperty<double> DefaultNumberProperty = AvaloniaProperty.Register<NumericAutoPropertyControl, double>(nameof(DefaultNumber), 0d);

        /// <summary>
        /// Property Changed Callback for the DefaultNumber property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event arguments.</param>
        private static void DefaultNumberChangedCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(NumericAutoPropertyControl))
                return;
            NumericAutoPropertyControl thisControl = (NumericAutoPropertyControl)d;
            // 
            if (e.NewValue == null)
                return;
            if (e.NewValue.GetType() != typeof(double))
                return;
            double newNumber = (double)e.NewValue;
            // 
            if (double.IsNaN(newNumber) || double.IsNaN(thisControl.Number))
            {
                thisControl.NumberIsDefault = double.IsNaN(newNumber) && double.IsNaN(thisControl.Number);
            }
            else
            {
                thisControl.NumberIsDefault = Math.Abs(newNumber - thisControl.Number) < Math.Pow(2d, -53);
            }
            // 
            thisControl.RaisePropertyChanged(nameof(DefaultNumber));
        }



        /// <summary>
        /// Gets and sets the default number.
        /// </summary>
        public double DefaultNumber
        {
            get
            {
                return (double)this.GetValue(DefaultNumberProperty);
            }
            set
            {
                this.SetValue(DefaultNumberProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the can have negative property.
        /// </summary>
        public static readonly StyledProperty<bool> CanHaveNegativeProperty = AvaloniaProperty.Register<NumericAutoPropertyControl, bool>(nameof(CanHaveNegative), true);

        /// <summary>
        /// Dependency property for the max value property.
        /// </summary>
        public static readonly StyledProperty<double> MaxValueProperty = AvaloniaProperty.Register<NumericAutoPropertyControl, double>(nameof(MaxValue), double.MaxValue);

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
        public static readonly StyledProperty<double> MinValueProperty = AvaloniaProperty.Register<NumericAutoPropertyControl, double>(nameof(MinValue), double.MinValue);

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
        /// Occurs before the number value changes, allowing the change to be cancelled.
        /// </summary>
        public event PreviewNumberChangedEventHandler PreviewNumberChanged;

        /// <summary>
        /// Represents the method that handles the <see cref="PreviewNumberChanged"/> event.
        /// </summary>
        /// <param name="oldValue">The previous numeric value.</param>
        /// <param name="newValue">The new numeric value.</param>
        /// <param name="cancel">A reference to a boolean that can be set to true to cancel the change.</param>
        public delegate void PreviewNumberChangedEventHandler(object oldValue, object newValue, ref bool cancel);
        private bool _cancelChange = false;

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
        /// Dependency property for the allow text entry property.
        /// </summary>
        public static readonly StyledProperty<bool> AllowTextEntryProperty = AvaloniaProperty.Register<NumericAutoPropertyControl, bool>(nameof(AllowTextEntry), true);

        /// <summary>
        /// Determines if text editing is allowed.
        /// </summary>
        public bool AllowTextEntry
        {
            get
            {
                return (bool)this.GetValue(AllowTextEntryProperty);
            }
            set
            {
                this.SetValue(AllowTextEntryProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the title property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<NumericAutoPropertyControl, string>(nameof(Title), "Title");

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
        public static readonly StyledProperty<double> MaxPropertyWidthProperty = AvaloniaProperty.Register<NumericAutoPropertyControl, double>(nameof(MaxPropertyWidth), PropertyDefaults.DefaultMaxPropertyWidth);

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
        public static readonly StyledProperty<double> MinPropertyWidthProperty = AvaloniaProperty.Register<NumericAutoPropertyControl, double>(nameof(MinPropertyWidth), PropertyDefaults.DefaultMinPropertyWidth);

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
        public static readonly StyledProperty<GridLength> PropertyWidthProperty = AvaloniaProperty.Register<NumericAutoPropertyControl, GridLength>(nameof(PropertyWidth), PropertyDefaults.DefaultPropertyWidth);

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
        /// Dependency property for the show leader line property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowLeaderLineProperty = AvaloniaProperty.Register<NumericAutoPropertyControl, bool>(nameof(ShowLeaderLine), true);

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
        /// Text box preview text input.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void TextBox_PreviewTextInput(object sender, TextInputEventArgs e)
        {
            if (AllowTextEntry == false)
            {
                TextBox tBox = (TextBox)sender;
                // XGoodFit port: WPF's SelectionStart is where the selection starts; Avalonia's is where it
                // was started, which can be its end.
                e.Handled = !NumberFormatHelper.IsValidNumericInput(
                    e.Text,
                    tBox.Text,
                    Math.Min(tBox.SelectionStart, tBox.SelectionEnd),
                    tBox.SelectedText,
                    CanHaveNegative,
                    allowDecimal: true,
                    allowScientific: false);
            }
        }

        /// <summary>
        /// Text box preview key down.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        /// <summary>
        /// Text box preview key up.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // XGoodFit port: Avalonia's focused element is its window's, which WPF's
                // Keyboard.FocusedElement is.
                InputElement elementWithFocus = (InputElement)TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
                if (!(elementWithFocus == null))
                {
                    elementWithFocus.MoveFocus(new TraversalRequest(FocusNavigationDirection.Right));
                    elementWithFocus.Focus();
                }
            }
        }

        /// <summary>
        /// When the text box loses focus, update the bound property.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txtBox = (TextBox)sender;
            double newValue;
            var be = BindingOperations.GetBindingExpressionBase(txtBox, TextBox.TextProperty);
            if (NumberFormatHelper.TryParseDouble(txtBox.Text, out newValue))
            {
                be.UpdateSource();
            }
            else
            {
                be.UpdateTarget();
            }
        }

        /// <summary>
        /// When the reset button is clicked, set the number to the default number.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Number = DefaultNumber;
        }

        #endregion

    }
}