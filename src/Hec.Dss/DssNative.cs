using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
// XGoodFit port: for loading and checking the native library (end of this file).
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

// XGoodFit port: these declarations match HEC-DSS 7-JA-7 (upstream/hec-dss), which XGoodFit builds
// from source for every platform (native/). Wrapper 2.0.3 was written for HEC's prebuilt 7-IU-6, and
// six of its functions have changed since (hec-dss #271 and #280 in March and April 2025, #341 in
// November 2025):
//   - hec_dss_tsRetrieve and hec_dss_pdRetrieve gained a time-zone output buffer and its length;
//   - hec_dss_tsStoreRegular and hec_dss_tsStoreIregular gained a time zone and a storage flag;
//   - hec_dss_pdStore gained a time zone;
//   - hec_dss_gridStore gained compressionSize, in the middle, after isTimeStamped.
// Upstream's declarations would pass too few arguments (or, for hec_dss_gridStore, shifted ones), and
// the reads would write the record's time zone through a leftover register or stack value. So each of
// the six is declared with 7-JA-7's parameters, and upstream's signature stays as a managed overload
// that calls it as 7-IU-6 behaved. Every other wrapper file compiles unchanged. The other declarations
// match 7-JA-7 parameter for parameter (tests/Hec.Dss.Tests checks this against hecdss.h), except the
// three that 7-JA-7 no longer exports and the return type of hec_dss_julianToYearMonthDay, noted below.
// hec_dss_open also maps 7-JA-7's failure for DSS 6 files back to 7-IU-6's status (see there).
// The library itself is loaded by a resolver (end of this file) that rejects any other version.
internal class DssNative
{
  [DllImport("hecdss")]
  public static extern int hec_dss_CONSTANT_MAX_PATH_SIZE();

  // XGoodFit port: 7-JA-7 can't read DSS 6 files at all, so hec_dss_open fails for them in zopen and
  // returns that error. 7-IU-6 opened them and returned -700, which DssReader reports as "This library
  // only supports DSS version 7 files". A DSS 6 file's failure is mapped back to -700 to keep that.
  [DllImport("hecdss", EntryPoint = "hec_dss_open")]
   private static extern int hec_dss_open_native(string fileName, out IntPtr dss);

   public static int hec_dss_open(string fileName, out IntPtr dss)
   {
     int status = hec_dss_open_native(fileName, out dss);
     if (status != 0 && hec_dss_getFileVersion(fileName) == 6)
       return OnlyDssVersion7;
     return status;
   }


  // XGoodFit port: not exported by 7-JA-7, which dropped DSS 6 support (DSS 6 files couldn't be
  // opened through hec_dss_open with 7-IU-6 either). Calling it throws EntryPointNotFoundException;
  // only DssWriter's DSS 6 conversion does.
  [DllImport("hecdss")]
  public static extern int hec_dss_convertToVersion7(string fileName6, string fileName7);

  [DllImport("hecdss")]
  public static extern int hec_dss_getFileVersion(string fileName);

  [DllImport("hecdss")]
   public static extern int hec_dss_close(IntPtr dss);

   [DllImport("hecdss")]
   internal static extern int hec_dss_getVersion(IntPtr dss);

  [DllImport("hecdss")]
  internal static extern int hec_dss_set_value(string name, int value);

  [DllImport("hecdss")]
  internal static extern int hec_dss_set_string(string name, string value);


  [DllImport("hecdss")]
  internal static extern int hec_dss_squeeze(string fileName);

  [DllImport("hecdss")]
  internal static extern int hec_dss_delete(IntPtr dss, string path);


  [DllImport("hecdss")]
   public static extern int hec_dss_record_count(IntPtr dss);

   // XGoodFit port: this and hec_dss_deprecated_ifltab_print aren't exported by 7-JA-7 (or 7-IU-6),
   // and nothing calls them.
   [DllImport("hecdss")]
   public static extern IntPtr hec_dss_deprecated_ifltab(IntPtr dss);

   [DllImport("hecdss")]
   public static extern void hec_dss_deprecated_ifltab_print(IntPtr ifltab);

