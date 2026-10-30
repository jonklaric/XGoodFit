using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace RMC.BestFit.UI.Tests;

/// <summary>
/// XGoodFit addition: the ported element icons (Resources/IconDictionary.axaml), which upstream's tests
/// don't cover. Elements find them in the application's resources, each lookup making a new image whose
/// brushes follow the theme's.
/// </summary>
[TestClass]
public sealed class IconDictionaryTests
{
    private static readonly string[] IconKeys =
    [
        "InputDataIcon", "TimeSeriesDataIcon", "TimeSeriesAnalysisIcon", "MixtureAnalysisIcon",
        "UnivariateAnalysisIcon", "B17AnalysisIcon", "FittingAnalysisIcon", "BivariateAnalysisIcon",
        "CoincidentFrequencyAnalysisIcon", "CompositeAnalysisIcon", "RatingCurveAnalysisIcon",
        "PointProcessAnalysisIcon", "BatchRunIcon",
    ];

    [STATestMethod]
    public void ElementIcons_AreNewImagesEachTime_ThatFollowTheThemeBrushes()
    {
        Application app = Application.Current!;
        var palette = new ResourceDictionary
        {
            ["ToolbarIconForeground"] = new SolidColorBrush(Color.Parse("#1E1E1E")),
            ["ToolbarIconDocumentFill"] = new SolidColorBrush(Colors.White),
            ["ToolbarIconAccent"] = new SolidColorBrush(Color.Parse("#00539C")),
        };
        var icons = (IResourceProvider)AvaloniaXamlLoader.Load(new Uri("avares://RMC.BestFit.UI/Resources/IconDictionary.axaml"));
        app.Resources.MergedDictionaries.Add(palette);
        app.Resources.MergedDictionaries.Add(icons);
        try
        {
            foreach (string key in IconKeys)
            {
                Assert.IsInstanceOfType<DrawingImage>(app.FindResource(key), key);
            }

            var element = new UI.InputData("Icons", new InputDataCollection(BestFitProject.GetInstance()));
            var image = (DrawingImage)element.ElementImage;
            Assert.AreNotSame(image, element.ElementImage, "x:Shared=\"False\": each lookup makes a new image");
            CollectionAssert.Contains(Brushes(image), palette["ToolbarIconForeground"]);

            // A theme switch replaces the palette's brushes, and the image follows.
            var dark = new SolidColorBrush(Color.Parse("#C8C8CC"));
            palette["ToolbarIconForeground"] = dark;
            CollectionAssert.Contains(Brushes(image), dark);
        }
        finally
        {
            app.Resources.MergedDictionaries.Remove(icons);
            app.Resources.MergedDictionaries.Remove(palette);
        }
    }

    private static List<IBrush?> Brushes(DrawingImage image) =>
        ((DrawingGroup)image.Drawing!).Children.OfType<GeometryDrawing>().Select(d => d.Brush).ToList();
}
