using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace NumericControls
{
    /// <summary>
    /// A user control for editing XY paired data in a data grid format.
    /// Provides functionality for entering and managing two-dimensional data points.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class XYDataGridControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XYDataGridControl"/> class.
        /// </summary>
        public XYDataGridControl()
        {
            InitializeComponent();

            // Add sample data points for demonstration
            XYDataList.Add(new XYDataPoint(1d, 4d));
            XYDataList.Add(new XYDataPoint(2d, 5d));
            XYDataList.Add(new XYDataPoint(3d, 6d));
            XYDataTable.ItemsSource = XYDataList;
        }

        /// <summary>
        /// Gets the collection of XY data points displayed in the data grid.
        /// </summary>
        public ObservableCollection<object> XYDataList { get; } = new ObservableCollection<object>();
    }

    /// <summary>
    /// Represents a single XY data point with property change notification.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class XYDataPoint : INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the XYDataPoint class with default values.
        /// </summary>
        public XYDataPoint()
        {
        }

        /// <summary>
        /// Initializes a new instance of the XYDataPoint class with specified X and Y values.
        /// </summary>
        /// <param name="xValue">The X coordinate value.</param>
        /// <param name="yValue">The Y coordinate value.</param>
        public XYDataPoint(double xValue, double yValue)
        {
            X = xValue;
            Y = yValue;
        }

        private double _x;
        private double _y;

        /// <summary>
        /// Gets or sets the X coordinate value.
        /// </summary>
        public double X
        {
            get { return _x; }
            set
            {
                if (_x != value)
                {
                    _x = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the Y coordinate value.
        /// </summary>
        public double Y
        {
            get { return _y; }
            set
            {
                if (_y != value)
                {
                    _y = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y)));
                }
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
