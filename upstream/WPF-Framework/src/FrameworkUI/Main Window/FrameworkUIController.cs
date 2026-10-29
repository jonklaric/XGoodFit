using FrameworkUI.ProjectExplorer;
using FrameworkInterfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace FrameworkUI
{
    /// <summary>
    /// A class for controlling the FrameworkUI.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public abstract class FrameworkUIController : ProjectNode
    {

        /// <summary>
        /// Construct new ProjectUI controller.
        /// </summary>
        /// <param name="project">Project as IProject.</param>
        public FrameworkUIController(IProject project): base(project)
        {
            // Load child nodes in the project node
            Load();

            // Define ProjectUI menu items
            DefineProjectMenuItems();
            DefineToolsMenuItems();
            DefineHelpMenuItems();
        }

        #region Members

        /// <summary>
        /// Event is raised when the properties control is set.
        /// </summary>
        public event SetPropertiesControlEventHandler? SetPropertiesControl;
        /// <summary>
        /// Delegate for handling the set properties control event.
        /// </summary>
        /// <param name="propertyControl">The property control to be set.</param>
        public delegate void SetPropertiesControlEventHandler(Control propertyControl);

        /// <summary>
        /// Event is raised when the properties control is closed.
        /// </summary>
        public event ClosePropertiesControlEventHandler? ClosePropertiesControl;
        /// <summary>
        /// Delegate for handling the close properties control event.
        /// </summary>
        /// <param name="propertyControl">The property control to be closed.</param>
        public delegate void ClosePropertiesControlEventHandler(Control propertyControl);

        /// <summary>
        /// The project explorer project node.
        /// </summary>
        public ProjectNode? ProjectNode { get; private set; }

        /// <summary>
        /// Defines the default Avalon Dock layout.
        /// </summary>
        public string? DefaultAvalonDockLayout { get; private set; }

        /// <summary>
        /// List of custom project menu items.
        /// </summary>
        protected readonly List<MenuItem> _projectMenuItems = new List<MenuItem>();
        /// <summary>
        /// List of custom tools menu items.
        /// </summary>
        protected readonly List<MenuItem> _toolsMenuItems = new List<MenuItem>();
        /// <summary>
        /// List of custom help menu items.
        /// </summary>
        protected readonly List<MenuItem> _helpMenuItems = new List<MenuItem>();
        /// <summary>
        /// List of custom menu items that appear between Tools and Window menus.
        /// </summary>
        protected readonly List<MenuItem> _customMenuItems = new List<MenuItem>();

        /// <summary>
        /// The custom project menu items to be set on the application menu bar.
        /// </summary>
        public ReadOnlyCollection<MenuItem> ProjectMenuItems => new ReadOnlyCollection<MenuItem>(_projectMenuItems);

        /// <summary>
        /// The custom tools menu items to be set on the application menu bar.
        /// </summary>
        public ReadOnlyCollection<MenuItem> ToolsMenuItems => new ReadOnlyCollection<MenuItem>(_toolsMenuItems);

        /// <summary>
        /// The custom help menu items to be set on the application menu bar.
        /// </summary>
        public ReadOnlyCollection<MenuItem> HelpMenuItems => new ReadOnlyCollection<MenuItem>(_helpMenuItems);

        /// <summary>
        /// The custom menu items to be set on the application menu bar. These are set in between the Tools and Window menu. E.g., "Mapping". 
        /// </summary>
        public ReadOnlyCollection<MenuItem> CustomMenuStripItems => new ReadOnlyCollection<MenuItem>(_customMenuItems);

        #endregion

        #region Methods

        /// <summary>
        /// This method is called in the base constructor and used to define the custom Project menu items set in the application menu bar.
        /// Override this method to add custom project menu items.
        /// </summary>
        protected virtual void DefineProjectMenuItems() { }

        /// <summary>
        /// Called by <see cref="FrameworkUI.MainWindow"/> when the theme changes to refresh menu item icons.
        /// The default implementation clears and re-calls <see cref="DefineProjectMenuItems"/>,
        /// <see cref="DefineToolsMenuItems"/>, and <see cref="DefineHelpMenuItems"/>.
        /// Override in derived classes if only a subset of menus needs rebuilding.
        /// </summary>
        protected internal virtual void RefreshMenuIcons()
        {
            _projectMenuItems.Clear();
            DefineProjectMenuItems();
            _toolsMenuItems.Clear();
            DefineToolsMenuItems();
            _helpMenuItems.Clear();
            DefineHelpMenuItems();
        }

        /// <summary>
        /// This method is called in the base constructor and used to define the custom Tools menu items set in the application menu bar.
        /// Override this method to add custom tools menu items.
        /// </summary>
        protected virtual void DefineToolsMenuItems() { }

        /// <summary>
        /// This method is called in the base constructor and used to define the custom Help menu items set in the application menu bar.
        /// The default implementation adds "Terms and Conditions for Use" and "About" menu items.
        /// Override this method to add custom help menu items. Call base.DefineHelpMenuItems() to include the defaults.
        /// </summary>
        protected virtual void DefineHelpMenuItems()
        {
            // Terms & Conditions for Use
            var tcuMenuItem = new MenuItem() { Header = "Terms & Conditions for Use", Icon = Application.Current.TryFindResource("TCUIcon") };
            tcuMenuItem.Click += (s, e) =>
            {
                var window = new TermsAndConditionsWindow();
                window.ShowButtons = false;
                if (Application.Current?.MainWindow != null)
                {
                    window.Owner = Application.Current.MainWindow;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                window.ShowDialog();
            };
            _helpMenuItems.Add(tcuMenuItem);

            // About
            var aboutMenuItem = new MenuItem() { Header = "About " + ApplicationAttributes.Title, Icon = Application.Current.TryFindResource("AboutIcon") };
            aboutMenuItem.Click += (s, e) =>
            {
                var window = new AboutWindow();
                if (Application.Current?.MainWindow != null)
                {
                    window.Owner = Application.Current.MainWindow;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                window.ShowDialog();
            };
            _helpMenuItems.Add(aboutMenuItem);
        }

        /// <summary>
        /// Get a new instance of the Document Control for a project element.
        /// </summary>
        /// <param name="element">The project element.</param>
        /// <returns></returns>
        public abstract Control GetDocumentControl(IElement element);

        /// <summary>
        /// When the document control is closed, this method can be called to perform extra tasks such as updating plot settings.
        /// </summary>
        /// <param name="documentControl">The document control for a project element.</param>
        public abstract void DocumentClosed(UIElement documentControl);

        /// <summary>
        /// When the properties control is closes, this method can be called to perform extra tasks. 
        /// </summary>
        /// <param name="propertiesControl">The properties control for a project element.</param>
        public abstract void PropertiesClosed(UIElement propertiesControl);

        /// <summary>
        /// Get a new instance of the Properties Control for a project element.
        /// </summary>
        /// <param name="documentControl">The document control for a project element.</param>
        public abstract Control GetPropertiesControl(UIElement documentControl);

        /// <summary>
        /// Get a new instance of the Properties Control for a project element.
        /// </summary>
        /// <param name="element">The project element.</param>
        public abstract Control GetPropertiesControl(IElement element);

        /// <summary>
        /// Gets the model element from the specified control. The control will generally be from a document or properties control.
        /// </summary>
        /// <param name="control">Document or properties control to get element from.</param>
        public abstract IElement GetControlElement(UIElement control);

        /// <summary>
        /// Raises the set properties control event. 
        /// </summary>
        /// <param name="propertyControl">The property control to set.</param>
        protected void RaiseSetPropertiesControl(Control propertyControl)
        {
            SetPropertiesControl?.Invoke(propertyControl);
        }

        /// <summary>
        /// Raises the close property control event.
        /// </summary>
        /// <param name="propertyControl">The property control to close.</param>
        protected void RaiseClosePropertiesControl(Control propertyControl)
        {
            ClosePropertiesControl?.Invoke(propertyControl);
        }

        #endregion
    }
}
