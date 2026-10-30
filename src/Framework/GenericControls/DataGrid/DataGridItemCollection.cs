using System.Collections;
using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace GenericControls
{
    /// <summary>
    /// WPF's <c>DataGrid.Items</c> for ProDataGrid's grid: the grid's items in the order it shows them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// XGoodFit addition. WPF's DataGrid is an ItemsControl, and upstream's grids and the code that uses
    /// them read its <c>Items</c>: the count, an item by index, an item's index, a refresh, and the sort
    /// descriptions. ProDataGrid's grid isn't an ItemsControl; it shows its items through a
    /// <see cref="DataGridCollectionView"/>, which this reads, so sorting is reflected as in WPF.
    /// </para>
    /// <para>
    /// As in WPF, the grid's new-item row is the last item, <see cref="DataGridCollectionView.NewItemPlaceholder"/>,
    /// while the user can add rows.
    /// </para>
    /// <para>
    /// The sort descriptions are ProDataGrid's (<see cref="DataGridSortDescription"/>): WPF's
    /// <c>SortDescription</c> is Windows-only. Live sorting, which WPF's view does as items change, is
    /// done when an edit ends (see <see cref="IsLiveSorting"/>).
    /// </para>
    /// </remarks>
    public sealed class DataGridItemCollection : IEnumerable
    {
        private readonly DataGrid _grid;
        private bool _isLiveSorting;
        private bool _refreshPending;

        internal DataGridItemCollection(DataGrid grid)
        {
            _grid = grid;
        }

        // The grid's view of its items; null before it has items.
        private DataGridCollectionView View => _grid.CollectionView as DataGridCollectionView;

        // Whether the grid shows its new-item row after the items.
        private bool HasNewItemPlaceholder
        {
            get
            {
                var view = View;
                return view != null && _grid.CanUserAddRows && !_grid.IsReadOnly && _grid.IsEnabled
                    && view.CanAddNew && !view.IsAddingNew;
            }
        }

        /// <summary>The number of items, with the new-item row if the grid shows one.</summary>
        public int Count => (View?.Count ?? 0) + (HasNewItemPlaceholder ? 1 : 0);

        /// <summary>Whether the grid has no items.</summary>
        public bool IsEmpty => Count == 0;

        /// <summary>The item at <paramref name="index"/>, in the order the grid shows them.</summary>
        /// <param name="index">The item's index.</param>
        public object this[int index]
        {
            get
            {
                var view = View;
                int count = view?.Count ?? 0;
                if (index == count && HasNewItemPlaceholder)
                {
                    return DataGridCollectionView.NewItemPlaceholder;
                }
                if (view == null || index < 0 || index >= count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return view[index];
            }
        }

        /// <summary>The index of <paramref name="item"/> in the order the grid shows them, or -1.</summary>
        /// <param name="item">The item.</param>
        public int IndexOf(object item)
        {
            var view = View;
            if (view == null)
            {
                return -1;
            }
            if (ReferenceEquals(item, DataGridCollectionView.NewItemPlaceholder))
            {
                return HasNewItemPlaceholder ? view.Count : -1;
            }
            return view.IndexOf(item);
        }

        /// <summary>Whether the grid shows <paramref name="item"/>.</summary>
        /// <param name="item">The item.</param>
        public bool Contains(object item) => IndexOf(item) >= 0;

        /// <summary>Recreates the view: sorts it again and reads the items' values again.</summary>
        public void Refresh() => View?.Refresh();

        /// <summary>The view's sort descriptions, empty before the grid has items.</summary>
        public DataGridSortDescriptionCollection SortDescriptions => View?.SortDescriptions ?? new DataGridSortDescriptionCollection();

        /// <summary>Whether an item is being edited: WPF's <c>IEditableCollectionView.IsEditingItem</c>.</summary>
        public bool IsEditingItem => View?.IsEditingItem ?? false;

        /// <summary>The collection the view shows.</summary>
        public IEnumerable SourceCollection => View?.SourceCollection;

        /// <summary>
        /// Whether a sorted grid sorts again when an item changes, as WPF's live sorting does.
        /// </summary>
        /// <remarks>
        /// WPF's view moves a changed item to its sorted place after the change. This sorts the view
        /// again after an edit in the grid ends, the way such items change; code that changes items
        /// itself refreshes the grid, as upstream's paste does.
        /// </remarks>
        public bool IsLiveSorting
        {
            get => _isLiveSorting;
            set
            {
                if (_isLiveSorting == value)
                {
                    return;
                }
                _isLiveSorting = value;
                if (value)
                {
                    _grid.AddHandler(DataGrid.CellEditEndedEvent, OnCellEditEnded, RoutingStrategies.Bubble, handledEventsToo: true);
                }
                else
                {
                    _grid.RemoveHandler(DataGrid.CellEditEndedEvent, OnCellEditEnded);
                }
            }
        }

        private void OnCellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            if (_refreshPending || e.EditAction != DataGridEditAction.Commit || SortDescriptions.Count == 0)
            {
                return;
            }
            // After the grid has finished ending the edit.
            _refreshPending = true;
            Dispatcher.UIThread.Post(() =>
            {
                _refreshPending = false;
                if (View is { IsEditingItem: false, IsAddingNew: false } view && view.SortDescriptions.Count > 0)
                {
                    view.Refresh();
                }
            }, DispatcherPriority.Background);
        }

        /// <inheritdoc/>
        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }
    }
}
