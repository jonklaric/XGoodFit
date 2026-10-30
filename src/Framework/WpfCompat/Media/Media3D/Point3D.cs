using System.Globalization;

namespace WpfCompat.Media.Media3D;

/// <summary>
/// WPF's <c>System.Windows.Media.Media3D.Point3D</c>: a point with X, Y and Z coordinates.
/// </summary>
/// <remarks>
/// Avalonia has no 3D points. Upstream's RMC-BestFit uses WPF's as plot data, an area series reading
/// its X, Y and Z (the second Y) through data field names, so the port keeps the type with WPF's
/// members: the coordinates, equality, and WPF's text form ("x,y,z", with the culture's list separator
/// when the decimal separator is a comma).
/// </remarks>
public struct Point3D : IEquatable<Point3D>
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
    public static bool operator ==(Point3D point1, Point3D point2) => point1.Equals(point2);

    /// <summary>Whether two points have different coordinates.</summary>
    public static bool operator !=(Point3D point1, Point3D point2) => !point1.Equals(point2);

    /// <inheritdoc/>
    public bool Equals(Point3D other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Point3D other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

    /// <summary>WPF's text form in the current culture.</summary>
    /// <returns>The coordinates, separated as WPF separates them.</returns>
    public override string ToString() => ToString(CultureInfo.CurrentCulture);

    /// <summary>WPF's text form: the coordinates separated by commas, or by ';' where the culture's decimal separator is a comma.</summary>
    /// <param name="provider">The culture.</param>
    /// <returns>The text.</returns>
    public string ToString(IFormatProvider provider)
    {
        var format = NumberFormatInfo.GetInstance(provider);
        char separator = format.NumberDecimalSeparator.Length > 0 && format.NumberDecimalSeparator[0] == ',' ? ';' : ',';
        return string.Format(provider, "{0}{3}{1}{3}{2}", X, Y, Z, separator);
    }
}
