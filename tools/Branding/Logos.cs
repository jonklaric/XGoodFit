using SkiaSharp;
using static Branding.Curves;

namespace Branding;

/// <summary>
/// A logo, drawn for an image of a given size. Below 48 px a logo may draw itself bolder (thicker
/// strokes, whole-pixel margins) so it stays legible; from 48 px up it's the same drawing.
/// </summary>
internal sealed record Logo(string Name, string Title, string Idea, Func<int, Drawing> Draw);

/// <summary>
/// XGoodFit's logo concepts (branding/README.md). Each keeps RMC-BestFit's two overlapping densities
/// and its blue-to-red gradient, and adds an X its own way.
/// </summary>
internal static class Logos
{
    private static readonly SKColor Blue = SKColor.Parse("#2F6BEA");
    private static readonly SKColor Violet = SKColor.Parse("#7C4DEB");
    private static readonly SKColor Rose = SKColor.Parse("#E83A62");
    private static readonly SKColor Ink = SKColor.Parse("#1E1B4B");

    public static readonly Logo CrossedTails = new("a-crossed-tails", "Crossed tails",
        "Two skewed densities, mirrored: their long tails cross into the X.", DrawCrossedTails);

    public static readonly Logo XBehind = new("b-x-behind", "X behind the curves",
        "RMC-BestFit's pair of densities in front of a bold X, on a dark tile.", DrawXBehind);

    public static readonly Logo ExceedanceX = new("c-exceedance-x", "Exceedance X",
        "The non-exceedance and exceedance curves cross into the X, over RMC-BestFit's pair of densities, on a light tile.", DrawExceedanceX);

    public static readonly Logo[] Concepts = [CrossedTails, XBehind, ExceedanceX];

    /// <summary>The chosen concept: XGoodFit's logo and icon.</summary>
    public static readonly Logo Chosen = ExceedanceX;

    // A length in drawing units that's at least `pixels` wide in an image of `size` pixels.
    private static float AtLeast(float units, float pixels, int size) => MathF.Max(units, pixels * Drawing.Size / size);

    // The blue-to-red gradient, across from x0 to x1.
    private static Gradient Across(float x0, float x1) =>
        new(new SKPoint(x0, 0), new SKPoint(x1, 0), (0, Blue), (0.5f, Violet), (1, Rose));

    // The area under a curve from x0 to x1, closed along the baseline.
    private static SKPath Area(SKPath curve, float x0, float x1, float baseline)
    {
        var area = new SKPath(curve);
        area.LineTo(x1, baseline);
        area.LineTo(x0, baseline);
        area.Close();
        return area;
    }

    // A stroke's outline, with round caps and joins.
    private static SKPath Outline(SKPath path, float width)
    {
        using var paint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = width, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
        var outline = new SKPath();
        paint.GetFillPath(path, outline);
        return outline;
    }

    // Two strokes crossing, the second over the first with a gap around it.
    private static (SKPath Under, SKPath Over) OverUnder(SKPath under, SKPath over, float width, float gap) =>
        (Outline(under, width).Op(Outline(over, width + 2 * gap), SKPathOp.Difference) ?? throw new InvalidOperationException("Path operation failed."),
         Outline(over, width));

    // A tile: a rounded square inset by `margin`, snapped to whole pixels in an image of `size` pixels.
    private static SKPath Tile(int size, out float margin)
    {
        float pixel = Drawing.Size / size;
        margin = MathF.Max(1, MathF.Round(16 / pixel)) * pixel;
        float side = Drawing.Size - 2 * margin;
        return RoundedSquare(margin, margin, side, side * 0.225f);
    }

    private static Drawing DrawCrossedTails(int size)
    {
        // Two split normals, mirrored: a steep rise to the mode, a long tail across to the other side.
        const float baseline = 222, height = 176, mode = 62, rise = 19, tail = 60;
        float Left(float x) => baseline - height * Bell(x, mode, rise, tail);
        float Right(float x) => baseline - height * Bell(x, Drawing.Size - mode, tail, rise);
        float start = mode - 3 * rise, end = mode + 3 * tail;
        SKPath left = Area(Plot(Plot(new SKPath(), Left, start, mode, 6), Left, mode, end, 12), start, end, baseline);
        SKPath right = Area(Plot(Plot(new SKPath(), Right, Drawing.Size - end, Drawing.Size - mode, 12), Right, Drawing.Size - mode, Drawing.Size - start, 6),
            Drawing.Size - end, Drawing.Size - start, baseline);

        // The X: each curve's inner side, from just over its peak down its tail.
        float from = mode - 0.7f * rise, to = mode + 2.2f * tail;
        SKPath leftArm = Plot(new SKPath(), Left, from, to, 14);
        SKPath rightArm = Plot(new SKPath(), Right, Drawing.Size - to, Drawing.Size - from, 14);
        (SKPath under, SKPath over) = OverUnder(leftArm, rightArm, AtLeast(20, 2.2f, size), AtLeast(9, 1, size));

        Gradient Fade(SKColor color) =>
            new(new SKPoint(0, baseline - height), new SKPoint(0, baseline), (0, color.WithAlpha(170)), (1, color.WithAlpha(30)));
        return new Drawing().Fill(left, Fade(Blue)).Fill(right, Fade(Rose)).Fill(under, Blue).Fill(over, Rose);
    }

