using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using DatabaseManager;

namespace DatabaseControls
{
    /// <summary>
    /// A window that displays summary statistics for a selected column in the TableViewer.
    /// Automatically determines whether to show numeric or alphabetic statistics based on the column data type.
    /// Supports filtering statistics to selected rows only.
    /// </summary>
    public partial class ColumnStatsWindow : GenericControls.MetroWindow
    {
        #region Private Fields

        /// <summary>
        /// Reference to the parent TableViewer control containing the data.
        /// May be null if constructor was called with null parameter (backwards compatibility).
        /// </summary>
        private readonly TableViewer? _viewer;

        /// <summary>
        /// The zero-based index of the column to display statistics for.
        /// </summary>
        private readonly int _columnIndex;

        /// <summary>
        /// Indicates whether the column contains numeric data.
        /// </summary>
        private bool _isNumeric;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnStatsWindow"/> class with the specified table viewer and column index.
        /// </summary>
        /// <param name="theViewer">The TableViewer control containing the data to analyze. If null, the window will close gracefully.</param>
        /// <param name="columnIndex">The zero-based index of the column to display statistics for.</param>
        public ColumnStatsWindow(TableViewer theViewer, int columnIndex)
        {
            InitializeComponent();

            // Allow null for backwards compatibility with VB version - queue a Close so the
            // caller doesn't see an empty stats window. Previously the constructor returned
            // silently and the caller's Show() displayed a blank window with no feedback.
            if (theViewer == null)
            {
                Dispatcher.BeginInvoke((Action)Close);
                return;
            }

            _viewer = theViewer;
            _columnIndex = columnIndex;

            string fieldName = _viewer.DataView.ColumnNames[_columnIndex];
            Title = fieldName + " Summary Statistics";

            _isNumeric = DatabaseManager.DatabaseManager.IsNumericType(_viewer.DataView.ColumnTypes[_columnIndex]);

            if (_isNumeric)
            {
                NumericColumnViewer.Visibility = Visibility.Visible;
                NumericColumnViewer.DataName = fieldName;
                AlphabeticColumnViewer.Visibility = Visibility.Collapsed;
            }
            else
            {
                NumericColumnViewer.Visibility = Visibility.Collapsed;
                AlphabeticColumnViewer.Visibility = Visibility.Visible;
                AlphabeticColumnViewer.DataName = fieldName;
            }

            _viewer.SelectedRowIndicesChanged += ViewerSelectionChanged;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles changes to the selected row indices in the parent TableViewer.
        /// Enables or disables the "Selected Rows Only" checkbox based on whether rows are selected.
        /// </summary>
        /// <param name="selectedRowIndices">The list of currently selected row indices.</param>
        private void ViewerSelectionChanged(List<int> selectedRowIndices)
        {
            if (selectedRowIndices == null || selectedRowIndices.Count == 0)
            {
                SelectedOnlyCheckbox.IsEnabled = false;
                SelectedOnlyCheckbox.IsChecked = false;
            }
            else
            {
                SelectedOnlyCheckbox.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the ContentRendered event of the ColumnStatsWindow.
        /// Loads and displays the initial column statistics after the window has been rendered.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ColumnStatsWindow_ContentRendered(object sender, EventArgs e)
        {
            // Safety check for null viewer (backwards compatibility with VB version)
            if (_viewer == null)
            {
                return;
            }

            object[] fieldData = _viewer.DataView.GetColumn(_viewer.DataView.ColumnNames[_columnIndex]);

            if (_isNumeric)
            {
                NumericColumnViewer.Data = Array.ConvertAll(fieldData, o => Convert.IsDBNull(o) ? double.NaN : Convert.ToDouble(o));
            }
            else
            {
                AlphabeticColumnViewer.Data = fieldData;
            }

            // Auto-check "Selected Rows Only" if rows are selected; the Checked handler will reload with filtered data
            if (_viewer.GetSelectedRows.Count > 0)
            {
                SelectedOnlyCheckbox.IsEnabled = true;
                SelectedOnlyCheckbox.IsChecked = true;
            }
        }

        /// <summary>
        /// Handles the Closing event of the ColumnStatsWindow.
        /// Unsubscribes from the TableViewer's SelectedRowIndicesChanged event to prevent memory leaks.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
        private void ColumnStatsWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_viewer != null)
                _viewer.SelectedRowIndicesChanged -= ViewerSelectionChanged;
        }

        /// <summary>
        /// Handles the Checked event of the SelectedOnlyCheckbox control.
        /// Filters the statistics to show only data from the selected rows.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void SelectedOnlyCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewer == null) return;

            object[] fieldData;

            object[] tempData = _viewer.DataView.GetColumn(_viewer.DataView.ColumnNames[_columnIndex]);
            List<int> selectedRows = _viewer.GetSelectedRows;

            if (selectedRows.Count > 0)
            {
                List<object> realData = new List<object>();
                for (int i = 0; i < selectedRows.Count; i++)
                {
                    realData.Add(tempData[selectedRows[i]]);
                }
                fieldData = realData.ToArray();
            }
            else
            {
                fieldData = tempData;
            }

            if (_isNumeric)
            {
                NumericColumnViewer.Data = Array.ConvertAll(fieldData, o => Convert.IsDBNull(o) ? double.NaN : Convert.ToDouble(o));
            }
            else
            {
                AlphabeticColumnViewer.Data = fieldData;
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the SelectedOnlyCheckbox control.
        /// Restores the statistics to show data from all rows in the column.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void SelectedOnlyCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_viewer == null) return;

            object[] fieldData = _viewer.DataView.GetColumn(_viewer.DataView.ColumnNames[_columnIndex]);

            if (_isNumeric)
            {
                NumericColumnViewer.Data = Array.ConvertAll(fieldData, o => Convert.IsDBNull(o) ? double.NaN : Convert.ToDouble(o));
            }
            else
            {
                AlphabeticColumnViewer.Data = fieldData;
            }
        }

        #endregion
    }
}
