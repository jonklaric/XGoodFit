using SkiaSharp;

namespace Branding;

/// <summary>The shapes the logos are made of.</summary>
internal static class Curves
{
    /// <summary>
    /// The height (0 to 1) at <paramref name="x"/> of a skewed bell curve, a probability density: a normal
    /// curve with a different spread on each side of its mode (a split normal).
    /// </summary>
    public static float Bell(float x, float mode, float left, float right)
    {
        float z = (x - mode) / (x < mode ? left : right);
        return MathF.Exp(-0.5f * z * z);
    }

    /// <summary>
    /// Adds the graph of <paramref name="y"/> from <paramref name="x0"/> to <paramref name="x1"/> to a path,
    /// as cubic curves through <paramref name="segments"/> + 1 points with y's slopes there.
    /// </summary>
    public static SKPath Plot(SKPath path, Func<float, float> y, float x0, float x1, int segments)
    {
        const float h = 0.01f;
        float Slope(float x) => (y(x + h) - y(x - h)) / (2 * h);
        float step = (x1 - x0) / segments;
        float xa = x0, ya = y(xa), sa = Slope(xa);
        if (path.PointCount == 0)
            path.MoveTo(xa, ya);
        else
            path.LineTo(xa, ya);
        for (int i = 1; i <= segments; i++)
        {
            float xb = i == segments ? x1 : x0 + i * step, yb = y(xb), sb = Slope(xb);
            path.CubicTo(xa + step / 3, ya + sa * step / 3, xb - step / 3, yb - sb * step / 3, xb, yb);
            (xa, ya, sa) = (xb, yb, sb);
        }
        return path;
    }

    /// <summary>A rounded square, its corners cubic curves (no arcs, which SVG and Skia draw differently).</summary>
    public static SKPath RoundedSquare(float left, float top, float size, float radius)
    {
        // A quarter circle's control points are 0.5523 of its radius from its ends.
        float k = radius * (1 - 0.5523f), right = left + size, bottom = top + size;
        var path = new SKPath();
        path.MoveTo(left + radius, top);
        path.LineTo(right - radius, top);
        path.CubicTo(right - k, top, right, top + k, right, top + radius);
        path.LineTo(right, bottom - radius);
        path.CubicTo(right, bottom - k, right - k, bottom, right - radius, bottom);
        path.LineTo(left + radius, bottom);
        path.CubicTo(left + k, bottom, left, bottom - k, left, bottom - radius);
        path.LineTo(left, top + radius);
        path.CubicTo(left, top + k, left + k, top, left + radius, top);
        path.Close();
        return path;
    }
}
