#nullable enable
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Hec.Dss.Tests
{
  /// <summary>
  /// XGoodFit addition: RMC-BestFit's HEC-DSS import example (examples/1-time-series-data/5-hec-dss-import
  /// in sample-data/RMC-BestFit): its DSS file, and the two series its project saved when they were
  /// imported with RMC-BestFit's own build of the wrapper and HEC's 7-IU-6 native.
  /// </summary>
  internal static class BestFitExample
  {
    /// <summary>The example's folder.</summary>
    internal static readonly string Folder = Path.Combine(
      typeof(BestFitExample).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "BestFitExamplesDir").Value!,
      "1-time-series-data", "5-hec-dss-import");

    /// <summary>The example's DSS file.</summary>
    internal static string DssFile => Path.Combine(Folder, "dss-example-data.dss");

    /// <summary>The example's project, which holds the series imported from <see cref="DssFile"/>.</summary>
    internal static string ProjectFile => Path.Combine(Folder, "hec-dss-import-example.bestfit");

    /// <summary>Whether the example has been downloaded.</summary>
    internal static bool IsAvailable => File.Exists(DssFile) && File.Exists(ProjectFile);

    /// <summary>A time series the example project imported from the DSS file, as the project saved it.</summary>
    internal sealed record SavedSeries(string Name, string Pathname, string Units, string Interval, DateTime[] Times, double[] Values);

    /// <summary>
    /// Reads the saved series from the project's [Time Series Data] table, as RMC-BestFit stores them: the
    /// time series' XML, UTF-8 encoded and deflated (Numerics' Tools.Compress) in TimeSeriesCompressed.
    /// </summary>
    internal static IReadOnlyList<SavedSeries> ReadSavedSeries()
    {
      // A copy, because opening a project can change it (RMC-BestFit's connections switch to WAL).
      string copy = Path.Combine(Path.GetTempPath(), "xgoodfit-" + Guid.NewGuid().ToString("N") + ".bestfit");
      File.Copy(ProjectFile, copy);
      try
      {
        var series = new List<SavedSeries>();
        using (var connection = new SQLiteConnection("Data Source=" + copy + ";Pooling=False"))
        {
          connection.Open();
          using var command = new SQLiteCommand(
            "SELECT [Name], [HECDSSDataPathname], [UnitLabel], [TimeInterval], [TimeSeriesCompressed] " +
            "FROM [Time Series Data] WHERE [EntryMethod] = 'HECDSS' ORDER BY [Name]", connection);
          using SQLiteDataReader reader = command.ExecuteReader();
          while (reader.Read())
          {
            XElement xml = Inflate((byte[])reader["TimeSeriesCompressed"]);
            XElement[] ordinates = xml.Elements("SeriesOrdinate").ToArray();
            series.Add(new SavedSeries(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
              ordinates.Select(o => DateTime.Parse((string)o.Attribute("Index")!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)).ToArray(),
              ordinates.Select(o => double.Parse((string)o.Attribute("Value")!, CultureInfo.InvariantCulture)).ToArray()));
          }
        }
        return series;
      }
      finally
      {
        SQLiteConnection.ClearAllPools();
        File.Delete(copy);
      }
    }

    private static XElement Inflate(byte[] compressed)
    {
      using var input = new MemoryStream(compressed);
      using var inflater = new DeflateStream(input, CompressionMode.Decompress);
      return XElement.Load(inflater);
    }
  }
}
