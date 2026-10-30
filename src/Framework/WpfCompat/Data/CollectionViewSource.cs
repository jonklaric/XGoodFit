using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Utilities;

namespace WpfCompat.Data;

/// <summary>
/// WPF's <c>SortDescription</c> (System.ComponentModel, in WindowsBase): a property path to sort a
/// collection view by, and the direction.
/// </summary>
/// <param name="propertyName">The property path, such as <c>"Alternative.Name"</c>.</param>
/// <param name="direction">The direction.</param>
public readonly struct SortDescription(string propertyName, ListSortDirection direction) : IEquatable<SortDescription>
{
    /// <summary>The property path the items are sorted by.</summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>The direction they're sorted in.</summary>
    public ListSortDirection Direction { get; } = direction;

    /// <inheritdoc/>
    public bool Equals(SortDescription other) => PropertyName == other.PropertyName && Direction == other.Direction;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SortDescription other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(PropertyName, Direction);

    /// <summary>Whether two sort descriptions are the same.</summary>
    public static bool operator ==(SortDescription left, SortDescription right) => left.Equals(right);

    /// <summary>Whether two sort descriptions differ.</summary>
    public static bool operator !=(SortDescription left, SortDescription right) => !left.Equals(right);
}

/// <summary>
/// WPF's <c>ICollectionView</c> (System.ComponentModel), as far as the ported code uses it: the items in
/// the view's order, its sort descriptions, and its current item.
/// </summary>
public interface ICollectionView : IEnumerable, INotifyCollectionChanged
{
    /// <summary>The descriptions the view sorts its items by, first to last.</summary>
    ObservableCollection<SortDescription> SortDescriptions { get; }

    /// <summary>The culture text is compared in; null compares as the invariant culture, as WPF does.</summary>
    CultureInfo? Culture { get; set; }

    /// <summary>The current item, or null when the current position is outside the view.</summary>
    object? CurrentItem { get; }

    /// <summary>The current item's position: -1 before the first item.</summary>
    int CurrentPosition { get; }

    /// <summary>Makes the item at a position (-1: none) the current one.</summary>
    /// <param name="position">The position, from -1 to the number of items.</param>
    /// <returns>Whether the current item is in the view.</returns>
    bool MoveCurrentToPosition(int position);

    /// <summary>Builds the view again from its source.</summary>
    void Refresh();
}

/// <summary>
/// WPF's <c>CollectionViewSource</c> for the way upstream uses it in code: a view of a collection, sorted
/// by its <see cref="SortDescriptions"/>, that follows the collection's changes and, with
/// <see cref="IsLiveSortingRequested"/>, its items' changes of the sorted properties.
/// <code>
/// CollectionViewSource csv = new CollectionViewSource() { Source = list, IsLiveSortingRequested = true };
/// csv.SortDescriptions.Add(new SortDescription("Alternative.Name", ListSortDirection.Ascending));
/// listBox.ItemsSource = csv.View;
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// XGoodFit addition: Avalonia has no collection views (ProDataGrid's DataGridCollectionView neither
/// sorts live nor reads property paths as WPF does). The view (<see cref="ListCollectionView"/>) sorts as
/// WPF's does: values along each property path, compared with the invariant culture's comparer (WPF's
/// default view culture); a path that doesn't resolve on an item gives null, so such items keep their
/// order. Unlike WPF's sort, equal items always keep the source's order.
/// </para>
/// <para>
/// Like WPF's, the view listens to its source and items through weak events, so a view that's no longer
/// shown can be collected even while the collection and its items live on.
/// </para>
/// <para>
/// Not ported: filtering, grouping, XAML use, and synchronizing a selector's selection with the current
/// item (Avalonia's selectors don't), which upstream turns off with <c>MoveCurrentToPosition(-1)</c>.
/// </para>
/// </remarks>
public class CollectionViewSource
{
    private IEnumerable? _source;
    private ListCollectionView? _view;
    private bool _isLiveSortingRequested;
    private CultureInfo? _culture;

    /// <summary>Creates a collection view source.</summary>
    public CollectionViewSource()
    {
        SortDescriptions.CollectionChanged += (_, _) => _view?.SortDescriptionsChanged(SortDescriptions);
    }

    /// <summary>The collection the view shows.</summary>
    public IEnumerable? Source
    {
        get => _source;
        set
        {
            _source = value;
            _view = null;
        }
    }

    /// <summary>Whether the view moves an item when a property it's sorted by changes.</summary>
    public bool IsLiveSortingRequested
    {
        get => _isLiveSortingRequested;
        set
        {
            _isLiveSortingRequested = value;
            if (_view != null) _view.IsLiveSorting = value;
        }
    }

    /// <summary>The culture the view compares text in: null compares as the invariant culture.</summary>
    public CultureInfo? Culture
    {
        get => _culture;
        set
        {
            _culture = value;
            if (_view != null) _view.Culture = value;
        }
    }

