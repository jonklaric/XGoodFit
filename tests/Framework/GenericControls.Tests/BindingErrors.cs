using Avalonia.Logging;

namespace GenericControls.Tests;

/// <summary>
/// Collects the binding and property errors Avalonia logs while it's in use, so a test can check that a
/// control's XAML binds as it should (Avalonia logs failed bindings rather than throwing).
/// </summary>
internal sealed class BindingErrors : ILogSink, IDisposable
{
    private readonly ILogSink? _previous;

    public BindingErrors()
    {
        _previous = Logger.Sink;
        Logger.Sink = this;
    }

    /// <summary>The messages logged so far.</summary>
    public List<string> Messages { get; } = [];

    public bool IsEnabled(LogEventLevel level, string area) =>
        level >= LogEventLevel.Warning && area is LogArea.Binding or LogArea.Property;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
        Messages.Add($"[{area}] {source}: {messageTemplate}");

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        string message = messageTemplate;
        foreach (object? value in propertyValues)
        {
            int start = message.IndexOf('{');
            int end = start < 0 ? -1 : message.IndexOf('}', start);
            if (end < 0)
            {
                break;
            }
            message = message[..start] + value + message[(end + 1)..];
        }
        Messages.Add($"[{area}] {source}: {message}");
    }

    public void Dispose() => Logger.Sink = _previous;
}
