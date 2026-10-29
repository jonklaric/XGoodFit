using System.Globalization;
using System.Threading;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DAG.Tests
{
    /// <summary>
    /// Verifies that graph serialization survives a round-trip under non-US cultures.
    /// </summary>
    /// <remarks>
    /// On a German machine (decimal comma), naive ToString/Parse without InvariantCulture
    /// would write "1,5" instead of "1.5", breaking interchange across machines.
    /// </remarks>
    [TestClass]
    public class CultureRoundTripTests
    {
        // Each value listed here has a decimal point in its G17 mantissa, so under bare
        // (non-Invariant) ToString on de-DE it would serialize with a comma separator and
        // fail to parse on read. -0.0 is included for sign preservation and is documented
        // as a serialization-completeness case (the "-0" string is culture-independent).
        private static readonly double[] TrickyValues =
        {
            1234.5678,
            -987.654321,
            1.23456789e-10,
            1.23456789e+15,
            0.1,
            -0.0,
            double.Epsilon,
            double.MaxValue,
            double.MinValue,
            1e-300,
            -1e+250,
        };

        [TestMethod]
        public void Graph_RoundTrip_PreservesNodePositionsBitExact_UnderGermanCulture()
        {
            RunUnderCulture("de-DE", () =>
            {
                var graph = new SimpleTestGraph { Scale = 1.234567890123456 };
                for (int i = 0; i < TrickyValues.Length; i++)
                {
                    var node = new SimpleTestNode($"Node{i}")
                    {
                        LeftPosition = TrickyValues[i],
                        TopPosition = TrickyValues[(i + 1) % TrickyValues.Length],
                    };
                    node.AddInput("In");
                    node.AddOutput("Out");
                    graph.Nodes.Add(node);
                }

                XElement serialized = graph.ToXElement();
                var loaded = new SimpleTestGraph(serialized);

                AssertBitExact(graph.Scale, loaded.Scale, "Scale");
                Assert.AreEqual(graph.Nodes.Count, loaded.Nodes.Count);
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    AssertBitExact(graph.Nodes[i].LeftPosition, loaded.Nodes[i].LeftPosition, $"Node{i}.LeftPosition");
                    AssertBitExact(graph.Nodes[i].TopPosition, loaded.Nodes[i].TopPosition, $"Node{i}.TopPosition");
                }
            });
        }

        [TestMethod]
        public void Graph_SerializedXml_UsesPeriodAsDecimalSeparator_UnderGermanCulture()
        {
            RunUnderCulture("de-DE", () =>
            {
                var graph = new SimpleTestGraph { Scale = 1.5 };
                var node = new SimpleTestNode("N") { LeftPosition = 2.5, TopPosition = 3.75 };
                node.AddInput("In");
                node.AddOutput("Out");
                graph.Nodes.Add(node);

                XElement serialized = graph.ToXElement();
                string xml = serialized.ToString();

                // The serialized XML must use '.' (Invariant), not ',' (German), for decimals.
                Assert.IsTrue(xml.Contains("1.5"), $"Scale '1.5' missing from XML:\n{xml}");
                Assert.IsTrue(xml.Contains("2.5"), $"LeftPosition '2.5' missing from XML:\n{xml}");
                Assert.IsTrue(xml.Contains("3.75"), $"TopPosition '3.75' missing from XML:\n{xml}");
            });
        }

        /// <summary>
        /// Verifies that the sign bit of negative zero survives round-trip.
        /// </summary>
        /// <remarks>
        /// NOT a culture regression test — "-0" is culture-independent in G17 form. This
        /// is a serialization-completeness test that documents sign preservation.
        /// </remarks>
        [TestMethod]
        public void Graph_RoundTrip_PreservesNegativeZeroSign()
        {
            RunUnderCulture("en-US", () =>
            {
                var graph = new SimpleTestGraph();
                var node = new SimpleTestNode("N") { LeftPosition = -0.0, TopPosition = 0.0 };
                node.AddInput("In");
                node.AddOutput("Out");
                graph.Nodes.Add(node);

                XElement serialized = graph.ToXElement();
                var loaded = new SimpleTestGraph(serialized);

                long expectedNegZeroBits = System.BitConverter.DoubleToInt64Bits(-0.0);
                long expectedPosZeroBits = System.BitConverter.DoubleToInt64Bits(0.0);
                long loadedLeftBits = System.BitConverter.DoubleToInt64Bits(loaded.Nodes[0].LeftPosition);
                long loadedTopBits = System.BitConverter.DoubleToInt64Bits(loaded.Nodes[0].TopPosition);

                Assert.AreEqual(expectedNegZeroBits, loadedLeftBits,
                    $"-0.0 sign bit lost: expected bits {expectedNegZeroBits}, got {loadedLeftBits}");
                Assert.AreEqual(expectedPosZeroBits, loadedTopBits,
                    $"+0.0 changed: expected bits {expectedPosZeroBits}, got {loadedTopBits}");
                Assert.AreNotEqual(loadedLeftBits, loadedTopBits,
                    "Negative zero and positive zero collapsed to the same bit pattern");
            });
        }

        /// <summary>
        /// Verifies that NaN, +Infinity, and -Infinity survive round-trip.
        /// </summary>
        /// <remarks>
        /// NaN portion is serialization-completeness (the "NaN" string is culture-independent).
        /// Infinity portion IS a genuine culture regression test: G17 + InvariantCulture
        /// produces "Infinity"; under bare ToString on de-DE, it produces "∞", which the
        /// Invariant reader rejects. Reverting the fix would break this test.
        /// </remarks>
        [TestMethod]
        public void Graph_RoundTrip_PreservesNaNAndInfinity_UnderGermanCulture()
        {
            RunUnderCulture("de-DE", () =>
            {
                var graph = new SimpleTestGraph();
                var nanNode = new SimpleTestNode("NaN") { LeftPosition = double.NaN, TopPosition = double.NaN };
                nanNode.AddInput("In"); nanNode.AddOutput("Out");
                graph.Nodes.Add(nanNode);

                var posInfNode = new SimpleTestNode("PosInf") { LeftPosition = double.PositiveInfinity, TopPosition = double.PositiveInfinity };
                posInfNode.AddInput("In"); posInfNode.AddOutput("Out");
                graph.Nodes.Add(posInfNode);

                var negInfNode = new SimpleTestNode("NegInf") { LeftPosition = double.NegativeInfinity, TopPosition = double.NegativeInfinity };
                negInfNode.AddInput("In"); negInfNode.AddOutput("Out");
                graph.Nodes.Add(negInfNode);

                XElement serialized = graph.ToXElement();
                var loaded = new SimpleTestGraph(serialized);

                Assert.IsTrue(double.IsNaN(loaded.Nodes[0].LeftPosition), "NaN.LeftPosition lost");
                Assert.IsTrue(double.IsNaN(loaded.Nodes[0].TopPosition), "NaN.TopPosition lost");
                Assert.IsTrue(double.IsPositiveInfinity(loaded.Nodes[1].LeftPosition), "+Inf.LeftPosition lost");
                Assert.IsTrue(double.IsPositiveInfinity(loaded.Nodes[1].TopPosition), "+Inf.TopPosition lost");
                Assert.IsTrue(double.IsNegativeInfinity(loaded.Nodes[2].LeftPosition), "-Inf.LeftPosition lost");
                Assert.IsTrue(double.IsNegativeInfinity(loaded.Nodes[2].TopPosition), "-Inf.TopPosition lost");
            });
        }

        [TestMethod]
        public void Graph_RoundTrip_LoadsUsCultureFileUnderGermanCulture()
        {
            // Build under en-US, serialize, then deserialize under de-DE — the file format
            // must be culture-neutral or non-US users would fail to load files from US users.
            XElement serialized = null;
            var sourceGraph = new SimpleTestGraph();
            RunUnderCulture("en-US", () =>
            {
                sourceGraph.Scale = 12345.6789;
                var node = new SimpleTestNode("N") { LeftPosition = 100.5, TopPosition = -200.25 };
                node.AddInput("In");
                node.AddOutput("Out");
                sourceGraph.Nodes.Add(node);
                serialized = sourceGraph.ToXElement();
            });

            RunUnderCulture("de-DE", () =>
            {
                var loaded = new SimpleTestGraph(serialized);
                AssertBitExact(sourceGraph.Scale, loaded.Scale, "Scale");
                AssertBitExact(sourceGraph.Nodes[0].LeftPosition, loaded.Nodes[0].LeftPosition, "LeftPosition");
                AssertBitExact(sourceGraph.Nodes[0].TopPosition, loaded.Nodes[0].TopPosition, "TopPosition");
            });
        }

        private static void RunUnderCulture(string cultureName, System.Action body)
        {
            var prevCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                var culture = new CultureInfo(cultureName);
                Thread.CurrentThread.CurrentCulture = culture;
                CultureInfo.CurrentCulture = culture;
                body();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = prevCulture;
                CultureInfo.CurrentCulture = prevCulture;
            }
        }

        private static void AssertBitExact(double expected, double actual, string label)
        {
            long expectedBits = System.BitConverter.DoubleToInt64Bits(expected);
            long actualBits = System.BitConverter.DoubleToInt64Bits(actual);
            Assert.AreEqual(expectedBits, actualBits,
                $"{label}: expected {expected:R} (bits {expectedBits}), got {actual:R} (bits {actualBits})");
        }
    }
}
