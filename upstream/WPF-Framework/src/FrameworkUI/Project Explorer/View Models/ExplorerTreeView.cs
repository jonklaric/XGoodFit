using GenericControls;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;

namespace FrameworkUI.ProjectExplorer
{
    /// <summary>
    /// Explorer tree view class that provides enhanced tree view functionality with drag-drop and multi-selection support.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public class ExplorerTreeView : TreeView
    {

        /// <summary>
        /// Construct new explorer tree view.
        /// </summary>
        public ExplorerTreeView()
        {
            // Add handlers
            PreviewMouseRightButtonDown += Me_PreviewMouseRightButtonDown;
            PreviewMouseLeftButtonDown += Me_PreviewMouseLeftButtonDown;
            //PreviewMouseLeftButtonUp += Me_PreviewMouseLeftButtonUp;
            PreviewMouseDown += Me_PreviewMouseDown;
            PreviewMouseMove += Me_PreviewMouseMove;
            DragOver += Me_DragOver;
            DragLeave += Me_DragLeave;
            Drop += Me_Drop;
            AllowDrop = true;
            AllowMultiSelect = true;
        }

        /// <summary>
        /// Private variables used for drag and drop.
        /// </summary>
        private bool _canStartDragDrop = false;
        private Point _startPoint;
        private InsertionAdorner? _insertionAdorner;
        private Node? _startNode;
        private Node? _dragNode;
        private List<Node> _selectedNodes = new List<Node>();

        /// <summary>
        /// Determines if the user can select multiple element nodes at once.
        /// </summary>
        public bool AllowMultiSelect { get; set; }

        /// <summary>
        /// Gets the list of selected element nodes.
        /// </summary>
        public List<Node> SelectedNodes
        {
            get { return _selectedNodes; }
        }

        #region Multi-Selection

        /// <summary>
        /// Clear all selections
        /// </summary>
        public void ClearSelection()
        {
            if (this != null) ClearTreeViewItemsControlSelection(Items, ItemContainerGenerator);
            for (int i = _selectedNodes.Count - 1; i >= 0; i -= 1)
            {
                _selectedNodes[i].IsNodeSelected = false;
                _selectedNodes.RemoveAt(i);
            }
        }

        /// <summary>
        /// Recursive routine used to deselect all items.
        /// </summary>
        /// <param name="itemCollection">The collection of items to clear.</param>
        /// <param name="itemContainerGenerator">The item container generator.</param>
        private void ClearTreeViewItemsControlSelection(ItemCollection itemCollection, ItemContainerGenerator itemContainerGenerator)
        {
            if (itemCollection != null && itemContainerGenerator != null)
            {
                for (int i = 0; i < itemCollection.Count; i++)
                {
                    TreeViewItem? tvi = itemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                    if (tvi != null)
                    {
                        ClearTreeViewItemsControlSelection(tvi.Items, tvi.ItemContainerGenerator);
                        tvi.IsSelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the preview mouse right button down event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Me_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem? treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                if (treeViewItem as ElementNodeCollection != null || treeViewItem as ElementNodeGroup != null || treeViewItem as NodeCollection != null || treeViewItem as NodeGroup != null)
                {
                    treeViewItem.Focus();
                    UpdateLayout();
                }
                //e.Handled = true;
            }
        }

        /// <summary>
        /// Get the starting mouse position on the preview mouse left button down event
        /// </summary>
        private void Me_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindTreeViewItem(this, e.OriginalSource as DependencyObject);
            if (item == null) return;
            if (item as ProjectNode != null || item as ElementNodeCollection != null || item as ElementNodeGroup != null)
            {
                ClearSelection();
            }
            else if (item as Node != null)
            {
                // node is going to be selected
                Node node = (Node)item;
                // Select the node
                if (Keyboard.Modifiers == ModifierKeys.Control && node.CanMultiSelect==true)
                {
                    // The user wants to select multiple nodes at random
                    if (_selectedNodes.Count == 0)
                    {
                        // This is the first element
                        if (SelectedItem as Node != null)
                        {
                            ((Node)SelectedItem).IsNodeSelected = true;
                            _selectedNodes.Add((Node)SelectedItem);
                        }

                        node.IsNodeSelected = true;
                        _startNode = node;
                        _selectedNodes.Add(node);
                    }
                    else if (_selectedNodes.IndexOf(node) < 0)
                    {
                        node.IsNodeSelected = true;
                        _startNode = node;
                        _selectedNodes.Add(node);
                    }
                    else
                    {
                        node.IsNodeSelected = false;
                        _selectedNodes.Remove(node);
                        if (_selectedNodes.Count > 0)
                        {
                            _startNode = _selectedNodes.Last();
                            _selectedNodes.Last().Focus();
                        }
                        else
                        {
                            var NodeCollection = FindAncestor<NodeCollection>((DependencyObject)e.OriginalSource);
                            NodeCollection?.Focus();
                        }
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Shift && node.CanMultiSelect == true)
                {
                    // The user wants to select multiple nodes continuously
                    if (_selectedNodes.Count == 0)
                    {
                        // This is the first element
                        if (SelectedItem as ProjectNode != null || SelectedItem as ElementNodeCollection != null || SelectedItem as ElementNodeGroup != null)
                        {
                            ((Node)SelectedItem).IsNodeSelected = true;
                            _selectedNodes.Add((Node)SelectedItem);
                        }
                        node.IsNodeSelected = true;
                        _startNode = node;
                        _selectedNodes.Add(node);
                    }
                    else if (_selectedNodes.Count > 0 && _startNode != null)
                    {
                        if (node.ParentNode == _startNode.ParentNode)
                        {
                            var parentNode = FindAncestor<Node>((DependencyObject)e.OriginalSource);
                            int startIndex = _startNode.ParentNode!.ChildNodes.IndexOf(_startNode);
                            int endIndex = node.ParentNode!.ChildNodes.IndexOf(node);
                            ClearSelection();
                            for (int i = Math.Min(startIndex, endIndex); i <= Math.Max(startIndex, endIndex); i++)
                            {
                                Node cNode = (Node)node.ParentNode!.ChildNodes[i];
                                cNode.IsNodeSelected = true;
                                _selectedNodes.Add(cNode);
                            }
                        }
                    }
                }
                else
                {
                    // The user wants to select a single node.
                    ClearSelection();
                    node.IsNodeSelected = true;
                    _startNode = node;
                    _selectedNodes.Add(node);
                }
            }
            else
            {
                // The user is going to click into white space
                UserClickedNonElementNode();
            }

            UpdateLayout();
        }

        /// <summary>
        /// Handles when the user clicks on white space (non-node area).
        /// </summary>
        private void UserClickedNonElementNode()
        {
            foreach (var treeItem in Items)
            {
                if (typeof(Node).IsAssignableFrom(treeItem.GetType()))
                {
                    UserClickedNonElementNode((Node)treeItem);
                }
            }
        }

        /// <summary>
        /// Recursively handles when the user clicks on white space for a given node.
        /// </summary>
        /// <param name="n">The node to process.</param>
        private void UserClickedNonElementNode(Node n)
        {
            n.UserClickedWhiteSpace();
            foreach (var item in n.ChildNodes)
            {
                UserClickedNonElementNode(item);
            }
        }

        #endregion

        #region Drag-Drop

        /// <summary>
        /// Determines if drag-drop can be initiated.
        /// </summary>
        private void Me_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _canStartDragDrop = false;
            _dragNode = null;

            // First check if we are pressing over a node, and if that node can be drag-dropped.
            var node = FindTreeViewItem(this, e.OriginalSource as DependencyObject) as Node;

            // If node is valid and left-button is pressed, then initiate drag-drop
            if (node != null && node.AllowDrop && e.LeftButton == MouseButtonState.Pressed)
            {
                // Allow drag drop
                _canStartDragDrop = true;
                // Get the start point for element move operations
                _startPoint = e.GetPosition(this);
                // Set the node to be dragged and dropped
                _dragNode = node;
            }
        }

        /// <summary>
        /// Initiate drag-drop when item is mouse is dragging.
        /// </summary>
        private void Me_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var node = FindTreeViewItem(this, e.OriginalSource as DependencyObject) as Node;
            if (node == null) return;

            // First, check if left button is pressed and if drag drop is allowed. 
            if (e.LeftButton == MouseButtonState.Pressed && _canStartDragDrop == true && _dragNode != null)
            {
                // Next, Check if the mouse has moved sufficiently in the horizontal or vertical direction. 
                var position = e.GetPosition(this);
                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    
                    // Check if there is only one node selected or a node group being dragged.
                    if (_dragNode.GetType() == typeof(NodeGroup) || _selectedNodes.Count == 1)
                    {
                        if (_dragNode.IsInEditMode == true) return;
                        var dragData = new DataObject(_dragNode);
                        dragData.SetText(_dragNode.GetDragText()); // the drag data text is set because and element node could be dragged-dropped from an external instance.
                        DragDrop.DoDragDrop(_dragNode, dragData, DragDropEffects.Move);
                    }
                }
            }
        }

