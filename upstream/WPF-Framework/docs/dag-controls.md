# DAG Controls

[<- Previous: Database Controls](database-controls.md) | [Back to Index](index.md) | [Next: Software Update ->](software-update.md)

## Overview

The DAG subsystem provides two libraries for working with directed acyclic graphs:

1. **DAG** -- a platform-agnostic model library (`net10.0`) defining the graph, node, and connector abstractions with cycle detection, topological sorting, path queries, and XML serialization.
2. **DAGControls** -- a WPF control library (`net10.0-windows`) providing an interactive visual editor for creating, connecting, and arranging nodes on a canvas.

## DAG Model

**Namespace:** `DAG`

### Graph (abstract)

The `Graph` class is the central container for a directed acyclic graph. It manages an `ObservableCollection<NodeBase>` of nodes and a read-only collection of connections between node connectors. Derived classes must implement `ReadNodeRequested` (for deserialization) and `AddToBaseElement` (for serialization).

```csharp
public abstract class Graph : INotifyPropertyChanged
{
    // Collections
    public readonly ObservableCollection<NodeBase> Nodes;
    public ReadOnlyCollection<Tuple<OutConnector, InConnector>> Connections { get; }

    // Display
    public double Scale { get; set; }  // Zoom level, default 1.0

    // Connection management
    public bool AddConnection(OutConnector outConnector, InConnector inConnector);
    public bool RemoveConnection(OutConnector outConnector, InConnector inConnector);

    // DAG operations
    public bool WouldCreateCycle(OutConnector from, InConnector to);
    public bool HasPath(NodeBase fromNode, NodeBase toNode);
    public bool HasCycle();
    public List<NodeBase> TopologicalSort();      // Returns null if cycle detected
    public List<NodeBase> GetRootNodes();          // Nodes with no incoming connections
    public List<NodeBase> GetLeafNodes();          // Nodes with no outgoing connections
    public List<NodeBase> GetAncestors(NodeBase node);
    public List<NodeBase> GetDescendants(NodeBase node);
    public int GetGraphDepth();                    // Longest path length

    // Serialization
    public XElement ToXElement();
    protected abstract void AddToBaseElement(XElement baseElement);
    protected abstract NodeBase ReadNodeRequested(XElement nodeElement);
}
```

**Events:**

| Event | Signature | Description |
|---|---|---|
| `PreviewConnectionsAdded` | `(Tuple<OutConnector, InConnector>[] connections, ref bool cancel)` | Raised before connections are added; set `cancel = true` to prevent |
| `ConnectionsAdded` | `(Tuple<OutConnector, InConnector>[] connections)` | Raised after connections are added |
| `PreviewConnectionsRemoved` | `(Tuple<OutConnector, InConnector>[] connections, ref bool cancel)` | Raised before connections are removed; set `cancel = true` to prevent |
| `ConnectionsRemoved` | `(Tuple<OutConnector, InConnector>[] connections)` | Raised after connections are removed |

### NodeBase (abstract)

The abstract base class for all nodes. Each node has a unique GUID, canvas position, input connectors, output connectors, and a display name.

```csharp
public abstract class NodeBase : INotifyPropertyChanged
{
    public Guid NodeGuid { get; }
    public abstract string Name { get; }
    public double LeftPosition { get; set; }    // Canvas X coordinate
    public double TopPosition { get; set; }     // Canvas Y coordinate

    public ObservableCollection<OutConnector> Outputs { get; }
    public ObservableCollection<InConnector> Inputs { get; }

    public abstract NodeBase Clone();
    public XElement ToXElement();
    protected abstract void AddToBaseElement(XElement baseElement);
}
```

Derived classes must:
- Override `Name` to provide a display label
- Override `Clone()` to create a deep copy with a new GUID
- Override `AddToBaseElement()` to serialize custom properties
- Define input and output connectors in the constructor

### InConnector / OutConnector

Connectors are the endpoints for connections between nodes. Each connector has a `Name`, an optional `Unit` string for compatibility validation, and a `Parent` reference to its owning node.

```csharp
// InConnector -- receives one incoming connection
public class InConnector : INotifyPropertyChanged
{
    public string Name { get; set; }
    public string Unit { get; set; }
    public NodeBase Parent { get; }

    public InConnector(string name, string unit, NodeBase parent);
}

// OutConnector -- can feed multiple downstream connections
public class OutConnector : INotifyPropertyChanged
{
    public string Name { get; set; }
    public string Unit { get; set; }
    public NodeBase Parent { get; }

    public OutConnector(string name, string unit, NodeBase parent);
}
```

## Creating Custom Nodes

Extend `NodeBase` and define connectors in the constructor. The following pattern is demonstrated in the DAG.Demo project:

