using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WpfCompat.Data;

namespace WpfCompat.Tests;

/// <summary>
/// WpfCompat's CollectionViewSource sorting by a property path, as upstream's time series selector sorts its
/// items by <c>"Alternative.Name"</c>, live, and the analysis selector by <c>"Name"</c>, a property its items
/// don't have. Run on the UI thread: live sorting through a path uses Avalonia's weak events.
/// </summary>
[TestClass]
public sealed class CollectionViewSourcePathTests
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
    }

    // An item that shows another object, as the selectors' items show an element.
    private sealed class Item : INotifyPropertyChanged
    {
        private Named _alternative;

        public Item(Named alternative) => _alternative = alternative;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Named Alternative
        {
            get => _alternative;
            set
            {
                _alternative = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Alternative)));
            }
        }

        public override string ToString() => Alternative.Name;
    }

    private static ICollectionView ViewOf(ObservableCollection<Item> items, string path, bool live = true)
    {
        var csv = new CollectionViewSource { Source = items, IsLiveSortingRequested = live };
        csv.SortDescriptions.Add(new SortDescription(path, ListSortDirection.Ascending));
        ICollectionView view = csv.View!;
        view.MoveCurrentToPosition(-1);
        return view;
    }

    private static string Names(ICollectionView view) => string.Join(",", view.Cast<object>());

    [TestMethod]
    public void View_SortsByAPropertyPath_AndKeepsTheOrderWhereThePathDoesNotResolve()
    {
        HeadlessSession.Run(() =>
        {
            var items = new ObservableCollection<Item> { new(new("b")), new(new("C")), new(new("a")) };

            Assert.AreEqual("a,b,C", Names(ViewOf(items, "Alternative.Name")), "text compares as the invariant culture, not ordinally");
            Assert.AreEqual("b,C,a", Names(ViewOf(items, "Name")), "the items have no Name: they keep the collection's order");
        });
    }

    [TestMethod]
    public void LiveSorting_ThroughAPath_FollowsTheObjectsAlongIt()
    {
        HeadlessSession.Run(() =>
        {
            var b = new Item(new("b"));
            var items = new ObservableCollection<Item> { new(new("a")), b, new(new("c")) };
            var view = ViewOf(items, "Alternative.Name");
            var changes = new List<string>();
            view.CollectionChanged += (_, e) => changes.Add($"{e.Action} {e.OldStartingIndex}->{e.NewStartingIndex}");

            b.Alternative.Name = "z";
            Assert.AreEqual("a,c,z", Names(view));
            // The view announces the move of the item b passes (c, up), not b's own: Avalonia's selectors
            // drop a moved item from their selection, and WPF's keep it.
            CollectionAssert.AreEqual(new[] { "Move 2->1" }, changes, string.Join(" / ", changes));

            // The item shows another object: the view follows the new one, and no longer the old.
            Named old = b.Alternative;
            b.Alternative = new Named("0");
            Assert.AreEqual("0,a,c", Names(view));
            old.Name = "zz";
            Assert.AreEqual("0,a,c", Names(view));
            b.Alternative.Name = "m";
            Assert.AreEqual("a,c,m", Names(view));

            var still = ViewOf(items, "Alternative.Name", live: false);
            b.Alternative.Name = "0";
            Assert.AreEqual("a,c,0", Names(still), "without live sorting the view keeps its order");
        });
    }

    [TestMethod]
    public void LiveSorting_ThroughAPath_LetsAnUnusedViewGo()
    {
        HeadlessSession.Run(() =>
        {
            // Upstream makes a view each time a list loads, and the elements along its paths outlive it.
            var alternatives = new[] { new Named("a"), new Named("b") };
            WeakReference view = MakeView(alternatives);
            for (int i = 0; i < 3 && view.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Assert.IsFalse(view.IsAlive, "the elements along the paths don't keep the view");
            alternatives[0].Name = "c";
        });
    }

    // A view of a list of its own, whose items show the alternatives.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference MakeView(Named[] alternatives) =>
        new(ViewOf(new ObservableCollection<Item>(alternatives.Select(a => new Item(a))), "Alternative.Name"));
}
