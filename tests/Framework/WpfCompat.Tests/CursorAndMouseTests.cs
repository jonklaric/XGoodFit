using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using WpfCompat.Input;

namespace WpfCompat.Tests;

/// <summary>WPF's cursors from cursor files (<c>new Cursor(stream)</c>) and global mouse buttons (<c>Mouse.LeftButton</c>).</summary>
[TestClass]
public sealed class CursorAndMouseTests
{
    // A cursor file with one image: a BITMAPINFOHEADER image of the given depth, its palette, its colour
    // rows (bottom first) and its AND mask.
    private static byte[] CursorFile(int width, int height, int bits, int hotX, int hotY, byte[] palette, byte[] colours, byte[] mask)
    {
        var image = new MemoryStream();
        var header = new BinaryWriter(image);
        header.Write(40);
        header.Write(width);
        header.Write(height * 2);
        header.Write((short)1);
        header.Write((short)bits);
        header.Write(0);
        header.Write(colours.Length + mask.Length);
        header.Write(0);
        header.Write(0);
        header.Write(0);
        header.Write(0);
        header.Write(palette);
        header.Write(colours);
        header.Write(mask);

        var file = new MemoryStream();
        var writer = new BinaryWriter(file);
        writer.Write((short)0);
        writer.Write((short)2);
        writer.Write((short)1);
        writer.Write((byte)width);
        writer.Write((byte)height);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((short)hotX);
        writer.Write((short)hotY);
        writer.Write((int)image.Length);
        writer.Write(22);
        writer.Write(image.ToArray());
        return file.ToArray();
    }

    [TestMethod]
    public void A32BitCursorFile_GivesItsImageAndHotspot()
    {
        HeadlessSession.Run(() =>
        {
            // 2 x 2: the bottom row is red (opaque) and blue (half), the top row clear and green.
            byte[] colours = [0, 0, 255, 255, 255, 0, 0, 128, 0, 0, 0, 0, 0, 255, 0, 255];
            byte[] mask = new byte[8];
            byte[] file = CursorFile(2, 2, 32, 1, 0, [], colours, mask);

            Assert.IsTrue(WpfCursor.TryDecode(file, out PixelSize size, out byte[]? pixels, out PixelPoint hotSpot));
            Assert.AreEqual(new PixelPoint(1, 0), hotSpot);
            Assert.AreEqual(new PixelSize(2, 2), size);
            Assert.AreEqual(0, pixels![3], "the top left is clear");
            Assert.AreEqual(255, pixels[5], "the top right is green");
            Assert.AreEqual(255, pixels[10], "the bottom left is red");
            Assert.AreEqual(128, pixels[15], "the bottom right keeps its alpha");

            Assert.IsTrue(WpfCursor.TryRead(file, out Bitmap? bitmap, out _));
            Assert.AreEqual(new PixelSize(2, 2), bitmap!.PixelSize);
            Assert.IsNotNull(WpfCursor.FromStream(new MemoryStream(file)));
        });
    }

    [TestMethod]
    public void AMonochromeCursorFile_TakesItsTransparencyFromTheMask()
    {
        HeadlessSession.Run(() =>
        {
            // 8 x 2, black and white: every pixel white, the mask clears the left half.
            byte[] palette = [0, 0, 0, 0, 255, 255, 255, 0];
            byte[] colours = [0xFF, 0, 0, 0, 0xFF, 0, 0, 0];
            byte[] mask = [0xF0, 0, 0, 0, 0xF0, 0, 0, 0];
            byte[] file = CursorFile(8, 2, 1, 7, 1, palette, colours, mask);

            Assert.IsTrue(WpfCursor.TryDecode(file, out _, out byte[]? pixels, out PixelPoint hotSpot));
            Assert.AreEqual(new PixelPoint(7, 1), hotSpot);
            Assert.AreEqual(0, pixels![3], "masked: transparent");
            Assert.AreEqual(255, pixels[(4 * 4) + 3], "unmasked: opaque");
            Assert.AreEqual(255, pixels[4 * 4], "and white");
        });
    }

    [TestMethod]
    public void DataThatIsntACursorFile_GivesNoCursor()
    {
        HeadlessSession.Run(() =>
        {
            Assert.IsNull(WpfCursor.FromStream(new MemoryStream([1, 2, 3, 4, 5, 6, 7])));
            Assert.IsFalse(WpfCursor.TryRead([0, 0, 2, 0, 1, 0], out _, out _), "a directory without its entry");
        });
    }

    [TestMethod]
    public void MouseButtons_FollowThePointer_IncludingTheEventInHand()
    {
        HeadlessSession.Run(() =>
        {
            Mouse.Initialize();
            var border = new Border { Width = 100, Height = 100, Background = Brushes.White };
            var window = new Window { Width = 100, Height = 100, Content = border };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var seen = new List<string>();
            border.PointerPressed += (_, _) => seen.Add($"down {Mouse.LeftButton} {Mouse.MiddleButton} {Mouse.RightButton}");
            border.PointerReleased += (_, _) => seen.Add($"up {Mouse.LeftButton} {Mouse.MiddleButton} {Mouse.RightButton}");

            var center = new Point(50, 50);
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
            window.MouseDown(center, MouseButton.Middle);
            window.MouseUp(center, MouseButton.Middle);

            CollectionAssert.AreEqual(
                new[] { "down Pressed Released Released", "up Released Released Released", "down Released Pressed Released", "up Released Released Released" },
                seen);
            window.Close();
        });
    }
}
