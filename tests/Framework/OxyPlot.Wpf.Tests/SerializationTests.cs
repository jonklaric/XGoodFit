using System.Xml.Linq;
using Avalonia.Media;
using OxyPlot.Wpf.Serialization;
using WpfCompat;

namespace OxyPlot.Wpf.Tests;

/// <summary>
/// The plot settings saved in .bestfit files: the port writes WPF's text forms (type names, colours,
/// font weights, thicknesses, visibilities), so files stay readable by both apps, and reading then
/// writing settings gives the same XML.
/// </summary>
[TestClass]
public sealed class SerializationTests
{
    private static XElement Serialize(Plot plot) => PlotSerializer.ToXElement(plot);

    [TestMethod]
    public void ToXElement_WritesTypeNamesAndValues_AsTheWpfForkDoes()
    {
        HeadlessSession.Run(() =>
        {
            var xml = Serialize(TestPlots.FrequencyPlot());
            Assert.AreEqual("OxyplotProperties", xml.Name.LocalName);

            var axes = xml.Element("Axes")!.Elements("Axis").ToList();
            CollectionAssert.AreEqual(
                new[] { "OxyPlot.Wpf.NormalProbabilityAxis", "OxyPlot.Wpf.LinearAxis" },
                axes.Select(a => (string?)a.Attribute("AxisType")).ToList(),
                "the WPF type names, which RMC-BestFit reads back");

            var series = xml.Element("Series")!.Elements("SeriesItem").ToList();
            CollectionAssert.AreEqual(
                new[] { "OxyPlot.Wpf.LineSeries", "OxyPlot.Wpf.ScatterSeries", "OxyPlot.Wpf.LineSeries" },
                series.Select(s => (string?)s.Attribute("SeriesType")).ToList());

            var fitted = series[0].Element("General")!;
            Assert.AreEqual("Fitted", (string?)fitted.Attribute("Title"));
            Assert.AreEqual("#FFE01010", (string?)fitted.Attribute("Color"), "WPF's #AARRGGBB colour text");
            Assert.AreEqual("Visible", (string?)fitted.Attribute("Visibility"));
            Assert.AreEqual("True", (string?)fitted.Attribute("IsEnabled"));
            Assert.AreEqual("Segoe UI", (string?)fitted.Attribute("FontFamily"));
            Assert.AreEqual("Normal", (string?)fitted.Attribute("FontWeight"));
            Assert.AreEqual("0,0,0,0", (string?)fitted.Attribute("Padding"), "WPF's thickness text");
            Assert.AreEqual("Hidden", (string?)series[2].Element("General")!.Attribute("Visibility"), "WPF's three-state visibility");
            Assert.AreEqual("#FF4682B4", (string?)series[1].Element("ScatterSeries")!.Attribute("MarkerFill"), "colours passed as objects too");

            var general = xml.Element("General")!;
            Assert.AreEqual("#FF203040", (string?)general.Element("Title")!.Attribute("TitleColor"));
            Assert.AreEqual("Bold", (string?)general.Element("Title")!.Attribute("TitleFontWeight"));
            Assert.AreEqual("#00000001", (string?)general.Element("Subtitle")!.Attribute("SubtitleColor"), "MoreColors.Automatic");
            Assert.AreEqual("#FFFFFFFF", (string?)general.Element("Chart")!.Attribute("Background"), "the theme's white, which WPF gives even a plot outside a window");
            Assert.AreEqual("", (string?)general.Element("Chart")!.Attribute("BorderBrush"), "no brush is empty text, as in WPF");
            Assert.AreEqual("8,8,8,8", (string?)general.Element("Chart")!.Attribute("Padding"));
            Assert.AreEqual("1,0,0,1", (string?)general.Element("Plot")!.Attribute("PlotAreaBorderThickness"));

            var legend = xml.Element("Legend")!;
            Assert.AreEqual("SemiBold", (string?)legend.Element("Items")!.Attribute("LegendFontWeight"));
            Assert.AreEqual("Left", (string?)legend.Element("Items")!.Attribute("LegendItemAlignment"));
            Assert.AreEqual("true", (string?)legend.Element("Area")!.Attribute("IsLegendVisible"), "booleans passed as objects are XML booleans");

            var linearAxis = axes[1];
            Assert.AreEqual("#40000000", (string?)linearAxis.Element("MajorGridlines")?.Attribute("MajorGridlineColor")
                ?? linearAxis.Descendants().Select(e => (string?)e.Attribute("MajorGridlineColor")).First(v => v != null));

            var annotations = xml.Element("Annotations")!.Elements("Annotation").ToList();
            CollectionAssert.AreEqual(
                new[] { "OxyPlot.Wpf.LineAnnotation", "OxyPlot.Wpf.TextAnnotation" },
                annotations.Select(a => (string?)a.Attribute("AnnotationType")).ToList());
        });
    }

