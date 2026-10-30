using Avalonia.Controls;

namespace WpfCompat.Tests;

/// <summary>
/// WpfCompat's <see cref="ContentHosts.TakeContent"/> moves a control between content controls, as WPF's
/// <c>Content</c> does, where Avalonia's throws.
/// </summary>
[TestClass]
public sealed class ContentHostsTests
{
    [TestMethod]
    public void TakeContent_MovesAControlThatAnotherHostShows()
    {
        HeadlessSession.Run(() =>
        {
            var button = new Button();
            var first = new ContentControl { Content = button };
            var second = new ContentControl();
            var window = new Window { Content = new StackPanel { Children = { first, second } } };
            window.Show();
            try
            {
                window.UpdateLayout();
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    second.Content = button;
                    window.UpdateLayout();
                }, "Avalonia's own assignment throws");
                second.Content = null;
                window.UpdateLayout();

                second.TakeContent(button);
                window.UpdateLayout();
                Assert.IsNull(first.Content, "taken from the first host");
                Assert.AreSame(second, button.Parent);
                Assert.IsTrue(Avalonia.VisualTree.VisualExtensions.IsAttachedToVisualTree(button));

                second.TakeContent(button);
                Assert.AreSame(button, second.Content, "taking its own content changes nothing");
            }
            finally
            {
                window.Close();
            }
        });
    }
}
