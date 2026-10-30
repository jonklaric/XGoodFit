using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Themes;
using WpfCompat;

namespace ExpressionParserControls.Tests;

/// <summary>
/// The expression editor on AvaloniaEdit, inside the calculator: typing tokenizes the expression and
/// raises ExpressionChanged, the editor takes no line breaks, and the buttons insert at the caret, as
/// upstream's RichTextBox editor did.
/// </summary>
[TestClass]
public sealed class CalculatorTests
{
    private static (CalculatorControl Calculator, Window Window) Show()
    {
        ThemeService.Instance.Initialize(Theme.Light);
        var calculator = new CalculatorControl();
        var window = new Window { Width = 770, Height = 390, Content = calculator };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        calculator.ExpressionEditor.ExpressionTextBox.TextArea.Focus();
        return (calculator, window);
    }

    [TestMethod]
    public void Typing_TokenizesTheExpression_AndRaisesExpressionChanged()
    {
        HeadlessSession.Run(() =>
        {
            var (calculator, window) = Show();
            int changes = 0;
            calculator.ExpressionChanged += () => changes++;

            window.KeyTextInput("((120 + 80) / 2) ^ 2");
            Dispatcher.UIThread.RunJobs();

            Assert.AreEqual("((120 + 80) / 2) ^ 2", calculator.GetExpressionText());
            Assert.IsTrue(changes > 0, "each change raises ExpressionChanged");
            Assert.IsTrue(calculator.ExpressionEditor.GetTokenList.Count >= 10);
            Assert.IsFalse(calculator.HasErrors);
            window.Close();
        });
    }

    [TestMethod]
    public void Enter_IsNotAccepted_AndPastedLineBreaksBecomeSpaces()
    {
        HeadlessSession.Run(() =>
        {
            var (calculator, window) = Show();
            var editor = calculator.ExpressionEditor.ExpressionTextBox;
            window.KeyTextInput("1 + 2");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual("1 + 2", editor.Text, "upstream's editor didn't accept Enter");

            editor.Document.Insert(editor.CaretOffset, "\n+ 3");
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual("1 + 2 + 3", calculator.GetExpressionText());
            Assert.AreEqual(editor.Document.TextLength, editor.CaretOffset, "the caret stays after the pasted text");
            window.Close();
        });
    }

    [TestMethod]
    public void OperatorButtons_InsertAtTheCaret()
    {
        HeadlessSession.Run(() =>
        {
            var (calculator, window) = Show();
            window.KeyTextInput("2");
            calculator.AddButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.KeyTextInput("3");
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual("2+3", calculator.GetExpressionText());
            window.Close();
        });
    }

    [TestMethod]
    public void InvalidExpression_ShowsItsErrors()
    {
        HeadlessSession.Run(() =>
        {
            var (calculator, window) = Show();
            window.KeyTextInput("(1 + ");
            Dispatcher.UIThread.RunJobs();
            Assert.IsTrue(calculator.HasErrors);
            Assert.AreEqual(Visibility.Visible, calculator.ErrorsExpander.Visibility);
            Assert.IsTrue(calculator.ErrorCount > 0);
            window.Close();
        });
    }

    [TestMethod]
    public void TextSetBeforeLoading_Shows_AndTheBoundTextFollowsEdits()
    {
        HeadlessSession.Run(() =>
        {
            ThemeService.Instance.Initialize(Theme.Light);
            var expression = new ExpressionControl { Text = "max(1, 2)" };
            Assert.AreEqual("max(1, 2)", expression.ExpressionTextBox.Text, "upstream's document showed the text before loading");
            var window = new Window { Content = expression };
            window.Show();
            Dispatcher.UIThread.RunJobs(); // Loaded comes after showing, as in WPF.
            expression.Text = "min(3, 4)";
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual("min(3, 4)", expression.ExpressionTextBox.Text);
            Assert.IsTrue(expression.GetTokenList.Count > 0, "setting Text after loading formats it");
            window.Close();
        });
    }
}
