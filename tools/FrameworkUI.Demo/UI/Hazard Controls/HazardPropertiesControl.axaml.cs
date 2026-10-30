// XGoodFit port: Avalonia's controls, bindings and colours, with WpfCompat's resource lookups, cursor
// and mouse events. The dependency properties are styled properties, the XAML's Preview handlers are
// added here, and the parameter grid's columns, which Avalonia's XAML can't name, are found by
// position. NumericControls' Parameter compiles from upstream's source until NumericControls is
// ported; its probability ordinates editor is deferred until then.
using FrameworkInterfaces;
using Numerics.Distributions;
using NumericControls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using WpfCompat;
using WpfCompat.Input;
using WpfCompat.Media;

namespace FrameworkUI.Demo.UI
{
    /// <summary>
    /// A WPF user control for editing parametric hazard function properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This control provides a property editor interface for configuring parametric hazard functions including:
    /// </para>
    /// <list type="bullet">
    ///     <item><description>Meta data (name, description, dates)</description></item>
    ///     <item><description>Hazard type and units selection</description></item>
    ///     <item><description>Parent distribution selection and parameter configuration</description></item>
    ///     <item><description>Uncertainty settings (record length, confidence intervals, realizations)</description></item>
    ///     <item><description>Probability ordinates table for frequency curve plotting</description></item>
    /// </list>
    /// </remarks>
    public partial class HazardPropertiesControl : UserControl
    {
        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="HazardPropertiesControl"/> class.
        /// </summary>
        public HazardPropertiesControl()
        {
            // This call is required by the designer.
            InitializeComponent();

            // Add any initialization after the InitializeComponent() call.
            PropertyAttributes.SetDefaultAttributes("Parametric Hazard Function Properties", _defaultDescription);

            _combobox.ItemsSource = DistributionOptions;
            // XGoodFit port: Avalonia's DisplayMemberBinding is WPF's DisplayMemberPath.
            _combobox.DisplayMemberBinding = new Binding("DisplayName");
            _combobox.HorizontalAlignment = HorizontalAlignment.Stretch;
            _combobox.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            Distribution.InnerContent = _combobox;

            // XGoodFit port: the XAML's PreviewMouseLeftButtonDown handlers.
            NameControl.AddPreviewMouseLeftButtonDownHandler(Name_PreviewMouseLeftButtonDown);
            Description.AddPreviewMouseLeftButtonDownHandler(Description_PreviewMouseLeftButtonDown);
            CreationDate.AddPreviewMouseLeftButtonDownHandler(CreationDate_PreviewMouseLeftButtonDown);
            LastModified.AddPreviewMouseLeftButtonDownHandler(LastModified_PreviewMouseLeftButtonDown);
            IsUncertain.AddPreviewMouseLeftButtonDownHandler(IsUncertain_PreviewMouseLeftButtonDown);
            EffectiveRecordLength.AddPreviewMouseLeftButtonDownHandler(EffectiveRecordLength_PreviewMouseLeftButtonDown);
            Distribution.AddPreviewMouseLeftButtonDownHandler(Distribution_PreviewMouseLeftButtonDown);
            ParametersTable.AddPreviewMouseLeftButtonDownHandler(ParameterDataGrid_PreviewMouseLeftButtonDown);
            EstimationMethod.AddPreviewMouseLeftButtonDownHandler(EstimationMethod_PreviewMouseLeftButtonDown);
            ConfidenceInterval.AddPreviewMouseLeftButtonDownHandler(ConfidenceInterval_PreviewMouseLeftButtonDown);
            Realizations.AddPreviewMouseLeftButtonDownHandler(Realizations_PreviewMouseLeftButtonDown);
            PRNGSeed.AddPreviewMouseLeftButtonDownHandler(PRNGSeed_PreviewMouseLeftButtonDown);
            General_TabItem.AddPreviewMouseLeftButtonDownHandler(TabItem_PreviewMouseLeftButtonDown);
            Options_TabItem.AddPreviewMouseLeftButtonDownHandler(TabItem_PreviewMouseLeftButtonDown);
        }

