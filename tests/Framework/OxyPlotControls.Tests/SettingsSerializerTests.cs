using System.Xml.Linq;
using Avalonia.Media;
using OxyPlot.Wpf.Tests;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls.Tests;

/// <summary>
/// OxyPlotSettingsSerializer, compiled unchanged from upstream, writes and reads the plot settings
/// BestFit keeps in its project files exactly as the WPF fork does: the fixtures are the XML the WPF
/// fork wrote for OxyPlot.Wpf.Tests' plots. The bar series editor's own settings XML keeps WPF's text
/// forms too.
/// </summary>
[TestClass]
public sealed class SettingsSerializerTests
{
    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".wpf.xml")).ReplaceLineEndings("\n").TrimEnd();

    [TestMethod]
    [DataRow("FrequencyPlot")]
    [DataRow("EveryElement")]
    public void ToXelement_WritesTheXmlTheWpfForkWrote_AndFromXelementReadsItBack(string name)
    {
        HeadlessSession.Run(() =>
        {
            string wpf = Fixture(name);
            Wpf.Plot plot = name == "FrequencyPlot" ? TestPlots.FrequencyPlot() : TestPlots.EveryElement();

            XElement xml = OxyPlotSettingsSerializer.ToXelement(plot);
            Assert.AreEqual(OxyPlotSettingsSerializer.OxyplotPropertiesTag, xml.Name.LocalName);
            Assert.AreEqual(wpf, xml.ToString().ReplaceLineEndings("\n"));

            var restored = new Wpf.Plot();
            OxyPlotSettingsSerializer.FromXelement(restored, XElement.Parse(wpf));
            Assert.AreEqual(wpf, OxyPlotSettingsSerializer.ToXelement(restored).ToString().ReplaceLineEndings("\n"));
        });
    }

    [TestMethod]
    public void BarSeriesSettings_UseWpfsTextForms_AndReadBack()
    {
        HeadlessSession.Run(() =>
        {
            var bars = new Wpf.BarSeries
            {
                Title = "Peaks",
                FillColor = Color.FromRgb(0x12, 0x34, 0x56),
                StrokeColor = Colors.Black,
                Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xBB, 0xCC)),
                FontFamily = new FontFamily("Arial"),
                FontWeight = FontWeight.Bold,
                Padding = new Avalonia.Thickness(1, 2, 3, 4),
            };

            XElement xml = BarSeriesControl.BarSeriesPropertiesToXElement(bars);
            XElement label = xml.Element("Labeling")!;
            Assert.AreEqual("#FFAABBCC", (string?)label.Attribute("TextColor"), "WPF's brush text");
            Assert.AreEqual("Arial", (string?)label.Attribute("Font"), "WPF's font family text");
            Assert.AreEqual("Bold", (string?)label.Attribute("FontWeight"));
            Assert.AreEqual("1,2,3,4", (string?)label.Attribute("Padding"), "WPF's thickness text");
            XElement display = xml.Element("Display")!;
            Assert.AreEqual("#FF123456", (string?)display.Attribute("Fill"), "WPF's colour text");

            var restored = new Wpf.BarSeries();
            BarSeriesControl.XElementToBarSeriesProperties(restored, xml);
            Assert.AreEqual(bars.FillColor, restored.FillColor);
            Assert.AreEqual(FontWeight.Bold, restored.FontWeight);
            Assert.AreEqual(new Avalonia.Thickness(1, 2, 3, 4), restored.Padding);
            Assert.AreEqual(Color.FromRgb(0xAA, 0xBB, 0xCC), ((ISolidColorBrush)restored.Foreground!).Color);

            // As upstream's: the font is read into the OxyPlot series, not the series' FontFamily, which
            // is what's written; so the rest reads back as written, the font doesn't.
            Assert.AreEqual("Arial", restored.InternalSeries.Font);
            XElement again = BarSeriesControl.BarSeriesPropertiesToXElement(restored);
            again.Element("Labeling")!.SetAttributeValue("Font", "Arial");
            Assert.AreEqual(xml.ToString(), again.ToString());
        });
    }
}