```csharp
public class HydraulicNode : NodeBase
{
    private double _manningsN = 0.035;

    public override string Name => "Hydraulic Reach";

    public double ManningsN
    {
        get => _manningsN;
        set
        {
            if (Math.Abs(value - _manningsN) > 1E-15)
            {
                _manningsN = value;
                RaisePropertyChanged(nameof(ManningsN));
            }
        }
    }

    public HydraulicNode()
    {
        Inputs.Add(new InConnector("Upstream Flow", "cfs", this));
        Inputs.Add(new InConnector("Lateral Inflow", "cfs", this));
        Outputs.Add(new OutConnector("Downstream Flow", "cfs", this));
    }

    // Deserialization constructor
    public HydraulicNode(XElement el) : base(el)
    {
        // Recreate connectors in the same order as the default constructor
        Inputs.Add(new InConnector("Upstream Flow", "cfs", this));
        Inputs.Add(new InConnector("Lateral Inflow", "cfs", this));
        Outputs.Add(new OutConnector("Downstream Flow", "cfs", this));

        // Parse custom properties
        double.TryParse(el.Attribute(nameof(ManningsN))?.Value,
            NumberStyles.Any, CultureInfo.InvariantCulture, out _manningsN);
    }

    protected override void AddToBaseElement(XElement baseElement)
    {
        baseElement.SetAttributeValue(nameof(ManningsN),
            _manningsN.ToString("G17", CultureInfo.InvariantCulture));
    }

    public override NodeBase Clone()
    {
        var clone = new HydraulicNode
        {
            LeftPosition = LeftPosition,
            TopPosition = TopPosition,
            ManningsN = ManningsN
        };
        // Connectors are created by the default constructor
        return clone;
    }
}
```

**Important:** The deserialization constructor must recreate connectors in the exact same order as the default constructor. Connections are stored by connector index, so the indices must match during deserialization.

## Creating a Custom Graph

Extend `Graph` and implement `ReadNodeRequested` to map XML elements to node types:

```csharp
public class HydraulicNetwork : Graph
{
    public HydraulicNetwork() : base() { }

    public HydraulicNetwork(XElement el) : base(el) { }

    protected override NodeBase ReadNodeRequested(XElement nodeElement)
    {
        string nodeName = nodeElement.Attribute("Name")?.Value ?? "";
        return nodeName switch
        {
            "Hydraulic Reach" => new HydraulicNode(nodeElement),
            "Reservoir" => new ReservoirNode(nodeElement),
            _ => throw new InvalidOperationException($"Unknown node type: {nodeName}")
        };
    }

    protected override void AddToBaseElement(XElement baseElement)
    {
        // Add graph-level custom data if needed
    }
}
```

## Graph Operations

### Cycle Detection

`AddConnection` automatically rejects connections that would create a cycle. You can also check proactively:

```csharp
if (!graph.WouldCreateCycle(outputConnector, inputConnector))
{
    graph.AddConnection(outputConnector, inputConnector);
}
```

### Topological Sort

Returns nodes in dependency order. Returns `null` if the graph contains a cycle.

```csharp
List<NodeBase> sorted = graph.TopologicalSort();
if (sorted != null)
{
    foreach (var node in sorted)
    {
        ProcessNode(node);  // Process in dependency order
    }
}
```

### Path Queries and Traversal

```csharp
// Check if a path exists between two nodes
bool connected = graph.HasPath(nodeA, nodeB);

// Get all nodes that feed into a given node
List<NodeBase> dependencies = graph.GetAncestors(targetNode);

// Get all nodes that depend on a given node
List<NodeBase> dependents = graph.GetDescendants(sourceNode);

// Get entry and exit points
List<NodeBase> roots = graph.GetRootNodes();  // No incoming connections
List<NodeBase> leaves = graph.GetLeafNodes(); // No outgoing connections

// Get the longest dependency chain length
int depth = graph.GetGraphDepth();

// Validate graph integrity
bool isValid = !graph.HasCycle();
```

## FlowGraphCanvas

**Namespace:** `DAGControls`

`FlowGraphCanvas` is a WPF `Canvas` control that provides an interactive visual editor for DAG graphs.

### Dependency Properties

| Property | Type | Description |
|---|---|---|
| `Graph` | `DAG.Graph` | The graph model to display and edit |

### Events

| Event | Signature | Description |
|---|---|---|
| `PreviewCanvasContextMenu` | `(ContextMenu cm, Point canvasPosition)` | Populate the context menu for right-click on empty canvas |
| `PreviewNodeContextMenu` | `(ContextMenu cm, NodeBase node)` | Populate the context menu for right-click on a node |
| `ConnectionAdded` | `(Tuple<OutConnector, InConnector> connection, Path connectionPath)` | A connection was created in the UI |
| `ConnectionRemoved` | `(Tuple<OutConnector, InConnector> connection, Path connectionPath)` | A connection was removed from the UI |
| `NodeMoved` | `(NodeBase node)` | A node was repositioned via drag |
| `NodeSizeChanged` | `(NodeBase node)` | A node's visual size changed |
| `GraphRedrawn` | `()` | The graph was fully redrawn |
| `AutoConnection_Clicked` | `(OutConnector fromConnector)` | The auto-connection button on an output connector was clicked |