  [DllImport("hecdss")]
  public static extern int hec_dss_dataType(IntPtr dss, string pathname);


  // XGoodFit port: 7-JA-7's signature (hec-dss #271 added timeZoneName, #280 storageFlag).
  // timeZoneName must not be null: 7-JA-7 copies it with strlen.
  [DllImport("hecdss")]
  public static extern int hec_dss_tsStoreRegular(IntPtr dss, string pathname,
            string startDate, string startTime,
            double[] valueArray, int arraySize,
            int[] qualityArray, int qualityArraySize,
            int saveAsFloat,
            string units, string type, string timeZoneName, int storageFlag);

  // XGoodFit port: upstream's signature, as 7-IU-6 behaved: no time zone, and always replacing
  // existing data (storage flag 0, REPLACE_ALL).
  public static int hec_dss_tsStoreRegular(IntPtr dss, string pathname,
            string startDate, string startTime,
            double[] valueArray, int arraySize,
            int[] qualityArray, int qualityArraySize,
            int saveAsFloat,
            string units, string type)
  {
    return hec_dss_tsStoreRegular(dss, pathname, startDate, startTime, valueArray, arraySize,
      qualityArray, qualityArraySize, saveAsFloat, units, type, NoTimeZone, RegularStoreReplaceAll);
  }

  // XGoodFit port: 7-JA-7's signature (hec-dss #271 added timeZoneName, #280 storageFlag).
  // timeZoneName must not be null: 7-JA-7 copies it with strlen.
  [DllImport("hecdss")]
  public static extern int hec_dss_tsStoreIregular(IntPtr dss, string pathname,
  string startDateBase,
  int[] times, int timeGranularitySeconds,
  double[] valueArray, int valueArraySize,
  int[] qualityArray, int qualityArraySize,
  int saveAsFloat,
  string units, string type, string timeZoneName, int storageFlag);

  // XGoodFit port: upstream's signature, as 7-IU-6 behaved: no time zone, and always replacing old
  // data with new (storage flag 1, DELETE_INSERT).
  public static int hec_dss_tsStoreIregular(IntPtr dss, string pathname,
  string startDateBase,
  int[] times, int timeGranularitySeconds,
  double[] valueArray, int valueArraySize,
  int[] qualityArray, int qualityArraySize,
  int saveAsFloat,
  string units, string type)
  {
    return hec_dss_tsStoreIregular(dss, pathname, startDateBase, times, timeGranularitySeconds,
      valueArray, valueArraySize, qualityArray, qualityArraySize, saveAsFloat, units, type,
      NoTimeZone, IrregularStoreReplace);
  }

   // XGoodFit port: 7-JA-7's signature (hec-dss #271 added the time-zone buffer and its length).
   [DllImport("hecdss", CharSet = CharSet.Ansi, ExactSpelling = true)]
   public static extern int hec_dss_tsRetrieve(IntPtr dss, string pathname,
                                     string startDate, string startTime,
                                     string endDate, string endTime,
                                     int[] timeArray, double[] valueArray, int arraySize,
                                     ref int numberValuesRead,int[] quality, int qualityLength, 
                                     ref int julianBaseDate, ref int timeGranularitySeconds,
                                     byte[] units, int unitsLength, byte[] type, int typeLength,
                                     byte[] timeZoneName, int timeZoneNameLength);

   // XGoodFit port: upstream's signature. The record's time zone, which 7-IU-6 didn't return, is
   // read into a buffer of its own and dropped.
   public static int hec_dss_tsRetrieve(IntPtr dss, string pathname,
                                     string startDate, string startTime,
                                     string endDate, string endTime,
                                     int[] timeArray, double[] valueArray, int arraySize,
                                     ref int numberValuesRead,int[] quality, int qualityLength,
                                     ref int julianBaseDate, ref int timeGranularitySeconds,
                                     byte[] units, int unitsLength, byte[] type, int typeLength)
   {
     byte[] timeZoneName = new byte[TimeZoneNameBufferSize];
     return hec_dss_tsRetrieve(dss, pathname, startDate, startTime, endDate, endTime,
       timeArray, valueArray, arraySize, ref numberValuesRead, quality, qualityLength,
       ref julianBaseDate, ref timeGranularitySeconds, units, unitsLength, type, typeLength,
       timeZoneName, timeZoneName.Length);
   }


