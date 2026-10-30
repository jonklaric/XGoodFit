using System.ComponentModel;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using GenericControls;
using OxyPlot;
using OxyPlot.Legends;
using OxyPlot.Wpf.Serialization;
using static OxyPlotControls.Tests.PlotTesting;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls.Tests;

/// <summary>
/// The property editors change the plot, its axes, series and annotations through their properties,
/// which raise the change notifications BestFit's undo records (PlotUndoManager follows the
/// serializers' visual properties), and show changes made elsewhere. Opening them changes nothing.
/// </summary>
[TestClass]
public sealed class EditorTests
{
    private const int GeneralPage = 0, LegendPage = 1, AxesPage = 2, SeriesPage = 3, AnnotationsPage = 4;

    // The properties PlotUndoManager records: the serializers' visual properties, less the theming ones.
    private static readonly HashSet<string> Theming = ["Foreground", "Background", "IsEnabled"];

    /// <summary>The changes the plot and its elements report, as BestFit's undo would see them.</summary>
    private sealed class Changes
    {
        public List<string> Recorded { get; } = [];

        public Changes(Wpf.Plot plot)
        {
            Follow(plot, "Plot", PlotSerializer.PlotVisualProperties);
            foreach (var axis in plot.Axes)
            {
                Follow(axis, "Axis", AxisSerializer.AxisVisualProperties);
            }
            foreach (var series in plot.Series)
            {
                Follow(series, "Series", SeriesSerializer.SeriesVisualProperties);
            }
            foreach (var annotation in plot.Annotations)
            {
                Follow(annotation, "Annotation", AnnotationSerializer.AnnotationVisualProperties);
            }
        }

        private void Follow(INotifyPropertyChanged element, string kind, IReadOnlyList<string> tracked) =>
            element.PropertyChanged += (_, e) =>
            {
                if (tracked.Contains(e.PropertyName!) && !Theming.Contains(e.PropertyName!))
                {
                    Recorded.Add(kind + "." + e.PropertyName);
                }
            };
    }

    private static T Page<T>(OxyPlotPropertiesControl properties, int index) where T : Control
    {
        properties.PropertyControlComboBox.SelectedIndex = index;
        Settle();
        return properties.PropertyControlsGrid.Children.OfType<T>().Single(c => c.IsVisible);
    }

    // The editor of a type whose Title is title, among a control's logical descendants (the expanders'
    // and tabs' content is there even while it isn't shown).
    private static T Editor<T>(Control within, string title) where T : Control =>
        within.GetLogicalDescendants().OfType<T>().First(c => (string?)c.GetValue(PropertyControlTitle(c)) == title);

    private static AvaloniaProperty PropertyControlTitle(Control control) =>
        AvaloniaPropertyRegistry.Instance.FindRegistered(control, "Title")!;

    private static Wpf.Plot AnnotatedPlot()
    {
        var plot = LinePlot();
        plot.Annotations.Add(new Wpf.TextAnnotation { Text = "Peak", TextPosition = new DataPoint(0, 0.4) });
        plot.Annotations.Add(new Wpf.LineAnnotation { Type = OxyPlot.Annotations.LineAnnotationType.Horizontal, Y = 0.2, Text = "Threshold" });
        return plot;
    }

