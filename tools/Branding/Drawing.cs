using System.Globalization;
using System.Text;
using SkiaSharp;

namespace Branding;

/// <summary>A linear gradient in the drawing's coordinates (SVG's userSpaceOnUse).</summary>
internal sealed record Gradient(SKPoint Start, SKPoint End, params (float Offset, SKColor Color)[] Stops);

/// <summary>What a shape is painted with: a colour, or a gradient, at an opacity.</summary>
internal sealed record Brush(SKColor Color, Gradient? Gradient = null, float Opacity = 1)
{
    public static implicit operator Brush(SKColor color) => new(color);

    public static implicit operator Brush(Gradient gradient) => new(SKColors.Black, gradient);

    public Brush WithOpacity(float opacity) => this with { Opacity = opacity };
}

/// <summary>A path, filled, or stroked with round caps and joins (a stroke width above 0).</summary>
internal sealed record Shape(SKPath Path, Brush Brush, float StrokeWidth = 0);

/// <summary>
/// A drawing on a square of <see cref="Size"/> units, its shapes back to front. Their paths are lines and
/// quadratic and cubic curves (conics, which SVG lacks, are split into quadratic curves), so SVG and
/// SkiaSharp draw exactly the same.
/// </summary>
internal sealed class Drawing
{
    public const float Size = 256;

    public List<Shape> Shapes { get; } = [];

    public Drawing Fill(SKPath path, Brush brush)
    {
        Shapes.Add(new Shape(WithoutConics(path), brush));
        return this;
    }

    public Drawing Stroke(SKPath path, Brush brush, float width)
    {
        Shapes.Add(new Shape(WithoutConics(path), brush, width));
        return this;
    }

