using DAG;
using System;
using System.Globalization;
using System.Xml.Linq;

namespace DAG.Demo
{
    /// <summary>
    /// A test implementation of the NodeBase class for demonstration purposes.
    /// Creates nodes with random inputs and outputs.
    /// </summary>
    public class TestNode : NodeBase
    {
        private int _mySpecialNumber = 1;

        /// <summary>
        /// Gets the display name of the node.
        /// </summary>
        public override string Name => "Sample Node";

        /// <summary>
        /// Gets or sets a sample integer property for demonstration.
        /// </summary>
        public int MySpecialNumber
        {
            get => _mySpecialNumber;
            set => Utilities.SetInteger(value, ref _mySpecialNumber, null, this, RaisePropertyChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestNode"/> class from an XML element.
        /// </summary>
        /// <param name="nodeElement">The XML element containing the node data.</param>
        public TestNode(XElement nodeElement) : base(nodeElement)
        {
            Init();
            // Clear the random connectors created by Init() before XML deserialization adds its own
            _inputs.Clear();
            _outputs.Clear();
            int.TryParse(nodeElement.Attribute(nameof(MySpecialNumber))?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out _mySpecialNumber);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestNode"/> class with random inputs and outputs.
        /// </summary>
        public TestNode() : base()
        {
            Init();
        }

        /// <summary>
        /// Initializes the node with random inputs and outputs.
        /// </summary>
        private void Init()
        {
            Random randy = new Random();
            for (int i = 0; i < randy.Next(2, 8); i++)
            {
                _inputs.Add(new InConnector($"Input {i + 1}", i.ToString(), this));
            }

            for (int i = 0; i < randy.Next(1, 5); i++)
            {
                _outputs.Add(new OutConnector($"Output {i + 1}", i.ToString(), this));
            }
        }

        /// <summary>
        /// Adds custom data to the XML element during serialization.
        /// </summary>
        /// <param name="baseElement">The base XML element to add data to.</param>
        protected override void AddToBaseElement(XElement baseElement)
        {
            baseElement.SetAttributeValue(nameof(MySpecialNumber), _mySpecialNumber.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Creates a deep copy of this node.
        /// </summary>
        /// <returns>A new TestNode instance that is a copy of this node.</returns>
        public override NodeBase Clone()
        {
            TestNode result = new TestNode
            {
                LeftPosition = LeftPosition,
                TopPosition = TopPosition,
                MySpecialNumber = MySpecialNumber
            };

            // Clear the random connectors created by Init() and copy from source
            result.Inputs.Clear();
            result.Outputs.Clear();

            foreach (OutConnector output in _outputs)
            {
                result.Outputs.Add(new OutConnector(output.Name, output.Unit, result));
            }

            foreach (InConnector input in _inputs)
            {
                result.Inputs.Add(new InConnector(input.Name, input.Unit, result));
            }

            return result;
        }
    }
}
