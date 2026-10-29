using Numerics.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NumericControls
{
    /// <summary>
    /// A user control that provides an interactive interface for managing probability ordinates in distribution fitting.
    /// This control displays a data grid where users can enter and modify exceedance probabilities for analysis.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class ProbabilityOrdinatesControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProbabilityOrdinatesControl"/> class.
        /// Sets up the control and subscribes to collection change events for probability row items.
        /// </summary>
        public ProbabilityOrdinatesControl()
        {
            InitializeComponent();
            _probabilityRowItems.CollectionChanged += ProbabilityRowItems_CollectionChanged;
            DataGrid.PreviewPasteData += DataGrid_PreviewPasteData;
            DataGrid.DataPasted += DataGrid_DataPasted;
        }

        /// <summary>
        /// Identifies the <see cref="ProbabilityOrdinates"/> dependency property.
        /// This property represents the probability ordinates to be displayed and edited.
        /// </summary>
        public static readonly DependencyProperty ProbabilityOrdinatesProperty = DependencyProperty.Register(nameof(ProbabilityOrdinates), typeof(ProbabilityOrdinates), typeof(ProbabilityOrdinatesControl), new PropertyMetadata(null, ElementCallback));

        /// <summary>
        /// Gets or sets the probability ordinates for this control.
        /// </summary>
        /// <value>An instance of <see cref="Numerics.Data.ProbabilityOrdinates"/>.</value>
        public ProbabilityOrdinates ProbabilityOrdinates
        {
            get { return (ProbabilityOrdinates)GetValue(ProbabilityOrdinatesProperty); }
            set { SetValue(ProbabilityOrdinatesProperty, value); }
        }

        /// <summary>
        /// Callback method invoked when the <see cref="ProbabilityOrdinates"/> property changes.
        /// Handles cleanup of the old ordinates and initialization of the new ordinates, including event subscriptions and data grid population.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void ElementCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d as ProbabilityOrdinatesControl == null) return;
            var thisControl = (ProbabilityOrdinatesControl)d;

            // Remove old stuff
            // Clear old probability row items
            for (int i = thisControl._probabilityRowItems.Count - 1; i >= 0; i--)
            {
                thisControl._probabilityRowItems.RemoveAt(i);
            }
            // Clear data grid
            thisControl.DataGrid.ItemsSource = null;
            // Remove handlers
            var oldOrdinates = (ProbabilityOrdinates)e.OldValue;
            if (oldOrdinates != null)
                oldOrdinates.CollectionChanged -= thisControl.ProbabilityOrdinates_CollectionChanged;

            // Set up new stuff
            if (e.NewValue == null) return;
            var newOrdinates = e.NewValue as ProbabilityOrdinates;
            if (newOrdinates != null)
            {
                // Add row items
                foreach (double ordinate in newOrdinates)
                {
                    thisControl._probabilityRowItems.Add(new ProbabilityOrdinateRowItem(thisControl._probabilityRowItems, ordinate));
                }
                // Set data grid
                thisControl.DataGrid.ItemsSource = thisControl._probabilityRowItems;
                // Set RowType so the grid can create new rows even when empty
                thisControl.DataGrid.RowType = typeof(ProbabilityOrdinateRowItem);
                // Add handlers
                newOrdinates.CollectionChanged += thisControl.ProbabilityOrdinates_CollectionChanged;
            }

        }

        /// <summary>
        /// Flag to suppress UI updates when model changes are being propagated to prevent circular update loops.
        /// </summary>
        private bool _suppressUIUpdate = false;

        /// <summary>
        /// Flag to suppress model updates when UI changes are being propagated to prevent circular update loops.
        /// </summary>
        private bool _suppressModelUpdate = false;

        /// <summary>
        /// Observable collection of probability ordinate row items displayed in the data grid.
        /// </summary>
        private ObservableCollection<object> _probabilityRowItems = new ObservableCollection<object>();

        /// <summary>
        /// Handles the automatic column generation event for the data grid.
        /// Configures column styling, width, and tooltips for the probability column.
        /// </summary>
        /// <param name="sender">The data grid generating the column.</param>
        /// <param name="e">Event arguments containing the column being generated.</param>
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == nameof(ProbabilityOrdinateRowItem.Probability))
            {
                e.Column.MinWidth = 20;
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                ((DataGridTextColumn)e.Column).CellStyle = (Style)FindResource("Right_CellStyle");

                // Create header style based on Center_ColumnHeaderStyle with tooltip
                // Note: Must use Style constructor with basedOn parameter to preserve the style inheritance chain
                var baseHeaderStyle = (Style)FindResource("Center_ColumnHeaderStyle");
                var headerStyle = new Style(typeof(DataGridColumnHeader), baseHeaderStyle);

                headerStyle.Setters.Add(new Setter(ToolTipProperty, new TextBlock()
                {
                    Text = "Enter the desired probabilities.",
                    FontWeight = FontWeights.Normal,
                    TextAlignment = TextAlignment.Left,
                    TextWrapping = TextWrapping.Wrap
                }));
                ((DataGridTextColumn)e.Column).HeaderStyle = headerStyle;
            }
        }

        /// <summary>
        /// Handles the addition of new rows to the data grid.
        /// Synchronizes the addition of new probability ordinates to the underlying model.
        /// </summary>
        /// <param name="startRowIndex">The index where the first new row was added.</param>
        /// <param name="nRows">The number of rows that were added.</param>
        private void DataGrid_RowsAdded(int startRowIndex, int nRows)
        {
            _suppressUIUpdate = true;
            bool wasSuppressed = ProbabilityOrdinates.SuppressCollectionChanged;
            ProbabilityOrdinates.SuppressCollectionChanged = true;
            for (int i = 0; i < nRows; i++)
            {
                ProbabilityOrdinates.Insert(startRowIndex + i, ((ProbabilityOrdinateRowItem)_probabilityRowItems[startRowIndex + i]).Probability);
            }
            ProbabilityOrdinates.SuppressCollectionChanged = wasSuppressed;
            if (!wasSuppressed)
            {
                ProbabilityOrdinates.RaiseCollectionChangedReset();
            }
            _suppressUIUpdate = false;
        }

        /// <summary>
        /// Handles the deletion of rows from the data grid.
        /// Synchronizes the removal of probability ordinates from the underlying model.
        /// </summary>
        /// <param name="rowindices">The list of row indices that were deleted.</param>
        private void DataGrid_RowsDeleted(List<int> rowindices)
        {
            _suppressUIUpdate = true;
            bool wasSuppressed = ProbabilityOrdinates.SuppressCollectionChanged;
            ProbabilityOrdinates.SuppressCollectionChanged = true;
            for (int i = rowindices.Count - 1; i >= 0; i--)
            {
                ProbabilityOrdinates.RemoveAt(rowindices[i]);
            }
            ProbabilityOrdinates.SuppressCollectionChanged = wasSuppressed;
            if (!wasSuppressed)
            {
                ProbabilityOrdinates.RaiseCollectionChangedReset();
            }
            _suppressUIUpdate = false;
        }

        /// <summary>
        /// Handles collection change events for the probability row items.
        /// Subscribes or unsubscribes property change handlers for items as they are added or removed.
        /// </summary>
        /// <param name="sender">The observable collection that changed.</param>
        /// <param name="e">Event arguments containing information about the collection change.</param>
        private void ProbabilityRowItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ProbabilityOrdinateRowItem item in e.NewItems)
                {
                    item.PropertyChanged += ProbabilityOrdinateRowItem_PropertyChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (ProbabilityOrdinateRowItem item in e.OldItems)
                {
                    item.PropertyChanged -= ProbabilityOrdinateRowItem_PropertyChanged;
                }
            }
        }

        /// <summary>
        /// Handles property change events for individual probability ordinate row items.
        /// Updates the underlying model when a probability value changes in the UI.
        /// </summary>
        /// <param name="sender">The probability ordinate row item that changed.</param>
        /// <param name="e">Event arguments containing the name of the property that changed.</param>
        private void ProbabilityOrdinateRowItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ProbabilityOrdinateRowItem.Probability)) return;
            if (_suppressModelUpdate) return;

            int rowIndex = _probabilityRowItems.IndexOf((ProbabilityOrdinateRowItem)sender);
            if (rowIndex >= 0 && rowIndex < ProbabilityOrdinates.Count)
            {
                _suppressUIUpdate = true;
                ProbabilityOrdinates[rowIndex] = ((ProbabilityOrdinateRowItem)sender).Probability;
                _suppressUIUpdate = false;
            }
        }

        /// <summary>
        /// Handles collection change events for the probability ordinates in the model.
        /// Synchronizes changes from the model to the UI by updating the row items collection.
        /// </summary>
        /// <param name="sender">The probability ordinates collection that changed.</param>
        /// <param name="e">Event arguments containing information about the collection change.</param>
        private void ProbabilityOrdinates_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_suppressUIUpdate == false)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        _probabilityRowItems.Insert(e.NewStartingIndex + i, new ProbabilityOrdinateRowItem(_probabilityRowItems, (double)e.NewItems[i]));
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    for (int i = e.OldItems.Count - 1; i >= 0; i--)
                    {
                        _probabilityRowItems.RemoveAt(e.OldStartingIndex + i);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Replace)
                {
                    _suppressModelUpdate = true;
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        ((ProbabilityOrdinateRowItem)_probabilityRowItems[e.NewStartingIndex + i]).Probability = (double)e.NewItems[i];
                    }
                    _suppressModelUpdate = false;
                }
                else if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    // Rebuild UI row items from the model state
                    _suppressModelUpdate = true;
                    _probabilityRowItems.Clear();
                    if (ProbabilityOrdinates != null)
                    {
                        foreach (double ordinate in ProbabilityOrdinates)
                        {
                            _probabilityRowItems.Add(new ProbabilityOrdinateRowItem(_probabilityRowItems, ordinate));
                        }
                    }
                    DataGrid.ItemsSource = _probabilityRowItems;
                    _suppressModelUpdate = false;
                }
            }
        }

        /// <summary>
        /// Handles the preview paste data event from the data grid.
        /// Suppresses collection changed events during the paste operation to prevent
        /// individual events for each cell change.
        /// </summary>
        /// <param name="clipboardData">The clipboard data being pasted.</param>
        /// <param name="cancelPaste">Reference parameter to cancel the paste operation if needed.</param>
        private void DataGrid_PreviewPasteData(string[][] clipboardData, ref bool cancelPaste)
        {
            if (ProbabilityOrdinates != null)
                ProbabilityOrdinates.SuppressCollectionChanged = true;
            Mouse.OverrideCursor = Cursors.Wait;
        }

        /// <summary>
        /// Handles the data pasted event from the data grid.
        /// Re-enables collection changed events and raises a single Reset event to notify
        /// listeners that the collection has been modified.
        /// </summary>
        private void DataGrid_DataPasted()
        {
            if (ProbabilityOrdinates != null)
            {
                ProbabilityOrdinates.SuppressCollectionChanged = false;
                ProbabilityOrdinates.RaiseCollectionChangedReset();
            }
            Mouse.OverrideCursor = null;
        }
    }
}
