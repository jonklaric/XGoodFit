using GenericControls;
using Numerics.Data;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NumericControls
{
    /// <summary>
    /// A user control for displaying and editing time series data in a tabular format.
    /// Provides functionality for data validation, mathematical operations, and data manipulation.
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
    public partial class TimeSeriesTable : UserControl
    {
        #region Public Events

        /// <summary>
        /// Raised before data is pasted from the clipboard. Allows the consumer to suppress collection changed events.
        /// </summary>
        public event CopyPasteDataGrid.PreviewPasteDataEventHandler PreviewPasteData;

        /// <summary>
        /// Raised after data has been pasted from the clipboard. Allows the consumer to unsuppress and raise reset.
        /// </summary>
        public event CopyPasteDataGrid.DataPastedEventHandler DataPasted;

        /// <summary>
        /// Raised before rows are added to the grid. Allows the consumer to suppress collection changed events.
        /// </summary>
        public event CopyPasteDataGrid.PreviewAddRowsEventHandler PreviewAddRows;

        /// <summary>
        /// Raised after rows have been added to the grid. Allows the consumer to unsuppress and raise reset.
        /// </summary>
        public event CopyPasteDataGrid.RowsAddedEventHandler RowsAdded;

        /// <summary>
        /// Raised before rows are deleted from the grid. Allows the consumer to suppress collection changed events.
        /// </summary>
        public event CopyPasteDataGrid.PreviewDeleteRowsEventHandler PreviewDeleteRows;

        /// <summary>
        /// Raised after rows have been deleted from the grid. Allows the consumer to unsuppress and raise reset.
        /// </summary>
        public event CopyPasteDataGrid.RowsDeletedEventHandler RowsDeleted;

        #endregion

        #region RowItem Infrastructure

        /// <summary>
        /// The observable collection of <see cref="TimeSeriesRowItem"/> wrappers bound to the DataGrid.
        /// </summary>
        private ObservableCollection<object> _rowItems = new ObservableCollection<object>();

        /// <summary>
        /// Tracks the previously subscribed TimeSeries for CollectionChanged unsubscription.
        /// </summary>
        private TimeSeries _previousSeries;

        /// <summary>
        /// Rebuilds the <see cref="_rowItems"/> collection from the current <see cref="Series"/>.
        /// Passes the positional index to each <see cref="TimeSeriesRowItem"/> for O(1) replacement.
        /// </summary>
        private void RebuildRowItems()
        {
            using (TimeSeriesDataGrid?.SuspendValidation(validateOnResume: false))
            {
                var rebuiltRowItems = new ObservableCollection<object>();
                if (Series != null)
                {
                    for (int i = 0; i < Series.Count; i++)
                    {
                        rebuiltRowItems.Add(new TimeSeriesRowItem(rebuiltRowItems, Series[i], Series, i, TimeSeriesDataGrid));
                    }
                }

                // Populate while detached so the DataGrid receives one ItemsSource change instead
                // of one CollectionChanged notification for every ordinate in a large series.
                _rowItems = rebuiltRowItems;
                if (TimeSeriesDataGrid != null)
                {
                    TimeSeriesDataGrid.ItemsSource = _rowItems;
                }
            }
        }

        /// <summary>
        /// Forces validation on the bound row items when the grid has been initialized.
        /// </summary>
        private void ValidateRows()
        {
            TimeSeriesDataGrid?.ValidateTable();
        }

        /// <summary>
        /// Refreshes the grid view and revalidates the underlying row items.
        /// </summary>
        private void RefreshGridAndValidateRows()
        {
            TimeSeriesDataGrid?.Items.Refresh();
            ValidateRows();
        }

        /// <summary>
        /// Handles CollectionChanged events to keep <see cref="_rowItems"/> in sync.
        /// On Replace (from cell edit or undo replay): updates the RowItem via <see cref="TimeSeriesRowItem.SetOrdinate"/>.
        /// On Reset (from bulk undo replay): rebuilds the entire RowItems collection and refreshes the DataGrid.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments describing the collection change.</param>
        private void Series_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Replace && e.NewItems != null)
            {
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    int rowIndex = e.NewStartingIndex + i;
                    if (rowIndex >= 0 && rowIndex < _rowItems.Count)
                    {
                        var rowItem = (TimeSeriesRowItem)_rowItems[rowIndex];
                        rowItem.SetOrdinate((SeriesOrdinate<DateTime, double>)e.NewItems[i]);
                        rowItem.ForceValidation();
                    }
                }
                ValidateFollowingDateTime(e.NewStartingIndex, e.NewItems.Count);
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                RebuildRowItems();
                RefreshGridAndValidateRows();
            }
        }

        /// <summary>
        /// Revalidates the first unchanged row following a contiguous replacement range.
        /// </summary>
        /// <param name="startingIndex">The first replaced series index.</param>
        /// <param name="replacedCount">The number of replaced ordinates.</param>
        /// <remarks>
        /// Replaced rows are validated by the replace handler. Only the following row has an
        /// additional ordering relationship affected by the replacement.
        /// </remarks>
        private void ValidateFollowingDateTime(int startingIndex, int replacedCount)
        {
            if (Series == null || Series.TimeInterval != TimeInterval.Irregular)
                return;

            int followingIndex = startingIndex + replacedCount;
            if (followingIndex >= 0 && followingIndex < _rowItems.Count
                && _rowItems[followingIndex] is TimeSeriesRowItem followingRow)
            {
                followingRow.ValidateProperty(nameof(TimeSeriesRowItem.DateTime));
            }
        }

        #endregion

        /// <summary>
        /// Identifies the <see cref="Series"/> dependency property.
        /// </summary>
        // Default is null (not `new TimeSeries()`) — every reference-type DP default is
        // a SHARED instance across all TimeSeriesTable instances; two unbound editors
        // would mutate one another's data. Per-instance initialization happens in the
        // constructor.
        public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(nameof(Series), typeof(TimeSeries), typeof(TimeSeriesTable), new PropertyMetadata(null, SetData));

        /// <summary>
        /// Handles changes to the Series property and configures the grid for the time interval type.
        /// Builds the <see cref="_rowItems"/> collection and subscribes to CollectionChanged.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The property changed event arguments.</param>
        private static void SetData(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(TimeSeriesTable)) return;
            TimeSeriesTable thisControl = (TimeSeriesTable)d;

            // Unsubscribe from old series
            if (thisControl._previousSeries != null)
            {
                thisControl._previousSeries.CollectionChanged -= thisControl.Series_CollectionChanged;
                thisControl._previousSeries = null;
            }

            thisControl.DateTimeColumn.IsReadOnly = true;
            thisControl.DateTimeSelectorColumn.Visibility = Visibility.Collapsed;
            thisControl.DateTimeColumn.CellStyle = (Style)thisControl.TryFindResource("DateTime_CellStyleDisabled");
            if (e.NewValue == null)
            {
                thisControl.RebuildRowItems();
                if (thisControl.IsLoaded) thisControl.ValidateRows();
                return;
            }
            TimeSeries newSeries = e.NewValue as TimeSeries;
            if (newSeries == null)
            {
                thisControl.RebuildRowItems();
                thisControl.TimeSeriesDataGrid.IsEnabled = false;
                if (thisControl.IsLoaded) thisControl.ValidateRows();
                return;
            }

            if (newSeries.TimeInterval == TimeInterval.Irregular)
            {
                thisControl.DateTimeColumn.CellStyle = (Style)thisControl.TryFindResource("DateTime_CellStyle");
                thisControl.DateTimeSelectorColumn.Visibility = Visibility.Visible;
                thisControl.DateTimeColumn.IsReadOnly = false;
            }

            // Build RowItems from the new series
            thisControl.RebuildRowItems();

            // ValidationDataGrid performs the initial pass from its Loaded handler. Series
            // assignments after loading validate once here after the complete table is bound.
            if (thisControl.IsLoaded) thisControl.ValidateRows();

            // Subscribe to CollectionChanged for undo replay sync
            newSeries.CollectionChanged += thisControl.Series_CollectionChanged;
            thisControl._previousSeries = newSeries;
        }

        /// <summary>
        /// Gets or sets the time series data displayed in the table.
        /// </summary>
        public TimeSeries Series
        {
            get { return (TimeSeries)GetValue(SeriesProperty); }
            set { SetValue(SeriesProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MaximumX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumXProperty = DependencyProperty.Register(nameof(MaximumX), typeof(DateTime), typeof(TimeSeriesTable), new FrameworkPropertyMetadata(DateTime.MaxValue));

        /// <summary>
        /// Gets or sets the maximum allowed date/time value for validation.
        /// </summary>
        public DateTime MaximumX
        {
            get { return (DateTime)GetValue(MaximumXProperty); }
            set { SetValue(MaximumXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MinimumX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumXProperty = DependencyProperty.Register(nameof(MinimumX), typeof(DateTime), typeof(TimeSeriesTable), new FrameworkPropertyMetadata(DateTime.MinValue));

        /// <summary>
        /// Gets or sets the minimum allowed date/time value for validation.
        /// </summary>
        public DateTime MinimumX
        {
            get { return (DateTime)GetValue(MinimumXProperty); }
            set { SetValue(MinimumXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MaximumY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumYProperty = DependencyProperty.Register(nameof(MaximumY), typeof(double), typeof(TimeSeriesTable), new FrameworkPropertyMetadata(double.MaxValue));

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
        public static readonly DependencyProperty MinimumYProperty = DependencyProperty.Register(nameof(MinimumY), typeof(double), typeof(TimeSeriesTable), new FrameworkPropertyMetadata(double.MinValue));

        /// <summary>
        /// Gets or sets the minimum allowed Y value for validation.
        /// </summary>
        public double MinimumY
        {
            get { return (double)GetValue(MinimumYProperty); }
            set { SetValue(MinimumYProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TimeSeriesTable), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the control is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="XColumnHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XColumnHeaderProperty = DependencyProperty.Register(nameof(XColumnHeader), typeof(string), typeof(TimeSeriesTable), new FrameworkPropertyMetadata("X Data"));

        /// <summary>
        /// Gets or sets the column header text for the date/time (X) column.
        /// </summary>
        public string XColumnHeader
        {
            get { return (string)GetValue(XColumnHeaderProperty); }
            set { SetValue(XColumnHeaderProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="YColumnHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YColumnHeaderProperty = DependencyProperty.Register(nameof(YColumnHeader), typeof(string), typeof(TimeSeriesTable), new FrameworkPropertyMetadata("Y Data"));

        /// <summary>
        /// Gets or sets the column header text for the value (Y) column.
        /// </summary>
        public string YColumnHeader
        {
            get { return (string)GetValue(YColumnHeaderProperty); }
            set { SetValue(YColumnHeaderProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSeriesTable"/> class.
        /// </summary>
        public TimeSeriesTable()
        {
            InitializeComponent();
            // Per-instance initialization (the SeriesProperty default is now null to
            // avoid sharing one TimeSeries instance across every unbound editor).
            if (Series == null)
            {
                Series = new TimeSeries();
            }
            // When the control is unloaded (e.g. window closing or tab removed), unsubscribe from
            // the live TimeSeries so a long-lived data model does not pin this control in memory.
            // SeriesProperty resubscribes its CollectionChanged handler whenever Series changes.
            Unloaded += (s, e) =>
            {
                if (_previousSeries != null)
                {
                    _previousSeries.CollectionChanged -= Series_CollectionChanged;
                    _previousSeries = null;
                }
            };
            TimeSeriesDataGrid.RowType = typeof(SeriesOrdinate<DateTime, double>);
            TimeSeriesDataGrid.PasteAddsRows = true;
            TimeSeriesDataGrid.ItemsSource = _rowItems;

            var HeaderBinding = new Binding(nameof(XColumnHeader)) { Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, FallbackValue = "Date Times", TargetNullValue = "Date Times" };
            BindingOperations.SetBinding((DateTimeColumn), DataGridTemplateColumn.HeaderProperty, HeaderBinding);

            HeaderBinding = new Binding(nameof(YColumnHeader)) { Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, FallbackValue = "Date Values", TargetNullValue = "Date Values" };
            BindingOperations.SetBinding((ValueColumn), DataGridTemplateColumn.HeaderProperty, HeaderBinding);


            // context menu
            List<MathFunctionType> operands = new List<MathFunctionType>();
            List<MathFunctionType> nonOperands = new List<MathFunctionType>();
            foreach (MathFunctionType fnc in (MathFunctionType[])Enum.GetValues(typeof(MathFunctionType)))
            {
                if (HasOperand(fnc)) { operands.Add(fnc); }
                else { nonOperands.Add(fnc); }
            }

            var MathFunctionsMenu = new MenuItem() { Header = "Math Functions", Icon = Application.Current.TryFindResource("MathFunctionIcon") };
            foreach (MathFunctionType fnc in operands)
            {
                MathFunctionsMenu.Items.Add(new MenuItem() { Header = MathFunctionTypeToNameConverter.GetName(fnc), Icon = FunctionToImage(fnc), ToolTip = MathFunctionTypeToTooltipConverter.GetTooltip(fnc), Tag = fnc });
                MenuItem AddedMenuItem = (MenuItem)MathFunctionsMenu.Items[MathFunctionsMenu.Items.Count - 1];
                AddedMenuItem.Click += MathFunctionButton_Click;
            }
            foreach (MathFunctionType fnc in nonOperands)
            {
                MathFunctionsMenu.Items.Add(new MenuItem() { Header = MathFunctionTypeToNameConverter.GetName(fnc), Icon = FunctionToImage(fnc), ToolTip = MathFunctionTypeToTooltipConverter.GetTooltip(fnc), Tag = fnc });
                ((MenuItem)MathFunctionsMenu.Items[MathFunctionsMenu.Items.Count - 1]).Click += MathFunctionButton_Click;
            }

            TimeSeriesDataGrid.CustomMenuItems.Add(MathFunctionsMenu);
        }

        /// <summary>
        /// Determines whether a mathematical function type requires an operand value.
        /// </summary>
        /// <param name="function">The mathematical function type to check.</param>
        /// <returns><c>true</c> if the function requires an operand; otherwise, <c>false</c>.</returns>
        public static bool HasOperand(MathFunctionType function)
        {
            if (function == MathFunctionType.Add || function == MathFunctionType.Subtract ||
                function == MathFunctionType.Multiply || function == MathFunctionType.Divide ||
                function == MathFunctionType.Logarithm || function == MathFunctionType.Exponentiate ||
                function == MathFunctionType.Replace)
            { return true; }
            else
            { return false; }
        }

        /// <summary>
        /// Converts a math function type to an icon element.
        /// </summary>
        /// <param name="fnc">The math function type.</param>
        /// <returns>A ContentControl containing the function's icon.</returns>
        private ContentControl FunctionToImage(MathFunctionType fnc)
        {
            return new ContentControl { Width = 16, Height = 16, Content = MathFunctionTypeToIconConverter.GetIcon(fnc) };
        }

        /// <summary>
        /// Handles calculator button clicks and applies mathematical operations to selected cells.
        /// Fires the <see cref="PreviewPasteData"/> and <see cref="DataPasted"/> public events
        /// (reusing the paste event pattern) to allow the consumer to suppress/unsuppress
        /// collection changed notifications for undo/redo support of bulk math operations.
        /// </summary>
        /// <param name="sender">The menu item that was clicked.</param>
        /// <param name="e">The routed event arguments.</param>
        private void MathFunctionButton_Click(object sender, RoutedEventArgs e)
        {
            var ClickedMenuItem = sender as MenuItem;
            if (ClickedMenuItem is null) { return; }
            if (ClickedMenuItem.Tag is null) { return; }

            var FunctionType = (MathFunctionType)ClickedMenuItem.Tag;

            List<int> SelectedRowIndices = new List<int>();
            var selectedCells = TimeSeriesDataGrid.SelectedCells.ToArray();
            foreach (DataGridCellInfo cellInfo in selectedCells)
            {
                if (cellInfo.Column.DisplayIndex <= 1) { continue; }
                var rowItem = cellInfo.Item as TimeSeriesRowItem;
                if (rowItem != null)
                {
                    int idx = _rowItems.IndexOf(rowItem);
                    if (idx >= 0)
                        SelectedRowIndices.Add(idx);
                }
            }

            bool apply = false;
            double operandValue = 0;

            if (HasOperand(FunctionType))
            {
                var Dialog = new NumericEntryDialog() { Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner };
                var NameConverter = new MathFunctionTypeToNameConverter();
                var TooltipConverter = new MathFunctionTypeToTooltipConverter();
                Dialog.Title = (string)NameConverter.Convert(FunctionType, FunctionType.GetType(), null, null);
                Dialog.ToolTip = (string)TooltipConverter.Convert(FunctionType, FunctionType.GetType(), null, null);
                // Label
                Dialog.ValueTextBox.IsWholeNumber = false;
                Dialog.ValueTextBox.CanHaveNegative = true;
                switch (FunctionType)
                {
                    case MathFunctionType.Add:
                        Dialog.NameLabel.Text = "x + "; break;
                    case MathFunctionType.Subtract:
                        Dialog.NameLabel.Text = "x - "; break;
                    case MathFunctionType.Multiply:
                        Dialog.NameLabel.Text = "x * "; break;
                    case MathFunctionType.Divide:
                        Dialog.NameLabel.Text = "x / ";
                        Dialog.ValueTextBox.Value = 1;
                        break;
                    case MathFunctionType.Logarithm:
                        Dialog.NameLabel.Text = $"log(x)";
                        Dialog.ValueTextBox.Value = 10;
                        Dialog.ValueTextBox.ToolTip = "Base value for the log function.";
                        Dialog.ValueTextBox.IsWholeNumber = true;
                        Dialog.ValueTextBox.CanHaveNegative = false;
                        break;
                    case MathFunctionType.Exponentiate:
                        Dialog.NameLabel.Text = "x^"; break;
                    case MathFunctionType.Replace:
                        Dialog.NameLabel.Text = "x = "; break;
                    default:
                        Dialog.NameLabel.Text = FunctionType.ToString(); break;
                }

                if (Dialog.ShowDialog() == true)
                {
                    apply = true;
                    operandValue = Dialog.ValueTextBox.Value;
                }
            }
            else
            {
                apply = true;
            }

            if (apply)
            {
                if (FunctionType == MathFunctionType.Divide && operandValue == 0)
                {
                    GenericControls.MessageBox.Show("Cannot divide by zero.", "Invalid Operand", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Use suppress/unsuppress pattern for bulk math operations
                // Fire PreviewPasteData as a signal to suppress (reusing paste events for bulk ops)
                bool cancelMath = false;
                PreviewPasteData?.Invoke(null, ref cancelMath);

                MathEditorControl.ApplyFunctionToSeries(Series, FunctionType, operandValue, SelectedRowIndices);

                // Rebuild RowItems since values changed in-place
                RebuildRowItems();
                RefreshGridAndValidateRows();

                // Fire DataPasted as a signal to unsuppress and raise reset
                DataPasted?.Invoke();
            }

            // Reselect cells
            TimeSeriesDataGrid.SelectedCells.Clear();
            foreach (DataGridCellInfo cellInfo in selectedCells)
            {
                TimeSeriesDataGrid.SelectedCells.Add(cellInfo);
            }
        }

        /// <summary>
        /// Handles column header clicks to select all cells in the clicked column.
        /// </summary>
        /// <param name="sender">The column header that was clicked.</param>
        /// <param name="e">The routed event arguments.</param>
        private void DataGridColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            DataGridColumnHeader columnHeader = sender as DataGridColumnHeader;
            if (columnHeader == null) return;
            TimeSeriesDataGrid.SelectedCells.Clear();
            foreach (var item in TimeSeriesDataGrid.Items)
            {
                TimeSeriesDataGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
            }
        }

        /// <summary>
        /// Handles preview add rows event to create new time series ordinates before they are added to the grid.
        /// Fires the <see cref="PreviewAddRows"/> and <see cref="RowsAdded"/> public events to allow the consumer
        /// to manage SuppressCollectionChanged for undo/redo support.
        /// </summary>
        /// <param name="startRowIndex">The starting row index for the new rows.</param>
        /// <param name="nRows">The number of rows to add.</param>
        /// <param name="cancelAddRows">Whether to cancel the add rows operation.</param>
        private void TimeSeriesDataGrid_PreviewAddRows(int startRowIndex, int nRows, ref bool cancelAddRows)
        {
            cancelAddRows = true;

            // Fire public event to allow consumer to suppress
            bool cancel = false;
            PreviewAddRows?.Invoke(startRowIndex, nRows, ref cancel);
            if (cancel) return;

            // Start Time
            DateTime startTime = (Series == null || Series.Count == 0) ? new DateTime(2020, 1, 1, 0, 0, 0) : Series[0].Index;
            if (startRowIndex == 0)
            {
                for (int i = 0; i < nRows; i++) { startTime = TimeSeries.SubtractTimeInterval(startTime, Series.TimeInterval); }
            }

            // Insert into both Series and _rowItems so CopyPasteDataGrid.PasteClipboard() can
            // immediately access the new rows via Items[index] when setting pasted values.
            for (int i = startRowIndex; i < startRowIndex + nRows; i++)
            {
                var ordinate = new SeriesOrdinate<DateTime, double>(startTime, double.NaN);
                Series.Insert(i, ordinate);
            }

            if (Series.TimeInterval != TimeInterval.Irregular) { ReplaceWithIncrementedDates(startTime); }

            // Rebuild all RowItems to sync _rowItems with Series and fix positional indices.
            // Must happen before PasteClipboard resumes so Items[rowIndex + i] finds the new rows.
            RebuildRowItems();
            RefreshGridAndValidateRows();

            RowsAdded?.Invoke(startRowIndex, nRows);
        }

        /// <summary>
        /// Handles the math popup opened event and initializes the math editor control.
        /// </summary>
        /// <param name="sender">The popup that was opened.</param>
        /// <param name="e">The event arguments.</param>
        private void MathPopup_Opened(object sender, EventArgs e)
        {
            if (sender == null || sender.GetType() != typeof(Popup)) { return; }
            Popup mathPopup = (Popup)sender;
            if (mathPopup.DataContext == null) { return; }
            if (mathPopup.DataContext.GetType() != typeof(TimeSeriesTable)) { return; }
            // 
            Border border = mathPopup.Child as Border;
            if (border == null) { return; }
            MathEditorControl picker = border.Child as MathEditorControl;
            if (picker == null) { return; }
            // 
            picker.Series = Series;
            picker.Source = TimeSeriesDataGrid;

            if (!picker.ValueTextBox.IsKeyboardFocusWithin) { picker.ValueTextBox.Focus(); }
            picker.ValueTextBox.SelectAll();
        }

        /// <summary>
        /// Handles preview key down events to handle the Delete key for clearing cell values.
        /// Uses the <see cref="TimeSeriesRowItem"/> setters which trigger clone-and-replace,
        /// firing CollectionChanged(Replace) for undo/redo support.
        /// </summary>
        /// <param name="sender">The data grid.</param>
        /// <param name="e">The key event arguments.</param>
        private void TimeSeriesDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var grid = (CopyPasteDataGrid)sender;
            if (Key.Delete == e.Key)
            {
                foreach (var cell in grid.SelectedCells)
                {
                    if (cell.Column.IsReadOnly == false)
                    {
                        var rowItem = cell.Item as TimeSeriesRowItem;
                        if (rowItem != null)
                        {
                            if (cell.Column.DisplayIndex == 1)
                            {
                                rowItem.DateTime = DateTime.MinValue;
                            }
                            else if (cell.Column.DisplayIndex == 2)
                            {
                                rowItem.Value = double.NaN;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles rows added to the grid by the CopyPasteDataGrid.
        /// Row addition is fully managed in <see cref="TimeSeriesDataGrid_PreviewAddRows"/>,
        /// so this handler is intentionally empty.
        /// </summary>
        /// <param name="startRowIndex">The starting row index of added rows.</param>
        /// <param name="nRows">The number of rows added.</param>
        private void TimeSeriesDataGrid_RowsAdded(int startRowIndex, int nRows)
        {
            // Intentionally empty — row addition and event firing is handled in PreviewAddRows.
        }

        /// <summary>
        /// Handles preview delete rows event. Cancels the CopyPasteDataGrid's default delete
        /// (which only removes from _rowItems) and performs the delete on both <see cref="Series"/>
        /// and <see cref="_rowItems"/> directly. Fires <see cref="PreviewDeleteRows"/> to allow the
        /// consumer to suppress, then <see cref="RowsDeleted"/> to allow the consumer to unsuppress.
        /// </summary>
        /// <param name="rowindices">The indices of rows to be deleted.</param>
        /// <param name="cancel">Whether to cancel the delete operation.</param>
        private void TimeSeriesDataGrid_PreviewDeleteRows(List<int> rowindices, ref bool cancel)
        {
            // Cancel CopyPasteDataGrid's own delete — we handle both collections ourselves
            cancel = true;

            var sourceRowIndices = MapVisibleRowIndicesToSourceRowIndices(rowindices);
            if (sourceRowIndices.Count == 0) return;

            // Fire public event to allow consumer to suppress
            bool userCancel = false;
            PreviewDeleteRows?.Invoke(sourceRowIndices, ref userCancel);
            if (userCancel) return;

            // Start Time
            DateTime startTime = (Series == null || Series.Count == 0) ? new DateTime(2020, 1, 1, 0, 0, 0) : Series[0].Index;

            // Delete from Series in reverse order to maintain indices
            var sorted = sourceRowIndices.OrderByDescending(i => i).ToList();
            foreach (int idx in sorted)
            {
                if (idx >= 0 && idx < Series.Count)
                    Series.RemoveAt(idx);
            }

            if (Series.TimeInterval != TimeInterval.Irregular) { ReplaceWithIncrementedDates(startTime); }

            // Rebuild all RowItems to sync _rowItems with Series and fix positional indices.
            RebuildRowItems();
            RefreshGridAndValidateRows();

            RowsDeleted?.Invoke(sourceRowIndices);
        }

        /// <summary>
        /// Converts row indices from the current sorted view to indices in the backing row collection.
        /// </summary>
        /// <param name="rowindices">The row indices from the current grid view.</param>
        /// <returns>The corresponding distinct source indices in ascending order.</returns>
        private List<int> MapVisibleRowIndicesToSourceRowIndices(IEnumerable<int> rowindices)
        {
            var sourceRowIndices = new HashSet<int>();
            if (rowindices == null) return sourceRowIndices.ToList();

            foreach (int rowIndex in rowindices)
            {
                if (rowIndex < 0 || rowIndex >= TimeSeriesDataGrid.Items.Count) continue;
                if (TimeSeriesDataGrid.Items[rowIndex] is not TimeSeriesRowItem rowItem) continue;

                int sourceIndex = _rowItems.IndexOf(rowItem);
                if (sourceIndex >= 0)
                {
                    sourceRowIndices.Add(sourceIndex);
                }
            }

            return sourceRowIndices.OrderBy(index => index).ToList();
        }

        /// <summary>
        /// Handles rows deleted from the grid by the CopyPasteDataGrid.
        /// Row deletion is fully managed in <see cref="TimeSeriesDataGrid_PreviewDeleteRows"/>,
        /// so this handler is intentionally empty.
        /// </summary>
        /// <param name="rowindices">The indices of deleted rows.</param>
        private void TimeSeriesDataGrid_RowsDeleted(List<int> rowindices)
        {
            // Intentionally empty — row deletion and event firing is handled in PreviewDeleteRows.
        }

        /// <summary>
        /// Replaces all items in the series with new <see cref="SeriesOrdinate{TIndex, TValue}"/> objects
        /// whose dates are re-incremented from the given start time.
        /// </summary>
        /// <param name="startTime">The start date for the first item.</param>
        /// <remarks>
        /// Unlike <see cref="TimeSeries.ShiftAllDates"/> (which mutates existing objects in-place),
        /// this method creates new objects via the indexer so that any external references to the
        /// original objects (e.g., the undo bridge's shadow copy) are not corrupted.
        /// This method should only be called while SuppressCollectionChanged
        /// is true, as each indexer assignment fires a Replace event otherwise.
        /// </remarks>
        private void ReplaceWithIncrementedDates(DateTime startTime)
        {
            if (Series == null || Series.Count == 0) return;

            DateTime current = startTime;
            for (int i = 0; i < Series.Count; i++)
            {
                Series[i] = new SeriesOrdinate<DateTime, double>(current, Series[i].Value);
                current = TimeSeries.AddTimeInterval(current, Series.TimeInterval);
            }
        }

        /// <summary>
        /// Handles preview paste data event and fires the <see cref="PreviewPasteData"/> public event
        /// to allow the consumer to suppress collection changed notifications for undo/redo support.
        /// </summary>
        /// <param name="clipboardData">The clipboard data being pasted.</param>
        /// <param name="cancelPaste">Whether to cancel the paste operation.</param>
        private void TimeSeriesDataGrid_PreviewPasteData(string[][] clipboardData, ref bool cancelPaste)
        {
            // Suppress per-cell visual updates during paste — RebuildRowItems() at end creates fresh RowItems
            foreach (var item in _rowItems)
                ((TimeSeriesRowItem)item).SuppressNotify = true;
            PreviewPasteData?.Invoke(clipboardData, ref cancelPaste);

            // If a PreviewPasteData subscriber cancels the paste, DataPasted never fires and the
            // row items would be left permanently suppressed, making the grid unresponsive to
            // subsequent value changes. Unsuppress on cancel.
            if (cancelPaste)
            {
                foreach (var item in _rowItems)
                    ((TimeSeriesRowItem)item).SuppressNotify = false;
            }
        }

        /// <summary>
        /// Handles data pasted into the grid. Rebuilds the RowItems collection to include pasted data,
        /// then fires the <see cref="DataPasted"/> public event to allow the consumer to unsuppress
        /// and raise reset for undo/redo support.
        /// </summary>
        private void TimeSeriesDataGrid_DataPasted()
        {
            // RebuildRowItems removed — Series_CollectionChanged(Reset) handles it
            // when consumer calls RaiseCollectionChangedReset() in the DataPasted handler.
            DataPasted?.Invoke();
            ValidateRows();
        }

    }

    /// <summary>
    /// Converts a double value to a font style, displaying NaN and Infinity values in italic.
    /// </summary>
    public class DoubleToFontFamilyConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (double.IsNaN((double)value) || double.IsInfinity((double)value)) { return FontStyles.Italic; }
            return FontStyles.Normal;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts between <see cref="DateTime"/> values and their string representations using the current culture's date/time format.
    /// </summary>
    /// <remarks>
    /// The date/time pattern and culture are read on each call rather than captured as readonly
    /// fields. A user who changes the application culture (e.g., via localization settings) after
    /// the converter is constructed would otherwise see cells formatted and parsed against the
    /// old culture.
    /// </remarks>
    public class DateToStringConverter : IValueConverter
    {
        /// <summary>
        /// Builds the date/time format pattern for the supplied culture.
        /// </summary>
        /// <param name="culture">The culture whose date and time patterns are used.</param>
        /// <returns>The combined short date and short time pattern.</returns>
        private static string BuildPattern(CultureInfo culture)
        {
            return $"{culture.DateTimeFormat.ShortDatePattern} {culture.DateTimeFormat.ShortTimePattern}";
        }

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var current = CultureInfo.CurrentCulture;
            return ((DateTime)value).ToString(BuildPattern(current), current);
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var current = CultureInfo.CurrentCulture;
            DateTime newDate;
            if (DateTime.TryParseExact((string)value, BuildPattern(current), current, DateTimeStyles.None, out newDate) == false) { DateTime.TryParse((string)value, out newDate); }

            return newDate;
        }
    }
}
