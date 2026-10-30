using System;
using System.Runtime.InteropServices;
using DSSUnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// The native library keeps process-wide state (message level, program name), as RMC-BestFit's own DSS
// tests note, so tests run one at a time.
[assembly: DoNotParallelize]

namespace Hec.Dss.Tests
{
  /// <summary>
  /// XGoodFit addition: checks that the natives and the sample data are there before any test runs, and
  /// removes the run's temporary DSS files afterwards.
  /// </summary>
  [TestClass]
  public class TestSetup
  {
    /// <summary>The environment variable that turns missing natives or sample data into a failure.</summary>
    internal const string RequireVariable = "XGOODFIT_REQUIRE_HECDSS";

    /// <summary>
    /// Reports every test as inconclusive (skipped) when the natives or the sample data are missing, or
    /// fails them all when <see cref="RequireVariable"/> is 1.
    /// </summary>
    [AssemblyInitialize]
    public static void CheckPrerequisites(TestContext context)
    {
      string missing = null;
      try
      {
        Marshal.PtrToStringAnsi(DssNative.hec_dss_api_version());
      }
      catch (DllNotFoundException e)
      {
        missing = "the HEC-DSS native library (build it with native/build-hecdss.sh): " + e.Message;
      }
      if (missing == null && !TestUtility.HasTestData)
        missing = "HEC's sample DSS files (scripts/fetch-sample-data.sh dss-test-data)";
      if (missing == null && !BestFitExample.IsAvailable)
        missing = "RMC-BestFit's example projects (scripts/fetch-sample-data.sh RMC-BestFit)";
      if (missing == null)
        return;

      string message = "The HEC-DSS tests need " + missing;
      if (Environment.GetEnvironmentVariable(RequireVariable) == "1")
        Assert.Fail(message);
      Assert.Inconclusive(message);
    }

    /// <summary>Removes the temporary DSS files HEC's tests create.</summary>
    [AssemblyCleanup]
    public static void DeleteTemporaryFiles()
    {
      TestUtility.DeleteTemporaryFiles();
    }
  }
}
