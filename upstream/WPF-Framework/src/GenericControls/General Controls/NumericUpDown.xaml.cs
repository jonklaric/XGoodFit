using System.Windows;

namespace GenericControls
{
    /// <summary>
    /// A numeric up-down control with increment/decrement buttons.
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
    /// This control uses the Numeric TextBox control. This control is meant to mimic
    /// the behavior of the Winforms NumericUpDown control.
    /// </para>
    /// </remarks>
    public partial class NumericUpDown
    {

        #region Construction

        /// <summary>
        /// Construct new numeric up-down control.
        /// </summary>
        public NumericUpDown()
        {
            // This call is required by the designer.
            this.InitializeComponent();
            // Add any initialization after the InitializeComponent() call.
            // set the default values
            Minimum = -100;
            Maximum = 100d;
            Value = 0.0d;
            Increment = 1d;
            DecimalPlaces = 0;
        }

        #endregion

        #region Members

        private double _max;
        private double _min;
        private bool _settingBounds;
        private int _decimalPlaces;
        private double _increment = 1d;
        private bool _thousandsSeperator;
        private string FormatString;

        /// <summary>
        /// Dependency property for setting the numeric text box value.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumericUpDown), new PropertyMetadata(0.0d, SetText));

        /// <summary>
        /// Gets and sets the current value of the numeric up-down control.
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
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void SetText(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDown thisControl = (NumericUpDown)d;
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
        }

        /// <summary>
        /// Gets and sets the maximum value for the numeric up-down control.
        /// </summary>
        public double Maximum
        {
            get
            {
                return _max;
            }
            set
            {
                if (_settingBounds) { _max = value; return; }
                _settingBounds = true;
                if (value < Minimum)
                {
                    Minimum = value;
                }
                _max = value;
                this.NumericTextBox.MaxValue = value;
                _settingBounds = false;
            }
        }

        /// <summary>
        /// Get and set the minimum value for the numeric up-down control.
        /// </summary>
        public double Minimum
        {
            get
            {
                return _min;
            }
            set
            {
                if (_settingBounds) { _min = value; return; }
                _settingBounds = true;
                if (value > Maximum)
                {
                    Maximum = value;
                }
                _min = value;
                this.NumericTextBox.MinValue = value;
                _settingBounds = false;
            }
        }

        /// <summary>
        /// Gets and sets the amount to increment and decrement on each button click.
        /// </summary>
        public double Increment
        {
            get
            {
                return _increment;
            }
            set
            {
                if (value < 0d)
                {
                    throw new ArgumentOutOfRangeException(nameof(Increment), $"Value of '{NumberFormatHelper.FormatDouble(value)}' is not valid. The increment must be positive.");
                }
                _increment = value;
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

        #endregion

        #region Methods

        /// <summary>
        /// On click, increment up.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The routed event arguments.</param>
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
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The routed event arguments.</param>
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