        // XGoodFit port: upstream's named grid columns, which Avalonia's XAML can't name.
        private DataGridTextColumn NameColumn => (DataGridTextColumn)ParametersTable.Columns[0];
        private DataGridTextColumn ValueColumn => (DataGridTextColumn)ParametersTable.Columns[1];

        #endregion

        #region Members

        private static readonly string _defaultDescription = "A parametric hazard function can be created using user-defined parameters, with or without uncertainty. When uncertainty is included, the parametric bootstrap method is used to quantify it.";
        private ComboBox _combobox = new ComboBox();
        private string _previousName = string.Empty;
        private ObservableCollection<Parameter> _parameterList = new ObservableCollection<Parameter>();
        private bool _syncingDistributionSelection;

        /// <summary>
        /// Identifies the <see cref="Element"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<HazardElement> ElementProperty =
            AvaloniaProperty.Register<HazardPropertiesControl, HazardElement>(nameof(Element));

        static HazardPropertiesControl()
        {
            // XGoodFit port: the property's change callback.
            ElementProperty.Changed.AddClassHandler<HazardPropertiesControl>((d, e) => ElementPropertyChanged(d, e));
        }

        /// <summary>
        /// Handles changes to the <see cref="Element"/> dependency property.
        /// </summary>
        /// <param name="d">The dependency object on which the property changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void ElementPropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(HazardPropertiesControl)) return;

            var thisControl = (HazardPropertiesControl)d;

            if (e.OldValue is HazardElement oldElement)
            {
                oldElement.PropertyChanged -= thisControl.Element_PropertyChanged;
            }

            thisControl._combobox.SelectionChanged -= thisControl.DistributionCombobox_SelectionChanged;
            thisControl.ClearParameterRows();

            if (e.NewValue is not HazardElement newElement)
            {
                thisControl.SyncDistributionSelection(null);
                thisControl.ParametersTable.ItemsSource = null;
                return;
            }

            thisControl.SyncDistributionSelection(newElement.ParentDistribution?.Type);
            newElement.PropertyChanged += thisControl.Element_PropertyChanged;
            thisControl._combobox.SelectionChanged += thisControl.DistributionCombobox_SelectionChanged;
            thisControl.PropertyAttributes.GetClassAttributes(newElement);
            thisControl.UpdateParameterDataGrid();
            thisControl.SetDataGridStyle();
        }

