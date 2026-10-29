using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace GenericControls
{
    /// <summary>
    /// A value converter that translates a boolean value to a <see cref="Visibility"/> enumeration.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
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
    /// This converter is commonly used in WPF data bindings to show or hide UI elements
    /// based on boolean properties in a view model.
    /// </para>
    /// </remarks>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Gets/sets the <see cref="Visibility"/> value to return when the bound value is <c>true</c>
        /// </summary>
        public Visibility TrueValue { get; set; }

        /// <summary>
        /// Gets/sets the <see cref="Visibility"/> value to return when the bound value is <c>false</c>
        /// </summary>
        public Visibility FalseValue { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanToVisibilityConverter"/> class.
        /// </summary>
        public BooleanToVisibilityConverter()
        {
            // set defaults
            TrueValue = Visibility.Visible;
            FalseValue = Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a boolean value to a <see cref="Visibility"/> value.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The target binding type (should be <see cref="Visibility"/>).</param>
        /// <param name="parameter">An optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter (not used).</param>
        /// <returns>The <see cref="Visibility"/> value corresponding to <see cref="TrueValue"/> or <see cref="FalseValue"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return FalseValue;
            if (value.GetType() != typeof(bool))
                return FalseValue;
            return (bool)value ? TrueValue : FalseValue;
        }

        /// <summary>
        /// Converts a <see cref="Visibility"/> value back to a boolean.
        /// </summary>
        /// <param name="value">The <see cref="Visibility"/> value to convert back.</param>
        /// <param name="targetType">The target type (should be <see cref="bool"/>).</param>
        /// <param name="parameter">An optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter (not used).</param>
        /// <returns>A boolean value: <c>true</c> if <paramref name="value"/> equals <see cref="TrueValue"/>, otherwise <c>false</c>.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
            if (value.GetType() != typeof(Visibility))
                return false;
            return (Visibility)value == TrueValue ? true : false;
        }
    }
}
