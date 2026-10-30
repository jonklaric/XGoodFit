using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using GenericControls;
using Numerics.Distributions;
using NumericControls.Distributions.Univariate;
using Themes;
using WpfCompat;
using static NumericControls.Tests.TestSupport;

namespace NumericControls.Tests;

/// <summary>
/// DistributionSelectorControl, as upstream implements it: choosing a distribution, editing its parameters,
/// its density plot and summary statistics, sample data with estimation, and RMC-BestFit's use of it in a
/// popup. The settings come from upstream's NumericControls.Demo and RMC-BestFit's priors.
/// </summary>
[TestClass]
public sealed class DistributionSelectorControlTests
{
    // The demo's distribution options.
    private static List<UnivariateDistributionBase> DemoOptions() =>
    [
        UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.Deterministic),
        UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.Normal),
        UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.LnNormal),
        UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.TruncatedNormal),
        UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.Triangular),
        UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.Pert),
    ];

    // RMC-BestFit's prior distributions (InputDataControl's and ParameterPriorsControl's popups).
    private static List<UnivariateDistributionBase> BestFitPriors() =>
    [
        new GammaDistribution(), new GeneralizedBeta(), new LnNormal(), new LogNormal(), new Normal(),
        new Pert(), new StudentT(), new Triangular(), new TruncatedNormal(), new Uniform(),
    ];

    [TestMethod]
    public void DefaultDistributions_AreTheConstructibleOnes_ByName()
    {
        HeadlessSession.Run(() =>
        {
            List<UnivariateDistributionBase> defaults = DistributionSelectorControl.DefaultDistributions;
            Assert.IsFalse(defaults.Any(d => d.Type is UnivariateDistributionType.Empirical or UnivariateDistributionType.KernelDensity or UnivariateDistributionType.UserDefined));
            Assert.IsTrue(defaults.Count > 20);

            var (window, selector) = ShowSelector();
            var combo = Combo(selector);
            Assert.AreEqual(defaults.Count, combo.ItemCount);
            combo.IsDropDownOpen = true;
            Settle();
            var item = (ComboBoxItem)combo.ContainerFromIndex(0)!;
            Assert.AreEqual(defaults[0].DisplayName, item.GetVisualDescendants().OfType<TextBlock>().First().Text, "the combo box shows DisplayName");
            combo.IsDropDownOpen = false;
            window.Close();
        });
    }

    [TestMethod]
    public void ChoosingADistribution_ShowsItsParameters_DensityAndStatistics()
    {
        HeadlessSession.Run(() =>
        {
            var (window, selector) = ShowSelector(s => s.Distributions = DemoOptions());
            Assert.AreEqual(Visibility.Collapsed, selector.FindControl<TextBlock>("InvalidDistributionTextBlock")!.Visibility, "nothing is checked until a distribution is chosen");

            Combo(selector).SelectedIndex = 1;
            Settle();

            var normal = (Normal)selector.SelectedDistribution;
            Assert.AreEqual(0, normal.Mean);
            Assert.AreNotSame(selector.Distributions[1], normal, "the selected distribution is a clone");
            var parameters = Parameters(selector);
            CollectionAssert.AreEqual(new[] { "Mean (µ)", "Std Dev (σ)" }, new[] { CellText(parameters, 0, 0), CellText(parameters, 1, 0) });
            CollectionAssert.AreEqual(new[] { "0", "1" }, new[] { CellText(parameters, 0, 1), CellText(parameters, 1, 1) });
            Assert.AreEqual(Visibility.Visible, Plot(selector).Visibility);
            Assert.AreEqual(500, Density(selector).Points.Count);
            Assert.AreEqual("0.0000", Statistic(selector, "Mean"));
            Assert.AreEqual("1.0000", Statistic(selector, "Std Dev"));
            Assert.AreEqual(Visibility.Collapsed, selector.FindControl<Button>("EstimateParametersButton")!.Visibility, "no sample data");
            window.Close();
        });
    }

    [TestMethod]
    public void EditingAParameter_SelectsTheEditedDistribution()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var (window, selector) = ShowSelector(s => s.Distributions = DemoOptions());
            Combo(selector).SelectedIndex = 1;
            Settle();
            var changes = new List<UnivariateDistributionBase>();
            selector.PropertyChanged += (_, e) =>
            {
                if (e.Property == DistributionSelectorControl.SelectedDistributionProperty)
                    changes.Add((UnivariateDistributionBase)e.NewValue!);
            };

            EditParameter(window, selector, 0, "100.5");

            Assert.AreEqual(100.5, ((Normal)selector.SelectedDistribution).Mean);
            Assert.AreEqual(1, changes.Count, "one new distribution");
            Assert.AreEqual("100.5000", Statistic(selector, "Mean"));
            Assert.AreEqual("100.5", CellText(Parameters(selector), 0, 1));
            window.Close();
        });
    }

    [TestMethod]
    public void AnInvalidParameter_IsFlagged_AndTheDensityHidden()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var (window, selector) = ShowSelector(s => s.Distributions = DemoOptions());
            Combo(selector).SelectedIndex = 1;
            Settle();

            EditParameter(window, selector, 1, "-1");

            Assert.IsFalse(selector.SelectedDistribution.ParametersValid);
            DataGridCell cell = Cell(Parameters(selector), 1, 1);
            Assert.AreEqual(selector.FindResource("DataGrid.Cell.Error.Background"), cell.Background);
            StringAssert.Contains((string)ToolTip.GetTip(cell)!, "Standard deviation");
            Assert.AreEqual("Selected distribution has invalid parameters.", Text(selector, "InvalidDistributionTextBlock"));
            Assert.AreEqual(Visibility.Hidden, Plot(selector).Visibility);
            Assert.AreEqual(NumberFormatHelper.FormatDouble(double.NaN, 4, false), Statistic(selector, "Mean"));

            EditParameter(window, selector, 1, "2");
            Assert.IsTrue(selector.SelectedDistribution.ParametersValid);
            Assert.AreNotEqual(selector.FindResource("DataGrid.Cell.Error.Background"), Cell(Parameters(selector), 1, 1).Background);
            Assert.AreEqual(Visibility.Visible, Plot(selector).Visibility);
            window.Close();
        });
    }

    [TestMethod]
    public void ParameterValues_FormatInTheCurrentCulture_AsUpstreamsBindingNamesIt()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("de-DE");
            var (window, selector) = ShowSelector(s => { s.Distributions = DemoOptions(); s.SelectedDistribution = new Normal(1.5, 0.25); });
            CollectionAssert.AreEqual(new[] { "1,5", "0,25" }, new[] { CellText(Parameters(selector), 0, 1), CellText(Parameters(selector), 1, 1) });
            window.Close();
        });
    }

    [TestMethod]
    public void ParametersShownRounded_KeepTheirValue()
    {
        HeadlessSession.Run(() =>
        {
            // The grid shows four decimals; showing the rows again (as a new distribution does) mustn't write
            // those back into the parameters (see GenericControls' DataGridDisplayBinding).
            var (window, selector) = ShowSelector(s => { s.Distributions = DemoOptions(); s.SelectedDistribution = new Normal(100.24375071551533, 15); });
            Assert.AreEqual("100.2438", CellText(Parameters(selector), 0, 1));
            Assert.AreEqual(100.24375071551533, ((Normal)selector.SelectedDistribution).Mean);
            selector.SelectedDistribution = new Normal(7.123456789, 2);
            Settle();
            Assert.AreEqual(7.123456789, ((Normal)selector.SelectedDistribution).Mean);
            window.Close();
        });
    }

    [TestMethod]
    public void SettingTheSelectedDistribution_ChoosesItsType_AndKeepsIt()
    {
        HeadlessSession.Run(() =>
        {
            var priors = BestFitPriors();
            var (window, selector) = ShowSelector(s => s.Distributions = priors);
            var distribution = new Normal(100, 15);

            selector.SelectedDistribution = distribution;
            Settle();
            Assert.AreSame(distribution, selector.SelectedDistribution);
            Assert.AreSame(distribution, Combo(selector).SelectedItem);
            Assert.AreSame(distribution, priors[4], "the list's Normal is replaced");
            CollectionAssert.AreEqual(new[] { "100", "15" }, new[] { CellText(Parameters(selector), 0, 1), CellText(Parameters(selector), 1, 1) });

            // Another of the same type replaces it again, and is selected.
            var other = new Normal(5, 2);
            selector.SelectedDistribution = other;
            Settle();
            Assert.AreSame(other, Combo(selector).SelectedItem);
            Assert.AreEqual("5", CellText(Parameters(selector), 0, 1));

            // A type the list doesn't have isn't selected.
            selector.SelectedDistribution = new Deterministic(3);
            Settle();
            Assert.IsNull(selector.SelectedDistribution);
            Assert.AreEqual(-1, Combo(selector).SelectedIndex);
            Assert.AreEqual(0, Parameters(selector).Items.Count);
            window.Close();
        });
    }

    [TestMethod]
    public void SampleData_AddsAHistogram_TheDataColumn_FitStatistics_AndEstimation()
    {
        HeadlessSession.Run(() =>
        {
            // As the demo: a Normal(100, 15) with a thousand of its values as the sample.
            var (window, selector) = ShowSelector(s =>
            {
                s.Distributions = [.. DemoOptions(), new Uniform()];
                s.SelectedDistribution = new Normal(100, 15);
                s.SampleData = new Normal(100, 15).GenerateRandomValues(1000, 12345);
            });

            var plot = Plot(selector);
            Assert.AreEqual("Histogram", plot.Series[0].Name);
            Assert.IsTrue(selector.FindControl<CopyPasteDataGrid>("SummaryTable")!.Columns[2].IsVisible, "the Data column");
            Assert.AreEqual(Visibility.Visible, selector.FindControl<Button>("EstimateParametersButton")!.Visibility);
            Assert.AreEqual(15, selector.FindControl<CopyPasteDataGrid>("SummaryTable")!.Items.Count, "with RMSE, Chi-Squared and K-S");

            Click(selector.FindControl<Button>("EstimateParametersButton")!);
            var estimated = (Normal)selector.SelectedDistribution;
            var expected = new Normal(100, 15);
            expected.Estimate(selector.SampleData, ParameterEstimationMethod.MethodOfMoments);
            Assert.AreEqual(expected.Mean, estimated.Mean, "the method of moments");
            Assert.AreEqual(estimated.Mean.ToString("0.####"), CellText(Parameters(selector), 0, 1));

            // A distribution that can't be estimated (the demo's are those that can) hides the button.
            Combo(selector).SelectedIndex = 6;
            Settle();
            Assert.AreEqual(Visibility.Collapsed, selector.FindControl<Button>("EstimateParametersButton")!.Visibility);
            window.Close();
        });
    }

    [TestMethod]
    public void ShowPlotAndShowStatistics_HideTheirExpanders_AndTheAxesFollowTheirOptions()
    {
        HeadlessSession.Run(() =>
        {
            var (window, selector) = ShowSelector(s => s.Distributions = DemoOptions());
            Assert.AreEqual(Visibility.Visible, selector.FindControl<Expander>("PlotExpander")!.Visibility);
            selector.ShowPlot = false;
            selector.ShowStatistics = false;
            Assert.AreEqual(Visibility.Collapsed, selector.FindControl<Expander>("PlotExpander")!.Visibility);
            Assert.AreEqual(Visibility.Collapsed, selector.FindControl<Expander>("SummaryStatisticstExpander")!.Visibility);

            var y = Plot(selector).Axes.Single(a => a.Name == "Yaxis");
            Assert.AreEqual("Density", y.Title);
            Assert.AreEqual(5, y.FontSize, "labels are small while ShowAxisLabel is false");
            Assert.AreEqual(Color.FromArgb(0, 0, 0, 0), y.TextColor, "and transparent");
            selector.ShowAxisLabel = true;
            selector.ShowAxisTitle = false;
            Assert.IsTrue(double.IsNaN(y.FontSize));
            Assert.AreEqual("", y.Title);
            Assert.AreEqual(8, y.TitleFontSize);
            Assert.AreEqual(Color.FromArgb(0, 0, 0, 0), y.TitleColor);

            selector.ShowTickLines = false;
            Assert.AreEqual(OxyPlot.Axes.TickStyle.None, y.TickStyle);
            window.Close();
        });
    }

    [TestMethod]
    public void ThePlot_TakesTheAppsThemeAsItChanges()
    {
        HeadlessSession.Run(() =>
        {
            var (window, selector) = ShowSelector(s => s.Distributions = DemoOptions());
            var x = Plot(selector).Axes.Single(a => a.Name == "Xaxis");
            Assert.AreEqual(Colors.Black, x.TitleColor);
            try
            {
                ThemeService.Instance.SetTheme(Theme.Dark);
                Settle();
                Assert.AreEqual(Color.FromRgb(0xF1, 0xF1, 0xF1), x.TitleColor);
                Assert.AreEqual(Color.FromArgb(0, 0, 0, 0), x.TextColor, "the label override is applied again");
            }
            finally
            {
                ThemeService.Instance.SetTheme(Theme.Light);
            }
            window.Close();
        });
    }

    [TestMethod]
    public void ClickingOutsideTheParameters_ClearsTheirSelection()
    {
        HeadlessSession.Run(() =>
        {
            var (window, selector) = ShowSelector(s => { s.Distributions = DemoOptions(); s.SelectedDistribution = new Normal(100, 15); });
            var parameters = Parameters(selector);
            Select(parameters, (0, 1));
            Assert.AreEqual(1, parameters.SelectedCells.Count);

            window.ClickAt(selector.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Select Distribution for "));
            Assert.AreEqual(0, parameters.SelectedCells.Count);
            window.Close();
        });
    }

    [TestMethod]
    public void InBestFitsPopup_TheSelectorEditsTheRowsDistribution()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            UseAppResources();
            // As InputDataControl's distribution popup: the selector in a border in a popup, given the prior
            // distributions and the row's distribution when it opens, and read when it closes.
            UnivariateDistributionBase rowDistribution = new Normal(10, 2);
            var selector = new DistributionSelectorControl { Margin = new Thickness(5), Width = 300, ShowAxisTitle = true, ShowTickLines = true, ShowAxisLabel = true, ExpandPlot = false, DistributionTitle = "Uncertain Data Point" };
            var target = new ToggleButton { Content = "Normal" };
            var popup = new Popup { Placement = PlacementMode.RightEdgeAlignedTop, PlacementTarget = target, IsLightDismissEnabled = true, Child = new Border { BorderThickness = new Thickness(1), Child = selector } };
            popup.Opened += (_, _) =>
            {
                var dsc = (DistributionSelectorControl)((Border)popup.Child).Child!;
                dsc.Distributions = BestFitPriors();
                dsc.SelectedDistribution = rowDistribution.Clone();
            };
            popup.Closed += (_, _) =>
            {
                var dsc = (DistributionSelectorControl)((Border)popup.Child).Child!;
                if (dsc.SelectedDistribution.ParametersValid)
                    rowDistribution = dsc.SelectedDistribution.Clone();
                dsc.SelectedDistribution = null!;
            };
            var window = Show(new StackPanel { Children = { target, popup } }, 700, 700);

            popup.IsOpen = true;
            Settle();
            Assert.AreEqual("Uncertain Data Point", Text(selector, "", "Select Distribution for "));
            Assert.AreEqual("Normal", ((UnivariateDistributionBase)Combo(selector).SelectedItem!).DisplayName);
            EditParameter(window, selector, 0, "12");
            popup.IsOpen = false;
            Settle();

            Assert.AreEqual(12, ((Normal)rowDistribution).Mean);
            Assert.IsNull(selector.SelectedDistribution);
            window.Close();
        });
    }

    // The selector, configured and shown in a scroll viewer, as the demo shows it.
    private static (Window Window, DistributionSelectorControl Selector) ShowSelector(Action<DistributionSelectorControl>? configure = null)
    {
        UseAppResources();
        var selector = new DistributionSelectorControl { Width = 400, ShowAxisTitle = true, ShowTickLines = true, DistributionTitle = "Sample Distribution" };
        configure?.Invoke(selector);
        var window = Show(new ScrollViewer { Content = selector }, 500, 800);
        selector.FindControl<Expander>("SummaryStatisticstExpander")!.IsExpanded = true;
        Settle();
        return (window, selector);
    }

    private static ComboBox Combo(DistributionSelectorControl selector) => selector.FindControl<ComboBox>("DistributionCombobox")!;

    private static CopyPasteDataGrid Parameters(DistributionSelectorControl selector) => selector.FindControl<CopyPasteDataGrid>("ParametersTable")!;

    private static OxyPlot.Wpf.Plot Plot(DistributionSelectorControl selector) => selector.FindControl<OxyPlot.Wpf.Plot>("Plot")!;

    private static OxyPlot.Series.AreaSeries Density(DistributionSelectorControl selector) =>
        (OxyPlot.Series.AreaSeries)selector.FindControl<OxyPlot.Wpf.AreaSeries>("PDF")!.InternalSeries;

    // The text of a named text block, or of the text block after the one with this text.
    private static string? Text(DistributionSelectorControl selector, string name, string? after = null)
    {
        if (after == null)
            return selector.FindControl<TextBlock>(name)!.Text;
        var blocks = selector.GetVisualDescendants().OfType<TextBlock>().ToList();
        return blocks[blocks.FindIndex(t => t.Text == after) + 1].Text;
    }

    // The distribution's value of a summary statistic.
    private static string Statistic(DistributionSelectorControl selector, string name)
    {
        var table = selector.FindControl<CopyPasteDataGrid>("SummaryTable")!;
        var statistic = table.Items.OfType<SummaryStatistic>().Single(s => s.StatName == name);
        return statistic.DistStat;
    }

    // Edits a parameter's value as a user does: F2, the text, Enter.
    private static void EditParameter(Window window, DistributionSelectorControl selector, int row, string text)
    {
        var grid = Parameters(selector);
        grid.Focus();
        grid.CurrentCell = new DataGridCellInfo(grid.Items[row], grid.Columns[1], row, 1);
        Settle();
        window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);
        Settle();
        var box = (TextBox)Cell(grid, row, 1).Content!;
        box.SelectAll();
        window.Type(text);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Settle();
    }
}