        /// <summary>
        /// On drag over, check if the drop target collection type is valid and set the adorner.
        /// </summary>
        private void Me_DragOver(object sender, DragEventArgs e)
        {
            // e.Effects = DragDropEffects.None;

            // Get drag elements parent element collection
            NodeCollection? dragParentCollection = null;
            if (_dragNode == null)
            {
                // Get drag data text and see if this is an element node or element node group
                if (e.Data == null) { e.Handled = true; return; }
                var data = e.Data.GetData(DataFormats.Text);
                if (data == null) { e.Handled = true; return; }

                var dataString = data.ToString()!.Split('>');
                if (dataString.Count() < 6) { e.Handled = true; return; }
                // dataString[3] is the parent collection header name (e.g. "Input Data")
                string eParentCollectionName = dataString[3];
                bool canCopyFromExternal = false;
                bool.TryParse(dataString[5], out canCopyFromExternal);
                if (canCopyFromExternal == false) { e.Handled = true; return; }
                foreach (var item in Items)
                {
                    if (item is NodeCollection nc && nc.NodeHeader?.HeaderText == eParentCollectionName)
                    {
                        dragParentCollection = nc;
                        break;
                    }
                    // Search children (NodeCollections are children of the root ProjectNode)
                    if (item is Node parentNode)
                    {
                        foreach (var child in parentNode.Items.OfType<NodeCollection>())
                        {
                            if (child.NodeHeader?.HeaderText == eParentCollectionName)
                            {
                                dragParentCollection = child;
                                break;
                            }
                        }
                        if (dragParentCollection != null) break;
                    }
                }
            }
            else
            {
                dragParentCollection = _dragNode.GetNodeCollection();
            }

            // Get drop target.
            if (e.OriginalSource == null) { e.Handled = true; return; }
            if (dragParentCollection == null) { e.Handled = true; return; }
            var dropTarget = FindAncestor<Node>((DependencyObject)e.OriginalSource);

            //e.Effects = DragDropEffects.Move;

            if (dropTarget == null)
            {
                var k = e.GetPosition(this);
                if (k.Y >= Math.Floor(this.DesiredSize.Height))
                {
                    if (_insertionAdorner != null) { _insertionAdorner.Detach(); }
                    AdornerLayer lyr = AdornerLayer.GetAdornerLayer(dragParentCollection);
                    if (lyr != null) { _insertionAdorner = new InsertionAdorner(true, false, dragParentCollection, lyr); }
                }
                else
                {
                    if (_insertionAdorner != null)
                    {
                        _insertionAdorner.Detach();
                        _insertionAdorner = null;
                    }
                    e.Effects = DragDropEffects.None;
                }
            }
            else if (_dragNode != null && (_dragNode.ContainsNode(dropTarget) || _dragNode.Equals(dropTarget)))
            {
                if (_insertionAdorner != null)
                {
                    _insertionAdorner.Detach();
                    _insertionAdorner = null;
                }
                e.Effects = DragDropEffects.None;
            }
            else if (dropTarget as NodeCollection != null)
            {
                if (dragParentCollection.Equals(dropTarget))
                {
                    if (_insertionAdorner != null) { _insertionAdorner.Detach(); }
                    AdornerLayer lyr = AdornerLayer.GetAdornerLayer(dropTarget.NodeHeader);
                    if (lyr != null) { _insertionAdorner = new InsertionAdorner(true, false, dropTarget.NodeHeader, lyr); }
                }
                else
                {
                    if (_insertionAdorner != null) { _insertionAdorner.Detach(); }
                    AdornerLayer lyr = AdornerLayer.GetAdornerLayer(dragParentCollection);
                    if (lyr != null) { _insertionAdorner = new InsertionAdorner(true, false, dragParentCollection, lyr); }

                }
                e.Effects = DragDropEffects.Move;
            }
            else if (dropTarget as NodeGroup != null)
            {
                NodeCollection? dropParentCollection = dropTarget.GetNodeCollection();
                if (dragParentCollection.Equals(dropParentCollection))
                {
                    if (_insertionAdorner != null) { _insertionAdorner.Detach(); }
                    AdornerLayer lyr = AdornerLayer.GetAdornerLayer(dropTarget.NodeHeader);
                    if (lyr != null) { _insertionAdorner = new InsertionAdorner(true, IsInTopHalf(dropTarget, e.GetPosition(dropTarget)), dropTarget.NodeHeader, lyr); }
                }
                else
                {
                    if (_insertionAdorner != null) { _insertionAdorner.Detach(); }
                    AdornerLayer lyr = AdornerLayer.GetAdornerLayer(dragParentCollection);
                    if (lyr != null) { _insertionAdorner = new InsertionAdorner(true, false, dragParentCollection, lyr); }
                }
                e.Effects = DragDropEffects.Move;
            }
            else if (dropTarget as ProjectNode != null)
            {
                if (_insertionAdorner != null) { _insertionAdorner.Detach(); }
                AdornerLayer lyr = AdornerLayer.GetAdornerLayer(dragParentCollection.NodeHeader);
                if (lyr != null) { _insertionAdorner = new InsertionAdorner(true, false, dragParentCollection.NodeHeader, lyr); }
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                NodeCollection? dropParentCollection = dropTarget.GetNodeCollection();
                if (dragParentCollection.Equals(dropParentCollection))
                {
                    if (_insertionAdorner != null) { _insertionAdorner.Detach(); }
                    AdornerLayer lyr = AdornerLayer.GetAdornerLayer(dropTarget.NodeHeader);
                    if (lyr != null) { _insertionAdorner = new InsertionAdorner(true, IsInTopHalf(dropTarget, e.GetPosition(dropTarget)), dropTarget.NodeHeader, lyr); }
                }

                else
                {
                    if (_insertionAdorner != null) { _insertionAdorner.Detach(); }
                    AdornerLayer lyr = AdornerLayer.GetAdornerLayer(dragParentCollection);
                    if (lyr != null) { _insertionAdorner = new InsertionAdorner(true, false, dragParentCollection, lyr); }
                }
                e.Effects = DragDropEffects.Move;
            }

            e.Handled = true;
        }

