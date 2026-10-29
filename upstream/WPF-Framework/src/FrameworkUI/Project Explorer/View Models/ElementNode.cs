using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GenericControls;
using FrameworkInterfaces;

namespace FrameworkUI.ProjectExplorer
{

    /// <summary>
    /// Element node class.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public class ElementNode : Node
    {

        /// <summary>
        /// Construct a new element node.
        /// </summary>
        /// <param name="element">The project IElement.</param>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="parentTreeView">The parent tree view.</param>
        public ElementNode(IElement element, Node? parentNode, ProjectExplorerTreeView? parentTreeView) : base(parentNode, parentTreeView)
        {
            // Set Properties
            Element = element;
            AllowDrop = true;
            IsReadOnly = false;

            // Set up dynamic icon references for theme support
            var editIcon = new Image();
            editIcon.SetResourceReference(Image.SourceProperty, "EditWindowImage");
            _editMenuItem.Icon = editIcon;
            var copyIcon = new Image();
            copyIcon.SetResourceReference(Image.SourceProperty, "CopyImage");
            _copyMenuItem.Icon = copyIcon;
            var renameIcon = new Image();
            renameIcon.SetResourceReference(Image.SourceProperty, "RenameImage");
            _renameMenuItem.Icon = renameIcon;
            var deleteIcon = new Image();
            deleteIcon.SetResourceReference(Image.SourceProperty, "DeleteImage");
            _deleteMenuItem.Icon = deleteIcon;

            // Node Header Appearance
            NodeHeader.ShowToolTip = true;
            if (element.ElementImageResourceKey != null)
            {
                // Use dynamic resource binding so the icon updates when the theme changes
                NodeHeader.SetResourceReference(NodeHeader.StaticImageProperty, element.ElementImageResourceKey);
                NodeHeader.SetResourceReference(NodeHeader.ExpandedImageProperty, element.ElementImageResourceKey);
            }
            else
            {
                NodeHeader.StaticImage = element.ElementImage;
                NodeHeader.ExpandedImage = element.ElementImage;
            }

            // Event Handlers
            _editMenuItem.Click += Edit_Click;
            _copyMenuItem.Click += Copy_Click;
            _renameMenuItem.Click += Rename_Click;
            _deleteMenuItem.Click += Delete_Click;
            _customContextItems.Add(_editMenuItem);
            _customContextItems.Add(_copyMenuItem);
            _customContextItems.Add(_renameMenuItem);
            _customContextItems.Add(_deleteMenuItem);

            PreviewMouseRightButtonDown += Me_PreviewMouseRightButtonDown;
            MouseDoubleClick += Me_MouseDoubleClick;
            KeyDown += Me_KeyDown;

            // Set bindings
            NodeHeader.SetBinding(NodeHeader.HeaderTextProperty, new Binding(nameof(Element.Name)) { Source = Element, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, Mode = BindingMode.TwoWay });
            NodeHeader.SetBinding(NodeHeader.HeaderDescriptionProperty, new Binding(nameof(Element.Description)) { Source = Element, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, Mode = BindingMode.OneWay });
            NodeHeader.SetBinding(NodeHeader.ShowAsteriskProperty, new Binding(nameof(Element.IsDirty)) { Source = Element, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, Mode = BindingMode.OneWay });
        }

        /// <summary>
        /// Context menu items.
        /// </summary>
        private readonly MenuItem _editMenuItem = new MenuItem() { Header = "Edit...", InputGestureText = "Ctrl+E" };
        private readonly MenuItem _copyMenuItem = new MenuItem() { Header = "Copy...", InputGestureText = "Ctrl+C" };
        private readonly MenuItem _renameMenuItem = new MenuItem() { Header = "Rename...", InputGestureText = "F2" };
        private readonly MenuItem _deleteMenuItem = new MenuItem() { Header = "Delete...", InputGestureText = "Del" };

        /// <summary>
        /// Event is raised when the element node is selected.
        /// </summary>
        public event ActivateEventHandler? Activate;

        /// <summary>
        /// Event is raised when the element node is selected.
        /// </summary>
        /// <param name="element">The project IElement.</param>
        public delegate void ActivateEventHandler(IElement element);

        /// <summary>
        /// Event is raised when the element node is double-clicked, or if edit is clicked in the context menu.
        /// </summary>
        public event EditEventHandler? Edit;

        /// <summary>
        /// Event is raised when the element node is double-clicked, or if edit is clicked in the context menu.
        /// </summary>
        /// <param name="element">The project IElement.</param>
        public delegate void EditEventHandler(IList<IElement> element);

        /// <summary>
        /// Event is raised when copy is clicked in the context menu.
        /// </summary>
        public event CopyEventHandler? Copy;

        /// <summary>
        /// Event is raised when copy is clicked in the context menu.
        /// </summary>
        /// <param name="element">The project IElement.</param>
        public delegate void CopyEventHandler(IElement element);

        /// <summary>
        /// Event is raised when delete is clicked in the context menu.
        /// </summary>
        public event DeleteEventHandler? Delete;

        /// <summary>
        /// Event is raised when delete is clicked in the context menu.
        /// </summary>
        /// <param name="element">The project IElement.</param>
        public delegate void DeleteEventHandler(IList<IElement> element);

        /// <summary>
        /// Gets the node element.
        /// </summary>
        public IElement Element { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this node can be multi-selected with other nodes.
        /// </summary>
        public override bool CanMultiSelect => true;

        /// <summary>
        /// Get the element node collection that contains this node.
        /// </summary>
        public ElementNodeCollection? GetElementNodeCollection()
        {
            ElementNodeCollection? elementNodeCollection = null;
            Node? parentNode = ParentNode;
            while (parentNode != null)
            {
                if (parentNode is ElementNodeCollection collection)
                {
                    elementNodeCollection = collection;
                    break;
                }
                else if (parentNode is ProjectNode)
                {
                    break;
                }
                parentNode = parentNode.ParentNode;
            }
            return elementNodeCollection;
        }

        /// <summary>
        /// On select, edit mode = false. Raise the edit or activate event.
        /// </summary>
        protected override void Me_Selected(object sender, RoutedEventArgs e)
        {
            Activate?.Invoke(Element);
        }

        /// <summary>
        /// Gets the drag text representation of this element node for drag-drop operations.
        /// </summary>
        /// <returns>A formatted string containing element information for drag-drop operations.</returns>
        public override string GetDragText()
        {
            return nameof(ElementNode) + ">" + Element.Name + ">" + Element.GetType().ToString() + ">" + Element.ParentCollection.Name + ">" + Element.ParentCollection.ParentProject.FullFileName + ">" + Element.CanCopyFromExternal;
        }

        /// <summary>
        /// On preview mouse right click, select node and show context menu.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Me_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ParentTreeView == null) return;

            if (ParentTreeView.SelectedNodes.Count > 1)
            {
                _editMenuItem.Visibility = Visibility.Visible;
                _copyMenuItem.Visibility = Visibility.Collapsed;
                _renameMenuItem.Visibility = Visibility.Collapsed;
                _deleteMenuItem.Visibility = Visibility.Visible;
            }
            else
            {
                _editMenuItem.Visibility = Visibility.Visible;
                _copyMenuItem.Visibility = Visibility.Visible;
                _renameMenuItem.Visibility = Visibility.Visible;
                _deleteMenuItem.Visibility = Visibility.Visible;
            }

        }

        /// <summary>
        /// On mouse double click, raise edit event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Me_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && IsInEditMode == false)
            {
                Edit?.Invoke(new[] { Element });
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handles the node key down event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Me_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl + E = Edit
            if (e.Key == Key.E && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                if (ParentTreeView == null) return;
                var elements = ParentTreeView.SelectedNodes
                    .OfType<ElementNode>()
                    .Select(n => n.Element)
                    .ToList();
                Edit?.Invoke(elements);
                ParentTreeView.ClearSelection();
            }

            // Ctrl + C = Copy
            else if (e.Key == Key.C && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                Copy?.Invoke(Element);
            }

            // F2 = Rename
            else if (e.Key == Key.F2)
            {
                if (IsInEditMode == false)
                {
                    IsInEditMode = true;
                }
                e.Handled = true;
            }

            // Enter = Stop Renaming OR Edit
            else if (e.Key == Key.Enter)
            {
                bool wasEditing = IsInEditMode;
                UpdateRenameTextBox();
                if (IsSelected == true && wasEditing==false)
                {
                    Edit?.Invoke(new[] { Element });
                }
            }

            // Esc = Cancel Renaming
            else if (e.Key == Key.Escape)
            {
                IsInEditMode = false;
            }

            // Delete = Delete
            else if (e.Key == Key.Delete)
            {
                if (ParentTreeView == null) return;
                var elements = ParentTreeView.SelectedNodes
                    .OfType<ElementNode>()
                    .Select(n => n.Element)
                    .ToList();
                Delete?.Invoke(elements);
                ParentTreeView.ClearSelection();
            }
        }

        #region Context menu click methods

     
        /// <summary>
        /// On click, raise edit event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ParentTreeView == null) return;
            var elements = ParentTreeView.SelectedNodes
                .OfType<ElementNode>()
                .Select(n => n.Element)
                .ToList();
            Edit?.Invoke(elements);
            ParentTreeView.ClearSelection();
            e.Handled = true;
        }

