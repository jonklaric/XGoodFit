using Avalonia.Controls;

namespace ThemeGallery;

/// <summary>A GenericControls page of the gallery; see its XAML.</summary>
public partial class GenericDateTimeColorPage : UserControl
{
    public GenericDateTimeColorPage()
    {
        InitializeComponent();
        DataContext = GenericControlsSample.Instance;
    }
}
