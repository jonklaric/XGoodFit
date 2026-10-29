using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Linq;
using DAG;
using System.Windows.Data;
using System.Collections.ObjectModel;

namespace DAGControls
{
    /// <summary>
    /// Delegate for handling canvas context menu requests.
    /// </summary>
    /// <param name="cm">The context menu to populate with items.</param>
    /// <param name="canvasPosition">The position on the canvas where the right-click occurred.</param>
    public delegate void CanvasContextMenu(ContextMenu cm, Point canvasPosition);

    /// <summary>
    /// Delegate for handling node context menu requests.
    /// </summary>
    /// <param name="cm">The context menu to populate with items.</param>
    /// <param name="node">The node that was right-clicked.</param>
    public delegate void NodeContextMenu(ContextMenu cm, NodeBase node);

    /// <summary>
    /// Delegate for handling connection added events in the UI.
    /// </summary>
    /// <param name="connection">The connection that was added.</param>
    /// <param name="connectionPath">The visual path representing the connection.</param>
    public delegate void ConnectionAdded(Tuple<OutConnector, InConnector> connection, Path connectionPath);

    /// <summary>
    /// Delegate for handling connection removed events in the UI.
    /// </summary>
    /// <param name="connection">The connection that was removed.</param>
    /// <param name="connectionPath">The visual path that was removed.</param>
    public delegate void ConnectionRemoved(Tuple<OutConnector, InConnector> connection, Path connectionPath);

    /// <summary>
    /// Delegate for handling graph redraw events.
    /// </summary>
    public delegate void GraphRedrawn();

    /// <summary>
    /// Delegate for handling node move events.
    /// </summary>
    /// <param name="node">The node that was moved.</param>
    public delegate void NodeMoved(NodeBase node);

    /// <summary>
    /// Delegate for handling node size change events.
    /// </summary>
    /// <param name="node">The node whose size changed.</param>
    public delegate void NodeSizeChanged(NodeBase node);

    /// <summary>
    /// A WPF Canvas control that provides interactive visualization and editing of a directed acyclic graph (DAG).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The FlowGraphCanvas provides a rich interactive experience for working with DAG graphs including:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Drag-and-drop node positioning</description></item>
    /// <item><description>Interactive connection creation between node connectors</description></item>
    /// <item><description>Zoom and pan navigation</description></item>
    /// <item><description>Context menus for nodes and canvas</description></item>
    /// <item><description>Visual connection paths with Bezier curves</description></item>
    /// <item><description>Custom visual overlays</description></item>
    /// </list>
    /// <para>
    /// The canvas automatically synchronizes with the underlying <see cref="Graph"/> object,
    /// reflecting changes to nodes and connections in real-time.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In XAML:
    /// // &lt;local:FlowGraphCanvas x:Name="graphCanvas" /&gt;
    ///
    /// // In code-behind:
    /// graphCanvas.Graph = new MyGraph();
    /// graphCanvas.PreviewCanvasContextMenu += (cm, pos) =>
    /// {
    ///     cm.Items.Add(new MenuItem { Header = "Add Node" });
    /// };
    /// </code>
    /// </example>
    public partial class FlowGraphCanvas : Canvas
    {
        #region Fields

        private bool _isMoving = false;
        private bool _isPanning = false;
        private bool _isConnecting = false;

        private Path _targetPath;
        private NodeBase _targetNode = null;
        private OutConnector _targetConnector = null;

        private Point _previousLocation;

        // Node and connector visual element tracking
        private readonly Dictionary<NodeBase, NodeControl> _nodes = new Dictionary<NodeBase, NodeControl>();
        private readonly Dictionary<Tuple<OutConnector, InConnector>, Path> _connections = new Dictionary<Tuple<OutConnector, InConnector>, Path>();
        private readonly List<UIElement> _customVisuals = new List<UIElement>();

        private int _graphGeneration;

        // Transform objects for zoom/pan
        private readonly ScaleTransform _scaleTransform = new ScaleTransform();
        private readonly ScaleTransform _backgroundScaleTransform = new ScaleTransform();
        private readonly TranslateTransform _backgroundTranslate = new TranslateTransform();

        // Custom cursors
        private readonly Cursor _panHandClosedCursor;
        private readonly Cursor _connectingCursor;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Graph"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GraphProperty = DependencyProperty.Register(
            nameof(Graph),
            typeof(Graph),
            typeof(FlowGraphCanvas),
            new FrameworkPropertyMetadata(null, GraphPropertyChangedCallback));

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether a node is currently being moved.
        /// </summary>
        /// <value><c>true</c> if a node drag operation is in progress; otherwise, <c>false</c>.</value>
        public bool IsMoving => _isMoving;

