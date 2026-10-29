using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace DAG
{
    /// <summary>
    /// Delegate for previewing connections being added to the graph.
    /// </summary>
    /// <param name="connections">The connections being added.</param>
    /// <param name="cancel">Set to true to cancel the operation.</param>
    public delegate void PreviewConnectionsAdded(Tuple<OutConnector, InConnector>[] connections, ref bool cancel);

    /// <summary>
    /// Delegate for handling connections that have been added to the graph.
    /// </summary>
    /// <param name="connections">The connections that were added.</param>
    public delegate void ConnectionsAdded(Tuple<OutConnector, InConnector>[] connections);

    /// <summary>
    /// Delegate for previewing connections being removed from the graph.
    /// </summary>
    /// <param name="connections">The connections being removed.</param>
    /// <param name="cancel">Set to true to cancel the operation.</param>
    public delegate void PreviewConnectionsRemoved(Tuple<OutConnector, InConnector>[] connections, ref bool cancel);

    /// <summary>
    /// Delegate for handling connections that have been removed from the graph.
    /// </summary>
    /// <param name="connections">The connections that were removed.</param>
    public delegate void ConnectionsRemoved(Tuple<OutConnector, InConnector>[] connections);

    /// <summary>
    /// Abstract base class representing a Directed Acyclic Graph (DAG).
    /// Provides core functionality for managing nodes and connections, including
    /// cycle detection, topological sorting, and path queries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A DAG is a directed graph with no directed cycles. This means that for any vertex v,
    /// there is no directed path that starts and ends on v. This property is enforced by
    /// the <see cref="WouldCreateCycle"/> method which should be called before adding connections.
    /// </para>
    /// <para>
    /// Derived classes must implement <see cref="AddToBaseElement"/> and <see cref="ReadNodeRequested"/>
    /// to support XML serialization and deserialization of custom node types.
    /// </para>
    /// </remarks>
    public abstract class Graph : INotifyPropertyChanged
    {
        #region Fields

        /// <summary>
        /// The collection of nodes in the graph.
        /// </summary>
        private readonly ObservableCollection<NodeBase> _nodes = new ObservableCollection<NodeBase>();

        /// <summary>
        /// Internal list of connections between nodes.
        /// </summary>
        private readonly List<Tuple<OutConnector, InConnector>> _connections = new List<Tuple<OutConnector, InConnector>>();

        /// <summary>
        /// Tracks nodes that have been subscribed to for collection changed events,
        /// enabling proper cleanup on <see cref="System.Collections.Specialized.NotifyCollectionChangedAction.Reset"/>.
        /// </summary>
        private readonly HashSet<NodeBase> _subscribedNodes = new HashSet<NodeBase>();

        /// <summary>
        /// The current scale/zoom level of the graph.
        /// </summary>
        protected double _scale = 1;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the collection of nodes in the graph.
        /// </summary>
        public ObservableCollection<NodeBase> Nodes => _nodes;

        /// <summary>
        /// Gets or sets the scale (zoom level) of the graph for visual display.
        /// </summary>
        /// <value>The scale factor. Default is 1.0.</value>
        public double Scale
        {
            get => _scale;
            set => Utilities.SetDouble(value, ref _scale, PropertyChanged, this);
        }

        /// <summary>
        /// Gets a read-only collection of all connections in the graph.
        /// </summary>
        /// <value>A read-only collection of tuples where Item1 is the source <see cref="OutConnector"/>
        /// and Item2 is the destination <see cref="InConnector"/>.</value>
        public ReadOnlyCollection<Tuple<OutConnector, InConnector>> Connections { get; }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs before connections are added to the graph. Allows cancellation.
        /// </summary>
        public event PreviewConnectionsAdded PreviewConnectionsAdded;

        /// <summary>
        /// Occurs after connections have been added to the graph.
        /// </summary>
        public event ConnectionsAdded ConnectionsAdded;

        /// <summary>
        /// Occurs before connections are removed from the graph. Allows cancellation.
        /// </summary>
        public event PreviewConnectionsRemoved PreviewConnectionsRemoved;

        /// <summary>
        /// Occurs after connections have been removed from the graph.
        /// </summary>
        public event ConnectionsRemoved ConnectionsRemoved;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Graph"/> class.
        /// </summary>
        public Graph()
        {
            _nodes.CollectionChanged += Nodes_CollectionChanged;
            Connections = new ReadOnlyCollection<Tuple<OutConnector, InConnector>>(_connections);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Graph"/> class from an XML element.
        /// </summary>
        /// <param name="el">The XML element containing the serialized graph data.</param>
        /// <remarks>
        /// The XML element must have a name matching the derived class type name.
        /// If the element is null or has a mismatched name, the graph will be initialized empty.
        /// </remarks>
        public Graph(XElement el)
        {
            _nodes.CollectionChanged += Nodes_CollectionChanged;
            Connections = new ReadOnlyCollection<Tuple<OutConnector, InConnector>>(_connections);
            if (el == null || el.Name != this.GetType().Name) { return; }

            // Clear the existing nodes. _connections was just initialized empty above,
            // so there is nothing to capture or clear here — the prior ConnectionsRemoved
            // invocation always fired with an empty array and was a no-op.
            Nodes.Clear();

            // Set the scale
            if (el.Attribute(nameof(Scale)) != null)
            {
                _ = double.TryParse(el.Attribute("Scale").Value, NumberStyles.Any, CultureInfo.InvariantCulture, out _scale);
            }

            // Load Nodes
            NodeBase node;
            Dictionary<string, NodeBase> nodes = new Dictionary<string, NodeBase>();
            IEnumerable<XElement> nodeElements = el.Elements("Nodes").Elements("Node");
            foreach (XElement nodeElement in nodeElements)
            {
                node = ReadNodeRequested(nodeElement);
                if (node == null) { continue; }
                string key = node.NodeGuid.ToString();
                // Skip duplicate-GUID nodes entirely. Previously the duplicate was added to Nodes
                // but not to the GUID lookup dictionary, so any connection targeting it silently
                // routed to the first node instead.
                if (nodes.ContainsKey(key)) { continue; }
                nodes.Add(key, node);
                Nodes.Add(node);
            }

            // Load Connections
            // Note: We validate each connection to prevent cycles from being loaded from malformed XML
            string fromGuid, toGuid;
            int fromConnectorIndex, toConnectorIndex;
            NodeBase fromNode, toNode;
            OutConnector fromConnector;
            InConnector toConnector;
            foreach (XElement conElement in el.Elements("Connections").Elements("Connection"))
            {
                fromConnectorIndex = -1;
                toConnectorIndex = -1;
                if (conElement.Attribute("From_Node") != null) { fromGuid = conElement.Attribute("From_Node").Value; } else { continue; }
                if (conElement.Attribute("From_Connector") != null) { int.TryParse(conElement.Attribute("From_Connector").Value, NumberStyles.Any, CultureInfo.InvariantCulture, out fromConnectorIndex); } else { continue; }
                if (conElement.Attribute("To_Node") != null) { toGuid = conElement.Attribute("To_Node").Value; } else { continue; }
                if (conElement.Attribute("To_Connector") != null) { int.TryParse(conElement.Attribute("To_Connector").Value, NumberStyles.Any, CultureInfo.InvariantCulture, out toConnectorIndex); } else { continue; }

                if (nodes.ContainsKey(fromGuid)) { fromNode = nodes[fromGuid]; } else { continue; }
                if (fromConnectorIndex == -1 || fromConnectorIndex >= fromNode.Outputs.Count) { continue; }
                fromConnector = fromNode.Outputs[fromConnectorIndex];
                if (nodes.ContainsKey(toGuid)) { toNode = nodes[toGuid]; } else { continue; }
                if (toConnectorIndex == -1 || toConnectorIndex >= toNode.Inputs.Count) { continue; }
                toConnector = toNode.Inputs[toConnectorIndex];

                // Validate that adding this connection won't create a cycle
                if (WouldCreateCycle(fromConnector, toConnector)) { continue; }

                Tuple<OutConnector, InConnector> connection = new Tuple<OutConnector, InConnector>(fromConnector, toConnector);
                if (_connections.Contains(connection) == false) { _connections.Add(connection); }
            }
        }

        #endregion

        #region DAG Operations

        /// <summary>
        /// Determines whether adding a connection between the specified connectors would create a cycle.
        /// </summary>
        /// <param name="fromConnector">The source output connector.</param>
        /// <param name="toConnector">The destination input connector.</param>
        /// <returns>
        /// <c>true</c> if adding the connection would create a cycle; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method uses depth-first search to detect if there is already a path from
        /// the destination node back to the source node. If such a path exists, adding the
        /// connection would create a cycle, violating the DAG property.
        /// </remarks>
        /// <example>
        /// <code>
        /// if (!graph.WouldCreateCycle(outputConnector, inputConnector))
        /// {
        ///     graph.AddConnection(outputConnector, inputConnector);
        /// }
        /// </code>
        /// </example>
        public bool WouldCreateCycle(OutConnector fromConnector, InConnector toConnector)
        {
            if (fromConnector == null || toConnector == null) { return false; }
            if (fromConnector.Parent == null || toConnector.Parent == null) { return false; }

            NodeBase sourceNode = fromConnector.Parent;
            NodeBase destNode = toConnector.Parent;

            // Self-loop check
            if (sourceNode == destNode) { return true; }

            // Check if there's already a path from destNode to sourceNode
            // If so, adding an edge from sourceNode to destNode would create a cycle
            return HasPath(destNode, sourceNode);
        }

        /// <summary>
        /// Determines whether there is a directed path from one node to another.
        /// </summary>
        /// <param name="fromNode">The starting node.</param>
        /// <param name="toNode">The target node.</param>
        /// <returns>
        /// <c>true</c> if a directed path exists from <paramref name="fromNode"/> to <paramref name="toNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Uses breadth-first search (BFS) to find if any path exists between the nodes.
        /// </remarks>
        public bool HasPath(NodeBase fromNode, NodeBase toNode)
        {
            if (fromNode == null || toNode == null) { return false; }
            if (fromNode == toNode) { return true; }

            var visited = new HashSet<NodeBase>();
            var queue = new Queue<NodeBase>();
            queue.Enqueue(fromNode);
            visited.Add(fromNode);

            while (queue.Count > 0)
            {
                NodeBase current = queue.Dequeue();

                // Get all nodes that current node connects to
                foreach (var connection in _connections)
                {
                    if (connection.Item1.Parent == current)
                    {
                        NodeBase neighbor = connection.Item2.Parent;
                        if (neighbor == toNode) { return true; }
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all root nodes in the graph (nodes with no incoming connections).
        /// </summary>
        /// <returns>A list of nodes that have no incoming connections.</returns>
        /// <remarks>
        /// Root nodes are entry points in the DAG - they have no dependencies.
        /// In a properly structured DAG, processing should typically start from root nodes.
        /// </remarks>
        public List<NodeBase> GetRootNodes()
        {
            var nodesWithIncoming = new HashSet<NodeBase>();

            foreach (var connection in _connections)
            {
                nodesWithIncoming.Add(connection.Item2.Parent);
            }

            return Nodes.Where(n => !nodesWithIncoming.Contains(n)).ToList();
        }

        /// <summary>
        /// Gets all leaf nodes in the graph (nodes with no outgoing connections).
        /// </summary>
        /// <returns>A list of nodes that have no outgoing connections.</returns>
        /// <remarks>
        /// Leaf nodes are terminal points in the DAG - nothing depends on them.
        /// In a properly structured DAG, these represent final outputs or endpoints.
        /// </remarks>
        public List<NodeBase> GetLeafNodes()
        {
            var nodesWithOutgoing = new HashSet<NodeBase>();

            foreach (var connection in _connections)
            {
                nodesWithOutgoing.Add(connection.Item1.Parent);
            }

            return Nodes.Where(n => !nodesWithOutgoing.Contains(n)).ToList();
        }

        /// <summary>
        /// Gets all ancestor nodes of the specified node (nodes that can reach this node).
        /// </summary>
        /// <param name="node">The node to find ancestors for.</param>
        /// <returns>A list of all nodes that have a directed path to the specified node.</returns>
        /// <remarks>
        /// Ancestors are all nodes that directly or indirectly connect to the specified node.
        /// This is useful for determining all dependencies of a given node.
        /// </remarks>
        public List<NodeBase> GetAncestors(NodeBase node)
        {
            if (node == null) { return new List<NodeBase>(); }

            var ancestors = new HashSet<NodeBase>();
            var queue = new Queue<NodeBase>();

            // Start with nodes that directly connect to this node
            foreach (var connection in _connections)
            {
                if (connection.Item2.Parent == node)
                {
                    NodeBase parent = connection.Item1.Parent;
                    if (!ancestors.Contains(parent))
                    {
                        ancestors.Add(parent);
                        queue.Enqueue(parent);
                    }
                }
            }

            // BFS to find all ancestors
            while (queue.Count > 0)
            {
                NodeBase current = queue.Dequeue();
                foreach (var connection in _connections)
                {
                    if (connection.Item2.Parent == current)
                    {
                        NodeBase parent = connection.Item1.Parent;
                        if (!ancestors.Contains(parent))
                        {
                            ancestors.Add(parent);
                            queue.Enqueue(parent);
                        }
                    }
                }
            }

            return ancestors.ToList();
        }

        /// <summary>
        /// Gets all descendant nodes of the specified node (nodes reachable from this node).
        /// </summary>
        /// <param name="node">The node to find descendants for.</param>
        /// <returns>A list of all nodes that can be reached from the specified node.</returns>
        /// <remarks>
        /// Descendants are all nodes that the specified node directly or indirectly connects to.
        /// This is useful for determining all nodes that depend on a given node.
        /// </remarks>
        public List<NodeBase> GetDescendants(NodeBase node)
        {
            if (node == null) { return new List<NodeBase>(); }

            var descendants = new HashSet<NodeBase>();
            var queue = new Queue<NodeBase>();

            // Start with nodes that this node directly connects to
            foreach (var connection in _connections)
            {
                if (connection.Item1.Parent == node)
                {
                    NodeBase child = connection.Item2.Parent;
                    if (!descendants.Contains(child))
                    {
                        descendants.Add(child);
                        queue.Enqueue(child);
                    }
                }
            }

            // BFS to find all descendants
            while (queue.Count > 0)
            {
                NodeBase current = queue.Dequeue();
                foreach (var connection in _connections)
                {
                    if (connection.Item1.Parent == current)
                    {
                        NodeBase child = connection.Item2.Parent;
                        if (!descendants.Contains(child))
                        {
                            descendants.Add(child);
                            queue.Enqueue(child);
                        }
                    }
                }
            }

            return descendants.ToList();
        }

        /// <summary>
        /// Performs a topological sort on the graph nodes.
        /// </summary>
        /// <returns>
        /// A list of nodes in topological order, where each node appears before all nodes it connects to.
        /// Returns null if the graph contains a cycle (which would violate DAG properties).
        /// </returns>
        /// <remarks>
        /// <para>
        /// Topological sorting produces a linear ordering of nodes such that for every directed edge (u, v),
        /// node u comes before node v in the ordering. This is useful for determining execution order
        /// in dependency-based systems.
        /// </para>
        /// <para>
        /// This implementation uses Kahn's algorithm with O(V + E) time complexity where V is the number
        /// of nodes and E is the number of connections.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var sortedNodes = graph.TopologicalSort();
        /// if (sortedNodes != null)
        /// {
        ///     foreach (var node in sortedNodes)
        ///     {
        ///         // Process nodes in dependency order
        ///         ProcessNode(node);
        ///     }
        /// }
        /// </code>
        /// </example>
        public List<NodeBase> TopologicalSort()
        {
            // Calculate in-degree for each node
            var inDegree = new Dictionary<NodeBase, int>();
            foreach (var node in Nodes)
            {
                inDegree[node] = 0;
            }

            foreach (var connection in _connections)
            {
                NodeBase destNode = connection.Item2.Parent;
                if (inDegree.ContainsKey(destNode))
                {
                    inDegree[destNode]++;
                }
            }

            // Queue nodes with zero in-degree
            var queue = new Queue<NodeBase>();
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                {
                    queue.Enqueue(kvp.Key);
                }
            }

            var result = new List<NodeBase>();

            while (queue.Count > 0)
            {
                NodeBase current = queue.Dequeue();
                result.Add(current);

                // Reduce in-degree for all neighbors
                foreach (var connection in _connections)
                {
                    if (connection.Item1.Parent == current)
                    {
                        NodeBase neighbor = connection.Item2.Parent;
                        if (inDegree.ContainsKey(neighbor))
                        {
                            inDegree[neighbor]--;
                            if (inDegree[neighbor] == 0)
                            {
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }
            }

            // If we couldn't process all nodes, there's a cycle
            if (result.Count != Nodes.Count)
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// Determines whether the graph contains any cycles.
        /// </summary>
        /// <returns><c>true</c> if the graph contains a cycle; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// A properly maintained DAG should never contain cycles. This method can be used
        /// to validate graph integrity after loading from external sources.
        /// </remarks>
        public bool HasCycle()
        {
            return TopologicalSort() == null;
        }

        /// <summary>
        /// Gets the depth (longest path length) of the graph.
        /// </summary>
        /// <returns>
        /// The length of the longest path in the graph, 0 if the graph is empty,
        /// or -1 if a cycle is detected.
        /// </returns>
        /// <remarks>
        /// The depth represents the maximum number of sequential dependencies in the graph.
        /// This is calculated by finding the longest path from any root node to any leaf node.
        /// Returns -1 if the graph contains a cycle (which should not occur in a properly maintained DAG).
        /// </remarks>
        public int GetGraphDepth()
        {
            if (Nodes.Count == 0) { return 0; }

            var sorted = TopologicalSort();
            if (sorted == null) { return -1; } // Cycle detected

            var depth = new Dictionary<NodeBase, int>();
            foreach (var node in sorted)
            {
                depth[node] = 0;
            }

            foreach (var node in sorted)
            {
                foreach (var connection in _connections)
                {
                    if (connection.Item1.Parent == node)
                    {
                        NodeBase neighbor = connection.Item2.Parent;
                        if (depth.ContainsKey(neighbor))
                        {
                            depth[neighbor] = Math.Max(depth[neighbor], depth[node] + 1);
                        }
                    }
                }
            }

            return depth.Values.Max();
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Adds a connection between an output connector and an input connector.
        /// </summary>
        /// <param name="outConnector">The source output connector.</param>
        /// <param name="inConnector">The destination input connector.</param>
        /// <returns><c>true</c> if the connection was successfully added; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The connection will not be added if it would create a cycle in the graph.
        /// Use <see cref="WouldCreateCycle"/> to check before calling this method if you need
        /// to provide feedback to the user about why a connection was rejected.
        /// </remarks>
        public bool AddConnection(OutConnector outConnector, InConnector inConnector)
        {
            Tuple<OutConnector, InConnector> newConnection = new Tuple<OutConnector, InConnector>(outConnector, inConnector);
            return AddConnection(newConnection);
        }

        /// <summary>
        /// Adds a connection to the graph.
        /// </summary>
        /// <param name="connection">The connection tuple to add.</param>
        /// <returns><c>true</c> if the connection was successfully added; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The connection will not be added if it would create a cycle in the graph.
        /// </remarks>
        public bool AddConnection(Tuple<OutConnector, InConnector> connection)
        {
            AddConnections(new Tuple<OutConnector, InConnector>[] { connection });
            return _connections.Contains(connection);
        }

        /// <summary>
        /// Removes a connection between an output connector and an input connector.
        /// </summary>
        /// <param name="outConnector">The source output connector.</param>
        /// <param name="inConnector">The destination input connector.</param>
        /// <returns><c>true</c> if the connection was successfully removed; otherwise, <c>false</c>.</returns>
        public bool RemoveConnection(OutConnector outConnector, InConnector inConnector)
        {
            Tuple<OutConnector, InConnector> connection = new Tuple<OutConnector, InConnector>(outConnector, inConnector);
            return RemoveConnection(connection);
        }

        /// <summary>
        /// Removes a connection from the graph.
        /// </summary>
        /// <param name="connection">The connection tuple to remove.</param>
        /// <returns><c>true</c> if the connection was successfully removed; otherwise, <c>false</c>.</returns>
        public bool RemoveConnection(Tuple<OutConnector, InConnector> connection)
        {
            RemoveConnections(new Tuple<OutConnector, InConnector>[] { connection });
            return !_connections.Contains(connection);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Updates connector subscriptions and removes invalid connections when nodes change.
        /// </summary>
        /// <param name="sender">The node collection that raised the event.</param>
        /// <param name="e">The collection change details.</param>
        private void Nodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                // Unsubscribe all tracked nodes
                foreach (var node in _subscribedNodes)
                {
                    node.Inputs.CollectionChanged -= Inputs_CollectionChanged;
                    node.Outputs.CollectionChanged -= Outputs_CollectionChanged;
                }
                _subscribedNodes.Clear();
                return;
            }

            if (e.OldItems != null)
            {
                // Remove connections associated with the removed nodes
                HashSet<Tuple<OutConnector, InConnector>> connectionsToRemove = new HashSet<Tuple<OutConnector, InConnector>>();

                foreach (object item in e.OldItems)
                {
                    if (!(item is NodeBase node)) { continue; }

                    // Release the node event handlers
                    node.Inputs.CollectionChanged -= Inputs_CollectionChanged;
                    node.Outputs.CollectionChanged -= Outputs_CollectionChanged;
                    _subscribedNodes.Remove(node);

                    for (int i = _connections.Count - 1; i >= 0; i--)
                    {
                        if (_connections[i].Item1.Parent == node || _connections[i].Item2.Parent == node)
                        {
                            connectionsToRemove.Add(_connections[i]);
                        }
                    }
                }

                ForceRemoveConnections(connectionsToRemove);
            }

            if (e.NewItems != null)
            {
                foreach (object item in e.NewItems)
                {
                    if (!(item is NodeBase node)) { continue; }

                    // Add handlers to manage connections if inputs or outputs change
                    node.Inputs.CollectionChanged += Inputs_CollectionChanged;
                    node.Outputs.CollectionChanged += Outputs_CollectionChanged;
                    _subscribedNodes.Add(node);
                }
            }
        }

        /// <summary>
        /// Removes connections that target outputs removed from a node.
        /// </summary>
        /// <param name="sender">The output connector collection that changed.</param>
        /// <param name="e">The collection change details.</param>
        private void Outputs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                // Remove any connections to removed outputs
                HashSet<Tuple<OutConnector, InConnector>> connectionsToRemove = new HashSet<Tuple<OutConnector, InConnector>>();

                foreach (object oldOut in e.OldItems)
                {
                    if (!(oldOut is OutConnector outConnector)) { continue; }

                    for (int i = _connections.Count - 1; i >= 0; i--)
                    {
                        if (_connections[i].Item1.Equals(outConnector)) { connectionsToRemove.Add(_connections[i]); }
                    }
                }

                // ForceRemoveConnections rather than RemoveConnections: the connector is already
                // gone from the node, so these connections are dangling and cancellation by a
                // PreviewConnectionsRemoved subscriber would leave the graph in an invalid state.
                ForceRemoveConnections(connectionsToRemove.ToArray());
            }
        }

        /// <summary>
        /// Removes connections that target inputs removed from a node.
        /// </summary>
        /// <param name="sender">The input connector collection that changed.</param>
        /// <param name="e">The collection change details.</param>
        private void Inputs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                // Remove any connections to removed inputs
                HashSet<Tuple<OutConnector, InConnector>> connectionsToRemove = new HashSet<Tuple<OutConnector, InConnector>>();

                foreach (object oldIn in e.OldItems)
                {
                    if (!(oldIn is InConnector inConnector)) { continue; }

                    for (int i = _connections.Count - 1; i >= 0; i--)
                    {
                        if (_connections[i].Item2.Equals(inConnector)) { connectionsToRemove.Add(_connections[i]); }
                    }
                }

                // ForceRemoveConnections: see the matching comment in Outputs_CollectionChanged.
                ForceRemoveConnections(connectionsToRemove.ToArray());
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Removes connections without firing <see cref="PreviewConnectionsRemoved"/>.
        /// Used during node removal to avoid cancellation of internal cleanup.
        /// </summary>
        private void ForceRemoveConnections(IEnumerable<Tuple<OutConnector, InConnector>> connections)
        {
            var toRemove = connections.ToArray();
            if (toRemove.Length == 0) { return; }

            foreach (var con in toRemove)
            {
                _ = _connections.Remove(con);
            }
            ConnectionsRemoved?.Invoke(toRemove);
        }

        /// <summary>
        /// Removes connections after giving subscribers a chance to cancel the operation.
        /// </summary>
        /// <param name="connectionsToRemove">The connections requested for removal.</param>
        private void RemoveConnections(Tuple<OutConnector, InConnector>[] connectionsToRemove)
        {
            if (connectionsToRemove == null || connectionsToRemove.Length == 0) { return; }

            bool cancelRemove = false;
            PreviewConnectionsRemoved?.Invoke(connectionsToRemove, ref cancelRemove);
            if (cancelRemove == false)
            {
                foreach (var con in connectionsToRemove)
                {
                    _ = _connections.Remove(con);
                }
                ConnectionsRemoved?.Invoke(connectionsToRemove);
            }
        }

        /// <summary>
        /// Adds valid, non-duplicate connections after preview subscribers approve the change.
        /// </summary>
        /// <param name="connectionsToAdd">The candidate connections to add.</param>
        private void AddConnections(Tuple<OutConnector, InConnector>[] connectionsToAdd)
        {
            if (connectionsToAdd == null || connectionsToAdd.Length == 0) { return; }

            // Filter to remove connections that already exist, would create cycles,
            // or target an InConnector that already has a connection
            var filteredConnections = new List<Tuple<OutConnector, InConnector>>(connectionsToAdd.Length);
            for (int i = 0; i < connectionsToAdd.Length; i++)
            {
                if (_connections.Contains(connectionsToAdd[i]) == false &&
                    !WouldCreateCycle(connectionsToAdd[i].Item1, connectionsToAdd[i].Item2) &&
                    !_connections.Any(c => c.Item2 == connectionsToAdd[i].Item2))
                {
                    filteredConnections.Add(connectionsToAdd[i]);
                }
            }
            if (filteredConnections.Count == 0) { return; }

            Tuple<OutConnector, InConnector>[] connections = filteredConnections.ToArray();

            bool cancelAdd = false;
            PreviewConnectionsAdded?.Invoke(connections, ref cancelAdd);
            if (cancelAdd == false)
            {
                foreach (var con in connections) { _connections.Add(con); }
                ConnectionsAdded?.Invoke(connections);
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates the structural integrity of the graph by checking that all connections
        /// reference nodes that are currently in the <see cref="Nodes"/> collection.
        /// </summary>
        /// <returns>A list of human-readable error messages. An empty list indicates the graph is valid.</returns>
        public List<string> ValidateIntegrity()
        {
            var errors = new List<string>();
            var nodeSet = new HashSet<NodeBase>(Nodes);

            for (int i = 0; i < _connections.Count; i++)
            {
                var connection = _connections[i];
                if (connection.Item1.Parent == null || !nodeSet.Contains(connection.Item1.Parent))
                {
                    errors.Add($"Connection {i}: source node is not in Nodes collection.");
                }
                if (connection.Item2.Parent == null || !nodeSet.Contains(connection.Item2.Parent))
                {
                    errors.Add($"Connection {i}: destination node is not in Nodes collection.");
                }
            }

            return errors;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// When overridden in a derived class, adds custom data to the XML element during serialization.
        /// </summary>
        /// <param name="baseElement">The base XML element to add data to.</param>
        protected abstract void AddToBaseElement(XElement baseElement);

        /// <summary>
        /// When overridden in a derived class, creates a node from an XML element during deserialization.
        /// </summary>
        /// <param name="nodeElement">The XML element containing the node data.</param>
        /// <returns>A new node instance created from the XML data.</returns>
        protected abstract NodeBase ReadNodeRequested(XElement nodeElement);

        /// <summary>
        /// Serializes the graph to an XML element.
        /// </summary>
        /// <returns>An <see cref="XElement"/> containing the serialized graph data.</returns>
        /// <remarks>
        /// The XML structure includes:
        /// <list type="bullet">
        /// <item><description>Graph-level attributes (Scale)</description></item>
        /// <item><description>A Nodes element containing all node data</description></item>
        /// <item><description>A Connections element containing all connection data</description></item>
        /// <item><description>Any additional data added by <see cref="AddToBaseElement"/></description></item>
        /// </list>
        /// </remarks>
        public XElement ToXElement()
        {
            XElement graphElement = new XElement(this.GetType().Name);
            graphElement.SetAttributeValue(nameof(Scale), _scale.ToString("G17", CultureInfo.InvariantCulture));

            // Serialize Nodes
            XElement nodesElement = new XElement("Nodes");
            foreach (NodeBase item in Nodes)
            {
                XElement nodeElement = item.ToXElement();
                nodesElement.Add(nodeElement);
            }
            graphElement.Add(nodesElement);

            // Serialize Connections
            XElement connectionsElement = new XElement("Connections");
            foreach (Tuple<OutConnector, InConnector> item in _connections)
            {
                int fromIdx = item.Item1.Parent.Outputs.IndexOf(item.Item1);
                int toIdx = item.Item2.Parent.Inputs.IndexOf(item.Item2);
                if (fromIdx == -1 || toIdx == -1) continue;

                XElement connectionElement = new XElement("Connection");
                connectionElement.SetAttributeValue("From_Node", item.Item1.Parent.NodeGuid);
                connectionElement.SetAttributeValue("From_Connector", fromIdx.ToString(CultureInfo.InvariantCulture));
                connectionElement.SetAttributeValue("To_Node", item.Item2.Parent.NodeGuid);
                connectionElement.SetAttributeValue("To_Connector", toIdx.ToString(CultureInfo.InvariantCulture));
                connectionsElement.Add(connectionElement);
            }
            graphElement.Add(connectionsElement);

            AddToBaseElement(graphElement);

            return graphElement;
        }

        #endregion
    }
}