  [DllImport("hecdss")]
  internal static extern int hec_dss_tsGetDateTimeRange(IntPtr dss, string pathname, int boolFullSet,
                                            ref int firstValidJulian, ref int firstSeconds,
                                            ref int lastValidJulian, ref int lastSeconds);

   [DllImport("hecdss", CharSet = CharSet.Ansi, ExactSpelling = true)] 
   public static extern int hec_dss_tsGetSizes(IntPtr dss, string pathname,
                                    string startDate, string startTime, 
                                    string endDate, string endTime,
                                    ref int numberValues, ref int qualityElementSize);

  [DllImport("hecdss")]
  public static extern int hec_dss_tsRetrieveInfo(IntPtr dss, string pathname,
                          byte[] units, int unitsLength,
                          byte[] type, int typeLength);


  [DllImport("hecdss")]
  public static extern int hec_dss_catalog(IntPtr dss, byte[] pathBuffer,int[] recordTypes,[In] byte[] pathFilter,
    int count, int pathBufferItemSize);

  [DllImport("hecdss")]
  public static extern int hec_dss_dateToYearMonthDay(string date,ref int year, ref int month, ref int day);

  [DllImport("hecdss")]
  public static extern int hec_dss_dateToJulian(string date);

  // XGoodFit port: unchanged, although the C function returns void (in 7-IU-6 too). Nothing reads
  // the int, and an unset return register is harmless.
  [DllImport("hecdss")]
  public static extern int hec_dss_julianToYearMonthDay(int julian, ref int year, ref int month, ref int day);


  [DllImport("hecdss")]
  public static extern int hec_dss_locationRetrieve(IntPtr dss, string fullPath, ref double x, ref double y, ref double z,
                            ref int coordinateSystem, ref int coordinateID,
                            ref int horizontalUnits, ref int horizontalDatum,
                            ref int verticalUnits, ref int verticalDatum,
                            byte[] timeZoneName, int timeZoneNameLength,
                            byte[] supplemental, int supplementalLength);

  [DllImport("hecdss")]
  public static extern int hec_dss_locationStore(IntPtr dss, string fullPath,
  double x,  double y,  double z,
   int coordinateSystem,  int coordinateID,
   int horizontalUnits,  int horizontalDatum,
   int verticalUnits,  int verticalDatum,
   string timeZoneName,
   string supplemental,
   int replace);

  [DllImport("hecdss")] 
  public static extern int hec_dss_pdRetrieveInfo(IntPtr dss, string pathname,
                            ref int numberOrdinates, ref int numberCurves,
                            byte[] unitsIndependent, int unitsIndependentLength,
                            byte[] unitsDependent, int unitsDependentLength,
                            byte[] typeIndependent, int typeIndependentLength,
                            byte[] typeDependent, int typeDependentLength,
                            ref int labelsLength);

  // XGoodFit port: 7-JA-7's signature (hec-dss #271 added the time-zone buffer and its length).
  [DllImport("hecdss")]
  public static extern int hec_dss_pdRetrieve(IntPtr dss, string pathname,
  double[] Ordinates, int OrdinatesLength,
  double[] Values, int ValuesLength,
  ref int numberOrdinates, ref int numberCurves,
  byte[] unitsIndependent, int unitsIndependentLength,
  byte[] typeIndependent, int typeIndependentLength,
  byte[] unitsDependent, int unitsDependentLength,
  byte[] typeDependent, int typeDependentLength,
  byte[] labels, int labelsLength,
  byte[] timeZoneName, int timeZoneNameLength);

