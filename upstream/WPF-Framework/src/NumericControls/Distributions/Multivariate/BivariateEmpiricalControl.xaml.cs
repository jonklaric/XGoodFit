using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Numerics.Distributions;
using OxyPlot.Wpf;

namespace NumericControls
{
    /// <summary>
    /// A user control for editing bivariate empirical cumulative distribution functions.
    /// Provides a data grid for editing X1, X2, and probability values, with real-time validation and plotting.
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
    public partial class BivariateEmpiricalControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BivariateEmpiricalControl"/> class.
        /// Sets up toolbar buttons, context menu items, and event handlers for data editing.
        /// </summary>
        public BivariateEmpiricalControl()
        {

            // Toolbar buttons
            _addColumnButton = new Button() { ToolTip = "Add Column(s) to the Right of the Table" };
            _insertColumnButton = new Button() { ToolTip = "Insert Column(s) into the Table", IsEnabled = false };
            _deleteColumnButton = new Button() { ToolTip = "Delete Column(s) from the Table", IsEnabled = false };
            // Context Menu Items
            _addColumnCMI = new MenuItem() { Header = "Add Column(s)" };
            _insertColumnCMI = new MenuItem() { Header = "Insert Column(s)" };
            _deleteColumnCMI = new MenuItem() { Header = "Delete Column(s)" };

            InitializeComponent();

            // Add any initialization after the InitializeComponent() call.
            X1HeaderBorder.Style = DefaultX1HeaderBorderStyle();
            X1HeaderTextBlock.Style = DefaultX1HeaderTextBlockStyle();
            // 
            X2HeaderBorder.Style = DefaultX2HeaderBorderStyle();
            X2HeaderTextBlock.Style = DefaultX2HeaderTextBlockStyle();
            //
            _dt.ColumnChanged += TableColumnChanged;
            _dt.TableNewRow += TableNewRowAdded;
             
            // Column buttons
            // Add
            _addColumnButton.Click += AddColumns_Click;
            _addColumnButton.Content = (Viewbox)TryFindResource("AddColumnIcon");
            // Insert
            _insertColumnButton.Click += InsertColumns_Click;
            _insertColumnButton.Content = (Viewbox)TryFindResource("InsertColumnIcon");
            // Delete
            _deleteColumnButton.Click += DeleteColumns_Click;
            _deleteColumnButton.Content = (Viewbox)TryFindResource("DeleteColumnIcon");
            
            // Column Context Items
            // Add
            _addColumnCMI.Icon = (Viewbox)TryFindResource("AddColumnIcon");
            _addColumnCMI.Click += AddColumns_Click;
            // Insert 
            _insertColumnCMI.Icon = (Viewbox)TryFindResource("InsertColumnIcon");
            _insertColumnCMI.Click += InsertColumns_Click;
            // Delete 
            _deleteColumnCMI.Icon = (Viewbox)TryFindResource("DeleteColumnIcon");
            _deleteColumnCMI.Click += DeleteColumns_Click;
        }

        /// <summary>
        /// Dependency property for the control distribution options.
        /// </summary>
        public static readonly DependencyProperty BivariateCDFProperty = DependencyProperty.Register(nameof(BivariateCDF), typeof(BivariateEmpirical), typeof(BivariateEmpiricalControl), new PropertyMetadata(null, BivariateCDFPropertyCallback));

