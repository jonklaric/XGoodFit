using System.Windows;
using System.Windows.Controls;

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
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(SeparatorWithHeader), new UIPropertyMetadata(""));
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
        public static readonly DependencyProperty LeftSeparatorWidthProperty = DependencyProperty.Register(nameof(LeftSeparatorWidth), typeof(GridLength), typeof(SeparatorWithHeader), new UIPropertyMetadata(new GridLength(0.5d, GridUnitType.Star)));
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
        public static readonly DependencyProperty RightSeparatorWidthProperty = DependencyProperty.Register(nameof(RightSeparatorWidth), typeof(GridLength), typeof(SeparatorWithHeader), new UIPropertyMetadata(new GridLength(0.5d, GridUnitType.Star)));
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