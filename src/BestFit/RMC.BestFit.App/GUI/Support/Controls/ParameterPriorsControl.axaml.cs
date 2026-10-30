// XGoodFit port of upstream's GUI/Support/Controls/ParameterPriorsControl.xaml.cs: the dependency properties are
// Avalonia styled properties with upstream's callback (readonly, as Avalonia requires), the XAML's
// PreviewMouseLeftButtonDown handlers are added in the constructor (WpfCompat's mouse events), and the header styles are ControlThemes. Avalonia's
// XAML can't name grid columns: upstream's named columns are found by position.
using NumericControls.Distributions.Univariate;
using Numerics.Distributions;
using RMC.BestFit.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using WpfCompat;
using WpfCompat.Input;

namespace RMC_BestFit
{
    /// <summary>
    /// User control for configuring and managing prior probability distributions for model parameters in Bayesian analysis.
    /// Provides an interactive interface for selecting and editing prior distributions for each parameter.
    /// </summary>
    public partial class ParameterPriorsControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterPriorsControl"/> class.
        /// </summary>
        public ParameterPriorsControl()
        {
            InitializeComponent();

            // XGoodFit port: upstream's XAML PreviewMouseLeftButtonDown handlers (Avalonia has no Preview events in XAML).
            PriorDistributionsDataGrid.AddPreviewMouseLeftButtonDownHandler(PriorDistributionsDataGrid_PreviewMouseLeftButtonDown);
            UsePriorDistributionDefaults.AddPreviewMouseLeftButtonDownHandler(UsePriorDistributionDefaults_PreviewMouseLeftButtonDown);
        }

        /// <summary>
        /// Raised when the user requests to view property attributes for a model property.
        /// </summary>
        public event ShowPropertyAttributesEventHandler ShowPropertyAttributes;

        /// <summary>
        /// Delegate for the <see cref="ShowPropertyAttributes"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property whose attributes are requested.</param>
        /// <param name="classObject">The object instance containing the property.</param>
        public delegate void ShowPropertyAttributesEventHandler(string propertyName, object classObject);

        /// <summary>
        /// Dependency property for the <see cref="Model"/> property.
        /// </summary>
        public static readonly StyledProperty<IModel> ModelProperty = AvaloniaProperty.Register<ParameterPriorsControl, IModel>(nameof(Model), null);

