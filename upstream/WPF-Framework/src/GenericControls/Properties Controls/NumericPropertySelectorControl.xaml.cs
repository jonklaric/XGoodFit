using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GenericControls
{
    /// <summary>
    /// A control that allows users to select or enter a numeric value from a predefined list.
    /// Supports validation, editable input, and custom formatting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
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
    public partial class NumericPropertySelectorControl : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumericPropertySelectorControl"/> class.
        /// </summary>
        public NumericPropertySelectorControl()
        {
            InitializeComponent();
            if (NumericOptions == null)
            {
                NumericOptions = new List<double> { 0d, 1d, 2d, 3d, 4d, 5d };
            }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedNumber"/>
        /// </summary>
        public static readonly DependencyProperty SelectedNumberProperty = DependencyProperty.Register(nameof(SelectedNumber), typeof(double), typeof(NumericPropertySelectorControl), new UIPropertyMetadata(0d));
        /// <summary>
        /// gets/sets the currently selected number.
        /// </summary>
        public double SelectedNumber
        {
            get
            {
                return (double)this.GetValue(SelectedNumberProperty);
            }
            set
            {
                this.SetValue(SelectedNumberProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="NumericOptions"/> dependency property.
        /// </summary>
        // Default null — reference-type DP defaults are shared across all instances.
        public static readonly DependencyProperty NumericOptionsProperty = DependencyProperty.Register(nameof(NumericOptions), typeof(IList<double>), typeof(NumericPropertySelectorControl), new PropertyMetadata(null));
        /// <summary>
        /// gets/sets the list of numeric options available for selection. 
        /// </summary>
        public IList<double> NumericOptions
        {
            get
            {
                return (IList<double>)this.GetValue(NumericOptionsProperty);
            }
            set
            {
                this.SetValue(NumericOptionsProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="IsEditable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(NumericPropertySelectorControl), new PropertyMetadata(true));
        /// <summary>
        /// gets/sets a value indicating whether the combo box is editable.
        /// </summary>
        public bool IsEditable
        {
            get
            {
                return (bool)this.GetValue(IsEditableProperty);
            }
            set
            {
                this.SetValue(IsEditableProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="CanHaveNegative"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanHaveNegativeProperty = DependencyProperty.Register(nameof(CanHaveNegative), typeof(bool), typeof(NumericPropertySelectorControl), new PropertyMetadata(true));
        /// <summary>
        /// gets/sets a value indicating whether negative numbers are allowed.
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
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(NumericPropertySelectorControl), new UIPropertyMetadata("Title"));
        /// <summary>
        /// Gets/sets the title label for the control.
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

        /// <summary>
        /// Identifies the <see cref="MaxPropertyWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxPropertyWidthProperty = DependencyProperty.Register(nameof(MaxPropertyWidth), typeof(double), typeof(NumericPropertySelectorControl), new UIPropertyMetadata(PropertyDefaults.DefaultMaxPropertyWidth));
        /// <summary>
        /// gets/sets the maximum allowed width for the property field.
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
        /// Identifies the <see cref="MinPropertyWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinPropertyWidthProperty = DependencyProperty.Register(nameof(MinPropertyWidth), typeof(double), typeof(NumericPropertySelectorControl), new UIPropertyMetadata(PropertyDefaults.DefaultMinPropertyWidth));
        /// <summary>
        /// gets/sets the minimum allowed width for the property field.
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
        /// Identifies the <see cref="PropertyWidth"/> dependency property.
        /// </summary> 
        public static readonly DependencyProperty PropertyWidthProperty = DependencyProperty.Register(nameof(PropertyWidth), typeof(GridLength), typeof(NumericPropertySelectorControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));
        /// <summary>
        /// gets/sets the width of the value field.
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
        /// Identifies the <see cref="ShowLeaderLine"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowLeaderLineProperty = DependencyProperty.Register(nameof(ShowLeaderLine), typeof(bool), typeof(NumericPropertySelectorControl), new UIPropertyMetadata(true));
        /// <summary>
        /// gets/sets a value indicating whether a leader line should be displayed.
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
        /// Gets the current rendered width of the property control.
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

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Updates property width if control size is changed.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement el = sender as FrameworkElement;
            ActualPropertyWidth = el.ActualWidth;
        }

        /// <summary>
        /// ComboBox preview text input.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            // Get the TextBox from the ComboBox template to check cursor position
            TextBox editableTextBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            int selectionStart = editableTextBox?.SelectionStart ?? 0;
            string selectedText = editableTextBox?.SelectedText;

            e.Handled = !NumberFormatHelper.IsValidNumericInput(
                e.Text,
                comboBox.Text,
                selectionStart,
                selectedText,
                CanHaveNegative,
                allowDecimal: true,
                allowScientific: false);
        }

        /// <summary>
        /// Combo box preview when Space is pressed.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        /// <summary>
        /// Workaround to allow single-click editing in editable ComboBox within DataGrid cells.
        /// Some contexts (e.g., DataGrid TemplateColumn, AvalonDock) consume MouseUp events
        /// before they reach the ComboBox TextBox.
        /// </summary>
        private bool _previewUp = false;

        /// <summary>
        /// Handles preview mouse up event for single-click editing support.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void NumericPropertySelector_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsEditable == false)
                return;
            _previewUp = true;
        }

        /// <summary>
        /// Handles mouse up event to focus the text box for single-click editing.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void NumericPropertySelector_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsEditable == false)
                return;
            if (_previewUp == true)
            {
                TextBox tb = (TextBox)this.NumericComboBox.Template.FindName("PART_EditableTextBox", this.NumericComboBox);
                tb.Focus();
            }
            _previewUp = false;
        }

    }

    /// <summary>
    /// Converts between double values and their string representations for use in data bindings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
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
    public class DoubleConverter : IValueConverter
    {
        /// <summary>
        /// Returns the input value unchanged.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The unchanged input value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        /// <summary>
        /// Converts a value back to a double, returning <see cref="Binding.DoNothing"/> if parsing fails.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The parsed double value, or <see cref="Binding.DoNothing"/> if parsing fails.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Binding.DoNothing;
            double d;
            if (NumberFormatHelper.TryParseDouble(value.ToString(), out d) == false)
                return Binding.DoNothing;
            return d;
        }
    }
}