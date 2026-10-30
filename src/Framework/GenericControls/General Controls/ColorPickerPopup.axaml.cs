using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WpfCompat;

namespace GenericControls
{
    /// <summary>
    /// A user control that provides a popup for selecting a color.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
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
    /// XGoodFit port: the properties are Avalonia styled properties. The colour's default is a black
    /// <see cref="SolidColorBrush"/> (Avalonia's <see cref="Brushes.Black"/> is an immutable brush), and the
    /// arrow's visibility is WpfCompat's <see cref="Visibility"/>.
    /// </para>
    /// </remarks>
    public partial class ColorPickerPopup:UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPickerPopup"/> class.
        /// </summary>
        public ColorPickerPopup()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Dependency property for the selected <see cref="Color"/>
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> ColorProperty = AvaloniaProperty.Register<ColorPickerPopup, SolidColorBrush>(nameof(Color), new SolidColorBrush(Colors.Black)); 
        /// <summary>
        /// Gets/sets the selected color shown in the color picker popup.
        /// </summary>
        public SolidColorBrush Color
        {
            get
            {
                return (SolidColorBrush)this.GetValue(ColorProperty);
            }
            set
            {
                this.SetValue(ColorProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for controlling the visibility of the arrow button in the popup.
        /// </summary>
        public static readonly StyledProperty<Visibility> ArrowButtonVisibilityProperty = AvaloniaProperty.Register<ColorPickerPopup, Visibility>(nameof(ArrowButtonVisibility), Visibility.Visible);
        /// <summary>
        /// Gets/sets the visibility of the arrow button in the popup interface.
        /// </summary>
        public Visibility ArrowButtonVisibility
        {
            get
            {
                return (Visibility)this.GetValue(ArrowButtonVisibilityProperty);
            }
            set
            {
                this.SetValue(ArrowButtonVisibilityProperty, value);
            }
        }
    }
}