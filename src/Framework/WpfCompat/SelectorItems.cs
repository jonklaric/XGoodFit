using System.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace WpfCompat;

/// <summary>
/// WPF's <c>selector.ItemsSource = items</c>: the selector keeps its <c>SelectedValue</c> and selects the item
/// that matches it, or none.
/// </summary>
/// <remarks>
/// <para>
/// XGoodFit addition. Avalonia's selectors clear <c>SelectedValue</c> when their items are replaced, so a
/// two-way binding writes null back to its source, and then the old value again if the new items have it.
/// WPF's write nothing. Upstream's properties panels give their element combo boxes their items when the
/// combo boxes load, after the bindings have set the selected elements: in Avalonia, the model's setter
/// would run with null, and InputData's, for one, clears its processed data.
/// </para>
/// <para>
/// This holds the value above the binding (at animation priority) while the items change, then sets it
/// again, which selects the matching item or leaves the value with none selected, as WPF does. The binding
/// sees no change, so nothing is written back. A selector showing a <see cref="Data.SortedCollectionView"/>
/// keeps its selection when live sorting moves the selected item, as WPF's does: Avalonia's selectors drop
/// a moved item's selection, so the view announces the moves of the items it passes instead.
/// </para>
/// </remarks>
public static class SelectorItems
{
    extension(SelectingItemsControl selector)
    {
        /// <summary>Gives the selector new items as WPF does, keeping its selected value (see <see cref="SelectorItems"/>).</summary>
        /// <param name="items">The new items.</param>
        public void SetItemsSourceKeepingSelection(IEnumerable? items)
        {
            object? value = selector.SelectedValue;
            IDisposable? held = selector.SetValue(SelectingItemsControl.SelectedValueProperty, value, BindingPriority.Animation);
            try
            {
                selector.ItemsSource = items;
                selector.SetCurrentValue(SelectingItemsControl.SelectedValueProperty, value);
            }
            finally
            {
                held?.Dispose();
            }
            if (items is Data.SortedCollectionView view)
            {
                view.SelectedItem = () => selector.ItemsSource == view ? selector.SelectedItem : null;
            }
        }
    }
}
