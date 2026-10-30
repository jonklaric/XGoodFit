using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Utilities;

namespace WpfCompat.Data;

/// <summary>
/// WPF's <c>System.ComponentModel.SortDescription</c>: a property, or a property path such as
/// <c>"Alternative.Name"</c>, to sort a view by, and the direction.
/// </summary>
public struct SortDescription
{
    /// <summary>Creates a sort description.</summary>
    /// <param name="propertyName">The property to sort by.</param>
    /// <param name="direction">The direction.</param>
    public SortDescription(string propertyName, ListSortDirection direction)
    {
        PropertyName = propertyName;
        Direction = direction;
    }

    /// <summary>The property, or property path, to sort by.</summary>
    public string PropertyName { get; set; }

    /// <summary>The direction.</summary>
    public ListSortDirection Direction { get; set; }
}

/// <summary>
/// WPF's <c>System.Windows.Data.CollectionViewSource</c>, as code makes one: a sorted view of a collection,
/// which a combo box shows.
/// </summary>
/// <remarks>
/// XGoodFit addition. Upstream's properties panels list the project's elements, sorted by name, in combo
/// boxes: they make a CollectionViewSource of a collection with <see cref="IsLiveSortingRequested"/> and a
/// sort description, and show its <see cref="View"/>. Avalonia has no collection views, so this gives that
/// use its WPF behaviour (see <see cref="SortedCollectionView"/>). The view is made from the properties
/// set when <see cref="View"/> is first read, as upstream reads it after setting them.
/// </remarks>
public class CollectionViewSource
{
    private SortedCollectionView? _view;

    /// <summary>The collection to view.</summary>
    public object? Source { get; set; }

    /// <summary>Whether the view sorts an item again when a property it sorts by changes.</summary>
    public bool IsLiveSortingRequested { get; set; }

    /// <summary>The properties the view sorts by, first to last.</summary>
    public Collection<SortDescription> SortDescriptions { get; } = new Collection<SortDescription>();

    /// <summary>The sorted view of <see cref="Source"/>.</summary>
    public SortedCollectionView View => _view ??= new SortedCollectionView((IEnumerable?)Source, SortDescriptions, IsLiveSortingRequested);
}

/// <summary>
/// A sorted, read-only view of a collection: WPF's <c>ListCollectionView</c> with sort descriptions, as
/// <see cref="CollectionViewSource"/> makes it.
/// </summary>
/// <remarks>
/// <para>
/// As in WPF, the view follows the collection: an added item goes to its sorted place and a removed one
/// leaves it, each with its own notification, so a combo box keeps its selection. Other changes resort the
/// view (a reset). With live sorting, an item whose sort property changes moves to its new place.
/// </para>
/// <para>
/// Values compare as WPF's view compares them without a culture (<see cref="Comparer.DefaultInvariant"/>),
/// and items that compare equal keep the collection's order. A sort property can be a path, as in WPF
/// (<c>"Alternative.Name"</c>); a path that doesn't resolve on an item gives null, so such items keep the
/// collection's order. Live sorting through a path follows the objects along it, through weak events, as
/// they are usually long-lived (a project's elements) while a view is made each time a list loads. The current position is WPF's: the view
/// starts at its first item, and <see cref="MoveCurrentToPosition"/> moves it (-1 is before the first).
/// Avalonia's selectors don't follow it; WPF's combo boxes did, which is why upstream moves it to -1.
/// </para>
/// </remarks>
public sealed class SortedCollectionView : IList, IReadOnlyList<object>, INotifyCollectionChanged, IWeakEventSubscriber<PropertyChangedEventArgs>
{
    private readonly IEnumerable? _source;
    private readonly SortDescription[] _sortDescriptions;
    private readonly bool _isLiveSorting;
    private readonly List<object> _items = new List<object>();
    private readonly HashSet<INotifyPropertyChanged> _watched = new HashSet<INotifyPropertyChanged>(ReferenceEqualityComparer.Instance);

    // Live sorting through paths: the objects along an item's sort paths (after the item itself), each with the
    // items it's on the path of, the path, and the property of it the path reads next.
    private readonly Dictionary<INotifyPropertyChanged, List<(object Item, string Path, string Property)>> _pathOwners =
        new Dictionary<INotifyPropertyChanged, List<(object Item, string Path, string Property)>>(ReferenceEqualityComparer.Instance);

