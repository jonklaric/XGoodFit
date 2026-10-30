using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using WpfCompat.Data;

namespace WpfCompat.Tests;

/// <summary>XGoodFit addition: three string sources and two text boxes that join them (see MultiBindingTests).</summary>
public partial class MultiBindingHost : UserControl
{
    /// <summary>The first source.</summary>
    public static readonly StyledProperty<string?> FirstProperty = AvaloniaProperty.Register<MultiBindingHost, string?>(nameof(First), "a");

    /// <summary>The second source.</summary>
    public static readonly StyledProperty<string?> SecondProperty = AvaloniaProperty.Register<MultiBindingHost, string?>(nameof(Second), "b");

    /// <summary>The third source.</summary>
    public static readonly StyledProperty<string?> ThirdProperty = AvaloniaProperty.Register<MultiBindingHost, string?>(nameof(Third), "c");

    /// <summary>Creates the host.</summary>
    public MultiBindingHost()
    {
        InitializeComponent();
    }

    /// <summary>The first source.</summary>
    public string? First { get => GetValue(FirstProperty); set => SetValue(FirstProperty, value); }

    /// <summary>The second source.</summary>
    public string? Second { get => GetValue(SecondProperty); set => SetValue(SecondProperty, value); }

    /// <summary>The third source.</summary>
    public string? Third { get => GetValue(ThirdProperty); set => SetValue(ThirdProperty, value); }

    /// <summary>The two-way joined box.</summary>
    public TextBox TwoWay => TwoWayBox;

    /// <summary>The one-way joined box.</summary>
    public TextBox OneWay => OneWayBox;
}

/// <summary>Joins strings with '|' and splits them back; a part "-" leaves its source as it is.</summary>
public sealed class JoinConverter : IMultiValueConverter
{
    /// <summary>The cultures the converter was given.</summary>
    public static List<CultureInfo> Cultures { get; } = [];

    /// <inheritdoc/>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        Cultures.Add(culture);
        return string.Join("|", values.Select(v => v as string ?? "?"));
    }

    /// <inheritdoc/>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        ((string)value).Split('|').Select(s => s == "-" ? BindingOperations.DoNothing : (object)s).ToArray();
}
