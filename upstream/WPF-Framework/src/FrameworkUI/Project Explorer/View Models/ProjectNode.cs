using GenericControls;
using FrameworkInterfaces;
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

namespace FrameworkUI.ProjectExplorer
{
    /// <summary>
    /// Project node class.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public abstract class ProjectNode : Node
    {

        /// <summary>
        /// Construct new project node based on IProject.
        /// </summary>
        /// <param name="project">Project as IProject.</param>
        public ProjectNode(IProject project) : base(null, null)
        {
            // Set Properties
            Project = project;
            IsExpanded = true;
            AllowDrop = true;
            IsReadOnly = true;

            // Node Header Appearance
            NodeHeader.ShowToolTip = true;
            NodeHeader.SetResourceReference(NodeHeader.StaticImageProperty, "ApplicationImage");
            NodeHeader.SetResourceReference(NodeHeader.ExpandedImageProperty, "ApplicationImage");
            NodeHeader.MouseLeftButtonUp += NodeHeader_MouseLeftButtonUp;

            // Set Bindings
            NodeHeader.SetBinding(NodeHeader.HeaderTextProperty, new Binding(nameof(Project.Name)) { Source = Project });
            NodeHeader.SetBinding(NodeHeader.HeaderDescriptionProperty, new Binding(nameof(Project.Description)) { Source = Project });
            NodeHeader.SetBinding(NodeHeader.ShowAsteriskProperty, new Binding(nameof(Project.IsDirty)) { Source = Project });

        }

        #region Members

        /// <summary>
        /// Event raised when the node is clicked.
        /// </summary>
        public event OnClickEventHandler? OnClick;

        /// <summary>
        /// Delegate for the OnClick event.
        /// </summary>
        public delegate void OnClickEventHandler();

        /// <summary>
        /// Get project as IProject.
        /// </summary>
        public IProject Project { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// This method is called in the load method to define custom context menu items in the project explorer. 
        /// </summary>
        protected abstract void DefineProjectExplorerMenuItems();

        /// <summary>
        /// Gets the element node for a given element within a specific element collection.
        /// </summary>
        /// <param name="elementCollectionName">Name of the element collection to search.</param>
        /// <param name="element">The element to find.</param>
        /// <returns>The ElementNode if found, or null if not found.</returns>
        public ElementNode? GetElementNode(string elementCollectionName, IElement element)
        {
            foreach (var nodeCollection in ChildNodes)
            {
                if (nodeCollection is not ElementNodeCollection enc) continue;
                if (enc.ElementCollection?.Name == elementCollectionName)
                    return ElementNode.FindElementNode(element, nodeCollection);
            }
            return null;
        }



        /// <summary>
        /// Get a project element by name for a given element collection.
        /// </summary>
        /// <param name="elementCollectionName">Name of the element collection that contains the element.</param>
        /// <param name="elementName">Name of the element to get.</param>
        public IElement? GetElement(string elementCollectionName, string elementName)
        {
            foreach (var nodeCollection in ChildNodes)
            {
                if (nodeCollection is not ElementNodeCollection enc) continue;
                if (enc.ElementCollection?.Name == elementCollectionName)
                {
                    foreach (var element in enc.ElementCollection)
                    {
                        if (element.Name == elementName) return element;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Check if the element matches the node.
        /// </summary>
        /// <param name="node">Node to check.</param>
        /// <param name="element">Element to check for.</param>
        /// <param name="matches">True if it matches, false if it doesn't.</param>
        public void ElementMatches(Node node, IElement element, ref bool matches)
        {
            if (matches == true) return;
            if (node as ElementNode != null)
            {
                if (((ElementNode)node).Element.Name == element.Name)
                {
                    matches = true;
                    return;
                }
            }
            if (node as ElementNodeCollection != null || node as ElementNodeGroup != null || node as NodeGroup != null)
            {
                foreach (var child in node.ChildNodes)
                {
                    ElementMatches(child, element, ref matches);
                }
            }

        }

        /// <summary>
        /// Load all of the child nodes. 
        /// </summary>
        public virtual void Load()
        {

            ClearChildNodesForRetry();

            var currentCandidate = TryCreateLayoutCandidate(Project.ProjectExplorerLayout, "current");
            var previousCandidate = TryCreateLayoutCandidate(Project.ProjectExplorerLayoutPrevious, "previous");
            var chosenCandidate = ChooseLayoutCandidate(currentCandidate, previousCandidate);

            if (chosenCandidate != null)
            {
                foreach (var node in chosenCandidate.Nodes)
                {
                    ChildNodes.Add(node);
                }

                if (chosenCandidate.Source == "previous")
                {
                    System.Diagnostics.Debug.WriteLine(
                        "ProjectNode: recovered Project Explorer layout from previous (dual-buffer) column.");
                }
            }
            else
            {
                LoadStandardLayout();
            }

            ResetItemsSource();
            Items.Refresh();

            // Define context menu items
            DefineProjectExplorerMenuItems();
        }

        /// <summary>
        /// Attempts to build a Project Explorer layout candidate from the given XML.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Layout XML is intentionally loaded tolerantly for backward compatibility.
        /// Older project files can contain stale element names or omit collections
        /// introduced by newer versions. Those mismatches should not discard all user
        /// grouping and ordering information.
        /// </para>
        /// <para>
        /// The returned candidate is not installed into <see cref="Node.ChildNodes"/>
        /// until <see cref="Load"/> chooses it, so a failed current layout cannot leave
        /// partial nodes behind before the previous-layout fallback is considered.
        /// </para>
        /// </remarks>
        /// <param name="xml">The layout XML string to parse.</param>
        /// <param name="source">Short diagnostic label (e.g. "current", "previous") for logging.</param>
        /// <returns>A candidate layout, or null if no usable layout could be built.</returns>
        private LayoutCandidate? TryCreateLayoutCandidate(string? xml, string source)
        {
            if (string.IsNullOrWhiteSpace(xml)) return null;

            try
            {
                var nodes = new List<Node>();
                var layout = XElement.Parse(xml);
                foreach (XElement xElement in layout.Elements())
                {
                    var node = NodeFromXElementTolerant(xElement, this);
                    if (node != null) nodes.Add(node);
                }

                AppendMissingCollections(nodes);
                AppendMissingElements(nodes);

                if (nodes.Count == 0) return null;

                return new LayoutCandidate(source, nodes, CountGroups(nodes), IsStandardFlatLayout(nodes));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ProjectNode: failed to load project explorer layout from {source} XML: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Selects the best usable layout candidate.
        /// </summary>
        /// <param name="current">Candidate created from the current layout column.</param>
        /// <param name="previous">Candidate created from the optional previous-layout column.</param>
        /// <returns>The candidate to install, or null to rebuild the standard layout.</returns>
        private static LayoutCandidate? ChooseLayoutCandidate(LayoutCandidate? current, LayoutCandidate? previous)
        {
            if (current == null) return previous;
            if (previous == null) return current;

            if (current.IsStandardFlatLayout && previous.GroupCount > 0)
            {
                return previous;
            }

            return current;
        }

        /// <summary>
        /// Adds collection nodes for project collections absent from the layout XML.
        /// </summary>
        /// <param name="nodes">The candidate top-level nodes.</param>
        private void AppendMissingCollections(IList<Node> nodes)
        {
            for (int i = 0; i < (Project.ElementCollections?.Count ?? 0); i++)
            {
                var collection = Project.ElementCollections![i];
                bool collectionExists = nodes
                    .OfType<ElementNodeCollection>()
                    .Any(node => node.ElementCollection?.Name == collection.Name);

                if (!collectionExists)
                {
                    nodes.Add(CreateStandardCollectionNode(collection));
                }
            }
        }

        /// <summary>
        /// Adds element nodes for project elements absent from the layout XML.
        /// </summary>
        /// <param name="nodes">The candidate top-level nodes.</param>
        private void AppendMissingElements(IEnumerable<Node> nodes)
        {
            foreach (var child in nodes.OfType<ElementNodeCollection>())
            {
                if (child.ElementCollection == null) continue;

                for (int i = 0; i < (Project.ElementCollections?.Count ?? 0); i++)
                {
                    if (child.ElementCollection.Name != Project.ElementCollections![i].Name) continue;

                    for (int j = 0; j < Project.ElementCollections[i].Count; j++)
                    {
                        bool itMatches = false;
                        ElementMatches(child, Project.ElementCollections[i][j], ref itMatches);

                        if (itMatches == false)
                        {
                            child.Add(new ElementNode(Project.ElementCollections[i][j], child, (ProjectExplorerTreeView?)ParentTreeView));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Counts all group nodes in a candidate tree.
        /// </summary>
        /// <param name="nodes">The candidate nodes to inspect.</param>
        /// <returns>The number of group nodes in the candidate tree.</returns>
        private static int CountGroups(IEnumerable<Node> nodes)
        {
            int count = 0;
            foreach (var node in nodes)
            {
                if (node is NodeGroup) count++;
                count += CountGroups(node.ChildNodes);
            }
            return count;
        }

        /// <summary>
        /// Determines whether the candidate is equivalent to the standard flat project layout.
        /// </summary>
        /// <param name="nodes">The candidate top-level nodes.</param>
        /// <returns><c>true</c> when the layout contains all collections flat and no groups.</returns>
        private bool IsStandardFlatLayout(IList<Node> nodes)
        {
            int collectionCount = Project.ElementCollections?.Count ?? 0;
            if (nodes.Count != collectionCount) return false;
            if (CountGroups(nodes) != 0) return false;

            for (int i = 0; i < collectionCount; i++)
            {
                if (nodes[i] is not ElementNodeCollection collectionNode) return false;
                if (collectionNode.ElementCollection != Project.ElementCollections![i]) return false;
            }

            return true;
        }

        /// <summary>
        /// Loads the default flat collection layout.
        /// </summary>
        private void LoadStandardLayout()
        {
            for (int i = 0; i < (Project.ElementCollections?.Count ?? 0); i++)
            {
                ChildNodes.Add(CreateStandardCollectionNode(Project.ElementCollections![i]));
            }
        }

        /// <summary>
        /// Creates a standard element collection node populated from the project collection.
        /// </summary>
        /// <param name="collection">The project element collection.</param>
        /// <returns>A populated collection node.</returns>
        private ElementNodeCollection CreateStandardCollectionNode(FrameworkInterfaces.IElementCollection collection)
        {
            return new ElementNodeCollection(this, (ProjectExplorerTreeView?)ParentTreeView) { ElementCollection = collection };
        }

        /// <summary>
        /// Tears down any partially-built tree, used when a layout-load attempt fails
        /// and the caller needs to retry from a different source (previous column,
        /// standard iteration).
        /// </summary>
        private void ClearChildNodesForRetry()
        {
            foreach (var nodeCollection in ChildNodes)
            {
                if (nodeCollection is not ElementNodeCollection enc) continue;
                enc.ElementCollection = null;
            }
            for (int i = ChildNodes.Count - 1; i >= 0; i--)
            {
                ChildNodes.RemoveAt(i);
            }
        }

        /// <summary>
        /// Save the project explorer tree view to XElement.
        /// </summary>
        public XElement SaveToXElement()
        {
            return NodeToXElement(this);
        }

        /// <summary>
        /// Write the node to an XElement.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>An XElement representation of the node.</returns>
        private XElement NodeToXElement(Node node)
        {
            // Create parent node
            var result = new XElement("Node");

            // Set attributes
            if (node as ProjectNode != null)
            {
                result.SetAttributeValue("NodeType", nameof(ProjectNode));
            }
            else if (node as ElementNodeCollection != null)
            {
                result.SetAttributeValue("NodeType", nameof(ElementNodeCollection));

            }
            else if (node as ElementNodeGroup != null)
            {
                result.SetAttributeValue("NodeType", nameof(ElementNodeGroup));
            }
            else if (node as NodeGroup != null)
            {
                result.SetAttributeValue("NodeType", nameof(NodeGroup));
            }
            else if (node as ElementNode != null)
            {
                result.SetAttributeValue("NodeType", nameof(ElementNode));
            }
            else
            {
                result.SetAttributeValue("NodeType", nameof(Node));
            }
            result.SetAttributeValue("Name", node.NodeHeader.HeaderText);
            result.SetAttributeValue(nameof(node.IsExpanded), node.IsExpanded);

            // Add child nodes
            foreach (Node child in node.ChildNodes)
            {
                result.Add(NodeToXElement(child));
            }

            return result;
        }

        /// <summary>
        /// Create a node from XElement
        /// </summary>
        /// <param name="xElement">The XElement to read.</param>
        /// <param name="parentNode">The parent node to add to.</param>
        /// <returns>A Node created from the XElement, or null if the element cannot be parsed.</returns>
        protected Node? NodeFromXElement(XElement xElement, Node parentNode)
        {
            var nodeTypeAttr = xElement.Attribute("NodeType");
            var nameAttr = xElement.Attribute("Name");
            if (nodeTypeAttr != null && nameAttr != null)
            {
                Node node;
                var treeView = ParentTreeView as ProjectExplorerTreeView;

                // Element Node Collection
                if (nodeTypeAttr.Value == nameof(ElementNodeCollection))
                {
                    // Set dummy node a placeholder, then look for element node collection
                    node = new SimpleNode("", parentNode, ParentTreeView);
                    for (int i = 0; i < (Project.ElementCollections?.Count ?? 0); i++)
                    {
                        if (Project.ElementCollections![i].Name == nameAttr.Value)
                        {
                            node = new ElementNodeCollection(this, treeView) { ElementCollection = Project.ElementCollections[i] };
                            bool expand = true;
                            var expandAttr = xElement.Attribute(nameof(node.IsExpanded));
                            if (expandAttr != null) bool.TryParse(expandAttr.Value, out expand);
                            node.IsExpanded = expand;
                            node.ChildNodes.Clear();
                            break;
                        }
                    }
                    if (node is not ElementNodeCollection) return null;
                }
                // Element Node Group
                else if (nodeTypeAttr.Value == nameof(ElementNodeGroup))
                {
                    node = new ElementNodeGroup(parentNode, treeView);
                    node.NodeHeader.HeaderText = nameAttr.Value;
                    bool expand = true;
                    var expandAttr = xElement.Attribute(nameof(node.IsExpanded));
                    if (expandAttr != null) bool.TryParse(expandAttr.Value, out expand);
                    node.IsExpanded = expand;
                }
                // Node Group
                else if (nodeTypeAttr.Value == nameof(NodeGroup))
                {
                    node = new NodeGroup(parentNode, treeView);
                    node.NodeHeader.HeaderText = nameAttr.Value;
                    bool expand = true;
                    var expandAttr = xElement.Attribute(nameof(node.IsExpanded));
                    if (expandAttr != null) bool.TryParse(expandAttr.Value, out expand);
                    node.IsExpanded = expand;
                }
                // Element Node
                else if (nodeTypeAttr.Value == nameof(ElementNode))
                {
                    // Set dummy node a placeholder, then look for element
                    node = new SimpleNode("", parentNode, ParentTreeView);
                    Node? tempParent = parentNode;

                    // Find the parent element node collection (with null check to prevent infinite loop)
                    while (tempParent != null)
                    {
                        if (tempParent is ElementNodeCollection elementNodeCollection)
                        {
                            // Now search for the IElementCollection with the same name
                            for (int i = 0; i < (Project.ElementCollections?.Count ?? 0); i++)
                            {
                                if (Project.ElementCollections![i].Name == elementNodeCollection.NodeHeader.HeaderText)
                                {
                                    // Now find the IElement with the same name
                                    for (int j = 0; j < Project.ElementCollections[i].Count; j++)
                                    {
                                        if (Project.ElementCollections[i][j].Name == nameAttr.Value)
                                        {
                                            node = new ElementNode(Project.ElementCollections[i][j], parentNode, treeView);
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                            break;
                        }
                        tempParent = tempParent.ParentNode;
                    }

                    if (node is not ElementNode) return null;
                }
                // Basic Node
                else
                {
                    node = new SimpleNode("", parentNode, ParentTreeView);
                }

                // Do recursion to load child nodes
                foreach (XElement child in xElement.Elements())
                {
                    var newNode = NodeFromXElement(child, node);
                    if (newNode == null) return null;

                    if (newNode is ElementNode elementNode && newNode.ParentNode is ElementNodeCollection parentCollection)
                    {
                        parentCollection.Add(elementNode);
                    }
                    else if (newNode is ElementNodeGroup elementNodeGroup && newNode.ParentNode is ElementNodeCollection)
                    {
                        var elementNodeCollection = elementNodeGroup.GetNodeCollection();
                        elementNodeCollection?.AddGroup(elementNodeGroup);
                    }
                    else if (newNode is NodeGroup nodeGroup && newNode.ParentNode is ElementNodeCollection)
                    {
                        var elementNodeCollection = nodeGroup.GetNodeCollection();
                        elementNodeCollection?.AddGroup(nodeGroup);
                    }
                    else
                    {
                        node.ChildNodes.Add(newNode);
                    }

                }

                return node;
            }
            else
            {
                // Return nothing if XML is not readable
                return null;
            }
        }

        /// <summary>
        /// Creates a node from XElement while tolerating stale layout entries.
        /// </summary>
        /// <param name="xElement">The XML element to read.</param>
        /// <param name="parentNode">The parent node to attach to.</param>
        /// <returns>A node when the XML entry matches the current project, otherwise null.</returns>
        private Node? NodeFromXElementTolerant(XElement xElement, Node parentNode)
        {
            var nodeTypeAttr = xElement.Attribute("NodeType");
            var nameAttr = xElement.Attribute("Name");
            if (nodeTypeAttr == null || nameAttr == null) return null;

            var treeView = ParentTreeView as ProjectExplorerTreeView;
            Node? node = null;

            if (nodeTypeAttr.Value == nameof(ElementNodeCollection))
            {
                var collection = FindElementCollection(nameAttr.Value);
                if (collection == null) return null;

                node = new ElementNodeCollection(this, treeView) { ElementCollection = collection };
                SetExpandedFromXml(node, xElement);
                node.ChildNodes.Clear();
            }
            else if (nodeTypeAttr.Value == nameof(ElementNodeGroup))
            {
                node = new ElementNodeGroup(parentNode, treeView);
                node.NodeHeader.HeaderText = nameAttr.Value;
                SetExpandedFromXml(node, xElement);
            }
            else if (nodeTypeAttr.Value == nameof(NodeGroup))
            {
                node = new NodeGroup(parentNode, treeView);
                node.NodeHeader.HeaderText = nameAttr.Value;
                SetExpandedFromXml(node, xElement);
            }
            else if (nodeTypeAttr.Value == nameof(ElementNode))
            {
                var collectionNode = FindParentElementNodeCollection(parentNode);
                if (collectionNode?.ElementCollection == null) return null;

                var element = collectionNode.ElementCollection.FirstOrDefault(e => e.Name == nameAttr.Value);
                if (element == null) return null;

                node = new ElementNode(element, parentNode, treeView);
                SetExpandedFromXml(node, xElement);
            }
            else
            {
                node = new SimpleNode("", parentNode, ParentTreeView);
                node.NodeHeader.HeaderText = nameAttr.Value;
                SetExpandedFromXml(node, xElement);
            }

            foreach (XElement child in xElement.Elements())
            {
                var newNode = NodeFromXElementTolerant(child, node);
                if (newNode == null) continue;

                AddLoadedChild(node, newNode);
            }

            return node;
        }

        /// <summary>
        /// Adds a child node reconstructed from XML to its parent node.
        /// </summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="child">The child node.</param>
        private static void AddLoadedChild(Node parent, Node child)
        {
            if (child is ElementNode elementNode && parent is ElementNodeCollection parentCollection)
            {
                parentCollection.Add(elementNode);
            }
            else if (child is ElementNodeGroup elementNodeGroup && parent is ElementNodeCollection parentElementCollection)
            {
                parentElementCollection.AddGroup(elementNodeGroup);
            }
            else if (child is NodeGroup nodeGroup && parent is ElementNodeCollection nodeCollection)
            {
                nodeCollection.AddGroup(nodeGroup);
            }
            else if (parent is NodeGroup group)
            {
                group.Add(child, false);
            }
            else
            {
                parent.ChildNodes.Add(child);
            }
        }

        /// <summary>
        /// Reads a node expansion value from XML.
        /// </summary>
        /// <param name="node">The node to update.</param>
        /// <param name="xElement">The XML element containing expansion state.</param>
        private static void SetExpandedFromXml(Node node, XElement xElement)
        {
            bool expand = true;
            var expandAttr = xElement.Attribute(nameof(node.IsExpanded));
            if (expandAttr != null) bool.TryParse(expandAttr.Value, out expand);
            node.IsExpanded = expand;
        }

        /// <summary>
        /// Finds a project element collection by name.
        /// </summary>
        /// <param name="name">The collection name.</param>
        /// <returns>The matching collection, or null if it does not exist.</returns>
        private FrameworkInterfaces.IElementCollection? FindElementCollection(string name)
        {
            for (int i = 0; i < (Project.ElementCollections?.Count ?? 0); i++)
            {
                if (Project.ElementCollections![i].Name == name) return Project.ElementCollections[i];
            }

            return null;
        }

        /// <summary>
        /// Finds the nearest parent element collection node.
        /// </summary>
        /// <param name="parentNode">The node where the parent search should begin.</param>
        /// <returns>The nearest element collection node, or null if there is none.</returns>
        private static ElementNodeCollection? FindParentElementNodeCollection(Node parentNode)
        {
            Node? tempParent = parentNode;
            while (tempParent != null)
            {
                if (tempParent is ElementNodeCollection elementNodeCollection)
                    return elementNodeCollection;

                tempParent = tempParent.ParentNode;
            }

            return null;
        }

        /// <summary>
        /// Candidate project-explorer layout built from one persisted XML column.
        /// </summary>
        /// <param name="Source">Diagnostic source label for the XML column.</param>
        /// <param name="Nodes">Top-level nodes in the candidate layout.</param>
        /// <param name="GroupCount">Number of group nodes in the candidate layout.</param>
        /// <param name="IsStandardFlatLayout">Whether the candidate matches the standard flat layout.</param>
        private sealed record LayoutCandidate(string Source, IList<Node> Nodes, int GroupCount, bool IsStandardFlatLayout);

        /// <summary>
        /// When the Project Node header is left clicked, raise OnClick event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void NodeHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) OnClick?.Invoke();
            e.Handled = true;
        }

        #endregion

    }
}
