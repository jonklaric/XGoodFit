namespace FrameworkUI.ProjectExplorer
{

    /// <summary>
    /// Element node group class.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public class ElementNodeGroup : NodeGroup
    {
        /// <summary>
        /// Construct a new element node group.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="parentTreeView">The parent tree view.</param>
        public ElementNodeGroup(Node? parentNode, ProjectExplorerTreeView? parentTreeView) : base(parentNode, parentTreeView)
        {

        }

    }
}
