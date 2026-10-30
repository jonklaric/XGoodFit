using System.Globalization;

namespace WpfCompat.Media.Media3D;

/// <summary>
/// WPF's <c>System.Windows.Media.Media3D.Point3D</c>: a point with X, Y and Z coordinates. Avalonia has no
/// 3-D point.
/// </summary>
/// <remarks>
/// XGoodFit addition. Upstream's analysis views use WPF's point as a plot series' items: an area series reads
/// its two bounds from properties it names (<c>DataFieldY = nameof(Point3D.Y)</c>,
/// <c>DataFieldY2 = nameof(Point3D.Z)</c>), so the coordinates are properties, as WPF's are. Equality and the
/// text form are WPF's too.
/// </remarks>
public struct Point3D : IEquatable<Point3D>, IFormattable
{
    /// <summary>Creates a point.</summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    public Point3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>The X coordinate.</summary>
    public double X { get; set; }

    /// <summary>The Y coordinate.</summary>
    public double Y { get; set; }

    /// <summary>The Z coordinate.</summary>
    public double Z { get; set; }

    /// <summary>Whether two points have the same coordinates.</summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <returns>True if they're equal.</returns>
    public static bool operator ==(Point3D point1, Point3D point2) => point1.Equals(point2);

    /// <summary>Whether two points have different coordinates.</summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <returns>True if they differ.</returns>
    public static bool operator !=(Point3D point1, Point3D point2) => !point1.Equals(point2);

    /// <summary>Whether two points have the same coordinates.</summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <returns>True if they're equal.</returns>
    public static bool Equals(Point3D point1, Point3D point2) => point1.Equals(point2);

    /// <inheritdoc/>
    public bool Equals(Point3D value) => X.Equals(value.X) && Y.Equals(value.Y) && Z.Equals(value.Z);

    /// <inheritdoc/>
    public override bool Equals(object? o) => o is Point3D point && Equals(point);

    /// <inheritdoc/>
    public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

    /// <summary>The point as WPF writes it: "X,Y,Z", in the current culture (with ";" where its decimal separator is ",").</summary>
    /// <returns>The text.</returns>
    public override string ToString() => ToString(null, null);

    /// <summary>The point as WPF writes it, in the given culture.</summary>
    /// <param name="provider">The culture, or null for the current culture.</param>
    /// <returns>The text.</returns>
    public string ToString(IFormatProvider? provider) => ToString(null, provider);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? provider)
    {
        // WPF's TokenizerHelper.GetNumericListSeparator: ';' when the decimal separator is ','.
        NumberFormatInfo numbers = NumberFormatInfo.GetInstance(provider);
        char separator = numbers.NumberDecimalSeparator.Length > 0 && numbers.NumberDecimalSeparator[0] == ',' ? ';' : ',';
        return string.Format(provider, "{1:" + format + "}{0}{2:" + format + "}{0}{3:" + format + "}", separator, X, Y, Z);
    }
}
