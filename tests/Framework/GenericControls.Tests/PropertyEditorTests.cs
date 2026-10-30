using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using WpfCompat;
using WpfCompat.Media;

namespace GenericControls.Tests;

/// <summary>
/// The property editors' values reach the controls inside them and come back from them, with upstream's
/// checks, options and events.
/// </summary>
[TestClass]
public sealed class PropertyEditorTests
{
    private static T Inner<T>(Control control) where T : Control => control.GetVisualDescendants().OfType<T>().Single();

    [TestMethod]
    public void NumericProperty_ShowsItsNumber_CommitsOnEnter_AndChecksIt()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var control = new NumericPropertyControl { Title = "Mean", MinValue = 0, MaxValue = 10, Number = 2.5 };
            Window window = TestInput.Show(control, out _, 500);
            TextBox text = control.NumericTBox;
            Assert.AreEqual("2.5", text.Text);

            text.Focus();
            text.SelectAll();
            window.Type("7a.25");
            Assert.AreEqual("7.25", text.Text);
            Assert.AreEqual(2.5, control.Number, "typing doesn't commit");
            window.Press(Key.Enter);
            Assert.AreEqual(7.25, control.Number);

            control.Number = 11;
            Assert.IsFalse(control.ValueIsValid);
            Assert.AreEqual("Number must be greater than or equal to '0' and less than or equal to '10'.", ToolTip.GetTip(control));
            Assert.IsNotNull(control.GetVisualDescendants().OfType<Border>().SingleOrDefault(b => b.Classes.Contains("invalid")),
                "the error border shows");

            control.CanHaveNaN = false;
            control.Number = double.NaN;
            Assert.AreEqual("NaN is not a number.", ToolTip.GetTip(control));

