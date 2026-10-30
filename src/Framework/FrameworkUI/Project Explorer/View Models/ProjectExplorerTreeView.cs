using Avalonia;

namespace FrameworkUI.ProjectExplorer
{

    /// <summary>
    /// Project Explorer TreeView
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </para>
    /// <para>
    /// XGoodFit port: ProjectNode is a styled property with upstream's callback. WPF's Items.Refresh()
    /// isn't needed: Avalonia's items follow Items as it changes.
    /// </para>
    /// </remarks>
    public class ProjectExplorerTreeView : ExplorerTreeView
    {

        /// <summary>
        /// Construct new project explorer tree view.
        /// </summary>
        public ProjectExplorerTreeView()
        {
        }

        /// <summary>
        /// Dependency property for the project node.
        /// </summary>
        public static readonly StyledProperty<ProjectNode?> ProjectNodeProperty = AvaloniaProperty.Register<ProjectExplorerTreeView, ProjectNode?>(nameof(ProjectNode), null);

        static ProjectExplorerTreeView()
        {
            // XGoodFit port: the property-changed callback of upstream's metadata.
            ProjectNodeProperty.Changed.AddClassHandler<ProjectExplorerTreeView>((d, e) => ProjectNode_PropertyChangedCallback(d, e));
        }

        /// <summary>
        /// Gets or sets the project node.
        /// </summary>
        public ProjectNode ProjectNode
        {
            get { return (ProjectNode)GetValue(ProjectNodeProperty)!; }
            set { SetValue(ProjectNodeProperty, value); }
        }

        /// <summary>
        /// When the project node is changed, update the tree view items.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event data.</param>
        private static void ProjectNode_PropertyChangedCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(ProjectExplorerTreeView)) return;
            ProjectExplorerTreeView thisControl = (ProjectExplorerTreeView)d;

            // Get the new value
            ProjectNode? newValue = e.NewValue as ProjectNode;
            if (newValue == null) return;

            newValue.ParentTreeView = thisControl;
            thisControl.Items.Clear();
            thisControl.Items.Add(newValue);
            // XGoodFit port: no Items.Refresh(); the items follow Items.
            thisControl.UpdateLayout();

        }

    }
}
