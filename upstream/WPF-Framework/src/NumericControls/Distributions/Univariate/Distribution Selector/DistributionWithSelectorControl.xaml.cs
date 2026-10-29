using Numerics.Distributions;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NumericControls.Distributions.Univariate
{
    /// <summary>
    /// A user control that combines a distribution text display with an expandable selector popup.
    /// Allows users to view the current distribution and click to change it via a popup editor.
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
    public partial class DistributionWithSelectorControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DistributionWithSelectorControl"/> class.
        /// </summary>
        public DistributionWithSelectorControl()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Dependency property for the selected distribution.
        /// </summary>
        public static readonly DependencyProperty SelectedDistributionProperty = DependencyProperty.Register(nameof(SelectedDistribution), typeof(UnivariateDistributionBase), typeof(DistributionWithSelectorControl), new PropertyMetadata(null, SetDistribution));
        /// <summary>
        /// Get and set the selected probability distribution.
        /// </summary>
        public UnivariateDistributionBase SelectedDistribution
        {
            get { return (UnivariateDistributionBase)GetValue(SelectedDistributionProperty); }
            set { SetValue(SelectedDistributionProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowPlot"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowPlotProperty = DependencyProperty.Register(nameof(ShowPlot), typeof(bool), typeof(DistributionWithSelectorControl), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the PDF plot is visible in the popup.
        /// </summary>
        /// <value><c>true</c> to show the PDF plot; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool ShowPlot
        {
            get { return (bool)GetValue(ShowPlotProperty); }
            set { SetValue(ShowPlotProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowStatistics"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStatisticsProperty = DependencyProperty.Register(nameof(ShowStatistics), typeof(bool), typeof(DistributionWithSelectorControl), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the summary statistics are visible in the popup.
        /// </summary>
        /// <value><c>true</c> to show summary statistics; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool ShowStatistics
        {
            get { return (bool)GetValue(ShowStatisticsProperty); }
            set { SetValue(ShowStatisticsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Distributions"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DistributionsProperty = DependencyProperty.Register(nameof(Distributions), typeof(IList<UnivariateDistributionBase>), typeof(DistributionWithSelectorControl), new PropertyMetadata(DistributionSelectorControl.DefaultDistributions));

        /// <summary>
        /// Gets or sets the list of available distribution types for selection.
        /// </summary>
        /// <value>
        /// A list of <see cref="UnivariateDistributionBase"/> instances representing the available distribution types.
        /// </value>
        public IList<UnivariateDistributionBase> Distributions
        {
            get { return (IList<UnivariateDistributionBase>)GetValue(DistributionsProperty); }
            set { SetValue(DistributionsProperty, value); }
        }

        /// <summary>
        /// When the dependency property changes, this sets the distribution.
        /// </summary>
        private static void SetDistribution(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(DistributionWithSelectorControl)) return;
            DistributionWithSelectorControl thisControl = (DistributionWithSelectorControl)d;
            // 
            if (e.NewValue == null)
            {
                thisControl.DistributionTextBlock.Text = "No Distribution Selected"; // .DistributionCombobox.SelectedIndex = -1
            }
            else
            {
                UnivariateDistributionBase newDistribution = e.NewValue as UnivariateDistributionBase;
                if ((newDistribution == null) || (thisControl.Distributions.FirstOrDefault(o => o.Type == newDistribution.Type) == null))
                {
                    // If this is reached that means the distribution doesn't exist in the distribution options
                    thisControl.DistributionTextBlock.Text = "No Distribution Selected";
                }
                else
                {
                    // Set the distribution textblock text.
                    var sb = new StringBuilder(newDistribution.DisplayName + "(");
                    var shortNames = newDistribution.ParameterNamesShortForm;
                    var paramVals = newDistribution.GetParameters;
                    for (int i = 0; i < shortNames.Count(); i++)
                    {
                        sb.Append(shortNames[i]);
                        sb.Append("=");
                        sb.Append(string.Format(CultureInfo.CurrentCulture, "{0:0.#####}", paramVals[i]));
                        if (i != shortNames.Count() - 1)
                            sb.Append(", ");
                    }
                    // 
                    sb.Append(")");
                    thisControl.DistributionTextBlock.Text = sb.ToString();
                }
            }
        }

        /// <summary>
        /// When the mouse enters the datagrid set the popup to staysopen=true to allow the datagrid to keep capture of the mouse and give the datagrid the focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            DistributionPopup.StaysOpen = true;
            SelectorControl.Focus();
        }

        /// <summary>
        /// When the mouse leaves the control set the popup to staysopen=false and give it focus so that clicking outside the popup will close it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            DistributionPopup.StaysOpen = false;
            DistributionPopup.Focus();
        }

        /// <summary>
        /// When a context menu closes the focus gets all out of whack in the popup and needs to be reset. Maybe because it is a popup on a popup? WPF Inception
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            DistributionPopup.IsOpen = false;
            DistributionPopup.IsOpen = true;
        }
    }
}
