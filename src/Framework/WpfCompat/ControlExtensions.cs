using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace WpfCompat;

/// <summary>
/// WPF members that Avalonia controls lack or keep elsewhere, so ported code-behind can keep writing
/// them: <c>ToolTip</c> on every control, a text box's <c>SelectionLength</c>, an items control's
/// <c>HasItems</c>, and <c>SetBinding</c>.
/// </summary>
public static class ControlExtensions
{
    extension(AvaloniaObject target)
    {
        /// <summary>Binds <paramref name="property"/>, as WPF's <c>SetBinding</c> (Avalonia's <c>Bind</c>).</summary>
        public BindingExpressionBase SetBinding(AvaloniaProperty property, BindingBase binding) => target.Bind(property, binding);
    }

    extension(ItemsControl itemsControl)
    {
        /// <summary>Whether the control has items, as WPF's <c>ItemsControl.HasItems</c>.</summary>
        public bool HasItems => itemsControl.ItemCount > 0;
    }

    extension(Control control)
    {
        /// <summary>The control's tool tip: WPF's <c>FrameworkElement.ToolTip</c>, Avalonia's <c>ToolTip.Tip</c>.</summary>
        public object? ToolTip
        {
            get => Avalonia.Controls.ToolTip.GetTip(control);
            set => Avalonia.Controls.ToolTip.SetTip(control, value);
        }
    }

    extension(TextBox textBox)
    {
        /// <summary>
        /// How many characters are selected: WPF's <c>TextBox.SelectionLength</c>. Setting it keeps the
        /// selection's start.
        /// </summary>
        public int SelectionLength
        {
            get => Math.Abs(textBox.SelectionEnd - textBox.SelectionStart);
            set
            {
                int start = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
                textBox.SelectionStart = start;
                textBox.SelectionEnd = start + value;
            }
        }
    }
}
