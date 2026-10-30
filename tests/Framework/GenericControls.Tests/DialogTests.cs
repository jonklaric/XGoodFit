using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Themes;
using WpfCompat;
using WpfCompat.Input;

namespace GenericControls.Tests;

/// <summary>
/// The themed MessageBox and NameDialog: they wait for the user as WPF's do, and return what the user
/// chose. Each test answers the dialog from a handler that runs when the dialog opens.
/// </summary>
[TestClass]
public sealed class DialogTests
{
    private static bool _resourcesAdded;

    // The app's resources, as upstream's App.xaml merges them: Themes, then GenericControls' theme.
    private static void UseAppResources()
    {
        ThemeService.Instance.Initialize(Theme.Light);
        if (!_resourcesAdded)
        {
            Application.Current!.Resources.MergedDictionaries.Add(
                new ResourceInclude(new Uri("avares://GenericControls.Tests"))
                {
                    Source = new Uri("avares://GenericControls/Themes/GenericControlsTheme.axaml"),
                });
            _resourcesAdded = true;
        }
    }

    // Runs answer on the next dialog of type T that opens, once its content is ready.
    private static IDisposable WhenOpened<T>(Action<T> answer) where T : Window
    {
        IDisposable? handler = null;
        handler = Window.WindowOpenedEvent.AddClassHandler<T>((window, _) =>
        {
            handler?.Dispose();
            Dispatcher.UIThread.Post(() => answer(window), DispatcherPriority.Background);
        });
        return handler;
    }

