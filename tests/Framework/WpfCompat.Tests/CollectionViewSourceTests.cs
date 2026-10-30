using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WpfCompat.Data;

namespace WpfCompat.Tests;

/// <summary>
/// WpfCompat's CollectionViewSource, as upstream's alternative selectors and properties panels use it: a
/// view of a list sorted by a property path, that follows the list and, sorting live, its items' names.
/// </summary>
[TestClass]
public sealed class CollectionViewSourceTests
{
    private sealed class Named : INotifyPropertyChanged
    {
        private string _name;

        public Named(string name) => _name = name;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // An item that shows another object, as the selectors' items show an element.
    private sealed class Item(Named alternative)
    {
        public Named Alternative { get; } = alternative;

        public override string ToString() => Alternative.Name;
    }

    private static ICollectionView SortedView(ObservableCollection<Item> list, string path, bool live = true, ListSortDirection direction = ListSortDirection.Ascending)
    {
        var csv = new CollectionViewSource { Source = list, IsLiveSortingRequested = live };
        csv.SortDescriptions.Add(new SortDescription(path, direction));
        return csv.View!;
    }

    private static string Names(ICollectionView view) => string.Join(",", view.Cast<object>());

    [TestMethod]
    public void View_SortsByAPropertyPath_InEitherDirection()
    {
        HeadlessSession.Run(() =>
        {
            var list = new ObservableCollection<Item> { new(new("b")), new(new("C")), new(new("a")) };

            Assert.AreEqual("a,b,C", Names(SortedView(list, "Alternative.Name")), "text compares as the invariant culture, not ordinally");
            Assert.AreEqual("C,b,a", Names(SortedView(list, "Alternative.Name", direction: ListSortDirection.Descending)));
            Assert.AreEqual("b,C,a", string.Join(",", list), "the source keeps its order");
        });
    }

    [TestMethod]
    public void View_KeepsTheSourcesOrder_WhereThePathDoesNotResolve()
    {
        HeadlessSession.Run(() =>
        {
            // As AlternativeControl's sort by "Name", which its items don't have.
            var list = new ObservableCollection<Item> { new(new("b")), new(new("c")), new(new("a")) };

            Assert.AreEqual("b,c,a", Names(SortedView(list, "Name")));
        });
    }

    [TestMethod]
    public void View_FollowsTheSource_AtSortedPositions()
    {
        HeadlessSession.Run(() =>
        {
            var list = new ObservableCollection<Item> { new(new("b")), new(new("d")) };
            var view = SortedView(list, "Alternative.Name");
            var changes = new List<string>();
            view.CollectionChanged += (_, e) => changes.Add($"{e.Action} {(e.NewItems ?? e.OldItems)?[0]} {(e.Action == NotifyCollectionChangedAction.Remove ? e.OldStartingIndex : e.NewStartingIndex)}");

            list.Add(new Item(new("c")));
            list.Insert(0, new Item(new("e")));
            list.RemoveAt(1);

            Assert.AreEqual("c,d,e", Names(view));
            CollectionAssert.AreEqual(new[] { "Add c 1", "Add e 3", "Remove b 0" }, changes);

            list.Clear();
            Assert.AreEqual(0, ((System.Collections.IList)view).Count);
            Assert.AreEqual("Reset", changes.Last().Split(' ')[0]);
        });
    }

    [TestMethod]
    public void LiveSorting_MovesARenamedItem_AndFollowsAReplacedObject()
    {
        HeadlessSession.Run(() =>
        {
            var b = new Item(new("b"));
            var list = new ObservableCollection<Item> { new(new("a")), b, new(new("c")) };
            var view = SortedView(list, "Alternative.Name");
            var moves = new List<string>();
            view.CollectionChanged += (_, e) => moves.Add($"{e.Action} {e.OldStartingIndex}->{e.NewStartingIndex}");

            b.Alternative.Name = "z";
            Assert.AreEqual("a,c,z", Names(view));
            CollectionAssert.AreEqual(new[] { "Move 1->2" }, moves);

            b.Alternative.Name = "0";
            Assert.AreEqual("0,a,c", Names(view));

            var still = SortedView(list, "Alternative.Name", live: false);
            b.Alternative.Name = "y";
            Assert.AreEqual("y,a,c", Names(still), "without live sorting the view keeps its order until it's refreshed");
            still.Refresh();
            Assert.AreEqual("a,c,y", Names(still));
        });
    }

    [TestMethod]
    public void MoveCurrentToPosition_MinusOne_LeavesNoCurrentItem()
    {
        HeadlessSession.Run(() =>
        {
            var list = new ObservableCollection<Item> { new(new("a")) };
            var view = SortedView(list, "Alternative.Name");

            Assert.IsFalse(view.MoveCurrentToPosition(-1));
            Assert.IsNull(view.CurrentItem);
            Assert.AreEqual(-1, view.CurrentPosition);
            Assert.IsTrue(view.MoveCurrentToPosition(0));
            Assert.AreSame(list[0], view.CurrentItem);
        });
    }

    [TestMethod]
    public void View_ListensWeakly_SoAnUnusedViewIsCollected()
    {
        HeadlessSession.Run(() =>
        {
            // Upstream makes a new view each time a list is loaded; the old ones mustn't live on with the items.
            var list = new ObservableCollection<Item> { new(new("a")), new(new("b")) };
            WeakReference view = MakeView(list);

            for (int i = 0; i < 3 && view.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.IsFalse(view.IsAlive);
            list.Add(new Item(new("c")));
            list[0].Alternative.Name = "d";
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference MakeView(ObservableCollection<Item> list) => new(SortedView(list, "Alternative.Name"));
}
