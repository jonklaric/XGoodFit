using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;

namespace WpfCompat;

/// <summary>
/// The clipboard operations the ported code uses, synchronous as in WPF's
/// <c>System.Windows.Clipboard</c>, over the active window's Avalonia clipboard.
/// </summary>
public static class Clipboard
{
    /// <summary>Puts <paramref name="text"/> on the clipboard.</summary>
    public static void SetText(string text) => Modal.Wait(Current().SetTextAsync(text));

    /// <summary>The text on the clipboard, or an empty string, as in WPF.</summary>
    public static string GetText() => Modal.Wait(Current().TryGetTextAsync()) ?? string.Empty;

    /// <summary>Whether the clipboard holds text.</summary>
    public static bool ContainsText() => !string.IsNullOrEmpty(Modal.Wait(Current().TryGetTextAsync()));

    /// <summary>Puts <paramref name="image"/> on the clipboard.</summary>
    public static void SetImage(Bitmap image) => Modal.Wait(Current().SetBitmapAsync(image));

    /// <summary>The image on the clipboard, or null.</summary>
    public static Bitmap? GetImage() => Modal.Wait(Current().TryGetBitmapAsync());

    /// <summary>Whether the clipboard holds an image.</summary>
    public static bool ContainsImage() => GetImage() is not null;

    /// <summary>Empties the clipboard.</summary>
    public static void Clear() => Modal.Wait(Current().ClearAsync());

    private static IClipboard Current() =>
        (Modal.ActiveWindow() as TopLevel)?.Clipboard
        ?? throw new InvalidOperationException("The clipboard needs a window.");
}
