using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using FrameworkUI.MessageWindow;
using FrameworkUI.ProjectExplorer;
using FrameworkInterfaces;
using FrameworkInterfaces.Undo;
using GenericControls;
using SoftwareUpdate;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace FrameworkUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public partial class MainWindow : MetroWindow
    {

        /// <summary>
        /// Construct new main window.
        /// </summary>
        public MainWindow()
        {

            // This call is required by the designer.
            InitializeComponent();

            // Mouse move handler
            MouseMove += Me_MouseMove;

            // Add any initialization after the InitializeComponent() call.
            //
            _projectExplorerTreeView = new ProjectExplorerTreeView() { Style = (Style)FindResource("TreeViewStyle") };
            _messageWindowControl = new MessageWindowControl();
            //
            // Load User Settings
            UserSettings.Load(ShellPublicVariables.UserSettingsFilePath);
            //
            // Set up Undo/Redo button visibility
            UndoRedoPanel.Visibility = UserSettings.ShowUndoRedoButtons ? Visibility.Visible : Visibility.Collapsed;
            //
            // Setup Recent Files
            RecentFiles.FilePath = ShellPublicVariables.RecentFileListFilePath;
            RecentFiles.NumberOfFilesToDisplay = UserSettings.MaxRecentFileItems;
            RecentFiles.LoadFromXML();
            RecentFiles.MenuClick += (x) => OpenRecentProject(x);
            // 
            // Setup default application layout
            ProjectExplorerLayout.Title = ShellPublicVariables.ProjectExplorerTitle;
            ProjectExplorerLayout.ContentId = ShellPublicVariables.ProjectExplorerContentID;
            MessageWindowLayout.Title = ShellPublicVariables.MessageWindowTitle;
            MessageWindowLayout.ContentId = ShellPublicVariables.MessageWindowContentID;
            PropertiesWindowLayout.Title = ShellPublicVariables.PropertiesWindowTitle;
            PropertiesWindowLayout.ContentId = ShellPublicVariables.PropertiesWindowContentID;
            // 
            if (UserSettings.SaveWindowLayout == true) LoadLayout();
            // 
            BuildMessageWindow();
            BuildPropertiesWindow();
            BuildProjectExplorer();
            OpenWindows.NumberOfWindowsToDisplay = UserSettings.MaxWindowMenuItems;
            // 
            // Set the message window properties
            var messenger = FrameworkInterfaces.Messaging.Messenger.GetInstance();
            messenger.MessagesAdded += ProjectMessageAdded;
            messenger.TextFileName = ShellPublicVariables.MessageLogFilePath;
            messenger.MessageBeep = UserSettings.MessageBeep;
            messenger.WarningBeep = UserSettings.WarningBeep;
            messenger.ErrorBeep = UserSettings.ErrorBeep;
            messenger.EventBeep = UserSettings.EventBeep;
            messenger.MessageColor = new SolidColorBrush(UserSettings.MessageColor);
            messenger.WarningColor = new SolidColorBrush(UserSettings.WarningColor);
            messenger.ErrorColor = new SolidColorBrush(UserSettings.ErrorColor);
            messenger.EventColor = new SolidColorBrush(UserSettings.EventColor);
            // 
            // Add handlers
            FileSizeManager.ReportProgress += FileSizeManager_ReportProgress;
            AutoBackup.ReportProgress += AutoBackup_ReportProgress;
            ThemeManager.ThemeChanged += ThemeChanged;
            // 
            // Update themes
            if (UserSettings.ColorTheme == "Blue")
            {
                ThemeManager.SetTheme(ThemeColor.Blue);
            }
            else if (UserSettings.ColorTheme == "Dark")
            {
                ThemeManager.SetTheme(ThemeColor.Dark);
            }
            else if (UserSettings.ColorTheme == "Light")
            {
                ThemeManager.SetTheme(ThemeColor.Light);
            }
            else
            {
                // Unknown / unrecognized theme value (corrupt setting, future theme name, etc.) — fall back to Light
                // so that ThemeManager always loads a valid theme dictionary before any UI renders.
                ThemeManager.SetTheme(ThemeColor.Light);
            }
            // Dummy call using Avalon Themes method so that the dlls will be copied with the ProjectUI control.
            var avalonDummy = new Xceed.Wpf.AvalonDock.Themes.Vs2013BlueTheme();
        }




        /// <summary>
        /// When the theme changes, update resource dictionaries.
        /// </summary>
        /// <param name="newThemeDictionary">The new resource dictionary.</param>
        /// <param name="newThemeColor">The new theme color.</param>
        private void ThemeChanged(ResourceDictionary newThemeDictionary, ThemeColor newThemeColor)
        {
            // Create a new resource dictionary
            string blueString = "pack://application:,,,/Xceed.Wpf.AvalonDock.Themes.VS2013;component/BlueTheme.xaml";
            string darkString = "pack://application:,,,/Xceed.Wpf.AvalonDock.Themes.VS2013;component/DarkTheme.xaml";
            string lightString = "pack://application:,,,/Xceed.Wpf.AvalonDock.Themes.VS2013;component/LightTheme.xaml";

            // Remove the old theme dictionary
            Resources.MergedDictionaries.Remove(_avalonDockThemeDictionary);

            // Set the new theme dictionary source
            if (newThemeColor == ThemeColor.Blue)
            {
                _avalonDockThemeDictionary.Source = new Uri(blueString, UriKind.RelativeOrAbsolute);
            }
            else if (newThemeColor == ThemeColor.Dark)
            {
                _avalonDockThemeDictionary.Source = new Uri(darkString, UriKind.RelativeOrAbsolute);
            }
            else if (newThemeColor == ThemeColor.Light)
            {
                _avalonDockThemeDictionary.Source = new Uri(lightString, UriKind.RelativeOrAbsolute);
            }
            else
            {
                // Unknown theme — re-add dictionary without changing Source
                Resources.MergedDictionaries.Add(_avalonDockThemeDictionary);
                return;
            }

            // Reset the theme dictionary
            Resources.MergedDictionaries.Add(_avalonDockThemeDictionary);

            // Refresh document tab icons for theme-aware elements
            foreach (var item in OpenWindows.Collection)
            {
                if (item.Element?.ElementImageResourceKey != null && item.Document != null)
                    item.Document.IconSource = Application.Current?.TryFindResource(item.Element.ElementImageResourceKey) as ImageSource;
            }

            // Rebuild application menu icons (Project, Tools, Help)
            var projectNode = ProjectNode;
            if (projectNode != null)
            {
                projectNode.RefreshMenuIcons();

                ProjectMenuItems.Clear();
                foreach (var item in projectNode.ProjectMenuItems)
                    ProjectMenuItems.Add(item);

                ToolsMenuItems.Clear();
                foreach (var item in projectNode.ToolsMenuItems)
                    ToolsMenuItems.Add(item);

                HelpMenuItems.Clear();
                foreach (var item in projectNode.HelpMenuItems)
                    HelpMenuItems.Add(item);
            }
        }

        #region Members

        /// <summary>
        /// Resource dictionary for the AvalonDock theme.
        /// </summary>
        private ResourceDictionary _avalonDockThemeDictionary = new ResourceDictionary();
        /// <summary>
        /// Layout anchorable for the project explorer dock.
        /// </summary>
        private LayoutAnchorable? _projectExplorerDock;
        /// <summary>
        /// Layout anchorable for the message window dock.
        /// </summary>
        private LayoutAnchorable? _messageWindowDock;
        /// <summary>
        /// Layout anchorable for the properties window dock.
        /// </summary>
        private LayoutAnchorable? _propertiesWindowDock;
        /// <summary>
        /// The project explorer tree view control.
        /// </summary>
        private ProjectExplorerTreeView _projectExplorerTreeView;
        /// <summary>
        /// The message window control.
        /// </summary>
        private MessageWindowControl _messageWindowControl;
        /// <summary>
        /// Reference to the previously active document.
        /// </summary>
        private LayoutDocument? _previousActiveDocument = null;
        /// <summary>
        /// Indicates whether to load the full layout including documents.
        /// </summary>
        private bool _loadFullLayout = false;
        /// <summary>
        /// Indicates whether a project is currently being opened.
        /// </summary>
        private bool _openingProject = false;
        /// <summary>
        /// Indicates whether a project is currently being closed.
        /// </summary>
        private bool _closingProject = false;
        /// <summary>
        /// Indicates whether the properties pane was clicked.
        /// </summary>
        private bool _propertiesPaneClicked = false;
        /// <summary>
        /// Indicates whether the project explorer pane was clicked.
        /// </summary>
        private bool _projectExplorerPaneClicked = false;

        /// <summary>
        /// Event raised when the options apply button is clicked.
        /// </summary>
        public event RoutedEventHandler? Options_Apply_Click;
        /// <summary>
        /// Delegate for handling the preview save as event.
        /// </summary>
        /// <param name="sender">The project being saved.</param>
        /// <param name="newFilePath">The new file path.</param>
        /// <param name="cancel">Reference parameter to cancel the save operation.</param>
        public delegate void PreviewObjectSavedAsEventHandler(IProject sender, string newFilePath, ref bool cancel);
        /// <summary>
        /// Event raised before a project is saved with a new file name.
        /// </summary>
        public event PreviewObjectSavedAsEventHandler? PreviewSaveAs;

        /// <summary>
        /// Dependency property for the ProjectNode property.
        /// </summary>
        public static DependencyProperty ProjectNodeProperty = DependencyProperty.Register(nameof(ProjectNode), typeof(FrameworkUIController), typeof(MainWindow), new FrameworkPropertyMetadata(null, ProjectNode_PropertyChangedCallback));

        /// <summary>
        /// Callback method invoked when the ProjectNode dependency property changes.
        /// Handles cleanup of old project and initialization of new project.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">The event data containing old and new values.</param>
        private static void ProjectNode_PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(MainWindow)) return;
            MainWindow thisControl = (MainWindow)d;
            // 
            thisControl.ProjectMenuItems.Clear();
            // 
            // Get the old value
            FrameworkUIController? oldValue = e.OldValue as FrameworkUIController;
            // clean up any links to old project.
            if (oldValue != null)
            {
                oldValue.OnClick -= thisControl.Project_Click;
                oldValue.SetPropertiesControl -= thisControl.SetPropertiesRequested;
                oldValue.ClosePropertiesControl -= thisControl.ClosePropertiesRequested;
                oldValue.Project.PropertyChanged -= thisControl.ProjectPropertyChanged!;
                oldValue.ParentTreeView = null!;

                for (int i = 0; i < oldValue.ChildNodes.Count; i++)
                {
                    if (!(oldValue.ChildNodes[i] is ElementNodeCollection elementnodeCollection)) continue;

                    elementnodeCollection.NodeAdded -= thisControl.NodeAdded;
                    elementnodeCollection.NodeRemoved -= thisControl.NodeRemoved;
                    elementnodeCollection.NodeSorted -= thisControl.NodeSorted;

                    if (elementnodeCollection.ElementCollection != null)
                    {
                        for (int j = 0; j < elementnodeCollection.ElementCollection.Count; j++)
                        {
                            var element = elementnodeCollection.ElementCollection[j];
                            var elementNode = ElementNode.FindElementNode(element, oldValue.ChildNodes[i]);
                            if (elementNode != null) thisControl.NodeRemoved(elementNode);
                        }
                    }
                }
            }
            // Cancel AutoBackup
            AutoBackup.Cancel();
            //
            // Get the new value
            FrameworkUIController? newValue = e.NewValue as FrameworkUIController;
            if (newValue == null)
            {
                thisControl._projectExplorerTreeView.ProjectNode = null!;
                return;
            }
            thisControl._projectExplorerTreeView.ProjectNode = newValue;

            // Set main window Icon
            thisControl.Icon = newValue.Project.ProjectImage;

            // Set the Project menu items
            foreach (var item in newValue.ProjectMenuItems)
                thisControl.ProjectMenuItems.Add(item);
            // Set the Tools menu items
            foreach (var item in newValue.ToolsMenuItems)
                thisControl.ToolsMenuItems.Add(item);
            // Set the Help menu items
            foreach (var item in newValue.HelpMenuItems)
                thisControl.HelpMenuItems.Add(item);

            // Add handlers
            // 
            // Add handler to click event on project node
            newValue.OnClick += thisControl.Project_Click;
            // Add handler to handle project requests to set/remove the properties control (usually from oxyplot plot properties but could be anything).
            newValue.SetPropertiesControl += thisControl.SetPropertiesRequested;
            newValue.ClosePropertiesControl += thisControl.ClosePropertiesRequested;
            // Add handler to Project property changed
            newValue.Project.PropertyChanged += thisControl.ProjectPropertyChanged!;

            for (int i = 0; i < newValue.ChildNodes.Count; i++)
            {
                if (!(newValue.ChildNodes[i] is ElementNodeCollection elementnodeCollection)) continue;

                elementnodeCollection.NodeAdded += thisControl.NodeAdded;
                elementnodeCollection.NodeRemoved += thisControl.NodeRemoved;
                elementnodeCollection.NodeSorted += thisControl.NodeSorted;

                if (elementnodeCollection.ElementCollection != null)
                {
                    for (int j = 0; j < elementnodeCollection.ElementCollection.Count; j++)
                    {
                        var element = elementnodeCollection.ElementCollection[j];
                        var elementNode = ElementNode.FindElementNode(element, newValue.ChildNodes[i]);
                        if (elementNode != null) thisControl.NodeAdded(elementNode);
                    }
                }
            }

            // Start AutoBackup
            AutoBackup.Project = newValue.Project;
            if (UserSettings.CreateAutoRecoverBackup == true)
                AutoBackup.Start();
            // 
            // Load the Project specific layout
            thisControl.LoadLayout();
            thisControl.BuildProjectExplorer();
            thisControl.BuildPropertiesWindow();
            thisControl.ProjectPropertyChanged(thisControl, new PropertyChangedEventArgs(nameof(Name)));
        }

        /// <summary>
        /// Reloads the project node by removing and re-adding event handlers and reloading child nodes.
        /// </summary>
        private void LoadProjectNode()
        {
            if (ProjectNode == null) return;
            // Remove handlers
            ProjectNode.OnClick -= Project_Click;
            ProjectNode.SetPropertiesControl -= SetPropertiesRequested;
            ProjectNode.ClosePropertiesControl -= ClosePropertiesRequested;
            ProjectNode.Project.PropertyChanged -= ProjectPropertyChanged!;

            for (int i = 0; i < ProjectNode.ChildNodes.Count; i++)
            {
                if (!(ProjectNode.ChildNodes[i] is ElementNodeCollection elementnodeCollection)) continue;

                elementnodeCollection.NodeAdded -= NodeAdded;
                elementnodeCollection.NodeRemoved -= NodeRemoved;
                elementnodeCollection.NodeSorted -= NodeSorted;

                if (elementnodeCollection.ElementCollection != null)
                {
                    for (int j = 0; j < elementnodeCollection.ElementCollection.Count; j++)
                    {
                        var element = elementnodeCollection.ElementCollection[j];
                        var elementNode = ElementNode.FindElementNode(element, ProjectNode.ChildNodes[i]);
                        if (elementNode != null) NodeRemoved(elementNode);
                    }
                }
            }

            // Load
            ProjectNode.Load();

            // Add handlers
            ProjectNode.OnClick += Project_Click;
            ProjectNode.SetPropertiesControl += SetPropertiesRequested;
            ProjectNode.ClosePropertiesControl += ClosePropertiesRequested;
            ProjectNode.Project.PropertyChanged += ProjectPropertyChanged!;

            for (int i = 0; i < ProjectNode.ChildNodes.Count; i++)
            {
                if (!(ProjectNode.ChildNodes[i] is ElementNodeCollection elementnodeCollection)) continue;

                elementnodeCollection.NodeAdded += NodeAdded;
                elementnodeCollection.NodeRemoved += NodeRemoved;
                elementnodeCollection.NodeSorted += NodeSorted;

                if (elementnodeCollection.ElementCollection != null)
                {
                    for (int j = 0; j < elementnodeCollection.ElementCollection.Count; j++)
                    {
                        var element = elementnodeCollection.ElementCollection[j];
                        var elementNode = ElementNode.FindElementNode(element, ProjectNode.ChildNodes[i]);
                        if (elementNode != null) NodeAdded(elementNode);
                    }
                }
            }

        }

        /// <summary>
        /// Gets or sets the project node.
        /// </summary>
        /// <remarks>
        /// Call the shell MainWindow from the Application class on start up and input the project from the model library.
        /// </remarks>
        public FrameworkUIController ProjectNode
        {
            get { return (FrameworkUIController)GetValue(ProjectNodeProperty); }
            set { SetValue(ProjectNodeProperty, value); }
        }

        /// <summary>
        /// The update service for checking and downloading software updates.
        /// </summary>
        private IUpdateService? _updateService;

        /// <summary>
        /// Gets or sets the update service for checking software updates.
        /// When set, the "Check for Updates" menu item becomes visible.
        /// </summary>
        public IUpdateService? UpdateService
        {
            get { return _updateService; }
            set
            {
                _updateService = value;
                // Show the menu item when update service is available
                CheckForUpdatesMenuItem.Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Dependency property for the Project menu items collection.
        /// </summary>
        public static DependencyProperty ProjectMenuItemsProperty = DependencyProperty.Register(nameof(ProjectMenuItems), typeof(ObservableCollection<MenuItem>), typeof(MainWindow), new FrameworkPropertyMetadata(new ObservableCollection<MenuItem>()));

        /// <summary>
        /// Gets or sets the custom Project menu items.
        /// </summary>
        public ObservableCollection<MenuItem> ProjectMenuItems
        {
            get { return (ObservableCollection<MenuItem>)GetValue(ProjectMenuItemsProperty); }
            set { SetValue(ProjectMenuItemsProperty, value); }
        }

        /// <summary>
        /// Dependency property for the Tools menu items collection.
        /// </summary>
        public static DependencyProperty ToolsMenuItemsProperty = DependencyProperty.Register(nameof(ToolsMenuItems), typeof(ObservableCollection<MenuItem>), typeof(MainWindow), new FrameworkPropertyMetadata(new ObservableCollection<MenuItem>()));

        /// <summary>
        /// Gets or sets the custom Tools menu items.
        /// </summary>
        public ObservableCollection<MenuItem> ToolsMenuItems
        {
            get { return (ObservableCollection<MenuItem>)GetValue(ToolsMenuItemsProperty); }
            set { SetValue(ToolsMenuItemsProperty, value); }
        }

        /// <summary>
        /// Dependency property for the Help menu items collection.
        /// </summary>
        public static DependencyProperty HelpMenuItemsProperty = DependencyProperty.Register(nameof(HelpMenuItems), typeof(ObservableCollection<MenuItem>), typeof(MainWindow), new FrameworkPropertyMetadata(new ObservableCollection<MenuItem>()));

        /// <summary>
        /// Gets or sets the custom Help menu items.
        /// </summary>
        public ObservableCollection<MenuItem> HelpMenuItems
        {
            get { return (ObservableCollection<MenuItem>)GetValue(HelpMenuItemsProperty); }
            set { SetValue(HelpMenuItemsProperty, value); }
        }

        /// <summary>
        /// Dependency property for the Tools menu items collection.
        /// </summary>
        public static DependencyProperty ProjectDefaultsControlProperty = DependencyProperty.Register(nameof(ProjectDefaultsControl), typeof(UIElement), typeof(MainWindow), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the custom Tools menu items.
        /// </summary>
        public UIElement ProjectDefaultsControl
        {
            get { return (UIElement)GetValue(ProjectDefaultsControlProperty); }
            set { SetValue(ProjectDefaultsControlProperty, value); }
        }

        #endregion

        #region Project Node

        /// <summary>
        /// When the project properties change, update window title.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The property changed event arguments.</param>
        private void ProjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (ProjectNode == null)
            {
                Title = ShellPublicVariables.SoftwareName;
                return;
            }

            if (e.PropertyName == nameof(ProjectNode.Name) || e.PropertyName == nameof(ProjectNode.Project.FullFileName))
            {
                if (ProjectNode.Project.FullFileName.Contains(Path.GetTempPath()) == true)
                {
                    Title = $"{ShellPublicVariables.SoftwareName} {ProjectNode.Project.SoftwareVersion}  -  {ProjectNode.Project.Name}";
                }
                else
                {
                    Title = $"{ShellPublicVariables.SoftwareName} {ProjectNode.Project.SoftwareVersion}  -  {ProjectNode.Project.FullFileName}";
                }
            }
        }

        /// <summary>
        /// When one or more messages are added, attach a message action that navigates the
        /// user to the source element when the message is clicked.
        /// </summary>
        /// <param name="messages">The new messages.</param>
        private void ProjectMessageAdded(IMessageItem[] messages)
        {
            foreach (IMessageItem message in messages)
            {
                if (message.Source as IProject != null)
                {
                    // the source is a project
                    message.MessageAction = new Action<IMessageItem>((x) => Project_Click());
                }
                else if (message.Source as IElement != null)
                {
                    // the source is an element
                    message.MessageAction = new Action<IMessageItem>((x) => EditElement(new[] { (IElement)message.Source }));
                }
            }
        }

        /// <summary>
        /// Resolves the icon for an element. If the element provides an <see cref="IElement.ElementImageResourceKey"/>,
        /// the icon is looked up from application resources (theme-aware). Otherwise, falls back to <see cref="IElement.ElementImage"/>.
        /// </summary>
        /// <param name="element">The element to resolve the icon for.</param>
        /// <returns>The resolved <see cref="ImageSource"/>, or null.</returns>
        private static ImageSource? ResolveElementIcon(IElement element)
        {
            if (element.ElementImageResourceKey != null)
                return Application.Current?.TryFindResource(element.ElementImageResourceKey) as ImageSource;
            return element.ElementImage;
        }

        #endregion

        #region Layout

        /// <summary>
        /// Build the project explorer.
        /// </summary>
        private void BuildProjectExplorer()
        {
            var LayoutAnchorableList = MainDock.Layout.Descendents().OfType<LayoutAnchorable>().ToList();
            for (int i = 0; i < LayoutAnchorableList.Count; i++)
            {
                if (LayoutAnchorableList[i].ContentId == ShellPublicVariables.ProjectExplorerContentID)
                {
                    _projectExplorerDock = LayoutAnchorableList[i];
                    _projectExplorerDock.Content = _projectExplorerTreeView;
                    break;
                }
            }
            if (_projectExplorerDock != null)
                _projectExplorerDock.IsActive = true;
        }

        /// <summary>
        /// Build the message window.
        /// </summary>
        private void BuildMessageWindow()
        {
            var LayoutAnchorableList = MainDock.Layout.Descendents().OfType<LayoutAnchorable>().ToList();
            for (int i = 0; i < LayoutAnchorableList.Count; i++)
            {
                if (LayoutAnchorableList[i].ContentId == ShellPublicVariables.MessageWindowContentID)
                {
                    _messageWindowDock = LayoutAnchorableList[i];
                    _messageWindowDock.Content = _messageWindowControl;
                    break;
                }
            }
        }

        /// <summary>
        /// Build the properties window.
        /// </summary>
        private void BuildPropertiesWindow()
        {
            if (_propertiesWindowDock != null)
                _propertiesWindowDock.IsActiveChanged -= PropertiesWindow_IsActiveChanged;

            var LayoutAnchorableList = MainDock.Layout.Descendents().OfType<LayoutAnchorable>().ToList();
            for (int i = 0; i < LayoutAnchorableList.Count; i++)
            {
                if (LayoutAnchorableList[i].ContentId == ShellPublicVariables.PropertiesWindowContentID)
                {
                    _propertiesWindowDock = LayoutAnchorableList[i];
                    _propertiesWindowDock.IsActiveChanged += PropertiesWindow_IsActiveChanged;
                    if (ProjectNode != null) Project_Click();
                    break;
                }
            }
        }



        /// <summary>
        /// Load AvalondDock layout.
        /// </summary>
        private void LoadLayout(bool loadDefaultResource = false)
        {
            var layoutSerializer = new XmlLayoutSerializer(MainDock);
            layoutSerializer.LayoutSerializationCallback += LayoutSerialization_Callback;
            if (loadDefaultResource == true)
            {
                // Load the Default Resource Avalon Dock layout
                _loadFullLayout = false;
                using (var stream = new MemoryStream(UtilityFunctions.UTF8StringToBytes(Properties.Resources.AvalonDock)))
                {
                    layoutSerializer.Deserialize(stream);
                }
            }
            else if (File.Exists(ShellPublicVariables.DefaultAvalonDockLayoutFilePath))
            {
                if (ProjectNode == null || ProjectNode.Project == null || ProjectNode.Project.AvalonDockLayout == null)
                {
                    try
                    {
                        // Load the Default Avalon Dock layout
                        _loadFullLayout = false;
                        using (var reader = new StreamReader(ShellPublicVariables.DefaultAvalonDockLayoutFilePath))
                        {
                            layoutSerializer.Deserialize(reader);
                        }
                    }
                    catch (Exception)
                    {
                        // Load the Default Resource Avalon Dock layout
                        _loadFullLayout = false;
                        using (var stream = new MemoryStream(UtilityFunctions.UTF8StringToBytes(Properties.Resources.AvalonDock)))
                        {
                            layoutSerializer.Deserialize(stream);
                        }
                    }
                }
                else
                {
                    try
                    {
                        // Load the project specific layout
                        _loadFullLayout = true;
                        using (var stream = new MemoryStream(UtilityFunctions.UTF8StringToBytes(ProjectNode.Project.AvalonDockLayout)))
                        {
                            layoutSerializer.Deserialize(stream);
                        }
                    }
                    catch (Exception currentEx)
                    {
                        // Current layout failed to deserialize. Before falling all the
                        // way back to the hardcoded default (which resets every pane),
                        // try the previous (dual-buffer) layout — this recovers from a
                        // corrupted or partially-written current column.
                        System.Diagnostics.Debug.WriteLine(
                            $"MainWindow: current AvalonDockLayout failed to deserialize ({currentEx.Message}); trying previous buffer.");

                        bool recovered = false;
                        if (!string.IsNullOrEmpty(ProjectNode.Project.AvalonDockLayoutPrevious))
                        {
                            try
                            {
                                _loadFullLayout = true;
                                using (var stream = new MemoryStream(UtilityFunctions.UTF8StringToBytes(ProjectNode.Project.AvalonDockLayoutPrevious)))
                                {
                                    layoutSerializer.Deserialize(stream);
                                }
                                recovered = true;
                                System.Diagnostics.Debug.WriteLine(
                                    "MainWindow: recovered AvalonDockLayout from previous (dual-buffer) column.");
                            }
                            catch (Exception prevEx)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"MainWindow: previous AvalonDockLayout also failed to deserialize ({prevEx.Message}); falling back to embedded default.");
                            }
                        }

                        if (!recovered)
                        {
                            // Last resort: load the hardcoded embedded resource default.
                            _loadFullLayout = false;
                            using (var stream = new MemoryStream(UtilityFunctions.UTF8StringToBytes(Properties.Resources.AvalonDock)))
                            {
                                layoutSerializer.Deserialize(stream);
                            }
                        }
                    }
                }
            }
            else
            {
                // Load the Default Resource Avalon Dock layout
                _loadFullLayout = false;
                using (var stream = new MemoryStream(UtilityFunctions.UTF8StringToBytes(Properties.Resources.AvalonDock)))
                {
                    layoutSerializer.Deserialize(stream);
                }
            }

        }

        /// <summary>
        /// Support function for loading AvalonDock layout
        /// </summary>
        private void LayoutSerialization_Callback(object? sender, LayoutSerializationCallbackEventArgs e)
        {
            // Always load content the main controls: Project Explorer, Message Window, Properties Window
            if (e.Model.ContentId == ShellPublicVariables.ProjectExplorerContentID || e.Model.ContentId == ShellPublicVariables.MessageWindowContentID || e.Model.ContentId == ShellPublicVariables.PropertiesWindowContentID)
            {
                // No action needed; content is already set for these built-in panes.
            }
            else if (_loadFullLayout == true)
            {
                // The user might close the app without saving a form.
                // So get name without asterisk.
                if (string.IsNullOrEmpty(e.Model?.Title))
                {
                    e.Model?.Close();
                    return;
                }
                var splitTitle = e.Model.Title.Split('*');
                string eModelTitle = splitTitle[0];
                e.Model.Title = eModelTitle;
                // 
                // Get the IElement
                var element = ProjectNode.GetElement(e.Model.ContentId, eModelTitle);
                if (element == null)
                {
                    e.Model.Close();
                    return;
                }
                // Get the element document control
                var control = ProjectNode.GetDocumentControl(element);
                if (control == null)
                {
                    e.Model.Close();
                    return;
                }
                // 
                // Set up the layout document.
                e.Content = control;
                BindingOperations.SetBinding(e.Model, LayoutContent.TitleProperty, new Binding(nameof(IElement.DisplayName)) { Source = element });

                // Add appropriate handlers (should mirror DocumentFactory() and OpenDocument() methods.
                e.Model.Closed += (s, ea) =>
                {
                    if (ProjectNode == null) return;
                    ProjectNode.DocumentClosed((UIElement)e.Model.Content);
                };
                // 
                e.Model.IconSource = ResolveElementIcon(element);
                e.Model.IsActiveChanged += Document_IsActiveChanged;
                e.Model.Closing += Document_Closing;
                //
                if (e.Model.IsSelected == true)
                {
                    // Select element node on open
                    if (element != null)
                    {
                        var node = ProjectNode.GetElementNode(element.ParentCollection.Name, element);
                        if (node != null)
                        {
                            node.IsSelected = true;
                            node.ExpandParentNodes();
                        }
                    }
                }

                //
                if (element != null)
                    OpenWindows.Open((LayoutDocument)e.Model, element);
            }
            else
            {
                e.Model.Close();
            }
        }

        /// <summary>
        /// Save AvalonDock layout.
        /// </summary>
        private void SaveLayout()
        {


            // Create the layout directory if it doesn't already exist.
            if (Directory.Exists(ShellPublicVariables.AvalonDockLayoutFolderPath) == false)
            {
                Directory.CreateDirectory(ShellPublicVariables.AvalonDockLayoutFolderPath);
            }
            // 
            // Serialize the Default AvalonDock layout. Only update the project's cached
            // layout if the serialize-and-read round-trip succeeds. Previously a mid-write
            // failure (disk full, permissions) left a partial/corrupted file on disk which
            // the next ReadAllBytes would happily persist as the new project layout,
            // silently corrupting future sessions.
            var layoutSerializer = new XmlLayoutSerializer(MainDock);
            try
            {
                layoutSerializer.Serialize(ShellPublicVariables.DefaultAvalonDockLayoutFilePath);
                ProjectNode.Project.AvalonDockLayout = UtilityFunctions.UTF8BytesToString(File.ReadAllBytes(ShellPublicVariables.DefaultAvalonDockLayoutFilePath));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                FrameworkInterfaces.Messaging.Messenger.GetInstance().Add(
                    new BasicMessageItem(MessageType.Warning,
                        $"Failed to save dock layout: {ex.Message}",
                        this, "MainWindow", string.Empty, "SaveLayout"));
            }
        }

        #endregion

        #region Project Explorer

        /// <summary>
        /// When project node is clicked, then show project properties.
        /// </summary>
        private void Project_Click()
        {
            var projectProperties = new ProjectPropertiesControl() { Project = ProjectNode.Project };
            OpenProperties(projectProperties);
        }

        /// <summary>
        /// When an element node is added to the project explorer tree, add edit and left click handler.
        /// Opens the document for the new node unless the project is currently opening.
        /// </summary>
        /// <param name="node">Project explorer element node.</param>
        private void NodeAdded(Node node)
        {
            if(node==null || node.GetType()!=typeof(ElementNode)) { return; }
            var elementNode = (ElementNode)node;
            elementNode.Edit += EditElement;
            elementNode.Activate += ActivateElement;
            elementNode.Copy += CopyElement_Click;
            elementNode.Delete += DeleteElement;
            elementNode.NodeMoved += NodeMoved;

            // Open the document for a node whenever it is added unless the project is opening.
            if (_openingProject == false)
            {
                OpenDocument(DocumentFactory(elementNode.Element), elementNode.Element);

                // Capture the current tree layout and persist immediately so a crash before
                // the next explicit save doesn't lose the user's structural edit.
                PersistProjectExplorerLayout();

                // Log Event
                FrameworkInterfaces.Messaging.Messenger.GetInstance().Add(new BasicMessageItem(MessageType.Event, $"The {elementNode.Element.ParentCollection.Name} '{elementNode.Element.Name}' was created.", ProjectNode.Project, "Project", ProjectNode.Project.Name, "ElementAdded"));
            }
        }

        /// <summary>
        /// When an element node is removed from the project explorer tree, remove handlers and update layout.
        /// </summary>
        /// <param name="node">Project explorer element node.</param>
        private void NodeRemoved(Node node)
        {
            if (node == null || node.GetType() != typeof(ElementNode)) { return; }
            var elementNode = (ElementNode)node;
            elementNode.Edit -= EditElement;
            elementNode.Activate -= ActivateElement;
            elementNode.Copy -= CopyElement_Click;
            elementNode.Delete -= DeleteElement;
            elementNode.NodeMoved -= NodeMoved;
            elementNode.ParentTreeView = null!;

            if (_openingProject == false)
            {
                // Capture the current tree layout and persist immediately — see PersistProjectExplorerLayout.
                PersistProjectExplorerLayout();
            }

        }

        /// <summary>
        /// When an element node is moved, update project explorer layout.
        /// </summary>
        /// <param name="node">Node that moved.</param>
        private void NodeMoved(Node node)
        {
            if (_openingProject) return;
            // Capture the current tree layout and persist immediately — see PersistProjectExplorerLayout.
            PersistProjectExplorerLayout();
        }

        /// <summary>
        /// When an element node is sorted, update project explorer layout.
        /// </summary>
        /// <param name="node">The node that was sorted.</param>
        private void NodeSorted(Node node)
        {
            if (_openingProject) return;
            // Capture the current tree layout and persist immediately — see PersistProjectExplorerLayout.
            PersistProjectExplorerLayout();
        }

        /// <summary>
        /// Captures the current Project Explorer tree state into
        /// <see cref="IProject.ProjectExplorerLayout"/> and triggers an immediate
        /// layout-only save so the change survives an unexpected crash.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Structural edits to the tree (add / remove / move / sort / group) previously
        /// only flipped the in-memory layout string and waited for an explicit
        /// <c>Save</c> or project close to persist. A crash in that window lost the
        /// user's groupings and ordering.
        /// </para>
        /// <para>
        /// The layout-only save path in <c>BestFitProject.Save()</c> writes just the
        /// two layout columns, does not stamp <c>LastModified</c>, and does not iterate
        /// element collections, so the cost of calling it on every structural edit is
        /// small (one SQLite open + two cell edits). Expansion / collapse state is
        /// intentionally NOT covered here — it is captured at the next real save.
        /// </para>
        /// <para>
        /// Errors are swallowed: a failure to persist the layout must not abort the
        /// user's current action. The next successful save will catch up.
        /// </para>
        /// </remarks>
        private void PersistProjectExplorerLayout()
        {
            if (ProjectNode == null || ProjectNode.Project == null) return;

            try
            {
                ProjectNode.Project.ProjectExplorerLayout = ProjectNode.SaveToXElement().ToString();
                if (ProjectNode.Project.LayoutDirty)
                    ProjectNode.Project.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"MainWindow.PersistProjectExplorerLayout: failed to persist layout - {ex.Message}");
            }
        }

        /// <summary>
        /// Check if the element is open for editing and activate its window if it is.
        /// </summary>
        /// <param name="element">Project element.</param>
        private void ActivateElement(IElement element)
        {
            int index = OpenWindows.WindowIndexOf(element);
            if (index < 0) return;
            OpenWindows.Activate(index);
        }

        /// <summary>
        /// On click, open the project element as a document.
        /// </summary>
        /// <param name="elements">List of project elements.</param>
        private void EditElement(IList<IElement> elements)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            for (int i = 0; i < elements.Count; i++)
            {
                int index = OpenWindows.WindowIndexOf(elements[i]);
                if (index < 0)
                {
                    OpenDocument(DocumentFactory(elements[i]), elements[i]);
                }
                else
                {
                    OpenWindows.Activate(index);
                }
            }
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// On click, copy project element.
        /// </summary>
        /// <param name="element">Project element.</param>
        private void CopyElement_Click(IElement element)
        {
            var existingElementNames = element.ParentCollection.Select(x => x.Name.ToString()).ToArray();
            var nameDialog = new NameDialog(50, "", false, existingElementNames, NameTextBox.GetDefaultInvalidCharacters())
            {
                Icon = Application.Current.FindResource("CopyImage") as ImageSource,
                Title = "Copy " + element.ParentCollection.Name + "...",
                Owner = GetWindow(this),
                Background = (Brush)FindResource("EnvironmentWindowBackground"),
                Foreground = (Brush)FindResource("EnvironmentWindowText"),
                Text = element.Name + "_copy"
            };
            if (nameDialog.ShowDialog() == true)
            {
                CopyElement(element, nameDialog.Text);
            }
        }

        /// <summary>
        /// Copy project element with a new name and add it to the parent collection.
        /// </summary>
        /// <param name="element">Project element to copy.</param>
        /// <param name="newName">New element name.</param>
        private void CopyElement(IElement element, string newName)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var newElement = element.Copy(newName);
            element.ParentCollection.Add(newElement);
            // Log Event
            FrameworkInterfaces.Messaging.Messenger.GetInstance().Add(new BasicMessageItem(MessageType.Event, $"The {element.ParentCollection.Name} '{element.Name}' was copied as '{newName}'.", ProjectNode.Project, "Project", ProjectNode.Project.Name, nameof(CopyElement)));
            // 
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// On click, delete project element after confirming with user.
        /// Closes any open documents for the deleted elements.
        /// </summary>
        /// <param name="elements">List of project elements to delete.</param>
        private void DeleteElement(IList<IElement> elements)
        {
            // Check if the user really wants to delete the element
            if (GenericControls.MessageBox.Show("Are you sure you would like to delete the selected project elements? This action is permanent.", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                for (int i = 0; i < elements.Count; i++)
                {
                    // Delete element
                    elements[i].Delete();
                    // 
                    // Check if the element is opened as a document. If it is, close the document.
                    int index = OpenWindows.WindowIndexOf(elements[i]);
                    if (index >= 0) OpenWindows.Close(index);

                    // Log Event
                    FrameworkInterfaces.Messaging.Messenger.GetInstance().Add(new BasicMessageItem(MessageType.Event, $"The {elements[i].ParentCollection.Name} '{elements[i].Name}' was deleted.", ProjectNode.Project, "Project", ProjectNode.Project.Name, nameof(DeleteElement)));
                }
            }
        }

        #endregion

        #region Properties Window

        /// <summary>
        /// Open UI element in properties window.
        /// </summary>
        /// <param name="control">UI element to show.</param>
        private void OpenProperties(UIElement control)
        {
            if (_propertiesWindowDock == null) return;
            if (_propertiesWindowDock.Content != null && _propertiesWindowDock.Content.Equals(control) == false)
            {
                BindingOperations.ClearAllBindings((UIElement)_propertiesWindowDock.Content);
                ProjectNode.PropertiesClosed((UIElement)_propertiesWindowDock.Content);
            }
            _propertiesWindowDock.Content = control;
        }

        /// <summary>
        /// Used to handle cases when the project requests to set properties in the properties pane. Currently this is only called when plot properties are clicked but could extend to other things.
        /// </summary>
        /// <param name="propertyControl">The property control to be set in the properties pane.</param>
        private void SetPropertiesRequested(UIElement propertyControl)
        {
            OpenProperties(propertyControl);
            if (_propertiesWindowDock != null)
                _propertiesWindowDock.IsActive = true;
        }

        /// <summary>
        /// Used to handle cases when the project requests to close/remove the properties control in the properties pane. Currently this is only called when plot properties back button is clicked but could extend to other things.
        /// </summary>
        /// <param name="propertyControl">The property control to be removed from the properties pane.</param>
        private void ClosePropertiesRequested(UIElement propertyControl)
        {
            if (_previousActiveDocument != null)
            {
                // Get the property control for the previous document.
                var propControl = ProjectNode.GetPropertiesControl((UIElement)_previousActiveDocument.Content);
                SetPropertiesRequested(propControl);
            }
            else
            {
                // if no previous document was set then default to the project properties.
                Project_Click();
            }
        }

        /// <summary>
        /// Handles the IsActiveChanged event for the properties window to update project explorer selection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void PropertiesWindow_IsActiveChanged(object? sender, EventArgs e)
        {
            // Check if a simulation is in progress
            if (ShellPublicVariables.SimulationInProgress == true) return;
            if (_openingProject || _closingProject) return;
            if (_previousActiveDocument == null) return;

            // Update Project Explorer Selection
            if (_previousActiveDocument.IsActive == false && _previousActiveDocument.IsSelected == true && _propertiesPaneClicked == true && _projectExplorerPaneClicked == false)
            {
                // User did not activate the document from the project explorer, so clear any multi-select
                if (ProjectNode.ParentTreeView?.SelectedNodes.Count > 0) { ProjectNode.ParentTreeView.ClearSelection(); }

                // Select node
                var prevDocumentElement = ProjectNode.GetControlElement((UIElement)_previousActiveDocument.Content);
                if (prevDocumentElement != null)
                {
                    var prevNode = ProjectNode.GetElementNode(prevDocumentElement.ParentCollection.Name, prevDocumentElement);
                    if (prevNode != null)
                    {
                        prevNode.IsSelected = true;
                        prevNode.ExpandParentNodes();
                    }
                }

            }
        }

        #endregion

        #region Documents

        /// <summary>
        /// Document factory.
        /// </summary>
        /// <param name="element">Project element.</param>
        private LayoutDocument? DocumentFactory(IElement element)
        {
            var document = new LayoutDocument() { IconSource = ResolveElementIcon(element) };
            var documentControl = ProjectNode.GetDocumentControl(element);
            if (documentControl == null) { return null; }
            document.Content = documentControl;
            document.ContentId = element.ParentCollection.Name;
            BindingOperations.SetBinding(document, LayoutContent.TitleProperty, new Binding(nameof(IElement.DisplayName)) { Source = element });
            // 
            document.Closed += (sender, e) =>
            {
                if (ProjectNode == null) return;
                if (sender == null || sender.GetType() != typeof(LayoutDocument)) return;
                LayoutDocument doc = (LayoutDocument)sender;
                doc.IsActiveChanged -= Document_IsActiveChanged;
                doc.IsSelectedChanged -= Document_IsSelectedChanged;
                doc.Closing -= Document_Closing;
                doc.Closed -= Document_Closed;
                if (doc.Content == null) return;
                ProjectNode.DocumentClosed((UIElement)doc.Content);
            };
            return document;
        }

        /// <summary>
        /// Open a document in the main document pane and makes it active.
        /// </summary>
        /// <param name="document">Document to open.</param>
        /// <param name="element">The element associated with the document.</param>
        public void OpenDocument(LayoutDocument? document, IElement element)
        {
            if (ShellPublicVariables.SimulationInProgress == true) return;
            if (document == null) { return; }
            document.IsActiveChanged += Document_IsActiveChanged;
            document.IsSelectedChanged += Document_IsSelectedChanged;
            document.Closing += Document_Closing;
            document.Closed += Document_Closed;
            OpenWindows.Open(document, element);
            // Open document and make active
            var layoutDocumentPane = MainDock.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
            if (layoutDocumentPane == null) return;
            layoutDocumentPane.Children.Add(document);
            // Select document
            _propertiesPaneClicked = false;
            _projectExplorerPaneClicked = false;
            document.IsSelected = true;
        }

        /// <summary>
        /// Handles the IsSelectedChanged event for a document to enable/disable based on simulation status.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Document_IsSelectedChanged(object? sender, EventArgs e)
        {
            if (sender == null) return;
            LayoutDocument document = (LayoutDocument)sender;
            if (ShellPublicVariables.SimulationInProgress == true)
            {
                document.IsEnabled = false;
                if (document.Content != null) ((UIElement)document.Content).IsEnabled = false;
            }
            else
            {
                document.IsEnabled = true;
                if (document.Content != null) ((UIElement)document.Content).IsEnabled = true;
            }
        }

        /// <summary>
        /// If the document is changed to active, show content element properties and update project explorer selection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Document_IsActiveChanged(object? sender, EventArgs e)
        {
            if (sender == null) return;
            LayoutDocument document = (LayoutDocument)sender;
            if (document.Content == null) return;

            // Check if a simulation is in progress
            if (ShellPublicVariables.SimulationInProgress == true)
            {
                document.IsEnabled = false;
                ((UIElement)document.Content).IsEnabled = false;
                return;
            }
            else
            {
                document.IsEnabled = true;
                ((UIElement)document.Content).IsEnabled = true;
            }

            if (_previousActiveDocument == null) _previousActiveDocument = document;

            // Update Project Explorer Selection
            if (document.IsActive == true && document.IsSelected == true && _propertiesPaneClicked == false && _projectExplorerPaneClicked == false &&
                    _openingProject == false && _closingProject == false)
            {
                // User did not activate the document from the project explorer, so clear any multi-select
                if (ProjectNode.ParentTreeView?.SelectedNodes.Count > 0) { ProjectNode.ParentTreeView.ClearSelection(); }

                bool isPreviousDocument = document.Equals(_previousActiveDocument);
                if (isPreviousDocument == false)
                {
                    // Deselect old node
                    var prevDocumentElement = ProjectNode.GetControlElement((UIElement)_previousActiveDocument.Content);
                    if (prevDocumentElement != null)
                    {
                        var prevNode = ProjectNode.GetElementNode(prevDocumentElement.ParentCollection.Name, prevDocumentElement);
                        if (prevNode != null)
                            prevNode.IsSelected = false;
                    }

                    // Select new node
                    var documentElement = ProjectNode.GetControlElement((UIElement)document.Content);
                    if (documentElement != null)
                    {
                        var node = ProjectNode.GetElementNode(documentElement.ParentCollection.Name, documentElement);
                        if (node != null)
                        {
                            node.IsSelected = true;
                            node.ExpandParentNodes();
                        }

                    }
                }
                else
                {
                    // Reselect old node
                    var prevDocumentElement = ProjectNode.GetControlElement((UIElement)_previousActiveDocument.Content);
                    if (prevDocumentElement != null)
                    {
                        var prevNode = ProjectNode.GetElementNode(prevDocumentElement.ParentCollection.Name, prevDocumentElement);
                        if (prevNode != null)
                        {
                            prevNode.IsSelected = true;
                            prevNode.ExpandParentNodes();
                        }
                    }
                }
            }

            // Update properties window
            if (document.IsActive == true && _propertiesPaneClicked == false)
            {
                bool isPreviousDocument = document.Equals(_previousActiveDocument);

                if (_propertiesWindowDock != null && _propertiesWindowDock.Content != null)
                {
                    var propertyElement = ProjectNode.GetControlElement((UIElement)_propertiesWindowDock.Content);
                    var documentElement = ProjectNode.GetControlElement((UIElement)document.Content);
                    if (isPreviousDocument && propertyElement != null && propertyElement.Equals(documentElement))
                    {
                        return;
                    }

                }

                var propControl = ProjectNode.GetPropertiesControl((UIElement)document.Content);
                if (propControl == null) { return; }

                OpenProperties(propControl);
                _previousActiveDocument = document;

            }

        }

        /// <summary>
        /// On closing, see if the project properties should be selected. Cancels close if simulation is in progress.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The cancel event data.</param>
        private void Document_Closing(object? sender, CancelEventArgs e)
        {
            if (ShellPublicVariables.SimulationInProgress == true)
            {
                e.Cancel = true;
                return;
            }
            var layoutDocumentPane = MainDock.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
            if (layoutDocumentPane != null && layoutDocumentPane.Children.Count <= 1) Project_Click();
        }

        /// <summary>
        /// Handles the Closed event for a document to clean up resources and event handlers.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Document_Closed(object? sender, EventArgs e)
        {
            if (sender == null) return;
            LayoutDocument document = (LayoutDocument)sender;
            document.Content = null!;
            document.IsActiveChanged -= Document_IsActiveChanged;
            document.IsSelectedChanged -= Document_IsSelectedChanged;
            document.Closing -= Document_Closing;
            document.Closed -= Document_Closed;
        }


        /// <summary>
        /// On mouse move, determine if the user's mouse is over an AvalonDock pane control. 
        /// </summary>
        private void Me_MouseMove(object sender, MouseEventArgs e)
        {
            _projectExplorerPaneClicked = false;
            _propertiesPaneClicked = false;
            var pane = FindPane(e.OriginalSource as DependencyObject) as Xceed.Wpf.AvalonDock.Controls.LayoutAnchorablePaneControl;
            if (pane != null && (pane.Items.CurrentItem as LayoutAnchorable != null))
            {
                if (((LayoutAnchorable)pane.Items.CurrentItem).ContentId == PropertiesWindowLayout.ContentId)
                {
                    _propertiesPaneClicked = true;
                }
                if (((LayoutAnchorable)pane.Items.CurrentItem).ContentId == ProjectExplorerLayout.ContentId)
                {
                    _projectExplorerPaneClicked = true;
                }
            }
        }


        /// <summary>
        /// Helper method to search for AvalonDock LayoutAnchorablePaneControl in the visual tree.
        /// </summary>
        /// <param name="dependencyObject">The starting point in the visual tree.</param>
        /// <returns>The LayoutAnchorablePaneControl if found, otherwise null.</returns>
        private Xceed.Wpf.AvalonDock.Controls.LayoutAnchorablePaneControl? FindPane(DependencyObject? dependencyObject)
        {
            if (dependencyObject == null) return null;
            if (!(dependencyObject is Visual || dependencyObject is Visual3D)) return null;
            Xceed.Wpf.AvalonDock.Controls.LayoutAnchorablePaneControl? item = dependencyObject as Xceed.Wpf.AvalonDock.Controls.LayoutAnchorablePaneControl;
            if (item != null) return item;
            return FindPane(VisualTreeHelper.GetParent(dependencyObject));
        }

        #endregion

        #region Drag-Drop

        /// <summary>
        /// On drag enter, check if file type is acceptable for opening.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The drag event data.</param>
        private void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            ShellPublicVariables.IsDroppableFile = false;
            // Get files being dropped
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;
            // Check if the user is trying to drag in more than one file
            if (files.Length > 1)
            {
                ShellPublicVariables.IsDroppableFile = false;
                e.Effects = DragDropEffects.None;
                return;
            }
            // Get filename and open project and check if the file extension is acceptable
            string fullFileName = files[0].ToString();
            string ext = fullFileName.Substring(fullFileName.Length - ShellPublicVariables.SoftwareExtension.Length);
            if (ext == ShellPublicVariables.SoftwareExtension)
            {
                ShellPublicVariables.IsDroppableFile = true;
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                ShellPublicVariables.IsDroppableFile = false;
                e.Effects = DragDropEffects.None;
            }
        }

        /// <summary>
        /// On drag over, show appropriate drag-drop cursor icons.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The drag event data.</param>
        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            if (ShellPublicVariables.IsDroppableFile == true)
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        /// <summary>
        /// On drop, if the file is acceptable, open the project file.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The drag event data.</param>
        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (ShellPublicVariables.IsDroppableFile == true)
            {
                // Get filename 
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null || files.Length == 0) return;
                string fullFileName = files[0].ToString();
                // Open project
                OpenRecentProject(fullFileName);
            }
        }

        #endregion

        #region File Menu

        /// <summary>
        /// On click, create new project via save file dialog.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void NewCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var SaveFileDialog = new SaveFileDialog() { Title = "Create New Project", Filter = ShellPublicVariables.FileDialogFilter };
            if (Directory.Exists(UserSettings.DefaultLocation)) SaveFileDialog.InitialDirectory = UserSettings.DefaultLocation;
            if (SaveFileDialog.ShowDialog() == true)
            {
                UserSettings.DefaultLocation = Path.GetDirectoryName(SaveFileDialog.FileName) ?? string.Empty;
                CloseProject();
                ProjectNode.Project.CreateNew(SaveFileDialog.FileName);
                OpenProject(SaveFileDialog.FileName);
            }
        }

        /// <summary>
        /// On click, open existing project via open file dialog.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var OpenFileDialog = new OpenFileDialog() { Title = "Open Project", Filter = ShellPublicVariables.FileDialogFilter };
            if (Directory.Exists(UserSettings.DefaultLocation)) OpenFileDialog.InitialDirectory = UserSettings.DefaultLocation;
            if (OpenFileDialog.ShowDialog() == true)
            {
                UserSettings.DefaultLocation = Path.GetDirectoryName(OpenFileDialog.FileName) ?? string.Empty;
                OpenRecentProject(OpenFileDialog.FileName);
            }
        }

        /// <summary>
        /// Open recent project.
        /// </summary>
        /// <param name="fullFileName">The full name of the project to open.</param>
        /// <param name="killProcessIfAlreadyOpen">Determines whether to send a message that the project is already open, or to kill the current process.</param>
        public void OpenRecentProject(string fullFileName, bool killProcessIfAlreadyOpen = false)
        {
            // If the project is already open in this application, then exit
            if (ProjectNode.Project.FullFileName == fullFileName) return;
            // Next, see if this project file is already open by another instance of this application
            var current = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(current.ProcessName);
            foreach (Process process in processes)
            {
                if (process.Id != current.Id)
                {
                    if (process.MainWindowTitle == ShellPublicVariables.SoftwareName + " " + ProjectNode.Project.SoftwareVersion + "  -  " + fullFileName + "")
                    {
                        // There is already an instance with this project opened, so send a message or kill the current process.
                        if (killProcessIfAlreadyOpen == true)
                        {
                            current.Kill();
                        }
                        else
                        {
                            GenericControls.MessageBox.Show(Path.GetFileNameWithoutExtension(fullFileName) + " is already open in another instance of " + ShellPublicVariables.SoftwareName);
                            return;
                        }
                    }
                }
            }
            CloseProject();
            OpenProject(fullFileName);
        }

        /// <summary>
        /// Open project file.
        /// </summary>
        /// <param name="fullFileName">The full name of the project to open.</param>
        public void OpenProject(string fullFileName)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            _openingProject = true;
            // Delete the backup file
            AutoBackup.DeleteBackupProjectFile();

            // Add recent file to list
            RecentFiles.AddItem(fullFileName);

            // Clear message window
            FrameworkInterfaces.Messaging.Messenger.GetInstance().Clear();

            // Close and Open project
            ProjectNode.Project.Close();
            ProjectNode.Project.FullFileName = fullFileName;
            ProjectNode.Project.Open();
            LoadProjectNode();
            
            // Load window layout
            if (UserSettings.SaveWindowLayout == true)
            {
                LoadLayout();
            }

            BuildProjectExplorer();
            BuildMessageWindow();
            BuildPropertiesWindow();

            // Cancel and Start AutoBackup
            AutoBackup.Cancel();
            if (UserSettings.CreateAutoRecoverBackup == true)
            {
                AutoBackup.Start();
            }

            // Log Event
            FrameworkInterfaces.Messaging.Messenger.GetInstance().Add(new BasicMessageItem(MessageType.Event, $"The project '{ProjectNode.Project.Name}' was opened.", ProjectNode.Project, "Project", ProjectNode.Project.Name, nameof(OpenProject)));

            _openingProject = false;

            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// Close the current project, saving layout and closing all open windows.
        /// </summary>
        private void CloseProject()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _closingProject = true;

            // Save window layout
            if (UserSettings.SaveWindowLayout == true)
            {
                SaveLayout();
            }

            // Clear open windows
            // Closing the window triggers the unload event. 
            // Save any chart settings or other non-data related settings that we wish to persist.

            OpenWindows.CloseAllWindows();
            if (OpenWindows.CancelClosing == true)
            {
                _closingProject = false;
                Mouse.OverrideCursor = null;
                return;
            }

            // Dispose of previous active document
            if (_previousActiveDocument != null && _previousActiveDocument.Content != null)
            {
                _previousActiveDocument.Content = null!;
                _previousActiveDocument = null;
            }

            // Dispose of properties window document
            if (_propertiesWindowDock != null)
                ProjectNode.PropertiesClosed((Control)_propertiesWindowDock.Content);

            // Save & close project.
            // Capturing the project-explorer layout may flip LayoutDirty if the window
            // arrangement has changed since last save. Only call Save() if something
            // actually needs to be persisted — a clean close should not stamp LastModified.
            ProjectNode.Project.ProjectExplorerLayout = ProjectNode.SaveToXElement().ToString();
            if (ProjectNode.Project.IsDirty || ProjectNode.Project.LayoutDirty)
                ProjectNode.Project.Save();
            ProjectNode.Project.Close();

            _closingProject = false;
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// On click, save project to current file location.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void SaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveProject();
        }

        /// <summary>
        /// Save project.
        /// </summary>
        public void SaveProject()
        {
            // If this is a temporary project, then force a Save As...
            if (ProjectNode.Project.FullFileName.Contains(Path.GetTempPath()) == true)
            {
                SaveProjectAs();
                return;
            }
            // Save project
            Mouse.OverrideCursor = Cursors.Wait;
            // Save window layout
            if (UserSettings.SaveWindowLayout == true)
            {
                SaveLayout();
            }
            // Capturing the project-explorer layout may flip LayoutDirty if the window
            // arrangement has changed since last save. Skip the full save pipeline when
            // nothing is dirty — Ctrl+S on a clean project is a no-op so that
            // LastModified is not stamped for an edit-free session.
            ProjectNode.Project.ProjectExplorerLayout = ProjectNode.SaveToXElement().ToString();
            if (ProjectNode.Project.IsDirty || ProjectNode.Project.LayoutDirty)
            {
                ProjectNode.Project.Save();
                // Update recent file name
                if (RecentFiles.Collection.Count > 0)
                {
                    if (RecentFiles.Collection[0].FilePath != ProjectNode.Project.FullFileName)
                    {
                        RecentFiles.Collection[0].FilePath = ProjectNode.Project.FullFileName;
                    }
                }
                // Log Event
                FrameworkInterfaces.Messaging.Messenger.GetInstance().Add(new BasicMessageItem(MessageType.Event, $"The project '{ProjectNode.Project.Name}' was saved.", ProjectNode.Project, "Project", ProjectNode.Project.Name, nameof(SaveProject)));
            }

            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// On click, save project as a new file via save file dialog.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void SaveAsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveProjectAs();
        }

        /// <summary>
        /// Save project as.
        /// </summary>
        public void SaveProjectAs()
        {
            var SaveFileDialog = new SaveFileDialog() { Title = "Save Project As...", Filter = ShellPublicVariables.FileDialogFilter };
            if (Directory.Exists(UserSettings.DefaultLocation)) SaveFileDialog.InitialDirectory = UserSettings.DefaultLocation;
            if (SaveFileDialog.ShowDialog() == true)
            {
                bool cancelSaveAs = false;
                PreviewSaveAs?.Invoke(ProjectNode.Project, SaveFileDialog.FileName, ref cancelSaveAs);
                if (cancelSaveAs == true) { return; }
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    // Save project as
                    UserSettings.DefaultLocation = Path.GetDirectoryName(SaveFileDialog.FileName) ?? string.Empty;
                    // Delete the backup file
                    AutoBackup.DeleteBackupProjectFile();
                    // Save As and change name
                    ProjectNode.Project.ProjectExplorerLayout = ProjectNode.SaveToXElement().ToString();
                    ProjectNode.Project.Save();
                    ProjectNode.Project.SaveAs(SaveFileDialog.FileName);
                    ProjectNode.Project.FullFileName = SaveFileDialog.FileName;
                    ProjectNode.Project.Name = Path.GetFileNameWithoutExtension(SaveFileDialog.FileName);
                    ProjectNode.Project.Save();
                    // Add recent file to list
                    RecentFiles.AddItem(SaveFileDialog.FileName);
                    // Cancel and Start AutoBackup
                    AutoBackup.Cancel();
                    if (UserSettings.CreateAutoRecoverBackup == true)
                    {
                        AutoBackup.Start();
                    }
                    // Log Event
                    FrameworkInterfaces.Messaging.Messenger.GetInstance().Add(new BasicMessageItem(MessageType.Event, $"The project was saved as '{ProjectNode.Project.Name}'.", ProjectNode.Project, "Project", ProjectNode.Project.Name, nameof(SaveProjectAs)));

                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        /// <summary>
        /// Exit program by closing the main window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ExitCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// On close, check for unsaved changes, compact project if needed, and save settings.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The cancel event data.</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (ShellPublicVariables.SimulationInProgress || ShellPublicVariables.CompactionInProgress)
            {
                e.Cancel = true;
                return;
            }

            if (!TryPromptToSaveDirtyState(out bool userSavedBeforeClosing))
            {
                e.Cancel = true;
                return;
            }



            // Compact on close
            if (UserSettings.CompressProjectFileOnClose == true && ProjectNode?.Project is not null)
            {
                // Compact and Optimize
                try
                {
                    FileSizeManager.CompactAndOptimizeFile(ProjectNode.Project);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    GenericControls.MessageBox.Show("The project file could not be compacted and optimized. " + ex.Message,
                        "Compact & Optimize Project Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (ShellPublicVariables.CompactionInProgress)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            // Close the current project
            CloseProject();

            // Delete backup file
            if (UserSettings.KeepLastBackupVersion == true && userSavedBeforeClosing == true)
            {
                // Delete backup file
                AutoBackup.DeleteBackupProjectFile();
            }
            else
            {
                // Delete backup file
                AutoBackup.DeleteBackupProjectFile();
            }

            // Save user settings — guard against disk-full / permission / locked-file errors at shutdown
            // so a transient failure here doesn't crash the close (loses unsaved Recent Files updates,
            // but the project itself was already saved above).
            try { UserSettings.Save(ShellPublicVariables.UserSettingsFilePath); }
            catch (Exception ex) { Debug.WriteLine($"[Closing] UserSettings.Save failed: {ex.Message}"); }

            // Save Recent Files — same shutdown-resilience rationale.
            try { RecentFiles.SaveToXML(); }
            catch (Exception ex) { Debug.WriteLine($"[Closing] RecentFiles.SaveToXML failed: {ex.Message}"); }
        }

        /// <summary>
        /// Prompts the user to save unsaved project state (elements and/or project file).
        /// Used by both the application-close flow and the auto-update install flow so
        /// dirty work is never silently lost when the process exits.
        /// </summary>
        /// <param name="userSaved">
        /// Set to <c>true</c> if the user chose Save (project was saved), <c>false</c> if
        /// they chose Don't Save or no save was needed.
        /// </param>
        /// <returns>
        /// <c>true</c> if the caller should proceed (user saved or chose not to save, or
        /// nothing was dirty); <c>false</c> if the user cancelled.
        /// </returns>
        private bool TryPromptToSaveDirtyState(out bool userSaved)
        {
            userSaved = false;

            // See if there are any unsaved elements. Ask if they want to save.
            var unSavedElementItems = new ObservableCollection<UnsavedElement>();
            if (ProjectNode?.Project?.ElementCollections != null)
            {
                for (int i = 0; i < ProjectNode.Project.ElementCollections.Count; i++)
                {
                    for (int j = 0; j < ProjectNode.Project.ElementCollections[i].Count; j++)
                    {
                        if (ProjectNode.Project.ElementCollections[i].ElementAt(j).IsDirty == true)
                        {
                            unSavedElementItems.Add(new UnsavedElement(ProjectNode.Project.ElementCollections[i].ElementAt(j)));
                        }
                    }
                }
            }
            // If there is, open the Save Window Dialog
            if (unSavedElementItems.Count > 0)
            {
                var saveElementsDialog = new SaveElementsDialog() { ElementItems = unSavedElementItems };
                saveElementsDialog.ShowDialog();
                if (saveElementsDialog.Result == SaveElementsDialog.DialogResultType.YesSave)
                {
                    userSaved = true;
                    SaveProject();
                }
                else if (saveElementsDialog.Result == SaveElementsDialog.DialogResultType.NoSave)
                {
                    userSaved = false;
                }
                else if (saveElementsDialog.Result == SaveElementsDialog.DialogResultType.Cancel)
                {
                    return false;
                }
            }
            else
            {
                // Check to see if the Project or Element collections need to be saved
                bool isDirty = false;
                if (ProjectNode?.Project is not null && ProjectNode.Project.IsDirty == true)
                {
                    isDirty = true;
                }
                else if (ProjectNode?.Project is { ElementCollections: { } elementCollections })
                {
                    for (int i = 0; i < elementCollections.Count; i++)
                    {
                        if (elementCollections[i].IsDirty == true)
                        {
                            isDirty = true;
                            break;
                        }
                    }
                }

                if (isDirty == true)
                {
                    MessageBoxResult result = GenericControls.MessageBox.Show("The project has unsaved changes. Would you like to save before closing?", "Save Changes",
                                                                MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        userSaved = true;
                        SaveProject();
                    }
                    if (result == MessageBoxResult.No)
                    {
                        userSaved = false;
                    }
                    if (result == MessageBoxResult.Cancel)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// On close, shutdown application completely.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            MouseMove -= Me_MouseMove;
            var messenger = FrameworkInterfaces.Messaging.Messenger.GetInstance();
            messenger.MessagesAdded -= ProjectMessageAdded;
            FileSizeManager.ReportProgress -= FileSizeManager_ReportProgress;
            AutoBackup.ReportProgress -= AutoBackup_ReportProgress;
            ThemeManager.ThemeChanged -= ThemeChanged;
            Application.Current.Shutdown();
        }

        #endregion

        #region Edit Menu

        /// <summary>
        /// Gets the undo manager for the currently active document or element.
        /// </summary>
        /// <returns>The active undo manager, or null if none is available.</returns>
        private IUndoManager? GetActiveUndoManager()
        {
            if (ProjectNode == null) return null;

            // First, try to get the undo manager from the active document
            if (_previousActiveDocument?.Content != null)
            {
                var element = ProjectNode.GetControlElement((UIElement)_previousActiveDocument.Content);
                if (element is IUndoableElement undoableElement)
                {
                    return undoableElement.UndoManager;
                }
            }

            // Fall back to project-level undo manager if available
            return null;
        }

        /// <summary>
        /// Determines if the Undo command can execute based on the active undo manager.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void UndoCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var undoManager = GetActiveUndoManager();
            e.CanExecute = undoManager?.CanUndo == true;

            // Update dropdown button states when command state is checked
            UpdateUndoRedoButtonStates();
        }

        /// <summary>
        /// Executes the Undo command on the active undo manager.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void UndoCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var undoManager = GetActiveUndoManager();
            if (undoManager?.CanUndo == true)
            {
                undoManager.Undo();
            }
        }

        /// <summary>
        /// Determines if the Redo command can execute based on the active undo manager.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void RedoCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var undoManager = GetActiveUndoManager();
            e.CanExecute = undoManager?.CanRedo == true;
        }

        /// <summary>
        /// Executes the Redo command on the active undo manager.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void RedoCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var undoManager = GetActiveUndoManager();
            if (undoManager?.CanRedo == true)
            {
                undoManager.Redo();
            }
        }

        /// <summary>
        /// Updates the visibility of the Undo/Redo toolbar buttons based on user settings.
        /// </summary>
        public void UpdateUndoRedoButtonVisibility()
        {
            UndoRedoPanel.Visibility = UserSettings.ShowUndoRedoButtons ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Updates the enabled state and appearance of the undo/redo dropdown buttons
        /// based on the current undo manager state.
        /// </summary>
        private void UpdateUndoRedoButtonStates()
        {
            // Guard against calls during initialization before controls are loaded
            if (UndoDropdownButton == null || UndoDropdownArrow == null ||
                RedoDropdownButton == null || RedoDropdownArrow == null)
            {
                return;
            }

            var undoManager = GetActiveUndoManager();
            bool canUndo = undoManager?.CanUndo == true;
            bool canRedo = undoManager?.CanRedo == true;

            // Update Undo dropdown button
            UndoDropdownButton.IsEnabled = canUndo;
            UndoDropdownArrow.Fill = canUndo
                ? (System.Windows.Media.Brush)FindResource("ToolbarIconForeground")
                : (System.Windows.Media.Brush)FindResource("ToolbarIconDisabledForeground");

            // Update Redo dropdown button
            RedoDropdownButton.IsEnabled = canRedo;
            RedoDropdownArrow.Fill = canRedo
                ? (System.Windows.Media.Brush)FindResource("ToolbarIconForeground")
                : (System.Windows.Media.Brush)FindResource("ToolbarIconDisabledForeground");
        }

        /// <summary>
        /// Handles the click event for the undo dropdown button.
        /// Shows a popup with the list of undoable actions.
        /// </summary>
        private void UndoDropdownButton_Click(object sender, RoutedEventArgs e)
        {
            var undoManager = GetActiveUndoManager();
            if (undoManager == null || !undoManager.CanUndo)
            {
                UndoPopup.IsOpen = false;
                return;
            }

            UndoListBox.ItemsSource = undoManager.UndoStack;
            UndoListBox.SelectedItems.Clear();
            UpdateUndoCountLabel(0);
            UndoPopup.IsOpen = true;
        }

        /// <summary>
        /// Handles mouse movement over the undo listbox to implement VS-style multi-select highlighting.
        /// Selects all items from the top to the item under the mouse cursor.
        /// </summary>
        private void UndoListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;

            var element = e.OriginalSource as FrameworkElement;
            while (element != null && !(element is ListBoxItem))
            {
                element = element.Parent as FrameworkElement ?? VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            if (element is ListBoxItem listBoxItem)
            {
                int hoveredIndex = listBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);
                if (hoveredIndex >= 0)
                {
                    SelectItemsUpToIndex(listBox, hoveredIndex);
                    UpdateUndoCountLabel(hoveredIndex + 1);
                }
            }
        }

        /// <summary>
        /// Handles mouse leaving the undo listbox to clear selection.
        /// </summary>
        private void UndoListBox_MouseLeave(object sender, MouseEventArgs e)
        {
            UndoListBox.SelectedItems.Clear();
            UpdateUndoCountLabel(0);
        }

        /// <summary>
        /// Handles mouse click on an undo listbox item to perform the undo operation.
        /// </summary>
        private void UndoListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null || listBox.SelectedItems.Count == 0) return;

            var undoManager = GetActiveUndoManager();
            if (undoManager == null) return;

            // Get the last selected item (furthest down in the list)
            var lastItem = listBox.SelectedItems[listBox.SelectedItems.Count - 1] as IUndoableAction;
            if (lastItem != null)
            {
                UndoPopup.IsOpen = false;
                undoManager.UndoTo(lastItem);
            }
        }

        /// <summary>
        /// Updates the undo count label with the current selection count.
        /// </summary>
        private void UpdateUndoCountLabel(int count)
        {
            if (count == 0)
                UndoCountLabel.Text = "Undo 1 Action";
            else if (count == 1)
                UndoCountLabel.Text = "Undo 1 Action";
            else
                UndoCountLabel.Text = $"Undo {count} Actions";
        }

        /// <summary>
        /// Handles the click event for "Undo All" to undo all actions in the stack.
        /// </summary>
        private void UndoAll_Click(object sender, MouseButtonEventArgs e)
        {
            var undoManager = GetActiveUndoManager();
            if (undoManager == null) return;

            UndoPopup.IsOpen = false;
            while (undoManager.CanUndo)
            {
                undoManager.Undo();
            }
        }

        /// <summary>
        /// Handles the click event for the redo dropdown button.
        /// Shows a popup with the list of redoable actions.
        /// </summary>
        private void RedoDropdownButton_Click(object sender, RoutedEventArgs e)
        {
            var undoManager = GetActiveUndoManager();
            if (undoManager == null || !undoManager.CanRedo)
            {
                RedoPopup.IsOpen = false;
                return;
            }

            RedoListBox.ItemsSource = undoManager.RedoStack;
            RedoListBox.SelectedItems.Clear();
            UpdateRedoCountLabel(0);
            RedoPopup.IsOpen = true;
        }

        /// <summary>
        /// Handles mouse movement over the redo listbox to implement VS-style multi-select highlighting.
        /// Selects all items from the top to the item under the mouse cursor.
        /// </summary>
        private void RedoListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;

            var element = e.OriginalSource as FrameworkElement;
            while (element != null && !(element is ListBoxItem))
            {
                element = element.Parent as FrameworkElement ?? VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            if (element is ListBoxItem listBoxItem)
            {
                int hoveredIndex = listBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);
                if (hoveredIndex >= 0)
                {
                    SelectItemsUpToIndex(listBox, hoveredIndex);
                    UpdateRedoCountLabel(hoveredIndex + 1);
                }
            }
        }

        /// <summary>
        /// Handles mouse leaving the redo listbox to clear selection.
        /// </summary>
        private void RedoListBox_MouseLeave(object sender, MouseEventArgs e)
        {
            RedoListBox.SelectedItems.Clear();
            UpdateRedoCountLabel(0);
        }

        /// <summary>
        /// Handles mouse click on a redo listbox item to perform the redo operation.
        /// </summary>
        private void RedoListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null || listBox.SelectedItems.Count == 0) return;

            var undoManager = GetActiveUndoManager();
            if (undoManager == null) return;

            // Get the last selected item (furthest down in the list)
            var lastItem = listBox.SelectedItems[listBox.SelectedItems.Count - 1] as IUndoableAction;
            if (lastItem != null)
            {
                RedoPopup.IsOpen = false;
                undoManager.RedoTo(lastItem);
            }
        }

        /// <summary>
        /// Updates the redo count label with the current selection count.
        /// </summary>
        private void UpdateRedoCountLabel(int count)
        {
            if (count == 0)
                RedoCountLabel.Text = "Redo 1 Action";
            else if (count == 1)
                RedoCountLabel.Text = "Redo 1 Action";
            else
                RedoCountLabel.Text = $"Redo {count} Actions";
        }

        /// <summary>
        /// Handles the click event for "Redo All" to redo all actions in the stack.
        /// </summary>
        private void RedoAll_Click(object sender, MouseButtonEventArgs e)
        {
            var undoManager = GetActiveUndoManager();
            if (undoManager == null) return;

            RedoPopup.IsOpen = false;
            while (undoManager.CanRedo)
            {
                undoManager.Redo();
            }
        }

        /// <summary>
        /// Selects all items in a listbox from the first item up to and including the specified index.
        /// This creates the VS-style multi-select behavior where hovering selects everything above.
        /// </summary>
        /// <param name="listBox">The listbox to modify selection on.</param>
        /// <param name="toIndex">The index to select up to (inclusive).</param>
        private void SelectItemsUpToIndex(ListBox listBox, int toIndex)
        {
            listBox.SelectedItems.Clear();
            for (int i = 0; i <= toIndex && i < listBox.Items.Count; i++)
            {
                listBox.SelectedItems.Add(listBox.Items[i]);
            }
        }

        #endregion

        #region View Menu

        /// <summary>
        /// Show project explorer docking panel and make it active.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ProjectExplorer_Click(object sender, RoutedEventArgs e)
        {
            BuildProjectExplorer();
            ShowAnchorable(_projectExplorerDock, AnchorableShowStrategy.Left);
        }

        /// <summary>
        /// Show message window docking panel and make it active.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void MessageWindow_Click(object sender, RoutedEventArgs e)
        {
            BuildMessageWindow();
            ShowAnchorable(_messageWindowDock, AnchorableShowStrategy.Bottom);
        }

        /// <summary>
        /// Show properties docking panel and make it active.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void PropertiesWindow_Click(object sender, RoutedEventArgs e)
        {
            BuildPropertiesWindow();
            ShowAnchorable(_propertiesWindowDock, AnchorableShowStrategy.Right);
        }

        /// <summary>
        /// Show all docking panels (project explorer, message window, and properties).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void AllWindows_Click(object sender, RoutedEventArgs e)
        {
            // Re-acquire references and restore each pane.
            BuildProjectExplorer();
            ShowAnchorable(_projectExplorerDock, AnchorableShowStrategy.Left);
            BuildMessageWindow();
            ShowAnchorable(_messageWindowDock, AnchorableShowStrategy.Bottom);
            BuildPropertiesWindow();
            ShowAnchorable(_propertiesWindowDock, AnchorableShowStrategy.Right);
        }

        /// <summary>
        /// Safely shows a LayoutAnchorable, handling auto-hidden, hidden, and detached states.
        /// </summary>
        private void ShowAnchorable(LayoutAnchorable? dock, AnchorableShowStrategy fallbackStrategy)
        {
            if (dock == null) return;
            if (dock.IsAutoHidden)
            {
                dock.ToggleAutoHide();
            }
            else if (dock.IsHidden)
            {
                dock.Show();
                if (!dock.IsVisible)
                {
                    // Show() failed because PreviousContainer was lost. Re-add to layout.
                    dock.AddToLayout(MainDock, fallbackStrategy);
                }
            }
            else if (!dock.IsVisible)
            {
                // Anchorable is fully detached (Parent is null). Re-add to layout.
                dock.AddToLayout(MainDock, fallbackStrategy);
            }
            dock.IsActive = true;
        }

        /// <summary>
        /// Reset the default layout for the project explorer, message window, and properties window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void DefaultLayout_Click(object sender, RoutedEventArgs e)
        {
            OpenWindows.CloseAllWindows();
            if (OpenWindows.CancelClosing == false)
            {
                // Clear stale references before loading new layout.
                // LoadLayout replaces the entire LayoutRoot, so old references become orphaned.
                _projectExplorerDock = null;
                _messageWindowDock = null;
                _propertiesWindowDock = null;

                LoadLayout(true);
                BuildProjectExplorer();
                BuildMessageWindow();
                BuildPropertiesWindow();
            }
        }

        #endregion

        #region Project Menu

        /// <summary>
        /// When the project menu is opened, reset the parent node for adding new elements.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ProjectMenuOpened(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Show the project properties control in the properties window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ProjectProperties_Click(object sender, RoutedEventArgs e)
        {
            Project_Click();
        }

        #endregion

        #region Tools Menu

        /// <summary>
        /// Compact (aka vacuum) project .sqlite file to reduce file size.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void CompactProjectFile_Click(object sender, RoutedEventArgs e)
        {
            // Check if the user really wants to compact the file
            if (GenericControls.MessageBox.Show("This action can take some time to execute depending on the file size. Are you sure you want to compact and optimize this project file?", "Compact & Optimize Project File", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // Check if there is enough available drive space
                string? projectPathRoot = Path.GetPathRoot(Path.GetDirectoryName(ProjectNode.Project.FullFileName));
                if (!string.IsNullOrEmpty(projectPathRoot) && UtilityFunctions.GetAvailableDriveSpace(projectPathRoot) < FileSizeManager.GetFileSize(ProjectNode.Project.FullFileName))
                {
                    GenericControls.MessageBox.Show("There is not enough available free space on the " + projectPathRoot + " drive to compact the project file. This action requires " + FileSizeManager.GetFileSizeText(ProjectNode.Project.FullFileName) + " of free space.", "Cannot Compact & Optimize Project!", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
                try
                {
                    // Compact and Optimize
                    FileSizeManager.CompactAndOptimizeFile(ProjectNode.Project);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    GenericControls.MessageBox.Show("The project file could not be compacted and optimized. " + ex.Message, "Compact & Optimize Project Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
        }

        /// <summary>
        /// Report the progress of the file size manager.
        /// </summary>
        /// <param name="message">The message to report.</param>
        private void FileSizeManager_ReportProgress(string message)
        {
            if (ProjectNode?.Project == null) return;
            FrameworkInterfaces.Messaging.Messenger.GetInstance().Add(new BasicMessageItem(MessageType.Event, message, ProjectNode.Project, "Project", ProjectNode.Project.Name, "CompactProjectFile"));
        }

        /// <summary>
        /// Add project to a zip file for backup or distribution.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ZipProjectFile_Click(object sender, RoutedEventArgs e)
        {
            var SaveFileDialog = new SaveFileDialog() { Title = "Zip Project", Filter = "(*.zip)|*.zip" };
            if (Directory.Exists(UserSettings.DefaultLocation)) SaveFileDialog.InitialDirectory = UserSettings.DefaultLocation;

            if (SaveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ProjectNode.Project.ZipProject(SaveFileDialog.FileName);
                    // Log Event
                    FrameworkInterfaces.Messaging.Messenger.GetInstance().Add(new BasicMessageItem(MessageType.Event, $"The project '{ProjectNode.Project.Name}' was zipped to '{SaveFileDialog.FileName}'.", ProjectNode.Project, "Project", ProjectNode.Project.Name, "ZipProjectFile"));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    GenericControls.MessageBox.Show("There was an unexpected error when trying to zip the project file.", "Zip Project Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
        }

        /// <summary>
        /// Report the progress of the auto-backup.
        /// </summary>
        /// <param name="message">The message to report.</param>
        private void AutoBackup_ReportProgress(string message)
        {
            if (ProjectNode?.Project == null) return;
            FrameworkInterfaces.Messaging.Messenger.GetInstance().Add(new BasicMessageItem(MessageType.Event, message, ProjectNode.Project, "Project", ProjectNode.Project.Name, "AutoBackup"));
        }

        /// <summary>
        /// Restore project from backup file (.bak extension).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void RestoreFromBackup_Click(object sender, RoutedEventArgs e)
        {
            var OpenFileDialog = new OpenFileDialog() { Title = "Restore Project", Filter = ShellPublicVariables.BackupFileDialogFilter };
            if (Directory.Exists(UserSettings.DefaultLocation)) OpenFileDialog.InitialDirectory = UserSettings.DefaultLocation;

            if (OpenFileDialog.ShowDialog() == true)
            {
                // Get filename and change file extension
                string fullFileName = OpenFileDialog.FileName;
                string newFileName = fullFileName.Replace(ShellPublicVariables.SoftwareExtension + ShellPublicVariables.BackupExtension, "_Restored" + ShellPublicVariables.SoftwareExtension);
                if (newFileName.Length > 259)
                {
                    GenericControls.MessageBox.Show("The file name is too long. Please shorten the name to have less than 250 characters before trying to restore.", "File name error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // Copy (don't move) the backup so a failed open leaves the original
                // .bak intact and the user can try a different recovery path. Move-
                // then-open consumed the backup name before knowing the open succeeded;
                // any open failure (corrupt file, version mismatch, missing dependencies)
                // left the user with no .bak to retry.
                try
                {
                    File.Copy(fullFileName, newFileName, overwrite: false);
                }
                catch (Exception copyEx)
                {
                    GenericControls.MessageBox.Show(
                        $"Failed to copy the backup file:\n\n{copyEx.Message}",
                        "Restore Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Open project. If this throws or otherwise fails, leave the original
                // backup file in place so the user can attempt restore again.
                bool openSucceeded = false;
                try
                {
                    OpenProject(newFileName);
                    openSucceeded = ProjectNode?.Project != null;
                }
                catch (Exception openEx)
                {
                    GenericControls.MessageBox.Show(
                        $"Failed to open the restored project. The original backup file has been preserved at:\n{fullFileName}\n\n{openEx.Message}",
                        "Restore Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    // Clean up the half-restored copy so it doesn't masquerade as a usable project.
                    try { File.Delete(newFileName); } catch (Exception deleteEx) { Debug.WriteLine(deleteEx.Message); }
                    return;
                }

                if (openSucceeded && ProjectNode?.Project is not null)
                {
                    // Only delete the original backup once the restored project is known good.
                    try { File.Delete(fullFileName); } catch (Exception deleteEx) { Debug.WriteLine(deleteEx.Message); }

                    // Log Event
                    var restoredProject = ProjectNode.Project;
                    FrameworkInterfaces.Messaging.Messenger.GetInstance().Add(new BasicMessageItem(MessageType.Event, $"The backup project file '{fullFileName}{ShellPublicVariables.BackupExtension}' was restored.", restoredProject, "Project", restoredProject.Name, "RestoreFromBackup"));
                }
            }
        }

        /// <summary>
        /// Open options dialog for configuring application settings.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Options_Click(object sender, RoutedEventArgs e)
        {
            var options = new OptionsDialog(this);
            options.DefaultsOptions.CustomOptions = ProjectDefaultsControl;
            options.Apply_Click += (s1, e1) => Options_Apply_Click?.Invoke(s1, e1);
            options.ShowDialog();
        }

        /// <summary>
        /// Handles the Check for Updates menu item click to check for available software updates.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (UpdateService == null) return;

            try
            {
                CheckForUpdatesMenuItem.IsEnabled = false;
                CheckForUpdatesMenuItem.Header = "Checking for updates...";

                var result = await UpdateService.CheckForUpdateAsync();

                if (result.Error != null)
                {
                    GenericControls.MessageBox.Show(
                        $"Failed to check for updates:\n\n{result.Error.Message}",
                        "Update Check Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else if (result.IsUpdateAvailable && result.Update != null)
                {
                    var update = result.Update;
                    var message = $"A new version is available!\n\n" +
                                  $"Current version: {result.CurrentVersion}\n" +
                                  $"New version: {update.Version}\n" +
                                  $"Published: {update.PublishedAt:d}\n\n" +
                                  $"Would you like to download and install the update?";

                    var dialogResult = GenericControls.MessageBox.Show(
                        message,
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        await DownloadAndInstallUpdateAsync(update);
                    }
                }
                else
                {
                    GenericControls.MessageBox.Show(
                        $"You are running the latest version ({result.CurrentVersion}).",
                        "No Updates Available",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                GenericControls.MessageBox.Show(
                    $"An error occurred while checking for updates:\n\n{ex.Message}",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                CheckForUpdatesMenuItem.IsEnabled = true;
                CheckForUpdatesMenuItem.Header = "Check for Updates...";
            }
        }

        /// <summary>
        /// Downloads and installs an update with progress reporting.
        /// </summary>
        /// <param name="update">The update information containing download details.</param>
        private async System.Threading.Tasks.Task DownloadAndInstallUpdateAsync(UpdateInfo update)
        {
            if (UpdateService == null) return;

            try
            {
                // Show download progress
                var progressWindow = new System.Windows.Window
                {
                    Title = "Downloading Update",
                    Width = 400,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.ToolWindow
                };

                var stackPanel = new StackPanel { Margin = new Thickness(20) };
                var statusText = new TextBlock { Text = "Downloading update...", Margin = new Thickness(0, 0, 0, 10) };
                var progressBar = new System.Windows.Controls.ProgressBar { Height = 20, Minimum = 0, Maximum = 100 };
                var progressText = new TextBlock { Text = "0%", Margin = new Thickness(0, 5, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };

                stackPanel.Children.Add(statusText);
                stackPanel.Children.Add(progressBar);
                stackPanel.Children.Add(progressText);
                progressWindow.Content = stackPanel;
                progressWindow.Show();

                var progress = new Progress<UpdateDownloadProgress>(p =>
                {
                    if (p.TotalBytes > 0)
                    {
                        progressBar.Value = p.ProgressPercentage;
                        progressText.Text = p.ProgressText;
                    }
                });

                var downloadResult = await UpdateService.DownloadUpdateAsync(update, progress);

                progressWindow.Close();

                if (downloadResult.Success)
                {
                    if (ShellPublicVariables.SimulationInProgress == true)
                    {
                        GenericControls.MessageBox.Show(
                            "A simulation is currently running. Please wait for it to finish before installing the update.",
                            "Cannot Install Update",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }

                    var confirmResult = GenericControls.MessageBox.Show(
                        "Update downloaded successfully.\n\n" +
                        "The application will now close to install the update and restart automatically.\n\n" +
                        "Do you want to continue?",
                        "Install Update",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirmResult == MessageBoxResult.Yes && downloadResult.FilePath != null)
                    {
                        // Prompt to save any unsaved project / element state. The updater
                        // calls Environment.Exit(0) so any dirty state not flushed here is lost.
                        if (!TryPromptToSaveDirtyState(out _))
                        {
                            return;
                        }

                        // Persist user settings + recent files before the updater terminates
                        // the process. Wrapped defensively so a settings I/O error doesn't
                        // block the install.
                        try { UserSettings.Save(ShellPublicVariables.UserSettingsFilePath); }
                        catch (Exception saveEx) { Debug.WriteLine($"[Update] UserSettings.Save failed: {saveEx.Message}"); }
                        try { RecentFiles.SaveToXML(); }
                        catch (Exception saveEx) { Debug.WriteLine($"[Update] RecentFiles.SaveToXML failed: {saveEx.Message}"); }

                        UpdateService.InstallUpdateAndRestart(downloadResult.FilePath);
                    }
                }
                else if (downloadResult.WasCancelled)
                {
                    GenericControls.MessageBox.Show(
                        "Download was cancelled.",
                        "Download Cancelled",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    GenericControls.MessageBox.Show(
                        $"Failed to download update:\n\n{downloadResult.Error?.Message}",
                        "Download Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                GenericControls.MessageBox.Show(
                    $"An error occurred while downloading the update:\n\n{ex.Message}",
                    "Download Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Window Menu

        /// <summary>
        /// Close all open documents and show project properties.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void CloseAllDocuments_Click(object sender, RoutedEventArgs e)
        {
            _propertiesPaneClicked = false;
            _projectExplorerPaneClicked = false;

            OpenWindows.CloseAllWindows();

            Project_Click();
            ProjectNode.IsSelected = true;
        }

        /// <summary>
        /// Open the windows dialog to manage open documents.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Windows_Click(object sender, RoutedEventArgs e)
        {
            _propertiesPaneClicked = false;
            _projectExplorerPaneClicked = false;

            var openWindowsDialog = new OpenWindowsDialog() { Windows = OpenWindows, Icon = Icon };
            openWindowsDialog.ShowDialog();
        }

        #endregion

        #region Disable-Enable Menu strip and Windows

        /// <summary>
        /// Disables the main menu and toolbar.
        /// </summary>
        public void DisableMenuStrip()
        {
            MainMenu.IsEnabled = false;
            MainStackPanel.IsEnabled = false;
        }

        /// <summary>
        /// Enables the main menu and toolbar.
        /// </summary>
        public void EnableMenuStrip()
        {
            MainMenu.IsEnabled = true;
            MainStackPanel.IsEnabled = true;
        }

        /// <summary>
        /// Disables the project explorer window.
        /// </summary>
        public void DisableProjectExplorer()
        {
            if (_projectExplorerDock != null)
                _projectExplorerDock.IsEnabled = false;
        }

        /// <summary>
        /// Enables the project explorer window.
        /// </summary>
        public void EnableProjectExplorer()
        {
            if (_projectExplorerDock != null)
                _projectExplorerDock.IsEnabled = true;
        }

        /// <summary>
        /// Disables the message window.
        /// </summary>
        public void DisableMessageWindow()
        {
            if (_messageWindowDock != null)
                _messageWindowDock.IsEnabled = false;
        }

        /// <summary>
        /// Enables the message window.
        /// </summary>
        public void EnableMessageWindow()
        {
            if (_messageWindowDock != null)
                _messageWindowDock.IsEnabled = true;
        }

        /// <summary>
        /// Disables the properties window.
        /// </summary>
        public void DisablePropertiesWindow()
        {
            if (_propertiesWindowDock != null)
                _propertiesWindowDock.IsEnabled = false;
        }

        /// <summary>
        /// Enables the properties window.
        /// </summary>
        public void EnablePropertiesWindow()
        {
            if (_propertiesWindowDock != null)
                _propertiesWindowDock.IsEnabled = true;
        }

        /// <summary>
        /// Disables all open document windows.
        /// </summary>
        public void DisableOpenWindows()
        {
            OpenWindows.DisableAllWindows();
        }

        /// <summary>
        /// Enables all open document windows.
        /// </summary>
        public void EnableOpenWindows()
        {
            OpenWindows.EnableAllWindows();
        }


        #endregion


    }
}
