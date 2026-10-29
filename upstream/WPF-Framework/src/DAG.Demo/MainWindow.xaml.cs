using DAG;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace DAG.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// Initializes the demo window and assigns the sample graph.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            GraphCanvas.Graph = new TestGraph();
        }

        /// <summary>
        /// Adds a sample node at the default demo position.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void AddNodeButton_Click(object sender, RoutedEventArgs e)
        {
            AddNode(new Point(180, 180));
        }

        /// <summary>
        /// Adds the demo node command to the canvas context menu.
        /// </summary>
        /// <param name="cm">The context menu being prepared.</param>
        /// <param name="canvasPosition">The canvas position where the menu was opened.</param>
        private void GraphCanvas_PreviewCanvasContextMenu(ContextMenu cm, Point canvasPosition)
        {
            Image addIcon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DAG.Demo;component/Resources/Add.png")) };
            var cmi = new MenuItem() { Header = "Add Node", Tag = canvasPosition, Icon = addIcon };
            cmi.Click += Cmi_Click;
            cm.Items.Add(cmi);
        }

        /// <summary>
        /// Adds a node near the context-menu position.
        /// </summary>
        /// <param name="sender">The context-menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void Cmi_Click(object sender, RoutedEventArgs e)
        {
            var p = (Point)((MenuItem)sender).Tag;
            AddNode(new Point(p.X - 20, p.Y - 20));
        }

        private static readonly Random _random = new();

        /// <summary>
        /// Creates and decorates a sample node at the specified canvas position.
        /// </summary>
        /// <param name="p">The canvas position for the node.</param>
        private void AddNode(Point p)
        {
            var newNode = new TestNode() { LeftPosition = p.X, TopPosition = p.Y };
            GraphCanvas.Graph.Nodes.Add(newNode);
            DAGControls.NodeControl cntrl = GraphCanvas.GetNodeControl(newNode);
            // GetNodeControl returns null if the visual hasn't been added to _nodes yet.
            // Skip the decoration if that happens; the canvas will fill in the NodeControl
            // on its own timing.
            if (cntrl == null) return;
            if (FindResource("HazardIcon") is DrawingImage icon)
                cntrl.NodeIcon = icon;

            ComboBox c = new ComboBox() { Margin = new Thickness(5) };
            _ = c.Items.Add(new ComboBoxItem() { Content = "Option 1" });
            _ = c.Items.Add(new ComboBoxItem() { Content = "Option 2" });
            cntrl.NodeContent = c;

            double r = _random.NextDouble();
            if (r < .33)
            {
                cntrl.HeaderColor = new SolidColorBrush(Colors.LightSeaGreen);
            }
            else if (r > .66)
            {
                cntrl.HeaderColor = new SolidColorBrush(Colors.LightGoldenrodYellow);
            }
            else
            {
                //leave it red
            }
        }

        /// <summary>
        /// Adds demo-specific node commands to the node context menu.
        /// </summary>
        /// <param name="cm">The context menu being prepared.</param>
        /// <param name="node">The node associated with the menu.</param>
        private void GraphCanvas_PreviewNodeContextMenu(ContextMenu cm, DAG.NodeBase node)
        {
            Image editIcon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DAG.Demo;component/Resources/EditWindow.png")) };
            var cmi = new MenuItem() { Header = "Edit", Tag = node, Icon = editIcon };
            cmi.Click += (s, e) => { MessageBox.Show("Nothing here"); };
            cm.Items.Add(cmi);
        }

        /// <summary>
        /// Displays feedback when the user requests an automatic connection.
        /// </summary>
        /// <param name="fromConnector">The output connector that starts the connection.</param>
        private void GraphCanvas_AutoConnection_Clicked(DAG.OutConnector fromConnector)
        {
            ConnectionText.TextDecorations = null;
            ConnectionText.Text = $"Create node and connection from {fromConnector.Parent.Name} - '{fromConnector.Name}'";
            if (FindResource("animate") is Storyboard sb1) sb1.Begin(ConnectionText);
        }

        /// <summary>
        /// Handles the placeholder graph save command in the demo.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void SaveGraphButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement when FlowGraphCanvas.GraphToXElement() is available
        }

        /// <summary>
        /// Handles the placeholder graph load command in the demo.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void LoadGraphButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement when FlowGraphCanvas.LoadFromXElement() is available
        }

        /// <summary>
        /// Adds a sample rectangle visual to the graph canvas.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Rectangle rect = new Rectangle() { Width = 350, Height = 200, Stroke = Brushes.Black, StrokeThickness = 2 };
            Canvas.SetLeft(rect, 90);
            Canvas.SetTop(rect, 200);

            GraphCanvas.AddVisual(rect);
        }

        /// <summary>
        /// Subscribes to graph connection notifications when the window loads.
        /// </summary>
        /// <param name="sender">The window that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GraphCanvas.Graph.ConnectionsAdded += Graph_ConnectionsAdded;
            GraphCanvas.Graph.ConnectionsRemoved += Graph_ConnectionsRemoved;
        }

        /// <summary>
        /// Displays connection-removal feedback in the demo status text.
        /// </summary>
        /// <param name="connections">The connections that were removed.</param>
        private void Graph_ConnectionsRemoved(Tuple<OutConnector, InConnector>[] connections)
        {
            //Show update
            ConnectionText.TextDecorations = TextDecorations.Strikethrough;
            StringBuilder sb = new StringBuilder();
            foreach (var connection in connections)
            {
                _ = sb.AppendLine($"{connection.Item1.Parent.Name} - '{connection.Item1.Name}' to '{connection.Item2.Name}' - {connection.Item2.Parent.Name}");
            }
            ConnectionText.Text = sb.ToString();
            if (FindResource("animate") is Storyboard sb2) sb2.Begin(ConnectionText);
        }

        /// <summary>
        /// Displays connection-addition feedback in the demo status text.
        /// </summary>
        /// <param name="connections">The connections that were added.</param>
        private void Graph_ConnectionsAdded(Tuple<OutConnector, InConnector>[] connections)
        {
            //Show update
            ConnectionText.TextDecorations = null;
            StringBuilder sb = new StringBuilder();
            foreach (var connection in connections)
            {
                _ = sb.AppendLine($"{connection.Item1.Parent.Name} - '{connection.Item1.Name}' to '{connection.Item2.Name}' - {connection.Item2.Parent.Name}");
            }
            ConnectionText.Text = sb.ToString();
            if (FindResource("animate") is Storyboard sb3) sb3.Begin(ConnectionText);
        }

        /// <summary>
        /// Unsubscribes from graph notifications before the window closes.
        /// </summary>
        /// <param name="e">The close event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            GraphCanvas.Graph.ConnectionsAdded -= Graph_ConnectionsAdded;
            GraphCanvas.Graph.ConnectionsRemoved -= Graph_ConnectionsRemoved;
            base.OnClosed(e);
        }

    }
}
