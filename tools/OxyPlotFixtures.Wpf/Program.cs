using System.IO;
using System.Text;
using OxyPlot.Wpf.Serialization;
using OxyPlot.Wpf.Tests;

namespace OxyPlotFixtures.Wpf;

/// <summary>
/// Writes the XML the WPF fork's PlotSerializer writes for the tests' plots (see the project file).
/// </summary>
public static class Program
{
    /// <summary>Writes the fixtures into the folder given as the only argument.</summary>
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: OxyPlotFixtures.Wpf <fixtures folder>");
            return 2;
        }

        Directory.CreateDirectory(args[0]);
        Write(Path.Combine(args[0], "FrequencyPlot.wpf.xml"), PlotSerializer.ToXElement(TestPlots.FrequencyPlot()).ToString());
        Write(Path.Combine(args[0], "EveryElement.wpf.xml"), PlotSerializer.ToXElement(TestPlots.EveryElement()).ToString());
        return 0;
    }

    private static void Write(string path, string xml)
    {
        // As the XML text the app stores: XElement.ToString(), here with "\n" line ends and a final one.
        File.WriteAllText(path, xml.ReplaceLineEndings("\n") + "\n", new UTF8Encoding(false));
        Console.WriteLine($"wrote {path}");
    }
}
