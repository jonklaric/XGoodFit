using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using MultiBinding = WpfCompat.Data.MultiBinding;

namespace WpfCompat.Tests;

/// <summary>
/// WpfCompat's MultiBinding: WPF's two-way multi-binding, written in WPF's element syntax, converts its
/// bindings' values to the target and the target's edits back to the two-way bindings.
/// </summary>
[TestClass]
public sealed class MultiBindingTests
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
    public void TheTarget_TakesTheConvertedValues_AndFollowsTheirChanges()
    {
        HeadlessSession.Run(() =>
        {
            MultiBindingHost host = Show();
            Assert.AreEqual("a|b|c", host.TwoWay.Text, "the element names resolve as on the element itself");
            Assert.AreEqual("a|b|c", host.OneWay.Text);

            host.Second = "B";
            Assert.AreEqual("a|B|c", host.TwoWay.Text);
            Assert.AreEqual("a|B|c", host.OneWay.Text);
            Assert.AreEqual(FormattingCulture.Current, JoinConverter.Cultures[^1], "WPF's bindings convert with en-US");
            ((Window)host.Parent!).Close();
        });
    }

    [TestMethod]
    public void EditingTheTarget_WritesBackToTheTwoWayBindings_Only()
    {
        HeadlessSession.Run(() =>
        {
            MultiBindingHost host = Show();
            host.TwoWay.Text = "x|y|z";
            Assert.AreEqual("x", host.First, "a two-way binding");
            Assert.AreEqual("b", host.Second, "a one-way binding keeps its source");
            Assert.AreEqual("z", host.Third, "a Default binding takes the multi-binding's mode, as in WPF");
            Assert.AreEqual("x|b|z", host.OneWay.Text, "the other binding follows the sources");

            host.TwoWay.Text = "-|y|w";
            Assert.AreEqual("x", host.First, "DoNothing leaves a binding as it is");
            Assert.AreEqual("w", host.Third);

            host.OneWay.Text = "p|q|r";
            Assert.AreEqual("x", host.First, "a one-way multi-binding never writes back");
            ((Window)host.Parent!).Close();
        });
    }

    [TestMethod]
    public void TheBindingCanBeMadeInCode()
    {
        HeadlessSession.Run(() =>
        {
            var source = new TextBlock { Text = "left" };
            var target = new TextBox();
            var multi = new MultiBinding { Converter = new JoinConverter(), Mode = BindingMode.TwoWay };
            multi.Bindings.Add(new Binding(nameof(TextBlock.Text)) { Source = source, Mode = BindingMode.TwoWay });
            multi.Bindings.Add(new Binding(nameof(TextBlock.Tag)) { Source = source, Mode = BindingMode.OneWay });
            target.Bind(TextBox.TextProperty, multi.CreateBinding(target, TextBox.TextProperty));
            Assert.AreEqual("left|?", target.Text);

            target.Text = "right|tag";
            Assert.AreEqual("right", source.Text);
            Assert.IsNull(source.Tag, "a one-way binding keeps its source");
        });
    }
}
