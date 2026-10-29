using System;
using System.Windows;
using System.Windows.Controls;

namespace DatabaseControls
{
    /// <summary>
    /// A dialog window for finding text within a specific column of the TableViewer.
    /// Provides options for case-sensitive and whole-word matching, as well as
    /// navigation to find the next or previous occurrence.
    /// </summary>
    public partial class FindAndReplace : GenericControls.MetroWindow
    {
        #region Private Fields

        /// <summary>
        /// Reference to the parent TableViewer control that contains the data to search.
        /// This may be null when the control is instantiated by the WPF designer.
        /// </summary>
        private readonly TableViewer? _tableViewer;

        /// <summary>
        /// The current row index position in the search.
        /// </summary>
        private int _currentRow;

        /// <summary>
        /// The column index to search within.
        /// </summary>
        private readonly int _columnIndex;

        /// <summary>
        /// Indicates whether a match has been found during the current search operation.
        /// </summary>
        private bool _foundInstance = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FindAndReplace"/> class.
        /// This parameterless constructor is required by the WPF designer.
        /// </summary>
        public FindAndReplace()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FindAndReplace"/> class with the specified table viewer and search parameters.
        /// </summary>
        /// <param name="tableViewer">The TableViewer control containing the data to search.</param>
        /// <param name="columnIndex">The zero-based index of the column to search within.</param>
        /// <param name="startRow">The zero-based row index to start the search from.</param>
        public FindAndReplace(TableViewer tableViewer, int columnIndex, int startRow)
        {
            InitializeComponent();

            _tableViewer = tableViewer;
            _columnIndex = columnIndex;
            Title = "Find In: " + tableViewer.DataView.ColumnNames[columnIndex];
            _currentRow = startRow;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the FindNextButton control.
        /// Searches forward through the column data for the next occurrence of the search text.
        /// Wraps around to the beginning of the column if the end is reached without finding a match.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_tableViewer == null || string.IsNullOrEmpty(FindText.Text))
                return;
            if (_tableViewer.DataView.NumberOfRows == 0) return;

            _foundInstance = false;
            _currentRow += 1;
            if (_currentRow == _tableViewer.DataView.NumberOfRows)
                _currentRow = 0;

            int searchIndex = _tableViewer.DataView.SearchColumn(
                _currentRow,
                _tableViewer.DataView.NumberOfRows - 1,
                _columnIndex,
                FindText.Text,
                MatchCaseCheckbox.IsChecked == true,
                MatchWordCheckbox.IsChecked == true);

            if (searchIndex >= 0)
            {
                _foundInstance = true;
                _tableViewer.SetActiveCell(searchIndex, _columnIndex, true);
                _currentRow = searchIndex;
            }
            else
            {
                _currentRow = 0;
                searchIndex = _tableViewer.DataView.SearchColumn(
                    _currentRow,
                    _tableViewer.DataView.NumberOfRows - 1,
                    _columnIndex,
                    FindText.Text,
                    MatchCaseCheckbox.IsChecked == true,
                    MatchWordCheckbox.IsChecked == true);

                if (searchIndex >= 0)
                {
                    _foundInstance = true;
                    _tableViewer.SetActiveCell(searchIndex, _columnIndex, true);
                    _currentRow = searchIndex;
                }
            }

            if (!_foundInstance)
            {
                GenericControls.MessageBox.Show("Search for: '" + FindText.Text + "' was not found.", "Find", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles the Click event of the FindPreviousButton control.
        /// Searches backward through the column data for the previous occurrence of the search text.
        /// Wraps around to the end of the column if the beginning is reached without finding a match.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_tableViewer == null || string.IsNullOrEmpty(FindText.Text))
                return;
            if (_tableViewer.DataView.NumberOfRows == 0) return;

            _foundInstance = false;
            _currentRow -= 1;
            if (_currentRow == -1)
                _currentRow = _tableViewer.DataView.NumberOfRows - 1;

            int searchIndex = _tableViewer.DataView.SearchColumn(
                _currentRow,
                0,
                _columnIndex,
                FindText.Text,
                MatchCaseCheckbox.IsChecked == true,
                MatchWordCheckbox.IsChecked == true);

            if (searchIndex >= 0)
            {
                _foundInstance = true;
                _tableViewer.SetActiveCell(searchIndex, _columnIndex, true);
                _currentRow = searchIndex;
            }
            else
            {
                _currentRow = _tableViewer.DataView.NumberOfRows - 1;
                searchIndex = _tableViewer.DataView.SearchColumn(
                    _currentRow,
                    0,
                    _columnIndex,
                    FindText.Text,
                    MatchCaseCheckbox.IsChecked == true,
                    MatchWordCheckbox.IsChecked == true);

                if (searchIndex >= 0)
                {
                    _foundInstance = true;
                    _tableViewer.SetActiveCell(searchIndex, _columnIndex, true);
                    _currentRow = searchIndex;
                }
            }

            if (!_foundInstance)
            {
                GenericControls.MessageBox.Show("Search for: '" + FindText.Text + "' was not found.", "Find", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the FindText control.
        /// Resets the found instance flag when the search text is modified.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="TextChangedEventArgs"/> instance containing the event data.</param>
        private void FindText_TextChanged(object sender, TextChangedEventArgs e)
        {
            _foundInstance = false;
        }

        #endregion
    }
}
