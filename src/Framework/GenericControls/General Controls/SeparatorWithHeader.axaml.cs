using Avalonia;
using Avalonia.Controls;

namespace GenericControls
{
    /// <summary>
    /// A user control that displays a horizontal separator with a centered header text and
    /// adjustable widths for the left and right separator lines.
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
    /// XGoodFit port: the properties are Avalonia styled properties.
    /// </para>
    /// </remarks>
    public partial class SeparatorWithHeader:UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeparatorWithHeader"/> class.
        /// </summary>
        public SeparatorWithHeader()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Dependency property for the header text.
        /// </summary>
        public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<SeparatorWithHeader, string>(nameof(Header), "");
        /// <summary>
        /// gets/sets the header text displayed between between the left and right separators.
        /// </summary>
        public string Header
        {
            get
            {
                return (this.GetValue(HeaderProperty)?.ToString());
            }
            set
            {
                this.SetValue(HeaderProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the width of the left separator 
        /// </summary>
        public static readonly StyledProperty<GridLength> LeftSeparatorWidthProperty = AvaloniaProperty.Register<SeparatorWithHeader, GridLength>(nameof(LeftSeparatorWidth), new GridLength(0.5d, GridUnitType.Star));
        /// <summary>
        /// gets/sets the width of the left separator line.
        /// </summary>
        public GridLength LeftSeparatorWidth
        {
            get
            {
                return (GridLength)this.GetValue(LeftSeparatorWidthProperty);
            }
            set
            {
                this.SetValue(LeftSeparatorWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the width of the right separator
        /// </summary>
        public static readonly StyledProperty<GridLength> RightSeparatorWidthProperty = AvaloniaProperty.Register<SeparatorWithHeader, GridLength>(nameof(RightSeparatorWidth), new GridLength(0.5d, GridUnitType.Star));
        /// <summary>
        /// gets/sets the width of the right separator line.
        /// </summary>
        public GridLength RightSeparatorWidth
        {
            get
            {
                return (GridLength)this.GetValue(RightSeparatorWidthProperty);
            }
            set
            {
                this.SetValue(RightSeparatorWidthProperty, value);
            }
        }

    }
}