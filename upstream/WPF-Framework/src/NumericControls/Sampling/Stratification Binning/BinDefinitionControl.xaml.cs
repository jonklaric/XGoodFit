using Numerics.Sampling;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace NumericControls
{
    /// <summary>
    /// A control for selecting a collection of stratified x values primarily for binning purposes.
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
    public partial class BinDefinitionControl : UserControl
    {

        /// <summary>
        /// Dependency property for the array of stratification options.
        /// </summary>
        public static readonly DependencyProperty StratificationOptionsCollectionProperty = DependencyProperty.Register(nameof(StratificationOptionsCollection), typeof(List<StratificationOptions>), typeof(BinDefinitionControl), new PropertyMetadata(null, SetStratificationOptionsCallback));

        /// <summary>
        /// Get and set the array of stratification options.
        /// </summary>
        public List<StratificationOptions> StratificationOptionsCollection
        {
            get { return (List<StratificationOptions>)GetValue(StratificationOptionsCollectionProperty); }
            set { SetValue(StratificationOptionsCollectionProperty, value); }
        }

        /// <summary>
        /// Callback invoked when the StratificationOptionsCollection property changes.
        /// Updates the data grid with the new collection of stratification options.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void SetStratificationOptionsCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(BinDefinitionControl)) return;
            BinDefinitionControl thisControl = (BinDefinitionControl)d;
            // 
            if (thisControl._settingSource) { return; }

            // Define the data
            foreach (var oldOptions in thisControl.StratificationOptionsRows)
            {
                ((StratificationOptionsRowItem)oldOptions).PropertyChanged -= thisControl.RItem_PropertyChanged;
            }
            thisControl.StratificationOptionsRows.Clear();

            //null checks
            if (e.NewValue == null) return;
            IList<StratificationOptions> newXValues = e.NewValue as IList<StratificationOptions>;
            if (newXValues == null) return;


            // Set data
            StratificationOptionsRowItem rowItem;
            foreach (StratificationOptions bin in newXValues)
            {
                rowItem = new StratificationOptionsRowItem(bin, thisControl.MaxBins, thisControl.StratificationOptionsRows);
                thisControl.StratificationOptionsRows.Add(rowItem);
            }
        }

        /// <summary>
        /// Identifies the <see cref="ColumnHeaderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderStyleProperty = DependencyProperty.Register(nameof(ColumnHeaderStyle), typeof(Style), typeof(BinDefinitionControl), new FrameworkPropertyMetadata(null));

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
        public static readonly DependencyProperty CellStyleProperty = DependencyProperty.Register(nameof(CellStyle), typeof(Style), typeof(BinDefinitionControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the style for cells in the data grid.
        /// </summary>
        public Style CellStyle
        {
            get { return (Style)GetValue(CellStyleProperty); }
            set { SetValue(CellStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MaxBins"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxBinsProperty = DependencyProperty.Register(nameof(MaxBins), typeof(int), typeof(BinDefinitionControl), new FrameworkPropertyMetadata(1000));

        /// <summary>
        /// Gets or sets the maximum number of bins allowed.
        /// </summary>
        /// <value>The maximum number of bins. Default is 1000.</value>
        public int MaxBins
        {
            get { return (int)GetValue(MaxBinsProperty); }
            set { SetValue(MaxBinsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(BinDefinitionControl), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the control is read-only.
        /// </summary>
        /// <value><c>true</c> if read-only; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="StringFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StringFormatProperty = DependencyProperty.Register(nameof(StringFormat), typeof(string), typeof(BinDefinitionControl), new FrameworkPropertyMetadata("{0:#,##0.####}"));

        /// <summary>
        /// Gets or sets the string format for displaying numeric values.
        /// </summary>
        /// <value>The format string. Default is "{0:#,##0.####}".</value>
        public string StringFormat
        {
            get { return (string)GetValue(StringFormatProperty); }
            set { SetValue(StringFormatProperty, value); }
        }

        /// <summary>
        /// Gets the collection of stratification option row items displayed in the data grid.
        /// </summary>
        public ObservableCollection<object> StratificationOptionsRows { get; private set; } = new ObservableCollection<object>();

        private bool _pastingData = false;
        private bool _settingSource = false;


        /// <summary>
        /// Dependency property for IsProbability.
        /// </summary>
        public static readonly DependencyProperty IsProbabilityProperty = DependencyProperty.Register(nameof(IsProbability), typeof(bool), typeof(BinDefinitionControl), new PropertyMetadata(false, IsProbabilityChanged));

        /// <summary>
        /// Callback invoked when the IsProbability property changes.
        /// Updates all row items in the validation data grid with the new probability setting.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void IsProbabilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(BinDefinitionControl)) return;
            BinDefinitionControl thisControl = (BinDefinitionControl)d;
            // 
            if (e.NewValue == null) return;
            bool newValue = (bool)e.NewValue;
            // Define the data
            foreach (StratificationOptionsRowItem r in thisControl.StratificationOptionsRows)
                r.IsProbability = newValue;
        }

        /// <summary>
        /// Determines whether or not the values in the bin definition control are probabilities.
        /// </summary>
        public bool IsProbability
        {
            get { return (bool)GetValue(IsProbabilityProperty); }
            set { SetValue(IsProbabilityProperty, value); }
        }

        /// <summary>
        /// Creates an empty bin definition control.
        /// </summary>
        public BinDefinitionControl()
        {

            // This call is required by the designer.
            InitializeComponent();

            // Add any initialization after the InitializeComponent() call.
            StratificationOptionsRows.CollectionChanged += StratificationOptionsRows_CollectionChanged;

            ValidationGrid.ItemsSource = StratificationOptionsRows;
        }

        /// <summary>
        /// Handles the collection changed event for stratification options rows.
        /// Manages property change event subscriptions for added and removed items.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Collection changed event arguments containing details about the change.</param>
        private void StratificationOptionsRows_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldOptions in e.OldItems)
                {
                    ((StratificationOptionsRowItem)oldOptions).PropertyChanged -= RItem_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newOptions in e.NewItems)
                {
                    ((StratificationOptionsRowItem)newOptions).PropertyChanged += RItem_PropertyChanged;
                }
            }
        }

        /// <summary>
        /// Handles the click event on a data grid column header.
        /// Selects all cells in the clicked column.
        /// </summary>
        /// <param name="sender">The column header that was clicked.</param>
        /// <param name="e">The routed event arguments.</param>
        private void DataGridColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.DataGridColumnHeader columnHeader = sender as System.Windows.Controls.Primitives.DataGridColumnHeader;
            if (columnHeader == null) return;
            // 
            ValidationGrid.SelectedCells.Clear();
            foreach (var item in ValidationGrid.Items)
            {
                ValidationGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
            }
        }

        /// <summary>
        /// Handles the event when rows are added to the validation grid.
        /// Updates the source collection with the new data.
        /// </summary>
        /// <param name="startrow">The starting row index of the added rows.</param>
        /// <param name="numrows">The number of rows that were added.</param>
        private void ValidationGrid_RowsAdded(int startrow, int numrows)
        {
            UpdateSource();
        }

        /// <summary>
        /// Handles the event when rows are deleted from the validation grid.
        /// Updates the source collection after deletion.
        /// </summary>
        /// <param name="rowindices">The list of indices of rows that were deleted.</param>
        private void ValidationGrid_RowsDeleted(List<int> rowindices)
        {
            UpdateSource();

        }

        /// <summary>
        /// Handles the preview paste event before data is pasted into the validation grid.
        /// Sets a flag to indicate that a paste operation is in progress.
        /// </summary>
        /// <param name="clipboardData">The two-dimensional array of clipboard data to be pasted.</param>
        /// <param name="cancelPaste">Reference to a boolean that can be set to true to cancel the paste operation.</param>
        private void ValidationGrid_PreviewPasteData(string[][] clipboardData, ref bool cancelPaste)
        {
            _pastingData = true;
        }

        /// <summary>
        /// Handles the event after data has been pasted into the validation grid.
        /// Resets the pasting flag and updates the source collection.
        /// </summary>
        private void ValidationGrid_DataPasted()
        {
            _pastingData = false;
            UpdateSource();
        }

        /// <summary>
        /// Handles the event after columns are auto-generated for the validation grid.
        /// Applies custom header styles and string formatting to the columns.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void ValidationGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            foreach (var c in ValidationGrid.Columns)
            {
                if (c.HeaderStyle == null) c.HeaderStyle = (Style)FindResource("WrappedColumnHeaderStyle");
                ((DataGridTextColumn)c).Binding.StringFormat = StringFormat;
            }
        }

        /// <summary>
        /// Updates the source stratification options collection from the current row items.
        /// Triggers the dependency property changed event to notify consumers of the updated data.
        /// </summary>
        private void UpdateSource()
        {
            if (_settingSource) { return; }
            if (_pastingData) { return; }
            _settingSource = true;
            try
            {
                List<StratificationOptions> optionsToSet = new List<StratificationOptions>();

                foreach (object option in StratificationOptionsRows)
                {
                    optionsToSet.Add(((StratificationOptionsRowItem)option).GetStratificationOptions());
                }

                StratificationOptionsCollection = optionsToSet;
            }
            finally
            {
                _settingSource = false;
            }
        }

        /// <summary>
        /// Handles property changed events for individual row items.
        /// Updates the source collection when any row item property changes.
        /// </summary>
        /// <param name="sender">The row item whose property changed.</param>
        /// <param name="e">Property changed event arguments containing the name of the changed property.</param>
        private void RItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            StratificationOptionsRowItem rItem = (StratificationOptionsRowItem)sender;
            int dataIndex = StratificationOptionsRows.IndexOf(rItem);
            if (dataIndex < 0) { return; }

            UpdateSource();
        }

    }
}
