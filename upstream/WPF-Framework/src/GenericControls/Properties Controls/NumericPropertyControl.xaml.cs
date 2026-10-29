using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GenericControls
{
    /// <summary>
    /// A property control for editing numeric values with validation and formatting options.
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
    public partial class NumericPropertyControl :UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericPropertyControl"/> class.
        /// </summary>
        public NumericPropertyControl()
        {
            InitializeComponent();
        }

        #region Members

        /// <summary>
        /// Dependency property for the number.
        /// </summary>
        public static readonly DependencyProperty NumberProperty = DependencyProperty.Register(nameof(Number), typeof(double), typeof(NumericPropertyControl), new UIPropertyMetadata(0d, NumberChangedCallback));

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
            if (d.GetType() != typeof(NumericPropertyControl))
                return;
            NumericPropertyControl thisControl = (NumericPropertyControl)d;
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
            if (thisControl.CanHaveNaN == false && double.IsNaN(newNumber))
            {
                thisControl.ValueIsValid = false;
                thisControl.ToolTip = $"NaN is not a number.";
                return;
            }

            if (thisControl.BoundsAreExclusive)
            {
                if (thisControl.Number <= thisControl.MinValue || thisControl.Number >= thisControl.MaxValue)
                {
                    thisControl.ValueIsValid = false;
                    thisControl.ToolTip = $"Number must be greater than '{NumberFormatHelper.FormatDouble(thisControl.MinValue)}' and less than '{NumberFormatHelper.FormatDouble(thisControl.MaxValue)}'.";
                    return;
                }
            }
            else if (thisControl.Number < thisControl.MinValue || thisControl.Number > thisControl.MaxValue)
            {
                thisControl.ValueIsValid = false;
                thisControl.ToolTip = $"Number must be greater than or equal to '{NumberFormatHelper.FormatDouble(thisControl.MinValue)}' and less than or equal to '{NumberFormatHelper.FormatDouble(thisControl.MaxValue)}'.";
                return;
            }

            thisControl.ValueIsValid = true;
            thisControl.ToolTip = null;
        }

        /// <summary>
        /// Dependency property for the can have negative property.
        /// </summary>
        public static readonly DependencyProperty CanHaveNegativeProperty = DependencyProperty.Register(nameof(CanHaveNegative), typeof(bool), typeof(NumericPropertyControl), new PropertyMetadata(true));

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
        /// Dependency property for the BoundsAreExclusive property.
        /// </summary>
        public static readonly DependencyProperty BoundsAreExclusiveProperty = DependencyProperty.Register(nameof(BoundsAreExclusive), typeof(bool), typeof(NumericPropertyControl), new PropertyMetadata(false));

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
        /// Gets/sets whether bounds are exclusive. Use <see cref="BoundsAreExclusive"/> instead.
        /// </summary>
        [Obsolete("Use BoundsAreExclusive instead. This property will be removed in a future version.")]
        public bool IncludeBounds
        {
            get { return !BoundsAreExclusive; }
            set { BoundsAreExclusive = !value; }
        }

        /// <summary>
        /// Dependency property for the can have NaN property.
        /// </summary>
        public static readonly DependencyProperty CanHaveNaNProperty = DependencyProperty.Register(nameof(CanHaveNaN), typeof(bool), typeof(NumericPropertyControl), new PropertyMetadata(true));

        /// <summary>
        /// Determines if the number can be NaN (Not a Number).
        /// </summary>
        public bool CanHaveNaN
        {
            get
            {
                return (bool)this.GetValue(CanHaveNaNProperty);
            }
            set
            {
                this.SetValue(CanHaveNaNProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the is text box enabled property.
        /// </summary>
        public static readonly DependencyProperty IsTextBoxEnabledProperty = DependencyProperty.Register(nameof(IsTextBoxEnabled), typeof(bool), typeof(NumericPropertyControl), new UIPropertyMetadata(true));

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
        public static readonly DependencyProperty IsWholeNumberProperty = DependencyProperty.Register(nameof(IsWholeNumber), typeof(bool), typeof(NumericPropertyControl), new PropertyMetadata(false));

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
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(NumericPropertyControl), new FrameworkPropertyMetadata(double.MaxValue, MinMaxChangedCallback));

        /// <summary>
        /// Property Changed Callback for the MinValue/MaxValue properties.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event arguments.</param>
        private static void MinMaxChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(NumericPropertyControl))
                return;
            NumericPropertyControl thisControl = (NumericPropertyControl)d;

            if (thisControl.CanHaveNaN == false && double.IsNaN(thisControl.Number))
            {
                thisControl.ValueIsValid = false;
                thisControl.ToolTip = $"NaN is not a number.";
                return;
            }

            if (thisControl.BoundsAreExclusive)
            {
                if (thisControl.Number <= thisControl.MinValue || thisControl.Number >= thisControl.MaxValue)
                {
                    thisControl.ValueIsValid = false;
                    thisControl.ToolTip = $"Number must be greater than '{NumberFormatHelper.FormatDouble(thisControl.MinValue)}' and less than '{NumberFormatHelper.FormatDouble(thisControl.MaxValue)}'.";
                    return;
                }
            }
            else if (thisControl.Number < thisControl.MinValue || thisControl.Number > thisControl.MaxValue)
            {
                thisControl.ValueIsValid = false;
                thisControl.ToolTip = $"Number must be greater than or equal to '{NumberFormatHelper.FormatDouble(thisControl.MinValue)}' and less than or equal to '{NumberFormatHelper.FormatDouble(thisControl.MaxValue)}'.";
                return;
            }

            thisControl.ValueIsValid = true;
            thisControl.ToolTip = null;
        }

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
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(NumericPropertyControl), new FrameworkPropertyMetadata(double.MinValue, MinMaxChangedCallback));

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
        /// Dependency property for the title property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(NumericPropertyControl), new UIPropertyMetadata("Title"));

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
        public static readonly DependencyProperty MaxPropertyWidthProperty = DependencyProperty.Register(nameof(MaxPropertyWidth), typeof(double), typeof(NumericPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMaxPropertyWidth));

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
        public static readonly DependencyProperty MinPropertyWidthProperty = DependencyProperty.Register(nameof(MinPropertyWidth), typeof(double), typeof(NumericPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMinPropertyWidth));

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
        public static readonly DependencyProperty PropertyWidthProperty = DependencyProperty.Register(nameof(PropertyWidth), typeof(GridLength), typeof(NumericPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));

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
        public static readonly DependencyProperty ShowLeaderLineProperty = DependencyProperty.Register(nameof(ShowLeaderLine), typeof(bool), typeof(NumericPropertyControl), new UIPropertyMetadata(true));

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