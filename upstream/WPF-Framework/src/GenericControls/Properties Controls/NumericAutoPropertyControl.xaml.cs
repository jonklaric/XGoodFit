using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
    /// </remarks>
    public partial class NumericAutoPropertyControl :UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericAutoPropertyControl"/> class.
        /// </summary>
        public NumericAutoPropertyControl()
        {
            InitializeComponent();
        }

        #region Members

        /// <summary>
        /// Dependency property for the number.
        /// </summary>
        public static readonly DependencyProperty NumberProperty = DependencyProperty.Register(nameof(Number), typeof(double), typeof(NumericAutoPropertyControl), new UIPropertyMetadata(0d, NumberChangedCallback));

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

        /// <summary>
        /// Property Changed Callback for the Number property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event arguments.</param>
        private static void NumberChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
        /// Identifies the read-only <see cref="NumberIsDefault"/> dependency property.
        /// </summary>
        private static readonly DependencyPropertyKey NumberIsDefaultPropertyKey = DependencyProperty.RegisterReadOnly(nameof(NumberIsDefault), typeof(bool), typeof(NumericAutoPropertyControl), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="NumberIsDefault"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NumberIsDefaultProperty = NumberIsDefaultPropertyKey.DependencyProperty;

        /// <summary>
        /// Determines if the number is the default.
        /// </summary>
        public bool NumberIsDefault
        {
            get
            {
                return (bool)this.GetValue(NumberIsDefaultProperty);
            }
            protected set
            {
                this.SetValue(NumberIsDefaultPropertyKey, value);
            }
        }

        /// <summary>
        /// Dependency property for the default number.
        /// </summary>
        public static readonly DependencyProperty DefaultNumberProperty = DependencyProperty.Register(nameof(DefaultNumber), typeof(double), typeof(NumericAutoPropertyControl), new UIPropertyMetadata(0d, DefaultNumberChangedCallback));

        /// <summary>
        /// Property Changed Callback for the DefaultNumber property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event arguments.</param>
        private static void DefaultNumberChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
        public static readonly DependencyProperty CanHaveNegativeProperty = DependencyProperty.Register(nameof(CanHaveNegative), typeof(bool), typeof(NumericAutoPropertyControl), new PropertyMetadata(true));

        /// <summary>
        /// Dependency property for the max value property.
        /// </summary>
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(NumericAutoPropertyControl), new FrameworkPropertyMetadata(double.MaxValue));

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
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(NumericAutoPropertyControl), new FrameworkPropertyMetadata(double.MinValue));

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
        public static readonly DependencyProperty AllowTextEntryProperty = DependencyProperty.Register(nameof(AllowTextEntry), typeof(bool), typeof(NumericAutoPropertyControl), new PropertyMetadata(true));

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
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(NumericAutoPropertyControl), new UIPropertyMetadata("Title"));

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
        public static readonly DependencyProperty MaxPropertyWidthProperty = DependencyProperty.Register(nameof(MaxPropertyWidth), typeof(double), typeof(NumericAutoPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMaxPropertyWidth));

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
        public static readonly DependencyProperty MinPropertyWidthProperty = DependencyProperty.Register(nameof(MinPropertyWidth), typeof(double), typeof(NumericAutoPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMinPropertyWidth));

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
        public static readonly DependencyProperty PropertyWidthProperty = DependencyProperty.Register(nameof(PropertyWidth), typeof(GridLength), typeof(NumericAutoPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));

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
        public static readonly DependencyProperty ShowLeaderLineProperty = DependencyProperty.Register(nameof(ShowLeaderLine), typeof(bool), typeof(NumericAutoPropertyControl), new UIPropertyMetadata(true));

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
        public event PropertyChangedEventHandler PropertyChanged;

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
            FrameworkElement el = sender as FrameworkElement;
            ActualPropertyWidth = el.ActualWidth;
        }

        /// <summary>
        /// Text box preview text input.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (AllowTextEntry == false)
            {
                TextBox tBox = (TextBox)sender;
                e.Handled = !NumberFormatHelper.IsValidNumericInput(
                    e.Text,
                    tBox.Text,
                    tBox.SelectionStart,
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
                UIElement elementWithFocus = (UIElement)Keyboard.FocusedElement;
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