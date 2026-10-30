using System.Collections.ObjectModel;
using System.Windows.Input;

namespace WpfCompat.Input;

/// <summary>Handles <see cref="CommandBinding.Executed"/>: WPF's <c>ExecutedRoutedEventHandler</c>.</summary>
public delegate void ExecutedRoutedEventHandler(object sender, ExecutedRoutedEventArgs e);

/// <summary>Handles <see cref="CommandBinding.CanExecute"/>: WPF's <c>CanExecuteRoutedEventHandler</c>.</summary>
public delegate void CanExecuteRoutedEventHandler(object sender, CanExecuteRoutedEventArgs e);

/// <summary>The arguments of a routed command's execution: WPF's <c>ExecutedRoutedEventArgs</c>.</summary>
public sealed class ExecutedRoutedEventArgs : EventArgs
{
    internal ExecutedRoutedEventArgs(ICommand command, object? parameter, object? source)
    {
        Command = command;
        Parameter = parameter;
        Source = source;
        OriginalSource = source;
    }

    /// <summary>The command being executed.</summary>
    public ICommand Command { get; }

    /// <summary>The command parameter.</summary>
    public object? Parameter { get; }

    /// <summary>The element the command was executed on.</summary>
    public object? Source { get; }

    /// <summary>The element the command was executed on (WPF distinguishes it for events that cross templates).</summary>
    public object? OriginalSource { get; }

    /// <summary>Whether a binding has handled the command; stops the routing.</summary>
    public bool Handled { get; set; }
}

/// <summary>The arguments of a routed command's can-execute query: WPF's <c>CanExecuteRoutedEventArgs</c>.</summary>
public sealed class CanExecuteRoutedEventArgs : EventArgs
{
    internal CanExecuteRoutedEventArgs(ICommand command, object? parameter, object? source)
    {
        Command = command;
        Parameter = parameter;
        Source = source;
        OriginalSource = source;
    }

    /// <summary>The command being queried.</summary>
    public ICommand Command { get; }

    /// <summary>The command parameter.</summary>
    public object? Parameter { get; }

    /// <summary>The element the command is queried on.</summary>
    public object? Source { get; }

    /// <summary>The element the command is queried on.</summary>
    public object? OriginalSource { get; }

    /// <summary>Whether the command can run.</summary>
    public bool CanExecute { get; set; }

    /// <summary>
    /// Whether the input event that invoked the command should carry on routing. Kept for source
    /// compatibility; in WPF it only affects key gestures.
    /// </summary>
    public bool ContinueRouting { get; set; }

    /// <summary>Whether a binding has answered the query; stops the routing.</summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Binds a <see cref="RoutedCommand"/> to the handlers that run it on an element: WPF's
/// <c>System.Windows.Input.CommandBinding</c>. Add bindings to an element's
/// <c>CommandBindings</c> (<see cref="CommandManager.GetCommandBindings"/>).
/// </summary>
public class CommandBinding
{
    /// <summary>Creates an empty binding.</summary>
    public CommandBinding()
    {
    }

    /// <summary>Creates a binding for <paramref name="command"/>.</summary>
    public CommandBinding(ICommand command)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
    }

    /// <summary>Creates a binding for <paramref name="command"/> that runs <paramref name="executed"/>.</summary>
    public CommandBinding(ICommand command, ExecutedRoutedEventHandler executed)
        : this(command)
    {
        Executed += executed ?? throw new ArgumentNullException(nameof(executed));
    }

    /// <summary>
    /// Creates a binding for <paramref name="command"/> that runs <paramref name="executed"/> when
    /// <paramref name="canExecute"/> allows.
    /// </summary>
    public CommandBinding(ICommand command, ExecutedRoutedEventHandler executed, CanExecuteRoutedEventHandler canExecute)
        : this(command, executed)
    {
        CanExecute += canExecute ?? throw new ArgumentNullException(nameof(canExecute));
    }

    /// <summary>The command this binding handles.</summary>
    public ICommand? Command { get; set; }

    /// <summary>Runs the command.</summary>
    public event ExecutedRoutedEventHandler? Executed;

    /// <summary>Decides whether the command can run.</summary>
    public event CanExecuteRoutedEventHandler? CanExecute;

    // WPF's CommandBinding.OnCanExecute: a CanExecute handler that allows the command answers the query;
    // without a handler, a binding with an Executed handler allows it.
    internal void OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (CanExecute != null)
        {
            CanExecute(sender, e);
            if (e.CanExecute)
            {
                e.Handled = true;
            }
        }
        else if (!e.CanExecute && Executed != null)
        {
            e.CanExecute = true;
            e.Handled = true;
        }
    }

    // WPF's CommandBinding.OnExecuted: runs only if this binding's own can-execute check allows it.
    internal void OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Handled || Executed == null)
        {
            return;
        }

        var canExecuteArgs = new CanExecuteRoutedEventArgs(e.Command, e.Parameter, e.Source);
        OnCanExecute(sender, canExecuteArgs);
        if (canExecuteArgs.CanExecute)
        {
            Executed(sender, e);
            e.Handled = true;
        }
    }
}

/// <summary>An element's command bindings: WPF's <c>CommandBindingCollection</c>.</summary>
public sealed class CommandBindingCollection : Collection<CommandBinding>
{
    /// <inheritdoc/>
    protected override void InsertItem(int index, CommandBinding item)
    {
        base.InsertItem(index, item ?? throw new ArgumentNullException(nameof(item)));
        CommandManager.InvalidateRequerySuggested();
    }

    /// <inheritdoc/>
    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        CommandManager.InvalidateRequerySuggested();
    }
}
