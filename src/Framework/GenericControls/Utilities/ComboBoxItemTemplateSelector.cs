using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.VisualTree;

namespace GenericControls
{
    /// <summary>
    /// A custom <c>DataTemplateSelector</c> that chooses between two templates based on  
    /// whether the ComboBox item is being rendered in the dropdown list or in the selected item display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// XGoodFit port: Avalonia has no DataTemplateSelector, and its data templates don't see the
    /// container they're built for. This is an <see cref="IDataTemplate"/>, which a combo box takes as its
    /// <c>ItemTemplate</c> (WPF's <c>ItemTemplateSelector</c>): <see cref="Build"/> makes a presenter that
    /// takes the template <see cref="SelectTemplate"/> chooses once it's in the visual tree.
    /// </para>
    /// </remarks>
    public class ComboBoxItemTemplateSelector : IDataTemplate
    {
        /// <summary>
        /// gets/sets the <see cref="DataTemplate"/> used to display items in the dropdown portion of the ComboBox.
        /// </summary>
        public DataTemplate DropDownTemplate { get; set; }

        /// <summary>
        /// gets/sets the <see cref="DataTemplate"/> used to display the selecte item in the ComboBox.
        /// </summary>
        public DataTemplate SelectedTemplate { get; set; }

        /// <summary>
        /// Returns a <see cref="DataTemplate"/> based on the container context-dropdown list or selected item.
        /// </summary>
        /// <param name="item">The data object.</param>
        /// <param name="container">The element in the visual tree that will contain the data template.</param>
        /// <returns></returns>
        public DataTemplate SelectTemplate(object item, AvaloniaObject container)
        {
            var comboBoxItem = GetVisualParent<ComboBoxItem>(container);

            if (comboBoxItem is not null)
            {
                return DropDownTemplate;
            }

            return SelectedTemplate;
        }

        /// <summary>
        /// Traverses the visual tree upward to find the first ancestor of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of ancestor to find.</typeparam>
        /// <param name="childObject">The starting child object.</param>
        /// <returns></returns>
        public static T GetVisualParent<T>(object childObject) where T : Visual
        {
            // XGoodFit port: Avalonia's visual tree is made of Visuals, and GetVisualParent is
            // VisualTreeHelper.GetParent.
            Visual child = childObject as Visual;

            while (child is not null && !(child is T))
                child = child.GetVisualParent();

            return child as T;
        }

        /// <summary>
        /// Builds the control that shows <paramref name="param"/>: a presenter that takes the template
        /// <see cref="SelectTemplate"/> chooses for it once it's in the visual tree, where it can see whether
        /// it's in a <see cref="ComboBoxItem"/>.
        /// </summary>
        /// <param name="param">The data object.</param>
        /// <returns>The presenter.</returns>
        /// <remarks>XGoodFit addition: Avalonia's <see cref="IDataTemplate"/>.</remarks>
        public Control Build(object param)
        {
            var presenter = new ContentPresenter { Content = param };
            presenter.AttachedToVisualTree += (sender, e) => presenter.ContentTemplate = SelectTemplate(param, presenter);
            return presenter;
        }

        /// <summary>
        /// Gets whether this template shows <paramref name="data"/>: it shows any item, as a selector does.
        /// </summary>
        /// <param name="data">The data object.</param>
        /// <returns>True.</returns>
        /// <remarks>XGoodFit addition: Avalonia's <see cref="IDataTemplate"/>.</remarks>
        public bool Match(object data) => true;
    }
}