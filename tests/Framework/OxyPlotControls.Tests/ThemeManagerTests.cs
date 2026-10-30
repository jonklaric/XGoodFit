using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using OxyPlot;
using OxyPlot.Axes;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls.Tests;

/// <summary>
/// OxyPlotThemeManager: the app's theme picks a plot theme, which colours the plot and its model (for
/// export), without the axes and series reporting property changes that undo would record.
/// </summary>
[TestClass]
public sealed class ThemeManagerTests
{
    private static Wpf.Plot Plot()
    {
        var plot = new Wpf.Plot();
        plot.Axes.Add(new Wpf.LinearAxis { Position = AxisPosition.Bottom });
        plot.Axes.Add(new Wpf.LinearAxis { Position = AxisPosition.Left });
        plot.Series.Add(new Wpf.LineSeries { ItemsSource = new[] { new DataPoint(0, 0), new DataPoint(1, 1) } });
        plot.InvalidatePlot(true);
        return plot;
    }

    [TestMethod]
    public void TheDarkAppTheme_GetsTheDarkPlotTheme_AndTheOthersTheLightOne()
    {
        Assert.AreSame(OxyPlotThemeManager.DarkTheme, OxyPlotThemeManager.GetThemeFor(Themes.Theme.Dark));
        Assert.AreSame(OxyPlotThemeManager.LightTheme, OxyPlotThemeManager.GetThemeFor(Themes.Theme.Light));
        Assert.AreSame(OxyPlotThemeManager.LightTheme, OxyPlotThemeManager.GetThemeFor(Themes.Theme.Blue));
    }

    [TestMethod]
    public void ApplyTheme_ColoursThePlotAndItsModel_WithoutAxisOrSeriesChangeNotifications()
    {
        HeadlessSession.Run(() =>
        {
            var plot = Plot();
            var changes = new List<string>();
            foreach (Wpf.Axis axis in plot.Axes)
            {
                ((INotifyPropertyChanged)axis).PropertyChanged += (_, e) => changes.Add("axis " + e.PropertyName);
            }
            ((INotifyPropertyChanged)plot.Series[0]).PropertyChanged += (_, e) => changes.Add("series " + e.PropertyName);

            OxyPlotThemeManager.ApplyTheme(plot, OxyPlotThemeManager.DarkTheme);

            Assert.AreEqual(Color.FromRgb(0x2D, 0x2D, 0x30), ((ISolidColorBrush)plot.Background!).Color);
            Assert.AreEqual(OxyColor.FromRgb(0x2D, 0x2D, 0x30), plot.ActualModel.Background, "the model's, which exports use");
            Assert.AreEqual(Color.FromRgb(0x80, 0x80, 0x8E), ((ISolidColorBrush)plot.PlotAreaBackground!).Color);
            Assert.AreEqual(Color.FromRgb(0xF1, 0xF1, 0xF1), plot.TextColor);
            foreach (Wpf.Axis axis in plot.Axes)
            {
                Assert.AreEqual(Color.FromRgb(0x6A, 0x6A, 0x78), axis.MajorGridlineColor);
            }
            foreach (var modelAxis in plot.ActualModel.Axes)
            {
                Assert.AreEqual(OxyColor.FromRgb(0xCC, 0xCC, 0xCC), modelAxis.TextColor);
            }
            CollectionAssert.AreEqual(Array.Empty<string>(), changes, "theme changes aren't user edits");
        });
    }

    [TestMethod]
    public void TheLightTheme_TakesTheWindowBackground_FromTheAppTheme()
    {
        HeadlessSession.Run(() =>
        {
            var resources = Application.Current!.Resources;
            resources["EnvironmentWindowBackground"] = new ImmutableSolidColorBrush(Color.Parse("#FFF5F5F5"));
            try
            {
                var plot = Plot();
                OxyPlotThemeManager.ApplyTheme(plot, OxyPlotThemeManager.LightTheme);
                Assert.AreEqual(Color.Parse("#FFF5F5F5"), ((ISolidColorBrush)plot.Background!).Color);
                Assert.AreEqual(OxyColor.Parse("#FFF5F5F5"), plot.ActualModel.Background);
            }
            finally
            {
                resources.Remove("EnvironmentWindowBackground");
            }
        });
    }

    [TestMethod]
    public void WithThemedModel_ThemesTheModelForTheAction_ThenRestoresIt()
    {
        HeadlessSession.Run(() =>
        {
            var plot = Plot();
            PlotModel model = plot.ActualModel;
            model.Background = OxyColors.Beige;
            model.Axes[0].TextColor = OxyColors.Red;
            OxyColor during = OxyColors.Undefined;

            OxyPlotThemeManager.WithThemedModel(model, OxyPlotThemeManager.ReportTheme, m => during = m.Background);

            Assert.AreEqual(OxyColors.White, during, "the report theme's background, while the action runs");
            Assert.AreEqual(OxyColors.Beige, model.Background);
            Assert.AreEqual(OxyColors.Red, model.Axes[0].TextColor);
        });
    }
}
