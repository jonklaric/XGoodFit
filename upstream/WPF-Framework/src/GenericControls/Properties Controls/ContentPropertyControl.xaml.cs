using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

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
    /// </remarks>
    public partial class ContentPropertyControl : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentPropertyControl"/> class.
        /// </summary>
        public ContentPropertyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Dependency property for the <see cref="InnerContent"/> property.
        /// </summary>
        public static readonly DependencyProperty InnerContentProperty = DependencyProperty.Register(nameof(InnerContent), typeof(object), typeof(ContentPropertyControl), new UIPropertyMetadata(null));
        /// <summary>
        /// Gets or sets the inner content displayed within the control.
        /// </summary>
        public object InnerContent
        {
            get
            {
                return this.GetValue(InnerContentProperty);
            }
            set
            {
                this.SetValue(InnerContentProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the <see cref="Title"/> property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ContentPropertyControl), new UIPropertyMetadata("Title"));
        /// <summary>
        /// Gets or sets the title displayed for the property control.
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
        /// Dependency property for the <see cref="MaxPropertyWidth"/> property.
        /// </summary>
        public static readonly DependencyProperty MaxPropertyWidthProperty = DependencyProperty.Register(nameof(MaxPropertyWidth), typeof(double), typeof(ContentPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMaxPropertyWidth));
        /// <summary>
        /// Gets or sets the maximum width of the property control.
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
        public static readonly DependencyProperty MinPropertyWidthProperty = DependencyProperty.Register(nameof(MinPropertyWidth), typeof(double), typeof(ContentPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMinPropertyWidth));
        /// <summary>
        /// Gets or sets the minimum width of the property control.
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
        /// Dependency property for the <see cref="PropertyWidth"/>
        /// </summary>
        public static readonly DependencyProperty PropertyWidthProperty = DependencyProperty.Register(nameof(PropertyWidth), typeof(GridLength), typeof(ContentPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));
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

        /// <summary>
        /// Dependency property for the <see cref="ShowLeaderLine"/> property.
        /// </summary>
        public static readonly DependencyProperty ShowLeaderLineProperty = DependencyProperty.Register(nameof(ShowLeaderLine), typeof(bool), typeof(ContentPropertyControl), new UIPropertyMetadata(true));
        /// <summary>
        /// Gets/sets a value indicating whether to display a visual leader line.
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
        public static readonly DependencyProperty PropertyHeightProperty = DependencyProperty.Register(nameof(PropertyHeight), typeof(double), typeof(ContentPropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyHeight));
        /// <summary>
        /// Gets/sets the height of the property row.
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

        private double _actualWidth = 0d;

        /// <summary>
        /// Gets the actual rendered width of the control.
        /// </summary>
        public double ActualPropertyWidth
        {
            get
            {
                return _actualWidth;
            }
            private set
            {
                if (_actualWidth != value)
                {
                    _actualWidth = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActualPropertyWidth)));
                }
            }
        }
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Updates the <see cref="ActualPropertyWidth"/> when the control's size changes.
        /// </summary>
        /// <param name="sender">The control raising the event.</param>
        /// <param name="e">The size change event arguments.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement el = sender as FrameworkElement;
            ActualPropertyWidth = el.ActualWidth;
        }
    }
}