    [TestMethod]
    public void OpeningEveryEditor_ChangesNothing_ButUpstreamsStringFormat()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, _, properties) = ShowDemo(AnnotatedPlot());
            string before = OxyPlotSettingsSerializer.ToXelement(plot).ToString();
            var changes = new Changes(plot);

            Page<GeneralPlotControl>(properties, GeneralPage);
            Page<LegendControl>(properties, LegendPage);
            var axes = Page<AxesControl>(properties, AxesPage);
            for (int i = 0; i < plot.Axes.Count; i++)
            {
                axes.AxesPropertyControlComboBox.SelectedIndex = i;
                Settle();
                foreach (TabItem tab in axes.AxisPropertiesControl.AxisTabControl.Items.OfType<TabItem>())
                {
                    tab.IsSelected = true;
                    Settle();
                }
            }
            var series = Page<SeriesSelectorControl>(properties, SeriesPage);
            for (int i = 0; i < plot.Series.Count; i++)
            {
                series.SeriesPropertyControlComboBox.SelectedIndex = i;
                Settle();
            }
            var annotations = Page<AnnotationSelectorControl>(properties, AnnotationsPage);
            for (int i = 0; i < plot.Annotations.Count; i++)
            {
                annotations.AnnotationPropertyControlComboBox.SelectedIndex = i;
                Settle();
            }

            // Upstream's one change: the axis editor syncs its label format controls from the axis without
            // its guard, so a null StringFormat becomes empty (the same in WPF).
            Assert.IsTrue(changes.Recorded.All(c => c == "Axis.StringFormat"), string.Join(", ", changes.Recorded));
            Assert.AreEqual("", plot.Axes[0].StringFormat, "the first axis the editor shows");
            Assert.AreEqual(before, OxyPlotSettingsSerializer.ToXelement(plot).ToString().Replace(" StringFormat=\"\"", ""), "and nothing else in the settings");
            window.Close();
        });
    }

    /// <summary>
    /// Every axis, series and annotation type the serializers know (OxyPlot.Wpf.Tests' EveryElement),
    /// each shown in its editor: none is changed, but for upstream's StringFormat (see above).
    /// </summary>
    [TestMethod]
    public void ShowingEveryKindOfElement_ChangesNothing_ButUpstreamsStringFormat()
    {
        HeadlessSession.Run(() =>
        {
            var plot = OxyPlot.Wpf.Tests.TestPlots.EveryElement();
            var (window, _, _, properties) = ShowDemo(plot);
            string before = OxyPlotSettingsSerializer.ToXelement(plot).ToString();
            var changes = new Changes(plot);

            var axes = Page<AxesControl>(properties, AxesPage);
            for (int i = 0; i < plot.Axes.Count; i++)
            {
                axes.AxesPropertyControlComboBox.SelectedIndex = i;
                Settle();
            }
            var series = Page<SeriesSelectorControl>(properties, SeriesPage);
            for (int i = 0; i < plot.Series.Count; i++)
            {
                series.SeriesPropertyControlComboBox.SelectedIndex = i;
                Settle();
            }
            var annotations = Page<AnnotationSelectorControl>(properties, AnnotationsPage);
            for (int i = 0; i < plot.Annotations.Count; i++)
            {
                annotations.AnnotationPropertyControlComboBox.SelectedIndex = i;
                Settle();
            }

            Assert.IsTrue(changes.Recorded.All(c => c == "Axis.StringFormat"), string.Join(", ", changes.Recorded));
            string after = OxyPlotSettingsSerializer.ToXelement(plot).ToString().Replace(" StringFormat=\"\"", "");
            Assert.AreEqual(before.Replace(" StringFormat=\"\"", ""), after);
            window.Close();
        });
    }

    [TestMethod]
    public void GeneralEditor_TypingATitle_RetitlesThePlot()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, _, properties) = ShowDemo();
            var general = Page<GeneralPlotControl>(properties, GeneralPage);
            var changes = new Changes(plot);

            TextBox box = general.TitleTextControl.GetVisualDescendants().OfType<TextBox>().First();
            box.Focus();
            box.SelectAll();
            window.KeyTextInput("Annual peaks");
            Settle();

            Assert.AreEqual("Annual peaks", plot.Title);
            Assert.AreEqual("Annual peaks", plot.ActualModel.Title, "the model follows");
            Assert.IsTrue(changes.Recorded.Count > 0 && changes.Recorded.All(c => c == "Plot.Title"), string.Join(", ", changes.Recorded));
            window.Close();
        });
    }

    [TestMethod]
    public void GeneralEditor_ShowsChangesMadeElsewhere()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, _, properties) = ShowDemo();
            var general = Page<GeneralPlotControl>(properties, GeneralPage);

            plot.Title = "Retitled";
            plot.TitleFontSize = 24;
            Settle();

            Assert.AreEqual("Retitled", general.TitleTextControl.Text);
            Assert.AreEqual(24, general.TitleFontSizeControl.SelectedNumber);
            window.Close();
        });
    }

    [TestMethod]
    public void GeneralEditor_ColourSizeAndWeight_ChangeTheTitle()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, _, properties) = ShowDemo();
            var general = Page<GeneralPlotControl>(properties, GeneralPage);
            var changes = new Changes(plot);

            general.TitleColorControl.SelectedColor = new SolidColorBrush(Colors.DarkRed);
            general.TitleFontSizeControl.SelectedNumber = 22;
            general.TitleFontWeightControl.SelectedFontWeight = FontWeight.Normal;
            Settle();

            Assert.AreEqual(Colors.DarkRed, plot.TitleColor);
            Assert.AreEqual(22, plot.TitleFontSize);
            Assert.AreEqual(FontWeight.Normal, plot.TitleFontWeight);
            CollectionAssert.AreEquivalent(new[] { "Plot.TitleColor", "Plot.TitleFontSize", "Plot.TitleFontWeight" }, changes.Recorded.Distinct().ToArray(), string.Join(", ", changes.Recorded));
            window.Close();
        });
    }

    [TestMethod]
    public void LegendEditor_PositionChoice_MovesTheLegend()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, _, properties) = ShowDemo();
            var legend = Page<LegendControl>(properties, LegendPage);
            var changes = new Changes(plot);

            var position = (ComboBox)legend.LegendPositionControl.InnerContent;
            Assert.AreEqual(LegendPosition.RightBottom, position.SelectedItem, "the editor shows the plot's position");
            position.SelectedItem = LegendPosition.TopLeft;
            Settle();

            Assert.AreEqual(LegendPosition.TopLeft, plot.LegendPosition);
            CollectionAssert.AreEqual(new[] { "Plot.LegendPosition" }, changes.Recorded.Distinct().ToArray(), string.Join(", ", changes.Recorded));
            window.Close();
        });
    }

    [TestMethod]
    public void AxisEditor_TitleAndRange_ChangeTheAxis()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, _, properties) = ShowDemo();
            var axes = Page<AxesControl>(properties, AxesPage);
            axes.AxesPropertyControlComboBox.SelectedIndex = 0;
            Settle();
            Wpf.Axis axis = axes.AxisPropertiesControl.Axis;
            Assert.AreSame(plot.Axes[0], axis);
            var changes = new Changes(plot);

            Editor<TextPropertyControl>(axes.AxisPropertiesControl, "Text").Text = "Discharge";
            axes.AxisPropertiesControl.AxisMinimum.Number = -3;
            Settle();

            Assert.AreEqual("Discharge", axis.Title);
            Assert.AreEqual(-3, axis.Minimum);
            Assert.AreEqual(-3, plot.ActualModel.Axes[0].ActualMinimum, 1e-9, "the plot redraws with the new range");
            // As upstream's: an axis reports no change of its range (Axis.MinMaxChanged raises none), so
            // only the title reaches BestFit's undo.
            CollectionAssert.AreEquivalent(new[] { "Axis.Title" }, changes.Recorded.Distinct().ToArray(), string.Join(", ", changes.Recorded));
            window.Close();
        });
    }

    /// <summary>The reverse check box is a two-way MultiBinding (WpfCompat's) over the axis's start and end positions.</summary>
    [TestMethod]
    public void AxisEditor_Reverse_SwapsTheAxisPositions_AndBack()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, _, properties) = ShowDemo();
            var axes = Page<AxesControl>(properties, AxesPage);
            axes.AxesPropertyControlComboBox.SelectedIndex = 1;
            Settle();
            Wpf.Axis axis = plot.Axes[1];
            Assert.AreSame(axis, axes.AxisPropertiesControl.Axis);
            var reverse = axes.AxisPropertiesControl.ReverseBooleanControl;
            Assert.IsFalse(reverse.IsSelected);
            var changes = new Changes(plot);

            reverse.IsSelected = true;
            Settle();
            Assert.AreEqual(1, axis.StartPosition);
            Assert.AreEqual(0, axis.EndPosition);
            Assert.IsTrue(plot.ActualModel.Axes[1].IsReversed, "the plot draws the axis reversed");

            reverse.IsSelected = false;
            Settle();
            Assert.AreEqual(0, axis.StartPosition);
            Assert.AreEqual(1, axis.EndPosition);
            CollectionAssert.AreEquivalent(new[] { "Axis.StartPosition", "Axis.EndPosition" }, changes.Recorded.Distinct().ToArray(), string.Join(", ", changes.Recorded));

            axis.StartPosition = 1;
            axis.EndPosition = 0;
            Settle();
            Assert.IsTrue(reverse.IsSelected, "the check box follows the axis");
            window.Close();
        });
    }

    [TestMethod]
    public void SeriesEditor_TitleColourAndThickness_ChangeTheSeries()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, _, properties) = ShowDemo();
            var selector = Page<SeriesSelectorControl>(properties, SeriesPage);
            selector.SeriesPropertyControlComboBox.SelectedIndex = 1;
            Settle();
            var series = (Wpf.LineSeries)plot.Series[1];
            var changes = new Changes(plot);

            Editor<TextPropertyControl>(selector, "Title").Text = "Narrow";
            Editor<ColorPropertyControl>(selector, "Line Color").SelectedColor = new SolidColorBrush(Colors.Purple);
            Editor<LineWidthSelectorControl>(selector, "Line Thickness").SelectedWidth = 4;
            Settle();

            Assert.AreEqual("Narrow", series.Title);
            Assert.AreEqual(Colors.Purple, series.Color);
            Assert.AreEqual(4, series.StrokeThickness);
            Assert.AreEqual(OxyColors.Purple, ((OxyPlot.Series.LineSeries)series.InternalSeries).Color, "the plot draws the new colour");
            CollectionAssert.AreEquivalent(new[] { "Series.Title", "Series.Color", "Series.StrokeThickness" }, changes.Recorded.Distinct().ToArray(), string.Join(", ", changes.Recorded));
            Assert.AreEqual("Test Line Series 1", plot.Series[0].Title, "the other series is left alone");
            window.Close();
        });
    }

    [TestMethod]
    public void AnnotationEditor_Text_ChangesTheAnnotation()
    {
        HeadlessSession.Run(() =>
        {
            var (window, plot, _, properties) = ShowDemo(AnnotatedPlot());
            var selector = Page<AnnotationSelectorControl>(properties, AnnotationsPage);
            selector.AnnotationPropertyControlComboBox.SelectedIndex = 1;
            Settle();
            var line = (Wpf.LineAnnotation)plot.Annotations[1];
            Assert.AreSame(line, selector.AnnotationPropertiesControl.Annotation);
            var changes = new Changes(plot);

            Editor<TextPropertyControl>(selector.AnnotationPropertiesControl, "Text").Text = "Flood stage";
            Settle();

            Assert.AreEqual("Flood stage", line.Text);
            Assert.AreEqual("Peak", ((Wpf.TextAnnotation)plot.Annotations[0]).Text);
            CollectionAssert.AreEqual(new[] { "Annotation.Text" }, changes.Recorded.Distinct().ToArray(), string.Join(", ", changes.Recorded));
            window.Close();
        });
    }
}
