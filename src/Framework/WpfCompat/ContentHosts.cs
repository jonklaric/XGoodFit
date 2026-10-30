using Avalonia.Controls;

namespace WpfCompat;

/// <summary>
/// WPF's <c>host.Content = control</c> for a control that another content control shows: WPF moves the
/// control to the new host; Avalonia throws ("already has a visual parent").
/// </summary>
/// <remarks>
/// XGoodFit addition. Upstream's analysis views show plots their element owns, in content controls. A new
/// view of an element (its document opened again) takes the plots while an earlier view, which keeps its
/// element, still holds them.
/// </remarks>
public static class ContentHosts
{
    extension(ContentControl host)
    {
        /// <summary>Sets <c>Content</c>, taking the content from another content control that shows it, as WPF does.</summary>
        /// <param name="content">The content.</param>
        public void TakeContent(object? content)
        {
            if (content is Control control && control.Parent is ContentControl previous && previous != host && ReferenceEquals(previous.Content, control))
            {
                previous.Content = null;
            }
            host.Content = content;
        }
    }
}