        /// <summary>
        /// Gets or sets the statistical model whose parameter priors are being configured.
        /// </summary>
        public IModel Model
        {
            get { return (IModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        static ParameterPriorsControl()
        {
            // XGoodFit port: the property-changed callbacks of upstream's metadata.
            ModelProperty.Changed.AddClassHandler<ParameterPriorsControl>(ModelCallback);
        }

        /// <summary>
        /// Callback method invoked when the Model dependency property changes.
        /// Subscribes to property changed events on the new model and updates the data grid.
        /// </summary>
        /// <param name="d">The dependency object that owns the property.</param>
        /// <param name="e">Event data containing the old and new property values.</param>
        private static void ModelCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d as ParameterPriorsControl == null) return;
            var thisControl = (ParameterPriorsControl)d;

            // Unsubscribe from old model to prevent memory leaks and stale event handlers
            if (e.OldValue is IModel oldModel)
                oldModel.PropertyChanged -= thisControl.Model_PropertyChanged;

            if (e.NewValue == null) return;
            var newElement = e.NewValue as IModel;
            if (newElement == null) return;

            newElement.PropertyChanged += thisControl.Model_PropertyChanged;
            thisControl.UpdateDataGrid();
        }

        /// <summary>
        /// Event handler for property changes in the Model object. Updates the control when model parameters change.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data containing the property name that changed.</param>
        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Model.Parameters) || e.PropertyName == nameof(Model.SetDefaultParameters))
            {
                UpdateDataGrid();
            }
        }

        /// <summary>
        /// Event handler for mouse down events on the "Use Prior Distribution Defaults" control.
        /// Raises the ShowPropertyAttributes event to display related property information.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data containing mouse button information.</param>
        private void UsePriorDistributionDefaults_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            ShowPropertyAttributes?.Invoke(nameof(Model.UseDefaultFlatPriors), Model);
        }

        /// <summary>
        /// Event handler for mouse down events on the prior distributions data grid.
        /// Raises the ShowPropertyAttributes event to display parameter information.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data containing mouse button information.</param>
        private void PriorDistributionsDataGrid_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            ShowPropertyAttributes?.Invoke(nameof(Model.Parameters), Model);
        }

        /// <summary>
        /// Event handler for when the distribution selector popup is opened.
        /// Initializes the distribution selector with available prior distributions and the current parameter's distribution.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data.</param>
        private void DistributionPopup_Opened(object sender, EventArgs e)
        {
            if (Model == null) return;
            Popup p = (Popup)sender;
            if (p.Child == null) return;
            var b = (Border)p.Child;
            DistributionSelectorControl dsc = (DistributionSelectorControl)b.Child;
            UnivariateDistributionBase dist = null;
            if(p.DataContext.GetType() == typeof(ModelParameter))
            {
                dist = ((ModelParameter)p.DataContext).PriorDistribution.Clone();
            }

            var PriorDistributionsList  = new List<UnivariateDistributionBase>()
            {
              new Exponential(),
              new GammaDistribution(),
              new GeneralizedBeta(),
              new LnNormal(),
              new LogNormal(),
              new Normal(),
              new Pert(),
              new StudentT(),
              new Triangular(),
              new TruncatedNormal(),
              new Uniform()
            };

            dsc.Distributions = PriorDistributionsList;
            dsc.SelectedDistribution = dist;

        }

        /// <summary>
        /// Event handler for when the distribution selector popup is closed.
        /// Applies the selected distribution to the parameter if valid and updates the data grid.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data.</param>
        private void DistributionPopup_Closed(object sender, EventArgs e)
        {
            if (Model == null) return;
            Popup p = (Popup)sender;
            if (p.Child == null) return;
            var b = (Border)p.Child;
            DistributionSelectorControl dsc = (DistributionSelectorControl)b.Child;
            if (p.DataContext.GetType() == typeof(ModelParameter) && dsc.SelectedDistribution.ParametersValid == true)
            {
                ((ModelParameter)p.DataContext).PriorDistribution = dsc.SelectedDistribution.Clone();
            }
            dsc.SelectedDistribution = null;
            UpdateDataGrid();
        }

        /// <summary>
        /// Updates the data grid with the current model parameters and their prior distributions.
        /// Configures column headers with tooltips and refreshes the display.
        /// </summary>
        private void UpdateDataGrid()
        {
            PriorDistributionsDataGrid.ItemsSource = null;
            if (Model == null || Model.Parameters == null) return;

            PriorDistributionsDataGrid.ItemsSource = Model.Parameters;

            // Parameter Column Header
            // XGoodFit port: styles are ControlThemes, whose BasedOn is WPF's basedOn (see HeaderThemeOf). A
            // setter can't hold a control, as WPF's can: the tool tip's text block is a template.
            var style = new ControlTheme(typeof(DataGridColumnHeader)) { BasedOn = HeaderThemeOf(ParameterNameColumn, "Left_ColumnHeaderStyle") };
            style.Setters.Add(new Setter(ToolTip.TipProperty, new FuncTemplate<TextBlock>(() => new TextBlock() { Text = "The model parameter names.", FontWeight = FontWeight.Normal, TextAlignment = TextAlignment.Left, TextWrapping = TextWrapping.Wrap })));
            ParameterNameColumn.HeaderTheme = style;

            // Distribution Column Header
            // XGoodFit port: styles are ControlThemes, whose BasedOn is WPF's basedOn (see HeaderThemeOf). A
            // setter can't hold a control, as WPF's can: the tool tip's text block is a template.
            style = new ControlTheme(typeof(DataGridColumnHeader)) { BasedOn = HeaderThemeOf(ParameterDistributionColumn, "Center_ColumnHeaderStyle") };
            style.Setters.Add(new Setter(ToolTip.TipProperty, new FuncTemplate<TextBlock>(() => new TextBlock() { Text = "Set the prior distributions of the model parameters.", FontWeight = FontWeight.Normal, TextAlignment = TextAlignment.Left, TextWrapping = TextWrapping.Wrap })));
            ParameterDistributionColumn.HeaderTheme = style;

            PriorDistributionsDataGrid.Items.Refresh();

        }

        /// <summary>
        /// XGoodFit port: upstream's ParameterNameColumn, the grid's first column (Avalonia's XAML can't name a column).
        /// </summary>
        private DataGridColumn ParameterNameColumn => PriorDistributionsDataGrid.Columns[0];

        /// <summary>
        /// XGoodFit port: upstream's ParameterDistributionColumn, the grid's second column (Avalonia's XAML can't name a column).
        /// </summary>
        private DataGridColumn ParameterDistributionColumn => PriorDistributionsDataGrid.Columns[1];

        /// <summary>
        /// XGoodFit port: the header theme a column's XAML gives it, as WPF finds it.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="key">The resource its XAML names.</param>
        /// <returns>The theme.</returns>
        /// <remarks>
        /// WPF's DynamicResource reference finds the application's resource at once, and a local value
        /// replaces it. Avalonia's is a binding, which reaches the application's resources only once the grid
        /// is in a window and which a local value doesn't clear: the next resource change would overwrite the
        /// tool tip's theme. So the theme is the application's while the binding hasn't found it, and the
        /// binding goes.
        /// </remarks>
        private static ControlTheme HeaderThemeOf(DataGridColumn column, string key)
        {
            var theme = column.HeaderTheme ?? Application.Current?.TryFindResource(key) as ControlTheme;
            BindingOperations.GetBindingExpressionBase(column, DataGridColumn.HeaderThemeProperty)?.Dispose();
            return theme;
        }

    }
}