        /// <summary>
        /// Property changed callback for the BivariateCDF dependency property.
        /// Updates the data grid with the bivariate distribution data.
        /// </summary>
        /// <param name="d">The dependency object on which the property changed.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void BivariateCDFPropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BivariateEmpiricalControl thisControl = (BivariateEmpiricalControl)d;
            thisControl._dt.Clear();
            thisControl._dt.Columns.Clear();
            if (e.NewValue != null)
            {
                BivariateEmpirical newBivariate = e.NewValue as BivariateEmpirical;
                // 
                if (newBivariate.X1Values == null) return;
                if (newBivariate.X2Values == null) return;
                for (int i = 0; i <= newBivariate.X2Values.Count(); i++)
                    thisControl._dt.Columns.Add(new DataColumn($"Column{i}", typeof(double)));

                // Top Row (X2 values)
                var topRow = new object[newBivariate.X2Values.Count() + 1];
                for (int i = 1; i <= newBivariate.X2Values.Count(); i++)
                    topRow[i] = newBivariate.X2Values[i - 1];
                // Buffer.BlockCopy(newBivariate.XValues, 0, topRow, 1, newBivariate.XValues.Count)
                thisControl._dt.Rows.Add(topRow);

                // Each Row(X1 values and probabilities)
                var nextRow = new object[(topRow.Count())];
                for (int i = 0; i < newBivariate.X1Values.Count(); i++)
                {
                    nextRow[0] = newBivariate.X1Values[i];
                    for (int j = 0; j < newBivariate.X2Values.Count(); j++)
                        nextRow[j + 1] = newBivariate.ProbabilityValues[i, j];
                    // 
                    thisControl._dt.Rows.Add(nextRow);
                }
            }
            // 
            thisControl.BivariateCDFDataGrid.ItemsSource = null;
            thisControl.BivariateCDFDataGrid.ItemsSource = thisControl._dt.DefaultView;
            thisControl.UpdatePlot();
        }

        /// <summary>
        /// Gets and sets the bivariate empirical CDF.
        /// </summary>
        public BivariateEmpirical BivariateCDF
        {
            get { return (BivariateEmpirical)GetValue(BivariateCDFProperty); }
            set { SetValue(BivariateCDFProperty, value); }
        }

        /// <summary>
        /// Creates the default style for the X2 header border.
        /// Uses theme-aware resources to match the DataGrid column header style.
        /// </summary>
        /// <returns>A style for the X2 header border.</returns>
        private static Style DefaultX2HeaderBorderStyle()
        {
            var s = new Style(typeof(Border));
            s.Setters.Add(new Setter { Property = Border.BackgroundProperty, Value = new DynamicResourceExtension("DataGrid.Row.Background") });
            s.Setters.Add(new Setter { Property = Border.BorderBrushProperty, Value = new DynamicResourceExtension("DataGrid.Header.Border") });
            s.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1d, 1d, 1d, 0d)));
            s.Setters.Add(new Setter(UIElement.IsHitTestVisibleProperty, false));
            return s;
        }

        /// <summary>
        /// Creates the default style for the X2 header text block.
        /// Uses theme-aware resources to match the DataGrid column header style.
        /// </summary>
        /// <returns>A style for the X2 header text block.</returns>
        private static Style DefaultX2HeaderTextBlockStyle()
        {
            var s = new Style(typeof(TextBlock));
            s.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            s.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
            s.Setters.Add(new Setter(TextBlock.BackgroundProperty, Brushes.Transparent));
            s.Setters.Add(new Setter { Property = TextBlock.ForegroundProperty, Value = new DynamicResourceExtension("DataGrid.Header.Foreground") });
            s.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(2d)));
            s.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            // s.Setters.Add(New Setter(TextBlock.TextWrappingProperty, TextWrapping.WrapWithOverflow))
            s.Setters.Add(new Setter(UIElement.IsHitTestVisibleProperty, false));
            return s;
        }

        /// <summary>
        /// Creates the default style for the X1 header border.
        /// Uses theme-aware resources to match the DataGrid row header style.
        /// </summary>
        /// <returns>A style for the X1 header border.</returns>
        private static Style DefaultX1HeaderBorderStyle()
        {
            var s = new Style(typeof(Border));
            s.Setters.Add(new Setter { Property = Border.BackgroundProperty, Value = new DynamicResourceExtension("DataGrid.Row.Background") });
            s.Setters.Add(new Setter { Property = Border.BorderBrushProperty, Value = new DynamicResourceExtension("DataGrid.Header.Border") });
            s.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1d, 1d, 0d, 1d)));
            s.Setters.Add(new Setter(UIElement.IsHitTestVisibleProperty, false));
            return s;
        }

        /// <summary>
        /// Creates the default style for the X1 header text block with vertical text rotation.
        /// Uses theme-aware resources to match the DataGrid row header style.
        /// </summary>
        /// <returns>A style for the X1 header text block.</returns>
        private static Style DefaultX1HeaderTextBlockStyle()
        {
            var s = new Style(typeof(TextBlock));
            s.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            s.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
            s.Setters.Add(new Setter(TextBlock.BackgroundProperty, Brushes.Transparent));
            s.Setters.Add(new Setter { Property = TextBlock.ForegroundProperty, Value = new DynamicResourceExtension("DataGrid.Header.Foreground") });
            s.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(2d)));
            s.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            // s.Setters.Add(New Setter(TextBlock.TextWrappingProperty, TextWrapping.WrapWithOverflow))
            s.Setters.Add(new Setter(UIElement.IsHitTestVisibleProperty, false));
            // Vertical text
            var verticalTextTransform = new RotateTransform(270d);
            s.Setters.Add(new Setter(FrameworkElement.LayoutTransformProperty, verticalTextTransform));
            return s;
        }

        /// <summary>
        /// Identifies the <see cref="X2HeaderBorderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty X2HeaderBorderStyleProperty = DependencyProperty.Register(nameof(X2HeaderBorderStyle), typeof(Style), typeof(BivariateEmpiricalControl), new UIPropertyMetadata(DefaultX2HeaderBorderStyle(), X2HeaderBorderStylePropertyCallback));

        /// <summary>
        /// Property changed callback for the X2HeaderBorderStyle dependency property.
        /// </summary>
        /// <param name="d">The dependency object on which the property changed.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void X2HeaderBorderStylePropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BivariateEmpiricalControl bvc = (BivariateEmpiricalControl)d;
            bvc.X2HeaderBorder.Style = e.NewValue as Style;
        }

        /// <summary>
        /// Gets or sets the style for the X2 header border.
        /// </summary>
        public Style X2HeaderBorderStyle
        {
            get { return (Style)GetValue(X2HeaderBorderStyleProperty); }
            set { SetValue(X2HeaderBorderStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="X2HeaderTextBlockStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty X2HeaderTextBlockStyleProperty = DependencyProperty.Register(nameof(X2HeaderTextBlockStyle), typeof(Style), typeof(BivariateEmpiricalControl), new UIPropertyMetadata(DefaultX2HeaderTextBlockStyle(), X2HeaderTextBlockStylePropertyCallback));

        /// <summary>
        /// Property changed callback for the X2HeaderTextBlockStyle dependency property.
        /// </summary>
        /// <param name="d">The dependency object on which the property changed.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void X2HeaderTextBlockStylePropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BivariateEmpiricalControl bvc = (BivariateEmpiricalControl)d;
            bvc.X2HeaderTextBlock.Style = e.NewValue as Style;
        }

        /// <summary>
        /// Gets or sets the style for the X2 header text block.
        /// </summary>
        public Style X2HeaderTextBlockStyle
        {
            get { return (Style)GetValue(X2HeaderTextBlockStyleProperty); }
            set { SetValue(X2HeaderTextBlockStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="X1HeaderBorderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty X1HeaderBorderStyleProperty = DependencyProperty.Register(nameof(X1HeaderBorderStyle), typeof(Style), typeof(BivariateEmpiricalControl), new UIPropertyMetadata(DefaultX1HeaderBorderStyle(), X1HeaderBorderStylePropertyCallback));

        /// <summary>
        /// Property changed callback for the X1HeaderBorderStyle dependency property.
        /// </summary>
        /// <param name="d">The dependency object on which the property changed.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void X1HeaderBorderStylePropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BivariateEmpiricalControl bvc = (BivariateEmpiricalControl)d;
            bvc.X1HeaderBorder.Style = e.NewValue as Style;
        }

        /// <summary>
        /// Gets or sets the style for the X1 header border.
        /// </summary>
        public Style X1HeaderBorderStyle
        {
            get { return (Style)GetValue(X1HeaderBorderStyleProperty); }
            set { SetValue(X1HeaderBorderStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="X1HeaderTextBlockStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty X1HeaderTextBlockStyleProperty = DependencyProperty.Register(nameof(X1HeaderTextBlockStyle), typeof(Style), typeof(BivariateEmpiricalControl), new UIPropertyMetadata(DefaultX1HeaderTextBlockStyle(), X1HeaderTextBlockStylePropertyCallback));

        /// <summary>
        /// Property changed callback for the X1HeaderTextBlockStyle dependency property.
        /// </summary>
        /// <param name="d">The dependency object on which the property changed.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void X1HeaderTextBlockStylePropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BivariateEmpiricalControl bvc = (BivariateEmpiricalControl)d;
            bvc.X1HeaderTextBlock.Style = e.NewValue as Style;
        }

        /// <summary>
        /// Gets or sets the style for the X1 header text block.
        /// </summary>
        public Style X1HeaderTextBlockStyle
        {
            get { return (Style)GetValue(X1HeaderTextBlockStyleProperty); }
            set { SetValue(X1HeaderTextBlockStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="X2Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty X2HeaderProperty = DependencyProperty.Register(nameof(X2Header), typeof(string), typeof(BivariateEmpiricalControl), new FrameworkPropertyMetadata("X2 Data"));

        /// <summary>
        /// Gets or sets the header text for the X2 data column.
        /// </summary>
        public string X2Header
        {
            get { return (string)GetValue(X2HeaderProperty); }
            set { SetValue(X2HeaderProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="X1Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty X1HeaderProperty = DependencyProperty.Register(nameof(X1Header), typeof(string), typeof(BivariateEmpiricalControl), new FrameworkPropertyMetadata("X1 Data"));

        /// <summary>
        /// Gets or sets the header text for the X1 data row.
        /// </summary>
        public string X1Header
        {
            get { return (string)GetValue(X1HeaderProperty); }
            set { SetValue(X1HeaderProperty, value); }
        }

        private bool _isLoaded = false;
        private DataTable _dt = new DataTable();
        private bool _pastingData = false;
        private bool _addingRows = false;
        private bool _addingColumns = false;
        private Button _addColumnButton;
        private Button _insertColumnButton;
        private Button _deleteColumnButton;
        private MenuItem _addColumnCMI;
        private MenuItem _insertColumnCMI;
        private MenuItem _deleteColumnCMI;

        /// <summary>
        /// Handles the Loaded event of the control.
        /// Initializes the data grid and adds custom toolbar buttons.
        /// </summary>
        /// <param name="sender">The control that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void BivariateEmpericalCDFControl_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDataGridView();
            if (_isLoaded == false)
            {
                GridToolbar.CustomButtons.Add(_addColumnButton);
                GridToolbar.CustomButtons.Add(_insertColumnButton);
                GridToolbar.CustomButtons.Add(_deleteColumnButton);
                BivariateCDFDataGrid.CustomMenuItems.Add(_addColumnCMI);
                BivariateCDFDataGrid.CustomMenuItems.Add(_insertColumnCMI);
                BivariateCDFDataGrid.CustomMenuItems.Add(_deleteColumnCMI);
            }
            _isLoaded = true;
        }

        /// <summary>
        /// Handles the TableNewRow event. Initializes new row cells to zero instead of DBNull.
        /// </summary>
        /// <param name="sender">The data table that raised the event.</param>
        /// <param name="e">Event arguments containing the new row.</param>
        private void TableNewRowAdded(object sender, DataTableNewRowEventArgs e)
        {
            // This is needed to set the new row data values to zero.
            for (int i = 0; i < e.Row.ItemArray.Count(); i++)
                if (e.Row[i] == DBNull.Value) e.Row[i] = 0;
        }

        /// <summary>
        /// Handles changes to data table column values.
        /// Validates the changed data and updates the bivariate CDF and plot.
        /// </summary>
        /// <param name="sender">The data table that raised the event.</param>
        /// <param name="e">Event arguments containing the changed column information.</param>
        private void TableColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            if (_pastingData == true) return;
            if (_addingRows == true) return;
            if (_addingColumns == true) return;
            int columnIndex = e.Column.Ordinal;
            int rowIndex = _dt.Rows.IndexOf(e.Row);
            ValidateData(rowIndex, columnIndex);
            UpdateBivariateCDF();
            UpdatePlot();
        }

        /// <summary>
        /// Handles the add columns button click event.
        /// Adds new columns to the right of the data grid.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void AddColumns_Click(object sender, RoutedEventArgs e)
        {
            if (BivariateCDFDataGrid == null) return;
            if (BivariateCDFDataGrid.CanUserAddInsertDeleteRows)
            {
                var uniqueColumns = BivariateCDFDataGrid.GetColumnsWithSelectedCells();
                AddColumns(Math.Max(uniqueColumns.Count, 1));
            }
        }

        /// <summary>
        /// Handles the insert columns button click event.
        /// Inserts new columns at the selected position in the data grid.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void InsertColumns_Click(object sender, RoutedEventArgs e)
        {
            if (BivariateCDFDataGrid == null) return;
            if (BivariateCDFDataGrid.CanUserAddInsertDeleteRows)
            {
                var uniqueColumns = BivariateCDFDataGrid.GetColumnsWithSelectedCells();
                int insertAtColumn = Math.Max(uniqueColumns.Min(), 1); // ensure that the first column (y values) doesn't get pushed.
                AddColumns(Math.Max(uniqueColumns.Count, 1), insertAtColumn);
            }
        }

        /// <summary>
        /// Handles the delete columns button click event.
        /// Removes the selected columns from the data grid.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void DeleteColumns_Click(object sender, RoutedEventArgs e)
        {
            if (BivariateCDFDataGrid == null) return;
            if (BivariateCDFDataGrid.CanUserAddInsertDeleteRows)
            {
                var uniqueColumns = BivariateCDFDataGrid.GetColumnsWithSelectedCells().ToList();
                uniqueColumns.Sort();
                for (int i = uniqueColumns.Count - 1; i >= 0; i -= 1)
                    _dt.Columns.RemoveAt(uniqueColumns[i]);
                // reset column names
                ResetColumnNames();
            }
            // Refresh DataGrid source
            BivariateCDFDataGrid.ItemsSource = null;
            BivariateCDFDataGrid.ItemsSource = _dt.DefaultView;
            BivariateCDFDataGrid.Focus();
            RefreshDataGridView();
            ValidateData();
            UpdateBivariateCDF();
            UpdatePlot();
        }

        /// <summary>
        /// Adds or inserts columns into the data table.
        /// </summary>
        /// <param name="nColumnsToAdd">The number of columns to add.</param>
        /// <param name="insertIndex">The index at which to insert columns, or -1 to append to the end.</param>
        private void AddColumns(int nColumnsToAdd, int insertIndex = -1)
        {
            _addingColumns = true;
            DataColumn newColumn;
            if (insertIndex == -1)
            {
                for (int i = 1; i < nColumnsToAdd; i++)
                {
                    if (_dt.Columns.Count >= 50) break;
                    newColumn = _dt.Columns.Add($"Column{_dt.Columns.Count}", typeof(double));
                    for (int j = 0; j < _dt.Rows.Count; j++)
                        _dt.Rows[j][newColumn] = 0;
                }
            }
            else
            {
                for (int i = 1; i <= nColumnsToAdd; i++)
                {
                    if (_dt.Columns.Count >= 50) break;
                    newColumn = _dt.Columns.Add($"Column{_dt.Columns.Count}", typeof(double));
                    newColumn.SetOrdinal(insertIndex);
                    for (int j = 0; j < _dt.Rows.Count; j++)
                        _dt.Rows[j][newColumn] = 0;
                }
                // Reset column names 
                ResetColumnNames();
            }
            // Refresh DataGrid source
            BivariateCDFDataGrid.ItemsSource = null;
            BivariateCDFDataGrid.ItemsSource = _dt.DefaultView;
            // 
            BivariateCDFDataGrid.Focus();
            RefreshDataGridView();
            ValidateData();
            _addingColumns = false;
            UpdateBivariateCDF();
            UpdatePlot();
        }

        /// <summary>
        /// Resets column names to sequential default values (Column0, Column1, etc.).
        /// </summary>
        private void ResetColumnNames()
        {
            // First set all column names to temporary names
            for (int i = 0; i < _dt.Columns.Count; i++)
                _dt.Columns[i].ColumnName = $"Temp{i}";
            // Reset column names
            for (int i = 0; i < _dt.Columns.Count; i++)
                _dt.Columns[i].ColumnName = $"Column{i}";
        }

        /// <summary>
        /// Handles the PreviewAddRows event of the data grid.
        /// Prevents adding rows to the header row and redirects to insert rows instead.
        /// </summary>
        /// <param name="startRowIndex">The index where rows will be added.</param>
        /// <param name="nRows">The number of rows to add.</param>
        /// <param name="cancelAddRows">Reference parameter to cancel the add operation.</param>
        private void BivariateCDFDataGrid_PreviewAddRows(int startRowIndex, int nRows, ref bool cancelAddRows)
        {
            _addingRows = true;
            if (startRowIndex > 0) return;
            // 
            _addingRows = false;
            cancelAddRows = true;
            if (startRowIndex < 0) return;
            // 
            if (_dt.Rows.Count == 1)
            {
                BivariateCDFDataGrid.AddRows(nRows);
            }
            else
            {
                BivariateCDFDataGrid.InsertRows(startRowIndex + 1, nRows);
            }
        }

        /// <summary>
        /// Handles the RowsAdded event of the data grid.
        /// Validates the newly added rows and updates the bivariate CDF.
        /// </summary>
        /// <param name="startRowIndex">The index of the first added row.</param>
        /// <param name="nRows">The number of rows added.</param>
        private void BivariateCDFDataGrid_RowsAdded(int startRowIndex, int nRows)
        {
            for (int i = startRowIndex; i < startRowIndex + nRows; i++)
                ValidateData(i, -1);
            _addingRows = false;
            if (_pastingData == false) UpdateBivariateCDF();
        }

        /// <summary>
        /// Handles the PreviewDeleteRows event of the data grid.
        /// Prevents deletion of the header row (row 0).
        /// </summary>
        /// <param name="rowindices">The list of row indices to delete.</param>
        /// <param name="cancel">Reference parameter to cancel the delete operation.</param>
        private void BivariateCDFDataGrid_PreviewDeleteRows(List<int> rowindices, ref bool cancel)
        {
            if (rowindices == null) return;
            // Ensure first row does not get deleted.
            int zeroIndex = rowindices.IndexOf(0);
            if (zeroIndex >= 0) rowindices.RemoveAt(zeroIndex);
        }

        /// <summary>
        /// Handles the RowsDeleted event of the data grid.
        /// Validates data and updates the bivariate CDF and plot after deletion.
        /// </summary>
        /// <param name="rowindices">The list of deleted row indices.</param>
        private void BivariateCDFDataGrid_RowsDeleted(List<int> rowindices)
        {
            ValidateData();
            UpdateBivariateCDF();
            UpdatePlot();
        }

        /// <summary>
        /// Handles the PreviewPasteData event of the data grid.
        /// Prevents pasting into cell (0,0) and expands columns if needed.
        /// </summary>
        /// <param name="clipboardData">The clipboard data to paste.</param>
        /// <param name="cancelPaste">Reference parameter to cancel the paste operation.</param>
        private void BivariateCDFDataGrid_PreviewPasteData(string[][] clipboardData, ref bool cancelPaste)
        {
            _pastingData = true;
            // Check to ensure cell(0,0) doesn't get pasted into
            int rowIndex, columnIndex;
            foreach (DataGridCellInfo c in BivariateCDFDataGrid.SelectedCells)
            {
                if (c.Column.DisplayIndex != 0) continue;
                rowIndex = BivariateCDFDataGrid.Items.IndexOf(c.Item);
                if (rowIndex != 0) continue;
                // 
                clipboardData[0][0] = "0";
                break;
            }
            if (BivariateCDFDataGrid.SelectedCells.Count != 1) return;

            // fill beyond selected cell
            var cellinfo = BivariateCDFDataGrid.SelectedCells[0];
            rowIndex = BivariateCDFDataGrid.Items.IndexOf(cellinfo.Item);
            columnIndex = cellinfo.Column.DisplayIndex;
            DataColumn newColumn;
            if (columnIndex + clipboardData[0].Count() > BivariateCDFDataGrid.Columns.Count - 1)
            {
                for (int i = 1; i <= clipboardData[0].Count() - (BivariateCDFDataGrid.Columns.Count - columnIndex); i++)
                {
                    newColumn = _dt.Columns.Add($"Column{_dt.Columns.Count}", typeof(double));
                    for (int j = 0; j < _dt.Rows.Count; j++)
                        _dt.Rows[j][newColumn] = 0;
                }
                BivariateCDFDataGrid.ItemsSource = null;
                BivariateCDFDataGrid.ItemsSource = _dt.DefaultView;
                BivariateCDFDataGrid.SelectedCells.Add(new DataGridCellInfo(BivariateCDFDataGrid.GetCell(rowIndex, columnIndex)));
            }
        }

        /// <summary>
        /// Handles the DataPasted event of the data grid.
        /// Validates pasted data and updates the bivariate CDF and plot.
        /// </summary>
        private void BivariateCDFDataGrid_DataPasted()
        {
            RefreshDataGridView();
            ValidateData();
            _pastingData = false;
            UpdateBivariateCDF();
            UpdatePlot();
        }

        /// <summary>
        /// Validates all data in the grid or a specific cell based on the provided indices.
        /// </summary>
        /// <param name="rowIndex">The row index to validate, or -1 to validate all rows.</param>
        /// <param name="columnIndex">The column index to validate, or -1 to validate all columns.</param>
        private void ValidateData(int rowIndex = -1, int columnIndex = -1)
        {
            if (_dt.Rows.Count == 0)
                return;
            if (_dt.Columns.Count == 0)
                return;
            if (rowIndex == 0 && columnIndex == 0)
                return;
            // 
            ValidateX1Values(rowIndex, columnIndex);
            ValidateX2Values(rowIndex, columnIndex);       
            ValidateProbabilities(rowIndex, columnIndex);
        }

        /// <summary>
        /// Validates X2 values (column headers) ensuring they are in ascending order.
        /// </summary>
        /// <param name="rowIndex">The row index to validate, or -1 to validate all rows.</param>
        /// <param name="columnIndex">The column index to validate, or -1 to validate all columns.</param>
        private void ValidateX2Values(int rowIndex = -1, int columnIndex = -1)
        {
            if (rowIndex > 0)
                return;
            // Get tooltip messages
            string header = string.IsNullOrEmpty(X2Header) == true ? "X2 values" : $"'{X2Header}'";
            string tooltipMessage = $"{header} must be in ascending order from left to right.";
            // 
            DataGridCell cell;
            double previousValue, currentValue;
            if (columnIndex == -1)
            {
                for (int i = 2; i < _dt.Columns.Count; i++)
                {
                    cell = BivariateCDFDataGrid.GetCell(0, i);
                    if (cell == null) continue;
                    // 
                    previousValue = _dt.Rows[0][i - 1] == DBNull.Value ? 0d : (double)_dt.Rows[0][i - 1];
                    currentValue = _dt.Rows[0][i] == DBNull.Value ? 0d : (double)_dt.Rows[0][i];
                    // 
                    SetCellValidation(cell, previousValue <= currentValue, tooltipMessage);
                }
            }
            else
            {
                if (columnIndex == 0) return;
                if (columnIndex >= _dt.Columns.Count) return;
                // 
                bool checkCurrent = true;
                bool checkNext = true;
                // 
                if (columnIndex == 1) checkCurrent = false;
                if (columnIndex + 1 >= _dt.Columns.Count) checkNext = false;
                // 
                if (checkCurrent == true)
                {
                    cell = BivariateCDFDataGrid.GetCell(0, columnIndex);
                    if (cell != null)
                    {
                        previousValue = _dt.Rows[0][columnIndex - 1] == DBNull.Value ? 0d : (double)_dt.Rows[0][columnIndex - 1];
                        currentValue = _dt.Rows[0][columnIndex] == DBNull.Value ? 0d : (double)_dt.Rows[0][columnIndex];
                        SetCellValidation(cell, previousValue <= currentValue, tooltipMessage);
                    }
                }
                // 
                if (checkNext == true)
                {
                    cell = BivariateCDFDataGrid.GetCell(0, columnIndex + 1);
                    if (cell != null)
                    {
                        previousValue = _dt.Rows[0][columnIndex] == DBNull.Value ? 0d : (double)_dt.Rows[0][columnIndex];
                        currentValue = _dt.Rows[0][columnIndex + 1] == DBNull.Value ? 0d : (double)_dt.Rows[0][columnIndex + 1];
                        SetCellValidation(cell, previousValue <= currentValue, tooltipMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Validates X1 values (row headers) ensuring they are in ascending order.
        /// </summary>
        /// <param name="rowIndex">The row index to validate, or -1 to validate all rows.</param>
        /// <param name="columnIndex">The column index to validate, or -1 to validate all columns.</param>
        private void ValidateX1Values(int rowIndex = -1, int columnIndex = -1)
        {
            if (columnIndex > 0) return;
            // Get tooltip messages
            string header = string.IsNullOrEmpty(X1Header) == true ? "X1 values" : $"'{X1Header}'";
            string tooltipMessage = $"{header} must be in ascending order from top to bottom.";
            // 
            DataGridCell cell;
            double previousValue, currentValue;
            if (rowIndex == -1)
            {
                for (int i = 2; i < _dt.Rows.Count; i++)
                {
                    cell = BivariateCDFDataGrid.GetCell(i, 0);
                    if (cell == null) continue;
                    previousValue = _dt.Rows[i - 1][0] == DBNull.Value ? 0d : (double)_dt.Rows[i - 1][0];
                    currentValue = _dt.Rows[i][0] == DBNull.Value ? 0d : (double)_dt.Rows[i][0];
                    SetCellValidation(cell, previousValue <= currentValue, tooltipMessage, true);
                }
            }
            else
            {
                if (rowIndex == 0) return;
                if (rowIndex >= _dt.Rows.Count) return;
                // 
                bool checkCurrent = true;
                bool checkNext = true;
                // 
                if (rowIndex == 1) checkCurrent = false;
                if (rowIndex + 1 >= _dt.Rows.Count) checkNext = false;
                // 
                if (checkCurrent == true)
                {
                    cell = BivariateCDFDataGrid.GetCell(rowIndex, 0);
                    if (cell != null)
                    {
                        previousValue = _dt.Rows[rowIndex - 1][0] == DBNull.Value ? 0d : (double)_dt.Rows[rowIndex - 1][0];
                        currentValue = _dt.Rows[rowIndex][0] == DBNull.Value ? 0d : (double)_dt.Rows[rowIndex][0];
                        SetCellValidation(cell, previousValue <= currentValue, tooltipMessage, true);
                    }
                }
                // 
                if (checkNext == true)
                {
                    cell = BivariateCDFDataGrid.GetCell(rowIndex + 1, 0);
                    if (cell != null)
                    {
                        previousValue = _dt.Rows[rowIndex][0] == DBNull.Value ? 0d : (double)_dt.Rows[rowIndex][0];
                        currentValue = _dt.Rows[rowIndex + 1][0] == DBNull.Value ? 0d : (double)_dt.Rows[rowIndex + 1][0];
                        SetCellValidation(cell, previousValue <= currentValue, tooltipMessage, true);
                    }
                }
            }
        }

        /// <summary>
        /// Validates probability values ensuring they are between 0 and 1 and increase appropriately.
        /// </summary>
        /// <param name="rowIndex">The row index to validate, or -1 to validate all rows.</param>
        /// <param name="columnIndex">The column index to validate, or -1 to validate all columns.</param>
        private void ValidateProbabilities(int rowIndex = -1, int columnIndex = -1)
        {
            if (columnIndex == 0 || rowIndex == 0)
                return;
            // Get tooltip messages
            string rangeTooltipMessage = "Probability values must be between 0 and 1.";
            string headerX1 = string.IsNullOrEmpty(X1Header) == true ? "X1 values" : $"'{X1Header}'";
            string ascendingX1TooltipMessage = $"Probability values must increase with increasing {headerX1}.";
            //
            string headerX2 = string.IsNullOrEmpty(X2Header) == true ? "X2 values" : $"'{X2Header}'";
            string ascendingX2TooltipMessage = $"Probability values must increase with increasing {headerX2}.";
            // Validate
            if (rowIndex == -1 && columnIndex == -1)
            {
                // validate all cells
                for (int i = 1; i < _dt.Columns.Count; i++)
                {
                    for (int j = 1; j < _dt.Rows.Count; j++)
                        ValidateProbability(j, i, rangeTooltipMessage, ascendingX1TooltipMessage, ascendingX2TooltipMessage);
                }
            }
            else if (rowIndex == -1 && columnIndex != -1)
            {
                if (columnIndex >= _dt.Rows.Count) return;
                // validate entire column
                for (int j = 1; j < _dt.Rows.Count; j++)
                {
                    ValidateProbability(j, columnIndex, rangeTooltipMessage, ascendingX1TooltipMessage, ascendingX2TooltipMessage);
                    ValidateProbability(j, columnIndex + 1, rangeTooltipMessage, ascendingX1TooltipMessage, ascendingX2TooltipMessage);
                }
            }
            else if (rowIndex != -1 && columnIndex == -1)
            {
                if (rowIndex >= _dt.Rows.Count) return;
                // validate entire row
                for (int i = 1; i < _dt.Columns.Count; i++)
                {
                    ValidateProbability(rowIndex, i, rangeTooltipMessage, ascendingX1TooltipMessage, ascendingX2TooltipMessage);
                    ValidateProbability(rowIndex + 1, i, rangeTooltipMessage, ascendingX1TooltipMessage, ascendingX2TooltipMessage);
                }
            }
            else
            {
                ValidateProbability(rowIndex, columnIndex, rangeTooltipMessage, ascendingX1TooltipMessage, ascendingX2TooltipMessage);
                ValidateProbability(rowIndex + 1, columnIndex, rangeTooltipMessage, ascendingX1TooltipMessage, ascendingX2TooltipMessage);
                ValidateProbability(rowIndex, columnIndex + 1, rangeTooltipMessage, ascendingX1TooltipMessage, ascendingX2TooltipMessage);
            }
        }

        /// <summary>
        /// Validates a single probability cell ensuring it meets range and ordering constraints.
        /// </summary>
        /// <param name="rowIndex">The row index of the cell to validate.</param>
        /// <param name="columnIndex">The column index of the cell to validate.</param>
        /// <param name="rangeMessage">The error message for range validation.</param>
        /// <param name="ascendingYMessage">The error message for Y-axis ordering validation.</param>
        /// <param name="ascendingXMessage">The error message for X-axis ordering validation.</param>
        private void ValidateProbability(int rowIndex, int columnIndex, string rangeMessage, string ascendingYMessage, string ascendingXMessage)
        {
            var cell = BivariateCDFDataGrid.GetCell(rowIndex, columnIndex);
            if (cell != null)
            {
                double currentValue, previousTopValue, previousLeftValue;
                // validate probability
                currentValue = _dt.Rows[rowIndex][columnIndex] == DBNull.Value ? -1 : (double)_dt.Rows[rowIndex][columnIndex];
                SetCellValidation(cell, currentValue <= 1d && currentValue >= 0d, rangeMessage);
                // 
                if (rowIndex > 1)
                {
                    previousTopValue = _dt.Rows[rowIndex - 1][columnIndex] == DBNull.Value ? 0d : (double)_dt.Rows[rowIndex - 1][columnIndex];
                    SetCellValidation(cell, previousTopValue <= currentValue, ascendingYMessage);
                }
                if (columnIndex > 1)
                {
                    previousLeftValue = _dt.Rows[rowIndex][columnIndex - 1] == DBNull.Value ? -1 : (double)_dt.Rows[rowIndex][columnIndex - 1];
                    SetCellValidation(cell, previousLeftValue <= currentValue, ascendingXMessage);
                }
            }
        }

        /// <summary>
        /// Sets the validation state of a data grid cell with visual feedback.
        /// </summary>
        /// <param name="cell">The cell to set validation for.</param>
        /// <param name="isValid">Whether the cell is valid.</param>
        /// <param name="tooltip">The tooltip message to display for invalid cells.</param>
        /// <param name="isY">Whether this is a Y-axis cell (requires different styling).</param>
        private void SetCellValidation(DataGridCell cell, bool isValid, string tooltip, bool isY = false)
        {
            // Setting the background colors and borders is a real pain with this control. The first column has to be handled differently to render reasonably well. 
            // I could not figure out how to get the first column to look consistent with the other columns so I am leaving the border off for the first column...it's very annoying. 
            if (isValid == false)
            {
                cell.Background = new SolidColorBrush(Color.FromArgb(255, 247, 182, 175));
                if (isY == false)
                {
                    cell.BorderThickness = new Thickness(1d);
                    cell.BorderBrush = Brushes.Red;
                    cell.Margin = new Thickness(0d);
                }
                else
                {
                    cell.Padding = new Thickness(1d);
                }
                cell.ToolTip = tooltip;
            }
            else
            {
                cell.ClearValue(Control.BorderThicknessProperty);
                cell.ClearValue(Control.BorderBrushProperty);
                cell.ClearValue(Control.BackgroundProperty);
                cell.ClearValue(Control.PaddingProperty);
                cell.ClearValue(FrameworkElement.MarginProperty);
                cell.ClearValue(FrameworkElement.ToolTipProperty);
            }
        }

        /// <summary>
        /// Updates the BivariateCDF property with current data from the data grid.
        /// </summary>
        private void UpdateBivariateCDF()
        {
            if (BivariateCDF == null) return;

            // X1 values
            var x1Vals = new double[_dt.Rows.Count - 1];
            for (int i = 1; i < _dt.Rows.Count; i++)
                x1Vals[i - 1] = _dt.Rows[i][0] == DBNull.Value ? 0d : (double)_dt.Rows[i][0];

            // X2 values
            var x2Vals = new double[_dt.Columns.Count - 1];
            for (int i = 1; i < _dt.Columns.Count; i++)
                x2Vals[i - 1] = _dt.Rows[0][i] == DBNull.Value ? 0d : (double)_dt.Rows[0][i];

            // Probabilities
            var pVals = new double[_dt.Rows.Count - 1, _dt.Columns.Count - 1];
            for (int i = 1; i < _dt.Rows.Count; i++)
            {
                for (int j = 1; j < _dt.Columns.Count; j++)
                    pVals[i - 1, j - 1] = _dt.Rows[i][j] == DBNull.Value ? 0d : (double)_dt.Rows[i][j];
            }
            BivariateCDF.SetParameters(x1Vals, x2Vals, pVals);
            GetBindingExpression(BivariateCDFProperty).UpdateSource();
        }

        private List<LineSeries> _lineSeriesList = new List<LineSeries>();

        /// <summary>
        /// Updates the plot with line series for each column of data.
        /// </summary>
        private void UpdatePlot()
        {
            // If the plot is nothing, then exit
            if (Plot == null) return;

            // Clear plot series
            Plot.Series.Clear();

            // Create a new line series for each column in the data grid
            var colorHexCodes = GenericControls.GeneralMethods.RandomColorsShortList;

            for (int i = 1; i < _dt.Columns.Count; i++)
            {
                var xyList = new List<Point>();
                for (int j = 1; j < _dt.Rows.Count; j++)
                {
                    xyList.Add(new Point(_dt.Rows[j][0] == DBNull.Value ? 0d : (double)_dt.Rows[j][0], _dt.Rows[j][i] == DBNull.Value ? 0d : (double)_dt.Rows[j][i]));
                }

                var newLineSeries = new LineSeries()
                {
                    Name = "series_" + i.ToString(),// (_dt.Rows[0][i] == DBNull.Value ? 0d : (double)_dt.Rows[0][i]).ToString(),
                    Title = (_dt.Rows[0][i] == DBNull.Value ? 0d : (double)_dt.Rows[0][i]).ToString(),
                    LineStyle = OxyPlot.LineStyle.Solid,
                    Color = (Color)ColorConverter.ConvertFromString(colorHexCodes[i-1]),
                    StrokeThickness = 1.5d,
                    ItemsSource = xyList,
                    DataFieldX = nameof(Point.X),
                    DataFieldY = nameof(Point.Y),
                    TrackerFormatString = "{0}" + Environment.NewLine + "{1}: {2:0}" + Environment.NewLine + "{3}: {4:0.000000}"
                };

                Plot.Series.Add(newLineSeries);
            }

        }

        /// <summary>
        /// Handles the AutoGeneratingColumn event of the data grid.
        /// Customizes column appearance and formatting.
        /// </summary>
        /// <param name="sender">The data grid that raised the event.</param>
        /// <param name="e">Event arguments containing the column being generated.</param>
        private void BivariateCDFDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.Width = new DataGridLength(1d, DataGridLengthUnitType.Star);
            e.Column.MinWidth = 35d;

            // Apply theme-aware editing style so the TextBox doesn't revert to white in dark mode
            if (e.Column is DataGridTextColumn textCol)
            {
                var editingStyle = TryFindResource("DataGridEditingTextBoxStyle") as Style;
                if (editingStyle != null)
                    textCol.EditingElementStyle = editingStyle;
            }

            if (e.PropertyName == "Column0")
            {
                e.Column.Header = "X";
                ((DataGridTextColumn)e.Column).CellStyle = (Style)Resources["BoldCellStyle"];
            }
            else
            {
                // Extract column number and create Y1, Y2, etc. header
                string columnNumber = e.PropertyName.Replace("Column", "");
                e.Column.Header = "Y" + columnNumber;
                (e.Column as DataGridTextColumn).Binding.TargetNullValue = Double.NaN.ToString();
                (e.Column as DataGridTextColumn).HeaderStyle = (Style)Resources["headerTemplate"];
            }
        }

        /// <summary>
        /// Refreshes the data grid view by styling the top row and upper left cell.
        /// </summary>
        private void RefreshDataGridView()
        {
            // Top Row
            var topRow = BivariateCDFDataGrid.GetRow(0);
            if (topRow == null) return;
            topRow.BorderThickness = new Thickness(0d, 0d, 0d, 2d);
            topRow.SetResourceReference(Control.BorderBrushProperty, "EnvironmentWindowText");

            // Apply styles to cells in the top row
            ApplyTopRowCellStyles();
        }

        /// <summary>
        /// Handles the LoadingRow event of the data grid.
        /// Applies styling to the top row (row 0) when it is loaded or reloaded.
        /// </summary>
        /// <param name="sender">The data grid that raised the event.</param>
        /// <param name="e">Event arguments containing the row being loaded.</param>
        private void BivariateCDFDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.GetIndex() == 0)
            {
                // Style the top row with a bottom border separator
                e.Row.BorderThickness = new Thickness(0d, 0d, 0d, 2d);
                e.Row.SetResourceReference(Control.BorderBrushProperty, "EnvironmentWindowText");

                // Apply cell styles after the row is fully loaded
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ApplyTopRowCellStyles();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// Applies styles to cells in the top row (row 0).
        /// </summary>
        private void ApplyTopRowCellStyles()
        {
            // Apply style to upper-left cell
            var upperLeftCell = BivariateCDFDataGrid.GetCell(0, 0);
            if (upperLeftCell != null)
                upperLeftCell.Style = (Style)Resources["UpperLeftCellStyle"];

            // Apply TopRowCellStyle to all other cells in row 0 (X2 values)
            var topRowCellStyle = (Style)Resources["TopRowCellStyle"];
            for (int col = 1; col < BivariateCDFDataGrid.Columns.Count; col++)
            {
                var cell = BivariateCDFDataGrid.GetCell(0, col);
                if (cell != null)
                    cell.Style = topRowCellStyle;
            }
        }

        /// <summary>
        /// Handles the BeginningEdit event of the data grid.
        /// Prevents editing of the top-left cell (0,0).
        /// </summary>
        /// <param name="sender">The data grid that raised the event.</param>
        /// <param name="e">Event arguments containing the cell being edited.</param>
        private void BivariateCDFDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // Ensure that the top left cell can't be edited.
            if (e.Column.DisplayIndex == 0 && e.Row.GetIndex() == 0) e.Cancel = true;
        }

        /// <summary>
        /// Handles the SelectedCellsChanged event of the data grid.
        /// Enables or disables insert and delete buttons based on cell selection.
        /// </summary>
        /// <param name="sender">The data grid that raised the event.</param>
        /// <param name="e">Event arguments containing the selection changes.</param>
        private void BivariateCDFDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (BivariateCDFDataGrid.SelectedCells.Count <= 0)
            {
                _insertColumnButton.IsEnabled = false;
                _deleteColumnButton.IsEnabled = false;
            }
            else
            {
                _insertColumnButton.IsEnabled = true;
                _deleteColumnButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the SizeChanged event of the data grid.
        /// </summary>
        /// <param name="sender">The data grid that raised the event.</param>
        /// <param name="e">Event arguments containing size information.</param>
        private void BivariateCDFDataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        /// <summary>
        /// Handles the LayoutUpdated event of the data grid.
        /// Adjusts the header margins to align with the first column and row.
        /// </summary>
        /// <param name="sender">The data grid that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void BivariateCDFDataGrid_LayoutUpdated(object sender, EventArgs e)
        {
            if (BivariateCDFDataGrid.Columns.Count == 0) return;

            // Calculate left margin for X2Header: row header width + Column0 width
            double rowHeaderWidth = GetRowHeaderWidth();
            double leftMargin = rowHeaderWidth + BivariateCDFDataGrid.Columns[0].ActualWidth;
            X2HeaderBorder.Margin = new Thickness(leftMargin, X2HeaderBorder.Margin.Top, X2HeaderBorder.Margin.Right, X2HeaderBorder.Margin.Bottom);

            var topRow = BivariateCDFDataGrid.GetRow(0);
            if (topRow == null) return;
            X1HeaderBorder.Margin = new Thickness(X1HeaderBorder.Margin.Left, topRow.ActualHeight, X1HeaderBorder.Margin.Right, X1HeaderBorder.Margin.Bottom);
        }

        /// <summary>
        /// Gets the actual width of the DataGrid row headers.
        /// </summary>
        /// <returns>The row header width, or 0 if not found.</returns>
        private double GetRowHeaderWidth()
        {
            var rowHeader = FindVisualChild<DataGridRowHeader>(BivariateCDFDataGrid);
            return rowHeader?.ActualWidth ?? 0;
        }

        /// <summary>
        /// Finds the first visual child of the specified type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of child to find.</typeparam>
        /// <param name="parent">The parent element to search.</param>
        /// <returns>The first child of the specified type, or null if not found.</returns>
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }


    }
}
