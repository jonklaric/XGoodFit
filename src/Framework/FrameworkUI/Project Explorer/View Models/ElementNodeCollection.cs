using FrameworkInterfaces;
using Avalonia;
using Avalonia.Data;
using Avalonia.Threading;
using WpfCompat;

namespace FrameworkUI.ProjectExplorer
{

    /// <summary>
    /// Element node collection class.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// <para>
    /// XGoodFit port: ElementCollection is a styled property with upstream's callback. Clearing the header
    /// text's binding disposes it, as Avalonia's ClearValue leaves a binding in place. The UI thread is
    /// Avalonia's dispatcher.
    /// </para>
    /// </remarks>
    public class ElementNodeCollection : NodeCollection
    {

        /// <summary>
        /// Construct new element node collection.
        /// </summary>
        /// <param name="parentNode">The parent node.</param>
        /// <param name="parentTreeView">The parent tree view.</param>
        public ElementNodeCollection(Node? parentNode, ProjectExplorerTreeView? parentTreeView) : base(parentNode, parentTreeView)
        {

        }

        /// <summary>
        /// Dependency property for ElementCollection.
        /// </summary>
        public static readonly StyledProperty<IElementCollection?> ElementCollectionProperty = AvaloniaProperty.Register<ElementNodeCollection, IElementCollection?>(nameof(ElementCollection), null);

        static ElementNodeCollection()
        {
            // XGoodFit port: the property-changed callback of upstream's metadata.
            ElementCollectionProperty.Changed.AddClassHandler<ElementNodeCollection>((d, e) => ElementCollection_PropertyChangedCallback(d, e));
        }

        /// <summary>
        /// Element collection property callback.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event data.</param>
        private static void ElementCollection_PropertyChangedCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(ElementNodeCollection)) return;
            ElementNodeCollection thisControl = (ElementNodeCollection)d;

            // Get the old value and remove any handlers
            IElementCollection? oldValue = null;
            oldValue = e.OldValue as IElementCollection;
            // Remove handlers
            if (oldValue != null)
            {
                oldValue.ElementRemoved -= thisControl.ElementRemoved;
                oldValue.ElementAdded -= thisControl.ElementAdded;
            }

            // Clear collections in preparation for new value.
            thisControl.ChildNodes.Clear();
            BindingOperations.GetBindingExpressionBase(thisControl.NodeHeader, NodeHeader.HeaderTextProperty)?.Dispose();

            // Get the new value
            IElementCollection? newElementCollection = null;
            newElementCollection = e.NewValue as IElementCollection;
            if (newElementCollection == null) return;

            // Set binding for header text
            // XGoodFit port: Avalonia's bindings raise no SourceUpdated events, so NotifyOnSourceUpdated has no counterpart.
            var binding = new Binding(nameof(IElementCollection.Name)) { Source = newElementCollection, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, Mode = BindingMode.OneWay };
            thisControl.NodeHeader.Bind(NodeHeader.HeaderTextProperty, binding);

            // Create Element Nodes
            foreach (var element in newElementCollection)
                thisControl.Add(new ElementNode(element, thisControl, thisControl.ParentTreeView as ProjectExplorerTreeView), false);

            // Child element collections.
            if (newElementCollection.ElementCollections != null)
            {
                for (int i = 0; i < newElementCollection.ElementCollections.Count; i++)
                    thisControl.ChildNodes.Add(new ElementNodeCollection(thisControl, thisControl.ParentTreeView as ProjectExplorerTreeView) { ElementCollection = newElementCollection.ElementCollections[i], ParentTreeView = thisControl.ParentTreeView });
            }

            // Add handlers
            newElementCollection.ElementRemoved += thisControl.ElementRemoved;
            newElementCollection.ElementAdded += thisControl.ElementAdded; 
            thisControl.ResetItemsSource();
        }

        /// <summary>
        /// The IElementCollection property that defines the nodes to be added under the tree view item header.
        /// </summary>
        public IElementCollection? ElementCollection
        {
            get { return (IElementCollection?)GetValue(ElementCollectionProperty); }
            set { SetValue(ElementCollectionProperty, value); }
        }

        /// <summary>
        /// When an IElement is removed, update the element node collection.
        /// </summary>
        /// <param name="element">IElement to remove.</param>
        private void ElementRemoved(IElement element)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => ElementRemoved(element));
                return;
            }
            var node = ElementNode.FindElementNode(element, this);
            if (node == null) return;
            node.ParentNode?.ChildNodes.Remove(node);
            node.ParentNode?.ResetItemsSource();
        }

        /// <summary>
        /// When an IElement is added, update the element node collection.
        /// </summary>
        /// <param name="element">IElement to add.</param>
        private void ElementAdded(IElement element)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => ElementAdded(element));
                return;
            }
            if (ElementNode.FindElementNode(element, this)==null)
            {
                Add(new ElementNode(element, this, ParentTreeView as ProjectExplorerTreeView));
            }
        }
    }
}
