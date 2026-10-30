using System.Globalization;
using WpfCompat.Media.Media3D;

namespace WpfCompat.Tests;

/// <summary>
/// WpfCompat's <see cref="Point3D"/> behaves as WPF's <c>System.Windows.Media.Media3D.Point3D</c> where
/// ported code relies on it: plot series read its coordinates as properties, by name.
/// </summary>
[TestClass]
public sealed class Point3DTests
{
    [TestMethod]
    public void Coordinates_ArePropertiesReadByName_AsPlotSeriesReadThem()
    {
        object point = new Point3D(1.5, 0, -2.25);
        Assert.AreEqual(1.5, point.GetType().GetProperty(nameof(Point3D.X))!.GetValue(point));
        Assert.AreEqual(0d, point.GetType().GetProperty(nameof(Point3D.Y))!.GetValue(point));
        Assert.AreEqual(-2.25, point.GetType().GetProperty(nameof(Point3D.Z))!.GetValue(point));
        Assert.AreEqual(default, new Point3D(), "the default point is the origin, as WPF's");
    }

    [TestMethod]
    public void Equality_ComparesTheCoordinates()
    {
        Assert.AreEqual(new Point3D(1, 2, 3), new Point3D(1, 2, 3));
        Assert.IsTrue(new Point3D(1, 2, 3) == new Point3D(1, 2, 3));
        Assert.IsTrue(new Point3D(1, 2, 3) != new Point3D(1, 2, 4));
        Assert.IsTrue(Point3D.Equals(new Point3D(0, 0, 1), new Point3D(0, 0, 1)));
        Assert.IsFalse(new Point3D(1, 2, 3).Equals((object)"1,2,3"));
        Assert.AreEqual(new Point3D(1, 2, 3).GetHashCode(), new Point3D(1, 2, 3).GetHashCode());
    }

    [TestMethod]
    public void ToString_WritesWpfsText_WithASemicolonWhereTheDecimalSeparatorIsAComma()
    {
        Assert.AreEqual("1.5,2,3", new Point3D(1.5, 2, 3).ToString(CultureInfo.InvariantCulture));
        Assert.AreEqual("1,5;2;3", new Point3D(1.5, 2, 3).ToString(new CultureInfo("de-DE")));
        Assert.AreEqual("1.50,2.00,3.00", new Point3D(1.5, 2, 3).ToString("F2", CultureInfo.InvariantCulture));
    }
}
