#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hec.Dss.Tests
{
  /// <summary>
  /// XGoodFit addition: the native library XGoodFit ships, and the check in DssNative.cs that rejects any
  /// other version.
  /// </summary>
  [TestClass]
  public class NativeLibraryTests
  {
    /// <summary>
    /// hecdss as another assembly declares it: RMC.BestFit.UI's DssTimeSeriesReader calls
    /// hec_dss_tsGetDateTimeRange through its own DllImport.
    /// </summary>
    [DllImport("hecdss")]
    private static extern int hec_dss_CONSTANT_MAX_PATH_SIZE();

    /// <summary>Loads (again) the library src/Hec.Dss ships for this process's platform.</summary>
    internal static IntPtr LoadBundledLibrary()
    {
      string? path = DssNative.BundledLibraryPaths(typeof(DssNative).Assembly).FirstOrDefault(File.Exists);
      Assert.IsNotNull(path, "No library in runtimes/" + DssNative.RuntimeIdentifier + "/native beside Hec.Dss.dll");
      return NativeLibrary.Load(path);
    }

    /// <summary>
    /// The wrapper uses the library built for this platform, from runtimes/&lt;rid&gt;/native: nothing else
    /// in the test output is called hecdss.
    /// </summary>
    [TestMethod]
    public void TheWrapperLoadsTheLibraryBuiltForThisPlatform()
    {
      CollectionAssert.Contains(new[] { "win-x64", "linux-x64", "osx-arm64", "osx-x64" }, DssNative.RuntimeIdentifier);
      string folder = Path.GetDirectoryName(typeof(DssNative).Assembly.Location)!;
      Assert.IsFalse(File.Exists(Path.Combine(folder, DssNative.LibraryFileName)), "The library should only be in runtimes/<rid>/native");

      Assert.AreEqual(DssNative.ExpectedApiVersion, Marshal.PtrToStringAnsi(DssNative.hec_dss_api_version()));
      // Loading the same file again returns the module the wrapper is bound to.
      IntPtr bundled = LoadBundledLibrary();
      Assert.AreEqual(DssNative.Library, bundled);
      Assert.AreEqual(DssNative.ExpectedApiVersion, DssNative.ReadApiVersion(bundled));
    }

    /// <summary>A library without hec_dss_api_version (such as HEC's 7-IU-6 and earlier) is rejected.</summary>
    [TestMethod]
    public void ALibraryWithoutApiVersionIsRejected()
    {
      string system = OperatingSystem.IsWindows() ? "kernel32.dll" : OperatingSystem.IsMacOS() ? "/usr/lib/libSystem.B.dylib" : "libc.so.6";
      IntPtr library = NativeLibrary.Load(system);
      try
      {
        Assert.IsNull(DssNative.ReadApiVersion(library));
        var e = Assert.ThrowsException<DllNotFoundException>(() => DssNative.CheckApiVersion(library, system));
        StringAssert.Contains(e.Message, "predates HEC-DSS 7-JA-7");
      }
      finally
      {
        NativeLibrary.Free(library);
      }
    }

    /// <summary>The library the wrapper checked also answers other assemblies' DllImport("hecdss").</summary>
    [TestMethod]
    public void OtherAssembliesGetTheSameLibrary()
    {
      int expected = DssNative.hec_dss_CONSTANT_MAX_PATH_SIZE();
      Assert.IsTrue(expected > 0);
      Assert.AreEqual(expected, hec_dss_CONSTANT_MAX_PATH_SIZE());
    }

    /// <summary>
    /// HEC's 7-IU-6 hecdss.dll, which RMC-BestFit 2.0 ships and wrapper 2.0.3 was written for, is rejected
    /// (Windows, with an RMC-BestFit release in <see cref="ReferenceComparisonTests.ReferenceVariable"/>).
    /// </summary>
    [TestMethod]
    public void HecsRelease7IU6IsRejected()
    {
      string native = ReferenceComparisonTests.RequireReference().NativeLibrary;
      IntPtr library = NativeLibrary.Load(native);
      try
      {
        Assert.IsNull(DssNative.ReadApiVersion(library));
        Assert.ThrowsException<DllNotFoundException>(() => DssNative.CheckApiVersion(library, native));
      }
      finally
      {
        NativeLibrary.Free(library);
      }
    }
  }
}