### Features

- **Drag-and-drop** node positioning
- **Interactive connection creation** by dragging from output to input connectors
- **Zoom and pan** navigation with custom cursors
- **Context menus** for canvas and node right-click
- **Bezier curve** connection paths
- **Custom visual overlays** for application-specific decorations
- **Automatic synchronization** with the underlying `Graph` model

## NodeControl

`NodeControl` is a `UserControl` that visually represents a single node with its header, connectors, and content area.

### Dependency Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Node` | `NodeBase` | `null` | The underlying node data (read-only) |
| `NodeContent` | `object` | `null` | Custom content displayed inside the node body |
| `HeaderColor` | `SolidColorBrush` | Red | Background color of the node header |
| `NodeIcon` | `ImageSource` | `null` | Icon displayed in the node header |

### Visual Structure

Each `NodeControl` displays:
- A colored header bar with the node name and optional icon
- Input connector ellipses on the left side
- Output connector ellipses on the right side with auto-connection buttons
- A content area for node-specific UI
- A delete button for removing the node

## Serialization

The DAG model serializes to XML using connector-index-based storage. Connections reference nodes by GUID and connectors by their index in the `Inputs` or `Outputs` collection.

```csharp
// Save
XElement xml = graph.ToXElement();
xml.Save("network.xml");

// Load
XElement xml = XElement.Load("network.xml");
var graph = new HydraulicNetwork(xml);
```

The XML structure:

```xml
<HydraulicNetwork Scale="1">
  <Nodes>
    <Node Name="Hydraulic Reach"
          NodeGuid="abc-123"
          LeftPosition="100"
          TopPosition="50"
          ManningsN="0.035" />
  </Nodes>
  <Connections>
    <Connection From_Node="abc-123" From_Connector="0"
                To_Node="def-456" To_Connector="0" />
  </Connections>
</HydraulicNetwork>
```

The `Graph(XElement)` constructor validates each connection during deserialization, rejecting any that would create a cycle. This protects against malformed XML.

## Code Example

```xml
<!-- XAML -->
<Window xmlns:dagControls="clr-namespace:DAGControls;assembly=DAGControls">
    <dagControls:FlowGraphCanvas x:Name="GraphCanvas" />
</Window>
```

```csharp
// Code-behind
public partial class MainWindow : Window
{
    private HydraulicNetwork _graph = new HydraulicNetwork();

    public MainWindow()
    {
        InitializeComponent();

        // Assign the graph to the canvas
        GraphCanvas.Graph = _graph;

        // Add context menu for creating nodes
        GraphCanvas.PreviewCanvasContextMenu += (cm, position) =>
        {
            var addItem = new MenuItem { Header = "Add Hydraulic Reach" };
            addItem.Click += (s, e) =>
            {
                var node = new HydraulicNode
                {
                    LeftPosition = position.X,
                    TopPosition = position.Y
                };
                _graph.Nodes.Add(node);
            };
            cm.Items.Add(addItem);
        };

        // Listen for connection changes
        GraphCanvas.ConnectionAdded += (connection, path) =>
        {
            // Recalculate downstream nodes
            var sorted = _graph.TopologicalSort();
        };
    }
}
```

## Demo Application

The `DAG.Demo` project provides a working example with `TestGraph` and `TestNode` implementations. It demonstrates node creation, connection, context menus, and graph interaction on a `FlowGraphCanvas`. Build and run it from the Demos solution folder.

## Best Practices

- **Recreate connectors in the same order during deserialization** -- connections are stored by connector index. The deserialization constructor must add connectors to `Inputs` and `Outputs` in the exact same order as the default constructor.
- **Check for cycles before adding connections** -- while `AddConnection` enforces cycle prevention automatically, calling `WouldCreateCycle` first allows you to provide user feedback about why a connection was rejected.
- **Use topological sort for execution order** -- when processing nodes (e.g., running a simulation), use `TopologicalSort()` to ensure each node is processed only after all its dependencies.
- **Clone connectors with the correct parent** -- when implementing `Clone()`, ensure new connectors reference the cloned node as their `Parent`, not the original node.
- **Subscribe to `PreviewConnectionsAdded` for validation** -- use the graph-level preview events to implement custom validation rules (e.g., unit compatibility checks) before connections are accepted.
- **Use `GetAncestors`/`GetDescendants` for impact analysis** -- when a node's configuration changes, use these methods to determine which other nodes are affected and need recalculation.
