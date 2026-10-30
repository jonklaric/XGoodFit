using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;

namespace GenericControls.Tests;

/// <summary>
/// The date and time text box, its picker and the clock: the formats they show and read, and the times
/// the clock's face picks.
/// </summary>
[TestClass]
public sealed class DateTimeControlTests
{
    private static readonly DateTime Evening = new(2026, 7, 1, 21, 5, 44);

    [TestMethod]
    public void DateAndTimeTextBox_ShowsUsFormats_WhateverTheCulture()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("de-DE");
            var box = new DateAndTimeTextBoxControl { SelectedDateTime = Evening, Width = 250, Height = 22 };
            Window window = TestInput.Show(box, out _);

            Assert.AreEqual("07/01/2026 09:05 PM", box.DateTimeTextBox.Text, "WPF's bindings format with en-US (FormattingCulture)");
            box.Is24Hour = true;
            Assert.AreEqual("07/01/2026 21:05", box.DateTimeTextBox.Text);
            window.Close();
        });
    }

    [TestMethod]
    public void DateAndTimeTextBox_ReadsATypedDate_OnEnterOrFocusLoss()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var box = new DateAndTimeTextBoxControl { SelectedDateTime = Evening, Width = 250, Height = 22 };
            Window window = TestInput.Show(box, out TextBox elsewhere);
            int changes = 0;
            box.PropertyChanged += (_, e) => changes += e.PropertyName == nameof(DateAndTimeTextBoxControl.SelectedDateTime) ? 1 : 0;
            TextBox text = box.DateTimeTextBox;

            text.Focus();
            text.SelectAll();
            window.Type("12/25/2026 08:30 AM");
            Assert.AreEqual(Evening, box.SelectedDateTime, "typing doesn't commit");
            window.Press(Key.Enter);
            Assert.AreEqual(new DateTime(2026, 12, 25, 8, 30, 0), box.SelectedDateTime);
            Assert.AreEqual(1, changes);

            box.Is24Hour = true;
            text.SelectAll();
            window.Type("01/02/2027 14:15");
            elsewhere.Focus();
            Assert.AreEqual(new DateTime(2027, 1, 2, 14, 15, 0), box.SelectedDateTime);
            Assert.AreEqual("01/02/2027 14:15", text.Text);

            text.Focus();
            text.SelectAll();
            window.Type("not a date");
            window.Press(Key.Enter);
            Assert.AreEqual(new DateTime(2027, 1, 2, 14, 15, 0), box.SelectedDateTime, "text that isn't a date leaves the date as it was");
            window.Close();
        });
    }

    [TestMethod]
    public void DateAndTimeTextBox_OpensAPickerOnItsDate_WhosePicksItTakes()
    {
        HeadlessSession.Run(() =>
        {
            var box = new DateAndTimeTextBoxControl { SelectedDateTime = Evening, Width = 250, Height = 22 };
            Window window = TestInput.Show(box, out _);

            window.Click(box.DateTimeRadioButton);
            Assert.IsTrue(box.DateTimePopup.IsOpen);
            DateAndTimePickerControl picker = box.DateAndTimePicker;
            Assert.AreEqual(Evening, picker.DateAndTime);
            Assert.AreEqual(new DateTime(2026, 7, 1), picker.CalendarSelector.DisplayDate.Date);
            Assert.IsTrue(picker.Clock.IsHours, "the picker starts on the hours (SetToHoursOnLoad)");

            picker.CalendarSelector.SelectedDate = new DateTime(2026, 7, 15);
            Assert.AreEqual(new DateTime(2026, 7, 15, 21, 5, 44), box.SelectedDateTime, "picking a day keeps the time");

            picker.CalendarSelector.DisplayDate = new DateTime(2026, 2, 1);
            Assert.AreEqual(new DateTime(2026, 2, 15, 21, 5, 44), box.SelectedDateTime, "showing another month moves the date to it");

            box.DateTimePopup.IsOpen = false;
            Assert.AreEqual(false, box.DateTimeRadioButton.IsChecked);
            window.Close();
        });
    }

    [TestMethod]
    public void Clock_PicksTheHourThenTheMinute_FromItsFace_AndSwitchesAmAndPm()
    {
        HeadlessSession.Run(() =>
        {
            var clock = new ClockControl { Width = 200, Height = 200, Time = Evening };
            Window window = TestInput.Show(clock, out _, 300, 300);
            Ellipse face = clock.ClockFace;
            Assert.IsTrue(clock.IsHours);

            window.Click(face, new Point(90, 50));
            Assert.AreEqual(15, clock.Time.Hour, "3 o'clock on a 12-hour face, in the afternoon as the time was");
            Assert.IsTrue(clock.IsMinutes, "a pick moves on to the minutes");

            window.Click(face, new Point(50, 90));
            Assert.AreEqual(new DateTime(2026, 7, 1, 15, 30, 44), clock.Time, "30 minutes at the bottom");
            Assert.IsTrue(clock.IsMinutes, "without seconds, the clock stays on the minutes");

            window.Click(clock.AMFace);
            Assert.AreEqual(3, clock.Time.Hour);
            window.Click(clock.PMFace);
            Assert.AreEqual(15, clock.Time.Hour);

            clock.HasSeconds = true;
            window.Click(face, new Point(10, 50));
            Assert.AreEqual(45, clock.Time.Minute);
            Assert.IsTrue(clock.IsSeconds, "with seconds, a minute pick moves on to them");
            window.Click(face, new Point(50, 10));
            Assert.AreEqual(0, clock.Time.Second);
            window.Close();
        });
    }

    [TestMethod]
    public void Clock_On24Hours_HasTwentyFourHours_WithMidnightAtTheTop()
    {
        HeadlessSession.Run(() =>
        {
            var clock = new ClockControl { Width = 200, Height = 200, Time = Evening, Is24Hour = true };
            Window window = TestInput.Show(clock, out _, 300, 300);

            window.Click(clock.ClockFace, new Point(50, 10));
            Assert.AreEqual(0, clock.Time.Hour, "midnight at the top");
            Assert.IsFalse(clock.AMFace.IsVisible, "no AM and PM on a 24-hour clock");

            clock.IsHours = true;
            clock.IsMinutes = false;
            window.Click(clock.ClockFace, new Point(50, 90));
            Assert.AreEqual(12, clock.Time.Hour, "noon at the bottom");
            window.Close();
        });
    }
}
