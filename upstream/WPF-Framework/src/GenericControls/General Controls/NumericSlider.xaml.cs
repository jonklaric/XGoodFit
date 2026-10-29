using System.Windows;
using System.Windows.Controls;

namespace GenericControls
{
    /// <summary>
    /// A numeric slider control combining a slider with increment/decrement buttons.
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
    public partial class NumericSlider:UserControl
    {

        #region Construction

        /// <summary>
        /// Construct new numeric up-down control.
        /// </summary>
        public NumericSlider()
        {
            // This call is required by the designer.
            this.InitializeComponent();
            // Add any initialization after the InitializeComponent() call.
            // set the default values
            Minimum = 0d;
            Maximum = 100d;
            Value = 0.0d;
            Increment = 1d;
            DecimalPlaces = 0;
        }

        #endregion

        #region Members

        private int _decimalPlaces;
        private bool _thousandsSeperator;
        private string FormatString;

        /// <summary>
        /// Dependency property for setting the numeric text box value.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumericSlider), new PropertyMetadata(0.0d, SetText));

        /// <summary>
        /// Gets and sets the current value of the numeric slider control.
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
        /// Set the text after the value has changed.
        /// </summary>
        /// <param name="d">The dependency object that triggered the callback.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void SetText(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumericSlider thisControl = (NumericSlider)d;
            thisControl.FormatString = thisControl.ThousandsSeparator ? "N" : "F";
            thisControl.FormatString += thisControl.DecimalPlaces.ToString();

            double valueToFormat;
            double newValue = (double)e.NewValue;
            if (newValue > thisControl.Maximum)
            {
                valueToFormat = thisControl.Maximum;
            }
            else if (newValue < thisControl.Minimum)
            {
                valueToFormat = thisControl.Minimum;
            }
            else
            {
                valueToFormat = newValue;
            }
            thisControl.NumericTextBox.Text = NumberFormatHelper.FormatDouble(valueToFormat, thisControl.FormatString);
            thisControl.ValueChanged?.Invoke(thisControl.Value);
        }

        /// <summary>
        /// Dependency property for setting the Maximum value.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumericSlider), new PropertyMetadata(100d));

        /// <summary>
        /// Gets and sets the maximum value for the numeric slider control.
        /// </summary>
        public double Maximum
        {
            get
            {
                return (double)this.GetValue(MaximumProperty);
            }
            set
            {
                this.SetValue(MaximumProperty, value);
                this.NumericTextBox.MaxValue = value;
            }
        }

        /// <summary>
        /// Dependency property for setting the Minimum value.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericSlider), new PropertyMetadata(0d));

        /// <summary>
        /// Get and set the minimum value for the numeric slider control.
        /// </summary>
        public double Minimum
        {
            get
            {
                return (double)this.GetValue(MinimumProperty);
            }
            set
            {
                this.SetValue(MinimumProperty, value);
                this.NumericTextBox.MinValue = value;
            }
        }

        /// <summary>
        /// Dependency property for setting the Increment value.
        /// </summary>
        public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(nameof(Increment), typeof(double), typeof(NumericSlider), new PropertyMetadata(1d));

        /// <summary>
        /// Gets and sets the amount to increment and decrement on each button click.
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
        /// Gets and sets the number of decimal places to display.
        /// </summary>
        public int DecimalPlaces
        {
            get
            {
                return _decimalPlaces;
            }
            set
            {
                if (value < 0 || value > 99)
                {
                    throw new ArgumentOutOfRangeException("DecimalPlaces", "Value of '" + value.ToString() + "' is not valid. 'DecimalPlaces' should be between 0 and 99.");
                }
                _decimalPlaces = value;
            }
        }

        /// <summary>
        /// Gets and sets whether the thousands separator will be displayed.
        /// </summary>
        public bool ThousandsSeparator
        {
            get
            {
                return _thousandsSeperator;
            }
            set
            {
                _thousandsSeperator = value;
            }
        }

        /// <summary>
        /// Occurs when the slider value changes.
        /// </summary>
        public event ValueChangedEventHandler ValueChanged;

        /// <summary>
        /// Represents the method that handles the <see cref="ValueChanged"/> event.
        /// </summary>
        /// <param name="value">The new value of the slider.</param>
        public delegate void ValueChangedEventHandler(double value);

        #endregion

        #region Methods

        /// <summary>
        /// On click, increment up.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Routed event arguments.</param>
        private void cmdUp_Click(object sender, RoutedEventArgs e)
        {
            if (Value < Maximum)
            {
                Value += Increment;
            }
        }

        /// <summary>
        /// On click, increment down.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Routed event arguments.</param>
        private void cmdDown_Click(object sender, RoutedEventArgs e)
        {
            if (Value > Minimum)
            {
                Value -= Increment;
            }
        }

        #endregion

    }
}