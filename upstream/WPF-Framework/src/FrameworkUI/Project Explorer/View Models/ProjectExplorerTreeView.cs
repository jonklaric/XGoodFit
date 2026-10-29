using System.Windows;

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
        public static DependencyProperty ProjectNodeProperty = DependencyProperty.Register(nameof(ProjectNode), typeof(ProjectNode), typeof(ProjectExplorerTreeView), new FrameworkPropertyMetadata(null, ProjectNode_PropertyChangedCallback));

        /// <summary>
        /// Gets or sets the project node.
        /// </summary>
        public ProjectNode ProjectNode
        {
            get { return (ProjectNode)GetValue(ProjectNodeProperty); }
            set { SetValue(ProjectNodeProperty, value); }
        }

        /// <summary>
        /// When the project node is changed, update the tree view items.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event data.</param>
        private static void ProjectNode_PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
            thisControl.Items.Refresh();
            thisControl.UpdateLayout();

        }

    }
}
