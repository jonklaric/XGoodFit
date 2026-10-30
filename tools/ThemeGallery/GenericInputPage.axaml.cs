using Avalonia.Controls;

namespace ThemeGallery;

/// <summary>A GenericControls page of the gallery; see its XAML.</summary>
public partial class GenericInputPage : UserControl
{
    public GenericInputPage()
    {
        InitializeComponent();
        DataContext = GenericControlsSample.Instance;
    }
}
