using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Numerics.Data;

namespace NumericControls
{
    /// <summary>
    /// A user control for editing ordered paired data in a table format.
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
    public partial class OrderedDataTableEditor : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the OrderedDataTableEditor class.
        /// </summary>
        public OrderedDataTableEditor()
        {
            InitializeComponent();
            // Per-instance OrderedData (DP default is null — see DP registration).
            if (OrderedData == null)
            {
                OrderedData = new OrderedPairedData(false, SortOrder.Ascending, false, SortOrder.Ascending);
            }
        }

        /// <summary>
        /// Dependency property for the OrderedData property.
        /// </summary>
        // Default null — every reference-type DP default is a SHARED instance across
        // all OrderedDataTableEditor instances; two unbound editors would mutate one
        // another's data. Per-instance initialization in the constructor.
        public static readonly DependencyProperty OrderedDataProperty = DependencyProperty.Register(nameof(OrderedData), typeof(OrderedPairedData), typeof(OrderedDataTableEditor), new PropertyMetadata(null, SetData));

        /// <summary>
        /// Handles changes to the OrderedData property and updates the data grid.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The property changed event arguments.</param>
        private static void SetData(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(OrderedDataTableEditor)) return;
            OrderedDataTableEditor thisControl = (OrderedDataTableEditor)d;
            if (e.NewValue == null)
            {
                thisControl.CurveRows.Clear();
                return;
            }
            OrderedPairedData newCurve = e.NewValue as OrderedPairedData;
            if (newCurve == null)
            {
                thisControl.CurveRows.Clear();
                return;
            }
            thisControl.CurveRows.Clear();
            OrdinateRowItem rowItem;
            int index = 0;
            foreach (Ordinate o in newCurve)
            {
                rowItem = new OrdinateRowItem(o.X, o.Y, thisControl.XColumnHeader, thisControl.YColumnHeader, thisControl.CurveRows, thisControl.MinimumX, thisControl.MaximumX, thisControl.MinimumY, thisControl.MaximumY, newCurve.StrictX, newCurve.StrictY, newCurve.OrderX, newCurve.OrderY)
                {
                    Index = index++
                };
                rowItem.PropertyChanged += thisControl.RowItemPropertyChanged;
                thisControl.CurveRows.Add(rowItem);
            }
        }

        /// <summary>
        /// Handles property changed events for row items and updates the data source.
        /// </summary>
        /// <param name="sender">The row item that changed.</param>
        /// <param name="e">The property changed event arguments.</param>
        private void RowItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OrdinateRowItem rItem = (OrdinateRowItem)sender;
            // Use the cached positional index instead of CurveRows.IndexOf which is O(n) and
            // value-equality based. With duplicate (X, Y) pairs IndexOf would target the wrong row.
            int dataIndex = rItem.Index;
            if (dataIndex < 0 || dataIndex >= OrderedData.Count) return;
            OrderedData[dataIndex] = rItem.GetOrdinate();
        }

        /// <summary>
        /// Gets or sets the ordered paired data to be edited.
        /// </summary>
        public OrderedPairedData OrderedData
        {
            get { return (OrderedPairedData)GetValue(OrderedDataProperty); }
            set { SetValue(OrderedDataProperty, value); }
        }

        /// <summary>
        /// Dependency property for the MaximumX property.
        /// </summary>
        public static readonly DependencyProperty MaximumXProperty = DependencyProperty.Register(nameof(MaximumX), typeof(double), typeof(OrderedDataTableEditor), new FrameworkPropertyMetadata(double.MaxValue));

        /// <summary>
        /// Gets or sets the maximum allowed X value.
        /// </summary>
        public double MaximumX
        {
            get { return (double)GetValue(MaximumXProperty); }
            set { SetValue(MaximumXProperty, value); }
        }

        /// <summary>
        /// Dependency property for the MinimumX property.
        /// </summary>
        public static readonly DependencyProperty MinimumXProperty = DependencyProperty.Register(nameof(MinimumX), typeof(double), typeof(OrderedDataTableEditor), new FrameworkPropertyMetadata(double.MinValue));

        /// <summary>
        /// Gets or sets the minimum allowed X value.
        /// </summary>
        public double MinimumX
        {
            get { return (double)GetValue(MinimumXProperty); }
            set { SetValue(MinimumXProperty, value); }
        }

        /// <summary>
        /// Dependency property for the MaximumY property.
        /// </summary>
        public static readonly DependencyProperty MaximumYProperty = DependencyProperty.Register(nameof(MaximumY), typeof(double), typeof(OrderedDataTableEditor), new FrameworkPropertyMetadata(double.MaxValue));

        /// <summary>
        /// Gets or sets the maximum allowed Y value.
        /// </summary>
        public double MaximumY
        {
            get { return (double)GetValue(MaximumYProperty); }
            set { SetValue(MaximumYProperty, value); }
        }

        /// <summary>
        /// Dependency property for the MinimumY property.
        /// </summary>
        public static readonly DependencyProperty MinimumYProperty = DependencyProperty.Register(nameof(MinimumY), typeof(double), typeof(OrderedDataTableEditor), new FrameworkPropertyMetadata(double.MinValue));

        /// <summary>
        /// Gets or sets the minimum allowed Y value.
        /// </summary>
        public double MinimumY
        {
            get { return (double)GetValue(MinimumYProperty); }
            set { SetValue(MinimumYProperty, value); }
        }

        /// <summary>
        /// Dependency property for the IsReadOnly property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(OrderedDataTableEditor), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the data grid is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// Dependency property for the XColumnHeader property.
        /// </summary>
        public static readonly DependencyProperty XColumnHeaderProperty = DependencyProperty.Register(nameof(XColumnHeader), typeof(string), typeof(OrderedDataTableEditor), new FrameworkPropertyMetadata("X Data"));

        /// <summary>
        /// Gets or sets the column header text for the X data column.
        /// </summary>
        public string XColumnHeader
        {
            get { return (string)GetValue(XColumnHeaderProperty); }
            set { SetValue(XColumnHeaderProperty, value); }
        }

        /// <summary>
        /// Dependency property for the YColumnHeader property.
        /// </summary>
        public static readonly DependencyProperty YColumnHeaderProperty = DependencyProperty.Register(nameof(YColumnHeader), typeof(string), typeof(OrderedDataTableEditor), new FrameworkPropertyMetadata("Y Data"));

        /// <summary>
        /// Gets or sets the column header text for the Y data column.
        /// </summary>
        public string YColumnHeader
        {
            get { return (string)GetValue(YColumnHeaderProperty); }
            set { SetValue(YColumnHeaderProperty, value); }
        }

        /// <summary>
        /// Gets the collection of curve row items for the data grid.
        /// </summary>
        public ObservableCollection<object> CurveRows { get; private set; } = new ObservableCollection<object>();

        /// <summary>
        /// Updates the data grid by forcing validation on all rows.
        /// </summary>
        public void UpdateGrid()
        {
            foreach (var r in CurveRows)
                ((OrdinateRowItem)r).ForceValidation();
        }

        /// <summary>
        /// Handles column header clicks to select all cells in the clicked column.
        /// </summary>
        /// <param name="sender">The column header that was clicked.</param>
        /// <param name="e">The routed event arguments.</param>
        private void DataGridColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.DataGridColumnHeader columnHeader = sender as System.Windows.Controls.Primitives.DataGridColumnHeader;
            if (columnHeader == null) return;
            ValidationGrid.SelectedCells.Clear();
            foreach (var item in ValidationGrid.Items)
                ValidationGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
        }

        /// <summary>
        /// Handles rows added to the validation grid and updates the data source.
        /// </summary>
        /// <param name="startrow">The starting row index.</param>
        /// <param name="numrows">The number of rows added.</param>
        private void ValidationGrid_RowsAdded(int startrow, int numrows)
        {
            for (int i = startrow; i < startrow + numrows; i++)
                OrderedData.Insert(i, ((OrdinateRowItem)CurveRows[i]).GetOrdinate());
            UpdateGrid();
        }

        /// <summary>
        /// Handles rows deleted from the validation grid and updates the data source.
        /// </summary>
        /// <param name="rowindices">The list of deleted row indices.</param>
        private void ValidationGrid_RowsDeleted(List<int> rowindices)
        {
            rowindices.Sort();
            for (int i = rowindices.Count - 1; i >= 0; i -= 1)
                OrderedData.RemoveAt(rowindices[i]);
            UpdateGrid();
        }

        /// <summary>
        /// Handles data pasted into the validation grid and updates the UI.
        /// </summary>
        private void ValidationGrid_DataPasted()
        {
            UpdateGrid();
        }

        /// <summary>
        /// Handles preview add rows event to create new row items before they are added to the grid.
        /// </summary>
        /// <param name="startRowIndex">The starting row index for the new rows.</param>
        /// <param name="nRows">The number of rows to add.</param>
        /// <param name="cancelAddRows">Whether to cancel the add rows operation.</param>
        private void ValidationGrid_PreviewAddRows(int startRowIndex, int nRows, ref bool cancelAddRows)
        {
            cancelAddRows = true;
            for (int i = startRowIndex; i < startRowIndex + nRows; i++)
            {
                var r = new OrdinateRowItem(0d, 0d, XColumnHeader, YColumnHeader, CurveRows, MinimumX, MaximumX, MinimumY, MaximumY, OrderedData.StrictX, OrderedData.StrictY, OrderedData.OrderX, OrderedData.OrderY);
                r.PropertyChanged += RowItemPropertyChanged;
                CurveRows.Insert(i, r);
            }
            ValidationGrid_RowsAdded(startRowIndex, nRows);
        }
    }
}
