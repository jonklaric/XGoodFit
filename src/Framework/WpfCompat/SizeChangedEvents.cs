using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;

namespace WpfCompat;

/// <summary>
/// WPF's <c>FrameworkElement.SizeChanged</c>, raised when WPF raises it: once the layout pass is done, so a
/// handler sees every element's new size. <c>element.AddSizeChangedHandler(H)</c> for WPF's
/// <c>SizeChanged += H</c> or XAML's <c>SizeChanged="H"</c>.
/// </summary>
/// <remarks>
/// <para>
/// XGoodFit addition. Avalonia raises <c>SizeChanged</c> while it arranges the element, before its
/// parents have their new sizes; WPF's layout raises it after arranging everything. A handler that reads
/// another element's size (a scroll viewer's viewport, say) gets the old one in Avalonia. These helpers
/// note Avalonia's event and call the handler from the element's next <c>LayoutUpdated</c>, after the
/// pass, with the size before the first change and after the last.
/// </para>
/// <para>
/// As in WPF, a handler that changes the layout makes another pass, which may raise the event again.
/// Each Remove method removes a handler its Add method added.
/// </para>
/// </remarks>
public static class SizeChangedEvents
{
    // The subscription made for each handler, so the Remove method can undo it.
    private static readonly ConditionalWeakTable<Control, Dictionary<EventHandler<SizeChangedEventArgs>, Subscription>> Subscriptions = new();

    private sealed class Subscription(Control control, EventHandler<SizeChangedEventArgs> handler)
    {
        private Size? _previousSize;
        private Size _newSize;

        public void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (_previousSize is null)
            {
                _previousSize = e.PreviousSize;
                control.LayoutUpdated += OnLayoutUpdated;
            }
            _newSize = e.NewSize;
        }

        public void OnLayoutUpdated(object? sender, EventArgs e)
        {
            control.LayoutUpdated -= OnLayoutUpdated;
            Size previous = _previousSize ?? _newSize;
            _previousSize = null;
            if (previous != _newSize)
            {
                handler(control, new SizeChangedEventArgs(Control.SizeChangedEvent, control, previous, _newSize));
            }
        }

        public void Detach()
        {
            control.SizeChanged -= OnSizeChanged;
            control.LayoutUpdated -= OnLayoutUpdated;
        }
    }

    extension(Control control)
    {
        /// <summary>WPF's <c>SizeChanged</c>: raised after the layout pass in which the size changed.</summary>
        public void AddSizeChangedHandler(EventHandler<SizeChangedEventArgs> handler)
        {
            var subscription = new Subscription(control, handler);
            Subscriptions.GetOrCreateValue(control)[handler] = subscription;
            control.SizeChanged += subscription.OnSizeChanged;
        }

        /// <summary>Removes a <c>SizeChanged</c> handler.</summary>
        public void RemoveSizeChangedHandler(EventHandler<SizeChangedEventArgs> handler)
        {
            if (Subscriptions.TryGetValue(control, out var subscriptions) && subscriptions.Remove(handler, out Subscription? subscription))
            {
                subscription.Detach();
            }
        }
    }
}
