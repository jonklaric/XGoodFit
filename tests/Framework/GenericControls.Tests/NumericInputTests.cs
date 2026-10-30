using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace GenericControls.Tests;

/// <summary>
/// The numeric text boxes, up-down and slider: which keystrokes they take, how they flag values out of
/// range, and when typed text becomes their value, as upstream's controls do.
/// </summary>
[TestClass]
public sealed class NumericInputTests
{
    // The border a numeric box draws over its text box: red while its value isn't valid.
    private static Border ErrorBorder(UserControl control) => ((Grid)control.Content!).Children.OfType<Border>().Single();

    [TestMethod]
    public void NumericTextBox_TakesOnlyTheCharactersOfANumber()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var box = new NumericTextBox();
            Window window = TestInput.Show(box, out _);
            TextBox text = box.NumericTBox;
            box.SetFocus();

            window.Type("1a2-.3.e4 ");
            Assert.AreEqual("12.34", text.Text, "letters, a second point, a sign, an exponent and spaces are refused");

            box.CanBeNegative = true;
            text.Text = "";
            window.Type("-1-2");
            Assert.AreEqual("-12", text.Text, "a minus sign only at the start");

            box.IsWholeNumber = true;
            text.Text = "";
            window.Type("3.5");
            Assert.AreEqual("35", text.Text, "no point in a whole number");

            box.IsWholeNumber = false;
            box.AllowScientificNotation = true;
            text.Text = "";
            window.Type("1.5e-3e");
            Assert.AreEqual("1.5e-3", text.Text, "one exponent, with its own sign");
            Assert.AreEqual(0.0015, box.GetValueAsDouble(), 1e-12);

