using System.Windows.Input;
using Avalonia.Input;

namespace WpfCompat.Input;

/// <summary>
/// A command that is handled by the <see cref="CommandBinding"/>s of the element it targets or of that
/// element's ancestors: WPF's <c>System.Windows.Input.RoutedCommand</c>.
/// </summary>
/// <remarks>
/// As in WPF, the command does nothing by itself. Executing it walks up from the target (by default
/// the element with keyboard focus) to the first binding for the command, and runs that binding's
/// handlers. Avalonia's buttons and menu items call it through <see cref="ICommand"/>, which has no
/// target, so they target the focused element of the active window.
/// </remarks>
public class RoutedCommand : ICommand
{
    /// <summary>Creates an unnamed command.</summary>
    public RoutedCommand()
        : this(string.Empty, typeof(RoutedCommand))
    {
    }

    /// <summary>Creates a command.</summary>
    /// <param name="name">The command's name, for diagnostics.</param>
    /// <param name="ownerType">The type that declares the command.</param>
    /// <param name="inputGestures">The key gestures that run the command.</param>
    public RoutedCommand(string name, Type ownerType, IEnumerable<KeyGesture>? inputGestures = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
        InputGestures = inputGestures?.ToList() ?? [];
    }

    /// <summary>The command's name.</summary>
    public string Name { get; }

    /// <summary>The type that declares the command.</summary>
    public Type OwnerType { get; }

    /// <summary>The key gestures that run the command where it has a binding.</summary>
    public IList<KeyGesture> InputGestures { get; }

    /// <summary>
    /// Raised when whether the command can run may have changed. As in WPF, this follows
    /// <see cref="CommandManager.RequerySuggested"/>.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>Whether the command can run on <paramref name="target"/>.</summary>
    /// <param name="parameter">The command parameter.</param>
    /// <param name="target">The element to start from, or null for the focused element.</param>
    public bool CanExecute(object? parameter, IInputElement? target) =>
        CommandManager.CanExecute(this, parameter, target);

    /// <summary>Runs the command on <paramref name="target"/>, if a binding allows it.</summary>
    /// <param name="parameter">The command parameter.</param>
    /// <param name="target">The element to start from, or null for the focused element.</param>
    public void Execute(object? parameter, IInputElement? target) =>
        CommandManager.Execute(this, parameter, target);

    bool ICommand.CanExecute(object? parameter) => CanExecute(parameter, null);

    void ICommand.Execute(object? parameter) => Execute(parameter, null);

    /// <inheritdoc/>
    public override string ToString() => Name;
}

/// <summary>A <see cref="RoutedCommand"/> with display text: WPF's <c>RoutedUICommand</c>.</summary>
public class RoutedUICommand : RoutedCommand
{
    /// <summary>Creates a command.</summary>
    /// <param name="text">The command's display text.</param>
    /// <param name="name">The command's name.</param>
    /// <param name="ownerType">The type that declares the command.</param>
    /// <param name="inputGestures">The key gestures that run the command.</param>
    public RoutedUICommand(string text, string name, Type ownerType, IEnumerable<KeyGesture>? inputGestures = null)
        : base(name, ownerType, inputGestures)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>The command's display text, for example for a menu item.</summary>
    public string Text { get; set; }
}
