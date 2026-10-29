using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GenericControls
{
    /// <summary>
    /// A user control that wraps resizable text box with support for read-only mode and
    /// dynamic height adjustment using a drag handle.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class ResizableTextBoxControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResizableTextBoxControl"/> class.
        /// </summary>
        public ResizableTextBoxControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Dependency property for the text content of the control.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(ResizableTextBoxControl), new UIPropertyMetadata(""));
        /// <summary>
        /// Gets/sets the text content of the control.
        /// </summary>
        public string Text
        {
            get
            {
                return (this.GetValue(TextProperty)?.ToString());
            }
            set
            {
                this.SetValue(TextProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for determining whether the text box is read-only.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(ResizableTextBoxControl), new UIPropertyMetadata(false));
        /// <summary>
        /// gets/sets a value indicating whether the text box is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return (bool)this.GetValue(IsReadOnlyProperty);
            }
            set
            {
                this.SetValue(IsReadOnlyProperty, value);
            }
        }

        /// <summary>
        /// Handles the drag movement of the resize thumb and adjusts the control's height accordingly.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The drag delta event arguments containing the vertical change.</param>
        private void ResizeThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double newHeight = this.ActualHeight + e.VerticalChange;
            if (newHeight < 18d)
                newHeight = 18d;
            if (newHeight > this.MaxHeight)
                newHeight = this.MaxHeight;
            this.Height = newHeight;
        }

        /// <summary>
        /// Updates the data binding source when Enter is pressed in the text box.
        /// </summary>
        /// <param name="sender">The source of the event (the text box).</param>
        /// <param name="e">The key event arguments.</param>
        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                var binding = BindingOperations.GetBindingExpression(tBox, TextBox.TextProperty);
                if (binding is not null)
                    binding.UpdateSource();
            }
        }
    }
}