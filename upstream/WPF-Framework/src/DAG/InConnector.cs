using System.ComponentModel;

namespace DAG
{
    /// <summary>
    /// Represents an input connector on a node in a directed acyclic graph (DAG).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Input connectors are the receiving end of connections between nodes. Each input connector
    /// can receive at most one connection from an output connector of another node.
    /// </para>
    /// <para>
    /// Input connectors have an optional <see cref="Unit"/> property that can be used to validate
    /// connection compatibility. When units are specified, connections are typically only allowed
    /// between connectors with matching units.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create an input connector for a node
    /// var input = new InConnector("Data Input", "kg/s", myNode);
    /// myNode.Inputs.Add(input);
    /// </code>
    /// </example>
    public class InConnector : INotifyPropertyChanged
    {
        #region Fields

        private string _name = "";
        private string _unit = "";

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the display name of the input connector.
        /// </summary>
        /// <value>The name shown next to the connector in the node control.</value>
        public string Name
        {
            get => _name;
            set => Utilities.SetString(value, ref _name, PropertyChanged, this);
        }

        /// <summary>
        /// Gets or sets the unit of measurement for this input connector.
        /// </summary>
        /// <value>
        /// A string representing the unit (e.g., "kg/s", "m", "PSI").
        /// An empty string indicates no unit restriction.
        /// </value>
        /// <remarks>
        /// Units can be used to validate connection compatibility. If units do not match between
        /// an input and output connector, the connection may be considered invalid by the application.
        /// </remarks>
        public string Unit
        {
            get => _unit;
            set => Utilities.SetString(value, ref _unit, PropertyChanged, this);
        }

        /// <summary>
        /// Gets the parent node that owns this connector.
        /// </summary>
        /// <value>The <see cref="NodeBase"/> instance that this connector belongs to.</value>
        /// <remarks>
        /// The parent reference is set during construction and cannot be changed.
        /// This ensures connectors maintain a stable relationship with their owning node.
        /// </remarks>
        public NodeBase Parent { get; } = null;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InConnector"/> class.
        /// </summary>
        /// <param name="name">The display name of the connector.</param>
        /// <param name="unit">The unit of measurement for connection validation. Use empty string for no unit.</param>
        /// <param name="parent">The node that owns this connector.</param>
        /// <example>
        /// <code>
        /// var flowInput = new InConnector("Flow Rate", "m3/s", parentNode);
        /// var signalInput = new InConnector("Trigger", "", parentNode); // No unit
        /// </code>
        /// </example>
        public InConnector(string name, string unit, NodeBase parent)
        {
            _name = name;
            _unit = unit;
            Parent = parent;
        }

        #endregion
    }
}
