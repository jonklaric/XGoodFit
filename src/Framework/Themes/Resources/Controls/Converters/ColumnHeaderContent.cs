using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Themes
{
    /// <summary>
    /// The content template a column header's style gives it: WPF's <c>ContentTemplate</c> setter in a
    /// <c>DataGridColumnHeader</c> style.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition. WPF's header shows its column's HeaderTemplate when the column has one, and
    /// otherwise the ContentTemplate its style sets. ProDataGrid binds the header's ContentTemplate to its
    /// column's HeaderTemplate even when that is null, which hides a style's ContentTemplate. So the
    /// ported header styles set <see cref="TemplateProperty"/> instead, and their templates show
    /// <see cref="EffectiveTemplateProperty"/>: the column's template if there is one, and the style's
    /// otherwise.
    /// </remarks>
    public static class ColumnHeaderContent
    {
        /// <summary>The content template the header's style sets.</summary>
        public static readonly AttachedProperty<IDataTemplate> TemplateProperty =
            AvaloniaProperty.RegisterAttached<DataGridColumnHeader, IDataTemplate>("Template", typeof(ColumnHeaderContent));

        /// <summary>The content template the header shows: its column's, or else its style's.</summary>
        public static readonly AttachedProperty<IDataTemplate> EffectiveTemplateProperty =
            AvaloniaProperty.RegisterAttached<DataGridColumnHeader, IDataTemplate>("EffectiveTemplate", typeof(ColumnHeaderContent));

        static ColumnHeaderContent()
        {
            TemplateProperty.Changed.AddClassHandler<DataGridColumnHeader>((header, _) => Update(header));
            ContentControl.ContentTemplateProperty.Changed.AddClassHandler<DataGridColumnHeader>((header, _) => Update(header));
        }

        /// <summary>Gets the content template <paramref name="header"/>'s style sets.</summary>
        public static IDataTemplate GetTemplate(DataGridColumnHeader header) => header.GetValue(TemplateProperty);

        /// <summary>Sets the content template <paramref name="header"/>'s style sets.</summary>
        public static void SetTemplate(DataGridColumnHeader header, IDataTemplate value) => header.SetValue(TemplateProperty, value);

        /// <summary>Gets the content template <paramref name="header"/> shows.</summary>
        public static IDataTemplate GetEffectiveTemplate(DataGridColumnHeader header) => header.GetValue(EffectiveTemplateProperty);

        private static void Update(DataGridColumnHeader header) =>
            header.SetValue(EffectiveTemplateProperty, header.ContentTemplate ?? header.GetValue(TemplateProperty));
    }
}
