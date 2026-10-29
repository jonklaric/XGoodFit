using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using OxyPlot;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls
{
    /// <summary>
    /// Control for editing axis properties in an OxyPlot chart.
    /// Provides UI elements for configuring axis type, range, labels, gridlines, and tick marks.
    /// </summary>
    public partial class AxisControl : UserControl
    {
        #region Fields

        private static readonly double Epsilon = 0.0000000000000001;

        #endregion

        /// <summary>
        /// Gets the available line style options for axis styling.
        /// </summary>
        public static List<DoubleCollection> LineStyleOptions => GenericControls.LineStyleSelectorControl.LineStyleOptions;

        /// <summary>
        /// Gets the available axis position options.
        /// </summary>
        public static List<OxyPlot.Axes.AxisPosition> AxisPositionOptions { get; } =
            new List<OxyPlot.Axes.AxisPosition>((OxyPlot.Axes.AxisPosition[])Enum.GetValues(typeof(OxyPlot.Axes.AxisPosition)));

        /// <summary>
        /// Gets the available axis tick style options.
        /// </summary>
        public static List<OxyPlot.Axes.TickStyle> AxisTickStyleOptions { get; } =
            new List<OxyPlot.Axes.TickStyle>((OxyPlot.Axes.TickStyle[])Enum.GetValues(typeof(OxyPlot.Axes.TickStyle)));

        /// <summary>
        /// Gets the available axis layer options.
        /// </summary>
        public static List<OxyPlot.Axes.AxisLayer> AxisLayerOptions { get; } =
            new List<OxyPlot.Axes.AxisLayer>((OxyPlot.Axes.AxisLayer[])Enum.GetValues(typeof(OxyPlot.Axes.AxisLayer)));

        /// <summary>
        /// Identifies the Axis dependency property.
        /// </summary>
        public static readonly DependencyProperty AxisProperty = DependencyProperty.Register(
            nameof(Axis), typeof(Wpf.Axis), typeof(AxisControl),
            new PropertyMetadata(null, InitializePlot));

        /// <summary>
        /// Gets or sets the axis being edited by this control.
        /// </summary>
        public Wpf.Axis Axis
        {
            get => (Wpf.Axis)GetValue(AxisProperty);
            set => SetValue(AxisProperty, value);
        }

        private Wpf.LinearAxis? _oldLinearAxis;
        private Wpf.LogarithmicAxis? _oldLogAxis;
        private bool _ignoreMaxMinChange = false;

        /// <summary>
        /// Occurs when the axis type is changed by the user.
        /// </summary>
        public event Action<Wpf.Axis, Wpf.Axis>? AxisTypeChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisControl"/> class.
        /// </summary>
        public AxisControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the plot when the Axis property changes.
        /// Configures UI elements and bindings based on the axis type.
        /// Forces a layout refresh after configuring the axis.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event args containing the old and new axis values.</param>
        private static void InitializePlot(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(AxisControl)) return;
            var thisControl = (AxisControl)d;


            var oldAxis = e.OldValue as Wpf.Axis;
            if (oldAxis != null)
            {
                oldAxis.PropertyChanged -= thisControl.OnAxisPropertyChanged;
            }

            var newAxis = e.NewValue as Wpf.Axis;
            if (newAxis == null) return;
            newAxis.PropertyChanged += thisControl.OnAxisPropertyChanged;
            if (thisControl.Content == null)
            {
                return;
            }

            var axisTypeComboBox = thisControl.AxisTypeSelector.InnerContent as ComboBox;
            if (axisTypeComboBox == null)
            {
                return;
            }

            bool isSupportedType = true;

            axisTypeComboBox.SelectionChanged -= thisControl.AxisTypeComboBox_SelectionChanged;

            // Reset axis type selector to visible (CategoryAxis and DateTimeAxis will collapse it)
            thisControl.AxisTypeSelector.Visibility = Visibility.Visible;

            // Hide axis specific properties and clear bindings
            thisControl.PowerPaddingControl.Visibility = Visibility.Collapsed;
            BindingOperations.ClearBinding(thisControl.PowerPaddingControl, GenericControls.BooleanPropertyControl.IsSelectedProperty);
            thisControl.GapWidthSelector.Visibility = Visibility.Collapsed;
            BindingOperations.ClearBinding(thisControl.GapWidthSelector, GenericControls.NumericPropertySelectorControl.SelectedNumberProperty);
            thisControl.AxisLabelsControl.Visibility = Visibility.Collapsed;
            BindingOperations.ClearBinding(thisControl.AxisLabelsControl, GenericControls.StringListPropertyControl.StringListProperty);
            thisControl.TickCenteredControl.Visibility = Visibility.Collapsed;
            BindingOperations.ClearBinding(thisControl.TickCenteredControl, GenericControls.BooleanPropertyControl.IsSelectedProperty);
            thisControl.DateAxisMinimum.Visibility = Visibility.Collapsed;
            thisControl.DateAxisMaximum.Visibility = Visibility.Collapsed;
            thisControl.AxisMinimum.Visibility = Visibility.Collapsed;
            thisControl.AxisMaximum.Visibility = Visibility.Collapsed;

            var axisType = newAxis.GetType();

            if (axisType == typeof(Wpf.LinearAxis))
            {
                axisTypeComboBox.SelectedIndex = 0;
                thisControl.AxisMinimum.Visibility = Visibility.Visible;
                thisControl.AxisMaximum.Visibility = Visibility.Visible;
                thisControl.AxisMinimum.DefaultNumber = double.NaN;
                thisControl.AxisMinimum.MinValue = double.MinValue;
                thisControl.AxisMinimum.MaxValue = double.MaxValue;
                thisControl.AxisMaximum.DefaultNumber = double.NaN;
                thisControl.AxisMaximum.MinValue = double.MinValue;
                thisControl.AxisMaximum.MaxValue = double.MaxValue;
                thisControl.LabelTypeSelector.Visibility = Visibility.Visible;
                thisControl.DecimalPlaces.Visibility = Visibility.Visible;
            }
            else if (axisType == typeof(Wpf.LogarithmicAxis))
            {
                axisTypeComboBox.SelectedIndex = 1;
                thisControl.AxisMinimum.Visibility = Visibility.Visible;
                thisControl.AxisMaximum.Visibility = Visibility.Visible;
                thisControl.AxisMinimum.DefaultNumber = double.NaN;
                thisControl.AxisMinimum.MinValue = Epsilon;
                thisControl.AxisMinimum.MaxValue = double.MaxValue;
                thisControl.AxisMaximum.DefaultNumber = double.NaN;
                thisControl.AxisMaximum.MinValue = Epsilon;
                thisControl.AxisMaximum.MaxValue = double.MaxValue;
                thisControl.LabelTypeSelector.Visibility = Visibility.Visible;
                thisControl.DecimalPlaces.Visibility = Visibility.Visible;
                thisControl.PowerPaddingControl.Visibility = Visibility.Visible;
                var powerPaddingBinding = new Binding(nameof(Wpf.LogarithmicAxis.PowerPadding))
                {
                    Source = newAxis,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Mode = BindingMode.TwoWay
                };
                BindingOperations.SetBinding(thisControl.PowerPaddingControl, GenericControls.BooleanPropertyControl.IsSelectedProperty, powerPaddingBinding);
            }
            else if (axisType == typeof(Wpf.NormalProbabilityAxis))
            {
                axisTypeComboBox.SelectedIndex = 2;
                thisControl.AxisMinimum.Visibility = Visibility.Visible;
                thisControl.AxisMaximum.Visibility = Visibility.Visible;
                thisControl.AxisMinimum.DefaultNumber = 0.0000001;
                thisControl.AxisMinimum.MinValue = Epsilon;
                thisControl.AxisMinimum.MaxValue = 1 - Epsilon;
                thisControl.AxisMaximum.DefaultNumber = 0.999;
                thisControl.AxisMaximum.MinValue = Epsilon;
                thisControl.AxisMaximum.MaxValue = 1 - Epsilon;
                thisControl.LabelTypeSelector.Visibility = Visibility.Collapsed;
                thisControl.DecimalPlaces.Visibility = Visibility.Collapsed;
            }
            else if (axisType == typeof(Wpf.GumbelProbabilityAxis))
            {
                axisTypeComboBox.SelectedIndex = 3;
                thisControl.AxisMinimum.Visibility = Visibility.Visible;
                thisControl.AxisMaximum.Visibility = Visibility.Visible;
                thisControl.AxisMinimum.DefaultNumber = 0.0000001;
                thisControl.AxisMinimum.MinValue = Epsilon;
                thisControl.AxisMinimum.MaxValue = 1 - Epsilon;
                thisControl.AxisMaximum.DefaultNumber = 0.99;
                thisControl.AxisMaximum.MinValue = Epsilon;
                thisControl.AxisMaximum.MaxValue = 1 - Epsilon;
                thisControl.LabelTypeSelector.Visibility = Visibility.Collapsed;
                thisControl.DecimalPlaces.Visibility = Visibility.Collapsed;
            }
            else if (axisType == typeof(Wpf.CategoryAxis))
            {
                thisControl.AxisTypeSelector.Visibility = Visibility.Collapsed;

                thisControl.GapWidthSelector.Visibility = Visibility.Visible;
                var gapWidthBinding = new Binding(nameof(Wpf.CategoryAxis.GapWidth)) { Source = newAxis, Mode = BindingMode.TwoWay };
                BindingOperations.SetBinding(thisControl.GapWidthSelector, GenericControls.NumericPropertySelectorControl.SelectedNumberProperty, gapWidthBinding);

                thisControl.AxisLabelsControl.Visibility = Visibility.Visible;
                var categoryAxis = (Wpf.CategoryAxis)newAxis;
                if (categoryAxis.ItemsSource != null && (categoryAxis.Labels == null || categoryAxis.Labels.Count == 0))
                {
                    var axisLabelsBinding = new Binding(nameof(Wpf.CategoryAxis.ItemsSource))
                    {
                        Source = newAxis,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        Mode = BindingMode.TwoWay
                    };
                    BindingOperations.SetBinding(thisControl.AxisLabelsControl, GenericControls.StringListPropertyControl.StringListProperty, axisLabelsBinding);
                }
                else
                {
                    var axisLabelsBinding = new Binding(nameof(Wpf.CategoryAxis.Labels))
                    {
                        Source = newAxis,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        Mode = BindingMode.TwoWay
                    };
                    BindingOperations.SetBinding(thisControl.AxisLabelsControl, GenericControls.StringListPropertyControl.StringListProperty, axisLabelsBinding);
                }

                thisControl.TickCenteredControl.Visibility = Visibility.Visible;
                var tickCenteredBinding = new Binding(nameof(Wpf.CategoryAxis.IsTickCentered)) { Source = newAxis };
                BindingOperations.SetBinding(thisControl.TickCenteredControl, GenericControls.BooleanPropertyControl.IsSelectedProperty, tickCenteredBinding);

                thisControl.LabelTypeSelector.Visibility = Visibility.Collapsed;
                thisControl.DecimalPlaces.Visibility = Visibility.Collapsed;
            }
            else if (axisType == typeof(Wpf.DateTimeAxis))
            {
                thisControl.DateAxisMinimum.Visibility = Visibility.Visible;
                thisControl.DateAxisMaximum.Visibility = Visibility.Visible;
                thisControl.AxisTypeSelector.Visibility = Visibility.Collapsed;
                thisControl.LabelTypeSelector.Visibility = Visibility.Collapsed;
                thisControl.DecimalPlaces.Visibility = Visibility.Collapsed;
            }
            else
            {
                isSupportedType = false;
            }

            BindingOperations.GetBindingExpression(thisControl.AxisMinimum, GenericControls.NumericAutoPropertyControl.NumberProperty)?.UpdateSource();
            BindingOperations.GetBindingExpression(thisControl.AxisMinimum, GenericControls.NumericAutoPropertyControl.NumberProperty)?.UpdateTarget();
            BindingOperations.GetBindingExpression(thisControl.AxisMaximum, GenericControls.NumericAutoPropertyControl.NumberProperty)?.UpdateSource();
            BindingOperations.GetBindingExpression(thisControl.AxisMaximum, GenericControls.NumericAutoPropertyControl.NumberProperty)?.UpdateTarget();

            axisTypeComboBox.SelectionChanged += thisControl.AxisTypeComboBox_SelectionChanged;

            if (isSupportedType)
            {
                axisTypeComboBox.IsEnabled = true;
                axisTypeComboBox.Visibility = Visibility.Visible;
            }
            else
            {
                axisTypeComboBox.SelectedIndex = -1;
                axisTypeComboBox.IsEnabled = false;
                axisTypeComboBox.Visibility = Visibility.Collapsed;
            }

            if (newAxis.GetType() != typeof(Wpf.DateTimeAxis) && newAxis.GetType() != typeof(Wpf.CategoryAxis))
            {
                thisControl.SyncLabelTypeFromStringFormat();
            }

            // Force layout update to sync bindings after axis change
            thisControl.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                thisControl.UpdateLayout();
            }));
        }

        /// <summary>
        /// Identifies the TabItemStyle dependency property.
        /// </summary>
        public static readonly DependencyProperty TabItemStyleProperty = DependencyProperty.Register(
            nameof(TabItemStyle), typeof(Style), typeof(AxisControl),
            new PropertyMetadata(new Style(typeof(TabItem))));

        /// <summary>
        /// Gets or sets the style applied to tab items in this control.
        /// </summary>
        public Style TabItemStyle
        {
            get => (Style)GetValue(TabItemStyleProperty);
            set => SetValue(TabItemStyleProperty, value);
        }

        /// <summary>
        /// Identifies the ExpanderStyle dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderStyleProperty = DependencyProperty.Register(
            nameof(ExpanderStyle), typeof(Style), typeof(AxisControl),
            new PropertyMetadata(new Style(typeof(Expander))));

        /// <summary>
        /// Gets or sets the style applied to expanders in this control.
        /// </summary>
        public Style ExpanderStyle
        {
            get => (Style)GetValue(ExpanderStyleProperty);
            set => SetValue(ExpanderStyleProperty, value);
        }

        /// <summary>
        /// Converts an axis to a logarithmic axis while preserving compatible properties.
        /// </summary>
        /// <param name="wpfAxis">The source axis to convert.</param>
        /// <param name="logBase">The logarithm base to use.</param>
        /// <param name="powerPadding">Whether to use power padding.</param>
        /// <returns>A new logarithmic axis with properties copied from the source.</returns>
        public static Wpf.LogarithmicAxis ConvertAxisToLogarithmicAxis(Wpf.Axis wpfAxis, double logBase = 10, bool powerPadding = true)
        {
            var newAxis = new Wpf.LogarithmicAxis { Base = logBase, PowerPadding = powerPadding };
            newAxis.FromAxisProperties(wpfAxis);
            if (newAxis.Minimum <= 0) newAxis.Minimum = Epsilon;
            newAxis.Maximum = wpfAxis.Maximum;
            newAxis.StartPosition = wpfAxis.StartPosition;
            newAxis.EndPosition = wpfAxis.EndPosition;
            return newAxis;
        }

        /// <summary>
        /// Converts an axis to a linear axis while preserving compatible properties.
        /// </summary>
        /// <param name="wpfAxis">The source axis to convert.</param>
        /// <param name="formatAsFractions">Whether to format values as fractions.</param>
        /// <param name="fractionUnits">The fraction unit value.</param>
        /// <param name="fractionSymbol">The symbol to use for fractions.</param>
        /// <returns>A new linear axis with properties copied from the source.</returns>
        public static Wpf.LinearAxis ConvertAxisToLinearAxis(Wpf.Axis wpfAxis, bool formatAsFractions = false, double fractionUnits = 1, string? fractionSymbol = null)
        {
            var newAxis = new Wpf.LinearAxis
            {
                FormatAsFractions = formatAsFractions,
                FractionUnit = fractionUnits,
                FractionUnitSymbol = fractionSymbol
            };
            newAxis.FromAxisProperties(wpfAxis);
            newAxis.Minimum = wpfAxis.Minimum;
            newAxis.Maximum = wpfAxis.Maximum;
            newAxis.StartPosition = wpfAxis.StartPosition;
            newAxis.EndPosition = wpfAxis.EndPosition;
            return newAxis;
        }

        /// <summary>
        /// Converts an axis to a normal probability axis while preserving compatible properties.
        /// </summary>
        /// <param name="wpfAxis">The source axis to convert.</param>
        /// <returns>A new normal probability axis with properties copied from the source.</returns>
        public static Wpf.NormalProbabilityAxis ConvertAxisToNormalAxis(Wpf.Axis wpfAxis)
        {
            var newAxis = new Wpf.NormalProbabilityAxis();
            newAxis.FromAxisProperties(wpfAxis);
            if (newAxis.Minimum < Epsilon) newAxis.Minimum = 0.0000001;
            if (newAxis.Maximum > 0.999 || double.IsNaN(newAxis.Maximum)) newAxis.Maximum = 0.999;
            newAxis.StartPosition = wpfAxis.StartPosition;
            newAxis.EndPosition = wpfAxis.EndPosition;
            return newAxis;
        }

        /// <summary>
        /// Converts an axis to a Gumbel probability axis while preserving compatible properties.
        /// </summary>
        /// <param name="wpfAxis">The source axis to convert.</param>
        /// <returns>A new Gumbel probability axis with properties copied from the source.</returns>
        public static Wpf.GumbelProbabilityAxis ConvertAxisToGumbelAxis(Wpf.Axis wpfAxis)
        {
            var newAxis = new Wpf.GumbelProbabilityAxis();
            newAxis.FromAxisProperties(wpfAxis);
            if (newAxis.Minimum < Epsilon) newAxis.Minimum = 0.0000001;
            if (newAxis.Maximum > 0.99 || double.IsNaN(newAxis.Maximum)) newAxis.Maximum = 0.99;
            newAxis.StartPosition = wpfAxis.StartPosition;
            newAxis.EndPosition = wpfAxis.EndPosition;
            return newAxis;
        }

        /// <summary>
        /// Converts an axis to a date time axis while preserving compatible properties.
        /// </summary>
        /// <param name="wpfAxis">The source axis to convert.</param>
        /// <returns>A new date time axis with properties copied from the source.</returns>
        public static Wpf.DateTimeAxis ConvertAxisToDateTimeAxis(Wpf.Axis wpfAxis)
        {
            var newAxis = new Wpf.DateTimeAxis();
            newAxis.FromAxisProperties(wpfAxis);
            newAxis.Minimum = wpfAxis.Minimum;
            newAxis.Maximum = wpfAxis.Maximum;
            newAxis.StartPosition = wpfAxis.StartPosition;
            newAxis.EndPosition = wpfAxis.EndPosition;
            return newAxis;
        }

        /// <summary>
        /// Handles the axis type selection change event.
        /// Converts the axis to the selected type and updates the plot.
        /// </summary>
        /// <param name="sender">The ComboBox that triggered the event.</param>
        /// <param name="e">The selection changed event args.</param>
        private void AxisTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) return;

            var axisTypeComboBox = sender as ComboBox;
            if (axisTypeComboBox == null) return;
            if (axisTypeComboBox.SelectedIndex == -1) return;
            if (Axis == null) return;
            if (Axis.Parent == null) return;
            if (Axis.Parent.GetType() != typeof(Wpf.Plot)) return;

            var axisType = Axis.GetType();
            if (axisType == typeof(Wpf.LinearAxis))
                _oldLinearAxis = (Wpf.LinearAxis)Axis;
            else if (axisType == typeof(Wpf.LogarithmicAxis))
                _oldLogAxis = (Wpf.LogarithmicAxis)Axis;

            var thePlot = (Wpf.Plot)Axis.Parent;
            Wpf.Axis newAxis;

            switch (axisTypeComboBox.SelectedIndex)
            {
                case 0: // Linear
                    AxisMinimum.DefaultNumber = double.NaN;
                    AxisMinimum.MinValue = double.MinValue;
                    AxisMinimum.MaxValue = double.MaxValue;
                    AxisMaximum.DefaultNumber = double.NaN;
                    AxisMaximum.MinValue = double.MinValue;
                    AxisMaximum.MaxValue = double.MaxValue;

                    if (Axis.GetType() == typeof(Wpf.LinearAxis)) return;
                    newAxis = ConvertAxisToLinearAxis(Axis);

                    LabelTypeSelector.Visibility = Visibility.Visible;
                    DecimalPlaces.Visibility = Visibility.Visible;
                    break;

                case 1: // Logarithmic
                    AxisMinimum.DefaultNumber = double.NaN;
                    AxisMinimum.MinValue = Epsilon;
                    AxisMinimum.MaxValue = double.MaxValue;
                    AxisMaximum.DefaultNumber = double.NaN;
                    AxisMaximum.MinValue = Epsilon;
                    AxisMaximum.MaxValue = double.MaxValue;

                    if (Axis.GetType() == typeof(Wpf.LogarithmicAxis)) return;
                    newAxis = ConvertAxisToLogarithmicAxis(Axis);

                    LabelTypeSelector.Visibility = Visibility.Visible;
                    DecimalPlaces.Visibility = Visibility.Visible;
                    break;

                case 2: // Normal Probability
                    if (Axis.InternalAxis.DataMinimum < 0 || Axis.InternalAxis.DataMaximum > 1)
                    {
                        GenericControls.MessageBox.Show("Axis cannot be converted to a Normal probability axis because the data is not between 0 and 1.",
                            "Normal Probability Axis", MessageBoxButton.OK, MessageBoxImage.Error);
                        axisTypeComboBox.SelectedItem = e.RemovedItems[0];
                        e.Handled = true;
                        return;
                    }

                    AxisMinimum.DefaultNumber = 0.0000001;
                    AxisMinimum.MinValue = Epsilon;
                    AxisMinimum.MaxValue = 1 - Epsilon;
                    AxisMaximum.DefaultNumber = 0.999;
                    AxisMaximum.MinValue = Epsilon;
                    AxisMaximum.MaxValue = 1 - Epsilon;

                    if (Axis.GetType() == typeof(Wpf.NormalProbabilityAxis)) return;
                    newAxis = ConvertAxisToNormalAxis(Axis);

                    LabelTypeSelector.Visibility = Visibility.Collapsed;
                    DecimalPlaces.Visibility = Visibility.Collapsed;
                    break;

                case 3: // Gumbel Probability
                    if (Axis.InternalAxis.DataMinimum < 0 || Axis.InternalAxis.DataMaximum > 1)
                    {
                        GenericControls.MessageBox.Show("Axis cannot be converted to a Gumbel probability axis because the data is not between 0 and 1.",
                            "Gumbel Probability Axis", MessageBoxButton.OK, MessageBoxImage.Error);
                        axisTypeComboBox.SelectedItem = e.RemovedItems[0];
                        e.Handled = true;
                        return;
                    }

                    AxisMinimum.DefaultNumber = 0.0000001;
                    AxisMinimum.MinValue = Epsilon;
                    AxisMinimum.MaxValue = 1 - Epsilon;
                    AxisMaximum.DefaultNumber = 0.99;
                    AxisMaximum.MinValue = Epsilon;
                    AxisMaximum.MaxValue = 1 - Epsilon;

                    if (Axis.GetType() == typeof(Wpf.GumbelProbabilityAxis)) return;
                    newAxis = ConvertAxisToGumbelAxis(Axis);

                    LabelTypeSelector.Visibility = Visibility.Collapsed;
                    DecimalPlaces.Visibility = Visibility.Collapsed;
                    break;

                case 4: // Date Time
                    if (Axis.GetType() == typeof(Wpf.DateTimeAxis)) return;
                    newAxis = ConvertAxisToDateTimeAxis(Axis);
                    break;

                default:
                    return;
            }

            thePlot.ReplaceAxis(Axis, newAxis);
            var oldAxis = Axis;
            Axis = newAxis;

            AxisTypeChanged?.Invoke(oldAxis, newAxis);

            thePlot.InvalidatePlot();
        }

        /// <summary>
        /// Closes all expander controls in the axis control.
        /// </summary>
        public void CloseExpanders()
        {
            GeneralEXP.IsExpanded = false;
            LabelsEXP.IsExpanded = false;
            TitleEXP.IsExpanded = false;
            MajorGridLinesEXP.IsExpanded = false;
            MinorGridLinesEXP.IsExpanded = false;
            TickOptionsEXP.IsExpanded = false;
        }

        /// <summary>
        /// Handles validation when the axis minimum value is about to change.
        /// Prevents setting a minimum value greater than or equal to the maximum.
        /// </summary>
        /// <param name="oldValue">The previous minimum value.</param>
        /// <param name="newValue">The new minimum value to validate.</param>
        /// <param name="cancel">Set to true to cancel the change.</param>
        private void AxisMinimum_PreviewNumberChanged(object oldValue, object newValue, ref bool cancel)
        {
            if (_ignoreMaxMinChange) return;

            // newValue always comes in boxed as a double from NumericTextBox; the prior
            // double.TryParse(newValue.ToString(), ...) round-trip was vestigial.
            if (newValue is not double newNumber) return;

            if (newNumber >= AxisMaximum.Number)
            {
                double oldNumber = (double)oldValue;
                _ignoreMaxMinChange = true;
                AxisMinimum.Number = oldNumber;
                _ignoreMaxMinChange = false;
                cancel = true;
            }
        }

        /// <summary>
        /// Handles validation when the axis maximum value is about to change.
        /// Prevents setting a maximum value less than or equal to the minimum.
        /// </summary>
        /// <param name="oldValue">The previous maximum value.</param>
        /// <param name="newValue">The new maximum value to validate.</param>
        /// <param name="cancel">Set to true to cancel the change.</param>
        private void AxisMaximum_PreviewNumberChanged(object oldValue, object newValue, ref bool cancel)
        {
            if (_ignoreMaxMinChange) return;

            // newValue always comes in boxed as a double from NumericTextBox; the prior
            // double.TryParse(newValue.ToString(), ...) round-trip was vestigial.
            if (newValue is not double newNumber) return;

            if (newNumber <= AxisMinimum.Number)
            {
                double oldNumber = (double)oldValue;
                _ignoreMaxMinChange = true;
                AxisMaximum.Number = oldNumber;
                _ignoreMaxMinChange = false;
                cancel = true;
            }
        }

        private string _stringFormatCategory = "";
        private string _stringFormatDecimals = "";
        private bool _isSyncingFromStringFormat;

        /// <summary>
        /// Handles the label type selection change event.
        /// Updates the axis string format based on the selected label type.
        /// </summary>
        /// <param name="sender">The ComboBox that triggered the event.</param>
        /// <param name="e">The selection changed event args.</param>
        private void LabelType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingFromStringFormat) return;
            if (sender == null) return;
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;
            if (comboBox.SelectedIndex == -1) return;
            if (Axis.Parent == null) return;
            if (Axis.Parent.GetType() != typeof(Wpf.Plot)) return;

            switch (comboBox.SelectedIndex)
            {
                case 0: // Currency
                    _stringFormatCategory = "C";
                    break;
                case 1: // General
                    _stringFormatCategory = "G";
                    break;
                case 2: // Number
                    _stringFormatCategory = "N";
                    break;
                case 3: // Percent
                    _stringFormatCategory = "P";
                    break;
                case 4: // Scientific
                    _stringFormatCategory = "E";
                    break;
            }
            Axis.StringFormat = _stringFormatCategory + _stringFormatDecimals;
        }

        /// <summary>
        /// Handles the decimal places property change event.
        /// Updates the axis string format when the number of decimal places changes.
        /// </summary>
        /// <param name="sender">The control that triggered the event.</param>
        /// <param name="e">The property changed event args.</param>
        private void DecimalPlaces_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isSyncingFromStringFormat) return;
            if (e.PropertyName == nameof(GenericControls.NumericAutoPropertyControl.Number))
            {
                if (double.IsNaN(DecimalPlaces.Number))
                    _stringFormatDecimals = "";
                else
                    _stringFormatDecimals = ((int)DecimalPlaces.Number).ToString(CultureInfo.InvariantCulture);

                Axis.StringFormat = _stringFormatCategory + _stringFormatDecimals;
            }
        }

        /// <summary>
        /// Handles validation when the decimal places value is about to change.
        /// Ensures the value is floored to a whole number.
        /// </summary>
        /// <param name="oldValue">The previous decimal places value.</param>
        /// <param name="newValue">The new decimal places value to validate.</param>
        /// <param name="cancel">Set to true to cancel the change.</param>
        private void DecimalPlaces_PreviewNumberChanged(object oldValue, object newValue, ref bool cancel)
        {
            // newValue always comes in boxed as a double from NumericTextBox; the prior
            // double.TryParse(newValue.ToString(), ...) round-trip was vestigial.
            if (newValue is not double newNumber) return;
            DecimalPlaces.Number = Math.Floor(newNumber);
        }

        /// <summary>
        /// Handles property changes on the current axis to keep the label type and decimal places
        /// controls in sync when <c>StringFormat</c> changes externally (e.g. via undo/redo).
        /// </summary>
        /// <param name="sender">The axis whose property changed.</param>
        /// <param name="e">The property changed event args.</param>
        private void OnAxisPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "StringFormat")
            {
                _isSyncingFromStringFormat = true;
                try
                {
                    SyncLabelTypeFromStringFormat();
                }
                finally
                {
                    _isSyncingFromStringFormat = false;
                }
            }
        }

        /// <summary>
        /// Synchronizes the Label Type ComboBox and Decimal Places control from the current
        /// <see cref="Axis"/>.<c>StringFormat</c> value.
        /// </summary>
        /// <remarks>
        /// Parses the <c>StringFormat</c> string (e.g. "N2", "E4", "G") into its category
        /// prefix and decimal suffix, sets the appropriate ComboBox selection and decimal
        /// places number, and updates the backing fields <see cref="_stringFormatCategory"/>
        /// and <see cref="_stringFormatDecimals"/>. The <c>LabelType_SelectionChanged</c>
        /// handler is temporarily unsubscribed to prevent re-entrant writes back to
        /// <c>StringFormat</c>.
        /// </remarks>
        private void SyncLabelTypeFromStringFormat()
        {
            var labelTypeComboBox = LabelTypeSelector.InnerContent as ComboBox;
            if (labelTypeComboBox == null || Axis == null) return;

            labelTypeComboBox.SelectionChanged -= LabelType_SelectionChanged;
            try
            {
                string stringFormatCategory = "G";
                string stringFormatDecimal = "";

                if (Axis.StringFormat != null && Axis.StringFormat.Length > 0)
                {
                    stringFormatCategory = Axis.StringFormat.Substring(0, 1);
                    if (stringFormatCategory == "C" || stringFormatCategory == "c")
                        labelTypeComboBox.SelectedIndex = 0;
                    else if (stringFormatCategory == "G" || stringFormatCategory == "g")
                        labelTypeComboBox.SelectedIndex = 1;
                    else if (stringFormatCategory == "N" || stringFormatCategory == "n")
                        labelTypeComboBox.SelectedIndex = 2;
                    else if (stringFormatCategory == "P" || stringFormatCategory == "p")
                        labelTypeComboBox.SelectedIndex = 3;
                    else if (stringFormatCategory == "E" || stringFormatCategory == "e")
                        labelTypeComboBox.SelectedIndex = 4;
                    else
                    {
                        labelTypeComboBox.SelectedIndex = 1;
                        stringFormatCategory = "G";
                    }
                }
                else
                {
                    labelTypeComboBox.SelectedIndex = 1;
                }

                if (Axis.StringFormat != null && Axis.StringFormat.Length > 1)
                {
                    stringFormatDecimal = Axis.StringFormat.Substring(1);
                    if (double.TryParse(stringFormatDecimal, NumberStyles.Integer, CultureInfo.InvariantCulture, out double decimals))
                    {
                        DecimalPlaces.Number = decimals;
                    }
                }

                if (stringFormatDecimal == "") DecimalPlaces.Number = double.NaN;
                _stringFormatCategory = stringFormatCategory;
                _stringFormatDecimals = stringFormatDecimal;
            }
            finally
            {
                labelTypeComboBox.SelectionChanged += LabelType_SelectionChanged;
            }
        }
    }

    /// <summary>
    /// Converter for determining if an axis is reversed based on start and end positions.
    /// </summary>
    public class ReverseAxisConverter : IMultiValueConverter
    {
        private Wpf.Axis? _axis;

        /// <summary>
        /// Converts start position, end position, and axis to a boolean indicating if the axis is reversed.
        /// </summary>
        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            // values[0] / values[1] are bound to Axis.StartPosition / Axis.EndPosition (double).
            // The prior double.TryParse(values[i]?.ToString(), ...) round-trip was vestigial.
            double startPosition = values[0] is double s ? s : 0d;
            double endPosition = values[1] is double e ? e : 0d;

            if (values[2] == null)
            {
                _axis = null;
                return false;
            }
            _axis = values[2] as Wpf.Axis;

            return endPosition < startPosition;
        }

        /// <summary>
        /// Converts a reversed boolean back to start and end positions.
        /// </summary>
        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            if (_axis == null) return new object?[] { 0.0, 1.0, null };

            bool result;
            bool.TryParse(value?.ToString(), out result);

            bool switchPositions = false;
            if (result == false)
                switchPositions = _axis.StartPosition > _axis.EndPosition;
            else
                switchPositions = _axis.StartPosition < _axis.EndPosition;

            if (switchPositions) return new object?[] { _axis.EndPosition, _axis.StartPosition, _axis };

            return new object?[] { _axis.StartPosition, _axis.EndPosition, _axis };
        }
    }

    /// <summary>
    /// Converts OxyPlot LineStyle to WPF DoubleCollection for dash arrays.
    /// Returns matching instances from LineStyleOptions for proper ComboBox selection.
    /// </summary>
    public class OxyLineStyleToDashArrayConverter : IValueConverter
    {
        /// <summary>
        /// Converts an OxyPlot LineStyle to a WPF DoubleCollection dash array.
        /// Returns the matching instance from LineStyleOptions to ensure proper ComboBox selection.
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return AxisControl.LineStyleOptions?.FirstOrDefault() ?? new DoubleCollection();
            if (value.GetType() != typeof(OxyPlot.LineStyle)) return AxisControl.LineStyleOptions?.FirstOrDefault() ?? new DoubleCollection();

            var lineStyle = (OxyPlot.LineStyle)value;
            double[]? dashArray;

            // Solid, Automatic, and None all render as solid lines (empty dash array)
            if (lineStyle == LineStyle.Solid || lineStyle == LineStyle.Automatic || lineStyle == LineStyle.None)
            {
                dashArray = Array.Empty<double>();
            }
            else
            {
                dashArray = lineStyle.GetDashArray();
                if (dashArray == null)
                {
                    // Fallback for any unrecognized style - treat as solid
                    dashArray = Array.Empty<double>();
                }
            }

            // Find matching instance from LineStyleOptions for proper ComboBox selection
            var options = AxisControl.LineStyleOptions;
            if (options != null)
            {
                foreach (var option in options)
                {
                    if (option.Count == dashArray.Length && option.SequenceEqual(dashArray))
                    {
                        return option;
                    }
                }

                // If no exact match, return first option (Solid) as fallback
                if (options.Count > 0)
                {
                    return options[0];
                }
            }

            // Fallback: return new collection (won't select in ComboBox, but won't crash)
            return new DoubleCollection(dashArray);
        }

        /// <summary>
        /// Converts a WPF DoubleCollection dash array back to an OxyPlot LineStyle.
        /// </summary>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return OxyPlot.LineStyle.None;
            if (value.GetType() != typeof(DoubleCollection)) return OxyPlot.LineStyle.None;
            var dashArray = (DoubleCollection)value;

            if (dashArray.Count == 0) return OxyPlot.LineStyle.Solid;

            foreach (var style in (OxyPlot.LineStyle[])Enum.GetValues(typeof(OxyPlot.LineStyle)))
            {
                var oxyArray = style.GetDashArray();
                if (oxyArray != null && dashArray.SequenceEqual(oxyArray)) return style;
            }

            return OxyPlot.LineStyle.None;
        }
    }

    /// <summary>
    /// Converts empty strings to null values for binding purposes.
    /// </summary>
    public class EmptyStringToNullConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value to its string representation.
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return value;
            return value.ToString();
        }

        /// <summary>
        /// Converts empty strings to null, otherwise returns the value.
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (value.ToString() == "") return null;
            return value;
        }
    }

    /// <summary>
    /// Converts between tab control dimensions for vertical tab sizing.
    /// </summary>
    public class VerticalTabSizeConverter : IMultiValueConverter
    {
        /// <summary>
        /// Calculates the height for each tab based on the tab control size and item count.
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var tabControl = (TabControl)values[0];
            double height = tabControl.ActualHeight / tabControl.Items.Count;
            if (height < 12) return 0;
            return height - 1;
        }

        /// <summary>
        /// Not implemented. Throws NotImplementedException.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts between OxyPlot date axis double values and DateTime objects.
    /// </summary>
    public class DateToNumberConverter : IValueConverter
    {
        /// <summary>
        /// Converts an OxyPlot double date value to a DateTime.
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || value.GetType() != typeof(double)) return value;
#pragma warning disable CS0618 // Type or member is obsolete
            return OxyPlot.Axes.DateTimeAxis.ToDateTime((double)value);
#pragma warning restore CS0618
        }

        /// <summary>
        /// Converts a DateTime to an OxyPlot double date value.
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || value.GetType() != typeof(DateTime)) return value;

            var dt = (DateTime)value;
            if (dt.Equals(DateTime.MinValue)) return double.NaN;
            return OxyPlot.Axes.DateTimeAxis.ToDouble((DateTime)value);
        }
    }
}
