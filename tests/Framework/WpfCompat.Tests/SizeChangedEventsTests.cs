using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace WpfCompat.Tests;

/// <summary>
/// WpfCompat's WPF-timed SizeChanged: the handler runs after the layout pass, when the element's parents
/// have their new sizes too, as WPF's does.
/// </summary>
[TestClass]
public sealed class SizeChangedEventsTests
{
    [TestMethod]
    public void SizeChanged_ComesAfterTheLayoutPass_WhenTheParentsHaveTheirNewSizes()
    {
        HeadlessSession.Run(() =>
        {
            var child = new Border();
            var parent = new Border { Width = 100, Height = 20, Child = child };
            var window = new Window { Width = 400, Height = 100, Content = new StackPanel { Children = { parent } } };
            var seen = new List<(double ParentWidth, Size Previous, Size New)>();
            EventHandler<SizeChangedEventArgs> handler = (sender, e) =>
            {
                Assert.AreSame(child, sender);
                seen.Add((parent.Bounds.Width, e.PreviousSize, e.NewSize));
            };
            child.AddSizeChangedHandler(handler);

            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual(1, seen.Count);
            Assert.AreEqual((100d, new Size(0, 0), new Size(100, 20)), seen[0], "the parent has its size when the handler runs");

            parent.Width = 150;
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual(2, seen.Count);
            Assert.AreEqual((150d, new Size(100, 20), new Size(150, 20)), seen[1]);

            child.RemoveSizeChangedHandler(handler);
            parent.Width = 120;
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual(2, seen.Count, "a removed handler isn't called");
            window.Close();
        });
    }
}
