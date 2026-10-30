using System.Globalization;

namespace WpfCompat;

/// <summary>
/// The culture WPF bindings convert values with: the target element's <c>Language</c>, which is en-US
/// unless a program overrides it, and BestFit doesn't. So its bound fields show and read numbers and
/// dates as en-US whatever the machine's culture (docs/migration-plan.md §5.2, decision D8).
/// </summary>
/// <remarks>
/// Avalonia bindings convert with the current culture. A ported binding that turns values into text or
/// text into values, and names no culture of its own, names this one:
/// <c>ConverterCulture={x:Static wpf:FormattingCulture.Current}</c>. Code-behind keeps the current
/// culture, as upstream's does. Bindings read the culture when their XAML loads.
/// </remarks>
public static class FormattingCulture
{
    /// <summary>The culture bindings convert values with: en-US, as in WPF.</summary>
    public static CultureInfo Current { get; set; } = CultureInfo.GetCultureInfo("en-US");
}
