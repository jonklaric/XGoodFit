using FrameworkInterfaces;
using FrameworkUI;
using FrameworkUI.ProjectExplorer;
using GenericControls;
using OxyPlot.Wpf;
using OxyPlotControls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace FrameworkUI.Demo.UI
{
    /// <summary>
    /// Example project node controller that demonstrates FrameworkUI features including custom context menus, element creation, and document management.
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
    public class DemoProjectNode : FrameworkUIController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DemoProjectNode"/> class.
        /// </summary>
        /// <param name="project">The project to be managed by this controller.</param>
        public DemoProjectNode(IProject project) : base(project)
        {

            _plotPropertiesControl = new OxyPlotPropertiesControl() { Margin = new Thickness(0, 0, 5, 5) };
            _plotPropertiesControl.ClosePropertiesCalled += (x) => { ClosePlotProperties_Click(x.Plot); };
        }

        /// <summary>
        /// Indicates whether the plot properties control panel is currently open and visible to the user.
        /// </summary>
        private bool _plotPropertiesOpen;

        /// <summary>
        /// Flag indicating that the plot properties control is in the process of closing.
        /// Used to prevent recursive closure operations and ensure proper cleanup.
        /// </summary>
        private bool _plotPropertiesClosing = false;

        /// <summary>
        /// The OxyPlot properties control instance used to display and edit plot configuration settings.
        /// This control is shared across all plot types in the application.
        /// </summary>
        private OxyPlotPropertiesControl _plotPropertiesControl;

        /// <summary>
        /// Gets a value indicating whether multiple nodes can be selected simultaneously.
        /// </summary>
        public override bool CanMultiSelect => false;

        #region Menu Items

        /// <summary>
        /// Defines and configures context menu items for the project explorer tree view. Creates custom menu items
        /// for each element collection type including time series, input data, and various analysis types.
        /// </summary>
        /// <remarks>
        /// This method iterates through all child nodes in the project tree and adds appropriate context menu items
        /// based on the element collection type. Menu items include creation wizards for new elements and batch
        /// run operations for analysis collections. The method disables the default "Create New" context item and
        /// replaces it with custom, type-specific menu items.
        /// </remarks>
        /// <summary>
        /// Creates an Image whose Source is bound to a theme-aware icon resource key.
        /// SetResourceReference keeps the icon live across theme changes, unlike a
        /// one-time StaticResource lookup.
        /// </summary>
        /// <param name="resourceKey">The icon resource key to bind to.</param>
        /// <returns>An Image bound to the themed icon resource.</returns>
        private static Image CreateThemedIcon(string resourceKey)
        {
            var img = new Image();
            img.SetResourceReference(Image.SourceProperty, resourceKey);
            return img;
        }

        /// <summary>Adds the hazard-element creation command to compatible collection nodes.</summary>
        protected override void DefineProjectExplorerMenuItems()
        {
            for (int i = 0; i < ChildNodes.Count; i++)
            {
                ElementNodeCollection elementNodeCollection = null;
                if (ChildNodes[i] as ElementNodeCollection != null)
                    elementNodeCollection = (ElementNodeCollection)ChildNodes[i];

                if (elementNodeCollection == null) continue;

                // Hazard Element
                if (elementNodeCollection.ElementCollection.GetType() == typeof(HazardElementCollection))
                {
                    MenuItem menuItem = new MenuItem() { Header = "New Hazard Element...", Icon = CreateThemedIcon("HazardElementIcon") };
                    menuItem.Click += (object sender, RoutedEventArgs e) => CreateNewHazardElement(elementNodeCollection);
                    elementNodeCollection.CustomContextItems.Add(menuItem);
                }
   
            }
        }

        /// <summary>
        /// Defines and configures menu items for the main window's Project menu. Creates menu entries for
        /// adding new data elements and analyses, organized by type and functionality.
        /// </summary>
        /// <remarks>
        /// This method builds the Project menu by iterating through all element collections in the project
        /// and creating appropriate menu items for each type. For univariate analyses, multiple sub-menu
        /// items are grouped under a single parent menu item. All menu items are added to the
        /// _projectMenuItems collection for display in the main window.
        /// </remarks>
        protected override void DefineProjectMenuItems()
        {
            for (int i = 0; i < Project.ElementCollections.Count; i++)
            {
                var collection = Project.ElementCollections[i];

                // Hazard Element
                if (collection.GetType() == typeof(HazardElementCollection))
                {
                    MenuItem menuItem = new MenuItem() { Header = "New Hazard Element...", Icon = CreateThemedIcon("HazardElementIcon") };
                    menuItem.Click += (object sender, RoutedEventArgs e) => CreateNewHazardElement_Click(collection);
                    _projectMenuItems.Add(menuItem);
                }

            }

        }


        /// <summary>
        /// Creates a new hazard element with a user-specified name.
        /// </summary>
        /// <param name="elementNodes">The element node collection to add the new element to.</param>
        /// <returns>The newly created hazard element, or <see langword="null"/> when the dialog is canceled.</returns>
        private HazardElement CreateNewHazardElement(ElementNodeCollection elementNodes)
        {
            string newName = CreateNewNameDialog($"Create New {elementNodes.ElementCollection.Name}...", $"{elementNodes.ElementCollection.Name}_{elementNodes.ElementCollection.Count + 1}", elementNodes.ElementCollection.Select(x => x.Name.ToString()).ToList());
            if (newName == "") return null;
            var newHazard = new HazardElement(newName, elementNodes.ElementCollection);
            elementNodes.ElementCollection.Add(newHazard);
            return newHazard;
        }

        /// <summary>
        /// Handles the creation of a new hazard element. Displays a naming dialog and adds the element
        /// to the collection if a valid name is provided.
        /// </summary>
        /// <param name="collection">The element collection to which the new hazard element will be added.</param>
        /// <remarks>
        /// The method generates a default name based on the current collection count and prompts the user
        /// to provide a unique name. If the user cancels or provides an empty name, no element is created.
        /// </remarks>
        private void CreateNewHazardElement_Click(IElementCollection collection)
        {
            string newName = CreateNewNameDialog("New Hazard", $"Hazard_{collection.Count + 1}", collection.Select(x => x.Name.ToString()).ToList());
            if (newName != "") collection.Add(new HazardElement(newName, collection));
        }

        /// <summary>
        /// Shows a dialog for creating a new element name, ensuring uniqueness.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="initialName">The initial suggested name.</param>
        /// <param name="existingElementNames">List of existing element names to check for uniqueness.</param>
        /// <returns>The new element name, or empty string if cancelled.</returns>
        private string CreateNewNameDialog(string title, string initialName, List<string> existingElementNames)
        {
            var nameDialog = new GenericControls.NameDialog(50, "", false, existingElementNames.ToArray(), NameTextBox.GetDefaultInvalidCharacters())
            {
                Icon = Application.Current.TryFindResource("AddImage") as ImageSource,
                Title = title,
                Owner = Window.GetWindow(this),
                Text = initialName
            };
            if (nameDialog.ShowDialog() == true)
            {
                return nameDialog.Text;
            }
            else
            {

                return "";
            }
        }

        /// <summary>
        /// Handles custom menu item click events.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The routed event arguments.</param>
        private void CustomMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Yay", "Custom Menu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        #endregion

        #region AvalonDock

        /// <summary>
        /// Creates and returns the appropriate document control for displaying and editing the specified element.
        /// Matches the element type to its corresponding control and configures event handlers for plot properties
        /// and preview interactions.
        /// </summary>
        /// <param name="element">The project element to create a document control for (e.g., time series, input data, analysis).</param>
        /// <returns>
        /// A <see cref="Control"/> instance configured for the specified element type, or null if the element type
        /// is not recognized or does not have a corresponding document control.
        /// </returns>
        /// <remarks>
        /// This method supports all element types in the FrameworkUI.Demo application including:
        /// <list type="bullet">
        /// <item>Time Series elements</item>
        /// <item>Input Data elements</item>
        /// <item>Fitting Analysis elements</item>
        /// <item>Univariate, Point Process, Mixture, B17C, and Composite analyses</item>
        /// <item>Bivariate Analysis elements</item>
        /// <item>Rating Curve Analysis elements</item>
        /// <item>Time Series Analysis elements</item>
        /// </list>
        /// Each control is configured with appropriate event handlers for plot property editing and user interactions.
        /// </remarks>
        public override Control GetDocumentControl(IElement element)
        {
            // Hazard Element
            if (element.ParentCollection as HazardElementCollection != null)
            {
                if (element as HazardElement != null)
                {
                    var cntrl = new HazardControl() { Element = (HazardElement)element };
                    cntrl.PlotToolbar.PropertiesCalled += PlotPropertiesCalled;
                    cntrl.PreviewControlClicked += DocumentControl_PreviewClicked;
                    return cntrl;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves the project element associated with the specified control. Works with both document controls
        /// and properties controls to return the underlying data element being displayed or edited.
        /// </summary>
        /// <param name="control">The control to retrieve the associated element from.</param>
        /// <returns>
        /// The <see cref="IElement"/> instance associated with the control, or null if the control type
        /// is not recognized or does not have an associated element.
        /// </returns>
        /// <remarks>
        /// This method supports extraction of elements from all document controls and properties controls
        /// used in the FrameworkUI.Demo application. It performs type checking on the control to determine which element
        /// property to access.
        /// </remarks>
        public override IElement GetControlElement(UIElement control)
        {
            // Check if document control
            // Hazard Element
            if (control as HazardControl != null)
                return ((HazardControl)control).Element;

            
            // Check if properties control         
            // Hazard Element
            if (control as HazardPropertiesControl != null)
                return ((HazardPropertiesControl)control).Element;

            return null;
        }

        /// <summary>
        /// Handles cleanup operations when a document control is closed. Saves plot settings to the element
        /// and unregisters event handlers to prevent memory leaks.
        /// </summary>
        /// <param name="documentControl">The document control that is being closed.</param>
        /// <remarks>
        /// This method performs essential cleanup operations for each control type:
        /// <list type="bullet">
        /// <item>Serializes and saves all plot configurations back to the element</item>
        /// <item>Unsubscribes from plot properties events</item>
        /// <item>Unsubscribes from preview control clicked events</item>
        /// </list>
        /// Proper cleanup ensures that user customizations to plots are preserved and that
        /// no memory leaks occur from orphaned event handlers.
        /// </remarks>
        public override void DocumentClosed(UIElement documentControl)
        {

            // Hazard Element
            if (documentControl as HazardControl != null)
            {
                var cntrl = (HazardControl)documentControl;
                cntrl.PlotToolbar.PropertiesCalled -= PlotPropertiesCalled;
                cntrl.PreviewControlClicked -= DocumentControl_PreviewClicked;
            }

            return;
        }

        /// <summary>
        /// Handles cleanup operations when a properties control is closed. Clears element references
        /// to prevent memory leaks and ensure proper garbage collection.
        /// </summary>
        /// <param name="documentControl">The properties control that is being closed.</param>
        /// <remarks>
        /// This method sets the Element property of each properties control to null, breaking the
        /// reference to the underlying data element and allowing proper cleanup. This is important
        /// for memory management in long-running sessions with many document operations.
        /// </remarks>
        public override void PropertiesClosed(UIElement documentControl)
        {
            // Hazard Element
            if (documentControl as HazardPropertiesControl != null)
                ((HazardPropertiesControl)documentControl).Element = null;
        }

        /// <summary>
        /// Retrieves a properties control for the specified element. This method is currently not implemented
        /// as RMC-BestFit uses document-based properties controls rather than element-based ones.
        /// </summary>
        /// <param name="element">The element to create a properties control for.</param>
        /// <returns>Always returns null as this method is not implemented.</returns>
        /// <remarks>
        /// Properties controls are created based on the active document control rather than
        /// directly from elements. See <see cref="GetPropertiesControl(UIElement)"/> for the implemented approach.
        /// </remarks>
        public override Control GetPropertiesControl(IElement element)
        {
            return null;
        }

        /// <summary>
        /// Creates and returns the appropriate properties control for the specified document control.
        /// Handles special logic for plot properties controls to prevent unnecessary closure and recreation.
        /// </summary>
        /// <param name="documentControl">The document control to create a properties panel for.</param>
        /// <returns>
        /// A <see cref="Control"/> instance configured as the properties panel for the document control,
        /// or null if the plot properties control is already open for the current plot or if the control
        /// type is not recognized.
        /// </returns>
        /// <remarks>
        /// This method implements smart properties panel management:
        /// <list type="bullet">
        /// <item>Detects if the plot properties control is already open for the current plot and avoids reopening</item>
        /// <item>Creates element-specific properties controls for each analysis and data type</item>
        /// <item>Handles the _plotPropertiesOpen and _plotPropertiesClosing flags to coordinate state</item>
        /// </list>
        /// The method checks all plot types within each control to determine if the properties panel
        /// should remain open or be replaced with a new properties control.
        /// </remarks>
        public override Control GetPropertiesControl(UIElement documentControl)
        {
            bool isOpen = _plotPropertiesOpen;
            _plotPropertiesOpen = false;

            // The plot properties check has to be done in each if statement below because we have to do an equality check on the plot.

            // Hazard Element
            if (documentControl as HazardControl != null)
            {
                if (isOpen == true && _plotPropertiesClosing == false)
                {
                    var plot = ((HazardControl)documentControl).Plot;
                    if (plot != null && _plotPropertiesControl.Plot.Equals(plot))
                    {
                        _plotPropertiesOpen = true;
                        return null;
                    }
                }
                var element = ((HazardControl)documentControl).Element;
                return new HazardPropertiesControl() { Element = element };
            }

            return null;
        }

        #endregion

        #region OxyPlot

        /// <summary>
        /// Handles requests to show or toggle the OxyPlot properties control panel. Manages the visibility state
        /// of the properties panel and coordinates which plot's properties are currently being displayed.
        /// </summary>
        /// <param name="plotRequestingProperties">The OxyPlot plot instance that is requesting the properties panel.</param>
        /// <param name="openProperties">Indicates whether to open the properties control if it is not already open.</param>
        /// <param name="propertyExpander">The specific property expander section to open within the properties control.</param>
        /// <param name="selectedObject">The OxyPlot object (axis, series, annotation, etc.) that has been selected by the user for editing.</param>
        /// <remarks>
        /// This method implements toggle behavior: if the properties panel is already open for the requesting plot
        /// and no specific object is selected, the panel will be closed. Otherwise, the panel is opened or updated
        /// to show the requested plot's properties. The method also handles expanding specific property sections
        /// when a plot element is selected.
        /// </remarks>
        private void PlotPropertiesCalled(Plot plotRequestingProperties, bool openProperties, OxyPlotPropertiesControl.PropertyEXP? propertyExpander, object selectedObject)
        {
            if (_plotPropertiesOpen && _plotPropertiesControl.Plot.Equals(plotRequestingProperties) && selectedObject == null)
            {
                ClosePlotProperties_Click(_plotPropertiesControl.Plot);
                _plotPropertiesOpen = false;
            }
            else if (openProperties == true)
            {
                _plotPropertiesControl.Plot = plotRequestingProperties;
                RaiseSetPropertiesControl(_plotPropertiesControl);
                _plotPropertiesOpen = true;
            }

            if (_plotPropertiesOpen && propertyExpander.HasValue)
            {
                _plotPropertiesControl.ExpandProperty(propertyExpander.Value, selectedObject);
            }
        }

        /// <summary>
        /// Closes the OxyPlot properties control panel if it is currently open for the specified plot.
        /// Sets appropriate state flags to coordinate the closure operation.
        /// </summary>
        /// <param name="plotRequestingProperties">The OxyPlot plot instance that is requesting closure of the properties panel.</param>
        /// <remarks>
        /// This method verifies that the properties panel is open and that it is displaying properties for
        /// the requesting plot before initiating closure. The _plotPropertiesClosing flag is set during the
        /// closure operation to prevent recursive calls or interference with other state management operations.
        /// If the properties panel is not open or is showing a different plot, the method returns without action.
        /// </remarks>
        private void ClosePlotProperties_Click(Plot plotRequestingProperties)
        {
            if (_plotPropertiesControl.Plot == null || _plotPropertiesOpen == false) return;
            if (_plotPropertiesControl.Plot.Equals(plotRequestingProperties) == false) return;
            _plotPropertiesClosing = true;
            RaiseClosePropertiesControl(_plotPropertiesControl);
            _plotPropertiesOpen = false;
            _plotPropertiesClosing = false;
        }

        /// <summary>
        /// Handles preview click events on document controls to determine if the plot properties panel should be closed.
        /// Closes the properties panel if the user clicked outside the plot and toolbar areas.
        /// </summary>
        /// <param name="plotClicked">Indicates whether the plot area itself was clicked by the user.</param>
        /// <param name="toolbarClicked">Indicates whether the plot's toolbar was clicked by the user.</param>
        /// <param name="plot">The plot instance associated with the clicked document control.</param>
        /// <remarks>
        /// This method implements user-friendly behavior where clicking on the document control's background
        /// or other non-plot areas will automatically close the plot properties panel. This helps maintain
        /// a clean interface and clear user intent. If either the plot or toolbar was clicked, the properties
        /// panel remains open to allow continued editing.
        /// </remarks>
        private void DocumentControl_PreviewClicked(bool plotClicked, bool toolbarClicked, Plot plot)
        {
            if (plotClicked == false && toolbarClicked == false)
                ClosePlotProperties_Click(plot);
        }

        #endregion
    }
}
