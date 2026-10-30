using System.Globalization;
using Avalonia;
using Avalonia.Media;
using WpfCompat.Media;
using BrushConverter = WpfCompat.Media.BrushConverter;

namespace WpfCompat.Tests;

/// <summary>
/// The WPF text converters write and read the same text as WPF's own converters, which ported code
/// relies on to save and load files that WPF also reads (for example the plot settings in .bestfit
/// files).
/// </summary>
[TestClass]
public sealed class ConverterTests
{
    [TestMethod]
    public void Color_IsWrittenAsUppercaseArgbHex_AsWpfsColorToString()
    {
        Assert.AreEqual("#FFFF0000", Colors.Red.ToWpfString(), "WPF never writes colour names");
        Assert.AreEqual("#4012ABCD", Color.FromArgb(0x40, 0x12, 0xAB, 0xCD).ToWpfString());
        Assert.AreEqual("#00FFFFFF", Colors.Transparent.ToWpfString());
        Assert.AreEqual("#FF000000", new ColorConverter().ConvertToInvariantString(Colors.Black));
    }

    [TestMethod]
    public void Color_IsReadFromHexInEveryWpfLength_AndFromNamesInAnyCase()
    {
        Assert.AreEqual(Colors.Red, (Color)ColorConverter.ConvertFromString("#FFFF0000")!);
        Assert.AreEqual(Color.FromArgb(0xFF, 0x11, 0x22, 0x33), (Color)ColorConverter.ConvertFromString("#112233")!);
        Assert.AreEqual(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00), (Color)ColorConverter.ConvertFromString("#F00")!);
        Assert.AreEqual(Color.FromArgb(0x88, 0xFF, 0x00, 0x00), (Color)ColorConverter.ConvertFromString("#8F00")!);
        Assert.AreEqual(Colors.Red, (Color)ColorConverter.ConvertFromString(" red ")!, "trimmed, any case");
        Assert.AreEqual(Colors.Transparent, (Color)ColorConverter.ConvertFromString("Transparent")!);
        Assert.IsNull(ColorConverter.ConvertFromString(null), "WPF returns null for null");

