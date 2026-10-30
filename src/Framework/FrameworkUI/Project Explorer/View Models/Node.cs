using GenericControls;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WpfCompat;
using WpfCompat.Input;

namespace FrameworkUI.ProjectExplorer
{
    /// <summary>
    /// Represents a base class for nodes in the project explorer tree view.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This abstract class provides the foundation for all tree view nodes, including:
    /// <list type="bullet">
    /// <item><description>Selection and multi-selection support</description></item>
    /// <item><description>In-place rename editing</description></item>
    /// <item><description>Drag-and-drop functionality</description></item>
    /// <item><description>Context menu support</description></item>
    /// <item><description>Node grouping capabilities</description></item>
    /// </list>
    /// </para>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// <para>
    /// XGoodFit port:
    /// <list type="bullet">
    /// <item><description>The dependency properties are styled properties with the same defaults and
    /// callbacks. Avalonia's TreeViewItem has no Selected event: <see cref="Me_Selected"/> handles
    /// Avalonia's IsSelectedChanged event, raised and bubbled as WPF's Selected is, for items that
    /// became selected.</description></item>
    /// <item><description>The mouse handlers are WpfCompat's WPF mouse events, with Avalonia's pointer
    /// arguments. The right-button release is handled even once handled, because Avalonia marks it
    /// handled when it opens the node's context menu, which WPF does only afterwards.</description></item>
    /// <item><description>The slow double click that starts renaming waits for the platform's
    /// double-click time (Avalonia's platform settings), where upstream called user32's
    /// GetDoubleClickTime (migration plan §2.3). <see cref="_hitTestResult"/> is the visual hit, as
    /// Avalonia has no HitTestResult.</description></item>
    /// <item><description>Upstream brought the rename text box into focus by simulating a key press,
    /// a hack its comments ask to replace; the port focuses the text box.</description></item>
    /// <item><description>WPF's Items.Refresh() isn't called: Avalonia's items follow ChildNodes, an
    /// observable collection, as it changes.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract class Node : TreeViewItem, INotifyPropertyChanged
    {
        #region Native Methods

        /// <summary>
        /// Gets the system double-click time in milliseconds.
        /// </summary>
        /// <returns>The double-click time in milliseconds.</returns>
        /// <remarks>
        /// XGoodFit port: the platform's double-click time, from Avalonia's platform settings, rather
        /// than user32's GetDoubleClickTime, which is Windows-only. Without settings it is Windows'
        /// default, 500 milliseconds.
        /// </remarks>
        private static uint GetDoubleClickTime() =>
            (uint)(Application.Current?.PlatformSettings?.GetDoubleTapTime(PointerType.Mouse).TotalMilliseconds ?? 500d);

        #endregion

        #region Fields

        /// <summary>
        /// Result of the last hit test operation.
        /// </summary>
        /// <remarks>XGoodFit port: the visual hit (WPF's <c>HitTestResult.VisualHit</c>).</remarks>
        protected Visual? _hitTestResult;

        /// <summary>
        /// Indicates whether the node is currently in edit (rename) mode.
        /// </summary>
        protected bool _isInEditMode = false;

        /// <summary>
        /// Timer used for slow double-click rename detection.
        /// </summary>
        protected DispatcherTimer? _timer;

        /// <summary>
        /// Counter for timer ticks.
        /// </summary>
        protected int _tickCount = 0;

        /// <summary>
        /// Characters that are not allowed in node names.
        /// </summary>
        protected char[] _invalidNameChars = NameTextBox.GetDefaultInvalidCharacters();

        /// <summary>
        /// The node header control that displays the node's visual representation.
        /// </summary>
        protected readonly NodeHeader _nodeHeader = new NodeHeader();

        #endregion

        // Context menu items.
        private readonly MenuItem _groupMenuItem = new MenuItem() { Header = "Group" };
        private readonly Separator _collectionContentSeparator = new Separator();
        private readonly Separator _moveUpDownSeparator = new Separator();
        private readonly MenuItem _moveUpMenuItem = new MenuItem() { Header = "Move Up" };
        private readonly MenuItem _moveDownMenuItem = new MenuItem() { Header = "Move Down" };

        /// <summary>
        /// Host-supplied additional context-menu items shown on this node.
        /// </summary>
        protected readonly ObservableCollection<MenuItem> _customContextItems = new ObservableCollection<MenuItem>();

        /// <summary>
        /// Host-supplied additional context-menu items shown on collection-type nodes.
        /// </summary>
        protected readonly ObservableCollection<MenuItem> _collectionContextItems = new ObservableCollection<MenuItem>();

        /// <summary>
        /// Construct a new project explorer tree view node.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="parentTreeView">The parent tree view.</param>
        public Node(Node? parentNode, ExplorerTreeView? parentTreeView)
        {
            // Set Properties
            Header = _nodeHeader;
            IsReadOnly = true;
            if (parentNode != null) ParentNode = parentNode;
            if (parentTreeView != null) ParentTreeView = parentTreeView;
            ResetItemsSource();

            // Set up dynamic icon references for theme support
            var groupIcon = new Image();
            groupIcon.SetResourceReference(Image.SourceProperty, "GroupImage");
            _groupMenuItem.Icon = groupIcon;
            var moveUpIcon = new Image();
            moveUpIcon.SetResourceReference(Image.SourceProperty, "MoveUpImage");
            _moveUpMenuItem.Icon = moveUpIcon;
            var moveDownIcon = new Image();
            moveDownIcon.SetResourceReference(Image.SourceProperty, "MoveDownImage");
            _moveDownMenuItem.Icon = moveDownIcon;

            // Event Handlers
            _nodeHeader.RenameTextBox.InvalidCharacters = _invalidNameChars;
            _nodeHeader.RenameTextBox.LostFocus += HeaderRenameTextbox_LostFocus;
            _nodeHeader.HeaderCheckBox.AddCheckedHandler(NodeHeader_Checked);
            _nodeHeader.HeaderCheckBox.AddUncheckedHandler(NodeHeader_Unchecked);

            //{ StaticResource TreeViewItemStyle}

            // XGoodFit port: WPF's Selected event is Avalonia's IsSelectedChanged for an item that became
            // selected; both bubble from the item.
            AddHandler(SelectingItemsControl.IsSelectedChangedEvent, (sender, e) =>
            {
                if (e.Source is TreeViewItem { IsSelected: true }) Me_Selected(sender!, e);
            });
            // Unselected += Me_Unselected;
            this.AddPreviewMouseDoubleClickHandler(Me_PreviewMouseDoubleClick);
            this.AddPreviewMouseLeftButtonDownHandler(Me_PreviewMouseLeftButtonDown);
            this.AddPreviewMouseRightButtonDownHandler(Me_PreviewMouseRightButtonDown);
            // XGoodFit port: also when handled; Avalonia handles the release when the context menu opens.
            AddHandler(PointerReleasedEvent, (sender, e) =>
            {
                if (e.InitialPressMouseButton == MouseButton.Right) Me_MouseRightButtonUp(sender, e);
            }, RoutingStrategies.Bubble, handledEventsToo: true);
            this.AddMouseLeftButtonUpHandler(Me_MouseLeftButtonUp);
            Unloaded += Me_Unloaded;

            // Context Menu Items
            _groupMenuItem.Click += Group_Click;
            _moveUpMenuItem.Click += MoveNodeUp;
            _moveDownMenuItem.Click += MoveNodeDown;
            _customContextItems.CollectionChanged += (sender, ea) => SetContextMenu();
            _collectionContextItems.CollectionChanged += (sender, ea) => SetContextMenu();
            SetContextMenu();


            // Bindings
            _nodeHeader.SetBinding(NodeHeader.IsCheckBoxProperty, new Binding(nameof(IsCheckBoxNode)) { Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, Mode = BindingMode.TwoWay });
            _nodeHeader.SetBinding(NodeHeader.IsCheckedProperty, new Binding(nameof(IsChecked)) { Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, Mode = BindingMode.TwoWay });
            _nodeHeader.SetBinding(NodeHeader.IsExpandedProperty, new Binding(nameof(IsExpanded)) { Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, Mode = BindingMode.TwoWay });
        }

        /// <summary>
        /// Gets or sets the context menu items custom to the concrete implementation.
        /// </summary>
        public ObservableCollection<MenuItem> CustomContextItems { get => _customContextItems; }

        /// <summary>
        /// The node header. 
        /// </summary>
        public NodeHeader NodeHeader { get => _nodeHeader; }

        /// <summary>
        /// The parent node of this node.
        /// </summary>
        public Node? ParentNode { get; set; }

        /// <summary>
        /// The parent tree view dependency property.
        /// </summary>
        public static readonly StyledProperty<ExplorerTreeView?> ParentTreeViewProperty = AvaloniaProperty.Register<Node, ExplorerTreeView?>(nameof(ParentTreeView), null);

        static Node()
        {
            // XGoodFit port: the property-changed callbacks of upstream's metadata.
            ParentTreeViewProperty.Changed.AddClassHandler<Node>((d, e) => ParentTreeView_PropertyChangedCallback(d, e));
            IsCheckBoxNodeProperty.Changed.AddClassHandler<Node>((d, e) => IsCheckBoxNode_PropertyChangedCallback(d, e));
        }

        /// <summary>
        /// The parent tree view callback.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event data.</param>
        private static void ParentTreeView_PropertyChangedCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d as Node == null) return;
            Node thisControl = (Node)d;

