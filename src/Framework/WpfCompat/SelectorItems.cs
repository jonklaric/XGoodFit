using System.Collections;
using Avalonia;
using Avalonia.Controls.Primitives;

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
/// So the selector has no data context, which is its selection binding's source, while the items change:
/// the binding writes nothing. Given its data context back (its own, or the inherited one), the binding
/// shows the value again, which selects the matching item or none, keeping the value, as WPF's selector
/// does. (Holding the value above the binding instead, at animation priority, left it pinned there once
/// the items changed, so the selector stopped following its source.) A selector showing a WpfCompat
/// <see cref="Data.ListCollectionView"/> also keeps its selection when live sorting moves the selected
/// item, as WPF's does (see that view).
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
            bool isOwn = selector.IsSet(StyledElement.DataContextProperty);
            object? dataContext = selector.DataContext;
            selector.DataContext = null;
            try
            {
                selector.ItemsSource = items;
            }
            finally
            {
                if (isOwn)
                    selector.DataContext = dataContext;
                else
                    selector.ClearValue(StyledElement.DataContextProperty);
            }
        }
    }
}
