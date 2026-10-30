using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace WpfCompat.Input;

/// <summary>
/// Gives a button or menu item a <see cref="RoutedCommand"/> that routes as it would in WPF:
/// <c>Command="{wpf:Command Save}"</c> or
/// <c>Command="{wpf:Command {x:Static wpf:SystemCommands.CloseWindowCommand}}"</c>.
/// </summary>
/// <remarks>
/// WPF routes a command from its source: a button's command starts at the button, and a menu item's
/// at the element with keyboard focus (menus are focus scopes). Avalonia calls a command without
/// saying where from, so this extension wraps the command with its source element. A name resolves to
/// one of the <see cref="ApplicationCommands"/>, as WPF's <c>Command="Save"</c> does.
/// </remarks>
public sealed class CommandExtension
{
    /// <summary>Creates the extension.</summary>
    public CommandExtension()
    {
    }

    /// <summary>Creates the extension for a command or an <see cref="ApplicationCommands"/> name.</summary>
    public CommandExtension(object command)
    {
        Command = command;
    }

    /// <summary>A <see cref="RoutedCommand"/>, or the name of one of the <see cref="ApplicationCommands"/>.</summary>
    public object? Command { get; set; }

    /// <summary>Returns the command, bound to the element it's set on.</summary>
    public ICommand ProvideValue(IServiceProvider serviceProvider)
    {
        RoutedCommand command = Command switch
        {
            RoutedCommand routed => routed,
            string name => ApplicationCommands.FromName(name)
                ?? throw new ArgumentException($"'{name}' isn't one of the ApplicationCommands."),
            _ => throw new ArgumentException("Command must be a RoutedCommand or the name of an ApplicationCommand."),
        };

        var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        return target?.TargetObject is InputElement source ? new RoutedCommandSource(command, source) : command;
    }
}

/// <summary>A <see cref="RoutedCommand"/> as its source element invokes it (see <see cref="CommandExtension"/>).</summary>
public sealed class RoutedCommandSource : ICommand
{
    /// <summary>Wraps <paramref name="command"/> for <paramref name="source"/>.</summary>
    public RoutedCommandSource(RoutedCommand command, InputElement source)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>The command.</summary>
    public RoutedCommand Command { get; }

    /// <summary>The element that invokes it.</summary>
    public InputElement Source { get; }

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => Command.CanExecute(parameter, Target);

    /// <inheritdoc/>
    public void Execute(object? parameter) => Command.Execute(parameter, Target);

    // Inside a menu (WPF's focus scope), the command applies to the focused element; elsewhere, to the source.
    private IInputElement? Target => CommandManager.IsInMenu(Source) ? null : Source;
}
