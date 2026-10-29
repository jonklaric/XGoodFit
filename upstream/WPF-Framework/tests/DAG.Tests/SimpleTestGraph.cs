using System.Xml.Linq;
using DAG;

namespace DAG.Tests
{
    /// <summary>
    /// A simple graph implementation for unit testing purposes.
    /// </summary>
    public class SimpleTestGraph : Graph
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTestGraph"/> class.
        /// </summary>
        public SimpleTestGraph() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleTestGraph"/> class from an XML element.
        /// </summary>
        /// <param name="el">The XML element containing the graph data.</param>
        public SimpleTestGraph(XElement el) : base(el)
        {
        }

        /// <summary>
        /// Adds custom data to the XML element during serialization.
        /// </summary>
        /// <param name="baseElement">The base XML element to add data to.</param>
        protected override void AddToBaseElement(XElement baseElement)
        {
            // No additional data to serialize for simple test graph
        }

        /// <summary>
        /// Creates a node from an XML element during deserialization.
        /// </summary>
        /// <param name="nodeElement">The XML element containing the node data.</param>
        /// <returns>A new node instance created from the XML data.</returns>
        protected override NodeBase ReadNodeRequested(XElement nodeElement)
        {
            return new SimpleTestNode(nodeElement);
        }
    }
}
