using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FrameworkInterfaces;
using WpfCompat.Input;

namespace FrameworkUI
{
    /// <summary>
    /// Interaction logic for ProjectPropertiesControl.xaml
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// <para>
    /// XGoodFit port: the dependency properties are styled properties with upstream's callback, and the
    /// PreviewMouseLeftButtonDown handlers from the XAML are WpfCompat's WPF mouse events, added here.
    /// </para>
    /// </remarks>
    public partial class ProjectPropertiesControl : UserControl
    {
        /// <summary>
        /// Initialize an empty control.
        /// </summary>
        public ProjectPropertiesControl()
        {
            // This call is required by the designer.
            InitializeComponent();
            // XGoodFit port: the XAML's PreviewMouseLeftButtonDown handlers.
            NameTextBox.AddPreviewMouseLeftButtonDownHandler(Name_PreviewMouseLeftButtonDown);
            Description.AddPreviewMouseLeftButtonDownHandler(Description_PreviewMouseLeftButtonDown);
            CreationDate.AddPreviewMouseLeftButtonDownHandler(CreationDate_PreviewMouseLeftButtonDown);
            LastModified.AddPreviewMouseLeftButtonDownHandler(LastModified_PreviewMouseLeftButtonDown);
            FileDirectory.AddPreviewMouseLeftButtonDownHandler(FileDirectory_PreviewMouseLeftButtonDown);
            SoftwareVersion.AddPreviewMouseLeftButtonDownHandler(SoftwareVersion_PreviewMouseLeftButtonDown);
            // Add any initialization after the InitializeComponent() call.
        }

        /// <summary>
        /// Stores the previous name value before editing begins.
        /// </summary>
        private string? _previousName;

        /// <summary>
        /// Dependency property for the IProject property.
        /// </summary>
        public static readonly StyledProperty<IProject?> ProjectProperty = AvaloniaProperty.Register<ProjectPropertiesControl, IProject?>(nameof(Project), null);

        static ProjectPropertiesControl()
        {
            // XGoodFit port: the property-changed callback of upstream's metadata.
            ProjectProperty.Changed.AddClassHandler<ProjectPropertiesControl>((d, e) => ProjectCallback(d, e));
        }

        /// <summary>
        /// Gets or sets the project.
        /// </summary>
        public IProject Project
        {
            get { return (IProject)GetValue(ProjectProperty)!; }
            set { SetValue(ProjectProperty, value); }
        }

        /// <summary>
        /// Set up the class attributes when the project class is set.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void ProjectCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(ProjectPropertiesControl)) return;
            ProjectPropertiesControl thisControl = (ProjectPropertiesControl)d;

            if (e.NewValue == null) return;
            IProject? newProject = e.NewValue as IProject;
            if (newProject == null) return;
            thisControl.PropertyAttributes.GetClassAttributes(newProject);
        }

        /// <summary>
        /// Dependency property for the existing names property.
        /// </summary>
        public static readonly StyledProperty<string[]> ExistingNamesProperty = AvaloniaProperty.Register<ProjectPropertiesControl, string[]>(nameof(ExistingNames), new string[] { });

        /// <summary>
        /// Gets or sets the existing names.
        /// </summary>
        public string[] ExistingNames
        {
            get { return (string[])GetValue(ExistingNamesProperty); }
            set { SetValue(ExistingNamesProperty, value); }
        }

        /// <summary>
        /// On left click, get property attributes.
        /// </summary>
        private void Name_PreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Project.Name), Project);
        }

        /// <summary>
        /// On left click, get property attributes.
        /// </summary>
        private void Description_PreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Project.Description), Project);
        }

        /// <summary>
        /// On left click, get property attributes.
        /// </summary>
        private void CreationDate_PreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Project.CreationDate), Project);
        }

        /// <summary>
        /// On left click, get property attributes.
        /// </summary>
        private void LastModified_PreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Project.LastModified), Project);
        }

        /// <summary>
        /// On left click, get property attributes.
        /// </summary>
        private void FileDirectory_PreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Project.FileDirectory), Project);
        }

        /// <summary>
        /// On left click, get property attributes.
        /// </summary>
        private void SoftwareVersion_PreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            PropertyAttributes.GetPropertyAttributes(nameof(Project.SoftwareVersion), Project);
        }

        /// <summary>
        /// When the name gets focus, load existing names.
        /// </summary>
        private void NameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _previousName = Project.Name;
            if (string.IsNullOrEmpty(Project.FileDirectory)) return;
            var fullfilepaths = Directory.GetFiles(Project.FileDirectory, "*" + ShellPublicVariables.SoftwareExtension);
            var files = new List<string>();
            for (int i = 0; i < fullfilepaths.Count(); i++)
            {
                if (fullfilepaths[i] != Project.FullFileName)
                {
                    files.Add(Path.GetFileNameWithoutExtension(fullfilepaths[i]));
                }
            }
            ExistingNames = files.ToArray();
        }

        /// <summary>
        /// On lost focus, if the name is not unique, revert the name, and show an error message.
        /// </summary>
        private void NameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (NameTextBox.NameTextBox.IsValid == true) return;
            Project.Name = _previousName ?? string.Empty;
        }

    }
}
