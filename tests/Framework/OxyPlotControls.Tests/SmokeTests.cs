using OxyPlot;
using Themes;
using static OxyPlotControls.Tests.PlotTesting;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls.Tests;

/// <summary>
/// The toolbar and every properties page load, apply their themes and bind in each of the three app
/// themes.
/// </summary>
[TestClass]
public sealed class SmokeTests
{
    [TestMethod]
    public void EveryPage_Binds_InEveryTheme()
    {
        HeadlessSession.Run(() =>
        {
            using var errors = new BindingErrors();
            var plot = LinePlot();
            plot.Annotations.Add(new Wpf.TextAnnotation { Text = "Peak", TextPosition = new DataPoint(0, 0.45) });
            var (window, _, toolbar, properties) = ShowDemo(plot);
            for (int page = 0; page < 5; page++)
            {
                properties.PropertyControlComboBox.SelectedIndex = page;
                Settle();
            }

            // An editor is built before its plot (axis, series, annotation) is set, and Avalonia warns of
            // the null on the way through each binding that starts from it; WPF says nothing. And as
            // upstream's, one editor serves every kind of annotation and series, so it binds properties
            // the one shown may lack (WPF traces those too; the editors hide them).
            Assert.IsTrue(errors.Messages.All(m => m.Contains("Value is null") || m.Contains("Could not find a matching property accessor")),
                string.Join(Environment.NewLine, errors.Messages));
            errors.Messages.Clear();

            foreach (Theme theme in new[] { Theme.Dark, Theme.Blue, Theme.Light })
            {
                ThemeService.Instance.SetTheme(theme);
                for (int page = 0; page < 5; page++)
                {
                    properties.PropertyControlComboBox.SelectedIndex = page;
                    Settle();
                    Assert.IsTrue(properties.PropertyControlsGrid.Children[page].Bounds.Height > 0, $"page {page} lays out");
                }
            }
            Assert.IsTrue(toolbar.Bounds.Height > 0);
            Assert.AreEqual(0, errors.Messages.Count, string.Join(Environment.NewLine, errors.Messages));
            window.Close();
        });
    }
}
