using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Themes
{
    /// <summary>
    /// A multi-value converter that calculates equal-width tab sizes for a TabControl.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This converter divides the TabControl's actual width by the number of tab items
    /// to create equal-width tabs that fill the available space.
    /// </para>
    /// <para>
    ///     <b>Authors:</b>
    ///     <list type="bullet">
    ///         <item>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</item>
    ///     </list>
    /// </para>
    /// </remarks>
    public class TabSizeConverter : IMultiValueConverter
    {
        /// <summary>
        /// Calculates the width of each tab by dividing the TabControl's width by its item count.
        /// </summary>
        /// <param name="values">An array where the first element is the TabControl and the second is its ActualWidth.</param>
        /// <param name="targetType">The target type for the binding.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The calculated width for each tab, or 0 if the width would be too small.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1 || values[0] is not TabControl tabControl) return 0.0;
            if (tabControl.Items.Count == 0) return 0.0;
            double width = tabControl.ActualWidth / tabControl.Items.Count;
            if (width < 12d)
                return 0.0;
            return width - (tabControl.Items.Count + 1);
        }

        /// <summary>
        /// Not implemented. This converter only supports one-way binding.
        /// </summary>
        /// <exception cref="NotImplementedException">Always thrown.</exception>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
