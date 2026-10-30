using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data;
using WpfCompat.Data;

namespace WpfCompat.Tests;

/// <summary>
/// WpfCompat's <see cref="CollectionViewSource"/> gives upstream's sorted element lists their WPF behaviour:
/// a view sorted by name that follows the collection, with live sorting, shown in a combo box that keeps its
/// selection.
/// </summary>
[TestClass]
public sealed class CollectionViewSourceTests
{
    private sealed class Named : INotifyPropertyChanged
    {
        private string _name;

        public Named(string name) => _name = name;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public override string ToString() => Name;
    }

    // Upstream's ComboBox_Loaded: a live-sorted view by Name, with no current item.
    private static SortedCollectionView ViewOf(ObservableCollection<Named> items)
    {
        var csv = new CollectionViewSource { Source = items, IsLiveSortingRequested = true };
        csv.SortDescriptions.Add(new SortDescription(nameof(Named.Name), ListSortDirection.Ascending));
        SortedCollectionView view = csv.View;
        view.MoveCurrentToPosition(-1);
        return view;
    }

    private static string Names(SortedCollectionView view) => string.Join(",", view.Cast<Named>().Select(n => n.Name));

    [TestMethod]
    public void View_IsSortedByThePropertyAndFollowsAddsAndRemoves()
    {
        var c = new Named("c");
        var items = new ObservableCollection<Named> { c, new Named("a"), new Named("B") };
        SortedCollectionView view = ViewOf(items);
        var changes = new List<NotifyCollectionChangedEventArgs>();
        view.CollectionChanged += (_, e) => changes.Add(e);

        Assert.AreEqual("a,B,c", Names(view), "the invariant culture's order, as WPF's view without a culture");
        Assert.AreEqual(-1, view.CurrentPosition);
        Assert.IsNull(view.CurrentItem);

        items.Add(new Named("b2"));
        Assert.AreEqual("a,B,b2,c", Names(view));
        Assert.AreEqual(NotifyCollectionChangedAction.Add, changes[0].Action);
        Assert.AreEqual(2, changes[0].NewStartingIndex, "added in its sorted place");

        items.Remove(c);
        Assert.AreEqual("a,B,b2", Names(view));
        Assert.AreEqual(NotifyCollectionChangedAction.Remove, changes[1].Action);
        Assert.AreEqual(3, changes[1].OldStartingIndex);

        items.Clear();
        Assert.AreEqual(0, view.Count);
        Assert.AreEqual(NotifyCollectionChangedAction.Reset, changes[2].Action);
    }

    [TestMethod]
    public void LiveSorting_MovesARenamedItem_AndIgnoresOtherProperties()
    {
        var a = new Named("a");
        var items = new ObservableCollection<Named> { a, new Named("m"), new Named("z") };
        SortedCollectionView view = ViewOf(items);
        var changes = new List<NotifyCollectionChangedEventArgs>();
        view.CollectionChanged += (_, e) => changes.Add(e);

        a.Name = "y";
        Assert.AreEqual("m,y,z", Names(view));
        Assert.AreEqual(NotifyCollectionChangedAction.Move, changes.Single().Action);

        a.Name = "x";
        Assert.AreEqual(1, changes.Count, "still in its sorted place: nothing moves");

        var notLive = new CollectionViewSource { Source = items };
        notLive.SortDescriptions.Add(new SortDescription(nameof(Named.Name), ListSortDirection.Ascending));
        Assert.AreEqual("m,x,z", Names(notLive.View));
        a.Name = "0";
        Assert.AreEqual("m,0,z", Names(notLive.View), "without live sorting, a view keeps its order");
        Assert.AreEqual("0,m,z", Names(view));
    }

