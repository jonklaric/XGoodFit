using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using WpfCompat;

namespace GenericControls
{
    /// <summary>
    /// WPF's <c>ElementStyle</c> and <c>EditingElementStyle</c> for ProDataGrid's columns: the theme of
    /// the element a column shows in its cells, and of the editor it shows while a cell is edited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// XGoodFit addition. WPF's DataGridTextColumn and DataGridCheckBoxColumn style the elements they
    /// create with these properties, and upstream's grids set them for their columns. ProDataGrid's
    /// columns give their elements the themes found under fixed resource keys (Themes' DataGrid.axaml
    /// defines them with WPF's defaults), so these attached properties carry a column's own themes,
    /// and a cell's element takes its column's theme when the cell shows it.
    /// </para>
    /// <para>
    /// As in WPF, a text or check box column's styles default to the column's default ones
    /// (WPF's <c>DefaultElementStyle</c> and <c>DefaultEditingElementStyle</c>): here the themes
    /// <c>DataGridCellTextBlockTheme</c>, <c>DataGridCellTextBoxTheme</c> and
    /// <c>DataGridCellCheckBoxTheme</c>.
    /// </para>
    /// </remarks>
    public static class DataGridColumnStyles
    {
        /// <summary>The theme of the element the column shows in a cell: WPF's <c>ElementStyle</c>.</summary>
        public static readonly AttachedProperty<ControlTheme> ElementStyleProperty =
            AvaloniaProperty.RegisterAttached<DataGridColumn, ControlTheme>("ElementStyle", typeof(DataGridColumnStyles));

        /// <summary>The theme of the column's editor: WPF's <c>EditingElementStyle</c>.</summary>
        public static readonly AttachedProperty<ControlTheme> EditingElementStyleProperty =
            AvaloniaProperty.RegisterAttached<DataGridColumn, ControlTheme>("EditingElementStyle", typeof(DataGridColumnStyles));

        static DataGridColumnStyles()
        {
            // A cell's content is the element its column created for it, or the column's editor while
            // the cell is edited (ProDataGrid marks the cell :edited first).
            ContentControl.ContentProperty.Changed.AddClassHandler<DataGridCell>((cell, _) => Apply(cell));
        }

        /// <summary>The theme of the element <paramref name="column"/> shows in a cell, or its default.</summary>
        /// <param name="column">The column.</param>
        public static ControlTheme GetElementStyle(DataGridColumn column) =>
            column.GetValue(ElementStyleProperty) ?? DefaultTheme(column switch
            {
                DataGridTextColumn => "DataGridCellTextBlockTheme",
                DataGridCheckBoxColumn => "DataGridCellCheckBoxTheme",
                _ => null,
            });

        /// <summary>Sets the theme of the element <paramref name="column"/> shows in a cell.</summary>
        /// <param name="column">The column.</param>
        /// <param name="value">The theme.</param>
        public static void SetElementStyle(DataGridColumn column, ControlTheme value) => column.SetValue(ElementStyleProperty, value);

        /// <summary>The theme of <paramref name="column"/>'s editor, or its default.</summary>
        /// <param name="column">The column.</param>
        public static ControlTheme GetEditingElementStyle(DataGridColumn column) =>
            column.GetValue(EditingElementStyleProperty) ?? DefaultTheme(column switch
            {
                DataGridTextColumn => "DataGridCellTextBoxTheme",
                DataGridCheckBoxColumn => "DataGridCellCheckBoxTheme",
                _ => null,
            });

        /// <summary>Sets the theme of <paramref name="column"/>'s editor.</summary>
        /// <param name="column">The column.</param>
        /// <param name="value">The theme.</param>
        public static void SetEditingElementStyle(DataGridColumn column, ControlTheme value) => column.SetValue(EditingElementStyleProperty, value);

        /// <summary>
        /// Gives <paramref name="cell"/>'s element the theme its column sets for it, if the column sets one.
        /// </summary>
        /// <param name="cell">The cell.</param>
        internal static void Apply(DataGridCell cell)
        {
            if (cell.OwningColumn is not { } column || cell.Content is not Control element)
            {
                return;
            }
            ControlTheme theme = cell.Classes.Contains(":edited")
                ? column.GetValue(EditingElementStyleProperty)
                : column.GetValue(ElementStyleProperty);
            if (theme != null && (theme.TargetType?.IsInstanceOfType(element) ?? true) && !ReferenceEquals(element.Theme, theme))
            {
                element.Theme = theme;
            }
        }

        private static ControlTheme DefaultTheme(string key) =>
            key == null ? null : Application.Current?.TryFindResource(key) as ControlTheme;
    }
}
