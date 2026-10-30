using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using WpfCompat.Input;

namespace WpfCompat.Tests;

/// <summary>WPF's mouse events and <c>Keyboard.Modifiers</c>, as WpfCompat provides them.</summary>
[TestClass]
public sealed class InputTests
{
    [TestMethod]
    public void MouseEvents_RunForWpfsButton_AndPreviewsTunnelFirst()
    {
        HeadlessSession.Run(() =>
        {
            Keyboard.Initialize();
            var inner = new Border { Width = 100, Height = 100, Background = Brushes.White };
            var outer = new Border { Child = inner, Background = Brushes.Gray };
            var window = new Window { Width = 200, Height = 200, Content = outer };
            window.Show();

            var calls = new List<string>();
            ModifierKeys modifiers = ModifierKeys.None;
            outer.AddPreviewMouseLeftButtonDownHandler((_, _) => calls.Add("outer preview left down"));
            outer.AddMouseLeftButtonDownHandler((_, _) => calls.Add("outer left down"));
            inner.AddMouseLeftButtonDownHandler((_, e) =>
            {
                calls.Add($"inner {e.ChangedButton} down");
                modifiers = Keyboard.Modifiers;
            });
            inner.AddMouseRightButtonUpHandler((_, e) => calls.Add($"inner {e.ChangedButton} up"));

            var center = new Point(100, 100);
            window.MouseDown(center, MouseButton.Left, RawInputModifiers.Shift);
            window.MouseUp(center, MouseButton.Left);
            window.MouseDown(center, MouseButton.Right);
            window.MouseUp(center, MouseButton.Right);

            CollectionAssert.AreEqual(
                new[] { "outer preview left down", "inner Left down", "outer left down", "inner Right up" }, calls,
                "Preview tunnels before the bubbling events; each handler sees only its button");
            Assert.AreEqual(ModifierKeys.Shift, modifiers, "Keyboard.Modifiers follows the input");
            window.Close();
        });
    }

    [TestMethod]
    public void RemoveHandler_RemovesTheWrapperItAdded()
    {
        HeadlessSession.Run(() =>
        {
            var border = new Border { Width = 100, Height = 100, Background = Brushes.White };
            var window = new Window { Width = 100, Height = 100, Content = border };
            window.Show();
            int count = 0;
            EventHandler<PointerPressedEventArgs> handler = (_, _) => count++;
            border.AddPreviewMouseLeftButtonDownHandler(handler);
            window.MouseDown(new Point(50, 50), MouseButton.Left);
            window.MouseUp(new Point(50, 50), MouseButton.Left);
            border.RemovePreviewMouseLeftButtonDownHandler(handler);
            window.MouseDown(new Point(50, 50), MouseButton.Left);
            window.MouseUp(new Point(50, 50), MouseButton.Left);
            Assert.AreEqual(1, count);
            window.Close();
        });
    }
}
