using System.Buffers.Binary;
using AppIcons;
using SkiaSharp;

namespace Branding;

// Usage: Branding [<folder>]   (default: the repository's branding/)
//   Draws XGoodFit's logo concepts and its icon (branding/README.md) and writes them into the folder:
//     concepts/<concept>.svg, .png   each concept, and a 512 px image of it
//     xgoodfit.svg                   the logo: the chosen concept
//     XGoodFit.ico                   the icon: PNG images of IcoSizes
//     xgoodfit-1024.png              the icon at 1024 px, for macOS's largest sizes
//     previews/*.png                 the concepts and the icon at their real sizes, on light and dark
//                                    backgrounds, beside RMC-BestFit's icon
internal static class Program
{
    /// <summary>The icon's sizes: Windows' and the freedesktop icon theme's, up to 256 px.</summary>
    public static readonly int[] IcoSizes = [16, 24, 32, 48, 64, 128, 256];

    private static readonly int[] PreviewSizes = [256, 64, 32, 16];

    public static int Main(string[] args)
    {
        string root = FindRoot();
        string output = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(root, "branding");
        string concepts = Path.Combine(output, "concepts"), previews = Path.Combine(output, "previews");
        Directory.CreateDirectory(concepts);
        Directory.CreateDirectory(previews);

        using IconSet bestFit = IconSet.Load(File.ReadAllBytes(Path.Combine(root, "upstream/RMC-BestFit/src/RMC.BestFit.App/Resources/BestFit_Icon.ico")));
        var bestFitRow = new Previews.Row("RMC-BestFit (BestFit_Icon.ico)", size => SKBitmap.Decode(bestFit.ToPng(size)));

        foreach (Logo logo in Logos.Concepts)
        {
            File.WriteAllText(Path.Combine(concepts, logo.Name + ".svg"), logo.Draw(256).ToSvg($"XGoodFit logo concept: {logo.Title}", logo.Idea));
            Save(logo.Draw(512).Render(512), Path.Combine(concepts, logo.Name + ".png"));
            Save(Previews.Sheet($"Concept {char.ToUpperInvariant(logo.Name[0])}: {logo.Title}. {logo.Idea}",
                [bestFitRow, new($"{logo.Title} ({logo.Name})", size => logo.Draw(size).Render(size))], PreviewSizes, 4),
                Path.Combine(previews, logo.Name + ".png"));
        }

        Logo chosen = Logos.Chosen;
        File.WriteAllText(Path.Combine(output, "xgoodfit.svg"), chosen.Draw(256).ToSvg("XGoodFit",
            $"XGoodFit's logo, concept {chosen.Name}: {chosen.Idea} Drawn by tools/Branding (branding/README.md). MIT licence, as the repository."));
        var pngs = IcoSizes.ToDictionary(size => size, size => Encode(chosen.Draw(size).Render(size)));
        File.WriteAllBytes(Path.Combine(output, "XGoodFit.ico"), Ico(pngs));
        Save(chosen.Draw(1024).Render(1024), Path.Combine(output, "xgoodfit-1024.png"));

        // The icon as it ships: XGoodFit.ico's own images.
        var icon = new Previews.Row("XGoodFit (XGoodFit.ico)", size => SKBitmap.Decode(pngs[size]));
        Save(Previews.Sheet($"XGoodFit's icon: concept {char.ToUpperInvariant(chosen.Name[0])}, {chosen.Title}", [bestFitRow, icon], PreviewSizes, 4),
            Path.Combine(previews, "xgoodfit.png"));
        Save(Previews.Panels("XGoodFit's icon on taskbars, Docks and panels (their colours roughly)", icon with { Label = "XGoodFit" }, bestFitRow with { Label = "RMC-BestFit" }, [16, 24, 32, 48]),
            Path.Combine(previews, "xgoodfit-panels.png"));

        Console.WriteLine($"{output}: concepts, xgoodfit.svg (concept {chosen.Name}), XGoodFit.ico ({string.Join(", ", IcoSizes)} px), xgoodfit-1024.png, previews");
        return 0;
    }

    /// <summary>An .ico file of PNG images, by size (up to 256 px), as Windows Vista and later read them.</summary>
    public static byte[] Ico(IReadOnlyDictionary<int, byte[]> pngs)
    {
        using var stream = new MemoryStream();
        var header = new byte[6];
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(4), (ushort)pngs.Count);
        stream.Write(header);
        int offset = 6 + 16 * pngs.Count;
        foreach ((int size, byte[] png) in pngs.OrderBy(entry => entry.Key))
        {
            var entry = new byte[16];
            entry[0] = entry[1] = (byte)(size == 256 ? 0 : size);
            BinaryPrimitives.WriteUInt16LittleEndian(entry.AsSpan(4), 1);
            BinaryPrimitives.WriteUInt16LittleEndian(entry.AsSpan(6), 32);
            BinaryPrimitives.WriteUInt32LittleEndian(entry.AsSpan(8), (uint)png.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(entry.AsSpan(12), (uint)offset);
            stream.Write(entry);
            offset += png.Length;
        }
        foreach ((_, byte[] png) in pngs.OrderBy(entry => entry.Key))
            stream.Write(png);
        return stream.ToArray();
    }

    private static byte[] Encode(SKBitmap bitmap)
    {
        using (bitmap)
        using (SKData png = bitmap.Encode(SKEncodedImageFormat.Png, 100))
            return png.ToArray();
    }

    private static void Save(SKBitmap bitmap, string path) => File.WriteAllBytes(path, Encode(bitmap));

    private static string FindRoot()
    {
        for (string? dir = AppContext.BaseDirectory; dir is not null; dir = Path.GetDirectoryName(dir))
            if (File.Exists(Path.Combine(dir, "XGoodFit.slnx")))
                return dir;
        throw new InvalidOperationException("Not within the XGoodFit repository: run with `dotnet run --project tools/Branding`.");
    }
}