        /// <summary>
        /// Gets or sets the graph to display and edit.
        /// </summary>
        /// <value>The <see cref="DAG.Graph"/> instance to visualize.</value>
        /// <remarks>
        /// When set, the canvas will automatically subscribe to the graph's events and
        /// redraw all nodes and connections.
        /// </remarks>
        public Graph Graph
        {
            get => (Graph)GetValue(GraphProperty);
            set => SetValue(GraphProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the user right-clicks on the canvas background.
        /// </summary>
        /// <remarks>
        /// Subscribe to this event to populate a context menu with canvas-specific actions
        /// such as adding new nodes.
        /// </remarks>
        public event CanvasContextMenu PreviewCanvasContextMenu;

        /// <summary>
        /// Occurs when the user right-clicks on a node.
        /// </summary>
        /// <remarks>
        /// Subscribe to this event to populate a context menu with node-specific actions
        /// such as editing or deleting the node.
        /// </remarks>
        public event NodeContextMenu PreviewNodeContextMenu;

        /// <summary>
        /// Occurs when the user clicks on an output connector to auto-create a connection.
        /// </summary>
        public event AutoConnectionClicked AutoConnection_Clicked;

        /// <summary>
        /// Occurs when a connection visual is added to the canvas.
        /// </summary>
        public event ConnectionAdded ConnectionAdded;

        /// <summary>
        /// Occurs when a connection visual is removed from the canvas.
        /// </summary>
        public event ConnectionRemoved ConnectionRemoved;

        /// <summary>
        /// Occurs when the graph has been redrawn.
        /// </summary>
        public event GraphRedrawn GraphRedrawn;

        /// <summary>
        /// Occurs when a node has been moved to a new position.
        /// </summary>
        public event NodeMoved NodeMoved;

        /// <summary>
        /// Occurs when a node's size has changed.
        /// </summary>
        public event NodeSizeChanged NodeSizeChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FlowGraphCanvas"/> class.
        /// </summary>
        public FlowGraphCanvas()
        {
            InitializeComponent();

            TransformGroup t = new TransformGroup();
            t.Children.Add(_backgroundTranslate);
            t.Children.Add(_backgroundScaleTransform);
            VisualBrush b = (VisualBrush)Background;
            b.RelativeTransform = t;

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Properties.Resources.Pan_Hand_Closed))
            {
                _panHandClosedCursor = new Cursor(ms);
            }

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(Properties.Resources.AddPointCursor))
            {
                _connectingCursor = new Cursor(ms);
            }

