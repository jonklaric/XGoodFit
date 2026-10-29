using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Themes
{
    /// <summary>
    /// Converts a TreeViewItem to a Thickness representing the indentation
    /// based on the item's depth in the tree hierarchy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This converter is used in TreeView control templates to calculate
    /// the left margin for tree items based on their nesting level.
    /// </para>
    /// <para>
    ///     <b>Authors:</b>
    ///     <list type="bullet">
    ///         <item>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</item>
    ///     </list>
    /// </para>
    /// </remarks>
    public class TreeViewIndentConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the indentation amount per level in pixels.
        /// </summary>
        /// <value>The indentation amount. Default is 0.</value>
        public double Indent { get; set; }

        /// <summary>
        /// Gets the depth of a TreeViewItem in the tree hierarchy.
        /// </summary>
        /// <param name="item">The TreeViewItem to get the depth for.</param>
        /// <returns>The zero-based depth of the item.</returns>
        public int GetItemDepth(TreeViewItem item)
        {
            int depth = 0;
            TreeViewItem current = item;
            while (true)
            {
                var parent = GetParent(current);
                if (parent == null)
                    break;
                depth++;
                current = parent;
            }
            return depth;
        }

        /// <summary>
        /// Gets the parent TreeViewItem of the specified item.
        /// </summary>
        /// <param name="item">The TreeViewItem to get the parent for.</param>
        /// <returns>The parent TreeViewItem, or null if the item is at the root level.</returns>
        private TreeViewItem GetParent(TreeViewItem item)
        {
            var parent = VisualTreeHelper.GetParent(item);
            while (parent != null && !(parent is TreeViewItem || parent is TreeView))
                parent = VisualTreeHelper.GetParent(parent);
            return parent as TreeViewItem;
        }

        /// <summary>
        /// Converts a TreeViewItem to a Thickness representing left indentation.
        /// </summary>
        /// <param name="value">The TreeViewItem to convert.</param>
        /// <param name="targetType">The target type (expected to be Thickness).</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A Thickness with left margin based on item depth, or zero thickness if value is null.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TreeViewItem item = value as TreeViewItem;
            if (item == null) return new Thickness(0d);
            return new Thickness(Indent * GetItemDepth(item), 0d, 0d, 0d);
        }

        /// <summary>
        /// Not implemented. This converter only supports one-way binding.
        /// </summary>
        /// <exception cref="NotImplementedException">Always thrown.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
