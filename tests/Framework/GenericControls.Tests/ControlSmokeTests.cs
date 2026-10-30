using Avalonia.Controls;
using Avalonia.Threading;

namespace GenericControls.Tests;

/// <summary>
/// Every ported control loads its XAML, applies its theme and lays out in a window without binding
/// errors, in each of the three themes.
/// </summary>
[TestClass]
public sealed class ControlSmokeTests
{
    private static IEnumerable<object[]> Controls() =>
    [
        [typeof(SeparatorWithHeader)], [typeof(PropertyAttributes)], [typeof(NumericTextBox)], [typeof(NumericTextBox2)],
        [typeof(NumericUpDown)], [typeof(NumericSlider)], [typeof(ResizableTextBoxControl)], [typeof(ColorPicker)],
        [typeof(ColorPickerPopup)], [typeof(ClockControl)], [typeof(DateAndTimePickerControl)], [typeof(DateAndTimeTextBoxControl)],
        [typeof(BooleanPropertyControl)], [typeof(ColorPropertyControl)], [typeof(ContentPropertyControl)], [typeof(DateTimePropertyControl)],
        [typeof(FileSelectorControl)], [typeof(FontSelectorControl)], [typeof(FontWeightSelectorControl)], [typeof(HorizontalAlignmentControl)],
        [typeof(LineStyleSelectorControl)], [typeof(LineWidthSelectorControl)], [typeof(NameTextPropertyControl)], [typeof(NumericAutoPropertyControl)],
        [typeof(NumericPropertyControl)], [typeof(NumericPropertySelectorControl)], [typeof(NumericSliderPropertyControl)], [typeof(PointPropertyControl)],
        [typeof(ResizeableTextPropertyControl)], [typeof(TextPropertyControl)], [typeof(VerticalAlignmentControl)],
    ];

    [TestMethod]
    public void BindingErrors_SeesAFailedBinding()
    {
        HeadlessSession.Run(() =>
        {
            using var errors = new BindingErrors();
            var text = new TextBlock();
            text.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("Missing") { Source = new object() });
            Window window = PropertyControlTests.Show(text);
            window.Close();
            Assert.AreNotEqual(0, errors.Messages.Count);
        });
    }

    [TestMethod]
    [DynamicData(nameof(Controls), DynamicDataSourceType.Method)]
    public void Control_LoadsAndBindsInEveryTheme(Type type)
    {
        HeadlessSession.Run(() =>
        {
            using var errors = new BindingErrors();
            var control = (Control)Activator.CreateInstance(type)!;
            Window window = PropertyControlTests.Show(control, 500);
            foreach (Themes.Theme theme in Enum.GetValues<Themes.Theme>())
            {
                Themes.ThemeService.Instance.SetTheme(theme);
                Dispatcher.UIThread.RunJobs();
            }
            Themes.ThemeService.Instance.SetTheme(Themes.Theme.Light);
            window.Close();

            Assert.IsTrue(control.Bounds.Width > 0, "the control lays out");
            Assert.AreEqual(0, errors.Messages.Count, string.Join(Environment.NewLine, errors.Messages));
        });
    }
}