        /// <summary>
        /// On drag leave, check if the treeview is still a target. Remove adorner if not.
        /// </summary>
        private void Me_DragLeave(object sender, DragEventArgs e)
        {
            // If the drag leave event has left the treeview then remove the adorner.
            // This doesn't work if you flick the mouse quickly out of the treeview. I am not sure the best way to handle it since the drag event captures the mouse.
            if (e.Source.GetType() == typeof(FrameworkUI.ProjectExplorer.ProjectExplorerTreeView))
            {
                if (_insertionAdorner != null)
                {
                    _insertionAdorner.Detach();
                    _insertionAdorner = null;
                }
                return;
            }

            var mp = e.GetPosition(this);
            if (mp.X >= this.ActualWidth || mp.X <= 0)
            {
                if (_insertionAdorner != null)
                {
                    _insertionAdorner.Detach();
                    _insertionAdorner = null;
                }
                return;
            }

            if (mp.Y >= this.ActualHeight || mp.Y <= 0)
            {
                if (_insertionAdorner != null)
                {
                    _insertionAdorner.Detach();
                    _insertionAdorner = null;
                }
                return;
            }
        }

        /// <summary>
        /// On drop, move or copy the element.
        /// </summary>
        private void Me_Drop(object sender, DragEventArgs e)
        {
            // Remove insertion adorner
            if (_insertionAdorner != null) { _insertionAdorner.Detach(); }
            _insertionAdorner = null;
            e.Effects = DragDropEffects.None;

            // Capture _dragNode in a local before any nulling so subsequent tests use the local.
            var dragNode = _dragNode;
            _dragNode = null;

            if (e.OriginalSource == null) { e.Handled = true; return; }
            var dropTarget = FindAncestor<Node>((DependencyObject)e.OriginalSource);

            // Get drag element collection
            NodeCollection? dragParentCollection = null;
            string eName = "";
            string elementType = "";
            string eParentProjectName = "";
            if (dragNode == null)
            {
                // Get drag data text and see if this is an element node or element node group
                if (e.Data == null) { e.Handled = true; return; }
                var data = e.Data.GetData(DataFormats.Text);
                if (data == null) { e.Handled = true; return; }

                var dataString = data.ToString()!.Split('>');
                if (dataString.Count() < 6) { e.Handled = true; return; }
                eName = dataString[1];
                elementType = dataString[2];
                // dataString[3] is the parent collection header name (e.g. "Input Data")
                string eParentCollectionName = dataString[3];
                eParentProjectName = dataString[4];
                bool canCopyFromExternal = false;
                bool.TryParse(dataString[5], out canCopyFromExternal);
                if (canCopyFromExternal == false) { e.Handled = true; return; }
                foreach (var item in Items)
                {
                    if (item is NodeCollection nc && nc.NodeHeader?.HeaderText == eParentCollectionName)
                    {
                        dragParentCollection = nc;
                        break;
                    }
                    // Search children (NodeCollections are children of the root ProjectNode)
                    if (item is Node parentNode)
                    {
                        foreach (var child in parentNode.Items.OfType<NodeCollection>())
                        {
                            if (child.NodeHeader?.HeaderText == eParentCollectionName)
                            {
                                dragParentCollection = child;
                                break;
                            }
                        }
                        if (dragParentCollection != null) break;
                    }
                }
                if (dragParentCollection is not ElementNodeCollection enc || enc.ElementCollection == null) { e.Handled = true; return; }
                enc.ElementCollection.InsertFromExternalProject(enc.ElementCollection.Count, eName, elementType, eParentProjectName);
                if (enc.ElementCollection.Count == 0) { e.Handled = true; return; }
                dragNode = ElementNode.FindElementNode(enc.ElementCollection[enc.ElementCollection.Count - 1], enc);
                if (dragNode == null) { e.Handled = true; return; }
            }
            else
            {
                dragParentCollection = dragNode.GetNodeCollection();
            }

            // Null checks for dragNode and dragParentCollection
            if (dragNode == null || dragParentCollection == null) { e.Handled = true; return; }

            if (dropTarget == null)
            {
                var k = e.GetPosition(this);
                if (k.Y >= Math.Floor(this.DesiredSize.Height))
                {
                    dragNode.Move(dragNode.ParentNode!, dragParentCollection, dragNode.ParentNode!.Items.IndexOf(dragNode), dragParentCollection.Items.Count); // Insert into the bottom of the list
                }
            }
            else if (dropTarget as NodeCollection != null)
            {
                if (dragParentCollection.Equals(dropTarget))
                {
                    dragNode.Move(dragNode.ParentNode!, dragParentCollection, dragNode.ParentNode!.Items.IndexOf(dragNode), 0); // Insert into the top of the list
                }
                else
                {
                    dragNode.Move(dragNode.ParentNode!, dragParentCollection, dragNode.ParentNode!.Items.IndexOf(dragNode), dragParentCollection.Items.Count); // Insert into the bottom of the list
                }
            }
            else if (dropTarget as ProjectNode != null)
            {
                dragNode.Move(dragNode.ParentNode!, dragParentCollection, dragNode.ParentNode!.Items.IndexOf(dragNode), 0); // Insert into the top of the list
            }
            else
            {
                NodeCollection? dropParentCollection = dropTarget.GetNodeCollection();
                if (dragParentCollection.Equals(dropParentCollection))
                {
                    int dragIndex = dragNode.ParentNode!.Items.IndexOf(dragNode);
                    bool firstHalf = IsInTopHalf(dropTarget, e.GetPosition(dropTarget));
                    if (dropTarget as NodeGroup != null && firstHalf == false)
                    {
                        dragNode.Move(dragNode.ParentNode!, dropTarget, dragIndex, 0);
                    }
                    else
                    {
                        int dropIndex = dropTarget.ParentNode!.Items.IndexOf(dropTarget);
                        if (firstHalf == false) { dropIndex += 1; }
                        if (dropTarget.ParentNode!.Equals(dragNode.ParentNode) && dragIndex < dropIndex) { dropIndex -= 1; }
                        dragNode.Move(dragNode.ParentNode!, dropTarget.ParentNode!, dragIndex, dropIndex);
                    }
                }
                else
                {
                    dragNode.Move(dragNode.ParentNode!, dragParentCollection, dragNode.ParentNode!.Items.IndexOf(dragNode), dragParentCollection.Items.Count); // Insert into the bottom of the list
                }
            }

            e.Handled = true;
        }

