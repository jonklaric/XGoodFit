using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using WpfCompat.Data;

namespace WpfCompat.Tests;

/// <summary>
/// <see cref="SelectorItems"/>' SetItemsSourceKeepingSelection: a combo box whose selection is bound two-way gets
/// new items, as upstream's properties panels give their element combo boxes theirs when they load, without
/// writing anything back, and keeps its selection as the items change.
/// </summary>
[TestClass]
public sealed class SelectorItemsTests
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected)));
            }
        }
    }

    // Upstream's ComboBox_Loaded: a live-sorted view by Name, with no current item.
    private static ICollectionView ViewOf(ObservableCollection<Named> items)
    {
        var csv = new CollectionViewSource { Source = items, IsLiveSortingRequested = true };
        csv.SortDescriptions.Add(new SortDescription(nameof(Named.Name), ListSortDirection.Ascending));
        ICollectionView view = csv.View!;
        view.MoveCurrentToPosition(-1);
        return view;
    }

    private static string Names(IEnumerable? items) => string.Join(",", items!.Cast<Named>().Select(n => n.Name));

    // A combo box whose SelectedValue is bound two-way to the holder's Selected, as upstream's XAML binds it,
    // with its own data context or one it inherits.
    private static (ComboBox Combo, Window Window) BoundComboBox(Holder holder, bool ownDataContext)
    {
        var combo = new ComboBox();
        if (ownDataContext) combo.DataContext = holder;
        combo.Bind(Avalonia.Controls.Primitives.SelectingItemsControl.SelectedValueProperty,
            new Binding(nameof(Holder.Selected)) { Mode = BindingMode.TwoWay });
        var window = new Window { Content = combo };
        if (!ownDataContext) window.DataContext = holder;
        window.Show();
        holder.Log.Clear();
        return (combo, window);
    }

    [TestMethod]
    public void ComboBox_ShowingTheView_KeepsItsBoundSelectionThroughAddsRenamesAndSourceChanges()
    {
        HeadlessSession.Run(() =>
        {
            foreach (bool ownDataContext in new[] { true, false })
            {
                var b = new Named("b");
                var items = new ObservableCollection<Named> { new Named("c"), b, new Named("a") };
                var holder = new Holder { Selected = b };
                var (combo, window) = BoundComboBox(holder, ownDataContext);
                try
                {
                    // As upstream: the items arrive when the combo box has loaded, after its selection's binding.
                    combo.SetItemsSourceKeepingSelection(ViewOf(items));
                    Assert.AreSame(b, combo.SelectedItem);
                    Assert.AreEqual(ownDataContext, combo.IsSet(Avalonia.StyledElement.DataContextProperty), "the data context is as it was");

                    items.Add(new Named("0"));
                    Assert.AreSame(b, combo.SelectedItem, "an item added before the selection");

                    items[0].Name = "z";
                    Assert.AreEqual("0,a,b,z", Names(combo.ItemsSource));
                    Assert.AreSame(b, combo.SelectedItem, "another item moved past the selection");

                    b.Name = "zz";
                    Assert.AreEqual("0,a,z,zz", Names(combo.ItemsSource));
                    Assert.AreSame(b, combo.SelectedItem, "the selected item moved");
                    Assert.AreSame(b, holder.Selected);
                    Assert.AreEqual("", string.Join(";", holder.Log), "showing and following the list writes nothing back");

                    // The selection still follows its source (an undo, say): nothing holds the old value.
                    Named a = items.Single(n => n.Name == "a");
                    holder.Selected = a;
                    Assert.AreSame(a, combo.SelectedItem, "the selection follows its source");
                }
                finally
                {
                    window.Close();
                }
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
                var (combo, window) = BoundComboBox(holder, ownDataContext: true);
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
}
