using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace WpfCompat.Tests;

/// <summary>WPF's <c>Window.Icon</c> image source, as WpfCompat's <see cref="WindowIconSource"/> provides it.</summary>
[TestClass]
public sealed class WindowIconSourceTests
{
    // A vector image, as upstream's icon resources are.
    private static DrawingImage Drawing() => new(new GeometryDrawing
    {
        Brush = Brushes.SteelBlue,
        Geometry = new RectangleGeometry(new Rect(0, 0, 16, 16)),
    });

    [TestMethod]
    public void ToWindowIcon_IsNullForNoImage()
    {
        Assert.IsNull(WindowIconSource.ToWindowIcon(null));
    }

    [TestMethod]
    public void ToWindowIcon_MakesAnIconOfABitmapOrADrawing()
    {
        HeadlessSession.Run(() =>
        {
            using var bitmap = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
            Assert.IsNotNull(WindowIconSource.ToWindowIcon(bitmap), "a bitmap is the icon as it is");
            Assert.IsNotNull(WindowIconSource.ToWindowIcon(Drawing()), "a drawing is drawn into a bitmap");
        });
    }

    [TestMethod]
    public void Icon_SetsAndClearsTheWindowsIcon()
    {
        HeadlessSession.Run(() =>
        {
            var window = new Window();
            var image = Drawing();

            WindowIconSource.SetIcon(window, image);
            Assert.AreSame(image, WindowIconSource.GetIcon(window));
            Assert.IsNotNull(window.Icon, "setting the image sets the window's icon");

            WindowIconSource.SetIcon(window, null);
            Assert.IsNull(window.Icon, "and clearing it clears the icon, as WPF's Icon = null");
        });
    }
}
