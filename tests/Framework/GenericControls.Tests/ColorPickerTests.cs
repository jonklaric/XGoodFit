using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GenericControls.Tests;

/// <summary>
/// The colour picker: its sliders, text boxes and gradients follow its colour, and each way of picking a
/// colour (the preview, the hue slider, the channel boxes and the swatches) sets it, keeping its alpha.
/// </summary>
[TestClass]
public sealed class ColorPickerTests
{
    private static readonly Color SteelBlue = Color.FromArgb(200, 70, 130, 180);

    private static (Window Window, ColorPicker Picker, TextBox Elsewhere) ShowPicker(Color color)
    {
        var picker = new ColorPicker { Width = 140, Height = 220, Color = new SolidColorBrush(color) };
        Window window = TestInput.Show(picker, out TextBox elsewhere);
        return (window, picker, elsewhere);
    }

    [TestMethod]
    public void ColorPicker_ShowsItsColourInItsSlidersBoxesAndGradients()
    {
        HeadlessSession.Run(() =>
        {
            var (window, picker, _) = ShowPicker(SteelBlue);

            CollectionAssert.AreEqual(new[] { 200d, 70d, 130d, 180d },
                new[] { picker.ASlider.Value, picker.RSlider.Value, picker.GSlider.Value, picker.BSlider.Value });
            CollectionAssert.AreEqual(new[] { 200d, 70d, 130d, 180d },
                new[] { picker.AlphaTextBox.Value, picker.RedTextBox.Value, picker.GreenTextBox.Value, picker.BlueTextBox.Value });
            Assert.AreEqual(200 / 255d, picker.PreviewDrawingBrush.Opacity, 1e-9, "the preview shows the alpha");
            Assert.AreEqual(Color.FromRgb(70, 130, 180), picker.MaxAStop.Color);
            Assert.AreEqual((Color.FromRgb(0, 130, 180), Color.FromRgb(255, 130, 180)), (picker.MinRStop.Color, picker.MaxRStop.Color));
            Assert.AreEqual((Color.FromRgb(70, 0, 180), Color.FromRgb(70, 255, 180)), (picker.MinGStop.Color, picker.MaxGStop.Color));
            Assert.AreEqual((Color.FromRgb(70, 130, 0), Color.FromRgb(70, 130, 255)), (picker.MinBStop.Color, picker.MaxBStop.Color));

            ColorPicker.HsvColor hsv = ColorPicker.ConvertRgbToHsv(70, 130, 180);
            Assert.AreEqual(360 - hsv.H, picker.PART_ColorSlider.Value, 1e-9, "the hue slider runs from 360 down");
            Color hue = ColorPicker.ConvertHsvToRgb(hsv.H, 1, 1);
            Assert.AreEqual(hue, picker.GradBrush1.Color, "the preview's base is the pure hue");
            Assert.AreEqual(hue, picker.GradStop2.Color);

            picker.Color = new SolidColorBrush(Colors.Gray);
            Assert.AreEqual(360 - hsv.H, picker.PART_ColorSlider.Value, 1e-9, "a grey has no hue, so the hue slider stays put");
            window.Close();
        });
    }

    [TestMethod]
    public void ColorPicker_TakesAChannelTypedIntoItsBox()
    {
        HeadlessSession.Run(() =>
        {
            var (window, picker, elsewhere) = ShowPicker(SteelBlue);

            picker.RedTextBox.SelectAll();
            window.Type("10");
            window.Press(Key.Enter);
            Assert.AreEqual(Color.FromArgb(200, 10, 130, 180), picker.Color.Color);
            Assert.AreEqual(10d, picker.RSlider.Value);

            picker.AlphaTextBox.SelectAll();
            window.Type("64");
            elsewhere.Focus();
            Assert.AreEqual(Color.FromArgb(64, 10, 130, 180), picker.Color.Color);
            window.Close();
        });
    }