    // RMC-BestFit's arrangement: a lower density on the left, a taller one on the right, overlapping.
    private static (SKPath Left, SKPath Right) Pair(float baseline, float x0, float x1, float leftMode, float leftHeight, float rightMode, float rightHeight)
    {
        float Left(float x) => baseline - leftHeight * Bell(x, leftMode, 26, 36);
        float Right(float x) => baseline - rightHeight * Bell(x, rightMode, 30, 36);
        return (Area(Plot(new SKPath(), Left, x0, x1, 24), x0, x1, baseline), Area(Plot(new SKPath(), Right, x0, x1, 24), x0, x1, baseline));
    }

    private static Drawing DrawXBehind(int size)
    {
        SKPath tile = Tile(size, out float margin);
        var x = new SKPath();
        // Its lower arms end out of sight, behind the densities.
        x.MoveTo(66, 60);
        x.LineTo(172, 166);
        x.MoveTo(190, 60);
        x.LineTo(84, 166);
        // The densities rise from the tile's foot.
        float foot = Drawing.Size - margin;
        (SKPath left, SKPath right) = Pair(foot, margin, foot, 94, foot - 112, 160, foot - 70);
        left = Clip(left, tile);
        right = Clip(right, tile);
        SKPath overlap = Clip(left, right);
        return new Drawing()
            .Fill(tile, new Gradient(new SKPoint(0, margin), new SKPoint(0, foot), (0, SKColor.Parse("#312E81")), (1, Ink)))
            .Stroke(x, SKColor.Parse("#E0E7FF"), AtLeast(30, 3, size))
            .Fill(left, Blue).Fill(right, Rose).Fill(overlap, Violet);
    }

    private static Drawing DrawExceedanceX(int size)
    {
        SKPath tile = Tile(size, out float margin);
        // The densities rise from the tile's foot, their peaks between the X's arms.
        float foot = Drawing.Size - margin;
        (SKPath left, SKPath right) = Pair(foot, margin, foot, 88, foot - 128, 168, foot - 98);
        left = Clip(left, tile);
        right = Clip(right, tile);

        // The X: a distribution's non-exceedance probability (its CDF, rising) and its exceedance
        // probability (falling), crossing at the median.
        static float Cdf(float x) => 1 / (1 + MathF.Exp(-(x - 128) / 20));
        SKPath rising = Plot(new SKPath(), x => 200 - 144 * Cdf(x), 56, 200, 16);
        SKPath falling = Plot(new SKPath(), x => 56 + 144 * Cdf(x), 56, 200, 16);
        // At 16 px a gap would break the X up: the arms cross solid.
        (SKPath under, SKPath over) = OverUnder(falling, rising, AtLeast(20, 2, size), size <= 16 ? 0 : AtLeast(8, 1, size));

        float edge = AtLeast(2, 0.375f, size);
        return new Drawing()
            .Fill(tile, new Gradient(new SKPoint(0, margin), new SKPoint(0, Drawing.Size - margin), (0, SKColors.White), (1, SKColor.Parse("#ECEAFB"))))
            .Stroke(Inset(tile, edge / 2), Ink.WithAlpha(36), edge)
            .Fill(left, Across(margin, foot)).Fill(right, new Brush(SKColors.Black, Across(margin, foot), 0.8f))
            .Fill(under, Ink).Fill(over, Ink);
    }

    private static SKPath Clip(SKPath path, SKPath clip) =>
        path.Op(clip, SKPathOp.Intersect) ?? throw new InvalidOperationException("Path operation failed.");

    // A tile's outline moved in by `inset`, for an edge drawn inside it.
    private static SKPath Inset(SKPath tile, float inset)
    {
        SKRect bounds = tile.Bounds;
        return RoundedSquare(bounds.Left + inset, bounds.Top + inset, bounds.Width - 2 * inset, (bounds.Width - 2 * inset) * 0.225f);
    }
}