  // XGoodFit port: upstream's signature. The record's time zone, which 7-IU-6 didn't return, is
  // read into a buffer of its own and dropped.
  public static int hec_dss_pdRetrieve(IntPtr dss, string pathname,
  double[] Ordinates, int OrdinatesLength,
  double[] Values, int ValuesLength,
  ref int numberOrdinates, ref int numberCurves,
  byte[] unitsIndependent, int unitsIndependentLength,
  byte[] typeIndependent, int typeIndependentLength,
  byte[] unitsDependent, int unitsDependentLength,
  byte[] typeDependent, int typeDependentLength,
  byte[] labels, int labelsLength)
  {
    byte[] timeZoneName = new byte[TimeZoneNameBufferSize];
    return hec_dss_pdRetrieve(dss, pathname, Ordinates, OrdinatesLength, Values, ValuesLength,
      ref numberOrdinates, ref numberCurves, unitsIndependent, unitsIndependentLength,
      typeIndependent, typeIndependentLength, unitsDependent, unitsDependentLength,
      typeDependent, typeDependentLength, labels, labelsLength, timeZoneName, timeZoneName.Length);
  }

  // XGoodFit port: 7-JA-7's signature (hec-dss #271 added timeZoneName). It must not be null:
  // 7-JA-7 copies it with strlen.
  [DllImport("hecdss")]
  public static extern int hec_dss_pdStore(IntPtr dss, string pathname,
  double[] Ordinates, int OrdinatesLength,
  double[] Values, int ValuesLength,
  int numberOrdinates, int numberCurves,
  string unitsIndependent,
  string typeIndependent,
  string unitsDependent,
  string typeDependent, 
  byte[] labels, int labelsLength,
  string timeZoneName);

  // XGoodFit port: upstream's signature, as 7-IU-6 behaved: no time zone.
  public static int hec_dss_pdStore(IntPtr dss, string pathname,
  double[] Ordinates, int OrdinatesLength,
  double[] Values, int ValuesLength,
  int numberOrdinates, int numberCurves,
  string unitsIndependent,
  string typeIndependent,
  string unitsDependent,
  string typeDependent,
  byte[] labels, int labelsLength)
  {
    return hec_dss_pdStore(dss, pathname, Ordinates, OrdinatesLength, Values, ValuesLength,
      numberOrdinates, numberCurves, unitsIndependent, typeIndependent, unitsDependent,
      typeDependent, labels, labelsLength, NoTimeZone);
  }

  [DllImport("hecdss")]
  public static extern int hec_dss_gridRetrieve(IntPtr dss, string pathname, int boolRetrieveData,
  ref int type, ref int dataType,
  ref int lowerLeftCellX, ref int lowerLeftCellY,
  ref int numberOfCellsX, ref int numberOfCellsY,
  ref int numberOfRanges, ref int srsDefinitionType,
  ref int timeZoneRawOffset, ref int isInterval,
  ref int isTimeStamped,
  byte[] dataUnits, int dataUnitsLength,
  byte[] dataSource, int dataSourceLength,
  byte[] srsName, int srsNameLength,
  byte[] srsDefinition, int srsDefinitionLength,
  byte[] timeZoneID, int timeZoneIDLength,
  ref float cellSize, ref float xCoordOfGridCellZero,
  ref float yCoordOfGridCellZero, ref float nullValue,
  ref float maxDataValue, ref float minDataValue,
  ref float meanDataValue,
  float[] rangeLimitTable, int rangeTablesLength,
  int[] numberEqualOrExceedingRangeLimit,
  float[] data, int dataLength);



  // XGoodFit port: 7-JA-7's signature (hec-dss #341 added compressionSize: the size of data that's
  // already compressed, or 0). Upstream's trailing dataLength was never a native parameter.
  [DllImport("hecdss")]
  public static extern int hec_dss_gridStore(IntPtr dss, string pathname,
  int gridType, int dataType,
  int lowerLeftCellX, int lowerLeftCellY,
  int numberOfCellsX, int numberOfCellsY,
  int numberOfRanges, int srsDefinitionType,
  int timeZoneRawOffset, int isInterval,
  int isTimeStamped,
  int compressionSize,
  string dataUnits,
  string dataSource,
  string srsName,
  string srsDefinition,
  string timeZoneID,
  float cellSize, float xCoordOfGridCellZero,
  float yCoordOfGridCellZero, float nullValue,
  float maxDataValue, float minDataValue,
  float meanDataValue,
  float[] rangeLimitTable,
  int[] numberEqualOrExceedingRangeLimit,
  float[] data);

