using System.Buffers.Binary;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace WpfCompat.Input;

/// <summary>
/// WPF's <c>new Cursor(Stream)</c>: a cursor from a Windows cursor file (.cur), as an Avalonia cursor
/// made from the file's image and hotspot.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia makes custom cursors from a bitmap and a hotspot, on every platform, but can't read cursor
/// files. This reads them: a .cur file is an icon directory whose entries hold each image's hotspot, and
/// each image is a PNG or a device-independent bitmap with an AND mask. Like Windows, it takes the image
/// nearest the standard 32 × 32 cursor size, the deepest one where there's a choice. A 32-bit image
/// keeps its alpha channel; images with fewer bits, and 32-bit ones whose alpha is all zero, take their
/// transparency from the AND mask. Pixels that Windows draws by inverting the screen (mask set over a
/// colour) have no bitmap equivalent and come out transparent.
/// </para>
/// <para>
/// XGoodFit addition: the shim behind ported <c>new Cursor(stream)</c> calls. Animated cursors (.ani)
/// aren't read.
/// </para>
/// </remarks>
public static class WpfCursor
{
    private const int StandardCursorSize = 32;

    /// <summary>
    /// Reads a cursor file. Returns null when <paramref name="stream"/> isn't a cursor file this can read,
    /// so the caller can choose the nearest standard cursor instead.
    /// </summary>
    public static Cursor? FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return TryRead(buffer.ToArray(), out Bitmap? bitmap, out PixelPoint hotSpot)
            ? new Cursor(bitmap!, hotSpot)
            : null;
    }

    /// <summary>
    /// Reads a cursor file's image and hotspot: the image Windows would show for a standard-size cursor.
    /// Returns false when <paramref name="data"/> isn't a cursor file this can read.
    /// </summary>
    public static bool TryRead(byte[] data, out Bitmap? bitmap, out PixelPoint hotSpot)
    {
        ArgumentNullException.ThrowIfNull(data);
        bitmap = null;
        if (!TryFindImage(data, out int offset, out int size, out hotSpot))
        {
            return false;
        }
        try
        {
            if (IsPng(data.AsSpan(offset, size)))
            {
                bitmap = new Bitmap(new MemoryStream(data, offset, size));
            }
            else if (ReadDib(data.AsSpan(offset, size), out PixelSize pixelSize, out byte[]? pixels))
            {
                bitmap = ToBitmap(pixelSize, pixels!);
            }
            else
            {
                return false;
            }
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException)
        {
            bitmap = null;
            return false;
        }
        hotSpot = new PixelPoint(
            Math.Clamp(hotSpot.X, 0, bitmap.PixelSize.Width - 1),
            Math.Clamp(hotSpot.Y, 0, bitmap.PixelSize.Height - 1));
        return true;
    }

    /// <summary>
    /// Decodes the image <see cref="TryRead"/> would take, when it's a bitmap (not a PNG): its size, its
    /// pixels as unpremultiplied BGRA rows from the top, and its hotspot.
    /// </summary>
    public static bool TryDecode(byte[] data, out PixelSize size, out byte[]? pixels, out PixelPoint hotSpot)
    {
        ArgumentNullException.ThrowIfNull(data);
        size = default;
        pixels = null;
        return TryFindImage(data, out int offset, out int length, out hotSpot)
            && !IsPng(data.AsSpan(offset, length))
            && ReadDib(data.AsSpan(offset, length), out size, out pixels);
    }

    // The directory entry nearest the standard size, the deepest where there's a choice.
    private static bool TryFindImage(byte[] data, out int offset, out int size, out PixelPoint hotSpot)
    {
        offset = 0;
        size = 0;
        hotSpot = default;
        ReadOnlySpan<byte> file = data;
        // ICONDIR: reserved (0), type (2 for cursors, 1 for icons, whose hotspot is the corner), count.
        if (file.Length < 6 || ReadUInt16(file, 0) != 0)
        {
            return false;
        }
        int type = ReadUInt16(file, 2);
        int count = ReadUInt16(file, 4);
        if ((type != 1 && type != 2) || count == 0 || file.Length < 6 + (16 * count))
        {
            return false;
        }

        int best = -1;
        int bestScore = int.MaxValue;
        for (int i = 0; i < count; i++)
        {
            // ICONDIRENTRY: width, height (0 means 256), colours, reserved, hotspot x, y (planes and bit
            // count for icons), size, offset.
            int entry = 6 + (16 * i);
            int width = file[entry] == 0 ? 256 : file[entry];
            int bits = ReadBitCount(file, entry);
            int score = (Math.Abs(width - StandardCursorSize) * 64) - bits;
            if (score < bestScore)
            {
                bestScore = score;
                best = entry;
            }
        }

        size = (int)ReadUInt32(file, best + 8);
        offset = (int)ReadUInt32(file, best + 12);
        if (offset < 0 || size <= 0 || (long)offset + size > file.Length)
        {
            return false;
        }
        hotSpot = type == 2 ? new PixelPoint(ReadUInt16(file, best + 4), ReadUInt16(file, best + 6)) : default;
        return true;
    }

    // A cursor entry's bit depth is in its image's header; an icon entry's is in the entry.
    private static int ReadBitCount(ReadOnlySpan<byte> file, int entry)
    {
        int offset = (int)ReadUInt32(file, entry + 12);
        if (offset >= 0 && offset + 16 <= file.Length && ReadUInt32(file, offset) == 40)
        {
            return ReadUInt16(file, offset + 14);
        }
        return 32;
    }

    private static bool IsPng(ReadOnlySpan<byte> image) =>
        image.Length >= 8 && image[0] == 0x89 && image[1] == (byte)'P' && image[2] == (byte)'N' && image[3] == (byte)'G';

    // A BITMAPINFOHEADER image: its height covers the colour (XOR) bitmap and the AND mask below it,
    // both stored bottom row first, each row padded to four bytes.
    private static bool ReadDib(ReadOnlySpan<byte> image, out PixelSize size, out byte[]? pixels)
    {
        size = default;
        pixels = null;
        if (image.Length < 40 || ReadUInt32(image, 0) != 40)
        {
            return false;
        }
        int width = (int)ReadUInt32(image, 4);
        int height = (int)ReadUInt32(image, 8) / 2;
        int bits = ReadUInt16(image, 14);
        int compression = (int)ReadUInt32(image, 16);
        int colorsUsed = (int)ReadUInt32(image, 32);
        if (width <= 0 || height <= 0 || compression != 0 || bits is not (1 or 4 or 8 or 24 or 32))
        {
            return false;
        }

        int paletteSize = bits <= 8 ? (colorsUsed == 0 ? 1 << bits : colorsUsed) : 0;
        int palette = 40;
        int xorStride = ((width * bits) + 31) / 32 * 4;
        int andStride = (width + 31) / 32 * 4;
        int xor = palette + (paletteSize * 4);
        int and = xor + (xorStride * height);
        bool hasMask = and + (andStride * height) <= image.Length;
        if (and > image.Length)
        {
            return false;
        }

        pixels = new byte[width * height * 4];
        bool anyAlpha = false;
        for (int y = 0; y < height; y++)
        {
            int xorRow = xor + ((height - 1 - y) * xorStride);
            for (int x = 0; x < width; x++)
            {
                byte b, g, r, a = 255;
                switch (bits)
                {
                    case 32:
                        b = image[xorRow + (x * 4)];
                        g = image[xorRow + (x * 4) + 1];
                        r = image[xorRow + (x * 4) + 2];
                        a = image[xorRow + (x * 4) + 3];
                        anyAlpha |= a != 0;
                        break;
                    case 24:
                        b = image[xorRow + (x * 3)];
                        g = image[xorRow + (x * 3) + 1];
                        r = image[xorRow + (x * 3) + 2];
                        break;
                    default:
                        int index = bits switch
                        {
                            8 => image[xorRow + x],
                            4 => (image[xorRow + (x / 2)] >> (x % 2 == 0 ? 4 : 0)) & 0x0F,
                            _ => (image[xorRow + (x / 8)] >> (7 - (x % 8))) & 1,
                        };
                        int entry = palette + (Math.Min(index, paletteSize - 1) * 4);
                        b = image[entry];
                        g = image[entry + 1];
                        r = image[entry + 2];
                        break;
                }
                int pixel = ((y * width) + x) * 4;
                pixels[pixel] = b;
                pixels[pixel + 1] = g;
                pixels[pixel + 2] = r;
                pixels[pixel + 3] = a;
            }
        }

        // The AND mask gives the transparency where the colours carry none: a set bit is transparent.
        if (hasMask && (bits < 32 || !anyAlpha))
        {
            for (int y = 0; y < height; y++)
            {
                int andRow = and + ((height - 1 - y) * andStride);
                for (int x = 0; x < width; x++)
                {
                    bool transparent = ((image[andRow + (x / 8)] >> (7 - (x % 8))) & 1) == 1;
                    pixels[(((y * width) + x) * 4) + 3] = transparent ? (byte)0 : (byte)255;
                }
            }
        }

        size = new PixelSize(width, height);
        return true;
    }

    private static WriteableBitmap ToBitmap(PixelSize size, byte[] pixels)
    {
        var bitmap = new WriteableBitmap(size, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using (ILockedFramebuffer frame = bitmap.Lock())
        {
            for (int y = 0; y < size.Height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(pixels, y * size.Width * 4, frame.Address + (y * frame.RowBytes), size.Width * 4);
            }
        }
        return bitmap;
    }

    private static int ReadUInt16(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
}