            int spaceKeys = 0;
            text.KeyDown += (_, e) => spaceKeys += e.Key == Key.Space ? 1 : 0;
            window.Press(Key.Space);
            Assert.AreEqual(0, spaceKeys, "the space key is handled before the text box sees it, as upstream's PreviewKeyDown does");
            window.Close();
        });
    }

    [TestMethod]
    public void NumericTextBox_ReadsTheCurrentCulturesDecimalSeparator()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("de-DE");
            var box = new NumericTextBox();
            Window window = TestInput.Show(box, out _);
            box.SetFocus();

            window.Type("1.5,25");
            Assert.AreEqual("15,25", box.NumericTBox.Text, "in German the comma is the decimal separator, and the point is refused");
            Assert.AreEqual(15.25, box.GetValueAsDouble());
            Assert.IsTrue(box.IsValidDouble());
            window.Close();
        });
    }

    [TestMethod]
    public void NumericTextBox_FlagsValuesOutsideItsRange_WithATip()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var box = new NumericTextBox { MinValue = 0, MaxValue = 10, CanBeNegative = true };
            Window window = TestInput.Show(box, out _);
            TextBox text = box.NumericTBox;
            Border border = ErrorBorder(box);
            box.SetFocus();

            window.Type("11");
            Assert.IsTrue(box.AboveMaxValue);
            Assert.IsFalse(box.ValueIsValid);
            Assert.AreEqual("Value is above maximum allowed value", ToolTip.GetTip(text));
            Assert.IsTrue(border.Classes.Contains("invalid"));
            Assert.IsNotNull(border.BorderBrush, "the error border shows");

            text.SelectAll();
            window.Type("-");
            Assert.IsTrue(box.ValueIsValid, "a lone minus sign is a number being typed");
            Assert.IsNull(ToolTip.GetTip(text));
            window.Type("1");
            Assert.IsTrue(box.BelowMinValue);
            Assert.AreEqual("Value is below minimum allowed value", ToolTip.GetTip(text));

            text.SelectAll();
            window.Type("5");
            Assert.IsTrue(box.ValueIsValid);
            Assert.IsNull(ToolTip.GetTip(text));
            Assert.IsFalse(border.Classes.Contains("invalid"));
            Assert.IsNull(border.BorderBrush);

            box.BoundsAreExclusive = true;
            text.SelectAll();
            window.Type("10");
            Assert.IsTrue(box.AboveMaxValue, "an exclusive maximum is itself out of range");

            box.Text = "abc";
            Assert.IsTrue(box.InvalidText);
            Assert.IsFalse(box.ValueIsValid);
            Assert.AreEqual("Text entered is not a number", ToolTip.GetTip(text));
            window.Close();
        });
    }

    [TestMethod]
    public void NumericTextBox_RaisesTextChangedAsItIsTyped_AndCommitsOnFocusLoss()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var box = new NumericTextBox();
            Window window = TestInput.Show(box, out TextBox elsewhere);
            var changes = new List<string?>();
            box.TextChanged += (_, _) => changes.Add(box.NumericTBox.Text);
            box.SetFocus();

            window.Type("42");
            CollectionAssert.AreEqual(new[] { "4", "42" }, changes, "TextChanged follows each keystroke, as WPF's does");
            Assert.AreEqual("", box.Text, "the control's Text waits for focus to leave, as a WPF text box's binding does");

            elsewhere.Focus();
            Assert.AreEqual("42", box.Text);
            Assert.IsTrue(box.IsValidInteger());
            Assert.AreEqual(42, box.GetValueAsInteger());
            window.Close();
        });
    }

    [TestMethod]
    public void NumericTextBox2_ShowsItsValue_AndCommitsTypedTextOnEnterOrFocusLoss()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var box = new NumericTextBox2 { Value = 3.5 };
            Window window = TestInput.Show(box, out TextBox elsewhere);
            Assert.AreEqual("3.5", box.NumericTBox.Text);

            box.SelectAll();
            window.Type("7.25");
            Assert.AreEqual(3.5, box.Value, "typing doesn't commit");
            window.Press(Key.Enter);
            Assert.AreEqual(7.25, box.Value, "Enter does, as upstream's PreviewKeyUp handler does");

            box.SelectAll();
            window.Type("8");
            elsewhere.Focus();
            Assert.AreEqual(8d, box.Value, "and so does focus loss");

            box.CanHaveNegative = false;
            box.IsWholeNumber = true;
            box.SelectAll();
            window.Type("-2.5");
            Assert.AreEqual("25", box.NumericTBox.Text);
            window.Close();
        });
    }

    [TestMethod]
    public void NumericTextBox2_ChecksItsValueAgainstItsRange()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var box = new NumericTextBox2 { MinValue = 0, MaxValue = 10 };
            Window window = TestInput.Show(box, out _);
            var changed = new List<string?>();
            box.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            box.Value = 11;
            Assert.IsFalse(box.ValueIsValid);
            Assert.AreEqual("Number must be greater than or equal to '0' and less than or equal to '10'.", ToolTip.GetTip(box));
            CollectionAssert.AreEqual(new[] { "ValueIsValid" }, changed);
            Assert.IsFalse(ErrorBorder(box).Classes.Contains("invalid"),
                "upstream's border never updates: NumericTextBox2 doesn't implement INotifyPropertyChanged");

            box.Value = 10;
            Assert.IsTrue(box.ValueIsValid);
            Assert.IsNull(ToolTip.GetTip(box));

            box.BoundsAreExclusive = true;
            box.Value = 0;
            Assert.IsFalse(box.ValueIsValid);
            Assert.AreEqual("Number must be greater than '0' and less than '10'.", ToolTip.GetTip(box));
            window.Close();
        });
    }

    [TestMethod]
    public void NumericUpDown_StepsByItsIncrement_WithinItsBounds()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var upDown = new NumericUpDown();
            Window window = TestInput.Show(upDown, out _);
            TextBox text = upDown.NumericTextBox.NumericTBox;
            Assert.AreEqual((-100d, 100d, 1d, 0), (upDown.Minimum, upDown.Maximum, upDown.Increment, upDown.DecimalPlaces));
            Assert.AreEqual("0", text.Text);

            window.Click(upDown.cmdUp);
            Assert.AreEqual(1d, upDown.Value);
            Assert.AreEqual("1", text.Text);
            window.Click(upDown.cmdDown);
            window.Click(upDown.cmdDown);
            Assert.AreEqual(-1d, upDown.Value);
            Assert.AreEqual("-1", text.Text);

            upDown.DecimalPlaces = 2;
            upDown.Increment = 0.25;
            window.Click(upDown.cmdUp);
            Assert.AreEqual(-0.75, upDown.Value);
            Assert.AreEqual("-0.75", text.Text);

            upDown.Maximum = 1;
            upDown.Value = 1;
            window.Click(upDown.cmdUp);
            Assert.AreEqual(1d, upDown.Value, "no step past the maximum");
            Assert.AreEqual(1d, upDown.NumericTextBox.MaxValue, "the bounds reach the text box");

            // The value's callback shows the maximum, which the text box's binding writes back, then the
            // binding shows the value as it is ("1", not "1.00"): in WPF too, the binding updates after
            // the callback.
            upDown.Value = 5;
            Assert.AreEqual(1d, upDown.Value);
            Assert.AreEqual("1", text.Text);

            upDown.Minimum = 3;
            Assert.AreEqual(3d, upDown.Maximum, "a minimum above the maximum raises the maximum");
            window.Close();
        });
    }

    [TestMethod]
    public void NumericUpDown_RefusesANegativeIncrementOrTooManyDecimalPlaces()
    {
        HeadlessSession.Run(() =>
        {
            var upDown = new NumericUpDown();
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => upDown.Increment = -1);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => upDown.DecimalPlaces = 100);
        });
    }

    [TestMethod]
    public void NumericUpDown_TakesATypedValue_AndStepsOnFromIt()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var upDown = new NumericUpDown();
            Window window = TestInput.Show(upDown, out TextBox elsewhere);
            window.Click(upDown.cmdUp);

            upDown.NumericTextBox.SetFocus();
            upDown.NumericTextBox.SelectAll();
            window.Type("7");
            elsewhere.Focus();
            Assert.AreEqual(7d, upDown.Value, "the typed text reaches the value when focus leaves");

            window.Click(upDown.cmdUp);
            Assert.AreEqual(8d, upDown.Value);
            Assert.AreEqual("8", upDown.NumericTextBox.NumericTBox.Text);

            // At the maximum, a larger typed value is clamped back to it, as WPF's binding writes the
            // clamped text back.
            upDown.Maximum = 8;
            upDown.NumericTextBox.SetFocus();
            upDown.NumericTextBox.SelectAll();
            window.Type("50");
            elsewhere.Focus();
            Assert.AreEqual(8d, upDown.Value);
            Assert.AreEqual("8", upDown.NumericTextBox.NumericTBox.Text);
            window.Close();
        });
    }

    [TestMethod]
    public void NumericSlider_PagesToTheNextIncrement_AndKeepsItsTextInStep()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var slider = new NumericSlider { Width = 300, Increment = 5 };
            Window window = TestInput.Show(slider, out TextBox elsewhere);
            var values = new List<double>();
            slider.ValueChanged += values.Add;
            Track track = slider.Slider.GetVisualDescendants().OfType<Track>().Single();

            window.Click(track, new Point(track.Bounds.Width - 2, track.Bounds.Height / 2));
            Assert.AreEqual(5d, slider.Value, "a press on the track pages by LargeChange (1) to the next tick, WPF's MoveToNextTick");
            Assert.AreEqual("5", slider.NumericTextBox.NumericTBox.Text);

            slider.NumericTextBox.SetFocus();
            slider.NumericTextBox.SelectAll();
            window.Type("30");
            elsewhere.Focus();
            Assert.AreEqual(30d, slider.Value);
            Assert.AreEqual(30d, slider.Slider.Value);
            CollectionAssert.AreEqual(new[] { 5d, 30d }, values);

            slider.Maximum = 50;
            Assert.AreEqual(50d, slider.NumericTextBox.MaxValue, "the bounds reach the text box");
            window.Close();
        });
    }
}
