#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hec.Dss.Tests
{
  /// <summary>
  /// XGoodFit addition: compares what XGoodFit's wrapper and 7-JA-7 native read with what RMC-BestFit 2's own
  /// build of the wrapper (2.0.3) and HEC's 7-IU-6 native read, from a release of RMC-BestFit for Windows:
  /// libraries/Hec.Dss.dll and runtimes/win-x64/native/hecdss.dll, loaded side by side in this process.
  /// Set XGOODFIT_BESTFIT_REFERENCE to the release's folder to run them. They're skipped otherwise, and on
  /// macOS and Linux, which RMC-BestFit doesn't support.
  /// </summary>
  /// <remarks>
  /// Two things are expected to differ, and are allowed:
  /// - Quality flags: for some records (in DSS-178.dss and version7AlbersGridsTimeSeries.dss) both
  ///   libraries return different flags on every run, so only their number is compared. RMC-BestFit
  ///   doesn't read them.
  /// - Units and data type: 7-IU-6 returns none for a series whose first records hold only missing values,
  ///   which HEC fixed in 7-JA-7 (DSS-255, hec-dss #343). Three series in CantSaveEdits4.dss.
  /// </remarks>
  [TestClass]
  public class ReferenceComparisonTests
  {
    /// <summary>The environment variable naming an RMC-BestFit 2 release folder for Windows.</summary>
    internal const string ReferenceVariable = "XGOODFIT_BESTFIT_REFERENCE";

    /// <summary>HEC's DSS 7 sample files with time series (HEC's DSS 6 samples can't be opened by either).</summary>
    private static readonly string[] HecSampleFiles =
    {
      "sample7.dss", "sample7_units_xyz.dss", "MoRiverObs7.dss", "MoRiverObs_TimeWindowTest7.dss", "rainfall7.dss",
      "Kinzua.dss", "examples-all-data-types.dss", "MathFunctions_1-2.dss", "CantSaveEdits4.dss", "DSS-178.dss",
      "OddNumberValues.dss", "PseudoEightHourIrregular.dss", "quality7.dss", "simpleQCT7.dss", "Test7b.dss",
      "VerticalDatumTest_v7.dss", "dot_net_AppendWithTimeGapRegularTimeSeries.dss", "version7AlbersGridsTimeSeries.dss",
      "versioned-rfc-ensembles.dss", "sample6.dss",
    };

    /// <summary>An RMC-BestFit release's wrapper and native library.</summary>
    internal sealed record Reference(string Wrapper, string NativeLibrary);

    private static Assembly? referenceWrapper;

    /// <summary>The release in <see cref="ReferenceVariable"/>; the calling test is inconclusive without one.</summary>
    internal static Reference RequireReference()
    {
      if (!OperatingSystem.IsWindows())
        Assert.Inconclusive("RMC-BestFit and HEC's 7-IU-6 hecdss.dll it ships are Windows-only.");
      string? folder = Environment.GetEnvironmentVariable(ReferenceVariable);
      if (string.IsNullOrEmpty(folder))
        Assert.Inconclusive($"Set {ReferenceVariable} to the folder of an RMC-BestFit 2 release for Windows to compare with it.");
      var reference = new Reference(Path.Combine(folder, "libraries", "Hec.Dss.dll"), Path.Combine(folder, "runtimes", "win-x64", "native", "hecdss.dll"));
      Assert.IsTrue(File.Exists(reference.Wrapper) && File.Exists(reference.NativeLibrary),
        $"{ReferenceVariable} ({folder}) should hold libraries/Hec.Dss.dll and runtimes/win-x64/native/hecdss.dll");
      return reference;
    }

    /// <summary>RMC-BestFit's example reads the same, including RMC-BestFit's block-by-block import.</summary>
    [TestMethod]
    public void RmcBestFitsExampleReadsTheSame()
    {
      Assembly wrapper = LoadReference();
      List<string> expected = Dump(file => Activator.CreateInstance(wrapper.GetType("Hec.Dss.DssReader", true)!, file, 0)!, BestFitExample.DssFile, blockReads: true);
      List<string> actual = Dump(file => new DssReader(file, 0), BestFitExample.DssFile, blockReads: true);
      // 967 records, 199 series, and the blocks of the 192 hourly series that aren't patterns.
      Assert.AreEqual(967, expected.Count(line => line.StartsWith("record ", StringComparison.Ordinal)));
      Assert.AreEqual(199, expected.Count(line => line.StartsWith("series ", StringComparison.Ordinal)));
      Assert.AreEqual(967 - 7, expected.Count(line => line.StartsWith("block ", StringComparison.Ordinal)));
      Comparison comparison = Compare(Path.GetFileName(BestFitExample.DssFile), expected, actual);
      Assert.IsNull(comparison.Differences, Environment.NewLine + comparison.Differences);
      Assert.AreEqual(0, comparison.UnitsFixedIn7JA7);
    }

    /// <summary>HEC's sample files with time series read the same.</summary>
    [TestMethod]
    public void HecsSampleFilesReadTheSame()
    {
      Assembly wrapper = LoadReference();
      var differences = new List<string>();
      int unitsFixed = 0;
      foreach (string name in HecSampleFiles)
      {
        string file = Path.Combine(DssTestData, name);
        List<string> expected = Dump(path => Activator.CreateInstance(wrapper.GetType("Hec.Dss.DssReader", true)!, path, 0)!, file, blockReads: false);
        List<string> actual = Dump(path => new DssReader(path, 0), file, blockReads: false);
        Comparison comparison = Compare(name, expected, actual);
        if (comparison.Differences != null)
          differences.Add(comparison.Differences);
        unitsFixed += comparison.UnitsFixedIn7JA7;
      }
      Assert.AreEqual(0, differences.Count, Environment.NewLine + string.Join(Environment.NewLine, differences));
      Assert.AreEqual(3, unitsFixed, "series whose units only 7-JA-7 reads (DSS-255)");
    }

    private static string DssTestData => typeof(ReferenceComparisonTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
      .Single(a => a.Key == "DssTestDataDir").Value!;

    /// <summary>
    /// Loads RMC-BestFit's Hec.Dss.dll into a load context of its own, bound to its own hecdss.dll, beside
    /// XGoodFit's wrapper and native library.
    /// </summary>
    private static Assembly LoadReference()
    {
      Reference reference = RequireReference();
      if (referenceWrapper == null)
      {
        var context = new ReferenceContext(reference);
        referenceWrapper = context.LoadFromAssemblyPath(reference.Wrapper);
      }
      // Make sure the two really are different libraries.
      IntPtr native = NativeLibrary.Load(reference.NativeLibrary);
      try
      {
        Assert.AreNotEqual(DssNative.Library == IntPtr.Zero ? NativeLibraryTests.LoadBundledLibrary() : DssNative.Library, native);
        Assert.IsNull(DssNative.ReadApiVersion(native), "The reference should be HEC's 7-IU-6, which has no hec_dss_api_version");
      }
      finally
      {
        NativeLibrary.Free(native);
      }
      return referenceWrapper;
    }

    /// <summary>
    /// Lines describing what a wrapper reads from a copy of the file: each record, and each time series
    /// (all of it, and with <paramref name="blockReads"/> each hourly block as RMC-BestFit's import reads it).
    /// </summary>
    private static List<string> Dump(Func<string, dynamic> open, string file, bool blockReads)
    {
      var lines = new List<string>();
      string copy = Path.Combine(Path.GetTempPath(), "xgoodfit-" + Guid.NewGuid().ToString("N") + ".dss");
      File.Copy(file, copy);
      try
      {
        dynamic reader;
        try
        {
          reader = open(copy);
        }
        catch (Exception e)
        {
          lines.Add("open failed: " + (e is TargetInvocationException && e.InnerException != null ? e.InnerException.Message : e.Message));
          return lines;
        }
        using ((IDisposable)reader)
        {
          dynamic catalog = reader.GetCatalog();
          foreach (dynamic path in catalog.UnCondensedPaths)
            lines.Add("record " + path.FullPath + " " + path.RecordType.ToString());
          foreach (dynamic path in catalog)
          {
            string type = path.RecordType.ToString();
            if (type != "RegularTimeSeries" && type != "IrregularTimeSeries")
              continue;
            lines.Add(Describe("series " + path.FullPath, reader.GetTimeSeries(path)));
            // RMC-BestFit imports regular series block by block; the example's are hourly, in monthly
            // blocks, besides unit-graph patterns, which have no dates.
            if (!blockReads || !path.IsRegular || path.Epart != "1Hour" || path.Dpart == "TS-PATTERN")
              continue;
            string dateless = path.PathWithoutDate;
            foreach (dynamic block in ((IEnumerable<dynamic>)Enumerable.Cast<dynamic>(catalog.UnCondensedPaths))
              .Where(block => (string)block.PathWithoutDate == dateless).OrderBy(block => (DateTime)block.DPartAsDateTime))
            {
              DateTime start = block.DPartAsDateTime;
              lines.Add(Describe("block " + block.FullPath, reader.GetTimeSeries(block, start.AddSeconds(1), start.AddMonths(1))));
            }
          }
        }
      }
      finally
      {
        try
        {
          File.Delete(copy);
        }
        catch (IOException)
        {
        }
      }
      return lines;
    }

    private static string Describe(string label, dynamic series)
    {
      DateTime[]? times = series.Times;
      double[]? values = series.Values;
      int[]? qualities = series.Qualities;
      using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
      foreach (DateTime time in times ?? Array.Empty<DateTime>())
        hash.AppendData(BitConverter.GetBytes(time.Ticks));
      foreach (double value in values ?? Array.Empty<double>())
        hash.AppendData(BitConverter.GetBytes(BitConverter.DoubleToInt64Bits(value)));
      dynamic location = series.LocationInformation;
      string where = location == null ? "none" :
        $"{(double)location.XOrdinate:R} {(double)location.YOrdinate:R} {(double)location.ZOrdiante:R} {location.CoordinateSystem} {location.CoordinateID} " +
        $"{location.HorizontalUnits} {location.HorizontalDatum} {location.VerticalUnits} {location.VerticalDatum} '{location.TimeZoneName}' '{location.Supplemental}'";
      return $"{label}: n={values?.Length} quality={qualities?.Length} units='{series.Units}' type='{series.DataType}' " +
        $"first={times?.FirstOrDefault():o} last={times?.LastOrDefault():o} sha256={Convert.ToHexString(hash.GetHashAndReset())[..16]} location={where}";
    }

    /// <summary>The unexpected differences (null if none), and how many series only differ by DSS-255's fix.</summary>
    private sealed record Comparison(string? Differences, int UnitsFixedIn7JA7);

    private static Comparison Compare(string name, List<string> expected, List<string> actual)
    {
      var differences = new List<string>();
      int unitsFixed = 0;
      for (int i = 0; i < Math.Max(expected.Count, actual.Count); i++)
      {
        string? left = i < expected.Count ? expected[i] : null;
        string? right = i < actual.Count ? actual[i] : null;
        if (left == right)
          continue;
        if (left != null && right != null && left.Contains(" units='' type='' ", StringComparison.Ordinal) &&
          Regex.Replace(right, " units='[^']*' type='[^']*' ", " units='' type='' ") == left)
        {
          unitsFixed++;
          continue;
        }
        if (differences.Count < 10)
          differences.Add($"  RMC-BestFit: {left ?? "(nothing)"}{Environment.NewLine}  XGoodFit:    {right ?? "(nothing)"}");
      }
      return new Comparison(differences.Count == 0 ? null :
        $"{name} ({expected.Count} and {actual.Count} lines):{Environment.NewLine}{string.Join(Environment.NewLine, differences)}", unitsFixed);
    }

    /// <summary>Resolves Hec.Dss to RMC-BestFit's build, and its "hecdss" to HEC's 7-IU-6 library.</summary>
    private sealed class ReferenceContext : AssemblyLoadContext
    {
      private readonly Reference reference;

      internal ReferenceContext(Reference reference) : base("RMC-BestFit's Hec.Dss")
      {
        this.reference = reference;
      }

      protected override Assembly? Load(AssemblyName assemblyName) =>
        assemblyName.Name == "Hec.Dss" ? LoadFromAssemblyPath(reference.Wrapper) : null;

      protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) =>
        unmanagedDllName == "hecdss" ? LoadUnmanagedDllFromPath(reference.NativeLibrary) : IntPtr.Zero;
    }
  }
}
