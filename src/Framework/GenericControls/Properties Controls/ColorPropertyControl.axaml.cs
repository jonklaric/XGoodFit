using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace GenericControls
{
    /// <summary>
    /// A user control for displaying and selecting a color value, with configurable layout properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// XGoodFit port: the properties are Avalonia styled properties; the colour is Avalonia's mutable
    /// <see cref="SolidColorBrush"/>, as upstream's is WPF's.
    /// </para>
    /// </remarks>
    public partial class ColorPropertyControl:UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPropertyControl"/> class.
        /// </summary>
        public ColorPropertyControl()
        {
            InitializeComponent();
            // Per-instance default (DP default is null — see DP registration).
            if (SelectedColor == null)
            {
                SelectedColor = new SolidColorBrush(Colors.Black);
            }
        }

        /// <summary>
        /// Dependency property for the <see cref="SelectedColor"/> property.
        /// </summary>
        // Default null — reference-type DP defaults are shared across all instances.
        // Per-instance default initialized in the constructor.
        public static readonly StyledProperty<SolidColorBrush> SelectedColorProperty = AvaloniaProperty.Register<ColorPropertyControl, SolidColorBrush>(nameof(SelectedColor), null);
        /// <summary>
        /// Gets or sets the selected color represented as a <see cref="SolidColorBrush"/>.
        /// </summary>
        public SolidColorBrush SelectedColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(SelectedColorProperty);
            }
            set
            {
                this.SetValue(SelectedColorProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the <see cref="Title"/> property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<ColorPropertyControl, string>(nameof(Title), "Title");
        /// <summary>
        /// Gets or sets the title displayed for the color property.
        /// </summary>
        public string Title
        {
            get
            {
                return (string)this.GetValue(TitleProperty);
            }
            set
            {
                this.SetValue(TitleProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the <see cref="ShowLeaderLine"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowLeaderLineProperty = AvaloniaProperty.Register<ColorPropertyControl, bool>(nameof(ShowLeaderLine), true);
        /// <summary>
        /// Gets or sets a value indicating whether to show a visual leader line in the UI.
        /// </summary>
        public bool ShowLeaderLine
        {
            get
            {
                return (bool)this.GetValue(ShowLeaderLineProperty);
            }
            set
            {
                this.SetValue(ShowLeaderLineProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the <see cref="PropertyHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> PropertyHeightProperty = AvaloniaProperty.Register<ColorPropertyControl, double>(nameof(PropertyHeight), PropertyDefaults.DefaultPropertyHeight);
        /// <summary>
        /// Gets or sets the height of the property row in the layout.
        /// </summary>
        public double PropertyHeight
        {
            get
            {
                return (double)this.GetValue(PropertyHeightProperty);
            }
            set
            {
                this.SetValue(PropertyHeightProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the <see cref="MaxPropertyWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxPropertyWidthProperty = AvaloniaProperty.Register<ColorPropertyControl, double>(nameof(MaxPropertyWidth), PropertyDefaults.DefaultMaxPropertyWidth);
        /// <summary>
        /// Gets or sets the maximum allowed width of the property.
        /// </summary>
        public double MaxPropertyWidth
        {
            get
            {
                return (double)this.GetValue(MaxPropertyWidthProperty);
            }
            set
            {
                this.SetValue(MaxPropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the <see cref="MinPropertyWidth"/> property.
        /// </summary> 
        public static readonly StyledProperty<double> MinPropertyWidthProperty = AvaloniaProperty.Register<ColorPropertyControl, double>(nameof(MinPropertyWidth), PropertyDefaults.DefaultMinPropertyWidth);
        /// <summary>
        /// Gets or sets the minimum allowed width of the property.
        /// </summary>
        public double MinPropertyWidth
        {
            get
            {
                return (double)this.GetValue(MinPropertyWidthProperty);
            }
            set
            {
                this.SetValue(MinPropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the <see cref="PropertyWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<GridLength> PropertyWidthProperty = AvaloniaProperty.Register<ColorPropertyControl, GridLength>(nameof(PropertyWidth), new GridLength(36d));
        /// <summary>
        /// Gets or sets the current width of the property layout column.
        /// </summary>
        public GridLength PropertyWidth
        {
            get
            {
                return (GridLength)this.GetValue(PropertyWidthProperty);
            }
            set
            {
                this.SetValue(PropertyWidthProperty, value);
            }
        }
    }
}