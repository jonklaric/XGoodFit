using System.Globalization;
using WpfCompat.Media.Media3D;

namespace WpfCompat.Tests;

/// <summary>WpfCompat's Point3D, which BestFit's area series read through data field names.</summary>
[TestClass]
public sealed class Point3DTests
{
    [TestMethod]
    public void Point3D_HasWpfsCoordinatesEqualityAndText()
    {
        var point = new Point3D(1.5, 0, -2.25);
        Assert.AreEqual(1.5, point.X);
        Assert.AreEqual(0, point.Y);
        Assert.AreEqual(-2.25, point.Z);
        Assert.AreEqual(new Point3D(1.5, 0, -2.25), point);
        Assert.IsTrue(point != new Point3D(1.5, 0, 2.25));
        Assert.AreEqual("1.5,0,-2.25", point.ToString(CultureInfo.InvariantCulture));
        Assert.AreEqual("1,5;0;-2,25", point.ToString(new CultureInfo("de-DE")), "WPF separates with ';' where the decimal separator is a comma");

        // Data field names read the coordinates by reflection, as OxyPlot's series do.
        Assert.AreEqual(-2.25, typeof(Point3D).GetProperty(nameof(Point3D.Z))!.GetValue(point));
    }
}
