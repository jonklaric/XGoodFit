using Numerics.Data;
using Numerics.Distributions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

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
    /// </remarks>
    public partial class UncertainOrderedTableEditor : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="AddRemoveRows"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AddRemoveRowsProperty = DependencyProperty.Register(nameof(AddRemoveRows), typeof(bool), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(true, AddRemoveRows_PropertyChanged));

        /// <summary>
        /// Callback invoked when the AddRemoveRows property changes.
        /// Updates the data grid row manipulation capabilities.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void AddRemoveRows_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
        public static readonly DependencyProperty XColumnHeaderProperty = DependencyProperty.Register(nameof(XColumnHeader), typeof(string), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata("X Data"));

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
        public static readonly DependencyProperty YColumnHeaderProperty = DependencyProperty.Register(nameof(YColumnHeader), typeof(string), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata("Y Data"));

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
        public static readonly DependencyProperty IsStrictXProperty = DependencyProperty.Register(nameof(IsStrictX), typeof(bool), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(false, DP_PropertyChanged));

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
        public static readonly DependencyProperty IsStrictYProperty = DependencyProperty.Register(nameof(IsStrictY), typeof(bool), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(false, DP_PropertyChanged));

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
        public static readonly DependencyProperty OrderXProperty = DependencyProperty.Register(nameof(OrderX), typeof(SortOrder), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(SortOrder.Ascending, DP_PropertyChanged));

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
        public static readonly DependencyProperty OrderYProperty = DependencyProperty.Register(nameof(OrderY), typeof(SortOrder), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(SortOrder.Ascending, DP_PropertyChanged));

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
        public static readonly DependencyProperty MaximumXProperty = DependencyProperty.Register(nameof(MaximumX), typeof(double), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(double.MaxValue, DP_PropertyChanged));

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
        public static readonly DependencyProperty MinimumXProperty = DependencyProperty.Register(nameof(MinimumX), typeof(double), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(double.MinValue, DP_PropertyChanged));

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
        public static readonly DependencyProperty MaximumYProperty = DependencyProperty.Register(nameof(MaximumY), typeof(double), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(double.MaxValue, DP_PropertyChanged));

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
        public static readonly DependencyProperty MinimumYProperty = DependencyProperty.Register(nameof(MinimumY), typeof(double), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(double.MinValue, DP_PropertyChanged));

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
        private static void DP_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UncertainOrderedTableEditor thisControl = (UncertainOrderedTableEditor)d;
            thisControl.Refresh();
        }

        /// <summary>
        /// Identifies the <see cref="IsMergedHeaderVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMergedHeaderVisibleProperty = DependencyProperty.Register(nameof(IsMergedHeaderVisible), typeof(bool), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(true));

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
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(false));

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
        public static readonly DependencyProperty ShowToolBarProperty = DependencyProperty.Register(nameof(ShowToolBar), typeof(bool), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(false));

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
        public static readonly DependencyProperty UncertainOrderedDataProperty = DependencyProperty.Register(nameof(UncertainOrderedData), typeof(UncertainOrderedPairedData), typeof(UncertainOrderedTableEditor), new PropertyMetadata(null, UncertainOrderedData_PropertyChanged));

        /// <summary>
        /// Callback invoked when the UncertainOrderedData property changes.
        /// Unsubscribes from old data events, clears existing rows, and subscribes to new data events.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void UncertainOrderedData_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
        public static readonly DependencyProperty ColumnHeaderStyleProperty = DependencyProperty.Register(nameof(ColumnHeaderStyle), typeof(Style), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the style for column headers in the data grid.
        /// </summary>
        public Style ColumnHeaderStyle
        {
            get { return (Style)GetValue(ColumnHeaderStyleProperty); }
            set { SetValue(ColumnHeaderStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CellStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellStyleProperty = DependencyProperty.Register(nameof(CellStyle), typeof(Style), typeof(UncertainOrderedTableEditor), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the style for cells in the data grid.
        /// </summary>
        public Style CellStyle
        {
            get { return (Style)GetValue(CellStyleProperty); }
            set { SetValue(CellStyleProperty, value); }
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
            ColumnHeaderStyle = (Style)FindResource("WrappedColumnHeaderStyle");

            ValidationGrid.AutoGeneratedColumns += (object sender, EventArgs e) => ColumnsAutoGenerated?.Invoke(sender, ValidationGrid.Columns);
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
            ValidationGrid.SelectedCells.Clear();
            foreach (var item in ValidationGrid.Items) { ValidationGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column)); }
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
            var cd = new ColumnDefinition();
            cd.SetBinding(ColumnDefinition.WidthProperty, new Binding(nameof(DataGrid.RowHeaderActualWidth)) { Source = ValidationGrid });
            MergedColumnHeaderGrid.ColumnDefinitions.Add(cd);
            foreach (var c in ValidationGrid.Columns)
            {
                cd = new ColumnDefinition();
                cd.SetBinding(ColumnDefinition.WidthProperty, new Binding(nameof(DataGridColumn.ActualWidth)) { Source = c });
                MergedColumnHeaderGrid.ColumnDefinitions.Add(cd);
                // 
                if (c.HeaderStyle == null) c.HeaderStyle = ColumnHeaderStyle;
            }
            // 
            if (XGridColumnHeader.Style == null) XGridColumnHeader.Style = ColumnHeaderStyle;
            if (YGridColumnHeader.Style == null) YGridColumnHeader.Style = ColumnHeaderStyle;
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
        private void ValidationGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Exit if not clicked in empty space below bottom row.
            if (e.OriginalSource.GetType() != typeof(ScrollViewer)) { return; }
            //Commit edits
            ValidationGrid.CommitEdit();
            //Clear Selection
            ValidationGrid.UnselectAllCells();
            //Move focus to force validation redraw
            ValidationGrid.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }
}