    [TestMethod]
    public void ColorPicker_SwatchesKeepTheAlpha_UnlessItIsZero()
    {
        HeadlessSession.Run(() =>
        {
            var (window, picker, _) = ShowPicker(SteelBlue);
            Rectangle red = picker.QuickColorsPanel.Children.OfType<Rectangle>().Single(r => (string?)ToolTip.GetTip(r) == "Red");

            window.Click(red);
            Assert.AreEqual(Color.FromArgb(200, 255, 0, 0), picker.Color.Color);

            picker.Color = new SolidColorBrush(Colors.Transparent);
            window.Click(red);
            Assert.AreEqual(Color.FromArgb(255, 255, 0, 0), picker.Color.Color, "a transparent colour becomes opaque, so the pick shows");
            window.Close();
        });
    }

    [TestMethod]
    public void ColorPicker_PicksFromThePreview_SaturationAcrossAndBrightnessDown()
    {
        HeadlessSession.Run(() =>
        {
            var (window, picker, _) = ShowPicker(SteelBlue);
            Border preview = picker.PreviewBorder;
            double width = preview.Bounds.Width, height = preview.Bounds.Height;
            Color hue = ColorPicker.ConvertHsvToRgb(360 - picker.PART_ColorSlider.Value, 1, 1);

            window.Click(preview, new Point(width - 0.01, 0.01));
            Color picked = picker.Color.Color;
            Assert.AreEqual(200, picked.A, "the alpha is kept");
            Assert.AreEqual(hue.R, picked.R, 1);
            Assert.AreEqual(hue.G, picked.G, 1);
            Assert.AreEqual(hue.B, picked.B, 1);

            window.Click(preview, new Point(0.01, 0.01));
            Assert.AreEqual(Color.FromArgb(200, 255, 255, 255), picker.Color.Color, "no saturation at the left: white at the top");

            Point start = window.PointOf(preview, new Point(width / 2, height / 2));
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(window.PointOf(preview, new Point(width / 2, height - 0.01)), RawInputModifiers.LeftMouseButton);
            Assert.AreEqual(Color.FromArgb(200, 0, 0, 0), picker.Color.Color, "dragging follows the pointer: black at the bottom");
            window.MouseUp(start, MouseButton.Left);
            window.Close();
        });
    }

    [TestMethod]
    public void ColorPicker_HueSliderChangesTheHue_KeepingSaturationAndBrightness()
    {
        HeadlessSession.Run(() =>
        {
            var (window, picker, _) = ShowPicker(Color.FromArgb(255, 255, 0, 0));
            picker.PART_ColorSlider.Value = 360 - 120;
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual(Color.FromArgb(255, 0, 255, 0), picker.Color.Color, "full saturation and brightness, now green");
            Assert.AreEqual(Color.FromRgb(0, 255, 0), picker.GradBrush1.Color);
            window.Close();
        });
    }

    [TestMethod]
    public void ColorPickerPopup_OpensItsPicker_WhichSharesItsColour()
    {
        HeadlessSession.Run(() =>
        {
            var popup = new ColorPickerPopup { Width = 60, Height = 28, Color = new SolidColorBrush(SteelBlue) };
            Window window = TestInput.Show(popup, out _);

            window.Click(popup.PopupToggleButton);
            Assert.IsTrue(popup.PickerPopup.IsOpen);
            Assert.IsTrue(popup.ColorPicker.IsEffectivelyVisible);
            Assert.AreEqual(SteelBlue, popup.ColorPicker.Color.Color);
            Assert.AreEqual(SteelBlue, ((ISolidColorBrush)popup.ColorRectangle.Fill!).Color, "the button shows the colour");

            popup.ColorPicker.Color = new SolidColorBrush(Colors.Orange);
            Assert.AreEqual(Colors.Orange, popup.Color.Color, "the picker's colour is the popup's");

            popup.PickerPopup.IsOpen = false;
            Assert.AreEqual(false, popup.PopupToggleButton.IsChecked, "closing the popup releases its button");
            window.Close();
        });
    }
}
