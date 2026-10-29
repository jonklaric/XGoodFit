using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GenericControls
{
    /// <summary>
    /// Represents the individual components of a color (Alpha, Red, Green, Blue).
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
    /// </remarks>
    public enum ColorComponent
    {
        /// <summary>Alpha (transparency) component.</summary>
        A,
        /// <summary>Red component.</summary>
        R,
        /// <summary>Green component.</summary>
        G,
        /// <summary>Blue component.</summary>
        B
    }

    /// <summary>
    /// Converts between a <see cref="SolidColorBrush"/> and a single byte value
    /// corresponding to a specific <see cref="ColorComponent"/> (A, R, G, B).
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
    /// </remarks>
    public class ColorToByteConverter : IValueConverter
    {
        /// <summary>
        /// Gets/sets the color component (A,R,G, or B) to extract or update.
        /// </summary>
        public ColorComponent Component { get; set; } = ColorComponent.R;

        // Track the last color seen during Convert for use in ConvertBack.
        // Instance-scoped so separate converter instances (e.g., Fill vs. Stroke
        // pickers) don't overwrite each other's cached color. WPF calls Convert
        // for every component binding whenever the bound Color changes, so all
        // four A/R/G/B converters inside a single picker stay in sync.
        private SolidColorBrush _lastColor;

        /// <summary>
        /// Converts a <see cref="SolidColorBrush"/> to the byte of the selected <see cref="ColorComponent"/>
        /// </summary>
        /// <param name="value">A <see cref="SolidColorBrush"/> to convert.</param>
        /// <param name="targetType"> The expected target type (byte).</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">The culture to use in the converter (not used).</param>
        /// <returns>A byte representing the selected color component, or <c>null</c> if input is invalid.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                _lastColor = null;
                return null;
            }
            var color = value as SolidColorBrush;
            if (color is null)
                return null;
            _lastColor = color;
            switch (Component)
            {
                case ColorComponent.A:
                    return color.Color.A;
                case ColorComponent.R:
                    return color.Color.R;
                case ColorComponent.G:
                    return color.Color.G;
                case ColorComponent.B:
                    return color.Color.B;
                default:
                    return (byte)0;
            }
        }
        /// <summary>
        /// Converts a numeric value back into a <see cref="SolidColorBrush"/>, updating only the selected <see cref="ColorComponent"/>.
        /// </summary>
        /// <param name="value">A numeric value (0-255) for the selected color component.</param>
        /// <param name="targetType">The expected target type (<see cref="SolidColorBrush"/>).</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">The culture to use in the converter (not used).</param>
        /// <returns>A new <see cref="SolidColorBrush"/> with the updated color component, or a fallback transparent brush if parsing fails.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return null;
            var color = _lastColor;
            if (color is null)
                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            double doubleValue = 0d;
            byte byteValue = 0;
            _ = NumberFormatHelper.TryParseDouble(value.ToString(), out doubleValue);
            if (doubleValue >= 0d && doubleValue <= 255d)
            {
                byteValue = (byte)Math.Min(255, Math.Max(0, Math.Round(doubleValue)));
            }

            switch (Component)
            {
                case ColorComponent.A:
                    return new SolidColorBrush(Color.FromArgb(byteValue, color.Color.R, color.Color.G, color.Color.B));
                case ColorComponent.R:
                    return new SolidColorBrush(Color.FromArgb(color.Color.A, byteValue, color.Color.G, color.Color.B));
                case ColorComponent.G:
                    return new SolidColorBrush(Color.FromArgb(color.Color.A, color.Color.R, byteValue, color.Color.B));
                case ColorComponent.B:
                    return new SolidColorBrush(Color.FromArgb(color.Color.A, color.Color.R, color.Color.G, byteValue));
                default:
                    return new SolidColorBrush(Color.FromArgb(byteValue, byteValue, byteValue, byteValue));
            }
        }

    }
}
