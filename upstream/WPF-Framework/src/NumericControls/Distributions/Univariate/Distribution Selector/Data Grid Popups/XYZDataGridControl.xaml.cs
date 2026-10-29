using System.Windows.Controls;

namespace NumericControls
{
    /// <summary>
    /// A user control for editing XYZ trivariate data in a data grid format.
    /// Provides functionality for entering and managing three-dimensional data points.
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
    public partial class XYZDataGridControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the XYZDataGridControl class.
        /// </summary>
        public XYZDataGridControl()
        {
            InitializeComponent();
        }
    }
}
