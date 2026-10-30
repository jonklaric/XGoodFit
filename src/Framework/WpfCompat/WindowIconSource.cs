using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace WpfCompat;

/// <summary>
/// WPF's <c>Window.Icon</c>, an image source such as a vector drawing, for Avalonia windows, whose
/// <see cref="Window.Icon"/> is an operating-system icon made from a bitmap (<see cref="WindowIcon"/>):
/// <c>wpf:WindowIconSource.Icon="{DynamicResource SaveImage}"</c> in XAML, or
/// <c>window.Icon = WindowIconSource.ToWindowIcon(image)</c> in code.
/// </summary>
/// <remarks>
/// XGoodFit addition. Upstream's windows take their icons from image resources, mostly drawings.
/// A bitmap becomes the window icon as it is; any other image is drawn into a 32-pixel bitmap at twice
/// the normal resolution, a 16-unit icon as sharp as the title bar shows it on a high-resolution screen.
/// </remarks>
public static class WindowIconSource
{
    /// <summary>The size, in device-independent units, at which an image is drawn as an icon.</summary>
    public const double IconSize = 16d;

    /// <summary>The window's icon as an image, as WPF's <c>Window.Icon</c>. Setting it sets the window's <see cref="Window.Icon"/>.</summary>
    public static readonly AttachedProperty<IImage?> IconProperty =
        AvaloniaProperty.RegisterAttached<Window, IImage?>("Icon", typeof(WindowIconSource));

    static WindowIconSource()
    {
        IconProperty.Changed.AddClassHandler<Window>((window, e) => window.Icon = ToWindowIcon(e.GetNewValue<IImage?>()));
    }

    /// <summary>Gets <paramref name="window"/>'s icon image.</summary>
    public static IImage? GetIcon(Window window) => window.GetValue(IconProperty);

    /// <summary>Sets <paramref name="window"/>'s icon image, and so its <see cref="Window.Icon"/>.</summary>
    public static void SetIcon(Window window, IImage? value) => window.SetValue(IconProperty, value);

    /// <summary>
    /// The window icon of <paramref name="image"/>: a bitmap as it is, any other image drawn into a
    /// bitmap. Null for no image, as WPF's window then has no icon of its own.
    /// </summary>
    public static WindowIcon? ToWindowIcon(IImage? image)
    {
        switch (image)
        {
            case null:
                return null;
            case Bitmap bitmap:
                return new WindowIcon(bitmap);
            default:
                const double scale = 2d;
                var size = new PixelSize((int)(IconSize * scale), (int)(IconSize * scale));
                var bitmapOfImage = new RenderTargetBitmap(size, new Vector(96d * scale, 96d * scale));
                using (DrawingContext context = bitmapOfImage.CreateDrawingContext())
                {
                    image.Draw(context, new Rect(image.Size), new Rect(0d, 0d, IconSize, IconSize));
                }
                return new WindowIcon(bitmapOfImage);
        }
    }
}
