using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using GenericControls;
using WpfCompat.Media;

namespace ThemeGallery;

/// <summary>
/// The sample values the GenericControls pages show: those of upstream's GenericControls.Demo, which its
/// gallery screenshots were taken from (upstream/WPF-Framework/docs/assets/gallery), so captures of the
/// pages can be compared with them. The date is the one in the screenshots, rather than the time now.
/// </summary>
public sealed class GenericControlsSample : INotifyPropertyChanged
{
    /// <summary>The values every page shares, as the demo window's DataContext.</summary>
    public static GenericControlsSample Instance { get; } = new();

    private string _nameProperty = "Sample Name";
    private string _textProperty = "Sample Text";
    private string _descriptionProperty = "This is a sample description that demonstrates the resizable text property control.";
    private string _fontProperty = "Segoe UI";
    private FontWeight _fontWeightProperty = FontWeight.Normal;
    private bool _booleanProperty = true;
    private double _numericProperty = 25.0;
    private double _numericAutoProperty = double.NaN;
    private double _opacityProperty = 0.8;
    private double _lineWidthProperty = 2.0;
    private DoubleCollection _lineStyleProperty = LineStyleSelectorControl.LineStyleOptions[0];
    private SolidColorBrush _colorProperty = new(Color.FromArgb(200, 70, 130, 180));
    private HorizontalAlignment _horizontalAlignmentProperty = HorizontalAlignment.Center;
    private VerticalAlignment _verticalAlignmentProperty = VerticalAlignment.Center;
    private Point _pointProperty = new(100, 200);
    private DateTime _dateTimeProperty = new(2026, 7, 1, 9, 31, 44);
    private string _filePathProperty = "";

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    public string NameProperty { get => _nameProperty; set => Set(ref _nameProperty, value); }
    public string TextProperty { get => _textProperty; set => Set(ref _textProperty, value); }
    public string DescriptionProperty { get => _descriptionProperty; set => Set(ref _descriptionProperty, value); }
    public string FontProperty { get => _fontProperty; set => Set(ref _fontProperty, value); }
    public FontWeight FontWeightProperty { get => _fontWeightProperty; set => Set(ref _fontWeightProperty, value); }
    public bool BooleanProperty { get => _booleanProperty; set => Set(ref _booleanProperty, value); }
    public double NumericProperty { get => _numericProperty; set => Set(ref _numericProperty, value); }
    public double NumericAutoProperty { get => _numericAutoProperty; set => Set(ref _numericAutoProperty, value); }
    public double OpacityProperty { get => _opacityProperty; set => Set(ref _opacityProperty, value); }
    public double LineWidthProperty { get => _lineWidthProperty; set => Set(ref _lineWidthProperty, value); }
    public DoubleCollection LineStyleProperty { get => _lineStyleProperty; set => Set(ref _lineStyleProperty, value); }
    public SolidColorBrush ColorProperty { get => _colorProperty; set => Set(ref _colorProperty, value); }
    public HorizontalAlignment HorizontalAlignmentProperty { get => _horizontalAlignmentProperty; set => Set(ref _horizontalAlignmentProperty, value); }
    public VerticalAlignment VerticalAlignmentProperty { get => _verticalAlignmentProperty; set => Set(ref _verticalAlignmentProperty, value); }
    public Point PointProperty { get => _pointProperty; set => Set(ref _pointProperty, value); }
    public DateTime DateTimeProperty { get => _dateTimeProperty; set => Set(ref _dateTimeProperty, value); }
    public string FilePathProperty { get => _filePathProperty; set => Set(ref _filePathProperty, value); }

    /// <summary>The demo's line width options (its LineWidthOptions resource).</summary>
    public IList<double> LineWidthOptions { get; } = [1, 2, 3, 4, 5, 6, 8, 10];

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