            // If mouse capture is lost unexpectedly (e.g., another process grabs focus
            // mid-drag), reset interaction state so the cursor and in-progress drag visuals
            // don't stick.
            GraphCanvas.LostMouseCapture += GraphCanvas_LostMouseCapture;
        }

        /// <summary>
        /// Clears drag state when the canvas loses mouse capture.
        /// </summary>
        /// <param name="sender">The canvas that raised the event.</param>
        /// <param name="e">The mouse event data.</param>
        private void GraphCanvas_LostMouseCapture(object sender, MouseEventArgs e)
        {
            _isPanning = false;
            _isMoving = false;
            if (_isConnecting)
            {
                _isConnecting = false;
                if (_targetPath != null) { GraphCanvas.Children.Remove(_targetPath); }
            }
            Mouse.OverrideCursor = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the visual path for a connection.
        /// </summary>
        /// <param name="fromNode">The source output connector.</param>
        /// <param name="toNode">The destination input connector.</param>
        /// <returns>The <see cref="Path"/> representing the connection, or null if not found.</returns>
        public Path GetConnectionPath(OutConnector fromNode, InConnector toNode)
        {
            return GetConnectionPath(new Tuple<OutConnector, InConnector>(fromNode, toNode));
        }

        /// <summary>
        /// Gets the visual path for a connection.
        /// </summary>
        /// <param name="connection">The connection tuple to find.</param>
        /// <returns>The <see cref="Path"/> representing the connection, or null if not found.</returns>
        public Path GetConnectionPath(Tuple<OutConnector, InConnector> connection)
        {
            return _connections.ContainsKey(connection) ? _connections[connection] : null;
        }

        /// <summary>
        /// Gets the visual control for a node.
        /// </summary>
        /// <param name="targetNode">The node to find the control for.</param>
        /// <returns>The <see cref="NodeControl"/> for the node, or null if not found.</returns>
        public NodeControl GetNodeControl(NodeBase targetNode)
        {
            return _nodes.ContainsKey(targetNode) ? _nodes[targetNode] : null;
        }

        /// <summary>
        /// Adds a custom visual element to the canvas.
        /// </summary>
        /// <param name="customVisual">The UI element to add.</param>
        /// <remarks>
        /// Custom visuals are scaled and panned along with the graph nodes.
        /// Use this to add annotations, highlights, or other overlay elements.
        /// </remarks>
        public void AddVisual(UIElement customVisual)
        {
            customVisual.RenderTransform = _scaleTransform;
            _customVisuals.Add(customVisual);
            GraphCanvas.Children.Add(customVisual);
        }

        /// <summary>
        /// Removes a custom visual element from the canvas.
        /// </summary>
        /// <param name="customVisual">The UI element to remove.</param>
        public void RemoveVisual(UIElement customVisual)
        {
            _customVisuals.Remove(customVisual);
            GraphCanvas.Children.Remove(customVisual);
        }

        /// <summary>
        /// Gets a read-only collection of all custom visual elements on the canvas.
        /// </summary>
        /// <returns>A read-only collection of custom UI elements.</returns>
        public ReadOnlyCollection<UIElement> GetVisuals()
        {
            return new ReadOnlyCollection<UIElement>(_customVisuals);
        }

        #endregion

        #region Private Methods - Graph Handling

        /// <summary>
        /// Rebinds graph event handlers when the <see cref="Graph"/> property changes.
        /// </summary>
        /// <param name="d">The canvas whose graph changed.</param>
        /// <param name="e">The dependency-property change details.</param>
        private static void GraphPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FlowGraphCanvas thisControl = (FlowGraphCanvas)d;

            // Release the old graph
            if (e.OldValue != null)
            {
                Graph oldValue = (Graph)e.OldValue;
                oldValue.Nodes.CollectionChanged -= thisControl.Nodes_CollectionChanged;
                oldValue.ConnectionsAdded -= thisControl.Graph_ConnectionsAdded;
                oldValue.ConnectionsRemoved -= thisControl.Graph_ConnectionsRemoved;
                BindingOperations.ClearBinding(thisControl._scaleTransform, ScaleTransform.ScaleXProperty);
                BindingOperations.ClearBinding(thisControl._scaleTransform, ScaleTransform.ScaleYProperty);
            }

            // Add handlers for the new graph
            if (e.NewValue != null)
            {
                Graph newValue = (Graph)e.NewValue;
                newValue.Nodes.CollectionChanged += thisControl.Nodes_CollectionChanged;
                newValue.ConnectionsAdded += thisControl.Graph_ConnectionsAdded;
                newValue.ConnectionsRemoved += thisControl.Graph_ConnectionsRemoved;

                Binding binder = new Binding(nameof(DAG.Graph.Scale)) { Source = thisControl.Graph };
                _ = BindingOperations.SetBinding(thisControl._scaleTransform, ScaleTransform.ScaleXProperty, binder);
                _ = BindingOperations.SetBinding(thisControl._scaleTransform, ScaleTransform.ScaleYProperty, binder);
            }

            thisControl._graphGeneration++;
            thisControl.RedrawGraph();
        }

        /// <summary>
        /// Rebuilds all node and connection visuals from the current graph model.
        /// </summary>
        private void RedrawGraph()
        {
            // Unsubscribe event handlers before clearing
            foreach (var nc in _nodes.Values)
            {
                nc.Delete_Clicked -= Node_Delete_Clicked;
                nc.AutoConnection_Clicked -= AutoConnection_Clicked;
                nc.SizeChanged -= Node_SizeChanged;
            }

            // Clear existing visual elements
            GraphCanvas.Children.Clear();
            _nodes.Clear();
            _connections.Clear();
            _customVisuals.Clear();

            if (Graph == null) { return; }

            int nodesLoaded = 0;
            int expectedNodeCount = Graph.Nodes.Count;
            int connectionsDrawn = 0;
            int capturedGeneration = _graphGeneration;

            foreach (NodeBase node in Graph.Nodes)
            {
                AddNode(node);
                GetNodeControl(node).Loaded += (object sender, RoutedEventArgs e) =>
                {
                    if (_graphGeneration != capturedGeneration) { return; }
                    int loadedCount = Interlocked.Increment(ref nodesLoaded);
                    if (loadedCount == expectedNodeCount && Interlocked.Exchange(ref connectionsDrawn, 1) == 0)
                    {
                        foreach (Tuple<OutConnector, InConnector> connection in Graph.Connections)
                        {
                            AddConnection(connection);
                        }
                    }
                };
            }

            GraphRedrawn?.Invoke();
        }

        /// <summary>
        /// Removes visual paths for connections removed from the graph model.
        /// </summary>
        /// <param name="connections">The graph connections that were removed.</param>
        private void Graph_ConnectionsRemoved(Tuple<OutConnector, InConnector>[] connections)
        {
            foreach (var connection in connections)
            {
                if (_connections.ContainsKey(connection) == false) { continue; }
                Path path = _connections[connection];
                GraphCanvas.Children.Remove(path);
                _ = _connections.Remove(connection);
                ConnectionRemoved?.Invoke(connection, path);
            }
        }

        /// <summary>
        /// Adds visual paths for connections added to the graph model.
        /// </summary>
        /// <param name="connections">The graph connections that were added.</param>
        private void Graph_ConnectionsAdded(Tuple<OutConnector, InConnector>[] connections)
        {
            foreach (var connection in connections)
            {
                AddConnection(connection);
            }
        }

        /// <summary>
        /// Creates and registers the visual path for a graph connection.
        /// </summary>
        /// <param name="connection">The connection to render.</param>
        private void AddConnection(Tuple<OutConnector, InConnector> connection)
        {
            if (_connections.ContainsKey(connection)) { return; }

            OutConnector outConnector = connection.Item1;
            InConnector inConnector = connection.Item2;

            if (outConnector == null || outConnector.Parent == null) { return; }
            if (inConnector == null || inConnector.Parent == null) { return; }
            if (_nodes.ContainsKey(outConnector.Parent) == false) { return; }
            if (_nodes.ContainsKey(inConnector.Parent) == false) { return; }

            NodeControl outNode = _nodes[outConnector.Parent];
            NodeControl inNode = _nodes[inConnector.Parent];

            if (outNode.Node.Outputs.Count != outNode.OutConnectors.Count) { outNode.RefreshOutConnectors(); }
            if (inNode.Node.Inputs.Count != inNode.InConnectors.Count) { inNode.RefreshInConnectors(); }

            if (outNode.OutConnectors.ContainsKey(outConnector) == false) { return; }
            Path p = CreatePath(outNode.OutConnectors[outConnector]);

            _connections.Add(connection, p);

            if (outNode.OutConnectors.ContainsKey(outConnector) && inNode.InConnectors.ContainsKey(inConnector))
            {
                DrawConnection(p, outNode.OutConnectors[outConnector], inNode.InConnectors[inConnector]);
            }

            ConnectionAdded?.Invoke(connection, p);
        }

        /// <summary>
        /// Synchronizes node controls with changes in the graph node collection.
        /// </summary>
        /// <param name="sender">The node collection that raised the event.</param>
        /// <param name="e">The collection change details.</param>
        private void Nodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (object item in e.OldItems)
                {
                    NodeBase node = (NodeBase)item;
                    if (node == null) { continue; }
                    if (!_nodes.TryGetValue(node, out var nodeControl)) { continue; }

                    // Delete the node from the UI
                    nodeControl.Delete_Clicked -= Node_Delete_Clicked;
                    nodeControl.AutoConnection_Clicked -= AutoConnection_Clicked;
                    nodeControl.SizeChanged -= Node_SizeChanged;
                    GraphCanvas.Children.Remove(nodeControl);
                    _ = _nodes.Remove(node);
                }
            }

            if (e.NewItems != null)
            {
                foreach (object item in e.NewItems)
                {
                    AddNode((NodeBase)item);
                }
            }
        }

        /// <summary>
        /// Creates and registers the visual control for a graph node.
        /// </summary>
        /// <param name="node">The node to render.</param>
        private void AddNode(NodeBase node)
        {
            if (node == null) { return; }

            NodeControl newNode = new NodeControl(node) { RenderTransform = _scaleTransform };
            newNode.Delete_Clicked += Node_Delete_Clicked;
            newNode.AutoConnection_Clicked += AutoConnection_Clicked;
            newNode.SizeChanged += Node_SizeChanged;

            _ = GraphCanvas.Children.Add(newNode);
            _nodes.Add(newNode.Node, newNode);
        }

        #endregion

        #region Private Methods - Mouse Handling

        /// <summary>
        /// Removes the node selected by the context menu.
        /// </summary>
        /// <param name="sender">The menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void Cmi_Click(object sender, RoutedEventArgs e)
        {
            _ = Graph.Nodes.Remove(_targetNode);
        }

        /// <summary>
        /// Starts node movement, connection creation, context-menu display, or panning.
        /// </summary>
        /// <param name="sender">The canvas that raised the event.</param>
        /// <param name="e">The mouse-button event data.</param>
        private void GraphCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                foreach (KeyValuePair<NodeBase, NodeControl> node in _nodes)
                {
                    if (node.Value.IsMouseOver)
                    {
                        _targetNode = node.Key;
                        ContextMenu cm = new ContextMenu();
                        BitmapImage bmp = new BitmapImage(new Uri("pack://application:,,,/DAGControls;component/Resources/Delete.png"));
                        Image deleteIcon = new Image() { Source = bmp };
                        MenuItem cmi = new MenuItem() { Header = "Delete", Icon = deleteIcon };
                        cmi.Click += Cmi_Click;
                        _ = cm.Items.Add(cmi);
                        PreviewNodeContextMenu?.Invoke(cm, _targetNode);
                        if (cm.Items.Count > 0) { cm.IsOpen = true; }
                        return;
                    }
                }

                Point mousePosition = e.GetPosition(GraphCanvas);
                ContextMenu canvasCM = new ContextMenu();
                PreviewCanvasContextMenu?.Invoke(canvasCM, mousePosition);
                if (canvasCM.Items.Count > 0) { canvasCM.IsOpen = true; }
                return;
            }
            else
            {
                foreach (KeyValuePair<NodeBase, NodeControl> node in _nodes)
                {
                    if (node.Value.IsMouseOver == false) { continue; }

                    // Check if out connector then initiate connecting
                    if (node.Key.Outputs.Count != node.Value.OutConnectors.Count) { node.Value.RefreshOutConnectors(); }
                    foreach (KeyValuePair<OutConnector, Ellipse> item in node.Value.OutConnectors)
                    {
                        if (item.Value.IsMouseOver)
                        {
                            // Initiate the connection path and set targets
                            _isConnecting = true;
                            _targetPath = CreatePath(item.Value);
                            _targetNode = node.Key;
                            _targetConnector = item.Key;
                            // Capture so MouseUp / LostMouseCapture fires reliably even if
                            // the cursor leaves the canvas bounds during the drag.
                            _ = GraphCanvas.CaptureMouse();
                            return;
                        }
                    }

                    // Check if input connector, if connected then break and re-initiate connecting
                    if (node.Key.Inputs.Count != node.Value.InConnectors.Count) { node.Value.RefreshInConnectors(); }
                    foreach (KeyValuePair<InConnector, Ellipse> item in node.Value.InConnectors)
                    {
                        if (item.Value.IsMouseOver)
                        {
                            foreach (Tuple<OutConnector, InConnector> connection in Graph.Connections)
                            {
                                if (connection.Item2.Equals(item.Key))
                                {
                                    _ = Graph.RemoveConnection(connection);
                                    _isConnecting = true;
                                    _targetPath = CreatePath(_nodes[connection.Item1.Parent].OutConnectors[connection.Item1]);
                                    _targetConnector = connection.Item1;
                                    _targetNode = _targetConnector.Parent;
                                    _ = GraphCanvas.CaptureMouse();
                                    return;
                                }
                            }
                        }
                    }

                    // Move the node
                    _isMoving = true;
                    _targetNode = node.Key;
                    _targetPath = null;
                    _targetConnector = null;
                    node.Value.NodeBorder.BorderBrush = Brushes.LightCyan;
                    _previousLocation = e.GetPosition(GraphCanvas);
                    _ = GraphCanvas.CaptureMouse();
                    return;
                }

                // If it gets to this point then go into pan mode
                _isPanning = true;
                Mouse.OverrideCursor = _panHandClosedCursor;
                _ = GraphCanvas.CaptureMouse();
            }
        }

        /// <summary>
        /// Updates the active drag, connection preview, or pan operation.
        /// </summary>
        /// <param name="sender">The canvas that raised the event.</param>
        /// <param name="e">The mouse event data.</param>
        private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(GraphCanvas);

            if (_isConnecting)
            {
                Mouse.OverrideCursor = _connectingCursor;
                if (_targetPath != null)
                {
                    DrawConnection(_targetPath, ((PathGeometry)_targetPath.Data).Figures[0].StartPoint, pos);
                }
            }
            else if (_isMoving)
            {
                // Move the target node
                _targetNode.LeftPosition += (pos.X - _previousLocation.X) / Graph.Scale;
                _targetNode.TopPosition += (pos.Y - _previousLocation.Y) / Graph.Scale;

                // Update all connection paths connected to the target moving node
                Point startPoint, endPoint;
                Path p;
                foreach (Tuple<OutConnector, InConnector> connection in Graph.Connections)
                {
                    // Use TryGetValue to safely access the connection path
                    if (!_connections.TryGetValue(connection, out p)) { continue; }

                    // Out connections
                    if (connection.Item1.Parent == _targetNode)
                    {
                        startPoint = ((PathGeometry)p.Data).Figures[0].StartPoint;
                        startPoint = new Point(startPoint.X + pos.X - _previousLocation.X, startPoint.Y + pos.Y - _previousLocation.Y);
                        endPoint = ((BezierSegment)((PathGeometry)p.Data).Figures[0].Segments[0]).Point3;
                    }
                    // In connection
                    else if (connection.Item2.Parent == _targetNode)
                    {
                        startPoint = ((PathGeometry)p.Data).Figures[0].StartPoint;
                        endPoint = ((BezierSegment)((PathGeometry)p.Data).Figures[0].Segments[0]).Point3;
                        endPoint = new Point(endPoint.X + pos.X - _previousLocation.X, endPoint.Y + pos.Y - _previousLocation.Y);
                    }
                    else
                    {
                        continue;
                    }

                    DrawConnection(p, startPoint, endPoint);
                }
            }
            else if (_isPanning)
            {
                double xTrans = pos.X - _previousLocation.X;
                double yTrans = pos.Y - _previousLocation.Y;

                // Set Translate Transform for all nodes
                foreach (KeyValuePair<NodeBase, NodeControl> item in _nodes)
                {
                    item.Key.LeftPosition += xTrans;
                    item.Key.TopPosition += yTrans;
                }

                // Reset the position of each connector
                foreach (KeyValuePair<Tuple<OutConnector, InConnector>, Path> item in _connections)
                {
                    TranslateConnection(item.Value, xTrans, yTrans);
                }

                // Reset the position of each custom visual
                foreach (UIElement visual in _customVisuals)
                {
                    Canvas.SetLeft(visual, Canvas.GetLeft(visual) + xTrans);
                    Canvas.SetTop(visual, Canvas.GetTop(visual) + yTrans);
                }

                // Move the background
                if (ActualWidth > 0 && ActualHeight > 0)
                {
                    _backgroundTranslate.X += xTrans / (ActualWidth * _backgroundScaleTransform.ScaleX);
                    _backgroundTranslate.Y += yTrans / (ActualHeight * _backgroundScaleTransform.ScaleY);
                }
            }

            _previousLocation = pos;
        }

        /// <summary>
        /// Completes the active mouse interaction and releases mouse capture.
        /// </summary>
        /// <param name="sender">The canvas that raised the event.</param>
        /// <param name="e">The mouse-button event data.</param>
        private void GraphCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;

            if (_isMoving)
            {
                _isMoving = false;
                // TryGetValue: _targetNode may have been removed from the graph between
                // MouseDown and MouseUp (e.g. via a context-menu delete on another control).
                if (_targetNode != null && _nodes.TryGetValue(_targetNode, out var movedNc))
                {
                    movedNc.NodeBorder.BorderBrush = Brushes.Transparent;
                }
                NodeMoved?.Invoke(_targetNode);
            }
            else if (_isConnecting)
            {
                bool raiseAddEvent = false;
                // Identify which node to connect to
                foreach (KeyValuePair<NodeBase, NodeControl> node in _nodes)
                {
                    if (node.Value.IsMouseOver == false) { continue; }

                    // If the out connector was clicked then raise the auto connection event
                    if (node.Key.Equals(_targetNode))
                    {
                        foreach (KeyValuePair<OutConnector, Ellipse> item in node.Value.OutConnectors)
                        {
                            if (item.Value.IsMouseOver && item.Key.Equals(_targetConnector)) { raiseAddEvent = true; }
                        }
                    }

                    // If mouse is over a valid input connector then add new connection
                    if (node.Key.Inputs.Count != node.Value.InConnectors.Count) { node.Value.RefreshInConnectors(); }
                    foreach (KeyValuePair<InConnector, Ellipse> item in node.Value.InConnectors)
                    {
                        if (item.Value.IsMouseOver && node.Key != _targetNode)
                        {
                            // Add new connection
                            Tuple<OutConnector, InConnector> connectionToAdd = new Tuple<OutConnector, InConnector>(_targetConnector, item.Key);
                            if (Graph.AddConnection(connectionToAdd))
                            {
                                // Remove any connections to this input, can only have one input connector
                                for (int i = Graph.Connections.Count() - 1; i >= 0; i--)
                                {
                                    if (Graph.Connections[i].Equals(connectionToAdd)) { continue; }
                                    if (Graph.Connections[i].Item2 == item.Key)
                                    {
                                        _ = Graph.RemoveConnection(Graph.Connections[i]);
                                    }
                                }
                            }

                            break; // Only one connection can be made
                        }
                    }
                }

                if (raiseAddEvent) { AutoConnection_Clicked?.Invoke(_targetConnector); }
                GraphCanvas.Children.Remove(_targetPath);
                _isConnecting = false;
            }

            Mouse.OverrideCursor = null;
            GraphCanvas.ReleaseMouseCapture();
        }

        /// <summary>
        /// Zooms the graph around the current mouse position.
        /// </summary>
        /// <param name="sender">The canvas that raised the event.</param>
        /// <param name="e">The mouse-wheel event data.</param>
        private void GraphCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Graph == null) return;
            Point mousePosition = e.GetPosition(GraphCanvas);

            double ratio = Math.Pow(0.9, -e.Delta / 120);
            double scale = ratio * Graph.Scale;
            if (scale > 4) { scale = 4; }
            if (scale < .1) { scale = .1; }

            double oldScale = Graph.Scale;

            // Apply zoom
            Graph.Scale = scale;
            _backgroundScaleTransform.ScaleX = scale * 1.001;
            _backgroundScaleTransform.ScaleY = scale * 1.001;

            // Scale each node away from or towards the mouse position
            foreach (KeyValuePair<NodeBase, NodeControl> node in _nodes)
            {
                // Determine the point where zoomed in
                double zoomX = (mousePosition.X - node.Key.LeftPosition) / oldScale;
                double zoomY = (mousePosition.Y - node.Key.TopPosition) / oldScale;

                // Calculate new x and y based on zoom
                double px = (-zoomX * scale) + mousePosition.X;
                double py = (-zoomY * scale) + mousePosition.Y;

                // Apply transform away from point
                node.Key.LeftPosition = px;
                node.Key.TopPosition = py;
            }

            // Scale each custom visual away from or towards the mouse position
            double left, top;
            foreach (UIElement visual in _customVisuals)
            {
                left = Canvas.GetLeft(visual);
                top = Canvas.GetTop(visual);

                // Determine the point where zoomed in
                double zoomX = (mousePosition.X - left) / oldScale;
                double zoomY = (mousePosition.Y - top) / oldScale;

                // Calculate new x and y based on zoom
                double px = (-zoomX * scale) + mousePosition.X;
                double py = (-zoomY * scale) + mousePosition.Y;

                // Apply transform away from point
                Canvas.SetLeft(visual, px);
                Canvas.SetTop(visual, py);
            }

            // Shift each connector to the updated node connector positions
            // Handle currently connecting path
            if (_isConnecting && _targetPath != null && _targetNode != null && _nodes.TryGetValue(_targetNode, out var targetNodeControl))
            {
                if (targetNodeControl.OutConnectors.TryGetValue(_targetConnector, out var targetOutConnector))
                {
                    // Start Position
                    Point connectorPosition = targetOutConnector.TranslatePoint(new Point(), targetNodeControl);
                    double startY = GetTop(targetNodeControl) + ((connectorPosition.Y + (targetOutConnector.ActualHeight * .5)) * scale);
                    Point startPoint = new Point(_targetNode.LeftPosition + ((targetNodeControl.ActualWidth - 1) * scale), startY);

                    // End Position
                    Point endPoint = mousePosition;

                    DrawConnection(_targetPath, startPoint, endPoint);
                }
            }

            // Handle existing connections
            foreach (KeyValuePair<Tuple<OutConnector, InConnector>, Path> item in _connections)
            {
                NodeControl sourceNode = _nodes[item.Key.Item1.Parent];
                NodeControl sinkNode = _nodes[item.Key.Item2.Parent];
                Ellipse sourceConnector = _nodes[sourceNode.Node].OutConnectors[item.Key.Item1];
                Ellipse sinkConnector = _nodes[sinkNode.Node].InConnectors[item.Key.Item2];

                // Get Start Point
                Point connectorPosition = sourceConnector.TranslatePoint(new Point(), sourceNode);
                double startY = GetTop(sourceNode) + ((connectorPosition.Y + (sourceConnector.ActualHeight * .5)) * scale);
                Point startPoint = new Point(item.Key.Item1.Parent.LeftPosition + ((sourceNode.ActualWidth - 1) * scale), startY);

                // Get End Point
                connectorPosition = sinkConnector.TranslatePoint(new Point(), sinkNode);
                double endY = GetTop(sinkNode) + ((connectorPosition.Y + (sinkConnector.ActualHeight * .5)) * scale);
                Point endPoint = new Point(item.Key.Item2.Parent.LeftPosition + (1 * scale), endY);

                DrawConnection(item.Value, startPoint, endPoint);
            }
        }

        #endregion

        #region Private Methods - Event Handlers

        /// <summary>
        /// Redraws affected connection paths after a node size changes.
        /// </summary>
        /// <param name="sender">The node control that changed size.</param>
        /// <param name="e">The size-change details.</param>
        private void Node_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!(sender is NodeControl nodeCntrl)) { return; }
            foreach (KeyValuePair<Tuple<OutConnector, InConnector>, Path> con in _connections)
            {
                if (con.Key.Item1.Parent.Equals(nodeCntrl.Node) || con.Key.Item2.Parent.Equals(nodeCntrl.Node))
                {
                    DrawConnection(con.Value, _nodes[con.Key.Item1.Parent].OutConnectors[con.Key.Item1], _nodes[con.Key.Item2.Parent].InConnectors[con.Key.Item2]);
                }
            }

            NodeSizeChanged?.Invoke(nodeCntrl.Node);
        }

        /// <summary>
        /// Removes the node whose delete command was clicked.
        /// </summary>
        /// <param name="sender">The node control that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void Node_Delete_Clicked(object sender, RoutedEventArgs e)
        {
            _ = Graph.Nodes.Remove(((NodeControl)sender).Node);
        }

        #endregion

        #region Private Methods - Path Drawing

        /// <summary>
        /// Creates the initial bezier path for a connection source.
        /// </summary>
        /// <param name="connectionSource">The output connector visual that starts the path.</param>
        /// <returns>The created path.</returns>
        private Path CreatePath(Ellipse connectionSource)
        {
            // Identify the center point of node connector. TransformToAncestor throws
            // InvalidOperationException if connectionSource is not a descendant of GraphCanvas
            // - this happens briefly during initial layout or after a node is removed from
            // the canvas but before the connect-drag is cleaned up.
            if (connectionSource == null || !GraphCanvas.IsAncestorOf(connectionSource))
            {
                return new Path { Data = new PathGeometry() };
            }

            Point connectorPosition = connectionSource.TransformToAncestor(GraphCanvas).Transform(new Point());
            Point connectorStart = new Point(
                connectorPosition.X + (connectionSource.ActualWidth * .5 * Graph.Scale),
                connectorPosition.Y + (connectionSource.ActualHeight * .5 * Graph.Scale));

            // Define the bezier segment
            PathFigure myPathFigure = new PathFigure() { StartPoint = connectorStart };
            myPathFigure.Segments.Add(new BezierSegment(connectorStart, connectorStart, connectorStart, true) { IsSmoothJoin = true });

            // Create the path
            Path p = new Path()
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Data = new PathGeometry(new PathFigure[] { myPathFigure }),
                SnapsToDevicePixels = false,
                UseLayoutRounding = true,
                IsHitTestVisible = false
            };

            SetZIndex(p, -1); // Render behind the nodes
            _ = GraphCanvas.Children.Add(p);

            return p;
        }

        /// <summary>
        /// Updates a connection path to span the specified start and end points.
        /// </summary>
        /// <param name="connector">The connection path to update.</param>
        /// <param name="startPoint">The start point of the connection.</param>
        /// <param name="endPoint">The end point of the connection.</param>
        private void DrawConnection(Path connector, Point startPoint, Point endPoint)
        {
            PathFigure pf = ((PathGeometry)connector.Data).Figures[0];
            pf.StartPoint = startPoint;

            BezierSegment b = (BezierSegment)pf.Segments[0];
            b.Point1 = new Point(startPoint.X + ((endPoint.X - startPoint.X) * .5), startPoint.Y);
            b.Point2 = new Point(startPoint.X + ((endPoint.X - startPoint.X) * .5), startPoint.Y + ((endPoint.Y - startPoint.Y) * .9));
            b.Point3 = endPoint;
        }

        /// <summary>
        /// Updates a connection path using output and input connector visuals.
        /// </summary>
        /// <param name="connector">The connection path to update.</param>
        /// <param name="outConnector">The output connector visual.</param>
        /// <param name="inConnector">The input connector visual.</param>
        private void DrawConnection(Path connector, Ellipse outConnector, Ellipse inConnector)
        {
            // Get Start Point
            Point connectorPosition = outConnector.TransformToAncestor(GraphCanvas).Transform(new Point());
            Point startPoint = new Point(
                connectorPosition.X + (outConnector.ActualWidth * .5 * Graph.Scale),
                connectorPosition.Y + (outConnector.ActualHeight * .5 * Graph.Scale));

            // Get End Point
            connectorPosition = inConnector.TransformToAncestor(GraphCanvas).Transform(new Point());
            Point endPoint = new Point(
                connectorPosition.X + (inConnector.ActualWidth * .5 * Graph.Scale),
                connectorPosition.Y + (inConnector.ActualHeight * .5 * Graph.Scale));

            DrawConnection(connector, startPoint, endPoint);
        }

        /// <summary>
        /// Translates every point in an existing connection path.
        /// </summary>
        /// <param name="connector">The connection path to translate.</param>
        /// <param name="xTrans">The horizontal translation amount.</param>
        /// <param name="yTrans">The vertical translation amount.</param>
        private void TranslateConnection(Path connector, double xTrans, double yTrans)
        {
            PathFigure pf = ((PathGeometry)connector.Data).Figures[0];
            pf.StartPoint = new Point(pf.StartPoint.X + xTrans, pf.StartPoint.Y + yTrans);

            BezierSegment b = (BezierSegment)pf.Segments[0];
            b.Point1 = new Point(b.Point1.X + xTrans, b.Point1.Y + yTrans);
            b.Point2 = new Point(b.Point2.X + xTrans, b.Point2.Y + yTrans);
            b.Point3 = new Point(b.Point3.X + xTrans, b.Point3.Y + yTrans);
        }

        #endregion
    }
}
