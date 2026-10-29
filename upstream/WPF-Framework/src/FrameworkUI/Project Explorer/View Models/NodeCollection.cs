using GenericControls;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FrameworkUI.ProjectExplorer
{
    /// <summary>
    /// Node collection class.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </para>
    /// </remarks>
    public class NodeCollection : Node
    {
        private bool _showSortingContextItems = true;
        private bool _showAddGroupContextItem = true;
        private readonly MenuItem _addGroupMenuItem = new MenuItem() { Header = "Add Group" };
        private readonly MenuItem _sortASCMenuItem = new MenuItem() { Name = "sortASC", Header = "Sort Ascending" };
        private readonly MenuItem _sortDSCMenuItem = new MenuItem() { Name = "sortDSC", Header = "Sort Descending" };

        /// <summary>
        /// Construct a new node collection.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="parentTreeView">The parent tree view.</param>
        public NodeCollection(Node? parentNode, ExplorerTreeView? parentTreeView) : base(parentNode, parentTreeView)
        {
            // Set Properties
            IsExpanded = true;
            AllowDrop = true;
            AllowGrouping = true;
            IsReadOnly = true;

            // Set up dynamic icon references for theme support
            var addGroupIcon = new Image();
            addGroupIcon.SetResourceReference(Image.SourceProperty, "GroupImage");
            _addGroupMenuItem.Icon = addGroupIcon;
            var sortAscIcon = new Image();
            sortAscIcon.SetResourceReference(Image.SourceProperty, "SortAscendingImage");
            _sortASCMenuItem.Icon = sortAscIcon;
            var sortDescIcon = new Image();
            sortDescIcon.SetResourceReference(Image.SourceProperty, "SortDescendingImage");
            _sortDSCMenuItem.Icon = sortDescIcon;

            // Node Header Appearance
            NodeHeader.ShowToolTip = false;
            NodeHeader.SetResourceReference(NodeHeader.StaticImageProperty, "FolderIcon");
            NodeHeader.SetResourceReference(NodeHeader.ExpandedImageProperty, "FolderOpenIcon");

            // Event Handlers
            _addGroupMenuItem.Click += AddGroup_Click;
            _sortASCMenuItem.Click += Sort_Click;
            _sortDSCMenuItem.Click += Sort_Click;

            // Set up context menu
            _collectionContextItems.Add(_addGroupMenuItem);
            _collectionContextItems.Add( _sortASCMenuItem);
            _collectionContextItems.Add(_sortDSCMenuItem);
        }

        /// <summary>
        /// Determines if the node item can be multi-selected or not.
        /// </summary>
        public override bool CanMultiSelect => false;

        /// <summary>
        /// Determines if grouping of nodes is allowed. 
        /// </summary>
        public bool AllowGrouping { get; set; }

        /// <summary>
        /// Event is raised when a node is added.
        /// </summary>
        public event NodeAddedEventHandler? NodeAdded;

        /// <summary>
        /// Event is raised when a node is added.
        /// </summary>
        /// <param name="node">The node that was added.</param>
        public delegate void NodeAddedEventHandler(Node node);

        /// <summary>
        /// Event is raised with a node is removed.
        /// </summary>
        public event NodeRemovedEventHandler? NodeRemoved;

        /// <summary>
        /// Event is raised with a node is removed.
        /// </summary>
        /// <param name="node">The node that was removed.</param>
        public delegate void NodeRemovedEventHandler(Node node);

        /// <summary>
        /// Event is raised when a node group is added.
        /// </summary>
        public event GroupAddedEventHandler? GroupAdded;

        /// <summary>
        /// Event is raised when a node group is added.
        /// </summary>
        /// <param name="nodeGroup">The node group that was added.</param>
        public delegate void GroupAddedEventHandler(NodeGroup nodeGroup);

        /// <summary>
        /// Event is raised with a node group is removed.
        /// </summary>
        public event GroupRemovedEventHandler? GroupRemoved;

        /// <summary>
        /// Event is raised with a node group is removed.
        /// </summary>
        /// <param name="nodeGroup">The node group that was removed.</param>
        public delegate void GroupRemovedEventHandler(NodeGroup nodeGroup);

        /// <summary>
        /// Boolean value to determine if the default sorting context menu items is shown. True by default.
        /// </summary>
        public bool ShowSortingContextItems
        {
            get { return _showSortingContextItems; }
            set
            {
                if (_showSortingContextItems != value)
                {
                    _showSortingContextItems = value;
                    _sortASCMenuItem.Visibility = value == true ? Visibility.Visible : Visibility.Collapsed;
                    _sortDSCMenuItem.Visibility = value == true ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Boolean value to determine if the default Create New context menu item is shown. True by default.
        /// </summary>
        public bool ShowAddGroupContextItem
        {
            get { return _showAddGroupContextItem; }
            set
            {
                if (_showAddGroupContextItem != value)
                {
                    _showAddGroupContextItem = value;
                    _addGroupMenuItem.Visibility = value == true ? Visibility.Visible : Visibility.Collapsed;
                    //SetContextMenu();
                }
            }
        }

        /// <summary>
        /// On click, add a node group to the collection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            var newGroup = new NodeGroup(this, ParentTreeView);
            AddGroup(newGroup);
            newGroup.IsInEditMode = true;
        }

        /// <summary>
        /// On click, sort nodes in ascending or descending order.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Sort_Click(object sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender).Name == "sortASC")
            {
                Sort(ListSortDirection.Ascending);
            }
            else if (((MenuItem)sender).Name == "sortDSC")
            {
                Sort(ListSortDirection.Descending);
            }
            Items.Refresh();
            RaiseNodeSorted(this);
            e.Handled = true;
        }

        /// <summary>
        /// Raise the Group Added event.
        /// </summary>
        /// <param name="groupNode">The group node.</param>
        public void RaiseGroupAdded(NodeGroup groupNode)
        {
            GroupAdded?.Invoke(groupNode);
        }

        /// <summary>
        /// Raise the Group Removed event.
        /// </summary>
        /// <param name="groupNode">The group node.</param>
        public void RaiseGroupRemoved(NodeGroup groupNode)
        {
            GroupRemoved?.Invoke(groupNode);
        }

        /// <summary>
        /// Add a node to the collection.
        /// </summary>
        /// <param name="node">The node to add.</param>
        /// <param name="refreshItems">If <c>true</c>, refreshes the underlying <see cref="ItemCollection"/> so bound views re-bind to the updated children.</param>
        public void Add(Node node, bool refreshItems = true)
        {
            node.ParentNode = this;
            node.ParentTreeView = ParentTreeView;
            ChildNodes.Add(node);
            if (refreshItems) Items.Refresh();
            NodeAdded?.Invoke(node);
        }

        /// <summary>
        /// Add new group. 
        /// </summary>
        /// <param name="nodeGroup">The node group.</param>
        public void AddGroup(NodeGroup nodeGroup)
        {
            nodeGroup.ParentNode = this;
            nodeGroup.ParentTreeView = ParentTreeView;
            ChildNodes.Add(nodeGroup);
            nodeGroup.IsSelected = true;
            GroupAdded?.Invoke(nodeGroup);
        }

        /// <summary>
        /// Inserts an node into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the node should be inserted.</param>
        /// <param name="node">node to insert.</param>
        public void Insert(int index, Node node)
        {
            node.ParentNode = this;
            node.ParentTreeView = ParentTreeView;
            if (index >= ChildNodes.Count) index = ChildNodes.Count;
            ChildNodes.Insert(index, node);
            Items.Refresh();
            (ParentTreeView as ExplorerTreeView)?.ClearSelection();
            node.IsSelected = true;
            NodeAdded?.Invoke(node);
        }

        /// <summary>
        /// Removes the first occurrence of the specified data object.
        /// </summary>
        /// <param name="node">The node to remove from the collection.</param>
        public void Remove(Node node)
        {
            int index = ChildNodes.IndexOf(node);
            ChildNodes.Remove(node);
            Items.Refresh();
            if (ChildNodes.Count > 0)
            {
                if (ChildNodes.Count - 1 >= index)
                {
                    ChildNodes[index].IsSelected = true;
                }
                else
                {
                    ChildNodes.Last().IsSelected = true;
                }
            }
            NodeRemoved?.Invoke(node);
        }

        /// <summary>
        /// Remove data at the specified index of the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the node to remove.</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= ChildNodes.Count) return;
            var node = ChildNodes[index];
            if (node != null) { Remove(node); }
        }
    }
}