  // XGoodFit port: upstream's signature, as 7-IU-6 behaved: the library compresses the data
  // (compressionSize 0). dataLength was ignored then too.
  public static int hec_dss_gridStore(IntPtr dss, string pathname,
  int gridType, int dataType,
  int lowerLeftCellX, int lowerLeftCellY,
  int numberOfCellsX, int numberOfCellsY,
  int numberOfRanges, int srsDefinitionType,
  int timeZoneRawOffset, int isInterval,
  int isTimeStamped,
  string dataUnits,
  string dataSource,
  string srsName, 
  string srsDefinition, 
  string timeZoneID, 
  float cellSize, float xCoordOfGridCellZero,
  float yCoordOfGridCellZero, float nullValue,
  float maxDataValue, float minDataValue,
  float meanDataValue,
  float[] rangeLimitTable, 
  int[] numberEqualOrExceedingRangeLimit,
  float[] data, int dataLength)
  {
    return hec_dss_gridStore(dss, pathname, gridType, dataType, lowerLeftCellX, lowerLeftCellY,
      numberOfCellsX, numberOfCellsY, numberOfRanges, srsDefinitionType, timeZoneRawOffset,
      isInterval, isTimeStamped, NotPreCompressed, dataUnits, dataSource, srsName, srsDefinition,
      timeZoneID, cellSize, xCoordOfGridCellZero, yCoordOfGridCellZero, nullValue, maxDataValue,
      minDataValue, meanDataValue, rangeLimitTable, numberEqualOrExceedingRangeLimit, data);
  }

  // XGoodFit port: what upstream's signatures pass for the parameters 7-JA-7 added, as 7-IU-6
  // behaved (hecdss.c before hec-dss #271, e.g. ee6dff5), and 7-IU-6's status for DSS 6 files.
  private const string NoTimeZone = "";
  private const int RegularStoreReplaceAll = 0;
  private const int IrregularStoreReplace = 1;
  private const int NotPreCompressed = 0;
  private const int TimeZoneNameBufferSize = 64;
  private const int OnlyDssVersion7 = -700;

  // XGoodFit port: everything below loads and checks the native library.
  //
  // Upstream leaves "hecdss" to the default search. Here a resolver loads it, first from
  // runtimes/<rid>/native beside this assembly (where projects that reference src/Hec.Dss get the
  // libraries native/build-hecdss.sh builds), then by the default search (a NuGet package's
  // runtimes folder, or the application folder after a RID-specific publish). The library must
  // report 7-JA-7's hec_dss_api_version(): with any other version the declarations above may not
  // match, and a mismatched call corrupts memory instead of failing. A library that doesn't match,
  // such as HEC's 7-IU-6 (which has no hec_dss_api_version), is rejected with DllNotFoundException,
  // the same as a missing one; RMC-BestFit reports that as the native library not being found.
  // The checked library also answers other assemblies' DllImport("hecdss") when the default search
  // finds nothing for them (RMC.BestFit.UI declares hec_dss_tsGetDateTimeRange itself).

  /// <summary>The library name every declaration above uses.</summary>
  internal const string LibraryName = "hecdss";

  /// <summary>hec_dss_api_version() of HEC-DSS 7-JA-7, the version the declarations above match.</summary>
  internal const string ExpectedApiVersion = "0.3.0";

  [DllImport("hecdss")]
  internal static extern IntPtr hec_dss_api_version();

  private static readonly object loadLock = new object();
  private static IntPtr library;
  [ThreadStatic] private static bool loading;

  /// <summary>The library the declarations above are bound to, once one has been loaded.</summary>
  internal static IntPtr Library
  {
    get { lock (loadLock) return library; }
  }

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate IntPtr ApiVersionFunction();

  // The resolver is registered before any declaration in this class is first called, so before the
  // library is first bound.
  static DssNative()
  {
    RegisterResolver();
  }

  private static void RegisterResolver()
  {
    Assembly assembly = typeof(DssNative).Assembly;
    NativeLibrary.SetDllImportResolver(assembly, (name, requester, searchPath) =>
      IsHecDss(name) ? Load(requester, searchPath) : IntPtr.Zero);
    AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(assembly);
    if (context != null)
      context.ResolvingUnmanagedDll += (requester, name) => IsHecDss(name) ? Load(assembly, null) : IntPtr.Zero;
  }

