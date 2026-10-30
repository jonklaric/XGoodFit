using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Themes
{
    /// <summary>
    /// Converts a window's icon to an image, so a window template can show it in its title bar.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition: WPF's <c>Window.Icon</c> is an image source that a title bar's
    /// <c>Image</c> shows directly. Avalonia's <see cref="WindowIcon"/> is for the operating system
    /// only, so this decodes it into a bitmap, once per icon.
    /// </remarks>
    public sealed class WindowIconImageConverter : IValueConverter
    {
        private static readonly ConditionalWeakTable<WindowIcon, Bitmap> Bitmaps = new();

        /// <summary>The shared instance.</summary>
        public static WindowIconImageConverter Instance { get; } = new();

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not WindowIcon icon)
            {
                return null;
            }
            return Bitmaps.GetValue(icon, static icon =>
            {
                using var stream = new MemoryStream();
                icon.Save(stream);
                stream.Position = 0;
                return new Bitmap(stream);
            });
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
