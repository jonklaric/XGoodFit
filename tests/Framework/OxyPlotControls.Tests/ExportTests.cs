using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using OxyPlot;
using static OxyPlotControls.Tests.PlotTesting;

namespace OxyPlotControls.Tests;

/// <summary>
/// The toolbar's Save Plot Image dialog writes the format and size the user picks (PNG, PDF or SVG),
/// optionally in the report theme, and the Export Series Data button writes the series' points.
/// </summary>
[TestClass]
public sealed class ExportTests
{
    private static string TempFile(string name)
    {
        string folder = Path.Combine(Path.GetTempPath(), "OxyPlotControls.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, name);
    }

    // Clicks the toolbar's Save Plot Image button, and in the dialog picks the size (an item of its size
    // list), the report theme, and Save As; the file picker answers with path.
    private static (List<string> Messages, string? SuggestedName) SaveImage(Window window, OxyPlotToolbar toolbar, string path, int sizeIndex, bool reportTheme)
    {
        var messages = new List<string>();
        FakeFilePicker picker = FakeFilePicker.Instance;
        picker.SavePath = path;
        using var closer = CloseMessageBoxes(messages);
        using var answer = WhenOpened<SavePlotImageDialog>(dialog =>
        {
            Assert.AreSame(window, dialog.Owner, "the dialog belongs to the plot's window");
            dialog.FindControl<ComboBox>("ImageSizeComboBox")!.SelectedIndex = sizeIndex;
            dialog.FindControl<CheckBox>("ReportThemeCheckBox")!.IsChecked = reportTheme;
            Settle();
            Button saveAs = dialog.FindControl<Button>("SaveAsButton")!;
            saveAs.Focus();
            RaiseClick(saveAs);
        });
        Click(window, toolbar.SaveImageButton);
        return (messages, picker.LastSaveOptions?.SuggestedFileName);
    }

    [TestMethod]
    public void SaveImage_WritesAPngOfTheChosenSize()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, toolbar, _) = ShowDemo();
            string path = TempFile("plot.png");

            var (messages, suggested) = SaveImage(window, toolbar, path, sizeIndex: 2, reportTheme: false);

            Assert.AreEqual(0, messages.Count, string.Join(Environment.NewLine, messages));
            Assert.AreEqual("Line Series", suggested, "the plot's title is the suggested name");
            using var bitmap = new Bitmap(path);
            Assert.AreEqual(new Avalonia.PixelSize(600, 450), bitmap.PixelSize, "600 x 450 (4:3)");
            window.Close();
        });
    }

    [TestMethod]
    public void SaveImage_WritesAnSvgOfTheChosenSize()
    {
        HeadlessSession.Run(() =>
        {
            var (window, _, toolbar, _) = ShowDemo();
            string path = TempFile("plot.svg");

            var (messages, _) = SaveImage(window, toolbar, path, sizeIndex: 4, reportTheme: false);

            Assert.AreEqual(0, messages.Count, string.Join(Environment.NewLine, messages));
            XElement svg = XElement.Load(path);
            Assert.AreEqual("svg", svg.Name.LocalName);
            Assert.AreEqual("1280", (string?)svg.Attribute("width"));
            Assert.AreEqual("720", (string?)svg.Attribute("height"));
            Assert.IsTrue(svg.Descendants().Any(e => e.Name.LocalName == "path" && ((string?)e.Attribute("transform"))?.StartsWith("translate") == true),
                "the text, which upstream's exporter draws as outlines");
            window.Close();
        });
    }

    [TestMethod]
    public void SaveImage_WritesAPdf()
    {
        HeadlessSession.Run(() =>
        {
            var (window, _, toolbar, _) = ShowDemo();
            string path = TempFile("plot.pdf");

            var (messages, _) = SaveImage(window, toolbar, path, sizeIndex: 0, reportTheme: false);

            Assert.AreEqual(0, messages.Count, string.Join(Environment.NewLine, messages));
            byte[] pdf = File.ReadAllBytes(path);
            Assert.AreEqual("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
            window.Close();
        });
    }

    [TestMethod]
    public void SaveImage_InTheReportTheme_HasAWhiteBackground_AndLeavesThePlotAlone()
    {
        HeadlessSession.Run(() =>
        {
            Themes.ThemeService.Instance.SetTheme(Themes.Theme.Dark);
            try
            {
                var (window, plot, toolbar, _) = ShowDemo();
                Settle();
                OxyColor background = plot.ActualModel.Background;
                string path = TempFile("report.png");

                var (messages, _) = SaveImage(window, toolbar, path, sizeIndex: 0, reportTheme: true);

                Assert.AreEqual(0, messages.Count, string.Join(Environment.NewLine, messages));
                using var bitmap = new Bitmap(path);
                IntPtr corner = System.Runtime.InteropServices.Marshal.AllocHGlobal(4);
                try
                {
                    bitmap.CopyPixels(new Avalonia.PixelRect(0, 0, 1, 1), corner, 4, 4);
                    Assert.AreEqual(unchecked((int)0xFFFFFFFF), System.Runtime.InteropServices.Marshal.ReadInt32(corner), "the report theme's white background");
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(corner);
                }
                Assert.AreEqual(background, plot.ActualModel.Background, "the plot keeps its own theme");
                window.Close();
            }
            finally
            {
                Themes.ThemeService.Instance.SetTheme(Themes.Theme.Light);
            }
        });
    }

    [TestMethod]
    public void ExportData_WritesTheSeriesToCsv()
    {
        HeadlessSession.Run(() =>
        {
            var (window, _, toolbar, _) = ShowDemo();
            string path = TempFile("data.csv");
            FakeFilePicker.Instance.SavePath = path;
            var messages = new List<string>();
            using (CloseMessageBoxes(messages))
            {
                Click(window, toolbar.ExportDataButton);
            }

            string[] lines = File.ReadAllLines(path);
            Assert.IsTrue(lines.Length > 100, $"a row for each point: {lines.Length} lines; {string.Join(" / ", messages)}");
            StringAssert.Contains(lines[0], "Test Line Series 1");
            StringAssert.Contains(lines[0], "Test Line Series 2");
            window.Close();
        });
    }
}
