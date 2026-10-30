using Avalonia.Controls;

namespace ThemeGallery;

/// <summary>A GenericControls page of the gallery; see its XAML.</summary>
public partial class GenericPropertyEditorsPage : UserControl
{
    public GenericPropertyEditorsPage()
    {
        InitializeComponent();
        DataContext = GenericControlsSample.Instance;
    }
}
