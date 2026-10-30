using System.Collections;
using Avalonia.Metadata;

namespace WpfCompat.Markup;

/// <summary>
/// WPF's <c>x:Array</c>, which Avalonia's XAML lacks: an array of <see cref="Type"/> with the items
/// written inside it.
/// </summary>
/// <example>
/// <code><![CDATA[
/// <wpfm:ArrayExtension x:Key="LineWidthOptions" Type="{x:Type sys:Double}">
///     <sys:Double>0</sys:Double>
///     <sys:Double>1</sys:Double>
/// </wpfm:ArrayExtension>
/// ]]></code>
/// with <c>xmlns:wpfm="using:WpfCompat.Markup"</c>.
/// </example>
public sealed class ArrayExtension
{
    /// <summary>The type of the array's items.</summary>
    public Type? Type { get; set; }

    /// <summary>The array's items.</summary>
    [Content]
    public IList Items { get; } = new List<object?>();

    /// <summary>Makes the array.</summary>
    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var array = Array.CreateInstance(Type ?? throw new InvalidOperationException("x:Array needs a Type."), Items.Count);
        Items.CopyTo(array, 0);
        return array;
    }
}
