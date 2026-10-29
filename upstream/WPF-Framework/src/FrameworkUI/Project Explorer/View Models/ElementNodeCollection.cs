using FrameworkInterfaces;
using System.Windows;
using System.Windows.Data;

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
        public static DependencyProperty ElementCollectionProperty = DependencyProperty.Register(nameof(ElementCollection), typeof(IElementCollection), typeof(ElementNodeCollection), new FrameworkPropertyMetadata(null, ElementCollection_PropertyChangedCallback));

        /// <summary>
        /// Element collection property callback.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event data.</param>
        private static void ElementCollection_PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
            BindingOperations.ClearBinding(thisControl.NodeHeader, NodeHeader.HeaderTextProperty);

            // Get the new value
            IElementCollection? newElementCollection = null;
            newElementCollection = e.NewValue as IElementCollection;
            if (newElementCollection == null) return;

            // Set binding for header text
            var binding = new Binding(nameof(IElementCollection.Name)) { Source = newElementCollection, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, NotifyOnSourceUpdated = true, Mode = BindingMode.OneWay };
            BindingOperations.SetBinding(thisControl.NodeHeader, NodeHeader.HeaderTextProperty, binding);

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
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ElementRemoved(element));
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
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ElementAdded(element));
                return;
            }
            if (ElementNode.FindElementNode(element, this)==null)
            {
                Add(new ElementNode(element, this, ParentTreeView as ProjectExplorerTreeView));
            }
        }
    }
}
