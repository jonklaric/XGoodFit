using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace WpfCompat;

/// <summary>
/// WPF's <c>Slider.IsMoveToPointEnabled</c> for Avalonia sliders: what pressing a slider's track does.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia's slider moves the thumb to the pointer and lets the pointer drag it from there. WPF's slider
/// does neither. Setting this property gives a slider WPF's behaviour; unset, it keeps Avalonia's:
/// </para>
/// <list type="bullet">
/// <item><c>False</c>, WPF's default: pressing the track moves the thumb towards the pointer by
/// <c>LargeChange</c> (to the next tick when <c>IsSnapToTickEnabled</c>), as WPF's track repeat buttons
/// do. Held, it moves again after the button's <c>Delay</c> and then every <c>Interval</c> (500 and 33 ms
/// in Themes, as in WPF) while the pointer is over the button, which shrinks as the thumb nears the
/// pointer.</item>
/// <item><c>True</c>: pressing anywhere off the thumb moves the thumb to the pointer (to the nearest
/// tick when snapping), once. Dragging needs a press on the thumb itself.</item>
/// </list>
/// <para>
/// Both follow WPF's <c>Slider</c> and <c>RepeatButton</c> code. Dragging the thumb is Avalonia's either
/// way.
/// </para>
/// </remarks>
public static class WpfSlider
{
    /// <summary>
    /// Whether pressing the slider's track moves the thumb to the pointer (<c>True</c>) or by
    /// <c>LargeChange</c> (<c>False</c>), as WPF's <c>IsMoveToPointEnabled</c>. Null, the default, keeps
    /// Avalonia's behaviour.
    /// </summary>
    public static readonly AttachedProperty<bool?> IsMoveToPointEnabledProperty =
        AvaloniaProperty.RegisterAttached<Slider, bool?>("IsMoveToPointEnabled", typeof(WpfSlider));

    // The sliders whose presses this class already handles.
    private static readonly ConditionalWeakTable<Slider, object> Watched = new();

