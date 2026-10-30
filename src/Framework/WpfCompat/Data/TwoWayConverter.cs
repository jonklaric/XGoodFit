using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Metadata;

namespace WpfCompat.Data;

/// <summary>
/// WPF's two-way binding for a converter that doesn't round-trip: a binding doesn't write back the
/// value it has just shown. Wrap the converter's resource in this, with <c>x:Shared="False"</c> so each
/// binding gets its own:
/// <c>&lt;wpfd:TwoWayConverter x:Key="…" x:Shared="False"&gt;&lt;local:SomeConverter/&gt;&lt;/wpfd:TwoWayConverter&gt;</c>,
/// with <c>xmlns:wpfd="using:WpfCompat.Data"</c>.
/// </summary>
/// <remarks>
/// <para>
/// A WPF binding never updates its source while it transfers a value to its target. An Avalonia
/// binding does: when the converted value lands in the target, it converts the target's value back and
/// writes it to the source if it differs. With a converter that shows a default for a missing value
/// (a NaN font size shown as 12, an automatic colour as the colour it will be, a hidden visibility as
/// unchecked) merely showing the value replaces it: the editor changes the plot it's opened on, and
/// an undo system records the change.
/// </para>
/// <para>
/// This keeps the value its converter last gave the target. While the target is converted back with
/// that value, it's the binding's own echo, so the source is left alone
/// (<see cref="BindingOperations.DoNothing"/>); any other value is an edit, converted back as usual. Values
/// match when they're equal, are brushes or colours of the same colour, are sequences of the same
/// numbers (dash arrays), are date-times in the same second, or are a value and its text (a text
/// box's): the forms a target may give back what it was shown in. A target that doesn't keep a null it
/// was shown gives back its stand-in for none, its type's default or a brush of the default colour (the
/// colour editors' pickers, shown a plot's missing border brush), which is an echo too. A shared
/// instance would mistake one binding's echo for another's edit, hence <c>x:Shared="False"</c>.
/// </para>
/// <para>
/// WPF ignores every change its binding's target makes while the value is transferred, even to another
/// value; this tells such a change apart from an edit only by its value, so a target that changes the
/// value it's given into a different one still writes that back.
/// </para>
/// <para>XGoodFit addition, for upstream's property editors (OxyPlotControls).</para>
/// </remarks>
public sealed class TwoWayConverter : IValueConverter
{
    private bool _hasShown;
    private object? _shown;

    /// <summary>The converter that does the conversions.</summary>
    [Content]
    public IValueConverter? Converter { get; set; }

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        object? shown = Converter is null ? value : Converter.Convert(value, targetType, parameter, culture);
        _shown = shown;
        _hasShown = true;
        return shown;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // The target can echo more than once: a control that swaps the value for its own copy changes it
        // again while the first change is reported. The shown value stays until an edit replaces it.
        if (_hasShown && (SameValue(value, _shown) || (_shown is null && IsDefault(value))))
        {
            return BindingOperations.DoNothing;
        }
        _hasShown = false;
        return Converter is null ? value : Converter.ConvertBack(value, targetType, parameter, culture);
    }

    // Whether the value is a target's stand-in for null: its value type's default, or a brush of the default
    // colour.
    private static bool IsDefault(object? value) =>
        (value is not null && value.GetType().IsValueType && value.Equals(Activator.CreateInstance(value.GetType())))
        || (value is ISolidColorBrush && ColorOf(value) == default(Color));

    /// <summary>
    /// Whether a target's value is the one a binding gave it: equal, the same colour (a brush may be
    /// copied or made from a colour on the way), the same numbers, the same second, or a value and its
    /// text.
    /// </summary>
    /// <param name="a">One value.</param>
    /// <param name="b">The other.</param>
    /// <returns>Whether they're the same value.</returns>
    public static bool SameValue(object? a, object? b)
    {
        if (Equals(a, b))
        {
            return true;
        }
        if (ColorOf(a) is { } colorA && ColorOf(b) is { } colorB)
        {
            return colorA == colorB;
        }
        if (a is IEnumerable<double> numbersA && b is IEnumerable<double> numbersB)
        {
            return numbersA.SequenceEqual(numbersB);
        }
        if (a is DateTime dateA && b is DateTime dateB)
        {
            // A date and time picker that keeps whole seconds (GenericControls') drops the rest.
            return dateA.Ticks / TimeSpan.TicksPerSecond == dateB.Ticks / TimeSpan.TicksPerSecond;
        }

        // A target of another type holds the value converted to it, as a text box holds a number's text.
        return (a is string && b is IConvertible && SameText((string)a, b)) || (b is string && a is IConvertible && SameText((string)b, a));
    }

    private static bool SameText(string text, object value)
    {
        try
        {
            return Equals(System.Convert.ChangeType(text, value.GetType(), FormattingCulture.Current), value);
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }

    private static Color? ColorOf(object? value) => value switch
    {
        Color color => color,
        ISolidColorBrush brush => brush.Opacity == 1 ? brush.Color : Color.FromArgb((byte)Math.Round(brush.Color.A * brush.Opacity), brush.Color.R, brush.Color.G, brush.Color.B),
        _ => null,
    };
}
