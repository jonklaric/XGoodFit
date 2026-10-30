using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Numerics.Data;
using Numerics.Distributions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using WpfCompat;
using WpfCompat.Input;

namespace NumericControls
{
    /// <summary>
    /// A user control for editing uncertain ordered paired data in a table format with distribution parameters.
    /// Provides data validation, row manipulation, and supports various probability distributions.
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
    /// <para>
    /// XGoodFit port: the properties are Avalonia styled properties, with upstream's callbacks, and the
    /// styles are ControlThemes. ProDataGrid's cells are selected through <see cref="CellSelection"/>, the
    /// column headers' Click is <see cref="ColumnHeaderClick"/>'s, and the merged header's column widths
    /// follow the grid's (<see cref="UpdateMergedColumnWidths"/>).
    /// </para>
    /// </remarks>
    public partial class UncertainOrderedTableEditor : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="AddRemoveRows"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> AddRemoveRowsProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, bool>(nameof(AddRemoveRows), true);

        /// <summary>
        /// Callback invoked when the AddRemoveRows property changes.
        /// Updates the data grid row manipulation capabilities.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void AddRemoveRows_PropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            UncertainOrderedTableEditor thisControl = (UncertainOrderedTableEditor)d;
            thisControl.ValidationGrid.CanUserAddInsertDeleteRows = thisControl.AddRemoveRows;
            thisControl.ValidationGrid.PasteAddsRows = thisControl.AddRemoveRows;
            thisControl.ValidationGridToolbar.DataGrid = thisControl.ValidationGrid;
        }

        /// <summary>
        /// Gets or sets whether users can add or remove rows in the data grid.
        /// </summary>
        public bool AddRemoveRows
        {
            get { return (bool)GetValue(AddRemoveRowsProperty); }
            set { SetValue(AddRemoveRowsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="XColumnHeader"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> XColumnHeaderProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, string>(nameof(XColumnHeader), "X Data");

        /// <summary>
        /// Gets or sets the header text for the X data column.
        /// </summary>
        public string XColumnHeader
        {
            get { return (string)GetValue(XColumnHeaderProperty); }
            set { SetValue(XColumnHeaderProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="YColumnHeader"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> YColumnHeaderProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, string>(nameof(YColumnHeader), "Y Data");

        /// <summary>
        /// Gets or sets the header text for the Y data column.
        /// </summary>
        public string YColumnHeader
        {
            get { return (string)GetValue(YColumnHeaderProperty); }
            set { SetValue(YColumnHeaderProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsStrictX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsStrictXProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, bool>(nameof(IsStrictX), false);

        /// <summary>
        /// Gets or sets whether X values must be strictly ordered (no duplicates allowed).
        /// </summary>
        public bool IsStrictX
        {
            get { return (bool)GetValue(IsStrictXProperty); }
            set { SetValue(IsStrictXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsStrictY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsStrictYProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, bool>(nameof(IsStrictY), false);

        /// <summary>
        /// Gets or sets whether Y values must be strictly ordered (no duplicates allowed).
        /// </summary>
        public bool IsStrictY
        {
            get { return (bool)GetValue(IsStrictYProperty); }
            set { SetValue(IsStrictYProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OrderX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<SortOrder> OrderXProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, SortOrder>(nameof(OrderX), SortOrder.Ascending);

        /// <summary>
        /// Gets or sets the sort order for X values.
        /// </summary>
        public SortOrder OrderX
        {
            get { return (SortOrder)GetValue(OrderXProperty); }
            set { SetValue(OrderXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OrderY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<SortOrder> OrderYProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, SortOrder>(nameof(OrderY), SortOrder.Ascending);

        /// <summary>
        /// Gets or sets the sort order for Y values.
        /// </summary>
        public SortOrder OrderY
        {
            get { return (SortOrder)GetValue(OrderYProperty); }
            set { SetValue(OrderYProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MaximumX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumXProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, double>(nameof(MaximumX), double.MaxValue);

        /// <summary>
        /// Gets or sets the maximum allowed X value for validation.
        /// </summary>
        public double MaximumX
        {
            get { return (double)GetValue(MaximumXProperty); }
            set { SetValue(MaximumXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MinimumX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumXProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, double>(nameof(MinimumX), double.MinValue);

        /// <summary>
        /// Gets or sets the minimum allowed X value for validation.
        /// </summary>
        public double MinimumX
        {
            get { return (double)GetValue(MinimumXProperty); }
            set { SetValue(MinimumXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MaximumY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumYProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, double>(nameof(MaximumY), double.MaxValue);

        /// <summary>
        /// Gets or sets the maximum allowed Y value for validation.
        /// </summary>
        public double MaximumY
        {
            get { return (double)GetValue(MaximumYProperty); }
            set { SetValue(MaximumYProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MinimumY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumYProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, double>(nameof(MinimumY), double.MinValue);

        /// <summary>
        /// Gets or sets the minimum allowed Y value for validation.
        /// </summary>
        public double MinimumY
        {
            get { return (double)GetValue(MinimumYProperty); }
            set { SetValue(MinimumYProperty, value); }
        }

        /// <summary>
        /// Callback invoked when validation-related dependency properties change.
        /// Refreshes the data grid to apply the new validation settings.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void DP_PropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            UncertainOrderedTableEditor thisControl = (UncertainOrderedTableEditor)d;
            thisControl.Refresh();
        }

        /// <summary>
        /// Identifies the <see cref="IsMergedHeaderVisible"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsMergedHeaderVisibleProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, bool>(nameof(IsMergedHeaderVisible), true);

        /// <summary>
        /// Gets or sets whether the merged column header row is visible.
        /// </summary>
        public bool IsMergedHeaderVisible
        {
            get { return (bool)GetValue(IsMergedHeaderVisibleProperty); }
            set { SetValue(IsMergedHeaderVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, bool>(nameof(IsReadOnly), false);

        /// <summary>
        /// Gets or sets whether the control is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowToolBar"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowToolBarProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, bool>(nameof(ShowToolBar), false);

        /// <summary>
        /// Gets or sets whether the toolbar is visible.
        /// </summary>
        public bool ShowToolBar
        {
            get { return (bool)GetValue(ShowToolBarProperty); }
            set { SetValue(ShowToolBarProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="UncertainOrderedData"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<UncertainOrderedPairedData> UncertainOrderedDataProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, UncertainOrderedPairedData>(nameof(UncertainOrderedData), null);

        /// <summary>
        /// Callback invoked when the UncertainOrderedData property changes.
        /// Unsubscribes from old data events, clears existing rows, and subscribes to new data events.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void UncertainOrderedData_PropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(UncertainOrderedTableEditor)) return;
            UncertainOrderedTableEditor thisControl = (UncertainOrderedTableEditor)d;
            // 
            if (e.OldValue != null)
            {
                UncertainOrderedPairedData oldValue = e.OldValue as UncertainOrderedPairedData;
                if (oldValue != null) oldValue.CollectionChanged -= thisControl.DataCollectionChanged;
            }

            foreach (DistributionRowItem row in thisControl.DistributionRows) { row.PropertyChanged -= thisControl.RowItem_PropertyChanged; }
            // 
            thisControl.DistributionRows.Clear();
            // 
            if (e.NewValue == null) return;
            UncertainOrderedPairedData newData = e.NewValue as UncertainOrderedPairedData;
            if (newData == null) return;
            newData.CollectionChanged += thisControl.DataCollectionChanged;
            // 
            thisControl.Refresh();
            thisControl.ForceGridValidation();
        }

        /// <summary>
        /// Get and set the selected probability distribution.
        /// </summary>
        public UncertainOrderedPairedData UncertainOrderedData
        {
            get { return (UncertainOrderedPairedData)GetValue(UncertainOrderedDataProperty); }
            set { SetValue(UncertainOrderedDataProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ColumnHeaderStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme> ColumnHeaderStyleProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, ControlTheme>(nameof(ColumnHeaderStyle), null);

        /// <summary>
        /// Gets or sets the style for column headers in the data grid.
        /// </summary>
        /// <remarks>XGoodFit port: WPF's styles are ControlThemes.</remarks>
        public ControlTheme ColumnHeaderStyle
        {
            get { return (ControlTheme)GetValue(ColumnHeaderStyleProperty); }
            set { SetValue(ColumnHeaderStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CellStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme> CellStyleProperty = AvaloniaProperty.Register<UncertainOrderedTableEditor, ControlTheme>(nameof(CellStyle), null);

        /// <summary>
        /// Gets or sets the style for cells in the data grid.
        /// </summary>
        /// <remarks>XGoodFit port: WPF's styles are ControlThemes.</remarks>
        public ControlTheme CellStyle
        {
            get { return (ControlTheme)GetValue(CellStyleProperty); }
            set { SetValue(CellStyleProperty, value); }
        }

        static UncertainOrderedTableEditor()
        {
            // XGoodFit port: the property-changed callbacks of upstream's metadata.
            AddRemoveRowsProperty.Changed.AddClassHandler<UncertainOrderedTableEditor>(AddRemoveRows_PropertyChanged);
            IsStrictXProperty.Changed.AddClassHandler<UncertainOrderedTableEditor>(DP_PropertyChanged);
            IsStrictYProperty.Changed.AddClassHandler<UncertainOrderedTableEditor>(DP_PropertyChanged);
            OrderXProperty.Changed.AddClassHandler<UncertainOrderedTableEditor>(DP_PropertyChanged);
            OrderYProperty.Changed.AddClassHandler<UncertainOrderedTableEditor>(DP_PropertyChanged);
            MaximumXProperty.Changed.AddClassHandler<UncertainOrderedTableEditor>(DP_PropertyChanged);
            MinimumXProperty.Changed.AddClassHandler<UncertainOrderedTableEditor>(DP_PropertyChanged);
            MaximumYProperty.Changed.AddClassHandler<UncertainOrderedTableEditor>(DP_PropertyChanged);
            MinimumYProperty.Changed.AddClassHandler<UncertainOrderedTableEditor>(DP_PropertyChanged);
            UncertainOrderedDataProperty.Changed.AddClassHandler<UncertainOrderedTableEditor>(UncertainOrderedData_PropertyChanged);
        }

        /// <summary>
        /// Gets the collection of distribution row items displayed in the data grid.
        /// </summary>
        public ObservableCollection<object> DistributionRows { get; private set; } = new ObservableCollection<object>();

        private bool _updatingData = false;

        /// <summary>
        /// Occurs when the data grid columns have been auto-generated.
        /// </summary>
        public event ColumnsAutoGeneratedEventHandler ColumnsAutoGenerated;

        /// <summary>
        /// Represents a method that handles the columns auto-generated event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The collection of auto-generated columns.</param>
        public delegate void ColumnsAutoGeneratedEventHandler(object sender, ObservableCollection<DataGridColumn> e);

        /// <summary>
        /// Initializes a new instance of the <see cref="UncertainOrderedTableEditor"/> class.
        /// </summary>
        public UncertainOrderedTableEditor()
        {
            // This call is required by the designer.
            InitializeComponent();
            // Add any initialization after the InitializeComponent() call.
            ColumnHeaderStyle = (ControlTheme)this.FindResource("WrappedColumnHeaderStyle");

            // XGoodFit port: ProDataGrid declares its columns an IList; they're an ObservableCollection, as WPF's.
            ValidationGrid.AutoGeneratedColumns += (object sender, EventArgs e) => ColumnsAutoGenerated?.Invoke(sender, (ObservableCollection<DataGridColumn>)ValidationGrid.Columns);

            // XGoodFit port: the grid's MouseDown (WpfCompat's, for every button), the Click EventSetter of
            // WrappedColumnHeaderStyle, and the merged header's column widths (see UpdateMergedColumnWidths).
            ValidationGrid.AddMouseDownHandler(ValidationGrid_MouseDown);
            ColumnHeaderClick.Add(ValidationGrid, (ControlTheme)Resources["WrappedColumnHeaderStyle"], DataGridColumnHeader_Click);
            _mergedColumnWidths.AddRange(new DataGridColumn[] { null, null, null });
            ValidationGrid.LayoutUpdated += (sender, e) => UpdateMergedColumnWidths();
        }

        /// <summary>
        /// Handles property changed events for distribution row items.
        /// Updates the underlying data when a row's values change.
        /// </summary>
        /// <param name="sender">The row item that changed.</param>
        /// <param name="e">The property changed event arguments.</param>
        private void RowItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _updatingData = true;
            DistributionRowItem rItem = (DistributionRowItem)sender;
            int dataIndex = DistributionRows.IndexOf(rItem);
            UncertainOrderedData[dataIndex] = new UncertainOrdinate(rItem.X, rItem.Distribution.Clone());
            _updatingData = false;
        }

        /// <summary>
        /// Handles collection changed events from the underlying data source.
        /// Synchronizes the distribution rows collection with data changes.
        /// </summary>
        /// <param name="sender">The collection that changed.</param>
        /// <param name="e">The collection changed event arguments.</param>
        private void DataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_updatingData == true) return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        DistributionRowItem rowItem;
                        foreach (UncertainOrdinate item in e.NewItems)
                        {
                            int index = UncertainOrderedData.IndexOf(item);
                            rowItem = new DistributionRowItem(item.X, item.Y, DistributionRows, MinimumX, MaximumX, MinimumY, MaximumY, IsStrictX, IsStrictY, OrderX, OrderY);
                            rowItem.PropertyChanged += RowItem_PropertyChanged;
                            // 
                            DistributionRows.Insert(index, rowItem);
                            rowItem.ForceValidation();
                        }

                        break;
                    }

                case NotifyCollectionChangedAction.Remove:
                    {
                        int startIndex = e.OldStartingIndex;
                        if (startIndex == -1)
                        {
                            Refresh();
                        }
                        else
                        {
                            for (int i = 1; i <= e.OldItems.Count; i++)
                                DistributionRows.RemoveAt(startIndex);
                        }

                        break;
                    }

                case NotifyCollectionChangedAction.Replace:
                    {
                        int index = UncertainOrderedData.IndexOf((UncertainOrdinate)e.NewItems[0]);
                        Refresh(index);
                        break;
                    }

                default:
                    {
                        Refresh();
                        break;
                    }
            }
            // 
        }

        /// <summary>
        /// Refreshes all rows in the data grid with current data.
        /// </summary>
        public void Refresh()
        {
            foreach (DistributionRowItem row in DistributionRows) { row.PropertyChanged -= RowItem_PropertyChanged; }
            DistributionRows.Clear();
            //
            if (UncertainOrderedData == null) return;
            DistributionRowItem rowItem;
            ValidationGrid.ItemsSource = null;
            foreach (var o in UncertainOrderedData)
            {
                rowItem = new DistributionRowItem(o.X, o.Y, DistributionRows, MinimumX, MaximumX, MinimumY, MaximumY, IsStrictX, IsStrictY, OrderX, OrderY);
                rowItem.PropertyChanged += RowItem_PropertyChanged;
                //
                DistributionRows.Add(rowItem);
            }

            ValidationGrid.ItemsSource = DistributionRows;
        }

        /// <summary>
        /// Refreshes a specific row in the data grid.
        /// </summary>
        /// <param name="rowIndex">The index of the row to refresh.</param>
        public void Refresh(int rowIndex)
        {
            if (UncertainOrderedData == null) return;
            if ((rowIndex >= UncertainOrderedData.Count) || (rowIndex < 0)) return;
            ((DistributionRowItem)DistributionRows[rowIndex]).PropertyChanged -= RowItem_PropertyChanged;
            //
            var rowItem = new DistributionRowItem(UncertainOrderedData[rowIndex].X, UncertainOrderedData[rowIndex].Y, DistributionRows, MinimumX, MaximumX, MinimumY, MaximumY, IsStrictX, IsStrictY, OrderX, OrderY);
            rowItem.PropertyChanged += RowItem_PropertyChanged;
            DistributionRows[rowIndex] = rowItem;
            rowItem.ForceValidation();
        }

        /// <summary>
        /// Forces validation on all rows in the data grid.
        /// </summary>
        public void ForceGridValidation()
        {
            foreach (var r in DistributionRows) { ((DistributionRowItem)r).ForceValidation(); }
        }

        /// <summary>
        /// Handles the column header click event to select all cells in that column.
        /// </summary>
        /// <param name="sender">The column header that was clicked.</param>
        /// <param name="e">The routed event arguments.</param>
        private void DataGridColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            DataGridColumnHeader columnHeader = sender as DataGridColumnHeader;
            if (columnHeader == null) { return; }
            //
            // XGoodFit port: the header's column is its OwningColumn, and ProDataGrid selects the cells its
            // SelectedCells is set to (CellSelection).
            var cells = new List<DataGridCellInfo>();
            foreach (var item in ValidationGrid.Items) { cells.Add(CellSelection.Cell(ValidationGrid, item, columnHeader.OwningColumn)); }
            CellSelection.Select(ValidationGrid, cells);
        }

        /// <summary>
        /// Handles the auto generated columns event from the validation grid.
        /// Creates merged column header definitions and applies column header styles.
        /// </summary>
        /// <param name="sender">The data grid that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void ValidationGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            MergedColumnHeaderGrid.ColumnDefinitions.Clear();
            // XGoodFit port: the widths follow the row headers and the columns (UpdateMergedColumnWidths), which
            // upstream binds them to.
            _mergedColumnWidths.Clear();
            var cd = new ColumnDefinition();
            _mergedColumnWidths.Add(null);
            MergedColumnHeaderGrid.ColumnDefinitions.Add(cd);
            foreach (var c in ValidationGrid.Columns)
            {
                cd = new ColumnDefinition();
                _mergedColumnWidths.Add(c);
                MergedColumnHeaderGrid.ColumnDefinitions.Add(cd);
                //
                if (c.HeaderTheme == null) c.HeaderTheme = ColumnHeaderStyle;
            }
            UpdateMergedColumnWidths();
            //
            if (XGridColumnHeader.Theme == null) XGridColumnHeader.Theme = ColumnHeaderStyle;
            if (YGridColumnHeader.Theme == null) YGridColumnHeader.Theme = ColumnHeaderStyle;
            // 
            // RaiseEvent ColumnsAutoGenerated(sender, ValidationGrid.Columns)
            if (IsMergedHeaderVisible == false)
            {
                MergedColumnHeaderGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                MergedColumnHeaderGrid.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Handles the preview add rows event from the validation grid.
        /// Creates new uncertain ordinates and inserts them into the data collection.
        /// </summary>
        /// <param name="startRowIndex">The starting index for the new rows.</param>
        /// <param name="nRows">The number of rows to add.</param>
        /// <param name="cancelAddRows">Reference parameter set to true to handle row addition manually.</param>
        private void ValidationGrid_PreviewAddRows(int startRowIndex, int nRows, ref bool cancelAddRows)
        {
            cancelAddRows = true;
            // 
            var type = UncertainOrderedData.Distribution;
            var itemsToInsert = new List<UncertainOrdinate>();
            UnivariateDistributionBase distribution;
            if (UncertainOrderedData.Count > 0)
                distribution = UncertainOrderedData[0].Y.Clone();
            else if (!UnivariateDistributionFactory.TryCreateDistribution(type, out distribution) || distribution is null)
                return;
            for (int i = startRowIndex; i < startRowIndex + nRows; i++)
                itemsToInsert.Add(new UncertainOrdinate(0d, distribution.Clone()));
            // Let it update the UI since this is a preview event that is being canceled.
            if (UncertainOrderedData.SuppressCollectionChanged == true)
            {
                UncertainOrderedData.SuppressCollectionChanged = false;
                UncertainOrderedData.InsertRange(startRowIndex, itemsToInsert);
                UncertainOrderedData.SuppressCollectionChanged = true;
            }
            else
            {
                UncertainOrderedData.InsertRange(startRowIndex, itemsToInsert);
            }
        }

        /// <summary>
        /// Handles the preview delete rows event from the validation grid.
        /// Removes the specified rows from the data collection.
        /// </summary>
        /// <param name="rowindices">The list of row indices to delete.</param>
        /// <param name="cancel">Reference parameter set to true to handle row deletion manually.</param>
        private void ValidationGrid_PreviewDeleteRows(List<int> rowindices, ref bool cancel)
        {
            cancel = true;
            // Let it update the UI since this is a preview event that is being canceled.
            if (UncertainOrderedData.SuppressCollectionChanged == true)
            {
                UncertainOrderedData.SuppressCollectionChanged = false;
                UncertainOrderedData.RemoveRange(rowindices.ToArray());
                UncertainOrderedData.SuppressCollectionChanged = true;
            }
            else
            {
                UncertainOrderedData.RemoveRange(rowindices.ToArray());
            }
            //
            ForceGridValidation();
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// Handles the preview paste data event from the validation grid.
        /// Suppresses collection changed events during paste and shows wait cursor.
        /// </summary>
        /// <param name="clipboardData">The clipboard data being pasted.</param>
        /// <param name="cancelPaste">Reference parameter to cancel the paste operation if needed.</param>
        private void ValidationGrid_PreviewPasteData(string[][] clipboardData, ref bool cancelPaste)
        {
            UncertainOrderedData.SuppressCollectionChanged = true;
            Mouse.OverrideCursor = Cursors.Wait;
        }

        /// <summary>
        /// Handles the data pasted event from the validation grid.
        /// Re-enables collection changed events, validates data, and restores cursor.
        /// </summary>
        private void ValidationGrid_DataPasted()
        {
            UncertainOrderedData.SuppressCollectionChanged = false;
            UncertainOrderedData.Validate();
            UncertainOrderedData.RaiseCollectionChangedReset();
            if (UncertainOrderedData.Distribution == UnivariateDistributionType.PertPercentile || UncertainOrderedData.Distribution == UnivariateDistributionType.PertPercentileZ) { Refresh(); }
            ForceGridValidation();
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// Handles the mouse down event on the validation grid.
        /// Commits edits and clears selection when clicking in empty space below rows.
        /// </summary>
        /// <param name="sender">The data grid that raised the event.</param>
        /// <param name="e">The mouse button event arguments.</param>
        /// <remarks>
        /// XGoodFit port: ProDataGrid scrolls its rows itself: the empty space below the bottom row is its rows
        /// presenter, where WPF's is the grid's ScrollViewer.
        /// </remarks>
        private void ValidationGrid_MouseDown(object sender, PointerPressedEventArgs e)
        {
            //Exit if not clicked in empty space below bottom row.
            if (e.OriginalSource.GetType() != typeof(DataGridRowsPresenter)) { return; }
            //Commit edits
            ValidationGrid.CommitEdit();
            //Clear Selection
            ValidationGrid.UnselectAllCells();
            //Move focus to force validation redraw
            ValidationGrid.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        // XGoodFit port: what the merged header's column widths follow, by column: the row headers (null), or
        // a grid column. Before the columns are generated, the second and third widths follow nothing (as
        // upstream's bindings to missing elements) and keep their star width.
        private readonly List<DataGridColumn> _mergedColumnWidths = new List<DataGridColumn>();

        /// <summary>
        /// Gives the merged header's columns the widths of the grid's row headers and columns, as upstream
        /// binds them to RowHeaderActualWidth and each column's ActualWidth.
        /// </summary>
        /// <remarks>
        /// XGoodFit addition. ProDataGrid has no RowHeaderActualWidth (its row headers' width is that of the
        /// top-left corner header, which shares their column) and its columns' ActualWidth raises no change,
        /// so the widths are copied after each layout of the grid.
        /// </remarks>
        private void UpdateMergedColumnWidths()
        {
            var definitions = MergedColumnHeaderGrid.ColumnDefinitions;
            for (int i = 0; i < definitions.Count && i < _mergedColumnWidths.Count; i++)
            {
                GridLength width;
                if (i == 0)
                {
                    var corner = ValidationGrid.GetVisualDescendants().OfType<DataGridColumnHeader>().FirstOrDefault(h => h.Name == "PART_TopLeftCornerHeader");
                    width = new GridLength(corner?.Bounds.Width ?? 0d);
                }
                else if (_mergedColumnWidths[i] is { } column)
                {
                    width = new GridLength(column.ActualWidth);
                }
                else
                {
                    continue;
                }
                if (definitions[i].Width != width)
                {
                    definitions[i].Width = width;
                }
            }
        }
    }
}
