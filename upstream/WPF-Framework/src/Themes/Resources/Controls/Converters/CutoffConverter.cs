using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Themes
{
    /// <summary>
    /// Converts a numeric value to a boolean indicating whether it is below a cutoff threshold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This converter is used in control templates to conditionally show or hide
    /// elements based on size thresholds. It supports both <see cref="double"/>
    /// and <see cref="GridLength"/> values.
    /// </para>
    /// <para>
    ///     <b>Authors:</b>
    ///     <list type="bullet">
    ///         <item>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</item>
    ///     </list>
    /// </para>
    /// </remarks>
    public class CutoffConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the cutoff threshold value.
        /// </summary>
        /// <value>The cutoff threshold. Values below this return true.</value>
        public double Cutoff { get; set; }

        /// <summary>
        /// Converts a numeric value to a boolean indicating if it's below the cutoff.
        /// </summary>
        /// <param name="value">The value to compare. Must be a double or GridLength.</param>
        /// <param name="targetType">The target type (expected to be bool).</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// <c>true</c> if the value is less than <see cref="Cutoff"/>; otherwise, <c>false</c>.
        /// Returns <c>false</c> if the value is null or not a supported type.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            if (value is double doubleValue)
            {
                return doubleValue < Cutoff;
            }

            if (value is GridLength gridLength)
            {
                // Only compare absolute pixel values; Star and Auto lengths have no meaningful
                // pixel magnitude and should never be considered below the cutoff.
                if (!gridLength.IsAbsolute)
                    return false;
                return gridLength.Value < Cutoff;
            }

            return false;
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