            control.MaxValue = double.PositiveInfinity;
            control.Number = double.PositiveInfinity;
            Assert.IsTrue(control.ValueIsValid);
            Assert.IsNull(ToolTip.GetTip(control));
            Assert.AreEqual("∞", text.Text);
            window.Close();
        });
    }

    [TestMethod]
    public void NumericAutoProperty_EnterCommits_AndItsResetButtonRestoresTheDefault()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var control = new NumericAutoPropertyControl { Title = "Bins", DefaultNumber = double.NaN, Number = 5 };
            Window window = TestInput.Show(control, out _, 500);
            Button reset = Inner<Button>(control);
            Assert.IsFalse(control.NumberIsDefault);
            Assert.AreEqual(Visibility.Visible, reset.Visibility, "the Reset button shows while the number isn't the default");

            control.NumericTBox.Focus();
            control.NumericTBox.SelectAll();
            window.Type("7");
            window.Press(Key.Enter);
            Assert.AreEqual(7d, control.Number, "Enter moves focus right, which commits the text");
            Assert.IsTrue(control.NumericTBox.IsFocused, "and back");

            window.Click(reset);
            Assert.IsTrue(double.IsNaN(control.Number));
            Assert.IsTrue(control.NumberIsDefault);
            Assert.AreEqual(Visibility.Hidden, reset.Visibility, "and hides, keeping its space, as upstream's");
            Assert.AreEqual("NaN", control.NumericTBox.Text);
            window.Close();
        });
    }

    [TestMethod]
    public void NumericPropertySelector_ShowsFiveDecimals_AndTakesAnOptionOrATypedNumber()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var control = new NumericPropertySelectorControl { Title = "Width", NumericOptions = [0.5, 1, 2], SelectedNumber = 1.234567 };
            Window window = TestInput.Show(control, out _, 500);
            ComboBox combo = control.NumericComboBox;
            Assert.AreEqual("1.23457", combo.Text);

            combo.SelectedIndex = 2;
            Assert.AreEqual(2d, control.SelectedNumber);

            TextBox editor = Inner<TextBox>(combo);
            editor.Focus();
            editor.SelectAll();
            window.Type("1x2");
            Assert.AreEqual("12", editor.Text, "letters are refused");
            Assert.AreEqual(12d, control.SelectedNumber, "typed numbers commit as they're typed");
            window.Close();
        });
    }

    [TestMethod]
    public void NumericSliderProperty_KeepsItsSliderAndTextInStep()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var control = new NumericSliderPropertyControl { Title = "Opacity", MinValue = 0, MaxValue = 1, Increment = 0.05, Number = 0.5 };
            Window window = TestInput.Show(control, out TextBox elsewhere, 500);
            Assert.AreEqual(0.5, control.Slider.Value);
            Assert.AreEqual("0.5", control.NumericTBox.Text);

            control.Slider.Value = 0.75;
            Assert.AreEqual(0.75, control.Number);
            Assert.AreEqual("0.75", control.NumericTBox.Text);

            control.NumericTBox.Focus();
            control.NumericTBox.SelectAll();
            window.Type("0.25");
            elsewhere.Focus();
            Assert.AreEqual(0.25, control.Number);
            Assert.AreEqual(0.25, control.Slider.Value);
            window.Close();
        });
    }

    [TestMethod]
    public void BooleanProperty_TicksWithIsSelected_AndRaisesCheckedAndUnchecked()
    {
        HeadlessSession.Run(() =>
        {
            var control = new BooleanPropertyControl { Title = "Enabled", IsSelected = false };
            Window window = TestInput.Show(control, out _, 500);
            CheckBox box = Inner<CheckBox>(control);
            var events = new List<string>();
            control.Checked += (_, _) => events.Add("Checked");
            control.Unchecked += (_, _) => events.Add("Unchecked");

            window.Click(box);
            Assert.IsTrue(control.IsSelected);
            window.Click(box);
            Assert.IsFalse(control.IsSelected);
            control.IsSelected = true;
            Assert.AreEqual(true, box.IsChecked);
            CollectionAssert.AreEqual(new[] { "Checked", "Unchecked", "Checked" }, events);
            window.Close();
        });
    }

    /// <summary>
    /// As WPF's: a click writes the check box's state through IsSelected's binding to its source before Checked or
    /// Unchecked is raised, so a handler reads the new state from the source (the Point Process panel's Is Seasonal
    /// shows its block year so).
    /// </summary>
    [TestMethod]
    public void BooleanProperty_RaisesCheckedAndUnchecked_AfterItsBindingWritesTheSource()
    {
        HeadlessSession.Run(() =>
        {
            var source = new BooleanSource();
            var control = new BooleanPropertyControl { Title = "Seasonal", DataContext = source };
            control.Bind(BooleanPropertyControl.IsSelectedProperty, new Avalonia.Data.Binding(nameof(BooleanSource.Value)) { Mode = Avalonia.Data.BindingMode.TwoWay });
            Window window = TestInput.Show(control, out _, 500);
            CheckBox box = Inner<CheckBox>(control);
            var seen = new List<string>();
            control.Checked += (_, _) => seen.Add("Checked " + source.Value);
            control.Unchecked += (_, _) => seen.Add("Unchecked " + source.Value);

            window.Click(box);
            window.Click(box);
            CollectionAssert.AreEqual(new[] { "Checked True", "Unchecked False" }, seen);
            Assert.IsFalse(control.IsSelected);
            window.Close();
        });
    }

    /// <summary>A bound source for <see cref="BooleanProperty_RaisesCheckedAndUnchecked_AfterItsBindingWritesTheSource"/>.</summary>
    public sealed class BooleanSource : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _value;

        /// <inheritdoc/>
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        /// <summary>The bound value.</summary>
        public bool Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }

    [TestMethod]
    public void FontWeightSelector_OffersWpfsWeights_ByName()
    {
        HeadlessSession.Run(() =>
        {
            var control = new FontWeightSelectorControl { Title = "Weight", SelectedFontWeight = FontWeight.SemiBold };
            Window window = TestInput.Show(control, out _, 500);
            ComboBox combo = Inner<ComboBox>(control);
            Assert.AreEqual(FontWeight.SemiBold, combo.SelectedValue);
            Assert.IsTrue(combo.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "SemiBold"), "the selection shows WPF's name");

            combo.SelectedValue = FontWeights.Bold;
            Assert.AreEqual(FontWeight.Bold, control.SelectedFontWeight);
            window.Close();
        });
    }

    [TestMethod]
    public void AlignmentSelectors_OfferWpfsOrder_WithStretchOptional()
    {
        HeadlessSession.Run(() =>
        {
            var horizontal = new HorizontalAlignmentControl { Title = "Horizontal", Alignment = HorizontalAlignment.Right };
            var vertical = new VerticalAlignmentControl { Title = "Vertical", Alignment = VerticalAlignment.Top };
            Window window = TestInput.Show(new StackPanel { Children = { horizontal, vertical } }, out _, 500);
            ComboBox hCombo = Inner<ComboBox>(horizontal), vCombo = Inner<ComboBox>(vertical);

            CollectionAssert.AreEqual(new object[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right, HorizontalAlignment.Stretch },
                hCombo.Items.ToArray());
            CollectionAssert.AreEqual(new object[] { VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom, VerticalAlignment.Stretch },
                vCombo.Items.ToArray());
            Assert.AreEqual(2, hCombo.SelectedIndex);
            Assert.AreEqual(0, vCombo.SelectedIndex);

            vCombo.SelectedIndex = 2;
            Assert.AreEqual(VerticalAlignment.Bottom, vertical.Alignment);

            horizontal.ShowStretch = false;
            CollectionAssert.AreEqual(new object[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right },
                hCombo.Items.ToArray());
            window.Close();
        });
    }

    [TestMethod]
    public void LineStyleSelector_FindsTheStyleByItsDashes_AndGivesBackACopy()
    {
        HeadlessSession.Run(() =>
        {
            var control = new LineStyleSelectorControl { Title = "Style", SelectedDashArray = new DoubleCollection([4d, 1d]) };
            Window window = TestInput.Show(control, out _, 500);
            ComboBox combo = Inner<ComboBox>(control);
            Assert.AreEqual(2, combo.SelectedIndex);

            combo.SelectedIndex = 3;
            CollectionAssert.AreEqual(new[] { 1d, 1d }, control.SelectedDashArray.ToArray());
            Assert.AreNotSame(LineStyleSelectorControl.LineStyleOptions[3], control.SelectedDashArray, "a copy, as upstream's Clone");

            control.SelectedDashArray = new DoubleCollection();
            Assert.AreEqual(1, combo.SelectedIndex, "an empty dash array is the solid line");
            window.Close();
        });
    }

    [TestMethod]
    public void LineWidthSelector_SelectsTheWidth_ShownInPixels()
    {
        HeadlessSession.Run(() =>
        {
            var control = new LineWidthSelectorControl { Title = "Width", WidthOptions = [0.5, 1, 1.5, 2], SelectedWidth = 1.5 };
            Window window = TestInput.Show(control, out _, 500);
            ComboBox combo = Inner<ComboBox>(control);
            Assert.AreEqual(2, combo.SelectedIndex);

            combo.SelectedIndex = 0;
            Assert.AreEqual(0.5, control.SelectedWidth);
            window.Close();
        });
    }

    [TestMethod]
    public void PointProperty_EditsItsXAndY()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var control = new PointPropertyControl { Title = "Point", DataPoint = new Point(1.5, -2) };
            Window window = TestInput.Show(control, out TextBox elsewhere, 500);
            Assert.AreEqual("1.5", control.DataPointX.NumericTBox.Text);
            Assert.AreEqual("-2", control.DataPointY.NumericTBox.Text);

            control.DataPointY.SetFocus();
            control.DataPointY.SelectAll();
            window.Type("3.25");
            elsewhere.Focus();
            Assert.AreEqual(new Point(1.5, 3.25), control.DataPoint);
            window.Close();
        });
    }

    [TestMethod]
    public void ColorDateTimeAndContentEditors_PassTheirValuesThrough()
    {
        HeadlessSession.Run(() =>
        {
            var steelBlue = new SolidColorBrush(Color.FromArgb(200, 70, 130, 180));
            var date = new DateTime(2026, 7, 1, 9, 31, 44);
            var slider = new Slider();
            var color = new ColorPropertyControl { Title = "Colour", SelectedColor = steelBlue };
            var dateTime = new DateTimePropertyControl { Title = "Date", SelectedDateTime = date };
            var content = new ContentPropertyControl { Title = "Custom", InnerContent = slider };
            Window window = TestInput.Show(new StackPanel { Children = { color, dateTime, content } }, out _, 500);

            ColorPickerPopup popup = Inner<ColorPickerPopup>(color);
            Assert.AreEqual(steelBlue.Color, popup.Color.Color);
            popup.Color = new SolidColorBrush(Colors.Orange);
            Assert.AreEqual(Colors.Orange, color.SelectedColor.Color);

            Assert.AreEqual(date, dateTime.DateTimeControl.SelectedDateTime);
            dateTime.DateTimeControl.SelectedDateTime = date.AddDays(1);
            Assert.AreEqual(date.AddDays(1), dateTime.SelectedDateTime);

            Assert.IsTrue(slider.IsEffectivelyVisible, "the custom content shows in the editor column");
            Assert.AreSame(content, slider.FindAncestorOfType<ContentPropertyControl>());
            window.Close();
        });
    }

    [TestMethod]
    public void TextEditors_PassTheirTextThrough_AndRaiseTextChanged()
    {
        HeadlessSession.Run(() =>
        {
            var file = new FileSelectorControl { Title = "File", Text = "data.csv" };
            var name = new NameTextPropertyControl { Title = "Name", Text = "Gauge" };
            var resizable = new ResizeableTextPropertyControl { Title = "Notes", Text = "First line" };
            Window window = TestInput.Show(new StackPanel { Children = { file, name, resizable } }, out _, 500);
            var fileChanges = new List<string?>();
            file.TextChanged += (_, _) => fileChanges.Add(file.FilePathTextBox.Text);

            Assert.AreEqual("data.csv", file.FilePathTextBox.Text);
            file.FilePathTextBox.Focus();
            file.FilePathTextBox.CaretIndex = file.FilePathTextBox.Text!.Length;
            window.Type("x");
            Assert.AreEqual("data.csvx", file.Text);
            CollectionAssert.AreEqual(new[] { "data.csvx" }, fileChanges);

            Assert.AreSame(name.NameTextBoxElement, name.NameTextBox, "the public NameTextBox other assemblies use");
            Assert.AreEqual("Gauge", name.NameTextBox.Text);
            name.NameTextBox.Text = "Gauge 2";
            Assert.AreEqual("Gauge 2", name.Text);

            Assert.AreEqual("First line", Inner<ResizableTextBoxControl>(resizable).Text);
            window.Close();
        });
    }
}
