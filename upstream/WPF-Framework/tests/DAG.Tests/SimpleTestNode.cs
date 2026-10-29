using System.Xml.Linq;
using DAG;

namespace DAG.Tests
{
    /// <summary>
    /// A simple node implementation for unit testing purposes.
    /// </summary>
    public class SimpleTestNode : NodeBase
    {
        private string _name;

        /// <summary>
        /// Gets the name of the node.
        /// </summary>
        public override string Name => _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTestNode"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        public SimpleTestNode(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTestNode"/> class with a default name.
        /// </summary>
        public SimpleTestNode() : this("TestNode")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTestNode"/> class from an XML element.
        /// </summary>
        /// <param name="nodeElement">The XML element containing the node data.</param>
        public SimpleTestNode(XElement nodeElement) : base(nodeElement)
        {
            _name = nodeElement.Attribute(nameof(Name))?.Value ?? "TestNode";
            // Recreate connectors — NodeBase serialization stores connections by index,
            // so the deserialized node must have the same connector layout.
            AddInput("In", "");
            AddOutput("Out", "");
        }

        /// <summary>
        /// Adds an input connector to this node.
        /// </summary>
        /// <param name="name">The name of the input connector.</param>
        /// <param name="unit">The unit of the input connector.</param>
        /// <returns>The created input connector.</returns>
        public InConnector AddInput(string name, string unit = "")
        {
            var connector = new InConnector(name, unit, this);
            Inputs.Add(connector);
            return connector;
        }

        /// <summary>
        /// Adds an output connector to this node.
        /// </summary>
        /// <param name="name">The name of the output connector.</param>
        /// <param name="unit">The unit of the output connector.</param>
        /// <returns>The created output connector.</returns>
        public OutConnector AddOutput(string name, string unit = "")
        {
            var connector = new OutConnector(name, unit, this);
            Outputs.Add(connector);
            return connector;
        }

        /// <summary>
        /// Creates a deep copy of this node.
        /// </summary>
        /// <returns>A new instance that is a copy of this node.</returns>
        public override NodeBase Clone()
        {
            var result = new SimpleTestNode(_name)
            {
                LeftPosition = LeftPosition,
                TopPosition = TopPosition
            };

            foreach (var output in _outputs)
            {
                result.Outputs.Add(new OutConnector(output.Name, output.Unit, result));
            }

            foreach (var input in _inputs)
            {
                result.Inputs.Add(new InConnector(input.Name, input.Unit, result));
            }

            return result;
        }

        /// <summary>
        /// Adds custom data to the XML element during serialization.
        /// </summary>
        /// <param name="baseElement">The base XML element to add data to.</param>
        protected override void AddToBaseElement(XElement baseElement)
        {
            // No additional data to serialize for simple test node
        }
    }
}
