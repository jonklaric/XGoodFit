using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkInterfaces.Messaging;
using FrameworkUI.MessageWindow;

namespace FrameworkUI.Tests;

/// <summary>
/// The Message Window: the messenger's messages in its grid, the type filters and their counts, and
/// each type's colour.
/// </summary>
[TestClass]
public sealed class MessageWindowTests
{
    private static (Window Window, MessageWindowControl Control) Show()
    {
        var control = new MessageWindowControl();
        var window = new Window { Width = 900, Height = 300, Content = control };
        window.Show();
        TestInput.Frame();
        return (window, control);
    }

    // The rows the grid shows, top to bottom.
    private static List<DataGridRow> Rows(MessageWindowControl control)
    {
        TestInput.Frame();
        return control.MyDataGrid.GetVisualDescendants().OfType<DataGridRow>()
            .Where(r => r.IsVisible && r.DataContext is MessageItem)
            .OrderBy(r => r.Index).ToList();
    }

    private static string[] Shown(MessageWindowControl control) =>
        Rows(control).Select(r => ((MessageItem)r.DataContext!).Message.Description ?? string.Empty).ToArray();

    private static void Click(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        TestInput.Frame();
    }

    // The element the messages are about: the messenger ignores messages without a source, and a
    // source's repeats of a message (one code) other than events.
    private static readonly object Source = new();

    private static BasicMessageItem Message(MessageType type, string description) =>
        new(type, description, Source, "Time Series Data", "Daily Discharge", "Values", "TEST-" + description);

    [TestMethod]
    public void Filters_ShowErrorsAndWarnings_AndTheButtonsShowOrHideEachType()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            var messenger = Messenger.GetInstance();
            messenger.Clear();
            var (window, control) = Show();
            try
            {
                messenger.Add(Message(MessageType.Error, "First error"));
                messenger.Add(Message(MessageType.Warning, "A warning"));
                messenger.Add(Message(MessageType.Message, "A message"));
                messenger.Add(Message(MessageType.Event, "An event"));
                messenger.Add(Message(MessageType.Error, "Second error"));

                CollectionAssert.AreEqual(new[] { "First error", "A warning", "Second error" }, Shown(control), "errors and warnings show at first");
                Assert.AreEqual(" 2 Errors", control.ErrorsButtonText.Text);
                Assert.AreEqual(" 1 Warnings", control.WarningsButtonText.Text);
                Assert.AreEqual(" 0 of 1 Messages", control.MessageButtonText.Text);
                Assert.AreEqual("0 of 1 Events", control.EventsButtonText.Text, "(upstream's events count has no leading space)");
                Assert.IsTrue(control.ClearFiltersButton.IsEnabled, "some types are filtered out");

                Click(control.EventsButton);
                CollectionAssert.AreEqual(new[] { "First error", "A warning", "An event", "Second error" }, Shown(control), "the Events button shows the events");
                Assert.AreEqual("0 of 1 Events", control.EventsButtonText.Text, "(upstream updates the counts only as messages come and go)");

                Click(control.ErrorsButton);
                CollectionAssert.AreEqual(new[] { "A warning", "An event" }, Shown(control), "the Errors button hides the errors");
                messenger.Add(Message(MessageType.Message, "Another message"));
                Assert.AreEqual(" 0 of 2 Errors", control.ErrorsButtonText.Text, "the counts follow the filters once a message comes");
                Assert.AreEqual("1 Events", control.EventsButtonText.Text);

                Click(control.ClearFiltersButton);
                CollectionAssert.AreEqual(new[] { "First error", "A warning", "A message", "An event", "Second error", "Another message" }, Shown(control), "Clear All Filters shows every type");
                Assert.IsFalse(control.ClearFiltersButton.IsEnabled);

                messenger.Clear();
                Assert.AreEqual(0, Shown(control).Length, "clearing the messenger empties the window");
                Assert.AreEqual(" 0 Errors", control.ErrorsButtonText.Text);
            }
            finally
            {
                messenger.Clear();
                window.Close();
            }
        });
    }

    [TestMethod]
    public void Rows_TakeTheirTypesColour_AndFollowItsChanges()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            var messenger = Messenger.GetInstance();
            messenger.Clear();
            ISolidColorBrush errorColor = messenger.ErrorColor, warningColor = messenger.WarningColor;
            var (window, control) = Show();
            try
            {
                messenger.ErrorColor = new SolidColorBrush(Colors.Firebrick);
                messenger.WarningColor = new SolidColorBrush(Colors.DarkOrange);
                messenger.Add(Message(MessageType.Error, "Disk full"));
                messenger.Add(Message(MessageType.Warning, "Short record"));

                Color TextColour(string description) => ((ISolidColorBrush)Rows(control)
                    .SelectMany(r => r.GetVisualDescendants().OfType<TextBlock>()).First(t => t.Text == description).Foreground!).Color;
                Assert.AreEqual(Colors.Firebrick, TextColour("Disk full"), "an error's text has the error colour");
                Assert.AreEqual(Colors.DarkOrange, TextColour("Short record"), "a warning's the warning colour");
                Assert.IsTrue(Rows(control).All(r => r.GetVisualDescendants().OfType<Image>().Any(i => i.Source != null)), "each row shows its type's icon");

                // As Tools > Options sets them.
                messenger.ErrorColor = new SolidColorBrush(Colors.DarkViolet);
                Assert.AreEqual(Colors.DarkViolet, TextColour("Disk full"), "the rows follow a new colour");
            }
            finally
            {
                messenger.ErrorColor = (SolidColorBrush)errorColor;
                messenger.WarningColor = (SolidColorBrush)warningColor;
                messenger.Clear();
                window.Close();
            }
        });
    }
}