    /// <summary>Creates the view.</summary>
    /// <param name="source">The collection to view, or null for an empty view.</param>
    /// <param name="sortDescriptions">The properties to sort by.</param>
    /// <param name="isLiveSorting">Whether an item moves when a property it's sorted by changes.</param>
    public SortedCollectionView(IEnumerable? source, IEnumerable<SortDescription> sortDescriptions, bool isLiveSorting)
    {
        _source = source;
        _sortDescriptions = sortDescriptions.ToArray();
        _isLiveSorting = isLiveSorting;
        if (_source is INotifyCollectionChanged changes)
        {
            changes.CollectionChanged += Source_CollectionChanged;
        }
        Load();
        CurrentPosition = _items.Count > 0 ? 0 : -1;
    }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    // The item a selector showing the view has selected (see SelectorItems.SetItemsSourceKeepingSelection).
    // Avalonia's selectors drop a moved item's selection, but keep one that other items move past, so a
    // live-sorting move of that item is announced as the moves of the items it passes.
    internal Func<object?>? SelectedItem { get; set; }

    /// <summary>The position of the current item, or -1 before the first item.</summary>
    public int CurrentPosition { get; private set; }

    /// <summary>The current item, or null if the position is outside the view.</summary>
    public object? CurrentItem => CurrentPosition >= 0 && CurrentPosition < _items.Count ? _items[CurrentPosition] : null;

    /// <summary>The number of items.</summary>
    public int Count => _items.Count;

    /// <summary>The item at a position.</summary>
    /// <param name="index">The position.</param>
    public object this[int index] => _items[index];

    /// <summary>Makes the item at a position current: WPF's <c>ICollectionView.MoveCurrentToPosition</c>.</summary>
    /// <param name="position">The position, from -1 (before the first item) to <see cref="Count"/> (after the last).</param>
    /// <returns>Whether the current item is in the view.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The position is before -1 or after <see cref="Count"/>.</exception>
    public bool MoveCurrentToPosition(int position)
    {
        if (position < -1 || position > _items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }
        CurrentPosition = position;
        return CurrentItem != null;
    }

    /// <inheritdoc/>
    public IEnumerator<object> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc/>
    public bool Contains(object? value) => value != null && _items.Contains(value);

    /// <inheritdoc/>
    public int IndexOf(object? value) => value == null ? -1 : _items.IndexOf(value);

    /// <inheritdoc/>
    public void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

    bool IList.IsReadOnly => true;

    bool IList.IsFixedSize => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    object? IList.this[int index]
    {
        get => _items[index];
        set => throw new NotSupportedException("The view is read-only.");
    }

    int IList.Add(object? value) => throw new NotSupportedException("The view is read-only.");

    void IList.Clear() => throw new NotSupportedException("The view is read-only.");

    void IList.Insert(int index, object? value) => throw new NotSupportedException("The view is read-only.");

    void IList.Remove(object? value) => throw new NotSupportedException("The view is read-only.");

    void IList.RemoveAt(int index) => throw new NotSupportedException("The view is read-only.");

    // Fills the view with the source's items, sorted.
    private void Load()
    {
        foreach (INotifyPropertyChanged item in _watched)
        {
            item.PropertyChanged -= Item_PropertyChanged;
        }
        _watched.Clear();
        foreach (INotifyPropertyChanged owner in _pathOwners.Keys)
        {
            WeakEvents.ThreadSafePropertyChanged.Unsubscribe(owner, this);
        }
        _pathOwners.Clear();
        _items.Clear();
        if (_source == null)
        {
            return;
        }
        // OrderBy is stable: equal items keep the collection's order.
        _items.AddRange(_source.Cast<object>().Select((item, index) => (item, index))
            .OrderBy(pair => pair.item, Comparer<object>.Create(Compare))
            .ThenBy(pair => pair.index)
            .Select(pair => pair.item));
        foreach (object item in _items)
        {
            Watch(item);
        }
    }

    private void Watch(object item)
    {
        if (_isLiveSorting && item is INotifyPropertyChanged changes && _watched.Add(changes))
        {
            changes.PropertyChanged += Item_PropertyChanged;
        }
        WatchPaths(item);
    }

    private void Unwatch(object item)
    {
        if (item is INotifyPropertyChanged changes && !_items.Contains(item) && _watched.Remove(changes))
        {
            changes.PropertyChanged -= Item_PropertyChanged;
        }
        if (!_items.Contains(item))
        {
            UnwatchPaths(item);
        }
    }

    // Watches the objects along an item's sort paths for the property the path reads next.
    private void WatchPaths(object item)
    {
        if (!_isLiveSorting)
        {
            return;
        }
        foreach (SortDescription description in _sortDescriptions)
        {
            string[] properties = (description.PropertyName ?? string.Empty).Split('.');
            object? owner = Read(item, properties[0]);
            for (int i = 1; i < properties.Length && owner != null; i++)
            {
                if (owner is INotifyPropertyChanged changes)
                {
                    if (!_pathOwners.TryGetValue(changes, out var watches))
                    {
                        _pathOwners[changes] = watches = new List<(object, string, string)>();
                        WeakEvents.ThreadSafePropertyChanged.Subscribe(changes, this);
                    }
                    watches.Add((item, description.PropertyName!, properties[i]));
                }
                owner = Read(owner, properties[i]);
            }
        }
    }