    [TestMethod]
    public void FromXElement_ThenToXElement_GivesTheSameXml()
    {
        HeadlessSession.Run(() =>
        {
            var original = Serialize(TestPlots.FrequencyPlot()).ToString();

            var restored = new Plot();
            PlotSerializer.FromXElement(restored, XElement.Parse(original));
            var again = Serialize(restored).ToString();

            Assert.AreEqual(original, again);
            Assert.AreEqual(Visibility.Hidden, restored.Series[2].Visibility);
            Assert.IsInstanceOfType<NormalProbabilityAxis>(restored.Axes[0]);
        });
    }

    [TestMethod]
    public void FromXElement_ReadsWpfColourAndBrushForms()
    {
        HeadlessSession.Run(() =>
        {
            var plot = new Plot();
            var xml = Serialize(plot);
            var chart = xml.Element("General")!.Element("Chart")!;
            chart.SetAttributeValue("Background", "#FF102030");
            chart.SetAttributeValue("Padding", "4,5,6,7");
            xml.Element("General")!.Element("Title")!.SetAttributeValue("TitleColor", "Red");
            xml.Element("General")!.Element("Title")!.SetAttributeValue("TitleFontWeight", "Black");

            PlotSerializer.FromXElement(plot, xml);
            Assert.AreEqual(Color.FromRgb(0x10, 0x20, 0x30), ((ISolidColorBrush)plot.Background!).Color);
            Assert.AreEqual(new Avalonia.Thickness(4, 5, 6, 7), plot.Padding);
            Assert.AreEqual(Colors.Red, plot.TitleColor);
            Assert.AreEqual(FontWeight.Black, plot.TitleFontWeight);
        });
    }

    [TestMethod]
    public void FromXElement_ReadsTheOlderAttributeNames()
    {
        HeadlessSession.Run(() =>
        {
            var xml = XElement.Parse(
                """
                <OxyplotProperties>
                  <General>
                    <Title Title="Older" Color="#FF112233" Size="20" Weight="SemiBold" Padding="9" />
                    <Plot BorderColor="#FF445566" BorderThickness="2" />
                  </General>
                  <Legend>
                    <Area LegendVisible="false" BackgroundColor="#80FFFFFF" BorderThickness="3" />
                    <Position Placement="Outside" Position="BottomCenter" Orientation="Horizontal" />
                    <Items Color="#FF0000FF" SymbolLength="20" ItemAlignment="Right" />
                  </Legend>
                </OxyplotProperties>
                """);

            var plot = new Plot();
            PlotSerializer.FromXElement(plot, xml);

            Assert.AreEqual("Older", plot.Title);
            Assert.AreEqual(Color.FromRgb(0x11, 0x22, 0x33), plot.TitleColor);
            Assert.AreEqual(20, plot.TitleFontSize);
            Assert.AreEqual(FontWeight.SemiBold, plot.TitleFontWeight);
            Assert.AreEqual(9, plot.TitlePadding);
            Assert.AreEqual(Color.FromRgb(0x44, 0x55, 0x66), plot.PlotAreaBorderColor);
            Assert.AreEqual(new Avalonia.Thickness(2), plot.PlotAreaBorderThickness);
            Assert.IsFalse(plot.IsLegendVisible);
            Assert.AreEqual(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF), plot.LegendBackground);
            Assert.AreEqual(3, plot.LegendBorderThickness);
            Assert.AreEqual(OxyPlot.Legends.LegendPlacement.Outside, plot.LegendPlacement);
            Assert.AreEqual(OxyPlot.Legends.LegendPosition.BottomCenter, plot.LegendPosition);
            Assert.AreEqual(OxyPlot.Legends.LegendOrientation.Horizontal, plot.LegendOrientation);
            Assert.AreEqual(Colors.Blue, plot.LegendTextColor);
            Assert.AreEqual(20, plot.LegendSymbolLength);
            Assert.AreEqual(Avalonia.Layout.HorizontalAlignment.Right, plot.LegendItemAlignment);
        });
    }

    /// <summary>
    /// The plot settings the WPF fork wrote for the tests' plots (Fixtures/README.md says how they were
    /// made). The port must write exactly the same XML, and read it back unchanged.
    /// </summary>
    [TestMethod]
    [DataRow("FrequencyPlot")]
    [DataRow("EveryElement")]
    public void ToXElement_MatchesTheXmlTheWpfForkWrote(string name)
    {
        HeadlessSession.Run(() =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".wpf.xml");
            var wpf = File.ReadAllText(path).ReplaceLineEndings("\n").TrimEnd();
            var plot = name == "FrequencyPlot" ? TestPlots.FrequencyPlot() : TestPlots.EveryElement();

            var port = Serialize(plot).ToString().ReplaceLineEndings("\n");
            Assert.AreEqual(wpf, port, "the port writes the WPF fork's XML");

            var restored = new Plot();
            PlotSerializer.FromXElement(restored, XElement.Parse(wpf));
            Assert.AreEqual(wpf, Serialize(restored).ToString().ReplaceLineEndings("\n"), "and reads it back unchanged");
        });
    }
}