        /// <summary>
        /// Gets or sets the parametric hazard element bound to this control.
        /// </summary>
        /// <value>
        /// The <see cref="HazardElement"/> instance being edited.
        /// </value>
        public HazardElement Element
        {
            get => (HazardElement)GetValue(ElementProperty);
            set => SetValue(ElementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ExistingNames"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string[]> ExistingNamesProperty =
            AvaloniaProperty.Register<HazardPropertiesControl, string[]>(nameof(ExistingNames), new string[] { });

        /// <summary>
        /// Gets or sets the array of existing element names for validation purposes.
        /// </summary>
        /// <value>
        /// An array of strings representing names already in use by other elements.
        /// </value>
        public string[] ExistingNames
        {
            get => (string[])GetValue(ExistingNamesProperty);
            private set => SetValue(ExistingNamesProperty, value);
        }

        private static readonly DistributionOption[] _distributionOptions = new[] {
            CreateDistributionOption(UnivariateDistributionType.Exponential),
            CreateDistributionOption(UnivariateDistributionType.GammaDistribution),
            CreateDistributionOption(UnivariateDistributionType.GeneralizedExtremeValue),
            CreateDistributionOption(UnivariateDistributionType.GeneralizedLogistic),
            CreateDistributionOption(UnivariateDistributionType.GeneralizedNormal),
            CreateDistributionOption(UnivariateDistributionType.GeneralizedPareto),
            CreateDistributionOption(UnivariateDistributionType.Gumbel),
            CreateDistributionOption(UnivariateDistributionType.KappaFour),
            CreateDistributionOption(UnivariateDistributionType.LnNormal),
            CreateDistributionOption(UnivariateDistributionType.LogNormal),
            CreateDistributionOption(UnivariateDistributionType.LogPearsonTypeIII),
            CreateDistributionOption(UnivariateDistributionType.Normal),
            CreateDistributionOption(UnivariateDistributionType.PearsonTypeIII)
        };

        /// <summary>
        /// Gets the available parent distribution options.
        /// </summary>
        /// <value>
        /// An array of distribution option items representing the supported probability
        /// distributions for parametric hazard functions.
        /// </value>
        public DistributionOption[] DistributionOptions => _distributionOptions;

        /// <summary>
        /// Lightweight distribution choice used by the parent-distribution combo box.
        /// </summary>
        public sealed class DistributionOption
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DistributionOption"/> class.
            /// </summary>
            /// <param name="displayName">The display name shown in the combo box.</param>
            /// <param name="type">The distribution type to create when selected.</param>
            public DistributionOption(string displayName, UnivariateDistributionType type)
            {
                DisplayName = displayName;
                Type = type;
            }

            /// <summary>
            /// Gets the display name shown in the combo box.
            /// </summary>
            public string DisplayName { get; }

            /// <summary>
            /// Gets the distribution type represented by this option.
            /// </summary>
            public UnivariateDistributionType Type { get; }
        }

        /// <summary>Creates a selection option using the distribution factory's display name.</summary>
        /// <param name="type">The distribution type represented by the option.</param>
        /// <returns>The display name and type used by the distribution selector.</returns>
        private static DistributionOption CreateDistributionOption(UnivariateDistributionType type)
        {
            var distribution = UnivariateDistributionFactory.CreateDistribution(type);
            return new DistributionOption(distribution.DisplayName, type);
        }

        private static readonly ObservableCollection<ConfidenceIntervalItem> _confidenceIntervalWidthList = new ObservableCollection<ConfidenceIntervalItem>(new[]
        {
            new ConfidenceIntervalItem("90%", 0.9),
            new ConfidenceIntervalItem("95%", 0.95),
            new ConfidenceIntervalItem("98%", 0.98),
            new ConfidenceIntervalItem("99%", 0.99)
        });

        /// <summary>
        /// Gets the list of available confidence interval width options.
        /// </summary>
        /// <value>
        /// An observable collection of <see cref="ConfidenceIntervalItem"/> objects
        /// representing common confidence interval widths (90%, 95%, 98%, 99%).
        /// </value>
        public ObservableCollection<ConfidenceIntervalItem> ConfidenceIntervalWidthList => _confidenceIntervalWidthList;

        private static readonly ObservableCollection<EstimationMethod> _estimationMethodList = new ObservableCollection<EstimationMethod>(new[]
        {
            new EstimationMethod("Product Moments", ParameterEstimationMethod.MethodOfMoments),
            new EstimationMethod("Linear Moments", ParameterEstimationMethod.MethodOfLinearMoments),
            new EstimationMethod("Maximum Likelihood", ParameterEstimationMethod.MaximumLikelihood)
        });

        /// <summary>
        /// Gets the list of available parameter estimation methods.
        /// </summary>
        /// <value>
        /// An observable collection of <see cref="EstimationMethod"/> objects
        /// representing the supported estimation techniques (Product Moments, Linear Moments, Maximum Likelihood).
        /// </value>
        public ObservableCollection<EstimationMethod> EstimationMethodList => _estimationMethodList;

        /// <summary>
        /// Gets the list of Monte Carlo realization count options.
        /// </summary>
        /// <value>
        /// A list of doubles representing common realization counts for bootstrap analysis.
        /// </value>
        public List<double> RealizationList => new List<double> { 100, 500, 1000, 5000, 10000 };

        #endregion

        #region Property Attributes

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the Name control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void Name_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Element.Name), Element);
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the Description control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void Description_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Element.Description), Element);
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the CreationDate control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void CreationDate_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Element.CreationDate), Element);
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the LastModified control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void LastModified_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Element.LastModified), Element);
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the Distribution control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void Distribution_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Element.ParentDistribution), Element);
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the ParameterDataGrid control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void ParameterDataGrid_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.SetDefaultAttributes("Distribution Parameters", "The parameters of the parent probability distribution.");
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the IsUncertain control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void IsUncertain_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Element.IsUncertain), Element);
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the EffectiveRecordLength control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void EffectiveRecordLength_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Element.EffectiveRecordLength), Element);
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the EstimationMethod control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void EstimationMethod_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Element.EstimationMethod), Element);
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the ConfidenceInterval control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void ConfidenceInterval_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Element.ConfidenceIntervalWidth), Element);
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the Realizations control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void Realizations_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Element.Realizations), Element);
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the PRNGSeed control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void PRNGSeed_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Element.PRNGSeed), Element);
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the DataGrid control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void DataGrid_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.SetDefaultAttributes("Probability Ordinates", "The exceedance probabilities used for plotting the probability distribution.");
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the ProbabilityOrdinatesControl.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void ProbabilityOrdinatesControl_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.SetDefaultAttributes("Probability Ordinates", "The exceedance probabilities used for plotting the probability distribution.");
        }

        /// <summary>
        /// Handles the PreviewMouseLeftButtonDown event for the TabItem control to display property attributes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> instance containing the event data.</param>
        private void TabItem_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.SetDefaultAttributes("Parametric Hazard Function Properties", _defaultDescription);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the GotFocus event of the Name control to store the previous name for validation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Name_GotFocus(object sender, RoutedEventArgs e)
        {
            _previousName = Element.Name;
            ExistingNames = Element.ParentCollection.GetElementNames(Element).ToArray();
        }

        /// <summary>
        /// Handles the LostFocus event of the Name control to revert invalid names.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// If the name is not unique or otherwise invalid, the name is reverted to its previous value.
        /// </remarks>
        private void Name_LostFocus(object sender, RoutedEventArgs e)
        {
            if (NameControl.NameTextBox.IsValid) return;
            if (Element != null) Element.Name = _previousName;
        }

        #endregion

        #region Distribution Parameters

        /// <summary>
        /// Handles property changed events from the bound element to update the parameter grid.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the property name.</param>
        private void Element_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Element.ParentDistribution))
            {
                SyncDistributionSelection(Element?.ParentDistribution?.Type);
                UpdateParameterDataGrid();
                SetDataGridStyle();
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the distribution combobox to update the parent distribution.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void DistributionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingDistributionSelection || Element == null) return;
            if (_combobox.SelectedItem is DistributionOption option)
            {
                Element.ParentDistribution = UnivariateDistributionFactory.CreateDistribution(option.Type);
            }
        }

        /// <summary>
        /// Updates the parameter data grid with the current distribution's parameters.
        /// </summary>
        private void UpdateParameterDataGrid()
        {
            ClearParameterRows();
            if (Element?.ParentDistribution == null)
            {
                ParametersTable.ItemsSource = null;
                return;
            }

            var names = Element.ParentDistribution.GetParameterPropertyNames;
            var display = Element.ParentDistribution.ParametersToString;
            var parms = Element.ParentDistribution.GetParameters;

            for (int i = 0; i < parms.Length; i++)
            {
                var parm = new Parameter(names[i], display[i, 0], parms[i]);
                parm.PropertyChanged += ParameterChanged;
                _parameterList.Add(parm);
            }

            ParametersTable.ItemsSource = _parameterList;
        }

        /// <summary>
        /// Handles parameter value changes to update the distribution and validate parameters.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the property name.</param>
        private void ParameterChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Parameter.Value))
            {
                for (int i = 0; i < _parameterList.Count; i++)
                {
                    _parameterList[i].IsValid = true;
                    _parameterList[i].ErrorMessage = null;
                }

                if (Element == null) return;

                Element.SetDistributionParameters(_parameterList.Select(x => x.Value).ToArray());

                if (!Element.ParentDistribution.ParametersValid)
                {
                    var ex = Element.ParentDistribution.ValidateParameters(_parameterList.Select(x => x.Value).ToArray(), false);
                    SetInvalidParameter(ex.ParamName, ex.Message);
                }

                SetDataGridStyle();
            }
        }

        /// <summary>
        /// Sets a parameter as invalid with the specified error message.
        /// </summary>
        /// <param name="parameterName">The name of the invalid parameter.</param>
        /// <param name="errorMessage">The error message describing the validation failure.</param>
        private void SetInvalidParameter(string parameterName, string errorMessage)
        {
            var param = _parameterList.FirstOrDefault(x => string.Equals(x.Name, parameterName, StringComparison.OrdinalIgnoreCase));
            if (param == null) return;
            param.IsValid = false;
            param.ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Sets the data grid cell styles including validation error highlighting.
        /// </summary>
        private void SetDataGridStyle()
        {
            // XGoodFit port: the cell styles are control themes. The value cells' theme is based on
            // Right_CellStyle, where upstream copied its setters, and its data trigger on the parameter's
            // IsValid is setters bound to it: an invalid value's background, border and tooltip, and
            // no value of their own (so Right_CellStyle's) for a valid one.
            if (this.TryFindResource("Left_CellStyle") is ControlTheme leftStyle) NameColumn.CellTheme = leftStyle;
            if (this.TryFindResource("Right_CellStyle") is ControlTheme rightStyle) ValueColumn.CellTheme = rightStyle;
            if (ValueColumn.CellTheme == null) return;

            var style = new ControlTheme(typeof(DataGridCell)) { BasedOn = ValueColumn.CellTheme };
            if (style != null)
            {
                var invalidBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7B6AF"));
                var invalidBorder = new SolidColorBrush(Colors.Red);
                style.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new Binding("IsValid") { Converter = WhenInvalid(invalidBackground) }));
                style.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, new Binding("IsValid") { Converter = WhenInvalid(invalidBorder) }));
                style.Setters.Add(new Setter(ToolTip.TipProperty, new Binding("ErrorMessage")));
            }

            ValueColumn.CellTheme = style;
        }

        // XGoodFit port: the value of the data trigger's setter for an invalid parameter (IsValid false),
        // and no value for a valid one.
        private static FuncValueConverter<bool, object> WhenInvalid(object value) =>
            new(isValid => isValid ? AvaloniaProperty.UnsetValue : value);

        #endregion

        /// <summary>
        /// Handles the Click event of the SimulateButton to run the bootstrap estimation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void SimulateButton_Click(object sender, RoutedEventArgs e)
        {
            if (Element == null) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                Element.Estimate();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>Synchronizes the selected distribution without treating the update as a user edit.</summary>
        /// <param name="distributionType">The type to select, or null to clear the selection.</param>
        private void SyncDistributionSelection(UnivariateDistributionType? distributionType)
        {
            _syncingDistributionSelection = true;
            try
            {
                _combobox.SelectedItem = distributionType.HasValue
                    ? DistributionOptions.FirstOrDefault(x => x.Type == distributionType.Value)
                    : null;
            }
            finally
            {
                _syncingDistributionSelection = false;
            }
        }

        /// <summary>Detaches parameter change handlers before removing the displayed parameter rows.</summary>
        private void ClearParameterRows()
        {
            foreach (var parameter in _parameterList)
            {
                parameter.PropertyChanged -= ParameterChanged;
            }

            _parameterList.Clear();
        }
    }
}