    static WpfSlider()
    {
        IsMoveToPointEnabledProperty.Changed.AddClassHandler<Slider>((slider, e) =>
        {
            if (e.GetNewValue<bool?>() is not null && !Watched.TryGetValue(slider, out _))
            {
                Watched.Add(slider, new object());
                // A handler on the slider sees a press before its track and track buttons do.
                slider.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel);
            }
        });
    }

    /// <summary>Gets what pressing <paramref name="slider"/>'s track does: see <see cref="IsMoveToPointEnabledProperty"/>.</summary>
    public static bool? GetIsMoveToPointEnabled(Slider slider) => slider.GetValue(IsMoveToPointEnabledProperty);

    /// <summary>Sets what pressing <paramref name="slider"/>'s track does: see <see cref="IsMoveToPointEnabledProperty"/>.</summary>
    public static void SetIsMoveToPointEnabled(Slider slider, bool? value) => slider.SetValue(IsMoveToPointEnabledProperty, value);

    private static void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Slider slider || slider.GetValue(IsMoveToPointEnabledProperty) is not { } moveToPoint
            || e.GetCurrentPoint(slider).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
        {
            return;
        }

        Track? track = slider.GetTemplateDescendants().OfType<Track>().FirstOrDefault(t => t.Name == "PART_Track");
        if (track?.Thumb is not { } thumb || IsWithin(e.Source, thumb))
        {
            // A press on the thumb drags it, in WPF as in Avalonia.
            return;
        }

        if (moveToPoint)
        {
            // WPF's Slider.OnPreviewMouseLeftButtonDown with IsMoveToPointEnabled.
            double newValue = track.ValueFromPoint(e.GetPosition(track));
            if (double.IsFinite(newValue))
            {
                UpdateValue(slider, newValue);
            }
            e.Handled = true;
        }
        else if (track.DecreaseButton is { } decrease && IsWithin(e.Source, decrease))
        {
            new TrackPress(slider, decrease, -1).Start(e);
        }
        else if (track.IncreaseButton is { } increase && IsWithin(e.Source, increase))
        {
            new TrackPress(slider, increase, 1).Start(e);
        }
    }

    private static bool IsWithin(object? source, Visual element) =>
        source is Visual visual && (visual == element || element.IsVisualAncestorOf(visual));

    /// <summary>
    /// A press on one of the track's repeat buttons, which moves the thumb as WPF's <c>RepeatButton</c>
    /// clicks: at once, then on a timer while the button is pressed, which it is while the pointer is over
    /// it (WPF's <c>ButtonBase.UpdateIsPressed</c>, also run when the button changes size).
    /// </summary>
    private sealed class TrackPress
    {
        private readonly Slider _slider;
        private readonly Button _button;
        private readonly int _direction;
        private readonly DispatcherTimer _timer = new();
        private TopLevel? _root;
        private Point _pointer;
        private bool _isPressed;

        public TrackPress(Slider slider, Button button, int direction)
        {
            _slider = slider;
            _button = button;
            _direction = direction;
        }

        public void Start(PointerPressedEventArgs e)
        {
            // The track button and Avalonia's move-to-point don't see the press.
            e.Handled = true;
            _root = TopLevel.GetTopLevel(_button);
            _pointer = _root is null ? default : e.GetPosition(_root);

            _button.AddHandler(InputElement.PointerMovedEvent, OnMoved, handledEventsToo: true);
            _button.AddHandler(InputElement.PointerReleasedEvent, OnReleased, handledEventsToo: true);
            _button.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, handledEventsToo: true);
            _button.PropertyChanged += OnButtonPropertyChanged;
            e.Pointer.Capture(_button);

            // WPF's RepeatButton clicks on the press (ClickMode.Press), then repeats.
            _isPressed = true;
            Page();
            _timer.Interval = TimeSpan.FromMilliseconds(_button is RepeatButton repeat ? repeat.Delay : 500);
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var interval = TimeSpan.FromMilliseconds(_button is RepeatButton repeat ? repeat.Interval : 33);
            if (_timer.Interval != interval)
            {
                _timer.Interval = interval;
            }
            if (_isPressed)
            {
                Page();
            }
        }

        // WPF's Slider.OnDecreaseLarge and OnIncreaseLarge.
        private void Page() => MoveToNextTick(_slider, _direction * _slider.LargeChange);

        private void OnMoved(object? sender, PointerEventArgs e)
        {
            if (_root is not null)
            {
                _pointer = e.GetPosition(_root);
            }
            UpdateIsPressed();
        }

        private void OnButtonPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Visual.BoundsProperty)
            {
                UpdateIsPressed();
            }
        }

        private void UpdateIsPressed()
        {
            if (_root?.TranslatePoint(_pointer, _button) is { } p)
            {
                _isPressed = p.X >= 0 && p.X <= _button.Bounds.Width && p.Y >= 0 && p.Y <= _button.Bounds.Height;
            }
        }

        private void OnReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                End();
                e.Pointer.Capture(null);
            }
        }

        private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e) => End();

        private void End()
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _isPressed = false;
            _button.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
            _button.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
            _button.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
            _button.PropertyChanged -= OnButtonPropertyChanged;
        }
    }

    // WPF's Slider.MoveToNextTick: sets Value to the snapped value + direction, or else to the next tick
    // in that direction.
    private static void MoveToNextTick(Slider slider, double direction)
    {
        if (direction != 0.0)
        {
            double value = slider.Value;

            // Find the next value by snapping
            double next = SnapToTick(slider, Math.Max(slider.Minimum, Math.Min(slider.Maximum, value + direction)));

            bool greaterThan = direction > 0; //search for the next tick greater than value?

            // If the snapping brought us back to value, find the next tick point
            if (next == value
                && !(greaterThan && value == slider.Maximum)  // Stop if searching up if already at Max
                && !(!greaterThan && value == slider.Minimum)) // Stop if searching down if already at Min
            {
                // If ticks collection is available, use it.
                // Note that ticks may be unsorted.
                AvaloniaList<double>? ticks = slider.Ticks;
                if ((ticks != null) && (ticks.Count > 0))
                {
                    for (int i = 0; i < ticks.Count; i++)
                    {
                        double tick = ticks[i];

                        // Find the smallest tick greater than value or the largest tick less than value
                        if ((greaterThan && DoubleUtil.GreaterThan(tick, value) && (DoubleUtil.LessThan(tick, next) || next == value))
                         || (!greaterThan && DoubleUtil.LessThan(tick, value) && (DoubleUtil.GreaterThan(tick, next) || next == value)))
                        {
                            next = tick;
                        }
                    }
                }
                else if (DoubleUtil.GreaterThanZero(slider.TickFrequency))
                {
                    // Find the current tick we are at
                    double tickNumber = Math.Round((value - slider.Minimum) / slider.TickFrequency);

                    if (greaterThan)
                        tickNumber += 1.0;
                    else
                        tickNumber -= 1.0;

                    next = slider.Minimum + tickNumber * slider.TickFrequency;
                }
            }

            // Update if we've found a better value
            if (next != value)
            {
                slider.SetCurrentValue(RangeBase.ValueProperty, next);
            }
        }
    }

    // WPF's Slider.SnapToTick: the nearest tick, or the greater one at a tie.
    private static double SnapToTick(Slider slider, double value)
    {
        if (slider.IsSnapToTickEnabled)
        {
            double previous = slider.Minimum;
            double next = slider.Maximum;

            // If ticks collection is available, use it.
            // Note that ticks may be unsorted.
            AvaloniaList<double>? ticks = slider.Ticks;
            if ((ticks != null) && (ticks.Count > 0))
            {
                for (int i = 0; i < ticks.Count; i++)
                {
                    double tick = ticks[i];
                    if (DoubleUtil.AreClose(tick, value))
                    {
                        return value;
                    }

                    if (DoubleUtil.LessThan(tick, value) && DoubleUtil.GreaterThan(tick, previous))
                    {
                        previous = tick;
                    }
                    else if (DoubleUtil.GreaterThan(tick, value) && DoubleUtil.LessThan(tick, next))
                    {
                        next = tick;
                    }
                }
            }
            else if (DoubleUtil.GreaterThanZero(slider.TickFrequency))
            {
                previous = slider.Minimum + (Math.Round(((value - slider.Minimum) / slider.TickFrequency)) * slider.TickFrequency);
                next = Math.Min(slider.Maximum, previous + slider.TickFrequency);
            }

            // Choose the closest value between previous and next. If tie, snap to 'next'.
            value = DoubleUtil.GreaterThanOrClose(value, (previous + next) * 0.5) ? next : previous;
        }

        return value;
    }

    // WPF's Slider.UpdateValue: sets the snapped value, within the range.
    private static void UpdateValue(Slider slider, double value)
    {
        double snappedValue = SnapToTick(slider, value);

        if (snappedValue != slider.Value)
        {
            slider.SetCurrentValue(RangeBase.ValueProperty, Math.Max(slider.Minimum, Math.Min(slider.Maximum, snappedValue)));
        }
    }

    // WPF's DoubleUtil comparisons, which the slider code above uses.
    private static class DoubleUtil
    {
        private const double DBL_EPSILON = 2.2204460492503131e-016;

        public static bool AreClose(double value1, double value2)
        {
            if (value1 == value2)
            {
                return true;
            }
            double eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DBL_EPSILON;
            double delta = value1 - value2;
            return (-eps < delta) && (eps > delta);
        }

        public static bool LessThan(double value1, double value2) => (value1 < value2) && !AreClose(value1, value2);

        public static bool GreaterThan(double value1, double value2) => (value1 > value2) && !AreClose(value1, value2);

        public static bool GreaterThanOrClose(double value1, double value2) => (value1 > value2) || AreClose(value1, value2);

        public static bool GreaterThanZero(double value) => value >= 10.0 * DBL_EPSILON;
    }
}
