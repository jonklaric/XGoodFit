#nullable enable
using System;
using DSSUnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hec.Dss.Tests
{
  /// <summary>
  /// XGoodFit addition: DSS 6 files, which neither 7-IU-6's API nor 7-JA-7 can read, are rejected with the
  /// same message as before (DssNative.hec_dss_open maps 7-JA-7's failure back to 7-IU-6's status).
  /// </summary>
  [TestClass]
  public class Dss6FileTests
  {
    /// <summary>RMC-BestFit shows this message when a DSS 6 file is chosen.</summary>
    [TestMethod]
    [DataRow("sample6.dss")]
    [DataRow("MoRiverObs.dss")]
    public void Dss6FilesAreRejectedAsBefore(string name)
    {
      string file = TestUtility.GetCopyForTesting(name);
      Assert.AreEqual(6, DssReader.GetVersion(file));
      var e = Assert.ThrowsException<Exception>(() => new DssReader(file, 0));
      Assert.AreEqual("This library only supports DSS version 7 files", e.Message);
    }

    /// <summary>Other failures keep 7-JA-7's status: a file that isn't a DSS file at all.</summary>
    [TestMethod]
    public void OtherOpenFailuresAreUnchanged()
    {
      string file = TestUtility.GetSimpleTempFileName(".dss");
      System.IO.File.WriteAllText(file, "This isn't a DSS file, but it's long enough to look like one could be." + new string('x', 4096));
      Assert.AreEqual(-1, DssReader.GetVersion(file));
      var e = Assert.ThrowsException<Exception>(() => new DssReader(file, 0));
      Assert.AreNotEqual("This library only supports DSS version 7 files", e.Message);
    }
  }
}