    // The path with each conic (from a round cap or join, or a path operation on one) as 8 quadratic curves.
    private static SKPath WithoutConics(SKPath path)
    {
        var result = new SKPath { FillType = path.FillType };
        using SKPath.RawIterator iterator = path.CreateRawIterator();
        var points = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    result.MoveTo(points[0]);
                    break;
                case SKPathVerb.Line:
                    result.LineTo(points[1]);
                    break;
                case SKPathVerb.Quad:
                    result.QuadTo(points[1], points[2]);
                    break;
                case SKPathVerb.Conic:
                    SKPoint[] quads = SKPath.ConvertConicToQuads(points[0], points[1], points[2], iterator.ConicWeight(), 3);
                    for (int i = 1; i + 1 < quads.Length; i += 2)
                        result.QuadTo(quads[i], quads[i + 1]);
                    break;
                case SKPathVerb.Cubic:
                    result.CubicTo(points[1], points[2], points[3]);
                    break;
                case SKPathVerb.Close:
                    result.Close();
                    break;
            }
        }
        return result;
    }

    /// <summary>Draws onto a canvas, scaled to a square of <paramref name="pixels"/>.</summary>
    public void Draw(SKCanvas canvas, float pixels)
    {
        canvas.Save();
        canvas.Scale(pixels / Size);
        foreach (Shape shape in Shapes)
        {
            using SKPaint paint = Paint(shape);
            canvas.DrawPath(shape.Path, paint);
        }
        canvas.Restore();
    }

    /// <summary>The drawing as a square image of <paramref name="pixels"/>, on a transparent background.</summary>
    public SKBitmap Render(int pixels)
    {
        var bitmap = new SKBitmap(new SKImageInfo(pixels, pixels, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        Draw(canvas, pixels);
        return bitmap;
    }

    private static SKPaint Paint(Shape shape)
    {
        Brush brush = shape.Brush;
        var paint = new SKPaint
        {
            IsAntialias = true,
            Style = shape.StrokeWidth > 0 ? SKPaintStyle.Stroke : SKPaintStyle.Fill,
            StrokeWidth = shape.StrokeWidth,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };
        byte alpha = (byte)MathF.Round(255 * brush.Opacity);
        if (brush.Gradient is { } gradient)
        {
            // The paint's alpha scales the shader's.
            paint.Color = SKColors.Black.WithAlpha(alpha);
            paint.Shader = SKShader.CreateLinearGradient(gradient.Start, gradient.End,
                gradient.Stops.Select(stop => stop.Color).ToArray(), gradient.Stops.Select(stop => stop.Offset).ToArray(),
                SKShaderTileMode.Clamp);
        }
        else
        {
            paint.Color = brush.Color.WithAlpha((byte)MathF.Round(brush.Color.Alpha * brush.Opacity));
        }
        return paint;
    }

    /// <summary>The drawing as an SVG document.</summary>
    public string ToSvg(string title, string? comment = null)
    {
        var svg = new StringBuilder();
        var defs = new StringBuilder();
        var body = new StringBuilder();
        // Each gradient once, by its definition (without its id).
        var gradients = new Dictionary<string, string>();
        foreach (Shape shape in Shapes)
        {
            string paint;
            if (shape.Brush.Gradient is { } gradient)
            {
                var definition = new StringBuilder();
                definition.Append($" gradientUnits=\"userSpaceOnUse\" x1=\"{N(gradient.Start.X)}\" y1=\"{N(gradient.Start.Y)}\" x2=\"{N(gradient.End.X)}\" y2=\"{N(gradient.End.Y)}\">\n");
                foreach ((float offset, SKColor color) in gradient.Stops)
                {
                    string stopOpacity = color.Alpha == 255 ? "" : $" stop-opacity=\"{N(color.Alpha / 255f, 3)}\"";
                    definition.Append($"      <stop offset=\"{N(offset, 3)}\" stop-color=\"{Hex(color)}\"{stopOpacity}/>\n");
                }
                if (!gradients.TryGetValue(definition.ToString(), out string? id))
                {
                    id = $"g{gradients.Count + 1}";
                    gradients.Add(definition.ToString(), id);
                    defs.Append($"    <linearGradient id=\"{id}\"").Append(definition).Append("    </linearGradient>\n");
                }
                paint = $"url(#{id})";
            }
            else
            {
                paint = Hex(shape.Brush.Color);
            }

            float opacity = shape.Brush.Opacity * (shape.Brush.Gradient is null ? shape.Brush.Color.Alpha / 255f : 1);
            string rule = shape.Path.FillType == SKPathFillType.EvenOdd ? " fill-rule=\"evenodd\"" : "";
            string attributes = shape.StrokeWidth > 0
                ? $"fill=\"none\" stroke=\"{paint}\" stroke-width=\"{N(shape.StrokeWidth)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"" + (opacity < 1 ? $" stroke-opacity=\"{N(opacity, 3)}\"" : "")
                : $"fill=\"{paint}\"{rule}" + (opacity < 1 ? $" fill-opacity=\"{N(opacity, 3)}\"" : "");
            body.Append($"  <path {attributes} d=\"{PathData(shape.Path)}\"/>\n");
        }

        svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(Size)}\" height=\"{N(Size)}\" viewBox=\"0 0 {N(Size)} {N(Size)}\">\n");
        svg.Append($"  <title>{title}</title>\n");
        if (comment is not null)
            svg.Append($"  <!-- {comment} -->\n");
        if (defs.Length > 0)
            svg.Append("  <defs>\n").Append(defs).Append("  </defs>\n");
        svg.Append(body);
        svg.Append("</svg>\n");
        return svg.ToString();
    }

    private static string PathData(SKPath path)
    {
        var data = new StringBuilder();
        using SKPath.RawIterator iterator = path.CreateRawIterator();
        var points = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            if (data.Length > 0)
                data.Append(' ');
            switch (verb)
            {
                case SKPathVerb.Move:
                    data.Append($"M{P(points[0])}");
                    break;
                case SKPathVerb.Line:
                    data.Append($"L{P(points[1])}");
                    break;
                case SKPathVerb.Quad:
                    data.Append($"Q{P(points[1])} {P(points[2])}");
                    break;
                case SKPathVerb.Cubic:
                    data.Append($"C{P(points[1])} {P(points[2])} {P(points[3])}");
                    break;
                case SKPathVerb.Close:
                    data.Append('Z');
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected {verb} in a path.");
            }
        }
        return data.ToString();
    }

    private static string P(SKPoint point) => $"{N(point.X)},{N(point.Y)}";

    private static string N(float value, int decimals = 2) =>
        Math.Round(value, decimals).ToString(CultureInfo.InvariantCulture);

    private static string Hex(SKColor color) => $"#{color.Red:x2}{color.Green:x2}{color.Blue:x2}";
}
