using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace GenericControls
{
    /// <summary>
    /// Measures a grid's row of column headers again when a column header's width changes, as WPF does: the
    /// row is as high as its headers are at their columns' widths.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition. The themes' column headers wrap their text. ProDataGrid measures the headers before
    /// it sizes star columns and doesn't measure them again when the columns' widths change, so a grid first
    /// laid out narrow keeps the row of headers it had then: a grid whose width binds to its container's (the
    /// distribution selector's statistics) showed headers one letter wide, a row of them 200 pixels high.
    /// </remarks>
    internal static class DataGridColumnHeaderHeight
    {
        static DataGridColumnHeaderHeight()
        {
            Visual.BoundsProperty.Changed.AddClassHandler<DataGridColumnHeader>((header, e) =>
            {
                if (e.GetOldValue<Rect>().Width != e.GetNewValue<Rect>().Width
                    && header.GetVisualParent() is DataGridColumnHeadersPresenter presenter)
                {
                    presenter.InvalidateMeasure();
                }
            });
        }

        /// <summary>Makes sure the grids' headers are handled (runs the static constructor).</summary>
        internal static void Register()
        {
        }
    }
}
