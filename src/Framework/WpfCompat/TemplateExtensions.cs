using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace WpfCompat;

/// <summary>
/// WPF's <c>FrameworkTemplate.FindName</c>, so ported code can keep finding a template part as
/// <c>comboBox.Template.FindName("PART_EditableTextBox", comboBox)</c>.
/// </summary>
public static class TemplateExtensions
{
    extension(IControlTemplate? template)
    {
        /// <summary>
        /// The element named <paramref name="name"/> in the template applied to
        /// <paramref name="templatedParent"/>, or null, as WPF's <c>FrameworkTemplate.FindName</c>.
        /// </summary>
        public object? FindName(string name, TemplatedControl templatedParent) =>
            templatedParent.GetTemplateDescendants().OfType<Control>().FirstOrDefault(child => child.Name == name);
    }
}
