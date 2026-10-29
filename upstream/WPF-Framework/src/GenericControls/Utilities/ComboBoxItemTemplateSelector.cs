using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GenericControls
{
    /// <summary>
    /// A custom <see cref="DataTemplateSelector"/> that chooses between two templates based on 
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
    /// </remarks>
    public class ComboBoxItemTemplateSelector : DataTemplateSelector
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
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
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
            DependencyObject child = childObject as DependencyObject;

            while (child is not null && !(child is T))
                child = VisualTreeHelper.GetParent(child);

            return child as T;
        }
    }
}