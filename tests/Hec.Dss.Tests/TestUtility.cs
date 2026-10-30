using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// XGoodFit port: for the test data folder recorded at build time.
using System.Reflection;

namespace DSSUnitTests
{
  public class TestUtility
  {

    // XGoodFit port: HEC's sample DSS files are in sample-data/dss-test-data (scripts/fetch-sample-data.sh),
    // recorded by the project file, instead of a dss-test-data clone six folders above the test output.
    private static readonly string BasePath = typeof(TestUtility).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
      .Single(attribute => attribute.Key == "DssTestDataDir").Value;

    // XGoodFit port: the copies and new files go in one temporary folder per test run, which
    // DeleteTemporaryFiles removes afterwards. Upstream left them in the temporary folder itself.
    private const string TempFolderPrefix = "xgoodfit-hecdss-tests-";
    private static readonly Lazy<string> TempPath = new Lazy<string>(() =>
      Directory.CreateTempSubdirectory(TempFolderPrefix).FullName);

    /// <summary>XGoodFit port: whether HEC's sample DSS files have been downloaded.</summary>
    internal static bool HasTestData => File.Exists(Path.Combine(BasePath, "sample7.dss"));

    /// <summary>
    /// XGoodFit port: removes this run's temporary files, and earlier runs' folders. Some of HEC's tests
    /// never close their files, which Windows then can't delete until the test process has ended.
    /// </summary>
    internal static void DeleteTemporaryFiles()
    {
      if (TempPath.IsValueCreated)
        TryDeleteFolder(TempPath.Value);
      foreach (string folder in Directory.GetDirectories(Path.GetTempPath(), TempFolderPrefix + "*"))
      {
        // An hour on, a folder can't belong to a run still going.
        if (Directory.GetLastWriteTimeUtc(folder) < DateTime.UtcNow.AddHours(-1))
          TryDeleteFolder(folder);
      }
    }

    private static void TryDeleteFolder(string folder)
    {
      try
      {
        Directory.Delete(folder, recursive: true);
      }
      catch (IOException)
      {
      }
      catch (UnauthorizedAccessException)
      {
      }
    }

    internal static string[] GetAllTestDssFiles()
    {
     return  Directory.GetFiles(TestUtility.BasePath, "*.dss", SearchOption.AllDirectories);
    }
    /// <summary>
    /// Gets path to a temporary file (that does not exist)
    /// </summary>
    /// <param name="extension">including the .</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static string GetSimpleTempFileName(string extension)
    {

      string path = TempPath.Value;

      var part1 = Guid.NewGuid().ToString("N").Substring(0, 8).ToLower();
      var fn = Path.Combine(path, part1 + extension);

      if (File.Exists(fn))
        throw new Exception("File allready exists");

      return fn;
    }

   // [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
    internal static string GetCopyForTesting(string fileName)
    {
      string tempFileName = GetSimpleTempFileName(".dss");
      // XGoodFit port: some tests name files with Windows separators (benchmarks6\BaldEDmbrk7.dss).
      string fn = Path.Combine(BasePath, fileName.Replace('\\', '/'));
      //Console.WriteLine("copying "+fn+"  to "+tempFileName);
      File.Copy(fn, tempFileName);

      return tempFileName;
    }
  }
}