    private void UnwatchPaths(object item)
    {
        foreach (var (owner, watches) in _pathOwners.Where(pair => pair.Value.Any(w => ReferenceEquals(w.Item, item))).ToList())
        {
            watches.RemoveAll(w => ReferenceEquals(w.Item, item));
            if (watches.Count == 0)
            {
                _pathOwners.Remove(owner);
                WeakEvents.ThreadSafePropertyChanged.Unsubscribe(owner, this);
            }
        }
    }

    // Live sorting through a path: an object along it changed the property the path reads next.
    void IWeakEventSubscriber<PropertyChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
    {
        if (sender is not INotifyPropertyChanged owner || !_pathOwners.TryGetValue(owner, out var watches))
        {
            return;
        }
        foreach (var (item, path, _) in watches.Where(w => string.IsNullOrEmpty(e.PropertyName) || w.Property == e.PropertyName).ToList())
        {
            // An object further along the path may have been replaced: follow the path again.
            UnwatchPaths(item);
            WatchPaths(item);
            Item_PropertyChanged(item, new PropertyChangedEventArgs(path));
        }
    }

    private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        object? current = CurrentItem;
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (object item in e.NewItems)
            {
                int index = InsertionIndex(item);
                _items.Insert(index, item);
                Watch(item);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (object item in e.OldItems)
            {
                int index = _items.IndexOf(item);
                if (index < 0)
                {
                    continue;
                }
                _items.RemoveAt(index);
                Unwatch(item);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            }
        }
        else
        {
            Load();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        CurrentPosition = current != null && _items.Contains(current) ? _items.IndexOf(current) : Math.Min(CurrentPosition, _items.Count - 1);
    }

    // Live sorting: moves an item whose sort property changed to its new place.
    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender == null
            || (!string.IsNullOrEmpty(e.PropertyName) && !_sortDescriptions.Any(d => d.PropertyName == e.PropertyName || StartsPath(d, e.PropertyName))))
        {
            return;
        }
        if (string.IsNullOrEmpty(e.PropertyName) || _sortDescriptions.Any(d => StartsPath(d, e.PropertyName)))
        {
            // The item replaced the first object along a sort path: follow the path again.
            UnwatchPaths(sender);
            WatchPaths(sender);
        }
        int oldIndex = _items.IndexOf(sender);
        if (oldIndex < 0
            || ((oldIndex == 0 || Compare(_items[oldIndex - 1], sender) <= 0)
                && (oldIndex == _items.Count - 1 || Compare(sender, _items[oldIndex + 1]) <= 0)))
        {
            // Not in the view, or still in its sorted place.
            return;
        }
        object? current = CurrentItem;
        // Asked before the list changes: a selector reads its selected item from the list.
        bool selected = SelectedItem?.Invoke() == sender;
        _items.RemoveAt(oldIndex);
        int newIndex = InsertionIndex(sender);
        if (selected)
        {
            // Move the items it passes, one place each, in turn.
            _items.Insert(oldIndex, sender);
            int step = newIndex > oldIndex ? 1 : -1;
            for (int index = oldIndex; index != newIndex; index += step)
            {
                object passed = _items[index + step];
                _items[index] = passed;
                _items[index + step] = sender;
                CurrentPosition = current != null ? _items.IndexOf(current) : CurrentPosition;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, passed, index, index + step));
            }
            return;
        }
        _items.Insert(newIndex, sender);
        CurrentPosition = current != null ? _items.IndexOf(current) : CurrentPosition;
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, sender, newIndex, oldIndex));
    }

    // Where an item goes: after the items that sort before it or equal to it.
    private int InsertionIndex(object item)
    {
        int index = _items.Count;
        while (index > 0 && Compare(_items[index - 1], item) > 0)
        {
            index--;
        }
        return index;
    }

    private int Compare(object? x, object? y)
    {
        foreach (SortDescription description in _sortDescriptions)
        {
            int result = Comparer.DefaultInvariant.Compare(Value(x, description.PropertyName), Value(y, description.PropertyName));
            if (result != 0)
            {
                return description.Direction == ListSortDirection.Descending ? -result : result;
            }
        }
        return 0;
    }

    // Whether a sort path goes through the item's property (the path's first part, before a dot).
    private static bool StartsPath(SortDescription description, string? property) =>
        !string.IsNullOrEmpty(property) && (description.PropertyName ?? string.Empty).StartsWith(property + ".", StringComparison.Ordinal);

    // A property path's value, as WPF's view reads it: null where the path doesn't resolve.
    private static object? Value(object? item, string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return item;
        }
        object? value = item;
        foreach (string property in propertyName.Split('.'))
        {
            value = Read(value, property);
        }
        return value;
    }

    private static object? Read(object? item, string property) =>
        item == null ? null : TypeDescriptor.GetProperties(item).Find(property, false)?.GetValue(item);
}
