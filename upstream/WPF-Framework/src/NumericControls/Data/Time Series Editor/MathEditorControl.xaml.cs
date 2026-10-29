using GenericControls;
using Numerics.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NumericControls
{
    /// <summary>
    /// A user control for applying mathematical operations to time series data.
    /// Provides a UI for selecting mathematical functions and applying them to selected cells or entire time series.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class MathEditorControl : UserControl
    {
        private CopyPasteDataGrid _source = null;
        private TimeSeries _series = null;
        private DataGridCellInfo[] _selectedCells = null;
        private List<int> _selectedValueRowIndices = new List<int>();
        private bool _selectionConsecutive = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MathEditorControl"/> class.
        /// </summary>
        public MathEditorControl()
        {
            InitializeComponent();

            List<MathFunctionType> operands = new List<MathFunctionType>();
            List<MathFunctionType> nonOperands = new List<MathFunctionType>();
            foreach (MathFunctionType fnc in (MathFunctionType[])Enum.GetValues(typeof(MathFunctionType)))
            {
                if (fnc == MathFunctionType.Add || fnc == MathFunctionType.Subtract ||
                    fnc == MathFunctionType.Multiply || fnc == MathFunctionType.Divide ||
                    fnc == MathFunctionType.Logarithm || fnc == MathFunctionType.Exponentiate)
                { operands.Add(fnc); }
                else
                { nonOperands.Add(fnc); }
            }

            OperandItemsControl.ItemsSource = operands;
            NoOperandItemsControl.ItemsSource = nonOperands;
        }

        /// <summary>
        /// Gets or sets the time series data that mathematical operations will be applied to.
        /// </summary>
        /// <value>The time series to operate on.</value>
        public TimeSeries Series { get => _series; set => _series = value; }

        /// <summary>
        /// Gets or sets the source data grid containing the selected cells.
        /// </summary>
        /// <value>The data grid that provides cell selection context for operations.</value>
        public CopyPasteDataGrid Source
        {
            get => _source;
            set
            {
                _source = value;
                _selectedValueRowIndices.Clear();
                _selectedCells = _source.SelectedCells.ToArray();
                foreach (DataGridCellInfo cellInfo in _selectedCells)
                {
                    if (cellInfo.Column.DisplayIndex <= 1) { continue; }
                    _selectedValueRowIndices.Add(_series.IndexOf(cellInfo.Item));
                }
                _selectionConsecutive = false;
                if (_selectedValueRowIndices.Count > 0 && _selectedValueRowIndices.Count < _series.Count)
                {
                    _selectedValueRowIndices.Sort();
                    _selectionConsecutive = true;
                    for (int i = 0; i < _selectedValueRowIndices.Count; i++)
                    {
                        if (_selectedValueRowIndices[i] - i != _selectedValueRowIndices[0]) { _selectionConsecutive = false; break; }
                    }

                    if (_selectionConsecutive)
                    {
                        NotificationText.Text = $"Applies to rows {_selectedValueRowIndices[0] + 1} - {_selectedValueRowIndices[_selectedValueRowIndices.Count - 1] + 1}.";// +
                                                                                                                                                                           //$"{Environment.NewLine}" +
                                                                                                                                                                           //$"Selection is {(_selectionConsecutive == true ? "continuous" : "discontinuous")}." +
                                                                                                                                                                           //$"{Environment.NewLine}" +
                                                                                                                                                                           //$"(Rows {_selectedValueRowIndices[0] + 1} - {_selectedValueRowIndices[_selectedValueRowIndices.Count - 1] + 1})";
                    }
                    else
                    {
                        string rowsString = string.Empty;
                        if (_selectedValueRowIndices.Count > 8)
                        {
                            rowsString = $"{_selectedValueRowIndices[0] + 1}, {_selectedValueRowIndices[1] + 1},...,{_selectedValueRowIndices[_selectedValueRowIndices.Count - 1] + 1}";
                        }
                        else
                        {
                            rowsString = string.Join(", ", _selectedValueRowIndices.Select(item => item + 1));
                        }
                        NotificationText.Text = $"Applies to rows {rowsString}.";
                    }
                }
                else
                {
                    NotificationText.Text = "Applies to all.";
                }
            }
        }

        /// <summary>
        /// Handles the click event for a math function button and applies the selected mathematical operation.
        /// </summary>
        /// <param name="sender">The button that was clicked.</param>
        /// <param name="e">The routed event arguments.</param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) { return; }
            var cntxt = btn.DataContext;
            if (cntxt == null || cntxt.GetType() != typeof(MathFunctionType)) { return; }
            //
            if (_source == null || _series == null) { return; }
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                MathFunctionType functionType = (MathFunctionType)cntxt;

                if (ValueTextBox.ValueIsValid == false)
                {
                    //update text notification
                    NotificationText.Text = "*Operand is not valid for this operation.";
                    return;
                }

                double value = ValueTextBox.Value;

                ApplyFunctionToSeries(_series, functionType, value, _selectedValueRowIndices);

                // Reselect cells
                foreach (DataGridCellInfo cellInfo in _selectedCells)
                {
                    _source.SelectedCells.Add(cellInfo);
                }
            }
            catch (Exception ex)
            {
                GenericControls.MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }

        /// <summary>
        /// Applies a mathematical function to a time series.
        /// </summary>
        /// <param name="series">The time series to modify.</param>
        /// <param name="functionType">The type of mathematical function to apply.</param>
        /// <param name="value">The operand value for functions that require one (e.g., add, multiply).</param>
        /// <param name="indices">The indices of specific rows to apply the function to, or null/empty to apply to all rows.</param>
        public static void ApplyFunctionToSeries(TimeSeries series, MathFunctionType functionType, double value, List<int> indices)
        {
            if (series == null) { return; }

            try
            {
                if (indices == null || indices.Count == 0 || indices.Count == series.Count)
                {
                    if (functionType == MathFunctionType.Add)
                    {
                        series.Add(value);
                    }
                    else if (functionType == MathFunctionType.Subtract)
                    {
                        series.Subtract(value);
                    }
                    else if (functionType == MathFunctionType.Multiply)
                    {
                        series.Multiply(value);
                    }
                    else if (functionType == MathFunctionType.Divide)
                    {
                        series.Divide(value);
                    }
                    else if (functionType == MathFunctionType.Exponentiate)
                    {
                        series.Exponentiate(value);
                    }
                    else if (functionType == MathFunctionType.Logarithm)
                    {
                        series.LogTransform(value);
                    }
                    else if (functionType == MathFunctionType.Inverse)
                    {
                        series.Inverse();
                    }
                    else if (functionType == MathFunctionType.Replace)
                    {
                        series.ReplaceMissingData(value);
                    }
                    else if (functionType == MathFunctionType.Interpolate)
                    {
                        series.InterpolateMissingData(series.Count);
                    }
                }
                else
                {
                    if (functionType == MathFunctionType.Add)
                    {
                        series.Add(value, indices);
                    }
                    else if (functionType == MathFunctionType.Subtract)
                    {
                        series.Subtract(value, indices);
                    }
                    else if (functionType == MathFunctionType.Multiply)
                    {
                        series.Multiply(value, indices);
                    }
                    else if (functionType == MathFunctionType.Divide)
                    {
                        series.Divide(value, indices);
                    }
                    else if (functionType == MathFunctionType.Exponentiate)
                    {
                        series.Exponentiate(value, indices);
                    }
                    else if (functionType == MathFunctionType.Logarithm)
                    {
                        series.LogTransform(indices, value);
                    }
                    else if (functionType == MathFunctionType.Inverse)
                    {
                        series.Inverse(indices);
                    }
                    else if (functionType == MathFunctionType.Replace)
                    {
                        series.ReplaceMissingData(indices, value);
                    }
                    else if (functionType == MathFunctionType.Interpolate)
                    {
                        series.InterpolateMissingData(indices.Count, indices);
                    }
                }
            }
            catch (Exception ex)
            {
                GenericControls.MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }
    }

    /// <summary>
    /// Converts a <see cref="MathFunctionType"/> enumeration value to its display name for use in UI bindings.
    /// </summary>
    public class MathFunctionTypeToNameConverter : IValueConverter
    {
        /// <summary>
        /// Gets the display name for a mathematical function type.
        /// </summary>
        /// <param name="function">The mathematical function type.</param>
        /// <returns>A human-readable display name for the function.</returns>
        public static string GetName(MathFunctionType function)
        {
            switch (function)
            {
                case MathFunctionType.Logarithm: return "Logarithmic Transform";
                default: return function.ToString();
            }
        }

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value.GetType() != typeof(MathFunctionType)) { return null; }

            return GetName((MathFunctionType)value);
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a <see cref="MathFunctionType"/> enumeration value to its tooltip description for use in UI bindings.
    /// </summary>
    public class MathFunctionTypeToTooltipConverter : IValueConverter
    {
        /// <summary>
        /// Gets the tooltip description for a mathematical function type.
        /// </summary>
        /// <param name="function">The mathematical function type.</param>
        /// <returns>A descriptive tooltip explaining what the function does.</returns>
        public static string GetTooltip(MathFunctionType function)
        {
            switch (function)
            {
                case MathFunctionType.Add: return "Add a constant to values. Missing values are kept as missing.";
                case MathFunctionType.Subtract: return "Subtract a constant from values. Missing values are kept as missing.";
                case MathFunctionType.Multiply: return "Multiply values by a constant. Missing values are kept as missing.";
                case MathFunctionType.Divide: return "Divide values by a constant. Missing values are kept as missing.";
                case MathFunctionType.Exponentiate: return "Raise values to a constant power. Missing values are kept as missing.";
                case MathFunctionType.Logarithm: return "Log transform values in a specified base. Missing values are kept as missing.";
                case MathFunctionType.Inverse: return "Replace values by its inverse (1/x). Missing values are kept as missing. Zero values are set to missing.";
                case MathFunctionType.Replace: return "Replace missing data (Double.NaN) with a constant.";
                case MathFunctionType.Interpolate: return "Interpolate missing data.";
                default: return function.ToString();
            }
        }

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value.GetType() != typeof(MathFunctionType)) { return null; }

            return GetTooltip((MathFunctionType)value);
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a <see cref="MathFunctionType"/> enumeration value to its icon brush for use in UI bindings.
    /// </summary>
    public class MathFunctionTypeToIconConverter : IValueConverter
    {
        /// <summary>
        /// Gets the icon resource key for a mathematical function type.
        /// </summary>
        /// <param name="function">The mathematical function type.</param>
        /// <returns>The resource key for the function's icon.</returns>
        public static string GetIconResourceKey(MathFunctionType function)
        {
            switch (function)
            {
                case MathFunctionType.Add: return "PlusIcon";
                case MathFunctionType.Subtract: return "MinusIcon";
                case MathFunctionType.Multiply: return "MultiplyIcon";
                case MathFunctionType.Divide: return "DivideIcon";
                case MathFunctionType.Exponentiate: return "ExpIcon";
                case MathFunctionType.Logarithm: return "LogIcon";
                case MathFunctionType.Inverse: return "InvertIcon";
                case MathFunctionType.Replace: return "ReplaceIcon";
                case MathFunctionType.Interpolate: return "InterpolateIcon";
                default: return "MathFunctionIcon";
            }
        }

        /// <summary>
        /// Gets the icon element for a mathematical function type from application resources.
        /// </summary>
        /// <param name="function">The mathematical function type.</param>
        /// <returns>A <see cref="FrameworkElement"/> representing the function's icon.</returns>
        public static FrameworkElement GetIcon(MathFunctionType function)
        {
            string resourceKey = GetIconResourceKey(function);
            return Application.Current.TryFindResource(resourceKey) as FrameworkElement;
        }

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value.GetType() != typeof(MathFunctionType)) { return null; }

            return GetIcon((MathFunctionType)value);
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
