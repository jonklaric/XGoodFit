using System.Buffers.Binary;
using System.Text;
using AppIcons;

namespace XGoodFit.Platform.Tests;

/// <summary>
/// The packages' icons, rendered from the application icon and its large image (Directory.Build.props'
/// XGoodFitIcon and XGoodFitLargeIcon).
/// </summary>
[TestClass]
public sealed class AppIconsTests
{
    private static readonly string IconPath = Path.Combine(AppContext.BaseDirectory, "TestData", "app.ico");
    private static readonly string LargeIconPath = Path.Combine(AppContext.BaseDirectory, "TestData", "app-large.png");

    [TestMethod]
    public void ReadsEveryImageOfTheIcon()
    {
        using IconSet icon = IconSet.Load(File.ReadAllBytes(IconPath));

        CollectionAssert.AreEqual(new[] { 16, 24, 32, 48, 64, 128, 256 }, icon.Sizes.ToArray());
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

        CollectionAssert.AreEqual(IconSet.IcnsEntries.Where(entry => entry.Size <= 256).ToArray(), IcnsEntries(icon.ToIcns()));
    }

    [TestMethod]
    public void TakesTheSizesAboveTheIconsFromTheLargeIcon()
    {
        using IconSet icon = IconSet.Load(File.ReadAllBytes(IconPath));
        using IconSet icoOnly = IconSet.Load(File.ReadAllBytes(IconPath));

        icon.AddLarger(File.ReadAllBytes(LargeIconPath));

        CollectionAssert.AreEqual(new[] { 16, 24, 32, 48, 64, 128, 256, 1024 }, icon.Sizes.ToArray());
        CollectionAssert.AreEqual(IconSet.IcnsEntries, IcnsEntries(icon.ToIcns()), "macOS's 512 and 1024 px too");
        Assert.AreEqual(512, PngSize(icon.ToPng(512)));
        foreach (int size in IconSet.LinuxSizes)
            CollectionAssert.AreEqual(icoOnly.ToPng(size), icon.ToPng(size), $"{size} px still comes from the .ico");
        Assert.ThrowsException<InvalidDataException>(() => icon.AddLarger(icoOnly.ToPng(256)), "Not larger than the icon's own images.");
    }

    // An .icns file's entries: their types and their PNG images' sizes.
    private static (string Type, int Size)[] IcnsEntries(byte[] icns)
    {
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
        return entries.ToArray();
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
