using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
    /// </remarks>
    public partial class NumericSliderPropertyControl : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericSliderPropertyControl"/> class.
        /// </summary>
        public NumericSliderPropertyControl()
        {
            InitializeComponent();
        }

        #region Members

        /// <summary>
        /// Dependency property for the number.
        /// </summary>
        public static readonly DependencyProperty NumberProperty = DependencyProperty.Register(nameof(Number), typeof(double), typeof(NumericSliderPropertyControl), new UIPropertyMetadata(0d, NumberChangedCallback));

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
        public static readonly DependencyProperty CanHaveNegativeProperty = DependencyProperty.Register(nameof(CanHaveNegative), typeof(bool), typeof(NumericSliderPropertyControl), new PropertyMetadata(true));

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
        public static readonly DependencyProperty IsTextBoxEnabledProperty = DependencyProperty.Register(nameof(IsTextBoxEnabled), typeof(bool), typeof(NumericSliderPropertyControl), new UIPropertyMetadata(true));

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
        public static readonly DependencyProperty IsWholeNumberProperty = DependencyProperty.Register(nameof(IsWholeNumber), typeof(bool), typeof(NumericSliderPropertyControl), new PropertyMetadata(false));

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
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(NumericSliderPropertyControl), new FrameworkPropertyMetadata(double.MaxValue));

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
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(NumericSliderPropertyControl), new FrameworkPropertyMetadata(double.MinValue));

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
        public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(nameof(Increment), typeof(double), typeof(NumericSliderPropertyControl), new FrameworkPropertyMetadata(1d));

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
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(NumericSliderPropertyControl), new UIPropertyMetadata("Title"));

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
        public static readonly DependencyProperty MaxPropertyWidthProperty = DependencyProperty.Register(nameof(MaxPropertyWidth), typeof(double), typeof(NumericSliderPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMaxPropertyWidth));

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
        public static readonly DependencyProperty MinPropertyWidthProperty = DependencyProperty.Register(nameof(MinPropertyWidth), typeof(double), typeof(NumericSliderPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMinPropertyWidth));

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
        public static readonly DependencyProperty PropertyWidthProperty = DependencyProperty.Register(nameof(PropertyWidth), typeof(GridLength), typeof(NumericSliderPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));

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
        public static readonly DependencyProperty TextPropertyWidthProperty = DependencyProperty.Register(nameof(TextPropertyWidth), typeof(GridLength), typeof(NumericSliderPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));

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
        public static readonly DependencyProperty ShowLeaderLineProperty = DependencyProperty.Register(nameof(ShowLeaderLine), typeof(bool), typeof(NumericSliderPropertyControl), new UIPropertyMetadata(true));

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
        /// Textbox preview text input.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox tBox = (TextBox)sender;
            e.Handled = !NumberFormatHelper.IsValidNumericInput(
                e.Text,
                tBox.Text,
                tBox.SelectionStart,
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
                var binding = BindingOperations.GetBindingExpression(tBox, TextBox.TextProperty);
                if (binding is not null)
                    binding.UpdateSource();
            }
        }

        #endregion
    }
}