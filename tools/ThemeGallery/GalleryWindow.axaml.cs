using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Themes;

namespace ThemeGallery;

/// <summary>
/// Shows the standard controls in the ported theme, GenericControls' data grids, and every palette key as a
/// live swatch grouped by the control it styles, on three tabs. The menu bar is a live sample of the Menu theme.
/// </summary>
public partial class GalleryWindow : GenericControls.MetroWindow
{
    public GalleryWindow()
    {
        InitializeComponent();

        CPDataGrid.ItemsSource = DataGridSamples.CreateSampleDataItems();
        ValidationGrid.RowType = typeof(SampleRuleRow);
        ValidationGrid.ItemsSource = DataGridSamples.CreateRuleRows(ValidationGrid);

        // The keys are the same in every palette; take them from the Light one.
        var palette = (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri(ThemeResourceHelper.LightColorsUri));
        List<string> keys = palette.Keys.OfType<string>().Order(StringComparer.Ordinal).ToList();
        Summary.Text = $"{keys.Count} palette keys";

        foreach (IGrouping<string, string> group in keys.GroupBy(GroupName))
        {
            var swatches = new WrapPanel();
            foreach (string key in group)
            {
                swatches.Children.Add(CreateSwatch(key));
            }
            Groups.Items.Add(new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 6),
                Children =
                {
                    new TextBlock { Text = group.Key, FontWeight = FontWeight.Bold },
                    swatches,
                },
            });
        }
    }

    /// <summary>Applies a theme and ticks its button.</summary>
    public void SelectTheme(Themes.Theme theme)
    {
        ThemeService.Instance.SetTheme(theme);
        LightTheme.IsChecked = theme == Themes.Theme.Light;
        BlueTheme.IsChecked = theme == Themes.Theme.Blue;
        DarkTheme.IsChecked = theme == Themes.Theme.Dark;
    }

    private void OnThemeChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true } button && ThemeService.Instance.IsInitialized)
        {
            ThemeService.Instance.SetTheme(ThemeResourceHelper.ParseTheme((string)button.Content!));
        }
    }

    // "Button.Static.Background" -> "Button"; keys without a dot are environment colours.
    private static string GroupName(string key) => key.Contains('.') ? key[..key.IndexOf('.')] : "Environment";

    private static Control CreateSwatch(string key)
    {
        var colour = new Border
        {
            Width = 22,
            Height = 14,
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center,
        };
        // Colour resources are shown through a brush; brush resources directly. Both follow theme changes.
        colour.Bind(Border.BackgroundProperty, colour.GetResourceObservable(key, value => value is Color c ? new SolidColorBrush(c) : value));

        string label = key.Contains('.') ? key[(key.IndexOf('.') + 1)..] : key;
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Width = 230,
            Margin = new Thickness(0, 1, 6, 1),
            Children =
            {
                colour,
                new TextBlock { Text = label, Width = 200, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center },
            },
            [ToolTip.TipProperty] = key,
        };
    }
}
