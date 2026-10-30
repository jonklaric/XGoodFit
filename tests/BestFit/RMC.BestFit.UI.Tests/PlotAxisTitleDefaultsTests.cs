using System.ComponentModel;
using System.Runtime.CompilerServices;
using OxyPlot.Wpf;

namespace RMC.BestFit.UI.Tests;

/// <summary>
/// XGoodFit addition: <see cref="PlotAxisTitleDefaults"/>' binding of an axis title to a data label, which
/// upstream relies on WPF to detach when the user edits the title (a local value clears a one-way
/// binding in WPF). Upstream's tests don't cover it; the App's views do this through the plot
/// properties editor, which sets the axis's Title.
/// </summary>
[TestClass]
public sealed class PlotAxisTitleDefaultsTests
{
    /// <summary>A data element's label, as InputData's and TimeSeriesElement's UnitLabel.</summary>
    private sealed class LabelSource : INotifyPropertyChanged
    {
        private string _unitLabel = "Flow (cfs)";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string UnitLabel
        {
            get => _unitLabel;
            set
            {
                _unitLabel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnitLabel)));
            }
        }
    }

    [STATestMethod]
    public void BoundTitle_FollowsTheLabel_UntilTheUserEditsIt()
    {
        var axis = new LinearAxis();
        var source = new LabelSource();

        Assert.IsTrue(PlotAxisTitleDefaults.BindTitleIfDefault(axis, source, nameof(LabelSource.UnitLabel), source.UnitLabel));
        Assert.AreEqual("Flow (cfs)", axis.Title);
        source.UnitLabel = "Flow (cms)";
        Assert.AreEqual("Flow (cms)", axis.Title, "the title follows the label");

        // The user edits the title (the plot properties editor sets it). In WPF the local value clears the
        // one-way binding, so the axis keeps the user's title from then on. Ending the binding changes
        // nothing else: the plot's undo sees the one edit.
        var titles = new List<string?>();
        ((Avalonia.AvaloniaObject)axis).PropertyChanged += (_, e) =>
        {
            if (e.Property == Axis.TitleProperty) titles.Add((string?)e.NewValue);
        };
        axis.Title = "Peak flow";
        source.UnitLabel = "Stage (ft)";
        Assert.AreEqual("Peak flow", axis.Title, "a later label change leaves the user's title");
        CollectionAssert.AreEqual(new[] { "Peak flow" }, titles, string.Join(" / ", titles.Select(t => t ?? "(null)")));
        Assert.IsFalse(PlotAxisTitleDefaults.IsDefaultManaged(axis, "Stage (ft)", "Flow (cms)"), "the edited axis isn't default-managed");
        Assert.IsFalse(PlotAxisTitleDefaults.BindTitleIfDefault(axis, source, nameof(LabelSource.UnitLabel), "Stage (ft)", "Flow (cms)"));
        Assert.IsFalse(PlotAxisTitleDefaults.SetTitleIfDefault(axis, "Stage (ft)", "Flow (cms)"));
        Assert.AreEqual("Peak flow", axis.Title);
    }

    [STATestMethod]
    public void FormattedBoundTitle_FollowsTheLabel_UntilTheUserEditsIt()
    {
        var axis = new LinearAxis();
        var source = new LabelSource();

        Assert.IsTrue(PlotAxisTitleDefaults.BindTitleIfDefault(axis, source, nameof(LabelSource.UnitLabel), "Marginal X - Flow (cfs)", stringFormat: "Marginal X - {0}"));
        Assert.AreEqual("Marginal X - Flow (cfs)", axis.Title);
        source.UnitLabel = "Flow (cms)";
        Assert.AreEqual("Marginal X - Flow (cms)", axis.Title);

        axis.Title = "Peak flow";
        source.UnitLabel = "Stage (ft)";
        Assert.AreEqual("Peak flow", axis.Title);
    }

    [STATestMethod]
    public void BlankedTitle_ReturnsToTheDefault()
    {
        // Upstream: a blank title asks for default management again.
        var axis = new LinearAxis();
        var source = new LabelSource();
        PlotAxisTitleDefaults.BindTitleIfDefault(axis, source, nameof(LabelSource.UnitLabel), source.UnitLabel);
        axis.Title = "";
        Assert.IsTrue(PlotAxisTitleDefaults.BindTitleIfDefault(axis, source, nameof(LabelSource.UnitLabel), source.UnitLabel));
        Assert.AreEqual("Flow (cfs)", axis.Title);
        source.UnitLabel = "Flow (cms)";
        Assert.AreEqual("Flow (cms)", axis.Title, "bound again");
    }

    [STATestMethod]
    public void LiteralDefault_ReplacesTheBinding()
    {
        var axis = new LinearAxis();
        var source = new LabelSource();
        PlotAxisTitleDefaults.BindTitleIfDefault(axis, source, nameof(LabelSource.UnitLabel), source.UnitLabel);

        Assert.IsTrue(PlotAxisTitleDefaults.SetTitleIfDefault(axis, "Relative Frequency", source.UnitLabel));
        Assert.AreEqual("Relative Frequency", axis.Title);
        source.UnitLabel = "Stage (ft)";
        Assert.AreEqual("Relative Frequency", axis.Title, "the literal title no longer follows the label");
        Assert.IsTrue(PlotAxisTitleDefaults.IsDefaultManaged(axis, "Something else"), "the remembered automatic title is still default-managed");
    }
}
