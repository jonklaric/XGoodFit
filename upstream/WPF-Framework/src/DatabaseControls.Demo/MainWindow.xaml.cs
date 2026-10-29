using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using DatabaseManager;
using Microsoft.Win32;
using GenericControls;
using Themes;

namespace DatabaseControls.Demo
{
    /// <summary>
    /// Main window for the DatabaseControls demo application.
    /// Provides functionality to open, view, and export database files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This window demonstrates the usage of the <see cref="DatabaseControls.TableViewer"/> control
    /// for viewing and editing database tables from various file formats including:
    /// </para>
    /// <list type="bullet">
    /// <item><description>DBF (dBASE) files</description></item>
    /// <item><description>MDB (Microsoft Access) files</description></item>
    /// <item><description>SQLite database files</description></item>
    /// <item><description>CSV (Comma-Separated Values) files</description></item>
    /// </list>
    /// <para>
    /// The application supports runtime theme switching between Light, Blue, and Dark themes.
    /// </para>
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public partial class MainWindow : MetroWindow
    {
        #region Fields

        /// <summary>
        /// The database manager instance used to read and manage database connections.
        /// </summary>
        private DatabaseManager.DatabaseManager? _databaseManager;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Theme Handling

        /// <summary>
        /// Handles theme radio button selection changes.
        /// </summary>
        /// <param name="sender">The radio button that was checked.</param>
        /// <param name="e">Event arguments.</param>
        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                Theme theme = Theme.Light;

                if (radioButton == LightThemeRadio)
                    theme = Theme.Light;
                else if (radioButton == BlueThemeRadio)
                    theme = Theme.Blue;
                else if (radioButton == DarkThemeRadio)
                    theme = Theme.Dark;

                ThemeService.Instance.SetTheme(theme);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the Select Database button.
        /// Opens a file dialog allowing the user to select a database file to open.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// Supported file formats:
        /// <list type="bullet">
        /// <item><description>.dbf - dBASE database files (single table)</description></item>
        /// <item><description>.mdb - Microsoft Access database files (multiple tables)</description></item>
        /// <item><description>.sqlite - SQLite database files (multiple tables)</description></item>
        /// <item><description>.csv - Comma-separated values files (single table)</description></item>
        /// </list>
        /// For multi-table formats (MDB, SQLite), a table selection combo box is enabled.
        /// For single-table formats (DBF, CSV), the table is loaded directly.
        /// </remarks>
        private void SelectDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            string inputFile = ShowFileOpenDialog("Database file (*.dbf, *.mdb, *.sqlite, *.csv)|*.dbf;*.mdb;*.sqlite;*.csv");

            if (string.IsNullOrEmpty(inputFile))
            {
                return;
            }

            string extension = Path.GetExtension(inputFile).ToLower();

            switch (extension)
            {
                case ".mdb":
                    GenericControls.MessageBox.Show("MDB file support is not available in this .NET version.");
                    break;

                case ".dbf":
                    LoadDbfFile(inputFile);
                    break;

                case ".sqlite":
                    LoadSqliteFile(inputFile);
                    break;

                case ".csv":
                    LoadCsvFile(inputFile);
                    break;
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the table combo box.
        /// Loads the selected table into the TableViewer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void TableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (TableComboBox.SelectedIndex == -1)
            {
                return;
            }

            if (_databaseManager == null)
            {
                return;
            }

            string selectedTable = (string)TableComboBox.SelectedValue;
            DataTableView tableView = _databaseManager.GetTableManager(selectedTable);
            TestViewer.DataView = tableView;

            UpdateStatusBar();
        }

        /// <summary>
        /// Handles the Click event of the Export menu item.
        /// Opens a save file dialog to export the current table data to various formats.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// Supported export formats:
        /// <list type="bullet">
        /// <item><description>.csv - Comma-separated values</description></item>
        /// <item><description>.dbf - dBASE database file</description></item>
        /// <item><description>.xlsx - Microsoft Excel workbook</description></item>
        /// <item><description>.sqlite - SQLite database file</description></item>
        /// </list>
        /// </remarks>
        private void ExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (TestViewer.DataView == null)
            {
                return;
            }

            string filters = "Comma delimited (*.csv)|*.csv|Database (*.dbf)|*.dbf|Excel (*.xlsx)|*.xlsx|SQLite (*.sqlite)|*.sqlite";
            var saveFileDialog = new SaveFileDialog { Filter = filters };

            if (saveFileDialog.ShowDialog() == true)
            {
                string outputPath = saveFileDialog.FileName;
                string extension = Path.GetExtension(outputPath).ToLower();

                try
                {
                    switch (extension)
                    {
                        case ".csv":
                            TestViewer.DataView.ExportToCsv(outputPath);
                            break;

                        case ".dbf":
                            TestViewer.DataView.ExportToDbf(outputPath);
                            break;

                        case ".xls":
                        case ".xlsx":
                            TestViewer.DataView.ExportToXlsx(outputPath);
                            break;

                        case ".sqlite":
                            TestViewer.DataView.ExportToSqlite(outputPath, TestViewer.DataView.TableName);
                            break;
                    }
                    RowCountText.Text = $"Exported to: {outputPath}";
                }
                catch (Exception ex)
                {
                    // File I/O on export can fail for permission / disk-full / file-locked
                    // reasons. Surface the error instead of crashing the demo.
                    GenericControls.MessageBox.Show(
                        $"Export failed: {ex.Message}",
                        "Export Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the status bar with the current row count.
        /// </summary>
        private void UpdateStatusBar()
        {
            if (TestViewer.DataView != null)
            {
                int rowCount = TestViewer.DataView.NumberOfRows;
                int colCount = TestViewer.DataView.ColumnNames.Length;
                RowCountText.Text = $"Rows: {rowCount:N0} | Columns: {colCount:N0}";
                StatusText.Text = $"Loaded: {TestViewer.DataView.TableName}";
            }
            else
            {
                RowCountText.Text = "Ready";
                StatusText.Text = "Select a database file to begin";
            }
        }

        /// <summary>
        /// Loads a dBASE database file (.dbf) directly into the viewer.
        /// </summary>
        /// <param name="filePath">The path to the DBF file.</param>
        private void LoadDbfFile(string filePath)
        {
            _databaseManager?.Close();
            _databaseManager = new DbfReader(filePath);

            TableComboBox.Items.Clear();
            TableComboBox.Items.Add(Path.GetFileNameWithoutExtension(filePath));
            TableComboBox.IsEnabled = false;
            TableComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Loads a SQLite database file and populates the table combo box.
        /// </summary>
        /// <param name="filePath">The path to the SQLite file.</param>
        private void LoadSqliteFile(string filePath)
        {
            _databaseManager?.Close();
            var tableReader = new DatabaseManager.SQLiteManager(filePath);

            TableComboBox.IsEnabled = true;
            TableComboBox.Items.Clear();

            string[] tableNames = tableReader.GetTableNames();
            foreach (string tableName in tableNames)
            {
                TableComboBox.Items.Add(tableName);
            }

            _databaseManager = tableReader;

            if (tableNames.Length > 0)
            {
                StatusText.Text = $"Loaded: {Path.GetFileName(filePath)} ({tableNames.Length} tables)";
                // Auto-select the first table so TestViewer.DataView is populated and the
                // viewer isn't left blank with no feedback (matches LoadCsvFile / LoadDbfFile).
                TableComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Loads a CSV file directly into the viewer.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        private void LoadCsvFile(string filePath)
        {
            _databaseManager?.Close();
            _databaseManager = new CsvReader(filePath, true, 1, false);

            TableComboBox.Items.Clear();
            TableComboBox.Items.Add(Path.GetFileNameWithoutExtension(filePath));
            TableComboBox.IsEnabled = false;
            TableComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Displays an open file dialog with the specified filter.
        /// </summary>
        /// <param name="filters">The file filter string for the dialog.</param>
        /// <returns>The selected file path, or an empty string if cancelled.</returns>
        private static string ShowFileOpenDialog(string filters)
        {
            var openFileDialog = new OpenFileDialog { Filter = filters };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return string.Empty;
        }

        #endregion

        /// <inheritdoc/>
        protected override void OnClosed(EventArgs e)
        {
            _databaseManager?.Close();
            base.OnClosed(e);
        }
    }
}