        /// <summary>
        /// On click, raise copy event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Copy?.Invoke(Element);
            e.Handled = true;
        }

        /// <summary>
        /// On click, raise rename event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            IsInEditMode = true;
            e.Handled = true;
        }

        /// <summary>
        /// On click, raise delete event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ParentTreeView == null) return;
            var elements = ParentTreeView.SelectedNodes
                .OfType<ElementNode>()
                .Select(n => n.Element)
                .ToList();
            Delete?.Invoke(elements);
            ParentTreeView.ClearSelection();
            e.Handled = true;
        }

        #endregion
        /// <summary>
        /// Find the element node for a given element within a node.
        /// </summary>
        /// <param name="element">IElement to find the node for.</param>
        /// <param name="node">Node to search in.</param>
        /// <returns>The ElementNode if found, or null if not found.</returns>
        public static ElementNode? FindElementNode(IElement element, Node node)
        {
            foreach (Node child in node.ChildNodes)
            {
                var childElementNode = child as ElementNode;
                if (childElementNode != null && childElementNode.Element != null && childElementNode.Element.Equals(element))
                {
                    return childElementNode;
                }
                else
                {
                    var elementNode = FindElementNode(element, child);
                    if (elementNode != null) { return elementNode; }
                }
            }
            return null;
        }
    }
}
