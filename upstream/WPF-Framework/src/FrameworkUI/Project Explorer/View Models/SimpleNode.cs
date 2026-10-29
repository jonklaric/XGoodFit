using GenericControls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FrameworkUI.ProjectExplorer
{
    /// <summary>
    /// Simple node class representing a basic tree view node.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public class SimpleNode : Node
    {
        private string _nodeName = "Node Name";

        /// <summary>
        /// Construct a new simple node.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="parentTreeView">The parent tree view.</param>
        public SimpleNode(string name, Node? parentNode, ExplorerTreeView? parentTreeView) : base(parentNode, parentTreeView)
        {
            AllowDrop = true;
            IsReadOnly = false;
            _nodeName = name;

            // Node Header Appearance
            //NodeHeader.ShowToolTip = true;
            NodeHeader.SetResourceReference(NodeHeader.StaticImageProperty, "MoveDownImage");
            NodeHeader.SetResourceReference(NodeHeader.ExpandedImageProperty, "MoveUpImage");

            // Event Handlers

            // Set bindings           
            NodeHeader.SetBinding(NodeHeader.HeaderTextProperty, new Binding(nameof(NodeName)) { Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, Mode = BindingMode.TwoWay });
        }

        /// <summary>
        /// Gets a value indicating whether this node can be multi-selected with other nodes.
        /// </summary>
        public override bool CanMultiSelect => true;

        /// <summary>
        /// Gets or sets the node name.
        /// </summary>
        public string NodeName
        {
            get { return _nodeName; }
            set
            {
                if (value == _nodeName) { return; }
                _nodeName = value;
                RaisePropertyChange(nameof(NodeName));
            }
        }
    }
}
