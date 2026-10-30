using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using WpfCompat.Data;

namespace WpfCompat.Tests;

/// <summary>
/// A WPF binding doesn't write back the value it has just shown; an Avalonia binding does, so a
/// converter that shows a default for a missing value (NaN as 12) replaces the value. WpfCompat's
/// TwoWayConverter and MultiBinding keep WPF's behaviour, and still write the user's edits back.
/// </summary>
[TestClass]
public sealed class TwoWayConverterTests
{
    private static MultiBindingHost Show()
    {
        var host = new MultiBindingHost();
        var window = new Window { Content = host };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return host;
    }

    [TestMethod]
    public void AvaloniasBinding_WritesBackTheValueItShows()
    {
        HeadlessSession.Run(() =>
        {
            MultiBindingHost host = Show();
            Assert.AreEqual("12", host.RawSizeText.Text);
            Assert.IsTrue(double.IsNaN(host.RawSize), "a binding's first value isn't written back");

            // Later values are, as when an editor's plot is set after its bindings start.
            host.RawSize = 5;
            host.RawSize = double.NaN;
            Assert.AreEqual("12", host.RawSizeText.Text);
            Assert.AreEqual(12, host.RawSize, "Avalonia's behaviour, which the wrapper is for: showing NaN as 12 made it 12");
            ((Window)host.Parent!).Close();
        });
    }

    [TestMethod]
    public void TheWrappedConverter_ShowsTheDefault_AndLeavesTheSourceAlone()
    {
        HeadlessSession.Run(() =>
        {
            MultiBindingHost host = Show();
            Assert.AreEqual("12", host.SizeText.Text);
            Assert.IsTrue(double.IsNaN(host.Size), "the source keeps its NaN, as in WPF");
            host.Size = 5;
            host.Size = double.NaN;
            Assert.AreEqual("12", host.SizeText.Text);
            Assert.IsTrue(double.IsNaN(host.Size), "a later NaN too");
            Assert.AreEqual("10", host.OtherSizeText.Text);
            Assert.AreEqual(10, host.OtherSize);
            ((Window)host.Parent!).Close();
        });
    }

    [TestMethod]
    public void TheWrappedConverter_WritesBackEdits_IncludingTheShownDefaultOnAnotherBinding()
    {
        HeadlessSession.Run(() =>
        {
            MultiBindingHost host = Show();
            host.SizeText.Text = "14";
            Assert.AreEqual(14, host.Size);

            // The other box shares the resource key, but x:Shared="False" gives it its own converter: 12,
            // which the first box showed, is an edit here.
            host.OtherSizeText.Text = "12";
            Assert.AreEqual(12, host.OtherSize);

            host.SizeText.Text = "12";
            Assert.AreEqual(12, host.Size, "back to the number the box first showed is an edit too");

            host.Size = double.NaN;
            Assert.AreEqual("12", host.SizeText.Text);
            Assert.IsTrue(double.IsNaN(host.Size));
            Assert.AreNotSame(host.FindResource("Size"), host.FindResource("Size"), "each lookup makes a converter");
            ((Window)host.Parent!).Close();
        });
    }

    [TestMethod]
    public void TheMultiBinding_ShowsTheDefault_AndWritesBackOnlyEdits()
    {
        HeadlessSession.Run(() =>
        {
            MultiBindingHost host = Show();
            Assert.AreEqual("12", host.MultiSizeText.Text);
            Assert.IsTrue(double.IsNaN(host.MultiSize), "the source keeps its NaN, as in WPF");
            host.MultiSize = 5;
            host.MultiSize = double.NaN;
            Assert.AreEqual("12", host.MultiSizeText.Text);
            Assert.IsTrue(double.IsNaN(host.MultiSize), "a later NaN too");

            host.MultiSizeText.Text = "16";
            Assert.AreEqual(16, host.MultiSize);
            ((Window)host.Parent!).Close();
        });
    }

    [TestMethod]
    [DataRow(12.0, "12", true)]
    [DataRow(12.0, "12.5", false)]
    [DataRow(double.NaN, double.NaN, true)]
    public void SameValue_MatchesAValueAndItsText(object a, object b, bool same) =>
        Assert.AreEqual(same, TwoWayConverter.SameValue(a, b));

    [TestMethod]
    public void SameValue_MatchesBrushesAndColoursOfTheSameColour_DashArraysOfTheSameNumbers_AndTimesInTheSameSecond()
    {
        HeadlessSession.Run(() =>
        {
            Assert.IsTrue(TwoWayConverter.SameValue(new SolidColorBrush(Colors.Red), new SolidColorBrush(Colors.Red)));
            Assert.IsTrue(TwoWayConverter.SameValue(Colors.Red, new SolidColorBrush(Colors.Red)));
            Assert.IsFalse(TwoWayConverter.SameValue(new SolidColorBrush(Colors.Red), new SolidColorBrush(Colors.Red, 0.5)));
            Assert.IsTrue(TwoWayConverter.SameValue(new Media.DoubleCollection([4, 1]), new Media.DoubleCollection([4, 1])));
            Assert.IsFalse(TwoWayConverter.SameValue(new Media.DoubleCollection([4, 1]), new Media.DoubleCollection([1, 1])));
            var time = new DateTime(1899, 12, 30, 23, 58, 33).AddMilliseconds(600);
            Assert.IsTrue(TwoWayConverter.SameValue(time, new DateTime(1899, 12, 30, 23, 58, 33)), "a picker of whole seconds");
            Assert.IsFalse(TwoWayConverter.SameValue(time, new DateTime(1899, 12, 30, 23, 58, 34)));
        });
    }
}