    [TestMethod]
    public void MoveCurrentToPosition_ChecksTheRange_AsWpf()
    {
        SortedCollectionView view = ViewOf(new ObservableCollection<Named> { new Named("a") });
        Assert.IsTrue(view.MoveCurrentToPosition(0));
        Assert.AreEqual("a", ((Named)view.CurrentItem!).Name);
        Assert.IsFalse(view.MoveCurrentToPosition(1), "after the last item");
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => view.MoveCurrentToPosition(2));
        Assert.AreEqual(0, new CollectionViewSource().View.Count, "no source, an empty view");
    }

    [TestMethod]
    public void ComboBox_ShowingTheView_KeepsItsBoundSelectionThroughAddsAndRenames()
    {
        HeadlessSession.Run(() =>
        {
            var b = new Named("b");
            var items = new ObservableCollection<Named> { new Named("c"), b, new Named("a") };
            var holder = new Holder { Selected = b };
            ComboBox combo = BoundComboBox(holder);
            var window = new Window { Content = combo };
            window.Show();
            try
            {
                // As upstream: the items arrive when the combo box has loaded, after its selection's binding.
                combo.SetItemsSourceKeepingSelection(ViewOf(items));
                Assert.AreSame(b, combo.SelectedItem);

                items.Add(new Named("0"));
                Assert.AreSame(b, combo.SelectedItem, "an item added before the selection");

                items[0].Name = "z";
                Assert.AreEqual("0,a,b,z", string.Join(",", ((SortedCollectionView)combo.ItemsSource!).Cast<Named>().Select(n => n.Name)));
                Assert.AreSame(b, combo.SelectedItem, "another item moved past the selection");

                b.Name = "zz";
                Assert.AreEqual("0,a,z,zz", string.Join(",", ((SortedCollectionView)combo.ItemsSource!).Cast<Named>().Select(n => n.Name)));
                Assert.AreSame(b, combo.SelectedItem, "the selected item moved");
                Assert.AreSame(b, holder.Selected);
                Assert.AreEqual("", string.Join(";", holder.Log), "showing and following the list writes nothing back");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [TestMethod]
    public void SetItemsSourceKeepingSelection_SelectsTheBoundValueOrNone_WithoutWritingBack()
    {
        HeadlessSession.Run(() =>
        {
            foreach (bool present in new[] { true, false })
            {
                var b = new Named("b");
                var holder = new Holder { Selected = b };
                ComboBox combo = BoundComboBox(holder);
                var window = new Window { Content = combo };
                window.Show();
                try
                {
                    combo.SetItemsSourceKeepingSelection(present ? new List<Named> { new Named("a"), b } : new List<Named> { new Named("a") });
                    Assert.AreEqual(present ? b : null, combo.SelectedItem, "present: " + present);
                    Assert.AreSame(b, combo.SelectedValue, "the value stays, as WPF's");
                    Assert.AreEqual("", string.Join(";", holder.Log), "nothing is written back, as in WPF");

                    // Avalonia's own assignment writes null back (then the value again, if it's among the items).
                    combo.ItemsSource = new List<Named> { b };
                    StringAssert.StartsWith(string.Join(";", holder.Log), "null");
                }
                finally
                {
                    window.Close();
                }
            }
        });
    }

    // A combo box whose SelectedValue is bound two-way to the holder's Selected, as upstream's XAML binds it.
    private static ComboBox BoundComboBox(Holder holder)
    {
        var combo = new ComboBox { DataContext = holder };
        combo.Bind(Avalonia.Controls.Primitives.SelectingItemsControl.SelectedValueProperty,
            new Binding(nameof(Holder.Selected)) { Mode = BindingMode.TwoWay });
        holder.Log.Clear();
        return combo;
    }

    private sealed class Holder : INotifyPropertyChanged
    {
        private object? _selected;

        public event PropertyChangedEventHandler? PropertyChanged;

        public List<string> Log { get; } = new List<string>();

        public object? Selected
        {
            get => _selected;
            set
            {
                Log.Add(value?.ToString() ?? "null");
                _selected = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
