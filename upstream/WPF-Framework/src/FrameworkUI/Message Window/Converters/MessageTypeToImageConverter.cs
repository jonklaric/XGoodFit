using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FrameworkInterfaces;

namespace FrameworkUI.MessageWindow.Converters
{
    /// <summary>
    /// Converts a MessageType enum value to an ImageSource for display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MessageTypeToImageConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the error message image source.
        /// </summary>
        public ImageSource? ErrorImageSource { get; set; }

        /// <summary>
        /// Gets or sets the message image source.
        /// </summary>
        public ImageSource? MessageImageSource { get; set; }

        /// <summary>
        /// Gets or sets the warning message image source.
        /// </summary>
        public ImageSource? WarningImageSource { get; set; }

        /// <summary>
        /// Gets or sets the event message image source.
        /// </summary>
        public ImageSource? EventImageSource { get; set; }

        /// <summary>
        /// Converts a MessageType value to the corresponding ImageSource.
        /// </summary>
        /// <param name="value">The MessageType enum value.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">Optional parameter.</param>
        /// <param name="culture">The culture info.</param>
        /// <returns>The ImageSource corresponding to the message type, or null if value is null.</returns>
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            switch ((MessageType) value)
            {
                case MessageType.Error: return ErrorImageSource;
                case MessageType.Warning: return WarningImageSource;
                case MessageType.Message: return MessageImageSource;
                case MessageType.Event: return EventImageSource;
                default: throw new ArgumentOutOfRangeException("value");
            }
        }

        /// <summary>
        /// Converts back from ImageSource to MessageType (not implemented).
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">Optional parameter.</param>
        /// <param name="culture">The culture info.</param>
        /// <returns>Throws NotImplementedException.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
