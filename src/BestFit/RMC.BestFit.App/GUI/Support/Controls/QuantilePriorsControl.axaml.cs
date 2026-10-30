// XGoodFit port of upstream's GUI/Support/Controls/QuantilePriorsControl.xaml.cs: the dependency properties are
// Avalonia styled properties with upstream's callback (readonly, as Avalonia requires), the XAML's
// PreviewMouseLeftButtonDown handlers are added in the constructor (WpfCompat's mouse events), and the header styles are ControlThemes. Avalonia's
// XAML can't name grid columns: upstream's named columns are found by position.
using NumericControls.Distributions.Univariate;
using Numerics.Distributions;
using RMC.BestFit.Models;
using System;
using System.Collections.Generic;
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
    /// A user control that provides an interactive interface for configuring quantile priors in distribution fitting.
    /// This control allows users to select quantiles, assign prior distributions to those quantiles, and configure
    /// related settings for Bayesian analysis.
    /// </summary>
    public partial class QuantilePriorsControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuantilePriorsControl"/> class.
        /// </summary>
        public QuantilePriorsControl()
        {
            InitializeComponent();

            // XGoodFit port: upstream's XAML PreviewMouseLeftButtonDown handlers (Avalonia has no Preview events in XAML).
            PriorDistributionsDataGrid.AddPreviewMouseLeftButtonDownHandler(PriorDistributionsDataGrid_PreviewMouseLeftButtonDown);
            EnablePriorQuantileDistributions.AddPreviewMouseLeftButtonDownHandler(EnablePriorQuantileDistributions_PreviewMouseLeftButtonDown);
            UseSingleQuantile.AddPreviewMouseLeftButtonDownHandler(UseSingleQuantile_PreviewMouseLeftButtonDown);
        }

        /// <summary>
        /// Occurs when a property's attributes should be displayed to the user.
        /// This event is raised when the user requests additional information about a specific property.
        /// </summary>
        public event ShowPropertyAttributesEventHandler ShowPropertyAttributes;

        /// <summary>
        /// Represents the method that will handle the <see cref="ShowPropertyAttributes"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property whose attributes should be displayed.</param>
        /// <param name="classObject">The object instance containing the property.</param>
        public delegate void ShowPropertyAttributesEventHandler(string propertyName, object classObject);

        /// <summary>
        /// Identifies the <see cref="ShowSingleQuantileCheckBox"/> dependency property.
        /// This property controls the visibility of the single quantile checkbox in the UI.
        /// </summary>
        public static readonly StyledProperty<bool> ShowSingleQuantileCheckBoxProperty = AvaloniaProperty.Register<QuantilePriorsControl, bool>(nameof(ShowSingleQuantileCheckBox), true);

        /// <summary>
        /// Gets or sets a value indicating whether the single quantile checkbox should be displayed.
        /// </summary>
        /// <value><c>true</c> if the single quantile checkbox should be shown; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool ShowSingleQuantileCheckBox
        {
            get { return (bool)GetValue(ShowSingleQuantileCheckBoxProperty); }
            set { SetValue(ShowSingleQuantileCheckBoxProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Model"/> dependency property.
        /// This property represents the data model containing quantile prior configuration.
        /// </summary>
        public static readonly StyledProperty<IQuantilePriors> ModelProperty = AvaloniaProperty.Register<QuantilePriorsControl, IQuantilePriors>(nameof(Model), null);

        /// <summary>
        /// Gets or sets the quantile priors model that provides the data for this control.
        /// </summary>
        /// <value>An <see cref="IQuantilePriors"/> instance containing the quantile prior configuration.</value>
        public IQuantilePriors Model
        {
            get { return (IQuantilePriors)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        static QuantilePriorsControl()
        {
            // XGoodFit port: the property-changed callbacks of upstream's metadata.
            ModelProperty.Changed.AddClassHandler<QuantilePriorsControl>(ModelCallback);
        }

        /// <summary>
        /// Callback method invoked when the <see cref="Model"/> property changes.
        /// Subscribes to property change events and updates the data grid with the new model data.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void ModelCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d as QuantilePriorsControl == null) return;
            var thisControl = (QuantilePriorsControl)d;

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
        /// Gets a list of predefined quantile options available for selection.
        /// These values represent common exceedance probabilities used in statistical analysis.
        /// </summary>
        /// <value>A list of double values ranging from 0.1 (10%) to 0.000001 (0.0001%).</value>
        public List<double> QuantileOptions => new List<double>() { 0.1, 0.05, 0.02, 0.01, 0.005, 0.002, 0.001, 0.0005, 0.0002, 0.0001, 0.00005, 0.00002, 0.00001, 0.000005, 0.000002, 0.000001 };

        /// <summary>
        /// Handles property change events for the model.
        /// Updates the data grid when the quantile priors collection changes.
        /// </summary>
        /// <param name="sender">The model that raised the property changed event.</param>
        /// <param name="e">Event arguments containing the name of the property that changed.</param>
        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Model.QuantilePriors) || e.PropertyName == nameof(Model.SetDefaultQuantilePriors))
            {
                UpdateDataGrid();
            }
        }

        /// <summary>
        /// Handles the opened event for the distribution selection popup.
        /// Initializes the distribution selector with available prior distributions and sets the current selection.
        /// </summary>
        /// <param name="sender">The popup control that was opened.</param>
        /// <param name="e">Event arguments for the popup opened event.</param>
        private void DistributionPopup_Opened(object sender, EventArgs e)
        {
            if (Model == null) return;
            Popup p = (Popup)sender;
            if (p.Child == null) return;
            var b = (Border)p.Child;
            DistributionSelectorControl dsc = (DistributionSelectorControl)b.Child;
            UnivariateDistributionBase dist = null;
            if (p.DataContext.GetType() == typeof(QuantilePrior))
            {
                dist = ((QuantilePrior)p.DataContext).Distribution.Clone();
            }

            var PriorDistributionsList = new List<UnivariateDistributionBase>()
            { 
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
        /// Handles the closed event for the distribution selection popup.
        /// Saves the selected distribution to the model if valid parameters were provided and refreshes the data grid.
        /// </summary>
        /// <param name="sender">The popup control that was closed.</param>
        /// <param name="e">Event arguments for the popup closed event.</param>
        private void DistributionPopup_Closed(object sender, EventArgs e)
        {
            if (Model == null) return;
            Popup p = (Popup)sender;
            if (p.Child == null) return;
            var b = (Border)p.Child;
            DistributionSelectorControl dsc = (DistributionSelectorControl)b.Child;
            if (p.DataContext.GetType() == typeof(QuantilePrior) && dsc.SelectedDistribution.ParametersValid == true)
            {
                ((QuantilePrior)p.DataContext).Distribution = dsc.SelectedDistribution.Clone();
            }
            dsc.SelectedDistribution = null;
            UpdateDataGrid();
        }

        /// <summary>
        /// Updates the data grid with the current quantile priors from the model.
        /// Configures column headers, tooltips, and binds the data source to the grid.
        /// </summary>
        private void UpdateDataGrid()
        {
            PriorDistributionsDataGrid.ItemsSource = null;
            if (Model == null || Model.QuantilePriors == null) return;

            PriorDistributionsDataGrid.ItemsSource = Model.QuantilePriors;

            // Parameter Column Header
            // XGoodFit port: styles are ControlThemes, whose BasedOn is WPF's basedOn (see HeaderThemeOf). A
            // setter can't hold a control, as WPF's can: the tool tip's text block is a template.
            var style = new ControlTheme(typeof(DataGridColumnHeader)) { BasedOn = HeaderThemeOf(QuantileColumn, "Left_ColumnHeaderStyle") };
            style.Setters.Add(new Setter(ToolTip.TipProperty, new FuncTemplate<TextBlock>(() => new TextBlock() { Text = "The exceedance probability of the quantile prior.", FontWeight = FontWeight.Normal, TextAlignment = TextAlignment.Left, TextWrapping = TextWrapping.Wrap })));
            QuantileColumn.HeaderTheme = style;

            // Distribution Column Header
            // XGoodFit port: styles are ControlThemes, whose BasedOn is WPF's basedOn (see HeaderThemeOf). A
            // setter can't hold a control, as WPF's can: the tool tip's text block is a template.
            style = new ControlTheme(typeof(DataGridColumnHeader)) { BasedOn = HeaderThemeOf(QuantileDistributionColumn, "Center_ColumnHeaderStyle") };
            style.Setters.Add(new Setter(ToolTip.TipProperty, new FuncTemplate<TextBlock>(() => new TextBlock() { Text = "Set the prior distributions of the distribution quantile.", FontWeight = FontWeight.Normal, TextAlignment = TextAlignment.Left, TextWrapping = TextWrapping.Wrap })));
            QuantileDistributionColumn.HeaderTheme = style;

            PriorDistributionsDataGrid.Items.Refresh();
        }

        /// <summary>
        /// XGoodFit port: upstream's QuantileColumn, the grid's first column (Avalonia's XAML can't name a column).
        /// </summary>
        private DataGridColumn QuantileColumn => PriorDistributionsDataGrid.Columns[0];

        /// <summary>
        /// XGoodFit port: upstream's QuantileDistributionColumn, the grid's second column (Avalonia's XAML can't name a column).
        /// </summary>
        private DataGridColumn QuantileDistributionColumn => PriorDistributionsDataGrid.Columns[1];

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

        /// <summary>
        /// Handles the preview mouse left button down event for the use single quantile control.
        /// Raises the <see cref="ShowPropertyAttributes"/> event to display property information for the UseSingleQuantile property.
        /// </summary>
        /// <param name="sender">The control that was clicked.</param>
        /// <param name="e">Event arguments for the mouse button event.</param>
        private void UseSingleQuantile_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            ShowPropertyAttributes?.Invoke(nameof(Model.UseSingleQuantile), Model);
        }

        /// <summary>
        /// Handles the preview mouse left button down event for the enable prior quantile distributions control.
        /// Raises the <see cref="ShowPropertyAttributes"/> event to display property information for the EnableQuantilePriors property.
        /// </summary>
        /// <param name="sender">The control that was clicked.</param>
        /// <param name="e">Event arguments for the mouse button event.</param>
        private void EnablePriorQuantileDistributions_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            ShowPropertyAttributes?.Invoke(nameof(Model.EnableQuantilePriors), Model);
        }

        /// <summary>
        /// Handles the preview mouse left button down event for the prior distributions data grid.
        /// Raises the <see cref="ShowPropertyAttributes"/> event to display property information for the QuantilePriors collection.
        /// </summary>
        /// <param name="sender">The data grid that was clicked.</param>
        /// <param name="e">Event arguments for the mouse button event.</param>
        private void PriorDistributionsDataGrid_PreviewMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            ShowPropertyAttributes?.Invoke(nameof(Model.QuantilePriors), Model);
        }

    }
}
