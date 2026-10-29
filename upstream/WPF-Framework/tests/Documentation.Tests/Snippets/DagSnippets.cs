#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value
#pragma warning disable CS0067 // Event is never used

using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using DAG;

namespace Documentation.Tests.Snippets.Dag
{
    // ---------------------------------------------------------------
    // Snippet: Creating Custom Nodes -- HydraulicNode
    // ---------------------------------------------------------------
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

    // Stub for a second node type referenced in the graph snippet
    public class ReservoirNode : NodeBase
    {
        public override string Name => "Reservoir";

        public ReservoirNode()
        {
            Inputs.Add(new InConnector("Inflow", "cfs", this));
            Outputs.Add(new OutConnector("Outflow", "cfs", this));
        }

        public ReservoirNode(XElement el) : base(el)
        {
            Inputs.Add(new InConnector("Inflow", "cfs", this));
            Outputs.Add(new OutConnector("Outflow", "cfs", this));
        }

        protected override void AddToBaseElement(XElement baseElement) { }

        public override NodeBase Clone()
        {
            return new ReservoirNode
            {
                LeftPosition = LeftPosition,
                TopPosition = TopPosition
            };
        }
    }

    // ---------------------------------------------------------------
    // Snippet: Creating a Custom Graph -- HydraulicNetwork
    // ---------------------------------------------------------------
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

    /// <summary>
    /// Validates that all C# code snippets in docs/dag-controls.md compile correctly.
    /// </summary>
    public class DagSnippets
    {
        // ---------------------------------------------------------------
        // Snippet: Cycle Detection
        // ---------------------------------------------------------------
        public void Snippet_CycleDetection()
        {
            var graph = new HydraulicNetwork();
            var node1 = new HydraulicNode();
            var node2 = new HydraulicNode();
            graph.Nodes.Add(node1);
            graph.Nodes.Add(node2);

            OutConnector outputConnector = node1.Outputs[0];
            InConnector inputConnector = node2.Inputs[0];

            if (!graph.WouldCreateCycle(outputConnector, inputConnector))
            {
                graph.AddConnection(outputConnector, inputConnector);
            }
        }

        // ---------------------------------------------------------------
        // Snippet: Topological Sort
        // ---------------------------------------------------------------
        public void Snippet_TopologicalSort()
        {
            var graph = new HydraulicNetwork();

            List<NodeBase> sorted = graph.TopologicalSort();
            if (sorted != null)
            {
                foreach (var node in sorted)
                {
                    ProcessNode(node);  // Process in dependency order
                }
            }
        }

        private void ProcessNode(NodeBase node) { }

        // ---------------------------------------------------------------
        // Snippet: Path Queries and Traversal
        // ---------------------------------------------------------------
        public void Snippet_PathQueries()
        {
            var graph = new HydraulicNetwork();
            var nodeA = new HydraulicNode();
            var nodeB = new HydraulicNode();
            var targetNode = new HydraulicNode();
            var sourceNode = new HydraulicNode();
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(targetNode);
            graph.Nodes.Add(sourceNode);

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
        }

        // ---------------------------------------------------------------
        // Snippet: Serialization -- Save and Load
        // ---------------------------------------------------------------
        public void Snippet_Serialization()
        {
            var graph = new HydraulicNetwork();

            // Save
            XElement xml = graph.ToXElement();
            // xml.Save("network.xml");  -- skipped, requires filesystem

            // Load
            // XElement loadedXml = XElement.Load("network.xml");
            // var loadedGraph = new HydraulicNetwork(loadedXml);

            // Verify deserialization constructor compiles:
            var testElement = new XElement("HydraulicNetwork",
                new XAttribute("Scale", "1"),
                new XElement("Nodes"),
                new XElement("Connections"));
            var loadedGraph = new HydraulicNetwork(testElement);
        }

        // ---------------------------------------------------------------
        // Snippet: Code-behind example with FlowGraphCanvas
        // ---------------------------------------------------------------
        public void Snippet_CodeBehind()
        {
            var _graph = new HydraulicNetwork();
            var graphCanvas = new DAGControls.FlowGraphCanvas();

            // Assign the graph to the canvas
            graphCanvas.Graph = _graph;

            // Add context menu for creating nodes
            graphCanvas.PreviewCanvasContextMenu += (cm, position) =>
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
            graphCanvas.ConnectionAdded += (connection, path) =>
            {
                // Recalculate downstream nodes
                var sorted = _graph.TopologicalSort();
            };
        }
    }
}
