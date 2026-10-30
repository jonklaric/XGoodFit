using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace WpfCompat.Input;

/// <summary>
/// Routes <see cref="RoutedCommand"/>s to <see cref="CommandBinding"/>s, holds each element's command
/// bindings, and tells command sources when to re-check their commands: the parts of WPF's
/// <c>System.Windows.Input.CommandManager</c> that the ported code uses.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Routing starts at the target element and walks up the visual tree to its window. From a popup
/// it carries on from the popup's owner, as WPF's routed events do.</item>
/// <item>Key gestures: when a key press isn't handled, each element it bubbles through checks its own
/// bindings for a command whose <see cref="RoutedCommand.InputGestures"/> match, as WPF does.</item>
/// <item><see cref="RequerySuggested"/> is raised after keyboard and pointer input and focus changes,
/// as in WPF.</item>
/// </list>
/// The input handlers start the first time the class is used, typically when a window adds command
/// bindings or a button binds a command. Until then the focused element comes from the window.
/// </remarks>
public static class CommandManager
{
    /// <summary>An element's command bindings. Use <see cref="GetCommandBindings"/> to read and add to them.</summary>
    public static readonly AttachedProperty<CommandBindingCollection?> CommandBindingsProperty =
        AvaloniaProperty.RegisterAttached<InputElement, CommandBindingCollection?>("CommandBindings", typeof(CommandManager));

    private static bool _requeryPending;
    private static WeakReference<InputElement>? _focused;
    private static WeakReference<InputElement>? _focusedOutsideMenus;

    static CommandManager()
    {
        InputElement.KeyDownEvent.AddClassHandler<InputElement>(OnKeyDown);
        InputElement.KeyUpEvent.AddClassHandler<InputElement>((_, _) => InvalidateRequerySuggested(), handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<InputElement>((_, _) => InvalidateRequerySuggested(), handledEventsToo: true);
        InputElement.GotFocusEvent.AddClassHandler<InputElement>(OnGotFocus, handledEventsToo: true);
    }


    /// <summary>
    /// Raised when commands should check again whether they can run. <see cref="RoutedCommand"/>s pass
    /// it on as their <see cref="ICommand.CanExecuteChanged"/>.
    /// </summary>
    public static event EventHandler? RequerySuggested;

    /// <summary>Returns <paramref name="element"/>'s command bindings, creating the collection if needed.</summary>
    public static CommandBindingCollection GetCommandBindings(InputElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element.GetValue(CommandBindingsProperty) is not { } bindings)
        {
            bindings = [];
            element.SetValue(CommandBindingsProperty, bindings);
        }
        return bindings;
    }

    /// <summary>Asks command sources to check their commands again, once the current input is processed.</summary>
    public static void InvalidateRequerySuggested()
    {
        if (_requeryPending)
        {
            return;
        }
        _requeryPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _requeryPending = false;
            RequerySuggested?.Invoke(null, EventArgs.Empty);
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// The element a command without an explicit target applies to: the element with keyboard focus, as
    /// in WPF. Menus are focus scopes in WPF, so while one has focus, the target is the element that had
    /// it before. Without a focused element, it's the active window.
    /// </summary>
    public static IInputElement? FocusedTarget()
    {
        if (TryGetLive(_focused, out InputElement? focused) && focused.IsFocused)
        {
            return IsInMenu(focused) && TryGetLive(_focusedOutsideMenus, out InputElement? beforeMenu) ? beforeMenu : focused;
        }
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Window? window = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow ?? desktop.Windows.FirstOrDefault();
            if (window != null)
            {
                return window.FocusManager?.GetFocusedElement() ?? window;
            }
        }
        return null;
    }

    /// <summary>Whether <paramref name="element"/> is in a menu or context menu, WPF's focus scopes for commands.</summary>
    internal static bool IsInMenu(StyledElement element) =>
        element is MenuBase || element.GetLogicalAncestors().OfType<MenuBase>().Any();

    // The element a weak reference holds, if it's still in a window.
    private static bool TryGetLive(WeakReference<InputElement>? reference, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out InputElement? element)
    {
        element = null;
        return reference != null && reference.TryGetTarget(out element) && TopLevel.GetTopLevel(element) != null;
    }

    private static void OnGotFocus(InputElement element, RoutedEventArgs e)
    {
        if (e.Source == element)
        {
            _focused = new WeakReference<InputElement>(element);
            if (!IsInMenu(element))
            {
                _focusedOutsideMenus = new WeakReference<InputElement>(element);
            }
        }
        InvalidateRequerySuggested();
    }

    internal static bool CanExecute(RoutedCommand command, object? parameter, IInputElement? target)
    {
        target ??= FocusedTarget();
        var args = new CanExecuteRoutedEventArgs(command, parameter, target);
        foreach ((InputElement element, CommandBinding binding) in Route(command, target))
        {
            binding.OnCanExecute(element, args);
            if (args.Handled)
            {
                break;
            }
        }
        return args.CanExecute;
    }

    internal static void Execute(RoutedCommand command, object? parameter, IInputElement? target)
    {
        target ??= FocusedTarget();
        var args = new ExecutedRoutedEventArgs(command, parameter, target);
        foreach ((InputElement element, CommandBinding binding) in Route(command, target))
        {
            binding.OnExecuted(element, args);
            if (args.Handled)
            {
                break;
            }
        }
        InvalidateRequerySuggested();
    }

    // The bindings for a command from the target up, in order.
    private static IEnumerable<(InputElement Element, CommandBinding Binding)> Route(ICommand command, IInputElement? target)
    {
        var visited = new HashSet<Visual>();
        for (Visual? visual = target as Visual; visual != null && visited.Add(visual); visual = Parent(visual))
        {
            if (visual is InputElement element && element.GetValue(CommandBindingsProperty) is { Count: > 0 } bindings)
            {
                foreach (CommandBinding binding in bindings.Where(b => b.Command == command).ToList())
                {
                    yield return (element, binding);
                }
            }
        }
    }

    // The next element up a route: the visual parent, up to the window. A popup's root carries on from
    // the popup that owns it.
    private static Visual? Parent(Visual visual) => visual switch
    {
        PopupRoot popupRoot => popupRoot.Parent as Visual,
        TopLevel => null,
        _ => visual.GetVisualParent(),
    };

    private static void OnKeyDown(InputElement element, KeyEventArgs e)
    {
        if (e.Handled || element.GetValue(CommandBindingsProperty) is not { Count: > 0 } bindings)
        {
            return;
        }

        foreach (CommandBinding binding in bindings.ToList())
        {
            if (binding.Command is RoutedCommand command && command.InputGestures.Any(g => g.Matches(e))
                && CanExecute(command, null, element))
            {
                Execute(command, null, element);
                e.Handled = true;
                return;
            }
        }
    }
}

/// <summary>Gives every element WPF's <c>CommandBindings</c> property.</summary>
public static class CommandBindingsExtensions
{
    extension(InputElement element)
    {
        /// <summary>The element's command bindings (<see cref="CommandManager.GetCommandBindings"/>).</summary>
        public CommandBindingCollection CommandBindings => CommandManager.GetCommandBindings(element);
    }
}