  private static bool IsHecDss(string name)
  {
    return name == LibraryName || name == LibraryFileName;
  }

  /// <summary>The library's file name on this OS.</summary>
  internal static string LibraryFileName
  {
    get
    {
      if (OperatingSystem.IsWindows()) return "hecdss.dll";
      if (OperatingSystem.IsMacOS()) return "libhecdss.dylib";
      return "libhecdss.so";
    }
  }

  /// <summary>The runtime identifier whose runtimes folder holds this process's library, or null.</summary>
  internal static string RuntimeIdentifier
  {
    get
    {
      string os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : OperatingSystem.IsLinux() ? "linux" : null;
      string arch = RuntimeInformation.ProcessArchitecture switch
      {
        Architecture.X64 => "x64",
        Architecture.Arm64 => "arm64",
        Architecture.X86 => "x86",
        _ => null,
      };
      return os == null || arch == null ? null : os + "-" + arch;
    }
  }

  /// <summary>Where the library is looked for before the default search.</summary>
  internal static IEnumerable<string> BundledLibraryPaths(Assembly assembly)
  {
    string rid = RuntimeIdentifier;
    if (rid == null)
      return Enumerable.Empty<string>();
    var folders = new List<string>();
    if (!string.IsNullOrEmpty(assembly.Location))
      folders.Add(Path.GetDirectoryName(assembly.Location));
    folders.Add(AppContext.BaseDirectory);
    return folders.Select(folder => Path.GetFullPath(Path.Combine(folder, "runtimes", rid, "native", LibraryFileName)))
      .Distinct(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
  }

  private static IntPtr Load(Assembly assembly, DllImportSearchPath? searchPath)
  {
    lock (loadLock)
    {
      // The default search below may raise ResolvingUnmanagedDll, which comes back here.
      if (library != IntPtr.Zero || loading)
        return library;
      loading = true;
      try
      {
        string source = BundledLibraryPaths(assembly).FirstOrDefault(File.Exists);
        IntPtr handle;
        if (source != null)
        {
          handle = NativeLibrary.Load(source);
        }
        else
        {
          try
          {
            handle = NativeLibrary.Load(LibraryName, assembly, searchPath);
          }
          catch (DllNotFoundException e)
          {
            throw new DllNotFoundException("The HEC-DSS native library (" + LibraryFileName + ") wasn't found in " +
              string.Join(" or ", BundledLibraryPaths(assembly)) + ", or by the default search: " + e.Message, e);
          }
          source = "the default search for " + LibraryName;
        }

        try
        {
          CheckApiVersion(handle, source);
        }
        catch
        {
          NativeLibrary.Free(handle);
          throw;
        }
        library = handle;
        return library;
      }
      finally
      {
        loading = false;
      }
    }
  }

  /// <summary>
  /// Returns the library's hec_dss_api_version(), or null if it doesn't export that function (as
  /// HEC-DSS releases before 7-JA-7 don't).
  /// </summary>
  internal static string ReadApiVersion(IntPtr handle)
  {
    if (!NativeLibrary.TryGetExport(handle, "hec_dss_api_version", out IntPtr export))
      return null;
    IntPtr version = Marshal.GetDelegateForFunctionPointer<ApiVersionFunction>(export)();
    return Marshal.PtrToStringAnsi(version);
  }

  /// <summary>Throws DllNotFoundException unless the library is the version these declarations match.</summary>
  internal static void CheckApiVersion(IntPtr handle, string source)
  {
    string version = ReadApiVersion(handle);
    if (version == ExpectedApiVersion)
      return;
    string found = version == null ? "has no hec_dss_api_version(), so it predates HEC-DSS 7-JA-7" : "reports API version " + version;
    throw new DllNotFoundException("The HEC-DSS native library from " + source + " " + found + ". This Hec.Dss " +
      "declares the functions of HEC-DSS 7-JA-7 (API version " + ExpectedApiVersion + "), and calling another " +
      "version could corrupt memory, so the library isn't used.");
  }

}

