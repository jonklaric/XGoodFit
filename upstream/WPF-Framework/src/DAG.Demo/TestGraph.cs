using DAG;
using System.Xml.Linq;

namespace DAG.Demo
{
    /// <summary>
    /// A test implementation of the Graph class for demonstration purposes.
    /// </summary>
    public class TestGraph : Graph
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestGraph"/> class.
        /// </summary>
        public TestGraph() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestGraph"/> class from an XML element.
        /// </summary>
        /// <param name="el">The XML element containing the serialized graph data.</param>
        public TestGraph(XElement el) : base(el)
        {
        }

        /// <summary>
        /// Adds custom data to the XML element during serialization.
        /// </summary>
        /// <param name="baseElement">The base XML element to add data to.</param>
        protected override void AddToBaseElement(XElement baseElement)
        {
            // No additional data to serialize for TestGraph
        }

        /// <summary>
        /// Creates a node from an XML element during deserialization.
        /// </summary>
        /// <param name="nodeElement">The XML element containing the node data.</param>
        /// <returns>A new TestNode instance created from the XML data.</returns>
        protected override NodeBase ReadNodeRequested(XElement nodeElement)
        {
            return new TestNode(nodeElement);
        }
    }
}