            // Get the old value and remove any handlers
            ExplorerTreeView? oldTreeView = null;
            oldTreeView = e.OldValue as ExplorerTreeView;

            // Get the new value
            ExplorerTreeView? newTreeView = null;
            newTreeView = e.NewValue as ExplorerTreeView;

            // Set parent tree view for each child node
            thisControl.SetParentTreeView(newTreeView);
        }

        /// <summary>
        /// Recursively sets the parent tree view for this node and all child nodes.
        /// </summary>
        /// <param name="newTreeView">The tree view to set as parent.</param>
        private void SetParentTreeView(ExplorerTreeView? newTreeView)
        {
            // Set parent tree view for each child node
            foreach (var child in ChildNodes)
            {
                child.ParentTreeView = newTreeView;
                child.SetParentTreeView(newTreeView);
            }
        }

        /// <summary>
        /// The parent tree view.
        /// </summary>
        public ExplorerTreeView? ParentTreeView
        {
            get { return (ExplorerTreeView?)GetValue(ParentTreeViewProperty); }
            set { SetValue(ParentTreeViewProperty, value); }
        }

        /// <summary>
        /// The collection of child nodes. 
        /// </summary>
        public ObservableCollection<Node> ChildNodes { get; protected set; } = new ObservableCollection<Node>();

        /// <summary>
        /// Determines if the node is read only. If true, the node 
        /// cannot be renamed, moved, or drag-dropped by the user. 
        /// </summary>
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Dependency property for IsCheckBoxNode.
        /// </summary>
        public static readonly StyledProperty<bool> IsCheckBoxNodeProperty = AvaloniaProperty.Register<Node, bool>(nameof(IsCheckBoxNode), false);

        /// <summary>
        /// Handles property changes for the <see cref="IsCheckBoxNodeProperty"/> dependency property.
        /// Propagates the new value to all child nodes.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event data.</param>
        private static void IsCheckBoxNode_PropertyChangedCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null) return;
            Node thisControl = (Node)d;
            // Get the new value
            var newValue = (bool)e.NewValue!;

            // Update all child nodes
            foreach (var child in thisControl.ChildNodes) { child.IsCheckBoxNode = newValue; }
        }

        /// <summary>
        /// Determines if the node is a check box node.
        /// </summary>
        public bool IsCheckBoxNode
        {
            get { return (bool)GetValue(IsCheckBoxNodeProperty); }
            set { SetValue(IsCheckBoxNodeProperty, value); }
        }

        /// <summary>
        /// Dependency property for IsChecked.
        /// </summary>
        public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<Node, bool>(nameof(IsChecked), false);

        /// <summary>
        /// Determines if the node is checked or not. 
        /// </summary>
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        /// <summary>
        /// Dependency property for IsNodeSelected.
        /// </summary>
        public static readonly StyledProperty<bool> IsNodeSelectedProperty = AvaloniaProperty.Register<Node, bool>(nameof(IsNodeSelected), false);

        /// <summary>
        /// Determines if the node is selected or not. This is used for multi-select functionality.
        /// </summary>
        public bool IsNodeSelected
        {
            get { return (bool)GetValue(IsNodeSelectedProperty); }
            set { SetValue(IsNodeSelectedProperty, value); }
        }

        /// <summary>
        /// Determines whether the node is in edit mode.
        /// </summary>
        public bool IsInEditMode
        {
            get { return _isInEditMode; }
            set
            {
                if (IsReadOnly || ParentTreeView == null ||
                    //ParentTreeView.SelectedNodes == null ||
                    //ParentTreeView.SelectedNodes.Count > 1 ||
                    Keyboard.Modifiers == ModifierKeys.Control ||
                    Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    if (_isInEditMode != false)
                    {
                        _isInEditMode = false;
                        EditModeChanged(false);
                        RaisePropertyChange(nameof(IsInEditMode));
                    }
                }
                else if (_isInEditMode != value)
                {
                    _isInEditMode = value;
                    EditModeChanged(value);
                    RaisePropertyChange(nameof(IsInEditMode));
                }
            }
        }

        /// <summary>
        /// Gets or sets the array of invalid characters for node names.
        /// </summary>
        public char[] InvalidNameChars
        {
            get { return _invalidNameChars; }
            set
            {
                _invalidNameChars = value;
                _nodeHeader.RenameTextBox.InvalidCharacters = _invalidNameChars;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this node type supports multi-selection.
        /// </summary>
        public abstract bool CanMultiSelect { get; }

        /// <summary>
        /// Event is raised when the node is checked.
        /// </summary>
        public event NodeCheckedEventHandler? NodeChecked;

        /// <summary>
        /// Event is raised when the node is checked. 
        /// </summary>
        /// <param name="node">The node that was checked.</param>
        public delegate void NodeCheckedEventHandler(Node node);

        /// <summary>
        /// Event is raised when the node is unchecked.
        /// </summary>
        public event NodeUncheckedEventHandler? NodeUnchecked;

        /// <summary>
        /// Event is raised when the node is unchecked. 
        /// </summary>
        /// <param name="node">The node that was unchecked.</param>
        public delegate void NodeUncheckedEventHandler(Node node);

        /// <summary>
        /// Event is raised before the node is moved.
        /// </summary>
        public event PreviewNodeMovedEventHandler? PreviewNodeMoved;

        /// <summary>
        /// Event is raised before the node is moved.
        /// </summary>
        /// <param name="node">The node to move.</param>
        public delegate void PreviewNodeMovedEventHandler(Node node);

        /// <summary>
        /// Event is raised when the node is moved.
        /// </summary>
        public event NodeMovedEventHandler? NodeMoved;

        /// <summary>
        /// Event is raised when the node is moved.
        /// </summary>
        /// <param name="node">The node to move.</param>
        public delegate void NodeMovedEventHandler(Node node);

        /// <summary>
        /// Event is raised when the node is sorted.
        /// </summary>
        public event NodeSortedEventHandler? NodeSorted;

        /// <summary>
        /// Event is raised when the node is sorted.
        /// </summary>
        /// <param name="node">The node that was sorted.</param>
        public delegate void NodeSortedEventHandler(Node node);

        /// <summary>
        /// Raise event when a property changes.
        /// </summary>
        /// <remarks>
        /// XGoodFit port: upstream's INotifyPropertyChanged event, which hides Avalonia's own
        /// PropertyChanged event for the node's Avalonia properties.
        /// </remarks>
        public new event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raise property changed event.
        /// </summary>
        /// <param name="propertyName">Name of property that changed.</param>
        protected void RaisePropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// when the custom context menu items change, update the context menu.
        /// </summary>
        protected void SetContextMenu()
        {
            if (ContextMenu != null)
            {
                ContextMenu.Items.Clear();
            }
            else
            {
                ContextMenu = new ContextMenu() { Focusable = false };
            }

            // Default Context Menu Strip
            var nodeCollection = GetNodeCollection();

            if (nodeCollection != null && nodeCollection.AllowGrouping == true) ContextMenu.Items.Add(_groupMenuItem);

            // Add custom items
            if (_customContextItems != null && _customContextItems.Count > 0)
            {
                foreach (var item in _customContextItems) { ContextMenu.Items.Add(item); }
            }
            if (_collectionContextItems != null && _collectionContextItems.Count > 0)
            {
                if (_customContextItems != null && _customContextItems.Count > 0) { ContextMenu.Items.Add(_collectionContentSeparator); }
                foreach (var item in _collectionContextItems) { ContextMenu.Items.Add(item); }
            }
            // 
            if (this.IsReadOnly == false)
            {
                ContextMenu.Items.Add(new Separator());
                ContextMenu.Items.Add(_moveUpMenuItem);
                ContextMenu.Items.Add(_moveDownMenuItem);
            }

        }

        /// <summary>
        /// Handles the click event for grouping selected element nodes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Group_Click(object? sender, RoutedEventArgs e)
        {
            ExplorerTreeView? parentTree = ParentTreeView;
            if (parentTree == null || parentTree.SelectedNodes == null || parentTree.SelectedNodes.Count == 0)
            {
                e.Handled = true;
                return;
            }

            // Get location of new group
            Node? parentNode = parentTree.SelectedNodes.FirstOrDefault()?.ParentNode;
            if (parentNode == null)
            {
                e.Handled = true;
                return;
            }
            int index = parentNode.ChildNodes.IndexOf(this);

            // add group to parent node
            var group = new NodeGroup(parentNode, parentTree);
            group.IsCheckBoxNode = parentNode.IsCheckBoxNode;
            parentNode.ChildNodes.Insert(index, group);

            // Add items to new group, and remove items from parent
            foreach (var node in parentTree.SelectedNodes)
            {
                node.ParentNode?.ChildNodes.RemoveAt(node.ParentNode.ChildNodes.IndexOf(node));
                group.ChildNodes.Add(node);
            }

            // Clear selected elements
            parentTree.ClearSelection();

            // update group nodes
            foreach (var node in parentTree.SelectedNodes) { node.ParentNode?.ResetItemsSource(); }
            group.ResetItemsSource();
            group.IsSelected = true;
            group.IsInEditMode = true;
            var nodeCollection = GetNodeCollection();
            if (nodeCollection != null) { nodeCollection.RaiseGroupAdded(group); }
            e.Handled = true;
        }

        /// <summary>
        /// Gets the drag text representation for drag-drop operations.
        /// </summary>
        /// <returns>The drag text string.</returns>
        public virtual string GetDragText()
        {
            return "";
        }

        /// <summary>
        /// Raise node sorted event.
        /// </summary>
        /// <param name="node">The node that sorted.</param>
        protected void RaiseNodeSorted(Node node)
        {
            NodeSorted?.Invoke(node);
        }

        /// <summary>
        /// Reset the node items source.
        /// </summary>
        public void ResetItemsSource()
        {
            ItemsSource = null;
            for (int i = 0; i < ChildNodes.Count; i++) { ChildNodes[i].ParentNode = this; }
            ItemsSource = ChildNodes;
        }

        /// <summary>
        /// Sorts the child node in the entire collection by name given a specified sort direction.
        /// </summary>
        /// <param name="order">Optional. Ascending or descending order. Default = Ascending.</param>
        public void Sort(ListSortDirection order = ListSortDirection.Ascending)
        {
            List<Node> sorted;
            if (order == ListSortDirection.Ascending)
            {
                sorted = ChildNodes.OrderBy(n => n._nodeHeader.HeaderText).ToList();
            }
            else
            {
                sorted = ChildNodes.OrderByDescending(n => n._nodeHeader.HeaderText).ToList();
            }
            // Sort in-place using Move to preserve existing bindings and listeners on ChildNodes.
            for (int i = 0; i < sorted.Count; i++)
            {
                int currentIndex = ChildNodes.IndexOf(sorted[i]);
                if (currentIndex != i)
                    ChildNodes.Move(currentIndex, i);
            }
            // XGoodFit port: no Items.Refresh(); the items follow ChildNodes' moves.
            NodeSorted?.Invoke(this);
        }

        /// <summary>
        /// Get the node collection that contains this node.
        /// </summary>
        /// <returns>The parent NodeCollection, or null if not found.</returns>
        public NodeCollection? GetNodeCollection()
        {
            NodeCollection? nodeCollection = null;
            Node? parentNode = ParentNode;
            do
            {
                if (parentNode == null) { break; }

                if (parentNode as NodeCollection != null)
                {
                    nodeCollection = (NodeCollection)parentNode;
                    break;
                }
                parentNode = parentNode.ParentNode;
            } while (true);
            return nodeCollection;
        }

        #region Node Checked-Unchecked

        /// <summary>
        /// The node was checked.
        /// </summary>
        protected virtual void NodeHeader_Checked(object? sender, RoutedEventArgs e)
        {
            NodeChecked?.Invoke(this);
            e.Handled = true;
        }

        /// <summary>
        /// The node was unchecked.
        /// </summary>
        protected virtual void NodeHeader_Unchecked(object? sender, RoutedEventArgs e)
        {
            NodeUnchecked?.Invoke(this);
            e.Handled = true;
        }

        #endregion

        /// <summary>
        /// Handles a key press as WPF's tree view item does: Enter neither expands nor collapses the node.
        /// </summary>
        /// <remarks>
        /// XGoodFit port: Avalonia's TreeViewItem toggles its expansion on Enter, and so a parent's on
        /// a child's unhandled Enter: Enter, which ends a rename or edits an element, would collapse the
        /// collection. WPF's TreeViewItem has no Enter key, so the nodes' own Enter handlers are all.
        /// </remarks>
        /// <param name="e">The event data.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter) return;
            base.OnKeyDown(e);
        }

        #region Rename Node

        /// <summary>
        /// Handles the selected event for the node.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        protected virtual void Me_Selected(object sender, RoutedEventArgs e)
        {
            IsInEditMode = false;
            var node = this;
            if (ParentTreeView?.Items != null)
            {
                ClearRenameTextBoxes(ParentTreeView.Items, ref node);
            }
            e.Handled = true; // handled to keep from bubbling up to parent nodes.
        }

        /// <summary>
        /// On preview mouse left click, if node is not selected, then left click is handled.
        /// </summary>
        protected virtual void Me_PreviewMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            // Retrieve the coordinate of the mouse position.
            var pt = e.GetPosition((Visual)sender!);
            // Perform the hit test against a given portion of the visual object tree.
            // XGoodFit port: Avalonia's GetVisualAt is WPF's VisualTreeHelper.HitTest.
            _hitTestResult = _nodeHeader.RenameTextBox.GetVisualAt(pt);
        }

        /// <summary>
        /// Updates tree selection before a right-click context menu opens.
        /// </summary>
        /// <param name="sender">The node that raised the event.</param>
        /// <param name="e">The mouse-button event data.</param>
        private void Me_PreviewMouseRightButtonDown(object? sender, PointerPressedEventArgs e)
        {
            // ExplorerTreeView pTree = ParentTreeView as ExplorerTreeView;

            if (ParentTreeView == null) return;
            var originalSource = e.OriginalSource as AvaloniaObject;
            if (originalSource == null) return;
            var tItem = ExplorerTreeView.FindTreeViewItem(ParentTreeView, originalSource);
            if (tItem != null && tItem.Equals(this) == false) { return; }
            int selectedCount = ParentTreeView.SelectedNodes.Count;

            if ((Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift) && this.CanMultiSelect)
            {
                if (ParentTreeView.SelectedNodes.Contains(this) == false)
                {
                    IsNodeSelected = true;
                    ParentTreeView.SelectedNodes.Add(this);
                }
            }
            if (ParentTreeView.SelectedNodes.Contains(this) == false)
            {
                ParentTreeView.ClearSelection();
                IsNodeSelected = true;
                ParentTreeView.SelectedNodes.Add(this);
            }

            if (selectedCount > 1)
            {
                // If multiple nodes are selected, limit context menu options
                _groupMenuItem.Visibility = Visibility.Visible;
                foreach (var item in _customContextItems) { item.Visibility = Visibility.Collapsed; }
                foreach (var item in _collectionContextItems) { item.Visibility = Visibility.Collapsed; }

                _moveUpDownSeparator.Visibility = Visibility.Collapsed;
                _collectionContentSeparator.Visibility = Visibility.Collapsed;
                _moveUpMenuItem.Visibility = Visibility.Collapsed;
                _moveDownMenuItem.Visibility = Visibility.Collapsed;
            }
            else
            {
                _groupMenuItem.Visibility = Visibility.Collapsed;
                // _editMenuItem.Visibility = Visibility.Visible;
                // _copyMenuItem.Visibility = Visibility.Visible;
                // _renameMenuItem.Visibility = Visibility.Visible;
                // _deleteMenuItem.Visibility = Visibility.Visible;

                //_customContentSeparator.Visibility = Visibility.Visible;
                foreach (var item in _customContextItems) { item.Visibility = Visibility.Visible; }
                foreach (var item in _collectionContextItems) { item.Visibility = Visibility.Visible; }
                _moveUpDownSeparator.Visibility = Visibility.Visible;
                _collectionContentSeparator.Visibility = Visibility.Visible;
                _moveUpMenuItem.Visibility = Visibility.Visible;
                _moveDownMenuItem.Visibility = Visibility.Visible;
                // Select node and bring into focus
                if (IsSelected == false) { IsSelected = true; }
                Focus();
                ParentTreeView.UpdateLayout();
            }

        }

        /// <summary>
        /// Handles the preview mouse double click event to cancel rename mode.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        protected virtual void Me_PreviewMouseDoubleClick(object? sender, PointerPressedEventArgs e)
        {
            StopTimer();
        }

        /// <summary>
        /// When user right clicks, update rename text box.
        /// </summary>
        protected virtual void Me_MouseRightButtonUp(object? sender, PointerReleasedEventArgs e)
        {
            UpdateRenameTextBox();
        }

        /// <summary>
        /// Handles the left mouse button up event for slow double-click rename detection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        protected virtual void Me_MouseLeftButtonUp(object? sender, PointerReleasedEventArgs e)
        {
            if (IsReadOnly) return;
            if (ParentTreeView == null) return;

            var originalSource = e.OriginalSource as AvaloniaObject;
            if (originalSource == null) return;
            var tItem = ExplorerTreeView.FindTreeViewItem(ParentTreeView, originalSource);
            if (tItem != null && !tItem.Equals(this)) { return; }

            bool timerEnabled = _timer?.IsEnabled ?? false;

            if (!IsInEditMode && !timerEnabled)
            {
                // Clean up old timer if it exists
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Tick -= Timer_Tick;
                }

                // Start slow double click timer
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(GetDoubleClickTime())
                };
                _timer.Tick += Timer_Tick;
                _timer.IsEnabled = true;
                _timer.Start();
                e.Handled = true;
            }
            else if (!IsInEditMode && timerEnabled && _tickCount <= 1)
            {
                // Start rename edit
                IsInEditMode = true;
                e.Handled = true;
            }
            else if (IsInEditMode)
            {
                StopTimer();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Stops and cleans up the timer, removing event handlers to prevent memory leaks.
        /// </summary>
        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.IsEnabled = false;
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
        }

        /// <summary>
        /// Handles the Unloaded event to clean up resources and prevent memory leaks.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Me_Unloaded(object? sender, RoutedEventArgs e)
        {
            StopTimer();
        }

        /// <summary>
        /// Handles the timer tick event for slow double-click rename detection.
        /// Stops after 2 ticks if no second click is received.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        protected virtual void Timer_Tick(object? sender, EventArgs e)
        {
            _tickCount++;
            if (_tickCount >= 2)
            {
                StopTimer();
                _tickCount = 0;
            }
        }

        /// <summary>
        /// Returns a list of node names that would be invalid for renaming this node.
        /// </summary>
        /// <returns>A list of names that are already used by sibling nodes.</returns>
        protected virtual List<string> GetInvalidNodeNames()
        {
            var invalidNames = new List<string>();
            if (ParentNode?.ChildNodes == null) return invalidNames;

            for (int i = 0; i < ParentNode.ChildNodes.Count; i++)
            {
                var siblingHeaderText = ParentNode.ChildNodes[i].NodeHeader?.HeaderText;
                if (siblingHeaderText != null && siblingHeaderText != NodeHeader?.HeaderText)
                {
                    invalidNames.Add(siblingHeaderText);
                }
            }
            return invalidNames;
        }

        /// <summary>
        /// Show rename text box if edit mode is true.
        /// </summary>
        /// <param name="value">Is in edit mode.</param>
        protected virtual void EditModeChanged(bool value)
        {
            if (IsReadOnly == true) return;
            if (value == true)
            {
                // Update text box and select all text
                _nodeHeader.RenameTextBox.InvalidStrings = GetInvalidNodeNames().ToArray();
                _nodeHeader.RenameText = _nodeHeader.HeaderText;
                _nodeHeader.ShowRenameTextBox = true;
                _nodeHeader.RenameTextBox.SelectAll();

                // !!**!!
                // This is a hack to bring the text box into focus.
                // I have not found a better way to do this.
                // Please help if you know of a better way!

                // First, Update Layout of TreeView.
                ParentTreeView?.UpdateLayout();

                // Next, simulate a key stroke to bring text box into focus.
                // XGoodFit port: rather than simulating a key press through WPF's input manager, focus
                // the rename text box's editor, which the key press did.
                _nodeHeader.RenameTextBox.GetLogicalDescendants().OfType<TextBox>().FirstOrDefault()?.Focus();
            }
            else
            {
                _nodeHeader.ShowRenameTextBox = false;
            }
        }

        /// <summary>
        /// Updates the rename text box with the current text and exits edit mode.
        /// </summary>
        protected virtual void UpdateRenameTextBox()
        {
            if (IsReadOnly) return;
            if (IsInEditMode)
            {
                if (_nodeHeader.RenameTextBox.IsValid && _nodeHeader.HeaderText != _nodeHeader.RenameText)
                {
                    _nodeHeader.HeaderText = _nodeHeader.RenameText;
                }
                StopTimer();
            }
            IsInEditMode = false;
        }

        /// <summary>
        /// When header loses focus, update rename text box.
        /// </summary>
        protected virtual void HeaderRenameTextbox_LostFocus(object? sender, RoutedEventArgs e)
        {
            if (_hitTestResult == null || !(_hitTestResult is TextBox))
            {
                UpdateRenameTextBox();
            }
        }

        /// <summary>
        /// Recursive routine used to de-select all items.
        /// </summary>
        /// <param name="treeViewItems">The collection of tree view items to process.</param>
        /// <param name="originalNode">The original node to preserve.</param>
        private void ClearRenameTextBoxes(IEnumerable treeViewItems, ref Node originalNode)
        {
            if (treeViewItems != null)
            {
                foreach (var item in treeViewItems)
                {
                    Node? node = item as Node;
                    if (node != null)
                    {
                        if (!node.Equals(originalNode)) node.UpdateRenameTextBox();
                        if (node.HasItems) ClearRenameTextBoxes(node.Items, ref originalNode);
                    }
                }
            }
        }

        /// <summary>
        /// The user clicked out into white space, so update rename text box.
        /// </summary>
        public void UserClickedWhiteSpace()
        {
            UpdateRenameTextBox();
        }

        #endregion

        #region Move Node

        /// <summary>
        /// Move the node up in the collection.
        /// </summary>
        protected virtual void MoveNodeUp(object? sender, RoutedEventArgs e)
        {
            if (IsReadOnly) return;
            if (ParentNode == null) return;
            int startIndex = ParentNode.Items.IndexOf(this);
            if (startIndex == 0) return;
            Move(ParentNode, ParentNode, startIndex, startIndex - 1);
        }

        /// <summary>
        /// Move the node down in the collection.
        /// </summary>
        protected virtual void MoveNodeDown(object? sender, RoutedEventArgs e)
        {
            if (IsReadOnly) return;
            if (ParentNode == null) return;
            int startIndex = ParentNode.Items.IndexOf(this);
            if (startIndex == ParentNode.Items.Count - 1) return;
            Move(ParentNode, ParentNode, startIndex, startIndex + 1);
        }

        /// <summary>
        /// Move node within a node collection, optionally across parents.
        /// </summary>
        /// <param name="startParent">The parent node the node is currently under.</param>
        /// <param name="endParent">The parent node the node should move to (may equal <paramref name="startParent"/>).</param>
        /// <param name="startIndex">The start position of the node to move.</param>
        /// <param name="endIndex">The end position of the node to move.</param>
        public virtual void Move(Node startParent, Node endParent, int startIndex, int endIndex)
        {
            if (IsReadOnly) return;
            if (startParent == null || endParent == null) return;
            if (startParent.Equals(endParent) && startParent.ChildNodes.Count < 2) return;
            if (startParent.Equals(this) || endParent.Equals(this)) return;
            if (startIndex < 0 || startIndex > startParent.ChildNodes.Count - 1) return;
            if (endIndex < 0) return;
            PreviewNodeMoved?.Invoke(this);

            startParent.ChildNodes.RemoveAt(startIndex);
            if (endIndex >= endParent.ChildNodes.Count)
            {
                endParent.ChildNodes.Add(this);
            }
            else
            {
                endParent.ChildNodes.Insert(endIndex, this);
            }

            startParent.ResetItemsSource();
            if (!startParent.Equals(endParent)) endParent.ResetItemsSource();
            NodeMoved?.Invoke(this);
            IsSelected = true;
        }

        #endregion


        /// <summary>
        /// Expands all parent nodes up to the root of the tree.
        /// </summary>
        public void ExpandParentNodes()
        {
            if (ParentNode != null)
            {
                ParentNode.IsExpanded = true;
                ParentNode.ExpandParentNodes();
            }
        }

        /// <summary>
        /// Recursively searches for the node in children.
        /// </summary>
        /// <param name="node">The node to search for.</param>
        /// <returns>True if the node is found in the children, otherwise false.</returns>
        public bool ContainsNode(Node node)
        {
            foreach (var n in ChildNodes)
            {
                if (n.Equals(node)) { return true; }
                if (n.ContainsNode(node)) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Recursively searches for all node groups in the children.
        /// </summary>
        /// <returns>A list of all node groups found in the tree.</returns>
        public List<NodeGroup> GetNodeGroups()
        {
            List<NodeGroup> result = new List<NodeGroup>();
            foreach (var n in ChildNodes)
            {
                if (n as NodeGroup != null) { result.Add((NodeGroup)n); }
                result.AddRange(n.GetNodeGroups());
            }
            return result;
        }
    }
}
