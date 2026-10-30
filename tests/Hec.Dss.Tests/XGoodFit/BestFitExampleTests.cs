#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hec.Dss.Tests
{
  /// <summary>
  /// XGoodFit addition: reads RMC-BestFit's HEC-DSS import example (dss-example-data.dss) as RMC-BestFit
  /// does (catalogue, pathnames, time series), and compares the series with the ones the example project
  /// saved when RMC-BestFit imported them with its own wrapper build and HEC's 7-IU-6 native.
  /// </summary>
  [TestClass]
  public class BestFitExampleTests
  {
    private static string? copy;

    /// <summary>Reads a copy, as HEC's tests do: opening a DSS file can update it.</summary>
    [ClassInitialize]
    public static void CopyExample(TestContext context)
    {
      copy = Path.Combine(Path.GetTempPath(), "xgoodfit-" + Guid.NewGuid().ToString("N") + ".dss");
      File.Copy(BestFitExample.DssFile, copy);
    }

    /// <summary>Deletes the copy.</summary>
    [ClassCleanup]
    public static void DeleteCopy()
    {
      if (copy != null && File.Exists(copy))
        File.Delete(copy);
    }

    /// <summary>
    /// RMC.BestFit.UI's own declaration (DssTimeSeriesReader.NativeMethods), from another assembly than the
    /// wrapper, as RMC-BestFit uses it to find a weekly series' first value.
    /// </summary>
    [DllImport("hecdss", EntryPoint = "hec_dss_tsGetDateTimeRange", CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern int GetTimeSeriesDateRange(IntPtr dss, string pathname, int fullSet,
      ref int firstJulian, ref int firstSeconds, ref int lastJulian, ref int lastSeconds);

    private static DssReader OpenExample() => new DssReader(copy!, 0);

    /// <summary>The catalogue: every record, condensed into series the pathname selector lists.</summary>
    [TestMethod]
    public void TheCatalogueListsEveryRecord()
    {
      using DssReader reader = OpenExample();
      DssPathCollection catalog = reader.GetCatalog();
      Assert.AreEqual(967, catalog.UnCondensedPaths.Count);
      Assert.AreEqual(199, catalog.Count);
      Assert.IsTrue(catalog.UnCondensedPaths.All(path => path.RecordType == RecordType.RegularTimeSeries));
      // 192 series stored in monthly blocks, and 7 unit-graph patterns. The wrapper takes every D part
      // for a range, the patterns' too ("TS-PATTERN" has a hyphen).
      Assert.AreEqual(192, catalog.Count(path => Regex.IsMatch(path.Dpart, @"^\d\d[A-Z][a-z]{2}\d{4}-\d\d[A-Z][a-z]{2}\d{4}$")));
      Assert.AreEqual(7, catalog.Count(path => path.Dpart == "TS-PATTERN"));
      Assert.IsTrue(catalog.All(path => path.IsDPartARange()));

      foreach (BestFitExample.SavedSeries saved in BestFitExample.ReadSavedSeries())
      {
        var savedPath = new DssPath(saved.Pathname);
        DssPath series = catalog.Single(path => DssPath.EqualsDateLess(path, savedPath));
        Assert.AreEqual("01Mar2007-01Jul2007", series.Dpart, saved.Name);
        CollectionAssert.AreEqual(
          new[] { "01Mar2007", "01Apr2007", "01May2007", "01Jun2007", "01Jul2007" },
          Blocks(catalog, savedPath).Select(block => block.Dpart).ToArray(), saved.Name);
      }
    }

    /// <summary>The project's pathnames parse as RMC-BestFit uses them.</summary>
    [TestMethod]
    public void TheSavedPathnamesParse()
    {
      IReadOnlyList<BestFitExample.SavedSeries> saved = BestFitExample.ReadSavedSeries();
      CollectionAssert.AreEqual(new[] { "Grapevine Dam - Inflow", "Grapevine Dam - Outflow" }, saved.Select(s => s.Name).ToArray());
      CollectionAssert.AreEqual(new[] { "//GRAPEVINE INFLOW/FLOW//1Hour/RUN:2007_MAR-JUL/", "//GRAPEVINE LAKE-RELEASE/FLOW//1Hour/RUN:2007_MAR-JUL/" },
        saved.Select(s => s.Pathname).ToArray());

      var path = new DssPath(saved[0].Pathname);
      Assert.AreEqual("", path.Apart);
      Assert.AreEqual("GRAPEVINE INFLOW", path.Bpart);
      Assert.AreEqual("FLOW", path.Cpart);
      Assert.AreEqual("", path.Dpart);
      Assert.AreEqual("1Hour", path.Epart);
      Assert.AreEqual("RUN:2007_MAR-JUL", path.Fpart);
      Assert.IsTrue(path.IsRegular);
      Assert.IsFalse(path.IsDPartARange());
      Assert.AreEqual(saved[0].Pathname, path.PathWithoutDate);

      var block = new DssPath("//GRAPEVINE INFLOW/FLOW/01Apr2007/1Hour/RUN:2007_MAR-JUL/");
      Assert.IsTrue(block.TryParseDPartAsDateTime(out DateTime start));
      Assert.AreEqual(new DateTime(2007, 4, 1), start);
      Assert.AreEqual(new DateTime(2007, 4, 1), block.DPartAsDateTime);
      Assert.IsTrue(DssPath.EqualsDateLess(block, path));
    }

    /// <summary>Reading a whole series (the pathname selector's range check) gives what the project saved.</summary>
    [TestMethod]
    public void WholeSeriesReadsMatchTheSavedProject()
    {
      using DssReader reader = OpenExample();
      foreach (BestFitExample.SavedSeries saved in BestFitExample.ReadSavedSeries())
      {
        TimeSeries series = reader.GetTimeSeries(new DssPath(saved.Pathname));
        AssertMatchesSaved(saved, series.Times, series.Values);
        Assert.AreEqual("CFS", series.Units, saved.Name);
        Assert.AreEqual(saved.Units, series.Units.Trim(), saved.Name);
        Assert.AreEqual("INST-VAL", series.DataType, saved.Name);
        Assert.AreEqual("OneHour", saved.Interval, saved.Name);
        Assert.IsNotNull(series.LocationInformation, saved.Name);
      }
    }

    /// <summary>
    /// Reading each storage block, as RMC-BestFit's import does (DssTimeSeriesReader.ReadBlocks), gives what
    /// the project saved.
    /// </summary>
    [TestMethod]
    public void BlockReadsMatchTheSavedProject()
    {
      using DssReader reader = OpenExample();
      foreach (BestFitExample.SavedSeries saved in BestFitExample.ReadSavedSeries())
      {
        var times = new List<DateTime>();
        var values = new List<double>();
        foreach (DssPath block in Blocks(reader.GetCatalog(), new DssPath(saved.Pathname)))
        {
          Assert.IsTrue(block.TryParseDPartAsDateTime(out DateTime start));
          // DssTimeSeriesReader: the block's own midnight belongs to the previous block, and hourly
          // data is stored in monthly blocks.
          TimeSeries chunk = reader.GetTimeSeries(block, start.AddSeconds(1), start.AddMonths(1),
            TimeWindow.TimeWindowBehavior.StrictlyInclusive, TimeWindow.ConsecutiveValueCompression.None);
          Assert.IsNotNull(chunk.LocationInformation, "a failed native read: " + block.FullPath);
          Assert.AreEqual("CFS", chunk.Units, block.FullPath);
          Assert.AreEqual("INST-VAL", chunk.DataType, block.FullPath);
          times.AddRange(chunk.Times);
          values.AddRange(chunk.Values);
        }
        AssertMatchesSaved(saved, times.ToArray(), values.ToArray());
      }
    }

    /// <summary>The native date range of a series and of one block, read through another assembly's declaration.</summary>
    [TestMethod]
    public void DateRangesMatchTheSavedProject()
    {
      using var reader = new HandleReader(copy!);
      foreach (BestFitExample.SavedSeries saved in BestFitExample.ReadSavedSeries())
      {
        int firstJulian = 0, firstSeconds = 0, lastJulian = 0, lastSeconds = 0;
        Assert.AreEqual(0, GetTimeSeriesDateRange(reader.Handle, saved.Pathname, 1, ref firstJulian, ref firstSeconds, ref lastJulian, ref lastSeconds));
        Assert.AreEqual(saved.Times.First(), FromHec(firstJulian, firstSeconds), saved.Name);
        Assert.AreEqual(saved.Times.Last(), FromHec(lastJulian, lastSeconds), saved.Name);

        // One record only, as RMC-BestFit's DssRecordRangeReader asks.
        string march = saved.Pathname.Replace("//1Hour/", "/01Mar2007/1Hour/");
        Assert.AreEqual(0, GetTimeSeriesDateRange(reader.Handle, march, 0, ref firstJulian, ref firstSeconds, ref lastJulian, ref lastSeconds));
        Assert.AreEqual(saved.Times.First(), FromHec(firstJulian, firstSeconds), saved.Name);
        Assert.AreEqual(new DateTime(2007, 4, 1), FromHec(lastJulian, lastSeconds), saved.Name);
      }
    }

    /// <summary>The storage blocks of a series, in date order, as RMC-BestFit's import finds them.</summary>
    private static List<DssPath> Blocks(DssPathCollection catalog, DssPath series) => catalog.UnCondensedPaths
      .Where(block => DssPath.EqualsDateLess(block, series)).OrderBy(block => block.DPartAsDateTime).ToList();

    /// <summary>HEC's Julian date (days since 31 Dec 1899) and seconds into the day, which may be 86,400.</summary>
    private static DateTime FromHec(int julian, int seconds) => DateTime.FromOADate(julian + 1d).AddSeconds(seconds);

    private static void AssertMatchesSaved(BestFitExample.SavedSeries saved, DateTime[] times, double[] values)
    {
      // The project saved 2,785 hourly values, 16 March to 10 July 2007, with nothing missing.
      Assert.AreEqual(2785, saved.Times.Length, saved.Name);
      Assert.AreEqual(new DateTime(2007, 3, 16), saved.Times.First(), saved.Name);
      Assert.AreEqual(new DateTime(2007, 7, 10), saved.Times.Last(), saved.Name);
      CollectionAssert.AreEqual(saved.Times, times, saved.Name + ": times");
      Assert.AreEqual(saved.Values.Length, values.Length, saved.Name);
      for (int i = 0; i < values.Length; i++)
      {
        // Bit for bit: RMC-BestFit saves values in round-trip format, and INST-VAL values unchanged.
        if (BitConverter.DoubleToInt64Bits(saved.Values[i]) != BitConverter.DoubleToInt64Bits(values[i]))
          Assert.Fail($"{saved.Name}: value {i} at {times[i]:s} is {values[i]:R}; the project saved {saved.Values[i]:R}");
      }
    }

    /// <summary>A reader whose native handle RMC-BestFit's own declaration can use, as DssRecordRangeReader does.</summary>
    private sealed class HandleReader : DssReader
    {
      internal HandleReader(string filename) : base(filename, 0) { }

      internal IntPtr Handle => dss;
    }
  }
}