        #endregion

        #region Support

        /// <summary>
        /// Determines if the mouse is in the first half of the tree view item.
        /// </summary>
        /// <param name="dropTarget">The drop target node.</param>
        /// <param name="mousePosition">The current mouse position.</param>
        /// <returns>True if the mouse is in the top half of the node, otherwise false.</returns>
        private bool IsInTopHalf(Node dropTarget, Point mousePosition)
        {
            return mousePosition.Y < dropTarget.NodeHeader.ActualHeight / 2d;
        }

        /// <summary>
        /// Helper method to search for ancestors up the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of ancestor to find.</typeparam>
        /// <param name="current">The starting dependency object.</param>
        /// <returns>The ancestor of type T if found, otherwise null.</returns>
        public T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            do
            {
                if (current is T) return (T)current;
                current = VisualTreeHelper.GetParent(current!);
            }
            while (current != null);
            return null;
        }

        /// <summary>
        /// Helper method to search for parent tree view item.
        /// </summary>
        /// <param name="tree">The tree view to search in.</param>
        /// <param name="dependencyObject">The dependency object to start searching from.</param>
        /// <returns>The parent TreeViewItem if found, otherwise null.</returns>
        public static TreeViewItem? FindTreeViewItem(TreeView tree, DependencyObject? dependencyObject)
        {
            if (!(dependencyObject is Visual || dependencyObject is Visual3D)) return null;
            TreeViewItem? treeViewItem = dependencyObject as TreeViewItem;
            if (treeViewItem != null) return treeViewItem;
            return FindTreeViewItem(tree, VisualTreeHelper.GetParent(dependencyObject));
        }

        /// <summary>
        /// Searches upward in the visual tree for a TreeViewItem.
        /// </summary>
        /// <param name="source">The source dependency object.</param>
        /// <returns>The parent TreeViewItem if found, otherwise null.</returns>
        static TreeViewItem? VisualUpwardSearch(DependencyObject? source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);
            return source as TreeViewItem;
        }

        #endregion


    }
}