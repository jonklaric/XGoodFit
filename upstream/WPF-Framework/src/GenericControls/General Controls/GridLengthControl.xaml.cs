using System.Windows;
using System.Windows.Controls;

namespace GenericControls
{
    /// <summary>
    /// A user control that allows editing of a <see cref="GridLength"/>, including both value and unit type (Auto, Pixel, Star).
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
    public partial class GridLengthControl:UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridLengthControl"/> class.
        /// </summary>
        public GridLengthControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Provides a list of available <see cref="GridUnitType"/> options for use in UI bindings.
        /// </summary>
        public static List<GridUnitType> GridLengthUnitOptions { get; private set; } = new List<GridUnitType>((GridUnitType[])Enum.GetValues(typeof(GridUnitType)));

        /// <summary>
        /// Identifies the <see cref="GridLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GridLengthProperty = DependencyProperty.Register(nameof(GridLength), typeof(GridLength), typeof(GridLengthControl), new FrameworkPropertyMetadata(GridLength.Auto, GridLengthPropertyCallback));
        /// <summary>
        /// Callback when <see cref="GridLength"/> is changed. Updates individual components.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void GridLengthPropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(GridLengthControl))
                return;
            GridLengthControl thisControl = (GridLengthControl)d;
            // 
            if (e.NewValue == null)
                return;
            if (e.NewValue.GetType() != typeof(GridLength))
                return;
            GridLength newGridLength = (GridLength)e.NewValue;
            if (thisControl.GridLengthValue != newGridLength.Value)
                thisControl.GridLengthValue = newGridLength.Value;
            if (thisControl.GridLengthUnit != newGridLength.GridUnitType)
                thisControl.GridLengthUnit = newGridLength.GridUnitType;
            // End If
        }

        /// <summary>
        /// Gets/sets the combined <see cref="GridLength"/> (value + unit).
        /// </summary>
        public GridLength GridLength
        {
            get
            {
                return (GridLength)this.GetValue(GridLengthProperty);
            }
            set
            {
                this.SetValue(GridLengthProperty, value);
            }
        }
        /// <summary>
        /// Identifies the <see cref="GridLengthValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GridLengthValueProperty = DependencyProperty.Register(nameof(GridLengthValue), typeof(double), typeof(GridLengthControl), new FrameworkPropertyMetadata(100d, GridLengthValuePropertyCallback));
        /// <summary>
        /// Callback when <see cref="GridLengthValue"/> is changed. Triggers update to <see cref="GridLength"/>
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void GridLengthValuePropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(GridLengthControl))
                return;
            GridLengthControl thisControl = (GridLengthControl)d;
            // 
            thisControl.UpdateGridLengthProperty();
        }
        /// <summary>
        /// gets/sets the numeric value of the <see cref="GridLength"/>
        /// </summary>
        public double GridLengthValue
        {
            get
            {
                return (double)this.GetValue(GridLengthValueProperty);
            }
            set
            {
                this.SetValue(GridLengthValueProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="GridLengthUnit"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GridLengthUnitProperty = DependencyProperty.Register(nameof(GridLengthUnit), typeof(GridUnitType), typeof(GridLengthControl), new FrameworkPropertyMetadata(GridUnitType.Auto, GridLengthUnitPropertyCallback));
        /// <summary>
        /// Callback when <see cref="GridLengthUnit"/> is changed. Triggers update to <see cref="GridLength"/>
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void GridLengthUnitPropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(GridLengthControl))
                return;
            GridLengthControl thisControl = (GridLengthControl)d;
            // 
            thisControl.UpdateGridLengthProperty();
        }

        /// <summary>
        /// Gets/sets the unit type of the <see cref="GridLength"/> (e.g., Auto, Pixel, Star).
        /// </summary>
        public GridUnitType GridLengthUnit
        {
            get
            {
                return (GridUnitType)(int)this.GetValue(GridLengthUnitProperty);
            }
            set
            {
                this.SetValue(GridLengthUnitProperty, value);
            }
        }
        /// <summary>
        /// Updates the combined <see cref="GridLength"/> property when the value or unit changes. 
        /// </summary>
        private void UpdateGridLengthProperty()
        {
            // Refresh the gridlength
            GridLength = new GridLength(GridLengthValue, GridLengthUnit);
        }

    }
}