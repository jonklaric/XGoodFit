using System;
using System.Collections.Generic;
using System.Linq;
using DAG;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DAG.Tests
{
    /// <summary>
    /// Unit tests for the Graph class and DAG operations.
    /// </summary>
    [TestClass]
    public class GraphTests
    {
        #region Setup

        private SimpleTestGraph CreateTestGraph()
        {
            return new SimpleTestGraph();
        }

        private SimpleTestNode CreateNode(string name)
        {
            var node = new SimpleTestNode(name);
            node.AddInput("In", "");
            node.AddOutput("Out", "");
            return node;
        }

        #endregion

        #region Basic Graph Operations Tests

        [TestMethod]
        public void Graph_AddNode_NodeIsAdded()
        {
            // Arrange
            var graph = CreateTestGraph();
            var node = CreateNode("A");

            // Act
            graph.Nodes.Add(node);

            // Assert
            Assert.AreEqual(1, graph.Nodes.Count);
            Assert.IsTrue(graph.Nodes.Contains(node));
        }

        [TestMethod]
        public void Graph_RemoveNode_NodeIsRemoved()
        {
            // Arrange
            var graph = CreateTestGraph();
            var node = CreateNode("A");
            graph.Nodes.Add(node);

            // Act
            graph.Nodes.Remove(node);

            // Assert
            Assert.AreEqual(0, graph.Nodes.Count);
        }

        [TestMethod]
        public void Graph_AddConnection_ConnectionIsAdded()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);

            // Act
            bool result = graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, graph.Connections.Count);
        }

        [TestMethod]
        public void Graph_RemoveConnection_ConnectionIsRemoved()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);

            // Act
            bool result = graph.RemoveConnection(nodeA.Outputs[0], nodeB.Inputs[0]);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, graph.Connections.Count);
        }

        [TestMethod]
        public void Graph_RemoveNode_RemovesAssociatedConnections()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            graph.Nodes.Remove(nodeB);

            // Assert
            Assert.AreEqual(0, graph.Connections.Count);
        }

        #endregion

        #region Cycle Detection Tests

        [TestMethod]
        public void WouldCreateCycle_SelfLoop_ReturnsTrue()
        {
            // Arrange
            var graph = CreateTestGraph();
            var node = new SimpleTestNode("A");
            node.AddInput("In", "");
            node.AddOutput("Out", "");
            graph.Nodes.Add(node);

            // Act
            bool result = graph.WouldCreateCycle(node.Outputs[0], node.Inputs[0]);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void WouldCreateCycle_SimpleConnection_ReturnsFalse()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);

            // Act
            bool result = graph.WouldCreateCycle(nodeA.Outputs[0], nodeB.Inputs[0]);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void WouldCreateCycle_WouldCreateCycle_ReturnsTrue()
        {
            // Arrange: A -> B -> C, now try to add C -> A
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            bool result = graph.WouldCreateCycle(nodeC.Outputs[0], nodeA.Inputs[0]);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void AddConnection_WouldCreateCycle_ConnectionNotAdded()
        {
            // Arrange: A -> B -> C, now try to add C -> A
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            bool result = graph.AddConnection(nodeC.Outputs[0], nodeA.Inputs[0]);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(2, graph.Connections.Count);
        }

        [TestMethod]
        public void HasCycle_NoCycle_ReturnsFalse()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            bool result = graph.HasCycle();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void HasCycle_EmptyGraph_ReturnsFalse()
        {
            // Arrange
            var graph = CreateTestGraph();

            // Act
            bool result = graph.HasCycle();

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Path Finding Tests

        [TestMethod]
        public void HasPath_DirectConnection_ReturnsTrue()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);

            // Act
            bool result = graph.HasPath(nodeA, nodeB);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasPath_IndirectConnection_ReturnsTrue()
        {
            // Arrange: A -> B -> C
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            bool result = graph.HasPath(nodeA, nodeC);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasPath_NoConnection_ReturnsFalse()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);

            // Act
            bool result = graph.HasPath(nodeA, nodeB);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void HasPath_ReverseDirection_ReturnsFalse()
        {
            // Arrange: A -> B
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);

            // Act
            bool result = graph.HasPath(nodeB, nodeA);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void HasPath_SameNode_ReturnsTrue()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            graph.Nodes.Add(nodeA);

            // Act
            bool result = graph.HasPath(nodeA, nodeA);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region Root and Leaf Node Tests

        [TestMethod]
        public void GetRootNodes_LinearGraph_ReturnsFirstNode()
        {
            // Arrange: A -> B -> C
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            var roots = graph.GetRootNodes();

            // Assert
            Assert.AreEqual(1, roots.Count);
            Assert.IsTrue(roots.Contains(nodeA));
        }

        [TestMethod]
        public void GetRootNodes_MultipleRoots_ReturnsAllRoots()
        {
            // Arrange: A -> C, B -> C (A and B are roots)
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = new SimpleTestNode("C");
            nodeC.AddInput("In1", "");
            nodeC.AddInput("In2", "");
            nodeC.AddOutput("Out", "");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeC.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[1]);

            // Act
            var roots = graph.GetRootNodes();

            // Assert
            Assert.AreEqual(2, roots.Count);
            Assert.IsTrue(roots.Contains(nodeA));
            Assert.IsTrue(roots.Contains(nodeB));
        }

        [TestMethod]
        public void GetLeafNodes_LinearGraph_ReturnsLastNode()
        {
            // Arrange: A -> B -> C
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            var leaves = graph.GetLeafNodes();

            // Assert
            Assert.AreEqual(1, leaves.Count);
            Assert.IsTrue(leaves.Contains(nodeC));
        }

        [TestMethod]
        public void GetLeafNodes_MultipleLeaves_ReturnsAllLeaves()
        {
            // Arrange: A -> B, A -> C (B and C are leaves)
            var graph = CreateTestGraph();
            var nodeA = new SimpleTestNode("A");
            nodeA.AddInput("In", "");
            nodeA.AddOutput("Out1", "");
            nodeA.AddOutput("Out2", "");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeA.Outputs[1], nodeC.Inputs[0]);

            // Act
            var leaves = graph.GetLeafNodes();

            // Assert
            Assert.AreEqual(2, leaves.Count);
            Assert.IsTrue(leaves.Contains(nodeB));
            Assert.IsTrue(leaves.Contains(nodeC));
        }

        [TestMethod]
        public void GetRootNodes_DisconnectedGraph_ReturnsAllNodes()
        {
            // Arrange: A, B, C (no connections)
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);

            // Act
            var roots = graph.GetRootNodes();

            // Assert
            Assert.AreEqual(3, roots.Count);
        }

        #endregion

        #region Ancestor and Descendant Tests

        [TestMethod]
        public void GetAncestors_LinearGraph_ReturnsAllPredecessors()
        {
            // Arrange: A -> B -> C
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            var ancestors = graph.GetAncestors(nodeC);

            // Assert
            Assert.AreEqual(2, ancestors.Count);
            Assert.IsTrue(ancestors.Contains(nodeA));
            Assert.IsTrue(ancestors.Contains(nodeB));
        }

        [TestMethod]
        public void GetAncestors_RootNode_ReturnsEmpty()
        {
            // Arrange: A -> B -> C
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);

            // Act
            var ancestors = graph.GetAncestors(nodeA);

            // Assert
            Assert.AreEqual(0, ancestors.Count);
        }

        [TestMethod]
        public void GetDescendants_LinearGraph_ReturnsAllSuccessors()
        {
            // Arrange: A -> B -> C
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            var descendants = graph.GetDescendants(nodeA);

            // Assert
            Assert.AreEqual(2, descendants.Count);
            Assert.IsTrue(descendants.Contains(nodeB));
            Assert.IsTrue(descendants.Contains(nodeC));
        }

        [TestMethod]
        public void GetDescendants_LeafNode_ReturnsEmpty()
        {
            // Arrange: A -> B -> C
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);

            // Act
            var descendants = graph.GetDescendants(nodeB);

            // Assert
            Assert.AreEqual(0, descendants.Count);
        }

        #endregion

        #region Topological Sort Tests

        [TestMethod]
        public void TopologicalSort_LinearGraph_ReturnsCorrectOrder()
        {
            // Arrange: A -> B -> C
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            var sorted = graph.TopologicalSort();

            // Assert
            Assert.IsNotNull(sorted);
            Assert.AreEqual(3, sorted.Count);
            Assert.IsTrue(sorted.IndexOf(nodeA) < sorted.IndexOf(nodeB));
            Assert.IsTrue(sorted.IndexOf(nodeB) < sorted.IndexOf(nodeC));
        }

        [TestMethod]
        public void TopologicalSort_DiamondGraph_ReturnsValidOrder()
        {
            // Arrange: A -> B -> D, A -> C -> D
            var graph = CreateTestGraph();
            var nodeA = new SimpleTestNode("A");
            nodeA.AddOutput("Out1", "");
            nodeA.AddOutput("Out2", "");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            var nodeD = new SimpleTestNode("D");
            nodeD.AddInput("In1", "");
            nodeD.AddInput("In2", "");

            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.Nodes.Add(nodeD);

            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeA.Outputs[1], nodeC.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeD.Inputs[0]);
            graph.AddConnection(nodeC.Outputs[0], nodeD.Inputs[1]);

            // Act
            var sorted = graph.TopologicalSort();

            // Assert
            Assert.IsNotNull(sorted);
            Assert.AreEqual(4, sorted.Count);
            Assert.IsTrue(sorted.IndexOf(nodeA) < sorted.IndexOf(nodeB));
            Assert.IsTrue(sorted.IndexOf(nodeA) < sorted.IndexOf(nodeC));
            Assert.IsTrue(sorted.IndexOf(nodeB) < sorted.IndexOf(nodeD));
            Assert.IsTrue(sorted.IndexOf(nodeC) < sorted.IndexOf(nodeD));
        }

        [TestMethod]
        public void TopologicalSort_EmptyGraph_ReturnsEmpty()
        {
            // Arrange
            var graph = CreateTestGraph();

            // Act
            var sorted = graph.TopologicalSort();

            // Assert
            Assert.IsNotNull(sorted);
            Assert.AreEqual(0, sorted.Count);
        }

        [TestMethod]
        public void TopologicalSort_DisconnectedNodes_ReturnsAllNodes()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);

            // Act
            var sorted = graph.TopologicalSort();

            // Assert
            Assert.IsNotNull(sorted);
            Assert.AreEqual(3, sorted.Count);
        }

        #endregion

        #region Graph Depth Tests

        [TestMethod]
        public void GetGraphDepth_LinearGraph_ReturnsCorrectDepth()
        {
            // Arrange: A -> B -> C (depth = 2)
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            int depth = graph.GetGraphDepth();

            // Assert
            Assert.AreEqual(2, depth);
        }

        [TestMethod]
        public void GetGraphDepth_SingleNode_ReturnsZero()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            graph.Nodes.Add(nodeA);

            // Act
            int depth = graph.GetGraphDepth();

            // Assert
            Assert.AreEqual(0, depth);
        }

        [TestMethod]
        public void GetGraphDepth_EmptyGraph_ReturnsZero()
        {
            // Arrange
            var graph = CreateTestGraph();

            // Act
            int depth = graph.GetGraphDepth();

            // Assert
            Assert.AreEqual(0, depth);
        }

        [TestMethod]
        public void GetGraphDepth_DiamondGraph_ReturnsLongestPath()
        {
            // Arrange: A -> B -> D, A -> C -> D (depth = 2)
            var graph = CreateTestGraph();
            var nodeA = new SimpleTestNode("A");
            nodeA.AddOutput("Out1", "");
            nodeA.AddOutput("Out2", "");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            var nodeD = new SimpleTestNode("D");
            nodeD.AddInput("In1", "");
            nodeD.AddInput("In2", "");

            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.Nodes.Add(nodeD);

            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeA.Outputs[1], nodeC.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeD.Inputs[0]);
            graph.AddConnection(nodeC.Outputs[0], nodeD.Inputs[1]);

            // Act
            int depth = graph.GetGraphDepth();

            // Assert
            Assert.AreEqual(2, depth);
        }

        #endregion

        #region Serialization Tests

        [TestMethod]
        public void ToXElement_SerializesGraphCorrectly()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);

            // Act
            var xml = graph.ToXElement();

            // Assert
            Assert.IsNotNull(xml);
            Assert.AreEqual("SimpleTestGraph", xml.Name.LocalName);
            Assert.IsNotNull(xml.Element("Nodes"));
            Assert.IsNotNull(xml.Element("Connections"));
            Assert.AreEqual(2, xml.Element("Nodes").Elements("Node").Count());
            Assert.AreEqual(1, xml.Element("Connections").Elements("Connection").Count());
        }

        [TestMethod]
        public void Graph_DeserializesFromXElement()
        {
            // Arrange
            var originalGraph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            originalGraph.Nodes.Add(nodeA);
            originalGraph.Nodes.Add(nodeB);
            originalGraph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            var xml = originalGraph.ToXElement();

            // Act
            var loadedGraph = new SimpleTestGraph(xml);

            // Assert
            Assert.AreEqual(2, loadedGraph.Nodes.Count);
            Assert.AreEqual(1, loadedGraph.Connections.Count);
        }

        [TestMethod]
        public void Graph_SerializationRoundTrip_PreservesNodeNames()
        {
            // Arrange
            var originalGraph = CreateTestGraph();
            var nodeA = new SimpleTestNode("CustomNodeNameA");
            nodeA.AddInput("In", "");
            nodeA.AddOutput("Out", "");
            var nodeB = new SimpleTestNode("CustomNodeNameB");
            nodeB.AddInput("In", "");
            nodeB.AddOutput("Out", "");
            originalGraph.Nodes.Add(nodeA);
            originalGraph.Nodes.Add(nodeB);
            originalGraph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);

            // Act
            var xml = originalGraph.ToXElement();
            var loadedGraph = new SimpleTestGraph(xml);

            // Assert
            Assert.AreEqual(2, loadedGraph.Nodes.Count);
            Assert.IsTrue(loadedGraph.Nodes.Any(n => n.Name == "CustomNodeNameA"));
            Assert.IsTrue(loadedGraph.Nodes.Any(n => n.Name == "CustomNodeNameB"));
        }

        [TestMethod]
        public void Graph_DeserializationRejectsCycles()
        {
            // Arrange - Create XML with a cycle manually
            // This simulates a malformed XML file that contains a cycle
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Get clean XML
            var xml = graph.ToXElement();

            // Manually add a cyclic connection (C -> A) to the XML
            var connectionsElement = xml.Element("Connections");
            var cyclicConnection = new System.Xml.Linq.XElement("Connection");
            cyclicConnection.SetAttributeValue("From_Node", nodeC.NodeGuid.ToString());
            cyclicConnection.SetAttributeValue("From_Connector", "0");
            cyclicConnection.SetAttributeValue("To_Node", nodeA.NodeGuid.ToString());
            cyclicConnection.SetAttributeValue("To_Connector", "0");
            connectionsElement.Add(cyclicConnection);

            // Act
            var loadedGraph = new SimpleTestGraph(xml);

            // Assert - The cyclic connection should have been rejected
            Assert.AreEqual(3, loadedGraph.Nodes.Count);
            Assert.AreEqual(2, loadedGraph.Connections.Count); // Only 2 valid connections, not 3
            Assert.IsFalse(loadedGraph.HasCycle());
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void WouldCreateCycle_NullConnector_ReturnsFalse()
        {
            // Arrange
            var graph = CreateTestGraph();

            // Act
            bool result = graph.WouldCreateCycle(null, null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void HasPath_NullNode_ReturnsFalse()
        {
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            graph.Nodes.Add(nodeA);

            // Act
            bool result = graph.HasPath(null, nodeA);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetAncestors_NullNode_ReturnsEmpty()
        {
            // Arrange
            var graph = CreateTestGraph();

            // Act
            var ancestors = graph.GetAncestors(null);

            // Assert
            Assert.AreEqual(0, ancestors.Count);
        }

        [TestMethod]
        public void GetDescendants_NullNode_ReturnsEmpty()
        {
            // Arrange
            var graph = CreateTestGraph();

            // Act
            var descendants = graph.GetDescendants(null);

            // Assert
            Assert.AreEqual(0, descendants.Count);
        }

        #endregion

        #region Regression Tests

        [TestMethod]
        public void AddConnection_InConnectorAlreadyConnected_SecondConnectionRejected()
        {
            // DAG-20: Each InConnector should accept at most one connection.
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);

            // Act
            bool firstResult = graph.AddConnection(nodeA.Outputs[0], nodeC.Inputs[0]);
            bool secondResult = graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Assert
            Assert.IsTrue(firstResult);
            Assert.IsFalse(secondResult);
            Assert.AreEqual(1, graph.Connections.Count);
        }

        [TestMethod]
        public void NodesClear_RemovesAllNodesAndUnsubscribes()
        {
            // DAG-3: Nodes.Clear() should unsubscribe event handlers and not crash.
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            graph.Nodes.Clear();

            // Assert
            Assert.AreEqual(0, graph.Nodes.Count);
            // After clear, adding a new node and manipulating it should not crash
            var nodeD = CreateNode("D");
            graph.Nodes.Add(nodeD);
            Assert.AreEqual(1, graph.Nodes.Count);
        }

        [TestMethod]
        public void RemoveSourceNode_ConnectionsAlsoRemoved()
        {
            // DAG-4: Removing a node must force-remove all its connections.
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            Assert.AreEqual(1, graph.Connections.Count);

            // Act - remove the source node
            graph.Nodes.Remove(nodeA);

            // Assert
            Assert.AreEqual(1, graph.Nodes.Count);
            Assert.AreEqual(0, graph.Connections.Count);
        }

        [TestMethod]
        public void SerializationRoundTrip_PreservesTopology()
        {
            // DAG-9/DAG-28: Round-trip serialization must preserve all nodes and connections.
            // Arrange: A -> B -> C
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            var xml = graph.ToXElement();
            var restored = new SimpleTestGraph(xml);

            // Assert
            Assert.AreEqual(3, restored.Nodes.Count);
            Assert.AreEqual(2, restored.Connections.Count);

            // Verify topology: the source of the first connection feeds the destination of the second
            var sortedRestored = restored.TopologicalSort();
            Assert.IsNotNull(sortedRestored);
            Assert.AreEqual(3, sortedRestored.Count);
            // Root should be "A", leaf should be "C"
            Assert.AreEqual("A", sortedRestored[0].Name);
            Assert.AreEqual("B", sortedRestored[1].Name);
            Assert.AreEqual("C", sortedRestored[2].Name);
        }

        [TestMethod]
        public void Deserialization_MalformedGuid_AssignsNewGuid()
        {
            // DAG-8: A node with a malformed GUID should not crash; it gets a new GUID.
            // Arrange
            var nodeElement = new System.Xml.Linq.XElement("Node");
            nodeElement.SetAttributeValue("Name", "BadGuidNode");
            nodeElement.SetAttributeValue("NodeGuid", "not-a-guid");
            nodeElement.SetAttributeValue("LeftPosition", "100");
            nodeElement.SetAttributeValue("TopPosition", "200");

            // Act
            var node = new SimpleTestNode(nodeElement);

            // Assert
            Assert.IsNotNull(node);
            Assert.AreNotEqual(Guid.Empty, node.NodeGuid);
            Assert.AreEqual("BadGuidNode", node.Name);
        }

        [TestMethod]
        public void GetGraphDepth_MixedConnectedAndIsolatedNodes_ReturnsChainDepth()
        {
            // DAG-27: Isolated nodes should not affect the depth of the longest chain.
            // Arrange: A -> B -> C (depth 2) and isolated node D
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            var nodeD = CreateNode("D");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.Nodes.Add(nodeD);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            int depth = graph.GetGraphDepth();

            // Assert
            Assert.AreEqual(2, depth);
        }

        [TestMethod]
        public void ValidateIntegrity_CleanGraph_ReturnsNoErrors()
        {
            // DAG-5: A properly constructed graph should pass integrity validation.
            // Arrange
            var graph = CreateTestGraph();
            var nodeA = CreateNode("A");
            var nodeB = CreateNode("B");
            var nodeC = CreateNode("C");
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);
            graph.AddConnection(nodeA.Outputs[0], nodeB.Inputs[0]);
            graph.AddConnection(nodeB.Outputs[0], nodeC.Inputs[0]);

            // Act
            var errors = graph.ValidateIntegrity();

            // Assert
            Assert.AreEqual(0, errors.Count);
        }

        #endregion
    }
}
