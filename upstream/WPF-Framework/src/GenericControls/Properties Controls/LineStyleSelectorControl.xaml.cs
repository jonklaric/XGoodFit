using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace GenericControls
{
    /// <summary>
    /// A user control that allows the selection of predefined line styles (dash patterns).
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
    public partial class LineStyleSelectorControl:UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineStyleSelectorControl"/> class.
        /// </summary>
        public LineStyleSelectorControl()
        {
            InitializeComponent();
            if (SelectedDashArray == null)
            {
                SelectedDashArray = new DoubleCollection();
            }
        }

        /// <summary>
        /// Gets a list of predefined dash patterns used as line styles.
        /// </summary>
        public static List<DoubleCollection> LineStyleOptions { get; private set; } = new List<DoubleCollection>(new[] { new DoubleCollection(new[] { 0d }), new DoubleCollection(), new DoubleCollection(new[] { 4d, 1d }), new DoubleCollection(new[] { 1d, 1d }), new DoubleCollection(new[] { 4d, 1d, 1d, 1d }), new DoubleCollection(new[] { 4d, 1d, 4d, 1d, 1d, 1d }), new DoubleCollection(new[] { 4d, 1d, 1d, 1d, 1d, 1d }), new DoubleCollection(new[] { 4d, 1d, 4d, 1d, 1d, 1d, 1d, 1d }), new DoubleCollection(new[] { 10d, 1d }), new DoubleCollection(new[] { 10d, 1d, 1d, 1d }), new DoubleCollection(new[] { 10d, 1d, 1d, 1d, 1d, 1d }) });

        /// <summary>
        /// Identifies the <see cref="SelectedDashArray"/> dependency property.
        /// </summary>
        // Default null — reference-type DP defaults are shared across all instances.
        public static readonly DependencyProperty SelectedDashArrayProperty = DependencyProperty.Register(nameof(SelectedDashArray), typeof(DoubleCollection), typeof(LineStyleSelectorControl), new UIPropertyMetadata(null));
        /// <summary>
        /// Gets or sets the currently selected dash array pattern.
        /// </summary>
        public DoubleCollection SelectedDashArray
        {
            get
            {
                return (DoubleCollection)this.GetValue(SelectedDashArrayProperty);
            }
            set
            {
                this.SetValue(SelectedDashArrayProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="DashArrayOptions"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DashArrayOptionsProperty = DependencyProperty.Register(nameof(DashArrayOptions), typeof(IList<DoubleCollection>), typeof(LineStyleSelectorControl), new FrameworkPropertyMetadata(LineStyleOptions));
        /// <summary>
        /// Gets or sets the list of available dash array options.
        /// </summary>
        public IList<DoubleCollection> DashArrayOptions
        {
            get
            {
                return (IList<DoubleCollection>)this.GetValue(DashArrayOptionsProperty);
            }
            set
            {
                this.SetValue(DashArrayOptionsProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(LineStyleSelectorControl), new UIPropertyMetadata("Title"));
        /// <summary>
        /// Gets or sets the display title of the control.
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
        /// Identifies the <see cref="MaxPropertyWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxPropertyWidthProperty = DependencyProperty.Register(nameof(MaxPropertyWidth), typeof(double), typeof(LineStyleSelectorControl), new UIPropertyMetadata(PropertyDefaults.DefaultMaxPropertyWidth));
        /// <summary>
        /// Gets or sets the maximum width of the control's label section.
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
        /// Identifies the <see cref="MinPropertyWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinPropertyWidthProperty = DependencyProperty.Register(nameof(MinPropertyWidth), typeof(double), typeof(LineStyleSelectorControl), new UIPropertyMetadata(PropertyDefaults.DefaultMinPropertyWidth));
        /// <summary>
        /// Gets or sets the minimum width of the control's label section.
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
        /// Identifies the <see cref="PropertyWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PropertyWidthProperty = DependencyProperty.Register(nameof(PropertyWidth), typeof(GridLength), typeof(LineStyleSelectorControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));
        /// <summary>
        /// Gets or sets the width of the control's property column.
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
        /// Identifies the <see cref="ShowLeaderLine"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowLeaderLineProperty = DependencyProperty.Register(nameof(ShowLeaderLine), typeof(bool), typeof(LineStyleSelectorControl), new UIPropertyMetadata(true));
        /// <summary>
        /// Gets or sets a value indicating whether a leader line is shown next to the label.
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

    }

    /// <summary>
    /// Converts between <see cref="DoubleCollection"/> objects for dash array selection.
    /// Used to ensure consistent object references when binding to <see cref="LineStyleSelectorControl.LineStyleOptions"/>
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
    public class DoubleCollectionConverter : IValueConverter
    {
        /// <summary>
        /// Converts a <see cref="DoubleCollection"/> to a known reference in the predefined <see cref="LineStyleSelectorControl.LineStyleOptions"/> list.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">Optional parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The matching reference or blank; indicating no match.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var blank = LineStyleSelectorControl.LineStyleOptions[0];
            if (value == null)
                return blank;
            if (value.GetType() != typeof(DoubleCollection))
                return blank;
            // 
            DoubleCollection selectedStyle = (DoubleCollection)value;
            if (selectedStyle.Count == 0)
                return LineStyleSelectorControl.LineStyleOptions[1];
            foreach (var lineStyle in LineStyleSelectorControl.LineStyleOptions)
            {
                if (lineStyle is null)
                    continue;
                if (lineStyle.SequenceEqual(selectedStyle))
                    return lineStyle;
            }
            // 
            return blank;
        }

        /// <summary>
        /// Converts back from the selection to a new <see cref="DoubleCollection"/> object.
        /// </summary>
        /// <param name="value">The selected dash array.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">Optional parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A cloned <see cref="DoubleCollection"/> from the input, or a new one if invalid.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return new DoubleCollection();
            if (value.GetType() != typeof(DoubleCollection))
                return new DoubleCollection();
            return ((DoubleCollection)value).Clone();
        }
    }
}