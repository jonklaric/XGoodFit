using DAG;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DAGControls
{
    /// <summary>
    /// Delegate for handling auto-connection click events on output connectors.
    /// </summary>
    /// <param name="fromConnector">The output connector that was clicked.</param>
    public delegate void AutoConnectionClicked(OutConnector fromConnector);

    /// <summary>
    /// A WPF UserControl that provides a visual representation of a node in a flow graph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The NodeControl displays a node with its header, input/output connectors, and optional content.
    /// It provides interactive features such as:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Visual display of input and output connectors as ellipses</description></item>
    /// <item><description>Customizable header color and icon</description></item>
    /// <item><description>Delete button for removing the node</description></item>
    /// <item><description>Auto-connection buttons on output connectors</description></item>
    /// <item><description>Custom content area for node-specific UI</description></item>
    /// </list>
    /// <para>
    /// The control automatically synchronizes its position with the underlying <see cref="NodeBase"/>
    /// through data binding.
    /// </para>
    /// </remarks>
    public partial class NodeControl : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Identifies the read-only <see cref="Node"/> dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey NodePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Node),
            typeof(NodeBase),
            typeof(NodeControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None));

        /// <summary>
        /// Identifies the <see cref="Node"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NodeProperty = NodePropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="NodeContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NodeContentProperty = DependencyProperty.Register(
            nameof(NodeContent),
            typeof(object),
            typeof(NodeControl),
            new UIPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HeaderColor"/> dependency property.
        /// </summary>
        // Default null — reference-type DP defaults are shared across all instances.
        // Per-instance default initialized in the constructor.
        public static readonly DependencyProperty HeaderColorProperty = DependencyProperty.Register(
            nameof(HeaderColor),
            typeof(SolidColorBrush),
            typeof(NodeControl),
            new FrameworkPropertyMetadata((SolidColorBrush)null));

        /// <summary>
        /// Identifies the <see cref="NodeIcon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NodeIconProperty = DependencyProperty.Register(
            nameof(NodeIcon),
            typeof(ImageSource),
            typeof(NodeControl),
            new UIPropertyMetadata(null));

        #endregion

        #region Properties

        /// <summary>
        /// Gets the underlying node data object.
        /// </summary>
        /// <value>The <see cref="NodeBase"/> instance this control represents.</value>
        public NodeBase Node
        {
            get => (NodeBase)GetValue(NodeProperty);
            protected set => SetValue(NodePropertyKey, value);
        }

        /// <summary>
        /// Gets or sets the custom content displayed inside the node.
        /// </summary>
        /// <value>An object that will be displayed in the node's content area.</value>
        /// <remarks>
        /// Use this property to add node-specific UI elements such as parameters,
        /// configuration options, or status indicators.
        /// </remarks>
        public object NodeContent
        {
            get => GetValue(NodeContentProperty);
            set => SetValue(NodeContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the background color of the node header.
        /// </summary>
        /// <value>A <see cref="SolidColorBrush"/> for the header background. Default is red.</value>
        public SolidColorBrush HeaderColor
        {
            get => (SolidColorBrush)GetValue(HeaderColorProperty);
            set => SetValue(HeaderColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon displayed in the node header.
        /// </summary>
        /// <value>An <see cref="ImageSource"/> for the header icon, or null for no icon.</value>
        public ImageSource NodeIcon
        {
            get => (ImageSource)GetValue(NodeIconProperty);
            set => SetValue(NodeIconProperty, value);
        }

        /// <summary>
        /// Gets a dictionary mapping input connectors to their visual ellipse elements.
        /// </summary>
        /// <value>A dictionary of <see cref="InConnector"/> to <see cref="Ellipse"/> mappings.</value>
        /// <remarks>
        /// This dictionary is used by the canvas to determine connector positions for drawing connection paths.
        /// </remarks>
        public Dictionary<InConnector, Ellipse> InConnectors { get; private set; } = new Dictionary<InConnector, Ellipse>();

        /// <summary>
        /// Gets a dictionary mapping output connectors to their visual ellipse elements.
        /// </summary>
        /// <value>A dictionary of <see cref="OutConnector"/> to <see cref="Ellipse"/> mappings.</value>
        /// <remarks>
        /// This dictionary is used by the canvas to determine connector positions for drawing connection paths.
        /// </remarks>
        public Dictionary<OutConnector, Ellipse> OutConnectors { get; private set; } = new Dictionary<OutConnector, Ellipse>();

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the delete button is clicked.
        /// </summary>
        public event RoutedEventHandler Delete_Clicked;

        /// <summary>
        /// Occurs when an auto-connection button on an output connector is clicked.
        /// </summary>
        public event AutoConnectionClicked AutoConnection_Clicked;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeControl"/> class.
        /// </summary>
        /// <param name="node">The node data object to display.</param>
        /// <remarks>
        /// The control will automatically bind its position to the node's LeftPosition and TopPosition properties,
        /// and will refresh connectors when the node's input or output collections change.
        /// </remarks>
        public NodeControl(NodeBase node)
        {
            InitializeComponent();
            // Per-instance default (DP default is null — see DP registration).
            if (HeaderColor == null)
            {
                HeaderColor = new SolidColorBrush(Colors.Red);
            }
            Node = node;

            RefreshInConnectors();
            RefreshOutConnectors();

            // Subscribe via named handlers so we can unsubscribe on Unloaded. Previous
            // anonymous-lambda subscriptions were never unhooked, keeping the NodeControl
            // alive for the lifetime of the node's observable collections.
            node.Inputs.CollectionChanged += OnInputsCollectionChanged;
            node.Outputs.CollectionChanged += OnOutputsCollectionChanged;
            Unloaded += OnNodeControlUnloaded;

            // Bind position to node
            Binding b = new Binding(nameof(NodeBase.LeftPosition))
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = node,
                Mode = BindingMode.TwoWay
            };
            _ = SetBinding(Canvas.LeftProperty, b);

            b = new Binding(nameof(NodeBase.TopPosition))
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = node
            };
            _ = SetBinding(Canvas.TopProperty, b);
        }

        /// <summary>
        /// Refreshes input connector visuals after the node input collection changes.
        /// </summary>
        /// <param name="sender">The input connector collection that raised the event.</param>
        /// <param name="e">The collection change details.</param>
        private void OnInputsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            => RefreshInConnectors();

        /// <summary>
        /// Refreshes output connector visuals after the node output collection changes.
        /// </summary>
        /// <param name="sender">The output connector collection that raised the event.</param>
        /// <param name="e">The collection change details.</param>
        private void OnOutputsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            => RefreshOutConnectors();

        /// <summary>
        /// Releases node collection subscriptions when the control unloads.
        /// </summary>
        /// <param name="sender">The control that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void OnNodeControlUnloaded(object sender, RoutedEventArgs e)
        {
            if (Node != null)
            {
                Node.Inputs.CollectionChanged -= OnInputsCollectionChanged;
                Node.Outputs.CollectionChanged -= OnOutputsCollectionChanged;
            }
            Unloaded -= OnNodeControlUnloaded;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the input connector visual elements dictionary.
        /// </summary>
        /// <remarks>
        /// Call this method when input connectors have been added or removed to update
        /// the visual element mappings used for drawing connections.
        /// </remarks>
        public void RefreshInConnectors()
        {
            if (InConnectors.Count() == Node.Inputs.Count()) { return; }

            InConnectors = new Dictionary<InConnector, Ellipse>(Node.Inputs.Count());
            for (int i = 0; i < Node.Inputs.Count; i++)
            {
                UIElement uiElement = (UIElement)InputsListBox.ItemContainerGenerator.ContainerFromIndex(i);
                if (uiElement == null) { continue; }
                _ = ((ContentPresenter)uiElement).ApplyTemplate();
                InConnectors.Add(Node.Inputs[i], (Ellipse)((ContentPresenter)uiElement).ContentTemplate.FindName("InConnector", (ContentPresenter)uiElement));
            }
        }

        /// <summary>
        /// Refreshes the output connector visual elements dictionary.
        /// </summary>
        /// <remarks>
        /// Call this method when output connectors have been added or removed to update
        /// the visual element mappings used for drawing connections.
        /// </remarks>
        public void RefreshOutConnectors()
        {
            if (OutConnectors.Count() == Node.Outputs.Count()) { return; }

            OutConnectors = new Dictionary<OutConnector, Ellipse>(Node.Outputs.Count());
            for (int i = 0; i < Node.Outputs.Count; i++)
            {
                UIElement uiElement = (UIElement)OutputsListBox.ItemContainerGenerator.ContainerFromIndex(i);
                if (uiElement == null) { continue; }
                _ = ((ContentPresenter)uiElement).ApplyTemplate();
                OutConnectors.Add(Node.Outputs[i], (Ellipse)((ContentPresenter)uiElement).ContentTemplate.FindName("OutConnector", (ContentPresenter)uiElement));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Refreshes connector visual dictionaries after layout creates item containers.
        /// </summary>
        /// <param name="sender">The control that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshInConnectors();
            RefreshOutConnectors();
        }

        /// <summary>
        /// Raises the node delete request for the hosting canvas.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Delete_Clicked?.Invoke(this, e);
        }

        /// <summary>
        /// Raises an automatic connection request from the clicked output connector.
        /// </summary>
        /// <param name="sender">The output connector host that raised the event.</param>
        /// <param name="e">The mouse-button event data.</param>
        private void AddButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid targetGrid = (Grid)sender;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AutoConnection_Clicked?.Invoke((OutConnector)targetGrid.DataContext);
                e.Handled = true;
            }
        }

        #endregion
    }
}
