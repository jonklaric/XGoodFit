using SkiaSharp;

namespace Branding;

/// <summary>Sheets that show icons at their real sizes on light and dark backgrounds, to judge them by.</summary>
internal static class Previews
{
    /// <summary>An icon on a sheet: a label, and its image at each size.</summary>
    public sealed record Row(string Label, Func<int, SKBitmap> Image);

    /// <summary>A light and a dark background (a light and a dark taskbar, Dock or panel).</summary>
    public static readonly (string Name, SKColor Color, SKColor Text)[] Backgrounds =
    [
        ("Light", new SKColor(0xF3, 0xF3, 0xF3), new SKColor(0x55, 0x55, 0x55)),
        ("Dark", new SKColor(0x20, 0x20, 0x20), new SKColor(0xB0, 0xB0, 0xB0)),
    ];

    private const int Pad = 16, LabelHeight = 26, CaptionHeight = 20;

    /// <summary>
    /// Each row's icon at <paramref name="sizes"/> (largest first) and its smallest size magnified
    /// <paramref name="magnify"/> times, pixel for pixel, on each background.
    /// </summary>
    public static SKBitmap Sheet(string title, IReadOnlyList<Row> rows, int[] sizes, int magnify)
    {
        int smallest = sizes.Min();
        int panelWidth = Pad + sizes.Sum(size => size + Pad) + smallest * magnify + Pad;
        int panelHeight = Pad + sizes.Max() + CaptionHeight + Pad / 2;
        int width = Pad + Backgrounds.Length * (panelWidth + Pad);
        int rowHeight = LabelHeight + panelHeight + Pad;
        int height = Pad + LabelHeight + rows.Count * rowHeight;

        var sheet = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(sheet);
        canvas.Clear(SKColors.White);
        using var titleFont = new SKFont(SKTypeface.Default, 17) { Edging = SKFontEdging.Antialias };
        using var font = new SKFont(SKTypeface.Default, 13) { Edging = SKFontEdging.Antialias };
        using var ink = new SKPaint { IsAntialias = true, Color = new SKColor(0x22, 0x22, 0x22) };
        canvas.DrawText(title, Pad, Pad + 16, SKTextAlign.Left, titleFont, ink);

        for (int r = 0; r < rows.Count; r++)
        {
            int top = Pad + LabelHeight + r * rowHeight;
            canvas.DrawText(rows[r].Label, Pad, top + 17, SKTextAlign.Left, font, ink);
            var images = sizes.ToDictionary(size => size, rows[r].Image);
            for (int b = 0; b < Backgrounds.Length; b++)
            {
                (_, SKColor color, SKColor text) = Backgrounds[b];
                float left = Pad + b * (panelWidth + Pad), panelTop = top + LabelHeight;
                using var fill = new SKPaint { Color = color };
                canvas.DrawRect(left, panelTop, panelWidth, panelHeight, fill);
                using var caption = new SKPaint { IsAntialias = true, Color = text };
                float x = left + Pad, baseline = panelTop + Pad + sizes.Max();
                foreach (int size in sizes)
                {
                    canvas.DrawBitmap(images[size], x, baseline - size);
                    canvas.DrawText($"{size}", x + size / 2f, baseline + 15, SKTextAlign.Center, font, caption);
                    x += size + Pad;
                }
                using SKImage small = SKImage.FromBitmap(images[smallest]);
                var zoomed = SKRect.Create(x, baseline - smallest * magnify, smallest * magnify, smallest * magnify);
                canvas.DrawImage(small, zoomed, new SKSamplingOptions(SKFilterMode.Nearest), null);
                canvas.DrawText($"{smallest} ×{magnify}", zoomed.MidX, baseline + 15, SKTextAlign.Center, font, caption);
            }
            foreach (SKBitmap image in images.Values)
                image.Dispose();
        }
        return sheet;
    }

    /// <summary>Panels the icon is seen on, roughly: their background colours, light and dark.</summary>
    public static readonly (string Name, SKColor Color)[] PanelBackgrounds =
    [
        ("Windows 11 taskbar, light", new SKColor(0xEE, 0xEE, 0xEE)),
        ("Windows 11 taskbar, dark", new SKColor(0x1C, 0x1C, 0x1C)),
        ("macOS Dock, light", new SKColor(0xE3, 0xE3, 0xE6)),
        ("macOS Dock, dark", new SKColor(0x2C, 0x2C, 0x2E)),
        ("GNOME top bar", new SKColor(0x00, 0x00, 0x00)),
        ("GNOME dash", new SKColor(0x36, 0x36, 0x36)),
        ("KDE Plasma panel, Breeze", new SKColor(0xEF, 0xF0, 0xF1)),
        ("KDE Plasma panel, Breeze Dark", new SKColor(0x2A, 0x2E, 0x32)),
    ];

    /// <summary>Two icons at <paramref name="sizes"/> on each of <see cref="PanelBackgrounds"/>.</summary>
    public static SKBitmap Panels(string title, Row icon, Row other, int[] sizes)
    {
        const int LabelWidth = 230, Gap = 14, Between = 4 * Gap;
        int band = sizes.Max() + 2 * Gap, iconsWidth = sizes.Sum(size => size + Gap);
        int width = Pad + LabelWidth + 2 * iconsWidth + Between + Pad;
        int height = Pad + LabelHeight + LabelHeight + PanelBackgrounds.Length * band + Pad;
        var sheet = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(sheet);
        canvas.Clear(SKColors.White);
        using var titleFont = new SKFont(SKTypeface.Default, 17) { Edging = SKFontEdging.Antialias };
        using var font = new SKFont(SKTypeface.Default, 13) { Edging = SKFontEdging.Antialias };
        using var ink = new SKPaint { IsAntialias = true, Color = new SKColor(0x22, 0x22, 0x22) };
        canvas.DrawText(title, Pad, Pad + 16, SKTextAlign.Left, titleFont, ink);
        float columns = Pad + LabelWidth;
        canvas.DrawText($"{string.Join(", ", sizes)} px", Pad, Pad + LabelHeight + 17, SKTextAlign.Left, font, ink);
        canvas.DrawText(icon.Label, columns, Pad + LabelHeight + 17, SKTextAlign.Left, font, ink);
        canvas.DrawText(other.Label, columns + iconsWidth + Between, Pad + LabelHeight + 17, SKTextAlign.Left, font, ink);

        var images = sizes.ToDictionary(size => size, size => (Icon: icon.Image(size), Other: other.Image(size)));
        for (int p = 0; p < PanelBackgrounds.Length; p++)
        {
            (string name, SKColor color) = PanelBackgrounds[p];
            float top = Pad + 2 * LabelHeight + p * band;
            using var fill = new SKPaint { Color = color };
            canvas.DrawRect(Pad, top, width - 2 * Pad, band, fill);
            color.ToHsl(out _, out _, out float lightness);
            using var text = new SKPaint { IsAntialias = true, Color = lightness > 50 ? new SKColor(0x33, 0x33, 0x33) : new SKColor(0xD0, 0xD0, 0xD0) };
            canvas.DrawText(name, Pad + Gap, top + band / 2f + 5, SKTextAlign.Left, font, text);
            float x = columns;
            foreach (bool first in new[] { true, false })
            {
                foreach (int size in sizes)
                {
                    canvas.DrawBitmap(first ? images[size].Icon : images[size].Other, x, top + (band - size) / 2f);
                    x += size + Gap;
                }
                x += Between;
            }
        }
        foreach ((SKBitmap first, SKBitmap second) in images.Values)
        {
            first.Dispose();
            second.Dispose();
        }
        return sheet;
    }
}
