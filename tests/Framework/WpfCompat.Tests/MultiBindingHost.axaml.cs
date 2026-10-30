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

    /// <summary>A size shown through the plain converter.</summary>
    public static readonly StyledProperty<double> RawSizeProperty = AvaloniaProperty.Register<MultiBindingHost, double>(nameof(RawSize), double.NaN);

    /// <summary>A size shown through the wrapped converter.</summary>
    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<MultiBindingHost, double>(nameof(Size), double.NaN);

    /// <summary>Another size shown through the same wrapped converter resource.</summary>
    public static readonly StyledProperty<double> OtherSizeProperty = AvaloniaProperty.Register<MultiBindingHost, double>(nameof(OtherSize), 10);

    /// <summary>A size shown through the multi-binding.</summary>
    public static readonly StyledProperty<double> MultiSizeProperty = AvaloniaProperty.Register<MultiBindingHost, double>(nameof(MultiSize), double.NaN);

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

    /// <summary>A size shown through the plain converter.</summary>
    public double RawSize { get => GetValue(RawSizeProperty); set => SetValue(RawSizeProperty, value); }

    /// <summary>A size shown through the wrapped converter.</summary>
    public double Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Another size shown through the same wrapped converter resource.</summary>
    public double OtherSize { get => GetValue(OtherSizeProperty); set => SetValue(OtherSizeProperty, value); }

    /// <summary>A size shown through the multi-binding.</summary>
    public double MultiSize { get => GetValue(MultiSizeProperty); set => SetValue(MultiSizeProperty, value); }

    /// <summary>The box of the size shown through the plain converter.</summary>
    public TextBox RawSizeText => RawSizeBox;

    /// <summary>The box of the size shown through the wrapped converter.</summary>
    public TextBox SizeText => SizeBox;

    /// <summary>The box of the other size.</summary>
    public TextBox OtherSizeText => OtherSizeBox;

    /// <summary>The box of the size shown through the multi-binding.</summary>
    public TextBox MultiSizeText => MultiSizeBox;

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

/// <summary>Shows a NaN size as 12, as OxyPlotControls' font size converter does; converts text back to a number.</summary>
public sealed class DefaultSizeConverter : Avalonia.Data.Converters.IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double size && !double.IsNaN(size) ? size : 12.0;

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        double.TryParse(value as string, NumberStyles.Float, culture, out double size) ? size : double.NaN;
}

/// <summary>The same, for a multi-binding of one size.</summary>
public sealed class DefaultSizesConverter : IMultiValueConverter
{
    /// <inheritdoc/>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values[0] is double size && !double.IsNaN(size) ? size : 12.0;

    /// <inheritdoc/>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        [double.TryParse(value as string, NumberStyles.Float, culture, out double size) ? size : double.NaN];
}
