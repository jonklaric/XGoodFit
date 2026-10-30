using System.Buffers.Binary;
using System.Text;
using AppIcons;

namespace XGoodFit.Platform.Tests;

/// <summary>The packages' icons, rendered from the application icon (Directory.Build.props' XGoodFitIcon).</summary>
[TestClass]
public sealed class AppIconsTests
{
    private static readonly string IconPath = Path.Combine(AppContext.BaseDirectory, "TestData", "app.ico");

    [TestMethod]
    public void ReadsEveryImageOfTheIcon()
    {
        using IconSet icon = IconSet.Load(File.ReadAllBytes(IconPath));

        CollectionAssert.AreEqual(new[] { 16, 32, 48, 64, 128, 256 }, icon.Sizes.ToArray());
    }

    [TestMethod]
    public void MakesPngsOfEachSize()
    {
        using IconSet icon = IconSet.Load(File.ReadAllBytes(IconPath));

        foreach (int size in IconSet.LinuxSizes)
            Assert.AreEqual(size, PngSize(icon.ToPng(size)), $"{size} px");
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => icon.ToPng(512), "Nothing is scaled up.");
    }

    [TestMethod]
    public void MakesAnIcnsFile()
    {
        using IconSet icon = IconSet.Load(File.ReadAllBytes(IconPath));

        byte[] icns = icon.ToIcns();

        Assert.AreEqual("icns", Encoding.ASCII.GetString(icns, 0, 4));
        Assert.AreEqual(icns.Length, BinaryPrimitives.ReadInt32BigEndian(icns.AsSpan(4)));
        var entries = new List<(string Type, int Size)>();
        for (int offset = 8; offset < icns.Length;)
        {
            string type = Encoding.ASCII.GetString(icns, offset, 4);
            int length = BinaryPrimitives.ReadInt32BigEndian(icns.AsSpan(offset + 4));
            entries.Add((type, PngSize(icns.AsSpan(offset + 8, length - 8).ToArray())));
            offset += length;
        }
        CollectionAssert.AreEqual(IconSet.IcnsEntries.Where(entry => entry.Size <= 256).ToArray(), entries.ToArray());
    }

    // The width in a PNG's header, which must also be its height.
    private static int PngSize(byte[] png)
    {
        Assert.AreEqual("IHDR", Encoding.ASCII.GetString(png, 12, 4));
        int width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16));
        Assert.AreEqual(width, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));
        return width;
    }
}