        foreach (string bad in new[] { "#12345", "#GG0000", "notacolour", "rgb(1,2,3)", "" })
        {
            Assert.ThrowsException<FormatException>(() => ColorConverter.ConvertFromString(bad), bad);
        }
    }

    [TestMethod]
    public void Brush_IsWrittenAsItsColour_AndNoBrushAsEmptyText()
    {
        HeadlessSession.Run(() =>
        {
            var converter = new BrushConverter();
            Assert.AreEqual("", converter.ConvertToInvariantString(null), "WPF writes null as empty text");
            Assert.AreEqual("#FFFFFFFF", converter.ConvertToInvariantString(new SolidColorBrush(Colors.White)));
            Assert.AreEqual("#FFFFFFFF", converter.ConvertToInvariantString(Brushes.White), "immutable brushes too");
            Assert.AreEqual("#FFFF0000", converter.ConvertToInvariantString(new SolidColorBrush(Colors.Red) { Opacity = 0.5 }),
                "WPF ignores the brush's opacity");

            var brush = (SolidColorBrush)converter.ConvertFromInvariantString("#FF00FF00")!;
            Assert.AreEqual(Color.FromArgb(0xFF, 0, 0xFF, 0), brush.Color);
            Assert.AreEqual(Colors.Blue, ((SolidColorBrush)converter.ConvertFromInvariantString("Blue")!).Color);
        });
    }

    [TestMethod]
    public void FontWeight_UsesWpfsNames_EvenForAvaloniasSynonyms()
    {
        var converter = new FontWeightConverter();
        Assert.AreEqual("Normal", converter.ConvertToInvariantString(FontWeight.Normal));
        Assert.AreEqual("Normal", converter.ConvertToInvariantString(FontWeight.Regular));
        Assert.AreEqual("Bold", converter.ConvertToInvariantString(FontWeight.Bold));
        Assert.AreEqual("SemiBold", converter.ConvertToInvariantString(FontWeight.DemiBold));
        Assert.AreEqual("Black", converter.ConvertToInvariantString(FontWeight.Heavy));
        Assert.AreEqual("ExtraBlack", converter.ConvertToInvariantString(FontWeight.UltraBlack));
        Assert.AreEqual("350", converter.ConvertToInvariantString(FontWeight.SemiLight), "WPF has no name for 350");

        Assert.AreEqual(FontWeight.Bold, converter.ConvertFromInvariantString("bold"));
        Assert.AreEqual(FontWeight.Black, converter.ConvertFromInvariantString("Heavy"));
        Assert.AreEqual(FontWeight.Normal, converter.ConvertFromInvariantString("Regular"));
        Assert.AreEqual((FontWeight)550, converter.ConvertFromInvariantString("550"));
        Assert.ThrowsException<FormatException>(() => converter.ConvertFromInvariantString(" Bold"), "WPF doesn't trim names");
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => converter.ConvertFromInvariantString("1000"));
    }

    [TestMethod]
    public void Thickness_IsWrittenWithTheCulturesListSeparator_AndNaNAsAuto()
    {
        var converter = new ThicknessConverter();
        Assert.AreEqual("8,8,8,8", converter.ConvertToInvariantString(new Thickness(8)));
        Assert.AreEqual("1.5,2,3,Auto", converter.ConvertToInvariantString(new Thickness(1.5, 2, 3, double.NaN)));
        Assert.AreEqual("1,5;2;3;4", converter.ConvertToString(null, CultureInfo.GetCultureInfo("de-DE"), new Thickness(1.5, 2, 3, 4)));
    }

    [TestMethod]
    public void Thickness_IsReadFromOneTwoOrFourValues_WithUnits()
    {
        var converter = new ThicknessConverter();
        Assert.AreEqual(new Thickness(8), converter.ConvertFromInvariantString("8"));
        Assert.AreEqual(new Thickness(1, 2, 1, 2), converter.ConvertFromInvariantString("1,2"));
        Assert.AreEqual(new Thickness(1, 2, 3, 4), converter.ConvertFromInvariantString("1,2,3,4"));
        Assert.AreEqual(new Thickness(1, 2, 3, 4), converter.ConvertFromInvariantString(" 1 2, 3  4 "));
        Assert.AreEqual(new Thickness(96, 0, 0, 0), converter.ConvertFromInvariantString("1in,0,0,0"));
        Assert.AreEqual(new Thickness(3), converter.ConvertFrom(3.0));
        Assert.IsTrue(double.IsNaN(((Thickness)converter.ConvertFromInvariantString("Auto")!).Left));

        foreach (string bad in new[] { "1,2,3", "1,2,3,4,5", "1,,2", ",1", "1,", "x" })
        {
            Assert.ThrowsException<FormatException>(() => converter.ConvertFromInvariantString(bad), bad);
        }
    }

    [TestMethod]
    public void FontFamily_IsWrittenAsItsInstalledFontNames()
    {
        var converter = new FontFamilyConverter();
        Assert.AreEqual("Segoe UI", converter.ConvertToInvariantString(new FontFamily("Segoe UI")));
        Assert.AreEqual("Arial, Verdana", converter.ConvertToInvariantString(new FontFamily("Arial, Verdana")));
        Assert.AreEqual("Segoe UI", converter.ConvertToInvariantString(new FontFamily("Segoe UI, avares://Themes/Assets/Fonts#Selawik")),
            "the theme's embedded fallback font is XGoodFit's, not WPF's");
        Assert.AreEqual("Selawik", converter.ConvertToInvariantString(new FontFamily("avares://Themes/Assets/Fonts#Selawik")));
        Assert.ThrowsException<ArgumentNullException>(() => converter.ConvertToInvariantString(null), "as in WPF");

        var family = (FontFamily)converter.ConvertFromInvariantString("Consolas")!;
        Assert.AreEqual("Consolas", family.Name);
    }

    [TestMethod]
    public void Visibility_HasWpfsNamesAndValues()
    {
        Assert.AreEqual("Visible,Hidden,Collapsed", string.Join(",", Enum.GetNames<Visibility>()));
        CollectionAssert.AreEqual(new byte[] { 0, 1, 2 }, Enum.GetValues<Visibility>().Select(v => (byte)v).ToArray());
    }
}
