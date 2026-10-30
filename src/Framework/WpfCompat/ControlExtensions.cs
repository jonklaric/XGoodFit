using Avalonia.Controls;

namespace WpfCompat;

/// <summary>
/// WPF properties that Avalonia controls lack or keep elsewhere, so ported code-behind can keep
/// writing them: <c>ToolTip</c> on every control, and a text box's <c>SelectionLength</c>.
/// </summary>
public static class ControlExtensions
{
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