    private static Button ButtonWithContent(Window window, string content) =>
        window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, content));

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    [TestMethod]
    public void MessageBox_ShowsTheMessage_AndReturnsTheButtonClicked()
    {
        HeadlessSession.Run(() =>
        {
            UseAppResources();
            string[]? buttons = null;
            IImage? icon = null;
            using var _ = WhenOpened<MessageBoxWindow>(window =>
            {
                Assert.AreEqual("Delete?", window.Title);
                Assert.AreEqual("Delete the analysis?", window.MessageText.Text);
                buttons = window.ButtonPanel.Children.OfType<Button>().Select(b => (string)b.Content!).ToArray();
                icon = window.IconImage.IsVisible ? window.IconImage.Source : null;
                Click(ButtonWithContent(window, "No"));
            });

            MessageBoxResult result = MessageBox.Show("Delete the analysis?", "Delete?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            Assert.AreEqual(MessageBoxResult.No, result);
            CollectionAssert.AreEqual(new[] { "Yes", "No", "Cancel" }, buttons);
            Assert.IsInstanceOfType<DrawingImage>(icon, "the question icon from GenericControls' icons");
        });
    }

    [TestMethod]
    public void MessageBox_CloseButton_GivesCancel_ButIsIgnoredForYesNo()
    {
        HeadlessSession.Run(() =>
        {
            UseAppResources();
            using (WhenOpened<MessageBoxWindow>(window => SystemCommands.CloseWindowCommand.Execute(null, window)))
            {
                Assert.AreEqual(MessageBoxResult.Cancel, MessageBox.Show("Save?", "Save", MessageBoxButton.OKCancel));
            }

            bool stillOpen = false;
            using (WhenOpened<MessageBoxWindow>(window =>
            {
                // As the native message box, Yes/No can't be closed without an answer.
                SystemCommands.CloseWindowCommand.Execute(null, window);
                stillOpen = window.IsVisible;
                Click(ButtonWithContent(window, "Yes"));
            }))
            {
                Assert.AreEqual(MessageBoxResult.Yes, MessageBox.Show("Continue?", "Continue", MessageBoxButton.YesNo));
            }
            Assert.IsTrue(stillOpen);
        });
    }

    [TestMethod]
    public void MessageBox_DontShowAgain_StoresTheChoice_AndThenReturnsTheDefault()
    {
        HeadlessSession.Run(() =>
        {
            UseAppResources();
            var store = new Dictionary<string, bool>();
            bool checkBoxShown = false;
            using (WhenOpened<MessageBoxWindow>(window =>
            {
                checkBoxShown = window.SuppressCheckBox.IsVisible;
                window.SuppressCheckBox.IsChecked = true;
                Click(ButtonWithContent(window, "OK"));
            }))
            {
                Assert.AreEqual(MessageBoxResult.OK, MessageBox.Show("Tip", "Tip", MessageBoxButton.OK,
                    MessageBoxImage.Information, "ShowTip", store, MessageBoxResult.OK));
            }
            Assert.IsTrue(checkBoxShown);
            Assert.IsTrue(store["ShowTip"]);

            // Suppressed: no dialog opens, and the default comes back.
            using (WhenOpened<MessageBoxWindow>(_ => Assert.Fail("a suppressed message shows no dialog")))
            {
                Assert.AreEqual(MessageBoxResult.Cancel, MessageBox.Show("Tip", "Tip", MessageBoxButton.OK,
                    MessageBoxImage.Information, "ShowTip", store, MessageBoxResult.Cancel));
            }
        });
    }

    [TestMethod]
    public void NameDialog_ValidatesTheName_AndOkReturnsIt()
    {
        HeadlessSession.Run(() =>
        {
            UseAppResources();
            var dialog = new NameDialog(10, "Taken", false, ["Taken"]);
            bool takenWasValid = true;
            bool borderShowedError = false;
            object? toolTip = null;
            using var _ = WhenOpened<NameDialog>(window =>
            {
                takenWasValid = window.NameTBox.IsValid;
                borderShowedError = window.NameTBox.NameBorder.Classes.Contains("invalid");
                toolTip = window.NameTBox.NameTBox.ToolTip;

                window.NameTBox.NameTBox.Text = "Fresh name";
                Assert.IsTrue(window.NameTBox.IsValid, "a new name under the limit is valid");
                Assert.AreEqual("Fresh name", window.Text, "the text box writes to the dialog's Text");
                Assert.IsFalse(window.NameTBox.NameBorder.Classes.Contains("invalid"));
                Click(window.OKButton);
            });

            Assert.IsTrue(dialog.ShowDialog());
            Assert.IsFalse(takenWasValid, "an existing name is invalid");
            Assert.IsTrue(borderShowedError, "and the border shows the error");
            Assert.AreEqual("Name entered already exists and must be unique.", toolTip);
            Assert.AreEqual("Fresh name", dialog.Text);
        });
    }

    [TestMethod]
    public void NameTextBox_RejectsBlankLongAndInvalidNames()
    {
        HeadlessSession.Run(() =>
        {
            var box = new NameTextBox { CharacterLimit = 5 };
            box.Text = "";
            Assert.IsFalse(box.IsValid, "blank");
            box.CanBeBlank = true;
            Assert.IsTrue(box.IsValid, "blank allowed");
            box.Text = "Too long";
            Assert.IsFalse(box.IsValid);
            box.Text = "a[b]";
            CollectionAssert.AreEqual(new[] { "Invalid character in name: '['", "Invalid character in name: ']'" }, box.GetErrorMessages());
        });
    }

    [TestMethod]
    public void Resources_HaveTheOptionArraysAndIcons()
    {
        HeadlessSession.Run(() =>
        {
            var resources = (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri("avares://GenericControls/Resources/ResourceDictionary.axaml"));
            Assert.IsTrue(resources.TryGetResource("LineWidthOptions", null, out object? widths));
            CollectionAssert.AreEqual(new[] { 0d, 1, 2, 3, 4, 5, 7, 10 }, (double[])widths!);
            Assert.IsTrue(resources.TryGetResource("AngleOptions", null, out object? angles));
            Assert.AreEqual(25, ((double[])angles!).Length);

            Assert.IsTrue(resources.TryGetResource("MessageBoxErrorIcon", null, out object? error));
            Assert.IsInstanceOfType<DrawingImage>(error);
            resources.TryGetResource("AddRowIcon", null, out object? first);
            resources.TryGetResource("AddRowIcon", null, out object? second);
            Assert.IsInstanceOfType<Viewbox>(first);
            Assert.AreNotSame(first, second, "x:Shared=False icons are a new element each time");
        });
    }
}