    /// <summary>The descriptions the view sorts by, first to last.</summary>
    public ObservableCollection<SortDescription> SortDescriptions { get; } = new();

    /// <summary>The view of <see cref="Source"/>, or null without a source.</summary>
    public ICollectionView? View
    {
        get
        {
            if (_view == null && _source != null)
            {
                _view = new ListCollectionView(_source) { Culture = _culture, IsLiveSorting = _isLiveSortingRequested };
                _view.SortDescriptionsChanged(SortDescriptions);
            }
            return _view;
        }
    }
}

/// <summary>
/// WPF's <c>ListCollectionView</c>, as <see cref="CollectionViewSource"/> makes it: a read-only list of a
/// collection's items, sorted, which raises the collection changes its own order makes.
/// </summary>
public sealed class ListCollectionView : ICollectionView, IList,
    IWeakEventSubscriber<NotifyCollectionChangedEventArgs>, IWeakEventSubscriber<PropertyChangedEventArgs>
{
    private readonly IEnumerable _source;
    private readonly List<object?> _items = new();

    // Live sorting: each item's objects along its sort paths, and the property each is watched for.
    private readonly Dictionary<object, List<(object Owner, string Property)>> _watchedFor = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, HashSet<object>> _watchedBy = new(ReferenceEqualityComparer.Instance);

    private bool _isLiveSorting;
    private CultureInfo? _culture;
    private IComparer _comparer = Comparer.DefaultInvariant;
    private int _currentPosition = -1;

    /// <summary>Creates a view of a collection, in the collection's order until sort descriptions are added.</summary>
    /// <param name="source">The collection.</param>
    public ListCollectionView(IEnumerable source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        if (source is INotifyCollectionChanged changes) WeakEvents.CollectionChanged.Subscribe(changes, this);
        SortDescriptions.CollectionChanged += (_, _) => Refresh();
        Refresh();
    }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public ObservableCollection<SortDescription> SortDescriptions { get; } = new();

    /// <inheritdoc/>
    public CultureInfo? Culture
    {
        get => _culture;
        set
        {
            _culture = value;
            _comparer = value == null || value.Equals(CultureInfo.InvariantCulture) ? Comparer.DefaultInvariant : new Comparer(value);
            Refresh();
        }
    }

    /// <summary>Whether an item moves when a property it's sorted by changes.</summary>
    public bool IsLiveSorting
    {
        get => _isLiveSorting;
        set
        {
            if (_isLiveSorting == value) return;
            _isLiveSorting = value;
            WatchAll();
        }
    }

    /// <inheritdoc/>
    public object? CurrentItem => _currentPosition >= 0 && _currentPosition < _items.Count ? _items[_currentPosition] : null;

    /// <inheritdoc/>
    public int CurrentPosition => _currentPosition;

    /// <inheritdoc/>
    public int Count => _items.Count;

    /// <inheritdoc/>
    public object? this[int index]
    {
        get => _items[index];
        set => throw new NotSupportedException();
    }

    bool IList.IsReadOnly => true;

    bool IList.IsFixedSize => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    /// <inheritdoc/>
    public bool MoveCurrentToPosition(int position)
    {
        if (position < -1 || position > _items.Count) throw new ArgumentOutOfRangeException(nameof(position));
        _currentPosition = position;
        return CurrentItem != null;
    }

    /// <inheritdoc/>
    public void Refresh()
    {
        object? current = CurrentItem;
        _items.Clear();
        foreach (object? item in _source)
        {
            _items.Insert(SortedIndex(item, _items.Count), item);
        }
        _currentPosition = current == null ? (_currentPosition < 0 ? -1 : Math.Min(_currentPosition, _items.Count)) : _items.IndexOf(current);
        WatchAll();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <inheritdoc/>
    public bool Contains(object? value) => _items.Contains(value);

    /// <inheritdoc/>
    public int IndexOf(object? value) => _items.IndexOf(value);

    /// <inheritdoc/>
    public void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

    /// <inheritdoc/>
    public IEnumerator GetEnumerator() => _items.GetEnumerator();

    int IList.Add(object? value) => throw new NotSupportedException();

    void IList.Clear() => throw new NotSupportedException();

    void IList.Insert(int index, object? value) => throw new NotSupportedException();

    void IList.Remove(object? value) => throw new NotSupportedException();

    void IList.RemoveAt(int index) => throw new NotSupportedException();

    /// <summary>Takes a <see cref="CollectionViewSource"/>'s sort descriptions.</summary>
    internal void SortDescriptionsChanged(IEnumerable<SortDescription> descriptions)
    {
        if (SortDescriptions.SequenceEqual(descriptions)) return;
        SortDescriptions.Clear();
        foreach (SortDescription description in descriptions)
            SortDescriptions.Add(description);
    }

    void IWeakEventSubscriber<NotifyCollectionChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems != null:
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    object? item = e.NewItems[i];
                    // Unsorted, the view keeps the source's order.
                    int index = SortDescriptions.Count == 0 && e.NewStartingIndex >= 0 ? Math.Min(e.NewStartingIndex + i, _items.Count) : SortedIndex(item, _items.Count);
                    _items.Insert(index, item);
                    if (_currentPosition >= index) _currentPosition++;
                    Watch(item);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                }
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems != null:
                foreach (object? item in e.OldItems)
                {
                    int index = _items.IndexOf(item);
                    if (index < 0) continue;
                    _items.RemoveAt(index);
                    if (_currentPosition > index || _currentPosition >= _items.Count) _currentPosition--;
                    Unwatch(item);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                }
                break;
            default:
                Refresh();
                break;
        }
    }

    void IWeakEventSubscriber<PropertyChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
    {
        if (sender == null || !_watchedFor.TryGetValue(sender, out var watches)) return;
        foreach (object item in watches
            .Where(w => string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == w.Property)
            .Select(w => w.Owner)
            .Distinct(ReferenceEqualityComparer.Instance)
            .ToList())
        {
            // An object along the path may have been replaced: watch the item's path again.
            Unwatch(item);
            Watch(item);
            Reposition(item);
        }
    }

    // Moves an item whose sorted values changed to where they now sort, as WPF's live sorting does.
    private void Reposition(object item)
    {
        int oldIndex = _items.FindIndex(x => ReferenceEquals(x, item));
        if (oldIndex < 0) return;
        _items.RemoveAt(oldIndex);
        int newIndex = SortedIndex(item, _items.Count);
        _items.Insert(newIndex, item);
        if (newIndex == oldIndex) return;
        if (_currentPosition == oldIndex) _currentPosition = newIndex;
        else if (_currentPosition > oldIndex && _currentPosition <= newIndex) _currentPosition--;
        else if (_currentPosition < oldIndex && _currentPosition >= newIndex) _currentPosition++;
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
    }

    // Where an item goes among the first <count> items: after the last one it doesn't sort before, so
    // equal items keep their order.
    private int SortedIndex(object? item, int count)
    {
        if (SortDescriptions.Count == 0) return count;
        int low = 0, high = count;
        while (low < high)
        {
            int middle = (low + high) / 2;
            if (Compare(item, _items[middle]) < 0) high = middle;
            else low = middle + 1;
        }
        return low;
    }

    private int Compare(object? a, object? b)
    {
        foreach (SortDescription description in SortDescriptions)
        {
            int result = _comparer.Compare(ValueOf(a, description.PropertyName), ValueOf(b, description.PropertyName));
            if (result != 0) return description.Direction == ListSortDirection.Descending ? -result : result;
        }
        return 0;
    }

    // The value at the end of a property path, or null where the path doesn't resolve.
    private static object? ValueOf(object? item, string path)
    {
        object? value = item;
        foreach (string property in Segments(path))
        {
            if (value == null) return null;
            value = PropertyOf(value, property)?.GetValue(value);
        }
        return value;
    }

    private static string[] Segments(string path) =>
        string.IsNullOrEmpty(path) ? Array.Empty<string>() : path.Split('.');

    private static PropertyInfo? PropertyOf(object owner, string name) =>
        owner.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

    private void WatchAll()
    {
        foreach (object item in _watchedBy.Keys.ToList())
            Unwatch(item);
        foreach (object? item in _items)
            Watch(item);
    }

    // Watches the objects along an item's sort paths for changes of the next property on the path.
    private void Watch(object? item)
    {
        if (!_isLiveSorting || item == null) return;
        var watched = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (SortDescription description in SortDescriptions)
        {
            object? owner = item;
            foreach (string property in Segments(description.PropertyName))
            {
                if (owner == null) break;
                if (owner is INotifyPropertyChanged notifier)
                {
                    if (!_watchedFor.TryGetValue(owner, out var watches))
                    {
                        _watchedFor[owner] = watches = new List<(object, string)>();
                        WeakEvents.ThreadSafePropertyChanged.Subscribe(notifier, this);
                    }
                    watches.Add((item, property));
                    watched.Add(owner);
                }
                owner = PropertyOf(owner, property)?.GetValue(owner);
            }
        }
        _watchedBy[item] = watched;
    }

    private void Unwatch(object? item)
    {
        if (item == null || !_watchedBy.Remove(item, out var watched)) return;
        foreach (object owner in watched)
        {
            if (!_watchedFor.TryGetValue(owner, out var watches)) continue;
            watches.RemoveAll(w => ReferenceEquals(w.Owner, item));
            if (watches.Count == 0)
            {
                _watchedFor.Remove(owner);
                WeakEvents.ThreadSafePropertyChanged.Unsubscribe((INotifyPropertyChanged)owner, this);
            }
        }
    }
}
