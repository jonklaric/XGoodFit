using System.Buffers.Binary;
using System.Text;
using SkiaSharp;

namespace AppIcons;

// Usage: AppIcons <icon.ico> <output folder> [<large icon.png>]
//   Writes <folder>/app.icns (macOS) and <folder>/<size>.png for each of IconSet.LinuxSizes (Linux).
//   Each size comes from the icon's own image of that size where it has one, else it's scaled down
//   from the next larger. An .ico holds up to 256 px: the large icon, a square PNG larger than that,
//   gives the sizes above (macOS's 512 and 1024 px). Nothing is scaled up: sizes above the largest
//   image are left out.
internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length is not (2 or 3))
        {
            Console.Error.WriteLine("Usage: AppIcons <icon.ico> <output folder> [<large icon.png>]");
            return 2;
        }

        try
        {
            using IconSet icon = IconSet.Load(File.ReadAllBytes(args[0]));
            if (args.Length == 3)
                icon.AddLarger(File.ReadAllBytes(args[2]));
            Directory.CreateDirectory(args[1]);
            int[] pngs = IconSet.LinuxSizes.Where(size => size <= icon.LargestSize).ToArray();
            foreach (int size in pngs)
                File.WriteAllBytes(Path.Combine(args[1], $"{size}.png"), icon.ToPng(size));
            File.WriteAllBytes(Path.Combine(args[1], "app.icns"), icon.ToIcns());
            int icns = IconSet.IcnsEntries.Where(entry => entry.Size <= icon.LargestSize).Max(entry => entry.Size);
            Console.WriteLine($"{args[1]}: app.icns up to {icns} px and PNGs up to {pngs.Max()} px, from {string.Join(" and ", args.Skip(2).Prepend(args[0]).Select(Path.GetFileName))} ({string.Join(", ", icon.Sizes)} px)");
            return 0;
        }
        catch (Exception e) when (e is InvalidDataException or IOException)
        {
            Console.Error.WriteLine($"AppIcons: {e.Message}");
            return 1;
        }
    }
}

/// <summary>The square images of an icon, by size.</summary>
internal sealed class IconSet : IDisposable
{
    /// <summary>The sizes of the freedesktop icon theme's (hicolor) application icons that are made.</summary>
    public static readonly int[] LinuxSizes = [16, 24, 32, 48, 64, 128, 256];

    /// <summary>An .icns file's PNG entries: type and size in pixels (the @2x ones are Retina sizes).</summary>
    public static readonly (string Type, int Size)[] IcnsEntries =
    [
        ("icp4", 16), ("ic11", 32), ("icp5", 32), ("ic12", 64), ("ic07", 128), ("ic13", 256),
        ("ic08", 256), ("ic14", 512), ("ic09", 512), ("ic10", 1024),
    ];

    private static readonly byte[] PngSignature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly SortedDictionary<int, SKBitmap> _images;

    private IconSet(SortedDictionary<int, SKBitmap> images) => _images = images;

    /// <summary>The sizes the icon has images of.</summary>
    public IEnumerable<int> Sizes => _images.Keys;

    /// <summary>The size of the icon's largest image.</summary>
    public int LargestSize => _images.Keys.Max();

    /// <summary>Reads the square images of an .ico file (BMP or PNG, as Windows stores them).</summary>
    public static IconSet Load(byte[] ico)
    {
        if (ico.Length < 6 || BinaryPrimitives.ReadUInt16LittleEndian(ico) != 0 || BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(2)) != 1)
            throw new InvalidDataException("Not an .ico file.");
        int count = BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(4));
        var images = new SortedDictionary<int, SKBitmap>();
        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> entry = ico.AsSpan(6 + 16 * i, 16);
            int width = entry[0] == 0 ? 256 : entry[0];
            int height = entry[1] == 0 ? 256 : entry[1];
            int length = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]);
            int offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[12..]);
            if (width != height || images.ContainsKey(width))
                continue;
            byte[] data = ico.AsSpan(offset, length).ToArray();
            SKBitmap bitmap = Decode(data, entry) ?? throw new InvalidDataException($"The icon's {width} px image can't be read.");
            if (bitmap.Width != width || bitmap.Height != height)
                throw new InvalidDataException($"The icon's {width} px image is {bitmap.Width}x{bitmap.Height}.");
            images.Add(width, bitmap);
        }
        if (images.Count == 0)
            throw new InvalidDataException("The icon has no square images.");
        return new IconSet(images);
    }

    // A PNG image decodes as it is. A BMP image (without its file header, and with the transparency
    // mask after the colours) decodes as the only image of an .ico, which Skia reads.
    private static SKBitmap? Decode(byte[] data, ReadOnlySpan<byte> entry)
    {
        if (data.AsSpan().StartsWith(PngSignature))
            return SKBitmap.Decode(data);

        var single = new byte[22 + data.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(single.AsSpan(2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(single.AsSpan(4), 1);
        entry.CopyTo(single.AsSpan(6));
        BinaryPrimitives.WriteUInt32LittleEndian(single.AsSpan(6 + 12), 22);
        data.CopyTo(single, 22);
        return SKBitmap.Decode(single);
    }

    /// <summary>
    /// Adds a square PNG image larger than the icon's own, for the sizes above them: an .ico holds
    /// up to 256 px, macOS's icons go up to 1024 px.
    /// </summary>
    public void AddLarger(byte[] png)
    {
        SKBitmap image = (png.AsSpan().StartsWith(PngSignature) ? SKBitmap.Decode(png) : null)
            ?? throw new InvalidDataException("The large icon isn't a PNG image.");
        if (image.Width != image.Height || image.Width <= LargestSize)
        {
            string size = $"{image.Width}x{image.Height}";
            image.Dispose();
            throw new InvalidDataException($"The large icon is {size}: it must be square, and larger than the icon's {LargestSize} px.");
        }
        _images.Add(image.Width, image);
    }

    /// <summary>The image of a size as a PNG file: the icon's own, or scaled down from the next larger.</summary>
    public byte[] ToPng(int size)
    {
        if (size > LargestSize)
            throw new ArgumentOutOfRangeException(nameof(size), $"The icon's largest image is {LargestSize} px.");
        if (_images.TryGetValue(size, out SKBitmap? exact))
            return Encode(exact);

        SKBitmap source = _images.First(image => image.Key > size).Value;
        using SKBitmap scaled = source.Resize(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new InvalidDataException($"The icon can't be scaled to {size} px.");
        return Encode(scaled);
    }

    /// <summary>An .icns file of the entries up to the icon's largest size, each a PNG image.</summary>
    public byte[] ToIcns()
    {
        var entries = IcnsEntries.Where(entry => entry.Size <= LargestSize).Select(entry => (entry.Type, Png: ToPng(entry.Size))).ToList();
        using var stream = new MemoryStream();
        WriteHeader(stream, "icns", 8 + entries.Sum(entry => 8 + entry.Png.Length));
        foreach ((string type, byte[] png) in entries)
        {
            WriteHeader(stream, type, 8 + png.Length);
            stream.Write(png);
        }
        return stream.ToArray();
    }

    private static void WriteHeader(Stream stream, string type, int length)
    {
        stream.Write(Encoding.ASCII.GetBytes(type));
        Span<byte> size = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(size, length);
        stream.Write(size);
    }

    private static byte[] Encode(SKBitmap bitmap)
    {
        using SKData png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    public void Dispose()
    {
        foreach (SKBitmap image in _images.Values)
            image.Dispose();
    }
}
